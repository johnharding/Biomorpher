using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biomorpher.IGA
{
    public static class TheSliders
    {
        public static List<Grasshopper.Kernel.Special.GH_NumberSlider> sliders = new List<Grasshopper.Kernel.Special.GH_NumberSlider>();

        public static void setSliders(List<Grasshopper.Kernel.Special.GH_NumberSlider> s)
        {
           for(int i=0; i<s.Count; i++)
           {
               sliders[i] = s[i];
           }
        }
    }
}
