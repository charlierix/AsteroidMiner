using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Primitives3D;

namespace Game.Newt.Testers
{
    /// <summary>
    /// This tester was written to figure out how to implement MotionController_Linear
    /// </summary>
    /// <remarks>
    /// MotionController_Linear has a disk of evenly distributed neurons, and needs to come up with a single 2D position.  While
    /// writing this, I thought of various ways to find weighted averages, then came up with the idea of contour lines
    /// 
    /// Then the question was: choose the contour polygon with the largest area, largest volume above, or some ratio of the two?
    /// Volume seems to give the best result in most cases (they usually agree, but when they don't, I tend to think volume is the
    /// better one to use.
    /// 
    /// The question after that is what elevation to choose.  I think 66% of the max value gives a pretty good answer
    /// </remarks>
    public partial class ClusteredPoints : Window
    {
        #region Class: PointValue

        private class PointValue
        {
            public Point Point;
            public double Value;
        }

        #endregion
        #region ORIG
        // this moved to Math3D

        //#region Class: ContourWorker

        ///// <summary>
        ///// This is a copy of Math3D.GetIntersection_Mesh_Plane, but returns the triangles along with the two points where they sliced
        ///// </summary>
        //private static class ContourWorker
        //{
        //    #region OLD

        //    //public static Tuple<Point3D[][], ContourTriangleIntersect[][]> GetIntersection_Mesh_Plane(ITriangleIndexed[] mesh, ITriangle plane)
        //    //{
        //    //    List<Tuple<Point3D, Point3D, ITriangle>> lineSegments = GetIntersectingLineSegements(mesh, plane);
        //    //    if (lineSegments == null || lineSegments.Count == 0)
        //    //    {
        //    //        return null;
        //    //    }

        //    //    Point3D[] allPoints;
        //    //    List<Tuple<int, int, ITriangle>> lineInts = ConvertToInts(out allPoints, lineSegments);

        //    //    //TODO: Make sure there are no segments with the same point

        //    //    List<Tuple<Point3D[], ContourTriangleIntersect[]>> polys = new List<Tuple<Point3D[], ContourTriangleIntersect[]>>();

        //    //    while (lineInts.Count > 0)
        //    //    {
        //    //        var poly = GetNextPoly(lineInts, allPoints);
        //    //        if (poly == null)
        //    //        {
        //    //            //return null;
        //    //            continue;
        //    //        }

        //    //        polys.Add(poly);
        //    //    }

        //    //    return Tuple.Create(polys.Select(o => o.Item1).ToArray(), polys.Select(o => o.Item2).ToArray());
        //    //}

        //    ////TODO: Require an array of TriangleIndexedLinked of the whole mesh.  So if there are triangle caps above the intersected triangles, their volume can be added to the whole
        //    ////TODO: Also return the area of each polygon
        //    //public static Tuple<Point3D[], Point3D[][], double>[] GetVolumeAbove(Point3D[][] polygons, ContourTriangleIntersect[][] triangles, ITriangle plane)
        //    //{
        //    //    if (polygons.Length != triangles.Length)        // just comparing the number of polygons to the number of triangle sets
        //    //    {
        //    //        throw new ArgumentException(string.Format("polygons must be the same length as triangles.  polygons={0}, triangles={1]", polygons.Length.ToString(), triangles.Length.ToString()));
        //    //    }

        //    //    // Figure out which polys are inside of others (holes)
        //    //    Tuple<int, int[]>[] polyIslands = GetIslands(polygons, plane);

        //    //    // Get the volume above each island
        //    //    return polyIslands.
        //    //        Select(o => Tuple.Create(
        //    //            polygons[o.Item1],      // outer polygon
        //    //            o.Item2.Select(p => polygons[p]).ToArray(),       // holes
        //    //            GetVolumeAbove_DoIt(o, triangles, plane))).     // volume
        //    //        ToArray();
        //    //}

        //    #endregion

        //    //TODO: Make this the Math3D version (it's not that much more expensive)
        //    public static ContourPolygon_local[] GetIntersection_Mesh_Plane(ITriangleIndexed[] mesh, ITriangle plane)
        //    {
        //        Point3D[][] initalPolys;
        //        ContourTriangleIntersect_local[][] initalTriangles;
        //        if (!GetIntersection_Mesh_Plane_Initial(out initalPolys, out initalTriangles, mesh, plane))
        //        {
        //            return null;
        //        }

        //        // Convert to 2D
        //        Quaternion rotation = Math3D.GetRotation(plane.Normal, new Vector3D(0, 0, 1));
        //        RotateTransform3D rotateTo2D = new RotateTransform3D(new QuaternionRotation3D(rotation));

        //        Point[][] initalPolys2D = initalPolys.Select(o => o.Select(p => rotateTo2D.Transform(p).ToPoint2D()).ToArray()).ToArray();

        //        // Figure out which polys are inside of others (holes)
        //        Tuple<int, int[]>[] polyIslands = Math2D.GetPolygonIslands(initalPolys2D);

        //        return polyIslands.
        //            Select(o => new ContourPolygon_local(
        //                initalPolys[o.Item1],
        //                o.Item2.Select(p => initalPolys[p]).ToArray(),
        //                initalPolys2D[o.Item1],
        //                o.Item2.Select(p => initalPolys2D[p]).ToArray(),
        //                initalTriangles[o.Item1],
        //                o.Item2.Select(p => initalTriangles[p]).ToArray(),
        //                plane)
        //            ).ToArray();
        //    }

        //    #region Private Methods

        //    private static bool GetIntersection_Mesh_Plane_Initial(out Point3D[][] polygons, out ContourTriangleIntersect_local[][] intersectedTriangles, ITriangleIndexed[] mesh, ITriangle plane)
        //    {
        //        List<Tuple<Point3D, Point3D, ITriangle>> lineSegments = GetIntersectingLineSegements(mesh, plane);
        //        if (lineSegments == null || lineSegments.Count == 0)
        //        {
        //            polygons = null;
        //            intersectedTriangles = null;
        //            return false;
        //        }

        //        Point3D[] allPoints;
        //        List<Tuple<int, int, ITriangle>> lineInts = ConvertToInts(out allPoints, lineSegments);

        //        //TODO: Make sure there are no segments with the same point

        //        List<Tuple<Point3D[], ContourTriangleIntersect_local[]>> polys = new List<Tuple<Point3D[], ContourTriangleIntersect_local[]>>();

