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
        private BiomorpherWindow myMainWindow;

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
                // Best to flip this boolean to iron out any errors
                //MyOwner.GO = true;
                //MyOwner.ExpireSolution(true);

                myMainWindow = new BiomorpherWindow(MyOwner);
                myMainWindow.Show();
                

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


                SolidBrush johnBrush = new SolidBrush(Color.FromArgb(255, 50, 50, 50));
                
                RectangleF myRect = new RectangleF(Bounds.X+16, Bounds.Y, Bounds.Width, Bounds.Height);
                //graphics.FillRectangle(johnBrush, Rectangle.Ceiling(myRect));

                Pen myPen = new Pen(johnBrush, 1);
                graphics.DrawRectangle(myPen, Rectangle.Ceiling(myRect));

                base.Render(canvas, graphics, channel);

                Font myFont = new Font("Tahoma", 5);
                StringFormat format = new StringFormat();
                format.FormatFlags = StringFormatFlags.DirectionVertical;
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                format.Trimming = StringTrimming.EllipsisCharacter;
                //graphics.RotateTransform(90);
                graphics.DrawString("(doubleclick icon)", myFont, johnBrush, (int)Bounds.Location.X + Bounds.Width+10, (int)Bounds.Location.Y+32, format);
                
            }
        }

    }
}

