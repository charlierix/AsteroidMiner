using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;

namespace Game.Newt.v2.AsteroidMiner.AstMin2D
{
    /// <summary>
    /// This draws and updates the background image
    /// </summary>
    /// <remarks>
    /// The stars can't all be 3D objects, there's too much of a performance hit.  So this creates a few layers of 2D
    /// star field images, and slides them at different rates to emulate 3D.  It also has some 3D stars to improve the
    /// effect
    /// </remarks>
    public class BackImageManager
    {
        //TODO: Instead of completely random positions, have more clumps and voids, ribbons, clouds
        #region Class: StarfieldVisual1

        private class StarfieldVisual1 : FrameworkElement
        {
            #region Declaration Section

            private readonly DrawingVisual _visual;

            private static ThreadLocal<Brush[]> _starBrushes = new ThreadLocal<Brush[]>(() =>
                {
                    Random rand = StaticRandom.GetRandomForThread();

                    return Enumerable.Range(0, 200).
                        //Select(o => new SolidColorBrush(new ColorHSV(rand.NextDouble(360), rand.NextDouble(0, 5), rand.NextDouble(80, 95)).ToRGB())).
                        Select(o => new SolidColorBrush(new ColorHSV(rand.NextDouble(360), rand.NextDouble(0, 7), rand.NextDouble(60, 95)).ToRGB())).
                        ToArray();
                });

            #endregion

            #region Constructor

