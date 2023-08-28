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
        int clusterShowID = -1;
        int clusterShowIDLimbo = -1;

        int newEpochID = -1;
        int newEpochIDLimbo = -1;

        public Grasshopper.GUI.Canvas.GH_Canvas canvas;
        BiomorpherComponent BioComp = null;


        /// <summary>
        /// Main constructor
        /// </summary>
        public BiomorpherTrigger()
            : base("BiomorpherTrigger", "BiomorpherTrigger", "Uses Biomorpher data to display paramter states", "Params", "Util")
        {
            canvas = Instances.ActiveCanvas;
            this.IconDisplayMode = GH_IconDisplayMode.icon;

        }

        /// <summary>
        /// Register component inputs
        /// </summary>
        /// <param name="pm"></param>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pm)
        {
            pm.AddIntegerParameter("ClusterShow", "ClusterShow", "(Optional) Show one of the cluster centroids", GH_ParamAccess.item, -1);
            pm.AddIntegerParameter("NewEpoch", "NewEpoch", "Trigger evolution from the component", GH_ParamAccess.item, -1);

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

            DA.GetData<int>("ClusterShow", ref clusterShowID);
            DA.GetData<int>("NewEpoch", ref newEpochID);

            //OnPingDocument().FindObject<GH_Component>()

            List<IGH_ActiveObject> canvasObject = canvas.Document.ActiveObjects();

            // Check for Embryo Components on the canvas
            for (int i = 0; i < canvasObject.Count; i++)
            {
                string george = canvasObject[i].ComponentGuid.ToString();

                if (george == "87264cc5-8461-4003-8ff7-7584b13baf06")
                {
                    BioComp = (BiomorpherComponent)canvasObject[i];
                    //willingOutput.Add((IGH_Param)willingThing.Params.Input[0].Sources[n]);
                }
            }


            /*
            if (hasbeenDoubleClicked)
            {
                if (newEpochID >= 0 && myMainWindow.GetGoState() && newEpochID != newEpochIDLimbo && myMainWindow.IsVisible)
                {
                    myMainWindow.NewEpoch();
                    newEpochIDLimbo = newEpochID;
                }
            }
            */

        }

       

        //public void ScheduleSolution(0.001, GH_Document.GH_ScheduleDelegate(canvas.Document){

        //}


        /// <summary>
        /// Runs after the solve instance method
        /// </summary>
        protected override void AfterSolveInstance()
        {
            Chromosome thisChromo = null;

            if (BioComp != null)
            {
                // If the initial population has been set up, the go state will be true
                // We have this clustershowIDLimbo because when the sliders change, the component is expired. This avoids a neverending loop - or should do.
                if (BioComp.hasbeenDoubleClicked)
                {
                    if (clusterShowID >= 0 && clusterShowID <= 11 && BioComp.myMainWindow.GetGoState() && clusterShowID != clusterShowIDLimbo && BioComp.myMainWindow.IsVisible)
                    {
                        for (int i = 0; i < BioComp.myMainWindow.GetPopulation().chromosomes.Length; i++)
                        {
                            if (BioComp.myMainWindow.GetPopulation().chromosomes[i].isRepresentative && BioComp.myMainWindow.GetPopulation().chromosomes[i].clusterId == clusterShowID)
                                thisChromo = BioComp.myMainWindow.GetPopulation().chromosomes[i];
                        }

                        if (thisChromo != null)
                        {
                            BioComp.myMainWindow.SetInstance(thisChromo);
                            clusterShowIDLimbo = clusterShowID;
                        }
                    }
                }


                // Now with the evolution button pressing. Why does this expire Biomorpher if we schedule the button event!!
                if (BioComp.myMainWindow.GetGoState() && newEpochID != newEpochIDLimbo && BioComp.myMainWindow.IsVisible)
                {
                    BioComp.myMainWindow.button_evo.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
                    newEpochIDLimbo = newEpochID;
                }
            }
            else
            {
                Message = "Can't find the Biomorpher Component";
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
                return Properties.Resources.BiomorpherTriggerIcon;
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
