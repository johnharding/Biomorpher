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
using MahApps.Metro.Controls.Dialogs;
using System.Windows.Controls.Primitives;
using System.ComponentModel;

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
        private List<Grasshopper.Kernel.Special.GH_NumberSlider> sliders;
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
                if(value != parentCount)
                {
                    parentCount = value;
                    OnPropertyChanged("ParentCount");
                }
            }
        }


        //A dictionary, which contains the controls that need to be accessible from other methods after their creation
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

            // Get sliders
            sliders = new List<Grasshopper.Kernel.Special.GH_NumberSlider>();
            owner.GetSliders(sliders);

            //Extract slider info
            sliderValuesMin = new double[sliders.Count];
            sliderValuesMax = new double[sliders.Count];
            sliderNames = new string[sliders.Count];

            for (int i = 0; i < sliders.Count; i++)
            {
                sliderValuesMin[i] = (double) sliders[i].Slider.Minimum;
                sliderValuesMax[i] = (double) sliders[i].Slider.Maximum;
                sliderNames[i] = sliders[i].NickName;
            }


            // Initial Window things
            InitializeComponent();
            Topmost = true;
            PopSize = 12;
            MutateProbability = 0.1;
            Generation = 0;
            ParentCount = 0;
            GO = false;
            controls = new Dictionary<string, FrameworkElement>();
            fontsize = 12;
            margin_w = 20;
            margin_h = 20;

            //Tab 0: Settings
            tab0_secondary_settings();
        }


        /// <summary>
        /// Gets the phenotype information for all the current chromosomes
        /// </summary>
        public void GetPhenotypes()
        {
            // Get geometry for each chromosome in the initial population
            for (int i = 0; i < population.chromosomes.Length; i++)
            {
                owner.canvas.Document.Enabled = false;                  // Disable the solver before tweaking sliders
                owner.SetSliders(population.chromosomes[i], sliders);   // Change the sliders
                owner.canvas.Document.Enabled = true;                   // Enable the solver again
                owner.ExpireSolution(true);                             // Now expire the main component and recompute
                owner.GetGeometry(population.chromosomes[i]);           // Get the new geometry for this particular chromosome
            }
        }

        /// <summary>
        /// Instantiate the population and intialise the window
        /// </summary>
        public void RunInit()
        {
            // 1. Initialise population history
            popHistory = new PopHistory();

            // 2. Create initial population and add to history
            population = new Population(popSize, sliders);
            popHistory.AddPop(population);

            // 3. Get geometry for each chromosome
            GetPhenotypes();

            // 4. Setup tab layout
            tab2_primary_permanent();
            tab2_secondary_settings();
            List<Mesh> popMeshes = getRepresentativePhenotypes(population);
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

            // 3. Get geometry for each chromosome
            GetPhenotypes();

            // 4. Display meshes
            List<Mesh> popMeshes = getRepresentativePhenotypes(population);
            tab2_primary_update(popMeshes);

            // 5. Advance the generation counter and store the population historically.
            popHistory.AddPop(population);
            Generation++; 
        }


        public void Exit()
        {
            // Set sliders and get geometry for a chosen chromosome

            // Close the window
        }



        //To do: change to get centroids from K-means clustering (or 'closest to centroid')
        private List<Mesh> getRepresentativePhenotypes(Population pop)
        {
            List<Mesh> phenotypes = new List<Mesh>();

            Chromosome[] chromosomes = pop.chromosomes;
            for (int i = 0; i < 12; i++)
            {
                phenotypes.Add(chromosomes[i].phenotype[0]);
            }

            return phenotypes;
        }



        //----------------------------------------------------------------------------UI METHODS-------------------------------------------------------------------------//


        //-------------------------------------------------------------------------------TAB 0------------------------------------------------------------------------//

        public void tab0_secondary_settings()
        {
            //Container for all the controls
            StackPanel sp = new StackPanel();


            //Create sliders with labels
            Border border_popSize = new Border();
            border_popSize.Margin = new Thickness(margin_w, margin_h, margin_w, 0);
            DockPanel dp_popSize = createSlider("Population size", "s_tab0_popSize", 12, 100, PopSize, true, new RoutedPropertyChangedEventHandler<double>(tab0_popSize_ValueChanged));
            border_popSize.Child = dp_popSize;
            sp.Children.Add(border_popSize);


            Border border_mutation = new Border();
            border_mutation.Margin = new Thickness(margin_w, margin_h, margin_w, 0);
            DockPanel dp_mutation = createSlider("Mutation probability", "s_tab0_mutation", 0.00, 1.00, MutateProbability, false, new RoutedPropertyChangedEventHandler<double>(tab0_mutation_ValueChanged));
            border_mutation.Child = dp_mutation;
            sp.Children.Add(border_mutation);


            DockPanel dp_buttons = new DockPanel();
            dp_buttons.LastChildFill = false;

            Border border_buttons = new Border();
            border_buttons.Margin = new Thickness(margin_w, margin_h*3, margin_w, 0);


            //GO button
            Button button_go = createButton("b_tab0_Go", "GO!", Tab0_secondary.Width * 0.3, new RoutedEventHandler(tab0_Go_Click));
            DockPanel.SetDock(button_go, Dock.Left);
            dp_buttons.Children.Add(button_go);

            //EXIT button
            Button button_exit = createButton("b_tab0_Exit", "Exit", Tab0_secondary.Width * 0.3, new RoutedEventHandler(tab0_Exit_Click));
            DockPanel.SetDock(button_exit, Dock.Right);
            dp_buttons.Children.Add(button_exit);


            border_buttons.Child = dp_buttons;
            sp.Children.Add(border_buttons);


            //Add the stackpanel to the secondary area of Tab 0
            Tab0_secondary.Child = sp;
        }


        //-------------------------------------------------------------------------------TAB 2------------------------------------------------------------------------//

        //Create permanent grid layout with check boxes
        public void tab2_primary_permanent()
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
                string dp_name = "dp_tab2_" + i;
                dp.Name = dp_name;

                //Sub Dock panel
                DockPanel dp_sub = new DockPanel();

                //Create checkbox with an event handler
                string cb_name = "cb_tab2_" + i;
                CheckBox cb = createCheckBox(cb_name, new RoutedEventHandler(tab2_SelectParents_Check), i); // TODO: Send chromosome ID not the grid ID 
                cb.HorizontalAlignment = HorizontalAlignment.Right;

                DockPanel.SetDock(cb, Dock.Right);
                dp_sub.Children.Add(cb);

                //Label
                Label l = new Label();
                l.Content = i.ToString();
                l.FontSize = fontsize;
                l.Foreground = Brushes.LightGray;
                l.HorizontalAlignment = HorizontalAlignment.Left;
                dp_sub.Children.Add(l);

                DockPanel.SetDock(dp_sub, Dock.Top);
                dp.Children.Add(dp_sub);

                //Add dockpanel to controls dictionary in order to access and update meshes afterwards (and not recreate the entire grid with checkboxes)
                controls.Add(dp_name, dp);

                //Set the dockpanel as the child of the border element
                border.Child = dp;

                //Add the border to the grid
                Grid.SetRow(border, (int)(i / columnCount));
                Grid.SetColumn(border, i % columnCount);
                grid.Children.Add(border);
            }


            //Add the grid to the primary area of Tab 2
            Tab2_primary.Child = grid;
        }


        public void tab2_primary_update(List<Mesh> meshes)
        {
            //Run through the list of meshes and create a viewport3d control for each
            for(int i=0; i<meshes.Count; i++)
            {
                //The name of the control to add the viewport3d to
                string dp_name = "dp_tab2_" + i;

                //Get this control from the dictionary
                DockPanel dp = (DockPanel) controls[dp_name];

                //If there already is a viewport3d control in the dockpanel then remove it
                if (dp.Children.Count > 1)
                {
                    dp.Children.RemoveAt(dp.Children.Count - 1);
                }

                //Add the new viewport3d control to the dockpanel
                Viewport3d vp3d = new Viewport3d(meshes[i]);
                dp.Children.Add(vp3d);
            }
        }


        public void tab2_secondary_settings()
        {
            StackPanel sp = new StackPanel();

            //Generation info
            Border border_gen = new Border();
            border_gen.Margin = new Thickness(margin_w, margin_h, margin_w, 0);

            Label label_gen = new Label();
            label_gen.SetBinding(ContentProperty, new Binding("Generation"));
            label_gen.DataContext = this;
            label_gen.ContentStringFormat = "GENERATION #{0}";
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
            txt_sel.Inlines.Add(new Bold(new Run("SELECTION")));
            txt_sel.Inlines.Add("\nSelect parent(s) whose genes will be used to create the next design generation via the checkboxes");

            border_sel.Child = txt_sel;
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
            border_cbox.Margin = new Thickness(margin_w, margin_h*3, margin_w, 0);

            List<string> comboboxItems = new List<string>();
            for(int i=0; i<12; i++)
            {
                string itemName = "Design " + i;
                comboboxItems.Add(itemName);
            }
            DockPanel dropdown = createComboBox("SELECT DESIGN", "cbox_tab2_selectDesign", comboboxItems, tab2_Combobox_SelectionChanged);

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

            for (int i=0; i<realGenes.Length; i++)
            {
                string controlName = "tab2_s_gene" + i;
                DockPanel dp_sliderG = createSlider(sliderNames[i], controlName, sliderValuesMin[i], sliderValuesMax[i], realGenes[i], false);

                Slider sliderG = (Slider) controls[controlName];
                sliderG.IsEnabled = false;

                DockPanel.SetDock(dp_sliderG, Dock.Top);
                dp.Children.Add(dp_sliderG);
            }

            DockPanel dp_sliderF = createSlider("Fitness", "tab2_s_fitness", 0.0, 1.0, fitness, false);

            Slider sliderF = (Slider) controls["tab2_s_fitness"];
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

            for (int i = 0; i < realGenes.Length; i++)
            {
                string controlName = "tab2_s_gene" + i;
                Slider sliderG = (Slider) controls[controlName];
                sliderG.IsEnabled = true;
                sliderG.Value = realGenes[i];
                sliderG.IsEnabled = false;
            }

            Slider sliderF = (Slider) controls["tab2_s_fitness"];
            sliderF.IsEnabled = true;
            sliderF.Value = fitness;
            sliderF.IsEnabled = false;
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

            Slider slider = (Slider) controls[controlName];
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

        //Tab 0 Popsize event handler
        private void tab0_popSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider s = (Slider) sender;
            int val = (int) s.Value;
            PopSize = val;
        }

        //Tab 0 MutateProbability event handler
        private void tab0_mutation_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider s = (Slider) sender;
            double val = s.Value;
            MutateProbability = val;
        }


        //Handle event when the "GO!" button is clicked in tab 0       
        public void tab0_Go_Click(object sender, RoutedEventArgs e)
        {
            //Button b_clicked = (Button)sender;

            if (!GO)
            {
                RunInit();

                //Disable sliders in tab 0
                Slider s_popSize = (Slider)controls["s_tab0_popSize"];
                s_popSize.IsEnabled = false;

                Slider s_mutation = (Slider)controls["s_tab0_mutation"];
                s_mutation.IsEnabled = false;

                //Automatically send user to the next tab
                Tab1.IsSelected = true;
            }

            GO = true;
        }


        //Handle event when the "Exit" button is clicked in tab 0       
        public void tab0_Exit_Click(object sender, RoutedEventArgs e)
        {
            //Button b_clicked = (Button)sender;

            Exit();
            
            this.Close();
            
        }


        //One event handler for all checkboxes in tab 2        
        public void tab2_SelectParents_Check(object sender, RoutedEventArgs e)
        {
            CheckBox checkbox = sender as CheckBox;          //Get the checkbox that triggered the event

            if (checkbox.IsChecked == true)
            {
                ParentCount++;

                if (checkbox.Tag != null)
                    population.chromosomes[(int)checkbox.Tag].SetFitness(1.0);

            }
            else
            {
                ParentCount--;

                if (checkbox.Tag != null)
                    population.chromosomes[(int)checkbox.Tag].SetFitness(0.0);

            }

        }


        //Event handler for dropdown menu in tab 2 to select a specific design
        private void tab2_Combobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cbox = (ComboBox) sender;
            int selectedIndex = cbox.SelectedIndex;

            //Change gene sliders according to selection
            tab2_secondary_genesUpdate(population, selectedIndex);                          //TODO: Send chromosome ID not the grid ID

        }


        //Handle event when the "Evolve" button is clicked in tab 2       
        public void tab2_Evolve_Click(object sender, RoutedEventArgs e)
        {
            Button b_clicked = (Button) sender;

            //Test if minimum one parent is selected
            if(ParentCount < 1)
            {
                MessageBoxResult message = MessageBox.Show(this, "Select minimum one parent via the checkboxes");
            }

            else
            {
                //Run now moved to before we start to uncheck checkboxes
                //In order to maintin fitness values
                Run();

                //Extract indexes from names of checked boxes and uncheck all
                for (int i=0; i<12; i++)
                {
                    //The name of the checkbox control
                    string cb_name = "cb_tab2_" + i;

                    //Get this control from the dictionary
                    CheckBox cb = (CheckBox) controls[cb_name];

                    if(cb.IsChecked == true)
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
