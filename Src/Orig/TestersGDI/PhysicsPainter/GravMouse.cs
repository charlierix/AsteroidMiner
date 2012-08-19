using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using Game.HelperClasses;
using Game.Orig.HelperClassesOrig;
using Game.Orig.HelperClassesGDI;
using Game.Orig.Math3D;
using Game.Orig.Map;

namespace Game.Orig.TestersGDI.PhysicsPainter
{
	public class GravMouse
	{
		#region Enum: MouseButtonDown

		private enum MouseButtonDown
		{
			None = 0,
			Left,
			Right
		}

		#endregion

		#region Declaration Section

		private const double GRAVITATIONALCONSTANT = 1750000;

		//	Misc global objects
		private LargeMapViewer2D _picturebox = null;
		private SimpleMap _map = null;
		private MyVector _boundryLower = null;
        private MyVector _boundryUpper = null;

		private bool _active = false;
		private bool _cursorInMap = false;

		private MouseButtonDown _isMouseDown = MouseButtonDown.None;

        private MyVector _curMousePoint = null;

		private BallBlip _cursorBlip = null;

		List<Ball> _accelLines = new List<Ball>();
		List<Color> _accelColors = new List<Color>();

		private Color _attractColor = Color.Yellow;
		private Color _repelColor = Color.Plum;

		#endregion

		#region Constructor

        public GravMouse(LargeMapViewer2D picturebox, SimpleMap map, MyVector boundryLower, MyVector boundryUpper)
		{
			const double RADIUS = 400;

			_picturebox = picturebox;
			_map = map;
			_boundryLower = boundryLower;
			_boundryUpper = boundryUpper;

            _cursorBlip = new BallBlip(new Ball(new MyVector(), new DoubleVector(1, 0, 0, 0, 1, 0), RADIUS, UtilityHelper.GetMassForRadius(RADIUS, 1d), 1, 0, 0, _boundryLower, _boundryUpper), CollisionStyle.Stationary, RadarBlipQual.BallUserDefined05, _map.GetNextToken());

			_picturebox.MouseDown += new MouseEventHandler(picturebox_MouseDown);
			_picturebox.MouseUp += new MouseEventHandler(picturebox_MouseUp);
			_picturebox.MouseMove += new MouseEventHandler(picturebox_MouseMove);
			_picturebox.MouseLeave += new EventHandler(picturebox_MouseLeave);
		}

		#endregion

		#region Public Properties

		public bool Active
		{
			get
			{
				return _active;
			}
			set
			{
				_active = value;

				if (_active)
				{
					InsureBlipAddedRemoved(true);
				}
				else
				{
					InsureBlipAddedRemoved(false);
				}
			}
		}

		public long CursorToken
		{
			get
			{
				return _cursorBlip.Token;
			}
		}

		#endregion

		#region Public Methods

		public void Timer()
		{
			if (!(_active && _cursorInMap))
			{
				return;
			}

			_accelLines.Clear();
			_accelColors.Clear();

			Ball ball;
			double force;

			foreach (RadarBlip blip in _map.GetAllBlips())
			{
				#region Check for exempt blips

				if (blip.Token == _cursorBlip.Token)
				{
					continue;
				}

				if (blip.CollisionStyle != CollisionStyle.Standard)
				{
					continue;
				}

				if (!(blip is BallBlip))
				{
					continue;
				}

				#endregion

				#region Pull Apart

				if (_map.CollisionHandler.IsColliding(blip, _cursorBlip) == CollisionDepth.Penetrating)
				{
					_map.CollisionHandler.PullItemsApartInstant(blip, _cursorBlip);
				}

				#endregion

				ball = ((BallBlip)blip).Ball;

				#region Calculate Force

				//	Apply Force (ball to mouse)
                MyVector ballToMouse = _curMousePoint - ball.Position;

				double distanceSquared = ballToMouse.GetMagnitudeSquared();

				//	Force = G * (M1*M2) / R^2
				force = GRAVITATIONALCONSTANT * (ball.Mass / distanceSquared);

				if (_isMouseDown == MouseButtonDown.Left)
				{
					force *= 10d;
				}
				else if (_isMouseDown == MouseButtonDown.Right)
				{
					force *= -2d;
				}

				ballToMouse.BecomeUnitVector();
				ballToMouse.Multiply(force);

				ball.ExternalForce.Add(ballToMouse);

				#endregion

				#region Store Accel Line

				double acceleration = force / ball.Mass;
				double absAccel = Math.Abs(acceleration);

				if (absAccel > .05d)		//	otherwise, it's too faint to see anyway
				{
					_accelLines.Add(ball);

					int alpha = Convert.ToInt32((absAccel - .05d) * (255d / .95d));
					if (alpha < 0) alpha = 0;
					if (alpha > 255) alpha = 255;

					if (acceleration > 0d)
					{
						_accelColors.Add(Color.FromArgb(alpha, _attractColor));
					}
					else
					{
						_accelColors.Add(Color.FromArgb(alpha, _repelColor));
					}
				}

				#endregion
			}
		}

