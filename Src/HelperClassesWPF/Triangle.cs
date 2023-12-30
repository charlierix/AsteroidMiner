using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;

namespace Game.HelperClassesWPF
{
    //TODO: Make Triangle threadsafe instead of having a separate class
    #region class: Triangle

    //TODO:  Add methods like Clone, GetTransformed(transform), etc

    /// <summary>
    /// This stores 3 points explicitly - as opposed to TriangleIndexed, which stores ints that point to a list of points
    /// </summary>
    public class Triangle : ITriangle
    {
        #region Declaration Section

        // Caching these so that Enum.GetValues doesn't have to be called all over the place
        public static TriangleEdge[] Edges = (TriangleEdge[])Enum.GetValues(typeof(TriangleEdge));
        public static TriangleCorner[] Corners = (TriangleCorner[])Enum.GetValues(typeof(TriangleCorner));

        #endregion

        #region Constructor

        public Triangle()
        {
        }

        public Triangle(Point3D point0, Point3D point1, Point3D point2)
        {
            //TODO:  If the property sets have more added to them in the future, do that in this constructor as well
            //this.Point0 = point0;
            //this.Point1 = point1;
            //this.Point2 = point2;
            _point0 = point0;
            _point1 = point1;
            _point2 = point2;
            OnPointChanged();
        }

        #endregion

        #region ITriangle Members

        private Point3D? _point0 = null;
        public Point3D Point0
        {
            get
            {
                // I could put an if statement for null, but I want this to be as fast as possible, and .net will throw an exception anyway
                return _point0.Value;
            }
            set
            {
                _point0 = value;
                OnPointChanged();
            }
        }

        private Point3D? _point1 = null;
        public Point3D Point1
        {
            get
            {
                // I could put an if statement for null, but I want this to be as fast as possible, and .net will throw an exception anyway
                return _point1.Value;
            }
            set
            {
                _point1 = value;
                OnPointChanged();
            }
        }

        private Point3D? _point2 = null;
        public Point3D Point2
        {
            get
            {
                // I could put an if statement for null, but I want this to be as fast as possible, and .net will throw an exception anyway
                return _point2.Value;
            }
            set
            {
                _point2 = value;
                OnPointChanged();
            }
        }

        public Point3D[] PointArray
        {
            get
            {
                return new[] { _point0.Value, _point1.Value, _point2.Value };
            }
        }

        public Point3D this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return this.Point0;

                    case 1:
                        return this.Point1;

                    case 2:
                        return this.Point2;

