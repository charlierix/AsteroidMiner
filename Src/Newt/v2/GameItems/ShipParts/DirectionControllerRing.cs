using Game.HelperClassesCore;
using Game.HelperClassesCore.Threads;
using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Controls3D;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.NewtonDynamics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks.Schedulers;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Game.Newt.v2.GameItems.ShipParts
{
    #region class: DirectionControllerRingToolItem

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
    #region class: DirectionControllerRingDesign

    public class DirectionControllerRingDesign : PartDesignBase
    {
        #region Declaration Section

        internal const double SIZEPERCENTOFSCALE = .6d;
        internal const double BRAINSIZE = .5d;

        public const PartDesignAllowedScale ALLOWEDSCALE = PartDesignAllowedScale.XYZ;		// This is here so the scale can be known through reflection

        private MassBreakdownCache _massBreakdown = null;

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
            if (_massBreakdown != null && _massBreakdown.Scale == Scale && _massBreakdown.CellSize == cellSize)
            {
                // This has already been built for this size
                return _massBreakdown.Breakdown;
            }

            // Convert this.Scale into a size that the mass breakdown will use
            Vector3D size = new Vector3D(this.Scale.X * SIZEPERCENTOFSCALE, this.Scale.Y * SIZEPERCENTOFSCALE, this.Scale.Z * SIZEPERCENTOFSCALE * BRAINSIZE);

            var breakdown = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Sphere, UtilityNewt.MassDistribution.Uniform, size, cellSize);

            // Store this
            _massBreakdown = new MassBreakdownCache(breakdown, Scale, cellSize);

            return _massBreakdown.Breakdown;
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
                GeometryModel3D geometry = new GeometryModel3D
                {
                    Material = material,
                    BackMaterial = material
                };

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
    #region class: DirectionControllerRing

    public class DirectionControllerRing : PartBase, INeuronContainer, IPartUpdatable
    {
        #region Declaration Section

        public const string PARTTYPE = nameof(DirectionControllerRing);

        private readonly ItemOptions _itemOptions;

        private readonly IContainer _energyTanks;

        private readonly NeuralUtility.NeuronShell _neuronsLinear;
        private readonly NeuralUtility.NeuronShell _neuronsRotation;
        private readonly Neuron_Direct[] _neurons;

        private readonly Thruster[] _thrusters;
        private readonly ImpulseEngine[] _impulseEngines;

        private readonly double _volume;        // this is used to calculate energy draw

        private readonly object _lockThrustWorker = new object();
        private bool _isThrustWorkerInitialized = false;
        private ThrustSolutionSolver _thrustWorker = null;
        private Bot _bot = null;

        //TODO: This should be passed in, or make a static manager: SolverThreadPool
        private readonly RoundRobinManager _thrustWorkerThread = new RoundRobinManager(new StaTaskScheduler(1));

        #endregion

        #region Constructor

        public DirectionControllerRing(EditorOptions options, ItemOptions itemOptions, ShipPartDNA dna, IContainer energyTanks, Thruster[] thrusters, ImpulseEngine[] impulseEngines)
            : base(options, dna, itemOptions.DirectionController_Damage.HitpointMin, itemOptions.DirectionController_Damage.HitpointSlope, itemOptions.DirectionController_Damage.Damage)
        {
            _itemOptions = itemOptions;
            _energyTanks = energyTanks;
            _thrusters = thrusters;
            _impulseEngines = impulseEngines;

            Design = new DirectionControllerRingDesign(options, true);
            Design.SetDNA(dna);

            GetMass(out _mass, out _volume, out double radius, out _scaleActual, dna, itemOptions, true);
            Radius = radius;

            #region neurons

            double area = Math.Pow(radius, itemOptions.DirectionController_Ring_NeuronGrowthExponent);

            int neuronCount = Convert.ToInt32(Math.Ceiling(itemOptions.DirectionController_Ring_NeuronDensity_Half * area));
            if (neuronCount == 0)
            {
                neuronCount = 1;
            }

            _neuronsLinear = NeuralUtility.CreateNeuronShell_Ring(1, neuronCount);
            _neuronsRotation = NeuralUtility.CreateNeuronShell_Line(1);

            _neurons = _neuronsLinear.Neurons.
                Concat(_neuronsRotation.Neurons).
                ToArray();

            #endregion
        }

        #endregion

        #region INeuronContainer Members

        public IEnumerable<INeuron> Neruons_Readonly => Enumerable.Empty<INeuron>();
        public IEnumerable<INeuron> Neruons_ReadWrite => Enumerable.Empty<INeuron>();
        public IEnumerable<INeuron> Neruons_Writeonly => _neurons;

        public IEnumerable<INeuron> Neruons_All => _neurons;

        // This controller has intelligence, but from the perspective of the other INeurals, it's a destination
        public NeuronContainerType NeuronContainerType => NeuronContainerType.Manipulator;

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
            if (_impulseEngines != null && _impulseEngines.Length > 0)
            {
                (Vector3D?, Vector3D?) forceTorquePercent = (_neuronsLinear.GetVector(), _neuronsRotation.GetVector());
                var impulseInstruction = new[] { forceTorquePercent };

                foreach (ImpulseEngine impulse in _impulseEngines)
                {
                    impulse.SetDesiredDirection(impulseInstruction);
                }
            }

            if (_thrusters != null && _thrusters.Length > 0)        //TODO: Will also need to consider tractor beams.  But they are harder, because their force is dependent on the object they're interacting with
            {
                lock (_lockThrustWorker)
                {
                    EnsureThrustMapInitialized();

                    //TODO: Thrusters -- use the controller logic from Game.Newt.v2.AsteroidMiner.AstMin2D.ShipPlayer
                }
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

        public ThrusterSetting[] FindSolution_DEBUG(Vector3D? linear, Vector3D? rotation)
        {
            if (_thrustWorker == null)
            {
                return new ThrusterSetting[0];
            }
            else
            {
                return _thrustWorker.FindSolution(linear, rotation);
            }
        }

        #endregion

        #region Private Methods

        private void EnsureThrustMapInitialized()
        {
            if (_isThrustWorkerInitialized)
            {
                return;
            }
            else if (_thrusters == null)
            {
                // No thrusters, so there's nothing to do
                _isThrustWorkerInitialized = true;
                return;
            }

            _bot = GetParent() as Bot;
            if (_bot == null)
            {
                // This part isn't being run from a bot
                _isThrustWorkerInitialized = true;
                return;
            }

            _thrustWorker = new ThrustSolutionSolver(_bot, _thrusters, _thrustWorkerThread, true);

            _isThrustWorkerInitialized = true;
        }

        #endregion
    }

    #endregion
}
