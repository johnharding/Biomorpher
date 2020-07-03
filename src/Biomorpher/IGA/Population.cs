using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel.Special;
using GalapagosComponents;
using System.Windows.Controls;
using System.Windows;

namespace Biomorpher.IGA
{
    /// <summary>
    /// Biomorpher population containing chromosomes
    /// </summary>
    public class Population
    {

        private BiomorpherComponent owner;

        /// <summary>
        /// List of sliders associated with this population
        /// </summary>
        private List<GH_NumberSlider> popSliders;

        /// <summary>
        /// List of chromosomes associated with this popualation
        /// </summary>
        private List<GalapagosGeneListObject> popGenePools;

        /// <summary>
        /// Chromosome array
        /// </summary>
        public Chromosome[] chromosomes { get; set; }

        /// <summary>
        /// Location on the history canvas for a bezier curve to spawn
        /// </summary>
        public System.Windows.Point HistoryNodeOUT { get; set; }

        /// <summary>
        /// Location on the history canvas for a bezier curve to finish
        /// </summary>
        public System.Windows.Point HistoryNodeIN { get; set; }

        /// <summary>
        /// List containing average performance values for this population
        /// </summary>
        public List<double> Performance_Averages { get; set; }

        /// <summary>
        /// List containing average performance values for this population
        /// </summary>
        public List<double> Performance_Minimums { get; set; }

        /// <summary>
        /// List containing average performance values for this population
        /// </summary>
        public List<double> Performance_Maximums { get; set; }

        /// <summary>
        /// Construct a new population of chromosomes using sliders and genepools
        /// </summary>
        /// <param name="popSize"></param>
        /// <param name="sliders"></param>
        /// <param name="genePools"></param>
        public Population(int popSize, List<GH_NumberSlider> sliders, List<GalapagosGeneListObject> genePools, BiomorpherComponent Owner, int runType)
        {

            owner = Owner;

            chromosomes = new Chromosome[popSize];
            popSliders = new List<GH_NumberSlider>(sliders);
            popGenePools = new List<GalapagosGeneListObject>(genePools);

            for (int i = 0; i < chromosomes.Length; i++)
            {
                chromosomes[i] = new Chromosome(popSliders, popGenePools);
            }

            
            // Random, Initial or Current population
            switch (runType)
            {
                case 0:
                    GenerateRandomPop();
                    break;

                case 1:
                    GenerateCurrentPop();
                    JigglePop(0.001);
                    break;

                default:
                    break;
            }

        }

        /// <summary>
        /// Creates a value copy of the population's chomosomes and genes
        /// </summary>
        /// <param name="pop"></param>
        public Population(Population pop)
        {
            // declare a new set of chromosomes of the same length
            chromosomes = new Chromosome[pop.chromosomes.Length];

            // copy the data, including all chromosome information.
            for (int i = 0; i < chromosomes.Length; i++)
            {
                chromosomes[i] = new Chromosome(pop.chromosomes[i]);
            }

            // clone the slider and genepool pointers
            popSliders = new List<GH_NumberSlider>(pop.popSliders);
            popGenePools = new List<GalapagosGeneListObject>(pop.popGenePools);
            Performance_Averages = new List<double>(pop.Performance_Averages);
            Performance_Minimums = new List<double>(pop.Performance_Minimums);
            Performance_Maximums = new List<double>(pop.Performance_Maximums);

            // clone the owner
            owner = pop.owner;

        }

        /// <summary>
        /// Generates a random population of new chromosomes.
        /// </summary>
        public void GenerateRandomPop()
        {
            for (int i = 0; i < chromosomes.Length; i++)
            {
                chromosomes[i].GenerateRandomGenes();
            }
        }

        /// <summary>
        /// Generates a new popuation based on current design
        /// </summary>
        public void GenerateCurrentPop()
        {
            for (int i = 0; i < chromosomes.Length; i++)
            {
                chromosomes[i].GenerateCurrentGenes();
            }
        }

