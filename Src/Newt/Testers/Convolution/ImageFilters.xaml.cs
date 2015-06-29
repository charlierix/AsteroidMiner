using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
using System.Windows.Shapes;
using System.Windows.Threading;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Microsoft.Win32;

namespace Game.Newt.Testers.Convolution
{
    //TODO: Support filters other than convolutions:
    //http://homepages.inf.ed.ac.uk/rbf/HIPR2/median.htm
    public partial class ImageFilters : Window
    {
        #region Class: DragDataObject

        internal class DragDataObject
        {
            #region Constructor

            public DragDataObject(int index, Border control, ConvolutionBase2D kernel)
            {
                this.Index = index;
                this.Control = control;
                this.Kernel = kernel;
            }

            #endregion

            public readonly int Index;
            public readonly Border Control;

            public readonly ConvolutionBase2D Kernel;
        }

        #endregion

        #region Declaration Section

        internal const string DATAFORMAT_KERNEL = "data format - ImageFilters - kernel";

        /// <summary>
        /// Converting to gray is a bit expensive, so cache it
        /// </summary>
        private Convolution2D _origImageGrays = null;

        private List<ConvolutionBase2D> _kernels = new List<ConvolutionBase2D>();
        private int _selectedKernelIndex = -1;

        private Tuple<Border, int> _mouseDownOn = null;

        private List<Window> _childWindows = new List<Window>();

        private readonly DropShadowEffect _selectEffect;

        private readonly ContextMenu _kernelContextMenu;

        private DispatcherTimer _refreshTimer = null;

        private bool _initialized = false;

        #endregion

        #region Constructor

        public ImageFilters()
        {
            InitializeComponent();

            // Selected effect
            _selectEffect = new DropShadowEffect()
            {
                Direction = 0,
                ShadowDepth = 0,
                BlurRadius = 40,
                Color = UtilityWPF.ColorFromHex("FFFFFF"),
                Opacity = 1,
            };

            // Context Menu
            _kernelContextMenu = (ContextMenu)this.Resources["kernelContextMenu"];

            // Source Image
            originalImage.Source = DownloadImage(GetImageURL());

            // Kernels
            AddDefaultKernels_Gaussian();
            AddDefaultKernels_Edges();
            AddDefaultKernels_Composite();
            AddRandomKernels(2, false);
            AddRandomKernels(2, true);

            // Timer
            _refreshTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(50),
                IsEnabled = false,
            };

            _refreshTimer.Tick += RefreshTimer_Tick;

            _initialized = true;
        }

        #endregion

        #region Event Listeners

