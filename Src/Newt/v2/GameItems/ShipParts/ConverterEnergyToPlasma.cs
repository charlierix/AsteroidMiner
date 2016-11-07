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
    #region Class: ConverterEnergyToPlasmaToolItem

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
            return new ConverterEnergyToPlasmaDesign(this.Options);
        }

        #endregion
    }

    #endregion
    #region Class: ConverterEnergyToPlasmaDesign

    public class ConverterEnergyToPlasmaDesign : PartDesignBase
    {
        #region Declaration Section

        public const PartDesignAllowedScale ALLOWEDSCALE = PartDesignAllowedScale.XYZ;		// This is here so the scale can be known through reflection

        private Tuple<UtilityNewt.IObjectMassBreakdown, Vector3D, double> _massBreakdown = null;

        #endregion

        #region Constructor

        public ConverterEnergyToPlasmaDesign(EditorOptions options)
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

        public override CollisionHull CreateCollisionHull(WorldBase world)
        {
            return ConverterEnergyToFuelDesign.CreateCollisionHull(world, this.Scale, this.Orientation, this.Position);
        }

        public override UtilityNewt.IObjectMassBreakdown GetMassBreakdown(double cellSize)
        {
            if (_massBreakdown != null && _massBreakdown.Item2 == this.Scale && _massBreakdown.Item3 == cellSize)
            {
                // This has already been built for this size
                return _massBreakdown.Item1;
            }

            var breakdown = ConverterEnergyToFuelDesign.GetMassBreakdown(this.Scale, cellSize);

            // Store this
            _massBreakdown = new Tuple<UtilityNewt.IObjectMassBreakdown, Vector3D, double>(breakdown, this.Scale, cellSize);

            // Exit Function
            return _massBreakdown.Item1;
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
                WorldColors.ConverterBase, WorldColors.ConverterBaseSpecular, WorldColors.ConverterPlasma, WorldColors.ConverterPlasmaSpecular,
                isFinal);
        }

        #endregion
    }

    #endregion
    #region Class: ConverterEnergyToPlasma

    public class ConverterEnergyToPlasma : PartBase, IPartUpdatable
    {
        #region Declaration Section

        public const string PARTTYPE = "ConverterEnergyToPlasma";

        private ItemOptions _itemOptions = null;

        private Converter _converter = null;

        #endregion

        #region Constructor

        /// <summary>
        /// NOTE: It's assumed that energyTanks and plasmaTanks are actually container groups holding the actual tanks, but it
        /// could be the tanks passed in directly
        /// </summary>
        public ConverterEnergyToPlasma(EditorOptions options, ItemOptions itemOptions, ShipPartDNA dna, IContainer energyTanks, IContainer plasmaTanks)
            : base(options, dna)
        {
            _itemOptions = itemOptions;

            this.Design = new ConverterEnergyToPlasmaDesign(options);
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
            this.Transfer(elapsedTime, 1);
        }

        public int? IntervalSkips_MainThread
        {
            get
            {
                return null;
            }
        }
        public int? IntervalSkips_AnyThread
        {
            get
            {
                return 0;
            }
        }

        #endregion

        #region Public Properties

        private double _mass = 0d;
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

        private Vector3D _scaleActual;
        public override Vector3D ScaleActual
        {
            get
            {
                return _scaleActual;
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

        #endregion
    }

    #endregion
}
