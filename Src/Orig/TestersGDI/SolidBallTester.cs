using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using Game.Orig.Math3D;

namespace Game.Orig.TestersGDI
{
	public partial class SolidBallTester : Form
	{
		#region Declaration Section

		private MyVector _boundryLower = null;
        private MyVector _boundryUpper = null;

		private Bitmap _bitmap = null;
		private Graphics _graphics = null;

		#region Pool Ball

		private SolidBall _poolBall = null;

		private bool _isMouseDown = false;
		private bool _isMouseJustReleased = false;
		private Point _mouseDownPoint;
		private Point _curMousePoint;

		#endregion

		#region Ship

		private SolidBall _ship = null;

		private bool _isUpPressed = false;
		private bool _isDownPressed = false;
		private bool _isLeftPressed = false;
		private bool _isRightPressed = false;
		private bool _isWPressed = false;
		private bool _isSPressed = false;

        private MyVector _shipThrusterOffset_BottomRight = null;
        private MyVector _shipThrusterOffset_BottomLeft = null;
        private MyVector _shipThrusterOffset_TopRight = null;
        private MyVector _shipThrusterOffset_TopLeft = null;
		
		#endregion

		#endregion

		#region Constructor

		public SolidBallTester()
		{
			InitializeComponent();

			_bitmap = new Bitmap(pictureBox1.DisplayRectangle.Width, pictureBox1.DisplayRectangle.Height);
			_graphics = Graphics.FromImage(_bitmap);

            _boundryLower = new MyVector(0, 0, 0);
            _boundryUpper = new MyVector(pictureBox1.DisplayRectangle.Width, pictureBox1.DisplayRectangle.Height, 0);
		}

		#endregion

		#region Event Listeners

		#region Pool Ball

		private void button1_Click(object sender, EventArgs e)
		{

			_poolBall = new SolidBall(GetMiddlePoint(), new DoubleVector(1, 0, 0, 0, 1, 0), 80, 100, _boundryLower, _boundryUpper);

			chkPoolRunning.Enabled = true;
			chkPoolRunning.Checked = true;

		}

		private void chkPoolRunning_CheckedChanged(object sender, EventArgs e)
		{
			if (chkPoolRunning.Checked)
			{
				chkShipRunning.Checked = false;
			}

			timer1.Enabled = chkPoolRunning.Checked;
		}

		private void timer1_Tick(object sender, EventArgs e)
		{

			const double ELAPSEDTIME = .5;

			#region Do Physics

			_poolBall.PrepareForNewTimerCycle();


			if (_isMouseJustReleased)
			{
				_isMouseJustReleased = false;

                MyVector force = new MyVector(_mouseDownPoint);
                force.Subtract(new MyVector(_curMousePoint));

                MyVector offset = new MyVector(_mouseDownPoint);
				offset.Subtract(_poolBall.Position);

				_poolBall.ApplyExternalForce(offset, force);
			}


			_poolBall.TimerTestPosition(ELAPSEDTIME);
			_poolBall.TimerFinish();

			#endregion

			#region Draw

			ClearPictureBox();

			if (_isMouseDown)
			{
                DrawVector(_poolBall.Position, new MyVector(_mouseDownPoint.X, _mouseDownPoint.Y, 0), Color.Olive);
                DrawVector(new MyVector(_mouseDownPoint.X, _mouseDownPoint.Y, 0), new MyVector(_curMousePoint.X, _curMousePoint.Y, 0), Color.Gold);
			}

			DrawBall(_poolBall, Color.Silver, Color.MediumPurple, Color.Purple);

			BlitImage();

			#endregion

		}

		private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
		{
			_isMouseDown = true;
			_isMouseJustReleased = false;

			_mouseDownPoint.X = e.X;
			_mouseDownPoint.Y = e.Y;

			_curMousePoint.X = e.X;
			_curMousePoint.Y = e.Y;
		}
		private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
		{
			_isMouseDown = false;
			_isMouseJustReleased = true;

			_curMousePoint.X = e.X;
			_curMousePoint.Y = e.Y;
		}
		private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
		{
			_curMousePoint.X = e.X;
			_curMousePoint.Y = e.Y;
		}

