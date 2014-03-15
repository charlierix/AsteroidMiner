using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using Game.Orig.HelperClassesGDI;
using Game.Orig.Math3D;

namespace Game.Orig.TestersGDI
{
    public partial class BallTester : Form
    {
        #region Declaration Section

        private Random _rand = new Random();

        private Bitmap _bitmap = null;
        private Graphics _graphics = null;

        #region Gravity Balls

        private MyVector _mouseLocation = null;

        private List<Ball> _balls = new List<Ball>();
        private List<Color> _ballColors = new List<Color>();
        private List<Trail> _ballTails = new List<Trail>();

        private double _gravitationalConstant = 0;

        #endregion

        #region Ship

        private Ball _ship = null;

        private bool _isUpPressed = false;
        private bool _isDownPressed = false;
        private bool _isLeftPressed = false;
        private bool _isRightPressed = false;

        #endregion

        #endregion

        #region Constructor

        public BallTester()
        {
            InitializeComponent();

            //System.Windows.Forms.Integration.EnableWindowsFormsInterop();

            _bitmap = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            _graphics = Graphics.FromImage(_bitmap);

            // Make the ship
            _ship = new Ball(new MyVector(300, 300, 0), new DoubleVector(new MyVector(0, 1, 0), new MyVector(1, 0, 0)), 60, 100, new MyVector(0, 0, -1), new MyVector(pictureBox1.Width, pictureBox1.Height, 1));

            _gravitationalConstant = trackBar1.Value;
        }

        #endregion

        #region Event Listeners

        private void button1_Click(object sender, EventArgs e)
        {
            Ball ball = new Ball(new MyVector(100, 100, 0), new DoubleVector(1, 0, 0, 0, 1, 0), 10, 5);

            ball = new Ball(new MyVector(100, 100, 0), new DoubleVector(1, 0, 0, 0, 1, 0), 10, 5, new MyVector(-100, -100, -100), new MyVector(100, 100, 100));
        }

        #region Gravity Balls

        private void btnAdd_Click(object sender, EventArgs e)
        {
            const int MINMASS = 10;
            const int MAXMASS = 1000;
            const int MINCOLOR = 75;
            const double MAXVELOCITY = 7;

            // Figure out ball properties
            int radius = _rand.Next(2, 31);
            MyVector position = Utility3D.GetRandomVector(new MyVector(radius, radius, 0), new MyVector(pictureBox1.Width - radius, pictureBox1.Height - radius, 0));
            DoubleVector dirFacing = new DoubleVector(new MyVector(1, 0, 0), new MyVector(0, 1, 0));
            int mass = _rand.Next(MINMASS, MAXMASS);
            MyVector boundingLower = new MyVector(0, 0, -1);
            MyVector boundingUpper = new MyVector(pictureBox1.Width, pictureBox1.Height, 1);

            double massPercent = Convert.ToDouble(mass - MINMASS) / Convert.ToDouble(MAXMASS - MINMASS);
            int colorValue = MINCOLOR + Convert.ToInt32((255 - MINCOLOR) * massPercent);

            // Make the ball
            _balls.Add(new Ball(position, dirFacing, radius, mass, boundingLower, boundingUpper));

            // Calculate the shell color
            if (chkRandVelocity.Checked)
            {
                _balls[_balls.Count - 1].Velocity.Add(Utility3D.GetRandomVector(MAXVELOCITY));
            }

            _ballColors.Add(Color.FromArgb(colorValue, colorValue, colorValue));

            // Make a tail
            _ballTails.Add(new Trail(100));
            _ballTails[_ballTails.Count - 1].SetColors(Color.MediumOrchid, Color.Black);
            _ballTails[_ballTails.Count - 1].SetWidths(3f, 1f);

            // Make sure the checkbox is checked
            if (!chkRunningGravBalls.Checked)
            {
                chkRunningGravBalls.Checked = true;
            }
        }
        private void btnClear_Click(object sender, EventArgs e)
        {
            _balls.Clear();
            _ballColors.Clear();
            _ballTails.Clear();
        }

