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

namespace Biomorpher
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class BiomorpherWindow : Window
    {
        Grid myGrid;
        List<UserControl1> myUserControls;

        public BiomorpherWindow(List<Mesh> myMeshes)
        {

            this.InitializeComponent();
            this.Topmost = true;

            myUserControls = new List<UserControl1>();

            if (myMeshes != null)
            {
                for (int i = 0; i < myMeshes.Count; i++)
                {
                    myUserControls.Add(new UserControl1(myMeshes[i]));
                }
            }

            // Create the Grid
            myGrid = new Grid();
            MakeGrid(4, 3);


            // Add the user controls (the sub-windows)
            for (int i = 0; i < myUserControls.Count; i++)
            {
                Grid.SetColumn(myUserControls[i], i % 4);
                Grid.SetRow(myUserControls[i], (int)(i / 4));

                myGrid.Children.Add(myUserControls[i]);
            }

            this.AddChild(myGrid);
        }


        void MakeGrid(int sizeX, int sizeY)
        {
            myGrid.Width = 1270;
            myGrid.Height = 684;
            myGrid.HorizontalAlignment = HorizontalAlignment.Left;
            myGrid.VerticalAlignment = VerticalAlignment.Top;
            myGrid.ShowGridLines = true;

            Thickness myThickness = new Thickness();
            myThickness.Bottom = 2;
            myThickness.Left = 2;
            myThickness.Right = 2;
            myThickness.Top = 2;
            this.BorderThickness = myThickness;

            // Define the Columns
            int COLNUM = 4;
            List<ColumnDefinition> myCols = new List<ColumnDefinition>();
            for (int i = 0; i < COLNUM; i++)
                myCols.Add(new ColumnDefinition());

            for (int i = 0; i < COLNUM; i++)
                myGrid.ColumnDefinitions.Add(myCols[i]);

            // Define the Rows
            int ROWNUM = 3;
            List<RowDefinition> myRows = new List<RowDefinition>();
            for (int i = 0; i < ROWNUM; i++)
                myRows.Add(new RowDefinition());

            for (int i = 0; i < ROWNUM; i++)
                myGrid.RowDefinitions.Add(myRows[i]);
        }


    }
}
