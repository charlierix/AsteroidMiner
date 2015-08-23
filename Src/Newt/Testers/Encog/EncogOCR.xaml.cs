using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xaml;
using System.Xml.Serialization;
using Encog.Neural.Networks;
using Game.HelperClassesAI;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;

namespace Game.Newt.Testers.Encog
{
    public partial class EncogOCR : Window
    {
        #region Class: SimpleNetworkTrainer

        private class SimpleNetworkTrainer
        {
            #region Events

            public event EventHandler UpdatedNetwork = null;

            #endregion

            #region Declaration Section

            private readonly EncogOCR _parent;

            private List<Tuple<long, CancellationTokenSource, Task<Tuple<long, TrainedNework_Simple>>>> _currentTasks = new List<Tuple<long, CancellationTokenSource, Task<Tuple<long, TrainedNework_Simple>>>>();

            /// <summary>
            /// This holds a token for the most recent network build task.  If a task finishes that isn't for this token,
            /// then the result is just thrown away
            /// </summary>
            private long? _currentToken = null;

            #endregion

            #region Constructor

            public SimpleNetworkTrainer(EncogOCR parent)
            {
                _parent = parent;
            }

            #endregion

            #region Public Methods

            public void TrainSet(EncogOCR_SketchData[] sketches, bool shouldGenerateGarbage = true)
            {
                // Assign a token so that only the latest result will be used
                long token = TokenGenerator.NextToken();
                _currentToken = token;

                _parent.lblTraining.Content = "training...";

                // Kill existing tasks
                //NOTE: Removing from the list is done in the continue
                foreach (var runningTask in _currentTasks)
                {
                    runningTask.Item2.Cancel();
                }

                // Group the sketches by name
                var groupedSketches = sketches.ToLookup(o => o.Name).
                    Select(o => Tuple.Create(o.Key, o.Select(p => p).ToArray())).
                    ToArray();
                if (groupedSketches.Length == 0)
                {
                    _parent._network = null;
                    _parent.lblTraining.Content = "";
                    return;
                }

                TrainNetwork(groupedSketches, token, shouldGenerateGarbage);
            }

            public void CancelAll()
            {
                foreach (var runningTask in _currentTasks)
                {
                    runningTask.Item2.Cancel();
                }
            }

            #endregion

            #region Private Methods

