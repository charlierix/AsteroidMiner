using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using Game.Newt.v2.GameItems.ShipEditor;
using Game.HelperClassesWPF;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.GameItems.ShipParts
{
    #region Class: SensorSpinToolItem

    public class SensorSpinToolItem : PartToolItemBase
    {
        #region Constructor

        public SensorSpinToolItem(EditorOptions options)
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
                return "Spin Sensor";
            }
        }
        public override string Description
        {
            get
            {
                return "Reports the current angular velociy";
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
            return new SensorSpinDesign(this.Options, false);
        }

        #endregion
    }

    #endregion
    #region Class: SensorSpinDesign

    public class SensorSpinDesign : PartDesignBase
    {
        #region Declaration Section

        public const PartDesignAllowedScale ALLOWEDSCALE = PartDesignAllowedScale.XYZ;		// This is here so the scale can be known through reflection

        private Tuple<UtilityNewt.IObjectMassBreakdown, Vector3D, double> _massBreakdown = null;

        #endregion

        #region Constructor

        public SensorSpinDesign(EditorOptions options, bool isFinalModel)
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
            return SensorGravityDesign.CreateSensorCollisionHull(world, this.Scale, this.Orientation, this.Position);
        }
        public override UtilityNewt.IObjectMassBreakdown GetMassBreakdown(double cellSize)
        {
            return SensorGravityDesign.GetSensorMassBreakdown(ref _massBreakdown, this.Scale, cellSize);
        }

        public override PartToolItemBase GetToolItem()
        {
            return new SensorSpinToolItem(this.Options);
        }

        #endregion

        #region Private Methods

        private Model3DGroup CreateGeometry(bool isFinal)
        {
            return SensorGravityDesign.CreateGeometry(this.MaterialBrushes, base.SelectionEmissives,
                GetTransformForGeometry(isFinal),
                WorldColors.SensorBase_Color, WorldColors.SensorBase_Specular, WorldColors.SensorSpin_Color, WorldColors.SensorSpin_Specular,
                isFinal);
        }

        #endregion
    }

    #endregion
    #region Class: SensorSpin

    /// <summary>
    /// Outputs the current angular velocity
    /// NOTE: This ignores all other forces felt (fluid, thrusters, etc)
    /// </summary>
    /// <remarks>
    /// This has a cloud of neurons, and each one's output will approach a value of one based on the dot product of its position and the current
    /// angular velocity vector
    /// </remarks>
    public class SensorSpin : PartBase, INeuronContainer, IPartUpdatable
    {
        #region Declaration Section

        public const string PARTTYPE = "SensorSpin";

        private readonly object _lock = new object();

        private readonly ItemOptions _itemOptions;

        private readonly IContainer _energyTanks;

        private readonly Neuron_SensorPosition[] _neurons;
        private readonly double _neuronMaxRadius;

        private readonly double _volume;		// this is used to calculate energy draw

        #endregion

        #region Constructor

        public SensorSpin(EditorOptions options, ItemOptions itemOptions, ShipPartDNA dna, IContainer energyTanks)
            : base(options, dna, itemOptions.Sensor_Damage.HitpointMin, itemOptions.Sensor_Damage.HitpointSlope, itemOptions.Sensor_Damage.Damage)
        {
            _itemOptions = itemOptions;
            _energyTanks = energyTanks;

            this.Design = new SensorSpinDesign(options, true);
            this.Design.SetDNA(dna);

            double radius;
            SensorGravity.GetMass(out _mass, out _volume, out radius, out _scaleActual, dna, itemOptions);

            this.Radius = radius;

            _neurons = SensorGravity.CreateNeurons(dna, itemOptions, itemOptions.SpinSensor_NeuronDensity);
            _neuronMaxRadius = _neurons.Max(o => o.PositionLength);
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
            lock (_lock)
            {
                if (this.IsDestroyed || _energyTanks == null || _energyTanks.RemoveQuantity(elapsedTime * _volume * _itemOptions.SpinSensor_AmountToDraw * ItemOptions.ENERGYDRAWMULT, true) > 0d)
                {
                    // The energy tank didn't have enough
                    //NOTE: To be clean, I should set the neuron outputs to zero, but anything pulling from them should be checking this
                    //anyway.  So save the processor (also, because of threading, setting them to zero isn't as atomic as this property)
                    _isOn = false;
                    return;
                }

                _isOn = true;

                //Vector3D angularVelocity = GetAngularVelocityModelCoords();		// this shouldn't be translated to model coords, it makes the vector appear to wobble even when the ship has a constant angular velocity
                var worldSpeed = GetWorldSpeed(null);

                // Figure out the magnitude to use
                double magnitude = SensorGravity.CalculateMagnitudePercent(worldSpeed.Item2.Length, _itemOptions.SpinSensor_MaxSpeed);

                SensorGravity.UpdateNeurons(_neurons, _neuronMaxRadius, worldSpeed.Item2, magnitude);
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

        #region Private Methods

        //private Vector3D GetAngularVelocityModelCoords()
        //{
        //    var worldCoords = GetWorldLocation();

        //    // Calculate the difference between the world orientation and model orientation
        //    Quaternion toModelQuat = worldCoords.Item2.ToUnit() * this.Orientation.ToUnit();

        //    RotateTransform3D toModelTransform = new RotateTransform3D(new QuaternionRotation3D(toModelQuat));

        //    // Get the force of gravity in world coords
        //    var worldSpeed = GetWorldSpeed(null);

        //    // Rotate the force into model coords
        //    return toModelTransform.Transform(worldSpeed.Item2);
        //}

        #endregion
    }

    #endregion
}
