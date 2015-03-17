using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace Game.Newt.Testers
{
    public partial class ShipCameraTesterSnapshot : Window
    {
        #region Class: TriangleOverlay

        public class TriangleOverlay
        {
            public TriangleOverlay(TriangleIndexed triangle, Tuple<int, int, double>[] pixels)
            {
                this.Triangle = triangle;
                this.Pixels = pixels;
            }

            public readonly TriangleIndexed Triangle;

            /// <summary>
            /// Ints are X,Y coords of the pixel.  Double is the percent (0 to 1) of influence of that pixel
            /// </summary>
            public readonly Tuple<int, int, double>[] Pixels;
        }

        #endregion
        #region Class: PolygonOverlay

        public class PolygonOverlay
        {
            public PolygonOverlay(Edge2D[] polygonEdges, Point[] polygonPoints, Tuple<int, int, double>[] pixels)
            {
                this.PolygonEdges = polygonEdges;
                this.PolygonPoints = polygonPoints;
                this.Pixels = pixels;
            }

            public readonly Edge2D[] PolygonEdges;
            public readonly Point[] PolygonPoints;

            /// <summary>
            /// Ints are X,Y coords of the pixel.  Double is the percent (0 to 1) of influence of that pixel
            /// </summary>
            public readonly Tuple<int, int, double>[] Pixels;
        }

        #endregion

        #region Declaration Section

        private Random _rand = new Random();

        private TriangleOverlay[] _triangles = null;
        private PolygonOverlay[] _polygons = null;

        private Rectangle[] _rectangleVisuals = null;
        private Polygon[] _polygonVisuals = null;
        private Polygon[] _triangleVisuals = null;
        private Ellipse[] _circleVisuals = null;

        private bool _isInitialized = false;

        #endregion

        #region Constructor

        public ShipCameraTesterSnapshot()
        {
            InitializeComponent();

            _isInitialized = true;

            AdjustPlates();
        }

        #endregion

        #region Public Properties

        //  This should be the same as _triangles.Length, but may be slightly off
        private int _numNeurons = -1;
        public int NumNeurons
        {
            get
            {
                return _numNeurons;
            }
        }

        private Tuple<int, int, Rect[]> _pixels = null;
        public Tuple<int, int, Rect[]> Pixels
        {
            get
            {
                return _pixels;
            }
        }

        private Size _triangleExtremes = new Size(0, 0);
        public Size TriangleExtremes
        {
            get
            {
                return _triangleExtremes;
            }
        }

        private Size _polygonExtremes = new Size(0, 0);
        public Size PolygonExtremes
        {
            get
            {
                return _polygonExtremes;
            }
        }

        public TriangleIndexed[] Triangles
        {
            get
            {
                if (_triangles == null)
                {
                    return null;
                }

                return _triangles.Select(o => o.Triangle).ToArray();
            }
        }

        public Point[][] Polygons
        {
            get
            {
                if (_polygons == null)
                {
                    return null;
                }

                return _polygons.Select(o => o.PolygonPoints).ToArray();
            }
        }

        private IBitmapCustom _bitmap = null;
        public IBitmapCustom Bitmap
        {
            get
            {
                return _bitmap;
            }
        }

        #endregion

        #region Public Methods

        public void SetTriangles(int numNeurons, TriangleOverlay[] triangles, Size triangleExtremes, PolygonOverlay[] polygons, Size polygonExtremes, Tuple<int, int, Rect[]> pixels)
        {
            _numNeurons = numNeurons;
            _triangles = triangles;
            _triangleExtremes = triangleExtremes;
            _polygons = polygons;
            _polygonExtremes = polygonExtremes;
            _pixels = pixels;

            ShowPixels();
            ShowPolygons();
            ShowTriangles();
            ShowCircles();
        }

        public void UpdateBitmap(IBitmapCustom bitmap)
        {
            if (_pixels == null)
            {
                return;
            }

            _bitmap = bitmap;

            Color[] colors = null;
            if (_bitmap == null)
            {
                colors = Enumerable.Repeat(Colors.Transparent, _pixels.Item1 * _pixels.Item2).ToArray();
            }
            else
            {
                colors = bitmap.GetColors(0, 0, _pixels.Item1, _pixels.Item2);
            }

            ColorPixels(colors);
            ColorPolygons(colors);
            ColorTriangles(colors);
            ColorCircles(colors);
        }

        #endregion

        #region Event Listeners

        private void grdTriangles_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                ResizeTrianglesCircles();
                ResizePolygons();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Show_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_isInitialized)
                {
                    return;
                }

                AdjustPlates();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Opacity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (!_isInitialized)
                {
                    return;
                }

                AdjustPlates();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Triangle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (_triangleVisuals == null || _rectangleVisuals == null)
                {
                    return;
                }

                int index = Array.IndexOf(_triangleVisuals, e.OriginalSource);
                if (index < 0)
                {
                    return;
                }

                RandColorTriangleAndPixels(index);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnColorRandom_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RandColorTriangleAndPixels(_rand.Next(_triangleVisuals.Length));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnColorAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                for (int cntr = 0; cntr < _triangleVisuals.Length; cntr++)
                {
                    RandColorTriangleAndPixels(cntr);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private void ShowPixels()
        {
            _rectangleVisuals = null;
            grdPixels.Children.Clear();

            if (_pixels == null)
            {
                return;
            }

            grdPixels.Columns = _pixels.Item1;
            grdPixels.Rows = _pixels.Item2;

            _rectangleVisuals = new Rectangle[_pixels.Item1 * _pixels.Item2];

            for (int cntr = 0; cntr < _rectangleVisuals.Length; cntr++)
            {
                _rectangleVisuals[cntr] = new Rectangle();
                _rectangleVisuals[cntr].Fill = Brushes.Transparent;
                grdPixels.Children.Add(_rectangleVisuals[cntr]);
            }
        }
        private void ShowPolygons()
        {
            _polygonVisuals = null;
            grdPolygons.Children.Clear();

            if (_polygons == null)
            {
                return;
            }

            _polygonVisuals = new Polygon[_polygons.Length];

            for (int cntr = 0; cntr < _polygonVisuals.Length; cntr++)
            {
                _polygonVisuals[cntr] = new Polygon();

                foreach (Point point in _polygons[cntr].PolygonPoints)
                {
                    _polygonVisuals[cntr].Points.Add(point);
                }

                _polygonVisuals[cntr].Fill = Brushes.Transparent;

                grdPolygons.Children.Add(_polygonVisuals[cntr]);
            }

            ResizeTrianglesCircles();
        }
        private void ShowTriangles()
        {
            _triangleVisuals = null;
            grdTriangles.Children.Clear();

            if (_triangles == null)
            {
                return;
            }

            _triangleVisuals = new Polygon[_triangles.Length];

            for (int cntr = 0; cntr < _triangleVisuals.Length; cntr++)
            {
                _triangleVisuals[cntr] = new Polygon();
                _triangleVisuals[cntr].Points.Add(new Point(_triangles[cntr].Triangle.Point0.X, _triangles[cntr].Triangle.Point0.Y));
                _triangleVisuals[cntr].Points.Add(new Point(_triangles[cntr].Triangle.Point1.X, _triangles[cntr].Triangle.Point1.Y));
                _triangleVisuals[cntr].Points.Add(new Point(_triangles[cntr].Triangle.Point2.X, _triangles[cntr].Triangle.Point2.Y));
                _triangleVisuals[cntr].Fill = Brushes.Transparent;

                _triangleVisuals[cntr].MouseDown += new MouseButtonEventHandler(Triangle_MouseDown);

                grdTriangles.Children.Add(_triangleVisuals[cntr]);
            }

            ResizePolygons();
        }
        private void ShowCircles()
        {
            _circleVisuals = null;
            grdCircles.Children.Clear();

            if (_triangles == null)
            {
                return;
            }

            _circleVisuals = new Ellipse[_triangles.Length];

            for (int cntr = 0; cntr < _circleVisuals.Length; cntr++)
            {
                _circleVisuals[cntr] = new Ellipse();

                Point3D center = _triangles[cntr].Triangle.GetCenterPoint();
                double radius = Math3D.Min(
                    Math3D.GetClosestDistance_Line_Point(_triangles[cntr].Triangle.Point0, _triangles[cntr].Triangle.Point1 - _triangles[cntr].Triangle.Point0, center),
                    Math3D.GetClosestDistance_Line_Point(_triangles[cntr].Triangle.Point1, _triangles[cntr].Triangle.Point2 - _triangles[cntr].Triangle.Point1, center),
                    Math3D.GetClosestDistance_Line_Point(_triangles[cntr].Triangle.Point2, _triangles[cntr].Triangle.Point0 - _triangles[cntr].Triangle.Point2, center));

                //  Min is too small, but avg has overlap.  Getting an inscribed circle would be the right way, but this is just a quick tester
                //double radius = new double[] {
                //    Math3D.GetClosestDistanceBetweenPointAndLine(_triangles[cntr].Triangle.Point0, _triangles[cntr].Triangle.Point1 - _triangles[cntr].Triangle.Point0, center),
                //    Math3D.GetClosestDistanceBetweenPointAndLine(_triangles[cntr].Triangle.Point1, _triangles[cntr].Triangle.Point2 - _triangles[cntr].Triangle.Point1, center),
                //    Math3D.GetClosestDistanceBetweenPointAndLine(_triangles[cntr].Triangle.Point2, _triangles[cntr].Triangle.Point0 - _triangles[cntr].Triangle.Point2, center) }.Average();

                _circleVisuals[cntr].HorizontalAlignment = HorizontalAlignment.Left;
                _circleVisuals[cntr].VerticalAlignment = VerticalAlignment.Top;
                _circleVisuals[cntr].Width = radius * 2d;
                _circleVisuals[cntr].Height = radius * 2d;
                _circleVisuals[cntr].Margin = new Thickness(center.X - radius, center.Y - radius, 0, 0);

                _circleVisuals[cntr].Fill = Brushes.Transparent;

                grdCircles.Children.Add(_circleVisuals[cntr]);
            }

            grdTriangles_SizeChanged(this, null);
        }

        private void ColorPixels(Color[] colors)
        {
            if (_rectangleVisuals == null || _rectangleVisuals.Length != colors.Length)
            {
                throw new ApplicationException("Rectangles are out of sync with the colors");
            }

            for (int cntr = 0; cntr < colors.Length; cntr++)
            {
                _rectangleVisuals[cntr].Fill = new SolidColorBrush(colors[cntr]);
            }
        }
        private void ColorPolygons(Color[] colors)
        {
            if (_polygonVisuals == null || _polygonVisuals.Length != _polygons.Length)
            {
                throw new ApplicationException("Polygons are out of sync");
            }

            for (int cntr = 0; cntr < _polygons.Length; cntr++)
            {
                _polygonVisuals[cntr].Fill = new SolidColorBrush(GetTriangleColor(_polygons[cntr].Pixels, colors, _pixels.Item1));
            }
        }
        private void ColorTriangles(Color[] colors)
        {
            if (_triangleVisuals == null || _triangleVisuals.Length != _triangles.Length)
            {
                throw new ApplicationException("Triangles are out of sync");
            }



            //int testMinX = _triangles.SelectMany(o => o.Pixels).Min(o => o.Item1);
            //int testMinY = _triangles.SelectMany(o => o.Pixels).Min(o => o.Item2);
            //int testMaxX = _triangles.SelectMany(o => o.Pixels).Max(o => o.Item1);
            //int testMaxY = _triangles.SelectMany(o => o.Pixels).Max(o => o.Item2);

            //var testEmpty = _triangles.Where(o => o.Pixels.Length == 0).ToArray();



            for (int cntr = 0; cntr < _triangles.Length; cntr++)
            {
                _triangleVisuals[cntr].Fill = new SolidColorBrush(GetTriangleColor(_triangles[cntr].Pixels, colors, _pixels.Item1));
            }
        }
        private void ColorCircles(Color[] colors)
        {
            if (_circleVisuals == null || _circleVisuals.Length != _triangles.Length)
            {
                throw new ApplicationException("Circles are out of sync");
            }

            for (int cntr = 0; cntr < _triangles.Length; cntr++)
            {
                _circleVisuals[cntr].Fill = new SolidColorBrush(GetTriangleColor(_triangles[cntr].Pixels, colors, _pixels.Item1));
            }
        }

        private void ResizeTrianglesCircles()
        {
            if (_triangles == null)
            {
                return;
            }

            grdTriangles.LayoutTransform = null;
            grdCircles.LayoutTransform = null;
            this.UpdateLayout();

            double minX = _triangles.SelectMany(o => o.Triangle.AllPoints).Min(o => o.X);
            double minY = _triangles.SelectMany(o => o.Triangle.AllPoints).Min(o => o.X);
            double maxX = _triangles.SelectMany(o => o.Triangle.AllPoints).Max(o => o.X);
            double maxY = _triangles.SelectMany(o => o.Triangle.AllPoints).Max(o => o.X);

            //  Whatever margin the top left has, replicate the bottom right
            double expanedX = maxX + minX;
            double expanedY = maxY + minY;

            double width = grdTriangles.ActualWidth;
            double height = grdTriangles.ActualHeight;
            grdTriangles.LayoutTransform = new ScaleTransform(width / expanedX, height / expanedY);
            //grdTriangles.RenderTransform = new ScaleTransform(width / expanedX, height / expanedY);

            width = grdCircles.ActualWidth;     //  this is the same size, but if the triangle plate is invisible, actualwidth will be zero
            height = grdCircles.ActualHeight;
            grdCircles.LayoutTransform = new ScaleTransform(width / expanedX, height / expanedY);
        }
        private void ResizePolygons()
        {
            if (_polygons == null)
            {
                return;
            }

            grdPolygons.LayoutTransform = null;
            this.UpdateLayout();

            double minX = _polygons.SelectMany(o => o.PolygonPoints).Min(o => o.X);
            double minY = _polygons.SelectMany(o => o.PolygonPoints).Min(o => o.X);
            double maxX = _polygons.SelectMany(o => o.PolygonPoints).Max(o => o.X);
            double maxY = _polygons.SelectMany(o => o.PolygonPoints).Max(o => o.X);

            //  Whatever margin the top left has, replicate the bottom right
            double expanedX = maxX + minX;
            double expanedY = maxY + minY;

            double width = grdPolygons.ActualWidth;
            double height = grdPolygons.ActualHeight;
            grdPolygons.LayoutTransform = new ScaleTransform(width / expanedX, height / expanedY);
        }

        private void AdjustPlates()
        {
            // Pixels
            if (chkShowPixels.IsChecked.Value)
            {
                grdPixels.Visibility = Visibility.Visible;
                grdPixels.Opacity = UtilityCore.GetScaledValue_Capped(0d, 1d, trkPixelOpacity.Minimum, trkPixelOpacity.Maximum, trkPixelOpacity.Value);
            }
            else
            {
                grdPixels.Visibility = Visibility.Collapsed;
            }

            // Polygons
            if (chkShowPolygons.IsChecked.Value)
            {
                grdPolygons.Visibility = Visibility.Visible;
                grdPolygons.Opacity = UtilityCore.GetScaledValue_Capped(0d, 1d, trkPolygonOpacity.Minimum, trkPolygonOpacity.Maximum, trkPolygonOpacity.Value);
            }
            else
            {
                grdPolygons.Visibility = Visibility.Collapsed;
            }

            // Triangles
            if (chkShowTriangles.IsChecked.Value)
            {
                grdTriangles.Visibility = Visibility.Visible;
                grdTriangles.Opacity = UtilityCore.GetScaledValue_Capped(0d, 1d, trkTriangleOpacity.Minimum, trkTriangleOpacity.Maximum, trkTriangleOpacity.Value);
            }
            else
            {
                grdTriangles.Visibility = Visibility.Collapsed;
            }

            // Circles
            if (chkShowCircles.IsChecked.Value)
            {
                grdCircles.Visibility = Visibility.Visible;
                grdCircles.Opacity = UtilityCore.GetScaledValue_Capped(0d, 1d, trkCircleOpacity.Minimum, trkCircleOpacity.Maximum, trkCircleOpacity.Value);
            }
            else
            {
                grdCircles.Visibility = Visibility.Collapsed;
            }

            grdTriangles_SizeChanged(this, null);
        }

        private void RandColorTriangleAndPixels(int index)
        {
            Color color = Color.FromRgb(Convert.ToByte(160 + _rand.Next(90)), Convert.ToByte(160 + _rand.Next(90)), Convert.ToByte(160 + _rand.Next(90)));
            Brush brush = new SolidColorBrush(color);

            //  Pixels
            foreach (var pixel in _triangles[index].Pixels)
            {
                _rectangleVisuals[(pixel.Item2 * _pixels.Item1) + pixel.Item1].Fill = brush;
            }

            //  Triangle
            _triangleVisuals[index].Fill = brush;
        }

        private static Color GetTriangleColor(Tuple<int, int, double>[] pixels, Color[] allColors, int width)
        {
            //TODO: See if linq is faster/slower than creating the array
            //return UtilityWPF.AverageColors(pixels.Select(o => Tuple.Create(allColors[(o.Item2 * o.Item1) + o.Item1], o.Item3)));

            Tuple<Color, double>[] colors = new Tuple<Color, double>[pixels.Length];

            for (int cntr = 0; cntr < pixels.Length; cntr++)
            {
                colors[cntr] = Tuple.Create(allColors[(pixels[cntr].Item2 * width) + pixels[cntr].Item1], pixels[cntr].Item3);
            }

            return UtilityWPF.AverageColors(colors);
            //return AverageColors(colors);
        }

        public static Color AverageColors(IEnumerable<Tuple<Color, double>> colors)
        {
            const double INV255 = 1d / 255d;
            const double NEARZERO = .001d;

            #region Convert to doubles

            List<Tuple<double, double, double, double>> doubles = new List<Tuple<double, double, double, double>>();

            double minAlpha = double.MaxValue;

            //  Convert to doubles from 0 to 1 (throw out fully transparent colors)
            foreach (var color in colors)
            {
                double a = (color.Item1.A * INV255) * color.Item2;

                if (a <= NEARZERO)
                {
                    continue;
                }

                doubles.Add(Tuple.Create(a, color.Item1.R * INV255, color.Item1.G * INV255, color.Item1.B * INV255));

                if (a < minAlpha)
                {
                    minAlpha = a;
                }
            }

            #endregion

            if (doubles.Count == 0)
            {
                return Colors.Transparent;
            }

            #region Weighted sum

            double maxA = 0;
            double sumA = 0, sumR = 0, sumG = 0, sumB = 0;
            double sumWeight = 0;

            foreach (var dbl in doubles)
            {
                double multiplier = dbl.Item1 / minAlpha;       //  dividing by min so that multiplier is always greater or equal to 1
                sumWeight += multiplier;

                //sumA += dbl.Item1;      //  this one isn't weighted, it's a simple average

                if (dbl.Item1 > maxA)
                {
                    maxA = dbl.Item1;
                }

                sumR += dbl.Item2 * multiplier;
                sumG += dbl.Item3 * multiplier;
                sumB += dbl.Item4 * multiplier;
            }

            double divisor = 1d / sumWeight;

            #endregion

            //  Exit Function
            //return GetColorCapped((sumA / doubles.Count) * 255d, sumR * divisor * 255d, sumG * divisor * 255d, sumB * divisor * 255d);
            return GetColorCapped(maxA * 255d, sumR * divisor * 255d, sumG * divisor * 255d, sumB * divisor * 255d);
        }

        private static Color GetColorCapped(double a, double r, double g, double b)
        {
            if (a < 0)
            {
                a = 0;
            }
            else if (a > 255)
            {
                a = 255;
            }

            if (r < 0)
            {
                r = 0;
            }
            else if (r > 255)
            {
                r = 255;
            }

            if (g < 0)
            {
                g = 0;
            }
            else if (g > 255)
            {
                g = 255;
            }

            if (b < 0)
            {
                b = 0;
            }
            else if (b > 255)
            {
                b = 255;
            }

            // Exit Function
            return Color.FromArgb(Convert.ToByte(a), Convert.ToByte(r), Convert.ToByte(g), Convert.ToByte(b));
        }

        #endregion
    }
}
