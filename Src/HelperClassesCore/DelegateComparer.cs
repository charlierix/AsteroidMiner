using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.HelperClassesCore
{
    /// <summary>
    /// This is a helper class for linq statements that want an IEqualityComparer
    /// </summary>
    /// <remarks>
    /// Got this here:
    /// http://stackoverflow.com/questions/4607485/linq-distinct-use-delegate-for-equality-comparer
    /// </remarks>
    public class DelegateComparer<T> : IEqualityComparer<T>
    {
        private readonly Func<T, T, bool> _equals;
        private readonly Func<T, int> _getHashCode;

        public DelegateComparer(Func<T, T, bool> equals)
            : this(equals, null) { }
        public DelegateComparer(Func<T, T, bool> equals, Func<T, int> getHashCode)
        {
            _equals = equals;
            _getHashCode = getHashCode;
        }

        public bool Equals(T arg1, T arg2)
        {
            return _equals(arg1, arg2);
        }

        public int GetHashCode(T a)
        {
            if (_getHashCode != null)
            {
                return _getHashCode(a);
            }
            else
            {
                //The problem with this code is that it does not follow the rules for GetHashCode and Equals (see msdn.microsoft.com/en-us/library/system.object.gethashcode.aspx). Any time you use the first overload on a custom class, you are very likely to get incorrect results. GetHashCode must return the same value for two objects when Equals returns true. –  Gideon Engelberth Jan 5 '11 at 18:40
                //return a.GetHashCode();

                // This doesn't give great hashing, but at least it's valid
                return 1;
            }
        }
    }
}
