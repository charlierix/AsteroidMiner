using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.GameItems.ShipParts;
using Game.HelperClassesWPF;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.Arcanorum
{
    #region Class: SensorHomingDesign

    public class SensorHomingDesign : PartDesignBase
    {
        #region Declaration Section

        internal const double SIZEPERCENTOFSCALE_XY = 1d;
        internal const double SIZEPERCENTOFSCALE_Z = .1d;

        public const PartDesignAllowedScale ALLOWEDSCALE = PartDesignAllowedScale.XYZ;		// This is here so the scale can be known through reflection

        private Tuple<UtilityNewt.IObjectMassBreakdown, Vector3D, double> _massBreakdown = null;

        #endregion

        #region Constructor

        public SensorHomingDesign(EditorOptions options)
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

        private GeometryModel3D _geometry = null;
        public override Model3D Model
        {
            get
            {
                if (_geometry == null)
                {
                    _geometry = CreateGeometry(false);
                }

                return _geometry;
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
            return CreateSensorCollisionHull(world, this.Scale, this.Orientation, this.Position);
        }
        public override UtilityNewt.IObjectMassBreakdown GetMassBreakdown(double cellSize)
        {
            return GetSensorMassBreakdown(ref _massBreakdown, this.Scale, cellSize);
        }

        internal static CollisionHull CreateSensorCollisionHull(WorldBase world, Vector3D scale, Quaternion orientation, Point3D position)
        {
            Transform3DGroup transform = new Transform3DGroup();
            //transform.Children.Add(new ScaleTransform3D(scale));		// it ignores scale
            transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(orientation)));
            transform.Children.Add(new TranslateTransform3D(position.ToVector()));

            // Scale X and Y should be identical, but average them to be safe
            double radius = ((SIZEPERCENTOFSCALE_XY * scale.X) + (SIZEPERCENTOFSCALE_XY * scale.Y)) / 2d;

            return CollisionHull.CreateCylinder(world, 0, radius, SIZEPERCENTOFSCALE_Z * scale.Z, transform.Value);
        }
        internal static UtilityNewt.IObjectMassBreakdown GetSensorMassBreakdown(ref Tuple<UtilityNewt.IObjectMassBreakdown, Vector3D, double> existing, Vector3D scale, double cellSize)
        {
            if (existing != null && existing.Item2 == scale && existing.Item3 == cellSize)
            {
                // This has already been built for this size
                return existing.Item1;
            }

            // Convert this.Scale into a size that the mass breakdown will use
            Vector3D size = new Vector3D(scale.X * SIZEPERCENTOFSCALE_XY, scale.Y * SIZEPERCENTOFSCALE_XY, scale.Z * SIZEPERCENTOFSCALE_Z);

            var breakdown = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Cylinder, UtilityNewt.MassDistribution.Uniform, size, cellSize);

            // Store this
            existing = new Tuple<UtilityNewt.IObjectMassBreakdown, Vector3D, double>(breakdown, scale, cellSize);

            // Exit Function
            return existing.Item1;
        }

        #endregion

        #region Private Methods

        private GeometryModel3D CreateGeometry(bool isFinal)
        {
            DiffuseMaterial diffuse = WorldColorsArco.SensorVision_Any_Diffuse.Value;
            SpecularMaterial specular = WorldColorsArco.SensorVision_Any_Specular.Value;
            if (!isFinal)
            {
                diffuse = diffuse.Clone();      // cloning, because the editor will manipulate the brush, and WorldColors is handing out a shared brush
                specular = specular.Clone();
            }

            MaterialGroup material = new MaterialGroup();
            this.MaterialBrushes.Add(new MaterialColorProps(diffuse, WorldColorsArco.SensorVision_Any_Color));
            material.Children.Add(diffuse);
            this.MaterialBrushes.Add(new MaterialColorProps(specular));
            material.Children.Add(specular);

            if (!isFinal)
            {
                EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
                material.Children.Add(selectionEmissive);
                base.SelectionEmissives.Add(selectionEmissive);
            }

            GeometryModel3D retVal = new GeometryModel3D();
            retVal.Material = material;
            retVal.BackMaterial = material;

            int segments = isFinal ? 6 : 35;

            double radius = ((this.Scale.X * SIZEPERCENTOFSCALE_XY) + (this.Scale.Y * SIZEPERCENTOFSCALE_XY)) / 2d;
            double height = this.Scale.Z * SIZEPERCENTOFSCALE_Z;
            RotateTransform3D rotateTransform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90));     // this needs to be along Z instead of X

            retVal.Geometry = UtilityWPF.GetCylinder_AlongX(segments, radius, height, rotateTransform);

            // Transform
            retVal.Transform = GetTransformForGeometry(isFinal);

            // Exit Function
            return retVal;
        }

        #endregion
    }

    #endregion
    #region Class: SensorHoming

    public class SensorHoming : PartBase, INeuronContainer, IPartUpdatable
    {
        #region Declaration Section

        public const string PARTTYPE = "SensorHoming";

        private readonly ItemOptionsArco _itemOptions;
        private readonly Map _map;

        private readonly Neuron_SensorPosition[] _neurons;

        #endregion

        #region Constructor

        public SensorHoming(EditorOptions options, ItemOptionsArco itemOptions, ShipPartDNA dna, Map map, Point3D homePoint, double homeRadius)
            : base(options, dna)
        {
            _itemOptions = itemOptions;
            _map = map;

            _homePoint = homePoint;
            _homeRadius = homeRadius;

            this.Design = new SensorHomingDesign(options);
            this.Design.SetDNA(dna);

            double radius, volume;
            SensorVision.GetMass(out _mass, out volume, out radius, out _scaleActual, dna, itemOptions);

            this.Radius = radius;

            _neurons = CreateNeurons(dna, itemOptions, itemOptions.HomingSensorNeuronDensity, true);

            double scale = homeRadius / _neurons.Max(o => o.PositionLength);
            this.NeuronWorldPositions = _neurons.Select(o => (o.PositionUnit.Value * (o.PositionLength * scale)).ToPoint()).ToArray();

            this.HomeRadius = homeRadius;
        }

        #endregion

        #region INeuronContainer Members

        public IEnumerable<INeuron> Neruons_Readonly
        {
            get
            {
                return _neurons;
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
                return _neurons;
            }
        }

        public NeuronContainerType NeuronContainerType
        {
            get
            {
                return NeuronContainerType.Sensor;
            }
        }

        public double Radius
        {
            get;
            private set;
        }

        private volatile bool _isOn = false;
        public bool IsOn
        {
            get
            {
                return _isOn;
            }
        }

        #endregion
        #region IPartUpdatable Members

        public void Update_MainThread(double elapsedTime)
        {
        }
        public void Update_AnyThread(double elapsedTime)
        {
            //TODO: Draw from energy (maybe only if non null)
            //if (_energyTanks.RemoveQuantity(elapsedTime * _volume * _itemOptions.VisionSensorAmountToDraw * ItemOptions.ENERGYDRAWMULT, true) > 0d)
            //{
            //    // The energy tank didn't have enough
            //    //NOTE: To be clean, I should set the neuron outputs to zero, but anything pulling from them should be checking this
            //    //anyway.  So save the processor (also, because of threading, setting them to zero isn't as atomic as this property)
            //    _isOn = false;
            //    return;
            //}

            _isOn = true;


            UpdateNeurons();
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

        private Point3D _homePoint = new Point3D(0, 0, 0);
        public Point3D HomePoint
        {
            get
            {
                return _homePoint;
            }
            set
            {
                _homePoint = value;
            }
        }

        private double _homeRadius = -1d;
        public double HomeRadius
        {
            get
            {
                return _homeRadius;
            }
            set
            {
                _homeRadius = value;

                //RebuildDistanceProps();
            }
        }

        public Point3D[] NeuronWorldPositions
        {
            get;
            private set;
        }

        #endregion

        #region Private Methods

        private void UpdateNeurons()
        {
            const double MINDOT = 1.75d;     //it's dot+1, so dot goes from 0 to 2

            // Get the direction from the current position to the home point
            Vector3D direction = GetDirectionModelCoords();

            if (Math3D.IsNearZero(direction))
            {
                // They are sitting on the home point.  Zero everything out and exit
                for (int cntr = 0; cntr < _neurons.Length; cntr++)
                {
                    _neurons[cntr].Value = 0d;
                }

                return;
            }

            // Get max neuron value
            double directionLength = direction.Length;
            double magnitude;
            if (directionLength < _homeRadius)
            {
                magnitude = directionLength / _homeRadius;
            }
            else
            {
                magnitude = 1;
            }

            Vector3D directionUnit = direction.ToUnit();

            for (int cntr = 0; cntr < _neurons.Length; cntr++)
            {
                if (_neurons[cntr].PositionUnit == null)
                {
                    // This neuron is sitting at 0,0,0
                    _neurons[cntr].Value = 0d;
                }
                else
                {
                    double dot = Vector3D.DotProduct(directionUnit, _neurons[cntr].PositionUnit.Value);
                    dot += 1d;		// get this to scale from 0 to 2 instead of -1 to 1

                    if (dot < MINDOT)
                    {
                        _neurons[cntr].Value = 0d;
                    }
                    else
                    {
                        _neurons[cntr].Value = UtilityCore.GetScaledValue_Capped(0d, magnitude, MINDOT, 2d, dot);
                    }
                }
            }
        }

        private Vector3D GetDirectionModelCoords()
        {
            var worldCoords = base.GetWorldLocation();

            // Calculate the difference between the world orientation and model orientation
            Quaternion toModelQuat = worldCoords.Item2.ToUnit() * this.Orientation.ToUnit();

            RotateTransform3D toModelTransform = new RotateTransform3D(new QuaternionRotation3D(toModelQuat));

            Vector3D directionWorld = _homePoint - worldCoords.Item1;

            // Rotate into model coords
            return toModelTransform.Transform(directionWorld);
        }

        private static Neuron_SensorPosition[] CreateNeurons(ShipPartDNA dna, ItemOptions itemOptions, double neuronDensity, bool ignoreSetValue)
        {
            #region Calculate Counts

            // Figure out how many to make
            //NOTE: This radius isn't taking SCALE into account.  The other neural parts do this as well, so the neural density properties can be more consistent
            double radius = (dna.Scale.X + dna.Scale.Y) / (2d * 2d);		// XY should always be the same anyway (not looking at Z for this.  Z is just to keep the sensors from getting too close to each other)
            double area = Math.Pow(radius, itemOptions.SensorNeuronGrowthExponent);

            int neuronCount = Convert.ToInt32(Math.Ceiling(neuronDensity * area));
            if (neuronCount == 0)
            {
                neuronCount = 1;
            }

            #endregion

            // Place them evenly in a ring
            // I don't want a neuron in the center, so placing a static point there to force the neurons away from the center
            Vector3D[] positions = Brain.GetNeuronPositions_Ring2D(dna.Neurons, neuronCount, radius);       //why 2D?

            // Exit Function
            return positions.
                Select(o => new Neuron_SensorPosition(o.ToPoint(), true, ignoreSetValue)).
                ToArray();
        }

        #endregion
    }

    #endregion
}
