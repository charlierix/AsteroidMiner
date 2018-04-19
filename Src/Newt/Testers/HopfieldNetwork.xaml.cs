using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Shapes;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.Testers.Convolution;
using Game.Newt.Testers.Encog;

namespace Game.Newt.Testers
{
    public partial class HopfieldNetwork : Window
    {
        #region Declaration Section

        private const string FILE = "HopfieldNetwork Options.xml";
        private const int IMAGESIZE = 40;

        private List<string> _imageFolders = new List<string>();
        private List<FeatureRecognizer_Image> _images = new List<FeatureRecognizer_Image>();

        private readonly ConvolutionBase2D _trainKernel;

        private INNPatternStorage _patternStorage = null;

        private bool _initialized = false;

        #endregion

        #region Constructor

        public HopfieldNetwork()
        {
            InitializeComponent();

            _trainKernel = Convolutions.GetEdgeSet_GuassianThenEdge(5);

            _initialized = true;
        }

        #endregion

        #region Event Listeners

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                HopfieldNetworkOptions options = UtilityCore.ReadOptions<HopfieldNetworkOptions>(FILE);

                if (options != null && options.ImageFolders != null && options.ImageFolders.Length > 0)
                {
                    foreach (string folder in options.ImageFolders)
                    {
                        if (AddFolder(folder))
                        {
                            _imageFolders.Add(folder);
                        }
                    }

                    ResetTraining();
                }
                else
                {
                    expanderFolder.IsExpanded = true;
                }

                SetInkColor();
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
                HopfieldNetworkOptions options = new HopfieldNetworkOptions()
                {
                    ImageFolders = _imageFolders.ToArray(),
                };

                UtilityCore.SaveOptions(options, FILE);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new System.Windows.Forms.FolderBrowserDialog();
                dialog.Description = "Please select root folder";
                if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                string selectedPath = dialog.SelectedPath;

                if (AddFolder(selectedPath))
                {
                    _imageFolders.Add(selectedPath);
                }

                ResetTraining();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ClearImages_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _images.Clear();
                _imageFolders.Clear();

