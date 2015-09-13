using System;
using System.Windows;
using System.Windows.Media.Media3D;

using Game.HelperClassesWPF;
using Game.Newt.v1.NewtonDynamics1.Api;

namespace Game.Newt.v1.NewtonDynamics1
{
    public class HingeJoint : Joint
    {
		#region Public Properties

		public new CJointHinge NewtonJoint
        {
            get { return (CJointHinge)base.NewtonJoint; }
		}

		#region DependencyProperty: PivotPoint

		public static readonly DependencyProperty PivotPointProperty = DependencyProperty.Register("PivotPoint", typeof(Point3D), typeof(HingeJoint), new PropertyMetadata(new Point3D(), OnConstructorPropertyChanged));

		public Point3D PivotPoint
		{
			get { return (Point3D)GetValue(PivotPointProperty); }
			set { SetValue(PivotPointProperty, value); }
		}

		#endregion
		#region DependencyProperty: Axis

		public static readonly DependencyProperty AxisProperty = DependencyProperty.Register("Axis", typeof(Vector3D), typeof(HingeJoint), new PropertyMetadata(new Vector3D(0, 0, 1), OnConstructorPropertyChanged));

		public Vector3D Axis
		{
			get { return (Vector3D)GetValue(AxisProperty); }
			set
			{
				value.Normalize();
				SetValue(AxisProperty, value);
			}
		}

		#endregion
		#region DependencyProperty: MinAngle

		public static readonly DependencyProperty MinAngleProperty = DependencyProperty.Register("MinAngle", typeof(double?), typeof(HingeJoint), new PropertyMetadata(null, OnUnPausePropertyChanged, OnCoerceMinMaxAngle));

		public static object OnCoerceMinMaxAngle(DependencyObject d, object baseValue)
		{
			if (baseValue != null)
			{
				double value = (double)baseValue;

				if (value < -180)
					return -180.0;
				else if (value > 180)
					return 180.0;
			}

			return baseValue;
		}

		public double? MinAngle
		{
			get { return (double?)GetValue(MinAngleProperty); }
			set { SetValue(MinAngleProperty, value); }
		}

		#endregion
		#region DependencyProperty: MaxAngle

		public static readonly DependencyProperty MaxAngleProperty = DependencyProperty.Register("MaxAngle", typeof(double?), typeof(HingeJoint), new PropertyMetadata(null, OnUnPausePropertyChanged, OnCoerceMinMaxAngle));

		public double? MaxAngle
		{
			get { return (double?)GetValue(MaxAngleProperty); }
			set { SetValue(MaxAngleProperty, value); }
		}

		#endregion
		#region DependencyProperty: Angle

		protected static readonly DependencyPropertyKey AnglePropertyKey = DependencyProperty.RegisterReadOnly("Angle", typeof(double), typeof(HingeJoint), new PropertyMetadata(0.0, OnUnPausePropertyChanged));

		public DependencyProperty AngleProperty = AnglePropertyKey.DependencyProperty;

		public double Angle
		{
			get { return (double)GetValue(AngleProperty); }
		}

		#endregion
		#region DependencyProperty: AngularDamperning

		protected static readonly DependencyProperty AngularDamperningProperty = DependencyProperty.Register("AngularDamperning", typeof(double), typeof(HingeJoint));

		public double AngularDamperning
		{
			get { return (double)GetValue(AngularDamperningProperty); }
			set { SetValue(AngularDamperningProperty, value); }
		}

		#endregion
		#region DependencyProperty: SetAngle

		public static readonly DependencyProperty SetAngleProperty = DependencyProperty.Register("SetAngle", typeof(double?), typeof(HingeJoint), new PropertyMetadata(null, OnUnPausePropertyChanged, OnCoerceSetAngleProperty));

		private static object OnCoerceSetAngleProperty(DependencyObject d, object baseValue)
		{
			double? value = (double?)baseValue;
			if (value != null)
			{
				HingeJoint joint = (HingeJoint)d;

				if ((joint.MinAngle != null) & (value < joint.MinAngle))
					value = joint.MinAngle;
				else if ((joint.MaxAngle != null) & (value > joint.MaxAngle))
					value = joint.MaxAngle;
			}

			return value;
		}

		public double? SetAngle
		{
			get { return (double?)GetValue(SetAngleProperty); }
			set { SetValue(SetAngleProperty, value); }
		}

		#endregion
		#region DependencyProperty: Torque

		protected static readonly DependencyProperty TorqueProperty = DependencyProperty.Register("Torque", typeof(double), typeof(HingeJoint), new PropertyMetadata(0.0, OnUnPausePropertyChanged));

