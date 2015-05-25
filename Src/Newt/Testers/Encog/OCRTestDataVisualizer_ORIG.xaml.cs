using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;
using Game.HelperClassesAI;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;

namespace Game.Newt.Testers.Encog
{
    //Instead of straight spring forces, give an option for t-SNE
    //http://colah.github.io/posts/2014-10-Visualizing-MNIST/
    //http://colah.github.io/posts/2015-01-Visualizing-Representations/

    //http://lvdmaaten.github.io/tsne/
    //https://github.com/lejon/T-SNE-Java/tree/master/tsne-core/src/main/java/com/jujutsu/tsne


    // Have a way to show how many images are at/near each pure point
    //
    // When they click one of those points, show a bubble, and inside use a t-SNE on the imput's coords

    public partial class OCRTestDataVisualizer_ORIG : Window
    {
        #region Class: SketchSample

        private class SketchSample
        {
            public readonly long Token = TokenGenerator.NextToken();

            public double[] NNInput { get; set; }
            public double[] NNOutput { get; set; }

            public bool IsMatch { get; set; }
            public ColorHSV Color { get; set; }

            public Point3D Position { get; set; }

            public UIElement Image_Large { get; set; }
            public UIElement Image_Small { get; set; }

            public TranslateTransform3D Translate_3DDot { get; set; }
        }

        #endregion
        #region Class: SketchDots

        private class SketchDots
        {
            public SketchSample[] Sketches { get; set; }

            // These are the distance between each sketch
            public Tuple<int, int, double>[] Distances_Input = null;
            public Tuple<int, int, double>[] Distances_Output = null;

            public Visual3D Visual { get; set; }
        }

        #endregion
        #region Class: ClickedBubble

        private class ClickedBubble
        {
            public Point3D Point { get; set; }
            public double Radius { get; set; }

            public int[] Inside { get; set; }
            public int[] Outside { get; set; }

            public Visual3D Visual { get; set; }
        }

        #endregion

        #region Declaration Section

        private const double AXIS_HALF = .6;
        private const double AXIS_RADIUS = .002;

        const double CIRCLE_MAJORRADIUS = .6;
        const double CIRCLE_MINORRADIUS = .001;

        const double SKETCHSIZE_LARGE = 200;
        const double SKETCHSIZE_SMALL = 100;

        private TrainedNework_Simple _network = null;
        private EncogOCR_SketchData[] _inputSketches = null;
        private List<double[]> _additionalSketches = null;

        /// <summary>
        /// These are the sketches/dots that are currently showing
        /// </summary>
        private SketchDots _sketches = null;

        private ClickedBubble _bubble = null;

        private double[] _hues = null;

        /// <summary>
        /// This listens to the mouse/keyboard and controls the camera
        /// </summary>
        private TrackBallRoam _trackball = null;

        private static ThreadLocal<SolidColorBrush[]> _grayBrushes_WhiteBlack = new ThreadLocal<SolidColorBrush[]>(() => Enumerable.Range(0, 256).Select(o => Convert.ToByte(255 - o)).Select(o => new SolidColorBrush(Color.FromRgb(o, o, o))).ToArray());
        private static ThreadLocal<SolidColorBrush[]> _grayBrushes_TransparentBlack = new ThreadLocal<SolidColorBrush[]>(() => Enumerable.Range(0, 256).Select(o => Convert.ToByte(o)).Select(o => new SolidColorBrush(Color.FromArgb(o, 0, 0, 0))).ToArray());
        private static ThreadLocal<SolidColorBrush> _imageBackBrush = new ThreadLocal<SolidColorBrush>(() => new SolidColorBrush(UtilityWPF.ColorFromHex("40FFFFFF")));

        //private Visual3D _axisVisual = null;
        //private Visual3D _colorWheelVisual = null;

        private int? _mouseOverIndex = null;

        private readonly DispatcherTimer _timer;

        #endregion

        #region Constructor

