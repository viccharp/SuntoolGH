using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

using Rhino;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace SunTools.Component
{
    public class SunSifting : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public SunSifting()
          : base("Sift sun orientations", "SunSifting",
              "Select sun orientation producing direct sun radiation on panels ",
              "SunTools", "Sun Vectors")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddVectorParameter("north", "north", "north direction - will be projected to the xy plane", GH_ParamAccess.item);
            pManager.AddNumberParameter("sun azimuth", "sazi","List of sun azimuth angles - north 0° - east 90° - south 180° - west 270°",GH_ParamAccess.list);
            pManager.AddNumberParameter("sun altitude", "salt", "List of sun altitude angles - horizontal 0° - Vertical 90°", GH_ParamAccess.list);
            pManager.AddNumberParameter("associated sun hours", "shours", "List of hours between 0 and 8752(=24*365) associated with a sun orientation", GH_ParamAccess.list);
            pManager.AddMeshParameter("Façade panel to evaluate", "pmesh", "List of façade panels subject of sun radiation assessment", GH_ParamAccess.list);
            
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Resulting sifted sun azimuth angles", "SiftAzi", "Resulting sifted sun azimuth angles", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Resulting sifted sun altitude angles", "SiftAlt", "Resulting sifted sun altitude angles", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Resulting sifted hours", "SiftHours", "", GH_ParamAccess.tree);
            pManager.AddVectorParameter("Resulting sufted sun vectors", "SiftSunVector", "", GH_ParamAccess.tree);

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="da">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess da)
        {
            // Definition of input variables
            var north = new Vector3d();
            var sazi = new List<double>();
            var salt = new List<double>();
            var shours = new List<double>();
            var pmesh = new List<Mesh>();

            //Definition of output variable
            var siftazi = new GH_Structure<GH_Number>();
            var siftalt = new GH_Structure<GH_Number>();
            var sifthours = new GH_Structure<GH_Number>();
            var svector = new GH_Structure<GH_Vector>();



            
            // Populating the list input variables
            if (!da.GetData(0, ref north)) { return; }
            if (!da.GetDataList(1, sazi)) { return; }
            if (!da.GetDataList(2, salt)) { return; }
            if (!da.GetDataList(3, shours)) { return; }
            if (!da.GetDataList(4, pmesh)) { return; }

            // Evaluate if North == null
            if (north == null)
            {
                north = new Vector3d(1.0, 0.0, 0.0);
            }

            //Project north in the XY world and unitize
            if (!north.IsPerpendicularTo(new Vector3d(0.0, 0.0, 1.0)))
            {
                var xyworld = new Plane(new Point3d(0.0, 0.0, 0.0), new Vector3d(0.0, 0.0, 1.0));
                var projXyworld = Transform.PlanarProjection(xyworld);

                north.Transform(projXyworld);
                north.Unitize();
            }
            else
            {
                north.Unitize();
            }

            // Abort the component if retrieval fails or input length are zeros
            if (sazi == null) { return; }
            if (salt == null) { return; }
            if (shours == null) { return; }
            
            if (sazi.Count == 0) { return; }
            if (salt.Count == 0) { return; }
            if (shours.Count == 0) { return; }
            
            // Abort if sazi, salt and shours counts differ
            if (!(sazi.Count==salt.Count || sazi.Count==shours.Count || salt.Count==shours.Count)){ return;}

            // start the analysis
            var scount = sazi.Count;
            var mcount = pmesh.Count;

            //for each input mesh
            for (int i=0; i < mcount; i++)
            {
                GH_Path p1 = new GH_Path(i);
                var currentMesh = pmesh[i];
                var currentCentroid = new Point3d(AreaMassProperties.Compute(currentMesh).Centroid);
                currentMesh.FaceNormals.ComputeFaceNormals();
                var currentNormal = currentMesh.FaceNormals[0];
                currentNormal.Unitize();


                //for each input sun vector
                for (int j = 0; j < scount; j++)
                {
                    GH_Path p2 = new GH_Path(i, j);
                    
                    //convert sun angles to radians 
                    var currentSaziR = RhinoMath.ToRadians(sazi[j]);
                    var currentSaltR = RhinoMath.ToRadians(salt[j]);
                    
                    // convert to clockwise angle values
                    currentSaziR = 2 * Math.PI - currentSaziR;
                    currentSaltR = 2 * Math.PI - currentSaltR;

                    var currentSazi = new GH_Number(RhinoMath.ToDegrees(currentSaziR));
                    var currentSalt = new GH_Number(RhinoMath.ToDegrees(currentSaltR));
                    var currentShour = new GH_Number(shours[j]);


                    //create sun vector
                    var currentSun = new Vector3d(north.X - currentCentroid.X, north.Y - currentCentroid.Y, north.Z - currentCentroid.Z);
                    currentSun.Unitize();
                    //current_sun.Reverse();
                    currentSun.Rotate(currentSaltR, Vector3d.CrossProduct(new Vector3d(0.0, 0.0, 1.0),north));
                    currentSun.Rotate(currentSaziR, new Vector3d(0.0, 0.0, 1.0));
                    //current_sun.Reverse();

                    // Dot product mesh normal current_sun
                    var dsn = Vector3d.Multiply(currentNormal, currentSun);


                    if (dsn > 0)
                    {
                        siftazi.Append(currentSazi,p1);
                        siftalt.Append(currentSalt,p1);
                        sifthours.Append(currentShour, p1);
                        svector.Append(new GH_Vector(currentSun));

                    }




                    
                }
            }

            

            da.SetDataTree(0, siftazi);
            da.SetDataTree(1, siftalt);
            da.SetDataTree(2, sifthours);
            da.SetDataTree(3, svector);


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
            get { return new Guid("{80f38543-4a39-4906-9db9-a1bd827ad45b}"); }
        }
    }
}