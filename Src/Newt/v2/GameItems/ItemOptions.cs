using Game.HelperClassesWPF;
using System;
using System.Threading;
using System.Windows.Media;

namespace Game.Newt.v2.GameItems
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
        #region mineral densities

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

        #region hit points

        public const double HITPOINTMIN = 32;
        public const double HITPOINTSLOPE = 1;

        public const double DAMAGE_VELOCITYTHRESHOLD = 8.5;       // I saw speed of 6 for really light taps, 100 for incredibly fast hits (so figure the standard range is 12 - 40)
        public const double DAMAGE_ENERGYTHESHOLD = 0;
        //public const double DAMAGE_ENERGYTOHITPOINTMULT = 1d / 12000000d;      // energy is velocity squared, but hitpoints are linear
        public const double DAMAGE_ENERGYTOHITPOINTMULT = 1d / 500000d;
        public const double DAMAGE_RANDOMMAX = 2.2;
        public static RandomBellArgs DAMAGE_RANDOMBELL = new RandomBellArgs(1, -45, 1, -45);      // this is a bell curve centered at x=.5 (see nonlinear random tester for a visual)

        #endregion
        #region class: DamageProps

        public class DamageProps
        {
            public DamageProps(double hitpointMin, double hitpointSlope, double damage_VelocityThreshold, double damage_EnergyTheshold, double damage_EnergyToHitpointMult, double damage_RandomMult)
            {
                _hitpointMin = hitpointMin;
                _hitpointSlope = hitpointSlope;
                _damage_VelocityThreshold = damage_VelocityThreshold;
                _damage_EnergyTheshold = damage_EnergyTheshold;
                _damage_EnergyToHitpointMult = damage_EnergyToHitpointMult;
                _damage_RandomMult = damage_RandomMult;
            }

            private volatile object _hitpointMin = HITPOINTMIN;
            public double HitpointMin
            {
                get
                {
                    return (double)_hitpointMin;
                }
                set
                {
                    _hitpointMin = value;
                }
            }

            private volatile object _hitpointSlope = HITPOINTSLOPE;
            public double HitpointSlope
            {
                get
                {
                    return (double)_hitpointSlope;
                }
                set
                {
                    _hitpointSlope = value;
                }
            }

            private volatile object _damage_VelocityThreshold = DAMAGE_VELOCITYTHRESHOLD;
            public double Damage_VelocityThreshold
            {
                get
                {
                    return (double)_damage_VelocityThreshold;
                }
                set
                {
                    _damage_VelocityThreshold = value;
                    _damage = null;
                }
            }

            private volatile object _damage_EnergyTheshold = DAMAGE_ENERGYTHESHOLD;
            public double Damage_EnergyTheshold
            {
                get
                {
                    return (double)_damage_EnergyTheshold;
                }
                set
                {
                    _damage_EnergyTheshold = value;
                    _damage = null;
                }
            }

            private volatile object _damage_EnergyToHitpointMult = DAMAGE_ENERGYTOHITPOINTMULT;
            public double Damage_EnergyToHitpointMult
            {
                get
                {
                    return (double)_damage_EnergyToHitpointMult;
                }
                set
                {
                    _damage_EnergyToHitpointMult = value;
                    _damage = null;
                }
            }

            private volatile object _damage_RandomMult = DAMAGE_RANDOMMAX;
            public double Damage_RandomMult
            {
                get
                {
                    return (double)_damage_RandomMult;
                }
                set
                {
                    _damage_RandomMult = value;
                    _damage = null;
                }
            }

            private volatile TakesDamageWorker _damage = null;
            public TakesDamageWorker Damage
            {
                get
                {
                    if (_damage == null)
                    {
                        TakesDamageWorker_Props props = new TakesDamageWorker_Props()
                        {
                            VelocityThreshold = Damage_VelocityThreshold,
                            EnergyTheshold = Damage_EnergyTheshold,
                            EnergyToHitpointMult = Damage_EnergyToHitpointMult,
                            RandomPercent = Damage_RandomMult,
                        };
                        TakesDamageWorker newInstance = new TakesDamageWorker(props);       // currently not bothering with type specific overrides
                        Interlocked.CompareExchange(ref _damage, newInstance, null);       //NOTE: This only stores the new instance if it's still null (there's a chance that another thread already populated the volatile)
                    }

                    return _damage;
                }
                set
                {
                    _damage = value;
                }
            }
        }

        #endregion

        #region egg

        private volatile object _egg_Density = 3500d;
        /// <summary>
        /// This is the mass of an egg that is a size of 1
        /// </summary>
        public double Egg_Density
        {
            get
            {
                return (double)_egg_Density;
            }
            set
            {
                _egg_Density = value;
            }
        }

        #endregion

        #region ship

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

        private volatile object _partMap_FuzzyLink_MaxLinkPercent = 1.2;
        public double PartMap_FuzzyLink_MaxLinkPercent
        {
            get
            {
                return (double)_partMap_FuzzyLink_MaxLinkPercent;
            }
            set
            {
                _partMap_FuzzyLink_MaxLinkPercent = value;
            }
        }

        private volatile object _partMap_FuzzyLink_MaxIntermediateCount = 3;
        public int PartMap_FuzzyLink_MaxIntermediateCount
        {
            get
            {
                return (int)_partMap_FuzzyLink_MaxIntermediateCount;
            }
            set
            {
                _partMap_FuzzyLink_MaxIntermediateCount = value;
            }
        }

        #endregion

        #region misc neural

        private volatile object _neural_ExistingRatioCap = 1.05;
        /// <summary>
        /// When linking from a source to destination container, and there are existing links, the relinker can potentially create
        /// more links (because uncertain links will create extra to nearby neurons)
        /// 
        /// This is a ratio that caps the final count (final = (source * ratio).ToIntRound)
        /// </summary>
        public double Neural_ExistingRatioCap
        {
            get
            {
                return (double)_neural_ExistingRatioCap;
            }
            set
            {
                _neural_ExistingRatioCap = value;
            }
        }

        #endregion

        // Containers
        #region cargo bay

        private volatile object _cargoBay_WallDensity = 20d;
        /// <summary>
        /// This is the mass of the cargo bay's shell (it should be fairly light)
        /// </summary>
        public double CargoBay_WallDensity
        {
            get
            {
                return (double)_cargoBay_WallDensity;
            }
            set
            {
                _cargoBay_WallDensity = value;
            }
        }

        public readonly DamageProps CargoBay_Damage = new DamageProps(
            HITPOINTMIN,
            HITPOINTSLOPE,
            DAMAGE_VELOCITYTHRESHOLD,
            DAMAGE_ENERGYTHESHOLD,
            DAMAGE_ENERGYTOHITPOINTMULT,
            DAMAGE_RANDOMMAX);

        #endregion
        #region ammo box

        private volatile object _ammo_Density = 10000d;
        /// <summary>
        /// This is the mass of ammo that is a size of 1
        /// </summary>
        public double Ammo_Density
        {
            get
            {
                return (double)_ammo_Density;
            }
            set
            {
                _ammo_Density = value;
            }
        }

        private volatile object _ammoBox_WallDensity = 20d;
        /// <summary>
        /// This is the mass of the ammo box's shell (it should be fairly light)
        /// </summary>
        public double AmmoBox_WallDensity
        {
            get
            {
                return (double)_ammoBox_WallDensity;
            }
            set
            {
                _ammoBox_WallDensity = value;
            }
        }

        public readonly DamageProps AmmoBox_Damage = new DamageProps(
            HITPOINTMIN,
            HITPOINTSLOPE,
            DAMAGE_VELOCITYTHRESHOLD,
            DAMAGE_ENERGYTHESHOLD,
            DAMAGE_ENERGYTOHITPOINTMULT,
            DAMAGE_RANDOMMAX);

        #endregion
        #region fuel tank

        private volatile object _fuel_Density = 750d;
        /// <summary>
        /// This is the mass of one unit of fuel
        /// </summary>
        /// <remarks>
        /// Water: 998 kg/m3
        /// Gasoline: 720 kg/m3
        /// Diesel: 830 kg/m3
        /// </remarks>
        public double Fuel_Density
        {
            get
            {
                return (double)_fuel_Density;
            }
            set
            {
                _fuel_Density = value;
            }
        }

        private volatile object _fuelTank_WallDensity = 10d;
        /// <summary>
        /// This is the mass of the fuel tank's shell (it should be fairly light)
        /// </summary>
        public double FuelTank_WallDensity
        {
            get
            {
                return (double)_fuelTank_WallDensity;
            }
            set
            {
                _fuelTank_WallDensity = value;
            }
        }

        public readonly DamageProps FuelTank_Damage = new DamageProps(
            HITPOINTMIN,
            HITPOINTSLOPE,
            DAMAGE_VELOCITYTHRESHOLD,
            DAMAGE_ENERGYTHESHOLD,
            DAMAGE_ENERGYTOHITPOINTMULT,
            DAMAGE_RANDOMMAX);

        #endregion
        #region energy tank

        private volatile object _energyTank_Density = 1000d;
        /// <summary>
        /// The energy tank has the same mass whether it's empty or full
        /// </summary>
        public double EnergyTank_Density
        {
            get
            {
                return (double)_energyTank_Density;
            }
            set
            {
                _energyTank_Density = value;
            }
        }

        /// <summary>
        /// All of the properties in this class for how much energy to draw need to be multiplied by this value
        /// </summary>
        /// <remarks>
        /// This is so that the values exposed to the user aren't so tiny and hard to work with
        /// </remarks>
        public const double ENERGYDRAWMULT = .001d;

        public readonly DamageProps EnergyTank_Damage = new DamageProps(
            HITPOINTMIN,
            HITPOINTSLOPE,
            DAMAGE_VELOCITYTHRESHOLD,
            DAMAGE_ENERGYTHESHOLD,
            DAMAGE_ENERGYTOHITPOINTMULT,
            DAMAGE_RANDOMMAX);

        #endregion
        #region plasma tank

        private volatile object _plasmaTank_Density = 1800d;
        /// <summary>
        /// The plasma tank has the same mass whether it's empty or full
        /// </summary>
        public double PlasmaTank_Density
        {
            get
            {
                return (double)_plasmaTank_Density;
            }
            set
            {
                _plasmaTank_Density = value;
            }
        }

        public readonly DamageProps PlasmaTank_Damage = new DamageProps(
            HITPOINTMIN,
            HITPOINTSLOPE,
            DAMAGE_VELOCITYTHRESHOLD,
            DAMAGE_ENERGYTHESHOLD,
            DAMAGE_ENERGYTOHITPOINTMULT,
            DAMAGE_RANDOMMAX);

        #endregion

        // Converters
        #region matter converters - all

        private volatile object _matterConverter_WallDensity = 600d;
        /// <summary>
        /// This is the mass of a matter converter's shell
        /// </summary>
        /// <remarks>
        /// The wall density needs to be larger, and the volume smaller (because the converters have all kinds of
        /// high tech equipment in them, not just aluminum boxes)
        /// </remarks>
        public double MatterConverter_WallDensity
        {
            get
            {
                return (double)_matterConverter_WallDensity;
            }
            set
            {
                _matterConverter_WallDensity = value;
            }
        }

        private volatile object _matterConverter_InternalVolume = .25d;
        /// <summary>
        /// This is how much of the total volume of a matter converter is available to storing the matter it's converting
        /// </summary>
        /// <remarks>
        /// 0 means 0%, 1 means 100%
        /// </remarks>
        public double MatterConverter_InternalVolume
        {
            get
            {
                return (double)_matterConverter_InternalVolume;
            }
            set
            {
                _matterConverter_InternalVolume = value;
            }
        }

        #endregion
        #region matter converters - matter ->

        public readonly DamageProps MatterConverter_Damage = new DamageProps(
            HITPOINTMIN,
            HITPOINTSLOPE,
            DAMAGE_VELOCITYTHRESHOLD,
            DAMAGE_ENERGYTHESHOLD,
            DAMAGE_ENERGYTOHITPOINTMULT,
            DAMAGE_RANDOMMAX);

        #endregion
        #region matter converters - energy ->

        public readonly DamageProps EnergyConverter_Damage = new DamageProps(
            HITPOINTMIN,
            HITPOINTSLOPE,
            DAMAGE_VELOCITYTHRESHOLD,
            DAMAGE_ENERGYTHESHOLD,
            DAMAGE_ENERGYTOHITPOINTMULT,
            DAMAGE_RANDOMMAX);

        #endregion
        #region matter -> fuel

        private volatile object _matterToFuel_AmountToDraw = 100d;
        /// <summary>
        /// How much matter to pull in one unit of time (this gets multiplied by the size of the converter)
        /// </summary>
        /// <remarks>
        /// The amount is volume * density, so it's the amount of mass that gets burned for one unit of time
        /// </remarks>
        public double MatterToFuel_AmountToDraw
        {
            get
            {
                return (double)_matterToFuel_AmountToDraw;
            }
            set
            {
                _matterToFuel_AmountToDraw = value;
            }
        }

        private volatile object _matterToFuel_ConversionRate = .01d;		// 100 units of mass for one unit of fuel
        public double MatterToFuel_ConversionRate
        {
            get
            {
                return (double)_matterToFuel_ConversionRate;
            }
            set
            {
                _matterToFuel_ConversionRate = value;
            }
        }

        #endregion
        #region matter -> energy

        private volatile object _matterToEnergy_AmountToDraw = 100d;
        /// <summary>
        /// How much matter to pull in one unit of time (this gets multiplied by the size of the converter)
        /// </summary>
        /// <remarks>
        /// The amount is volume * density, so it's the amount of mass that gets burned for one unit of time
        /// </remarks>
        public double MatterToEnergy_AmountToDraw
        {
            get
            {
                return (double)_matterToEnergy_AmountToDraw;
            }
            set
            {
                _matterToEnergy_AmountToDraw = value;
            }
        }

        private volatile object _matterToEnergy_ConversionRate = .02d;		// 50 units of mass for one unit of energy?
        public double MatterToEnergy_ConversionRate
        {
            get
            {
                return (double)_matterToEnergy_ConversionRate;
            }
            set
            {
                _matterToEnergy_ConversionRate = value;
            }
        }

        #endregion
        #region matter -> plasma

        private volatile object _matterToPlasma_AmountToDraw = 100d;
        /// <summary>
        /// How much matter to pull in one unit of time (this gets multiplied by the size of the converter)
        /// </summary>
        /// <remarks>
        /// The amount is volume * density, so it's the amount of mass that gets burned for one unit of time
        /// </remarks>
        public double MatterToPlasma_AmountToDraw
        {
            get
            {
                return (double)_matterToPlasma_AmountToDraw;
            }
            set
            {
                _matterToPlasma_AmountToDraw = value;
            }
        }

        private volatile object _matterToPlasma_ConversionRate = .005d;		// 500 units of mass for one unit of plasma? (or 200?)
        public double MatterToPlasma_ConversionRate
        {
            get
            {
                return (double)_matterToPlasma_ConversionRate;
            }
            set
            {
                _matterToPlasma_ConversionRate = value;
            }
        }

        #endregion
        #region matter -> ammo

        private volatile object _matterToAmmo_AmountToDraw = 100d;
        /// <summary>
        /// How much matter to pull in one unit of time (this gets multiplied by the size of the converter)
        /// </summary>
        /// <remarks>
        /// The amount is volume * density, so it's the amount of mass that gets burned for one unit of time
        /// </remarks>
        public double MatterToAmmo_AmountToDraw
        {
            get
            {
                return (double)_matterToAmmo_AmountToDraw;
            }
            set
            {
                _matterToAmmo_AmountToDraw = value;
            }
        }

        private volatile object _matterToAmmo_ConversionRate = .01d;		// 100 units of mass for one unit of ammo
        public double MatterToAmmo_ConversionRate
        {
            get
            {
                return (double)_matterToAmmo_ConversionRate;
            }
            set
            {
                _matterToAmmo_ConversionRate = value;
            }
        }

        #endregion
        #region energy -> ammo

        private volatile object _energyToAmmo_ConversionRate = .033d;		// 30 units of energy for one unit of ammo
        public double EnergyToAmmo_ConversionRate
        {
            get
            {
                return (double)_energyToAmmo_ConversionRate;
            }
            set
            {
                _energyToAmmo_ConversionRate = value;
            }
        }

        private volatile object _energyToAmmo_AmountToDraw = .3d;
        /// <summary>
        /// How much energy to pull in one unit of time (this gets multiplied by the size of the converter)
        /// </summary>
        public double EnergyToAmmo_AmountToDraw
        {
            get
            {
                return (double)_energyToAmmo_AmountToDraw;
            }
            set
            {
                _energyToAmmo_AmountToDraw = value;
            }
        }

        private volatile object _energyToAmmo_Density = 7500d;
        public double EnergyToAmmo_Density
        {
            get
            {
                return (double)_energyToAmmo_Density;
            }
            set
            {
                _energyToAmmo_Density = value;
            }
        }

        #endregion
        #region energy -> fuel

        private volatile object _energyToFuel_ConversionRate = .1d;		// 10 units of energy for one unit of fuel
        public double EnergyToFuel_ConversionRate
        {
            get
            {
                return (double)_energyToFuel_ConversionRate;
            }
            set
            {
                _energyToFuel_ConversionRate = value;
            }
        }

        private volatile object _energyToFuel_AmountToDraw = .3d;
        /// <summary>
        /// How much energy to pull in one unit of time (this gets multiplied by the size of the converter)
        /// </summary>
        public double EnergyToFuel_AmountToDraw
        {
            get
            {
                return (double)_energyToFuel_AmountToDraw;
            }
            set
            {
                _energyToFuel_AmountToDraw = value;
            }
        }

        private volatile object _energyToFuel_Density = 7500d;
        public double EnergyToFuel_Density
        {
            get
            {
                return (double)_energyToFuel_Density;
            }
            set
            {
                _energyToFuel_Density = value;
            }
        }

        #endregion
        #region energy -> plasma

        private volatile object _energyToPlasma_ConversionRate = .05d;		// 20 units of energy for one unit of plasma
        public double EnergyToPlasma_ConversionRate
        {
            get
            {
                return (double)_energyToPlasma_ConversionRate;
            }
            set
            {
                _energyToPlasma_ConversionRate = value;
            }
        }

        private volatile object _energyToPlasma_AmountToDraw = .3d;
        /// <summary>
        /// How much energy to pull in one unit of time (this gets multiplied by the size of the converter)
        /// </summary>
        public double EnergyToPlasma_AmountToDraw
        {
            get
            {
                return (double)_energyToPlasma_AmountToDraw;
            }
            set
            {
                _energyToPlasma_AmountToDraw = value;
            }
        }

        private volatile object _energyToPlasma_Density = 7500d;
        public double EnergyToPlasma_Density
        {
            get
            {
                return (double)_energyToPlasma_Density;
            }
            set
            {
                _energyToPlasma_Density = value;
            }
        }

        #endregion
        #region fuel -> energy

        private volatile object _fuelToEnergy_ConversionRate = .9d;		// 10 units of fuel for 9 units of energy (it's just burning fuel)
        public double FuelToEnergy_ConversionRate
        {
            get
            {
                return (double)_fuelToEnergy_ConversionRate;
            }
            set
            {
                _fuelToEnergy_ConversionRate = value;
            }
        }

        private volatile object _fuelToEnergy_AmountToDraw = 1d;
        /// <summary>
        /// How much fuel to pull in one unit of time (this gets multiplied by the size of the converter)
        /// </summary>
        public double FuelToEnergy_AmountToDraw
        {
            get
            {
                return (double)_fuelToEnergy_AmountToDraw;
            }
            set
            {
                _fuelToEnergy_AmountToDraw = value;
            }
        }

        private volatile object _fuelToEnergy_Density = 500d;
        public double FuelToEnergy_Density
        {
            get
            {
                return (double)_fuelToEnergy_Density;
            }
            set
            {
                _fuelToEnergy_Density = value;
            }
        }

        public readonly DamageProps FuelToEnergy_Damage = new DamageProps(
            HITPOINTMIN,
            HITPOINTSLOPE,
            DAMAGE_VELOCITYTHRESHOLD,
            DAMAGE_ENERGYTHESHOLD,
            DAMAGE_ENERGYTOHITPOINTMULT,
            DAMAGE_RANDOMMAX);

        #endregion
        #region solar panel (radiation -> energy)

        private volatile object _solarPanel_ConversionRate = .003d;
        public double SolarPanel_ConversionRate
        {
            get
            {
                return (double)_solarPanel_ConversionRate;
            }
            set
            {
                _solarPanel_ConversionRate = value;
            }
        }

        private volatile object _solarPanel_Density = 500d;
        public double SolarPanel_Density
        {
            get
            {
                return (double)_solarPanel_Density;
            }
            set
            {
                _solarPanel_Density = value;
            }
        }

        public readonly DamageProps SolarPanel_Damage = new DamageProps(
            HITPOINTMIN,
            HITPOINTSLOPE,
            DAMAGE_VELOCITYTHRESHOLD,
            DAMAGE_ENERGYTHESHOLD,
            DAMAGE_ENERGYTOHITPOINTMULT,
            DAMAGE_RANDOMMAX);

        #endregion

        // Sensors
        #region sensors - all

        private volatile object _sensor_Density = 1500d;
        public double Sensor_Density
        {
            get
            {
                return (double)_sensor_Density;
            }
            set
            {
                _sensor_Density = value;
            }
        }

        private volatile object _sensor_NeuronGrowthExponent = .8d;
        /// <summary>
        /// Even though the sensor appears to be a cube, the neurons are placed in a sphere, but if volume
        /// is calculated as a sphere, then the number of neurons explodes for radius greater than 1, and shrinks
        /// quickly for smaller values.
        /// 
        /// So instead the volume is calculated as Math.Pow(radius, this.SensorNeuronGrowthExponent)
        /// </summary>
        /// <remarks>
        /// If you want spherical growth, just set this to 3
        /// </remarks>
        public double Sensor_NeuronGrowthExponent
        {
            get
            {
                return (double)_sensor_NeuronGrowthExponent;
            }
            set
            {
                _sensor_NeuronGrowthExponent = value;
            }
        }

        public readonly DamageProps Sensor_Damage = new DamageProps(
            HITPOINTMIN,
            HITPOINTSLOPE,
            DAMAGE_VELOCITYTHRESHOLD,
            DAMAGE_ENERGYTHESHOLD,
            DAMAGE_ENERGYTOHITPOINTMULT,
            DAMAGE_RANDOMMAX);

        #endregion
        #region gravity sensor

        private volatile object _gravitySensor_NeuronDensity = 20d;
        /// <summary>
        /// This is how many neurons to place inside of a gravity sensor
        /// </summary>
        public double GravitySensor_NeuronDensity
        {
            get
            {
                return (double)_gravitySensor_NeuronDensity;
            }
            set
            {
                _gravitySensor_NeuronDensity = value;
            }
        }

        private volatile object _gravitySensor_AmountToDraw = 1d; //.001d;  this is multiplied by ENERGYDRAWMULT
        public double GravitySensor_AmountToDraw
        {
            get
            {
                return (double)_gravitySensor_AmountToDraw;
            }
            set
            {
                _gravitySensor_AmountToDraw = value;
            }
        }

        #endregion
        #region spin sensor

        private volatile object _spinSensor_NeuronDensity = 20d;
        /// <summary>
        /// This is how many neurons to place inside of a spin sensor
        /// </summary>
        public double SpinSensor_NeuronDensity
        {
            get
            {
                return (double)_spinSensor_NeuronDensity;
            }
            set
            {
                _spinSensor_NeuronDensity = value;
            }
        }

        private volatile object _spinSensor_AmountToDraw = 1d; //.001d;		this is multiplied by ENERGYDRAWMULT
        public double SpinSensor_AmountToDraw
        {
            get
            {
                return (double)_spinSensor_AmountToDraw;
            }
            set
            {
                _spinSensor_AmountToDraw = value;
            }
        }

        private volatile object _spinSensor_MaxSpeed = 1d;
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
        public double SpinSensor_MaxSpeed
        {
            get
            {
                return (double)_spinSensor_MaxSpeed;
            }
            set
            {
                _spinSensor_MaxSpeed = value;
            }
        }

        #endregion
        #region velocity sensor

        private volatile object _velocitySensor_NeuronDensity = 20d;
        /// <summary>
        /// This is how many neurons to place inside of a velocity sensor
        /// </summary>
        public double VelocitySensor_NeuronDensity
        {
            get
            {
                return (double)_velocitySensor_NeuronDensity;
            }
            set
            {
                _velocitySensor_NeuronDensity = value;
            }
        }

        private volatile object _velocitySensor_AmountToDraw = 1d; //.001d;		this is multiplied by ENERGYDRAWMULT
        public double VelocitySensor_AmountToDraw
        {
            get
            {
                return (double)_velocitySensor_AmountToDraw;
            }
            set
            {
                _velocitySensor_AmountToDraw = value;
            }
        }

        #endregion
        #region homing sensor

        private volatile object _homingSensor_NeuronDensity = 20d;
        /// <summary>
        /// This is how many neurons to place inside of a homing sensor
        /// </summary>
        public double HomingSensor_NeuronDensity
        {
            get
            {
                return (double)_homingSensor_NeuronDensity;
            }
            set
            {
                _homingSensor_NeuronDensity = value;
            }
        }

        private volatile object _homingSensor_AmountToDraw = 1d; //.001d;  this is multiplied by ENERGYDRAWMULT
        public double HomingSensor_AmountToDraw
        {
            get
            {
                return (double)_homingSensor_AmountToDraw;
            }
            set
            {
                _homingSensor_AmountToDraw = value;
            }
        }

        private volatile object _homingSensor_HomeRadiusPercentOfRadius = 10d;
        public double HomingSensor_HomeRadiusPercentOfRadius
        {
            get
            {
                return (double)_homingSensor_HomeRadiusPercentOfRadius;
            }
            set
            {
                _homingSensor_HomeRadiusPercentOfRadius = value;
            }
        }

        #endregion
        #region camera - all

        private volatile object _camera_Density = 1100d;
        public double Camera_Density
        {
            get
            {
                return (double)_camera_Density;
            }
            set
            {
                _camera_Density = value;
            }
        }

        private volatile object _camera_NeuronGrowthExponent = .8d;
        /// <summary>
        /// Volume is calculated as Math.Pow(radius, this.CameraNeuronGrowthExponent)
        /// </summary>
        /// <remarks>
        /// If you want growth to be like the area of a circle, just set this to 2
        /// </remarks>
        public double Camera_NeuronGrowthExponent
        {
            get
            {
                return (double)_camera_NeuronGrowthExponent;
            }
            set
            {
                _camera_NeuronGrowthExponent = value;
            }
        }

        public readonly DamageProps Camera_Damage = new DamageProps(
            HITPOINTMIN,
            HITPOINTSLOPE,
            DAMAGE_VELOCITYTHRESHOLD,
            DAMAGE_ENERGYTHESHOLD,
            DAMAGE_ENERGYTOHITPOINTMULT,
            DAMAGE_RANDOMMAX);

        #endregion
        #region camera color rgb

        private volatile object _cameraColorRGB_NeuronDensity = 20d * 3d;
        /// <summary>
        /// This is how many neurons to place inside of a brain
        /// NOTE: There are 3 plates of neurons, so each plate will be a third of this number
        /// </summary>
        public double CameraColorRGB_NeuronDensity
        {
            get
            {
                return (double)_cameraColorRGB_NeuronDensity;
            }
            set
            {
                _cameraColorRGB_NeuronDensity = value;
            }
        }

        private volatile object _cameraColorRGB_AmountToDraw = 3d; //this is multiplied by ENERGYDRAWMULT
        public double CameraColorRGB_AmountToDraw
        {
            get
            {
                return (double)_cameraColorRGB_AmountToDraw;
            }
            set
            {
                _cameraColorRGB_AmountToDraw = value;
            }
        }

        public readonly DamageProps CameraColorRGB_Damage = new DamageProps(
            HITPOINTMIN,
            HITPOINTSLOPE,
            DAMAGE_VELOCITYTHRESHOLD,
            DAMAGE_ENERGYTHESHOLD,
            DAMAGE_ENERGYTOHITPOINTMULT,
            DAMAGE_RANDOMMAX);

        #endregion
        #region camera hardcoded

        private volatile object _cameraHardCoded_NeuronDensity = 32d;
        /// <summary>
        /// This is how many neurons to place inside of a cone
        /// </summary>
        public double CameraHardCoded_NeuronDensity
        {
            get
            {
                return (double)_cameraHardCoded_NeuronDensity;
            }
            set
            {
                _cameraHardCoded_NeuronDensity = value;
            }
        }

        private volatile object _cameraHardCoded_WorldMax = 50d;
        /// <summary>
        /// This is how far the camera sees
        /// </summary>
        public double CameraHardCoded_WorldMax
        {
            get
            {
                return (double)_cameraHardCoded_WorldMax;
            }
            set
            {
                _cameraHardCoded_WorldMax = value;
            }
        }

        /// <summary>
        /// This gives a chance for custom classification logic.
        /// See CameraHardCoded.ClassifyObject() for insparation
        /// </summary>
        /// <remarks>
        /// TODO: May want to pass in the bot's token.  Currently, parts don't have access to that, so would need some kind of event, or a property to be set after construction
        /// </remarks>
        public volatile Func<MapObjectInfo, double[]> CameraHardCoded_ClassifyObject = null;

        #endregion

        #region brain

        private volatile object _brain_Density = 800d;
        public double Brain_Density
        {
            get
            {
                return (double)_brain_Density;
            }
            set
            {
                _brain_Density = value;
            }
        }

        private volatile object _brain_AmountToDraw = 5d; //.005d;  this is multiplied by ENERGYDRAWMULT
        public double Brain_AmountToDraw
        {
            get
            {
                return (double)_brain_AmountToDraw;
            }
            set
            {
                _brain_AmountToDraw = value;
            }
        }

        private volatile object _brain_NeuronGrowthExponent = .8d;
        /// <summary>
        /// If you calculate volume as a sphere, then the number of neurons explodes for radius greater than 1, and shrinks
        /// quickly for smaller values.
        /// 
        /// So instead the volume is calculated as Math.Pow(radius, this.BrainNeuronGrowthExponent)
        /// </summary>
        /// <remarks>
        /// If you want spherical growth, just set this to 3
        /// </remarks>
        public double Brain_NeuronGrowthExponent
        {
            get
            {
                return (double)_brain_NeuronGrowthExponent;
            }
            set
            {
                _brain_NeuronGrowthExponent = value;
            }
        }

        private volatile object _brain_NeuronDensity = 40d;
        /// <summary>
        /// This is how many neurons to place inside of a brain
        /// </summary>
        public double Brain_NeuronDensity
        {
            get
            {
                return (double)_brain_NeuronDensity;
            }
            set
            {
                _brain_NeuronDensity = value;
            }
        }

        private volatile object _brain_ChemicalDensity = 3d;
        /// <summary>
        /// This is how many brain chemical neurons to place inside of a brain
        /// NOTE: It should probably be named Brain_BrainChemicalDensity, but that's tedious
        /// </summary>
        public double Brain_ChemicalDensity
        {
            get
            {
                return (double)_brain_ChemicalDensity;
            }
            set
            {
                _brain_ChemicalDensity = value;
            }
        }

        private volatile object _brain_NeuronMinClusterDistPercent = .075d;
        /// <summary>
        /// Neurons won't be allowed to get closer together than this value
        /// (This is a percent of diameter)
        /// </summary>
        public double Brain_NeuronMinClusterDistPercent
        {
            get
            {
                return (double)_brain_NeuronMinClusterDistPercent;
            }
            set
            {
                _brain_NeuronMinClusterDistPercent = value;
            }
        }

        private volatile object _brain_LinksPerNeuron_Internal = 3d;
        public double Brain_LinksPerNeuron_Internal
        {
            get
            {
                return (double)_brain_LinksPerNeuron_Internal;
            }
            set
            {
                _brain_LinksPerNeuron_Internal = value;
            }
        }

        private volatile object _brain_LinksPerNeuron_External_FromSensor = 1.2d;
        /// <summary>
        /// This uses the one with the fewer neurons (which is likely to be the sensor)
        /// </summary>
        public double Brain_LinksPerNeuron_External_FromSensor
        {
            get
            {
                return (double)_brain_LinksPerNeuron_External_FromSensor;
            }
            set
            {
                _brain_LinksPerNeuron_External_FromSensor = value;
            }
        }

        private volatile object _brain_LinksPerNeuron_External_FromBrain = .1d;
        /// <summary>
        /// This uses the average number of neurons between the two brains
        /// </summary>
        public double Brain_LinksPerNeuron_External_FromBrain
        {
            get
            {
                return (double)_brain_LinksPerNeuron_External_FromBrain;
            }
            set
            {
                _brain_LinksPerNeuron_External_FromBrain = value;
            }
        }

        private volatile object _brain_LinksPerNeuron_External_FromManipulator = 1.5d;
        /// <summary>
        /// This uses the one with the fewer neurons (which is likely to be the manipulator)
        /// NOTE: This only considers the number of readable neurons exposed from the manipulator (which could be zero, the manipulator
        /// could be writeonly)
        /// </summary>
        public double Brain_LinksPerNeuron_External_FromManipulator
        {
            get
            {
                return (double)_brain_LinksPerNeuron_External_FromManipulator;
            }
            set
            {
                _brain_LinksPerNeuron_External_FromManipulator = value;
            }
        }

        private volatile object _neuralLink_MaxWeight = 2d;
        public double NeuralLink_MaxWeight
        {
            get
            {
                return (double)_neuralLink_MaxWeight;
            }
            set
            {
                _neuralLink_MaxWeight = value;
            }
        }

        private volatile int _shortTermMemory_Size = 100;
        public int ShortTermMemory_Size
        {
            get
            {
                return _shortTermMemory_Size;
            }
            set
            {
                _shortTermMemory_Size = value;
            }
        }

        private volatile object _shortTermMemory_MillisecondsBetween = 150d;
        public double ShortTermMemory_MillisecondsBetween
        {
            get
            {
                return (double)_shortTermMemory_MillisecondsBetween;
            }
            set
            {
                _shortTermMemory_MillisecondsBetween = value;
            }
        }

        public readonly DamageProps Brain_Damage = new DamageProps(
            HITPOINTMIN,
            HITPOINTSLOPE,
            DAMAGE_VELOCITYTHRESHOLD,
            DAMAGE_ENERGYTHESHOLD,
            DAMAGE_ENERGYTOHITPOINTMULT,
            DAMAGE_RANDOMMAX);

        #endregion
        #region brain neat

        private volatile object _brainNEAT_NeuronDensity_Input = 85d;
        /// <summary>
        /// This is how many neurons to place on the shell of a brain
        /// </summary>
        public double BrainNEAT_NeuronDensity_Input
        {
            get
            {
                return (double)_brainNEAT_NeuronDensity_Input;
            }
            set
            {
                _brainNEAT_NeuronDensity_Input = value;
            }
        }

        private volatile object _brainNEAT_NeuronDensity_Output = 25d;
        /// <summary>
        /// This is how many neurons to place on the shell of a brain
        /// </summary>
        public double BrainNEAT_NeuronDensity_Output
        {
            get
            {
                return (double)_brainNEAT_NeuronDensity_Output;
            }
            set
            {
                _brainNEAT_NeuronDensity_Output = value;
            }
        }

        #endregion
        #region direction controller

        private volatile object _directionController_Density = 900d;
        public double DirectionController_Density
        {
            get
            {
                return (double)_directionController_Density;
            }
            set
            {
                _directionController_Density = value;
            }
        }

        private volatile object _directionController_Sphere_NeuronGrowthExponent = .8d;
        /// <summary>
        /// If densitiy is multiplied by volume of a sphere, then the number of neurons explodes for radius greater than 1, and
        /// shrinks quickly for smaller values.
        /// 
        /// So instead the volume is calculated as Math.Pow(radius, this.NeuronGrowthExponent)
        /// </summary>
        /// <remarks>
        /// If you want spherical growth, just set this to 3
        /// </remarks>
        public double DirectionController_Sphere_NeuronGrowthExponent
        {
            get
            {
                return (double)_directionController_Sphere_NeuronGrowthExponent;
            }
            set
            {
                _directionController_Sphere_NeuronGrowthExponent = value;
            }
        }

        private volatile object _directionController_Sphere_NeuronDensity_Half = 30d;
        /// <summary>
        /// This is how many neurons to place on a sphere shell
        /// </summary>
        /// <remarks>
        /// There is one shell for linear, one for rotation
        /// </remarks>
        public double DirectionController_Sphere_NeuronDensity_Half
        {
            get
            {
                return (double)_directionController_Sphere_NeuronDensity_Half;
            }
            set
            {
                _directionController_Sphere_NeuronDensity_Half = value;
            }
        }

        private volatile object _directionController_Ring_NeuronGrowthExponent = .8d;
        /// <summary>
        /// If densitiy is multiplied by volume of a sphere, then the number of neurons explodes for radius greater than 1, and
        /// shrinks quickly for smaller values.
        /// 
        /// So instead the volume is calculated as Math.Pow(radius, this.NeuronGrowthExponent)
        /// </summary>
        /// <remarks>
        /// If you want spherical growth, just set this to 3
        /// </remarks>
        public double DirectionController_Ring_NeuronGrowthExponent
        {
            get
            {
                return (double)_directionController_Ring_NeuronGrowthExponent;
            }
            set
            {
                _directionController_Ring_NeuronGrowthExponent = value;
            }
        }

        private volatile object _directionController_Ring_NeuronDensity_Half = 30d;
        /// <summary>
        /// This is how many neurons to place on a sphere shell
        /// </summary>
        /// <remarks>
        /// There is one shell for linear, one for rotation
        /// </remarks>
        public double DirectionController_Ring_NeuronDensity_Half
        {
            get
            {
                return (double)_directionController_Ring_NeuronDensity_Half;
            }
            set
            {
                _directionController_Ring_NeuronDensity_Half = value;
            }
        }

        public readonly DamageProps DirectionController_Damage = new DamageProps(
            HITPOINTMIN,
            HITPOINTSLOPE,
            DAMAGE_VELOCITYTHRESHOLD,
            DAMAGE_ENERGYTHESHOLD,
            DAMAGE_ENERGYTOHITPOINTMULT,
            DAMAGE_RANDOMMAX);

        #endregion

        // Propulsion
        #region thruster (fuel -> force)

        private volatile object _thruster_Density = 600d;
        public double Thruster_Density
        {
            get
            {
                return (double)_thruster_Density;
            }
            set
            {
                _thruster_Density = value;
            }
        }

        private volatile object _thruster_AdditionalMassPercent = .1d;
        /// <summary>
        /// A ThrusterType of One will just have a base mass.  But a two way thrust will have base + (base * %).
        /// A three way will have base + 2(base * %), etc
        /// </summary>
        public double Thruster_AdditionalMassPercent
        {
            get
            {
                return (double)_thruster_AdditionalMassPercent;
            }
            set
            {
                _thruster_AdditionalMassPercent = value;
            }
        }

        public const double THRUSTER_FORCESTRENGTHMULT = 1000d;
        private volatile object _thruster_StrengthRatio = 30d; //30000d;		// thruster takes times FORCESTRENGTHMULT
        /// <summary>
        /// This is the amount of force that the thruster is able to generate (this is taken times the volume of the
        /// thruster cylinder)
        /// </summary>
        /// <remarks>
        /// NOTE: This value is taken times 1000 by the thruster.  This is so that the number exposed to the user isn't so large
        /// </remarks>
        public double Thruster_StrengthRatio
        {
            get
            {
                return (double)_thruster_StrengthRatio;
            }
            set
            {
                _thruster_StrengthRatio = value;
            }
        }

        public const double THRUSTER_FUELTOTHRUSTMULT = .000001d;
        private volatile object _thruster_FuelToThrustRatio = 1d; //.000001;		// thruster takes this times FUELTOTHRUSTMULT
        public double Thruster_FuelToThrustRatio
        {
            get
            {
                return (double)_thruster_FuelToThrustRatio;
            }
            set
            {
                _thruster_FuelToThrustRatio = value;
            }
        }

        private volatile object _thruster_LinksPerNeuron_Sensor = .5d;
        /// <summary>
        /// This uses the destination's number of neurons (always one neuron per thrust line)
        /// </summary>
        public double Thruster_LinksPerNeuron_Sensor
        {
            get
            {
                return (double)_thruster_LinksPerNeuron_Sensor;
            }
            set
            {
                _thruster_LinksPerNeuron_Sensor = value;
            }
        }

        private volatile object _thruster_LinksPerNeuron_Brain = 3d;
        /// <summary>
        /// This uses the destination's number of neurons (always one neuron per thrust line)
        /// </summary>
        public double Thruster_LinksPerNeuron_Brain
        {
            get
            {
                return (double)_thruster_LinksPerNeuron_Brain;
            }
            set
            {
                _thruster_LinksPerNeuron_Brain = value;
            }
        }

        public readonly DamageProps Thruster_Damage = new DamageProps(
            HITPOINTMIN,
            HITPOINTSLOPE,
            DAMAGE_VELOCITYTHRESHOLD,
            DAMAGE_ENERGYTHESHOLD,
            DAMAGE_ENERGYTOHITPOINTMULT,
            DAMAGE_RANDOMMAX);

        #endregion
        #region tractorbeam (plasma -> force)

        private volatile object _tractorBeam_Density = 1800d;
        public double TractorBeam_Density
        {
            get
            {
                return (double)_tractorBeam_Density;
            }
            set
            {
                _tractorBeam_Density = value;
            }
        }

        public readonly DamageProps TractorBeam_Damage = new DamageProps(
            HITPOINTMIN,
            HITPOINTSLOPE,
            DAMAGE_VELOCITYTHRESHOLD,
            DAMAGE_ENERGYTHESHOLD,
            DAMAGE_ENERGYTOHITPOINTMULT,
            DAMAGE_RANDOMMAX);

        #endregion
        #region impulseengine (plasma -> force)

        private volatile object _impulseEngine_Density = 1800d;
        public double ImpulseEngine_Density
        {
            get
            {
                return (double)_impulseEngine_Density;
            }
            set
            {
                _impulseEngine_Density = value;
            }
        }

        public const double IMPULSEENGINE_FORCESTRENGTHMULT = 1000d;

        private volatile object _impuseEngine_LinearStrengthRatio = 100d; //30000d;		// impulse takes times FORCESTRENGTHMULT
        /// <summary>
        /// This is the amount of force that the impulse engine is able to generate (this is taken times the volume)
        /// </summary>
        /// <remarks>
        /// NOTE: This value is taken times 1000 by the impulse engine.  This is so that the number exposed to the user isn't so large
        /// </remarks>
        public double Impulse_LinearStrengthRatio
        {
            get
            {
                return (double)_impuseEngine_LinearStrengthRatio;
            }
            set
            {
                _impuseEngine_LinearStrengthRatio = value;
            }
        }

        private volatile object _impuseEngine_RotationStrengthRatio = 35d; //30000d;		// impulse takes times FORCESTRENGTHMULT
        public double Impulse_RotationStrengthRatio
        {
            get
            {
                return (double)_impuseEngine_RotationStrengthRatio;
            }
            set
            {
                _impuseEngine_RotationStrengthRatio = value;
            }
        }

        public const double IMPULSEENGINE_PLASMATOTHRUSTMULT = .00000001d;
        private volatile object _impulseEngine_PlasmaToThrustRatio = 2d;        // impulse takes this times PLASMATOTHRUSTMULT
        public double ImpulseEngine_PlasmaToThrustRatio
        {
            get
            {
                return (double)_impulseEngine_PlasmaToThrustRatio;
            }
            set
            {
                _impulseEngine_PlasmaToThrustRatio = value;
            }
        }

        private volatile object _impulseEngine_NeuronGrowthExponent = .8d;
        /// <summary>
        /// If densitiy is multiplied by volume of a sphere, then the number of neurons explodes for radius greater than 1, and
        /// shrinks quickly for smaller values.
        /// 
        /// So instead the volume is calculated as Math.Pow(radius, this.NeuronGrowthExponent)
        /// </summary>
        /// <remarks>
        /// If you want spherical growth, just set this to 3
        /// </remarks>
        public double ImpulseEngine_NeuronGrowthExponent
        {
            get
            {
                return (double)_impulseEngine_NeuronGrowthExponent;
            }
            set
            {
                _impulseEngine_NeuronGrowthExponent = value;
            }
        }

        private volatile object _impulseEngine_NeuronDensity_Half = 30d;
        /// <summary>
        /// This is how many neurons to place on a sphere shell
        /// </summary>
        /// <remarks>
        /// There is one shell for linear, one for rotation
        /// </remarks>
        public double ImpulseEngine_NeuronDensity_Half
        {
            get
            {
                return (double)_impulseEngine_NeuronDensity_Half;
            }
            set
            {
                _impulseEngine_NeuronDensity_Half = value;
            }
        }

        public readonly DamageProps ImpulseEngine_Damage = new DamageProps(
            HITPOINTMIN,
            HITPOINTSLOPE,
            DAMAGE_VELOCITYTHRESHOLD,
            DAMAGE_ENERGYTHESHOLD,
            DAMAGE_ENERGYTOHITPOINTMULT,
            DAMAGE_RANDOMMAX);

        #endregion

        // Guns
        #region projectile weapon

        private volatile object _projectileWeapon_Density = 1700d;
        public double ProjectileWeapon_Density
        {
            get
            {
                return (double)_projectileWeapon_Density;
            }
            set
            {
                _projectileWeapon_Density = value;
            }
        }

        private volatile object _projectileWeapon_CaliberRatio = 1d;
        /// <summary>
        /// The caliber is based on the Scale * BarrelRadius * ProjectileWeaponCaliberRatio
        /// </summary>
        /// <remarks>
        /// Caliber is the diameter of the bullet
        /// </remarks>
        public double ProjectileWeapon_CaliberRatio
        {
            get
            {
                return (double)_projectileWeapon_CaliberRatio;
            }
            set
            {
                _projectileWeapon_CaliberRatio = value;
            }
        }

        private volatile object _projectileWeapon_CaliberRangePercent = .1d;
        /// <summary>
        /// This defines the range of calibers that a gun could fire (set to zero for an exact only)
        /// </summary>
        public double ProjectileWeapon_CaliberRangePercent
        {
            get
            {
                return (double)_projectileWeapon_CaliberRangePercent;
            }
            set
            {
                _projectileWeapon_CaliberRangePercent = value;
            }
        }

        private volatile object _projectile_Color = null;
        public Color? Projectile_Color
        {
            get
            {
                return (Color?)_projectile_Color;
            }
            set
            {
                _projectile_Color = value;
            }
        }

        private volatile object _projectile_RadiusRatio = 2.5d;
        public double Projectile_RadiusRatio
        {
            get
            {
                return (double)_projectile_RadiusRatio;
            }
            set
            {
                _projectile_RadiusRatio = value;
            }
        }

        public readonly DamageProps ProjectileWeapon_Damage = new DamageProps(
            HITPOINTMIN,
            HITPOINTSLOPE,
            DAMAGE_VELOCITYTHRESHOLD,
            DAMAGE_ENERGYTHESHOLD,
            DAMAGE_ENERGYTOHITPOINTMULT,
            DAMAGE_RANDOMMAX);

        #endregion
        #region beam gun

        private volatile object _beamGun_Density = 1800d;
        public double BeamGun_Density
        {
            get
            {
                return (double)_beamGun_Density;
            }
            set
            {
                _beamGun_Density = value;
            }
        }

        public readonly DamageProps BeamGun_Damage = new DamageProps(
            HITPOINTMIN,
            HITPOINTSLOPE,
            DAMAGE_VELOCITYTHRESHOLD,
            DAMAGE_ENERGYTHESHOLD,
            DAMAGE_ENERGYTOHITPOINTMULT,
            DAMAGE_RANDOMMAX);

        #endregion

        // Shields
        #region shields - all

        private volatile object _shield_Density = 1800d;
        public double Shield_Density
        {
            get
            {
                return (double)_shield_Density;
            }
            set
            {
                _shield_Density = value;
            }
        }

        public readonly DamageProps Shield_Damage = new DamageProps(
            HITPOINTMIN,
            HITPOINTSLOPE,
            DAMAGE_VELOCITYTHRESHOLD,
            DAMAGE_ENERGYTHESHOLD,
            DAMAGE_ENERGYTOHITPOINTMULT,
            DAMAGE_RANDOMMAX);

        #endregion

        // Misc
        #region swarm bay

        private volatile object _swarmBay_Density = 3000d;
        public double SwarmBay_Density
        {
            get
            {
                return (double)_swarmBay_Density;
            }
            set
            {
                _swarmBay_Density = value;
            }
        }

        public double SwarmBay_BirthRate = 3;
        public double SwarmBay_BirthCost = .015;
        public double SwarmBay_BirthSize = 1;
        public double SwarmBay_Birth_TankThresholdPercent = .65;
        public double SwarmBay_MaxCount = 9;

        public readonly DamageProps SwarmBay_Damage = new DamageProps(
            HITPOINTMIN,
            HITPOINTSLOPE,
            DAMAGE_VELOCITYTHRESHOLD,
            DAMAGE_ENERGYTHESHOLD,
            DAMAGE_ENERGYTOHITPOINTMULT,
            DAMAGE_RANDOMMAX);

        #endregion
        #region swarm bot

        public double SwarmBot_AmountToDraw;
        public double SwarmBot_SearchRadius = 50;
        public double SwarmBot_ChaseNeighborCount = 5;

        public double SwarmBot_MaxAccel = 1;
        public double SwarmBot_MaxAngAccel = 1;
        public double SwarmBot_MinSpeed = 7;
        public double SwarmBot_MaxSpeed = 13;
        public double SwarmBot_MaxAngSpeed = 10;

        public double SwarmBot_HealRate = 1;
        public double SwarmBot_DamageAtMaxSpeed = 33;
        public double SwarmBot_MaxHealth = 100;

        #endregion
    }
}
