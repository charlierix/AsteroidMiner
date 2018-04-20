using System;
using System.Collections.Generic;
using System.Text;

using Game.HelperClassesCore;
using Game.Orig.Math3D;
using Game.Orig.Map;

namespace Game.Orig.HelperClassesOrig
{
    /// <summary>
    /// This is a tractor beam that has a cone of influence (the only force is straight out or straight in)
    /// </summary>
    public class TractorBeamCone
    {
        //TODO: Put this in the Items.dll
        //TODO: Make a derived class that ties this to a container (need protected hooks in this base class)

        #region Enum: BeamMode

        public enum BeamMode
        {
            PushPull = 0,
            LeftRight
        }

        #endregion
        #region Class: Interaction

        private class Interaction
        {
            public BallBlip Blip = null;
            public MyVector ForceDirection = null;		// this is expected to be a unit vector
            public double Force = 0d;

            public Interaction(BallBlip blip, MyVector forceDirection, double force)
            {
                this.Blip = blip;
                this.ForceDirection = forceDirection;
                this.Force = force;
            }
        }

        #endregion
        #region Class: AngularVelocityInfo

        private class AngularVelocityInfo
        {
            public double AngularVelocity = 0d;
            public MyVector SpinDirection = null;
            public MyVector CenterMass = null;
        }

        #endregion

        #region Declaration Section

        private SimpleMap _map = null;

        /// <summary>
        /// The ship that this tractor beam is tied to
        /// </summary>
        private BallBlip _ship = null;

        /// <summary>
        /// This is where on the ship this tractor beam sits (offset from center point)
        /// </summary>
        private MyVector _offset = null;
        /// <summary>
        /// This is the direction this tractor beam is pointing (relative to OriginalDirectionFacing)
        /// </summary>
        private DoubleVector _dirFacing = null;

        private bool _isSoft = false;

        /// <summary>
        /// How wide the tractor beam coverage is (radians)
        /// </summary>
        private double _sweepAngle = 0d;
        /// <summary>
        /// The max distance covered
        /// </summary>
        private double _maxDistance = 0d;
        /// <summary>
        /// The force of the beam at zero distance
        /// </summary>
        private double _forceAtZero = 0d;
        /// <summary>
        /// The force of the beam at max distance (for now, I'm just modeling a linear dropoff)
        /// </summary>
        private double _forceAtMax = 0d;
        /// <summary>
        /// This is the total output in one tick.  This output is spread evenly across the available bodies
        /// </summary>
        private double _maxForcePerTick = 0d;

        /// <summary>
        /// I went with a boolean, because setting a double to zero isn't precisely off
        /// </summary>
        private bool _isOn = false;
        /// <summary>
        /// This goes from -1 to 1 (only applies when _isOn is true)
        /// </summary>
        private double _percent = 0d;
        /// <summary>
        /// This tries to hold object's relative velocity to zero (static distance to ship)
        /// </summary>
        /// <remarks>
        /// Either static or percent is used, never both
        /// </remarks>
        private bool _isStatic = false;
        private BeamMode _mode = BeamMode.PushPull;

        #endregion

        #region Constructor

        public TractorBeamCone(SimpleMap map, BallBlip ship, MyVector offset, DoubleVector dirFacing, BeamMode mode, bool isSoft, double sweepAngle, double maxDistance, double forceAtZero, double forceAtMax, double maxForcePerTick)
        {
            _map = map;

            _ship = ship;

            _offset = offset;
            _dirFacing = dirFacing;

            _mode = mode;

            _isSoft = isSoft;

            _sweepAngle = sweepAngle;
            _maxDistance = maxDistance;
            _forceAtZero = forceAtZero;
            _forceAtMax = forceAtMax;
            _maxForcePerTick = maxForcePerTick;
        }

        #endregion

        #region Public Properties

        public bool IsActive
        {
            get
            {
                return _isOn;
            }
        }

        /// <summary>
        /// The ship that this tractor beam is tied to
        /// </summary>
        public BallBlip Ship
        {
            get
            {
                return _ship;
            }
            set
            {
                _ship = value;
            }
        }

