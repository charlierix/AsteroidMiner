using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Data;
using System.Text;
using System.Windows.Forms;

using Game.HelperClasses;
using Game.Orig.HelperClassesOrig;
using Game.Orig.HelperClassesGDI;
using Game.Orig.HelperClassesGDI.Controls;
using Game.Orig.Math3D;
using Game.Orig.Map;

namespace Game.Orig.TestersGDI.PhysicsPainter
{
    public partial class SolidBallProps : PiePanel
    {
        #region Declaration Section

        private const string BUTTON_SIZE = "Size";
        private const string BUTTON_VELOCITY = "Velocity";

        public BallProps ExposedProps = new BallProps();

        #endregion

        #region Constructor

        public SolidBallProps()
        {
            InitializeComponent();

            // Init Everything
            piePanelMenuTop1.AddButton(BUTTON_SIZE);
            piePanelMenuTop1.AddButton(BUTTON_VELOCITY);

            sizeProps1.ValueChanged += new EventHandler(Props_ValueChanged);
            velocityProps1.ValueChanged += new EventHandler(Props_ValueChanged);

            sizeProps1.SetProps(this.ExposedProps, true);		// This must be called after ValueChanged is hooked up (menu wasn't drawing properly)
            velocityProps1.SetProps(this.ExposedProps);

            // Position Everything
            Resized();

            // Show the size property
            //TODO:  Tell the menu to highlight the size button
            ShowPropertyTab(sizeProps1);
        }

        #endregion

        #region Misc Control Events

