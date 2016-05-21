using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using Game.HelperClassesCore;

namespace Game.HelperClassesWPF.Controls2D
{
    //TODO: Finish this

    /// <summary>
    /// This maintains a tiled checkerboard background, meant to be used by testers so they are less boring
    /// </summary>
    public class BackgoundTiles : IDisposable
    {
        #region Declaration Section

        private const string TITLE = "BackgoundTiles";

        private readonly Canvas _canvas;
        private readonly PerspectiveCamera _camera;
        private readonly Brush _brush;

        private readonly Tuple<double, double> _standardSizeAtZoom;
        private readonly Tuple<double, double> _maxSizeAtZoom;
        private readonly double _k;
        private readonly double _b;

        private readonly Vector _initialOffset;

        private SortedList<VectorInt, Rectangle> _rectangles = new SortedList<VectorInt, Rectangle>();

        #endregion

        #region Constructor

        public BackgoundTiles(Canvas canvas, PerspectiveCamera camera, Brush brush, Tuple<double, double> standardSizeAtZoom = null, Tuple<double, double> maxSizeAtZoom = null)
        {
            _canvas = canvas;
            _camera = camera;
            _brush = brush;

            GetFixedScales(_camera.Position.Z, ref standardSizeAtZoom, ref maxSizeAtZoom, out _k, out _b);
            _standardSizeAtZoom = standardSizeAtZoom;
            _maxSizeAtZoom = maxSizeAtZoom;

            _initialOffset = Math3D.GetRandomVector(_standardSizeAtZoom.Item1).ToVector2D();

            _canvas.SizeChanged += Canvas_SizeChanged;
            _camera.Changed += Camera_Changed;

            _canvas.ClipToBounds = true;

            UpdateTiles();
        }

        #endregion

        #region IDisposable Members

        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {





            }
        }

        #endregion

        #region Event Listeners

