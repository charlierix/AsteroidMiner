using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Orig.Math3D
{
	/// <summary>
	/// This class represents a solid ball, and has standard momentum as well as angular momentum.
	/// 
	/// You can then hit me with forces along any vector relative to my center (of position), and I will turn those into translational
	/// and rotational momentum.
	/// </summary>
	/// <remarks>
	/// The postion and direction that my base ball represents is the center of position.  When you hit the base's External and Internal
	/// force properties, those are translational only (never any angular force).  When you hit my force methods, I split force into rotation
	/// and translation
	/// </remarks>
	public class SolidBall : TorqueBall
	{
		#region Constructor

		public SolidBall(MyVector position, DoubleVector origDirectionFacing, double radius, double mass)
			: base(position, origDirectionFacing, radius, mass) { }

		public SolidBall(MyVector position, DoubleVector origDirectionFacing, double radius, double mass, MyVector boundingBoxLower, MyVector boundingBoxUpper)
			: base(position, origDirectionFacing, radius, mass, 1, 1, 1, boundingBoxLower, boundingBoxUpper) { }

		/// <summary>
		/// This overload is used if you plan to do collisions
		/// </summary>
		public SolidBall(MyVector position, DoubleVector origDirectionFacing, double radius, double mass, double elasticity, double kineticFriction, double staticFriction, MyVector boundingBoxLower, MyVector boundingBoxUpper)
			: base(position, origDirectionFacing, radius, mass, elasticity, kineticFriction, staticFriction, boundingBoxLower, boundingBoxUpper) { }

		/// <summary>
		/// This one is used to assist with the clone method (especially for my derived classes)
		/// </summary>
		/// <param name="usesBoundingBox">Just pass in what you have</param>
		/// <param name="boundingBoxLower">Set this to null if bounding box is false</param>
		/// <param name="boundingBoxUpper">Set this to null if bounding box is false</param>
		protected SolidBall(MyVector position, DoubleVector origDirectionFacing, MyQuaternion rotation, double radius, double mass, double elasticity, double kineticFriction, double staticFriction, bool usesBoundingBox, MyVector boundingBoxLower, MyVector boundingBoxUpper)
			: base(position, origDirectionFacing, rotation, radius, mass, elasticity, kineticFriction, staticFriction, usesBoundingBox, boundingBoxLower, boundingBoxUpper) { }

		#endregion

		#region Public Properties

		public override double Mass
		{
			get
			{
				return base.Mass;
			}
			set
			{
				base.Mass = value;
				ResetInertiaTensorAndCenterOfMass();
			}
		}

		public override double Radius
		{
			get
			{
				return base.Radius;
			}
			set
			{
				base.Radius = value;
				ResetInertiaTensorAndCenterOfMass();
			}
		}

		#endregion

		#region Public Methods

		public override Sphere Clone()
		{
			//	I want a copy of the bounding box, not a clone (everything else gets cloned)
			return new SolidBall(this.Position.Clone(), this.OriginalDirectionFacing.Clone(), this.Rotation.Clone(), this.Radius, this.Mass, this.Elasticity, this.KineticFriction, this.StaticFriction, this.UsesBoundingBox, this.BoundryLower, this.BoundryUpper);
		}
		public SolidBall CloneSolidBall()
		{
			return this.Clone() as SolidBall;
		}

		#endregion
		#region Protected Methods

		protected override void ResetInertiaTensorAndCenterOfMass()
		{
			double momentOfInertia = (2d / 5d) * base.Mass * (base.Radius * base.Radius);

			MyMatrix3 inertiaTensor = new MyMatrix3();

			inertiaTensor.M11 = momentOfInertia;
			inertiaTensor.M12 = 0;
			inertiaTensor.M13 = 0;

			inertiaTensor.M21 = 0;
			inertiaTensor.M22 = momentOfInertia;
			inertiaTensor.M23 = 0;

			inertiaTensor.M31 = 0;
			inertiaTensor.M32 = 0;
			inertiaTensor.M33 = momentOfInertia;

			base.InertialTensorBody = inertiaTensor;
			
			//	I will let the center of mass stay 0,0,0
		}

		#endregion
	}
}
