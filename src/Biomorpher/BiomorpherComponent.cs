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

        public Grasshopper.GUI.Canvas.GH_Canvas canvas; 
        public bool GO = false;
        private IGH_DataAccess deej;
        private int solveinstanceCounter;

        /// <summary>
        /// Main constructor
        /// </summary>
        public BiomorpherComponent()
            : base("Biomorpher", "Biomorpher", "Interactive Genetic Algorithms for Grasshopper", "Params", "Util")
        {    
            solveinstanceCounter = 0;
            canvas = Instances.ActiveCanvas;
        }

        /// <summary>
        /// Register inputs
        /// </summary>
        /// <param name="pm"></param>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pm)
        {
            pm.AddNumberParameter("Genome", "Genome", "(genotype) Connect slider here (currently only one)", GH_ParamAccess.list);
            pm.AddGeometryParameter("Geometry", "Geometry", "(phenotype) Connect geometry here - currently only meshes", GH_ParamAccess.list);
            //TODO: Measures.. input 'performance measures' 
            //TODO: Labels.. input string for external quantitative measures

            pm[0].WireDisplay = GH_ParamWireDisplay.faint;
            pm[1].WireDisplay = GH_ParamWireDisplay.faint;
            //pm[2].Optional = true;
            //pm[3].Optional = true;
        }

        /// <summary>
        /// Register outputs
        /// </summary>
        /// <param name="pm"></param>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pm)
        {
            pm.AddIntegerParameter("SICount", "SICount", "solve instance counter", GH_ParamAccess.item);
        }

        /// <summary>
        /// Grasshopper solve method
        /// </summary>
        /// <param name="DA"></param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (solveinstanceCounter == 0)
            {
                deej = DA;
            }

            solveinstanceCounter++;
            DA.SetData(0, solveinstanceCounter);
        }

        
        /// <summary>
        /// Gets the current sliders in Input[0]
        /// </summary>
        public void GetSliders(List<Grasshopper.Kernel.Special.GH_NumberSlider> sliders)
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
        /// Sets the current slider values for a geven input chromosome
        /// </summary>
        /// <param name="chromo"></param>
        public void SetSliders(Chromosome chromo, List<Grasshopper.Kernel.Special.GH_NumberSlider> sliders)
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

        /// <summary>
        /// Updates the geometry for an input chromosome
        /// </summary>
        /// <param name="chromo"></param>
        /// <param name="da"></param>
        /// <returns></returns>
        public void GetGeometry(Chromosome chromo)
        {

            // Collect the object at the current instance
            List<object> localObjs = new List<object>();

            // Thank you Dimitrie :)
            foreach (IGH_Param param in Params.Input[1].Sources)
            {
                foreach (Object myObj in param.VolatileData.AllData(true))
                {
                    localObjs.Add(myObj);
                }
            }
            
            // Get only mesh geometry from the object list
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

            // TODO: Get other types of geometry

            // TODO: The allGeometry should not be of type Mesh.
            List<Mesh> allGeometry = new List<Mesh>();
            allGeometry.Add(joinedMesh);

            // Set the phenotype within the chromosome class
            chromo.SetPhenotype(allGeometry);
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