        private void piePanelMenuTop1_ButtonClicked(object sender, PieMenuButtonClickedArgs e)
        {
            switch (e.Name)
            {
                case BUTTON_SIZE:
                    ShowPropertyTab(sizeProps1);
                    break;

                case BUTTON_VELOCITY:
                    ShowPropertyTab(velocityProps1);
                    break;

                default:
                    MessageBox.Show("Unknown Button: " + e.Name, "SolidBallProps Button Clicked", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
            }
        }
        private void piePanelMenuTop1_DrawButton(object sender, PieMenuDrawButtonArgs e)
        {
            switch (e.Name)
            {
                case BUTTON_SIZE:
                    DrawSize(e);
                    break;

                case BUTTON_VELOCITY:
                    DrawVelocity(e);
                    break;

                default:
                    MessageBox.Show("Unknown Button: " + e.Name, "SolidBallProps Button Clicked", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
            }
        }

        private void Props_ValueChanged(object sender, EventArgs e)
        {
            piePanelMenuTop1.PropsChanged();
        }

        #endregion
        #region Overrides

        protected override void OnBackColorChanged(EventArgs e)
        {
            piePanelMenuTop1.BackColor = SystemColors.Control;

            sizeProps1.BackColor = this.BackColor;
            velocityProps1.BackColor = this.BackColor;

            base.OnBackColorChanged(e);
        }

        protected override void OnResize(EventArgs e)
        {
            Resized();
            base.OnResize(e);
        }

        #endregion

        #region Private Methods

        private void Resized()
        {
            if (piePanelMenuTop1 == null || sizeProps1 == null || velocityProps1 == null)
            {
                // OnResize is getting called before the child controls get created
                return;
            }

            piePanelMenuTop1.Top = 0;
            piePanelMenuTop1.Left = 0;
            piePanelMenuTop1.Width = this.Width;
            piePanelMenuTop1.Height = this.Height - sizeProps1.Height;

            sizeProps1.Left = 0;
            sizeProps1.Top = this.Height - sizeProps1.Height;
            sizeProps1.Width = this.Width;

            velocityProps1.Left = 0;
            velocityProps1.Top = this.Height - velocityProps1.Height;
            velocityProps1.Width = this.Width;
        }

        /// <summary>
        /// This shows the tab passed in, and hides all others
        /// </summary>
        private void ShowPropertyTab(PiePanelBottom propertyTab)
        {
            foreach (Control childControl in this.Controls)
            {
                if (childControl is PiePanelBottom)
                {
                    if (childControl == propertyTab)
                    {
                        childControl.Visible = true;
                    }
                    else
                    {
                        childControl.Visible = false;
                    }
                }
            }
        }

        private void DrawSize(PieMenuDrawButtonArgs e)
        {
            const double MINRADIUSPERCENT = .15d;
            const double MAXRADIUSPERCENT = 1d;
            const double MINTARGET = 20d;
            const double MAXTARGET = 1000d;

            #region Figure out the radius

            // Figure out how big the real ball will be
            double ballRadius = 0;

            switch (this.ExposedProps.SizeMode)
            {
                case BallProps.SizeModes.Draw:
                    //TODO:  Use a different line color
                    ballRadius = UtilityHelper.GetScaledValue(MINTARGET, MAXTARGET, 0d, 1d, .75d);
                    break;

                case BallProps.SizeModes.Fixed:
                    ballRadius = this.ExposedProps.SizeIfFixed;
                    break;

                case BallProps.SizeModes.Random:
                    ballRadius = (this.ExposedProps.MinRandSize + this.ExposedProps.MaxRandSize) / 2d;
                    break;

                default:
                    throw new ApplicationException("Unknown BallProps.SizeModes: " + this.ExposedProps.SizeMode.ToString());
            }

            // Figure out the radius to draw
            double radiusPercent = UtilityHelper.GetScaledValue_Capped(MINRADIUSPERCENT, MAXRADIUSPERCENT, MINTARGET, MAXTARGET, ballRadius);

            #endregion

            // Figure out the color
            Color radiusColor = GetGreenRedColor(MINTARGET, MAXTARGET, ballRadius, radiusPercent);

            float drawWidth = Convert.ToSingle((e.ButtonSize - 2) * radiusPercent);
            float halfDrawWidth = drawWidth * .5f;
            float halfSize = (e.ButtonSize - 2) * .5f;

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

            // Draw Radius
            using (Pen radiusPen = new Pen(radiusColor, 2f))
            {
                MyVector radiusLine = new MyVector(halfDrawWidth, 0, 0);
                radiusLine.RotateAroundAxis(new MyVector(0, 0, 1), Utility3D.GetDegreesToRadians(-30d));

                e.Graphics.DrawLine(radiusPen, halfSize, halfSize, Convert.ToSingle(halfSize + radiusLine.X), Convert.ToSingle(halfSize + radiusLine.Y));
            }

            // Draw Circle
            Color circleColor = Color.Black;
            if (this.ExposedProps.SizeMode == BallProps.SizeModes.Draw)
            {
                circleColor = SystemColors.ControlDark;
            }

            using (Pen circlePen = new Pen(circleColor, 2f))
            {
                e.Graphics.DrawEllipse(circlePen, halfSize - halfDrawWidth, halfSize - halfDrawWidth, drawWidth, drawWidth);
            }
        }
        private void DrawVelocity(PieMenuDrawButtonArgs e)
        {
            const double MINLENGTHPERCENT = 0d;
            const double MAXLENGTHPERCENT = 1d;
            const double MINTARGET = 10d;
            const double MAXTARGET = 250d;

            #region Figure out the length

            // Figure out how long the real velocity will be
            double realVelocity = 0d;

            if (this.ExposedProps.RandomVelocity)
            {
                realVelocity = this.ExposedProps.MaxVelocity * .75d;
            }
            else
            {
                if (this.ExposedProps.Velocity != null && !this.ExposedProps.Velocity.IsZero)
                {
                    realVelocity = this.ExposedProps.Velocity.GetMagnitude();
                }
            }

            // Figure out the velocity to draw
            double velocityPercent = 0d;
            if (realVelocity >= MINLENGTHPERCENT)
            {
                velocityPercent = UtilityHelper.GetScaledValue_Capped(MINLENGTHPERCENT, MAXLENGTHPERCENT, MINTARGET, MAXTARGET, realVelocity);
            }

            #endregion

            // Figure out the color
            Color velocityColor = GetGreenRedColor(MINTARGET, MAXTARGET, realVelocity, velocityPercent);

            float halfSize = e.ButtonSize * .5f;

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

            if (velocityPercent > 0d)
            {
                #region Draw Vector

                double drawLength = (e.ButtonSize / 2d) * velocityPercent;

                // Draw Vector
                using (Pen vectorPen = new Pen(velocityColor, 2f))
                {
                    vectorPen.StartCap = LineCap.Round;	// LineCap.RoundAnchor;
                    vectorPen.EndCap = LineCap.ArrowAnchor;

                    MyVector vectorLine;
                    if (this.ExposedProps.RandomVelocity)
                    {
                        // Draw a circle underneath
                        using (Pen circlePen = new Pen(SystemColors.ControlDark, 1f))
                        {
                            e.Graphics.DrawEllipse(circlePen, Convert.ToSingle(halfSize - drawLength), Convert.ToSingle(halfSize - drawLength), Convert.ToSingle(drawLength * 2d), Convert.ToSingle(drawLength * 2d));
                        }

                        vectorLine = new MyVector(drawLength, 0, 0);

                        vectorLine.RotateAroundAxis(new MyVector(0, 0, 1), Utility3D.GetDegreesToRadians(-60d));
                        e.Graphics.DrawLine(vectorPen, halfSize, halfSize, Convert.ToSingle(halfSize + vectorLine.X), Convert.ToSingle(halfSize + vectorLine.Y));

                        vectorLine.RotateAroundAxis(new MyVector(0, 0, 1), Utility3D.GetDegreesToRadians(-85d));
                        e.Graphics.DrawLine(vectorPen, halfSize, halfSize, Convert.ToSingle(halfSize + vectorLine.X), Convert.ToSingle(halfSize + vectorLine.Y));

                        vectorLine.RotateAroundAxis(new MyVector(0, 0, 1), Utility3D.GetDegreesToRadians(-150d));
                        e.Graphics.DrawLine(vectorPen, halfSize, halfSize, Convert.ToSingle(halfSize + vectorLine.X), Convert.ToSingle(halfSize + vectorLine.Y));

                    }
                    else
                    {
                        vectorLine = this.ExposedProps.Velocity.Clone();
                        vectorLine.BecomeUnitVector();
                        vectorLine.Multiply(drawLength);

                        e.Graphics.DrawLine(vectorPen, halfSize, halfSize, Convert.ToSingle(halfSize + vectorLine.X), Convert.ToSingle(halfSize + vectorLine.Y));
                    }
                }

                #endregion
            }
            else
            {
                e.Graphics.DrawString("Velocity", new Font("Arial", 8), Brushes.Black, 0, e.ButtonSize - 13);
            }

        }

        private Color GetGreenRedColor(double minTarget, double maxTarget, double actualValue, double sizePercent)
        {
            if (sizePercent < .9d)
            {
                return Color.OliveDrab;
            }
            else
            {
                double derivedMinTarget = UtilityHelper.GetScaledValue(minTarget, maxTarget, 0d, 1d, .9d);
                double colorPercent = UtilityHelper.GetScaledValue_Capped(0d, 1d, derivedMinTarget, maxTarget * 3d, actualValue);

                return UtilityGDI.AlphaBlend(Color.Firebrick, Color.OliveDrab, colorPercent);
            }
        }

        #endregion
    }
}
