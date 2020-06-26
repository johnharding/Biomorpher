using Biomorpher.IGA;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;

namespace Biomorpher
{
    /// <summary>
    /// Interaction logic for ViewportBasic.xaml
    /// </summary>
    public partial class ViewportBasic : UserControl
    {

        //private Viewport3D myViewport;
        private HelixViewport3D myViewport;
        private Rect3D bounds {get; set;}
        private Chromosome thisDesign;
        private BiomorpherWindow Owner;

        /// <summary>
        /// Creates a simple viewport without helix
        /// </summary>
        /// <param name="chromo"></param>
        /// <param name="owner"></param>
        public ViewportBasic(Chromosome chromo, BiomorpherWindow owner)
        {
            InitializeComponent();

            Owner = owner;
            thisDesign = chromo;
            this.ToolTip = "double click to display in main viewport";

            myViewport = new HelixViewport3D();
            myViewport.ZoomExtentsWhenLoaded = true;
            myViewport.ShowViewCube = false;
            DefaultLights lights = new DefaultLights();
            myViewport.Children.Add(lights);

            List<Mesh> rMesh = thisDesign.phenoMesh;
            List<PolylineCurve> polys = thisDesign.phenoPoly;

            List<ModelVisual3D> vis = new List<ModelVisual3D>();

            for (int i = 0; i < rMesh.Count; i++)
            {
                if (rMesh[i] != null)
                {
                    MeshGeometry3D wMesh = new MeshGeometry3D();
                    DiffuseMaterial material = new DiffuseMaterial();
                    Friends.ConvertRhinotoWpfMesh(rMesh[i], wMesh, material);

                    GeometryModel3D model = new GeometryModel3D(wMesh, material);

                    model.BackMaterial = material;

                    // DirectionalLight myLight = new DirectionalLight(Colors.White, new Vector3D(-0.5, -1, -1));

                    Model3DGroup modelGroup = new Model3DGroup();
                    modelGroup.Children.Add(model);
                    //modelGroup.Children.Add(myLight);
                    ModelVisual3D v = new ModelVisual3D();
                    v.Content = modelGroup;
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
                        line.Points.Add(new Point3D(result[j + 1].X, result[j + 1].Y, result[j + 1].Z));
                    }
                    vis.Add(line);
                }
            }

            for (int i = 0; i < vis.Count; i++)
            {
                myViewport.Children.Add(vis[i]);
            }

            myViewport.IsEnabled = false;

            //Add viewport to user control
            this.AddChild(myViewport);

        }

        /// <summary>
        /// Double click to set the Grasshopper instance
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            Owner.SetInstance(thisDesign);
        }
    }
}
