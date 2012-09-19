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
using Game.Orig.HelperClassesGDI.Controls;
using Game.Orig.Map;
using Game.Orig.Math3D;

namespace Game.Orig.TestersGDI.PhysicsPainter
{
	public partial class PhysicsPainterMainForm : Form
	{
		#region Declaration Section

		public const double BOUNDRY = 5000;
		private const double ELAPSEDTIME = 1;
		private const double PENETRATIONTHRESHOLDPERCENT = .02d;

		private Random _rand = new Random();

		private SimpleMap _map = new SimpleMap();

        private MyVector _boundryLower = new MyVector(BOUNDRY * -1, BOUNDRY * -1, 0);
        private MyVector _boundryUpper = new MyVector(BOUNDRY, BOUNDRY, 0);

		private ObjectRenderer _renderer = null;

		private Selector _selector = null;
		private BallAdder _ballAdder = null;
		private GravMouse _gravMouse = null;
		private ShipController _shipController = null;
        private GravityController _gravController = null;

        // This keeps track of tokens that should be given special attention (_gravMouse and _shipController)
        private List<long> _ignoreTokens = null;

		private ToolStripButton[] _checkButtons = null;
		private ToolStripButton _beforePanButton = null;
		private PiePanel[] _piePanels = null;

		private bool _isAltDown = false;

		//	These will be removed after they collide
		private List<long> _tempObjects = new List<long>();

        private int _zeroResetCntr = 0;

		#endregion

		#region Constructor

        public PhysicsPainterMainForm()
		{
			InitializeComponent();

			//	Setup the map
			_map.CollisionHandler = new CollisionHandler();
			_map.CollisionHandler.PenetrationThresholdPercent = PENETRATIONTHRESHOLDPERCENT;
			_map.TimerPullApartType = PullApartType.Force;
			_map.CollisionHandler.PullApartSpringVelocity = 15d;

			//	Setup the viewer
            pictureBox1.SetBorder(_boundryLower, _boundryUpper);
            pictureBox1.ShowBorder(Color.GhostWhite, 3);
			pictureBox1.ShowCheckerBackground(UtilityGDI.AlphaBlend(Color.DarkGray, pictureBox1.BackColor, .15d), 10);
            pictureBox1.ZoomFit();
			//pictureBox1.KeyDown += new KeyEventHandler(PhysicsPainter_KeyDown);
			//pictureBox1.KeyUp += new KeyEventHandler(PhysicsPainter_KeyUp);

			//	Setup the renderer
			_renderer = new ObjectRenderer(pictureBox1);
			btnShowStats_Click(this, new EventArgs());

            _ignoreTokens = new List<long>();

			//	Setup the adders/selecter
            _selector = new Selector(pictureBox1, _map, _renderer, _ignoreTokens);
			_ballAdder = new BallAdder(pictureBox1, _renderer, ballProps1.ExposedProps, _map, _boundryLower, _boundryUpper, _tempObjects);
			_gravMouse = new GravMouse(pictureBox1, _map, _boundryLower, _boundryUpper);
			_shipController = new ShipController(pictureBox1, _map, _boundryLower, _boundryUpper);
            _gravController = new GravityController(_map);

            _ignoreTokens.Add(_gravMouse.CursorToken);
            _ignoreTokens.Add(_shipController.ShipToken);

			//	Setup Panels
			scenes1.SetPointers(_map, _renderer, _ignoreTokens);
			shipProps1.SetPointers(pictureBox1, _shipController, _map);
            gravityProps1.SetPointers(_gravController);
			mapShape1.SetPointers(_boundryLower, _boundryUpper);
			generalProps1.SetPointers();

			//	Put all the checkable toolbuttons into an array
			_checkButtons = new ToolStripButton[16];
			_checkButtons[0] = btnArrow;
			_checkButtons[1] = btnPan;
			_checkButtons[2] = btnGravityArrow;
			_checkButtons[3] = btnVectorFieldArrow;
			_checkButtons[4] = btnNewBall;
			_checkButtons[5] = btnNewSolidBall;
			_checkButtons[6] = btnNewRigidBody;
			_checkButtons[7] = btnNewPolygon;
			_checkButtons[8] = btnNewVectorField;
			_checkButtons[9] = btnGravityProps;
			_checkButtons[10] = btnCollisionProps;
			_checkButtons[11] = btnGeneralProps;
			_checkButtons[12] = btnMapShape;
			_checkButtons[13] = btnScenes;
			_checkButtons[14] = btnShip;
            _checkButtons[15] = btnMenuTester;

			//	Put all the pie panels into an array
			_piePanels = new PiePanel[8];
			_piePanels[0] = ballProps1;
			_piePanels[1] = solidBallProps1;
			_piePanels[2] = scenes1;
			_piePanels[3] = shipProps1;
            _piePanels[4] = pieMenuTester1;
            _piePanels[5] = gravityProps1;
			_piePanels[6] = mapShape1;
			_piePanels[7] = generalProps1;

			//	Position all the pie controls (their anchor is already set)
			foreach (PiePanel piePanel in _piePanels)
			{
				piePanel.Anchor = ((AnchorStyles)(AnchorStyles.Bottom | AnchorStyles.Left));

				piePanel.Top = this.DisplayRectangle.Height - piePanel.Height;
				piePanel.Left = 0;
				piePanel.SetDefaultBackColor();
			}

			//	Start with the ball (I've watched several people just stare at this form when they see if for the first time, and
			//	they don't know what to do.  Hopefully with the ball brush selected, they'll catch on faster)
			btnNewBall.Checked = true;
			btnNewBall_Click(this, new EventArgs());

			//	Get the world started
			//TODO:  Don't use the timer to do drawing, only physics
			timer1.Enabled = true;		//	this always stays enabled, because I need to keep drawing the world.
		}

