using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;

namespace Game.HelperClassesWPF
{
    /// <summary>
    /// This has similar interface as TriangleIndexedLinked, but defines a tetrahedron instead
    /// </summary>
    /// <remarks>
    /// If you want to visualize the tetrahedron that I used when coming up with edges and faces:
    ///     1,2,3 are the base in counter clockwise order
    ///     0 is the tip of the pyramid
    /// </remarks>
    public class Tetrahedron : IComparable<Tetrahedron>
    {
        #region Declaration Section

        private readonly object _lock = new object();

        // Caching these so that Enum.GetValues doesn't have to be called all over the place
        public static TetrahedronEdge[] Edges = (TetrahedronEdge[])Enum.GetValues(typeof(TetrahedronEdge));
        public static TetrahedronFace[] Faces = (TetrahedronFace[])Enum.GetValues(typeof(TetrahedronFace));

        #endregion

        #region Constructor

        #region CONSTRUCTOR ATTEMPTS

        //TODO: May want to implement this (though it would be very tedious/error prone for the caller to get all the triangles correct)
        ///// <summary>
        ///// This overload takes in prebuilt triangles (presumably already linked)
        ///// </summary>
        //public Tetrahedron(TriangleIndexedLinked face012, TriangleIndexedLinked face023, TriangleIndexedLinked face031, TriangleIndexedLinked face132)
        //{
        //    this.Index0 = face012.Index0;
        //    this.Index1 = face012.Index1;
        //    this.Index2 = face012.Index2;
        //    this.Index3 = face023.Index2;

        //    this.AllPoints = face012.AllPoints;

        //    this.Face_012 = face012;
        //    this.Face_023 = face023;
        //    this.Face_031 = face031;
        //    this.Face_132 = face132;
        //}

        ///// <summary>
        ///// WARNING: If this overload is used, then it's impossible for the triangle faces to be shared across tetrahedrons (the tetrahedrons
        ///// can still be linked at the tetra level, but the triangle faces won't be connected
        ///// </summary>
        //public Tetrahedron(int index0, int index1, int index2, int index3, Point3D[] allPoints)
        //{
        //    this.Index0 = index0;
        //    this.Index1 = index1;
        //    this.Index2 = index2;
        //    this.Index3 = index3;

        //    this.AllPoints = allPoints;

        //    this.Face_012 = new TriangleIndexedLinked(index0, index1, index2, allPoints);
        //    this.Face_023 = new TriangleIndexedLinked(index0, index2, index3, allPoints);
        //    this.Face_031 = new TriangleIndexedLinked(index0, index3, index1, allPoints);
        //    this.Face_132 = new TriangleIndexedLinked(index1, index3, index2, allPoints);
        //}

        #endregion

        /// <param name="buildingTriangles">
        /// When creating a set of tetrahedrons, instantiate an empty sorted list, and let this constructor fill it up to guarantee that only one
        /// instance of each triangle face is used across tetrahedrons (none of the neighbors get set, but the user could if they needed to)
        /// </param>
        public Tetrahedron(int index0, int index1, int index2, int index3, Point3D[] allPoints, SortedList<Tuple<int, int, int>, TriangleIndexedLinked> buildingTriangles)
        {
            this.Index0 = index0;
            this.Index1 = index1;
            this.Index2 = index2;
            this.Index3 = index3;

            this.AllPoints = allPoints;

            this.Face_012 = FindOrCreateTriangle(index0, index1, index2, allPoints, buildingTriangles);
            this.Face_023 = FindOrCreateTriangle(index0, index2, index3, allPoints, buildingTriangles);
            this.Face_031 = FindOrCreateTriangle(index0, index3, index1, allPoints, buildingTriangles);
            this.Face_132 = FindOrCreateTriangle(index1, index3, index2, allPoints, buildingTriangles);
        }

        #endregion

        #region IComparable<Tetrahedron> Members

        /// <summary>
        /// I wanted to be able to use tetrahedrons as keys in a sorted list
        /// </summary>
        public int CompareTo(Tetrahedron other)
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

        #region Public Properties

