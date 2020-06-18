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
    /// <summary>
    /// Chromosome representing one Grasshopper instance
    /// </summary>
    public class Chromosome
    {
        /// <summary>
        /// Geometry phenotype. Currently only meshes
        /// </summary>
        public List<Mesh> phenotype;

        /// <summary>
        /// Optional list of performance values
        /// </summary>
        private List<double> performance; 

        /// <summary>
        /// Associated list of performance criteria descriptions
        /// </summary>
        private List<string> criteria; 
        
        /// <summary>
        /// gene array
        /// </summary>
        private double[] genes;

        /// <summary>
        /// List of associated sliders to this chromosome
        /// </summary>
        private List<Grasshopper.Kernel.Special.GH_NumberSlider> chromoSliders;

        /// <summary>
        /// List of associated genepools to this chromosome
        /// </summary>
        private List<GalapagosComponents.GalapagosGeneListObject> chromoGenePools;

        /// <summary>
        /// Fitness from 0.0 to 1.0 used for elitism selection
        /// </summary>
        private double fitness {get; set;}

        /// <summary>
        /// Describes if the chromosome is a k-means cluster representative
        /// </summary>
        public bool isRepresentative;

        /// <summary>
        /// Describes if the member is an elite candidate and preserved for the next generation
        /// </summary>
        public bool isElite;

        /// <summary>
        /// A design that has min or max fitness for the population. May be more than one depending on objective count.
        /// </summary>
        public bool isOptimal;

        /// <summary>
        /// A design that is min for the population
        /// </summary>
        public bool isMinimum;

        /// <summary>
        /// A design that is max for the population
        /// </summary>
        public bool isMaximum;

        /// <summary>
        /// The associated cluster centroid representative ID (could be itself).
        /// </summary>
        public int clusterId;

        /// <summary>
        /// Distance to cluster representative (could be zero). Used for K-means plot.
        /// </summary>
        public double distToRepresentative;

        /// <summary>
        /// Indicates if it is checked
        /// </summary>
        public bool isChecked;

        /// <summary>
        /// Main chromosome constructor
        /// </summary>
        /// <param name="sliders">Grasshopper sliders used to formulate the chromosome</param>
        /// <param name="genePools">Grasshopper genepools used to formulate the chromosome</param>
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
            fitness = 0.0;

            isRepresentative = false;
            isElite = false;
            isChecked = false;
            isOptimal = false;
            isMinimum = false;
            isMaximum = false;

            clusterId = -1;
            distToRepresentative = -1.0;
        }

        /// <summary>
        /// Cloning constructor
        /// </summary>
        /// <param name="cloner"></param>
        public Chromosome(Chromosome cloner)
        {
            this.chromoSliders = cloner.chromoSliders; // is this copying by reference!?
            this.chromoGenePools = cloner.chromoGenePools; // is this copying by reference!?

            Array.Copy(cloner.genes, this.genes, cloner.genes.Length); // by value

            this.fitness = cloner.fitness;
            this.clusterId = cloner.clusterId;
            this.isRepresentative = cloner.isRepresentative;
            this.isElite = cloner.isElite;
            this.distToRepresentative = cloner.distToRepresentative;
            this.isChecked = cloner.isChecked;
            this.isOptimal = cloner.isOptimal;
            this.isMinimum = cloner.isMinimum;
            this.isMaximum = cloner.isMaximum;

            // Clone phenotype mesh
            if (this.phenotype != null)
            {
                this.phenotype = new List<Mesh>(cloner.phenotype);
            }

            // Clone performance values
            if (this.performance != null)
            {
                this.performance = new List<double>(cloner.performance);
            }

            // Clone crieria
            if (this.criteria != null)
            {
                this.criteria = new List<string>(cloner.criteria);
            }
        }



        /// <summary>
        /// Generates a random set of genes for this chromosome
        /// </summary>
        public void GenerateRandomGenes()
        {
            for (int i=0; i<genes.Length; i++) genes[i] = Friends.GetRandomDouble();
        }

        /// <summary>
        /// Sets the initial genes according to input data
        /// </summary>
        /// <param name="hapsberg"></param>
        public void GenerateExistingGenes(List<double> hapsberg)
        {
            for (int i = 0; i < genes.Length; i++)
            {
                // apply bounds
                double val = hapsberg[i];
                if (val > 1.0) val = 1.0;
                if (val < 0.0) val = 0.0;
                genes[i] = val;
            }
        }

        /// <summary>
        /// Sets the initial genes according to current parameter states
        /// </summary>
        public void GenerateCurrentGenes()
        {
            double[] genes = GetGenes();
            int sCount = chromoSliders.Count;

            for (int i = 0; i < sCount; i++)
            {
                double min = (double)chromoSliders[i].Slider.Minimum;
                double max = (double)chromoSliders[i].Slider.Maximum;
                double range = max - min;

                genes[i] = ((double)chromoSliders[i].Slider.Value - min) / range;
            }

            // Set the gene pool values
            // Note that we use the back end of the genes, beyond the slider count
            int geneIndex = sCount;

            for (int i = 0; i < chromoGenePools.Count; i++)
            {
                for (int j = 0; j < chromoGenePools[i].Count; j++)
                {
                    genes[geneIndex] = (double)chromoGenePools[i].get_NormalisedValue(j);
                    geneIndex++;
                }
            }
        }

        /// <summary>
        /// Returns the fitness value for this chromosome
        /// </summary>
        /// <returns></returns>
        public double GetFitness()
        {
            return fitness;
        }

        /// <summary>
        /// Clones the chromosome including k-means data, geometry phenotype and performance data
        /// </summary>
        /// <returns></returns>
        public Chromosome Clone()
        {
            // Clone sliders and genepools associated with this chromosome
            Chromosome clone = new Chromosome(this.chromoSliders, this.chromoGenePools);

            // Clone gene array
            // These are value types, so all good here
            Array.Copy(this.genes, clone.genes, this.genes.Length);
            
            // Clone fitness and K-means data
            clone.fitness = this.fitness;
            clone.clusterId = this.clusterId;
            clone.isRepresentative = this.isRepresentative;
            clone.isElite = this.isElite;
            clone.distToRepresentative = this.distToRepresentative;
            clone.isChecked = this.isChecked;
            clone.isOptimal = this.isOptimal;
            clone.isMinimum = this.isMinimum;
            clone.isMaximum = this.isMaximum;

            // Clone phenotype mesh
            if (this.phenotype != null)
            {
                clone.phenotype = new List<Mesh>(this.phenotype);
            }

            // Clone performance values
            if (this.performance != null)
            {
                clone.performance = new List<double>(this.performance);
            }

            // Clone crieria
            if (this.criteria != null)
            {
                clone.criteria = new List<string>(this.criteria);
            }
            
            // Return new chromosome
            return clone;
        }

        /// <summary>
        /// Goes through each gene and mutates it depending on a given probability
        /// </summary>
        /// <param name="probability"> probability that a single gene in the chomrosome will mutate</param>
        public void Mutate(double probability)
        {
            for(int i=0; i<genes.Length; i++)
            {
                double tempRand = Friends.GetRandomDouble();

                if (tempRand < probability)
                {
                    genes[i] = Friends.GetRandomDouble();
                }
            }
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
        /// Sets a new set of gene values. MUST be same length else old genes are maintained.
        /// </summary>
        /// <param name="newgenes"></param>
        public void SetGenes(double[] newgenes)
        {
            if(genes.Length == newgenes.Length)
                newgenes.CopyTo(genes, 0);
        }


        /// <summary>
        /// Sets the phenotype for this chromosome (geometry and performance criteria)
        /// </summary>
        /// <param name="meshes"></param>
        public void SetPhenotype(List<Mesh> meshes, List<double> performas, List<string> crit)
        {
            phenotype = new List<Mesh>(meshes);
            performance = new List<double>(performas);
            criteria = new List<string>(crit);
        }

        /// <summary>
        /// Sets the performance criteria without changing the mesh
        /// </summary>
        /// <returns></returns>
        public void SetPerformas(List<double> performas, List<string> crit)
        {
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
        /// Cumulates the current fitness value
        /// </summary>
        /// <param name="value"></param>
        public void CummulateFitness(double value)
        {
            fitness += value;
        }


        /// <summary>
        /// Shakes the genes a little
        /// </summary>
        /// <param name="t">amount to jiggle the genes by from 0.0 to 1.0</param>
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
