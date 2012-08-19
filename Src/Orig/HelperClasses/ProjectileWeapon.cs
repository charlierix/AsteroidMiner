using System;
using System.Collections.Generic;
using System.Text;

using Game.Orig.Map;
using Game.Orig.Math3D;

namespace Game.Orig.HelperClassesOrig
{
    /// <summary>
    /// This class will fire projectiles.  It is meant to fire only one type of projectile (speed, explosion settings, etc)
    /// </summary>
    /// <remarks>
    /// I want to keep the ammo clips fairly generic, and make the weapon class define the properties of what gets fired.  For instance,
    /// the ammo clip type could simply be shotgun shell, but the gun could be a 20 gauge quail gun, or it could be a fully auto 10
    /// gauge slug gun.
    /// 
    /// You can set up multiple firing modes, each one with a different rate of fire.
    /// 
    /// Every time a projectile is fired, it generates kick against the ship that fired it
    /// </remarks>
    public class ProjectileWeapon
    {
        #region Classes: New Projectile Settings

        //	These help me know how to set up a projectile

        private class ProjectileFuseSettings		//	This is it's own class, because it could be null, and I don't want to deal with a nullable double
        {
            public double Duration = 0;

            public ProjectileFuseSettings(double duration)
            {
                this.Duration = duration;
            }
        }

        private class ProjectileExplosionSettings
        {
            public double Radius = 0;
            public double Duration = 0;
            public double Force = 0;

            public ProjectileExplosionSettings(double radius, double duration, double force)
            {
                this.Radius = radius;
                this.Duration = duration;
                this.Force = force;
            }
        }

        private class ProjectileSettings
        {
            public double Speed = 0;
            public double Radius = 0;
            public double Mass = 0;
            public double Pain = 0;
            public bool IgnoreOtherProjectiles = true;
            public RadarBlipQual Qual = RadarBlipQual.Projectile;
            public ProjectileFuseSettings Fuse = null;
            public ProjectileExplosionSettings Explosion = null;

            public ProjectileSettings(double speed, double radius, double mass, double pain, bool ignoreOtherProjectiles, RadarBlipQual qual)
            {
                this.Speed = speed;
                this.Radius = radius;
                this.Mass = mass;
                this.Pain = pain;
                this.IgnoreOtherProjectiles = ignoreOtherProjectiles;
                this.Qual = qual;
            }
        }

        #endregion
        #region Class: FiringMode

        /// <summary>
        /// This defines a rate of fire mode.  Keep in mind that if these rates are smaller than the elapsed time of the timer, then
        /// I will fire a round every time the timer is called (assuming the rounds per outer haven't been exceeded)
        /// </summary>
        private class FiringMode
        {
            /// <summary>
            /// This is how often I will reset myself
            /// </summary>
            public double OuterFiringRate = 0;

            /// <summary>
            /// This is how often I will fire a round
            /// </summary>
            public double InnerFiringRate = 0;

            /// <summary>
            /// This is how many rounds to fire within the outer time cycle
            /// </summary>
            public int NumRoundsPerOuter = 0;

            public FiringMode(double outerRate, double innerRate, int numRoundsPerOuter)
            {
                this.OuterFiringRate = outerRate;
                this.InnerFiringRate = innerRate;
                this.NumRoundsPerOuter = numRoundsPerOuter;
            }
        }

        #endregion
        #region Class: Barrel

        /// <summary>
        /// This tells me if there is a direction offset, or if I should just use my normal direction facing
        /// </summary>
        public class Barrel
        {
            /// <summary>
            /// The offset relative to the ship's position.  Leave null if it should come from the center.
            /// </summary>
            public MyVector Offset = null;
            /// <summary>
            /// The direction facing of the barrel (relative to the ship's direction facing).  Leave null if it will
            /// fire in the same direction as the ship.
            /// </summary>
            public MyQuaternion Rotation = null;

            public Barrel()
            {
            }
            public Barrel(MyVector offset)
            {
                this.Offset = offset;
            }
            public Barrel(MyQuaternion rotation)
            {
                this.Rotation = rotation;
            }
            public Barrel(MyVector offset, MyQuaternion rotation)
            {
                this.Offset = offset;
                this.Rotation = rotation;
            }
        }

        #endregion

        #region Declaration Section

        /// <summary>
        /// This tells me if they are holding in the trigger
        /// </summary>
        private bool _isFiring = false;

        /// <summary>
        /// Every time I create a projectile, I hand it to the map
        /// </summary>
        private SimpleMap _map = null;