        //        while (lineInts.Count > 0)
        //        {
        //            var poly = GetNextPoly(lineInts, allPoints);
        //            if (poly == null)
        //            {
        //                //return null;
        //                continue;
        //            }

        //            polys.Add(poly);
        //        }

        //        polygons = polys.Select(o => o.Item1).ToArray();
        //        intersectedTriangles = polys.Select(o => o.Item2).ToArray();
        //        return true;
        //    }

        //    private static List<Tuple<Point3D, Point3D, ITriangle>> GetIntersectingLineSegements(ITriangleIndexed[] triangles, ITriangle plane)
        //    {
        //        // Shoot through all the triangles in the hull, and get line segment intersections
        //        List<Tuple<Point3D, Point3D, ITriangle>> retVal = null;

        //        if (triangles.Length > 100)
        //        {
        //            retVal = triangles.
        //                AsParallel().
        //                Select(o => new { Intersect = Math3D.GetIntersection_Plane_Triangle(plane, o), Triangle = o }).
        //                Where(o => o.Intersect != null).
        //                Select(o => Tuple.Create(o.Intersect.Item1, o.Intersect.Item2, (ITriangle)o.Triangle)).
        //                ToList();
        //        }
        //        else
        //        {
        //            retVal = triangles.
        //                Select(o => new { Intersect = Math3D.GetIntersection_Plane_Triangle(plane, o), Triangle = o }).
        //                Where(o => o.Intersect != null).
        //                Select(o => Tuple.Create(o.Intersect.Item1, o.Intersect.Item2, (ITriangle)o.Triangle)).
        //                ToList();
        //        }

        //        if (retVal.Count < 2)
        //        {
        //            // length of 0 is a clear miss, 1 is just touching
        //            //NOTE: All the points could be colinear, and just touching the hull, but deeper analysis is needed
        //            return null;
        //        }

        //        return retVal;
        //    }

        //    private static List<Tuple<int, int, ITriangle>> ConvertToInts(out Point3D[] allPoints, List<Tuple<Point3D, Point3D, ITriangle>> lineSegments)
        //    {
        //        List<Point3D> dedupedPoints = new List<Point3D>();

        //        List<Tuple<int, int, ITriangle>> retVal = new List<Tuple<int, int, ITriangle>>();

        //        foreach (var segment in lineSegments)
        //        {
        //            retVal.Add(Tuple.Create(ConvertToIntsSprtIndex(dedupedPoints, segment.Item1), ConvertToIntsSprtIndex(dedupedPoints, segment.Item2), segment.Item3));
        //        }

        //        allPoints = dedupedPoints.ToArray();
        //        return retVal;
        //    }
        //    private static int ConvertToIntsSprtIndex(List<Point3D> deduped, Point3D point)
        //    {
        //        for (int cntr = 0; cntr < deduped.Count; cntr++)
        //        {
        //            if (Math3D.IsNearValue(deduped[cntr], point))
        //            {
        //                return cntr;
        //            }
        //        }

        //        deduped.Add(point);
        //        return deduped.Count - 1;
        //    }

        //    private static Tuple<Point3D[], ContourTriangleIntersect_local[]> GetNextPoly(List<Tuple<int, int, ITriangle>> segments, Point3D[] allPoints)
        //    {
        //        List<int> polygon = new List<int>();
        //        List<ContourTriangleIntersect_local> triangles = new List<ContourTriangleIntersect_local>();

        //        polygon.Add(segments[0].Item1);
        //        int currentPoint = segments[0].Item2;
        //        ContourTriangleIntersect_local currentTriangle = new ContourTriangleIntersect_local(allPoints[segments[0].Item1], allPoints[segments[0].Item2], segments[0].Item3);
        //        segments.RemoveAt(0);

        //        while (true)
        //        {
        //            polygon.Add(currentPoint);
        //            triangles.Add(currentTriangle);

        //            // Find the segment that contains currentPoint, and get the other end
        //            Tuple<int, ContourTriangleIntersect_local> nextPoint = GetNextPolySprtNextSegment(segments, currentPoint, allPoints);        //NOTE: This removes the match from segments
        //            if (nextPoint == null)
        //            {
        //                if (polygon.Count < 3)
        //                {
        //                    // This only happens when the intersection is along a single triangle (where the plane just barely touches a triangle)
        //                    return null;
        //                }
        //                else
        //                {
        //                    // This is a fragment of a polygon.  Exit early, and it will be considered a full polygon
        //                    break;
        //                }
        //            }

        //            if (nextPoint.Item1 == polygon[0])
        //            {
        //                // This polygon is complete
        //                triangles.Add(nextPoint.Item2);     // polygon is a list of points, but triangles is a list of segments.  So polygon infers this last segment, but triangles needs to have it explicitely added
        //                break;
        //            }

        //            currentPoint = nextPoint.Item1;
        //            currentTriangle = nextPoint.Item2;
        //        }

        //        return Tuple.Create(polygon.Select(o => allPoints[o]).ToArray(), triangles.ToArray());
        //    }
        //    private static Tuple<int, ContourTriangleIntersect_local> GetNextPolySprtNextSegment(List<Tuple<int, int, ITriangle>> segments, int currentPoint, Point3D[] allPoints)
        //    {
        //        int? nextPoint = null;
        //        int index = -1;

        //        for (int cntr = 0; cntr < segments.Count; cntr++)
        //        {
        //            if (segments[cntr].Item1 == currentPoint)
        //            {
        //                // Found at 1, return point 2
        //                nextPoint = segments[cntr].Item2;
        //                index = cntr;
        //                break;
        //            }
        //            else if (segments[cntr].Item2 == currentPoint)
        //            {
        //                // Found at 2, return point 1
        //                nextPoint = segments[cntr].Item1;
        //                index = cntr;
        //                break;
        //            }
        //        }

        //        if (nextPoint == null)
        //        {
        //            return null;
        //        }
        //        else
        //        {
        //            // Grab the triangle for this segment
        //            ContourTriangleIntersect_local triangle = new ContourTriangleIntersect_local(allPoints[currentPoint], allPoints[nextPoint.Value], segments[index].Item3);

        //            // Remove this segment from the list of remaining segments
        //            segments.RemoveAt(index);

        //            return Tuple.Create(nextPoint.Value, triangle);
        //        }
        //    }

