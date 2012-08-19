using System;
using System.Collections.Generic;
using Game.Newt.NewtonDynamics_153.Api;
using System.Text;
using System.Windows;
using System.Windows.Media.Media3D;

namespace Game.Newt.NewtonDynamics_153
{
    public class UniversalJoint : Joint
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

        #region ChildAxisProperty

        public static readonly DependencyProperty ChildAxisProperty =
            DependencyProperty.Register(
                "ChildAxis",
                typeof(Vector3D),
                typeof(HingeJoint),
                new PropertyMetadata(new Vector3D(), OnConstructorPropertyChanged));

        public Vector3D ChildAxis
        {
            get { return (Vector3D)GetValue(ChildAxisProperty); }
            set
            {
                value.Normalize();
                SetValue(ChildAxisProperty, value);
            }
        }

        #endregion

        #region ParentAxisProperty

        public static readonly DependencyProperty ParentAxisProperty =
            DependencyProperty.Register(
                "ParentAxis",
                typeof(Vector3D),
                typeof(HingeJoint),
                new PropertyMetadata(new Vector3D(), OnConstructorPropertyChanged));

        public Vector3D ParentAxis
        {
            get { return (Vector3D)GetValue(ParentAxisProperty); }
            set
            {
                value.Normalize();
                SetValue(ParentAxisProperty, value);
            }
        }

        #endregion

        protected override CJoint OnInitialise()
        {
            CJointUniversal joint = new CJointUniversal(this.World.NewtonWorld);
            joint.CreateUniversal(
                (Vector3D)BodyToWorld(this.ChildBody, this.PivotPoint),
                this.ChildAxis,
                this.ParentAxis,
                this.ChildBody.NewtonBody,
                this.ParentBody.NewtonBody);

            return joint;
        }
    }
}