                lblNumImages.Text = _images.Count.ToString("N0");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearCanvas_Click(object sender, RoutedEventArgs e)
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

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_images.Count == 0)
                {
                    MessageBox.Show("Need to add folders first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ResetTraining();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PenDarkness_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                SetInkColor();
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
                EncogOCR_SketchData sketch = new EncogOCR_SketchData()
                {
                    Name = "",
                    Strokes = canvasInk.Strokes.
                        Select(o => new EncogOCR_StrokeDefinition(o)).
                        ToArray(),
                    InkCanvasSize = new Size(canvasInk.ActualWidth, canvasInk.ActualHeight),
                };

                sketch.GenerateBitmap(IMAGESIZE);

                // The bitmap is white on black, switch to black on white
                double[] inverted = sketch.NNInput.
                    Select(o => 1d - o).
                    ToArray();

                double[] converted = inverted;
                if (_patternStorage != null)
                {
                    converted = _patternStorage.Convert_Local_External(converted);
                }

                DrawImage(imageCurrent, converted);

                panelGuessedImages.Children.Clear();

                if (_patternStorage != null)
                {
                    double[][] recognized = _patternStorage.Recognize(inverted);

                    #region show thumbnails

                    if (recognized != null)
                    {
                        foreach (var guessImage in recognized)
                        {
                            DrawImage(panelGuessedImages.Children, guessImage);
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

        #endregion

        #region Private Methods

        private bool AddFolder(string folder)
        {
            int prevCount = _images.Count;

            string[] childFolders = Directory.GetDirectories(folder);
            if (childFolders.Length == 0)
            {
                #region Single Folder

                string categoryName = System.IO.Path.GetFileName(folder);

                foreach (string filename in Directory.GetFiles(folder))
                {
                    try
                    {
                        AddImage(filename, categoryName);
                    }
                    catch (Exception)
                    {
                        // Probably not an image
                        continue;
                    }
                }

                #endregion
            }
            else
            {
                #region Child Folders

                foreach (string categoryFolder in childFolders)
                {
                    string categoryName = System.IO.Path.GetFileName(categoryFolder);

                    foreach (string filename in Directory.GetFiles(categoryFolder))
                    {
                        try
                        {
                            AddImage(filename, categoryName);
                        }
                        catch (Exception)
                        {
                            // Probably not an image
                            continue;
                        }
                    }
                }

                #endregion
            }

            lblNumImages.Text = _images.Count.ToString("N0");

            return prevCount != _images.Count;      // if the count is unchanged, then no images were added
        }
        private void AddImage(string filename, string category)
        {
            string uniqueID = Guid.NewGuid().ToString();

            // Try to read as a bitmap.  The caller should have a catch block that will handle non images (better to do this now than bomb later)
            BitmapSource bitmap = new BitmapImage(new Uri(filename));

            // Build entry
            FeatureRecognizer_Image entry = new FeatureRecognizer_Image()
            {
                Category = category,
                UniqueID = uniqueID,
                Filename = filename,
                //ImageControl = FeatureRecognizer.GetTreeviewImageCtrl(new BitmapImage(new Uri(filename))),
                //Bitmap = bitmap,
            };

            // Store it
            //FeatureRecognizer.AddImage(entry, _images, treeImages, cboImageLabel);
            _images.Add(entry);
        }

        private void ResetTraining()
        {
            _patternStorage = null;
            panelTrainingImages.Children.Clear();

            if (_images.Count == 0)
            {
                return;
            }

            #region choose images

            int numImages;
            if (!int.TryParse(txtTrainCount.Text, out numImages))
            {
                MessageBox.Show("Couldn't parse count as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            numImages = Math.Min(_images.Count, numImages);

            var trainImages = UtilityCore.RandomRange(0, _images.Count, numImages).
                Select(o => new
                {
                    File = _images[o],
                    Conv = GetTrainingImage(_images[o], _trainKernel),
                }).
                ToArray();

            #endregion

            //_patternStorage = new RandomPatternStorage();
            _patternStorage = new Hopfield(IMAGESIZE * IMAGESIZE, 0, 1, .9);

            #region show thumbnails

            // Display thumbnails
            //< !--Run them through a KMeans, then sort in 1D-- >
            //< !--Show full resolution over the canvas on mouseover-- >
            //< !--Show full resolution under the canvas on click-- >

            foreach (var trainImage in trainImages)
            {
                double widthHeight = Math.Sqrt(trainImage.Conv.Length);     // they should be square
                int widthHeightInt = widthHeight.ToInt_Round();
                if (!Math1D.IsNearValue(widthHeight, widthHeightInt))
                {
                    throw new ApplicationException("Expected square images");
                }

                double[] imageConv = trainImage.Conv;
                imageConv = _patternStorage.Convert_Local_External(imageConv);

                BitmapSource source;
                //if (isColor)
                //{
                //    source = UtilityWPF.GetBitmap_RGB(example, width, height);
                //}
                //else
                //{
                source = UtilityWPF.GetBitmap(imageConv, widthHeightInt, widthHeightInt);
                //}

                Image image = new Image()
                {
                    Source = source,
                    Width = source.PixelWidth,      // if this isn't set, the image will take up all of the width, and be huge
                    Height = source.PixelHeight,
                    Margin = new Thickness(8),
                };

                panelTrainingImages.Children.Add(image);
            }

            #endregion

            _patternStorage.AddItems(trainImages.Select(o => o.Conv).ToArray());
        }

        private static double[] GetTrainingImage(FeatureRecognizer_Image image, ConvolutionBase2D kernel)
        {
            // Enlarge the initial image by the kernel's reduction so that after convolution, it is the desired size
            VectorInt reduction = kernel.GetReduction();
            if (reduction.X != reduction.Y)
            {
                throw new ApplicationException(string.Format("Kernel should be square: {0}x{1}", reduction.X, reduction.Y));
            }

            BitmapSource bitmap = new BitmapImage(new Uri(image.Filename));
            bitmap = UtilityWPF.ResizeImage(bitmap, IMAGESIZE + reduction.X, true);

            Convolution2D retVal = UtilityWPF.ConvertToConvolution(bitmap, 1d);
            if (retVal.Width != retVal.Height)
            {
                retVal = Convolutions.ExtendBorders(retVal, IMAGESIZE + reduction.X, IMAGESIZE + reduction.X);        //NOTE: width or height is already the desired size, this will just enlarge the other to make it square
            }

            retVal = Convolutions.Convolute(retVal, kernel);
            retVal = Convolutions.Abs(retVal);

            // It looks better when it's black on white
            double[] inverted = retVal.Values.
                Select(o => 1d - o).
                ToArray();

            return inverted;
        }

        private void SetInkColor()
        {
            if (radPenDark.IsChecked.Value)
            {
                penAttrib.Color = UtilityWPF.ColorFromHex("A0A0A0");
                penAttrib.Width = 45;
                penAttrib.Height = 45;
            }
            else if (radPenMed.IsChecked.Value)
            {
                penAttrib.Color = UtilityWPF.ColorFromHex("C0C0C0");
                penAttrib.Width = 25;
                penAttrib.Height = 25;
            }
            else if (radPenLight.IsChecked.Value)
            {
                penAttrib.Color = UtilityWPF.ColorFromHex("E0E0E0");
                penAttrib.Width = 10;
                penAttrib.Height = 10;
            }
            else
            {
                MessageBox.Show("", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        private static void DrawImage(UIElementCollection panel, double[] values)
        {
            Image image = new Image();

            DrawImage(image, values);

            panel.Add(image);
        }
        private static void DrawImage(Image image, double[] values)
        {
            double widthHeight = Math.Sqrt(values.Length);     // they should be square
            int widthHeightInt = widthHeight.ToInt_Round();
            if (!Math1D.IsNearValue(widthHeight, widthHeightInt))
            {
                throw new ApplicationException("Expected square images");
            }

            BitmapSource source;
            //if (isColor)
            //{
            //    source = UtilityWPF.GetBitmap_RGB(example, width, height);
            //}
            //else
            //{
            source = UtilityWPF.GetBitmap(values, widthHeightInt, widthHeightInt);
            //}

            image.Source = source;
            image.Width = source.PixelWidth;      // if this isn't set, the image will take up all of the width, and be huge
            image.Height = source.PixelHeight;
        }

        #endregion
    }

    #region interface: INNPatternStorage

    public interface INNPatternStorage
    {
        void AddItems(IEnumerable<double[]> items);

        double[][] Recognize(double[] item);

        /// <summary>
        /// This converts the item to internal, then back to external
        /// </summary>
        /// <remarks>
        /// This is just for debugging reasons (since the floating point values get converted to bools, this is lossy)
        /// </remarks>
        double[] Convert_Local_External(double[] item);
    }

    #endregion
    #region class: RandomPatternStorage

    public class RandomPatternStorage : INNPatternStorage
    {
        private readonly List<double[]> _items = new List<double[]>();

        public void AddItems(IEnumerable<double[]> items)
        {
            _items.AddRange(items);
        }

        public double[][] Recognize(double[] item)
        {
            if (_items.Count == 0)
            {
                return null;
            }

            int returnCount = StaticRandom.Next(Math.Min(4, _items.Count));

            return UtilityCore.RandomRange(0, _items.Count, returnCount).
                Select(o => _items[o]).
                ToArray();
        }

        public double[] Convert_Local_External(double[] item)
        {
            return item;
        }
    }

    #endregion
    #region class: Hopfield

    //http://web.cs.ucla.edu/~rosen/161/notes/hopfield.html
    public class Hopfield : INNPatternStorage
    {
        #region Declaration Section

        // Attempt1
        //private const double LOCAL_LOW = 0d;
        //private const double LOCAL_HIGH = 1d;
        //private const double LOCAL_MID = .5d;

        private const double LOCAL_LOW = -1d;
        private const double LOCAL_HIGH = 1d;
        private const double LOCAL_MID = 0d;

        private readonly int _nodeCount;

        private readonly double _lowValue;
        private readonly double _highValue;
        private readonly double _midValue;

        /// <summary>
        /// Item1=External Vector
        /// Item2=Local Vector
        /// </summary>
        private readonly List<Tuple<double[], double[]>> _storedItems = new List<Tuple<double[], double[]>>();

        private double[,] _weights = null;

        #endregion

        #region Constructor

        /// <summary>
        /// The hopfield can only store booleans.  So low and high tell how to map from external to local values
        /// </summary>
        /// <param name="midValue">
        /// If left null, this will just be the value between low and high.
        /// But if you want to exaggerate lows, make this larger than that average (because all values less than this go low).  Or the
        /// other way to exaggerate highs.
        /// </param>
        public Hopfield(int nodeCount, double lowValue, double highValue, double? midValue = null)
        {
            _nodeCount = nodeCount;
            _lowValue = lowValue;
            _highValue = highValue;
            _midValue = midValue ?? Math1D.Avg(lowValue, highValue);
        }

        #endregion

        #region Public Methods

        public void AddItems(IEnumerable<double[]> items)
        {
            _storedItems.AddRange(items.Select(o => Tuple.Create(o, ConvertToLocal(o, _midValue))));

            _weights = CalculateWeights(_storedItems.Select(o => o.Item2).ToArray(), _nodeCount);
        }

        public double[][] Recognize_RAND(double[] item)
        {
            if (_storedItems.Count == 0)
            {
                return null;
            }

            int returnCount = StaticRandom.Next(Math.Min(4, _storedItems.Count));

            return UtilityCore.RandomRange(0, _storedItems.Count, returnCount).
                Select(o => ConvertToExternal(_storedItems[o].Item2, _lowValue, _highValue)).
                ToArray();
        }
        public double[][] Recognize(double[] item)
        {
            if (_storedItems.Count == 0)
            {
                return null;
            }

            double[] retVal = ConvertToLocal(item, _midValue);

            bool hadChange = false;
            do
            {
                hadChange = false;

                foreach (int index in UtilityCore.RandomRange(0, item.Length))
                {
                    double newValue = GetValueAtIndex(index, retVal, _weights);

                    if (newValue != retVal[index])
                    {
                        hadChange = true;
                        retVal[index] = newValue;
                    }
                }
            } while (hadChange);

            retVal = ConvertToExternal(retVal, _lowValue, _highValue);

            return new[] { retVal };
        }

        public double[] Convert_Local_External(double[] item)
        {
            return ConvertToExternal(ConvertToLocal(item, _midValue), _lowValue, _highValue);
        }

        #endregion

        #region Private Methods

        // These map vectors tofrom external -> local (low/high -> -1/1)
        private static double[] ConvertToLocal(double[] input, double midValue)
        {
            return input.
                Select(o => o <= midValue ? LOCAL_LOW : LOCAL_HIGH).
                ToArray();
        }
        private static double[] ConvertToExternal(double[] input, double lowValue, double highValue)
        {
            return input.
                Select(o => o <= LOCAL_MID ? lowValue : highValue).
                ToArray();
        }

        private static double[,] CalculateWeights_ATTEMPT1(double[][] items, int nodeCount)
        {
            double[,] retVal = new double[nodeCount, nodeCount];

            for (int i = 0; i < nodeCount; i++)
            {
                for (int j = 0; j < nodeCount; j++)
                {
                    if (i == j)
                    {
                        continue;
                    }

                    double sum = 0;
                    for (int cntr = 0; cntr < items.Length; cntr++)
                    {
                        sum += ((2 * items[cntr][i]) - 1) * ((2 * items[cntr][j]) - 1);
                    }

                    retVal[i, j] = sum;
                    retVal[j, i] = sum;
                }
            }

            //List<double> test = new List<double>();
            //for (int i = 0; i < nodeCount; i++)
            //    for (int j = 0; j < nodeCount; j++)
            //        test.Add(retVal[i, j]);

            //test = test.Distinct().ToList();

            return retVal;
        }
        private static double[,] CalculateWeights(double[][] items, int nodeCount)
        {
            // There seems to be two main algorithms.  This is Hebbian, which is basic, but not as good
            // A better algorithm for Hopfield is Storkey
            //
            // But, it sounds like Restricted Boltzmann is a better structure than Hopfield

            // I believe this algorithm is correct, but not useful in practice.  I think there is constructive interference going on.  So you can't just
            // add images, you also need to smooth out peaks
            //https://www.youtube.com/watch?v=3WgIxMFyfdI

            double[,] retVal = new double[nodeCount, nodeCount];

            double oneOverN = 1d / items.Length;

            for (int i = 0; i < nodeCount; i++)
            {
                for (int j = 0; j < nodeCount; j++)
                {
                    if (i == j)
                    {
                        continue;
                    }

                    double sum = 0;
                    for (int cntr = 0; cntr < items.Length; cntr++)
                    {
                        sum += items[cntr][i] * items[cntr][j];
                    }

                    sum *= oneOverN;

                    retVal[i, j] = sum;
                    retVal[j, i] = sum;
                }
            }

            //List<double> test = new List<double>();
            //for (int i = 0; i < nodeCount; i++)
            //    for (int j = 0; j < nodeCount; j++)
            //        test.Add(retVal[i, j]);

            //test = test.Distinct().ToList();

            return retVal;
        }

        private static double GetValueAtIndex_ATTEMPT1(int index, double[] item, double[,] weights)
        {
            double sum = 0d;

            for (int cntr = 0; cntr < item.Length; cntr++)
            {
                if (cntr == index)
                {
                    continue;
                }

                sum += item[cntr] * weights[cntr, index];
            }

            if (sum >= 0)
            {
                return LOCAL_HIGH;
            }
            else
            {
                return LOCAL_LOW;
            }
        }
        private static double GetValueAtIndex(int index, double[] item, double[,] weights)
        {
            double sum = 0d;

            for (int cntr = 0; cntr < item.Length; cntr++)
            {
                if (cntr == index)
                {
                    continue;
                }

                sum += item[cntr] * weights[cntr, index];
            }

            if (sum >= item[index])
            {
                return LOCAL_HIGH;
            }
            else
            {
                return LOCAL_LOW;
            }
        }

        #endregion
    }

    #endregion
    #region class: RestrictedBoltzmann

    public class RestrictedBoltzmann : INNPatternStorage
    {
        #region Declaration Section


        #endregion

        public void AddItems(IEnumerable<double[]> items)
        {
        }

        public double[][] Recognize(double[] item)
        {
            return null;
        }

        public double[] Convert_Local_External(double[] item)
        {
            return item;
        }
    }

    #endregion
    #region class: HopfieldNetworkOptions

    /// <summary>
    /// This gets serialized to file
    /// </summary>
    public class HopfieldNetworkOptions
    {
        public string[] ImageFolders { get; set; }
    }

    #endregion
}