        private void chkRunning_CheckedChanged(object sender, EventArgs e)
        {
            if (chkRunningGravBalls.Checked)
            {
                chkRunningShip.Checked = false;
            }

            timer1.Enabled = chkRunningGravBalls.Checked;
        }
        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            _gravitationalConstant = trackBar1.Value;
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            _mouseLocation = new MyVector(e.X, e.Y, 0);
        }
        private void pictureBox1_MouseLeave(object sender, EventArgs e)
        {
            _mouseLocation = null;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            const double ELAPSEDTIME = .1d;
            const double MAXVELOCITY = 100d;
            const double MAXVELOCITYSQUARED = MAXVELOCITY * MAXVELOCITY;

            bool drawAccel = chkDrawAccel.Checked;
            List<Ball> accelLines = new List<Ball>();
            List<Color> accelColors = new List<Color>();
            double force;

            // Tell the balls to move
            //foreach (Ball ball in _balls)
            for (int ballCntr = 0; ballCntr < _balls.Count; ballCntr++)
            {
                _balls[ballCntr].PrepareForNewTimerCycle();

                if (_mouseLocation != null)
                {
                    #region Apply Force (ball to mouse)

                    MyVector ballToMouse = _mouseLocation - _balls[ballCntr].Position;

                    double distanceSquared = ballToMouse.GetMagnitudeSquared();

                    ballToMouse.BecomeUnitVector();
                    // Force = G * (M1*M2) / R^2
                    force = _gravitationalConstant * (_balls[ballCntr].Mass / distanceSquared);
                    ballToMouse.Multiply(force);

                    _balls[ballCntr].ExternalForce.Add(ballToMouse);

                    if (drawAccel)
                    {
                        #region Store Accel Line

                        double acceleration = force / _balls[ballCntr].Mass;

                        if (acceleration > .05d)
                        {
                            accelLines.Add(_balls[ballCntr]);

                            if (acceleration > 1d)
                            {
                                accelColors.Add(Color.Yellow);
                            }
                            else
                            {
                                int color = Convert.ToInt32((acceleration - .05d) * (255d / .95d));
                                if (color < 0) color = 0;
                                if (color > 255) color = 255;
                                accelColors.Add(Color.FromArgb(color, color, 0));
                            }
                        }

                        #endregion
                    }

                    #endregion
                }

                _balls[ballCntr].TimerTestPosition(ELAPSEDTIME);
                _balls[ballCntr].TimerFinish();
                _balls[ballCntr].Velocity.Z = 0;
                _balls[ballCntr].Position.Z = 0;
                if (_balls[ballCntr].Velocity.GetMagnitudeSquared() > MAXVELOCITYSQUARED)
                {
                    _balls[ballCntr].Velocity.BecomeUnitVector();
                    _balls[ballCntr].Velocity.Multiply(MAXVELOCITY);
                }

                _ballTails[ballCntr].AddPoint(_balls[ballCntr].Position.Clone());
            }

            // Clear the view
            ClearPictureBox();

            // Draw the force lines
            if (accelLines.Count > 0)
            {
                for (int lineCntr = 0; lineCntr < accelLines.Count; lineCntr++)
                {
                    DrawVector(_mouseLocation, accelLines[lineCntr].Position, accelColors[lineCntr]);
                }
            }

            if (chkDrawTail.Checked)
            {
                foreach (Trail tail in _ballTails)
                {
                    tail.Draw(_graphics);
                }
            }

            // Draw the balls
            for (int ballCntr = 0; ballCntr < _balls.Count; ballCntr++)
            {
                DrawBall(_balls[ballCntr], _ballColors[ballCntr]);
            }

            BlitImage();
        }

        #endregion

        #region Ship

