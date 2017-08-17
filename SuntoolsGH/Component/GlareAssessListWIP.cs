using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace SunTools.Component
{
    public class GlareAssessListWIP : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public GlareAssessListWIP()
          : base("Glare on wall panels", "GlareAssessList",
                "Projection of exposed light on room walls",
                "SunTools", "Comfort")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Wall Panel", "wpanel", "A list of planar meshes representing wall panels", GH_ParamAccess.list);
            pManager.AddMeshParameter("Source Outline", "soutline", "A list of closed curves as direct light sources", GH_ParamAccess.list);
            pManager.AddVectorParameter("Projection direction vector", "dir", "List of vectors for projection onto the wall panels", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Launch the analysis", "start", "If bool is True: analysis is running, if bool is False: analysis stopped", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Outline of Glare", "Glare_Outline", "Tree of glare oultine on the wall panels", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Area of exposed", "Area_exposed", "Area of the exposed part of the façade panel", GH_ParamAccess.tree);
            pManager.AddTextParameter("Comment on operation type", "outCom","", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // input variables
            var wallPanel = new List<Mesh>();
            var source = new List<Mesh>();
            var sunVector = new List<Vector3d>();
            var run = new bool();

            DA.GetDataList(0, wallPanel);
            DA.GetDataList(1, source);
            DA.GetDataList(2, sunVector);
            DA.GetData(3, ref run);

            const double tol = 0.001;

            if (run == false) { return; }


            // output variables
            var glareOutline = new GH_Structure<GH_Curve>();
            var glareAreas = new GH_Structure<GH_Number>();

            // temporary output variables
            var result = new GH_Structure<GH_Curve>();
            var areaResult = new GH_Structure<GH_Number>();
            var comment = new GH_Structure<GH_String>();

            for (int i = 0; i < source.Count; i++)
            {
                // Define current wall panel
                var sourceOutlineList = new List<Polyline>(source[i].GetNakedEdges());
                if (sourceOutlineList.Count > 1) { return; }
                //var currentSourceOutline = new Polyline(sourceOutlineList[0]).ToNurbsCurve(); // Define outline as a nurbscurve

                // Initialize temporary area
                double[] tempArea = new double[sunVector.Count];

                // Define the output path for areas
                var areaPath = new GH_Path(i);

                // Compute surface area of source outline
                var CurrentSourceArea = AreaMassProperties.Compute(new Polyline(sourceOutlineList[0]).ToNurbsCurve()).Area;

                //-------------------------------------
                // Each wall is tested for intersection
                //------------------------------------- 

                // Test whether the source outline is null
                if (sourceOutlineList[0] == null)
                {
                    // Append 0.0 area array 
                    for (int k = 0; k < sunVector.Count; k++)
                    {
                        areaResult.Append(new GH_Number(0.0), areaPath);
                    }
                    // Append null outlines array 
                    for (int j = 0; j < wallPanel.Count; j++)
                    {
                        var outlinePath = new GH_Path(i, j);
                        for (int k = 0; k < sunVector.Count; k++)
                        {
                            result.Append(null, outlinePath);
                        }
                    }
                }
                else
                {
                    for (int j = 0; j < wallPanel.Count; j++)
                    {
                        // Define current wall panel
                        var currentWPanel = wallPanel[j];
                        if (currentWPanel == null) { return; }

                        // Define output path for outlines
                        var outlinePath = new GH_Path(i, j);

                        // Define panel outline
                        var panelOutlineList = new List<Polyline>(currentWPanel.GetNakedEdges());
                        if (panelOutlineList.Count > 1) { return; }
                        var currentPanelOutline = new Polyline(panelOutlineList[0]).ToNurbsCurve(); // Define outline as a nurbscurve

                        // Define plane of panel
                        var panelPlane = new Plane();
                        Plane.FitPlaneToPoints(panelOutlineList[0], out panelPlane);

                        // Compute surface area of wall panel
                        var CurrentWallArea = AreaMassProperties.Compute(currentPanelOutline).Area;

                        //--------------------------------
                        // Each sunVector is tested for intersection
                        //--------------------------------

                        for (int k = 0; k < sunVector.Count; k++)
                        {
                            // Define current sun vector
                            var currentSunVector = sunVector[k];

                            // Initialize 
                            var currentSourceOutline = new Polyline(sourceOutlineList[0]).ToNurbsCurve(); // Define outline as a nurbscurve

                            // Define current projection
                            var projectionOnWall = new Transform();
                            projectionOnWall = GetObliqueTransformation(panelPlane, currentSunVector);
                            currentSourceOutline.Transform(projectionOnWall);

                            RegionContainment status = Curve.PlanarClosedCurveRelationship(currentSourceOutline, currentPanelOutline, panelPlane, tol);

                            switch (status)
                            {
                                case RegionContainment.Disjoint:
                                    result.Append(null, outlinePath);
                                    areaResult.Append(new GH_Number(0.0), areaPath);
                                    comment.Append(new GH_String("Disjoint"), outlinePath);
                                    break;
                                case RegionContainment.MutualIntersection:
                                    var current_intersection = Curve.CreateBooleanIntersection(currentSourceOutline, currentPanelOutline);
                                    // test needed to determine if there is one or more distinct resulting curves

                                    if (current_intersection.Length == 0)
                                    {
                                        result.Append(null, outlinePath);
                                        areaResult.Append(new GH_Number(0.0), areaPath);
                                        comment.Append(new GH_String("MutualIntersection, line intersection"), outlinePath);
                                    }

                                    else if (current_intersection.Length == 1)
                                    {
                                        result.Append(new GH_Curve(current_intersection[0]), outlinePath);
                                        areaResult.Append(new GH_Number(AreaMassProperties.Compute(current_intersection[0]).Area), areaPath);
                                        comment.Append(new GH_String("MutualIntersection, 1 resulting closed curve"), outlinePath);
                                    }
                                    else
                                    {
                                        for (int m = 0; m < current_intersection.Length; m++)
                                        {
                                            result.Append(new GH_Curve(current_intersection[m]), outlinePath);
                                        }
                                        areaResult.Append(new GH_Number(AreaMassProperties.Compute(current_intersection).Area), areaPath);
                                        comment.Append(new GH_String("MutualIntersection, " + current_intersection.Length.ToString() + " resulting closed curves"), outlinePath);
                                    }
                                    break;
                                case RegionContainment.AInsideB:
                                    if (currentSourceOutline.IsClosed)
                                    {
                                        result.Append(new GH_Curve(currentSourceOutline), outlinePath);
                                        areaResult.Append(new GH_Number(AreaMassProperties.Compute(currentSourceOutline).Area), areaPath);
                                        comment.Append(new GH_String("A Inside B, resulting curve is closed"), outlinePath);
                                    }
                                    else
                                    {
                                        result.Append(new GH_Curve(currentSourceOutline), outlinePath);
                                        areaResult.Append(null, areaPath);
                                        comment.Append(new GH_String("A Inside B,  resulting curve is NOT closed"), outlinePath);
                                    }
                                    break;
                                case RegionContainment.BInsideA:
                                    result.Append(new GH_Curve(currentPanelOutline), outlinePath);
                                    areaResult.Append(new GH_Number(AreaMassProperties.Compute(currentPanelOutline).Area), areaPath);
                                    comment.Append(new GH_String("B Inside A,  resulting curve is  closed"), outlinePath);
                                    break;
                            }

                            // End of sunVector loop
                        }

                        // End of wall panel loop
                    }



                    // End of outline loop
                }







            }


            DA.SetDataTree(0, glareOutline);
            DA.SetDataTree(1, glareAreas);
            DA.SetDataTree(2, comment);
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


        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("123eb198-4a2a-44e1-b760-a798514bea7e"); }
        }
    }
}