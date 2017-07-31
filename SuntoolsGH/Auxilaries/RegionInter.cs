using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using GH_IO.Types;
using Grasshopper.Kernel.Types;

namespace SunTools.Auxilaries
{
    public class RegionInter : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public RegionInter()
          : base("Region Inter", "RInter",
              "Computes the boolean intersection of co - planar curves and the area of the resulting curve",
              "SunTools", "Utilities")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curves A", "A", "First set of regions", GH_ParamAccess.list);
            pManager.AddCurveParameter("Curve B", "B", "Second region item ", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Plane", "P", "Plane of intersection containing both curves", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Resulting intersection", "Result", "Region resulting from the Intersection", GH_ParamAccess.list);
            pManager.AddNumberParameter("Area of resulting region", "Area_result", "Area of the region resulting from Intersection", GH_ParamAccess.list);
            pManager.AddTextParameter("Comment on intersection type", "outCom",
                "type of intersection between the two curves: Disjoint, MutualIntersection(and number of resulting disjoint closed curves), AinsideB, BinsideA",GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var curveA = new List<Curve>();
            Curve curveB = new PolylineCurve();
            var Int_plane = new Plane();

            double tol = 0.001;

            var res = new List<GH_Curve>();
            var Ares = new List<GH_Number>();
            var comment=new List<string>();

            if (!DA.GetDataList(0, curveA)) { return; }
            if (!DA.GetData(1,ref curveB)) { return; }
            if (!DA.GetData(2,ref Int_plane)) { return; }
            
            for (int i = 0; i < curveA.Count; i++)
            {
                RegionContainment status = Curve.PlanarClosedCurveRelationship(curveA[i], curveB, Int_plane, tol);

                switch (status)
                {
                    case RegionContainment.Disjoint:
                        res.Add(null);
                        Ares.Add(new GH_Number(0.0));
                        comment.Add("Disjoint");
                        break;
                    case RegionContainment.MutualIntersection:
                        var current_intersection = Curve.CreateBooleanIntersection(curveA[i], curveB);
                        // test needed to determine if there is one or more distinct resulting curves

                        if (current_intersection.Length == 0)
                        {
                            res.Add(null);
                            Ares.Add(new GH_Number(0.0));
                            comment.Add("MutualIntersection, line intersection");
                        }
                    
                        else if (current_intersection.Length == 1)
                        {
                            res.Add(new GH_Curve(current_intersection[0]));
                            Ares.Add(new GH_Number(AreaMassProperties.Compute(current_intersection[0]).Area));
                            comment.Add("MutualIntersection, 1 resulting closed curve");
                        }
                        else
                        {
                            for (int j=0;j< current_intersection.Length;j++)
                            {
                                res.Add(new GH_Curve(current_intersection[j]));
                            }
                            Ares.Add(new GH_Number(AreaMassProperties.Compute(current_intersection).Area));
                            comment.Add("MutualIntersection, "+current_intersection.Length.ToString()+" resulting closed curves");
                        }
                        break;
                    case RegionContainment.AInsideB:
                        if (curveA[i].IsClosed)
                        {
                            res.Add(new GH_Curve(curveA[i]));

                            Ares.Add(new GH_Number(AreaMassProperties.Compute(curveA[i]).Area));
                            comment.Add("A Inside B, resulting curve is closed");
                        }
                        else
                        {
                            res.Add(new GH_Curve(curveA[i]));
                            Ares.Add(null);
                            comment.Add("A Inside B,  resulting curve is NOT closed");
                        }
                        break;
                    case RegionContainment.BInsideA:
                        res.Add(new GH_Curve(curveB));

                        Ares.Add(new GH_Number(AreaMassProperties.Compute(curveB).Area));
                        comment.Add("B Inside A,  resulting curve is  closed");
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
            get { return new Guid("b469f98c-6a95-423b-97fb-328ddbf855c1"); }
        }
    }
}