using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Shapes;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.Testers.Convolution;
using Microsoft.Win32;

namespace Game.Newt.Testers.Encog
{
    //TODO: Serialize session
    //TODO: Context menus
    public partial class FeatureRecognizer2 : Window
    {
        #region Enum: PrefilterType

        private enum PrefilterType
        {
            Gaussian,
            Gaussian_Subtract,
            Individual_Sobel,

            MaxAbs_Sobel,
            Gaussian_Edge,
        }

        #endregion

        #region Declaration Section

        private const int IMAGESIZE = 300;
        private const int THUMBSIZE_EXTRACT = 40;

        private List<Window> _childWindows = new List<Window>();

        private List<FeatureRecognizer_Image> _images = new List<FeatureRecognizer_Image>();

        /// <summary>
        /// These are optional convolutions that could be applied between the image and extract
        /// </summary>
        private readonly Tuple<PrefilterType, ConvolutionBase2D[]>[] _preFilters;

        private List<FeatureRecognizer2_FeatureConv> _featureConvs = new List<FeatureRecognizer2_FeatureConv>();
        private int _selectedFeatureConvIndex = -1;

        private List<FeatureRecognizer2_FeatureAnalyzer> _analyzers = new List<FeatureRecognizer2_FeatureAnalyzer>();
        private int _selectedAnalyzerIndex = -1;

        private List<FeatureRecognizer2_FeatureRecognizer> _recognizers = new List<FeatureRecognizer2_FeatureRecognizer>();
        private int _selectedRecognizerIndex = -1;

        private readonly DropShadowEffect _selectEffect;

        private readonly ContextMenu _featureExtractContextMenu;

        #endregion

        #region Constructor

        public FeatureRecognizer2()
        {
            InitializeComponent();

            this.Background = SystemColors.ControlBrush;

            // Pre filters
            _preFilters = GetPreFilters();

            // Selected effect
            _selectEffect = new DropShadowEffect()
            {
                Direction = 0,
                ShadowDepth = 0,
                BlurRadius = 13,
                Color = UtilityWPF.ColorFromHex("6DF727"),
                Opacity = .9,
            };

            // Context Menus
            _featureExtractContextMenu = (ContextMenu)this.Resources["featureExtractContextMenu"];

            // NegPos coloring
            foreach (ConvolutionResultNegPosColoring coloring in Enum.GetValues(typeof(ConvolutionResultNegPosColoring)))
            {
                cboEdgeColors.Items.Add(coloring);
            }
            cboEdgeColors.SelectedIndex = 0;

            // FeatureExtract Combobox
            cboExtractType.Items.Add(ConvolutionExtractType.EdgeSoftBorder);
            cboExtractType.Items.Add(ConvolutionExtractType.Edge);
            cboExtractType.SelectedIndex = 0;
        }

        #endregion

        #region Event Listeners

