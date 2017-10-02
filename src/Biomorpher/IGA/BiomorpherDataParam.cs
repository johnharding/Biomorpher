using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Biomorpher.IGA
{
    public class BiomorpherDataParam : GH_PersistentParam<BiomorpherGoo>
    {

        private BiomorpherData SoupData;

        public BiomorpherDataParam() :
            base(new GH_InstanceDescription("BiomorpherData", "BiomorpherData", "BiomorpherData", "Params", "Util"))
        {
            SoupData = new BiomorpherData();
        }

        public override System.Guid ComponentGuid
        {
            get { return new Guid("5922851E-AF03-48EC-AB83-370E319AAD85"); }
        }

        public override GH_Exposure Exposure
        {

            get
            {
                return GH_Exposure.hidden;
            }

        }

        /*
        protected override Bitmap Icon
        {
            get
            {
                return Properties.Resources.
            }
        }
        */

        protected override GH_GetterResult Prompt_Singular(ref BiomorpherGoo value)
        {
            return GH_GetterResult.success;
        }

        protected override GH_GetterResult Prompt_Plural(ref System.Collections.Generic.List<BiomorpherGoo> values)
        {
            return GH_GetterResult.success;
        }

        /*
        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            
            //writer.SetString("soupdragon", "fishstix");
            return base.Write(writer);
        }

        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            //string myText = null;
            //reader.TryGetString("soupdragon", ref myText);
            //this.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, myText);


            return base.Read(reader);
        }
        */

        
    }
}
