using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.GameItems.ShipParts;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.Arcanorum.Parts
{
    // Have a ring of neurons for linear motion, and two inside for rotation (may want the two inside to be on a line going through the origin (3D instead of 2D)

    #region class: MotionController2Design

    public class MotionController2Design : PartDesignBase
    {
        #region Declaration Section

        internal const double SIZEPERCENTOFSCALE_XY = 1d;
        internal const double SIZEPERCENTOFSCALE_Z = .1d;

        public const PartDesignAllowedScale ALLOWEDSCALE = PartDesignAllowedScale.XYZ;		// This is here so the scale can be known through reflection

        private MassBreakdownCache _massBreakdown = null;

        #endregion

        #region Constructor

        public MotionController2Design(EditorOptions options, bool isFinalModel)
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

        private Model3D _geometry = null;
        public override Model3D Model
        {
            get
            {
                if (_geometry == null)
                {
                    _geometry = CreateGeometry(this.IsFinalModel);
                }

                return _geometry;
            }
        }

        #endregion

        #region Public Methods

        public override CollisionHull CreateCollisionHull(WorldBase world)
        {
            return CreateSensorCollisionHull(world, Scale, Orientation, Position);
        }
        public override UtilityNewt.IObjectMassBreakdown GetMassBreakdown(double cellSize)
        {
            return GetSensorMassBreakdown(ref _massBreakdown, Scale, cellSize);
        }

        internal static CollisionHull CreateSensorCollisionHull(WorldBase world, Vector3D scale, Quaternion orientation, Point3D position)
        {
            Transform3DGroup transform = new Transform3DGroup();
            //transform.Children.Add(new ScaleTransform3D(scale));		// it ignores scale
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90)));		// the physics hull is along x, but dna is along z
            transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(orientation)));
            transform.Children.Add(new TranslateTransform3D(position.ToVector()));

            // Scale X and Y should be identical, but average them to be safe
            double radius = SIZEPERCENTOFSCALE_XY * Math1D.Avg(scale.X, scale.Y) * .5;      // multiplying by .5 to turn diameter into radius
            double height = SIZEPERCENTOFSCALE_Z * scale.Z;

            return CollisionHull.CreateCylinder(world, 0, radius, height, transform.Value);
        }
        internal static UtilityNewt.IObjectMassBreakdown GetSensorMassBreakdown(ref MassBreakdownCache existing, Vector3D scale, double cellSize)
        {
            if (existing != null && existing.Scale == scale && existing.CellSize == cellSize)
            {
                // This has already been built for this size
                return existing.Breakdown;
            }

            // Convert this.Scale into a size that the mass breakdown will use (mass breakdown wants height along X, and scale is for radius, but the mass breakdown wants diameter)
            Vector3D size = new Vector3D(scale.Z * SIZEPERCENTOFSCALE_Z, scale.X * SIZEPERCENTOFSCALE_XY * 2, scale.Y * SIZEPERCENTOFSCALE_XY * 2);

            var cylinder = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Cylinder, UtilityNewt.MassDistribution.Uniform, size, cellSize);

            Transform3D transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90));		// the physics hull is along x, but dna is along z

            // Rotated
            UtilityNewt.ObjectMassBreakdownSet combined = new UtilityNewt.ObjectMassBreakdownSet(
                new UtilityNewt.ObjectMassBreakdown[] { cylinder },
                new Transform3D[] { transform });

            // Store this
            existing = new MassBreakdownCache(combined, scale, cellSize);

            return existing.Breakdown;
        }

        public override PartToolItemBase GetToolItem()
        {
            throw new NotImplementedException(nameof(MotionController2) + " doesn't have a tool item class");
        }

        #endregion

        #region Private Methods

        private Model3D CreateGeometry(bool isFinal)
        {
            #region material

            DiffuseMaterial diffuse = WorldColorsArco.MotionController_Linear_Diffuse.Value;
            SpecularMaterial specular = WorldColorsArco.MotionController_Linear_Specular.Value;
            if (!isFinal)
            {
                diffuse = diffuse.Clone();      // cloning, because the editor will manipulate the brush, and WorldColors is handing out a shared brush
                specular = specular.Clone();
            }

            MaterialGroup material = new MaterialGroup();
            this.MaterialBrushes.Add(new MaterialColorProps(diffuse, WorldColorsArco.MotionController_Linear_Color));
            material.Children.Add(diffuse);
            this.MaterialBrushes.Add(new MaterialColorProps(specular));
            material.Children.Add(specular);

            if (!isFinal)
            {
                EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
                material.Children.Add(selectionEmissive);
                base.SelectionEmissives.Add(selectionEmissive);
            }

            #endregion

            double radius = ((this.Scale.X * SIZEPERCENTOFSCALE_XY) + (this.Scale.Y * SIZEPERCENTOFSCALE_XY)) / 2d;
            double height = this.Scale.Z * SIZEPERCENTOFSCALE_Z;
            double halfHeight = height / 2d;

            Model3DGroup retVal = new Model3DGroup();

            #region center

            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = material;
            geometry.BackMaterial = material;

            List<TubeRingBase> rings = new List<TubeRingBase>();

            rings.Add(new TubeRingPoint(0, false));
            rings.Add(new TubeRingRegularPolygon(halfHeight, false, radius * .3, radius * .3, false));
            rings.Add(new TubeRingPoint(halfHeight, false));

            geometry.Geometry = UtilityWPF.GetMultiRingedTube(5, rings, false, true);

            retVal.Children.Add(geometry);

            #endregion

            #region ring

            geometry = new GeometryModel3D();
            geometry.Material = material;
            geometry.BackMaterial = material;

            int segments = isFinal ? 10 : 35;

            geometry.Geometry = UtilityWPF.GetRing(segments, radius - (height / 2d), radius, height);

            retVal.Children.Add(geometry);

            #endregion

            // Transform
            retVal.Transform = GetTransformForGeometry(isFinal);

            return retVal;
        }

        #endregion
    }

    #endregion
    #region class: MotionController2

    public class MotionController2 : PartBase, INeuronContainer, IPartUpdatable
    {
        #region Declaration Section

        public const string PARTTYPE = nameof(MotionController2);

        private readonly ItemOptionsArco _itemOptions;

        private readonly AIMousePlate _mousePlate;

        /// <summary>
        /// The sign represents clockwise or counterclockwise.  The magnitude represents speed
        /// </summary>
        private readonly Neuron_SensorPosition _neuron_rotate_direction_speed;      // this one is -+
        /// <summary>
        /// This represents the radius of the circle
        /// </summary>
        /// <remarks>
        /// The real world radius should be based on the bot's radius.  So something like 0 to 10xRadius
        /// </remarks>
        private readonly Neuron_SensorPosition _neuron_rotate_radius;
        /// <summary>
        /// These are a ring of neurons.  The result vector is simply the sum of each neuron
        /// </summary>
        /// <remarks>
        private readonly Neuron_SensorPosition[] _neurons_linear;

        /// <summary>
        /// This is all the neurons in a single array.  Just useful for consumers of this class and saving to dna
        /// </summary>
        private readonly Neuron_SensorPosition[] _neurons_all;

        #endregion

        #region Constructor

        public MotionController2(EditorOptions options, ItemOptionsArco itemOptions, ShipPartDNA dna, AIMousePlate mousePlate)
            : base(options, dna, itemOptions.MotionController_Damage.HitpointMin, itemOptions.MotionController_Damage.HitpointSlope, itemOptions.MotionController_Damage.Damage)
        {
            _itemOptions = itemOptions;
            _mousePlate = mousePlate;

            this.Design = new MotionController2Design(options, true);
            this.Design.SetDNA(dna);

            GetMass(out _mass, out double volume, out double radius, out _scaleActual, dna, itemOptions);

            this.Radius = radius;

            var neurons = CreateNeurons(dna, itemOptions);
            _neuron_rotate_direction_speed = neurons.rot_dirspeed;
            _neuron_rotate_radius = neurons.rot_radius;
            _neurons_linear = neurons.linear;
            _neurons_all = UtilityCore.Iterate<Neuron_SensorPosition>(_neuron_rotate_direction_speed, _neuron_rotate_radius, _neurons_linear).ToArray();
        }

        #endregion

        #region INeuronContainer Members

        public IEnumerable<INeuron> Neruons_Readonly => Enumerable.Empty<INeuron>();
        public IEnumerable<INeuron> Neruons_ReadWrite => Enumerable.Empty<INeuron>();
        public IEnumerable<INeuron> Neruons_Writeonly => _neurons_all;

        public IEnumerable<INeuron> Neruons_All => _neurons_all;

        public NeuronContainerType NeuronContainerType => NeuronContainerType.Manipulator;

        public double Radius
        {
            get;
            private set;
        }

        //private volatile bool _isOn = false;
        public bool IsOn => true;

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

            //_isOn = true;

            Point linear = Math3D.GetCenter(_neurons_linear.Select(o => Tuple.Create(o.Position, o.Value)).ToArray()).ToPoint2D();


            //TODO: Look at the desired rotation
            //
            // Need to store the point that is being rotated around
            //
            // Basically a unit vector that holds the current location within a circle, then use radius to calculate the center of rotation for this tick



            _mousePlate.CurrentPoint2D = linear;
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

        internal static void GetMass(out double mass, out double volume, out double radius, out Vector3D actualScale, ShipPartDNA dna, ItemOptions itemOptions)
        {
            double radiusLocal = ((dna.Scale.X * MotionController2Design.SIZEPERCENTOFSCALE_XY) + (dna.Scale.Y * MotionController2Design.SIZEPERCENTOFSCALE_XY)) / (2d * 2d);     // scale is diameter, so divide an extra two to get radius
            double heightLocal = dna.Scale.Z * MotionController2Design.SIZEPERCENTOFSCALE_Z;
            double halfHeightLocal = heightLocal / 2d;

            volume = Math.PI * radiusLocal * radiusLocal * heightLocal;		// get volume of the cylinder

            // This isn't the radius of the cylinder, it is the radius of the bounding sphere
            radius = Math.Sqrt((radiusLocal * radiusLocal) + (halfHeightLocal * halfHeightLocal));

            mass = volume * itemOptions.Sensor_Density;

            actualScale = new Vector3D(dna.Scale.X * MotionController2Design.SIZEPERCENTOFSCALE_XY, dna.Scale.Y * MotionController2Design.SIZEPERCENTOFSCALE_XY, dna.Scale.Z * MotionController2Design.SIZEPERCENTOFSCALE_Z);
        }

        #endregion

        #region Private Methods

        private static (Neuron_SensorPosition rot_dirspeed, Neuron_SensorPosition rot_radius, Neuron_SensorPosition[] linear) CreateNeurons(ShipPartDNA dna, ItemOptionsArco itemOptions)
        {
            #region ring - linear

            // Figure out how many to make
            //NOTE: This radius isn't taking SCALE into account.  The other neural parts do this as well, so the neural density properties can be more consistent
            double radius = (dna.Scale.X + dna.Scale.Y) / (2d * 2d);		// XY should always be the same anyway (not looking at Z for this.  Z is just to keep the sensors from getting too close to each other)
            double area = Math.Pow(radius, itemOptions.MotionController2_NeuronGrowthExponent);

            int neuronCount = (area * itemOptions.MotionController2_NeuronDensity).ToInt_Ceiling();
            neuronCount += 2;       // manually add two for the rotation neruons

            if (neuronCount < 7)
            {
                neuronCount = 7;
            }

            var neuronPositions = SplitNeuronPositions(dna.Neurons);

            // Place them evenly around the perimiter of a circle.
            Vector3D[] linearPositions = NeuralUtility.GetNeuronPositions_CircularShell_Even(neuronPositions?.linear, neuronCount, radius);

            Neuron_SensorPosition[] linearNeurons = linearPositions.
                Select(o => new Neuron_SensorPosition(o.ToPoint(), true, false)).
                ToArray();

            #endregion

            #region interior - rotation

            Neuron_SensorPosition rotateDirSpeed = new Neuron_SensorPosition(new Point3D(-.25, 0, 0), false, false);
            Neuron_SensorPosition rotateRadius = new Neuron_SensorPosition(new Point3D(.25, 0, 0), true, false);

            #endregion

            return
                (
                    rotateDirSpeed,
                    rotateRadius,
                    linearNeurons
                );
        }

        private static (Point3D rotationLeft, Point3D rotationRight, Point3D[] linear)? SplitNeuronPositions(Point3D[] allPositions)
        {
            if (allPositions == null || allPositions.Length < 3)        // the first two are hardcoded positions, so if it's less than that, just start over (it never should be that small)
            {
                return null;
            }

            return (allPositions[0], allPositions[1], allPositions.Skip(2).ToArray());
        }
        private static Point3D[] CombineNeuronPositions(Point3D rotationLeft, Point3D rotationRight, Point3D[] linear)
        {
            return UtilityCore.Iterate<Point3D>(rotationLeft, rotationRight, linear).
                ToArray();
        }

        #endregion
    }

    #endregion
}