        private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                UpdateTiles();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Camera_Changed(object sender, EventArgs e)
        {
            try
            {
                UpdateTiles();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private void UpdateTiles()
        {
            const double MINSIZE = 20;

            double width = _canvas.ActualWidth;
            double height = _canvas.ActualHeight;
            Point center = new Point(width / 2d, height / 2d);

            double size = 100;

            //TODO: Need an extra term for the speed of zoom
            //double size = _tileSize * (_atCameraZoom / Math.Abs(_camera.Position.Z));
            //double size = _tileSize * (_atCameraZoom / Math.Pow(Math.Abs(_camera.Position.Z), _zoomPow));
            size = Math.Max(size, MINSIZE);

            size = Math1D.Min(size, width, height);     // when they zoom way in, the size grows enormous


            // params:
            // MaxSize
            // MinSize
            // MinSize at zoom --- this will be used to calculate k and b


            const double MIN = 20;
            const double MAX = 300;
            double K = .1;
            double B = 5.8;

            double zoom = Math.Abs(_camera.Position.Z);
            zoom = (K * zoom) - B;
            size = (MAX - MIN) / (1 + Math.Exp(zoom));
            size += MIN;









            int right = ((width - (center.X + _initialOffset.X)) / size).ToInt_Ceiling();
            int down = ((height - (center.Y + _initialOffset.Y)) / size).ToInt_Ceiling();

            int left = -((center.X + _initialOffset.X) / size).ToInt_Ceiling();
            int up = -((center.Y + _initialOffset.Y) / size).ToInt_Ceiling();

            //left = Math.Min(-1, left);      // when it gets really zoomed in, the up and left disappear
            //up = Math.Min(-1, up);

            UpdateTiles_Quadrant(new AxisFor(Axis.X, left, right), new AxisFor(Axis.Y, up, down), center, size);

            foreach (VectorInt existing in _rectangles.Keys.ToArray())
            {
                if (existing.X < left || existing.X > right || existing.Y < up || existing.Y > down)
                {
                    Rectangle removeRect = _rectangles[existing];
                    _canvas.Children.Remove(removeRect);
                    _rectangles.Remove(existing);
                }
            }


        }
        private void UpdateTiles_Quadrant(AxisFor axisX, AxisFor axisY, Point center, double size)
        {
            foreach (int x in axisX.Iterate())
            {
                foreach (int y in axisY.Iterate())
                {
                    if ((x + y) % 2 != 0)
                    {
                        continue;       // skip every other, so that it's a checkerboard
                    }

                    VectorInt vect = new VectorInt(x, y);

                    Rectangle rect;
                    if (!_rectangles.TryGetValue(vect, out rect))
                    {
                        rect = new Rectangle() { Fill = _brush, };
                        _rectangles.Add(vect, rect);
                        _canvas.Children.Add(rect);
                    }

                    rect.Width = size;
                    rect.Height = size;

                    Canvas.SetLeft(rect, center.X + _initialOffset.X + (x * size));
                    Canvas.SetTop(rect, center.Y + _initialOffset.Y + (y * size));
                }
            }
        }

        private static void GetFixedScales(double cameraZ, ref Tuple<double, double> standardSizeAtZoom, ref Tuple<double, double> maxSizeAtZoom, out double k, out double b)
        {
            const double SIZESCALE = 4;
            const double ZOOMSCALE = 50;

            cameraZ = Math.Abs(cameraZ);

            if (standardSizeAtZoom == null && maxSizeAtZoom == null)
            {
                standardSizeAtZoom = Tuple.Create(100d, cameraZ);
                maxSizeAtZoom = Tuple.Create(standardSizeAtZoom.Item1 / SIZESCALE, standardSizeAtZoom.Item2 * ZOOMSCALE);
            }
            else if (standardSizeAtZoom == null)
            {
                standardSizeAtZoom = Tuple.Create(maxSizeAtZoom.Item1 * SIZESCALE, maxSizeAtZoom.Item2 / ZOOMSCALE);
            }
            else if (maxSizeAtZoom == null)
            {
                maxSizeAtZoom = Tuple.Create(standardSizeAtZoom.Item1 / SIZESCALE, standardSizeAtZoom.Item2 * ZOOMSCALE);
            }
            else
            {
                // It could be debated whether these should be fixed, or just throw an exception.  Since this class is just a background visual, I'll just fix it
                if (maxSizeAtZoom.Item1 >= standardSizeAtZoom.Item1)
                    maxSizeAtZoom = Tuple.Create(standardSizeAtZoom.Item1 / SIZESCALE, maxSizeAtZoom.Item2);

                if (maxSizeAtZoom.Item2 <= standardSizeAtZoom.Item2)
                    maxSizeAtZoom = Tuple.Create(maxSizeAtZoom.Item1, standardSizeAtZoom.Item2 * ZOOMSCALE);
            }

            // Solve for k first with b=0, to get the slope
            //https://www.symbolab.com/solver/equation-calculator
            //1.05 * maxSizeAtZoom.Item1 = maxSizeAtZoom.Item1 + ((standardSizeAtZoom.Item1 - maxSizeAtZoom.Item1) / (1 + Math.Exp(k * (maxSizeAtZoom.Item2 - standardSizeAtZoom.Item2))));
            //1.05 * maxSizeAtZoom.Item1 = maxSizeAtZoom.Item1 + ((standardSizeAtZoom.Item1 - maxSizeAtZoom.Item1) / (1 + u));
            //(1.05 * maxSizeAtZoom.Item1)(u+1) = maxSizeAtZoom.Item1*(u+1) + (standardSizeAtZoom.Item1 - maxSizeAtZoom.Item1);

            double almostMax = 1.05 * maxSizeAtZoom.Item1;
            //double diff = standardSizeAtZoom.Item1 - maxSizeAtZoom.Item1;
            //almostMax(u+1) = maxSizeAtZoom.Item1*(u+1) + diff;
            //almostMax*u + almostMax = maxSizeAtZoom.Item1*u + standardSizeAtZoom.Item1;
            //almostMax*u = maxSizeAtZoom.Item1*u + standardSizeAtZoom.Item1 - almostMax;
            //almostMax*u - maxSizeAtZoom.Item1*u = standardSizeAtZoom.Item1 - almostMax;

            k = 0;
            b = 0;
        }

        #endregion

        //TODO: Make this work
        private static void Solve(Tuple<double, double> std, Tuple<double, double> max)
        {
            //Maybe try to find an approximate solution.  Use that N-1 point method


            //Solve(Tuple.Create(200d, 10d), Tuple.Create(20d, 100d));


            //y = b + (1 / (kx - c))

            //p1 = std.i2*2, 0
            //p2 = std
            //p3 = max

            double b, k, c;
            Solve(out b, out k, out c, new Point(std.Item1 * 2, 0), new Point(std.Item1, std.Item2), new Point(max.Item1, max.Item2));
        }
        private static void Solve(out double b, out double k, out double c, Point p1, Point p2, Point p3)
        {
            // Solve for p1
            //y = b + (1/-c)

            // Solve for p3


            b = 0;
            k = 0;
            c = 0;

        }
    }
}
