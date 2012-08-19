using System;
using System.Windows;
using System.Collections.Generic;

namespace Game.Newt.NewtonDynamics_153
{
    public abstract partial class UserJointConstraint : DependencyObject
	{
		#region Declaration Section

		private const double STIFFNESS_DEFAULT = 0.9;
        private List<double> _averages = new List<double>();

        private UserJointConstraintCollection _parentCollection;

		#endregion

		#region Public Properties

		#region DependencyProperty: MinFriction

		public static readonly DependencyProperty MinFrictionProperty = DependencyProperty.Register("MinFriction", typeof(double?), typeof(UserJointConstraint), new PropertyMetadata(OnMinFrictionChanged));

		protected static void OnMinFrictionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if ((e.NewValue != null) && ((double)e.NewValue > 0))
				throw new ArgumentOutOfRangeException(
					e.Property.Name,
					e.NewValue,
					string.Format("The {0} Property has to be zero or negative.", e.Property.Name));
		}

		public double? MinFriction
		{
			get { return (double?)GetValue(MinFrictionProperty); }
			set { SetValue(MinFrictionProperty, value); }
		}

		#endregion
		#region DependencyProperty: MaxFriction

		public static readonly DependencyProperty MaxFrictionProperty =DependencyProperty.Register("MaxFriction", typeof(double?), typeof(UserJointConstraint), new PropertyMetadata(OnMaxFrictionChanged));

