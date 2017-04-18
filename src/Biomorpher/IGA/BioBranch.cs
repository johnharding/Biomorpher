using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biomorpher.IGA
{
    class BioBranch
    {
        public int ParentBranchIndex { get; set; }
        public int ParentTwigIndex { get; set; }
        public List<Population> Twigs { get; set; }

        /// <summary>
        /// Contructor for a BioBranch
        /// </summary>
        /// <param name="parentBranchIndex">Every BioBranch has a parent branch. Use -1 for initial branch</param>
        /// <param name="parentTwigIndex">Every new branch can grow from a twig. Store index this twig here</param>
        public BioBranch(int parentBranchIndex, int parentTwigIndex)
        {
            Twigs = new List<Population>();
            this.ParentBranchIndex = parentBranchIndex;
            this.ParentTwigIndex = parentTwigIndex;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pop">Population that will be copied and stored</param>
        public void AddTwig(Population pop)
        {
            Twigs.Add(new Population(pop)); // copy of the current pop
        }

    }
}
