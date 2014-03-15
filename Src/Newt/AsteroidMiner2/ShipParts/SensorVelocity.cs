using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using Game.Newt.AsteroidMiner2.ShipEditor;
using Game.Newt.HelperClasses;
using Game.Newt.NewtonDynamics;

namespace Game.Newt.AsteroidMiner2.ShipParts
{
    #region Class: SensorVelocityToolItem

    public class SensorVelocityToolItem : PartToolItemBase
    {
        #region Constructor

        public SensorVelocityToolItem(EditorOptions options)
            : base(options)
        {
            _visual2D = PartToolItemBase.GetVisual2D(this.Name, this.Description, options.EditorColors);
            this.TabName = PartToolItemBase.TAB_SHIPPART;
        }

        #endregion

        #region Public Properties

        public override string Name
        {
            get
            {
                return "Velocity Sensor";
            }
        }
        public override string Description
        {
            get
            {
                return "Reports the current absolute velocity";
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
            return new SensorVelocityDesign(this.Options);
        }

        #endregion
    }

    #endregion
    #region Class: SensorVelocityDesign

    public class SensorVelocityDesign : PartDesignBase
    {
        #region Declaration Section

        public const PartDesignAllowedScale ALLOWEDSCALE = PartDesignAllowedScale.XYZ;		// This is here so the scale can be known through reflection

        private Tuple<UtilityNewt.IObjectMassBreakdown, Vector3D, double> _massBreakdown = null;

        #endregion

        #region Constructor

        public SensorVelocityDesign(EditorOptions options)
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

        private Model3DGroup _geometry = null;
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

        public override PartDNA GetDNA()
        {
            PartNeuralDNA retVal = new PartNeuralDNA();

            base.FillDNA(retVal);

            return retVal;
        }
        public override void SetDNA(PartDNA dna)
        {
            if (!(dna is PartNeuralDNA))
            {
                throw new ArgumentException("The class passed in must be PartNeuralDNA");
            }

            base.StoreDNA(dna);
        }

        public override CollisionHull CreateCollisionHull(WorldBase world)
        {
            return SensorGravityDesign.CreateSensorCollisionHull(world, this.Scale, this.Orientation, this.Position);
        }
        public override UtilityNewt.IObjectMassBreakdown GetMassBreakdown(double cellSize)
        {
            return SensorGravityDesign.GetSensorMassBreakdown(ref _massBreakdown, this.Scale, cellSize);
        }

        #endregion

        #region Private Methods

        private Model3DGroup CreateGeometry(bool isFinal)
        {
            return SensorGravityDesign.CreateGeometry(this.MaterialBrushes, base.SelectionEmissives,
                GetTransformForGeometry(isFinal),
                WorldColors.SensorBase, WorldColors.SensorBaseSpecular, WorldColors.SensorVelocity, WorldColors.SensorVelocitySpecular,
                isFinal);
        }

        #endregion
    }

    #endregion
    #region Class: SensorVelocity

    /// <summary>
    /// This reports the current absolute velocity
    /// </summary>
    /// <remarks>
    /// This sensor is a cheat, and the desicion to allow it should be carefully considered (a bot should know how fast it's going based
    /// on other senses)
    /// 
    /// But it could be useful for quick and dirty simulations, and to keep brain complexity down
    /// </remarks>
    public class SensorVelocity : PartBase, INeuronContainer, IPartUpdatable
    {
        #region Declaration Section

        public const string PARTTYPE = "SensorVelocity";

        private ItemOptions _itemOptions = null;

        private IContainer _energyTanks = null;

        private Neuron_SensorPosition[] _neurons = null;
        private double _neuronMaxRadius = 0d;

        private double _volume = 0d;		// this is used to calculate energy draw

        private SensorGravity.MagnitudeHistory _magnitudeHistory = new SensorGravity.MagnitudeHistory();

        #endregion

        #region Constructor

        public SensorVelocity(EditorOptions options, ItemOptions itemOptions, PartNeuralDNA dna, IContainer energyTanks)
            : base(options, dna)
        {
            _itemOptions = itemOptions;
            _energyTanks = energyTanks;

            this.Design = new SensorVelocityDesign(options);
            this.Design.SetDNA(dna);

            double radius;
            SensorGravity.GetMass(out _mass, out _volume, out radius, out _scaleActual, dna, itemOptions);

            this.Radius = radius;

            _neurons = SensorGravity.CreateNeurons(dna, itemOptions, itemOptions.VelocitySensorNeuronDensity);
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

        public void Update(double elapsedTime)
        {
            if (_energyTanks.RemoveQuantity(elapsedTime * _volume * _itemOptions.VelocitySensorAmountToDraw * ItemOptions.ENERGYDRAWMULT, true) > 0d)
            {
                // The energy tank didn't have enough
                //NOTE: To be clean, I should set the neuron outputs to zero, but anything pulling from them should be checking this
                //anyway.  So save the processor (also, because of threading, setting them to zero isn't as atomic as this property)
                _isOn = false;
                return;
            }

            _isOn = true;

            //Vector3D velocity = GetVelocityModelCoords();		// this shouldn't be translated to model coords, it makes the vector appear to wobble if the ship is spinning, even when the ship has a constant velocity
            var worldSpeed = GetWorldSpeed(null);

            // Figure out the magnitude to use
            double magnitude = CalculateMagnitude(worldSpeed.Item1.Length, elapsedTime);

            SensorGravity.UpdateNeurons(_neurons, _neuronMaxRadius, worldSpeed.Item1, magnitude);
        }

        #endregion

        #region Public Properties

        private double _mass = 0d;
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

        private Vector3D _scaleActual;
        public override Vector3D ScaleActual
        {
            get
            {
                return _scaleActual;
            }
        }

        #endregion

        #region Private Methods

        private double CalculateMagnitude(double currentStrength, double elapsedTime)
        {
            // Add to history
            _magnitudeHistory.StoreMagnitude(currentStrength, elapsedTime);

            // Figure out the percent
            return SensorGravity.CalculateMagnitudePercent(currentStrength, _magnitudeHistory.CurrentMax);
        }

        #endregion
    }

    #endregion
}
