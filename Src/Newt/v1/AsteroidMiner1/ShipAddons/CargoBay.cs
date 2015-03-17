using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Game.Newt.v1.AsteroidMiner1.ShipAddons
{
	public class CargoBay : IShipAddon
	{
		#region Declaration Section

		private const double MINERALVOLUME = 1d;

		#endregion

		#region Constructor

		public CargoBay(double dryMass, double maxVolume)
		{
			_dryMass = dryMass;
			_maxVolume = maxVolume;
		}

		#endregion

		#region IShipAddon Members

		private double _dryMass = 1d;
		public double DryMass
		{
			get
			{
				return _dryMass;
			}
		}

		public double TotalMass
		{
			get
			{
				double retVal = _dryMass;

				foreach (Mineral mineral in _contents)
				{
					retVal += mineral.Mass;
				}

				return retVal;
			}
		}

		#endregion

		#region Public Properties

		private double _maxVolume = 1d;
		public double MaxVolume
		{
			get
			{
				return _maxVolume;
			}
			set
			{
				_maxVolume = value;
			}
		}

		public double UsedVolume
		{
			get
			{
				// Mineral currently doesn't expose a volume property, so I'll just use 1 for each
				return _contents.Count * MINERALVOLUME;
			}
		}

		/// <summary>
		/// For now, this will only be able to add minerals.  In the future, it should be able to hold scrap parts
		/// </summary>
		private List<Mineral> _contents = new List<Mineral>();
		public ReadOnlyCollection<Mineral> Contents
		{
			get
			{
				return _contents.AsReadOnly();
			}
		}

		#endregion

		#region Public Methods

		public bool Add(Mineral mineral)
		{
			if (this.UsedVolume + MINERALVOLUME > this.MaxVolume)
			{
				return false;
			}

			_contents.Add(mineral);

			return true;
		}
		public void Remove(Mineral mineral)
		{
			_contents.Remove(mineral);
		}
		public void ClearContents()
		{
			_contents.Clear();
		}

		#endregion
	}
}
