using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Orig.Math3D
{
	public abstract class TorqueBall : Ball
	{
		#region Declaration Section

		/// <summary>
		/// This represents a rigid body's resistance to rotational change (sort of like the moment of inertia of the body, but
		/// around the center of mass instead of just the current axis of rotation)
		/// </summary>
		/// <remarks>
		/// Here is a cool definition of an inertia tensor that I read somewhere:
		///	It is a scaling factor between angular momentum and angular velocity
		/// 
		/// This is actually the inverse of the inertia tensor.  First find the inertia tensor (the way you get that difers based on
		/// the makup of your body).  Once you have the inertia tensor, then take Matrix3.Inverse
		/// </remarks>
		private MyMatrix3 _inertialTensorBodyInverse = null;		//	inverse of InertialTensorBody (body frame)
		private MyMatrix3 _inertialTensorBody = null;

		/// <summary>
		/// The direction is the axis of rotation, the magnitude is the speed of rotation
		/// </summary>
		/// <remarks>
		/// NOTE: This is around base.Position + CenterOfMass, not base.Position
		/// </remarks>
		private MyVector _angularVelocity = new MyVector(0, 0, 0);

		//	These get reset whenever the timer is called (applied to base.Position + CenterOfMass)
		private MyVector _externalTorque = new MyVector(0, 0, 0);
		private MyVector _internalTorque = new MyVector(0, 0, 0);

		//	The momentum around base.Position + CenterOfMass
		private MyVector _angularMomentum = new MyVector(0, 0, 0);

		private MyVector _centerOfMass = new MyVector(0, 0, 0);

		//	These are used by the timer functions to remember some state
		private MyQuaternion _savedRotation = null;
		private double _elapsedTime = 0;		//	the ball also has a private by this name.  it should probably be protected, but it just feels cleaner for me to have my own

		#endregion

		#region Constructor

		/// <summary>
		/// Use this overload for standard instantiations by the outside user
		/// </summary>
		protected TorqueBall(MyVector position, DoubleVector origDirectionFacing, double radius, double mass)
			: base(position, origDirectionFacing, radius, mass)
		{
			ResetInertiaTensorAndCenterOfMass();
		}

		/// <summary>
		/// Use this overload for standard instantiations by the outside user
		/// </summary>
		protected TorqueBall(MyVector position, DoubleVector origDirectionFacing, double radius, double mass, double elasticity, double kineticFriction, double staticFriction, MyVector boundingBoxLower, MyVector boundingBoxUpper)
			: base(position, origDirectionFacing, radius, mass, elasticity, kineticFriction, staticFriction, boundingBoxLower, boundingBoxUpper)
		{
			ResetInertiaTensorAndCenterOfMass();
		}

		/// <summary>
		/// Use this overload for clone
		/// </summary>
		protected TorqueBall(MyVector position, DoubleVector origDirectionFacing, MyQuaternion rotation, double radius, double mass, double elasticity, double kineticFriction, double staticFriction, bool usesBoundingBox, MyVector boundingBoxLower, MyVector boundingBoxUpper)
			: base(position, origDirectionFacing, rotation, radius, mass, elasticity, kineticFriction, staticFriction, usesBoundingBox, boundingBoxLower, boundingBoxUpper)
		{
			ResetInertiaTensorAndCenterOfMass();
		}

		#endregion

		#region Public Properties

		public MyVector AngularVelocity
		{
			get
			{
				return _angularVelocity;
			}
		}

		public MyVector AngularMomentum
		{
			get
			{
				return _angularMomentum;
			}
		}

		public MyVector ExternalTorque
		{
			get
			{
				return _externalTorque;
			}
		}
		public MyVector InternalTorque
		{
			get
			{
				return _internalTorque;
			}
		}

		/// <summary>
		/// This is relative to base.Position
		/// </summary>
		public MyVector CenterOfMass
		{
			get
			{
				return _centerOfMass;
			}
		}

		public MyMatrix3 InertialTensorBody
		{
			get
			{
				return _inertialTensorBody;
			}
			protected set
			{
				_inertialTensorBody = value;
				_inertialTensorBodyInverse = MyMatrix3.Inverse(_inertialTensorBody);
			}
		}
		public MyMatrix3 InertialTensorBodyInverse
		{
			get
			{
				return _inertialTensorBodyInverse;
			}
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// The vectors passed in are orientated to the world frame
		/// NOTE: This gets reset at the beginning of each timer tick
		/// </summary>
		/// <remarks>
		/// See Ball.ExternalForce's remarks for when you would use this as apposed to ApplyInternalForce()
		/// </remarks>
		/// <param name="offset">
		/// This is the displacement from my center where the force is applied (if you know this is always zero at compile time,
		/// just directly add force to base.ExternalForce instead)
		/// </param>
		/// <param name="force">This is the direction and strength of the force being applied</param>
		public void ApplyExternalForce(MyVector offset, MyVector force)
		{
			//	Split the force acting on me into rotational and translational
			MyVector translationForce, torque;
			SplitForceIntoTranslationAndTorque(out translationForce, out torque, _centerOfMass, offset, force);

			//System.Windows.Forms.MessageBox.Show(torque.X.ToString() + ", " + torque.Y.ToString() + ", " + torque.Z.ToString());

			//	Add to my torque
			_externalTorque.Add(torque);

			//	Add the translational force to my base
			this.ExternalForce.Add(translationForce);
		}
		/// <summary>
		/// The vectors passed in are orientated to the body frame
		/// NOTE: This gets reset at the beginning of each timer tick
		/// </summary>
		/// <remarks>
		/// See Ball.InternalForce's remarks for when you would use this as apposed to ApplyExternalForce()
		/// </remarks>
		/// <param name="offset">
		/// This is the displacement from my center where the force is applied (if you know this is always zero at compile time,
		/// just directly add force to base.InternalForce instead)
		/// </param>
		/// <param name="force">This is the direction and strength of the force being applied</param>
		public void ApplyInternalForce(MyVector offset, MyVector force)
		{
			//TODO: Convert forcePos from being relative to the center of position to being relative to the center of mass

			//	Split the force acting on me into rotational and translational
			MyVector translationForce, torque;
			SplitForceIntoTranslationAndTorque(out translationForce, out torque, _centerOfMass, offset, force);

			//	Add to my torque
			_internalTorque.Add(torque);

			//	Add the translational force to my base
			this.InternalForce.Add(translationForce);
		}

		/// <summary>
		/// Every frame, angular velocity is wiped out and recalculated based on angular momentum.  So this function actually
		/// sets angular momentum to produce the velocity passed in.
		/// </summary>
		/// <remarks>
		/// This function is the opposite of the calculation in ApplyTorque
		/// </remarks>
		public void SetAngularVelocity(MyVector angularVelocity)
		{
			//	Figure out the world frame's inertia tensor
			//	(Rotation * bodyInertialTensorInverse * Transposed Rotation)
			MyMatrix3 curRotation = base.RotationMatrix.Clone();

			MyMatrix3 worldInertiaTensor = MyMatrix3.Multiply(MyMatrix3.Multiply(curRotation, _inertialTensorBody), MyMatrix3.Transpose(curRotation));

			//	Now store the angular momentum required to generate this velocity
			_angularMomentum.StoreNewValues(MyMatrix3.Multiply(worldInertiaTensor, angularVelocity));
		}

		public override void StopBall()
		{
			_angularVelocity.X = 0;
			_angularVelocity.Y = 0;
			_angularVelocity.Z = 0;

			_angularMomentum.X = 0;
			_angularMomentum.Y = 0;
			_angularMomentum.Z = 0;

			base.StopBall();
		}

		public override void PrepareForNewTimerCycle()
		{
			_savedRotation = null;
			_elapsedTime = 0;

			_externalTorque.X = 0;
			_externalTorque.Y = 0;
			_externalTorque.Z = 0;

			_internalTorque.X = 0;
			_internalTorque.Y = 0;
			_internalTorque.Z = 0;

			//	Call the base
			base.PrepareForNewTimerCycle();
		}

		public override void TimerTestPosition(double elapsedTime)
		{
			base.TimerTestPosition(elapsedTime);

			//	Either remember the orig rotation, or restore to that orig rotation (as of PrepareForNew)
			if (_savedRotation == null)
			{
				_savedRotation = this.Rotation.Clone();
			}
			else
			{
				this.Rotation.StoreNewValues(_savedRotation);
			}

			//	Remember the elapsed time that was passed in
			_elapsedTime = elapsedTime;

			//	Figure out the new rotation (the body has been rotating at some velocity during the previous tick)
			if (!_angularVelocity.IsZero)
			{
				//	Figure out what the angle will be
				double timedAngle = _angularVelocity.GetMagnitude();
				timedAngle *= elapsedTime;

				if (!_centerOfMass.IsZero)
				{
					#region Rotate around center of mass

					//	Remember where the center of mass is in world coords
					MyVector cmRotated = this.Rotation.GetRotatedVector(_centerOfMass, true);
					MyVector cmWorld = this.Position + cmRotated;

					//	Get the opposite of the cm
					MyVector posRelativeToCM = cmRotated.Clone();
					posRelativeToCM.Multiply(-1d);

					//	Rotate the center of position around the center of mass
					posRelativeToCM.RotateAroundAxis(_angularVelocity, timedAngle);

					//	Now figure out the new center of position
					this.Position.X = cmWorld.X + posRelativeToCM.X;
					this.Position.Y = cmWorld.Y + posRelativeToCM.Y;
					this.Position.Z = cmWorld.Z + posRelativeToCM.Z;

					#endregion
				}

				//	Rotate myself
				this.RotateAroundAxis(_angularVelocity, timedAngle);
			}
		}

		public override void TimerFinish()
		{
			base.TimerFinish();

			//	Rotate the external torque and add to the internal torque (I should probably do this against saved rotation, because that's
			//	the orientation that the forces were applied to, but the base doesn't know that I've played with the rotation, and it has similiar
			//	logic.  So I will do what the base does)
			if (!_externalTorque.IsZero)
			{
				CombineExternalAndInternalTorques();
			}

			//	Apply the torque for the length of time passed in (getting a new angular velocity, to be applied on the
			//	next tick)
			//if (!_internalTorque.IsZero)		//	they may have added to angular momentum directly
			//{
			ApplyTorque(_elapsedTime);
			//}
		}

		#endregion
		#region Protected Methods

		protected abstract void ResetInertiaTensorAndCenterOfMass();

		#endregion

		#region Private Methods

		private static void SplitForceIntoTranslationAndTorque(out MyVector translationForce, out MyVector torque, MyVector centerOfMass, MyVector offset, MyVector force)
		{
			//	The offset passed in is relative to position.  I need it to be relative to the center of mass
			MyVector trueOffset = offset - centerOfMass;

			//	Torque is how much of the force is applied perpendicular to the radius
			torque = MyVector.Cross(trueOffset, force);

			//	I'm still not convinced this is totally right, but none of the articles I've read seem to do anything
			//	different
			translationForce = force.Clone();
		}

		/// <summary>
		/// This adds external torque to internal torque.  Before I do that, I need to rotate a copy of external torque
		/// so that it is the same orientation as the internal torque.
		/// </summary>
		private void CombineExternalAndInternalTorques()
		{
			//	My base's rotation quaternion already knows how to rotate from orig to dirfacing.  I need one that
			//	goes the other way
			MyQuaternion rotation = base.Rotation.Clone();
			rotation.W *= -1;

			//	Rotate the external to line up with the internal
			MyVector externalRotated = rotation.GetRotatedVector(_externalTorque, true);

			//	Now that it's aligned, I can just add it onto the internal
			_internalTorque.Add(externalRotated);
		}

		private void ApplyTorque(double elapsedTime)
		{
			//	Calculate the new angular momentum (current + (torque * time))
			MyVector newMomentum = _internalTorque.Clone();
			newMomentum.Multiply(elapsedTime);
			_angularMomentum.Add(newMomentum);

			//	Figure out the inverse of the world frame's inertia tensor
			//	(Rotation * bodyInertialTensorInverse * Transposed Rotation)
			MyMatrix3 curRotation = base.RotationMatrix.Clone();
			MyMatrix3 inverseWorldInertiaTensor = MyMatrix3.Multiply(MyMatrix3.Multiply(curRotation, _inertialTensorBodyInverse), MyMatrix3.Transpose(curRotation));

			//	Now all that's left is to figure out the new angular velocity
			_angularVelocity.StoreNewValues(MyMatrix3.Multiply(inverseWorldInertiaTensor, _angularMomentum));
		}

		/// <remarks>
		/// The other author just called this star (probably for vector star)
		/// </remarks>
		private static MyMatrix3 SkewSymmetric(MyVector vector)
		{
			MyMatrix3 retVal = new MyMatrix3();

			retVal.M11 = 0;
			retVal.M12 = vector.Z * -1;
			retVal.M13 = vector.Y;

			retVal.M21 = vector.Z;
			retVal.M22 = 0;
			retVal.M23 = vector.X * -1;

			retVal.M31 = vector.Y * -1;
			retVal.M32 = vector.X;
			retVal.M33 = 0;

			return retVal;
		}

		/// <summary>
		/// I only made this public so I could hook a tester to it
		/// </summary>
		public static void OrthonormalizeOrientation(MyMatrix3 orientation)
		{
			//	Do some crazy math (something about constraining 9 degrees of freedom of a matrix down to 3)
			MyVector x = new MyVector(orientation.M11, orientation.M21, orientation.M31);
			x.BecomeUnitVector();

			MyVector y = new MyVector(orientation.M12, orientation.M22, orientation.M32);		//	just store a temp variable into y (until I calculate z)

			MyVector z = MyVector.Cross(x, y);
			z.BecomeUnitVector();

			y = MyVector.Cross(z, x);
			y.BecomeUnitVector();

			//	Overwrite the matrix passed in
			orientation.M11 = x.X;
			orientation.M12 = y.X;
			orientation.M13 = z.X;

			orientation.M21 = x.Y;
			orientation.M22 = y.Y;
			orientation.M23 = z.Y;

			orientation.M31 = x.Z;
			orientation.M32 = y.Z;
			orientation.M33 = z.Z;
		}

		#endregion
	}
}
