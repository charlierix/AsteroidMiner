using System;
using System.Collections.Generic;
using System.Text;

using Game.Orig.Map;
using Game.Orig.Math3D;

namespace Game.Orig.HelperClassesOrig
{
    /// <summary>
    /// This class is responsible for traveling until a ceartain thing happens, and then it explodes (If the qualifier says explosive)
    /// </summary>
    /// <remarks>
    /// TODO:  Make derived classes that:  are guided, fire off other projectiles on a periodic basis, drop mines on a periodic basis, etc
    /// 
    /// My ownership life cycle is a little weird.  I start out being instantiated by a weapon class, but as soon as I'm fired, it hands me
    /// to the map class.  I also carry a pointer to the map class.  When the collision event occurs, I explode, and hang out for a bit in
    /// the exploded state.  Then I tell the map to drop all knowledge of me, and I drop knowledge of the map.
    /// </remarks>
    public class Projectile : BallBlip
    {
        #region Class: ExplosionProps

        /// <summary>
        /// Theoretically, the radius should grow, then shrink.  But for this one, it instantaneously expands, hangs out for a second, and
        /// disapears.
        /// </summary>
        /// <remarks>
        /// TODO:  It would also be nice if I created an outward force
        /// </remarks>
        private class ExplosionProps
        {
            public double ElapsedTime = 0;
            public double Duration = 0;
            public double Radius = 0;
            public double Force = 0;
        }

        #endregion
        #region Class: FuseProps

        /// <summary>
        /// This can be used so the projectile doesn't fly around forever
        /// </summary>
        private class FuseProps
        {
            public double ElapsedTime = 0;
            public double Duration = 0;
        }

        #endregion

        #region Declaration Section

        /// <summary>
        /// This is the token of the ship that fired me
        /// </summary>
        private long _shipToken = 0;

        /// <summary>
        /// The map owns this class, but I can self destruct
        /// </summary>
        private SimpleMap _map = null;

        /// <summary>
        /// This only has meaning to the using classes
        /// </summary>
        private double _pain = 0;

        /// <summary>
        /// If this is true, then collisions with other projectiles won't trigger an explosion.
        /// </summary>
        private bool _ignoreOtherProjectiles = true;

        private ProjectileState _state = ProjectileState.Flying;

        // These will stay null if that feature is unused
        private ExplosionProps _explosion = null;
        private FuseProps _fuse = null;

        private double _curTickElapsedTime = 0;

        #endregion

        #region Constructor

        public Projectile(Ball ball, long shipToken, bool ignoreOtherProjectiles, double pain, SimpleMap map, RadarBlipQual blipQual, long token)
            : this(ball, shipToken, ignoreOtherProjectiles, pain, map, blipQual, token, Guid.NewGuid()) { }

        public Projectile(Ball ball, long shipToken, bool ignoreOtherProjectiles, double pain, SimpleMap map, RadarBlipQual blipQual, long token, Guid objectID)
            : base(ball, CollisionStyle.Standard, blipQual, token, objectID)
        {
            _shipToken = shipToken;
            _ignoreOtherProjectiles = ignoreOtherProjectiles;
            _pain = pain;
            _map = map;

            _map.Collisions += new CollisionsDelegate(Map_Collisions);
        }

        #endregion

        #region Public Properties

        public double Pain
        {
            get
            {
                return _pain;
            }
        }

