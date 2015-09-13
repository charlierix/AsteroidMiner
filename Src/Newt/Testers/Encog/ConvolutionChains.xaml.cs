using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
using System.Windows.Shapes;
using Encog.Neural.Networks;
using Game.HelperClassesAI;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.Testers.Convolution;

namespace Game.Newt.Testers.Encog
{
    public partial class ConvolutionChains : Window
    {
        #region Class: InstructionsPrimitive

        private class InstructionsPrimitive
        {
            public int ImageSize { get; set; }
            public ConvolutionBase2D[] Candidates { get; set; }
            public ConvolutionBase2D GetCandidate()
            {
                return this.Candidates[StaticRandom.Next(this.Candidates.Length)];
            }
        }

        #endregion
        #region Class: InstructionsExtract

        private class InstructionsExtract
        {
            public int ImageSize { get; set; }

            public FeatureRecognizer_Image SelectedImage { get; set; }
            public FeatureRecognizer_Image[] AllImages { get; set; }

            public bool IsSquare { get; set; }
            public bool IsRectangle { get; set; }
            public bool GetIsSquare()
            {
                if (this.IsRectangle && this.IsSquare)
                {
                    return StaticRandom.NextBool();
                }
                else if (this.IsRectangle)
                {
                    return false;
                }
                else if (this.IsSquare)
                {
                    return true;
                }
                else
                {
                    throw new ApplicationException("At least one of Is rectangle/square must be true");
                }
            }

            public double SizePercentMin { get; set; }
            public double SizePercentMax { get; set; }

            public bool IsRaw { get; set; }
            public bool IsEdge { get; set; }
            public bool GetIsRaw()
            {
                if (this.IsRaw && this.IsEdge)
                {
                    return StaticRandom.NextBool();
                }
                else if (this.IsRaw)
                {
                    return true;
                }
                else if (this.IsEdge)
                {
                    return false;
                }
                else
                {
                    throw new ApplicationException("Add least one of Raw/Edge needs to be true");
                }
            }

            public bool IsFullBorder { get; set; }
            public bool IsSoftBorder { get; set; }
            public bool GetIsFullBorder()
            {
                if (this.IsFullBorder && this.IsSoftBorder)
                {
                    return StaticRandom.NextBool();
                }
                else if (this.IsFullBorder)
                {
                    return true;
                }
                else if (this.IsSoftBorder)
                {
                    return false;
                }
                else
                {
                    throw new ApplicationException("Add least one of the edge types needs to be true");
                }
            }

            public ConvolutionExtractType GetExtractType()
            {
                if (GetIsRaw())
                {
                    if (GetIsFullBorder())
                    {
                        return ConvolutionExtractType.RawUnit;
                    }
                    else
                    {
                        return ConvolutionExtractType.RawUnitSoftBorder;
                    }
                }
                else
                {
                    if (GetIsFullBorder())
                    {
                        return ConvolutionExtractType.Edge;
                    }
                    else
                    {
                        return ConvolutionExtractType.EdgeSoftBorder;
                    }
                }
            }

            public bool ShouldAddHoles { get; set; }
            public int? MinHoles { get; set; }
            public int? MaxHoles { get; set; }
            public int GetNumHoles()
            {
                if (!this.ShouldAddHoles)
                {
                    return 0;
                }

                return StaticRandom.Next(this.MinHoles.Value, this.MaxHoles.Value + 1);
            }
        }

        #endregion
        #region Class: InstructionsNN

        private class InstructionsNN
        {
            public int InputSizeMin { get; set; }
            public int InputSizeMax { get; set; }

            public bool AllowNoChain { get; set; }

            public bool AllowNegPos { get; set; }
            public bool AllowPosOnly { get; set; }
            public bool GetIsPosOnly()
            {
                if (this.AllowNegPos && this.AllowPosOnly)
                {
                    return StaticRandom.NextBool();
                }
                else if (this.AllowNegPos)
                {
                    return false;
                }
                else if (this.AllowPosOnly)
                {
                    return true;
                }
                else
                {
                    throw new ApplicationException("At least one of NegPos/PosOnly must be true");
                }
            }

            public int NumInputChainsMin { get; set; }
            public int NumInputChainsMax { get; set; }

            public int NumCategoriesMin { get; set; }
            public int NumCategoriesMax { get; set; }

            public int MaxImagesPerCategoryMin { get; set; }
            public int MaxImagesPerCategoryMax { get; set; }

            public FeatureRecognizer_Image[] Images { get; set; }
            public ConvChain_ConvChain[] Chains { get; set; }
        }

        #endregion

        #region Class: ImageConvsBySize

        private class ImageConvsBySize
        {
            public ImageConvsBySize(FeatureRecognizer_Image image, Tuple<int, Convolution2D>[] convsBySize)
            {
                this.Image = image;
                this.ConvsBySize = convsBySize;
            }

            public readonly FeatureRecognizer_Image Image;
            public readonly Tuple<int, Convolution2D>[] ConvsBySize;
        }

        #endregion
        #region Class: ChainOutput

        private class ChainOutput
        {
            public ChainOutput(FeatureRecognizer_Image image, Tuple<ConvChain_ConvChain, Convolution2D>[] chainOutputs)
            {
                this.Image = image;
                this.ChainOutputs = chainOutputs;

                this.FullOutput = chainOutputs.
                    SelectMany(o => o.Item2.Values).
                    ToArray();
            }

            public readonly FeatureRecognizer_Image Image;
            /// <summary>
            /// These are in the order that the neural net specifies
            /// </summary>
            public readonly Tuple<ConvChain_ConvChain, Convolution2D>[] ChainOutputs;
            public readonly double[] FullOutput;
        }

        #endregion

        #region Class: TestResults

        private class TestResults
        {
            public double[] SumErrors_All { get; set; }
            public double[] SumErrors_Trained { get; set; }
            public double[] SumErrors_Untrained { get; set; }

            public double TotalAvg_All { get; set; }
            public double TotalAvg_Trained { get; set; }
            public double TotalAvg_Untrained { get; set; }
        }

        #endregion

        #region Enum: LastShown

        private enum LastShown
        {
            Unknown,
            Chain,
            NN,
        }

        #endregion

        #region Declaration Section

        private const int THUMBSIZE_CONV = 24;
        private const int THUMBSIZE_RESULT = 70;
        private const int RESULTMARGIN = 12;
        private const int MAXPOOLDISPLAYSIZE = 100;

        private List<FeatureRecognizer_Image> _images = new List<FeatureRecognizer_Image>();

        private List<ConvChain_ConvChain> _chains = new List<ConvChain_ConvChain>();

        private List<ConvChain_NeuralNet> _neuralNets = new List<ConvChain_NeuralNet>();

        private List<Window> _childWindows = new List<Window>();

        private ScaleTransform _resultsScale = null;

        // This is used when creating a new neural net.  Testing shows that it's faster to do everything in parallel up front
        private Tuple<int, Tuple<FeatureRecognizer_Image, Convolution2D>[]>[] _cachedImages = null;

        private LastShown _lastShown = LastShown.Unknown;

        private readonly Tuple<ConvolutionPrimitiveType, ConvolutionBase2D[]>[] _primitives;

        private readonly ContextMenu _chainContextMenu;
        private readonly ContextMenu _convolutionContextMenu;
        private readonly ContextMenu _nnContextMenu;

        #endregion

        #region Constructor

        public ConvolutionChains()
        {
            const int TOOLTIPSIZE = 30;

            InitializeComponent();

            // Context Menu
            _chainContextMenu = (ContextMenu)this.Resources["chainContextMenu"];
            _convolutionContextMenu = (ContextMenu)this.Resources["convolutionContextMenu"];
            _nnContextMenu = (ContextMenu)this.Resources["nnContextMenu"];

            // NegPos coloring
            foreach (ConvolutionResultNegPosColoring coloring in Enum.GetValues(typeof(ConvolutionResultNegPosColoring)))
            {
                cboEdgeColors.Items.Add(coloring);
            }
            cboEdgeColors.SelectedIndex = 0;

            //Primitives
            _primitives = Convolutions.GetPrimitiveConvolutions();

            chkPrimitiveGaussian.ToolTip = BuildCheckboxTooltip("Blurs the image", Convolutions.GetThumbnail(Convolutions.GetGaussian(7), TOOLTIPSIZE, null));
            chkPrimitiveLaplacian.ToolTip = BuildCheckboxTooltip("Highlights interior edges\r\n(a bit weak)", Convolutions.GetThumbnail(Convolutions.GetEdge_Laplacian(), TOOLTIPSIZE, null));
            chkPrimitiveGaussianSubtract.ToolTip = BuildCheckboxTooltip("All around edge detect\r\n(a bit weak)", Convolutions.GetThumbnail(Convolutions.GetEdgeSet_GaussianSubtract(), TOOLTIPSIZE, null));
            chkPrimitiveSingleSobel.ToolTip = BuildCheckboxTooltip("Edge detect in a single direction", Convolutions.GetThumbnail(Convolutions.GetEdge_Sobel(), TOOLTIPSIZE, null));
            chkPrimitiveMaxAbsSobel.ToolTip = BuildCheckboxTooltip("Per pixel |all single sobels|\r\n(a good strong edge detect)", Convolutions.GetThumbnail(Convolutions.GetEdgeSet_Sobel(), TOOLTIPSIZE, null));
            chkPrimitiveGaussianEdge.ToolTip = BuildCheckboxTooltip("Blur then MaxSobel\r\n(good edge detect for noisy images)", Convolutions.GetThumbnail(Convolutions.GetEdgeSet_GuassianThenEdge(), TOOLTIPSIZE, null));
        }

        #endregion

        #region Event Listeners

