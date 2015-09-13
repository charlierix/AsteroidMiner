using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using Game.Newt.v1.AsteroidMiner1.ShipAddons;
using Game.HelperClassesWPF;
using Game.Newt.v1.NewtonDynamics1;

namespace Game.Newt.v1.AsteroidMiner1
{
    /// <summary>
    /// This is my first attempt at the swarmbot.  The settings are all hardcoded, and it's not really meant to go into
    /// production code. (swarmbot2 is a copy of 1, but using the base, so is a bit easier to look at)
    /// </summary>
    public class SwarmBot1
    {
        #region Enum: BehaviorType

        public enum BehaviorType
        {
            /// <summary>
            /// This is the simplest.  It always goes straight for the chase point, full force, ignoring everything else
            /// </summary>
            StraightToTarget,
            /// <summary>
            /// This is like the other, but will try to adjust for the current velocity
            /// </summary>
            StraightToTarget_VelocityAware1,
            /// <summary>
            /// Like the previous, but limits the velocity
            /// </summary>
            //StraightToTarget_VelocityAware2

            /// <summary>
            /// This one ignores the chase point, and flies toward the center of the flock
            /// </summary>
            TowardCenterOfFlock,
            /// <summary>
            /// They go toward the center of the flock, and also keep a small distance from others
            /// </summary>
            CenterFlock_AvoidNeighbors,
            /// <summary>
            /// They also try to match the flock's cumulative velocity
            /// </summary>
            CenterFlock_AvoidNeighbors_FlockVelocity,

            /// <summary>
            /// This has the 3 flocking rules, and also chases the chase point
            /// </summary>
            Flocking_ChasePoint
        }

        #endregion

        #region Declaration Section

        private Viewport3D _viewport = null;

        // These are visual models that don't count toward collisions.  But, once per frame, they need to be transformed to stay with the ship
        private ModelVisual3D _core = null;
        private GeometryModel3D _coreGeometry = null;
        private MaterialGroup _coreMaterialNeutral = null;
        private MaterialGroup _coreMaterialAttack = null;
        private PointLight _lightAttack = null;

        private bool _isAttacking = false;

        // This is the thruster
        private ThrustLine _thruster = null;
        private Vector3D _origThrustDirection;

        // Once per frame, these are adjusted (they are instructions to the thruster)
        /// <summary>
        /// This tells how to rotate the thruster
        /// </summary>
        /// <remarks>
        /// This one is a local transform.  It will have to be added to the physics body's transform
        /// </remarks>
        private Transform3D _thrustTransform = null;
        /// <summary>
        /// This is how hard to run the thruster
        /// </summary>
        private double _thrustPercent = 1d;

        #endregion

        #region Public Properties

        private Random _rand = new Random();
        public Random Rand
        {
            get
            {
                return _rand;
            }
            set
            {
                _rand = value;
            }
        }

        public ConvexBody3D PhysicsBody
        {
            get;
            private set;
        }

        public Vector3D VelocityWorld
        {
            get
            {
                return this.PhysicsBody.VelocityCached;		// this one is safer (can be called at any time, not just within the apply force/torque event)
            }
        }

        //TODO:  Support setting mass
        private double _mass = 1d;
        public double Mass
        {
            get
            {
                return _mass;
            }
        }

        //TODO:  Support setting size at any time
        private double _radius = 1d;
        public double Radius
        {
            get
            {
                return _radius;
            }
            set
            {
                if (this.PhysicsBody != null)
                {
                    throw new InvalidOperationException("Can't set the radius after the bot has been created");
                }

                _radius = value;
            }
        }

        private Point3D _chasePoint = new Point3D(0, 0, 0);
        public Point3D ChasePoint
        {
            get
            {
                return _chasePoint;
            }
            set
            {
                _chasePoint = value;
            }
        }

