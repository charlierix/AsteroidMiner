using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using Game.HelperClasses;
using Game.Orig.HelperClassesOrig;
using Game.Orig.HelperClassesGDI;
using Game.Orig.Map;
using Game.Orig.Math3D;

namespace Game.Orig.TestersGDI
{
	public partial class MapTester1 : Form
	{
		#region Enum: GravityMode

		private enum GravityMode
		{
			None = 0,
			Down,
			EachOther
		}

		#endregion

		#region Declaration Section

		private const double MAXVELOCITY = 15d;
		private const double MINRADIUSMASS = 100;
		private const double MAXRADIUSMASS = 500;

		private const double BOUNDRY = 5000;

		Random _rand = new Random();

		private SimpleMap _map = new SimpleMap();

		private double _elapsedTime = 0;

        private MyVector _boundryLower = new MyVector(BOUNDRY * -1, BOUNDRY * -1, 0);
        private MyVector _boundryUpper = new MyVector(BOUNDRY, BOUNDRY, 0);

		private VectorField2D _vectorField = null;

		private GravityMode _gravityMode;
		private double _gravityMultiplier;

		private bool _drawCollisionsRed;

		#region Ship

		private BallBlip _ship = null;

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

		#region Mouse

		private bool _isMouseDown = false;
		private bool _isMouseJustReleased = false;
		private Point _mouseDownPoint;		//	this is the point at the time of mousedown
		private Point _curMousePoint;

		#endregion

		#endregion

		#region Constructor

		public MapTester1()
		{
			InitializeComponent();

			//	Setup the map
			_map.CollisionHandler = new CollisionHandler();
			trkElapsedTime_Scroll(this, new EventArgs());
			trkThreshold_Scroll(this, new EventArgs());

			//	Setup the viewer
            pictureBox1.SetBorder(_boundryLower, _boundryUpper);
            pictureBox1.ShowBorder(Color.GhostWhite, 3);
            trkYBoundry_Scroll(this, new EventArgs());
            pictureBox1.ZoomFit();

			//	Setup the vector field
			_vectorField = new VectorField2D();
			_vectorField.SquaresPerSideX = 10;
			_vectorField.SquaresPerSideY = 10;
			trkVectorFieldForce_Scroll(this, new EventArgs());
			trkVectorFieldSize_Scroll(this, new EventArgs());

			//	Setup the vector field combo
			foreach (string enumName in Enum.GetNames(typeof(VectorField2DMode)))
			{
				cboVectorField.Items.Add(enumName);
			}
			cboVectorField.Text = VectorField2DMode.None.ToString();

			//	Misc
			radGravity_CheckedChanged(this, new EventArgs());
			chkDrawCollisionsRed_CheckedChanged(this, new EventArgs());
			trkElasticity_Scroll(this, new EventArgs());
			radPullApart_CheckedChanged(this, new EventArgs());
			trkPullApartPercent_Scroll(this, new EventArgs());
			trkPullApartSpring_Scroll(this, new EventArgs());
			trkGravityForce_Scroll(this, new EventArgs());

			toolTip1.SetToolTip(chkSmallObjectsAreMassive, "(doesn't affect rigid bodies)");

			//	Run it
			chkRunning.Checked = true;

			trkElapsedTime.BackColor = tabControl1.BackColor;

		}

		#endregion

		#region Misc Control Events

		#region Picturebox Events

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
			//	Remember this position
			_curMousePoint.X = e.X;
			_curMousePoint.Y = e.Y;

			//	Update the vector field
            _vectorField.Position.StoreNewValues(pictureBox1.GetPositionViewToWorld(new MyVector(_curMousePoint)));
		}

		#endregion

		#region Add/Remove

