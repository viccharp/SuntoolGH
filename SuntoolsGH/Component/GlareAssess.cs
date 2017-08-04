using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace SunTools.Component
{
    public class GlareAssess : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public GlareAssess()
          : base("Glare on wall panels", "GlareAssess",
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
            get { return new Guid("123eb198-4a2a-44e1-b760-a798514bea7e"); }
        }
    }
}