		#endregion

		#region Ship

		private void btnResetShip_Click(object sender, EventArgs e)
		{
			//	Set up the ship
			double radius = 60;
            MyVector radiusBoundry = new MyVector(radius, radius, 0);
            MyVector boundryLower = _boundryLower + radiusBoundry;
            MyVector boundryUpper = _boundryUpper - radiusBoundry;
			_ship = new SolidBall(GetMiddlePoint(), new DoubleVector(0, -1, 0, -1, 0, 0), radius, 100, boundryLower, boundryUpper);

			//	Set up the thrusters
			trackBar1_Scroll(this, new EventArgs());

			chkShipRunning.Enabled = true;
			chkShipRunning.Checked = true;
		}

		private void chkShipRunning_CheckedChanged(object sender, EventArgs e)
		{
			if (chkShipRunning.Checked)
			{
				chkPoolRunning.Checked = false;
			}

			timer2.Enabled = chkShipRunning.Checked;
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (keyData == Keys.Up)
			{
				_isUpPressed = true;
			}

			if (keyData == Keys.Down)
			{
				_isDownPressed = true;
			}

			if (keyData == Keys.Left)
			{
				_isLeftPressed = true;
			}

			if (keyData == Keys.Right)
			{
				_isRightPressed = true;
			}

			if (keyData == Keys.W)
			{
				_isWPressed = true;
			}

			if (keyData == Keys.S)
			{
				_isSPressed = true;
			}

			//return base.ProcessCmdKey(ref msg, keyData);
			return true;
		}
		protected override void OnKeyUp(KeyEventArgs e)
		{

			if (e.KeyData == Keys.Up)
			{
				_isUpPressed = false;
			}

			if (e.KeyData == Keys.Down)
			{
				_isDownPressed = false;
			}

			if (e.KeyData == Keys.Left)
			{
				_isLeftPressed = false;
			}

			if (e.KeyData == Keys.Right)
			{
				_isRightPressed = false;
			}

			if (e.KeyData == Keys.W)
			{
				_isWPressed = false;
			}

			if (e.KeyData == Keys.S)
			{
				_isSPressed = false;
			}

			base.OnKeyUp(e);
		}

		private void trackBar1_Scroll(object sender, EventArgs e)
		{
            MyVector thrusterSeed = new MyVector(0, _ship.Radius, 0);
            MyVector zAxis = new MyVector(0, 0, 1);
			double angle = trackBar1.Value;

			//	Bottom Thrusters
			_shipThrusterOffset_BottomRight = thrusterSeed.Clone();
			_shipThrusterOffset_BottomRight.RotateAroundAxis(zAxis, Utility3D.GetDegreesToRadians(angle * -1));

			_shipThrusterOffset_BottomLeft = thrusterSeed.Clone();
			_shipThrusterOffset_BottomLeft.RotateAroundAxis(zAxis, Utility3D.GetDegreesToRadians(angle));

			//	Top Thrusters
            thrusterSeed = new MyVector(0, _ship.Radius * -1, 0);
			_shipThrusterOffset_TopRight = thrusterSeed.Clone();
			_shipThrusterOffset_TopRight.RotateAroundAxis(zAxis, Utility3D.GetDegreesToRadians(angle));

			_shipThrusterOffset_TopLeft = thrusterSeed.Clone();
			_shipThrusterOffset_TopLeft.RotateAroundAxis(zAxis, Utility3D.GetDegreesToRadians(angle * -1));
		}