        /// <summary>
        /// This is where on the ship this tractor beam sits (offset from center point)
        /// </summary>
        public MyVector Offset
        {
            get
            {
                return _offset;
            }
            set
            {
                _offset = value;
            }
        }
        /// <summary>
        /// This is the direction this tractor beam is pointing (relative to OriginalDirectionFacing)
        /// </summary>
        public DoubleVector DirFacing
        {
            get
            {
                return _dirFacing;
            }
            set
            {
                _dirFacing = value;
            }
        }

        public BeamMode Mode
        {
            get
            {
                return _mode;
            }
            set
            {
                _mode = value;
            }
        }

        public bool IsSoft
        {
            get
            {
                return _isSoft;
            }
            set
            {
                _isSoft = value;
            }
        }

        /// <summary>
        /// How wide the tractor beam coverage is (radians)
        /// </summary>
        public double SweepAngle
        {
            get
            {
                return _sweepAngle;
            }
            set
            {
                _sweepAngle = value;
            }
        }
        /// <summary>
        /// The max distance covered
        /// </summary>
        public double MaxDistance
        {
            get
            {
                return _maxDistance;
            }
            set
            {
                _maxDistance = value;
            }
        }
        /// <summary>
        /// The force of the beam at zero distance
        /// </summary>
        public double ForceAtZero
        {
            get
            {
                return _forceAtZero;
            }
            set
            {
                _forceAtZero = value;
            }
        }
        /// <summary>
        /// The force of the beam at max distance (for now, I'm just modeling a linear dropoff)
        /// </summary>
        public double ForceAtMax
        {
            get
            {
                return _forceAtMax;
            }
            set
            {
                _forceAtMax = value;
            }
        }
        /// <summary>
        /// This is the total output in one tick.  This output is spread evenly across the available bodies
        /// </summary>
        public double MaxForcePerTick
        {
            get
            {
                return _maxForcePerTick;
            }
            set
            {
                _maxForcePerTick = value;
            }
        }

        #endregion

        #region Public Methods

        public void TurnOff()
        {
            _isOn = false;
        }
        public void TurnOn(double percent)
        {
            _isStatic = false;
            _percent = percent;
            _isOn = true;
        }
        public void TurnOnStatic()
        {
            _isStatic = true;
            _isOn = true;
        }

        public void Timer()
        {
            if (!_isOn)
            {
                return;
            }

            // Figure out the current center point of the tractor beam
            MyVector centerLocal = _ship.Ball.Rotation.GetRotatedVector(_offset, true);
            MyVector centerWorld = centerLocal + _ship.Ball.Position;

            // Figure out the current direction facing of the tractor beam
            MyVector dirFacingWorld = _ship.Ball.Rotation.GetRotatedVector(_dirFacing.Standard, true);

            // Find blips
            double totalForce;
            List<Interaction> interactions = null;

            if (_isStatic)
            {
                interactions = GetInteractions_Static(out totalForce, centerWorld, dirFacingWorld);
            }
            else
            {
                interactions = GetInteractions_Standard(out totalForce, centerWorld, dirFacingWorld);
            }

            // See if the max required force exceeds the tractor beam's output
            double overloadPercent = 1d;
            if (totalForce > _maxForcePerTick)
            {
                // I can only apply a fraction of my total available output to each blip
                overloadPercent = _maxForcePerTick / totalForce;
            }

            foreach (Interaction interaction in interactions)
            {
                #region Whack Balls

                // Turn the line between into a force vector
                interaction.ForceDirection.Multiply(interaction.Force * overloadPercent);

                // Apply to ship
                if (_ship.TorqueBall != null)
                {
                    switch (_mode)
                    {
                        case BeamMode.PushPull:
                            // Apply the force at the location of the tractor beam on the ship
                            _ship.TorqueBall.ApplyExternalForce(centerLocal, interaction.ForceDirection);
                            break;

                        case BeamMode.LeftRight:
                            // Apply the force at the location of the blip
                            _ship.TorqueBall.ApplyExternalForce(interaction.Blip.Ball.Position - _ship.Ball.Position, interaction.ForceDirection);
                            break;

                        default:
                            throw new ApplicationException("Unknown BeamMode: " + _mode);
                    }
                }
                else
                {
                    // Apply the force at the center of the ball
                    _ship.Ball.ExternalForce.Add(interaction.ForceDirection);
                }

                // Apply to blip
                if (interaction.Blip.CollisionStyle == CollisionStyle.Standard)     // can't move around stationary objects
                {
                    interaction.ForceDirection.Multiply(-1d);
                    //TODO:  Figure out rotational
                    interaction.Blip.Ball.ExternalForce.Add(interaction.ForceDirection);
                }

                #endregion
            }
        }