        /// <summary>
        /// Replaces the population with a new one based on fitness using Roulette Wheel selection
        /// </summary>
        public void SelectPop()
        {
            // First Elitism,  then Roulette Wheel technique as described by Melanie Mitchell, An Introduction to GAs, p.166
            double fitSum;
            double totalFitness = 0.0;
            int counter = 0;

            // Set up a fresh new population (no clone).
            Population newPop = new Population(this.chromosomes.Length, popSliders, popGenePools, owner, -1);

            
            // Copy the elites
            for (int i = 0; i < chromosomes.Length; i++)
            {
                if (chromosomes[i].GetFitness() == 1.0)
                {
                    newPop.chromosomes[counter] = new Chromosome(chromosomes[i]);
                    newPop.chromosomes[counter].isRepresentative = false;
                    newPop.chromosomes[counter].isElite = true;
                    counter++;
                }
            }

            // Now for the roulette wheel selection for the new population
            for (int i = 0; i < chromosomes.Length; i++)
            {
                totalFitness += chromosomes[i].GetFitness();
            }

            // Run from the counter above
            for (int i = counter; i < newPop.chromosomes.Length; i++)
            {
                double weightedRandom = Friends.GetRandomDouble() * totalFitness;
                fitSum = 0.0;

                for (int j = 0; j < chromosomes.Length; j++)
                {
                    fitSum += chromosomes[j].GetFitness();

                    // Stop when we get to the right design
                    // higher fitnesses will result in a greater jump in fitsum, and therefore more likely to be above the random number.
                    if (fitSum > weightedRandom)
                    {
                        // Clone the chromosomes, reset all the representatives
                        // Fitness is cloned at this stage
                        newPop.chromosomes[i] = new Chromosome(chromosomes[j]);
                        newPop.chromosomes[i].isRepresentative = false;
                        newPop.chromosomes[i].isElite = false;

                        break;
                    }
                }
            }

            // Finally, Replace the current popultion of chromosomes with the newPop
            for (int i = 0; i < chromosomes.Length; i++)
            {
                chromosomes[i] = new Chromosome(newPop.chromosomes[i]);
            }
            
        }

        /// <summary>
        /// Gets the fittest design there is (or at least one of them if there are several the same).
        /// </summary>
        /// <returns></returns>
        public Chromosome GetFittest()
        {
            double fittest = 0d;
            int ID = 0;
            for (int i = 0; i < chromosomes.Length; i++)
            {
                if(chromosomes[i].GetFitness()>fittest)
                {
                    fittest = chromosomes[i].GetFitness();
                    ID = i;
                }
            }
            return chromosomes[ID];
        }


        /// <summary>
        /// Set the fitness of each design to be performance based.
        /// </summary>
        public void SetPerformanceBasedFitness(Dictionary<string, FrameworkElement> controls, int pCount)
        {

            RadioButton[] radButtonMin = new RadioButton[pCount];
            RadioButton[] radButtonMax = new RadioButton[pCount];

            double[] minValues = new double[pCount];
            double[] maxValues = new double[pCount];

            int toBeOptimisedCount = 0;


            // For each performance measure...
            for(int p=0; p<pCount; p++)
            {

                // Get the associated min/max radio buttons
                radButtonMin[p] = (RadioButton)controls["RADBUTTONMIN" + p];
                radButtonMax[p] = (RadioButton)controls["RADBUTTONMAX" + p];

                if(radButtonMin[p].IsChecked == true || radButtonMax[p].IsChecked == true)
                {
                    minValues[p] = 9999999999;
                    maxValues[p] = -9999999999;
                    int minID = 0;
                    int maxID = 0;

                    // Find the min and max values before normalisation.
                    for(int j=0; j<chromosomes.Length; j++)
                    {
                        double value = chromosomes[j].GetPerformas()[p];
                        if (value < minValues[p])
                        {
                            minValues[p] = value;
                            minID = j;
                        }
                        if (value > maxValues[p])
                        {
                            maxValues[p] = value;
                            maxID = j;
                        }
                    }

                    // Record the number of criteria to be optimised (for fitness weightings)
                    toBeOptimisedCount++;

                    
                }
            }


            // Now time to update the fitnesses
            for (int p = 0; p < pCount; p++)
            {
                // for the minimising criteria
                if (radButtonMin[p].IsChecked == true || radButtonMax[p].IsChecked == true)
                {
                    // For this performance measure, find the range
                    double range = maxValues[p] - minValues[p];

                    // Find the normalised value for each chromosome
                    for (int j = 0; j < chromosomes.Length; j++)
                    {
                        double value = 0.0;

                        // Get the normalised value, and flip value for minimising criteria
                        // Catch division by zero. This may occur if all the performances are identical.
                        if (range != 0)
                        {
                            value = (chromosomes[j].GetPerformas()[p] - minValues[p]) / range;

                            if (radButtonMin[p].IsChecked == true)
                                value = 1 - value;
                        }
                        
                        else
                        {
                            value = 1.0;
                        }

                        // Adjust depending on number of performance measures to be optimised
                        value /= (double)toBeOptimisedCount;

                        // Set the fitness of this chromosome based on 1-value (minimise) or value (maximise)
                        // If the fitness is 1.0, then it has been selected using a tickbox. 
                        if(chromosomes[j].GetFitness()!=1.0)
                            chromosomes[j].CummulateFitness(value);

                        // Check that we are within bounds
                        if (chromosomes[j].GetFitness() > 1.0 || chromosomes[j].GetFitness() < 0.0)
                            System.Console.Beep();
                    }
                }
            }
        }


