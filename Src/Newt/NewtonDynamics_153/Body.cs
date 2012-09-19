using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using Game.HelperClasses;
using Game.Newt.HelperClasses;
using Game.Newt.NewtonDynamics_153.Api;

namespace Game.Newt.NewtonDynamics_153
{
	[TypeConverter(typeof(BodyConverter))]
	public abstract class Body : DependencyObject, INotifyPropertyChanged, IDisposable, IComparable //,ISupportInitialize
	{
		#region Declaration Section

		private CBody _body;
		private bool _isInitialised;
		//private int _initialising;
		private World _world;
		private readonly MatrixTransform3D _visualMatrix = new MatrixTransform3D();
		private CollisionMask _collision;
		//private bool _changingTransform;

		protected bool _overrideCenterOfMass;

		private long _token;

		#endregion

		#region Constructor

		public Body()
		{
			_token = TokenGenerator.Instance.NextToken();
		}

		#endregion

		#region INotifyPropertyChanged Members

		/// <summary>
		/// Notify that the property changed
		/// </summary>
		/// <param name="propertyName">Property name</param>
		protected void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		protected virtual Matrix3D OnGetCollisionMatrix()
		{
			return Matrix3D.Identity;
		}

		private void body_ApplyForceAndTorque(object sender, CApplyForceAndTorqueEventArgs e)
		{
			if (ApplyForce != null)
			{
				ApplyForce(this, new BodyForceEventArgs(this));
			}

			//	Cache velocity
			Vector3D prevVelocity = _velocityCached;
			_velocityCached = _body.Velocity;

			//	Cache acceleration
			if (_wasAccelerationCachedCalled)
			{
				double elapsedTime = _world.TimeStep;
				if (elapsedTime > 0)
				{
					_accelerationCached = (_velocityCached - prevVelocity) / elapsedTime;
				}
				else
				{
					_accelerationCached = new Vector3D(0, 0, 0);
				}
			}
		}

		protected abstract void CalculateMass(float mass);

		public event PropertyChangedEventHandler PropertyChanged;
		public event BodyForceEventHandler ApplyForce;
		public event BodyTransformEventHandler Transforming;

		#endregion
		#region IDisposable Members

		public void Dispose()
		{
			if (_body != null)
			{
				_body.Destructor -= _body_Destructor;

				if (_isInitialised)
				{
					_body.Dispose();

					_isInitialised = false;
				}
				_body = null;
			}
		}

		#endregion
		#region ISupportInitialize Members

		/*

        public void BeginInit()
        {
            _initialising++;
        }

        public void EndInit()
        {
            _initialising--;
        }

        */

		#endregion
		#region IComparable Members

		public int CompareTo(object obj)
		{
			Body cast = obj as Body;
			if (cast == null)
			{
				//	I'm greater than null
				return 1;
			}

			if (_token < cast.Token)
			{
				return -1;
			}
			else if (_token > cast.Token)
			{
				return 1;
			}
			else
			{
				return 0;
			}
		}

		#endregion

		#region Public Properties

		public bool IsInitialised
		{
			get { return _isInitialised; }
		}

		/// <summary>
		/// This is something I added in case a body wanted to be remembered out of a crowd
		/// </summary>
		public long Token
		{
			get
			{
				return _token;
			}
		}

		public World World
		{
			get { return _world; }
		}

		public CollisionMask CollisionMask
		{
			get
			{
				return _collision;
			}
			set
			{
				//	Store what was passed in
				_collision = value;
				if (_collision == null)
				{
					_collision = new NullCollision(_world);
				}

				//	Tell the api body about this
				_body.Collision = _collision.NewtonCollision;
			}
		}

		public int MaterialGroupID
		{
			get
			{
				return _body.MaterialGroupID;
			}
			set
			{
				_body.MaterialGroupID = value;
			}
		}

		public CBody NewtonBody
		{
			get { return _body; }
		}