        //    #endregion
        //}

        //#endregion
        //#region Class: ContourTriangleIntersect

        //private class ContourTriangleIntersect_local
        //{
        //    public ContourTriangleIntersect_local(Point3D intersect1, Point3D intersect2, ITriangle triangle)
        //    {
        //        this.Intersect1 = intersect1;
        //        this.Intersect2 = intersect2;
        //        this.Triangle = triangle;
        //    }

        //    public readonly Point3D Intersect1;
        //    public readonly Point3D Intersect2;
        //    public readonly ITriangle Triangle;
        //}

        //#endregion
        //#region Class: ContourPolygon

        //private class ContourPolygon_local
        //{
        //    public ContourPolygon_local(Point3D[] polygon3D, Point3D[][] holes3D, Point[] polygon2D, Point[][] holes2D, ContourTriangleIntersect_local[] intersectedTriangles, ContourTriangleIntersect_local[][] intersectedTrianglesHoles, ITriangle plane)
        //    {
        //        this.Polygon3D = polygon3D;
        //        this.Holes3D = holes3D;

        //        this.Polygon2D = polygon2D;
        //        this.Holes2D = holes2D;

        //        this.IntersectedTriangles = intersectedTriangles;
        //        this.IntersectedTrianglesHoles = intersectedTrianglesHoles;

        //        this.Plane = plane;
        //    }

        //    public readonly Point3D[] Polygon3D;
        //    public readonly Point3D[][] Holes3D;

        //    // This is the same polygon and holes, but rotated onto the XY plane
        //    public readonly Point[] Polygon2D;
        //    public readonly Point[][] Holes2D;

        //    // These are the triangles and intersect points where the polygons sliced through
        //    public readonly ContourTriangleIntersect_local[] IntersectedTriangles;
        //    public readonly ContourTriangleIntersect_local[][] IntersectedTrianglesHoles;

        //    public readonly ITriangle Plane;

        //    /// <summary>
        //    /// This calculates the volume of the polygon above the plane
        //    /// WARNING: The intersect triangles need to be of type TriangleIndexedLinked, or this method will throw an exception (they need to
        //    /// be linked by edges, no need to link corners)
        //    /// </summary>
        //    public double GetVolumeAbove()
        //    {
        //        // All the perimiter triangles must be done here.  Otherwise holes will cause problems
        //        long[] permiters = UtilityHelper.Iterate(this.IntersectedTriangles, this.IntersectedTrianglesHoles.SelectMany(o => o)).
        //            Select(o => o.Triangle.Token).
        //            ToArray();

        //        List<long> seenMiddles = new List<long>();

        //        // Main Polygon
        //        double retVal = GetVolumeAbove_DoIt(this.IntersectedTriangles, this.Plane, permiters, seenMiddles);

        //        // Holes
        //        foreach (var hole in this.IntersectedTrianglesHoles)
        //        {
        //            //NOTE: Adding to the total, not subtracting.  This is because the part of the triangle that is above the plane is leaning
        //            //over into the main part of the polygon (the hole is the part of the triangle that's below the plane)
        //            retVal += GetVolumeAbove_DoIt(hole, this.Plane, permiters, seenMiddles);
        //        }

        //        return retVal;
        //    }

        //    private static double GetVolumeAbove_DoIt(ContourTriangleIntersect_local[] triangles, ITriangle plane, long[] permiters, List<long> seenMiddles)
        //    {
        //        double retVal = 0;

        //        foreach (ContourTriangleIntersect_local triangle in triangles)
        //        {
        //            TriangleIndexedLinked triangleCast = triangle.Triangle as TriangleIndexedLinked;
        //            if (triangleCast == null)
        //            {
        //                throw new ArgumentException("Triangles must be of type TriangleIndexedLinked");
        //            }

        //            #region Edge triangle

        //            // Get the points that are above the plane, and the corresponding point on the plane
        //            var abovePoints = new TriangleCorner[] { TriangleCorner.Corner_0, TriangleCorner.Corner_1, TriangleCorner.Corner_2 }.
        //                Where(o => Math3D.IsAbovePlane(plane, triangleCast.GetPoint(o))).
        //                Select(o => new { Corner = o, AbovePoint = triangleCast.GetPoint(o), PlanePoint = Math3D.GetClosestPoint_Point_Plane(plane, triangleCast.GetPoint(o)) }).
        //                ToArray();

        //            foreach (var abovePoint in abovePoints)     // there will either be 1 or 2 points
        //            {
        //                // Get the area of the base
        //                Vector3D baseVect = triangle.Intersect2 - triangle.Intersect1;
        //                double baseHeight = (Math3D.GetClosestPoint_Point_Line(triangle.Intersect1, baseVect, abovePoint.PlanePoint) - abovePoint.PlanePoint).Length;
        //                double area = baseVect.Length * baseHeight / 2d;

        //                // Get the 3D height
        //                double hullHeight = (abovePoint.AbovePoint - abovePoint.PlanePoint).Length;

        //                // Now add this volume to the total
        //                retVal += area * hullHeight / 3d;
        //            }

        //            #endregion

        //            if (abovePoints.Length == 2)
        //            {
        //                #region Middle triangles

        //                TriangleEdge edge = TriangleIndexedLinked.GetEdge(abovePoints[0].Corner, abovePoints[1].Corner);

        //                TriangleIndexedLinked neighbor = triangleCast.GetNeighbor(edge);
        //                if (neighbor != null && !permiters.Contains(neighbor.Token) && !seenMiddles.Contains(neighbor.Token))
        //                {
        //                    //NOTE: This is a recursive method
        //                    retVal += GetVolumeAbove_Middle(neighbor, permiters, seenMiddles, plane);
        //                }

        //                #endregion
        //            }
        //        }

        //        return retVal;
        //    }
        //    private static double GetVolumeAbove_Middle(TriangleIndexedLinked triangle, long[] permiters, List<long> seenMiddles, ITriangle plane)
        //    {
        //        seenMiddles.Add(triangle.Token);

        //        if (!triangle.IndexArray.All(o => Math3D.IsAbovePlane(plane, triangle.AllPoints[o])))
        //        {
        //            // This seems to only happen when the plane is Z=0
        //            return 0;
        //        }

        //        #region Get the volume under this triangle

