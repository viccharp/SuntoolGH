using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace SunTools.Auxilaries
{
    public class RegionDiff : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public RegionDiff()
          : base("Boolean difference of co-planar curves", "RegionDiff",
              "Computes the boolean difference of co-planar curves and the area of the resulting curve",
              "Victor", "Meshtools")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curve 1", "c1", "Curve to substract from", GH_ParamAccess.list);
            pManager.AddCurveParameter("Curve 2", "c2", "Curve to substract with", GH_ParamAccess.list);
            //pManager.AddPlaneParameter("Analysis plane", "p", "plane in which the intersection takes place", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("difference region c1 minus c2", "res", "Region resulting from the difference of C1 minus c2", GH_ParamAccess.list);
            pManager.AddNumberParameter("area of resulting region", "Ares", "Area of the region resulting from difference of c1 minus c2", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var c1 = new List<Curve>();
            var c2 = new List<Curve>();
            //var planes = new GH_Structure<GH_Plane>();


            var res = new List<GH_Curve>();
            var Ares = new List<GH_Number>();

            if (!DA.GetDataList(0, c1)) { return; }
            if (!DA.GetDataList(1, c2)) { return; }

            if (!(c1.Count == c2.Count))
            {
                return;
            }

            for (int i = 0; i < c1.Count; i++)
            {
                var temp_current_diff= Curve.CreateBooleanDifference(c1[i], c2[i]);

                if (temp_current_diff.Length == 0)
                {
                    res.Add(null);
                    Ares.Add(null);
                    
                }
                else
                {
                    var current_diff = temp_current_diff[0];

                    res.Add(new GH_Curve(current_diff));
                    Ares.Add(new GH_Number(AreaMassProperties.Compute(current_diff).Area));
                }
                
                


            }


            //var res = new GH_Structure<GH_Curve>();
            //var Ares = new GH_Structure<GH_Number>();

            //if (!DA.GetDataList(0,  master)) { return; }
            //if (!DA.GetDataTree(1, out substractor)) { return; }
            ////if (!DA.GetDataTree(2, out planes)) { return; }

            //for (int i = 0; i < substractor.Branches.Count; i++)
            //{
            //    var current_subtractors = substractor.Branches[i];
            //    var ncurve = current_subtractors.Count;

            //    if (!(ncurve == master.Count))
            //    {
            //        return;
            //    }

            //    for (int j = 0; j < ncurve; j++)
            //    {
            //        var p2 = new GH_Path(i,j);

            //        var current_curve = new PolylineCurve();
            //        current_subtractors[j].CastTo<PolylineCurve>(out current_curve);

            //        var temp_current_res = Curve.CreateBooleanDifference(master[j], current_curve);

            //        if ((temp_current_res.Length == 0))
            //        {
            //         return;
            //        }

            //        var current_res =temp_current_res[0];
            //        var current_area = new GH_Number(AreaMassProperties.Compute(current_res).Area);

            //        var current_res_gh = new GH_Curve(current_res);
            //        //var current_area_gh = new GH_Number((current_area[0]));

            //        //for (int k = 0; k <current_res.Length;k++)
            //        //{
            //        //    current_res_gh.Add(new GH_Curve(current_res[k]));
            //        //    var current_area = new GH_Number(AreaMassProperties.Compute(current_res[k]).Area);
            //        //    current_area_gh.Add(current_area);
            //        //}

            //        res.Append(current_res_gh, p2);
            //        Ares.Append(current_area,p2);


            //    }


            //}

            DA.SetDataList(0, res);
            DA.SetDataList(1, Ares);
            

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