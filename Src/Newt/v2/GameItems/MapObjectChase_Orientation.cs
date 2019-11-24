using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.NewtonDynamics;

// The objects in this file chase an orientation
namespace Game.Newt.v2.GameItems
{
    #region class: MapObject_ChaseOrientation_Velocity

    /// <summary>
    /// This chases an orientation
    /// NOTE: Only a single vector is chased (not a double vector).  So the body can still spin about the axis of the vector being chased
    /// </summary>
    public class MapObject_ChaseOrientation_Velocity : IDisposable
    {
        #region Declaration Section

        private Vector3D? _desiredOrientation = null;

        #endregion

        #region Constructor

        public MapObject_ChaseOrientation_Velocity(IMapObject item)
        {
            this.Item = item;

            this.Item.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(PhysicsBody_ApplyForceAndTorque);
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
                StopChasing();

                this.Item.PhysicsBody.ApplyForceAndTorque -= new EventHandler<BodyApplyForceAndTorqueArgs>(PhysicsBody_ApplyForceAndTorque);
            }
        }

        #endregion

        #region Public Properties

        public readonly IMapObject Item;

        /// <summary>
        /// The body's Z axis will get aligned with the chase vector.  If it should be another axis, set this to tell how to
        /// orient that axis to Z
        /// </summary>
        //public Quaternion? Offset
        //{
        //    get;
        //    set;
        //}

        private double _multiplier = 20d;
        public double Multiplier
        {
            get
            {
                return _multiplier;
            }
            set
            {
                _multiplier = value;
            }
        }

        public double? MaxVelocity
        {
            get;
            set;
        }

        #endregion

        #region Public Methods

        public void SetOrientation(Vector3D orientation)
        {
            _desiredOrientation = orientation;
        }

        public void StopChasing()
        {
            _desiredOrientation = null;
        }

        #endregion

        #region Event Listeners

        private void PhysicsBody_ApplyForceAndTorque(object sender, BodyApplyForceAndTorqueArgs e)
        {
            // See if there is anything to do
            if (_desiredOrientation == null)
            {
                return;
            }

            //TODO: Implement this.Offset
            //TODO: Allow rotations in the destination axis

            Vector3D current = e.Body.DirectionToWorld(new Vector3D(0, 0, 1));
            Quaternion rotation = Math3D.GetRotation(current, _desiredOrientation.Value);

            if (rotation.IsIdentity)
            {
                // Don't set anything.  If they are rotating along the allowed axis, then no problem.  If they try
                // to rotate off that axis, another iteration of this method will rotate back
                //e.Body.AngularVelocity = new Vector3D(0, 0, 0);
                return;
            }

            // According to the newton wiki, angular velociy is radians per second
            Vector3D newAngVel = rotation.Axis.ToUnit() * Math1D.DegreesToRadians(rotation.Angle);
            newAngVel *= _multiplier;

            if (this.MaxVelocity != null && newAngVel.LengthSquared > this.MaxVelocity.Value * this.MaxVelocity.Value)
            {
                newAngVel = newAngVel.ToUnit() * this.MaxVelocity.Value;
            }

            e.Body.AngularVelocity = newAngVel;
        }