		private void timer2_Tick(object sender, EventArgs e)
		{
			const double GRAVITY = 20;
			const double THRUSTERFORCE = 30;
			const double ELAPSEDTIME = .5;

            List<MyVector[]> thrustLines = new List<MyVector[]>();

			#region Color PictureBoxes

			if (_isUpPressed)
				pctUp.BackColor = SystemColors.ControlDark;
			else
				pctUp.BackColor = SystemColors.Control;

			if (_isDownPressed)
				pctDown.BackColor = SystemColors.ControlDark;
			else
				pctDown.BackColor = SystemColors.Control;

			if (_isLeftPressed && radPairedThrusters.Checked)
				pctLeft.BackColor = SystemColors.ControlDark;
			else
				pctLeft.BackColor = SystemColors.Control;

			if (_isRightPressed && radPairedThrusters.Checked)
				pctRight.BackColor = SystemColors.ControlDark;
			else
				pctRight.BackColor = SystemColors.Control;

			if (_isWPressed && radIndependantThrusters.Checked)
				pctW.BackColor = SystemColors.ControlDark;
			else
				pctW.BackColor = SystemColors.Control;

			if (_isSPressed && radIndependantThrusters.Checked)
				pctS.BackColor = SystemColors.ControlDark;
			else
				pctS.BackColor = SystemColors.Control;

			#endregion

			#region Do Physics

			_ship.PrepareForNewTimerCycle();

			if (radIndependantThrusters.Checked)
			{
				#region Independant Thrusters

				if (_isUpPressed)
				{
                    timer2_TickSprtApplyThrust(thrustLines, _shipThrusterOffset_BottomRight, new MyVector(0, -1, 0), THRUSTERFORCE);		//	up
				}

				if (_isDownPressed)
				{
                    timer2_TickSprtApplyThrust(thrustLines, _shipThrusterOffset_TopRight, new MyVector(0, 1, 0), THRUSTERFORCE);		//	down
				}

				if (_isWPressed)
				{
                    timer2_TickSprtApplyThrust(thrustLines, _shipThrusterOffset_BottomLeft, new MyVector(0, -1, 0), THRUSTERFORCE);		//	w
				}

				if (_isSPressed)
				{
                    timer2_TickSprtApplyThrust(thrustLines, _shipThrusterOffset_TopLeft, new MyVector(0, 1, 0), THRUSTERFORCE);		//	s
				}

				#endregion
			}
			else
			{
				#region Paired Thrusters

				if (_isUpPressed)
				{
                    timer2_TickSprtApplyThrust(thrustLines, _shipThrusterOffset_BottomRight, new MyVector(0, -1, 0), THRUSTERFORCE);		//	up
                    timer2_TickSprtApplyThrust(thrustLines, _shipThrusterOffset_BottomLeft, new MyVector(0, -1, 0), THRUSTERFORCE);		//	w
				}

				if (_isDownPressed)
				{
                    timer2_TickSprtApplyThrust(thrustLines, _shipThrusterOffset_TopRight, new MyVector(0, 1, 0), THRUSTERFORCE);		//	down
                    timer2_TickSprtApplyThrust(thrustLines, _shipThrusterOffset_TopLeft, new MyVector(0, 1, 0), THRUSTERFORCE);		//	s
				}

				if (_isLeftPressed)
				{
                    timer2_TickSprtApplyThrust(thrustLines, _shipThrusterOffset_BottomRight, new MyVector(0, -1, 0), THRUSTERFORCE);		//	up
                    timer2_TickSprtApplyThrust(thrustLines, _shipThrusterOffset_TopLeft, new MyVector(0, 1, 0), THRUSTERFORCE);		//	s
				}

				if (_isRightPressed)
				{
                    timer2_TickSprtApplyThrust(thrustLines, _shipThrusterOffset_TopRight, new MyVector(0, 1, 0), THRUSTERFORCE);		//	down
                    timer2_TickSprtApplyThrust(thrustLines, _shipThrusterOffset_BottomLeft, new MyVector(0, -1, 0), THRUSTERFORCE);		//	w
				}

				#endregion
			}

			if (chkShipGravity.Checked)
			{
                _ship.ExternalForce.Add(new MyVector(0, GRAVITY, 0));
			}

			_ship.TimerTestPosition(ELAPSEDTIME);
			_ship.TimerFinish();

			#endregion

			#region Draw

			ClearPictureBox();

			//	Ship
			DrawBall(_ship, Color.SteelBlue, Color.LightSteelBlue, Color.DimGray);

			//	Thrusters
			DrawThruster(_ship, _shipThrusterOffset_BottomRight);
			DrawThruster(_ship, _shipThrusterOffset_BottomLeft);
			DrawThruster(_ship, _shipThrusterOffset_TopRight);
			DrawThruster(_ship, _shipThrusterOffset_TopLeft);

			//	Thrust Lines
            foreach (MyVector[] thrustPair in thrustLines)
			{
                MyVector thrustStart = _ship.Rotation.GetRotatedVector(thrustPair[0], true);
				thrustStart.Add(_ship.Position);

                MyVector thrustStop = thrustPair[1] * -20d;
				thrustStop.Add(thrustPair[0]);
				thrustStop = _ship.Rotation.GetRotatedVector(thrustStop, true);
				thrustStop.Add(_ship.Position);

				DrawVector(thrustStart, thrustStop, Color.Coral, 4);
			}

			BlitImage();

			#endregion
		}
        private void timer2_TickSprtApplyThrust(List<MyVector[]> thrustLines, MyVector offset, MyVector force, double forceMultiplier)
		{
            thrustLines.Add(new MyVector[] { offset, force });

			_ship.ApplyInternalForce(offset, force * forceMultiplier);
		}

