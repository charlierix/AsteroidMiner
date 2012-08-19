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
	/// </remarks>
	public class ItemOptions
	{
		#region Mineral Densities

		//	These are copied here for convenience (all in kg/m^3)
		//	Ice: 934
		//	Iron: 7900
		//	Graphite: 2267
		//	Gold: 19300
		//	Platinum: 21450
		//	Emerald: 2760
		//	Saphire: 4000
		//	Ruby: 4000
		//	Diamond: 3515
		//	Rixium: 66666

		#endregion

		//	Ammo Box
		private double _ammoDensity = 10000d;
		/// <summary>
		/// This is the mass of ammo that is a size of 1
		/// </summary>
		public double AmmoDensity
		{
			get
			{
				return _ammoDensity;
			}
			set
			{
				_ammoDensity = value;
			}
		}

		private double _ammoBoxWallDensity = 20d;
		/// <summary>
		/// This is the mass of the ammo box's shell (it should be fairly light)
		/// </summary>
		public double AmmoBoxWallDensity
		{
			get
			{
				return _ammoBoxWallDensity;
			}
			set
			{
				_ammoBoxWallDensity = value;
			}
		}

		//	Fuel Tank
		private double _fuelDensity = 750d;
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
				return _fuelDensity;
			}
			set
			{
				_fuelDensity = value;
			}
		}

		private double _fuelTankWallDensity = 10d;
		/// <summary>
		/// This is the mass of the fuel tank's shell (it should be fairly light)
		/// </summary>
		public double FuelTankWallDensity
		{
			get
			{
				return _fuelTankWallDensity;
			}
			set
			{
				_fuelTankWallDensity = value;
			}
		}

		//	Energy Tank
		private double _energyTankDensity = 1000d;
		/// <summary>
		/// The energy tank has the same mass whether it's empty or full
		/// </summary>
		public double EnergyTankDensity
		{
			get
			{
				return _energyTankDensity;
			}
			set
			{
				_energyTankDensity = value;
			}
		}

		//	Energy -> Ammo
		private double _energyToAmmoConversionRate = .02d;		//	50 units of energy for one unit of ammo
		public double EnergyToAmmoConversionRate
		{
			get
			{
				return _energyToAmmoConversionRate;
			}
			set
			{
				_energyToAmmoConversionRate = value;
			}
		}

		private double _energyToAmmoAmountToDraw = 1d;
		/// <summary>
		/// How much energy to pull in one unit of time (this gets multiplied by the size of the converter)
		/// </summary>
		public double EnergyToAmmoAmountToDraw
		{
			get
			{
				return _energyToAmmoAmountToDraw;
			}
			set
			{
				_energyToAmmoAmountToDraw = value;
			}
		}

		private double _energyToAmmoDensity = 7500d;
		public double EnergyToAmmoDensity
		{
			get
			{
				return _energyToAmmoDensity;
			}
			set
			{
				_energyToAmmoDensity = value;
			}
		}

		//	Energy -> Fuel
		private double _energyToFuelConversionRate = .1d;		//	10 units of energy for one unit of fuel
		public double EnergyToFuelConversionRate
		{
			get
			{
				return _energyToFuelConversionRate;
			}
			set
			{
				_energyToFuelConversionRate = value;
			}
		}

		private double _energyToFuelAmountToDraw = 1d;
		/// <summary>
		/// How much energy to pull in one unit of time (this gets multiplied by the size of the converter)
		/// </summary>
		public double EnergyToFuelAmountToDraw
		{
			get
			{
				return _energyToFuelAmountToDraw;
			}
			set
			{
				_energyToFuelAmountToDraw = value;
			}
		}

		private double _energyToFuelDensity = 7500d;
		public double EnergyToFuelDensity
		{
			get
			{
				return _energyToFuelDensity;
			}
			set
			{
				_energyToFuelDensity = value;
			}
		}

		//	Fuel -> Energy
		private double _fuelToEnergyConversionRate = .9d;		//	10 units of fuel for 9 units of energy (it's just burning fuel)
		public double FuelToEnergyConversionRate
		{
			get
			{
				return _fuelToEnergyConversionRate;
			}
			set
			{
				_fuelToEnergyConversionRate = value;
			}
		}

		private double _fuelToEnergyAmountToDraw = 1d;
		/// <summary>
		/// How much fuel to pull in one unit of time (this gets multiplied by the size of the converter)
		/// </summary>
		public double FuelToEnergyAmountToDraw
		{
			get
			{
				return _fuelToEnergyAmountToDraw;
			}
			set
			{
				_fuelToEnergyAmountToDraw = value;
			}
		}

		private double _fuelToEnergyDensity = 500d;
		public double FuelToEnergyDensity
		{
			get
			{
				return _fuelToEnergyDensity;
			}
			set
			{
				_fuelToEnergyDensity = value;
			}
		}

		//	Solar Panel (Radiation -> Energy)
		private double _solarPanelConversionRate = .05d;
		public double SolarPanelConversionRate
		{
			get
			{
				return _solarPanelConversionRate;
			}
			set
			{
				_solarPanelConversionRate = value;
			}
		}

		private double _solarPanelDensity = 500d;
		public double SolarPanelDensity
		{
			get
			{
				return _solarPanelDensity;
			}
			set
			{
				_solarPanelDensity = value;
			}
		}

		//	Thruster (Fuel -> Force)
		private double _thrusterDensity = 600d;
		public double ThrusterDensity
		{
			get
			{
				return _thrusterDensity;
			}
			set
			{
				_thrusterDensity = value;
			}
		}

		private double _thrusterAdditionalMassPercent = .1d;
		/// <summary>
		/// A ThrusterType of One will just have a base mass.  But a two way thrust will have base + (base * %).
		/// A three way will have base + 2(base * %), etc
		/// </summary>
		public double ThrusterAdditionalMassPercent
		{
			get
			{
				return _thrusterAdditionalMassPercent;
			}
			set
			{
				_thrusterAdditionalMassPercent = value;
			}
		}

		private double _thrusterStrengthRatio = 1d;
		/// <summary>
		/// This is the amount of force that the thruster is able to generate (this is taken times the volume of the
		/// thruster cylinder)
		/// </summary>
		public double ThrusterStrengthRatio
		{
			get
			{
				return _thrusterStrengthRatio;
			}
			set
			{
				_thrusterStrengthRatio = value;
			}
		}

		private double _fuelToThrustRatio = .001d;
		public double FuelToThrustRatio
		{
			get
			{
				return _fuelToThrustRatio;
			}
			set
			{
				_fuelToThrustRatio = value;
			}
		}
	}
}