                    default:
                        throw new ArgumentOutOfRangeException("index", "index can only be 0, 1, 2: " + index.ToString());
                }
            }
            set
            {
                switch (index)
                {
                    case 0:
                        this.Point0 = value;
                        break;

                    case 1:
                        this.Point1 = value;
                        break;

                    case 2:
                        this.Point2 = value;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException("index", "index can only be 0, 1, 2: " + index.ToString());
                }
            }
        }

        private Vector3D? _normal = null;
        /// <summary>
        /// This returns the triangle's normal.  Its length is the area of the triangle
        /// </summary>
        public Vector3D Normal
        {
            get
            {
                if (_normal == null)
                {
                    Vector3D normal, normalUnit;
                    double length;
                    CalculateNormal(out normal, out length, out normalUnit, this.Point0, this.Point1, this.Point2);

                    _normal = normal;
                    _normalLength = length;
                    _normalUnit = normalUnit;
                }

                return _normal.Value;
            }
        }
        private Vector3D? _normalUnit = null;
        /// <summary>
        /// This returns the triangle's normal.  Its length is one
        /// </summary>
        public Vector3D NormalUnit
        {
            get
            {
                if (_normalUnit == null)
                {
                    Vector3D normal, normalUnit;
                    double length;
                    CalculateNormal(out normal, out length, out normalUnit, this.Point0, this.Point1, this.Point2);

                    _normal = normal;
                    _normalLength = length;
                    _normalUnit = normalUnit;
                }

                return _normalUnit.Value;
            }
        }
        private double? _normalLength = null;
        /// <summary>
        /// This returns the length of the normal (the area of the triangle)
        /// NOTE:  Call this if you just want to know the length of the normal, it's cheaper than calling this.Normal.Length, since it's already been calculated
        /// </summary>
        public double NormalLength
        {
            get
            {
                if (_normalLength == null)
                {
                    Vector3D normal, normalUnit;
                    double length;
                    CalculateNormal(out normal, out length, out normalUnit, this.Point0, this.Point1, this.Point2);

                    _normal = normal;
                    _normalLength = length;
                    _normalUnit = normalUnit;
                }

                return _normalLength.Value;
            }
        }

        private double? _planeDistance = null;
        public double PlaneDistance
        {
            get
            {
                if (_planeDistance == null)
                {
                    _planeDistance = Math3D.GetPlaneOriginDistance(this.NormalUnit, this.Point0);
                }

                return _planeDistance.Value;
            }
        }

        private long? _token = null;
        public long Token
        {
            get
            {
                if (_token == null)
                {
                    _token = TokenGenerator.NextToken();
                }

                return _token.Value;
            }
        }

        public Point3D GetCenterPoint()
        {
            return GetCenterPoint(this.Point0, this.Point1, this.Point2);
        }
        public Point3D GetPoint(TriangleEdge edge, bool isFrom)
        {
            return GetPoint(this, edge, isFrom);
        }
        public Point3D GetCommonPoint(TriangleEdge edge0, TriangleEdge edge1)
        {
            return GetCommonPoint(this, edge0, edge1);
        }
        public Point3D GetUncommonPoint(TriangleEdge edge0, TriangleEdge edge1)
        {
            return GetUncommonPoint(this, edge0, edge1);
        }
        public Point3D GetOppositePoint(TriangleEdge edge)
        {
            return GetOppositePoint(this, edge);
        }
        public Point3D GetEdgeMidpoint(TriangleEdge edge)
        {
            return GetEdgeMidpoint(this, edge);
        }
        public double GetEdgeLength(TriangleEdge edge)
        {
            return GetEdgeLength(this, edge);
        }

        #endregion
        #region IComparable<ITriangle> Members

        /// <summary>
        /// I wanted to be able to use triangles as keys in a sorted list
        /// </summary>
        public int CompareTo(ITriangle other)
        {
            if (other == null)
            {
                // I'm greater than null
                return 1;
            }

            if (this.Token < other.Token)
            {
                return -1;
            }
            else if (this.Token > other.Token)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        #endregion

        #region Public Methods

        public static Point3D[] GetUniquePoints(IEnumerable<ITriangle> triangles)
        {
            return triangles.
                SelectMany(o => o.PointArray).
                Distinct((p1, p2) => Math3D.IsNearValue(p1, p2)).
                ToArray();
        }

        /// <summary>
        /// This helps a lot when looking at lists of triangles in the quick watch
        /// </summary>
        public override string ToString()
        {
            return string.Format("({0}) ({1}) ({2})",
                _point0 == null ? "null" : _point0.Value.ToString(2),
                _point1 == null ? "null" : _point1.Value.ToString(2),
                _point2 == null ? "null" : _point2.Value.ToString(2));
        }

        #endregion
        #region Internal Methods

        internal static void CalculateNormal(out Vector3D normal, out double normalLength, out Vector3D normalUnit, Point3D point0, Point3D point1, Point3D point2)
        {
            Vector3D dir1 = point0 - point1;
            Vector3D dir2 = point2 - point1;

            Vector3D triangleNormal = Vector3D.CrossProduct(dir2, dir1);

            normal = triangleNormal;
            normalLength = triangleNormal.Length;
            normalUnit = triangleNormal / normalLength;
        }

        internal static Point3D GetCenterPoint(Point3D point0, Point3D point1, Point3D point2)
        {
            //return ((triangle.Point0.ToVector() + triangle.Point1.ToVector() + triangle.Point2.ToVector()) / 3d).ToPoint();

            // Doing the math with doubles to avoid casting to vector
            double x = (point0.X + point1.X + point2.X) / 3d;
            double y = (point0.Y + point1.Y + point2.Y) / 3d;
            double z = (point0.Z + point1.Z + point2.Z) / 3d;

            return new Point3D(x, y, z);
        }

        internal static Point3D GetPoint(ITriangle triangle, TriangleEdge edge, bool isFrom)
        {
            switch (edge)
            {
                case TriangleEdge.Edge_01:
                    if (isFrom)
                    {
                        return triangle.Point0;
                    }
                    else
                    {
                        return triangle.Point1;
                    }

                case TriangleEdge.Edge_12:
                    if (isFrom)
                    {
                        return triangle.Point1;
                    }
                    else
                    {
                        return triangle.Point2;
                    }

                case TriangleEdge.Edge_20:
                    if (isFrom)
                    {
                        return triangle.Point2;
                    }
                    else
                    {
                        return triangle.Point0;
                    }

                default:
                    throw new ApplicationException("Unknown TriangleEdge: " + edge.ToString());
            }
        }

        internal static Point3D GetCommonPoint(ITriangle triangle, TriangleEdge edge0, TriangleEdge edge1)
        {
            Point3D[] points0 = new Point3D[] { triangle.GetPoint(edge0, true), triangle.GetPoint(edge0, false) };
            Point3D[] points1 = new Point3D[] { triangle.GetPoint(edge1, true), triangle.GetPoint(edge1, false) };

            // Find exact
            for (int cntr0 = 0; cntr0 < points0.Length; cntr0++)
            {
                for (int cntr1 = 0; cntr1 < points1.Length; cntr1++)
                {
                    if (points0[cntr0] == points1[cntr1])
                    {
                        return points0[cntr0];
                    }
                }
            }

            // Find close - execution should never get here, just being safe
            for (int cntr0 = 0; cntr0 < points0.Length; cntr0++)
            {
                for (int cntr1 = 0; cntr1 < points1.Length; cntr1++)
                {
                    if (Math3D.IsNearValue(points0[cntr0], points1[cntr1]))
                    {
                        return points0[cntr0];
                    }
                }
            }

            throw new ApplicationException("Didn't find a common point");
        }

        internal static Point3D GetUncommonPoint(ITriangle triangle, TriangleEdge edge0, TriangleEdge edge1)
        {
            Point3D[] points0 = new Point3D[] { triangle.GetPoint(edge0, true), triangle.GetPoint(edge0, false) };
            Point3D[] points1 = new Point3D[] { triangle.GetPoint(edge1, true), triangle.GetPoint(edge1, false) };

            // Find exact
            for (int cntr0 = 0; cntr0 < points0.Length; cntr0++)
            {
                for (int cntr1 = 0; cntr1 < points1.Length; cntr1++)
                {
                    if (points0[cntr0] == points1[cntr1])
                    {
                        return points0[cntr0 == 0 ? 1 : 0];     // return the one that isn't common
                    }
                }
            }

            // Find close - execution should never get here, just being safe
            for (int cntr0 = 0; cntr0 < points0.Length; cntr0++)
            {
                for (int cntr1 = 0; cntr1 < points1.Length; cntr1++)
                {
                    if (Math3D.IsNearValue(points0[cntr0], points1[cntr1]))
                    {
                        return points0[cntr0 == 0 ? 1 : 0];     // return the one that isn't common
                    }
                }
            }

            throw new ApplicationException("Didn't find a common point");
        }

        internal static Point3D GetOppositePoint(ITriangle triangle, TriangleEdge edge)
        {
            switch (edge)
            {
                case TriangleEdge.Edge_01:
                    return triangle.Point2;

                case TriangleEdge.Edge_12:
                    return triangle.Point0;

                case TriangleEdge.Edge_20:
                    return triangle.Point1;

                default:
                    throw new ApplicationException("Unknown TriangleEdge: " + edge.ToString());
            }
        }

        internal static Point3D GetEdgeMidpoint(ITriangle triangle, TriangleEdge edge)
        {
            Point3D point0 = triangle.GetPoint(edge, true);
            Point3D point1 = triangle.GetPoint(edge, false);

            Vector3D halfLength = (point1 - point0) * .5d;

            return point0 + halfLength;
        }

        internal static double GetEdgeLength(ITriangle triangle, TriangleEdge edge)
        {
            Point3D point0 = triangle.GetPoint(edge, true);
            Point3D point1 = triangle.GetPoint(edge, false);

            return (point1 - point0).Length;
        }

        #endregion
        #region Protected Methods

        protected virtual void OnPointChanged()
        {
            _normal = null;
            _normalUnit = null;
            _normalLength = null;
            _planeDistance = null;
        }

        #endregion
    }

    #endregion
    #region class: TriangleThreadsafe

    /// <summary>
    /// This is a copy of Triangle, but is readonly
    /// NOTE: Only use this class if it's needed.  Extra stuff needs to be cached during the constructor, even if it will never be used, so this class is a bit more expensive
    /// </summary>
    public class TriangleThreadsafe : ITriangle
    {
        #region Constructor

        public TriangleThreadsafe(Point3D point0, Point3D point1, Point3D point2, bool calculateNormalUpFront)
        {
            _point0 = point0;
            _point1 = point1;
            _point2 = point2;

            if (calculateNormalUpFront)
            {
                Vector3D normal, normalUnit;
                double length;
                Triangle.CalculateNormal(out normal, out length, out normalUnit, point0, point1, point2);

                _normal = normal;
                _normalLength = length;
                _normalUnit = normalUnit;

                _planeDistance = Math3D.GetPlaneOriginDistance(normalUnit, point0);
            }
            else
            {
                _normal = null;
                _normalLength = null;
                _normalUnit = null;
                _planeDistance = null;
            }

            _token = TokenGenerator.NextToken();
        }

        #endregion

        #region ITriangle Members

        private readonly Point3D _point0;
        public Point3D Point0
        {
            get
            {
                return _point0;
            }
        }

        private readonly Point3D _point1;
        public Point3D Point1
        {
            get
            {
                return _point1;
            }
        }

        private readonly Point3D _point2;
        public Point3D Point2
        {
            get
            {
                return _point2;
            }
        }

        public Point3D[] PointArray
        {
            get
            {
                return new[] { _point0, _point1, _point2 };
            }
        }

        public Point3D this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return this.Point0;

                    case 1:
                        return this.Point1;

                    case 2:
                        return this.Point2;

                    default:
                        throw new ArgumentOutOfRangeException("index", "index can only be 0, 1, 2: " + index.ToString());
                }
            }
        }

        private readonly Vector3D? _normal;
        /// <summary>
        /// This returns the triangle's normal.  Its length is the area of the triangle
        /// </summary>
        public Vector3D Normal
        {
            get
            {
                if (_normal == null)
                {
                    return Vector3D.CrossProduct(_point2 - _point1, _point0 - _point1);
                }
                else
                {
                    return _normal.Value;
                }
            }
        }
        private readonly Vector3D? _normalUnit;
        /// <summary>
        /// This returns the triangle's normal.  Its length is one
        /// </summary>
        public Vector3D NormalUnit
        {
            get
            {
                if (_normalUnit == null)
                {
                    Vector3D normal, normalUnit;
                    double length;
                    Triangle.CalculateNormal(out normal, out length, out normalUnit, _point0, _point1, _point2);

                    return normalUnit;
                }
                else
                {
                    return _normalUnit.Value;
                }
            }
        }
        private readonly double? _normalLength;
        /// <summary>
        /// This returns the length of the normal (the area of the triangle)
        /// NOTE:  Call this if you just want to know the length of the normal, it's cheaper than calling this.Normal.Length, since it's already been calculated
        /// </summary>
        public double NormalLength
        {
            get
            {
                if (_normalLength == null)
                {
                    Vector3D normal, normalUnit;
                    double length;
                    Triangle.CalculateNormal(out normal, out length, out normalUnit, _point0, _point1, _point2);

                    return length;
                }
                else
                {
                    return _normalLength.Value;
                }
            }
        }

        private readonly double? _planeDistance;
        public double PlaneDistance
        {
            get
            {
                if (_planeDistance == null)
                {
                    return Math3D.GetPlaneOriginDistance(this.NormalUnit, _point0);
                }
                else
                {
                    return _planeDistance.Value;
                }
            }
        }

        private readonly long _token;
        public long Token
        {
            get
            {
                return _token;
            }
        }

        public Point3D GetCenterPoint()
        {
            return Triangle.GetCenterPoint(_point0, _point1, _point2);
        }
        public Point3D GetPoint(TriangleEdge edge, bool isFrom)
        {
            return Triangle.GetPoint(this, edge, isFrom);
        }
        public Point3D GetCommonPoint(TriangleEdge edge0, TriangleEdge edge1)
        {
            return Triangle.GetCommonPoint(this, edge0, edge1);
        }
        public Point3D GetUncommonPoint(TriangleEdge edge0, TriangleEdge edge1)
        {
            return Triangle.GetUncommonPoint(this, edge0, edge1);
        }
        public Point3D GetOppositePoint(TriangleEdge edge)
        {
            return Triangle.GetOppositePoint(this, edge);
        }
        public Point3D GetEdgeMidpoint(TriangleEdge edge)
        {
            return Triangle.GetEdgeMidpoint(this, edge);
        }
        public double GetEdgeLength(TriangleEdge edge)
        {
            return Triangle.GetEdgeLength(this, edge);
        }

        #endregion
        #region IComparable<ITriangle> Members

        /// <summary>
        /// I wanted to be able to use triangles as keys in a sorted list
        /// </summary>
        public int CompareTo(ITriangle other)
        {
            if (other == null)
            {
                // I'm greater than null
                return 1;
            }

            if (this.Token < other.Token)
            {
                return -1;
            }
            else if (this.Token > other.Token)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// This helps a lot when looking at lists of triangles in the quick watch
        /// </summary>
        public override string ToString()
        {
            return string.Format("({0}) ({1}) ({2})", _point0.ToString(2), _point1.ToString(2), _point2.ToString(2));
        }

        #endregion
    }

    #endregion

    // These two are now threadsafe
    #region class: TriangleIndexed

    /// <summary>
    /// This takes an array of points, then holds three ints that point to three of those points
    /// </summary>
    public class TriangleIndexed : ITriangleIndexed
    {
        #region Declaration Section

        // I was going to use RWLS, but it is 1.7 times slower than standard lock, so it would take quite a few simultaneous reads to
        // justify using
        //http://blogs.msdn.com/b/pedram/archive/2007/10/07/a-performance-comparison-of-readerwriterlockslim-with-readerwriterlock.aspx
        //private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        private readonly object _lock = new object();

        #endregion

        #region Constructor

        public TriangleIndexed(int index0, int index1, int index2, Point3D[] allPoints)
        {
            _index0 = index0;
            _index1 = index1;
            _index2 = index2;
            _allPoints = allPoints;
            OnPointChanged();
        }

        #endregion

        #region ITriangle Members

        public Point3D Point0
        {
            get
            {
                return _allPoints[_index0];
            }
        }
        public Point3D Point1
        {
            get
            {
                return _allPoints[_index1];
            }
        }
        public Point3D Point2
        {
            get
            {
                return _allPoints[_index2];
            }
        }

        private Point3D[] _pointArray;
        public Point3D[] PointArray
        {
            get
            {
                lock (_lock)
                {
                    if (_pointArray == null)
                    {
                        _pointArray = new[] { this.Point0, this.Point1, this.Point2 };
                    }

                    return _pointArray;
                }
            }
        }

        public Point3D this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return _allPoints[_index0];

                    case 1:
                        return _allPoints[_index1];

                    case 2:
                        return _allPoints[_index2];

                    default:
                        throw new ArgumentOutOfRangeException("index", "index can only be 0, 1, 2: " + index.ToString());
                }
            }
        }

        private Vector3D? _normal = null;
        /// <summary>
        /// This returns the triangle's normal.  Its length is the area of the triangle
        /// </summary>
        public Vector3D Normal
        {
            get
            {
                lock (_lock)
                {
                    if (_normal == null)
                    {
                        Vector3D normal, normalUnit;
                        double length;
                        Triangle.CalculateNormal(out normal, out length, out normalUnit, this.Point0, this.Point1, this.Point2);

                        _normal = normal;
                        _normalLength = length;
                        _normalUnit = normalUnit;
                    }

                    return _normal.Value;
                }
            }
        }
        private Vector3D? _normalUnit = null;
        /// <summary>
        /// This returns the triangle's normal.  Its length is one
        /// </summary>
        public Vector3D NormalUnit
        {
            get
            {
                lock (_lock)
                {
                    if (_normalUnit == null)
                    {
                        Vector3D normal, normalUnit;
                        double length;
                        Triangle.CalculateNormal(out normal, out length, out normalUnit, this.Point0, this.Point1, this.Point2);

                        _normal = normal;
                        _normalLength = length;
                        _normalUnit = normalUnit;
                    }

                    return _normalUnit.Value;
                }
            }
        }
        private double? _normalLength = null;
        /// <summary>
        /// This returns the length of the normal (the area of the triangle)
        /// NOTE:  Call this if you just want to know the length of the normal, it's cheaper than calling this.Normal.Length, since it's already been calculated
        /// </summary>
        public double NormalLength
        {
            get
            {
                lock (_lock)
                {
                    if (_normalLength == null)
                    {
                        Vector3D normal, normalUnit;
                        double length;
                        Triangle.CalculateNormal(out normal, out length, out normalUnit, this.Point0, this.Point1, this.Point2);

                        _normal = normal;
                        _normalLength = length;
                        _normalUnit = normalUnit;
                    }

                    return _normalLength.Value;
                }
            }
        }

        private double? _planeDistance = null;
        public double PlaneDistance
        {
            get
            {
                lock (_lock)
                {
                    if (_planeDistance == null)
                    {
                        if (_normalUnit == null)
                        {
                            Vector3D normal, normalUnit;
                            double length;
                            Triangle.CalculateNormal(out normal, out length, out normalUnit, this.Point0, this.Point1, this.Point2);

                            _normal = normal;
                            _normalLength = length;
                            _normalUnit = normalUnit;
                        }

                        _planeDistance = Math3D.GetPlaneOriginDistance(_normalUnit.Value, this.Point0);
                    }

                    return _planeDistance.Value;
                }
            }
        }

        private long? _token = null;
        public long Token
        {
            get
            {
                lock (_lock)
                {
                    if (_token == null)
                    {
                        _token = TokenGenerator.NextToken();
                    }

                    return _token.Value;
                }
            }
        }

        public Point3D GetCenterPoint()
        {
            return Triangle.GetCenterPoint(this.Point0, this.Point1, this.Point2);
        }
        public Point3D GetPoint(TriangleEdge edge, bool isFrom)
        {
            return Triangle.GetPoint(this, edge, isFrom);
        }
        public Point3D GetCommonPoint(TriangleEdge edge0, TriangleEdge edge1)
        {
            return Triangle.GetCommonPoint(this, edge0, edge1);
        }
        public Point3D GetUncommonPoint(TriangleEdge edge0, TriangleEdge edge1)
        {
            return Triangle.GetUncommonPoint(this, edge0, edge1);
        }
        public Point3D GetOppositePoint(TriangleEdge edge)
        {
            return Triangle.GetOppositePoint(this, edge);
        }
        public Point3D GetEdgeMidpoint(TriangleEdge edge)
        {
            return Triangle.GetEdgeMidpoint(this, edge);
        }
        public double GetEdgeLength(TriangleEdge edge)
        {
            return Triangle.GetEdgeLength(this, edge);
        }

        #endregion
        #region ITriangleIndexed Members

        private readonly int _index0;
        public int Index0
        {
            get
            {
                return _index0;
            }
        }

        private readonly int _index1;
        public int Index1
        {
            get
            {
                return _index1;
            }
        }

        private readonly int _index2;
        public int Index2
        {
            get
            {
                return _index2;
            }
        }

        private readonly Point3D[] _allPoints;
        public Point3D[] AllPoints
        {
            get
            {
                return _allPoints;
            }
        }

        private int[] _indexArray = null;
        /// <summary>
        /// This returns an array (element 0 is this.Index0, etc)
        /// NOTE:  This is readonly - any changes to this array won't be reflected by this class
        /// </summary>
        public int[] IndexArray
        {
            get
            {
                lock (_lock)
                {
                    if (_indexArray == null)
                    {
                        _indexArray = new int[] { this.Index0, this.Index1, this.Index2 };
                    }

                    return _indexArray;
                }
            }
        }

        public int GetIndex(int whichIndex)
        {
            switch (whichIndex)
            {
                case 0:
                    return this.Index0;

                case 1:
                    return this.Index1;

                case 2:
                    return this.Index2;

                default:
                    throw new ArgumentOutOfRangeException("whichIndex", "whichIndex can only be 0, 1, 2: " + whichIndex.ToString());
            }
        }
        public int GetIndex(TriangleEdge edge, bool isFrom)
        {
            switch (edge)
            {
                case TriangleEdge.Edge_01:
                    if (isFrom)
                    {
                        return this.Index0;
                    }
                    else
                    {
                        return this.Index1;
                    }

                case TriangleEdge.Edge_12:
                    if (isFrom)
                    {
                        return this.Index1;
                    }
                    else
                    {
                        return this.Index2;
                    }

                case TriangleEdge.Edge_20:
                    if (isFrom)
                    {
                        return this.Index2;
                    }
                    else
                    {
                        return this.Index0;
                    }

                default:
                    throw new ApplicationException("Unknown TriangleEdge: " + edge.ToString());
            }
        }
        public int GetCommonIndex(TriangleEdge edge0, TriangleEdge edge1)
        {
            int[] indices0 = new int[] { this.GetIndex(edge0, true), this.GetIndex(edge0, false) };
            int[] indices1 = new int[] { this.GetIndex(edge1, true), this.GetIndex(edge1, false) };

            for (int cntr0 = 0; cntr0 < indices0.Length; cntr0++)
            {
                for (int cntr1 = 0; cntr1 < indices1.Length; cntr1++)
                {
                    if (indices0[cntr0] == indices1[cntr1])
                    {
                        return indices0[cntr0];
                    }
                }
            }

            throw new ApplicationException("Didn't find a common index");
        }
        public int GetUncommonIndex(TriangleEdge edge0, TriangleEdge edge1)
        {
            int[] indices0 = new int[] { this.GetIndex(edge0, true), this.GetIndex(edge0, false) };
            int[] indices1 = new int[] { this.GetIndex(edge1, true), this.GetIndex(edge1, false) };

            for (int cntr0 = 0; cntr0 < indices0.Length; cntr0++)
            {
                for (int cntr1 = 0; cntr1 < indices1.Length; cntr1++)
                {
                    if (indices0[cntr0] == indices1[cntr1])
                    {
                        return indices0[cntr0 == 0 ? 1 : 0];        // return the one that isn't common
                    }
                }
            }

            throw new ApplicationException("Didn't find a common index");
        }
        public int GetOppositeIndex(TriangleEdge edge)
        {
            switch (edge)
            {
                case TriangleEdge.Edge_01:
                    return this.Index2;

                case TriangleEdge.Edge_12:
                    return this.Index0;

                case TriangleEdge.Edge_20:
                    return this.Index1;

                default:
                    throw new ApplicationException("Unknown TriangleEdge: " + edge.ToString());
            }
        }

        #endregion
        #region IComparable<ITriangle> Members

        /// <summary>
        /// I wanted to be able to use triangles as keys in a sorted list
        /// </summary>
        public int CompareTo(ITriangle other)
        {
            if (other == null)
            {
                // I'm greater than null
                return 1;
            }

            if (this.Token < other.Token)
            {
                return -1;
            }
            else if (this.Token > other.Token)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        #endregion

        #region Public Methods

        public Triangle ToTriangle()
        {
            return new Triangle(this.Point0, this.Point1, this.Point2);
        }

        /// <summary>
        /// This helps a lot when looking at lists of triangles in the quick watch
        /// </summary>
        public override string ToString()
        {
            return string.Format("{0} - {1} - {2}       |       ({3}) ({4}) ({5})",
                _index0.ToString(),
                _index1.ToString(),
                _index2.ToString(),

                this.Point0.ToString(2),
                this.Point1.ToString(2),
                this.Point2.ToString(2));
        }

        /// <summary>
        /// This creates a clone of the triangles passed in, but the new list will only have points that are used
        /// </summary>
        public static ITriangleIndexed[] Clone_CondensePoints(ITriangleIndexed[] triangles)
        {
            // Analize the points
            Point3D[] allUsedPoints;
            SortedList<int, int> oldToNewIndex;
            GetCondensedPointMap(out allUsedPoints, out oldToNewIndex, triangles);

            // Make new triangles that only have the used points
            TriangleIndexed[] retVal = new TriangleIndexed[triangles.Length];

            for (int cntr = 0; cntr < triangles.Length; cntr++)
            {
                retVal[cntr] = new TriangleIndexed(oldToNewIndex[triangles[cntr].Index0], oldToNewIndex[triangles[cntr].Index1], oldToNewIndex[triangles[cntr].Index2], allUsedPoints);
            }

            return retVal;
        }

        /// <summary>
        /// This clones the set of triangles, but with the points run through a transform
        /// </summary>
        public static ITriangleIndexed[] Clone_Transformed(ITriangleIndexed[] triangles, Transform3D transform)
        {
            if (triangles == null)
            {
                return new TriangleIndexed[0];
            }

            Point3D[] transformedPoints = triangles[0].AllPoints.
                Select(o => transform.Transform(o)).
                ToArray();

            return triangles.
                Select(o => new TriangleIndexed(o.Index0, o.Index1, o.Index2, transformedPoints)).
                ToArray();
        }

        /// <summary>
        /// This looks at all the lines in the triangles passed in, and returns the unique indices
        /// </summary>
        public static Tuple<int, int>[] GetUniqueLines(IEnumerable<ITriangleIndexed> triangles)
        {
            List<Tuple<int, int>> retVal = new List<Tuple<int, int>>();

            foreach (ITriangleIndexed triangle in triangles)
            {
                retVal.Add(GetLine(triangle.Index0, triangle.Index1));
                retVal.Add(GetLine(triangle.Index1, triangle.Index2));
                retVal.Add(GetLine(triangle.Index2, triangle.Index0));
            }

            return retVal.Distinct().ToArray();		//distinct works, because the tuple always has the smaller index as item1
        }

        /// <summary>
        /// This returns only the points that are used across the triangles
        /// </summary>
        /// <param name="forcePointCompare">If the points should be directly compared (ignore indices), then pass true</param>
        public static Point3D[] GetUsedPoints(IEnumerable<ITriangleIndexed> triangles, bool forcePointCompare = false)
        {
            if (forcePointCompare)
            {
                return Triangle.GetUniquePoints(triangles);
            }

            ITriangleIndexed first = triangles.FirstOrDefault();
            if (first == null)
            {
                return new Point3D[0];
            }

            Point3D[] allPoints = first.AllPoints;

            // Since these are indexed triangles, dedupe on the indices.  This will be much faster than directly comparing points
            return triangles.
                SelectMany(o => o.IndexArray).
                Distinct().
                //OrderBy(o => o).
                Select(o => allPoints[o]).
                ToArray();
        }
        /// <summary>
        /// This overload takes in a bunch of sets of triangles.  Each set is independent of the others (unique AllPoints per set)
        /// </summary>
        public static Point3D[] GetUsedPoints(IEnumerable<IEnumerable<ITriangleIndexed>> triangleSets, bool forcePointCompare = false)
        {
            List<Point3D> retVal = new List<Point3D>();

            var pointCompare = new Func<Point3D, Point3D, bool>((p1, p2) => Math3D.IsNearValue(p1, p2));

            foreach (var set in triangleSets)
            {
                retVal.AddRangeUnique(TriangleIndexed.GetUsedPoints(set, forcePointCompare), pointCompare);
            }

            return retVal.ToArray();
        }

        public static TriangleIndexed[] ConvertToIndexed(ITriangle[] triangles)
        {
            // Find the unique points
            Point3D[] points;
            Tuple<int, int, int>[] map;
            GetPointsAndIndices(out points, out map, triangles);

            TriangleIndexed[] retVal = new TriangleIndexed[triangles.Length];

            // Turn the map into triangles
            for (int cntr = 0; cntr < triangles.Length; cntr++)
            {
                retVal[cntr] = new TriangleIndexed(map[cntr].Item1, map[cntr].Item2, map[cntr].Item3, points);
            }

            return retVal;
        }

        /// <summary>
        /// Call this if the contents of AllPoints changed.  It will wipe out any cached variables
        /// </summary>
        public void PointsChanged()
        {
            lock (_lock)
            {
                OnPointChanged();
            }
        }

        #endregion
        #region Internal Methods

        /// <summary>
        /// This takes in triangles, and builds finds the unique points so that an array of TriangleIndexed
        /// can be built
        /// </summary>
        internal static void GetPointsAndIndices(out Point3D[] points, out Tuple<int, int, int>[] map, ITriangle[] triangles)
        {
            List<Point3D> pointList = new List<Point3D>();
            map = new Tuple<int, int, int>[triangles.Length];

            for (int cntr = 0; cntr < triangles.Length; cntr++)
            {
                map[cntr] = Tuple.Create(
                    FindOrAddPoint(pointList, triangles[cntr].Point0),
                    FindOrAddPoint(pointList, triangles[cntr].Point1),
                    FindOrAddPoint(pointList, triangles[cntr].Point2));
            }

            points = pointList.ToArray();
        }
        internal static int FindOrAddPoint(List<Point3D> points, Point3D newPoint)
        {
            // Try exact
            for (int cntr = 0; cntr < points.Count; cntr++)
            {
                if (points[cntr] == newPoint)
                {
                    return cntr;
                }
            }

            // Try close
            for (int cntr = 0; cntr < points.Count; cntr++)
            {
                if (Math3D.IsNearValue(points[cntr], newPoint))
                {
                    return cntr;
                }
            }

            // It's unique, add it
            points.Add(newPoint);
            return points.Count - 1;
        }

        #endregion
        #region Protected Methods

        //NOTE: This doesn't lock itself, it expects the caller to have taken a lock
        protected virtual void OnPointChanged()
        {
            _normal = null;
            _normalUnit = null;
            _normalLength = null;
            //_indexArray = null;       // moving points doesn't affect this
            _planeDistance = null;
        }

        /// <summary>
        /// This figures out which points in the list of triangles are used, and returns out to map from AllPoints to allUsedPoints
        /// </summary>
        /// <param name="allUsedPoints">These are only the points that are used</param>
        /// <param name="oldToNewIndex">
        /// Key = Index to a point from the list of triangles passed in.
        /// Value = Corresponding index into allUsedPoints.
        /// </param>
        protected static void GetCondensedPointMap(out Point3D[] allUsedPoints, out SortedList<int, int> oldToNewIndex, ITriangleIndexed[] triangles)
        {
            if (triangles == null || triangles.Length == 0)
            {
                allUsedPoints = new Point3D[0];
                oldToNewIndex = new SortedList<int, int>();
                return;
            }

            // Get all the used indices
            int[] allUsedIndices = triangles.
                SelectMany(o => o.IndexArray).
                Distinct().
                OrderBy(o => o).
                ToArray();

            // Get the points
            Point3D[] allPoints = triangles[0].AllPoints;
            allUsedPoints = allUsedIndices.Select(o => allPoints[o]).ToArray();

            // Build the map
            oldToNewIndex = new SortedList<int, int>();
            for (int cntr = 0; cntr < allUsedIndices.Length; cntr++)
            {
                oldToNewIndex.Add(allUsedIndices[cntr], cntr);
            }
        }

        #endregion

        #region Private Methods

        private static Tuple<int, int> GetLine(int index1, int index2)
        {
            return Tuple.Create(Math.Min(index1, index2), Math.Max(index1, index2));
        }

        #endregion
    }

    #endregion
    #region class: TriangleIndexedLinked

    /// <summary>
    /// This one also knows about its neighbors
    /// </summary>
    public class TriangleIndexedLinked : TriangleIndexed, IComparable<TriangleIndexedLinked>
    {
        #region Declaration Section

        private readonly object _lock = new object();

        #endregion

        #region Constructor

        public TriangleIndexedLinked(int index0, int index1, int index2, Point3D[] allPoints)
            : base(index0, index1, index2, allPoints) { }

        #endregion

        #region IComparable<TriangleLinkedIndexed> Members

        // SortedList fails if it doesn't have an explicit CompareTo for this type
        public int CompareTo(TriangleIndexedLinked other)
        {
            // The compare is by token, so just call the base
            return base.CompareTo(other);
        }

        #endregion

        #region Public Properties

        // These neighbors share one vertex
        private Tuple<TriangleIndexedLinked, int, TriangleCorner>[] _neighbor_0 = null;
        /// <summary>
        /// These are the other triangles that share the same point with this.Index0
        /// </summary>
        public Tuple<TriangleIndexedLinked, int, TriangleCorner>[] Neighbor_0
        {
            get
            {
                lock (_lock)
                {
                    return _neighbor_0;
                }
            }
            set
            {
                lock (_lock)
                {
                    _neighbor_0 = value;
                }
            }
        }

        private Tuple<TriangleIndexedLinked, int, TriangleCorner>[] _neighbor_1 = null;
        /// <summary>
        /// These are the other triangles that share the same point with this.Index1
        /// </summary>
        public Tuple<TriangleIndexedLinked, int, TriangleCorner>[] Neighbor_1
        {
            get
            {
                lock (_lock)
                {
                    return _neighbor_1;
                }
            }
            set
            {
                lock (_lock)
                {
                    _neighbor_1 = value;
                }
            }
        }

        private Tuple<TriangleIndexedLinked, int, TriangleCorner>[] _neighbor_2 = null;
        /// <summary>
        /// These are the other triangles that share the same point with this.Index2
        /// </summary>
        public Tuple<TriangleIndexedLinked, int, TriangleCorner>[] Neighbor_2
        {
            get
            {
                lock (_lock)
                {
                    return _neighbor_2;
                }
            }
            set
            {
                lock (_lock)
                {
                    _neighbor_2 = value;
                }
            }
        }

        // These neighbors share two vertices
        //TODO: It's possible for more than two triangles to share an edge
        private TriangleIndexedLinked _neighbor_01 = null;
        public TriangleIndexedLinked Neighbor_01
        {
            get
            {
                lock (_lock)
                {
                    return _neighbor_01;
                }
            }
            set
            {
                lock (_lock)
                {
                    _neighbor_01 = value;
                }
            }
        }

        private TriangleIndexedLinked _neighbor_12 = null;
        public TriangleIndexedLinked Neighbor_12
        {
            get
            {
                lock (_lock)
                {
                    return _neighbor_12;
                }
            }
            set
            {
                lock (_lock)
                {
                    _neighbor_12 = value;
                }
            }
        }

        private TriangleIndexedLinked _neighbor_20 = null;
        public TriangleIndexedLinked Neighbor_20
        {
            get
            {
                lock (_lock)
                {
                    return _neighbor_20;
                }
            }
            set
            {
                lock (_lock)
                {
                    _neighbor_20 = value;
                }
            }
        }

        private int[] _indexArraySorted = null;
        /// <summary>
        /// This returns a sorted array (useful for comparing triangles)
        /// NOTE:  This is readonly - any changes to this array won't be reflected by this class
        /// </summary>
        public int[] IndexArraySorted
        {
            get
            {
                lock (_lock)
                {
                    if (_indexArraySorted == null)
                    {
                        _indexArraySorted = new int[] { this.Index0, this.Index1, this.Index2 }.
                            OrderBy(o => o).
                            ToArray();
                    }

                    return _indexArraySorted;
                }
            }
        }

        #endregion

        #region Public Methods

        // These let you get at the neighbors with an enum instead of calling the explicit properties
        public TriangleIndexedLinked GetNeighbor(TriangleEdge edge)
        {
            switch (edge)
            {
                case TriangleEdge.Edge_01:
                    return this.Neighbor_01;

                case TriangleEdge.Edge_12:
                    return this.Neighbor_12;

                case TriangleEdge.Edge_20:
                    return this.Neighbor_20;

                default:
                    throw new ApplicationException("Unknown TriangleEdge: " + edge.ToString());
            }
        }
        public void SetNeighbor(TriangleEdge edge, TriangleIndexedLinked neighbor)
        {
            switch (edge)
            {
                case TriangleEdge.Edge_01:
                    this.Neighbor_01 = neighbor;
                    break;

                case TriangleEdge.Edge_12:
                    this.Neighbor_12 = neighbor;
                    break;

                case TriangleEdge.Edge_20:
                    this.Neighbor_20 = neighbor;
                    break;

                default:
                    throw new ApplicationException("Unknown TriangleEdge: " + edge.ToString());
            }
        }
        public Tuple<TriangleIndexedLinked, int, TriangleCorner>[] GetNeighbors(TriangleCorner corner)
        {
            switch (corner)
            {
                case TriangleCorner.Corner_0:
                    return this.Neighbor_0;

                case TriangleCorner.Corner_1:
                    return this.Neighbor_1;

                case TriangleCorner.Corner_2:
                    return this.Neighbor_2;

                default:
                    throw new ApplicationException("Unknown TriangleCorner: " + corner.ToString());
            }
        }
        public Tuple<TriangleIndexedLinked, int, TriangleCorner>[] GetNeighbors(int cornerIndex)
        {
            if (cornerIndex == this.Index0)
            {
                return this.Neighbor_0;
            }
            else if (cornerIndex == this.Index1)
            {
                return this.Neighbor_1;
            }
            else if (cornerIndex == this.Index2)
            {
                return this.Neighbor_2;
            }
            else
            {
                throw new ApplicationException(string.Format("Didn't find index: {0} in {1},{2},{3}", cornerIndex.ToString(), this.Index0.ToString(), this.Index1.ToString(), this.Index2.ToString()));
            }
        }

        //NOTE: These return how the neighbor is related when walking from this to the neighbor
        public TriangleEdge WhichEdge(TriangleIndexedLinked neighbor)
        {
            long neighborToken = neighbor.Token;

            if (this.Neighbor_01 != null && this.Neighbor_01.Token == neighborToken)
            {
                return TriangleEdge.Edge_01;
            }
            else if (this.Neighbor_12 != null && this.Neighbor_12.Token == neighborToken)
            {
                return TriangleEdge.Edge_12;
            }
            else if (this.Neighbor_20 != null && this.Neighbor_20.Token == neighborToken)
            {
                return TriangleEdge.Edge_20;
            }
            else
            {
                throw new ApplicationException(string.Format("Not a neighbor\r\n{0}\r\n{1}", this.ToString(), neighbor.ToString()));
            }
        }
        public TriangleCorner WhichCorner(TriangleIndexedLinked neighbor)
        {
            long neighborToken = neighbor.Token;

            if (this.Neighbor_0 != null && this.Neighbor_0.Any(o => o.Item1.Token == neighborToken))
            {
                return TriangleCorner.Corner_0;
            }
            else if (this.Neighbor_1 != null && this.Neighbor_1.Any(o => o.Item1.Token == neighborToken))
            {
                return TriangleCorner.Corner_1;
            }
            else if (this.Neighbor_2 != null && this.Neighbor_2.Any(o => o.Item1.Token == neighborToken))
            {
                return TriangleCorner.Corner_2;
            }
            else
            {
                throw new ApplicationException(string.Format("Not a neighbor\r\n{0}\r\n{1}", this.ToString(), neighbor.ToString()));
            }
        }

        public static List<TriangleIndexedLinked> ConvertToLinked(List<ITriangleIndexed> triangles, bool linkEdges, bool linkCorners)
        {
            List<TriangleIndexedLinked> retVal = triangles.Select(o => new TriangleIndexedLinked(o.Index0, o.Index1, o.Index2, o.AllPoints)).ToList();

            if (linkEdges)
            {
                LinkTriangles_Edges(retVal, true);
            }

            if (linkCorners)
            {
                LinkTriangles_Corners(retVal, true);
            }

            return retVal;
        }
        public static TriangleIndexedLinked[] ConvertToLinked(ITriangleIndexed[] triangles, bool linkEdges, bool linkCorners)
        {
            List<TriangleIndexedLinked> retVal = triangles.Select(o => new TriangleIndexedLinked(o.Index0, o.Index1, o.Index2, o.AllPoints)).ToList();

            if (linkEdges)
            {
                LinkTriangles_Edges(retVal, true);
            }

            if (linkCorners)
            {
                LinkTriangles_Corners(retVal, true);
            }

            return retVal.ToArray();
        }

        /// <summary>
        /// This does a brute force scan of the triangles, and finds the adjacent triangles (doesn't look at shared corners, only edges)
        /// </summary>
        /// <param name="setNullIfNoLink">
        /// True:  If no link is found for an edge, that edge is set to null
        /// False:  If no link is found for an edge, that edge is left alone
        /// </param>
        public static void LinkTriangles_Edges(List<TriangleIndexedLinked> triangles, bool setNullIfNoLink)
        {
            for (int cntr = 0; cntr < triangles.Count; cntr++)
            {
                TriangleIndexedLinked neighbor = FindLinkEdge(triangles, cntr, triangles[cntr].Index0, triangles[cntr].Index1);
                if (neighbor != null || setNullIfNoLink)
                {
                    triangles[cntr].Neighbor_01 = neighbor;
                }

                neighbor = FindLinkEdge(triangles, cntr, triangles[cntr].Index1, triangles[cntr].Index2);
                if (neighbor != null || setNullIfNoLink)
                {
                    triangles[cntr].Neighbor_12 = neighbor;
                }

                neighbor = FindLinkEdge(triangles, cntr, triangles[cntr].Index2, triangles[cntr].Index0);
                if (neighbor != null || setNullIfNoLink)
                {
                    triangles[cntr].Neighbor_20 = neighbor;
                }
            }
        }
        /// <summary>
        /// This does a brute force scan of the triangles, and finds the adjacent triangles (doesn't look at shared edges, only corners)
        /// </summary>
        /// <param name="setNullIfNoLink">
        /// True:  If no link is found for an edge, that edge is set to null
        /// False:  If no link is found for an edge, that edge is left alone
        /// </param>
        public static void LinkTriangles_Corners(List<TriangleIndexedLinked> triangles, bool setNullIfNoLink)
        {
            if (setNullIfNoLink)
            {
                foreach (TriangleIndexedLinked triangle in triangles)
                {
                    triangle.Neighbor_0 = null;
                    triangle.Neighbor_1 = null;
                    triangle.Neighbor_2 = null;
                }
            }

            if (triangles.Count <= 1)
            {
                return;
            }

            //NOTE: This method only works if all triangles use the same points list
            foreach (int index in Enumerable.Range(0, triangles[0].AllPoints.Length))
            {
                List<Tuple<TriangleIndexedLinked, int, TriangleCorner>> used = new List<Tuple<TriangleIndexedLinked, int, TriangleCorner>>();

                // Find the triangles with this index
                foreach (TriangleIndexedLinked triangle in triangles)
                {
                    if (triangle.Index0 == index)
                    {
                        used.Add(Tuple.Create(triangle, 0, TriangleCorner.Corner_0));
                    }
                    else if (triangle.Index1 == index)
                    {
                        used.Add(Tuple.Create(triangle, 1, TriangleCorner.Corner_1));
                    }
                    else if (triangle.Index2 == index)
                    {
                        used.Add(Tuple.Create(triangle, 2, TriangleCorner.Corner_2));
                    }
                }

                if (used.Count <= 1)
                {
                    continue;
                }

                // Distribute them
                for (int cntr = 0; cntr < used.Count; cntr++)
                {
                    var neighbors = used.Where((o, i) => i != cntr).ToArray();

                    switch (used[cntr].Item3)
                    {
                        case TriangleCorner.Corner_0:
                            used[cntr].Item1.Neighbor_0 = neighbors;
                            break;

                        case TriangleCorner.Corner_1:
                            used[cntr].Item1.Neighbor_1 = neighbors;
                            break;

                        case TriangleCorner.Corner_2:
                            used[cntr].Item1.Neighbor_2 = neighbors;
                            break;

                        default:
                            throw new ApplicationException("Unknown TriangleCorner: " + used[cntr].Item3.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// This does a brute force scan of the triangles, and finds the adjacent triangles (both shared edges and corners)
        /// </summary>
        /// <param name="setNullIfNoLink">
        /// True:  If no link is found for an edge, that edge is set to null
        /// False:  If no link is found for an edge, that edge is left alone
        /// </param>
        public static void LinkTriangles_Both(List<TriangleIndexedLinked> triangles, bool setNullIfNoLink)
        {
            LinkTriangles_Corners(triangles, setNullIfNoLink);
            LinkTriangles_Edges(triangles, setNullIfNoLink);
        }

        /// <summary>
        /// Only call this method if you know the two triangles are neighbors 
        /// </summary>
        public static void LinkTriangles_Edges(TriangleIndexedLinked triangle1, TriangleIndexedLinked triangle2)
        {
            foreach (TriangleEdge edge1 in Triangle.Edges)
            {
                int[] indices1 = new int[2];
                triangle1.GetIndices(out indices1[0], out indices1[1], edge1);

                foreach (TriangleEdge edge2 in Triangle.Edges)
                {
                    int[] indices2 = new int[2];
                    triangle2.GetIndices(out indices2[0], out indices2[1], edge2);

                    if ((indices1[0] == indices2[0] && indices1[1] == indices2[1]) ||
                        (indices1[0] == indices2[1] && indices1[1] == indices2[0]))
                    {
                        triangle1.SetNeighbor(edge1, triangle2);
                        triangle2.SetNeighbor(edge2, triangle1);
                        return;
                    }
                }
            }

            throw new ArgumentException(string.Format("The two triangles passed in don't share an edge:\r\n{0}\r\n{1}", triangle1.ToString(), triangle2.ToString()));
        }

        public int GetIndex(TriangleCorner corner)
        {
            switch (corner)
            {
                case TriangleCorner.Corner_0:
                    return this.Index0;

                case TriangleCorner.Corner_1:
                    return this.Index1;

                case TriangleCorner.Corner_2:
                    return this.Index2;

                default:
                    throw new ApplicationException("Unknown TriangleCorner: " + corner.ToString());
            }
        }
        public void GetIndices(out int index1, out int index2, TriangleEdge edge)
        {
            switch (edge)
            {
                case TriangleEdge.Edge_01:
                    index1 = this.Index0;
                    index2 = this.Index1;
                    break;

                case TriangleEdge.Edge_12:
                    index1 = this.Index1;
                    index2 = this.Index2;
                    break;

                case TriangleEdge.Edge_20:
                    index1 = this.Index2;
                    index2 = this.Index0;
                    break;

                default:
                    throw new ApplicationException("Unknown TriangleEdge: " + edge.ToString());
            }
        }

        public Point3D GetPoint(TriangleCorner corner)
        {
            switch (corner)
            {
                case TriangleCorner.Corner_0:
                    return this.Point0;

                case TriangleCorner.Corner_1:
                    return this.Point1;

                case TriangleCorner.Corner_2:
                    return this.Point2;

                default:
                    throw new ApplicationException("Unknown TriangleCorner: " + corner.ToString());
            }
        }
        public void GetPoints(out Point3D point1, out Point3D point2, TriangleEdge edge)
        {
            switch (edge)
            {
                case TriangleEdge.Edge_01:
                    point1 = this.Point0;
                    point2 = this.Point1;
                    break;

                case TriangleEdge.Edge_12:
                    point1 = this.Point1;
                    point2 = this.Point2;
                    break;

                case TriangleEdge.Edge_20:
                    point1 = this.Point2;
                    point2 = this.Point0;
                    break;

                default:
                    throw new ApplicationException("Unknown TriangleEdge: " + edge.ToString());
            }
        }

        /// <summary>
        /// Pass in two corners, and this returns what edge they are
        /// </summary>
        public static TriangleEdge GetEdge(TriangleCorner corner0, TriangleCorner corner1)
        {
            switch (corner0)
            {
                case TriangleCorner.Corner_0:
                    switch (corner1)
                    {
                        case TriangleCorner.Corner_1:
                            return TriangleEdge.Edge_01;

                        case TriangleCorner.Corner_2:
                            return TriangleEdge.Edge_20;

                        default:
                            throw new ApplicationException("Unexpected TriangleCorner: " + corner0);
                    }

                case TriangleCorner.Corner_1:
                    switch (corner1)
                    {
                        case TriangleCorner.Corner_0:
                            return TriangleEdge.Edge_01;

                        case TriangleCorner.Corner_2:
                            return TriangleEdge.Edge_12;

                        default:
                            throw new ApplicationException("Unexpected TriangleCorner: " + corner0);
                    }

                case TriangleCorner.Corner_2:
                    switch (corner1)
                    {
                        case TriangleCorner.Corner_0:
                            return TriangleEdge.Edge_20;

                        case TriangleCorner.Corner_1:
                            return TriangleEdge.Edge_12;

                        default:
                            throw new ApplicationException("Unexpected TriangleCorner: " + corner0);
                    }

                default:
                    throw new ApplicationException("Unknown TriangleCorner: " + corner0);
            }
        }

        public bool IsMatch(int[] indeciesSorted)
        {
            int[] mySorted = this.IndexArraySorted;

            // Speed is important, so I'm skipping range checking, and looping

            return mySorted[0] == indeciesSorted[0] &&
                mySorted[1] == indeciesSorted[1] &&
                mySorted[2] == indeciesSorted[2];
        }

        #endregion

        #region Private Methods

        private static TriangleIndexedLinked FindLinkEdge(List<TriangleIndexedLinked> triangles, int index, int vertex1, int vertex2)
        {
            // Find the triangle that has vertex1 and 2
            for (int cntr = 0; cntr < triangles.Count; cntr++)
            {
                if (cntr == index)
                {
                    // This is the currently requested triangle, so ignore it
                    continue;
                }

                if ((triangles[cntr].Index0 == vertex1 && triangles[cntr].Index1 == vertex2) ||
                    (triangles[cntr].Index0 == vertex1 && triangles[cntr].Index2 == vertex2) ||
                    (triangles[cntr].Index1 == vertex1 && triangles[cntr].Index0 == vertex2) ||
                    (triangles[cntr].Index1 == vertex1 && triangles[cntr].Index2 == vertex2) ||
                    (triangles[cntr].Index2 == vertex1 && triangles[cntr].Index0 == vertex2) ||
                    (triangles[cntr].Index2 == vertex1 && triangles[cntr].Index1 == vertex2))
                {
                    // Found it
                    //NOTE:  Just returning the first match.  Assuming that the list of triangles is a valid hull
                    //NOTE:  Not validating that the triangle has 3 unique points, and the 2 points passed in are unique
                    return triangles[cntr];
                }
            }

            // No neighbor found
            return null;
        }
        private static TriangleIndexedLinked FindLinkCorner(List<TriangleIndexedLinked> triangles, int index, int cornerVertex, int otherVertex1, int otherVertex2)
        {
            // Find the triangle that has cornerVertex, but not otherVertex1 or 2
            for (int cntr = 0; cntr < triangles.Count; cntr++)
            {
                if (cntr == index)
                {
                    // This is the currently requested triangle, so ignore it
                    continue;
                }

                if ((triangles[cntr].Index0 == cornerVertex && triangles[cntr].Index1 != otherVertex1 && triangles[cntr].Index2 != otherVertex1 && triangles[cntr].Index1 != otherVertex2 && triangles[cntr].Index2 != otherVertex2) ||
                    (triangles[cntr].Index1 == cornerVertex && triangles[cntr].Index0 != otherVertex1 && triangles[cntr].Index2 != otherVertex1 && triangles[cntr].Index0 != otherVertex2 && triangles[cntr].Index2 != otherVertex2) ||
                    (triangles[cntr].Index2 == cornerVertex && triangles[cntr].Index0 != otherVertex1 && triangles[cntr].Index1 != otherVertex1 && triangles[cntr].Index0 != otherVertex2 && triangles[cntr].Index1 != otherVertex2))
                {
                    // Found it
                    //NOTE:  Just returning the first match.  Assuming that the list of triangles is a valid hull
                    //NOTE:  Not validating that the triangle has 3 unique points, and the 3 points passed in are unique
                    return triangles[cntr];
                }
            }

            // No neighbor found
            return null;
        }

        #endregion
    }

    #endregion

    #region interface: ITriangle

    //TODO:  May want more readonly statistics methods, like IsIntersecting, is Acute/Right/Obtuse
    public interface ITriangle : IComparable<ITriangle>
    {
        Point3D Point0
        {
            get;
        }
        Point3D Point1
        {
            get;
        }
        Point3D Point2
        {
            get;
        }

        Point3D[] PointArray
        {
            get;
        }

        Point3D this[int index]
        {
            get;
        }

        Vector3D Normal
        {
            get;
        }
        /// <summary>
        /// This returns the triangle's normal.  Its length is one
        /// </summary>
        Vector3D NormalUnit
        {
            get;
        }
        /// <summary>
        /// This returns the length of the normal (the area of the triangle)
        /// NOTE:  Call this if you just want to know the length of the normal, it's cheaper than calling this.Normal.Length, since it's already been calculated
        /// </summary>
        double NormalLength
        {
            get;
        }

        /// <summary>
        /// This is useful for functions that use this triangle as the definition of a plane
        /// (normal * planeDist = 0)
        /// </summary>
        double PlaneDistance
        {
            get;
        }

        Point3D GetCenterPoint();
        Point3D GetPoint(TriangleEdge edge, bool isFrom);
        Point3D GetCommonPoint(TriangleEdge edge0, TriangleEdge edge1);
        /// <summary>
        /// Returns the point in edge0 that isn't in edge1
        /// </summary>
        Point3D GetUncommonPoint(TriangleEdge edge0, TriangleEdge edge1);
        Point3D GetOppositePoint(TriangleEdge edge);
        Point3D GetEdgeMidpoint(TriangleEdge edge);
        double GetEdgeLength(TriangleEdge edge);

        long Token
        {
            get;
        }
    }

    #endregion
    #region interface: ITriangleIndexed

    public interface ITriangleIndexed : ITriangle
    {
        int Index0
        {
            get;
        }
        int Index1
        {
            get;
        }
        int Index2
        {
            get;
        }

        Point3D[] AllPoints
        {
            get;
        }

        int[] IndexArray
        {
            get;
        }

        int GetIndex(int whichIndex);
        int GetIndex(TriangleEdge edge, bool isFrom);
        int GetCommonIndex(TriangleEdge edge0, TriangleEdge edge1);
        int GetUncommonIndex(TriangleEdge edge0, TriangleEdge edge1);
        int GetOppositeIndex(TriangleEdge edge);
    }

    #endregion

    #region enum: TriangleEdge

    public enum TriangleEdge
    {
        Edge_01,
        Edge_12,
        Edge_20
    }

    #endregion
    #region enum: TriangleCorner

    public enum TriangleCorner
    {
        Corner_0,
        Corner_1,
        Corner_2
    }

    #endregion
}