        private void Window_Closed(object sender, EventArgs e)
        {
            try
            {
                #region Child windows

                foreach (Window child in _childWindows.ToArray())        // taking the array so that the list can be removed from while in the for loop (it shouldn't happen, but just in case)
                {
                    child.Closed -= Child_Closed;
                    child.Close();
                }

                _childWindows.Clear();

                #endregion
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

        #endregion
        #region Event Listeners - Images

        private void btnAddImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string tag = cboImageLabel.Text;
                if (string.IsNullOrWhiteSpace(tag))
                {
                    MessageBox.Show("Please give this image a name", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                OpenFileDialog dialog = new OpenFileDialog();
                dialog.Multiselect = true;
                dialog.Title = "Please select an image";
                bool? result = dialog.ShowDialog();
                if (result == null || !result.Value)
                {
                    return;
                }

                foreach (string dialogFilename in dialog.FileNames)
                {
                    // Make sure that it's a valid file
                    BitmapSource bitmap = new BitmapImage(new Uri(dialogFilename));

                    // Resize (for this tester, just force it to be square)
                    bitmap = UtilityWPF.ResizeImage(bitmap, IMAGESIZE, IMAGESIZE);

                    string uniqueID = Guid.NewGuid().ToString();

                    // Copy to the working folder
                    //string filename = tag + " - " + uniqueID + ".png";
                    //string fullFilename = System.IO.Path.Combine(_workingFolder, filename);

                    //UtilityWPF.SaveBitmapPNG(bitmap, fullFilename);

                    // Build entry
                    FeatureRecognizer_Image entry = new FeatureRecognizer_Image()
                    {
                        Tag = tag,
                        UniqueID = uniqueID,
                        //Filename = filename,
                        ImageControl = FeatureRecognizer.GetTreeviewImageCtrl(bitmap),
                        Bitmap = bitmap,
                    };

                    // Store it
                    FeatureRecognizer.AddImage(entry, _images, treeImages, cboImageLabel);
                }

                // Update the session file
                //SaveSession_SessionFile(_workingFolder);
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
                foreach (TreeViewItem node in treeImages.Items)
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
                foreach (TreeViewItem node in treeImages.Items)
                {
                    node.IsExpanded = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
        #region Event Listeners - Feature Convolutions

        //TODO: This method only deals with full size images, full size extract.  Once deciding a rectangle, figure out how much reduction to use
        private void RandomExtract_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_images.Count == 0)
                {
                    MessageBox.Show("Add an image first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Image
                var image = GetSelected_Image();
                if (image == null)
                {
                    image = _images[StaticRandom.Next(_images.Count)];
                }

                // Prefilter (could be null)
                ConvolutionBase2D preFilter = GetRandomPreFilter();

                // Image convolution
                Convolution2D imageConv = UtilityWPF.ConvertToConvolution(image.Bitmap);

                if (preFilter != null)
                {
                    imageConv = Convolutions.Convolute(imageConv, preFilter);
                }

                // Extract
                RectInt extractSize = GetExtractSize(image, preFilter, chkIsExtractSquare.IsChecked.Value, trkExtractSizePercent.Value);

                ConvolutionExtractType extractType = UtilityCore.EnumParse<ConvolutionExtractType>(cboExtractType.Text);
                Convolution2D extractConv = imageConv.Extract(extractSize, extractType);

                // Add it
                AddFeatureConvolution(extractConv, preFilter);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //TODO: Show an image and the convolution with this extract
        private void panelFeatureConvolutions_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.ChangedButton != MouseButton.Left)
                {
                    return;
                }

                Tuple<Border, int> clickedCtrl = GetSelected_FeatureConv(e.OriginalSource, false);
                if (clickedCtrl == null)
                {
                    Select_FeatureConv(-1);
                }
                else
                {
                    Select_FeatureConv(clickedCtrl.Item2);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FeatureConvView_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Tuple<Border, int> selectedCtrl = GetSelected_FeatureConv(_featureExtractContextMenu.PlacementTarget, false);
                if (selectedCtrl == null)
                {
                    MessageBox.Show("Couldn't identify feature convolution", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ViewConvolution(_featureConvs[selectedCtrl.Item2].Convolution);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void FeatureConvEdit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Tuple<Border, int> selectedCtrl = GetSelected_FeatureConv(_featureExtractContextMenu.PlacementTarget, false);
                if (selectedCtrl == null)
                {
                    MessageBox.Show("Couldn't identify feature convolution", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                EditConvolution(_featureConvs[selectedCtrl.Item2].Convolution, _featureConvs[selectedCtrl.Item2], FeatureConvolution_SaveRequested);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void FeatureConvRemoveSection_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Tuple<Border, int> selectedCtrl = GetSelectedExtract(_extractContextMenu.PlacementTarget);
                //if (selectedCtrl == null)
                //{
                //MessageBox.Show("Couldn't identify feature convolution", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                //    return;
                //}

                //FeatureRecognizer_Extract origExtract = _extracts[selectedCtrl.Item2];

                //Convolution2D conv = Convolutions.RemoveSection(origExtract.Extracts[0].Extract);

                //BuildChangedExtract(origExtract, conv);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void FeatureConvDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Tuple<Border, int> selectedCtrl = GetSelected_FeatureConv(_featureExtractContextMenu.PlacementTarget, false);
                if (selectedCtrl == null)
                {
                    MessageBox.Show("Couldn't identify feature convolution", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                panelFeatureConvolutions.Children.Remove(selectedCtrl.Item1);
                _featureConvs.RemoveAt(selectedCtrl.Item2);

                if (_selectedFeatureConvIndex == selectedCtrl.Item2)
                {
                    _selectedFeatureConvIndex = -1;
                }
                else if (_selectedFeatureConvIndex > selectedCtrl.Item2)
                {
                    _selectedFeatureConvIndex--;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FeatureConvolution_SaveRequested(object sender, Convolution2D e)
        {
            try
            {
                ImageFilterPainter senderCast = sender as ImageFilterPainter;
                if (senderCast == null)
                {
                    MessageBox.Show("Expected sender to be the painter window", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                FeatureRecognizer2_FeatureConv origExtract = senderCast.Tag as FeatureRecognizer2_FeatureConv;
                if (origExtract == null)
                {
                    MessageBox.Show("Expected the painter to contain the original feature convolution", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                AddFeatureConvolution(e, origExtract.PreFilter);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
        #region Event Listeners - Analyzers

        private void RandomAnalyzer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_featureConvs.Count == 0)
                {
                    MessageBox.Show("Add a feature convolution first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Feature Convolution
                FeatureRecognizer2_FeatureConv featConv = _selectedFeatureConvIndex < 0 ?
                    _featureConvs[StaticRandom.Next(_featureConvs.Count)] :
                    _featureConvs[_selectedFeatureConvIndex];

                //TODO: Ideal Pattern

                double idealBrightness = StaticRandom.NextDouble(170, 255);

                AddAnalyzer(featConv, null, idealBrightness);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //TODO: Show an image and the convolution and scores
        private void panelFeatureAnalyzers_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.ChangedButton != MouseButton.Left)
                {
                    return;
                }

                Tuple<Border, int> clickedCtrl = GetSelected_Analyzer(e.OriginalSource, false);
                if (clickedCtrl == null)
                {
                    Select_Analyzer(-1);
                }
                else
                {
                    Select_Analyzer(clickedCtrl.Item2);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
        #region Event Listeners - Recognizers

        private void RandomRecognizer_Click(object sender, RoutedEventArgs e)
        {
            const int MAXANALYZERS = 5;

            try
            {
                if (_analyzers.Count == 0)
                {
                    MessageBox.Show("Add a feature analyzer first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int numAnalyzers = StaticRandom.Next(1, Math.Min(_analyzers.Count, MAXANALYZERS));

                FeatureRecognizer2_FeatureAnalyzer[] analyzers = UtilityCore.RandomRange(0, _analyzers.Count, numAnalyzers).
                    Select(o => _analyzers[o]).
                    ToArray();

                //TODO: Use some algorithm to come up with this value......
                int inputSize = StaticRandom.Next(3, 12);

                AddRecognizer(analyzers, inputSize);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void panelFeatureRecognizers_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private void AddFeatureConvolution(Convolution2D conv, ConvolutionBase2D preFilter)
        {
            FeatureRecognizer2_FeatureConv item = new FeatureRecognizer2_FeatureConv()
            {
                UniqueID = Guid.NewGuid().ToString(),

                Convolution = conv,
                ConvolutionDNA = conv.ToDNA(),

                PreFilter = preFilter,

                Control = Convolutions.GetKernelThumbnail(conv, THUMBSIZE_EXTRACT, _featureExtractContextMenu),
            };

            // Prefilter DNA
            if (preFilter != null)
            {
                if (preFilter is Convolution2D)
                {
                    item.PreFilterDNA_Single = ((Convolution2D)preFilter).ToDNA();
                }
                else if (preFilter is ConvolutionSet2D)
                {
                    item.PreFilterDNA_Set = ((ConvolutionSet2D)preFilter).ToDNA();
                }
                else
                {
                    throw new ApplicationException("Unknown type of convolution: " + preFilter.GetType().ToString());
                }
            }

            // Add it
            _featureConvs.Add(item);
            panelFeatureConvolutions.Children.Add(item.Control);
        }
        private void AddAnalyzer(FeatureRecognizer2_FeatureConv featConv, object idealPattern, double idealBrightness)
        {
            FeatureRecognizer2_FeatureAnalyzer analyzer = new FeatureRecognizer2_FeatureAnalyzer()
            {
                UniqueID = Guid.NewGuid().ToString(),

                FeatureConv_UniqueID = featConv.UniqueID,
                FeatureConvolution = featConv,

                IdealBrightness = idealBrightness,

                //IdealPattern_UniqueID = ,
                //IdealPatternConvolution = ,

                Control = Convolutions.GetKernelThumbnail(featConv.Convolution, THUMBSIZE_EXTRACT, null),
            };

            _analyzers.Add(analyzer);
            panelAnalyzers.Children.Add(analyzer.Control);
        }
        private void AddRecognizer(FeatureRecognizer2_FeatureAnalyzer[] analyzers, int inputSize)
        {
            FeatureRecognizer2_FeatureRecognizer recognizer = new FeatureRecognizer2_FeatureRecognizer()
            {
                UniqueID = Guid.NewGuid().ToString(),

                Analyzers = analyzers,
                AnalyzerIDs = analyzers.
                    Select(o => o.UniqueID).
                    ToArray(),

                InputSize = inputSize,

                Control = new Rectangle()
                {
                    Fill = new SolidColorBrush(UtilityWPF.GetRandomColor(0, 255)),
                    Width = THUMBSIZE_EXTRACT,
                    Height = THUMBSIZE_EXTRACT,
                    Margin = new Thickness(6),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    //ContextMenu = contextMenu,
                    //Tag = kernel,
                },
            };

            _recognizers.Add(recognizer);
            panelRecognizers.Children.Add(recognizer.Control);
        }

        private FeatureRecognizer_Image GetSelected_Image()
        {
            Image selected = treeImages.SelectedItem as Image;
            if (selected == null)
            {
                return null;
            }

            return _images.FirstOrDefault(o => o.ImageControl == selected);
        }
        private Tuple<Border, int> GetSelected_FeatureConv(object source, bool showErrorMsg)
        {
            Border clickedCtrl = GetClickedConvolution(source);

            // Get the index
            int index = panelFeatureConvolutions.Children.IndexOf(clickedCtrl);
            if (index < 0)
            {
                if (showErrorMsg)
                {
                    throw new ApplicationException("Couldn't find clicked item");
                }
                else
                {
                    return null;
                }
            }

            if (_featureConvs.Count != panelFeatureConvolutions.Children.Count)
            {
                throw new ApplicationException("_featureConvs and panelFeatureConvolutions are out of sync");
            }

            return Tuple.Create(clickedCtrl, index);
        }
        private Tuple<Border, int> GetSelected_Analyzer(object source, bool showErrorMsg)
        {
            Border clickedCtrl = GetClickedConvolution(source);

            // Get the index
            int index = panelAnalyzers.Children.IndexOf(clickedCtrl);
            if (index < 0)
            {
                if (showErrorMsg)
                {
                    throw new ApplicationException("Couldn't find clicked item");
                }
                else
                {
                    return null;
                }
            }

            if (_analyzers.Count != panelAnalyzers.Children.Count)
            {
                throw new ApplicationException("_featureAnalyzers and panelAnalyzers are out of sync");
            }

            return Tuple.Create(clickedCtrl, index);
        }

        private void Select_FeatureConv(int index)
        {
            // Set the effect
            int childIndex = 0;
            foreach (UIElement child in panelFeatureConvolutions.Children)
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

            // Remember which is selected
            _selectedFeatureConvIndex = index;

            // Choose an image
            var image = GetSelected_Image();
            if (image == null)
            {
                image = _images[StaticRandom.Next(_images.Count)];
            }

            // Apply this kernel to the original image
            ApplyFeatureConv(image, _featureConvs[_selectedFeatureConvIndex]);
        }
        private void Select_Analyzer(int index)
        {
            // Set the effect
            int childIndex = 0;
            foreach (UIElement child in panelAnalyzers.Children)
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

            // Remember which is selected
            _selectedAnalyzerIndex = index;
        }

        private static Border GetClickedConvolution(object source)
        {
            if (source is Border)
            {
                return (Border)source;
            }
            else if (source is Image)
            {
                Image image = (Image)source;
                return (Border)image.Parent;
            }
            else
            {
                return null;
            }
        }

        private void ViewConvolution(Convolution2D convolution)
        {
            ImageFilterPainter viewer = new ImageFilterPainter();

            viewer.Closed += Child_Closed;

            _childWindows.Add(viewer);

            viewer.ViewKernel(convolution);

            viewer.Show();
        }
        private void EditConvolution(Convolution2D convolution, FeatureRecognizer2_FeatureConv source, EventHandler<Convolution2D> saveMethod)
        {
            ImageFilterPainter viewer = new ImageFilterPainter();

            viewer.Closed += Child_Closed;
            viewer.SaveRequested += saveMethod;

            _childWindows.Add(viewer);

            viewer.Tag = source;
            viewer.EditKernel(convolution);

            viewer.Show();
        }


        private void ApplyFeatureConv(FeatureRecognizer_Image image, FeatureRecognizer2_FeatureConv conv)
        {
            ConvolutionResultNegPosColoring edgeColor = (ConvolutionResultNegPosColoring)cboEdgeColors.SelectedValue;

            // Create a panel that will hold the result
            Grid grid = new Grid();

            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Pixel) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

            panelRight.Child = grid;

            // Left
            Convolution2D imageConv = UtilityWPF.ConvertToConvolution(image.Bitmap);
            if (conv.PreFilter != null)
            {
                imageConv = Convolutions.Convolute(imageConv, conv.PreFilter);
            }

            ApplyFeatureConv_LeftImage(grid, imageConv, conv.PreFilter, edgeColor);

            // Right
            //Convolutions.




        }
        private static void ApplyFeatureConv_LeftImage(Grid grid, Convolution2D imageConv, ConvolutionBase2D preFilter, ConvolutionResultNegPosColoring edgeColor)
        {
            bool isSourceNegPos = preFilter == null ? false : preFilter.IsNegPos;

            string tooltip = string.Format("{0}x{1}", imageConv.Width, imageConv.Height);
            if (preFilter != null)
            {
                tooltip = preFilter.Description + "\r\n" + tooltip;
            }

            Image image = new Image()
            {
                Source = Convolutions.ShowConvolutionResult(imageConv, isSourceNegPos, edgeColor),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                ToolTip = tooltip,
            };

            Grid.SetColumn(image, 0);
            grid.Children.Add(image);
        }
        private static void ApplyFeatureConv_RightImage(Grid grid, Convolution2D imageConv, ConvolutionResultNegPosColoring edgeColor)
        {
            Image image = new Image()
            {
                Source = Convolutions.ShowConvolutionResult(imageConv, true, edgeColor),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                ToolTip = string.Format("{0}x{1}", imageConv.Width, imageConv.Height),
            };

            Grid.SetColumn(image, 2);
            grid.Children.Add(image);
        }


        private ConvolutionBase2D GetRandomPreFilter()
        {
            // Figure out which pre filters to choose from
            List<ConvolutionBase2D> prefilterCandidates = new List<ConvolutionBase2D>();

            if (chkPrefilterNone.IsChecked.Value)
            {
                prefilterCandidates.Add(null);
            }

            if (chkPrefilterGaussian.IsChecked.Value)
            {
                prefilterCandidates.AddRange(_preFilters.Where(o => o.Item1 == PrefilterType.Gaussian).SelectMany(o => o.Item2));
            }

            if (chkPrefilterGaussianSubtract.IsChecked.Value)
            {
                prefilterCandidates.AddRange(_preFilters.Where(o => o.Item1 == PrefilterType.Gaussian_Subtract).SelectMany(o => o.Item2));
            }

            if (chkPrefilterMaxAbsSobel.IsChecked.Value)
            {
                prefilterCandidates.AddRange(_preFilters.Where(o => o.Item1 == PrefilterType.MaxAbs_Sobel).SelectMany(o => o.Item2));
            }

            if (chkPrefilterGaussianEdge.IsChecked.Value)
            {
                prefilterCandidates.AddRange(_preFilters.Where(o => o.Item1 == PrefilterType.Gaussian_Edge).SelectMany(o => o.Item2));
            }

            // Random chance of pre filters
            ConvolutionBase2D retVal = null;
            if (prefilterCandidates.Count > 0)
            {
                retVal = prefilterCandidates[StaticRandom.Next(prefilterCandidates.Count)];
            }

            return retVal;
        }
        private static Tuple<PrefilterType, ConvolutionBase2D[]>[] GetPreFilters()
        {
            var retVal = new List<Tuple<PrefilterType, ConvolutionBase2D[]>>();

            List<ConvolutionBase2D> convolutions = new List<ConvolutionBase2D>();

            #region Gaussian

            convolutions.Clear();

            foreach (int size in new[] { 3, 7, 15 })
            {
                convolutions.Add(Convolutions.GetGaussian(size, 1));
                convolutions.Add(Convolutions.GetGaussian(size, 2));
            }

            retVal.Add(Tuple.Create(PrefilterType.Gaussian, convolutions.ToArray()));

            #endregion
            #region Gaussian Subtract

            convolutions.Clear();
            convolutions.Add(new ConvolutionSet2D(new[] { Convolutions.GetGaussian(3, 1) }, SetOperationType.Subtract));

            retVal.Add(Tuple.Create(PrefilterType.Gaussian_Subtract, convolutions.ToArray()));

            #endregion
            #region MaxAbs Sobel

            convolutions.Clear();

            Convolution2D vert = Convolutions.GetEdge_Sobel(true);
            Convolution2D horz = Convolutions.GetEdge_Sobel(false);
            Convolution2D vert45 = Convolutions.Rotate_45(vert, true);
            Convolution2D horz45 = Convolutions.Rotate_45(horz, true);
            ConvolutionSet2D first = null;

            foreach (int gain in new[] { 1, 2 })
            {
                var singles = new[]
                {
                    new Convolution2D(vert.Values, vert.Width, vert.Height, vert.IsNegPos, gain),
                    new Convolution2D(horz.Values, horz.Width, horz.Height, horz.IsNegPos, gain),
                    new Convolution2D(vert45.Values, vert45.Width, vert45.Height, vert45.IsNegPos, gain),
                    new Convolution2D(horz45.Values, horz45.Width, horz45.Height, horz45.IsNegPos, gain),
                };

                ConvolutionSet2D set = new ConvolutionSet2D(singles, SetOperationType.MaxOf);
                convolutions.Add(set);

                first = first ?? set;
            }

            retVal.Add(Tuple.Create(PrefilterType.MaxAbs_Sobel, convolutions.ToArray()));

            #endregion
            #region Gausian then edge

            convolutions.Clear();

            foreach (int size in new[] { 3, 5, 7 })
            {
                ConvolutionBase2D[] convs = new ConvolutionBase2D[]
                {
                    Convolutions.GetGaussian(size, 1),
                    first,
                };

                convolutions.Add(new ConvolutionSet2D(convs, SetOperationType.Standard));
            }

            retVal.Add(Tuple.Create(PrefilterType.Gaussian_Edge, convolutions.ToArray()));

            #endregion

            return retVal.ToArray();
        }

        private static RectInt GetExtractSize(FeatureRecognizer_Image image, ConvolutionBase2D filter, bool isSquare, double sizePercent)
        {
            // Calculate largest image's size
            VectorInt finalSize = new VectorInt()
            {
                X = image.Bitmap.PixelWidth,
                Y = image.Bitmap.PixelHeight,
            };

            if (filter != null)
            {
                finalSize -= filter.GetReduction();
            }

            // Extract rectangle
            return Convolutions.GetExtractRectangle(finalSize, isSquare, sizePercent);
        }

        #endregion
    }

    #region Class: FeatureRecognizer2_FeatureConv

    public class FeatureRecognizer2_FeatureConv
    {
        public Convolution2D_DNA ConvolutionDNA { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Convolution2D Convolution { get; set; }

        // Only one of these two (if any) will be populated
        public Convolution2D_DNA PreFilterDNA_Single { get; set; }
        public ConvolutionSet2D_DNA PreFilterDNA_Set { get; set; }

        /// <remarks>
        /// I'm hesitant to store the prefilter here.  I would rather the FeatureAnalyzer store it.  Ultimately, it's up to the analyzer to use
        /// this prefilter, or use a different one.  The best results should be to use this filter (if this convolution is the result of an extract),
        /// but there could be interesting results in using a different filter (or similar, like the same basic filter, but a stronger or weaker
        /// version)
        /// </remarks>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ConvolutionBase2D PreFilter { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public UIElement Control { get; set; }

        /// <summary>
        /// This is the same as what's in the filename, and can be used to reference this instance
        /// </summary>
        public string UniqueID { get; set; }
    }

    #endregion
    #region Class: FeatureRecognizer2_FeatureAnalyzer

    public class FeatureRecognizer2_FeatureAnalyzer
    {
        public string UniqueID { get; set; }

        //TODO: The referenced convolution is full size.  This class may want to store a smaller version of it, and reduce any image by the same percent
        public string FeatureConv_UniqueID { get; set; }

        public string IdealPattern_UniqueID { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public FeatureRecognizer2_FeatureConv FeatureConvolution { get; set; }

        //TODO: Reference the class that holds this convolution instead
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Convolution2D IdealPatternConvolution { get; set; }

        public double IdealBrightness { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public UIElement Control { get; set; }
    }

    #endregion
    #region Class: FeatureRecognizer2_FeatureRecognizer

    public class FeatureRecognizer2_FeatureRecognizer
    {
        public string UniqueID { get; set; }

        public string[] AnalyzerIDs { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public FeatureRecognizer2_FeatureAnalyzer[] Analyzers { get; set; }

        /// <summary>
        /// This is the width and height of each analyzer's output convolution
        /// </summary>
        public int InputSize { get; set; }


        //TODO: Store the trained NN


        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public UIElement Control { get; set; }
    }

    #endregion
}
