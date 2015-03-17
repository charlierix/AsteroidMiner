using System;
using System.Collections.Specialized;
using System.Windows.Markup;
using System.Xml.Serialization;

namespace Game.Newt.v1.NewtonDynamics1
{
    [ContentProperty("Constraints")]
    public class UserJoint : UserJointBase, IAddChild
    {
        #region IAddChild Members

        public void AddChild(object value)
        {
            if (value == null) throw new ArgumentNullException("value");

            if (!(value is UserJointConstraint))
                throw new ArgumentException("Only UserJointConstraint supported.", "value");

            VerifyAccess();
            this.Constraints.Add((UserJointConstraint)value);
        }

        public void AddText(string text)
        {
            throw new ArgumentException("User Joints do not support text.", "text");
        }

        #endregion

		#region Public Properties

		public new UserJointConstraintCollection Constraints
		{
			get { return base.Constraints; }
		}

		#endregion
	}
}
