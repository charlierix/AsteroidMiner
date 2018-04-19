using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Game.HelperClassesWPF;
using Game.Newt.v2.GameItems;

namespace Game.Newt.v2.Arcanorum
{
    //NOTE: Game.Newt.v2.AsteroidMiner.AstMin2D made a copy of this class.  Later, the orientation logic was fixed over there, and later still, that orientation logic was copied back over here
    //The classes are different enough that they need to stay separate.  Maybe when a third one is needed, figure out how much can go into a base class
    public class KeepItems2D : IDisposable
    {
        #region class: TrackedItem

        private class TrackedItem
        {
            public TrackedItem(IMapObject mapObject, MapObject_ChasePoint_Forces translate, MapObject_ChaseOrientation_Torques rotate)
            {
                MapObject = mapObject;
                Translate = translate;
                Rotate = rotate;
            }

            public readonly IMapObject MapObject;
            public readonly MapObject_ChasePoint_Forces Translate;
            public readonly MapObject_ChaseOrientation_Torques Rotate;
        }

        #endregion

        #region Declaration Section

        //private List<(IMapObject mapObject, MapObject_ChasePoint_Forces force, LockOrientation orientation)> _items = new List<(IMapObject, MapObject_ChasePoint_Forces, LockOrientation)>();
        private List<TrackedItem> _items = new List<TrackedItem>();

        #endregion

        #region Constructor

        public KeepItems2D()
        {
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var item in _items)
                {
                    //NOTE: Only disposing the chase class, because that is being managed by this class.  The body it's chasing is not managed by this class, so should be disposed elsewhere
                    if (item.Translate != null)
                    {
                        item.Translate.Dispose();
                    }

                    if (item.Rotate != null)
                    {
                        item.Rotate.Dispose();
                    }
                }

                _items.Clear();
            }
        }

        #endregion

        #region Public Properties

        public DragHitShape SnapShape
        {
            get;
            set;
        }

        #endregion

        #region Public Methods

        public void Add(IMapObject item, bool shouldLockOrientation, Vector3D? orientationRotateAxis = null, Vector3D? orientationModelUp = null)
        {
            if (_items.Any(o => o.MapObject.Equals(item)))
            {
                // It's already added
                return;
            }

            #region forces

            List<ChasePoint_Force> forces = new List<ChasePoint_Force>();

            // Attraction Force
            GradientEntry[] gradient = new[]
            {
                new GradientEntry(0d, .04d),     // distance, %
                new GradientEntry(1d, 1d),
            };
            forces.Add(new ChasePoint_Force(ChaseDirectionType.Attract_Direction, 500d, gradient: gradient));

            // These act like a shock absorber
            forces.Add(new ChasePoint_Force(ChaseDirectionType.Drag_Velocity_AlongIfVelocityAway, 50d));

            gradient = new[]
            {
                new GradientEntry(0d, 1d),
                new GradientEntry(.75d, .2d),
                new GradientEntry(2d, 0d),
            };
            forces.Add(new ChasePoint_Force(ChaseDirectionType.Drag_Velocity_AlongIfVelocityToward, 100d, gradient: gradient));

            MapObject_ChasePoint_Forces chaseForces = new MapObject_ChasePoint_Forces(item, false)
            {
                Forces = forces.ToArray()
            };

            #endregion
            #region torques

            MapObject_ChaseOrientation_Torques chaseTorques = null;

            List<ChaseOrientation_Torque> torques = new List<ChaseOrientation_Torque>();

            double mult = 6;

            // Attraction
            gradient = new[]
            {
                new GradientEntry(0d, 0d),     // distance, %
                new GradientEntry(10d, 1d),
            };
            torques.Add(new ChaseOrientation_Torque(ChaseDirectionType.Attract_Direction, .4 * mult, gradient: gradient));

            // Drag
            gradient = new[]        // this gradient is needed, because there needs to be no drag along the desired axis (otherwise, this drag will fight with the user's desire to rotate the ship)
            {
                new GradientEntry(0d, 0d),     // distance, %
                new GradientEntry(5d, 1d),
            };
            torques.Add(new ChaseOrientation_Torque(ChaseDirectionType.Drag_Velocity_Orth, .0739 * mult, gradient: gradient));

            torques.Add(new ChaseOrientation_Torque(ChaseDirectionType.Drag_Velocity_AlongIfVelocityAway, .0408 * mult));

            chaseTorques = new MapObject_ChaseOrientation_Torques(item)
            {
                Torques = torques.ToArray()
            };

            #endregion

            _items.Add(new TrackedItem(item, chaseForces, chaseTorques));
        }
        public void Remove(IMapObject item)
        {
            for (int cntr = 0; cntr < _items.Count; cntr++)
            {
                if (_items[cntr].MapObject.Equals(item))
                {
                    if (_items[cntr].Translate != null)
                    {
                        _items[cntr].Translate.Dispose();
                    }

                    if (_items[cntr].Rotate != null)
                    {
                        _items[cntr].Rotate.Dispose();
                    }

                    _items.RemoveAt(cntr);

                    return;
                }
            }
        }

        public void Clear()
        {
            while (_items.Count > 0)
            {
                Remove(_items[0].MapObject);
            }
        }

        public void Update()
        {
            foreach (var item in _items)
            {
                Point3D position = item.MapObject.PositionWorld;

                // Get a ray
                Point3D? chasePoint = SnapShape.CastRay(position);

                // Chase that point
                if (chasePoint == null || Math3D.IsNearValue(position, chasePoint.Value))
                {
                    item.Translate.StopChasing();
                }
                else
                {
                    item.Translate.SetPosition(chasePoint.Value);
                }

                // Lock orientation
                if (item.Rotate != null)
                {
                    // In the future, the snap shape could be a large cylinder, so the normal needs to be calculated relative to where the item is.  It won't matter
                    // if the item is on the plane or above/below it, the normal will still be the same
                    Vector3D? normal = SnapShape.GetNormal(position);

                    if (normal != null)
                    {
                        item.Rotate.SetOrientation(normal.Value);
                    }
                }
            }
        }

        /// <summary>
        /// The items continue chasing the point from the last call to update.  So make all objects stop chasing, call this
        /// </summary>
        public void StopChasing()
        {
            foreach (var item in _items)
            {
                if (item.Translate != null)
                {
                    item.Translate.StopChasing();
                }

                if (item.Rotate != null)
                {
                    item.Rotate.StopChasing();
                }
            }
        }

        #endregion
    }
}
