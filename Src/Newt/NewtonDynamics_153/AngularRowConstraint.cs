using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media.Media3D;

namespace Game.Newt.NewtonDynamics_153
{
    public class AngularRowConstraint : UserJointConstraint
    {
        #region AxisProperty

        public static readonly DependencyProperty AxisProperty =
            DependencyProperty.Register(
                "Axis",
                typeof(Vector3D),
                typeof(AngularRowConstraint),
                new PropertyMetadata(new Vector3D()));

        public Vector3D Axis
        {
            get { return (Vector3D)GetValue(AxisProperty); }
            set { SetValue(AxisProperty, value); }
        }

        #endregion

        #region RelativeAngleErrorProperty

        public static readonly DependencyProperty RelativeAngleErrorProperty =
            DependencyProperty.Register(
                "RelativeAngleError",
                typeof(double),
                typeof(AngularRowConstraint),
                new PropertyMetadata(0.0));

        public double RelativeAngleError
        {
            get { return (double)GetValue(RelativeAngleErrorProperty); }
            set { SetValue(RelativeAngleErrorProperty, value); }
        }

        #endregion

        protected internal override void OnApplyConstraint(UserJointBase joint)
        {
            joint.NewtonJoint.UserBilateralAddAngularRow(
                (float)RelativeAngleError,
                Joint.BodyToWorld(joint.ChildBody, this.Axis)
                );
            ApplyConstraintProperties(joint);
        }
    }
}
