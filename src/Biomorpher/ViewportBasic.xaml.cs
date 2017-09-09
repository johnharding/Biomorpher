using Biomorpher.IGA;
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
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Biomorpher
{
    /// <summary>
    /// Interaction logic for ViewportBasic.xaml
    /// </summary>
    public partial class ViewportBasic : UserControl
    {
        
        private Viewport3D myViewport;
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

            myViewport = new Viewport3D();

            List<Mesh> rMesh = thisDesign.phenotype;
            List<ModelVisual3D> vis = new List<ModelVisual3D>();

            Model3DGroup boundsGroup = new Model3DGroup();

            for (int i = 0; i < rMesh.Count; i++)
            {
                if (rMesh[i] != null)
                {
                    MeshGeometry3D wMesh = new MeshGeometry3D();
                    DiffuseMaterial material = new DiffuseMaterial();
                    Friends.ConvertRhinotoWpfMesh(rMesh[i], wMesh, material);

                    GeometryModel3D model = new GeometryModel3D(wMesh, material);

                    model.BackMaterial = material;

                    DirectionalLight myLight = new DirectionalLight(Colors.White, new Vector3D(-0.5, -1, -1));

                    // ModelGroup
                    Model3DGroup modelGroup = new Model3DGroup();
                    modelGroup.Children.Add(model);
                    modelGroup.Children.Add(myLight);
                    ModelVisual3D v = new ModelVisual3D();
                    v.Content = modelGroup;
                    vis.Add(v);

                    boundsGroup.Children.Add(model);

                }
            }

            bounds = boundsGroup.Bounds;

            for (int i = 0; i < vis.Count; i++)
            {
                myViewport.Children.Add(vis[i]);
            }

            ZoomExtents();

            //Add viewport to user control
            this.AddChild(myViewport);

        }

        /// <summary>
        /// Zoom to extent of mesh
        /// </summary>
        public void ZoomExtents()
        {

            // find centre and origin of bounding box
            Point3d cen = new Point3d(bounds.X + bounds.SizeX / 2, bounds.Y + bounds.SizeY / 2, bounds.Z + bounds.SizeZ / 2);

            // Find distances to corners of bounding box
            double directrixX = cen.DistanceTo(new Point3d(bounds.X + bounds.SizeX, 0d, 0d));
            double directrixY = cen.DistanceTo(new Point3d(0d, bounds.Y + bounds.SizeY, 0d));
            double directrixZ = cen.DistanceTo(new Point3d(0d, 0d, bounds.Z + bounds.SizeZ));

            // Find the radius of the camera sphere
            double sphereRad = directrixX;
            if (directrixY > sphereRad) sphereRad = directrixY;
            if (directrixZ > sphereRad) sphereRad = directrixZ;

            // Now set the camera based on this max view sphere rad
            double camX = cen.X + sphereRad;
            double camY = cen.Y + sphereRad;
            double camZ = cen.Z + sphereRad;

            // Find the orthowidth
            double orthoWidth = bounds.SizeX;
            if (bounds.SizeY > orthoWidth) orthoWidth = bounds.SizeY;
            if (bounds.SizeZ > orthoWidth) orthoWidth = bounds.SizeZ;
            orthoWidth *= 1.618; //phi?

            //myViewport.Camera = new PerspectiveCamera(new Point3D(camX, camY, camZ), new Vector3D(cen.X - camX, cen.Y - camY, cen.Z - camZ), new Vector3D(0, 0, 1), 30);
            myViewport.Camera = new OrthographicCamera(new Point3D(camX, camY, camZ), new Vector3D(cen.X - camX, cen.Y - camY, cen.Z - camZ), new Vector3D(0, 0, 1), orthoWidth);

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
