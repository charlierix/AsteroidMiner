using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Game.Orig.HelperClassesGDI
{
    public static class UtilityGDI
    {
        /// <summary>
        /// WPF doesn't have a way to enable visual styles of winform apps, so it can call this method, and all
        /// winform apps will look right.
        /// </summary>
        public static void EnableVisualStyles()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
        }

        /// <summary>
        /// This returns a color that is the result of the two colors blended
        /// </summary>
        /// <param name="alpha">0 is all back color, 1 is all fore color, .5 is half way between</param>
        public static Color AlphaBlend(Color foreColor, Color backColor, double alpha)
        {
            // Figure out the new color
            int intANew = Convert.ToInt32(Convert.ToDouble(backColor.A) + (((Convert.ToDouble(foreColor.A) - Convert.ToDouble(backColor.A)) / 255) * alpha * 255));
            int intRNew = Convert.ToInt32(Convert.ToDouble(backColor.R) + (((Convert.ToDouble(foreColor.R) - Convert.ToDouble(backColor.R)) / 255) * alpha * 255));
            int intGNew = Convert.ToInt32(Convert.ToDouble(backColor.G) + (((Convert.ToDouble(foreColor.G) - Convert.ToDouble(backColor.G)) / 255) * alpha * 255));
            int intBNew = Convert.ToInt32(Convert.ToDouble(backColor.B) + (((Convert.ToDouble(foreColor.B) - Convert.ToDouble(backColor.B)) / 255) * alpha * 255));

            // Make sure the values are in range
            if (intANew < 0)
            {
                intANew = 0;
            }
            else if (intANew > 255)
            {
                intANew = 255;
            }

            if (intRNew < 0)
            {
                intRNew = 0;
            }
            else if (intRNew > 255)
            {
                intRNew = 255;
            }

            if (intGNew < 0)
            {
                intGNew = 0;
            }
            else if (intGNew > 255)
            {
                intGNew = 255;
            }

            if (intBNew < 0)
            {
                intBNew = 0;
            }
            else if (intBNew > 255)
            {
                intBNew = 255;
            }

            // Exit Function
            return Color.FromArgb(intANew, intRNew, intGNew, intBNew);
        }

        public static GraphicsPath GetRoundedRectangle(Rectangle rectangle, int cornerRadius)
        {
            return GetRoundedRectangle(rectangle.Left, rectangle.Top, rectangle.Width, rectangle.Height, cornerRadius);
        }
        public static GraphicsPath GetRoundedRectangle(int width, int height, int cornerRadius)
        {
            return GetRoundedRectangle(0, 0, width, height, cornerRadius);
        }
        public static GraphicsPath GetRoundedRectangle(int left, int top, int width, int height, int cornerRadius)
        {
            GraphicsPath retVal = new GraphicsPath();

            if (cornerRadius > 0)
            {
                Rectangle topLeft = new Rectangle(left, top, cornerRadius, cornerRadius);
                Rectangle topRight = new Rectangle(left + width - cornerRadius, top, cornerRadius, cornerRadius);
                Rectangle bottomLeft = new Rectangle(left, top + height - cornerRadius, cornerRadius, cornerRadius);
                Rectangle bottomRight = new Rectangle(left + width - cornerRadius, top + height - cornerRadius, cornerRadius, cornerRadius);

                retVal.AddArc(topLeft, 180f, 90f);
                retVal.AddLine(topLeft.Left + topLeft.Width, topLeft.Top, topRight.Left, topRight.Top);
                retVal.AddArc(topRight, 270f, 90f);
                retVal.AddLine(topRight.Left + topRight.Width, topRight.Top + topRight.Height, bottomRight.Left + bottomRight.Width, bottomRight.Top);
                retVal.AddArc(bottomRight, 360f, 90f);
                retVal.AddLine(bottomRight.Left, bottomRight.Top + bottomRight.Height, bottomLeft.Left + bottomLeft.Width, bottomLeft.Top + bottomLeft.Height);
                retVal.AddArc(bottomLeft, 90f, 90f);
                retVal.AddLine(bottomLeft.Left, bottomLeft.Top, topLeft.Left, topLeft.Top + topLeft.Height);
            }
            else
            {
                retVal.AddRectangle(new Rectangle(left, top, width, height));
            }

            retVal.CloseAllFigures();

            // Exit Function
            return retVal;
        }
    }
}
