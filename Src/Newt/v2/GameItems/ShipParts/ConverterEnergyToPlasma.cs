using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.GameItems.ShipParts
{
    #region class: ConverterEnergyToPlasmaToolItem

    public class ConverterEnergyToPlasmaToolItem : PartToolItemBase
    {
        #region Constructor

        public ConverterEnergyToPlasmaToolItem(EditorOptions options)
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
                return "Energy to Plasma Converter";
            }
        }
        public override string Description
        {
            get
            {
                return "Consumes energy and puts plasma in the plasma tank";
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
            return new ConverterEnergyToPlasmaDesign(this.Options, false);
        }

        #endregion
    }

    #endregion
    #region class: ConverterEnergyToPlasmaDesign

    public class ConverterEnergyToPlasmaDesign : PartDesignBase
    {
        #region Declaration Section

        public const PartDesignAllowedScale ALLOWEDSCALE = PartDesignAllowedScale.XYZ;		// This is here so the scale can be known through reflection

        private MassBreakdownCache _massBreakdown = null;

        #endregion

        #region Constructor

        public ConverterEnergyToPlasmaDesign(EditorOptions options, bool isFinalModel)
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
            return ConverterEnergyToFuelDesign.CreateCollisionHull(world, this.Scale, this.Orientation, this.Position);
        }

        public override UtilityNewt.IObjectMassBreakdown GetMassBreakdown(double cellSize)
        {
            if (_massBreakdown != null && _massBreakdown.Scale == Scale && _massBreakdown.CellSize == cellSize)
            {
                // This has already been built for this size
                return _massBreakdown.Breakdown;
            }

            var breakdown = ConverterEnergyToFuelDesign.GetMassBreakdown(this.Scale, cellSize);

            // Store this
            _massBreakdown = new MassBreakdownCache(breakdown, Scale, cellSize);

            return _massBreakdown.Breakdown;
        }

        public override PartToolItemBase GetToolItem()
        {
            return new ConverterEnergyToPlasmaToolItem(this.Options);
        }

        #endregion

        #region Private Methods

        private Model3DGroup CreateGeometry(bool isFinal)
        {
            return ConverterEnergyToFuelDesign.CreateGeometry(this.MaterialBrushes, this.SelectionEmissives,
                GetTransformForGeometry(isFinal),
                WorldColors.ConverterBase_Color, WorldColors.ConverterBase_Specular, WorldColors.ConverterPlasma_Color, WorldColors.ConverterPlasma_Specular,
                isFinal);
        }

        #endregion
    }

    #endregion
    #region class: ConverterEnergyToPlasma

    public class ConverterEnergyToPlasma : PartBase, IPartUpdatable
    {
        #region Declaration Section

        public const string PARTTYPE = nameof(ConverterEnergyToPlasma);

        private ItemOptions _itemOptions = null;

        private Converter _converter = null;

        #endregion

        #region Constructor

        /// <summary>
        /// NOTE: It's assumed that energyTanks and plasmaTanks are actually container groups holding the actual tanks, but it
        /// could be the tanks passed in directly
        /// </summary>
        public ConverterEnergyToPlasma(EditorOptions options, ItemOptions itemOptions, ShipPartDNA dna, IContainer energyTanks, IContainer plasmaTanks)
            : base(options, dna, itemOptions.EnergyConverter_Damage.HitpointMin, itemOptions.EnergyConverter_Damage.HitpointSlope, itemOptions.EnergyConverter_Damage.Damage)
        {
            _itemOptions = itemOptions;

            this.Design = new ConverterEnergyToPlasmaDesign(options, true);
            this.Design.SetDNA(dna);

            double volume = ConverterEnergyToFuel.GetVolume(out _scaleActual, dna);

            if (energyTanks != null && plasmaTanks != null)
            {
                _converter = new Converter(energyTanks, plasmaTanks, itemOptions.EnergyToPlasma_ConversionRate, itemOptions.EnergyToPlasma_AmountToDraw * volume);
            }

            _mass = volume * itemOptions.EnergyToPlasma_Density;
        }

        #endregion

        #region IPartUpdatable Members

        public void Update_MainThread(double elapsedTime)
        {
        }
        public void Update_AnyThread(double elapsedTime)
        {
            if (this.IsDestroyed)
            {
                return;
            }

            this.Transfer(elapsedTime, 1);
        }

        public int? IntervalSkips_MainThread => null;
        public int? IntervalSkips_AnyThread => 0;

        #endregion

        #region Public Properties

        private readonly double _mass;
        public override double DryMass => _mass;
        public override double TotalMass => _mass;

        private readonly Vector3D _scaleActual;
        public override Vector3D ScaleActual => _scaleActual;

        #endregion

        #region Public Methods

        public void Transfer(double elapsedTime, double percent)
        {
            if (_converter != null)
            {
                _converter.Transfer(elapsedTime, percent);
            }
        }

        #endregion
    }

    #endregion
}
