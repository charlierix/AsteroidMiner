using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

using Game.Newt.HelperClasses;
using Game.Newt.NewtonDynamics.Import;

namespace Game.Newt.NewtonDynamics
{
	/// <summary>
	/// WorldBase is a pure newton object.  World syncs with a wpf viewport, has a timer
	/// </summary>
	/// <remarks>
	/// I'm building this wrapper to have good generic default behavior, but there could be cases where it fails.
	/// 
	/// I also added SetCollisionBoundry, which will make bodies bounce off the edges instead of freezing.  Note that ray casting calls
	/// will pick up my edge bodies
	/// 
	/// An interesting idea for a physics engine that has no visual (or at least not a standard visual) would be for an AI to build mental
	/// models of its surroundings
	/// </remarks>
	public class World : WorldBase
	{
		#region Class: RealtimeClock

		private class RealtimeClock
		{
			private bool _isFirstCall = true;
			private DateTime _lastUpdate;

			/// <summary>
			/// This returns how many seconds have elapsed since the last time this was called
			/// </summary>
			public double Update()
			{
				double retVal = 0d;

				DateTime time = DateTime.Now;

				//	Figure out how much time has elapsed
				if (_isFirstCall)
				{
					_isFirstCall = false;
				}
				else
				{
					retVal = (time - _lastUpdate).TotalSeconds;
				}

				//	Remember when this was called
				_lastUpdate = time;

				//	Exit Function
				return retVal;
			}
		}

		#endregion

		#region Events

		public event EventHandler<WorldUpdatingArgs> Updating = null;
		public event EventHandler<WorldUpdatingArgs> Updated = null;		//	I can't think of a need for having a post update event, but it's easy to implement

		#endregion

		#region Declaration Section

		/// <summary>
		/// If you've called SetCollisionBoundry, this is the shapeID of the hulls (I tried to use a number that won't interfere)
		/// </summary>
		public const int BOUNDRYBODYID = int.MaxValue - 15;

		/// <summary>
		/// This remembers how much time has elapsed between timer ticks
		/// </summary>
		private RealtimeClock _clock = new RealtimeClock();

		/// <summary>
		/// This is the timer that updates the world
		/// </summary>
		/// <remarks>
		/// Have a visual timer and a physics timer?
		/// </remarks>
		private DispatcherTimer _timer;

		/// <summary>
		/// This is here just in cause dispose is called from within a world update event
		/// </summary>
		private bool _mustDispose = false;

		/// <summary>
		/// This defines a rectangular cavity that objects collide against (keeps things inside, rather than just stopping
		/// them)
		/// </summary>
		private List<Body> _boundry = null;
		private Point3D? _boundryMin = null;     // these are stored explicitly so I can force objects to get back inside (the boundry bodies are to let the engine do the collisions, but fast moving objects still get through)
		private Point3D? _boundryMax = null;

		#endregion

		#region Constructor

		public World()
			: base()
		{
			//	Not sure if this needs to be called or not
			SetMemorySystem();

			//	Defaulting to this (it's the best overall choice)
			SetSolverModel(SolverModel.AdaptativeMode);

			_timer = new DispatcherTimer();
			_timer.IsEnabled = false;		//	this gets enabled when they call UnPause
			_timer.Interval = TimeSpan.FromMilliseconds(25);
			_timer.Tick += new EventHandler(Timer_Tick);
		}

		#endregion

		#region Public Properties

		private bool _isPaused = true;
		public bool IsPaused
		{
			get
			{
				return _isPaused;
			}
		}

		private double _simulationSpeed = 1d;
		/// <summary>
		/// This is a multiplier telling how fast the simulation should run (multiplied against number of seconds
		/// since last frame)
		/// </summary>
		/// <remarks>
		/// NOTE:  Newton won't allow an elapsed time greater than 50 milliseconds in any one cycle, so if the
		/// calculated elapsed time is greater than this, I cheat and update the world multiple times.  If this takes
		/// longer than the timer's interval, then the simulation speed becomes limited by the hardware.
		/// </remarks>
		public double SimulationSpeed
		{
			get
			{
				return _simulationSpeed;
			}
			set
			{
				if (value <= 0d)
				{
					throw new ArgumentException("SimulationSpeed must be greater than zero: " + value.ToString());
				}

				_simulationSpeed = value;
			}
		}

