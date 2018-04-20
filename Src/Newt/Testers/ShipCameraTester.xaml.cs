using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Schedulers;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using System.Xaml;

using Game.HelperClassesCore;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.GameItems.ShipParts;
using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Controls3D;

namespace Game.Newt.Testers
{
    public partial class ShipCameraTester : Window
    {
        #region Class: ViewportOffline

        private class ViewportOffline
        {
            public ViewportOffline(Brush background)
            {
                this.Viewport = new Viewport3D();

                this.Camera = new PerspectiveCamera(new Point3D(0, 0, 25), new Vector3D(0, 0, -1), new Vector3D(0, 1, 0), 45d);
                this.Viewport.Camera = this.Camera;

                Model3DGroup lightGroup = new Model3DGroup();
                lightGroup.Children.Add(new AmbientLight(UtilityWPF.ColorFromHex("808080")));
                lightGroup.Children.Add(new DirectionalLight(UtilityWPF.ColorFromHex("FFFFFF"), new Vector3D(1, -1, -1)));
                lightGroup.Children.Add(new DirectionalLight(UtilityWPF.ColorFromHex("303030"), new Vector3D(-1, 1, 1)));

                ModelVisual3D lightModel = new ModelVisual3D();
                lightModel.Content = lightGroup;

                this.Viewport.Children.Add(lightModel);

                // Viewport3D won't render to a bitmap when it's not part of the visual tree, but a border containing a viewport will
                Border border = new Border();
                border.Background = background;
                border.Child = this.Viewport;
                this.Control = border;
            }

            public readonly FrameworkElement Control;
            public readonly Viewport3D Viewport;
            public readonly PerspectiveCamera Camera;
            public readonly List<Visual3D> Visuals = new List<Visual3D>();

            public void SyncCamera(PerspectiveCamera camera)
            {
                this.Camera.Position = camera.Position;
                this.Camera.LookDirection = camera.LookDirection;
                this.Camera.UpDirection = camera.UpDirection;
                this.Camera.FieldOfView = camera.FieldOfView;
            }
        }

        #endregion
        #region Class: TriangleTileOverlay

        private static class TriangleTileOverlay
        {
            #region Class: OverlayResult

            public class OverlayResult
            {
                public OverlayResult(int x, int y, double percent, Point[] intersection)
                {
                    this.X = x;
                    this.Y = y;
                    this.Percent = percent;
                    this.Intersection = intersection;
                }

                public readonly int X;
                public readonly int Y;
                public readonly double Percent;

                //TODO: Remove this
                public readonly Point[] Intersection;
            }

            #endregion

            /// <summary>
            /// This figures out which tiles each triangle intersects
            /// </summary>
            /// <param name="size">The size of the grid</param>
            /// <param name="pixelsX">How many tiles along X</param>
            /// <param name="pixelsY">How many tiles along Y</param>
            /// <param name="triangles">The triangles should be shifted to cover the grid, Z is ignored</param>
            /// <returns>
            /// [index of triangle][array of tiles it covers]
            /// </returns>
            public static OverlayResult[][] GetIntersections(out Rect[] pixelRects, Size size, int pixelsX, int pixelsY, ITriangle[] triangles)
            {
                OverlayResult[][] retVal = new OverlayResult[triangles.Length][];

                // Convert each pixel into a rectangle
                Tuple<Rect, int, int>[] tiles = GetTiles(size, pixelsX, pixelsY);

                for (int cntr = 0; cntr < triangles.Length; cntr++)
                {
                    // See which tiles this triangle intersects (and how much of each tile)
                    retVal[cntr] = IntersectTiles(triangles[cntr], tiles);
                }

                // Exit Function
                pixelRects = tiles.Select(o => o.Item1).ToArray();
                return retVal;
            }
            public static OverlayResult[][] GetIntersections(out Rect[] pixelRects, Size size, int pixelsX, int pixelsY, Point[][] polygons)
            {
                OverlayResult[][] retVal = new OverlayResult[polygons.Length][];

                // Convert each pixel into a rectangle
                Tuple<Rect, int, int>[] tiles = GetTiles(size, pixelsX, pixelsY);

                for (int cntr = 0; cntr < polygons.Length; cntr++)
                {
                    // See which tiles this triangle intersects (and how much of each tile)
                    retVal[cntr] = IntersectTiles(polygons[cntr], tiles);
                }

                // Exit Function
                pixelRects = tiles.Select(o => o.Item1).ToArray();
                return retVal;
            }

            #region Private Methods

            private static Tuple<Rect, int, int>[] GetTiles(Size size, int pixelsX, int pixelsY)
            {
                Tuple<Rect, int, int>[] retVal = new Tuple<Rect, int, int>[pixelsX * pixelsY];

                Size cellSize = new Size(size.Width / Convert.ToDouble(pixelsX), size.Height / Convert.ToDouble(pixelsY));

                for (int y = 0; y < pixelsY; y++)
                {
                    int offsetY = y * pixelsX;

                    for (int x = 0; x < pixelsX; x++)
                    {
                        retVal[offsetY + x] = Tuple.Create(new Rect(cellSize.Width * Convert.ToDouble(x), cellSize.Height * Convert.ToDouble(y), cellSize.Width, cellSize.Height), x, y);
                    }
                }

                return retVal;
            }

            private static OverlayResult[] IntersectTiles(ITriangle triangle, Tuple<Rect, int, int>[] tiles)
            {
                List<OverlayResult> retVal = new List<OverlayResult>();

                Point min = new Point(
                    Math1D.Min(triangle.Point0.X, triangle.Point1.X, triangle.Point2.X),
                    Math1D.Min(triangle.Point0.Y, triangle.Point1.Y, triangle.Point2.Y));

                Point max = new Point(
                    Math1D.Max(triangle.Point0.X, triangle.Point1.X, triangle.Point2.X),
                    Math1D.Max(triangle.Point0.Y, triangle.Point1.Y, triangle.Point2.Y));

                foreach (var tile in tiles)
                {
                    // AABB check
                    if (tile.Item1.Left > max.X)
                    {
                        continue;		// triangle is left of the rectangle
                    }
                    else if (tile.Item1.Right < min.X)
                    {
                        continue;		// triangle is right of the rectangle
                    }
                    else if (tile.Item1.Top > max.Y)
                    {
                        continue;		// triangle is above rectangle
                    }
                    else if (tile.Item1.Bottom < min.Y)
                    {
                        continue;		// triangle is below rectangle
                    }

                    // See if the triangle is completely inside the rectangle
                    if (min.X >= tile.Item1.Left && max.X <= tile.Item1.Right && min.Y >= tile.Item1.Top && max.Y <= tile.Item1.Bottom)
                    {
                        double areaTriangle = triangle.NormalLength / 2d;
                        double areaRect = tile.Item1.Width * tile.Item1.Height;

                        if (areaTriangle > areaRect)
                        {
                            throw new ApplicationException(string.Format("Area of contained triangle is larger than the rectangle that contains it: triangle={0}, rectangle={1}", areaTriangle.ToString(), areaRect.ToString()));
                        }

                        Point[] triangle2D = new Point[] { new Point(triangle.Point0.X, triangle.Point0.Y), new Point(triangle.Point1.X, triangle.Point1.Y), new Point(triangle.Point2.X, triangle.Point2.Y) };

                        retVal.Add(new OverlayResult(tile.Item2, tile.Item3, areaTriangle / areaRect, triangle2D));
                        continue;
                    }

                    // Intersect triangle with rect
                    //double? percent = GetPercentCovered(triangle, tile.Item1);
                    var percent = GetPercentCovered(triangle, tile.Item1);
                    if (percent != null)
                    {
                        retVal.Add(new OverlayResult(tile.Item2, tile.Item3, percent.Item1, percent.Item2));
                    }
                }

                return retVal.ToArray();
            }
            private static OverlayResult[] IntersectTiles(Point[] polygon, Tuple<Rect, int, int>[] tiles)
            {
                List<OverlayResult> retVal = new List<OverlayResult>();

                #region Get polygon AABB

                double minX = double.MaxValue;
                double minY = double.MaxValue;
                double maxX = double.MinValue;
                double maxY = double.MinValue;

                for (int cntr = 0; cntr < polygon.Length; cntr++)
                {
                    if (polygon[cntr].X < minX)
                    {
                        minX = polygon[cntr].X;
                    }

                    if (polygon[cntr].X > maxX)
                    {
                        maxX = polygon[cntr].X;
                    }

                    if (polygon[cntr].Y < minY)
                    {
                        minY = polygon[cntr].Y;
                    }

                    if (polygon[cntr].Y > maxY)
                    {
                        maxY = polygon[cntr].Y;
                    }
                }

                #endregion

                foreach (var tile in tiles)
                {
                    // AABB check
                    if (tile.Item1.Left > maxX)
                    {
                        continue;		// polygon is left of the rectangle
                    }
                    else if (tile.Item1.Right < minX)
                    {
                        continue;		// polygon is right of the rectangle
                    }
                    else if (tile.Item1.Top > maxY)
                    {
                        continue;		// polygon is above rectangle
                    }
                    else if (tile.Item1.Bottom < minY)
                    {
                        continue;		// polygon is below rectangle
                    }

                    // See if the polygon is completely inside the rectangle
                    if (minX >= tile.Item1.Left && maxX <= tile.Item1.Right && minY >= tile.Item1.Top && maxY <= tile.Item1.Bottom)
                    {
                        double areaPoly = Math2D.GetAreaPolygon(polygon);
                        double areaRect = tile.Item1.Width * tile.Item1.Height;

                        if (areaPoly > areaRect)
                        {
                            throw new ApplicationException(string.Format("Area of contained polygon is larger than the rectangle that contains it: polygon={0}, rectangle={1}", areaPoly.ToString(), areaRect.ToString()));
                        }

                        retVal.Add(new OverlayResult(tile.Item2, tile.Item3, areaPoly / areaRect, polygon));
                        continue;
                    }

                    // Intersect polygon with rect
                    var percent = GetPercentCovered(polygon, tile.Item1);
                    if (percent != null)
                    {
                        retVal.Add(new OverlayResult(tile.Item2, tile.Item3, percent.Item1, percent.Item2));
                    }
                }

                return retVal.ToArray();
            }

