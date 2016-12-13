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
    public class DesignSpaceAttributes : GH_ComponentAttributes
    {

        public DesignSpaceComponent MyOwner
        {
            get;
            private set;
        }

        public DesignSpaceAttributes(DesignSpaceComponent owner)
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

                // 2. Canvas Segmentation
                //graphics.DrawLine(Pens.Black, 0, 0, 0, -100000);
                //graphics.DrawLine(Pens.Black, 0, 0, -100000, 0);
                //graphics.FillEllipse(Brushes.Black, -2, -2, 4, 4);

                Font ubuntuFont = new Font("ubuntu", 8);
                StringFormat format = new StringFormat();
                format.Alignment = StringAlignment.Near;
                format.LineAlignment = StringAlignment.Center;
                format.Trimming = StringTrimming.EllipsisCharacter;

                graphics.DrawString(MyOwner.sliderValues.Count + " designs", ubuntuFont, Brushes.Black, (int)Bounds.Location.X, (int)Bounds.Location.Y - 8, format);

                //GH_Palette palette = GH_Palette.Pink;

                //Color myColor = Color.LightGray;

                //switch (Owner.RuntimeMessageLevel)
                //{
                //    case GH_RuntimeMessageLevel.Warning:
                //        myColor = Color.Orange;
                //        break;

                //    case GH_RuntimeMessageLevel.Error:
                //        myColor = Color.Red;
                //        break;
                //}

                //if (Owner.Hidden) myColor = Color.Gray;
                //if (Owner.Locked) myColor = Color.DarkGray;

                //Rectangle myRect = new Rectangle((int)Bounds.Location.X, (int)Bounds.Location.Y-20, (int)Bounds.Size.Width, 10);
                //Pen myPen = new Pen(Brushes.Black, 1);
                //graphics.DrawRectangle(myPen, myRect);
                //GH_Capsule capsule = GH_Capsule.CreateCapsule(myRect, palette, 10, 0);

                //capsule.Render(graphics, myColor);
                //capsule.Dispose();
                //capsule = null;
                //base.RenderComponentCapsule(canvas, graphics, false, false, false, true, true, false);
                //PointF iconLocation = new PointF(ContentBox.X-4, ContentBox.Y+70);
                //graphics.DrawImage(Owner.Icon_24x24, iconLocation);
                //
                //format.Dispose();

            }
        }

    }
}