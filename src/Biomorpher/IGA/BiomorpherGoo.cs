using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.Kernel.Special;

namespace Biomorpher.IGA
{
    /// <summary>
    /// Wraps up the biomorpher data for use in a GH parameter
    /// </summary>
    public class BiomorpherGoo : GH_Goo<BiomorpherData>
    {

        public BiomorpherGoo()
        {
            this.Value = new BiomorpherData();
        }

        // Constructor with initial value
        public BiomorpherGoo(BiomorpherData theValue)
        {
            this.Value = theValue;
        }

        // Copy Constructor
        public BiomorpherGoo(BiomorpherGoo localGoo)
        {
            this.Value = localGoo.Value;
        }

        public override IGH_Goo Duplicate()
        {
            return new BiomorpherGoo(this);
        }

        public override bool IsValid
        {
            get { return true; }
        }

        public override string TypeName
        {
            get { return "BiomorpherGoo"; }
        }

        public override string TypeDescription
        {
            get { return "BiomorpherData"; }
        }

        public override object ScriptVariable()
        {
            return this.Value;
        }

        /// <summary>
        /// Return a string representation of the state (value) of this instance.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "BiomorpherData";
        }

        /// <summary>
        /// Write to a file
        /// </summary>
        /// <param name="writer"></param>
        /// <returns></returns>
        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            
            if (Value != null)
            {
                Value.populationData.Write(writer.CreateChunk("populationData"));
                Value.historicData.Write(writer.CreateChunk("historicData"));
                Value.clusterData.Write(writer.CreateChunk("clusterData"));
                Value.genoGuids.Write(writer.CreateChunk("genoData"));

            }
            

            return true;
        }

        /// <summary>
        /// Read a file
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            var popChunk = reader.FindChunk("populationData");
            var hisChunk = reader.FindChunk("historicData");
            var cluChunk = reader.FindChunk("clusterData");
            var genChunk = reader.FindChunk("genoData");

            if (popChunk != null)
            {
                var data = new GH_Structure<GH_Number>();
                data.Read(popChunk);
                Value.populationData = new GH_Structure<GH_Number>(data, true);
            }

            if (hisChunk != null)
            {
                var data = new GH_Structure<GH_Number>();
                data.Read(hisChunk);
                Value.historicData = new GH_Structure<GH_Number>(data, true);
            }

            if (cluChunk != null)
            {
                var data = new GH_Structure<GH_Number>();
                data.Read(cluChunk);
                Value.clusterData = new GH_Structure<GH_Number>(data, true);
            }

            if (genChunk != null)
            {
                var data = new GH_Structure<GH_Guid>();
                data.Read(genChunk);
                Value.genoGuids = new GH_Structure<GH_Guid>(data, true);
            }

            return true;

        }


    }
}
