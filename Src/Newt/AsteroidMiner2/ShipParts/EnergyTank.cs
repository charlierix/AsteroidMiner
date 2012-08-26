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
	#region Class: EnergyTankToolItem

	public class EnergyTankToolItem : PartToolItemBase
	{
		#region Constructor

		public EnergyTankToolItem(EditorOptions options)
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
				return "Energy Tank";
			}
		}
		public override string Description
		{
			get
			{
				return "Stores energy";
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
			return new EnergyTankDesign(this.Options);
		}

		#endregion
	}

	#endregion
	#region Class: EnergyTankDesign

	public class EnergyTankDesign : PartDesignBase
	{
		#region Declaration Section

		internal const double RADIUSPERCENTOFSCALE = 0.3030303030303d;		//	when x scale is 1, the x radius will be this (x and y are the same)

		#endregion

		#region Constructor

		public EnergyTankDesign(EditorOptions options)
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

			//TODO: Every ship will have an energy tank.  Do a performance test of cylinder vs chamfercylinder
			//return CollisionHull.CreateCylinder(world, 0, radius, height, transform.Value);
			return CollisionHull.CreateChamferCylinder(world, 0, radius, height, transform.Value);
		}

		#endregion

		#region Private Methods

		private GeometryModel3D CreateGeometry(bool isFinal)
		{
			MaterialGroup material = new MaterialGroup();
			DiffuseMaterial diffuse = new DiffuseMaterial(new SolidColorBrush(this.Options.WorldColors.EnergyTank));
			this.MaterialBrushes.Add(new MaterialColorProps(diffuse, diffuse.Brush, this.Options.WorldColors.EnergyTank));
			material.Children.Add(diffuse);
			SpecularMaterial specular = this.Options.WorldColors.EnergyTankSpecular;
			this.MaterialBrushes.Add(new MaterialColorProps(specular));
			material.Children.Add(specular);
			EmissiveMaterial emissive = this.Options.WorldColors.EnergyTankEmissive;
			this.MaterialBrushes.Add(new MaterialColorProps(emissive));
			material.Children.Add(emissive);

			if (!isFinal)
			{
				EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
				material.Children.Add(selectionEmissive);
				base.SelectionEmissives.Add(selectionEmissive);
			}

			GeometryModel3D retVal = new GeometryModel3D();
			retVal.Material = material;
			retVal.BackMaterial = material;

			int domeSegments = isFinal ? 2 : 10;
			int cylinderSegments = isFinal ? 6 : 35;

			List<TubeRingBase> rings = new List<TubeRingBase>();
			rings.Add(new TubeRingDome(0, false, domeSegments));
			rings.Add(new TubeRingRegularPolygon(.4, false, 1, 1, false));
			rings.Add(new TubeRingRegularPolygon(2.5, false, 1, 1, false));
			rings.Add(new TubeRingDome(.4, false, domeSegments));

			//	Scale this so the height is 1
			double scale = 1d / rings.Sum(o => o.DistFromPrevRing);

			Transform3DGroup transformInitial = new Transform3DGroup();
			transformInitial.Children.Add(new ScaleTransform3D(scale, scale, scale));
			retVal.Geometry = UtilityWPF.GetMultiRingedTube(cylinderSegments, rings, true, true, transformInitial);

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
	#region Class: EnergyTank

	/// <summary>
	/// This is the real world object that physically holds energy, has mass takes impact damage, etc
	/// </summary>
	public class EnergyTank : PartBase, IContainer
	{
		#region Declaration Section

		public const string PARTTYPE = "EnergyTank";

		private EnergyTankDesign _design = null;

		private Container _container = null;

		private ItemOptions _itemOptions = null;

		private double _mass = 0d;

		#endregion

		#region Constructor

		public EnergyTank(EditorOptions options, ItemOptions itemOptions, PartDNA dna)
			: base(options, dna)
		{
			_itemOptions = itemOptions;

			_design = new EnergyTankDesign(options);
			_design.SetDNA(dna);

			_container = GetContainer(itemOptions, dna);

			_mass = _container.QuantityMax * itemOptions.EnergyTankDensity;
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
				throw new NotSupportedException("The container max can't be set directly.  It is derived from the dna.scale");
				//_container.QuantityMax = value;
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
			return _container.AddQuantity(amount, exactAmountOnly);
		}
		public double AddQuantity(IContainer pullFrom, double amount, bool exactAmountOnly)
		{
			return _container.AddQuantity(pullFrom, amount, exactAmountOnly);
		}
		public double AddQuantity(IContainer pullFrom, bool exactAmountOnly)
		{
			return _container.AddQuantity(pullFrom, exactAmountOnly);
		}

		public double RemoveQuantity(double amount, bool exactAmountOnly)
		{
			return _container.RemoveQuantity(amount, exactAmountOnly);
		}

		#endregion

		#region Public Properties

		public override double DryMass
		{
			get
			{
				return _mass;
			}
		}
		public override double TotalMass
		{
			get
			{
				return _mass;
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

		private static Container GetContainer(ItemOptions itemOptions, PartDNA dna)
		{
			Container retVal = new Container();

			retVal.OnlyRemoveMultiples = false;

			//	Even though it has slightly rounded endcaps, I'll assume it's just a cylinder

			double radX = dna.Scale.X * EnergyTankDesign.RADIUSPERCENTOFSCALE;
			double radY = dna.Scale.Y * EnergyTankDesign.RADIUSPERCENTOFSCALE;
			double height = dna.Scale.Z;

			double cylinderVolume = Math.PI * radX * radY * height;

			retVal.QuantityMax = cylinderVolume;

			//	Exit Function
			return retVal;
		}

		#endregion
	}

	#endregion
}
