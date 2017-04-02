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
using Game.HelperClassesCore;

namespace Game.Newt.v1.AsteroidMiner1
{
    public class SwarmBotBase : IMapObject
    {
        #region Declaration Section

        private const double PI_DIV_TWO = Math.PI * .5d;		// 90 degrees
        private const double PI_DIV_FOUR = Math.PI * .25d;		// 45 degrees
        private const double RADIANS_60DEG = 1.0471975511966d;
        private const double SQRT_TWO = 1.4142135623731d;

        private Random _rand = new Random();

        private SharedVisuals _sharedVisuals = null;

        // These are visual models that don't count toward collisions.  But, once per frame, they need to be transformed to stay with the ship
        private ModelVisual3D _core = null;
        private GeometryModel3D _coreGeometry = null;
        private MaterialGroup _coreMaterialNeutral = null;
        private DiffuseMaterial _coreMaterialNeutralColor = null;       // this is the one to change the color
        private MaterialGroup _coreMaterialAttack = null;
        private PointLight _lightAttack = null;

        private PointVisualizer _pointVisualizer = null;
        private PointVisualizer _pointVisualizer_VelAware2 = null;
        private PointVisualizer _pointVisualizer_VelAware3 = null;

        #endregion

        #region IMapObject Members

        private ConvexBody3D _physicsBody = null;
        public ConvexBody3D PhysicsBody
        {
            get
            {
                return _physicsBody;
            }
        }

        private List<ModelVisual3D> _visuals = null;
        public IEnumerable<ModelVisual3D> Visuals3D
        {
            get
            {
                return _visuals;
            }
        }

        public Point3D PositionWorld
        {
            get
            {
                return _physicsBody.PositionToWorld(_physicsBody.CenterOfMass);
            }
        }

        public Vector3D VelocityWorld
        {
            get
            {
                //return _physicsBody.Velocity;
                return _physicsBody.VelocityCached;		// this one is safer (can be called at any time, not just within the apply force/torque event)
            }
        }

        private double _radius = 1d;
        //TODO:  Support setting size at any time
        public double Radius
        {
            get
            {
                return _radius;
            }
            set
            {
                if (_physicsBody != null)
                {
                    throw new InvalidOperationException("Can't set the radius after the bot has been created");
                }

                _radius = value;
            }
        }

        #endregion

        #region Public Properties

        //TODO:  Support setting mass
        private double _mass = 1d;
        public double Mass
        {
            get
            {
                return _mass;
            }
            set
            {
                if (_physicsBody != null)
                {
                    throw new InvalidOperationException("Can't set the mass after the bot has been created");
                }

                _mass = value;
            }
        }

        private double _thrustForce = 6d;
        public double ThrustForce
        {
            get
            {
                return _thrustForce;
            }
            set
            {
                if (_physicsBody != null)
                {
                    throw new InvalidOperationException("Can't set the thrust after the bot has been created");
                }

                _thrustForce = value;
            }
        }

        private Point3D _chasePoint = new Point3D(0, 0, 0);
        /// <summary>
        /// This is in world coords
        /// </summary>
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

        private Vector3D _chasePointVelocity = new Vector3D(0, 0, 0);
        /// <summary>
        /// This is in world coords
        /// </summary>
        public Vector3D ChasePointVelocity
        {
            get
            {
                return _chasePointVelocity;
            }
            set
            {
                _chasePointVelocity = value;
            }
        }

        private List<SwarmBotBase> _otherBots = new List<SwarmBotBase>();
        /// <summary>
        /// These are the other bots that belong to this swarm
        /// </summary>
        public List<SwarmBotBase> OtherBots
        {
            get
            {
                return _otherBots;
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

                if (!_shouldDrawThrustLine && _thruster != null && _thruster.IsFiring)
                {
                    // By simply not calling _thruster.DrawVisual, the thrust line would still be visible.  So by turning off the thruster, it will
                    // quit drawing the thrust line, and wait for DrawVisual to be called again (hackish, but it works)
                    _thruster.IsFiring = false;
                    _thruster.IsFiring = true;
                }
            }
        }

        private bool _shouldShowDebugVisuals = false;
        public bool ShouldShowDebugVisuals
        {
            get
            {
                return _shouldShowDebugVisuals;
            }
            set
            {
                if (_shouldShowDebugVisuals == value)
                {
                    return;
                }

                _shouldShowDebugVisuals = value;

                if (_pointVisualizer != null)
                {
                    _pointVisualizer.ShowPosition = false;
                    _pointVisualizer.ShowVelocity = _shouldShowDebugVisuals;
                    _pointVisualizer.ShowAcceleration = _shouldShowDebugVisuals && Ship.DEBUGSHOWSACCELERATION;
                }

                if (!_shouldShowDebugVisuals && _pointVisualizer_VelAware2 != null)
                {
                    _pointVisualizer_VelAware2.HideAll();
                }

                if (!_shouldShowDebugVisuals && _pointVisualizer_VelAware3 != null)
                {
                    _pointVisualizer_VelAware3.HideAll();
                }
            }
        }

        private double ThrustLineStandardLength
        {
            get
            {
                return _radius * 4;
            }
        }

        private double _thrustLineMultiplier = 1d;
        /// <summary>
        /// This is just the visual, you can make the thrust line longer to make it easier to see
        /// </summary>
        public double ThrustLineMultiplier
        {
            get
            {
                return _thrustLineMultiplier;
            }
            set
            {
                _thrustLineMultiplier = value;

                if (_thruster != null)
                {
                    _thruster.LineMaxLength = this.ThrustLineStandardLength * _thrustLineMultiplier;
                }
            }
        }

        private bool _useLighting = true;
        public bool UseLighting
        {
            get
            {
                return _useLighting;
            }
            set
            {
                _useLighting = value;
            }
        }

        private Color _coreColor = Colors.DimGray;
        /// <summary>
        /// This is only used when not attacking (because attacking uses a specially colored core)
        /// </summary>
        public Color CoreColor
        {
            get
            {
                return _coreColor;
            }
            set
            {
                _coreColor = value;

                if (_coreMaterialNeutralColor != null)       // if it's still null, then it will take this color when the bot is created
                {
                    _coreMaterialNeutralColor.Brush = new SolidColorBrush(_coreColor);
                }
            }
        }

        #endregion
        #region Protected Properties

        private World _world = null;
        protected World World
        {
            get
            {
                return _world;
            }
        }

        private Viewport3D _viewport = null;
        protected Viewport3D Viewport
        {
            get
            {
                return _viewport;
            }
        }

        private bool _isAttacking = false;
        protected bool IsAttacking
        {
            get
            {
                return _isAttacking;
            }
            set
            {
                _isAttacking = value;

                if (_core != null)
                {
                    if (_isAttacking)
                    {
                        #region Show attacking core

                        _coreGeometry.Material = _coreMaterialAttack;
                        _coreGeometry.BackMaterial = _coreMaterialAttack;

                        if (_useLighting)
                        {
                            Model3DGroup group = new Model3DGroup();
                            group.Children.Add(_coreGeometry);
                            group.Children.Add(_lightAttack);

                            _core.Content = group;
                        }
                        else
                        {
                            _core.Content = _coreGeometry;
                        }

                        #endregion
                    }
                    else
                    {
                        #region Show neutral core

                        _coreGeometry.Material = _coreMaterialNeutral;
                        _coreGeometry.BackMaterial = _coreMaterialNeutral;

                        _core.Content = _coreGeometry;      // no light

                        #endregion
                    }
                }
            }
        }

