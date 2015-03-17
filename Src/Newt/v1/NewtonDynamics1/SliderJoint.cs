using System;
using System.Collections.Generic;
using Game.Newt.v1.NewtonDynamics1.Api;
using System.Text;
using System.Windows;
using System.Windows.Media.Media3D;

namespace Game.Newt.v1.NewtonDynamics1
{
    public class SliderJoint : Joint
    {
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
                new PropertyMetadata(new Vector3D(), OnConstructorPropertyChanged));

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

        protected override CJoint OnInitialise()
        {
            CJointSlider joint = new CJointSlider(this.World.NewtonWorld);
            joint.CreateSlider(
                (Vector3D)BodyToWorld(this.ChildBody, this.PivotPoint),
                BodyToWorld(this.ChildBody, this.Direction),
                this.ChildBody.NewtonBody,
                this.ParentBody.NewtonBody);

            return joint;
        }
    }
}
