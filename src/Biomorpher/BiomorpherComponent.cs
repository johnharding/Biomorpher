using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino;
using System.Windows.Forms;
using System.Drawing;
using Rhino.Geometry;
using Biomorpher.IGA;

namespace Biomorpher
{
    public class BiomorpherComponent : GH_Component
    {

        private BiomorpherWindow myMainWindow;
        public bool GO = false;
        private int counter;
        private int popSize;
        private List<Grasshopper.Kernel.Special.GH_NumberSlider> sliders = new List<Grasshopper.Kernel.Special.GH_NumberSlider>();

        // Lists containing initial slider values and geometry
        public List<double> sliderValues = new List<double>();
        private List<object> inputGeometry = new List<object>();

        /// <summary>
        /// Main constructor
        /// </summary>
        public BiomorpherComponent()
            : base("Biomorpher", "Biomorpher", "Interactive Genetic Algorithms for Grasshopper", "Params", "Util")
        {    
        }

        /// <summary>
        /// Register inputs
        /// </summary>
        /// <param name="pm"></param>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pm)
        {
            pm.AddNumberParameter("Genome", "Genome", "(genotype) Connect slider here (currently only one)", GH_ParamAccess.list);
            pm.AddGeometryParameter("Geometry", "Geometry", "(phenotype) Connect geometry here - currently only meshes", GH_ParamAccess.list);

            pm[0].WireDisplay = GH_ParamWireDisplay.faint;
            pm[1].WireDisplay = GH_ParamWireDisplay.faint;
            //pm[1].Optional = true;

        }

        /// <summary>
        /// Register outputs
        /// </summary>
        /// <param name="pm"></param>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pm)
        {
        }

        /// <summary>
        /// Grasshopper solve method
        /// </summary>
        /// <param name="DA"></param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // If we are currently static, then reset things and collect sliders
            if (!GO)
            {
                // Spring clean
                counter = 0;
                sliders.Clear();
                inputGeometry.Clear();
                sliderValues.Clear();

                // TODO: This should be based on number of chromosomes
                popSize = 12;

                // Collect the sliders up (just one at the moment)
                foreach (IGH_Param param in this.Params.Input[0].Sources)
                {
                    Grasshopper.Kernel.Special.GH_NumberSlider slider = param as Grasshopper.Kernel.Special.GH_NumberSlider;
                    if (slider != null)
                    {
                        sliders.Add(slider);
                    }
                }


                // Now set the value list
                // TODO: Replace with a tree, not just the first slider!
                // Thanks to Dimitrie A. Stefanescu for making Speckle open which has helped greatly here.

                for (int i = 0; i < sliders.Count; i++)
                {
                    double min = (double)sliders[i].Slider.Minimum;
                    double max = (double)sliders[i].Slider.Maximum;

                    // Note we use divisions-1 because we have inclusive slider bounds
                    double increment = (max - min) / ((double)popSize - 1);

                    for (int j = 0; j < popSize; j++)
                        sliderValues.Add(j * increment + min);
                }
            }

            // So if GO = true...
            else
            {
                // Get the slider values.
                // TODO: Include more than one slider.
                if (counter < popSize)
                {
                    //for (int i = 0; i < sliders.Count; i++)
                    sliders[0].Slider.Value = (decimal)sliderValues[counter];
                }


                // First things first...
                // We have to do the else stuff AFTER the slider has moved and the component is expired (tricky).
                if (counter == 0)
                {
                }
                else
                {
                    // Collect the object at the current instance
                    List<object> localObjs = new List<object>();
                    DA.GetDataList("Geometry", localObjs);

                    // Currently we only take meshes
                    Mesh joinedMesh = new Mesh();

                    for (int i = 0; i < localObjs.Count; i++)
                    {
                        if (localObjs[i] is GH_Mesh)
                        {
                            GH_Mesh myGHMesh = new GH_Mesh();
                            myGHMesh = (GH_Mesh)localObjs[i];
                            Mesh myLocalMesh = new Mesh();
                            GH_Convert.ToMesh(myGHMesh, ref myLocalMesh, GH_Conversion.Primary);
                            myLocalMesh.Faces.ConvertQuadsToTriangles();
                            joinedMesh.Append(myLocalMesh);
                        }
                    }

                    inputGeometry.Add(joinedMesh);
                }


                // If we reach a limit, then stop and launch the window
                if (counter == sliderValues.Count)
                {

                    // Instantiate the window and export the geometry to WPF3D
                    myMainWindow = new BiomorpherWindow(GetMeshList());
                    myMainWindow.Show();

                    GO = false;

                    // Expire this component
                    this.ExpireSolution(true);
                }

                // NOW iterate the master counter
                counter++;

            }


        }

        /// <summary>
        /// Returns meshes for this instance
        /// </summary>
        /// <returns></returns>
        public List<Mesh> GetMeshList()
        {
            List<Mesh> myMeshes = new List<Mesh>();
            foreach (object myObject in inputGeometry)
            {
                if (myObject is Mesh)
                {
                    Mesh myLocalMesh = (Mesh)myObject;
                    myMeshes.Add(myLocalMesh);
                }
            }

            return myMeshes;
        }


        public override Guid ComponentGuid
        {
            get { return new Guid("87264CC5-8461-4003-8FF7-7584B13BAF06"); }
        }

        public override void CreateAttributes()
        {
            m_attributes = new BiomorpherAttributes(this);
        }


        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.senary;
            }
        }

        protected override Bitmap Icon
        {
            get
            {
                return Properties.Resources.BiomorpherIcon_24;
            }
        }

        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_AppendItem(menu, @"github source", gotoGithub);
            //Menu_AppendItem(menu, @"gh group page", gotoGrasshopperGroup);

        }

        private void gotoGithub(Object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(@"https://github.com/johnharding/Biomorpher");
        }

        //TODO: send to grasshopper group
        //private void gotoGrasshopperGroup(Object sender, EventArgs e)
        //{
        //System.Diagnostics.Process.Start(@"http://www.????");
        //}
    }
}