        // This is the thruster
        private ThrustLine _thruster = null;
        protected ThrustLine Thruster
        {
            get
            {
                return _thruster;
            }
        }
        private Vector3D _origThrustDirection;
        protected Vector3D OrigThrustDirection
        {
            get
            {
                return _origThrustDirection;
            }
        }

        // Once per frame, these are adjusted (they are instructions to the thruster)
        /// <summary>
        /// This tells how to rotate the thruster
        /// </summary>
        /// <remarks>
        /// This one is a local transform.  It will have to be added to the physics body's transform
        /// </remarks>
        private Transform3D _thrustTransform = null;
        protected Transform3D ThrustTransform
        {
            get
            {
                return _thrustTransform;
            }
            set
            {
                _thrustTransform = value;
            }
        }
        /// <summary>
        /// This is how hard to run the thruster
        /// </summary>
        private double _thrustPercent = 1d;
        protected double ThrustPercent
        {
            get
            {
                return _thrustPercent;
            }
            set
            {
                _thrustPercent = value;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// The derived class should modify the thruster settings before calling base.WorldUpdating
        /// </summary>
        public virtual void WorldUpdating()
        {
            #region Visuals

            _core.Transform = _physicsBody.Transform;

            #endregion

            #region Thrust Line

            if (_shouldDrawThrustLine)
            {
                _thruster.DrawVisual(_thrustPercent, _physicsBody.Transform, _thrustTransform);
            }

            #endregion

            #region Debug Visuals

            if (_shouldShowDebugVisuals)
            {
                if (_pointVisualizer == null)
                {
                    _pointVisualizer = new PointVisualizer(_viewport, _sharedVisuals, _physicsBody);
                    _pointVisualizer.ShowPosition = false;
                    _pointVisualizer.ShowVelocity = true;
                    _pointVisualizer.ShowAcceleration = Ship.DEBUGSHOWSACCELERATION;
                    _pointVisualizer.VelocityAccelerationLengthMultiplier = .05d;		// this is the same as the ship's
                }

                _pointVisualizer.Update();
            }

            #endregion
        }

        #endregion
        #region Protected Methods

        /// <summary>
        /// Set the properties, then call create
        /// NOTE:  This adds itself to the viewport and world.  In the future, that should be handled by the caller
        /// </summary>
        protected void CreateBot(Viewport3D viewport, SharedVisuals sharedVisuals, World world, Point3D worldPosition)
        {
            _viewport = viewport;
            _sharedVisuals = sharedVisuals;
            _world = world;

            // Thruster
            _origThrustDirection = new Vector3D(0, _thrustForce, 0);
            _thruster = new ThrustLine(_viewport, sharedVisuals, _origThrustDirection, new Vector3D(0, 0, 0));
            _thruster.LineMaxLength = this.ThrustLineStandardLength * _thrustLineMultiplier;

            MaterialGroup material = null;
            GeometryModel3D geometry = null;
            ModelVisual3D model = null;

            _visuals = new List<ModelVisual3D>();

            #region Interior Extra Visuals

            // These are visuals that will stay oriented to the ship, but don't count in collision calculations

            #region Core

            // Neutral
            _coreMaterialNeutral = new MaterialGroup();
            _coreMaterialNeutralColor = new DiffuseMaterial(new SolidColorBrush(_coreColor));
            _coreMaterialNeutral.Children.Add(_coreMaterialNeutralColor);
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

            _visuals.Add(_core);

            // Add to the viewport
            _viewport.Children.Add(_core);

            #endregion

            #endregion

            #region Glass Shell

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

            _visuals.Add(model);

            // Add to the viewport
            _viewport.Children.Add(model);

            #endregion
            #region Physics Body

            // Make a physics body that represents this shape
            _physicsBody = new ConvexBody3D(world, model);

            //NOTE:  Not setting material _physicsBody.MaterialGroupID, so it takes the default material

            _physicsBody.NewtonBody.UserData = this;

            _physicsBody.Mass = Convert.ToSingle(this.Mass);

            _physicsBody.LinearDamping = .01f;
            _physicsBody.AngularDamping = new Vector3D(10f, 10f, 10f);   // fairly heavy damping (the bot doesn't try to cancel its spin, so I'll let Newt do it for me)

            _physicsBody.ApplyForce += new BodyForceEventHandler(Body_ApplyForce);

            #endregion

            #region Exterior Extra Visuals

            // There is a bug in WPF where visuals added after a semitransparent one won't show inside.  So if you want to add exterior
            // bits that aren't visible inside, this would be the place

            #endregion

            _thruster.IsFiring = true;

            // Show the proper core
            this.IsAttacking = _isAttacking;
        }



        // These take in params in world coords, but the vector returned is in local coords
        // These will return the vectors with a magnitude from 0 to 1, but will go up to 10 if things get urgent
        /// <summary>
        /// Just the vector from here to the point passed in
        /// </summary>
        protected Vector3D GetDirection_StraightToTarget(Point3D targetPointWorld)
        {
            // Convert everything to local coords
            Point3D position = new Point3D(0, 0, 0);
            Point3D targetPointLocal = _physicsBody.PositionFromWorld(targetPointWorld);

            // Always return a length of 1
            Vector3D retVal = targetPointLocal.ToVector() - position.ToVector();
            retVal.Normalize();

            // Exit Function
            return retVal;
        }
        /// <summary>
        /// This will go for the target point, but tries to get my velocity going straight to the target (tries to counter any velocity
        /// that is orthoganal to the target point)
        /// </summary>
        protected Vector3D GetDirection_StraightToTarget_VelocityAware1(Point3D targetPointWorld)
        {
            // Convert everything to local coords
            Point3D position = _physicsBody.CenterOfMass;
            Point3D targetPointLocal = _physicsBody.PositionFromWorld(targetPointWorld);

            Vector3D directionToGo = targetPointLocal.ToVector() - position.ToVector();

            Vector3D axis;
            double radians;

            #region Adjust for current velocity

            Vector3D currentVelocity = this.VelocityWorld;

            if (!Math1D.IsNearZero(currentVelocity.LengthSquared))
            {
                currentVelocity = _physicsBody.DirectionFromWorld(currentVelocity);

                Quaternion rotation = Math3D.GetRotation(directionToGo, currentVelocity);

                // This is how much to rotate direction to align with current velocity, I want to go against the current velocity (if aligned,
                // the angle will be zero, so negating won't make a difference)
                rotation = rotation.ToReverse();
                double angle = rotation.Angle;

                if (rotation.IsIdentity || angle.IsNearZero() || angle.IsNearValue(90))
                {
                    // No modification needed
                }
                else if (Math.Abs(angle) < 90)
                {
                    // Change the direction by the angle
                    directionToGo = directionToGo.GetRotatedVector(rotation.Axis, angle);
                }
                else if (Math.Abs(angle) >= 90)
                {
                    // The velocity is more than 90 degrees from where I want to go.  By applying a force exactly opposite of the current
                    // velocity, I will negate the velocity that orthogonal to the desired direction, as well as slow down the bot
                    directionToGo = currentVelocity * -1d;
                }
            }

            #endregion

            directionToGo.Normalize();

            // Exit Function
            return directionToGo;
        }
        /// <summary>
        /// This one won't let the bot come at the target too fast
        /// </summary>
        protected Vector3D GetDirection_StraightToTarget_VelocityAware2(Point3D targetPointWorld, double maxAcceleration)
        {
            // Convert everything to local coords
            Point3D position = _physicsBody.CenterOfMass;
            Point3D targetPointLocal = _physicsBody.PositionFromWorld(targetPointWorld);

            Vector3D directionToGo = targetPointLocal.ToVector() - position.ToVector();
            double distanceToGo = directionToGo.Length;

            if (Math3D.IsNearZero(directionToGo))
            {
                // Just sit here
                return new Vector3D(0, 0, 0);
            }

            Vector3D currentVelocity = this.VelocityWorld;

            if (Math1D.IsNearZero(currentVelocity.LengthSquared))
            {
                #region Currently Stopped

                if (Math1D.IsNearZero(directionToGo.LengthSquared))
                {
                    // Already sitting on the target.  STAY!!!
                    return new Vector3D(0, 0, 0);
                }
                else
                {
                    // I'm currently stopped.  Gun it straight toward the target
                    return directionToGo.ToUnit();
                }

                #endregion
            }

            #region Adjust for current velocity

            // I think this description is too complex

            // I need to a few things:
            //     Figure out how fast I can be approaching the point, and still be able to stop on it
            //         if too fast, negate the velocity that is along the direction of the target
            //
            //     If there is left over thrust, remove as much velocity as possible that is not in the direction to the target
            //
            //     If there is left over thrust, use the remainder to speed up (unless I'm already at the max velocity, then just coast)


            // Turn this into local coords
            currentVelocity = _physicsBody.DirectionFromWorld(currentVelocity);

            Vector3D currentVelocityAlongDirectionLine = currentVelocity.GetProjectedVector(directionToGo);
            double currentSpeed = currentVelocityAlongDirectionLine.Length;

            // If going away from the target, then there is no need to slow down
            double distanceToStop = -1d;
            if (Vector3D.DotProduct(currentVelocityAlongDirectionLine, directionToGo) > 0)
            {
                // Calculate max velocity
                // t = (v1 - v0) / a
                // v1 will need to be zero, v0 is the current velocity, a is negative.  so I can just take initial velocity / acceleration
                double timeToStop = currentSpeed / maxAcceleration;

                // Now that I know how long it will take to stop, figure out how far I'll go in that time under constant acceleration
                // d = vt + 1/2 at^2
                // I'll let the vt be zero and acceleration be positive to make things cleaner
                distanceToStop = .5 * maxAcceleration * timeToStop * timeToStop;
            }

            if (distanceToStop > distanceToGo)
            {
                #region Brakes directly away from the target

                //TODO:  Take in the magnitude to use

                //directionToGo *= -3d;   // I'll give this a higher than normal priority (I still want to keep the highest priority with obstacle avoidance)
                //directionToGo *= -.25d;

                // If the difference between stop distance and distance to go is very small, then don't use full thrust.  It causes the bot to jitter back and forth
                double brakesPercent = GetEdgePercentAcceleration(distanceToStop - distanceToGo, maxAcceleration);

                // Reverse the thrust
                directionToGo.Normalize();
                directionToGo *= -1d * brakesPercent;

                #endregion
            }
            else
            {
                #region Accelerate toward target (negating tangent speed)

                // Figure out how fast I could safely be going toward the target
                // If I accelerate full force, will I exceed that?  (who cares, speed up, and let the next frame handle that)

                Quaternion rotation = Math3D.GetRotation(directionToGo, currentVelocity);

                // This is how much to rotate direction to align with current velocity, I want to go against the current velocity (if aligned,
                // the angle will be zero, so negating won't make a difference)
                rotation = rotation.ToReverse();
                double radians = Math1D.DegreesToRadians(rotation.Angle);

                //if (Math3D.IsNearZero(radians) || Math3D.IsNearValue(radians, PI_DIV_TWO) || Math3D.IsNearValue(radians, Math.PI))
                //{
                //    // No modification needed (I don't think this if statement is really needed)
                //}

                if (Math1D.IsNearValue(radians, Math.PI))
                {
                    // Flying directly away from the target.  Turn around
                    directionToGo = currentVelocity * -1d;
                }
                else if (Math.Abs(radians) < RADIANS_60DEG)		// PI_DIV_TWO is waiting too long, PI_DIV_FOUR is capping off too early
                {
                    // Change the direction by the angle
                    directionToGo = directionToGo.GetRotatedVector(rotation.Axis, Math1D.RadiansToDegrees(radians));
                }
                //else if (Math.Abs(radians) >= PI_DIV_TWO)
                //{
                //    // The velocity is more than 90 degrees from where I want to go.  By applying a force exactly opposite of the current
                //    // velocity, I will negate the velocity that's orthogonal to the desired direction, as well as slow down the bot
                //
                //    // I used to stop in this case.  It is most efficient, but looks really odd (I want the swarmbot to flow more instead of stalling out,
                //    // and then charging off).  So I don't rotate more than 60 degrees now
                //    directionToGo = currentVelocity * -1d;
                //}
                else
                {
                    double actualRadians = RADIANS_60DEG;
                    if (radians < 0)
                    {
                        actualRadians *= -1d;
                    }

                    directionToGo = directionToGo.GetRotatedVector(rotation.Axis, Math1D.RadiansToDegrees(actualRadians));
                }

                // Don't jitter back and forth at full throttle when at the boundry
                double percentThrust = GetEdgePercentAcceleration(distanceToGo - distanceToStop, maxAcceleration);

                directionToGo.Normalize();
                directionToGo *= percentThrust;

                #endregion
            }

            #endregion

            // Exit Function
            return directionToGo;
        }
        protected Vector3D GetDirection_StraightToTarget_VelocityAware2_DebugVisuals(Point3D targetPointWorld, double maxAcceleration)
        {
            if (_shouldShowDebugVisuals && _pointVisualizer_VelAware2 == null)
            {
                #region Visualizer

                // This method has it's own visualizer

                _pointVisualizer_VelAware2 = new PointVisualizer(_viewport, _sharedVisuals, 8);

                _pointVisualizer_VelAware2.SetCustomItemProperties(0, true, Colors.HotPink, .03d);		// chase point
                _pointVisualizer_VelAware2.SetCustomItemProperties(1, false, Colors.Black, 1d);		// direction to go
                _pointVisualizer_VelAware2.SetCustomItemProperties(2, false, Colors.Red, 1d);		// the return vector away
                _pointVisualizer_VelAware2.SetCustomItemProperties(3, false, Colors.Green, .05d);		// velocity along direction to go (the other visualizer's velocity is scaled the same)
                _pointVisualizer_VelAware2.SetCustomItemProperties(4, false, Colors.DarkGoldenrod, .05d);		// this is my max acceleration
                _pointVisualizer_VelAware2.SetCustomItemProperties(5, false, Colors.Cyan, 1d);		// the return vector toward
                _pointVisualizer_VelAware2.SetCustomItemProperties(6, false, Colors.DarkOrchid, 1d);		// distance to stop
                _pointVisualizer_VelAware2.SetCustomItemProperties(7, false, Colors.DarkCyan, 1d);		// attempted turn direction

                #endregion
            }

            // clear off intermitent visuals (the ones that don't update every frame will appear to hang if I don't clean them up)
            if (_shouldShowDebugVisuals)
            {
                _pointVisualizer_VelAware2.Update(2, new Point3D(), new Vector3D());
                _pointVisualizer_VelAware2.Update(4, new Point3D(), new Vector3D());
                _pointVisualizer_VelAware2.Update(5, new Point3D(), new Vector3D());
                _pointVisualizer_VelAware2.Update(6, new Point3D(), new Vector3D());
                _pointVisualizer_VelAware2.Update(7, new Point3D(), new Vector3D());
            }

            Point3D positionWorld = _physicsBody.PositionToWorld(_physicsBody.CenterOfMass);

            if (_shouldShowDebugVisuals)
            {
                _pointVisualizer_VelAware2.Update(0, targetPointWorld);
                if (Ship.DEBUGSHOWSACCELERATION)
                {
                    _pointVisualizer_VelAware2.Update(4, positionWorld, _physicsBody.DirectionToWorld(new Vector3D(maxAcceleration, 0, 0)));
                }
            }

            // Convert everything to local coords
            Point3D position = _physicsBody.CenterOfMass;
            Point3D targetPointLocal = _physicsBody.PositionFromWorld(targetPointWorld);

            Vector3D directionToGo = targetPointLocal.ToVector() - position.ToVector();
            double distanceToGo = directionToGo.Length;

            if (_shouldShowDebugVisuals)
            {
                _pointVisualizer_VelAware2.Update(1, positionWorld, _physicsBody.DirectionToWorld(directionToGo));
            }

            if (Math3D.IsNearZero(directionToGo))
            {
                // Just sit here
                return new Vector3D(0, 0, 0);
            }

            Vector3D currentVelocity = this.VelocityWorld;

            if (Math1D.IsNearZero(currentVelocity.LengthSquared))
            {
                #region Currently Stopped

                if (Math1D.IsNearZero(directionToGo.LengthSquared))
                {
                    // Already sitting on the target.  STAY!!!
                    return new Vector3D(0, 0, 0);
                }
                else
                {
                    // I'm currently stopped.  Gun it straight toward the target
                    return directionToGo.ToUnit();
                }

                #endregion
            }

            #region Adjust for current velocity

            // I think this description is too complex

            // I need to a few things:
            //     Figure out how fast I can be approaching the point, and still be able to stop on it
            //         if too fast, negate the velocity that is along the direction of the target
            //
            //     If there is left over thrust, remove as much velocity as possible that is not in the direction to the target
            //
            //     If there is left over thrust, use the remainder to speed up (unless I'm already at the max velocity, then just coast)


            // Turn this into local coords
            currentVelocity = _physicsBody.DirectionFromWorld(currentVelocity);

            Vector3D currentVelocityAlongDirectionLine = currentVelocity.GetProjectedVector(directionToGo);

            if (_shouldShowDebugVisuals)
            {
                _pointVisualizer_VelAware2.Update(3, positionWorld, _physicsBody.DirectionToWorld(currentVelocityAlongDirectionLine));
            }

            double currentSpeed = currentVelocityAlongDirectionLine.Length;

            // If going away from the target, then there is no need to slow down
            double distanceToStop = -1d;
            if (Vector3D.DotProduct(currentVelocityAlongDirectionLine, directionToGo) > 0)
            {
                // Calculate max velocity
                // t = (v1 - v0) / a
                // v1 will need to be zero, v0 is the current velocity, a is negative.  so I can just take initial velocity / acceleration
                double timeToStop = currentSpeed / maxAcceleration;

                // Now that I know how long it will take to stop, figure out how far I'll go in that time under constant acceleration
                // d = vt + 1/2 at^2
                // I'll let the vt be zero and acceleration be positive to make things cleaner
                distanceToStop = .5 * maxAcceleration * timeToStop * timeToStop;

                if (_shouldShowDebugVisuals)
                {
                    Vector3D vectToStop = directionToGo;
                    vectToStop.Normalize();
                    vectToStop *= distanceToStop;
                    _pointVisualizer_VelAware2.Update(6, positionWorld, _physicsBody.DirectionToWorld(vectToStop));
                }
            }

            bool suppress5 = false;

            if (distanceToStop > distanceToGo)
            {
                #region Brakes directly away from the target

                //TODO:  Take in the magnitude to use

                //directionToGo *= -3d;   // I'll give this a higher than normal priority (I still want to keep the highest priority with obstacle avoidance)
                //directionToGo *= -.25d;

                // If the difference between stop distance and distance to go is very small, then don't use full thrust.  It causes the bot to jitter back and forth
                double brakesPercent = GetEdgePercentAcceleration(distanceToStop - distanceToGo, maxAcceleration);

                // Reverse the thrust
                directionToGo.Normalize();
                directionToGo *= -1d * brakesPercent;

                if (_shouldShowDebugVisuals)
                {
                    suppress5 = true;
                    _pointVisualizer_VelAware2.Update(2, positionWorld, _physicsBody.DirectionToWorld(directionToGo));
                }

                #endregion
            }
            else
            {
                #region Accelrate toward target (negating tangent speed)

                // Figure out how fast I could safely be going toward the target
                // If I accelerate full force, will I exceed that?  (who cares, speed up, and let the next frame handle that)

                Quaternion rotation = Math3D.GetRotation(directionToGo, currentVelocity);

                // This is how much to rotate direction to align with current velocity, I want to go against the current velocity (if aligned,
                // the angle will be zero, so negating won't make a difference)
                rotation = rotation.ToReverse();
                double radians = Math1D.DegreesToRadians(rotation.Angle);

                //if (Math3D.IsNearZero(radians) || Math3D.IsNearValue(radians, PI_DIV_TWO) || Math3D.IsNearValue(radians, Math.PI))
                //{
                //    // No modification needed (I don't think this if statement is really needed)
                //}

                if (Math1D.IsNearValue(radians, Math.PI))
                {
                    // Flying directly away from the target.  Turn around
                    directionToGo = currentVelocity * -1d;
                }
                //else if (Math.Abs(radians) < PI_DIV_TWO)
                //else if (Math.Abs(radians) < PI_DIV_FOUR)		// between 45 and 90 gets too extreme
                else if (Math.Abs(radians) < RADIANS_60DEG)
                {
                    // Change the direction by the angle
                    directionToGo = directionToGo.GetRotatedVector(rotation.Axis, Math1D.RadiansToDegrees(radians));
                }
                else
                {
                    if (_shouldShowDebugVisuals)
                    {
                        Vector3D vectTryTurn = directionToGo.GetRotatedVector(rotation.Axis, Math1D.RadiansToDegrees(radians));
                        vectTryTurn.Normalize();
                        _pointVisualizer_VelAware2.Update(7, positionWorld, _physicsBody.DirectionToWorld(vectTryTurn));
                    }

                    //double actualRadians = PI_DIV_FOUR;
                    double actualRadians = RADIANS_60DEG;
                    if (radians < 0)
                    {
                        actualRadians *= -1d;
                    }

                    directionToGo = directionToGo.GetRotatedVector(rotation.Axis, Math1D.RadiansToDegrees(actualRadians));
                }


                //else if (Math.Abs(radians) >= PI_DIV_TWO)
                //{
                //    // I used to stop in this case.  It is most efficient, but looks really odd (I want the swarmbot to flow more instead of stalling out,
                //    // and then charging off).  So I'll try rotating by 90 degrees

                //    if (_shouldShowDebugVisuals)
                //    {
                //        Vector3D vectTryTurn = directionToGo.GetRotatedVector(axis, Math3D.RadiansToDegrees(radians));
                //        vectTryTurn.Normalize();
                //        _pointVisualizer_VelAware2.Update(7, positionWorld, _physicsBody.DirectionToWorld(vectTryTurn));

                //    }


                //    directionToGo = currentVelocity * -1d;

                //}


                //else if (Math.Abs(radians) >= PI_DIV_TWO)
                //{
                //    // The velocity is more than 90 degrees from where I want to go.  By applying a force exactly opposite of the current
                //    // velocity, I will negate the velocity that orthogonal to the desired direction, as well as slow down the bot

                //    //TODO:  This gives an unnatural flight.  Don't fully stop before charging toward the chase point

                //    directionToGo = currentVelocity * -1d;
                //}


                double percentThrust = GetEdgePercentAcceleration(distanceToGo - distanceToStop, maxAcceleration);

                //TODO:  Do partial thrust similar to the fly away logic

                directionToGo.Normalize();
                directionToGo *= percentThrust;

                #endregion
            }

            #endregion

            if (_shouldShowDebugVisuals && !suppress5)
            {
                _pointVisualizer_VelAware2.Update(5, positionWorld, _physicsBody.DirectionToWorld(directionToGo));
            }

            // Exit Function
            return directionToGo;
        }

        /// <summary>
        /// This goes straight for the chase point, and tries to be going at the target's velocity when it's at the target point
        /// </summary>
        /// <remarks>
        /// The difference between this method and intercept is that this always goes straight for the point it's told to, fully
        /// turning if nessassary.  Intercept tries to calculate the best way to intercept, possibly chasing after a derived point.
        /// </remarks>
        protected Vector3D GetDirection_StraightToTarget_VelocityAware2(Point3D targetPointWorld, Vector3D targetVelocityWorld, double maxAcceleration)
        {
            if (_shouldShowDebugVisuals && _pointVisualizer_VelAware2 == null)
            {
                #region Visualizer

                // This method has it's own visualizer

                _pointVisualizer_VelAware2 = new PointVisualizer(_viewport, _sharedVisuals, 4);

                _pointVisualizer_VelAware2.SetCustomItemProperties(0, true, Colors.HotPink, .03d);		// chase point
                _pointVisualizer_VelAware2.SetCustomItemProperties(1, false, Colors.Black, 1d);		// direction to go
                _pointVisualizer_VelAware2.SetCustomItemProperties(2, false, Colors.Green, .05d);		// velocity along direction to go (the other visualizer's velocity is scaled the same)
                _pointVisualizer_VelAware2.SetCustomItemProperties(3, false, Colors.DarkOrchid, 1d);		// distance to stop

                #endregion
            }

            if (_shouldShowDebugVisuals)
            {
                // clear intermittant lines
                _pointVisualizer_VelAware2.Update(3, new Point3D(), new Vector3D());

                _pointVisualizer_VelAware2.Update(0, targetPointWorld);
            }

            // Convert everything to local coords
            Point3D position = _physicsBody.CenterOfMass;
            Point3D targetPointLocal = _physicsBody.PositionFromWorld(targetPointWorld);
            Vector3D targetVelocityLocal = _physicsBody.DirectionFromWorld(targetVelocityWorld);

            Vector3D directionToGo = targetPointLocal.ToVector() - position.ToVector();
            double distanceToGo = directionToGo.Length;

            Vector3D currentVelocity = _physicsBody.DirectionFromWorld(this.VelocityWorld);

            if (_shouldShowDebugVisuals)
            {
                _pointVisualizer_VelAware2.Update(1, PositionWorld, _physicsBody.DirectionToWorld(directionToGo));
            }

            if (Math3D.IsNearZero(directionToGo) && Math3D.IsNearZero(currentVelocity - targetVelocityLocal))
            {
                // Already on the point, moving along the same vector
                return new Vector3D(0, 0, 0);
            }

            // I think this description is too complex

            // I need to a few things:
            //     Figure out how fast I can be approaching the point, and still be able to stop on it
            //         if too fast, negate the velocity that is along the direction of the target
            //
            //     If there is left over thrust, remove as much velocity as possible that is not in the direction to the target
            //
            //     If there is left over thrust, use the remainder to speed up (unless I'm already at the max velocity, then just coast)



            //directionToGo += targetVelocityLocal;		// try adding the two, and see if that's good - this doesn't work, they units are far too different


            Vector3D currentVelocityAlongDirectionLine = currentVelocity.GetProjectedVector(directionToGo);
            double currentSpeedAlongDirectionLine = currentVelocityAlongDirectionLine.Length;

            //Vector3D currentVelocityAlongTargetVelocity = currentVelocity.GetProjectedVector(targetVelocityLocal);		// I don't see how this helps
            //double currentSpeedAlongTargetVelocity = currentVelocityAlongTargetVelocity.Length;

            Vector3D targetVelocityAlongDirectionLine = targetVelocityLocal.GetProjectedVector(directionToGo);
            double targetSpeedAlongDirectionLine = targetVelocityAlongDirectionLine.Length;


            if (_shouldShowDebugVisuals)
            {
                _pointVisualizer_VelAware2.Update(2, PositionWorld, _physicsBody.DirectionToWorld(currentVelocityAlongDirectionLine));
            }


            // If going away from the target, then there is no need to slow down
            double distanceToStop = -1d;
            if (Vector3D.DotProduct(currentVelocityAlongDirectionLine, directionToGo) > 0)
            {
                // Calculate max velocity
                // t = (v1 - v0) / a
                // v1 will need to be zero, v0 is the current velocity, a is negative.  so I can just take initial velocity / acceleration
                double timeToStop = currentSpeedAlongDirectionLine / maxAcceleration;

                // Now including the portion of the target's velocity along the direction
                //double timeToStop = (currentSpeedAlongDirectionLine + targetSpeedAlongDirectionLine) / maxAcceleration;		// this fails pretty badly too (try taking the greater of the two speeds?)

                // Now that I know how long it will take to stop, figure out how far I'll go in that time under constant acceleration
                // d = vt + 1/2 at^2
                // I'll let the vt be zero and acceleration be positive to make things cleaner
                distanceToStop = .5 * maxAcceleration * timeToStop * timeToStop;

                if (_shouldShowDebugVisuals)
                {
                    Vector3D vectToStop = directionToGo;
                    vectToStop.Normalize();
                    vectToStop *= distanceToStop;
                    _pointVisualizer_VelAware2.Update(3, PositionWorld, _physicsBody.DirectionToWorld(vectToStop));
                }
            }


            // distance and time to stop are to get stopped.  Now I have to leave some leeway for target velocity???


            if (distanceToStop > distanceToGo)
            {
                #region Brakes directly away from the target

                //TODO:  Take in the magnitude to use

                //directionToGo *= -3d;   // I'll give this a higher than normal priority (I still want to keep the highest priority with obstacle avoidance)
                //directionToGo *= -.25d;

                // If the difference between stop distance and distance to go is very small, then don't use full thrust.  It causes the bot to jitter back and forth
                double brakesPercent = GetEdgePercentAcceleration(distanceToStop - distanceToGo, maxAcceleration);

                // Reverse the thrust
                directionToGo.Normalize();
                directionToGo *= -1d * brakesPercent;

                #endregion
            }
            else
            {
                #region Accelrate toward target (negating tangent speed)

                // Figure out how fast I could safely be going toward the target
                // If I accelerate full force, will I exceed that?  (who cares, speed up, and let the next frame handle that)

                Quaternion rotation = Math3D.GetRotation(directionToGo, currentVelocity);

                // This is how much to rotate direction to align with current velocity, I want to go against the current velocity (if aligned,
                // the angle will be zero, so negating won't make a difference)
                rotation = rotation.ToReverse();
                double radians = Math1D.DegreesToRadians(rotation.Angle);

                //if (Math3D.IsNearZero(radians) || Math3D.IsNearValue(radians, PI_DIV_TWO) || Math3D.IsNearValue(radians, Math.PI))
                //{
                //    // No modification needed (I don't think this if statement is really needed)
                //}

                if (Math1D.IsNearValue(radians, Math.PI))
                {
                    // Flying directly away from the target.  Turn around
                    directionToGo = currentVelocity * -1d;
                }
                else if (Math.Abs(radians) < RADIANS_60DEG)		// PI_DIV_TWO is waiting too long, PI_DIV_FOUR is capping off too early
                {
                    // Change the direction by the angle
                    directionToGo = directionToGo.GetRotatedVector(rotation.Axis, Math1D.RadiansToDegrees(radians));
                }
                //else if (Math.Abs(radians) >= PI_DIV_TWO)
                //{
                //    // The velocity is more than 90 degrees from where I want to go.  By applying a force exactly opposite of the current
                //    // velocity, I will negate the velocity that's orthogonal to the desired direction, as well as slow down the bot
                //
                //    // I used to stop in this case.  It is most efficient, but looks really odd (I want the swarmbot to flow more instead of stalling out,
                //    // and then charging off).  So I don't rotate more than 60 degrees now
                //    directionToGo = currentVelocity * -1d;
                //}
                else
                {
                    double actualRadians = RADIANS_60DEG;
                    if (radians < 0)
                    {
                        actualRadians *= -1d;
                    }

                    directionToGo = directionToGo.GetRotatedVector(rotation.Axis, Math1D.RadiansToDegrees(actualRadians));
                }

                // Don't jitter back and forth at full throttle when at the boundry
                double percentThrust = GetEdgePercentAcceleration(distanceToGo - distanceToStop, maxAcceleration);

                directionToGo.Normalize();
                directionToGo *= percentThrust;

                #endregion
            }

            // Exit Function
            return directionToGo;
        }

        /// <summary>
        /// This one won't let the bot come at the target too fast (and tries to match the target's velocity)
        /// </summary>
        protected Vector3D GetDirection_InterceptTarget_VelocityAware(Point3D targetPointWorld, Vector3D targetVelocityWorld, double maxAcceleration)
        {
            const double DOT_POSTOTHESIDE = .5d;
            const double DOT_VELORTH = .5d;
            const double TOOFARTIME = 4d;		// if it takes longer than this to get to the point, then just chase it directly
            const bool SHOULDREVERSETHRUSTWHENAPPROPRIATE = true;		// true:  just reverse thrust, false: turn toward chase point (not as efficient, but looks more like flight)

            if (_shouldShowDebugVisuals && _pointVisualizer_VelAware3 == null)
            {
                #region Visualizer

                // This method has it's own visualizer

                _pointVisualizer_VelAware3 = new PointVisualizer(_viewport, _sharedVisuals, 5);

                _pointVisualizer_VelAware3.SetCustomItemProperties(0, true, Colors.HotPink, .03d);		// chase point
                _pointVisualizer_VelAware3.SetCustomItemProperties(1, false, Colors.DarkGoldenrod, .05d);		// this is my max acceleration
                _pointVisualizer_VelAware3.SetCustomItemProperties(2, false, Colors.DarkOrchid, 1d);		// projected chase point1
                _pointVisualizer_VelAware3.SetCustomItemProperties(3, false, Colors.Orchid, 1d);		// projected chase point2
                _pointVisualizer_VelAware3.SetCustomItemProperties(4, true, Colors.Red, .1d);		// unchased point

                #endregion
            }

            // clear off intermitent visuals (the ones that don't update every frame will appear to hang if I don't clean them up)
            if (_shouldShowDebugVisuals)
            {
                _pointVisualizer_VelAware3.Update(1, new Point3D(), new Vector3D());
                _pointVisualizer_VelAware3.Update(4, new Point3D());
            }

            Point3D positionLocal = _physicsBody.CenterOfMass;
            Point3D positionWorld = _physicsBody.PositionToWorld(positionLocal);

            if (_shouldShowDebugVisuals)
            {
                _pointVisualizer_VelAware3.Update(0, targetPointWorld);
                if (Ship.DEBUGSHOWSACCELERATION)
                {
                    _pointVisualizer_VelAware3.Update(1, positionWorld, _physicsBody.DirectionToWorld(new Vector3D(maxAcceleration, 0, 0)));
                }
            }

            // Get some general vectors
            Point3D targetPointLocal = _physicsBody.PositionFromWorld(targetPointWorld);
            Vector3D targetVelocityLocal = _physicsBody.DirectionFromWorld(targetVelocityWorld);

            Vector3D directionToGo = targetPointLocal.ToVector() - positionLocal.ToVector();
            double distanceToGo = directionToGo.Length;

            Vector3D velocityLocal = _physicsBody.DirectionFromWorld(_physicsBody.VelocityCached);

            #region Get approximate intercept point

            // Get time to point
            double interceptTime1 = GetInterceptTime(targetPointWorld, positionWorld, _physicsBody.VelocityCached, maxAcceleration);

            // Project where the chase point will be at that time
            Point3D targetPointWorld1 = targetPointWorld + (targetVelocityWorld * interceptTime1);

            // Get the intercept time for this second point
            double interceptTime2 = GetInterceptTime(targetPointWorld1, positionWorld, _physicsBody.VelocityCached, maxAcceleration);

            // Project again
            Point3D targetPointWorld2 = targetPointWorld + (targetVelocityWorld * interceptTime2);

            if (_shouldShowDebugVisuals)
            {
                _pointVisualizer_VelAware3.Update(2, positionWorld, targetPointWorld1 - positionWorld);
                _pointVisualizer_VelAware3.Update(3, positionWorld, targetPointWorld2 - positionWorld);
            }

            #endregion

            if (interceptTime1 > TOOFARTIME)
            {
                // Simplifying if too far away
                return GetDirection_StraightToTarget_VelocityAware2(targetPointWorld2, maxAcceleration);
            }

            #region Determine relative positions/velocities

            // Dot product of my velocity with chase point location
            Vector3D velocityLocalUnit = velocityLocal.ToUnit();
            Vector3D directionToGoUnit = directionToGo.ToUnit();
            double velDotDir = Vector3D.DotProduct(velocityLocalUnit, directionToGoUnit);

            // Dot product of my velocity with chase velocity
            Vector3D targetVelocityLocalUnit = targetVelocityLocal.ToUnit();
            double velDotVel = Vector3D.DotProduct(velocityLocalUnit, targetVelocityLocalUnit);

            //NOTE:  In several conditions, it's a toss up whether the swarm bot should turn around, or reverse thrust.  This should be a member
            // level boolean

            if (velDotDir > DOT_POSTOTHESIDE)
            {
                if (velDotVel > DOT_POSTOTHESIDE)
                {
                    #region Point is in front & Traveling in the same direction

                    // Make a modified version of speed aware 2, and head directly for the chase point, trying to match velocity
                    //
                    // Also may want to try to shoot for a bit in front of them

                    #endregion
                }
                else if (velDotVel > -DOT_POSTOTHESIDE)
                {
                    #region Point is in front & Traveling orthoganal

                    // I've found when dogfighting, that I don't want to go to where they are now, I want to follow the path they've taken so I can
                    // run up their rear.
                    //
                    // So project a point behind them and go for that
                    //
                    // I probably want to do a hybrid between going directly behind them, and going for the chase point


                    #endregion
                }
                else
                {
                    #region Point is in front & Traveling in opposite directions

                    // Go straight for them, then reverse thrust in time so they run into me (sort of playing chicken)

                    #endregion
                }
            }
            else if (velDotDir > -DOT_POSTOTHESIDE)
            {
                if (velDotVel > DOT_POSTOTHESIDE)
                {
                    #region Point is to the side & Traveling in the same direction

                    // Don't turn toward them, just slide over to them (match the velocity along their direction of travel, and use the remaining
                    // thrust to shoot to the side

                    #endregion
                }
                else if (velDotVel > -DOT_POSTOTHESIDE)
                {
                    #region Point is to the side & Traveling orthoganal

                    // Turn toward them (call speed aware 2 going straight for the chase point)

                    #endregion
                }
                else
                {
                    #region Point is to the side & Traveling in opposite directions

                    // Either:
                    //		turn toward it
                    //		or reverse thrust and eventually get to the point where you can slide up to it

                    #endregion
                }
            }
            else
            {
                if (velDotVel > DOT_POSTOTHESIDE)
                {
                    #region Point is behind & Traveling in the same direction

                    // Get in front of them, then either just slow down, or reverse thrust toward them until they're close enough to reverse thrust again

                    #endregion
                }
                else if (velDotVel > -DOT_POSTOTHESIDE)
                {
                    #region Point is behind & Traveling orthoganal

                    // Turn toward them (similar case with "Point is to the side & Traveling orthoganal"?)

                    #endregion
                }
                else
                {
                    #region Point is behind & Traveling in opposite directions

                    // Reverse thrust

                    #endregion
                }
            }

            #endregion







            return GetDirection_StraightToTarget_VelocityAware2(targetPointWorld2, maxAcceleration);








            _pointVisualizer_VelAware3.Update(4, targetPointWorld);
            return new Vector3D();
        }
        /// <summary>
        /// This will return a direction to get away from the other bots
        /// </summary>
        /// <param name="otherBots">I don't use _otherBots, in case you want to limit based on vision constraints</param>
        /// <param name="divisionFactor">Swarmbot tester needs 8, asteroid miner needs 1.  Need to come up with a better way to do this</param>
        protected Vector3D GetDirection_AvoidBots(List<SwarmBotBase> otherBots, double divisionFactor)
        {
            Vector3D retVal = new Vector3D(0, 0, 0);

            Vector3D myPositionWorld = _physicsBody.PositionToWorld(_physicsBody.CenterOfMass).ToVector();       // center mass should always be at zero, but I want to be consistent

            foreach (SwarmBotBase bot in otherBots)
            {
                // Get its position relative to me
                Vector3D botPositionWorld = bot.PhysicsBody.PositionToWorld(bot.PhysicsBody.CenterOfMass).ToVector();

                Vector3D offsetLocal = _physicsBody.DirectionFromWorld(botPositionWorld - myPositionWorld);

                // I need to invert this, because the greater the distance, the less the influence
                double offsetLength = offsetLocal.Length;

                // Subtract the two radii (it's should be the distance between the bots, not their centers)
                double distanceBetween = offsetLength - this.Radius - bot.Radius;

                if (Math1D.IsNearZero(distanceBetween) || distanceBetween < 0)
                {
                    // Can't divide by zero.  For now, I'll just skip this bot
                    //TODO:  May want to create an arbitrary vector with a length of 10
                    continue;
                }


                // Figure out the constants to put into this equation
                //     rad1+rad2 should have a force of 1 (the gap between them being twice the radius)
                //     3(rad1+rad2) should have a force of .1 (I can't also do this with a 1/x equation, I would need to multiply the distance by 10 (not 3) to get .1) (if I wanted that, I think I would need 1/x^2, or something?  can't really think straight right now)




                //TODO:  this needs to come from the caller.  Either take in settings, or a delegate, or enum with settings
                double awayForce = (this.Radius + bot.Radius) / distanceBetween;
                //awayForce /= 8d;     // it seems to be about 10 times stronger than it should be
                awayForce /= divisionFactor;



                offsetLocal /= offsetLength;    // normalize
                offsetLocal *= -awayForce;    // point away from the bot, with the away force strength

                // Now add this to the return vector
                retVal += offsetLocal;
            }

            // Cap the return length
            double returnLengthSquared = retVal.LengthSquared;
            if (returnLengthSquared > 100d)  // it shouldn't be unless it's really close to another bot
            {
                // This method needs to cap the output to a magnitude of 10
                retVal.Normalize();
                retVal *= 10d;
            }

            // Exit Function
            return retVal;
        }
        /// <summary>
        /// This will return a direction to get away from the other bodies
        /// </summary>
        protected Vector3D GetDirection_AvoidBodies(List<ConvexBody3D> bodies, double avoidStrengthMultiplier, bool shouldCapOutput)
        {
            //TODO:  Merge logic with AvoidBots?

            Vector3D retVal = new Vector3D(0, 0, 0);

            Vector3D myPositionWorld = _physicsBody.PositionToWorld(_physicsBody.CenterOfMass).ToVector();       // center mass should always be at zero, but I want to be consistent

            foreach (ConvexBody3D body in bodies)
            {
                // Get its position relative to me
                Vector3D bodyPositionWorld = body.PositionToWorld(body.CenterOfMass).ToVector();

                Vector3D offsetLocal = _physicsBody.DirectionFromWorld(bodyPositionWorld - myPositionWorld);

                // I need to invert this, because the greater the distance, the less the influence
                double offsetLength = offsetLocal.Length;

                // Subtract to two radii (it's should be the distance between the bot and body, not their centers)
                //TODO:  Figure out the radius of the other body (or rough radius based on a ray cast into it?
                double distanceBetween = offsetLength - this.Radius;

                if (Math1D.IsNearZero(distanceBetween) || distanceBetween < 0)
                {
                    // Can't divide by zero.  For now, I'll just skip this bot
                    //TODO:  May want to create an arbitrary vector with a length of 10
                    continue;
                }


                // Figure out the constants to put into this equation
                //     rad1+rad2 should have a force of 1 (the gap between them being twice the radius)
                //     3(rad1+rad2) should have a force of .1 (I can't also do this with a 1/x equation, I would need to multiply the distance by 10 (not 3) to get .1) (if I wanted that, I think I would need 1/x^2, or something?  can't really think straight right now)




                //TODO:  this needs to come from the caller.  Either take in settings, or a delegate, or enum with settings
                double awayForce = (this.Radius) / distanceBetween;
                awayForce *= avoidStrengthMultiplier;     // it seems to be about 10 times stronger than it should be



                offsetLocal /= offsetLength;    // normalize
                offsetLocal *= -awayForce;    // point away from the bot, with the away force strength

                // Now add this to the return vector
                retVal += offsetLocal;
            }

            // Cap the return length
            if (shouldCapOutput)
            {
                double returnLengthSquared = retVal.LengthSquared;
                if (returnLengthSquared > 100d)  // it shouldn't be unless it's really close to another bot
                {
                    // This method needs to cap the output to a magnitude of 10
                    retVal.Normalize();
                    retVal *= 10d;
                }
            }

            // Exit Function
            return retVal;
        }



        /// <summary>
        /// This returns the combined velocity of the swarm passed in
        /// The result is returned in local coords, but isn't normalized - that way the consumer can use it however they want
        /// </summary>
        /// <param name="otherBots">I don't use _otherBots, in case you want to limit based on vision constraints</param>
        protected Vector3D GetSwarmVelocity(List<SwarmBotBase> otherBots)
        {
            Vector3D retVal = new Vector3D(0, 0, 0);

            foreach (SwarmBotBase bot in otherBots)
            {
                retVal += bot.VelocityWorld;
            }

            // The velocities were in world coords.  Change to local
            retVal = _physicsBody.DirectionFromWorld(retVal);

            // Exit Function
            return retVal;
        }

        /// <summary>
        /// This returns the center of mass of the known flock in world coords (including me)
        /// </summary>
        protected Vector3D GetCenterOfFLock(List<SwarmBotBase> otherBots)
        {
            // Start with myself
            Vector3D retVal = _physicsBody.PositionToWorld(_physicsBody.CenterOfMass).ToVector();

            // Add the other bots
            foreach (SwarmBotBase bot in otherBots)
            {
                retVal += bot.PhysicsBody.PositionToWorld(bot.PhysicsBody.CenterOfMass).ToVector();
            }

            // Now take the average
            if (otherBots.Count > 0)       // dividing by one is pointless
            {
                retVal /= otherBots.Count + 1;   // add one because of me
            }

            // Exit Function
            return retVal;
        }

        /// <summary>
        /// This returns the bots within the range passed in
        /// </summary>
        protected List<SwarmBotBase> GetOtherBots(double visionDistance, bool sortByDistance)
        {
            List<SwarmBotBase> retVal = new List<SwarmBotBase>();
            List<double> distancesSquared = new List<double>();        // this is the distance from the bot in retVal to me (used when sorting)

            if (visionDistance == double.MaxValue && !sortByDistance)
            {
                // Just return a copy of the known list
                retVal.AddRange(_otherBots);
            }
            else
            {
                #region Limit Vision

                double visionDistanceSquared = visionDistance * visionDistance;

                Vector3D myPositionWorld = _physicsBody.PositionToWorld(_physicsBody.CenterOfMass).ToVector();       // center mass should always be at zero, but I want to be consistent

                foreach (SwarmBotBase bot in _otherBots)
                {
                    // Get its position relative to me
                    Vector3D botPositionWorld = bot.PhysicsBody.PositionToWorld(bot.PhysicsBody.CenterOfMass).ToVector();

                    Vector3D offset = botPositionWorld - myPositionWorld;

                    // I'm including the bot's radius, but not mine, because my eye is in the center   :)
                    double distSquared = offset.LengthSquared - (bot.Radius * bot.Radius);

                    if (distSquared <= visionDistanceSquared)
                    {
                        retVal.Add(bot);
                        distancesSquared.Add(distSquared);
                    }
                }

                #endregion

                if (sortByDistance && retVal.Count > 1)
                {
                    #region Sort by distance

                    // Since I already know the distances, I'll use array's sort method

                    double[] keys = distancesSquared.ToArray();
                    SwarmBotBase[] botArr = retVal.ToArray();

                    Array.Sort(keys, botArr);

                    retVal.Clear();
                    retVal.AddRange(botArr);

                    #endregion
                }
            }

            // Exit Function
            return retVal;
        }

        /// <summary>
        /// This will return all other objects that could be considered obstacles
        /// </summary>
        /// <param name="includeOtherBots">
        /// True:  Include the bots in this.OtherBots
        /// False:  Exclude the bots in this.OtherBots (any swarmbot that's not in the list will be included - must be an enemy bot)
        /// </param>
        protected List<ConvexBody3D> GetObstacles(double visionDistance, bool includeOtherBots)
        {
            List<ConvexBody3D> retVal = new List<ConvexBody3D>();

            #region Square the vision distance

            bool limitByDistance = false;  // no need to calculate the distance if they allow everything
            double visionDistanceSquared = visionDistance;    // using the square to avoid all the square roots (they don't taste right)
            Vector3D myPositionWorld = new Vector3D();

            if (visionDistance < double.MaxValue)
            {
                limitByDistance = true;

                visionDistanceSquared = visionDistance * visionDistance;

                myPositionWorld = _physicsBody.PositionToWorld(_physicsBody.CenterOfMass).ToVector();       // center mass should always be at zero, but I want to be consistent
            }

            #endregion

            // Go through the list of other objects in the world
            foreach (ConvexBody3D body in _world.GetBodies())
            {
                if (body == _physicsBody)
                {
                    continue;
                }

                if (!includeOtherBots)
                {
                    #region Filter other bots

                    bool isSwarmMate = false;

                    foreach (SwarmBotBase bot in _otherBots)
                    {
                        if (body == bot.PhysicsBody)
                        {
                            isSwarmMate = true;
                            break;
                        }
                    }

                    if (isSwarmMate)
                    {
                        continue;
                    }

                    #endregion
                }

                if (limitByDistance)
                {
                    #region Filter by distance

                    // Get its position relative to me
                    Vector3D bodyPositionWorld = body.PositionToWorld(body.CenterOfMass).ToVector();

                    Vector3D offset = bodyPositionWorld - myPositionWorld;

                    if (offset.LengthSquared > visionDistanceSquared)
                    {
                        continue;
                    }

                    #endregion
                }

                // It passed the filters, add it
                retVal.Add(body);
            }

            // Exit Function
            return retVal;
        }

        #endregion

        #region Event Listeners

        private void Body_ApplyForce(Body sender, BodyForceEventArgs e)
        {
            if (sender != _physicsBody)
            {
                return;
            }

            #region Thrusters

            if (_thrustTransform != null && _thrustPercent > 0d)
            {
                _thruster.ApplyForce(_thrustPercent, _physicsBody.Transform, _thrustTransform, e);
            }

            #endregion
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// This is called when accelerating toward or away from something.  The gap distance isn't the entire distance to accelerate
        /// across, but the distance at the edge
        /// </summary>
        /// <remarks>
        /// I'm getting a case where the gap distance is just barely off, and this fires full thrust toward.  Then next frame full thrust away, it just jitters.
        /// So I want to give a bit of a ramp up before going full power
        /// </remarks>
        /// <returns>
        /// A % of max thrust from 0 to 1
        /// </returns>
        private static double GetEdgePercentAcceleration(double gapDistance, double maxAcceleration)
        {
            const double SOFTACCELERATIONTIME = .5;		// half second

            // If I can cover the gap distance in less some fraction of a second, then use partial thrust
            // d = 1/2 at^2
            // t = sqrt(2 * d/a)
            double timeToAccelerateThatDistance = Math.Sqrt(2 * gapDistance / maxAcceleration);

            if (timeToAccelerateThatDistance < SOFTACCELERATIONTIME)
            {
                return timeToAccelerateThatDistance / SOFTACCELERATIONTIME;
            }
            else
            {
                return 1d;
            }
        }

        /// <summary>
        /// Returns how long it will take to reach a point while under constant acceleration
        /// NOTE:  This is invalid if distance is negative, or acceleration is negative or zero
        /// </summary>
        /// <param name="initialVelocity">The velocity along the direction to the chase point</param>
        private static double GetInterceptTime(double distance, double initialVelocity, double acceleration)
        {
            // d = vt + (at^2)/2
            // t = (sqrt(v^2 + 2ad) - v) / a
            return (Math.Sqrt((initialVelocity * initialVelocity) + (2 * acceleration * distance)) - initialVelocity) / acceleration;
        }
        private static double GetInterceptTime(Point3D chasePoint, Point3D position, Vector3D velocity, double maxAcceleration)
        {
            // Get the appropriate scalars from the vectors passed in
            Vector3D directionToGo = chasePoint - position;

            Vector3D velocityAlongDirectionLine = velocity.GetProjectedVector(directionToGo);

            // Call the scalar overload
            return GetInterceptTime(directionToGo.Length, velocityAlongDirectionLine.Length, maxAcceleration);
        }

        #endregion
    }
}
