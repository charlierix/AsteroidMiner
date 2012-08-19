using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;

namespace Game.Newt.AsteroidMiner2.ShipEditor
{
	/// <summary>
	/// This holds various options (some user configurable, some hardcoded)
	/// </summary>
	public class EditorOptions
	{
		public EditorOptions()
		{
			this.EditorColors = new EditorColors();
			this.WorldColors = new WorldColors();
		}

		public EditorColors EditorColors
		{
			get;
			private set;
		}
		public WorldColors WorldColors
		{
			get;
			private set;
		}

		//private Quaternion _defaultOrientation = new Quaternion(new Vector3D(0d, 1d, 0d), -90d);		//	there's still a problem with group selection drag modifiers not rotating
		private Quaternion _defaultOrientation = Quaternion.Identity;
		/// <summary>
		/// All the parts have a height along Z, but the editor wants forward to be along -X instead of Z
		/// </summary>
		/// <remarks>
		/// NOTE: When doing a group select on several parts, changing the scale can look funny if they have a different orientation
		/// than the selection class.  This will only be a problem if a ship is imported that was built using a different default orientation.
		/// The simplest way to hide this is to not make this user configurable (but nothing can be done about a ship that's a product of
		/// evolution)
		/// </remarks>
		public Quaternion DefaultOrientation
		{
			get
			{
				return _defaultOrientation;
			}
		}
	}
}
