using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

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
            pManager.AddMeshParameter("Source Outline", "sourceOL", "A list of closed curves as direct light sources", GH_ParamAccess.list);
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
            pManager.AddTextParameter("Comment on operation type", "outCom", "", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // input variables
            var wallPanel = new Mesh();
            var source = new List<Mesh>(); //this is the exposed surface on the window (source of glare)
            var sunVector = new List<Vector3d>();
            var run = new bool();

            DA.GetData(0, ref wallPanel);
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

            // Define wall plane
            var panelOutlineList = new List<Polyline>(wallPanel.GetNakedEdges());
            if (panelOutlineList.Count > 1) { return; }

            //// Define panel outline as a nurbscurve 
            //var panelOutline = new Polyline(panelOutlineList[0]).ToNurbsCurve();

            var panelPlane = new Plane();
            Plane.FitPlaneToPoints(panelOutlineList[0], out panelPlane);



            for (int i = 0; i < source.Count; i++)
            {
                // Define source Path for output 
                var areaPath=new GH_Path(i);

                for (int j = 0; j < sunVector.Count; j++)
                {
                    // Define projection transform
                    var currentProjTrans = GetObliqueTransformation(panelPlane, sunVector[j]);

                    // Define projected source mesh
                    var currentProjSource = source[i].Transform(currentProjTrans);
                    
                    // Define outlines of bounding boxes for the wall panel and the projected source

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

            var oblique = new Transform(1);
            var eq = pln.GetPlaneEquation();
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
            get { return new Guid("74432574-ca9a-419b-a67d-5915e367bd42"); }
        }
    }
}