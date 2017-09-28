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
    class OutputData
    {
        private GH_Structure<GH_Number> populationData;
        private GH_Structure<GH_Number> historicData;
        private GH_Structure<GH_Number> clusterData;
        private List<GH_NumberSlider> sliderData;
        private List<GalapagosGeneListObject>genepoolData;
        private List<object> slidergenepoolData;
        
        public OutputData(){}

        public void SetPopulationData(GH_Structure<GH_Number> incoming){ populationData = new GH_Structure<GH_Number>(incoming, false);}
        public void SetHistoricData(GH_Structure<GH_Number> incoming){ historicData = new GH_Structure<GH_Number>(incoming, false);}
        public void SetClusterData(GH_Structure<GH_Number> incoming){ clusterData = new GH_Structure<GH_Number>(incoming, false);}
        public void SetSliderData(List<GH_NumberSlider> incoming) { sliderData = new List<GH_NumberSlider>(incoming); }
        public void SetGenePoolData(List<GalapagosGeneListObject> incoming){ genepoolData = new List<GalapagosGeneListObject>(incoming);}

        public GH_Structure<GH_Number> GetPopulationData() { return populationData; }
        public GH_Structure<GH_Number> GetHistoricData() { return historicData; }
        public GH_Structure<GH_Number> GetClusterData() { return clusterData; }
        public List<GH_NumberSlider> GetSliders() { return sliderData; }
        public List<GalapagosGeneListObject> GetGenePools() { return genepoolData; }
    }
}
