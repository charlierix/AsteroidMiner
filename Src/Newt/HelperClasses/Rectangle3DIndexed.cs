using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Game.Newt.HelperClasses
{
    #region Class: Rectangle3DIndexed

    /// <summary>
    /// This represents a 3D rectangle.  It's a similar idea to TriangleIndexed, but a 3D rectangle instead of a triangle
    /// </summary>
    /// <remarks>
    /// TODO: CenterPoint, Area, Rectangle3DIndexedLinked
    /// </remarks>
    public class Rectangle3DIndexed
    {
        #region Constructor

        public Rectangle3DIndexed(Point3D[] allPoints, int[] indices)
        {
            if (indices.Length != 8)
            {
                throw new ArgumentException("indices must have a length of 8: " + indices.Length.ToString());
            }

            _allPoints = allPoints;
            _indices = indices;
        }
        public Rectangle3DIndexed(Point3D[] allPoints, int index_000, int index_001, int index_010, int index_011, int index_100, int index_101, int index_110, int index_111)
        {
            _allPoints = allPoints;
            _indices = new int[] { index_000, index_001, index_010, index_011, index_100, index_101, index_110, index_111 };
        }

        #endregion

        private readonly Point3D[] _allPoints;
        public Point3D[] AllPoints
        {
            get
            {
                return _allPoints;
            }
        }

        private readonly int[] _indices;
        public int[] Indices
        {
            get
            {
                return _indices;
            }
        }

        public int Index(int index_0_to_7)
        {
            return _indices[index_0_to_7];
        }

        public Point3D Point(int index_0_to_7)
        {
            return _allPoints[_indices[index_0_to_7]];
        }

        public Point3D this[int index_0_to_7]
        {
            get
            {
                return _allPoints[_indices[index_0_to_7]];
            }
        }

        // These are the corners
        // Position is XYZ.  0 is min, 1 is max
        public int Index_000
        {
            get
            {
                return _indices[0];
            }
        }
        public int Index_001
        {
            get
            {
                return _indices[1];
            }
        }
        public int Index_010
        {
            get
            {
                return _indices[2];
            }
        }
        public int Index_011
        {
            get
            {
                return _indices[3];
            }
        }
        public int Index_100
        {
            get
            {
                return _indices[4];
            }
        }
        public int Index_101
        {
            get
            {
                return _indices[5];
            }
        }
        public int Index_110
        {
            get
            {
                return _indices[6];
            }
        }
        public int Index_111
        {
            get
            {
                return _indices[7];
            }
        }

        public Point3D Point_000
        {
            get
            {
                return _allPoints[_indices[0]];
            }
        }
        public Point3D Point_001
        {
            get
            {
                return _allPoints[_indices[1]];
            }
        }
        public Point3D Point_010
        {
            get
            {
                return _allPoints[_indices[2]];
            }
        }
        public Point3D Point_011
        {
            get
            {
                return _allPoints[_indices[3]];
            }
        }
        public Point3D Point_100
        {
            get
            {
                return _allPoints[_indices[4]];
            }
        }
        public Point3D Point_101
        {
            get
            {
                return _allPoints[_indices[5]];
            }
        }
        public Point3D Point_110
        {
            get
            {
                return _allPoints[_indices[6]];
            }
        }
        public Point3D Point_111
        {
            get
            {
                return _allPoints[_indices[7]];
            }
        }

        // These are just here as a convenience
        public Point3D AABBMin
        {
            get
            {
                return _allPoints[_indices[0]];
            }
        }
        public Point3D AABBMax
        {
            get
            {
                return _allPoints[_indices[7]];
            }
        }

        public Rect3D ToRect3D()
        {
            return new Rect3D(this.AABBMin, (this.AABBMax - this.AABBMin).ToSize());
        }

        /// <summary>
        /// This is a helper method that returns line segments of all the edges (useful for drawing)
        /// </summary>
        public Tuple<int, int>[] GetEdgeLines()
        {
            List<Tuple<int, int>> retVal = new List<Tuple<int, int>>();

            // Top (z=0)
            retVal.Add(Tuple.Create(this.Index_000, this.Index_100));
            retVal.Add(Tuple.Create(this.Index_100, this.Index_110));
            retVal.Add(Tuple.Create(this.Index_110, this.Index_010));
            retVal.Add(Tuple.Create(this.Index_010, this.Index_000));

            // Bottom (z=1)
            retVal.Add(Tuple.Create(this.Index_001, this.Index_101));
            retVal.Add(Tuple.Create(this.Index_101, this.Index_111));
            retVal.Add(Tuple.Create(this.Index_111, this.Index_011));
            retVal.Add(Tuple.Create(this.Index_011, this.Index_001));

            // Sides
            retVal.Add(Tuple.Create(this.Index_000, this.Index_001));
            retVal.Add(Tuple.Create(this.Index_100, this.Index_101));
            retVal.Add(Tuple.Create(this.Index_110, this.Index_111));
            retVal.Add(Tuple.Create(this.Index_010, this.Index_011));

            return retVal.ToArray();
        }
        public static Tuple<int, int>[] GetEdgeLinesDeduped(IEnumerable<Rectangle3DIndexed> rectangles)
        {
            List<Tuple<int, int>> retVal = new List<Tuple<int, int>>();

            foreach (Rectangle3DIndexed rectangle in rectangles)
            {
                retVal.AddRange(rectangle.GetEdgeLines());
            }

            return retVal.
                Select(o => o.Item1 < o.Item2 ? o : Tuple.Create(o.Item2, o.Item1)).        // need to make sure that item1 is always less than item2 so that distinct can work properly
                Distinct().
                ToArray();
        }

        public ITriangleIndexed[] GetEdgeTriangles()
        {
            //TODO: Make sure all the normals point out

            List<ITriangleIndexed> retVal = new List<ITriangleIndexed>();

            // Top (z=0)
            retVal.Add(new TriangleIndexed(this.Index_000, this.Index_110, this.Index_100, _allPoints));
            retVal.Add(new TriangleIndexed(this.Index_000, this.Index_010, this.Index_110, _allPoints));

            // Bottom (z=1)
            retVal.Add(new TriangleIndexed(this.Index_001, this.Index_101, this.Index_111, _allPoints));
            retVal.Add(new TriangleIndexed(this.Index_001, this.Index_111, this.Index_011, _allPoints));

            // Front (y=0)
            retVal.Add(new TriangleIndexed(this.Index_000, this.Index_100, this.Index_001, _allPoints));
            retVal.Add(new TriangleIndexed(this.Index_001, this.Index_100, this.Index_101, _allPoints));

            // Back (y=1)
            retVal.Add(new TriangleIndexed(this.Index_010, this.Index_011, this.Index_110, _allPoints));
            retVal.Add(new TriangleIndexed(this.Index_011, this.Index_111, this.Index_110, _allPoints));

            // Left (x=0)
            retVal.Add(new TriangleIndexed(this.Index_001, this.Index_010, this.Index_000, _allPoints));
            retVal.Add(new TriangleIndexed(this.Index_001, this.Index_011, this.Index_010, _allPoints));

            // Right (x=1)
            retVal.Add(new TriangleIndexed(this.Index_101, this.Index_100, this.Index_110, _allPoints));
            retVal.Add(new TriangleIndexed(this.Index_101, this.Index_110, this.Index_111, _allPoints));

            return retVal.ToArray();
        }
    }

    #endregion
    #region Class: Rectangle3DIndexedMapped

    public class Rectangle3DIndexedMapped : Rectangle3DIndexed
    {
        public Rectangle3DIndexedMapped(Mapping_3D_1D mapping, Point3D[] allPoints, int[] indices)
            : base(allPoints, indices)
        {
            this.Mapping = mapping;
        }
        public Rectangle3DIndexedMapped(Mapping_3D_1D mapping, Point3D[] allPoints, int index_000, int index_001, int index_010, int index_011, int index_100, int index_101, int index_110, int index_111)
            : base(allPoints, index_000, index_001, index_010, index_011, index_100, index_101, index_110, index_111)
        {
            this.Mapping = mapping;
        }

        public readonly Mapping_3D_1D Mapping;
    }

    #endregion
}
