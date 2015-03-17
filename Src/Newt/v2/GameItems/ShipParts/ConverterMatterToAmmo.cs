using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Media3D;

using Game.Newt.v2.GameItems.ShipEditor;
using Game.HelperClassesWPF;

namespace Game.Newt.v2.GameItems.ShipParts
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
        #region Declaration Section

        public const PartDesignAllowedScale ALLOWEDSCALE = PartDesignAllowedScale.XYZ;		// This is here so the scale can be known through reflection

        #endregion

        #region Constructor

        public ConverterMatterToAmmoDesign(EditorOptions options)
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
            return ConverterMatterToFuelDesign.CreateGeometry(this.MaterialBrushes, base.SelectionEmissives,
                GetTransformForGeometry(isFinal),
                WorldColors.ConverterBase, WorldColors.ConverterBaseSpecular, WorldColors.ConverterAmmo, WorldColors.ConverterAmmoSpecular,
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