		#endregion

		#region Misc Control Events

		#region Tool Buttons

		//	Standalone Checkboxes
		private void btnRunning_Click(object sender, EventArgs e)
		{
			if (btnRunning.Checked)
			{
				btnRunning.Image = global::Game.Orig.TestersGDI.Properties.Resources.Pause;
				btnRunning.ToolTipText = "Pause Simulation (P)";
			}
			else
			{
				btnRunning.Image = global::Game.Orig.TestersGDI.Properties.Resources.Start;
				btnRunning.ToolTipText = "Run Simulation (P)";
			}
		}
		private void btnShowStats_Click(object sender, EventArgs e)
		{
			if (btnShowStats.Checked)
			{
				btnShowStats.ToolTipText = "Hide Stats";
			}
			else
			{
				btnShowStats.ToolTipText = "Show Stats";
			}

			_renderer.DrawVelocities = btnShowStats.Checked;
		}

		//	Single Selection RadioButtons
		private void btnArrow_Click(object sender, EventArgs e)
		{
			SelectToolButtons(btnArrow);
			ShowHidePies(null);
			DeactivateSelectorAdders();

			_selector.Active = true;
		}
		private void btnPan_Click(object sender, EventArgs e)
		{
			SelectToolButtons(btnPan);
			ShowHidePies(null);
			DeactivateSelectorAdders();
		}
		private void btnGravityArrow_Click(object sender, EventArgs e)
		{
			SelectToolButtons(btnGravityArrow);
			ShowHidePies(null);
			DeactivateSelectorAdders();

			_gravMouse.Active = true;
		}
		private void btnVectorFieldArrow_Click(object sender, EventArgs e)
		{
			SelectToolButtons(btnVectorFieldArrow);
			ShowHidePies(null);
			DeactivateSelectorAdders();
		}
		private void btnNewBall_Click(object sender, EventArgs e)
		{
			SelectToolButtons(btnNewBall);
			ShowHidePies(ballProps1);
			DeactivateSelectorAdders();

			_ballAdder.Mode = BallAdder.AddingMode.AddBall;
			_ballAdder.NewBallProps = ballProps1.ExposedProps;
		}
		private void btnNewSolidBall_Click(object sender, EventArgs e)
		{
			SelectToolButtons(btnNewSolidBall);
			ShowHidePies(solidBallProps1);
			DeactivateSelectorAdders();

			_ballAdder.Mode = BallAdder.AddingMode.AddSolidBall;
			_ballAdder.NewBallProps = solidBallProps1.ExposedProps;
		}
		private void btnNewRigidBody_Click(object sender, EventArgs e)
		{
			SelectToolButtons(btnNewRigidBody);
			ShowHidePies(null);
			DeactivateSelectorAdders();
		}
		private void btnNewPolygon_Click(object sender, EventArgs e)
		{
			SelectToolButtons(btnNewPolygon);
			ShowHidePies(null);
			DeactivateSelectorAdders();
		}
		private void btnNewVectorField_Click(object sender, EventArgs e)
		{
			SelectToolButtons(btnNewVectorField);
			ShowHidePies(null);
			DeactivateSelectorAdders();
		}