        private void chkRunningShip_CheckedChanged(object sender, EventArgs e)
        {
            if (chkRunningShip.Checked)
            {
                chkRunningGravBalls.Checked = false;
            }

            timer2.Enabled = chkRunningShip.Checked;
        }

        private void btnResetDirFacing_Click(object sender, EventArgs e)
        {
            _ship.ResetRotation();
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            const double GRAVITY = 200;
            const double FORWARDFORCE = 400;
            const double BACKWARDFORCE = 150;
            const double TURNRADIANS = .06;
            const double ELAPSEDTIME = .1;

            #region Color PictureBoxes

            if (_isUpPressed)
                pctUp.BackColor = SystemColors.ControlDark;
            else
                pctUp.BackColor = SystemColors.Control;

            if (_isDownPressed)
                pctDown.BackColor = SystemColors.ControlDark;
            else
                pctDown.BackColor = SystemColors.Control;

            if (_isLeftPressed)
                pctLeft.BackColor = SystemColors.ControlDark;
            else
                pctLeft.BackColor = SystemColors.Control;

            if (_isRightPressed)
                pctRight.BackColor = SystemColors.ControlDark;
            else
                pctRight.BackColor = SystemColors.Control;

            #endregion

            #region Do Physics

            _ship.PrepareForNewTimerCycle();

            if (_isUpPressed)
            {
                MyVector force = _ship.OriginalDirectionFacing.Standard.Clone();
                force.BecomeUnitVector();
                force.Multiply(FORWARDFORCE);
                _ship.InternalForce.Add(force);
            }

            if (_isDownPressed)
            {
                MyVector force = _ship.OriginalDirectionFacing.Standard.Clone();
                force.BecomeUnitVector();
                force.Multiply(BACKWARDFORCE * -1);
                _ship.InternalForce.Add(force);
            }

            if (_isLeftPressed)
            {
                _ship.RotateAroundAxis(new MyVector(0, 0, 1), TURNRADIANS * -1);
            }

            if (_isRightPressed)
            {
                _ship.RotateAroundAxis(new MyVector(0, 0, 1), TURNRADIANS);
            }

            if (chkApplyGravityShip.Checked)
            {
                _ship.ExternalForce.Add(new MyVector(0, GRAVITY, 0));
            }

            _ship.TimerTestPosition(ELAPSEDTIME);
            _ship.TimerFinish();

            #endregion

            #region Draw Ship

            ClearPictureBox();

            // Ball
            DrawBall(_ship, Color.DarkSeaGreen, Color.Chartreuse, Color.DarkOliveGreen);

            BlitImage();

            #endregion
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

            base.OnKeyUp(e);
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

        private void DrawBall(Ball ball, Color ballColor)
        {
            // Turn it into ints
            MyVector topLeft = ball.Position.Clone();
            topLeft.X -= ball.Radius;
            topLeft.Y -= ball.Radius;
            Point topLeftInt = topLeft.ToPoint();
            int size = Convert.ToInt32(ball.Radius * 2);

            // Draw the ball
            _graphics.DrawEllipse(new Pen(ballColor, 3), topLeftInt.X, topLeftInt.Y, size, size);
        }
        private void DrawBall(Ball ball, Color ballColor, Color dirFacingStandColor, Color dirFacingOrthColor)
        {
            // Standard Facing
            MyVector workingVect = ball.DirectionFacing.Standard.Clone();
            workingVect.BecomeUnitVector();
            workingVect.Multiply(ball.Radius);
            workingVect.Add(ball.Position);

            DrawVector(ball.Position, workingVect, dirFacingStandColor);

            // Orthogonal Facing
            workingVect = ball.DirectionFacing.Orth.Clone();
            workingVect.BecomeUnitVector();
            workingVect.Multiply(ball.Radius);
            workingVect.Add(ball.Position);

            DrawVector(ball.Position, workingVect, dirFacingOrthColor);

            // Ball
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