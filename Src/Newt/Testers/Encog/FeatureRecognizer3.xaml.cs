using System;
using System.Collections.Generic;
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
using Microsoft.Win32;

namespace Game.Newt.Testers.Encog
{
    public partial class FeatureRecognizer3 : Window
    {
        #region Declaration Section

        private const int IMAGESIZE = 300;
        private const int THUMBSIZE_EXTRACT = 40;

        private List<FeatureRecognizer_Image> _images = new List<FeatureRecognizer_Image>();

        private List<ConvolutionWrapper2D> _convolutions = new List<ConvolutionWrapper2D>();

        //TODO: Store ConvSets, come up with some way to give them a score

        #endregion

        #region Constructor

        public FeatureRecognizer3()
        {
            InitializeComponent();

            // Seed the convolution list with lineage 0s
            _convolutions.AddRange(GetLineage0Convolutions());

        }

        #endregion

        #region Event Listeners

        private void RandomConvSet_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int targetSize;
                if (!int.TryParse(txtConvSetTargetSize.Text, out targetSize))
                {
                    MessageBox.Show("Couldn't parse target size as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var convByLineage = _convolutions.
                    ToLookup(o => o.Lineage).
                    OrderBy(o => o.Key).
                    ToArray();

                List<ConvolutionWrapper2D> convolutions = new List<ConvolutionWrapper2D>();

                while (true)
                {
                    var candidates = convByLineage.SelectMany(o => o).ToArray();

                    double percentInList = StaticRandom.NextPow(3, isPlusMinus: false);

                    int index = UtilityCore.GetIndexIntoList(percentInList, candidates.Length);

                    convolutions.Add(candidates[index]);
                }







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

        #region Private Methods

        private static IEnumerable<ConvolutionWrapper2D> GetLineage0Convolutions()
        {
            List<ConvolutionBase2D> convolutions = new List<ConvolutionBase2D>();

            // Gaussian
            foreach (int size in new[] { 3, 7, 15 })
            {
                convolutions.Add(Convolutions.GetGaussian(size, 1));
                convolutions.Add(Convolutions.GetGaussian(size, 2));
            }

            // Gaussian Subtract
            convolutions.Add(new ConvolutionSet2D(new[] { Convolutions.GetGaussian(3, 1) }, SetOperationType.Subtract));

            // MaxAbs Sobel
            foreach (int gain in new[] { 1, 2 })
            {
                convolutions.Add(Convolutions.GetEdgeSet_Sobel(gain));
            }

            // Gausian then edge
            foreach (int size in new[] { 3, 5, 7 })
            {
                convolutions.Add(Convolutions.GetEdgeSet_GuassianThenEdge(size));
            }

            // Single sobels
            Convolution2D sobelVert = Convolutions.GetEdge_Sobel(true);
            Convolution2D sobelHorz = Convolutions.GetEdge_Sobel(false);
            Convolution2D sobel45 = Convolutions.Rotate_45(sobelVert, true);
            Convolution2D sobel135 = Convolutions.Rotate_45(sobelHorz, true);
            convolutions.Add(sobelVert);
            convolutions.Add(sobelHorz);
            convolutions.Add(sobel45);
            convolutions.Add(sobel135);
            convolutions.Add(Convolutions.Invert(sobelVert));
            convolutions.Add(Convolutions.Invert(sobelHorz));
            convolutions.Add(Convolutions.Invert(sobel45));
            convolutions.Add(Convolutions.Invert(sobel135));

            // Laplacian
            convolutions.Add(Convolutions.GetEdge_Laplacian(true));
            convolutions.Add(Convolutions.GetEdge_Laplacian(false));

            return convolutions.
                Select(o => new ConvolutionWrapper2D(o, 0));
        }

        #endregion
    }

    #region Class: ConvolutionWrapper

    public class ConvolutionWrapper2D
    {
        public ConvolutionWrapper2D(ConvolutionBase2D convolution, int lineage)
        {
            this.Convolution = convolution;
            this.Lineage = lineage;
            this.Reduction = convolution.GetReduction();
        }

        // only one of these two will be populated
        public readonly ConvolutionBase2D Convolution;
        //public readonly MaxPool2D MaxPool;

        /// <summary>
        /// This is used to give a bit of a sort order to this convolution.  Larger lineages should be applied to lesser (because it's likely
        /// a result of a later convolution)
        /// </summary>
        /// <remarks>
        /// A 3x3 hardcoded patch will have no lineage
        /// 
        /// An extract of image+lin0 will be lin1
        /// </remarks>
        public readonly int Lineage;

        public readonly VectorInt Reduction;
    }

    #endregion
    #region Class: FeatureRecognizer3_ConvSet

    public class FeatureRecognizer3_ConvSet
    {
        public FeatureRecognizer3_ConvSet(ConvolutionWrapper2D[] convSet)
        {
            this.ConvSet = convSet;

            VectorInt reduction = new VectorInt(0, 0);

            foreach (ConvolutionWrapper2D conv in convSet)
            {
                reduction += conv.Reduction;
            }

            this.Reduction = reduction;
            this.MaxLineage = convSet.Max(o => o.Lineage);
        }

        public readonly ConvolutionWrapper2D[] ConvSet;

        public readonly VectorInt Reduction;

        public readonly int MaxLineage;

        public ConvolutionWrapper2D Convolute(ConvolutionWrapper2D image)
        {
            if (image.Convolution == null)       // if null, then it's probably a maxpool
            {
                throw new ArgumentException("image must be a convolution");
            }

            Convolution2D imageCast = image.Convolution as Convolution2D;
            if (imageCast == null)
            {
                throw new ArgumentException("image must be Convolution2D");
            }

            if (imageCast.Width <= this.Reduction.X || imageCast.Height <= this.Reduction.Y)
            {
                throw new ArgumentException("The image is too small for these convolutions");
            }

            Convolution2D retVal = imageCast;

            foreach (var conv in this.ConvSet)
            {
                if (conv.Convolution != null)
                {
                    retVal = Convolutions.Convolute(retVal, conv.Convolution);
                }
                else //if(conv.maxpool != null)
                {
                    throw new ApplicationException("Unknown type of convolution wrapper");
                }
            }

            return new ConvolutionWrapper2D(retVal, this.MaxLineage + 1);
        }
    }

    #endregion
}
