using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Game.Newt.v1.NewtonDynamics1.Api;
using System.Text;
using System.Windows;

namespace Game.Newt.v1.NewtonDynamics1
{
    public class UserJointBase : Joint
	{
		#region Events

		public event EventHandler Broke;

		#endregion

		#region Declaration Section

		private readonly UserJointConstraintCollection _constraints;

		#endregion

		#region Constructor

		public UserJointBase()
        {
            _constraints = new UserJointConstraintCollection(this);
            _constraints.CollectionChanged += _contraints_CollectionChanged;
		}

		#endregion

		#region Public Properties

		public new CJointUserDefinedBilateral NewtonJoint
        {
            get { return (CJointUserDefinedBilateral)base.NewtonJoint; }
		}

		#region DependencyProperty: IsEnabled

		public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.Register("IsEnabled", typeof(bool), typeof(UserJointBase), new PropertyMetadata(true));

		protected bool IsEnabled
		{
			get { return (bool)GetValue(IsEnabledProperty); }
			set { SetValue(IsEnabledProperty, value); }
		}

		#endregion
		#region DependencyProperty: MaxForce

		public static readonly DependencyProperty MaxForceProperty = DependencyProperty.Register("MaxForce", typeof(double), typeof(UserJointBase), new PropertyMetadata(0.0));

		public double MaxForce
		{
			get { return (double)GetValue(MaxForceProperty); }
			set { SetValue(MaxForceProperty, value); }
		}

		#endregion
		#region DependencyProperty: BreakStrategy

		public static readonly DependencyProperty BreakStrategyProperty = DependencyProperty.Register("BreakStrategy", typeof(BreakStrategy), typeof(Joint), new PropertyMetadata(BreakStrategy.DisableAndBecomeCollidable));

		public BreakStrategy BreakStrategy
		{
			get { return (BreakStrategy)GetValue(BreakStrategyProperty); }
			set { SetValue(BreakStrategyProperty, value); }
		}

		#endregion

		#endregion
		#region Protected Properties

		protected UserJointConstraintCollection Constraints
		{
			get
			{
				VerifyAccess();
				return _constraints;
			}
		}

		#endregion

		#region Public Methods

		public void Break()
        {
            OnBroke();
		}

		#endregion
		#region Protected Methods

		protected void OnBroke()
		{
			switch (BreakStrategy)
			{
				case BreakStrategy.DisableAndBecomeCollidable:
					IsEnabled = false;
					CollisionState = CollisionState.EnableCollisions;
					break;
				case BreakStrategy.DisableJoint:
					IsEnabled = false;
					break;
				case BreakStrategy.DisposeJoint:
					Dispose();
					break;
			}

			if (Broke != null)
				Broke(this, EventArgs.Empty);
		}

		#endregion

		#region Event Listeners

		private void _contraints_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if ((e.Action == NotifyCollectionChangedAction.Add) ||
				(e.Action == NotifyCollectionChangedAction.Remove) ||
				(e.Action == NotifyCollectionChangedAction.Reset))
			{
				RebuildJoint();
			}
		}

		#endregion
		#region Overrides

		protected override CJoint OnInitialise()
		{
			CJointUserDefinedBilateral joint = new CJointUserDefinedBilateral(this.World.NewtonWorld);
			int count = 0;
			foreach (UserJointConstraint constraint in _constraints)
			{
				count += constraint.MaxDofCount;
			}

			joint.CreateUserBilateral(count, joint_Callback, this.ChildBody.NewtonBody, this.ParentBody.NewtonBody);
			return joint;
		}

		protected override void AfterInitialise()
		{
			base.AfterInitialise();

			foreach (UserJointConstraint constraint in _constraints)
			{
				constraint.Initialise(this);
			}
		}

		#endregion

		#region Private Methods

		private void joint_Callback(object sender, CUserBilateralEventArgs e)
        {
            if (!IsEnabled)
                return;

            foreach (UserJointConstraint constraint in _constraints)
            {
                constraint.ApplyConstraint(this);
            }

            CJointUserDefinedBilateral handle = this.NewtonJoint;
            double maxForce = 0;
            // break any constraints
            for(int i = 0; i < _constraints.Count; i++)
            {
                _constraints[i].UpdateForce(this, i);
                double force = _constraints[i].Force;

                if (force > maxForce)
                    maxForce = force;

                if ((_constraints[i].MaxForce > 0) &&
                    (force > _constraints[i].MaxForce))
                {
                    OnBroke();
                    return;
                }
            }

            double max = this.MaxForce;
            if ((max > 0) && (maxForce > max))
            {
                OnBroke();
            }
        }

		#endregion
	}

	#region Enum: BreakStrategy

	public enum BreakStrategy
	{
		DisableAndBecomeCollidable,
		DisableJoint,
		DisposeJoint
	}

	#endregion
}