        #endregion

        #region Private Methods

        private List<Interaction> GetInteractions_Standard(out double totalForce, MyVector centerWorld, MyVector dirFacingWorld)
        {
            totalForce = 0d;
            List<Interaction> retVal = new List<Interaction>();

            AngularVelocityInfo tractorAngularInfo = null;

            // Scan for objects in my path
            foreach (BallBlip blip in FindBlipsInCone(centerWorld, dirFacingWorld))
            {
                // Get the distance
                MyVector lineBetween = blip.Sphere.Position - centerWorld;
                double distance = lineBetween.GetMagnitude();

                // Figure out the force to apply
                double force = UtilityCore.GetScaledValue(_forceAtZero, _forceAtMax, 0d, _maxDistance, distance);
                force *= _percent;

                switch (_mode)
                {
                    case BeamMode.PushPull:
                        #region Push Pull

                        if (!Utility3D.IsNearZero(distance))
                        {
                            // Turn lineBetween into a unit vector (it will be multiplied by force later)
                            lineBetween.BecomeUnitVector();

                            if (_isSoft)
                            {
                                force = GetForceForSoft(ref tractorAngularInfo, force, lineBetween, distance, blip.Ball, dirFacingWorld);
                            }

                            // Add this to the return list
                            retVal.Add(new Interaction(blip, lineBetween, force));
                            totalForce += Math.Abs(force);  // percent is negative when in repulse mode
                        }

                        #endregion
                        break;

                    case BeamMode.LeftRight:
                        #region Left Right

                        // Only do something if the lines aren't sitting directly on top of each other (even if they want to repel,
                        // I'd be hesitant to just repel in any random direction)
                        if (!Utility3D.IsNearValue(MyVector.Dot(lineBetween, dirFacingWorld, true), 1d))
                        {
                            // Get a line that's orthogonal to lineBetween, and always points toward the dirFacingWorld vector
                            MyVector dirToCenterLine = MyVector.Cross(lineBetween, MyVector.Cross(lineBetween, dirFacingWorld));
                            dirToCenterLine.BecomeUnitVector();

                            // Add to the return list
                            retVal.Add(new Interaction(blip, dirToCenterLine, force));
                            totalForce += Math.Abs(force);  // percent is negative when in repulse mode
                        }

                        #endregion
                        break;

                    default:
                        throw new ApplicationException("Unknown BeamMode: " + _mode.ToString());
                }
            }

            // Exit Function
            return retVal;
        }
        private List<Interaction> GetInteractions_Static(out double totalForce, MyVector centerWorld, MyVector dirFacingWorld)
        {
            totalForce = 0d;
            List<Interaction> retVal = new List<Interaction>();


            // This is only used for left/right modes (lazy initialization)
            AngularVelocityInfo angularInfo = null;


            // Scan for objects in my path
            foreach (BallBlip blip in FindBlipsInCone(centerWorld, dirFacingWorld))
            {
                // Get a vector from me to the ball
                MyVector lineBetween = blip.Ball.Position - centerWorld;
                double distance = lineBetween.GetMagnitude();

                switch (_mode)
                {
                    case BeamMode.PushPull:
                        #region Push Pull

                        if (!Utility3D.IsNearZero(distance))
                        {
                            lineBetween.BecomeUnitVector();
                            lineBetween.Multiply(-1);

                            double relativeVelocity = MyVector.Dot(lineBetween, blip.Ball.Velocity - _ship.Ball.Velocity);

                            // Figure out how much force is required to make this relative velocity equal zero
                            double force = relativeVelocity * blip.Ball.Mass;   // Velocity * Mass is impulse force

                            // See if force needs to be limited by the tractor's max force
                            double maxForce = UtilityCore.GetScaledValue(_forceAtZero, _forceAtMax, 0d, _maxDistance, distance);
                            if (Math.Abs(force) > maxForce)
                            {
                                if (force > 0d)
                                {
                                    force = maxForce;
                                }
                                else
                                {
                                    force = maxForce * -1d;
                                }
                            }

                            // Add to results
                            retVal.Add(new Interaction(blip, lineBetween, force));
                            totalForce += Math.Abs(force);
                        }

                        #endregion
                        break;

                    case BeamMode.LeftRight:
                        #region Left Right

                        // Only do something if the lines aren't sitting directly on top of each other (even if they want to repel,
                        // I'd be hesitant to just repel in any random direction)
                        if (!Utility3D.IsNearValue(MyVector.Dot(lineBetween, dirFacingWorld, true), 1d))
                        {
                            // Figure out how fast the ship is spinning where the blip is
                            MyVector dirToCenterLine;
                            MyVector spinVelocity = GetSpinVelocityAtPoint(ref angularInfo, out dirToCenterLine, dirFacingWorld, lineBetween, blip.Ball.Position);

                            // Figure out the relative velocity (between blip and my spin)
                            double relativeVelocity1 = MyVector.Dot(dirToCenterLine, blip.Ball.Velocity - spinVelocity);

                            // Figure out how much force is required to make this relative velocity equal zero
                            double force1 = relativeVelocity1 * blip.Ball.Mass;   // Velocity * Mass is impulse force

                            // See if force needs to be limited by the tractor's max force
                            double maxForce1 = UtilityCore.GetScaledValue(_forceAtZero, _forceAtMax, 0d, _maxDistance, distance);
                            if (Math.Abs(force1) > maxForce1)
                            {
                                if (force1 > 0d)
                                {
                                    force1 = maxForce1;
                                }
                                else
                                {
                                    force1 = maxForce1 * -1d;
                                }
                            }

                            // Add to results
                            retVal.Add(new Interaction(blip, dirToCenterLine, force1));
                            totalForce += force1;
                        }

                        #endregion
                        break;

                    default:
                        throw new ApplicationException("Unknown BeamMode: " + _mode.ToString());
                }
            }

            // Exit Function
            return retVal;
        }

