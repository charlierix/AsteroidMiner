using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Collections.ObjectModel;
using System.Windows;

namespace Game.Newt.NewtonDynamics_153
{
    public class UserJointConstraintCollection : ObservableCollection<UserJointConstraint>
    {
        private readonly DependencyObject _owner;

        public UserJointConstraintCollection(DependencyObject owner)
        {
            _owner = owner;
        }

        public int MaxDofCount
        {
            get
            {
                int result = 0;
                foreach(UserJointConstraint constraint in this)
                    result += constraint.MaxDofCount;

                return result;
            }
        }

        internal protected void NotifyItemChanged(UserJointConstraint item)
        {
            int i = IndexOf(item);
            if (i >= 0)
                this.OnCollectionChanged(
                    new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Replace,
                        item,
                        i, i)
                    );
        }

        internal UserJointBase OwnerJoint
        {
            get
            {
                if (_owner != null)
                {
                    UserJoint _joint = _owner as UserJoint;
                    if (_joint != null)
                        return _joint;
                    else
                        return ((UserJointConstraint)_owner).ParentCollection.OwnerJoint;
                }
                else
                    return null;
            }
        }

        internal void Initialise(UserJointConstraint item)
        {
            UserJointBase joint = this.OwnerJoint;
            if (joint != null)
                if (joint.IsInitialised)
                    item.Initialise(joint);
        }

        protected override void InsertItem(int index, UserJointConstraint item)
        {
            base.InsertItem(index, item);

            item.ParentCollection = this;

            Initialise(item);
        }

        protected override void RemoveItem(int index)
        {
            this[index].ParentCollection = null;

            base.RemoveItem(index);
        }
    }
}
