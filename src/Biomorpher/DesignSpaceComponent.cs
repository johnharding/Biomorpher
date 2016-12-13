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

namespace Biomorpher
{
    public class DesignSpaceComponent : GH_Component
    {

        private DesignSpaceWindow myMainWindow;
        public bool GO = false;
        private int counter;
        private int popSize;
        private List<Grasshopper.Kernel.Special.GH_NumberSlider> sliders = new List<Grasshopper.Kernel.Special.GH_NumberSlider>();
        public List<double> sliderValues = new List<double>();
        private List<object> persGeo = new List<object>();

        /// <summary>
        /// Main constructor
        /// </summary>
        public DesignSpaceComponent()
            : base("Biomorpher", "DS", "Displays multiple parameter instances in one place", "Extra", "Rosebud")
        {
        }

        /// <summary>
        /// Register component inputs
        /// </summary>
        /// <param name="pm"></param>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pm)
        {
            pm.AddNumberParameter("Sliders", "S", "(genotype) Connect slider here (currently only one)", GH_ParamAccess.list);
            //pm.AddGeometryParameter("Volatile Geometry", "vG", "(phenotype) Connect geometry that is dependent on sliders here", GH_ParamAccess.item);
            pm.AddGeometryParameter("Geometry", "G", "(phenotype) Connect geometry here - currently only meshes", GH_ParamAccess.list);
            pm.AddIntegerParameter("PopSize", "P", "Number of instances to display e.g. 12 = 4x3 viewports", GH_ParamAccess.item, 12);

            pm[0].WireDisplay = GH_ParamWireDisplay.faint;
            pm[1].WireDisplay = GH_ParamWireDisplay.faint;
            //pm[1].Optional = true;

        }

        /// <summary>
        /// Register component outputs
        /// </summary>
        /// <param name="pm"></param>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pm)
        {
            pm.AddNumberParameter("ChosenOnes", "Ch", "Each list contains a collection of selected parameters", GH_ParamAccess.tree);
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
                persGeo.Clear();
                sliderValues.Clear();

                DA.GetData("PopSize", ref popSize);

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

                    persGeo.Add(joinedMesh);
                }


                // If we reach a limit, then stop and launch the window
                if (counter == sliderValues.Count)
                {

                    // Instantiate the window and export the geometry to WPF3D
                    myMainWindow = new DesignSpaceWindow(GetPersMeshList());
                    myMainWindow.Show();

                    GO = false;

                    // Expire this component
                    this.ExpireSolution(true);
                }

                // NOW iterate the master counter
                counter++;

            }

            // We need some interaction with the form before sending out the chosen phenotypes.
            DA.SetData(0, 444);

        }

        /// <summary>
        /// Returns persisent meshes
        /// </summary>
        /// <returns></returns>
        public List<Mesh> GetPersMeshList()
        {
            List<Mesh> myMeshes = new List<Mesh>();
            foreach (object myObject in persGeo)
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
            get { return new Guid("73F1D5F1-7208-4501-8C8C-66ED25BA5A1D"); }
        }

        public override void CreateAttributes()
        {
            m_attributes = new DesignSpaceAttributes(this);
        }


        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.primary;
            }
        }

        protected override Bitmap Icon
        {
            get
            {
                return Properties.Resources.DoubleClickIcon;
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
