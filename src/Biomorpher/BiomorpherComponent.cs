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
using Grasshopper.Kernel.Special;
using GalapagosComponents;
using Grasshopper.Kernel.Data;

namespace Biomorpher
{
    /// <summary>
    /// The Grasshopper component
    /// </summary>
    public class BiomorpherComponent : GH_Component
    {
        public Grasshopper.GUI.Canvas.GH_Canvas canvas; 
        public bool GO = false;
        private IGH_DataAccess deej;
        public int solveinstanceCounter;
        private GH_Structure<GH_Number> clusterNumbers, historicNumbers, populationNumbers;
        private static readonly object syncLock = new object();
        public GH_Structure<GH_Number> existingPopTree = new GH_Structure<GH_Number>();

        private List<GH_NumberSlider> cSliders = new List<GH_NumberSlider>();
        private List<GalapagosGeneListObject> cGenePools = new List<GalapagosGeneListObject>();

        private BiomorpherData myOutputData = new BiomorpherData();

        /// <summary>
        /// Main constructor
        /// </summary>
        public BiomorpherComponent()
            : base("Biomorpher", "Biomorpher", "Interactive Genetic Algorithms for Grasshopper", "Params", "Util")
        {    
            solveinstanceCounter = 0;
            canvas = Instances.ActiveCanvas;
            this.IconDisplayMode = GH_IconDisplayMode.icon;
        }

        /// <summary>
        /// Register component inputs
        /// </summary>
        /// <param name="pm"></param>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pm)
        {
            pm.AddNumberParameter("Genome", "Genome", "(genotype) Connect sliders and genepools here", GH_ParamAccess.tree);
            pm.AddMeshParameter("Mesh(es)", "Mesh(es)", "(phenotype) Connect geometry here: currently meshes only please. Use mesh pipe for lines", GH_ParamAccess.tree);
            pm.AddNumberParameter("Performance", "Performance", "(Optional) List of performance measures for the design. One per output parameter only", GH_ParamAccess.tree);
            pm.AddNumberParameter("InitialPop", "InitialPop", "(Optional) initial population (non-random)", GH_ParamAccess.tree);
            //pm.AddIntegerParameter("Selection", "Selection", "(Optional) selection choice for each generation", GH_ParamAccess.list);

            pm[0].WireDisplay = GH_ParamWireDisplay.faint;
            pm[1].WireDisplay = GH_ParamWireDisplay.faint;
            pm[2].WireDisplay = GH_ParamWireDisplay.faint;
            pm[3].WireDisplay = GH_ParamWireDisplay.faint;
            //pm[4].WireDisplay = GH_ParamWireDisplay.faint;

            pm[2].Optional = true;
            pm[3].Optional = true;
            //pm[4].Optional = true;
        }
        
        /// <summary>
        /// Register component outputs
        /// </summary>
        /// <param name="pm"></param>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pm)
        {
            pm.AddTextParameter("GenoGuids", "GenoGuids", "GUIDs of the sliders and genepools to be manipulated", GH_ParamAccess.list);
            pm.AddGenericParameter("Population", "Population", "Current biomorpher population as normalised genes", GH_ParamAccess.tree);
            pm.AddGenericParameter("Historic", "Historic", "Historic biomorpher populations as normalised genes", GH_ParamAccess.tree);
            pm.AddGenericParameter("Clusters", "Clusters", "K-means clusters as normalised genes", GH_ParamAccess.tree);
        }

        /// <summary>
        /// Grasshopper solveinstance
        /// </summary>
        /// <param name="DA"></param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            DA.GetDataTree("InitialPop", out existingPopTree);

            // Make the DA global
            if (solveinstanceCounter == 0)
            {
                deej = DA;
            }

