using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

using Game.HelperClasses;
using Game.Orig.HelperClassesOrig;
using Game.Orig.HelperClassesGDI;
using Game.Orig.Map;
using Game.Orig.Math3D;

namespace Game.Orig.TestersGDI.PhysicsPainter
{
	/// <summary>
	/// This is only meant to be used by the physics painter form.  It takes care of handling mouse input and drawing onto the
	/// picturebox
	/// </summary>
	/// <remarks>
	/// TODO:  May want to make a common interface or base class for these adder/selecter classes
	/// </remarks>
	public class BallAdder
	{
		#region Enum:  AddingMode

		public enum AddingMode
		{
			Inactive = 0,
			AddBall,
			AddSolidBall,
			AddRigidBody
		}

		#endregion

		#region Declaration Section

		private const double MINRADIUS = 20d;

		private Random _rand = new Random();

		//	Misc global objects
		private LargeMapViewer2D _picturebox = null;
		private ObjectRenderer _renderer = null;
		private BallProps _newBallProps = null;
		private SimpleMap _map = null;
        private MyVector _boundryLower = null;
        private MyVector _boundryUpper = null;
		private List<long> _tempObjects = null;

		private AddingMode _mode = AddingMode.Inactive;

		//	This will only go true during a left mouse press
		private bool _isMouseDown = false;

		//	Where the mouse was pushed
        private MyVector _mouseDownPoint = null;
        private MyVector _curMousePoint = null;

		//	If the size is set to something other than draw, this is how often new obects are creating (during
		//	mouse move)
		private int _creationRate = 0;
		private int _lastCreateTime = int.MinValue;

		/// <summary>
		/// If no ball was created, then during the mouseup, one will be forced to be created
		/// </summary>
		private bool _createdBallDuringMouseDrag = false;

		//	This will only get set during a mousedown and sizemode of draw
		//	Now it's also used as the next ball during a mouse drag (or eventually, a mouse up)
		private Ball _drawingBall = null;		//	ball is the lowest base class.  it could also be solidball or rigidbody

		//private double _diminishPercent = 1d;

		#endregion

		#region Constructor

        public BallAdder(LargeMapViewer2D picturebox, ObjectRenderer renderer, BallProps newBallProps, SimpleMap map, MyVector boundryLower, MyVector boundryUpper, List<long> tempObjects)
		{
			_picturebox = picturebox;
			_renderer = renderer;
			_newBallProps = newBallProps;
			_map = map;
			_boundryLower = boundryLower;
			_boundryUpper = boundryUpper;
			_tempObjects = tempObjects;

			_picturebox.MouseDown += new MouseEventHandler(picturebox_MouseDown);
			_picturebox.MouseUp += new MouseEventHandler(picturebox_MouseUp);
			_picturebox.MouseMove += new MouseEventHandler(picturebox_MouseMove);
		}

		#endregion

		#region Public Properties

		public AddingMode Mode
		{
			get
			{
				return _mode;
			}
			set
			{
				_mode = value;

				_isMouseDown = false;
			}
		}

		public BallProps NewBallProps
		{
			get
			{
				return _newBallProps;
			}
			set
			{
				//TODO:  If they were in the middle of a ball creation, then discard it

				_newBallProps = value;
			}
		}

