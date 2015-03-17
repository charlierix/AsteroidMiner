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
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.AsteroidMiner.MapParts;
using Game.Newt.v2.GameItems;

namespace Game.Newt.v2.AsteroidMiner.AstMin2D
{
    public partial class InfoScreen : UserControl
    {
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

        #endregion

        #region Constructor

        public InfoScreen(Map map, Point3D boundryMin, Point3D boundryMax)
        {
            InitializeComponent();

            _map = map;
            _boundryMin = boundryMin;
            _boundryMax = boundryMax;
        }

        #endregion

        #region Public Methods

        public void UpdateScreen()
        {
            RedrawCanvas();
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

                    if (this.CloseRequested == null)
                    {
                        MessageBox.Show("There is no event listener to close this panel", "Back Button", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    this.CloseRequested(this, new EventArgs());
                }
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
                if (this.CloseRequested == null)
                {
                    MessageBox.Show("There is no event listener for the back button", "Back Button", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                this.CloseRequested(this, new EventArgs());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

        private void DrawFlag(Point position, double width, double height, FrameworkElement flag, Effect effect = null)
        {
            FrameworkElement cloned = UtilityCore.Clone(flag);
            cloned.Width = width;
            cloned.Height = height;

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

        private Transform GetMapToCanvasTransform()
        {
            return GetMapToCanvasTransform(new Point(_boundryMin.X, _boundryMin.Y), new Point(_boundryMax.X, _boundryMax.Y), new Point(0, 0), new Point(canvasMap.ActualWidth, canvasMap.ActualHeight));
        }
        //TODO: Put this in UtilityWPF - may want to reword the name canvas to view rectangle - also, make an overload that takes Rects
        private static Transform GetMapToCanvasTransform(Point worldMin, Point worldMax, Point canvasMin, Point canvasMax)
        {
            Point worldCenter = new Point((worldMin.X + worldMax.X) / 2, (worldMin.Y + worldMax.Y) / 2);
            Point canvasCenter = new Point((canvasMax.X - canvasMin.X) / 2, (canvasMax.Y - canvasMin.Y) / 2);

            // Figure out zoom
            double zoomX = (canvasMax.X - canvasMin.X) / (worldMax.X - worldMin.X);
            double zoomY = (canvasMax.Y - canvasMin.Y) / (worldMax.Y - worldMin.Y);

            double zoom = Math.Min(zoomX, zoomY);

            TransformGroup retVal = new TransformGroup();
            retVal.Children.Add(new TranslateTransform((canvasCenter.X / zoom) - worldCenter.X, (canvasCenter.Y / zoom) - worldCenter.Y));
            retVal.Children.Add(new ScaleTransform(zoom, zoom));
            //retVal.Children.Add(new TranslateTransform(canvasCenter.X, canvasCenter.Y));

            return retVal;
        }

        #endregion
    }
}
