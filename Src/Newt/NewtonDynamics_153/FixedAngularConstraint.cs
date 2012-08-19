using System;
using System.Windows;
using System.Windows.Media.Media3D;

using Game.Newt.HelperClasses;

namespace Game.Newt.NewtonDynamics_153
{
    public class FixedAngularConstraint : UserJointConstraint
    {
        #region DirectionProperty

        public static readonly DependencyProperty DirectionProperty =
            DependencyProperty.Register(
                "Direction",
                typeof(Vector3D),
                typeof(FixedAngularConstraint),
                new PropertyMetadata(new Vector3D(0, 0, 1), OnChanged));

        private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FixedAngularConstraint constraint = (FixedAngularConstraint)d;

            constraint.Update();
        }

        public Vector3D Direction
        {
            get { return (Vector3D)GetValue(DirectionProperty); }
            set
            {
                value.Normalize();
                SetValue(DirectionProperty, value);
            }
        }

        #endregion

        private Vector3D _parentDirection;

        public override void Initialise(UserJointBase joint)
        {
            base.Initialise(joint);

            Update();
        }

        internal protected override int MaxDofCount
        {
            get { return 3; }
        }

        protected internal override void OnApplyConstraint(UserJointBase joint)
        {
            Matrix3D m = Joint.BodyToWorldMatrix(joint.ParentBody, _parentDirection);
            Matrix3D c = Joint.BodyToWorldMatrix(joint.ChildBody, this.Direction);

            double sa = Vector3D.DotProduct(Vector3D.CrossProduct(Math3D.GetUpVector(ref c), Math3D.GetUpVector(ref m)), Math3D.GetFrontVector(ref c));
            double ca = Vector3D.DotProduct(Math3D.GetUpVector(ref c), Math3D.GetUpVector(ref m));
            double a = Math.Atan2(sa, ca);

            joint.NewtonJoint.UserBilateralAddAngularRow((float)a, Math3D.GetFrontVector(ref m));
            ApplyConstraintProperties(joint);

            sa = Vector3D.DotProduct(Vector3D.CrossProduct(Math3D.GetRightVector(ref c), Math3D.GetRightVector(ref m)), Math3D.GetUpVector(ref c));
            ca = Vector3D.DotProduct(Math3D.GetRightVector(ref c), Math3D.GetRightVector(ref m));
            a = Math.Atan2(sa, ca);

            joint.NewtonJoint.UserBilateralAddAngularRow((float)a, Math3D.GetUpVector(ref m));
            ApplyConstraintProperties(joint);

            sa = Vector3D.DotProduct(Vector3D.CrossProduct(Math3D.GetFrontVector(ref c), Math3D.GetFrontVector(ref m)), Math3D.GetRightVector(ref c));
            ca = Vector3D.DotProduct(Math3D.GetFrontVector(ref c), Math3D.GetFrontVector(ref m));
            a = Math.Atan2(sa, ca);

            joint.NewtonJoint.UserBilateralAddAngularRow((float)a, Math3D.GetRightVector(ref m));
            ApplyConstraintProperties(joint);
        }

        private void Update()
        {
            if ((ParentCollection != null) && ParentCollection.OwnerJoint.IsInitialised)
            {
                _parentDirection =
                    Joint.BodyToBody(ParentCollection.OwnerJoint.ChildBody,
                                     ParentCollection.OwnerJoint.ParentBody,
                                     this.Direction);
            }
        }
    }
}
