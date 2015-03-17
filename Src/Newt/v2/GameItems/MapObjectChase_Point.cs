using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Primitives3D;
using Game.Newt.v2.NewtonDynamics;

// The objects in this file chase a point (see GameTester\Documentation for a class diagram)
namespace Game.Newt.v2.GameItems
{
    #region Class: MapObject_ChasePoint_Direct

    /// <summary>
    /// This is an item that is currently selected.  It and any derived classes hold selection visuals
    /// </summary>
    /// <remarks>
    /// This class immediately sets the body's position to the chase point (or if IsSpring, it emulates a squared
    /// spring: f=kx^2)
    /// 
    /// This is called direct, because the intended use is the user dragging a body around (the chase point would
    /// be the mouse position).  The other classes are geared toward being used inside the game (snapping objects
    /// to a 2D plane, slaving the camera to a ball, then the camera ball will chase some projected point off the
    /// player, maybe a tractor beam, etc)
    /// </remarks>
    public class MapObject_ChasePoint_Direct : IDisposable
    {
        #region Declaration Section

        protected readonly Viewport3D _viewport;

        private readonly ScreenSpaceLines3D _springVisual;

        /// <summary>
        /// This only has meaning if they are using a spring.  This is the k in
        /// f=kx
        /// </summary>
        private readonly double _springConstant;

        /// <summary>
        /// This only has meaning if IsUsingSpring is true.
        /// True: Forces will be applied at the drag point.
        /// False: Force will be applied directly to the body.
        /// </summary>
        private readonly bool _shouldSpringCauseTorque;

        /// <summary>
        /// If they are using a spring, this is the point to move to
        /// </summary>
        private Point3D? _desiredPoint = null;

        bool _isDamped = false;
        private double _existingDamping;
        private Vector3D _existingAngularDamping;

        #endregion

        #region Constructor

        public MapObject_ChasePoint_Direct(IMapObject item, Vector3D offset, bool shouldMoveWithSpring, bool shouldSpringCauseTorque, bool shouldDampenWhenSpring, Viewport3D viewport, Color? springColor = null)
        {
            this.Item = item;
            this.Offset = item.PhysicsBody.DirectionFromWorld(offset);       // convert to model coords
            _viewport = viewport;

            this.SpringForceMultiplier = 1d;

            // Newton uses zero (or maybe negative?) mass for bodies that ignore physics.  So a spring is only effective on
            // bodies with mass
            //TODO: See if PhysicsBody.IsFrozen will block the spring
            if (shouldMoveWithSpring && item.PhysicsBody.MassMatrix.Mass > 0)
            {
                #region Init spring

                this.IsUsingSpring = true;
                this.ShouldDampenWhenSpring = shouldDampenWhenSpring;
                _shouldSpringCauseTorque = shouldSpringCauseTorque;

                if (springColor != null)
                {
                    _springVisual = new ScreenSpaceLines3D();
                    _springVisual.Thickness = 1d;
                    _springVisual.Color = springColor.Value;
                    _viewport.Children.Add(_springVisual);
                }
                else
                {
                    _springVisual = null;
                }

                _springConstant = item.PhysicsBody.MassMatrix.Mass * 50d;

                this.Item.PhysicsBody.ApplyForceAndTorque += new EventHandler<NewtonDynamics.BodyApplyForceAndTorqueArgs>(PhysicsBody_ApplyForceAndTorque);

                #endregion
            }
            else
            {
                #region Init direct move

                this.IsUsingSpring = false;
                this.ShouldDampenWhenSpring = false;
                _shouldSpringCauseTorque = false;
                _springVisual = null;
                _springConstant = 0d;

                #endregion
            }
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
                StopDragging();

                if (this.IsUsingSpring)
                {
                    this.Item.PhysicsBody.ApplyForceAndTorque -= new EventHandler<NewtonDynamics.BodyApplyForceAndTorqueArgs>(PhysicsBody_ApplyForceAndTorque);
                }

                if (_viewport != null && _springVisual != null)
                {
                    _viewport.Children.Remove(_springVisual);
                }
            }
        }

        #endregion

        #region Public Properties

        public readonly IMapObject Item;
        public Vector3D Offset;

        public double SpringForceMultiplier
        {
            get;
            set;
        }

        public readonly bool IsUsingSpring;
        public readonly bool ShouldDampenWhenSpring;

        #endregion

        #region Public Methods

