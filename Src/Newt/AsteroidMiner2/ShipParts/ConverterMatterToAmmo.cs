using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Media3D;

using Game.Newt.AsteroidMiner2.ShipEditor;

namespace Game.Newt.AsteroidMiner2.ShipParts
{
	#region Class: ConverterMatterToAmmo

	public class ConverterMatterToAmmoToolItem : PartToolItemBase
	{
		#region Constructor

		public ConverterMatterToAmmoToolItem(EditorOptions options)
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
				return "Matter to Ammo Converter";
			}
		}
		public override string Description
		{
			get
			{
				return "Pulls matter out of the cargo bay and puts ammo in the ammo box";
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
			return new ConverterMatterToAmmoDesign(this.Options);
		}

		#endregion
	}

	#endregion
	#region Class: ConverterMatterToAmmoDesign

	public class ConverterMatterToAmmoDesign : PartDesignBase
	{
		#region Constructor

		public ConverterMatterToAmmoDesign(EditorOptions options)
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

		#endregion

		#region Private Methods

		private Model3DGroup CreateGeometry(bool isFinal)
		{
			return ConverterMatterToFuelDesign.CreateGeometry(this.MaterialBrushes, base.SelectionEmissives,
				_scaleTransform, _rotateTransform, _translateTransform,
				this.Options.WorldColors.ConverterBase, this.Options.WorldColors.ConverterBaseSpecular, this.Options.WorldColors.ConverterAmmo, this.Options.WorldColors.ConverterAmmoSpecular,
				isFinal);
		}

		#endregion
	}

	#endregion
	#region Class: ConverterMatterToAmmo

	public class ConverterMatterToAmmo
	{
		public const string PARTTYPE = "ConverterMatterToAmmo";
	}

	#endregion
}
