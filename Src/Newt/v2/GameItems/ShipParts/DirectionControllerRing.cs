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
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.GameItems.ShipParts
{
    #region Class: DirectionControllerRingToolItem

    public class DirectionControllerRingToolItem : PartToolItemBase
    {
        #region Constructor

        public DirectionControllerRingToolItem(EditorOptions options)
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
                return "Direction Controller (ring)";
            }
        }
        public override string Description
        {
            get
            {
                return "Consumes energy, manages thrusters and other propulsion (2D)";
            }
        }
        public override string Category
        {
            get
            {
                return PartToolItemBase.CATEGORY_BRAIN;
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
            return new DirectionControllerRingDesign(this.Options, false);
        }

        #endregion
    }

    #endregion
    #region Class: DirectionControllerRingDesign

    public class DirectionControllerRingDesign : PartDesignBase
    {
        #region Declaration Section

        internal const double SIZEPERCENTOFSCALE = .6d;
        internal const double BRAINSIZE = .5d;

        public const PartDesignAllowedScale ALLOWEDSCALE = PartDesignAllowedScale.XYZ;		// This is here so the scale can be known through reflection

        private Tuple<UtilityNewt.IObjectMassBreakdown, Vector3D, double> _massBreakdown = null;

        #endregion

        #region Constructor

        public DirectionControllerRingDesign(EditorOptions options, bool isFinalModel)
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
            Transform3DGroup transform = new Transform3DGroup();
            //transform.Children.Add(new ScaleTransform3D(scale));		// it ignores scale
            transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(this.Orientation)));
            transform.Children.Add(new TranslateTransform3D(this.Position.ToVector()));

            Vector3D size = new Vector3D(this.Scale.X * SIZEPERCENTOFSCALE * .5d, this.Scale.Y * SIZEPERCENTOFSCALE * .5d, this.Scale.Z * SIZEPERCENTOFSCALE * BRAINSIZE * .5d);

            return CollisionHull.CreateSphere(world, 0, size, transform.Value);
        }
        public override UtilityNewt.IObjectMassBreakdown GetMassBreakdown(double cellSize)
        {
            if (_massBreakdown != null && _massBreakdown.Item2 == this.Scale && _massBreakdown.Item3 == cellSize)
            {
                // This has already been built for this size
                return _massBreakdown.Item1;
            }

            // Convert this.Scale into a size that the mass breakdown will use
            Vector3D size = new Vector3D(this.Scale.X * SIZEPERCENTOFSCALE, this.Scale.Y * SIZEPERCENTOFSCALE, this.Scale.Z * SIZEPERCENTOFSCALE * BRAINSIZE);

            var breakdown = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Sphere, UtilityNewt.MassDistribution.Uniform, size, cellSize);

            // Store this
            _massBreakdown = new Tuple<UtilityNewt.IObjectMassBreakdown, Vector3D, double>(breakdown, this.Scale, cellSize);

            // Exit Function
            return _massBreakdown.Item1;
        }

        public override PartToolItemBase GetToolItem()
        {
            return new DirectionControllerRingToolItem(this.Options);
        }

        internal static Model3D CreateGeometry(List<MaterialColorProps> materialBrushes, List<EmissiveMaterial> selectionEmissives, Transform3D transform, Tuple<Vector3D, double>[][] ringRotations, bool isFinal)
        {
            Model3DGroup retVal = new Model3DGroup();

            #region rings

            MaterialGroup material = new MaterialGroup();

            Color ringColor = WorldColors.DirectionControllerRing_Color;
            DiffuseMaterial diffuse = new DiffuseMaterial(new SolidColorBrush(ringColor));
            materialBrushes.Add(new MaterialColorProps(diffuse, ringColor));
            material.Children.Add(diffuse);

            SpecularMaterial specular = WorldColors.DirectionControllerRing_Specular;
            materialBrushes.Add(new MaterialColorProps(specular));
            material.Children.Add(specular);

            if (!isFinal)
            {
                EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
                material.Children.Add(selectionEmissive);
                selectionEmissives.Add(selectionEmissive);
            }

            foreach (var torusAngle in ringRotations)
            {
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = material;
                geometry.BackMaterial = material;

                Transform3DGroup transformGroup = new Transform3DGroup();
                foreach (var individualAngle in torusAngle)
                {
                    transformGroup.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(individualAngle.Item1, individualAngle.Item2)));
                }

                const double THICKNESS = SIZEPERCENTOFSCALE * .04;       // it's actually half thickness
                const double RINGRADIUS = (SIZEPERCENTOFSCALE * BRAINSIZE) - THICKNESS;

                if (isFinal)
                {
                    geometry.Geometry = UtilityWPF.GetTorus(10, 4, THICKNESS, RINGRADIUS);       // must be 10 sided to match the icosidodecahedron (not same as the ichthyosaur)
                }
                else
                {
                    geometry.Geometry = UtilityWPF.GetTorus(60, 12, THICKNESS, RINGRADIUS);
                }
                geometry.Transform = transformGroup;

                retVal.Children.Add(geometry);
            }

            #endregion

            #region brain

            double brainScale = SIZEPERCENTOFSCALE * BRAINSIZE;
            ScaleTransform3D scaleTransform = new ScaleTransform3D();
            scaleTransform.ScaleX = brainScale;
            scaleTransform.ScaleY = brainScale;
            scaleTransform.ScaleZ = brainScale;

            if (!isFinal)
            {
                retVal.Children.AddRange(BrainDesign.CreateInsideVisuals(brainScale * .9, materialBrushes, selectionEmissives, scaleTransform));
            }

            retVal.Children.Add(BrainDesign.CreateShellVisual(isFinal, materialBrushes, selectionEmissives, scaleTransform));

            #endregion

            retVal.Transform = transform;

            return retVal;
        }

        #endregion

        #region Private Methods

        private Model3D CreateGeometry(bool isFinal)
        {
            Tuple<Vector3D, double>[][] ringRotations = new[] { new[] { Tuple.Create(new Vector3D(1, 0, 0), 0d) } };        // no rotation needed.  This is just here to make a ring

            return CreateGeometry(
                this.MaterialBrushes, base.SelectionEmissives,
                GetTransformForGeometry(isFinal),
                ringRotations, isFinal);
        }

        #endregion
    }

    #endregion
    #region Class: DirectionControllerRing

    public class DirectionControllerRing : PartBase, INeuronContainer, IPartUpdatable
    {
        #region Class: NeuronShell

        /// <summary>
        /// This could hold a spherical shell, circular ring, or endpoints of a line
        /// </summary>
        internal class NeuronShell
        {
            public NeuronShell(Neuron_SensorPosition[] neurons, double radius)
            {
                this.Neurons = neurons;
                this.Radius = radius;
                this.VectorsUnit = neurons.
                    Select(o => o.Position.ToVector() / radius).
                    ToArray();
            }

            public readonly Neuron_SensorPosition[] Neurons;
            public readonly double Radius;

            public readonly Vector3D[] VectorsUnit;

            public Vector3D GetVector()
            {
                Vector3D retVal = new Vector3D(0, 0, 0);

                for (int cntr = 0; cntr < this.Neurons.Length; cntr++)
                {
                    retVal += this.VectorsUnit[cntr] * this.Neurons[cntr].Value;
                }

                return retVal;
            }
        }

        #endregion

        #region Declaration Section

        public const string PARTTYPE = "DirectionControllerRing";

        private readonly ItemOptions _itemOptions;

        private readonly IContainer _energyTanks;

        private readonly DirectionControllerRing.NeuronShell _neuronsLinear;
        private readonly DirectionControllerRing.NeuronShell _neuronsRotation;
        private readonly Neuron_SensorPosition[] _neurons;

        private readonly Thruster[] _thrusters;
        private readonly ImpulseEngine[] _impulseEngines;

        private readonly double _volume;        // this is used to calculate energy draw

        #endregion

        #region Constructor

        public DirectionControllerRing(EditorOptions options, ItemOptions itemOptions, ShipPartDNA dna, IContainer energyTanks, Thruster[] thrusters, ImpulseEngine[] impulseEngines)
            : base(options, dna, itemOptions.DirectionController_Damage.HitpointMin, itemOptions.DirectionController_Damage.HitpointSlope, itemOptions.DirectionController_Damage.Damage)
        {
            _itemOptions = itemOptions;
            _energyTanks = energyTanks;
            _thrusters = thrusters;
            _impulseEngines = impulseEngines;

            this.Design = new DirectionControllerRingDesign(options, true);
            this.Design.SetDNA(dna);

            double radius;
            GetMass(out _mass, out _volume, out radius, out _scaleActual, dna, itemOptions, true);
            this.Radius = radius;

            #region neurons

            double area = Math.Pow(radius, itemOptions.DirectionController_Ring_NeuronGrowthExponent);

            int neuronCount = Convert.ToInt32(Math.Ceiling(itemOptions.DirectionController_Ring_NeuronDensity_Half * area));
            if (neuronCount == 0)
            {
                neuronCount = 1;
            }

            _neuronsLinear = DirectionControllerRing.CreateNeuronShell_Ring(1, neuronCount);
            _neuronsRotation = DirectionControllerRing.CreateNeuronShell_Line(1);

            _neurons = _neuronsLinear.Neurons.
                Concat(_neuronsRotation.Neurons).
                ToArray();

            #endregion
        }

        #endregion

        #region INeuronContainer Members

        public IEnumerable<INeuron> Neruons_Readonly
        {
            get
            {
                return Enumerable.Empty<INeuron>();
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
                return _neurons;
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
                return NeuronContainerType.Manipulator;     // this controller has intelligence, but from the perspective of the other INeurals, it's a destination
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
            var forceTorquePercent = new Tuple<Vector3D?, Vector3D?>(_neuronsLinear.GetVector(), _neuronsRotation.GetVector());

            if (_impulseEngines != null && _impulseEngines.Length > 0)
            {
                var impulseInstruction = new[] { forceTorquePercent };
                foreach (ImpulseEngine impulse in _impulseEngines)
                {
                    impulse.SetDesiredDirection(impulseInstruction);
                }
            }

            //TODO: Thrusters -- use the controller logic from Game.Newt.v2.AsteroidMiner.AstMin2D.ShipPlayer
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

        #endregion

        #region Public Methods

        internal static void GetMass(out double mass, out double volume, out double radius, out Vector3D actualScale, ShipPartDNA dna, ItemOptions itemOptions, bool isRing)
        {
            radius = (dna.Scale.X + dna.Scale.Y + dna.Scale.Z) / (3d * 2d);		// they should be identical anyway
            radius *= DirectionControllerRingDesign.SIZEPERCENTOFSCALE;       // scale it

            if (isRing)
            {
                actualScale = new Vector3D(radius * 2d, radius * 2d, radius * 2d * DirectionControllerRingDesign.BRAINSIZE);
            }
            else
            {
                actualScale = new Vector3D(radius * 2d, radius * 2d, radius * 2d);
            }

            volume = 4d / 3d * Math.PI * radius * radius * radius;
            mass = volume * itemOptions.DirectionController_Density;
        }

        internal static NeuronShell CreateNeuronShell_Sphere(double radius, int count)
        {
            Vector3D[] positions = Math3D.GetRandomVectors_SphericalShell_EvenDist(count, radius);

            Neuron_SensorPosition[] neurons = positions.
                Select(o => new Neuron_SensorPosition(o.ToPoint(), true, false)).
                ToArray();

            return new NeuronShell(neurons, radius);
        }
        internal static NeuronShell CreateNeuronShell_Ring(double radius, int count, ITriangle plane = null)
        {
            Vector[] positions2D = Math3D.GetRandomVectors_CircularRing_EvenDist(count, radius);

            IEnumerable<Vector3D> positions3D;
            if (plane == null)
            {
                positions3D = positions2D.
                    Select(o => o.ToVector3D());
            }
            else
            {
                RotateTransform3D transform = new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRotation(new Vector3D(0, 0, 1), plane.Normal)));

                positions3D = positions2D.
                    Select(o => transform.Transform(o.ToVector3D()));
            }

            Neuron_SensorPosition[] neurons = positions3D.
                Select(o => new Neuron_SensorPosition(o.ToPoint(), true, false)).
                ToArray();

            return new NeuronShell(neurons, radius);
        }
        internal static NeuronShell CreateNeuronShell_Line(double radius, Vector3D? line = null)
        {
            Vector3D lineUnit;

            if (line != null)
            {
                lineUnit = line.Value.ToUnit();
                if (Math3D.IsInvalid(lineUnit))
                {
                    lineUnit = new Vector3D(0, 0, 1);
                }
            }
            else
            {
                lineUnit = new Vector3D(0, 0, 1);
            }

            Neuron_SensorPosition[] neurons = new[]
            {
                new Neuron_SensorPosition((lineUnit * radius).ToPoint(), true, false),
                new Neuron_SensorPosition((-lineUnit * radius).ToPoint(), true, false),
            };

            return new NeuronShell(neurons, radius);
        }

        #endregion
    }

    #endregion
}