        public void SetPosition(Point3D point)
        {
            if (this.IsUsingSpring)
            {
                // Control with a spring.  The real work is done in this.PhysicsBody_ApplyForceAndTorque
                _desiredPoint = point;

                if (!_isDamped && this.ShouldDampenWhenSpring)
                {
                    _existingDamping = this.Item.PhysicsBody.LinearDamping;
                    _existingAngularDamping = this.Item.PhysicsBody.AngularDamping;

                    // They get capped at one if I try to set it any higher
                    this.Item.PhysicsBody.LinearDamping = 1;
                    this.Item.PhysicsBody.AngularDamping = new Vector3D(1, 1, 1);

                    _isDamped = true;
                }
            }
            else
            {
                // Not using a spring, so set position directly
                //NOTE: For bodies that have mass, I think I read that it's not wise to set position directly.  While experimenting, bodies
                //sometimes don't collide with others while behing dragged around
                //NOTE: When setting position directly, there's no need to look at this.Offset, the mousemove method that calls this already
                //accounted for that
                this.Item.PhysicsBody.Position = point;
                this.Item.PhysicsBody.Velocity = new Vector3D(0, 0, 0);
                this.Item.PhysicsBody.AngularVelocity = new Vector3D(0, 0, 0);
            }
        }

        public void StopDragging()
        {
            if (_springVisual != null)
            {
                _springVisual.Points.Clear();
            }

            _desiredPoint = null;

            if (_isDamped)
            {
                this.Item.PhysicsBody.LinearDamping = _existingDamping;
                this.Item.PhysicsBody.AngularDamping = _existingAngularDamping;

                _isDamped = false;
            }
        }

        #endregion

        #region Event Listeners

        private void PhysicsBody_ApplyForceAndTorque(object sender, NewtonDynamics.BodyApplyForceAndTorqueArgs e)
        {
            // Clear visual
            if (_springVisual != null)
            {
                _springVisual.Points.Clear();
            }

            // See if there is anything to do
            if (_desiredPoint == null)
            {
                return;
            }

            Point3D current = e.Body.PositionToWorld(this.Offset.ToPoint());
            Point3D desired = _desiredPoint.Value;

            // I decided to add x^2, because a linear spring is too weak
            // f = k x^2
            Vector3D spring = desired - current;
            double length = spring.Length;
            spring = spring.ToUnit() * (_springConstant * this.SpringForceMultiplier * length * length);

            // Apply the force
            if (_shouldSpringCauseTorque)
            {
                e.Body.AddForceAtPoint(spring, current);
            }
            else
            {
                e.Body.AddForce(spring);
            }

            if (_isDamped)      // this.ShouldDampenWhenSpring is taken into account before setting _isDamped to true
            {
                // Body.LinearDamping isn't strong enough, so do some more
                //Vector3D drag = e.Body.Velocity * (-_springConstant * this.SpringForceMultiplier * .33d);
                Vector3D drag = e.Body.Velocity * (-_springConstant * .33d);
                e.Body.AddForce(drag);
            }

            // Update visual
            if (_springVisual != null)
            {
                _springVisual.AddLine(current, desired);
            }
        }

