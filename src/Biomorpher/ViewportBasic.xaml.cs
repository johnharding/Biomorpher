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
        public ViewportBasic(Mesh mesh)
        {
            InitializeComponent();

            Viewport3D myViewport = new Viewport3D();
            MeshGeometry3D mesh_w = new MeshGeometry3D();
            Point3DCollection pts_w = new Point3DCollection();

            double aveA = 0;
            double aveR = 0;
            double aveG = 0;
            double aveB = 0;

            if (mesh != null)
            {
                //define vertices
                for (int i = 0; i < mesh.Vertices.Count; i++)
                {
                    mesh_w.Positions.Add(new Point3D(mesh.Vertices[i].X, mesh.Vertices[i].Y, mesh.Vertices[i].Z));
                }

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

            if (mesh.VertexColors.Count > 0)
            {

                aveA /= mesh.VertexColors.Count;
                aveR /= mesh.VertexColors.Count;
                aveG /= mesh.VertexColors.Count;
                aveB /= mesh.VertexColors.Count;

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


            // Bits of the model
            GeometryModel3D model = new GeometryModel3D(mesh_w, material);
            model.BackMaterial = backmaterial;
            DirectionalLight myLight = new DirectionalLight(Colors.White, new Vector3D(-0.5, -1, -1));

            // ModelGroup
            Model3DGroup modelGroup = new Model3DGroup();
            modelGroup.Children.Add(model);
            modelGroup.Children.Add(myLight);
            
            // ModelVisual
            ModelVisual3D vis = new ModelVisual3D();
            vis.Content = modelGroup;

            // Viewport
            myViewport.Children.Add(vis);
            myViewport.Camera = new PerspectiveCamera(new Point3D(200, 200, 200), new Vector3D(-1, -1, -1), new Vector3D(0, 0, 1), 30);

            // Add to UserControl
            this.AddChild(myViewport);

        }
    }
}
