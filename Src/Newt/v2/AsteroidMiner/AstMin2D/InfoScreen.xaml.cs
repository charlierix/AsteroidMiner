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
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.AsteroidMiner.MapParts;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.MapParts;

namespace Game.Newt.v2.AsteroidMiner.AstMin2D
{
    public partial class InfoScreen : UserControl
    {
        #region Class: ResourcesVisual

        private class ResourcesVisual : FrameworkElement
        {
            #region Declaration Section

            private readonly DrawingVisual _visual;

            #endregion

            #region Constructor

            public ResourcesVisual(MapOctree snapshot, Func<MapOctree, double> getValue, Color color, Transform transform)
            {
                //const double STANDARDAREA = 1000000;
                const double STANDARDAREA = 750000;

                //TODO: Use this to get a normalization multiplier
                double totalArea = (snapshot.MaxRange.X - snapshot.MinRange.X) * (snapshot.MaxRange.Y - snapshot.MinRange.Y);
                double valueMult = totalArea / STANDARDAREA;

                _visual = new DrawingVisual();
                using (DrawingContext dc = _visual.RenderOpen())
                {
                    foreach (MapOctree node in snapshot.Descendants(o => o.Children))
                    {
                        if (node.Items == null)
                        {
                            continue;
                        }

                        // Get the color
                        Brush brush = GetBrush(node, getValue, valueMult, color);
                        if (brush == null)
                        {
                            continue;
                        }

                        // Define the rectangle
                        Point min = transform.Transform(new Point(node.MinRange.X, -node.MinRange.Y));      // need to negate Y
                        Point max = transform.Transform(new Point(node.MaxRange.X, -node.MaxRange.Y));

                        double x = min.X;
                        double y = max.Y;       // can't use min, because Y is backward

                        double width = max.X - min.X;
                        double height = Math.Abs(max.Y - min.Y);       // Y is all flipped around

                        // Fill the box
                        dc.DrawRectangle(brush, null, new Rect(x, y, width, height));
                    }
                }
            }

            #endregion

            #region Overrides

            protected override Visual GetVisualChild(int index)
            {
                return _visual;
            }
            protected override int VisualChildrenCount
            {
                get
                {
                    return 1;
                }
            }

            #endregion

            #region Private Methods

            private static Brush GetBrush(MapOctree node, Func<MapOctree, double> getValue, double valueMult, Color color)
            {
                const double MAXOPACITY = .25;

                //if (node.Items.Any(o => o.MapObject is ShipPlayer))
                //{
                //    return new SolidColorBrush(UtilityWPF.ColorFromHex("600000FF"));
                //}

                // Add up the resources
                double resourceValue = getValue(node);
                if (resourceValue.IsNearZero())
                {
                    return null;
                }

                double area = (node.MaxRange.X - node.MinRange.X) * (node.MaxRange.Y - node.MinRange.Y);

                double opacity = (resourceValue * valueMult) / area;
                if (opacity > 1)
                {
                    opacity = 1;
                }

                Color colorFinal = UtilityWPF.AlphaBlend(color, Colors.Transparent, opacity * MAXOPACITY);
                return new SolidColorBrush(colorFinal);
            }

            #endregion
        }

        #endregion

        #region Events

        public event EventHandler CloseRequested = null;

        #endregion

        #region Declaration Section

        private const string TITLE = "Info Screen";

        private readonly Point3D _boundryMin;
        private readonly Point3D _boundryMax;

        private readonly Map _map;

        private readonly Brush _playerBrush = new SolidColorBrush(UtilityWPF.ColorFromHex("FF176BBF"));
        private readonly Brush _playerStroke = new SolidColorBrush(UtilityWPF.ColorFromHex("FF5C99D6"));

        private readonly DispatcherTimer _backgroundSwapTimer;

        private UIElement _mineralVisual = null;
        private UIElement _asteroidVisual = null;
        private bool _isShowingMineral = false;

        #endregion

        #region Constructor

        public InfoScreen(Map map, Point3D boundryMin, Point3D boundryMax)
        {
            InitializeComponent();

            _map = map;
            _boundryMin = boundryMin;
            _boundryMax = boundryMax;

            _backgroundSwapTimer = new DispatcherTimer();
            _backgroundSwapTimer.Interval = TimeSpan.FromSeconds(1.5);
            _backgroundSwapTimer.IsEnabled = false;
            _backgroundSwapTimer.Tick += new EventHandler(BackgroundSwapTimer_Tick);
        }

