using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Game.HelperClassesWPF
{
    /// <summary>
    /// This is a way to set up animations on rotate transforms
    /// </summary>
    /// <remarks>
    /// This isn't meant to be used from xaml.  WPF has its own animate classes.  I just figured I'd write my own that is lightweight,
    /// simple to set up (it doesn't have its own timer, and will likely sit next to many other instances)
    /// 
    /// Each of the constructors define a different way to do animation.  Then call Tick on a regular basis
    /// </remarks>
    public class AnimateRotation
    {
        #region interface: IAnimateRotationWorker

        private interface IAnimateRotationWorker
        {
            void Tick(double elapsedTime);
        }

        #endregion
        #region class: FixedWorker

        private class FixedWorker : IAnimateRotationWorker
        {
            private readonly bool _isQuat;
            private readonly QuaternionRotation3D _transformQuat;
            private readonly AxisAngleRotation3D _transformAxis;
            private readonly double _delta;

            public FixedWorker(QuaternionRotation3D transform, double delta)
            {
                _isQuat = true;
                _transformQuat = transform;
                _transformAxis = null;
                _delta = delta;
            }
            public FixedWorker(AxisAngleRotation3D transform, double delta)
            {
                _isQuat = false;
                _transformAxis = transform;
                _transformQuat = null;
                _delta = delta;
            }

            public void Tick(double elapsedTime)
            {
                if (_isQuat)
                {
                    _transformQuat.Quaternion = new Quaternion(_transformQuat.Quaternion.Axis, _transformQuat.Quaternion.Angle + (_delta * elapsedTime));
                }
                else
                {
                    _transformAxis.Angle = _transformAxis.Angle + (_delta * elapsedTime);
                }
            }
        }

        #endregion
        #region class: AnyQuatWorker

        private class AnyQuatWorker : IAnimateRotationWorker
        {
            private readonly QuaternionRotation3D _transform;
            private readonly double _angleDelta;
            private readonly int _numFullRotations;

            private readonly double? _maxTransitionAngle;

            private double _degreesLeft = -1;
            private Quaternion _quatDelta;

            public AnyQuatWorker(QuaternionRotation3D transform, double angleDelta, int numFullRotations, double? maxTransitionAngle = null)
            {
                _transform = transform;
                _angleDelta = angleDelta;
                _numFullRotations = numFullRotations;
                _maxTransitionAngle = maxTransitionAngle;
            }

            public void Tick(double elapsedTime)
            {
                if (_degreesLeft <= 0)
                {
                    // Come up with a new destination
                    Quaternion rotateTo;
                    if (_maxTransitionAngle == null)
                    {
                        rotateTo = Math3D.GetRandomRotation();
                    }
                    else
                    {
                        Vector3D newAxis = Math3D.GetRandomVector_Cone(_transform.Quaternion.Axis, 0, _maxTransitionAngle.Value, 1, 1);
                        //NOTE: Once going in one direction, never change, because that would look abrupt (that's why this overload was
                        //used).  Don't want to use random, want to stay under 180, but the larger the angle, the longer it will take to get there, and will be more smooth
                        double newAngle = _transform.Quaternion.Angle + 178;
                        rotateTo = new Quaternion(newAxis, newAngle);
                    }

                    // Figure out how long it will take to get there
                    var delta = GetDelta(_transform.Quaternion, rotateTo, _angleDelta, _numFullRotations);
                    _quatDelta = delta.Item1;
                    _degreesLeft = delta.Item2;
                }

                // Rotate it
                double deltaAngle = _quatDelta.Angle * elapsedTime;
                _transform.Quaternion = _transform.Quaternion.RotateBy(new Quaternion(_quatDelta.Axis, deltaAngle));

                _degreesLeft -= Math.Abs(deltaAngle);
            }
        }

        #endregion
        #region class: ConeQuatWorker

        private class ConeQuatWorker : IAnimateRotationWorker
        {
            private readonly QuaternionRotation3D _transform;
            private readonly double _angleDelta;
            private readonly int _numFullRotations;

            private readonly Vector3D _centerAxis;
            private readonly double _maxConeAngle;

            private double _degreesLeft = -1;
            private Quaternion _quatDelta;

            public ConeQuatWorker(QuaternionRotation3D transform, Vector3D centerAxis, double maxConeAngle, double angleDelta, int numFullRotations)
            {
                _transform = transform;
                _centerAxis = centerAxis;
                _maxConeAngle = maxConeAngle;
                _angleDelta = angleDelta;
                _numFullRotations = numFullRotations;
            }

            public void Tick(double elapsedTime)
            {
                if (_degreesLeft <= 0)
                {
                    // Come up with a new destination
                    //Quaternion rotateTo = Math3D.GetRandomRotation(_centerAxis, _maxConeAngle);

                    Vector3D newAxis = Math3D.GetRandomVector_Cone(_centerAxis, 0, _maxConeAngle, 1, 1);
                    //NOTE: Once going in one direction, never change, because that would look abrupt (that's why this overload was
                    //used).  Don't want to use random, want to stay under 180, but the larger the angle, the longer it will take to get there, and will be more smooth
                    double newAngle = _transform.Quaternion.Angle + 178;
                    Quaternion rotateTo = new Quaternion(newAxis, newAngle);

                    // Figure out how long it will take to get there
                    var delta = GetDelta(_transform.Quaternion, rotateTo, _angleDelta, _numFullRotations);
                    _quatDelta = delta.Item1;
                    _degreesLeft = delta.Item2;
                }

                // Rotate it
                double deltaAngle = _quatDelta.Angle * elapsedTime;
                _transform.Quaternion = _transform.Quaternion.RotateBy(new Quaternion(_quatDelta.Axis, deltaAngle));

                _degreesLeft -= Math.Abs(deltaAngle);
            }
        }

        #endregion
        #region class: AnyQuatConeWorker

        private class AnyQuatConeWorker : IAnimateRotationWorker
        {
            private const double NEWANGLE = 90;

            private readonly QuaternionRotation3D _transform;

            private readonly double _maxTransitionConeAngle;
            private readonly double _angleBetweenTransitionsAdjusted;

            private double _currentPercent;
            private readonly double _destinationPercent;
            private Quaternion _from;
            private Quaternion _to;

            public AnyQuatConeWorker(QuaternionRotation3D transform, double maxTransitionConeAngle, double anglePerSecond, double angleBetweenTransitions)
            {
                _transform = transform;
                _maxTransitionConeAngle = maxTransitionConeAngle;
                _angleBetweenTransitionsAdjusted = anglePerSecond / NEWANGLE;
                _destinationPercent = angleBetweenTransitions / NEWANGLE;
                _currentPercent = _destinationPercent + 1;      // set it greater so the first time tick is called, a new destination will be chosen
            }

            public void Tick(double elapsedTime)
            {
                if (_currentPercent >= _destinationPercent)
                {
                    _from = _transform.Quaternion;
                    _to = new Quaternion(Math3D.GetRandomVector_Cone(_from.Axis, 0, _maxTransitionConeAngle, 1, 1), _from.Angle + NEWANGLE);

                    _currentPercent = 0d;
                }

                _currentPercent += _angleBetweenTransitionsAdjusted * elapsedTime;

                Quaternion newQuat = Quaternion.Slerp(_from, _to, _currentPercent, true);
                if (Vector3D.DotProduct(_from.Axis, newQuat.Axis) < 0)
                {
                    // Once the from,to crosses over 360 degrees, slerp tries to reverse direction.  So fix it so the item continuously rotates
                    // in the same direction
                    newQuat = new Quaternion(newQuat.Axis * -1d, 360d - newQuat.Angle);
                }

                _transform.Quaternion = newQuat.ToUnit();
            }
        }

        #endregion

        #region Declaration Section

        /// <summary>
        /// I decided to go with small dedicated worker classes instead of a switch statement and a mess of semi
        /// common member variables
        /// </summary>
        private IAnimateRotationWorker _worker = null;

        #endregion

        #region Public Factory Methods

        // Use the factory methods to create an instance
        private AnimateRotation() { }

        /// <summary>
        /// This simply rotates the same direction and same speed forever
        /// </summary>
        public static AnimateRotation Create_Constant(AxisAngleRotation3D transform, double delta)
        {
            return new AnimateRotation()
            {
                _worker = new FixedWorker(transform, delta)
            };
        }
        /// <summary>
        /// This simply rotates the same direction and same speed forever
        /// </summary>
        public static AnimateRotation Create_Constant(QuaternionRotation3D transform, double angleDelta)
        {
            if (transform.Quaternion.IsIdentity)
            {
                throw new ArgumentException("The transform passed in is an identity quaternion, so the axis is unpredictable");
            }

            return new AnimateRotation()
            {
                _worker = new FixedWorker(transform, angleDelta)
            };
        }

        /// <summary>
        /// This will rotate to a random rotation.  Once at that destination, choose a new random rotation to
        /// go to, and rotate to that.  Always at a constant speed
        /// </summary>
        /// <remarks>
        /// There are no limits on what axis can be used.  The rotation will always be at the fixed speed
        /// </remarks>
        /// <param name="angleDelta">degrees per elapsed</param>
        /// <param name="numFullRotations">
        /// If 0, this will rotate directly to destination, then choose a new destination.
        /// If 1, this will rotate to the destination, then a full 360, then choose another destination, 2 does 2 rotations, etc
        /// </param>
        public static AnimateRotation Create_AnyOrientation(QuaternionRotation3D transform, double angleDelta, int numFullRotations = 0)
        {
            return new AnimateRotation()
            {
                _worker = new AnyQuatWorker(transform, angleDelta, numFullRotations)
            };
        }
        /// <summary>
        /// This will go from any angle to any angle, but at the time of choosing a new destination, it won't
        /// exceed a cone defined by maxTransitionAngle
        /// </summary>
        /// <remarks>
        /// Without this constraint, the changes in direction are pretty jarring.  This is an attempt to smooth that out
        /// </remarks>
        public static AnimateRotation Create_AnyOrientation_LimitChange(QuaternionRotation3D transform, double maxTransitionConeAngle, double anglePerSecond, double angleBetweenTransitions)
        {
            return new AnimateRotation()
            {
                _worker = new AnyQuatConeWorker(transform, maxTransitionConeAngle, anglePerSecond, angleBetweenTransitions)
            };
        }

        /// <summary>
        /// This limits destination orientation axiis to a cone
        /// </summary>
        public static AnimateRotation Create_LimitedOrientation(QuaternionRotation3D transform, Vector3D centerAxis, double maxConeAngle, double angleDelta, int numFullRotations = 0)
        {
            return new AnimateRotation()
            {
                _worker = new ConeQuatWorker(transform, centerAxis, maxConeAngle, angleDelta, numFullRotations)
            };
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// This is expected to be called on a regular basis
        /// </summary>
        /// <remarks>
        /// This class doesn't have its own timer.  I figured there would be a bunch of these instances, and it would be
        /// inneficient if each had its own timer.  Plus requiring a dispose to turn the timer off would be a headache
        /// </remarks>
        public void Tick(double elapsedTime)
        {
            _worker.Tick(elapsedTime);
        }

        #endregion

        #region Private Methods

        private static Tuple<Quaternion, double> GetDelta(Quaternion current, Quaternion destination, double angleDelta, int numFullRotations)
        {
            Quaternion delta = Math3D.GetRotation(current, destination);

            double degreesLeft = Math.Abs(delta.Angle) + (360d * numFullRotations);

            return Tuple.Create(new Quaternion(delta.Axis, angleDelta), degreesLeft);
        }

        #endregion
    }
}
