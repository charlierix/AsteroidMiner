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
	#region Class: ConverterFuelToEnergyToolItem

	public class ConverterFuelToEnergyToolItem : PartToolItemBase
	{
		#region Constructor

		public ConverterFuelToEnergyToolItem(EditorOptions options)
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
				return "Fuel to Energy Converter";
			}
		}
		public override string Description
		{
			get
			{
				return "Burns fuel and stores energy";
			}
		}
		public override string Category
		{
			get
			{
				return PartToolItemBase.CATEGORY_CONVERTERS;
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
			return new ConverterFuelToEnergyDesign(this.Options);
		}

		#endregion
	}

	#endregion
	#region Class: ConverterFuelToEnergyDesign

	public class ConverterFuelToEnergyDesign : PartDesignBase
	{
		#region Declaration Section

		public const double RADIUSPERCENTOFSCALE = .3d;
		public const double HEIGHTPERCENTOFSCALE = .4d;

		#endregion

		#region Constructor

		public ConverterFuelToEnergyDesign(EditorOptions options)
			: base(options) { }

		#endregion

		#region Public Properties

		public override PartDesignAllowedScale AllowedScale
		{
			get
			{
				return PartDesignAllowedScale.XYZ;
			}
		}
		public override PartDesignAllowedRotation AllowedRotation
		{
			get
			{
				return PartDesignAllowedRotation.X_Y_Z;
			}
		}

		private Model3DGroup _geometries = null;
		public override Model3D Model
		{
			get
			{
				if (_geometries == null)
				{
					_geometries = CreateGeometry(false);
				}

				return _geometries;
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
			transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(this.Orientation)));
			transform.Children.Add(new TranslateTransform3D(this.Position.ToVector()));

			Vector3D scale = this.Scale;

			return CollisionHull.CreateSphere(world, 0, new Vector3D(scale.X * RADIUSPERCENTOFSCALE, scale.Y * RADIUSPERCENTOFSCALE, scale.Z * HEIGHTPERCENTOFSCALE), transform.Value);
		}

		#endregion

		#region Private Methods

		private Model3DGroup CreateGeometry(bool isFinal)
		{
			const double SCALE = .75d;

			Model3DGroup retVal = new Model3DGroup();

			GeometryModel3D geometry;
			MaterialGroup material;
			DiffuseMaterial diffuse;
			SpecularMaterial specular;

			int domeSegments = isFinal ? 2 : 10;
			int cylinderSegments = isFinal ? 6 : 35;

			#region Main Cylinder

			geometry = new GeometryModel3D();
			material = new MaterialGroup();
			diffuse = new DiffuseMaterial(new SolidColorBrush(this.Options.WorldColors.ConverterBase));
			this.MaterialBrushes.Add(new MaterialColorProps(diffuse, diffuse.Brush, this.Options.WorldColors.ConverterBase));
			material.Children.Add(diffuse);
			specular = this.Options.WorldColors.ConverterBaseSpecular;
			this.MaterialBrushes.Add(new MaterialColorProps(specular));
			material.Children.Add(specular);

			if (!isFinal)
			{
				EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
				material.Children.Add(selectionEmissive);
				this.SelectionEmissives.Add(selectionEmissive);
			}

			geometry.Material = material;
			geometry.BackMaterial = material;

			List<TubeRingBase> rings = new List<TubeRingBase>();
			rings.Add(new TubeRingRegularPolygon(0, false, .4, .4, true));
			rings.Add(new TubeRingRegularPolygon(.125, false, .48, .48, false));
			rings.Add(new TubeRingRegularPolygon(.25, false, .6, .6, false));

			rings.Add(new TubeRingRegularPolygon(.125, false, .65, .65, false));
			rings.Add(new TubeRingRegularPolygon(.25, false, .7, .7, false));
			rings.Add(new TubeRingRegularPolygon(.25, false, .65, .65, false));

			rings.Add(new TubeRingRegularPolygon(.125, false, .6, .6, false));
			rings.Add(new TubeRingRegularPolygon(.25, false, .48, .48, false));
			rings.Add(new TubeRingRegularPolygon(.125, false, .4, .4, true));

			//	Scale this so the height is 1
			double localScale = SCALE * .85d / rings.Sum(o => o.DistFromPrevRing);

			Transform3DGroup transformInitial = new Transform3DGroup();
			transformInitial.Children.Add(new ScaleTransform3D(localScale, localScale, localScale));
			geometry.Geometry = UtilityWPF.GetMultiRingedTube(cylinderSegments, rings, true, true, transformInitial);

			retVal.Children.Add(geometry);

			#endregion

			#region Axis

			geometry = new GeometryModel3D();
			material = new MaterialGroup();
			diffuse = new DiffuseMaterial(new SolidColorBrush(this.Options.WorldColors.ConverterEnergy));
			this.MaterialBrushes.Add(new MaterialColorProps(diffuse, diffuse.Brush, this.Options.WorldColors.ConverterEnergy));
			material.Children.Add(diffuse);
			specular = this.Options.WorldColors.ConverterEnergySpecular;
			this.MaterialBrushes.Add(new MaterialColorProps(specular));
			material.Children.Add(specular);

			if (!isFinal)
			{
				EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
				material.Children.Add(selectionEmissive);
				this.SelectionEmissives.Add(selectionEmissive);
			}

			geometry.Material = material;
			geometry.BackMaterial = material;

			geometry.Geometry = UtilityWPF.GetCapsule_AlongZ(cylinderSegments, domeSegments, .15 * SCALE, SCALE);

			retVal.Children.Add(geometry);

			#endregion

			//	Transform
			Transform3DGroup transformGlobal = new Transform3DGroup();
			if (isFinal)
			{
				transformGlobal.Children.Add(_scaleTransform.Clone());
				transformGlobal.Children.Add(new RotateTransform3D(_rotateTransform.Clone()));
				transformGlobal.Children.Add(_translateTransform.Clone());
			}
			else
			{
				transformGlobal.Children.Add(_scaleTransform);
				transformGlobal.Children.Add(new RotateTransform3D(_rotateTransform));
				transformGlobal.Children.Add(_translateTransform);
			}
			retVal.Transform = transformGlobal;

			//	Exit Function
			return retVal;
		}

		#endregion
	}

	#endregion
	#region Class: ConverterFuelToEnergy

	public class ConverterFuelToEnergy : PartBase
	{
		#region Declaration Section

		public const string PARTTYPE = "ConverterFuelToEnergy";

		private ItemOptions _itemOptions = null;

		private Converter _converter = null;

		private ConverterFuelToEnergyDesign _design = null;

		private double _mass = 0d;

		#endregion

		#region Constructor

		public ConverterFuelToEnergy(EditorOptions options, ItemOptions itemOptions, PartDNA dna, IContainer fuelTanks, IContainer energyTanks)
			: base(options, dna)
		{
			_itemOptions = itemOptions;

			_design = new ConverterFuelToEnergyDesign(options);
			_design.SetDNA(dna);

			double volume = GetVolume(dna);

			if (fuelTanks != null && energyTanks != null)
			{
				_converter = new Converter(fuelTanks, energyTanks, itemOptions.FuelToEnergyConversionRate, itemOptions.FuelToEnergyAmountToDraw * volume);
			}

			_mass = volume * itemOptions.FuelToEnergyDensity;
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

		public void Transfer(double elapsedTime, double percent)
		{
			if (_converter != null)
			{
				_converter.Transfer(elapsedTime, percent);
			}
		}

		public override CollisionHull CreateCollisionHull(WorldBase world)
		{
			return _design.CreateCollisionHull(world);
		}

		#endregion

		#region Private Methods

		private static double GetVolume(PartDNA dna)
		{
			//	In reality, it's an odd shape.  But for this, just assume an ellipse
			double radX = dna.Scale.X * ConverterFuelToEnergyDesign.RADIUSPERCENTOFSCALE;
			double radY = dna.Scale.Y * ConverterFuelToEnergyDesign.RADIUSPERCENTOFSCALE;
			double radZ = dna.Scale.Z * ConverterFuelToEnergyDesign.HEIGHTPERCENTOFSCALE;

			return 4d / 3d * Math.PI * radX * radY * radZ;
		}

		#endregion
	}

	#endregion
}