		private void btnAdd_Click(object sender, EventArgs e)
		{
			double radius = _rand.Next(Convert.ToInt32(MINRADIUSMASS), Convert.ToInt32(MAXRADIUSMASS));
			double mass = GetMass(radius);
			double elasticity = GetElasticity();

			Ball ball = new Ball(Utility3D.GetRandomVector(_rand, _boundryLower, _boundryUpper), new DoubleVector(0, 1, 0, 1, 0, 0), radius, mass, elasticity, 1, 1, _boundryLower, _boundryUpper);
			ball.Velocity.Add(Utility3D.GetRandomVector(_rand, MAXVELOCITY));
			ball.Velocity.Z = 0;

			BallBlip blip = new BallBlip(ball, CollisionStyle.Standard, RadarBlipQual.BallUserDefined00, _map.GetNextToken());

			_map.Add(blip);
		}
		private void btnAddSolidBall_Click(object sender, EventArgs e)
		{
			double radius = _rand.Next(Convert.ToInt32(MINRADIUSMASS), Convert.ToInt32(MAXRADIUSMASS));
			double mass = GetMass(radius);
			double elasticity = GetElasticity();

			SolidBall ball = new SolidBall(Utility3D.GetRandomVector(_rand, _boundryLower, _boundryUpper), new DoubleVector(0, 1, 0, 1, 0, 0), radius, mass, elasticity, 1, 1, _boundryLower, _boundryUpper);
			ball.Velocity.Add(Utility3D.GetRandomVector(_rand, MAXVELOCITY));
			ball.Velocity.Z = 0;

			BallBlip blip = new BallBlip(ball, CollisionStyle.Standard, RadarBlipQual.BallUserDefined01, _map.GetNextToken());

			_map.Add(blip);
		}
		private void btnAddRigidBody_Click(object sender, EventArgs e)
		{
			const int MINPOINTMASSES = 3;
			const int MAXPOINTMASSES = 8;
			const double MINPOINTMASSRADIUS = MINRADIUSMASS / MINPOINTMASSES;
			const double MAXPOINTMASSRADIUS = MAXRADIUSMASS / MAXPOINTMASSES;

			//	Make the chassis
			RigidBody ball = new RigidBody(Utility3D.GetRandomVector(_rand, _boundryLower, _boundryUpper), new DoubleVector(0, -1, 0, -1, 0, 0), .1d, GetElasticity(), 1, 1, _boundryLower, _boundryUpper);

			int numPointMasses = _rand.Next(MINPOINTMASSES, MAXPOINTMASSES + 1);
			//double maxOffset = MAXRADIUSMASS - ((MINPOINTMASSRADIUS + MAXPOINTMASSRADIUS) / 2d);		//	this could result in bodies slightly larger than the other two types, but it should be close
			double maxOffset = MAXRADIUSMASS - MAXPOINTMASSRADIUS;
			double ballRadius = ball.Radius;
			double curRadius;

			//	Add point masses
			for (int massCntr = 1; massCntr <= numPointMasses; massCntr++)
			{
                MyVector pointMassPos = Utility3D.GetRandomVectorSpherical(_rand, maxOffset);
				pointMassPos.Z = 0;		//	I do this here for the radius calculation below
				double pointMassMass = MINPOINTMASSRADIUS + (_rand.NextDouble() * (MAXPOINTMASSRADIUS - MINPOINTMASSRADIUS));

				//	Add a point mass
				ball.AddPointMass(pointMassPos.X, pointMassPos.Y, 0, pointMassMass);

				//	See if this pushes out the ball's overall radius
				curRadius = pointMassPos.GetMagnitude() + pointMassMass;		//	I assume that the pointmass's mass is the same as its radius
				if (curRadius > ballRadius)
				{
					ballRadius = curRadius;
				}
			}

			//	Store the new radius
			ball.Radius = ballRadius * 1.1d;		//	make it slightly bigger

			//	Set the velocity
			ball.Velocity.Add(Utility3D.GetRandomVector(_rand, MAXVELOCITY));
			ball.Velocity.Z = 0;

			BallBlip blip = new BallBlip(ball, CollisionStyle.Standard, RadarBlipQual.BallUserDefined02, _map.GetNextToken());

			_map.Add(blip);
		}

