using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.HelperClassesCore
{
    #region Struct: VectorInt

    public struct VectorInt : IComparable<VectorInt>, IComparable, IEquatable<VectorInt>
    {
        #region Constructor

        public VectorInt(int x, int y)
        {
            _x = x;
            _y = y;
        }

        #endregion

        #region IComparable<VectorInt> Members

        public int CompareTo(VectorInt other)
        {
            // X then Y
            int retVal = _x.CompareTo(other.X);
            if (retVal != 0)
            {
                return retVal;
            }

            return _y.CompareTo(other.Y);
        }

        #endregion
        #region IComparable Members

        public int CompareTo(object obj)
        {
            if (obj is VectorInt)
            {
                return CompareTo((VectorInt)obj);
            }
            else
            {
                return 1;
            }
        }

        #endregion
        #region IEquatable<VectorInt> Members

        public bool Equals(VectorInt other)
        {
            return _x == other.X && _y == other.Y;
        }

        #endregion

        #region Public Properties

        private int _x;
        public int X
        {
            get
            {
                return _x;
            }
            set
            {
                _x = value;
            }
        }

        private int _y;
        public int Y
        {
            get
            {
                return _y;
            }
            set
            {
                _y = value;
            }
        }

        #endregion

        #region Operator Overloads

        public static VectorInt operator -(VectorInt vector)
        {
            return new VectorInt(-vector.X, -vector.Y);
        }
        public static VectorInt operator -(VectorInt vector1, VectorInt vector2)
        {
            return new VectorInt(vector1.X - vector2.X, vector1.Y - vector2.Y);
        }
        public static VectorInt operator *(int scalar, VectorInt vector)
        {
            return new VectorInt(vector.X * scalar, vector.Y * scalar);
        }
        public static VectorInt operator *(VectorInt vector, int scalar)
        {
            return new VectorInt(vector.X * scalar, vector.Y * scalar);
        }
        public static VectorInt operator /(VectorInt vector, int scalar)
        {
            return new VectorInt(vector.X / scalar, vector.Y / scalar);
        }
        public static VectorInt operator /(VectorInt vector, double scalar)
        {
            return new VectorInt((vector.X / scalar).ToInt_Round(), (vector.Y / scalar).ToInt_Round());
        }
        public static VectorInt operator +(VectorInt vector1, VectorInt vector2)
        {
            return new VectorInt(vector1.X + vector2.X, vector1.Y + vector2.Y);
        }
        public static bool operator ==(VectorInt vector1, VectorInt vector2)
        {
            return vector1.X == vector2.X && vector1.Y == vector2.Y;
        }
        public static bool operator !=(VectorInt vector1, VectorInt vector2)
        {
            return vector1.X != vector2.X || vector1.Y != vector2.Y;
        }

        #endregion
        #region Public Methods

        public override string ToString()
        {
            return string.Format("{0}, {1}", _x, _y);
        }

        #endregion
    }

    #endregion

    #region Struct: RectInt

    public struct RectInt
    {
        #region Constructor

        public RectInt(int width, int height)
        {
            _x = 0;
            _y = 0;
            _width = width;
            _height = height;
        }
        public RectInt(VectorInt size)
        {
            _x = 0;
            _y = 0;
            _width = size.X;
            _height = size.Y;
        }

        public RectInt(VectorInt location, VectorInt size)
        {
            _x = location.X;
            _y = location.Y;
            _width = size.X;
            _height = size.Y;
        }
        public RectInt(int x, int y, int width, int height)
        {
            _x = x;
            _y = y;
            _width = width;
            _height = height;
        }

        #endregion

        #region Public Properties

        private int _x;
        public int X
        {
            get
            {
                return _x;
            }
            set
            {
                _x = value;
            }
        }

        private int _y;
        public int Y
        {
            get
            {
                return _y;
            }
            set
            {
                _y = value;
            }
        }

        private int _width;
        public int Width
        {
            get
            {
                return _width;
            }
            set
            {
                _width = value;
            }
        }

        private int _height;
        public int Height
        {
            get
            {
                return _height;
            }
            set
            {
                _height = value;
            }
        }

        public int Left { get { return _x; } }
        public int Right { get { return _x + _width; } }
        public int Top { get { return _y; } }
        public int Bottom { get { return _y + _height; } }

        public VectorInt Position
        {
            get
            {
                return new VectorInt(_x, _y);
            }
        }
        public VectorInt Size
        {
            get
            {
                return new VectorInt(_width, _height);
            }
        }

        #endregion

        #region Public Methods

        public static RectInt? Intersect(RectInt rect1, RectInt rect2)
        {
            if (rect1.Right <= rect2.Left || rect2.Right <= rect1.Left || rect1.Bottom <= rect2.Top || rect2.Bottom <= rect1.Top)
            {
                return null;
            }

            int left = Math.Max(rect1.Left, rect2.Left);
            int top = Math.Max(rect1.Top, rect2.Top);

            int right = Math.Min(rect1.Right, rect2.Right);
            int bottom = Math.Min(rect1.Bottom, rect2.Bottom);

            return new RectInt(left, top, right - left, bottom - top);
        }

        public bool Contains(int x, int y)
        {
            return x >= this.Left && x <= this.Right && y >= this.Top && y <= this.Bottom;
        }

        public override string ToString()
        {
            return string.Format("pos({0}, {1}) size({2}, {3})", _x, _y, _width, _height);
        }

        #endregion
    }

    #endregion
}
