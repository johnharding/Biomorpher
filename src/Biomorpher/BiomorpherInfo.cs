using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Biomorpher.IGA;

namespace Biomorpher
{
    public class BiomorpherInfo : Grasshopper.Kernel.GH_AssemblyInfo
    {
        public override string Description
        {
            get { return "Interactive Evolutionary Algorithms for Grasshopper"; }
        }
        public override System.Drawing.Bitmap Icon
        {
            get { return Properties.Resources.BiomorpherIcon2_24; }
        }
        public override string Name
        {
            get { return "Biomorpher"; }
        }
        public override string Version
        {
            get { return Friends.VerionInfo(); }
        }
        public override Guid Id
        {
            get { return new Guid("{8E64BAEB-D698-4029-A543-76FC4086900A}"); }
        }

        public override string AuthorName
        {
            get { return "John Harding & Cecilie Brandt-Olsen"; }
        }
        public override string AuthorContact
        {
            get { return "johnharding@fastmail.fm"; }
        }
    }
}