		private void chkIncludeShip_CheckedChanged(object sender, EventArgs e)
		{
			const double THRUSTERANGLE = 75;

			if (chkIncludeShip.Checked)
			{
				if (_ship == null)
				{
					#region Create Ship

					//	Set up the ship
					double radius = MINRADIUSMASS + (_rand.NextDouble() * (MAXRADIUSMASS - MINRADIUSMASS));
					SolidBall ship = new SolidBall(Utility3D.GetRandomVector(_rand, _boundryLower, _boundryUpper), new DoubleVector(0, 1, 0, 1, 0, 0), radius, GetMass(radius), GetElasticity(), 1, 1, _boundryLower, _boundryUpper);

					//	Set up the thrusters
                    MyVector thrusterSeed = new MyVector(0, ship.Radius, 0);
                    MyVector zAxis = new MyVector(0, 0, 1);

					//	Bottom Thrusters
					_shipThrusterOffset_BottomRight = thrusterSeed.Clone();
					_shipThrusterOffset_BottomRight.RotateAroundAxis(zAxis, Utility3D.GetDegreesToRadians(THRUSTERANGLE * -1));

					_shipThrusterOffset_BottomLeft = thrusterSeed.Clone();
					_shipThrusterOffset_BottomLeft.RotateAroundAxis(zAxis, Utility3D.GetDegreesToRadians(THRUSTERANGLE));

					//	Top Thrusters
                    thrusterSeed = new MyVector(0, ship.Radius * -1, 0);
					_shipThrusterOffset_TopRight = thrusterSeed.Clone();
					_shipThrusterOffset_TopRight.RotateAroundAxis(zAxis, Utility3D.GetDegreesToRadians(THRUSTERANGLE));

					_shipThrusterOffset_TopLeft = thrusterSeed.Clone();
					_shipThrusterOffset_TopLeft.RotateAroundAxis(zAxis, Utility3D.GetDegreesToRadians(THRUSTERANGLE * -1));

					//	Add to the map
					_ship = new BallBlip(ship, CollisionStyle.Standard, RadarBlipQual.BallUserDefined03, _map.GetNextToken());
					_map.Add(_ship);

					#endregion
				}
			}
			else
			{
				if (_ship != null)
				{
					_map.Remove(_ship.Token);
					_ship = null;
				}
			}
		}