		private bool _inWorldUpdate = false;
		public bool InWorldUpdate
		{
			get
			{
				return _inWorldUpdate;
			}
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// This lets you define a cube that everything is inside of, and will collide against.
		/// This overload returns lines so they could be drawn (if you want)
		/// </summary>
		/// <remarks>
		/// SetCollisionBoundry will cause bodies to bounce off of the boundry.  WorldBase.SetWorldSize will cause bodies to simply
		/// freeze once they hit the boundry.  This calls worldbase.setworldsize, but with bit larger size to minimize the chance of bodies
		/// freezing
		/// </remarks>
		/// <param name="innerLines">element0 - from, element1 - to</param>
		/// <param name="outerLines">element0 - from, element1 - to</param>
		public void SetCollisionBoundry(out List<Point3D[]> innerLines, out List<Point3D[]> outerLines, Point3D min, Point3D max)
		{
			const double MAXDEPTH = 1000d;

			ClearCollisionBoundry();

			_boundry = new List<Body>();

			#region Calculate Depths

			//	If the boundry is just plates, then fast moving objects will pass thru
			double depthX = ((max.Y - min.Y) + (max.Z - min.Z)) / 2d;
			double depthY = ((max.X - min.X) + (max.Z - min.Z)) / 2d;
			double depthZ = ((max.X - min.X) + (max.Y - min.Y)) / 2d;

			if (depthX > MAXDEPTH)
			{
				depthX = MAXDEPTH;
			}
			if (depthY > MAXDEPTH)
			{
				depthY = MAXDEPTH;
			}
			if (depthZ > MAXDEPTH)
			{
				depthZ = MAXDEPTH;
			}

			#endregion

			#region Collision Bodies

			//	Left
			CreateBoundryBody(new Point3D(min.X - depthX, min.Y - depthY, min.Z - depthZ), new Point3D(min.X, max.Y + depthY, max.Z));
			//	Right
			CreateBoundryBody(new Point3D(max.X, min.Y - depthY, min.Z), new Point3D(max.X + depthX, max.Y + depthY, max.Z + depthZ));

			//	Top
			CreateBoundryBody(new Point3D(min.X, max.Y, min.Z), new Point3D(max.X, max.Y + depthY, max.Z));
			//	Bottom
			CreateBoundryBody(new Point3D(min.X, min.Y - depthY, min.Z), new Point3D(max.X, min.Y, max.Z));

			//	Far
			CreateBoundryBody(new Point3D(min.X, min.Y - depthY, min.Z - depthZ), new Point3D(max.X + depthX, max.Y + depthY, min.Z));
			//	Near
			CreateBoundryBody(new Point3D(min.X - depthX, min.Y - depthY, max.Z), new Point3D(max.X, max.Y + depthY, max.Z + depthZ));

			#endregion

			#region Inner Lines

			innerLines = new List<Point3D[]>();

			//	Far
			innerLines.Add(new Point3D[] { new Point3D(min.X, min.Y, min.Z), new Point3D(max.X, min.Y, min.Z) });
			innerLines.Add(new Point3D[] { new Point3D(min.X, max.Y, min.Z), new Point3D(max.X, max.Y, min.Z) });
			innerLines.Add(new Point3D[] { new Point3D(min.X, min.Y, min.Z), new Point3D(min.X, max.Y, min.Z) });
			innerLines.Add(new Point3D[] { new Point3D(max.X, min.Y, min.Z), new Point3D(max.X, max.Y, min.Z) });

			//	Near
			innerLines.Add(new Point3D[] { new Point3D(min.X, min.Y, max.Z), new Point3D(max.X, min.Y, max.Z) });
			innerLines.Add(new Point3D[] { new Point3D(min.X, max.Y, max.Z), new Point3D(max.X, max.Y, max.Z) });
			innerLines.Add(new Point3D[] { new Point3D(min.X, min.Y, max.Z), new Point3D(min.X, max.Y, max.Z) });
			innerLines.Add(new Point3D[] { new Point3D(max.X, min.Y, max.Z), new Point3D(max.X, max.Y, max.Z) });

			//	Connecting Z's
			innerLines.Add(new Point3D[] { new Point3D(min.X, min.Y, min.Z), new Point3D(min.X, min.Y, max.Z) });
			innerLines.Add(new Point3D[] { new Point3D(min.X, max.Y, min.Z), new Point3D(min.X, max.Y, max.Z) });
			innerLines.Add(new Point3D[] { new Point3D(max.X, min.Y, min.Z), new Point3D(max.X, min.Y, max.Z) });
			innerLines.Add(new Point3D[] { new Point3D(max.X, max.Y, min.Z), new Point3D(max.X, max.Y, max.Z) });

			#endregion
			#region Outer Lines

			outerLines = new List<Point3D[]>();

			//	Far
			outerLines.Add(new Point3D[] { new Point3D(min.X - depthX, min.Y - depthY, min.Z - depthZ), new Point3D(max.X + depthX, min.Y - depthY, min.Z - depthZ) });
			outerLines.Add(new Point3D[] { new Point3D(min.X - depthX, max.Y + depthY, min.Z - depthZ), new Point3D(max.X + depthX, max.Y + depthY, min.Z - depthZ) });
			outerLines.Add(new Point3D[] { new Point3D(min.X - depthX, min.Y - depthY, min.Z - depthZ), new Point3D(min.X - depthX, max.Y + depthY, min.Z - depthZ) });
			outerLines.Add(new Point3D[] { new Point3D(max.X + depthX, min.Y - depthY, min.Z - depthZ), new Point3D(max.X + depthX, max.Y + depthY, min.Z - depthZ) });

			//	Near
			outerLines.Add(new Point3D[] { new Point3D(min.X - depthX, min.Y - depthY, max.Z + depthZ), new Point3D(max.X + depthX, min.Y - depthY, max.Z + depthZ) });
			outerLines.Add(new Point3D[] { new Point3D(min.X - depthX, max.Y + depthY, max.Z + depthZ), new Point3D(max.X + depthX, max.Y + depthY, max.Z + depthZ) });
			outerLines.Add(new Point3D[] { new Point3D(min.X - depthX, min.Y - depthY, max.Z + depthZ), new Point3D(min.X - depthX, max.Y + depthY, max.Z + depthZ) });
			outerLines.Add(new Point3D[] { new Point3D(max.X + depthX, min.Y - depthY, max.Z + depthZ), new Point3D(max.X + depthX, max.Y + depthY, max.Z + depthZ) });

			//	Connecting Z's
			outerLines.Add(new Point3D[] { new Point3D(min.X - depthX, min.Y - depthY, min.Z - depthZ), new Point3D(min.X - depthX, min.Y - depthY, max.Z + depthZ) });
			outerLines.Add(new Point3D[] { new Point3D(min.X - depthX, max.Y + depthY, min.Z - depthZ), new Point3D(min.X - depthX, max.Y + depthY, max.Z + depthZ) });
			outerLines.Add(new Point3D[] { new Point3D(max.X + depthX, min.Y - depthY, min.Z - depthZ), new Point3D(max.X + depthX, min.Y - depthY, max.Z + depthZ) });
			outerLines.Add(new Point3D[] { new Point3D(max.X + depthX, max.Y + depthY, min.Z - depthZ), new Point3D(max.X + depthX, max.Y + depthY, max.Z + depthZ) });

			#endregion

			//	Nothing should be placed outside these boundries, so let the engine optimize
			this.SetWorldSize(new Point3D(min.X - depthX, min.Y - depthY, min.Z - depthZ), new Point3D(max.X + depthX, max.Y + depthY, max.Z + depthZ));
			_boundryMin = min;  // storing the more constrained boundry, because if anything gets outside the actual boundry, it's frozen
			_boundryMax = max;
		}
		/// <summary>
		/// This leaves the world size alone, it just gets rid of the bodies that reflect objects back inside the body
		/// </summary>
		public void ClearCollisionBoundry()
		{
			if (_boundry != null)
			{
				foreach (Body prev in _boundry)
				{
					prev.Dispose();		//	the body's dispose will call this.BodyDisposed
				}
				_boundry.Clear();
				_boundry = null;
			}
		}

		public void Pause()
		{
			_timer.IsEnabled = false;
			_isPaused = true;
		}
		public void UnPause()
		{
			// I need to call this, or when the timer first fires, the clock will see a large time gap, and newton will take a long time
			// to calculate new positions, then everything will jump when it finally does start running again.  Just bad
			_clock.Update();

			_isPaused = false;
			_timer.IsEnabled = true;
		}

		#endregion

		#region Event Listeners

		private void Timer_Tick(object sender, EventArgs e)
		{
			//	Elapsed time is in seconds
			//const double MAXELAPSEDTIME = 1d / 20d;		//	newton won't allow > 20 fps during a single update

			//	Figure out how much time has elapsed since the last update
			double elapsedTime = _clock.Update();
			elapsedTime *= _simulationSpeed;

			OnUpdate(elapsedTime);


			//	I'm not sure that all this does much good

			//if (elapsedTime > MAXELAPSEDTIME)
			//{
			//    elapsedTime = MAXELAPSEDTIME;
			//}

			//if (elapsedTime > MAXELAPSEDTIME)
			//{
			//    #region Call multiple times

			//    double remaining = elapsedTime;

			//    while (remaining > 0d)
			//    {
			//        if (remaining > MAXELAPSEDTIME)
			//        {
			//            OnUpdate(MAXELAPSEDTIME);
			//            remaining -= MAXELAPSEDTIME;
			//        }
			//        else
			//        {
			//            //	To be consistent with the way the original update method was designed, I need to update the
			//            //	tracker clock now
			//            _clock.Update();

			//            OnUpdate(remaining);
			//            break;
			//        }
			//    }

			//    #endregion
			//}
			//else
			//{
			//    OnUpdate(elapsedTime);
			//}
		}

		private void body_ApplyForceAndTorque(object sender, BodyApplyForceAndTorqueArgs e)
		{
			if (_boundryMin != null && e.Body.Mass > 0)// && !e.Body.IsAsleep)
			{
				FixPositionsAndVelocities(e.Body);
			}
		}

		#endregion
		#region Overrides

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (_inWorldUpdate)
				{
					_mustDispose = true;
				}
				else
				{
					Pause(); // stop the body events


					//TODO:  Finish this


					base.Dispose(disposing);
				}
			}
		}

