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

namespace Game.Newt.Testers.SOM
{
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
        #region Enum: NodeWeightColor

        private enum NodeWeightColor
        {
            Color,
            BlackWhite,
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
            public ImageInput(FeatureRecognizer_Image image, VectorND value_orig, VectorND value_normalized)
            {
                this.Image = image;
                this.Weights_Orig = value_orig;
                this.Weights = value_normalized;
            }

            public readonly FeatureRecognizer_Image Image;
            /// <summary>
            /// These should be from 0 to 1
            /// </summary>
            public readonly VectorND Weights_Orig;
            public VectorND Weights { get; private set; }
        }

        #endregion

        #region Class: OverlayPolygonStats

        private class OverlayPolygonStats
        {
            public OverlayPolygonStats(SOMNode node, ISOMInput[] images, Rect canvasAABB, Vector cursorOffset, Canvas overlay)
            {
                this.Node = node;
                this.Images = images;
                this.CanvasAABB = canvasAABB;
                this.CursorOffset = cursorOffset;
                this.Overlay = overlay;
            }

            public readonly SOMNode Node;
            public readonly ISOMInput[] Images;

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
        private ISOMInput[][] _imagesByNode = null;
        private bool _wasEllipseTransferred = false;

        #endregion

        #region Constructor

        public SelfOrganizingMapsWindow()
        {
            InitializeComponent();

            #region cboSimpleInputColor

            foreach (SimpleColorScheme scheme in Enum.GetValues(typeof(SimpleColorScheme)))
            {
                cboSimpleInputColor.Items.Add(scheme);
                cboSimpleOutputColor.Items.Add(scheme);
            }
            cboSimpleInputColor.SelectedIndex = 0;
            cboSimpleOutputColor.SelectedIndex = 0;

            #endregion
            #region cboThreePixelsTop

            foreach (SimpleColorComponent component in Enum.GetValues(typeof(SimpleColorComponent)))
            {
                cboThreePixelsTop.Items.Add(component);
                cboThreePixelsMid.Items.Add(component);
                cboThreePixelsBot.Items.Add(component);
            }
            cboThreePixelsTop.SelectedIndex = 0;
            cboThreePixelsMid.SelectedIndex = 1;
            cboThreePixelsBot.SelectedIndex = 2;

            #endregion
            #region cboSimpleNodeLayout

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

            #endregion
            #region cboConvColor

            foreach (NodeWeightColor color in Enum.GetValues(typeof(NodeWeightColor)))
            {
                cboConvColor.Items.Add(color);
            }
            cboConvColor.SelectedIndex = Array.IndexOf(UtilityCore.GetEnums<NodeWeightColor>(), NodeWeightColor.BlackWhite);

            #endregion
            #region cboConvNormalization

            foreach (NormalizationType norm in Enum.GetValues(typeof(NormalizationType)))
            {
                cboConvNormalization.Items.Add(norm);
            }
            cboConvNormalization.SelectedIndex = Array.IndexOf(UtilityCore.GetEnums<NormalizationType>(), NormalizationType.Normalize);

            #endregion
            #region cboAttraction

            foreach (SOMAttractionFunction norm in Enum.GetValues(typeof(SOMAttractionFunction)))
            {
                cboAttraction.Items.Add(norm);
            }
            cboAttraction.SelectedIndex = Array.IndexOf(UtilityCore.GetEnums<SOMAttractionFunction>(), SOMAttractionFunction.Guassian);

            #endregion
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

        private void Polygon_MouseMove(Polygon poly, SOMNode node, ISOMInput[] inputs, MouseEventArgs e)
        {
            try
            {
                if (_overlayPolyStats == null || _overlayPolyStats.Node.Token != node.Token)
                {
                    BuildOverlay2D(node, inputs, chkPreviewCount.IsChecked.Value, chkPreviewNodeHash.IsChecked.Value, chkPreviewImageHash.IsChecked.Value, chkPreviewSpread.IsChecked.Value, chkPreviewPerImageSpread.IsChecked.Value);
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
        private void Polygon_MouseLeave()
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

        private void Polygon_MouseMove_OLD(object sender, MouseEventArgs e)
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
        private void Polygon_MouseLeave_OLD(object sender, MouseEventArgs e)
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
        private void Polygon_Click(Polygon sender, SOMNode node, ISOMInput[] images, MouseEventArgs e)
        {
            try
            {
                Color nodeColor = UtilityWPF.ExtractColor(sender.Fill);

                RadialGradientBrush brush = new RadialGradientBrush();
                brush.GradientStops.Add(new GradientStop(UtilityWPF.AlphaBlend(Colors.White, nodeColor, .8), 0));
                brush.GradientStops.Add(new GradientStop(UtilityWPF.AlphaBlend(Colors.White, nodeColor, .58), 1.9));

                HighDimensionVisualizer visualizer = new HighDimensionVisualizer(GetPreviewImage, SelfOrganizingMapsWPF.GetNodeColor, chkVisualizerStaticDots.IsChecked.Value, chkVisualizer3D.IsChecked.Value);
                visualizer.AddStaticItems(HighDimensionVisualizer.GetSOMNodeStaticPositions(node, _nodes, HighDimensionVisualizer.RADIUS));
                visualizer.AddItems(images);

                Window window = new Window()
                {
                    Width = 400,
                    Height = 400,
                    Title = this.Title + " " + images.Length.ToString(),
                    Background = brush,
                    ResizeMode = ResizeMode.CanResizeWithGrip,
                    Content = visualizer,
                    Owner = this,       // keep it on top of this window
                };

                window.Show();
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

                Func<FeatureRecognizer_Image, double[]> getValueFromImage = GetValuesFromImage_Delegate();

                ImageInput[] inputs = GetImageInputs(_images, maxImages, (NormalizationType)cboConvNormalization.SelectedValue, getValueFromImage);

                if (chkConvMaxSpreadPercent.IsChecked.Value)
                {
                    double maxSpreadPercent = trkConvMaxSpread.Value / 100d;

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
        private void KMeans_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_images.Count == 0)
                {
                    MessageBox.Show("Please add some images first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int maxImages = trkMaxInputs.Value.ToInt_Round();

                Func<FeatureRecognizer_Image, double[]> getValueFromImage = GetValuesFromImage_Delegate();

                ImageInput[] inputs = GetImageInputs(_images, maxImages, (NormalizationType)cboConvNormalization.SelectedValue, getValueFromImage);

                DoKMeans(inputs);
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
            var getNodeColor = new Func<SOMNode, Color>(o => GetColor(o.Weights.VectorArray, scheme));

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
                    var events = new SelfOrganizingMapsWPF.BlobEvents(Polygon_MouseMove, Polygon_MouseLeave, null);
                    SelfOrganizingMapsWPF.ShowResults2D_Blobs(panelDisplay, result, getNodeColor, events);

                    // This is for the manual manipulate buttons
                    _nodes = result.Nodes;
                    _imagesByNode = result.InputsByNode;
                    _wasEllipseTransferred = false;
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
                .GetColors_Byte();

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
                .GetColors_Byte();

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

            var getNodeColor = chkRandomNodeColors.IsChecked.Value ? _getNodeColor_Random : SelfOrganizingMapsWPF.GetNodeColor;

            var events = new SelfOrganizingMapsWPF.BlobEvents(Polygon_MouseMove, Polygon_MouseLeave, Polygon_Click);

            SelfOrganizingMapsWPF.ShowResults2D_Blobs(panelDisplay, result, getNodeColor, events);

            // This is for the manual manipulate buttons
            _nodes = result.Nodes;
            _imagesByNode = result.InputsByNode;
            _wasEllipseTransferred = false;
        }
        private void DoConvolution2(ImageInput[] inputs, double maxSpreadPercent, int minNodeItemsForSplit)
        {
            SOMRules rules = GetSOMRules();

            SOMResult result = SelfOrganizingMaps.TrainSOM(inputs, rules, maxSpreadPercent, true, false);

            var getNodeColor = chkRandomNodeColors.IsChecked.Value ? _getNodeColor_Random : SelfOrganizingMapsWPF.GetNodeColor;

            var events = new SelfOrganizingMapsWPF.BlobEvents(Polygon_MouseMove, Polygon_MouseLeave, null);

            SelfOrganizingMapsWPF.ShowResults2D_Blobs(panelDisplay, result, getNodeColor, events);

            // This is for the manual manipulate buttons
            _nodes = result.Nodes;
            _imagesByNode = result.InputsByNode;
            _wasEllipseTransferred = false;
        }

        private Func<FeatureRecognizer_Image, double[]> GetValuesFromImage_Delegate()
        {
            int imageSize = trkConvImageSize.Value.ToInt_Round();
            double scaleValue = trkConvValue.Value;

            bool isColor = (NodeWeightColor)cboConvColor.SelectedValue == NodeWeightColor.Color;

            var filterType = (ImageFilterType)cboConvImageFilter.SelectedValue;
            switch (filterType)
            {
                case ImageFilterType.None:
                    if (isColor) return new Func<FeatureRecognizer_Image, double[]>(o => GetValuesFromImage_Resize_Color(o, imageSize, scaleValue));
                    else return new Func<FeatureRecognizer_Image, double[]>(o => GetValuesFromImage_Resize_Gray(o, imageSize, scaleValue));

                case ImageFilterType.Edge:
                    if (isColor) return new Func<FeatureRecognizer_Image, double[]>(o => GetValuesFromImage_ConvMaxpool_Color(o, Convolutions.GetEdgeSet_Sobel(), imageSize, scaleValue));
                    else return new Func<FeatureRecognizer_Image, double[]>(o => GetValuesFromImage_ConvMaxpool_Gray(o, Convolutions.GetEdgeSet_Sobel(), imageSize, scaleValue));

                case ImageFilterType.Edge_Horzontal:
                    if (isColor) return new Func<FeatureRecognizer_Image, double[]>(o => GetValuesFromImage_ConvMaxpool_Color(o, Convolutions.GetEdge_Sobel(false), imageSize, scaleValue));
                    else return new Func<FeatureRecognizer_Image, double[]>(o => GetValuesFromImage_ConvMaxpool_Gray(o, Convolutions.GetEdge_Sobel(false), imageSize, scaleValue));

                case ImageFilterType.Edge_Vertical:
                    if (isColor) return new Func<FeatureRecognizer_Image, double[]>(o => GetValuesFromImage_ConvMaxpool_Color(o, Convolutions.GetEdge_Sobel(true), imageSize, scaleValue));
                    else return new Func<FeatureRecognizer_Image, double[]>(o => GetValuesFromImage_ConvMaxpool_Gray(o, Convolutions.GetEdge_Sobel(true), imageSize, scaleValue));

                case ImageFilterType.Edge_DiagonalUp:
                    if (isColor) return new Func<FeatureRecognizer_Image, double[]>(o => GetValuesFromImage_ConvMaxpool_Color(o, Convolutions.Rotate_45(Convolutions.GetEdge_Sobel(false), false), imageSize, scaleValue));
                    else return new Func<FeatureRecognizer_Image, double[]>(o => GetValuesFromImage_ConvMaxpool_Gray(o, Convolutions.Rotate_45(Convolutions.GetEdge_Sobel(false), false), imageSize, scaleValue));

                case ImageFilterType.Edge_DiagonalDown:
                    if (isColor) return new Func<FeatureRecognizer_Image, double[]>(o => GetValuesFromImage_ConvMaxpool_Color(o, Convolutions.Rotate_45(Convolutions.GetEdge_Sobel(true), false), imageSize, scaleValue));
                    else return new Func<FeatureRecognizer_Image, double[]>(o => GetValuesFromImage_ConvMaxpool_Gray(o, Convolutions.Rotate_45(Convolutions.GetEdge_Sobel(true), false), imageSize, scaleValue));

                default:
                    throw new ApplicationException("Unknown ImageFilterType: " + filterType.ToString());
            }
        }

        private static double[] GetValuesFromImage_Resize_Color(FeatureRecognizer_Image image, int size, double scaleValue)
        {
            BitmapSource bitmap = new BitmapImage(new Uri(image.Filename));
            bitmap = UtilityWPF.ResizeImage(bitmap, size, true);

            var convs = UtilityWPF.ConvertToConvolution_RGB(bitmap, scaleValue);
            if (convs.Item1.Width != convs.Item1.Height)
            {
                convs = Tuple.Create(
                    Convolutions.ExtendBorders(convs.Item1, size, size),        //NOTE: width or height is already the desired size, this will just enlarge the other to make it square
                    Convolutions.ExtendBorders(convs.Item2, size, size),
                    Convolutions.ExtendBorders(convs.Item3, size, size));
            }

            return MergeConvs(convs.Item1, convs.Item2, convs.Item3);
        }
        private static double[] GetValuesFromImage_Resize_Gray(FeatureRecognizer_Image image, int size, double scaleValue)
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

        private static double[] GetValuesFromImage_ConvMaxpool_Color(FeatureRecognizer_Image image, ConvolutionBase2D kernel, int size, double scaleValue)
        {
            const int INITIALSIZE = 80;

            BitmapSource bitmap = new BitmapImage(new Uri(image.Filename));
            bitmap = UtilityWPF.ResizeImage(bitmap, INITIALSIZE, true);

            var convs = UtilityWPF.ConvertToConvolution_RGB(bitmap, scaleValue);

            var final = new[] { convs.Item1, convs.Item2, convs.Item3 }.
                Select(o =>
                {
                    Convolution2D retVal = o;

                    if (retVal.Width != retVal.Height)
                    {
                        retVal = Convolutions.ExtendBorders(retVal, INITIALSIZE, INITIALSIZE);        //NOTE: width or height is already the desired size, this will just enlarge the other to make it square
                    }

                    retVal = Convolutions.Convolute(retVal, kernel);
                    retVal = Convolutions.MaxPool(retVal, size, size);
                    return Convolutions.Abs(retVal);
                }).
                ToArray();

            return MergeConvs(final[0], final[1], final[2]);
        }
        private static double[] GetValuesFromImage_ConvMaxpool_Gray(FeatureRecognizer_Image image, ConvolutionBase2D kernel, int size, double scaleValue)
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

        private static double[] MergeConvs(Convolution2D red, Convolution2D green, Convolution2D blue)
        {
            if (red.Values.Length != green.Values.Length || red.Values.Length != blue.Values.Length)
            {
                throw new ArgumentException(string.Format("The convolutions need to be the same size: {0}, {1}, {2}", red.Values.Length, green.Values.Length, blue.Values.Length));
            }

            double[] retVal = new double[red.Values.Length * 3];

            for (int cntr = 0; cntr < red.Values.Length; cntr++)
            {
                int index = cntr * 3;
                retVal[index + 0] = red.Values[cntr];
                retVal[index + 1] = green.Values[cntr];
                retVal[index + 2] = blue.Values[cntr];
            }

            return retVal;
        }

        #endregion
        #region Private Methods - kmeans

        //TODO: Move this section to SelfOrganizingMaps.TrainKMeans()

        private void DoKMeans(ImageInput[] inputs)
        {
            int numNodes = trkKMeansNumNodes.Value.ToInt_Round();

            SOMResult result = SelfOrganizingMaps.TrainKMeans(inputs, numNodes, true);

            var getNodeColor = chkRandomNodeColors.IsChecked.Value ? _getNodeColor_Random : SelfOrganizingMapsWPF.GetNodeColor;

            var events = new SelfOrganizingMapsWPF.BlobEvents(Polygon_MouseMove, Polygon_MouseLeave, Polygon_Click);

            SelfOrganizingMapsWPF.ShowResults2D_Blobs(panelDisplay, result, getNodeColor, events);

            // This is for the manual manipulate buttons
            _nodes = result.Nodes;
            _imagesByNode = result.InputsByNode;
            _wasEllipseTransferred = false;
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

                polygon.MouseMove += Polygon_MouseMove_OLD;
                polygon.MouseLeave += Polygon_MouseLeave_OLD;

                retVal.Children.Add(polygon);

                #endregion
            }

            return retVal;
        }

        #endregion
        #region Private Methods - tooltip

        private void BuildOverlay2D(SOMNode node, ISOMInput[] images, bool showCount, bool showNodeHash, bool showImageHash, bool showSpread, bool showPerImageDistance)
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

            var cursorRect = SelfOrganizingMapsWPF.GetMouseCursorRect(0);
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
                OutlinedTextBlock text = SelfOrganizingMapsWPF.GetOutlineText("images ", SMALLFONT1, SMALLLINE1);
                text.Margin = new Thickness(0, 0, 4, 0);
                textPanel.Children.Add(text);

                // count
                text = SelfOrganizingMapsWPF.GetOutlineText(images.Length.ToString("N0"), LARGEFONT1, LARGELINE1);
                textPanel.Children.Add(text);

                // Place on canvas
                textPanel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));       // aparently, the infinity is important to get an accurate desired size
                Size textSize = textPanel.DesiredSize;

                Rect textRect = SelfOrganizingMapsWPF.GetFreeSpot(textSize, new Point(0, 0), new Vector(0, 1), rectangles);
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

            if (showSpread && images.Length > 0)
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

                Rect spreadRect = SelfOrganizingMapsWPF.GetFreeSpot(spreadSize, new Point(0, 0), new Vector(0, 1), rectangles);
                rectangles.Add(spreadRect);

                Canvas.SetLeft(spreadPanel, spreadRect.Left);
                Canvas.SetTop(spreadPanel, spreadRect.Top);

                canvas.Children.Add(spreadPanel);
            }

            #endregion

            VectorND nodeCenter = images.Length == 0 ? node.Weights : MathND.GetCenter(nodeImages);

            #region node hash

            if (showNodeHash)
            {
                ImageInput nodeImage = new ImageInput(null, node.Weights, node.Weights);

                double nodeDist = (nodeImage.Weights - nodeCenter).Length;
                double nodeDistPercent = nodeSpread.IsNearZero() ? 1d : (nodeDist / nodeSpread);     // if zero or one node, then spread will be zero

                Tuple<UIElement, VectorInt> nodeCtrl = GetPreviewImage(nodeImage, true, NODEHASHSIZE, showPerImageDistance, nodeDistPercent);

                // Place on canvas
                Rect nodeRect = SelfOrganizingMapsWPF.GetFreeSpot(new Size(nodeCtrl.Item2.X, nodeCtrl.Item2.Y), new Point(0, 0), new Vector(0, -1), rectangles);
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
                    imageDistPercent = (image.Weights - nodeCenter).Length / nodeSpread;
                }

                // Create the image (and any other graphics for that image)
                Tuple<UIElement, VectorInt> imageCtrl = GetPreviewImage(image, showImageHash, IMAGESIZE, showPerImageDistance, imageDistPercent);

                // Find a spot for it
                var imageRect = Enumerable.Range(0, 10).
                    Select(o =>
                    {
                        Vector direction = Math3D.GetRandomVector_Circular_Shell(1).ToVector2D();

                        Rect imageRect2 = SelfOrganizingMapsWPF.GetFreeSpot(new Size(imageCtrl.Item2.X, imageCtrl.Item2.Y), new Point(0, 0), direction, rectangles);

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
            OutlinedTextBlock text = SelfOrganizingMapsWPF.GetOutlineText(leftText, fontSizeLeft, strokeThicknessLeft, invert);
            text.HorizontalAlignment = HorizontalAlignment.Right;
            Grid.SetColumn(text, 0);
            Grid.SetRow(text, row);
            grid.Children.Add(text);

            text = SelfOrganizingMapsWPF.GetOutlineText(rightText, fontSizeRight, strokeThicknessRight, invert);
            text.HorizontalAlignment = HorizontalAlignment.Left;
            Grid.SetColumn(text, 2);
            Grid.SetRow(text, row);
            grid.Children.Add(text);
        }

        /// <summary>
        /// This is used by child HighDimensionVisualizer windows
        /// </summary>
        private Tuple<UIElement, VectorInt> GetPreviewImage(ISOMInput input)
        {
            bool showHash = chkVisualizerShowHash.IsChecked.Value;

            ImageInput nodeImage = input as ImageInput;
            if (nodeImage == null)
            {
                // This is probably a node, instead of an input image (the nodes are sent as static control points)
                nodeImage = new ImageInput(null, input.Weights, input.Weights);
                showHash = true;
            }

            return GetPreviewImage(nodeImage, showHash, 80, false, -1);
        }

        /// <summary>
        /// This creates an image, and any other descriptions
        /// </summary>
        private static Tuple<UIElement, VectorInt> GetPreviewImage(ImageInput image, bool showHash, int imageSize, bool showPerImageDistance, double imageDistPercent)
        {
            const int ALIASMULT = 20;

            #region image

            BitmapSource bitmap;
            if (showHash)
            {
                #region hash

                int width, height;

                bool isColor = false;
                double widthHeight = Math.Sqrt(image.Weights_Orig.Size);
                if (widthHeight.ToInt_Floor() == widthHeight.ToInt_Ceiling())
                {
                    // Black and white, 2D
                    width = height = widthHeight.ToInt_Floor();
                }
                else
                {
                    double widthHeight2 = Math.Sqrt(image.Weights_Orig.Size / 3);
                    if (widthHeight2.ToInt_Floor() == widthHeight2.ToInt_Ceiling())
                    {
                        // Color, 2D
                        isColor = true;
                        width = height = widthHeight2.ToInt_Floor();
                    }
                    else
                    {
                        // Black and white, 1D
                        width = image.Weights_Orig.Size;
                        height = 1;
                    }
                }

                //double? maxValue = image.Weights_Orig.Max() > 1d ? (double?)null : 1d;
                //Convolution2D conv = new Convolution2D(image.Weights_Orig, width, height, false);
                //bitmap = Convolutions.GetBitmap_Aliased(conv, absMaxValue: maxValue, negPosColoring: ConvolutionResultNegPosColoring.BlackWhite, forcePos_WhiteBlack: false);

                if (isColor)
                {
                    bitmap = UtilityWPF.GetBitmap_Aliased_RGB(image.Weights_Orig.VectorArray, width, height, width * ALIASMULT, height * ALIASMULT);
                }
                else
                {
                    bitmap = UtilityWPF.GetBitmap_Aliased(image.Weights_Orig.VectorArray, width, height, width * ALIASMULT, height * ALIASMULT);
                }

                #endregion
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
                trkLearningRate.Value,
                (SOMAttractionFunction)cboAttraction.SelectedValue);
        }

        private static ImageInput[] GetImageInputs(IList<FeatureRecognizer_Image> images, int maxInputs, NormalizationType normalizationType, Func<FeatureRecognizer_Image, double[]> getValueFromImage)
        {
            return UtilityCore.RandomOrder(images, maxInputs).
                AsParallel().
                Select(o =>
                    {
                        double[] values = getValueFromImage(o);
                        double[] normalized = GetNormalizedVector(values, normalizationType);
                        return new ImageInput(o, new VectorND(values), new VectorND(normalized));
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
                    return vector.
                        ToVectorND().
                        ToScaledCap().
                        VectorArray;

                case NormalizationType.ToUnit:
                    return vector.
                        ToVectorND().
                        ToUnit(false).
                        VectorArray;

                default:
                    throw new ApplicationException("Unknown NormalizationType: " + normalizationType.ToString());
            }
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
