using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biomorpher.IGA
{
    /// <summary>
    /// Historic record of populations
    /// </summary>
    class PopHistory
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
        public PopHistory()
        {
            historicpops = new List<Population>();
        }

        /// <summary>
        /// adds a population to the store
        /// </summary>
        /// <param name="pop"></param>
        public void AddPop(Population pop)
        {
            historicpops.Add(new Population(pop));
        }

    }
}