        public OCRTestDataVisualizer_ORIG()
        {
            InitializeComponent();

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
        }

        #endregion

        #region Public Methods

        //TODO: If they are only adding a new training sketch, then add to existing instead of repositioning everything (but do reavaluate all outputs, because the network is different)
        public void VisualizeThis(TrainedNework_Simple network, IEnumerable<EncogOCR_SketchData> sketches)
        {
            panelLegend.Children.Clear();

            //_viewport.Children.RemoveAll()
            //_viewport.Children.Remove(_colorWheelVisual);
            if (_sketches != null)
            {
                _viewport.Children.Remove(_sketches.Visual);
            }

            _network = network;

            if (sketches == null)
            {
                _inputSketches = null;
            }
            else
            {
                _inputSketches = sketches.ToArray();
            }

            if (_network == null || _inputSketches == null || _inputSketches.Length == 0)
            {
                return;
            }

            _hues = GetHues(_network.Outputs.Length);

            // Make a 2D legend that shows color of each name
            BuildLegend(_network.Outputs, _hues);

            //_colorWheelVisual = GetColorWheel(_hues.Item1, _hues.Item2);
            //_viewport.Children.Add(_colorWheelVisual);

            _sketches = TestSamples(_network, _inputSketches, _hues);
            _viewport.Children.Add(_sketches.Visual);
        }

        public void AddSample(double[] sketch)
        {
            //_additionalSketches.Add(sketch);






            //TODO: Only keep if this is the same size as what the network is built for

            //TODO: Run this against the network, and plot the point

            //_userDrawn.Add(   );

        }

        #endregion

        #region Event Listeners

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                //TODO: Have a checkbox to show/hide this
                //_axisVisual = GetAxisVisual();
                //_viewport.Children.Add(_axisVisual);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void grdViewPort_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                var hitResult = GetMouseOver(e);
                int? index = hitResult.Item1;

                //NOTE: This mousemove event will fire again if the region the mouse is over gets invalidated, so this if statement stops that
                if (index != null && _mouseOverIndex != null && index.Value == _mouseOverIndex.Value)
                {
                    return;
                }

                _mouseOverIndex = index;

                canvas.Children.Clear();

                if (index == null)
                {
                    return;
                }

                Point position = new Point(canvas.ActualWidth / 2, canvas.ActualHeight / 2);
                ShowImage(index.Value, position, true);

                // Find nearby images
                var nearby = GetNearbySketches(_sketches.Sketches[index.Value], _sketches.Sketches, .1);

