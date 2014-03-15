using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Game.Newt.AsteroidMiner2;
using Game.Newt.HelperClasses;

namespace Game.Newt.Testers.Arcanorum
{
    public class KeepItems2D : IDisposable
    {
        #region Declaration Section

        private List<Tuple<IMapObject, MapObjectChaseForces>> _items = new List<Tuple<IMapObject, MapObjectChaseForces>>();

        // Can't use this, because it will prevent the weapon from swinging (it keeps directly setting the
        // the weapon's velocity)
        //private List<Tuple<IMapObject, MapObjectChaseVelocity>> _items = new List<Tuple<IMapObject, MapObjectChaseVelocity>>();

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
                    //NOTE: Only disposing the chase class, because that is being managed by this class.  The body its chasing is not managed by this class, so should be disposed elsewhere
                    item.Item2.Dispose();
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

        public void Add(IMapObject item)
        {
            if (_items.Any(o => o.Item1.Equals(item)))
            {
                // It's already added
                return;
            }

            //MapObjectChaseVelocity chase = new MapObjectChaseVelocity(item);
            //chase.MaxVelocity = 10d;
            //chase.Multiplier = 40d;

            MapObjectChaseForces chase = new MapObjectChaseForces(item);

            // Attraction Force
            chase.Forces.Add(new ChaseForcesGradient<ChaseForcesConstant>(new[]
                    {
                        new ChaseForcesGradientStop<ChaseForcesConstant>(new ChaseDistance(true, 0d), new ChaseForcesConstant(ChaseDirectionType.Direction) { BaseAcceleration = 20d, ApplyWhenUnderSpeed = 100d }),
                        new ChaseForcesGradientStop<ChaseForcesConstant>(new ChaseDistance(false, 1d), new ChaseForcesConstant(ChaseDirectionType.Direction) { BaseAcceleration = 500d, ApplyWhenUnderSpeed = 100d }),
                        new ChaseForcesGradientStop<ChaseForcesConstant>(new ChaseDistance(true, double.MaxValue), new ChaseForcesConstant(ChaseDirectionType.Direction) { BaseAcceleration = 500d, ApplyWhenUnderSpeed = 100d })
                    }));

            // These act like a shock absorber
            chase.Forces.Add(new ChaseForcesDrag(ChaseDirectionType.Velocity_AlongIfVelocityAway) { BaseAcceleration = 50d });

            chase.Forces.Add(new ChaseForcesGradient<ChaseForcesDrag>(new[]
                    {
                        new ChaseForcesGradientStop<ChaseForcesDrag>(new ChaseDistance(true, 0d), new ChaseForcesDrag(ChaseDirectionType.Velocity_AlongIfVelocityToward) { BaseAcceleration = 100d }),
                        new ChaseForcesGradientStop<ChaseForcesDrag>(new ChaseDistance(false, .75d), new ChaseForcesDrag(ChaseDirectionType.Velocity_AlongIfVelocityToward) { BaseAcceleration = 20d }),
                        new ChaseForcesGradientStop<ChaseForcesDrag>(new ChaseDistance(false, 2d), new ChaseForcesDrag(ChaseDirectionType.Velocity_AlongIfVelocityToward) { BaseAcceleration = 0d }),
                    }));

            _items.Add(Tuple.Create(item, chase));
        }
        public void Remove(IMapObject item)
        {
            for (int cntr = 0; cntr < _items.Count; cntr++)
            {
                if (_items[cntr].Item1.Equals(item))
                {
                    _items[cntr].Item2.Dispose();
                    _items.RemoveAt(cntr);

                    return;
                }
            }
        }

        public void Update()
        {
            foreach (var item in _items)
            {
                Point3D position = item.Item1.PositionWorld;

                // Get a ray
                Point3D? chasePoint = this.SnapShape.CastRay(position);

                // Chase that point
                if (chasePoint == null || Math3D.IsNearValue(position, chasePoint.Value))
                {
                    item.Item2.StopChasing();
                }
                else
                {
                    item.Item2.SetPosition(chasePoint.Value);
                }
            }
        }

        #endregion
    }
}