        /// <summary>
        /// Sets the average performance values for this population
        /// </summary>
        /// <param name="pCount"></param>
        /// <param name="isClusterRepsOnly"></param>
        public void SetAveragePerformanceValues(int pCount, bool isClusterRepsOnly)
        {
            // Declare a brand new list
            Performance_Averages = new List<double>();
            Performance_Minimums = new List<double>();
            Performance_Maximums = new List<double>();

            for (int p = 0; p < pCount; p++)
            {
                Performance_Averages.Add(0.0);

                double minValue = 9999999999;
                double maxValue = -9999999999;

                for (int i = 0; i < chromosomes.Length; i++)
                {
                    if (isClusterRepsOnly)
                    {
                        if (chromosomes[i].isRepresentative)
                        {
                            Performance_Averages[p] += chromosomes[i].GetPerformas()[p];
                            if (chromosomes[i].GetPerformas()[p] < minValue)
                                minValue = chromosomes[i].GetPerformas()[p];
                            if (chromosomes[i].GetPerformas()[p] > maxValue)
                                maxValue = chromosomes[i].GetPerformas()[p];
                        }
                    }

                    else
                    {
                        Performance_Averages[p] += chromosomes[i].GetPerformas()[p];
                        if (chromosomes[i].GetPerformas()[p] < minValue)
                            minValue = chromosomes[i].GetPerformas()[p];
                        if (chromosomes[i].GetPerformas()[p] > maxValue)
                            maxValue = chromosomes[i].GetPerformas()[p];
                    }
                }

                if (isClusterRepsOnly) Performance_Averages[p] /= 12;
                else Performance_Averages[p] /= chromosomes.Length;

                Performance_Averages[p] = Math.Round(Performance_Averages[p], 3);

                Performance_Minimums.Add(minValue);
                Performance_Maximums.Add(maxValue);

            }
        }

        /// <summary>
        /// Mutates a gene according to a probability
        /// </summary>
        /// <param name="controls"></param>
        /// <param name="probability"></param>
        public void MutatePop(Dictionary<string, FrameworkElement> controls, double probability)
        {

            CheckBox checkBox = (CheckBox)controls["cb_mutateElites"];

            for (int i = 0; i < chromosomes.Length; i++)
            {

                if (!chromosomes[i].isElite)
                {
                    chromosomes[i].Mutate(probability);
                }

                else if ((bool)checkBox.IsChecked)
                {
                    chromosomes[i].Mutate(probability);
                }
            }
        }


        /// <summary>
        /// Single point crossover
        /// </summary>
        /// <param name="probability"></param>
        public void CrossoverPop(double probability)
        {
            if (probability > 0)
            {
                int size = chromosomes[0].GetGenes().Length;

                for (int i = 0; i < chromosomes.Length; i += 2)
                {

                    if (Friends.GetRandomDouble() < probability)
                    {
                        if (!chromosomes[i].isElite && !chromosomes[i + 1].isElite)
                        {
                            int splice = Friends.GetRandomInt(0, size);

                            double[] limbo1 = new double[size];
                            double[] limbo2 = new double[size];

                            chromosomes[i].GetGenes().CopyTo(limbo1, 0);
                            chromosomes[i + 1].GetGenes().CopyTo(limbo2, 0);

                            for (int s = splice; s < size; s++)
                            {
                                limbo1[s] = chromosomes[i + 1].GetGenes()[s];
                                limbo2[s] = chromosomes[i].GetGenes()[s];
                            }

                            chromosomes[i].SetGenes(limbo1);
                            chromosomes[i + 1].SetGenes(limbo2);
                        }
                    }
                }
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
                chromosomes[i].isChecked = false;
                chromosomes[i].isOptimal = false;
                chromosomes[i].isMinimum = false;
                chromosomes[i].isMaximum = false;
            }
        }


