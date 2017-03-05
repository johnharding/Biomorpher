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

namespace Biomorpher
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class BiomorpherWindow : MetroWindow, INotifyPropertyChanged
    {
        // Fields
        private bool GO;
        private Population population;
        private PopHistory popHistory;
        private List<GH_NumberSlider> sliders;
        private List<GalapagosGeneListObject> genePools;
        private BiomorpherComponent owner;

        private double[] sliderValuesMin;
        private double[] sliderValuesMax;
        private string[] sliderNames;


        //UI properties
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


        //A dictionary, which contains the controls that need to be accessible from other methods after their creation (key to update controls)
        private Dictionary<string, FrameworkElement> controls;

        //Font and spacing
        int fontsize;
        int margin_w;
        int margin_h;


        // Constructor
        public BiomorpherWindow(BiomorpherComponent Owner)
        {
            // Set the component passed here to a field
            owner = Owner;

            // Get sliders and gene pools
            sliders = new List<GH_NumberSlider>();
            genePools = new List<GalapagosGeneListObject>();
            owner.GetSliders(sliders, genePools);

            // Extract slider info
            sliderValuesMin = new double[sliders.Count];
            sliderValuesMax = new double[sliders.Count];
            sliderNames = new string[sliders.Count];

            for (int i = 0; i < sliders.Count; i++)
            {
                sliderValuesMin[i] = (double)sliders[i].Slider.Minimum;
                sliderValuesMax[i] = (double)sliders[i].Slider.Maximum;
                sliderNames[i] = sliders[i].NickName;                       //To do: if Nickname is empty then use Name instead
            }


            // Initial Window things
            InitializeComponent();
            Topmost = true;
            PopSize = 100;
            MutateProbability = 0.1;
            Generation = 0;
            ParentCount = 0;
            GO = false;
            controls = new Dictionary<string, FrameworkElement>();
            fontsize = 12;
            margin_w = 20;
            margin_h = 20;

            //Tab 1: Settings
            tab1_secondary_settings();
        }


        /// <summary>
        /// Gets the phenotype information for all the current chromosomes
        /// </summary>
        public void GetPhenotypes()
        {
            // Get geometry for each chromosome in the initial population
            for (int i = 0; i < population.chromosomes.Length; i++)
            {
                if (population.chromosomes[i].isRepresentative)
                {
                    owner.canvas.Document.Enabled = false;                  // Disable the solver before tweaking sliders
                    owner.SetSliders(population.chromosomes[i], sliders, genePools);   // Change the sliders
                    owner.canvas.Document.Enabled = true;                   // Enable the solver again
                    owner.SetComponentOut(population);
                    owner.ExpireSolution(true);                             // Now expire the main component and recompute
                    owner.GetGeometry(population.chromosomes[i]);           // Get the new geometry for this particular chromosome
                }
            }
        }

        /// <summary>
        /// Instantiate the population and intialise the window
        /// </summary>
        public void RunInit()
        {
            // 1. Initialise population history
            popHistory = new PopHistory();

            // 2. Create initial population
            population = new Population(popSize, sliders, genePools);

            // 3. Perform K-means clustering
            population.KMeansClustering(12);

            // 4. Get geometry for each chromosome
            GetPhenotypes();

            // 5. Add population to history
            popHistory.AddPop(population);

            // 6. Setup tab layout
            tab12_primary_permanent(1);
            tab1_primary_update();

            tab12_primary_permanent(2);
            tab2_secondary_settings();
            List<Mesh> popMeshes = getRepresentativePhenotypes();
            tab2_primary_update(popMeshes);
        }



        /// <summary>
        /// When this gets called (probably via a button being triggered) we advance a generation 
        /// </summary>
        public void Run()
        {
            // 1. Create new populaltion using user selection
            population.RoulettePop();

            // 2. Mutate population using user preferences
            population.MutatePop(mutateProbability);

            // 3. Perform K-means clustering
            population.KMeansClustering(12);

            // 4. Get geometry for each chromosome
            GetPhenotypes();

            // 5. Display meshes and update windows
            tab1_primary_update();
            List<Mesh> popMeshes = getRepresentativePhenotypes();
            tab2_primary_update(popMeshes);

            // 6. Advance the generation counter and store the population historically.
            popHistory.AddPop(population);
            Generation++;

        }


        public void Exit()
        {
            // TODO: Set sliders and get geometry for a chosen chromosome


            // Close the window
            this.Close();
        }

        //Get representative meshes
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

            return phenotypes.ToList();
        }

        //Get representative performas
        private double[][] getRepresentativePerformas()
        {
            double[][] performas = new double[12][];

            Chromosome[] chromosomes = population.chromosomes;
            for (int i = 0; i < chromosomes.Length; i++)
            {
                if (chromosomes[i].isRepresentative)
                {
                    //int performasCount = chromosomes[i].GetPerformas().Count;
                    int performasCount = 3;             //temporary

                    performas[chromosomes[i].clusterId] = new double[performasCount];
                    for(int j=0; j< performasCount; j++)
                    {
                        //performas[chromosomes[i].clusterId][j] = chromosomes[i].GetPerformas()[j];
                        performas[chromosomes[i].clusterId][j] = Friends.GetRandomInt(0, 100);        //temporary
                    }
                }
                    
            }
            return performas;
        }



        //----------------------------------------------------------------------------UI METHODS-------------------------------------------------------------------------//


        //-------------------------------------------------------------------------------TAB 1: START------------------------------------------------------------------------//

        public void tab1_primary_update()
        {
            Color[] rgbs = new Color[12] { Color.FromArgb(255, 192, 255, 255), Color.FromArgb(255, 179, 251, 251), Color.FromArgb(255, 132, 235, 235), Color.FromArgb(255, 70, 215, 215), Color.FromArgb(255, 18, 198, 198), Color.FromArgb(255, 0, 192, 192), Color.FromArgb(255, 7, 182, 189), Color.FromArgb(255, 25, 155, 180), Color.FromArgb(255, 51, 116, 167), Color.FromArgb(255, 79, 74, 153), Color.FromArgb(255, 104, 36, 140), Color.FromArgb(255, 122, 9, 131) };

            //Run through the 12 designs
            for (int i = 0; i < 12; i++)
            {
                //Create canvas
                SolidColorBrush brush = new SolidColorBrush();
                brush.Color = rgbs[i];
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
            }
        }


        //Create canvas to visualise K-Means clustering
        public Canvas createKMeansVisualisation(int clusterIndex, SolidColorBrush colour)
        {
            int width = 150;
            int diameter = 10;

            Canvas canvas = new Canvas();
            canvas.Background = new SolidColorBrush(Colors.White);
            string name = "canvas" + clusterIndex;
            canvas.Name = name;


            //Add outline circle
            System.Windows.Shapes.Ellipse outline = new System.Windows.Shapes.Ellipse();
            outline.Height = width;
            outline.Width = width;
            outline.StrokeThickness = 1;
            outline.Stroke = Brushes.LightGray;

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
                circle.Fill = colour;

                //Calculate angle
                double angle = (2 * Math.PI * i) / clusterItems;
                double xCoord = distancesMapped[i] * Math.Cos(angle);
                double yCoord = distancesMapped[i] * Math.Sin(angle);


                //Lines
                System.Windows.Shapes.Line ln = new System.Windows.Shapes.Line();
                ln.StrokeThickness = 1;
                ln.Stroke = Brushes.LightGray;
                ln.X1 = width / 2.0;
                ln.Y1 = width / 2.0;
                ln.X2 = (width / 2.0) + xCoord;
                ln.Y2 = (width / 2.0) + yCoord;
                canvas.Children.Add(ln);

                //drawing order
                Canvas.SetLeft(circle, (width / 2.0) + xCoord - (diameter / 2.0));
                Canvas.SetTop(circle, (width / 2.0) + yCoord - (diameter / 2.0));
                canvas.Children.Add(circle);
            }

            return canvas;
        }



        public void tab1_secondary_settings()
        {
            //Container for all the controls
            StackPanel sp = new StackPanel();

            //Header
            Border border_head = new Border();
            border_head.Margin = new Thickness(margin_w, margin_h, margin_w, 0);

            Label label_head = new Label();
            label_head.FontSize = fontsize;
            label_head.FontWeight = FontWeights.Bold;
            label_head.Content = "EVOLUTIONARY SETTINGS";

            border_head.Child = label_head;
            sp.Children.Add(border_head);


            //Create sliders with labels
            Border border_popSize = new Border();
            border_popSize.Margin = new Thickness(margin_w, margin_h, margin_w, 0);
            DockPanel dp_popSize = createSlider("Population size", "s_tab1_popSize", 12, 200, PopSize, true, new RoutedPropertyChangedEventHandler<double>(tab1_popSize_ValueChanged));
            border_popSize.Child = dp_popSize;
            sp.Children.Add(border_popSize);


            Border border_mutation = new Border();
            border_mutation.Margin = new Thickness(margin_w, margin_h, margin_w, 0);
            DockPanel dp_mutation = createSlider("Mutation probability", "s_tab1_mutation", 0.00, 1.00, MutateProbability, false, new RoutedPropertyChangedEventHandler<double>(tab1_mutation_ValueChanged));
            border_mutation.Child = dp_mutation;
            sp.Children.Add(border_mutation);


            DockPanel dp_buttons = new DockPanel();
            dp_buttons.LastChildFill = false;

            Border border_buttons = new Border();
            border_buttons.Margin = new Thickness(margin_w, margin_h * 3, margin_w, 0);


            //GO button
            Button button_go = createButton("b_tab1_Go", "GO!", Tab1_secondary.Width * 0.3, new RoutedEventHandler(tab1_Go_Click));
            DockPanel.SetDock(button_go, Dock.Left);
            dp_buttons.Children.Add(button_go);

            //EXIT button
            Button button_exit = createButton("b_tab1_Exit", "Exit", Tab1_secondary.Width * 0.3, new RoutedEventHandler(tab1_Exit_Click));
            DockPanel.SetDock(button_exit, Dock.Right);
            dp_buttons.Children.Add(button_exit);


            border_buttons.Child = dp_buttons;
            sp.Children.Add(border_buttons);


            //Add the stackpanel to the secondary area of Tab 0
            Tab1_secondary.Child = sp;
        }


        //-------------------------------------------------------------------------------TAB 2: DESIGNS------------------------------------------------------------------------//

        //Create permanent grid layout with check boxes
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
                l.Content = i.ToString();
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

                //Add dockpanel to controls dictionary in order to access and update meshes afterwards (and not recreate the entire grid with checkboxes)
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


        public void tab2_primary_update(List<Mesh> meshes)
        {
            List<Canvas> performanceCanvas = tab2_primary_showPerformance();

            //Run through the design windows and add a viewport3d control and performance display to each
            for (int i = 0; i < meshes.Count; i++)
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
                Viewport3d vp3d = new Viewport3d(meshes[i]);
                dp.Children.Add(vp3d);


                //Performance display update
                if (dp_sub.Children.Count > 2)
                {
                    dp_sub.Children.RemoveAt(dp_sub.Children.Count - 1);
                }
                /*
                Color[] colours = new Color[4] { Color.FromArgb(255, 192, 255, 255), Color.FromArgb(255, 179, 251, 251), Color.FromArgb(255, 132, 235, 235), Color.FromArgb(255, 70, 215, 215) };
                bool[] isExtrema = new bool[4] { true, true, false, false };
                Canvas c = tab2_primary_performasCanvas(colours.ToList(), isExtrema.ToList());
                dp_sub.Children.Add(c);
                */

                Canvas c = performanceCanvas[i];
                dp_sub.Children.Add(c);


            }
        }


        public void tab2_secondary_settings()
        {
            StackPanel sp = new StackPanel();

            //Header
            Border border_head = new Border();
            border_head.Margin = new Thickness(margin_w, margin_h, margin_w, 0);

            Label label_head = new Label();
            label_head.FontSize = fontsize;
            label_head.FontWeight = FontWeights.Bold;
            label_head.Content = "CLUSTER REPRESENTATIVES";

            border_head.Child = label_head;
            sp.Children.Add(border_head);


            //Generation info
            Border border_gen = new Border();
            border_gen.Margin = new Thickness(margin_w, margin_h, margin_w, 0);

            Label label_gen = new Label();
            label_gen.SetBinding(ContentProperty, new Binding("Generation"));
            label_gen.DataContext = this;
            label_gen.ContentStringFormat = "Generation #{0}";
            label_gen.FontSize = fontsize;
            label_gen.FontWeight = FontWeights.Bold;

            border_gen.Child = label_gen;
            sp.Children.Add(border_gen);


            //Selection description
            Border border_sel = new Border();
            border_sel.Margin = new Thickness(margin_w, margin_h, margin_w, 0);

            TextBlock txt_sel = new TextBlock();
            txt_sel.TextWrapping = TextWrapping.Wrap;
            txt_sel.FontSize = fontsize;
            txt_sel.Inlines.Add(new Bold(new Run("Selection")));
            txt_sel.Inlines.Add("\nSelect parent(s) whose genes will be used to create the next design generation via the checkboxes");

            Label label_sel = new Label();
            label_sel.Content = txt_sel;

            border_sel.Child = label_sel;
            sp.Children.Add(border_sel);


            //Selected number of parents
            Border border_par = new Border();
            border_par.Margin = new Thickness(margin_w, margin_h, margin_w, 0);

            Label label_par = new Label();
            label_par.SetBinding(ContentProperty, new Binding("ParentCount"));
            label_par.DataContext = this;
            label_par.ContentStringFormat = "Selected parents: {0}";
            label_par.FontSize = fontsize;

            border_par.Child = label_par;
            sp.Children.Add(border_par);


            //Evolve button
            Border border_evo = new Border();
            border_evo.Margin = new Thickness(margin_w, margin_h, margin_w, 0);

            Button button_evo = createButton("b_tab2_Evolve", "Evolve", Tab2_secondary.Width * 0.5, new RoutedEventHandler(tab2_Evolve_Click));

            border_evo.Child = button_evo;
            sp.Children.Add(border_evo);


            //Design selection dropdown menu
            Border border_cbox = new Border();
            border_cbox.Margin = new Thickness(margin_w, margin_h * 3, margin_w, 0);

            List<string> comboboxItems = new List<string>();
            for (int i = 0; i < 12; i++)
            {
                string itemName = "Design " + i;
                comboboxItems.Add(itemName);
            }
            DockPanel dropdown = createComboBox("Select design", "cbox_tab2_selectDesign", comboboxItems, tab2_Combobox_SelectionChanged);

            border_cbox.Child = dropdown;
            sp.Children.Add(border_cbox);


            //Selected design properties
            Border border_prop = new Border();
            border_prop.Margin = new Thickness(margin_w, margin_h, margin_w, 0);

            ComboBox cbox = (ComboBox)controls["cbox_tab2_selectDesign"];
            int selItem = cbox.SelectedIndex;
            ScrollViewer sv_properties = tab2_secondary_genesCreate(population, selItem);              //TODO: Send chromosome ID not the grid ID

            border_prop.Child = sv_properties;
            sp.Children.Add(border_prop);


            //Add the stackpanel to the secondary area of Tab 2
            Tab2_secondary.Child = sp;
        }


        private ScrollViewer tab2_secondary_genesCreate(Population pop, int chromoID)
        {
            DockPanel dp = new DockPanel();

            double[] realGenes = pop.chromosomes[chromoID].GetRealGenes();
            double fitness = pop.chromosomes[chromoID].GetFitness();

            // Just the sliders, not gene pool
            for (int i = 0; i < sliders.Count; i++)
            {
                string controlName = "tab2_s_gene" + i;
                DockPanel dp_sliderG = createSlider(sliderNames[i], controlName, sliderValuesMin[i], sliderValuesMax[i], realGenes[i], false);

                Slider sliderG = (Slider)controls[controlName];
                sliderG.IsEnabled = false;

                DockPanel.SetDock(dp_sliderG, Dock.Top);
                dp.Children.Add(dp_sliderG);
            }

            DockPanel dp_sliderF = createSlider("Fitness", "tab2_s_fitness", 0.0, 1.0, fitness, false);

            Slider sliderF = (Slider)controls["tab2_s_fitness"];
            sliderF.IsEnabled = false;

            dp.Children.Add(dp_sliderF);

            //Create scroll view of sliders (useful in case there are many)
            ScrollViewer sv = new ScrollViewer();
            sv.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
            sv.Height = 250;
            sv.Content = dp;

            return sv;
        }


        private void tab2_secondary_genesUpdate(Population pop, int chromoID)
        {
            double[] realGenes = pop.chromosomes[chromoID].GetRealGenes();
            double fitness = pop.chromosomes[chromoID].GetFitness();

            for (int i = 0; i < sliders.Count; i++)
            {
                string controlName = "tab2_s_gene" + i;
                Slider sliderG = (Slider)controls[controlName];
                sliderG.IsEnabled = true;
                sliderG.Value = realGenes[i];
                sliderG.IsEnabled = false;
            }

            Slider sliderF = (Slider)controls["tab2_s_fitness"];
            sliderF.IsEnabled = true;
            sliderF.Value = fitness;
            sliderF.IsEnabled = false;
        }


        private List<Canvas> tab2_primary_showPerformance()
        {
            Color[] rgbs = new Color[6] { Color.FromArgb(255,0,174,239), Color.FromArgb(255, 0, 231, 239), Color.FromArgb(255,0,239,191), Color.FromArgb(255,0,135,239), Color.FromArgb(255,84,0,239), Color.FromArgb(255,152,0,239) };
            int alfaMin = 100;
            int alfaMax = 255;

            double[][] performas = getRepresentativePerformas();
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


            //Create a performance canvas for each representative design
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


                    //change alpha value
                    Color c = Color.FromArgb(Convert.ToByte(t_map),rgbs[j].R, rgbs[j].G, rgbs[j].B);
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
                Canvas canvas = tab2_primary_performasCanvas(colours, isExtrema);
                performanceCanvas.Add(canvas);
            }

            return performanceCanvas;
        }


        //Create performas canvas with coloured circles
        private Canvas tab2_primary_performasCanvas(List<Color> colours, List<bool> isExtrema)
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
                    extremaCircle.Stroke = Brushes.LightGray;
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




        //-------------------------------------------------------------------------------CREATE CONTROLS------------------------------------------------------------------------//

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


        //Create checkbox control
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


        //Create slider control with label
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
            l.FontWeight = FontWeights.Bold;

            DockPanel.SetDock(l, Dock.Top);
            dp.Children.Add(l);
            dp.Children.Add(cbox);

            return dp;
        }



        //-------------------------------------------------------------------------------EVENT HANDLERS------------------------------------------------------------------------//

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


        //Handle event when the "GO!" button is clicked in tab 1       
        public void tab1_Go_Click(object sender, RoutedEventArgs e)
        {

            if (!GO)
            {
                RunInit();

                //Disable sliders in tab 1
                Slider s_popSize = (Slider)controls["s_tab1_popSize"];
                s_popSize.IsEnabled = false;

                Slider s_mutation = (Slider)controls["s_tab1_mutation"];
                s_mutation.IsEnabled = false;
            }

            GO = true;
        }


        //Handle event when the "Exit" button is clicked in tab 1       
        public void tab1_Exit_Click(object sender, RoutedEventArgs e)
        {
            Exit();
        }


        //One event handler for all checkboxes in tab 2        
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
                            chromo.SetFitness(1.0);
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
                            chromo.SetFitness(0.0);
                    }
                }

            }

        }


        //Event handler for dropdown menu in tab 2 to select a specific design
        private void tab2_Combobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cbox = (ComboBox)sender;
            int selectedIndex = cbox.SelectedIndex;

            //Change gene sliders according to selection
            tab2_secondary_genesUpdate(population, selectedIndex);                          //TODO: Send chromosome ID not the grid ID

        }


        //Handle event when the "Evolve" button is clicked in tab 2       
        public void tab2_Evolve_Click(object sender, RoutedEventArgs e)
        {
            Button b_clicked = (Button)sender;

            //Test if minimum one parent is selected
            if (ParentCount < 1)
            {
                MessageBoxResult message = MessageBox.Show(this, "Select minimum one parent via the checkboxes");
            }

            else
            {
                //Run now moved to before we start to uncheck checkboxes
                //In order to maintin fitness values
                Run();

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


                //Reset selected item in dropdown menu to 0 and update slider properties
                ComboBox cbox = (ComboBox)controls["cbox_tab2_selectDesign"];
                cbox.SelectedIndex = 0;
                tab2_secondary_genesUpdate(population, 0);                                      //TODO: Send chromosome ID not the grid ID

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



    }
}
