using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Biomorpher.IGA
{
    /// <summary>
    /// Branch containing a list of biomorpher generations
    /// </summary>
    public class BioBranch
    {
        /// <summary>
        /// Source branchID where growth began
        /// </summary>
        public int ParentBranchIndex { get; set; }
        
        /// <summary>
        /// Source twig index where growth began
        /// </summary>
        public int ParentTwigIndex { get; set; }

        /// <summary>
        /// Location of starting y coordinate on the history canvas
        /// </summary>
        public int StartY { get; set; }

        /// <summary>
        /// List of populations in this branch
        /// </summary>
        public List<Population> PopTwigs { get; set; }

        /// <summary>
        /// The BSpline curve drawn back to the parent
        /// </summary>
        public Path OriginCurve { get; set; }

        /// <summary>
        /// Mins out of the entire run
        /// </summary>
        public double[] minPerformanceValues { get; set; }

        /// <summary>
        /// Maxes out of the entire run
        /// </summary>
        public double[] maxPerformanceValues { get; set; }

        /// <summary>
        /// Performance count
        /// </summary>
        public int performanceCount { get; set; }


        /// <summary>
        /// Contructor for a BioBranch
        /// </summary>
        /// <param name="parentBranchIndex">Every BioBranch has a parent branch. Use -1 for initial branch</param>
        /// <param name="parentTwigIndex">Every new branch can grow from a twig. Store index this twig here</param>
        /// <param name="startY">Location of starting y coordinate on the history canvas</param>
        public BioBranch(int parentBranchIndex, int parentTwigIndex, int startY)
        {
            PopTwigs = new List<Population>();
            this.ParentBranchIndex = parentBranchIndex;
            this.ParentTwigIndex = parentTwigIndex;
            StartY = startY; // used for the history bit
            performanceCount = 0;
        }

        /// <summary>
        /// Adds a new population 'twig'
        /// </summary>
        /// <param name="pop">Population that will be copied and stored</param>
        public void AddTwig(Population pop, int pCount)
        {
            PopTwigs.Add(new Population(pop)); // copy of the current pop
            performanceCount = pCount;
            PerformAnalytics();
        }


        /// <summary>
        /// Draws a BSpline curve from parent branch to new branch
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="allBranches"></param>
        public void DrawOriginCurve(Canvas canvas, List<BioBranch> allBranches)
        {
            OriginCurve = new Path();

            System.Windows.Point outNode = allBranches[ParentBranchIndex].PopTwigs[ParentTwigIndex].HistoryNodeOUT;
            System.Windows.Point inNode = this.PopTwigs[0].HistoryNodeIN;

            int offset = (int)((inNode.X - outNode.X)/2);

            // Add a random factor to separate lines.
            int randomY = Friends.GetRandomInt(0, 32);

            System.Windows.Point P1 = new System.Windows.Point(outNode.X, outNode.Y + randomY);
            System.Windows.Point P2 = new System.Windows.Point(outNode.X + offset, outNode.Y + randomY);
            System.Windows.Point P3 = new System.Windows.Point(inNode.X - offset, inNode.Y + randomY);
            System.Windows.Point P4 = new System.Windows.Point(inNode.X, inNode.Y + randomY);

            OriginCurve.Data = Friends.MakeBezierGeometry(P1, P2, P3, P4);
            OriginCurve.Stroke = Brushes.DarkSlateGray;
            OriginCurve.StrokeThickness = 0.6;
            Canvas.SetZIndex(OriginCurve, -1);
            canvas.Children.Add(OriginCurve);

            //Add outline circle
            double s = 6;
            System.Windows.Shapes.Ellipse nodule = new System.Windows.Shapes.Ellipse();
            nodule.Height = s;
            nodule.Width = s;
            nodule.Fill = Brushes.DarkSlateGray;
            System.Windows.Shapes.Ellipse nodule2 = new System.Windows.Shapes.Ellipse();
            nodule2.Height = s;
            nodule2.Width = s;
            nodule2.Fill = Brushes.DarkSlateGray;

            Canvas.SetLeft(nodule, P1.X-s/2);
            Canvas.SetTop(nodule, P1.Y-s/2);
            canvas.Children.Add(nodule);

            Canvas.SetLeft(nodule2, P4.X-s/2);
            Canvas.SetTop(nodule2, P4.Y-s/2);
            canvas.Children.Add(nodule2);
        }

        /// <summary>
        /// Crunch some data for this branch.
        /// </summary>
        public void PerformAnalytics()
        {
            // min max for this entire branch
            // Only looks at representatives, as data might be null for the others
            minPerformanceValues = new double[performanceCount];
            maxPerformanceValues = new double[performanceCount];

            for(int p=0; p<minPerformanceValues.Length; p++)
            {
                minPerformanceValues[p] = 9999999999d;
                maxPerformanceValues[p] = -9999999999d;
            }

            for(int j=0; j<PopTwigs.Count; j++)
            {
                for(int k=0; k<PopTwigs[j].chromosomes.Length; k++)
                {
                   Chromosome thisDesign = PopTwigs[j].chromosomes[k];
                   if(thisDesign.isRepresentative)
                   {
                       for(int p=0; p<thisDesign.GetPerformas().Count; p++)
                       {
                           if(thisDesign.GetPerformas()[p] < minPerformanceValues[p])
                           {
                               minPerformanceValues[p] = thisDesign.GetPerformas()[p];
                           }

                           if (thisDesign.GetPerformas()[p] > maxPerformanceValues[p])
                           {
                               maxPerformanceValues[p] = thisDesign.GetPerformas()[p];
                           }
                       }
                   }
                }
            }
        }
    }
}
