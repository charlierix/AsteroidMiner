using System.Windows.Media.Media3D;

namespace Game.Newt.v2.Arcanorum
{
    /// <summary>
    /// This helps SensorVision see the shape of an object (instead of just having centerpoint)
    /// </summary>
    /// <remarks>
    /// This should be implemented by items that the vision sensor will see.  So objects that also implement IMapObject
    /// </remarks>
    public interface ISensorVisionPoints
    {
        /// <summary>
        /// This returns the sample points in world coords
        /// </summary>
        /// <remarks>
        /// The points need to be rotated and translated into world coods
        /// </remarks>
        Point3D[] GetSensorVisionPoints();
    }
}
