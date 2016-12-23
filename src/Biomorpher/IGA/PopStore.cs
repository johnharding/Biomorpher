using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biomorpher.GA
{
    /// <summary>
    /// Historic record of populations
    /// </summary>
    class PopStore
    {
        /// <summary>
        /// number of historic populations
        /// </summary>
        private int generations;

        /// <summary>
        /// Stores population at each generation
        /// </summary>
        private List<Population> historicpops;

        /// <summary>
        /// constructor
        /// </summary>
        public PopStore()
        {
            historicpops = new List<Population>();
        }

        /// <summary>
        /// adds a population to the store
        /// </summary>
        /// <param name="pop"></param>
        public void AddPop(Population pop)
        {
            historicpops.Add(pop);
        }

    }
}
