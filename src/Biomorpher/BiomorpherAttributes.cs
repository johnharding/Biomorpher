using System;
using System.Drawing;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Attributes;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Biomorpher
{
    public class BiomorpherAttributes : GH_ComponentAttributes
    {

        public BiomorpherComponent MyOwner
        {
            get;
            private set;
        }

        public BiomorpherAttributes(BiomorpherComponent owner)
            : base(owner)
        {
            this.MyOwner = owner;
        }

        protected override void Layout()
        {
            base.Layout();
        }


        public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if ((ContentBox.Contains(e.CanvasLocation)))
            {
                // Best to flip this to iron out any errors
                MyOwner.GO = !MyOwner.GO;

                MyOwner.ExpireSolution(true);

                return GH_ObjectResponse.Handled;
            }



            return GH_ObjectResponse.Ignore;
        }



        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            if (channel == GH_CanvasChannel.Wires)
            {
                base.Render(canvas, graphics, channel);
            }

            if (channel == GH_CanvasChannel.Objects)
            {
                // 1. Component Render
                base.Render(canvas, graphics, channel);

                // 2. New upper tab
                //Color myColor = Color.LightGray;
                //Rectangle myRect = new Rectangle((int)Bounds.Location.X, (int)Bounds.Location.Y - 20, (int)Bounds.Size.Width, 10);
                //Pen myPen = new Pen(Brushes.Black, 1);
                //graphics.DrawRectangle(myPen, myRect);

                // 3. Show combinations
                Font ubuntuFont = new Font("ubuntu", 8);
                StringFormat format = new StringFormat();
                format.Alignment = StringAlignment.Near;
                format.LineAlignment = StringAlignment.Center;
                format.Trimming = StringTrimming.EllipsisCharacter;

                graphics.DrawString(MyOwner.sliderValues.Count + " designs", ubuntuFont, Brushes.Black, (int)Bounds.Location.X, (int)Bounds.Location.Y - 8, format);

            }
        }

    }
}