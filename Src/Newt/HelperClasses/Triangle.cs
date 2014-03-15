using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;

using Game.HelperClasses;

namespace Game.Newt.HelperClasses
{
    #region Class: Triangle

    //TODO:  Add methods like Clone, GetTransformed(transform), etc

    /// <summary>
    /// This stores 3 points explicitly - as opposed to TriangleIndexed, which stores ints that point to a list of points
    /// </summary>
    public class Triangle : ITriangle
    {
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
                    _token = TokenGenerator.Instance.NextToken();
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

    #region Class: TriangleIndexed

    /// <summary>
    /// This takes an array of points, then holds three ints that point to three of those points
    /// </summary>
    public class TriangleIndexed : ITriangleIndexed
    {
        #region Constructor

        public TriangleIndexed()
        {
        }

        public TriangleIndexed(int index0, int index1, int index2, Point3D[] allPoints)
        {
            //TODO:  If the property sets have more added to them in the future, do that in this constructor as well
            //this.Index0 = index0;
            //this.Index1 = index1;
            //this.Index2 = index2;
            //this.AllPoints = allPoints;
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
                // I could put an if statement for null, but I want this to be as fast as possible, and .net will throw an exception anyway
                return _allPoints[_index0.Value];
            }
        }
        public Point3D Point1
        {
            get
            {
                // I could put an if statement for null, but I want this to be as fast as possible, and .net will throw an exception anyway
                return _allPoints[_index1.Value];
            }
        }
        public Point3D Point2
        {
            get
            {
                // I could put an if statement for null, but I want this to be as fast as possible, and .net will throw an exception anyway
                return _allPoints[_index2.Value];
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
                    Triangle.CalculateNormal(out normal, out length, out normalUnit, this.Point0, this.Point1, this.Point2);

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
                    Triangle.CalculateNormal(out normal, out length, out normalUnit, this.Point0, this.Point1, this.Point2);

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
                    Triangle.CalculateNormal(out normal, out length, out normalUnit, this.Point0, this.Point1, this.Point2);

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
                    _token = TokenGenerator.Instance.NextToken();
                }

                return _token.Value;
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

        private int? _index0 = null;
        public int Index0
        {
            get
            {
                // I could put an if statement for null, but I want this to be as fast as possible, and .net will throw an exception anyway
                return _index0.Value;
            }
            set
            {
                _index0 = value;
                OnPointChanged();
            }
        }

        private int? _index1 = null;
        public int Index1
        {
            get
            {
                // I could put an if statement for null, but I want this to be as fast as possible, and .net will throw an exception anyway
                return _index1.Value;
            }
            set
            {
                _index1 = value;
                OnPointChanged();
            }
        }

        private int? _index2 = null;
        public int Index2
        {
            get
            {
                // I could put an if statement for null, but I want this to be as fast as possible, and .net will throw an exception anyway
                return _index2.Value;
            }
            set
            {
                _index2 = value;
                OnPointChanged();
            }
        }

        private Point3D[] _allPoints = null;
        public Point3D[] AllPoints
        {
            get
            {
                return _allPoints;
            }
            set
            {
                _allPoints = value;
                OnPointChanged();
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
                if (_indexArray == null)
                {
                    _indexArray = new int[] { this.Index0, this.Index1, this.Index2 };
                }

                return _indexArray;
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

        public void SetIndex(int whichIndex, int newValue)
        {
            switch (whichIndex)
            {
                case 0:
                    this.Index0 = newValue;
                    break;

                case 1:
                    this.Index1 = newValue;
                    break;

                case 2:
                    this.Index2 = newValue;
                    break;

                default:
                    throw new ArgumentOutOfRangeException("whichIndex", "whichIndex can only be 0, 1, 2: " + whichIndex.ToString());
            }
        }

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
                _index0 == null ? "null" : _index0.Value.ToString(),
                _index1 == null ? "null" : _index1.Value.ToString(),
                _index2 == null ? "null" : _index2.Value.ToString(),

                _index0 == null ? "null" : this.Point0.ToString(2),
                _index1 == null ? "null" : this.Point1.ToString(2),
                _index2 == null ? "null" : this.Point2.ToString(2));
        }

        /// <summary>
        /// This creates a clone of the triangles passed in, but the new list will only have points that are used
        /// </summary>
        public static TriangleIndexed[] Clone_CondensePoints(TriangleIndexed[] triangles)
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

            // Exit Function
            return retVal;
        }