            private static Tuple<double, Point[]> GetPercentCovered(ITriangle triangle, Rect rect)
            {
                // Figure out the intersected polygon
                Point[] intersection = Math2D.GetIntersection_Polygon_Polygon(
                    new Point[] { new Point(triangle.Point0.X, triangle.Point0.Y), new Point(triangle.Point1.X, triangle.Point1.Y), new Point(triangle.Point2.X, triangle.Point2.Y) },
                    new Point[] { rect.TopLeft, rect.TopRight, rect.BottomRight, rect.BottomLeft });

                if (intersection == null || intersection.Length == 0)
                {
                    return null;
                }

                // Calculate the area of the polygon
                double areaPolygon = Math2D.GetAreaPolygon(intersection);

                double areaRect = rect.Width * rect.Height;

                if (areaPolygon > areaRect)
                {
                    if (areaPolygon > areaRect * 1.01)
                    {
                        throw new ApplicationException(string.Format("Area of intersected polygon is larger than the rectangle that clipped it: polygon={0}, rectangle={1}", areaPolygon.ToString(), areaRect.ToString()));
                    }
                    areaPolygon = areaRect;
                }

                return Tuple.Create(areaPolygon / areaRect, intersection);
            }
            private static Tuple<double, Point[]> GetPercentCovered(Point[] polygon, Rect rect)
            {
                // Figure out the intersected polygon
                Point[] intersection = Math2D.GetIntersection_Polygon_Polygon(
                    polygon,
                    new Point[] { rect.TopLeft, rect.TopRight, rect.BottomRight, rect.BottomLeft });

                if (intersection == null || intersection.Length == 0)
                {
                    return null;
                }

                // Calculate the area of the polygon
                double areaPolygon = Math2D.GetAreaPolygon(intersection);

                double areaRect = rect.Width * rect.Height;

                if (areaPolygon > areaRect)
                {
                    if (areaPolygon > areaRect * 1.01)
                    {
                        throw new ApplicationException(string.Format("Area of intersected polygon is larger than the rectangle that clipped it: polygon={0}, rectangle={1}", areaPolygon.ToString(), areaRect.ToString()));
                    }
                    areaPolygon = areaRect;
                }

                return Tuple.Create(areaPolygon / areaRect, intersection);
            }

            /// <summary>
            /// a1 is line1 start, a2 is line1 end, b1 is line2 start, b2 is line2 end
            /// </summary>
            /// <remarks>
            /// Got this here:
            /// http://stackoverflow.com/questions/14480124/how-do-i-detect-triangle-and-rectangle-intersection
            /// 
            /// A similar article:
            /// http://www.flipcode.com/archives/Point-Plane_Collision.shtml
            /// </remarks>
            private static Vector? Intersects(Vector a1, Vector a2, Vector b1, Vector b2)
            {
                Vector b = Vector.Subtract(a2, a1);
                Vector d = Vector.Subtract(b2, b1);
                double bDotDPerp = (b.X * d.Y) - (b.Y * d.X);

                // if b dot d == 0, it means the lines are parallel so have infinite intersection points
                if (Math1D.IsNearZero(bDotDPerp))
                    return null;

                Vector c = Vector.Subtract(b1, a1);
                double t = (c.X * d.Y - c.Y * d.X) / bDotDPerp;
                if (t < 0 || t > 1)
                    return null;

                double u = (c.X * b.Y - c.Y * b.X) / bDotDPerp;
                if (u < 0 || u > 1)
                    return null;

                // Return the intersection point
                return Vector.Add(a1, Vector.Multiply(b, t));
            }

            #endregion
        }

        #endregion

        #region Declaration Section

        private EditorOptions _editorOptions = new EditorOptions();
        private ItemOptions _itemOptions = new ItemOptions();

        /// <summary>
        /// This listens to the mouse/keyboard and controls the camera
        /// </summary>
        private TrackBallRoam _trackball = null;

        private List<Visual3D> _visuals = new List<Visual3D>();

        private ViewportOffline _offline1 = null;

        // These are just for the even dist test buttons
        private Vector3D[] _evenDistPoints = null;

        private ShipCameraTesterSnapshot _snapshotViewer = null;

        private StaTaskScheduler _staScheduler = null;

        private CameraPool _cameraPool = null;
        private List<CameraPoolVisual> _cameraPoolVisuals = new List<CameraPoolVisual>();

        private bool _isInitialized = false;

        #endregion

        #region Constructor

        public ShipCameraTester()
        {
            InitializeComponent();

            _isInitialized = true;
        }

        #endregion

        #region Event Listeners

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                #region Trackball

                // Trackball
                _trackball = new TrackBallRoam(_camera);
                _trackball.KeyPanScale = 1d;
                _trackball.EventSource = grdViewPort;		//NOTE:  If this control doesn't have a background color set, the trackball won't see events (I think transparent is ok, just not null)
                _trackball.AllowZoomOnMouseWheel = true;