        /// <summary>
        /// This method will adjust the force to keep the relative velocity relativaly small
        /// </summary>
        /// <remarks>
        /// This method needs work.  It would help if I exposed velocity and force lines to be drawn for debugging
        /// </remarks>
        private double GetForceForSoft(ref AngularVelocityInfo angularInfo, double maxForce, MyVector forceDirection, double distance, Ball ball, MyVector dirFacingWorld)
        {
            const double MINVELOCITY = 20d;

            double minVelocity = UtilityCore.GetScaledValue(0, MINVELOCITY, 0, _maxDistance, distance);

            MyVector dummy;
            MyVector tractorVelocity = GetSpinVelocityAtPoint(ref angularInfo, out dummy, dirFacingWorld, _offset, _ship.Ball.Position + _offset);
            tractorVelocity = tractorVelocity + _ship.Ball.Velocity;

            double relativeVelocity = MyVector.Dot(forceDirection, ball.Velocity - tractorVelocity);

            double retVal = maxForce;
            if (maxForce > 0)
            {
                #region Pulling In

                // Positive force means the relative velocity will need to be negative (pulling the object in)

                if (Utility3D.IsNearValue(relativeVelocity, minVelocity * -1d))
                {
                    // It's going the right speed.  No force needed
                    return 0;
                }
                else if (relativeVelocity < (minVelocity * -1d))
                {
                    // It's coming in too fast.  Slow it down
                    retVal = Math.Abs(relativeVelocity) - Math.Abs(minVelocity) * ball.Mass;   // Velocity * Mass is impulse force
                }

                #endregion
            }
            else
            {
                #region Pushing Away

                // Negative force means the relative velocity will need to be positive (pushing the object away)


                if (Utility3D.IsNearValue(relativeVelocity, minVelocity))
                {
                    // It's going the right speed.  No force needed
                    return 0;
                }
                else if (relativeVelocity > minVelocity)
                {
                    // It's going away too fast.  Slow it down
                    retVal = Math.Abs(relativeVelocity) - Math.Abs(minVelocity) * ball.Mass;   // Velocity * Mass is impulse force

                    retVal *= -1d;
                }



                //if (relativeVelocity > MINVELOCITY)
                //{
                //    // It's going fast enough, no need to apply any more force
                //    return 0;
                //}


                // Figure out how much force is required to make this relative velocity equal MINVELOCITY
                //retVal = (relativeVelocity - MINVELOCITY) * ball.Mass;   // Velocity * Mass is impulse force

                #endregion
            }

            // Cap the return the max force
            if (Math.Abs(retVal) > Math.Abs(maxForce))
            {
                if (retVal > 0)
                {
                    retVal = Math.Abs(maxForce);
                }
                else
                {
                    retVal = Math.Abs(maxForce) * -1d;
                }
            }

            // Exit Function
            return retVal;
        }

