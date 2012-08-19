using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;

namespace Game.Newt.AsteroidMiner_153.ShipAddons
{
	public interface IShipAddon
	{
		/// <summary>
		/// This is how much mass this addon has when empty
		/// </summary>
		double DryMass { get; }

		/// <summary>
		/// This is how much mass this addon has total (DryMass + the mass of its contents)
		/// </summary>
		double TotalMass { get; }

		/// <summary>
		/// This is how this item looks (also a good way to prevent overlapping items)
		/// </summary>
		//ModelVisual3D VisualModel { get; }
	}
}