        //        Point3D[] points = new Point3D[]
        //        {
        //            Math3D.GetClosestPoint_Point_Plane(plane, triangle.Point0),     // 0
        //            Math3D.GetClosestPoint_Point_Plane(plane, triangle.Point1),     // 1
        //            Math3D.GetClosestPoint_Point_Plane(plane, triangle.Point2),     // 2
        //            triangle.Point0,        // 3
        //            triangle.Point1,        // 4
        //            triangle.Point2     // 5
        //        };

        //        List<TriangleIndexed> hull = new List<TriangleIndexed>();

        //        // Bottom
        //        hull.Add(new TriangleIndexed(0, 1, 2, points));

        //        // Top
        //        hull.Add(new TriangleIndexed(3, 4, 5, points));

        //        // Face 0-1
        //        hull.Add(new TriangleIndexed(0, 1, 3, points));
        //        hull.Add(new TriangleIndexed(0, 1, 4, points));

        //        // Face 1-2
        //        hull.Add(new TriangleIndexed(1, 2, 4, points));
        //        hull.Add(new TriangleIndexed(1, 2, 5, points));

        //        // Face 2-0
        //        hull.Add(new TriangleIndexed(0, 2, 3, points));
        //        hull.Add(new TriangleIndexed(0, 2, 5, points));

        //        double retVal = Math3D.GetVolume_ConvexHull(hull.ToArray());

        //        #endregion

        //        // Try each of the three neighbors
        //        foreach (TriangleEdge edge in new TriangleEdge[] { TriangleEdge.Edge_01, TriangleEdge.Edge_12, TriangleEdge.Edge_20 })
        //        {
        //            TriangleIndexedLinked neighbor = triangle.GetNeighbor(edge);        // neighbor should never be null in practice - just in some edge cases :D
        //            if (neighbor != null && !permiters.Contains(neighbor.Token) && !seenMiddles.Contains(neighbor.Token))
        //            {
        //                // Recurse
        //                retVal += GetVolumeAbove_Middle(neighbor, permiters, seenMiddles, plane);
        //            }
        //        }

        //        return retVal;
        //    }
        //}

        //#endregion
        #endregion

        #region Declaration Section

        private const double RADIUS = 5;

        private const double DOTRADIUS = .05d;
        private const double LINETHICKNESS = DOTRADIUS / 2d;

        /// <summary>
        /// This listens to the mouse/keyboard and controls the camera
        /// </summary>
        private TrackBallRoam _trackball = null;

        private PointValue[] _points = null;

        private List<Visual3D> _visuals = new List<Visual3D>();

        private bool _isInitialized = false;

        #endregion

        #region Constructor

        public ClusteredPoints()
        {
            InitializeComponent();

            #region Trackball

            // Trackball
            _trackball = new TrackBallRoam(_camera);
            _trackball.KeyPanScale = 1d;
            _trackball.EventSource = grdViewPort;		//NOTE:  If this control doesn't have a background color set, the trackball won't see events (I think transparent is ok, just not null)
            _trackball.AllowZoomOnMouseWheel = true;

            _trackball.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.MouseComplete));
            //_trackball.GetOrbitRadius += new GetOrbitRadiusHandler(Trackball_GetOrbitRadius);
            //_trackball.ShouldHitTestOnOrbit = true;

            #endregion