		internal override void BodyCreated(Body body)
		{
			body.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(body_ApplyForceAndTorque);

			base.BodyCreated(body);
		}
		internal override void BodyDisposed(Body body)
		{
			body.ApplyForceAndTorque -= new EventHandler<BodyApplyForceAndTorqueArgs>(body_ApplyForceAndTorque);

			base.BodyDisposed(body);
		}

		public override List<IntersectionPoint> RayCast(Point3D startPoint, Point3D endPoint)
		{
			List<IntersectionPoint> retVal = base.RayCast(startPoint, endPoint);

			if (_boundry != null)
			{
				//	Remove the boundry bodies
				int index = 0;
				while (index < retVal.Count)
				{
					if (_boundry.Contains(retVal[index].Body))
					{
						retVal.RemoveAt(index);
					}
					else
					{
						index++;
					}
				}
			}

			return retVal;
		}

		#endregion

		#region Protected Methods

		protected virtual void OnUpdate(double elapsedTime)
		{
			//	Raise an event (doing this first to give listeners a chance to prep for the body update events that will get raised by newton)
			if (this.Updating != null)
			{
				this.Updating(this, new WorldUpdatingArgs(elapsedTime));
			}

			//	Tell the world to do its thing
			_inWorldUpdate = true;
			base.Update(elapsedTime);
			_inWorldUpdate = false;

			if (_mustDispose)
			{
				//	Dispose was called during the world update
				Dispose(true);
				return;
			}

			//TODO:  hook up bodies
			//	Remove anything that needs to be removed
			//if (_removed.Count > 0)
			//{
			//    foreach (Body body in _removed)
			//    {
			//        body.Dispose();
			//    }

			//    _removed.Clear();
			//}

			if (this.Updated != null)
			{
				this.Updated(this, new WorldUpdatingArgs(elapsedTime));
			}
		}

		#endregion

		#region Private Methods

		private void CreateBoundryBody(Point3D min, Point3D max)
		{
			Vector3D size = new Vector3D(max.X - min.X, max.Y - min.Y, max.Z - min.Z);
			CollisionHull hull = CollisionHull.CreateBox(this, BOUNDRYBODYID, size, null);

			Vector3D offset = min.ToVector() + (size / 2d);
			TranslateTransform3D transform = new TranslateTransform3D(offset);

			Body body = new Body(hull, transform.Value, 0, null);		//	by giving it zero mass, it will be a static object

			_boundry.Add(body);
		}

