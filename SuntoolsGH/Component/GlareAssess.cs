﻿using System;
using System.Collections.Generic;

using Rhino.Geometry;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using MIConvexHull;

using System.Linq;
using Grasshopper;

namespace SunTools.Component
{
    public class GlareAssess : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public GlareAssess()
          : base("Glare on wall panel", "GlareAssess",
                "Projection of exposed light on single room wall",
                "SunTools", "Comfort")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Wall Panel", "wpanel", "A list of planar meshes representing wall panels", GH_ParamAccess.item);
            pManager.AddMeshParameter("Source Outline", "sourceOL", "A list of closed curves as direct light sources", GH_ParamAccess.tree);
            pManager.AddVectorParameter("Projection direction vector", "dir", "List of vectors for projection onto the wall panels", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Launch the analysis", "start", "If bool is True: analysis is running, if bool is False: analysis stopped", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh of Glare on the wall panel", "GlareMesh", "Tree of meshes for glare on the wall panel", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Area of Glare on the wall panel", "GlareArea", "Area of the exposed part of the window panel", GH_ParamAccess.tree);
            pManager.AddTextParameter("Comment on operation type", "outCom", "", GH_ParamAccess.tree);
            pManager.AddMeshParameter("debug meshcutter", "meshcutter", "", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // input variables
            var wallPanel = new Mesh();
            var source = new GH_Structure<GH_Mesh>(); //this is the exposed surface on the window (source of glare)
            var sunVector = new List<Vector3d>();
            var run = new bool();

            DA.GetData(0, ref wallPanel);
            DA.GetDataTree(1, out source);
            DA.GetDataList(2, sunVector);
            DA.GetData(3, ref run);

            const double tol = 0.0001;

            if (run == false) { return; }


            // output variables
            var GlareMesh = new GH_Structure<GH_Mesh>();
            var glareAreas = new GH_Structure<GH_Number>();
            var comment = new GH_Structure<GH_String>();

            //debug
            var MshCttr = new GH_Structure<GH_Mesh>();

            // Define wall outline
            if (!wallPanel.IsValid) { return; }
            var panelOutlineList = new List<Polyline>(wallPanel.GetNakedEdges());
            if (panelOutlineList.Count > 1) { return; }

            // Define wall normal vector
            wallPanel.Normals.ComputeNormals();
            var wNV = wallPanel.Normals[0];
            wNV.Unitize();

            // Area and centroid of wall panel
            var propWallPanel = AreaMassProperties.Compute(wallPanel);
            var areaWallPanel = propWallPanel.Area;
            var centroidWallPanel = propWallPanel.Centroid;

            // Define panel outline as a nurbscurve
            var panelOutline = new Polyline(panelOutlineList[0]).ToNurbsCurve();
            if (!panelOutline.IsClosed) { panelOutline.MakeClosed(0.01); }

            // Define cutter for mesh
            var tempCutter = Surface.CreateExtrusion(panelOutline, wNV);
            tempCutter.Translate(Vector3d.Multiply(new Vector3d(wNV), -0.5));
            var meshCutter = Mesh.CreateFromBrep(tempCutter.ToBrep(), MeshingParameters.Smooth);

            // Define Wall plane
            Plane panelPlane = new Plane();
            Plane.FitPlaneToPoints(panelOutlineList[0], out panelPlane);


            for (int i = 0; i < source.PathCount; i++)
            {
                // Define source Path for output
                var areaPath = new GH_Path(i);

                if (source[i].Count != sunVector.Count) return;

                for (int j = 0; j < sunVector.Count; j++)
                {
                    // Define projection transform
                    var currentProjTrans = GetObliqueTransformation(panelPlane, sunVector[j]);

                    // Define projected source mesh
                    var currentProjSource = new Mesh();
                    currentProjSource.CopyFrom(source[i][j].Value);
                    if (!currentProjSource.IsValid)
                    {
                        comment.Append(new GH_String("invalid currentprojsource, mesh" + i.ToString() + " sun vector" + j.ToString() + ", the window panel is inside the projected shade or about"),
                            areaPath);
                        GlareMesh.Append(null, areaPath);
                        glareAreas.Append(new GH_Number(0.00), areaPath);
                    }
                    else
                    {


                        currentProjSource.Transform(currentProjTrans);


                        // Define centroid of current projected source
                        var currentProjOutCentroid = AreaMassProperties.Compute(currentProjSource).Centroid;
                        var dsjtMeshCount = currentProjSource.DisjointMeshCount;

                        // Convex hull of the mesh vertices
                        if (!currentProjSource.IsValid)
                        {
                            throw new NullReferenceException(
                                "The projected source mesh is invalid, convexHull fail, shade number: " +
                                i.ToString() + " ,Sun vector number: " + j.ToString());
                        }
                        var convexHullCurve = ConvexHullMesh(currentProjSource, panelPlane);

                        RegionContainment status =
                            Curve.PlanarClosedCurveRelationship(panelOutline, convexHullCurve, panelPlane, tol);

                        switch (status)
                        {
                            case RegionContainment.Disjoint:
                                comment.Append(new GH_String("Disjoint"), areaPath);
                                GlareMesh.Append(null, areaPath);
                                glareAreas.Append(new GH_Number(0.00), areaPath);

                                break;

                            case RegionContainment.MutualIntersection:


                                var splitMesh = currentProjSource.Split(meshCutter[0]);
                                //MshCttr.AppendRange(splitMesh.Select(p => new GH_Mesh(p)), areaPath);
                                MshCttr.Append(new GH_Mesh(currentProjSource), areaPath);
                                var minCentroidDistance = 9999.0;
                                var resultMesh = new Mesh();
                                for (var index = 0; index < splitMesh.Length; index++)
                                {
                                    var tempmesh = splitMesh[index];
                                    var centroidTempMesh = AreaMassProperties.Compute(tempmesh).Centroid;

                                    var tempDistanceCentroid = (centroidWallPanel - centroidTempMesh).Length;
                                    if (!(tempDistanceCentroid < minCentroidDistance)) continue;
                                    minCentroidDistance = tempDistanceCentroid;
                                    resultMesh = tempmesh;

                                    if (resultMesh.DisjointMeshCount == dsjtMeshCount) continue;
                                    var dsjtCurrentProjSource = currentProjSource.SplitDisjointPieces();
                                    foreach (var dsjtmesh in dsjtCurrentProjSource)
                                    {
                                        var tempHullCurve = ConvexHullMesh(dsjtmesh, panelPlane);
                                        if (Curve.PlanarClosedCurveRelationship(panelOutline, tempHullCurve, panelPlane,
                                                tol) == RegionContainment.BInsideA)
                                            resultMesh.Append(dsjtmesh);
                                    }
                                }

                                var areaResultMesh = AreaMassProperties.Compute(resultMesh).Area;
                                comment.Append(
                                    new GH_String(
                                        "MutualIntersection, intersection of projected source and wall, tempMesh case, " +
                                        panelOutline.Contains(AreaMassProperties.Compute(resultMesh).Centroid) +
                                        " , length of splitMesh " + splitMesh.Length.ToString()),
                                    areaPath);
                                if (panelOutline.Contains(AreaMassProperties.Compute(resultMesh).Centroid) ==
                                    PointContainment.Outside)
                                {
                                    resultMesh = null;
                                    areaResultMesh = 0;
                                }
                                GlareMesh.Append(new GH_Mesh(resultMesh), areaPath);
                                glareAreas.Append(new GH_Number(areaResultMesh), areaPath);
                                break;

                            case RegionContainment.AInsideB:
                                comment.Append(new GH_String("AInsideB, wall inside convexhull of projected source"),
                                    areaPath);
                                GlareMesh.Append(new GH_Mesh(wallPanel), areaPath);
                                glareAreas.Append(new GH_Number(areaWallPanel), areaPath);

                                break;

                            case RegionContainment.BInsideA:
                                comment.Append(new GH_String("BInsideA, projected source wholly inside wall"),
                                    areaPath);
                                GlareMesh.Append(new GH_Mesh(currentProjSource), areaPath);
                                glareAreas.Append(new GH_Number(AreaMassProperties.Compute(currentProjSource).Area),
                                    areaPath);

                                break;
                        }

                    }
                }

            }


            DA.SetDataTree(0, GlareMesh);
            DA.SetDataTree(1, glareAreas);
            DA.SetDataTree(2, comment);
            DA.SetDataTree(3, MshCttr);
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

            var oblique = new Transform(1);
            var eq = pln.GetPlaneEquation();
            var a = eq[0];
            var b = eq[1];
            var c = eq[2];
            var d = eq[3];
            var dx = v.X;
            var dy = v.Y;
            var dz = v.Z;
            var D = a * dx + b * dy + c * dz;
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
            var bc = Transform.ChangeBasis(worldPlane, pl);
            var bcb = Transform.ChangeBasis(pl, worldPlane);
            var tempMsh = new Mesh();
            tempMsh.CopyFrom(msh);

            tempMsh.Transform(bc);
            var vertices = new Vertex2D[tempMsh.Vertices.Count];
            var mshPts = tempMsh.Vertices.ToPoint3dArray();
            for (int i = 0; i < tempMsh.Vertices.Count; i++) vertices[i] = new Vertex2D(mshPts[i].X, mshPts[i].Y);

            var convexHull = ConvexHull.Create<Vertex2D>(vertices);
            var hullPts = DoubleArraytoPts3DList(convexHull.Points.Select(p => p.Position).ToArray());

            var hullPtsWorld = new Point3d[hullPts.Count];
            hullPts.CopyTo(hullPtsWorld);
            for (int i = 0; i < hullPts.Count; i++) hullPtsWorld[i].Transform(bcb);

            var hullCurve = new Polyline(hullPtsWorld);
            if (!hullCurve.IsClosed) { hullCurve.Add(hullCurve[0]); }

            return hullCurve.ToNurbsCurve();
        }

        public List<Point3d> DoubleArraytoPts3DList(Double[][] vrtxArray)
        {
            var lstPts3D = new List<Point3d>();
            if (vrtxArray[0].Length == 2)
            {
                lstPts3D.AddRange(vrtxArray.Select(t => new Point3d(t[0], t[1], 0)));
                return lstPts3D;
            }
            else
            {
                lstPts3D.AddRange(vrtxArray.Select(t => new Point3d(t[0], t[1], t[2])));
                return lstPts3D;
            }
        }




        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("74432574-ca9a-419b-a67d-5915e367bd42"); }
        }
    }
}
