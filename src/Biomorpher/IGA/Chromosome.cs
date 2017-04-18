using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Biomorpher.IGA;
using Grasshopper.Kernel.Special;
using GalapagosComponents;


namespace Biomorpher.IGA
{
    public class Chromosome
    {
        public List<Mesh> phenotype;        // TODO: should be more than meshes at some point
        private List<double> performance;   // (optional) list of performance values
        private List<string> criteria;      // (optional) labels for performance criteria
        
        // Normalised values used in the chromosome
        private double[] genes;
        private List<Grasshopper.Kernel.Special.GH_NumberSlider> chromoSliders;
        private List<GalapagosComponents.GalapagosGeneListObject> chromoGenePools;

        // Actual slider values.. 
        private double[] realgenes;

        // Fitness used for elitism selection
        private double fitness {get; set;}

        //k-means representatve
        public bool isRepresentative;

        /// <summary>
        /// The associated cluster centroid representative ID (could be itself)
        /// </summary>
        public int clusterId;

        public double distToRepresentative;

        /// <summary>
        /// Main chromosome constructor
        /// </summary>
        /// <param name="newLength"></param>
        /// <param name="randSeed"></param>
        public Chromosome(List<GH_NumberSlider> sliders, List<GalapagosGeneListObject> genePools)
        {
            chromoSliders = new List<GH_NumberSlider>(sliders);
            chromoGenePools = new List<GalapagosGeneListObject>(genePools);

            int GenePoolCounter = 0;
            for (int i = 0; i < chromoGenePools.Count; i++)
            {
                GenePoolCounter += chromoGenePools[i].Count;
            }

            genes = new double[chromoSliders.Count + GenePoolCounter];
            realgenes = new double[chromoSliders.Count + GenePoolCounter];
            fitness = 0.0;

            isRepresentative = false;
            clusterId = -1;
            distToRepresentative = -1.0;
        }

        public void GenerateRandomGenes()
        {
            // Generates a random set of genes and real counterparts
            for (int i=0; i<genes.Length; i++)
            {
                genes[i] = Friends.GetRandomDouble();
            }

            // Update the real genes
            UpdateRealGenes();
        }


        public void UpdateRealGenes()
        {
            for (int i = 0; i < chromoSliders.Count; i++)
            {
                double sliderMin = (double) chromoSliders[i].Slider.Minimum;
                double sliderMax = (double) chromoSliders[i].Slider.Maximum;

                realgenes[i] = genes[i] * (sliderMax - sliderMin) + sliderMin;
            }

            for (int i = 0; i < chromoGenePools.Count; i++)
            {
                // TODO??
            }


        }

        public double GetFitness()
        {
            return fitness;
        }

        /// <summary>
        /// Clones the chromosome TODO: needs to be more than just the genes we clone here!
        /// </summary>
        /// <returns></returns>
        public Chromosome Clone()
        {
            Chromosome clone = new Chromosome(this.chromoSliders, this.chromoGenePools); // Pass the original sliders and genepools
            Array.Copy(this.genes, clone.genes, this.genes.Length);
            Array.Copy(this.realgenes, clone.realgenes, this.realgenes.Length);

            clone.fitness = this.fitness;
            clone.clusterId = this.clusterId;
            clone.isRepresentative = this.isRepresentative;
            clone.distToRepresentative = this.distToRepresentative;

            if (this.phenotype != null)
            {
                clone.phenotype = new List<Mesh>(this.phenotype);
            }

            if (this.performance != null)
            {
                clone.performance = new List<double>(this.performance);
            }
            
            return clone;
        }

        /// <summary>
        /// Goes through each gene and mutates it depending on a given probability
        /// </summary>
        /// <param name="probability"> probability that a single gene in the chomrosome will mutate</param>
        public void Mutate(double probability)
        {
            bool flag = false;
            for(int i=0; i<genes.Length; i++)
            {
                double tempRand = Friends.GetRandomDouble();

                if (tempRand < probability)
                {
                    genes[i] = Friends.GetRandomDouble();
                    flag = true;
                }
            }

            // Only call this if the chromosome has changed
            if (flag) UpdateRealGenes();

        }

        /// <summary>
        /// Returns the genes for this chromosome
        /// </summary>
        /// <returns>the list of genes</returns>
        public double[] GetGenes()
        {
            return genes;
        }

        /// <summary>
        /// Returns the real genes for this chromosome
        /// </summary>
        /// <returns>the list of real genes (i.e. slider values)</returns>
        public double[] GetRealGenes()
        {
            return realgenes;
        }

        /// <summary>
        /// Returns a list of performance values
        /// </summary>
        /// <returns></returns>
        public List<double> GetPerformas()
        {
            return performance;
        }

        /// <summary>
        /// Returns criteria names
        /// </summary>
        /// <returns></returns>
        public List<string> GetCriteria()
        {
            return criteria;
        }

        /// <summary>
        /// Sets the phenotype for this chromosome with some input geometry
        /// </summary>
        /// <param name="meshes"></param>
        public void SetPhenotype(List<Mesh> meshes, List<double> performas, List<string> crit)
        {
            // Reset the phenotype to some input meshes
            // TODO: in the future this should be generic geometry including curves, etc.
            //       the argument for this method will have to be updated also.
            phenotype = new List<Mesh>(meshes);
            performance = new List<double>(performas);
            criteria = new List<string>(crit);

        }

        /// <summary>
        /// Sets the fitness value (0.0 min to 1.0 max)
        /// </summary>
        public void SetFitness(double value)
        {
            if (value < 0.0) value = 0.0;
            if (value > 1.0) value = 1.0;
            fitness = value;
        }


        /// <summary>
        /// Shakes the genes a little
        /// </summary>
        /// <param name="t"></param>
        public void JiggleGenes(double t)
        {
            for(int i=0; i<genes.Length; i++)
            {
                // Note, you can't risk having two chromosomes with the same genes, even 0.0 and 1.0
                genes[i] += (0.5*t - Friends.GetRandomDouble() * t);
                if (genes[i] < 0.0) 
                    genes[i] = 0.0 + Friends.GetRandomDouble()*0.001;
                if (genes[i] > 1.0) 
                    genes[i] = 1.0 - Friends.GetRandomDouble()*0.001;
            }
        }
    }
 
}
