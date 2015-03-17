using System.Windows;
using System.Windows.Media.Media3D;
using Game.HelperClassesWPF;

namespace Game.Newt.v1.NewtonDynamics1
{
    public class FixedLinearConstraint : UserJointConstraint
    {
        #region PivotPointProperty

        public static readonly DependencyProperty PivotPointProperty =
            DependencyProperty.Register(
                "PivotPoint",
                typeof(Point3D),
                typeof(FixedLinearConstraint),
                new PropertyMetadata(new Point3D(), OnChanged));

        private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FixedLinearConstraint constraint = (FixedLinearConstraint)d;

            constraint.Update();
        }

        public Point3D PivotPoint
        {
            get { return (Point3D)GetValue(PivotPointProperty); }
            set { SetValue(PivotPointProperty, value); }
        }

        #endregion

        #region DirectionProperty

        public static readonly DependencyProperty DirectionProperty =
            DependencyProperty.Register(
                "Direction",
                typeof(Vector3D),
                typeof(FixedLinearConstraint),
                new PropertyMetadata(new Vector3D(0,0,1), OnChanged));

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

        private Point3D _parentPivotPoint;

        internal protected override int MaxDofCount
        {
            get { return 3; }
        }

        protected internal override void OnApplyConstraint(UserJointBase joint)
        {
            Matrix3D m = Joint.BodyToWorldMatrix(joint.ChildBody, this.PivotPoint, this.Direction);
            Vector3D childPivotPoint = Math3D.GetOffset(ref m);

            Vector3D parentPivotPoint = (Vector3D)Joint.BodyToWorld(joint.ParentBody, _parentPivotPoint);

            joint.NewtonJoint.UserBilateralAddLinearRow(
                childPivotPoint, parentPivotPoint,
                Math3D.GetFrontVector(ref m)
                );
            ApplyConstraintProperties(joint);

            joint.NewtonJoint.UserBilateralAddLinearRow(
                childPivotPoint, parentPivotPoint,
                Math3D.GetUpVector(ref m)
                );
            ApplyConstraintProperties(joint);

            joint.NewtonJoint.UserBilateralAddLinearRow(
                childPivotPoint, parentPivotPoint,
                Math3D.GetRightVector(ref m)
                );
            ApplyConstraintProperties(joint);
        }

        public override void Initialise(UserJointBase joint)
        {
            base.Initialise(joint);

            Update();
        }

        private void Update()
        {
            if ((ParentCollection != null) && ParentCollection.OwnerJoint.IsInitialised)
            {
                _parentPivotPoint =
                    Joint.BodyToBody(ParentCollection.OwnerJoint.ChildBody,
                                     ParentCollection.OwnerJoint.ParentBody,
                                     this.PivotPoint);
            }
        }
    }
}
