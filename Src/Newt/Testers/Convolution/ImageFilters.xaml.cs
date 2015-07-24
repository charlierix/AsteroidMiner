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

        /// <summary>
        /// This is the kernel panel
        /// </summary>
        private Tuple<Border, int> _mouseDownOn = null;
        /// <summary>
        /// This is the original image
        /// </summary>
        private Point? _selectionStartPoint = null;

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
                Color = UtilityWPF.ColorFromHex("FFEB85"),
                Opacity = 1,
            };

            // Context Menu
            _kernelContextMenu = (ContextMenu)this.Resources["kernelContextMenu"];

            // NegPos coloring
            foreach (ConvolutionResultNegPosColoring coloring in Enum.GetValues(typeof(ConvolutionResultNegPosColoring)))
            {
                cboEdgeColors.Items.Add(coloring);
            }
            cboEdgeColors.SelectedIndex = 0;

            // Source Image
            originalImage.Source = DownloadImage(GetImageURL());

            // Kernels
            AddDefaultKernels_Gaussian();
            AddDefaultKernels_Edges_Small();
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
                //NOTE: Can't resize the image, because it's still downloading in the background
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

                BitmapSource bitmap = new BitmapImage(new Uri(dialog.FileName));

                int limit;
                if (chkLimitImageSize.IsChecked.Value && int.TryParse(txtSizeLimit.Text, out limit))
                {
                    bitmap = UtilityWPF.ResizeImage(bitmap, limit);       // this will only resize if it's too big
                }

                originalImage.Source = bitmap;
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

        private void ExtractEdgeSquare_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Convert the original image to grayscale
                Convolution2D image = GetOriginalImageGrays();
                if (image == null)
                {
                    // The original image is empty
                    MessageBox.Show("Please load an image first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Square
                RectInt rect = Convolutions.GetExtractRectangle(new VectorInt(image.Width, image.Height), true);

                // Extract
                AddKernel(image.Extract(rect, ConvolutionExtractType.Edge));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ExtractEdgeRect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Convert the original image to grayscale
                Convolution2D image = GetOriginalImageGrays();
                if (image == null)
                {
                    // The original image is empty
                    MessageBox.Show("Please load an image first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Rectangle
                RectInt rect = Convolutions.GetExtractRectangle(new VectorInt(image.Width, image.Height), false);

                // Extract
                AddKernel(image.Extract(rect, ConvolutionExtractType.Edge));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ExtractRawSquare_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Convert the original image to grayscale
                Convolution2D image = GetOriginalImageGrays();
                if (image == null)
                {
                    // The original image is empty
                    MessageBox.Show("Please load an image first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Square
                RectInt rect = Convolutions.GetExtractRectangle(new VectorInt(image.Width, image.Height), true);

                // Extract
                AddKernel(image.Extract(rect, ConvolutionExtractType.RawUnit));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ExtractRawRect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Convert the original image to grayscale
                Convolution2D image = GetOriginalImageGrays();
                if (image == null)
                {
                    // The original image is empty
                    MessageBox.Show("Please load an image first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Rectangle
                RectInt rect = Convolutions.GetExtractRectangle(new VectorInt(image.Width, image.Height), false);

                // Extract
                AddKernel(image.Extract(rect, ConvolutionExtractType.RawUnit));
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

        private void cboEdgeColors_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

        private void grdOrigImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (_selectionStartPoint != null)       // this should never happen
                {
                    return;
                }

                _selectionStartPoint = e.GetPosition(grdOrigImage);
                grdOrigImage.CaptureMouse();

                // Initial placement of the drag selection box
                Canvas.SetLeft(selectionBox, _selectionStartPoint.Value.X);
                Canvas.SetTop(selectionBox, _selectionStartPoint.Value.Y);
                selectionBox.Width = 0;
                selectionBox.Height = 0;

                // Make the drag selection box visible.
                selectionBox.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void grdOrigImage_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (_selectionStartPoint == null)
                {
                    return;
                }

                Point mousePos = e.GetPosition(grdOrigImage);

                double minX = Math.Min(_selectionStartPoint.Value.X, mousePos.X);
                double minY = Math.Min(_selectionStartPoint.Value.Y, mousePos.Y);
                double maxX = Math.Max(_selectionStartPoint.Value.X, mousePos.X);
                double maxY = Math.Max(_selectionStartPoint.Value.Y, mousePos.Y);

                Canvas.SetLeft(selectionBox, minX);
                Canvas.SetTop(selectionBox, minY);
                selectionBox.Width = maxX - minX;
                selectionBox.Height = maxY - minY;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void grdOrigImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (_selectionStartPoint == null)
                {
                    return;
                }

                // Clear selection
                grdOrigImage.ReleaseMouseCapture();
                selectionBox.Visibility = Visibility.Collapsed;

                Point start = _selectionStartPoint.Value;
                _selectionStartPoint = null;

                Convolution2D image = GetOriginalImageGrays();
                if (image == null)
                {
                    return;
                }

                // Screen coords
                Point mousePos = e.GetPosition(grdOrigImage);

                double minX = Math.Min(start.X, mousePos.X);
                double minY = Math.Min(start.Y, mousePos.Y);
                double maxX = Math.Max(start.X, mousePos.X);
                double maxY = Math.Max(start.Y, mousePos.Y);

                // Convert to convolution coords
                double scaleX = image.Width / grdOrigImage.ActualWidth;
                double scaleY = image.Height / grdOrigImage.ActualHeight;

                int extractX1 = (minX * scaleX).ToInt_Round();
                int extractY1 = (minY * scaleY).ToInt_Round();
                int extractX2 = (maxX * scaleX).ToInt_Round();
                int extractY2 = (maxY * scaleY).ToInt_Round();

                if (extractX1 < 0) extractX1 = 0;
                if (extractY1 < 0) extractY1 = 0;
                if (extractX2 < 0) extractX2 = 0;
                if (extractY2 < 0) extractY2 = 0;

                if (extractX1 >= image.Width) extractX1 = image.Width - 1;
                if (extractY1 >= image.Height) extractY1 = image.Height - 1;
                if (extractX2 >= image.Width) extractX2 = image.Width - 1;
                if (extractY2 >= image.Height) extractY2 = image.Height - 1;

                int extractWidth = extractX2 - extractX1;
                int extractHeight = extractY2 - extractY1;

                if (extractWidth < 1 || extractHeight < 1)
                {
                    return;
                }

                // Extract
                AddKernel(image.Extract(new RectInt(extractX1, extractY1, extractWidth, extractHeight), ConvolutionExtractType.Edge));
                AddKernel(image.Extract(new RectInt(extractX1, extractY1, extractWidth, extractHeight), ConvolutionExtractType.EdgeSoftBorder));
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

                if (e.ChangedButton != MouseButton.Left)
                {
                    return;
                }

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

        private void AddDefaultKernels_Gaussian()
        {
            int[] sizes = new[] { 3, 4, 5, 6, 7, 9, 15, 20, 30 };

            foreach (int size in sizes)
            {
                AddKernel(Convolutions.GetGaussian(size, 1), "gaussian");
            }

            foreach (int size in sizes)
            {
                AddKernel(Convolutions.GetGaussian(size, 2), "gaussian x2");
            }
        }
        private void AddDefaultKernels_Edges_Small()
        {
            AddKernel(Convolutions.GetEdge_Sobel(true), "sobel");
            AddKernel(Convolutions.GetEdge_Sobel(false), "sobel");
            AddKernel(Convolutions.Rotate_45(Convolutions.GetEdge_Sobel(true), true), "sobel");
            AddKernel(Convolutions.Rotate_45(Convolutions.GetEdge_Sobel(false), true), "sobel");

            AddKernel(Convolutions.GetEdge_Prewitt(true), "prewitt");
            AddKernel(Convolutions.GetEdge_Prewitt(false), "prewitt");
            AddKernel(Convolutions.Rotate_45(Convolutions.GetEdge_Prewitt(true), true), "prewitt");
            AddKernel(Convolutions.Rotate_45(Convolutions.GetEdge_Prewitt(false), true), "prewitt");

            AddKernel(Convolutions.GetEdge_Compass(true), "compass");
            AddKernel(Convolutions.GetEdge_Compass(false), "compass");
            AddKernel(Convolutions.Rotate_45(Convolutions.GetEdge_Compass(true), true), "compass");
            AddKernel(Convolutions.Rotate_45(Convolutions.GetEdge_Compass(false), true), "compass");

            AddKernel(Convolutions.GetEdge_Kirsch(true), "kirsch");
            AddKernel(Convolutions.GetEdge_Kirsch(false), "kirsch");
            AddKernel(Convolutions.Rotate_45(Convolutions.GetEdge_Kirsch(true), true), "kirsch");
            AddKernel(Convolutions.Rotate_45(Convolutions.GetEdge_Kirsch(false), true), "kirsch");

            AddKernel(Convolutions.GetEdge_Laplacian(true), "laplacian");
            AddKernel(Convolutions.GetEdge_Laplacian(false), "laplacian");
        }
        private void AddDefaultKernels_Edges_Large()
        {
            // these should be larger.  The simple edge is good at 3x3, but these mini diagrams should be larger


            //  - | / \ + X
            //  L ^
            //  O (      also quarter circle

            //maybe:
            // T
            //  filled circle
            // diamond
            // =



            // Let them specify line width?
        }
        private void AddDefaultKernels_Composite()
        {
            // Gaussian Subtract
            AddKernel(new ConvolutionSet2D(new[] { Convolutions.GetGaussian(3, 1) }, SetOperationType.Subtract), "gaussian subtract");

            // MaxAbs Sobel
            Convolution2D vert = Convolutions.GetEdge_Sobel(true);
            Convolution2D horz = Convolutions.GetEdge_Sobel(false);
            Convolution2D vert45 = Convolutions.Rotate_45(vert, true);
            Convolution2D horz45 = Convolutions.Rotate_45(horz, true);
            ConvolutionSet2D first = null;

            foreach (int gain in new[] { 1, 2, 4 })
            {
                var singles = new[]
                {
                    new Convolution2D(vert.Values, vert.Width, vert.Height, vert.IsNegPos, gain),
                    new Convolution2D(horz.Values, horz.Width, horz.Height, horz.IsNegPos, gain),
                    new Convolution2D(vert45.Values, vert45.Width, vert45.Height, vert45.IsNegPos, gain),
                    new Convolution2D(horz45.Values, horz45.Width, horz45.Height, horz45.IsNegPos, gain),
                };

                ConvolutionSet2D set = new ConvolutionSet2D(singles, SetOperationType.MaxOf);
                AddKernel(set, string.Format("max of sobel{0}", gain == 1 ? "" : string.Format("\r\ngain={0}", gain)));

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

                AddKernel(new ConvolutionSet2D(convs, SetOperationType.Standard), string.Format("guass then edge\r\n{0}x{0}", size));
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

        private void AddKernel(ConvolutionBase2D kernel, string tooltipHeader = "")
        {
            Border border = Convolutions.GetKernelThumbnail(kernel, 40, _kernelContextMenu);

            if (!string.IsNullOrEmpty(tooltipHeader))
            {
                // For simple (not composite) kernels, it's the image that gets the tooltip.  So if this is one of those, add to the tooltip
                if (border.Child is Image)
                {
                    string existingTooltip = ((Image)border.Child).ToolTip as string;

                    if (!string.IsNullOrEmpty(existingTooltip))
                    {
                        ((Image)border.Child).ToolTip = tooltipHeader + "\r\n" + existingTooltip;
                    }
                    else
                    {
                        border.ToolTip = tooltipHeader;
                    }
                }
                else
                {
                    border.ToolTip = tooltipHeader;
                }
            }

            // Store them
            panelKernels.Children.Add(border);
            _kernels.Add(kernel);
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

            // Show Filtered
            modifiedImage.Source = Convolutions.ShowConvolutionResult(filtered, isNegPos, (ConvolutionResultNegPosColoring)cboEdgeColors.SelectedValue);
        }

        /// <summary>
        /// Converts the original image into a grayscale
        /// </summary>
        private Convolution2D GetOriginalImageGrays()
        {
            if (originalImage.Source == null)
            {
                return null;
            }

            int? limit = null;
            int limitInt;
            if (chkLimitImageSize.IsChecked.Value && int.TryParse(txtSizeLimit.Text, out limitInt))
            {
                limit = limitInt;
            }

            bool shouldBuild = false;

            if (_origImageGrays == null)
            {
                shouldBuild = true;
            }
            else if (limit != null && (_origImageGrays.Width > limit.Value || _origImageGrays.Height > limit.Value))
            {
                // The current one is the wrong size
                shouldBuild = true;
            }

            if (shouldBuild)
            {
                BitmapSource bitmap = (BitmapSource)originalImage.Source;

                BitmapCustomCachedBytes colors = null;

                if (limit != null)
                {
                    bitmap = UtilityWPF.ResizeImage(bitmap, limit.Value);       // this will only resize if it's too big
                }

                colors = (BitmapCustomCachedBytes)UtilityWPF.ConvertToColorArray(bitmap, false, Colors.Transparent);

                _origImageGrays = colors.ToConvolution();
            }

            return _origImageGrays;
        }

        #endregion
    }
}
