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
    public class BiomorpherData
    {
        public GH_Structure<GH_Number> populationData;
        public GH_Structure<GH_Number> historicData;
        public GH_Structure<GH_Number> clusterData;
        public List<GH_NumberSlider> sliderData;
        public List<GalapagosGeneListObject> genepoolData;
        public int fish;
        
        public BiomorpherData(){}

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

        public List<Guid> GetGenoGUIDs()
        {
            List<Guid> myList = new List<Guid>();
            for (int i = 0; i < sliderData.Count; i++)
            {
                myList.Add(sliderData[i].InstanceGuid);
            }

            for (int i = 0; i < genepoolData.Count; i++)
            {
                myList.Add(genepoolData[i].InstanceGuid);
            }

            return myList;
        }
    }
}