        //	This is the bounding box that I will set up all the projectiles with
        private bool _useBoundry = false;
        private MyVector _boundryLower = null;
        private MyVector _boundryUpper = null;

        /// <summary>
        /// This is the ship that the weapon is tied to
        /// </summary>
        /// <remarks>
        /// This weapon will only fire if the ship is non null
        /// </remarks>
        private BallBlip _ship = null;

        /// <summary>
        /// If this is true, then the gun will only fire if _ammoClip is non-null and has enough ammo,
        /// otherwise the gun has infinte ammo
        /// </summary>
        private bool _useAmmoClip = false;
        #region AmmoClip Settings

        /// <summary>
        /// This tells me what type of ammo I can use
        /// </summary>
        /// <remarks>
        /// When this is supported, do away with _useAmmoClip, and make a setting for infinite
        /// </remarks>
        //private AmmoType _ammoType;
        /// <summary>
        /// This is the ammo clip that I draw from
        /// </summary>
        //private AmmoClip _ammoClip = null;
        /// <summary>
        /// Every time I fire, I will try to pull this much ammo out of the clip first.  This is the sum of all barrels.
        /// </summary>
        //private double _amtAmmoToPull = 0;

        #endregion

        /// <summary>
        /// Every time a projectile is fired, I will add the kick force to the ball's internal force
        /// </summary>
        private bool _produceKick = false;

        /// <summary>
        /// This is everything I need to get a single projectile going
        /// </summary>
        private ProjectileSettings _projectileSettings = null;

        /// <summary>
        /// These are the individual barrels.  Every time I fire, each barrel gets fired simultaneously.
        /// </summary>
        /// <remarks>
        /// All the barrels use the same firing pattern
        /// </remarks>
        private List<Barrel> _barrels = new List<Barrel>();

        /// <summary>
        /// If you want to force them to wait for the next bullet to be ready, then turn this on.  If you do, I will force them to wait until
        /// _elapsedTime.OuterFiringRate has dropped back down to zero before starting a new inner cycle.  If this is false, then every time
        /// they release the trigger, or change firing modes, I will set the elapsed.outer back to zero.  If this is true, then it is highly recommended 
        /// that you keep the OuterTime the same across all firing modes.
        /// </summary>
        private bool _enforceFiringModeOuterTime = false;

        /// <summary>
        /// This keeps track of the last time that a projectile was fired.  I go ahead and reuse the FiringMode class, even though the variable
        /// names don't make perfect sense.
        /// </summary>
        private FiringMode _elapsedTime = null;

        /// <summary>
        /// This is the pointer into pudtFiringModes that I am currently set up to use
        /// </summary>
        private int _activeFiringMode = 0;
        /// <summary>
        /// These are the firing modes that you can choose from
        /// </summary>
        private List<FiringMode> _firingModes = new List<FiringMode>();

        #endregion

        #region Constructor

        public ProjectileWeapon(double projectilePain, double projectileRadius, double projectileMass, double projectileSpeed, bool produceKick, bool ignoreOtherProjectiles, RadarBlipQual projectileQual, bool enforceFiringModeOuterTime, SimpleMap map)
            : this(projectilePain, projectileRadius, projectileMass, projectileSpeed, produceKick, ignoreOtherProjectiles, projectileQual, enforceFiringModeOuterTime, map, null, null) { }
        public ProjectileWeapon(double projectilePain, double projectileRadius, double projectileMass, double projectileSpeed, bool produceKick, bool ignoreOtherProjectiles, RadarBlipQual projectileQual, bool enforceFiringModeOuterTime, SimpleMap map, MyVector boundryLower, MyVector boundryUpper)
        {
            _projectileSettings = new ProjectileSettings(projectileSpeed, projectileRadius, projectileMass, projectilePain, ignoreOtherProjectiles, projectileQual);
            _enforceFiringModeOuterTime = enforceFiringModeOuterTime;
            _map = map;
            _produceKick = produceKick;

            if (boundryLower != null && boundryUpper != null)
            {
                _useBoundry = true;
                _boundryLower = boundryLower;
                _boundryUpper = boundryUpper;
            }

            _elapsedTime = new FiringMode(0, 0, 0);
        }

        #endregion

        #region Properties

        public Barrel[] Barrels
        {
            get
            {
                return _barrels.ToArray();
            }
        }

