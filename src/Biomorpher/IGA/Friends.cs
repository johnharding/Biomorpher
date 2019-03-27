using HelixToolkit.Wpf;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace Biomorpher.IGA
{
    /// <summary>
    /// Some static things to help us out
    /// </summary>
    public static class Friends
    {

        /// <summary>
        /// A dummy helix viewport, used to share the camera settings
        /// </summary>
        public static HelixViewport3D dummyHelix = new HelixViewport3D();
        private static System.Globalization.CultureInfo ci = new System.Globalization.CultureInfo("en-us");

        /// <summary>
        /// Biomorpher version
        /// </summary>
        /// <returns>returns the version number</returns>
        public static string VerionInfo()
        {
            return "0.6.1";
        }


        /// <summary>
        /// Master random number generator
        /// </summary>
        private static readonly Random getrandom = new Random(21);

        /// <summary>
        /// Synclock for single threading
        /// </summary>
        private static readonly object syncLock = new object();
        
        /// <summary>
        /// Returns a random integer within a domain
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static int GetRandomInt(int min, int max)
        {
            lock (syncLock)
            {
                int value = getrandom.Next(min, max);
                return value;
            }
        }



        /// <summary>
        /// Returns a double between [0.0, 1.0)
        /// </summary>
        /// <returns></returns>
        public static double GetRandomDouble()
        {
            lock (syncLock)
            {
                double value = getrandom.NextDouble();
                return value;
            }
        }

        /// <summary>
        /// Returns an open bezier curve pathgeometry with 4 points
        /// </summary>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="P3"></param>
        /// <param name="P4"></param>
        /// <returns></returns>
        public static PathGeometry MakeBezierGeometry(System.Windows.Point P1, System.Windows.Point P2, System.Windows.Point P3, System.Windows.Point P4)
        {
            BezierSegment myBezier = new BezierSegment(P2, P3, P4, true);
            PathFigure myPathFigure = new PathFigure();
            myPathFigure.StartPoint = P1;
            myPathFigure.Segments.Add(myBezier);
            PathGeometry myPathGeometry = new PathGeometry();
            myPathGeometry.Figures.Add(myPathFigure);
            return myPathGeometry;
        }


        /// <summary>
        /// A default Rhino Mesh object used for debugging
        /// </summary>
        /// <returns></returns>
        public static Mesh SampleMesh()
        {
            Mesh sampleMesh = new Mesh();
            sampleMesh.Vertices.Add(200, 200, 200);
            sampleMesh.Vertices.Add(200, 600, 200);
            sampleMesh.Vertices.Add(400, 300, 400);
            sampleMesh.Faces.AddFace(0, 1, 2);
            return sampleMesh;
        }

        /// <summary>
        /// Exports a canvas to an image png file
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="filename"></param>
        public static void CreateSaveBitmap(Canvas canvas, string filename)
        {

            RenderTargetBitmap renderBitmap = new RenderTargetBitmap((int)(canvas.Width * 3.125), (int)(canvas.Height * 3.125), 300d, 300d, PixelFormats.Pbgra32);

            renderBitmap.Render(canvas);

            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

            using (Stream file = File.Create(filename))
            {
                encoder.Save(file);
            }

        }

        /// <summary>
        /// Calculate Euclidean distance
        /// </summary>
        /// <param name="genes"></param>
        /// <param name="mean"></param>
        /// <returns></returns>
        public static double calcDistance(double[] genes, double[] mean)
        {
            double dist = 0.0;

            for (int i = 0; i < genes.Length; i++)
            {
                dist += Math.Pow((genes[i] - mean[i]), 2);
            }

            return Math.Sqrt(dist);
        }



        /// <summary>
        /// Copies the rhinoMesh data to a new wpf mesh
        /// </summary>
        /// <param name="rhinoMesh"></param>
        /// <param name="wpfMesh"></param>
        /// <param name="material"></param>
        public static void ConvertRhinotoWpfMesh(Mesh rhinoMesh, MeshGeometry3D wpfMesh, DiffuseMaterial material)
        {
            // Make sure to start afresh
            //wpfMesh = new MeshGeometry3D();
            //material = new DiffuseMaterial();

            //define vertices
            for (int i = 0; i < rhinoMesh.Vertices.Count; i++)
            {
                /* TODO SharpDX update
                SharpDX.Vector3 me;
                me.X = rhinoMesh.Vertices[i].X;
                me.Y = rhinoMesh.Vertices[i].Y;
                me.Z = rhinoMesh.Vertices[i].Z;
                wpfMesh.Positions.Add(me);
                */
                 
                wpfMesh.Positions.Add(new Point3D(rhinoMesh.Vertices[i].X, rhinoMesh.Vertices[i].Y, rhinoMesh.Vertices[i].Z));
            }

            //define faces - triangulation only
            for (int i = 0; i < rhinoMesh.Faces.Count; i++)
            {
                wpfMesh.TriangleIndices.Add(rhinoMesh.Faces[i].A);
                wpfMesh.TriangleIndices.Add(rhinoMesh.Faces[i].B);
                wpfMesh.TriangleIndices.Add(rhinoMesh.Faces[i].C);
            }

            // Get colours
            double aveA = 0;
            double aveR = 0;
            double aveG = 0;
            double aveB = 0;

            for (int i = 0; i < rhinoMesh.VertexColors.Count; i++)
            {
                aveA += rhinoMesh.VertexColors[i].A;
                aveR += rhinoMesh.VertexColors[i].R;
                aveG += rhinoMesh.VertexColors[i].G;
                aveB += rhinoMesh.VertexColors[i].B;
            }

            if (rhinoMesh.VertexColors.Count > 0)
            {
                aveA /= rhinoMesh.VertexColors.Count;
                aveR /= rhinoMesh.VertexColors.Count;
                aveG /= rhinoMesh.VertexColors.Count;
                aveB /= rhinoMesh.VertexColors.Count;

                material.Brush = new SolidColorBrush(Color.FromArgb((byte)aveA, (byte)aveR, (byte)aveG, (byte)aveB));
            }

            else
            {
                material.Brush = new SolidColorBrush(Color.FromArgb((byte)220, (byte)51, (byte)188, (byte)188));
            }
        }


        /// <summary>
        /// https://stackoverflow.com/questions/374316/round-a-double-to-x-significant-figures
        /// </summary>
        /// <param name="d"></param>
        /// <param name="digits"></param>
        /// <returns></returns>
        public static double RoundToSignificantDigits(this double d, int digits)
        {
            if (d == 0)
                return 0;

            double scale = Math.Pow(10, Math.Floor(Math.Log10(Math.Abs(d))) + 1);
            return scale * Math.Round(d / scale, digits);
        }


        /// <summary>
        /// https://stackoverflow.com/questions/374316/round-a-double-to-x-significant-figures
        /// </summary>
        /// <param name="d"></param>
        /// <param name="digits"></param>
        /// <returns></returns>
        public static double TruncateToSignificantDigits(this double d, int digits)
        {
            if (d == 0)
                return 0;

            double scale = Math.Pow(10, Math.Floor(Math.Log10(Math.Abs(d))) + 1 - digits);
            return scale * Math.Truncate(d / scale);
        }

        /// <summary>
        /// Generates an axis label appropriate for the graph plotter
        /// </summary>
        /// <param name="theValue"></param>
        /// <returns></returns>
        public static string AxisLabelText(double theValue)
        {
            String theText;

            if (theValue > 99999999 || theValue < -9999999)
            {
                theText = theValue.ToString("E02", ci);
            }

            else
            {
                if (theValue < 1.0 && theValue > -1.0)
                {
                    theText = Friends.TruncateToSignificantDigits(theValue, 7).ToString();
                }
                else
                {
                    theText = Friends.RoundToSignificantDigits(theValue, 8).ToString();
                }
            }

            return theText + " ";
        }

    }

}
