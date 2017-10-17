using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using ClipperLib;
using StudioAvw.Geometry;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms.VisualStyles;

namespace SunTools.Component
{
    public class ShadeAssessPC : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public ShadeAssessPC()
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
            pManager.AddBrepParameter("Mesh of exposed windows surface", "ExposedBrep", "Tree of exposed window surface mesh on the window panel", GH_ParamAccess.list);
            pManager.AddNumberParameter("Area of exposed", "ExposedArea", "Area of the exposed part of the window panel", GH_ParamAccess.tree);


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

            const double tol = 1e-2;

            if (run == false) { return; }

            // output variables
            var result = new GH_Structure<GH_Brep>();
            var areaResult = new GH_Structure<GH_Number>();
            var comment = new GH_Structure<GH_String>();

            // Define window outline as a nurbscurve
            if (!windowPanel.IsValid) { return; }
            var panelOutlineList = new List<Polyline>(windowPanel.GetNakedEdges());
            if (panelOutlineList.Count > 1) { return; }
            
            //Define type of clipping operation
            var type = (ClipType) 2;

            // Define window panel plane
            var windowPlane = new Plane();
            Plane.FitPlaneToPoints(panelOutlineList[0], out windowPlane);

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
                    var currentShadeProj = new Mesh();
                    currentShadeProj.CopyFrom(shadeModule[i]);
                    currentShadeProj.Transform(projectToWindowPanel);

                    // Create currentShadeProj Outline
                    var currentShadeProjOutline = currentShadeProj.GetNakedEdges();

                    List<Polyline> resultClipList = Polyline3D.Boolean(type, panelOutlineList, currentShadeProjOutline,
                        windowPlane, tol,true);

                    var resultBrep = Brep.CreatePlanarBreps(resultClipList.Select(t=>t.ToNurbsCurve()));
                    var resultArea = AreaMassProperties.Compute(resultBrep).Area;

                    result.AppendRange(resultBrep.Select(t=>new GH_Brep(t)), p1);
                    areaResult.Append(new GH_Number(resultArea), p1);
                }
            }

            da.SetDataTree(0, result);
            da.SetDataTree(1, areaResult);

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


        //public NurbsCurve ConvexHullMesh(Mesh msh, Plane pl)
        //{

        //    var worldPlane = new Plane(new Point3d(0.0, 0.0, 0.0), new Vector3d(0.0, 0.0, 1.0));
        //    var BC = Transform.ChangeBasis(worldPlane, pl);
        //    var BCB = Transform.ChangeBasis(pl, worldPlane);
        //    var tempMsh = new Mesh();
        //    tempMsh.CopyFrom(msh);

        //    tempMsh.Transform(BC);
        //    var vertices = new Vertex2D[tempMsh.Vertices.Count];
        //    var mshPts = tempMsh.Vertices.ToPoint3dArray();
        //    for (int i = 0; i < tempMsh.Vertices.Count; i++) vertices[i] = new Vertex2D(mshPts[i].X, mshPts[i].Y);

        //    var convexHull = ConvexHull.Create<Vertex2D>(vertices);
        //    var hullPts = DoubleArraytoPts3DList(convexHull.Points.Select(p => p.Position).ToArray());

        //    var hullPtsWorld = new Point3d[hullPts.Count];
        //    hullPts.CopyTo(hullPtsWorld);
        //    for (int i = 0; i < hullPts.Count; i++) hullPtsWorld[i].Transform(BCB);

        //    var hullCurve = new Polyline(hullPtsWorld);
        //    if (!hullCurve.IsClosed) { hullCurve.Add(hullCurve[0]); }

        //    return hullCurve.ToNurbsCurve();
        //}

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

        public static T[] RemoveAt<T>(int index)
        {
            return RemoveAtt<T>(new T[] { }, index);
        }

        public static T[] RemoveAtt<T>(T[] source, int index)
        {
            T[] dest = new T[source.Length - 1];
            if (index > 0)
                Array.Copy(source, 0, dest, 0, index);

            if (index < source.Length - 1)
                Array.Copy(source, index + 1, dest, index, source.Length - index - 1);

            return dest;
        }


        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{275cb518-2637-4875-8d05-55d714edfcfa}"); }
        }
    }
}
