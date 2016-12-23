using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biomorpher.GA
{
    class Population
    {
        // the current generation
        int generation;
        
        // the population of chromosomes
        Chromosome[] chromosomes;

        // TODO: Store the geometry phenotype here

        /// <summary>
        /// Construct a new population of chromosomes
        /// </summary>
        /// <param name="popSize"></param>
        public Population(int popSize)
        {
            chromosomes = new Chromosome[popSize];
            generation = 0;
        }

        void GenerateRandom()
        {

        }

    }
}