        public double ProjectileSpeed
        {
            get
            {
                return _projectileSettings.Speed;
            }
            set
            {
                _projectileSettings.Speed = value;
            }
        }
        public double ProjectileRadius
        {
            get
            {
                return _projectileSettings.Radius;
            }
            set
            {
                _projectileSettings.Radius = value;
            }
        }
        public double ProjectileMass
        {
            get
            {
                return _projectileSettings.Mass;
            }
            set
            {
                _projectileSettings.Mass = value;
            }
        }
        public double ProjectilePain
        {
            get
            {
                return _projectileSettings.Pain;
            }
            set
            {
                _projectileSettings.Pain = value;
            }
        }

        public bool IgnoreOtherProjectiles
        {
            get
            {
                return _projectileSettings.IgnoreOtherProjectiles;
            }
            set
            {
                _projectileSettings.IgnoreOtherProjectiles = value;
            }
        }

        #endregion

        #region Public Methods

        public void SetProjectileExplosion(double radius, double duration, double force)
        {
            _projectileSettings.Explosion = new ProjectileExplosionSettings(radius, duration, force);
        }
        public void SeProjectileFuse(double duration)
        {
            _projectileSettings.Fuse = new ProjectileFuseSettings(duration);
        }

        public void AddBarrel()
        {
            _barrels.Add(new Barrel());
        }
        public void AddBarrel(MyVector offset)
        {
            _barrels.Add(new Barrel(offset));
        }
        public void AddBarrel(MyQuaternion rotation)
        {
            _barrels.Add(new Barrel(rotation));
        }
        public void AddBarrel(MyVector offset, MyQuaternion rotation)
        {
            _barrels.Add(new Barrel(offset, rotation));
        }

        /// <summary>
        /// This ties this gun to a ship (null is an allowed value)
        /// </summary>
        public void SetShip(BallBlip ship)
        {
            _ship = ship;
        }

        /// <summary>
        /// I figured I'd make an easy to use version of this function
        /// </summary>
        /// <returns>The index into the list of firing modes</returns>
        public int AddFiringMode(double firingRate)
        {
            return AddFiringMode(firingRate, firingRate, 1);
        }
        /// <summary>
        /// This function will add a firing mode to me.
        /// </summary>
        /// <remarks>
        /// Soldier of Fortune 2 had some awesome ideas for firing mode.  Some of their assault rifles had three modes (Fully Auto,
        /// 3 Round Burst, Single Shot)  For running and gunning, I'd use the full auto, or the burst, but when I pulled up the scope,
        /// I'd set it to single shot.  This would then double as a sniper rifle (if I didn't use single shot with the scope, the kick would
        /// be terrible).  I ended up mapping the toggle to the thumb button on my mouse because I liked the feature so much.  BF2
        /// uses the same key as the gun (3) to switch firing modes.
        /// 
        /// The first firing mode that you add will be the default, so you probably want that one to be full auto
        /// 
        /// If you use the mentality of a burst fire, then you probably want to turn EnforceFiringModeOuterTime off, and set the outer
        /// rate to Double.MaxValue.  That way every time they release the trigger, it will be ready for a new burst of bullets.  Or if you don't
        /// want to force them to release the trigger, you could set the outer rate to a couple seconds.  Then it will be BRAAAAP...pause...BRAAAAP...etc
        /// </remarks>
        /// <returns>The index into the list of firing modes</returns>
        public int AddFiringMode(double outerFiringRate, double innerFiringRate, int numRoundsPerOuter)
        {
            _firingModes.Add(new FiringMode(outerFiringRate, innerFiringRate, numRoundsPerOuter));

            return _firingModes.Count - 1;
        }

        /// <summary>
        /// This will cycle through the firing modes.  I return the new mode I'm on.
        /// </summary>
        public int SwitchFiringMode()
        {
            _activeFiringMode++;

            if (_activeFiringMode >= _firingModes.Count)
            {
                _activeFiringMode = 0;
            }

            if (!_enforceFiringModeOuterTime)
            {
                //	?????????????????
                _elapsedTime.OuterFiringRate = 0;
            }

            return _activeFiringMode;
        }
        /// <summary>
        /// This sets the current firing mode
        /// </summary>
        public void SwitchFiringModes(int index)
        {
            if (index < 0 || index >= _firingModes.Count)
            {
                throw new ArgumentOutOfRangeException("index", index.ToString(), "index is out of range: " + index.ToString() + ", Max Allowed: " + ((int)(_firingModes.Count - 1)).ToString());
            }

            _activeFiringMode = index;

            if (!_enforceFiringModeOuterTime)
            {
                //	?????????????????
                _elapsedTime.OuterFiringRate = 0;
            }
        }