            public StarfieldVisual1(double size, double margin, int numLayers, double starSizeMult, Color? color = null)
            {
                const double DENSITY = .002;

                int numStars = Convert.ToInt32((DENSITY / numLayers) * size * size);

                Brush[] brushes;
                if (color == null)
                {
                    brushes = _starBrushes.Value;
                }
                else
                {
                    brushes = new[] { new SolidColorBrush(color.Value) };
                }

                Random rand = StaticRandom.GetRandomForThread();
                double sizeMinMargin = size - margin;

                _visual = new DrawingVisual();
                using (DrawingContext dc = _visual.RenderOpen())
                {
                    for (int cntr = 0; cntr < numStars; cntr++)
                    {
                        //NOTE: Can't go all the way to the edge, stars would get cut off (the caller has to know the margin so it can properly overlap the images)
                        Point point = new Point(rand.NextDouble(margin, sizeMinMargin), rand.NextDouble(margin, sizeMinMargin));
                        double radius;
                        if (rand.NextDouble() > .9)
                        {
                            // Big
                            radius = rand.NextDouble(.9, 1.2);
                        }
                        else
                        {
                            // Small
                            radius = rand.NextDouble(.7, .9);
                        }

                        radius *= starSizeMult;

                        Brush brush = brushes[rand.Next(brushes.Length)];

                        //TODO: Blur these a bit
                        dc.DrawEllipse(brush, null, point, radius, radius);
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
        }

        #endregion
        #region Class: StarfieldVisual2

        private class StarfieldVisual2 : FrameworkElement
        {
            #region Declaration Section

            private readonly DrawingVisual _visual;

            private static ThreadLocal<Brush[]> _starBrushes_Small = new ThreadLocal<Brush[]>(() =>
            {
                Random rand = StaticRandom.GetRandomForThread();

                //TODO: Blur these a bit
                return Enumerable.Range(0, 200).
                    Select(o => new SolidColorBrush(new ColorHSV(rand.NextDouble(360), rand.NextDouble(0, 7), rand.NextDouble(60, 95)).ToRGB())).
                    ToArray();
            });

            private static ThreadLocal<Brush[]> _starBrushes_Large = new ThreadLocal<Brush[]>(() =>
            {
                Random rand = StaticRandom.GetRandomForThread();

                return Enumerable.Range(0, 200).
                    Select(o => GetBigStarBrush(rand)).
                    ToArray();
            });

            #endregion

            #region Constructor

            //TODO: Take in the points/sizes/colorindex instead of deriving them here
            public StarfieldVisual2(double size, double margin, int numStars, double starSizeMult, Tuple<Vector[], int> vectorField = null, Color? color = null)
            {
                this.Size = size;

                Brush[] brushes_Small;
                Brush[] brushes_Large;
                if (color == null)
                {
                    brushes_Small = _starBrushes_Small.Value;
                    brushes_Large = _starBrushes_Large.Value;
                }
                else
                {
                    brushes_Small = new[] { new SolidColorBrush(color.Value) };
                    brushes_Large = new[] { new SolidColorBrush(color.Value) };
                }

                Random rand = StaticRandom.GetRandomForThread();

                Point[] points = GetPoints(rand, size, margin, numStars, vectorField);

                _visual = new DrawingVisual();
                using (DrawingContext dc = _visual.RenderOpen())
                {
                    for (int cntr = 0; cntr < numStars; cntr++)
                    {
                        Brush brush = null;
                        double radius;
                        double sizeRand = rand.NextDouble();
                        if (sizeRand > .995)
                        {
                            // Really Big
                            brush = brushes_Large[rand.Next(brushes_Large.Length)];
                            radius = rand.NextDouble(2, 6);
                        }
                        else if (sizeRand > .9)
                        {
                            // Big
                            radius = rand.NextDouble(.9, 1.2);
                        }
                        else
                        {
                            // Small
                            radius = rand.NextDouble(.7, .9);
                        }

                        radius *= starSizeMult;

                        if (brush == null)
                        {
                            brush = brushes_Small[rand.Next(brushes_Small.Length)];
                        }

                        dc.DrawEllipse(brush, null, points[cntr], radius, radius);
                    }
                }

                //_visual.BitmapEffect
                //_visual.CacheMode
            }

            #endregion

            #region Public Properties

            public readonly double Size;

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

            private static Point[] GetPoints(Random rand, double size, double margin, int numStars, Tuple<Vector[], int> vectorField)
            {
                //NOTE: Can't go all the way to the edge, stars would get cut off (the caller has to know the margin so it can properly overlap the images)
                double reducedSize = size - (margin * 2);

                Point[] points = Enumerable.Range(0, numStars).
                    Select(o => new Point(rand.NextDouble(reducedSize), rand.NextDouble(reducedSize))).
                    ToArray();

                if (vectorField != null)
                {
                    points = AdjustPoints(points, reducedSize, vectorField.Item1, vectorField.Item2);
                }

                Transform transform = new TranslateTransform(margin, margin);

                return points.
                    Select(o => transform.Transform(o)).
                    ToArray();
            }

            private static Point[] AdjustPoints(Point[] points, double pointsSize, Vector[] field, int fieldSize)
            {
                double vectStep = pointsSize / fieldSize;

                Point[] retVal = new Point[points.Length];

                for (int cntr = 0; cntr < points.Length; cntr++)
                {
                    #region 4 corner indices

                    // Top Left
                    int col0 = Convert.ToInt32(Math.Floor(points[cntr].X / vectStep));
                    if (col0 >= fieldSize)
                    {
                        //NOTE: There's a chance these will be == _fieldSize.  This only happens when they are on the bottom or right edge.  When that happens,
                        //back up.  The % will be 1, so col1 will actually be used
                        col0 = fieldSize - 1;
                    }

                    int row0 = Convert.ToInt32(Math.Floor(points[cntr].Y / vectStep));
                    if (row0 >= fieldSize)
                    {
                        row0 = fieldSize - 1;
                    }

                    // Bottom Right
                    int col1 = col0 + 1;
                    if (col1 >= fieldSize)
                    {
                        col1 = 0;
                    }

                    int row1 = row0 + 1;
                    if (row1 >= fieldSize)
                    {
                        row1 = 0;
                    }

                    #endregion

                    Point ul = new Point(col0 * vectStep, row0 * vectStep);

                    double percentX = (points[cntr].X - ul.X) / vectStep;
                    double percentY = (points[cntr].Y - ul.Y) / vectStep;

                    Vector displace = LERP(field[(row0 * fieldSize) + col0], field[(row0 * fieldSize) + col1], field[(row1 * fieldSize) + col0], field[(row1 * fieldSize) + col1], percentX, percentY);

                    double displacedX = points[cntr].X + displace.X;
                    double displacedY = points[cntr].Y + displace.Y;

                    #region Wrap position

                    if (displacedX < 0)
                    {
                        displacedX += pointsSize;
                    }
                    else if (displacedX >= pointsSize)
                    {
                        displacedX -= pointsSize;
                    }

                    if (displacedY < 0)
                    {
                        displacedY += pointsSize;
                    }
                    else if (displacedY >= pointsSize)
                    {
                        displacedY -= pointsSize;
                    }

                    #endregion

                    retVal[cntr] = new Point(displacedX, displacedY);
                }

                return retVal;
            }

            private static RadialGradientBrush GetBigStarBrush(Random rand)
            {
                double hue;
                double hueRand = rand.NextDouble();
                if (hueRand > .95)
                {
                    // Greens
                    hue = rand.NextDouble(60, 80);
                }
                else if (hueRand > .9)
                {
                    // Reds
                    hue = rand.NextDouble(20, 45);
                }
                else if (hueRand > .45)
                {
                    // Yellow-Orange
                    hue = rand.NextDouble(45, 60);
                }
                else
                {
                    // Blues
                    hue = rand.NextDouble(196, 254);
                }

                RadialGradientBrush retVal = new RadialGradientBrush()
                {
                    RadiusX = .5,
                    RadiusY = .5,
                    Center = new Point(.5, .5),
                    GradientOrigin = new Point(.5, .5),
                };

                //TODO: Randomize these stops slightly
                retVal.GradientStops.Add(new GradientStop(new ColorHSV(hue, 2, 84).ToRGB(), 0));
                retVal.GradientStops.Add(new GradientStop(new ColorHSV(240, hue, 21, 91).ToRGB(), 0.450628));
                //retVal.GradientStops.Add(new GradientStop(new ColorHSV(193, hue, 96, 76).ToRGB(), 0.594255));
                //retVal.GradientStops.Add(new GradientStop(new ColorHSV(16, hue, 96, 76).ToRGB(), 1));
                retVal.GradientStops.Add(new GradientStop(new ColorHSV(46, hue, 96, 76).ToRGB(), 0.594255));
                retVal.GradientStops.Add(new GradientStop(new ColorHSV(0, hue, 96, 76).ToRGB(), 1));

                return retVal;
            }

            /// <summary>
            /// Bilinear interpolation
            /// </summary>
            /// <remarks>
            /// http://en.wikipedia.org/wiki/Bilinear_interpolation
            /// </remarks>
            /// <param name="ul">Upper Left</param>
            /// <param name="ur">Upper Right</param>
            /// <param name="bl">Bottom Left</param>
            /// <param name="br">Bottom Right</param>
            /// <param name="pos">The position within the box (values are always between 0 and 1)</param>
            private static Vector LERP(Vector ul, Vector ur, Vector bl, Vector br, double percentX, double percentY)
            {
                Vector upper = Math2D.LERP(ul, ur, percentX);
                Vector bottom = Math2D.LERP(bl, br, percentX);

                return Math2D.LERP(upper, bottom, percentY);
            }

            #endregion
        }

        #endregion

        #region Declaration Section

        private const string TITLE = "BackImageManager";

        private readonly Canvas _canvas;
        private readonly Player _player;

        private StarfieldVisual1 _visual = null;
        private Tuple<StarfieldVisual1, double>[] _visuals1 = null;
        private Tuple<StarfieldVisual2, double>[] _visuals2 = null;

        #endregion

        #region Constructor

        public BackImageManager(Canvas canvas, Player player)
        {
            _canvas = canvas;
            _player = player;

            _canvas.SizeChanged += Canvas_SizeChanged;
        }

        #endregion

        #region Public Methods

        public void Update_SINGLE()
        {
            //const double SIZE = 800;
            const double SIZE = 3000;
            const double HALF = SIZE / 2;

            if (_visual == null)
            {
                #region Create

                _visual = new StarfieldVisual1(SIZE, 10, 1, 1);

                Canvas.SetLeft(_visual, (_canvas.ActualWidth / 2) - HALF);
                Canvas.SetTop(_visual, (_canvas.ActualHeight / 2) - HALF);

                _canvas.Children.Add(_visual);

                #endregion
            }

            TransformGroup transform = new TransformGroup();

            #region Translate

            Point3D position = _player.Ship.PositionWorld;

            transform.Children.Add(new TranslateTransform(-position.X, position.Y));        // don't need to negate Y, because it's already backward

            #endregion
            #region Rotate

            Vector3D desiredUp = new Vector3D(0, -1, 0);
            Vector3D up = _player.Ship.PhysicsBody.DirectionToWorld(desiredUp);

            double angle = Vector.AngleBetween(desiredUp.ToVector2D(), up.ToVector2D());

            transform.Children.Add(new RotateTransform(angle, HALF, HALF));

            #endregion

            _visual.RenderTransform = transform;
        }
        public void Update_MULTISAME()
        {
            //const double SIZE = 800;
            const double SIZE = 3000;
            const double HALF = SIZE / 2;

            if (_visuals1 == null)
            {
                #region Create

                _visuals1 = new Tuple<StarfieldVisual1, double>[5];

                for (int cntr = 0; cntr < _visuals1.Length; cntr++)
                {
                    double starSizeMult = UtilityCore.GetScaledValue(.7, 1.1, 0, _visuals1.Length, cntr);
                    double slideSpeed = UtilityCore.GetScaledValue(.75, 5, 0, _visuals1.Length, cntr);

                    _visuals1[cntr] = Tuple.Create(new StarfieldVisual1(SIZE, 10, _visuals1.Length, starSizeMult), slideSpeed);

                    Canvas.SetLeft(_visuals1[cntr].Item1, (_canvas.ActualWidth / 2) - HALF);
                    Canvas.SetTop(_visuals1[cntr].Item1, (_canvas.ActualHeight / 2) - HALF);

                    // This kills the framerate
                    //_visuals[cntr].Item1.Effect = new BlurEffect()
                    //{
                    //    Radius = 2,
                    //};

                    _canvas.Children.Add(_visuals1[cntr].Item1);
                }

                #endregion
            }

            // Figure out angle
            Vector3D desiredUp = new Vector3D(0, -1, 0);
            Vector3D up = _player.Ship.PhysicsBody.DirectionToWorld(desiredUp);

            double angle = Vector.AngleBetween(desiredUp.ToVector2D(), up.ToVector2D());

            for (int cntr = 0; cntr < _visuals1.Length; cntr++)
            {
                TransformGroup transform = new TransformGroup();

                #region Translate

                Point3D position = _player.Ship.PositionWorld;

                transform.Children.Add(new TranslateTransform(-position.X * _visuals1[cntr].Item2, position.Y * _visuals1[cntr].Item2));        // don't need to negate Y, because it's already backward

                #endregion
                #region Rotate

                transform.Children.Add(new RotateTransform(angle, HALF, HALF));

                #endregion

                _visuals1[cntr].Item1.RenderTransform = transform;
            }
        }
        public void Update()
        {
            //const double DENSITY = .002;
            const double DENSITY = .004;

            //const double SIZE = 800;
            const double SIZE = 3000;
            const double HALF = SIZE / 2;
            const double MARGIN = 10;

            if (_visuals2 == null)
            {
                #region Create

                _visuals2 = new Tuple<StarfieldVisual2, double>[5];

                //NOTE: Playing around with different values.  8,60 looks good with 500x500, but isn't enough for 3000x3000
                //Tuple<Vector[], int> vectorField = GetVectorField(8, 60);
                Tuple<Vector[], int> vectorField = GetVectorField(30, 500);

                for (int cntr = 0; cntr < _visuals2.Length; cntr++)
                {
                    double starSizeMult = UtilityCore.GetScaledValue(.7, 1.1, 0, _visuals2.Length, cntr);
                    double slideSpeed = UtilityCore.GetScaledValue(.75, 5, 0, _visuals2.Length, cntr);


                    bool isComplexLayer = cntr == 2;

                    double density = isComplexLayer ? DENSITY * .95 : DENSITY * .05;
                    int numStars = Convert.ToInt32((density / _visuals2.Length) * SIZE * SIZE);


                    if (isComplexLayer)
                    {
                        _visuals2[cntr] = Tuple.Create(new StarfieldVisual2(SIZE, MARGIN, numStars, starSizeMult, vectorField), slideSpeed);
                    }
                    else
                    {
                        _visuals2[cntr] = Tuple.Create(new StarfieldVisual2(SIZE, MARGIN, numStars, starSizeMult), slideSpeed);
                    }

                    Canvas.SetLeft(_visuals2[cntr].Item1, (_canvas.ActualWidth / 2) - HALF);
                    Canvas.SetTop(_visuals2[cntr].Item1, (_canvas.ActualHeight / 2) - HALF);

                    _canvas.Children.Add(_visuals2[cntr].Item1);
                }

                #endregion
            }

            // Figure out angle
            Vector3D desiredUp = new Vector3D(0, -1, 0);
            Vector3D up = _player.Ship.PhysicsBody.DirectionToWorld(desiredUp);

            double angle = Vector.AngleBetween(desiredUp.ToVector2D(), up.ToVector2D());

            for (int cntr = 0; cntr < _visuals2.Length; cntr++)
            {
                TransformGroup transform = new TransformGroup();

                #region Translate

                Point3D position = _player.Ship.PositionWorld;

                transform.Children.Add(new TranslateTransform(-position.X * _visuals2[cntr].Item2, position.Y * _visuals2[cntr].Item2));        // don't need to negate Y, because it's already backward

                #endregion
                #region Rotate

                transform.Children.Add(new RotateTransform(angle, HALF, HALF));

                #endregion

                _visuals2[cntr].Item1.RenderTransform = transform;
            }
        }

        #endregion

        #region Event Listeners

        private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                if (_visuals2 == null)
                {
                    return;
                }

                double canvasHalfWidth = _canvas.ActualWidth / 2;
                double canvasHalfHeight = _canvas.ActualHeight / 2;

                foreach (var visual in _visuals2)
                {
                    double visualHalf = visual.Item1.Size / 2;

                    Canvas.SetLeft(visual.Item1, canvasHalfWidth - visualHalf);
                    Canvas.SetTop(visual.Item1, canvasHalfHeight - visualHalf);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private static Tuple<Vector[], int> GetVectorField(int size, double vectorLen)
        {
            Vector[] field = Enumerable.Range(0, size * size).
                Select(o => Math3D.GetRandomVector_Circular(vectorLen).ToVector2D()).
                ToArray();

            return Tuple.Create(field, size);
        }

        #endregion
    }
}
