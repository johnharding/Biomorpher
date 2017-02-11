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
        
        // IDs of (12) cluster representatives
        private List<int> clusterIDs {get; set;}

        // TODO: Store the geometry phenotype here

        /// <summary>
        /// Construct a new population of chromosomes
        /// </summary>
        /// <param name="popSize"></param>
        public Population(int popSize, List<Grasshopper.Kernel.Special.GH_NumberSlider> sliders)
        {
            chromosomes = new Chromosome[popSize];
            generation = 0;
            GenerateRandomPop(sliders);
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
        public void GenerateRandomPop(List<Grasshopper.Kernel.Special.GH_NumberSlider> sliders)
        {
            for (int i = 0; i < chromosomes.Length; i++)
            {
                chromosomes[i] = new Chromosome(sliders);
                chromosomes[i].GenerateRandomGenes();
            }
        }

        /// <summary>
        /// Replaces the population with a new one based on fitness using Roulette Wheel selection
        /// </summary>
        public void RoulettePop()
        {
            // Roulette Wheel technique here as described by Melanie Mitchell, An Introduction to GAs, p.166
            double fitSum;
            double totalFitness = 0.0;

            // Set up a fresh population  
            // TODO: Do we have to calculate new geometry for everything? Why not have flags if GetGeometry() needs to be called
            Population newPop = new Population(this.chromosomes.Length, this.chromosomes[0].GetGenes().Length);

            // find the total fitness
            for (int i = 0; i < chromosomes.Length; i++)
            {
                totalFitness += chromosomes[i].GetFitness();
            }

            // Now for the roulette wheel selection for the new population
            for (int i = 0; i < newPop.chromosomes.Length; i++)
            {
                double weightedRandom = Friends.GetRandomDouble()*totalFitness;
                fitSum = 0.0;

                for (int j = 0; j < chromosomes.Length; j++)
                {
                    fitSum += chromosomes[j].GetFitness();
                    if (fitSum > weightedRandom)
                    {
                        newPop.chromosomes[i] = chromosomes[j].Clone();
                        break;
                    }
                }
            }
            
            // Replace the current popultion of chromosomes with the newPop
            for (int i = 0; i < chromosomes.Length; i++)
            {
                chromosomes[i] = newPop.chromosomes[i].Clone();
            }

            // Make sure to reset all the fitnesses here (selection has now already occured).
            ResetAllFitness();
        }

        /// <summary>
        /// Mutates a gene according to a probability
        /// </summary>
        /// <param name="probability"></param>
        public void MutatePop(double probability)
        {
            for (int i = 0; i < chromosomes.Length; i++)
            {
                chromosomes[i].Mutate(probability);
            }
        }

        /// <summary>
        /// Resets all the fitness values to zero at the start of a new generation
        /// </summary>
        public void ResetAllFitness()
        {
            for (int i = 0; i < chromosomes.Length; i++)
            {
                chromosomes[i].SetFitness(0.0);
            }
        }

    }
}
