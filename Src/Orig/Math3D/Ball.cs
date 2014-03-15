using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Orig.Math3D
{
    /// <summary>
    /// This class is a sphere with mass, radius, forces, velocity.
    /// </summary>
    /// <remarks>
    /// I do not support angular momentum and velocity.  If you want rotation, use CRigidBody.  It inherits me.  So when you use it, you
    /// get the whole works.
    /// </remarks>
    public class Ball : Sphere
    {
        #region Private Enum: Coords

        private enum Coords
        {
            X = 0,
            Y,
            Z
        }

        #endregion

        #region Declaration Section

        private MyVector _acceleration = new MyVector(0, 0, 0);
        private MyVector _internalForce = new MyVector(0, 0, 0);
        private MyVector _externalForce = new MyVector(0, 0, 0);
        private MyVector _velocity = new MyVector(0, 0, 0);

        private double _mass = -1;

        /// <summary>
        /// This is used when performing collisions.  1 is a super ball, 0 is a wet towel.
        /// </summary>
        private double _elasticity;

        private double _kineticFriction;
        private double _staticFriction;

        private bool _usesBoundingBox = false;
        private MyVector _boundingLower = null;
        private MyVector _boundingUpper = null;

        // These are used by the timer functions to remember some state
        private MyVector _savedPosition = null;
        private double _elapsedTime = 0;

        #endregion

        #region Constructor

        public Ball(MyVector position, DoubleVector origDirectionFacing, double radius, double mass)
            : base(position, origDirectionFacing, radius)
        {
            // I use the property sets to enforce the values
            this.Mass = mass;
            this.Elasticity = 1d;

            _usesBoundingBox = false;
            _boundingLower = null;
            _boundingUpper = null;
        }

        public Ball(MyVector position, DoubleVector origDirectionFacing, double radius, double mass, MyVector boundingBoxLower, MyVector boundingBoxUpper)
            : this(position, origDirectionFacing, radius, mass, 1, 1, 1, boundingBoxLower, boundingBoxUpper) { }

        /// <summary>
        /// This overload is used if you plan to do collisions
        /// </summary>
        public Ball(MyVector position, DoubleVector origDirectionFacing, double radius, double mass, double elasticity, double kineticFriction, double staticFriction, MyVector boundingBoxLower, MyVector boundingBoxUpper)
            : base(position, origDirectionFacing, radius)
        {
            // I use the property sets to enforce the values
            this.Mass = mass;
            this.Elasticity = elasticity;
            this.KineticFriction = kineticFriction;
            this.StaticFriction = staticFriction;

            _usesBoundingBox = true;
            _boundingLower = boundingBoxLower;
            _boundingUpper = boundingBoxUpper;
        }

        /// <summary>
        /// This one is used to assist with the clone method (especially for my derived classes)
        /// </summary>
        /// <param name="usesBoundingBox">Just pass in what you have</param>
        /// <param name="boundingBoxLower">Set this to null if bounding box is false</param>
        /// <param name="boundingBoxUpper">Set this to null if bounding box is false</param>
        protected Ball(MyVector position, DoubleVector origDirectionFacing, MyQuaternion rotation, double radius, double mass, double elasticity, double kineticFriction, double staticFriction, bool usesBoundingBox, MyVector boundingBoxLower, MyVector boundingBoxUpper)
            : base(position, origDirectionFacing, rotation, radius)
        {
            // I use the property sets to enforce the values
            this.Mass = mass;
            this.Elasticity = elasticity;
            this.KineticFriction = kineticFriction;
            this.StaticFriction = staticFriction;

            _usesBoundingBox = usesBoundingBox;
            _boundingLower = boundingBoxLower;
            _boundingUpper = boundingBoxUpper;
        }

        #endregion

        #region Public Properties

        public virtual double Mass
        {
            get
            {
                return _mass;
            }
            set
            {
                // Make sure mass is greater than zero
                if (value <= 0d)
                {
                    throw new ArgumentOutOfRangeException("Mass must be greater than zero");
                }

                // Store it
                _mass = value;
            }
        }

        public double Elasticity
        {
            get
            {
                return _elasticity;
            }
            set
            {
                if (value < 0d)
                {
                    throw new ArgumentOutOfRangeException("Elasticity must be zero or greater");
                }

                // I should probably cap it to 1 as well, since anything greater is flubber.  But that may be desired in
                // some cases

                _elasticity = value;
            }
        }

        public double KineticFriction
        {
            get
            {
                return _kineticFriction;
            }
            set
            {
                _kineticFriction = value;
            }
        }
        public double StaticFriction
        {
            get
            {
                return _staticFriction;
            }
            set
            {
                _staticFriction = value;
            }
        }

        /// <summary>
        /// When whacking me around, it is most likely the external force that you will add to (this one is in the world frame).
        /// NOTE: This gets reset at the beginning of each timer tick
        /// </summary>
        /// <remarks>
        /// This would be good to use if you are modeling forces generated by the external world.
        /// 
        /// For instance, if you want to model gravity, add (0, 0, -10) to the external force every frame.  This will cause the ball
        /// to be feel a constant downward pull (as viewed from the world) - think of a high diver: they fall toward the pool,
        /// even though they are spinning and flipping around.
        /// </remarks>
        public MyVector ExternalForce
        {
            get
            {
                return _externalForce;
            }
        }
        /// <summary>
        /// This force is aligned to my orientation (body frame).
        /// NOTE: This gets reset at the beginning of each timer tick
        /// </summary>
        /// <remarks>
        /// You would use this (as opposed to external) if you are attaching force generating equipment to the ball.
        /// 
        /// For instance, if you want to emulate a thruster, when the user holds in the up arrow, you would add (0, 0, 10) to the
        /// internal force every frame, not caring what direction the ball is currently facing in the world frame.  This will cause the
        /// ball to be pushed along this.DirectionFacing.Stanard.Z
        /// </remarks>
        public MyVector InternalForce
        {
            get
            {
                return _internalForce;
            }
        }
        /// <summary>
        /// It is unlikely that you will add to this directly, you will either hit me with forces, or change my velocity
        /// directly.
        /// NOTE: This gets reset at the beginning of each timer tick
        /// </summary>
        public MyVector Acceleration
        {
            get
            {
                return _acceleration;
            }
        }
        /// <summary>
        /// NOTE: This does NOT get reset at the beginning of each timer tick
        /// </summary>
        public MyVector Velocity
        {
            get
            {
                return _velocity;
            }
        }

        public MyVector BoundryLower
        {
            get
            {
                return _boundingLower;
            }
        }
        public MyVector BoundryUpper
        {
            get
            {
                return _boundingUpper;
            }
        }

        #endregion
        #region Protected Properties

        protected bool UsesBoundingBox
        {
            get
            {
                return _usesBoundingBox;
            }
        }

        #endregion

        #region Public Methods

        public override Sphere Clone()
        {
            // I want a copy of the bounding box, not a clone (everything else gets cloned)
            return new Ball(this.Position.Clone(), this.OriginalDirectionFacing.Clone(), this.Rotation.Clone(), this.Radius, _mass, _elasticity, _kineticFriction, _staticFriction, _usesBoundingBox, _boundingLower, _boundingUpper);
        }
        /// <summary>
        /// This calls my clone method, and casts the result back to ball for you
        /// </summary>
        public Ball CloneBall()
        {
            return this.Clone() as Ball;
        }

        /// <summary>
        /// This causes the ball to come to a complete stop
        /// </summary>
        public virtual void StopBall()
        {
            _velocity.X = 0;
            _velocity.Y = 0;
            _velocity.Z = 0;
        }

        /// <summary>
        /// This function needs to be called before the timer is called.  Between this function and the timer function is when all the outside
        /// forces have a chance to influence the object (gravity, explosion blasts, conveyor belts, etc)
        /// </summary>
        /// <remarks>
        /// I've given this function a bit of thought.  It's really not needed, because when TimerFinish is done, that should be the equivalent
        /// of a new cycle.  But I like having more defined phases (even though there is an extra round of function calls to make)
        /// </remarks>
        public virtual void PrepareForNewTimerCycle()
        {

            // I can't decide whether to put this here, or the timerfinish
            _savedPosition = null;
            _elapsedTime = 0;

            // Reset stuff
            _internalForce.Multiply(0);
            _externalForce.Multiply(0);
            _acceleration.Multiply(0);

        }

        /// <summary>
        /// This function can be called many times with different elapsed times passed in.  This function will only figure out a new
        /// position based on the previous tick's velocity and the time passed in.  Every time this function gets called, the starting point
        /// is the last call to PrepareForNewCycle.
        /// </summary>
        public virtual void TimerTestPosition(double elapsedTime)
        {

            // Make sure position is where it was when PrepareForNew was called
            if (_savedPosition == null)
            {
                // This is the first time I've been called since PrepareForNew.  Remember the current position
                _savedPosition = this.Position.Clone();
            }
            else
            {
                // I've been called before.  Reset my position
                this.Position.StoreNewValues(_savedPosition);
            }

            // Remember the elapsed time that was passed in
            _elapsedTime = elapsedTime;

            // Get the change in position
            MyVector changeInPos = _velocity * elapsedTime;

            // See if I'm about to clip the box boundry (If I am, the ChangeInPos will be changed so that I don't cross the boundry line.)
            if (_usesBoundingBox)
            {
                TimerBoxBoundry(this.Position, ref changeInPos, elapsedTime);
            }

            // ChangeInPos is now a position.  I will add it to the current position
            this.Position.Add(changeInPos);

        }

        /// <summary>
        /// This function is used to calculate the new velocity based on all the forces that occured from Prepare to Now
        /// </summary>
        public virtual void TimerFinish()
        {

            // Combine the internal and external force
            MyVector force = TimerSprtCombineForce();

            // Figure out the new acceleration
            TimerSprtAccel(force);

            // Figure out the new velocity
            TimerSprtVel(_elapsedTime);

            // I could set the saved position to null here, but I'll just wait until PrepareForNew is called

        }

        #endregion

        #region Private Methods

        /// <summary>
        /// This function returns the addition of internal force and external force.  Before I do that, I need to rotate a copy of internal
        /// force so that it is based off of the same orientation as the external force.
        /// </summary>
        private MyVector TimerSprtCombineForce()
        {

            if (_internalForce.IsZero)
            {
                // Internal force is zero, so there is nothing to rotate (or add)
                return _externalForce.Clone();
            }

            // The sphere's rotation quaternion already knows how to rotate from internal to external.  So make a copy of
            // internal line up with external.
            MyVector retVal = base.Rotation.GetRotatedVector(_internalForce, true);

            // retVal holds the internal force in the same orientation as the external force.  I will add external force to it, and retVal
            // will hold the net force placed on the ball
            retVal.Add(_externalForce);

            // Exit Function
            return retVal;
        }

        /// <summary>
        /// This function will compute the new acceleration based on the force pushing against the ball.  The result will overwrite the
        /// current acceleration
        /// </summary>
        private void TimerSprtAccel(MyVector force)
        {

            // Make the PartialAccel into a clone of the force passed in
            MyVector partialAccel = force.Clone();

            // I will divide by mass, and it will no longer be force, it will be change in accel (the property guarantees that mass
            // isn't zero)
            partialAccel.Divide(_mass);

            // Add the partial accel to this ball's accel
            _acceleration.Add(partialAccel);

        }

        /// <summary>
        /// This function will calculate the new velocity based on the current acceleration and elapsed time.  I will store the result back into
        /// my velocity property.
        /// </summary>
        private void TimerSprtVel(double elapsedTime)
        {

            // Clone the current accel
            MyVector changeInVel = _acceleration.Clone();

            // Now make the ChangeInVel a velocity instead of an acceleration by multiplying it by time
            changeInVel.Multiply(elapsedTime);

            // I have the change in velocity, so I will add it to the current velocity in order to make a new velocity
            _velocity.Add(changeInVel);

        }

        /// <summary>
        /// This function will check to see if I've clipped the box boundry.  If I have, I will change the ChangeInPosition, so that the ball
        /// won't cross the boundry line.
        /// </summary>
        /// <param name="curPositionCloned">The values of this will be changed if there is a boundry collision</param>
        /// <param name="changeInPos">This will get reset if there is a collision with the boundry</param>
        /// <param name="elapsedTime">This is needed to recalculate changeInPos if there is a boundry collision</param>
        private void TimerBoxBoundry(MyVector position, ref MyVector changeInPos, double elapsedTime)
        {

            List<Coords> affectedAxiis = new List<Coords>();

            // Clone the Position in order to start generating the ProposedPosition
            MyVector proposedPosition = this.Position.Clone();

            // Add the change in position to the proposed position to make it a real proposed position
            proposedPosition.Add(changeInPos);

            // Now compare the proposed position with the stated corner stones to see if I went to far
            #region X

            if (proposedPosition.X < _boundingLower.X)
            {
                affectedAxiis.Add(Coords.X);
                position.X = _boundingLower.X;
            }
            else if (proposedPosition.X > _boundingUpper.X)
            {
                affectedAxiis.Add(Coords.X);
                position.X = _boundingUpper.X;
            }

            #endregion
            #region Y

            if (proposedPosition.Y < _boundingLower.Y)
            {
                affectedAxiis.Add(Coords.Y);
                position.Y = _boundingLower.Y;
            }
            else if (proposedPosition.Y > _boundingUpper.Y)
            {
                affectedAxiis.Add(Coords.Y);
                position.Y = _boundingUpper.Y;
            }

            #endregion
            #region Z

            if (proposedPosition.Z < _boundingLower.Z)
            {
                affectedAxiis.Add(Coords.Z);
                position.Z = _boundingLower.Z;
            }
            else if (proposedPosition.Z > _boundingUpper.Z)
            {
                affectedAxiis.Add(Coords.Z);
                position.Z = _boundingUpper.Z;
            }

            #endregion

            if (affectedAxiis.Count > 0)
            {
                // Bounce the ball
                changeInPos = TimerBoxBoundrySprtBounceIt(affectedAxiis, elapsedTime);
            }

        }
        private MyVector TimerBoxBoundrySprtBounceIt(List<Coords> affectedAxiis, double elapsedTime)
        {

            // Shoot through the affected axiis, and flip the corrosponding velocities
            foreach (Coords coord in affectedAxiis)
            {
                switch (coord)
                {
                    case Coords.X:
                        _velocity.X *= -1;
                        break;

                    case Coords.Y:
                        _velocity.Y *= -1;
                        break;

                    case Coords.Z:
                        _velocity.Z *= -1;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException("Unknown Coord: " + coord.ToString());
                }
            }

            // Now that the velocity is changed, I need to return a new change in position
            return _velocity * elapsedTime;

        }

        #endregion

    }
}
