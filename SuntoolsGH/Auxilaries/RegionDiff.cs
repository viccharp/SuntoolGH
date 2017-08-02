using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace SunTools.Component
{
    public class RegionDiff : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public RegionDiff()
          : base("Boolean difference of co-planar curves", "RegionDiff",
              "Computes the boolean difference of co-planar curves and the area of the resulting curve",
              "SunTools","Utilities")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curve 1", "c1", "Curve to substract from", GH_ParamAccess.list);
            pManager.AddCurveParameter("Curve 2", "c2", "Curve to substract with", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Plane", "P", "Plane of intersection containing both curves", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("difference region c1 minus c2", "res", "Region resulting from the difference of C1 minus c2", GH_ParamAccess.list);
            pManager.AddNumberParameter("area of resulting region", "Ares", "Area of the region resulting from difference of c1 minus c2", GH_ParamAccess.list);
            pManager.AddTextParameter("Comment on difference type", "outCom",
                "type of difference between the two curves: Disjoint, MutualIntersection(and number of resulting disjoint closed curves), AinsideB, BinsideA", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var c1 = new List<Curve>();
            Curve c2 = new PolylineCurve();
            var intPlane = new Plane();
            
            const double tol = 0.001;

            var res = new List<GH_Curve>();
            var Ares = new List<GH_Number>();
            var comment = new List<string>();

            if (!DA.GetDataList(0, c1)) { return; }
            if (!DA.GetData(1, ref c2)) { return; }
            if (!DA.GetData(2, ref intPlane)) { return; }


            for (int i = 0; i < c1.Count; i++)
            {
                RegionContainment status = Curve.PlanarClosedCurveRelationship(c1[i], c2, intPlane, tol);

                switch (status)
                {
                    case RegionContainment.Disjoint:
                        res.Add(new GH_Curve(c1[i]));
                        Ares.Add(new GH_Number(AreaMassProperties.Compute(c1[i]).Area));
                        comment.Add("Disjoint, case 1");
                        break;
                    case RegionContainment.MutualIntersection:
                        var currentDifference = Curve.CreateBooleanDifference(c1[i], c2);
                        // test needed to determine if there is one or more distinct resulting curves

                        if (currentDifference.Length == 0)
                        {
                            var areaInter = AreaMassProperties.Compute(Curve.CreateBooleanIntersection(c1[i], c2)).Area;
                            if (areaInter < tol * AreaMassProperties.Compute(c1[i]).Area)
                            {
                                res.Add(new GH_Curve(c1[i]));
                                Ares.Add(new GH_Number(AreaMassProperties.Compute(c1[i]).Area));
                                comment.Add("MutualIntersection, line/point intersection, case 2a_a");
                            }
                            else 
                            {
                                res.Add(new GH_Curve(c2));
                                res.Add(new GH_Curve(c1[i]));
                                Ares.Add(new GH_Number(AreaMassProperties.Compute(c1[i]).Area - AreaMassProperties.Compute(c2).Area));
                                comment.Add("MutualIntersection, line/point intersection, case 2a_b");
                            }
                            
                        }

                        else if (currentDifference.Length == 1)
                        {
                            res.Add(new GH_Curve(currentDifference[0]));
                            Ares.Add(new GH_Number(AreaMassProperties.Compute(currentDifference[0]).Area));
                            comment.Add("MutualIntersection, 1 resulting closed curve, case 2b");
                        }
                        else
                        {
                            for (int j = 0; j < currentDifference.Length; j++)
                            {
                                res.Add(new GH_Curve(currentDifference[j]));
                            }
                            Ares.Add(new GH_Number(AreaMassProperties.Compute(currentDifference).Area));
                            comment.Add("MutualIntersection, " + currentDifference.Length.ToString() + " resulting closed curves, case 2c");
                        }
                        break;
                    case RegionContainment.AInsideB:
                        if (c1[i].IsClosed)
                        {
                            res.Add(new GH_Curve(c1[i]));
                            Ares.Add(new GH_Number(AreaMassProperties.Compute(c1[i]).Area));
                            comment.Add("A Inside B, resulting curve is closed, case 3a");
                        }
                        else
                        {
                            res.Add(new GH_Curve(c1[i]));
                            Ares.Add(null);
                            comment.Add("A Inside B,  resulting curve is NOT closed, case 3b");
                        }
                        break;
                    case RegionContainment.BInsideA:
                        res.Add(new GH_Curve(c2));
                        res.Add(new GH_Curve(c1[i]));

                        Ares.Add(new GH_Number(AreaMassProperties.Compute(c1[i]).Area-AreaMassProperties.Compute(c2).Area));
                        comment.Add("B Inside A,  resulting curve is  closed, case 4");
                        break;
                }
            }





            DA.SetDataList(0, res);
            DA.SetDataList(1, Ares);
            DA.SetDataList(2, comment);


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

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{a5f02b6a-d09e-4f83-8805-e44dec7373e1}"); }
        }
    }
}