		private void btnRemove_Click(object sender, EventArgs e)
		{
			if (_map.Count == 0)
			{
				MessageBox.Show("No blips to remove", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			RadarBlip[] blips = _map.GetAllBlips();

			if (_ship != null && blips.Length == 1)
			{
				//	The only one left is the ship
				return;
			}

			do
			{
				//	Pick a random ball
				int removeIndex = _rand.Next(blips.Length);

				//	Make sure it's not the ship
				if (_ship != null && blips[removeIndex].Token == _ship.Token)
				{
					continue;
				}

				//	This one isn't the ship.  Remove it
				_map.Remove(blips[removeIndex].Token);
				return;
			} while (true);
		}
		private void btnClear_Click(object sender, EventArgs e)
		{
			_map.Clear();

			if (_ship != null)
			{
				_map.Add(_ship);
			}
		}

		#endregion

		#region Vector Field

		private void cboVectorField_SelectedIndexChanged(object sender, EventArgs e)
		{
			try
			{
				_vectorField.FieldMode = (VectorField2DMode)Enum.Parse(typeof(VectorField2DMode), cboVectorField.Text);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Unsupported field type\n\n" + ex.Message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				cboVectorField.Text = VectorField2DMode.None.ToString();
				cboVectorField_SelectedIndexChanged(this, new EventArgs());
			}
		}

		private void trkVectorFieldForce_Scroll(object sender, EventArgs e)
		{
			const double MINFORCE = 1;
			const double MAXFORCE = 750;

			_vectorField.Strength = UtilityHelper.GetScaledValue(MINFORCE, MAXFORCE, trkVectorFieldForce.Minimum, trkVectorFieldForce.Maximum, trkVectorFieldForce.Value);
		}

		private void trkVectorFieldSize_Scroll(object sender, EventArgs e)
		{
			const double MINSIZE = 10;
			const double MAXSIZE = 10000;

			double size = UtilityHelper.GetScaledValue(MINSIZE, MAXSIZE, trkVectorFieldSize.Minimum, trkVectorFieldSize.Maximum, trkVectorFieldSize.Value);

			_vectorField.SizeX = size;
			_vectorField.SizeY = size;
		}

		#endregion

		#region Misc

		private void btnZeroVelocity_Click(object sender, EventArgs e)
		{
			foreach (BallBlip blip in _map.GetAllBlips())
			{
				blip.Ball.StopBall();
			}
		}
		private void btnRandomVelocity_Click(object sender, EventArgs e)
		{
			foreach (BallBlip blip in _map.GetAllBlips())
			{
				blip.Ball.Velocity.StoreNewValues(Utility3D.GetRandomVector(_rand, MAXVELOCITY));
				blip.Ball.Velocity.Z = 0;
			}
		}

		private void btnZoomFit_Click(object sender, EventArgs e)
		{
			pictureBox1.ZoomFit();
		}
		private void btnChaseShip_Click(object sender, EventArgs e)
		{
			if (_ship == null)
			{
				MessageBox.Show("Ship isn't selected", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			pictureBox1.ChasePoint(_ship.Sphere.Position);
		}

		private void chkRunning_CheckedChanged(object sender, EventArgs e)
		{
			timer1.Enabled = chkRunning.Checked;
		}

		private void trkElapsedTime_Scroll(object sender, EventArgs e)
		{
			const double MINTIME = .01d;
			const double MAXTIME = 10d;

			_elapsedTime = UtilityHelper.GetScaledValue(MINTIME, MAXTIME, trkElapsedTime.Minimum, trkElapsedTime.Maximum, trkElapsedTime.Value);

			txtElapsedTime.Text = Math.Round(_elapsedTime, 2).ToString();
		}

		private void trkThreshold_Scroll(object sender, EventArgs e)
		{
			const double MINPERCENT = .001d;
			const double MAXPERCENT = .999d;

			_map.CollisionHandler.PenetrationThresholdPercent = UtilityHelper.GetScaledValue(MINPERCENT, MAXPERCENT, trkThreshold.Minimum, trkThreshold.Maximum, trkThreshold.Value);

			txtThreshold.Text = Math.Round(_map.CollisionHandler.PenetrationThresholdPercent * 100d, 1).ToString() + "%";
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
			else if (radGravityBalls.Checked)
			{
				_gravityMode = GravityMode.EachOther;
			}
			else
			{
				MessageBox.Show("Unknown Gravity Radio Button", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
		private void trkGravityForce_Scroll(object sender, EventArgs e)
		{
			const double MINGRAVITY = 0d;
			const double MAXGRAVITY = 50d;

			_gravityMultiplier = UtilityHelper.GetScaledValue(MINGRAVITY, MAXGRAVITY, trkGravityForce.Minimum, trkGravityForce.Maximum, trkGravityForce.Value);
		}

		private void chkDrawCollisionsRed_CheckedChanged(object sender, EventArgs e)
		{
			_drawCollisionsRed = chkDrawCollisionsRed.Checked;
		}

		private void trkYBoundry_Scroll(object sender, EventArgs e)
		{
			_boundryLower.Y = trkYBoundry.Value * trkBoundryScale.Value * -1;
			_boundryUpper.Y = trkYBoundry.Value * trkBoundryScale.Value;

			int numSquares = trkYBoundry.Value / 500;		//	integer division

			if (numSquares > 0)
			{
				pictureBox1.ShowCheckerBackground(UtilityGDI.AlphaBlend(Color.DarkGray, pictureBox1.BackColor, .15d), numSquares);
			}
			else
			{
                pictureBox1.HideBackground();
			}
		}
		private void trkBoundryScale_Scroll(object sender, EventArgs e)
		{
			double scale = trkBoundryScale.Value;		//	I want this as a double

			_boundryLower.X = BOUNDRY * scale * -1d;
			_boundryLower.Y = trkYBoundry.Value * scale * -1;

			_boundryUpper.X = BOUNDRY * scale;
			_boundryUpper.Y = trkYBoundry.Value * scale;
		}

		private void trkElasticity_Scroll(object sender, EventArgs e)
		{
			double elasticity = GetElasticity();
			txtElasticity.Text = elasticity.ToString();

			foreach (BallBlip blip in _map.GetAllBlips())
			{
				blip.Ball.Elasticity = elasticity;
			}
		}

		private void radPullApart_CheckedChanged(object sender, EventArgs e)
		{
			if (radPullApartNone.Checked)
			{
				trkPullApartPercent.Enabled = false;
				txtPullApartPercent.Visible = false;
				trkPullApartSpring.Enabled = false;
				txtPullApartSpring.Visible = false;

				_map.TimerPullApartType = PullApartType.None;
			}
			else if (radPullApartInstant.Checked)
			{
				trkPullApartPercent.Enabled = true;
				txtPullApartPercent.Visible = true;
				trkPullApartSpring.Enabled = false;
				txtPullApartSpring.Visible = false;

				_map.TimerPullApartType = PullApartType.Instant;
			}
			else if (radPullApartSpring.Checked)
			{
				trkPullApartPercent.Enabled = false;
				txtPullApartPercent.Visible = false;
				trkPullApartSpring.Enabled = true;
				txtPullApartSpring.Visible = true;

				_map.TimerPullApartType = PullApartType.Force;
			}
			else
			{
				MessageBox.Show("No PullApart radio button is pushed", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
		private void trkPullApartPercent_Scroll(object sender, EventArgs e)
		{
			const double MINPERCENT = 1d;
			const double MAXPERCENT = 1.25d;

			try
			{
				_map.CollisionHandler.PullApartInstantPercent = UtilityHelper.GetScaledValue(MINPERCENT, MAXPERCENT, trkPullApartPercent.Minimum, trkPullApartPercent.Maximum, trkPullApartPercent.Value);

				txtPullApartPercent.Text = Math.Round(_map.CollisionHandler.PullApartInstantPercent * 100d, 1).ToString() + "%";
			}
			catch (Exception ex)
			{
				//	I added caps in the property set
				MessageBox.Show("Had trouble setting property\n\n" + ex.Message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
		}
		private void trkPullApartSpring_Scroll(object sender, EventArgs e)
		{
			const double MINVELOCITY = .01d;
			const double MAXVELOCITY = 30d;

			_map.CollisionHandler.PullApartSpringVelocity = UtilityHelper.GetScaledValue(MINVELOCITY, MAXVELOCITY, trkPullApartSpring.Minimum, trkPullApartSpring.Maximum, trkPullApartSpring.Value);

			txtPullApartSpring.Text = _map.CollisionHandler.PullApartSpringVelocity.ToString();
		}

		private void chkDoStandardCollisions_CheckedChanged(object sender, EventArgs e)
		{
			_map.DoStandardCollisions = chkDoStandardCollisions.Checked;
		}

		private void chkSmallObjectsAreMassive_CheckedChanged(object sender, EventArgs e)
		{
			foreach (BallBlip blip in _map.GetAllBlips())
			{
				if (!(blip.Ball is RigidBody))
				{
					blip.Ball.Mass = GetMass(blip.Ball.Radius);
				}
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

		#endregion

		private void timer1_Tick(object sender, EventArgs e)
		{
			const double THRUSTERFORCE = 80;

            List<MyVector[]> thrustLines = new List<MyVector[]>();

			#region Physics

			_map.PrepareForNewTimerCycle();

			switch (_gravityMode)
			{
				case GravityMode.None:
					break;

				case GravityMode.Down:
					DoGravityDown();
					break;

				case GravityMode.EachOther:
					DoGravityEachOther();
					break;
			}

			if (_ship != null)
			{
				#region Ship Thrusters

				if (_isUpPressed)
				{
                    timer1_TickSprtApplyThrust(thrustLines, _shipThrusterOffset_TopRight, new MyVector(0, 1, 0), THRUSTERFORCE);		//	down
                    timer1_TickSprtApplyThrust(thrustLines, _shipThrusterOffset_TopLeft, new MyVector(0, 1, 0), THRUSTERFORCE);		//	s
				}

				if (_isWPressed)
				{
                    timer1_TickSprtApplyThrust(thrustLines, _shipThrusterOffset_TopRight, new MyVector(0, 1, 0), THRUSTERFORCE * 10d);		//	down
                    timer1_TickSprtApplyThrust(thrustLines, _shipThrusterOffset_TopLeft, new MyVector(0, 1, 0), THRUSTERFORCE * 10d);		//	s
				}

				if (_isDownPressed)
				{
                    timer1_TickSprtApplyThrust(thrustLines, _shipThrusterOffset_BottomRight, new MyVector(0, -1, 0), THRUSTERFORCE);		//	up
                    timer1_TickSprtApplyThrust(thrustLines, _shipThrusterOffset_BottomLeft, new MyVector(0, -1, 0), THRUSTERFORCE);		//	w
				}

				if (_isSPressed)
				{
                    timer1_TickSprtApplyThrust(thrustLines, _shipThrusterOffset_BottomRight, new MyVector(0, -1, 0), THRUSTERFORCE * 10d);		//	up
                    timer1_TickSprtApplyThrust(thrustLines, _shipThrusterOffset_BottomLeft, new MyVector(0, -1, 0), THRUSTERFORCE * 10d);		//	w
				}

				if (_isLeftPressed)
				{
                    timer1_TickSprtApplyThrust(thrustLines, _shipThrusterOffset_BottomRight, new MyVector(0, -1, 0), THRUSTERFORCE);		//	up
                    timer1_TickSprtApplyThrust(thrustLines, _shipThrusterOffset_TopLeft, new MyVector(0, 1, 0), THRUSTERFORCE);		//	s
				}

				if (_isRightPressed)
				{
                    timer1_TickSprtApplyThrust(thrustLines, _shipThrusterOffset_TopRight, new MyVector(0, 1, 0), THRUSTERFORCE);		//	down
                    timer1_TickSprtApplyThrust(thrustLines, _shipThrusterOffset_BottomLeft, new MyVector(0, -1, 0), THRUSTERFORCE);		//	w
				}

				#endregion
			}

			DoVectorField();

			Collision[] collisions = _map.Timer(_elapsedTime);

			#endregion

			#region Draw

			//	Turn the collision list into a list of tokens
			List<long> collisionTokens = GetCollisionTokens(collisions);

            pictureBox1.PrepareForNewDraw();

            #region Vector Field

            if (_vectorField.FieldMode != VectorField2DMode.None)
			{
                List<MyVector[]> drawLines, fieldLines;
				_vectorField.GetDrawLines(out drawLines, out fieldLines);

                foreach (MyVector[] drawLine in drawLines)
				{
                    pictureBox1.DrawLine(Color.DimGray, 1, drawLine[0], drawLine[1]);
				}

                foreach (MyVector[] fieldLine in fieldLines)
				{
                    pictureBox1.DrawArrow(Color.Chartreuse, 1, fieldLine[0], fieldLine[0] + fieldLine[1]);
				}
			}

			#endregion

			RadarBlip[] blips = _map.GetAllBlips();

			Brush brushBall = new SolidBrush(Color.FromArgb(160, Color.Gold));
			Brush brushTorqueBall = new SolidBrush(Color.FromArgb(160, Color.RoyalBlue));
			Brush brushRed = new SolidBrush(Color.FromArgb(160, Color.Tomato));

			Brush curBrush;

			for (int blipCntr = 0; blipCntr < blips.Length; blipCntr++)
			{
				if (blips[blipCntr].Sphere is RigidBody)
				{
					#region Draw Rigid Body

					DrawRigidBody(blips[blipCntr].Sphere as RigidBody, Color.MediumSlateBlue, Color.White, _drawCollisionsRed && collisionTokens.Contains(blips[blipCntr].Token));

					#endregion
				}
				else if (_ship != null && blips[blipCntr].Token == _ship.Token)
				{
					DrawShip(thrustLines);
				}
				else
				{
					#region Draw Ball

					bool isSolidBall = blips[blipCntr].Sphere is SolidBall;

					//	Fill the circle
					if (_drawCollisionsRed && collisionTokens.Contains(blips[blipCntr].Token))
					{
						curBrush = brushRed;
					}
					else if (isSolidBall)
					{
						curBrush = brushTorqueBall;
					}
					else
					{
						curBrush = brushBall;
					}

                    pictureBox1.FillCircle(curBrush, blips[blipCntr].Sphere.Position, blips[blipCntr].Sphere.Radius);

					//	Draw direction facing
					if (isSolidBall)
					{
                        MyVector dirFacing = blips[blipCntr].Sphere.DirectionFacing.Standard.Clone();
						dirFacing.BecomeUnitVector();
						dirFacing.Multiply(blips[blipCntr].Sphere.Radius);
						dirFacing.Add(blips[blipCntr].Sphere.Position);

                        pictureBox1.DrawLine(Color.White, 2, blips[blipCntr].Sphere.Position, dirFacing);
					}

					//	Draw an edge
                    pictureBox1.DrawCircle(Color.Black, 1d, blips[blipCntr].Sphere.Position, blips[blipCntr].Sphere.Radius);

					#endregion
				}
			}

			brushBall.Dispose();
			brushTorqueBall.Dispose();
			brushRed.Dispose();

            pictureBox1.FinishedDrawing();

			#endregion
		}

		#endregion

		#region Private Methods

		private void DoGravityDown()
		{
            MyVector gravity = new MyVector(0, 100d * _gravityMultiplier, 0);

			RadarBlip[] blips = _map.GetAllBlips();
			BallBlip ballBlip;

			for (int blipCntr = 0; blipCntr < blips.Length; blipCntr++)
			{
				ballBlip = blips[blipCntr] as BallBlip;
				ballBlip.Ball.ExternalForce.Add(gravity);
			}
		}
		private void DoGravityEachOther()
		{
			const double GRAVITATIONALCONSTANT = 500d;
			double gravitationalConstantAdjusted = GRAVITATIONALCONSTANT * _gravityMultiplier;

			RadarBlip[] blips = _map.GetAllBlips();

			for (int outerCntr = 0; outerCntr < blips.Length - 1; outerCntr++)
			{
				Ball ball1 = blips[outerCntr].Sphere as Ball;

				for (int innerCntr = outerCntr + 1; innerCntr < blips.Length; innerCntr++)
				{
					#region Apply Gravity

					Ball ball2 = blips[innerCntr].Sphere as Ball;

                    MyVector centerMass1, centerMass2;
					if(ball1 is RigidBody)
					{
						centerMass1 = ball1.Position + ((RigidBody)ball1).CenterOfMass;
					}
					else
					{
						centerMass1 = ball1.Position;
					}

					if(ball2 is RigidBody)
					{
						centerMass2 = ball2.Position + ((RigidBody)ball2).CenterOfMass;
					}
					else
					{
						centerMass2 = ball2.Position;
					}

                    MyVector gravityLink = centerMass1 - centerMass2;

					double force = gravitationalConstantAdjusted * (ball1.Mass * ball2.Mass) / gravityLink.GetMagnitudeSquared();

					gravityLink.BecomeUnitVector();
					gravityLink.Multiply(force);

					ball2.ExternalForce.Add(gravityLink);

					gravityLink.Multiply(-1d);

					ball1.ExternalForce.Add(gravityLink);

					#endregion
				}
			}
		}
		private void DoVectorField()
		{
			RadarBlip[] blips = _map.GetAllBlips();
			BallBlip ballBlip;

			for (int blipCntr = 0; blipCntr < blips.Length; blipCntr++)
			{
				ballBlip = blips[blipCntr] as BallBlip;
				ballBlip.Ball.ExternalForce.Add(_vectorField.GetFieldStrengthWorld(ballBlip.Ball.Position));
			}
		}

		private List<long> GetCollisionTokens(Collision[] collisions)
		{
			List<long> retVal = new List<long>();

			if (collisions != null)
			{
				foreach (Collision collision in collisions)
				{
					if (!retVal.Contains(collision.Blip1.Token))
					{
						retVal.Add(collision.Blip1.Token);
					}

					if (!retVal.Contains(collision.Blip2.Token))
					{
						retVal.Add(collision.Blip2.Token);
					}
				}
			}

			return retVal;
		}

		private double GetMass(double radius)
		{
			double retVal = radius;

			if (chkSmallObjectsAreMassive.Checked)
			{
				double percent = (radius - MINRADIUSMASS) / (MAXRADIUSMASS - MINRADIUSMASS);
				percent = 1d - percent;

				retVal = MINRADIUSMASS + (percent * (MAXRADIUSMASS - MINRADIUSMASS));
			}

			return retVal;
		}

		private double GetElasticity()
		{
			return trkElasticity.Value / 100d;
		}

		private void DrawRigidBody(RigidBody body, Color massColor, Color massOutlineColor, bool drawRed)
		{
			//	Radius
			if (drawRed)
			{
                pictureBox1.FillCircle(Color.FromArgb(160, Color.Tomato), body.Position, body.Radius);
			}
			else
			{
                pictureBox1.FillCircle(Color.FromArgb(32, Color.DarkTurquoise), body.Position, body.Radius);
			}
			pictureBox1.DrawCircle(UtilityGDI.AlphaBlend(Color.DarkTurquoise, Color.Black, .5d), .5d, body.Position, body.Radius);

			//	Point Masses
			SolidBrush massBrush = new SolidBrush(massColor);

			foreach (PointMass pointMass in body.PointMasses)
			{
                MyVector rotatedMass = body.Rotation.GetRotatedVector(pointMass.Position, true);

                pictureBox1.FillCircle(massBrush, body.Position + rotatedMass, pointMass.Mass);
                pictureBox1.DrawCircle(massOutlineColor, 1, body.Position + rotatedMass, pointMass.Mass);
			}

			massBrush.Dispose();

			//	Orientation
            //_viewer.DrawLine(Color.FromArgb(64, 64, 64), 1, body.Position, MyVector.AddVector(body.Position, MyVector.MultiplyConstant(body.DirectionFacing.Stanard, 100)));
            //_viewer.DrawLine(Color.FromArgb(32, 32, 32), 1, body.Position, MyVector.AddVector(body.Position, MyVector.MultiplyConstant(body.DirectionFacing.Orth, 100)));

            MyVector rotatedCenterMass = body.Rotation.GetRotatedVector(body.CenterOfMass, true);

			//	Line from centerpoint to centermass
            pictureBox1.DrawLine(Color.DarkMagenta, 1, body.Position, body.Position + rotatedCenterMass);

			//	Center Point
            pictureBox1.FillCircle(Color.DarkMagenta, body.Position, 10);

			//	Center Mass
            pictureBox1.FillCircle(Color.HotPink, body.Position + rotatedCenterMass, 22);
		}

        private void DrawShip(List<MyVector[]> thrustLines)
		{
			//	Fill Circle
			Brush brushShip = new SolidBrush(Color.FromArgb(100, Color.LimeGreen));

            pictureBox1.FillCircle(brushShip, _ship.Sphere.Position, _ship.Sphere.Radius);

			brushShip.Dispose();

			//	Draw direction facing
            MyVector dirFacing = _ship.Sphere.DirectionFacing.Standard.Clone();
			dirFacing.BecomeUnitVector();
			dirFacing.Multiply(_ship.Sphere.Radius);
			dirFacing.Add(_ship.Sphere.Position);

            pictureBox1.DrawLine(Color.White, 4, _ship.Sphere.Position, dirFacing);

			//	Draw an edge
            pictureBox1.DrawCircle(Color.Black, 25d, _ship.Sphere.Position, _ship.Sphere.Radius);

			//	Thrust Lines
            foreach (MyVector[] thrustPair in thrustLines)
			{
                MyVector thrustStart = _ship.TorqueBall.Rotation.GetRotatedVector(thrustPair[0], true);
				thrustStart.Add(_ship.TorqueBall.Position);

                MyVector thrustStop = thrustPair[1] * -250d;
				thrustStop.Add(thrustPair[0]);
				thrustStop = _ship.TorqueBall.Rotation.GetRotatedVector(thrustStop, true);
				thrustStop.Add(_ship.TorqueBall.Position);

                pictureBox1.DrawLine(Color.Coral, 40d, thrustStart, thrustStop);
			}

			//	Thrusters
			DrawThruster(_shipThrusterOffset_BottomRight);
			DrawThruster(_shipThrusterOffset_BottomLeft);
			DrawThruster(_shipThrusterOffset_TopRight);
			DrawThruster(_shipThrusterOffset_TopLeft);
		}
        private void DrawThruster(MyVector thruster)
		{
            MyVector worldThruster = _ship.TorqueBall.Rotation.GetRotatedVector(thruster, true);
			worldThruster.Add(_ship.TorqueBall.Position);

            pictureBox1.FillCircle(Color.Silver, worldThruster, 60d);
            pictureBox1.DrawCircle(Color.Black, 1d, worldThruster, 60d);
		}

        private void timer1_TickSprtApplyThrust(List<MyVector[]> thrustLines, MyVector offset, MyVector force, double forceMultiplier)
		{
            thrustLines.Add(new MyVector[] { offset, force });

			_ship.TorqueBall.ApplyInternalForce(offset, force * forceMultiplier);
		}

		#endregion
	}
}