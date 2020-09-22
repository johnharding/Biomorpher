using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Biomorpher.IGA;
using Grasshopper.Kernel;
using MahApps.Metro.Controls;
using System.ComponentModel;
using Grasshopper.Kernel.Special;
using GalapagosComponents;

namespace Biomorpher
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class BiomorpherWindow : MetroWindow, INotifyPropertyChanged
    {

        #region FIELDS & PROPERTIES

        // Fields
        private bool GO;
        private Population population;
        private List<BioBranch> BioBranches;
        private List<GH_NumberSlider> sliders;
        private List<GalapagosGeneListObject> genePools;
        private int performanceCount;
        private int biobranchID;
        //private static readonly object syncLock = new object();
        private int fontsize;
        private int fontsize2;
        private int margin_w;
        private Color[] rgb_performance;

        /// <summary>
        /// A dictionary, which contains the controls that need to be accessible from other methods after their creation (key to update controls)
        /// </summary>
        private Dictionary<string, FrameworkElement> controls;

        /// <summary>
        /// Progress bar window 
        /// </summary>
        // TODO private ProgressWindow myProgressWindow = new ProgressWindow();

        /// <summary>
        /// Component itself will be passed to this
        /// </summary>
        private BiomorpherComponent owner;
        
        // History fields
        Canvas _historycanvas;
        int _historyY;
        int pngHeight;

        /// <summary>
        /// indicates the particular cluster that is in focus (i.e. with performance values shown) NOT CHROMOSOME ID
        /// </summary>
        private int highlightedCluster;
        public int HighlightedCluster
        {
            get { return highlightedCluster; }
            set
            {
                if (value != highlightedCluster)
                {
                    highlightedCluster = value;
                    OnPropertyChanged("HighlightedCluster");
                }
            }
        }

        /// <summary>
        /// Population size
        /// </summary>
        private int popSize;
        public int PopSize
        {
            get { return popSize; }
            set
            {
                if (value != popSize)
                {
                    popSize = value;
                    OnPropertyChanged("PopSize");
                }
            }
        }

        /// <summary>
        /// Mutatation probability
        /// </summary>
        private double mutateProbability;
        public double MutateProbability
        {
            get { return mutateProbability; }
            set
            {
                if (value != mutateProbability)
                {
                    mutateProbability = value;
                    OnPropertyChanged("MutateProbability");
                }
            }
        }

        /// <summary>
        /// Crossover probability
        /// </summary>
        private double crossoverProbability;
        public double CrossoverProbability
        {
            get { return crossoverProbability; }
            set
            {
                if (value != crossoverProbability)
                {
                    crossoverProbability = value;
                    OnPropertyChanged("CrossoverProbability");
                }
            }
        }

        /// <summary>
        /// Current generation (i.e. twigID)
        /// </summary>
        private int generation;
        public int Generation
        {
            get { return generation; }
            set
            {
                if (value != generation)
                {
                    generation = value;
                    OnPropertyChanged("Generation");
                }
            }
        }

        /// <summary>
        /// Number of parents selected
        /// </summary>
        private int parentCount;
        public int ParentCount
        {
            get { return parentCount; }
            set
            {
                if (value != parentCount)
                {
                    parentCount = value;
                    OnPropertyChanged("ParentCount");
                }
            }
        }

        #endregion

        #region CONSTRUCTOR

        /// <summary>
        /// Main window constructor. Biomorpher component itself is passed here.
        /// </summary>
        /// <param name="Owner"></param>
        public BiomorpherWindow(BiomorpherComponent Owner)
        {
            // Set the component passed here to a field
            owner = Owner;

            // Get sliders and gene pools
            sliders = new List<GH_NumberSlider>();
            genePools = new List<GalapagosGeneListObject>();
            if (!owner.GetSliders(sliders, genePools)) return;

            // Initial Window things
            InitializeComponent();
            Title = "  \u2009  " + Friends.VerionInfo();
            WindowTransitionsEnabled = false;
            ShowIconOnTitleBar = true;

            // Initialise history canvas
            _historycanvas = new Canvas();
            //_historycanvas.Background = Friends.RhinoGrey();
            HistoryCanvas.Children.Add(_historycanvas);
            _historyY = 0;
            pngHeight = 0;

            // Window settings
            Topmost = true;
            PopSize = 48;
            CrossoverProbability = 0.10;
            MutateProbability = 0.01;

            GO = false;
            fontsize = 18;
            fontsize2 = 12;
            margin_w = 20;
            rgb_performance = Friends.PerformanceColours();
            
            // Dictionary of control elements
            controls = new Dictionary<string, FrameworkElement>();

            //Initialise Tab 1 Start settings (i.e. popsize and mutation sliders)
            Tab1_primary_initial();
            Tab1_secondary_settings();

            // Make sure that tab 3 history graphics are clipped to bounds
            Tab3_primary.ClipToBounds = true;

            // Show biomorpher info
            Tab6_primary_permanent();
            
        }

        #endregion

        #region MAIN METHODS

        /// <summary>
        /// Instantiate the population and intialise the window
        /// Runtype 0: Random, 1: Existing 2: Current
        /// </summary>
        public void RunInit(int runType)
        {
            Generation = 0;
            ParentCount = 0;
            performanceCount = 0;
            HighlightedCluster = 0;

            _historycanvas.Children.Clear();
            _historyY = 0;
            pngHeight = 0;

            // 1. Initialise population history. Biobranches are only used for the history and plot bits.
            // Note that we won't actually add a population here yet, but we initialise it at the start.
            BioBranches = new List<BioBranch>();
            biobranchID = 0;
            BioBranches.Add(new BioBranch(-1, 0, 0));

            // 2. Create initial population
            population = new Population(popSize, sliders, genePools, owner, runType);

            // 3. Perform K-means clustering
            population.KMeansClustering(12);

            // 4. Get geometry and performance for each chromosome
            // First time boolean resets the performance count
            GetPhenotypes(true, true);

            // 5. Now get the average performance values (cluster reps only)
            population.SetAveragePerformanceValues(performanceCount, true);

            // 6. Setup tab layouts
            if (!GO)
                Tab12_primary_permanent(1); // 1 indicates tab 1
            Tab1_primary_update();

            if (!GO)
                Tab12_primary_permanent(2); // 2 indicates tab 2 (but same method!)

            Tab2_primary_update();

            if (!GO)
                Tab2_secondary_settings();

            Tab2_updatePerforms();

            if (!GO)
                Tab3_secondary_settings();

            Tab4_secondary_settings();
            Tab5_secondary_settings();

            // Reset plotcanvas if this isn't the first time.
            if (GO)
                Tab4_plotcanvas();

            // 7. Set component outputs
            owner.SetComponentOut(population, BioBranches, performanceCount, biobranchID);

            GO = true;
        }


        /// <summary>
        /// When this gets called (probably via a button being triggered) we advance a generation 
        /// </summary>
        public void Run(bool isPerformanceCriteriaBased)
        {

            // 8. Get fitness values sorted if performance optimisation is selected
            // We put these before adding to history, to ensure performance display is correct.
            if (isPerformanceCriteriaBased)
            {
                GetPhenotypes(false, false); // We have to do this to make sure we have performance for the whole population.
                population.SetPerformanceBasedFitness(controls, performanceCount);
            }
            else
            {
                NumericUpDown myNumericUpDown = (NumericUpDown) controls["myNumericUpDown"];
                myNumericUpDown.Value = 1; 
            }

            // 9. Add old population to history.
            BioBranches[biobranchID].AddTwig(population, performanceCount);

            //////////////////////////////////////////////////////////////////////////

            // 1. Create a new population using fitness values (also resets fitnesses)
            Generation++;
            population.SelectPop();
            //
            population.ResetAllFitness();

            // 2. Crossover and Mutate population using user preferences
            population.CrossoverPop(crossoverProbability);
            population.MutatePop(controls, mutateProbability);

            // Now to display the new population...
            // 2a. Jiggle the population a little to avoid repeats (don't tell anyone)
            population.JigglePop(0.001);

            // 3. Perform K-means clustering
            population.KMeansClustering(12);

            // 4. Get geometry for cluster reps only for the display period
            GetPhenotypes(true, false);

            // 5. Now get the average performance values. Cluster reps only bool here
            population.SetAveragePerformanceValues(performanceCount, true);
            
            // 6. Update display of K-Means, representative meshes history and plot canvas
            Tab1_primary_update();

            Tab2_primary_update();
            Tab2_updatePerforms();

            Tab3_primary_update(isPerformanceCriteriaBased);

            Tab4_plotcanvas();

            // 7. Set component outputs
            owner.SetComponentOut(population, BioBranches, performanceCount, biobranchID);

            // 8. Finally, if performance based
            // Set the current grasshopper instance to the best fitness.
            // As the population has mutated and crossover, we have to do this again.
            if (isPerformanceCriteriaBased)
            {
                population.SetPerformanceBasedFitness(controls, performanceCount);
                SetInstance(population.GetFittest());
                population.ResetAllFitness();
            }

        }


        /// <summary>
        /// Runs when a new biobranch is spawned, when the reinstate button is clicked basically
        /// </summary>
        public void RunNewBranch()
        {
            // Reset generation counter
            Generation = 0;

            // Get geometry for each chromosome
            GetPhenotypes(true, false);

            // 5. Now get the average performance values (cluster reps only)
            population.SetAveragePerformanceValues(performanceCount, true);

            // Update display of K-Means and representative meshes
            Tab1_primary_update();

            Tab2_primary_update();
            Tab2_updatePerforms();

            Tab4_plotcanvas();

        }


        /// <summary>
        /// Gets the phenotype information for the current cluster representatives
        /// </summary>
        public void GetPhenotypes(bool clusterRepsOnly, bool firstTime)
        {
            bool disablePreview;
            CheckBox cb = (CheckBox) controls["cb_disablepreview"];
            disablePreview = (bool) cb.IsChecked;

            if (disablePreview)
                owner.OnPingDocument().PreviewMode = GH_PreviewMode.Disabled;

            // Get geometry for each chromosome in the initial population
            // This is probably the bit that has claimed most of my life getting to work!
            if (clusterRepsOnly)
            {
                for (int i = 0; i < population.chromosomes.Length; i++)
                {

                    if (population.chromosomes[i].isRepresentative)
                    {
                        owner.canvas.Document.Enabled = false;                              // Disable the solver before tweaking sliders
                        owner.SetSliders(population.chromosomes[i], sliders, genePools);    // Change the sliders using gene values
                        owner.canvas.Document.Enabled = true;                               // Enable the solver again
                        owner.ExpireSolution(true);                                         // Now expire the main component and recompute
                        int pCount = owner.GetGeometry(population.chromosomes[i]);          // Get the new geometry for this particular chromosome
                        if (firstTime)                                                      // If first generation, sets the performanceCount value
                            if (pCount > performanceCount)
                                performanceCount = pCount;
                    }
                }
            }

            else
            {
                for (int i = 0; i < population.chromosomes.Length; i++)
                {
                    owner.canvas.Document.Enabled = false;                              // Disable the solver before tweaking sliders
                    owner.SetSliders(population.chromosomes[i], sliders, genePools);    // Change the sliders using gene values
                    owner.canvas.Document.Enabled = true;                               // Enable the solver again
                    owner.ExpireSolution(true);                                         // Now expire the main component and recompute
                    int pCount = owner.GetGeometry(population.chromosomes[i]);          // Get the new geometry for this particular chromosome
                    if(firstTime)                                                       // If first generation, sets the performanceCount value
                        if (pCount > performanceCount)
                            performanceCount = pCount;
                }
            }

            // TODO: Fill up null performance values instead, because this way if you have a null performance value it kills all the others.
            population.RepairPerforms(performanceCount);

            // Turn the preview back on
            if (disablePreview)
                owner.OnPingDocument().PreviewMode = GH_PreviewMode.Shaded;

        }


        /// <summary>
        /// Sets the Grasshopper instance to this chromosome (does not get any data)
        /// </summary>
        /// <param name="chromo"></param>
        public void SetInstance(Chromosome chromo)
        {
            owner.canvas.Document.Enabled = false;
            owner.SetSliders(chromo, sliders, genePools);
            owner.canvas.Document.Enabled = true;
            owner.ExpireSolution(true);
            HighlightedCluster = chromo.clusterId;

            // Update performance tab
            Tab2_updatePerforms();
        }


        /// <summary>
        /// Returns the window controls added to the dictionary
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, FrameworkElement> GetControls()
        {
            return controls;
        }


        /// <summary>
        /// Gets the current population
        /// </summary>
        /// <returns></returns>
        public Population GetPopulation()
        {
            return population;
        }


        //Gets representative meshes
        private List<Mesh>[] getRepresentativeMeshes()
        {
            // EXACTLY 12 SOUPDRAGONS
            List<Mesh>[] soupdragon = new List<Mesh>[12];

            for (int i = 0; i < population.chromosomes.Length; i++)
            {
                if (population.chromosomes[i].isRepresentative)
                {
                    soupdragon[population.chromosomes[i].clusterId] = population.chromosomes[i].phenoMesh;
                }
                    
            }

            // List is now ordered according to cluster IDs
            return soupdragon;
        }

        //Gets representative polys
        private List<PolylineCurve>[] getRepresentativePolys()
        {
            // EXACTLY 12 SOUPDRAGONS
            List<PolylineCurve>[] soupdragon = new List<PolylineCurve>[12];

            for (int i = 0; i < population.chromosomes.Length; i++)
            {
                if (population.chromosomes[i].isRepresentative)
                {
                    soupdragon[population.chromosomes[i].clusterId] = population.chromosomes[i].phenoPoly;
                }

            }

            // List is now ordered according to cluster IDs
            return soupdragon;
        }

        //Gets *representative* performance values
        private double[][] getRepresentativePerformas(Population thisPop)
        {
            double[][] performas = new double[12][];

            Chromosome[] chromosomes = thisPop.chromosomes;
            for (int i = 0; i < chromosomes.Length; i++)
            {
                if (chromosomes[i].isRepresentative)
                {
                    int performasCount = chromosomes[i].GetPerformas().Count;

                    performas[chromosomes[i].clusterId] = new double[performasCount];
                    for (int j = 0; j < performasCount; j++)
                    {
                        performas[chromosomes[i].clusterId][j] = chromosomes[i].GetPerformas()[j];
                    }

                }
                    
            }
            return performas;
        }



        //Gets *representative* criteria names
        private string[][] getRepresentativeCriteria(Population thisPop)
        {
            string[][] crit = new string[12][];

            Chromosome[] chromosomes = thisPop.chromosomes;
            for (int i = 0; i < chromosomes.Length; i++)
            {
                if (chromosomes[i].isRepresentative)
                {
                    int critCount = chromosomes[i].GetCriteria().Count;

                    crit[chromosomes[i].clusterId] = new string[critCount];
                    for (int j = 0; j < critCount; j++)
                    {
                        crit[chromosomes[i].clusterId][j] = chromosomes[i].GetCriteria()[j];
                    }
                }

            }
            return crit;
        }

        #endregion

        #region UI TAB 1 (POPULATION)

        /// <summary>
        /// Update display of K-Means clustering
        /// </summary>
        public void Tab1_primary_update()
        {
            //Run through the 12 designs
            for (int i = 0; i < 12; i++)
            {
                //Create canvas
                Canvas canvas = CreateKMeansVisualisation(i);

                //The name of the control to add the canvas to
                string dp_name = "dp_tab1_" + i;

                //Get this control from the dictionary
                DockPanel dp = (DockPanel)controls[dp_name];

                //If there already is a canvas in the dockpanel then remove it
                if (dp.Children.Count > 1)
                {
                    dp.Children.RemoveAt(dp.Children.Count - 1);
                }

                //Add the new canvas to the dockpanel
                dp.Children.Add(canvas);
                dp.ClipToBounds = true;
            }
        }


        /// <summary>
        /// Create canvas to visualise K-Means clustering for a specific ID
        /// </summary>
        /// <param name="clusterIndex"></param>
        /// <returns></returns>
        public Canvas CreateKMeansVisualisation(int clusterIndex)
        {
            int width = 150;
            int diameter = 8;

            Canvas canvas = new Canvas();
            string name = "canvas" + clusterIndex;
            canvas.Name = name;
            canvas.Width = width;
            canvas.Height = width;

            //Add chromosome dots
            // Include the cluster representative here to avoid division by zero
            List<double> distances = new List<double>();
            for (int i = 0; i < population.chromosomes.Length; i++)
            {
                if (population.chromosomes[i].clusterId == clusterIndex)
                {
                    double d = population.chromosomes[i].distToRepresentative;
                    distances.Add(d);
                }
            }

            //Map distances to width domain
            double distMax = distances.Max();

            List<double> distancesMapped = new List<double>();

            for (int i = 0; i < distances.Count; i++)
            {
                // Now don't include the representative
                if (distances[i] != 0.0)
                {
                    double d_normal = (distances[i] / distMax);
                    double d_map = (d_normal * (((width) / 2.0)-5))+5;
                    distancesMapped.Add(d_map);
                }
            }

            //Create shapes and add to canvas
            for (int i = 0; i < distancesMapped.Count; i++)
            {
                //Circles
                System.Windows.Shapes.Ellipse circle = new System.Windows.Shapes.Ellipse();
                circle.Height = diameter;
                circle.Width = diameter;
                circle.Fill = Brushes.DarkSlateGray;

                //Calculate angle
                double angle = (2 * Math.PI * i) / distancesMapped.Count;
                double xCoord = distancesMapped[i] * Math.Cos(angle);
                double yCoord = distancesMapped[i] * Math.Sin(angle);

                //Lines
                System.Windows.Shapes.Line ln = new System.Windows.Shapes.Line();
                ln.StrokeThickness = 1;
                ln.Stroke = Brushes.DarkSlateGray;
                ln.X1 = width / 2.0 + 0;
                ln.Y1 = (width / 2.0) -12;
                ln.X2 = (width / 2.0) + xCoord + 0;
                ln.Y2 = (width / 2.0) + yCoord -12;
                canvas.Children.Add(ln);

                //drawing order
                Canvas.SetLeft(circle, (width / 2.0) + xCoord - (diameter / 2.0) + 0);
                Canvas.SetTop(circle, (width / 2.0) + yCoord - (diameter / 2.0) -12);
                canvas.Children.Add(circle);
            }

            // centre circle
            System.Windows.Shapes.Ellipse circle2 = new System.Windows.Shapes.Ellipse();
            circle2.Height = diameter;
            circle2.Width = diameter;
            circle2.Fill = Brushes.DarkSlateGray;
            circle2.Stroke = Brushes.DarkSlateGray;
            circle2.StrokeThickness = 1;
            Canvas.SetLeft(circle2, (width / 2.0) - (diameter / 2.0)+0);
            Canvas.SetTop(circle2, (width / 2.0) - (diameter / 2.0) -12);
            canvas.Children.Add(circle2);

            return canvas;
        }


        /// <summary>
        /// An initial background for tab 1.
        /// </summary>
        public void Tab1_primary_initial()
        { 
            BitmapImage b = new BitmapImage();
            b.BeginInit();
            b.UriSource = new Uri(@"Resources\BioIcon3_240.png", UriKind.Relative);
            b.EndInit();
            Image myImage = new Image();
            myImage.Source = b;
            myImage.Width = 120;
            myImage.Height = 120;

            Grid gp = new Grid();
            Grid.SetColumn(gp, 1);
            Grid.SetRow(gp, 1);
            gp.Children.Add(myImage);

            Tab1_primary.Child = gp;
        }


        /// <summary>
        /// Create settings panel for Tab 1
        /// </summary>
        public void Tab1_secondary_settings()
        {
            //Container for all the controls
            StackPanel sp = new StackPanel();

            //Header
            Border border_head = new Border();
            border_head.Margin = new Thickness(margin_w, 0, margin_w, 0);
            Label label_head = new Label();
            label_head.FontSize = fontsize;
            label_head.Content = "Initial Settings";
            border_head.Child = label_head;
            sp.Children.Add(border_head);

            // Settings description
            Border _border = new Border();
            _border.Margin = new Thickness(margin_w, 0, margin_w, 0);
            TextBlock _txt = new TextBlock();
            _txt.TextWrapping = TextWrapping.Wrap;
            _txt.FontSize = fontsize2;
            _txt.Inlines.Add("Choose the initial population size and mutation rate. During evolution, mutation rate can be altered whereas population size cannot.");
            Label _label = new Label();
            _label.Content = _txt;
            _border.Child = _label;
            sp.Children.Add(_border);

            // Initial population can be random, from a previous run or based on the current parameter state.

            // Create sliders with labels
            Border border_popSize = new Border();
            border_popSize.Margin = new Thickness(margin_w, 10, margin_w, 0);
            DockPanel dp_popSize = CreateSlider("Population size", "s_tab1_popSize", 12, 200, PopSize, true, new RoutedPropertyChangedEventHandler<double>(Tab1_popSize_ValueChanged));
            border_popSize.Child = dp_popSize;
            sp.Children.Add(border_popSize);

            Border border_crossover = new Border();
            border_crossover.Margin = new Thickness(margin_w, 0, margin_w, 0);
            DockPanel dp_crossover = CreateSlider("Crossover rate", "s_tab1_crossover", 0.00, 1.00, CrossoverProbability, false, new RoutedPropertyChangedEventHandler<double>(Tab1_crossover_ValueChanged));
            border_crossover.Child = dp_crossover;
            sp.Children.Add(border_crossover);

            Border border_mutation = new Border();
            border_mutation.Margin = new Thickness(margin_w, 0, margin_w, 0);
            DockPanel dp_mutation = CreateSlider("Mutation rate", "s_tab1_mutation", 0.00, 1.00, MutateProbability, false, new RoutedPropertyChangedEventHandler<double>(Tab1_mutation_ValueChanged));
            border_mutation.Child = dp_mutation;
            sp.Children.Add(border_mutation);

            // Now for the three buttons
            DockPanel dp_buttons = new DockPanel();
            dp_buttons.LastChildFill = false; 

            //GO button
            DockPanel dock_go = new DockPanel();
            Button button_go = CreateButton("b_tab1_Go", "Go", 125, new RoutedEventHandler(Tab1_Go_Click));
            button_go.ToolTip = "Uses a random initial population";
            DockPanel.SetDock(button_go, Dock.Top);
            Label label_go = new Label();
            label_go.Content = "Random";
            label_go.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center;
            DockPanel.SetDock(label_go, Dock.Bottom);
            dock_go.Children.Add(button_go);
            dock_go.Children.Add(label_go);
            DockPanel.SetDock(dock_go, Dock.Left);
            dp_buttons.Children.Add(dock_go);

            // Add a little gap between buttons
            Border littlegap = new Border();
            littlegap.Width = 4;
            DockPanel.SetDock(littlegap, Dock.Left);
            dp_buttons.Children.Add(littlegap);

            //GO2 button
            DockPanel dock_go2 = new DockPanel();
            Button button_go2 = CreateButton("b_tab1_Go2", "Go", 125, new RoutedEventHandler(Tab1_Go2_Click));
            button_go2.ToolTip = "Creates an initial population from the current parameter state";
            DockPanel.SetDock(button_go2, Dock.Top);
            Label label_go2 = new Label();
            label_go2.Content = "Current";
            label_go2.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center;
            DockPanel.SetDock(label_go2, Dock.Bottom);
            dock_go2.Children.Add(button_go2);
            dock_go2.Children.Add(label_go2);
            DockPanel.SetDock(dock_go2, Dock.Left);
            dp_buttons.Children.Add(dock_go2);

            Border border_buttons = new Border();
            border_buttons.Margin = new Thickness(margin_w, 20, margin_w, 0);
            border_buttons.Child = dp_buttons;
            sp.Children.Add(border_buttons);

            // K-means text
            Border border_kmeans = new Border();
            border_kmeans.Margin = new Thickness(margin_w, 20, margin_w, 0);
            Label label_kmeans = new Label();
            label_kmeans.FontSize = fontsize;
            label_kmeans.Content = "K-means Clusters";
            border_kmeans.Child = label_kmeans;
            sp.Children.Add(border_kmeans);
            
            // K-means description
            Border border = new Border();
            border.Margin = new Thickness(margin_w, 0, margin_w, 0);
            TextBlock txt = new TextBlock();
            txt.TextWrapping = TextWrapping.Wrap;
            txt.FontSize = fontsize2;
            txt.Inlines.Add("Designs are clustered into 12 groups based on parameter similarity. Click on the 'design' tab to see representative closest to each group centroid.");
            Label label = new Label();
            label.Content = txt;
            border.Child = label;
            sp.Children.Add(border);

            // Now for the ShowAll12 designs checkbox
            Border border_showall12 = new Border();
            border_showall12.Margin = new Thickness(margin_w, 20, margin_w, 0);
            DockPanel dp_showall12 = new DockPanel();
            Label label_showall12 = new Label();
            label_showall12.HorizontalContentAlignment = HorizontalAlignment.Left;
            label_showall12.Content = "Show all 12 cluster centroids in history";
            DockPanel.SetDock(label_showall12, Dock.Left);
            dp_showall12.Children.Add(label_showall12);

            CheckBox cb_showall12 = new CheckBox();
            cb_showall12.Name = "cb_showall12";
            cb_showall12.IsChecked = false;
            cb_showall12.Background = Friends.AlphaShade();
            cb_showall12.BorderBrush = Brushes.Black;
            cb_showall12.BorderThickness = new Thickness(1);
            controls.Add(cb_showall12.Name, cb_showall12);
            cb_showall12.HorizontalAlignment = HorizontalAlignment.Right;
            DockPanel.SetDock(cb_showall12, Dock.Right);
            dp_showall12.Children.Add(cb_showall12);
            border_showall12.Child = dp_showall12;
            sp.Children.Add(border_showall12);

            // Now for the disable preview checkbox
            Border border_disablepreview = new Border();
            border_disablepreview.Padding = new Thickness(0);
            border_disablepreview.BorderThickness = new Thickness(margin_w, 0, margin_w, 0);
            DockPanel dp_disablepreview = new DockPanel();
            Label label_disablepreview = new Label();
            label_disablepreview.HorizontalContentAlignment = HorizontalAlignment.Left;
            label_disablepreview.Content = "Disable Grasshopper preview (faster)";
            DockPanel.SetDock(label_disablepreview, Dock.Left);
            dp_disablepreview.Children.Add(label_disablepreview);
           
            CheckBox cb_disablepreview = new CheckBox();
            cb_disablepreview.Name = "cb_disablepreview";
            cb_disablepreview.IsChecked = false;
            cb_disablepreview.Background = Friends.AlphaShade();
            cb_disablepreview.BorderBrush = Brushes.Black;
            cb_disablepreview.BorderThickness = new Thickness(1);
            controls.Add(cb_disablepreview.Name, cb_disablepreview);
            cb_disablepreview.HorizontalAlignment = HorizontalAlignment.Right;
            DockPanel.SetDock(cb_disablepreview, Dock.Right);
            dp_disablepreview.Children.Add(cb_disablepreview);
            border_disablepreview.Child = dp_disablepreview;
            sp.Children.Add(border_disablepreview);

            // Now for the mutate elites checkbox
            Border border_mutateElites = new Border();
            border_mutateElites.Padding = new Thickness(0);
            border_mutateElites.BorderThickness = new Thickness(margin_w, 0, margin_w, 0);
            DockPanel dp_mutateElites = new DockPanel();
            Label label_mutateElites = new Label();
            label_mutateElites.HorizontalContentAlignment = HorizontalAlignment.Left;
            label_mutateElites.Content = "Mutate elite (fittest) designs";
            DockPanel.SetDock(label_mutateElites, Dock.Left);
            dp_mutateElites.Children.Add(label_mutateElites);

            CheckBox cb_mutateElites = new CheckBox();
            cb_mutateElites.Name = "cb_mutateElites";
            cb_mutateElites.IsChecked = false;
            cb_mutateElites.Background = Friends.AlphaShade();
            cb_mutateElites.BorderBrush = Brushes.Black;
            cb_mutateElites.BorderThickness = new Thickness(1);
            controls.Add(cb_mutateElites.Name, cb_mutateElites);
            cb_mutateElites.HorizontalAlignment = HorizontalAlignment.Right;
            DockPanel.SetDock(cb_mutateElites, Dock.Right);
            dp_mutateElites.Children.Add(cb_mutateElites);
            border_mutateElites.Child = dp_mutateElites;
            sp.Children.Add(border_mutateElites);

            // Create backgroundSlider
            Border border_backgroundSlider = new Border();
            border_backgroundSlider.Margin = new Thickness(margin_w, 10, margin_w, 0);
            DockPanel dp_backgroundSlider = CreateSlider("Brightness", "backgroundSlider", 0.00, 1.00, 0.00, false, new RoutedPropertyChangedEventHandler<double>(backgroundSlider_ValueChanged));
            border_backgroundSlider.Child = dp_backgroundSlider;
            sp.Children.Add(border_backgroundSlider);


            //Add the stackpanel to the secondary area of Tab 0
            Tab1_secondary.Child = sp;
        }

        #endregion

        #region UI TAB 2 (DESIGNS)

        /// <summary>
        /// Create permanent grid layout for Tab 1 and Tab 2 (if Tab 2 is specified then checkboxes are added to the top right corners of the grid as well)
        /// </summary>
        /// <param name="tabIndex"></param>
        public void Tab12_primary_permanent(int tabIndex)
        {
            // Create grid 3x4 layout
            int rowCount = 3;
            int columnCount = 4;
            int gridCount = rowCount * columnCount;
            Grid grid = CreateGrid(rowCount, columnCount, Tab2_primary.Width, Tab2_primary.Height);
            
            // For each grid cell: create border with padding, a dock panel and add a checkbox
            for (int i = 0; i < gridCount; i++)
            {
                // Outer border
                Border oBorder = new Border();
                oBorder.Padding = new Thickness(4);

                // Border
                Border border = new Border();
                border.BorderBrush = Brushes.Gray;
                border.BorderThickness = new Thickness(1);
                border.Padding = new Thickness(2);

                // Master Dock panel
                DockPanel dp = new DockPanel();
                string dp_name = "dp_tab" + tabIndex + "_" + i;
                dp.Name = dp_name;

                // Sub Dock panel (this the top band that contains number, perfs & checkbox)
                DockPanel dp_sub = new DockPanel();
                string dp_sub_name = "dp_sub_tab" + tabIndex + "_" + i;
                dp_sub.Name = dp_sub_name;

                // Label
                Label l = new Label();
                int index = i;
                l.Content = " " + index.ToString();
                l.FontSize = fontsize2;
                l.Foreground = Brushes.DarkSlateGray;
                l.HorizontalAlignment = HorizontalAlignment.Left;
                DockPanel.SetDock(l, Dock.Left);
                dp_sub.Children.Add(l);

                if (tabIndex == 2)
                {
                    // Create checkbox with an event handler
                    string cb_name = "cb_tab2_" + i;
                    CheckBox cb = CreateCheckBox(cb_name, new RoutedEventHandler(Tab2_SelectParents_Check), i);
                    cb.Background = Friends.AlphaShade();
                    
                    cb.BorderBrush = Brushes.Black;
                    cb.BorderThickness = new Thickness(1);
                    cb.HorizontalAlignment = HorizontalAlignment.Right;
                    DockPanel.SetDock(cb, Dock.Right);
                    dp_sub.Children.Add(cb);
                }

                DockPanel.SetDock(dp_sub, Dock.Top);
                dp.Children.Add(dp_sub);

                // Add dockpanel to controls dictionary in order to access and update content without recreating the entire grid with checkboxes
                controls.Add(dp_name, dp);
                controls.Add(dp_sub_name, dp_sub);

                // Set the dockpanel as the child of the border element
                border.Child = dp;
                oBorder.Child = border;

                // Add the border to the grid
                Grid.SetRow(oBorder, (int)(i / columnCount));
                Grid.SetColumn(oBorder, i % columnCount);
                grid.Children.Add(oBorder);
            }


            //Add the grid to the primary area of Tab 1 or 2
            if (tabIndex == 1)
            {
                Tab1_primary.Child = grid;
            }
            else
            {
                Tab2_primary.Child = grid;
            }
        }


        /// <summary>
        /// Updates the display of the representative meshes and their performance values
        /// </summary>
        public void Tab2_primary_update()
        {
            List<Mesh>[] meshes = getRepresentativeMeshes();
            List<PolylineCurve>[] polys = getRepresentativePolys();
            List<Canvas> performanceCanvas = CreatePerformanceCanvasAll();

            //Run through the design windows and add a viewport3d control and performance display to each
            for (int i = 0; i < 12; i++)
            {
                // The name of the control to add to
                string dp_name = "dp_tab2_" + i;
                string dp_sub_name = "dp_sub_tab2_" + i;

                // Get this control from the dictionary
                DockPanel dp = (DockPanel)controls[dp_name];
                DockPanel dp_sub = (DockPanel)controls[dp_sub_name];

                // Viewport update
                if (dp.Children.Count > 1)
                {
                    dp.Children.RemoveAt(dp.Children.Count - 1);
                }

                Viewport3d vp3d = new Viewport3d(meshes[i], polys[i], i, this, true);
                dp.Children.Add(vp3d);

                // Performance display update
                if (dp_sub.Children.Count > 2)
                {
                    dp_sub.Children.RemoveAt(dp_sub.Children.Count - 1);
                }

                Canvas c = performanceCanvas[i];
                dp_sub.Children.Add(c);
            }


            // Match the cameras
            // TODO: Is there a better way to do this?
            for (int i = 0; i < 12; i++)
            {
                // The name of the control to add to
                string dp_name = "dp_tab2_" + i;

                // Get this control from the dictionary
                DockPanel dp = (DockPanel)controls[dp_name];
                Viewport3d vp3d = (Viewport3d)dp.Children[1];
                vp3d.MatchCamera();
            }
        }


        /// <summary>
        /// Create performance canvas for all representative designs
        /// </summary>
        /// <returns></returns>
        private List<Canvas> CreatePerformanceCanvasAll()
        {
            double alfaMin = 0.2;
            double alfaMax = 1.0;

            double[][] performas = getRepresentativePerformas(population);
            int performasCount = performas[0].Length;

            //Extract min/max values of each performance measure
            List<double> minValues = new List<double>();
            List<double> maxValues = new List<double>();

            for(int i=0; i< performasCount; i++)
            {
                double min = performas[0][i];
                double max = performas[0][i];

                for (int j = 0; j < 12; j++)
                {
                    if (performas[j][i] < min)
                    {
                        min = performas[j][i];
                    }

                    if (performas[j][i] > max)
                    {
                        max = performas[j][i];
                    }
                }

                minValues.Add(min);
                maxValues.Add(max);
            }


            //Now create canvas for each representative design
            List<Canvas> performanceCanvas = new List<Canvas>();

            for(int i=0; i<12; i++)
            {
                List<double> tmaps = new List<double>();
                List<bool> isExtrema = new List<bool>();

                for (int j = 0; j < performasCount; j++)
                {
                    //map performance value to alpha value
                    double range = maxValues[j] - minValues[j];

                    double t_normal = 1.0;
                    if (range != 0.0)
                    {
                        t_normal = (performas[i][j] - minValues[j]) / range;
                    }

                    double t_map = alfaMin + (t_normal * (alfaMax - alfaMin));

                    tmaps.Add(t_map);

                    //detect if the performance is an extrema value
                    if(performas[i][j] == minValues[j] || performas[i][j] == maxValues[j])
                    {
                        isExtrema.Add(true);
                    }
                    else
                    {
                        isExtrema.Add(false);
                    }
                }

                //Create canvas
                Canvas canvas = CreatePerformanceCanvas(tmaps, isExtrema);
                performanceCanvas.Add(canvas);
            }

            return performanceCanvas;
        }


        /// <summary>
        /// Create performas canvas with coloured circles for one representative design
        /// </summary>
        /// <param name="tmaps"></param>
        /// <param name="isExtrema"></param>
        /// <returns></returns>
        private Canvas CreatePerformanceCanvas(List<double> tmaps, List<bool> isExtrema)
        {
            int numCircles = tmaps.Count;
            int dOuter = 16; // the diameter of the extrema circle
            int dInner = 12; // the diameter of the inner circle
            int dGap = 3; // gap between each performance measure 
            int topOffset = 6;

            Canvas canvas = new Canvas();

            //Add circles
            for(int i=0; i<numCircles; i++)
            {
                int distFromLeft = 16 + ((dOuter + dGap) * i);

                //Outer circle
                System.Windows.Shapes.Ellipse extremaCircle = new System.Windows.Shapes.Ellipse();
                extremaCircle.Height = dOuter;
                extremaCircle.Width = dOuter;
                extremaCircle.StrokeThickness = 0.5;
                extremaCircle.Stroke = Brushes.Gray;

                Canvas.SetLeft(extremaCircle, distFromLeft);
                Canvas.SetTop(extremaCircle, topOffset);
                canvas.Children.Add(extremaCircle);
                

                //Performance circle
                System.Windows.Shapes.Ellipse performanceCircle = new System.Windows.Shapes.Ellipse();
                double performanceDiameter = dInner * tmaps[i];
                int iDiameter = (int)(performanceDiameter * 0.5);
                performanceDiameter = iDiameter * 2; // even number for pixel orientation
                performanceCircle.Height = performanceDiameter;
                performanceCircle.Width = performanceDiameter;
                SolidColorBrush brush = new SolidColorBrush();
                brush.Color = rgb_performance[i % 8];
                performanceCircle.Fill = brush;
                performanceCircle.ToolTip = performanceDiameter;
                Canvas.SetLeft(performanceCircle, distFromLeft + (dOuter - performanceDiameter)* 0.5);
                Canvas.SetTop(performanceCircle, topOffset + (dOuter - performanceDiameter) * 0.5);
                canvas.Children.Add(performanceCircle);

            }

            return canvas;
        }


        /// <summary>
        /// Create settings panel for Tab 2
        /// </summary>
        public void Tab2_secondary_settings()
        {
            StackPanel sp = new StackPanel();
            controls.Add("SP", sp);

            //Header
            Border border_head = new Border();
            border_head.Margin = new Thickness(margin_w, 0, margin_w, 0);

            Label label_head = new Label();
            label_head.FontSize = fontsize;
            label_head.SetBinding(ContentProperty, new Binding("Generation"));
            label_head.DataContext = this;
            label_head.ContentStringFormat = "Generation {0}";

            border_head.Child = label_head;
            sp.Children.Add(border_head);


            //Selection description
            Border border_sel = new Border();
            border_sel.Margin = new Thickness(margin_w, 0, margin_w, 0);

            TextBlock txt_sel = new TextBlock();
            txt_sel.TextWrapping = TextWrapping.Wrap;
            txt_sel.FontSize = fontsize2;
            txt_sel.Inlines.Add("Select parents whose genes will be used to create the next design generation via the checkboxes");

            Label label_sel = new Label();
            label_sel.Content = txt_sel;

            border_sel.Child = label_sel;
            sp.Children.Add(border_sel);

            DockPanel dp_buttons = new DockPanel();
            dp_buttons.LastChildFill = false;

            Border border_buttons = new Border();
            border_buttons.Margin = new Thickness(margin_w, 20, margin_w, 0);


            //Evolve button
            Button button_evo = CreateButton("b_tab2_Evolve", "Evolve", 125, new RoutedEventHandler(Tab2_Evolve_Click));
            button_evo.ToolTip = "Advance to next generation(s)";
            DockPanel.SetDock(button_evo, Dock.Left);
            dp_buttons.Children.Add(button_evo);

            // Bit of a space
            Border spacer = new Border();
            spacer.Width = 4;
            dp_buttons.Children.Add(spacer);

            // Up down text
            Label label_numeric = new Label();
            label_numeric.Content = "Iterations:";
            label_numeric.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Left;
            dp_buttons.Children.Add(label_numeric);

            // Numeric upDown
            NumericUpDown myNumericUpDown = new NumericUpDown
            {
                Width = 62,
                ToolTip = "Increases the number of generations calculated (performance based only).",
                UpDownButtonsWidth = 16,
                Value = 1,
                Minimum = 1,
                Maximum = 49,
                Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                BorderBrush = Brushes.Black,
            };

            controls.Add("myNumericUpDown", myNumericUpDown);
            DockPanel.SetDock(myNumericUpDown, Dock.Right);
            dp_buttons.Children.Add(myNumericUpDown);

            // Final events
            border_buttons.Child = dp_buttons;
            sp.Children.Add(border_buttons);

            // Header 2
            Border border_data = new Border();
            border_data.Margin = new Thickness(margin_w, 24, margin_w, 0);
            Label label_data = new Label();
            label_data.FontSize = fontsize;
            label_data.Content = "Performance Optimisation";
            border_data.Child = label_data;
            sp.Children.Add(border_data);

            // Doubleclick description
            Border border_dcl = new Border();
            border_dcl.Margin = new Thickness(margin_w, 0, margin_w, 0);
            TextBlock txt_dcl = new TextBlock();
            txt_dcl.TextWrapping = TextWrapping.Wrap;
            txt_dcl.FontSize = fontsize2;
            txt_dcl.Inlines.Add("Double click a design to diplay its Rhino/Grasshopper instance and review performance below. ");
            txt_dcl.Inlines.Add("\n\nUse the radio buttons below to optimise for different criteria (uses the whole population). Artificial selection can also be used concurrently).");

            Label label_dcl = new Label();
            label_dcl.Content = txt_dcl;
            border_dcl.Child = label_dcl;
            sp.Children.Add(border_dcl);


            // Display the highlighted design label (i.e. "Design 0"). 
            // Add to controls so it can be updated using tab2_updateperforms (see below)
            Border border_cluster = new Border();
            controls.Add("CLUSTER", border_cluster);
            sp.Children.Add(border_cluster);

            // Now for the soupdragons...
            StackPanel soupdragonMaster = new StackPanel();
            controls.Add("SOUPDRAGONMASTER", soupdragonMaster);
            sp.Children.Add(soupdragonMaster);

            // Add the stackpanels to the secondary area of Tab 2
            Tab2_secondary.Child = sp;

        }


        /// <summary>
        /// Updates the list of performance 'borders' on the right hand side of the main window (tab 2)
        /// Called again when a design is double clicked
        /// </summary>
        private void Tab2_updatePerforms()
        {
            // Design info
            Border border_clus = (Border)controls["CLUSTER"];
            border_clus.Margin = new Thickness(margin_w, 16, margin_w, 4);

            // Add root Grid
            Grid myGrid = new Grid();
            ColumnDefinition myColDef1 = new ColumnDefinition();
            ColumnDefinition myColDef2 = new ColumnDefinition();
            ColumnDefinition myColDef3 = new ColumnDefinition();
            ColumnDefinition myColDef4 = new ColumnDefinition();

            myGrid.Height = 56;

            myColDef2.Width = new GridLength(24);
            myColDef3.Width = new GridLength(24);
            myColDef4.Width = new GridLength(23);

            myGrid.ColumnDefinitions.Add(myColDef1);
            myGrid.ColumnDefinitions.Add(myColDef2);
            myGrid.ColumnDefinitions.Add(myColDef3);
            myGrid.ColumnDefinitions.Add(myColDef4);

            if (performanceCount > 0)
            {
                Label label_1 = new Label();
                Label label_2 = new Label();
                Label label_3 = new Label();

                label_1.Content = "none";
                label_2.Content = "minimise";
                label_3.Content = "maximise";

                label_1.FontSize = 11;
                label_2.FontSize = 11;
                label_3.FontSize = 11;

                label_1.LayoutTransform = new RotateTransform(-90);
                label_2.LayoutTransform = new RotateTransform(-90);
                label_3.LayoutTransform = new RotateTransform(-90);

                Grid.SetColumn(label_1, 1);
                myGrid.Children.Add(label_1);
                Grid.SetColumn(label_2, 2);
                myGrid.Children.Add(label_2);
                Grid.SetColumn(label_3, 3);
                myGrid.Children.Add(label_3);
            }

            Label label_gen = new Label();
            label_gen.Content = "Design " + HighlightedCluster;
            label_gen.FontSize = fontsize;
            label_gen.VerticalAlignment = VerticalAlignment.Bottom;
            Grid.SetColumn(label_gen, 0);
            myGrid.Children.Add(label_gen);

            border_clus.Child = myGrid;

            // 8 maximum performance count
            // Try to get rid of anything we don't need.
            List<bool?> radmincheckList = new List<bool?>();
            List<bool?> radmaxcheckList = new List<bool?>();
            List<bool?> radnoncheckList = new List<bool?>();

            // Basically we need to get all the checked data before we remove the radio buttons.
            // What a faff, just to enable the user to change the performance count when restarting!
            for (int i = 0; i < 8; i++)
            {
                controls.Remove("PERFBORDER" + i);

                if (controls.ContainsKey("RADBUTTONNON" + i))
                {
                    RadioButton radnon = (RadioButton)controls["RADBUTTONNON" + i];
                    radnoncheckList.Add(radnon.IsChecked);
                    controls.Remove("RADBUTTONNON" + i);
                }

                if (controls.ContainsKey("RADBUTTONMIN" + i))
                {
                    RadioButton radmin = (RadioButton)controls["RADBUTTONMIN" + i];
                    radmincheckList.Add(radmin.IsChecked);
                    controls.Remove("RADBUTTONMIN" + i);
                }

                if (controls.ContainsKey("RADBUTTONMAX" + i))
                {
                    RadioButton radmax = (RadioButton)controls["RADBUTTONMAX" + i];
                    radmaxcheckList.Add(radmax.IsChecked);
                    controls.Remove("RADBUTTONMAX" + i);
                }
            }

            // Get the soupdragonmaster that has already been added to sp
            StackPanel soupSP = (StackPanel)controls["SOUPDRAGONMASTER"];
            soupSP.Children.Clear();
            StackPanel soupdragon1 = new StackPanel();
            StackPanel soupdragon2 = new StackPanel();

            // Add the performance borders to soupdragon 1
            // Note that these performance borders are for ONE design.
            List<Border> myBorders = new List<Border>();
            for (int i = 0; i < performanceCount; i++)
            {
                Border border_p = new Border();
                controls.Add("PERFBORDER" + i, border_p);
                soupdragon1.Children.Add(border_p);
                myBorders.Add(border_p);
            }

            // Add the radiobuttons
            for (int i = 0; i < performanceCount; i++)
            {
                DockPanel radButtonPanel = new DockPanel();

                RadioButton radButtonNon = new RadioButton();
                RadioButton radButtonMin = new RadioButton();
                RadioButton radButtonMax = new RadioButton();

                radButtonNon.IsChecked = true;

                if (i < radnoncheckList.Count)
                    radButtonNon.IsChecked = radnoncheckList[i];
                if (i < radmincheckList.Count)
                    radButtonMin.IsChecked = radmincheckList[i];
                if (i < radmaxcheckList.Count)
                    radButtonMax.IsChecked = radmaxcheckList[i];

                radButtonNon.ToolTip = "no optimisation";
                radButtonMin.ToolTip = "minimise";
                radButtonMax.ToolTip = "maximise";

                radButtonNon.Background = Friends.AlphaShade();
                radButtonMin.Background = Friends.AlphaShade();
                radButtonMax.Background = Friends.AlphaShade();

                radButtonNon.BorderBrush = Brushes.Black;
                radButtonMin.BorderBrush = Brushes.Black;
                radButtonMax.BorderBrush = Brushes.Black;

                controls.Add("RADBUTTONNON" + i, radButtonNon);
                controls.Add("RADBUTTONMIN" + i, radButtonMin);
                controls.Add("RADBUTTONMAX" + i, radButtonMax);

                radButtonPanel.Children.Add(radButtonNon);
                radButtonPanel.Children.Add(radButtonMin);
                radButtonPanel.Children.Add(radButtonMax);

                radButtonPanel.Height = 26;

                soupdragon2.Children.Add(radButtonPanel);
            }

            soupdragon1.Width = 214;
            soupdragon1.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            soupdragon2.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;

            // Bring the soupdragons together and add to the overall stackpanel
            soupSP.Orientation = Orientation.Horizontal;
            soupSP.Children.Add(soupdragon1);
            soupSP.Children.Add(soupdragon2);

            // A separate method is used due to the history tab also utilising this facility
            AddPerformanceInfo(population, myBorders, HighlightedCluster, false);
        }


        /// <summary>
        /// Adds performance name criteria, value and coloured dot to a given list of borders
        /// </summary>
        /// <param name="thisPop"></param>
        /// <param name="yourBorders"></param>
        /// <param name="clusterID"></param>
        /// <param name="isHistory"></param>
        public void AddPerformanceInfo(Population thisPop, List<Border> yourBorders, int clusterID, bool isHistory)
        {

            // Performance labels
            double[][] performas = getRepresentativePerformas(thisPop);
            string[][] criteria = getRepresentativeCriteria(thisPop);

            //Add performance label
            for (int i = 0; i < yourBorders.Count; i++)
            {

                if (!isHistory)
                    yourBorders[i].Margin = new Thickness(margin_w + 5, 0, margin_w, 0);
                else
                {
                    yourBorders[i].Margin = new Thickness(0, 0, 0, 0);
                }
                // Try to catch if we just don't have the criteria info
                string label_p;

                // CAREFUL!!
                try
                {
                    double roundedPerf = Math.Round(performas[clusterID][i], 3);

                    if (!isHistory) label_p = criteria[clusterID][i].ToString() + "   =   " + roundedPerf.ToString();
                    else label_p = "  " + roundedPerf.ToString();

                    // 6 colours MAX!
                    string tooltiptext = "(pop average = " + thisPop.Performance_Averages[i]+")";
                    DockPanel dp_p = CreateColourCodedLabel(label_p, tooltiptext, rgb_performance[i % 8], isHistory, i);
                
                    yourBorders[i].Child = dp_p;
                }
                catch
                {
                    DockPanel dp_p = new DockPanel();
                    Label l = new Label();
                    l.Content = "No performance data available!";
                    l.FontSize = fontsize2;
                    dp_p.Children.Add(l);
                    yourBorders[i].Child = dp_p;
                }

                
            }
        }


        /// <summary>
        /// Create colour-coded label for each performance values
        /// </summary>
        /// <param name="text"></param>
        /// <param name="tooltiptext"></param>
        /// <param name="c"></param>
        /// <param name="isHistoryTab"></param>
        /// <param name="performanceID"></param>
        /// <returns></returns>
        private DockPanel CreateColourCodedLabel(string text, string tooltiptext, Color c, bool isHistoryTab, int performanceID)
        {
            DockPanel dp = new DockPanel();
            int diameter;
            int topOffset;
            int margin;
            int fSize;

            //Create filled circle
            if (isHistoryTab)
            {
                diameter = 7;
                topOffset = 9;
                margin = 0;
                fSize = 10;
            }
            else
            {
                diameter = 8;
                topOffset = 8;
                margin = margin_w;
                fSize = fontsize2;
            }

            Canvas canvas = new Canvas();
            canvas.Background = new SolidColorBrush(Colors.Transparent);

            System.Windows.Shapes.Ellipse circle = new System.Windows.Shapes.Ellipse();
            circle.Height = diameter;
            circle.Width = diameter;
            SolidColorBrush brush = new SolidColorBrush();
            brush.Color = c;
            circle.Fill = brush;

            Canvas.SetLeft(circle, 0);
            Canvas.SetTop(circle, topOffset);
            canvas.Children.Add(circle);

            Border border_c = new Border();
            border_c.Margin = new Thickness(2, 0, margin, 0);
            border_c.Child = canvas;

            DockPanel.SetDock(border_c, Dock.Left);
            dp.Children.Add(border_c);

            //Create performance label
            Label l = new Label();
            l.Content = text;
            l.FontSize = fSize;
            dp.Children.Add(l);

            dp.ToolTip = tooltiptext;

            return dp;
        }



        #endregion

        #region UI TAB 3 (HISTORY)

        /// <summary>
        /// Updates history canvas
        /// </summary>
        public void Tab3_primary_update(bool isOptimisationRun)
        {
            // Set viewport size (TODO: variable?)
            int vportWidth = 120;
            int vportHeight = 120;
            int gridHeight = vportHeight + 20 * (performanceCount + 2);
            int vMargin = 20;

            Grid myGrid = new Grid();
            myGrid.Height = gridHeight;
            myGrid.RowDefinitions.Add(new RowDefinition());
                    
            int xCount = 0;
            int j = Generation - 1;

            // Create the left hand side border
            Border dpborder = new Border();
            dpborder.BorderThickness = new Thickness(0);
            dpborder.Margin = new Thickness(30, 0, 0, 0);
            StackPanel dp = new StackPanel();
            dp.Orientation = Orientation.Vertical;

            // Create the text identifier
            TextBlock txt = new TextBlock();
            txt.HorizontalAlignment = HorizontalAlignment.Left;
            txt.FontSize = 20;
            string name = biobranchID + "." + j;
            txt.Inlines.Add(name);
            if (isOptimisationRun)
                txt.Foreground = Brushes.Red;
            dp.Children.Add(txt);
            
            Button myButton = new Button();
            myButton.Background = Friends.AlphaShade();
            myButton.Width = 70;
            myButton.Height = 16;

            // Tag button with some info
            int[] myTag = new int[2];
            myTag[0] = biobranchID;
            myTag[1] = j;
            myButton.Tag = myTag;
            myButton.BorderBrush = Brushes.Black;
            myButton.Content = "Reinstate";
            myButton.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            myButton.Click += new RoutedEventHandler(ReinstatePopClick);

            Border border_buttons = new Border();
            border_buttons.Margin = new Thickness(0, 10, 0, 0);
            border_buttons.Child = myButton;
            dp.Children.Add(border_buttons);
            dpborder.Child = dp;

            myGrid.ColumnDefinitions.Add(new ColumnDefinition());
            myGrid.Width = (xCount + 1) * vportWidth;
            Grid.SetRow(dpborder, 0);
            Grid.SetColumn(dpborder, xCount);
            myGrid.Children.Add(dpborder);

            xCount++;

            // Now to populate the selected designs for this generation
            for (int k = 0; k < BioBranches[biobranchID].PopTwigs[j].chromosomes.Length; k++)
            {
                bool flag = false;

                Population thisPop = BioBranches[biobranchID].PopTwigs[j];
                Chromosome thisDesign = BioBranches[biobranchID].PopTwigs[j].chromosomes[k];

                TagExtrema(thisPop);

                // Show all 12 cluster centroids?
                CheckBox myCb = (CheckBox)controls["cb_showall12"];

                if ((bool) myCb.IsChecked)
                {
                    if (thisDesign.isRepresentative) flag = true;
                }

                else
                {
                    if (isOptimisationRun && thisDesign.isRepresentative && thisDesign.isOptimal) flag = true;
                    if (thisDesign.isRepresentative && thisDesign.isChecked) flag = true;
                }
                
                // Now just show those representatives that are representatives.
                if (flag)
                {
                    StackPanel sp = new StackPanel();
                    sp.VerticalAlignment = System.Windows.VerticalAlignment.Top;

                    Border border = new Border {BorderThickness = new Thickness(0), Padding = new Thickness(2)};
                    ViewportBasic vp4 = new ViewportBasic(thisDesign, this) { Background = Friends.AlphaShade(), BorderThickness = new Thickness(1.0)};

                    if (thisDesign.isOptimal) {vp4.BorderBrush = Brushes.Red;}
                    else if (thisDesign.isChecked) {vp4.BorderBrush = Brushes.Black; }
                    else {vp4.BorderBrush = Brushes.SlateGray; }

                    border.Child = vp4;
                    border.Height = 120;
                    sp.Children.Add(border);
                    
                    // Design info
                    Border border_clus = new Border();
                    border_clus.Margin = new Thickness(0, 0, 0, 0);

                    // Get the performance borders from the dictionary
                    // Note that these performance borders are for ONE design.
                    List<Border> myBorders = new List<Border>();
                    for (int i = 0; i < performanceCount; i++)
                    {
                        myBorders.Add(new Border());
                        sp.Children.Add(myBorders[i]);
                    }

                    // A separate method is used due to the history tab also utilising this facility
                    AddPerformanceInfo(thisPop, myBorders, thisDesign.clusterId, true);
                    
                    myGrid.ColumnDefinitions.Add(new ColumnDefinition());
                    myGrid.Width = (xCount + 1) * vportWidth;
                    Grid.SetRow(border, 0);
                    Grid.SetColumn(border, xCount);
                    Grid.SetRow(sp, 0);
                    Grid.SetColumn(sp, xCount);
                    myGrid.Children.Add(sp);

                    xCount++;
                }

            }

            // Update X shift
            if (myGrid.Width > _historyY)
                _historyY = (int)myGrid.Width + 50;
                  
            // Set the left side based on the startY position for the new branch
            Canvas.SetLeft(myGrid, BioBranches[biobranchID].StartY);
            int yLocation = (generation - 1) * gridHeight + vMargin;
            Canvas.SetTop(myGrid, yLocation);
            _historycanvas.Children.Add(myGrid); // See xaml for history canvas

            // Now set some node points
            // TODO: Why is this StartY used in the X coordinate?
            BioBranches[biobranchID].PopTwigs[j].HistoryNodeIN = new System.Windows.Point(BioBranches[biobranchID].StartY + vportWidth -10, 20 + yLocation + vportHeight / 2);
            BioBranches[biobranchID].PopTwigs[j].HistoryNodeOUT = new System.Windows.Point(BioBranches[biobranchID].StartY + myGrid.Width + 10, 20 + yLocation + vportHeight / 2);

            // Set the pngHeight to the maximum so far
            int soupdragon = yLocation + gridHeight;
            if (soupdragon > pngHeight) pngHeight = soupdragon;

            // Draw the origin curve if we are not the first branch, and we are the first history member
            if(biobranchID!=0 && j==0)
                BioBranches[biobranchID].DrawOriginCurve(_historycanvas, BioBranches);

            // TODO: Define these DYNAMICALLY, important for the png export.
            _historycanvas.Width = BioBranches[biobranchID].StartY + _historyY + 30;
            _historycanvas.Height = pngHeight + 30;

            HistoryCanvas.Width = _historycanvas.Width;
            HistoryCanvas.Height = _historycanvas.Height;

        }

        /// <summary>
        /// Tag extrema values during optimisation
        /// </summary>
        /// <param name="thisPop"></param>
        public void TagExtrema(Population thisPop)
        {
            for (int p = 0; p < performanceCount; p++)
            {
                int minID = 0;
                int maxID = 0;
                double maxDouble = -99999999999999;
                double minDouble = 99999999999999;

                for (int i = 0; i < thisPop.chromosomes.Length; i++)
                {
                    if (thisPop.chromosomes[i].isRepresentative)
                    {
                        double val = thisPop.chromosomes[i].GetPerformas()[p];

                        if (val > maxDouble)
                        {
                            maxDouble = val;
                            maxID = i;
                        }

                        if (val < minDouble)
                        {
                            minDouble = val;
                            minID = i;
                        }

                    }
                }

                thisPop.chromosomes[minID].isMinimum = true;
                thisPop.chromosomes[maxID].isMaximum = true;

                // Get the associated min/max radio buttons
                RadioButton radButtonMin = (RadioButton)controls["RADBUTTONMIN" + p];
                RadioButton radButtonMax = (RadioButton)controls["RADBUTTONMAX" + p];

                if (radButtonMin.IsChecked == true)
                    thisPop.chromosomes[minID].isOptimal = true;

                if (radButtonMax.IsChecked == true)
                    thisPop.chromosomes[maxID].isOptimal = true;

            }
        }

        /// <summary>
        /// Create settings panel for Tab 3
        /// </summary>
        public void Tab3_secondary_settings()
        {
            StackPanel sp3 = new StackPanel();
            controls.Add("SP3", sp3);

            //Header
            Border border_head = new Border();
            border_head.Margin = new Thickness(margin_w, 0, margin_w, 0);
            Label label_head = new Label();
            label_head.FontSize = fontsize;
            label_head.Content = "Evolution History";
            border_head.Child = label_head;
            sp3.Children.Add(border_head);

            // History description
            Border border = new Border();
            border.Margin = new Thickness(margin_w, 0, margin_w, 0);
            TextBlock txt = new TextBlock();
            txt.TextWrapping = TextWrapping.Wrap;
            txt.FontSize = fontsize2;
            txt.Inlines.Add("Recorded history of selected designs, with results of a 'performance optimisation' run shown in red. \n\nDouble click a design to diplay the instance in the Rhino viewport.");
            Label label = new Label();
            label.Content = txt;
            border.Child = label;
            sp3.Children.Add(border);

            // Buttons
            DockPanel dp_buttons = new DockPanel();
            dp_buttons.LastChildFill = false;

            Border border_buttons = new Border();
            border_buttons.Margin = new Thickness(margin_w, 20, margin_w, 0);

            Button button_ExportPNG = CreateButton("b_tab3_ExportPNG", "save png", Tab3_secondary.Width * 0.3, new RoutedEventHandler(Tab3_ExportPNG_Click));
            DockPanel.SetDock(button_ExportPNG, Dock.Left);
            dp_buttons.Children.Add(button_ExportPNG);

            border_buttons.Child = dp_buttons;
            sp3.Children.Add(border_buttons);

            //Add the stackpanels to the secondary area of Tab 3
            Tab3_secondary.Child = sp3;

        }

        #endregion

        #region UI TAB 4 & 5 (PLOTS)

        /// <summary>
        /// Plot graph for 4 and 5
        /// </summary>
        public void Tab4_plotcanvas()
        {
            PlotCanvas.Children.Clear();
            PlotCanvas2.Children.Clear();

            // Include existing population
            int totalGenerations = BioBranches[biobranchID].PopTwigs.Count + 1;


            // Find the min and maximums from both current and historic populations
            List<double> miniP = new List<double>();
            List<double> maxiP = new List<double>();

            for (int p = 0; p < performanceCount; p++)
            {
                miniP.Add(population.Performance_Minimums[p]);
                maxiP.Add(population.Performance_Maximums[p]);

                if (BioBranches[biobranchID].PopTwigs.Count > 0)
                {
                    if (BioBranches[biobranchID].minPerformanceValues[p] < miniP[p])
                        miniP[p] = BioBranches[biobranchID].minPerformanceValues[p];
                    if (BioBranches[biobranchID].maxPerformanceValues[p] > maxiP[p])
                        maxiP[p] = BioBranches[biobranchID].maxPerformanceValues[p];

                }

                // Avoid division by zero
                if (miniP[p] == maxiP[p]) maxiP[p]++;
            }


            // Set up IDs for performance count
            ComboBox myComboX = (ComboBox)controls["MYCOMBOX"];
            ComboBox myComboY = (ComboBox)controls["MYCOMBOY"];

            int ParetoXID = myComboX.SelectedIndex;
            int ParetoYID = myComboY.SelectedIndex;

            // Generate the graph labels for the scatterplot
            if(performanceCount >= 2 && myComboX.SelectedIndex!=-1 && myComboY.SelectedIndex != -1)
            {
                this.Plot2XName.Text = myComboX.SelectedItem.ToString();
                this.Plot2YName.Text = myComboY.SelectedItem.ToString();

                this.MinXName.Text = Friends.AxisLabelText(miniP[ParetoXID]);
                this.MaxXName.Text = Friends.AxisLabelText(maxiP[ParetoXID]);

                this.MinYName.Text = Friends.AxisLabelText(miniP[ParetoYID]);
                this.MaxYName.Text = Friends.AxisLabelText(maxiP[ParetoYID]);
            }


            // Could be an array, but potentially more risky in case of early events.
            // Set up points for a polyline for each performance measure
            List<System.Windows.Shapes.Polyline> myPoly = new List<System.Windows.Shapes.Polyline>();
            for (int p = 0; p < performanceCount; p++)
            {
                    myPoly.Add(new System.Windows.Shapes.Polyline());
                    myPoly[p].StrokeThickness = 1.5;
                    myPoly[p].Stroke = new SolidColorBrush(rgb_performance[p % 8]);
                    PlotCanvas.Children.Add(myPoly[p]);
            }


            // 1. Add the historic populations first. 
            // j indicates generation
            for (int j = 0; j < BioBranches[biobranchID].PopTwigs.Count; j++)
            {

                double xPos = (Convert.ToDouble(j) / (totalGenerations)) * (PlotCanvas.Width-10) + 5;

                // k indicates chromosome (design)
                for (int k = 0; k < BioBranches[biobranchID].PopTwigs[j].chromosomes.Length; k++)
                {
                    Chromosome thisDesign = BioBranches[biobranchID].PopTwigs[j].chromosomes[k];
                    
                    if (thisDesign.isRepresentative || thisDesign.isMinimum || thisDesign.isMaximum || thisDesign.isOptimal)
                    {
                        List<double> myPerforms = thisDesign.GetPerformas();

                        // Note that manual selection does not run performance measures for all population, so we have to avoid this
                        if (myPerforms != null)
                        {
                            for (int p = 0; p < performanceCount; p++)
                            {
                                CheckBox checkBox = (CheckBox)controls["PLOTCHECKBOX" + p];
                                if ((bool)checkBox.IsChecked)
                                {

                                    // Includes some margins
                                    double yPos = PlotCanvas.Height - ((myPerforms[p] - miniP[p]) * Math.Abs((PlotCanvas.Height - 10)) / (maxiP[p] - miniP[p]) + 5);

                                    // Draw the circle
                                    System.Windows.Shapes.Path myCircle = new System.Windows.Shapes.Path();
                                    myCircle.Fill = new SolidColorBrush(rgb_performance[p % 8]); // 8 colours max
                                    myCircle.Data = new EllipseGeometry(new System.Windows.Point(xPos, yPos), 3, 3);

                                    PlotCanvas.Children.Add(myCircle);

                                }

                            }

                            // Now for the Pareto graph, which has to have minimum 2 performance values to work
                            // If index is -1, then combobox has not been selected yet
                            if (performanceCount > 1 && ParetoXID !=-1 && ParetoYID !=-1)
                            {

                                // Avoidance of division by zero has already been circumnavigated
                                double minXP = miniP[ParetoXID];
                                double maxXP = maxiP[ParetoXID];
                                double minYP = miniP[ParetoYID];
                                double maxYP = maxiP[ParetoYID];

                                // Includes margin
                                double paretoX = (myPerforms[ParetoXID] - minXP) * Math.Abs(PlotCanvas2.Width-10) / (maxXP - minXP) +5;
                                double paretoY = PlotCanvas2.Height - ((myPerforms[ParetoYID] - minYP) * Math.Abs((PlotCanvas2.Height - 10)) / (maxYP - minYP) + 5);

                                // Draw the circle
                                System.Windows.Shapes.Path myCircle2 = new System.Windows.Shapes.Path();
                                double alpha = (double)(j + 1) / BioBranches[biobranchID].PopTwigs.Count;

                                myCircle2.Fill = new SolidColorBrush(Color.FromArgb((byte)(int)(255 * alpha), 0, 0, 0));
                                myCircle2.Data = new EllipseGeometry(new System.Windows.Point(paretoX, paretoY), 3, 3);
                                //myCircle2.MouseDown += new MouseButtonEventHandler(ScatterCircleClick);
                                
                                PlotCanvas2.Children.Add(myCircle2);
                            }

                        }
                    }
                }

                // Average Polylines
                for (int p = 0; p < performanceCount; p++)
                {
                    
                    CheckBox checkBox = (CheckBox)controls["PLOTCHECKBOX" + p];
                    if ((bool)checkBox.IsChecked)
                    {

                        double yPos = PlotCanvas.Height - ((BioBranches[biobranchID].PopTwigs[j].Performance_Averages[p] - miniP[p]) * Math.Abs((PlotCanvas.Height - 10)) / (maxiP[p] - miniP[p]) + 5);

                        myPoly[p].Points.Add(new System.Windows.Point(xPos, yPos));

                        System.Windows.Shapes.Path myCircle = new System.Windows.Shapes.Path
                        {
                            Fill = Brushes.White,
                            StrokeThickness = 1.5,
                            Stroke = new SolidColorBrush(rgb_performance[p % 8]),// 8 colours max
                            Data = new EllipseGeometry(new System.Windows.Point(xPos, yPos), 4, 4),
                            ToolTip = "pop average = " + BioBranches[biobranchID].PopTwigs[j].Performance_Averages[p]
                        };

                        PlotCanvas.Children.Add(myCircle);

                    }
                }

            }


            // 2. Add the current population
            for (int p = 0; p < performanceCount; p++)
            {
                CheckBox checkBox = (CheckBox)controls["PLOTCHECKBOX" + p];
                if ((bool)checkBox.IsChecked)
                {

                    double xPos = ((totalGenerations - 1) * (PlotCanvas.Width - 10) / totalGenerations) + 5;

                    for (int j = 0; j < population.chromosomes.Length; j++)
                    {
                        if (population.chromosomes[j].isRepresentative)
                        {

                            // Includes some margins
                            double yPos = PlotCanvas.Height - ((population.chromosomes[j].GetPerformas()[p] - miniP[p]) * Math.Abs((PlotCanvas.Height - 10)) / (maxiP[p] - miniP[p]) + 5);

                            // Draw the circle
                            System.Windows.Shapes.Path myCircle = new System.Windows.Shapes.Path();
                            myCircle.Fill = new SolidColorBrush(rgb_performance[p % 8]); // 8 colours max
                            myCircle.Data = new EllipseGeometry(new System.Windows.Point(xPos, yPos), 3, 3);

                            PlotCanvas.Children.Add(myCircle);

                        }
                    }


                    double yAve = PlotCanvas.Height - ((population.Performance_Averages[p] - miniP[p]) * Math.Abs((PlotCanvas.Height - 10)) / (maxiP[p] - miniP[p]) + 5);

                    myPoly[p].Points.Add(new System.Windows.Point(xPos, yAve));

                    System.Windows.Shapes.Path aveCircle = new System.Windows.Shapes.Path
                    {
                        Fill = Brushes.White,
                        StrokeThickness = 1.5,
                        Stroke = new SolidColorBrush(rgb_performance[p % 8]),// 8 colours max
                        Data = new EllipseGeometry(new System.Windows.Point(xPos, yAve), 4, 4),
                        ToolTip = "pop average = " + population.Performance_Averages[p]
                    };

                    PlotCanvas.Children.Add(aveCircle);

                }
            }

            // This is essentially the same but with the current population
            if (performanceCount > 1 && ParetoXID != -1 && ParetoYID != -1)
            {

                // Avoidance of division by zero has already been circumnavigated
                double minXP = miniP[ParetoXID];
                double maxXP = maxiP[ParetoXID];
                double minYP = miniP[ParetoYID];
                double maxYP = maxiP[ParetoYID];

                // Includes margin
                for (int j = 0; j < population.chromosomes.Length; j++)
                {
                    if (population.chromosomes[j].isRepresentative)
                    {
                        double xx = population.chromosomes[j].GetPerformas()[ParetoXID];
                        double yy = population.chromosomes[j].GetPerformas()[ParetoYID];

                        double paretoX = (xx - minXP) * Math.Abs(PlotCanvas2.Width - 10) / (maxXP - minXP) + 5;
                        double paretoY = PlotCanvas2.Height - ((yy - minYP) * Math.Abs((PlotCanvas2.Height - 10)) / (maxYP - minYP) + 5);

                        // Draw the circle
                        System.Windows.Shapes.Path myCircle2 = new System.Windows.Shapes.Path
                        {
                            StrokeThickness = 1,
                            Stroke = Brushes.White,
                            Fill = Brushes.Black,
                            ToolTip = population.chromosomes[j].clusterId,
                            Data = new EllipseGeometry(new System.Windows.Point(paretoX, paretoY), 4, 4)
                        };
                        //myCircle2.MouseDown += new MouseButtonEventHandler(ScatterCircleClick);

                        PlotCanvas2.Children.Add(myCircle2);
                    }
                }
            }


            // 3. Finally the legend
            MinGraphLabels.Children.Clear();
            MaxGraphLabels.Children.Clear();
            
            for (int p = 0; p < performanceCount; p++)
            {
                CheckBox checkBox = (CheckBox)controls["PLOTCHECKBOX" + p];
                if ((bool)checkBox.IsChecked)
                {

                    TextBlock myTextBlock = new TextBlock
                    {
                        Foreground = new SolidColorBrush(rgb_performance[p % 8]),
                        Text = Friends.AxisLabelText(maxiP[p]),
                        UseLayoutRounding = true,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Right
                    };
                    MaxGraphLabels.Children.Add(myTextBlock);

                    TextBlock myTextBlock2 = new TextBlock
                    {
                        Foreground = new SolidColorBrush(rgb_performance[p % 8]),
                        Text = Friends.AxisLabelText(miniP[p]),
                        UseLayoutRounding = true,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Right
                    };
                    MinGraphLabels.Children.Add(myTextBlock2);
                }
            }

        }


        /// <summary>
        /// Create settings panel for Tab 4
        /// </summary>
        public void Tab4_secondary_settings()
        {
            StackPanel sp4 = new StackPanel();

            //Header
            Border border_head = new Border();
            border_head.Margin = new Thickness(margin_w, 0, margin_w, 0);
            Label label_head = new Label();
            label_head.FontSize = fontsize;
            label_head.Content = "Performance History";
            border_head.Child = label_head;
            sp4.Children.Add(border_head);

            // History description
            Border border = new Border();
            border.Margin = new Thickness(margin_w, 0, margin_w, 12);
            TextBlock txt = new TextBlock();
            txt.TextWrapping = TextWrapping.Wrap;
            txt.FontSize = fontsize2;
            txt.Inlines.Add("A history of quantitative performance (if applicable) on this evolutionary branch. \n\nShows data for the 12 cluster representatives and performance optimals, with an average shown for the whole population.");
            Label label = new Label();
            label.Content = txt;
            border.Child = label;
            sp4.Children.Add(border);

            // Now for the soupdragons...
            StackPanel soupdragon = new StackPanel();
            string[][] criteria = getRepresentativeCriteria(population);

            // Remove any checkboxes that might exist
            for (int i=0; i < performanceCount; i++)
            {
                controls.Remove("PLOTCHECKBOX" + i);
            }

            for (int i = 0; i < performanceCount; i++)
            {

                Border myBorder = new Border();
                myBorder.Margin = new Thickness(margin_w + 5, 0, margin_w, 0);
                
                DockPanel myPanel = new DockPanel();
                myPanel.LastChildFill = false;
                DockPanel dp_p = CreateColourCodedLabel(criteria[0][i].ToString(), "none", rgb_performance[i % 8], false, i);
                DockPanel.SetDock(dp_p, Dock.Left);
                myPanel.Children.Add(dp_p);

                CheckBox myCheck = new CheckBox();
                myCheck.IsChecked = true;
                myCheck.Background = Friends.AlphaShade();
                myCheck.BorderBrush = Brushes.Black;
                myCheck.Click += new RoutedEventHandler(Replot);
                controls.Add("PLOTCHECKBOX" + i, myCheck);

                DockPanel.SetDock(myCheck, Dock.Right);
                myPanel.Children.Add(myCheck);

                myBorder.Child = myPanel;
                soupdragon.Children.Add(myBorder);
            }

            // Bring the soupdragons together and add to the overall stackpanel
            soupdragon.Orientation = Orientation.Vertical;
            sp4.Children.Add(soupdragon);

            //Add the stackpanels to the secondary area of Tab 3
            Tab4_secondary.Child = sp4;

        }

        /// <summary>
        /// Create settings panel for Tab 5
        /// </summary>
        public void Tab5_secondary_settings()
        {
            StackPanel sp5 = new StackPanel();

            //Header
            Border border_head = new Border();
            border_head.Margin = new Thickness(margin_w, 0, margin_w, 0);
            Label label_head = new Label();
            label_head.FontSize = fontsize;
            label_head.Content = "Scatter Plot";
            border_head.Child = label_head;
            sp5.Children.Add(border_head);

            // History description
            Border border = new Border();
            border.Margin = new Thickness(margin_w, 0, margin_w, 12);
            TextBlock txt = new TextBlock();
            txt.TextWrapping = TextWrapping.Wrap;
            txt.FontSize = fontsize2;
            txt.Inlines.Add("Plots two performance criteria against each other on a scatter graph. \n\nAll generations for the current evolutionary branch are displayed, with those in the past the faded the most and the latest generation outlined.");
            Label label = new Label();
            label.Content = txt;
            border.Child = label;
            sp5.Children.Add(border);

            // Dropdowns
            string[][] criteria = getRepresentativeCriteria(population);

            // Remove any checkboxes that might exist already
            controls.Remove("MYCOMBOX");
            controls.Remove("MYCOMBOY");

            // Now make the new ones
            Border menuBorder = new Border();
            menuBorder.Margin = new Thickness(margin_w, 0, margin_w, 12);
            Border menuBorder2 = new Border();
            menuBorder2.Margin = new Thickness(margin_w, 0, margin_w, 12);

            ComboBox myComboX = new ComboBox();
            ComboBox myComboY = new ComboBox();

            controls.Add("MYCOMBOX", myComboX);
            controls.Add("MYCOMBOY", myComboY);

            myComboX.Background = Friends.AlphaShade();
            myComboY.Background = Friends.AlphaShade();

            for (int i = 0; i < performanceCount; i++)
            {
                myComboX.Items.Add(criteria[0][i].ToString());
                myComboY.Items.Add(criteria[0][i].ToString());
            }

            myComboX.SelectionChanged += new SelectionChangedEventHandler(Replot);
            myComboY.SelectionChanged += new SelectionChangedEventHandler(Replot);

            menuBorder.Child = myComboX;
            menuBorder2.Child = myComboY;

            sp5.Children.Add(menuBorder);
            sp5.Children.Add(menuBorder2);

            //Add the stackpanels to the secondary area of Tab 3
            Tab5_secondary.Child = sp5;
        }

        #endregion

        #region UI TAB 6 (ABOUT)

        void Tab6_primary_permanent()
        {
            StackPanel sp = new StackPanel();
            Border border_dcl = new Border();
            border_dcl.Margin = new Thickness(10, 10, 40, 20);
            sp.Children.Add(border_dcl);
            
            BitmapImage b = new BitmapImage();
            b.BeginInit();
            b.UriSource = new Uri(@"Resources\BiomorpherIcon2_240.png", UriKind.Relative);
            b.EndInit();

            Image myImage = new Image();
            myImage.Source = b;
            myImage.Width = 100;
            myImage.Height = 100;
            sp.Children.Add(myImage);

            TextBlock txt_dcl = new TextBlock();
            txt_dcl.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            txt_dcl.FontSize = 24;
            txt_dcl.Inlines.Add("\nBiomorpher  v"+Friends.VerionInfo());
            sp.Children.Add(txt_dcl);

            Border border_dcl2 = new Border();
            border_dcl2.Margin = new Thickness(10, 10, 16, 20);
            sp.Children.Add(border_dcl2);

            TextBlock txt_dcl2 = new TextBlock();
            txt_dcl2.TextWrapping = TextWrapping.Wrap;
            txt_dcl2.FontSize = 12;
            txt_dcl2.Inlines.Add("\nInteractive Evolutionary Algorithms (IEAs) allow designers to engage with the process of evolutionary development. This gives rise to an involved experience, helping to explore the wide combinatorial space of parametric models without always knowing where you are headed. ");
            txt_dcl2.Inlines.Add("\n\nInspired by Richard Dawkins' Biomorphs from 1986, who borrowed the term from the surrealist painter Desmond Morris. Everything you do is a balloon.");
            txt_dcl2.Inlines.Add("\n\nDevelopment:\tJohn Harding & Cecilie Brandt-Olsen");
            txt_dcl2.Inlines.Add("\nCopyright:\t2020 John Harding");
            txt_dcl2.Inlines.Add("\nContact:\t\tjohnharding@fastmail.fm");
            txt_dcl2.Inlines.Add("\nLicence:\t\tMIT");
            txt_dcl2.Inlines.Add("\nSource:\t\thttp://github.com/johnharding/Biomorpher");
            txt_dcl2.Inlines.Add("\nGHgroup:\thttp://www.grasshopper3d.com/group/biomorpher");
            txt_dcl2.Inlines.Add("\n\nDependencies:\tHelixToolkit: https://github.com/helix-toolkit");
            txt_dcl2.Inlines.Add("\n\t\tMahapps.metro: http://mahapps.com/");
            txt_dcl2.Inlines.Add("\n\nBiomorpher is completely free, but if you would like to make a small donation and leave a message you can do so at my ko-fi page (launches browser):");
            txt_dcl2.Inlines.Add("\n ");

            // Donate button
            Button donate = CreateButton("donate", "donate", 75, new RoutedEventHandler(ClickDonate));
            donate.BorderThickness = new Thickness(0);
            donate.Background = Brushes.LightGray;

            sp.Children.Add(txt_dcl2);
            sp.Children.Add(donate);

            Tab6_primary.Child = sp;
        }

        # endregion

        #region CONTROL HELPERS

        /// <summary>
        /// Shortcut to create grid control
        /// </summary>
        /// <param name="rowCount"></param>
        /// <param name="columnCount"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public Grid CreateGrid(int rowCount, int columnCount, double width, double height)
        {
            Grid grid = new Grid();
            grid.Width = width;
            grid.Height = height;

            for (int i = 0; i < rowCount; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition());
            }

            for (int i = 0; i < columnCount; i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition());
            }

            return grid;
        }


        /// <summary>
        /// Creates a checkbox control
        /// </summary>
        /// <param name="name"></param>
        /// <param name="handler"></param>
        /// <param name="chromoID"></param>
        /// <returns></returns>
        public CheckBox CreateCheckBox(string name, RoutedEventHandler handler, int chromoID)
        {
            CheckBox cb = new CheckBox();
            cb.Name = name;
            cb.IsChecked = false;
            cb.Checked += handler;
            cb.Unchecked += handler;
            cb.Tag = chromoID;
            cb.Background = Friends.AlphaShade();
            controls.Add(name, cb);
            return cb;
        }


        /// <summary>
        /// Create button control
        /// </summary>
        /// <param name="name"></param>
        /// <param name="content"></param>
        /// <param name="width"></param>
        /// <param name="handler"></param>
        /// <returns></returns>
        public Button CreateButton(string name, string content, double width, RoutedEventHandler handler)
        {
            Button b = new Button();
            b.Name = name;
            b.Content = content;
            b.Width = width;
            b.HorizontalAlignment = HorizontalAlignment.Left;
            b.Click += handler;
            b.Background = Friends.AlphaShade();
            b.BorderBrush = Brushes.Black;
            b.Padding = new Thickness(3);
            b.BorderThickness = new Thickness(1);
            controls.Add(name, b);
            return b;
        }


        /// <summary>
        /// Create slider with label WITHOUT eventhandler (useful to display e.g. performance values)
        /// </summary>
        /// <param name="labelName"></param>
        /// <param name="controlName"></param>
        /// <param name="minVal"></param>
        /// <param name="maxVal"></param>
        /// <param name="val"></param>
        /// <param name="isIntSlider"></param>
        /// <returns></returns>
        public DockPanel CreateSlider(string labelName, string controlName, double minVal, double maxVal, double val, bool isIntSlider)
        {
            //Container for slider + label
            DockPanel dp = new DockPanel();

            //Create slider
            Slider slider = new Slider();
            slider.Minimum = minVal;
            slider.Maximum = maxVal;
            slider.Value = val;
            slider.Name = controlName;
            slider.Focusable = false;
            slider.TickFrequency = 0.01;
            slider.IsSnapToTickEnabled = true;

            string format = "{0:0.00}";
            if (isIntSlider)
            {
                slider.TickFrequency = 2;
                format = "{0:0}";
            }


            //Add slider to control dictionary
            controls.Add(controlName, slider);

            //Create a label with the name of the slider
            Label label_name = new Label();
            label_name.HorizontalContentAlignment = HorizontalAlignment.Left;
            label_name.Content = labelName;

            DockPanel.SetDock(label_name, Dock.Top);
            dp.Children.Add(label_name);

            //Create a label with the current value of the slider
            Label label_val = new Label();
            Binding binding_val = new Binding("Value");
            label_val.ContentStringFormat = format;
            binding_val.Source = slider;
            label_val.SetBinding(Label.ContentProperty, binding_val);

            DockPanel.SetDock(label_val, Dock.Right);
            dp.Children.Add(label_val);
            dp.Children.Add(slider);

            return dp;
        }


        /// <summary>
        /// Create slider with label WITH eventhandler (allows user to e.g. control popSize and mutation rate)
        /// </summary>
        /// <param name="labelName"></param>
        /// <param name="controlName"></param>
        /// <param name="minVal"></param>
        /// <param name="maxVal"></param>
        /// <param name="val"></param>
        /// <param name="isIntSlider"></param>
        /// <param name="handler"></param>
        /// <returns></returns>
        public DockPanel CreateSlider(string labelName, string controlName, double minVal, double maxVal, double val, bool isIntSlider, RoutedPropertyChangedEventHandler<double> handler)
        {
            DockPanel dp = CreateSlider(labelName, controlName, minVal, maxVal, val, isIntSlider);
            
            Slider slider = (Slider)controls[controlName];
            slider.ValueChanged += handler;
     
            return dp;
        }


        /// <summary>
        /// Create combobox (dropdown menu)
        /// </summary>
        /// <param name="label"></param>
        /// <param name="name"></param>
        /// <param name="items"></param>
        /// <param name="handler"></param>
        /// <returns></returns>
        public DockPanel CreateComboBox(string label, string name, List<string> items, SelectionChangedEventHandler handler)
        {
            DockPanel dp = new DockPanel();

            ComboBox cbox = new ComboBox();
            cbox.Name = name;
            cbox.ItemsSource = items;
            cbox.SelectedIndex = 0;
            cbox.SelectionChanged += handler;

            controls.Add(name, cbox);

            Label l = new Label();
            l.Content = label;
            l.FontSize = fontsize;

            DockPanel.SetDock(l, Dock.Top);
            dp.Children.Add(l);
            dp.Children.Add(cbox);

            return dp;
        }

        #endregion

        #region EVENT HANDLERS

        // Donation weblink
        private void ClickDonate(Object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(@"https://ko-fi.com/ab8jeh");
        }

        //Tab 1 Popsize event handler
        private void Tab1_popSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider s = (Slider)sender;
            int val = (int)s.Value;
            PopSize = val;
        }

        //Tab 1 CrossoverProbability event handler
        private void Tab1_crossover_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider s = (Slider)sender;
            double val = s.Value;
            CrossoverProbability = val;
        }

        //Tab 1 MutateProbability event handler
        private void Tab1_mutation_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider s = (Slider)sender;
            double val = s.Value;
            MutateProbability = val;
        }

        private void backgroundSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider s = (Slider)sender;
            double val = s.Value;
            MainGrid.Background = Friends.RhinoGrey(val);
            tabControl.Background = Friends.RhinoGrey(val);
            Timmy1.Background = Friends.RhinoGrey(val);
            Timmy2.Background = Friends.RhinoGrey(val);
            Timmy3.Background = Friends.RhinoGrey(val);
            Timmy4.Background = Friends.RhinoGrey(val);
            Timmy5.Background = Friends.RhinoGrey(val);
            Timmy6.Background = Friends.RhinoGrey(val);
        }

        /// <summary>
        /// When a graph button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Replot(object sender, RoutedEventArgs e)
        {
            Tab4_plotcanvas();
        }

        /// <summary>
        /// Handle event when the "GO!" button is clicked in tab 1 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Tab1_Go_Click(object sender, RoutedEventArgs e)
        {
            RunInit(0);
            Button b1 = (Button)controls["b_tab1_Go"];
            b1.Content = "Restart";
            Button b2 = (Button)controls["b_tab1_Go2"];
            b2.Content = "Restart";
        }

        /// <summary>
        /// Handle the event when the Go2 button is clicked (existing population)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Tab1_Go2_Click(object sender, RoutedEventArgs e)
        {
            RunInit(1);
            Button b1 = (Button)controls["b_tab1_Go"];
            b1.Content = "Restart";
            Button b2 = (Button)controls["b_tab1_Go2"];
            b2.Content = "Restart";
        }

        /// <summary>
        /// When a circle is clicked on the scatter plot
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ScatterCircleClick(object sender, RoutedEventArgs e)
        {
            // TODO
            // Owner.SetInstance(thisDesign);
        }


        /// <summary>
        /// Reinstates an old population and makes a new biobranch
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ReinstatePopClick(object sender, RoutedEventArgs e)
        {
            // Get info from the sender button
            Button myButton = (Button)sender;
            int[] myTag = (int[])myButton.Tag;
            int branch = myTag[0];
            int twig   = myTag[1];

            // Define the offset Y for the new biobrach
            int offset = BioBranches[BioBranches.Count-1].StartY;

            // Add a new biobranch, using the tag information as the parent BRANCH and TWIG 
            BioBranches.Add(new BioBranch(branch, twig, offset + _historyY + 20));
            _historyY = 0;
            biobranchID++;

            // Clone the population
            population = new Population(BioBranches[branch].PopTwigs[twig]);

            // Clone carries of fitnesses, so it needs to be reset for a new run.
            population.ResetAllFitness();
       
            RunNewBranch();
        }


        /// <summary>
        /// Event handler for all checkboxes in tab 2
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Tab2_SelectParents_Check(object sender, RoutedEventArgs e)
        {
            CheckBox checkbox = sender as CheckBox;          //Get the checkbox that triggered the event

            if (checkbox.IsChecked == true)
            {
                ParentCount++;

                if (checkbox.Tag != null)
                {
                    // Cycle through the population. Tag any with this cluterId)
                    int ID = (int)checkbox.Tag;
                    foreach (Chromosome chromo in population.chromosomes)
                    {
                        if (chromo.clusterId == ID)
                        {
                            chromo.SetFitness(1.0);
                            chromo.isChecked = true;
                        }
                    }
                }

            }
            else
            {
                ParentCount--;
                //checkbox.BorderBrush = Brushes.Black;

                if (checkbox.Tag != null)
                {
                    // Cycle through the population. Tag any with this cluterId)
                    int ID = (int)checkbox.Tag;
                    foreach (Chromosome chromo in population.chromosomes)
                    {
                        if (chromo.clusterId == ID)
                        {
                            chromo.SetFitness(0.0);
                            chromo.isChecked = false;
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Handle event when the "Evolve" button is clicked in tab 2 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Tab2_Evolve_Click(object sender, RoutedEventArgs e)
        {
            Button b_clicked = (Button)sender;
            bool isPerformanceCriteriaBased = IsRadioMinMaxButtonChecked();

            //Test if minimum one parent is selected
            if (ParentCount < 1 && !isPerformanceCriteriaBased)
            {
                MessageBoxResult message = MessageBox.Show(this, "Manually select designs and/or performance criteria to evolve.");
            }

            else
            {
                //Run now moved to before we start to uncheck checkboxes
                //In order to maintin fitness values
                if (isPerformanceCriteriaBased)
                {
                    NumericUpDown nup = (NumericUpDown)controls["myNumericUpDown"];
                    for (int i = 0; i < nup.Value; i++)
                    {
                        Run(true);
                    }
                }

                else
                {
                    Run(false);
                }

                //Extract indices from names of checked boxes and uncheck all
                for (int i = 0; i < 12; i++)
                {
                    //The name of the checkbox control
                    string cb_name = "cb_tab2_" + i;

                    //Get this control from the dictionary
                    CheckBox cb = (CheckBox)controls[cb_name];

                    if (cb.IsChecked == true)
                    {
                        cb.IsChecked = false;
                    }
                }

                //Set parent count to zero
                ParentCount = 0;
            }

        }


        /// <summary>
        /// Finds out if one of the min or max radiobuttons is checked.
        /// </summary>
        /// <returns></returns>
        public bool IsRadioMinMaxButtonChecked()
        {
            bool isOneChecked = false;

            try
            {
                for (int i = 0; i < performanceCount; i++)
                {
                    RadioButton radmin = (RadioButton)controls["RADBUTTONMIN" + i];
                    RadioButton radmax = (RadioButton)controls["RADBUTTONMAX" + i];

                    if (radmin.IsChecked == true || radmax.IsChecked == true)
                    {
                        isOneChecked = true;
                    }
                }
            }

            catch{}

            return isOneChecked;
        }



        /// <summary>
        /// Handle event when the "SavePNG" button is clicked in tab 3
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Tab3_ExportPNG_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                System.Windows.Forms.SaveFileDialog fd = new System.Windows.Forms.SaveFileDialog();
                fd.Filter = "Png(*.PNG;)|*.PNG";
                fd.AddExtension = true;

                if (fd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {

                    Friends.CreateSaveBitmap(_historycanvas, fd.FileName);

                }
                
            }
            catch
            {
                System.Console.Beep(10000, 50);
                System.Console.Beep(20000, 100);
            }
            
        }


        /// <summary>
        /// INotifyPropertyChanged Implementation
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string name)
        {
            var handler = System.Threading.Interlocked.CompareExchange(ref PropertyChanged, null, null);
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        /// <summary>
        /// Handle when user resizes the window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChartGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            PlotCanvas.Width = chartGrid.ActualWidth;
            PlotCanvas.Height = chartGrid.ActualHeight;

            PlotCanvas2.Width = chartGrid2.ActualWidth;
            PlotCanvas2.Height = chartGrid2.ActualHeight;

            // Only if evolution has started do we do this bit.
            if (GO)
                Tab4_plotcanvas();
        }

        #endregion

    }

}