        private void DrawThruster(SolidBall ship, MyVector thruster)
		{
            MyVector worldThruster = _ship.Rotation.GetRotatedVector(thruster, true);
			worldThruster.Add(_ship.Position);

			DrawDot(worldThruster, 3f, Color.Silver);
		}

		#endregion

		#endregion

		#region Private Methods

		private void ClearPictureBox()
		{
			_graphics.Clear(pictureBox1.BackColor);
		}

        private void DrawVector(MyVector fromPoint, MyVector toPoint, Color color)
		{
			_graphics.DrawLine(new Pen(color), fromPoint.ToPoint(), toPoint.ToPoint());
		}
        private void DrawVector(MyVector fromPoint, MyVector toPoint, Color color, int lineWidth)
		{
			_graphics.DrawLine(new Pen(color, lineWidth), fromPoint.ToPoint(), toPoint.ToPoint());
		}

        private void DrawDot(MyVector centerPoint, float radius, Color color)
		{

			float centerX = Convert.ToSingle(centerPoint.X);
			float centerY = Convert.ToSingle(centerPoint.Y);

			_graphics.FillEllipse(new SolidBrush(color), centerX - radius, centerY - radius, radius * 2f, radius * 2f);

		}

		private void DrawBall(SolidBall ball, Color ballColor)
		{

			//	Turn it into ints
            MyVector topLeft = ball.Position.Clone();
			topLeft.X -= ball.Radius;
			topLeft.Y -= ball.Radius;
			Point topLeftInt = topLeft.ToPoint();
			int size = Convert.ToInt32(ball.Radius * 2);

			//	Draw the ball
			_graphics.DrawEllipse(new Pen(ballColor, 3), topLeftInt.X, topLeftInt.Y, size, size);

		}
		private void DrawBall(SolidBall ball, Color ballColor, Color dirFacingStandColor, Color dirFacingOrthColor)
		{

			//	Standard Facing
            MyVector workingVect = ball.DirectionFacing.Standard.Clone();
			workingVect.BecomeUnitVector();
			workingVect.Multiply(ball.Radius);
			workingVect.Add(ball.Position);

			DrawVector(ball.Position, workingVect, dirFacingStandColor);

			//	Orthogonal Facing
			workingVect = ball.DirectionFacing.Orth.Clone();
			workingVect.BecomeUnitVector();
			workingVect.Multiply(ball.Radius);
			workingVect.Add(ball.Position);

			DrawVector(ball.Position, workingVect, dirFacingOrthColor);

			//	Ball
			DrawBall(ball, ballColor);

		}

		private void BlitImage()
		{
			pictureBox1.CreateGraphics().DrawImageUnscaled(_bitmap, 0, 0);
		}

        private MyVector GetMiddlePoint()
		{
            MyVector retVal = new MyVector();

			retVal.X = pictureBox1.DisplayRectangle.Width / 2d;
			retVal.Y = pictureBox1.DisplayRectangle.Height / 2d;
			retVal.Z = 0d;

			return retVal;
		}

		#endregion
	}
}