        #endregion
    }

    #endregion

    #region class: MapObject_ChaseOrientation_Torques

    /// <summary>
    /// This chases an orientation
    /// (the body's Z axis will get pulled to align with the destination direction)
    /// </summary>
    public class MapObject_ChaseOrientation_Torques : IDisposable
    {
        #region Declaration Section

        /// <summary>
        /// If they are using a spring, this is the point to move to
        /// </summary>
        private Vector3D? _desiredOrientation = null;

        #endregion

        #region Constructor

        public MapObject_ChaseOrientation_Torques(IMapObject item)
        {
            this.Item = item;
            this.Percent = 1d;

            this.Item.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(PhysicsBody_ApplyForceAndTorque);
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
                StopChasing();

                this.Item.PhysicsBody.ApplyForceAndTorque -= new EventHandler<BodyApplyForceAndTorqueArgs>(PhysicsBody_ApplyForceAndTorque);
            }
        }

        #endregion

        #region Public Properties

        public readonly IMapObject Item;

        //public Vector3D Offset
        //{
        //    get;
        //    set;
        //}

        // If these are populated, then a torque will get reduced if it will exceed one of these
        public double? MaxTorque
        {
            get;
            set;
        }
        public double? MaxAcceleration
        {
            get;
            set;
        }

        /// <summary>
        /// This gives an option of only applying a percent of the full force
        /// </summary>
        /// <remarks>
        /// This way objects can be gradually ramped up to full force (good when first creating an object)
        /// </remarks>
        public double Percent
        {
            get;
            set;
        }

        public ChaseOrientation_Torque[] Torques
        {
            get;
            set;
        }

        #endregion

        #region Public Methods

        public void SetOrientation(Vector3D direction)
        {
            _desiredOrientation = direction;
        }

        public void StopChasing()
        {
            _desiredOrientation = null;
        }

        #endregion

        #region Event Listeners

        private void PhysicsBody_ApplyForceAndTorque(object sender, BodyApplyForceAndTorqueArgs e)
        {
            // See if there is anything to do
            if (_desiredOrientation == null)
            {
                return;
            }

            //TODO: Offset
            Vector3D current = e.Body.DirectionToWorld(new Vector3D(0, 0, 1));
            Quaternion rotation = Math3D.GetRotation(current, _desiredOrientation.Value);

            if (rotation.IsIdentity)
            {
                // Don't set anything.  If they are rotating along the allowed axis, then no problem.  If they try
                // to rotate off that axis, another iteration of this method will rotate back
                //e.Body.AngularVelocity = new Vector3D(0, 0, 0);
                return;
            }

            ChaseOrientation_GetTorqueArgs args = new ChaseOrientation_GetTorqueArgs(this.Item, rotation);

            Vector3D? torque = null;

            // Call each worker
            foreach (var worker in this.Torques)
            {
                Vector3D? localForce = worker.GetTorque(args);

                if (localForce != null)
                {
                    if (torque == null)
                    {
                        torque = localForce;
                    }
                    else
                    {
                        torque = torque.Value + localForce.Value;
                    }
                }
            }

            // Apply the torque
            if (torque != null)
            {
                // Limit if exceeds this.MaxForce
                if (this.MaxTorque != null && torque.Value.LengthSquared > this.MaxTorque.Value * this.MaxTorque.Value)
                {
                    torque = torque.Value.ToUnit() * this.MaxTorque.Value;
                }

                // Limit acceleration
                if (this.MaxAcceleration != null)
                {
                    double mass = Item.PhysicsBody.Mass;

                    //f=ma
                    double accel = torque.Value.Length / mass;

                    if (accel > this.MaxAcceleration.Value)
                    {
                        torque = torque.Value.ToUnit() * (this.MaxAcceleration.Value * mass);
                    }
                }

                torque = torque.Value * this.Percent;

                e.Body.AddTorque(torque.Value);
            }
        }

        #endregion
    }

    #endregion
    #region class: ChaseOrientation_Torque

    public class ChaseOrientation_Torque
    {
        #region Constructor

        public ChaseOrientation_Torque(ChaseDirectionType direction, double value, bool isAccel = true, bool isSpring = false, GradientEntry[] gradient = null)
        {
            if (gradient != null && gradient.Length == 1)
            {
                throw new ArgumentException("Gradient must have at least two items if it is populated");
            }

            Direction = direction;
            Value = value;
            IsAccel = isAccel;
            IsSpring = isSpring;

            if (gradient == null || gradient.Length == 0)
            {
                Gradient = null;
            }
            else
            {
                Gradient = gradient;
            }
        }

        #endregion

        #region Public Properties

        public readonly ChaseDirectionType Direction;

        public bool IsDrag
        {
            get
            {
                // Direction is attract, all else is drag
                return this.Direction != ChaseDirectionType.Attract_Direction;
            }
        }

        /// <summary>
        /// True: The value is an acceleration
        /// False: The value is a force
        /// </summary>
        public readonly bool IsAccel;
        /// <summary>
        /// True: The value is multiplied by distance (f=kx)
        /// False: The value is it (f=k)
        /// NOTE: Distance is degrees, so any value from 0 to 180
        /// </summary>
        /// <remarks>
        /// Nothing in this class will prevent you from having this true and a gradient at the same time, but that
        /// would give pretty strange results
        /// </remarks>
        public readonly bool IsSpring;

        public readonly double Value;

        /// <summary>
        /// This specifies varying percents based on distance to target
        /// Item1: Distance (in degrees from 0 to 180)
        /// Item2: Percent
        /// </summary>
        /// <remarks>
        /// If a distance is less than what is specified, then the lowest value gradient stop will be used (same with larger distances)
        /// So you could set up a crude s curve (though I don't know why you would):
        ///     at 5: 25%
        ///     at 20: 75%
        /// 
        /// I think the most common use of the gradient would be to set up a dead spot near 0:
        ///     at 0: 0%
        ///     at 10: 100%
        /// 
        /// Or maybe set up a crude 1/x repulsive force near the destination point:
        ///     at 0: 100%
        ///     at 2: 27%
        ///     at 4: 12%
        ///     at 6: 5.7%
        ///     at 8: 2%
        ///     at 10: 0%
        /// </remarks>
        public readonly GradientEntry[] Gradient;

        #endregion

        #region Public Methods

        public Vector3D? GetTorque(ChaseOrientation_GetTorqueArgs e)
        {
            Vector3D unit;
            double length;
            GetDesiredVector(out unit, out length, e, this.Direction);
            if (Math3D.IsNearZero(unit))
            {
                return null;
            }

            double torque = this.Value;
            if (this.IsAccel)
            {
                // f=ma
                torque *= e.ItemMass;
            }

            if (this.IsSpring)
            {
                torque *= e.Rotation.Angle;
            }

            if (this.IsDrag)
            {
                torque *= -length;       // negative, because it needs to be a drag force
            }

            // Gradient %
            if (this.Gradient != null)
            {
                torque *= ChasePoint_Force.GetGradientPercent(e.Rotation.Angle, this.Gradient);
            }

            return unit * torque;
        }

        #endregion

        #region Private Methods

        private static void GetDesiredVector(out Vector3D unit, out double length, ChaseOrientation_GetTorqueArgs e, ChaseDirectionType direction)
        {
            switch (direction)
            {
                case ChaseDirectionType.Drag_Velocity_Along:
                case ChaseDirectionType.Drag_Velocity_AlongIfVelocityAway:
                case ChaseDirectionType.Drag_Velocity_AlongIfVelocityToward:
                    unit = e.AngVelocityAlongUnit;
                    length = e.AngVelocityAlongLength;
                    break;

                case ChaseDirectionType.Attract_Direction:
                    unit = e.Rotation.Axis;
                    length = e.Rotation.Angle;
                    break;

                case ChaseDirectionType.Drag_Velocity_Any:
                    unit = e.AngVelocityUnit;
                    length = e.AngVelocityLength;
                    break;

                case ChaseDirectionType.Drag_Velocity_Orth:
                    unit = e.AngVelocityOrthUnit;
                    length = e.AngVelocityOrthLength;
                    break;

                default:
                    throw new ApplicationException("Unknown DirectionType: " + direction.ToString());
            }
        }

        #endregion
    }

    #endregion

    #region class: ChaseOrientation_GetTorqueArgs

    public class ChaseOrientation_GetTorqueArgs
    {
        //TODO: Have a way to determine which props will be needed up front, and only populate those
        public ChaseOrientation_GetTorqueArgs(IMapObject item, Quaternion rotation)
        {
            this.Item = item;
            this.ItemMass = item.PhysicsBody.Mass;

            this.Rotation = rotation;
            Vector3D direction = rotation.Axis;

            // Angular Velocity
            Vector3D angularVelocity = this.Item.PhysicsBody.AngularVelocity;
            this.AngVelocityLength = angularVelocity.Length;
            this.AngVelocityUnit = angularVelocity.ToUnit();

            // Along
            Vector3D velocityAlong = angularVelocity.GetProjectedVector(direction);
            this.AngVelocityAlongLength = velocityAlong.Length;
            this.AngVelocityAlongUnit = velocityAlong.ToUnit();
            this.IsAngVelocityAlongTowards = Vector3D.DotProduct(direction, angularVelocity) > 0d;

            // Orth
            Vector3D orth = Vector3D.CrossProduct(direction, angularVelocity);       // the first cross is orth to both (outside the plane)
            orth = Vector3D.CrossProduct(orth, direction);       // the second cross is in the plane, but orth to distance
            Vector3D velocityOrth = angularVelocity.GetProjectedVector(orth);

            this.AngVelocityOrthLength = velocityOrth.Length;
            this.AngVelocityOrthUnit = velocityOrth.ToUnit();
        }

        public readonly IMapObject Item;
        public readonly double ItemMass;

        public readonly Quaternion Rotation;

        public readonly Vector3D AngVelocityUnit;
        public readonly double AngVelocityLength;

        public readonly bool IsAngVelocityAlongTowards;
        public readonly Vector3D AngVelocityAlongUnit;
        public readonly double AngVelocityAlongLength;

        public readonly Vector3D AngVelocityOrthUnit;
        public readonly double AngVelocityOrthLength;
    }

    #endregion

    #region TODOs

    //TODO: AngularDrag
    //  This would need to be a torque modifier (call body.addtorque instead of body.addforce) - or
    //  directly modify angular velocity
    //
    //  This shouldn't act on world coords.  Instead pass in a vector every frame (when drag is a plane,
    //  world works, but when it's a cylinder, the vector needs to be tangent to the cylinder)

    /// <summary>
    /// This applies torque if the angular velocity isn't right
    /// </summary>
    internal class MapObject_ChaseAngVel_Forces
    {
        //TODO: Finish this:
        //  Instead of SetPosition, have SetAngularVelocity - this would be useful for keeping things spinning forever
        //  Or have a way of clamping to only one plane of rotation
    }

    #endregion
}
