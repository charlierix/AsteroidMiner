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
    public partial class RigidBodyTester1 : Form
    {
        #region Enums

        private enum GlobalState
        {
            AddingMass = 0,
            AddingThruster,
            Running
        }

        private enum ThrusterKey
        {
            A = 0,
            S,
            D,
            W,
            Up,
            Down,
            Left,
            Right
        }

        private enum GravityMode
        {
            None = 0,
            Down,
            Ball
        }

        private enum ThrustMode
        {
            Standard = 0,
            Twin,
            Custom
        }

        #endregion
        #region Declaration Section

        private const double MINSHIPRADIUS = .1d;

        private GlobalState _state;
        private ThrusterKey _addThrusterTo;		//	This only has meaning when _state is AddingThruster

        #region Keyboard/Mouse

        private bool _isMouseDown = false;
        private bool _isMouseJustReleased = false;
        private Point _mouseDownPoint;
        private Point _curMousePoint;

        private bool _isUpPressed = false;
        private bool _isDownPressed = false;
        private bool _isLeftPressed = false;
        private bool _isRightPressed = false;
        private bool _isAPressed = false;
        private bool _isSPressed = false;
        private bool _isDPressed = false;
        private bool _isWPressed = false;

        #endregion

        private MyVector _boundryLower = null;
        private MyVector _boundryUpper = null;

        //	These are used during design mode
        private Bitmap _bitmap = null;
        private Graphics _graphics = null;

        private GravityMode _gravityMode;
        private ThrustMode _thrustMode;

        private RigidBody _ship;
        private MyVector _centerMassWorld;

        //	The keyboard is the key, the value is a list of offset/force vectors
        private SortedList<ThrusterKey, List<MyVector[]>> _thrustersCustom = new SortedList<ThrusterKey, List<MyVector[]>>();		//	vector[0] - offset, vector[1] - force
        private SortedList<ThrusterKey, List<MyVector[]>> _thrustersStandard = new SortedList<ThrusterKey, List<MyVector[]>>();		//	vector[0] - offset, vector[1] - force
        private SortedList<ThrusterKey, Color> _thrusterColors = new SortedList<ThrusterKey, Color>();

        //	This controls the force of the thrusters (it's tied to direction radio buttons)
        private double _thrustForceMultiplier = 1d;

        private List<Ball> _gravityBalls = new List<Ball>();
        private List<Color> _gravityBallColors = new List<Color>();

        #endregion

        #region Constructor

        public RigidBodyTester1()
        {
            const double THRUST = 500d;

            InitializeComponent();

            _bitmap = new Bitmap(pictureBox1.DisplayRectangle.Width, pictureBox1.DisplayRectangle.Height);
            _graphics = Graphics.FromImage(_bitmap);

            _boundryLower = new MyVector(-12000, -12000, 0);
            _boundryUpper = new MyVector(12000, 12000, 0);

            pictureBox1.SetBorder(_boundryLower, _boundryUpper);
            pictureBox1.ShowBorder(Color.Chartreuse, 3);
            pictureBox1.ShowCheckerBackground(Color.FromArgb(24, 24, 24), 15);
            btnResetZoom_Click(this, new EventArgs());

            radGravity_CheckedChanged(this, new EventArgs());
            radThrustDirection_CheckedChanged(this, new EventArgs());

            #region Define Thruster Colors

            _thrusterColors.Add(ThrusterKey.A, Color.SandyBrown);
            _thrusterColors.Add(ThrusterKey.S, Color.Coral);
            _thrusterColors.Add(ThrusterKey.D, Color.DarkOrange);
            _thrusterColors.Add(ThrusterKey.W, Color.Sienna);

            _thrusterColors.Add(ThrusterKey.Up, Color.OliveDrab);
            _thrusterColors.Add(ThrusterKey.Down, Color.DarkSeaGreen);
            _thrusterColors.Add(ThrusterKey.Left, Color.GreenYellow);
            _thrusterColors.Add(ThrusterKey.Right, Color.LimeGreen);

            #endregion
            #region Define Standard Thrusters

            //	Up
            _thrustersStandard.Add(ThrusterKey.Up, new List<MyVector[]>());
            _thrustersStandard[ThrusterKey.Up].Add(new MyVector[] { new MyVector(200, 0, 0), new MyVector(0, THRUST, 0) });
            _thrustersStandard[ThrusterKey.Up].Add(new MyVector[] { new MyVector(-200, 0, 0), new MyVector(0, THRUST, 0) });

            //	Down
            _thrustersStandard.Add(ThrusterKey.Down, new List<MyVector[]>());
            _thrustersStandard[ThrusterKey.Down].Add(new MyVector[] { new MyVector(200, 0, 0), new MyVector(0, THRUST * -1d, 0) });
            _thrustersStandard[ThrusterKey.Down].Add(new MyVector[] { new MyVector(-200, 0, 0), new MyVector(0, THRUST * -1d, 0) });

            //	Left
            _thrustersStandard.Add(ThrusterKey.Left, new List<MyVector[]>());
            _thrustersStandard[ThrusterKey.Left].Add(new MyVector[] { new MyVector(200, 0, 0), new MyVector(0, THRUST * .33d, 0) });
            _thrustersStandard[ThrusterKey.Left].Add(new MyVector[] { new MyVector(-200, 0, 0), new MyVector(0, THRUST * -.33d, 0) });

            //	Right
            _thrustersStandard.Add(ThrusterKey.Right, new List<MyVector[]>());
            _thrustersStandard[ThrusterKey.Right].Add(new MyVector[] { new MyVector(200, 0, 0), new MyVector(0, THRUST * -.33d, 0) });
            _thrustersStandard[ThrusterKey.Right].Add(new MyVector[] { new MyVector(-200, 0, 0), new MyVector(0, THRUST * .33d, 0) });

            //	W
            _thrustersStandard.Add(ThrusterKey.W, new List<MyVector[]>());
            _thrustersStandard[ThrusterKey.W].Add(new MyVector[] { new MyVector(150, 0, 0), new MyVector(0, THRUST, 0) });
            _thrustersStandard[ThrusterKey.W].Add(new MyVector[] { new MyVector(-150, 0, 0), new MyVector(0, THRUST, 0) });

            //	S
            _thrustersStandard.Add(ThrusterKey.S, new List<MyVector[]>());
            _thrustersStandard[ThrusterKey.S].Add(new MyVector[] { new MyVector(150, 0, 0), new MyVector(0, THRUST * -1d, 0) });
            _thrustersStandard[ThrusterKey.S].Add(new MyVector[] { new MyVector(-150, 0, 0), new MyVector(0, THRUST * -1d, 0) });

            //	A
            _thrustersStandard.Add(ThrusterKey.A, new List<MyVector[]>());
            _thrustersStandard[ThrusterKey.A].Add(new MyVector[] { new MyVector(0, 150, 0), new MyVector(THRUST, 0, 0) });
            _thrustersStandard[ThrusterKey.A].Add(new MyVector[] { new MyVector(0, -150, 0), new MyVector(THRUST, 0, 0) });

            //	D
            _thrustersStandard.Add(ThrusterKey.D, new List<MyVector[]>());
            _thrustersStandard[ThrusterKey.D].Add(new MyVector[] { new MyVector(0, 150, 0), new MyVector(THRUST * -1, 0, 0) });
            _thrustersStandard[ThrusterKey.D].Add(new MyVector[] { new MyVector(0, -150, 0), new MyVector(THRUST * -1, 0, 0) });

            #endregion
            #region Define Twin Thrusters
            #endregion

            //	Pretend they clicked create new ship
            btnResetShip_Click(this, new EventArgs());

            //	Now I can let the timer go.  It won't stop until the form goes away
            timer1.Enabled = true;
        }

        #endregion

        #region Misc Control Events

        private void pictureBox1_Resize(object sender, EventArgs e)
        {
            _bitmap = new Bitmap(pictureBox1.DisplayRectangle.Width, pictureBox1.DisplayRectangle.Height);
            _graphics = Graphics.FromImage(_bitmap);
        }

        #region Keyboard/Mouse

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isMouseDown = true;
                _isMouseJustReleased = false;

                _mouseDownPoint.X = e.X;
                _mouseDownPoint.Y = e.Y;

                _curMousePoint.X = e.X;
                _curMousePoint.Y = e.Y;
            }
        }
        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isMouseDown = false;
                _isMouseJustReleased = true;

                _curMousePoint.X = e.X;
                _curMousePoint.Y = e.Y;
            }
        }
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _curMousePoint.X = e.X;
                _curMousePoint.Y = e.Y;
            }
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

            if (keyData == Keys.A)
            {
                _isAPressed = true;
            }

            if (keyData == Keys.S)
            {
                _isSPressed = true;
            }

            if (keyData == Keys.D)
            {
                _isDPressed = true;
            }

            if (keyData == Keys.W)
            {
                _isWPressed = true;
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

            if (e.KeyData == Keys.A)
            {
                _isAPressed = false;
            }

            if (e.KeyData == Keys.S)
            {
                _isSPressed = false;
            }

            if (e.KeyData == Keys.D)
            {
                _isDPressed = false;
            }

            if (e.KeyData == Keys.W)
            {
                _isWPressed = false;
            }

            base.OnKeyUp(e);
        }

        #endregion

        #region Build Ship

        private void btnResetShip_Click(object sender, EventArgs e)
        {
            //	Make sure the simulation isn't running (allow it to run when they've added 3 masses)
            chkRunning.Enabled = false;
            chkRunning.Checked = false;

            //	Create a blank ship
            _ship = new RigidBody(new MyVector(0, 0, 0), new DoubleVector(0, -1, 0, -1, 0, 0), MINSHIPRADIUS, _boundryLower, _boundryUpper);

            //	Tell the viewer to chase the center of mass
            _centerMassWorld = new MyVector();
            CalculateCenterMassWorld();
            pictureBox1.ChasePoint(_centerMassWorld);

            //	Finally, put myself into a ship building state
            SetState(GlobalState.AddingMass);
        }

        private void btnAddMass_Click(object sender, EventArgs e)
        {
            SetState(GlobalState.AddingMass);
        }

        #endregion
        #region Thruster Mapping

        private void btnResetThrusters_Click(object sender, EventArgs e)
        {
            _thrustersCustom.Clear();
            radThrusterCustom.Checked = true;
        }

        private void pctThrust_Click(object sender, EventArgs e)
        {

            if (sender == pctA)
            {
                _addThrusterTo = ThrusterKey.A;
            }
            else if (sender == pctS)
            {
                _addThrusterTo = ThrusterKey.S;
            }
            else if (sender == pctD)
            {
                _addThrusterTo = ThrusterKey.D;
            }
            else if (sender == pctW)
            {
                _addThrusterTo = ThrusterKey.W;
            }
            else if (sender == pctUp)
            {
                _addThrusterTo = ThrusterKey.Up;
            }
            else if (sender == pctDown)
            {
                _addThrusterTo = ThrusterKey.Down;
            }
            else if (sender == pctLeft)
            {
                _addThrusterTo = ThrusterKey.Left;
            }
            else if (sender == pctRight)
            {
                _addThrusterTo = ThrusterKey.Right;
            }
            else
            {
                throw new ApplicationException("Unknown Sender: " + sender.ToString());
            }

            SetState(GlobalState.AddingThruster);

            radThrusterCustom.Checked = true;

        }

        private void radThruster_CheckedChanged(object sender, EventArgs e)
        {

            if (radThrusterStandard.Checked)
            {
                _thrustMode = ThrustMode.Standard;
            }
            else if (radThrusterTwin.Checked)
            {
                _thrustMode = ThrustMode.Twin;
            }
            else if (radThrusterCustom.Checked)
            {
                _thrustMode = ThrustMode.Custom;
            }
            else
            {
                throw new ApplicationException("None of the thrust modes are selected");
            }

        }

        #endregion
        #region Simulation

        private void chkRunning_CheckedChanged(object sender, EventArgs e)
        {
            if (chkRunning.Checked)
            {
                SetState(GlobalState.Running);
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            _ship.StopBall();
        }

        private void radGravity_CheckedChanged(object sender, EventArgs e)
        {

            if (radGravityNone.Checked)
            {
                _gravityMode = GravityMode.None;
            }
            else if (radGravityDown.Checked)
            {
                _gravityMode = GravityMode.Down;
            }
            else if (radGravityBall.Checked)
            {
                _gravityMode = GravityMode.Ball;

                //	Insure that there are gravity balls
                numericUpDown1_ValueChanged(this, new EventArgs());
            }
            else
            {
                throw new ApplicationException("None of the gravity modes are selected");
            }

        }

        private void btnResetZoom_Click(object sender, EventArgs e)
        {
            pictureBox1.ZoomFit();
            pictureBox1.ChasePoint(_centerMassWorld);
        }

        private void btnChaseShip_Click(object sender, EventArgs e)
        {
            if (_ship != null)
            {
                pictureBox1.ChasePoint(_ship.Position);
            }
        }

        private void btnResetGravityBall_Click(object sender, EventArgs e)
        {
            _gravityBalls.Clear();
            _gravityBallColors.Clear();
            numericUpDown1.Value = 1;
            radGravityBall.Checked = true;
        }
        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {

            radGravityBall.Checked = true;

            int numBalls = Convert.ToInt32(numericUpDown1.Value);

            if (_gravityBalls.Count == numBalls)
            {
                //	Nothing to do
                return;
            }
            else if (_gravityBalls.Count < numBalls)
            {
                //	Make balls
                Random rand = new Random();

                while (_gravityBalls.Count < numBalls)
                {
                    CreateGravityBall(rand);
                }
            }
            else  //if (_gravityBalls.Count > numBalls)
            {
                //	Remove balls
                _gravityBalls.RemoveRange(numBalls, _gravityBalls.Count - numBalls);
                _gravityBallColors.RemoveRange(numBalls, _gravityBalls.Count - numBalls);
            }

        }
        private void btnGravBallRandomSpeed_Click(object sender, EventArgs e)
        {

            const double MAXSPEED = 40;

            Random rand = new Random();
            foreach (Ball gravBall in _gravityBalls)
            {
                MyVector newVelocity = Utility3D.GetRandomVector(MAXSPEED);
                newVelocity.Z = 0;
                gravBall.Velocity.StoreNewValues(newVelocity);
            }
        }

        #endregion

        /// <summary>
        /// This timer never stops running.  Because of that, state is very important
        /// </summary>
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (_ship == null)
            {
                //	I should never have this case, but I'd rather be safe
                return;
            }

            ColorArrowKeys();

            //	Figure out what to do
            switch (_state)
            {
                case GlobalState.AddingMass:
                    _graphics.Clear(pictureBox1.BackColor);

                    ShipBuildingTick();

                    pictureBox1.CreateGraphics().DrawImageUnscaled(_bitmap, 0, 0);
                    break;

                case GlobalState.AddingThruster:
                    _graphics.Clear(pictureBox1.BackColor);

                    ThrusterBuildingTick();

                    pictureBox1.CreateGraphics().DrawImageUnscaled(_bitmap, 0, 0);
                    break;

                case GlobalState.Running:
                    pictureBox1.PrepareForNewDraw();
                    CalculateCenterMassWorld();		//	I want this always up to date, so I do it before and after physics are called

                    //	Draw the ship
                    DrawShipRunning(Color.MediumSlateBlue, Color.White);

                    if (chkRunning.Checked)
                    {
                        SimulationTick();
                    }

                    CalculateCenterMassWorld();
                    pictureBox1.FinishedDrawing();
                    break;

                default:
                    throw new ApplicationException("Unknown GlobalState: " + _state.ToString());
            }

        }

        #endregion

        #region ShipBuilding Tick

        private void ShipBuildingTick()
        {

            MyVector centerPoint = GetMiddlePoint();

            //	If there is a new mass, add it before drawing the ship
            if (_isMouseJustReleased)
            {
                _isMouseJustReleased = false;
                #region Add Mass

                double mass = MyVector.Subtract(new MyVector(_curMousePoint), new MyVector(_mouseDownPoint)).GetMagnitude();

                if (mass > 0)		//	it was erroring out when I clicked without moving the mouse
                {
                    //	They just added a new mass
                    MyVector massPos = new MyVector(_mouseDownPoint) - centerPoint;

                    _ship.AddPointMass(massPos.X, massPos.Y, massPos.Z, mass);

                    if (_ship.PointMassCount > 2)
                    {
                        chkRunning.Enabled = true;
                    }

                    _ship.Radius = GetLargestRadius();
                }

                #endregion
            }

            //	Draw the ship
            DrawShipDesign(Color.MediumSlateBlue, Color.White);
            DrawThrustDesign(Color.DimGray, .5d);

            if (_isMouseDown)
            {
                //	They are in the middle of adding a mass
                DrawDot(new MyVector(_mouseDownPoint), MyVector.Subtract(new MyVector(_curMousePoint), new MyVector(_mouseDownPoint)).GetMagnitude(), Color.Gray);
                DrawVector(new MyVector(_curMousePoint), new MyVector(_mouseDownPoint), Color.Yellow);
            }

            txtNumPointMasses.Text = _ship.PointMassCount.ToString();
            txtTotalMass.Text = Math.Round(_ship.Mass, 1).ToString();

        }

        /// <summary>
        /// This function figures out the radius that surrounds all of the point masses
        /// </summary>
        private double GetLargestRadius()
        {

            double retVal = MINSHIPRADIUS;
            double curRadius;

            foreach (PointMass pointMass in _ship.PointMasses)
            {
                curRadius = pointMass.Position.GetMagnitude() + pointMass.Mass;		//	I assume that the pointmass's mass is the same as its radius

                if (curRadius > retVal)
                {
                    retVal = curRadius;
                }
            }

            return retVal * 1.1d;		//	make it slightly bigger
        }

        #endregion
        #region ThrusterBuilding Tick

        private void ThrusterBuildingTick()
        {

            MyVector centerPoint = GetMiddlePoint();

            DrawShipDesign(Color.DimGray, Color.Silver);

            if (_isMouseJustReleased)
            {
                _isMouseJustReleased = false;

                //	They just created a thruster.  Add it to the list
                MyVector offset = new MyVector(_mouseDownPoint) - centerPoint;
                MyVector force = new MyVector(_curMousePoint) - new MyVector(_mouseDownPoint);

                if (!_thrustersCustom.ContainsKey(_addThrusterTo))
                {
                    _thrustersCustom.Add(_addThrusterTo, new List<MyVector[]>());
                }

                _thrustersCustom[_addThrusterTo].Add(new MyVector[] { offset, force });
            }

            DrawThrustDesign(Color.Black, 1d);

            if (_isMouseDown)
            {
                DrawVector(new MyVector(_mouseDownPoint), new MyVector(_curMousePoint), Color.Yellow);
            }


        }

        private void radThrustDirection_CheckedChanged(object sender, EventArgs e)
        {

            if (radThrustDirectionFlame.Checked)
            {
                _thrustForceMultiplier = -1d;
            }
            else
            {
                _thrustForceMultiplier = 1d;
            }

        }

        #endregion
        #region Simulation Tick

        private void SimulationTick()
        {

            const double ELAPSEDTIME = .5;

            _ship.PrepareForNewTimerCycle();

            #region Keyboard Forces

            if (_isUpPressed)
            {
                FireThruster(ThrusterKey.Up);
            }
            if (_isDownPressed)
            {
                FireThruster(ThrusterKey.Down);
            }
            if (_isLeftPressed)
            {
                FireThruster(ThrusterKey.Left);
            }
            if (_isRightPressed)
            {
                FireThruster(ThrusterKey.Right);
            }

            if (_isAPressed)
            {
                FireThruster(ThrusterKey.A);
            }
            if (_isSPressed)
            {
                FireThruster(ThrusterKey.S);
            }
            if (_isDPressed)
            {
                FireThruster(ThrusterKey.D);
            }
            if (_isWPressed)
            {
                FireThruster(ThrusterKey.W);
            }

            #endregion

            #region Pool Stick

            if (_isMouseDown)
            {
                MyVector mouseDownWorld = pictureBox1.GetPositionViewToWorld(new MyVector(_mouseDownPoint));
                MyVector curMouseWorld = pictureBox1.GetPositionViewToWorld(new MyVector(_curMousePoint));

                pictureBox1.DrawLine(Color.Yellow, 1, mouseDownWorld, curMouseWorld);
                pictureBox1.DrawLine(Color.Olive, 1, mouseDownWorld, _ship.Position + _ship.Rotation.GetRotatedVector(_ship.CenterOfMass, true));
            }

            if (_isMouseJustReleased)
            {
                _isMouseJustReleased = false;

                MyVector mouseDownWorld = pictureBox1.GetPositionViewToWorld(new MyVector(_mouseDownPoint));
                MyVector curMouseWorld = pictureBox1.GetPositionViewToWorld(new MyVector(_curMousePoint));

                _ship.ApplyExternalForce(mouseDownWorld - _ship.Position, mouseDownWorld - curMouseWorld);
            }

            #endregion

            #region Gravity

            switch (_gravityMode)
            {
                case GravityMode.Down:
                    _ship.ExternalForce.Add(new MyVector(0, 100, 0));
                    break;

                case GravityMode.Ball:
                    for (int gravCntr = 0; gravCntr < _gravityBalls.Count; gravCntr++)
                    {
                        RunGravityBall(ELAPSEDTIME, _gravityBalls[gravCntr], _gravityBallColors[gravCntr]);
                    }
                    break;
            }

            #endregion

            _ship.TimerTestPosition(ELAPSEDTIME);
            _ship.TimerFinish();

        }

        private void FireThruster(ThrusterKey key)
        {

            switch (_thrustMode)
            {
                case ThrustMode.Standard:
                    if (_thrustersStandard.ContainsKey(key))
                    {
                        FireThrusterSprtFire(_thrustersStandard[key], key);
                    }
                    break;

                case ThrustMode.Custom:
                    if (_thrustersCustom.ContainsKey(key))
                    {
                        FireThrusterSprtFire(_thrustersCustom[key], key);
                    }
                    break;

                default:
                    throw new ApplicationException("Unknown ThrustMode: " + _thrustMode);
            }

        }
        private void FireThrusterSprtFire(List<MyVector[]> thrusters, ThrusterKey key)
        {

            foreach (MyVector[] thruster in thrusters)
            {
                _ship.ApplyInternalForce(thruster[0], thruster[1] * _thrustForceMultiplier);

                MyVector offsetWorld = _ship.Rotation.GetRotatedVector(thruster[0], true);
                offsetWorld.Add(_ship.Position);

                MyVector forceWorld = _ship.Rotation.GetRotatedVector(thruster[1], true);

                DrawThrustRunning(offsetWorld, forceWorld, _thrusterColors[key]);
            }

        }

        private void RunGravityBall(double elapsedTime, Ball gravityBall, Color color)
        {

            const double GRAVITATIONALCONSTANT = 1000d;

            pictureBox1.FillCircle(color, gravityBall.Position, gravityBall.Radius);

            gravityBall.PrepareForNewTimerCycle();

            MyVector gravityLink = _centerMassWorld - gravityBall.Position;

            double force = GRAVITATIONALCONSTANT * (gravityBall.Mass * _ship.Mass) / gravityLink.GetMagnitudeSquared();

            gravityLink.BecomeUnitVector();
            gravityLink.Multiply(force);

            gravityBall.ExternalForce.Add(gravityLink);

            gravityLink.Multiply(-1d);

            _ship.ExternalForce.Add(gravityLink);

            gravityBall.TimerTestPosition(elapsedTime);
            gravityBall.TimerFinish();

        }

        private void CreateGravityBall(Random rand)
        {

            const int MINMASS = 500;
            const int MAXMASS = 5000;
            const int MINCOLOR = 75;

            //	Figure out ball properties
            int radius = rand.Next(100, 500);
            MyVector position = Utility3D.GetRandomVector(_boundryLower, _boundryUpper);
            DoubleVector dirFacing = new DoubleVector(new MyVector(1, 0, 0), new MyVector(0, 1, 0));
            int mass = rand.Next(MINMASS, MAXMASS);

            double massPercent = Convert.ToDouble(mass - MINMASS) / Convert.ToDouble(MAXMASS - MINMASS);
            int colorValue = MINCOLOR + Convert.ToInt32((255 - MINCOLOR) * massPercent);

            //	Make the ball
            _gravityBalls.Add(new Ball(position, dirFacing, radius, mass, _boundryLower, _boundryUpper));
            _gravityBallColors.Add(Color.FromArgb(colorValue, colorValue, colorValue));

        }

        #endregion

        #region Private Methods

        private void SetState(GlobalState state)
        {

            switch (state)
            {
                case GlobalState.AddingMass:
                    lblCurrentInstruction.Text = "Add Mass";
                    lblCurrentInstruction.Visible = true;
                    chkRunning.Checked = false;
                    break;

                case GlobalState.AddingThruster:
                    lblCurrentInstruction.Text = "Add Thrusters (" + _addThrusterTo.ToString() + ")";
                    lblCurrentInstruction.Visible = true;
                    chkRunning.Checked = false;
                    break;

                case GlobalState.Running:
                    lblCurrentInstruction.Text = "";
                    lblCurrentInstruction.Visible = false;
                    break;

                default:
                    throw new ApplicationException("Unknown GlobalState: " + state.ToString());
            }

            _state = state;

        }

        private MyVector GetMiddlePoint()
        {
            MyVector retVal = new MyVector();

            retVal.X = pictureBox1.DisplayRectangle.Width / 2d;
            retVal.Y = pictureBox1.DisplayRectangle.Height / 2d;
            retVal.Z = 0d;

            return retVal;
        }

        private void DrawVector(MyVector fromPoint, MyVector toPoint, Color color)
        {
            _graphics.DrawLine(new Pen(color), fromPoint.ToPointF(), toPoint.ToPointF());
        }
        private void DrawVector(MyVector fromPoint, MyVector toPoint, Color color, int lineWidth)
        {
            _graphics.DrawLine(new Pen(color, lineWidth), fromPoint.ToPointF(), toPoint.ToPointF());
        }

        private void DrawDot(MyVector centerPoint, double radius, Color color)
        {

            float centerX = Convert.ToSingle(centerPoint.X);
            float centerY = Convert.ToSingle(centerPoint.Y);

            _graphics.FillEllipse(new SolidBrush(color), Convert.ToSingle(centerX - radius), Convert.ToSingle(centerY - radius), Convert.ToSingle(radius) * 2f, Convert.ToSingle(radius) * 2f);

        }
        private void DrawDot(MyVector centerPoint, double radius, Color backColor, Color outlineColor)
        {

            float centerX = Convert.ToSingle(centerPoint.X);
            float centerY = Convert.ToSingle(centerPoint.Y);

            float fromX = Convert.ToSingle(centerX - radius);
            float toX = Convert.ToSingle(centerY - radius);
            float size = Convert.ToSingle(radius) * 2f;

            _graphics.FillEllipse(new SolidBrush(backColor), fromX, toX, size, size);
            _graphics.DrawEllipse(new Pen(outlineColor), fromX, toX, size, size);

        }

        private void DrawShipDesign(Color massColor, Color massOutlineColor)
        {

            MyVector centerPoint = GetMiddlePoint();

            //	Draw Masses
            foreach (PointMass pointMass in _ship.PointMasses)
            {
                DrawDot(centerPoint + pointMass.Position, pointMass.Mass, massColor, massOutlineColor);
            }

            //	Orientation
            DrawVector(centerPoint, centerPoint + (_ship.OriginalDirectionFacing.Standard * 100d), Color.Silver);
            DrawVector(centerPoint, centerPoint + (_ship.OriginalDirectionFacing.Orth * 100d), Color.DimGray);

            //	Line from centerpoint to centermass
            DrawVector(centerPoint, centerPoint + _ship.CenterOfMass, Color.DarkMagenta);

            //	Center Point
            DrawDot(centerPoint, 3, Color.DarkMagenta);

            //	Center Mass
            DrawDot(centerPoint + _ship.CenterOfMass, 3, Color.HotPink);

        }
        private void DrawThrustDesign(Color fromColor, double alpha)
        {

            MyVector centerPoint = GetMiddlePoint();

            //	Draw all the thrusters
            foreach (ThrusterKey thrusterKey in _thrustersCustom.Keys)
            {
                foreach (MyVector[] thruster in _thrustersCustom[thrusterKey])
                {
                    MyVector offset = centerPoint + thruster[0];
                    MyVector toPoint = offset + thruster[1];

					Color finalColor = UtilityGDI.AlphaBlend(_thrusterColors[thrusterKey], fromColor, alpha);

                    DrawDot(offset, 5, finalColor);
                    DrawVector(offset, toPoint, finalColor);
                }
            }

        }

        private void DrawShipRunning(Color massColor, Color massOutlineColor)
        {

            //	Radius
			pictureBox1.DrawCircle(UtilityGDI.AlphaBlend(Color.DarkCyan, Color.Black, .5d), .5d, _ship.Position, _ship.Radius);

            //	Point Masses
            SolidBrush massBrush = new SolidBrush(massColor);

            foreach (PointMass pointMass in _ship.PointMasses)
            {
                MyVector rotatedMass = _ship.Rotation.GetRotatedVector(pointMass.Position, true);

                pictureBox1.FillCircle(massBrush, _ship.Position + rotatedMass, pointMass.Mass);
                pictureBox1.DrawCircle(massOutlineColor, 1, _ship.Position + rotatedMass, pointMass.Mass);
            }

            massBrush.Dispose();

            //	Orientation
            pictureBox1.DrawLine(Color.FromArgb(64, 64, 64), 1, _ship.Position, _ship.Position + (_ship.DirectionFacing.Standard * 100d));
            pictureBox1.DrawLine(Color.FromArgb(32, 32, 32), 1, _ship.Position, _ship.Position + (_ship.DirectionFacing.Orth * 100d));

            MyVector rotatedCenterMass = _ship.Rotation.GetRotatedVector(_ship.CenterOfMass, true);

            //	Line from centerpoint to centermass
			pictureBox1.DrawLine(UtilityGDI.AlphaBlend(Color.DarkMagenta, Color.Black, .4d), 1, _ship.Position, _ship.Position + rotatedCenterMass);

            //	Center Point
            pictureBox1.FillCircle(Color.DarkMagenta, _ship.Position, 2);

            //	Center Mass
            pictureBox1.FillCircle(Color.HotPink, _ship.Position + rotatedCenterMass, 2);

        }
        private void DrawThrustRunning(MyVector offset, MyVector force, Color color)
        {
            pictureBox1.FillCircle(color, offset, 5);
            pictureBox1.DrawLine(color, 1.5d, offset, offset + force);
        }

        private void ColorArrowKeys()
        {

            ColorArrowKeysSprtSinglePictureBox(pctA, _isAPressed, ThrusterKey.A);
            ColorArrowKeysSprtSinglePictureBox(pctS, _isSPressed, ThrusterKey.S);
            ColorArrowKeysSprtSinglePictureBox(pctD, _isDPressed, ThrusterKey.D);
            ColorArrowKeysSprtSinglePictureBox(pctW, _isWPressed, ThrusterKey.W);

            ColorArrowKeysSprtSinglePictureBox(pctUp, _isUpPressed, ThrusterKey.Up);
            ColorArrowKeysSprtSinglePictureBox(pctDown, _isDownPressed, ThrusterKey.Down);
            ColorArrowKeysSprtSinglePictureBox(pctLeft, _isLeftPressed, ThrusterKey.Left);
            ColorArrowKeysSprtSinglePictureBox(pctRight, _isRightPressed, ThrusterKey.Right);

        }
        private void ColorArrowKeysSprtSinglePictureBox(PictureBox pctBox, bool isPressed, ThrusterKey key)
        {

            Color pressedColor;
            double alpha;

            if (isPressed)
            {
                pressedColor = SystemColors.ControlDark;
                alpha = .5d;
            }
            else
            {
                pressedColor = SystemColors.Control;
                alpha = .25d;
            }

            if (_thrustersCustom.ContainsKey(key))
            {
				pctBox.BackColor = UtilityGDI.AlphaBlend(_thrusterColors[key], pressedColor, alpha);
            }
            else
            {
                pctBox.BackColor = SystemColors.Control;
            }

        }

        private void CalculateCenterMassWorld()
        {
            MyVector cmWorld = _ship.Rotation.GetRotatedVector(_ship.CenterOfMass, true);
            cmWorld.Add(_ship.Position);

            _centerMassWorld.StoreNewValues(cmWorld);
        }

        #endregion
    }
}