        /// <summary>
        /// This looks at all the lines in the triangles passed in, and returns the unique indices
        /// </summary>
        public static Tuple<int, int>[] GetUniqueLines(ITriangleIndexed[] triangles)
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

        protected virtual void OnPointChanged()
        {
            _normal = null;
            _normalUnit = null;
            _normalLength = null;
            _indexArray = null;
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
        protected static void GetCondensedPointMap(out Point3D[] allUsedPoints, out SortedList<int, int> oldToNewIndex, TriangleIndexed[] triangles)
        {
            if (triangles == null || triangles.Length == 0)
            {
                allUsedPoints = new Point3D[0];
                oldToNewIndex = new SortedList<int, int>();
                return;
            }

            // Get all the used indices
            int[] allUsedIndices = triangles.SelectMany(o => o.IndexArray).Distinct().OrderBy(o => o).ToArray();

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
    #region Class: TriangleIndexedLinked

    /// <summary>
    /// This one also knows about its neighbors
    /// </summary>
    public class TriangleIndexedLinked : TriangleIndexed, IComparable<TriangleIndexedLinked>
    {
        #region Constructor

        public TriangleIndexedLinked()
            : base() { }

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
        /// <summary>
        /// this.Index0 is the same value as one of this.Neighbor_0's 3 Indices
        /// </summary>
        public TriangleIndexedLinked Neighbor_0
        {
            get;
            set;
        }
        /// <summary>
        /// this.Index1 is the same value as one of this.Neighbor_1's 3 Indices
        /// </summary>
        public TriangleIndexedLinked Neighbor_1
        {
            get;
            set;
        }
        /// <summary>
        /// this.Index2 is the same value as one of this.Neighbor_2's 3 Indices
        /// </summary>
        public TriangleIndexedLinked Neighbor_2
        {
            get;
            set;
        }

        // These neighbors share two vertices
        public TriangleIndexedLinked Neighbor_01
        {
            get;
            set;
        }
        public TriangleIndexedLinked Neighbor_12
        {
            get;
            set;
        }
        public TriangleIndexedLinked Neighbor_20
        {
            get;
            set;
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
        public TriangleIndexedLinked GetNeighbor(TriangleCorner corner)
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
        public void SetNeighbor(TriangleCorner corner, TriangleIndexedLinked neighbor)
        {
            switch (corner)
            {
                case TriangleCorner.Corner_0:
                    this.Neighbor_0 = neighbor;
                    break;

                case TriangleCorner.Corner_1:
                    this.Neighbor_1 = neighbor;
                    break;

                case TriangleCorner.Corner_2:
                    this.Neighbor_2 = neighbor;
                    break;

                default:
                    throw new ApplicationException("Unknown TriangleCorner: " + corner.ToString());
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

            if (this.Neighbor_0 != null && this.Neighbor_0.Token == neighborToken)
            {
                return TriangleCorner.Corner_0;
            }
            else if (this.Neighbor_1 != null && this.Neighbor_1.Token == neighborToken)
            {
                return TriangleCorner.Corner_1;
            }
            else if (this.Neighbor_2 != null && this.Neighbor_2.Token == neighborToken)
            {
                return TriangleCorner.Corner_2;
            }
            else
            {
                throw new ApplicationException(string.Format("Not a neighbor\r\n{0}\r\n{1}", this.ToString(), neighbor.ToString()));
            }
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
            for (int cntr = 0; cntr < triangles.Count; cntr++)
            {
                TriangleIndexedLinked neighbor = FindLinkCorner(triangles, cntr, triangles[cntr].Index0, triangles[cntr].Index1, triangles[cntr].Index2);
                if (neighbor != null || setNullIfNoLink)
                {
                    triangles[cntr].Neighbor_0 = neighbor;
                }

                neighbor = FindLinkCorner(triangles, cntr, triangles[cntr].Index1, triangles[cntr].Index0, triangles[cntr].Index1);
                if (neighbor != null || setNullIfNoLink)
                {
                    triangles[cntr].Neighbor_1 = neighbor;
                }

                neighbor = FindLinkCorner(triangles, cntr, triangles[cntr].Index2, triangles[cntr].Index0, triangles[cntr].Index1);
                if (neighbor != null || setNullIfNoLink)
                {
                    triangles[cntr].Neighbor_2 = neighbor;
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

        // Only call these methods if you know the two triangles are neighbors
        public static void LinkTriangles_Edges(TriangleIndexedLinked triangle1, TriangleIndexedLinked triangle2)
        {
            foreach (TriangleEdge edge1 in Enum.GetValues(typeof(TriangleEdge)))
            {
                int[] indices1 = new int[2];
                triangle1.GetIndices(out indices1[0], out indices1[1], edge1);

                foreach (TriangleEdge edge2 in Enum.GetValues(typeof(TriangleEdge)))
                {
                    int[] indices2 = new int[2];
                    triangle2.GetIndices(out indices2[0], out indices2[1], edge2);

                    if ((indices1[0] == indices2[0] && indices1[1] == indices2[1]) ||
                        (indices1[0] == indices2[1] && indices1[1] == indices2[0])
                        )
                    {
                        triangle1.SetNeighbor(edge1, triangle2);
                        triangle2.SetNeighbor(edge2, triangle1);
                        return;
                    }
                }
            }

            throw new ArgumentException(string.Format("The two triangles passed in don't share an edge:\r\n{0}\r\n{1}", triangle1.ToString(), triangle2.ToString()));
        }
        public static void LinkTriangles_Corners(TriangleIndexedLinked triangle1, TriangleIndexedLinked triangle2)
        {
            foreach (TriangleCorner corner1 in Enum.GetValues(typeof(TriangleCorner)))
            {
                foreach (TriangleCorner corner2 in Enum.GetValues(typeof(TriangleCorner)))
                {
                    int index1 = triangle1.GetIndex(corner1);
                    int index2 = triangle1.GetIndex(corner2);

                    if (index1 == index2)
                    {
                        triangle1.SetNeighbor(corner1, triangle2);
                        triangle2.SetNeighbor(corner2, triangle1);
                    }
                }
            }

            throw new ArgumentException(string.Format("The two triangles passed in don't share a corner:\r\n{0}\r\n{1}", triangle1.ToString(), triangle2.ToString()));
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


    //TODO: Get rid of these.  Make the other triangles readonly, and put locks around normal and other property gets
    #region Class: TriangleThreadsafe

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

            _token = TokenGenerator.Instance.NextToken();
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
    #region Class: TriangleIndexedThreadsafe

    /// <summary>
    /// This is a copy of TriangleIndexed, but is readonly
    /// NOTE: Only use this class if it's needed.  Extra stuff needs to be cached during the constructor, even if it will never be used, so this class is a bit more expensive
    /// </summary>
    public class TriangleIndexedThreadsafe : ITriangleIndexed
    {
        #region Constructor

        public TriangleIndexedThreadsafe(int index0, int index1, int index2, Point3D[] allPoints, bool calculateNormalUpFront)
        {
            _index0 = index0;
            _index1 = index1;
            _index2 = index2;
            _allPoints = allPoints;
            _indexArray = new int[] { index0, index1, index2 };

            if (calculateNormalUpFront)
            {
                Vector3D normal, normalUnit;
                double length;
                Triangle.CalculateNormal(out normal, out length, out normalUnit, allPoints[index0], allPoints[index1], allPoints[index2]);

                _normal = normal;
                _normalLength = length;
                _normalUnit = normalUnit;

                _planeDistance = Math3D.GetPlaneOriginDistance(normalUnit, allPoints[index0]);
            }
            else
            {
                _normal = null;
                _normalLength = null;
                _normalUnit = null;
                _planeDistance = null;
            }

            _token = TokenGenerator.Instance.NextToken();
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
                    return Vector3D.CrossProduct(this.Point2 - this.Point1, this.Point0 - this.Point1);
                }
                else
                {
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
                if (_normalUnit == null)
                {
                    Vector3D normal, normalUnit;
                    double length;
                    Triangle.CalculateNormal(out normal, out length, out normalUnit, this.Point0, this.Point1, this.Point2);

                    return normalUnit;
                }
                else
                {
                    return _normalUnit.Value;
                }
            }
        }
        private double? _normalLength = null;
        /// <summary>
        /// This returns the length of the normal (the area of the triangle)
        /// </summary>
        public double NormalLength
        {
            get
            {
                if (_normalLength == null)
                {
                    Vector3D normal, normalUnit;
                    double length;
                    Triangle.CalculateNormal(out normal, out length, out normalUnit, this.Point0, this.Point1, this.Point2);

                    return length;
                }
                else
                {
                    return _normalLength.Value;
                }
            }
        }

        private double? _planeDistance = null;
        public double PlaneDistance
        {
            get
            {
                if (_planeDistance == null)
                {
                    return Math3D.GetPlaneOriginDistance(this.NormalUnit, this.Point0);
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

        //NOTE: There is no readonly version of array, just don't change any values
        private readonly Point3D[] _allPoints;
        public Point3D[] AllPoints
        {
            get
            {
                return _allPoints;
            }
        }

        private readonly int[] _indexArray;
        /// <summary>
        /// This returns an array (element 0 is this.Index0, etc)
        /// NOTE:  This is readonly - any changes to this array won't be reflected by this class
        /// </summary>
        public int[] IndexArray
        {
            get
            {
                return _indexArray;
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

        public TriangleThreadsafe ToTriangle()
        {
            return new TriangleThreadsafe(this.Point0, this.Point1, this.Point2, _normal != null);
        }

        /// <summary>
        /// This helps a lot when looking at lists of triangles in the quick watch
        /// </summary>
        public override string ToString()
        {
            return string.Format("{0} - {1} - {2}       |       ({3}) ({4}) ({5})",
                _index0.ToString(), _index1.ToString(), _index2.ToString(),
                this.Point0.ToString(2), this.Point1.ToString(2), this.Point2.ToString(2));
        }

        /// <summary>
        /// This creates a clone of the triangles passed in, but the new list will only have points that are used
        /// </summary>
        public static TriangleIndexedThreadsafe[] Clone_CondensePoints(TriangleIndexedThreadsafe[] triangles, bool calculateNormalUpFront)
        {
            // Analize the points
            Point3D[] allUsedPoints;
            SortedList<int, int> oldToNewIndex;
            GetCondensedPointMap(out allUsedPoints, out oldToNewIndex, triangles);

            // Make new triangles that only have the used points
            TriangleIndexedThreadsafe[] retVal = new TriangleIndexedThreadsafe[triangles.Length];

            for (int cntr = 0; cntr < triangles.Length; cntr++)
            {
                retVal[cntr] = new TriangleIndexedThreadsafe(oldToNewIndex[triangles[cntr].Index0], oldToNewIndex[triangles[cntr].Index1], oldToNewIndex[triangles[cntr].Index2], allUsedPoints, calculateNormalUpFront);
            }

            // Exit Function
            return retVal;
        }

        public static TriangleIndexedThreadsafe[] ConvertToIndexed(ITriangle[] triangles, bool calculateNormalUpFront)
        {
            // Find the unique points
            Point3D[] points;
            Tuple<int, int, int>[] map;
            TriangleIndexed.GetPointsAndIndices(out points, out map, triangles);

            TriangleIndexedThreadsafe[] retVal = new TriangleIndexedThreadsafe[triangles.Length];

            // Turn the map into triangles
            for (int cntr = 0; cntr < triangles.Length; cntr++)
            {
                retVal[cntr] = new TriangleIndexedThreadsafe(map[cntr].Item1, map[cntr].Item2, map[cntr].Item3, points, calculateNormalUpFront);
            }

            return retVal;
        }

        #endregion
        #region Protected Methods

        /// <summary>
        /// This figures out which points in the list of triangles are used, and returns out to map from AllPoints to allUsedPoints
        /// </summary>
        /// <param name="allUsedPoints">These are only the points that are used</param>
        /// <param name="oldToNewIndex">
        /// Key = Index to a point from the list of triangles passed in.
        /// Value = Corresponding index into allUsedPoints.
        /// </param>
        protected static void GetCondensedPointMap(out Point3D[] allUsedPoints, out SortedList<int, int> oldToNewIndex, TriangleIndexedThreadsafe[] triangles)
        {
            if (triangles == null || triangles.Length == 0)
            {
                allUsedPoints = new Point3D[0];
                oldToNewIndex = new SortedList<int, int>();
                return;
            }

            // Get all the used indices
            int[] allUsedIndices = triangles.SelectMany(o => o.IndexArray).Distinct().OrderBy(o => o).ToArray();

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
    }

    #endregion


    #region Interface: ITriangle

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
        Point3D GetEdgeMidpoint(TriangleEdge edge);
        double GetEdgeLength(TriangleEdge edge);

        long Token
        {
            get;
        }
    }

    #endregion
    #region Interface: ITriangleIndexed

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
    }

    #endregion

    #region Enum: TriangleEdge

    public enum TriangleEdge
    {
        Edge_01,
        Edge_12,
        Edge_20
    }

    #endregion
    #region Enum: TriangleCorner

    public enum TriangleCorner
    {
        Corner_0,
        Corner_1,
        Corner_2
    }

    #endregion
}
