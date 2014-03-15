using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;

namespace Game.Orig.HelperClassesGDI.Controls
{
    /// <summary>
    /// This control is hardcoded to be a quarter circle for the bottom left corner of the screen.  It wouldn't
    /// be too tough to make an enum telling which quadrant should be rounded.
    /// </summary>
    public partial class PiePanel : UserControl
    {
        #region Declaration Section

        private int _radius = 10;

        private Bitmap _bitmap = null;

        #endregion

        #region Constructor

        public PiePanel()
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

        /// <summary>
        /// This sets the visibility of the expand/collapse button
        /// </summary>
        [Description("This sets the visibility of the expand/collapse button"),
        Category("Appearance"),
        DefaultValue(true)]
        public bool ExpandCollapseVisible
        {
            get
            {
                return expandCollapsePieButton1.Visible;
            }
            set
            {
                expandCollapsePieButton1.Visible = value;
            }
        }

        #endregion

        #region Public Methods

        public void SetDefaultBackColor()
        {
            // I tried to match the backcolor of the tab control
            //this.BackColor = UtilityHelper.AlphaBlend(UtilityHelper.AlphaBlend(SystemColors.Control, SystemColors.Window, .4d), Color.GhostWhite, .85d);

            this.BackColor = UtilityGDI.AlphaBlend(SystemColors.Window, SystemColors.Control, .65d);		// that gray was UGLY

            // Repaint the background
            RebuildRegion();
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

            expandCollapsePieButton1.Top = 0;
            expandCollapsePieButton1.Left = 0;
            expandCollapsePieButton1.Width = _radius;
            expandCollapsePieButton1.Height = _radius;
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

            // Calculate the pie wedge
            GraphicsPath outline = new GraphicsPath();
            outline.AddPie(this.Width * -1, 0, this.Width * 2, this.Height * 2, 270, 90);

            // Draw my background image
            _bitmap = new Bitmap(this.Width, this.Height);

            //TODO:  Make a gradient forground
            using (Graphics graphics = Graphics.FromImage(_bitmap))
            {
                graphics.SmoothingMode = SmoothingMode.HighQuality;

                graphics.Clear(this.BackColor);

                using (Pen pen = new Pen(SystemColors.ControlDark, 1f))
                {
                    graphics.DrawPath(pen, outline);
                    graphics.DrawLine(pen, 0, this.Height - 1, this.Width, this.Height - 1);		// the bottom line isn't drawing, so I'll back up a pixel
                }
            }

            // Make me the shape of the pie wedge
            this.Region = new Region(outline);
            this.Refresh();

        }

        #endregion
    }
}
