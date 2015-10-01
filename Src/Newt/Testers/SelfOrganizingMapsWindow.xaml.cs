using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using Game.HelperClassesAI;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Controls2D;
using Game.Newt.Testers.Convolution;

namespace Game.Newt.Testers
{
    //TODO: Make a SelfOrganizingMapsWPF helper class that generates views, handles mouse events
    public partial class SelfOrganizingMapsWindow : Window
    {
        #region Enum: SimpleColorScheme

        private enum SimpleColorScheme
        {
            RGB,
            HSV,
        }

        #endregion
        #region Enum: SimpleColorComponent

        private enum SimpleColorComponent
        {
            R,
            G,
            B,
            H,
            S,
            V,
        }

        #endregion
        #region Enum: NodeDisplayLayout

        private enum NodeDisplayLayout
        {
            Disk_All,       // the area of each cell is fixed
            Disk_NonZero,      //
            Blobs,      // the area of the cell is how many inputs are in that cell
            //Grid_UniformSize,
            //Grid_VariableSize,
        }

        #endregion
        #region Enum: ImageFilterType

        private enum ImageFilterType
        {
            None,
            Edge,
            Edge_Horzontal,
            Edge_Vertical,
            Edge_DiagonalUp,
            Edge_DiagonalDown,
        }

        #endregion
        #region Enum: NormalizationType

        private enum NormalizationType
        {
            None,
            ToUnit,
            Normalize,
        }

        #endregion

        #region Class: ImageInput

        private class ImageInput : ISOMInput
        {
            public ImageInput(FeatureRecognizer_Image image, double[] value_orig, double[] value_normalized)
            {
                this.Image = image;
                this.Weights_Orig = value_orig;
                this.Weights = value_normalized;
            }

            public readonly FeatureRecognizer_Image Image;
            /// <summary>
            /// These should be from 0 to 1
            /// </summary>
            public readonly double[] Weights_Orig;
            public double[] Weights { get; private set; }
        }

        #endregion

        #region Class: NodePositions

        private class NodePositions
        {
            public NodePositions(double[][] positions, double mapRadius, double cellSize)
            {
                this.Positions = positions;
                this.MapRadius = mapRadius;
                this.CellSize = cellSize;
            }

            public readonly double[][] Positions;
            public double MapRadius;
            public double CellSize;
        }

        #endregion
        #region Class: OverlayPolygonStats

        private class OverlayPolygonStats
        {
            public OverlayPolygonStats(SOMNode node, ImageInput[] images, Rect canvasAABB, Vector cursorOffset, Canvas overlay)
            {
                this.Node = node;
                this.Images = images;
                this.CanvasAABB = canvasAABB;
                this.CursorOffset = cursorOffset;
                this.Overlay = overlay;
            }

            public readonly SOMNode Node;
            public readonly ImageInput[] Images;

            public readonly Rect CanvasAABB;
            public readonly Vector CursorOffset;

            public readonly Canvas Overlay;
        }

        #endregion

        #region Class: SOMNodeVoronoiSet

        private class SOMNodeVoronoiSet
        {
            public SOMNodeVoronoiSet(SOMNodeVoronoiCell[] cells, Tuple<int, int>[] neighborLinks, int[][] neighborsByNode)
            {
                this.Cells = cells;
                this.NeighborLinks = neighborLinks;
                this.NeighborsByNode = neighborsByNode;
            }

            public readonly SOMNodeVoronoiCell[] Cells;
            public readonly Tuple<int, int>[] NeighborLinks;
            public readonly int[][] NeighborsByNode;
        }

        #endregion
        #region Class: SOMNodeVoronoiCell

        private class SOMNodeVoronoiCell
        {
            public SOMNodeVoronoiCell(int index, SOMNode node, int[] neighbors, double size, double sizePercentOfTotal)
            {
                this.Index = index;
                this.Node = node;
                this.Neighbors = neighbors;
                this.Size = size;
                this.SizePercentOfTotal = sizePercentOfTotal;
            }

            public readonly int Index;
            public readonly SOMNode Node;

            public readonly int[] Neighbors;

            /// <summary>
            /// This is the area/volume of the voronoi cell
            /// </summary>
            public readonly double Size;
            public readonly double SizePercentOfTotal;

            //Not sure if this is useful.  Since a link straddles a cell, it would have to be divided between the cells
            //public readonly double AvgLinkRadius;
        }

        #endregion

        #region Class: VoronoiStats

        private class VoronoiStats
        {
            public SOMNodeVoronoiSet Set { get; set; }
            public VoronoiStats_Node[] NodeStats { get; set; }
            public Tuple<int, int, double>[] CurrentDistances { get; set; }
        }

        #endregion
        #region Class: VoronoiStats_Node

        private class VoronoiStats_Node
        {
            public int Index { get; set; }
            public double Area { get; set; }
            public double Image { get; set; }
            public double Diff { get { return this.Image - this.Area; } }
            public double MinRadius { get; set; }
        }

        #endregion

        #region Declaration Section

        private const string FILE = "SelfOrganizingMaps Options.xml";

        private List<string> _imageFolders = new List<string>();
        private List<FeatureRecognizer_Image> _images = new List<FeatureRecognizer_Image>();

        private OverlayPolygonStats _overlayPolyStats = null;

        private Func<SOMNode, Color> _getNodeColor_Random = new Func<SOMNode, Color>(o => new ColorHSV(StaticRandom.NextDouble(360), StaticRandom.NextDouble(30, 70), StaticRandom.NextDouble(45, 80)).ToRGB());

        // These are helpers for the attempt at adjusting
        private SOMNode[] _nodes = null;
        private ImageInput[][] _imagesByNode = null;
        private bool _wasEllipseTransferred = false;

        #endregion

        #region Constructor