        private void Window_Closed(object sender, EventArgs e)
        {
            try
            {
                foreach (Window child in _childWindows.ToArray())        // taking the array so that the list can be removed from while in the for loop
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

                var dialog = new Microsoft.Win32.OpenFileDialog();
                dialog.Multiselect = true;
                dialog.Title = "Please select an image";
                bool? result = dialog.ShowDialog();
                if (result == null || !result.Value)
                {
                    return;
                }

                foreach (string dialogFilename in dialog.FileNames)
                {
                    AddImage(dialogFilename, tag);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnAddFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new System.Windows.Forms.FolderBrowserDialog();
                dialog.Description = "Please select root folder";
                if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                foreach (string categoryFolder in Directory.GetDirectories(dialog.SelectedPath))
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

        private void CreateChain_Primitive_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Parse the gui
                InstructionsPrimitive instructions = GetInstructionsPrimitive();
                if (instructions == null)
                {
                    return;
                }

                // Create it
                ConvChain_ConvChain conv = AddPrimitiveToChain(instructions);
                if (conv == null)
                {
                    MessageBox.Show("Unable to create (image size probably too small)", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                AddChain(conv);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void GrowChain_Primitive_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Pick a source chain
                if (_chains.Count == 0)
                {
                    MessageBox.Show("Please create an initial chain first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ConvChain_ConvChain sourceChain = _chains[StaticRandom.Next(_chains.Count)];

                // Parse the gui
                InstructionsPrimitive instructions = GetInstructionsPrimitive();
                if (instructions == null)
                {
                    return;
                }

                // Create it
                ConvChain_ConvChain conv = AddPrimitiveToChain(instructions, sourceChain);
                if (conv == null)
                {
                    MessageBox.Show("Unable to create (image size probably too small)", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                AddChain(conv);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void CreateChain_Extract_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Parse the gui
                InstructionsExtract instructions = GetInstructionsExtract();
                if (instructions == null)
                {
                    return;
                }

                // Create it
                ConvChain_ConvChain conv = AddExtractToChain(instructions);
                if (conv == null)
                {
                    MessageBox.Show("Unable to create (image probably blank)", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                AddChain(conv);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void GrowChain_Extract_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Pick a source chain
                if (_chains.Count == 0)
                {
                    MessageBox.Show("Please create an initial chain first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ConvChain_ConvChain sourceChain = _chains[StaticRandom.Next(_chains.Count)];

                // Parse the gui
                InstructionsExtract instructions = GetInstructionsExtract();
                if (instructions == null)
                {
                    return;
                }

                // Create it
                ConvChain_ConvChain conv = AddExtractToChain(instructions, sourceChain);
                if (conv == null)
                {
                    MessageBox.Show("Unable to create (image probably blank)", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                AddChain(conv);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void TenRandomChains_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Parse the gui
                InstructionsPrimitive instrPrimitive = GetInstructionsPrimitive();
                if (instrPrimitive == null)
                {
                    return;
                }

                InstructionsExtract instrExtract = GetInstructionsExtract();
                if (instrExtract == null)
                {
                    return;
                }

                // Build chains
                var chains = Enumerable.Range(0, 10).
                    AsParallel().
                    Select(o => CreateRandomChain(instrPrimitive, instrExtract)).
                    Where(o => o != null).
                    ToArray();

                // Store them
                foreach (var chain in chains)
                {
                    AddChain(chain);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChainAddPrimitive_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get the chain they clicked on
                Border senderCast = _chainContextMenu.PlacementTarget as Border;
                if (senderCast == null)
                {
                    return;
                }

                ConvChain_ConvChain sourceChain = senderCast.Tag as ConvChain_ConvChain;
                if (sourceChain == null)
                {
                    return;
                }

                // Parse the gui
                InstructionsPrimitive instructions = GetInstructionsPrimitive();
                if (instructions == null)
                {
                    return;
                }

                // Create it
                ConvChain_ConvChain conv = AddPrimitiveToChain(instructions, sourceChain);
                if (conv == null)
                {
                    MessageBox.Show("Unable to create (image size probably too small)", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                AddChain(conv);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ChainAddExtract_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get the chain they clicked on
                Border senderCast = _chainContextMenu.PlacementTarget as Border;
                if (senderCast == null)
                {
                    return;
                }

                ConvChain_ConvChain sourceChain = senderCast.Tag as ConvChain_ConvChain;
                if (sourceChain == null)
                {
                    return;
                }

                // Parse the gui
                InstructionsExtract instructions = GetInstructionsExtract();
                if (instructions == null)
                {
                    return;
                }

                // Create it
                ConvChain_ConvChain conv = AddExtractToChain(instructions, sourceChain);
                if (conv == null)
                {
                    MessageBox.Show("Unable to create (image probably blank)", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                AddChain(conv);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ChainDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Border senderCast = _chainContextMenu.PlacementTarget as Border;
                if (senderCast == null)
                {
                    return;
                }

                ConvChain_ConvChain chain = senderCast.Tag as ConvChain_ConvChain;
                if (chain == null)
                {
                    return;
                }

                DeleteChain(chain);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateNN_Click_ORIG(object sender, RoutedEventArgs e)
        {
            try
            {
                InstructionsNN instr = GetInstructionsNN();
                if (instr == null)
                {
                    return;
                }

                ConvChain_NeuralNet item = CreateRandomNN(instr);

                TextBlock status = AddNN(item);

                // Finish in another thread
                int maxImagesPerCategory = StaticRandom.Next(instr.MaxImagesPerCategoryMin, instr.MaxImagesPerCategoryMax);
                TrainAndTestNN(item, status, instr.Images, maxImagesPerCategory);        // sending an array of images so that the list could be changed, and the training thread won't be affected
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void CreateNN_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                InstructionsNN instr = GetInstructionsNN();
                if (instr == null)
                {
                    return;
                }

                ConvChain_NeuralNet item = CreateRandomNN(instr);

                TextBlock status = AddNN(item);

                // Image cache
                EnsureImageCacheIsCurrent();

                // Finish in another thread
                int maxImagesPerCategory = StaticRandom.Next(instr.MaxImagesPerCategoryMin, instr.MaxImagesPerCategoryMax);
                TrainAndTestNN2(item, status, instr.Images, maxImagesPerCategory, _cachedImages);        // sending an array of images so that the list could be changed, and the training thread won't be affected
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NNDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Border senderCast = _nnContextMenu.PlacementTarget as Border;
                if (senderCast == null)
                {
                    return;
                }

                ConvChain_NeuralNet nn = senderCast.Tag as ConvChain_NeuralNet;
                if (nn == null)
                {
                    return;
                }

                DeleteNN(nn);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ConvolutionView_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FrameworkElement senderCast = _convolutionContextMenu.PlacementTarget as FrameworkElement;
                if (senderCast == null)
                {
                    return;
                }

                ConvolutionBase2D conv = senderCast.Tag as ConvolutionBase2D;
                if (conv == null)
                {
                    return;
                }

                ViewConvolution(conv);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void treeImages_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            try
            {
                ShowResult_Latest();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void lstConvChains_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                ShowResult_Chain();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void lstNNs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                ShowResult_NN();
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
                ShowResult_Latest();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void chkIsPosWhiteBlack_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowResult_Latest();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Redraw_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowResult_Latest();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UnselectImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                treeImages.ClearSelection();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void UnselectChain_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                lstConvChains.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void UnselectNNs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                lstNNs.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteImages_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _images.Clear();
                treeImages.Items.Clear();
                cboImageLabel.Items.Clear();
                cboImageLabel.Text = "";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void DeleteChains_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //NOTE: I was going to delete dependent neural nets, but they store an instance of their chains.  If neural nets get serialized, there
                //should just be a check to serialize each dependent chain if it wasn't already

                _chains.Clear();
                lstConvChains.Items.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void DeleteNNs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var nn in _neuralNets)
                {
                    nn.Cancel.Cancel();
                }

                _neuralNets.Clear();
                lstNNs.Items.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void trkZoom_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (_resultsScale == null)
                {
                    return;
                }

                _resultsScale.ScaleX = trkZoom.Value;
                _resultsScale.ScaleY = trkZoom.Value;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        // Parse the gui, and return instructions
        private InstructionsPrimitive GetInstructionsPrimitive()
        {
            // Image Size
            int imageSize;
            if (!int.TryParse(txtImageSizePrimitives.Text, out imageSize))
            {
                MessageBox.Show("Couldn't parse image size as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            // Figure out which primitive convs to choose from
            List<ConvolutionBase2D> prefilterCandidates = new List<ConvolutionBase2D>();

            if (chkPrimitiveGaussian.IsChecked.Value)
            {
                prefilterCandidates.AddRange(_primitives.Where(o => o.Item1 == ConvolutionPrimitiveType.Gaussian).SelectMany(o => o.Item2));
            }

            if (chkPrimitiveLaplacian.IsChecked.Value)
            {
                prefilterCandidates.AddRange(_primitives.Where(o => o.Item1 == ConvolutionPrimitiveType.Laplacian).SelectMany(o => o.Item2));
            }

            if (chkPrimitiveGaussianSubtract.IsChecked.Value)
            {
                prefilterCandidates.AddRange(_primitives.Where(o => o.Item1 == ConvolutionPrimitiveType.Gaussian_Subtract).SelectMany(o => o.Item2));
            }

            if (chkPrimitiveSingleSobel.IsChecked.Value)
            {
                prefilterCandidates.AddRange(_primitives.Where(o => o.Item1 == ConvolutionPrimitiveType.Individual_Sobel).SelectMany(o => o.Item2));
            }

            if (chkPrimitiveMaxAbsSobel.IsChecked.Value)
            {
                prefilterCandidates.AddRange(_primitives.Where(o => o.Item1 == ConvolutionPrimitiveType.MaxAbs_Sobel).SelectMany(o => o.Item2));
            }

            if (chkPrimitiveGaussianEdge.IsChecked.Value)
            {
                prefilterCandidates.AddRange(_primitives.Where(o => o.Item1 == ConvolutionPrimitiveType.Gaussian_Then_Edge).SelectMany(o => o.Item2));
            }

            if (prefilterCandidates.Count == 0)
            {
                MessageBox.Show("No filter type specified", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            return new InstructionsPrimitive()
            {
                ImageSize = imageSize,
                Candidates = prefilterCandidates.ToArray(),
            };
        }
        private InstructionsExtract GetInstructionsExtract()
        {
            InstructionsExtract retVal = new InstructionsExtract();

            #region image size

            int imageSize;
            if (!int.TryParse(txtImageSizeExtracts.Text, out imageSize))
            {
                MessageBox.Show("Couldn't parse image size as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            retVal.ImageSize = imageSize;

            #endregion
            #region images

            if (_images.Count == 0)
            {
                MessageBox.Show("Add an image first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            retVal.SelectedImage = GetSelected_Image();
            retVal.AllImages = _images.ToArray();

            #endregion
            #region rectangle / square

            retVal.IsRectangle = chkExtractIsRectangle.IsChecked.Value;
            retVal.IsSquare = chkExtractIsSquare.IsChecked.Value;

            if (!retVal.IsRectangle && !retVal.IsSquare)
            {
                MessageBox.Show("At least one of Is rectangle/square must be checked", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            #endregion
            #region  size percent

            retVal.SizePercentMin = trkExtractSizePercentMin.Value;
            retVal.SizePercentMax = trkExtractSizePercentMax.Value;

            #endregion
            #region  raw / edge detect

            retVal.IsRaw = chkExtractRaw.IsChecked.Value;
            retVal.IsEdge = chkExtractEdge.IsChecked.Value;

            if (!chkExtractRaw.IsChecked.Value && !chkExtractEdge.IsChecked.Value)
            {
                MessageBox.Show("Add least one of Raw/Edge needs to be checked", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            #endregion
            #region  full / soft border

            retVal.IsFullBorder = chkExtractFullBorder.IsChecked.Value;
            retVal.IsSoftBorder = chkExtractSoftBorder.IsChecked.Value;

            if (!chkExtractFullBorder.IsChecked.Value && !chkExtractSoftBorder.IsChecked.Value)
            {
                MessageBox.Show("Add least one of the edge types needs to be checked", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            #endregion
            #region  num holes

            retVal.ShouldAddHoles = chkExtractHoles.IsChecked.Value;

            if (chkExtractHoles.IsChecked.Value)
            {
                int minHoles, maxHoles;
                if (!int.TryParse(txtMinHoles.Text, out minHoles) || !int.TryParse(txtMaxHoles.Text, out maxHoles))
                {
                    MessageBox.Show("Couldn't pass min/max holes as integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return null;
                }

                if (minHoles > maxHoles)
                {
                    MessageBox.Show("Min holes must be less than max holes", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return null;
                }

                retVal.MinHoles = minHoles;
                retVal.MaxHoles = maxHoles;
            }

            #endregion

            return retVal;
        }
        private InstructionsNN GetInstructionsNN()
        {
            InstructionsNN retVal = new InstructionsNN();

            #region input size

            int inputSizeMin;
            if (!int.TryParse(txtNNInputSizeMin.Text, out inputSizeMin))
            {
                MessageBox.Show("Couldn't parse min input size as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            int inputSizeMax;
            if (!int.TryParse(txtNNInputSizeMax.Text, out inputSizeMax))
            {
                MessageBox.Show("Couldn't parse max input size as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            if (inputSizeMin > inputSizeMax)
            {
                MessageBox.Show("min input size must be <= max", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            retVal.InputSizeMin = inputSizeMin;
            retVal.InputSizeMax = inputSizeMax;

            #endregion
            #region input chains

            int numInputChainsMin;
            if (!int.TryParse(txtNNInputChainsMin.Text, out numInputChainsMin))
            {
                MessageBox.Show("Couldn't parse min number of input convolution chains as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            int numInputChainsMax;
            if (!int.TryParse(txtNNInputChainsMax.Text, out numInputChainsMax))
            {
                MessageBox.Show("Couldn't parse max number of input convolution chains as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            if (numInputChainsMin > numInputChainsMax)
            {
                MessageBox.Show("min number of chains must be <= max", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            if (_chains.Count < numInputChainsMax - (chkNNNoChain.IsChecked.Value ? 1 : 0))
            {
                MessageBox.Show("There aren't enough convolution chains to draw from", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            retVal.NumInputChainsMin = numInputChainsMin;
            retVal.NumInputChainsMax = numInputChainsMax;

            retVal.AllowNoChain = chkNNNoChain.IsChecked.Value;

            #endregion
            #region checkboxes

            if (!chkNNNegPos.IsChecked.Value && !chkNNPositiveOnly.IsChecked.Value)
            {
                MessageBox.Show("At least one of NegPos/PosOnly must be true", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            retVal.AllowNegPos = chkNNNegPos.IsChecked.Value;
            retVal.AllowPosOnly = chkNNPositiveOnly.IsChecked.Value;

            #endregion
            #region output categories

            if (_images.Count == 0)
            {
                MessageBox.Show("There are no images loaded", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            int numCategoriesMin;
            if (!int.TryParse(txtNNCategoriesMin.Text, out numCategoriesMin))
            {
                MessageBox.Show("Couldn't parse min number of categories as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            int numCategoriesMax;
            if (!int.TryParse(txtNNCategoriesMax.Text, out numCategoriesMax))
            {
                MessageBox.Show("Couldn't parse max number of categories as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            if (numCategoriesMin > numCategoriesMax)
            {
                MessageBox.Show("min number of categories must be <= max", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            retVal.NumCategoriesMin = numCategoriesMin;
            retVal.NumCategoriesMax = numCategoriesMax;

            #endregion
            #region images per category

            int maxImagesPerCategoryMin;
            if (!int.TryParse(txtNNImagesPerCategoryMin.Text, out maxImagesPerCategoryMin))
            {
                MessageBox.Show("Couldn't parse min images per category as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            int maxImagesPerCategoryMax;
            if (!int.TryParse(txtNNImagesPerCategoryMax.Text, out maxImagesPerCategoryMax))
            {
                MessageBox.Show("Couldn't parse max images per category as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            if (maxImagesPerCategoryMin > maxImagesPerCategoryMax)
            {
                MessageBox.Show("min images per category must be <= max", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            retVal.MaxImagesPerCategoryMin = maxImagesPerCategoryMin;
            retVal.MaxImagesPerCategoryMax = maxImagesPerCategoryMax;

            #endregion

            // Store images and chains as arrays so that the network can be built in a static method (also so images and
            // chains could change without affecting these instructions)
            retVal.Images = _images.ToArray();
            retVal.Chains = _chains.ToArray();

            return retVal;
        }

        private static ConvChain_ConvChain CreateRandomChain(InstructionsPrimitive instrPrimitive, InstructionsExtract instrExtract)
        {
            const int MINLENGTH = 2;
            const int MAXLENGTH = 5;
            const double CHANCEEXTRACT = .6667;

            Random rand = StaticRandom.GetRandomForThread();

            // Create the initial chain
            ConvChain_ConvChain retVal = null;

            // Figure out how many links to add
            int length = rand.Next(MINLENGTH, MAXLENGTH + 1);

            for (int inner = 0; inner < length; inner++)
            {
                if (rand.NextDouble() < CHANCEEXTRACT)
                {
                    // Extract
                    retVal = AddExtractToChain(instrExtract, retVal);
                }
                else
                {
                    // Primitive
                    retVal = AddPrimitiveToChain(instrPrimitive, retVal);
                }

                if (retVal == null)
                {
                    return null;
                }
            }

            return retVal;
        }
        private static ConvChain_ConvChain AddPrimitiveToChain(InstructionsPrimitive instr, ConvChain_ConvChain sourceChain = null)
        {
            if (sourceChain == null)
            {
                sourceChain = new ConvChain_ConvChain() { ImageSize = instr.ImageSize };
            }

            ConvolutionBase2D primitive = instr.GetCandidate();

            VectorInt sourceSize = sourceChain.GetFinalSize();
            VectorInt primitiveReduction = primitive.GetReduction();
            if (sourceSize.X - primitiveReduction.X < 1 || sourceSize.Y - primitiveReduction.Y < 1)
            {
                // This will be too small
                return null;
            }

            return new ConvChain_ConvChain()
            {
                UniqueID = Guid.NewGuid().ToString(),
                ImageSize = sourceChain.ImageSize,
                Convolutions = UtilityCore.ArrayAdd(sourceChain.Convolutions, primitive),
            };
        }
        private static ConvChain_ConvChain AddExtractToChain(InstructionsExtract instr, ConvChain_ConvChain sourceChain = null)
        {
            Random rand = StaticRandom.GetRandomForThread();

            if (sourceChain == null)
            {
                sourceChain = new ConvChain_ConvChain() { ImageSize = instr.ImageSize };
            }

            #region get bitmap

            var image = instr.SelectedImage ?? instr.AllImages[rand.Next(instr.AllImages.Length)];

            Convolution2D imageConv = GetConvolution(image.Filename, sourceChain.ImageSize);

            #endregion
            #region apply source convolutions

            if (sourceChain.Convolutions != null)
            {
                foreach (ConvolutionBase2D link in sourceChain.Convolutions)
                {
                    imageConv = Convolutions.Convolute(imageConv, link);
                }
            }

            #endregion
            #region build extract

            Convolution2D extractConv = null;
            for (int cntr = 0; cntr < 10; cntr++)
            {
                double sizePercent = rand.NextDouble(instr.SizePercentMin, instr.SizePercentMax);

                RectInt rectangle = Convolutions.GetExtractRectangle(imageConv.Size, instr.GetIsSquare(), sizePercent);

                extractConv = imageConv.Extract(rectangle, instr.GetExtractType());

                // See if it's blank
                if (extractConv.Values.All(o => o.IsNearZero()))
                {
                    extractConv = null;

                    // See if the source image is blank
                    if (cntr == 0 && imageConv.Values.All(o => o.IsNearZero()))
                    {
                        break;      // source is blank, no point looping
                    }

                    // Try again to get a nonblank extract
                    continue;
                }

                break;
            }

            if (extractConv == null)
            {
                return null;
            }

            #endregion
            #region shoot it with holes

            int numHoles = instr.GetNumHoles();

            if (numHoles > 0)
            {
                Convolution2D holyConv = null;
                for (int cntr = 0; cntr < 10; cntr++)
                {
                    holyConv = Convolutions.RemoveSection(extractConv, numHoles);
                    if (holyConv.Values.All(o => o.IsNearZero()))
                    {
                        holyConv = null;
                    }
                    else
                    {
                        break;
                    }
                }

                if (holyConv == null)
                {
                    return null;
                }

                extractConv = holyConv;
            }

            #endregion

            // Create it
            return new ConvChain_ConvChain()
            {
                UniqueID = Guid.NewGuid().ToString(),
                ImageSize = sourceChain.ImageSize,
                Convolutions = UtilityCore.ArrayAdd(sourceChain.Convolutions, extractConv),
            };
        }

        private static ConvChain_NeuralNet CreateRandomNN(InstructionsNN instr)
        {
            //NOTE: This just creates a definition, it doesn't train it

            Random rand = StaticRandom.GetRandomForThread();

            // Input Size
            int inputSize = rand.Next(instr.InputSizeMin, instr.InputSizeMax + 1);

            // Input chains
            bool useNonChain = false;
            //if (instr.AllowNoChain && rand.Next(instr.Chains.Length + 1) == 0)
            if (instr.AllowNoChain && rand.Next(3) == 0)        // using the number of chains gives too low of a probability
            {
                useNonChain = true;
            }

            int minChains = instr.NumInputChainsMin;
            int maxChains = instr.NumInputChainsMax;
            if (useNonChain)
            {
                minChains--;
                maxChains--;
            }

            int numInputChains = rand.Next(minChains, maxChains + 1);
            ConvChain_ConvChain[] chains;
            if (numInputChains <= 0)
            {
                chains = new ConvChain_ConvChain[0];
            }
            else
            {
                chains = UtilityCore.RandomOrder(instr.Chains, numInputChains).ToArray();
            }

            // Output categories
            int numCategories = rand.Next(instr.NumCategoriesMin, instr.NumCategoriesMax + 1);

            string[] allCategories = instr.Images.
                ToLookup(o => o.Category).
                Select(o => o.Key).
                ToArray();

            string[] outputCategories = UtilityCore.RandomOrder(allCategories, numCategories).
                OrderBy(o => o).
                ToArray();

            return new ConvChain_NeuralNet()
            {
                UniqueID = Guid.NewGuid().ToString(),
                UseNonChain = useNonChain,
                ChainIDs = chains.Select(o => o.UniqueID).ToArray(),
                Chains = chains,
                InputSize = inputSize,
                IsPositiveOnly = instr.GetIsPosOnly(),
                OutputCategories = outputCategories,
                Cancel = new CancellationTokenSource(),
            };
        }

        private void AddImage(string filename, string category)
        {
            string uniqueID = Guid.NewGuid().ToString();

            // Build entry
            FeatureRecognizer_Image entry = new FeatureRecognizer_Image()
            {
                Category = category,
                UniqueID = uniqueID,
                Filename = filename,
                ImageControl = FeatureRecognizer.GetTreeviewImageCtrl(new BitmapImage(new Uri(filename))),
                //Bitmap = bitmap,
            };

            // Store it
            FeatureRecognizer.AddImage(entry, _images, treeImages, cboImageLabel);
        }
        private void AddChain(ConvChain_ConvChain chain)
        {
            StackPanel stack = new StackPanel();

            foreach (ConvolutionBase2D conv in chain.Convolutions)
            {
                Border thumbnail = Convolutions.GetThumbnail(conv, THUMBSIZE_CONV, null);
                thumbnail.HorizontalAlignment = HorizontalAlignment.Center;
                thumbnail.Margin = new Thickness(2);

                stack.Children.Add(thumbnail);
            }

            Border border = new Border()
            {
                Background = new SolidColorBrush(UtilityWPF.ColorFromHex("40FFFFFF")),
                BorderBrush = new SolidColorBrush(UtilityWPF.ColorFromHex("C0C0C0")),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(6),
                ContextMenu = _chainContextMenu,
                Tag = chain,
                Child = stack,
            };

            chain.Control = border;

            _chains.Add(chain);
            lstConvChains.Items.Add(chain.Control);
        }
        private TextBlock AddNN(ConvChain_NeuralNet nn)
        {
            const int FONTSIZE = 10;

            StackPanel stackpanel = new StackPanel();

            // Categories
            foreach (string category in nn.OutputCategories)
            {
                stackpanel.Children.Add(new TextBlock()
                {
                    Text = category,
                    FontSize = FONTSIZE,
                });
            }

            // InputSize x #Chains = TotalSize
            stackpanel.Children.Add(new TextBlock()
            {
                Text = string.Format("{0}x{1}", nn.InputSize, (nn.UseNonChain ? 1 : 0) + nn.Chains.Length),
                Margin = new Thickness(0, 4, 0, 0),
                FontSize = FONTSIZE,
            });

            // Status
            TextBlock status = new TextBlock()
            {
                Text = "prepping...",
                Foreground = Brushes.Silver,
                Margin = new Thickness(0, 4, 0, 0),
                FontSize = FONTSIZE,
            };
            stackpanel.Children.Add(status);

            Border border = new Border()
            {
                Background = new SolidColorBrush(UtilityWPF.ColorFromHex("60FFFFFF")),
                BorderBrush = new SolidColorBrush(UtilityWPF.ColorFromHex("C0C0C0")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(2),
                Margin = new Thickness(6),
                Padding = new Thickness(3),
                ContextMenu = _nnContextMenu,
                Tag = nn,
                Child = stackpanel,
            };

            nn.Control = border;

            _neuralNets.Add(nn);
            lstNNs.Items.Add(nn.Control);

            return status;
        }

        private void DeleteChain(ConvChain_ConvChain chain)
        {
            // Listbox
            bool removedFromListbox = false;
            for (int cntr = 0; cntr < lstConvChains.Items.Count; cntr++)
            {
                Border childCtrl = lstConvChains.Items[cntr] as Border;
                if (childCtrl == null)
                {
                    throw new ApplicationException("Expected chain control to be a border");
                }

                ConvChain_ConvChain childChain = childCtrl.Tag as ConvChain_ConvChain;
                if (childChain == null)
                {
                    throw new ApplicationException("Expected tag to be a FeatureRecognizer4_ConvChain");
                }

                if (childChain.UniqueID != chain.UniqueID)
                {
                    continue;
                }

                lstConvChains.Items.RemoveAt(cntr);
                removedFromListbox = true;
                break;
            }

            if (!removedFromListbox)
            {
                throw new ApplicationException("Expected to find chain in listbox");
            }

            // List
            var removeIndex = _chains.
                Select((o, i) => new { Chain = o, Index = i }).
                FirstOrDefault(o => o.Chain.UniqueID == chain.UniqueID);

            if (removeIndex == null)
            {
                throw new ApplicationException("Expected to find chain in _chains");
            }

            _chains.RemoveAt(removeIndex.Index);
        }
        private void DeleteNN(ConvChain_NeuralNet nn)
        {
            nn.Cancel.Cancel();

            // Listbox
            bool removedFromListbox = false;
            for (int cntr = 0; cntr < lstNNs.Items.Count; cntr++)
            {
                Border childCtrl = lstNNs.Items[cntr] as Border;
                if (childCtrl == null)
                {
                    throw new ApplicationException("Expected chain control to be a border");
                }

                ConvChain_NeuralNet childNN = childCtrl.Tag as ConvChain_NeuralNet;
                if (childNN == null)
                {
                    throw new ApplicationException("Expected tag to be a FeatureRecognizer4_ConvChain");
                }

                if (childNN.UniqueID != nn.UniqueID)
                {
                    continue;
                }

                lstNNs.Items.RemoveAt(cntr);
                removedFromListbox = true;
                break;
            }

            if (!removedFromListbox)
            {
                throw new ApplicationException("Expected to find neural net in listbox");
            }

            // List
            var removeIndex = _neuralNets.
                Select((o, i) => new { NN = o, Index = i }).
                FirstOrDefault(o => o.NN.UniqueID == nn.UniqueID);

            if (removeIndex == null)
            {
                throw new ApplicationException("Expected to find neural net in _neuralNets");
            }

            _neuralNets.RemoveAt(removeIndex.Index);
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
        private ConvChain_ConvChain GetSelected_ConvChain()
        {
            Border selected = lstConvChains.SelectedItem as Border;
            if (selected == null)
            {
                return null;
            }

            ConvChain_ConvChain retVal = selected.Tag as ConvChain_ConvChain;
            if (retVal == null)
            {
                throw new ApplicationException("Expected tag to contain the chain");
            }

            return retVal;
        }
        private ConvChain_NeuralNet GetSelected_NN()
        {
            Border selected = lstNNs.SelectedItem as Border;
            if (selected == null)
            {
                return null;
            }

            ConvChain_NeuralNet retVal = selected.Tag as ConvChain_NeuralNet;
            if (retVal == null)
            {
                throw new ApplicationException("Expected tag to contain the neural net");
            }

            return retVal;
        }

        private void ViewConvolution(ConvolutionBase2D conv)
        {
            if (conv is Convolution2D)
            {
                #region Single

                ImageFilterPainter editor = new ImageFilterPainter();

                editor.Closed += Child_Closed;

                _childWindows.Add(editor);

                editor.ViewKernel((Convolution2D)conv);

                editor.Show();

                #endregion
            }
            else if (conv is ConvolutionSet2D)
            {
                #region Set

                CompositeFilter composite = new CompositeFilter((ConvolutionSet2D)conv, false);     // false makes this a viewer

                composite.Closed += Child_Closed;

                _childWindows.Add(composite);

                composite.Show();

                #endregion
            }
            else
            {
                MessageBox.Show("Unknown type of kernel: " + conv.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private ConvolutionBase2D GetRandomPrimitiveConv()
        {
            // Figure out which primitive convs to choose from
            List<ConvolutionBase2D> prefilterCandidates = new List<ConvolutionBase2D>();

            if (chkPrimitiveGaussian.IsChecked.Value)
            {
                prefilterCandidates.AddRange(_primitives.Where(o => o.Item1 == ConvolutionPrimitiveType.Gaussian).SelectMany(o => o.Item2));
            }

            if (chkPrimitiveLaplacian.IsChecked.Value)
            {
                prefilterCandidates.AddRange(_primitives.Where(o => o.Item1 == ConvolutionPrimitiveType.Laplacian).SelectMany(o => o.Item2));
            }

            if (chkPrimitiveGaussianSubtract.IsChecked.Value)
            {
                prefilterCandidates.AddRange(_primitives.Where(o => o.Item1 == ConvolutionPrimitiveType.Gaussian_Subtract).SelectMany(o => o.Item2));
            }

            if (chkPrimitiveSingleSobel.IsChecked.Value)
            {
                prefilterCandidates.AddRange(_primitives.Where(o => o.Item1 == ConvolutionPrimitiveType.Individual_Sobel).SelectMany(o => o.Item2));
            }

            if (chkPrimitiveMaxAbsSobel.IsChecked.Value)
            {
                prefilterCandidates.AddRange(_primitives.Where(o => o.Item1 == ConvolutionPrimitiveType.MaxAbs_Sobel).SelectMany(o => o.Item2));
            }

            if (chkPrimitiveGaussianEdge.IsChecked.Value)
            {
                prefilterCandidates.AddRange(_primitives.Where(o => o.Item1 == ConvolutionPrimitiveType.Gaussian_Then_Edge).SelectMany(o => o.Item2));
            }

            // Pick one
            ConvolutionBase2D retVal = null;
            if (prefilterCandidates.Count > 0)
            {
                retVal = prefilterCandidates[StaticRandom.Next(prefilterCandidates.Count)];
            }

            return retVal;
        }

        // ***** no cache
        private static async void TrainAndTestNN(ConvChain_NeuralNet item, TextBlock status, FeatureRecognizer_Image[] images, int maxImagesPerCategory)
        {
            #region Group images by category

            // Get the images by category (only keep what this neural net will use)
            var groupedImages = images.
                Where(o => item.OutputCategories.Any(p => o.Category == p)).
                ToLookup(o => o.Category).
                Select(o => new
                {
                    Category = o.Key,
                    Images = o.Select(p => p).ToArray()
                }).
                ToArray();

            if (groupedImages.Length == 0)
            {
                status.Text = "no images found";
                status.Foreground = Brushes.Red;
                return;
            }

            #endregion

            #region Training images

            // Choose subset to train with (don't bother staying grouped by category, that's stored in the image anyway)
            var trainingImages = groupedImages.
                SelectMany(o => UtilityCore.RandomOrder(o.Images, maxImagesPerCategory).ToArray()).
                ToArray();

            // Remember which images are used for training
            item.TrainingImageIDs = trainingImages.
                Select(o => o.UniqueID).
                ToArray();

            // Run the images through the convolution chains
            var trainingConvsTask = Task.Run(() => GetChainConvolutions(trainingImages, item.UseNonChain, item.Chains, item.InputSize, item.IsPositiveOnly));

            #endregion
            #region Remaining images

            // Grab all the images that weren't used for training
            var remainingImages = groupedImages.
                SelectMany(o => o.Images).
                Where(o => !trainingImages.Any(p => p.UniqueID == o.UniqueID)).
                ToArray();

            // Convert to convolutions
            var remainingConvsTask = Task.Run(() => GetChainConvolutions(remainingImages, item.UseNonChain, item.Chains, item.InputSize, item.IsPositiveOnly));

            #endregion

            await trainingConvsTask;

            status.Text = "training...";
            status.Foreground = Brushes.DarkGray;

            if (item.Cancel.IsCancellationRequested)
            {
                return;
            }

            #region Train network

            double[][] outputEntries = UtilityEncog.GetOutputLayer(item.OutputCategories.Length);

            var trainedNetworkTask = Task.Run(() =>
                {
                    double[][] trainingInput = trainingConvsTask.Result.
                        Select(o => o.FullOutput).
                        ToArray();

                    double[][] trainingOutput = trainingConvsTask.Result.
                        Select(o =>
                            {
                                int outputIndex = Array.IndexOf<string>(item.OutputCategories, o.Image.Category);
                                return outputEntries[outputIndex];
                            }).
                        ToArray();

                    return UtilityEncog.GetTrainedNetwork(trainingInput, trainingOutput, maxSeconds_PerAttempt: 20, cancelToken: item.Cancel.Token);
                });

            #endregion

            EncogTrainingResponse network = await trainedNetworkTask;

            if (!network.IsSuccess)
            {
                status.Text = "FAIL";
                status.Foreground = Brushes.Red;
                return;
            }

            status.Text = "testing...";
            status.Foreground = Brushes.Gray;

            if (item.Cancel.IsCancellationRequested)
            {
                return;
            }

            #region Test the network

            var testResultTask = Task.Run(() =>
                {
                    ChainOutput[] allImageConvs = trainingConvsTask.Result.
                        Concat(remainingConvsTask.Result).
                        ToArray();

                    return TestNetwork(network.Network, allImageConvs, item.OutputCategories, item.TrainingImageIDs);
                });

            #endregion

            TestResults scores = await testResultTask;

            status.Text = string.Format("trained: {0}\r\nuntrained: {1}\r\nall: {2}", scores.TotalAvg_Trained.ToStringSignificantDigits(3), scores.TotalAvg_Untrained.ToStringSignificantDigits(3), scores.TotalAvg_All.ToStringSignificantDigits(3));
            status.Foreground = Brushes.OliveDrab;  //some form of red to green

            item.Network = network.Network;
        }

        private static ChainOutput[] GetChainConvolutions(FeatureRecognizer_Image[] images, bool useNonChain, ConvChain_ConvChain[] chains, int finalSize, bool isPositiveOnly)
        {
            if (images == null || images.Length == 0)
            {
                return new ChainOutput[0];
            }

            // Figure out what sizes the images should be
            List<int> allImageSizes = new List<int>();
            if (useNonChain)
            {
                allImageSizes.Add(finalSize);
            }

            allImageSizes.AddRange(chains.Select(o => o.ImageSize));

            int[] imageSizes = allImageSizes.
                Distinct().
                ToArray();

            // Get the starting images
            ImageConvsBySize[] initialConvs = GetImageConvolutions(images, imageSizes);

            // Apply convolution chains
            return GetFinalImageSizes(initialConvs, useNonChain, chains, finalSize, isPositiveOnly);
        }
        private static ImageConvsBySize[] GetImageConvolutions(FeatureRecognizer_Image[] images, int[] sizes)
        {
            // Read all the images, convert to convolutions for each size (multithreaded)
            var rawResults = images.
                AsParallel().
                Select(image => new
                {
                    Image = image,
                    BySize = sizes.
                    AsParallel().
                    Select(size => new
                    {
                        Size = size,
                        Convolution = GetConvolution(image.Filename, size),
                    }).ToArray(),
                }).
                ToArray();

            // Now that everything is done, commit to classes
            return rawResults.
                Select(o => new ImageConvsBySize(o.Image, o.BySize.Select(p => Tuple.Create(p.Size, p.Convolution)).ToArray())).
                ToArray();
        }
        private static ChainOutput[] GetFinalImageSizes(ImageConvsBySize[] imagesBySize, bool useNonChain, ConvChain_ConvChain[] chains, int finalSize, bool isPositiveOnly, object chainDependencies = null)
        {
            //TODO: Identify chains that are additions to prior ones and use their results as a starting point (use chainDependencies graph)

            List<ConvChain_ConvChain> actualChains = new List<ConvChain_ConvChain>();

            if (useNonChain)
            {
                actualChains.Add(null);
            }

            actualChains.AddRange(chains);

            //NOTE: I decided to use Task.Run instead of AsParallel to make sure each runs in its own task (applying the chain can be expensive)
            var chainResults = UtilityCore.Collate(actualChains, imagesBySize).
                Select(o =>
                {
                    Tuple<int, Convolution2D> match = null;

                    if (o.Item1 == null)
                    {
                        // Find the convolution for this image and chain to start with
                        match = o.Item2.ConvsBySize.First(p => p.Item1 == finalSize);

                        // Kick off this chain
                        return new
                        {
                            Chain = (ConvChain_ConvChain)null,
                            Image = o.Item2,
                            ConvResultTask = Task.Run(() => isPositiveOnly ? Convolutions.Abs(match.Item2) : match.Item2),
                        };
                    }
                    else
                    {
                        // Find the convolution for this image and chain to start with
                        match = o.Item2.ConvsBySize.First(p => p.Item1 == o.Item1.ImageSize);

                        // Kick off this chain
                        return new
                        {
                            Chain = o.Item1,
                            Image = o.Item2,
                            ConvResultTask = Task.Run(() => ApplyConvolutionChain(match.Item2, o.Item1, finalSize, isPositiveOnly)),
                        };
                    }
                }).
                ToLookup(o => o.Image.Image.UniqueID).
                ToArray();

            // Commit to output class (acts like a WaitAll)
            return chainResults.Select(result =>
                {
                    ImageConvsBySize image = result.First().Image;      // this is grouped by imageID, so all entries will be the same image

                    // Make sure the chain convolutions are in the same order that was passed in
                    var orderedChains = new List<Tuple<ConvChain_ConvChain, Convolution2D>>();

                    if (useNonChain)
                    {
                        orderedChains.Add(Tuple.Create((ConvChain_ConvChain)null, result.First(p => p.Chain == null).ConvResultTask.Result));
                    }

                    orderedChains.AddRange(chains.
                        Select(o => result.First(p => p.Chain != null && p.Chain.UniqueID == o.UniqueID)).
                        Select(o => Tuple.Create(o.Chain, o.ConvResultTask.Result)));

                    return new ChainOutput(image.Image, orderedChains.ToArray());
                }).ToArray();
        }
        // ***** 

        private void EnsureImageCacheIsCurrent()
        {
            #region Empty

            if (_cachedImages == null)
            {
                _cachedImages = BuildImageCache(_images.ToArray(), _chains);
                return;
            }

            #endregion
            #region Sizes

            int[] sizes = _chains.
                Select(o => o.ImageSize).
                Distinct().
                ToArray();

            if (!sizes.All(o => _cachedImages.Any(p => p.Item1 == o)))
            {
                _cachedImages = BuildImageCache(_images.ToArray(), _chains);
                return;
            }

            #endregion
            #region IDs

            string[] sorted1 = _images.
                Select(o => o.UniqueID).
                OrderBy(o => o).
                ToArray();

            bool outOfSync = false;
            foreach (var set in _cachedImages)
            {
                if (set.Item2.Length != sorted1.Length)
                {
                    outOfSync = true;
                    break;
                }

                string[] sorted2 = set.Item2.
                    Select(o => o.Item1.UniqueID).
                    OrderBy(o => o).
                    ToArray();

                for (int cntr = 0; cntr < sorted1.Length; cntr++)
                {
                    if (sorted1[cntr] != sorted2[cntr])
                    {
                        outOfSync = true;
                        break;
                    }
                }

                if (outOfSync)
                {
                    break;
                }
            }

            if (outOfSync)
            {
                _cachedImages = BuildImageCache(_images.ToArray(), _chains);
            }

            #endregion
        }
        private static Tuple<int, Tuple<FeatureRecognizer_Image, Convolution2D>[]>[] BuildImageCache(FeatureRecognizer_Image[] images, IEnumerable<ConvChain_ConvChain> chains)
        {
            return chains.
                Select(o => o.ImageSize).
                Distinct().
                AsParallel().
                Select(o => Tuple.Create(o, GetImageConvolutions(images, o))).
                ToArray();
        }
        private static Tuple<FeatureRecognizer_Image, Convolution2D>[] GetImageConvolutions(FeatureRecognizer_Image[] images, int size)
        {
            // Read all the images, convert to convolutions for each size (multithreaded)
            return images.
                AsParallel().
                Select(o => Tuple.Create(o, GetConvolution(o.Filename, size))).
                ToArray();
        }

        /// <summary>
        /// Trains a neural net in a separate thread.
        /// Runs more examples through that net and comes up with a score
        /// Stores that score in the status textblock.
        /// </summary>
        private static async void TrainAndTestNN2(ConvChain_NeuralNet item, TextBlock status, FeatureRecognizer_Image[] images, int maxImagesPerCategory, Tuple<int, Tuple<FeatureRecognizer_Image, Convolution2D>[]>[] cachedImages)
        {
            #region Group images by category

            // Get the images by category (only keep what this neural net will use)
            var groupedImages = images.
                Where(o => item.OutputCategories.Any(p => o.Category == p)).
                ToLookup(o => o.Category).
                Select(o => new
                {
                    Category = o.Key,
                    Images = o.Select(p => p).ToArray()
                }).
                ToArray();

            if (groupedImages.Length == 0)
            {
                status.Text = "no images found";
                status.Foreground = Brushes.Red;
                return;
            }

            #endregion

            // Get image convolution delegate
            Func<FeatureRecognizer_Image, int, Convolution2D> getImageConv = new Func<FeatureRecognizer_Image, int, Convolution2D>((img, size) =>
            {
                var matchSize = cachedImages.FirstOrDefault(o => o.Item1 == size);
                if (matchSize == null)
                {
                    return GetConvolution(img.Filename, size);      // if there wasn't a match, it's because the nn's final size was requested (a direct non chain).  Those could be detected and cached up front, but that's a lot of extra work, and not much gained
                }
                else
                {
                    return matchSize.Item2.First(o => o.Item1.UniqueID == img.UniqueID).Item2;
                }
            });

            #region Training images

            // Choose subset to train with (don't bother staying grouped by category, that's stored in the image anyway)
            var trainingImages = groupedImages.
                SelectMany(o => UtilityCore.RandomOrder(o.Images, maxImagesPerCategory).ToArray()).
                ToArray();

            // Remember which images are used for training
            item.TrainingImageIDs = trainingImages.
                Select(o => o.UniqueID).
                ToArray();

            // Run the images through the convolution chains
            var trainingConvsTask = Task.Run(() => GetChainConvolutions2(trainingImages, item.UseNonChain, item.Chains, item.InputSize, item.IsPositiveOnly, getImageConv));

            #endregion
            #region Remaining images

            // Grab all the images that weren't used for training
            var remainingImages = groupedImages.
                SelectMany(o => o.Images).
                Where(o => !trainingImages.Any(p => p.UniqueID == o.UniqueID)).
                ToArray();

            // Convert to convolutions
            var remainingConvsTask = Task.Run(() => GetChainConvolutions(remainingImages, item.UseNonChain, item.Chains, item.InputSize, item.IsPositiveOnly));

            #endregion

            await trainingConvsTask;

            status.Text = "training...";
            status.Foreground = Brushes.DarkGray;

            if (item.Cancel.IsCancellationRequested)
            {
                return;
            }

            #region Train network

            double[][] outputEntries = UtilityEncog.GetOutputLayer(item.OutputCategories.Length);

            var trainedNetworkTask = Task.Run(() =>
            {
                double[][] trainingInput = trainingConvsTask.Result.
                    Select(o => o.FullOutput).
                    ToArray();

                double[][] trainingOutput = trainingConvsTask.Result.
                    Select(o =>
                    {
                        int outputIndex = Array.IndexOf<string>(item.OutputCategories, o.Image.Category);
                        return outputEntries[outputIndex];
                    }).
                    ToArray();

                return UtilityEncog.GetTrainedNetwork(trainingInput, trainingOutput, maxSeconds_PerAttempt: 20, cancelToken: item.Cancel.Token);
            });

            #endregion

            EncogTrainingResponse network = await trainedNetworkTask;

            if (!network.IsSuccess)
            {
                status.Text = "FAIL";
                status.Foreground = Brushes.Red;
                return;
            }

            status.Text = "testing...";
            status.Foreground = Brushes.Gray;

            if (item.Cancel.IsCancellationRequested)
            {
                return;
            }

            #region Test the network

            var testResultTask = Task.Run(() =>
            {
                ChainOutput[] allImageConvs = trainingConvsTask.Result.
                    Concat(remainingConvsTask.Result).
                    ToArray();

                return TestNetwork(network.Network, allImageConvs, item.OutputCategories, item.TrainingImageIDs);
            });

            #endregion

            TestResults scores = await testResultTask;

            status.Text = string.Format("trained: {0}\r\nuntrained: {1}\r\nall: {2}", scores.TotalAvg_Trained.ToStringSignificantDigits(3), scores.TotalAvg_Untrained.ToStringSignificantDigits(3), scores.TotalAvg_All.ToStringSignificantDigits(3));
            status.Foreground = Brushes.OliveDrab;  //some form of red to green

            item.Network = network.Network;
        }

        private static ChainOutput[] GetChainConvolutions2(FeatureRecognizer_Image[] images, bool useNonChain, ConvChain_ConvChain[] chains, int finalSize, bool isPositiveOnly, Func<FeatureRecognizer_Image, int, Convolution2D> getImageConv)
        {
            if (images == null || images.Length == 0)
            {
                return new ChainOutput[0];
            }

            // Figure out what sizes the images should be
            List<int> allImageSizes = new List<int>();
            if (useNonChain)
            {
                allImageSizes.Add(finalSize);
            }

            allImageSizes.AddRange(chains.Select(o => o.ImageSize));

            int[] imageSizes = allImageSizes.
                Distinct().
                ToArray();


            //var finished = GetFinalImageSizes_TEST_Cache(images, chains, finalSize, getImageConv);

            // Apply convolution chains
            return GetFinalImageSizes2(images, useNonChain, chains, finalSize, isPositiveOnly, getImageConv);
        }
        private static ChainOutput[] GetFinalImageSizes2(FeatureRecognizer_Image[] images, bool useNonChain, ConvChain_ConvChain[] chains, int finalSize, bool isPositiveOnly, Func<FeatureRecognizer_Image, int, Convolution2D> getImageConv)
        {
            #region Get list of chains

            List<ConvChain_ConvChain> actualChains = new List<ConvChain_ConvChain>();

            if (useNonChain)
            {
                actualChains.Add(null);
            }

            actualChains.AddRange(chains);

            #endregion

            #region Apply chain convolutions to images (take chains*images)

            var chainResults = UtilityCore.Collate(actualChains, images).
                AsParallel().
                Select(o =>
                {
                    Convolution2D conv = null;

                    if (o.Item1 == null)
                    {
                        // Find the convolution for this image and chain to start with
                        conv = getImageConv(o.Item2, finalSize);

                        return new
                        {
                            Chain = (ConvChain_ConvChain)null,
                            Image = o.Item2,
                            ConvResult = isPositiveOnly ? Convolutions.Abs(conv) : conv,
                        };
                    }
                    else
                    {
                        // Kick off this chain
                        return new
                        {
                            Chain = o.Item1,
                            Image = o.Item2,
                            ConvResult = ApplyConvolutionChain2(o.Item2, o.Item1, finalSize, isPositiveOnly, getImageConv),
                        };
                    }
                }).
                ToLookup(o => o.Image.UniqueID).
                ToArray();

            #endregion

            #region Commit to output class

            return chainResults.Select(result =>
            {
                FeatureRecognizer_Image image = result.First().Image;      // this is grouped by imageID, so all entries will be the same image

                // Make sure the chain convolutions are in the same order that was passed in
                var orderedChains = new List<Tuple<ConvChain_ConvChain, Convolution2D>>();

                if (useNonChain)
                {
                    orderedChains.Add(Tuple.Create((ConvChain_ConvChain)null, result.First(p => p.Chain == null).ConvResult));
                }

                orderedChains.AddRange(chains.
                    Select(o => result.First(p => p.Chain != null && p.Chain.UniqueID == o.UniqueID)).
                    Select(o => Tuple.Create(o.Chain, o.ConvResult)));

                return new ChainOutput(image, orderedChains.ToArray());
            }).ToArray();

            #endregion
        }

        private static Convolution2D ApplyConvolutionChain2(FeatureRecognizer_Image image, ConvChain_ConvChain chain, int finalSize, bool isPositiveOnly, Func<FeatureRecognizer_Image, int, Convolution2D> getImageConv)
        {
            Convolution2D retVal = chain.ApplyChain(image, getImageConv);

            // MaxPool
            if (retVal.Width != finalSize || retVal.Height != finalSize)
            {
                retVal = Convolutions.MaxPool(retVal, finalSize, finalSize);
            }

            // Abs
            if (isPositiveOnly)
            {
                retVal = Convolutions.Abs(retVal);
            }

            return retVal;
        }

        private static TestResults TestNetwork(BasicNetwork network, ChainOutput[] images, string[] categories, string[] trainingImageIDs)
        {
            double[] sumErrors_all = new double[categories.Length];
            int[] sampleCounts_all = new int[categories.Length];

            double[] sumErrors_trained = new double[categories.Length];
            int[] sampleCounts_trained = new int[categories.Length];

            double[] sumErrors_untrained = new double[categories.Length];
            int[] sampleCounts_untrained = new int[categories.Length];

            double[][] idealOutputs = UtilityEncog.GetOutputLayer(categories.Length);

            double[] output = new double[categories.Length];

            foreach (ChainOutput image in images)
            {
                // Classify the image
                network.Compute(image.FullOutput, output);

                int categoryIndex = Array.IndexOf<string>(categories, image.Image.Category);

                // Score is the distance from ideal
                //TODO: See if there is a more statistically sound way of calculating distance (least mean squares, or something?)
                double distance = MathND.GetDistance(output, idealOutputs[categoryIndex]);

                sampleCounts_all[categoryIndex]++;
                sumErrors_all[categoryIndex] += distance;

                if (trainingImageIDs.Any(o => o == image.Image.UniqueID))
                {
                    sampleCounts_trained[categoryIndex]++;
                    sumErrors_trained[categoryIndex] += distance;
                }
                else
                {
                    sampleCounts_untrained[categoryIndex]++;
                    sumErrors_untrained[categoryIndex] += distance;
                }

                // Another way to calculate error could count up non matches and mis matches
                //int? matchIndex = UtilityEncog.IsMatch(output);
            }

            double totalAvg_all = sumErrors_all.Sum() / images.Length.ToDouble();
            double totalAvg_trained = sumErrors_trained.Sum() / sampleCounts_trained.Sum().ToDouble();
            double totalAvg_untrained = sumErrors_untrained.Sum() / sampleCounts_untrained.Sum().ToDouble();

            // Get the averages
            for (int cntr = 0; cntr < categories.Length; cntr++)
            {
                if (sampleCounts_all[cntr] > 0)
                {
                    sumErrors_all[cntr] /= sampleCounts_all[cntr].ToDouble();
                }

                if (sampleCounts_trained[cntr] > 0)
                {
                    sumErrors_trained[cntr] /= sampleCounts_trained[cntr].ToDouble();
                }

                if (sampleCounts_untrained[cntr] > 0)
                {
                    sumErrors_untrained[cntr] /= sampleCounts_untrained[cntr].ToDouble();
                }
            }

            return new TestResults()
            {
                SumErrors_All = sumErrors_all,
                SumErrors_Trained = sumErrors_trained,
                SumErrors_Untrained = sumErrors_untrained,

                TotalAvg_All = totalAvg_all,
                TotalAvg_Trained = totalAvg_trained,
                TotalAvg_Untrained = totalAvg_untrained,
            };
        }

        private void ShowResult_Latest()
        {
            if (_lastShown == LastShown.NN || _lastShown == LastShown.Unknown)
            {
                // Try to show neural net results
                ShowResult_NN();
                if (_lastShown == LastShown.NN)
                {
                    return;
                }
            }

            // Try to show a chain
            ShowResult_Chain();
        }

        private void ShowResult_NN()
        {
            panelResult.Content = null;

            if (_images.Count == 0 || _neuralNets.Count == 0)
            {
                return;
            }

            var nn = GetSelected_NN();
            if (nn == null)
            {
                nn = _neuralNets[StaticRandom.Next(_neuralNets.Count)];
            }

            var image = GetSelected_Image();
            if (image == null)
            {
                var trainedAgainst = _images.Where(o => nn.OutputCategories.Any(p => p == o.Category)).ToArray();
                if (trainedAgainst.Length == 0)      // should never happen
                {
                    image = _images[StaticRandom.Next(_images.Count)];
                }
                else
                {
                    image = trainedAgainst[StaticRandom.Next(trainedAgainst.Length)];
                }
            }

            ShowResult_NN(image, nn);

            // If they are way zoomed in, they are likely looking at the last image
            panelResult.ScrollToEnd();

            _lastShown = LastShown.NN;
        }
        private void ShowResult_NN(FeatureRecognizer_Image image, ConvChain_NeuralNet nn)
        {
            ConvolutionResultNegPosColoring edgeColor = (ConvolutionResultNegPosColoring)cboEdgeColors.SelectedValue;

            _resultsScale = new ScaleTransform(trkZoom.Value, trkZoom.Value);

            StackPanel mainPanel = new StackPanel();

            Grid grid = new Grid();
            mainPanel.Children.Add(grid);

            List<Convolution2D> results = new List<Convolution2D>();

            // One column per conv chain
            foreach (var chain in nn.GetChains())       // using the method, because the first item could be null (direct use of the image)
            {
                #region Convolution Chain

                if (grid.ColumnDefinitions.Count > 0)
                {
                    grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(RESULTMARGIN) });
                }

                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });

                var chainColumn = ShowResult_Chain(image, chain, nn.InputSize, edgeColor, chkIsPosWhiteBlack.IsChecked.Value, _convolutionContextMenu);

                Grid.SetColumn(chainColumn.Item1, grid.ColumnDefinitions.Count - 1);

                chainColumn.Item1.LayoutTransform = _resultsScale;

                grid.Children.Add(chainColumn.Item1);

                Convolution2D conv = chainColumn.Item2;

                #endregion

                #region Finish Chain

                // MaxPool
                if (conv.Width != nn.InputSize || conv.Height != nn.InputSize)
                {
                    conv = Convolutions.MaxPool(conv, nn.InputSize, nn.InputSize);
                }

                // Abs
                if (nn.IsPositiveOnly)
                {
                    conv = Convolutions.Abs(conv);
                }

                // Store it
                results.Add(conv);

                #endregion
            }

            #region Show final convolution results

            grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(RESULTMARGIN) });
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
            grid.Margin = new Thickness(0, RESULTMARGIN, 0, 0);
            mainPanel.Children.Add(grid);

            foreach (Convolution2D finalConv in results)
            {
                var poolSize = Convolutions.GetThumbSizeAndPixelMultiplier(finalConv, MAXPOOLDISPLAYSIZE);

                for (int cntr = 0; cntr <= 1; cntr++)       // draws two.  the first is original, and second is exaggerated
                {
                    string tooltip = Convolutions.GetToolTip(finalConv, ConvolutionToolTipType.Size_MinMax);
                    if (cntr == 1)
                    {
                        tooltip = "exaggerated\r\n" + tooltip;
                    }

                    Image poolImage = new Image()
                    {
                        Source = Convolutions.GetBitmap_Aliased(finalConv, poolSize.Item2, edgeColor, cntr == 0 ? 255 : (double?)null, chkIsPosWhiteBlack.IsChecked.Value),
                        Width = poolSize.Item1.Width,
                        Height = poolSize.Item1.Height,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Tag = finalConv,
                        ContextMenu = _convolutionContextMenu,
                        ToolTip = tooltip,
                    };

                    if (cntr == 0)      // only add a column once
                    {
                        if (grid.ColumnDefinitions.Count > 0)
                        {
                            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(RESULTMARGIN) });
                        }

                        grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
                    }

                    Grid.SetColumn(poolImage, grid.ColumnDefinitions.Count - 1);
                    Grid.SetRow(poolImage, cntr * 2);        // 0 or 2

                    poolImage.LayoutTransform = _resultsScale;

                    grid.Children.Add(poolImage);
                }
            }

            #endregion

            // Under that is the results (what it thinks the image is)
            if (nn.Network != null)
            {
                #region NN Results

                double[] result = nn.Network.Compute(results.SelectMany(o => o.Values).ToArray());

                grid = new Grid()
                {
                    Margin = new Thickness(0, RESULTMARGIN, 0, 0),
                };

                mainPanel.Children.Add(grid);

                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(RESULTMARGIN) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });

                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(RESULTMARGIN) });
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });

                // Final Guess
                UIElement guess = UtilityEncogWPF.GetFinalGuess(nn.OutputCategories, result);
                Grid.SetColumn(guess, 0);
                Grid.SetColumnSpan(guess, 3);
                Grid.SetRow(guess, 0);
                grid.Children.Add(guess);

                // Result list (unsorted)
                UIElement resultList = UtilityEncogWPF.GetResultList(nn.OutputCategories, result, false);
                Grid.SetColumn(resultList, 0);
                Grid.SetRow(resultList, 2);
                grid.Children.Add(resultList);

                // Result list (sorted)
                resultList = UtilityEncogWPF.GetResultList(nn.OutputCategories, result, true);
                Grid.SetColumn(resultList, 2);
                Grid.SetRow(resultList, 2);
                grid.Children.Add(resultList);

                #endregion
            }

            if (nn.TrainingImageIDs != null && nn.TrainingImageIDs.Any(o => o == image.UniqueID))
            {
                mainPanel.Children.Add(new TextBlock()
                    {
                        Text = "used in training",
                        Foreground = Brushes.Gray,
                        Margin = new Thickness(0, RESULTMARGIN, 0, 0),
                    });
            }

            // To the right of the guess should be visuals of the neural net: (also put an option in the context menu)
            //UtilityEncogWPF.GetNetworkVisual_Collapsed


            panelResult.Content = mainPanel;
        }

        private void ShowResult_Chain()
        {
            panelResult.Content = null;

            if (_images.Count == 0 || _chains.Count == 0)
            {
                return;
            }

            var image = GetSelected_Image();
            if (image == null)
            {
                image = _images[StaticRandom.Next(_images.Count)];
            }

            var chain = GetSelected_ConvChain();
            if (chain == null)
            {
                chain = _chains[StaticRandom.Next(_chains.Count)];
            }

            var result = ShowResult_Chain(image, chain, -1, (ConvolutionResultNegPosColoring)cboEdgeColors.SelectedValue, chkIsPosWhiteBlack.IsChecked.Value, _convolutionContextMenu);
            Grid grid = result.Item1;

            if (grid != null)
            {
                _resultsScale = new ScaleTransform(trkZoom.Value, trkZoom.Value);
                grid.LayoutTransform = _resultsScale;
            }

            panelResult.Content = grid;

            // If they are way zoomed in, they are likely looking at the last image
            panelResult.ScrollToEnd();

            _lastShown = LastShown.Chain;
        }
        private static Tuple<Grid, Convolution2D> ShowResult_Chain(FeatureRecognizer_Image image, ConvChain_ConvChain chain, int finalSize, ConvolutionResultNegPosColoring edgeColor, bool invertPos, ContextMenu convContextMenu)
        {
            // Init grid
            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(RESULTMARGIN, GridUnitType.Pixel) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });

            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });

            // Image
            Convolution2D convolution = GetConvolution(image.Filename, chain == null ? finalSize : chain.ImageSize);

            // Add to grid
            ShowResult_Chain_Right(grid, convolution, ConvolutionResultNegPosColoring.BlackWhite, false, convContextMenu);     // this first is the original image, and shouldn't be inverted (in case they have whiteblack selected)

            if (chain != null)
            {
                foreach (ConvolutionBase2D link in chain.Convolutions)
                {
                    convolution = Convolutions.Convolute(convolution, link);

                    ShowResult_Chain_Left(grid, link, edgeColor, convContextMenu);
                    ShowResult_Chain_Right(grid, convolution, edgeColor, invertPos, convContextMenu);
                }
            }

            return Tuple.Create(grid, convolution);
        }
        private static void ShowResult_Chain_Left(Grid grid, ConvolutionBase2D kernel, ConvolutionResultNegPosColoring edgeColor, ContextMenu convContextMenu)
        {
            Border border = Convolutions.GetThumbnail(kernel, THUMBSIZE_RESULT, convContextMenu);

            border.HorizontalAlignment = HorizontalAlignment.Right;
            border.VerticalAlignment = VerticalAlignment.Center;

            Grid.SetColumn(border, 0);

            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(RESULTMARGIN), MinHeight = RESULTMARGIN, MaxHeight = RESULTMARGIN });     // without the maxheight, this will grow instead of the auto rows
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });

            Grid.SetRow(border, grid.RowDefinitions.Count - 3);
            Grid.SetRowSpan(border, 3);

            grid.Children.Add(border);
        }
        private static void ShowResult_Chain_Right(Grid grid, Convolution2D convolution, ConvolutionResultNegPosColoring edgeColor, bool invertPos, ContextMenu convContextMenu)
        {
            Image imageCtrl = new Image()
            {
                Source = Convolutions.GetBitmap(convolution, edgeColor, forcePos_WhiteBlack: invertPos),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = Convolutions.GetToolTip(convolution, ConvolutionToolTipType.Size_MinMax),
                Tag = convolution,
                ContextMenu = convContextMenu,
            };

            Grid.SetColumn(imageCtrl, 2);

            //NOTE: There is already an auto row
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(RESULTMARGIN), MinHeight = RESULTMARGIN, MaxHeight = RESULTMARGIN });     // without the maxheight, this will grow instead of the auto rows
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });

            Grid.SetRow(imageCtrl, grid.RowDefinitions.Count - 3);
            Grid.SetRowSpan(imageCtrl, 3);

            grid.Children.Add(imageCtrl);
        }

        private static Convolution2D GetConvolution(string filename, int size)
        {
            BitmapSource bitmap = new BitmapImage(new Uri(filename));
            bitmap = UtilityWPF.ResizeImage(bitmap, size, true);

            Convolution2D retVal = UtilityWPF.ConvertToConvolution(bitmap);
            if (retVal.Width != retVal.Height)
            {
                retVal = Convolutions.ExtendBorders(retVal, size, size);        //NOTE: width or height is already the desired size, this will just enlarge the other to make it square
            }

            return retVal;
        }

        private static Convolution2D ApplyConvolutionChain(Convolution2D image, ConvChain_ConvChain chain, int finalSize, bool isPositiveOnly)
        {
            Convolution2D retVal = image;

            // Apply chain
            foreach (ConvolutionBase2D link in chain.Convolutions)
            {
                retVal = Convolutions.Convolute(retVal, link);
            }

            // MaxPool
            if (retVal.Width != finalSize || retVal.Height != finalSize)
            {
                retVal = Convolutions.MaxPool(retVal, finalSize, finalSize);
            }

            // Abs
            if (isPositiveOnly)
            {
                retVal = Convolutions.Abs(retVal);
            }

            return retVal;
        }

        private static UIElement BuildCheckboxTooltip(string text, Border thumbnail)
        {
            StackPanel retVal = new StackPanel() { Orientation = Orientation.Horizontal };

            // Textblock
            TextBlock textblock = new TextBlock()
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(2, 2, 0, 2),
            };

            // Convert \r\n into <LineBreak/>
            string[] lines = text.Split("\r\n".ToCharArray());
            for (int cntr = 0; cntr < lines.Length; cntr++)
            {
                if (string.IsNullOrWhiteSpace(lines[cntr]))      // for some reason the string.split is creating extra blank lines
                {
                    continue;
                }

                if (cntr > 0)
                {
                    textblock.Inlines.Add(new LineBreak());
                }

                textblock.Inlines.Add(lines[cntr]);
            }

            retVal.Children.Add(textblock);

            // Default is 6 all around, which is excessive
            thumbnail.Margin = new Thickness(6, 4, 2, 2);
            retVal.Children.Add(thumbnail);

            return retVal;
        }

        #endregion

        #region test -- tasks

        private async void TestTasks_Click(object sender, RoutedEventArgs e)
        {
            const int FINALSIZE = 10;

            try
            {
                var images = _images.ToArray();
                var chain = _chains[0];
                DateTime start = DateTime.UtcNow;

                var trainingConvs = await Task.Run(() => new ThreadIDLog<ChainOutput[]>(GetChainConvolutions_TEST_Tasks(images, chain, FINALSIZE), Tuple.Create(Thread.CurrentThread.ManagedThreadId, DateTime.UtcNow, "global")));

                DateTime stop = DateTime.UtcNow;

                TestTasks_Report(panelResult, trainingConvs, start, stop);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void TestTasks_Report(ScrollViewer panelResult, ThreadIDLog<ChainOutput[]> trainingConvs, DateTime start, DateTime stop)
        {
            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(8) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(8) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
            grid.Margin = new Thickness(100, 0, 0, 60);

            panelResult.Content = grid;

            // Header
            TestTasks_AddRow(grid, "Thread ID", "Time", "Description");

            foreach (var threadID in trainingConvs.ThreadIDs.OrderBy(o => o.Item2))
            {
                // Entry
                TestTasks_AddRow(grid,
                    threadID.Item1.ToString(),
                    (threadID.Item2 - start).TotalMilliseconds.ToStringSignificantDigits(1),
                    threadID.Item3);
            }

            TestTasks_AddRow(grid, "", "", "");

            // Final
            TestTasks_AddRow(grid,
                "",
                (stop - start).TotalMilliseconds.ToStringSignificantDigits(1),
                "final");

            TestTasks_AddRow(grid, "", "", "");

            // num images
            TestTasks_AddRow(grid, "", trainingConvs.Item.Length.ToString(), "num images");

            // num uses
            foreach (var threadGroup in trainingConvs.ThreadIDs.ToLookup(o => o.Item1))
            {
                TestTasks_AddRow(grid, threadGroup.Key.ToString(), threadGroup.Count().ToString(), "thread ID usage");
            }

            // num threads
            TestTasks_AddRow(grid, "", trainingConvs.ThreadIDs.Distinct(o => o.Item1).Count().ToString(), "num threads");
        }
        private static void TestTasks_AddRow(Grid grid, string col1, string col2, string col3)
        {
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });

            // ThreadID
            TextBlock text = new TextBlock()
            {
                Text = col1,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
            };

            Grid.SetColumn(text, 0);
            Grid.SetRow(text, grid.RowDefinitions.Count - 1);
            grid.Children.Add(text);

            // Time
            text = new TextBlock()
            {
                Text = col2,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
            };

            Grid.SetColumn(text, 2);
            Grid.SetRow(text, grid.RowDefinitions.Count - 1);
            grid.Children.Add(text);

            // Description
            text = new TextBlock()
            {
                Text = col3,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
            };

            Grid.SetColumn(text, 4);
            Grid.SetRow(text, grid.RowDefinitions.Count - 1);
            grid.Children.Add(text);
        }

        private static ThreadIDLog<ChainOutput[]> GetChainConvolutions_TEST_Tasks(FeatureRecognizer_Image[] images, ConvChain_ConvChain chain, int finalSize)
        {
            var initialConvs = GetImageConvolutions_TEST_Tasks(images, new[] { chain.ImageSize });

            var finished = GetFinalImageSizes_TEST_Tasks(initialConvs.Item, new[] { chain }, finalSize);

            var threadIDs = initialConvs.ThreadIDs.Concat(finished.ThreadIDs);

            return new ThreadIDLog<ChainOutput[]>(finished.Item, threadIDs);
        }
        private static ThreadIDLog<ImageConvsBySize[]> GetImageConvolutions_TEST_Tasks(FeatureRecognizer_Image[] images, int[] sizes)
        {
            // Read all the images, convert to convolutions for each size (multithreaded)
            var rawResults = images.
                AsParallel().
                Select(image => new
                {
                    ThreadID = Tuple.Create(Thread.CurrentThread.ManagedThreadId, DateTime.UtcNow, "image group"),
                    Image = image,
                    BySize = sizes.
                        AsParallel().
                        Select(size => new
                        {
                            Size = size,
                            Convolution = GetConvolution(image.Filename, size),
                            ThreadID = Tuple.Create(Thread.CurrentThread.ManagedThreadId, DateTime.UtcNow, "image,size"),
                        }).ToArray(),
                }).
                ToArray();

            var threadIDs = rawResults.
                Select(o => o.ThreadID).
                Concat(rawResults.SelectMany(o => o.BySize).Select(o => o.ThreadID));

            // Now that everything is done, commit to classes
            var array = rawResults.
                Select(o => new ImageConvsBySize(o.Image, o.BySize.Select(p => Tuple.Create(p.Size, p.Convolution)).ToArray())).
                ToArray();

            return new ThreadIDLog<ImageConvsBySize[]>(array, threadIDs);
        }
        private static ThreadIDLog<ChainOutput[]> GetFinalImageSizes_TEST_Tasks(ImageConvsBySize[] imagesBySize, ConvChain_ConvChain[] chains, int finalSize)
        {
            //NOTE: I decided to use Task.Run instead of AsParallel to make sure each runs in its own task (applying the chain can be expensive)
            var chainResults = UtilityCore.Collate(chains, imagesBySize).
                Select(o =>
                {
                    Tuple<int, Convolution2D> match = null;

                    // Find the convolution for this image and chain to start with
                    match = o.Item2.ConvsBySize.First(p => p.Item1 == o.Item1.ImageSize);

                    // Kick off this chain
                    return new
                    {
                        Chain = o.Item1,
                        Image = o.Item2,
                        ConvResultTask = Task.Run(() => new ThreadIDLog<Convolution2D>(ApplyConvolutionChain(match.Item2, o.Item1, finalSize, false), Tuple.Create(Thread.CurrentThread.ManagedThreadId, DateTime.UtcNow, "finish chain"))),
                    };
                }).
                ToLookup(o => o.Image.Image.UniqueID).
                ToArray();

            // Commit to output class (acts like a WaitAll)
            var resultPairs = chainResults.Select(result =>
            {
                ImageConvsBySize image = result.First().Image;      // this is grouped by imageID, so all entries will be the same image

                // Make sure the chain convolutions are in the same order that was passed in
                var orderedChains = chains.
                    Select(o => result.First(p => p.Chain != null && p.Chain.UniqueID == o.UniqueID)).
                    Select(o => new { Item = Tuple.Create(o.Chain, o.ConvResultTask.Result.Item), IDs = o.ConvResultTask.Result.ThreadIDs }).
                    ToArray();

                return new
                {
                    Output = new ChainOutput(image.Image, orderedChains.Select(o => o.Item).ToArray()),
                    IDs = orderedChains.SelectMany(o => o.IDs).ToArray(),
                };
            }).ToArray();

            return new ThreadIDLog<ChainOutput[]>(resultPairs.Select(o => o.Output).ToArray(), resultPairs.SelectMany(o => o.IDs));
        }

        #region Class: ThreadIDLog

        private class ThreadIDLog<T>
        {
            public ThreadIDLog(ThreadIDLog<T> item, Tuple<int, DateTime, string> threadID)
            {
                this.Item = item.Item;
                this.ThreadIDs = item.ThreadIDs.Concat(new[] { threadID }).ToArray();
            }
            public ThreadIDLog(T item, Tuple<int, DateTime, string> threadID)
            {
                this.Item = item;
                this.ThreadIDs = new[] { threadID };
            }
            public ThreadIDLog(T item, IEnumerable<Tuple<int, DateTime, string>> threadIDs)
            {
                this.Item = item;
                this.ThreadIDs = threadIDs.ToArray();
            }

            public readonly T Item;
            public readonly Tuple<int, DateTime, string>[] ThreadIDs;
        }

        #endregion

        #endregion
        #region test -- as parallel

        //NOTE: AsParallel is same speed, but uses fewer threads (the only method that changed is GetFinalImageSizes)
        private async void TestAsParallel_Click(object sender, RoutedEventArgs e)
        {
            const int FINALSIZE = 10;

            try
            {
                var images = _images.ToArray();
                var chain = _chains[0];
                DateTime start = DateTime.UtcNow;

                var trainingConvs = await Task.Run(() => new ThreadIDLog<ChainOutput[]>(GetChainConvolutions_TEST_AsParallel(images, chain, FINALSIZE), Tuple.Create(Thread.CurrentThread.ManagedThreadId, DateTime.UtcNow, "global")));

                DateTime stop = DateTime.UtcNow;

                TestTasks_Report(panelResult, trainingConvs, start, stop);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static ThreadIDLog<ChainOutput[]> GetChainConvolutions_TEST_AsParallel(FeatureRecognizer_Image[] images, ConvChain_ConvChain chain, int finalSize)
        {
            var initialConvs = GetImageConvolutions_TEST_Tasks(images, new[] { chain.ImageSize });      // no need to change this method, it's already as parallel

            var finished = GetFinalImageSizes_TEST_AsParallel(initialConvs.Item, new[] { chain }, finalSize);

            var threadIDs = initialConvs.ThreadIDs.Concat(finished.ThreadIDs);

            return new ThreadIDLog<ChainOutput[]>(finished.Item, threadIDs);
        }
        private static ThreadIDLog<ChainOutput[]> GetFinalImageSizes_TEST_AsParallel(ImageConvsBySize[] imagesBySize, ConvChain_ConvChain[] chains, int finalSize)
        {
            var chainResults = UtilityCore.Collate(chains, imagesBySize).
                AsParallel().
                Select(o =>
                {
                    Tuple<int, Convolution2D> match = null;

                    // Find the convolution for this image and chain to start with
                    match = o.Item2.ConvsBySize.First(p => p.Item1 == o.Item1.ImageSize);

                    // Kick off this chain
                    return new
                    {
                        ThreadID = Tuple.Create(Thread.CurrentThread.ManagedThreadId, DateTime.UtcNow, "finish chain"),
                        Chain = o.Item1,
                        Image = o.Item2,
                        ConvResultTask = ApplyConvolutionChain(match.Item2, o.Item1, finalSize, false),
                    };
                }).
                ToLookup(o => o.Image.Image.UniqueID).
                ToArray();

            // Commit to output class
            var resultPairs = chainResults.Select(result =>
            {
                ImageConvsBySize image = result.First().Image;      // this is grouped by imageID, so all entries will be the same image

                // Make sure the chain convolutions are in the same order that was passed in
                var orderedChains = chains.
                    Select(o => result.First(p => p.Chain != null && p.Chain.UniqueID == o.UniqueID)).
                    Select(o => Tuple.Create(o.Chain, o.ConvResultTask)).
                    ToArray();

                return new ChainOutput(image.Image, orderedChains);
            }).ToArray();

            return new ThreadIDLog<ChainOutput[]>(resultPairs, chainResults.SelectMany(o => o).Select(o => o.ThreadID));
        }

        #endregion
        #region test -- cache final

        //NOTE: AsParallel is same speed, but uses fewer threads (the only method that changed is GetFinalImageSizes)
        private async void TestCacheFinal_Click(object sender, RoutedEventArgs e)
        {
            const int FINALSIZE = 10;

            try
            {
                var images = _images.ToArray();
                var chain = _chains[0];
                DateTime start = DateTime.UtcNow;

                var trainingConvs = await Task.Run(() => new ThreadIDLog<ChainOutput[]>(GetChainConvolutions_TEST_Cache(images, chain, FINALSIZE), Tuple.Create(Thread.CurrentThread.ManagedThreadId, DateTime.UtcNow, "global")));

                DateTime stop = DateTime.UtcNow;

                TestTasks_Report(panelResult, trainingConvs, start, stop);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static ThreadIDLog<ChainOutput[]> GetChainConvolutions_TEST_Cache(FeatureRecognizer_Image[] images, ConvChain_ConvChain chain, int finalSize)
        {
            //NOTE: When there's only one chain, there's no need to cache the image convolutions.  Final code will need to cache these as well
            Func<FeatureRecognizer_Image, int, Convolution2D> getImageConv = new Func<FeatureRecognizer_Image, int, Convolution2D>((img, size) =>
            {
                return GetConvolution(img.Filename, size);
            });

            var finished = GetFinalImageSizes_TEST_Cache(images, new[] { chain }, finalSize, getImageConv);

            return new ThreadIDLog<ChainOutput[]>(finished.Item, finished.ThreadIDs);
        }
        private static ThreadIDLog<ChainOutput[]> GetFinalImageSizes_TEST_Cache(FeatureRecognizer_Image[] images, ConvChain_ConvChain[] chains, int finalSize, Func<FeatureRecognizer_Image, int, Convolution2D> getImageConv)
        {
            var chainResults = UtilityCore.Collate(chains, images).
                AsParallel().
                Select(o =>
                {
                    // Kick off this chain
                    return new
                    {
                        ThreadID = Tuple.Create(Thread.CurrentThread.ManagedThreadId, DateTime.UtcNow, "finish chain"),
                        Chain = o.Item1,
                        Image = o.Item2,
                        ConvResultTask = ApplyConvolutionChain_Cache(o.Item2, o.Item1, finalSize, false, getImageConv),
                    };
                }).
                ToLookup(o => o.Image.UniqueID).
                ToArray();

            // Commit to output class
            var resultPairs = chainResults.Select(result =>
            {
                FeatureRecognizer_Image image = result.First().Image;      // this is grouped by imageID, so all entries will be the same image

                // Make sure the chain convolutions are in the same order that was passed in
                var orderedChains = chains.
                    Select(o => result.First(p => p.Chain != null && p.Chain.UniqueID == o.UniqueID)).
                    Select(o => Tuple.Create(o.Chain, o.ConvResultTask)).
                    ToArray();

                return new ChainOutput(image, orderedChains);
            }).ToArray();

            return new ThreadIDLog<ChainOutput[]>(resultPairs, chainResults.SelectMany(o => o).Select(o => o.ThreadID));
        }

        private static Convolution2D ApplyConvolutionChain_Cache(FeatureRecognizer_Image image, ConvChain_ConvChain chain, int finalSize, bool isPositiveOnly, Func<FeatureRecognizer_Image, int, Convolution2D> getImageConv)
        {
            Convolution2D retVal = chain.ApplyChain(image, getImageConv);

            // MaxPool
            if (retVal.Width != finalSize || retVal.Height != finalSize)
            {
                retVal = Convolutions.MaxPool(retVal, finalSize, finalSize);
            }

            // Abs
            if (isPositiveOnly)
            {
                retVal = Convolutions.Abs(retVal);
            }

            return retVal;
        }

        #endregion
        #region test -- cache final2

        //NOTE: AsParallel is same speed, but uses fewer threads (the only method that changed is GetFinalImageSizes)
        private async void TestCacheFinal2_Click(object sender, RoutedEventArgs e)
        {
            const int FINALSIZE = 10;

            try
            {
                var images = _images.ToArray();
                var chain = _chains[0];
                DateTime start = DateTime.UtcNow;

                var trainingConvs = await Task.Run(() => new ThreadIDLog<ChainOutput[]>(GetChainConvolutions_TEST_Cache2(images, chain, FINALSIZE), Tuple.Create(Thread.CurrentThread.ManagedThreadId, DateTime.UtcNow, "global")));

                DateTime stop = DateTime.UtcNow;

                TestTasks_Report(panelResult, trainingConvs, start, stop);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static ThreadIDLog<ChainOutput[]> GetChainConvolutions_TEST_Cache2(FeatureRecognizer_Image[] images, ConvChain_ConvChain chain, int finalSize)
        {
            ConvChain_ConvChain[] chains = new[] { chain };

            ThreadIDLog<ImageConvsBySize[]> imagesBySize = GetImageConvolutions_TEST_Tasks(images, chains.Select(o => o.ImageSize).Distinct().ToArray());

            Func<FeatureRecognizer_Image, int, Convolution2D> getImageConv = new Func<FeatureRecognizer_Image, int, Convolution2D>((img, size) =>
            {
                var matchImage = imagesBySize.Item.First(o => o.Image.UniqueID == img.UniqueID);
                return matchImage.ConvsBySize.First(o => o.Item1 == size).Item2;
            });

            var finished = GetFinalImageSizes_TEST_Cache(images, chains, finalSize, getImageConv);

            return new ThreadIDLog<ChainOutput[]>(finished.Item, imagesBySize.ThreadIDs.Concat(finished.ThreadIDs));
        }

        #endregion
    }

    #region Class: ConvChain_ConvChain

    //TODO: Make properties so that this is serializable
    public class ConvChain_ConvChain
    {
        #region Declaration Section

        private readonly object _lock = new object();

        private SortedList<string, Tuple<int, Convolution2D>[]> _cache = new SortedList<string, Tuple<int, Convolution2D>[]>();

        #endregion

        public string UniqueID { get; set; }

        public int ImageSize { get; set; }

        public ConvolutionBase2D[] Convolutions { get; set; }

        public UIElement Control { get; set; }

        #region Public Methods

        public VectorInt GetFinalSize()
        {
            VectorInt retVal = new VectorInt(this.ImageSize, this.ImageSize);

            if (this.Convolutions != null)
            {
                foreach (var conv in this.Convolutions)
                {
                    retVal -= conv.GetReduction();
                }
            }

            return retVal;
        }

        /// <summary>
        /// This applies the chain's convolutions, and caches the result (any request after that will be pulled from the cache)
        /// </summary>
        /// <param name="image">The image to apply the chain to</param>
        /// <param name="getImage">If the result isn't cached, this delegate will get called to get an image of that size (and will probably want to cache it)</param>
        /// <remarks>
        /// TODO: If this chain will see lots of images, come up with a way for old ones to roll off (may want to make/find a generic memory cache that has similar features as the garbage collector)
        /// </remarks>
        public Convolution2D ApplyChain(FeatureRecognizer_Image image, Func<FeatureRecognizer_Image, int, Convolution2D> getImage)
        {
            lock (_lock)
            {
                // Pull from cache
                Tuple<int, Convolution2D>[] bySize;
                if (_cache.TryGetValue(image.UniqueID, out bySize))
                {
                    var match = bySize.FirstOrDefault(o => o.Item1 == this.ImageSize);
                    if (match != null)
                    {
                        return match.Item2;
                    }
                }

                // It's not in the cache, build it
                Convolution2D imageConv = getImage(image, this.ImageSize);
                Convolution2D retVal = ApplyChain(imageConv);

                // Store it
                if (bySize == null)
                {
                    _cache.Add(image.UniqueID, new[] { Tuple.Create(this.ImageSize, retVal) });
                }
                else
                {
                    bySize = UtilityCore.ArrayAdd(bySize, Tuple.Create(this.ImageSize, retVal));
                    _cache[image.UniqueID] = bySize;
                }

                return retVal;
            }
        }
        /// <summary>
        /// This overload doesn't cache the result
        /// </summary>
        public Convolution2D ApplyChain(Convolution2D image)
        {
            if (image.Width != this.ImageSize || image.Height != this.ImageSize)
            {
                throw new ArgumentException(string.Format("The image passed in is the wrong size.  Image: {0}x{1}, Expected: {2}", image.Width, image.Height, this.ImageSize));
            }

            Convolution2D retVal = image;

            foreach (ConvolutionBase2D link in this.Convolutions)
            {
                retVal = Game.HelperClassesWPF.Convolutions.Convolute(retVal, link);
            }

            return retVal;
        }

        #endregion
    }

    #endregion
    #region Class: ConvChain_NeuralNet

    //TODO: Make properties so that this is serializable
    public class ConvChain_NeuralNet
    {
        public string UniqueID { get; set; }

        public bool UseNonChain { get; set; }

        public string[] ChainIDs { get; set; }
        public ConvChain_ConvChain[] Chains { get; set; }

        public IEnumerable<ConvChain_ConvChain> GetChains()
        {
            if (this.UseNonChain)
            {
                yield return null;
            }

            foreach (var chain in this.Chains)
            {
                yield return chain;
            }
        }

        public int InputSize { get; set; }

        public bool IsPositiveOnly { get; set; }

        public string[] OutputCategories { get; set; }

        public string[] TrainingImageIDs { get; set; }

        public CancellationTokenSource Cancel { get; set; }

        public BasicNetwork Network { get; set; }

        public UIElement Control { get; set; }
    }

    #endregion
}
