using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Biomorpher.IGA
{
    public class Chromosome
    {
        private Random randGene;
        private Random randMutate;
        public List<Mesh> phenotype;       // TODO: should be more than meshes at some point
        private List<double> performance;   // (optional) list of performance values
        private List<string> criteria;      // (optional) labels for performance criteria
        
        // Normalised values used in the chromosome
        private double[] genes;

        // The real slider values
        // private double[] mapped_genes;
        private double fitness {get; set;}


        public Chromosome(int newLength)
        {
            genes = new double[newLength];
            fitness = 0.0;
            randMutate = new Random();
        }

        public void GenerateRandomGenes(int seed)
        {
            randGene = new Random(seed+10);

            for (int i=0; i<genes.Length; i++)
            {
                genes[i] = randGene.NextDouble();
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
            Chromosome clone = new Chromosome(this.genes.Length);
            Array.Copy(this.genes, clone.genes, this.genes.Length);
            clone.fitness = this.fitness;

            // need to also clone the phenotype geometry and optional performance criteria

            return clone;
        }

        /// <summary>
        /// Goes through each gene and mutates it depending on a given probability
        /// </summary>
        /// <param name="probability"></param>
        public void Mutate(double probability)
        {
            for(int i=0; i<genes.Length; i++)
            {
                double tempRand = randMutate.NextDouble();

                if (tempRand < probability)
                {
                    // int index = (int)(randMutate.NextDouble() * genes.Length);
                    double newgene = randMutate.NextDouble();
                    genes[i] = newgene;
                }
            }
        }

        /// <summary>
        /// Returns the genes for this chromosome
        /// </summary>
        /// <returns></returns>
        public double[] GetGenes()
        {
            return genes;
        }

        /// <summary>
        /// Sets the phenotype for this chromosome with some input geometry
        /// </summary>
        /// <param name="meshes"></param>
        public void SetPhenotype(List<Mesh> meshes)
        {
            // Reset the phenotype to some input meshes
            // TODO: in the future this should be generic geometry including curves, etc.
            //       the argument for this method will have to be updated also.
            phenotype = new List<Mesh>(meshes);

            // TODO: Update performance criteria in this method.
        }

    }
 
}
