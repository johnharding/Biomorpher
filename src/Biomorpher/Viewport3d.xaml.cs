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

namespace Biomorpher
{
    /// <summary>
    /// Interaction logic for Viewport3d.xaml
    /// </summary>
    public partial class Viewport3d : UserControl
    {
        public Viewport3d(Mesh mesh)
        {
            InitializeComponent();
            create3DViewPort(mesh);
        }


        private void create3DViewPort(Mesh mesh)
        {
            var hVp3D = new HelixViewport3D();

            //Settings
            hVp3D.ShowFrameRate = false;
            hVp3D.ViewCubeOpacity = 0.2;
            hVp3D.ViewCubeTopText = "T";
            hVp3D.ViewCubeBottomText = "B";
            hVp3D.ViewCubeFrontText = "E";
            hVp3D.ViewCubeRightText = "N";
            hVp3D.ViewCubeLeftText = "S";
            hVp3D.ViewCubeBackText = "W";

            var lights = new DefaultLights();
            hVp3D.Children.Add(lights);


            //Windows geometry objects
            MeshGeometry3D mesh_w = new MeshGeometry3D();
            Point3DCollection pts_w = new Point3DCollection();

            if (mesh != null)
            {
                //define vertices
                for (int i = 0; i < mesh.Vertices.Count; i++)
                {
                    pts_w.Add(new Point3D(mesh.Vertices[i].X, mesh.Vertices[i].Y, mesh.Vertices[i].Z));
                }

                mesh_w.Positions = pts_w;

                //define faces
                for (int i = 0; i < mesh.Faces.Count; i++)
                {
                    mesh_w.TriangleIndices.Add(mesh.Faces[i].A);
                    mesh_w.TriangleIndices.Add(mesh.Faces[i].B);
                    mesh_w.TriangleIndices.Add(mesh.Faces[i].C);
                }
            }


            //Create material and add geometry to viewport
            DiffuseMaterial material = new DiffuseMaterial(Brushes.GreenYellow);
            GeometryModel3D model = new GeometryModel3D(mesh_w, material);
            ModelVisual3D vis = new ModelVisual3D();
            vis.Content = model;

            hVp3D.Children.Add(vis);
            hVp3D.ZoomExtentsWhenLoaded = true;

            //Add viewport to user control
            this.AddChild(hVp3D);
        }


    }
}
