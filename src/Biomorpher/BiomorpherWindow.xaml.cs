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

namespace Biomorpher
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class BiomorpherWindow : Window
    {

        // Fields
        private int generation;
        private int popSize;
        private Population population;
        private PopHistory popHistory;
        private List<Grasshopper.Kernel.Special.GH_NumberSlider> sliders;
        private bool GO;

        Grid myGrid;
        List<UserControl1> myUserControls;

        // Constructor
        public BiomorpherWindow(BiomorpherComponent Owner)
        {
            // Get sliders
            sliders = new List<Grasshopper.Kernel.Special.GH_NumberSlider>();
            Owner.GetSliders(sliders);
            
            // GA things
            generation = 0;
            popSize = 12; // TODO: The population number needs to come from the user
            population = new Population(popSize);
            popHistory = new PopHistory();

            // Get geometry for each chromosome in the initial population
            for (int i = 0; i < population.chromosomes.Length; i++)
            {
                Owner.SetSliders(population.chromosomes[i], sliders);
                Owner.ExpireSolution(true); // This may not work! We have to expire to get the geometry to update after altering sliders
                Owner.GetGeometry(population.chromosomes[i]);
            }

            // Initial Window things
            InitializeComponent();
            Topmost = true;
            myUserControls = new List<UserControl1>();
            
            // 1. create the grid?
            // 2. display initial population here
            // 3. add user controls?
            // 4. add them as children to this window?

        }


        /// <summary>
        /// When this gets called (probably via a button being triggered) we advance a generation 
        /// </summary>
        public void Run()
        {
            // 1. Create new populaltion using user selection

            // 2. Mutate population using user preferences

            // 3. Set sliders and get geometry for each chromosome

            // 4. Advance the generation counter and store the population historically.
            popHistory.AddPop(population);
            generation++;
            
            // 5. Visualise the current population

        }


        public void Exit()
        {
            // Set sliders and get geometry for a chosen chromosome

            // Close the window
        }


    }
}
