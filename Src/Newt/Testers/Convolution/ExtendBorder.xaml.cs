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
using System.Windows.Threading;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;

namespace Game.Newt.Testers.Convolution
{
    public partial class ExtendBorder : Window
    {
        #region Declaration Section

        private Convolution2D _origConv = null;

        private readonly DispatcherTimer _timer;

        private bool _initialized = false;

        #endregion

        #region Constructor

        public ExtendBorder()
        {
            InitializeComponent();

            _timer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(25),
                IsEnabled = false,
            };

            _timer.Tick += Timer_Tick;

            _initialized = true;
        }

        #endregion

        #region Event Listeners

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            const double MAXMULT = 2;

            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog();
                dialog.Multiselect = false;
                dialog.Title = "Please select an image";
                bool? result = dialog.ShowDialog();
                if (result == null || !result.Value)
                {
                    return;
                }

                _origConv = UtilityWPF.ConvertToConvolution(new BitmapImage(new Uri(dialog.FileName)));

                lblOrigWidth.Text = _origConv.Width.ToString("N0");
                lblOrigHeight.Text = _origConv.Height.ToString("N0");

                double max = Math.Max(_origConv.Width, _origConv.Height) * MAXMULT;

                trkWidth.Minimum = _origConv.Width;
                trkWidth.Maximum = max;
                trkWidth.Value = _origConv.Width;

                trkHeight.Minimum = _origConv.Height;
                trkHeight.Maximum = max;
                trkHeight.Value = _origConv.Height;

                _timer.IsEnabled = false;       // touching the sliders causes the timer to be enabled

                grdSize.Visibility = Visibility.Visible;

                ShowConvolution(_origConv);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void trkWidth_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                lblWidth.Text = trkWidth.Value.ToString("N0");

                _timer.IsEnabled = false;
                _timer.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void trkHeight_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                lblHeight.Text = trkHeight.Value.ToString("N0");

                _timer.IsEnabled = false;
                _timer.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void chkContinuous_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                _timer.IsEnabled = false;
                _timer.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (!chkContinuous.IsChecked.Value)
                {
                    _timer.IsEnabled = false;
                }

                int width = trkWidth.Value.ToInt_Round();
                int height = trkHeight.Value.ToInt_Round();

                Convolution2D conv = Convolutions.ExtendBorders(_origConv, width, height);

                ShowConvolution(conv);
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
                if (!_initialized)
                {
                    return;
                }

                _imageScale.ScaleX = trkZoom.Value;
                _imageScale.ScaleY = trkZoom.Value;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private void ShowConvolution(Convolution2D conv)
        {
            byte[][] colors = Convolutions.GetColors(conv, ConvolutionResultNegPosColoring.BlackWhite);

            _image.Source = UtilityWPF.GetBitmap(colors, conv.Width, conv.Height);
        }

        #region OLD

        //private static Convolution2D ExtendBorders(Convolution2D conv, int width, int height)
        //{
        //    if (width < conv.Width || height < conv.Height)
        //    {
        //        throw new ArgumentException(string.Format("The new size can't be smaller than old.  Old={0},{1}  --  New={2},{3}", conv.Width, conv.Height, width, height));
        //    }

        //    VectorInt offset = new VectorInt()
        //    {
        //        X = (width - conv.Width) / 2,
        //        Y = (height - conv.Height) / 2,
        //    };

        //    double[] values = new double[width * height];

        //    #region Copy the image

        //    for (int y = 0; y < conv.Height; y++)
        //    {
        //        int offsetOrigY = y * conv.Width;
        //        int offsetNewY = (y + offset.Y) * width;

        //        for (int x = 0; x < conv.Width; x++)
        //        {
        //            values[offsetNewY + offset.X + x] = conv.Values[offsetOrigY + x];
        //        }
        //    }

        //    #endregion

        //    #region Edges

        //    bool hasNegX = offset.X > 0;
        //    AxisFor forNegX = new AxisFor(Axis.X, offset.X - 1, 0);
        //    if (hasNegX)
        //    {
        //        ExtendEdge_RandCopy_Gauss(values, width, conv.Width, forNegX, new AxisFor(Axis.Y, offset.Y, offset.Y + conv.Height - 1));
        //    }