        public SelfOrganizingMapsWindow()
        {
            InitializeComponent();

            // cboSimpleInputColor
            foreach (SimpleColorScheme scheme in Enum.GetValues(typeof(SimpleColorScheme)))
            {
                cboSimpleInputColor.Items.Add(scheme);
                cboSimpleOutputColor.Items.Add(scheme);
            }
            cboSimpleInputColor.SelectedIndex = 0;
            cboSimpleOutputColor.SelectedIndex = 0;

            // cboThreePixelsTop
            foreach (SimpleColorComponent component in Enum.GetValues(typeof(SimpleColorComponent)))
            {
                cboThreePixelsTop.Items.Add(component);
                cboThreePixelsMid.Items.Add(component);
                cboThreePixelsBot.Items.Add(component);
            }
            cboThreePixelsTop.SelectedIndex = 0;
            cboThreePixelsMid.SelectedIndex = 1;
            cboThreePixelsBot.SelectedIndex = 2;

            // cboSimpleNodeLayout
            // skipping Grid_UniformSize.  There's not much point implementing that, it's unnatural to force categories into a square
            cboSimpleNodeLayout.Items.Add(NodeDisplayLayout.Blobs);
            cboSimpleNodeLayout.Items.Add(NodeDisplayLayout.Disk_NonZero);
            cboSimpleNodeLayout.Items.Add(NodeDisplayLayout.Disk_All);
            cboSimpleNodeLayout.SelectedIndex = 0;

            // cboConvImageFilter
            foreach (ImageFilterType filter in Enum.GetValues(typeof(ImageFilterType)))
            {
                cboConvImageFilter.Items.Add(filter);
            }
            cboConvImageFilter.SelectedIndex = Array.IndexOf(UtilityCore.GetEnums<ImageFilterType>(), ImageFilterType.Edge);

            // cboConvNormalization
            foreach (NormalizationType norm in Enum.GetValues(typeof(NormalizationType)))
            {
                cboConvNormalization.Items.Add(norm);
            }
            cboConvNormalization.SelectedIndex = Array.IndexOf(UtilityCore.GetEnums<NormalizationType>(), NormalizationType.Normalize);
        }

        #endregion

        #region Event Listeners

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                SelfOrganizingMapsOptions options = UtilityCore.ReadOptions<SelfOrganizingMapsOptions>(FILE);

                if (options != null && options.ImageFolders != null && options.ImageFolders.Length > 0)
                {
                    foreach (string folder in options.ImageFolders)
                    {
                        if (AddFolder(folder))
                        {
                            _imageFolders.Add(folder);
                        }
                    }
                }
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
                SelfOrganizingMapsOptions options = new SelfOrganizingMapsOptions()
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

        private void Polygon_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                Polygon senderCast = sender as Polygon;
                if (senderCast == null)
                {
                    return;
                }

                var tag = senderCast.Tag as Tuple<SOMNode, ImageInput[]>;
                if (tag == null)
                {
                    return;
                }

                if (_overlayPolyStats == null || _overlayPolyStats.Node.Token != tag.Item1.Token)
                {
                    BuildOverlay2D(tag.Item1, tag.Item2, chkPreviewCount.IsChecked.Value, chkPreviewNodeHash.IsChecked.Value, chkPreviewImageHash.IsChecked.Value, chkPreviewSpread.IsChecked.Value, chkPreviewPerImageSpread.IsChecked.Value);
                }

                Point mousePos = e.GetPosition(panelDisplay);

                double left = mousePos.X + _overlayPolyStats.CanvasAABB.Left + _overlayPolyStats.CursorOffset.X - 1;
                //double top = mousePos.Y + _overlayPolyStats.CanvasAABB.Top + _overlayPolyStats.CursorOffset.Y - 1;
                //double top = mousePos.Y + _overlayPolyStats.CanvasAABB.Top - _overlayPolyStats.CursorOffset.Y - 1;
                double top = mousePos.Y + _overlayPolyStats.CanvasAABB.Top - 1;     // Y is already centered

