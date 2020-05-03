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
    public class BiomorpherReaderAttributes : GH_ComponentAttributes
    {

        /// <summary>
        /// Component attributes
        /// </summary>
        /// <param name="owner"></param>
        public BiomorpherReaderAttributes(BiomorpherReader owner) : base(owner)
        {
            
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
                GH_Skin.palette_normal_standard = new GH_PaletteStyle(Color.FromArgb(255, 13, 138), Color.Black, Color.Black);
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

