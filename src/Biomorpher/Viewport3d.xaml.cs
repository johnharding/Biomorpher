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
        //HelixToolkit.Wpf.SharpDX.Viewport3DX hVp3D;

        /// <summary>
        /// Our own Viewport3d class here
        /// </summary>
        /// <param name="meshes"></param>
        /// <param name="polys"></param>
        /// <param name="id"></param>
        /// <param name="w"></param>
        /// <param name="hasViewcube"></param>
        public Viewport3d(List<Mesh> meshes, List<PolylineCurve> polys, int id, BiomorpherWindow w, bool hasViewcube)
        {
            ID = id;
            W = w;
            InitializeComponent();
            create3DViewPort(meshes, polys, hasViewcube);
        }

        private void create3DViewPort(List<Mesh> meshes, List<PolylineCurve> polys, bool hasViewcube)
        {
            hVp3D = new HelixViewport3D();
            //hVp3D = new HelixToolkit.Wpf.SharpDX.Viewport3DX();
 
            //Settings
            hVp3D.ShowFrameRate = false;
            //hVp3D.ViewCubeOpacity = 0.1;
            hVp3D.ViewCubeTopText = "T";
            hVp3D.ViewCubeBottomText = "B";
            hVp3D.ViewCubeFrontText = "E";
            hVp3D.ViewCubeRightText = "N";
            hVp3D.ViewCubeLeftText = "S";
            hVp3D.ViewCubeBackText = "W";
            hVp3D.ViewCubeHeight = 40;
            hVp3D.ViewCubeWidth = 40;
            hVp3D.ShowViewCube = hasViewcube;
            DefaultLights lights = new DefaultLights();
            hVp3D.Children.Add(lights);
            hVp3D.IsInertiaEnabled = true;
            hVp3D.ZoomExtentsWhenLoaded = true;

            List<ModelVisual3D> vis = new List<ModelVisual3D>();

            for (int i = 0; i < meshes.Count; i++)
            {
                if(meshes[i] != null)
                {
                    MeshGeometry3D wMesh = new MeshGeometry3D();
                    DiffuseMaterial material = new DiffuseMaterial();
                    Friends.ConvertRhinotoWpfMesh(meshes[i], wMesh, material);
                    GeometryModel3D model = new GeometryModel3D(wMesh, material);

                    model.BackMaterial = material;
                    ModelVisual3D v = new ModelVisual3D();
                    v.Content = model;

                    vis.Add(v);
                }
            }

            for (int i = 0; i < polys.Count; i++)
            {
                if (polys[i] != null)
                {
                    LinesVisual3D line = new LinesVisual3D();
                    line.Color = Colors.Black;
                    line.Thickness = 1;

                    Rhino.Geometry.Polyline result = new Rhino.Geometry.Polyline();
                    polys[i].TryGetPolyline(out result);

                    for (int j = 0; j < result.Count - 1; j++)
                    {
                        line.Points.Add(new Point3D(result[j].X, result[j].Y, result[j].Z));
                        line.Points.Add(new Point3D(result[j+1].X, result[j+1].Y, result[j+1].Z));
                    }
                    vis.Add(line);
                }
            }
                    

            for (int i = 0; i < vis.Count; i++)
            {
                hVp3D.Children.Add(vis[i]);
            }

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

        /// <summary>
        /// Sets the camera
        /// </summary>
        /// <param name="cam"></param>
        public void SetCamera(ProjectionCamera cam)
        {
            hVp3D.Camera = cam;
        }

        /// <summary>
        /// Returns the camera
        /// </summary>
        /// <returns></returns>
        public ProjectionCamera GetCamera()
        {
            return hVp3D.Camera;
        }

        /// <summary>
        /// Enables the current helix viewport
        /// </summary>
        public void Enable()
        {
            hVp3D.IsEnabled = true;
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
   
                    // Noting that the first child of dp is the performance bar, hence Children[1]
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
            hVp3D.ZoomExtents();
        }
        */

    }
}