		public double Torque
		{
			get { return (double)GetValue(TorqueProperty); }
			set { SetValue(TorqueProperty, value); }
		}

		#endregion
		#region DependencyProperty: SetAngleStiffness

		public static readonly DependencyProperty SetAngleStiffnessProperty = DependencyProperty.Register("SetAngleStiffness", typeof(double), typeof(HingeJoint), new PropertyMetadata(1.0, OnUnPausePropertyChanged, OnCoerceSetAngleStiffnessProperty));

		private static object OnCoerceSetAngleStiffnessProperty(DependencyObject d, object baseValue)
		{
			return MathUtils.MinMax(0, (double)baseValue, 1);
		}

		public double SetAngleStiffness
		{
			get { return (double)GetValue(SetAngleStiffnessProperty); }
			set { SetValue(SetAngleStiffnessProperty, value); }
		}

		#endregion

		#endregion

		#region Event Listeners

		private void joint_Hinge(object sender, CHingeEventArgs e)
        {
            double newAngle = Math1D.RadiansToDegrees(NewtonJoint.HingeAngle);
            double angle = this.Angle + AngleDiffence(this.Angle, newAngle);
            SetValue(AnglePropertyKey, angle);

            if (this.SetAngle != null)
            {
                e.Desc.m_Accel = NewtonJoint.HingeCalculateStopAlpha(e.Desc,
                                                                     Math1D.DegreesToRadians((float)MathUtils.MinMax(this.MinAngle, (double)this.SetAngle, this.MaxAngle)))

                                                                     * (float)this.SetAngleStiffness;
                e.ApplyConstraint = true;
            }
            else if ((this.MinAngle != null) && (angle < (double)this.MinAngle))
            {
                e.Desc.m_Accel = NewtonJoint.HingeCalculateStopAlpha(e.Desc,
                                                                     (float)Math1D.DegreesToRadians((double)this.MinAngle));
                e.ApplyConstraint = true;
            }
            else if ((this.MaxAngle != null) && (angle > (double)this.MaxAngle))
            {
                e.Desc.m_Accel = NewtonJoint.HingeCalculateStopAlpha(e.Desc,
                                                                     (float)Math1D.DegreesToRadians((double)this.MaxAngle));
                e.ApplyConstraint = true;
            }
            else
            {
                if (this.AngularDamperning != 0)
                {
                    // -(NewtonJoint.HingeOmega * e.Desc.m_Timestep * (float)this.AngularDamperning);
                    e.Desc.m_Accel = -(NewtonJoint.HingeOmega / e.Desc.m_Timestep) * (float)AngularDamperning;
                    e.ApplyConstraint = true;
                }

                if (this.Torque != 0)
                {
                    e.Desc.m_Accel += (float)this.Torque;
                    e.ApplyConstraint = true;
                }
            }
		}

		#endregion
		#region Overrides

		protected override CJoint OnInitialise()
		{
			CJointHinge joint = new CJointHinge(this.World.NewtonWorld);
			joint.CreateHinge(
				(Vector3D)BodyToWorld(this.ChildBody, this.PivotPoint),
				BodyToWorld(this.ChildBody, this.Axis),
				this.ChildBody.NewtonBody,
				this.ParentBody.NewtonBody);

			joint.Hinge += joint_Hinge;

			ClearValue(AnglePropertyKey);

			return joint;
		}

		#endregion

		#region Private Methods

		private static double FixAngle(double angle)
		{
			double result = angle % 360; // limit angle to only ever be 0 - 360

			if (result > 180)
				result = -180 + (result - 180);
			else
				if (result < -180)
					result = 180 + (result + 180);

			return result;
		}

		private static double AngleDiffence(double currentAngle, double newAngle)
        {
            double sin_da = (Math.Sin(newAngle) * Math.Cos(currentAngle)) - (Math.Cos(newAngle) * Math.Sin(currentAngle));
            double cos_da = (Math.Cos(newAngle) * Math.Cos(currentAngle)) + (Math.Sin(newAngle) * Math.Sin(currentAngle));

            return Math.Atan2(sin_da, cos_da);
		}

		#endregion

		/*
        public void BindMinMaxAngle()
        {
            BindingHelper.SetBinding(this, MinAngleProperty, MaxAngleProperty);
        }
        */

		//
		// torque = k * (desiredVelocity - Physics.GetHingeJointRotationSpeed(WheelFLJointID)); 
		//
	}
}
