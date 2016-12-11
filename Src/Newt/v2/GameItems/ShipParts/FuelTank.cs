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
    #region Class: FuelTankToolItem

    public class FuelTankToolItem : PartToolItemBase
    {
        #region Constructor

        public FuelTankToolItem(EditorOptions options)
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
                return "Fuel Tank";
            }
        }
        public override string Description
        {
            get
            {
                return "Stores fuel";
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
            return new FuelTankDesign(this.Options, false);
        }

        #endregion
    }

    #endregion
    #region Class: FuelTankDesign

    public class FuelTankDesign : PartDesignBase
    {
        #region Declaration Section

        internal const double RADIUSPERCENTOFSCALE = .25d;		// when x scale is 1, the x radius will be this (x and y are the same)

        public const PartDesignAllowedScale ALLOWEDSCALE = PartDesignAllowedScale.XY_Z;		// This is here so the scale can be known through reflection

        private Tuple<UtilityNewt.IObjectMassBreakdown, Vector3D, double> _massBreakdown = null;

        #endregion

        #region Constructor

        public FuelTankDesign(EditorOptions options, bool isFinalModel)
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
            return CreateTankCollisionHull(world, this.Scale, this.Orientation, this.Position);
        }
        public override UtilityNewt.IObjectMassBreakdown GetMassBreakdown(double cellSize)
        {
            return GetTankMassBreakdown(ref _massBreakdown, this.Scale, cellSize);
        }

        internal static CollisionHull CreateTankCollisionHull(WorldBase world, Vector3D scale, Quaternion orientation, Point3D position)
        {
            Transform3DGroup transform = new Transform3DGroup();
            //transform.Children.Add(new ScaleTransform3D(this.Scale));		// it ignores scale
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90)));		// the physics hull is along x, but dna is along z
            transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(orientation)));
            transform.Children.Add(new TranslateTransform3D(position.ToVector()));

            double radius = RADIUSPERCENTOFSCALE * (scale.X + scale.Y) * .5d;
            double height = scale.Z;

            if (height < radius * 2d)
            {
                // Newton keeps the capsule caps spherical, but the visual scales them.  So when the height is less than the radius, newton
                // make a sphere.  So just make a cylinder instead
                //return CollisionHull.CreateChamferCylinder(world, 0, radius, height, transform.Value);
                return CollisionHull.CreateCylinder(world, 0, radius, height, transform.Value);
            }
            else
            {
                //NOTE: The visual changes the caps around, but I want the physics to be a capsule
                return CollisionHull.CreateCapsule(world, 0, radius, height, transform.Value);
            }
        }
        internal static UtilityNewt.IObjectMassBreakdown GetTankMassBreakdown(ref Tuple<UtilityNewt.IObjectMassBreakdown, Vector3D, double> existing, Vector3D scale, double cellSize)
        {
            if (existing != null && existing.Item2 == scale && existing.Item3 == cellSize)
            {
                // This has already been built for this size
                return existing.Item1;
            }

            // Convert this.Scale into a size that the mass breakdown will use (mass breakdown wants height along X, and scale is for radius, but the mass breakdown wants diameter
            Vector3D size = new Vector3D(scale.Z, scale.X * RADIUSPERCENTOFSCALE * 2d, scale.Y * RADIUSPERCENTOFSCALE * 2d);

            // Main Cylinder
            Vector3D mainSize = new Vector3D(size.X * .75d, size.Y, size.Z);
            double mainVolume = Math.Pow((mainSize.Y + mainSize.Z) / 4d, 2d) * Math.PI * mainSize.X;		// dividing by 4, because div 2 is the average, then another 2 is to convert diameter to radius
            var mainCylinder = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Cylinder, UtilityNewt.MassDistribution.Uniform, mainSize, cellSize);

            // End Caps
            Vector3D capSize = new Vector3D(size.X * .125d, size.Y * .5d, size.Z * .5d);
            double capVolume = Math.Pow((capSize.Y + capSize.Z) / 4d, 2d) * Math.PI * capSize.X;
            var capCylinder = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Cylinder, UtilityNewt.MassDistribution.Uniform, capSize, cellSize);

            // Combined shape
            double offsetX = (mainSize.X * .5d) + (capSize.X * .5d);
            Quaternion rotate = new Quaternion(new Vector3D(0, 1, 0), 90);

            var objects = new Tuple<UtilityNewt.ObjectMassBreakdown, Point3D, Quaternion, double>[3];
            objects[0] = new Tuple<UtilityNewt.ObjectMassBreakdown, Point3D, Quaternion, double>(mainCylinder, new Point3D(0, 0, 0), rotate, mainVolume);
            objects[1] = new Tuple<UtilityNewt.ObjectMassBreakdown, Point3D, Quaternion, double>(capCylinder, new Point3D(0, 0, offsetX), rotate, capVolume);		// the breakdowns were build along X, but now putting them back along Z
            objects[2] = new Tuple<UtilityNewt.ObjectMassBreakdown, Point3D, Quaternion, double>(capCylinder, new Point3D(0, 0, -offsetX), rotate, capVolume);
            var combined = UtilityNewt.Combine(objects);

            // Store this
            existing = new Tuple<UtilityNewt.IObjectMassBreakdown, Vector3D, double>(combined, scale, cellSize);

            // Exit Function
            return existing.Item1;
        }

        public override PartToolItemBase GetToolItem()
        {
            return new FuelTankToolItem(this.Options);
        }

        #endregion

        #region Private Methods

        private Model3D CreateGeometry(bool isFinal)
        {
            return CreateGeometry(this.MaterialBrushes, base.SelectionEmissives,
                GetTransformForGeometry(isFinal),
                WorldColors.FuelTank, WorldColors.FuelTankSpecular,
                isFinal);
        }

        internal static Model3D CreateGeometry(List<MaterialColorProps> materialBrushes, List<EmissiveMaterial> selectionEmissives, Transform3D transform, Color color, SpecularMaterial specular, bool isFinal)
        {
            // Material
            MaterialGroup material = new MaterialGroup();
            DiffuseMaterial diffuse = new DiffuseMaterial(new SolidColorBrush(color));
            materialBrushes.Add(new MaterialColorProps(diffuse, color));
            material.Children.Add(diffuse);
            materialBrushes.Add(new MaterialColorProps(specular));
            material.Children.Add(specular);

            if (!isFinal)
            {
                EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
                material.Children.Add(selectionEmissive);
                selectionEmissives.Add(selectionEmissive);
            }

            // Geometry Model
            GeometryModel3D retVal = new GeometryModel3D();
            retVal.Material = material;
            retVal.BackMaterial = material;

            int domeSegments = isFinal ? 2 : 10;
            int cylinderSegments = isFinal ? 6 : 35;

            retVal.Geometry = UtilityWPF.GetCapsule_AlongZ(cylinderSegments, domeSegments, RADIUSPERCENTOFSCALE, 1d);

            // Transform
            retVal.Transform = transform;

            // Exit Function
            return retVal;
        }

        #endregion
    }

    #endregion
    #region Class: FuelTank

    public class FuelTank : PartBase, IContainer, INeuronContainer, IPartUpdatable
    {
        #region Declaration Section

        public const string PARTTYPE = "FuelTank";

        private readonly object _lock = new object();

        private readonly Container _container;

        private readonly ItemOptions _itemOptions;

        private readonly Neuron_SensorPosition _neuron;

        #endregion

        #region Constructor

        public FuelTank(EditorOptions options, ItemOptions itemOptions, ShipPartDNA dna)
            : base(options, dna, itemOptions.FuelTank_Damage.HitpointMin, itemOptions.FuelTank_Damage.HitpointSlope, itemOptions.FuelTank_Damage.Damage)
        {
            _itemOptions = itemOptions;

            this.Design = new FuelTankDesign(options, true);
            this.Design.SetDNA(dna);

            double surfaceArea, radius;
            _container = GetContainer(out surfaceArea, out _scaleActual, out radius, itemOptions, dna);

            _dryMass = surfaceArea * itemOptions.FuelTank_WallDensity;
            this.Radius = radius;

            _neuron = new Neuron_SensorPosition(new Point3D(0, 0, 0), false);

            this.Destroyed += FuelTank_Destroyed;
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
                base.OnMassChanged();
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

            double retVal = _container.AddQuantity(amount, exactAmountOnly);        // no need for a lock, this statement is atomic

            if (retVal < amount)
            {
                OnMassChanged();        // this can't be raised from within a lock
            }

            return retVal;
        }
        public double AddQuantity(IContainer pullFrom, double amount, bool exactAmountOnly)
        {
            if (this.IsDestroyed)
            {
                return amount;
            }

            double retVal = _container.AddQuantity(pullFrom, amount, exactAmountOnly);      // no need for a lock, this statement is atomic

            if (retVal < amount)
            {
                OnMassChanged();        // this can't be raised from within a lock
            }

            return retVal;
        }
        public double AddQuantity(IContainer pullFrom, bool exactAmountOnly)
        {
            if (this.IsDestroyed)
            {
                return pullFrom.QuantityCurrent;
            }

            bool shouldRaiseEvent = false;
            double retVal;

            lock (_lock)
            {
                double prevQuantity = this.QuantityCurrent;

                retVal = _container.AddQuantity(pullFrom, exactAmountOnly);

                if (this.QuantityCurrent > prevQuantity)
                {
                    shouldRaiseEvent = true;
                }
            }

            if (shouldRaiseEvent)
            {
                OnMassChanged();        // this can't be raised from within the lock
            }

            return retVal;
        }

        public double RemoveQuantity(double amount, bool exactAmountOnly)
        {
            double retVal = _container.RemoveQuantity(amount, exactAmountOnly);     // no need for a lock, this statement is atomic

            if (retVal < amount)
            {
                OnMassChanged();        // this can't be raised from within a lock
            }

            return retVal;
        }

        #endregion
        #region INeuronContainer Members

        public IEnumerable<INeuron> Neruons_Readonly
        {
            get
            {
                return new INeuron[] { _neuron };
            }
        }
        public IEnumerable<INeuron> Neruons_ReadWrite
        {
            get
            {
                return Enumerable.Empty<INeuron>();
            }
        }
        public IEnumerable<INeuron> Neruons_Writeonly
        {
            get
            {
                return Enumerable.Empty<INeuron>();
            }
        }

        public IEnumerable<INeuron> Neruons_All
        {
            get
            {
                return new INeuron[] { _neuron };
            }
        }

        public double Radius
        {
            get;
            private set;
        }

        public NeuronContainerType NeuronContainerType
        {
            get
            {
                return NeuronContainerType.Sensor;
            }
        }

        public bool IsOn
        {
            get
            {
                // This is a basic container that doesn't consume energy, so is always "on"
                return true;
            }
        }

        #endregion
        #region IPartUpdatable Members

        public void Update_MainThread(double elapsedTime)
        {
        }
        public void Update_AnyThread(double elapsedTime)
        {
            // -1 when empty, 1 when full
            _neuron.Value = UtilityCore.GetScaledValue_Capped(-1d, 1d, 0d, _container.QuantityMax, _container.QuantityCurrent);       // no need for a lock, max never changes
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
                return _dryMass + (_container.QuantityCurrent * _itemOptions.Fuel_Density);      // I don't want to bother with a lock.  Density is in another class and volatile, so current quantity is the only loose variable from this class
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

        private void FuelTank_Destroyed(object sender, EventArgs e)
        {
            double prev = _container.QuantityCurrent;

            _container.QuantityCurrent = 0;

            if (prev > 0)
            {
                OnMassChanged();
            }
        }

        #endregion

        #region Private Methods

        /// <remarks>
        /// Got ellipse circumference equation here:
        /// http://paulbourke.net/geometry/ellipsecirc/
        /// </remarks>
        internal static Container GetContainer(out double surfaceArea, out Vector3D actualScale, out double radius, ItemOptions itemOptions, ShipPartDNA dna)
        {
            Container retVal = new Container();

            retVal.OnlyRemoveMultiples = false;

            // The fuel tank is a cylinder with two half spheres on each end (height along z.  x and y are supposed to be the same)
            // But when it gets squeezed/stretched along z, the caps don't keep their aspect ratio.  Their height always makes up half of the total height

            #region XYZ H

            double radX = dna.Scale.X * FuelTankDesign.RADIUSPERCENTOFSCALE;
            double radY = dna.Scale.Y * FuelTankDesign.RADIUSPERCENTOFSCALE;
            double radZ = dna.Scale.Z * FuelTankDesign.RADIUSPERCENTOFSCALE;
            double height = dna.Scale.Z - (radZ * 2);
            if (height < 0)
            {
                throw new ApplicationException(string.Format("height should never be zero.  ScaleZ={0}, RAD%SCALE={1}, heigth={2}", dna.Scale.Z.ToString(), FuelTankDesign.RADIUSPERCENTOFSCALE.ToString(), height.ToString()));
            }

            #endregion

            #region Volume

            double sphereVolume = 4d / 3d * Math.PI * radX * radY * radZ;

            double cylinderVolume = Math.PI * radX * radY * height;

            retVal.QuantityMax = sphereVolume + cylinderVolume;

            #endregion

            #region Surface Area

            // While I have the various radius, calculate the dry mass

            double cylinderSA = 0d;

            if (height > 0d)
            {
                // Ellipse circumference = pi (a + b) [ 1 + 3 h / (10 + (4 - 3 h)1/2 ) ]
                // (a=xrad, b=yrad, h = (a - b)^2 / (a + b)^2)  (Ramanujan, second approximation)
                double h = Math.Pow((radX - radY), 2d) / Math.Pow((radX + radY), 2d);
                double circumference = Math.PI * (radX + radY) * (1d + (3d * h) / Math.Pow(10d + (4d - (3d * h)), .5d));

                // Cylinder side surface area = ellipse circ * height
                cylinderSA = circumference * height;
            }

            // Sphere surface area = 4 pi ((a^p * b^p) + (a^p * c^p) + (b^p * c^p) / 3) ^ (1/p)
            // p=1.6075
            double p = 1.6075d;
            double a = Math.Pow(radX, p);
            double b = Math.Pow(radY, p);
            double c = Math.Pow(radZ, p);
            double sphereSA = 4 * Math.PI * Math.Pow(((a * b) + (a * c) + (b * c) / 3d), (1d / p));

            // Combine them
            surfaceArea = cylinderSA + sphereSA;

            #endregion

            actualScale = new Vector3D(radX * 2d, radY * 2d, dna.Scale.Z);

            // Exit Function
            radius = (radX + radY + height) / 3d;       // this is just approximate, and is used by INeuronContainer
            return retVal;
        }

        #endregion
    }

    #endregion
}
