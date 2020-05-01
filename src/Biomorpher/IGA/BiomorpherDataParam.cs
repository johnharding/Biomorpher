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
        
        public BiomorpherDataParam() :
            base(new GH_InstanceDescription("BiomorpherSolution", "BiomorpherSolution", "Biomorpher solution to use in Reader component", "Params", "Util"))
        {
            this.IconDisplayMode = GH_IconDisplayMode.icon;
        }

        public override System.Guid ComponentGuid
        {
            get { return new Guid("5922851E-AF03-48EC-AB83-370E319AAD85"); }
        }

        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.senary;
            }
        }

        protected override Bitmap Icon
        {
            get
            {
                return Properties.Resources.ParamIcon2;
            }
        }

        protected override GH_GetterResult Prompt_Singular(ref BiomorpherGoo value)
        {
            return GH_GetterResult.success;
        }

        protected override GH_GetterResult Prompt_Plural(ref System.Collections.Generic.List<BiomorpherGoo> values)
        {
            return GH_GetterResult.success;
        }
        
    }
}
