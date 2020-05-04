using GalapagosComponents;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biomorpher.IGA
{
    /// <summary>
    /// A datatype used to store information about a Biomorpher solution
    /// </summary>
    public class BiomorpherData
    {

        /// <summary>
        /// Historic population genes
        /// </summary>
        public GH_Structure<GH_Number> historicData;

        /// <summary>
        /// Population number
        /// </summary>
        public int PopCount { get; set; }

        /// <summary>
        /// Slider and genepool guids
        /// </summary>
        public GH_Structure<GH_Guid> genoGuids;
      
        /// <summary>
        /// Standard constructor
        /// </summary>
        public BiomorpherData()
        {
        }

        /// <summary>
        /// Sets the historic popopulation data
        /// </summary>
        /// <param name="incoming"></param>
        public void SetHistoricData(GH_Structure<GH_Number> incoming)
        {
            historicData = new GH_Structure<GH_Number>(incoming, false);
        }

        /// <summary>
        /// Sets the guids for the sliders and genepools used
        /// </summary>
        /// <param name="incoming"></param>
        public void SetGenoGuids(GH_Structure<GH_Guid> incoming)
        {
            genoGuids = new GH_Structure<GH_Guid>(incoming, false);
        }

    }
}
