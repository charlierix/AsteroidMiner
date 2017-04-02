using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;
using Game.HelperClassesWPF;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.GameItems.ShipParts
{
    #region Class: DirectionControllerSphereToolItem

    public class DirectionControllerSphereToolItem : PartToolItemBase
    {
        #region Constructor

        public DirectionControllerSphereToolItem(EditorOptions options)
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
                return "Direction Controller (sphere)";
            }
        }
        public override string Description
        {
            get
            {
                return "Consumes energy, manages thrusters and other propulsion (3D)";
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
            return new DirectionControllerSphereDesign(this.Options, false);
        }

        #endregion
    }

    #endregion
    #region Class: DirectionControllerSphereDesign

    public class DirectionControllerSphereDesign : PartDesignBase
    {
        #region Declaration Section

        public const PartDesignAllowedScale ALLOWEDSCALE = PartDesignAllowedScale.XYZ;		// This is here so the scale can be known through reflection

        private Tuple<UtilityNewt.IObjectMassBreakdown, Vector3D, double> _massBreakdown = null;

        #endregion

        #region Constructor

        public DirectionControllerSphereDesign(EditorOptions options, bool isFinalModel)
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

            Vector3D size = new Vector3D(this.Scale.X * DirectionControllerRingDesign.SIZEPERCENTOFSCALE * .5d, this.Scale.Y * DirectionControllerRingDesign.SIZEPERCENTOFSCALE * .5d, this.Scale.Z * DirectionControllerRingDesign.SIZEPERCENTOFSCALE * .5d);

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
            Vector3D size = new Vector3D(this.Scale.X * DirectionControllerRingDesign.SIZEPERCENTOFSCALE, this.Scale.Y * DirectionControllerRingDesign.SIZEPERCENTOFSCALE, this.Scale.Z * DirectionControllerRingDesign.SIZEPERCENTOFSCALE);

            var breakdown = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Sphere, UtilityNewt.MassDistribution.Uniform, size, cellSize);

            // Store this
            _massBreakdown = new Tuple<UtilityNewt.IObjectMassBreakdown, Vector3D, double>(breakdown, this.Scale, cellSize);

            // Exit Function
            return _massBreakdown.Item1;
        }

        public override PartToolItemBase GetToolItem()
        {
            return new DirectionControllerSphereToolItem(this.Options);
        }

        #endregion

        #region Private Methods

        private Model3D CreateGeometry(bool isFinal)
        {
            const double ANGLE1 = 58;       // got to angle1 and 2 by experimentation
            const double ANGLE2 = 32;
            const double HALFROTATE = 18;       // the first rotation around Z is 18 because there are 10 points (36 degrees / 2) -- it needs half a rotation to line up

            Tuple<Vector3D, double>[][] ringRotations = new[]
            {
                new[] { Tuple.Create(new Vector3D(1, 0, 0), ANGLE1) },
                new[] { Tuple.Create(new Vector3D(1, 0, 0), -ANGLE1) },

                new[] { Tuple.Create(new Vector3D(0, 0, 1), HALFROTATE), Tuple.Create(new Vector3D(0, 1, 0), ANGLE2) },
                new[] { Tuple.Create(new Vector3D(0, 0, 1), HALFROTATE), Tuple.Create(new Vector3D(0, 1, 0), -ANGLE2) },

                new[] { Tuple.Create(new Vector3D(0, 0, 1), HALFROTATE), Tuple.Create(new Vector3D(1, 0, 0), 90d), Tuple.Create(new Vector3D(0, 0, 1), ANGLE1) },
                new[] { Tuple.Create(new Vector3D(0, 0, 1), HALFROTATE), Tuple.Create(new Vector3D(1, 0, 0), 90d), Tuple.Create(new Vector3D(0, 0, 1), -ANGLE1) },
            };

            return DirectionControllerRingDesign.CreateGeometry(
                this.MaterialBrushes, base.SelectionEmissives,
                GetTransformForGeometry(isFinal),
                ringRotations, isFinal);
        }

        #endregion
    }

    #endregion
    #region Class: DirectionControllerSphere

    public class DirectionControllerSphere : PartBase, INeuronContainer, IPartUpdatable
    {
        #region Declaration Section

        public const string PARTTYPE = "DirectionControllerSphere";

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

        public DirectionControllerSphere(EditorOptions options, ItemOptions itemOptions, ShipPartDNA dna, IContainer energyTanks, Thruster[] thrusters, ImpulseEngine[] impulseEngines)
            : base(options, dna, itemOptions.DirectionController_Damage.HitpointMin, itemOptions.DirectionController_Damage.HitpointSlope, itemOptions.DirectionController_Damage.Damage)
        {
            _itemOptions = itemOptions;
            _energyTanks = energyTanks;
            _thrusters = thrusters;
            _impulseEngines = impulseEngines;

            this.Design = new DirectionControllerSphereDesign(options, true);
            this.Design.SetDNA(dna);

            double radius;
            DirectionControllerRing.GetMass(out _mass, out _volume, out radius, out _scaleActual, dna, itemOptions, false);
            this.Radius = radius;

            #region neurons

            double area = Math.Pow(radius, itemOptions.DirectionController_Sphere_NeuronGrowthExponent);

            int neuronCount = Convert.ToInt32(Math.Ceiling(itemOptions.DirectionController_Sphere_NeuronDensity_Half * area));
            if (neuronCount == 0)
            {
                neuronCount = 1;
            }

            _neuronsLinear = DirectionControllerRing.CreateNeuronShell_Sphere(1, neuronCount);
            _neuronsRotation = DirectionControllerRing.CreateNeuronShell_Sphere(.4, neuronCount);

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
    }

    #endregion
}
