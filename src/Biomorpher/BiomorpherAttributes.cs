using System;
using System.Drawing;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Attributes;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace Biomorpher
{
    /// <summary>
    /// Component attributes
    /// </summary>
    public class BiomorpherAttributes : GH_ComponentAttributes
    {
        /// <summary>
        /// Declare main window as part of component attributes
        /// </summary>
        private BiomorpherWindow myMainWindow;

        /// <summary>
        /// Component attributes constructor
        /// </summary>
        public BiomorpherComponent MyOwner
        {
            get;
            private set;
        }

        /// <summary>
        /// Component attributes
        /// </summary>
        /// <param name="owner"></param>
        public BiomorpherAttributes(BiomorpherComponent owner)
            : base(owner)
        {
            this.MyOwner = owner;
        }

        /// <summary>
        /// Layout component
        /// </summary>
        protected override void Layout()
        {
            base.Layout();
        }


        /// <summary>
        /// Open the biomorpher window upon doubleclick
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if ((ContentBox.Contains(e.CanvasLocation)))
            {
                myMainWindow = new BiomorpherWindow(MyOwner);
                myMainWindow.Show();
                
                return GH_ObjectResponse.Handled;
            }

            return GH_ObjectResponse.Ignore;
        }


        /// <summary>
        /// Render the component
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="graphics"></param>
        /// <param name="channel"></param>
        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            if (channel == GH_CanvasChannel.Wires)
            {
                base.Render(canvas, graphics, channel);
            }

            if (channel == GH_CanvasChannel.Objects)
            {

                SolidBrush johnBrush = new SolidBrush(Color.FromArgb(255, 50, 50, 50));
              
                Pen myPen = new Pen(johnBrush, 1);
                
                GraphicsPath path = RoundedRectangle.Create((int)(Bounds.Location.X), (int)Bounds.Y - 13, (int)Bounds.Width, 24, 3);
                graphics.DrawPath(myPen, path);

                Font myFont = new Font(Grasshopper.Kernel.GH_FontServer.Standard.FontFamily, 5, FontStyle.Italic);
                StringFormat format = new StringFormat();

                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                format.Trimming = StringTrimming.EllipsisCharacter;
                ;
                graphics.DrawString("doubleclick icon to launch window", myFont, johnBrush, (int)(Bounds.Location.X+(Bounds.Width/2)), (int)Bounds.Location.Y-6, format);

                format.Dispose();

                base.Render(canvas, graphics, channel);

            }
        }

    }
}