		/// <summary>
		/// This is a method I threw in that will enforce that bodies don't fly out farther than they are supposed to
		/// NOTE: This must be called from within the ApplyForceAndTorque callback
		/// </summary>
		private void FixPositionsAndVelocities(Body body)
		{
			const double RETURNVELOCITY = 0d;

			// Get the center of mass in world coords
			Point3D position = body.Position;

			if (_boundryMin != null)        // if min is non null, _boundryMax is also non null.  I don't want to waste the processor checking that each frame
			{
				#region Stay inside bounding box

				//NOTE:  Position is the center of mass, so if the position is past the boundry, that means the body was travelling too fast for newton to bounce
				//it back (there is a wall at the boundry position).  So this logic is an extra helper

				//	My original attempt was to set acceleration/velocity, but stuff was still stuck.  So I'll physically move the objects

				//TODO:  Since this method no longer applies forces, there's no reason for it to be called every frame during the force/torque callback

				//TODO:  May want to take the body's AABB into account instead of the center

				Vector3D velocity = body.Velocity;    // already in world coords
				bool modifiedVelocity = false;
				bool modifiedPosition = false;

				#region X

				if (position.X < _boundryMin.Value.X)
				{
					if (velocity.X < 0)
					{
						velocity.X = RETURNVELOCITY;
						modifiedVelocity = true;
					}

					position.X = _boundryMin.Value.X;
					modifiedPosition = true;
				}
				else if (position.X > _boundryMax.Value.X)
				{
					if (velocity.X > 0)
					{
						velocity.X = -RETURNVELOCITY;
						modifiedVelocity = true;
					}

					position.X = _boundryMax.Value.X;
					modifiedPosition = true;
				}

				#endregion
				#region Y

				if (position.Y < _boundryMin.Value.Y)
				{
					if (velocity.Y < 0)
					{
						velocity.Y = RETURNVELOCITY;
						modifiedVelocity = true;
					}

					position.Y = _boundryMin.Value.Y;
					modifiedPosition = true;
				}
				else if (position.Y > _boundryMax.Value.Y)
				{
					if (velocity.Y > 0)
					{
						velocity.Y = -RETURNVELOCITY;
						modifiedVelocity = true;
					}

					position.Y = _boundryMax.Value.Y;
					modifiedPosition = true;
				}

				#endregion
				#region Z

				if (position.Z < _boundryMin.Value.Z)
				{
					if (velocity.Z < 0)
					{
						velocity.Z = RETURNVELOCITY;
						modifiedVelocity = true;
					}

					position.Z = _boundryMin.Value.Z;
					modifiedPosition = true;
				}
				else if (position.Z > _boundryMax.Value.Z)
				{
					if (velocity.Z > 0)
					{
						velocity.Z = -RETURNVELOCITY;
						modifiedVelocity = true;
					}

					position.Z = _boundryMax.Value.Z;
					modifiedPosition = true;
				}

				#endregion

				if (modifiedVelocity)
				{
					body.Velocity = velocity;        // already in world coords
				}

				if (modifiedPosition)
				{
					body.Position = position;		// already in world coords
				}

				#endregion
			}

			#region Force2D

			//if (_shouldForce2D)
			//{
			//    if (!body.Override2DEnforcement_Rotation)
			//    {
			//        #region Angular Pos/Vel

			//        //body.NewtonBody.AddTorque(new Vector3D(.01, 0, 0));       // pulls back (front comes up, rear goes down)
			//        //body.NewtonBody.AddTorque(new Vector3D(0, .1, 0));    // creates a roll to the right (left side goes up, right side goes down)


			//        Vector3D angularVelocity = body.Omega;        // Omega seems to be angular velocity
			//        bool modifiedAngularVelocity = false;

			//        //const double RESTORETORQUE = 15d;     // TODO:  look at the inertia tensor for the axis I want to apply torque to, and do and calculate to make a constant accel
			//        const double RESTORETORQUE = .25d;     // TODO:  look at the inertia tensor for the axis I want to apply torque to, and do and calculate to make a constant accel

			//        double massMatrixLength = body.MassMatrix.m_I.Length;
			//        double restoreTorqueX = RESTORETORQUE * body.MassMatrix.m_Mass * (1 / (body.MassMatrix.m_I.X / massMatrixLength));
			//        double restoreTorqueY = RESTORETORQUE * body.MassMatrix.m_Mass * (1 / (body.MassMatrix.m_I.Y / massMatrixLength));

			//        //double restoreTorqueX = RESTORETORQUE;     // pulling values from the mass matrix seemed to cause the engine to corrupt.  See if there's a problem casting that structure
			//        //double restoreTorqueY = RESTORETORQUE;

			//        //TODO:  Dampen the angluar velocidy if the object is very close to zero and the angular speed is small.  Currently, the object has
			//        //	a very slight wobble indefinately

			//        #region X

			//        Vector3D fromVect = new Vector3D(1, 0, 0);
			//        Vector3D toVect = body.DirectionToWorld(fromVect);
			//        Vector3D axis;
			//        double radians;
			//        Math3D.GetRotation(out axis, out radians, fromVect, toVect);

			//        if ((axis.Y > 0 && radians > 0) || (axis.Y < 0 && radians < 0))
			//        {
			//            if (angularVelocity.Y > 0)
			//            {
			//                angularVelocity.Y = 0;
			//                modifiedAngularVelocity = true;
			//            }

			//            //body.NewtonBody.AddTorque(new Vector3D(0, -RESTORETORQUE, 0));
			//            //body.NewtonBody.AddTorque(body.DirectionToWorld(new Vector3D(0, -RESTORETORQUE, 0)));     // apply torque seems to want world coords
			//            body.NewtonBody.AddTorque(body.DirectionToWorld(new Vector3D(0, -restoreTorqueY * Math.Abs(radians), 0)));
			//        }
			//        else if ((axis.Y > 0 && radians < 0) || (axis.Y < 0 && radians > 0))
			//        {
			//            if (angularVelocity.Y < 0)
			//            {
			//                angularVelocity.Y = 0;
			//                modifiedAngularVelocity = true;
			//            }

			//            //body.NewtonBody.AddTorque(new Vector3D(0, RESTORETORQUE, 0));
			//            body.NewtonBody.AddTorque(body.DirectionToWorld(new Vector3D(0, restoreTorqueY * Math.Abs(radians), 0)));
			//        }

			//        #endregion
			//        #region Y

			//        fromVect = new Vector3D(0, 1, 0);
			//        toVect = body.DirectionToWorld(fromVect);
			//        Math3D.GetRotation(out axis, out radians, fromVect, toVect);

			//        if ((axis.X > 0 && radians > 0) || (axis.X < 0 && radians < 0))
			//        {
			//            if (angularVelocity.X > 0)
			//            {
			//                angularVelocity.X = 0;
			//                modifiedAngularVelocity = true;
			//            }

			//            //body.NewtonBody.AddTorque(new Vector3D(-RESTORETORQUE, 0, 0));
			//            body.NewtonBody.AddTorque(body.DirectionToWorld(new Vector3D(-restoreTorqueX * Math.Abs(radians), 0, 0)));
			//        }
			//        else if ((axis.X > 0 && radians < 0) || (axis.X < 0 && radians > 0))
			//        {
			//            if (angularVelocity.X < 0)
			//            {
			//                angularVelocity.X = 0;
			//                modifiedAngularVelocity = true;
			//            }

			//            //body.NewtonBody.AddTorque(new Vector3D(RESTORETORQUE, 0, 0));
			//            body.NewtonBody.AddTorque(body.DirectionToWorld(new Vector3D(restoreTorqueX * Math.Abs(radians), 0, 0)));
			//        }

			//        #endregion

			//        if (modifiedAngularVelocity)
			//        {
			//            body.Omega = angularVelocity;
			//        }

			//        #endregion
			//    }

			//    if (!body.Override2DEnforcement_Translation)
			//    {
			//        #region Z Pos/Vel

			//        //Vector3D velocityWorld = body.DirectionToWorld(body.Velocity);
			//        Vector3D velocityWorld = body.Velocity;     // already in world coords
			//        bool modifiedVelocity = false;

			//        if (position.Z < 0)
			//        {
			//            if (velocityWorld.Z < 0)
			//            {
			//                velocityWorld.Z = 0;
			//                modifiedVelocity = true;
			//            }

			//            body.NewtonBody.AddForce(new Vector3D(0, 0, body.Mass * ZACCEL));       // Apply a constant acceleration until it hits zero
			//        }
			//        else if (position.Z > 0)
			//        {
			//            if (velocityWorld.Z > 0)
			//            {
			//                velocityWorld.Z = 0;
			//                modifiedVelocity = true;
			//            }

			//            body.NewtonBody.AddForce(new Vector3D(0, 0, body.Mass * ZACCEL * -1d));      // Apply a constant acceleration until it hits zero
			//        }

			//        if (modifiedVelocity)
			//        {
			//            //body.Velocity = body.DirectionFromWorld(velocityWorld);
			//            body.Velocity = velocityWorld;
			//        }

			//        #endregion
			//    }
			//}

			#endregion
		}
		private void FixPositionsAndVelocities_ORIG(Body body)
		{
			const double RETURNVELOCITY = 0d;
			double ZACCEL = 10;

			// Get the center of mass in world coords
			Point3D position = body.Position;

			if (_boundryMin != null)        // if min is non null, _boundryMax is also non null.  I don't want to waste the processor checking that each frame
			{
				#region Stay inside bounding box

				// Set the velocity going away to zero, apply a force
				//NOTE:  Position is the center of mass, so if the position is past the boundry, that means the body was travelling too fast for newton to bounce
				//it back (there is a wall at the boundry position).  So this logic is an extra helper

				Vector3D velocity = body.Velocity;    // already in world coords
				bool modifiedVelocity = false;

				#region X

				if (position.X < _boundryMin.Value.X)
				{
					if (velocity.X < 0)
					{
						velocity.X = RETURNVELOCITY;
						modifiedVelocity = true;
					}

					body.AddForce(new Vector3D(body.Mass * ZACCEL, 0, 0));       // Apply a constant acceleration until it hits zero
				}
				else if (position.X > _boundryMax.Value.X)
				{
					if (velocity.X > 0)
					{
						velocity.X = -RETURNVELOCITY;
						modifiedVelocity = true;
					}

					body.AddForce(new Vector3D(body.Mass * ZACCEL * -1, 0, 0));       // Apply a constant acceleration until it hits zero
				}

				#endregion
				#region Y

				if (position.Y < _boundryMin.Value.Y)
				{
					if (velocity.Y < 0)
					{
						velocity.Y = RETURNVELOCITY;
						modifiedVelocity = true;
					}

					body.AddForce(new Vector3D(0, body.Mass * ZACCEL, 0));       // Apply a constant acceleration until it hits zero
				}
				else if (position.Y > _boundryMax.Value.Y)
				{
					if (velocity.Y > 0)
					{
						velocity.Y = -RETURNVELOCITY;
						modifiedVelocity = true;
					}

					body.AddForce(new Vector3D(0, body.Mass * ZACCEL * -1, 0));       // Apply a constant acceleration until it hits zero
				}

				#endregion
				#region Z

				if (position.Z < _boundryMin.Value.Z)
				{
					if (velocity.Z < 0)
					{
						velocity.Z = RETURNVELOCITY;
						modifiedVelocity = true;
					}

					body.AddForce(new Vector3D(0, 0, body.Mass * ZACCEL));       // Apply a constant acceleration until it hits zero
				}
				else if (position.Z > _boundryMax.Value.Z)
				{
					if (velocity.Z > 0)
					{
						velocity.Z = -RETURNVELOCITY;
						modifiedVelocity = true;
					}

					body.AddForce(new Vector3D(0, 0, body.Mass * ZACCEL * -1));       // Apply a constant acceleration until it hits zero
				}

				#endregion

				if (modifiedVelocity)
				{
					body.Velocity = velocity;        // already in world coords
				}

				#endregion
			}

			#region Force2D

			//if (_shouldForce2D)
			//{
			//    if (!body.Override2DEnforcement_Rotation)
			//    {
			//        #region Angular Pos/Vel

			//        //body.NewtonBody.AddTorque(new Vector3D(.01, 0, 0));       // pulls back (front comes up, rear goes down)
			//        //body.NewtonBody.AddTorque(new Vector3D(0, .1, 0));    // creates a roll to the right (left side goes up, right side goes down)


			//        Vector3D angularVelocity = body.Omega;        // Omega seems to be angular velocity
			//        bool modifiedAngularVelocity = false;

			//        //const double RESTORETORQUE = 15d;     // TODO:  look at the inertia tensor for the axis I want to apply torque to, and do and calculate to make a constant accel
			//        const double RESTORETORQUE = .25d;     // TODO:  look at the inertia tensor for the axis I want to apply torque to, and do and calculate to make a constant accel

			//        double massMatrixLength = body.MassMatrix.m_I.Length;
			//        double restoreTorqueX = RESTORETORQUE * body.MassMatrix.m_Mass * (1 / (body.MassMatrix.m_I.X / massMatrixLength));
			//        double restoreTorqueY = RESTORETORQUE * body.MassMatrix.m_Mass * (1 / (body.MassMatrix.m_I.Y / massMatrixLength));

			//        //double restoreTorqueX = RESTORETORQUE;     // pulling values from the mass matrix seemed to cause the engine to corrupt.  See if there's a problem casting that structure
			//        //double restoreTorqueY = RESTORETORQUE;

			//        //TODO:  Dampen the angluar velocidy if the object is very close to zero and the angular speed is small.  Currently, the object has
			//        //	a very slight wobble indefinately

			//        #region X

			//        Vector3D fromVect = new Vector3D(1, 0, 0);
			//        Vector3D toVect = body.DirectionToWorld(fromVect);
			//        Vector3D axis;
			//        double radians;
			//        Math3D.GetRotation(out axis, out radians, fromVect, toVect);

			//        if ((axis.Y > 0 && radians > 0) || (axis.Y < 0 && radians < 0))
			//        {
			//            if (angularVelocity.Y > 0)
			//            {
			//                angularVelocity.Y = 0;
			//                modifiedAngularVelocity = true;
			//            }

			//            //body.NewtonBody.AddTorque(new Vector3D(0, -RESTORETORQUE, 0));
			//            //body.NewtonBody.AddTorque(body.DirectionToWorld(new Vector3D(0, -RESTORETORQUE, 0)));     // apply torque seems to want world coords
			//            body.NewtonBody.AddTorque(body.DirectionToWorld(new Vector3D(0, -restoreTorqueY * Math.Abs(radians), 0)));
			//        }
			//        else if ((axis.Y > 0 && radians < 0) || (axis.Y < 0 && radians > 0))
			//        {
			//            if (angularVelocity.Y < 0)
			//            {
			//                angularVelocity.Y = 0;
			//                modifiedAngularVelocity = true;
			//            }

			//            //body.NewtonBody.AddTorque(new Vector3D(0, RESTORETORQUE, 0));
			//            body.NewtonBody.AddTorque(body.DirectionToWorld(new Vector3D(0, restoreTorqueY * Math.Abs(radians), 0)));
			//        }

			//        #endregion
			//        #region Y

			//        fromVect = new Vector3D(0, 1, 0);
			//        toVect = body.DirectionToWorld(fromVect);
			//        Math3D.GetRotation(out axis, out radians, fromVect, toVect);

			//        if ((axis.X > 0 && radians > 0) || (axis.X < 0 && radians < 0))
			//        {
			//            if (angularVelocity.X > 0)
			//            {
			//                angularVelocity.X = 0;
			//                modifiedAngularVelocity = true;
			//            }

			//            //body.NewtonBody.AddTorque(new Vector3D(-RESTORETORQUE, 0, 0));
			//            body.NewtonBody.AddTorque(body.DirectionToWorld(new Vector3D(-restoreTorqueX * Math.Abs(radians), 0, 0)));
			//        }
			//        else if ((axis.X > 0 && radians < 0) || (axis.X < 0 && radians > 0))
			//        {
			//            if (angularVelocity.X < 0)
			//            {
			//                angularVelocity.X = 0;
			//                modifiedAngularVelocity = true;
			//            }

			//            //body.NewtonBody.AddTorque(new Vector3D(RESTORETORQUE, 0, 0));
			//            body.NewtonBody.AddTorque(body.DirectionToWorld(new Vector3D(restoreTorqueX * Math.Abs(radians), 0, 0)));
			//        }

			//        #endregion

			//        if (modifiedAngularVelocity)
			//        {
			//            body.Omega = angularVelocity;
			//        }

			//        #endregion
			//    }

			//    if (!body.Override2DEnforcement_Translation)
			//    {
			//        #region Z Pos/Vel

			//        //Vector3D velocityWorld = body.DirectionToWorld(body.Velocity);
			//        Vector3D velocityWorld = body.Velocity;     // already in world coords
			//        bool modifiedVelocity = false;

			//        if (position.Z < 0)
			//        {
			//            if (velocityWorld.Z < 0)
			//            {
			//                velocityWorld.Z = 0;
			//                modifiedVelocity = true;
			//            }

			//            body.NewtonBody.AddForce(new Vector3D(0, 0, body.Mass * ZACCEL));       // Apply a constant acceleration until it hits zero
			//        }
			//        else if (position.Z > 0)
			//        {
			//            if (velocityWorld.Z > 0)
			//            {
			//                velocityWorld.Z = 0;
			//                modifiedVelocity = true;
			//            }

			//            body.NewtonBody.AddForce(new Vector3D(0, 0, body.Mass * ZACCEL * -1d));      // Apply a constant acceleration until it hits zero
			//        }

			//        if (modifiedVelocity)
			//        {
			//            //body.Velocity = body.DirectionFromWorld(velocityWorld);
			//            body.Velocity = velocityWorld;
			//        }

			//        #endregion
			//    }
			//}

			#endregion
		}

