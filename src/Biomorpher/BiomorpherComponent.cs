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

        // For the outputs
        private GH_Structure<GH_Number> historicNumbers;
        private GH_Structure<GH_Guid> genoGuids;
        private int popCount = 0;

        private static readonly object syncLock = new object();
        private BiomorpherDataParam myParam;

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
            pm.AddMeshParameter("Meshes", "Meshes", "(phenotype) Connect geometry here: currently meshes only please. Use mesh pipe for lines", GH_ParamAccess.tree);
            pm.AddNumberParameter("Perform", "Perform", "(Optional) List of performance criteria for the design. One per output parameter only", GH_ParamAccess.tree);
            
            pm[0].WireDisplay = GH_ParamWireDisplay.faint;
            pm[1].WireDisplay = GH_ParamWireDisplay.faint;
            pm[2].WireDisplay = GH_ParamWireDisplay.faint;

            pm[2].Optional = true;
        }
        
        /// <summary>
        /// Register component outputs
        /// </summary>
        /// <param name="pm"></param>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pm)
        {
            myParam = new BiomorpherDataParam();
            pm.AddParameter(myParam, "Solution", "Solution", "Biomorpher Solution Data for use in reader", GH_ParamAccess.item);
        }

        /// <summary>
        /// Grasshopper solveinstance
        /// </summary>
        /// <param name="DA"></param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            // Make the DA global
            if (solveinstanceCounter == 0)
            {
                deej = DA;
            }

            // Output info
                                            myOutputData.PopCount = popCount;
            if (historicNumbers != null)    myOutputData.SetHistoricData(historicNumbers);
            if (genoGuids != null)          myOutputData.SetGenoGuids(genoGuids);

            DA.SetData(0, new BiomorpherGoo(myOutputData));

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

            // Store info within Component
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
                    //Mesh joinedMesh = new Mesh(); yes this is commented out and no I am not a software engineer.
                    //joinedMesh.Append(myLocalMesh);
                    allGeometry.Add(myLocalMesh);
                }
            }

            // Get performance data
            List<double> performas = new List<double>();
            List<string> criteria = new List<string>();

            // Cap at eight criteria max.
            int pCount = 0;
            int repeatCounter = 1;

            foreach (IGH_Param param in Params.Input[2].Sources)
            {
                foreach (Object myObj in param.VolatileData.AllData(true))
                {
                    if (myObj is GH_Number && pCount < 8)
                    {
                        GH_Number temp = (GH_Number)myObj;
                        performas.Add(temp.Value);

                        if (!criteria.Contains(param.NickName))
                        {
                            criteria.Add(param.NickName);
                        }

                        else
                        {
                            criteria.Add(param.NickName + " (" + repeatCounter + ")");
                            repeatCounter++;
                        }

                        pCount++;
                    }

                    else if (myObj is GH_Integer && pCount < 8)
                    {
                        GH_Integer temp = (GH_Integer)myObj;
                        performas.Add((double)temp.Value);

                        if (!criteria.Contains(param.NickName))
                        {
                            criteria.Add(param.NickName);
                        }

                        else
                        {
                            criteria.Add(param.NickName + " (" + repeatCounter + ")");
                            repeatCounter++;
                        }

                        pCount++;
                    }
                }
            }

            // Set the phenotype within the chromosome class
            chromo.SetPhenotype(allGeometry, performas, criteria);

            // Return the number of performance criteria
            return performas.Count;
        }

        /// <summary>
        /// Updates the GH_Structure component out values ready to go
        /// </summary>
        /// <param name="pop"></param>
        /// <param name="BioBranches"></param>
        /// <param name="performanceCount"></param>
        /// <param name="bioBranchID"></param>
        public void SetComponentOut(Population pop, List<BioBranch> BioBranches, int performanceCount, int bioBranchID)
        {

            popCount = pop.chromosomes.Length;

            // Historic pop
            historicNumbers = new GH_Structure<GH_Number>();

            for (int i = 0; i < BioBranches.Count; i++)
            {
                for (int j = 0; j < BioBranches[i].PopTwigs.Count; j++)
                {
                    for (int k = 0; k < BioBranches[i].PopTwigs[j].chromosomes.Length; k++)
                    {

                        List<GH_Number> myList = new List<GH_Number>();
                        for (int c = 0; c < pop.chromosomes[k].GetGenes().Length; c++)
                        {
                            GH_Number myGHNumber = new GH_Number(BioBranches[i].PopTwigs[j].chromosomes[k].GetGenes()[c]);
                            myList.Add(myGHNumber);
                        }

                        GH_Path myPath = new GH_Path(i, j, k);
                        historicNumbers.AppendRange(myList, myPath);

                    }

                }
            }

            // Find the current number of population twigs in the latest biobranch
            int lastBranchTwigCount = BioBranches[bioBranchID].PopTwigs.Count;

            // Now add the current population to the output, using the latest biobranch ID.
            // This is going to be really hard to understand in a month's time.
            for (int k = 0; k < pop.chromosomes.Length; k++)
            {
                List<GH_Number> myList = new List<GH_Number>();
                for (int c = 0; c < pop.chromosomes[k].GetGenes().Length; c++)
                {
                    GH_Number myGHNumber = new GH_Number(pop.chromosomes[k].GetGenes()[c]);
                    myList.Add(myGHNumber);
                }

                GH_Path myPath = new GH_Path(bioBranchID, lastBranchTwigCount, k);
                historicNumbers.AppendRange(myList, myPath);
            }



            // Get the Guids for the sliders and genepools to pass on
            genoGuids = new GH_Structure<GH_Guid>();

            List<GH_Guid> mySliderList = new List<GH_Guid>();
            List<GH_Guid> myGenepoolList = new List<GH_Guid>();

            // Add the sliders
            for (int i = 0; i < cSliders.Count; i++)
            {
                GH_Guid myGuid = new GH_Guid(cSliders[i].InstanceGuid);
                mySliderList.Add(myGuid);
            }

            // Add the genepools
            for (int i = 0; i < cGenePools.Count; i++)
            {
                GH_Guid myGuid = new GH_Guid(cGenePools[i].InstanceGuid);
                myGenepoolList.Add(myGuid);
            }

            // Add to the GH_Structure
            genoGuids.AppendRange(mySliderList, new GH_Path(0));
            genoGuids.AppendRange(myGenepoolList, new GH_Path(1));


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
            Menu_AppendItem(menu, @"Connect all sliders", AddAllSliders);
            Menu_AppendItem(menu, @"Connect selected sliders", AddSelectedSliders);
            Menu_AppendItem(menu, @"Remove all sliders", RemoveAllSliders);
            Menu_AppendItem(menu, @"Link to github src", GotoGithub);
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
