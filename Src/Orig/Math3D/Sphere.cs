using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Orig.Math3D
{
	/// <summary>
	/// This is both a sphere, and a plane aligned cube (aligned to any arbitrary plane).  My old project always called this sphere,
	/// so I'll keep that name
	/// </summary>
	/// <remarks>
	/// Another way of thinkng about this class is an enforcer of the DoubleVector (keeps the two axiis orthoganal) - with a radius
	/// and centerpoint added for convinience.
	/// 
	/// You could use this class for either case, but I think it's overkill to separate it (you'll probably use both features at the same
	/// time)
	/// 
	/// I want to keep this thing as close to a theoretical object as possible.  An obvious wrapper to this would be a ball with mass
	/// and forces, acceleration, velocity.  If you wanted to go nuts, you could add angular momentum.
	/// 
	/// A good use for this is to be the heart of a sprite, or a kee joint of a character, or any point with size, orientation, and location
	/// (but no physics constraints).
	/// </remarks>
	public class Sphere
	{
		#region Declaration Section

		/// <summary>
		/// The center point
		/// </summary>
		private MyVector _position = null;

		/// <summary>
		/// The radius of this sphere (if you want to think of this as a cube, you may want this to be the length of one
		/// of the sides)
		/// </summary>
		private double _radius = 0;

		/// <summary>
		/// This is the direction I am currently facing
		/// </summary>
		/// <remarks>
		/// This will stay null until someone actually requests it.  After that, I will always keep it synced (because they are
		/// then holding a live pointer, and expect it to be up to date)
		/// </remarks>
		private DoubleVector _dirFacing = null;
		private bool _dirFacingRequested = false;

		/// <summary>
		/// This is my original orientation
		/// </summary>
		/// <remarks>
		/// I made this protected so that derived classes can properly clone themselves
		/// </remarks>
		private DoubleVector _origDirFacing = null;

		/// <summary>
		/// This holds the true rotation (_dirFacing is just a byproduct)
		/// </summary>
		private MyQuaternion _rotation = null;

		/// <summary>
		/// This helps me know when to normalize the quaternion (I don't want to normalize every time the call
		/// Rotate, because that would be expensive)
		/// </summary>
		private int _numRotations = 0;

		#endregion

		#region Constructor

		/// <summary>
		/// Use this overload when you don't care about the direction facing
		/// </summary>
		public Sphere(MyVector position, double radius)
			: this(position, new DoubleVector(new MyVector(1, 0, 0), new MyVector(0, 1, 0)), radius) { }

		/// <summary>
		/// Use this overload when you don't care about the position and radius
		/// </summary>
		public Sphere(DoubleVector origDirectionFacing)
			: this(new MyVector(0, 0, 0), origDirectionFacing, 0) { }

		/// <summary>
		/// This sets up everything in one shot
		/// </summary>
		public Sphere(MyVector position, DoubleVector origDirectionFacing, double radius)
		{
			//	Store what was passed in
			_position = position;
			_radius = radius;
			_origDirFacing = origDirectionFacing;

			//	Force the original direction facing to be unit vectors
			_origDirFacing.Standard.BecomeUnitVector();
			_origDirFacing.Orth.BecomeUnitVector();

			//	Create a default quaternion (I don't want to instantiate dirfacing until it's requested)
			_rotation = new MyQuaternion(new MyVector(0, 0, 0), 0);
		}

		/// <summary>
		/// This overload should only be used during a clone.  I simply trust the values passed to me
		/// </summary>
		protected Sphere(MyVector position, DoubleVector origDirectionFacing, MyQuaternion rotation, double radius)
		{
			_position = position;
			_radius = radius;
			_origDirFacing = origDirectionFacing;
			_rotation = rotation;
		}

		#endregion

		#region Public Properties

		public MyVector Position
		{
			get
			{
				return _position;
			}
		}

		/// <remarks>
		/// Some of my derived classes needs to know when radius changes
		/// </remarks>
		public virtual double Radius
		{
			get
			{
				return _radius;
			}
			set
			{
				_radius = value;
			}
		}

		public DoubleVector DirectionFacing
		{
			get
			{
				if (!_dirFacingRequested)
				{
					//	From this point on, _dirFacing will be a live pointer
					_dirFacingRequested = true;
					_dirFacing = new DoubleVector(new MyVector(), new MyVector());		//	The values don't matter, they will get blown away in a second.  (the sync function doesn't instantiate, just populates)
					SyncDirFacing();
				}

				return _dirFacing;
			}
		}

		/// <summary>
		/// WARNING:  This is the original orientation, and should never be modified
		/// </summary>
		public DoubleVector OriginalDirectionFacing
		{
			get
			{
				return _origDirFacing;
			}
		}

		/// <summary>
		/// You can use this to rotate vectors that are stored in body coords
		/// </summary>
		public MyQuaternion Rotation
		{
			get
			{
				return _rotation;
			}
		}

		/// <summary>
		/// This is the matrix that describes how to rotate the sphere from original direction facing to current direction facing
		/// NOTE: You will not receive a live pointer, just a snapshot of the current rotation
		/// </summary>
		protected MyMatrix3 RotationMatrix
		{
			get
			{
				return _rotation.ToMatrix3FromUnitQuaternion();
			}
			set
			{
				_rotation.FromRotationMatrix(value);
				_numRotations = 0;		//	FromMatrix insures that the quaternion is normalized

				if (_dirFacingRequested)
				{
					SyncDirFacing();
				}
			}
		}

		#endregion

		#region Public Methods

		public virtual Sphere Clone()
		{
			return new Sphere(_position.Clone(), _origDirFacing.Clone(), _rotation.Clone(), _radius);
		}

		/// <summary>
		/// WARNING: Don't rotate my direction facing outside of me, we will fall out of sync
		/// </summary>
		public virtual void RotateAroundAxis(MyVector rotateAround, double radians)
		{
			//	Avoid mathmatical drift
			_numRotations++;
			if (_numRotations > 10000)
			{
				_numRotations = 0;
				_rotation.BecomeUnitQuaternion();
			}

			//	Make a quaternion that holds the axis and radians passed in
			MyQuaternion intermediate = new MyQuaternion(rotateAround, radians);

			//	Apply that to my existing one
			_rotation = MyQuaternion.Multiply(intermediate, _rotation);		//	it seems counterintuitive to multiply them in this order, but testing shows it doesn't work the other way (and reading some articles confirms the order)

			if (_dirFacingRequested)
			{
				SyncDirFacing();
			}
		}

		public void ResetRotation()
		{
			//	I don't want to destroy the instances, just reset the values underneath
			if (_dirFacingRequested)
			{
				_dirFacing.Standard.StoreNewValues(_origDirFacing.Standard);
				_dirFacing.Orth.StoreNewValues(_origDirFacing.Orth);
			}

			MyQuaternion newRotation = new MyQuaternion(new MyVector(0, 0, 0), 0);
			_rotation.X = newRotation.X;
			_rotation.Y = newRotation.Y;
			_rotation.Z = newRotation.Z;
			_rotation.W = newRotation.W;
		}

		#endregion

		#region Private Methods

		private void SyncDirFacing()
		{
			_dirFacing.Standard.StoreNewValues(_rotation.GetRotatedVector(_origDirFacing.Standard, true));
			_dirFacing.Orth.StoreNewValues(_rotation.GetRotatedVector(_origDirFacing.Orth, true));
		}

		#endregion
	}
}
