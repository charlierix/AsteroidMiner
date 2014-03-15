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
        #region Declaration Section

        public const PartDesignAllowedScale ALLOWEDSCALE = PartDesignAllowedScale.XYZ;		// This is here so the scale can be known through reflection

        #endregion

        #region Constructor

        public ShieldKineticDesign(EditorOptions options)
            : base(options) { }

        #endregion

        #region Public Properties

        public override PartDesignAllowedScale AllowedScale
        {
            get
            {
                return ALLOWEDSCALE;
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
                GetTransformForGeometry(isFinal),
                WorldColors.ShieldBase, WorldColors.ShieldBaseSpecular, WorldColors.ShieldBaseEmissive, WorldColors.ShieldKinetic, WorldColors.ShieldKineticSpecular,
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