        //    bool hasNegY = offset.Y > 0;
        //    AxisFor forNegY = new AxisFor(Axis.Y, offset.Y - 1, 0);
        //    if (hasNegY)
        //    {
        //        ExtendEdge_RandCopy_Gauss(values, width, conv.Height, forNegY, new AxisFor(Axis.X, offset.X, offset.X + conv.Width - 1));
        //    }

        //    bool hasPosX = width > offset.X + conv.Width;
        //    AxisFor forPosX = new AxisFor(Axis.X, offset.X + conv.Width, width - 1);
        //    if (hasPosX)
        //    {
        //        ExtendEdge_RandCopy_Gauss(values, width, conv.Width, forPosX, new AxisFor(Axis.Y, offset.Y, offset.Y + conv.Height - 1));
        //    }

        //    bool hasPosY = height > offset.Y + conv.Height;
        //    AxisFor forPosY = new AxisFor(Axis.Y, offset.Y + conv.Height, height - 1);
        //    if (hasPosY)
        //    {
        //        ExtendEdge_RandCopy_Gauss(values, width, conv.Height, forPosY, new AxisFor(Axis.X, offset.X, offset.X + conv.Width - 1));
        //    }

        //    #endregion
        //    #region Corners

        //    if (hasNegX && hasNegY)
        //    {
        //        ExtendCorner(values, width, forNegX, forNegY);
        //    }

        //    if (hasPosX && hasNegY)
        //    {
        //        ExtendCorner(values, width, forPosX, forNegY);
        //    }

        //    if (hasPosX && hasPosY)
        //    {
        //        ExtendCorner(values, width, forPosX, forPosY);
        //    }

        //    if (hasNegX && hasPosY)
        //    {
        //        ExtendCorner(values, width, forNegX, forPosY);
        //    }

        //    #endregion

        //    return new Convolution2D(values, width, height, conv.IsNegPos);
        //}

        //private static void ExtendEdge_Fuzz(double[] values, int width, AxisFor orth, AxisFor edge)
        //{
        //    Random rand = StaticRandom.GetRandomForThread();

        //    foreach (int edgeIndex in edge.Iterate())
        //    {
        //        foreach (int orthIndex in orth.Iterate())
        //        {
        //            int x = -1;
        //            int y = -1;
        //            orth.Set2DIndex(ref x, ref y, orthIndex);
        //            edge.Set2DIndex(ref x, ref y, edgeIndex);

        //            values[(y * width) + x] = rand.Next(256);
        //        }
        //    }
        //}
        //private static void ExtendEdge_Copy(double[] values, int width, AxisFor orth, AxisFor edge)
        //{
        //    int orthInc = GetOrthIncrement(orth);

        //    foreach (int orthIndex in orth.Iterate())
        //    {
        //        foreach (int edgeIndex in edge.Iterate())
        //        {
        //            int toX = -1;
        //            int toY = -1;
        //            orth.Set2DIndex(ref toX, ref toY, orthIndex);
        //            edge.Set2DIndex(ref toX, ref toY, edgeIndex);

        //            int fromX = -1;
        //            int fromY = -1;
        //            orth.Set2DIndex(ref fromX, ref fromY, orthIndex + orthInc);
        //            edge.Set2DIndex(ref fromX, ref fromY, edgeIndex);

        //            values[(toY * width) + toX] = values[(fromY * width) + fromX];
        //        }
        //    }
        //}
        //private static void ExtendEdge_RandCopy(double[] values, int width, int orthHeight, AxisFor orth, AxisFor edge)
        //{
        //    int ORTHDEPTH = Math.Min(5, orthHeight) + 1;
        //    const int EDGEDEPTH = 4;

        //    Random rand = StaticRandom.GetRandomForThread();

        //    // Figure out which direction to copy rows from
        //    int orthInc = GetOrthIncrement(orth);

        //    foreach (int orthIndex in orth.Iterate())
        //    {
        //        CopyRandomPixels(values, width, orthIndex, orthInc, ORTHDEPTH, EDGEDEPTH, orth, edge, rand);
        //    }
        //}
        //private static void ExtendEdge_RandCopy_Gauss(double[] values, int width, int orthHeight, AxisFor orth, AxisFor edge)
        //{
        //    const int EDGEDEPTH = 4;
        //    int ORTHDEPTH = Math.Min(5, orthHeight) + 1;

        //    const int EDGEMIDOFFSET = 5;

        //    //const int GAUSSOPACITYDIST = 1;       // how many pixels before the gaussian is full strength

