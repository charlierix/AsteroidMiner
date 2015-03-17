using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Markup;

namespace Game.Newt.v1.NewtonDynamics1
{
    public class UserConstraintGroup : UserJointConstraint,
        IAddChild
    {
        private UserJointConstraintCollection _children;

        internal protected override int MaxDofCount
        {
            get
            {
                return _children.MaxDofCount;
            }
        }

        public override void Initialise(UserJointBase joint)
        {
            base.Initialise(joint);

            foreach (UserJointConstraint constraint in _children)
                constraint.Initialise(joint);
        }

        public UserConstraintGroup()
        {
            _children = new UserJointConstraintCollection(this);
        }

        public UserJointConstraintCollection Children
        {
            get { return _children; }
        }

        #region IAddChild Members

        public void AddChild(object value)
        {
            if (value == null) throw new ArgumentNullException("value");

            if (!(value is UserJointConstraint))
                throw new ArgumentException("Only UserJointConstraint supported.", "value");

            VerifyAccess();
            _children.Add((UserJointConstraint)value);
        }

        public void AddText(string text)
        {
            throw new ArgumentException("User Joints do not support text.", "text");
        }

        #endregion

        protected internal override void ApplyConstraint(UserJointBase joint)
        {
            foreach (UserJointConstraint constraint in _children)
            {
                constraint.ApplyConstraint(joint);
            }
        }

        protected internal override void OnApplyConstraint(UserJointBase joint)
        {
            //
        }
    }
}
