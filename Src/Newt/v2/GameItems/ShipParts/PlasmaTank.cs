using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using Game.HelperClassesCore;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.HelperClassesWPF;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.GameItems.ShipParts
{
    #region class: PlasmaTankToolItem

    public class PlasmaTankToolItem : PartToolItemBase
    {
        #region Constructor

        public PlasmaTankToolItem(EditorOptions options)
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
                return "Plasma Tank";
            }
        }
        public override string Description
        {
            get
            {
                return "Stores plasma (used by high energy parts)";
            }
        }
        public override string Category
        {
            get
            {
                return PartToolItemBase.CATEGORY_CONTAINER;
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
            return new PlasmaTankDesign(this.Options, false);
        }

        #endregion
    }

    #endregion
    #region class: PlasmaTankDesign

    public class PlasmaTankDesign : PartDesignBase
    {
        #region Declaration Section

        internal const double RADIUSPERCENTOFSCALE = FuelTankDesign.RADIUSPERCENTOFSCALE;		// when x scale is 1, the x radius will be this (x and y are the same)

        public const PartDesignAllowedScale ALLOWEDSCALE = PartDesignAllowedScale.XY_Z;		// This is here so the scale can be known through reflection

        private MassBreakdownCache _massBreakdown = null;

        #endregion

        #region Constructor

        public PlasmaTankDesign(EditorOptions options, bool isFinalModel)
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

        private Model3D _model = null;
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
            return FuelTankDesign.CreateTankCollisionHull(world, this.Scale, this.Orientation, this.Position);
        }
        public override UtilityNewt.IObjectMassBreakdown GetMassBreakdown(double cellSize)
        {
            return FuelTankDesign.GetTankMassBreakdown(ref _massBreakdown, this.Scale, cellSize);
        }

        public override PartToolItemBase GetToolItem()
        {
            return new PlasmaTankToolItem(this.Options);
        }

        #endregion

        #region Private Methods

        private Model3D CreateGeometry(bool isFinal)
        {
            return FuelTankDesign.CreateGeometry(this.MaterialBrushes, base.SelectionEmissives,
                GetTransformForGeometry(isFinal),
                WorldColors.PlasmaTank_Color, WorldColors.PlasmaTank_Specular,
                isFinal);
        }

        #endregion
    }

    #endregion
    #region class: PlasmaTank

    /// <summary>
    /// This is used to power high energy equipment (shields, tractor, beam weapon)
    /// </summary>
    /// <remarks>
    /// It's probably more accurate to call this a plasma magnetic bottle, but I wanted to keep it named similar to the
    /// fuel tank and energy tank
    /// 
    /// I decided to make the high energy parts use something other than energy tank, so that the energy tank can be
    /// used for standard ship operations.  The high energy parts would have needed so much more energy to operate
    /// that they would have quickly depleted the standard energy tanks.
    /// </remarks>
    public class PlasmaTank : PartBase, IContainer, INeuronContainer, IPartUpdatable
    {
        #region Declaration Section

        public const string PARTTYPE = nameof(PlasmaTank);

        private Container _container = null;

        private ItemOptions _itemOptions = null;

        private Neuron_SensorPosition _neuron = null;

        #endregion

        #region Constructor

        public PlasmaTank(EditorOptions options, ItemOptions itemOptions, ShipPartDNA dna)
            : base(options, dna, itemOptions.PlasmaTank_Damage.HitpointMin, itemOptions.PlasmaTank_Damage.HitpointSlope, itemOptions.PlasmaTank_Damage.Damage)
        {
            _itemOptions = itemOptions;

            this.Design = new PlasmaTankDesign(options, true);
            this.Design.SetDNA(dna);

            double surfaceArea, radius;
            _container = FuelTank.GetContainer(out surfaceArea, out _scaleActual, out radius, itemOptions, dna);

            _mass = _container.QuantityMax * itemOptions.PlasmaTank_Density;     // max quantity is the volume
            this.Radius = radius;

            _neuron = new Neuron_SensorPosition(new Point3D(0, 0, 0), false);

            this.Destroyed += PlasmaTank_Destroyed;
        }

        #endregion

        #region IContainer Members

        public double QuantityCurrent
        {
            get
            {
                return _container.QuantityCurrent;
            }
            set
            {
                _container.QuantityCurrent = value;
            }
        }
        public double QuantityMax
        {
            get
            {
                return _container.QuantityMax;
            }
            set
            {
                //_container.QuantityMax = value;
                throw new NotSupportedException("The container max can't be set directly.  It is derived from the dna.scale");
            }
        }
        public double QuantityMax_Usable
        {
            get
            {
                if (this.IsDestroyed)
                {
                    return 0d;
                }
                else
                {
                    return _container.QuantityMax;
                }
            }
        }

        public double QuantityMaxMinusCurrent
        {
            get
            {
                return _container.QuantityMaxMinusCurrent;
            }
        }
        public double QuantityMaxMinusCurrent_Usable
        {
            get
            {
                if (this.IsDestroyed)
                {
                    return 0d;
                }
                else
                {
                    return _container.QuantityMaxMinusCurrent;
                }
            }
        }

        public bool OnlyRemoveMultiples
        {
            get
            {
                return false;
            }
            set
            {
                if (value)
                {
                    throw new NotSupportedException("This property can only be false");
                }
            }
        }
        public double RemovalMultiple
        {
            get
            {
                return 0d;
            }
            set
            {
                throw new NotSupportedException("This doesn't support removal multiples");
            }
        }

        public double AddQuantity(double amount, bool exactAmountOnly)
        {
            if (this.IsDestroyed)
            {
                return amount;
            }

            double retVal = _container.AddQuantity(amount, exactAmountOnly);

            return retVal;
        }
        public double AddQuantity(IContainer pullFrom, double amount, bool exactAmountOnly)
        {
            if (this.IsDestroyed)
            {
                return amount;
            }

            double retVal = _container.AddQuantity(pullFrom, amount, exactAmountOnly);

            return retVal;
        }
        public double AddQuantity(IContainer pullFrom, bool exactAmountOnly)
        {
            if (this.IsDestroyed)
            {
                return pullFrom.QuantityCurrent;
            }

            double prevQuantity = this.QuantityCurrent;

            double retVal = _container.AddQuantity(pullFrom, exactAmountOnly);

            return retVal;
        }

        public double RemoveQuantity(double amount, bool exactAmountOnly)
        {
            double retVal = _container.RemoveQuantity(amount, exactAmountOnly);

            return retVal;
        }

        #endregion
        #region INeuronContainer Members

        public IEnumerable<INeuron> Neruons_Readonly => new INeuron[] { _neuron };
        public IEnumerable<INeuron> Neruons_ReadWrite => Enumerable.Empty<INeuron>();
        public IEnumerable<INeuron> Neruons_Writeonly => Enumerable.Empty<INeuron>();

        public IEnumerable<INeuron> Neruons_All => new INeuron[] { _neuron };

        public double Radius
        {
            get;
            private set;
        }

        public NeuronContainerType NeuronContainerType => NeuronContainerType.Sensor;

        // This is a basic container that doesn't consume energy, so is always "on"
        public bool IsOn => true;

        #endregion
        #region IPartUpdatable Members

        public void Update_MainThread(double elapsedTime)
        {
        }
        public void Update_AnyThread(double elapsedTime)
        {
            // -1 when empty, 1 when full
            _neuron.Value = UtilityCore.GetScaledValue_Capped(-1d, 1d, 0d, _container.QuantityMax, _container.QuantityCurrent);
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

        #region Event Listeners

        private void PlasmaTank_Destroyed(object sender, EventArgs e)
        {
            _container.QuantityCurrent = 0;
        }

        #endregion
    }

    #endregion
}
