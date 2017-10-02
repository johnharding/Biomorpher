using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biomorpher.IGA
{
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

            // Return a string representation of the state (value) of this instance.
            public override string ToString()
            {
                return "OutputDataSoup";
            }


            public override bool Write(GH_IO.Serialization.GH_IWriter writer)
            {
                return true;
            }

            // Deserialize this instance from a Grasshopper reader object.
            public override bool Read(GH_IO.Serialization.GH_IReader reader)
            {

                this.Value = new BiomorpherData();
                return true;
            }

            

            


    }
}