		//	Property Pages (Activates Arrow Selection)
		private void btnGravityProps_Click(object sender, EventArgs e)
		{
			SelectToolButtons(btnGravityProps, btnArrow);
			ShowHidePies(gravityProps1);
			DeactivateSelectorAdders();

            _selector.Active = true;
        }
		private void btnCollisionProps_Click(object sender, EventArgs e)
		{
			SelectToolButtons(btnCollisionProps, btnArrow);
			ShowHidePies(null);
			DeactivateSelectorAdders();

            _selector.Active = true;
        }
		private void btnGeneralProps_Click(object sender, EventArgs e)
		{
			SelectToolButtons(btnGeneralProps, btnArrow);
			ShowHidePies(generalProps1);
			DeactivateSelectorAdders();

            _selector.Active = true;
        }
		private void btnMapShape_Click(object sender, EventArgs e)
		{
			SelectToolButtons(btnMapShape, btnArrow);
			ShowHidePies(mapShape1);
			DeactivateSelectorAdders();

            _selector.Active = true;
        }
		private void btnScenes_Click(object sender, EventArgs e)
		{
			SelectToolButtons(btnScenes, btnArrow);
			ShowHidePies(scenes1);
			DeactivateSelectorAdders();

			_selector.Active = true;
		}
		private void btnShip_Click(object sender, EventArgs e)
		{
			SelectToolButtons(btnShip, btnArrow);
			ShowHidePies(shipProps1);
			DeactivateSelectorAdders();

            _selector.Active = true;
        }
        private void btnMenuTester_Click(object sender, EventArgs e)
        {
            SelectToolButtons(btnMenuTester, btnArrow);
            ShowHidePies(pieMenuTester1);
            DeactivateSelectorAdders();

            _selector.Active = true;
        }

		//	Simple Buttons
		private void btnClear_Click(object sender, EventArgs e)
		{
			_map.Clear(_ignoreTokens);
		}
		private void btnStopVelocity_Click(object sender, EventArgs e)
		{
			foreach (RadarBlip blip in _map.GetAllBlips())
			{
				if (blip.Sphere is Ball)
				{
					((Ball)blip.Sphere).StopBall();
				}
			}
		}
		private void btnZoomIn_Click(object sender, EventArgs e)
		{
			pictureBox1.ZoomRelative(.1);
		}
		private void btnZoomOut_Click(object sender, EventArgs e)
		{
			pictureBox1.ZoomRelative(-.1);
		}
		private void btnZoomFit_Click(object sender, EventArgs e)
		{
			pictureBox1.ZoomFit();
		}

		#endregion

		//	Keyboard
		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			/*
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
			*/

			if (keyData == Keys.Delete)
			{
				foreach (long selectedToken in _selector.SelectedObjects)
				{
                    if (_ignoreTokens.Contains(selectedToken))
                    {
                        continue;
                    }

					_map.Remove(selectedToken);
				}
			}

			if (keyData == Keys.Space)
			{
				btnStopVelocity_Click(this, new EventArgs());
			}

			if (keyData == Keys.F5)
			{
				scenes1.LoadCurrentScene();
			}
			if (keyData == Keys.F6)
			{
				scenes1.SaveCurrentScene();
			}

			if (keyData == Keys.P)
			{
				btnRunning.Checked = !btnRunning.Checked;
				btnRunning_Click(this, new EventArgs());
			}