            _isInitialized = true;
        }

        #endregion

        #region Event Listeners

        private void btnGenerateEven_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int count;
                if (!int.TryParse(txtGenerateCount.Text, out count))
                {
                    MessageBox.Show("Couldn't parse the number of points", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _points = Math3D.GetRandomVectors_Circular_EvenDist(count, RADIUS, .03d, 1000, null, null, null).
                    Select(o => new PointValue() { Point = o.ToPoint(), Value = trkStartValue.Value }).
                    ToArray();

                RedrawPoints();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnGenerateClustered_Click(object sender, RoutedEventArgs e)
        {
            double FACTOR = .7;

            try
            {
                int count;
                if (!int.TryParse(txtGenerateCount.Text, out count))
                {
                    MessageBox.Show("Couldn't parse the number of points", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Calculate min distance
                double area = Math.PI * RADIUS * RADIUS;
                area *= FACTOR;		// give some slack (factor needs to be less than 1, the smaller it is, the closer they're allowed to be together)
                area /= count;		// the sum of the little volumes can't exceed the total volume

                double minDistance = Math.Sqrt(area / Math.PI);		// run the area equation backward to get the radius

                _points = Math3D.GetRandomVectors_Circular_ClusteredMinDist(count, RADIUS, minDistance, .03d, 1000, null, null, null).
                    Select(o => new PointValue() { Point = o.ToPoint(), Value = trkStartValue.Value }).
                    ToArray();

                RedrawPoints();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnSetSameValues_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_points == null)
                {
                    MessageBox.Show("Need to add points first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                for (int cntr = 0; cntr < _points.Length; cntr++)
                {
                    _points[cntr].Value = trkStartValue.Value;
                }

                RedrawPoints();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnSetRandomValues_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_points == null)
                {
                    MessageBox.Show("Need to add points first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Random rand = StaticRandom.GetRandomForThread();

                for (int cntr = 0; cntr < _points.Length; cntr++)
                {
                    _points[cntr].Value = rand.NextDouble();
                }

                RedrawPoints();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnSetRandomValues2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_points == null)
                {
                    MessageBox.Show("Need to add points first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Random rand = StaticRandom.GetRandomForThread();

                for (int cntr = 0; cntr < _points.Length; cntr++)
                {
                    _points[cntr].Value = rand.NextPow(2, 1, false);
                }

                RedrawPoints();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnSetRandomValues4_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_points == null)
                {
                    MessageBox.Show("Need to add points first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Random rand = StaticRandom.GetRandomForThread();

                for (int cntr = 0; cntr < _points.Length; cntr++)
                {
                    _points[cntr].Value = rand.NextPow(4, 1, false);
                }

                RedrawPoints();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnSetRandomValues8_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_points == null)
                {
                    MessageBox.Show("Need to add points first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Random rand = StaticRandom.GetRandomForThread();

                for (int cntr = 0; cntr < _points.Length; cntr++)
                {
                    _points[cntr].Value = rand.NextPow(8, 1, false);
                }

                RedrawPoints();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnSetRandomValues16_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_points == null)
                {
                    MessageBox.Show("Need to add points first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Random rand = StaticRandom.GetRandomForThread();

                for (int cntr = 0; cntr < _points.Length; cntr++)
                {
                    _points[cntr].Value = rand.NextPow(16, 1, false);
                }

                RedrawPoints();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnSetRandomValues32_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_points == null)
                {
                    MessageBox.Show("Need to add points first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Random rand = StaticRandom.GetRandomForThread();

                for (int cntr = 0; cntr < _points.Length; cntr++)
                {
                    _points[cntr].Value = rand.NextPow(32, 1, false);
                }

                RedrawPoints();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnSetRandomValues64_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_points == null)
                {
                    MessageBox.Show("Need to add points first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Random rand = StaticRandom.GetRandomForThread();

                for (int cntr = 0; cntr < _points.Length; cntr++)
                {
                    _points[cntr].Value = rand.NextPow(64, 1, false);
                }

                RedrawPoints();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnSetRandomValues128_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_points == null)
                {
                    MessageBox.Show("Need to add points first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Random rand = StaticRandom.GetRandomForThread();

                for (int cntr = 0; cntr < _points.Length; cntr++)
                {
                    _points[cntr].Value = rand.NextPow(128, 1, false);
                }

                RedrawPoints();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RadioHeight_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_isInitialized)
                {
                    return;
                }

                if (radShowContour.IsChecked.Value || radShowTriangles1.IsChecked.Value || radShowTriangles2.IsChecked.Value || radShowContourAndTriangles.IsChecked.Value)
                {
                    chkShowTriangleEdges.Visibility = Visibility.Visible;
                }
                else
                {
                    chkShowTriangleEdges.Visibility = Visibility.Collapsed;
                }

                if (radShowContour.IsChecked.Value || radShowContourAndTriangles.IsChecked.Value)
                {
                    grdNumContours.Visibility = Visibility.Visible;
                }
                else
                {
                    grdNumContours.Visibility = Visibility.Collapsed;
                }

                RedrawPoints();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void chkShowTriangleEdges_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_isInitialized)
                {
                    return;
                }

                RedrawPoints();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void trkNumContours_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (!_isInitialized)
                {
                    return;
                }

                RedrawPoints();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void trkTriangleHeight_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (!_isInitialized)
                {
                    return;
                }

                RedrawPoints();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnKMeans_Click(object sender, RoutedEventArgs e)
        {
            const double MINFILTERPERCENT = .5d;
            const double MAXERROR = 1;

            try
            {
                if (_points == null)
                {
                    MessageBox.Show("Need to add points first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int numGroups;
                if (!int.TryParse(txtNumGroups.Text, out numGroups))
                {
                    MessageBox.Show("Couldn't parse the number of groups", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (numGroups > _points.Length)
                {
                    MessageBox.Show("The number of groups needs to be less than the number of points", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Start with a clean visual
                RedrawPoints();



                // I don't think it's worth the effort to make this work.  By having a fixed number of clusters up front, things
                // could get unstable
                MessageBox.Show("finish this");
                return;





                //http://scikit-learn.org/stable/modules/clustering.html



                // Get initial centers
                Point[] centers = Math3D.GetRandomVectors_Circular_EvenDist(numGroups, RADIUS).Select(o => o.ToPoint()).ToArray();

                double error = double.MaxValue;

                while (error > MAXERROR)
                {
                    //TODO: Filter out neurons that are two low
                    //// Get the max value
                    //double max = _points.Max(o => o.Value);
                    //double min = max * MINFILTERPERCENT;     // any value less than min will be thought of as a value of zero

                    //var voronoi = Math2D.CapVoronoiCircle(Math2D.GetVoronoi(centers, true));
                    var voronoi = Math2D.GetVoronoi(centers, false);

                    //----Step 1) Assign points to each voronoi center
                    //NOTE: Can't just use position.  If that were the case, this would just be an even division of points, and wouldn't
                    //consider value (and could just be precalculated).

                    // The goal is the each of the centers to contain the same volume




                    //----Step 2) Get the volume of each voronoi poly






                }

                // The winning voronoi polygon isn't necessarily the one with the highest value, it would need to be the
                // highest density.  This is because a polygon could be huge, but each point has a low volume




            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void trkContourTestHeight_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (!_isInitialized)
                {
                    return;
                }

                btnContour_Click(this, new RoutedEventArgs());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void trkContourTestCount_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (!_isInitialized)
                {
                    return;
                }

                btnContour_Click(this, new RoutedEventArgs());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnContour_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RedrawPoints();

                if (_points == null || _points.Length == 0)
                {
                    return;
                }

                #region Get terrain triangles

                // Cut a contour slice at the height of trkContourTestHeight
                var triangles = GetDelaunay_ExpandRing(_points, trkTriangleHeight.Value);

                List<TriangleIndexedLinked> trianglesLinked = triangles.Item2.Select(o => new TriangleIndexedLinked(o.Index0, o.Index1, o.Index2, o.AllPoints)).ToList();
                TriangleIndexedLinked.LinkTriangles_Edges(trianglesLinked, true);

                #endregion

                int contourCount = Convert.ToInt32(trkContourTestCount.Value);

                double minHeight = trkContourTestHeight.Value;
                double maxHeight = 1;
                if (chkContourTestHeightIsPercent.IsChecked.Value)
                {
                    double maxValue = _points.Max(o => o.Value);
                    minHeight *= maxValue;
                    maxHeight *= maxValue;
                }

                double heightStep = (maxHeight - minHeight) / Convert.ToDouble(contourCount);

                for (int contourCntr = 0; contourCntr < contourCount; contourCntr++)
                {
                    double height = minHeight + (heightStep * contourCntr);
                    double heightScaled = height * trkTriangleHeight.Value;

                    Triangle plane = new Triangle(new Point3D(-1, 0, heightScaled), new Point3D(1, 0, heightScaled), new Point3D(0, 1, heightScaled));      // normal needs to point up

                    // Get the contour polygons
                    var polys = Math3D.GetIntersection_Mesh_Plane(trianglesLinked.ToArray(), plane);
                    if (polys == null || polys.Length == 0)
                    {
                        return;
                    }

                    #region Get area, volume

                    var densities = polys.Select(o =>
                        {
                            double area = Math2D.GetAreaPolygon(o.Polygon2D);

                            foreach (Point[] hole in o.Holes2D)
                            {
                                area -= Math2D.GetAreaPolygon(hole);
                            }

                            double volume = o.GetVolumeAbove();

                            return new { Area = area, Volume = volume, Density = volume / area };
                        }).ToArray();

                    int winnerIndexArea = 0;
                    int winnerIndexVolume = 0;
                    double maxArea = densities[0].Area;
                    double maxVolume = densities[0].Volume;
                    for (int cntr = 1; cntr < densities.Length; cntr++)
                    {
                        if (densities[cntr].Area > maxArea)
                        {
                            maxArea = densities[cntr].Area;
                            winnerIndexArea = cntr;
                        }

                        if (densities[cntr].Volume > maxVolume)
                        {
                            maxVolume = densities[cntr].Volume;
                            winnerIndexVolume = cntr;
                        }
                    }

                    #endregion

                    #region Draw Poly Islands

                    BillboardLine3DSet holes = null;
                    if (polys.Any(o => o.Holes3D.Length > 0))
                    {
                        holes = new BillboardLine3DSet();

                        holes.Color = UtilityWPF.ColorFromHex("68BAA0BA");
                        holes.IsReflectiveColor = false;
                        holes.BeginAddingLines();
                    }

                    for (int cntr = 0; cntr < polys.Length; cntr++)
                    {
                        // Main polygon
                        BillboardLine3DSet lines = new BillboardLine3DSet();

                        if (cntr == winnerIndexArea && cntr == winnerIndexVolume)
                        {
                            lines.Color = Color.FromRgb(192, 64, 192);
                        }
                        else if (cntr == winnerIndexArea)
                        {
                            lines.Color = Color.FromRgb(192, 64, 64);
                        }
                        else if (cntr == winnerIndexVolume)
                        {
                            lines.Color = Color.FromRgb(64, 64, 192);
                        }
                        else
                        {
                            lines.Color = UtilityWPF.GetRandomColor(255, 64, 64, 100, 200, 64, 64);
                        }

                        lines.IsReflectiveColor = false;
                        lines.BeginAddingLines();

                        AddPolyLines(lines, polys[cntr].Polygon3D, LINETHICKNESS);

                        // Winner Point
                        if (cntr == winnerIndexArea || cntr == winnerIndexVolume)
                        {
                            Point3D polyCenter = Math3D.GetCenter(polys[cntr].Polygon3D);

                            lines.AddLine(new Point3D(polyCenter.X, polyCenter.Y, 0), new Point3D(polyCenter.X, polyCenter.Y, RADIUS * 2), LINETHICKNESS * 2);
                        }

                        //// Volume
                        //lines.AddLine(polys[cntr].Polygon3D[0], polys[cntr].Polygon3D[0] + new Vector3D(0, 0, densities[cntr].Volume * 10), LINETHICKNESS * 5);

                        //// Area
                        //int halfIndex = polys[cntr].Polygon3D.Length / 2;
                        //lines.AddLine(polys[cntr].Polygon3D[halfIndex], polys[cntr].Polygon3D[halfIndex] + new Vector3D(0, 0, densities[cntr].Area * 10), LINETHICKNESS);

                        ////Density just doesn't seem to add value.  The height will attract a larger polygon, so relating volume to area doesn't make much sense
                        //// Density
                        //int quarterIndex = polys[cntr].Polygon3D.Length / 4;
                        //lines.AddLine(polys[cntr].Polygon3D[quarterIndex], polys[cntr].Polygon3D[quarterIndex] + new Vector3D(0, 0, densities[cntr].Density * 10), LINETHICKNESS * 2);

                        lines.EndAddingLines();

                        _visuals.Add(lines);
                        _viewport.Children.Add(lines);

                        // Holes
                        foreach (Point3D[] polyHole in polys[cntr].Holes3D)
                        {
                            AddPolyLines(holes, polyHole, LINETHICKNESS);
                        }
                    }

                    if (holes != null)
                    {
                        holes.EndAddingLines();

                        _visuals.Add(holes);
                        _viewport.Children.Add(holes);
                    }

                    #endregion
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private void RedrawPoints()
        {
            // Clear old
            _viewport.Children.RemoveAll(_visuals);
            _visuals.Clear();

            if (_points == null || _points.Length == 0)
            {
                return;
            }

            #region Points

            Model3DGroup geometries = new Model3DGroup();

            for (int cntr = 0; cntr < _points.Length; cntr++)
            {
                Color color = UtilityWPF.AlphaBlend(Colors.Black, UtilityWPF.ColorFromHex("08000000"), _points[cntr].Value);

                // Material
                MaterialGroup materials = new MaterialGroup();
                materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
                materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(color, Colors.White, .5d)), 100d));

                // Geometry Model
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = materials;
                geometry.BackMaterial = materials;
                geometry.Geometry = UtilityWPF.GetSphere_LatLon(2, DOTRADIUS);

                geometry.Transform = new TranslateTransform3D(_points[cntr].Point.ToVector3D());

                geometries.Children.Add(geometry);
            }

            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = geometries;

            _visuals.Add(visual);
            _viewport.Children.Add(visual);

            #endregion

            if (radShowTriangles1.IsChecked.Value || radShowTriangles2.IsChecked.Value || radShowContour.IsChecked.Value || radShowContourAndTriangles.IsChecked.Value)
            {
                #region Height Triangles, Contour

                //var triangles = Math2D.GetDelaunayTriangulation(_points.Select(o => o.Point).ToArray(), _points.Select(o => o.Point.ToPoint3D() + new Vector3D(0, 0, o.Value * trkTriangleHeight.Value)).ToArray());
                var triangles = GetDelaunay_ExpandRing(_points, trkTriangleHeight.Value);

                if (radShowTriangles1.IsChecked.Value || radShowContourAndTriangles.IsChecked.Value)
                {
                    #region Triangles (mono color)

                    Color color = UtilityWPF.ColorFromHex("40808080");

                    // Material
                    MaterialGroup materials = new MaterialGroup();
                    materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
                    materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(UtilityWPF.AlphaBlend(color, Colors.White, .5d), Colors.Transparent, .25d)), 5d));

                    // Geometry Model
                    GeometryModel3D geometry = new GeometryModel3D();
                    geometry.Material = materials;
                    geometry.BackMaterial = materials;
                    geometry.Geometry = UtilityWPF.GetMeshFromTriangles_IndependentFaces(triangles.Item2);

                    visual = new ModelVisual3D();
                    visual.Content = geometry;

                    _visuals.Add(visual);
                    _viewport.Children.Add(visual);

                    #endregion
                }
                else if (radShowTriangles2.IsChecked.Value)
                {
                    #region Triangles (height color)

                    geometries = new Model3DGroup();

                    foreach (var triangle in triangles.Item2)
                    {
                        double averageHeight = Math1D.Avg(triangles.Item1[triangle.Index0].Value, triangles.Item1[triangle.Index1].Value, triangles.Item1[triangle.Index2].Value);
                        Color color = UtilityWPF.AlphaBlend(Colors.Black, Colors.White, averageHeight);

                        // Material
                        Material material = UtilityWPF.GetUnlitMaterial(Color.FromArgb(192, color.R, color.G, color.B));     // Don't want reflection, it will just confuse the coloring

                        // Geometry Model
                        GeometryModel3D geometry = new GeometryModel3D();
                        geometry.Material = material;
                        geometry.BackMaterial = material;
                        geometry.Geometry = UtilityWPF.GetMeshFromTriangles(new ITriangleIndexed[] { triangle });

                        geometries.Children.Add(geometry);
                    }

                    visual = new ModelVisual3D();
                    visual.Content = geometries;

                    _visuals.Add(visual);
                    _viewport.Children.Add(visual);

                    #endregion
                }

                if (radShowContour.IsChecked.Value || radShowContourAndTriangles.IsChecked.Value)
                {
                    #region Contours

                    //TODO: Instead of just triangles, make parabola hills
                    //var triangles = GetDelaunay_ExpandRing(_points, trkTriangleHeight.Value);

                    int numContours = Convert.ToInt32(trkNumContours.Value);

                    for (int cntr = 0; cntr < numContours; cntr++)
                    {
                        double height = Convert.ToDouble(cntr + 1) / Convert.ToDouble(numContours + 1);     // one contour would be 50%, two would be 33% and 66%, etc
                        double heightScaled = height * trkTriangleHeight.Value;

                        var polys = Math3D.GetIntersection_Mesh_Plane(triangles.Item2, new Triangle(new Point3D(-1, 0, heightScaled), new Point3D(1, 0, heightScaled), new Point3D(0, 1, heightScaled)));

                        if (polys != null && polys.Length > 0)
                        {
                            BillboardLine3DSet lines = new BillboardLine3DSet();

                            lines.Color = UtilityWPF.AlphaBlend(Colors.Black, Colors.White, height);
                            lines.IsReflectiveColor = false;
                            lines.BeginAddingLines();

                            for (int outer = 0; outer < polys.Length; outer++)
                            {
                                AddPolyLines(lines, polys[outer].Polygon3D, LINETHICKNESS);

                                for (int inner = 0; inner < polys[outer].Holes3D.Length; inner++)
                                {
                                    AddPolyLines(lines, polys[outer].Holes3D[inner], LINETHICKNESS);
                                }
                            }

                            lines.EndAddingLines();

                            _visuals.Add(lines);
                            _viewport.Children.Add(lines);
                        }
                    }

                    #endregion
                }

                if (chkShowTriangleEdges.IsChecked.Value)
                {
                    #region Edge Lines

                    BillboardLine3DSet lines = new BillboardLine3DSet();

                    lines.Color = UtilityWPF.ColorFromHex("80808080");
                    lines.IsReflectiveColor = false;
                    lines.BeginAddingLines();

                    Point3D[] trianglePoints = triangles.Item2[0].AllPoints;

                    foreach (var line in TriangleIndexed.GetUniqueLines(triangles.Item2))
                    {
                        lines.AddLine(trianglePoints[line.Item1], trianglePoints[line.Item2], LINETHICKNESS);
                    }

                    lines.EndAddingLines();

                    _visuals.Add(lines);
                    _viewport.Children.Add(lines);

                    #endregion
                }

                #endregion
            }
            else if (radShowLines.IsChecked.Value)
            {
                #region Height Lines

                BillboardLine3DSet lines = new BillboardLine3DSet();

                lines.Color = UtilityWPF.ColorFromHex("80808080");
                lines.IsReflectiveColor = false;
                lines.BeginAddingLines();

                for (int cntr = 0; cntr < _points.Length; cntr++)
                {
                    lines.AddLine(_points[cntr].Point.ToPoint3D(), _points[cntr].Point.ToPoint3D() + new Vector3D(0, 0, _points[cntr].Value * trkTriangleHeight.Value), LINETHICKNESS);
                }

                lines.EndAddingLines();

                _visuals.Add(lines);
                _viewport.Children.Add(lines);

                #endregion
            }
        }

        /// <summary>
        /// This is needed when the points passed in could have values greater than zero at the edge.  This creates an extra ring of
        /// points that always have a value of zero.  That way any mesh made out of this won't just stop abruptly
        /// </summary>
        private static Tuple<PointValue[], TriangleIndexed[]> GetDelaunay_ExpandRing(PointValue[] points, double heightMult)
        {
            if (points.Length == 0)
            {
                return null;
            }

            // Get the average min distance between the points
            List<Tuple<int, int, double>> distances = new List<Tuple<int, int, double>>();

            for (int outer = 0; outer < points.Length - 1; outer++)
            {
                for (int inner = outer + 1; inner < points.Length; inner++)
                {
                    double distance = (points[outer].Point - points[inner].Point).LengthSquared;

                    distances.Add(Tuple.Create(outer, inner, distance));
                }
            }

            double avgDist;
            if (distances.Count == 0)
            {
                avgDist = points[0].Point.ToVector().Length;
                if (Math1D.IsNearZero(avgDist))
                {
                    avgDist = .1;
                }
            }
            else
            {
                avgDist = Enumerable.Range(0, points.Length).
                    Select(o => distances.
                        Where(p => p.Item1 == o || p.Item2 == o).       // get the disances that mention this index
                        Min(p => p.Item3)).     // only keep the smallest of those distances
                    Average();      // get the average of all the mins

                avgDist = Math.Sqrt(avgDist);
            }

            double maxDist = points.Max(o => o.Point.ToVector().LengthSquared);
            maxDist = Math.Sqrt(maxDist);

            double radius = maxDist + avgDist;

            // Create a ring of points at that distance
            List<PointValue> expandedPoints = new List<PointValue>(points);

            int numExtra = GetNumExtraPoints(points.Length);

            expandedPoints.AddRange(Math2D.GetCircle_Cached(numExtra).Select(o => new PointValue() { Point = new Point(o.X * radius, o.Y * radius), Value = 0 }));

            // Get the delaunay of all these points
            TriangleIndexed[] triangles = Math2D.GetDelaunayTriangulation(expandedPoints.Select(o => o.Point).ToArray(), expandedPoints.Select(o => o.Point.ToPoint3D() + new Vector3D(0, 0, o.Value * heightMult)).ToArray());

            // Exit Function
            return Tuple.Create(expandedPoints.ToArray(), triangles);
        }

        private static int GetNumExtraPoints(int count)
        {
            const int MIN = 6;

            //TODO: .5 is good for small numbers, but over 100, the % of extra should drop off.  By 1000, % should probably be .1
            int retVal = Convert.ToInt32(Math.Ceiling(count * .5));

            if (retVal < MIN)
            {
                return MIN;
            }
            else
            {
                return retVal;
            }
        }

        private static void AddPolyLines(BillboardLine3DSet lines, Point3D[] polygon, double thickness)
        {
            for (int cntr = 0; cntr < polygon.Length - 1; cntr++)
            {
                lines.AddLine(polygon[cntr], polygon[cntr + 1], thickness);
            }

            lines.AddLine(polygon[polygon.Length - 1], polygon[0], thickness);
        }

        #region OLD

        //private static Point3D[][] GetIntersection_Mesh_Plane(ITriangleIndexed[] mesh, ITriangle plane)
        //{
        //    List<Tuple<Point3D, Point3D>> lineSegments = GetIntersectingLineSegements(mesh, plane);
        //    if (lineSegments == null || lineSegments.Count == 0)
        //    {
        //        return null;
        //    }

        //    Point3D[] allPoints;
        //    List<Tuple<int, int>> lineInts = ConvertToInts(out allPoints, lineSegments);

        //    //TODO: Make sure there are no segments with the same point

        //    List<Point3D[]> polys = new List<Point3D[]>();

        //    while (lineInts.Count > 0)
        //    {
        //        Point3D[] poly = GetNextPoly(lineInts, allPoints);
        //        if (poly == null)
        //        {
        //            //return null;
        //            continue;
        //        }

        //        polys.Add(poly);
        //    }

        //    return polys.ToArray();
        //}

        //private static List<Tuple<int, int>> ConvertToInts(out Point3D[] allPoints, List<Tuple<Point3D, Point3D>> lineSegments)
        //{
        //    List<Point3D> dedupedPoints = new List<Point3D>();

        //    List<Tuple<int, int>> retVal = new List<Tuple<int, int>>();

        //    foreach (var segment in lineSegments)
        //    {
        //        retVal.Add(Tuple.Create(ConvertToIntsSprtIndex(dedupedPoints, segment.Item1), ConvertToIntsSprtIndex(dedupedPoints, segment.Item2)));
        //    }

        //    allPoints = dedupedPoints.ToArray();
        //    return retVal;
        //}
        //private static int ConvertToIntsSprtIndex(List<Point3D> deduped, Point3D point)
        //{
        //    for (int cntr = 0; cntr < deduped.Count; cntr++)
        //    {
        //        if (Math3D.IsNearValue(deduped[cntr], point))
        //        {
        //            return cntr;
        //        }
        //    }

        //    deduped.Add(point);
        //    return deduped.Count - 1;
        //}

        //private static Point3D[] GetNextPoly(List<Tuple<int, int>> segments, Point3D[] allPoints)
        //{
        //    List<int> retVal = new List<int>();

        //    retVal.Add(segments[0].Item1);
        //    int currentPoint = segments[0].Item2;
        //    segments.RemoveAt(0);

        //    while (true)
        //    {
        //        retVal.Add(currentPoint);

        //        // Find the segment that contains currentPoint, and get the other end
        //        int? nextPoint = GetNextPolySprtNextSegment(segments, currentPoint);        //NOTE: This removes the match from segments
        //        if (nextPoint == null)
        //        {
        //            if (retVal.Count < 3)
        //            {
        //                //throw new ApplicationException("Incomplete polygon");
        //                return null;
        //            }
        //            else
        //            {
        //                // This is a fragment of a polygon.  Exit early, and it will be considered a full polygon
        //                break;
        //            }
        //        }

        //        if (nextPoint.Value == retVal[0])
        //        {
        //            // This polygon is complete
        //            break;
        //        }

        //        currentPoint = nextPoint.Value;
        //    }

        //    return retVal.Select(o => allPoints[o]).ToArray();
        //}
        //private static int? GetNextPolySprtNextSegment(List<Tuple<int, int>> segments, int currentPoint)
        //{
        //    int? retVal = null;

        //    for (int cntr = 0; cntr < segments.Count; cntr++)
        //    {
        //        if (segments[cntr].Item1 == currentPoint)
        //        {
        //            retVal = segments[cntr].Item2;
        //            segments.RemoveAt(cntr);
        //            break;
        //        }
        //        else if (segments[cntr].Item2 == currentPoint)
        //        {
        //            retVal = segments[cntr].Item1;
        //            segments.RemoveAt(cntr);
        //            break;
        //        }
        //    }

        //    return retVal;
        //}

        ////Copied from HullTriangleIntersect
        //private static List<Tuple<Point3D, Point3D>> GetIntersectingLineSegements(ITriangleIndexed[] triangles, ITriangle plane)
        //{
        //    // Shoot through all the triangles in the hull, and get line segment intersections
        //    List<Tuple<Point3D, Point3D>> retVal = null;

        //    if (triangles.Length > 100)
        //    {
        //        retVal = triangles.
        //            AsParallel().
        //            Select(o => Math3D.GetIntersection_Plane_Triangle(plane, o)).
        //            Where(o => o != null).
        //            ToList();
        //    }
        //    else
        //    {
        //        retVal = triangles.
        //            Select(o => Math3D.GetIntersection_Plane_Triangle(plane, o)).
        //            Where(o => o != null).
        //            ToList();
        //    }

        //    if (retVal.Count < 2)
        //    {
        //        // length of 0 is a clear miss, 1 is just touching
        //        //NOTE: All the points could be colinear, and just touching the hull, but deeper analysis is needed
        //        return null;
        //    }

        //    return retVal;
        //}

        #endregion

        #endregion
    }
}