        public readonly int Index0;
        public readonly int Index1;
        public readonly int Index2;
        public readonly int Index3;

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
                        _indexArray = new int[] { this.Index0, this.Index1, this.Index2, this.Index3 };
                    }

                    return _indexArray;
                }
            }
        }

        private int[] _indexArraySorted = null;
        /// <summary>
        /// This returns a sorted array (useful for comparing tetrahedrons - see this.IsMatch())
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
                        _indexArraySorted = new int[] { this.Index0, this.Index1, this.Index2, this.Index3 }.
                            OrderBy(o => o).
                            ToArray();
                    }

                    return _indexArraySorted;
                }
            }
        }

        public Point3D Point0 { get { return this.AllPoints[this.Index0]; } }
        public Point3D Point1 { get { return this.AllPoints[this.Index1]; } }
        public Point3D Point2 { get { return this.AllPoints[this.Index2]; } }
        public Point3D Point3 { get { return this.AllPoints[this.Index3]; } }

        public Point3D this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return this.AllPoints[this.Index0];

                    case 1:
                        return this.AllPoints[this.Index1];

                    case 2:
                        return this.AllPoints[this.Index2];

                    case 3:
                        return this.AllPoints[this.Index3];

                    default:
                        throw new ArgumentOutOfRangeException("index", "index can only be 0, 1, 2, 3: " + index.ToString());
                }
            }
        }

        private Point3D[] _pointArray = null;
        public Point3D[] PointArray
        {
            get
            {
                lock (_lock)
                {
                    if (_pointArray == null)
                    {
                        _pointArray = new[] { this.AllPoints[this.Index0], this.AllPoints[this.Index1], this.AllPoints[this.Index2], this.AllPoints[this.Index3] };
                    }

                    return _pointArray;
                }
            }
        }

        public readonly Point3D[] AllPoints;

        //TODO: When two tetrahedrons get linked, share the face between them
        public readonly TriangleIndexedLinked Face_012;
        public readonly TriangleIndexedLinked Face_023;
        public readonly TriangleIndexedLinked Face_031;
        public readonly TriangleIndexedLinked Face_132;     // think of this as the base of the pyramid (if that helps)

        private TriangleIndexedLinked[] _faceArray = null;
        /// <summary>
        /// NOTE:  This is readonly - any changes to this array won't be reflected by this class
        /// </summary>
        public TriangleIndexedLinked[] FaceArray
        {
            get
            {
                lock (_lock)
                {
                    if (_faceArray == null)
                    {
                        _faceArray = new[] { this.Face_012, this.Face_023, this.Face_031, this.Face_132 };
                    }

                    return _faceArray;
                }
            }
        }

        private Tuple<int, int>[] _edgeArray = null;
        /// <summary>
        /// NOTE:  This is readonly - any changes to this array won't be reflected by this class
        /// </summary>
        public Tuple<int, int>[] EdgeArray
        {
            get
            {
                lock (_lock)
                {
                    if (_edgeArray == null)
                    {
                        _edgeArray = Edges.Select(o => Tuple.Create(GetIndex(o, true), GetIndex(o, false))).ToArray();
                    }

                    return _edgeArray;
                }
            }
        }

        private Tuple<Point3D, double> _circumsphere = null;
        /// <summary>
        /// This is the center of the circumsphere (the sphere that touches each of the 4 points)
        /// This is NOT the centroid (the averaged center of the 4 points)
        /// </summary>
        /// <remarks>
        /// http://people.sc.fsu.edu/~jburkardt/cpp_src/tetrahedron_properties/tetrahedron_properties.html
        /// http://www.geogebratube.org/student/m110172
        /// </remarks>
        public Point3D CircumsphereCenter
        {
            get
            {
                lock (_lock)
                {
                    if (_circumsphere == null)
                    {
                        _circumsphere = Math3D.GetCircumsphere(this.AllPoints[this.Index0], this.AllPoints[this.Index1], this.AllPoints[this.Index2], this.AllPoints[this.Index3]);
                    }

                    return _circumsphere.Item1;
                }
            }
        }
        public double CircumsphereRadius
        {
            get
            {
                lock (_lock)
                {
                    if (_circumsphere == null)
                    {
                        _circumsphere = Math3D.GetCircumsphere(this.AllPoints[this.Index0], this.AllPoints[this.Index1], this.AllPoints[this.Index2], this.AllPoints[this.Index3]);
                    }

                    return _circumsphere.Item2;
                }
            }
        }

        #region IMPLEMENT WHEN NEEDED

        //// These neighbors share one vertex
        //private Tetrahedron _neighbor_0 = null;
        ///// <summary>
        ///// this.Index0 is the same value as one of this.Neighbor_0's 4 Indices
        ///// </summary>
        //public Tetrahedron Neighbor_0
        //{
        //    get
        //    {
        //        lock (_lock)
        //        {
        //            return _neighbor_0;
        //        }
        //    }
        //    set
        //    {
        //        lock (_lock)
        //        {
        //            _neighbor_0 = value;
        //        }
        //    }
        //}

        //private Tetrahedron _neighbor_1 = null;
        ///// <summary>
        ///// this.Index1 is the same value as one of this.Neighbor_1's 4 Indices
        ///// </summary>
        //public Tetrahedron Neighbor_1
        //{
        //    get
        //    {
        //        lock (_lock)
        //        {
        //            return _neighbor_1;
        //        }
        //    }
        //    set
        //    {
        //        lock (_lock)
        //        {
        //            _neighbor_1 = value;
        //        }
        //    }
        //}

        //private Tetrahedron _neighbor_2 = null;
        ///// <summary>
        ///// this.Index2 is the same value as one of this.Neighbor_2's 4 Indices
        ///// </summary>
        //public Tetrahedron Neighbor_2
        //{
        //    get
        //    {
        //        lock (_lock)
        //        {
        //            return _neighbor_2;
        //        }
        //    }
        //    set
        //    {
        //        lock (_lock)
        //        {
        //            _neighbor_2 = value;
        //        }
        //    }
        //}

        //private Tetrahedron _neighbor_3 = null;
        ///// <summary>
        ///// this.Index2 is the same value as one of this.Neighbor_2's 4 Indices
        ///// </summary>
        //public Tetrahedron Neighbor_3
        //{
        //    get
        //    {
        //        lock (_lock)
        //        {
        //            return _neighbor_1;
        //        }
        //    }
        //    set
        //    {
        //        lock (_lock)
        //        {
        //            _neighbor_2 = value;
        //        }
        //    }
        //}

        //// These neighbors share two vertices
        //private Tetrahedron _neighbor_01 = null;
        //public Tetrahedron Neighbor_01
        //{
        //    get
        //    {
        //        lock (_lock)
        //        {
        //            return _neighbor_01;
        //        }
        //    }
        //    set
        //    {
        //        lock (_lock)
        //        {
        //            _neighbor_01 = value;
        //        }
        //    }
        //}

        //private Tetrahedron _neighbor_02 = null;
        //public Tetrahedron Neighbor_02
        //{
        //    get
        //    {
        //        lock (_lock)
        //        {
        //            return _neighbor_02;
        //        }
        //    }
        //    set
        //    {
        //        lock (_lock)
        //        {
        //            _neighbor_02 = value;
        //        }
        //    }
        //}

        //private Tetrahedron _neighbor_03 = null;
        //public Tetrahedron Neighbor_03
        //{
        //    get
        //    {
        //        lock (_lock)
        //        {
        //            return _neighbor_03;
        //        }
        //    }
        //    set
        //    {
        //        lock (_lock)
        //        {
        //            _neighbor_03 = value;
        //        }
        //    }
        //}

        //private Tetrahedron _neighbor_12 = null;
        //public Tetrahedron Neighbor_12
        //{
        //    get
        //    {
        //        lock (_lock)
        //        {
        //            return _neighbor_12;
        //        }
        //    }
        //    set
        //    {
        //        lock (_lock)
        //        {
        //            _neighbor_12 = value;
        //        }
        //    }
        //}

        //private Tetrahedron _neighbor_13 = null;
        //public Tetrahedron Neighbor_13
        //{
        //    get
        //    {
        //        lock (_lock)
        //        {
        //            return _neighbor_13;
        //        }
        //    }
        //    set
        //    {
        //        lock (_lock)
        //        {
        //            _neighbor_13 = value;
        //        }
        //    }
        //}

        //private Tetrahedron _neighbor_23 = null;
        //public Tetrahedron Neighbor_23
        //{
        //    get
        //    {
        //        lock (_lock)
        //        {
        //            return _neighbor_23;
        //        }
        //    }
        //    set
        //    {
        //        lock (_lock)
        //        {
        //            _neighbor_23 = value;
        //        }
        //    }
        //}

        #endregion

        // These neighbors share three vertices
        private Tetrahedron _neighbor_012 = null;
        public Tetrahedron Neighbor_012
        {
            get
            {
                lock (_lock)
                {
                    return _neighbor_012;
                }
            }
            set
            {
                lock (_lock)
                {
                    _neighbor_012 = value;
                }
            }
        }

        private Tetrahedron _neighbor_023 = null;
        public Tetrahedron Neighbor_023
        {
            get
            {
                lock (_lock)
                {
                    return _neighbor_023;
                }
            }
            set
            {
                lock (_lock)
                {
                    _neighbor_023 = value;
                }
            }
        }

        private Tetrahedron _neighbor_031 = null;
        public Tetrahedron Neighbor_031
        {
            get
            {
                lock (_lock)
                {
                    return _neighbor_031;
                }
            }
            set
            {
                lock (_lock)
                {
                    _neighbor_031 = value;
                }
            }
        }

        private Tetrahedron _neighbor_132 = null;
        public Tetrahedron Neighbor_132
        {
            get
            {
                lock (_lock)
                {
                    return _neighbor_132;
                }
            }
            set
            {
                lock (_lock)
                {
                    _neighbor_132 = value;
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

        #endregion

        #region Public Methods

        /// <summary>
        /// This is the average of the 4 points
        /// </summary>
        public Point3D GetCenterPoint()
        {
            return Math3D.GetCenter(new[] { this.AllPoints[this.Index0], this.AllPoints[this.Index1], this.AllPoints[this.Index2], this.AllPoints[this.Index3] });
        }

        public Point3D GetPoint(TetrahedronEdge edge, bool isFrom)
        {
            switch (edge)
            {
                case TetrahedronEdge.Edge_01:
                    if (isFrom)
                    {
                        return this.AllPoints[this.Index0];
                    }
                    else
                    {
                        return this.AllPoints[this.Index1];
                    }

                case TetrahedronEdge.Edge_02:
                    if (isFrom)
                    {
                        return this.AllPoints[this.Index0];
                    }
                    else
                    {
                        return this.AllPoints[this.Index2];
                    }

                case TetrahedronEdge.Edge_03:
                    if (isFrom)
                    {
                        return this.AllPoints[this.Index0];
                    }
                    else
                    {
                        return this.AllPoints[this.Index3];
                    }

                case TetrahedronEdge.Edge_12:
                    if (isFrom)
                    {
                        return this.AllPoints[this.Index1];
                    }
                    else
                    {
                        return this.AllPoints[this.Index2];
                    }

                case TetrahedronEdge.Edge_13:
                    if (isFrom)
                    {
                        return this.AllPoints[this.Index1];
                    }
                    else
                    {
                        return this.AllPoints[this.Index3];
                    }

                case TetrahedronEdge.Edge_23:
                    if (isFrom)
                    {
                        return this.AllPoints[this.Index2];
                    }
                    else
                    {
                        return this.AllPoints[this.Index3];
                    }

                default:
                    throw new ApplicationException("Unknown TetrahedronEdge: " + edge.ToString());
            }
        }

        public Point3D GetCommonPoint(TetrahedronEdge edge0, TetrahedronEdge edge1)
        {
            Point3D[] points0 = new Point3D[] { GetPoint(edge0, true), GetPoint(edge0, false) };
            Point3D[] points1 = new Point3D[] { GetPoint(edge1, true), GetPoint(edge1, false) };

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
        public TetrahedronEdge GetCommonEdge(TetrahedronFace face0, TetrahedronFace face1)
        {
            switch (face0)
            {
                case TetrahedronFace.Face_012:
                    switch (face1)
                    {
                        case TetrahedronFace.Face_012:
                            throw new ArgumentException("The two faces can't be identical: " + face0.ToString());

                        case TetrahedronFace.Face_023:
                            return TetrahedronEdge.Edge_02;

                        case TetrahedronFace.Face_031:
                            return TetrahedronEdge.Edge_01;

                        case TetrahedronFace.Face_132:
                            return TetrahedronEdge.Edge_12;

                        default:
                            throw new ApplicationException("Unknown TetrahedronFace: " + face0.ToString());
                    }

                case TetrahedronFace.Face_023:
                    switch (face1)
                    {
                        case TetrahedronFace.Face_023:
                            throw new ArgumentException("The two faces can't be identical: " + face0.ToString());

                        case TetrahedronFace.Face_012:
                            return TetrahedronEdge.Edge_02;

                        case TetrahedronFace.Face_031:
                            return TetrahedronEdge.Edge_03;

                        case TetrahedronFace.Face_132:
                            return TetrahedronEdge.Edge_23;

                        default:
                            throw new ApplicationException("Unknown TetrahedronFace: " + face0.ToString());
                    }

                case TetrahedronFace.Face_031:
                    switch (face1)
                    {
                        case TetrahedronFace.Face_031:
                            throw new ArgumentException("The two faces can't be identical: " + face0.ToString());

                        case TetrahedronFace.Face_012:
                            return TetrahedronEdge.Edge_01;

                        case TetrahedronFace.Face_023:
                            return TetrahedronEdge.Edge_03;

                        case TetrahedronFace.Face_132:
                            return TetrahedronEdge.Edge_13;

                        default:
                            throw new ApplicationException("Unknown TetrahedronFace: " + face0.ToString());
                    }

                case TetrahedronFace.Face_132:
                    switch (face1)
                    {
                        case TetrahedronFace.Face_132:
                            throw new ArgumentException("The two faces can't be identical: " + face0.ToString());

                        case TetrahedronFace.Face_012:
                            return TetrahedronEdge.Edge_12;

                        case TetrahedronFace.Face_023:
                            return TetrahedronEdge.Edge_23;

                        case TetrahedronFace.Face_031:
                            return TetrahedronEdge.Edge_13;

                        default:
                            throw new ApplicationException("Unknown TetrahedronFace: " + face0.ToString());
                    }

                default:
                    throw new ApplicationException("Unknown TetrahedronFace: " + face0.ToString());
            }
        }

        public Point3D GetEdgeMidpoint(TetrahedronEdge edge)
        {
            Point3D point0 = GetPoint(edge, true);
            Point3D point1 = GetPoint(edge, false);

            Vector3D halfLength = (point1 - point0) * .5d;

            return point0 + halfLength;
        }
        public double GetEdgeLength(TetrahedronEdge edge)
        {
            Point3D point0 = GetPoint(edge, true);
            Point3D point1 = GetPoint(edge, false);

            return (point1 - point0).Length;
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

                case 3:
                    return this.Index3;

                default:
                    throw new ArgumentOutOfRangeException("whichIndex", "whichIndex can only be 0, 1, 2, 3: " + whichIndex.ToString());
            }
        }
        public int GetIndex(TetrahedronEdge edge, bool isFrom)
        {
            switch (edge)
            {
                case TetrahedronEdge.Edge_01:
                    if (isFrom)
                    {
                        return this.Index0;
                    }
                    else
                    {
                        return this.Index1;
                    }

                case TetrahedronEdge.Edge_02:
                    if (isFrom)
                    {
                        return this.Index0;
                    }
                    else
                    {
                        return this.Index2;
                    }

                case TetrahedronEdge.Edge_03:
                    if (isFrom)
                    {
                        return this.Index0;
                    }
                    else
                    {
                        return this.Index3;
                    }

                case TetrahedronEdge.Edge_12:
                    if (isFrom)
                    {
                        return this.Index1;
                    }
                    else
                    {
                        return this.Index2;
                    }

                case TetrahedronEdge.Edge_13:
                    if (isFrom)
                    {
                        return this.Index1;
                    }
                    else
                    {
                        return this.Index3;
                    }

                case TetrahedronEdge.Edge_23:
                    if (isFrom)
                    {
                        return this.Index2;
                    }
                    else
                    {
                        return this.Index3;
                    }

                default:
                    throw new ApplicationException("Unknown TetrahedronEdge: " + edge.ToString());
            }
        }
        public int GetCommonIndex(TetrahedronEdge edge0, TetrahedronEdge edge1)
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

        public TriangleIndexedLinked GetFace(TetrahedronFace face)
        {
            switch (face)
            {
                case TetrahedronFace.Face_012:
                    return this.Face_012;

                case TetrahedronFace.Face_023:
                    return this.Face_023;

                case TetrahedronFace.Face_031:
                    return this.Face_031;

                case TetrahedronFace.Face_132:
                    return this.Face_132;

                default:
                    throw new ApplicationException("Unknown TetrahedronFace: " + face.ToString());
            }
        }

        // These let you get at the neighbors with an enum instead of calling the explicit properties
        public Tetrahedron GetNeighbor(TetrahedronFace face)
        {
            switch (face)
            {
                case TetrahedronFace.Face_012:
                    return this.Neighbor_012;

                case TetrahedronFace.Face_023:
                    return this.Neighbor_023;

                case TetrahedronFace.Face_031:
                    return this.Neighbor_031;

                case TetrahedronFace.Face_132:
                    return this.Neighbor_132;

                default:
                    throw new ApplicationException("Unknown TetrahedronFace: " + face.ToString());
            }
        }
        public void SetNeighbor(TetrahedronFace face, Tetrahedron neighbor)
        {
            switch (face)
            {
                case TetrahedronFace.Face_012:
                    this.Neighbor_012 = neighbor;
                    break;

                case TetrahedronFace.Face_023:
                    this.Neighbor_023 = neighbor;
                    break;

                case TetrahedronFace.Face_031:
                    this.Neighbor_031 = neighbor;
                    break;

                case TetrahedronFace.Face_132:
                    this.Neighbor_132 = neighbor;
                    break;

                default:
                    throw new ApplicationException("Unknown TetrahedronFace: " + face.ToString());
            }
        }
        /// <summary>
        /// This overload is easier to use, but is more expensive to execute
        /// </summary>
        public void SetNeighbor(Tetrahedron neighbor)
        {
            // Figure out which face is matching
            foreach (TetrahedronFace face in Faces)
            {
                int[] vertices = GetFace(face).IndexArraySorted;

                if (neighbor.FaceArray.Any(o => o.IsMatch(vertices)))
                {
                    SetNeighbor(face, neighbor);
                    return;
                }
            }

            throw new ApplicationException("Didn't find a matching face");
        }

        //NOTE: These return how the neighbor is related when walking from this to the neighbor
        public TetrahedronFace WhichFace(Tetrahedron neighbor)
        {
            long neighborToken = neighbor.Token;

            if (this.Neighbor_012 != null && this.Neighbor_012.Token == neighborToken)
            {
                return TetrahedronFace.Face_012;
            }
            if (this.Neighbor_023 != null && this.Neighbor_023.Token == neighborToken)
            {
                return TetrahedronFace.Face_023;
            }
            if (this.Neighbor_031 != null && this.Neighbor_031.Token == neighborToken)
            {
                return TetrahedronFace.Face_031;
            }
            if (this.Neighbor_132 != null && this.Neighbor_132.Token == neighborToken)
            {
                return TetrahedronFace.Face_132;
            }
            else
            {
                throw new ApplicationException(string.Format("Not a neighbor\r\n{0}\r\n{1}", this.ToString(), neighbor.ToString()));
            }
        }

        /// <summary>
        /// This links tetrahedrons by face (must share an entire neighboring triangle)
        /// NOTE: The list will be iterated over many times, so make sure it's not some complex linq statement
        /// NOTE: This does not link triangles (though that could be added in the future if needed)
        /// </summary>
        /// <remarks>
        /// This class currently doesn't support edge and corner neighbors.  If needed, implement it
        /// </remarks>
        public static void LinkTetrahedrons(IEnumerable<Tetrahedron> tetrahedrons, bool setNullIfNoLink)
        {
            foreach (Tetrahedron tetra in tetrahedrons)
            {
                long currentToken = tetra.Token;

                foreach (TetrahedronFace face in Tetrahedron.Faces)
                {
                    int[] indicesSorted = tetra.GetFace(face).IndexArraySorted;

                    //TODO: There may be a chance for optimization by telling the neighbor about this as well, but that would involve keeping
                    //track of what's already been compared

                    Tetrahedron neighbor = tetrahedrons.FirstOrDefault(o => o.Token != currentToken && o.FaceArray.Any(p => p.IsMatch(indicesSorted)));
                    if (neighbor != null || setNullIfNoLink)
                    {
                        tetra.SetNeighbor(face, neighbor);
                    }
                }
            }
        }

        /// <summary>
        /// This helps a lot when looking at lists of tetrahedrons in the quick watch
        /// </summary>
        public override string ToString()
        {
            return string.Format("{0} - {1} - {2} - {3}       |       ({4}) ({5}) ({6}) ({7})",
                this.Index0.ToString(),
                this.Index1.ToString(),
                this.Index2.ToString(),
                this.Index3.ToString(),

                this.Point0.ToString(2),
                this.Point1.ToString(2),
                this.Point2.ToString(2),
                this.Point3.ToString(2));
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

        /// <summary>
        /// This looks at all the lines in the triangles passed in, and returns the unique indices
        /// </summary>
        public static Tuple<int, int>[] GetUniqueLines(IEnumerable<Tetrahedron> tetrahedrons)
        {
            List<Tuple<int, int>> retVal = new List<Tuple<int, int>>();

            foreach (Tetrahedron tetra in tetrahedrons)
            {
                foreach (TetrahedronEdge edge in Edges)
                {
                    retVal.Add(GetLine(tetra.GetIndex(edge, true), tetra.GetIndex(edge, false)));
                }
            }

            return retVal.Distinct().ToArray();		//distinct works, because the tuple always has the smaller index as item1
        }

        public bool IsMatch(int[] indeciesSorted)
        {
            int[] mySorted = this.IndexArraySorted;

            // Speed is important, so I'm skipping range checking, and looping

            return mySorted[0] == indeciesSorted[0] &&
                mySorted[1] == indeciesSorted[1] &&
                mySorted[2] == indeciesSorted[2] &&
                mySorted[3] == indeciesSorted[3];
        }

        #endregion

        #region Private Methods

        //NOTE: This doesn't lock itself, it expects the caller to have taken a lock
        protected virtual void OnPointChanged()
        {
            // There is nothing cached that would be affected by points moving (like volume, etc)
        }

        private static Tuple<int, int> GetLine(int index1, int index2)
        {
            return Tuple.Create(Math.Min(index1, index2), Math.Max(index1, index2));
        }

        private static TriangleIndexedLinked FindOrCreateTriangle(int index0, int index1, int index2, Point3D[] allPoints, SortedList<Tuple<int, int, int>, TriangleIndexedLinked> buildingTriangles)
        {
            // Make a key out of these indices
            int[] array = new[] { index0, index1, index2 }.OrderBy(o => o).ToArray();
            Tuple<int, int, int> key = Tuple.Create(array[0], array[1], array[2]);

            // Find or create the triangle
            TriangleIndexedLinked retVal;
            if (!buildingTriangles.TryGetValue(key, out retVal))
            {
                retVal = new TriangleIndexedLinked(index0, index1, index2, allPoints);
                buildingTriangles.Add(key, retVal);
            }

            return retVal;
        }

        #endregion
    }

    #region Enum: TetrahedronEdge

    public enum TetrahedronEdge
    {
        Edge_01,
        Edge_02,
        Edge_03,
        Edge_12,
        Edge_13,
        Edge_23,
    }

    #endregion
    #region Enum: TetrahedronFace

    public enum TetrahedronFace
    {
        Face_012,
        Face_023,
        Face_031,
        Face_132,
    }

    #endregion
}
