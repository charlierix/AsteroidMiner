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
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Controls2D;
using Game.Newt.Testers.Convolution;

namespace Game.Newt.Testers
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// http://www.ai-junkie.com/ann/som/som1.html
    /// https://www.mql5.com/en/articles/283?source=metaeditor5_article
    ///
    /// Scroll to the section "IV. Determining the Quality of SOMs".  Instead of generating a second black/white image, draw borders between colors where it's too black
    /// http://davis.wpi.edu/~matt/courses/soms/
    /// implementation code (screen has the important code)
    /// http://davis.wpi.edu/~matt/courses/soms/soms.java
    /// http://davis.wpi.edu/~matt/courses/soms/Screen.java
    /// http://davis.wpi.edu/~matt/courses/soms/fpoint.java
    /// </remarks>
    public partial class SelfOrganizingMaps : Window
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
        #region Enum: SimpleNodeLayout

        private enum SimpleNodeLayout
        {
            Disk_All,       // the area of each cell is fixed
            Disk_NonZero,      //
            Blobs,      // the area of the cell is how many inputs are in that cell
            Grid_UniformSize,
            //Grid_VariableSize,
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

        #region Class: SOMRules

        private class SOMRules
        {
            public SOMRules(int numNodes, int numIterations, double initialRadiusPercent, double learningRate)
            {
                this.NumNodes = numNodes;
                this.NumIterations = numIterations;
                this.InitialRadiusPercent = initialRadiusPercent;
                this.LearningRate = learningRate;
            }

            public readonly int NumNodes;
            public readonly int NumIterations;
            public readonly double InitialRadiusPercent;
            public readonly double LearningRate;
        }

        #endregion
        #region Class: SOMNode

        private class SOMNode
        {
            public readonly long Token = TokenGenerator.NextToken();

            /// <summary>
            /// These should be from 0 to 1
            /// </summary>
            public double[] Weights { get; set; }

            /// <summary>
            /// This doesn't seem to have anything to do with the SOM algorithm.  An example online stores the nodes
            /// in a 2D array, so the position in that array determines the on screen rectangle.
            /// </summary>
            /// <remarks>
            /// By being explicit with position, the nodes could be layed out as rectangles (like all the examples), or arranged
            /// as a voronoi
            /// 
            /// Once the weight training is done, a second pass could arrange positions using a similar SOM based on the shape
            /// of the matches?  Or just use spring forces (larger number of matching inputs should get a bigger area)
            /// </remarks>
            public double[] Position { get; set; }

            public SOMNode Clone()
            {
                return new SOMNode()
                {
                    Weights = this.Weights.ToArray(),
                    Position = this.Position.ToArray(),
                };
            }
        }

        #endregion
        #region Class: ImageInput

        private class ImageInput
        {
            public ImageInput(FeatureRecognizer_Image image, double[] value_orig, double[] value_normalized)
            {
                this.Image = image;
                this.Value_Orig = value_orig;
                this.Value_Normalized = value_normalized;
            }

            public readonly FeatureRecognizer_Image Image;
            /// <summary>
            /// These should be from 0 to 1
            /// </summary>
            public readonly double[] Value_Orig;
            public readonly double[] Value_Normalized;
        }

        #endregion

        #region Class: SOMNodes_Inputs

        private class SOMNodesInputs
        {
            public SOMNodesInputs(SOMNode[] nodes_all, ImageInput[][] imagesByNode_all)
            {
                this.Nodes_all = nodes_all;
                this.ImagesByNode_all = imagesByNode_all;

                SOMNode[] nodes_filled = nodes_all.ToArray();
                ImageInput[][] imagesByNode_filled = imagesByNode_all.ToArray();
                RemoveZeroNodes(ref nodes_filled, ref imagesByNode_filled);

                this.Nodes_filled = nodes_filled;
                this.ImagesByNode_filled = imagesByNode_filled;
            }

            public readonly SOMNode[] Nodes_all;
            public readonly ImageInput[][] ImagesByNode_all;

            public readonly SOMNode[] Nodes_filled;
            public readonly ImageInput[][] ImagesByNode_filled;
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

        public SelfOrganizingMaps()
        {
            InitializeComponent();

            foreach (SimpleColorScheme scheme in Enum.GetValues(typeof(SimpleColorScheme)))
            {
                cboSimpleInputColor.Items.Add(scheme);
                cboSimpleOutputColor.Items.Add(scheme);
            }
            cboSimpleInputColor.SelectedIndex = 0;
            cboSimpleOutputColor.SelectedIndex = 0;

            foreach (SimpleColorComponent component in Enum.GetValues(typeof(SimpleColorComponent)))
            {
                cboThreePixelsTop.Items.Add(component);
                cboThreePixelsMid.Items.Add(component);
                cboThreePixelsBot.Items.Add(component);
            }
            cboThreePixelsTop.SelectedIndex = 0;
            cboThreePixelsMid.SelectedIndex = 1;
            cboThreePixelsBot.SelectedIndex = 2;

            // skipping Grid_UniformSize.  There's not much point implementing that, it's unnatural to force categories into a square
            cboSimpleNodeLayout.Items.Add(SimpleNodeLayout.Blobs);
            cboSimpleNodeLayout.Items.Add(SimpleNodeLayout.Disk_NonZero);
            cboSimpleNodeLayout.Items.Add(SimpleNodeLayout.Disk_All);
            cboSimpleNodeLayout.SelectedIndex = 0;

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

        private void SimplePrep_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AdjustStepPrep(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void SimpleStep_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AdjustStepPrep(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResizedImage_Click(object sender, RoutedEventArgs e)
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

                var getValueFromImage = new Func<FeatureRecognizer_Image, double[]>(o => GetValuesFromImage_ResizeGray(o, imageSize, scaleValue));

                ImageInput[] inputs = GetImageInputs(_images, maxImages, (NormalizationType)cboConvNormalization.SelectedValue, getValueFromImage);

                //var test = inputs.Select(o => Tuple.Create(o.Value[0], o.Value[1], o.Value[2])).ToArray();

                DoConvolution(inputs);     // need the await for the catch block
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void EdgeMaxpool_Click(object sender, RoutedEventArgs e)
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

                var getValueFromImage = new Func<FeatureRecognizer_Image, double[]>(o => GetValuesFromImage_EdgeMaxpool(o, imageSize, scaleValue));

                ImageInput[] inputs = GetImageInputs(_images, maxImages, (NormalizationType)cboConvNormalization.SelectedValue, getValueFromImage);

                //var test = inputs.Select(o => Tuple.Create(o.Value[0], o.Value[1], o.Value[2])).ToArray();

                DoConvolution(inputs);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void EdgeMaxpoolMult_Click(object sender, RoutedEventArgs e)
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

                var getValueFromImage = new Func<FeatureRecognizer_Image, double[]>(o => GetValuesFromImage_EdgeMaxpool(o, imageSize, scaleValue));

                ImageInput[] inputs = GetImageInputs(_images, maxImages, (NormalizationType)cboConvNormalization.SelectedValue, getValueFromImage);

                //var test = inputs.Select(o => Tuple.Create(o.Value[0], o.Value[1], o.Value[2])).ToArray();

                DoConvolution_Multiple1(inputs, 10);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void EdgeMaxpool2_Click(object sender, RoutedEventArgs e)
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

                var getValueFromImage = new Func<FeatureRecognizer_Image, double[]>(o => GetValuesFromImage_EdgeMaxpool(o, imageSize, scaleValue));

                ImageInput[] inputs = GetImageInputs(_images, maxImages, (NormalizationType)cboConvNormalization.SelectedValue, getValueFromImage);

                //var test = inputs.Select(o => Tuple.Create(o.Value[0], o.Value[1], o.Value[2])).ToArray();

                DoConvolution2(inputs, maxSpreadPercent, 4);
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
            Random rand = StaticRandom.GetRandomForThread();

            SOMRules rules = GetSOMRules();

            SimpleNodeLayout layout = (SimpleNodeLayout)cboSimpleNodeLayout.SelectedItem;

            NodePositions positions = GetNodePositions2D(rules.NumNodes, layout);
            double[][] weights = GetRandomWeights(rules.NumNodes, inputs);

            SOMNode[] nodes = Enumerable.Range(0, rules.NumNodes).
                Select(o => new SOMNode() { Position = positions.Positions[o], Weights = weights[o] }).
                ToArray();

            // Train
            nodes = TrainSOM(nodes, inputs, rules, 0, rules.NumIterations - 1, positions.MapRadius);

            SimpleColorScheme scheme = (SimpleColorScheme)cboSimpleOutputColor.SelectedItem;
            var getNodeColor = new Func<SOMNode, Color>(o => GetColor(o.Weights, scheme));

            // Show results
            switch (layout)
            {
                case SimpleNodeLayout.Disk_All:
                    ShowResults_Disk(panelDisplay, nodes, inputs, getNodeColor, true);
                    break;

                case SimpleNodeLayout.Disk_NonZero:
                    ShowResults_Disk(panelDisplay, nodes, inputs, getNodeColor, false);
                    break;

                case SimpleNodeLayout.Blobs:
                    ShowResults_Blobs(panelDisplay, nodes, inputs, getNodeColor);
                    break;

                case SimpleNodeLayout.Grid_UniformSize:
                    throw new ApplicationException("finish this");
                    //ShowResults_Grid(panelDisplay, nodes, (SimpleColorScheme)cboSimpleOutputColor.SelectedItem, positions.GridCellSize);
                    break;

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
        #region Private Methods - simple - draw - disk

        private static void RemoveZeroNodes(ref SOMNode[] nodes, ref ImageInput[][] imagesByNode)
        {
            List<SOMNode> subNodes = new List<SOMNode>();
            List<ImageInput[]> subImagesByNode = new List<ImageInput[]>();

            for (int cntr = 0; cntr < nodes.Length; cntr++)
            {
                if (imagesByNode[cntr].Length == 0)
                {
                    continue;
                }

                subNodes.Add(nodes[cntr]);
                subImagesByNode.Add(imagesByNode[cntr]);
            }

            nodes = subNodes.ToArray();
            imagesByNode = subImagesByNode.ToArray();
        }

        private static void ArrangeNodes_LikesAttract(ref SOMNode[] nodes, ref ImageInput[][] imagesByNode)
        {
            double[][] weights = nodes.
                Select(o => o.Weights).
                ToArray();

            // Get the high dimension distances
            var desiredDistances = MathND.GetDistancesBetween(weights);

            // Merge nodes that have the same high dimension position
            if (MergeTouchingNodes(ref nodes, ref imagesByNode, desiredDistances))
            {
                // Redo it
                weights = nodes.
                    Select(o => o.Weights).
                    ToArray();

                desiredDistances = MathND.GetDistancesBetween(weights);
            }

            // Pull the low dimension positions to try to match the high dimension distances
            nodes = MoveNodes_BallOfSprings(nodes, desiredDistances, 1500);
        }

        //NOTE: This method is a dead end.  Blobs is a better way
        private static SOMNode[] ArrangeNodes_EnforceSizes(SOMNode[] nodes, ImageInput[][] imagesByNode)
        {
            if (nodes.Length <= 3)
            {
                return nodes;
            }

            // Run a voronoi against the nodes, and get sizes/neighbors
            SOMNodeVoronoiSet voronoiResults = GetVoronoiNeighbors(nodes, imagesByNode);

            // Get image sizes
            double totalImages = imagesByNode.
                Sum(o => o.Length).
                ToDouble();

            double[] imagePercentages = imagesByNode.
                Select(o => o.Length.ToDouble() / totalImages).
                ToArray();

            // Init desired distances to be the current distances between nodes
            var desiredDistances = voronoiResults.NeighborLinks.
                Select(o => Tuple.Create(o.Item1, o.Item2, MathND.GetDistance(nodes[o.Item1].Position, nodes[o.Item2].Position))).
                ToArray();


            // adjust link distances based on error between actual cell area and desired



            var debugPercents = Enumerable.Range(0, nodes.Length).
                Select(o => new { Area = Math.Round(voronoiResults.Cells[o].SizePercentOfTotal, 2), Image = Math.Round(imagePercentages[o], 2) }).
                Select(o => new { o.Area, o.Image, Diff = Math.Round(Math.Abs(o.Area - o.Image), 2) }).
                ToArray();




            return nodes;
        }

        private static SOMNodeVoronoiSet GetVoronoiNeighbors(SOMNode[] nodes, ImageInput[][] imagesByNode)
        {
            if (nodes[0].Position.Length == 2)
            {
                return GetVoronoiNeighbors_2D(nodes, imagesByNode);
            }
            else if (nodes[0].Position.Length == 3)
            {
                return GetVoronoiNeighbors_3D(nodes);
            }
            else
            {
                throw new ApplicationException("Can only handle 2D and 3D points");
            }
        }
        private static SOMNodeVoronoiSet GetVoronoiNeighbors_2D(SOMNode[] nodes, ImageInput[][] imagesByNode)
        {
            // Get Voronoi
            Point[] points = nodes.
                Select(o => new Point(o.Position[0], o.Position[1])).
                ToArray();

            VoronoiResult2D result = Math2D.GetVoronoi(points, true);
            result = Math2D.CapVoronoiCircle(result);

            // Get cell sizes
            double[] sizes = Enumerable.Range(0, nodes.Length).
                Select(o => Math2D.GetAreaPolygon(result.GetPolygon(o, 1))).        // there are no rays
                ToArray();

            double totalSize = sizes.Sum();


            // Radius constraints








            // Figure out neighbors
            int[][] neighbors = result.GetNeighbors();

            Tuple<int, int>[] neighborLinks = result.GetNeighborLinks();

            // Build Cells
            SOMNodeVoronoiCell[] cells = Enumerable.Range(0, nodes.Length).
                Select(o => new SOMNodeVoronoiCell(o, nodes[o], neighbors[o], sizes[o], sizes[o] / totalSize)).
                ToArray();

            return new SOMNodeVoronoiSet(cells, neighborLinks, neighbors);
        }
        private static SOMNodeVoronoiSet GetVoronoiNeighbors_3D(SOMNode[] nodes)
        {
            throw new NotImplementedException();
        }

        private static bool MergeTouchingNodes(ref SOMNode[] nodes, ref ImageInput[][] imagesByNode, Tuple<int, int, double>[] distances)
        {
            const double MINDIST = .01;

            // Find touching
            var touching = distances.
                Where(o => o.Item3 < MINDIST).
                ToArray();

            if (touching.Length == 0)
            {
                return false;
            }

            #region Merge key pairs

            // There could be several pairs that need to be joined.  ex:
            //      {0,2} {0,3} {2,5}       ->      {0,2,3,5}
            //      {1,6}       ->      {1,6}

            List<List<int>> sets = new List<List<int>>();

            foreach (var pair in touching)
            {
                List<int> existing = sets.FirstOrDefault(o => o.Contains(pair.Item1) || o.Contains(pair.Item2));
                if (existing == null)
                {
                    existing = new List<int>();
                    existing.Add(pair.Item1);
                    existing.Add(pair.Item2);
                    sets.Add(existing);
                }
                else
                {
                    if (!existing.Contains(pair.Item1))
                    {
                        existing.Add(pair.Item1);
                    }
                    else if (!existing.Contains(pair.Item2))     // if it didn't contain 1, then it matched on 2, so no need to look for 2
                    {
                        existing.Add(pair.Item2);
                    }
                }
            }

            #endregion
            #region Singular sets

            // Identify stand alone nodes, and add their index to the sets list (makes the next section easier to implement)

            for (int cntr = 0; cntr < nodes.Length; cntr++)
            {
                if (!sets.Any(o => o.Contains(cntr)))
                {
                    List<int> singleSet = new List<int>();
                    singleSet.Add(cntr);
                    sets.Add(singleSet);
                }
            }

            #endregion
            #region Merge nodes

            List<SOMNode> newNodes = new List<SOMNode>();
            List<ImageInput[]> newImagesByNode = new List<ImageInput[]>();

            foreach (List<int> set in sets)
            {
                // Just use the first node (no need to take the average of weights since they're nearly identical, and taking the average position
                // doesn't add any value - later methods will move the node positions around anyway)
                newNodes.Add(nodes[set[0]]);

                if (set.Count == 1)
                {
                    newImagesByNode.Add(imagesByNode[set[0]]);
                }
                else
                {
                    List<ImageInput> mergedImages = new List<ImageInput>();
                    foreach (int index in set)
                    {
                        mergedImages.AddRange(imagesByNode[index]);
                    }

                    newImagesByNode.Add(mergedImages.ToArray());
                }
            }

            #endregion

            nodes = newNodes.ToArray();
            imagesByNode = newImagesByNode.ToArray();

            return true;
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

                if (chkSimpleNodes.IsChecked.Value)
                {
                    #region debug dot

                    double dotSize = Math1D.Avg(imageWidth, imageHeight) * .04;
                    Point dotPos = transform.Transform(voronoi.ControlPoints[cntr]);

                    Ellipse dot = new Ellipse()
                    {
                        Fill = Brushes.Black,
                        Stroke = null,
                        Width = dotSize,
                        Height = dotSize,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Top,
                        Margin = new Thickness(dotPos.X - (dotSize / 2d), dotPos.Y - (dotSize / 2d), 0, 0),
                        Tag = nodes[cntr],
                    };

                    retVal.Children.Add(dot);

                    #endregion
                }
            }

            if (chkSimpleLinks.IsChecked.Value)
            {
                #region debug links

                foreach (var link in voronoi.GetNeighborLinks())
                {
                    Point pos1 = transform.Transform(voronoi.ControlPoints[link.Item1]);
                    Point pos2 = transform.Transform(voronoi.ControlPoints[link.Item2]);

                    Line line = new Line()
                    {
                        Stroke = Brushes.Black,
                        StrokeThickness = 1,
                        X1 = pos1.X,
                        Y1 = pos1.Y,
                        X2 = pos2.X,
                        Y2 = pos2.Y,
                    };

                    retVal.Children.Add(line);
                }

                #endregion
            }

            return retVal;
        }

        private static ImageInput[][] GetImagesByNode(SOMNode[] nodes, ImageInput[] images)
        {
            List<ImageInput>[] retVal = Enumerable.Range(0, nodes.Length).
                Select(o => new List<ImageInput>()).
                ToArray();

            foreach (var image in images)
            {
                var match = GetClosest(nodes, image);
                retVal[match.Item2].Add(image);
            }

            return retVal.
                Select(o => o.ToArray()).
                ToArray();
        }

        #endregion
        #region Private Methods - simple - draw - blobs

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

                if (chkSimpleNodes.IsChecked.Value)
                {
                    #region debug dot

                    double dotSize = Math1D.Avg(imageWidth, imageHeight) * .04;
                    Point dotPos = transform.Transform(voronoi.ControlPoints[cntr]);

                    Ellipse dot = new Ellipse()
                    {
                        Fill = Brushes.Black,
                        Stroke = null,
                        Width = dotSize,
                        Height = dotSize,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Top,
                        Margin = new Thickness(dotPos.X - (dotSize / 2d), dotPos.Y - (dotSize / 2d), 0, 0),
                        Tag = nodes[cntr],
                    };

                    retVal.Children.Add(dot);

                    #endregion
                }
            }

            if (chkSimpleLinks.IsChecked.Value)
            {
                #region debug links

                foreach (var link in voronoi.GetNeighborLinks())
                {
                    Point pos1 = transform.Transform(voronoi.ControlPoints[link.Item1]);
                    Point pos2 = transform.Transform(voronoi.ControlPoints[link.Item2]);

                    Line line = new Line()
                    {
                        Stroke = Brushes.Black,
                        StrokeThickness = 1,
                        X1 = pos1.X,
                        Y1 = pos1.Y,
                        X2 = pos2.X,
                        Y2 = pos2.Y,
                    };

                    retVal.Children.Add(line);
                }

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
        #region Private Methods - simple attempt adjust

        private void AdjustStepPrep(bool isStep)
        {
            if (_nodes == null)
            {
                MessageBox.Show("Do a simple first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            VoronoiStats stats = GetVoronoiStats(_nodes, _imagesByNode);

            if (isStep)
            {
                AdjustStepPrep_Move(stats);

                stats = GetVoronoiStats(_nodes, _imagesByNode);
                AdjustStepPrep_Recolor(stats);
            }
            else
            {
                AdjustStepPrep_Recolor(stats);
            }
        }
        private void AdjustStepPrep_Move(VoronoiStats stats)
        {
            const int COUNT = 20;

            //TODO: There is a high chance of the points bunching up.  Enforce a minimum distance

            // Desired distance
            var desiredDistances = AdjustDesiredDistances(stats.NodeStats, stats.CurrentDistances);

            SOMNode[] nodes = _nodes.ToArray();

            // Move nodes
            nodes = MoveNodes_BallOfSprings(nodes, desiredDistances, COUNT);

            Border border = panelDisplay;
            SimpleColorScheme scheme = (SimpleColorScheme)cboSimpleOutputColor.SelectedItem;

            #region draw

            Point[] points = nodes.
                Select(o => new Point(o.Position[0], o.Position[1])).
                ToArray();

            VoronoiResult2D voronoi = Math2D.GetVoronoi(points, true);
            voronoi = Math2D.CapVoronoiCircle(voronoi);

            Color[] colors = nodes.
                Select(o => GetColor(o.Weights, scheme)).
                ToArray();

            Vector size = new Vector(border.ActualWidth - border.Padding.Left - border.Padding.Right, border.ActualHeight - border.Padding.Top - border.Padding.Bottom);
            Canvas canvas = DrawVoronoi(voronoi, colors, nodes, _imagesByNode, size.X.ToInt_Floor(), size.Y.ToInt_Floor());

            border.Child = canvas;

            #endregion

            _nodes = nodes;
            //_imagesByNode = _imagesByNode;
            _wasEllipseTransferred = false;
        }
        private void AdjustStepPrep_Recolor(VoronoiStats stats)
        {
            const double MAXDIFF = .25;

            #region find controls

            Canvas canvas = panelDisplay.Child as Canvas;
            if (canvas == null)
            {
                MessageBox.Show("Should be a canvas", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var nodeCtrls = _nodes.
                Select(o => new Tuple<Ellipse, Polygon>(FindEllipse(o, canvas), FindPoly(o, canvas))).
                ToArray();

            #endregion

            Color white = UtilityWPF.ColorFromHex("FFF8F6");
            Color red = UtilityWPF.ColorFromHex("8C142A");
            Color green = UtilityWPF.ColorFromHex("49590E");

            for (int cntr = 0; cntr < _nodes.Length; cntr++)
            {
                if (nodeCtrls[cntr].Item2 == null)
                {
                    continue;
                }

                // Use ellipse to show the original color
                if (!_wasEllipseTransferred && nodeCtrls[cntr].Item1 != null)
                {
                    nodeCtrls[cntr].Item1.Fill = nodeCtrls[cntr].Item2.Fill;
                }

                // Polygon should be Red <-- White --> Green based on % difference
                nodeCtrls[cntr].Item2.Stroke = Brushes.Black;

                double diff = stats.NodeStats[cntr].Diff;
                if (diff < -MAXDIFF) diff = -MAXDIFF;
                if (diff > MAXDIFF) diff = MAXDIFF;

                double percent = UtilityCore.GetScaledValue(0, 1, 0, MAXDIFF, Math.Abs(diff));

                Color cellColor = Colors.Transparent;
                if (diff < 0)
                {
                    cellColor = UtilityWPF.AlphaBlend(red, white, percent);
                }
                else
                {
                    cellColor = UtilityWPF.AlphaBlend(green, white, percent);
                }

                nodeCtrls[cntr].Item2.Fill = new SolidColorBrush(cellColor);
            }

            _wasEllipseTransferred = true;
        }

        private static Tuple<int, int, double>[] AdjustDesiredDistances(VoronoiStats_Node[] nodes, Tuple<int, int, double>[] currentDistances)
        {
            const double MAXDIFF = .25;
            const double PERCENTATMAX = .15;

            // See how much to adjust each link
            double[] percentAdjustements = nodes.
                Select(o =>
                {
                    double diff = o.Diff;
                    if (diff < -MAXDIFF) diff = -MAXDIFF;
                    if (diff > MAXDIFF) diff = MAXDIFF;

                    double percent = UtilityCore.GetScaledValue(0, PERCENTATMAX, 0, MAXDIFF, Math.Abs(diff));

                    if (diff < 0)
                    {
                        return -percent;
                    }
                    else
                    {
                        return percent;
                    }
                }).
                ToArray();

            double sumBefore = currentDistances.Sum(o => o.Item3);

            // Adjust the lengths of the links
            var retVal = currentDistances.
                Select(o =>
                {
                    //double percent = Math1D.Avg(percentAdjustements[o.Item1], percentAdjustements[o.Item2]);
                    double percent = percentAdjustements[o.Item1] + percentAdjustements[o.Item2];
                    percent += 1d;
                    return Tuple.Create(o.Item1, o.Item2, o.Item3 * percent);
                }).
                ToArray();

            double sumAfter = retVal.Sum(o => o.Item3);

            // Preserve the link lengths
            double mult = sumBefore / sumAfter;
            retVal = retVal.
                Select(o => Tuple.Create(o.Item1, o.Item2, o.Item3 * mult)).
                ToArray();

            return retVal;
        }

        private static VoronoiStats GetVoronoiStats(SOMNode[] nodes, ImageInput[][] imagesByNode)
        {
            if (nodes.Length <= 3)
            {
                return null;
            }

            // Run a voronoi against the nodes, and get sizes/neighbors
            SOMNodeVoronoiSet voronoiResults = GetVoronoiNeighbors(nodes, imagesByNode);

            // Get image sizes
            double totalImages = imagesByNode.
                Sum(o => o.Length).
                ToDouble();

            double[] imagePercentages = imagesByNode.
                Select(o => o.Length.ToDouble() / totalImages).
                ToArray();

            // Init desired distances to be the current distances between nodes
            var desiredDistances = voronoiResults.NeighborLinks.
                Select(o => Tuple.Create(o.Item1, o.Item2, MathND.GetDistance(nodes[o.Item1].Position, nodes[o.Item2].Position))).
                ToArray();


            // Debug
            var debugPercents = Enumerable.Range(0, nodes.Length).
                Select(o => new VoronoiStats_Node { Area = voronoiResults.Cells[o].SizePercentOfTotal, Image = imagePercentages[o] }).
                ToArray();


            // Exit Function
            return new VoronoiStats()
            {
                Set = voronoiResults,
                NodeStats = debugPercents,
                CurrentDistances = desiredDistances,
            };
        }

        private static Ellipse FindEllipse(SOMNode node, Canvas canvas)
        {
            foreach (object graphic in canvas.Children)
            {
                if (!(graphic is Ellipse))
                {
                    continue;
                }

                Ellipse ellipse = (Ellipse)graphic;

                SOMNode graphicNode = ellipse.Tag as SOMNode;
                if (graphicNode != null && graphicNode.Token == node.Token)
                {
                    return ellipse;
                }
            }

            return null;
        }
        private Polygon FindPoly(SOMNode node, Canvas canvas)
        {
            foreach (object graphic in canvas.Children)
            {
                if (!(graphic is Polygon))
                {
                    continue;
                }

                Polygon polygon = (Polygon)graphic;

                var graphicNode = polygon.Tag as Tuple<SOMNode, ImageInput[]>;
                if (graphicNode != null && graphicNode.Item1 != null && graphicNode.Item1.Token == node.Token)
                {
                    return polygon;
                }
            }

            throw new ApplicationException("didn't find polygon");
        }

        #endregion
        #region Private Methods - simple - tooltip

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

            var nodeImages = images.Select(o => o.Value_Normalized);
            var allImages = _imagesByNode.SelectMany(o => o).Select(o => o.Value_Normalized);

            double nodeSpread = GetTotalSpread(nodeImages);
            double totalSpread = GetTotalSpread(allImages);

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

            double[] nodeCenter = MathND.GetCenter(nodeImages);

            #region node hash

            if (showNodeHash)
            {
                ImageInput nodeImage = new ImageInput(null, node.Weights, node.Weights);

                double nodeDist = MathND.GetDistance(nodeImage.Value_Normalized, nodeCenter);
                double nodeDistPercent = nodeDist / Math.Max(nodeSpread, nodeDist);     // if there's only one node, then spread will be zero

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
                    imageDistPercent = MathND.GetDistance(image.Value_Normalized, nodeCenter) / nodeSpread;
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

                double widthHeight = Math.Sqrt(image.Value_Orig.Length);
                if (widthHeight.ToInt_Floor() == widthHeight.ToInt_Ceiling())
                {
                    width = height = widthHeight.ToInt_Floor();
                }
                else
                {
                    width = image.Value_Orig.Length;
                    height = 1;
                }

                double? maxValue = image.Value_Orig.Max() > 1d ? (double?)null : 1d;

                Convolution2D conv = new Convolution2D(image.Value_Orig, width, height, false);
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

        /// <summary>
        /// This is similar logic as standard deviation, but this returns the max distance from the average
        /// NOTE: Spread is probably the wrong word, since this only returns the max distance (radius instead of diameter)
        /// </summary>
        private static double GetTotalSpread(IEnumerable<double[]> values)
        {
            double[] mean = MathND.GetCenter(values);

            double distancesSquared = values.
                Select(o =>
                {
                    double[] diff = MathND.Subtract(o, mean);
                    return MathND.GetLengthSquared(diff);
                }).
                OrderByDescending().
                First();

            return Math.Sqrt(distancesSquared);
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
        #region Private Methods - convolution

        private void DoConvolution(ImageInput[] inputs)
        {
            SOMRules rules = GetSOMRules();

            SOMNodesInputs result = DoSOM2D(inputs, rules);

            ShowResults_Blobs(panelDisplay, result.Nodes_all, inputs, _getNodeColor_Random);
        }
        private void DoConvolution_Multiple1(ImageInput[] inputs, int numRuns)
        {
            //TODO: Make an overload that tries with slightly randomized rules.  Shoot for a breakdown of X to Y categories.  If the number of categories is too high or low, adjust

            SOMRules rules = GetSOMRules();

            // Get some random self organizing maps
            var runs = Enumerable.Range(0, numRuns).
                AsParallel().
                Select(o => DoSOM2D(inputs, rules)).
                ToArray();

            // Get non single blobs
            var nonSingleRuns = runs.
                Where(o => o.Nodes_filled.Length > 1).
                ToArray();

            if (nonSingleRuns.Length == 0)
            {
                // Only single blobs, just draw one
                ShowResults_Blobs(panelDisplay, runs[0].Nodes_filled, inputs, _getNodeColor_Random);
                return;
            }
            else if (nonSingleRuns.Length == 1)
            {
                // Only one non single blob.  Draw it
                ShowResults_Blobs(panelDisplay, nonSingleRuns[0].Nodes_filled, inputs, _getNodeColor_Random);
                return;
            }

            // See which nodes tend to be grouped together, convert into a single set of nodes
            Tuple<SOMNode, double>[] mergedNodes = MergeNodeRuns(nonSingleRuns, inputs);

            //// Delegates to get node color
            //var getRandColor = _getNodeColor_Random;        // storing local so it could be called from another thread

            //Func<SOMNode, Color> getNodeColor = new Func<SOMNode, Color>(o =>
            //    {
            //        double percent = mergedNodes.First(p => o.Token == p.Item1.Token).Item2;
            //        byte alpha = Convert.ToByte(UtilityCore.GetScaledValue_Capped(64, 255, 0, 1, percent));

            //        Color color = getRandColor(o);

            //        return Color.FromArgb(alpha, color.R, color.G, color.B);
            //    });

            // Show results
            ShowResults_Blobs(panelDisplay, mergedNodes.Select(o => o.Item1).ToArray(), inputs, _getNodeColor_Random);
        }
        private void DoConvolution2(ImageInput[] inputs, double maxSpreadPercent, int minNodeItemsForSplit)
        {
            SOMRules rules = GetSOMRules();

            double totalSpread = GetTotalSpread(inputs.Select(o => o.Value_Normalized));

            SOMNodesInputs result = DoSOM2D(inputs, rules);

            while (true)
            {
                // Split up nodes that have too much variation (image's distance from average)
                var reduced = Enumerable.Range(0, result.Nodes_filled.Length).
                    AsParallel().
                    Select(cntr => DoConvolution2_Split(cntr, result, minNodeItemsForSplit, maxSpreadPercent, totalSpread, rules)).
                    ToArray();

                if (reduced.All(o => !o.Item1))
                {
                    // No changes were needed this pass
                    break;
                }

                SOMNode[] reducedNodes = reduced.
                    SelectMany(o => o.Item2).
                    ToArray();

                // Rebuild result
                ImageInput[][] imagesByNode = GetImagesByNode(reducedNodes, inputs);
                result = new SOMNodesInputs(reducedNodes, imagesByNode);
            }

            ShowResults_Blobs(panelDisplay, result.Nodes_filled, inputs, _getNodeColor_Random);
        }

        private static Tuple<bool, SOMNode[]> DoConvolution2_Split(int index, SOMNodesInputs result, int minNodeItemsForSplit, double maxSpreadPercent, double totalSpread, SOMRules rules)
        {
            // Don't split if there aren't enough inputs in the parent
            if (result.ImagesByNode_filled[index].Length < minNodeItemsForSplit)
            {
                return Tuple.Create(false, new[] { result.Nodes_filled[index] });
            }

            // See how this node's distances from the average compare with the total
            double nodeSpread = GetTotalSpread(result.ImagesByNode_filled[index].Select(o => o.Value_Normalized));
            double percentSpread = nodeSpread / totalSpread;

            if (percentSpread < maxSpreadPercent)
            {
                return Tuple.Create(false, new[] { result.Nodes_filled[index] });
            }

            // Split up this node
            SOMNodesInputs subResult = DoSOM2D_Continuation(result.Nodes_filled, index, result.ImagesByNode_filled[index], rules);

            return Tuple.Create(true, subResult.Nodes_filled);
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

        private static double[] GetValuesFromImage_EdgeMaxpool(FeatureRecognizer_Image image, int size, double scaleValue)
        {
            const int INITIALSIZE = 80;

            BitmapSource bitmap = new BitmapImage(new Uri(image.Filename));
            bitmap = UtilityWPF.ResizeImage(bitmap, INITIALSIZE, true);

            Convolution2D retVal = UtilityWPF.ConvertToConvolution(bitmap, scaleValue);
            if (retVal.Width != retVal.Height)
            {
                retVal = Convolutions.ExtendBorders(retVal, INITIALSIZE, INITIALSIZE);        //NOTE: width or height is already the desired size, this will just enlarge the other to make it square
            }

            retVal = Convolutions.Convolute(retVal, Convolutions.GetEdgeSet_Sobel());
            retVal = Convolutions.MaxPool(retVal, size, size);
            retVal = Convolutions.Abs(retVal);

            return retVal.Values;
        }

        /// <summary>
        /// This creates random nodes, trains a self organizing map, returns the nodes that have inputs, and corresponding inputs
        /// </summary>
        private static SOMNodesInputs DoSOM2D(ImageInput[] inputs, SOMRules rules)
        {
            NodePositions positions = GetNodePositions2D(rules.NumNodes, SimpleNodeLayout.Blobs);
            double[][] weights = GetRandomWeights(rules.NumNodes, inputs);

            SOMNode[] nodes = Enumerable.Range(0, rules.NumNodes).
                Select(o => new SOMNode() { Position = positions.Positions[o], Weights = weights[o] }).
                ToArray();

            nodes = TrainSOM(nodes, inputs, rules, 0, rules.NumIterations - 1, positions.MapRadius);

            ImageInput[][] imagesByNode = GetImagesByNode(nodes, inputs);

            return new SOMNodesInputs(nodes, imagesByNode);
        }

        /// <summary>
        /// This does a new SOM for one node (sort of like recursing on a node)
        /// </summary>
        /// <param name="parents">Need all existing parent nodes, because the random weights chosen will be closer to the parent node than any others</param>
        /// <param name="nodeIndex">The node to break apart</param>
        /// <param name="nodesInputs">Only the inputs for the parent node pointed to by nodeIndex</param>
        private static SOMNodesInputs DoSOM2D_Continuation(SOMNode[] parents, int nodeIndex, ImageInput[] nodesInputs, SOMRules rules)
        {
            NodePositions positions = GetNodePositions2D(rules.NumNodes, SimpleNodeLayout.Blobs);

            // Get random node weights.  Don't let any of those weights be closer to other nodes than this node
            double[][] weights = GetRandomWeights_InsideCell(rules.NumNodes, nodesInputs, parents, nodeIndex);

            SOMNode[] nodes = Enumerable.Range(0, rules.NumNodes).
                Select(o => new SOMNode() { Position = positions.Positions[o], Weights = weights[o] }).
                ToArray();

            nodes = TrainSOM(nodes, nodesInputs, rules, 0, rules.NumIterations - 1, positions.MapRadius);

            ImageInput[][] imagesByNode = GetImagesByNode(nodes, nodesInputs);

            return new SOMNodesInputs(nodes, imagesByNode);
        }

        private static Tuple<SOMNode, double>[] MergeNodeRuns_InputLinks(SOMNodesInputs[] runs, ImageInput[] inputs)
        {
            double mult = 10d / inputs.Length;

            List<LinkWeight> links = new List<LinkWeight>();

            foreach (SOMNodesInputs run in runs)
            {
                foreach (ImageInput[] imageSet in run.ImagesByNode_filled)
                {
                    // Give more weight to smaller sets (1/X)
                    double weight = 1d / (imageSet.Length * mult);

                    if (imageSet.Length == 1)
                    {
                        LinkWeight.AddLink(links, imageSet[0].Image.UniqueID, "", weight);
                    }
                    else
                    {
                        foreach (var pair in UtilityCore.GetPairs(imageSet))
                        {
                            LinkWeight.AddLink(links, pair.Item1.Image.UniqueID, pair.Item2.Image.UniqueID, weight);
                        }
                    }
                }
            }

            links = links.OrderByDescending(o => o.Weight).ToList();


            return runs[0].Nodes_filled.Select(o => Tuple.Create(o, 1d)).ToArray();
        }
        private static Tuple<SOMNode, double>[] MergeNodeRuns_NearestNode(SOMNodesInputs[] runs, ImageInput[] inputs)
        {
            SOMNode[] allNodes = runs.SelectMany(o => o.Nodes_filled).ToArray();

            List<SOMNode> matchNodes = new List<SOMNode>();

            List<int> debug_matchIndices = new List<int>();

            // Shoot through each input, looking for the closest node, and keep that node
            foreach (ImageInput input in inputs)
            {
                var closest = GetClosest(allNodes, input);

                matchNodes.Add(closest.Item1);

                debug_matchIndices.Add(closest.Item2);
            }

            debug_matchIndices = debug_matchIndices.Distinct().OrderBy().ToList();
            int[] debug_nonMatchIndices = Enumerable.Range(0, allNodes.Length).Where(o => !debug_matchIndices.Contains(o)).ToArray();

            return matchNodes.
                Distinct(o => o.Token).
                Select(o => Tuple.Create(o, 1d)).
                ToArray();
        }
        private static Tuple<SOMNode, double>[] MergeNodeRuns(SOMNodesInputs[] runs, ImageInput[] inputs)
        {
            SOMNode[] allNodes = runs.SelectMany(o => o.Nodes_filled).ToArray();

            #region keep closest nodes

            List<SOMNode> matchNodes = new List<SOMNode>();

            List<int> debug_matchIndices = new List<int>();

            // Shoot through each input, looking for the closest node, and keep that node
            foreach (ImageInput input in inputs)
            {
                var closest = GetClosest(allNodes, input);

                matchNodes.Add(closest.Item1);

                debug_matchIndices.Add(closest.Item2);
            }

            debug_matchIndices = debug_matchIndices.Distinct().OrderBy().ToList();
            int[] debug_nonMatchIndices = Enumerable.Range(0, allNodes.Length).Where(o => !debug_matchIndices.Contains(o)).ToArray();

            matchNodes = matchNodes.
                Distinct(o => o.Token).
                ToList();

            #endregion

            #region merge close nodes

            var aabb = MathND.GetAABB(inputs.Select(o => o.Value_Normalized));
            double diagLength = MathND.GetLength(MathND.Subtract(aabb.Item2, aabb.Item1));

            var distances = UtilityCore.GetPairs(matchNodes).
                Select(o => new
                {
                    Item1 = o.Item1,
                    Item2 = o.Item2,
                    Distance = MathND.GetDistance(o.Item1.Weights, o.Item2.Weights),
                }).
                OrderBy(o => o.Distance).
                ToArray();




            #endregion

            return matchNodes.
                //Distinct(o => o.Token).
                Select(o => Tuple.Create(o, 1d)).
                ToArray();
        }

        #region Class: LinkWeight

        private class LinkWeight
        {
            public LinkWeight(string image1, string image2)
            {
                this.Image1 = image1;
                this.Image2 = image2;
            }

            public readonly string Image1;
            public readonly string Image2;

            public double Weight { get; set; }

            public bool IsMatch(string image1, string image2)
            {
                return (this.Image1 == image1 && this.Image2 == image2) || (this.Image1 == image2 && this.Image2 == image1);
            }

            public static void AddLink(List<LinkWeight> links, string image1, string image2, double weight)
            {
                LinkWeight existing = links.FirstOrDefault(o => o.IsMatch(image1, image2));

                if (existing == null)
                {
                    links.Add(new LinkWeight(image1, image2) { Weight = weight });
                }
                else
                {
                    existing.Weight += weight;
                }
            }
        }

        #endregion

        #endregion
        #region Private Methods - show results

        private void ShowResults_Disk(Border border, SOMNode[] nodes, ImageInput[] images, Func<SOMNode, Color> getNodeColor, bool keepZeros)
        {
            ImageInput[][] imagesByNode = GetImagesByNode(nodes, images);

            SOMNode[] nodesUsed = nodes;
            if (!keepZeros)
            {
                RemoveZeroNodes(ref nodesUsed, ref imagesByNode);
                ArrangeNodes_LikesAttract(ref nodesUsed, ref imagesByNode);
                //nodesUsed = ArrangeNodes_EnforceSizes(nodesUsed, imagesByNode);
            }

            Point[] points = nodesUsed.
                Select(o => new Point(o.Position[0], o.Position[1])).
                ToArray();

            VoronoiResult2D voronoi = Math2D.GetVoronoi(points, true);
            voronoi = Math2D.CapVoronoiCircle(voronoi);

            Color[] colors = nodesUsed.
                Select(o => getNodeColor(o)).
                ToArray();

            Vector size = new Vector(border.ActualWidth - border.Padding.Left - border.Padding.Right, border.ActualHeight - border.Padding.Top - border.Padding.Bottom);
            Canvas canvas = DrawVoronoi(voronoi, colors, nodesUsed, imagesByNode, size.X.ToInt_Floor(), size.Y.ToInt_Floor());

            border.Child = canvas;

            // This is for the manual manipulate buttons
            _nodes = nodesUsed;
            _imagesByNode = imagesByNode;
            _wasEllipseTransferred = false;
        }

        private void ShowResults_Blobs(Border border, SOMNode[] nodes, ImageInput[] images, Func<SOMNode, Color> getNodeColor)
        {
            ImageInput[][] imagesByNode = GetImagesByNode(nodes, images);

            SOMNode[] nodesUsed = nodes;
            RemoveZeroNodes(ref nodesUsed, ref imagesByNode);
            ArrangeNodes_LikesAttract(ref nodesUsed, ref imagesByNode);
            //nodesUsed = ArrangeNodes_EnlargeCompressed(nodesUsed, imagesByNode);

            Point[] points = nodesUsed.
                Select(o => new Point(o.Position[0], o.Position[1])).
                ToArray();

            VoronoiResult2D voronoi = Math2D.GetVoronoi(points, true);
            voronoi = Math2D.CapVoronoiCircle(voronoi);

            Color[] colors = nodesUsed.
                Select(o => getNodeColor(o)).
                ToArray();

            Vector size = new Vector(border.ActualWidth - border.Padding.Left - border.Padding.Right, border.ActualHeight - border.Padding.Top - border.Padding.Bottom);
            Canvas canvas = DrawVoronoiBlobs(voronoi, colors, nodesUsed, imagesByNode, size.X.ToInt_Floor(), size.Y.ToInt_Floor());

            border.Child = canvas;

            // This is for the manual manipulate buttons
            _nodes = nodesUsed;
            _imagesByNode = imagesByNode;
            _wasEllipseTransferred = false;
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

        private static void AdjustNodeWeights(SOMNode node, double[] position, double percent)
        {
            #region validate
#if DEBUG
            if (node.Weights.Length != position.Length)
            {
                throw new ArgumentException(string.Format("Position vectors aren't the same length.  node={0}, arg={1}", node.Weights.Length, position.Length));
            }
#endif
            #endregion

            //W(t+1) = W(t) + (pos - W(t)) * %

            for (int cntr = 0; cntr < position.Length; cntr++)
            {
                node.Weights[cntr] += (position[cntr] - node.Weights[cntr]) * percent;
            }
        }

        private static Tuple<SOMNode, double>[] GetNeighbors(SOMNode[] nodes, SOMNode match, double maxDistance)
        {
            List<Tuple<SOMNode, double>> retVal = new List<Tuple<SOMNode, double>>();

            double maxDistanceSquared = maxDistance * maxDistance;

            for (int cntr = 0; cntr < nodes.Length; cntr++)
            {
                if (nodes[cntr].Token == match.Token)
                {
                    continue;
                }

                double distSquared = MathND.GetDistanceSquared(nodes[cntr].Weights, match.Weights);

                if (distSquared < maxDistanceSquared)
                {
                    retVal.Add(Tuple.Create(nodes[cntr], distSquared));     // no need for a square root.  The calling method needs it squared
                }
            }

            return retVal.ToArray();
        }

        private static SOMNode[] MoveNodes_BallOfSprings(SOMNode[] nodes, Tuple<int, int, double>[] desiredDistances, int numIterations)
        {
            double[][] positions = nodes.
                Select(o => o.Position).
                ToArray();

            positions = MathND.ApplyBallOfSprings(positions, desiredDistances, numIterations);

            // Rebuild nodes
            return Enumerable.Range(0, nodes.Length).
                Select(o => new SOMNode()
                {
                    Weights = nodes[o].Weights,
                    Position = positions[o]
                }).
                ToArray();
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

        private static SOMNode[] TrainSOM(SOMNode[] nodes, ImageInput[] inputs, SOMRules rules, int fromIteration, int toIteration, double mapRadius)
        {
            SOMNode[] retVal = nodes.
                Select(o => o.Clone()).
                ToArray();

            double timeConstant = rules.NumIterations / Math.Log(mapRadius);

            int iteration = fromIteration;
            int remainingIterations = toIteration - fromIteration + 1;

            while (remainingIterations > 0)
            {
                foreach (ImageInput input in UtilityCore.RandomOrder(inputs, Math.Min(remainingIterations, inputs.Length)))
                {
                    // Find closest node
                    SOMNode closest = GetClosest(retVal, input).Item1;

                    // Find other affected nodes (a node and distance squared)
                    double searchRadius = mapRadius * rules.InitialRadiusPercent * Math.Exp(-iteration / timeConstant);
                    Tuple<SOMNode, double>[] neigbors = GetNeighbors(retVal, closest, searchRadius);

                    double learningRate = rules.LearningRate * Math.Exp(-(double)iteration / (double)rules.NumIterations);

                    // Adjust the matched node (full learning rate)
                    AdjustNodeWeights(closest, input.Value_Normalized, learningRate);

                    double twiceSearchRadiusSquared = 2d * (searchRadius * searchRadius);

                    foreach (var node in neigbors)
                    {
                        double influence = Math.Exp(-node.Item2 / twiceSearchRadiusSquared);        // this will reduce the learning rate as distance increases (guassian dropoff)

                        // Adjust a neighbor
                        AdjustNodeWeights(node.Item1, input.Value_Normalized, learningRate * influence);
                    }

                    iteration++;
                }

                remainingIterations -= inputs.Length;
            }

            return retVal;
        }

        private static Tuple<SOMNode, int> GetClosest(SOMNode[] nodes, ImageInput input)
        {
            int closestIndex = -1;
            double closest = double.MaxValue;
            SOMNode retVal = null;

            for (int cntr = 0; cntr < nodes.Length; cntr++)
            {
                double distSquared = MathND.GetDistanceSquared(nodes[cntr].Weights, input.Value_Normalized);

                if (distSquared < closest)
                {
                    closestIndex = cntr;
                    closest = distSquared;
                    retVal = nodes[cntr];
                }
            }

            return Tuple.Create(retVal, closestIndex);
        }

        /// <summary>
        /// This overload lays them out in 2D (easy to display results)
        /// </summary>
        private static NodePositions GetNodePositions2D(int numNodes, SimpleNodeLayout layout)
        {
            Vector[] positions = null;
            double mapRadius = -1;
            double cellSize = -1d;      // only used when grid

            switch (layout)
            {
                case SimpleNodeLayout.Grid_UniformSize:
                    #region Grid

                    // Make sure the count is a square
                    int widthHeight = Math.Sqrt(numNodes).ToInt_Ceiling();
                    numNodes = widthHeight * widthHeight;

                    positions = new Vector[numNodes];

                    // Just use a cell size of 1.  The choice is arbitrary, since the algorithm will be based off of map size, and the render will scale to fit
                    cellSize = 1d;
                    double halfCellSize = cellSize / 2d;

                    mapRadius = ((widthHeight + 1) * cellSize) / 2d;

                    for (int y = 0; y < widthHeight; y++)
                    {
                        int yOffset = y * widthHeight;

                        for (int x = 0; x < widthHeight; x++)
                        {
                            positions[yOffset + x] = new Vector((x * cellSize) + halfCellSize, (y * cellSize) + halfCellSize);
                        }
                    }

                    #endregion
                    break;

                case SimpleNodeLayout.Disk_All:
                case SimpleNodeLayout.Disk_NonZero:
                case SimpleNodeLayout.Blobs:
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

        /// <summary>
        /// This gets the bounding box of all the input values, then creates random vectors within that box
        /// </summary>
        private static double[][] GetRandomWeights(int count, ImageInput[] inputs)
        {
            var aabb = MathND.GetAABB(inputs.Select(o => o.Value_Normalized));
            aabb = MathND.ResizeAABB(aabb, 1.1);     // allow the return vectors to be slightly outside the input box

            return Enumerable.Range(0, count).
                Select(o => MathND.GetRandomVector(aabb.Item1, aabb.Item2)).
                ToArray();
        }

        //TODO: This method is fragile.  Using a bounding box can cause infinite loops.  The best way would be a voronoi, and choose random points within the convex hull.
        /// <summary>
        /// This overload won't let any returned weights be closer to other nodes than this node
        /// </summary>
        private static double[][] GetRandomWeights_InsideCell(int count, ImageInput[] inputs, SOMNode[] nodes, int nodeIndex)
        {
            var inputAABB = MathND.GetAABB(inputs.Select(o => o.Value_Normalized));
            //var inputAABB = MathND.GetAABB(UtilityCore.Iterate<double[]>(inputs.Select(o => o.Value_Normalized), nodes[nodeIndex].Weights));
            //inputAABB = MathND.ResizeAABB(inputAABB, 2);        // this eliminates the infinite loop in this method, but pushes an infinite loop out (nodes never reduce)

            // This actually could happen.  Detecting an infinite loop instead
            //if (!MathND.IsInside(inputAABB, nodes[nodeIndex].Weights))
            //{
            //    throw new ArgumentException("The node sits outside the input's aabb");
            //}

            List<double[]> retVal = new List<double[]>();

            for (int cntr = 0; cntr < count; cntr++)
            {
                int infiniteLoopDetector = 0;

                while (true)
                {
                    double[] attempt = MathND.GetRandomVector(inputAABB.Item1, inputAABB.Item2);

                    var closest = nodes.
                        Select((o, i) => new { Index = i, DistSquared = MathND.GetDistanceSquared(attempt, o.Weights) }).
                        OrderBy(o => o.DistSquared).
                        First();

                    if (closest.Index == nodeIndex)
                    {
                        retVal.Add(attempt);
                        break;
                    }

                    // This also hits.  Need a larger AABB
                    infiniteLoopDetector++;
                    if (infiniteLoopDetector > 200000)
                    {
                        throw new ApplicationException("Infinite loop detected");
                    }
                }
            }

            return retVal.ToArray();
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