        /// <summary>
        /// Jiggles everychromosome slightly by multiplying by Rand(0, t)
        /// </summary>
        /// <param name="t">the amount of jiggle from 0 to 1.0</param>
        public void JigglePop(double t)
        {
            if (t < 0.0) t = 0.0;
            if (t > 1.0) t = 1.0;

            for (int i = 0; i < chromosomes.Length; i++)
            {
                chromosomes[i].JiggleGenes(t);
            }
        }



        /// <summary>
        /// Sets some dummy performance values in case performanceCount changes mid run
        /// </summary>
        public void RepairPerforms(int performanceCount)
        {

            // Fills the null performances with zeros (TODO: A better way? What if we optimised to a minimum?
            for (int i = 0; i < chromosomes.Length; i++)
            {
                int pc;

                try
                {
                    pc = chromosomes[i].GetPerformas().Count;
                }
                catch
                {
                    pc = 0;
                }

                if (pc != performanceCount)
                {
                    List<double> newPerforms = new List<double>();
                    List<string> newCrits = new List<string>();

                    for (int j = 0; j < performanceCount; j++)
                    {
                        newPerforms.Add(0.0);
                        newCrits.Add("pCount changed!");
                    }

                    chromosomes[i].SetPerformas(newPerforms, newCrits);
                }
            }
             
              
        }



        //----------------------------------------------------------------- K-MEANS --------------------------------------------------------------//

        /// <summary>
        /// K-means clustering main method
        /// </summary>
        /// <param name="numClusters"></param>
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



        /// <summary>
        /// Calculate cluster centroids using k-means++
        /// </summary>
        /// <param name="numClusters"></param>
        /// <returns></returns>
        public double[][] calcClusterCentroidsInit(int numClusters)
        {
            int numGenes = chromosomes[0].GetGenes().Length;

            //Initialise array
            double[][] centroidVectorsInit = new double[numClusters][];
            List<int> centroidChromoIndexes = new List<int>();

            // 1. Choose random initial centroid
            //int rndCentroidChromo = Friends.GetRandomInt(0, chromosomes.Length);
            int rndCentroidChromo = 0; // Just choose the first one to keep some continuity
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
                        distances.Add(Friends.calcDistance(chromosomes[i].GetGenes(), chromosomes[centroidIndex].GetGenes()));
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


        /// <summary>
        /// Calculate cluster mean vectors (same length as genes)
        /// </summary>
        /// <param name="numClusters"></param>
        /// <returns></returns>
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

        /// <summary>
        /// c) Update clustering by calculating the distance between the chromosome genes and the mean vectors for each cluster
        /// </summary>
        /// <param name="numClusters"></param>
        /// <param name="clusterMeanVectors"></param>
        /// <returns></returns>
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
                    distances[j] = Friends.calcDistance(chromosomes[i].GetGenes(), clusterMeanVectors[j]);
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

        /// <summary>
        /// d) Update chromosome representatives and cluster distances after k-means clustering
        /// </summary>
        /// <param name="numClusters"></param>
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
                distances[chromosomes[i].clusterId].Add(Friends.calcDistance(chromosomes[i].GetGenes(), clusterMeanVectors[chromosomes[i].clusterId]));
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
                chromosomes[i].distToRepresentative = Friends.calcDistance(chromosomes[i].GetGenes(), chromosomes[closestToMean].GetGenes());
            }

        }

        /// <summary>
        /// Identify clusterId by finding the index of the smallest distance
        /// </summary>
        /// <param name="distances"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Count the number of members in each cluster
        /// </summary>
        /// <param name="numClusters"></param>
        /// <returns></returns>
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