            // Output info
            if (populationNumbers != null)  myOutputData.SetPopulationData(populationNumbers);
            if (historicNumbers != null)    myOutputData.SetHistoricData(historicNumbers);
            if (clusterNumbers!=null)       myOutputData.SetClusterData(clusterNumbers);
            if (cSliders!=null)             myOutputData.SetSliderData(cSliders);
            if (cGenePools!=null)           myOutputData.SetGenePoolData(cGenePools);

            if (myOutputData.GetPopulationData() != null)
            {
                DA.SetDataList(0, myOutputData.GetGenoGUIDs());
            }

            if (myOutputData.GetPopulationData() != null)
            {
                DA.SetDataTree(1, myOutputData.GetPopulationData());
            }
            else
            {
                //this.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Data contains no population");
                //return;
            }

            if (myOutputData.GetHistoricData() != null) DA.SetDataTree(2, myOutputData.GetHistoricData());
            if (myOutputData.GetClusterData() != null) DA.SetDataTree(3, myOutputData.GetClusterData());    

            solveinstanceCounter++;

        }

        /// <summary>
        /// Gets the current sliders in Input[0]
        /// </summary>
        public bool GetSliders(List<GH_NumberSlider> sliders, List<GalapagosGeneListObject> genePools)
        {
            bool hasData = false;

            lock (syncLock)
            { // synchronize

                foreach (IGH_Param param in this.Params.Input[0].Sources)
                {
                    Grasshopper.Kernel.Special.GH_NumberSlider slider = param as Grasshopper.Kernel.Special.GH_NumberSlider;
                    if (slider != null)
                    {
                        sliders.Add(slider);
                        hasData = true;
                    }

                    GalapagosGeneListObject genepool = param as GalapagosGeneListObject;
                    if (genepool != null)
                    {
                        genePools.Add(genepool);
                        hasData = true;
                    }
                    
                }
            }

            cSliders = sliders;
            cGenePools = genePools;

            return hasData;
        }


        /// <summary>
        /// Sets the current slider values for a geven input chromosome
        /// </summary>
        /// <param name="chromo"></param>
        /// <param name="sliders"></param>
        /// <param name="genePools"></param>
        public void SetSliders(Chromosome chromo, List<GH_NumberSlider> sliders, List<GalapagosGeneListObject> genePools)
        {
            double[] genes = chromo.GetGenes();
            int sCount = sliders.Count;

            for (int i = 0; i < sCount; i++)
            {
                double min = (double)sliders[i].Slider.Minimum;
                double max = (double)sliders[i].Slider.Maximum;
                double range = max - min;

                sliders[i].Slider.Value = (decimal)(genes[i] * range + min);
            }

            // Set the gene pool values
            // Note that we use the back end of the genes, beyond the slider count
            int geneIndex = sCount;

            for (int i = 0; i < genePools.Count; i++)
            {
                for (int j = 0; j < genePools[i].Count; j++)
                {
                    Decimal myDecimal = System.Convert.ToDecimal(genes[geneIndex]);
                    genePools[i].set_NormalisedValue(j, myDecimal);

                    geneIndex++;
                }
                genePools[i].ExpireSolution(false);
            }
        }

