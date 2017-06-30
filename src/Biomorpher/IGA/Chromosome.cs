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
        /// A soupdragon is someone that wants to be recorded after an optimisation run
        /// </summary>
        public bool isSoupDragon;

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
        /// Unique chromosome ID
        /// </summary>
        public int ID;

        /// <summary>
        /// Main chromosome constructor
        /// </summary>
        /// <param name="sliders">Grasshopper sliders used to formulate the chromosome</param>
        /// <param name="genePools">Grasshopper genepools used to formulate the chromosome</param>
        public Chromosome(List<GH_NumberSlider> sliders, List<GalapagosGeneListObject> genePools, int id)
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
            isChecked = false;
            isSoupDragon = false;

            clusterId = -1;
            distToRepresentative = -1.0;
            ID = id;
        }

        /// <summary>
        /// Generates a random set of genes for this chromosome
        /// </summary>
        public void GenerateRandomGenes()
        {
            for (int i=0; i<genes.Length; i++) genes[i] = Friends.GetRandomDouble();
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
            Chromosome clone = new Chromosome(this.chromoSliders, this.chromoGenePools, this.ID);

            // Clone gene array
            Array.Copy(this.genes, clone.genes, this.genes.Length);
            
            // Clone fitness and K-means data
            clone.fitness = this.fitness;
            clone.clusterId = this.clusterId;
            clone.isRepresentative = this.isRepresentative;
            clone.distToRepresentative = this.distToRepresentative;
            clone.isChecked = this.isChecked;
            clone.isSoupDragon = this.isSoupDragon;

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
