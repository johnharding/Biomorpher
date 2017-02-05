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



        // Constructor
        public BiomorpherWindow(BiomorpherComponent Owner)
        {
            // Set the component passed here to a field
            owner = Owner;

            // Get sliders
            sliders = new List<Grasshopper.Kernel.Special.GH_NumberSlider>();
            owner.GetSliders(sliders);
 
            // Initial Window things
            InitializeComponent();
            Topmost = true;

            PopSize = 12;
            MutateProbability = 0.1;
            Generation = 0;
            ParentCount = 0;
            GO = false;
            controls = new Dictionary<string, FrameworkElement>();

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
            population = new Population(popSize, sliders.Count);
            popHistory.AddPop(population);

            // 3. Get geometry for each chromosome
            GetPhenotypes();

            // 4. Setup tab layout
            tab2_primary_permanent();
            tab2_secondary_settings();
            List<Mesh> popMeshes = getRepresentativePhenotypes(population);
            tab2_primary_variable(popMeshes);            
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
            tab2_primary_variable(popMeshes);

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
            int fontsize = 12;
            int margin_w = 20;
            int margin_h = 20;

            //Container for all the controls
            StackPanel sp = new StackPanel();


            //Create sliders with labels
            Border border_popSize = new Border();
            border_popSize.Margin = new Thickness(margin_w, margin_h, margin_w, 0);
            DockPanel dp_popSize = createSlider("Population size", "s_tab0_popSize", 12, 500, 100, true);
            border_popSize.Child = dp_popSize;
            sp.Children.Add(border_popSize);

            Border border_mutation = new Border();
            border_mutation.Margin = new Thickness(margin_w, margin_h, margin_w, 0);
            DockPanel dp_mutation = createSlider("Mutation probability", "s_tab0_mutation", 0.00, 1.00, 0.10, false);
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
                border.Padding = new Thickness(5);

                //Dock panel
                DockPanel dp = new DockPanel();
                string dp_name = "dp_tab2_" + i;
                dp.Name = dp_name;

                //Create checkbox with an event handler
                string cb_name = "cb_tab2_" + i;
                CheckBox cb = createCheckBox(cb_name, new RoutedEventHandler(tab2_SelectParents_Check), i); // TODO: Send chromosome ID not the grid ID 
                cb.HorizontalAlignment = HorizontalAlignment.Right;
 
                DockPanel.SetDock(cb, Dock.Top);
                dp.Children.Add(cb);

                //Add dockpanel to controls dictionary in order to access and update meshes afterwards (and not recreate the entire grid with checkboxes)
                controls.Add(dp_name, dp);
                controls.Add(cb_name, cb);

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


        public void tab2_primary_variable(List<Mesh> meshes)
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
            int fontsize = 12;

            int margin_w = 20;
            int margin_h = 20;

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


            //Add the stackpanel to the secondary area of Tab 2
            Tab2_secondary.Child = sp;
        }


        //-------------------------------------------------------------------------------CREATE CONTROLS------------------------------------------------------------------------//

        //Create Grid control
        public Grid createGrid(int rowCount, int columnCount, double width, double height)
        {
            Grid grid = new Grid();
            grid.Width = width;
            grid.Height = height;
            grid.ShowGridLines = true;

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


        //-------------------------------------------------------------------------------EVENT HANDLERS------------------------------------------------------------------------//

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
                //Create list of selected parent indexes
                //List<int> selectedParentIndexes = new List<int>(); (now not required... I think)

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
                        //selectedParentIndexes.Add(i);
                        cb.IsChecked = false;
                    }
                }


                //Set parent count to zero
                ParentCount = 0;

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
