using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Game.Newt.AsteroidMiner_153.ShipAddons
{
	public class FuelTank : Container, IShipAddon
	{
		#region IShipAddon Members

		private double _dryMass = 1d;
		public double DryMass
		{
			get
			{
				return _dryMass;
			}
			set
			{
				_dryMass = value;
			}
		}

		public double TotalMass
		{
			get
			{
				return _dryMass + (this.QuantityCurrent * _fuelDensity);
			}
		}

		#endregion

		#region Public Properties

		private double _fuelDensity = 1d;
		/// <summary>
		/// This is the mass of one unit of fuel
		/// </summary>
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

		#endregion
	}
}
