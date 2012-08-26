using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using Game.Newt.AsteroidMiner2.ShipEditor;
using Game.Newt.HelperClasses;
using Game.Newt.NewtonDynamics;

namespace Game.Newt.AsteroidMiner2.ShipParts
{
	#region Class: FuelTankToolItem

	public class FuelTankToolItem : PartToolItemBase
	{
		#region Constructor

		public FuelTankToolItem(EditorOptions options)
			: base(options)
		{
			_visual2D = PartToolItemBase.GetVisual2D(this.Name, this.Description, options.EditorColors);
			this.TabName = PartToolItemBase.TAB_SHIPPART;
		}

		#endregion

		#region Public Properties

		public override string Name
		{
			get
			{
				return "Fuel Tank";
			}
		}
		public override string Description
		{
			get
			{
				return "Stores fuel";
			}
		}
		public override string Category
		{
			get
			{
				return PartToolItemBase.CATEGORY_CONTAINER;
			}
		}

		private UIElement _visual2D = null;
		public override UIElement Visual2D
		{
			get
			{
				return _visual2D;
			}
		}

		#endregion

		#region Public Methods

		public override PartDesignBase GetNewDesignPart()
		{
			return new FuelTankDesign(this.Options);
		}

		#endregion
	}

	#endregion
	#region Class: FuelTankDesign

	public class FuelTankDesign : PartDesignBase
	{
		#region Declaration Section

		internal const double RADIUSPERCENTOFSCALE = .25d;		//	when x scale is 1, the x radius will be this (x and y are the same)

		#endregion

		#region Constructor

		public FuelTankDesign(EditorOptions options)
			: base(options) { }

		#endregion

		#region Public Properties

		public override PartDesignAllowedScale AllowedScale
		{
			get
			{
				return PartDesignAllowedScale.XY_Z;
			}
		}
		public override PartDesignAllowedRotation AllowedRotation
		{
			get
			{
				return PartDesignAllowedRotation.X_Y_Z;
			}
		}

		private GeometryModel3D _geometry = null;
		public override Model3D Model
		{
			get
			{
				if (_geometry == null)
				{
					_geometry = CreateGeometry(false);
				}

				return _geometry;
			}
		}

		#endregion

		#region Public Methods

		public override Model3D GetFinalModel()
		{
			return CreateGeometry(true);
		}

