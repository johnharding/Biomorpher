using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.Kernel.Special;
using GalapagosComponents;

namespace Biomorpher.IGA
{
    public class Population
    {

        // List of sliders
        private List<GH_NumberSlider> popSliders;
        private List<GalapagosGeneListObject> popGenePools;

        // the population of chromosomes
        public Chromosome[] chromosomes { get; set; }

        /// <summary>
        /// Construct a new population of chromosomes
        /// </summary>
        /// <param name="popSize"></param>
        public Population(int popSize, List<GH_NumberSlider> sliders, List<GalapagosGeneListObject> genePools)
        {
            chromosomes = new Chromosome[popSize];
            popSliders = new List<GH_NumberSlider>(sliders);
            popGenePools = new List<GalapagosGeneListObject>(genePools);
            GenerateRandomPop();
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
            for (int i = 0; i < chromosomes.Length; i++)
            {
                chromosomes[i] = pop.chromosomes[i].Clone();
            }
        }

        /// <summary>
        /// Generates a random population of new chromosomes.
        /// </summary>
        /// <param name="geneNumber"></param>
        public void GenerateRandomPop()
        {
            for (int i = 0; i < chromosomes.Length; i++)
            {
                chromosomes[i] = new Chromosome(popSliders, popGenePools);
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
            Population newPop = new Population(this.chromosomes.Length, popSliders, popGenePools);

            // find the total fitness
            for (int i = 0; i < chromosomes.Length; i++)
            {
                totalFitness += chromosomes[i].GetFitness();
            }

            // Now for the roulette wheel selection for the new population
            for (int i = 0; i < newPop.chromosomes.Length; i++)
            {
                double weightedRandom = Friends.GetRandomDouble() * totalFitness;
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



        //----------------------------------------------------------------- K-MEANS --------------------------------------------------------------//

        //K-Means clustering of the chromosomes in this population (overall method that calls the sub-methods)
        public void KMeansClustering(int numClusters)
        {
            // a) Initiliase clustering
            double[][] clusterCentroidsInit = calcClusterCentroidsInit(numClusters);
            updateClustering(numClusters, clusterCentroidsInit);

            //Loop
            bool go = true;
            int count = 0;
            int maxIter = 20;

            while (go && count < maxIter)
            {
                // b) Calculate mean vectors based on current clustering
                double[][] clusterMeanVectors = calcClusterMeans(numClusters);

                // c) Update clustering
                go = updateClustering(numClusters, clusterMeanVectors);

                count++;
            }
            
            // d) Update chromosome info
            calcKMeansRepresentatives(numClusters);
        }



        // a) K-Means++ method to create a better initial clustering
        public double[][] calcClusterCentroidsInit(int numClusters)
        {
            int numGenes = chromosomes[0].GetGenes().Length;

            //Initialise array
            double[][] centroidVectorsInit = new double[numClusters][];
            List<int> centroidChromoIndexes = new List<int>();

            // 1. Choose random initial centroid
            int rndCentroidChromo = Friends.GetRandomInt(0, chromosomes.Length);
            centroidChromoIndexes.Add(rndCentroidChromo);


            while(centroidChromoIndexes.Count < numClusters)
            {
                // 2. Calculate distance from each chromo to the nearest centroid that has already been chosen

                //the distance to the nearest centroid for each chromosome
                List<double> chromoDistances = new List<double>();

                for (int i = 0; i < chromosomes.Length; i++)
                {
                    //distances from one chromo to all of the already chosen centroids
                    List<double> distances = new List<double>();

                    for (int j = 0; j < centroidChromoIndexes.Count; j++)
                    {
                        int centroidIndex = centroidChromoIndexes[j];
                        distances.Add(calcDistance(chromosomes[i].GetGenes(), chromosomes[centroidIndex].GetGenes()));
                    }

                    chromoDistances.Add(distances.Min());  //if the chromosome compares to itself and is chosen as a centroid, the distance will be zero (fine as we choose the largest distance for all chromosomes afterwards)
                }

                //3. Choose next centroid furthest away from the already selected ones
                int indexOfMaxDist = chromoDistances.IndexOf(chromoDistances.Max());
                centroidChromoIndexes.Add(indexOfMaxDist);
            }

            //Update array
            for (int i = 0; i < numClusters; i++)
            {
                centroidVectorsInit[i] = new double[numGenes];

                for (int j = 0; j < numGenes; j++)
                {
                    centroidVectorsInit[i][j] = chromosomes[centroidChromoIndexes[i]].GetGenes()[j];
                }
            }

            return centroidVectorsInit;
        }


        // b) Calculate cluster mean vectors (same length as genes)
        public double[][] calcClusterMeans(int numClusters)
        {
            int numGenes = chromosomes[0].GetGenes().Length;

            //Initialise arrays
            double[][] clusterMeanVectors = new double[numClusters][];

            for (int i = 0; i < numClusters; i++)
            {
                clusterMeanVectors[i] = new double[numGenes];

                for (int j = 0; j < numGenes; j++)
                {
                    clusterMeanVectors[i][j] = 0.0;
                }
            }

            //Run through all the chromosomes
            for (int i = 0; i < chromosomes.Length; i++)
            {
                // get chromosome genes and sum each component
                double[] genes = chromosomes[i].GetGenes();
                for (int j = 0; j < numGenes; j++)
                {
                    clusterMeanVectors[chromosomes[i].clusterId][j] += genes[j];
                }
            }

            //Calculate average
            int[] clusterCounts = calcClusterSizes(numClusters);

            for (int i = 0; i < numClusters; i++)
            {
                for (int j = 0; j < numGenes; j++)
                {
                    clusterMeanVectors[i][j] /= clusterCounts[i];
                }
            }

            return clusterMeanVectors;
        }


        // c) Update clustering by calculating the distance between the chromosome genes and the mean vectors for each cluster
        public bool updateClustering(int numClusters, double[][] clusterMeanVectors)
        {
            bool go = true;
            bool hasChanged = false;
            bool hasZeroMembers = false;

            //Run through all the chromosomes and compare its genes to all the mean vectors. Store new temporary clusterIds
            int[] tempClusterId = new int[chromosomes.Length];

            for (int i = 0; i < chromosomes.Length; i++)
            {
                double[] distances = new double[numClusters];

                //Run through the clusters in order to compare to each mean vector
                for (int j = 0; j < numClusters; j++)
                {
                    distances[j] = calcDistance(chromosomes[i].GetGenes(), clusterMeanVectors[j]);
                }

                int newClusterId = identifyClusterId(distances);
                tempClusterId[i] = newClusterId;

                if (chromosomes[i].clusterId != newClusterId)
                {
                    hasChanged = true;
                }
            }

            //Check that each new cluster contains at least one chromosome before changing the clusterId property
            int[] tempClusterCounts = new int[numClusters];
            for (int i = 0; i < numClusters; i++)
            {
                tempClusterCounts[i] = 0;
            }

            for (int i = 0; i < chromosomes.Length; i++)
            {
                tempClusterCounts[tempClusterId[i]]++;
            }

            for (int i = 0; i < tempClusterCounts.Length; i++)
            {
                if (tempClusterCounts[i] == 0)
                {
                    hasZeroMembers = true;
                }
            }

            //If no cluster has zero elements then update the chromosome clusterId property
            if (!hasZeroMembers)
            {
                for (int i = 0; i < chromosomes.Length; i++)
                {
                    chromosomes[i].clusterId = tempClusterId[i];
                }
            }

            //Check whether the cluster loop shall continue or not
            if (!hasChanged || hasZeroMembers)
            {
                go = false;
            }

            return go;
        }


        // d) Update chromosome representatives and cluster distances after k-means clustering
        public void calcKMeansRepresentatives(int numClusters)
        {
            double[][] clusterMeanVectors = calcClusterMeans(numClusters);

            //Initialise lists
            List<double>[] distances = new List<double>[numClusters];
            List<int>[] distanceIndexes = new List<int>[numClusters];

            for (int i = 0; i < numClusters; i++)
            {
                distances[i] = new List<double>();
                distanceIndexes[i] = new List<int>();
            }

            //Run through each chromosome
            for (int i = 0; i < chromosomes.Length; i++)
            {
                distances[chromosomes[i].clusterId].Add(calcDistance(chromosomes[i].GetGenes(), clusterMeanVectors[chromosomes[i].clusterId]));
                distanceIndexes[chromosomes[i].clusterId].Add(i);
            }

            //Find the chromosome in each cluster with the smallest distance to the cluster mean
            int[] chromoRepresentatives = new int[numClusters];
            for (int i = 0; i < numClusters; i++)
            {
                double minDist = distances[i].Min();
                int indexOfMin = distances[i].IndexOf(minDist);
                int chromoRepId = distanceIndexes[i][indexOfMin];

                chromosomes[chromoRepId].isRepresentative = true;
                chromoRepresentatives[i] = chromoRepId;
            }

            //Calculate the distance between a chromosome and the chromosome closest to the cluster mean
            for (int i = 0; i < chromosomes.Length; i++)
            {
                int closestToMean = chromoRepresentatives[chromosomes[i].clusterId];
                chromosomes[i].distToRepresentative = calcDistance(chromosomes[i].GetGenes(), chromosomes[closestToMean].GetGenes());
            }

        }




        //Helper methods

        //Calculate Euclidean distance between two n-dimensional vectors
        public double calcDistance(double[] genes, double[] mean)
        {
            double dist = 0.0;

            for (int i = 0; i < genes.Length; i++)
            {
                dist += Math.Pow((genes[i] - mean[i]), 2);
            }

            return Math.Sqrt(dist);
        }

        //Identify clusterId by finding the index of the smallest distance
        public int identifyClusterId(double[] distances)
        {
            int index = 0;
            double distMin = distances[0];

            for (int i = 0; i < distances.Length; i++)
            {
                if (distances[i] < distMin)
                {
                    distMin = distances[i];
                    index = i;
                }
            }

            return index;
        }

        //Count the number of members in each cluster
        public int[] calcClusterSizes(int numClusters)
        {
            int[] clusterCounts = new int[numClusters];
            for (int i = 0; i < numClusters; i++)
            {
                clusterCounts[i] = 0;
            }

            for (int i = 0; i < chromosomes.Length; i++)
            {
                clusterCounts[chromosomes[i].clusterId]++;
            }

            return clusterCounts;
        }




    }
}
