using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Media3D;

using Game.Newt.AsteroidMiner2.ShipEditor;

namespace Game.Newt.AsteroidMiner2.ShipParts
{
	//TODO: Add an extended description
	#region Class: ShieldKineticToolItem

	public class ShieldKineticToolItem : PartToolItemBase
	{
		#region Constructor

		public ShieldKineticToolItem(EditorOptions options)
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
				return "Kinetic shields";
			}
		}
		public override string Description
		{
			get
			{
				return "Blocks against kinetic weapons/impacts (consumes energy)";
			}
		}
		public override string Category
		{
			get
			{
				return PartToolItemBase.CATEGORY_SHIELD;
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
			return new ShieldKineticDesign(this.Options);
		}

		#endregion
	}

	#endregion
	#region Class: ShieldKineticDesign

	public class ShieldKineticDesign : PartDesignBase
	{
		#region Constructor

		public ShieldKineticDesign(EditorOptions options)
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
			return ShieldEnergyDesign.CreateGeometry(this.MaterialBrushes, this.SelectionEmissives,
				_scaleTransform, _rotateTransform, _translateTransform,
				this.Options.WorldColors.ShieldBase, this.Options.WorldColors.ShieldBaseSpecular, this.Options.WorldColors.ShieldBaseEmissive, this.Options.WorldColors.ShieldKinetic, this.Options.WorldColors.ShieldKineticSpecular,
				isFinal);
		}

		#endregion
	}

	#endregion
	#region Class: ShieldKinetic

	public class ShieldKinetic
	{
		public const string PARTTYPE = "ShieldKinetic";
	}

	#endregion
}
