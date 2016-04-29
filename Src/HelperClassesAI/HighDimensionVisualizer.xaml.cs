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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;

namespace Game.HelperClassesAI
{
    public partial class HighDimensionVisualizer : UserControl
    {
        #region Class: Dot

        private class Dot
        {
            public readonly long Token = TokenGenerator.NextToken();

            public bool IsStatic { get; set; }

            public ISOMInput Input { get; set; }

            public Point3D Position { get; set; }

            public Color Color { get; set; }

            public GeometryModel3D Geometry_3DDot { get; set; }
            public TranslateTransform3D Translate_3DDot { get; set; }

            public Border Preview { get; set; }
            public VectorInt PreviewSize { get; set; }
        }

        #endregion

        #region Declaration Section

        public const double RADIUS = .5;
        private const double DOTRADIUS = .01;

        private const string TITLE = "High Dimension Visualizer";

        //TODO: This doens't need to be VectorInt.  Make it Vector
        private readonly Func<ISOMInput, Tuple<UIElement, VectorInt>> _getPreview;
        private readonly Func<ISOMInput, Color> _getDotColor;

        private readonly bool _showStaticDots;

        /// <summary>
        /// This listens to the mouse/keyboard and controls the camera
        /// </summary>
        private readonly TrackBallRoam _trackball;

        private readonly DispatcherTimer _timer;

        private readonly Visual3D _visual;
        private readonly Model3DGroup _modelGroup;

        private readonly List<Dot> _dots = new List<Dot>();
        private readonly List<Dot> _staticDots = new List<Dot>();

        private Dot _mouseOver = null;

        private readonly List<Tuple<Dot, Dot, double>> _distances = new List<Tuple<Dot, Dot, double>>();

