using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace SunTools.Component
{
    public class ShadeAssess : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public ShadeAssess()
            : base("Evaluate shade on façade panel", "ShadeAssess",
                "Projection of Shading surface on ",
                "SunTools", "Shading Tools")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Façade Panel", "mfpanel", "A list of planar panels representing part of a façade", GH_ParamAccess.item);
            pManager.AddMeshParameter("Shade surface", "mshade", "A list of shade meshes to be evaluated for direct shade coverage", GH_ParamAccess.list);
            pManager.AddVectorParameter("Projection direction vector", "dir", "The vectors for shading evaluation", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Launch the analysis", "start", "If bool is True: analysis is running, if bool is False: analysis stopped", GH_ParamAccess.item);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Outline of shade", "Outline_exposed", "Tree of shade oultine on the façade panel", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Area of exposed", "Area_exposed", "Area of the exposed part of the façade panel", GH_ParamAccess.tree);
            pManager.AddTextParameter("Comment on operation type", "outCom",
                "type of difference between the two curves: Disjoint, MutualIntersection(and number of resulting disjoint closed curves), AinsideB, BinsideA", GH_ParamAccess.tree);

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="da">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess da)
        {
            // input variables
            var panel = new Mesh();
            var mshade = new List<Mesh>();
            var vsun = new List<Vector3d>();
            var run = new bool();

            da.GetData(0, ref panel);
            da.GetDataList(1, mshade);
            da.GetDataList(2, vsun);
            da.GetData(3, ref run);

            if (run == false) { return; }



            //output variables
            var shadeOutline = new GH_Structure<GH_Curve>();
            var shadeAreas = new GH_Structure<GH_Number>();

            //temporary output variables
            var res = new GH_Structure<GH_Curve>();
            var ares = new GH_Structure<GH_Number>();
            var comment = new GH_Structure<GH_String>();


            var panelOutlineList = new List<Polyline>(panel.GetNakedEdges());
            if (panelOutlineList.Count > 1) { return; }

            // Define panel outline as a nurbscurve 
            var panelOutline = new Polyline(panelOutlineList[0]).ToNurbsCurve();

            var panelPlane = new Plane();
            Plane.FitPlaneToPoints(panelOutlineList[0], out panelPlane);

            
            double tol = 0.001;

            
            // Surface Area of window panel
            var winArea= AreaMassProperties.Compute(panelOutline).Area;


            for (int i = 0; i < mshade.Count; i++)
            {
                Mesh currentShade = mshade[i];
                var currentShadeOutline = new Polyline((new List<Polyline>(currentShade.GetNakedEdges()))[0]);
                var p1 = new GH_Path(i);
                
                for (int j = 0; j < vsun.Count; j++)
                {
                    // Create projection transform of shade to panel plane along the sun vector direction
                    var projectToPp = new Transform();
                    projectToPp = GetObliqueTransformation(panelPlane, vsun[j]);
                    
                    // Project shade outline to panel plane along the sun vector direction 
                    var currentShadeSunProj = new Polyline(currentShadeOutline).ToNurbsCurve();
                    currentShadeSunProj.Transform(projectToPp);

                    // Determine the difference of the panel outline - the shade outline
                    RegionContainment status = Curve.PlanarClosedCurveRelationship(panelOutline, currentShadeSunProj, panelPlane, tol);

                    switch (status)
                    {
                        case RegionContainment.Disjoint:
                            res.Append(new GH_Curve(panelOutline), p1);
                            ares.Append(new GH_Number(winArea), p1);
                            comment.Append(new GH_String("Disjoint, case 1"),p1);
                            break;
                        case RegionContainment.MutualIntersection:
                            Curve[] currentDifference =Curve.CreateBooleanDifference(panelOutline, currentShadeSunProj);
                            if (currentDifference == null) throw new ArgumentNullException(nameof(currentDifference));
                            // test needed to determine if there is one or more distinct resulting curves

                            if (currentDifference.Length == 0)
                            {
                                var areaInter = AreaMassProperties.Compute(Curve.CreateBooleanIntersection(panelOutline, currentShadeSunProj)).Area;
                                if (areaInter < tol * AreaMassProperties.Compute(panelOutline).Area)
                                {
                                    res.Append(new GH_Curve(panelOutline), p1);
                                    ares.Append(new GH_Number(winArea), p1);
                                    comment.Append(new GH_String("MutualIntersection, line/point intersection, case 2a_a"), p1);
                                }
                                else
                                {
                                    res.Append(new GH_Curve(currentShadeSunProj), p1);
                                    res.Append(new GH_Curve(panelOutline), p1);
                                    ares.Append(new GH_Number(winArea - AreaMassProperties.Compute(currentShadeSunProj).Area), p1);
                                    comment.Append(new GH_String("MutualIntersection, line/point intersection, case 2a_b"), p1);
                                }
                                
                            }

                            else if (currentDifference.Length == 1)
                            {
                                currentDifference[0].Transform(projectToPp);
                                res.Append(new GH_Curve(currentDifference[0]),p1);
                                ares.Append(new GH_Number(AreaMassProperties.Compute(currentDifference[0]).Area), p1);
                                comment.Append(new GH_String("MutualIntersection, 1 resulting closed curve, case 2b"), p1);
                            }
                            else
                            {
                                for (int k = 0; k < currentDifference.Length; k++)
                                {
                                    res.Append(new GH_Curve(currentDifference[k]), p1);
                                }
                                ares.Append(new GH_Number(AreaMassProperties.Compute(currentDifference).Area), p1);
                                comment.Append(new GH_String("MutualIntersection, " + currentDifference.Length.ToString() + " resulting closed curves, case 2c"), p1);
                            }
                            break;
                        case RegionContainment.AInsideB:
                            if (panelOutline.IsClosed)
                            {
                                res.Append(new GH_Curve(panelOutline), p1); 
                                ares.Append(new GH_Number(winArea), p1);
                                comment.Append(new GH_String("A Inside B, resulting curve is closed, case 3a"),p1);
                            }
                            else
                            {
                                res.Append(new GH_Curve(panelOutline), p1);
                                ares.Append(null, p1);
                                comment.Append(new GH_String("A Inside B,  resulting curve is NOT closed, case 3b"),p1);
                            }
                            break;
                        case RegionContainment.BInsideA:
                            res.Append(new GH_Curve(currentShadeSunProj), p1);
                            res.Append(new GH_Curve(panelOutline), p1);
                            ares.Append(new GH_Number(winArea - AreaMassProperties.Compute(currentShadeSunProj).Area), p1);
                            comment.Append(new GH_String("B Inside A,  resulting curve is  closed, case 4"),p1);
                            break;
                    }
                }
            }

            shadeOutline = res;
            shadeAreas = ares;

            da.SetDataTree(0, shadeOutline);
            da.SetDataTree(1, shadeAreas);
            da.SetDataTree(2, comment);

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
            get { return new Guid("{46928a0e-c0fe-4900-a8bb-b0b449c4b513}"); }
        }
    }
}