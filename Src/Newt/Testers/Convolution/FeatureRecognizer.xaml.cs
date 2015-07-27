using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.Testers.Convolution;
using Microsoft.Win32;

namespace Game.Newt.Testers.Convolution
{
    //TODO: Some convolutions should just be an outline (like a coffee mug)
    //Prove that setting the interior to zero will ignore any feautures in that area (logos/pictures on the mug)
    //If zeros don't work, make a derived convolution class that has nullable values
    //
    //These outline convolutions will probably need to be built against edge data

    //Once that is proven, come up with a way to get the outline convolution
    //probably compare several similar extracts, and only keep the intersection of them

    //TODO: Outline convolutions shouldn't care if an item is dark on light, or light on dark

    public partial class FeatureRecognizer : Window
    {
        #region Class: ReducedExtract

        private class ReducedExtract
        {
            public double Percent { get; set; }

            public VectorInt ImageSize { get; set; }

            public RectInt Extract { get; set; }
        }

        #endregion
        #region Class: ApplyResult

        private class ApplyResult
        {
            public ApplyResult(Convolution2D imageConv, Convolution2D filtered, Tuple<VectorInt, ApplyResult_Match[]>[] matches)
            {
                this.ImageConv = imageConv;
                this.Filtered = filtered;
                this.Matches = matches;
            }

            public readonly Convolution2D ImageConv;
            public readonly Convolution2D Filtered;

            /// <summary>
            /// The vector is the location as percent width, percent height.
            /// The double is a score, 1 is a perfect score.
            /// </summary>
            public readonly Tuple<VectorInt, ApplyResult_Match[]>[] Matches;
        }

        #endregion
        #region Class: ApplyResult_Match

        private class ApplyResult_Match
        {
            public ApplyResult_Match(bool isMatch, double weight, Convolution2D patch)
            {
                this.IsMatch = isMatch;
                this.Weight = weight;
                this.Patch = patch;
            }

            //NOTE: The final version shouldn't bother storing non matches.  This is just so the candidate patches can be shown
            public readonly bool IsMatch;

            public readonly double Weight;      // or score?

            // These are only so the patches can be shown.  They aren't needed in the final version
            public readonly Convolution2D Patch;
        }

        #endregion

        #region Enum: PrefilterType

        private enum PrefilterType
        {
            Gaussian,
            Gaussian_Subtract,
            MaxAbs_Sobel,
            Gaussian_Edge,
        }

        #endregion

        #region Declaration Section

        private const int IMAGESIZE = 300;
        private const int THUMBSIZE_IMAGE = 85;
        private const int THUMBSIZE_EXTRACT = 40;

        /// <summary>
        /// This will be a subfolder of AsteroidMiner
        /// </summary>
        private const string FOLDER = "FeatureRecognizer";
        private const string LATEST = "Latest";
        private const string WORKING = "working - ";
        private const string SESSION = "session.xml";

        private string _workingFolder = null;

        /// <summary>
        /// These are optional convolutions that could be applied between the image and extract
        /// </summary>
        private Tuple<PrefilterType, ConvolutionBase2D[]>[] _preFilters = null;

        private List<FeatureRecognizer_Image> _images = new List<FeatureRecognizer_Image>();

        private List<FeatureRecognizer_Extract> _extracts = new List<FeatureRecognizer_Extract>();
        private int _selectedExtractIndex = -1;

        private List<Window> _childWindows = new List<Window>();

        private readonly DropShadowEffect _selectEffect;

        private readonly ContextMenu _extractContextMenu;
        private readonly ContextMenu _extractResultContextMenu;

        #endregion

        #region Constructor

        public FeatureRecognizer()
        {
            InitializeComponent();

            this.Background = SystemColors.ControlBrush;

            // Selected effect
            _selectEffect = new DropShadowEffect()
            {
                Direction = 0,
                ShadowDepth = 0,
                BlurRadius = 13,
                //Color = UtilityWPF.ColorFromHex("FFC929"),        // orange
                Color = UtilityWPF.ColorFromHex("29FFC6"),
                Opacity = .9,
            };

            // Context Menu
            _extractContextMenu = (ContextMenu)this.Resources["extractContextMenu"];
            _extractResultContextMenu = (ContextMenu)this.Resources["extractResultContextMenu"];

            // NegPos coloring
            foreach (ConvolutionResultNegPosColoring coloring in Enum.GetValues(typeof(ConvolutionResultNegPosColoring)))
            {
                cboEdgeColors.Items.Add(coloring);
            }
            cboEdgeColors.SelectedIndex = 0;

            // Working folder
            string foldername = UtilityCore.GetOptionsFolder();
            foldername = System.IO.Path.Combine(foldername, FOLDER);
            Directory.CreateDirectory(foldername);

            _workingFolder = System.IO.Path.Combine(foldername, WORKING + Guid.NewGuid().ToString());
            Directory.CreateDirectory(_workingFolder);

            // Pre filters
            _preFilters = GetPreFilters();

            // Load
            LoadSessionsCombobox();
            TryLoadSession(LATEST);
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

                #region Folders

                if (cboSession.Text != "")
                {
                    SaveSession(cboSession.Text, false);
                }

                SaveSession(LATEST, false);

                if (_workingFolder.StartsWith(UtilityCore.GetOptionsFolder(), StringComparison.OrdinalIgnoreCase))      // it always should start with this, but I'd rather leave a folder laying around than delete a hijacked foldername
                {
                    //_images.Clear();
                    //pnlImages.Items.Clear();

                    Directory.Delete(_workingFolder, true);
                }

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
                SaveSession(cboSession.Text, true);
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
                Tuple<bool, string> result = TryLoadSession(cboSession.Text);
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
                    string filename = tag + " - " + uniqueID + ".png";
                    string fullFilename = System.IO.Path.Combine(_workingFolder, filename);

                    UtilityWPF.SaveBitmapPNG(bitmap, fullFilename);

                    // Build entry
                    FeatureRecognizer_Image entry = new FeatureRecognizer_Image()
                    {
                        Tag = tag,
                        UniqueID = uniqueID,
                        Filename = filename,
                        ImageControl = GetTreeviewImageCtrl(bitmap),
                        Bitmap = bitmap,
                        Token = TokenGenerator.NextToken(),
                    };

                    // Store it
                    AddImage(entry);
                }

