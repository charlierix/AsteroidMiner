using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Game.Newt.AsteroidMiner2
{
    /// <summary>
    /// This holds various ratios that will be the same for all parts in a particular instance of a map.
    /// </summary>
    /// <remarks>
    /// I wanted an easy way for a map to define its own ratios and economy.  So you could make fuel expensive, tractor beams cheap, and
    /// see what emerges
    /// 
    /// You could think of this a bit like a css stylesheet for physics.  The code is elsewhere, this is just values
    /// </remarks>
    public class ItemOptions
    {
        #region Mineral Densities

        // These are copied here for convenience (all in kg/m^3)
        // Ice: 934
        // Iron: 7900
        // Graphite: 2267
        // Gold: 19300
        // Platinum: 21450
        // Emerald: 2760
        // Saphire: 4000
        // Ruby: 4000
        // Diamond: 3515
        // Rixium: 66666

        #endregion

        // Egg
        private volatile object _eggDensity = 3500d;
        /// <summary>
        /// This is the mass of an egg that is a size of 1
        /// </summary>
        public double EggDensity
        {
            get
            {
                return (double)_eggDensity;
            }
            set
            {
                _eggDensity = value;
            }
        }

        // Ship
        private volatile object _momentOfInertiaMultiplier = 1d;
        /// <summary>
        /// This won't change the mass, but will make the ship harder/easier to rotate
        /// </summary>
        public double MomentOfInertiaMultiplier
        {
            get
            {
                return (double)_momentOfInertiaMultiplier;
            }
            set
            {
                _momentOfInertiaMultiplier = value;
            }
        }

        // Cargo Bay
        private volatile object _cargoBayWallDensity = 20d;
        /// <summary>
        /// This is the mass of the cargo bay's shell (it should be fairly light)
        /// </summary>
        public double CargoBayWallDensity
        {
            get
            {
                return (double)_cargoBayWallDensity;
            }
            set
            {
                _cargoBayWallDensity = value;
            }
        }

        // Ammo Box
        private volatile object _ammoDensity = 10000d;
        /// <summary>
        /// This is the mass of ammo that is a size of 1
        /// </summary>
        public double AmmoDensity
        {
            get
            {
                return (double)_ammoDensity;
            }
            set
            {
                _ammoDensity = value;
            }
        }

        private volatile object _ammoBoxWallDensity = 20d;
        /// <summary>
        /// This is the mass of the ammo box's shell (it should be fairly light)
        /// </summary>
        public double AmmoBoxWallDensity
        {
            get
            {
                return (double)_ammoBoxWallDensity;
            }
            set
            {
                _ammoBoxWallDensity = value;
            }
        }

        // Fuel Tank
        private volatile object _fuelDensity = 750d;
        /// <summary>
        /// This is the mass of one unit of fuel
        /// </summary>
        /// <remarks>
        /// Water: 998 kg/m3
        /// Gasoline: 720 kg/m3
        /// Diesel: 830 kg/m3
        /// </remarks>
        public double FuelDensity
        {
            get
            {
                return (double)_fuelDensity;
            }
            set
            {
                _fuelDensity = value;
            }
        }

        private volatile object _fuelTankWallDensity = 10d;
        /// <summary>
        /// This is the mass of the fuel tank's shell (it should be fairly light)
        /// </summary>
        public double FuelTankWallDensity
        {
            get
            {
                return (double)_fuelTankWallDensity;
            }
            set
            {
                _fuelTankWallDensity = value;
            }
        }

        // Energy Tank
        private volatile object _energyTankDensity = 1000d;
        /// <summary>
        /// The energy tank has the same mass whether it's empty or full
        /// </summary>
        public double EnergyTankDensity
        {
            get
            {
                return (double)_energyTankDensity;
            }
            set
            {
                _energyTankDensity = value;
            }
        }

        /// <summary>
        /// All of the properties in this class for how much energy to draw need to be multiplied by this value
        /// </summary>
        /// <remarks>
        /// This is so that the values exposed to the user aren't so tiny and hard to work with
        /// </remarks>
        public const double ENERGYDRAWMULT = .001d;

        // Plasma Tank
        private volatile object _plasmaTankDensity = 1800d;
        /// <summary>
        /// The plasma tank has the same mass whether it's empty or full
        /// </summary>
        public double PlasmaTankDensity
        {
            get
            {
                return (double)_plasmaTankDensity;
            }
            set
            {
                _plasmaTankDensity = value;
            }
        }

        // Matter Converters
        private volatile object _matterConverterWallDensity = 600d;
        /// <summary>
        /// This is the mass of a matter converter's shell
        /// </summary>
        /// <remarks>
        /// The wall density needs to be larger, and the volume smaller (because the converters have all kinds of
        /// high tech equipment in them, not just aluminum boxes)
        /// </remarks>
        public double MatterConverterWallDensity
        {
            get
            {
                return (double)_matterConverterWallDensity;
            }
            set
            {
                _matterConverterWallDensity = value;
            }
        }

        private volatile object _matterConverterInternalVolume = .25d;
        /// <summary>
        /// This is how much of the total volume of a matter converter is available to storing the matter it's converting
        /// </summary>
        /// <remarks>
        /// 0 means 0%, 1 means 100%
        /// </remarks>
        public double MatterConverterInternalVolume
        {
            get
            {
                return (double)_matterConverterInternalVolume;
            }
            set
            {
                _matterConverterInternalVolume = value;
            }
        }

        // Matter -> Fuel
        private volatile object _matterToFuelAmountToDraw = 100d;
        /// <summary>
        /// How much matter to pull in one unit of time (this gets multiplied by the size of the converter)
        /// </summary>
        /// <remarks>
        /// The amount is volume * density, so it's the amount of mass that gets burned for one unit of time
        /// </remarks>
        public double MatterToFuelAmountToDraw
        {
            get
            {
                return (double)_matterToFuelAmountToDraw;
            }
            set
            {
                _matterToFuelAmountToDraw = value;
            }
        }

        private volatile object _matterToFuelConversionRate = .002d;		// 500 units of mass for one unit of fuel
        public double MatterToFuelConversionRate
        {
            get
            {
                return (double)_matterToFuelConversionRate;
            }
            set
            {
                _matterToFuelConversionRate = value;
            }
        }

        // Matter -> Energy
        private volatile object _matterToEnergyAmountToDraw = 100d;
        /// <summary>
        /// How much matter to pull in one unit of time (this gets multiplied by the size of the converter)
        /// </summary>
        /// <remarks>
        /// The amount is volume * density, so it's the amount of mass that gets burned for one unit of time
        /// </remarks>
        public double MatterToEnergyAmountToDraw
        {
            get
            {
                return (double)_matterToEnergyAmountToDraw;
            }
            set
            {
                _matterToEnergyAmountToDraw = value;
            }
        }

        private volatile object _matterToEnergyConversionRate = .002d;		// 500 units of mass for one unit of energy
        public double MatterToEnergyConversionRate
        {
            get
            {
                return (double)_matterToEnergyConversionRate;
            }
            set
            {
                _matterToEnergyConversionRate = value;
            }
        }

        // Matter -> Plasma
        private volatile object _matterToPlasmaAmountToDraw = 100d;
        /// <summary>
        /// How much matter to pull in one unit of time (this gets multiplied by the size of the converter)
        /// </summary>
        /// <remarks>
        /// The amount is volume * density, so it's the amount of mass that gets burned for one unit of time
        /// </remarks>
        public double MatterToPlasmaAmountToDraw
        {
            get
            {
                return (double)_matterToPlasmaAmountToDraw;
            }
            set
            {
                _matterToPlasmaAmountToDraw = value;
            }
        }

        private volatile object _matterToPlasmaConversionRate = .001d;		// 1000 units of mass for one unit of plasma
        public double MatterToPlasmaConversionRate
        {
            get
            {
                return (double)_matterToPlasmaConversionRate;
            }
            set
            {
                _matterToPlasmaConversionRate = value;
            }
        }

        // Energy -> Ammo
        private volatile object _energyToAmmoConversionRate = .02d;		// 50 units of energy for one unit of ammo
        public double EnergyToAmmoConversionRate
        {
            get
            {
                return (double)_energyToAmmoConversionRate;
            }
            set
            {
                _energyToAmmoConversionRate = value;
            }
        }

        private volatile object _energyToAmmoAmountToDraw = .1d;
        /// <summary>
        /// How much energy to pull in one unit of time (this gets multiplied by the size of the converter)
        /// </summary>
        public double EnergyToAmmoAmountToDraw
        {
            get
            {
                return (double)_energyToAmmoAmountToDraw;
            }
            set
            {
                _energyToAmmoAmountToDraw = value;
            }
        }

        private volatile object _energyToAmmoDensity = 7500d;
        public double EnergyToAmmoDensity
        {
            get
            {
                return (double)_energyToAmmoDensity;
            }
            set
            {
                _energyToAmmoDensity = value;
            }
        }

        // Energy -> Fuel
        private volatile object _energyToFuelConversionRate = .1d;		// 10 units of energy for one unit of fuel
        public double EnergyToFuelConversionRate
        {
            get
            {
                return (double)_energyToFuelConversionRate;
            }
            set
            {
                _energyToFuelConversionRate = value;
            }
        }

        private volatile object _energyToFuelAmountToDraw = .1d;
        /// <summary>
        /// How much energy to pull in one unit of time (this gets multiplied by the size of the converter)
        /// </summary>
        public double EnergyToFuelAmountToDraw
        {
            get
            {
                return (double)_energyToFuelAmountToDraw;
            }
            set
            {
                _energyToFuelAmountToDraw = value;
            }
        }

        private volatile object _energyToFuelDensity = 7500d;
        public double EnergyToFuelDensity
        {
            get
            {
                return (double)_energyToFuelDensity;
            }
            set
            {
                _energyToFuelDensity = value;
            }
        }

        // Fuel -> Energy
        private volatile object _fuelToEnergyConversionRate = .9d;		// 10 units of fuel for 9 units of energy (it's just burning fuel)
        public double FuelToEnergyConversionRate
        {
            get
            {
                return (double)_fuelToEnergyConversionRate;
            }
            set
            {
                _fuelToEnergyConversionRate = value;
            }
        }

        private volatile object _fuelToEnergyAmountToDraw = 1d;
        /// <summary>
        /// How much fuel to pull in one unit of time (this gets multiplied by the size of the converter)
        /// </summary>
        public double FuelToEnergyAmountToDraw
        {
            get
            {
                return (double)_fuelToEnergyAmountToDraw;
            }
            set
            {
                _fuelToEnergyAmountToDraw = value;
            }
        }

        private volatile object _fuelToEnergyDensity = 500d;
        public double FuelToEnergyDensity
        {
            get
            {
                return (double)_fuelToEnergyDensity;
            }
            set
            {
                _fuelToEnergyDensity = value;
            }
        }

        // Solar Panel (Radiation -> Energy)
        private volatile object _solarPanelConversionRate = .05d;
        public double SolarPanelConversionRate
        {
            get
            {
                return (double)_solarPanelConversionRate;
            }
            set
            {
                _solarPanelConversionRate = value;
            }
        }

        private volatile object _solarPanelDensity = 500d;
        public double SolarPanelDensity
        {
            get
            {
                return (double)_solarPanelDensity;
            }
            set
            {
                _solarPanelDensity = value;
            }
        }

        // Thruster (Fuel -> Force)
        private volatile object _thrusterDensity = 600d;
        public double ThrusterDensity
        {
            get
            {
                return (double)_thrusterDensity;
            }
            set
            {
                _thrusterDensity = value;
            }
        }

        private volatile object _thrusterAdditionalMassPercent = .1d;
        /// <summary>
        /// A ThrusterType of One will just have a base mass.  But a two way thrust will have base + (base * %).
        /// A three way will have base + 2(base * %), etc
        /// </summary>
        public double ThrusterAdditionalMassPercent
        {
            get
            {
                return (double)_thrusterAdditionalMassPercent;
            }
            set
            {
                _thrusterAdditionalMassPercent = value;
            }
        }

        public const double FORCESTRENGTHMULT = 1000d;
        private volatile object _thrusterStrengthRatio = 30d; //30000d;		// thruster takes times FORCESTRENGTHMULT
        /// <summary>
        /// This is the amount of force that the thruster is able to generate (this is taken times the volume of the
        /// thruster cylinder)
        /// </summary>
        /// <remarks>
        /// NOTE: This value is taken times 1000 by the thruster.  This is so that the number exposed to the user isn't so large
        /// </remarks>
        public double ThrusterStrengthRatio
        {
            get
            {
                return (double)_thrusterStrengthRatio;
            }
            set
            {
                _thrusterStrengthRatio = value;
            }
        }

        public const double FUELTOTHRUSTMULT = .000001d;
        private volatile object _fuelToThrustRatio = 1d; //.000001;		// thruster takes this times FUELTOTHRUSTMULT
        public double FuelToThrustRatio
        {
            get
            {
                return (double)_fuelToThrustRatio;
            }
            set
            {
                _fuelToThrustRatio = value;
            }
        }

        private volatile object _thrusterLinksPerNeuron_Sensor = .5d;
        /// <summary>
        /// This uses the destination's number of neurons (always one neuron per thrust line)
        /// </summary>
        public double ThrusterLinksPerNeuron_Sensor
        {
            get
            {
                return (double)_thrusterLinksPerNeuron_Sensor;
            }
            set
            {
                _thrusterLinksPerNeuron_Sensor = value;
            }
        }

        private volatile object _thrusterLinksPerNeuron_Brain = 3d;
        /// <summary>
        /// This uses the destination's number of neurons (always one neuron per thrust line)
        /// </summary>
        public double ThrusterLinksPerNeuron_Brain
        {
            get
            {
                return (double)_thrusterLinksPerNeuron_Brain;
            }
            set
            {
                _thrusterLinksPerNeuron_Brain = value;
            }
        }

        // Sensors
        private volatile object _sensorDensity = 1500d;
        public double SensorDensity
        {
            get
            {
                return (double)_sensorDensity;
            }
            set
            {
                _sensorDensity = value;
            }
        }

        private volatile object _sensorNeuronGrowthExponent = .8d;
        /// <summary>
        /// Even though the sensor appears to be a cube, the neurons are placed in a sphere, but if I calculate
        /// volume as a sphere, then the number of neurons explodes for radius greater than 1, and shrinks
        /// quickly for smaller values.
        /// 
        /// So instead the volume is calculated as Math.Pow(radius, this.SensorNeuronGrowthExponent)
        /// </summary>
        /// <remarks>
        /// If you want spherical growth, just set this to 3
        /// </remarks>
        public double SensorNeuronGrowthExponent
        {
            get
            {
                return (double)_sensorNeuronGrowthExponent;
            }
            set
            {
                _sensorNeuronGrowthExponent = value;
            }
        }

        // Gravity Sensor
        private volatile object _gravitySensorNeuronDensity = 20d;
        /// <summary>
        /// This is how many neurons to place inside of a gravity sensor
        /// </summary>
        public double GravitySensorNeuronDensity
        {
            get
            {
                return (double)_gravitySensorNeuronDensity;
            }
            set
            {
                _gravitySensorNeuronDensity = value;
            }
        }

        private volatile object _gravitySensorAmountToDraw = 1d; //.001d;  this is multiplied by ENERGYDRAWMULT
        public double GravitySensorAmountToDraw
        {
            get
            {
                return (double)_gravitySensorAmountToDraw;
            }
            set
            {
                _gravitySensorAmountToDraw = value;
            }
        }

        // Spin Sensor
        private volatile object _spinSensorNeuronDensity = 20d;
        /// <summary>
        /// This is how many neurons to place inside of a spin sensor
        /// </summary>
        public double SpinSensorNeuronDensity
        {
            get
            {
                return (double)_spinSensorNeuronDensity;
            }
            set
            {
                _spinSensorNeuronDensity = value;
            }
        }

        private volatile object _spinSensorAmountToDraw = 1d; //.001d;		this is multiplied by ENERGYDRAWMULT
        public double SpinSensorAmountToDraw
        {
            get
            {
                return (double)_spinSensorAmountToDraw;
            }
            set
            {
                _spinSensorAmountToDraw = value;
            }
        }

        private volatile object _spinSensorMaxSpeed = 1d;
        /// <summary>
        /// This is the angular speed where the neurons output 1 (anything over this speed is just one)
        /// </summary>
        /// <remarks>
        /// See the gravity sensor for a more elaborite, variable solution.  I decided to make the spin sensor's max fixed.
        /// Because the difference between spinning 1 vs 10 isn't as important as 0 vs 1 (the point of this sensor is to show
        /// how the ship is spinning)
        /// 
        /// But if there are cases where the variable model is needed, add an enum to the gravity sensor (but enums can't
        /// be mutated, so should be avoided)
        /// </remarks>
        public double SpinSensorMaxSpeed
        {
            get
            {
                return (double)_spinSensorMaxSpeed;
            }
            set
            {
                _spinSensorMaxSpeed = value;
            }
        }

        // Velocity Sensor
        private volatile object _velocitySensorNeuronDensity = 20d;
        /// <summary>
        /// This is how many neurons to place inside of a velocity sensor
        /// </summary>
        public double VelocitySensorNeuronDensity
        {
            get
            {
                return (double)_velocitySensorNeuronDensity;
            }
            set
            {
                _velocitySensorNeuronDensity = value;
            }
        }

        private volatile object _velocitySensorAmountToDraw = 1d; //.001d;		this is multiplied by ENERGYDRAWMULT
        public double VelocitySensorAmountToDraw
        {
            get
            {
                return (double)_velocitySensorAmountToDraw;
            }
            set
            {
                _velocitySensorAmountToDraw = value;
            }
        }

        // Camera
        private volatile object _cameraDensity = 1100d;
        public double CameraDensity
        {
            get
            {
                return (double)_cameraDensity;
            }
            set
            {
                _cameraDensity = value;
            }
        }

        private volatile object _cameraNeuronGrowthExponent = .8d;
        /// <summary>
        /// Volume is calculated as Math.Pow(radius, this.CameraNeuronGrowthExponent)
        /// </summary>
        /// <remarks>
        /// If you want growth to be like the area of a circle, just set this to 2
        /// </remarks>
        public double CameraNeuronGrowthExponent
        {
            get
            {
                return (double)_cameraNeuronGrowthExponent;
            }
            set
            {
                _cameraNeuronGrowthExponent = value;
            }
        }

        // CameraColorRGB
        private volatile object _cameraColorRGBNeuronDensity = 20d * 3d;
        /// <summary>
        /// This is how many neurons to place inside of a brain
        /// NOTE: There are 3 plates of neurons, so each plate will be a third of this number
        /// </summary>
        public double CameraColorRGBNeuronDensity
        {
            get
            {
                return (double)_cameraColorRGBNeuronDensity;
            }
            set
            {
                _cameraColorRGBNeuronDensity = value;
            }
        }

        private volatile object _cameraColorRGBAmountToDraw = 3d; //this is multiplied by ENERGYDRAWMULT
        public double CameraColorRGBAmountToDraw
        {
            get
            {
                return (double)_cameraColorRGBAmountToDraw;
            }
            set
            {
                _cameraColorRGBAmountToDraw = value;
            }
        }

        // Brain
        private volatile object _brainDensity = 800d;
        public double BrainDensity
        {
            get
            {
                return (double)_brainDensity;
            }
            set
            {
                _brainDensity = value;
            }
        }

        private volatile object _brainAmountToDraw = 5d; //.005d;  this is multiplied by ENERGYDRAWMULT
        public double BrainAmountToDraw
        {
            get
            {
                return (double)_brainAmountToDraw;
            }
            set
            {
                _brainAmountToDraw = value;
            }
        }

        private volatile object _brainNeuronGrowthExponent = .8d;
        /// <summary>
        /// If I calculate volume as a sphere, then the number of neurons explodes for radius greater than 1, and shrinks
        /// quickly for smaller values.
        /// 
        /// So instead the volume is calculated as Math.Pow(radius, this.BrainNeuronGrowthExponent)
        /// </summary>
        /// <remarks>
        /// If you want spherical growth, just set this to 3
        /// </remarks>
        public double BrainNeuronGrowthExponent
        {
            get
            {
                return (double)_brainNeuronGrowthExponent;
            }
            set
            {
                _brainNeuronGrowthExponent = value;
            }
        }

        private volatile object _brainNeuronDensity = 40d;
        /// <summary>
        /// This is how many neurons to place inside of a brain
        /// </summary>
        public double BrainNeuronDensity
        {
            get
            {
                return (double)_brainNeuronDensity;
            }
            set
            {
                _brainNeuronDensity = value;
            }
        }

        private volatile object _brainChemicalDensity = 3d;
        /// <summary>
        /// This is how many brain chemical neurons to place inside of a brain
        /// </summary>
        public double BrainChemicalDensity
        {
            get
            {
                return (double)_brainChemicalDensity;
            }
            set
            {
                _brainChemicalDensity = value;
            }
        }

        private volatile object _brainNeuronMinClusterDistPercent = .075d;
        /// <summary>
        /// Neurons won't be allowed to get closer together than this value
        /// (This is a percent of diameter)
        /// </summary>
        public double BrainNeuronMinClusterDistPercent
        {
            get
            {
                return (double)_brainNeuronMinClusterDistPercent;
            }
            set
            {
                _brainNeuronMinClusterDistPercent = value;
            }
        }

        private volatile object _brainLinksPerNeuron_Internal = 3d;
        public double BrainLinksPerNeuron_Internal
        {
            get
            {
                return (double)_brainLinksPerNeuron_Internal;
            }
            set
            {
                _brainLinksPerNeuron_Internal = value;
            }
        }

        private volatile object _brainLinksPerNeuron_External_FromSensor = 1.2d;
        /// <summary>
        /// This uses the one with the fewer neurons (which is likely to be the sensor)
        /// </summary>
        public double BrainLinksPerNeuron_External_FromSensor
        {
            get
            {
                return (double)_brainLinksPerNeuron_External_FromSensor;
            }
            set
            {
                _brainLinksPerNeuron_External_FromSensor = value;
            }
        }

        private volatile object _brainLinksPerNeuron_External_FromBrain = .1d;
        /// <summary>
        /// This uses the average number of neurons between the two brains
        /// </summary>
        public double BrainLinksPerNeuron_External_FromBrain
        {
            get
            {
                return (double)_brainLinksPerNeuron_External_FromBrain;
            }
            set
            {
                _brainLinksPerNeuron_External_FromBrain = value;
            }
        }

        private volatile object _brainLinksPerNeuron_External_FromManipulator = 1.5d;
        /// <summary>
        /// This uses the one with the fewer neurons (which is likely to be the manipulator)
        /// NOTE: This only considers the number of readable neurons exposed from the manipulator (which could be zero, the manipulator
        /// could be writeonly)
        /// </summary>
        public double BrainLinksPerNeuron_External_FromManipulator
        {
            get
            {
                return (double)_brainLinksPerNeuron_External_FromManipulator;
            }
            set
            {
                _brainLinksPerNeuron_External_FromManipulator = value;
            }
        }

        private volatile object _neuralLinkMaxWeight = 2d;
        public double NeuralLinkMaxWeight
        {
            get
            {
                return (double)_neuralLinkMaxWeight;
            }
            set
            {
                _neuralLinkMaxWeight = value;
            }
        }
    }
}
