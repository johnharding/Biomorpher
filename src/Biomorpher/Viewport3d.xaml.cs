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
using System.Windows.Navigation;
using System.Windows.Shapes;
using HelixToolkit.Wpf;
using System.Windows.Media.Media3D;
using Rhino.Geometry;
using Biomorpher.IGA;

namespace Biomorpher
{
    /// <summary>
    /// Interaction logic for Viewport3d.xaml
    /// </summary>
    public partial class Viewport3d : UserControl
    {
        private int ID;
        private BiomorpherWindow W;
        private HelixViewport3D hVp3D;

        public Viewport3d(Mesh mesh, int id, BiomorpherWindow w, bool hasViewcube)
        {
            ID = id;
            W = w;
            InitializeComponent();
            create3DViewPort(mesh, hasViewcube);
        }

        private void create3DViewPort(Mesh mesh, bool hasViewcube)
        {
            hVp3D = new HelixViewport3D();
 
            //Settings
            hVp3D.ShowFrameRate = false;
            hVp3D.ViewCubeOpacity = 0.1;
            hVp3D.ViewCubeTopText = "T";
            hVp3D.ViewCubeBottomText = "B";
            hVp3D.ViewCubeFrontText = "E";
            hVp3D.ViewCubeRightText = "N";
            hVp3D.ViewCubeLeftText = "S";
            hVp3D.ViewCubeBackText = "W";
            hVp3D.ViewCubeHeight = 40;
            hVp3D.ViewCubeWidth = 40;
            hVp3D.ShowViewCube = hasViewcube;
            var lights = new DefaultLights();
            hVp3D.Children.Add(lights);
            hVp3D.IsInertiaEnabled = true;
            //hVp3D.Camera = Friends.dummyHelix.Camera;

            //Windows geometry objects
            MeshGeometry3D mesh_w = new MeshGeometry3D();
            Point3DCollection pts_w = new Point3DCollection();
            //GradientStopCollection gsp = new GradientStopCollection();
            //int vertexCount = mesh.Vertices.Count;
            double aveA = 0;
            double aveR = 0;
            double aveG = 0;
            double aveB = 0;

            // TODO: Use Helix own meshgeometry wrap
            //MeshGeometryVisual3D jimmy = new MeshGeometryVisual3D();
          
            if (mesh != null)
            {
                //define vertices
                for (int i = 0; i < mesh.Vertices.Count; i++)
                {
                    pts_w.Add(new Point3D(mesh.Vertices[i].X, mesh.Vertices[i].Y, mesh.Vertices[i].Z));
                    
                }

                mesh_w.Positions = pts_w;

                //gsp.Clear();
                //gsp.Add(new GradientStop(Colors.Red, 0.0));
                //gsp.Add(new GradientStop(Colors.Blue, 1.0));

                
                /*
                //define vertex colouring
                for (int i = 0; i < mesh.VertexColors.Count; i++)
                {
                    //mesh_w.TextureCoordinates.Add(new System.Windows.Point(((double)i) / (double)mesh.VertexColors.Count-1, 1));
                    //gsp.Add(new GradientStop(Color.FromArgb(mesh.VertexColors[i].A, mesh.VertexColors[i].R, mesh.VertexColors[i].G, mesh.VertexColors[i].B), ((double)i)/mesh.VertexColors.Count-1));
                    //mesh_w.TextureCoordinates.Add(new System.Windows.Point(i / mesh.VertexColors.Count, 1));
                    //gsp.Add(new GradientStop(Color.FromArgb(mesh.VertexColors[i].A, mesh.VertexColors[i].R, mesh.VertexColors[i].G, mesh.VertexColors[i].B), i));
                    //mesh_w.TextureCoordinates.Add(new System.Windows.Point(0, Friends.GetRandomDouble()));
                    //mesh_w.TextureCoordinates.Add(new System.Windows.Point(0, 0.1));
                    //mesh_w.TextureCoordinates.Add(new System.Windows.Point(0, 0.9));

                    double val = (double)i / (double)(mesh.VertexColors.Count+1);
                    mesh_w.TextureCoordinates.Add(new System.Windows.Point(0, val));

                    gsp.Add(new GradientStop(Color.FromRgb(mesh.VertexColors[i].R, mesh.VertexColors[i].G, mesh.VertexColors[i].B), val));
                }

                */
                
                for (int i = 0; i < mesh.VertexColors.Count; i++)
                {
                    aveA += mesh.VertexColors[i].A;
                    aveR += mesh.VertexColors[i].R;
                    aveG += mesh.VertexColors[i].G;
                    aveB += mesh.VertexColors[i].B;
                }


                //define faces - triangulation only
                for (int i = 0; i < mesh.Faces.Count; i++)
                {
                    mesh_w.TriangleIndices.Add(mesh.Faces[i].A);
                    mesh_w.TriangleIndices.Add(mesh.Faces[i].B);
                    mesh_w.TriangleIndices.Add(mesh.Faces[i].C);
                }
            }
            
             

            //Create material and add geometry to viewport
            DiffuseMaterial material;
            DiffuseMaterial backmaterial;
            //LinearGradientBrush lBrsh = new LinearGradientBrush(gsp, new System.Windows.Point(0.0, 0.0), new System.Windows.Point(0, 1.0));

            if (mesh.VertexColors.Count > 0)
            {

                aveA/=mesh.VertexColors.Count;
                aveR/=mesh.VertexColors.Count;
                aveG/=mesh.VertexColors.Count;
                aveB/=mesh.VertexColors.Count;

                var avebrush = new SolidColorBrush(Color.FromArgb((byte)aveA, (byte)aveR, (byte)aveG, (byte)aveB));
                material = new DiffuseMaterial(avebrush);
                backmaterial = new DiffuseMaterial(avebrush);
            }

            else
            {
                var brush = new SolidColorBrush(Color.FromArgb(220, (byte)51, (byte)188, (byte)188));
                material = new DiffuseMaterial(brush);
                backmaterial = new DiffuseMaterial(Brushes.LightGray);
            }

            GeometryModel3D model = new GeometryModel3D(mesh_w, material);
            model.BackMaterial = backmaterial;
            ModelVisual3D vis = new ModelVisual3D();
            vis.Content = model;

            hVp3D.Children.Add(vis);
            hVp3D.ZoomExtentsWhenLoaded = true;

            //Add viewport to user control
            this.AddChild(hVp3D);

            /*
            ContextMenu myMenu = new ContextMenu();

            MenuItem item1 = new MenuItem();
            MenuItem item2 = new MenuItem();

            item1.Header = "item1";
            //item1.Click += new RoutedEventHandler(item1_Click);
            myMenu.Items.Add(item1);

            item2.Header = "item2";
            //item2.Click += new RoutedEventHandler(item2_Click);
            myMenu.Items.Add(item2);

            //this.ContextMenu = myMenu;
            //myMenu.IsOpen = true;
            hVp3D.ContextMenu = myMenu;
             */

        }


        public void SetCamera(ProjectionCamera cam)
        {
            hVp3D.Camera = cam;
        }

        public ProjectionCamera GetCamera()
        {
            return hVp3D.Camera;
        }


        /// <summary>
        /// Matches this camera to the others
        /// </summary>
        public void MatchCamera()
        {
            try
            {
                for (int i = 0; i < 12; i++)
                {
                    string dp_name = "dp_tab2_" + i;

                    DockPanel dp = (DockPanel)W.GetControls()[dp_name];
   
                    Viewport3d myViewport = (Viewport3d)dp.Children[1];
                    myViewport.SetCamera(this.GetCamera());

                }
            }
            catch
            {
            }

        }

        /// <summary>
        /// Double click to set the Grasshopper instance
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
        {
            base.OnMouseDoubleClick(e);

            Population pop = W.GetPopulation();

            foreach (Chromosome jimmy in pop.chromosomes)
            {
                if(jimmy.isRepresentative && jimmy.clusterId==ID)
                {
                    W.SetInstance(jimmy);
                    break;
                }
            }

        }

        /*
        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseRightButtonDown(e);
        }
         */

    }
}