		public Matrix3D VisualMatrix
		{
			get
			{
				return _visualMatrix.Matrix;
				//Matrix3D matrix = _body.Matrix;// GetTransformMatrix(_body.Matrix, _offset);
				//matrix.Prepend(OnGetCollisionMatrix());
				//return matrix;
			}
			set
			{
				_visualMatrix.Matrix = value;
			}
		}
		// These are also known as Inertia Tensor
		public MassMatrix MassMatrix
		{
			get
			{
				VerifyInitialised();
				return _body.MassMatrix;
			}
			set
			{
				VerifyInitialised();
				_body.MassMatrix = value;
			}
		}
		public MassMatrix InverseMassMatrix
		{
			get
			{
				VerifyInitialised();
				return _body.InvMassMatrix;
			}
		}

		public bool IsPaused
		{
			get
			{
				VerifyInitialised();
				return _body.SleepingState;
			}
		}

		/// <summary>
		/// This is in world coords
		/// WARNING: Only valid when called from within ApplyForceAndTorque event (Newt 2 lets it get called any time)
		/// </summary>
		public Vector3D Velocity
		{
			get
			{
				VerifyInitialised();
				return _body.Velocity;
			}
			set
			{
				VerifyInitialised();
				_body.Velocity = value;
			}
		}
		private Vector3D _velocityCached = new Vector3D(0, 0, 0);
		/// <summary>
		/// This is the velocity from the last apply torque event (this class always listens to the event, even if the caller doesn't)
		/// </summary>
		public Vector3D VelocityCached
		{
			get
			{
				return _velocityCached;
			}
		}

		private bool _wasAccelerationCachedCalled = false;		//	I don't want to calculate the acceleration unless it has been requested.  So the first request will be zero, but all others will work
		private Vector3D _accelerationCached = new Vector3D(0, 0, 0);
		/// <summary>
		/// This one is calculated between the 2nd to last frame and last frame
		/// </summary>
		public Vector3D AccelerationCached
		{
			get
			{
				_wasAccelerationCachedCalled = true;
				return _accelerationCached;
			}
		}

		public Vector3D Omega
		{
			get
			{
				VerifyInitialised();
				return _body.Omega;
			}
			set
			{
				VerifyInitialised();
				_body.Omega = value;
			}
		}

		public float LinearDamping
		{
			get
			{
				return _body.LinearDamping;
			}
			set
			{
				_body.LinearDamping = value;
			}
		}
		public Vector3D AngularDamping
		{
			get
			{
				return _body.AngularDamping;
			}
			set
			{
				_body.AngularDamping = value;
			}
		}

		// The world can be told to force everything to stay in 2D.  These can be set to true to allow the body to go in 3D
		private bool _override2DEnforcement_Rotation = false;
		public bool Override2DEnforcement_Rotation
		{
			get
			{
				return _override2DEnforcement_Rotation;
			}
			set
			{
				_override2DEnforcement_Rotation = value;
			}
		}
		private bool _override2DEnforcement_Translation = false;
		public bool Override2DEnforcement_Translation
		{
			get
			{
				return _override2DEnforcement_Translation;
			}
			set
			{
				_override2DEnforcement_Translation = value;
			}
		}

		#region DependencyProperty: Transform

		public static readonly DependencyProperty TransformProperty = DependencyProperty.Register("Transform", typeof(Transform3D), typeof(Body), new PropertyMetadata(OnTransformChanged));

		private static void OnTransformChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			Body body = (Body)d;

			if (body._isInitialised)
			{
				if (body._body != null) //TODO: shouldn't need to check (happens after world.Dispose() is called
				{
					body._body.Matrix = ((Transform3D)e.NewValue).Value;
					body.UpdateVisualMatrix();
				}
			}
		}

		public Transform3D Transform
		{
			get { return (Transform3D)GetValue(TransformProperty); }
			set { SetValue(TransformProperty, value); }
		}

