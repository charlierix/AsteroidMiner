using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Media3D;

using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.GameItems.ShipParts
{
    //TODO: Add an extended description
    #region class: ShieldTractorToolItem

    public class ShieldTractorToolItem : PartToolItemBase
    {
        #region Constructor

        public ShieldTractorToolItem(EditorOptions options)
            : base(options)
        {
            this.TabName = PartToolItemBase.TAB_SHIPPART;
            _visual2D = PartToolItemBase.GetVisual2D(this.Name, this.Description, options, this);
        }

        #endregion

        #region Public Properties

        public override string Name
        {
            get
            {
                return "Tractor shields";
            }
        }
        public override string Description
        {
            get
            {
                return "Blocks tractor beams from external sources (consumes energy)";
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
            return new ShieldTractorDesign(this.Options, false);
        }

        #endregion
    }

    #endregion
    #region class: ShieldTractorDesign

    public class ShieldTractorDesign : PartDesignBase
    {
        #region Declaration Section

        public const PartDesignAllowedScale ALLOWEDSCALE = PartDesignAllowedScale.XYZ;		// This is here so the scale can be known through reflection

        private MassBreakdownCache _massBreakdown = null;

        #endregion

        #region Constructor

        public ShieldTractorDesign(EditorOptions options, bool isFinalModel)
            : base(options, isFinalModel) { }

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

        private Model3DGroup _model = null;
        public override Model3D Model
        {
            get
            {
                if (_model == null)
                {
                    _model = CreateGeometry(this.IsFinalModel);
                }

                return _model;
            }
        }

        #endregion

        #region Public Methods

        public override CollisionHull CreateCollisionHull(WorldBase world)
        {
            return ShieldEnergyDesign.CreateCollisionHull(world, this.Scale, this.Orientation, this.Position);
        }
        public override UtilityNewt.IObjectMassBreakdown GetMassBreakdown(double cellSize)
        {
            if (_massBreakdown != null && _massBreakdown.Scale == Scale && _massBreakdown.CellSize == cellSize)
            {
                // This has already been built for this size
                return _massBreakdown.Breakdown;
            }

            var breakdown = ShieldEnergyDesign.GetMassBreakdown(this.Scale, cellSize);

            // Store this
            _massBreakdown = new MassBreakdownCache(breakdown, Scale, cellSize);

            return _massBreakdown.Breakdown;
        }

        public override PartToolItemBase GetToolItem()
        {
            return new ShieldTractorToolItem(this.Options);
        }

        #endregion

        #region Private Methods

        private Model3DGroup CreateGeometry(bool isFinal)
        {
            return ShieldEnergyDesign.CreateGeometry(this.MaterialBrushes, this.SelectionEmissives,
                GetTransformForGeometry(isFinal),
                WorldColors.ShieldBase_Color, WorldColors.ShieldBase_Specular, WorldColors.ShieldBase_Emissive, WorldColors.ShieldTractor_Color, WorldColors.ShieldTractor_Specular,
                isFinal);
        }

        #endregion
    }

    #endregion
    #region class: ShieldTractor

    public class ShieldTractor : PartBase
    {
        #region Declaration Section

        public const string PARTTYPE = nameof(ShieldTractor);

        private readonly ItemOptions _itemOptions;

        private readonly IContainer _plasma;

        #endregion

        #region Constructor

        public ShieldTractor(EditorOptions options, ItemOptions itemOptions, ShipPartDNA dna, IContainer plasma)
            : base(options, dna, itemOptions.Shield_Damage.HitpointMin, itemOptions.Shield_Damage.HitpointSlope, itemOptions.Shield_Damage.Damage)
        {
            _itemOptions = itemOptions;
            _plasma = plasma;

            this.Design = new ShieldTractorDesign(options, true);
            this.Design.SetDNA(dna);

            double volume, radius;
            ShieldEnergy.GetMass(out _mass, out volume, out radius, out _scaleActual, dna, itemOptions);

            //this.Radius = radius;

        }

        #endregion

        #region Public Properties

        private readonly double _mass;
        public override double DryMass => _mass;
        public override double TotalMass => _mass;

        private readonly Vector3D _scaleActual;
        public override Vector3D ScaleActual => _scaleActual;

        #endregion
    }

    #endregion
}
