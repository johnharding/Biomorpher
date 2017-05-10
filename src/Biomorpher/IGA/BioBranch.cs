using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biomorpher.IGA
{
    /// <summary>
    /// Branch containing a list of biomorpher generations
    /// </summary>
    class BioBranch
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
        public List<Population> Twigs { get; set; }

        /// <summary>
        /// Contructor for a BioBranch
        /// </summary>
        /// <param name="parentBranchIndex">Every BioBranch has a parent branch. Use -1 for initial branch</param>
        /// <param name="parentTwigIndex">Every new branch can grow from a twig. Store index this twig here</param>
        /// <param name="startY">Location of starting y coordinate on the history canvas</param>
        public BioBranch(int parentBranchIndex, int parentTwigIndex, int startY)
        {
            Twigs = new List<Population>();
            this.ParentBranchIndex = parentBranchIndex;
            this.ParentTwigIndex = parentTwigIndex;
            StartY = startY; // used for the history bit
        }

        /// <summary>
        /// Adds a new population 'twig'
        /// </summary>
        /// <param name="pop">Population that will be copied and stored</param>
        public void AddTwig(Population pop)
        {
            Twigs.Add(new Population(pop)); // copy of the current pop
        }

    }
}