        #endregion

        #region Public Methods

        public void UpdateScreen()
        {
            RedrawCanvas();

            _backgroundSwapTimer.IsEnabled = true;
        }

        #endregion

        #region Event Listeners

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                //NOTE: This MUST be set in loaded, or keyboard events get very flaky (even if the usercontrol has focus, something
                //inside of it must also have focus)
                //lblFocusable.Focus();
                btnBack.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                RedrawCanvas();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UserControl_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Tab)
                {
                    e.Handled = true;

                    OnCloseRequested();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackgroundSwapTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (_mineralVisual == null || _asteroidVisual == null)
                {
                    // There's either zero or one background.  Nothing to swap
                    return;
                }

                canvasMap.Children.Remove(_mineralVisual);
                canvasMap.Children.Remove(_asteroidVisual);

                _isShowingMineral = !_isShowingMineral;

                // The background is based on the map's snapshot, and opacities change based on node size.  Even though the world is paused, the
                // map is still generating snapshots with random node placements, which change the view a bit
                DrawAsteroidsMinerals(GetMapToCanvasTransform(), false);

                canvasMap.Children.Insert(0, _isShowingMineral ? _mineralVisual : _asteroidVisual);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OnCloseRequested();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Protected Methods

        protected virtual void OnCloseRequested()
        {
            _backgroundSwapTimer.IsEnabled = false;

            if (this.CloseRequested == null)
            {
                MessageBox.Show("There is no event listener for the back button", "Back Button", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            this.CloseRequested(this, new EventArgs());
        }

        #endregion

        #region Private Methods

        private void RedrawCanvas()
        {
            if (Math3D.IsNearZero(canvasMap.ActualWidth) || Math3D.IsNearZero(canvasMap.ActualHeight))
            {
                return;
            }

            Transform transform = GetMapToCanvasTransform();

            Effect effect = new DropShadowEffect()
            {
                Color = Colors.Black,
                Opacity = .25,
                BlurRadius = 10,
                Direction = 0,
                ShadowDepth = 0
            };

            canvasMap.Children.Clear();

            foreach (SpaceStation2D station in _map.GetItems<SpaceStation2D>(false))
            {
                Point3D position = station.PositionWorld;
                Point positionView = transform.Transform(new Point(position.X, -position.Y));

                DrawFlag(positionView, 30, 20, station.Flag, effect);
            }

            foreach (ShipPlayer ship in _map.GetItems<ShipPlayer>(false))
            {
                Point3D position = ship.PositionWorld;
                Point positionView = transform.Transform(new Point(position.X, -position.Y));

                Vector3D dir3D = ship.PhysicsBody.DirectionToWorld(new Vector3D(0, 1, 0));
                double angle = Vector.AngleBetween(new Vector(dir3D.X, dir3D.Y), new Vector(0, 1));

                DrawTriangle(positionView, 10, 20, angle, _playerBrush, _playerStroke, 1, effect);
            }

            DrawAsteroidsMinerals(transform, true);

            ShowCameraView();
        }

        private void DrawTestLines(Transform transform)
        {
            DrawLine(_boundryMin.X, _boundryMin.Y, _boundryMax.X, _boundryMax.Y, Brushes.Chartreuse, 10, transform);
            DrawLine(_boundryMax.X, _boundryMin.Y, _boundryMin.X, _boundryMax.Y, Brushes.Chartreuse, 10, transform);

            DrawLine(_boundryMin.X, _boundryMin.Y, _boundryMax.X, _boundryMin.Y, Brushes.HotPink, 10, transform);
            DrawLine(_boundryMax.X, _boundryMin.Y, _boundryMax.X, _boundryMax.Y, Brushes.HotPink, 10, transform);
            DrawLine(_boundryMax.X, _boundryMax.Y, _boundryMin.X, _boundryMax.Y, Brushes.HotPink, 10, transform);
            DrawLine(_boundryMin.X, _boundryMax.Y, _boundryMin.X, _boundryMin.Y, Brushes.HotPink, 10, transform);

            DrawCircle(0, 0, Brushes.Yellow, 10, Transform.Identity);
            DrawCircle(canvasMap.ActualWidth, 0, Brushes.Yellow, 10, Transform.Identity);
            DrawCircle(canvasMap.ActualWidth, canvasMap.ActualHeight, Brushes.Yellow, 10, Transform.Identity);
            DrawCircle(0, canvasMap.ActualHeight, Brushes.Yellow, 10, Transform.Identity);
        }

        private void DrawFlag(Point position, double width, double height, FlagVisual flag, Effect effect = null)
        {
            // This fails when trying to clone (no default constructor)
            //FrameworkElement cloned = UtilityCore.Clone(flag);
            //cloned.Width = width;
            //cloned.Height = height;

            FlagVisual cloned = new FlagVisual(width, height, flag.FlagProps);

            Canvas.SetLeft(cloned, position.X - (width / 2));
            Canvas.SetTop(cloned, position.Y - (height / 2));

            if (effect != null)
            {
                cloned.Effect = effect;
            }

            canvasMap.Children.Add(cloned);
        }
        private void DrawTriangle(Point position, double width, double height, double angle, Brush fill, Brush stroke, double strokeThickness, Effect effect = null)
        {
            Polygon triangle = new Polygon()
            {
                Fill = fill,
                Stroke = stroke,
                StrokeThickness = strokeThickness
            };

            double halfWidth = width / 2;
            double halfHeight = height / 2;

            triangle.Points.Add(new Point(-halfWidth, halfHeight));
            triangle.Points.Add(new Point(halfWidth, halfHeight));
            triangle.Points.Add(new Point(0, -halfHeight));

            Canvas.SetLeft(triangle, position.X - halfWidth);
            Canvas.SetTop(triangle, position.Y - halfHeight);

            if (effect != null)
            {
                triangle.Effect = effect;
            }

            triangle.RenderTransform = new RotateTransform(angle);

            canvasMap.Children.Add(triangle);
        }

        private void DrawLine(double x1, double y1, double x2, double y2, Brush color, double thickness, Transform transform)
        {
            Line line = new Line()
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Stroke = color,
                StrokeThickness = thickness,
            };

            //line.LayoutTransform = transform;
            line.RenderTransform = transform;

            canvasMap.Children.Add(line);
        }
        private void DrawCircle(double x, double y, Brush color, double radius, Transform transform)
        {
            Ellipse ellipse = new Ellipse()
            {
                Width = radius * 2,
                Height = radius * 2,
                Fill = color
            };

            Canvas.SetLeft(ellipse, x - radius);
            Canvas.SetTop(ellipse, y - radius);

            //ellipse.LayoutTransform = transform;
            ellipse.RenderTransform = transform;

            canvasMap.Children.Add(ellipse);
        }

        private void DrawAsteroidsMinerals(Transform transform, bool shouldPopulateCanvas)
        {
            const double MINERALMULT = 100d;
            const double ASTEROIDMULT = .25d;

            MapOctree snapshot = _map.LatestSnapshot;
            if (snapshot == null)
            {
                return;
            }

            #region GetValue delegates

            // These get the value of the items in that node.  The value will be divided by the area of the node to figure out opacity.
            // So the value/area should be 1 when the node is rich in that resource

            Func<MapOctree, double> getMineralValue = new Func<MapOctree, double>((node) =>
            {
                Mineral[] minerals = node.Items.
                    Where(o => o.MapObject is Mineral).
                    Select(o => (Mineral)o.MapObject).
                    ToArray();

                if (minerals.Length == 0)
                {
                    return 0d;
                }
                else
                {
                    return minerals.Sum(o => Convert.ToDouble(o.Credits) * MINERALMULT);
                }
            });

            Func<MapOctree, double> getAsteroidValue = new Func<MapOctree, double>((node) =>
            {
                Asteroid[] asteroids = node.Items.
                    Where(o => o.MapObject is Asteroid).
                    Select(o => (Asteroid)o.MapObject).
                    ToArray();

                if (asteroids.Length == 0)
                {
                    return 0d;
                }
                else
                {
                    return asteroids.Sum(o => o.PhysicsBody.Mass * ASTEROIDMULT);
                }
            });

            #endregion

            // Create the visuals
            _mineralVisual = new ResourcesVisual(snapshot, getMineralValue, Colors.Lime, transform);
            _asteroidVisual = new ResourcesVisual(snapshot, getAsteroidValue, Colors.LightGray, transform);

            // Show a random one (the timer will swap them every tick)
            if (shouldPopulateCanvas)
            {
                _isShowingMineral = StaticRandom.NextBool();
                canvasMap.Children.Insert(0, _isShowingMineral ? _mineralVisual : _asteroidVisual);
            }

            #region Get Stats

            //double width = snapshot.MaxRange.X - snapshot.MinRange.X;
            //double height = snapshot.MaxRange.Y - snapshot.MinRange.Y;
            //double diagonal = Math3D.Avg(width, height) * Math.Sqrt(2);
            //double area = width * height;
            //double mineralValue = snapshot.Descendants(o => o.Children).Where(o => o.Items != null).Sum(o => getMineralValue(o));
            //double asteroidValue = snapshot.Descendants(o => o.Children).Where(o => o.Items != null).Sum(o => getAsteroidValue(o));

            //string stats = "";
            //stats += string.Format("width\t{0}\r\n", width);
            //stats += string.Format("height\t{0}\r\n", height);
            //stats += string.Format("diagonal\t{0}\r\n", diagonal);
            //stats += string.Format("area\t{0}\r\n", area);
            //stats += string.Format("minerals\t{0}\r\n", mineralValue);
            //stats += string.Format("asteroids\t{0}\r\n", asteroidValue);
            //stats += "\r\n";
            //stats += string.Format("min/diag\t{0}\r\n", mineralValue / diagonal);
            //stats += string.Format("ast/diag\t{0}\r\n", asteroidValue / diagonal);
            //stats += string.Format("min/area\t{0}\r\n", mineralValue / area);
            //stats += string.Format("ast/area\t{0}\r\n", asteroidValue / area);

            //Clipboard.SetText(stats);

            #endregion
        }

        private Transform GetMapToCanvasTransform()
        {
            return UtilityWPF.GetMapToCanvasTransform(new Point(_boundryMin.X, _boundryMin.Y), new Point(_boundryMax.X, _boundryMax.Y), new Point(0, 0), new Point(canvasMap.ActualWidth, canvasMap.ActualHeight));
        }

        #endregion

        private readonly double _tangent = Math.Tan(Math3D.DegreesToRadians(45 / 2));       // camera's field of view is 45, half of that is a right triangle

        //TODO: If this stays being used, get constants from CameraHelper
        private void ShowCameraView()
        {
            Transform transform = GetMapToCanvasTransform();

            ShipPlayer ship = _map.GetItems<ShipPlayer>(false).First();

            Point3D position = ship.PositionWorld;
            Point positionView = transform.Transform(new Point(position.X, -position.Y));

            position = new Point3D(position.X, -position.Y, 0);



            double cameraZ = 30 * ship.Radius;        // multiply by radius so that bigger ships see more



            //tan(theta)=rise/run
            //rise=tan(theta)*run
            double halfWidth = _tangent * cameraZ;

            DrawBox(position, halfWidth, new SolidColorBrush(UtilityWPF.ColorFromHex("70EEEEEE")), transform);


            // These are for the stars

            //double starMin = _boundryMin.Z * 20;
            //double starMax = _boundryMin.Z * 1.5;

            //halfWidth = _tangent * (cameraZ + Math.Abs(starMax));
            //DrawBox(position, halfWidth, Brushes.Orange, transform);

            //halfWidth = _tangent * (cameraZ + Math.Abs(starMin));
            //DrawBox(position, halfWidth, Brushes.Orange, transform);


        }
        private void DrawBox(Point3D point, double halfWidth, Brush brush, Transform transform)
        {
            DrawLine(point.X - halfWidth, point.Y - halfWidth, point.X + halfWidth, point.Y - halfWidth, brush, 1, transform);
            DrawLine(point.X + halfWidth, point.Y - halfWidth, point.X + halfWidth, point.Y + halfWidth, brush, 1, transform);
            DrawLine(point.X + halfWidth, point.Y + halfWidth, point.X - halfWidth, point.Y + halfWidth, brush, 1, transform);
            DrawLine(point.X - halfWidth, point.Y + halfWidth, point.X - halfWidth, point.Y - halfWidth, brush, 1, transform);
        }
    }
}