        private readonly SpecularMaterial _specular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("50FFFFFF")), 2);
        private readonly MeshGeometry3D _dotGeometry = UtilityWPF.GetSphere_Ico(DOTRADIUS, 1, true);
        private readonly SortedList<string, MaterialGroup> _dotMaterials = new SortedList<string, MaterialGroup>();

        private bool _initialized = false;

        #endregion

        #region Constructor

        public HighDimensionVisualizer(Func<ISOMInput, Tuple<UIElement, VectorInt>> getPreview = null, Func<ISOMInput, Color> getDotColor = null, bool showStaticDots = false, bool is3D = false)
        {
            InitializeComponent();

            _showStaticDots = showStaticDots;

            _getPreview = getPreview;
            _getDotColor = getDotColor;

            chk3D.IsChecked = is3D;

            // Camera Trackball
            _trackball = new TrackBallRoam(_camera);
            _trackball.EventSource = grdViewPort;		//NOTE:  If this control doesn't have a background color set, the trackball won't see events (I think transparent is ok, just not null)
            _trackball.AllowZoomOnMouseWheel = true;
            _trackball.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.MouseComplete_NoLeft));
            _trackball.MouseWheelScale *= .1;
            //_trackball.GetOrbitRadius += new GetOrbitRadiusHandler(Trackball_GetOrbitRadius);

            // Timer
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(25);
            _timer.Tick += Timer_Tick;
            _timer.Start();

            // Visual
            _modelGroup = new Model3DGroup();
            _visual = new ModelVisual3D()
            {
                Content = _modelGroup,
            };

            _initialized = true;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// This is used to help make the movable items stay aligned to a global scene
        /// </summary>
        /// <remarks>
        /// If you save a SOM result view, and this control shows the contents of a single node, then these
        /// static points would be the surrounding nodes.  This way, individual items in this control will
        /// align with their neighbors
        /// </remarks>
        public void AddStaticItems(IEnumerable<Tuple<ISOMInput, Point3D>> items)
        {
            MeshGeometry3D bigGeometry = null, smallGeometry = null;
            if (_showStaticDots)
            {
                bigGeometry = UtilityWPF.GetSphere_Ico(DOTRADIUS * 2, 1, true);
                //smallGeometry = UtilityWPF.GetSphere_Ico(DOTRADIUS * .3, 1, true);
            }

            foreach (var item in items)
            {
                Dot dot = new Dot()
                {
                    IsStatic = true,
                    Input = item.Item1,
                    Position = item.Item2,

                    Color = GetDotColor(item.Item1),
                    Translate_3DDot = new TranslateTransform3D(item.Item2.ToVector()),
                };

                if (_showStaticDots)
                {
                    dot.Geometry_3DDot = BuildDot_Visual(_modelGroup, bigGeometry, _dotMaterials, _specular, dot.Color, dot.Translate_3DDot);
                }

                _staticDots.Add(dot);

                _distances.AddRange(BuildDot_Distances(dot, _dots));
            }

            //if (_showStaticDots)
            //{
            //    Point3D center = Math3D.GetCenter(items.Select(o => o.Item2));
            //    BuildDot_Visual(_modelGroup, smallGeometry, _dotMaterials, _specular, Colors.White, new TranslateTransform3D(center.ToVector()));
            //}
        }

        public void AddItems(IEnumerable<ISOMInput> items)
        {
            foreach (ISOMInput item in items)
            {
                Point3D position = Math3D.GetRandomVector_Spherical(RADIUS).ToPoint();

                Dot dot = new Dot()
                {
                    IsStatic = false,
                    Input = item,
                    Color = GetDotColor(item),
                    Position = position,
                    Translate_3DDot = new TranslateTransform3D(position.ToVector()),
                };

                dot.Geometry_3DDot = BuildDot_Visual(_modelGroup, _dotGeometry, _dotMaterials, _specular, dot.Color, dot.Translate_3DDot);

                _distances.AddRange(BuildDot_Distances(dot, _dots.Concat(_staticDots)));

                _dots.Add(dot);
            }

            //TODO: Do this in a separate thread.  Don't store the dots and distances in global variables until after this finishes (that's a lot of work though for very little gain)
            MoveDots(_dots, _distances, chk3D.IsChecked.Value, 1500, RADIUS / 10000);

            foreach (Dot dot in _dots)
            {
                dot.Translate_3DDot.OffsetX = dot.Position.X;
                dot.Translate_3DDot.OffsetY = dot.Position.Y;
                dot.Translate_3DDot.OffsetZ = dot.Position.Z;
            }
        }

        public void Clear()
        {
            MessageBox.Show("finish this");
        }

        /// <summary>
        /// This is a helper method to turn som node positions into points to pass to AddStaticItems()
        /// </summary>
        public static IEnumerable<Tuple<ISOMInput, Point3D>> GetSOMNodeStaticPositions(SOMNode node, SOMNode[] allNodes, double innerRadius)
        {
            // Convert to Point3D
            var initial = allNodes.
                Select(o =>
                {
                    Vector3D position = new Vector3D();
                    if (o.Position.Length >= 1) position.X = o.Position[0];
                    if (o.Position.Length >= 2) position.Y = o.Position[1];
                    if (o.Position.Length >= 3) position.Z = o.Position[2];

                    return Tuple.Create(o, position, position.Length);
                }).
                OrderBy(o => o.Item3).
                ToArray();

            // Find the closest one (that isn't the one passed in)
            var firstNonZero = initial.
                Where(o => o.Item1.Token != node.Token).
                FirstOrDefault(o => !o.Item3.IsNearZero());

            double scale = 1d;
            if (firstNonZero != null)
            {
                scale = (innerRadius * 1.25) / firstNonZero.Item3;
            }

            // Make sure they are spaced properly
            var scaled = initial.
                Select(o => Tuple.Create(o.Item1, (o.Item2 * scale).ToPoint())).
                ToArray();

            // These need to be centered over the origin, because the points will try to drift to the center
            Point3D center = Math3D.GetCenter(scaled.Select(o => o.Item2));
            Vector3D offset = new Vector3D(-center.X, -center.Y, -center.Z);

            return scaled.
                Select(o => Tuple.Create((ISOMInput)o.Item1, o.Item2 + offset)).
                ToArray();
        }

        #endregion

        #region Event Listeners

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewport.Children.Add(_visual);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void grdViewPort_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (_getPreview == null)
                {
                    return;
                }

                var hitResult = GetMouseOver(e);
                Dot dot = hitResult.Item1;

                //NOTE: This mousemove event will fire again if the region the mouse is over gets invalidated, so this if statement stops that
                if (dot != null && _mouseOver != null && dot.Token == _mouseOver.Token)
                {
                    return;
                }

                _mouseOver = dot;

                canvas.Children.Clear();

                if (dot == null)
                {
                    return;
                }

                // Find dots near this one
                var nearby = GetNearbyDots(dot, _dots, RADIUS / 3d);

                Point center = new Point(canvas.ActualWidth / 2, canvas.ActualHeight / 2);

                // Put the mouse over image in the center
                ShowPreview(dot, center);

                // Lay out the other ones out
                if (nearby.Length > 0)
                {
                    ShowNearbyPreviews(nearby, dot, hitResult.Item2, center);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                MoveDots(_dots, _distances, chk3D.IsChecked.Value);

                foreach (Dot dot in _dots)
                {
                    dot.Translate_3DDot.OffsetX = dot.Position.X;
                    dot.Translate_3DDot.OffsetY = dot.Position.Y;
                    dot.Translate_3DDot.OffsetZ = dot.Position.Z;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void chk3D_Checked(object sender, RoutedEventArgs e)
        {
            const double ZKICK = RADIUS / 100;

            try
            {
                if (!_initialized)
                {
                    return;
                }

                Random rand = StaticRandom.GetRandomForThread();

                // When it's going from 2D to 3D, all the Zs will be zero.  So give them a slight push into 3D
                foreach (Dot dot in _dots)
                {
                    if (dot.Position.Z.IsNearZero())
                    {
                        dot.Position = new Point3D(dot.Position.X, dot.Position.Y, dot.Position.Z + rand.NextDouble(-ZKICK, ZKICK));
                        dot.Translate_3DDot.OffsetZ = dot.Position.Z;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Method

        private Color GetDotColor(ISOMInput item)
        {
            if (_getDotColor == null)
            {
                return UtilityWPF.GetRandomColor(96, 192);
            }
            else
            {
                return _getDotColor(item);
            }
        }

        private static void MoveDots(IList<Dot> movableDots, IEnumerable<Tuple<Dot, Dot, double>> distances, bool is3D, int iterations = 1, double stopDistance = 0)
        {
            const double MULT = .005;
            const double MAXSPEED = .05;
            const double MAXSPEED_Z = MAXSPEED / 10d;

            for (int cntr = 0; cntr < iterations; cntr++)
            {
                Tuple<Dot, Vector3D>[] forces = GetForces(distances, movableDots, MULT).
                    Where(o => !o.Item1.IsStatic).
                    ToArray();

                // Cap the speed
                forces = forces.
                    Select(o => CapSpeed(o, MAXSPEED)).
                    ToArray();

                if (!is3D)
                {
                    forces = forces.
                        Select(o => Force2D(o, MAXSPEED_Z)).
                        ToArray();
                }

                double maxMovement = 0;

                foreach (var force in forces)
                {
                    maxMovement = Math.Max(maxMovement, force.Item2.LengthSquared);

                    force.Item1.Position += force.Item2;
                }

                if (maxMovement < stopDistance * stopDistance)
                {
                    break;
                }
            }
        }

        private static Tuple<Dot, Vector3D> CapSpeed(Tuple<Dot, Vector3D> force, double maxSpeed)
        {
            if (force.Item2.LengthSquared < maxSpeed * maxSpeed)
            {
                return force;
            }
            else
            {
                return Tuple.Create(force.Item1, force.Item2.ToUnit() * maxSpeed);
            }
        }
        private static Tuple<Dot, Vector3D> Force2D(Tuple<Dot, Vector3D> force, double maxSpeed)
        {
            // Figure out how quick to move toward Z=0
            double zSpeed = Math.Min(Math.Abs(force.Item1.Position.Z), maxSpeed);

            // The sign of z speed need to be opposite of the current z coordinate
            if (force.Item1.Position.Z > 0)
            {
                zSpeed = -zSpeed;
            }

            // Make a force that's always pulling toward z=0
            return Tuple.Create(force.Item1, new Vector3D(force.Item2.X, force.Item2.Y, zSpeed));
        }

        private static Tuple<Dot, Vector3D>[] GetForces(IEnumerable<Tuple<Dot, Dot, double>> distances, IEnumerable<Dot> allDots, double mult)
        {
            #region Calculate forces

            Tuple<Dot, Vector3D>[] forces = distances.
                //AsParallel().     //TODO: if distances.Length > threshold, do this in parallel
                SelectMany(o =>
                {
                    // Spring from 1 to 2
                    Vector3D spring = o.Item2.Position - o.Item1.Position;
                    double springLength = spring.Length;

                    double difference = o.Item3 - springLength;
                    difference *= mult;

                    if (Math1D.IsNearZero(springLength))
                    {
                        spring = Math3D.GetRandomVector_Spherical_Shell(Math.Abs(difference));
                    }
                    else
                    {
                        spring = spring.ToUnit() * Math.Abs(difference);
                    }

                    if (difference > 0)
                    {
                        // Gap needs to be bigger, push them away (default is closing the gap)
                        spring = -spring;
                    }

                    return new[]
                        {
                            Tuple.Create(o.Item1, spring),
                            Tuple.Create(o.Item2, -spring)
                        };
                }).
                ToArray();

            #endregion

            // Give them a very slight pull toward the origin so that the cloud doesn't drift away
            Point3D center = Math3D.GetCenter(allDots.Select(o => o.Position));
            double centerMult = mult * -5;

            Vector3D centerPullForce = center.ToVector() * centerMult;

            // Group by dot
            var grouped = forces.
                GroupBy(o => o.Item1.Token);

            return grouped.
                Select(o =>
                {
                    Vector3D sum = centerPullForce;
                    Dot dot = null;

                    foreach (var force in o)
                    {
                        dot = force.Item1;
                        sum += force.Item2;
                    }

                    return Tuple.Create(dot, sum);
                }).
                ToArray();
        }

        private Tuple<Dot, RayHitTestParameters> GetMouseOver(MouseEventArgs e)
        {
            const double MAXHITDISTANCE = .035;

            // Fire a ray from the mouse point
            Point mousePoint = e.GetPosition(grdViewPort);
            var ray = UtilityWPF.RayFromViewportPoint(_camera, _viewport, mousePoint);

            var hit = _dots.Concat(_staticDots).
                Select(o => new
                {
                    Dot = o,
                    Position = o.Position,
                    DotProduct = Vector3D.DotProduct(ray.Direction, o.Position - ray.Origin)
                }).
                Where(o => o.DotProduct > 0).      // throwing out points that are behind the camera
                Select(o => new
                {
                    o.Dot,
                    o.Position,
                    Distance = Math3D.GetClosestDistance_Line_Point(ray.Origin, ray.Direction, o.Position)
                }).
                Where(o => o.Distance <= MAXHITDISTANCE).
                OrderBy(o => (o.Position - ray.Origin).LengthSquared).
                FirstOrDefault();

            // Sooooon :)
            //hit?.Index;
            Dot dot = hit == null ? (Dot)null : hit.Dot;
            return Tuple.Create(dot, ray);
        }

        private void ShowPreview(Dot dot, Point position)
        {
            if (_getPreview == null)
            {
                return;
            }

            EnsurePreviewGenerated(dot, _getPreview);

            Canvas.SetLeft(dot.Preview, position.X - (dot.PreviewSize.X / 2d));
            Canvas.SetTop(dot.Preview, position.Y - (dot.PreviewSize.Y / 2d));
            canvas.Children.Add(dot.Preview);
        }
        private void ShowNearbyPreviews(Dot[] nearby, Dot dot, RayHitTestParameters cameraRay, Point center2D)
        {
            #region previews, sizes

            int dotMinSize = Math.Min(dot.PreviewSize.X, dot.PreviewSize.Y);
            int minSize = dotMinSize;

            foreach (Dot nearDot in nearby)
            {
                EnsurePreviewGenerated(nearDot, _getPreview);

                minSize = Math1D.Min(minSize, nearDot.PreviewSize.X, nearDot.PreviewSize.Y);
            }

            double halfMin = minSize / 2d;
            double stepDist = halfMin * .05;

            #endregion

            #region project plane

            // Get a plane that is perpendicular to the look ray
            ITriangle plane = Math3D.GetPlane(dot.Position, cameraRay.Direction);

            // Project the points onto that plane
            var nearbyOnPlane = nearby.
                Select(o => new { Dot = o, PlanePoint = Math3D.GetClosestPoint_Plane_Point(plane, o.Position) }).
                ToArray();

            RotateTransform3D rotate = new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRotation(new DoubleVector(cameraRay.Direction, _camera.UpDirection), new DoubleVector(new Vector3D(0, 0, -1), new Vector3D(0, 1, 0)))));

            #endregion

            // Lay these previews down along the directions of the projected points
            // nearby is sorted by distance from the center image

            // Don't start counter at a distance of zero, that's just wasted steps.  Figure out what cntr to use that is the distance of the two images touching
            int startIncrement = Convert.ToInt32(Math.Floor(((dotMinSize / 2d) + halfMin) / stepDist));

            // Remember the locations of each image rect
            List<Rect> existing = new List<Rect>();
            existing.Add(new Rect(center2D.X - (dot.PreviewSize.X / 2d), center2D.Y - (dot.PreviewSize.Y / 2d), dot.PreviewSize.X, dot.PreviewSize.Y));

            foreach (var nextDot in nearbyOnPlane)
            {
                #region project dot plane

                // Get the 2D direction this sketch is from the main
                Vector direction = rotate.Transform(nextDot.PlanePoint - dot.Position).ToVector2D();

                Vector dirUnit = direction.ToUnit();
                if (Math2D.IsInvalid(dirUnit))
                {
                    dirUnit = Math3D.GetRandomVector_Circular_Shell(1).ToVector2D();        // sitting on top of each other, just push it in a random direction
                }
                dirUnit = new Vector(dirUnit.X, -dirUnit.Y);

                #endregion

                #region find clear space

                Point point = new Point();
                Rect rect = new Rect();
                double halfX = nextDot.Dot.PreviewSize.X / 2d;
                double halfY = nextDot.Dot.PreviewSize.Y / 2d;

                // Keep walking along that direction until the rectangle doesn't intersect any existing sketches
                for (int cntr = startIncrement; cntr < 1000; cntr++)
                {
                    point = center2D + (dirUnit * (stepDist * cntr));
                    rect = new Rect(point.X - halfX, point.Y - halfY, nextDot.Dot.PreviewSize.X, nextDot.Dot.PreviewSize.Y);

                    if (!existing.Any(o => o.IntersectsWith(rect)))
                    {
                        break;
                    }
                }

                existing.Add(rect);

                #endregion

                ShowPreview(nextDot.Dot, point);
            }
        }

        private static void EnsurePreviewGenerated(Dot dot, Func<ISOMInput, Tuple<UIElement, VectorInt>> getPreview)
        {
            if (dot.Preview != null)
            {
                return;
            }

            var previewRaw = getPreview(dot.Input);

            dot.Preview = new Border()
            {
                BorderBrush = new SolidColorBrush(Color.FromArgb(192, dot.Color.R, dot.Color.G, dot.Color.B)),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(2),
                Child = previewRaw.Item1,
            };

            dot.PreviewSize = new VectorInt(previewRaw.Item2.X + 4, previewRaw.Item2.Y + 4);
        }

        private static Dot[] GetNearbyDots(Dot dot, IEnumerable<Dot> all, double radius)
        {
            double radiusSquared = radius * radius;

            return all.
                Where(o => o.Token != dot.Token).
                Select(o => new { Dot = o, DistSquared = (o.Position - dot.Position).LengthSquared }).
                Where(o => o.DistSquared <= radiusSquared).
                OrderBy(o => o.DistSquared).
                Select(o => o.Dot).
                ToArray();
        }

        private static GeometryModel3D BuildDot_Visual(Model3DGroup modelGroup, MeshGeometry3D dotGeometry, SortedList<string, MaterialGroup> dotMaterials, SpecularMaterial specular, Color color, TranslateTransform3D translate)
        {
            string colorKey = UtilityWPF.ColorToHex(color);

            // Material
            MaterialGroup material;
            if (!dotMaterials.TryGetValue(colorKey, out material))
            {
                material = new MaterialGroup();
                material.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
                material.Children.Add(specular);

                dotMaterials.Add(colorKey, material);
            }

            // Geometry
            GeometryModel3D retVal = new GeometryModel3D()
            {
                Material = material,
                BackMaterial = material,
                Geometry = dotGeometry,
                Transform = translate,
            };

            modelGroup.Children.Add(retVal);

            return retVal;
        }
        private static IEnumerable<Tuple<Dot, Dot, double>> BuildDot_Distances(Dot dot, IEnumerable<Dot> otherDots)
        {
            double mult = .5d / Math.Sqrt(dot.Input.Weights.Length);        // the .5 doesn't mean anything.  It just helps control overall distance

            foreach (Dot other in otherDots)
            {
                double distance = MathND.GetDistance(dot.Input.Weights, other.Input.Weights) * mult;

                yield return Tuple.Create(dot, other, distance);
            }
        }

        #endregion
    }
}