		public void Draw()
		{
			if (!(_active && _cursorInMap))
			{
				return;
			}

			if (_accelLines.Count > 0)
			{
				for (int lineCntr = 0; lineCntr < _accelLines.Count; lineCntr++)
				{
					_picturebox.DrawLine(_accelColors[lineCntr], 1, _curMousePoint, _accelLines[lineCntr].Position);
				}
			}

			_picturebox.DrawCircle(Color.Silver, 15, _curMousePoint, _cursorBlip.Ball.Radius);
		}

		#endregion

		#region Misc Control Events

		void picturebox_MouseDown(object sender, MouseEventArgs e)
		{
			if (!_active)
			{
				return;
			}

            _curMousePoint = _picturebox.GetPositionViewToWorld(new MyVector(e.X, e.Y, 0));
			_cursorBlip.Sphere.Position.StoreNewValues(_curMousePoint);

			if (e.Button == MouseButtons.Left)
			{
				_isMouseDown = MouseButtonDown.Left;
			}
			else if (e.Button == MouseButtons.Right)
			{
				_isMouseDown = MouseButtonDown.Right;
			}
		}
		void picturebox_MouseUp(object sender, MouseEventArgs e)
		{
			if (!_active)
			{
				return;
			}

            _curMousePoint = _picturebox.GetPositionViewToWorld(new MyVector(e.X, e.Y, 0));

			if (_isMouseDown == MouseButtonDown.Left && e.Button == MouseButtons.Left)
			{
				_isMouseDown = MouseButtonDown.None;
			}
			else if (_isMouseDown == MouseButtonDown.Right && e.Button == MouseButtons.Right)
			{
				_isMouseDown = MouseButtonDown.None;
			}

		}
		void picturebox_MouseMove(object sender, MouseEventArgs e)
		{
			//	Remember the mouse position
            _curMousePoint = _picturebox.GetPositionViewToWorld(new MyVector(e.X, e.Y, 0));
			_cursorBlip.Sphere.Position.StoreNewValues(_curMousePoint);

			if (!_active)
			{
				return;
			}

			InsureBlipAddedRemoved(true);

		}
		void picturebox_MouseLeave(object sender, EventArgs e)
		{
			InsureBlipAddedRemoved(false);
		}

		#endregion

		#region Private Methods

		private void InsureBlipAddedRemoved(bool added)
		{
			if (added)// && !_cursorInMap)			//	I can't trust that (they may have cleared all blips outside of this class
			{
				foreach (RadarBlip blip in _map.GetAllBlips())
				{
					if (blip.Token == _cursorBlip.Token)
					{
						return;
					}
				}

				//	It's not in
				_map.Add(_cursorBlip);
				_cursorInMap = true;
			}
			else if (!added && _cursorInMap)
			{
				//	Take it out
				_map.Remove(_cursorBlip.Token);
				_cursorInMap = false;
			}
		}

		#endregion
	}
}
