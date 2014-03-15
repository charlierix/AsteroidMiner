using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

using Game.HelperClasses;
using Game.Orig.Math3D;
using Game.Orig.HelperClassesOrig;

namespace Game.Orig.TestersGDI.PhysicsPainter
{
    public partial class VectorPanel : Game.Orig.HelperClassesGDI.Controls.CirclePanel
    {
        #region Declaration Section

        private const double SAFEPERCENT = .99999d;

        // Change Events
        public event EventHandler ValueChanged = null;
        public event EventHandler MultiplierChanged = null;

        // The vector that this control represents
        private MyVector _vector = new MyVector();

        // The magnitude of the vector at the edge of the control
        private double _multiplier = 1;

        // The background image
        private bool _imageDirty = true;		// this only has meaning when _backImage is not null, and is the same size as this control (otherwise, the image is obviously dirty)
        private Bitmap _backImage = null;

        // Grid
        private bool _showGrid = true;
        private Color _gridColor = SystemColors.Control;

        // This is used so I don't get the message bouncing back to the property
        private bool _settingMagnitudeText = false;

        // Mousing
        private bool _isMouseDown = false;		// this is the left mouse button

        #endregion

        #region Constructor

        public VectorPanel()
        {
            InitializeComponent();

            displayItem1.Text = _vector.ToString(0);
            displayItem2.Text = "Length = 0";
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// This is the vector that this control represents
        /// WARNING:  Do not manipulate this vector directly, the change event won't fire
        /// </summary>
        [Description("This is the vector that this control represents"),
        Category("Behavior"),
        Browsable(false)]
        public MyVector Value
        {
            get
            {
                return _vector;
            }
        }

        /// <summary>
        /// This is the magnitude of the vector when it's length matches the control's radius
        /// </summary>
        [Description("This is the magnitude of the vector when it's length matches the control's radius"),
        Category("Behavior"),
        DefaultValue(1)]
        public double Multiplier
        {
            get
            {
                return _multiplier;
            }
            set
            {
                if (_multiplier <= 0d)
                {
                    throw new ArgumentOutOfRangeException("Must be a positive multiplier");
                }

                // Cap the vector if it's longer than the new value
                if (!_vector.IsZero)
                {
                    if (_vector.GetMagnitude() > value)
                    {
                        MyVector newValue = MyVector.BecomeUnitVector(_vector);
                        newValue.Multiply(value * SAFEPERCENT);
                        StoreNewValue(newValue);
                    }
                }

                // Store the value passed in
                _multiplier = value;

                _settingMagnitudeText = true;
                txtMultiplier.Text = _multiplier.ToString();
                _settingMagnitudeText = false;

                // Redraw
                _imageDirty = true;
                this.Invalidate();

                // Inform the world
                OnMultiplierChanged();
            }
        }

        /// <summary>
        /// This is whether to show the grid or not
        /// </summary>
        [Description("This is whether to show the grid or not"),
        Category("Appearance"),
        DefaultValue(true)]
        public bool ShowGrid
        {
            get
            {
                return _showGrid;
            }
            set
            {
                _showGrid = value;

                _imageDirty = true;
                this.Invalidate();
            }
        }

        /// <summary>
        /// The color to make the grid
        /// </summary>
        [Description("The color to make the grid"),
        Category("Appearance"),
        DefaultValue(typeof(Color), "Control")]
        public Color GridColor
        {
            get
            {
                return _gridColor;
            }
            set
            {
                _gridColor = value;

                _imageDirty = true;
                this.Invalidate();
            }
        }

        /// <summary>
        /// This is whether to show the tooltip or not
        /// NOTE:  The tooltip is VERY annoying.  Only use for debugging.
        /// </summary>
        [Description("This is whether to show the tooltip or not"),
        Category("Appearance"),
        DefaultValue(false)]
        public bool ShowToolTip
        {
            get
            {
                return toolTip1.Active;
            }
            set
            {
                toolTip1.Active = value;
                btnShowToolTip.Checked = value;
            }
        }

        /// <summary>
        /// This allows outside code to turn off the ability of the user to change the magnitude
        /// </summary>
        [Description("This allows outside code to turn off the ability of the user to change the magnitude"),
       Category("Behavior"),
        DefaultValue(true)]
        public bool EnableMagnitudeChangeInContextMenu
        {
            get
            {
                return txtMultiplier.Enabled;
            }
            set
            {
                txtMultiplier.Enabled = value;
            }
        }

        #endregion

        #region Public Methods

        public void StoreNewValue(MyVector valueToGrab)
        {
            StoreNewValue(valueToGrab.X, valueToGrab.Y, valueToGrab.Z);
        }
        public void StoreNewValue(double x, double y, double z)
        {
            // Store what was passed in
            _vector.X = x;
            _vector.Y = y;
            _vector.Z = z;

            // See if the multiplier needs to be expanded
            double newMagnitude = _vector.GetMagnitude();

            if (newMagnitude > _multiplier)
            {
                // I need to set the property so that the appropriate events fire
                this.Multiplier = newMagnitude * 1.1d;		// leave a bit more room for growth
            }
            else
            {
                // I put this in an else, because setting the multiplier invalidates as well
                _imageDirty = true;
                this.Invalidate();
            }

            if (_vector.IsZero)
            {
                toolTip1.SetToolTip(this, _vector.ToString(0) + "\nLength = 0");
                displayItem1.Text = _vector.ToString(0);
                displayItem2.Text = "Length = 0";
            }
            else
            {
                toolTip1.SetToolTip(this, _vector.ToString(2) + "\nlength = " + _vector.GetMagnitude().ToString("N2"));
                displayItem1.Text = _vector.ToString(2);
                displayItem2.Text = "Length = " + _vector.GetMagnitude().ToString("N2");
            }

            // Inform the world
            OnValueChanged();
        }

        #endregion

        #region Overrides

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isMouseDown = true;
            }

