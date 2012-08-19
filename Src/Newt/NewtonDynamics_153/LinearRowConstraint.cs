using System.Windows;
using System.Windows.Media.Media3D;
using Game.Newt.HelperClasses;

namespace Game.Newt.NewtonDynamics_153
{
    public class LinearRowConstraint : UserJointConstraint
    {
        #region ParentPivotPointProperty

        public static readonly DependencyProperty ParentPivotPointProperty =
            DependencyProperty.Register(
                "ParentPivotPoint",
                typeof(Point3D),
                typeof(LinearRowConstraint),
                new PropertyMetadata(new Point3D()));

        public Point3D ParentPivotPoint
        {
            get { return (Point3D)GetValue(ParentPivotPointProperty); }
            set { SetValue(ParentPivotPointProperty, value); }
        }

        #endregion

        #region ChildPivotPointProperty

        public static readonly DependencyProperty ChildPivotPointProperty =
            DependencyProperty.Register(
                "ChildPivotPoint",
                typeof(Point3D),
                typeof(LinearRowConstraint),
                new PropertyMetadata(new Point3D()));

        public Point3D ChildPivotPoint
        {
            get { return (Point3D)GetValue(ChildPivotPointProperty); }
            set { SetValue(ChildPivotPointProperty, value); }
        }

        #endregion

        #region DirectionProperty

        public static readonly DependencyProperty DirectionProperty =
            DependencyProperty.Register(
                "Direction",
                typeof(Vector3D),
                typeof(LinearRowConstraint),
                new PropertyMetadata(new Vector3D()));

        public Vector3D Direction
        {
            get { return (Vector3D)GetValue(DirectionProperty); }
            set { SetValue(DirectionProperty, value); }
        }

        #endregion

        private Point3D _parentPivotPoint;

        public override void Initialise(UserJointBase joint)
        {
            base.Initialise(joint);

            Update();
        }

        protected internal override void OnApplyConstraint(UserJointBase joint)
        {
            Matrix3D m = Joint.BodyToWorldMatrix(joint.ChildBody, this.ParentPivotPoint, this.Direction);
            Vector3D childPivotPoint = Math3D.GetOffset(ref m);

            Vector3D parentPivotPoint = (Vector3D)Joint.BodyToWorld(joint.ParentBody, _parentPivotPoint);

            joint.NewtonJoint.UserBilateralAddLinearRow(
                childPivotPoint, parentPivotPoint,
                Math3D.GetFrontVector(ref m)
                );
            ApplyConstraintProperties(joint);
        }

        private void Update()
        {
            if ((ParentCollection != null) && ParentCollection.OwnerJoint.IsInitialised)
            {
                _parentPivotPoint =
                    Joint.BodyToBody(ParentCollection.OwnerJoint.ChildBody,
                                     ParentCollection.OwnerJoint.ParentBody,
                                     this.ParentPivotPoint);
            }
        }
    }
}
