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
    /// This control is hardcoded to be a button that sits on the PiePanel.  This button will act as an expand/collapse toggle
    /// </summary>
    public partial class ExpandCollapsePieButton : UserControl
    {
        #region Declaration Section

        private int _buttonRadius = 18;

        private int _radius = 10;		// this acts as the outer radius.  The inner radius is _radius - _buttonRadius

        private Bitmap _bitmap = null;

        #endregion

        #region Constructor

        public ExpandCollapsePieButton()
        {
            InitializeComponent();
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// This replaces Width and Height
        /// </summary>
        [Description("This replaces Width and Height"),
        Category("Layout"),
        DefaultValue(50)]
        public virtual int Radius
        {
            get
            {
                return _radius;
            }
            set
            {
                _radius = value;
                this.Width = _radius;
                this.Height = _radius;
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

            // Wipe out the previous background image
            if (_bitmap != null)
            {
                _bitmap.Dispose();
            }
            _bitmap = null;

            #region Validate Width/Height

            // See if the height and width are unstable
            if (this.Width <= 0 || this.Height <= 0)
            {
                return;
            }

            if (this.Width != this.Height)
            {
                // Set my radius to the one that changed
                if (this.Width != _radius)
                {
                    this.Radius = this.Width;
                }
                else
                {
                    this.Radius = this.Height;
                }

                return;		// the radius property is in the middle of making these the same.  No need to waste my time building a bitmap when the other property is about to be set
            }

            if (_radius != this.Width)
            {
                this.Radius = this.Width;
                return;
            }

            #endregion

            // ArcLength=(angle*pi*r)/180
            float sweepAngle = Convert.ToSingle((180 * (_buttonRadius * 1.618d)) / (Math.PI * _radius));		// make the button wider than it is tall
            float startAngle = (90f - sweepAngle) / 2f;

            // Calculate the pie wedge
            GraphicsPath outline = new GraphicsPath();
            outline.AddArc(this.Width * -1, 0, this.Width * 2, this.Height * 2, 270 + startAngle, sweepAngle);

            int newWidth = this.Width - _buttonRadius;
            int newHeight = this.Height - _buttonRadius;

            if (newWidth < 0 || newHeight < 0)
            {
                return;
            }

            outline.AddArc(newWidth * -1, this.Height - newHeight, newWidth * 2, newHeight * 2, -startAngle, -sweepAngle);

            outline.CloseFigure();

            // Draw my background image
            _bitmap = new Bitmap(this.Width, this.Height);

            //TODO:  Make a gradient forground
            using (Graphics graphics = Graphics.FromImage(_bitmap))
            {
                graphics.SmoothingMode = SmoothingMode.HighQuality;

                graphics.Clear(this.BackColor);

                using (Pen pen = new Pen(Color.Black, 1f))
                {
                    graphics.DrawPath(pen, outline);
                }
            }

            // Make me the shape of the pie wedge
            this.Region = new Region(outline);
            this.Refresh();

        }

        #endregion
    }
}