        /// <summary>
        /// Updates the geometry for an input chromosome
        /// </summary>
        /// <param name="chromo">The chromosome used to get geometry from the gh canvas</param>
        /// <returns></returns>
        public int GetGeometry(Chromosome chromo)
        {

            // Collect the object at the current instance
            List<object> localObjs = new List<object>();

            // Thank you Dimitrie :)
            foreach (IGH_Param param in Params.Input[1].Sources)
            {
                foreach (Object myObj in param.VolatileData.AllData(true)) // AllData flattens the tree
                {
                    localObjs.Add(myObj);
                }
            }

            // TODO: The allGeometry should not be of type Mesh.
            List<Mesh> allGeometry = new List<Mesh>();

            // Get only mesh geometry from the object list
            for (int i = 0; i < localObjs.Count; i++)
            {
                if (localObjs[i] is GH_Mesh)
                {
                    GH_Mesh myGHMesh = new GH_Mesh();
                    myGHMesh = (GH_Mesh)localObjs[i];
                    Mesh myLocalMesh = new Mesh();
                    GH_Convert.ToMesh(myGHMesh, ref myLocalMesh, GH_Conversion.Primary);
                    myLocalMesh.Faces.ConvertQuadsToTriangles();
                    //Mesh joinedMesh = new Mesh(); yes this is commented out and no I am not a software engineer. Deal with it.
                    //joinedMesh.Append(myLocalMesh);
                    allGeometry.Add(myLocalMesh);
                }
            }

            // Get performance data
            List<double> performas = new List<double>();
            List<string> criteria = new List<string>();

            // Cap at eight criteria max.
            int pCount = 0;

            foreach (IGH_Param param in Params.Input[2].Sources)
            {
                foreach (Object myObj in param.VolatileData.AllData(true))
                {
                    if (myObj is GH_Number && pCount < 8)
                    {
                        if(!criteria.Contains(param.NickName))
                        {
                            GH_Number temp = (GH_Number)myObj;
                            performas.Add(temp.Value);
                            criteria.Add(param.NickName);
                            pCount++;
                        }
                    }

                    else if (myObj is GH_Integer && pCount < 8)
                    {
                        if (!criteria.Contains(param.NickName))
                        {
                            GH_Integer temp = (GH_Integer)myObj;
                            performas.Add((double)temp.Value);
                            criteria.Add(param.NickName);
                            pCount++;
                        }
                    }
                }
            }

            // Set the phenotype within the chromosome class
            chromo.SetPhenotype(allGeometry, performas, criteria);

            // Return the number of performance criteria
            return performas.Count;
        }

        /*
         * Get choice method to be implemented later.
         * 
        /// <summary>
        /// Gets
        /// </summary>
        /// <returns></returns>
        public List<int> GetGhoice()
        {
            // Collect the object at the current instance
            List<int> choices = new List<int>();
            
            // Thank you Dimitrie :)
            foreach (IGH_Param param in Params.Input[4].Sources)
            {
                foreach (Object myObj in param.VolatileData.AllData(true)) // AllData flattens the tree
                {
                    if (myObj is Grasshopper.Kernel.Types.GH_Number)
                    {
                        GH_Number thisChoice = (GH_Number)myObj;
                        int x = (int)thisChoice.Value;
                        if(x>=0 && x<=11)
                        {
                            choices.Add(x);
                        }            
                    }
                }
            }

            return choices;
        }
        */

        /// <summary>
        /// Population cluster data for the component output. Outputs normalised genes values.
        /// </summary>
        /// <param name="pop">Uses the given population to set the cluster output data</param>
        public void SetComponentOut(Population pop, List<BioBranch> BioBranches)
        {

            // Curent pop
            populationNumbers = new GH_Structure<GH_Number>();
            
            for (int i = 0; i < pop.chromosomes.Length; i++)
            {
                GH_Path myPath = new GH_Path(i);

                List<GH_Number> myList = new List<GH_Number>();
                for (int k = 0; k < pop.chromosomes[i].GetGenes().Length; k++)
                {
                    GH_Number myGHNumber = new GH_Number(pop.chromosomes[i].GetGenes()[k]);
                    myList.Add(myGHNumber);
                }

                populationNumbers.AppendRange(myList, myPath);
            }


            // Cluster data
            clusterNumbers = new GH_Structure<GH_Number>();

            for (int i = 0; i < 12; i++)
            {
                GH_Path myPath = new GH_Path(i);
                int localCounter = 0;

                // Go through all the chromosomes. If cluster ID of it equals 'i', then stick it in the branch.
                for (int j = 0; j < pop.chromosomes.Length; j++)
                {
                    List<GH_Number> myList = new List<GH_Number>();
                    
                    if (pop.chromosomes[j].clusterId == i)
                    {
                        for (int k = 0; k < pop.chromosomes[j].GetGenes().Length; k++)
                        {
                            GH_Number myGHNumber = new GH_Number(pop.chromosomes[j].GetGenes()[k]);
                            myList.Add(myGHNumber);
                        }

                        clusterNumbers.AppendRange(myList, myPath.AppendElement(localCounter));
                        localCounter++;
                    }   

                }
            }


            // Historic pop
            historicNumbers = new GH_Structure<GH_Number>();

            for (int i = 0; i < BioBranches.Count; i++)
            {
                for (int j = 0; j < BioBranches[i].Twigs.Count; j++)
                {
                    for (int k = 0; k < BioBranches[i].Twigs[j].chromosomes.Length; k++)
                    {

                        List<GH_Number> myList = new List<GH_Number>();
                        for (int c = 0; c < pop.chromosomes[k].GetGenes().Length; c++)
                        {
                            GH_Number myGHNumber = new GH_Number(BioBranches[i].Twigs[j].chromosomes[k].GetGenes()[c]);
                            myList.Add(myGHNumber);
                        }

                        GH_Path myPath = new GH_Path(i, j, k);
                        historicNumbers.AppendRange(myList, myPath);

                    }

                }
            }




            this.ExpireSolution(true);
        }

