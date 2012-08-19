using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Media3D;

using Game.Newt.AsteroidMiner2.ShipParts;

namespace Game.Newt.AsteroidMiner2.ShipEditor
{
	/// <summary>
	/// This is the wrapper to a part that is being dragged from the tab control to the design surface
	/// </summary>
	public class DraggingDropPart
	{
		public PartToolItemBase Part2D = null;
		public PartDesignBase Part3D = null;
		public ModelVisual3D Model = null;

		public bool HasAddedToViewport = false;

		public Point DragStart;
	}
}