        public ProjectileState State
        {
            get
            {
                return _state;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// After you instantiate me, but before you let me fly, you can set up the properties of how I will explode.
        /// </summary>
        public void SetExplosion(double radius, double duration, double force)
        {
            _explosion = new ExplosionProps();
            _explosion.ElapsedTime = 0;
            _explosion.Duration = duration;
            _explosion.Radius = radius;
            _explosion.Force = force;
        }
        /// <summary>
        /// After you instantiate me, but before you let me fly, you can set up the properties of my fuse.
        /// </summary>
        public void SetFuse(double duration)
        {
            _fuse = new FuseProps();
            _fuse.ElapsedTime = 0;
            _fuse.Duration = duration;
        }

        #endregion

        #region Event Handlers

        private void Map_Collisions(object sender, Collision[] collisions)
        {
            if (_state != ProjectileState.Flying)
            {
                return;
            }

            long token = this.Token;		// cache the property

            for (int cntr = 0; cntr < collisions.Length; cntr++)
            {
                if (collisions[cntr].Blip1.Token == token || collisions[cntr].Blip2.Token == token)
                {
                    if (_ignoreOtherProjectiles && (collisions[cntr].Blip1 is Projectile) && (collisions[cntr].Blip2 is Projectile))
                    {
                        // I don't explode against other projectiles
                        continue;
                    }

                    if (_explosion != null)
                    {
                        if (!(collisions[cntr].Blip1 is Projectile))
                        {
                            ApplyExplosionForce(collisions[cntr].Blip1);
                        }
                        else if (!(collisions[cntr].Blip2 is Projectile))
                        {
                            ApplyExplosionForce(collisions[cntr].Blip2);
                        }
                    }

                    if (_state == ProjectileState.Flying)
                    {
                        // I only want to explode once
                        Explode();
                    }
                }
            }
        }

        #endregion
        #region Overrides

        public override void TimerTestPosition(double elapsedTime)
        {
            // Test position may get called several times.  I just need to remember the elapsed time from the last call
            _curTickElapsedTime = elapsedTime;

            base.TimerTestPosition(elapsedTime);
        }

        public override void TimerFinish()
        {
            switch (_state)
            {
                case ProjectileState.Flying:
                    Flying();
                    break;

                case ProjectileState.Exploding:
                    Exploding();
                    break;

                case ProjectileState.Dying:
                    // Nothing to do
                    break;

                default:
                    throw new ApplicationException("Unknown ProjectileState: " + _state.ToString());
            }

            base.TimerFinish();
        }

        #endregion

        #region Private Methods

        private void Flying()
        {
            if (_fuse != null)
            {
                _fuse.ElapsedTime += _curTickElapsedTime;

                if (_fuse.ElapsedTime > _fuse.Duration)
                {
                    Explode();
                }
            }
        }

        private void Exploding()
        {
            // Keep setting my velocity back to zero
            base.Ball.Velocity.StoreNewValues(new MyVector());

            // See if enough time has gone by
            bool isExpired = false;
            if (_explosion == null)
            {
                isExpired = true;
            }
            else
            {
                // Bump up my elapsed time
                _explosion.ElapsedTime += _curTickElapsedTime;

                if (_explosion.ElapsedTime > _explosion.Duration)
                {
                    isExpired = true;
                }
            }

            if (isExpired)
            {
                // Let myself know that I am in the process of dying
                _state = ProjectileState.Dying;

                // Tell the map to drop me, and drop reference to the map.
                _map.Remove(this.Token);
                _map.Collisions -= new CollisionsDelegate(Map_Collisions);
                _map = null;
            }
        }

        private void Explode()
        {
            if (_state != ProjectileState.Flying)
            {
                throw new InvalidOperationException("Explode was called when not in a flying state: " + _state.ToString());
            }

            // Drop my velocity to zero (In case somebody asks what my velocity is)
            //TODO:  May want to go into a ghost state, but if I do, the map won't do any collision detection, and nothing will know
            // to add more pain
            base.Ball.Velocity.StoreNewValues(new MyVector());

            // Explode myself
            if (_explosion != null)
            {
                base.Ball.Radius = _explosion.Radius;
            }

            // Store my new state
            _state = ProjectileState.Exploding;

            // Don't do any other collision physics
            base.CollisionStyle = CollisionStyle.Ghost;

            if (_explosion == null)
            {
                // This will put me straight into a dying state (after removing me from the map)
                Exploding();
            }
        }

        private void ApplyExplosionForce(RadarBlip blip)
        {
            if (!(blip is BallBlip))
            {
                return;
            }

            BallBlip castBlip = (BallBlip)blip;

            MyVector force = castBlip.Ball.Position - this.Ball.Position;
            force.BecomeUnitVector();
            force.Multiply(_explosion.Force);

            // Setting the force does no good, because PrepareForNewTick is called before it can take affect
            //castBlip.Ball.ExternalForce.Add(force);

            force.Divide(castBlip.Ball.Mass);		// F=MA
            castBlip.Ball.Velocity.Add(force);
        }

        #endregion
    }

    #region Enum: ProjectileState

    public enum ProjectileState
    {
        Flying = 0,
        Exploding,
        Dying
    }

    #endregion
}
