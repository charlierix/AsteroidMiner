using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Data;
using System.Text;
using System.Windows.Forms;

using Game.Orig.Math3D;

namespace Game.Orig.HelperClassesGDI.Controls
{
	/// <summary>
	/// This control is hardcoded to be the bottom part of a quarter circle for the bottom left corner of the screen.
	/// Width is the width of the whole pie panel
	/// Height is my custom height
	/// </summary>
	public partial class PiePanelBottom : UserControl
	{
		#region Declaration Section

		private Bitmap _bitmap = null;

		#endregion

		#region Constructor

		public PiePanelBottom()
		{
			InitializeComponent();
		}

		#endregion

		#region Overrides

		protected override void OnLoad(EventArgs e)
		{
			RebuildRegion();		//	BackColorChanged was too soon, and I wasn't being resized, so I need to rebuild here
			base.OnLoad(e);
		}

		protected override void OnBackColorChanged(EventArgs e)
		{
			RebuildRegion();
			base.OnBackColorChanged(e);
		}

		protected override void OnResize(EventArgs e)
		{
			RebuildRegion();
			base.OnResize(e);
		}

		protected override void OnPaintBackground(PaintEventArgs e)
		{
			if (_bitmap == null || _bitmap.Width != this.Width || _bitmap.Height != this.Height)
			{
				base.OnPaintBackground(e);
				RebuildRegion();
			}
			else
			{
				e.Graphics.DrawImageUnscaled(_bitmap, e.ClipRectangle);
			}
		}

		#endregion

		#region Private Methods

		private void RebuildRegion()
		{

			//	Wipe out the previous background image
			if (_bitmap != null)
			{
				_bitmap.Dispose();
			}
			_bitmap = null;

			#region Validate Width/Height

			//	See if the height and width are unstable
			if (this.Width <= 0 || this.Height <= 0)
			{
				return;
			}

			#endregion

			//	Calculate the pie wedge
			GraphicsPath outline = new GraphicsPath();
			//outline.AddPie(this.Width * -1, 0, this.Width * 2, this.Width * 2, 270, 90);



			//	Calculate the angle
			double angle = Math.Asin(Convert.ToDouble(Height) / Convert.ToDouble(Width));
			angle = Utility3D.GetRadiansToDegrees(angle);

			//	Calculate the base length
			double baseLength = Math.Sqrt((Width * Width) - (Height * Height));



			//outline.AddArc(this.Width * -1, 0, this.Width * 2, this.Width * 2, Convert.ToSingle(360f - angle), Convert.ToSingle(angle));
			outline.AddArc(this.Width * -1, this.Height - this.Width, this.Width * 2, this.Width * 2, Convert.ToSingle(360f - angle), Convert.ToSingle(angle));

			outline.AddLine(Width, Height, 0, Height);
			outline.AddLine(0, Height, 0, 0);
			outline.CloseFigure();		//outline.AddLine(0, 0, Convert.ToSingle(baseLength), 0);
			



			//	Draw my background image
			_bitmap = new Bitmap(this.Width, this.Height);

			//TODO:  Make a gradient forground
			using (Graphics graphics = Graphics.FromImage(_bitmap))
			{
				graphics.SmoothingMode = SmoothingMode.HighQuality;

				graphics.Clear(this.BackColor);

				using (Pen pen = new Pen(SystemColors.ControlDark, 1f))
				{
					graphics.DrawPath(pen, outline);
					graphics.DrawLine(pen, 0, this.Height - 1, this.Width, this.Height - 1);		//	the bottom line isn't drawing, so I'll back up a pixel
				}
			}

			//	Make me the shape of the pie wedge
			this.Region = new Region(outline);
			this.Refresh();

		}

		#endregion
	}
}