                Canvas.SetLeft(_overlayPolyStats.Overlay, left);
                Canvas.SetTop(_overlayPolyStats.Overlay, top);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Polygon_MouseLeave(object sender, MouseEventArgs e)
        {
            try
            {
                _overlayPolyStats = null;
                panelOverlay.Children.Clear();
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

        private void SimpleAvgColor_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_images.Count == 0)
                {
                    MessageBox.Show("Please add some images first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int maxImages = trkMaxInputs.Value.ToInt_Round();

                SimpleColorScheme scheme = (SimpleColorScheme)cboSimpleInputColor.SelectedItem;

                var getValueFromImage = new Func<FeatureRecognizer_Image, double[]>(o => GetValuesFromImage_1Pixel(o, scheme));

                ImageInput[] inputs = GetImageInputs(_images, maxImages, NormalizationType.None, getValueFromImage);

                //var test = inputs.Select(o => Tuple.Create(o.Value[0], o.Value[1], o.Value[2])).ToArray();

                DoSimple(inputs);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void SimpleThreePixels_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_images.Count == 0)
                {
                    MessageBox.Show("Please add some images first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int maxImages = trkMaxInputs.Value.ToInt_Round();

                SimpleColorComponent top = (SimpleColorComponent)cboThreePixelsTop.SelectedItem;
                SimpleColorComponent middle = (SimpleColorComponent)cboThreePixelsMid.SelectedItem;
                SimpleColorComponent bottom = (SimpleColorComponent)cboThreePixelsBot.SelectedItem;

                var getValueFromImage = new Func<FeatureRecognizer_Image, double[]>(o => GetValuesFromImage_3Pixel(o, top, middle, bottom));

                ImageInput[] inputs = GetImageInputs(_images, maxImages, NormalizationType.None, getValueFromImage);

                //var test = inputs.Select(o => Tuple.Create(o.Value[0], o.Value[1], o.Value[2])).ToArray();

                DoSimple(inputs);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Convolution_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_images.Count == 0)
                {
                    MessageBox.Show("Please add some images first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int maxImages = trkMaxInputs.Value.ToInt_Round();
                int imageSize = trkConvImageSize.Value.ToInt_Round();
                double scaleValue = trkConvValue.Value;
                double maxSpreadPercent = trkConvMaxSpread.Value / 100d;

                Func<FeatureRecognizer_Image, double[]> getValueFromImage = null;

                var filterType = (ImageFilterType)cboConvImageFilter.SelectedValue;
                switch (filterType)
                {
                    case ImageFilterType.None:
                        getValueFromImage = new Func<FeatureRecognizer_Image, double[]>(o => GetValuesFromImage_ResizeGray(o, imageSize, scaleValue));
                        break;

                    case ImageFilterType.Edge:
                        getValueFromImage = new Func<FeatureRecognizer_Image, double[]>(o => GetValuesFromImage_ConvMaxpool(o, Convolutions.GetEdgeSet_Sobel(), imageSize, scaleValue));
                        break;

                    case ImageFilterType.Edge_Horzontal:
                        getValueFromImage = new Func<FeatureRecognizer_Image, double[]>(o => GetValuesFromImage_ConvMaxpool(o, Convolutions.GetEdge_Sobel(false), imageSize, scaleValue));
                        break;

                    case ImageFilterType.Edge_Vertical:
                        getValueFromImage = new Func<FeatureRecognizer_Image, double[]>(o => GetValuesFromImage_ConvMaxpool(o, Convolutions.GetEdge_Sobel(true), imageSize, scaleValue));
                        break;

                    case ImageFilterType.Edge_DiagonalUp:
                        getValueFromImage = new Func<FeatureRecognizer_Image, double[]>(o => GetValuesFromImage_ConvMaxpool(o, Convolutions.Rotate_45(Convolutions.GetEdge_Sobel(false), false), imageSize, scaleValue));
                        break;

                    case ImageFilterType.Edge_DiagonalDown:
                        getValueFromImage = new Func<FeatureRecognizer_Image, double[]>(o => GetValuesFromImage_ConvMaxpool(o, Convolutions.Rotate_45(Convolutions.GetEdge_Sobel(true), false), imageSize, scaleValue));
                        break;

                    default:
                        throw new ApplicationException("Unknown ImageFilterType: " + filterType.ToString());
                }

                ImageInput[] inputs = GetImageInputs(_images, maxImages, (NormalizationType)cboConvNormalization.SelectedValue, getValueFromImage);

                //var test = inputs.Select(o => Tuple.Create(o.Value[0], o.Value[1], o.Value[2])).ToArray();

                if (chkConvMaxSpreadPercent.IsChecked.Value)
                {
                    DoConvolution2(inputs, maxSpreadPercent, 4);
                }
                else
                {
                    DoConvolution(inputs);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods - simple

        private void DoSimple(ImageInput[] inputs)
        {
            SOMRules rules = GetSOMRules();

            NodeDisplayLayout layout = (NodeDisplayLayout)cboSimpleNodeLayout.SelectedItem;

            bool returnEmptyNodes = layout == NodeDisplayLayout.Disk_All;

            SOMResult result = SelfOrganizingMaps.TrainSOM(inputs, rules, true, returnEmptyNodes);

            SimpleColorScheme scheme = (SimpleColorScheme)cboSimpleOutputColor.SelectedItem;
            var getNodeColor = new Func<SOMNode, Color>(o => GetColor(o.Weights, scheme));

            // Show results
            switch (layout)
            {
                case NodeDisplayLayout.Disk_All:
                    ShowResults_Disk(panelDisplay, result, getNodeColor);
                    break;

                case NodeDisplayLayout.Disk_NonZero:
                    result = SelfOrganizingMaps.ArrangeNodes_LikesAttract(result);
                    ShowResults_Disk(panelDisplay, result, getNodeColor);
                    break;

                case NodeDisplayLayout.Blobs:
                    ShowResults_Blobs(panelDisplay, result, getNodeColor);
                    break;

                //case NodeDisplayLayout.Grid_UniformSize:
                //    throw new ApplicationException("finish this");
                //    //ShowResults_Grid(panelDisplay, nodes, (SimpleColorScheme)cboSimpleOutputColor.SelectedItem, positions.GridCellSize);
                //    break;

                default:
                    throw new ApplicationException("Unknown SimpleNodeLayout: " + layout.ToString());
            }
        }

        private static Color GetColor(double[] values, SimpleColorScheme scheme)
        {
            #region validate
#if DEBUG
            if (values.Length != 3)
            {
                throw new ArgumentException("Expected values to be length 3: " + values.Length.ToString());
            }
#endif
            #endregion

            switch (scheme)
            {
                case SimpleColorScheme.RGB:
                    double r = values[0] * 255d;
                    double g = values[1] * 255d;
                    double b = values[2] * 255d;

                    if (r < 0) r = 0;
                    if (r > 255) r = 255;

                    if (g < 0) g = 0;
                    if (g > 255) g = 255;

                    if (b < 0) b = 0;
                    if (b > 255) b = 255;

                    return Color.FromRgb(Convert.ToByte(r), Convert.ToByte(g), Convert.ToByte(b));

                case SimpleColorScheme.HSV:
                    double h = values[0] * 360d;
                    double s = values[1] * 100d;
                    double v = values[2] * 100d;

                    if (h < 0) h = 0;
                    if (h > 360) h = 360;

                    if (s < 0) s = 0;
                    if (s > 100) s = 100;

                    if (v < 0) v = 0;
                    if (v > 255) v = 255;

                    return new ColorHSV(h, s, v).ToRGB();

                default:
                    throw new ApplicationException("Unknown SimpleColorScheme: " + scheme.ToString());
            }
        }

        private static double[] GetValuesFromImage_1Pixel(FeatureRecognizer_Image image, SimpleColorScheme scheme)
        {
            BitmapSource bitmap = UtilityWPF.ResizeImage(new BitmapImage(new Uri(image.Filename)), 1, 1);

            byte[][] colors = ((BitmapCustomCachedBytes)UtilityWPF.ConvertToColorArray(bitmap, false, Colors.Black))
                .GetColorBytes();

            if (colors.Length != 1)
            {
                throw new ApplicationException("Expected exactly one pixel: " + colors.Length.ToString());
            }

            double[] retVal = new double[3];

            switch (scheme)
            {
                case SimpleColorScheme.RGB:
                    #region RGB

                    retVal[0] = colors[0][1] / 255d;        //colors[0][0] is alpha
                    retVal[1] = colors[0][2] / 255d;
                    retVal[2] = colors[0][3] / 255d;

                    #endregion
                    break;

                case SimpleColorScheme.HSV:
                    #region HSV

                    ColorHSV hsv = UtilityWPF.RGBtoHSV(Color.FromArgb(colors[0][0], colors[0][1], colors[0][2], colors[0][3]));

                    retVal[0] = hsv.H / 360d;
                    retVal[1] = hsv.S / 100d;
                    retVal[2] = hsv.V / 100d;

                    #endregion
                    break;

                default:
                    throw new ApplicationException("Unknown SimpleColorScheme: " + scheme.ToString());
            }

            return retVal;
        }
        private static double[] GetValuesFromImage_3Pixel(FeatureRecognizer_Image image, SimpleColorComponent top, SimpleColorComponent middle, SimpleColorComponent bottom)
        {
            BitmapSource bitmap = UtilityWPF.ResizeImage(new BitmapImage(new Uri(image.Filename)), 1, 3);

            byte[][] colors = ((BitmapCustomCachedBytes)UtilityWPF.ConvertToColorArray(bitmap, false, Colors.Black))
                .GetColorBytes();

            if (colors.Length != 3)
            {
                throw new ApplicationException("Expected exactly three pixels: " + colors.Length.ToString());
            }

            double[] retVal = new double[3];

            SimpleColorComponent[] components = new[] { top, middle, bottom };

            for (int cntr = 0; cntr < 3; cntr++)
            {
                switch (components[cntr])
                {
                    case SimpleColorComponent.R:
                        #region R

                        retVal[cntr] = colors[cntr][1] / 255d;        //colors[cntr][0] is alpha

                        #endregion
                        break;

                    case SimpleColorComponent.G:
                        #region G

                        retVal[cntr] = colors[cntr][2] / 255d;        //colors[cntr][0] is alpha

                        #endregion
                        break;

                    case SimpleColorComponent.B:
                        #region B

                        retVal[cntr] = colors[cntr][3] / 255d;        //colors[cntr][0] is alpha

                        #endregion
                        break;

                    case SimpleColorComponent.H:
                        #region H

                        ColorHSV hsvH = UtilityWPF.RGBtoHSV(Color.FromArgb(colors[cntr][0], colors[cntr][1], colors[cntr][2], colors[cntr][3]));

                        retVal[cntr] = hsvH.H / 360d;

                        #endregion
                        break;

                    case SimpleColorComponent.S:
                        #region S

                        ColorHSV hsvS = UtilityWPF.RGBtoHSV(Color.FromArgb(colors[cntr][0], colors[cntr][1], colors[cntr][2], colors[cntr][3]));

                        retVal[cntr] = hsvS.S / 100d;

                        #endregion
                        break;

                    case SimpleColorComponent.V:
                        #region V

                        ColorHSV hsvV = UtilityWPF.RGBtoHSV(Color.FromArgb(colors[cntr][0], colors[cntr][1], colors[cntr][2], colors[cntr][3]));

                        retVal[cntr] = hsvV.V / 100d;

                        #endregion
                        break;

                    default:
                        throw new ApplicationException("Unknown SimpleColorComponent: " + components[cntr].ToString());
                }
            }

            return retVal;
        }

        #endregion
        #region Private Methods - convolution

        private void DoConvolution(ImageInput[] inputs)
        {
            SOMRules rules = GetSOMRules();

            SOMResult result = SelfOrganizingMaps.TrainSOM(inputs, rules, true, false);

            ShowResults_Blobs(panelDisplay, result, _getNodeColor_Random);
        }
        private void DoConvolution2(ImageInput[] inputs, double maxSpreadPercent, int minNodeItemsForSplit)
        {
            SOMRules rules = GetSOMRules();

            SOMResult result = SelfOrganizingMaps.TrainSOM(inputs, rules, maxSpreadPercent, true, false);

            ShowResults_Blobs(panelDisplay, result, _getNodeColor_Random);
        }

        private static double[] GetValuesFromImage_ResizeGray(FeatureRecognizer_Image image, int size, double scaleValue)
        {
            BitmapSource bitmap = new BitmapImage(new Uri(image.Filename));
            bitmap = UtilityWPF.ResizeImage(bitmap, size, true);

            Convolution2D retVal = UtilityWPF.ConvertToConvolution(bitmap, scaleValue);
            if (retVal.Width != retVal.Height)
            {
                retVal = Convolutions.ExtendBorders(retVal, size, size);        //NOTE: width or height is already the desired size, this will just enlarge the other to make it square
            }

            return retVal.Values;
        }
        private static double[] GetValuesFromImage_ConvMaxpool(FeatureRecognizer_Image image, ConvolutionBase2D kernel, int size, double scaleValue)
        {
            const int INITIALSIZE = 80;

            BitmapSource bitmap = new BitmapImage(new Uri(image.Filename));
            bitmap = UtilityWPF.ResizeImage(bitmap, INITIALSIZE, true);

            Convolution2D retVal = UtilityWPF.ConvertToConvolution(bitmap, scaleValue);
            if (retVal.Width != retVal.Height)
            {
                retVal = Convolutions.ExtendBorders(retVal, INITIALSIZE, INITIALSIZE);        //NOTE: width or height is already the desired size, this will just enlarge the other to make it square
            }

            retVal = Convolutions.Convolute(retVal, kernel);
            retVal = Convolutions.MaxPool(retVal, size, size);
            retVal = Convolutions.Abs(retVal);

            return retVal.Values;
        }

        #endregion
        #region Private Methods - show results

        private void ShowResults_Disk(Border border, SOMResult result, Func<SOMNode, Color> getNodeColor)
        {
            Point[] points = result.Nodes.
                Select(o => new Point(o.Position[0], o.Position[1])).
                ToArray();

            VoronoiResult2D voronoi = Math2D.GetVoronoi(points, true);
            voronoi = Math2D.CapVoronoiCircle(voronoi);

            Color[] colors = result.Nodes.
                Select(o => getNodeColor(o)).
                ToArray();

            ImageInput[][] imagesByNode = UtilityCore.ConvertJaggedArray<ImageInput>(result.InputsByNode);

            Vector size = new Vector(border.ActualWidth - border.Padding.Left - border.Padding.Right, border.ActualHeight - border.Padding.Top - border.Padding.Bottom);
            Canvas canvas = DrawVoronoi(voronoi, colors, result.Nodes, imagesByNode, size.X.ToInt_Floor(), size.Y.ToInt_Floor());

            border.Child = canvas;

            // This is for the manual manipulate buttons
            _nodes = result.Nodes;
            _imagesByNode = imagesByNode;
            _wasEllipseTransferred = false;
        }
        private void ShowResults_Blobs(Border border, SOMResult result, Func<SOMNode, Color> getNodeColor)
        {
            Point[] points = result.Nodes.
                Select(o => new Point(o.Position[0], o.Position[1])).
                ToArray();

            VoronoiResult2D voronoi = Math2D.GetVoronoi(points, true);
            voronoi = Math2D.CapVoronoiCircle(voronoi);

            Color[] colors = result.Nodes.
                Select(o => getNodeColor(o)).
                ToArray();

            ImageInput[][] imagesByNode = UtilityCore.ConvertJaggedArray<ImageInput>(result.InputsByNode);

            Vector size = new Vector(border.ActualWidth - border.Padding.Left - border.Padding.Right, border.ActualHeight - border.Padding.Top - border.Padding.Bottom);
            Canvas canvas = DrawVoronoiBlobs(voronoi, colors, result.Nodes, imagesByNode, size.X.ToInt_Floor(), size.Y.ToInt_Floor());

            border.Child = canvas;

            // This is for the manual manipulate buttons
            _nodes = result.Nodes;
            _imagesByNode = imagesByNode;
            _wasEllipseTransferred = false;
        }

        private Canvas DrawVoronoi(VoronoiResult2D voronoi, Color[] colors, SOMNode[] nodes, ImageInput[][] images, int imageWidth, int imageHeight)
        {
            const double MARGINPERCENT = 1.05;

            #region transform

            var aabb = Math2D.GetAABB(voronoi.EdgePoints);
            aabb = Tuple.Create((aabb.Item1.ToVector() * MARGINPERCENT).ToPoint(), (aabb.Item2.ToVector() * MARGINPERCENT).ToPoint());

            TransformGroup transform = new TransformGroup();
            transform.Children.Add(new TranslateTransform(-aabb.Item1.X, -aabb.Item1.Y));
            transform.Children.Add(new ScaleTransform(imageWidth / (aabb.Item2.X - aabb.Item1.X), imageHeight / (aabb.Item2.Y - aabb.Item1.Y)));

            #endregion

            Canvas retVal = new Canvas();

            for (int cntr = 0; cntr < voronoi.ControlPoints.Length; cntr++)
            {
                #region polygon

                Polygon polygon = new Polygon();

                if (voronoi.EdgesByControlPoint[cntr].Length < 3)
                {
                    throw new ApplicationException("Expected at least three edge points");
                }

                Edge2D[] edges = voronoi.EdgesByControlPoint[cntr].Select(o => voronoi.Edges[o]).ToArray();
                Point[] edgePoints = Edge2D.GetPolygon(edges, 1d);

                edgePoints = edgePoints.
                    Select(o => transform.Transform(o)).
                    ToArray();

                foreach (Point point in edgePoints)
                {
                    polygon.Points.Add(point);
                }

                polygon.Fill = new SolidColorBrush(colors[cntr]);
                polygon.Stroke = new SolidColorBrush(UtilityWPF.OppositeColor(colors[cntr], false));
                polygon.StrokeThickness = 1;

                polygon.Tag = Tuple.Create(nodes[cntr], images[cntr]);

                polygon.MouseMove += Polygon_MouseMove;
                polygon.MouseLeave += Polygon_MouseLeave;

                retVal.Children.Add(polygon);

                #endregion
            }

            return retVal;
        }

        private Canvas DrawVoronoiBlobs(VoronoiResult2D voronoi, Color[] colors, SOMNode[] nodes, ImageInput[][] imagesByNode, int imageWidth, int imageHeight)
        {
            const double MARGINPERCENT = 1.05;

            // Analyze size ratios
            double[] areas = AnalyzeVoronoiCellSizes(voronoi, imagesByNode);

            #region transform

            var aabb = Math2D.GetAABB(voronoi.EdgePoints);
            aabb = Tuple.Create((aabb.Item1.ToVector() * MARGINPERCENT).ToPoint(), (aabb.Item2.ToVector() * MARGINPERCENT).ToPoint());

            TransformGroup transform = new TransformGroup();
            transform.Children.Add(new TranslateTransform(-aabb.Item1.X, -aabb.Item1.Y));
            transform.Children.Add(new ScaleTransform(imageWidth / (aabb.Item2.X - aabb.Item1.X), imageHeight / (aabb.Item2.Y - aabb.Item1.Y)));

            #endregion

            Canvas retVal = new Canvas();

            for (int cntr = 0; cntr < voronoi.ControlPoints.Length; cntr++)
            {
                #region polygon

                Polygon polygon = new Polygon();

                if (voronoi.EdgesByControlPoint[cntr].Length < 3)
                {
                    throw new ApplicationException("Expected at least three edge points");
                }

                Edge2D[] edges = voronoi.EdgesByControlPoint[cntr].Select(o => voronoi.Edges[o]).ToArray();
                Point[] edgePoints = Edge2D.GetPolygon(edges, 1d);

                // Resize to match the desired area
                edgePoints = ResizeConvexPolygon(edgePoints, areas[cntr]);

                // Convert into a smooth blob
                BezierSegmentDef[] bezier = BezierSegmentDef.GetBezierSegments(edgePoints.Select(o => o.ToPoint3D()).ToArray(), .25, true);
                edgePoints = Math3D.GetBezierPath(75, bezier).
                    Select(o => o.ToPoint2D()).
                    ToArray();

                // Transform to canvas coords
                edgePoints = edgePoints.
                    Select(o => transform.Transform(o)).
                    ToArray();

                foreach (Point point in edgePoints)
                {
                    polygon.Points.Add(point);
                }

                polygon.Fill = new SolidColorBrush(colors[cntr]);
                polygon.Stroke = null; // new SolidColorBrush(UtilityWPF.OppositeColor(colors[cntr], false));
                polygon.StrokeThickness = 1;

                polygon.Tag = Tuple.Create(nodes[cntr], imagesByNode[cntr]);

                polygon.MouseMove += Polygon_MouseMove;
                polygon.MouseLeave += Polygon_MouseLeave;

                retVal.Children.Add(polygon);

                #endregion
            }

            return retVal;
        }

        private static double[] AnalyzeVoronoiCellSizes(VoronoiResult2D voronoi, ImageInput[][] imagesByNode)
        {
            // Calculate area, density of each node
            var sizes = Enumerable.Range(0, voronoi.ControlPoints.Length).
                Select(o =>
                {
                    double area = Math2D.GetAreaPolygon(voronoi.GetPolygon(o, 1));        // there are no rays
                    double imageCount = imagesByNode[o].Length.ToDouble();

                    return new
                    {
                        ImagesCount = imageCount,
                        Area = area,
                        Density = imageCount / area,
                    };
                }).
                ToArray();

            // Don't let any node have an area smaller than this
            double minArea = sizes.Min(o => o.Area) * .25;

            // Find the node with the largest density.  This is the density to use when drawing all cells
            var largestDensity = sizes.
                OrderByDescending(o => o.Density).
                First();

            return sizes.Select(o =>
            {
                // Figure out how much area it would take using the highest density
                double area = o.ImagesCount / largestDensity.Density;
                if (area < minArea)
                {
                    area = minArea;
                }

                return area;
            }).
            ToArray();
        }

        private static Point[] ResizeConvexPolygon(Point[] polygon, double newArea)
        {
            Point center = Math2D.GetCenter(polygon);

            // Create a delagate that returns the area of the polygon based on the percent size
            Func<double, double> getOutput = new Func<double, double>(o =>
            {
                Point[] polyPoints = GetPolygon(center, polygon, o);
                return Math2D.GetAreaPolygon(polyPoints);
            });

            // Find a percent that returns the desired area
            double percent = Math1D.GetInputForDesiredOutput_PosInput_PosCorrelation(newArea, newArea * .01, getOutput);

            // Return the sized polygon
            return GetPolygon(center, polygon, percent);
        }

        private static Point[] GetPolygon(Point center, Point[] polygon, double percent)
        {
            return polygon.
                Select(o =>
                {
                    Vector displace = o - center;
                    displace *= percent;
                    return center + displace;
                }).
                ToArray();
        }

        #endregion
        #region Private Methods - tooltip

        private void BuildOverlay2D(SOMNode node, ImageInput[] images, bool showCount, bool showNodeHash, bool showImageHash, bool showSpread, bool showPerImageDistance)
        {
            const int IMAGESIZE = 80;
            const int NODEHASHSIZE = 100;

            const double SMALLFONT1 = 17;
            const double LARGEFONT1 = 21;
            const double SMALLFONT2 = 15;
            const double LARGEFONT2 = 18;
            const double SMALLFONT3 = 12;
            const double LARGEFONT3 = 14;

            const double SMALLLINE1 = .8;
            const double LARGELINE1 = 1;
            const double SMALLLINE2 = .5;
            const double LARGELINE2 = .85;
            const double SMALLLINE3 = .3;
            const double LARGELINE3 = .7;

            Canvas canvas = new Canvas();
            List<Rect> rectangles = new List<Rect>();

            #region cursor rectangle

            var cursorRect = GetMouseCursorRect(0);
            rectangles.Add(cursorRect.Item1);

            // This is just for debugging
            //Rectangle cursorVisual = new Rectangle()
            //{
            //    Width = cursorRect.Item1.Width,
            //    Height = cursorRect.Item1.Height,
            //    Fill = new SolidColorBrush(UtilityWPF.GetRandomColor(64, 192, 255)),
            //};

            //Canvas.SetLeft(cursorVisual, cursorRect.Item1.Left);
            //Canvas.SetTop(cursorVisual, cursorRect.Item1.Top);

            //canvas.Children.Add(cursorVisual);

            #endregion

            #region count text

            if (showCount)
            {
                StackPanel textPanel = new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                };

                // "images "
                OutlinedTextBlock text = GetOutlineText("images ", SMALLFONT1, SMALLLINE1);
                text.Margin = new Thickness(0, 0, 4, 0);
                textPanel.Children.Add(text);

                // count
                text = GetOutlineText(images.Length.ToString("N0"), LARGEFONT1, LARGELINE1);
                textPanel.Children.Add(text);

                // Place on canvas
                textPanel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));       // aparently, the infinity is important to get an accurate desired size
                Size textSize = textPanel.DesiredSize;

                Rect textRect = GetFreeSpot(textSize, new Point(0, 0), new Vector(0, 1), rectangles);
                rectangles.Add(textRect);

                Canvas.SetLeft(textPanel, textRect.Left);
                Canvas.SetTop(textPanel, textRect.Top);

                canvas.Children.Add(textPanel);
            }

            #endregion
            #region spread

            var nodeImages = images.Select(o => o.Weights);
            var allImages = _imagesByNode.SelectMany(o => o).Select(o => o.Weights);

            double nodeSpread = images.Length == 0 ? 0d : SelfOrganizingMaps.GetTotalSpread(nodeImages);
            double totalSpread = SelfOrganizingMaps.GetTotalSpread(allImages);

            if (showSpread)
            {
                double nodeStandDev = MathND.GetStandardDeviation(nodeImages);
                double totalStandDev = MathND.GetStandardDeviation(allImages);

                double percentSpread = nodeSpread / totalSpread;
                double pecentStandDev = nodeStandDev / totalSpread;

                Grid spreadPanel = new Grid()
                {
                    Margin = new Thickness(2),
                };
                spreadPanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
                spreadPanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(4) });
                spreadPanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });

                spreadPanel.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                spreadPanel.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(2) });
                spreadPanel.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                spreadPanel.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(2) });
                spreadPanel.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                spreadPanel.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(2) });
                spreadPanel.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                spreadPanel.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(2) });
                spreadPanel.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                spreadPanel.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(2) });
                spreadPanel.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });

                AddTextRow(spreadPanel, 0, "node stand dev", (pecentStandDev * 100).ToStringSignificantDigits(2) + "%", SMALLFONT2, LARGEFONT2, SMALLLINE2, LARGELINE2, true);
                AddTextRow(spreadPanel, 2, "node spread", (percentSpread * 100).ToStringSignificantDigits(2) + "%", SMALLFONT2, LARGEFONT2, SMALLLINE2, LARGELINE2, false);

                AddTextRow(spreadPanel, 4, "node stand dev", nodeStandDev.ToStringSignificantDigits(2), SMALLFONT3, LARGEFONT3, SMALLLINE3, LARGELINE3, true);
                AddTextRow(spreadPanel, 6, "node spread", nodeSpread.ToStringSignificantDigits(2), SMALLFONT3, LARGEFONT3, SMALLLINE3, LARGELINE3, false);

                AddTextRow(spreadPanel, 8, "total stand dev", totalStandDev.ToStringSignificantDigits(2), SMALLFONT3, LARGEFONT3, SMALLLINE3, LARGELINE3, true);
                AddTextRow(spreadPanel, 10, "total spread", totalSpread.ToStringSignificantDigits(2), SMALLFONT3, LARGEFONT3, SMALLLINE3, LARGELINE3, false);

                // Place on canvas
                spreadPanel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));       // aparently, the infinity is important to get an accurate desired size
                Size spreadSize = spreadPanel.DesiredSize;

                Rect spreadRect = GetFreeSpot(spreadSize, new Point(0, 0), new Vector(0, 1), rectangles);
                rectangles.Add(spreadRect);

                Canvas.SetLeft(spreadPanel, spreadRect.Left);
                Canvas.SetTop(spreadPanel, spreadRect.Top);

                canvas.Children.Add(spreadPanel);
            }

            #endregion

            double[] nodeCenter = images.Length == 0 ? node.Weights : MathND.GetCenter(nodeImages);

            #region node hash

            if (showNodeHash)
            {
                ImageInput nodeImage = new ImageInput(null, node.Weights, node.Weights);

                double nodeDist = MathND.GetDistance(nodeImage.Weights, nodeCenter);
                double nodeDistPercent = nodeSpread.IsNearZero() ? 1d : (nodeDist / nodeSpread);     // if zero or one node, then spread will be zero

                Tuple<UIElement, VectorInt> nodeCtrl = GetPreviewImage(nodeImage, true, NODEHASHSIZE, showPerImageDistance, nodeDistPercent);

                // Place on canvas
                Rect nodeRect = GetFreeSpot(new Size(nodeCtrl.Item2.X, nodeCtrl.Item2.Y), new Point(0, 0), new Vector(0, -1), rectangles);
                rectangles.Add(nodeRect);

                Canvas.SetLeft(nodeCtrl.Item1, nodeRect.Left);
                Canvas.SetTop(nodeCtrl.Item1, nodeRect.Top);

                canvas.Children.Add(nodeCtrl.Item1);
            }

            #endregion

            #region images

            foreach (ImageInput image in images)
            {
                double imageDistPercent;
                if (images.Length == 1)
                {
                    imageDistPercent = 1;
                }
                else
                {
                    imageDistPercent = MathND.GetDistance(image.Weights, nodeCenter) / nodeSpread;
                }

                // Create the image (and any other graphics for that image)
                Tuple<UIElement, VectorInt> imageCtrl = GetPreviewImage(image, showImageHash, IMAGESIZE, showPerImageDistance, imageDistPercent);

                // Find a spot for it
                var imageRect = Enumerable.Range(0, 10).
                    Select(o =>
                    {
                        Vector direction = Math3D.GetRandomVector_Circular_Shell(1).ToVector2D();

                        Rect imageRect2 = GetFreeSpot(new Size(imageCtrl.Item2.X, imageCtrl.Item2.Y), new Point(0, 0), direction, rectangles);

                        return new { Rect = imageRect2, Distance = new Vector(imageRect2.CenterX(), imageRect2.CenterY()).LengthSquared };
                    }).
                    OrderBy(o => o.Distance).
                    First().
                    Rect;

                Canvas.SetLeft(imageCtrl.Item1, imageRect.Left);
                Canvas.SetTop(imageCtrl.Item1, imageRect.Top);

                // Add it
                rectangles.Add(imageRect);
                canvas.Children.Add(imageCtrl.Item1);
            }

            #endregion

            Rect canvasAABB = Math2D.GetAABB(rectangles);

            //NOTE: All the items are placed around zero zero, but that may not be half width and height (items may not be centered)
            canvas.RenderTransform = new TranslateTransform(-canvasAABB.Left, -canvasAABB.Top);

            panelOverlay.Children.Clear();
            panelOverlay.Children.Add(canvas);

            _overlayPolyStats = new OverlayPolygonStats(node, images, canvasAABB, cursorRect.Item2, canvas);
        }

        private static void AddTextRow(Grid grid, int row, string leftText, string rightText, double fontSizeLeft, double fontSizeRight, double strokeThicknessLeft, double strokeThicknessRight, bool invert)
        {
            OutlinedTextBlock text = GetOutlineText(leftText, fontSizeLeft, strokeThicknessLeft, invert);
            text.HorizontalAlignment = HorizontalAlignment.Right;
            Grid.SetColumn(text, 0);
            Grid.SetRow(text, row);
            grid.Children.Add(text);

            text = GetOutlineText(rightText, fontSizeRight, strokeThicknessRight, invert);
            text.HorizontalAlignment = HorizontalAlignment.Left;
            Grid.SetColumn(text, 2);
            Grid.SetRow(text, row);
            grid.Children.Add(text);
        }

        /// <summary>
        /// This creates an image, and any other descriptions
        /// </summary>
        private static Tuple<UIElement, VectorInt> GetPreviewImage(ImageInput image, bool showHash, int imageSize, bool showPerImageDistance, double imageDistPercent)
        {
            #region image

            BitmapSource bitmap;
            if (showHash)
            {
                int width, height;

                double widthHeight = Math.Sqrt(image.Weights_Orig.Length);
                if (widthHeight.ToInt_Floor() == widthHeight.ToInt_Ceiling())
                {
                    width = height = widthHeight.ToInt_Floor();
                }
                else
                {
                    width = image.Weights_Orig.Length;
                    height = 1;
                }

                double? maxValue = image.Weights_Orig.Max() > 1d ? (double?)null : 1d;

                Convolution2D conv = new Convolution2D(image.Weights_Orig, width, height, false);
                bitmap = Convolutions.GetBitmap_Aliased(conv, absMaxValue: maxValue, negPosColoring: ConvolutionResultNegPosColoring.BlackWhite, forcePos_WhiteBlack: false);
            }
            else
            {
                bitmap = UtilityWPF.GetBitmap(image.Image.Filename);
            }

            bitmap = UtilityWPF.ResizeImage(bitmap, imageSize, true);

            Image imageCtrl = new Image()
            {
                Source = bitmap,
            };

            #endregion

            if (!showPerImageDistance)
            {
                // Nothing else needed, just return the image
                return new Tuple<UIElement, VectorInt>(imageCtrl, new VectorInt(bitmap.PixelWidth, bitmap.PixelHeight));
            }

            StackPanel retVal = new StackPanel();

            retVal.Children.Add(imageCtrl);
            retVal.Children.Add(GetPercentVisual(bitmap.PixelWidth, 10, imageDistPercent));

            return new Tuple<UIElement, VectorInt>(retVal, new VectorInt(bitmap.PixelWidth, bitmap.PixelHeight + 10));
        }

        private static Tuple<Rect, Vector> GetMouseCursorRect(double marginPercent = .1)
        {
            // Get system size
            double width = SystemParameters.CursorWidth;
            double height = SystemParameters.CursorHeight;

            // Offset (so the center will be center of the cursor, and not topleft)
            //NOTE: Doing this before margin is applied
            Vector offset = new Vector(width / 2d, height / 2d);

            // Apply margin
            width *= 1 + marginPercent;
            height *= 1 + marginPercent;

            // Build return
            double halfWidth = width / 2d;
            double halfHeight = height / 2d;
            Rect rect = new Rect(-halfWidth, -halfHeight, width, height);

            return Tuple.Create(rect, offset);
        }

        /// <summary>
        /// This looks long direction until a spot large enough to hold size is available
        /// </summary>
        /// <param name="size">The size of the rectangle to return</param>
        /// <param name="start">The point that the rectangle would like to be centered over</param>
        /// <param name="direction">
        /// The direction to slide the rectangle until an empty space is found
        /// NOTE: Set dir.Y to be negative to match screen coords
        /// </param>
        private static Rect GetFreeSpot(Size size, Point desiredPos, Vector direction, List<Rect> existing)
        {
            double halfWidth = size.Width / 2d;
            double halfHeight = size.Height / 2d;

            if (existing.Count == 0)
            {
                // There is nothing blocking this, center the rectangle over the position
                return new Rect(desiredPos.X - halfWidth, desiredPos.Y - halfHeight, size.Width, size.Height);
            }

            // Direction unit
            Vector dirUnit = direction.ToUnit();
            if (Math2D.IsInvalid(dirUnit))
            {
                dirUnit = Math3D.GetRandomVector_Circular_Shell(1).ToVector2D();
            }

            // Calculate step distance (5% of the average of all the sizes)
            double stepDist = UtilityCore.Iterate<Size>(size, existing.Select(o => o.Size)).
                SelectMany(o => new[] { o.Width, o.Height }).
                Average();

            stepDist *= .05;

            // Keep walking along direction until the rectangle doesn't intersect any existing rectangles
            Point point = new Point();
            Rect rect = new Rect();

            for (int cntr = 0; cntr < 5000; cntr++)
            {
                point = desiredPos + (dirUnit * (stepDist * cntr));
                rect = new Rect(point.X - halfWidth, point.Y - halfHeight, size.Width, size.Height);

                if (!existing.Any(o => o.IntersectsWith(rect)))
                {
                    break;
                }
            }

            return rect;
        }

        private static OutlinedTextBlock GetOutlineText(string text, double fontSize, double strokeThickness, bool invert = false)
        {
            return new OutlinedTextBlock()
            {
                Text = text,
                FontSize = fontSize,
                FontWeight = FontWeight.FromOpenTypeWeight(900),
                StrokeThickness = strokeThickness,
                Fill = invert ? Brushes.Black : Brushes.White,
                Stroke = invert ? Brushes.White : Brushes.Black,
                VerticalAlignment = VerticalAlignment.Center,
            };
        }

        private static UIElement GetPercentVisual(double width, double height, double percent)
        {
            return new v2.GameItems.Controls.ProgressBarGame()
            {
                Width = width,
                Height = height,
                Minimum = 0,
                Maximum = 1,
                Value = percent,
                //RightLabelText = percent.ToStringSignificantDigits(2) + "%",
                //RightLabelVisibility = Visibility.Visible,
                //Foreground = new SolidColorBrush(UtilityWPF.ColorFromHex("32331D")),
                ProgressBackColor = UtilityWPF.ColorFromHex("40411E"),
                ProgressColor = UtilityWPF.ColorFromHex("B4AF91"),
            };

            #region MANUAL

            //Brush percentStroke = new SolidColorBrush(UtilityWPF.ColorFromHex("60000000"));
            //Brush percentFill = new SolidColorBrush(UtilityWPF.ColorFromHex("30000000"));

            //Canvas retVal = new Canvas();

            //// border
            //Border border = new Border()
            //{
            //    Height = 20,
            //    Width = width,
            //    CornerRadius = new CornerRadius(1),
            //    BorderThickness = new Thickness(1),
            //    BorderBrush = Brushes.DimGray,
            //    Background = Brushes.Black,
            //};

            //Canvas.SetLeft(border, 0);
            //Canvas.SetTop(border, 0);

            //retVal.Children.Add(border);

            //// % background
            //Rectangle rectPerc = new Rectangle()
            //{
            //    Height = height,
            //    Width = resultEntry.Value * 100,
            //    HorizontalAlignment = HorizontalAlignment.Left,
            //    Fill = percentFill,
            //};

            //Grid.SetColumn(rectPerc, 2);
            //Grid.SetRow(rectPerc, retVal.RowDefinitions.Count - 1);

            //retVal.Children.Add(rectPerc);

            //// % text
            //TextBlock outputPerc = new TextBlock()
            //{
            //    Text = Math.Round(resultEntry.Value * 100).ToString(),
            //    HorizontalAlignment = HorizontalAlignment.Center,
            //    VerticalAlignment = VerticalAlignment.Center,
            //};

            //Grid.SetColumn(outputPerc, 2);
            //Grid.SetRow(outputPerc, retVal.RowDefinitions.Count - 1);

            //retVal.Children.Add(outputPerc);



            //return retVal;

            #endregion
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

        private SOMRules GetSOMRules()
        {
            return new SOMRules(
                trkNumNodes.Value.ToInt_Round(),
                trkNumIterations.Value.ToInt_Round(),
                trkInitialRadiusPercent.Value / 100d,
                trkLearningRate.Value);
        }

        private static ImageInput[] GetImageInputs(IList<FeatureRecognizer_Image> images, int maxInputs, NormalizationType normalizationType, Func<FeatureRecognizer_Image, double[]> getValueFromImage)
        {
            return UtilityCore.RandomOrder(images, maxInputs).
                AsParallel().
                Select(o =>
                    {
                        double[] values = getValueFromImage(o);
                        double[] normalized = GetNormalizedVector(values, normalizationType);
                        return new ImageInput(o, values, normalized);
                    }).
                ToArray();
        }

        private static double[] GetNormalizedVector(double[] vector, NormalizationType normalizationType)
        {
            switch (normalizationType)
            {
                case NormalizationType.None:
                    return vector;

                case NormalizationType.Normalize:
                    return MathND.Normalize(vector);

                case NormalizationType.ToUnit:
                    return MathND.ToUnit(vector, false);

                default:
                    throw new ApplicationException("Unknown NormalizationType: " + normalizationType.ToString());
            }
        }

        /// <summary>
        /// This overload lays them out in 2D (easy to display results)
        /// </summary>
        private static NodePositions GetNodePositions2D(int numNodes, NodeDisplayLayout layout)
        {
            Vector[] positions = null;
            double mapRadius = -1;
            double cellSize = -1d;      // only used when grid

            switch (layout)
            {
                //case NodeDisplayLayout.Grid_UniformSize:
                //    #region Grid

                //    // Make sure the count is a square
                //    int widthHeight = Math.Sqrt(numNodes).ToInt_Ceiling();
                //    numNodes = widthHeight * widthHeight;

                //    positions = new Vector[numNodes];

                //    // Just use a cell size of 1.  The choice is arbitrary, since the algorithm will be based off of map size, and the render will scale to fit
                //    cellSize = 1d;
                //    double halfCellSize = cellSize / 2d;

                //    mapRadius = ((widthHeight + 1) * cellSize) / 2d;

                //    for (int y = 0; y < widthHeight; y++)
                //    {
                //        int yOffset = y * widthHeight;

                //        for (int x = 0; x < widthHeight; x++)
                //        {
                //            positions[yOffset + x] = new Vector((x * cellSize) + halfCellSize, (y * cellSize) + halfCellSize);
                //        }
                //    }

                //    #endregion
                //    break;

                case NodeDisplayLayout.Disk_All:
                case NodeDisplayLayout.Disk_NonZero:
                case NodeDisplayLayout.Blobs:
                    #region Disk

                    positions = Math3D.GetRandomVectors_Circular_EvenDist(numNodes, 1);

                    var aabb = Math2D.GetAABB(positions);
                    mapRadius = Math1D.Avg(aabb.Item2.X - aabb.Item1.X, aabb.Item2.Y - aabb.Item1.Y) / 2d;

                    // Delaunay tries to link the points up into equilateral triangles
                    double avgEdgeLength = Math2D.GetDelaunayTriangulation(positions.Select(o => o.ToPoint()).ToArray(), true).
                        Select(o => (positions[o.Item1] - positions[o.Item2]).Length).
                        Average();

                    // Assuming the triangles are roughly equilateral, the radius can be calculated with some trig
                    //https://www.quora.com/An-equilateral-triangle-of-side-6cm-is-inscribed-in-a-circle-How-do-I-find-the-radius-of-the-circle
                    cellSize = avgEdgeLength / Math.Sqrt(3);
                    cellSize *= 2;      // need size, not radius

                    #endregion
                    break;

                default:
                    throw new ApplicationException("Unknown SimpleNodeLayout: " + layout.ToString());
            }

            double[][] positionsGeneric = positions.
                Select(o => new[] { o.X, o.Y }).
                ToArray();

            return new NodePositions(positionsGeneric, mapRadius, cellSize);
        }

        #endregion
    }

    #region Class: SelfOrganizingMapsOptions

    /// <summary>
    /// This gets serialized to file
    /// </summary>
    public class SelfOrganizingMapsOptions
    {
        public string[] ImageFolders { get; set; }
    }

    #endregion
}