        /// <summary>
        /// Gets the component guid
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("87264CC5-8461-4003-8FF7-7584B13BAF06"); }
        }

        /// <summary>
        /// Create bespoke component attributes
        /// </summary>
        public override void CreateAttributes()
        {
            m_attributes = new BiomorpherAttributes(this);
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
                return Properties.Resources.BiomorpherIcon2_24;
            }
        }

        /// <summary>
        /// Extra fancy menu items
        /// </summary>
        /// <param name="menu"></param>
        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_AppendItem(menu, @"All Sliders", AddAllSliders);
            Menu_AppendItem(menu, @"Selected Sliders", AddSelectedSliders);
            Menu_AppendItem(menu, @"No Sliders", RemoveAllSliders);
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

        /// <summary>
        /// Add all sliders
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddAllSliders(Object sender, EventArgs e)
        {
            try
	        {
                IEnumerator<IGH_DocumentObject> enumerator = canvas.Document.Objects.GetEnumerator();
		        while (enumerator.MoveNext())
		        {
			        IGH_DocumentObject current = enumerator.Current;
			        if (current != null)
			        {
                        if (current is GH_NumberSlider)
				        {
                            this.Params.Input[0].AddSource((IGH_Param)current, 0);
				        }
			        }
		        }
	        }
            catch
            {

            }

            ExpireSolution(true);
        }


        /// <summary>
        /// Add selected sliders
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddSelectedSliders(Object sender, EventArgs e)
        {
            try
            {
                IEnumerator<IGH_DocumentObject> enumerator = canvas.Document.Objects.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    IGH_DocumentObject current = enumerator.Current;
                    if (current != null)
                    {
                        if (current.Attributes.Selected)
                        {
                            if (current is GH_NumberSlider)
                            {
                                this.Params.Input[0].AddSource((IGH_Param)current, 0);
                            }
                        }
                    }
                }
            }
            catch
            {

            }

            ExpireSolution(true);
        }


        /// <summary>
        /// Remove all connected sliders
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RemoveAllSliders(Object sender, EventArgs e)
        {
            this.Params.Input[0].RemoveAllSources();
            ExpireSolution(true);
        }


        /// <summary>
        /// Add a warning to the component
        /// </summary>
        /// <param name="text"></param>
        public void AddWarning(string text)
        {
            this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, text);
        }

        //TODO: send to grasshopper group from a window link (in About)
        //private void gotoGrasshopperGroup(Object sender, EventArgs e)
        //{
        //System.Diagnostics.Process.Start(@"http://www.????");
        //}
    }
}
