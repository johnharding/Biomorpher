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
using System.Windows.Shapes;

namespace Biomorpher.IGA
{
    /// <summary>
    /// Some static things to help us out
    /// </summary>
    public static class Friends
    {

        /// <summary>
        /// Biomorpher version
        /// </summary>
        /// <returns></returns>
        public static string VerionInfo()
        {
            return "0.1.1";
        }


        //Function to get random number
        private static readonly Random getrandom = new Random(21);
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
            { // synchronize
                return getrandom.Next(min, max);
            }
        }

        /// <summary>
        /// Returns a double between [0.0, 1.0)
        /// </summary>
        /// <returns></returns>
        public static double GetRandomDouble()
        {
            lock (syncLock)
            { // synchronize
                return getrandom.NextDouble();
            }
        }

        /// <summary>
        /// Makes an open bezier curve pathgeometry with 4 points
        /// </summary>
        /// <param name="P1x"></param>
        /// <param name="P1y"></param>
        /// <param name="P2x"></param>
        /// <param name="P2y"></param>
        /// <param name="P3x"></param>
        /// <param name="P3y"></param>
        /// <param name="P4x"></param>
        /// <param name="P4y"></param>
        /// <returns></returns>
        public static PathGeometry MakeBezierGeometry(double P1x, double P1y, double P2x, double P2y, double P3x, double P3y, double P4x, double P4y)
        {
            BezierSegment myBezier = new BezierSegment(new System.Windows.Point(P2x, P2y), new System.Windows.Point(P3x, P3y), new System.Windows.Point(P4x, P4y), true);
            PathFigure myPathFigure = new PathFigure();
            myPathFigure.StartPoint = new System.Windows.Point(P1x, P1y);
            myPathFigure.Segments.Add(myBezier);
            PathGeometry myPathGeometry = new PathGeometry();
            myPathGeometry.Figures.Add(myPathFigure);
            return myPathGeometry;
        }

        /// <summary>
        /// A default Rhino Mesh object
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


        public static void CreateSaveBitmap(Canvas canvas, string filename)
        {


            RenderTargetBitmap renderBitmap = new RenderTargetBitmap((int)canvas.Width, (int)canvas.Height, 96d, 96d, PixelFormats.Pbgra32);
            // Pbgra32 needed otherwise the image output is black
            //canvas.Measure(new Size((int)canvas.Width, (int)canvas.Height));
            //canvas.Arrange(new Rect(new Size((int)canvas.Width, (int)canvas.Height)));

            renderBitmap.Render(canvas);

            //JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

            using (Stream file = File.Create(filename))
            {
                encoder.Save(file);
            }

        }
    }

}
