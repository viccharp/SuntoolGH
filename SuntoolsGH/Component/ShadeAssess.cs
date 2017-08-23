using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using MIConvexHull;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SunTools.Component
{
    public class ShadeAssess : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public ShadeAssess()
            : base("Shade Assessment", "ShadeAssess",
                "Shading modules geometric performance (direct lighting) by projection of shade on window ",
                "SunTools", "Shading")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("window Panel Mesh", "mfpanel", "A planar mesh representing a panel of a window", GH_ParamAccess.item);
            pManager.AddMeshParameter("Shade Module Mesh", "mshade", "A list of shade meshes to be evaluated for direct shade coverage", GH_ParamAccess.list);
            pManager.AddVectorParameter("Projection Vector", "dir", "The vectors for shading evaluation (sun vectors)", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Launch the analysis", "start", "If bool is True: analysis is running, if bool is False: analysis stopped", GH_ParamAccess.item);

        }

        /// <summary>
        /// Registers all the output parameters for this component
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh of exposed windows surface", "ExposedMesh", "Tree of exposed window surface mesh on the window panel", GH_ParamAccess.list);
            pManager.AddNumberParameter("Area of exposed", "ExposedArea", "Area of the exposed part of the window panel", GH_ParamAccess.tree);
            pManager.AddTextParameter("Comment on operation type", "outCom",
                "", GH_ParamAccess.tree);
            pManager.AddMeshParameter("debug meshcutter", "meshcutter", "", GH_ParamAccess.tree);

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="da">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess da)
        {
            // input variables
            var windowPanel = new Mesh();
            var shadeModule = new List<Mesh>();
            var sunVector = new List<Vector3d>();
            var run = new bool();

            da.GetData(0, ref windowPanel);
            da.GetDataList(1, shadeModule);
            da.GetDataList(2, sunVector);
            da.GetData(3, ref run);

            const double tol = 0.0001;

            if (run == false) { return; }

            // output variables
            var result = new GH_Structure<GH_Mesh>();
            var areaResult = new GH_Structure<GH_Number>();
            var comment = new GH_Structure<GH_String>();

            // Define window outline as a nurbscurve
            if (!windowPanel.IsValid) { return; }
            var panelOutlineList = new List<Polyline>(windowPanel.GetNakedEdges());
            if (panelOutlineList.Count > 1) { return; }
            var panelOutline = new Polyline(panelOutlineList[0]).ToNurbsCurve();

            // Define window panel plane
            var windowPlane = new Plane();
            Plane.FitPlaneToPoints(panelOutlineList[0], out windowPlane);

            // Surface area and centroid of window panel
            var propWindowPanel = AreaMassProperties.Compute(panelOutline);
            var windowArea= propWindowPanel.Area;
            var centroidWindowPanel = propWindowPanel.Centroid;

            // Define normal of window panel
            windowPanel.Normals.ComputeNormals();
            var wNV = windowPanel.Normals[0];
            wNV.Unitize();

            // Debug var
            var MshCttr = new GH_Structure<GH_Mesh>();

            for (int i = 0; i < shadeModule.Count; i++)
            {
                // Define Shade path for output
                var p1 = new GH_Path(i);

                for (int j = 0; j < sunVector.Count; j++)
                {
                    // Create projection transform of shade to panel plane along the sun vector direction
                    var projectToWindowPanel = new Transform();
                    projectToWindowPanel = GetObliqueTransformation(windowPlane, sunVector[j]);

                    // Project shade outline to panel plane along the sun vector direction
                    var currentShadeProj = shadeModule[i];
                    currentShadeProj.Transform(projectToWindowPanel);

                    // Create currentShadeProj Outline
                    var currentShadeProjOutline = currentShadeProj.GetNakedEdges();

                    // Define cutter for mesh
                    var tempCutter = new List<Surface>();
                    for (int k=0; k < currentShadeProjOutline.Length; k++)
                    {
                        tempCutter.Add(Surface.CreateExtrusion(currentShadeProjOutline[k].ToNurbsCurve(), Vector3d.Multiply(new Vector3d(wNV), 1.0)));
                    }
                    foreach (var mshcuttemp in tempCutter) { mshcuttemp.Translate(Vector3d.Multiply(new Vector3d(wNV), -0.5)); }
                    var tempCutterBrep = tempCutter.Select(p => p.ToBrep());
                    var meshCutterLst = new List<Mesh>();
                    for (int k = 0; k < tempCutterBrep.ToList().Count; k++)
                    {
                        meshCutterLst.AddRange(Mesh.CreateFromBrep(tempCutterBrep.ToList()[k], MeshingParameters.Coarse));
                    }
                    var meshCutter = meshCutterLst.ToArray();
                    MshCttr.AppendRange(meshCutter.Select(p=>new GH_Mesh(p)), p1);
                    

                    // Convex hull of the projected mesh's vertices
                    var convexHullCurve = ConvexHullMesh(currentShadeProj, windowPlane);

                    // Determine the difference of the panel outline - the shade outline
                    RegionContainment status = Curve.PlanarClosedCurveRelationship(panelOutline, convexHullCurve, windowPlane, tol);

                    switch (status)
                    {
                        case RegionContainment.Disjoint:
                            //result.Append(new GH_Mesh(windowPanel), p1);
                            //areaResult.Append(new GH_Number(windowArea), p1);
                            comment.Append(new GH_String("Disjoint, case 1"),p1);
                            break;
                        case RegionContainment.MutualIntersection:
                            comment.Append(new GH_String("MutualIntersection, intersection of projected source and wall"), p1);

                            //var splitMesh = windowPanel.Split(meshCutter); ;
                            //var minCentroidDistance = 9999.0;
                            //var resultMesh = new Mesh();

                            break;
                        case RegionContainment.AInsideB:
                            //result.Append(new GH_Mesh(windowPanel), p1);
                            //areaResult.Append(new GH_Number(windowArea), p1);
                            comment.Append(new GH_String("A Inside B, the window is inside the convex hull of the projected shade module, case 3a"), p1);
                            break;
                        case RegionContainment.BInsideA:
                            var splitMesh = windowPanel.Split(meshCutter); ;
                            var minCentroidDistance = 9999.0;
                            var resultMesh = new Mesh();

                            result.AppendRange(splitMesh.Select(p => new GH_Mesh(p)), p1);
                            //areaResult.Append(new GH_Number(windowArea - AreaMassProperties.Compute(currentShadeProj).Area), p1);
                            comment.Append(new GH_String("B Inside A,  the shade module projection is inside the window, case 4"), p1);
                            break;
                    }
                }
            }

            da.SetDataTree(0, result);
            da.SetDataTree(1, areaResult);
            da.SetDataTree(2, comment);
            da.SetDataList(3, MshCttr);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }


        /**
        See: http://stackoverflow.com/questions/2500499/howto-project-a-planar-p...
        1-a*dx/D    -b*dx/D    -c*dx/D   -d*dx/D
        -a*dy/D   1-b*dy/D    -c*dy/D   -d*dy/D
        -a*dz/D    -b*dz/D   1-c*dz/D   -d*dz/D
        0          0          0         1
       */
        public Transform GetObliqueTransformation(Plane pln, Vector3d v)
        {

            Transform oblique = new Transform(1);
            double[] eq = pln.GetPlaneEquation();
            double a, b, c, d, dx, dy, dz, D;
            a = eq[0];
            b = eq[1];
            c = eq[2];
            d = eq[3];
            dx = v.X;
            dy = v.Y;
            dz = v.Z;
            D = a * dx + b * dy + c * dz;
            oblique.M00 = 1 - a * dx / D;
            oblique.M10 = -a * dy / D;
            oblique.M20 = -a * dz / D;
            oblique.M30 = 0;
            oblique.M01 = -b * dx / D;
            oblique.M11 = 1 - b * dy / D;
            oblique.M21 = -b * dz / D;
            oblique.M31 = 0;
            oblique.M02 = -c * dx / D;
            oblique.M12 = -c * dy / D;
            oblique.M22 = 1 - c * dz / D;
            oblique.M32 = 0;
            oblique.M03 = -d * dx / D;
            oblique.M13 = -d * dy / D;
            oblique.M23 = -d * dz / D;
            oblique.M33 = 1;
            return oblique;
        }


        public NurbsCurve ConvexHullMesh(Mesh msh, Plane pl)
        {

            var worldPlane = new Plane(new Point3d(0.0, 0.0, 0.0), new Vector3d(0.0, 0.0, 1.0));
            var BC = Transform.ChangeBasis(worldPlane, pl);
            var BCB = Transform.ChangeBasis(pl, worldPlane);
            var tempMsh = new Mesh();
            tempMsh.CopyFrom(msh);

            tempMsh.Transform(BC);
            var vertices = new Vertex2D[tempMsh.Vertices.Count];
            var mshPts = tempMsh.Vertices.ToPoint3dArray();
            for (int i = 0; i < tempMsh.Vertices.Count; i++) vertices[i] = new Vertex2D(mshPts[i].X, mshPts[i].Y);

            var convexHull = ConvexHull.Create<Vertex2D>(vertices);
            var hullPts = DoubleArraytoPts3DList(convexHull.Points.Select(p => p.Position).ToArray());

            var hullPtsWorld = new Point3d[hullPts.Count];
            hullPts.CopyTo(hullPtsWorld);
            for (int i = 0; i < hullPts.Count; i++) hullPtsWorld[i].Transform(BCB);

            var hullCurve = new Polyline(hullPtsWorld);
            if (!hullCurve.IsClosed) { hullCurve.Add(hullCurve[0]); }

            return hullCurve.ToNurbsCurve();
        }

        public List<Point3d> DoubleArraytoPts3DList(Double[][] vrtxArray)
        {
            var LstPts3D = new List<Point3d>();
            if (vrtxArray[0].Length == 2)
            {
                for (int i = 0; i < vrtxArray.Length; i++)
                {
                    LstPts3D.Add(new Point3d(vrtxArray[i][0], vrtxArray[i][1], 0));
                }
                return LstPts3D;
            }
            else
            {
                for (int i = 0; i < vrtxArray.Length; i++)
                {
                    LstPts3D.Add(new Point3d(vrtxArray[i][0], vrtxArray[i][1], vrtxArray[i][2]));
                }
                return LstPts3D;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{46928a0e-c0fe-4900-a8bb-b0b449c4b513}"); }
        }
    }
}