        private BehaviorType _behavior = BehaviorType.StraightToTarget;
        public BehaviorType Behavior
        {
            get
            {
                return _behavior;
            }
            set
            {
                _behavior = value;
            }
        }

        private bool _shouldDrawThrustLine = false;
        public bool ShouldDrawThrustLine
        {
            get
            {
                return _shouldDrawThrustLine;
            }
            set
            {
                _shouldDrawThrustLine = value;

                if (!_shouldDrawThrustLine && _thruster.IsFiring)
                {
                    // By simply not calling _thruster.DrawVisual, the thrust line would still be visible.  So by turning off the thruster, it will
                    // quit drawing the thrust line, and wait for DrawVisual to be called again (hackish, but it works)
                    _thruster.IsFiring = false;
                    _thruster.IsFiring = true;
                }
            }
        }

        private List<SwarmBot1> _otherBots = new List<SwarmBot1>();
        public List<SwarmBot1> OtherBots
        {
            get
            {
                return _otherBots;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Set the properties, then call create
        /// NOTE:  This adds itself to the viewport and world.  In the future, that should be handled by the caller
        /// </summary>
        public void CreateBot(Viewport3D viewport, SharedVisuals sharedVisuals, World world, Point3D worldPosition)
        {
            _viewport = viewport;

            // Thruster
            _origThrustDirection = new Vector3D(0, 4, 0);
            _thruster = new ThrustLine(_viewport, sharedVisuals, _origThrustDirection, new Vector3D(0, 0, 0));

            MaterialGroup material = null;
            GeometryModel3D geometry = null;
            ModelVisual3D model = null;

            #region Interior Extra Visuals

            // These are visuals that will stay oriented to the ship, but don't count in collision calculations

            #region Core

            // Neutral
            _coreMaterialNeutral = new MaterialGroup();
            _coreMaterialNeutral.Children.Add(new DiffuseMaterial(Brushes.DimGray));
            _coreMaterialNeutral.Children.Add(new SpecularMaterial(Brushes.DimGray, 75d));

            // Attack
            _coreMaterialAttack = new MaterialGroup();
            _coreMaterialAttack.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(Colors.Red, UtilityWPF.AlphaBlend(Colors.Black, Colors.DimGray, .5), .15d))));
            _coreMaterialAttack.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(255, 255, 128, 128)), 100d));

            _lightAttack = new PointLight();
            _lightAttack.Color = Color.FromArgb(255, 96, 0, 0);
            _lightAttack.Position = new Point3D(0, 0, 0);
            _lightAttack.Range = _radius * 3;

            // Geometry Model
            _coreGeometry = new GeometryModel3D();
            _coreGeometry.Material = _coreMaterialNeutral;
            _coreGeometry.BackMaterial = _coreMaterialNeutral;
            _coreGeometry.Geometry = UtilityWPF.GetSphere_LatLon(5, _radius * .4, _radius * .4, _radius * .4);
            _coreGeometry.Transform = new TranslateTransform3D(0, 0, 0);

            // Model Visual
            _core = new ModelVisual3D();
            _core.Content = _coreGeometry;

            //NOTE: model.Transform is set to the physics body's transform every frame

            // Add to the viewport
            _viewport.Children.Add(_core);

            #endregion

            #endregion

            #region WPF Model

            // Material
            //NOTE:  There seems to be an issue with drawing objects inside a semitransparent object - I think they have to be added in a certain order or something
            Brush skinBrush = new SolidColorBrush(Color.FromArgb(25, 255, 255, 255));  // making the skin semitransparent, so you can see the components inside

            material = new MaterialGroup();
            material.Children.Add(new DiffuseMaterial(skinBrush));
            material.Children.Add(new SpecularMaterial(Brushes.White, 75d));     // more reflective (and white light)

            MaterialGroup backMaterial = new MaterialGroup();
            backMaterial.Children.Add(new DiffuseMaterial(skinBrush));
            backMaterial.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(255, 20, 20, 20)), 10d));       // dark light, and not very reflective

            // Geometry Model
            geometry = new GeometryModel3D();
            geometry.Material = material;
            geometry.BackMaterial = backMaterial;
            geometry.Geometry = UtilityWPF.GetSphere_LatLon(5, _radius, _radius, _radius);

            // Transform
            Transform3DGroup transform = new Transform3DGroup();		// rotate needs to be added before translate
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 0)));
            transform.Children.Add(new TranslateTransform3D(worldPosition.ToVector()));

            // Model Visual
            model = new ModelVisual3D();
            model.Content = geometry;
            model.Transform = transform;

            // Add to the viewport
            _viewport.Children.Add(model);

            #endregion
            #region Physics Body

            // Make a physics body that represents this shape
            this.PhysicsBody = new ConvexBody3D(world, model);

            this.PhysicsBody.Mass = Convert.ToSingle(this.Mass);

            this.PhysicsBody.LinearDamping = .01f;
            //this.PhysicsBody.AngularDamping = new Vector3D(.01f, .01f, .01f);
            //this.PhysicsBody.AngularDamping = new Vector3D(.01f, .01f, 100000f);		// this doesn't work.  probably to to cap the z back to zero, and any spin to zero
            this.PhysicsBody.AngularDamping = new Vector3D(10f, 10f, 10f);

            this.PhysicsBody.ApplyForce += new BodyForceEventHandler(Body_ApplyForce);
            //this.PhysicsBody.NewtonBody.ApplyForceAndTorque

            #endregion

            #region Exterior Extra Visuals

            // There is a bug in WPF where visuals added after a semitransparent one won't show inside.  So if you want to add exterior
            // bits, this would be the place

            #endregion

            _thruster.IsFiring = true;
        }

        public void GoNeutral()
        {
            if (_core == null)
            {
                throw new InvalidOperationException("Must call CreateBot first");
            }

            _coreGeometry.Material = _coreMaterialNeutral;
            _coreGeometry.BackMaterial = _coreMaterialNeutral;

            _core.Content = _coreGeometry;      // no light

            _isAttacking = false;
        }
        public void GoAttack()
        {
            if (_core == null)
            {
                throw new InvalidOperationException("Must call CreateBot first");
            }

            _coreGeometry.Material = _coreMaterialAttack;
            _coreGeometry.BackMaterial = _coreMaterialAttack;

            Model3DGroup group = new Model3DGroup();
            group.Children.Add(_coreGeometry);
            group.Children.Add(_lightAttack);

            _core.Content = group;

            _isAttacking = true;
        }

        public void WorldUpdating()
        {
            #region Visuals

            _core.Transform = this.PhysicsBody.Transform;

            #endregion

            #region Behavior

            switch (_behavior)
            {
                case BehaviorType.StraightToTarget:
                    Brain_StraightToTarget();
                    break;
                case BehaviorType.StraightToTarget_VelocityAware1:
                    Brain_StraightToTarget_VelocityAware1();
                    break;
                //case BehaviorType.StraightToTarget_VelocityAware2:
                //Brain_StraightToTarget_VelocityAware2();        // currently there is no difference
                //break;


                case BehaviorType.TowardCenterOfFlock:
                    Brain_TowardCenterOfFlock();
                    break;
                case BehaviorType.CenterFlock_AvoidNeighbors:
                    Brain_CenterFlock_AvoidNeighbors();
                    break;
                case BehaviorType.CenterFlock_AvoidNeighbors_FlockVelocity:
                    Brain_CenterFlock_AvoidNeighbors_FlockVelocity();
                    break;


                case BehaviorType.Flocking_ChasePoint:
                    Brain_Flocking_ChasePoint();
                    break;

                default:
                    throw new ApplicationException("Unknown BehaviorType: " + _behavior.ToString());
            }

            #endregion

            #region Thrust Line

            if (_shouldDrawThrustLine)
            {
                _thruster.DrawVisual(_thrustPercent, this.PhysicsBody.Transform, _thrustTransform);
            }

            #endregion
        }

        #endregion

        #region Event Listeners

        private void Body_ApplyForce(Body sender, BodyForceEventArgs e)
        {
            if (sender != this.PhysicsBody)
            {
                return;
            }

            #region Thrusters

            if (_thrustTransform != null)
            {
                _thruster.ApplyForce(_thrustPercent, this.PhysicsBody.Transform, _thrustTransform, e);
            }

            #endregion
        }

        #endregion

        #region Private Methods

        private void Brain_StraightToTarget()
        {
            // Figure out what direction to go to get to the chase point (in local coords)
            Vector3D directionToGo = StraightToTargetWorker(_chasePoint);

            // Now that I know where to go, rotate the original thruster direction (0,1,0) to line up with the desired direction
            Vector3D axis;
            double radians;
            Math3D.GetRotation(out axis, out radians, _origThrustDirection, directionToGo);

            // Thrust Direction
            _thrustTransform = new RotateTransform3D(new AxisAngleRotation3D(axis, Math1D.RadiansToDegrees(radians)));

            // Thrust Strength
            if (_isAttacking)
            {
                _thrustPercent = 1d;
            }
            else
            {
                _thrustPercent = .5d;
            }

            #region Attempt to world

            // This fails when converting everything to world coords

            //Point3D position = this.PhysicsBody.PositionToWorld(new Point3D(0, 0, 0));

            //Vector3D directionToGo = _chasePoint.ToVector() - position.ToVector();

            //Vector3D origThrustDirWorld = this.PhysicsBody.DirectionToWorld(_origThrustDirection);

            //Vector3D axis;
            //double radians;
            //Math3D.GetRotation(out axis, out radians, origThrustDirWorld, directionToGo);

            //_thrustTransform = new RotateTransform3D(new AxisAngleRotation3D(axis, Math3D.RadiansToDegrees(radians)));

            #endregion
        }
        private Vector3D StraightToTargetWorker(Point3D chasePointWorld)
        {
            // Convert everything to local coords
            Point3D position = new Point3D(0, 0, 0);
            Point3D chasePointLocal = this.PhysicsBody.PositionFromWorld(chasePointWorld);

            return chasePointLocal.ToVector() - position.ToVector();
        }

        private void Brain_StraightToTarget_VelocityAware1()
        {
            // Figure out what direction to go in local coords
            Vector3D directionToGo = StraightToTarget_VelocityAware1Worker(_chasePoint);

            // Now that I know where to go, rotate the original thruster direction (0,1,0) to line up with the desired direction
            Vector3D axis;
            double radians;
            Math3D.GetRotation(out axis, out radians, _origThrustDirection, directionToGo);

            // Thrust Direction
            _thrustTransform = new RotateTransform3D(new AxisAngleRotation3D(axis, Math1D.RadiansToDegrees(radians)));

            // Thrust Strength
            if (_isAttacking)
            {
                _thrustPercent = 1d;
            }
            else
            {
                _thrustPercent = .5d;
            }
        }
        private Vector3D StraightToTarget_VelocityAware1Worker(Point3D chasePointWorld)
        {
            // Convert everything to local coords
            Point3D position = new Point3D(0, 0, 0);
            Point3D chasePointLocal = this.PhysicsBody.PositionFromWorld(chasePointWorld);

            Vector3D directionToGo = chasePointLocal.ToVector() - position.ToVector();

            Vector3D axis;
            double radians;

            #region Adjust for current velocity attempt1a

            Vector3D currentVelocity = this.VelocityWorld;

            if (!Math1D.IsNearZero(currentVelocity.LengthSquared))
            {
                currentVelocity = this.PhysicsBody.DirectionFromWorld(currentVelocity);

                Math3D.GetRotation(out axis, out radians, directionToGo, currentVelocity);

                // This is how much to rotate direction to align with current velocity, I want to go against the current velocity (if aligned,
                // the angle will be zero, so negating won't make a difference)
                radians *= -1;

                // If it's greater than 90 degrees, then just use the original direction (because it will pull the velocity in line
                // eventually)  I don't multiply by .5, because when it is very close to 90 degrees, the bot will thrash a bit
                if (Math.Abs(radians) < Math.PI * .4d)
                {
                    // Change the direction by the angle
                    directionToGo = directionToGo.GetRotatedVector(axis, Math1D.RadiansToDegrees(radians));
                }
            }

            #endregion

            // Exit Function
            return directionToGo;
        }

        private void Brain_StraightToTarget_VelocityAware2()
        {
            // Velocity should be zero when touching the chase point

            //TODO:  Implement this (when not attacking, velocity should slow to a max speed the closer to the chase point)



            // Convert everything to local coords
            Point3D position = new Point3D(0, 0, 0);
            Point3D chasePoint = this.PhysicsBody.PositionFromWorld(_chasePoint);

            Vector3D directionToGo = chasePoint.ToVector() - position.ToVector();

            Vector3D axis;
            double radians;

            #region Adjust for current velocity attempt1a

            Vector3D currentVelocity = this.VelocityWorld;

            if (!Math1D.IsNearZero(currentVelocity.LengthSquared))
            {
                currentVelocity = this.PhysicsBody.DirectionFromWorld(currentVelocity);

                Math3D.GetRotation(out axis, out radians, directionToGo, currentVelocity);

                // This is how much to rotate direction to align with current velocity, I want to go against the current velocity (if aligned,
                // the angle will be zero, so negating won't make a difference)
                radians *= -1;

                // If it's greater than 90 degrees, then just use the original direction (because it will pull the velocity in line
                // eventually)  I don't multiply by .5, because when it is very close to 90 degrees, the bot will thrash a bit
                if (Math.Abs(radians) < Math.PI * .4d)
                {
                    // Change the direction by the angle
                    directionToGo = directionToGo.GetRotatedVector(axis, Math1D.RadiansToDegrees(radians));
                }
            }

            #endregion

            // Now that I know where to go, rotate the original thruster direction (0,1,0) to line up with the desired direction
            Math3D.GetRotation(out axis, out radians, _origThrustDirection, directionToGo);

            // Thrust Direction
            _thrustTransform = new RotateTransform3D(new AxisAngleRotation3D(axis, Math1D.RadiansToDegrees(radians)));

            // Thrust Strength
            if (_isAttacking)
            {
                _thrustPercent = 1d;
            }
            else
            {
                _thrustPercent = .5d;
            }
        }

        private void Brain_TowardCenterOfFlock()
        {
            // Calculate the center of position of the flock
            //TODO:  Have a vision limit

            Vector3D centerPosition = new Vector3D(0, 0, 0);
            foreach (SwarmBot1 bot in _otherBots)
            {
                centerPosition += bot.PhysicsBody.PositionToWorld(bot.PhysicsBody.CenterOfMass).ToVector();
            }

            if (_otherBots.Count > 0)       // can't divide by zero
            {
                centerPosition /= _otherBots.Count;
            }

            // Figure out what direction to go in local coords
            Vector3D flockCenterDirection = StraightToTarget_VelocityAware1Worker(centerPosition.ToPoint());





            // Now that I know where to go, rotate the original thruster direction (0,1,0) to line up with the desired direction
            Vector3D axis;
            double radians;
            Math3D.GetRotation(out axis, out radians, _origThrustDirection, flockCenterDirection);

            // Thrust Direction
            _thrustTransform = new RotateTransform3D(new AxisAngleRotation3D(axis, Math1D.RadiansToDegrees(radians)));

            // Thrust Strength
            if (_isAttacking)
            {
                _thrustPercent = 1d;
            }
            else
            {
                _thrustPercent = .5d;
            }
        }

        private void Brain_CenterFlock_AvoidNeighbors()
        {
            // Calculate the center of position of the flock
            //TODO:  Have a vision limit

            #region Direction to center flock

            Vector3D centerPosition = new Vector3D(0, 0, 0);
            foreach (SwarmBot1 bot in _otherBots)
            {
                centerPosition += bot.PhysicsBody.PositionToWorld(bot.PhysicsBody.CenterOfMass).ToVector();
            }

            if (_otherBots.Count > 0)       // can't divide by zero
            {
                centerPosition /= _otherBots.Count;
            }

            // Figure out what direction to go in local coords
            //Vector3D flockCenterDirection = StraightToTarget_VelocityAware1Worker(centerPosition.ToPoint());
            Vector3D flockCenterDirection = StraightToTargetWorker(centerPosition.ToPoint());

            #endregion

            // Get a cumulative vector that tells how to avoid others
            Vector3D avoidDirection = AvoidWorker();




            // For now, I will just normalize the two and add them together with equal weight
            //NOTE:  Normalizing gives bad results.  The avoid already has a priority magnitude, normalizing wipes it
            //flockCenterDirection.Normalize();
            //avoidDirection.Normalize();

            flockCenterDirection /= 2;

            Vector3D directionToGo = flockCenterDirection + avoidDirection;





            // Now that I know where to go, rotate the original thruster direction (0,1,0) to line up with the desired direction
            Vector3D axis;
            double radians;
            Math3D.GetRotation(out axis, out radians, _origThrustDirection, directionToGo);

            // Thrust Direction
            _thrustTransform = new RotateTransform3D(new AxisAngleRotation3D(axis, Math1D.RadiansToDegrees(radians)));

            // Thrust Strength
            if (_isAttacking)
            {
                _thrustPercent = 1d;
            }
            else
            {
                _thrustPercent = .5d;
            }
        }
        private Vector3D AvoidWorker()
        {
            Vector3D retVal = new Vector3D(0, 0, 0);

            Vector3D myPositionWorld = this.PhysicsBody.PositionToWorld(this.PhysicsBody.CenterOfMass).ToVector();       // center mass should always be at zero, but I want to be consistent

            foreach (SwarmBot1 bot in _otherBots)
            {
                // Get its position relative to me
                Vector3D botPositionWorld = bot.PhysicsBody.PositionToWorld(bot.PhysicsBody.CenterOfMass).ToVector();

                Vector3D offsetLocal = this.PhysicsBody.DirectionFromWorld(botPositionWorld - myPositionWorld);

                // I need to invert this, because the greater the distance, the less the influence
                double offsetLength = offsetLocal.Length;
                if (Math1D.IsNearZero(offsetLength))
                {
                    // Can't divide by zero.  For now, I'll just skip this bot
                    continue;
                }

                double awayForce = 1 / offsetLength;

                offsetLocal /= offsetLength;    // normalize
                offsetLocal *= -awayForce;    // point away from the bot, with the away force strength

                // Now add this to the return vector
                retVal += offsetLocal;
            }

            // Exit Function
            return retVal;
        }

        private void Brain_CenterFlock_AvoidNeighbors_FlockVelocity()
        {
            // Calculate the center of position of the flock
            //TODO:  Have a vision limit

            #region Direction to center flock

            Vector3D centerPosition = new Vector3D(0, 0, 0);
            foreach (SwarmBot1 bot in _otherBots)
            {
                centerPosition += bot.PhysicsBody.PositionToWorld(bot.PhysicsBody.CenterOfMass).ToVector();
            }

            if (_otherBots.Count > 0)       // can't divide by zero
            {
                centerPosition /= _otherBots.Count;
            }

            // Figure out what direction to go in local coords
            Vector3D flockCenterDirection = StraightToTarget_VelocityAware1Worker(centerPosition.ToPoint());
            //Vector3D flockCenterDirection = StraightToTargetWorker(centerPosition.ToPoint());

            #endregion

            // Get a cumulative vector that tells how to avoid others
            Vector3D avoidDirection = AvoidWorker();

            // Get the average flock velocity
            Vector3D commonVelocityDirection = MatchVelocityWorker();




            // For now, I will just normalize the two and add them together with equal weight
            //NOTE:  Normalizing gives bad results.  The avoid already has a priority magnitude, normalizing wipes it
            //flockCenterDirection.Normalize();
            //avoidDirection.Normalize();

            flockCenterDirection /= 2;

            commonVelocityDirection.Normalize();

            Vector3D directionToGo = flockCenterDirection + avoidDirection + commonVelocityDirection;





            // Now that I know where to go, rotate the original thruster direction (0,1,0) to line up with the desired direction
            Vector3D axis;
            double radians;
            Math3D.GetRotation(out axis, out radians, _origThrustDirection, directionToGo);

            // Thrust Direction
            _thrustTransform = new RotateTransform3D(new AxisAngleRotation3D(axis, Math1D.RadiansToDegrees(radians)));

            // Thrust Strength
            if (_isAttacking)
            {
                _thrustPercent = 1d;
            }
            else
            {
                _thrustPercent = .5d;
            }
        }
        private Vector3D MatchVelocityWorker()
        {
            Vector3D retVal = new Vector3D(0, 0, 0);

            foreach (SwarmBot1 bot in _otherBots)
            {
                retVal += bot.VelocityWorld;
            }

            // The velocities were in world coords.  Change to local
            retVal = this.PhysicsBody.DirectionFromWorld(retVal);

            // Exit Function
            return retVal;
        }

        private void Brain_Flocking_ChasePoint()
        {
            // Calculate the center of position of the flock
            //TODO:  Have a vision limit

            #region Direction to center flock

            Vector3D centerPosition = new Vector3D(0, 0, 0);
            foreach (SwarmBot1 bot in _otherBots)
            {
                centerPosition += bot.PhysicsBody.PositionToWorld(bot.PhysicsBody.CenterOfMass).ToVector();
            }

            if (_otherBots.Count > 0)       // can't divide by zero
            {
                centerPosition /= _otherBots.Count;
            }

            // Figure out what direction to go in local coords
            Vector3D flockCenterDirection = StraightToTarget_VelocityAware1Worker(centerPosition.ToPoint());
            //Vector3D flockCenterDirection = StraightToTargetWorker(centerPosition.ToPoint());

            #endregion

            // Get a cumulative vector that tells how to avoid others
            Vector3D avoidDirection = AvoidWorker();

            // Get the average flock velocity
            Vector3D commonVelocityDirection = MatchVelocityWorker();

            // Go toward the chase point
            Vector3D chasePointDirection = StraightToTarget_VelocityAware1Worker(_chasePoint);



            // For now, I will just normalize the two and add them together with equal weight
            //NOTE:  Normalizing gives bad results.  The avoid already has a priority magnitude, normalizing wipes it
            //flockCenterDirection.Normalize();
            //avoidDirection.Normalize();

            flockCenterDirection /= 2;

            commonVelocityDirection.Normalize();

            chasePointDirection /= (chasePointDirection.Length / 2);

            Vector3D directionToGo = flockCenterDirection + avoidDirection + commonVelocityDirection + chasePointDirection;





            // Now that I know where to go, rotate the original thruster direction (0,1,0) to line up with the desired direction
            Vector3D axis;
            double radians;
            Math3D.GetRotation(out axis, out radians, _origThrustDirection, directionToGo);

            // Thrust Direction
            _thrustTransform = new RotateTransform3D(new AxisAngleRotation3D(axis, Math1D.RadiansToDegrees(radians)));

            // Thrust Strength
            if (_isAttacking)
            {
                _thrustPercent = 1d;
            }
            else
            {
                _thrustPercent = .5d;
            }
        }

        #endregion
    }
}
