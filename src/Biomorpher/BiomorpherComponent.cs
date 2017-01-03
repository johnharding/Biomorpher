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
        private List<Grasshopper.Kernel.Special.GH_NumberSlider> sliders = new List<Grasshopper.Kernel.Special.GH_NumberSlider>();
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
                sliders.Clear();
                inputGeometry.Clear();

                // Collect the sliders up
                GetSliders();

            }

            else
            {
                // Instantiate the window and export the geometry to WPF3D
                myMainWindow = new BiomorpherWindow();
                myMainWindow.Show();
                GO = false;

                // Expire this component
                this.ExpireSolution(true);
            }
        }


        /// <summary>
        /// Gets the current sliders in Input[0]
        /// </summary>
        public void GetSliders()
        {
            foreach (IGH_Param param in this.Params.Input[0].Sources)
            {
                Grasshopper.Kernel.Special.GH_NumberSlider slider = param as Grasshopper.Kernel.Special.GH_NumberSlider;
                if (slider != null)
                {
                    sliders.Add(slider);
                }
            }
        }

        /// <summary>
        /// Clear the list of sliders
        /// </summary>
        public void ClearSliders()
        {
            sliders.Clear();
        }

        /// <summary>
        /// Sets the current slider values based on the chomrosome
        /// </summary>
        /// <param name="chromo"></param>
        public void SetSliders(Chromosome chromo)
        {
            double[] genes = chromo.GetGenes();
 
            for (int i = 0; i < sliders.Count; i++)
            {
                double min = (double)sliders[i].Slider.Minimum;
                double max = (double)sliders[i].Slider.Maximum;
                double range = max - min;

                sliders[i].Slider.Value = (decimal)(genes[i] * range + min);
            }
        }


        public List<Mesh> GetGeometry(Chromosome chromo, IGH_DataAccess da)
        {
            //TODO: Copy the geometry over to the input chromosome, rather than call is when the window is launched

            // Collect the object at the current instance
            List<object> localObjs = new List<object>();
            da.GetDataList("Geometry", localObjs);

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
