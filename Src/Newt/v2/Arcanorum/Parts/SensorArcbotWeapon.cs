using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.Arcanorum.MapObjects;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.GameItems.ShipParts;
using Game.Newt.v2.NewtonDynamics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media.Media3D;

namespace Game.Newt.v2.Arcanorum.Parts
{
    //TODO: Move this to the Arcanorum project so that it can reference the arcbot and weapons

    #region class: SensorArcbotWeaponToolItem

    public class SensorArcbotWeaponToolItem : PartToolItemBase
    {
        #region Constructor

        public SensorArcbotWeaponToolItem(EditorOptions options)
            : base(options)
        {
            TabName = PartToolItemBase.TAB_SHIPPART;
            _visual2D = PartToolItemBase.GetVisual2D(Name, Description, options, this);
        }

        #endregion

        #region Public Properties

        public override string Name
        {
            get
            {
                return "Arcbot's Weapon Sensor";
            }
        }
        public override string Description
        {
            get
            {
                return "Shows weapon's location relative to the arcbot as the weapon spins";
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
            return new SensorArcbotWeaponDesign(Options, false);
        }

        #endregion
    }

    #endregion
    #region class: SensorArcbotWeaponDesign

    public class SensorArcbotWeaponDesign : PartDesignBase
    {
        #region Declaration Section

        public const PartDesignAllowedScale ALLOWEDSCALE = PartDesignAllowedScale.XYZ;		// This is here so the scale can be known through reflection

        private MassBreakdownCache _massBreakdown = null;

        #endregion

        #region Constructor

        public SensorArcbotWeaponDesign(EditorOptions options, bool isFinalModel)
            : base(options, isFinalModel) { }

        #endregion

        #region Public Properties

        public override PartDesignAllowedScale AllowedScale => ALLOWEDSCALE;
        public override PartDesignAllowedRotation AllowedRotation => PartDesignAllowedRotation.X_Y_Z;

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
            return SensorGravityDesign.CreateSensorCollisionHull(world, Scale, Orientation, Position);
        }
        public override UtilityNewt.IObjectMassBreakdown GetMassBreakdown(double cellSize)
        {
            return SensorGravityDesign.GetSensorMassBreakdown(ref _massBreakdown, Scale, cellSize);
        }

        public override PartToolItemBase GetToolItem()
        {
            return new SensorArcbotWeaponToolItem(Options);
        }

        #endregion

        #region Private Methods

        private Model3DGroup CreateGeometry(bool isFinal)
        {
            return SensorGravityDesign.CreateGeometry(MaterialBrushes, SelectionEmissives,
                GetTransformForGeometry(isFinal),
                WorldColors.SensorBase_Color, WorldColors.SensorBase_Specular, WorldColorsArco.SensorArcbotWeapon_Color, WorldColorsArco.SensorArcbotWeapon_Specular,
                isFinal);
        }

        #endregion
    }

    #endregion
    #region class: SensorArcbotWeapon

    public class SensorArcbotWeapon : PartBase, INeuronContainer, IPartUpdatable
    {
        #region Declaration Section

        public const string PARTTYPE = nameof(SensorArcbotWeapon);

        private readonly ItemOptionsArco _itemOptions;
        private readonly IContainer _energyTanks;

        private readonly Neuron_SensorPosition[] _neurons;

        private readonly double _volume;        // this is used to calculate energy draw

        #endregion

        #region Constructor

        public SensorArcbotWeapon(EditorOptions options, ItemOptionsArco itemOptions, ShipPartDNA dna, IContainer energyTanks)
            : base(options, dna, itemOptions.Sensor_Damage.HitpointMin, itemOptions.Sensor_Damage.HitpointSlope, itemOptions.Sensor_Damage.Damage)
        {
            _itemOptions = itemOptions;
            _energyTanks = energyTanks;

            Design = new SensorArcbotWeaponDesign(options, true);
            Design.SetDNA(dna);

            SensorGravity.GetMass(out _mass, out _volume, out double radius, out _scaleActual, dna, itemOptions);

            Radius = radius;

            _neurons = CreateNeurons(dna, itemOptions, itemOptions.SensorArcbotWeapon_NeuronDensity, true);


            //TODO: Need to know:
            //      the size of the bot --- might just assume a hardcoded value
            //      size of the weapons --- might also just hardcode
            //      access to the bot's weapon, or lack of
            //      access to the current drag plane so that the circle of neurons is accurate


        }

        #endregion

        #region INeuronContainer Members

        public IEnumerable<INeuron> Neruons_Readonly => _neurons;
        public IEnumerable<INeuron> Neruons_ReadWrite => Enumerable.Empty<INeuron>();
        public IEnumerable<INeuron> Neruons_Writeonly => Enumerable.Empty<INeuron>();

        public IEnumerable<INeuron> Neruons_All => _neurons;

        public NeuronContainerType NeuronContainerType => NeuronContainerType.Sensor;

        public double Radius { get; private set; }

        private volatile bool _isOn = false;
        public bool IsOn => _isOn;

        #endregion
        #region IPartUpdatable Members

        public void Update_MainThread(double elapsedTime)
        {
        }
        public void Update_AnyThread(double elapsedTime)
        {
            if (IsDestroyed || _energyTanks == null || _energyTanks.RemoveQuantity(elapsedTime * _volume * _itemOptions.SensorArcbotWeapon_AmountToDraw * ItemOptions.ENERGYDRAWMULT, true) > 0d)
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
        public override double DryMass => _mass;
        public override double TotalMass => _mass;

        private readonly Vector3D _scaleActual;
        public override Vector3D ScaleActual => _scaleActual;

        public Point3D[] NeuronWorldPositions { get; private set; }

        public ArcBot ArcBot { get; set; }

        #endregion

        #region Private Methods

        private void UpdateNeurons()
        {
            foreach(var neuron in _neurons)
            {
                neuron.Value = 0;
            }
        }

        private static Neuron_SensorPosition[] CreateNeurons(ShipPartDNA dna, ItemOptions itemOptions, double neuronDensity, bool ignoreSetValue)
        {
            #region calculate counts

            // Figure out how many to make
            //NOTE: This radius isn't taking SCALE into account.  The other neural parts do this as well, so the neural density properties can be more consistent
            double radius = Math1D.Avg(dna.Scale.X, dna.Scale.Y, dna.Scale.Z) / 2d;     // dividing by 2, because radius is wanted, not diameter
            double area = Math.Pow(radius, itemOptions.Sensor_NeuronGrowthExponent);

            int neuronCount = (neuronDensity * area).ToInt_Ceiling();
            neuronCount = Math.Max(neuronCount, 30);

            #endregion

            // Place them evenly on the surface of a sphere
            Vector3D[] positions = NeuralUtility.GetNeuronPositions_Circular_Even(dna.Neurons, neuronCount, radius);

            return positions.
                Select(o => new Neuron_SensorPosition(o.ToPoint(), true, ignoreSetValue)).
                ToArray();
        }

        #endregion
    }

    #endregion
}
