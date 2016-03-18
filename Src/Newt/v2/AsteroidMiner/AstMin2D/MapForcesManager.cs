using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.AsteroidMiner.MapParts;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.MapParts;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.AsteroidMiner.AstMin2D
{
    /// <summary>
    /// This applies forces to item (inward force if they are near the edge, etc)
    /// </summary>
    /// <remarks>
    /// TODO: Gravity
    /// TODO: Jet Streams
    /// </remarks>
    public class MapForcesManager : IDisposable, IPartUpdatable
    {
        #region Declaration Section

        private readonly Point3D _boundryMin;
        private readonly Point3D _boundryMax;

        // These are just cached values of simple calculations (all based on _boundryMax.X)
        private readonly double _boundryForceBegin;
        private readonly double _boundryForceBeginSquared;
        private readonly double _boundryForceEnd;
        private readonly double _boundryWidth;

        private readonly Map _map;
        private readonly KeepItems2D _keep2D;

        #endregion

        #region Constructor

        public MapForcesManager(Map map, Point3D boundryMin, Point3D boundryMax)
        {
            _map = map;
            _boundryMin = boundryMin;
            _boundryMax = boundryMax;

            // This will keep objects on the XY plane using forces (not velocities)
            _keep2D = new KeepItems2D();

            _map.ItemAdded += new EventHandler<MapItemArgs>(Map_ItemAdded);
            _map.ItemRemoved += new EventHandler<MapItemArgs>(Map_ItemRemoved);

            // Just using one property from boundry (assumes the boundry is square, and max is positive)
            _boundryForceEnd = _boundryMax.X;
            _boundryForceBegin = _boundryForceEnd * .85d;
            _boundryForceBeginSquared = _boundryForceBegin * _boundryForceBegin;
            _boundryWidth = _boundryForceEnd - _boundryForceBegin;
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _keep2D.Dispose();
            }
        }

        #endregion
        #region IPartUpdatable Members

        public void Update_MainThread(double elapsedTime)
        {
            _keep2D.Update(elapsedTime);
        }
        public void Update_AnyThread(double elapsedTime)
        {
        }

        public int? IntervalSkips_MainThread
        {
            get
            {
                return 0;
            }
        }
        public int? IntervalSkips_AnyThread
        {
            get
            {
                return null;
            }
        }

        #endregion

        #region Event Listeners

        private void Map_ItemAdded(object sender, MapItemArgs e)
        {
            if (e.Item is SpaceStation)
            {
                // Space stations are always stationary
                return;
            }

            e.Item.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(PhysicsBody_ApplyForceAndTorque);

            #region Keep 2D

            //NOTE: Not including projectiles, because guns could be mounted above and below the plane, and if projectiles are smashed onto
            //the plane, they sometimes collide with each other or the ship

            if (e.Item is Asteroid || e.Item is Mineral || e.Item is Ship || e.Item is Bot)
            {
                bool limitRotation = e.Item is Ship || e.Item is Bot;

                // Doing this in case the new item is the child of an exploding asteroid.  The parent asteroid can shatter in 3D, then ease
                // back onto the plane
                double animateDuration = 60;
                double animatePreDelay = 2;

                _keep2D.Add(e.Item, limitRotation, animateDuration, animatePreDelay);
            }

            #endregion
        }
        private void Map_ItemRemoved(object sender, MapItemArgs e)
        {
            e.Item.PhysicsBody.ApplyForceAndTorque -= new EventHandler<BodyApplyForceAndTorqueArgs>(PhysicsBody_ApplyForceAndTorque);
            _keep2D.Remove(e.Item);
        }

        private void PhysicsBody_ApplyForceAndTorque(object sender, BodyApplyForceAndTorqueArgs e)
        {

            //TODO: Gravity (see Asteroid Field)


            #region Boundry Force

            Vector3D position = e.Body.Position.ToVector();

            //NOTE: Even though the boundry is square, this turns it into a circle (so the corners of the map are even harder
            //to get to - which is good, the map feel circular)
            if (position.LengthSquared > _boundryForceBeginSquared)
            {
                // See how far into the boundry the item is
                double distaceInto = position.Length - _boundryForceBegin;

                // I want an acceleration of zero when distFromMax is 1, but an accel of 10 when it's boundryMax
                //NOTE: _boundryMax.X is to the edge of the box.  If they are in the corner the distance will be greater, so the accel will be greater
                double accel = UtilityCore.GetScaledValue(0, 30, 0, _boundryWidth, distaceInto);

                // Apply a force toward the center
                Vector3D force = position.ToUnit();
                force *= accel * e.Body.Mass * -1d;

                e.Body.AddForce(force);
            }

            #endregion
        }

        #endregion
    }
}
