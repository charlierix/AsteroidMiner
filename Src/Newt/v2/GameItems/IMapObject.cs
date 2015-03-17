using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;

using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.GameItems
{
    public interface IMapObject : ICameraPoolVisual, IComparable<IMapObject>, IEquatable<IMapObject>        // see Map.CompareToIMaps, Map.EqualsIMaps
    {
        long Token { get; }

        /// <summary>
        /// This one could be null.  This would allow objects to be added to and managed by the map, but not be
        /// physics objects
        /// </summary>
        Body PhysicsBody { get; }

        Visual3D[] Visuals3D { get; }       //TODO: Get rid of this
        Model3D Model { get; }

        Point3D PositionWorld { get; }
        Vector3D VelocityWorld { get; }

        /// <summary>
        /// This is the bounding sphere, or rough size of the object
        /// </summary>
        double Radius { get; }

        /// <summary>
        /// This can be helpful if there are too many objects, and old ones need to be cleared
        /// </summary>
        /// <remarks>
        /// The physics world could be running at a different rate than real time, so this should only be used when
        /// calculating the age from the human's perspective
        /// </remarks>
        DateTime CreationTime { get; }
    }

    #region Class: MapObjectUtil

    public static class MapObjectUtil
    {
        // These are helper methods for objects that implement IMapObject
        public static int CompareToT(IMapObject thisObject, IMapObject other)
        {
#if DEBUG
            if (thisObject == null)
            {
                throw new ArgumentException("thisObject should never be null");
            }
#endif

            if (other == null)
            {
                // I'm greater than null
                return 1;
            }

            if (thisObject.Token < other.Token)
            {
                return -1;
            }
            else if (thisObject.Token > other.Token)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }
        public static bool EqualsT(IMapObject thisObject, IMapObject other)
        {
#if DEBUG
            if (thisObject == null)
            {
                throw new ArgumentException("thisObject should never be null");
            }
#endif

            if (other == null)
            {
                // I'm greater than null
                return false;
            }
            else
            {
                return thisObject.Token == other.Token;
            }
        }
        public static bool EqualsObj(IMapObject thisObject, object obj)
        {
#if DEBUG
            if (thisObject == null)
            {
                throw new ArgumentException("thisObject should never be null");
            }
#endif

            IMapObject other = obj as IMapObject;
            if (other == null)
            {
                return false;
            }
            else
            {
                return thisObject.Token == other.Token;
            }
        }
        public static int GetHashCode(IMapObject thisObject)
        {
#if DEBUG
            if (thisObject == null)
            {
                throw new ArgumentException("thisObject should never be null");
            }
#endif

            //http://blogs.msdn.com/b/ericlippert/archive/2011/02/28/guidelines-and-rules-for-gethashcode.aspx
            //http://stackoverflow.com/questions/13837774/gethashcode-and-buckets
            //http://stackoverflow.com/questions/858904/can-i-convert-long-to-int

            //return (int)thisObject.Token;
            //return unchecked((int)thisObject.Token);
            return thisObject.Token.GetHashCode();      //after much reading about GetHashCode, this most obvious solution should be the best :)
        }
    }

    #endregion
}
