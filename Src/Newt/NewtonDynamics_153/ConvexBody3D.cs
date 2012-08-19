using System.Collections.Generic;
using Game.Newt.NewtonDynamics_153.Api;
using System.Windows.Media.Media3D;
using System;
using System.Windows;

using Game.Newt.HelperClasses;

namespace Game.Newt.NewtonDynamics_153
{
    public class ConvexBody3D : Visual3DBodyBase, IMesh
	{
		#region Enum: CollisionShape

		public enum CollisionShape
		{
			UseModel = 0,
			Cube,
			Sphere,
			Capsule,
			Cylinder,
			Cone,
			ChamferCylinder
		}

		#endregion

		#region Declaration Section

		private CollisionShape _collisionShape = CollisionShape.UseModel;
		private double _collisionShapeRatio1;		//	either x or radius
		private double _collisionShapeRatio2;		//	either y or height
		private double _collisionShapeRatio3;		//	either z or not used

		private Matrix3D _modifierMatrix;
		private Vector3D _offset;

		#endregion

		#region Constructor

		public ConvexBody3D()
		{
		}

		public ConvexBody3D(World world, ModelVisual3D model)
			: base(world, model)
		{
			RecalculateCollisionMask();
		}

		public ConvexBody3D(World world, ModelVisual3D model, CollisionShape collisionShape, double ratioX, double ratioY, double ratioZ)
			: base(world, model)
		{
			//	Validate
			switch (collisionShape)
			{
				case CollisionShape.Cube:
				case CollisionShape.Sphere:
					break;

				default:
					throw new ArgumentException("This constructor overload only supports collision shapes of cube and sphere");
			}

			//	Store the definition of the collision shape
			_collisionShape = collisionShape;
			_collisionShapeRatio1 = ratioX;		//	reusing 3 doubles so I don't waste as much memory (it's not much, but makes me feel better)
			_collisionShapeRatio2 = ratioY;
			_collisionShapeRatio3 = ratioZ;

			//	Calculate the collision mask
			RecalculateCollisionMask();
		}

		public ConvexBody3D(World world, ModelVisual3D model, CollisionShape collisionShape, double radius, double height)
			: base(world, model)
		{
			//	Validate
			switch (collisionShape)
			{
				case CollisionShape.Capsule:
				case CollisionShape.ChamferCylinder:
				case CollisionShape.Cone:
				case CollisionShape.Cylinder:
					break;

				default:
					throw new ArgumentException("This constructor overload only supports collision shapes of cone, cylinder, capsule, chamfer cylinder");
			}

			//	Store the definition of the collision shape
			_collisionShape = collisionShape;
			_collisionShapeRatio1 = radius;
			_collisionShapeRatio2 = height;
			_collisionShapeRatio3 = -1d;

			//	Calculate the collision mask
			RecalculateCollisionMask();
		}

		#endregion

		#region IMesh Members

		public IList<Point3D> GetPoints()
		{
			if (this.CollisionMask != null)
			{
				List<Point3D> result = new List<Point3D>();

				foreach (ConvexCollisionMask collision in this.CollisionMask.Collisions)
				{
					AddPoints(collision, result);
				}

				return result;
			}
			else
				return null;
		}

		private static void AddPoints(ConvexCollisionMask collision, List<Point3D> points)
		{
			if (collision is IMesh)
			{
				IList<Point3D> newPoints = ((IMesh)collision).GetPoints();
				if (newPoints != null)
					points.AddRange(newPoints);
			}
			else
			{
				ConvextCollisionModifier modifier = (collision as ConvextCollisionModifier);
				if (modifier != null)
				{
					int i = points.Count; // starting index of new points
					AddPoints(modifier.CollisionMask, points);
					for (int count = points.Count; i < count; i++)
						points[i] = modifier.ModifierMatrix.Transform(points[i]);
				}
			}
		}

		#endregion

		#region Public Properties

		public Matrix3D ModifierMatrix
		{
			get { return _modifierMatrix; }
		}

		public new CompoundCollision CollisionMask
		{
			get { return base.CollisionMask as CompoundCollision; }
		}

		#region DependencyProperty: AlignToGeometryCenter

		public static readonly DependencyProperty AlignToGeometryCenterProperty = DependencyProperty.Register("AlignToGeometryCenter", typeof(Point3D?), typeof(ConvexBody3D), new PropertyMetadata(AlignToGeometryCenterChanged));