        private MyVector GetSpinVelocityAtPoint(ref AngularVelocityInfo angularInfo, out MyVector dirToCenterLine, MyVector dirFacingWorld, MyVector lineBetween, MyVector blipPosition)
        {
            // Get a line that's orthogonal to lineBetween, and always points toward the dirFacingWorld vector
            dirToCenterLine = MyVector.Cross(MyVector.Cross(lineBetween, dirFacingWorld), lineBetween);
            dirToCenterLine.BecomeUnitVector();

            if (angularInfo == null)
            {
                #region Cache Angular Velocity

                angularInfo = new AngularVelocityInfo();

                if (_ship.TorqueBall != null)
                {
                    angularInfo.AngularVelocity = _ship.TorqueBall.AngularVelocity.GetMagnitude();

                    angularInfo.SpinDirection = MyVector.Cross(_ship.TorqueBall.AngularVelocity, _ship.TorqueBall.DirectionFacing.Standard);
                    angularInfo.SpinDirection.BecomeUnitVector();

                    angularInfo.CenterMass = _ship.TorqueBall.Rotation.GetRotatedVector(_ship.TorqueBall.CenterOfMass, true);
                    angularInfo.CenterMass.Add(_ship.TorqueBall.Position);
                }
                else
                {
                    angularInfo.SpinDirection = dirToCenterLine.Clone();
                    angularInfo.AngularVelocity = 0d;
                    angularInfo.CenterMass = _ship.Ball.Position.Clone();
                }

                #endregion
            }

            // Get the line between the blip and the center of mass
            MyVector lineBetweenCM = blipPosition - angularInfo.CenterMass;

            // Figure out my velocity of spin where the blip is
            return angularInfo.SpinDirection * (angularInfo.AngularVelocity * lineBetweenCM.GetMagnitude());
        }

        private List<BallBlip> FindBlipsInCone(MyVector centerWorld, MyVector dirFacingWorld)
        {
            List<BallBlip> retVal = new List<BallBlip>();

            // Cache some stuff for use inside the loop
            double halfSweepAngle = _sweepAngle / 2d;
            double maxDistSquared = _maxDistance * _maxDistance;

            // Scan for objects in my path
            foreach (RadarBlip blip in _map.GetAllBlips())
            {
                if (blip.Token == _ship.Token)
                {
                    // Can't manipulate me
                    continue;
                }

                if (blip.CollisionStyle == CollisionStyle.Ghost)
                {
                    // Ghost blips don't interact with anything
                    continue;
                }

                if (!(blip is BallBlip))
                {
                    // The blip needs to be at least a ball
                    continue;
                }

                MyVector lineBetween = blip.Sphere.Position - centerWorld;
                if (lineBetween.GetMagnitudeSquared() > maxDistSquared)
                {
                    // The object is too far away
                    continue;
                }

                MyVector rotationAxis;
                double rotationRadians;
                dirFacingWorld.GetAngleAroundAxis(out rotationAxis, out rotationRadians, lineBetween);
                if (rotationRadians > halfSweepAngle)
                {
                    // The sweep angle is too great (not inside the cone)
                    continue;
                }

                // It's inside the cone
                retVal.Add(blip as BallBlip);
            }

            // Exit Function
            return retVal;
        }

        #endregion
    }
}
