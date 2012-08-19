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
using Game.Orig.Math3D;

namespace Game.Orig.TestersGDI.PhysicsPainter
{
	public partial class SolidBallPropsMenu : Game.Orig.HelperClassesGDI.Controls.PiePanelTop
    {
        #region Enum: WhichButton

        private enum WhichButton
        {
            None = 0,
            Size,
            Velocity
        }

        #endregion

        #region Declaration Section

		public event EventHandler SizeClicked = null;
		public event EventHandler VelocityClicked = null;

		private int _buttonSize = 10;

		private BallProps _exposedProps = null;

		private bool _isDirty = false;
		private Bitmap _bitmap = null;

		private WhichButton _hotTrack = WhichButton.None;
		private Brush _hotTrackBrush = null;
		private Pen _hotTrackPen = null;

		//	These are used for hit tests
		private Point _sizeLocation;
		private GraphicsPath _sizeOutline = null;
		private Point _velocityLocation;
		private GraphicsPath _velocityOutline = null;

		#endregion

		#region Constructor

		public SolidBallPropsMenu()
		{
			InitializeComponent();

			_hotTrackBrush = new SolidBrush(UtilityGDI.AlphaBlend(SystemColors.HotTrack, this.BackColor, .05d));
			_hotTrackPen = new Pen(UtilityGDI.AlphaBlend(SystemColors.HotTrack, this.BackColor, .33d));
		}

		#endregion

		#region Public Methods

		public void SetProps(BallProps props)
		{
			_exposedProps = props;
		}

		/// <summary>
		/// This tells me that the properties have changed, and I need to invalidate my buttons
		/// </summary>
		/// <remarks>
		/// TODO: If there is too much flicker, then this method should be broken into one for each button (but that would
		/// end up as a management headache for the calling class
		/// </remarks>
		public void PropsChanged()
		{
			_isDirty = true;
			this.Invalidate();
		}

		#endregion

		#region Overrides

		protected override void OnMouseDown(MouseEventArgs e)
		{
			//TODO, shift the location so it looks clicked

			base.OnMouseDown(e);
		}
		protected override void OnMouseUp(MouseEventArgs e)
		{
			switch (_hotTrack)
			{
				case WhichButton.Size:
					if (this.SizeClicked != null)
					{
						this.SizeClicked(this, new EventArgs());
					}
					break;

				case WhichButton.Velocity:
					if (this.VelocityClicked != null)
					{
						this.VelocityClicked(this, new EventArgs());
					}
					break;
			}

			base.OnMouseUp(e);
		}
		protected override void OnMouseMove(MouseEventArgs e)
		{
			//	See if they are over any of the buttons

			WhichButton oldHotTrack = _hotTrack;

			if (_sizeOutline.IsVisible(e.X, e.Y))
			{
				_hotTrack = WhichButton.Size;
			}
			else if (_velocityOutline.IsVisible(e.X, e.Y))
			{
				_hotTrack = WhichButton.Velocity;
			}
			else
			{
				_hotTrack = WhichButton.None;
			}

			if (oldHotTrack != _hotTrack)
			{
				_isDirty = true;
				this.Invalidate();
			}

			base.OnMouseMove(e);
		}
		protected override void OnMouseLeave(EventArgs e)
		{
			if (_hotTrack != WhichButton.None)
			{
				_hotTrack = WhichButton.None;
				_isDirty = true;
				this.Invalidate();
			}

			base.OnMouseLeave(e);
		}

		protected override void OnBackColorChanged(EventArgs e)
		{
			DrawAll();
			base.OnBackColorChanged(e);
		}

		protected override void OnResize(EventArgs e)
		{
			DrawAll();
			base.OnResize(e);
		}

		protected override void OnPaintBackground(PaintEventArgs e)
		{
			if (_bitmap == null)		//	If I have a bitmap, then my paint event will do everything
			{
				base.OnPaintBackground(e);
			}
		}
		protected override void OnPaint(PaintEventArgs e)
		{
			if (_bitmap == null)
			{
				base.OnPaintBackground(e);   // was this supposed to be OnPaint?
				DrawAll();
			}
			else
			{
				if (_isDirty)
				{
					DrawAll();
				}

				e.Graphics.DrawImageUnscaled(_bitmap, 0, 0);
			}
		}

		#endregion

		#region Private Methods

