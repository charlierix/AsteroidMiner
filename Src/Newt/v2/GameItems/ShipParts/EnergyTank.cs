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
    #region class: EnergyTankToolItem

    public class EnergyTankToolItem : PartToolItemBase
    {
        #region Constructor

        public EnergyTankToolItem(EditorOptions options)
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
                return "Energy Tank";
            }
        }
        public override string Description
        {
            get
            {
                return "Stores energy";
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
            return new EnergyTankDesign(this.Options, false);
        }

        #endregion
    }

    #endregion
    #region class: EnergyTankDesign

    public class EnergyTankDesign : PartDesignBase
    {
        #region Declaration Section

        internal const double RADIUSPERCENTOFSCALE = 0.3030303030303d;		// when x scale is 1, the x radius will be this (x and y are the same)

        public const PartDesignAllowedScale ALLOWEDSCALE = PartDesignAllowedScale.XY_Z;		// This is here so the scale can be known through reflection

        private MassBreakdownCache _massBreakdown = null;

        #endregion

        #region Constructor

        public EnergyTankDesign(EditorOptions options, bool isFinalModel)
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

        private GeometryModel3D _model = null;
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
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90)));		// the physics hull is along x, but dna is along z
            transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(this.Orientation)));
            transform.Children.Add(new TranslateTransform3D(this.Position.ToVector()));

            Vector3D scale = this.Scale;
            double radius = RADIUSPERCENTOFSCALE * ((scale.X + scale.Y) * .5d);
            double height = scale.Z;

            return CollisionHull.CreateCylinder(world, 0, radius, height, transform.Value);
            //return CollisionHull.CreateChamferCylinder(world, 0, radius, height, transform.Value);		//Chamfer collides weird when it's tall and skinny
        }

        public override UtilityNewt.IObjectMassBreakdown GetMassBreakdown(double cellSize)
        {
            if (_massBreakdown != null && _massBreakdown.Scale == Scale && _massBreakdown.CellSize == cellSize)
            {
                // This has already been built for this size
                return _massBreakdown.Breakdown;
            }

            // Convert this.Scale into a size that the mass breakdown will use (mass breakdown wants height along X, and scale is for radius, but the mass breakdown wants diameter
            // Reducing Z a bit, because the energy tank has a rounded cap
            Vector3D size = new Vector3D(this.Scale.Z * .92d, this.Scale.X * RADIUSPERCENTOFSCALE * 2d, this.Scale.Y * RADIUSPERCENTOFSCALE * 2d);

            // Cylinder
            UtilityNewt.ObjectMassBreakdown cylinder = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Cylinder, UtilityNewt.MassDistribution.Uniform, size, cellSize);

            Transform3D transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90));		// the physics hull is along x, but dna is along z

            // Rotated
            UtilityNewt.ObjectMassBreakdownSet combined = new UtilityNewt.ObjectMassBreakdownSet(
                new UtilityNewt.ObjectMassBreakdown[] { cylinder },
                new Transform3D[] { transform });

            // Store this
            _massBreakdown = new MassBreakdownCache(combined, Scale, cellSize);

            return _massBreakdown.Breakdown;
        }

        public override PartToolItemBase GetToolItem()
        {
            return new EnergyTankToolItem(this.Options);
        }

        #endregion

        #region Private Methods

        private GeometryModel3D CreateGeometry(bool isFinal)
        {
            MaterialGroup material = new MaterialGroup();
            DiffuseMaterial diffuse = new DiffuseMaterial(new SolidColorBrush(WorldColors.EnergyTank_Color));
            this.MaterialBrushes.Add(new MaterialColorProps(diffuse, WorldColors.EnergyTank_Color));
            material.Children.Add(diffuse);
            SpecularMaterial specular = WorldColors.EnergyTank_Specular;
            this.MaterialBrushes.Add(new MaterialColorProps(specular));
            material.Children.Add(specular);
            EmissiveMaterial emissive = WorldColors.EnergyTank_Emissive;
            this.MaterialBrushes.Add(new MaterialColorProps(emissive));
            material.Children.Add(emissive);

            if (!isFinal)
            {
                EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
                material.Children.Add(selectionEmissive);
                base.SelectionEmissives.Add(selectionEmissive);
            }

            GeometryModel3D retVal = new GeometryModel3D();
            retVal.Material = material;
            retVal.BackMaterial = material;

            int domeSegments = isFinal ? 2 : 10;
            int cylinderSegments = isFinal ? 6 : 35;

            List<TubeRingBase> rings = new List<TubeRingBase>();
            rings.Add(new TubeRingDome(0, false, domeSegments));
            rings.Add(new TubeRingRegularPolygon(.4, false, 1, 1, false));
            rings.Add(new TubeRingRegularPolygon(2.5, false, 1, 1, false));
            rings.Add(new TubeRingDome(.4, false, domeSegments));

            // Scale this so the height is 1
            double scale = 1d / rings.Sum(o => o.DistFromPrevRing);

            Transform3DGroup transformInitial = new Transform3DGroup();
            transformInitial.Children.Add(new ScaleTransform3D(scale, scale, scale));
            retVal.Geometry = UtilityWPF.GetMultiRingedTube(cylinderSegments, rings, true, true, transformInitial);

            // Transform
            retVal.Transform = GetTransformForGeometry(isFinal);

            // Exit Function
            return retVal;
        }

        #endregion
    }

    #endregion
    #region class: EnergyTank

    /// <summary>
    /// This is the real world object that physically holds energy, has mass takes impact damage, etc
    /// </summary>
    public class EnergyTank : PartBase, IContainer, INeuronContainer, IPartUpdatable
    {
        #region Declaration Section

        public const string PARTTYPE = nameof(EnergyTank);

        private readonly Container _container;

        private readonly ItemOptions _itemOptions;

        private readonly Neuron_SensorPosition _neuron;

        #endregion

        #region Constructor

        public EnergyTank(EditorOptions options, ItemOptions itemOptions, ShipPartDNA dna)
            : base(options, dna, itemOptions.EnergyTank_Damage.HitpointMin, itemOptions.EnergyTank_Damage.HitpointSlope, itemOptions.EnergyTank_Damage.Damage)
        {
            _itemOptions = itemOptions;

            this.Design = new EnergyTankDesign(options, true);
            this.Design.SetDNA(dna);

            double radius;
            _container = GetContainer(out _scaleActual, out radius, itemOptions, dna);

            this.Radius = radius;

            _mass = _container.QuantityMax * itemOptions.EnergyTank_Density;

            _neuron = new Neuron_SensorPosition(new Point3D(0, 0, 0), false);

            this.Destroyed += EnergyTank_Destroyed;
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
                throw new NotSupportedException("The container max can't be set directly.  It is derived from the dna.scale");
                //_container.QuantityMax = value;
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

            return _container.AddQuantity(amount, exactAmountOnly);
        }
        public double AddQuantity(IContainer pullFrom, double amount, bool exactAmountOnly)
        {
            if (this.IsDestroyed)
            {
                return amount;
            }

            return _container.AddQuantity(pullFrom, amount, exactAmountOnly);
        }
        public double AddQuantity(IContainer pullFrom, bool exactAmountOnly)
        {
            if (this.IsDestroyed)
            {
                return pullFrom.QuantityCurrent;
            }

            return _container.AddQuantity(pullFrom, exactAmountOnly);
        }

        public double RemoveQuantity(double amount, bool exactAmountOnly)
        {
            return _container.RemoveQuantity(amount, exactAmountOnly);
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
            _neuron.Value = UtilityCore.GetScaledValue_Capped(-1d, 1d, 0d, _container.QuantityMax, _container.QuantityCurrent);       // Don't want to bother with a lock here, the only variable that changes is _container.QuantityCurrent
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

        private void EnergyTank_Destroyed(object sender, EventArgs e)
        {
            _container.QuantityCurrent = 0;
        }

        #endregion

        #region Private Methods

        private static Container GetContainer(out Vector3D actualScale, out double radius, ItemOptions itemOptions, ShipPartDNA dna)
        {
            Container retVal = new Container();

            retVal.OnlyRemoveMultiples = false;

            // Even though it has slightly rounded endcaps, I'll assume it's just a cylinder

            double radX = dna.Scale.X * EnergyTankDesign.RADIUSPERCENTOFSCALE;
            double radY = dna.Scale.Y * EnergyTankDesign.RADIUSPERCENTOFSCALE;
            double height = dna.Scale.Z;

            double cylinderVolume = Math.PI * radX * radY * height;

            retVal.QuantityMax = cylinderVolume;

            actualScale = new Vector3D(radX * 2d, radY * 2d, height);

            // Exit Function
            radius = (radX + radY + height) / 3d;       // this is just approximate, and is used by INeuronContainer
            return retVal;
        }

        #endregion
    }

    #endregion
}
