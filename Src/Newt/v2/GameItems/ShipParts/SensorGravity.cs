using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.NewtonDynamics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Game.Newt.v2.GameItems.ShipParts
{
    #region class: SensorGravityToolItem

    public class SensorGravityToolItem : PartToolItemBase
    {
        #region Constructor

        public SensorGravityToolItem(EditorOptions options)
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
                return "Gravity Sensor";
            }
        }
        public override string Description
        {
            get
            {
                return "Reports how much gravity is felt (ignores other forces felt)";
            }
        }
        public override string Category
        {
            get
            {
                return PartToolItemBase.CATEGORY_SENSOR;
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
            return new SensorGravityDesign(this.Options, false);
        }

        #endregion
    }

    #endregion
    #region class: SensorGravityDesign

    public class SensorGravityDesign : PartDesignBase
    {
        #region Declaration Section

        internal const double SIZEPERCENTOFSCALE = .2d;

        public const PartDesignAllowedScale ALLOWEDSCALE = PartDesignAllowedScale.XYZ;		// This is here so the scale can be known through reflection

        private MassBreakdownCache _massBreakdown = null;

        #endregion

        #region Constructor

        public SensorGravityDesign(EditorOptions options, bool isFinalModel)
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
            return CreateSensorCollisionHull(world, this.Scale, this.Orientation, this.Position);
        }
        public override UtilityNewt.IObjectMassBreakdown GetMassBreakdown(double cellSize)
        {
            return GetSensorMassBreakdown(ref _massBreakdown, this.Scale, cellSize);
        }

        public static CollisionHull CreateSensorCollisionHull(WorldBase world, Vector3D scale, Quaternion orientation, Point3D position)
        {
            Transform3DGroup transform = new Transform3DGroup();
            //transform.Children.Add(new ScaleTransform3D(scale));		// it ignores scale
            transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(orientation)));
            transform.Children.Add(new TranslateTransform3D(position.ToVector()));

            Vector3D size = new Vector3D(SIZEPERCENTOFSCALE * scale.X, SIZEPERCENTOFSCALE * scale.Y, SIZEPERCENTOFSCALE * scale.Z);

            return CollisionHull.CreateBox(world, 0, size, transform.Value);
        }
        public static UtilityNewt.IObjectMassBreakdown GetSensorMassBreakdown(ref MassBreakdownCache existing, Vector3D scale, double cellSize)
        {
            if (existing != null && existing.Scale == scale && existing.CellSize == cellSize)
            {
                // This has already been built for this size
                return existing.Breakdown;
            }

            // Convert this.Scale into a size that the mass breakdown will use
            Vector3D size = new Vector3D(scale.X * SIZEPERCENTOFSCALE, scale.Y * SIZEPERCENTOFSCALE, scale.Z * SIZEPERCENTOFSCALE);

            var breakdown = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Box, UtilityNewt.MassDistribution.Uniform, size, cellSize);

            // Store this
            existing = new MassBreakdownCache(breakdown, scale, cellSize);

            return existing.Breakdown;
        }

        public override PartToolItemBase GetToolItem()
        {
            return new SensorGravityToolItem(this.Options);
        }

        #endregion

        #region Private Methods

        private Model3DGroup CreateGeometry(bool isFinal)
        {
            return CreateGeometry(this.MaterialBrushes, base.SelectionEmissives,
                GetTransformForGeometry(isFinal),
                WorldColors.SensorBase_Color, WorldColors.SensorBase_Specular, WorldColors.SensorGravity_Color, WorldColors.SensorGravity_Specular,
                isFinal);
        }

        public static Model3DGroup CreateGeometry(List<MaterialColorProps> materialBrushes, List<EmissiveMaterial> selectionEmissives, Transform3D transform, Color baseColor, SpecularMaterial baseSpecular, Color colorColor, SpecularMaterial colorSpecular, bool isFinal)
        {
            //NOTE: This is copied and tweaked from ConverterMatterToFuelDesign

            Model3DGroup retVal = new Model3DGroup();

            GeometryModel3D geometry;
            MaterialGroup material;
            DiffuseMaterial diffuse;
            SpecularMaterial specular;

            #region Main Cube

            geometry = new GeometryModel3D();
            material = new MaterialGroup();
            diffuse = new DiffuseMaterial(new SolidColorBrush(baseColor));
            materialBrushes.Add(new MaterialColorProps(diffuse, baseColor));
            material.Children.Add(diffuse);
            specular = baseSpecular;
            materialBrushes.Add(new MaterialColorProps(specular));
            material.Children.Add(specular);

            if (!isFinal)
            {
                EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
                material.Children.Add(selectionEmissive);
                selectionEmissives.Add(selectionEmissive);
            }

            geometry.Material = material;
            geometry.BackMaterial = material;

            geometry.Geometry = GetMeshBase(SIZEPERCENTOFSCALE);

            retVal.Children.Add(geometry);

            #endregion

            #region Color Cube

            geometry = new GeometryModel3D();
            material = new MaterialGroup();
            diffuse = new DiffuseMaterial(new SolidColorBrush(colorColor));
            materialBrushes.Add(new MaterialColorProps(diffuse, colorColor));
            material.Children.Add(diffuse);
            specular = colorSpecular;
            materialBrushes.Add(new MaterialColorProps(specular));
            material.Children.Add(specular);

            if (!isFinal)
            {
                EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
                material.Children.Add(selectionEmissive);
                selectionEmissives.Add(selectionEmissive);
            }

            geometry.Material = material;
            geometry.BackMaterial = material;

            geometry.Geometry = GetMeshColor(SIZEPERCENTOFSCALE);

            retVal.Children.Add(geometry);

            #endregion

            // Transform
            retVal.Transform = transform;

            // Exit Function
            return retVal;
        }

        public static MeshGeometry3D GetMeshBase(double scale)
        {
            //NOTE: These tips are negative
            return ConverterMatterToFuelDesign.GetMesh(.5d * scale, -.35d * scale, 1);
        }
        public static MeshGeometry3D GetMeshColor(double scale)
        {
            return ConverterMatterToFuelDesign.GetMesh(.35d * scale, .15d * scale, 1);
        }

        #endregion
    }

    #endregion
    #region class: SensorGravity

    /// <summary>
    /// Outputs how much gravity force is felt
    /// NOTE: This ignores all other forces felt (fluid, thrusters, etc)
    /// </summary>
    /// <remarks>
    /// This has a cloud of neurons, and each one's output will approach a value of one based on the dot product of its position and the current
    /// gravity vector
    /// </remarks>
    public class SensorGravity : PartBase, INeuronContainer, IPartUpdatable
    {
        #region class: MagnitudeHistory

        /// <summary>
        /// This is an implementation of newton's law of heating/cooling
        /// </summary>
        /// <remarks>
        /// Neurons can only output 0 to 1.  But the forces felt by the sensor could be just about anything.  So once per tick, and this
        /// class the current force felt (or velocity, or whatever the sensor is recording).  Then use this.CurrentMax as the maximum
        /// expected force (any force experienced greater than CurrentMax will just cause the neuron to output 1).
        /// 
        /// In heating/cooling terms, CurrentMax is the ambient temperature
        /// </remarks>
        internal class MagnitudeHistory
        {
            private readonly object _lock = new object();

            /// <summary>
            /// When first instantiated, k will start out small.  This will let _currentMax get to the average ambient force quickly.  Then
            /// k will get larger so that _currentMax stays more stable over time.  _k shouldn't get too large though, or this sensor will
            /// be unchangable
            /// </summary>
            private double _k = 1d;

            private double _maxObserved = 0d;

            private double _currentMax = 0d;
            public double CurrentMax
            {
                get
                {
                    lock (_lock)
                    {
                        return _currentMax;
                    }
                }
                set
                {
                    lock (_lock)
                    {
                        _currentMax = value;
                    }
                }
            }

            /// <summary>
            /// Call this once per tick, and it will recalculate this.CurrentMax (aka the current ambient max)
            /// </summary>
            public void StoreMagnitude(double currentStrength, double elapsedTime)
            {
                //TODO: Make some of these constants configurable

                const double LOGSTRENGTHMULT = 1.1d;
                const double MINALLOWED = .05d;
                const double MAXK = 250d;
                const double ADDK = 3d;

                lock (_lock)
                {
                    // Make the current a little bigger so that forces larger than average will still show up stronger
                    double logStrength = currentStrength * LOGSTRENGTHMULT;

                    if (currentStrength > _maxObserved)
                    {
                        // using actual instead of the inflated one
                        _maxObserved = currentStrength;
                    }

                    if (logStrength < _maxObserved * MINALLOWED)
                    {
                        // Once some large force has been observed, don't let the logged strength below some ratio of that.
                        // The reasoning for this is if a large amount of time is spent in empty space with momentary spikes
                        // of large gravity, this sensor shouldn't show minor fluctuations of gravity as major
                        logStrength = _maxObserved * MINALLOWED;
                    }

                    // Use newton's law of heating/cooling:
                    // T = T0 + Tdelta * e^-kt
                    double difference = logStrength - _currentMax;
                    _currentMax = _currentMax + (difference * Math.Pow(Math.E, -1d * _k * elapsedTime));

                    // Update k (this will make the sensor more sluggish to change)
                    if (_k < MAXK)
                    {
                        _k += elapsedTime * ADDK;

                        if (_k > MAXK)
                        {
                            _k = MAXK;
                        }
                    }
                }
            }
        }

        #endregion

        #region Declaration Section

        public const string PARTTYPE = nameof(SensorGravity);

        private readonly object _lock = new object();

        private readonly ItemOptions _itemOptions;

        private readonly IContainer _energyTanks;
        private readonly IGravityField _field;

        private readonly Neuron_SensorPosition[] _neurons;
        private readonly double _neuronMaxRadius;

        private readonly double _volume;		// this is used to calculate energy draw

        private readonly MagnitudeHistory _magnitudeHistory = new MagnitudeHistory();

        #endregion

        #region Constructor

        public SensorGravity(EditorOptions options, ItemOptions itemOptions, ShipPartDNA dna, IContainer energyTanks, IGravityField field)
            : base(options, dna, itemOptions.Sensor_Damage.HitpointMin, itemOptions.Sensor_Damage.HitpointSlope, itemOptions.Sensor_Damage.Damage)
        {
            _itemOptions = itemOptions;
            _energyTanks = energyTanks;
            _field = field;

            Design = new SensorGravityDesign(options, true);
            Design.SetDNA(dna);

            GetMass(out _mass, out _volume, out double radius, out _scaleActual, dna, itemOptions);
            Radius = radius;

            _neurons = CreateNeurons(dna, itemOptions, itemOptions.GravitySensor_NeuronDensity);
            _neuronMaxRadius = _neurons.Max(o => o.PositionLength);
        }

        #endregion

        #region INeuronContainer Members

        public IEnumerable<INeuron> Neruons_Readonly => _neurons;
        public IEnumerable<INeuron> Neruons_ReadWrite => Enumerable.Empty<INeuron>();
        public IEnumerable<INeuron> Neruons_Writeonly => Enumerable.Empty<INeuron>();

        public IEnumerable<INeuron> Neruons_All => _neurons;

        public NeuronContainerType NeuronContainerType => NeuronContainerType.Sensor;

        public double Radius
        {
            get;
            private set;
        }

        private volatile bool _isOn = false;
        public bool IsOn => _isOn;

        #endregion
        #region IPartUpdatable Members

        public void Update_MainThread(double elapsedTime)
        {
        }
        public void Update_AnyThread(double elapsedTime)
        {
            lock (_lock)
            {
                if (this.IsDestroyed || _energyTanks == null || _energyTanks.RemoveQuantity(elapsedTime * _volume * _itemOptions.GravitySensor_AmountToDraw * ItemOptions.ENERGYDRAWMULT, true) > 0d)
                {
                    // The energy tank didn't have enough
                    //NOTE: To be clean, I should set the neuron outputs to zero, but anything pulling from them should be checking this
                    //anyway.  So save the processor (also, because of threading, setting them to zero isn't as atomic as this property)
                    _isOn = false;
                    return;
                }

                _isOn = true;

                Vector3D gravity = GetGravityModelCoords();

                // Figure out the magnitude to use
                double magnitude = CalculateMagnitude(gravity.Length, elapsedTime);

                // Attempt1: Use a fixed dot product
                //UpdateNeurons_fixed(gravity, magnitude);

                // Attempt2: Scale by radius
                //UpdateNeurons_progressiveRadius(gravity, magnitude);
                UpdateNeurons(_neurons, _neuronMaxRadius, gravity, magnitude);
            }
        }

        public int? IntervalSkips_MainThread => null;
        public int? IntervalSkips_AnyThread => 0;

        #endregion

        #region Public Properties

        private readonly double _mass;
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

        private readonly Vector3D _scaleActual;
        public override Vector3D ScaleActual
        {
            get
            {
                return _scaleActual;
            }
        }

        /// <summary>
        /// This is exposed for debugging purposes.  This is the magnitude of gravity required to get a perfectly aligned neuron to
        /// fire at 1
        /// </summary>
        public double CurrentMax
        {
            get
            {
                return _magnitudeHistory.CurrentMax;
            }
            set
            {
                _magnitudeHistory.CurrentMax = value;
            }
        }

        #endregion

        #region Public Methods

        public static void GetMass(out double mass, out double volume, out double radius, out Vector3D actualScale, ShipPartDNA dna, ItemOptions itemOptions)
        {
            volume = dna.Scale.X * dna.Scale.Y * dna.Scale.Z;		// get volume of the cube
            volume *= SensorGravityDesign.SIZEPERCENTOFSCALE;		// scale it

            radius = (dna.Scale.X + dna.Scale.Y + dna.Scale.Z) / (3d * 2d);
            radius *= SensorGravityDesign.SIZEPERCENTOFSCALE;

            mass = volume * itemOptions.Sensor_Density;

            actualScale = new Vector3D(dna.Scale.X * SensorGravityDesign.SIZEPERCENTOFSCALE, dna.Scale.Y * SensorGravityDesign.SIZEPERCENTOFSCALE, dna.Scale.Z * SensorGravityDesign.SIZEPERCENTOFSCALE);
        }

        public static Neuron_SensorPosition[] CreateNeurons(ShipPartDNA dna, ItemOptions itemOptions, double neuronDensity)
        {
            // Figure out how many to make
            //NOTE: This radius isn't taking SCALE into account.  The other neural parts do this as well, so the neural density properties can be more consistent
            double radius = (dna.Scale.X + dna.Scale.Y + dna.Scale.Z) / (3d * 2d);		// xyz should all be the same anyway
            double volume = Math.Pow(radius, itemOptions.Sensor_NeuronGrowthExponent);

            int count = Convert.ToInt32(Math.Ceiling(neuronDensity * volume));
            if (count == 0)
            {
                count = 1;
            }

            // Place them evenly in a sphere
            //NOTE: An interesting side effect of calling this for each generation is that the parent may not have been perfectly evenly spaced, but calling this
            //again will slightly refine the positions
            Vector3D[] positions = NeuralUtility.GetNeuronPositions_Spherical_Even(dna.Neurons, count, radius);

            //TODO: Remove this, it's now done in getmass
            // The radius exposed through the neuron container interface needs to be reduced to actual size
            //radius *= SensorGravityDesign.SIZEPERCENTOFSCALE;

            return positions.
                Select(o => new Neuron_SensorPosition(o.ToPoint(), true)).
                ToArray();
        }

        #endregion

        #region Private Methods

        private Vector3D GetGravityModelCoords()
        {
            if (_field == null)
            {
                return new Vector3D(0, 0, 0);
            }

            var worldCoords = GetWorldLocation();

            // Calculate the difference between the world orientation and model orientation
            Quaternion toModelQuat = worldCoords.rotation.ToUnit() * this.Orientation.ToUnit();

            //TODO: See if this is needed
            //toModelQuat = new Quaternion(quatDelta.Axis, quatDelta.Angle * -1d);

            RotateTransform3D toModelTransform = new RotateTransform3D(new QuaternionRotation3D(toModelQuat));

            // Get the force of gravity in world coords
            Vector3D forceWorld = _field.GetForce(worldCoords.position);

            // Rotate the force into model coords
            return toModelTransform.Transform(forceWorld);
        }

        private double CalculateMagnitude(double currentStrength, double elapsedTime)
        {
            // Add to history
            _magnitudeHistory.StoreMagnitude(currentStrength, elapsedTime);

            // Figure out the percent
            return CalculateMagnitudePercent(currentStrength, _magnitudeHistory.CurrentMax);
        }

        internal static double CalculateMagnitudePercent(double current, double max)
        {
            if (current > max)
            {
                return 1d;
            }
            else if (Math1D.IsNearZero(max))		// can't divide by zero
            {
                if (Math1D.IsNearZero(current))
                {
                    return 0d;
                }
                else
                {
                    return 1d;
                }
            }
            else
            {
                return current / max;
            }
        }

        /// <summary>
        /// This sets all the neurons from 0 to magnitude.  Magnitude needs to be from 0 to 1, and is based on the currently felt gravity compared
        /// to what the sensor has seen as the max felt
        /// </summary>
        internal static void UpdateNeurons(Neuron_SensorPosition[] neurons, double neuronMaxRadius, Vector3D vector, double magnitude)
        {
            const double MINDOT = 1.25d;

            if (Math3D.IsNearZero(vector))
            {
                // There is no gravity to report
                for (int cntr = 0; cntr < neurons.Length; cntr++)
                {
                    neurons[cntr].Value = 0d;
                }
                return;
            }

            Vector3D gravityUnit = vector.ToUnit();

            for (int cntr = 0; cntr < neurons.Length; cntr++)
            {
                if (neurons[cntr].PositionUnit == null)
                {
                    // This neuron is sitting at 0,0,0
                    neurons[cntr].Value = magnitude;
                }
                else
                {
                    // Figure out how aligned this neuron is with the gravity vector
                    double dot = Vector3D.DotProduct(gravityUnit, neurons[cntr].PositionUnit.Value);
                    dot += 1d;		// get this to scale from 0 to 2 instead of -1 to 1

                    // Scale minDot
                    double radiusPercent = neurons[cntr].PositionLength / neuronMaxRadius;
                    double revisedMinDot = UtilityCore.GetScaledValue_Capped(0d, MINDOT, 0d, 1d, radiusPercent);

                    // Rig it so that the lower radius neurons will fire stronger
                    double minReturn = 1d - radiusPercent;
                    if (minReturn < 0d)
                    {
                        minReturn = 0d;
                    }

                    // Figure out what percentage of magnitude to use
                    double percent;
                    if (dot < revisedMinDot)
                    {
                        percent = UtilityCore.GetScaledValue_Capped(0d, minReturn, 0d, revisedMinDot, dot);
                    }
                    else
                    {
                        percent = UtilityCore.GetScaledValue_Capped(minReturn, 1d, revisedMinDot, 2d, dot);
                    }

                    // Set the neuron
                    neurons[cntr].Value = percent * magnitude;
                }
            }
        }
        private void UpdateNeurons_progressiveRadius(Vector3D gravity, double magnitude)
        {
            if (Math3D.IsNearZero(gravity))
            {
                // There is no gravity to report
                for (int cntr = 0; cntr < _neurons.Length; cntr++)
                {
                    _neurons[cntr].Value = 0d;
                }
                return;
            }

            for (int cntr = 0; cntr < _neurons.Length; cntr++)
            {
                if (_neurons[cntr].PositionUnit == null)
                {
                    // This neuron is sitting at 0,0,0
                    _neurons[cntr].Value = magnitude;
                }
                else
                {
                    // Scale minDot
                    double radiusPercent = _neurons[cntr].PositionLength / _neuronMaxRadius;

                    _neurons[cntr].Value = UtilityCore.GetScaledValue_Capped(0d, 1d, 0d, 1d, 1d - radiusPercent);
                }
            }
        }
        private void UpdateNeurons_fixed(Vector3D gravity, double magnitude)
        {
            const double MINDOT = 1d;

            if (Math3D.IsNearZero(gravity))
            {
                // There is no gravity to report
                for (int cntr = 0; cntr < _neurons.Length; cntr++)
                {
                    _neurons[cntr].Value = 0d;
                }
                return;
            }

            Vector3D gravityUnit = gravity.ToUnit();

            for (int cntr = 0; cntr < _neurons.Length; cntr++)
            {
                if (_neurons[cntr].PositionUnit == null)
                {
                    // This neuron is sitting at 0,0,0
                    _neurons[cntr].Value = magnitude;
                }
                else
                {
                    double dot = Vector3D.DotProduct(gravityUnit, _neurons[cntr].PositionUnit.Value);
                    dot += 1d;		// get this to scale from 0 to 2 instead of -1 to 1

                    if (dot < MINDOT)
                    {
                        _neurons[cntr].Value = 0d;
                    }
                    else
                    {
                        _neurons[cntr].Value = UtilityCore.GetScaledValue_Capped(0d, 1d, MINDOT, 2d, dot);
                    }
                }
            }
        }

        #endregion
    }

    #endregion
}