		private void DrawAll()
		{
			const int LEFTMARGIN = 3;
			const int TOPMARGIN = 6;
			const int MARGIN = 2;
			const int BOTTOMMARGIN = 3;

			if (_exposedProps == null)
			{
				return;
			}

			//	Calculate the button size
			int oldButtonSize = _buttonSize;
			_buttonSize = (this.Height - TOPMARGIN - BOTTOMMARGIN - (MARGIN * 1)) / 2;		//	integer division

			//	Build a new bitmap
			if (_bitmap != null)
			{
				_bitmap.Dispose();
				_bitmap = null;
			}

			_bitmap = new Bitmap(this.Width, this.Height);

			#region Build Button Outlines

			if (_sizeOutline != null && oldButtonSize != _buttonSize)
			{
				_sizeOutline.Dispose();
				_sizeOutline = null;
				_velocityOutline.Dispose();
				_velocityOutline = null;
			}

			if (_sizeOutline == null)
			{
				//TODO:  Use rounded rectangles
				//TODO:  Include the arc
				_sizeLocation = new Point(LEFTMARGIN, TOPMARGIN);
				_sizeOutline = new GraphicsPath();
				_sizeOutline.AddRectangle(new Rectangle(_sizeLocation.X, _sizeLocation.Y, _buttonSize, _buttonSize));

				_velocityLocation = new Point(LEFTMARGIN, TOPMARGIN + _buttonSize + MARGIN);
				_velocityOutline = new GraphicsPath();
				_velocityOutline.AddRectangle(new Rectangle(_velocityLocation.X, _velocityLocation.Y, _buttonSize, _buttonSize));
			}

			#endregion

			#region Draw

			using (Graphics graphics = Graphics.FromImage(_bitmap))
			{
				graphics.SmoothingMode = SmoothingMode.HighQuality;

				graphics.Clear(this.BackColor);

				//	Draw HotTrack background
				switch (_hotTrack)
				{
					case WhichButton.None:
						break;

					case WhichButton.Size:
						graphics.FillPath(_hotTrackBrush, _sizeOutline);
						break;

					case WhichButton.Velocity:
						graphics.FillPath(_hotTrackBrush, _velocityOutline);
						break;

					default:
						throw new ApplicationException("Unknown WhichButton: " + _hotTrack.ToString());
				}

				//	Size
				graphics.DrawImageUnscaled(DrawSize(), _sizeLocation.X, _sizeLocation.Y);

				//	Velocity
				graphics.DrawImageUnscaled(DrawVelocity(), _velocityLocation.X, _velocityLocation.Y);

				//	Draw HotTrack outline
				switch (_hotTrack)
				{
					case WhichButton.None:
						break;

					case WhichButton.Size:
						graphics.DrawPath(_hotTrackPen, _sizeOutline);
						break;

					case WhichButton.Velocity:
						graphics.DrawPath(_hotTrackPen, _velocityOutline);
						break;

					default:
						throw new ApplicationException("Unknown WhichButton: " + _hotTrack.ToString());
				}

				using (Pen pen = new Pen(SystemColors.ControlDark, 1.5f))
				{
					graphics.DrawPath(pen, this.RegionPath);
				}
			}

			#endregion

			_isDirty = false;
			this.Invalidate();
		}
		private Bitmap DrawSize()
		{
			const double MINRADIUSPERCENT = .15d;
			const double MAXRADIUSPERCENT = 1d;
			const double MINTARGET = 20d;
			const double MAXTARGET = 1000d;

			#region Figure out the radius

			//	Figure out how big the real ball will be
			double ballRadius = 0;

			switch (_exposedProps.SizeMode)
			{
				case BallProps.SizeModes.Draw:
					//TODO:  Use a different line color
					ballRadius = UtilityHelper.GetScaledValue(MINTARGET, MAXTARGET, 0d, 1d, .75d);
					break;

				case BallProps.SizeModes.Fixed:
					ballRadius = _exposedProps.SizeIfFixed;
					break;

				case BallProps.SizeModes.Random:
					ballRadius = (_exposedProps.MinRandSize + _exposedProps.MaxRandSize) / 2d;
					break;

				default:
					throw new ApplicationException("Unknown BallProps.SizeModes: " + _exposedProps.SizeMode.ToString());
			}

			//	Figure out the radius to draw
			double radiusPercent = UtilityHelper.GetScaledValue_Capped(MINRADIUSPERCENT, MAXRADIUSPERCENT, MINTARGET, MAXTARGET, ballRadius);

			#endregion

			//	Figure out the color
			Color radiusColor = GetGreenRedColor(MINTARGET, MAXTARGET, ballRadius, radiusPercent);

			float drawWidth = Convert.ToSingle((_buttonSize - 2) * radiusPercent);
			float halfDrawWidth = drawWidth * .5f;
			float halfSize = (_buttonSize - 2) * .5f;

			Bitmap retVal = new Bitmap(_buttonSize, _buttonSize);
			using (Graphics graphics = Graphics.FromImage(retVal))
			{
				graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

				//	Draw Radius
				using (Pen radiusPen = new Pen(radiusColor, 2f))
				{
                    MyVector radiusLine = new MyVector(halfDrawWidth, 0, 0);
                    radiusLine.RotateAroundAxis(new MyVector(0, 0, 1), Utility3D.GetDegreesToRadians(-30d));

					graphics.DrawLine(radiusPen, halfSize, halfSize, Convert.ToSingle(halfSize + radiusLine.X), Convert.ToSingle(halfSize + radiusLine.Y));
				}

				//	Draw Circle
				Color circleColor = Color.Black;
				if (_exposedProps.SizeMode == BallProps.SizeModes.Draw)
				{
					circleColor = SystemColors.ControlDark;
				}

				using (Pen circlePen = new Pen(circleColor, 2f))
				{
					graphics.DrawEllipse(circlePen, halfSize - halfDrawWidth, halfSize - halfDrawWidth, drawWidth, drawWidth);
				}
			}

			//	Exit Function
			return retVal;
		}
		private Bitmap DrawVelocity()
		{
			const double MINLENGTHPERCENT = 0d;
			const double MAXLENGTHPERCENT = 1d;
			const double MINTARGET = 10d;
			const double MAXTARGET = 250d;

			#region Figure out the length

			//	Figure out how long the real velocity will be
			double realVelocity = 0d;

			if (_exposedProps.RandomVelocity)
			{
				realVelocity = _exposedProps.MaxVelocity * .75d;
			}
			else
			{
				if (_exposedProps.Velocity != null && !_exposedProps.Velocity.IsZero)
				{
					realVelocity = _exposedProps.Velocity.GetMagnitude();
				}
			}

			//	Figure out the velocity to draw
			double velocityPercent = 0d;
			if (realVelocity >= MINLENGTHPERCENT)
			{
				velocityPercent = UtilityHelper.GetScaledValue_Capped(MINLENGTHPERCENT, MAXLENGTHPERCENT, MINTARGET, MAXTARGET, realVelocity);
			}

			#endregion

			//	Figure out the color
			Color velocityColor = GetGreenRedColor(MINTARGET, MAXTARGET, realVelocity, velocityPercent);

			float halfSize = _buttonSize * .5f;

			Bitmap retVal = new Bitmap(_buttonSize + 1, _buttonSize + 1);
			using (Graphics graphics = Graphics.FromImage(retVal))
			{
				graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

				if (velocityPercent > 0d)
				{
					double drawLength = (_buttonSize / 2d) * velocityPercent;

                    //	Draw Vector
					using (Pen vectorPen = new Pen(velocityColor, 2f))
					{
						vectorPen.StartCap = LineCap.Round;	// LineCap.RoundAnchor;
						vectorPen.EndCap = LineCap.ArrowAnchor;

                        MyVector vectorLine;
						if (_exposedProps.RandomVelocity)
						{
							//	Draw a circle underneath
							using (Pen circlePen = new Pen(SystemColors.ControlDark, 1f))
							{
								graphics.DrawEllipse(circlePen, Convert.ToSingle(halfSize - drawLength), Convert.ToSingle(halfSize - drawLength), Convert.ToSingle(drawLength * 2d), Convert.ToSingle(drawLength * 2d));
							}

                            vectorLine = new MyVector(drawLength, 0, 0);

                            vectorLine.RotateAroundAxis(new MyVector(0, 0, 1), Utility3D.GetDegreesToRadians(-60d));
							graphics.DrawLine(vectorPen, halfSize, halfSize, Convert.ToSingle(halfSize + vectorLine.X), Convert.ToSingle(halfSize + vectorLine.Y));

                            vectorLine.RotateAroundAxis(new MyVector(0, 0, 1), Utility3D.GetDegreesToRadians(-85d));
							graphics.DrawLine(vectorPen, halfSize, halfSize, Convert.ToSingle(halfSize + vectorLine.X), Convert.ToSingle(halfSize + vectorLine.Y));

                            vectorLine.RotateAroundAxis(new MyVector(0, 0, 1), Utility3D.GetDegreesToRadians(-150d));
							graphics.DrawLine(vectorPen, halfSize, halfSize, Convert.ToSingle(halfSize + vectorLine.X), Convert.ToSingle(halfSize + vectorLine.Y));

						}
						else
						{
							vectorLine = _exposedProps.Velocity.Clone();
							vectorLine.BecomeUnitVector();
							vectorLine.Multiply(drawLength);

							graphics.DrawLine(vectorPen, halfSize, halfSize, Convert.ToSingle(halfSize + vectorLine.X), Convert.ToSingle(halfSize + vectorLine.Y));
						}
					}
				}
			}

			//	Exit Function
			return retVal;
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
