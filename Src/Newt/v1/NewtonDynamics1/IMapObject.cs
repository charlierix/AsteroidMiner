using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Media3D;

namespace Game.Newt.v1.NewtonDynamics1
{
    /// <summary>
    /// This gives basic info about an object.  Only objects that implement this interface can be in the Map class
    /// </summary>
    /// <remarks>
    /// NOTE:  This used to be in the AsteroidMiner project, but I'm pushing it down here, because I want to be able to reference
    /// the physics body from world, but store higher level objects in the body's UserData property
    /// 
    /// Newt calls these bodies, I used to call them radar blips
    /// </remarks>
    public interface IMapObject
    {
        //TODO:  Add a VisualsChanged property (maybe also a SizedChanged property?)

        /// <summary>
        /// This one could be null (space stations aren't managed by newtwon, but are still considered part of the map)
        /// </summary>
        ConvexBody3D PhysicsBody { get; }

        IEnumerable<ModelVisual3D> Visuals3D { get; }

        Point3D PositionWorld { get; }
        Vector3D VelocityWorld { get; }

        /// <summary>
        /// This is the bounding sphere, or rough size of the object
        /// </summary>
        double Radius { get; }
    }
}