		#endregion
		#region DependencyProperty: ApplyGravity

		public static readonly DependencyProperty ApplyGravityProperty = DependencyProperty.Register("ApplyGravity", typeof(bool), typeof(Body), new PropertyMetadata(true));

		public bool ApplyGravity
		{
			get { return (bool)GetValue(ApplyGravityProperty); }
			set { SetValue(ApplyGravityProperty, value); }
		}

		#endregion
		#region DependencyProperty: Mass

		//NOTE:  This used to default to 1.0, then when I was setting it to 1.0, the change event never fired, and the mass matrix
		// was still identity.  So I'm defaulting it to something very small so that the mass matrix is at least more accurate if they
		// never set the mass
		public static readonly DependencyProperty MassProperty = DependencyProperty.Register("Mass", typeof(float), typeof(Body), new PropertyMetadata(.0001f, OnMassPropertyChanged));

		private static void OnMassPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			Body body = (Body)d;
			float mass = (float)e.NewValue;
			if (mass < 0)
				throw new ArgumentException("The Body Mass can not be less then zero.");

			if (body._isInitialised)
				body.CalculateMass(mass);
		}

		public float Mass
		{
			get { return (float)GetValue(MassProperty); }
			set { SetValue(MassProperty, value); }
		}

		#endregion
		#region DependencyProperty: CenterOfMass

		public static readonly DependencyProperty CenterOfMassProperty = DependencyProperty.Register("CenterOfMass", typeof(Point3D), typeof(Body), new PropertyMetadata(new Point3D(), OnCenterOfMassPropertyChanged));

		private static void OnCenterOfMassPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			Body body = (Body)d;
			Point3D center = (Point3D)e.NewValue;

			body._overrideCenterOfMass = true;

			if (body._isInitialised)
				body._body.CentreOfMass = (Vector3D)center;
		}

		/// <summary>
		/// This is in local coords
		/// </summary>
		public Point3D CenterOfMass
		{
			get
			{
				return (Point3D)GetValue(CenterOfMassProperty);
			}
			set
			{
				SetValue(CenterOfMassProperty, value);
			}
		}

		#endregion
		#region DependencyProperty: AutoPause

		public static readonly DependencyProperty AutoPauseProperty = DependencyProperty.Register("AutoPause", typeof(bool), typeof(Body), new PropertyMetadata(false, OnAutoPauseChanged));

		private static void OnAutoPauseChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			Body body = (Body)d;
			bool value = (bool)e.NewValue;

			if (body.NewtonBody != null)
				body.NewtonBody.AutoFreeze = value;
		}

		public bool AutoPause
		{
			get { return (bool)GetValue(AutoPauseProperty); }
			set { SetValue(AutoPauseProperty, value); }
		}

		#endregion
		#region DependencyProperty: Joint

		public static readonly DependencyProperty JointProperty = DependencyProperty.Register("Joint", typeof(Joint), typeof(Body), new PropertyMetadata(OnJointPropertyChanged));

		private static void OnJointPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			Body body = (Body)d;

			body.VerifyNotInitialised("Joint");
		}

		public Joint Joint
		{
			get { return (Joint)GetValue(JointProperty); }
			set { SetValue(JointProperty, value); }
		}

		#endregion

		#endregion
		#region Protected Properties

		protected Transform3D VisualTransform
		{
			get { return _visualMatrix; }
		}

		#endregion

		#region Public Methods

		public void Initialise(World world)
		{
			if (_isInitialised)
				return;

			if (world == null) throw new ArgumentNullException("world");

			_world = world;

			_collision = OnInitialise(world);

			if (_collision == null)
			{
				_collision = new NullCollision(_world);
			}

			_body = new CBody(_collision.NewtonCollision);
			_body.Destructor += _body_Destructor;
			_body.AutoFreeze = this.AutoPause;
			_body.UserData = this;
			_body.Matrix = _visualMatrix.Matrix;

			OnInitialiseEnd();  // this might adjust the _body.Matrix property

			// not initialised yet, nothing should be called on property changed event
			this.Transform = new MatrixTransform3D(_body.Matrix);

			_body.SetTransform += body_setTransform;
			_body.ApplyForceAndTorque += body_ApplyForceAndTorque;

			CalculateMass(this.Mass);

			world.AddBody(this);
			_isInitialised = true;
		}

		public void Pause()
		{
			VerifyInitialised();

			if (_body != null)
				_body.Freeze();
		}
		public void UnPause()
		{
			VerifyInitialised();

			if (_body != null)
				_body.Unfreeze();
		}

		/// <summary>
		/// This is a helper method that transforms the position in local coords to the position in world coords
		/// </summary>
		public Vector3D PositionToWorld(Vector3D positionLocal)
		{
			//NOTE:  This transform method acts different based on whether a vector or point is passed in
			return this.VisualMatrix.Transform(positionLocal.ToPoint()).ToVector();
		}
		public Point3D PositionToWorld(Point3D positionLocal)
		{
			//NOTE:  This transform method acts different based on whether a vector or point is passed in
			return this.VisualMatrix.Transform(positionLocal);
		}

		public Vector3D PositionFromWorld(Vector3D positionWorld)
		{
			Matrix3D matrix = this.VisualMatrix;
			matrix.Invert();

			//NOTE:  This transform method acts different based on whether a vector or point is passed in
			return matrix.Transform(positionWorld.ToPoint()).ToVector();
		}
		public Point3D PositionFromWorld(Point3D positionWorld)
		{
			Matrix3D matrix = this.VisualMatrix;
			matrix.Invert();

			//NOTE:  This transform method acts different based on whether a vector or point is passed in
			return matrix.Transform(positionWorld);
		}

		/// <summary>
		/// This is a helper method that transforms the direction in local coords to the direction in world coords
		/// </summary>
		public Vector3D DirectionToWorld(Vector3D directionLocal)
		{
			//NOTE:  This transform method acts different based on whether a vector or point is passed in
			return this.VisualMatrix.Transform(directionLocal);
		}
		public Point3D DirectionToWorld(Point3D directionLocal)
		{
			//NOTE:  This transform method acts different based on whether a vector or point is passed in
			return this.VisualMatrix.Transform(directionLocal.ToVector()).ToPoint();
		}

		public Vector3D DirectionFromWorld(Vector3D directionWorld)
		{
			Matrix3D matrix = this.VisualMatrix;
			matrix.Invert();

			//NOTE:  This transform method acts different based on whether a vector or point is passed in
			return matrix.Transform(directionWorld);
		}
		public Point3D DirectionFromWorld(Point3D directionWorld)
		{
			Matrix3D matrix = this.VisualMatrix;
			matrix.Invert();

			//NOTE:  This transform method acts different based on whether a vector or point is passed in
			return matrix.Transform(directionWorld.ToVector()).ToPoint();
		}

		/*
		public static Matrix3D GetTransformMatrix(Matrix3D m, Vector3D offset)
		{
			m.OffsetX += offset.X;
			m.OffsetY += offset.Y;
			m.OffsetZ += offset.Z;
			return m;
		}
		*/

		#endregion
		#region Internal Methods

		internal protected void VerifyInitialised()
		{
			if (!IsInitialised)
				throw new InvalidOperationException("The Body has not been initialised yet.");
		}
		internal protected void VerifyInitialised(string bodyName)
		{
			if (!IsInitialised)
				throw new InvalidOperationException(string.Format("The {0} Body has not been initialised yet.", bodyName));
		}

		internal protected void VerifyNotInitialised(string propertyName)
		{
			if (IsInitialised)
				throw new InvalidOperationException(string.Format("The {0}.{1} property can only be set when initialising the Body.", this.GetType().Name, propertyName));
		}

		#endregion
		#region Protected Methods

		protected abstract CollisionMask OnInitialise(World world);

		protected virtual void OnInitialiseEnd()
		{
		}

		protected void UpdateVisualMatrix()
		{
			Matrix3D matrix = _body.Matrix;

			// change the matrix by the hull modifier (to update the visual to the correct position and scale as indicated by the Hull Modifier.)
			matrix.Prepend(OnGetCollisionMatrix());

			_visualMatrix.Matrix = matrix;

			OnPropertyChanged("VisualMatrix");
		}

		#endregion

		#region Event Listeners

		private void _body_Destructor(object sender, CBodyDestructorEventArgs e)
		{
			_body.Destructor -= _body_Destructor;
			_body = null;
		}

		/// <summary>
		/// Handle Matrix changed callback
		/// </summary>
		/// <param name="sender">Body</param>
		/// <param name="e">Event arguments</param>
		private void body_setTransform(object sender, CSetTransformEventArgs e)
		{
			Matrix3D matrix = e.Matrix;

			// first raise the Body's event
			BodyTransformEventArgs bodyArgs = null;
			if (Transforming != null)
			{
				bodyArgs = new BodyTransformEventArgs(matrix);
				Transforming(this, bodyArgs);
			}

			// if there's a world event and the body did not handle it, raise the world event.
			if (_world.CanRaiseBodyTransforming || ((bodyArgs != null) && !bodyArgs.Handled))
			{
				if (bodyArgs == null)
					bodyArgs = new BodyTransformEventArgs(matrix);

				_world.RaiseBodyTransforming(this, bodyArgs);
			}

			// if there was an event what changed the args, update the body's matrix
			if ((bodyArgs != null) && bodyArgs.Changed)
			{
				matrix = bodyArgs.Matrix;
				_body.Matrix = matrix;
			}

			//_changingTransform = true;
			this.Transform = new MatrixTransform3D(matrix);
			//_changingTransform = false;
		}

		#endregion
	}

	#region BodyForceEvent Delegate/Args

	public delegate void BodyForceEventHandler(Body sender, BodyForceEventArgs e);

	public class BodyForceEventArgs : EventArgs
	{
		#region Constructor

		public BodyForceEventArgs(Body body)
		{
			_body = body;
		}

		#endregion

		#region Public Properties

		private readonly Body _body;
		public Body Body
		{
			get { return _body; }
		}

		private double? _elapsedTime = null;
		public double ElapsedTime
		{
			get
			{
				if (_elapsedTime == null)
				{
					_elapsedTime = _body.World.TimeStep;
				}

				return _elapsedTime.Value;
			}
		}

		#endregion

		#region Public Methods

		public void AddForce(Vector3D force, bool alignedToBody)
		{
			if (alignedToBody)
				force = _body.NewtonBody.Matrix.Transform(force);

			_body.NewtonBody.AddForce(force);
		}
		public void AddForce(Vector3D force)
		{
			_body.NewtonBody.AddForce(force);
		}

		public void AddTorque(Vector3D torque)
		{
			_body.NewtonBody.AddTorque(torque);
		}

		/// <summary>
		/// The positionOnBody needs to be in world coords
		/// </summary>
		public void AddForceAtPoint(Vector3D force, Vector3D positionOnBody)
		{
			// I need to calculate this offset in world coords, because the force is in world coords
			Vector3D offsetFromMassWorld = positionOnBody - _body.PositionToWorld(_body.CenterOfMass).ToVector();

			Vector3D translationForce, torque;
			Math3D.SplitForceIntoTranslationAndTorque(out translationForce, out torque, offsetFromMassWorld, force);

			AddForce(translationForce);
			AddTorque(torque);
		}

		/// <summary>
		/// The positionOnBody needs to be in world coords
		/// </summary>
		public void AddImpulse(Vector3D deltaVelocity, Vector3D positionOnBody)
		{
			_body.NewtonBody.AddImpulse(deltaVelocity, positionOnBody);
		}

		#endregion
	}

	#endregion
	#region BodyTransformEvent Delegate/Args

	public delegate void BodyTransformEventHandler(Body sender, BodyTransformEventArgs e);

	public class BodyTransformEventArgs : EventArgs
	{
		private Matrix3D _matrix;
		private Vector3D _translation;
		private Vector3D _rotation;
		private bool _matrixDirty;
		private bool _decomposeDirty;
		private bool _handled;

		internal bool Changed;

		public Matrix3D Matrix
		{
			get
			{
				ComposeMatrix();
				return _matrix;
			}
			set
			{
				_matrix = value;
				_matrixDirty = true;
				Changed = true;
			}
		}

		public BodyTransformEventArgs(Matrix3D matrix)
		{
			_matrix = matrix;
			_matrixDirty = true;
		}

		public Vector3D Translation
		{
			get
			{
				DecomposeMatrix();
				return _translation;
			}
			set
			{
				if (_translation != value)
				{
					_translation = value;
					_decomposeDirty = true;
					Changed = true;
				}
			}
		}
		public double TranslationX
		{
			get
			{
				DecomposeMatrix();
				return _translation.X;
			}
			set
			{
				DecomposeMatrix();
				if (_translation.X != value)
				{
					_translation.X = value;
					_decomposeDirty = true;
					Changed = true;
				}
			}
		}
		public double TranslationY
		{
			get
			{
				DecomposeMatrix();
				return _translation.Y;
			}
			set
			{
				DecomposeMatrix();
				if (_translation.Y != value)
				{
					_translation.Y = value;
					_decomposeDirty = true;
					Changed = true;
				}
			}
		}
		public double TranslationZ
		{
			get
			{
				DecomposeMatrix();
				return _translation.Z;
			}
			set
			{
				DecomposeMatrix();
				if (_translation.Z != value)
				{
					_translation.Z = value;
					_decomposeDirty = true;
					Changed = true;
				}
			}
		}

		public Vector3D Rotation
		{
			get
			{
				DecomposeMatrix();
				return _rotation;
			}
			set
			{
				if (_rotation != value)
				{
					_rotation = value;
					_decomposeDirty = true;
					Changed = true;
				}
			}
		}
		public double RotationX
		{
			get
			{
				DecomposeMatrix();
				return _rotation.X;
			}
			set
			{
				DecomposeMatrix();
				if (_rotation.X != value)
				{
					_rotation.X = value;
					_decomposeDirty = true;
					Changed = true;
				}
			}
		}
		public double RotationY
		{
			get
			{
				DecomposeMatrix();
				return _rotation.Y;
			}
			set
			{
				DecomposeMatrix();
				if (_rotation.Y != value)
				{
					_rotation.Y = value;
					_decomposeDirty = true;
					Changed = true;
				}
			}
		}
		public double RotationZ
		{
			get
			{
				DecomposeMatrix();
				return _rotation.Z;
			}
			set
			{
				DecomposeMatrix();
				if (_rotation.Z != value)
				{
					_rotation.Z = value;
					_decomposeDirty = true;
					Changed = true;
				}
			}
		}

		public bool Handled
		{
			get { return _handled; }
			set { _handled = value; }
		}

		private void ComposeMatrix()
		{
			if (_decomposeDirty)
			{
				_matrix = Matrix3D.Identity;
				_matrix.Translate(_translation);
				_matrix.Prepend(Math3D.CreateYawPitchRollMatrix(_rotation));
				_decomposeDirty = false;
			}
		}
		private void DecomposeMatrix()
		{
			if (_matrixDirty)
			{
				Vector3D scale;
				Math3D.MatrixDecompose(_matrix, out _translation, out scale, out _rotation, false);
				_matrixDirty = false;
			}
		}
	}

	#endregion
	#region BodyEvent Delegate

	public delegate void BodyEventHandler(Body sender, EventArgs e);

	#endregion
}