            private void TrainNetwork(Tuple<string, EncogOCR_SketchData[]>[] sketches, long token, bool shouldGenerateGarbage)
            {
                CancellationTokenSource cancel = new CancellationTokenSource();

                // Get a trained network
                var task = TrainAsync(sketches, token, shouldGenerateGarbage, cancel);

                // Remember this so it can be killed if another is spun up (that's why I can't just await)
                _currentTasks.Add(Tuple.Create(_currentToken.Value, cancel, task));

                // Store the trained network, remove the task from the list
                task.ContinueWith(result =>
                {
                    CommitResult(result);

                    // I decided not to cascade from pure to dirty.  The network trains quickly, so double training is unnecessary
                    //// Now try a more complex set of inputs (same inputs passed in + anti inputs)
                    //if (_currentToken == token && !shouldGenerateGarbage)
                    //{
                    //    TrainNetwork(sketches, token, true);
                    //}
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }

            private static Task<Tuple<long, TrainedNework_Simple>> TrainAsync(Tuple<string, EncogOCR_SketchData[]>[] sketches, long token, bool shouldGenerateGarbage, CancellationTokenSource cancel)
            {
                CancellationToken cancelToken = cancel.Token;

                return Task.Run(() =>
                {
                    string[] outputMap = sketches.
                        Select(o => o.Item1).
                        ToArray();

                    int inputSize = sketches[0].Item2[0].NNInput.Length;

                    #region Training Data

                    List<double[]> inputs = new List<double[]>();
                    List<double[]> outputs = new List<double[]>();

                    int groupIndex = 0;
                    foreach (var group in sketches)
                    {
                        double[] output = Enumerable.Range(0, outputMap.Length).
                            Select((o, i) => i == groupIndex ? 1d : 0d).
                            ToArray();

                        foreach (var input in group.Item2)
                        {
                            inputs.Add(input.NNInput);
                            outputs.Add(output);
                        }

                        groupIndex++;
                    }

                    if (shouldGenerateGarbage)
                    {
                        GenerateGarbageData(sketches.SelectMany(o => o.Item2).ToArray(), outputMap.Length, inputs, outputs);
                    }

                    #endregion

                    //NOTE: If there is an exception, the network couldn't be trained
                    BasicNetwork network = null;
                    try
                    {
                        network = UtilityEncog.GetTrainedNetwork(inputs.ToArray(), outputs.ToArray(), UtilityEncog.ERROR, 20, 300, cancelToken).NetworkOrNull;
                    }
                    catch (Exception) { }

                    var returnProps = new TrainedNework_Simple()
                    {
                        InputSize = inputSize,
                        Outputs = outputMap,
                        Network = network,
                    };

                    return Tuple.Create(token, returnProps);
                }, cancelToken);
            }

            private static void GenerateGarbageData(EncogOCR_SketchData[] sketches, int outputSize, List<double[]> inputs, List<double[]> outputs)
            {
                #region validate
#if DEBUG
                if (sketches.Select(o => o.BitmapColors.Width).Distinct().Count() != 1 || sketches.Select(o => o.BitmapColors.Height).Distinct().Count() != 1)
                {
                    throw new ApplicationException("Stored images are different sizes");
                }
#endif
                #endregion

                double[] zeroOutput = Enumerable.Range(0, outputSize).Select(o => 0d).ToArray();

                #region efficient if only a final is needed

                //Color[][] colors = sketches.
                //    Select(o => o.BitmapColors.GetColors(0, 0, o.Bitmap.PixelWidth, o.Bitmap.PixelHeight)).
                //    ToArray();

                //bool[] isBlack = Enumerable.Range(0, colors[0].Length).
                //    AsParallel().
                //    Select(o => new { Index = o, IsBlack = colors.Any(p => p[o].A > 200 && Math3D.Avg(p[o].R, p[o].G, p[o].B) < 20) }).
                //    OrderBy(o => o.Index).
                //    Select(o => o.IsBlack).
                //    ToArray();

                #endregion

                // Figure out which pixels of each sketch are black
                bool[][] isBlack = sketches.
                    Select(o => o.BitmapColors.GetColors(0, 0, o.BitmapColors.Width, o.BitmapColors.Height)).
                    Select(o => o.Select(p => p.A > 200 && Math3D.Avg(p.R, p.G, p.B) < 20).ToArray()).
                    ToArray();

                // Or the images together
                bool[] composite = Enumerable.Range(0, isBlack[0].Length).
                    AsParallel().
                    Select(o => new { Index = o, IsBlack = isBlack.Any(p => p[o]) }).
                    OrderBy(o => o.Index).
                    Select(o => o.IsBlack).
                    ToArray();

                #region Blank sketch

                double[] percentBlack = isBlack.
                    Select(o => o.Sum(p => p ? 1d : 0d) / o.Length).
                    ToArray();

                if (percentBlack.All(o => o > .01))
                {
                    // None of the inputs are blank, so make blank a garbage state
                    inputs.Add(Enumerable.Range(0, composite.Length).Select(o => 0d).ToArray());
                    outputs.Add(zeroOutput);
                }

                #endregion
                #region Inverted sketch

                // Making this, because the assumption is that if they draw in a region outside any of the inputs, then it should be a zero output

                inputs.Add(composite.Select(o => o ? 0d : 1d).ToArray());
                outputs.Add(zeroOutput);

                #endregion

                //TODO: Do a few subsets of inverted

            }

            private void CommitResult(Task<Tuple<long, TrainedNework_Simple>> result)
            {
                try
                {
                    long taskToken = result.Result.Item1;

                    // Store the network
                    if (_currentToken == taskToken && result.Result.Item2.Network != null)
                    {
                        _parent._network = result.Result.Item2;
                        _parent.lblTraining.Content = "";

                        if (this.UpdatedNetwork != null)
                        {
                            this.UpdatedNetwork(this, new EventArgs());
                        }
                    }

                    // Remove this task from the list
                    var thisTask = _currentTasks.
                        Select((o, i) => new { Token = o.Item1, Index = i }).
                        FirstOrDefault(o => o.Token == taskToken);

                    if (thisTask != null)
                    {
                        _currentTasks.RemoveAt(thisTask.Index);
                    }
                }
                catch (Exception ex)
                {
                    //NOTE: If execution gets here, _currentTasks will still hold this task.  That's sloppy, but no harm.  The cancel token will just get called a lot
                    MessageBox.Show(ex.ToString(), _parent.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            #endregion
        }

        #endregion

        #region Declaration Section

        /// <summary>
        /// This will be a subfolder of AsteroidMiner
        /// </summary>
        private const string FOLDER = "EncogOCR";

        private readonly SimpleNetworkTrainer _simpleTrainer;

        private int _pixels;

        private EncogOCR_SketchData _currentSketch = null;
        private List<EncogOCR_SketchData> _prevSketches = new List<EncogOCR_SketchData>();

        /// <summary>
        /// This is the currently trained network
        /// NOTE: An old version may be stored here while a new one is being trained
        /// </summary>
        private TrainedNework_Simple _network = null;

        private readonly DropShadowEffect _errorEffect;

        private OCRTestDataVisualizer _visualizer = null;

        private bool _isLoadingSession = false;
        private bool _initialized = false;

        #endregion

        #region Constructor

        public EncogOCR()
        {
            InitializeComponent();

            this.Background = SystemColors.ControlBrush;

            _errorEffect = new DropShadowEffect()
            {
                Color = Colors.Red,
                BlurRadius = 15,
                Direction = 0,
                ShadowDepth = 0,
                Opacity = .8,
            };

            _simpleTrainer = new SimpleNetworkTrainer(this);
            _simpleTrainer.UpdatedNetwork += SimpleTrainer_UpdatedNetwork;

            _pixels = int.Parse(txtPixels.Text);

            _initialized = true;
        }

        #endregion

        #region Event Listeners

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Visualizer
                _visualizer = new OCRTestDataVisualizer();
                _visualizer.Closed += Visualizer_Closed;

                _visualizer.Top = this.Top;
                _visualizer.Left = this.Left + this.ActualWidth;
                _visualizer.Show();


                // Load the last saved session
                TryLoadSession();
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
                if (_visualizer != null)
                {
                    _visualizer.Close();
                }

                if (_simpleTrainer != null)
                {
                    _simpleTrainer.CancelAll();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Visualizer_Closed(object sender, EventArgs e)
        {
            try
            {
                _visualizer = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SimpleTrainer_UpdatedNetwork(object sender, EventArgs e)
        {
            try
            {
                if (_visualizer != null)
                {
                    _visualizer.NetworkChanged(_network);

                    foreach (var sketch in _prevSketches)
                        _visualizer.AddSketch(sketch);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnClearDrawing_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                canvasInk.Strokes.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnStoreDrawing_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cboDrawingLabel.Text == "")
                {
                    MessageBox.Show("Please give this drawing a name", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_currentSketch == null)
                {
                    RedrawSmallImage();
                }

                #region Create sketch for storage

                EncogOCR_SketchData sketch = new EncogOCR_SketchData()
                {
                    Name = cboDrawingLabel.Text,
                    InkCanvasSize = new Size(canvasInk.ActualWidth, canvasInk.ActualHeight),
                    Strokes = _currentSketch.Strokes.ToArray(),
                };

                sketch.GenerateBitmap(_pixels);
                sketch.GenerateImageControl();

                #endregion

                AddSketch(sketch);
                TrainNetwork();

                //NOTE: Event listeners will clear the rest based on this
                canvasInk.Strokes.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void txtPen_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                int penSize;
                if (int.TryParse(txtPen.Text, out penSize))
                {
                    txtPen.Effect = null;
                }
                else
                {
                    txtPen.Effect = _errorEffect;
                    return;
                }

                if (penSize < 1)
                {
                    txtPen.Effect = _errorEffect;
                    return;
                }

                penAttrib.Width = penSize;
                penAttrib.Height = penSize;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void txtPixels_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (!_initialized || _isLoadingSession)
                {
                    return;
                }

                int pixels;
                if (int.TryParse(txtPixels.Text, out pixels))
                {
                    txtPixels.Effect = null;
                }
                else
                {
                    txtPixels.Effect = _errorEffect;
                    return;
                }

                if (pixels < 1)
                {
                    txtPixels.Effect = _errorEffect;
                    return;
                }

                if (pixels == _pixels)
                {
                    return;
                }

                _pixels = pixels;

                RedrawSmallImage();
                RedrawPreviousImages(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StrokeCollection_StrokesChanged(object sender, System.Windows.Ink.StrokeCollectionChangedEventArgs e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                RedrawSmallImage();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCollapseAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (TreeViewItem node in pnlPreviousDrawings.Items)
                {
                    node.IsExpanded = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnExpandAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (TreeViewItem node in pnlPreviousDrawings.Items)
                {
                    node.IsExpanded = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void pnlPreviousDrawings_KeyUp(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key != Key.Delete)
                {
                    return;
                }
                else if (!(e.OriginalSource is TreeViewItem))
                {
                    return;
                }

                TreeViewItem sourceCast = (TreeViewItem)e.OriginalSource;

                // Figure out what was selected
                if (sourceCast.Header is string)
                {
                    #region Entire category

                    string header = (string)sourceCast.Header;

                    pnlPreviousDrawings.Items.Remove(sourceCast);

                    int index = 0;
                    while (index < _prevSketches.Count)
                    {
                        if (_prevSketches[index].Name.Equals(header, StringComparison.OrdinalIgnoreCase))
                        {
                            _prevSketches.RemoveAt(index);
                        }
                        else
                        {
                            index++;
                        }
                    }

                    #endregion
                }
                else if (sourceCast.Header is Image)
                {
                    #region Single image

                    Image imageSource = (Image)sourceCast.Header;

                    // Find the sketch for this image
                    for (int cntr = 0; cntr < _prevSketches.Count; cntr++)
                    {
                        if (_prevSketches[cntr].ImageControl == imageSource)
                        {
                            // Remove from the treeview (need to find the parent of this image)
                            foreach (TreeViewItem parentNode in pnlPreviousDrawings.Items)
                            {
                                if (((string)parentNode.Header).Equals(_prevSketches[cntr].Name, StringComparison.OrdinalIgnoreCase))
                                {
                                    // Found it
                                    parentNode.Items.Remove(imageSource);
                                    break;
                                }
                            }

                            // parent is null
                            //((TreeViewItem)sourceCast.Parent).Items.Remove(sourceCast);



                            // Now remove the sketch
                            _prevSketches.RemoveAt(cntr);
                            break;
                        }
                    }

                    #endregion
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnGarbage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_prevSketches.Count == 0)
                {
                    MessageBox.Show("Need stored images first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                #region Drawing attrib

                // This is a percent of the canvas's size (each sketch could come from a different size canvas)
                double avgStroke = _prevSketches.
                    Select(o => new { CanvasSize = Math3D.Avg(o.InkCanvasSize.Width, o.InkCanvasSize.Height), Sizes = o.Strokes.Select(p => p.Thickness) }).
                    Select(o => o.Sizes.Select(p => p / o.CanvasSize)).
                    SelectMany(o => o).
                    Average();

                // Now figure out the stroke size for this current canvas
                double strokeSize = Math3D.Avg(canvasInk.ActualWidth, canvasInk.ActualHeight);
                strokeSize = avgStroke * strokeSize;

                DrawingAttributes attrib = new DrawingAttributes()
                {
                    Width = strokeSize,
                    Height = strokeSize,
                    Color = Colors.Black,
                    FitToCurve = true,
                    IsHighlighter = false,
                    StylusTip = StylusTip.Ellipse,
                    StylusTipTransform = Transform.Identity.Value,
                };

                #endregion

                StrokeCollection strokes = new StrokeCollection();

                Point[] points = new[]
                    {
                        new Point(0,0),
                        new Point(canvasInk.ActualWidth, canvasInk.ActualHeight),
                    };

                strokes.Add(new Stroke(new StylusPointCollection(points), attrib));

                // Show the drawing
                canvasInk.Strokes.Clear();
                canvasInk.Strokes.Add(strokes);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnGarbage2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_prevSketches.Count == 0)
                {
                    MessageBox.Show("Need stored images first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Pick a random sketch
                var sketch = StaticRandom.NextItem(_prevSketches);

                Color[] colors = sketch.BitmapColors.GetColors(0, 0, sketch.Bitmap.PixelWidth, sketch.Bitmap.PixelHeight);
                Color[] inverseColors = colors.
                    Select(o =>
                    {
                        bool isBlack = (o.A > 200 && Math3D.Avg(o.R, o.G, o.B) < 20);

                        // Invert the color
                        return isBlack ? Colors.Transparent : Colors.Black;
                    }).
                    ToArray();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnGarbage3_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_prevSketches.Count == 0)
                {
                    MessageBox.Show("Need stored images first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_prevSketches.Select(o => o.Bitmap.PixelWidth).Distinct().Count() != 1 || _prevSketches.Select(o => o.Bitmap.PixelHeight).Distinct().Count() != 1)
                {
                    MessageBox.Show("Stored images are different sizes", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Color[][] colors = _prevSketches.
                    Select(o => o.BitmapColors.GetColors(0, 0, o.Bitmap.PixelWidth, o.Bitmap.PixelHeight)).
                    ToArray();

                bool[] isBlack = Enumerable.Range(0, colors[0].Length).
                    AsParallel().
                    Select(o => new { Index = o, IsBlack = colors.Any(p => p[o].A > 200 && Math3D.Avg(p[o].R, p[o].G, p[o].B) < 20) }).
                    OrderBy(o => o.Index).
                    Select(o => o.IsBlack).
                    ToArray();

                bool[] inverted = isBlack.
                    Select(o => !o).
                    ToArray();


                //canvasPixels.Source = GetBitmap(isBlack);
                canvasPixels.Source = GetBitmap(inverted);




            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnNewSession_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearEverything();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnSaveSession_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EncogOCR_Session session = new EncogOCR_Session()
                {
                    ImageSize = _pixels,
                    Sketches = _prevSketches.ToArray(),
                };

                //C:\Users\<username>\AppData\Roaming\Asteroid Miner\EncogOCR\
                string foldername = UtilityCore.GetOptionsFolder();
                foldername = System.IO.Path.Combine(foldername, FOLDER);
                Directory.CreateDirectory(foldername);

                string filename = DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss.fff - ");
                filename += cboSession.Text + ".xml";       //TODO: convert illegal chars
                filename = System.IO.Path.Combine(foldername, filename);

                UtilityCore.SerializeToFile(filename, session);

                LoadSessionsCombobox();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnLoadSession_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = TryLoadSession(cboSession.Text);

                if (!result.Item1)
                {
                    MessageBox.Show(result.Item2, this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private Tuple<bool, string> TryLoadSession(string name = "")
        {
            #region Find file

            //C:\Users\<username>\AppData\Roaming\Asteroid Miner\EncogOCR\
            string foldername = UtilityCore.GetOptionsFolder();
            foldername = System.IO.Path.Combine(foldername, FOLDER);

            if (!Directory.Exists(foldername))
            {
                return Tuple.Create(false, "Saves folder doesn't exist");
            }

            string[] filenames;
            if (string.IsNullOrEmpty(name))
            {
                filenames = Directory.GetFiles(foldername);
            }
            else
            {
                filenames = Directory.GetFiles(foldername, "*" + name + "*");
            }

            string filename = filenames.
                OrderByDescending(o => o).      // the files all start with a date same (ymdhmsf), so the last file alphabetically will be the latest
                FirstOrDefault();

            if (filename == null)
            {
                return Tuple.Create(false, "Couldn't find session");
            }

            #endregion

            #region Deserialize session

            EncogOCR_Session session = null;
            try
            {
                session = UtilityCore.DeserializeFromFile<EncogOCR_Session>(filename);
            }
            catch (Exception ex)
            {
                return Tuple.Create(false, ex.Message);
            }

            #endregion

            #region Load session

            _isLoadingSession = true;

            ClearEverything();

            _pixels = session.ImageSize;
            txtPixels.Text = _pixels.ToString();

            if (session.Sketches != null)
            {
                foreach (var sketch in session.Sketches)
                {
                    sketch.GenerateBitmap(session.ImageSize);
                    sketch.GenerateImageControl();

                    AddSketch(sketch);
                }
            }

            _isLoadingSession = false;

            #endregion

            // The combo should already have this, be just making sure
            LoadSessionsCombobox();
            cboSession.Text = GetSessionNameFromFilename(filename);

            #region Redraw

            canvasInk.Strokes.Clear();

            RedrawSmallImage();

            //TODO: When the NN gets serialized/deserialized, turn this off
            RedrawPreviousImages(true);

            #endregion

            return Tuple.Create(true, "");
        }

        /// <summary>
        /// This clears everything except:
        ///     session names out of the combo box
        ///     pixels
        ///     pen size
        /// </summary>
        private void ClearEverything()
        {
            if (_visualizer != null)
            {
                _visualizer.Clear();
            }

            _currentSketch = null;
            grdGuessDetails.Children.Clear();
            grdGuessDetails.RowDefinitions.Clear();

            pnlPreviousDrawings.Items.Clear();
            _prevSketches.Clear();

            cboDrawingLabel.Items.Clear();

            canvasInk.Strokes.Clear();

            _network = null;

            RedrawSmallImage();
            RedrawPreviousImages(true);
        }

        private void LoadSessionsCombobox()
        {
            const string DEFAULT = "session";

            string foldername = UtilityCore.GetOptionsFolder();
            foldername = System.IO.Path.Combine(foldername, FOLDER);

            string current = cboSession.Text;

            cboSession.Items.Clear();

            if (string.IsNullOrEmpty(current))
            {
                current = DEFAULT;
            }

            if (!Directory.Exists(foldername))
            {
                cboSession.Items.Add(DEFAULT);
                cboSession.Text = current;
                return;
            }

            string[] names = Directory.GetFiles(foldername).
                Select(o => GetSessionNameFromFilename(o)).
                Where(o => !o.Equals(DEFAULT, StringComparison.OrdinalIgnoreCase)).
                Distinct((o, n) => o.Equals(n, StringComparison.OrdinalIgnoreCase)).
                OrderBy(o => o).
                ToArray();

            if (names.Length == 0)
            {
                cboSession.Items.Add(DEFAULT);
            }
            else
            {
                foreach (string name in names)
                {
                    cboSession.Items.Add(name);
                }

                // Just let it stay DEFAULT.  That's no better than picking a random valid name
                //if(current == DEFAULT && !names.Any(o => o == DEFAULT))
                //{
                //    current = names[0];
                //}
            }

            cboSession.Text = current;
        }

        private static string GetSessionNameFromFilename(string filename)
        {
            return Regex.
                Match(System.IO.Path.GetFileNameWithoutExtension(filename), @"^\d{4}(-\d{2}){2} (\d{2}\.){3}\d{3} - (?<name>.+)").
                Groups["name"].
                Value;
        }

        private void AddSketch(EncogOCR_SketchData sketch)
        {
            _prevSketches.Add(sketch);

            // Find existing name node
            TreeViewItem nameNode = null;
            foreach (TreeViewItem node in pnlPreviousDrawings.Items)
            {
                if (((string)node.Header).Equals(sketch.Name, StringComparison.OrdinalIgnoreCase))
                {
                    nameNode = node;
                    break;
                }
            }

            if (nameNode == null)
            {
                // Create new
                nameNode = new TreeViewItem() { Header = sketch.Name };

                pnlPreviousDrawings.Items.Add(nameNode);
            }

            // Add the image control to this name node
            nameNode.Items.Add(sketch.ImageControl);

            nameNode.IsExpanded = true;

            #region Update the sketch name combo

            string currentText = cboDrawingLabel.Text;
            cboDrawingLabel.Items.Clear();

            foreach (string comboItem in _prevSketches.Select(o => o.Name).Distinct().OrderBy(o => o))
            {
                cboDrawingLabel.Items.Add(comboItem);
            }

            cboDrawingLabel.Text = currentText;

            #endregion
        }

        private void RedrawSmallImage()
        {
            //canvasPixels.Source = UtilityWPF.RenderControl(canvasInk, _pixels, _pixels, true);

            _currentSketch = new EncogOCR_SketchData()
            {
                Name = cboDrawingLabel.Text,
                Strokes = canvasInk.Strokes.
                    Select(o => new EncogOCR_StrokeDefinition(o)).
                    ToArray(),
                InkCanvasSize = new Size(canvasInk.ActualWidth, canvasInk.ActualHeight),
            };

            _currentSketch.GenerateBitmap(_pixels);

            canvasPixels.Source = _currentSketch.Bitmap;

            RecognizeImage();

            if (_visualizer != null)
            {
                _visualizer.AddSketch(_currentSketch.Clone());
            }
        }
        private void RedrawPreviousImages(bool shouldRebuildNN)
        {
            foreach (var sketch in _prevSketches)
            {
                sketch.GenerateBitmap(_pixels);

                sketch.ImageControl.Source = sketch.Bitmap;
            }

            if (shouldRebuildNN)
            {
                TrainNetwork();
            }
        }

        private void TrainNetwork()
        {
            //TODO: Look at radio buttons to see what kind of network they want

            _simpleTrainer.TrainSet(_prevSketches.ToArray());
        }

        private void RecognizeImage()
        {
            const string BORDER_STAND = "mildBorder";
            const string BORDER_FOUND = "validGuessBorder";

            lblCurrentGuess.Text = "";
            pnlCurrentGuess.Style = (Style)this.Resources[BORDER_STAND];
            grdGuessDetails.Children.Clear();
            grdGuessDetails.RowDefinitions.Clear();

            if (_network == null || _currentSketch == null || _currentSketch.NNInput.Length != _network.InputSize)
            {
                return;
            }

            // Recognize
            double[] output = _network.Network.Compute(_currentSketch.NNInput);

            var allOutputs = output.
                Select((o, i) => new { Value = o, Index = i, Name = _network.Outputs[i] }).
                OrderByDescending(o => o.Value).
                ToArray();

            #region Show results

            Brush percentStroke = new SolidColorBrush(UtilityWPF.ColorFromHex("60000000"));
            Brush percentFill = new SolidColorBrush(UtilityWPF.ColorFromHex("30000000"));

            foreach (var outputEntry in allOutputs)
            {
                grdGuessDetails.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });

                // Name
                TextBlock outputText = new TextBlock()
                {
                    Text = outputEntry.Name,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center,
                };

                Grid.SetColumn(outputText, 0);
                Grid.SetRow(outputText, grdGuessDetails.RowDefinitions.Count - 1);

                grdGuessDetails.Children.Add(outputText);

                // % background
                Rectangle rectPerc = new Rectangle()
                {
                    Height = 20,
                    Width = outputEntry.Value * 100,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Fill = percentFill,
                };

                Grid.SetColumn(rectPerc, 2);
                Grid.SetRow(rectPerc, grdGuessDetails.RowDefinitions.Count - 1);

                grdGuessDetails.Children.Add(rectPerc);

                // % border
                rectPerc = new Rectangle()
                {
                    Height = 20,
                    Width = 100,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Stroke = percentStroke,
                    StrokeThickness = 1,
                };

                Grid.SetColumn(rectPerc, 2);
                Grid.SetRow(rectPerc, grdGuessDetails.RowDefinitions.Count - 1);

                grdGuessDetails.Children.Add(rectPerc);

                // % text
                TextBlock outputPerc = new TextBlock()
                {
                    Text = Math.Round(outputEntry.Value * 100).ToString(),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };

                Grid.SetColumn(outputPerc, 2);
                Grid.SetRow(outputPerc, grdGuessDetails.RowDefinitions.Count - 1);

                grdGuessDetails.Children.Add(outputPerc);
            }

            #endregion

            #region Show final guess

            int? matchIndex = UtilityEncog.IsMatch(allOutputs.Select(o => o.Value).ToArray());

            if (matchIndex != null)
            {
                lblCurrentGuess.Text = allOutputs[matchIndex.Value].Name;
                pnlCurrentGuess.Style = (Style)this.Resources[BORDER_FOUND];
            }
            else
            {
                lblCurrentGuess.Text = "";
                pnlCurrentGuess.Style = (Style)this.Resources[BORDER_STAND];
            }

            #endregion
        }

        private static BitmapSource GetBitmap(bool[] isBlack)
        {
            int size = Convert.ToInt32(Math.Sqrt(isBlack.Length));
            if (size * size != isBlack.Length)
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
                        if (isBlack[index])
                        {
                            ctx.DrawRectangle(Brushes.Black, null, new Rect(x, y, 1, 1));
                        }

                        index++;
                    }
                }
            }

            retVal.Render(dv);

            return retVal;
        }

        #endregion
    }

    #region Class: EncogOCR_Session

    public class EncogOCR_Session
    {
        public EncogOCR_SketchData[] Sketches { get; set; }
        public int ImageSize { get; set; }

        //TODO: Support other types of networks (convolution)
        public TrainedNework_Simple NetworkSingle { get; set; }
    }

    #endregion
    #region Class: TrainedNework_Simple

    public class TrainedNework_Simple
    {
        public BasicNetwork Network { get; set; }

        public int InputSize { get; set; }
        public string[] Outputs { get; set; }
    }

    #endregion
    #region Class: SketchData

    public class EncogOCR_SketchData
    {
        public string Name { get; set; }

        public EncogOCR_StrokeDefinition[] Strokes { get; set; }
        public Size InkCanvasSize { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Image ImageControl { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public BitmapSource Bitmap { get; private set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IBitmapCustom BitmapColors { get; private set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public double[] NNInput { get; private set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public readonly long Token = TokenGenerator.NextToken();

        public void GenerateBitmap(int imageSize)
        {
            this.Bitmap = DrawStrokes(this.Strokes, imageSize, imageSize, this.InkCanvasSize.Width, this.InkCanvasSize.Height);
            this.BitmapColors = UtilityWPF.ConvertToColorArray(this.Bitmap, true, Colors.Transparent);

            this.NNInput = this.BitmapColors.GetColors(0, 0, imageSize, imageSize).
                Select(o => (o.A / 255d) * (255d - Math3D.Avg(o.R, o.G, o.B)) / 255d).     // 0 should be 1d, 255 should be 0d
                ToArray();
        }
        public void GenerateImageControl()
        {
            this.ImageControl = new Image()
            {
                Stretch = Stretch.Fill,
                Width = 100,
                Height = 100,
                Source = this.Bitmap,
            };
        }

        public EncogOCR_SketchData Clone()
        {
            return new EncogOCR_SketchData()
            {
                Name = this.Name,
                Strokes = this.Strokes.ToArray(),
                InkCanvasSize = this.InkCanvasSize,
            };
        }

        private static BitmapSource DrawStrokes(IEnumerable<EncogOCR_StrokeDefinition> strokes, int width, int height, double origWidth, double origHeight)
        {
            //NOTE: InkCanvas draws lines smoother than this method.  This is a crude way to approximate the thickness/darkeness of the lines
            double scale = Math3D.Avg(width, height) / Math3D.Avg(origWidth, origHeight);
            double reduce = 1;
            if (scale < .2)
            {
                reduce = UtilityCore.GetScaledValue(.8, 1, .05, .2, scale);
            }

            RenderTargetBitmap retVal = new RenderTargetBitmap(width, height, UtilityWPF.DPI, UtilityWPF.DPI, PixelFormats.Pbgra32);

            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext ctx = dv.RenderOpen())
            {
                foreach (EncogOCR_StrokeDefinition stroke in strokes)
                {
                    SolidColorBrush brush = new SolidColorBrush(UtilityWPF.ColorFromHex(stroke.Color));

                    if (stroke.Points.Length == 1)
                    {
                        // Single Point
                        double radius = stroke.Thickness / 2d;
                        radius *= reduce;
                        ctx.DrawEllipse(brush, null, stroke.Points[0], radius, radius);
                    }
                    else
                    {
                        // Multiple Points
                        Pen pen = new Pen(brush, stroke.Thickness * reduce)
                        {
                            StartLineCap = PenLineCap.Round,
                            EndLineCap = PenLineCap.Round,
                        };

                        for (int cntr = 0; cntr < stroke.Points.Length - 1; cntr++)
                        {
                            ctx.DrawLine(pen, stroke.Points[cntr], stroke.Points[cntr + 1]);
                        }
                    }
                }
            }

            dv.Transform = new ScaleTransform(width / origWidth, height / origHeight);

            retVal.Render(dv);

            return retVal;
        }
    }

    #endregion
    #region Class: StrokeDefinition

    public class EncogOCR_StrokeDefinition
    {
        public EncogOCR_StrokeDefinition() { }
        public EncogOCR_StrokeDefinition(Stroke stroke)
        {
            this.Points = (stroke.DrawingAttributes.FitToCurve ? stroke.GetBezierStylusPoints() : stroke.StylusPoints).
                    Select(o => new Point(o.X, o.Y)).
                    ToArray();

            this.Color = stroke.DrawingAttributes.Color.ToHex();
            this.Thickness = Math3D.Avg(stroke.DrawingAttributes.Width, stroke.DrawingAttributes.Height);
        }

        public Point[] Points { get; set; }
        public string Color { get; set; }
        public double Thickness { get; set; }
    }

    #endregion
}
