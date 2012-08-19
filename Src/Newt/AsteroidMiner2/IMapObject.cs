using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;

using Game.Newt.NewtonDynamics;

namespace Game.Newt.AsteroidMiner2
{
	public interface IMapObject
	{
		/// <summary>
		/// This one could be null.  This would allow objects to be added to and managed by the map, but not be
		/// physics objects
		/// </summary>
		Body PhysicsBody { get; }

		Visual3D[] Visuals3D { get; }

		Point3D PositionWorld { get; }
		Vector3D VelocityWorld { get; }

		/// <summary>
		/// This is the bounding sphere, or rough size of the object
		/// </summary>
		double Radius { get; }
	}
}
