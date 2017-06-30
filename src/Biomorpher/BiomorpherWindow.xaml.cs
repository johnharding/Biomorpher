using HelixToolkit.Wpf;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Biomorpher.IGA;
using Biomorpher;
using Grasshopper.Kernel;
using MahApps.Metro.Controls;
using System.Windows.Controls.Primitives;
using System.ComponentModel;
using Grasshopper.Kernel.Special;
using GalapagosComponents;
using Grasshopper.Kernel.Data;
using Grasshopper;
using System.IO;

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
        private static readonly object syncLock = new object();

        /// <summary>
        /// Progress bar window
        /// </summary>
        //private ProgressWindow myProgressWindow = new ProgressWindow();
        
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

        /// <summary>
        /// A dictionary, which contains the controls that need to be accessible from other methods after their creation (key to update controls)
        /// </summary>
        private Dictionary<string, FrameworkElement> controls;

        //Font, spacing and colours
        int fontsize;
        int fontsize2;
        int margin_w;
        Color[] rgb_performance;
        Color[] rgb_kmeans;

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
            owner.GetSliders(sliders, genePools);

            // Initial Window things
            InitializeComponent();

            _historycanvas = new Canvas();
            _historycanvas.Background = Brushes.White;
            HistoryCanvas.Children.Add(_historycanvas);
            _historyY = 0;
            pngHeight = 0;

            Topmost = true;
            PopSize = 100;
            MutateProbability = 0.01;
            Generation = 0;
            ParentCount = 0;
            performanceCount = 0;
            GO = false;
            HighlightedCluster = 0;
            fontsize = 20;
            fontsize2 = 12;
            margin_w = 20;
            rgb_performance = new Color[6] { Color.FromArgb(255, 236, 28, 59), Color.FromArgb(255, 121, 0, 120), Color.FromArgb(255, 17, 141, 200), Color.FromArgb(255, 36, 180, 66), Color.FromArgb(255, 222, 231, 31), Color.FromArgb(255, 243, 57, 0) };
            rgb_kmeans = new Color[12] { Color.FromArgb(255, 192, 255, 255), Color.FromArgb(255, 179, 251, 251), Color.FromArgb(255, 132, 235, 235), Color.FromArgb(255, 70, 215, 215), Color.FromArgb(255, 18, 198, 198), Color.FromArgb(255, 0, 192, 192), Color.FromArgb(255, 7, 182, 189), Color.FromArgb(255, 25, 155, 180), Color.FromArgb(255, 51, 116, 167), Color.FromArgb(255, 79, 74, 153), Color.FromArgb(255, 104, 36, 140), Color.FromArgb(255, 122, 9, 131) };

            // Dictionary of control elements
            controls = new Dictionary<string, FrameworkElement>();

            //Initialise Tab 1 Start settings (i.e. popsize and mutation sliders)
            tab1_secondary_settings();

            //Make sure that tab 3 graphics are clipped to bounds
            Tab3_primary.ClipToBounds = true;

            // Show biomorpher info
            Tab4_primary_permanent();
            
        }

        #endregion

        #region MAIN METHODS

        
        /// <summary>
        /// Instantiate the population and intialise the window
        /// </summary>
        public void RunInit()
        {
            // 1. Initialise population history
            BioBranches = new List<BioBranch>();
            biobranchID = 0;
            BioBranches.Add(new BioBranch(-1, 0, 0));

            // 2. Create initial population
            population = new Population(popSize, sliders, genePools);

            // 3. Perform K-means clustering
            population.KMeansClustering(12);

            // 4. Get geometry and performance for each chromosome
            GetPhenotypes(true);

            // 5. Now get the average performance values (cluster reps only)
            population.SetAveragePerformanceValues(performanceCount, true);

            // 6. Setup tab layouts
            tab12_primary_permanent(1); // 1 indicates tab 1
            tab1_primary_update();

            tab12_primary_permanent(2); // 2 indicates tab 2 (but same method!)
            tab2_primary_update();

            tab2_secondary_settings();

            tab3_secondary_settings();

            // 7. Set component outputs
            owner.SetComponentOut(population, BioBranches);
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
                GetPhenotypes(false); // We have to do this to make sure we have performance for the whole population.
                population.ResetAllFitness();
                population.SetPerformanceBasedFitness(controls, performanceCount);
            }

            // 9. Add old population to history.
            BioBranches[biobranchID].AddTwig(population);

            //////////////////////////////////////////////////////////////////////////



            // 1. Create a new population using fitness values (also resets fitnesses)
            Generation++;
            population.RoulettePop();
            
            // 2. Mutate population using user preferences
            population.MutatePop(mutateProbability);

            // 2a. Jiggle the population a little to avoid repeats (don't tell anyone)
            population.JigglePop(0.01);

            // 3. Perform K-means clustering
            population.KMeansClustering(12);

            // 4. Get geometry for cluster reps only
            GetPhenotypes(true);

            // 5. Now get the average performance values. Cluster reps only bool here
            population.SetAveragePerformanceValues(performanceCount, true);

            // 6. Update display of K-Means and representative meshes
            tab1_primary_update();

            tab2_primary_update();
            tab2_updatePerforms();

            tab3_primary_update(isPerformanceCriteriaBased);

            // 7. Set component outputs
            owner.SetComponentOut(population, BioBranches);   
        }

        /// <summary>
        /// Runs when a new biobranch is spawned.
        /// </summary>
        public void RunNewBranch()
        {
            // Reset generation counter
            Generation = 0;

            // Perform K-means clustering again?
            //population.KMeansClustering(12);

            // Get geometry for each chromosome
            GetPhenotypes(true);

            // 5. Now get the average performance values (cluster reps only)
            population.SetAveragePerformanceValues(performanceCount, true);

            // Update display of K-Means and representative meshes
            tab1_primary_update();

            tab2_primary_update();
            tab2_updatePerforms();

        }



        /// <summary>
        /// Gets the phenotype information for the current cluster representatives
        /// </summary>
        public void GetPhenotypes(bool clusterRepsOnly)
        {

            // Get geometry for each chromosome in the initial population
            // TODO: Don't repeat code like this!
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
                        performanceCount = owner.GetGeometry(population.chromosomes[i]);    // Get the new geometry for this particular chromosome
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
                    performanceCount = owner.GetGeometry(population.chromosomes[i]);    // Get the new geometry for this particular chromosome
                }
            }

            // TODO: Fill up null performance values instead, because this way if you have a null performance value it kills all the others.
            population.RepairPerforms();

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
            tab2_updatePerforms();
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
        private List<Mesh> getRepresentativePhenotypes()
        {
            Mesh[] phenotypes = new Mesh[12];

            Chromosome[] chromosomes = population.chromosomes;

            for (int i = 0; i < chromosomes.Length; i++)
            {
                if (chromosomes[i].isRepresentative)
                {
                    phenotypes[chromosomes[i].clusterId] = chromosomes[i].phenotype[0];
                }
                    
            }

            // List is now ordered according to cluster IDs
            return phenotypes.ToList();
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

        //Update display of K-Means clustering
        public void tab1_primary_update()
        {
            //Run through the 12 designs
            for (int i = 0; i < 12; i++)
            {
                //Create canvas
                SolidColorBrush brush = new SolidColorBrush();
                brush.Color = rgb_kmeans[i];
                Canvas canvas = createKMeansVisualisation(i, brush);

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


        //Create canvas to visualise K-Means clustering for a specific ID
        public Canvas createKMeansVisualisation(int clusterIndex, SolidColorBrush colour)
        {
            int width = 150;
            int diameter = 8;

            Canvas canvas = new Canvas();
            canvas.Background = new SolidColorBrush(Colors.White);
            string name = "canvas" + clusterIndex;
            canvas.Name = name;
            canvas.Width = width;
            canvas.Height = width;

            //Add outline circle
            System.Windows.Shapes.Ellipse outline = new System.Windows.Shapes.Ellipse();
            outline.Height = width;
            outline.Width = width;
            outline.StrokeThickness = 1;
            outline.Stroke = Brushes.SlateGray;

            Canvas.SetLeft(outline, 0);
            Canvas.SetTop(outline, 0);
            canvas.Children.Add(outline);

            //Add chromosome dots
            List<double> distances = new List<double>();
            for (int i = 0; i < population.chromosomes.Length; i++)
            {
                if (population.chromosomes[i].clusterId == clusterIndex)
                {
                    double d = population.chromosomes[i].distToRepresentative;
                    distances.Add(d);
                }
            }

            int clusterItems = distances.Count;

            //Map distances to width domain
            double distMin = distances.Min();
            double distMax = distances.Max();
            double distRange = distMax - distMin;

            List<double> distancesMapped = new List<double>();
            for (int i = 0; i < distances.Count; i++)
            {
                double d_normal = 0.0;
                if (distRange != 0.0)
                {
                    d_normal = (distances[i] - distMin) / (distRange);
                }
                double d_map = d_normal * (width / 2.0);
                distancesMapped.Add(d_map);
            }

            //Create shapes and add to canvas
            for (int i = 0; i < clusterItems; i++)
            {
                //Circles
                System.Windows.Shapes.Ellipse circle = new System.Windows.Shapes.Ellipse();
                circle.Height = diameter;
                circle.Width = diameter;
                circle.Fill = Brushes.SlateGray; //colour

                //Calculate angle
                double angle = (2 * Math.PI * i) / clusterItems;
                double xCoord = distancesMapped[i] * Math.Cos(angle);
                double yCoord = distancesMapped[i] * Math.Sin(angle);

                //Lines
                System.Windows.Shapes.Line ln = new System.Windows.Shapes.Line();
                ln.StrokeThickness = 1;
                ln.Stroke = Brushes.SlateGray;
                ln.X1 = width / 2.0 + 0;
                ln.Y1 = width / 2.0;
                ln.X2 = (width / 2.0) + xCoord + 0;
                ln.Y2 = (width / 2.0) + yCoord;
                canvas.Children.Add(ln);

                //drawing order
                Canvas.SetLeft(circle, (width / 2.0) + xCoord - (diameter / 2.0) + 0);
                Canvas.SetTop(circle, (width / 2.0) + yCoord - (diameter / 2.0));
                canvas.Children.Add(circle);
            }

            // centre circle
            System.Windows.Shapes.Ellipse circle2 = new System.Windows.Shapes.Ellipse();
            circle2.Height = diameter;
            circle2.Width = diameter;
            circle2.Fill = Brushes.White; //colour
            circle2.Stroke = Brushes.SlateGray;
            circle2.StrokeThickness = 1;
            Canvas.SetLeft(circle2, (width / 2.0) - (diameter / 2.0)+0);
            Canvas.SetTop(circle2, (width / 2.0) - (diameter / 2.0));
            canvas.Children.Add(circle2);

            return canvas;
        }


        //Create settings panel for Tab 1
        public void tab1_secondary_settings()
        {
            //Container for all the controls
            StackPanel sp = new StackPanel();

            //Header
            Border border_head = new Border();
            border_head.Margin = new Thickness(margin_w, 0, margin_w, 0);
            Label label_head = new Label();
            label_head.FontSize = fontsize;
            label_head.Content = "Settings";
            border_head.Child = label_head;
            sp.Children.Add(border_head);

            // K-means description
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



            // Create sliders with labels
            Border border_popSize = new Border();
            border_popSize.Margin = new Thickness(margin_w, 20, margin_w, 0);
            DockPanel dp_popSize = createSlider("Population size", "s_tab1_popSize", 12, 200, PopSize, true, new RoutedPropertyChangedEventHandler<double>(tab1_popSize_ValueChanged));
            border_popSize.Child = dp_popSize;
            sp.Children.Add(border_popSize);

            Border border_mutation = new Border();
            border_mutation.Margin = new Thickness(margin_w, 0, margin_w, 0);
            DockPanel dp_mutation = createSlider("Mutation probability", "s_tab1_mutation", 0.00, 1.00, MutateProbability, false, new RoutedPropertyChangedEventHandler<double>(tab1_mutation_ValueChanged));
            border_mutation.Child = dp_mutation;
            sp.Children.Add(border_mutation);


            // Now for the buttons
            DockPanel dp_buttons = new DockPanel();
            dp_buttons.LastChildFill = false;
            Border border_buttons = new Border();
            border_buttons.Margin = new Thickness(margin_w, 20, margin_w, 0);
            //dp_buttons.Arrange(new Rect(200, 200, 200, 200));

            //GO button
            Button button_go = createButton("b_tab1_Go", "Go", Tab1_secondary.Width * 0.3, new RoutedEventHandler(tab1_Go_Click));
            DockPanel.SetDock(button_go, Dock.Left);
            dp_buttons.Children.Add(button_go);

            //EXIT button
            Button button_exit = createButton("b_tab1_Exit", "Exit", Tab1_secondary.Width * 0.3, new RoutedEventHandler(Exit_Click));
            DockPanel.SetDock(button_exit, Dock.Right);
            dp_buttons.Children.Add(button_exit);
            border_buttons.Child = dp_buttons;
            
            sp.Children.Add(border_buttons);

            
            // K-means text
            Border border_kmeans = new Border();
            border_kmeans.Margin = new Thickness(margin_w, 50, margin_w, 0);
            Label label_kmeans = new Label();
            label_kmeans.FontSize = fontsize;
            label_kmeans.Content = "K-means Clusters";
            border_kmeans.Child = label_kmeans;
            sp.Children.Add(border_kmeans);
            
            
            // K-means description
            Border border = new Border();
            border.Margin = new Thickness(margin_w, 0, margin_w, 0);
            TextBlock txt = new TextBlock();
            //txt.Foreground = Brushes.White;
            txt.TextWrapping = TextWrapping.Wrap;
            txt.FontSize = fontsize2;
            txt.Inlines.Add("Designs are clustered into 12 groups based on parameter (n-dimensional) similarity. Click on the 'design' tab to see representative closest to centroid of each group");
            Label label = new Label();
            label.Content = txt;
            border.Child = label;
            sp.Children.Add(border);
            

            //Add the stackpanel to the secondary area of Tab 0
            Tab1_secondary.Child = sp;
        }

        #endregion

        #region UI TAB 2 (DESIGNS)

        /// <summary>
        /// Create permanent grid layout for Tab 1 and Tab 2 (if Tab 2 is specified then checkboxes are added to the top right corners of the grid as well)
        /// </summary>
        /// <param name="tabIndex"></param>
        public void tab12_primary_permanent(int tabIndex)
        {
            //Create grid 3x4 layout
            int rowCount = 3;
            int columnCount = 4;
            int gridCount = rowCount * columnCount;
            Grid grid = createGrid(rowCount, columnCount, Tab2_primary.Width, Tab2_primary.Height);

            //For each grid cell: create border with padding, a dock panel and add a checkbox
            for (int i = 0; i < gridCount; i++)
            {
                //Border
                Border border = new Border();
                border.BorderBrush = Brushes.LightGray;
                border.BorderThickness = new Thickness(0.3);
                border.Padding = new Thickness(5);

                //Master Dock panel
                DockPanel dp = new DockPanel();
                string dp_name = "dp_tab" + tabIndex + "_" + i;
                dp.Name = dp_name;

                //Sub Dock panel
                DockPanel dp_sub = new DockPanel();
                string dp_sub_name = "dp_sub_tab" + tabIndex + "_" + i;
                dp_sub.Name = dp_sub_name;

                //Label
                Label l = new Label();
                int index = i;
                l.Content = index.ToString();
                l.FontSize = fontsize;
                l.Foreground = Brushes.LightGray;
                l.HorizontalAlignment = HorizontalAlignment.Left;
                DockPanel.SetDock(l, Dock.Left);
                dp_sub.Children.Add(l);

                if (tabIndex == 2)
                {
                    //Create checkbox with an event handler
                    string cb_name = "cb_tab2_" + i;
                    CheckBox cb = createCheckBox(cb_name, new RoutedEventHandler(tab2_SelectParents_Check), i);
                    cb.HorizontalAlignment = HorizontalAlignment.Right;
                    DockPanel.SetDock(cb, Dock.Right);
                    dp_sub.Children.Add(cb);
                }

                DockPanel.SetDock(dp_sub, Dock.Top);
                dp.Children.Add(dp_sub);

                //Add dockpanel to controls dictionary in order to access and update content without recreating the entire grid with checkboxes
                controls.Add(dp_name, dp);
                controls.Add(dp_sub_name, dp_sub);

                //Set the dockpanel as the child of the border element
                border.Child = dp;

                //Add the border to the grid
                Grid.SetRow(border, (int)(i / columnCount));
                Grid.SetColumn(border, i % columnCount);
                grid.Children.Add(border);
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


        //Updates the display of the representative meshes and their performance values
        public void tab2_primary_update()
        {
            List<Mesh> meshes = getRepresentativePhenotypes();
            List<Canvas> performanceCanvas = createPerformanceCanvasAll();

            //Run through the design windows and add a viewport3d control and performance display to each
            // TODO: Is using the meshes count cool here???
            for (int i = 0; i < 12; i++)
            {
                //The name of the control to add to
                string dp_name = "dp_tab2_" + i;
                string dp_sub_name = "dp_sub_tab2_" + i;

                //Get this control from the dictionary
                DockPanel dp = (DockPanel)controls[dp_name];
                DockPanel dp_sub = (DockPanel)controls[dp_sub_name];

                //Viewport update
                if (dp.Children.Count > 1)
                {
                    dp.Children.RemoveAt(dp.Children.Count - 1);
                }

                Viewport3d vp3d = new Viewport3d(meshes[i], i, this, true);
                dp.Children.Add(vp3d);

                //Performance display update
                if (dp_sub.Children.Count > 2)
                {
                    dp_sub.Children.RemoveAt(dp_sub.Children.Count - 1);
                }

                Canvas c = performanceCanvas[i];
                dp_sub.Children.Add(c);
            }


            // Match the cameras
            // TODO: Better way to do this???
            for (int i = 0; i < 12; i++)
            {
                //The name of the control to add to
                string dp_name = "dp_tab2_" + i;

                //Get this control from the dictionary
                DockPanel dp = (DockPanel)controls[dp_name];
                Viewport3d vp3d = (Viewport3d)dp.Children[1];
                vp3d.MatchCamera();

            }
        }


        //Create performance canvas for all representative designs
        private List<Canvas> createPerformanceCanvasAll()
        {
            int alfaMin = 50;
            int alfaMax = 255;

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
                List<Color> colours = new List<Color>();
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


                    //change alpha value. MODULO 6 MAXIMUM?
                    Color c = Color.FromArgb(Convert.ToByte(t_map), rgb_performance[j % 6].R, rgb_performance[j % 6].G, rgb_performance[j % 6].B);
                    colours.Add(c);

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
                Canvas canvas = createPerformanceCanvas(colours, isExtrema);
                performanceCanvas.Add(canvas);
            }

            return performanceCanvas;
        }


        /// <summary>
        /// Create performas canvas with coloured circles for one representative design
        /// </summary>
        /// <param name="colours"></param>
        /// <param name="isExtrema"></param>
        /// <returns></returns>
        private Canvas createPerformanceCanvas(List<Color> colours, List<bool> isExtrema)
        {
            int numCircles = colours.Count;
            int dOuter = 16;
            int dOffset = 3;
            int topOffset = 5;

            Canvas canvas = new Canvas();
            canvas.Background = new SolidColorBrush(Colors.White);

            //Add circles
            for(int i=0; i<numCircles; i++)
            {
                int distFromLeft = dOuter + ((dOuter + dOffset) * i);

                //Extrema circle
                System.Windows.Shapes.Ellipse extremaCircle = new System.Windows.Shapes.Ellipse();
                extremaCircle.Height = dOuter;
                extremaCircle.Width = dOuter;
                extremaCircle.StrokeThickness = 0.5;
                if (isExtrema[i])
                {
                    extremaCircle.Stroke = Brushes.Gray;
                }
                else
                {
                    extremaCircle.Stroke = Brushes.White;
                }                

                Canvas.SetLeft(extremaCircle, distFromLeft);
                Canvas.SetTop(extremaCircle, topOffset);
                canvas.Children.Add(extremaCircle);


                //Performance circle
                System.Windows.Shapes.Ellipse performanceCircle = new System.Windows.Shapes.Ellipse();
                performanceCircle.Height = dOuter - (2*dOffset);
                performanceCircle.Width = dOuter - (2*dOffset);
                SolidColorBrush brush = new SolidColorBrush();
                brush.Color = colours[i];
                performanceCircle.Fill = brush;

                Canvas.SetLeft(performanceCircle, (distFromLeft+dOffset) );
                Canvas.SetTop(performanceCircle, (topOffset + dOffset));
                canvas.Children.Add(performanceCircle);
            }

            return canvas;
        }


        /// <summary>
        /// Create settings panel for Tab 2
        /// </summary>
        public void tab2_secondary_settings()
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
            Button button_evo = createButton("b_tab2_Evolve", "Evolve", Tab2_secondary.Width * 0.3, new RoutedEventHandler(tab2_Evolve_Click));
            DockPanel.SetDock(button_evo, Dock.Left);
            dp_buttons.Children.Add(button_evo);

            //EXIT2 button
            Button button_exit = createButton("b_tab2_Exit", "Exit", Tab1_secondary.Width * 0.3, new RoutedEventHandler(Exit_Click));
            DockPanel.SetDock(button_exit, Dock.Right);
            dp_buttons.Children.Add(button_exit);

            border_buttons.Child = dp_buttons;
            sp.Children.Add(border_buttons);

            //Header 2
            Border border_data = new Border();
            border_data.Margin = new Thickness(margin_w, 50, margin_w, 0);
            Label label_data = new Label();
            label_data.FontSize = fontsize;
            label_data.Content = "Design Properties";
            border_data.Child = label_data;
            sp.Children.Add(border_data);

            // Doubleclick description
            Border border_dcl = new Border();
            border_dcl.Margin = new Thickness(margin_w, 0, margin_w, 0);
            TextBlock txt_dcl = new TextBlock();
            txt_dcl.TextWrapping = TextWrapping.Wrap;
            txt_dcl.FontSize = fontsize2;
            txt_dcl.Inlines.Add("Double click a design to diplay its Rhino/Grasshopper instance and review performance data");

            Label label_dcl = new Label();
            label_dcl.Content = txt_dcl;
            border_dcl.Child = label_dcl;
            sp.Children.Add(border_dcl);


            // Display the highlighted design label (i.e. "Design 0"). Add to controls so it can be updated using tab2_updateperforms
            Border border_cluster = new Border();
            controls.Add("CLUSTER", border_cluster);
            sp.Children.Add(border_cluster);


            // Now for the soupdragons...
            StackPanel soupdragonMaster = new StackPanel();
            StackPanel soupdragon1 = new StackPanel();
            StackPanel soupdragon2 = new StackPanel();


            // Add the performance borders to soupdragon 1
            for (int i = 0; i < performanceCount; i++)
            {
                Border border_p = new Border();
                controls.Add("PERFBORDER" + i, border_p);
                soupdragon1.Children.Add(border_p);
            }

            // Add the performance buttons to soupdragon 2
            //Viewbox myBox = new Viewbox();
            //myBox.Child = dummy;
            //myBox.Height = 16;
 
            // Add the radiobuttons
            for (int i = 0; i < performanceCount; i++)
            {
                DockPanel radButtonPanel = new DockPanel();

                RadioButton radButtonNon = new RadioButton();
                RadioButton radButtonMin = new RadioButton();
                RadioButton radButtonMax = new RadioButton();
                

                radButtonNon.IsChecked = true;

                radButtonNon.ToolTip = "no optimisation";
                radButtonMin.ToolTip = "minimise";
                radButtonMax.ToolTip = "maximise";

                controls.Add("RADBUTTONMIN" + i, radButtonMin);
                controls.Add("RADBUTTONMAX" + i, radButtonMax);
                controls.Add("RADBUTTONNON" + i, radButtonNon);

                radButtonPanel.Children.Add(radButtonNon);
                radButtonPanel.Children.Add(radButtonMin);
                radButtonPanel.Children.Add(radButtonMax);

                radButtonPanel.Height = 24;

                soupdragon2.Children.Add(radButtonPanel);
            }

            soupdragon1.Width = 214;
            //soupdragon2.Width = 60;
            soupdragon1.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            soupdragon2.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;

            // Bring the soupdragons together and add to the overall stackpanel
            soupdragonMaster.Orientation = Orientation.Horizontal;
            soupdragonMaster.Children.Add(soupdragon1);
            soupdragonMaster.Children.Add(soupdragon2);
            sp.Children.Add(soupdragonMaster);


            // Performance labels
            tab2_updatePerforms();

            //Add the stackpanels to the secondary area of Tab 2
            Tab2_secondary.Child = sp;

        }


        /// <summary>
        /// Updates the list of performance 'borders' on the right hand side of the main window (tab 2)
        /// Called when a design is double clicked
        /// </summary>
        private void tab2_updatePerforms()
        {
            //Design info
            Border border_clus = (Border) controls["CLUSTER"];
            border_clus.Margin = new Thickness(margin_w, 30, margin_w, 10);
            
            Label label_gen = new Label();
            label_gen.Content = "Design " + HighlightedCluster +":";
            label_gen.FontSize = fontsize-2;
            border_clus.Child = label_gen;

            // Get the performance borders from the dictionary
            // Note that these performance borders are for ONE design.
            List<Border> myBorders = new List<Border>();
            for (int i = 0; i < performanceCount; i++)
            {
                myBorders.Add((Border)controls["PERFBORDER" + i]);
            }
            
            // A separate method is used due to the history tab also utilising this facility
            AddPerformanceInfo(population, myBorders, HighlightedCluster, false);
        }


        /// <summary>
        /// Adds performance name criteria, value and coloured dot to a given list of borders
        /// </summary>
        /// <param name="yourBorders"></param>
        /// <param name="clusterID"></param>
        public void AddPerformanceInfo(Population thisPop, List<Border> yourBorders, int clusterID, bool isHistory)
        {

            // Performance labels
            double[][] performas = getRepresentativePerformas(thisPop);
            string[][] criteria = getRepresentativeCriteria(thisPop);

            //Add performance label
            for (int i = 0; i < yourBorders.Count; i++)
            {

                if(!isHistory)
                    yourBorders[i].Margin = new Thickness(margin_w + 5, 0, margin_w, 0);
                else
                    yourBorders[i].Margin = new Thickness(0, 0, 0, 0);

                // Try to catch if we just don't have the criteria info
                string label_p;

                // CAREFUL!!
                try
                {

                    double roundedPerf = Math.Round(performas[clusterID][i], 3);
                    if (!isHistory)
                        label_p = criteria[clusterID][i].ToString() + "   =   " + roundedPerf.ToString();
                    else
                        label_p = "  " + roundedPerf.ToString();

                    // 6 colours MAX!
                    string tooltiptext = "(pop average = " + thisPop.AveragePerformanceValues[i]+")";
                    DockPanel dp_p = createColourCodedLabel(label_p, tooltiptext, rgb_performance[i % 6], isHistory, i);
                
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
        /// <param name="c"></param>
        /// <param name="isHistoryTab"></param>
        /// <param name="performanceID"></param>
        /// <returns></returns>
        private DockPanel createColourCodedLabel(string text, string tooltiptext, Color c, bool isHistoryTab, int performanceID)
        {
            DockPanel dp = new DockPanel();
            int diameter;
            int topOffset;
            int margin;
            int fSize;

            //Create filled circle
            if (isHistoryTab)
            {
                diameter = 6;
                topOffset = 6;
                margin = 6;
                fSize = 8;
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
        public void tab3_primary_update(bool isOptimisationRun)
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
            dpborder.BorderBrush = Brushes.White;
            dpborder.BorderThickness = new Thickness(0.3);
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
            myButton.Width = 70;
            myButton.Height = 16;

            // Tag button with some info
            int[] myTag = new int[2];
            myTag[0] = biobranchID;
            myTag[1] = j;
            myButton.Tag = myTag;

            myButton.Content = "reinstate";
            myButton.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            myButton.Click += new RoutedEventHandler(reinstatePopClick);

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
            for (int k = 0; k < BioBranches[biobranchID].Twigs[j].chromosomes.Length; k++)
            {
                bool flag = false;

                Population thisPop = BioBranches[biobranchID].Twigs[j];
                Chromosome thisDesign = BioBranches[biobranchID].Twigs[j].chromosomes[k];


                TagExtrema(thisPop);

                if (isOptimisationRun && thisDesign.isRepresentative) flag = true;
                if (!isOptimisationRun && thisDesign.isRepresentative && thisDesign.isChecked) flag = true;

                // Now just show those representatives that are checked
                if (flag)
                {
                    StackPanel sp = new StackPanel();
                    sp.VerticalAlignment = System.Windows.VerticalAlignment.Top;

                    Border border = new Border();
                    border.BorderBrush = Brushes.White;
                    border.BorderThickness = new Thickness(0.3);
                    border.Padding = new Thickness(2);
                    
                    ViewportBasic vp4 = new ViewportBasic(thisDesign, this);
                    vp4.Background = Brushes.White;

                    if(thisDesign.isSoupDragon || !isOptimisationRun)
                        vp4.BorderThickness = new Thickness(1.2);
                    else
                        vp4.BorderThickness = new Thickness(0.6);

                    if (thisDesign.isSoupDragon)
                        vp4.BorderBrush = Brushes.Red;
                    else
                        vp4.BorderBrush = Brushes.LightGray;
                    border.Child = vp4;
                    border.Height = 120;
                    sp.Children.Add(border);
                    
                    //Design info
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
                    
                    //myGrid.Children.Add(border);
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

            // Update shift
            if (myGrid.Width > _historyY)
                _historyY = (int)myGrid.Width;
            
                    
            // Set the left side based on the startY position for the new branch
            Canvas.SetLeft(myGrid, BioBranches[biobranchID].StartY);
            int yLocation = (generation - 1) * gridHeight + vMargin;
            Canvas.SetTop(myGrid, yLocation);
            _historycanvas.Children.Add(myGrid); // See xaml for history canvas

            // Now set some node points
            BioBranches[biobranchID].Twigs[j].HistoryNodeIN = new System.Windows.Point(BioBranches[biobranchID].StartY + vportWidth, 20 + yLocation + vportHeight / 2);
            BioBranches[biobranchID].Twigs[j].HistoryNodeOUT = new System.Windows.Point(BioBranches[biobranchID].StartY + myGrid.Width, 20 + yLocation + vportHeight / 2);

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

                // Get the associated min/max radio buttons
                RadioButton radButtonMin = (RadioButton)controls["RADBUTTONMIN" + p];
                RadioButton radButtonMax = (RadioButton)controls["RADBUTTONMAX" + p];

                if (radButtonMin.IsChecked == true)
                    thisPop.chromosomes[minID].isSoupDragon = true;

                if (radButtonMax.IsChecked == true)
                    thisPop.chromosomes[maxID].isSoupDragon = true;
            }
        }




        /// <summary>
        /// Create settings panel for Tab 3
        /// </summary>
        public void tab3_secondary_settings()
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
            txt.Inlines.Add("Recorded history of designs. Double click a design to diplay the instance in the Rhino viewport");
            Label label = new Label();
            label.Content = txt;
            border.Child = label;
            sp3.Children.Add(border);


            // Buttons
            DockPanel dp_buttons = new DockPanel();
            dp_buttons.LastChildFill = false;

            Border border_buttons = new Border();
            border_buttons.Margin = new Thickness(margin_w, 20, margin_w, 0);

            Button button_ExportPNG = createButton("b_tab3_ExportPNG", "save png", Tab3_secondary.Width * 0.3, new RoutedEventHandler(tab3_ExportPNG_Click));
            DockPanel.SetDock(button_ExportPNG, Dock.Left);
            dp_buttons.Children.Add(button_ExportPNG);

            Button button_exit = createButton("b_tab3_Exit", "Exit", Tab3_secondary.Width * 0.3, new RoutedEventHandler(Exit_Click));
            DockPanel.SetDock(button_exit, Dock.Right);
            dp_buttons.Children.Add(button_exit);

            border_buttons.Child = dp_buttons;
            sp3.Children.Add(border_buttons);


            // Note header
            Border border_head2 = new Border();
            border_head2.Margin = new Thickness(margin_w, 50, margin_w, 0);
            Label label_head2 = new Label();
            label_head2.FontSize = fontsize;
            label_head2.Content = "Notes";
            border_head2.Child = label_head2;
            sp3.Children.Add(border_head2);

            // Notes
            Border border_txt = new Border();
            border_txt.Margin = new Thickness(margin_w, 10, margin_w, 0);
            TextBox myTextbox = new TextBox();
            myTextbox.MinHeight = 400;
            myTextbox.BorderThickness = new Thickness(0);
            myTextbox.IsManipulationEnabled = true;
            myTextbox.TextWrapping = TextWrapping.Wrap;
            myTextbox.SnapsToDevicePixels = true;
            myTextbox.AcceptsReturn = true;
            myTextbox.AcceptsTab = true;
            myTextbox.Background = Brushes.GhostWhite;
            border_txt.Child = myTextbox;
            sp3.Children.Add(border_txt);

            //Add the stackpanels to the secondary area of Tab 3
            Tab3_secondary.Child = sp3;

        }


        #endregion

        #region UI TAB 4 (ABOUT)

        void Tab4_primary_permanent()
        {
            StackPanel sp = new StackPanel();
            Border border_dcl = new Border();
            border_dcl.Margin = new Thickness(10, 10, 40, 20);
            sp.Children.Add(border_dcl);
            
            BitmapImage b = new BitmapImage();
            b.BeginInit();
            b.UriSource = new Uri(@"Images\BioIcon2_240.png", UriKind.Relative);
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
            txt_dcl2.FontSize = 14;
            txt_dcl2.Inlines.Add("\nInteractive Evolutionary Algorithms (IEAs) allow designers to engage with the process of evolutionary development. This gives rise to an involved experience, helping to explore the wide combinatorial space of parametric models without always knowing where you are headed.");
            txt_dcl2.Inlines.Add("\n\nThis work is sponsored by the 2016/17 UWE VC Early Career Researcher Development Award and was initially inspired by Richard Dawkins' Biomorphs from his 1986 book, The Blind Watchmaker: Why the Evidence of Evolution Reveals a Universe without Design.");
            txt_dcl2.Inlines.Add("\n\n\nDevelopment:\tJohn Harding & Cecilie Brandt Olsen");
            txt_dcl2.Inlines.Add("\nCopyright:\t2017 John Harding & UWE");
            txt_dcl2.Inlines.Add("\nContact:\t\tjohnharding@fastmail.fm");
            txt_dcl2.Inlines.Add("\nLicence:\t\tMIT");
            txt_dcl2.Inlines.Add("\nSource:\t\thttp://github.com/johnharding/Biomorpher");
            txt_dcl2.Inlines.Add("\nGHgroup:\thttp://www.grasshopper3d.com/group/biomorpher");
            txt_dcl2.Inlines.Add("\n\nDependencies:\tHelixToolkit: https://github.com/helix-toolkit");
            txt_dcl2.Inlines.Add("\n\t\tMahapps.metro: http://mahapps.com/");
            sp.Children.Add(txt_dcl2);

            Tab4_primary.Child = sp;
        }

        # endregion

        #region CONTROL HELPERS

        //Create Grid control
        public Grid createGrid(int rowCount, int columnCount, double width, double height)
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
        public CheckBox createCheckBox(string name, RoutedEventHandler handler, int chromoID)
        {
            CheckBox cb = new CheckBox();
            cb.Name = name;
            cb.IsChecked = false;
            cb.Checked += handler;
            cb.Unchecked += handler;
            cb.Tag = chromoID;

            controls.Add(name, cb);
            return cb;
        }


        //Create button control
        public Button createButton(string name, string content, double width, RoutedEventHandler handler)
        {
            Button b = new Button();
            b.Name = name;
            b.Content = content;
            b.Width = width;
            b.HorizontalAlignment = HorizontalAlignment.Left;
            b.Click += handler;

            controls.Add(name, b);
            return b;
        }


        //Create slider with label WITHOUT eventhandler (useful to display e.g. performance values)
        public DockPanel createSlider(string labelName, string controlName, double minVal, double maxVal, double val, bool isIntSlider)
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
                slider.TickFrequency = 1.0;
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


        //Create slider with label WITH eventhandler (allows user to e.g. control popSize and mutation rate)
        public DockPanel createSlider(string labelName, string controlName, double minVal, double maxVal, double val, bool isIntSlider, RoutedPropertyChangedEventHandler<double> handler)
        {
            DockPanel dp = createSlider(labelName, controlName, minVal, maxVal, val, isIntSlider);
            
            Slider slider = (Slider)controls[controlName];
            slider.ValueChanged += handler;

            return dp;
        }


        //Create combobox (dropdown menu)
        public DockPanel createComboBox(string label, string name, List<string> items, SelectionChangedEventHandler handler)
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

        //Tab 1 Popsize event handler
        private void tab1_popSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider s = (Slider)sender;
            int val = (int)s.Value;
            PopSize = val;
        }

        //Tab 1 MutateProbability event handler
        private void tab1_mutation_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider s = (Slider)sender;
            double val = s.Value;
            MutateProbability = val;
        }


        /// <summary>
        /// Handle event when the "GO!" button is clicked in tab 1 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void tab1_Go_Click(object sender, RoutedEventArgs e)
        {

            if (!GO)
            {
                RunInit();

                //Disable sliders in tab 1
                Slider s_popSize = (Slider)controls["s_tab1_popSize"];
                s_popSize.IsEnabled = false;

            }

            GO = true;
        }


        /// <summary>
        /// Closes the window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
      
        public void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


        /// <summary>
        /// Reinstates an old population and makes a new biobranch
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void reinstatePopClick(object sender, RoutedEventArgs e)
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
            population = new Population(BioBranches[branch].Twigs[twig]);

            // Clone carries of fitnesses, so it needs to be reset for a new run.
            population.ResetAllFitness();

            RunNewBranch();
        }


        /// <summary>
        /// Event handler for all checkboxes in tab 2
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void tab2_SelectParents_Check(object sender, RoutedEventArgs e)
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
        public void tab2_Evolve_Click(object sender, RoutedEventArgs e)
        {
            Button b_clicked = (Button)sender;

            bool isPerformanceCriteriaBased = isRadioMinMaxButtonChecked();


            //Test if minimum one parent is selected
            if (ParentCount < 1 && !isPerformanceCriteriaBased)
            {
                MessageBoxResult message = MessageBox.Show(this, "Select a minimum of one parent design using the checkboxes, or else select performance criteria to optimise");
            }

            else
            {
                //Run now moved to before we start to uncheck checkboxes
                //In order to maintin fitness values
                if (isPerformanceCriteriaBased)
                    Run(true);
                else
                    Run(false);


                //Extract indexes from names of checked boxes and uncheck all
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
        public bool isRadioMinMaxButtonChecked()
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

            catch { }

            return isOneChecked;
        }



        /// <summary>
        /// Handle event when the "SavePNG" button is clicked in tab 3
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void tab3_ExportPNG_Click(object sender, RoutedEventArgs e)
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


        //INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string name)
        {
            var handler = System.Threading.Interlocked.CompareExchange(ref PropertyChanged, null, null);
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

     

        #endregion

    }
}