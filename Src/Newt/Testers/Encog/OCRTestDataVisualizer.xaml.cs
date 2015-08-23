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

    public partial class OCRTestDataVisualizer : Window
    {
        #region Class: Network

        private class Network
        {
            public Network(TrainedNework_Simple network)
            {
                this.Net = network;
            }

            public readonly TrainedNework_Simple Net;       // can't be same name as the class, couldn't think of a better name (don't want to use Value)

            //TODO: Have a control that shows a map the the network's input/hidden/output layers
        }

        #endregion
        #region Class: NetworkInputs

        private class NetworkInputs
        {
            public NetworkInputs(int size)
            {
                this.Size = size;

                int imageSize = Convert.ToInt32(Math.Sqrt(size));
                if (imageSize * imageSize != size)
                {
                    throw new ArgumentException("Must pass in a square image");
                }

                this.ImageHeightWidth = imageSize;
            }

            /// <summary>
            /// This is how many neurons there are in the input layer (image size squared)
            /// </summary>
            public readonly int Size;

            /// <summary>
            /// This is the height and width of a sketch
            /// </summary>
            public readonly int ImageHeightWidth;

            //TODO: Generate some raster images

        }

        #endregion
        #region Class: NetworkOutputs

        private class NetworkOutputs
        {
            #region Constructor

            public NetworkOutputs(string[] names)
            {
                this.Size = names.Length;
                this.Names = names;

                this.Hues = CreateHues(this.Size);

                #region Materials

                SpecularMaterial specular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("50FFFFFF")), 2);

                // Colors
                this.DotColors = this.Hues.Select(o =>
                {
                    ColorHSV color = new ColorHSV(o, 75, 75);

                    MaterialGroup material = new MaterialGroup();
                    material.Children.Add(new DiffuseMaterial(new SolidColorBrush(color.ToRGB())));
                    material.Children.Add(specular);

                    return new Tuple<ColorHSV, Material>(color, material);
                }).ToArray();

                // Gray
                this.ColorGray = new ColorHSV(0, 0, 50);

                MaterialGroup material_Gray = new MaterialGroup();
                material_Gray.Children.Add(new DiffuseMaterial(new SolidColorBrush(this.ColorGray.ToRGB())));
                material_Gray.Children.Add(specular);

                this.DotGray = material_Gray;

                #endregion
            }

            #endregion

            public readonly int Size;

            public readonly string[] Names;

            public readonly double[] Hues;

            public readonly Tuple<ColorHSV, Material>[] DotColors;
            public readonly ColorHSV ColorGray;
            public readonly Material DotGray;

            #region Public Methods

            public bool IsSame(string[] names)
            {
                if (names.Length != this.Names.Length)
                {
                    return false;
                }

                return Enumerable.Range(0, names.Length).
                    All(o => this.Names[o].Equals(names[o], StringComparison.Ordinal));
            }

            #endregion

            #region Private Methods

            private static double[] CreateHues(int numItems)
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

            #endregion
        }

        #endregion

        #region Class: Dot

        private class Dot
        {
            public Dot(long token)
            {
                this.Token = token;
            }

            public readonly long Token;

            public double[] NNInput { get; set; }
            public double[] NNOutput { get; set; }

            public bool IsMatch { get; set; }
            public ColorHSV Color { get; set; }

            public Point3D Position { get; set; }

            public Tuple<UIElement, Image> Image { get; set; }

            public GeometryModel3D Geometry_3DDot { get; set; }
            public TranslateTransform3D Translate_3DDot { get; set; }

            /// <summary>
            /// This will only be populated if the dot came from a vector sketch
            /// </summary>
            public EncogOCR_SketchData VectorSource { get; set; }
        }

        #endregion
        #region Class: DotVisuals

        private class DotVisuals
        {
            public DotVisuals()
            {
                this.ModelGroup = new Model3DGroup();
                this.Visual = new ModelVisual3D()
                {
                    Content = this.ModelGroup,
                };
            }

            public readonly Visual3D Visual;
            public readonly Model3DGroup ModelGroup;

            public readonly List<Dot> Dots = new List<Dot>();

            public readonly List<Tuple<Dot, Dot, double>> Distances_Input = new List<Tuple<Dot, Dot, double>>();
            public readonly List<Tuple<Dot, Dot, double>> Distances_Output = new List<Tuple<Dot, Dot, double>>();
        }

        #endregion

        #region Declaration Section

        private const double RADIUS = .5;
        private const double DOTRADIUS = .035;

        private const double SKETCHSIZE_LARGE = 200;
        private const double SKETCHSIZE_SMALL = 100;

        private Network _network = null;
        private NetworkInputs _networkInputs = null;
        private NetworkOutputs _networkOutputs = null;

        private List<EncogOCR_SketchData> _vectorImages = new List<EncogOCR_SketchData>();

        private DotVisuals _dots = null;

        private Dot _mouseOver = null;

        /// <summary>
        /// This listens to the mouse/keyboard and controls the camera
        /// </summary>
        private TrackBallRoam _trackball = null;

        private static ThreadLocal<SolidColorBrush[]> _grayBrushes_WhiteBlack = new ThreadLocal<SolidColorBrush[]>(() => Enumerable.Range(0, 256).Select(o => Convert.ToByte(255 - o)).Select(o => new SolidColorBrush(Color.FromRgb(o, o, o))).ToArray());
        private static ThreadLocal<SolidColorBrush[]> _grayBrushes_TransparentBlack = new ThreadLocal<SolidColorBrush[]>(() => Enumerable.Range(0, 256).Select(o => Convert.ToByte(o)).Select(o => new SolidColorBrush(Color.FromArgb(o, 0, 0, 0))).ToArray());
        private static ThreadLocal<SolidColorBrush> _imageBackBrush = new ThreadLocal<SolidColorBrush>(() => new SolidColorBrush(UtilityWPF.ColorFromHex("40FFFFFF")));

        private readonly DispatcherTimer _timer;

        #endregion

        #region Constructor

        public OCRTestDataVisualizer()
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

        public void NetworkChanged(TrainedNework_Simple network)
        {
            _network = new Network(network);

            // Detect input size change
            if (_networkInputs == null || _networkInputs.Size != network.InputSize)
            {
                _networkInputs = new NetworkInputs(network.InputSize);
            }

            // Detect output size change
            if (_networkOutputs == null || !_networkOutputs.IsSame(network.Outputs))
            {
                _networkOutputs = new NetworkOutputs(network.Outputs);

                BuildLegend(_networkOutputs.Names, _networkOutputs.Hues);
            }

            #region Dots

            if (_dots == null)
            {
                EnsureDotsCreated();
            }
            else
            {
                _dots.Distances_Input.Clear();
                _dots.Distances_Output.Clear();

                // Clear
                foreach (Dot dot in _dots.Dots)
                {
                    dot.NNInput = null;
                    dot.NNOutput = null;
                }

                // Rebuild
                foreach (Dot dot in _dots.Dots)
                {
                    ReconstructDot(dot);
                }
            }

            #endregion
        }

        /// <summary>
        /// It's ok to add the same sketch multiple times.  Only one copy will be kept (based on token)
        /// </summary>
        public void AddSketch(EncogOCR_SketchData sketch)
        {
            if (_vectorImages.Any(o => o.Token == sketch.Token))
            {
                // Already have it
                return;
            }
            else if (sketch.Strokes.Length == 0 && _vectorImages.Any(o => o.Strokes.Length == 0))
            {
                // Only store one blank image
                return;
            }
            else if (_vectorImages.AsParallel().Any(o => IsSame_Strokes(o, sketch)))
            {
                // When they draw a test image, it gets added.  Then when they hit store, a duplicate gets added (different token), so
                // look for an exact dupe
                return;
            }

            _vectorImages.Add(sketch);

            EnsureDotCreated(sketch);
        }

        /// <summary>
        /// This wipes out everything (call this when loading a new session)
        /// </summary>
        public void Clear()
        {
            _network = null;
            _networkInputs = null;
            _networkOutputs = null;

            _vectorImages.Clear();

            if (_dots != null)
            {
                _viewport.Children.Remove(_dots.Visual);
                _dots = null;
            }
        }

        #endregion

        #region Event Listener

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void grdViewPort_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (_dots == null)
                {
                    return;
                }

                var hitResult = GetMouseOver(e, _dots);
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

                Point position = new Point(canvas.ActualWidth / 2, canvas.ActualHeight / 2);
                ShowImage(dot, position, true);

                // Find nearby images
                var nearby = GetNearbySketches(dot, _dots.Dots, .1);

                if (nearby.Length > 0)
                {
                    ShowNearbySketches(nearby, dot.Position, hitResult.Item2, position);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void grdViewPort_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //TODO: Create a buble around what they clicked on.  Everything inside the bubble uses input instead of output
            //the dots inside the bubble aren't affected by anything outside, but the dots outside do need to move out of the way
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            const double MULT = .005;
            const double MAXSPEED = .05;
            const double MAXSPEED_SQUARED = MAXSPEED * MAXSPEED;

            try
            {
                if (_dots == null)
                {
                    return;
                }

                Tuple<Dot, Vector3D>[] forces = GetForces(_dots, chkOutput.IsChecked.Value, MULT);

                // Cap the speed
                forces = forces.
                    Select(o =>
                    {
                        if (o.Item2.LengthSquared < MAXSPEED_SQUARED)
                        {
                            return o;
                        }
                        else
                        {
                            return Tuple.Create(o.Item1, o.Item2.ToUnit() * MAXSPEED);
                        }
                    }).
                    ToArray();

                foreach (var force in forces)
                {
                    force.Item1.Position += force.Item2;
                    force.Item1.Translate_3DDot.OffsetX = force.Item1.Position.X + force.Item2.X;
                    force.Item1.Translate_3DDot.OffsetY = force.Item1.Position.Y + force.Item2.Y;
                    force.Item1.Translate_3DDot.OffsetZ = force.Item1.Position.Z + force.Item2.Z;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private void EnsureDotsCreated()
        {
            if (_dots == null)
            {
                _dots = new DotVisuals();
                _viewport.Children.Add(_dots.Visual);
            }

            foreach (var sketch in _vectorImages)
            {
                EnsureDotCreated(sketch);
            }
        }

        private void EnsureDotCreated(EncogOCR_SketchData sketch)
        {
            if (_dots == null)
            {
                return;
            }
            else if (_dots.Dots.Any(o => o.Token == sketch.Token))
            {
                return;
            }

            Dot dot = new Dot(sketch.Token)
            {
                VectorSource = sketch.Clone(),      // cloning to ensure the sketch passed in doesn't get manipulated.  NOTE: The clone will have a different token
                Position = Math3D.GetRandomVector_Spherical(RADIUS).ToPoint(),
            };

            _dots.Dots.Add(dot);

            if (_networkOutputs != null)
            {
                ReconstructDot(dot);
            }
        }

        /// <summary>
        /// Call this if a new dot is added, or if the network gets swapped out
        /// NOTE: EnsureDotCreated needs to be called first
        /// </summary>
        private void ReconstructDot(Dot dot)
        {
            if (_dots == null)
            {
                return;
            }

            #region Remove Existing

            // Remove distances
            _dots.Distances_Input.RemoveAll(o => o.Item1.Token == dot.Token || o.Item2.Token == dot.Token);
            _dots.Distances_Output.RemoveAll(o => o.Item1.Token == dot.Token || o.Item2.Token == dot.Token);

            // Remove the 3D dot from visual
            if (dot.Geometry_3DDot != null)
            {
                _dots.ModelGroup.Children.Remove(dot.Geometry_3DDot);

                dot.Geometry_3DDot = null;
                dot.Translate_3DDot = null;
            }

            // Remove the 2D visual
            dot.Image = null;

            #endregion

            if (_network == null || _networkInputs == null || _networkOutputs == null)
            {
                return;
            }

            #region Test against network

            if (dot.VectorSource != null)
            {
                // Make sure the input is correct
                dot.VectorSource.GenerateBitmap(_networkInputs.ImageHeightWidth);
                dot.NNInput = dot.VectorSource.NNInput;
            }

            if (dot.NNInput == null || dot.NNInput.Length != _networkInputs.Size)
            {
                return;
            }

            // Run it through the network
            dot.NNOutput = _network.Net.Network.Compute(dot.NNInput);

            #endregion

            #region Draw

            int? matchIndex = UtilityEncog.IsMatch(dot.NNOutput);

            // Material
            Material material;
            if (matchIndex == null)
            {
                dot.IsMatch = false;
                dot.Color = _networkOutputs.ColorGray;
                material = _networkOutputs.DotGray;
            }
            else
            {
                dot.IsMatch = true;
                dot.Color = _networkOutputs.DotColors[matchIndex.Value].Item1;
                material = _networkOutputs.DotColors[matchIndex.Value].Item2;
            }

            dot.Translate_3DDot = new TranslateTransform3D(dot.Position.ToVector());

            // Geometry
            dot.Geometry_3DDot = new GeometryModel3D()
            {
                Material = material,
                BackMaterial = material,
                Geometry = UtilityWPF.GetSphere_Ico(DOTRADIUS, 1, true),
                Transform = dot.Translate_3DDot,
            };

            _dots.ModelGroup.Children.Add(dot.Geometry_3DDot);

            #endregion

            #region Distances

            var inputs = new List<Tuple<Dot, Dot, double>>();
            var outputs = new List<Tuple<Dot, Dot, double>>();

            foreach (Dot other in _dots.Dots.Where(o => o.Token != dot.Token && o.NNInput != null && o.NNOutput != null))       // when the network gets reset, it sets all the dots' NNInput/Output to null.  Then goes through each dot and reconstructs.  So if I/O is null, it just hasn't been reconstructed yet
            {
                double distance = Math3D.GetDistance(dot.NNInput, other.NNInput);
                inputs.Add(Tuple.Create(dot, other, distance));

                distance = Math3D.GetDistance(dot.NNOutput, other.NNOutput);
                outputs.Add(Tuple.Create(dot, other, distance));
            }

            _dots.Distances_Input.AddRange(inputs);
            _dots.Distances_Output.AddRange(outputs);

            #endregion
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

        private static Tuple<Dot, Vector3D>[] GetForces(DotVisuals dots, bool useOutput, double mult)
        {
            // Figure out which set of distances to use
            var distances = useOutput ? dots.Distances_Output : dots.Distances_Input;

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
            Point3D center = Math3D.GetCenter(dots.Dots.Select(o => o.Position));
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

        /// <summary>
        /// This compares the strokes (ignores token)
        /// </summary>
        private static bool IsSame_Strokes(EncogOCR_SketchData sketch1, EncogOCR_SketchData sketch2)
        {
            if (sketch1.Strokes.Length != sketch2.Strokes.Length)
            {
                return false;
            }

            if (!Enumerable.Range(0, sketch1.Strokes.Length).
                All(o =>
                    {
                        if (sketch1.Strokes[o].Points.Length != sketch2.Strokes[o].Points.Length)
                            return false;

                        if (sketch1.Strokes[o].Color != sketch2.Strokes[o].Color)
                            return false;

                        if (!sketch1.Strokes[o].Thickness.IsNearValue(sketch2.Strokes[o].Thickness))
                            return false;

                        return true;
                    }))
            {
                return false;
            }

            // Compare each point
            for (int outer = 0; outer < sketch1.Strokes.Length; outer++)
            {
                for (int inner = 0; inner < sketch1.Strokes[outer].Points.Length; inner++)
                {
                    if (!Math2D.IsNearValue(sketch1.Strokes[outer].Points[inner], sketch2.Strokes[outer].Points[inner]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private Tuple<Dot, RayHitTestParameters> GetMouseOver(MouseEventArgs e, DotVisuals dots)
        {
            const double MAXHITDISTANCE = .035;

            // Fire a ray from the mouse point
            Point mousePoint = e.GetPosition(grdViewPort);
            var ray = UtilityWPF.RayFromViewportPoint(_camera, _viewport, mousePoint);

            var hit = dots.Dots.
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

        private void ShowImage(Dot dot, Point position, bool isLarge)
        {
            #region Get image

            var image = dot.Image;
            if (image == null)
            {
                var bitmap = GetBitmap(dot.NNInput);

                Color? borderColor = null;
                if (dot.IsMatch)
                {
                    borderColor = dot.Color.ToRGB();
                }

                image = GetImageCtrl(bitmap, borderColor);

                dot.Image = image;
            }

            #endregion

            double size = isLarge ? SKETCHSIZE_LARGE : SKETCHSIZE_SMALL;

            // This is the portion of the control that is the image.  This is what needs to be sized (the parent of this image is a border)
            image.Item2.Width = size;
            image.Item2.Height = size;

            double halfSize = size / 2;

            Canvas.SetLeft(image.Item1, position.X - halfSize);
            Canvas.SetTop(image.Item1, position.Y - halfSize);
            canvas.Children.Add(image.Item1);
        }

        private static Tuple<UIElement, Image> GetImageCtrl(BitmapSource bitmap, Color? borderColor = null)
        {
            Image imageCtrl = new Image()
            {
                Width = 10,     // this needs to be adjusted later
                Height = 10,
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

            return new Tuple<UIElement, Image>(borderCtrl, imageCtrl);
        }

        private static BitmapSource GetBitmap_ORIG(double[] input)
        {
            int size = Convert.ToInt32(Math.Sqrt(input.Length));
            if (size * size != input.Length)
            {
                throw new ArgumentException("Must pass in a square image");
            }

            RenderTargetBitmap retVal = new RenderTargetBitmap(size, size, UtilityWPF.DPI, UtilityWPF.DPI, PixelFormats.Pbgra32);

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
        private static BitmapSource GetBitmap(double[] input)
        {
            int size = Convert.ToInt32(Math.Sqrt(input.Length));
            if (size * size != input.Length)
            {
                throw new ArgumentException("Must pass in a square image");
            }

            Color[] colors = input.Select(o =>
                {
                    byte alpha = Convert.ToByte(Math.Round(o * 255));
                    return Color.FromArgb(alpha, 0, 0, 0);
                }).
                ToArray();

            return UtilityWPF.GetBitmap(colors, size, size);
        }

        private static Dot[] GetNearbySketches(Dot sketch, IEnumerable<Dot> all, double radius)
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

        private void ShowNearbySketches(Dot[] nearby, Point3D center3D, RayHitTestParameters cameraRay, Point center2D)
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

            double halfLarge = SKETCHSIZE_LARGE / 2;
            double halfSmall = SKETCHSIZE_SMALL / 2;

            double stepDist = SKETCHSIZE_SMALL * .05;

            // Don't start counter at a distance of zero, that's just wasted steps.  Figure out what cntr to use that is the distance of the two images touching
            int startIncrement = Convert.ToInt32(Math.Floor((halfSmall + halfLarge) / stepDist));

            // Remember the locations of each image rect
            List<Rect> existing = new List<Rect>();
            existing.Add(new Rect(center2D.X - halfLarge, center2D.Y - halfLarge, SKETCHSIZE_LARGE, SKETCHSIZE_LARGE));

            foreach (var nextSketch in nearbyOnPlane)
            {
                // Get the 2D direction this sketch is from the main
                Vector direction = rotate.Transform(nextSketch.PlanePoint - center3D).ToVector2D();

                Vector dirUnit = direction.ToUnit();
                if (Math2D.IsInvalid(dirUnit))
                {
                    dirUnit = Math3D.GetRandomVector_Circular_Shell(1).ToVector2D();        // sitting on top of each other, just push it in a random direction
                }
                dirUnit = new Vector(dirUnit.X, -dirUnit.Y);

                Point point = new Point();
                Rect rect = new Rect();

                // Keep walking along that direction until the rectangle doesn't intersect any existing sketches
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

                ShowImage(nextSketch.Sketch, point, false);
            }
        }

        #endregion
    }
}