			return base.ProcessCmdKey(ref msg, keyData);
			//return true;
		}
		protected override void OnKeyUp(KeyEventArgs e)
		{
			/*
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
			*/

			base.OnKeyUp(e);
		}
		private void PhysicsPainter_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Alt)		//	e.KeyData isn't Alt, it's something weird
			{
				if (!_isAltDown)		//	the event keeps firing while they hold it down
				{
					_isAltDown = true;
					btnPan.PerformClick();
				}
			}
		}
		private void PhysicsPainter_KeyUp(object sender, KeyEventArgs e)
		{
			if (_isAltDown && !e.Alt)
			{
				//	Once they let off the ban, go back to before
				_isAltDown = false;
				_beforePanButton.PerformClick();
			}
		}

		//	Timer
		private void timer1_Tick(object sender, EventArgs e)
		{
			Collision[] collisions = null;
			if (btnRunning.Checked)
			{
				#region Physics

				_map.PrepareForNewTimerCycle();

				if (_gravMouse.Active)
				{
					_gravMouse.Timer();
				}

				_shipController.Timer(ELAPSEDTIME);

                _gravController.Timer();

				collisions = _map.Timer(ELAPSEDTIME);

				if (_selector.Active)
				{
					_selector.TimerTick(ELAPSEDTIME);
				}

				#endregion
			}

			#region Examine Collisions

			if (collisions != null)
			{
				foreach (Collision collision in collisions)
				{
					#region Check Temps

					bool wasTemp = false;

					if (_tempObjects.Contains(collision.Blip1.Token))
					{
						_tempObjects.Remove(collision.Blip1.Token);
						_map.Remove(collision.Blip1.Token);
						wasTemp = true;
					}

					if (_tempObjects.Contains(collision.Blip2.Token))
					{
						_tempObjects.Remove(collision.Blip2.Token);
						_map.Remove(collision.Blip2.Token);
						wasTemp = true;
					}

					if (wasTemp)
					{
						continue;
					}

					#endregion

					if (_ignoreTokens.Contains(collision.Blip1.Token) || _ignoreTokens.Contains(collision.Blip2.Token))
					{
						continue;
					}

					if (collision.Blip1 is Projectile && !(collision.Blip2 is Projectile))
					{
						HurtBlip((Projectile)collision.Blip1, collision.Blip2);
					}
					else if (collision.Blip2 is Projectile && !(collision.Blip1 is Projectile))
					{
						HurtBlip((Projectile)collision.Blip2, collision.Blip1);
					}
				}
			}

			#endregion

			#region Reset Z

			_zeroResetCntr++;

			if (_zeroResetCntr % 100000 == 0)
			{
				_zeroResetCntr = 0;

				foreach (RadarBlip blip in _map.GetAllBlips())
				{
					blip.Sphere.Position.Z = 0;

					if (blip is BallBlip)
					{
						if (((BallBlip)blip).Ball.Velocity.Z != 0d)
						{
							MessageBox.Show("not zero velocity");
						}

						((BallBlip)blip).Ball.Velocity.Z = 0;
					}
				}
			}

			#endregion

			#region Draw

			pictureBox1.PrepareForNewDraw();

			if (_gravMouse.Active)
			{
				_gravMouse.Draw();
			}

			RadarBlip[] blips = _map.GetAllBlips();

			//	Draw all the blips
			for (int blipCntr = 0; blipCntr < blips.Length; blipCntr++)
			{
				if (_ignoreTokens.Contains(blips[blipCntr].Token))
				{
					continue;
				}

				//	Ask the selecter if this is selected
				ObjectRenderer.DrawMode drawMode = ObjectRenderer.DrawMode.Standard;
				if (_selector.IsSelected(blips[blipCntr].Token))
				{
					drawMode = ObjectRenderer.DrawMode.Selected;
				}

				//	See if it should be displayed as standard
				CollisionStyle collisionStyle = blips[blipCntr].CollisionStyle;
				if (_selector.TempStationaryObjects.Contains(blips[blipCntr].Token))
				{
					collisionStyle = CollisionStyle.Standard;
				}

				//	Draw the blip
				if (blips[blipCntr] is Projectile)
				{
					_renderer.DrawProjectile((Projectile)blips[blipCntr], drawMode, false);
				}
				else if (blips[blipCntr].Sphere is RigidBody)
				{
					_renderer.DrawRigidBody((RigidBody)blips[blipCntr].Sphere, drawMode, collisionStyle, false);
				}
				else if (blips[blipCntr].Sphere is SolidBall)
				{
					_renderer.DrawSolidBall((SolidBall)blips[blipCntr].Sphere, drawMode, collisionStyle, false);
				}
				else if (blips[blipCntr].Sphere is Ball)
				{
					_renderer.DrawBall((Ball)blips[blipCntr].Sphere, drawMode, collisionStyle, false);
				}
				else
				{
					MessageBox.Show("Unknown blip", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}

            _shipController.Draw();

			//	Let the active adder/selecter have a chance to draw
			if (_ballAdder.Mode != BallAdder.AddingMode.Inactive)
			{
				_ballAdder.Draw();
			}
			else if (_selector.Active)
			{
				_selector.Draw();
			}

			pictureBox1.FinishedDrawing();

			#endregion
		}

		#endregion

		#region Private Methods

		private void ShowHidePies(PiePanel shownPanel)
		{
			foreach (PiePanel piePanel in _piePanels)
			{
				if (piePanel == shownPanel)
				{
					piePanel.Visible = true;
				}
				else
				{
					piePanel.Visible = false;
				}
			}
		}

		private void SelectToolButtons(ToolStripButton checkedButton)
		{
			SelectToolButtons(checkedButton, null);
		}
		private void SelectToolButtons(ToolStripButton checkedButton1, ToolStripButton checkedButton2)
		{
			#region Change Check States

			foreach (ToolStripButton button in _checkButtons)
			{
				if (button == checkedButton1 || button == checkedButton2)
				{
					button.Checked = true;
				}
				else
				{
					button.Checked = false;
				}
			}

			#endregion

			#region Remember which button was pressed

			if (checkedButton2 == null)
			{
				//	Just one button
				if (checkedButton1 != null && checkedButton1 != btnPan)
				{
					_beforePanButton = checkedButton1;
				}
			}
			else
			{
				if (checkedButton1 == btnArrow || checkedButton2 == btnArrow)
				{
					//	One of them is the arrow, remember the other (because it's the real one that was pressed
					if (checkedButton1 == btnArrow)
					{
						_beforePanButton = checkedButton2;
					}
					else
					{
						_beforePanButton = checkedButton1;
					}
				}
				else
				{
					//	This may be a legitimate case, but wasn't anticipated at the time this function was written
					throw new ApplicationException("Two buttons were selected, and neither is an arrow");
				}
			}

			#endregion

			#region Apply Pan

			//TODO:  Come up with a better place for this
            if (btnPan.Checked)
            {
                pictureBox1.PanMouseButton = MouseButtons.Left;
            }
            else
            {
                pictureBox1.PanMouseButton = MouseButtons.None;
			}

			#endregion
		}

		private void DeactivateSelectorAdders()
		{
			_selector.Active = false;
			_ballAdder.Mode = BallAdder.AddingMode.Inactive;
			_gravMouse.Active = false;
		}

		private void HurtBlip(Projectile projectile, RadarBlip blip)
		{
			#region See if a split should occur

			if (blip.CollisionStyle != CollisionStyle.Standard)
			{
				return;
			}

			if (!(blip is BallBlip))
			{
				return;
			}

			BallBlip castBlip = (BallBlip)blip;

			double ratio = projectile.Pain / castBlip.Ball.Mass;
			double rand = _rand.NextDouble();

			if (ratio < rand)
			{
				return;
			}

			#endregion

			#region Calculate Split Percents

			int numParts = 2 + _rand.Next(3);		//	between 2 and 4 parts
			double[] splitPercents = new double[numParts];

			double remainder = 1d;

			for (int cntr = 0; cntr < numParts - 1; cntr++)
			{
				splitPercents[cntr] = _rand.NextDouble() * remainder;
				remainder -= splitPercents[cntr];
			}
			splitPercents[numParts - 1] = remainder;

			#endregion

			#region Build Objects

			_map.Remove(blip.Token);

			foreach (double percent in splitPercents)
			{
				double size = castBlip.Ball.Mass * percent;

				if (size < 20)
				{
					continue;
				}

				Ball ball;
				if (castBlip.Ball is SolidBall)
				{
					ball = new SolidBall(castBlip.Ball.Position.Clone(), castBlip.Ball.OriginalDirectionFacing.Clone(), size, size, castBlip.Ball.Elasticity, castBlip.Ball.KineticFriction, castBlip.Ball.StaticFriction, _boundryLower, _boundryUpper);
				}
				else
				{
					ball = new Ball(castBlip.Ball.Position.Clone(), castBlip.Ball.OriginalDirectionFacing.Clone(), size, size, castBlip.Ball.Elasticity, castBlip.Ball.KineticFriction, castBlip.Ball.StaticFriction, _boundryLower, _boundryUpper);
				}

				ball.Velocity.StoreNewValues(castBlip.Ball.Velocity);

				//TODO:  Lay them out so they aren't touching each other.  The smallest ones should be closest
				//	to the point of impact (maybe give them slightly tweaked velocities as well so they explode
				//	outward)

				_map.Add(new BallBlip(ball, blip.CollisionStyle, blip.Qual, TokenGenerator.Instance.NextToken()));
			}

			#endregion
		}

		#endregion
	}
}