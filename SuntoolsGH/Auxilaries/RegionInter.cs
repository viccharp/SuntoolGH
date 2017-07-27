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
              "Victor", "Meshtools")
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
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="da">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess da)
        {
            var curveA = new List<Curve>();
            Curve curveB = new PolylineCurve();
            var intPlane = new Plane();

            double tol = 0.001;

            var res = new List<GH_Curve>();
            var ares = new List<GH_Number>();

            if (!da.GetDataList(0, curveA)) { return; }
            if (!da.GetData(1,ref curveB)) { return; }
            if (!da.GetData(2,ref intPlane)) { return; }
            
            for (int i = 0; i < curveA.Count; i++)
            {
                RegionContainment status = Curve.PlanarClosedCurveRelationship(curveA[i], curveB, intPlane, tol);

                switch (status)
                {
                    case RegionContainment.Disjoint:
                        res.Add(null);
                        ares.Add(new GH_Number(0.0));
                        break;
                    case RegionContainment.MutualIntersection:
                        var currentIntersection = Curve.CreateBooleanIntersection(curveA[i], curveB);
                        // test needed to determine if there is one or more distinct resulting curves

                        switch (currentIntersection.Length)
                        {
                            case 0:
                                res.Add(null);
                                ares.Add(new GH_Number(0.0));
                                break;
                            case 1:
                                res.Add(new GH_Curve(currentIntersection[0]));
                                ares.Add(new GH_Number(AreaMassProperties.Compute(currentIntersection[0]).Area));
                                break;
                            default:
                                for (int j=0;j< currentIntersection.Length;j++)
                                {
                                    res.Add(new GH_Curve(currentIntersection[j]));
                                    ares.Add(new GH_Number(9999999));
                                }
                                break;
                        }
                        break;
                    case RegionContainment.AInsideB:
                        if (curveA[i].IsClosed)
                        {
                            res.Add(new GH_Curve(curveA[i]));

                            ares.Add(new GH_Number(AreaMassProperties.Compute(curveA[i]).Area));
                        }
                        else
                        {
                            res.Add(new GH_Curve(curveA[i]));
                            ares.Add(null);
                        }
                        break;
                    case RegionContainment.BInsideA:
                        res.Add(new GH_Curve(curveB));

                        ares.Add(new GH_Number(AreaMassProperties.Compute(curveB).Area));
                        break;
                }
            }

            da.SetDataList(0, res);
            da.SetDataList(1, ares);

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