		private static void AlignToGeometryCenterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			ConvexBody3D body = (ConvexBody3D)d;

			body.VerifyNotInitialised("AlignToGeometryCenter");
		}

		public Point3D? AlignToGeometryCenter
		{
			get { return (Point3D?)GetValue(AlignToGeometryCenterProperty); }
			set { SetValue(AlignToGeometryCenterProperty, value); }
		}

		#endregion
		#region DependencyProperty: AlignToGeometryUVZ

		public static readonly DependencyProperty AlignToGeometryUVZProperty = DependencyProperty.Register("AlignToGeometryUVZ", typeof(Vector3D?), typeof(ConvexBody3D), new PropertyMetadata(AlignToGeometryUVZChanged));

		private static void AlignToGeometryUVZChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			ConvexBody3D body = (ConvexBody3D)d;

			body.VerifyNotInitialised("AlignToGeometryUVZ");
		}

		public Vector3D? AlignToGeometryUVZ
		{
			get { return (Vector3D?)GetValue(AlignToGeometryUVZProperty); }
			set { SetValue(AlignToGeometryUVZProperty, value); }
		}

		#endregion
		#region DependencyProperty: RotationAxis

		public static readonly DependencyProperty RotationAxisProperty = DependencyProperty.Register("RotationAxis", typeof(Vector3D?), typeof(ConvexBody3D), new PropertyMetadata(RotationAxisChanged));

		private static void RotationAxisChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			ConvexBody3D body = (ConvexBody3D)d;

			body.VerifyNotInitialised("RotationAxis");
		}

		public Vector3D RotationAxis
		{
			get { return (Vector3D)GetValue(RotationAxisProperty); }
			set { SetValue(RotationAxisProperty, value); }
		}

		#endregion

		#endregion

		#region Overrides

		protected override CollisionMask OnInitialise(Matrix3D initialMatrix)
		{
			_modifierMatrix = Math3D.GetScaleMatrix(ref initialMatrix);

			//	Not worrying about the collision here (because _collisionShape hasn't been set yet.  This method gets called
			//	from my base's constructor, so only world and model are known)
			return null;
		}

		protected override void OnInitialiseEnd()
		{
			if (AlignToGeometryCenter != null)
			{
				BoundingBox box = GeometryHelper.GetBoundingBox(this.GetPoints());

				Point3D newCenter = (Point3D)AlignToGeometryCenter;
				SetNewCenter(newCenter, box);
			}

			if (AlignToGeometryUVZ != null)
			{
				BoundingBox box = GeometryHelper.GetBoundingBox(this.GetPoints());

				Vector3D offset = (Vector3D)AlignToGeometryUVZ;
				offset.Y = 1 - offset.Y;
				offset.Z = 1 - offset.Z;
				Point3D newCenter = box.CenterPosFromFactor(offset) - (Vector3D)box.CenterPos;
				SetNewCenter(newCenter, box);
			}

			/*
			if (this.RotationAxis != new Vector3D())
			{
				AxisAngleRotation3D r = new AxisAngleRotation3D(this.RotationAxis, 90);
				RotateTransform3D t = new RotateTransform3D(r);
				OffsetCollisionMask(t.Value);
			}
			 */
		}

		protected override Matrix3D OnGetCollisionMatrix()
		{
			return _modifierMatrix * base.OnGetCollisionMatrix();
		}

		protected override void OffsetCollisionMask(Vector3D offset, bool offsetModel)
		{
			CompoundCollision c = this.CollisionMask;
			if (c != null)
				c.Translate(offset);

			base.OffsetCollisionMask(offset, offsetModel);
		}

		protected override void CalculateMass(float mass)
		{
			if (NewtonBody == null)
				return;

			InertialMatrix inertialMatrix;
			if (NewtonBody.Collision is CCollisionConvexPrimitives)
			{
				inertialMatrix = ((CCollisionConvexPrimitives)NewtonBody.Collision).CalculateInertialMatrix;
			}
			else if (NewtonBody.Collision is CCollisionComplexPrimitives)
			{
				inertialMatrix = ((CCollisionComplexPrimitives)NewtonBody.Collision).CalculateInertialMatrix;
			}
			else
			{
				throw new InvalidOperationException(Properties.Resources.MeshBody_CalculateMass);
			}

			this.NewtonBody.MassMatrix = new MassMatrix(mass, inertialMatrix.m_Inertia * mass);

			// shouldn't be initialised yet.
			if (!_overrideCenterOfMass)
			{
				this.CenterOfMass = (Point3D)inertialMatrix.m_Origin;
				_overrideCenterOfMass = false;
			}
		}

		#endregion

		#region Private Methods

		private void SetNewCenter(Point3D newCenter, BoundingBox box)
		{
			newCenter.Y *= -1;
			Point3D boxCenter = box.CenterPos;

			Vector3D offsetBy = ((Vector3D)newCenter - (Vector3D)boxCenter) - _offset;
			OffsetCollisionMask(offsetBy, this.Visual.Content != null);
			_offset += offsetBy;
		}

		private void RecalculateCollisionMask()
		{
			List<ConvexCollisionMask> collisions = new List<ConvexCollisionMask>();

			switch (_collisionShape)
			{
				case CollisionShape.Capsule:
				case CollisionShape.ChamferCylinder:
				case CollisionShape.Cone:
				case CollisionShape.Cube:
				case CollisionShape.Cylinder:
				case CollisionShape.Sphere:
					throw new ApplicationException("Unsupported CollisionShape: " + _collisionShape.ToString());

				case CollisionShape.UseModel:
					collisions = GetCollisions(Visual, _modifierMatrix);
					break;

				default:
					throw new ApplicationException("Unknown CollisionShape: " + _collisionShape.ToString());
			}

			//	Store it
			if (collisions.Count == 0)
			{
				//NOTE: Storing null will get intercepted, and store a NullCollision
				base.CollisionMask = null;		//	I can't call this.CollisionMask because it's shadowed
			}
			else if (collisions.Count == 1)
			{
				base.CollisionMask = collisions[0];
			}
			else
			{
				base.CollisionMask = new CompoundCollision(collisions);
			}
		}

		/// <summary>
		/// This returns convex collisions based on the geometry of the model
		/// </summary>
		private List<ConvexCollisionMask> GetCollisions(ModelVisual3D visual, Matrix3D initialMatrix)
		{
			List<ConvexCollisionMask> retVal = new List<ConvexCollisionMask>();
			AddCollisions(retVal, visual, initialMatrix);
			return retVal;
		}

		private void AddCollisions(List<ConvexCollisionMask> collisions, ModelVisual3D visual, Matrix3D parentMatrix)
		{
			Body body = World.GetBody(visual);

			if (body == null || body == this)
			{
				if (visual.Content != null)
				{
					AddCollisions(collisions, visual.Content, parentMatrix);
				}

				foreach (ModelVisual3D child in visual.Children)
				{
					Matrix3D m = child.Transform.Value * parentMatrix;

					AddCollisions(collisions, child, m);
				}
			}
		}
		private void AddCollisions(List<ConvexCollisionMask> collisions, Model3D model, Matrix3D parentMatrix)
		{
			if (model.Transform != null)
			{
				parentMatrix = model.Transform.Value * parentMatrix;
			}

			Model3DGroup models = (model as Model3DGroup);
			if (models != null)
			{
				//	The model passed in is actually a group.  Recurse
				foreach (Model3D m in models.Children)
				{
					AddCollisions(collisions, m, parentMatrix);
				}
			}
			else
			{
				CollisionMask collisionMask = null;
				GeometryModel3D geometryModel = (model as GeometryModel3D);
				if (geometryModel != null)
				{
					//	See if a collision mask is already assosiated with this geometry (will very likely come back null)
					collisionMask = World.GetCollisionMask(geometryModel.Geometry);

					if (!(collisionMask is NullCollision))		//	even nulls will fall into this if statement
					{
						if (collisionMask == null)
						{
							//	Create a new collision mask
							collisionMask = new GeometryCollisionMask3D();
							World.SetCollisionMask(geometryModel.Geometry, collisionMask);
						}
						else if (!(collisionMask is ConvexCollisionMask))
						{
							throw new InvalidOperationException("The GeometryModel3D already has a non Convex CollisionMaskMask attached.");
						}
					}
				}

				if (collisionMask != null)
				{
					collisionMask.Initialise(this.World);

					collisions.Add(new ConvextCollisionModifier((ConvexCollisionMask)collisionMask, parentMatrix));		//	why is a convex collision modifier needed?  what's wrong with the convex collision mask?
				}
			}
		}

		#endregion
    }
}