		protected static void OnMaxFrictionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if ((e.NewValue != null) && ((double)e.NewValue < 0))
				throw new ArgumentOutOfRangeException(
					e.Property.Name,
					e.NewValue,
					string.Format("The {0} Property has to be zero or positive.", e.Property.Name));
		}

		public double? MaxFriction
		{
			get { return (double?)GetValue(MaxFrictionProperty); }
			set { SetValue(MaxFrictionProperty, value); }
		}

		#endregion
		#region DependencyProperty: Acceleration

		public static readonly DependencyProperty AccelerationProperty = DependencyProperty.Register("Acceleration", typeof(double?), typeof(UserJointConstraint));

		public double? Acceleration
		{
			get { return (double?)GetValue(AccelerationProperty); }
			set { SetValue(AccelerationProperty, value); }
		}

		#endregion
		#region DependencyProperty: Stiffness

		public static readonly DependencyProperty StiffnessProperty = DependencyProperty.Register("Stiffness", typeof(double), typeof(UserJointConstraint), new PropertyMetadata(STIFFNESS_DEFAULT, OnStiffnessChanged));

		protected static void OnStiffnessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			double value = (double)e.NewValue;

			if ((value < 0) || (value > 1))
				throw new ArgumentOutOfRangeException(
					e.Property.Name,
					e.NewValue,
					string.Format("The {0} Property has to be between 0 and 1.", e.Property.Name));
		}

		public double Stiffness
		{
			get { return (double)GetValue(StiffnessProperty); }
			set { SetValue(StiffnessProperty, value); }
		}

		#endregion
		#region DependencyProperty: SpringDamper

		public static readonly DependencyProperty SpringDamperProperty = DependencyProperty.Register("SpringDamper", typeof(double?), typeof(UserJointConstraint), new PropertyMetadata(OnSpringChanged));

		protected static void OnSpringChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (e.NewValue != null)
			{
				UserJointConstraint constraint = (UserJointConstraint)d;

				if (constraint.Stiffness >= 1)
					throw new ArgumentOutOfRangeException(
						StiffnessProperty.Name,
						constraint.Stiffness,
						string.Format("The {0} Property has to be less than 1 when the spring properties are set.", e.Property.Name));

				if ((double)e.NewValue <= 0)
					throw new ArgumentOutOfRangeException(
						e.Property.Name,
						e.NewValue,
						string.Format("The {0} Property has to be positive", e.Property.Name));
			}
		}

		public double? SpringDamper
		{
			get { return (double?)GetValue(SpringDamperProperty); }
			set { SetValue(SpringDamperProperty, value); }
		}

		#endregion
		#region DependencyProperty: SpringStiffness

		public static readonly DependencyProperty SpringStiffnessProperty = DependencyProperty.Register("SpringStiffness", typeof(double?), typeof(UserJointConstraint), new PropertyMetadata(OnSpringChanged));

		public double? SpringStiffness
		{
			get { return (double?)GetValue(SpringStiffnessProperty); }
			set { SetValue(SpringStiffnessProperty, value); }
		}

		#endregion
		#region DependencyProperty: MaxForce

		public static readonly DependencyProperty MaxForceProperty = DependencyProperty.Register("MaxForce", typeof(double), typeof(UserJointConstraint), new PropertyMetadata(0.0));

		public double MaxForce
		{
			get { return (double)GetValue(MaxForceProperty); }
			set { SetValue(MaxForceProperty, value); }
		}

		#endregion
		#region DependencyProperty: ForceAverageSampleCount

		public static readonly DependencyProperty ForceAverageSampleCountProperty = DependencyProperty.Register("AverageForceSampleCount", typeof(int), typeof(UserJointBase), new PropertyMetadata(1, ForceAverageSampleCountPropertyChanged));

		private static void ForceAverageSampleCountPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var constraint = (UserJointConstraint)d;

			constraint._averages.Clear();
			constraint.Force = 0;
		}

		public int ForceAverageSampleCount
		{
			get { return (int)GetValue(ForceAverageSampleCountProperty); }
			set { SetValue(ForceAverageSampleCountProperty, value); }
		}

		#endregion
		#region DependencyProperty: Force

		private static readonly DependencyPropertyKey ForcePropertyKey = DependencyProperty.RegisterReadOnly("ForceProperty", typeof(double), typeof(UserJointConstraint), new PropertyMetadata(0.0));

		public static readonly DependencyProperty ForceProperty = ForcePropertyKey.DependencyProperty;

		public double Force
		{
			get { return (double)GetValue(ForceProperty); }
			protected set { SetValue(ForcePropertyKey, value); }
		}

		#endregion
		#region DependencyProperty: ImpulseForce

		private static readonly DependencyPropertyKey ImpulseForcePropertyKey =
			DependencyProperty.RegisterReadOnly("ImpulseForce", typeof(double), typeof(UserJointConstraint),
			new PropertyMetadata(0.0));

		public static readonly DependencyProperty ImpulseForceProperty = ForcePropertyKey.DependencyProperty;

		public double ImpulseForce
		{
			get { return (double)GetValue(ImpulseForceProperty); }
			protected set { SetValue(ImpulseForcePropertyKey, value); }
		}

		#endregion

		#endregion
		#region Internal Properties

		/// <summary>
		/// by default a User constraint object will only constrain 1 Degree of freedom.
		/// </summary>
		/// <remarks>
		/// Override if the constraint object will add a multiple constraints.
		/// </remarks>
		/// 
		internal protected virtual int MaxDofCount
		{
			get { return 1; }
		}

		internal UserJointConstraintCollection ParentCollection
		{
			get { return _parentCollection; }
			set
			{
				if (value != _parentCollection)
				{
					_parentCollection = value;

					OnParentCollectionChanged();
				}
			}
		}

		#endregion

		#region Public Methods

		public virtual void Initialise(UserJointBase joint)
		{
		}

		#endregion
		#region Internal Methods

		internal protected abstract void OnApplyConstraint(UserJointBase joint);

		internal protected virtual void ApplyConstraint(UserJointBase joint)
		{
			OnApplyConstraint(joint);
		}

		internal protected void UpdateForce(UserJointBase joint, int index)
		{
			if (_averages.Count > ForceAverageSampleCount)
				_averages.RemoveAt(0);

			var impulseForce = joint.NewtonJoint.UserBilateralGetRowForce(index);
			_averages.Add(impulseForce);

			ImpulseForce = impulseForce;

			int count = _averages.Count;
			if (count >= ForceAverageSampleCount)
			{
				double force = 0;
				for (int i = 0; i < count; i++)
				{
					force += _averages[i];
				}

				Force = force / count;
			}
		}

		#endregion
		#region Protected Methods

		protected virtual void ApplyConstraintProperties(UserJointBase joint)
		{
			CJointUserDefinedBilateral handle = joint.NewtonJoint;

			object value = this.MinFriction;
			if (value != null)
				handle.UserBilateralRowMinimumFriction = (float)(double)value;

			value = this.MaxFriction;
			if (value != null)
				handle.UserBilateralRowMaximumFriction = (float)(double)value;

			value = this.Acceleration;
			if (value != null)
				handle.UserBilateralRowAcceleration = (float)(double)value;

			double dvalue = this.Stiffness;
			if (dvalue != STIFFNESS_DEFAULT)
				handle.UserBilateralRowStiffness = (float)dvalue;

			value = this.SpringStiffness;
			object value2 = this.SpringDamper;
			if ((value != null) && (value2 != null))
				handle.UserBilateralSetRowSpringDamperAcceleration((float)(double)value, (float)(double)value2);
		}

		protected virtual void OnParentCollectionChanged()
		{
		}

		#endregion
    }
}
