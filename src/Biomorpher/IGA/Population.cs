using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biomorpher.IGA
{
    class Population
    {
        // the current generation
        int generation;
        
        // the population of chromosomes
        public Chromosome[] chromosomes {get; set;}

        // TODO: Store the geometry phenotype here

        /// <summary>
        /// Construct a new population of chromosomes
        /// </summary>
        /// <param name="popSize"></param>
        public Population(int popSize, int sliderCount)
        {
            chromosomes = new Chromosome[popSize];
            generation = 0;
            GenerateRandom(sliderCount);
        }

        /// <summary>
        /// Creates a value copy of the population's chomosomes and genes
        /// </summary>
        /// <param name="pop"></param>
        public Population(Population pop)
        {
            // same length
            chromosomes = new Chromosome[pop.chromosomes.Length];

            // clone the chromosomes
            for(int i=0; i<chromosomes.Length; i++)
                chromosomes[i] = pop.chromosomes[i].Clone();
        }

        /// <summary>
        /// Generates a random population of new chromosomes. TODO: take away seed
        /// </summary>
        /// <param name="geneNumber"></param>
        public void GenerateRandom(int geneNumber)
        {
            for (int i = 0; i < chromosomes.Length; i++)
            {
                chromosomes[i] = new Chromosome(geneNumber);
                chromosomes[i].GenerateRandomGenes(i);
            }
        }

    }
}
