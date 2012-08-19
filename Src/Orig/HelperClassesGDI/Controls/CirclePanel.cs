using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace Game.Orig.HelperClassesGDI.Controls
{
	/// <summary>
	/// This control is hardcoded to be a full circle.
	/// </summary>
	public partial class CirclePanel : UserControl
	{
		#region Declaration Section

		private int _diameter = 150;

		private Bitmap _bitmap = null;

		#endregion

		#region Constructor

		public CirclePanel()
		{
			InitializeComponent();
		}

		#endregion

		#region Public Properties

		/// <summary>
		/// This replaces Width and Height
		/// TODO:  Make this diameter
		/// </summary>
		[Description("This replaces Width and Height"),
		Category("Layout"),
		DefaultValue(50)]
		public virtual int Diameter
		{
			get
			{
				return _diameter;
			}
			set
			{
				_diameter = value;
				this.Width = _diameter;
				this.Height = _diameter;
			}
		}

		#endregion

		#region Overrides

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
			if (_bitmap == null)
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

			if (this.Width != this.Height)
			{
				//	Set my diameter to the one that changed
				if (this.Width != _diameter)
				{
					this.Diameter = this.Width;
				}
				else
				{
					this.Diameter = this.Height;
				}

				return;		//	the diameter property is in the middle of making these the same.  No need to waste my time building a bitmap when the other property is about to be set
			}

			if (_diameter != this.Width)
			{
				this.Diameter = this.Width;
				return;
			}

			#endregion

			//	Calculate the pie wedge
			GraphicsPath outline = new GraphicsPath();
			outline.AddEllipse(0, 0, this.Width, this.Height);

			//	Draw my background image
			_bitmap = new Bitmap(this.Width, this.Height);

			//TODO:  Make a gradient forground
			using (Graphics graphics = Graphics.FromImage(_bitmap))
			{
				graphics.SmoothingMode = SmoothingMode.HighQuality;

				graphics.Clear(this.BackColor);

				using (Pen pen = new Pen(Color.Black, 1.5f))
				{
					graphics.DrawPath(pen, outline);
				}
			}

			//	Make me the shape of the circle
			this.Region = new Region(outline);
			this.Refresh();

		}

		#endregion
	}
}