        //    Random rand = StaticRandom.GetRandomForThread();

        //    Convolution2D gauss = Convolutions.GetGaussian(3);

        //    // Figure out which direction to copy rows from
        //    int orthInc = GetOrthIncrement(orth);

        //    #region Edge midpoint range

        //    // Each row, a random midpoint is chosen.  This way, an artifact won't be created down the middle.
        //    // mid start and stop are the range of possible values that the random midpoint can be from

        //    int edgeMidStart = edge.Start;
        //    int edgeMidStop = edge.Stop;

        //    UtilityCore.MinMax(ref edgeMidStart, ref edgeMidStop);

        //    edgeMidStart += EDGEMIDOFFSET;
        //    edgeMidStop -= EDGEMIDOFFSET;

        //    #endregion

        //    foreach (int orthIndex in orth.Iterate())
        //    {
        //        CopyRandomPixels(values, width, orthIndex, orthInc, ORTHDEPTH, EDGEDEPTH, orth, edge, rand);

        //        OverlayBlurredPixels(values, width, orthHeight, orthIndex, orthInc, edgeMidStart, edgeMidStop, orth, edge, gauss, /*GAUSSOPACITYDIST,*/ rand);
        //    }
        //}

        ///// <summary>
        ///// For each pixel in this row, pick a random pixel from a box above
        ///// </summary>
        ///// <remarks>
        ///// At each pixel, this draws from a box of size orthDepth x (edgeDepth*2)+1
        ///// </remarks>
        ///// <param name="orthIndex">The row being copied to</param>
        //private static void CopyRandomPixels(double[] values, int width, int orthIndex, int orthInc, int orthDepth, int edgeDepth, AxisFor orth, AxisFor edge, Random rand)
        //{
        //    // See how many rows to randomly pull from
        //    int orthDepthStart = orthIndex + orthInc;
        //    int orthDepthStop = orthIndex + (orthDepth * orthInc);
        //    UtilityCore.MinMax(ref orthDepthStart, ref orthDepthStop);
        //    int orthAdd = orthInc < 0 ? 1 : 0;

        //    foreach (int edgeIndex in edge.Iterate())
        //    {
        //        // Figure out which column to pull from
        //        int toEdge = -1;
        //        do
        //        {
        //            toEdge = rand.Next(edgeIndex - edgeDepth, edgeIndex + edgeDepth + 1);
        //        } while (!edge.IsBetween(toEdge));

        //        // Figure out which row to pull from
        //        int toOrth = rand.Next(orthDepthStart, orthDepthStop) + orthAdd;

        //        // From XY
        //        int fromX = -1;
        //        int fromY = -1;
        //        orth.Set2DIndex(ref fromX, ref fromY, toOrth);
        //        edge.Set2DIndex(ref fromX, ref fromY, toEdge);

        //        // To XY
        //        int toX = -1;
        //        int toY = -1;
        //        orth.Set2DIndex(ref toX, ref toY, orthIndex);
        //        edge.Set2DIndex(ref toX, ref toY, edgeIndex);

        //        // Copy pixel
        //        values[(toY * width) + toX] = values[(fromY * width) + fromX];
        //    }
        //}
        ///// <summary>
        ///// Runs a gaussian over the current row and the two prior.  Then copies those blurred values onto this row
        ///// </summary>
        ///// <param name="orthIndex">The row being copied to</param>
        //private static void OverlayBlurredPixels(double[] values, int width, int orthHeight, int orthIndex, int orthInc, int edgeMidStart, int edgeMidStop, AxisFor orth, AxisFor edge, Convolution2D gauss, /*int opacityDistance,*/ Random rand)
        //{
        //    if (orthHeight < 2)
        //    {
        //        // There's not enough to do a full 3x3 blur.  A smaller sized blur could be done, but that's a lot of extra logic, and not really worth the trouble
        //        return;
        //    }

        //    //double opacity = UtilityCore.GetScaledValue_Capped(0d, 1d, 0, opacityDistance, Math.Abs(orthIndex - orth.Start));

        //    // Get a random midpoint
        //    int edgeMid = rand.Next(edgeMidStart, edgeMidStop + 1);

        //    AxisFor orth3 = new AxisFor(orth.Axis, orthIndex + (orthInc * 2), orthIndex);
        //    AxisFor leftEdge = new AxisFor(edge.Axis, edge.Start, edgeMid + (edge.Increment * 2));
        //    AxisFor rightEdge = new AxisFor(edge.Axis, edgeMid - (edge.Increment * 2), edge.Stop);