        #endregion
    }

    #endregion

    //TODO: Pass in ChasePointVelocity as well
    #region Class: MapObject_ChasePoint_Velocity

    /// <summary>
    /// Chases a point, modifies velocity directly.  The only modifier is max velocity
    /// </summary>
    /// <remarks>
    /// ChasePoint_Direct can also emulate a spring force, but is f=kx^2.  This class is f=kx, and can also
    /// be capped to a max speed
    /// </remarks>
    public class MapObject_ChasePoint_Velocity : IDisposable
    {
        #region Declaration Section

        /// <summary>
        /// If they are using a spring, this is the point to move to
        /// </summary>
        private Point3D? _desiredPoint = null;

        #endregion

        #region Constructor

        public MapObject_ChasePoint_Velocity(IMapObject item)
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

        private void PhysicsBody_ApplyForceAndTorque(object sender, BodyApplyForceAndTorqueArgs e)
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

    #region Class: MapObject_ChasePoint_Forces

    /// <summary>
    /// This chases a point
    /// </summary>
    public class MapObject_ChasePoint_Forces : IDisposable
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

        public MapObject_ChasePoint_Forces(IMapObject item, bool shouldCauseTorque = false)
        {
            this.Item = item;
            _shouldCauseTorque = shouldCauseTorque;
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

        public ChasePoint_Force[] Forces
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

        private void PhysicsBody_ApplyForceAndTorque(object sender, BodyApplyForceAndTorqueArgs e)
        {
            // See if there is anything to do
            if (_desiredPoint == null)
            {
                return;
            }

            //NOTE: Offset needs to be center of mass
            Point3D current = e.Body.PositionToWorld(e.Body.CenterOfMass + this.Offset);

            ChasePoint_GetForceArgs args = new ChasePoint_GetForceArgs(this.Item, _desiredPoint.Value - current);

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
            if (force != null && !force.Value.IsNearZero())
            {
                // Limit if exceeds this.MaxForce
                if (this.MaxForce != null && force.Value.LengthSquared > this.MaxForce.Value * this.MaxForce.Value)
                {
                    force = force.Value.ToUnit(false) * this.MaxForce.Value;
                }

                // Limit acceleration
                if (this.MaxAcceleration != null)
                {
                    double mass = Item.PhysicsBody.Mass;

                    //f=ma
                    double accel = force.Value.Length / mass;

                    if (accel > this.MaxAcceleration.Value)
                    {
                        force = force.Value.ToUnit(false) * (this.MaxAcceleration.Value * mass);
                    }
                }

                force = force.Value * this.Percent;

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
    #region Class: ChasePoint_Force

    public class ChasePoint_Force
    {
        #region Constructor

        public ChasePoint_Force(ChaseDirectionType direction, double value, bool isAccel = true, bool isSpring = false, bool isDistanceRadius = true, Tuple<double, double>[] gradient = null)
        {
            if (gradient != null && gradient.Length == 1)
            {
                throw new ArgumentException("Gradient must have at least two items if it is populated");
            }

            this.Direction = direction;
            this.Value = value;
            this.IsAccel = isAccel;
            this.IsSpring = isSpring;
            this.IsDistanceRadius = isDistanceRadius;

            if (gradient == null || gradient.Length == 0)
            {
                this.Gradient = null;
            }
            else
            {
                this.Gradient = gradient;
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
                return this.Direction != ChaseDirectionType.Direction;
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
        /// </summary>
        /// <remarks>
        /// Nothing in this class will prevent you from having this true and a gradient at the same time, but that
        /// would give pretty strange results
        /// </remarks>
        public readonly bool IsSpring;
        /// <summary>
        /// This only has meaning if distance is part of the equation (spring force, or gradients).
        /// True: Distance is percent of item radius
        /// False: Distance is actual world distance
        /// </summary>
        public readonly bool IsDistanceRadius;

        public readonly double Value;

        /// <summary>
        /// This specifies varying percents based on distance to target
        /// Item1: Distance
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
        public readonly Tuple<double, double>[] Gradient;

        #endregion

        #region Public Methods

        public Vector3D? GetForce(ChasePoint_GetForceArgs e)
        {
            if (!IsDirectionValid(e.IsVelocityAlongTowards))
            {
                return null;
            }

            Vector3D unit;
            double length;
            GetDesiredVector(out unit, out length, e, this.Direction);
            if (Math3D.IsNearZero(unit))
            {
                return null;
            }

            double force = this.Value;
            if (this.IsAccel)
            {
                // f=ma
                force *= e.ItemMass;
            }

            if (this.IsSpring)
            {
                force *= GetDistance(e.DirectionLength, e.Item.Radius);
            }

            if (this.IsDrag)
            {
                force *= -length;       // negative, because it needs to be a drag force
            }

            // Gradient %
            if (this.Gradient != null)
            {
                force *= GetGradientPercent(GetDistance(e.DirectionLength, e.Item.Radius), this.Gradient);
            }

            return unit * force;
        }

        public static double GetGradientPercent(double distance, Tuple<double, double>[] gradient)
        {
            // See if they are outside the gradient (if so, use that cap's %)
            if (distance <= gradient[0].Item1)
            {
                return gradient[0].Item2;
            }
            else if (distance >= gradient[gradient.Length - 1].Item1)
            {
                return gradient[gradient.Length - 1].Item2;
            }

            //  It is inside the gradient.  Find the two stops that are on either side
            for (int cntr = 0; cntr < gradient.Length - 1; cntr++)
            {
                if (distance > gradient[cntr].Item1 && distance <= gradient[cntr + 1].Item1)
                {
                    // LERP between the from % and to %
                    return UtilityCore.GetScaledValue(gradient[cntr].Item2, gradient[cntr + 1].Item2, gradient[cntr].Item1, gradient[cntr + 1].Item1, distance);        //NOTE: Not calling the capped overload, because max could be smaller than min (and capped would fail)
                }
            }

            throw new ApplicationException("Execution should never get here");
        }

        #endregion

        #region Private Methods

        private bool IsDirectionValid(bool isVelocityAlongTowards)
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

        private static void GetDesiredVector(out Vector3D unit, out double length, ChasePoint_GetForceArgs e, ChaseDirectionType direction)
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

        private double GetDistance(double distance, double itemRadius)
        {
            if (this.IsDistanceRadius)
            {
                // A distance of 1 item radius will return 1.  2 radii will be 2, etc
                return distance / itemRadius;
            }
            else
            {
                return distance;
            }
        }

        #endregion
    }

    #endregion

    #region Class: ChasePoint_GetForceArgs

    public class ChasePoint_GetForceArgs
    {
        //TODO: Have a way to determine which props will be needed up front, and only populate those
        public ChasePoint_GetForceArgs(IMapObject item, Vector3D direction)
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
    #region Enum: ChaseDirectionType

    public enum ChaseDirectionType
    {
        //------ this is an attraction force

        /// <summary>
        /// The force is along the direction vector
        /// </summary>
        Direction,

        //------ everything below is a drag force

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
