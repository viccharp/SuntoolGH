using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Data;

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
            pManager.AddMeshParameter("Shade surface", "mshade","A list of shade meshes to be evaluated for direct shade coverage",GH_ParamAccess.list);
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

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // input variables
            var panel = new Mesh();
            var mshade = new List<Mesh>();
            var vsun = new List<Vector3d>();
            var run = new bool();

            DA.GetData(0, ref panel);
            DA.GetDataList(1,  mshade);
            DA.GetDataList(2, vsun);
            DA.GetData(3, ref run);

            if (run==false) { return; }

            
            //temporary output variables
            var res = new GH_Structure<GH_Curve>();
            var Ares = new GH_Structure<GH_Number>();
            
             
            var panel_outline_list= new List<Polyline>(panel.GetNakedEdges());
            Polyline panel_outline;

            if (panel_outline_list.Count > 1) { return; }
            else
            {
                panel_outline = panel_outline_list[0];
            }

            Plane.FitPlaneToPoints(panel_outline, out Plane panel_plane);

            //create transformation for reverse projection of the difference of outline 
            //projectback = Transform.PlanarProjection(panel_plane);


            for (int i = 0; i < mshade.Count; i++)
            {
                var current_shade = mshade[i];
                var current_shade_outline= new Polyline((new List<Polyline>(current_shade.GetNakedEdges()))[0]);
                var p1 = new GH_Path(i);


                for (int j = 0; j < vsun.Count; j++)
                {
                    var currentSunPlane = new Plane(panel_outline[0],vsun[j]);
                    var projectsun = Transform.PlanarProjection(currentSunPlane);
                    //projectsun.TryGetInverse(out projectback);
                    var projectback = GetObliqueTransformation(panel_plane, vsun[j]);


                    new Polyline(panel_outline).Transform(projectsun);
                    new Polyline(current_shade_outline).Transform(projectsun);

                    var temp_current_diff = Curve.CreateBooleanDifference(new Polyline(panel_outline).ToNurbsCurve(), new Polyline(current_shade_outline).ToNurbsCurve());

                    if (temp_current_diff.Length == 0)
                    {
                        
                        res.Append(null,p1);
                        Ares.Append(null,p1);

                    }
                    else
                    {
                        if (temp_current_diff[0] != null)
                        {
                            temp_current_diff[0].Transform(projectback);

                            res.Append(data: new GH_Curve(temp_current_diff[0]), path: p1);
                            Ares.Append(data: new GH_Number(AreaMassProperties.Compute(temp_current_diff[0]).Area), path: p1);
                        }
                        else
                        {
                            res.Append(null, p1);
                            Ares.Append(null, p1);
                        }
                    }




                }


            }

            var shade_outline = res;
            var shade_areas = Ares;

            DA.SetDataTree(0, shade_outline);
            DA.SetDataTree(1, shade_areas);

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
        public Transform GetObliqueTransformation(Plane Pln, Vector3d V)
        {

            var oblique = new Transform(1);
            var eq = Pln.GetPlaneEquation();
            var a = eq[0];
            var b = eq[1];
            var c = eq[2];
            var d = eq[3];
            var dx = V.X;
            var dy = V.Y;
            var dz = V.Z;
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




        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{46928a0e-c0fe-4900-a8bb-b0b449c4b513}"); }
        }
    }
}