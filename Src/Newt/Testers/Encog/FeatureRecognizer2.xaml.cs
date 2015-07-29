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
    public partial class FeatureRecognizer2 : Window
    {
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
        private const int THUMBSIZE_EXTRACT = 40;

        private List<FeatureRecognizer_Image> _images = new List<FeatureRecognizer_Image>();

        /// <summary>
        /// These are optional convolutions that could be applied between the image and extract
        /// </summary>
        private readonly Tuple<PrefilterType, ConvolutionBase2D[]>[] _preFilters;

        private readonly DropShadowEffect _selectEffect;

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
                Color = UtilityWPF.ColorFromHex("66E825"),
                Opacity = .9,
            };

        }

        #endregion

        #region Event Listeners

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
                var image = GetSelectedImage();
                if (image == null)
                {
                    image = _images[StaticRandom.Next(_images.Count)];
                }

                // Prefilter (could be null)
                ConvolutionBase2D preFilter = GetRandomPreFilter();






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

                //Tuple<Border, int> clickedCtrl = GetSelectedExtract(e.OriginalSource);
                //if (clickedCtrl == null)
                //{
                //    return;
                //}

                //SelectExtract(clickedCtrl.Item2);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private FeatureRecognizer_Image GetSelectedImage()
        {
            Image selected = treeImages.SelectedItem as Image;
            if (selected == null)
            {
                return null;
            }

            return _images.FirstOrDefault(o => o.ImageControl == selected);
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

        #endregion
    }

    #region Class: FeatureRecognizer2_FeatureConv

    public class FeatureRecognizer2_FeatureConv
    {
        // Only one of these two will be populated
        public Convolution2D_DNA ConvolutionDNA_Single { get; set; }
        public ConvolutionSet2D_DNA ConvolutionDNA_Set { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ConvolutionBase2D Convolution { get; set; }

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
        //TODO: The referenced convolution is full size.  This class may want to store a smaller version of it, and reduce any image by the same percent
        public string FeatureConv_UniqueID { get; set; }
    }

    #endregion
}