        //    // Copy the values (these are 3 tall, and edgeMid-edgeStart+2 wide)
        //    Convolution2D leftRect = CopyRect(values, width, leftEdge, orth3, false);
        //    Convolution2D rightRect = CopyRect(values, width, rightEdge, orth3, true);      // this one is rotated 180 so that when the gaussian is applied, it will be from the right border to the mid

        //    // Apply a gaussian (these are 1 tall, and edgeMid-edgeStart wide)
        //    Convolution2D leftBlurred = Convolutions.Convolute(leftRect, gauss);
        //    Convolution2D rightBlurred = Convolutions.Convolute(rightRect, gauss);

        //    // Overlay onto this newest row
        //    //TODO: use an opacity LERP from original edge
        //    OverlayRow(values, width, orthIndex, leftBlurred.Values, orth, edge, /*opacity,*/ true);
        //    OverlayRow(values, width, orthIndex, rightBlurred.Values, orth, edge, /*opacity,*/ false);
        //}

        //private static Convolution2D CopyRect(double[] values, int width, AxisFor edge, AxisFor orth, bool shouldRotate180)
        //{
        //    if (shouldRotate180)
        //    {
        //        return CopyRect(values, width, new AxisFor(edge.Axis, edge.Stop, edge.Start), new AxisFor(orth.Axis, orth.Stop, orth.Start), false);
        //    }

        //    double[] retVal = new double[edge.Length * orth.Length];

        //    int index = 0;

        //    foreach (int orthIndex in orth.Iterate())
        //    {
        //        int x = -1;
        //        int y = -1;
        //        orth.Set2DIndex(ref x, ref y, orthIndex);

        //        foreach (int edgeIndex in edge.Iterate())
        //        {
        //            edge.Set2DIndex(ref x, ref y, edgeIndex);

        //            retVal[index] = values[(y * width) + x];

        //            index++;
        //        }
        //    }

        //    return new Convolution2D(retVal, edge.Length, orth.Length, false);
        //}
        //private static void OverlayRow(double[] values, int width, int orthIndex, double[] overlay, AxisFor orth, AxisFor edge, /*double opacity,*/ bool isLeftToRight)
        //{
        //    int x = -1;
        //    int y = -1;
        //    orth.Set2DIndex(ref x, ref y, orthIndex);

        //    for (int cntr = 0; cntr < overlay.Length; cntr++)
        //    {
        //        int edgeIndex = -1;
        //        if (isLeftToRight)
        //        {
        //            //edgeIndex = edge.Start + (cntr * edge.Increment);
        //            edgeIndex = edge.Start + ((cntr + 1) * edge.Increment);
        //        }
        //        else
        //        {
        //            //edgeIndex = edge.Stop - (cntr * edge.Increment);
        //            edgeIndex = edge.Stop - ((cntr + 1) * edge.Increment);
        //        }

        //        edge.Set2DIndex(ref x, ref y, edgeIndex);

        //        int index = (y * width) + x;
        //        //values[index] = UtilityCore.GetScaledValue(values[index], overlay[cntr], 0d, 1d, opacity);
        //        values[index] = overlay[cntr];
        //    }
        //}

        //private static int GetOrthIncrement(AxisFor orth)
        //{
        //    if (orth.Start == orth.Stop)
        //    {
        //        if (orth.Start == 0)
        //        {
        //            return 1;        // it's sitting on the zero edge.  Need to pull from the positive side
        //        }
        //        else
        //        {
        //            return -1;       // likely sitting on the other edge
        //        }
        //    }
        //    else
        //    {
        //        return orth.Increment * -1;
        //    }
        //}

        //private static void ExtendCorner(double[] values, int width, AxisFor orth, AxisFor edge)
        //{
        //    Random rand = StaticRandom.GetRandomForThread();

        //    foreach (int edgeIndex in edge.Iterate())
        //    {
        //        foreach (int orthIndex in orth.Iterate())
        //        {
        //            int x = -1;
        //            int y = -1;
        //            orth.Set2DIndex(ref x, ref y, orthIndex);
        //            edge.Set2DIndex(ref x, ref y, edgeIndex);

        //            values[(y * width) + x] = rand.Next(256);
        //        }
        //    }
        //}

        #endregion

        #endregion
    }
}