        public void StartFiring()
        {
            _isFiring = true;
        }
        public void StopFiring()
        {
            _isFiring = false;

            if (!_enforceFiringModeOuterTime)
            {
                _elapsedTime.OuterFiringRate = 0;
            }
        }

        public void Timer(double elapsedTime)
        {
            //	Decrement the elapsed time (or increment depending on how you want to think about it)
            _elapsedTime.OuterFiringRate -= elapsedTime;
            if (_elapsedTime.OuterFiringRate < 0)
            {
                _elapsedTime.OuterFiringRate = 0;
            }

            _elapsedTime.InnerFiringRate -= elapsedTime;
            if (_elapsedTime.InnerFiringRate < 0)
            {
                _elapsedTime.InnerFiringRate = 0;
            }

            //	If I'm not currently firing, then there is nothing left to do
            if (!_isFiring)
            {
                return;
            }

            //	First off, see if I'm ready for a new outer loop
            if (_elapsedTime.OuterFiringRate == 0)
            {
                if (Fire())
                {
                    //	I fired, reset some stuff
                    _elapsedTime.NumRoundsPerOuter = 0;
                    _elapsedTime.OuterFiringRate = _firingModes[_activeFiringMode].OuterFiringRate;
                }
            }
            else if (_elapsedTime.InnerFiringRate == 0 && _elapsedTime.NumRoundsPerOuter < _firingModes[_activeFiringMode].NumRoundsPerOuter)
            {
                //	I wasn't ready for a new outer loop, but I am ready for a new inner loop
                Fire();
            }
        }

        #endregion

        #region Private Methods

        private bool Fire()
        {
            //	Before I begin, try to grab some ammo
            //if(_useAmmoClip && _ammoClip.RemoveQuantity(_amtAmmoToPull, true) > 0)
            //{
            //    return false;
            //}

            //	The ammo has been grabbed, bump the elapsed time structure
            _elapsedTime.InnerFiringRate = _firingModes[_activeFiringMode].InnerFiringRate;
            _elapsedTime.NumRoundsPerOuter++;

            //	Fire each barrel
            foreach (Barrel barrel in _barrels)
            {
                #region Fire New Projectile

                //	Create the position
                MyVector position = _ship.Ball.Position.Clone();
                if (barrel.Offset != null)
                {
                    position = _ship.Ball.Rotation.GetRotatedVector(barrel.Offset, true) + position;
                }

                //	Create the direction facing
                DoubleVector dirFacing = _ship.Ball.DirectionFacing.Clone();
                if (barrel.Rotation != null)
                {
                    dirFacing = barrel.Rotation.GetRotatedVector(dirFacing, true);
                }

                //	Make a ball
                Ball ball = null;
                if (_useBoundry)
                {
                    ball = new Ball(position, dirFacing, _projectileSettings.Radius, _projectileSettings.Mass, _boundryLower, _boundryUpper);
                }
                else
                {
                    ball = new Ball(position, dirFacing, _projectileSettings.Radius, _projectileSettings.Mass);
                }

                MyVector dirFacingUnit = dirFacing.Standard;
                dirFacingUnit.BecomeUnitVector();

                //	Set the velocity
                ball.Velocity.StoreNewValues(dirFacingUnit * _projectileSettings.Speed);
                ball.Velocity.Add(_ship.Ball.Velocity);

                //	Make a projectile
                Projectile projectile = new Projectile(ball, _ship.Token, _projectileSettings.IgnoreOtherProjectiles, _projectileSettings.Pain, _map, _projectileSettings.Qual, _map.GetNextToken());

                //	Set up explosion and fuse settings
                if (_projectileSettings.Explosion != null)
                {
                    projectile.SetExplosion(_projectileSettings.Explosion.Radius, _projectileSettings.Explosion.Duration, _projectileSettings.Explosion.Force);
                }
                if (_projectileSettings.Fuse != null)
                {
                    projectile.SetFuse(_projectileSettings.Fuse.Duration);
                }

                //	Generate the kick
                if (_produceKick)
                {
                    #region Kick

                    MyVector kick = dirFacingUnit * _projectileSettings.Speed * (ball.Mass * -1d);

                    if (_ship.TorqueBall != null)
                    {
                        _ship.TorqueBall.ApplyExternalForce(ball.Position - _ship.Ball.Position, kick);
                    }
                    else
                    {
                        _ship.Ball.ExternalForce.Add(kick);
                    }

                    #endregion
                }

                //	Hand the projectile to the map
                _map.Add(projectile);

                #endregion
            }

            //	Exit function
            return true;
        }

        #endregion
    }
}
