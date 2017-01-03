using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.Kernel;

namespace Biomorpher.IGA
{
    public class Chromosome
    {
        private Random randGene;
        private Random randMutate;
        private object phenotype;   // store the geometry here?
        
        // Normalised values used in the chromosome
        private double[] genes;

        // The real slider values
        private double[] mapped_genes;
        double? fitness {get; set;}

        public Chromosome(int newLength)
        {
            genes = new double[newLength];
            fitness = 0.0;

            // Let's work with a seed to start with
            randGene = new Random(21);
            randMutate = new Random(42);
        }

        public void GenerateNew()
        {
            for (int i=0; i<genes.Length; i++)
            {
                genes[i] = randGene.NextDouble();

            }
        }

        double? GetFitness()
        {
            return fitness;
        }

        public Chromosome Clone()
        {
            Chromosome clone = new Chromosome(this.genes.Length);
            Array.Copy(this.genes, clone.genes, this.genes.Length);
            clone.fitness = this.fitness;

            return clone;
        }

        public void Mutate(double probability)
        {
            double tempRand = randMutate.NextDouble();

            if(tempRand < probability)
            {
                int index = (int)(randMutate.NextDouble() * genes.Length);
                double newgene = randMutate.NextDouble();
                this.genes[index] = newgene;
            }

        }

        public double[] GetGenes()
        {
            return genes;
        }

    }
 
}