                if (nearby.Length > 0)
                {
                    ShowNearbySketches(nearby, _sketches.Sketches[index.Value].Position, hitResult.Item2, position);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void grdViewPort_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //TODO: Calculate forces in another thread.  Build up the next X frames
        private void Timer_Tick(object sender, EventArgs e)
        {
            const double MULT = .005;
            const double MAXSPEED = .05;
            const double MAXSPEED_SQUARED = MAXSPEED * MAXSPEED;

            if (_sketches == null)
            {
                return;
            }

            Vector3D[] forces = GetForces(_sketches, chkOutput.IsChecked.Value, MULT);

            // Cap the speed
            forces = forces.
                Select(o =>
                {
                    if (o.LengthSquared < MAXSPEED_SQUARED)
                    {
                        return o;
                    }
                    else
                    {
                        return o.ToUnit() * MAXSPEED;
                    }
                }).
                ToArray();

            for (int cntr = 0; cntr < forces.Length; cntr++)
            {
                _sketches.Sketches[cntr].Position += forces[cntr];
                _sketches.Sketches[cntr].Translate_3DDot.OffsetX = _sketches.Sketches[cntr].Position.X + forces[cntr].X;
                _sketches.Sketches[cntr].Translate_3DDot.OffsetY = _sketches.Sketches[cntr].Position.Y + forces[cntr].Y;
                _sketches.Sketches[cntr].Translate_3DDot.OffsetZ = _sketches.Sketches[cntr].Position.Z + forces[cntr].Z;
            }
        }

        #endregion

        #region Private Methods

        private Tuple<int?, RayHitTestParameters> GetMouseOver(MouseEventArgs e)
        {
            const double MAXHITDISTANCE = .035;

            // Fire a ray from the mouse point
            Point mousePoint = e.GetPosition(grdViewPort);
            var ray = UtilityWPF.RayFromViewportPoint(_camera, _viewport, mousePoint);

            var hit = _sketches.Sketches.
                Select((o, i) => new
                {
                    Index = i,
                    Position = o.Position,
                    Dot = Vector3D.DotProduct(ray.Direction, o.Position - ray.Origin)
                }).
                Where(o => o.Dot > 0).      // throwing out points that are behind the camera
                Select(o => new
                {
                    o.Index,
                    o.Position,
                    Distance = Math3D.GetClosestDistance_Line_Point(ray.Origin, ray.Direction, o.Position)
                }).
                Where(o => o.Distance <= MAXHITDISTANCE).
                OrderBy(o => (o.Position - ray.Origin).LengthSquared).
                FirstOrDefault();

            // Sooooon :)
            //hit?.Index;
            int? hitIndex = hit == null ? (int?)null : hit.Index;
            return Tuple.Create(hitIndex, ray);
        }

        private static Vector3D[] GetForces(SketchDots sketches, bool useOutput, double mult)
        {
            // Give them a very slight pull toward the origin so that the cloud doesn't drift away
            Point3D center = Math3D.GetCenter(sketches.Sketches.Select(o => o.Position));
            double centerMult = mult * -5;

            Vector3D centerPullForce = center.ToVector() * centerMult;

            Vector3D[] retVal = Enumerable.Range(0, sketches.Sketches.Length).
                Select(o => centerPullForce).
                ToArray();

            // Figure out which set of distances to use
            var distances = useOutput ? sketches.Distances_Output : sketches.Distances_Input;

            foreach (var link in distances)
            {
                // Spring from 1 to 2
                Vector3D spring = sketches.Sketches[link.Item2].Position - sketches.Sketches[link.Item1].Position;
                double springLength = spring.Length;

                double difference = link.Item3 - springLength;
                difference *= mult;

                if (Math3D.IsNearZero(springLength))
                {
                    spring = Math3D.GetRandomVector_Spherical_Shell(Math.Abs(difference));
                }
                else
                {
                    spring = spring.ToUnit() * Math.Abs(difference);
                }

                if (difference > 0)
                {
                    // Gap needs to be bigger, push them away
                    retVal[link.Item1] -= spring;
                    retVal[link.Item2] += spring;
                }
                else if (difference < 0)
                {
                    // Close the gap
                    retVal[link.Item1] += spring;
                    retVal[link.Item2] -= spring;
                }
            }

            return retVal;
        }

        private void BuildLegend(string[] names, double[] hues)
        {
            if (names.Length != hues.Length)
            {
                throw new ApplicationException("The arrays need to be the same size");
            }

            panelLegend.Children.Clear();

            for (int cntr = 0; cntr < names.Length; cntr++)
            {
                TextBlock text = new TextBlock()
                {
                    //Foreground = new SolidColorBrush(new ColorHSV(hues[cntr], 100, 100).ToRGB()),
                    Foreground = Brushes.White,
                    FontSize = 14,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Text = names[cntr],
                };

                Border border = new Border()
                {
                    CornerRadius = new CornerRadius(2),
                    BorderThickness = new Thickness(1),
                    Background = new SolidColorBrush(new ColorHSV(hues[cntr], 25, 50).ToRGB()),
                    BorderBrush = new SolidColorBrush(new ColorHSV(hues[cntr], 100, 100).ToRGB()),
                    Padding = new Thickness(4, 2, 4, 2),
                    Margin = new Thickness(3),
                    Child = text,
                };

                panelLegend.Children.Add(border);
            }
        }

        private static SketchDots TestSamples(TrainedNework_Simple network, EncogOCR_SketchData[] inputSketches, double[] hues)
        {
            // Run a bunch of test images through the NN
            //NOTE: Only .NNInput and .NNOutput props are populated
            SketchSample[] sketches = TestSamples_InputOutput(network, inputSketches);

            // Build a visual with each sketch as a dot
            Visual3D visual = TestSamples_Draw(sketches, hues);

            // Calculate the forces between each sketch
            Tuple<int, int, double>[] distance_Input, distance_Output;
            TestSamples_Distance(out distance_Input, out distance_Output, sketches);

            return new SketchDots()
            {
                Sketches = sketches,
                Visual = visual,
                Distances_Input = distance_Input,
                Distances_Output = distance_Output,
            };
        }
        private static SketchSample[] TestSamples_InputOutput(TrainedNework_Simple network, EncogOCR_SketchData[] inputSketches)
        {
            List<SketchSample> retVal = new List<SketchSample>();

            int inputSize = network.Network.InputCount;
            int outputSize = network.Network.OutputCount;

            // Inputs
            foreach (EncogOCR_SketchData input in inputSketches)
            {
                double[] output = new double[outputSize];
                network.Network.Compute(input.NNInput, output);

                retVal.Add(new SketchSample() { NNInput = input.NNInput, NNOutput = output });
            }

            // Solid Colors
            for (int cntr = 0; cntr <= 16; cntr++)
            {
                double val = cntr / 16d;

                double[] input = Enumerable.Range(0, inputSize).Select(o => val).ToArray();
                double[] output = new double[outputSize];

                network.Network.Compute(input, output);

                retVal.Add(new SketchSample() { NNInput = input, NNOutput = output });
            }

            //TODO: Generate random brush stroke images
            //TODO: Generate random pixelated images

            return retVal.ToArray();
        }
        private static Visual3D TestSamples_Draw(SketchSample[] sketches, double[] hues)
        {
            const double RADIUS = .5;
            const double DOTRADIUS = .035;

            #region Materials

            SpecularMaterial specular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("50FFFFFF")), 2);

