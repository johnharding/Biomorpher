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
using Biomorpher.IGA;

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
        public BiomorpherAttributes(BiomorpherComponent owner) : base(owner)
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
                if(Owner.Params.Input[0].SourceCount != 0 && Owner.Params.Input[1].SourceCount !=0)
                {
                    myMainWindow = new BiomorpherWindow(MyOwner);
                    myMainWindow.Show();

                    return GH_ObjectResponse.Handled;
                }
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

            Grasshopper.GUI.Canvas.GH_PaletteStyle styleStandard = null;

            if (channel == GH_CanvasChannel.Objects)
            {

                // Cache the current styles.
                styleStandard = GH_Skin.palette_normal_standard;
                GH_Skin.palette_normal_standard = new GH_PaletteStyle(Color.FromArgb(255,13,138), Color.Black, Color.Black);

                Pen myPen = new Pen(Brushes.Black, 1);

                GraphicsPath path = RoundedRectangle.Create((int)(Bounds.Location.X), (int)Bounds.Y - 13, (int)Bounds.Width, 24, 3);
                graphics.DrawPath(myPen, path);

                Font myFont = new Font(Grasshopper.Kernel.GH_FontServer.Standard.FontFamily, 5, FontStyle.Italic);
                StringFormat format = new StringFormat();

                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                format.Trimming = StringTrimming.EllipsisCharacter;
                
                graphics.DrawString("doubleclick icon (v"+ Friends.VerionInfo() +")", myFont, Brushes.Black, (int)(Bounds.Location.X + (Bounds.Width / 2)), (int)Bounds.Location.Y - 6, format);

                format.Dispose();

            }

            base.Render(canvas, graphics, channel);

            if (channel == GH_CanvasChannel.Objects)
            {

                // Restore the cached styles.
                GH_Skin.palette_normal_standard = styleStandard;


            }
        }

    }
}

