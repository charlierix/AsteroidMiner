using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Game.HelperClasses;
using Game.Newt.AsteroidMiner2;
using Game.Newt.HelperClasses;
using Game.Newt.NewtonDynamics;

namespace Game.Newt.Testers.Arcanorum
{
    //TODO: Rename Game.Newt.AsteroidMiner2.SelectedItem to MapObjectChaseDirect, and put these next to it
    //TODO: Pass in ChasePointVelocity as well

    #region Class: MapObjectChaseVelocity

    public class MapObjectChaseVelocity : IDisposable
    {
        #region Declaration Section

        /// <summary>
        /// If they are using a spring, this is the point to move to
        /// </summary>
        private Point3D? _desiredPoint = null;

        #endregion

        #region Constructor

        public MapObjectChaseVelocity(IMapObject item)
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

                this.Item.PhysicsBody.ApplyForceAndTorque -= new EventHandler<NewtonDynamics.BodyApplyForceAndTorqueArgs>(PhysicsBody_ApplyForceAndTorque);
            }
        }

        #endregion

        #region Public Properties

        public readonly IMapObject Item;
        public Vector3D Offset
        {
            get;
            set;
        }

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

        public void SetPosition(Point3D point)
        {
            _desiredPoint = point;
        }

        public void StopChasing()
        {
            _desiredPoint = null;
        }

        #endregion

        #region Event Listeners

        private void PhysicsBody_ApplyForceAndTorque(object sender, NewtonDynamics.BodyApplyForceAndTorqueArgs e)
        {
            // See if there is anything to do
            if (_desiredPoint == null)
            {
                return;
            }

            Point3D current = e.Body.PositionToWorld(this.Offset.ToPoint());
            Vector3D direction = _desiredPoint.Value - current;

            Vector3D newVelocity = direction * _multiplier;

            if (this.MaxVelocity != null && newVelocity.LengthSquared > this.MaxVelocity.Value * this.MaxVelocity.Value)
            {
                newVelocity = newVelocity.ToUnit(false) * this.MaxVelocity.Value;
            }

            e.Body.Velocity = newVelocity;
        }

        #endregion
    }

    #endregion

    #region Class: MapObjectChaseForces

    /// <summary>
    /// This chases a point
    /// </summary>
    public class MapObjectChaseForces : IDisposable
    {
        #region Declaration Section

        /// <summary>
        /// True: Forces will be applied at the drag point.
        /// False: Force will be applied directly to the body.
        /// </summary>
        private readonly bool _shouldCauseTorque;

        /// <summary>
        /// If they are using a spring, this is the point to move to
        /// </summary>
        private Point3D? _desiredPoint = null;

        #endregion

        #region Constructor

        public MapObjectChaseForces(IMapObject item, bool shouldCauseTorque = false)
        {
            this.Item = item;
            _shouldCauseTorque = shouldCauseTorque;
            this.Forces = new List<ChaseForcesBase>();

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

                this.Item.PhysicsBody.ApplyForceAndTorque -= new EventHandler<NewtonDynamics.BodyApplyForceAndTorqueArgs>(PhysicsBody_ApplyForceAndTorque);
            }
        }

        #endregion

        #region Public Properties

        public readonly IMapObject Item;
        public Vector3D Offset
        {
            get;
            set;
        }

        // If these are populated, then a force will get reduced if it will exceed one of these
        public double? MaxForce
        {
            get;
            set;
        }
        public double? MaxAcceleration
        {
            get;
            set;
        }

        public List<ChaseForcesBase> Forces
        {
            get;
            private set;
        }

        #endregion

        #region Public Methods

        public void SetPosition(Point3D point)
        {
            _desiredPoint = point;
        }

        public void StopChasing()
        {
            _desiredPoint = null;
        }

        #endregion

        #region Event Listeners

        private void PhysicsBody_ApplyForceAndTorque(object sender, NewtonDynamics.BodyApplyForceAndTorqueArgs e)
        {
            // See if there is anything to do
            if (_desiredPoint == null)
            {
                return;
            }

            Point3D current = e.Body.PositionToWorld(this.Offset.ToPoint());

            ChaseGetForceArgs args = new ChaseGetForceArgs(this.Item, _desiredPoint.Value - current);

            Vector3D? force = null;

            // Call each worker
            foreach (var worker in this.Forces)
            {
                Vector3D? localForce = worker.GetForce(args);

                if (localForce != null)
                {
                    if (force == null)
                    {
                        force = localForce;
                    }
                    else
                    {
                        force = force.Value + localForce.Value;
                    }
                }
            }

            // Apply the force
            if (force != null)
            {


                //TODO: Limit if exceeds this.MaxForce, this.MaxAcceleration





                if (_shouldCauseTorque)
                {
                    e.Body.AddForceAtPoint(force.Value, current);
                }
                else
                {
                    e.Body.AddForce(force.Value);
                }
            }
        }

        #endregion
    }

    #endregion

    #region Class: MapObjectChaseTorque

    /// <summary>
    /// This is an oddly named class.  It applies torque if the angular velocity isn't right
    /// </summary>
    public class MapObjectChaseTorque
    {
        //TODO: Finish this:
        //  Instead of SetPosition, have SetAngularVelocity - this would be useful for keeping things spinning forever
        //  Or have a way of clamping to only one plane of rotation
    }

    #endregion

    #region Class: ChaseForcesBase

    public abstract class ChaseForcesBase
    {
        //TODO: Make this return two nullable vectors, one for force, one for torque
        public abstract Vector3D? GetForce(ChaseGetForceArgs e);
    }

    #endregion
    #region Class: ChaseForcesForceBase

    public abstract class ChaseForcesForceBase : ChaseForcesBase
    {
        #region Constructor

        public ChaseForcesForceBase(ChaseDirectionType direction)
        {
            this.Direction = direction;
        }

        #endregion

        #region Public Properties

        public readonly ChaseDirectionType Direction;

        /// <summary>
        /// Either BaseForce or BaseAcceleration will need to be populated.  If both are populated, the lower value will be used
        /// </summary>
        public double? BaseForce
        {
            get;
            set;
        }
        /// <summary>
        /// Either BaseForce or BaseAcceleration will need to be populated.  If both are populated, the lower value will be used
        /// </summary>
        public double? BaseAcceleration
        {
            get;
            set;
        }

        /// <summary>
        /// If set, this will only apply forces when the current velocity (defined by Direction) is less than this speed
        /// </summary>
        public double? ApplyWhenUnderSpeed
        {
            get;
            set;
        }
        /// <summary>
        /// If set, this will only apply forces when the current velocity (defined by Direction) is greater than this speed
        /// </summary>
        public double? ApplyWhenOverSpeed
        {
            get;
            set;
        }

        #endregion

        #region Public Methods

        /// <param name="percentToOther">
        /// 0=All this
        /// 1=All other
        /// </param>
        public abstract Vector3D? GetForceLERP(ChaseForcesForceBase other, ChaseGetForceArgs e, double percentToOther);

        public double GetBaseForce(double mass)
        {
            if (this.BaseForce == null && this.BaseAcceleration == null)
            {
                throw new InvalidOperationException("BaseForce and BaseAcceleration can't both be null");
            }
            else if (this.BaseForce != null && this.BaseAcceleration != null)
            {
                // f=ma
                double accelForce = mass * this.BaseAcceleration.Value;

                // Return the one that is lower
                if (this.BaseForce.Value < accelForce)
                {
                    return this.BaseForce.Value;
                }
                else
                {
                    return accelForce;
                }
            }
            else if (this.BaseForce != null)
            {
                return this.BaseForce.Value;
            }
            else //if (this.BaseAcceleration != null)
            {
                // f=ma
                return mass * this.BaseAcceleration.Value;
            }
        }

        public static void GetDesiredVector(out Vector3D unit, out double length, ChaseGetForceArgs e, ChaseDirectionType direction)
        {
            switch (direction)
            {
                case ChaseDirectionType.Velocity_Along:
                case ChaseDirectionType.Velocity_AlongIfVelocityAway:
                case ChaseDirectionType.Velocity_AlongIfVelocityToward:
                    unit = e.VelocityAlongUnit;
                    length = e.VelocityAlongLength;
                    break;

                case ChaseDirectionType.Direction:
                    unit = e.DirectionUnit;
                    length = e.DirectionLength;
                    break;

                case ChaseDirectionType.Velocity_Any:
                    unit = e.VelocityUnit;
                    length = e.VelocityLength;
                    break;

                case ChaseDirectionType.Velocity_Orth:
                    unit = e.VelocityOrthUnit;
                    length = e.VelocityOrthLength;
                    break;

                default:
                    throw new ApplicationException("Unknown DirectionType: " + direction.ToString());
            }
        }

        public bool IsDirectionValid(bool isVelocityAlongTowards)
        {
            if ((this.Direction == ChaseDirectionType.Velocity_AlongIfVelocityAway && isVelocityAlongTowards) ||
                (this.Direction == ChaseDirectionType.Velocity_AlongIfVelocityToward && !isVelocityAlongTowards))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public bool IsSpeedValid(double speed)
        {
            if (this.ApplyWhenUnderSpeed == null && this.ApplyWhenOverSpeed == null)
            {
                // No limits
                return true;
            }
            else if (this.ApplyWhenUnderSpeed != null && this.ApplyWhenOverSpeed != null)
            {
                // They want a range
                return speed > this.ApplyWhenOverSpeed.Value && speed < this.ApplyWhenUnderSpeed.Value;
            }
            else if (this.ApplyWhenUnderSpeed != null)
            {
                return speed < this.ApplyWhenUnderSpeed.Value;
            }
            else //if (this.ApplyWhenOverSpeed != null)
            {
                return speed > this.ApplyWhenOverSpeed.Value;
            }
        }

        // All the derived classes need to do these same checks up front
        protected Tuple<Vector3D, double> GetForce_InitialChecks(ChaseGetForceArgs e)
        {
            if (!IsDirectionValid(e.IsVelocityAlongTowards))
            {
                return null;
            }

            Vector3D unit;
            double length;
            GetDesiredVector(out unit, out length, e, this.Direction);

            if (!IsSpeedValid(length))
            {
                return null;
            }

            return Tuple.Create(unit, length);
        }
        protected Tuple<T, Vector3D, double> GetForceLERP_InitialChecks<T>(ChaseForcesForceBase other, ChaseGetForceArgs e) where T : ChaseForcesForceBase
        {
            if (!(other is T))
            {
                throw new ArgumentException(string.Format("other must be the same type as this class ({0})", typeof(T).ToString()));
            }

            if (this.Direction != other.Direction)
            {
                throw new ArgumentException(string.Format("this must have the same direction as other: {0}, {1}", this.Direction.ToString(), other.Direction.ToString()));
            }

            if (!IsDirectionValid(e.IsVelocityAlongTowards))
            {
                return null;
            }

            Vector3D unit;
            double length;
            GetDesiredVector(out unit, out length, e, this.Direction);

            if (!this.IsSpeedValid(length) || !other.IsSpeedValid(length))
            {
                return null;
            }

            return Tuple.Create((T)other, unit, length);
        }

        #endregion
    }

    #endregion

    #region Class: ChaseForcesGradient

    /// <summary>
    /// This is a way to have force definitions that are dependent on distance
    /// NOTE: All force definitions passed to a gradient must be the same type
    /// NOTE: Pass in gradient stops from lowest distance to highest, they will be skipped if reversed
    /// </summary>
    public class ChaseForcesGradient<T> : ChaseForcesBase where T : ChaseForcesForceBase
    {
        public ChaseForcesGradient(IEnumerable<ChaseForcesGradientStop<T>> forces)
        {
            ChaseForcesGradientStop<T>[] forceArray = forces.ToArray();

            if (forceArray.Length < 2)
            {
                throw new ArgumentException("Gradient needs at least two objects passed in: " + forceArray.Length.ToString());
            }

            Type first = forceArray[0].GetType();

            for (int cntr = 1; cntr < forceArray.Length; cntr++)
            {
                if (forceArray[cntr].GetType() != first)
                {
                    throw new ArgumentException(string.Format("Can't throw mixed types into the gradient: {0}, {1}", first.ToString(), forceArray[cntr].GetType().ToString()));
                }
            }

            this.Forces = forceArray;
        }

        public readonly ChaseForcesGradientStop<T>[] Forces;

        /// <summary>
        /// This finds two force definitions that straddle e.distance, and returns the percentage force
        /// </summary>
        public override Vector3D? GetForce(ChaseGetForceArgs e)
        {
            double itemRadius = e.Item.Radius;

            double fromDistance = this.Forces[0].Distance.GetDistance(itemRadius);

            // Find a pair that straddles the current position
            for (int cntr = 1; cntr < this.Forces.Length; cntr++)
            {
                double toDistance = this.Forces[cntr].Distance.GetDistance(itemRadius);

                if (e.DirectionLength < fromDistance || e.DirectionLength > toDistance)
                {
                    // Outside the range, prep for the next one
                    fromDistance = toDistance;
                    continue;
                }

                // Figure out the percent from from to to
                double percent = UtilityHelper.GetScaledValue_Capped(0d, 1d, fromDistance, toDistance, e.DirectionLength);

                // Call the special method
                return this.Forces[cntr - 1].Force.GetForceLERP(this.Forces[cntr].Force, e, percent);
            }

            // Didn't find a pair in range
            return null;
        }

        /// <summary>
        /// This lets you define a non linear function, and get some points
        /// </summary>
        /// <remarks>
        /// Make overloads for:
        ///     Sqrt
        ///     Square
        ///     S Curve
        ///     Bell Curve
        ///     etc
        /// </remarks>
        /// <returns>
        /// Item1=Distance
        /// Item2=Value
        /// </returns>
        public static Tuple<double, double>[] GetStopPoints(double start, double? stop, int samples)
        {
            throw new ApplicationException("finish this");
        }
    }

    #endregion
    #region Class: ChaseForcesGradientStop

    //TODO: This gradient assumes distance, but what if a gradient is needed based on velocity?  Probably wouldn't, if so, expand the ChaseForcesDrag class
    public class ChaseForcesGradientStop<T> where T : ChaseForcesForceBase
    {
        public ChaseForcesGradientStop(ChaseDistance distance, T force)
        {
            this.Distance = distance;
            this.Force = force;
        }

        public readonly ChaseDistance Distance;      // just use double.max if you want this to go to infinity
        public readonly T Force;
    }

    #endregion

    #region Class: ChaseForcesDrag

    //TODO: If velocity dependent levels of drag are needed, make a ChaseForcesDragSpring class (or stack several instances of this class, each targeting a different velocity range)

    /// <summary>
    /// Drag applies force based on current velocity.  The other types apply force based on distance
    /// </summary>
    public class ChaseForcesDrag : ChaseForcesForceBase
    {
        #region Constructor

        public ChaseForcesDrag(ChaseDirectionType direction)
            : base(direction) { }

        #endregion

        #region Public Methods

        public override Vector3D? GetForceLERP(ChaseForcesForceBase other, ChaseGetForceArgs e, double percentToOther)
        {
            var initial = GetForceLERP_InitialChecks<ChaseForcesDrag>(other, e);
            if (initial == null)
            {
                return null;
            }

            // Calculate the amount of force to use based on this/other/%
            double baseForce = UtilityHelper.GetScaledValue(base.GetBaseForce(e.ItemMass), other.GetBaseForce(e.ItemMass), 0, 1, percentToOther);

            return GetForce(initial.Item2, initial.Item3, baseForce);
        }

        public override Vector3D? GetForce(ChaseGetForceArgs e)
        {
            var initial = GetForce_InitialChecks(e);
            if (initial == null)
            {
                return null;
            }

            return GetForce(initial.Item1, initial.Item2, base.GetBaseForce(e.ItemMass));
        }

        #endregion

        #region Private Methods

        private static Vector3D GetForce(Vector3D velocityUnit, double velocityLength, double baseForce)
        {
            // This is drag, so force is applied opposite of velocity
            return velocityUnit * (velocityLength * -baseForce);
        }

        #endregion
    }

    #endregion
    #region Class: ChaseForcesConstant

    public class ChaseForcesConstant : ChaseForcesForceBase
    {
        #region Constructor

        public ChaseForcesConstant(ChaseDirectionType direction)
            : base(direction) { }

        #endregion

        #region Public Methods

        public override Vector3D? GetForceLERP(ChaseForcesForceBase other, ChaseGetForceArgs e, double percentToOther)
        {
            var initial = GetForceLERP_InitialChecks<ChaseForcesConstant>(other, e);
            if (initial == null)
            {
                return null;
            }

            // Calculate the amount of force to use based on this/other/%
            double baseForce = UtilityHelper.GetScaledValue(base.GetBaseForce(e.ItemMass), other.GetBaseForce(e.ItemMass), 0, 1, percentToOther);

            return initial.Item2 * baseForce;
        }

        public override Vector3D? GetForce(ChaseGetForceArgs e)
        {
            var initial = GetForce_InitialChecks(e);
            if (initial == null)
            {
                return null;
            }

            return initial.Item1 * base.GetBaseForce(e.ItemMass);
        }

        #endregion
    }

    #endregion
    #region Class: ChaseForcesSpring

    //public class ChaseForcesSpring : ChaseForcesForceBase
    //{
    //}

    #endregion

    // This isn't needed, it's just a specialized gradient of drag forces
    #region Class: ChaseForcesShockAbsorber

    //public class ChaseForcesShockAbsorber : ChaseForcesForceBase
    //{
    //}

    #endregion


    //TODO: AngularDrag
    //  This would need to be a torque modifier (call body.addtorque instead of body.addforce) - or
    //  directly modify angular velocity
    //
    //  This shouldn't act on world coords.  Instead pass in a vector every frame (when drag is a plane,
    //  world works, but when it's a cylinder, the vector needs to be tangent to the cylinder)


    #region Class: ChaseGetForceArgs

    public class ChaseGetForceArgs
    {
        //TODO: Have a way to determine which props will be needed up front, and only populate those
        public ChaseGetForceArgs(IMapObject item, Vector3D direction)
        {
            this.Item = item;
            this.ItemMass = item.PhysicsBody.Mass;

            this.DirectionLength = direction.Length;
            this.DirectionUnit = direction.ToUnit(false);

            // Velocity
            Vector3D velocity = this.Item.PhysicsBody.Velocity;
            this.VelocityLength = velocity.Length;
            this.VelocityUnit = velocity.ToUnit(false);

            // Along
            Vector3D velocityAlong = velocity.GetProjectedVector(direction);
            this.VelocityAlongLength = velocityAlong.Length;
            this.VelocityAlongUnit = velocityAlong.ToUnit(false);
            this.IsVelocityAlongTowards = Vector3D.DotProduct(direction, velocity) > 0d;

            // Orth
            Vector3D orth = Vector3D.CrossProduct(direction, velocity);       // the first cross is orth to both (outside the plane)
            orth = Vector3D.CrossProduct(orth, direction);       // the second cross is in the plane, but orth to distance
            Vector3D velocityOrth = velocity.GetProjectedVector(orth);

            this.VelocityOrthLength = velocityOrth.Length;
            this.VelocityOrthUnit = velocityOrth.ToUnit(false);
        }

        public readonly IMapObject Item;
        public readonly double ItemMass;

        public readonly Vector3D DirectionUnit;
        public readonly double DirectionLength;

        public readonly Vector3D VelocityUnit;
        public readonly double VelocityLength;

        public readonly bool IsVelocityAlongTowards;
        public readonly Vector3D VelocityAlongUnit;
        public readonly double VelocityAlongLength;

        public readonly Vector3D VelocityOrthUnit;
        public readonly double VelocityOrthLength;
    }

    #endregion
    #region Class: ChaseDistance

    public class ChaseDistance
    {
        public ChaseDistance(bool isAbsolute, double distance)
        {
            this.IsAbsolute = isAbsolute;
            this.Distance = distance;
        }

        /// <summary>
        /// True=Distance is actual distance
        /// False=Distance is percent of item radius
        /// </summary>
        public readonly bool IsAbsolute;
        public readonly double Distance;

        public double GetDistance(double itemRadius)
        {
            if (this.IsAbsolute)
            {
                return this.Distance;
            }
            else
            {
                // Reusing this.Distance to be a percent
                return itemRadius * this.Distance;
            }
        }
    }

    #endregion
    #region Enum: ChaseDirectionType

    public enum ChaseDirectionType
    {
        /// <summary>
        /// The force is along the direction vector
        /// </summary>
        Direction,
        /// <summary>
        /// Drag is applied to the entire velocity
        /// </summary>
        Velocity_Any,
        /// <summary>
        /// Drag is only applied along the part of the velocity that is along the direction to the chase point
        /// </summary>
        Velocity_Along,
        /// <summary>
        /// Drag is only applied along the part of the velocity that is along the direction to the chase point.
        /// But only if that velocity is toward the chase point
        /// </summary>
        Velocity_AlongIfVelocityToward,
        /// <summary>
        /// Drag is only applied along the part of the velocity that is along the direction to the chase point.
        /// But only if that velocity is away from chase point
        /// </summary>
        Velocity_AlongIfVelocityAway,
        /// <summary>
        /// Drag is only applied along the part of the velocity that is othrogonal to the direction to the chase point
        /// </summary>
        Velocity_Orth
    }

    #endregion
}
