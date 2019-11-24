using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.NewtonDynamics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media.Media3D;

namespace Game.Newt.v2.GameItems.ShipParts
{
    #region class: SensorHomingToolItem

    public class SensorHomingToolItem : PartToolItemBase
    {
        #region Constructor

        public SensorHomingToolItem(EditorOptions options)
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
                return "Homing Sensor";
            }
        }
        public override string Description
        {
            get
            {
                return "Reports direction/distance from a specific point";
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
            return new SensorHomingDesign(this.Options, false);
        }

        #endregion
    }

    #endregion
    #region class: SensorHomingDesign

    public class SensorHomingDesign : PartDesignBase
    {
        #region Declaration Section

        public const PartDesignAllowedScale ALLOWEDSCALE = PartDesignAllowedScale.XYZ;		// This is here so the scale can be known through reflection

        private MassBreakdownCache _massBreakdown = null;

        #endregion

        #region Constructor

        public SensorHomingDesign(EditorOptions options, bool isFinalModel)
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

        private Model3DGroup _geometry = null;
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
            return SensorGravityDesign.CreateSensorCollisionHull(world, this.Scale, this.Orientation, this.Position);
        }
        public override UtilityNewt.IObjectMassBreakdown GetMassBreakdown(double cellSize)
        {
            return SensorGravityDesign.GetSensorMassBreakdown(ref _massBreakdown, this.Scale, cellSize);
        }

        public override PartToolItemBase GetToolItem()
        {
            return new SensorHomingToolItem(this.Options);
        }

        #endregion

        #region Private Methods

        private Model3DGroup CreateGeometry(bool isFinal)
        {
            return SensorGravityDesign.CreateGeometry(this.MaterialBrushes, base.SelectionEmissives,
                GetTransformForGeometry(isFinal),
                WorldColors.SensorBase_Color, WorldColors.SensorBase_Specular, WorldColors.SensorHoming_Color, WorldColors.SensorHoming_Specular,
                isFinal);
        }

        #endregion
    }

    #endregion
    #region class: SensorHoming

    public class SensorHoming : PartBase, INeuronContainer, IPartUpdatable
    {
        #region Declaration Section

        public const string PARTTYPE = nameof(SensorHoming);

        private readonly ItemOptions _itemOptions;
        private readonly Map _map;
        private readonly IContainer _energyTanks;

        private readonly Neuron_SensorPosition[] _neurons;

        private readonly double _volume;		// this is used to calculate energy draw

        #endregion

        #region Constructor

        public SensorHoming(EditorOptions options, ItemOptions itemOptions, ShipPartDNA dna, Map map, IContainer energyTanks, Point3D? homePoint = null, double? homeRadius = null)
            : base(options, dna, itemOptions.Sensor_Damage.HitpointMin, itemOptions.Sensor_Damage.HitpointSlope, itemOptions.Sensor_Damage.Damage)
        {
            _itemOptions = itemOptions;
            _map = map;
            _energyTanks = energyTanks;

            Design = new SensorHomingDesign(options, true);
            Design.SetDNA(dna);

            SensorGravity.GetMass(out _mass, out _volume, out double radius, out _scaleActual, dna, itemOptions);

            Radius = radius;

            _neurons = CreateNeurons(dna, itemOptions, itemOptions.HomingSensor_NeuronDensity, true);

            _homePoint = homePoint ?? new Point3D(0, 0, 0);
            HomeRadius = homeRadius ?? _itemOptions.HomingSensor_HomeRadiusPercentOfRadius * Radius;
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
            if (IsDestroyed || _energyTanks == null || _energyTanks.RemoveQuantity(elapsedTime * _volume * _itemOptions.HomingSensor_AmountToDraw * ItemOptions.ENERGYDRAWMULT, true) > 0d)
            {
                // The energy tank didn't have enough
                //NOTE: To be clean, I should set the neuron outputs to zero, but anything pulling from them should be checking this
                //anyway.  So save the processor (also, because of threading, setting them to zero isn't as atomic as this property)
                _isOn = false;
                return;
            }

            _isOn = true;

            UpdateNeurons();
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

                RebuildDistanceProps();
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
                foreach(var neuron in _neurons)
                {
                    neuron.Value = 0d;
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

            foreach (var neuron in _neurons)
            {
                if (neuron.PositionUnit == null)
                {
                    // This neuron is sitting at 0,0,0
                    neuron.Value = 0d;
                }
                else
                {
                    double dot = Vector3D.DotProduct(directionUnit, neuron.PositionUnit.Value);
                    dot += 1d;		// get this to scale from 0 to 2 instead of -1 to 1

                    if (dot < MINDOT)
                    {
                        neuron.Value = 0d;
                    }
                    else
                    {
                        neuron.Value = UtilityCore.GetScaledValue_Capped(0d, magnitude, MINDOT, 2d, dot);
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

        private void RebuildDistanceProps()
        {
            double scale = _homeRadius / _neurons.Max(o => o.PositionLength);

            NeuronWorldPositions = _neurons.
                Select(o => (o.PositionUnit.Value * (o.PositionLength * scale)).ToPoint()).
                ToArray();
        }

        private static Neuron_SensorPosition[] CreateNeurons(ShipPartDNA dna, ItemOptions itemOptions, double neuronDensity, bool ignoreSetValue)
        {
            #region calculate counts

            // Figure out how many to make
            //NOTE: This radius isn't taking SCALE into account.  The other neural parts do this as well, so the neural density properties can be more consistent
            double radius = Math1D.Avg(dna.Scale.X, dna.Scale.Y, dna.Scale.Z) / 2d;     // dividing by 2, because radius is wanted, not diameter
            double area = Math.Pow(radius, itemOptions.Sensor_NeuronGrowthExponent);

            int neuronCount = (neuronDensity * area).ToInt_Ceiling();
            neuronCount = Math.Max(neuronCount, 10);

            #endregion

            // Place them evenly on the surface of a sphere
            Vector3D[] positions = NeuralUtility.GetNeuronPositions_SphericalShell_Even(dna.Neurons, neuronCount, radius);

            return positions.
                Select(o => new Neuron_SensorPosition(o.ToPoint(), true, ignoreSetValue)).
                ToArray();
        }

        #endregion
    }

    #endregion
}