            //TODO: Support context menu


            base.OnMouseDown(e);
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isMouseDown = false;

                StoreMouseMove(e.X, e.Y);
            }

            base.OnMouseUp(e);
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_isMouseDown)
            {
                StoreMouseMove(e.X, e.Y);
            }

            base.OnMouseMove(e);
        }

        protected override void OnResize(EventArgs e)
        {
            _imageDirty = true;
            this.Invalidate();

            base.OnResize(e);
        }
        protected override void OnBackColorChanged(EventArgs e)
        {
            _imageDirty = true;
            this.Invalidate();

            base.OnBackColorChanged(e);
        }
        protected override void OnForeColorChanged(EventArgs e)
        {
            _imageDirty = true;
            this.Invalidate();

            base.OnForeColorChanged(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Make sure the image is up to date
            if (_backImage == null || _backImage.Width != this.Width || _backImage.Height != this.Height || _imageDirty)
            {
                RedrawImage();
            }

            // Blit the appropriate region
            e.Graphics.DrawImageUnscaled(_backImage, 0, 0);
        }

        #endregion
        #region Protected Methods

        protected virtual void OnValueChanged()
        {
            if (ValueChanged != null)
            {
                ValueChanged(this, new EventArgs());
            }
        }

        protected virtual void OnMultiplierChanged()
        {
            if (MultiplierChanged != null)
            {
                MultiplierChanged(this, new EventArgs());
            }
        }

        #endregion

        #region Misc Control Events

        private void contextMenuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (e.ClickedItem == displayItem1)
            {
                // Do nothing
            }
            else if (e.ClickedItem == btnNormalize)
            {
                #region Normalize

                if (_vector.IsZero)
                {
                    MessageBox.Show("The vector is zero length.  It can't be normalized.", "Context Menu Click", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    StoreNewValue(MyVector.BecomeUnitVector(_vector));
                }

                #endregion
            }
            else if (e.ClickedItem == btnMaximize)
            {
                #region Maximize

                if (_vector.IsZero)
                {
                    MessageBox.Show("The vector is zero length.  It can't be maximized.", "Context Menu Click", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    MyVector newVect = MyVector.BecomeUnitVector(_vector);
                    newVect.Multiply(_multiplier);

                    StoreNewValue(newVect);
                }

                #endregion
            }
            else if (e.ClickedItem == btnRandom)
            {
                StoreNewValue(Utility3D.GetRandomVectorSpherical2D(_multiplier * SAFEPERCENT));		// go under just to be safe
            }
            else if (e.ClickedItem == btnNegate)
            {
                StoreNewValue(_vector.X * -1d, _vector.Y * -1d, _vector.Z * -1d);
            }
            else if (e.ClickedItem == btnZero)
            {
                StoreNewValue(0, 0, 0);
            }
            else if (e.ClickedItem == btnZeroX)
            {
                StoreNewValue(0, _vector.Y, _vector.Z);
            }
            else if (e.ClickedItem == btnZeroY)
            {
                StoreNewValue(_vector.X, 0, _vector.Z);
            }
            else if (e.ClickedItem == btnZeroZ)
            {
                StoreNewValue(_vector.X, _vector.Y, 0);
            }
            else if (e.ClickedItem == btnShowToolTip)
            {
                this.ShowToolTip = !btnShowToolTip.Checked;		// note: I turned off CheckOnClick (with that on, I got a feedback loop, and it kept negating itself)
            }
            else if (e.ClickedItem is ToolStripSeparator)
            {
                // Do Nothing
            }
            else
            {
                MessageBox.Show("Menu item is unknown", "Context Menu Click", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void txtMultiplier_TextChanged(object sender, EventArgs e)
        {
            if (_settingMagnitudeText)
            {
                return;
            }

            double maxVelocity;
            if (double.TryParse(txtMultiplier.Text, out maxVelocity))
            {
                this.Multiplier = maxVelocity;
                txtMultiplier.ForeColor = SystemColors.WindowText;
            }
            else
            {
                txtMultiplier.ForeColor = Color.Red;
            }
        }

        #endregion

        #region Private Methods

        private void StoreMouseMove(int x, int y)
        {
            MyVector safe = new MyVector();
            safe.X = UtilityHelper.GetScaledValue(_multiplier * -1d, _multiplier, 0, this.Width, x);
            safe.Y = UtilityHelper.GetScaledValue(_multiplier * -1d, _multiplier, 0, this.Height, y);

            double safeMultiplier = _multiplier * SAFEPERCENT;		// I don't want to butt up against the multiplier, or store value will increase it on me
            if (safe.GetMagnitudeSquared() > safeMultiplier * safeMultiplier)
            {
                safe.BecomeUnitVector();
                safe.Multiply(safeMultiplier);
            }

            StoreNewValue(safe.X, safe.Y, 0d);
        }

        private void RedrawImage()
        {

            // Kill the old image
            if (_backImage != null)
            {
                _backImage.Dispose();
                _backImage = null;
            }

            // Draw the image
            _backImage = new Bitmap(this.Width, this.Height);
            using (Graphics graphics = Graphics.FromImage(_backImage))
            {
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

                graphics.Clear(this.BackColor);

                if (_showGrid)
                {
                    #region Draw Grid

                    //TODO:  Support a spherical opacity gradient brush
                    using (Pen gridPen = new Pen(_gridColor))
                    {
                        float halfWidth = this.Width / 2f;
                        float quarterWidth = this.Width / 4f;

                        // Main Axiis
                        graphics.DrawLine(gridPen, 0, halfWidth, this.Width, halfWidth);
                        graphics.DrawLine(gridPen, halfWidth, 0, halfWidth, this.Height);

                        // Middle Circle
                        graphics.DrawEllipse(gridPen, quarterWidth, quarterWidth, halfWidth, halfWidth);

                        // Minor Axiis
                        float minorAxisLength = Convert.ToSingle(quarterWidth / Math.Sqrt(2d));

                        // Draw in inner circle
                        //graphics.DrawLine(gridPen, halfWidth - minorAxisLength, halfWidth - minorAxisLength, halfWidth + minorAxisLength, halfWidth + minorAxisLength);
                        //graphics.DrawLine(gridPen, halfWidth - minorAxisLength, halfWidth + minorAxisLength, halfWidth + minorAxisLength, halfWidth - minorAxisLength);

                        // Draw in outer circle
                        graphics.DrawLine(gridPen, halfWidth - minorAxisLength, halfWidth - minorAxisLength, halfWidth - (minorAxisLength * 2f), halfWidth - (minorAxisLength * 2f));
                        graphics.DrawLine(gridPen, halfWidth + minorAxisLength, halfWidth + minorAxisLength, halfWidth + (minorAxisLength * 2f), halfWidth + (minorAxisLength * 2f));

                        graphics.DrawLine(gridPen, halfWidth - minorAxisLength, halfWidth + minorAxisLength, halfWidth - (minorAxisLength * 2f), halfWidth + (minorAxisLength * 2f));
                        graphics.DrawLine(gridPen, halfWidth + minorAxisLength, halfWidth - minorAxisLength, halfWidth + (minorAxisLength * 2f), halfWidth - (minorAxisLength * 2f));
                    }

                    #endregion
                }

                if (!_vector.IsZero)
                {
                    #region Draw Vector

                    try
                    {
                        // Cache my half size
                        //NOTE:  This assumes that this control is always circular (not an ellipse)
                        double controlRadius = this.Width / 2d;


                        // Draw the vector
                        using (Pen pen = new Pen(this.ForeColor, 3f))
                        {
                            float controlRadiusF = Convert.ToSingle(controlRadius);

                            float drawX = Convert.ToSingle(UtilityHelper.GetScaledValue(0, this.Width, _multiplier * -1d, _multiplier, _vector.X));
                            float drawY = Convert.ToSingle(UtilityHelper.GetScaledValue(0, this.Height, _multiplier * -1d, _multiplier, _vector.Y));

                            graphics.DrawLine(pen, controlRadiusF, controlRadiusF, drawX, drawY);
                        }



                        #region OLD
                        /*
						// Get a vector that is scaled to the control
						MyVector drawVector = _vector.Clone();
						drawVector.Multiply(UtilityHelper.GetScaledValue(0, controlRadius, 0, _multiplier, _vector.GetMagnitude()));

						// Draw the vector
						using (Pen pen = new Pen(this.ForeColor, 3f))
						{
							float controlRadiusF = Convert.ToSingle(controlRadius);
							graphics.DrawLine(pen, controlRadiusF, controlRadiusF, Convert.ToSingle(controlRadiusF + drawVector.X), Convert.ToSingle(controlRadiusF + drawVector.Y));
						}
						*/
                        #endregion
                    }
                    catch (OverflowException)
                    {
                        graphics.Clear(Color.Tomato);
                    }

                    #endregion
                }

                // Draw border
                using (Pen borderPen = new Pen(Color.Black, 1.3f))
                {
                    graphics.DrawEllipse(borderPen, 0, 0, this.Width, this.Height);
                }
            }

            // The image is up to date
            _imageDirty = false;

        }

        #endregion
    }
}
