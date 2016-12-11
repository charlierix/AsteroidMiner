using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Media3D;

using Game.Newt.v2.GameItems.ShipEditor;
using Game.HelperClassesWPF;
using Game.Newt.v2.NewtonDynamics;
using Game.HelperClassesCore;

namespace Game.Newt.v2.GameItems.ShipParts
{
    #region Class: ConverterMatterToEnergyToolItem

    public class ConverterMatterToEnergyToolItem : PartToolItemBase
    {
        #region Constructor

        public ConverterMatterToEnergyToolItem(EditorOptions options)
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
                return "Matter to Energy Converter";
            }
        }
        public override string Description
        {
            get
            {
                return "Pulls matter out of the cargo bay, and recharges the energy tank";
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
            return new ConverterMatterToEnergyDesign(this.Options, false);
        }

        #endregion
    }

    #endregion
    #region Class: ConverterMatterToEnergyDesign

    public class ConverterMatterToEnergyDesign : PartDesignBase
    {
        #region Declaration Section

        public const PartDesignAllowedScale ALLOWEDSCALE = PartDesignAllowedScale.XYZ;		// This is here so the scale can be known through reflection

        private Tuple<UtilityNewt.IObjectMassBreakdown, Vector3D, double> _massBreakdown = null;

        #endregion

        #region Constructor

        public ConverterMatterToEnergyDesign(EditorOptions options, bool isFinalModel)
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
            Transform3DGroup transform = new Transform3DGroup();
            //transform.Children.Add(new ScaleTransform3D(this.Scale));		// it ignores scale
            transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(this.Orientation)));
            transform.Children.Add(new TranslateTransform3D(this.Position.ToVector()));

            return CollisionHull.CreateBox(world, 0, this.Scale * ConverterMatterToFuelDesign.SCALE, transform.Value);
        }
        public override UtilityNewt.IObjectMassBreakdown GetMassBreakdown(double cellSize)
        {
            if (_massBreakdown != null && _massBreakdown.Item2 == this.Scale && _massBreakdown.Item3 == cellSize)
            {
                // This has already been built for this size
                return _massBreakdown.Item1;
            }

            // Convert this.Scale into a size that the mass breakdown will use
            Vector3D size = this.Scale * ConverterMatterToFuelDesign.SCALE;

            var breakdown = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Box, UtilityNewt.MassDistribution.Uniform, size, cellSize);

            // Store this
            _massBreakdown = new Tuple<UtilityNewt.IObjectMassBreakdown, Vector3D, double>(breakdown, this.Scale, cellSize);

            // Exit Function
            return _massBreakdown.Item1;
        }

        public override PartToolItemBase GetToolItem()
        {
            return new ConverterMatterToEnergyToolItem(this.Options);
        }

        #endregion

        #region Private Methods

        private Model3DGroup CreateGeometry(bool isFinal)
        {
            return ConverterMatterToFuelDesign.CreateGeometry(this.MaterialBrushes, base.SelectionEmissives,
                GetTransformForGeometry(isFinal),
                WorldColors.ConverterBase, WorldColors.ConverterBaseSpecular, WorldColors.ConverterEnergy, WorldColors.ConverterEnergySpecular,
                isFinal);
        }

        #endregion
    }

    #endregion
    #region Class: ConverterMatterToEnergy

    public class ConverterMatterToEnergy : PartBase, IPartUpdatable, IContainer, IConverterMatter
    {
        #region Declaration Section

        public const string PARTTYPE = "ConverterMatterToEnergy";

        private readonly object _lock = new object();

        private ItemOptions _itemOptions = null;

        private IContainer _energyTanks = null;

        // This stays sorted from low to high density
        private List<Cargo> _cargo = new List<Cargo>();

        private Converter _converter = null;

        #endregion

        #region Constructor

        public ConverterMatterToEnergy(EditorOptions options, ItemOptions itemOptions, ShipPartDNA dna, IContainer energyTanks)
            : base(options, dna, itemOptions.MatterConverter_Damage.HitpointMin, itemOptions.MatterConverter_Damage.HitpointSlope, itemOptions.MatterConverter_Damage.Damage)
        {
            _itemOptions = itemOptions;
            _energyTanks = energyTanks;

            this.Design = new ConverterMatterToEnergyDesign(options, true);
            this.Design.SetDNA(dna);

            double volume;
            ConverterMatterToFuel.GetMass(out _dryMass, out volume, out _scaleActual, _itemOptions, dna);

            this.MaxVolume = volume;

            if (_energyTanks != null)
            {
                double scaleVolume = _scaleActual.X * _scaleActual.Y * _scaleActual.Z;      // can't use volume from above, because that is the amount of matter that can be held.  This is to get conversion ratios
                _converter = new Converter(this, _energyTanks, _itemOptions.MatterToEnergy_ConversionRate, _itemOptions.MatterToEnergy_AmountToDraw * scaleVolume);
            }

            this.Destroyed += ConverterMatterToEnergy_Destroyed;
        }

        #endregion

        #region IPartUpdatable Members

        public void Update_MainThread(double elapsedTime)
        {
        }
        public void Update_AnyThread(double elapsedTime)
        {
            lock (_lock)
            {
                if (!this.IsDestroyed && _converter != null && _cargo.Count > 0)
                {
                    _converter.Transfer(elapsedTime);
                }
            }
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
        #region IContainer Members

        // IContainer was only implemented so that _converter could pull from this class's cargo
        //NOTE: The converter only cares about mass, so the quantities that these IContainer methods deal with are mass.  The other methods in this class are more worried about volume

        public double QuantityCurrent
        {
            get
            {
                lock (_lock)
                {
                    return _cargo.Sum(o => o.Density * o.Volume);
                }
            }
            set
            {
                throw new NotImplementedException();
            }
        }
        public double QuantityMax
        {
            get
            {
                // This class isn't constrained by mass, only volume, so this property has no meaning
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }
        public double QuantityMax_Usable
        {
            get { throw new NotImplementedException(); }
        }

        public double QuantityMaxMinusCurrent
        {
            get { throw new NotImplementedException(); }
        }
        public double QuantityMaxMinusCurrent_Usable
        {
            get { throw new NotImplementedException(); }
        }

        public bool OnlyRemoveMultiples
        {
            get
            {
                return false;
            }
            set
            {
                throw new NotImplementedException();
            }
        }
        public double RemovalMultiple
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public double AddQuantity(double amount, bool exactAmountOnly)
        {
            throw new NotImplementedException();
        }
        public double AddQuantity(IContainer pullFrom, double amount, bool exactAmountOnly)
        {
            throw new NotImplementedException();
        }
        public double AddQuantity(IContainer pullFrom, bool exactAmountOnly)
        {
            throw new NotImplementedException();
        }

        public double RemoveQuantity(double amount, bool exactAmountOnly)
        {
            lock (_lock)
            {
                if (exactAmountOnly)
                {
                    throw new ArgumentException("exactAmountOnly cannot be true");
                }

                return ConverterMatterToFuel.RemoveQuantity(amount, _cargo);
            }
        }

        #endregion
        #region IConverterMatter Members

        //NOTE: There is only an add method.  Any cargo added to this converter is burned off over time
        public bool Add(Cargo cargo)
        {
            lock (_lock)
            {
                //double sumVolume = this.UsedVolume + cargo.Volume;
                double sumVolume = _cargo.Sum(o => o.Volume) + cargo.Volume;        // inlined this.UsedVolume because of the lock

                if (sumVolume <= this.MaxVolume || Math1D.IsNearValue(sumVolume, this.MaxVolume))
                {
                    ConverterMatterToFuel.Add(_cargo, cargo);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public double UsedVolume
        {
            get
            {
                lock (_lock)
                {
                    return _cargo.Sum(o => o.Volume);
                }
            }
        }
        public double MaxVolume
        {
            get;
            private set;
        }

        #endregion

        #region Public Properties

        private readonly double _dryMass;
        public override double DryMass
        {
            get
            {
                return _dryMass;
            }
        }
        public override double TotalMass
        {
            get
            {
                lock (_lock)
                {
                    return _dryMass + _cargo.Sum(o => o.Density * o.Volume);
                }
            }
        }

        private readonly Vector3D _scaleActual;
        public override Vector3D ScaleActual
        {
            get
            {
                return _scaleActual;
            }
        }

        #endregion

        #region Event Listeners

        private void ConverterMatterToEnergy_Destroyed(object sender, EventArgs e)
        {
            lock (_lock)
            {
                _cargo.Clear();
            }
        }

        #endregion
    }

    #endregion
}
