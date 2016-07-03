using System;
using System.Windows;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v1.NewtonDynamics1.Api;

namespace Game.Newt.v1.NewtonDynamics1
{
    public class BallSocketJoint : Joint
    {
        private bool _needsUpdate;

        #region PivotPointProperty

        public static readonly DependencyProperty PivotPointProperty =
            DependencyProperty.Register(
                "PivotPoint",
                typeof(Point3D),
                typeof(BallSocketJoint),
                new PropertyMetadata(new Point3D(), OnConstructorPropertyChanged));

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
                typeof(HingeJoint),
                new PropertyMetadata(new Vector3D(0, 0, 1), OnDirectionChanged));

        private static void OnDirectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BallSocketJoint joint = (BallSocketJoint)d;
            joint.Update();
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

        #region MaxConeAngleProperty

        public static readonly DependencyProperty MaxConeAngleProperty =
            DependencyProperty.Register(
                "MaxConeAngle",
                typeof(double),
                typeof(BallSocketJoint),
                new PropertyMetadata((double)0, MaxConeAngleChanged));

        private static void MaxConeAngleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BallSocketJoint joint = (BallSocketJoint)d;

            joint.Update();
        }

        public double MaxConeAngle
        {
            get { return (double)GetValue(MaxConeAngleProperty); }
            set { SetValue(MaxConeAngleProperty, value); }
        }

        #endregion

        #region MaxTwistAngle

        public static readonly DependencyProperty MaxTwistAngleProperty =
            DependencyProperty.Register(
                "MaxTwistAngle",
                typeof(double),
                typeof(BallSocketJoint),
                new PropertyMetadata((double)0, MaxTwistAngleChanged));

        private static void MaxTwistAngleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BallSocketJoint joint = (BallSocketJoint)d;

            joint.Update();
        }

        public double MaxTwistAngle
        {
            get { return (double)GetValue(MaxTwistAngleProperty); }
            set { SetValue(MaxTwistAngleProperty, value); }
        }

        #endregion

        public CJointBallSocket NetwonJoint
        {
            get { return (CJointBallSocket)base.NewtonJoint; }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (IsInitialised)
                    NewtonJoint.Ball -= InternalUserCallback;
            }

            base.Dispose(disposing);
        }

        protected override CJoint OnInitialise()
        {

            CJointBallSocket joint = new CJointBallSocket(this.World.NewtonWorld);
            joint.NewtonConstraintCreateBall(
                (Vector3D)BodyToWorld(this.ChildBody, this.PivotPoint),
                this.ChildBody.NewtonBody,
                this.ParentBody.NewtonBody);

            joint.BallSetConeLimits(
                BodyToWorld(this.ChildBody, Direction),
                (float)Math1D.DegreesToRadians(MaxConeAngle),
                (float)Math1D.DegreesToRadians(MaxTwistAngle));

            joint.Ball += InternalUserCallback;

            return joint;
        }

        public override void EndInit()
        {
            base.EndInit();

            if (_needsUpdate)
                Update();
        }

        public new CJointBallSocket NewtonJoint
        {
            get { return (CJointBallSocket)base.NewtonJoint; }
        }

        public event BallSocketJointEventHandler UserCallback;

        protected void Update()
        {
            if (IsInitialised)
            {
                if (IsInitialising)
                    _needsUpdate = true;
                else
                {
                    NetwonJoint.BallSetConeLimits(
                        BodyToWorld(this.ChildBody, Direction),
                        (float)Math1D.DegreesToRadians(MaxConeAngle),
                        (float)Math1D.DegreesToRadians(MaxTwistAngle));

                    _needsUpdate = false;
                }
            }
        }

        protected void OnUserCallback(BallSocketJointEventArgs e)
        {
            if (UserCallback != null)
                UserCallback(this, e);
        }

        private void InternalUserCallback(object sender, CBallEventArgs e)
        {
            OnUserCallback(new BallSocketJointEventArgs());
        }
    }

    public delegate void BallSocketJointEventHandler(BallSocketJoint joint, BallSocketJointEventArgs e);

    public class BallSocketJointEventArgs : EventArgs
    {
    }
}