		/// <summary>
		/// How often to create a new object during a mouse move (milliseconds)
		/// </summary>
		public int CreationRate
		{
			get
			{
				return _creationRate;
			}
			set
			{
				_creationRate = value;
			}
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// This is my chance to draw any objects I may be creating
		/// </summary>
		public void Draw()
		{
			if (_mode != AddingMode.Inactive && _isMouseDown && _drawingBall != null)
			{
				//	They are in the middle of dragging a ball into existance.  Draw it in an intermidiate form
				switch (_mode)
				{
					case AddingMode.AddBall:
						_renderer.DrawBall((Ball)_drawingBall, ObjectRenderer.DrawMode.Building, _newBallProps.CollisionStyle, false);
						break;

					case AddingMode.AddSolidBall:
						_renderer.DrawSolidBall((SolidBall)_drawingBall, ObjectRenderer.DrawMode.Building, _newBallProps.CollisionStyle, false);
						break;

					case AddingMode.AddRigidBody:
						_renderer.DrawRigidBody((RigidBody)_drawingBall, ObjectRenderer.DrawMode.Building, _newBallProps.CollisionStyle, false);
						break;

					default:
						throw new ApplicationException("Unknown AddingMode: " + _mode.ToString());
				}
			}

			//	Nothing else to draw

		}

		#endregion

		#region Misc Control Events

		void picturebox_MouseDown(object sender, MouseEventArgs e)
		{
			if (_mode == AddingMode.Inactive)
			{
				return;
			}

			if (e.Button == MouseButtons.Left)
			{
				_isMouseDown = true;
                _mouseDownPoint = _picturebox.GetPositionViewToWorld(new MyVector(e.X, e.Y, 0));
				_curMousePoint = _mouseDownPoint.Clone();
				_lastCreateTime = Environment.TickCount;
				_createdBallDuringMouseDrag = false;
				//_diminishPercent = 1d;

				if (_newBallProps.SizeMode == BallProps.SizeModes.Draw)
				{
					//	I need to create an object now (but don't commit it to the map), so that the user can see
					//	it while they drag the size
					_drawingBall = BuildObject();
				}
			}
		}
		void picturebox_MouseUp(object sender, MouseEventArgs e)
		{
			if (_mode == AddingMode.Inactive)
			{
				return;
			}

			if (_isMouseDown && e.Button == MouseButtons.Left)
			{
				_isMouseDown = false;
                _mouseDownPoint = _picturebox.GetPositionViewToWorld(new MyVector(e.X, e.Y, 0));

				if (_drawingBall == null)
				{
					_drawingBall = BuildObject();
				}

				if (_newBallProps.SizeMode == BallProps.SizeModes.Draw)
				{
					CommitObject();
				}
				else
				{
					_drawingBall.Position.StoreNewValues(_curMousePoint);

					if (!WillCollide(_drawingBall) || !_createdBallDuringMouseDrag)
					{
						//	It won't collide, or nothing has been created yet
						CommitObject();		//	I don't care about the draw rate during mouse up
					}
				}

				_drawingBall = null;		//	even if I didn't commit it, I still need to kill it now
			}
		}
		void picturebox_MouseMove(object sender, MouseEventArgs e)
		{
			if (_mode == AddingMode.Inactive || !_isMouseDown)
			{
				return;
			}

			//	Remember the mouse position
            _curMousePoint = _picturebox.GetPositionViewToWorld(new MyVector(e.X, e.Y, 0));

			switch (_newBallProps.SizeMode)
			{
				case BallProps.SizeModes.Draw:
					ResizeDrawingObject();
					break;

				case BallProps.SizeModes.Fixed:
				case BallProps.SizeModes.Random:
					if (Environment.TickCount - _lastCreateTime > _creationRate)
					{
						//	Enough time has elapsed.  Try to create a new object
						if (_drawingBall == null)
						{
							_drawingBall = BuildObject();
						}

						_drawingBall.Position.StoreNewValues(_curMousePoint);

						//	See if this will collide with anything
						if (!WillCollide(_drawingBall))
						{
							CommitObject();
							_lastCreateTime = Environment.TickCount;
							_createdBallDuringMouseDrag = true;
							_drawingBall = null;
						}
					}
					break;

				default:
					throw new ApplicationException("Unknown BallProps.SizeModes: " + _newBallProps.SizeMode.ToString());
			}
		}

		#endregion

		#region Private Methods

		private void CommitObject()
		{
			Ball newObject;
			#region Get the base object to add

			if (_drawingBall != null)
			{
				newObject = _drawingBall;
				_drawingBall = null;
			}
			else
			{
				newObject = BuildObject();
			}

			#endregion

			RadarBlipQual blipQual;
			#region Figure out the blipqual

			//TODO:  Define these as constants somewhere
			switch (_mode)
			{
				case AddingMode.AddBall:
					blipQual = RadarBlipQual.BallUserDefined00;
					break;

				case AddingMode.AddSolidBall:
					blipQual = RadarBlipQual.BallUserDefined01;
					break;

				case AddingMode.AddRigidBody:
					blipQual = RadarBlipQual.BallUserDefined02;
					break;

				default:
					throw new ApplicationException("Unknown AddingMode: " + _mode.ToString());
			}

			#endregion

			//	Create a blip to contain this object
			BallBlip blip = new BallBlip(newObject, _newBallProps.CollisionStyle, blipQual, _map.GetNextToken());

			//	If this is a torqueball, then it will get the angular velocity.  I have to wait until now, because
			//	the size could change during a draw (you don't see it spin during the drag anyway, because I
			//	don't call it's timer, so there is no reason to set angular velocity before now.)
			StoreAngularVelocity(blip.Ball);

			//	Add it to the map
			_map.Add(blip);

			if (_newBallProps.Temporary)
			{
				_tempObjects.Add(blip.Token);
			}
		}
		private Ball BuildObject()
		{
			double radius;
			#region Calculate Radius

			switch (_newBallProps.SizeMode)
			{
				case BallProps.SizeModes.Draw:
					radius = .01d;		//	this function will only get called during mouse down if it's in draw mode, so I need to start with an arbitrarily small radius
					break;

				case BallProps.SizeModes.Fixed:
					radius = _newBallProps.SizeIfFixed;
					break;

				case BallProps.SizeModes.Random:
					radius = _rand.Next(Convert.ToInt32(_newBallProps.MinRandSize), Convert.ToInt32(_newBallProps.MaxRandSize));
					break;

				default:
					throw new ApplicationException("Unknown BallProps.SizeModes: " + _newBallProps.SizeMode);
			}

			if (radius < MINRADIUS)
			{
				radius = MINRADIUS;
			}

			#endregion

			double mass = UtilityHelper.GetMassForRadius(radius, 1d);

            MyVector velocity;
			#region Calculate Velocity

			if (_newBallProps.RandomVelocity)
			{
				velocity = Utility3D.GetRandomVectorSpherical2D(_newBallProps.MaxVelocity);
			}
			else
			{
				velocity = _newBallProps.Velocity;		//	no need to clone it.  I won't manipulate it
			}

			#endregion

			//TODO:  Listen to global props
			double elasticity = .75d;
			double kineticFriction = .75d;
			double staticFriction = 1d;

			Ball retVal;
			#region Build the ball

			switch (_mode)
			{
				case AddingMode.AddBall:
					#region Create Ball

					retVal = new Ball(_curMousePoint.Clone(), new DoubleVector(0, 1, 0, 1, 0, 0), radius, mass, elasticity, kineticFriction, staticFriction, _boundryLower, _boundryUpper);
					retVal.Velocity.Add(velocity);

					#endregion
					break;

				case AddingMode.AddSolidBall:
					#region Create Solid Ball

					retVal = new SolidBall(_curMousePoint.Clone(), new DoubleVector(0, 1, 0, 1, 0, 0), radius, mass, elasticity, kineticFriction, staticFriction, _boundryLower, _boundryUpper);
					retVal.Velocity.Add(velocity);
					//StoreAngularVelocity(retVal);		//	no reason to do this here.  it will be applied during commit (if I'm in draw mode, the size will change, and the angular velocity will need to be reapplied anyway)

					#endregion
					break;

				default:
					throw new ApplicationException("Unsupported AddingMode: " + _mode.ToString());
			}

			#endregion

			//	Exit Function
			return retVal;
		}

		/// <summary>
		/// This should only be called if _newBallProps.SizeMode is Draw
		/// </summary>
		private void ResizeDrawingObject()
		{
			//	Find the vector from the mousedown point to the current point
            MyVector fromToLine = _curMousePoint - _mouseDownPoint;

			//	Adjust the radius and mass
			switch (_mode)
			{
				case AddingMode.AddBall:
				case AddingMode.AddSolidBall:
					double newValue = fromToLine.GetMagnitude();
					if (newValue < MINRADIUS)
					{
						newValue = MINRADIUS;
					}

					_drawingBall.Radius = newValue;
					_drawingBall.Mass = UtilityHelper.GetMassForRadius(newValue, 1d);
					break;

				//case AddingMode.AddRigidBody:
				//    //TODO:  I will need to pull all the point masses out proportionatly, as well as change their masses
				//    break;

				default:
					throw new ApplicationException("Unknown AddingMode: " + _mode.ToString());
			}
		}

		private bool WillCollide(Ball ball)
		{
			//	Make a temp blip to be a wrapper for this
			RadarBlip blip = new RadarBlip(ball, CollisionStyle.Standard, RadarBlipQual.BallUserDefined10, _map.GetNextToken());

			foreach(RadarBlip existingBlip in _map.GetAllBlips())
			{
				if (_map.CollisionHandler.IsColliding(blip, existingBlip) != CollisionDepth.NotColliding)
				{
					//	It's colliding
					return true;
				}
			}

			return false;

		}

		private void StoreAngularVelocity(Ball ball)
		{
			if (!(ball is TorqueBall))
			{
				return;
			}

            MyVector angularVelocity;
			#region Calculate Angular Velocity

			switch (_newBallProps.AngularVelocityMode)
			{
				case BallProps.AngularVelocityModes.Fixed:
                    angularVelocity = new MyVector(0, 0, _newBallProps.AngularVelocityIfFixed);
					break;

				case BallProps.AngularVelocityModes.Random:
                    angularVelocity = new MyVector(0, 0, UtilityHelper.GetScaledValue(_newBallProps.MinRandAngularVelocity, _newBallProps.MaxRandAngularVelocity, 0, 1, _rand.NextDouble()));
					break;

				default:
					throw new ApplicationException("Unknown BallProps.AngularVelocityModes: " + _newBallProps.AngularVelocityMode.ToString());
			}

			#endregion

			//	Apply Angular Velocity
			((TorqueBall)ball).SetAngularVelocity(angularVelocity);

		}

		#endregion
	}
}