		public override CollisionHull CreateCollisionHull(WorldBase world)
		{
			Transform3DGroup transform = new Transform3DGroup();
			//transform.Children.Add(new ScaleTransform3D(this.Scale));		//	it ignores scale
			transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90)));		//	the physics hull is along x, but dna is along z
			transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(this.Orientation)));
			transform.Children.Add(new TranslateTransform3D(this.Position.ToVector()));

			Vector3D scale = this.Scale;
			double radius = RADIUSPERCENTOFSCALE * (scale.X + scale.Y) * .5d;
			double height = scale.Z;

			if (height < radius * 2d)
			{
				//	Newton keeps the capsule caps spherical, but the visual scales them.  So when the height is less than the radius, newton
				//	make a sphere.  So just make a cylinder instead
				return CollisionHull.CreateChamferCylinder(world, 0, radius, height, transform.Value);
				//return CollisionHull.CreateCylinder(world, 0, radius, height, transform.Value);
			}
			else
			{
				//NOTE: The visual changes the caps around, but I want the physics to be a capsule
				return CollisionHull.CreateCapsule(world, 0, radius, height, transform.Value);
			}
		}

		#endregion

		#region Private Methods

		private GeometryModel3D CreateGeometry(bool isFinal)
		{
			//	Material
			MaterialGroup material = new MaterialGroup();
			DiffuseMaterial diffuse = new DiffuseMaterial(new SolidColorBrush(this.Options.WorldColors.FuelTank));
			this.MaterialBrushes.Add(new MaterialColorProps(diffuse, diffuse.Brush, this.Options.WorldColors.FuelTank));
			material.Children.Add(diffuse);
			SpecularMaterial specular = this.Options.WorldColors.FuelTankSpecular;
			this.MaterialBrushes.Add(new MaterialColorProps(specular));
			material.Children.Add(specular);

			if (!isFinal)
			{
				EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
				material.Children.Add(selectionEmissive);
				base.SelectionEmissives.Add(selectionEmissive);
			}

			//	Geometry Model
			GeometryModel3D retVal = new GeometryModel3D();
			retVal.Material = material;
			retVal.BackMaterial = material;

			int domeSegments = isFinal ? 2 : 10;
			int cylinderSegments = isFinal ? 6 : 35;

			retVal.Geometry = UtilityWPF.GetCapsule_AlongZ(cylinderSegments, domeSegments, RADIUSPERCENTOFSCALE, 1d);

			//	Transform
			Transform3DGroup transform = new Transform3DGroup();
			if (isFinal)
			{
				transform.Children.Add(_scaleTransform.Clone());
				transform.Children.Add(new RotateTransform3D(_rotateTransform.Clone()));
				transform.Children.Add(_translateTransform.Clone());
			}
			else
			{
				transform.Children.Add(_scaleTransform);
				transform.Children.Add(new RotateTransform3D(_rotateTransform));
				transform.Children.Add(_translateTransform);
			}
			retVal.Transform = transform;

			//	Exit Function
			return retVal;
		}

		#endregion
	}

	#endregion
	#region Class: FuelTank

	public class FuelTank : PartBase, IContainer
	{
		#region Declaration Section

		public const string PARTTYPE = "FuelTank";

		private FuelTankDesign _design = null;

		private Container _container = null;

		private ItemOptions _itemOptions = null;

		#endregion

		#region Constructor

		public FuelTank(EditorOptions options, ItemOptions itemOptions, PartDNA dna)
			: base(options, dna)
		{
			_itemOptions = itemOptions;

			_design = new FuelTankDesign(options);
			_design.SetDNA(dna);

			double surfaceArea;
			_container = GetContainer(out surfaceArea, itemOptions, dna);

			_dryMass = surfaceArea * itemOptions.FuelTankWallDensity;
		}

		#endregion

		#region IContainer Members

		public double QuantityCurrent
		{
			get
			{
				return _container.QuantityCurrent;
			}
			set
			{
				_container.QuantityCurrent = value;
				base.OnMassChanged();
			}
		}
		public double QuantityMax
		{
			get
			{
				return _container.QuantityMax;
			}
			set
			{
				//_container.QuantityMax = value;
				throw new NotSupportedException("The container max can't be set directly.  It is derived from the dna.scale");
			}
		}
		public double QuantityMaxMinusCurrent
		{
			get
			{
				return _container.QuantityMaxMinusCurrent;
			}
		}

		public bool OnlyRemoveMultiples
		{
			get
			{
				return false;
			}
			set
			{
				if (value)
				{
					throw new NotSupportedException("This property can only be false");
				}
			}
		}
		public double RemovalMultiple
		{
			get
			{
				return 0d;
			}
			set
			{
				throw new NotSupportedException("This doesn't support removal multiples");
			}
		}

		public double AddQuantity(double amount, bool exactAmountOnly)
		{
			double retVal = _container.AddQuantity(amount, exactAmountOnly);

			if (retVal < amount)
			{
				OnMassChanged();
			}

			return retVal;
		}
		public double AddQuantity(IContainer pullFrom, double amount, bool exactAmountOnly)
		{
			double retVal = _container.AddQuantity(pullFrom, amount, exactAmountOnly);

			if (retVal < amount)
			{
				OnMassChanged();
			}

			return retVal;
		}
		public double AddQuantity(IContainer pullFrom, bool exactAmountOnly)
		{
			double prevQuantity = this.QuantityCurrent;

			double retVal = _container.AddQuantity(pullFrom, exactAmountOnly);

			if (this.QuantityCurrent > prevQuantity)
			{
				OnMassChanged();
			}

			return retVal;
		}

		public double RemoveQuantity(double amount, bool exactAmountOnly)
		{
			double retVal = _container.RemoveQuantity(amount, exactAmountOnly);

			if (retVal < amount)
			{
				OnMassChanged();
			}

			return retVal;
		}

		#endregion

		#region Public Properties

		private double _dryMass = 0d;
		public override double DryMass
		{
			get
			{
				return _dryMass;
			}
		}
		public override double TotalMass
		{
			get
			{
				return this.DryMass + (_container.QuantityCurrent * _itemOptions.FuelDensity);
			}
		}

		public override Point3D Position
		{
			get
			{
				return this.DNA.Position;
			}
		}
		public override Quaternion Orientation
		{
			get
			{
				return this.DNA.Orientation;
			}
		}

		private Model3D _model = null;
		public override Model3D Model
		{
			get
			{
				if (_model == null)
				{
					_model = _design.GetFinalModel();
				}

				return _model;
			}
		}

		#endregion

		#region Public Methods

		public override CollisionHull CreateCollisionHull(WorldBase world)
		{
			return _design.CreateCollisionHull(world);
		}

		#endregion

		#region Private Methods

		/// <remarks>
		/// Got ellipse circumference equation here:
		/// http://paulbourke.net/geometry/ellipsecirc/
		/// </remarks>
		private static Container GetContainer(out double surfaceArea, ItemOptions itemOptions, PartDNA dna)
		{
			Container retVal = new Container();

			retVal.OnlyRemoveMultiples = false;

			//	The fuel tank is a cylinder with two half spheres on each end (height along z.  x and y are supposed to be the same)
			//	But when it gets squeezed/stretched along z, the caps don't keep their aspect ratio.  Their height always makes up half of the total height

			#region XYZ H

			double radX = dna.Scale.X * FuelTankDesign.RADIUSPERCENTOFSCALE;
			double radY = dna.Scale.Y * FuelTankDesign.RADIUSPERCENTOFSCALE;
			double radZ = dna.Scale.Z * FuelTankDesign.RADIUSPERCENTOFSCALE;
			double height = dna.Scale.Z - (radZ * 2);
			if (height < 0)
			{
				throw new ApplicationException(string.Format("height should never be zero.  ScaleZ={0}, RAD%SCALE={1}, heigth={2}", dna.Scale.Z.ToString(), FuelTankDesign.RADIUSPERCENTOFSCALE.ToString(), height.ToString()));
			}

			#endregion

			#region Volume

			double sphereVolume = 4d / 3d * Math.PI * radX * radY * radZ;

			double cylinderVolume = Math.PI * radX * radY * height;

			retVal.QuantityMax = sphereVolume + cylinderVolume;

			#endregion

			#region Surface Area

			//	While I have the various radius, calculate the dry mass

			double cylinderSA = 0d;

			if (height > 0d)
			{
				//	Ellipse circumference = pi (a + b) [ 1 + 3 h / (10 + (4 - 3 h)1/2 ) ]
				//	(a=xrad, b=yrad, h = (a - b)^2 / (a + b)^2)  (Ramanujan, second approximation)
				double h = Math.Pow((radX - radY), 2d) / Math.Pow((radX + radY), 2d);
				double circumference = Math.PI * (radX + radY) * (1d + (3d * h) / Math.Pow(10d + (4d - (3d * h)), .5d));

				//	Cylinder side surface area = ellipse circ * height
				cylinderSA = circumference * height;
			}

			//	Sphere surface area = 4 pi ((a^p * b^p) + (a^p * c^p) + (b^p * c^p) / 3) ^ (1/p)
			//	p=1.6075
			double p = 1.6075d;
			double a = Math.Pow(radX, p);
			double b = Math.Pow(radY, p);
			double c = Math.Pow(radZ, p);
			double sphereSA = 4 * Math.PI * Math.Pow(((a * b) + (a * c) + (b * c) / 3d), (1d / p));

			//	Combine them
			surfaceArea = cylinderSA + sphereSA;

			#endregion

			//	Exit Function
			return retVal;
		}

		#endregion
	}

	#endregion
}