                _trackball.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.MouseComplete));
                //_trackball.GetOrbitRadius += new GetOrbitRadiusHandler(Trackball_GetOrbitRadius);
                _trackball.ShouldHitTestOnOrbit = true;

                #endregion

                _camera.Changed += new EventHandler(Camera_Changed);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            try
            {
                if (_cameraPool != null)
                {
                    _cameraPool.Dispose();
                    _cameraPool = null;
                }

                if (_staScheduler != null)
                {
                    _staScheduler.Dispose();
                    _staScheduler = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Camera_Changed(object sender, EventArgs e)
        {
            try
            {
                if (chkSnapshotAuto.IsChecked.Value)
                {
                    btnViewSnapshot_Click(this, new RoutedEventArgs());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnSimple1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearScene();

                Color color = Colors.DodgerBlue;

                // Material
                MaterialGroup materials = new MaterialGroup();
                materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
                materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(color, Colors.White, .5d)), 100d));

                // Geometry Model
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = materials;
                geometry.BackMaterial = materials;
                geometry.Geometry = UtilityWPF.GetCone_AlongX(3, 1d, 3d);

                geometry.Transform = new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRandomRotation()));

                // Add it
                AddVisual(geometry);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnRGB_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_offline1 == null)
                {
                    MessageBox.Show("Start a scene first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ShipPartDNA energyDNA = new ShipPartDNA() { PartType = EnergyTank.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(), Scale = new Vector3D(10, 10, 10) };
                EnergyTank energy = new EnergyTank(_editorOptions, _itemOptions, energyDNA);
                energy.QuantityCurrent = energy.QuantityMax;

                ShipPartDNA dna = new ShipPartDNA() { PartType = CameraColorRGB.PARTTYPE, Orientation = Quaternion.Identity, Position = new Point3D(0, 0, 0), Scale = new Vector3D(1, 1, 1) };

                CameraColorRGB camera = new CameraColorRGB(_editorOptions, _itemOptions, dna, energy, _cameraPool);

                camera.RequestWorldLocation += new EventHandler<PartRequestWorldLocationArgs>(TestCamera_RequestWorldLocation);

                //var location = camera.GetWorldLocation_Camera();

                //_offline1.SyncCamera(_camera);
                //IBitmapCustom bitmap = UtilityWPF.RenderControl(_offline1.Control, camera.PixelWidthHeight, camera.PixelWidthHeight, true, Colors.Black, false);

                //camera.StoreSnapshot(bitmap);

                camera.Update_MainThread(1);
                camera.Update_AnyThread(1);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void TestCamera_RequestWorldLocation(object sender, PartRequestWorldLocationArgs e)
        {
            e.Position = new Point3D(0, 0, 0);
            e.Orientation = Quaternion.Identity;
        }

        private void btnViewSnapshot_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_offline1 == null)
                {
                    MessageBox.Show("Start a scene first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                #region Parse ints

                int numNeurons;
                if (!int.TryParse(txtSnapshotCones.Text, out numNeurons))
                {
                    MessageBox.Show("Couldn't parse the number of neurons as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int numPixelsX;
                if (!int.TryParse(txtSnapshotPixelsX.Text, out numPixelsX))
                {
                    MessageBox.Show("Couldn't parse the number of x pixels as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int numPixelsY;
                if (!int.TryParse(txtSnapshotPixelsY.Text, out numPixelsY))
                {
                    MessageBox.Show("Couldn't parse the number of y pixels as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                #endregion

                if (_snapshotViewer == null)
                {
                    _snapshotViewer = new ShipCameraTesterSnapshot();
                    _snapshotViewer.Show();
                }

                TriangleIndexed[] triangles = null;
                Size triangleExtremes;
                Point[][] polygons = null;
                Size polygonExtremes;
                bool isNewTriangles = true;
                if (_snapshotViewer.NumNeurons != numNeurons)
                {
                    #region Build triangles

                    //  Get the vertices of the triangles
                    Vector[] vertices = Math3D.GetRandomVectors_Circular_CenterPacked(Enumerable.Range(0, numNeurons + 2).Select(o => Math3D.GetRandomVector_Circular(1d)).Select(o => new Vector(o.X, o.Y)).ToArray(), 1d, .03d, 1000, null, null, null);

                    #region Figure out the extremes

                    Point min = new Point(vertices.Min(o => o.X), vertices.Min(o => o.Y));
                    Point max = new Point(vertices.Max(o => o.X), vertices.Max(o => o.Y));

                    double width = max.X - min.X;
                    double height = max.Y - min.Y;

                    // Enlarge a bit
                    min.X -= width * .05d;
                    min.Y -= height * .05d;
                    max.X += width * .05d;
                    max.Y += height * .05d;

                    width = max.X - min.X;
                    height = max.Y - min.Y;

                    Vector3D offset = new Vector3D(-min.X, -min.Y, 0d);

                    #endregion

                    // Make the 3D vertices shifted over so that min is at 0,0
                    Point3D[] points3D = vertices.Select(o => new Point3D(o.X + offset.X, o.Y + offset.Y, 0d)).ToArray();

                    // Build triangles
                    triangles = Math2D.GetDelaunayTriangulation(vertices.Select(o => o.ToPoint()).ToArray(), points3D);

                    triangleExtremes = new Size(width, height);

                    #endregion
                    #region Build polygons

                    //  Get the control points
                    Point[] controlPoints = Math3D.GetRandomVectors_Circular_CenterPacked(numNeurons, 1d, .03d, 1000, null, null, null).Select(o => o.ToPoint()).ToArray();

                    var voronoi = Math2D.CapVoronoiCircle(Math2D.GetVoronoi(controlPoints, true));

                    #region Figure out the extremes

                    min = new Point(voronoi.EdgePoints.Min(o => o.X), voronoi.EdgePoints.Min(o => o.Y));
                    max = new Point(voronoi.EdgePoints.Max(o => o.X), voronoi.EdgePoints.Max(o => o.Y));

                    width = max.X - min.X;
                    height = max.Y - min.Y;

                    // Enlarge a bit
                    min.X -= width * .05d;
                    min.Y -= height * .05d;
                    max.X += width * .05d;
                    max.Y += height * .05d;

                    width = max.X - min.X;
                    height = max.Y - min.Y;

                    //offset = new Vector3D(-min.X, -min.Y, 0d);
                    Vector offset2 = new Vector(-min.X, -min.Y);

                    #endregion

                    // Build polygons
                    polygons = new Point[controlPoints.Length][];
                    for (int cntr = 0; cntr < controlPoints.Length; cntr++)
                    {
                        Edge2D[] edges = voronoi.EdgesByControlPoint[cntr].Select(o => voronoi.Edges[o]).ToArray();
                        polygons[cntr] = Edge2D.GetPolygon(edges, 1d).Select(o => o + offset2).ToArray();       // don't need to worry about ray length, they are all segments.  shifting the points by offset
                    }

                    polygonExtremes = new Size(width, height);

                    #endregion
                }
                else
                {
                    triangles = _snapshotViewer.Triangles;
                    triangleExtremes = _snapshotViewer.TriangleExtremes;
                    polygons = _snapshotViewer.Polygons;
                    polygonExtremes = _snapshotViewer.PolygonExtremes;
                    isNewTriangles = false;
                }

                if (isNewTriangles || _snapshotViewer.Pixels == null || _snapshotViewer.Pixels.Item1 != numPixelsX || _snapshotViewer.Pixels.Item2 != numPixelsY)
                {
                    #region Build triangles

                    //  Figure out which pixels each triangle overlaps
                    Rect[] pixelRects;
                    var overlay = TriangleTileOverlay.GetIntersections(out pixelRects, triangleExtremes, numPixelsX, numPixelsY, triangles);

                    ShipCameraTesterSnapshot.TriangleOverlay[] snapshotTriangles = new ShipCameraTesterSnapshot.TriangleOverlay[triangles.Length];

                    for (int cntr = 0; cntr < triangles.Length; cntr++)
                    {
                        snapshotTriangles[cntr] = new ShipCameraTesterSnapshot.TriangleOverlay(triangles[cntr], overlay[cntr].Select(o => Tuple.Create(o.X, o.Y, o.Percent)).ToArray());
                    }

                    #endregion
                    #region Build polygons

                    //  Figure out which pixels each polygon overlaps
                    overlay = TriangleTileOverlay.GetIntersections(out pixelRects, polygonExtremes, numPixelsX, numPixelsY, polygons);

                    ShipCameraTesterSnapshot.PolygonOverlay[] snapshotPolygons = new ShipCameraTesterSnapshot.PolygonOverlay[polygons.Length];

                    for (int cntr = 0; cntr < polygons.Length; cntr++)
                    {
                        snapshotPolygons[cntr] = new ShipCameraTesterSnapshot.PolygonOverlay(null, polygons[cntr], overlay[cntr].Select(o => Tuple.Create(o.X, o.Y, o.Percent)).ToArray());
                    }

                    #endregion

                    _snapshotViewer.SetTriangles(numNeurons, snapshotTriangles, triangleExtremes, snapshotPolygons, polygonExtremes, Tuple.Create(numPixelsX, numPixelsY, pixelRects));
                }

                //  Take a picture
                _offline1.SyncCamera(_camera);
                IBitmapCustom bitmap = UtilityWPF.RenderControl(_offline1.Control, numPixelsX, numPixelsY, true, Colors.Black, false);

                _snapshotViewer.UpdateBitmap(bitmap);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnBells_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                //  When adding two bell curves together, an x offset of 3.4 times stddev gives a pretty good flat curve

                //GetBellCurve(x, 1, 




            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnPerfResolution_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //  Results;
                //5: 4764
                //10: 4722
                //15: 4739
                //20: 4747
                //25: 4726
                //30: 4757
                //35: 4744
                //40: 4767
                //45: 4794
                //50: 4763
                //55: 4781
                //60: 4798
                //65: 4799
                //70: 4832
                //75: 4823
                //80: 4841
                //85: 4852
                //90: 4870
                //95: 4884
                //255: 5438
                //505: 7481
                //755: 10503
                //1005: 14679

                List<Tuple<int, TimeSpan>> times = new List<Tuple<int, TimeSpan>>();

                for (int resolution = 5; resolution < 100; resolution += 5)
                //for (int resolution = 5; resolution < 1100; resolution += 250)
                {
                    DateTime start = DateTime.UtcNow;

                    for (int cntr = 0; cntr < 100; cntr++)
                    {
                        IBitmapCustom bitmap = UtilityWPF.RenderControl(_offline1.Control, resolution, resolution, true, Colors.Transparent, false);
                    }

                    times.Add(Tuple.Create(resolution, DateTime.UtcNow - start));
                }

                string report = string.Join("\r\n", times.Select(o => o.Item1.ToString() + ": " + Math.Round(o.Item2.TotalMilliseconds).ToString()).ToArray());

                MessageBox.Show(report, this.Title, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnThreads1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                #region Build model in main thread

                Color color = Colors.DodgerBlue;

                // Material
                MaterialGroup materials = new MaterialGroup();
                materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
                materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(color, Colors.White, .5d)), 100d));

                // Geometry Model
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = materials;
                geometry.BackMaterial = materials;
                geometry.Geometry = UtilityWPF.GetCone_AlongX(3, 1d, 3d);

                geometry.Transform = new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRandomRotation()));

                #endregion

                //NOTE: WPF explicitly throws an exception
                Task.Factory.StartNew(() =>
                    {
                        ThreadTest1(geometry);
                    });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnThreads2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                #region Build model in main thread

                Color color = Colors.DodgerBlue;

                // Material
                MaterialGroup materials = new MaterialGroup();
                materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
                materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(color, Colors.White, .5d)), 100d));

                // Geometry Model
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = materials;
                geometry.BackMaterial = materials;
                geometry.Geometry = UtilityWPF.GetCone_AlongX(3, 1d, 3d);

                geometry.Transform = new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRandomRotation()));


                Model3DGroup models = new Model3DGroup();
                models.Children.Add(geometry);


                #endregion

                #region Serialize it

                byte[] geometryBytes = null;

                using (MemoryStream stream = new MemoryStream())
                {
                    //XamlServices.Save(stream, geometry);
                    XamlServices.Save(stream, models);
                    stream.Position = 0;

                    geometryBytes = stream.ToArray();
                }

                #endregion

                //  Viewport3D must be created in an STA thread
                if (_staScheduler == null)
                {
                    _staScheduler = new StaTaskScheduler(3);
                }

                Task.Factory.StartNew(() =>
                {
                    ThreadTest2(geometryBytes);
                }, CancellationToken.None, TaskCreationOptions.None, _staScheduler);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnOffline1Snapshot_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                const int WIDTH = 30;
                const int HEIGHT = 30;

                // == works as expected.  It seems to do a deep compare
                //Color test1 = Color.FromArgb(100, 200, 50, 75);
                //Color test2 = Color.FromArgb(100, 200, 50, 75);

                //bool isEqual = test1 == test2;


                //IBitmapCustom bitmap1 = UtilityWPF.RenderControl(grdViewPort, WIDTH, HEIGHT, true, Colors.Transparent, true);

                IBitmapCustom bitmap1 = UtilityWPF.RenderControl(_offline1.Control, WIDTH, HEIGHT, true, Colors.Transparent, false);
                IBitmapCustom bitmap2 = UtilityWPF.RenderControl(_offline1.Control, WIDTH, HEIGHT, false, Colors.Transparent, false);


                Color color1 = bitmap1.GetColor(WIDTH / 2, HEIGHT / 2);
                Color color2 = bitmap2.GetColor(WIDTH / 2, HEIGHT / 2);

                #region Validate

                if (color1 != color2)
                {
                    throw new ApplicationException("fail");
                }

                #endregion

                Color[] colors1a = bitmap1.GetColors(WIDTH / 4, HEIGHT / 4, WIDTH / 2, HEIGHT / 2);
                Color[] colors2a = bitmap2.GetColors(WIDTH / 4, HEIGHT / 4, WIDTH / 2, HEIGHT / 2);		// this one is returning all the #FF000000

                #region Validate

                if (colors1a.Length != colors2a.Length)
                {
                    throw new ApplicationException("fail");
                }

                for (int cntr = 0; cntr < colors1a.Length; cntr++)
                {
                    if (colors1a[cntr] != colors2a[cntr])
                    {
                        throw new ApplicationException("fail");
                    }
                }

                #endregion

                Color[] colors1b = bitmap1.GetColors(-5, -5, WIDTH + 10, HEIGHT + 10);
                Color[] colors2b = bitmap2.GetColors(-5, -5, WIDTH + 10, HEIGHT + 10);

                #region Validate

                if (colors1b.Length != colors2b.Length)
                {
                    throw new ApplicationException("fail");
                }

                for (int cntr = 0; cntr < colors1b.Length; cntr++)
                {
                    if (colors1b[cntr] != colors2b[cntr])
                    {
                        throw new ApplicationException("fail");
                    }
                }

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnPolyIntersect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                const double MAX = 8d;

                ClearScene();

                // Random
                Point[] poly1 = new Point[] {
				    new Point(Math1D.GetNearZeroValue(MAX), Math1D.GetNearZeroValue(MAX)),
				    new Point(Math1D.GetNearZeroValue(MAX), Math1D.GetNearZeroValue(MAX)),
				    new Point(Math1D.GetNearZeroValue(MAX), Math1D.GetNearZeroValue(MAX)) };

                Rect rect = new Rect(
                    new Point(Math1D.GetNearZeroValue(MAX), Math1D.GetNearZeroValue(MAX)),
                    new Size(StaticRandom.NextDouble() * MAX, StaticRandom.NextDouble() * MAX));


                // Fixed
                //Point[] poly1 = new Point[] { new Point(-1.2, -1.2), new Point(-1.2, 0), new Point(0, -1.2) };

                //Rect rect = new Rect(new Point(-1d, -1d), new Size(2d, 2d));

                Point[] poly2 = new Point[] { rect.TopLeft, rect.TopRight, rect.BottomRight, rect.BottomLeft };


                // With convex
                //Point[] poly1 = new Point[] { new Point(5, 15), new Point(20, 5), new Point(35, 15), new Point(35, 30), new Point(25, 30), new Point(20, 25), new Point(15, 35), new Point(10, 25), new Point(10, 20) };
                //Point[] poly2 = new Point[] { new Point(10, 10), new Point(30, 10), new Point(30, 30), new Point(10, 30) };


                #region Draw poly1

                ScreenSpaceLines3D lines = new ScreenSpaceLines3D();
                lines.Color = UtilityWPF.ColorFromHex("808080");
                lines.Thickness = 1d;

                for (int cntr = 0; cntr < poly1.Length - 1; cntr++)
                {
                    lines.AddLine(poly1[cntr].ToPoint3D(), poly1[cntr + 1].ToPoint3D());
                }

                lines.AddLine(poly1[poly1.Length - 1].ToPoint3D(), poly1[0].ToPoint3D());

                _visuals.Add(lines);
                _viewport.Children.Add(lines);

                #endregion
                #region Draw poly2

                lines = new ScreenSpaceLines3D();
                lines.Color = UtilityWPF.ColorFromHex("808080");
                lines.Thickness = 1d;

                for (int cntr = 0; cntr < poly2.Length - 1; cntr++)
                {
                    lines.AddLine(poly2[cntr].ToPoint3D(), poly2[cntr + 1].ToPoint3D());
                }

                lines.AddLine(poly2[poly2.Length - 1].ToPoint3D(), poly2[0].ToPoint3D());

                _visuals.Add(lines);
                _viewport.Children.Add(lines);

                #endregion

                // doing this after in case there's an exception
                Point[] intersect = Math2D.GetIntersection_Polygon_Polygon(poly1, poly2);

                #region Draw intersect

                if (intersect != null && intersect.Length > 0)
                {
                    if (intersect.Length == 1)
                    {
                        Color color = UtilityWPF.ColorFromHex("202020");

                        // Material
                        MaterialGroup materials = new MaterialGroup();
                        materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
                        materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(color, Colors.White, .5d)), 100d));

                        // Geometry Model
                        GeometryModel3D geometry = new GeometryModel3D();
                        geometry.Material = materials;
                        geometry.BackMaterial = materials;
                        geometry.Geometry = UtilityWPF.GetSphere_LatLon(4, .05d);

                        geometry.Transform = new TranslateTransform3D(intersect[0].ToVector3D());

                        ModelVisual3D visual = new ModelVisual3D();
                        visual.Content = geometry;

                        _visuals.Add(visual);
                        _viewport.Children.Add(visual);
                    }
                    else
                    {
                        lines = new ScreenSpaceLines3D();
                        lines.Color = UtilityWPF.ColorFromHex("202020");
                        lines.Thickness = 3d;

                        for (int cntr = 0; cntr < intersect.Length - 1; cntr++)
                        {
                            lines.AddLine(intersect[cntr].ToPoint3D(), intersect[cntr + 1].ToPoint3D());
                        }

                        lines.AddLine(intersect[intersect.Length - 1].ToPoint3D(), intersect[0].ToPoint3D());

                        _visuals.Add(lines);
                        _viewport.Children.Add(lines);
                    }
                }

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnPolyArea_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Point[] poly = new Point[] { new Point(-.5, -.5), new Point(.5, -.5), new Point(.5, .5), new Point(-.5, .5) };
                double area = Math2D.GetAreaPolygon(poly);

                poly = new Point[] { new Point(-1, -1), new Point(1, -1), new Point(1, 1), new Point(-1, 1) };
                area = Math2D.GetAreaPolygon(poly);



            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnAvgColor_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                Color[] colors = new Color[] { Color.FromArgb(255, 255, 255, 255), Color.FromArgb(0, 0, 0, 0), Color.FromArgb(0, 0, 0, 0) };

                Color avg = UtilityWPF.AverageColors(colors);


            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnEmulateCones_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // http://en.wikipedia.org/wiki/Color_vision
                // http://en.wikipedia.org/wiki/File:Cone-fundamentals-with-srgb-spectrum.svg

                // http://en.wikipedia.org/wiki/Gaussian_function

                // http://dot-color.com/2012/08/14/color-space-confusion/


                // Look at CIELAB 1931, 1976
                // The perimiter of that gamut curve is the actual frequency.  All the colors in the middle are the result of mixing pure colors.  The line along the bottom
                // is the result of mixing the extreme ends of the frequency spectrum (red and blue)
                //
                // So only the colors along the permiter are real, all others are in our minds
                //
                // When given a mixed color, there isn't a single exact mixture of frequencies: you could have small amounts of lots of frequencies with just a few pronounced,
                // or just two
                //
                // So I think the CIELAB approach could be made to work, but will be glitchy, and require a lot of processing





                //---------------------------------- CameraFixedRGB
                // The simplest approach is just use the RGB, and not let the cone sensitivity ever change - maybe make a simplified camera that is locked into this mode (it will
                // be fastest, always be able to recreate the colors perfectly, and for the most part force enough brain complexity)

                //---------------------------------- CameraFixedGray


                //---------------------------------- CameraVariableCones
                // But if I want to allow for shifting frequency cones (like a cone that's focused on pink - even though pink isn't a real frequency), then I think just convert the
                // RGB to HSV, and treat hue like it's a pure frequency.  This doesn't mirror reality perfectly, but should be a good approximation.
                //
                // Since there is only one hue at a time, there won't ever be a mixture of frequencies, but cones focused on different hues will combine to interpolate the actual
                // color.
                //
                // When figuring out how much a cone should fire:
                //		Hue:  Run a bell curve function over the actual hue, and the cone will fire according to the value of the curve at that cone's hue.
                //		Saturation:  A lower saturation will widen the curve (emulating a mixture of lights).  A saturation of zero will basically be a flat line, a saturation of one
                //							will be a sharper spike.
                //		Value:  This is an intensity multiplier, so increases the overall height of the bell curve

                //TODO: When making this CameraVariableCones, prove that 3 cones placed at the appropriate hues (R, G, B) will closely match the output of the CameraFixedRGB


                //NOTE: People's cones aren't evenly spaced, plus the rods can act kind of like a fourth cone (rods are better in low light, and don't have as much impact in bright
                // light).  So this creates a bias to green.
                //
                // That is where this CameraVariableCones could possibly excel, maybe a ship wants to specialize on gold or pink, etc.  So the cones would be more focued on those
                // hues




            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnEvenDistrCircle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                const double MAXRADIUS = 8d;

                int count;
                if (!int.TryParse(txtNumPointsCircle.Text, out count))
                {
                    MessageBox.Show("Couldn't parse the number of points as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Vector3D[] before = null;
                if (_evenDistPoints != null && _evenDistPoints.Length == count)
                {
                    before = _evenDistPoints;
                }
                else
                {
                    before = Enumerable.Range(0, count).Select(o => Math3D.GetRandomVector_Circular(MAXRADIUS)).ToArray();
                    //before = Enumerable.Range(0, count).Select(o => Math3D.GetRandomVectorSpherical(MAXRADIUS)).ToArray();
                }

                //Vector[] after2D = Math3D.GetRandomVectorsCircularEvenDist(before.Select(o => new Vector(o.X, o.Y)).ToArray(), null, MAXRADIUS, .03d, 1000, null, null);
                Vector[] after2D = Math3D.GetRandomVectors_Circular_EvenDist(before.Select(o => new Vector(o.X, o.Y)).ToArray(), MAXRADIUS, .001d, 1, null, null, null);
                Vector3D[] after = after2D.Select(o => new Vector3D(o.X, o.Y, 0)).ToArray();

                //Vector3D[] after = Math3D.GetRandomVectorsSphericalEvenDist(before, null, MAXRADIUS, .001d, 25, null, null);

                _evenDistPoints = after;

                double maxRadius = after.Max(o => o.Length);

                ClearScene();

                if (chkControlDots.IsChecked.Value)
                {
                    DrawEvenDistPoints(before, after, MAXRADIUS);
                }

                ShowDelaunay(after, chkDelaunay.IsChecked.Value, chkCenters.IsChecked.Value, chkReconstructTriangles.IsChecked.Value);

                if (chkVoronoi.IsChecked.Value)
                {
                    ShowVoronoi(after);
                }

                if (chkPlate.IsChecked.Value)
                {
                    ShowPlate(MAXRADIUS);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnCenterPackedCircle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                const double MAXRADIUS = 10d;

                int count;
                if (!int.TryParse(txtNumPointsCircle.Text, out count))
                {
                    MessageBox.Show("Couldn't parse the number of points as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Vector3D[] before = null;
                if (_evenDistPoints != null && _evenDistPoints.Length == count)
                {
                    before = _evenDistPoints;
                }
                else
                {
                    before = Enumerable.Range(0, count).Select(o => Math3D.GetRandomVector_Circular(MAXRADIUS)).ToArray();
                }

                //Vector[] after2D = Math3D.GetRandomVectorsCircularEvenDist(before.Select(o => new Vector(o.X, o.Y)).ToArray(), null, MAXRADIUS, .03d, 1000, null, null);
                Vector[] after2D = Math3D.GetRandomVectors_Circular_CenterPacked(before.Select(o => new Vector(o.X, o.Y)).ToArray(), MAXRADIUS, .001d, 1, null, null, null);
                Vector3D[] after = after2D.Select(o => new Vector3D(o.X, o.Y, 0)).ToArray();

                _evenDistPoints = after;

                double maxRadius = after.Max(o => o.Length);

                ClearScene();

                if (chkControlDots.IsChecked.Value)
                {
                    DrawEvenDistPoints(before, after, MAXRADIUS);
                }

                ShowDelaunay(after, chkDelaunay.IsChecked.Value, chkCenters.IsChecked.Value, chkReconstructTriangles.IsChecked.Value);

                if (chkVoronoi.IsChecked.Value)
                {
                    ShowVoronoi(after);
                }

                if (chkPlate.IsChecked.Value)
                {
                    ShowPlate(MAXRADIUS);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnTestTileOverlap_Click(object sender, RoutedEventArgs e)
        {
            const double RECTZ = 0d;
            const double INTERSECTZ = -8d;

            try
            {
                if (_evenDistPoints == null)
                {
                    MessageBox.Show("Create some points first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Match match = Regex.Match(txtPixels.Text, @"^\s*(?<x>\d+)\s*,\s*(?<y>\d+)\s*$");
                if (!match.Success)
                {
                    MessageBox.Show("Pixels are in the wrong format (\\d+, \\d+)", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int pixelsX = int.Parse(match.Groups["x"].Value);
                int pixelsY = int.Parse(match.Groups["y"].Value);

                Point[] points2D = _evenDistPoints.Select(o => new Point(o.X, o.Y)).ToArray();

                #region Figure out the extremes

                Point min = new Point(points2D.Min(o => o.X), points2D.Min(o => o.Y));
                Point max = new Point(points2D.Max(o => o.X), points2D.Max(o => o.Y));

                double width = max.X - min.X;
                double height = max.Y - min.Y;

                // Enlarge a bit
                min.X -= width * .05d;
                min.Y -= height * .05d;
                max.X += width * .05d;
                max.Y += height * .05d;

                width = max.X - min.X;
                height = max.Y - min.Y;

                //Vector3D offset = new Vector3D(width * .5d, height * .5d, 0d);        //  can't assume half
                Vector3D offset = new Vector3D(-min.X, -min.Y, 0d);

                #endregion

                // Make the 3D points shifted over so that min is at 0,0
                Point3D[] points3D = points2D.Select(o => new Point3D(o.X + offset.X, o.Y + offset.Y, 0d)).ToArray();

                TriangleIndexed[] triangles = Math2D.GetDelaunayTriangulation(points2D, points3D);

                Rect[] pixelRects;
                var overlay = TriangleTileOverlay.GetIntersections(out pixelRects, new Size(width, height), pixelsX, pixelsY, triangles);

                double minPercent = overlay.SelectMany(o => o).Min(o => o.Percent);
                double maxPercent = overlay.SelectMany(o => o).Max(o => o.Percent);

                #region Tiles (lines)

                List<Tuple<Point3D, Point3D>> pairs = new List<Tuple<Point3D, Point3D>>();
                foreach (Rect pixel in pixelRects)
                {
                    pairs.Add(Tuple.Create(pixel.TopLeft.ToPoint3D(RECTZ), pixel.TopRight.ToPoint3D(RECTZ)));
                    pairs.Add(Tuple.Create(pixel.TopRight.ToPoint3D(RECTZ), pixel.BottomRight.ToPoint3D(RECTZ)));
                    pairs.Add(Tuple.Create(pixel.BottomRight.ToPoint3D(RECTZ), pixel.BottomLeft.ToPoint3D(RECTZ)));
                    pairs.Add(Tuple.Create(pixel.BottomLeft.ToPoint3D(RECTZ), pixel.TopLeft.ToPoint3D(RECTZ)));
                }

                ScreenSpaceLines3D lines = new ScreenSpaceLines3D();
                lines.Color = UtilityWPF.ColorFromHex("505050");
                lines.Thickness = 1d;

                foreach (var line in GetUniqueLines(pairs))
                {
                    lines.AddLine(line.Item1 - offset, line.Item2 - offset);
                }

                _viewport.Children.Add(lines);
                _visuals.Add(lines);

                #endregion

                if (chkTestLines.IsChecked.Value)
                {
                    #region Intersections (lines)

                    pairs.Clear();

                    foreach (Point[] poly in overlay.SelectMany(o => o).Select(o => o.Intersection))
                    {
                        for (int cntr = 0; cntr < poly.Length - 1; cntr++)
                        {
                            pairs.Add(Tuple.Create(poly[cntr].ToPoint3D(INTERSECTZ), poly[cntr + 1].ToPoint3D(INTERSECTZ)));
                        }

                        pairs.Add(Tuple.Create(poly[poly.Length - 1].ToPoint3D(INTERSECTZ), poly[0].ToPoint3D(INTERSECTZ)));
                    }

                    lines = new ScreenSpaceLines3D();
                    lines.Color = UtilityWPF.ColorFromHex("0000FF");
                    lines.Thickness = 1d;

                    foreach (var line in GetUniqueLines(pairs))
                    {
                        lines.AddLine(line.Item1 - offset, line.Item2 - offset);
                    }

                    _viewport.Children.Add(lines);
                    _visuals.Add(lines);

                    #endregion
                }
                else
                {
                    Model3DGroup geometries = new Model3DGroup();
                    Color[] triangleColors = triangles.Select(o => UtilityWPF.GetRandomColor(100, 192)).ToArray();

                    #region Intersections (plates)

                    for (int triCntr = 0; triCntr < triangles.Length; triCntr++)
                    {
                        foreach (var poly in overlay[triCntr])
                        {
                            int offsetColor = 7;
                            Color color = UtilityWPF.GetRandomColor(192,
                                GetOffsetCapped(triangleColors[triCntr].R, -offsetColor), GetOffsetCapped(triangleColors[triCntr].R, offsetColor),
                                GetOffsetCapped(triangleColors[triCntr].G, -offsetColor), GetOffsetCapped(triangleColors[triCntr].G, offsetColor),
                                GetOffsetCapped(triangleColors[triCntr].B, -offsetColor), GetOffsetCapped(triangleColors[triCntr].B, offsetColor));

                            // Material
                            MaterialGroup materials = new MaterialGroup();
                            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
                            materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(color, Colors.White, .5d)), 100d));

                            // Geometry Model
                            GeometryModel3D geometry = new GeometryModel3D();
                            geometry.Material = materials;
                            geometry.BackMaterial = materials;

                            geometry.Geometry = UtilityWPF.GetMeshFromTriangles(Math2D.GetTrianglesFromConvexPoly(poly.Intersection, INTERSECTZ));
                            geometry.Transform = new TranslateTransform3D(-offset);

                            geometries.Children.Add(geometry);
                        }
                    }

                    #endregion
                    #region Triangles (plates)

                    for (int cntr = 0; cntr < triangles.Length; cntr++)
                    {
                        Color color = Color.FromArgb(96, triangleColors[cntr].R, triangleColors[cntr].G, triangleColors[cntr].B);

                        // Material
                        MaterialGroup materials = new MaterialGroup();
                        materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
                        materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(color, Colors.White, .5d)), 100d));

                        // Geometry Model
                        GeometryModel3D geometry = new GeometryModel3D();
                        geometry.Material = materials;
                        geometry.BackMaterial = materials;
                        geometry.Geometry = UtilityWPF.GetMeshFromTriangles(new TriangleIndexed[] { triangles[cntr] });
                        geometry.Transform = new TranslateTransform3D(-offset);

                        geometries.Children.Add(geometry);
                    }

                    #endregion

                    ModelVisual3D visual = new ModelVisual3D();
                    visual.Content = geometries;

                    _visuals.Add(visual);
                    _viewport.Children.Add(visual);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private void ClearScene()
        {
            // Standard
            foreach (Visual3D visual in _visuals)
            {
                _viewport.Children.Remove(visual);
            }
            _visuals.Clear();

            // Offline 1
            if (_offline1 != null)
            {
                foreach (Visual3D visual in _offline1.Visuals)
                {
                    _offline1.Viewport.Children.Remove(visual);
                }
                _offline1.Visuals.Clear();
            }

            //  Camera Pool
            if (_cameraPool != null)
            {
                foreach (CameraPoolVisual visual in _cameraPoolVisuals)
                {
                    _cameraPool.Remove(visual);
                }
                _cameraPoolVisuals.Clear();
            }
        }

        private void AddVisual(Model3D model)
        {
            #region Standard

            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = model;

            _visuals.Add(visual);
            _viewport.Children.Add(visual);

            #endregion

            #region Offline 1

            if (_offline1 == null)
            {
                Brush background = null;
                if (radSceneBlack.IsChecked.Value)
                {
                    background = Brushes.Black;
                }
                else if (radSceneTransparent.IsChecked.Value)
                {
                    background = Brushes.Transparent;
                }
                else
                {
                    throw new ApplicationException("Unknown background color");
                }

                _offline1 = new ViewportOffline(background);

                radSceneBlack.IsEnabled = false;        //  crude, but I don't want to listen to events
                radSceneTransparent.IsEnabled = false;
            }

            visual = new ModelVisual3D();
            visual.Content = model;

            _offline1.Visuals.Add(visual);
            _offline1.Viewport.Children.Add(visual);

            #endregion

            #region Camera Pool

            //  Create pool
            if (_cameraPool == null)
            {
                _cameraPool = new CameraPool(1, radSceneBlack.IsChecked.Value ? Colors.Black : Colors.Transparent);
            }

            //  Serialize model
            byte[] modelBytes = null;

            using (MemoryStream stream = new MemoryStream())
            {
                XamlServices.Save(stream, model);
                stream.Position = 0;

                modelBytes = stream.ToArray();
            }

            CameraPoolVisual poolVisual = new CameraPoolVisual(TokenGenerator.NextToken(), modelBytes, null);

            //  Add it
            _cameraPoolVisuals.Add(poolVisual);
            _cameraPool.Add(poolVisual);

            #endregion
        }

        private void DrawEvenDistPoints(Vector3D[] before, Vector3D[] after, double radius)
        {
            bool drawBefore = before.Length < 75;

            #region Before

            Color color = Colors.Silver;

            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
            materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(color, Colors.White, .5d)), 100d));

            Model3DGroup geometries = new Model3DGroup();

            if (drawBefore)
            {
                for (int cntr = 0; cntr < before.Length; cntr++)
                {
                    // Geometry Model
                    GeometryModel3D geometry = new GeometryModel3D();
                    geometry.Material = materials;
                    geometry.BackMaterial = materials;
                    geometry.Geometry = UtilityWPF.GetSphere_LatLon(2, .02d);

                    geometry.Transform = new TranslateTransform3D(before[cntr]);

                    geometries.Children.Add(geometry);
                }

                // Add it
                AddVisual(geometries);
            }

            #endregion
            #region After

            color = Colors.Black;

            // Material
            materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
            materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(color, Colors.White, .5d)), 100d));

            geometries = new Model3DGroup();

            for (int cntr = 0; cntr < before.Length; cntr++)
            {
                // Geometry Model
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = materials;
                geometry.BackMaterial = materials;
                geometry.Geometry = UtilityWPF.GetSphere_LatLon(2, .03d);

                geometry.Transform = new TranslateTransform3D(after[cntr]);

                geometries.Children.Add(geometry);
            }

            // Add it
            AddVisual(geometries);

            #endregion
        }

        private void ShowDelaunay(Vector3D[] points, bool showLines, bool showCenters, bool reconstructTriangles)
        {
            if (!showLines && !showCenters && !reconstructTriangles)
            {
                return;
            }

            Point3D[] points3D = points.Select(o => o.ToPoint()).ToArray();
            Point[] points2D = points3D.Select(o => new Point(o.X, o.Y)).ToArray();

            TriangleIndexed[] triangles = Math2D.GetDelaunayTriangulation(points2D, points3D);

            if (showLines)
            {
                #region Lines

                ScreenSpaceLines3D lines = new ScreenSpaceLines3D();
                lines.Color = UtilityWPF.ColorFromHex("709070");
                lines.Thickness = 1d;

                foreach (var line in TriangleIndexed.GetUniqueLines(triangles))
                {
                    lines.AddLine(points3D[line.Item1], points3D[line.Item2]);
                }

                _visuals.Add(lines);
                _viewport.Children.Add(lines);

                #endregion
            }

            if (showCenters)
            {
                #region Centers

                Color color = Colors.Red;

                // Material
                MaterialGroup materials = new MaterialGroup();
                materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
                materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(color, Colors.White, .5d)), 100d));

                Model3DGroup geometries = new Model3DGroup();

                for (int cntr = 0; cntr < triangles.Length; cntr++)
                {
                    // Geometry Model
                    GeometryModel3D geometry = new GeometryModel3D();
                    geometry.Material = materials;
                    geometry.BackMaterial = materials;
                    geometry.Geometry = UtilityWPF.GetSphere_LatLon(2, .04d);

                    geometry.Transform = new TranslateTransform3D(triangles[cntr].GetCenterPoint().ToVector());

                    geometries.Children.Add(geometry);
                }

                // Add it
                AddVisual(geometries);

                #endregion
            }

            if (reconstructTriangles)
            {
                ShowReconstructTriangles(triangles.Select(o => o.GetCenterPoint()).ToArray());
            }
        }

        private void ShowReconstructTriangles(Point3D[] points)
        {
            //NOTE: This thinking is flawed (trying to reconstruct the triangle definitions based on the centers of the triangles) - I need this when instantiating a
            //camera from dna, the original control points are lost (vertices of the triangles)
            //
            //Instead, I'll go with Voronoi around the original even dist points, which is a much cleaner design all around

            Point3D[] points3D = points;//.Select(o => o.ToPoint()).ToArray();
            Point[] points2D = points3D.Select(o => new Point(o.X, o.Y)).ToArray();

            TriangleIndexed[] triangles = Math2D.GetDelaunayTriangulation(points2D, points3D);

            var quickHull = Math2D.GetConvexHull(points);
            bool[] isPerimiter = Enumerable.Range(0, points.Length).Select(o => quickHull.PerimiterLines.Contains(o)).ToArray();

            var remaining = TriangleIndexed.GetUniqueLines(triangles).ToList();

            #region First Pass

            List<Tuple<int, int>> first = new List<Tuple<int, int>>();

            for (int cntr = 0; cntr < points.Length; cntr++)
            {
                //  Get the lines connected to this point
                var connected = remaining.Where(o => o.Item1 == cntr || o.Item2 == cntr).
                    OrderBy(o => (points[o.Item2] - points[o.Item1]).LengthSquared).
                    ToArray();

                if (connected.Length == 0)
                {
                    continue;
                }

                //  Of these, choose the shortest
                first.Add(connected[0]);
            }


            first = first.Distinct().ToList();
            foreach (var shrt in first)
            {
                remaining.Remove(shrt);
            }

            #endregion
            #region Second Pass

            List<Tuple<int, int>> second = new List<Tuple<int, int>>();

            for (int cntr = 0; cntr < points.Length; cntr++)
            {
                int numConnected = first.Count(o => o.Item1 == cntr || o.Item2 == cntr);

                if (numConnected > 3)
                {
                    int seven = 8;
                }

                if (numConnected == 3)
                {
                    continue;
                }

                if (isPerimiter[cntr] && numConnected == 2)
                {
                    continue;
                }

                //  This still needs connections
                var connected = remaining.Where(o => o.Item1 == cntr || o.Item2 == cntr).
                    OrderBy(o => (points[o.Item2] - points[o.Item1]).LengthSquared).
                    ToArray();

                if (connected.Length == 0)
                {
                    continue;
                }

                //  Of these, choose the shortest
                second.Add(connected[0]);
            }


            second = second.Distinct().ToList();
            foreach (var shrt in second)
            {
                remaining.Remove(shrt);
            }

            #endregion
            #region Third Pass

            List<Tuple<int, int>> third = new List<Tuple<int, int>>();

            for (int cntr = 0; cntr < points.Length; cntr++)
            {
                int numConnected = first.Count(o => o.Item1 == cntr || o.Item2 == cntr);
                numConnected += second.Count(o => o.Item1 == cntr || o.Item2 == cntr);

                if (numConnected > 3)
                {
                    int seven = 8;
                }

                if (numConnected == 3)
                {
                    continue;
                }

                if (isPerimiter[cntr] && numConnected == 2)
                {
                    continue;
                }

                //  This still needs connections
                var connected = remaining.Where(o => o.Item1 == cntr || o.Item2 == cntr).
                    OrderBy(o => (points[o.Item2] - points[o.Item1]).LengthSquared).
                    ToArray();

                if (connected.Length == 0)
                {
                    continue;
                }

                //  Of these, choose the shortest
                third.Add(connected[0]);
            }


            third = third.Distinct().ToList();
            foreach (var shrt in third)
            {
                remaining.Remove(shrt);
            }

            #endregion


            #region First Lines

            ScreenSpaceLines3D lines = new ScreenSpaceLines3D();
            lines.Color = UtilityWPF.ColorFromHex("50F050");
            lines.Thickness = 5d;

            foreach (var line in first)
            {
                lines.AddLine(points3D[line.Item1], points3D[line.Item2]);
            }

            _visuals.Add(lines);
            _viewport.Children.Add(lines);

            #endregion
            #region Second Lines

            lines = new ScreenSpaceLines3D();
            lines.Color = UtilityWPF.ColorFromHex("70E070");
            lines.Thickness = 2d;

            foreach (var line in second)
            {
                lines.AddLine(points3D[line.Item1], points3D[line.Item2]);
            }

            _visuals.Add(lines);
            _viewport.Children.Add(lines);

            #endregion
            #region Third Lines

            lines = new ScreenSpaceLines3D();
            lines.Color = UtilityWPF.ColorFromHex("90D090");
            lines.Thickness = 1d;

            foreach (var line in third)
            {
                lines.AddLine(points3D[line.Item1], points3D[line.Item2]);
            }

            _visuals.Add(lines);
            _viewport.Children.Add(lines);

            #endregion
            #region Longest Lines

            lines = new ScreenSpaceLines3D();
            lines.Color = UtilityWPF.ColorFromHex("D09090");
            lines.Thickness = 1d;

            foreach (var line in remaining)
            {
                lines.AddLine(points3D[line.Item1], points3D[line.Item2]);
            }

            _visuals.Add(lines);
            _viewport.Children.Add(lines);

            #endregion
        }

        private void ShowVoronoi(Vector3D[] after)
        {
            Point[] points2D = after.Select(o => new Point(o.X, o.Y)).ToArray();

            var result = Math2D.GetVoronoi(points2D, true);

            if (chkCapVoronoi.IsChecked.Value)
            {
                //result = CapVoronoi.CapCircle(result);
                result = Math2D.CapVoronoiCircle(result);
            }

            #region Edges

            ScreenSpaceLines3D lines = new ScreenSpaceLines3D();
            lines.Color = UtilityWPF.ColorFromHex("808080");
            lines.Thickness = 1d;

            foreach (var edge in result.Edges)
            {
                switch (edge.EdgeType)
                {
                    case EdgeType.Segment:
                        lines.AddLine(edge.Point0.ToPoint3D(), edge.Point1.Value.ToPoint3D());
                        break;

                    case EdgeType.Ray:
                        lines.AddLine(edge.Point0.ToPoint3D(), edge.Point0.ToPoint3D() + (edge.Direction.Value.ToVector3D().ToUnit() * 100));
                        break;

                    case EdgeType.Line:
                        Point3D point3D = edge.Point0.ToPoint3D();
                        Vector3D dir3D = edge.Direction.Value.ToVector3D().ToUnit() * 100;
                        lines.AddLine(point3D - dir3D, point3D + dir3D);
                        break;

                    default:
                        throw new ApplicationException("Unknown EdgeType: " + edge.EdgeType.ToString());
                }

            }

            _visuals.Add(lines);
            _viewport.Children.Add(lines);

            #endregion
            #region Polygons




            #endregion
        }

        private void ShowPlate(double radius)
        {
            Color color = UtilityWPF.ColorFromHex("04000000");

            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
            materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(color, Colors.White, .5d)), 100d));

            Transform3DGroup transform = new Transform3DGroup();
            transform.Children.Add(new ScaleTransform3D(radius, radius, radius));
            transform.Children.Add(new TranslateTransform3D(0, 0, -.01));

            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;
            geometry.Geometry = UtilityWPF.GetCircle2D(20, transform, Transform3D.Identity);

            AddVisual(geometry);
        }

        private static Tuple<Point3D, Point3D>[] GetUniqueLines(IEnumerable<Tuple<Point3D, Point3D>> allLines)
        {
            List<Tuple<Point3D, Point3D>> intermediate = new List<Tuple<Point3D, Point3D>>();

            //  Put the "smaller" point as item1
            foreach (var line in allLines)
            {
                if (line.Item1.X < line.Item2.X)
                {
                    intermediate.Add(line);
                }
                else if (line.Item1.X == line.Item2.X)
                {
                    if (line.Item1.Y < line.Item2.Y)
                    {
                        intermediate.Add(line);
                    }
                    else
                    {
                        intermediate.Add(Tuple.Create(line.Item2, line.Item1));
                    }
                }
                else
                {
                    intermediate.Add(Tuple.Create(line.Item2, line.Item1));
                }
            }

            return intermediate.Distinct().ToArray();
        }

        private static byte GetOffsetCapped(byte value, int offset)
        {
            int retVal = value + offset;

            if (retVal < 0)
            {
                return 0;
            }
            else if (retVal > 255)
            {
                return 255;
            }
            else
            {
                return Convert.ToByte(retVal);
            }
        }

        /// <summary>
        /// This is the bell curve equation
        /// </summary>
        /// <remarks>
        /// http://en.wikipedia.org/wiki/Gaussian_function
        /// </remarks>
        private static double GetBellCurve(double x, double height, double xOffset, double standardDeviation)
        {
            double numerator = x - xOffset;
            double denominator = 2d * standardDeviation;

            return Math.Pow(
                height * Math.E,
                -((numerator * numerator) / (denominator * denominator))
                );
        }

        private static void ThreadTest1(Model3D model)
        {
            //  Store it in a new viewport
            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = model;

            ViewportOffline viewport = new ViewportOffline(Brushes.Black);

            viewport.Visuals.Add(visual);
            viewport.Viewport.Children.Add(visual);

            //  Take a picture
            IBitmapCustom bitmap = UtilityWPF.RenderControl(viewport.Control, 100, 100, true, Colors.Black, false);
        }
        private static void ThreadTest2(byte[] geometryBytes)
        {
            Model3D model;
            using (MemoryStream stream = new MemoryStream(geometryBytes))
            {
                model = XamlServices.Load(stream) as Model3D;
            }

            //  Store it in a new viewport
            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = model;

            ViewportOffline viewport = new ViewportOffline(Brushes.Black);

            viewport.Visuals.Add(visual);
            viewport.Viewport.Children.Add(visual);

            //  Take a picture
            IBitmapCustom bitmap = UtilityWPF.RenderControl(viewport.Control, 100, 100, true, Colors.Black, false);
        }

        #endregion
    }
}
