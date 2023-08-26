using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino;
using System.Drawing;
using Rhino.Geometry;
using Biomorpher.IGA;
using Grasshopper.Kernel.Special;
using GalapagosComponents;
using Grasshopper.Kernel.Data;
using System.Windows;

namespace Biomorpher
{
    /// <summary>
    /// The Grasshopper component
    /// </summary>
    public class BiomorpherTrigger: GH_Component
    {
        public Grasshopper.GUI.Canvas.GH_Canvas canvas;
        BiomorpherComponent BioComp;
        List<int> generation;

        /// <summary>
        /// Main constructor
        /// </summary>
        public BiomorpherTrigger()
            : base("BiomorpherTrigger", "BiomorpherTrigger", "Uses Biomorpher data to display paramter states", "Params", "Util")
        {
            canvas = Instances.ActiveCanvas;
            this.IconDisplayMode = GH_IconDisplayMode.icon;
            generation = new List<int>();
        }

        /// <summary>
        /// Register component inputs
        /// </summary>
        /// <param name="pm"></param>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pm)
        {
            pm.AddBooleanParameter("EvolveTrigger", "EvolveTrigger", "(Optional) Triggers the next epoch via the component itself", GH_ParamAccess.item, false);
            pm.AddBooleanParameter("RestartTrigger", "RestartTrigger", "(Optional) Restarts with a new random population of designs", GH_ParamAccess.item, false);

            pm[0].Optional = true;
            pm[1].Optional = true;
        }
        
        /// <summary>
        /// Register component outputs
        /// </summary>
        /// <param name="pm"></param>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pm)
        {
            //pm.AddTextParameter("outstring", "o", "sdfds", GH_ParamAccess.list);
        }

        /// <summary>
        /// Grasshopper solveinstance
        /// </summary>
        /// <param name="DA"></param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool eTrigger = false;
            bool rTrigger = false;

            DA.GetData<bool>("EvolveTrigger", ref eTrigger);
            DA.GetData<bool>("RestartTrigger", ref rTrigger);

            //OnPingDocument().FindObject<GH_Component>()

            List<IGH_ActiveObject> canvasObject = canvas.Document.ActiveObjects();

            // Check for Embryo Components on the canvas
            for (int i = 0; i < canvasObject.Count; i++)
            {
                
                string george = canvasObject[i].ComponentGuid.ToString();

                if (george == "87264cc5-8461-4003-8ff7-7584b13baf06")
                {
                    System.Console.Beep(10000, 100);
                    BioComp = (BiomorpherComponent)canvasObject[i];

                    //willingOutput.Add((IGH_Param)willingThing.Params.Input[0].Sources[n]);

                }
            }


            int currentGeneration = BioComp.myMainWindow.Generation;


            if (eTrigger && !generation.Contains(currentGeneration) && BioComp.myMainWindow.IsActive)
            {

                //this.Locked = true;

                BioComp.myMainWindow.NewEpoch();
                generation.Add(currentGeneration);

                //canvas.Document.ExpireSolution();

                //this.Locked = false;
                //BioComp.myMainWindow.button_evo.RaiseEvent(new RoutedEventArgs(Button.ClickEvent);
            }


        }



        /// <summary>
        /// Gets the component guid
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("06FB5C8A-B416-4FA3-9BF8-352CE37B0692"); }
        }

        /// <summary>
        /// Create bespoke component attributes
        /// </summary>
        public override void CreateAttributes()
        {
            m_attributes = new BiomorpherTriggerAttributes(this);
        }

        /// <summary>
        /// Locate the component with the rest of the rif raf
        /// </summary>
        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.senary;
            }
        }

        /// <summary>
        /// Icon icon what a lovely icon
        /// </summary>
        protected override Bitmap Icon
        {
            get
            {
                return Properties.Resources.BiomorpherReaderIcon_24;
            }
        }

        /// <summary>
        /// Extra fancy menu items
        /// </summary>
        /// <param name="menu"></param>
        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_AppendItem(menu, @"Github source", GotoGithub);
        }

        /// <summary>
        /// Dare ye go to github?
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GotoGithub(Object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(@"https://github.com/johnharding/Biomorpher");
        }

        
    }
}