        private void Window_Closed(object sender, EventArgs e)
        {
            try
            {
                foreach (Window child in _childWindows.ToArray())        // taking the array so that the list can be removed from while in the for loop (it shouldn't happen, but just in case)
                {
                    child.Closed -= Child_Closed;
                    child.Close();
                }

                _childWindows.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnRandomImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                originalImage.Source = DownloadImage(GetImageURL());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnBrowseImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.Multiselect = false;
                dialog.Title = "Please select an image";
                bool? result = dialog.ShowDialog();
                if (result == null || !result.Value)
                {
                    return;
                }

                originalImage.Source = new BitmapImage(new Uri(dialog.FileName));
                _origImageGrays = null;
            }
            catch (NotSupportedException)
            {
                MessageBox.Show("Not an image file", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void chkSubtract_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                if (_selectedKernelIndex >= 0 && _selectedKernelIndex < _kernels.Count)
                {
                    ApplyFilter(_kernels[_selectedKernelIndex]);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void trkGain_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                lblGain.Content = trkGain.Value.ToStringSignificantDigits(2);

                // Need to reapply the filter, but wait until they finish dragging the slider around
                _refreshTimer.IsEnabled = false;
                _refreshTimer.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void trkIterations_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                lblIterations.Content = trkIterations.Value.ToString();

                // Need to reapply the filter, but wait until they finish dragging the slider around
                _refreshTimer.IsEnabled = false;
                _refreshTimer.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                _refreshTimer.IsEnabled = false;

                if (_selectedKernelIndex >= 0 && _selectedKernelIndex < _kernels.Count)
                {
                    ApplyFilter(_kernels[_selectedKernelIndex]);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CompositeFilter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CompositeFilter composite = new CompositeFilter()
                {
                    Owner = this,       // setting this so it stays on top of this window
                };

                composite.SaveRequested += Composite_SaveRequested;
                composite.Closed += Child_Closed;

                _childWindows.Add(composite);

                composite.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void NewFilter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ImageFilterPainter painter = new ImageFilterPainter();

                painter.SaveRequested += Painter_SaveRequested;
                painter.Closed += Child_Closed;

                _childWindows.Add(painter);

                painter.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Composite_SaveRequested(object sender, ConvolutionSet2D e)
        {
            try
            {
                AddKernel(e);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Painter_SaveRequested(object sender, Convolution2D e)
        {
            try
            {
                AddKernel(e);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Child_Closed(object sender, EventArgs e)
        {
            try
            {
                Window senderCast = sender as Window;
                if (senderCast == null)
                {
                    return;
                }

                senderCast.Closed -= Child_Closed;

                _childWindows.Remove(senderCast);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void panelKernels_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.ChangedButton != MouseButton.Left)
                {
                    return;
                }

                _mouseDownOn = null;

                Tuple<Border, int> clickedCtrl = GetSelectedKernel(e.OriginalSource);
                if (clickedCtrl == null)
                {
                    return;
                }

                _mouseDownOn = clickedCtrl;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void panelKernels_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (_mouseDownOn == null)
                {
                    return;
                }

                // Start Drag
                DragDataObject dragObject = new DragDataObject(_mouseDownOn.Item2, _mouseDownOn.Item1, _kernels[_mouseDownOn.Item2]);
                DataObject dragData = new DataObject(DATAFORMAT_KERNEL, dragObject);
                DragDrop.DoDragDrop(_mouseDownOn.Item1, dragData, DragDropEffects.Move);

                _mouseDownOn = null;        // this way another drag won't start until they release and reclick
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void panelKernels_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                _mouseDownOn = null;

                Tuple<Border, int> clickedCtrl = GetSelectedKernel(e.OriginalSource);
                if (clickedCtrl == null)
                {
                    return;
                }

                SelectKernel(clickedCtrl.Item2);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void panelKernels_PreviewDragOver(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DATAFORMAT_KERNEL))
                {
                    e.Effects = DragDropEffects.Move;
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }

                e.Handled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void panelKernels_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DATAFORMAT_KERNEL))
                {
                    var dropped = (DragDataObject)e.Data.GetData(DATAFORMAT_KERNEL);

                    int insertIndex = GetDropInsertIndex(panelKernels, e, true);
                    if (insertIndex < 0)
                    {
                        return;
                    }

                    if (insertIndex >= dropped.Index)
                    {
                        insertIndex--;
                    }

                    panelKernels.Children.RemoveAt(dropped.Index);
                    _kernels.RemoveAt(dropped.Index);

                    panelKernels.Children.Insert(insertIndex, dropped.Control);
                    _kernels.Insert(insertIndex, dropped.Kernel);

                    SelectKernel(insertIndex);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void KernelEdit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Tuple<Border, int> kernel = GetSelectedKernel(_kernelContextMenu.PlacementTarget);
                if (kernel == null)
                {
                    MessageBox.Show("Couldn't identify filter", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ConvolutionBase2D selected = _kernels[kernel.Item2];

                if (selected is Convolution2D)
                {
                    #region Single

                    ImageFilterPainter editor = new ImageFilterPainter();

                    editor.SaveRequested += Painter_SaveRequested;
                    editor.Closed += Child_Closed;

                    _childWindows.Add(editor);

                    editor.EditKernel((Convolution2D)selected);

                    editor.Show();

                    #endregion
                }
                else if (selected is ConvolutionSet2D)
                {
                    #region Set

                    CompositeFilter composite = new CompositeFilter((ConvolutionSet2D)selected)
                    {
                        // I don't want this one on top
                        //Owner = this,       // setting this so it stays on top of this window
                    };

                    composite.SaveRequested += Composite_SaveRequested;
                    composite.Closed += Child_Closed;

                    _childWindows.Add(composite);

                    composite.Show();

                    #endregion
                }
                else
                {
                    MessageBox.Show("Unknown type of kernel: " + _kernels[kernel.Item2].GetType().ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void KernelDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Tuple<Border, int> kernel = GetSelectedKernel(_kernelContextMenu.PlacementTarget);
                if (kernel == null)
                {
                    MessageBox.Show("Couldn't identify filter", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                panelKernels.Children.Remove(kernel.Item1);
                _kernels.RemoveAt(kernel.Item2);

                if (_selectedKernelIndex == kernel.Item2)
                {
                    _selectedKernelIndex = -1;
                }
                else if (_selectedKernelIndex > kernel.Item2)
                {
                    _selectedKernelIndex--;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Internal Methods

        internal static Border GetKernelThumbnail(ConvolutionBase2D kernel, int thumbSize, ContextMenu contextMenu)
        {
            if (kernel is Convolution2D)
            {
                return GetKernelThumbnail_Single((Convolution2D)kernel, thumbSize, contextMenu);
            }
            else if (kernel is ConvolutionSet2D)
            {
                return GetKernelThumbnail_Set((ConvolutionSet2D)kernel, thumbSize, contextMenu);
            }
            else
            {
                throw new ArgumentException("Unknown type of kernel: " + kernel.GetType().ToString());
            }
        }

        /// <summary>
        /// This isn't for processing, just a visual to show to the user
        /// </summary>
        internal static BitmapSource GetKernelBitmap(Convolution2D kernel, int sizeMult = 20, bool isNegativeRedBlue = true)
        {
            double min = kernel.Values.Min(o => o);
            double max = kernel.Values.Max(o => o);
            double absMax = Math.Max(Math.Abs(min), Math.Abs(max));

            Color[] colors = null;
            if (!kernel.IsNegPos)
            {
                // 0 to 1
                colors = kernel.Values.
                    Select(o => GetKernelPixelColor_ZeroToOne(o, max)).
                    ToArray();
            }
            else if (isNegativeRedBlue)
            {
                // -1 to 1 (red-blue)
                colors = kernel.Values.
                    Select(o => GetKernelPixelColor_NegPos_RedBlue(o, absMax)).
                    ToArray();
            }
            else
            {
                // -1 to 1 (black-white)
                colors = kernel.Values.
                    Select(o => GetKernelPixelColor_NegPos_BlackWhite(o, absMax)).
                    ToArray();
            }

            return UtilityWPF.GetBitmap_Aliased(colors, kernel.Width, kernel.Height, kernel.Width * sizeMult, kernel.Height * sizeMult);
        }
        internal static Color GetKernelPixelColor(double value, double min, double max, double absMax, bool isZeroToOne, bool isNegativeRedBlue = true)
        {
            if (isZeroToOne)
            {
                return GetKernelPixelColor_ZeroToOne(value, max);
            }
            else if (isNegativeRedBlue)
            {
                return GetKernelPixelColor_NegPos_RedBlue(value, absMax);
            }
            else
            {
                return GetKernelPixelColor_NegPos_BlackWhite(value, absMax);
            }
        }
        internal static Color GetKernelPixelColor_ZeroToOne(double value, double max)
        {
            if (max.IsNearZero())
            {
                return Colors.Black;
            }

            double scaled = (value / max) * 255d;       // need to scale to max, because the sum of the cells is 1.  So if it's not scaled, the bitmap will be nearly black
            if (scaled < 0)
            {
                scaled = 0;
            }

            byte rgb = Convert.ToByte(Math.Round(scaled));
            return Color.FromRgb(rgb, rgb, rgb);
        }
        internal static Color GetKernelPixelColor_NegPos_RedBlue(double value, double absMax)
        {
            if (absMax.IsNearZero())
            {
                return Colors.White;
            }

            byte[] white = new byte[] { 255, 255, 255, 255 };

            double scaled = (Math.Abs(value) / absMax) * 255d;      //NOTE: Can't use a posMax and negMax, because the black/white will be deceptive
            byte opacity = Convert.ToByte(Math.Round(scaled));

            byte[] color = new byte[4];
            color[0] = opacity;
            color[1] = Convert.ToByte(value < 0 ? 255 : 0);
            color[2] = 0;
            color[3] = Convert.ToByte(value > 0 ? 255 : 0);

            color = UtilityWPF.OverlayColors(new[] { white, color });

            return Color.FromRgb(color[1], color[2], color[3]);     // the white background is fully opaque, so there's no need for the alpha overload
        }
        internal static Color GetKernelPixelColor_NegPos_BlackWhite(double value, double absMax)
        {
            if (absMax.IsNearZero())
            {
                return Colors.Gray;
            }

            double offset = (value / absMax) * 127d;        //NOTE: Can't use a posMax and negMax, because the black/white will be deceptive

            byte rgb = Convert.ToByte(128 + Math.Round(offset));

            return Color.FromRgb(rgb, rgb, rgb);
        }

        internal static int GetDropInsertIndex(Panel panel, DragEventArgs e, bool isHorizontal)
        {
            if (!isHorizontal)
            {
                throw new ApplicationException("Handle non horizontal panels");
            }

            if (panel.Children.Count == 0)
            {
                return 0;
            }

            // Get the position of the drop location relative to each child
            var childPositions = new List<Tuple<int, FrameworkElement, Point>>();

            foreach (FrameworkElement child in panel.Children)
            {
                //Vector halfSize = new Vector(child.ActualWidth / 2d, child.ActualHeight / 2d);
                Point relativePos = e.GetPosition(child);// +halfSize;      // putting position in the center just seems to screw it up, not sure why

                childPositions.Add(Tuple.Create(childPositions.Count, child, relativePos));
            }

            // Sort them
            var distances = childPositions.
                Select(o => new { Index = o.Item1, Border = o.Item2, Position = o.Item3, DistanceSquared = o.Item3.ToVector().LengthSquared });

            var closest = distances.
                Where(o => o.Position.Y > 0).       // this needs to be here, or the row below will often be considered closer (because the position is the control's top/left)
                OrderBy(o => o.DistanceSquared).
                FirstOrDefault();

            if (closest == null)
            {
                // They dropped above the first row
                closest = distances.
                    OrderBy(o => o.DistanceSquared).
                    FirstOrDefault();
            }

            if (closest == null)
            {
                // Not sure what happened
                return -1;
            }

            int retVal = -1;

            if (closest.Position.X < 0)
            {
                // Place it in front of the closest object
                retVal = closest.Index;
            }
            else
            {
                // Place it after the closest object
                retVal = closest.Index + 1;
            }

            return retVal;
        }

        #endregion

        #region Private Methods

        private string GetImageURL(bool? isColor = null)
        {
            // This gets a grayscale image 400x200
            //http://lorempixel.com/g/400/200

            // Using the image's width and height doesn't work.  Somewhere it went to zero
            //return string.Format("http://lorempixel.com/g/{0}/{1}", Convert.ToInt32(originalImage.ActualWidth).ToString(), Convert.ToInt32(originalImage.ActualHeight).ToString());

            if (isColor ?? StaticRandom.NextBool())
            {
                return "http://lorempixel.com/300/300";
            }
            else
            {
                return "http://lorempixel.com/g/300/300";
            }
        }

        private Tuple<Border, int> GetSelectedKernel(object source)
        {
            Border clickedCtrl;

            if (source is Border)
            {
                clickedCtrl = (Border)source;
            }
            else if (source is Image)
            {
                Image image = (Image)source;
                clickedCtrl = (Border)image.Parent;
            }
            else if (source == panelKernels)
            {
                return null;
            }
            else
            {
                throw new ApplicationException("Expected an image or border");
            }

            // Get the index
            int index = panelKernels.Children.IndexOf(clickedCtrl);
            if (index < 0)
            {
                throw new ApplicationException("Couldn't find clicked item");
            }

            if (_kernels.Count != panelKernels.Children.Count)
            {
                throw new ApplicationException("_kernels and panelKernels are out of sync");
            }

            return Tuple.Create(clickedCtrl, index);
        }

        //Got this here:
        //http://www.dotnetspider.com/resources/42565-Download-images-from-URL-using-C.aspx
        private BitmapSource DownloadImage(string imageUrl)
        {
            try
            {
                _origImageGrays = null;

                HttpWebRequest webRequest = (HttpWebRequest)System.Net.HttpWebRequest.Create(imageUrl);
                webRequest.AllowWriteStreamBuffering = true;
                webRequest.Timeout = 30000;

                if (_webResponse != null)
                {
                    _webResponse.Close();
                    _webResponse = null;
                }

                var retVal = new BitmapImage();

                retVal.DownloadCompleted += webResponse_DownloadCompleted;

                _webResponse = webRequest.GetResponse();

                Stream stream = _webResponse.GetResponseStream();

                retVal.BeginInit();
                retVal.CacheOption = BitmapCacheOption.OnLoad;
                retVal.StreamSource = stream;
                retVal.EndInit();
                //retVal.Freeze();      // this throws an exception

                // Can't close yet.  It seems to be downloading async, so wait for the finished event
                //webResponse.Close();

                return retVal;
            }
            catch (Exception)
            {
                return null;
            }
        }
        private WebResponse _webResponse = null;
        private void webResponse_DownloadCompleted(object sender, EventArgs e)
        {
            if (_webResponse != null)
            {
                _webResponse.Close();
                _webResponse = null;
            }
        }

        private void AddKernel(ConvolutionBase2D kernel)
        {
            Border border = GetKernelThumbnail(kernel, 40, _kernelContextMenu);

            // Store them
            panelKernels.Children.Add(border);
            _kernels.Add(kernel);
        }
        private void AddDefaultKernels_Gaussian()
        {
            int[] sizes = new[] { 3, 4, 5, 6, 7, 9, 15, 20, 30 };

            foreach (int size in sizes)
            {
                AddKernel(Convolutions.GetGaussian(size, 1));
            }

            foreach (int size in sizes)
            {
                AddKernel(Convolutions.GetGaussian(size, 2));
            }
        }
        private void AddDefaultKernels_Edges()
        {
            //TODO:
            //  - | / \ + X
            //  L ^
            //  O (      also quarter circle

            //maybe:
            // T
            //  filled circle
            // diamond
            // =


            // Let them specify line width



            AddKernel(Convolutions.GetEdge_Sobel(true));
            AddKernel(Convolutions.GetEdge_Sobel(false));

            AddKernel(Convolutions.GetEdge_Prewitt(true));
            AddKernel(Convolutions.GetEdge_Prewitt(false));

            AddKernel(Convolutions.GetEdge_Compass(true));
            AddKernel(Convolutions.GetEdge_Compass(false));

            AddKernel(Convolutions.GetEdge_Kirsch(true));
            AddKernel(Convolutions.GetEdge_Kirsch(false));
        }
        private void AddDefaultKernels_Composite()
        {
            // Gaussian Subtract
            AddKernel(new ConvolutionSet2D(new[] { Convolutions.GetGaussian(3, 1) }, SetOperationType.Subtract));

            // MaxAbs Sobel
            Convolution2D vert = Convolutions.GetEdge_Sobel(true);
            Convolution2D horz = Convolutions.GetEdge_Sobel(false);
            ConvolutionSet2D first = null;

            foreach (int gain in new[] { 1, 2, 4 })
            {
                var singles = new[]
                {
                    new Convolution2D(vert.Values, vert.Width, vert.Height, vert.IsNegPos, gain),
                    new Convolution2D(horz.Values, horz.Width, horz.Height, horz.IsNegPos, gain),
                };

                ConvolutionSet2D set = new ConvolutionSet2D(singles, SetOperationType.MaxOf);
                AddKernel(set);

                first = first ?? set;
            }

            // Gausian then edge
            foreach (int size in new[] { 3, 5, 7 })
            {
                ConvolutionBase2D[] convs = new ConvolutionBase2D[]
                {
                    Convolutions.GetGaussian(size, 1),
                    first,
                };

                AddKernel(new ConvolutionSet2D(convs, SetOperationType.Standard));
            }
        }
        private void AddRandomKernels(int count, bool isNegPos, bool? isSquare = null)
        {
            for (int cntr = 0; cntr < count; cntr++)
            {
                bool isSquareActual = isSquare ?? StaticRandom.NextBool();

                // Create kernel
                Convolution2D kernel;
                if (isNegPos)
                {
                    kernel = GetRandomFilter_PosNeg(isSquareActual);
                }
                else
                {
                    kernel = GetRandomFilter_Positive(isSquareActual);
                }

                AddKernel(kernel);
            }
        }

        private static Convolution2D GetRandomFilter_Positive(bool isSquare)
        {
            Random rand = StaticRandom.GetRandomForThread();

            var size = GetFilterSize(rand, isSquare);

            double[] values = Enumerable.Range(0, size.Item1 * size.Item2).
                Select(o => rand.NextDouble()).
                ToArray();

            values = Convolutions.ToUnit(values);

            return new Convolution2D(values, size.Item1, size.Item2, false);
        }
        private static Convolution2D GetRandomFilter_PosNeg(bool isSquare)
        {
            Random rand = StaticRandom.GetRandomForThread();

            var size = GetFilterSize(rand, isSquare);

            double[] values = Enumerable.Range(0, size.Item1 * size.Item2).
                Select(o => rand.NextDouble(-1, 1)).
                ToArray();

            if (values.All(o => o > 0) || values.All(o => o < 0))
            {
                // They are all positive or all negative.  Flip one of them
                values[rand.Next(values.Length)] *= -1d;
            }

            values = Convolutions.ToUnit(values);

            return new Convolution2D(values, size.Item1, size.Item2, true);
        }

        private static Tuple<int, int> GetFilterSize(Random rand, bool isSquare)
        {
            int width, height;
            if (isSquare)
            {
                height = width = rand.Next(2, 10);
            }
            else
            {
                width = rand.Next(1, 10);
                height = rand.Next(width == 1 ? 2 : 1, 10);      // can't have a 1x1, but can have 1xN or Nx1
            }

            return Tuple.Create(width, height);
        }

        private void SelectKernel(int index)
        {
            // Set the effect
            int childIndex = 0;
            foreach (UIElement child in panelKernels.Children)
            {
                if (childIndex == index)
                {
                    child.Effect = _selectEffect;
                }
                else
                {
                    child.Effect = null;
                }

                childIndex++;
            }

            _selectedKernelIndex = index;

            // Apply this kernel to the original image
            ApplyFilter(_kernels[_selectedKernelIndex]);
        }

        private void ApplyFilter(ConvolutionBase2D kernel)
        {
            // Convert the original image to grayscale
            Convolution2D image = GetOriginalImageGrays();
            if (image == null)
            {
                // The original image is empty
                return;
            }

            bool isNegPos = false;
            Convolution2D filtered = null;

            if (kernel is Convolution2D)
            {
                #region Single

                Convolution2D kernelSingle = (Convolution2D)kernel;

                // This window builds kernels without gain or iterations, so make a clone with those tacked on
                Convolution2D kernelFinal = new Convolution2D(
                    kernelSingle.Values,
                    kernelSingle.Width,
                    kernelSingle.Height,
                    kernelSingle.IsNegPos,
                    trkGain.Value,
                    Convert.ToInt32(trkIterations.Value),
                    chkExpandBorder.IsChecked.Value);

                filtered = Convolutions.Convolute(image, kernelFinal);

                isNegPos = kernelSingle.IsNegPos;

                if (chkSubtract.IsChecked.Value)
                {
                    filtered = Convolutions.Subtract(image, filtered);
                    isNegPos = true;
                }

                #endregion
            }
            else if (kernel is ConvolutionSet2D)
            {
                #region Set

                ConvolutionSet2D kernelSet = (ConvolutionSet2D)kernel;

                filtered = Convolutions.Convolute(image, kernelSet);

                isNegPos = kernelSet.IsNegPos;

                #endregion
            }
            else
            {
                throw new ArgumentException("Unknown type of kernel: " + kernel.GetType().ToString());
            }

            #region Convert filtered to gray

            Color[] filteredColors;

            //double min = filtered.Values.Min();
            //double max = filtered.Values.Max();

            if (isNegPos)
            {
                filteredColors = filtered.Values.Select(o =>
                {
                    double offset = (o / 255d) * 127d;
                    double rgbDbl = 128 + Math.Round(offset);

                    if (rgbDbl < 0) rgbDbl = 0;
                    else if (rgbDbl > 255) rgbDbl = 255;

                    byte rgb = Convert.ToByte(rgbDbl);
                    return Color.FromRgb(rgb, rgb, rgb);
                }).
                ToArray();
            }
            else
            {
                filteredColors = filtered.Values.Select(o =>
                {
                    double rgbDbl = Math.Round(o);

                    if (rgbDbl < 0) rgbDbl = 0;
                    else if (rgbDbl > 255) rgbDbl = 255;

                    byte rgb = Convert.ToByte(rgbDbl);
                    return Color.FromRgb(rgb, rgb, rgb);
                }).
                ToArray();
            }

            #endregion

            // Show Filtered
            modifiedImage.Source = UtilityWPF.GetBitmap(filteredColors, filtered.Width, filtered.Height);
            //modifiedImage.Width = filtered.Width;
            //modifiedImage.Height = filtered.Height;
        }

        /// <summary>
        /// Converts the original image into a grayscale
        /// </summary>
        private Convolution2D GetOriginalImageGrays()
        {
            if (_origImageGrays == null && originalImage.Source != null)
            {
                BitmapSource bitmap = (BitmapSource)originalImage.Source;

                var colors = UtilityWPF.ConvertToColorArray(bitmap, false, Colors.Transparent);

                _origImageGrays = ((BitmapCustomCachedBytes)colors).ToConvolution();
            }

            return _origImageGrays;
        }

        private static Border GetKernelThumbnail_Single(Convolution2D kernel, int thumbSize, ContextMenu contextMenu)
        {
            // Figure out thumb size
            double width, height;
            if (kernel.Width == kernel.Height)
            {
                width = height = thumbSize;
            }
            else if (kernel.Width > kernel.Height)
            {
                width = thumbSize;
                height = Convert.ToDouble(kernel.Height) / Convert.ToDouble(kernel.Width) * thumbSize;
            }
            else
            {
                height = thumbSize;
                width = Convert.ToDouble(kernel.Width) / Convert.ToDouble(kernel.Height) * thumbSize;
            }

            int pixelWidth = Convert.ToInt32(Math.Ceiling(width / kernel.Width));
            int pixelHeight = Convert.ToInt32(Math.Ceiling(height / kernel.Height));

            int pixelMult = Math.Max(pixelWidth, pixelHeight);
            if (pixelMult < 1)
            {
                pixelMult = 1;
            }

            // Display it as a border and image
            Image image = new Image()
            {
                Source = GetKernelBitmap(kernel, pixelMult),
                Width = width,
                Height = height,
                ToolTip = string.Format("{0}x{1}", kernel.Width, kernel.Height),
            };

            Border border = new Border()
            {
                Child = image,
                BorderBrush = new SolidColorBrush(UtilityWPF.ColorFromHex("C0C0C0")),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(6),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                ContextMenu = contextMenu,
            };

            return border;
        }
        private static Border GetKernelThumbnail_Set_ATTEMPT1(ConvolutionSet2D kernel, int thumbSize, ContextMenu contextMenu)
        {
            StackPanel children = new StackPanel()
            {
                Orientation = Orientation.Horizontal,
            };

            int childSize = Convert.ToInt32(thumbSize * .9);
            double secondChildShift = thumbSize * .33;

            foreach (ConvolutionBase2D child in kernel.Convolutions)
            {
                Border childCtrl = GetKernelThumbnail(child, childSize, null);

                //NOTE: I wanted a trapazoid transform, but that is impossible with a 3x3 matrix.  The only way would be to render in 3D
                TransformGroup tranform = new TransformGroup();
                //tranform.Children.Add(new ScaleTransform(.75, 1));
                tranform.Children.Add(new SkewTransform(0, -30));
                childCtrl.LayoutTransform = tranform;

                if (children.Children.Count == 0)
                {
                    childCtrl.Margin = new Thickness(0);
                }
                else
                {
                    //tranform.Children.Add(new TranslateTransform(-secondChildShift, 0));      // doesn't work
                    childCtrl.Margin = new Thickness(-secondChildShift, 0, 0, 0);
                }

                children.Children.Add(childCtrl);
            }

            Border border = new Border()
            {
                Child = children,
                //Width = thumbSize,
                //Height = thumbSize,

                Background = new SolidColorBrush(UtilityWPF.ColorFromHex("28F2B702")),
                BorderBrush = new SolidColorBrush(UtilityWPF.ColorFromHex("F5CC05")),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(6),
                Padding = new Thickness(3),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                ContextMenu = contextMenu,
            };

            return border;
        }
        private static Border GetKernelThumbnail_Set(ConvolutionSet2D kernel, int thumbSize, ContextMenu contextMenu)
        {
            StackPanel children = new StackPanel()
            {
                Orientation = Orientation.Horizontal,
            };

            int childSize = Convert.ToInt32(thumbSize * .9);
            double secondChildShift = thumbSize * .33;

            foreach (ConvolutionBase2D child in kernel.Convolutions)
            {
                //NOTE: It doesn't work to apply skew transforms to a border that has children.  Instead, make a visual brush out of the child
                Border childCtrl = GetKernelThumbnail(child, childSize, null);
                childCtrl.Margin = new Thickness(0);

                Border actualChild = new Border();
                actualChild.Background = new VisualBrush() { Visual = childCtrl };

                double width = 0;
                double height = 0;
                if (child is Convolution2D)
                {
                    width = ((FrameworkElement)childCtrl.Child).Width;
                    height = ((FrameworkElement)childCtrl.Child).Height;
                }
                else //if(child is ConvolutionSet2D)
                {
                    // There doesn't seem to be a way to get the child composite's size (it's NaN).  Maybe there's a way to force it to do layout?
                    width = thumbSize;
                    height = thumbSize;
                }

                actualChild.Width = width;
                actualChild.Height = height;

                //NOTE: I wanted a trapazoid transform, but that is impossible with a 3x3 matrix.  The only way would be to render in 3D
                TransformGroup tranform = new TransformGroup();
                tranform.Children.Add(new ScaleTransform(.75, 1));
                tranform.Children.Add(new SkewTransform(0, -30));
                actualChild.LayoutTransform = tranform;

                if (children.Children.Count > 0)
                {
                    actualChild.Margin = new Thickness(-secondChildShift, 0, 0, 0);
                }

                actualChild.IsHitTestVisible = false;       // setting this to false so that click events come from the returned border instead of the child

                children.Children.Add(actualChild);
            }

            Border border = new Border()
            {
                Child = children,

                Background = new SolidColorBrush(UtilityWPF.ColorFromHex("28F2B702")),
                BorderBrush = new SolidColorBrush(UtilityWPF.ColorFromHex("F5CC05")),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(6),
                Padding = new Thickness(3),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                ContextMenu = contextMenu,
            };

            return border;
        }

        #endregion
    }
}
