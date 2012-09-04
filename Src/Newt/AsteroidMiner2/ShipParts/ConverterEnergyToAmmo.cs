using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Media3D;

using Game.Newt.AsteroidMiner2.ShipEditor;
using Game.Newt.HelperClasses;
using Game.Newt.NewtonDynamics;

namespace Game.Newt.AsteroidMiner2.ShipParts
{
	#region Class: ConverterEnergyToAmmoToolItem

	public class ConverterEnergyToAmmoToolItem : PartToolItemBase
	{
		#region Constructor

		public ConverterEnergyToAmmoToolItem(EditorOptions options)
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
				return "Energy to Ammo Converter";
			}
		}
		public override string Description
		{
			get
			{
				return "Consumes energy and puts ammo in the ammo box";
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
			return new ConverterEnergyToAmmoDesign(this.Options);
		}

		#endregion
	}

	#endregion
	#region Class: ConverterEnergyToAmmoDesign

	public class ConverterEnergyToAmmoDesign : PartDesignBase
	{
		#region Declaration Section

		private Tuple<UtilityNewt.IObjectMassBreakdown, Vector3D, double> _massBreakdown = null;

		#endregion

		#region Constructor

		public ConverterEnergyToAmmoDesign(EditorOptions options)
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
			return ConverterEnergyToFuelDesign.CreateCollisionHull(world, this.Scale, this.Orientation, this.Position);
		}

		public override UtilityNewt.IObjectMassBreakdown GetMassBreakdown(double cellSize)
		{
			if (_massBreakdown != null && _massBreakdown.Item2 == this.Scale && _massBreakdown.Item3 == cellSize)
			{
				//	This has already been built for this size
				return _massBreakdown.Item1;
			}

			var breakdown = ConverterEnergyToFuelDesign.GetMassBreakdown(this.Scale, cellSize);

			//	Store this
			_massBreakdown = new Tuple<UtilityNewt.IObjectMassBreakdown, Vector3D, double>(breakdown, this.Scale, cellSize);

			//	Exit Function
			return _massBreakdown.Item1;
		}

		#endregion

		#region Private Methods

		private Model3DGroup CreateGeometry(bool isFinal)
		{
			return ConverterEnergyToFuelDesign.CreateGeometry(this.MaterialBrushes, this.SelectionEmissives,
				_scaleTransform, _rotateTransform, _translateTransform,
				this.Options.WorldColors.ConverterBase, this.Options.WorldColors.ConverterBaseSpecular, this.Options.WorldColors.ConverterAmmo, this.Options.WorldColors.ConverterAmmoSpecular,
				isFinal);
		}

		#endregion
	}

	#endregion
	#region Class: ConverterEnergyToAmmo

	public class ConverterEnergyToAmmo : PartBase
	{
		#region Declaration Section

		public const string PARTTYPE = "ConverterEnergyToAmmo";

		private ItemOptions _itemOptions = null;

		private Converter _converter = null;

		private ConverterEnergyToAmmoDesign _design = null;

		private double _mass = 0d;

		#endregion

		#region Constructor

		/// <summary>
		/// NOTE: It's assumed that energyTanks and ammoBoxes are actually container groups holding the actual tanks, but it
		/// could be the tanks passed in directly
		/// </summary>
		public ConverterEnergyToAmmo(EditorOptions options, ItemOptions itemOptions, PartDNA dna, IContainer energyTanks, IContainer ammoBoxes)
			: base(options, dna)
		{
			_itemOptions = itemOptions;

			_design = new ConverterEnergyToAmmoDesign(options);
			_design.SetDNA(dna);

			double volume = ConverterEnergyToFuel.GetVolume(dna);

			if (energyTanks != null && ammoBoxes != null)
			{
				_converter = new Converter(energyTanks, ammoBoxes, itemOptions.EnergyToAmmoConversionRate, itemOptions.EnergyToAmmoAmountToDraw * volume);
			}

			_mass = volume * itemOptions.EnergyToAmmoDensity;
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

		public override UtilityNewt.IObjectMassBreakdown GetMassBreakdown(double cellSize)
		{
			return _design.GetMassBreakdown(cellSize);
		}

		#endregion
	}

	#endregion
}