                // Update the session file
                SaveSession_SessionFile(_workingFolder);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void pnlImages_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            try
            {
                if (_selectedExtractIndex < 0)
                {
                    return;
                }

                FeatureRecognizer_Image image = GetSelectedImage();
                if (image == null)
                {
                    return;
                }

                ApplyExtract(image, _extracts[_selectedExtractIndex]);
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
                if (_selectedExtractIndex < 0)
                {
                    return;
                }

                FeatureRecognizer_Image image = GetSelectedImage();
                if (image == null)
                {
                    return;
                }

                ApplyExtract(image, _extracts[_selectedExtractIndex]);
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
                foreach (TreeViewItem node in pnlImages.Items)
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
                foreach (TreeViewItem node in pnlImages.Items)
                {
                    node.IsExpanded = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NewExtract_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_images.Count == 0)
                {
                    MessageBox.Show("Add an image first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Pick a random image
                var image = GetSelectedImage();
                if (image == null)
                {
                    image = _images[StaticRandom.Next(_images.Count)];
                }

                #region Prefilter

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
                ConvolutionBase2D filter = null;
                if (prefilterCandidates.Count > 0)
                {
                    filter = prefilterCandidates[StaticRandom.Next(prefilterCandidates.Count)];
                }

                #endregion

                #region Extract convolution

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
                RectInt rect = Convolutions.GetExtractRectangle(finalSize, chkIsSquare.IsChecked.Value);

                List<FeatureRecognizer_Extract_Sub> subs = new List<FeatureRecognizer_Extract_Sub>();

                #endregion

                foreach (ReducedExtract size in GetExtractSizes(image.Bitmap, filter, rect))
                {
                    #region Image and prefilter

                    // Initial image
                    BitmapSource bitmap;
                    if (size.Percent.IsNearValue(1))
                    {
                        bitmap = image.Bitmap;
                    }
                    else
                    {
                        bitmap = UtilityWPF.ResizeImage(image.Bitmap, size.ImageSize.X, size.ImageSize.Y);
                    }

                    // Get convolution
                    Convolution2D imageConv = ((BitmapCustomCachedBytes)UtilityWPF.ConvertToColorArray(bitmap, false, Colors.Transparent)).ToConvolution();

                    if (filter != null)
                    {
                        imageConv = Convolutions.Convolute(imageConv, filter);
                    }

                    #endregion

                    // Extract
                    Convolution2D extractConv = imageConv.Extract(size.Extract, ConvolutionExtractType.EdgeSoftBorder);

                    // Generate results
                    FeatureRecognizer_Extract_Sub sub = BuildExtractResult(imageConv, extractConv);
                    if (sub != null)
                    {
                        subs.Add(sub);
                    }
                }

                // Finish
                FinishBuildingExtract(filter, subs.ToArray(), image.UniqueID);
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
                ImageFilterPainter senderCast = sender as ImageFilterPainter;
                if (senderCast == null)
                {
                    MessageBox.Show("Expected sender to be the painter window", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                FeatureRecognizer_Extract origExtract = senderCast.Tag as FeatureRecognizer_Extract;
                if (origExtract == null)
                {
                    MessageBox.Show("Expected the painter to contain the original extract that the convolution came from", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                BuildChangedExtract(origExtract, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void panelExtracts_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.ChangedButton != MouseButton.Left)
                {
                    return;
                }

                Tuple<Border, int> clickedCtrl = GetSelectedExtract(e.OriginalSource);
                if (clickedCtrl == null)
                {
                    return;
                }

                SelectExtract(clickedCtrl.Item2);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExtractView_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Tuple<Border, int> selectedCtrl = GetSelectedExtract(_extractContextMenu.PlacementTarget);
                if (selectedCtrl == null)
                {
                    MessageBox.Show("Couldn't identify extract", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ViewConvolution((Convolution2D)_extracts[selectedCtrl.Item2].Extracts[0].Extract);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ExtractEdit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Tuple<Border, int> selectedCtrl = GetSelectedExtract(_extractContextMenu.PlacementTarget);
                if (selectedCtrl == null)
                {
                    MessageBox.Show("Couldn't identify extract", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                EditConvolution(_extracts[selectedCtrl.Item2]);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void RemoveSection_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Tuple<Border, int> selectedCtrl = GetSelectedExtract(_extractContextMenu.PlacementTarget);
                if (selectedCtrl == null)
                {
                    MessageBox.Show("Couldn't identify extract", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                FeatureRecognizer_Extract origExtract = _extracts[selectedCtrl.Item2];

                Convolution2D conv = RemoveSection(origExtract.Extracts[0].Extract);

                BuildChangedExtract(origExtract, conv);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ExtractDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Tuple<Border, int> selectedCtrl = GetSelectedExtract(_extractContextMenu.PlacementTarget);
                if (selectedCtrl == null)
                {
                    MessageBox.Show("Couldn't identify extract", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                panelExtracts.Children.Remove(selectedCtrl.Item1);
                _extracts.RemoveAt(selectedCtrl.Item2);

                if (_selectedExtractIndex == selectedCtrl.Item2)
                {
                    _selectedExtractIndex = -1;
                }
                else if (_selectedExtractIndex > selectedCtrl.Item2)
                {
                    _selectedExtractIndex--;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExtractResultView_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Border selectedCtrl = GetClickedConvolution(_extractResultContextMenu.PlacementTarget);
                if (selectedCtrl == null)
                {
                    MessageBox.Show("Couldn't find selected convolution", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Convolution2D convolution = selectedCtrl.Tag as Convolution2D;
                if (convolution == null)
                {
                    MessageBox.Show("Control.Tag doesn't contain the convolution", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ViewConvolution(convolution);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private void ClearEverything()
        {
            if (Directory.Exists(_workingFolder))
            {
                foreach (string name in Directory.GetFiles(_workingFolder))
                {
                    File.Delete(name);
                }
            }
            else
            {
                Directory.CreateDirectory(_workingFolder);
            }

            // Images
            _images.Clear();
            pnlImages.Items.Clear();
            cboImageLabel.Items.Clear();

            // Extracts
            _extracts.Clear();
            panelExtracts.Children.Clear();
            pnlExtractResults.Children.Clear();
        }

        private void SaveSession(string subfolderName, bool shouldRebuildSessionsCombobox)
        {
            //C:\Users\<username>\AppData\Roaming\Asteroid Miner\FeatureRecognizer\
            string foldername = UtilityCore.GetOptionsFolder();
            foldername = System.IO.Path.Combine(foldername, FOLDER);
            Directory.CreateDirectory(foldername);

            // Subfolder
            foldername = System.IO.Path.Combine(foldername, subfolderName);
            if (Directory.Exists(foldername))
            {
                foreach (string name in Directory.GetFiles(foldername))
                {
                    File.Delete(name);
                }
            }
            else
            {
                Directory.CreateDirectory(foldername);
            }

            // Images
            foreach (var image in _images)
            {
                // Copy from working
                string fromName = System.IO.Path.Combine(_workingFolder, image.Filename);
                string toName = System.IO.Path.Combine(foldername, image.Filename);

                if (File.Exists(fromName))
                {
                    File.Copy(fromName, toName);
                }
                else if (image.Bitmap != null)
                {
                    UtilityWPF.SaveBitmapPNG(image.Bitmap, toName);
                }
            }

            // Extracts
            foreach (var extract in _extracts)
            {
                // Copy from working
                string fromName = System.IO.Path.Combine(_workingFolder, extract.Filename);
                string toName = System.IO.Path.Combine(foldername, extract.Filename);

                if (File.Exists(fromName))
                {
                    File.Copy(fromName, toName);
                }
                else
                {
                    UtilityCore.SerializeToFile(toName, extract);
                }
            }

            // Session
            SaveSession_SessionFile(foldername);

            // Combobox
            if (shouldRebuildSessionsCombobox)
            {
                LoadSessionsCombobox();
            }
        }
        private void SaveSession_SessionFile(string foldername)
        {
            FeatureRecognizer_Session session = new FeatureRecognizer_Session()
            {
                SessionName = cboSession.Text,
                Images = _images.ToArray(),
                //Extracts = _extracts.ToArray(),
                ExtractFilenames = _extracts.Select(o => o.Filename).ToArray(),
            };

            string filename = System.IO.Path.Combine(foldername, SESSION);
            UtilityCore.SerializeToFile(filename, session);
        }

        private Tuple<bool, string> TryLoadSession(string subfolderName)
        {
            #region determine folders

            string workingSub = System.IO.Path.GetFileName(_workingFolder);
            if (subfolderName.Equals(workingSub, StringComparison.OrdinalIgnoreCase))
            {
                return Tuple.Create(false, "Can't load the current working folder");
            }

            string foldername = UtilityCore.GetOptionsFolder();
            foldername = System.IO.Path.Combine(foldername, FOLDER);
            foldername = System.IO.Path.Combine(foldername, subfolderName);

            if (!Directory.Exists(foldername))
            {
                return Tuple.Create(false, "Folder doesn't exist");
            }

            #endregion

            ClearEverything();

            try
            {
                #region copy files

                foreach (string fromFilename in Directory.GetFiles(foldername))
                {
                    string toFilename = System.IO.Path.Combine(_workingFolder, System.IO.Path.GetFileName(fromFilename));
                    File.Copy(fromFilename, toFilename);
                }

                #endregion

                #region session

                string filename = System.IO.Path.Combine(foldername, SESSION);
                FeatureRecognizer_Session session = UtilityCore.DeserializeFromFile<FeatureRecognizer_Session>(filename);

                LoadSessionsCombobox();

                cboSession.Text = session.SessionName;

                #endregion

                #region images

                foreach (var image in session.Images)
                {
                    // These two properties weren't serialized, load them now
                    image.Bitmap = UtilityWPF.GetBitmap(System.IO.Path.Combine(_workingFolder, image.Filename));

                    image.ImageControl = GetTreeviewImageCtrl(image.Bitmap);

                    AddImage(image);
                }

                #endregion

                #region extracts

                foreach (string extractName in session.ExtractFilenames)
                {
                    filename = System.IO.Path.Combine(_workingFolder, extractName);

                    FeatureRecognizer_Extract extract = UtilityCore.DeserializeFromFile<FeatureRecognizer_Extract>(filename);

                    if (extract.PreFilterDNA_Single != null)
                    {
                        extract.PreFilter = new Convolution2D(extract.PreFilterDNA_Single);
                    }
                    else if (extract.PreFilterDNA_Set != null)
                    {
                        extract.PreFilter = new ConvolutionSet2D(extract.PreFilterDNA_Set);
                    }

                    foreach (var sub in extract.Extracts)
                    {
                        sub.Extract = new Convolution2D(sub.ExtractDNA);

                        foreach (var result in sub.Results)
                        {
                            result.Result = new Convolution2D(result.ResultDNA);
                        }
                    }

                    extract.Control = Convolutions.GetKernelThumbnail(extract.Extracts[0].Extract, THUMBSIZE_EXTRACT, _extractContextMenu);

                    AddExtract(extract);
                }

                #endregion

                return Tuple.Create(true, "");
            }
            catch (Exception ex)
            {
                ClearEverything();
                return Tuple.Create(false, ex.ToString());
            }
        }

        private void BuildChangedExtract(FeatureRecognizer_Extract origExtract, Convolution2D newConv)
        {
            var image = _images.FirstOrDefault(o => o.UniqueID == origExtract.ImageID);
            if (image == null)
            {
                throw new ApplicationException("Couldn't find the image that the original extract references");
            }

            // Image convolution
            Convolution2D imageConv = ((BitmapCustomCachedBytes)UtilityWPF.ConvertToColorArray(image.Bitmap, false, Colors.Transparent)).ToConvolution();
            if (origExtract.PreFilter != null)
            {
                imageConv = Convolutions.Convolute(imageConv, origExtract.PreFilter);
            }

            //NOTE: The original extract contains multiple sizes (reductions of the largest one).  But since this convolution is derived from
            //the largest extract, it would be difficult to reduce.  So only keeping the highest resolution image
            FeatureRecognizer_Extract_Sub resultPatch = BuildExtractResult(imageConv, newConv);

            FinishBuildingExtract(origExtract.PreFilter, new[] { resultPatch }, origExtract.ImageID);
        }
        private void FinishBuildingExtract(ConvolutionBase2D filter, FeatureRecognizer_Extract_Sub[] subs, string imageID)
        {
            string uniqueID = Guid.NewGuid().ToString();

            // Determine filename
            string filename = "extract - " + uniqueID + ".xml";
            string fullFilename = System.IO.Path.Combine(_workingFolder, filename);

            // Add it
            FeatureRecognizer_Extract extract = new FeatureRecognizer_Extract()
            {
                Extracts = subs,
                PreFilter = filter,
                Control = Convolutions.GetKernelThumbnail(subs[0].Extract, THUMBSIZE_EXTRACT, _extractContextMenu),
                ImageID = imageID,
                UniqueID = uniqueID,
                Filename = filename,
            };

            if (extract.PreFilter != null && extract.PreFilter is Convolution2D)
            {
                extract.PreFilterDNA_Single = ((Convolution2D)extract.PreFilter).ToDNA();
            }
            else if (extract.PreFilter != null && extract.PreFilter is ConvolutionSet2D)
            {
                extract.PreFilterDNA_Set = ((ConvolutionSet2D)extract.PreFilter).ToDNA();
            }

            // Copy to the working folder
            UtilityCore.SerializeToFile(fullFilename, extract);

            AddExtract(extract);

            // Update the session file
            SaveSession_SessionFile(_workingFolder);
        }

        private void AddImage(FeatureRecognizer_Image image)
        {
            _images.Add(image);

            List<string> tags = new List<string>();

            // Find existing name node
            TreeViewItem nameNode = null;
            foreach (TreeViewItem node in pnlImages.Items)
            {
                string header = (string)node.Header;
                tags.Add(header);

                if (header.Equals(image.Tag, StringComparison.OrdinalIgnoreCase))
                {
                    nameNode = node;
                    break;
                }
            }

            if (nameNode == null)
            {
                // Create new
                nameNode = new TreeViewItem() { Header = image.Tag };

                // Store this alphabetically
                int insertIndex = UtilityCore.GetInsertIndex(tags, image.Tag);
                pnlImages.Items.Insert(insertIndex, nameNode);
            }

            // Add the image control to this name node
            nameNode.Items.Add(image.ImageControl);

            nameNode.IsExpanded = true;

            #region Update tag combobox

            string currentText = cboImageLabel.Text;
            cboImageLabel.Items.Clear();

            foreach (string comboItem in _images.Select(o => o.Tag).Distinct().OrderBy(o => o))
            {
                cboImageLabel.Items.Add(comboItem);
            }

            cboImageLabel.Text = currentText;

            #endregion
        }
        private void AddExtract(FeatureRecognizer_Extract extract)
        {
            panelExtracts.Children.Add(extract.Control);
            _extracts.Add(extract);
        }

        private void LoadSessionsCombobox()
        {
            string foldername = UtilityCore.GetOptionsFolder();
            foldername = System.IO.Path.Combine(foldername, FOLDER);

            cboSession.Items.Clear();
            cboSession.Text = "";

            if (!Directory.Exists(foldername))
            {
                return;
            }

            string[] names = Directory.GetDirectories(foldername).
                Select(o => System.IO.Path.GetFileName(o)).
                Where(o => o != LATEST && !o.StartsWith(WORKING, StringComparison.OrdinalIgnoreCase)).
                OrderBy(o => o).
                ToArray();

            foreach (string name in names)
            {
                cboSession.Items.Add(name);
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
        private void EditConvolution(FeatureRecognizer_Extract extract)
        {
            ImageFilterPainter viewer = new ImageFilterPainter();

            viewer.Closed += Child_Closed;
            viewer.SaveRequested += Painter_SaveRequested;

            _childWindows.Add(viewer);

            viewer.Tag = extract;
            viewer.EditKernel((Convolution2D)extract.Extracts[0].Extract);      // give it the highest resolution one

            viewer.Show();
        }

        private FeatureRecognizer_Image GetSelectedImage()
        {
            Image selected = pnlImages.SelectedItem as Image;
            if (selected == null)
            {
                return null;
            }

            return _images.FirstOrDefault(o => o.ImageControl == selected);
        }
        private Tuple<Border, int> GetSelectedExtract(object source)
        {
            Border clickedCtrl = GetClickedConvolution(source);

            // Get the index
            int index = panelExtracts.Children.IndexOf(clickedCtrl);
            if (index < 0)
            {
                throw new ApplicationException("Couldn't find clicked item");
            }

            if (_extracts.Count != panelExtracts.Children.Count)
            {
                throw new ApplicationException("_extracts and panelExtracts are out of sync");
            }

            return Tuple.Create(clickedCtrl, index);
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

        private void SelectExtract(int index)
        {
            // Set the effect
            int childIndex = 0;
            foreach (UIElement child in panelExtracts.Children)
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

            _selectedExtractIndex = index;

            // Choose an image
            FeatureRecognizer_Image image = GetSelectedImage();
            if (image == null)
            {
                image = _images[StaticRandom.Next(_images.Count - 1)];
            }

            // Apply this kernel to the original image
            ApplyExtract(image, _extracts[_selectedExtractIndex]);
        }

        private void ApplyExtract(FeatureRecognizer_Image image, FeatureRecognizer_Extract extract)
        {
            pnlExtractResults.Children.Clear();

            ConvolutionResultNegPosColoring edgeColor = (ConvolutionResultNegPosColoring)cboEdgeColors.SelectedValue;

            foreach (var sub in extract.Extracts)
            {
                // Create a panel that will hold the result
                Grid grid = new Grid();
                if (pnlExtractResults.Children.Count > 0)
                {
                    grid.Margin = new Thickness(0, 10, 0, 0);
                }

                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Pixel) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(5, GridUnitType.Pixel) });
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });

                pnlExtractResults.Children.Add(grid);

                // Do the work in another thread
                ApplyExtract_DoIt(grid, image, extract, sub, edgeColor, _extractResultContextMenu);
            }
        }
        private async static void ApplyExtract_DoIt(Grid grid, FeatureRecognizer_Image image, FeatureRecognizer_Extract extract, FeatureRecognizer_Extract_Sub sub, ConvolutionResultNegPosColoring edgeColor, ContextMenu contextMenu)
        {
            // Source image
            BitmapSource bitmap;
            if (sub.InputWidth == image.Bitmap.PixelWidth && sub.InputHeight == image.Bitmap.PixelHeight)
            {
                bitmap = image.Bitmap;
            }
            else
            {
                bitmap = UtilityWPF.ResizeImage(image.Bitmap, sub.InputWidth, sub.InputHeight);
            }

            Convolution2D imageConv = ((BitmapCustomCachedBytes)UtilityWPF.ConvertToColorArray(bitmap, false, Colors.Transparent)).ToConvolution();

            // Convolute, look for matches
            var results = await ApplyExtract_DoIt_Task(imageConv, extract, sub);

            #region Show results

            // Left Image
            ApplyExtract_Draw_LeftImage(grid, results.ImageConv, extract.PreFilter, edgeColor);

            // Right Image
            if (results.Filtered != null)
            {
                ApplyExtract_Draw_RightImage(grid, results.Filtered, edgeColor);
            }

            // Matches
            if (results.Matches != null && results.Matches.Length > 0)
            {
                ApplyExtract_Draw_Matches(grid, results.Matches, results.Filtered.Size, sub, contextMenu);
            }

            #endregion
        }

        private static void ApplyExtract_Draw_LeftImage(Grid grid, Convolution2D imageConv, ConvolutionBase2D preFilter, ConvolutionResultNegPosColoring edgeColor)
        {
            bool isSourceNegPos = preFilter == null ? false : preFilter.IsNegPos;

            Image image = new Image()
            {
                Source = Convolutions.ShowConvolutionResult(imageConv, isSourceNegPos, edgeColor),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                ToolTip = string.Format("{0}x{1}", imageConv.Width, imageConv.Height),
            };

            Grid.SetColumn(image, 0);
            Grid.SetRow(image, 0);
            grid.Children.Add(image);
        }
        private static void ApplyExtract_Draw_RightImage(Grid grid, Convolution2D imageConv, ConvolutionResultNegPosColoring edgeColor)
        {
            Image image = new Image()
            {
                Source = Convolutions.ShowConvolutionResult(imageConv, true, edgeColor),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                ToolTip = string.Format("{0}x{1}", imageConv.Width, imageConv.Height),
            };

            Grid.SetColumn(image, 2);
            Grid.SetRow(image, 0);
            grid.Children.Add(image);
        }
        private static void ApplyExtract_Draw_Matches(Grid grid, Tuple<VectorInt, ApplyResult_Match[]>[] matches, VectorInt imageSize, FeatureRecognizer_Extract_Sub sub, ContextMenu contextMenu)
        {
            const int HITSSIZE_SMALL = 50;
            const int HITSSIZE_BIG = 70;

            StackPanel mainPanel = new StackPanel();

            Grid headerRow = new Grid();
            mainPanel.Children.Add(headerRow);

            #region Draw all hit positions

            var hits = matches.Select(o =>
                {
                    double weight = 0;
                    if (o.Item2 != null && o.Item2.Length > 0)
                    {
                        weight = o.Item2.Max(p => p.Weight);
                    }

                    return new Tuple<VectorInt, double>(o.Item1, weight);
                });

            Border hitsMap = GetResultPositionsImage(hits, imageSize, HITSSIZE_BIG);

            hitsMap.HorizontalAlignment = HorizontalAlignment.Left;
            hitsMap.VerticalAlignment = VerticalAlignment.Bottom;

            headerRow.Children.Add(hitsMap);

            #endregion
            #region Compare extracts

            Grid comparesGrid = new Grid();
            comparesGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
            comparesGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });

            comparesGrid.HorizontalAlignment = HorizontalAlignment.Right;
            comparesGrid.VerticalAlignment = VerticalAlignment.Bottom;

            headerRow.Children.Add(comparesGrid);

            // Label
            TextBlock comparesHeading = new TextBlock() { Text = "compare to", ToolTip = "These are results from the original extract, and can be used to compare the current image's extracts against", HorizontalAlignment = HorizontalAlignment.Center };

            Grid.SetRow(comparesHeading, 0);
            comparesGrid.Children.Add(comparesHeading);

            // Extracts
            StackPanel headerExtracts = new StackPanel()
            {
                Orientation = Orientation.Horizontal,
            };

            foreach (Convolution2D compareResult in sub.Results.Select(o => o.Result))
            {
                headerExtracts.Children.Add(Convolutions.GetKernelThumbnail(compareResult, THUMBSIZE_EXTRACT, contextMenu));
            }

            Grid.SetRow(headerExtracts, 1);
            comparesGrid.Children.Add(headerExtracts);

            #endregion

            foreach (var match in matches)
            {
                //TODO: Make a grid
                StackPanel row = new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(2),
                };

                #region Position

                StackPanel positionPanel = new StackPanel()
                {
                    VerticalAlignment = VerticalAlignment.Center,
                };

                row.Children.Add(positionPanel);

                // Point map
                Border pointMap = GetResultPositionsImage(new[] { Tuple.Create(match.Item1, 1d) }, imageSize, HITSSIZE_SMALL);
                pointMap.HorizontalAlignment = HorizontalAlignment.Left;
                positionPanel.Children.Add(pointMap);

                // Coordinates
                positionPanel.Children.Add(new TextBlock() { Text = match.Item1.ToString(), HorizontalAlignment = HorizontalAlignment.Center });

                #endregion

                foreach (var patch in match.Item2)
                {
                    #region Match Patch

                    StackPanel patchPanel = new StackPanel();
                    row.Children.Add(patchPanel);

                    // Show convolution
                    patchPanel.Children.Add(Convolutions.GetKernelThumbnail(patch.Patch, THUMBSIZE_EXTRACT, contextMenu));

                    // Weight
                    patchPanel.Children.Add(new TextBlock()
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Text = patch.IsMatch ? patch.Weight.ToStringSignificantDigits(2) : "no match",
                    });

                    #endregion
                }

                mainPanel.Children.Add(row);
            }

            #region Return border

            Border border = new Border()
            {
                BorderBrush = new SolidColorBrush(UtilityWPF.ColorFromHex("40000000")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(4),
                Background = new SolidColorBrush(UtilityWPF.ColorFromHex("10000000")),
                Child = mainPanel,
            };

            Grid.SetColumn(border, 0);
            Grid.SetColumnSpan(border, 3);
            Grid.SetRow(border, 2);
            grid.Children.Add(border);

            #endregion
        }

        private static Image GetTreeviewImageCtrl(BitmapSource bitmap)
        {
            return new Image()
            {
                Stretch = Stretch.Fill,
                Width = THUMBSIZE_IMAGE,
                Height = THUMBSIZE_IMAGE,
                Source = bitmap,
                Margin = new Thickness(3),
                ToolTip = string.Format("{0}x{1}", bitmap.PixelWidth, bitmap.PixelHeight)
            };
        }

        private static Border GetResultPositionsImage(IEnumerable<Tuple<VectorInt, double>> hits, VectorInt patchSize, int returnSize)
        {
            const int SIZE = 150;
            const double RADIUS_MAX = SIZE * .1;
            const double RADIUS_MIN = RADIUS_MAX * .25;
            const double MINOPACITY = .1;

            #region Bitmap

            RenderTargetBitmap bitmap = new RenderTargetBitmap(SIZE, SIZE, UtilityWPF.DPI, UtilityWPF.DPI, PixelFormats.Pbgra32);

            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext ctx = dv.RenderOpen())
            {
                foreach (var hit in hits)
                {
                    Point center = new Point()
                    {
                        X = UtilityCore.GetScaledValue(0, SIZE, 0, patchSize.X, hit.Item1.X),
                        Y = UtilityCore.GetScaledValue(0, SIZE, 0, patchSize.Y, hit.Item1.Y),
                    };

                    double radius = UtilityCore.GetScaledValue(RADIUS_MIN, RADIUS_MAX, 0, 1, hit.Item2);
                    double alphaPercent = UtilityCore.GetScaledValue(MINOPACITY, 1, 0, 1, hit.Item2);
                    Color color = UtilityWPF.AlphaBlend(Colors.Black, Colors.Transparent, alphaPercent);

                    ctx.DrawEllipse(new SolidColorBrush(color), null, center, radius, radius);
                }
            }

            bitmap.Render(dv);

            #endregion
            #region Image/Border

            Image image = new Image()
            {
                Source = bitmap,
                Width = returnSize,
                Height = returnSize,
            };

            Border border = new Border()
            {
                Child = image,
                BorderBrush = new SolidColorBrush(UtilityWPF.ColorFromHex("A0A0A0")),
                BorderThickness = new Thickness(1),
                Background = Brushes.White,
                Margin = new Thickness(2),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };

            #endregion

            return border;
        }

        #endregion
        #region Private Methods - pure workers

        private static Task<ApplyResult> ApplyExtract_DoIt_Task(Convolution2D imageConv, FeatureRecognizer_Extract extract, FeatureRecognizer_Extract_Sub sub)
        {
            return Task.Run(() =>
            {
                VectorInt totalReduce = sub.Extract.GetReduction();

                Convolution2D finalImage = imageConv;
                if (extract.PreFilter != null)
                {
                    finalImage = Convolutions.Convolute(imageConv, extract.PreFilter);      //TODO: The final worker method shouldn't do this if too small.  I'm just doing it to show the user something
                    totalReduce += extract.PreFilter.GetReduction();
                }

                if (imageConv.Width <= totalReduce.X || imageConv.Height <= totalReduce.Y)
                {
                    // Too small, exit early
                    return new ApplyResult(finalImage, null, null);
                }

                // Apply convolutions
                Convolution2D filtered = Convolutions.Convolute(finalImage, sub.Extract);

                // Look at the brightest spots, and see if they are matches
                var matches = AnalyzeBrightSpots(filtered, sub.Results);

                return new ApplyResult(finalImage, filtered, matches);
            });
        }

        /// <summary>
        /// This applies the extract to the image, and returns some result patches of the brightest hit
        /// </summary>
        /// <param name="image">This is the image after filter is applied</param>
        /// <param name="extract">This is the extract to apply to the image</param>
        private static FeatureRecognizer_Extract_Sub BuildExtractResult(Convolution2D image, Convolution2D extract)
        {
            // Apply the extract
            Convolution2D applied = Convolutions.Convolute(image, extract);

            Tuple<VectorInt, double> brightestPoint = applied.GetMax();

            // Sometimes the extract is small compared to the image, sometimes it's nearly the same size.  As the extract rect
            // gets bigger, the result conv gets smaller
            VectorInt analyzeSize = new VectorInt()
            {
                X = Math.Min(applied.Width, extract.Width),
                Y = Math.Min(applied.Height, extract.Height),
            };

            VectorInt[] sizes = new[]
            {
                analyzeSize / 2d,
                analyzeSize / 4d,
                analyzeSize / 8d,
            };

            // Now extract the area around the brightest point.  This will be the ideal
            var results = sizes.
                Select(o => new { AnalyzeSize = o, Rectangle = GetResultRect(applied.Size, brightestPoint.Item1, o) }).
                Where(o => o.Rectangle != null).
                Select(o =>
                {
                    var convolution = applied.Extract(o.Rectangle.Item1, ConvolutionExtractType.Raw);
                    double score = Convolutions.GetExtractScore(convolution, brightestPoint.Item1 - o.Rectangle.Item1.Position, brightestPoint.Item2);

                    return new { o.AnalyzeSize, o.Rectangle, Convolution = convolution, Score = score };
                }).
                Where(o => o.Score > 0).
                ToArray();

            if (results.Length == 0)
            {
                return null;
            }

            // Exit Function
            return new FeatureRecognizer_Extract_Sub()
            {
                InputWidth = image.Width,
                InputHeight = image.Height,

                Extract = extract,
                ExtractDNA = extract.ToDNA(),

                Results = results.Select(o =>
                    new FeatureRecognizer_Extract_Result()
                    {
                        Offset = o.Rectangle.Item2,
                        Rectangle = o.Rectangle.Item1,
                        BrightestValue = brightestPoint.Item2,
                        Score = o.Score,
                        Result = o.Convolution,
                        ResultDNA = o.Convolution.ToDNA(),
                    }).
                    ToArray(),
            };
        }

        /// <summary>
        /// Find the brightest spot, then find the next brightest spot that is outside the result rectangle, etc.
        /// Stop when that value is less than some % of original
        /// </summary>
        private static Tuple<VectorInt, ApplyResult_Match[]>[] AnalyzeBrightSpots(Convolution2D image, FeatureRecognizer_Extract_Result[] comparePatches)
        {
            var retVal = new List<Tuple<VectorInt, ApplyResult_Match[]>>();

            List<RectInt> previous = new List<RectInt>();

            // The compare patches are sorted descending by size, so the largest patch will also be sure to contain the brightest value (the
            // brightest value should be the same for all, because they are centered on it)
            double minBrightness = comparePatches[0].BrightestValue * .33;
            double minWeight = comparePatches[0].Score * .5;

            while (true)
            {
                // Find the brightest point in the image passed in
                var brightest = image.GetMax(previous);
                if (brightest == null || brightest.Item2 < minBrightness)
                {
                    break;
                }

                List<ApplyResult_Match> patches = new List<ApplyResult_Match>();

                foreach (FeatureRecognizer_Extract_Result comparePatch in comparePatches)
                {
                    RectInt patchRect = new RectInt(brightest.Item1 + comparePatch.Offset, comparePatch.Result.Size);
                    patchRect = RectInt.Intersect(patchRect, new RectInt(0, 0, image.Width, image.Height)).Value;

                    previous.Add(patchRect);

                    Convolution2D patchConv = image.Extract(patchRect, ConvolutionExtractType.Raw);

                    VectorInt brightestPos = brightest.Item1 - patchRect.Position;

                    // Initial check (pure algorithm)
                    double weight1 = Convolutions.GetExtractScore(patchConv, brightestPos, comparePatch.BrightestValue);        // this method should be pure algorithm
                    if (weight1 < minWeight)
                    {
                        patches.Add(new ApplyResult_Match(false, weight1, patchConv));
                        continue;
                    }

                    patches.Add(new ApplyResult_Match(true, weight1, patchConv));

                    // Second check (compare with the original patch)
                    //double weight2 = Convolutions.GetExtractScore(patchConv, brightestPos, comparePatch.Result, -comparePatch.Offset, comparePatch.BrightestValue);       // this method should subtract the two patches and compare the difference

                    //patches.Add(new ApplyResult_Match(true, weight2, patchConv));
                }

                retVal.Add(Tuple.Create(brightest.Item1, patches.ToArray()));
            }

            return retVal.ToArray();
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

        private static ReducedExtract[] GetExtractSizes(BitmapSource bitmap, ConvolutionBase2D filter, RectInt rect, int minExtractSize = 10)
        {
            List<ReducedExtract> retVal = new List<ReducedExtract>();

            VectorInt filterReduce = filter == null ? new VectorInt(0, 0) : filter.GetReduction();

            Rect percents = new Rect()
            {
                X = rect.X.ToDouble() / bitmap.PixelWidth.ToDouble(),
                Y = rect.Y.ToDouble() / bitmap.PixelHeight.ToDouble(),
                Width = rect.Width.ToDouble() / bitmap.PixelWidth.ToDouble(),
                Height = rect.Height.ToDouble() / bitmap.PixelHeight.ToDouble(),
            };

            double percent = 1d;

            while (true)
            {
                VectorInt imageSize = new VectorInt()
                {
                    X = (bitmap.PixelWidth * percent).ToInt_Round(),
                    Y = (bitmap.PixelHeight * percent).ToInt_Round(),
                };

                VectorInt postSize = imageSize - filterReduce;

                RectInt newRect = new RectInt()
                {
                    X = (percents.X * postSize.X).ToInt_Round(),
                    Y = (percents.Y * postSize.Y).ToInt_Round(),
                    Width = (percents.Width * postSize.X).ToInt_Round(),
                    Height = (percents.Height * postSize.Y).ToInt_Round(),
                };

                if (newRect.Width < minExtractSize || newRect.Height < minExtractSize)
                {
                    break;
                }

                retVal.Add(new ReducedExtract()
                {
                    Percent = percent,
                    ImageSize = imageSize,
                    Extract = newRect,
                });

                percent *= .75;
            }

            return retVal.ToArray();
        }

        private static Tuple<RectInt, VectorInt> GetResultRect(VectorInt imageSize, VectorInt center, VectorInt size)
        {
            if (Convolutions.IsTooSmall(imageSize) || Convolutions.IsTooSmall(size))
            {
                return null;
            }

            VectorInt offset = -(size / 2);
            VectorInt positionInitial = center + offset;

            RectInt? rect = RectInt.Intersect(new RectInt(positionInitial, size), new RectInt(0, 0, imageSize.X, imageSize.Y));
            if (rect == null)
            {
                throw new ArgumentException("No rectangle found");
            }

            if (rect.Value.X > positionInitial.X)
            {
                offset.X += rect.Value.X - positionInitial.X;
            }

            if (rect.Value.Y > positionInitial.Y)
            {
                offset.Y += rect.Value.Y - positionInitial.Y;
            }

            return Tuple.Create(rect.Value, offset);
        }

        #endregion

        private static Convolution2D RemoveSection(Convolution2D conv)
        {
            //Convolution2D mask = GetMask_ScatterShot(conv.Width, conv.Height);
            Convolution2D mask = GetMask_Ellipse(conv.Width, conv.Height, StaticRandom.Next(1, 4));

            double[] values = new double[conv.Values.Length];

            for (int cntr = 0; cntr < values.Length; cntr++)
            {
                values[cntr] = conv.Values[cntr] * mask.Values[cntr];
            }

            values = Convolutions.ToUnit(values);

            return new Convolution2D(values, conv.Width, conv.Height, conv.IsNegPos, conv.Gain, conv.Iterations, conv.ExpandBorder);
        }

        /// <summary>
        /// This returns a 0 to 1 convolution.  This should be used as an opacity mask, multiply another convolution's values
        /// by each of mask's values
        /// </summary>
        /// <remarks>
        /// TODO: Take in settings to fine tune the shapes
        /// </remarks>
        private static Convolution2D GetMask_ScatterShot(int width, int height, double percentKeep = .25)
        {
            //TODO: Create a white bitmap and draw random semitransparent black shapes on it

            Random rand = StaticRandom.GetRandomForThread();

            double[] values = Enumerable.Range(0, width * height).
                Select(o => rand.NextDouble() < percentKeep ? 1d : rand.NextDouble()).
                ToArray();

            return new Convolution2D(values, width, height, false);
        }
        private static Convolution2D GetMask_Ellipse(int width, int height, int count = 1)
        {
            const double POWER = 2d;        // this gives larger probability of smaller values
            const double MINPERCENT = .05;
            const double MAXPERCENT = .8d;
            const double MARGIN = .3;
            const double MAXCENTERPERCENT = 1.2;
            const double MAXASPECT = Math3D.GOLDENRATIO * 3d;

            Point center = new Point(width / 2d, height / 2d);

            double avgSize = Math3D.Avg(width, height);
            double minRadius = avgSize * MINPERCENT;
            double maxRadius = avgSize * MAXPERCENT;

            double marginX = width * MARGIN;
            double marginY = height * MARGIN;

            Random rand = StaticRandom.GetRandomForThread();

            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext ctx = dv.RenderOpen())
            {
                for (int cntr = 0; cntr < count; cntr++)
                {
                    #region Position/Radius

                    Point position = new Point();
                    double radiusX = 0d;
                    double radiusY = 0d;

                    // Keep trying until the ellipse is over the image
                    while (true)
                    {
                        position = center + new Vector(Math3D.GetNearZeroValue(center.X * MAXCENTERPERCENT), Math3D.GetNearZeroValue(center.Y * MAXCENTERPERCENT));

                        radiusX = UtilityCore.GetScaledValue(minRadius, maxRadius, 0, 1, rand.NextPow(POWER, isPlusMinus: false));
                        radiusY = UtilityCore.GetScaledValue(minRadius, maxRadius, 0, 1, rand.NextPow(POWER, isPlusMinus: false));

                        if(radiusX > radiusY)
                        {
                            double aspect = radiusX / radiusY;
                            if(aspect > MAXASPECT)
                            {
                                radiusY = radiusX / MAXASPECT;
                            }
                        }
                        else
                        {
                            double aspect = radiusY / radiusX;
                            if (aspect > MAXASPECT)
                            {
                                radiusX = radiusY / MAXASPECT;
                            }
                        }

                        if (position.X + radiusX < marginX)
                        {
                            continue;
                        }
                        else if (position.X - radiusX > width - marginX)
                        {
                            continue;
                        }
                        else if (position.Y + radiusY < marginY)
                        {
                            continue;
                        }
                        else if (position.Y - radiusY > height - marginY)
                        {
                            continue;
                        }

                        break;
                    }

                    #endregion
                    #region Opacity - FAIL

                    //TODO: Instead of subtracting from 255, just change the inputs to the rand statements

                    ////double fromTransparency = rand.NextDouble(.5);
                    ////double toTransparency = rand.NextDouble(.7, 1.1);       // going a bit beyond 1 to give a higher chance of transparent
                    ////double midTransparency = rand.NextDouble(fromTransparency, toTransparency);

                    ////double fromTransparency = rand.NextDouble(.1);
                    ////double toTransparency = rand.NextDouble(.3, 1.1);       // going a bit beyond 1 to give a higher chance of transparent
                    ////double midTransparency = rand.NextDouble(fromTransparency, toTransparency);

                    //double fromTransparency = rand.NextDouble(.3);
                    //double toTransparency = rand.NextDouble(.85, 1.1);       // going a bit beyond 1 to give a higher chance of transparent
                    //double midTransparency = rand.NextDouble(fromTransparency, toTransparency);

                    //byte fromAlpha = Convert.ToByte(255 - (fromTransparency * 255).ToInt_Round());
                    //byte midAlpha = Convert.ToByte(255 - (midTransparency * 255).ToInt_Round());
                    //byte toAlpha = toTransparency > 1d ? (byte)0 : Convert.ToByte(255 - (toTransparency * 255).ToInt_Round());

                    ////double fromOffset = rand.NextDouble(0, .08);
                    ////double toOffset = rand.NextDouble(.8, 1.5);
                    ////double midOffset = rand.NextDouble(.1, .75);

                    //double fromOffset = rand.NextDouble(0, .4);
                    //double toOffset = rand.NextDouble(.95, 1.05);
                    //double midOffset = rand.NextDouble(fromOffset, toOffset);

                    //RadialGradientBrush brush = new RadialGradientBrush()
                    //{
                    //    //GradientOrigin = new Point(.5, .5),
                    //    RadiusX = radiusY,
                    //    RadiusY = radiusY,
                    //};

                    ////fromAlpha = (byte)255;
                    ////midAlpha = (byte)128;
                    ////toAlpha = (byte)0;

                    //fromAlpha = (byte)0;
                    //midAlpha = (byte)255;
                    //toAlpha = (byte)0;

                    //fromOffset = 0;
                    //midOffset = .5;
                    //toOffset = 1;


                    //brush.GradientStops.Add(new GradientStop(Color.FromArgb(fromAlpha, 255, 255, 255), fromOffset));
                    //brush.GradientStops.Add(new GradientStop(Color.FromArgb(midAlpha, 255, 255, 255), midOffset));
                    //brush.GradientStops.Add(new GradientStop(Color.FromArgb(toAlpha, 255, 255, 255), toOffset));

                    ////<RadialGradientBrush>
                    ////    <RadialGradientBrush.GradientStops>
                    ////        <GradientStop Offset="0" Color="#FF000000"/>
                    ////        <GradientStop Offset=".7" Color="#80000000"/>
                    ////        <GradientStop Offset="1" Color="#00000000"/>
                    ////    </RadialGradientBrush.GradientStops>
                    ////</RadialGradientBrush>

                    #endregion

                    //TODO: May want to randomize the alphas a bit (or get the failed region working with 3 stops)
                    RadialGradientBrush brush = new RadialGradientBrush(Color.FromArgb(255, 255, 255, 255), Color.FromArgb(128, 255, 255, 255));

                    ctx.DrawEllipse(brush, null, position, radiusX, radiusY);
                }
            }

            dv.Transform = new RotateTransform(rand.NextDouble(360), center.X, center.Y);

            RenderTargetBitmap bitmap = new RenderTargetBitmap(width, height, UtilityWPF.DPI, UtilityWPF.DPI, PixelFormats.Pbgra32);
            bitmap.Render(dv);

            // Convert to a convolution
            Convolution2D retVal = UtilityWPF.ConvertToConvolution(bitmap, 1d);

            double test = retVal.Values.Max();

            //NOTE: It's easier to let the background default to black, and draw white shapes on it.  Now it needs to be reversed
            retVal = Convolutions.Invert(retVal, 1d);

            return retVal;
        }
    }

    #region Class: FeatureRecognizer_Session

    public class FeatureRecognizer_Session
    {
        /// <summary>
        /// This will be blank if there is no session.  This is useful when reading the session xml in the "latest" folder
        /// </summary>
        public string SessionName { get; set; }

        public FeatureRecognizer_Image[] Images { get; set; }

        // If I store this here, the serialized file gets big in a hurry.  Now saving each extract as it's own file
        //public FeatureRecognizer_Extract[] Extracts { get; set; }
        public string[] ExtractFilenames { get; set; }
    }

    #endregion
    #region Class: FeatureRecognizer_Image

    public class FeatureRecognizer_Image
    {
        public string Tag { get; set; }

        /// <summary>
        /// This is a guid, and should be the same as what's in the filename
        /// </summary>
        /// <remarks>
        /// Extracts need a way to reference images, so this is the value that they'll store
        /// </remarks>
        public string UniqueID { get; set; }

        public string Filename { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Image ImageControl { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public BitmapSource Bitmap { get; set; }

        public long Token { get; set; }
    }

    #endregion
    #region Class: FeatureRecognizer_Extract

    public class FeatureRecognizer_Extract
    {
        public FeatureRecognizer_Extract_Sub[] Extracts { get; set; }

        // Only one of these two (if any) will be populated
        public Convolution2D_DNA PreFilterDNA_Single { get; set; }
        public ConvolutionSet2D_DNA PreFilterDNA_Set { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ConvolutionBase2D PreFilter { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public UIElement Control { get; set; }

        /// <summary>
        /// This is the image that this was created from
        /// </summary>
        public string ImageID { get; set; }

        /// <summary>
        /// This is the same as what's in the filename, and can be used to reference this instance
        /// </summary>
        public string UniqueID { get; set; }

        public string Filename { get; set; }
    }

    #endregion
    #region Class: FeatureRecognizer_Extract_Sub

    public class FeatureRecognizer_Extract_Sub
    {
        public int InputWidth { get; set; }
        public int InputHeight { get; set; }

        public Convolution2D_DNA ExtractDNA { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Convolution2D Extract { get; set; }

        public FeatureRecognizer_Extract_Result[] Results { get; set; }
    }

    #endregion
    #region Class: FeatureRecognizer_Extract_Result

    /// <summary>
    /// After an image is run through the prefilter, then run through the extract, find the brightest pixel, and compare the rectangle around that
    /// pixel with the convolution stored here
    /// </summary>
    public class FeatureRecognizer_Extract_Result
    {
        /// <summary>
        /// This is the offset from the brightest point (start the rectangle at brightest pixel + offset)
        /// NOTE: The offset will always be negative
        /// </summary>
        public VectorInt Offset { get; set; }
        /// <summary>
        /// This is the rectangle to grab out of the larger convolution
        /// </summary>
        public RectInt Rectangle { get; set; }

        /// <summary>
        /// This is the value of the pixel that this structure is pointing to (it was the brightest pixel)
        /// </summary>
        public double BrightestValue { get; set; }

        /// <summary>
        /// This is the score given from the pure algorithm
        /// </summary>
        public double Score { get; set; }

        /// <summary>
        /// This holds a snippet of what to expect.  Subtract this from your image, and if the values are near zero, it's a match
        /// </summary>
        public Convolution2D_DNA ResultDNA { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Convolution2D Result { get; set; }
    }

    #endregion
}