		#endregion
	}

	#region Class: WorldBase

	public abstract class WorldBase : IDisposable
	{
		#region Struct: IntersectionPoint

		/// <summary>
		/// This is returned when they do a ray cast
		/// </summary>
		public struct IntersectionPoint
		{
			public Body Body;
			public Point3D ContactPoint;
			public Vector3D Normal;
		}

		#endregion

		#region Events

		#region BodyLeaveWorld

		private EventHandler<BodyLeaveWorldArgs> _bodyLeaveWorld = null;
		public event EventHandler<BodyLeaveWorldArgs> BodyLeaveWorld
		{
			add
			{
				if (_bodyLeaveWorld == null)
				{
					_newtonBodyLeaveWorld = new Newton.NewtonBodyLeaveWorld(InvokeBodyLeaveWorld);
					Newton.NewtonSetBodyLeaveWorldEvent(_handle, _newtonBodyLeaveWorld);
				}

				_bodyLeaveWorld += value;
			}
			remove
			{
				_bodyLeaveWorld -= value;

				if (_bodyLeaveWorld == null)
				{
					_newtonBodyLeaveWorld = null;
					Newton.NewtonSetBodyLeaveWorldEvent(_handle, _newtonBodyLeaveWorld);
				}
			}
		}

		private Newton.NewtonBodyLeaveWorld _newtonBodyLeaveWorld = null;
		private void InvokeBodyLeaveWorld(IntPtr newtonBody, int threadIndex)
		{
			OnBodyLeaveWorld(new BodyLeaveWorldArgs(ObjectStorage.Instance.GetBody(newtonBody), threadIndex));
		}

		protected virtual void OnBodyLeaveWorld(BodyLeaveWorldArgs e)
		{
			if (_bodyLeaveWorld != null)
			{
				_bodyLeaveWorld(this, e);
			}

			//	I don't want to remove from the object store based on this event.  Whoever disposed it should take care of that
		}

		#endregion

		#region SetPerformanceClock

		//	I don't get this one
		//    NEWTON_API void NewtonSetPerformanceClock (const NewtonWorld* const newtonWorld, NewtonGetTicksCountCallback callback);

		//private EventHandler<CGetTicksCountArgs> _setPerformanceClock = null;
		//public event EventHandler<CGetTicksCountArgs> SetPerformanceClock
		//{
		//    add
		//    {
		//        throw new ApplicationException("finish this");

		//        if (_setPerformanceClock == null)
		//        {
		//            _newtonSetPerformanceClock = new Newton.NewtonGetTicksCountCallback(InvokeSetPerformanceClock);
		//        }
		//    }
		//    remove
		//    {
		//        throw new ApplicationException("finish this");
		//    }
		//}

		//private uint InvokeSetPerformanceClock()
		//{
		//    throw new ApplicationException("finish this");
		//}

		//#region Class: GetTicksCountArgs

		//public class GetTicksCountArgs : EventArgs
		//{
		//    public GetTicksCountArgs(uint ticks)
		//    {
		//        this.Ticks = ticks;
		//    }

		//    public uint Ticks
		//    {
		//        get;
		//        private set;
		//    }
		//}

		//#endregion

		#endregion

		#region CollisionIslandUpdate

		private EventHandler<CollisionIslandUpdateArgs> _collisionIslandUpdate = null;
		public event EventHandler<CollisionIslandUpdateArgs> CollisionIslandUpdate
		{
			add
			{
				if (_collisionIslandUpdate == null)
				{
					_newtonIslandUpdate = new Newton.NewtonIslandUpdate(InvokeCollisionIslandUpdate);
					Newton.NewtonSetIslandUpdateEvent(_handle, _newtonIslandUpdate);
				}

				_collisionIslandUpdate += value;
			}
			remove
			{
				_collisionIslandUpdate -= value;

				if (_collisionIslandUpdate == null)
				{
					_newtonIslandUpdate = null;
					Newton.NewtonSetIslandUpdateEvent(_handle, _newtonIslandUpdate);
				}
			}
		}

		private Newton.NewtonIslandUpdate _newtonIslandUpdate = null;
		private int InvokeCollisionIslandUpdate(IntPtr newtonWorld, IntPtr islandHandle, int bodyCount)
		{
			CollisionIsland island = new CollisionIsland(islandHandle, bodyCount);		//	I don't see the need to pass the world handle

			CollisionIslandUpdateArgs e = new CollisionIslandUpdateArgs(island);
			OnCollisionIslandUpdate(e);

			return e.ShouldHandle ? 1 : 0;
		}

		protected virtual void OnCollisionIslandUpdate(CollisionIslandUpdateArgs e)
		{
			if (_collisionIslandUpdate != null)
			{
				_collisionIslandUpdate(this, e);
			}
		}

		#endregion

		#region CollisionHullDestroyed

		private EventHandler<CollisionHullDestroyedArgs> _collisionHullDestroyed = null;
		public event EventHandler<CollisionHullDestroyedArgs> CollisionHullDestroyed
		{
			add
			{
				if (_collisionHullDestroyed == null)
				{
					_newtonCollisionHullDestroyed = new Newton.NewtonCollisionDestructor(InvokeCollisionHullDestroyed);
					Newton.NewtonSetCollisionDestructor(_handle, _newtonCollisionHullDestroyed);
				}

				_collisionHullDestroyed += value;
			}
			remove
			{
				_collisionHullDestroyed -= value;

				if (_collisionHullDestroyed == null)
				{
					_newtonCollisionHullDestroyed = null;
					Newton.NewtonSetCollisionDestructor(_handle, _newtonCollisionHullDestroyed);
				}
			}
		}

		private Newton.NewtonCollisionDestructor _newtonCollisionHullDestroyed = null;
		private void InvokeCollisionHullDestroyed(IntPtr newtonWorld, IntPtr collision)
		{
			//	I assume it's for this world (I'm pretty much only set up for one world anyway)
			OnCollisionHullDestroyed(new CollisionHullDestroyedArgs(this, ObjectStorage.Instance.GetCollisionHull(collision)));
		}

		protected virtual void OnCollisionHullDestroyed(CollisionHullDestroyedArgs e)
		{
			if (_collisionHullDestroyed != null)
			{
				_collisionHullDestroyed(this, e);
			}

			//	I don't want to remove from the object store based on this event.  Whoever disposed it should take care of that
		}

		#endregion

		#endregion

		#region Constructor

		protected WorldBase()
		{
			_handle = Newton.NewtonCreate();
		}

		#endregion

		#region IDisposable Members

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (_handle != IntPtr.Zero)
				{
					Newton.NewtonDestroy(_handle);
					_handle = IntPtr.Zero;
				}
			}
		}

		#endregion

		#region Public Properties

		private IntPtr _handle;
		public IntPtr Handle
		{
			get
			{
				return _handle;
			}
		}

		//TODO:  See if I need to tell newton about this?
		private bool _isPaused = false;
		public bool IsPaused
		{
			get
			{
				return _isPaused;
			}
			set
			{
				_isPaused = value;
			}
		}

		public int Version
		{
			get
			{
				return Newton.NewtonWorldGetVersion();
			}
		}

		public int FloatSize
		{
			get
			{
				return Newton.NewtonWorldFloatSize();
			}
		}

		public bool UseMultiThreadSolverOnSingleIsland
		{
			get
			{
				int retVal = Newton.NewtonGetMultiThreadSolverOnSingleIsland(_handle);
				return retVal == 1;		//	1 is enabled, 0 is disabled
			}
			set
			{
				Newton.NewtonSetMultiThreadSolverOnSingleIsland(_handle, value ? 1 : 0);
			}
		}

		public int ThreadCount
		{
			get
			{
				return Newton.NewtonGetThreadsCount(_handle);
			}
			set
			{
				Newton.NewtonSetThreadsCount(_handle, value);
			}
		}
		public int MaxThreadCount
		{
			get
			{
				return Newton.NewtonGetMaxThreadsCount(_handle);
			}
		}

		/// <summary>
		/// Use this if you think newton and ObjectStorage are out of sync
		/// </summary>
		public int BodyCount
		{
			get
			{
				return Newton.NewtonWorldGetBodyCount(_handle);
			}
		}

		public int ConstraintCount
		{
			get
			{
				return Newton.NewtonWorldGetConstraintCount(_handle);
			}
		}

		#endregion

		#region Public Methods

		public void SetMemorySystem()
		{
			Newton.NewtonSetMemorySystem(null, null);
		}

		public int GetMemoryUsed()
		{
			return Newton.NewtonGetMemoryUsed();
		}

		public void DestroyAllBodies()
		{
			Newton.NewtonDestroyAllBodies(_handle);
		}

		public void InvalidateCache()
		{
			Newton.NewtonInvalidateCache(_handle);
		}

		//NOTE:  Use Adaptive for games (exact is slow)
		public void SetSolverModel(SolverModel model)
		{
			Newton.NewtonSetSolverModel(_handle, (int)model);
		}

		//NOTE:  Keep this one exact, adaptive is 10% faster, but can have strange side effects
		public void SetFrictionModel(FrictionModel model)
		{
			Newton.NewtonSetFrictionModel(_handle, (int)model);
		}

		//NOTE:  The description is unclear of what is best (I'm guessing zero?)
		public void SetPlatformArchitecture(PlatformArchitecture mode)
		{
			Newton.NewtonSetPlatformArchitecture(_handle, (int)mode);
		}
		public string GetPlatformArchitecture()
		{
			char[] retVal = new char[256];
			Newton.NewtonGetPlatformArchitecture(_handle, retVal);

			return new string(retVal).Replace("\0", "");
		}

		public uint ReadPerformanceTicks(uint performanceEntry)
		{
			return Newton.NewtonReadPerformanceTicks(_handle, performanceEntry);
		}
		public uint ReadThreadPerformanceTicks(uint threadIndex)
		{
			return Newton.NewtonReadThreadPerformanceTicks(_handle, threadIndex);
		}

		//	Locks the world's thread for your pleasure (only needed during a callback)
		public void WorldCriticalSectionLock()
		{
			Newton.NewtonWorldCriticalSectionLock(_handle);
		}
		public void WorldCriticalSectionUnlock()
		{
			Newton.NewtonWorldCriticalSectionUnlock(_handle);
		}

		public void SetMinimumFrameRate(double frameRate)
		{
			Newton.NewtonSetMinimumFrameRate(_handle, Convert.ToSingle(frameRate));
		}

		/// <summary>
		/// Default is a -100 to 100 for x y z
		/// </summary>
		public void SetWorldSize(Point3D minPoint, Point3D maxPoint)
		{
			Newton.NewtonSetWorldSize(_handle, new NewtonVector3(minPoint).Vector, new NewtonVector3(maxPoint).Vector);
		}

		public virtual List<IntersectionPoint> RayCast(Point3D startPoint, Point3D endPoint)
		{
			//NOTE:  I'm exposing a bit of a dumbed down version of what newton's ray cast can do.  By exposing events, newton gives a lot of flexibility
			//for the user to ignore certain hits, which could be quite an optimization.  So if that's needed make an overload (just directly call newton again,
			//no need to mess with this overload)
			//
			//This overload just returns what intersects, and lets the caller poke through the results.  Maybe not as fast, but easier to use

			SortedList<float, IntersectionPoint> retVal = new SortedList<float, IntersectionPoint>();

			//	This is an anonymous method that gets called for each body hit
			//NOTE:  Newton doesn't appear to return the closest hit first, so I need to let it find all matches anyway, and store the results sorted by distance
			Newton.NewtonWorldRayFilterCallback filterCallback = delegate(IntPtr body, float[] hitNormal, int collisionID, IntPtr userData, float percentAlongLine)
				{
					IntersectionPoint intersect;
					intersect.Body = ObjectStorage.Instance.GetBody(body);
					intersect.ContactPoint = ((endPoint - startPoint) * percentAlongLine).ToPoint();
					intersect.Normal = new NewtonVector3(hitNormal).ToVectorWPF();

					while ((retVal.ContainsKey(percentAlongLine)))		//	every once in a great while, I'm getting a dupe key exception
					{
						percentAlongLine += .0001f;
					}
					retVal.Add(percentAlongLine, intersect);

					return 1f;		//	returning 1 won't let it stop early
				};

			//	Prefilter hands you AABB's, so you have a chance to cancel certain bodies, but I'll just let everything through
			Newton.NewtonWorldRayPrefilterCallback prefilterCallback = null;		//TODO:  make sure it's ok to pass in null

			//NOTE:  Even though the method raises events, it's not a fire and forget.  The method won't finish until the ray is exhausted, or the event listener says to stop early
			Newton.NewtonWorldRayCast(_handle, new NewtonVector3(startPoint).Vector, new NewtonVector3(endPoint).Vector, filterCallback, IntPtr.Zero, prefilterCallback);

			//	Exit Function
			return new List<IntersectionPoint>(retVal.Values);
		}

		#endregion
		#region Internal Methods

		//	These are called directly from the body class.  They don't have meaning in this base class, but derived classes may care
		internal virtual void BodyCreated(Body body)
		{
		}
		internal virtual void BodyDisposed(Body body)
		{
		}

		#endregion
		#region Protected Methods

		//	Don't use both Update and CollisionUpdate, just one or the other (I think CollisionUpdate is only for detection, doesn't do all physics?)
		protected void Update(double timeStep)
		{
			Newton.NewtonUpdate(_handle, Convert.ToSingle(timeStep));
		}
		protected void CollisionUpdate()
		{
			Newton.NewtonCollisionUpdate(_handle);
		}

		#endregion

		//TODO:  Implement this when I need it.  It's like a ray cast, but uses a hull (basically returns anything that collides with that hull)
		//NEWTON_API int NewtonWorldConvexCast (const NewtonWorld* const newtonWorld, const dFloat* const matrix, const dFloat* const target, const NewtonCollision* shape, dFloat* const hitParam, void* const userData, NewtonWorldRayPrefilterCallback prefilter, NewtonWorldConvexCastReturnInfo* info, int maxContactsCount, int threadIndex);

		//TODO:  Implement these if the need arises
		//NEWTON_API void NewtonWorldSetDestructorCallBack (const NewtonWorld* const newtonWorld, NewtonDestroyWorld destructor);
		//NEWTON_API NewtonDestroyWorld NewtonWorldGetDestructorCallBack (const NewtonWorld* const newtonWorld);

		//TODO:  Implement this if I really need to (If I do, I can just expose a property in c# that returns object.  No need for newton to get involved)
		//NEWTON_API void NewtonWorldSetUserData (const NewtonWorld* const newtonWorld, void* const userData);
		//NEWTON_API void* NewtonWorldGetUserData (const NewtonWorld* const newtonWorld);

		//TODO:  Implement these if I need to (I already store the bodies in ObjectStorage, so I'd only need to query newton directly while debugging if I thought they were out of sync)
		//NEWTON_API void NewtonWorldForEachBodyDo (const NewtonWorld* const newtonWorld, NewtonBodyIterator callback);
		//NEWTON_API void NewtonWorldForEachJointDo (const NewtonWorld* const newtonWorld, NewtonJointIterator callback, void* const userData);
		//NEWTON_API void NewtonWorldForEachBodyInAABBDo (const NewtonWorld* const newtonWorld, const dFloat* const p0, const dFloat* const p1, NewtonBodyIterator callback, void* const userData);

		//TODO:  Implement this when I know more about it
		//NEWTON_API void NewtonSetDestroyBodyByExeciveForce (const NewtonWorld* const newtonWorld, NewtonDestroyBodyByExeciveForce callback); 
	}

	#endregion

	#region Enum: SolverModel

	public enum SolverModel
	{
		ExactMode = 0,
		AdaptativeMode = 1,
		LinearMode_2Passes = 2,
		LinearMode_3Passes = 3,
		LinearMode_4Passes = 4,
		LinearMode_5Passes = 5,
		LinearMode_6Passes = 6,
		LinearMode_7Passes = 7,
		LinearMode_8Passes = 8,
		LinearMode_9Passes = 9
	}

	#endregion
	#region Enum: FrictionModel

	public enum FrictionModel
	{
		ExactModel = 0,
		AdaptativeModel = 1
	}

	#endregion
	#region Enum: PlatformArchitecture

	public enum PlatformArchitecture
	{
		ForceHardware = 0,
		FloatingPointEnhancement = 1,
		BestHardwareSetting = 2,
	}

	#endregion

	#region Class: BodyLeaveWorldArgs

	public class BodyLeaveWorldArgs : EventArgs
	{
		public BodyLeaveWorldArgs(Body body, int threadIndex)
		{
			this.Body = body;
			this.ThreadIndex = threadIndex;
		}

		public Body Body
		{
			get;
			private set;
		}
		public int ThreadIndex
		{
			get;
			private set;
		}
	}

	#endregion
	#region Class: CollisionIslandUpdateArgs

	public class CollisionIslandUpdateArgs : EventArgs
	{
		public CollisionIslandUpdateArgs(CollisionIsland island)
		{
			this.Island = island;
			this.ShouldHandle = true;
		}

		public CollisionIsland Island
		{
			get;
			private set;
		}

		/// <summary>
		/// Gives the listeners of the event a chance to skip physics calculations on this island of bodies (for this frame only)
		/// </summary>
		public bool ShouldHandle
		{
			get;
			set;
		}
	}

	#endregion
	#region Class: CollisionHullDestroyedArgs

	public class CollisionHullDestroyedArgs : EventArgs
	{
		public CollisionHullDestroyedArgs(WorldBase world, CollisionHull collisionHull)
		{
			this.World = world;
			this.CollisionHull = collisionHull;
		}

		public WorldBase World
		{
			get;
			private set;
		}

		public CollisionHull CollisionHull
		{
			get;
			private set;
		}
	}

	#endregion
	#region Class: WorldUpdatingArgs

	public class WorldUpdatingArgs : EventArgs
	{
		public WorldUpdatingArgs(double elapsedTime)
		{
			this.ElapsedTime = elapsedTime;
		}

		public double ElapsedTime
		{
			get;
			private set;
		}
	}

	#endregion
}