            var material_Color = hues.Select(o =>
                {
                    ColorHSV color = new ColorHSV(o, 75, 75);

                    MaterialGroup material = new MaterialGroup();
                    material.Children.Add(new DiffuseMaterial(new SolidColorBrush(color.ToRGB())));
                    material.Children.Add(specular);

                    return new { Color = color, Material = material };
                }).ToArray();

            ColorHSV color_Gray = new ColorHSV(0, 0, 50);
            MaterialGroup material_Gray = new MaterialGroup();
            material_Gray.Children.Add(new DiffuseMaterial(new SolidColorBrush(color_Gray.ToRGB())));
            material_Gray.Children.Add(specular);

            #endregion

            Model3DGroup group = new Model3DGroup();

            foreach (SketchSample sketch in sketches)
            {
                int? matchIndex = UtilityEncog.IsMatch(sketch.NNOutput);

                Material material;
                if (matchIndex == null)
                {
                    sketch.IsMatch = false;
                    sketch.Color = color_Gray;
                    material = material_Gray;
                }
                else
                {
                    sketch.IsMatch = true;
                    sketch.Color = material_Color[matchIndex.Value].Color;
                    material = material_Color[matchIndex.Value].Material;
                }

                sketch.Position = Math3D.GetRandomVector_Spherical(RADIUS).ToPoint();
                sketch.Translate_3DDot = new TranslateTransform3D(sketch.Position.ToVector());

                // Geometry Model
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = material;
                geometry.BackMaterial = material;
                geometry.Geometry = UtilityWPF.GetSphere_Ico(DOTRADIUS, 1, true);

                geometry.Transform = sketch.Translate_3DDot;

                group.Children.Add(geometry);
            }

            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = group;
            return visual;
        }
        private static void TestSamples_Distance(out Tuple<int, int, double>[] distance_Input, out Tuple<int, int, double>[] distance_Output, SketchSample[] sketches)
        {
            #region Get Distances

            var inputs = new List<Tuple<int, int, double>>();
            var outputs = new List<Tuple<int, int, double>>();

            for (int outer = 0; outer < sketches.Length - 1; outer++)
            {
                for (int inner = outer + 1; inner < sketches.Length; inner++)
                {
                    double distance = Math3D.GetDistance(sketches[outer].NNInput, sketches[inner].NNInput);
                    inputs.Add(Tuple.Create(outer, inner, distance));

                    distance = Math3D.GetDistance(sketches[outer].NNOutput, sketches[inner].NNOutput);
                    outputs.Add(Tuple.Create(outer, inner, distance));
                }
            }

            distance_Input = inputs.ToArray();
            distance_Output = outputs.ToArray();

            #endregion

            if (distance_Input.Length == 0)
            {
                return;
            }

            //#region Normalize

            //double inputMax = distance_Input.Max(o => o.Item3);
            //double outputMax = distance_Output.Max(o => o.Item3);

            //distance_Input = distance_Input.
            //    Select(o => Tuple.Create(o.Item1, o.Item2, o.Item3 / inputMax)).
            //    ToArray();

            //distance_Output = distance_Output.
            //    Select(o => Tuple.Create(o.Item1, o.Item2, o.Item3 / inputMax)).
            //    ToArray();

            //#endregion
        }

