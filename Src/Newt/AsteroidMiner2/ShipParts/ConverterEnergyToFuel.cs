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
	#region Class: ConverterEnergyToFuelToolItem

	public class ConverterEnergyToFuelToolItem : PartToolItemBase
	{
		#region Constructor

		public ConverterEnergyToFuelToolItem(EditorOptions options)
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
				return "Energy to Fuel Converter";
			}
		}
		public override string Description
		{
			get
			{
				return "Consumes energy and puts fuel in the fuel tank";
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
			return new ConverterEnergyToFuelDesign(this.Options);
		}

		#endregion
	}

	#endregion
	#region Class: ConverterEnergyToFuelDesign

	public class ConverterEnergyToFuelDesign : PartDesignBase
	{
		#region Declaration Section

		public const double RADIUSPERCENTOFSCALE = .3d;
		public const double HEIGHTPERCENTOFSCALE = .66d;

		#endregion

		#region Constructor

		public ConverterEnergyToFuelDesign(EditorOptions options)
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
			return CreateCollisionHull(world, this.Scale, this.Orientation, this.Position);
		}

		internal static CollisionHull CreateCollisionHull(WorldBase world, Vector3D scale, Quaternion orientation, Point3D position)
		{
			Transform3DGroup transform = new Transform3DGroup();
			transform.Children.Add(new ScaleTransform3D(scale));
			transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90)));		//	the physics hull is along x, but dna is along z
			transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(orientation)));
			transform.Children.Add(new TranslateTransform3D(position.ToVector()));

			//NOTE: The visual changes the caps around, but I want the physics to be a capsule
			//return CollisionHull.CreateChamferCylinder(world, 0, RADIUSPERCENTOFSCALE, 1d, transform.Value);
			return CollisionHull.CreateChamferCylinder(world, 0, RADIUSPERCENTOFSCALE, HEIGHTPERCENTOFSCALE, transform.Value);
		}

		#endregion

		#region Private Methods

		private Model3DGroup CreateGeometry(bool isFinal)
		{
			return CreateGeometry(this.MaterialBrushes, this.SelectionEmissives,
				_scaleTransform, _rotateTransform, _translateTransform,
				this.Options.WorldColors.ConverterBase, this.Options.WorldColors.ConverterBaseSpecular, this.Options.WorldColors.ConverterFuel, this.Options.WorldColors.ConverterFuelSpecular,
				isFinal);
		}

		internal static Model3DGroup CreateGeometry(List<MaterialColorProps> materialBrushes, List<EmissiveMaterial> selectionEmissives, ScaleTransform3D scaleTransform, QuaternionRotation3D rotateTransform, TranslateTransform3D translateTransform, Color baseColor, SpecularMaterial baseSpecular, Color colorColor, SpecularMaterial colorSpecular, bool isFinal)
		{
			const double SCALE = .66d;

			Model3DGroup retVal = new Model3DGroup();

			GeometryModel3D geometry;
			MaterialGroup material;
			DiffuseMaterial diffuse;
			SpecularMaterial specular;

			#region Main Cylinder

			geometry = new GeometryModel3D();
			material = new MaterialGroup();
			diffuse = new DiffuseMaterial(new SolidColorBrush(baseColor));
			materialBrushes.Add(new MaterialColorProps(diffuse, diffuse.Brush, baseColor));
			material.Children.Add(diffuse);
			specular = baseSpecular;
			materialBrushes.Add(new MaterialColorProps(specular));
			material.Children.Add(specular);

			if (!isFinal)
			{
				EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
				material.Children.Add(selectionEmissive);
				selectionEmissives.Add(selectionEmissive);
			}

			geometry.Material = material;
			geometry.BackMaterial = material;

			geometry.Geometry = GetMeshBase(SCALE, isFinal);

			retVal.Children.Add(geometry);

			#endregion

			#region Ring

			geometry = new GeometryModel3D();
			material = new MaterialGroup();
			diffuse = new DiffuseMaterial(new SolidColorBrush(colorColor));
			materialBrushes.Add(new MaterialColorProps(diffuse, diffuse.Brush, colorColor));
			material.Children.Add(diffuse);
			specular = colorSpecular;
			materialBrushes.Add(new MaterialColorProps(specular));
			material.Children.Add(specular);

			if (!isFinal)
			{
				EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
				material.Children.Add(selectionEmissive);
				selectionEmissives.Add(selectionEmissive);
			}

			geometry.Material = material;
			geometry.BackMaterial = material;

			geometry.Geometry = GetMeshColor(SCALE, isFinal);

			retVal.Children.Add(geometry);

			#endregion

			//	Transform
			Transform3DGroup transformGlobal = new Transform3DGroup();
			if (isFinal)
			{
				transformGlobal.Children.Add(scaleTransform.Clone());
				transformGlobal.Children.Add(new RotateTransform3D(rotateTransform.Clone()));
				transformGlobal.Children.Add(translateTransform.Clone());
			}
			else
			{
				transformGlobal.Children.Add(scaleTransform);
				transformGlobal.Children.Add(new RotateTransform3D(rotateTransform));
				transformGlobal.Children.Add(translateTransform);
			}
			retVal.Transform = transformGlobal;

			//	Exit Function
			return retVal;
		}

		internal static MeshGeometry3D GetMeshBase(double scale, bool isFinal)
		{
			int domeSegments = isFinal ? 2 : 10;
			int cylinderSegments = isFinal ? 6 : 35;

			List<TubeRingBase> rings = new List<TubeRingBase>();
			rings.Add(new TubeRingDome(0, false, domeSegments));
			rings.Add(new TubeRingRegularPolygon(.5, false, 1, 1, false));

			rings.Add(new TubeRingRegularPolygon(.75, false, .75, .75, false));

			rings.Add(new TubeRingRegularPolygon(.75, false, 1, 1, false));
			rings.Add(new TubeRingDome(.5, false, domeSegments));

			//	Scale this so the height is 1
			double localScale = scale / rings.Sum(o => o.DistFromPrevRing);

			Transform3DGroup transformInitial = new Transform3DGroup();
			transformInitial.Children.Add(new ScaleTransform3D(localScale, localScale, localScale));
			return UtilityWPF.GetMultiRingedTube(cylinderSegments, rings, true, true, transformInitial);
		}
		internal static MeshGeometry3D GetMeshColor(double scale, bool isFinal)
		{
			//int spineSegments = isFinal ? 6 : 35;
			//int fleshSegments = isFinal ? 3 : 10;
			//return UtilityWPF.GetTorus(spineSegments, fleshSegments, .1d * scale, .25d * scale);

			int segments = isFinal ? 2 : 20;
			return UtilityWPF.GetSphere(segments, .35d * scale, .35d * scale, .2d * scale);
		}

		#endregion
	}

	#endregion
	#region Class: ConverterEnergyToFuel

	public class ConverterEnergyToFuel : PartBase
	{
		#region Declaration Section

		public const string PARTTYPE = "ConverterEnergyToFuel";

		private ItemOptions _itemOptions = null;

		private Converter _converter = null;

		private ConverterEnergyToFuelDesign _design = null;

		private double _mass = 0d;

		#endregion

		#region Constructor

		/// <summary>
		/// NOTE: It's assumed that energyTanks and fuelTanks are actually container groups holding the actual tanks, but it
		/// could be the tanks passed in directly
		/// </summary>
		public ConverterEnergyToFuel(EditorOptions options, ItemOptions itemOptions, PartDNA dna, IContainer energyTanks, IContainer fuelTanks)
			: base(options, dna)
		{
			_itemOptions = itemOptions;

			_design = new ConverterEnergyToFuelDesign(options);
			_design.SetDNA(dna);

			double volume = GetVolume(dna);

			if (energyTanks != null && fuelTanks != null)
			{
				_converter = new Converter(energyTanks, fuelTanks, itemOptions.EnergyToFuelConversionRate, itemOptions.EnergyToFuelAmountToDraw * volume);
			}

			_mass = volume * itemOptions.EnergyToFuelDensity;
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

		internal static double GetVolume(PartDNA dna)
		{
			//	In reality, it's an odd shape.  But for this, just assume a cylinder
			double radX = dna.Scale.X * ConverterEnergyToFuelDesign.RADIUSPERCENTOFSCALE;
			double radY = dna.Scale.Y * ConverterEnergyToFuelDesign.RADIUSPERCENTOFSCALE;
			double height = dna.Scale.Z;

			return Math.PI * radX * radY * height;
		}

		#endregion
	}

	#endregion
}