        private void ShowNearbySketches(SketchSample[] nearby, Point3D center3D, RayHitTestParameters cameraRay, Point center2D)
        {
            // Get a plane that is perpendicular to the look ray
            ITriangle plane = Math3D.GetPlane(center3D, cameraRay.Direction);

            // Project the points onto that plane
            var nearbyOnPlane = nearby.
                Select(o => new { Sketch = o, PlanePoint = Math3D.GetClosestPoint_Plane_Point(plane, o.Position) }).
                ToArray();

            RotateTransform3D rotate = new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRotation(new DoubleVector(cameraRay.Direction, _camera.UpDirection), new DoubleVector(new Vector3D(0, 0, -1), new Vector3D(0, 1, 0)))));

            // Lay these images down along the directions of the projected points
            // nearby is sorted by distance from the center image

            #region Attempt1

            double halfLarge = SKETCHSIZE_LARGE / 2;
            double halfSmall = SKETCHSIZE_SMALL / 2;

            double stepDist = SKETCHSIZE_SMALL * .05;

            int startIncrement = Convert.ToInt32(Math.Floor((halfSmall + halfLarge) / stepDist));

            List<Rect> existing = new List<Rect>();
            existing.Add(new Rect(center2D.X - halfLarge, center2D.Y - halfLarge, SKETCHSIZE_LARGE, SKETCHSIZE_LARGE));

            foreach (var nextSketch in nearbyOnPlane)
            {
                Vector direction = rotate.Transform(nextSketch.PlanePoint - center3D).ToVector2D();

                Vector dirUnit = direction.ToUnit();
                if (Math2D.IsInvalid(dirUnit))
                {
                    dirUnit = Math3D.GetRandomVector_Circular_Shell(1).ToVector2D();
                }
                dirUnit = new Vector(dirUnit.X, -dirUnit.Y);

                Point point = new Point();
                Rect rect = new Rect();

                for (int cntr = startIncrement; cntr < 1000; cntr++)
                {
                    point = center2D + (dirUnit * (stepDist * cntr));
                    rect = new Rect(point.X - halfSmall, point.Y - halfSmall, SKETCHSIZE_SMALL, SKETCHSIZE_SMALL);

                    if (!existing.Any(o => o.IntersectsWith(rect)))
                    {
                        break;
                    }
                }

                existing.Add(rect);

                //ShowImage(nextSketch.Sketch, center2D + (dirUnit * 100), false);
                ShowImage(nextSketch.Sketch, point, false);
            }

            #endregion
        }

        private void ShowImage(int sketchIndex, Point position, bool isLarge)
        {
            ShowImage(_sketches.Sketches[sketchIndex], position, isLarge);
        }
        private void ShowImage(SketchSample sketch, Point position, bool isLarge)
        {
            double size = isLarge ? SKETCHSIZE_LARGE : SKETCHSIZE_SMALL;

            #region Get image

            UIElement image = isLarge ? sketch.Image_Large : sketch.Image_Small;
            if (image == null)
            {
                var bitmap = GetBitmap(sketch.NNInput);

                Color? borderColor = null;
                if (sketch.IsMatch)
                {
                    borderColor = sketch.Color.ToRGB();
                }

                image = GetImageCtrl(bitmap, size, borderColor);

                if (isLarge)
                {
                    sketch.Image_Large = image;
                }
                else
                {
                    sketch.Image_Small = image;
                }
            }

            #endregion

            double halfSize = size / 2;

            Canvas.SetLeft(image, position.X - halfSize);
            Canvas.SetTop(image, position.Y - halfSize);
            canvas.Children.Add(image);
        }

        private static UIElement GetImageCtrl(BitmapSource bitmap, double size, Color? borderColor = null)
        {
            Image imageCtrl = new Image()
            {
                Width = size,
                Height = size,
                Stretch = Stretch.Fill,
                Source = bitmap,
            };

            Border borderCtrl = new Border()
            {
                Background = _imageBackBrush.Value,
                Child = imageCtrl,
            };

            if (borderColor != null)
            {
                borderCtrl.BorderBrush = new SolidColorBrush(Color.FromArgb(192, borderColor.Value.R, borderColor.Value.G, borderColor.Value.B));
                borderCtrl.BorderThickness = new Thickness(2);
                borderCtrl.CornerRadius = new CornerRadius(2);
            }

            return borderCtrl;
        }

        private static BitmapSource GetBitmap(double[] input)
        {
            int size = Convert.ToInt32(Math.Sqrt(input.Length));
            if (size * size != input.Length)
            {
                throw new ArgumentException("Must pass in a square image");
            }

            RenderTargetBitmap retVal = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);

            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext ctx = dv.RenderOpen())
            {
                int index = 0;

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        int colorIndex = Convert.ToInt32(Math.Round(input[index] * 255));

                        ctx.DrawRectangle(_grayBrushes_TransparentBlack.Value[colorIndex], null, new Rect(x, y, 1, 1));

                        index++;
                    }
                }
            }

            retVal.Render(dv);

            return retVal;
        }
        private static BitmapSource GetBitmap_COLOR(double[] input)
        {
            int size = Convert.ToInt32(Math.Sqrt(input.Length));
            if (size * size != input.Length)
            {
                throw new ArgumentException("Must pass in a square image");
            }

            RenderTargetBitmap retVal = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);

            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext ctx = dv.RenderOpen())
            {
                ctx.DrawRectangle(new SolidColorBrush(UtilityWPF.GetRandomColor(64, 0, 255)), null, new Rect(0, 0, size, size));
            }

            retVal.Render(dv);

            return retVal;
        }

        private static SketchSample[] GetNearbySketches(SketchSample sketch, SketchSample[] all, double radius)
        {
            double radiusSquared = radius * radius;

            return all.
                Where(o => o.Token != sketch.Token).
                Select(o => new { Sketch = o, DistSquared = (o.Position - sketch.Position).LengthSquared }).
                Where(o => o.DistSquared <= radiusSquared).
                OrderBy(o => o.DistSquared).
                Select(o => o.Sketch).
                ToArray();
        }

        private static double[] GetHues(int numItems)
        {
            if (numItems <= 0)
            {
                throw new ArgumentOutOfRangeException("numItems must be 1 or greater");
            }
            else if (numItems == 1)
            {
                return new[] { 0d };
            }

            double angle = 360d / numItems;

            return Enumerable.Range(0, numItems).
                Select(o => o * angle).
                ToArray();
        }

        private static Visual3D GetAxisVisual()
        {
            const double AXIS_QUARTER = AXIS_HALF / 2;
            const int AXIS_SEGMENTS = 15;

            Model3DGroup group = new Model3DGroup();

            #region Axis Up

            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(UtilityWPF.GetUnlitMaterial(UtilityWPF.ColorFromHex("202020")));

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;
            geometry.Geometry = UtilityWPF.GetCone_AlongX(AXIS_SEGMENTS, AXIS_RADIUS, AXIS_HALF);

            Transform3DGroup transform = new Transform3DGroup();
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), -90)));
            transform.Children.Add(new TranslateTransform3D(0, 0, AXIS_QUARTER));
            geometry.Transform = transform;

            group.Children.Add(geometry);

            #endregion
            #region Axis Down

            // Material
            materials = new MaterialGroup();
            materials.Children.Add(UtilityWPF.GetUnlitMaterial(UtilityWPF.ColorFromHex("F0F0F0")));

            // Geometry Model
            geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;
            geometry.Geometry = UtilityWPF.GetCone_AlongX(AXIS_SEGMENTS, AXIS_RADIUS, AXIS_HALF);

            transform = new Transform3DGroup();
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90)));
            transform.Children.Add(new TranslateTransform3D(0, 0, -AXIS_QUARTER));
            geometry.Transform = transform;

            group.Children.Add(geometry);

            #endregion

            #region Circle

            // Material
            materials = new MaterialGroup();
            materials.Children.Add(UtilityWPF.GetUnlitMaterial(UtilityWPF.ColorFromHex("999")));

            // Geometry Model
            geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;
            geometry.Geometry = UtilityWPF.GetTorus(70, 10, CIRCLE_MINORRADIUS, CIRCLE_MAJORRADIUS);

            group.Children.Add(geometry);

            #endregion

            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = group;
            return visual;
        }
        private static Visual3D GetColorWheel(double[] hues, double stepAngle)
        {
            //const double CONE_RADIUS = CIRCLE_MINORRADIUS * .6667;
            const double CONE_RADIUS = CIRCLE_MINORRADIUS * 1.5;
            const double CONE_HEIGHT1 = CIRCLE_MAJORRADIUS * .25;
            const double CONE_HEIGHT2 = CIRCLE_MAJORRADIUS * .1;
            const int SEGMENTS = 8;

            Model3DGroup group = new Model3DGroup();

            for (int cntr = 0; cntr < hues.Length; cntr++)
            {
                #region In facing cone

                // Material
                MaterialGroup materials = new MaterialGroup();
                materials.Children.Add(UtilityWPF.GetUnlitMaterial(new ColorHSV(hues[cntr], 75, 75).ToRGB()));

                // Geometry Model
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = materials;
                geometry.BackMaterial = materials;
                geometry.Geometry = UtilityWPF.GetCone_AlongX(SEGMENTS, CONE_RADIUS, CONE_HEIGHT1);

                Transform3DGroup transform = new Transform3DGroup();
                transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 180)));
                transform.Children.Add(new TranslateTransform3D(CIRCLE_MAJORRADIUS - (CONE_HEIGHT1 / 2), 0, 0));
                transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), stepAngle * cntr)));
                geometry.Transform = transform;

                group.Children.Add(geometry);

                #endregion
                #region Out facing cone

                // Geometry Model
                geometry = new GeometryModel3D();
                geometry.Material = materials;
                geometry.BackMaterial = materials;
                geometry.Geometry = UtilityWPF.GetCone_AlongX(SEGMENTS, CONE_RADIUS, CONE_HEIGHT2);

                transform = new Transform3DGroup();
                transform.Children.Add(new TranslateTransform3D(CIRCLE_MAJORRADIUS + (CONE_HEIGHT2 / 2), 0, 0));
                transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), stepAngle * cntr)));
                geometry.Transform = transform;

                group.Children.Add(geometry);

                #endregion
            }

            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = group;
            return visual;
        }

        #endregion
    }
}
