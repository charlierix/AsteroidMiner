using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Game.HelperClassesCore;

namespace Game.HelperClassesWPF
{
    public static class Convolutions
    {
        #region Main operations

        public static Convolution2D Convolute(Convolution2D image, ConvolutionBase2D kernel, string description = "")
        {
            if (kernel is Convolution2D)
            {
                return Convolute_Single(image, (Convolution2D)kernel, description);
            }
            else if (kernel is ConvolutionSet2D)
            {
                return Convolute_Set(image, (ConvolutionSet2D)kernel, description);
            }
            else
            {
                throw new ApplicationException("Unexpected type of kernel: " + kernel.GetType().ToString());
            }
        }

        //NOTE: This ignores gain and iterations
        public static Convolution2D Subtract(Convolution2D orig, Convolution2D filtered, string description = "")
        {
            int width = Math.Min(orig.Width, filtered.Width);
            int height = Math.Min(orig.Height, filtered.Height);

            #region Calculate offsets

            int diffX = orig.Width - filtered.Width;
            int diffY = orig.Height - filtered.Height;

            int offsetOrigX = 0;
            int offsetOrigY = 0;
            int offsetFiltX = 0;
            int offsetFiltY = 0;

            if (diffX > 0)
            {
                offsetOrigX = Subtract_GetOffset(diffX);
            }
            else if (diffX < 0)
            {
                offsetFiltX = Subtract_GetOffset(-diffX);
            }

            if (diffY > 0)
            {
                offsetOrigY = Subtract_GetOffset(diffY);
            }
            else if (diffY < 0)
            {
                offsetFiltY = Subtract_GetOffset(-diffY);
            }

            #endregion

            double[] values = new double[width * height];

            for (int y = 0; y < height; y++)
            {
                int filterY = (offsetFiltY + y) * filtered.Width;
                int origY = (offsetOrigY + y) * orig.Width;
                int valuesY = y * width;

                for (int x = 0; x < width; x++)
                {
                    int filterIndex = filterY + offsetFiltX + x;
                    int origIndex = origY + offsetOrigX + x;

                    values[valuesY + x] = orig.Values[origIndex] - filtered.Values[filterIndex];
                }
            }

            return new Convolution2D(values, width, height, true, description: description);
        }

        #endregion

        #region Manipulate convolution

        /// <summary>
        /// This scales so the sum is 1 and -1
        /// NOTE: The convolution needs to be a unit before sending to this.Convolute
        /// </summary>
        public static double[] ToUnit(double[] values)
        {
            if (values.All(o => o.IsNearZero()))
            {
                // All zero
                return values;
            }

            // Separate into positives and negatives
            var pos = values.
                Select((o, i) => new { Index = i, Value = o }).
                Where(o => o.Value > 0).
                ToArray();

            var neg = values.
                Select((o, i) => new { Index = i, Value = o }).
                Where(o => o.Value < 0).
                ToArray();

            if (neg.Length > 0 && pos.Length > 0)
            {
                #region Both

                // Positives need to add to 1, negatives to -1
                double[] unitPos = SumTo(pos.Select(o => o.Value), 1);
                double[] unitNeg = SumTo(neg.Select(o => o.Value), -1);

                // Put the normalized values into a full sized array
                double[] retVal = new double[values.Length];

                for (int cntr = 0; cntr < pos.Length; cntr++)
                {
                    retVal[pos[cntr].Index] = unitPos[cntr];
                }

                for (int cntr = 0; cntr < neg.Length; cntr++)
                {
                    retVal[neg[cntr].Index] = unitNeg[cntr];
                }

                return retVal;

                #endregion
            }
            else if (pos.Length > 0)
            {
                // They need to add to 1
                return SumTo(values, 1);
            }
            else if (neg.Length > 0)
            {
                // They need to add to -1
                return SumTo(values, -1);
            }
            else
            {
                return values;
            }
        }
        /// <summary>
        /// This scales so the largest value is one
        /// NOTE: This amplifies differences, and is only meant for sending to a painter
        /// </summary>
        public static double[] ToMaximized(double[] values, double max = 1d)
        {
            if (values.All(o => o.IsNearZero()))
            {
                // All zero
                return values;
            }

            // Separate into positives and negatives
            var pos = values.
                Select((o, i) => new { Index = i, Value = o }).
                Where(o => o.Value > 0).
                ToArray();

            var neg = values.
                Select((o, i) => new { Index = i, Value = o }).
                Where(o => o.Value < 0).
                ToArray();

            if (neg.Length > 0 && pos.Length > 0)
            {
                #region Both

                double scalePos = max / pos.Max(o => o.Value);
                double scaleNeg = -max / neg.Min(o => o.Value);        // they're both negative, so the scale is positive (taking min, because they are all negative)

                double[] fixedPos = pos.Select(o => o.Value * scalePos).ToArray();
                double[] fixedNeg = neg.Select(o => o.Value * scaleNeg).ToArray();

                // Put the normalized values into a full sized array
                double[] retVal = new double[values.Length];

                for (int cntr = 0; cntr < pos.Length; cntr++)
                {
                    retVal[pos[cntr].Index] = fixedPos[cntr];
                }

                for (int cntr = 0; cntr < neg.Length; cntr++)
                {
                    retVal[neg[cntr].Index] = fixedNeg[cntr];
                }

                return retVal;

                #endregion
            }
            else if (pos.Length > 0)
            {
                // They're all positive
                double scale = max / values.Max();
                return values.Select(o => o * scale).ToArray();
            }
            else if (neg.Length > 0)
            {
                // They're all negative
                double scale = -max / neg.Min(o => o.Value);
                return values.Select(o => o * scale).ToArray();
            }
            else
            {
                return values;
            }
        }

        public static Convolution2D Rotate_90(Convolution2D convolution, bool isClockwise)
        {
            double[] newValues = Rotate_90(convolution.Values, convolution.Width, convolution.Height, isClockwise);

            //NOTE: width becomes height
            return new Convolution2D(newValues, convolution.Height, convolution.Width, convolution.IsNegPos, convolution.Gain, convolution.Iterations, convolution.ExpandBorder, convolution.Description);
        }
        public static double[] Rotate_90(double[] values, int width, int height, bool isClockwise)
        {
            bool revX = !isClockwise;
            bool revY = isClockwise;

            double[] retVal = new double[values.Length];

            for (int newX = 0; newX < height; newX++)     // old height becomes new width
            {
                for (int newY = 0; newY < width; newY++)      // width becomes height
                {
                    int oldX = revX ? width - newY - 1 : newY;
                    int oldY = revY ? height - newX - 1 : newX;

                    retVal[(newY * height) + newX] = values[(oldY * width) + oldX];
                }
            }

            return retVal;
        }

        public static Convolution2D Rotate_45(Convolution2D convolution, bool isClockwise, bool shouldAdvanceEvensExtra = false)
        {
            if (convolution.Width != convolution.Height)
            {
                throw new ArgumentException("Width and height must be the same for 45 degree rotations");
            }

            double[] newValues = Rotate_45(convolution.Values, convolution.Width, isClockwise, shouldAdvanceEvensExtra);

            return new Convolution2D(newValues, convolution.Width, convolution.Height, convolution.IsNegPos, convolution.Gain, convolution.Iterations, convolution.ExpandBorder, convolution.Description);
        }
        /// <summary>
        /// This rotates 45 degrees
        /// NOTE: This just slides/swirls values around, and will look odd with larger convolutions
        /// NOTE: This only works with square convolutions
        /// </summary>
        /// <param name="widthHeight">This only works on square images</param>
        /// <param name="shouldAdvanceEvensExtra">
        /// It's not possible to do 45 degree rotations on images that have an even width/height.  The rings will slide around
        /// each other at different rates.  This is sort of fixed by taking a boolean that advances the odd numbered rings 1 extra,
        /// then it's up to the caller to pass true/false every other time they call this method
        /// </param>
        /// <remarks>
        /// -1 0 1
        /// -2 0 2
        /// -1 0 1
        /// counter:
        /// 0 1 2
        /// -1 0 1
        /// -2 -1 0
        /// 
        /// Quarter samples
        ///    3x3				3x3					4x4					4x4			
        ///    a	b	c			a	b			a	b	c	d				a	b
        ///        a				a	c				a	b					a	c
        ///                                                                    b	e
        ///
        ///5x5						5x5							6x6							6x6					
        ///a	b	c	d	e				a	b	c			a	b	c	d	e	f					a	b	c
        ///    a	b	c					a	b	d				a	b	c	d						a	b	d
        ///    h	a	d					a	c	e					a	b							a	c	e
        ///    g	f	e																				b	d	f
        ///
        ///7x7								7x7								
        ///a	b	c	d	e	f	g					a	b	c	d		
        ///    a	b	c	d	e						a	b	c	e		
        ///        a	b	c							a	b	d	f		
        ///            a								a	c	e	g		
        ///
        ///8x8									8x8							
        ///a	b	c	d	e	f	g	h						a	b	c	d
        ///    a	b	c	d	e	f							a	b	c	e
        ///        a	b	c	d								a	b	d	f
        ///            a	b									a	c	e	g
        ///                                                    b	d	f	h
        /// </remarks>
        public static double[] Rotate_45(double[] values, int widthHeight, bool isClockwise, bool shouldAdvanceEvensExtra = false)
        {
            double[] retVal = new double[values.Length];

            for (int size = widthHeight; size > 0; size -= 2)
            {
                Rotate45_Ring(values, retVal, size, widthHeight, isClockwise, shouldAdvanceEvensExtra);
            }

            return retVal;
        }

        public static Convolution2D Invert(Convolution2D convolution, double? maxValue = null)
        {
            double[] newValues = Invert(convolution.Values, convolution.IsNegPos, maxValue);
            return new Convolution2D(newValues, convolution.Width, convolution.Height, convolution.IsNegPos, convolution.Gain, convolution.Iterations, convolution.ExpandBorder, convolution.Description);
        }
        /// <summary>
        /// This will invert the values
        /// </summary>
        /// <param name="isNegPos">
        /// True: the values will just be multiplied by -1
        /// False: the values go from 0 to maxValue, and will be flipped relative to the halfway point
        /// </param>
        /// <param name="maxValue">
        /// Only needed when isNegPos is false.  If left null, the max will be calculated.
        /// NOTE: If you know what the max should be, pass it in.  Otherwise there's a good chance the the current max is less than that, and the cells will be flipped around a halfway point less than they should be
        /// </param>
        public static double[] Invert(double[] values, bool isNegPos, double? maxValue = null)
        {
            double[] retVal = new double[values.Length];

            if (isNegPos)
            {
                // Has negative
                for (int cntr = 0; cntr < retVal.Length; cntr++)
                {
                    retVal[cntr] = values[cntr] * -1d;
                }
            }
            else
            {
                // Zero to Max
                double actualMaxValue = maxValue ?? values.Max();
                double halfMax = actualMaxValue / 2d;

                for (int cntr = 0; cntr < retVal.Length; cntr++)
                {
                    retVal[cntr] = halfMax - (values[cntr] - halfMax);
                }
            }

            return retVal;
        }

        //TODO: Take an enum or something that lets the user pick what type of removals to choose from
        public static Convolution2D RemoveSection(Convolution2D conv)
        {
            //Convolution2D mask = GetMask_ScatterShot(conv.Width, conv.Height);
            Convolution2D mask = GetMask_Ellipse(conv.Width, conv.Height, StaticRandom.Next(1, 4));

            double[] values = new double[conv.Values.Length];

            for (int cntr = 0; cntr < values.Length; cntr++)
            {
                values[cntr] = conv.Values[cntr] * mask.Values[cntr];
            }

            values = Convolutions.ToUnit(values);

            return new Convolution2D(values, conv.Width, conv.Height, conv.IsNegPos, conv.Gain, conv.Iterations, conv.ExpandBorder, conv.Description);
        }

        //TODO: Convolution2D GetExpanded(Convolution2D conv, int toWidth, int toHeight)
        //The border should be some kind of blur of the outermost pixels (the from convo will be centered onto the destination)

        #endregion

        #region Generators

        /// <summary>
        /// This returns a kernel that will blur the image.  The kernel is a bell curve
        /// </summary>
        /// <param name="size">Width=size, Height=size</param>
        /// <param name="standardDeviation"></param>
        /// <param name="standardDeviationMultiplier"></param>
        /// <remarks>
        /// Got this here:
        /// https://code.msdn.microsoft.com/Calculating-Gaussian-c7c95d2c
        /// http://softwarebydefault.com/2013/06/08/calculating-gaussian-kernels/
        /// 
        /// Another good read
        /// http://homepages.inf.ed.ac.uk/rbf/HIPR2/gsmooth.htm
        /// </remarks>
        public static Convolution2D GetGaussian(int size, double standardDeviationMultiplier = 1d, double? standardDeviation = null)
        {
            if (size < 1)
            {
                throw new ArgumentOutOfRangeException("Size must be greater than zero");
            }

            //TODO: Calculate standard deviation better
            double weight = standardDeviation ?? (size / 4d) * standardDeviationMultiplier;

            double calculatedEuler = 1d / (2d * Math.PI * Math.Pow(weight, 2));
            double twiceWeightSquared = weight * weight * 2;

            double[] values = new double[size * size];
            double sum = 0;

            int kernelRadius = size / 2;

            if (size % 2 == 0)
            {
                #region Even

                for (int y = 0; y < size; y++)
                {
                    int yIndex = y * size;

                    for (int x = 0; x < size; x++)
                    {
                        double distX = x - kernelRadius + .5;
                        double distY = y - kernelRadius + .5;

                        // -------------- copy of below
                        double distance = ((distX * distX) + (distY * distY)) / twiceWeightSquared;
                        double value = calculatedEuler * Math.Exp(-distance);
                        sum += value;
                        // --------------

                        values[yIndex + x] = value;
                    }
                }

                #endregion
            }
            else
            {
                #region Odd

                for (int y = -kernelRadius; y <= kernelRadius; y++)
                {
                    int yIndex = (y + kernelRadius) * size;

                    for (int x = -kernelRadius; x <= kernelRadius; x++)
                    {
                        // -------------- copy of above
                        double distance = ((x * x) + (y * y)) / twiceWeightSquared;
                        double value = calculatedEuler * Math.Exp(-distance);
                        sum += value;
                        // --------------

                        int index = yIndex + (x + kernelRadius);
                        values[index] = value;
                    }
                }

                #endregion
            }

            // Make it add to one
            for (int cntr = 0; cntr < values.Length; cntr++)
            {
                values[cntr] /= sum;
            }

            string description = "gaussian";
            if (!standardDeviationMultiplier.IsNearValue(1))
            {
                description += " x" + standardDeviationMultiplier.ToStringSignificantDigits(1);
            }

            return new Convolution2D(values, size, size, false, description: description);
        }

        public static Convolution2D GetEdge_Sobel(bool vertical = true, double gain = 1d)
        {
            //Got this here:
            //http://www.imagemagick.org/Usage/convolve/#sobel

            //further down they say (sounds useful):
            //One way to collect all the edges of an image using a 'Sobel' kernel, is to apply the kernel 4 times in all directions, and collect the maximum value seen (using a Lighten Mathematical Composition. This is an approximation to the gradient magnitude.

            double[] values = null;
            if (vertical)
            {
                values = new[]
                {
                    1d, 0d, -1d,
                    2d, 0d, -2d,
                    1d, 0d, -1d,
                };
            }
            else
            {
                values = new[]
                {
                    -1d, -2d, -1d,
                    0d, 0d, 0d,
                    1d, 2d, 1d,
                };
            }

            values = ToUnit(values);

            string description = string.Format("sobel{0}", gain.IsNearValue(1) ? "" : string.Format(" [gain={0}]", gain.ToStringSignificantDigits(1)));
            return new Convolution2D(values, 3, 3, true, gain, description: description);
        }
        public static Convolution2D GetEdge_Prewitt(bool vertical = true, double gain = 1d)
        {
            //Got this here:
            //http://www.imagemagick.org/Usage/convolve/#prewitt

            double[] values = null;
            if (vertical)
            {
                values = new[]
                {
                    1d, 0d, -1d,
                    1d, 0d, -1d,
                    1d, 0d, -1d,
                };
            }
            else
            {
                values = new[]
                {
                    -1d, -1d, -1d,
                    0d, 0d, 0d,
                    1d, 1d, 1d,
                };
            }

            values = ToUnit(values);

            string description = string.Format("prewitt{0}", gain.IsNearValue(1) ? "" : string.Format(" [gain={0}]", gain.ToStringSignificantDigits(1)));
            return new Convolution2D(values, 3, 3, true, gain, description: description);
        }
        public static Convolution2D GetEdge_Compass(bool vertical = true, double gain = 1d)
        {
            //Got this here:
            //http://www.imagemagick.org/Usage/convolve/#compass

            double[] values = null;
            if (vertical)
            {
                values = new[]
                {
                    1d, 1d, -1d,
                    1d, -2d, -1d,
                    1d, 1d, -1d,
                };
            }
            else
            {
                values = new[]
                {
                    -1d, -1d, -1d,
                    1d, -2d, 1d,
                    1d, 1d, 1d,
                };
            }

            values = ToUnit(values);

            string description = string.Format("compass{0}", gain.IsNearValue(1) ? "" : string.Format(" [gain={0}]", gain.ToStringSignificantDigits(1)));
            return new Convolution2D(values, 3, 3, true, gain, description: description);
        }
        public static Convolution2D GetEdge_Kirsch(bool vertical = true, double gain = 1d)
        {
            //Got this here:
            //http://www.imagemagick.org/Usage/convolve/#kirsch

            double[] values = null;
            if (vertical)
            {
                values = new[]
                {
                    5d, -3d, -3d,
                    5d, 0d, -3d,
                    5d, -3d, -3d,
                };
            }
            else
            {
                values = new[]
                {
                    -3d, -3d, -3d,
                    -3d, 0d, -3d,
                    5d, 5d, 5d,
                };
            }

            values = ToUnit(values);

            string description = string.Format("kirsch{0}", gain.IsNearValue(1) ? "" : string.Format(" [gain={0}]", gain.ToStringSignificantDigits(1)));
            return new Convolution2D(values, 3, 3, true, gain, description: description);
        }
        public static Convolution2D GetEdge_Laplacian(bool positive = true, double gain = 1d)
        {
            //Got this here:
            //http://www.tutorialspoint.com/dip/Laplacian_Operator.htm

            double[] values = null;
            if (positive)
            {
                values = new[]
                {
                    0d, 1d, 0d,
                    1d, -4d, 1d,
                    0d, 1d, 0d,
                };
            }
            else
            {
                values = new[]
                {
                    0d, -1d, 0d,
                    -1d, 4d, -1d,
                    0d, -1d, 0d,
                };
            }

            values = ToUnit(values);

            string description = string.Format("laplacian{0}", gain.IsNearValue(1) ? "" : string.Format(" [gain={0}]", gain.ToStringSignificantDigits(1)));
            return new Convolution2D(values, 3, 3, true, gain, description: description);
        }

        public static ConvolutionSet2D GetEdgeSet_GaussianSubtract()
        {
            return new ConvolutionSet2D(new[] { Convolutions.GetGaussian(3, 1) }, SetOperationType.Subtract, "gaussian subtract");
        }
        public static ConvolutionSet2D GetEdgeSet_Sobel(double gain = 1d)
        {
            Convolution2D vert = GetEdge_Sobel(true, gain);
            Convolution2D horz = GetEdge_Sobel(false, gain);

            var singles = new[]
                {
                    vert,
                    horz,
                    Rotate_45(vert, true),
                    Rotate_45(horz, true),
                };

            return new ConvolutionSet2D(singles, SetOperationType.MaxOf, "max of " + vert.Description);
        }
        public static ConvolutionSet2D GetEdgeSet_GuassianThenEdge(int guassianSize = 3, double guassianStandardDeviationMultiplier = 1d, double edgeGain = 1d)
        {
            ConvolutionBase2D[] convs = new ConvolutionBase2D[]
            {
                GetGaussian(guassianSize, guassianStandardDeviationMultiplier),
                GetEdgeSet_Sobel(edgeGain),
            };

            string guassianDescription = string.Format("gaussian {0}", guassianSize);
            if (!guassianStandardDeviationMultiplier.IsNearValue(1))
            {
                guassianDescription += " x" + guassianStandardDeviationMultiplier.ToStringSignificantDigits(1);
            }

            string edgeDescription = "edge";
            if (!edgeGain.IsNearValue(1))
            {
                edgeDescription += string.Format(" [gain={0}]", edgeGain.ToStringSignificantDigits(1));
            }

            string description = string.Format("{0} then {1}", guassianDescription, edgeDescription);

            return new ConvolutionSet2D(convs, SetOperationType.Standard, description);
        }

        #endregion

        #region WPF helpers

        public static Border GetKernelThumbnail(ConvolutionBase2D kernel, int thumbSize, ContextMenu contextMenu)
        {
            if (kernel is Convolution2D)
            {
                return GetKernelThumbnail_Single((Convolution2D)kernel, thumbSize, contextMenu);
            }
            else if (kernel is ConvolutionSet2D)
            {
                return GetKernelThumbnail_Set((ConvolutionSet2D)kernel, thumbSize, contextMenu);
            }
            else
            {
                throw new ArgumentException("Unknown type of kernel: " + kernel.GetType().ToString());
            }
        }

        /// <summary>
        /// This isn't for processing, just a visual to show to the user
        /// </summary>
        public static BitmapSource GetKernelBitmap(Convolution2D kernel, int sizeMult = 20, bool isNegativeRedBlue = true)
        {
            double min = kernel.Values.Min(o => o);
            double max = kernel.Values.Max(o => o);
            double absMax = Math.Max(Math.Abs(min), Math.Abs(max));

            Color[] colors = null;
            if (!kernel.IsNegPos)
            {
                // 0 to 1
                colors = kernel.Values.
                    Select(o => GetKernelPixelColor_ZeroToOne(o, max)).
                    ToArray();
            }
            else if (isNegativeRedBlue)
            {
                // -1 to 1 (red-blue)
                colors = kernel.Values.
                    Select(o => GetKernelPixelColor_NegPos_RedBlue(o, absMax)).
                    ToArray();
            }
            else
            {
                // -1 to 1 (black-white)
                colors = kernel.Values.
                    Select(o => GetKernelPixelColor_NegPos_BlackWhite(o, absMax)).
                    ToArray();
            }

            return UtilityWPF.GetBitmap_Aliased(colors, kernel.Width, kernel.Height, kernel.Width * sizeMult, kernel.Height * sizeMult);
        }
        public static Color GetKernelPixelColor(double value, double min, double max, double absMax, bool isZeroToOne, bool isNegativeRedBlue = true)
        {
            if (isZeroToOne)
            {
                return GetKernelPixelColor_ZeroToOne(value, max);
            }
            else if (isNegativeRedBlue)
            {
                return GetKernelPixelColor_NegPos_RedBlue(value, absMax);
            }
            else
            {
                return GetKernelPixelColor_NegPos_BlackWhite(value, absMax);
            }
        }

        public static BitmapSource ShowConvolutionResult(Convolution2D result, bool isNegPos, ConvolutionResultNegPosColoring negPosColoring)
        {
            byte[][] colors;

            if (isNegPos)
            {
                switch (negPosColoring)
                {
                    case ConvolutionResultNegPosColoring.BlackWhite:
                        #region negpos - BlackWhite

                        colors = result.Values.Select(o =>
                        {
                            double rgbDbl = Math.Round(Math.Abs(o));

                            if (rgbDbl < 0) rgbDbl = 0;
                            else if (rgbDbl > 255) rgbDbl = 255;

                            byte rgb = Convert.ToByte(rgbDbl);
                            return new byte[] { 255, rgb, rgb, rgb };
                        }).
                        ToArray();

                        #endregion
                        break;

                    case ConvolutionResultNegPosColoring.Gray:
                        #region negpos - Gray

                        colors = result.Values.Select(o =>
                        {
                            double offset = (o / 255d) * 127d;
                            double rgbDbl = 128 + Math.Round(offset);

                            if (rgbDbl < 0) rgbDbl = 0;
                            else if (rgbDbl > 255) rgbDbl = 255;

                            byte rgb = Convert.ToByte(rgbDbl);
                            return new byte[] { 255, rgb, rgb, rgb };
                        }).
                        ToArray();

                        #endregion
                        break;

                    case ConvolutionResultNegPosColoring.RedBlue:
                        #region negpos RedBlue

                        byte[] back = new byte[] { 255, 255, 255, 255 };

                        colors = result.Values.Select(o =>
                        {
                            double rgbDbl = Math.Round(o);

                            if (rgbDbl < -255) rgbDbl = -255;
                            else if (rgbDbl > 255) rgbDbl = 255;

                            byte alpha = Convert.ToByte(Math.Abs(rgbDbl));

                            byte[] fore = null;
                            if (rgbDbl < 0)
                            {
                                fore = new byte[] { alpha, 255, 0, 0 };
                            }
                            else
                            {
                                fore = new byte[] { alpha, 0, 0, 255 };
                            }

                            return UtilityWPF.OverlayColors(new[] { back, fore });
                        }).
                        ToArray();

                        #endregion
                        break;

                    default:
                        throw new ApplicationException("Unknown ConvolutionResultNegPosColoring: " + negPosColoring.ToString());
                }
            }
            else
            {
                #region 0 to 1

                colors = result.Values.Select(o =>
                {
                    double rgbDbl = Math.Round(o);

                    if (rgbDbl < 0) rgbDbl = 0;
                    else if (rgbDbl > 255) rgbDbl = 255;

                    byte rgb = Convert.ToByte(rgbDbl);
                    return new byte[] { 255, rgb, rgb, rgb };
                }).
                ToArray();

                #endregion
            }

            return UtilityWPF.GetBitmap(colors, result.Width, result.Height);
        }

        #endregion

        #region Misc

        private const double MINPERCENT = .03;
        private const double MAXPERCENT = .97;

        public static RectInt GetExtractRectangle(VectorInt imageSize, bool isSquare)
        {
            Random rand = StaticRandom.GetRandomForThread();

            // Min Size
            int minSize = Math.Min(imageSize.X, imageSize.Y);
            double extractSizeMin, extractSizeMax;

            extractSizeMin = minSize * MINPERCENT;
            extractSizeMax = minSize * MAXPERCENT;

            // Width
            int width = rand.NextDouble(extractSizeMin, extractSizeMax).ToInt_Round();
            int height;

            // Height
            if (isSquare)
            {
                height = width;
            }
            else
            {
                height = rand.NextDouble(extractSizeMin, extractSizeMax).ToInt_Round();

                double ratio = width.ToDouble() / height.ToDouble();

                if (ratio > Math3D.GOLDENRATIO)
                {
                    width = (height * Math3D.GOLDENRATIO).ToInt_Round();
                }
                else
                {
                    ratio = height.ToDouble() / width.ToDouble();

                    if (ratio > Math3D.GOLDENRATIO)
                    {
                        height = (width * Math3D.GOLDENRATIO).ToInt_Round();
                    }
                }
            }

            // Rectangle
            return new RectInt()
            {
                X = rand.Next(imageSize.X - width),
                Y = rand.Next(imageSize.Y - height),
                Width = width,
                Height = height,
            };
        }
        public static RectInt GetExtractRectangle(VectorInt imageSize, bool isSquare, double sizePercent)
        {
            const double PERCENTDIFF = .02;

            Random rand = StaticRandom.GetRandomForThread();

            // Min Size
            int minSize = Math.Min(imageSize.X, imageSize.Y);

            // Percents
            double scaledPercent = UtilityCore.GetScaledValue_Capped(MINPERCENT + PERCENTDIFF, MAXPERCENT - PERCENTDIFF, 0, 1, sizePercent);

            double minPercent = scaledPercent - PERCENTDIFF;
            double maxPercent = scaledPercent + PERCENTDIFF;

            // Size
            double extractSizeMin = minSize * minPercent;
            double extractSizeMax = minSize * maxPercent;

            // Width
            int width = rand.NextDouble(extractSizeMin, extractSizeMax).ToInt_Round();
            int height;

            // Height
            if (isSquare)
            {
                height = width;
            }
            else
            {
                if (scaledPercent > .5)
                {
                    height = rand.NextDouble(width / Math3D.GOLDENRATIO, width).ToInt_Round();      // Reduce
                }
                else
                {
                    height = rand.NextDouble(width, width * Math3D.GOLDENRATIO).ToInt_Round();      // Increase
                }
            }

            // Random chance of swapping so height might be taller
            if (rand.NextBool())
            {
                UtilityCore.Swap(ref width, ref height);
            }

            // Rectangle
            return new RectInt()
            {
                X = rand.Next(imageSize.X - width),
                Y = rand.Next(imageSize.Y - height),
                Width = width,
                Height = height,
            };
        }

        public static bool IsTooSmall(VectorInt size, bool allow1x1 = false)
        {
            return IsTooSmall(size.X, size.Y, allow1x1);
        }
        public static bool IsTooSmall(int width, int height, bool allow1x1 = false)
        {
            if (width <= 0 || height <= 0)
            {
                return true;
            }

            if (!allow1x1 && width == 1 && height == 1)
            {
                // A 1x1 patch is worthless.  About the only useful case is if the value is -1, it will invert the image
                return true;
            }

            return false;
        }

        /// <summary>
        /// This looks at a patch that is extracted from the result of an image and a convolution.  This patch should be a relatively
        /// small rectangle centered around a bright spot (convolution.GetMax()).  This returns a score from 0 to 1.  This is looking
        /// for a peak that drops off (ideally a very large difference between the max pixel and min pixel)
        /// </summary>
        /// <remarks>
        /// TODO: May want another overload that takes a sample ideal patch to compare to:
        ///     Subtract patches, compare differences
        ///         line up the brightest point before subtracting
        ///         allow larger differences farther away from center
        /// </remarks>
        public static double GetExtractScore(Convolution2D patch, VectorInt brightestPoint, double? idealBrightness = null)
        {
            const double PERCENT_POS_MIN = .25;
            const double PERCENT_POS_MAX = .75;

            const double PERCENT_NEG_MIN = -.25;
            const double PERCENT_NEG_MAX = .5;

            if (patch.Width < 3 || patch.Height < 3)
            {
                return 0;
            }

            double max = patch[brightestPoint];

            // Get the min.  If not enough difference from max, then return zero
            var min = patch.GetMin();

            double percent = min.Item2 / max;

            double retVal = 0;

            if (patch.IsNegPos)
            {
                #region Neg/Pos

                if (percent < PERCENT_NEG_MIN)
                {
                    retVal = 1;
                }
                else if (percent < PERCENT_NEG_MAX)
                {
                    retVal = UtilityCore.GetScaledValue(1, 0, PERCENT_NEG_MIN, PERCENT_NEG_MAX, percent);
                }
                else
                {
                    return 0;
                }

                #endregion
            }
            else
            {
                #region Positive

                if (percent < PERCENT_POS_MIN)
                {
                    retVal = 1;
                }
                else if (percent < PERCENT_POS_MAX)
                {
                    retVal = UtilityCore.GetScaledValue(1, 0, PERCENT_POS_MIN, PERCENT_POS_MAX, percent);
                }
                else
                {
                    return 0;
                }

                #endregion
            }

            if (idealBrightness == null || max >= idealBrightness.Value)
            {
                return retVal;
            }

            return retVal * (max / idealBrightness.Value);
        }

        /// <summary>
        /// This returns a 0 to 1 convolution.  This should be used as an opacity mask, multiply another convolution's values
        /// by each of mask's values
        /// </summary>
        /// <remarks>
        /// TODO: Take in settings to fine tune the shapes
        /// </remarks>
        public static Convolution2D GetMask_ScatterShot(int width, int height, double percentKeep = .25)
        {
            //TODO: Create a white bitmap and draw random semitransparent black shapes on it

            Random rand = StaticRandom.GetRandomForThread();

            double[] values = Enumerable.Range(0, width * height).
                Select(o => rand.NextDouble() < percentKeep ? 1d : rand.NextDouble()).
                ToArray();

            return new Convolution2D(values, width, height, false, description: "scatter shot mask");
        }
        public static Convolution2D GetMask_Ellipse(int width, int height, int count = 1)
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

                        if (radiusX > radiusY)
                        {
                            double aspect = radiusX / radiusY;
                            if (aspect > MAXASPECT)
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
            Convolution2D retVal = UtilityWPF.ConvertToConvolution(bitmap, 1d, "ellipse mask");

            double test = retVal.Values.Max();

            //NOTE: It's easier to let the background default to black, and draw white shapes on it.  Now it needs to be reversed
            return Convolutions.Invert(retVal, 1d);
        }
        private static Convolution2D GetMask_Triangle(int width, int height, int count = 1)
        {
            // Define a random triangle, define a random linear gradient brush for the opacity
            throw new ApplicationException("finish this");
        }

        #endregion

        #region Private Methods

        private static Convolution2D Convolute_Set(Convolution2D image, ConvolutionSet2D kernel, string description)
        {
            if (kernel.OperationType == SetOperationType.MaxOf)
            {
                // This is special, because it needs to convolute each child, then pick the values it wants to keep.  Also, each child will need to be the same size
                return Convolute_Set_MaxOf(image, kernel, description);
            }

            Convolution2D retVal = image;

            foreach (ConvolutionBase2D child in kernel.Convolutions)
            {
                retVal = Convolute(retVal, child, description);
            }

            if (kernel.OperationType == SetOperationType.Subtract)
            {
                retVal = Convolutions.Subtract(image, retVal, description);
            }

            return retVal;
        }
        private static Convolution2D Convolute_Set_MaxOf(Convolution2D image, ConvolutionSet2D kernel, string description)
        {
            #region validate
#if DEBUG
            if (kernel.OperationType != SetOperationType.MaxOf)
            {
                throw new ArgumentException("kernel must be MaxOf: " + kernel.OperationType.ToString());
            }
            else if (kernel.Convolutions.Length < 2)
            {
                throw new ArgumentException("MaxOf kernel set needs at least two children");
            }

            VectorInt firstReduce = kernel.Convolutions[0].GetReduction();

            for (int cntr = 1; cntr < kernel.Convolutions.Length; cntr++)
            {
                VectorInt nextReduce = kernel.Convolutions[cntr].GetReduction();

                if (firstReduce.X != nextReduce.X || firstReduce.Y != nextReduce.Y)
                {
                    throw new ArgumentException("When the operation is MaxOf, then all kernels must reduce the same amount");
                }
            }
#endif
            #endregion

            Convolution2D[] children = kernel.Convolutions.
                Select(o => Convolute(image, o, description)).
                ToArray();

            double[] retVal = new double[children[0].Values.Length];

            for (int index = 0; index < retVal.Length; index++)
            {
                double maxValue = 0;
                double maxAbsValue = 0;

                for (int cntr = 0; cntr < children.Length; cntr++)
                {
                    double abs = Math.Abs(children[cntr].Values[index]);
                    if (abs > maxAbsValue)
                    {
                        maxValue = children[cntr].Values[index];
                        maxAbsValue = abs;
                    }
                }

                retVal[index] = maxValue;
            }

            return new Convolution2D(retVal, children[0].Width, children[0].Height, image.IsNegPos || children.Any(o => o.IsNegPos), description: description);
        }
        /// <summary>
        /// This applies the convolution kernel to the image
        /// </summary>
        /// <remarks>
        /// http://homepages.inf.ed.ac.uk/rbf/HIPR2/convolve.htm
        /// http://homepages.inf.ed.ac.uk/rbf/HIPR2/gsmooth.htm
        /// http://homepages.inf.ed.ac.uk/rbf/HIPR2/sobel.htm
        /// http://homepages.inf.ed.ac.uk/rbf/HIPR2/canny.htm
        ///
        /// http://www.tannerhelland.com/952/edge-detection-vb6/
        /// </remarks>
        /// <param name="image">This is the original image</param>
        /// <param name="kernel">This is the kernel image (a small patch that will be slid over image)</param>
        /// <param name="expandBorder">
        /// True: The output image will be the same size as the input (but border cells need to be made up for this to happen)
        /// False: The output image will be smaller (output.W = image.W - kernel.W + 1, same with height)
        /// </param>
        /// <returns></returns>
        private static Convolution2D Convolute_Single(Convolution2D image, Convolution2D kernel, string description)
        {
            Convolution2D retVal = image;

            for (int cntr = 0; cntr < kernel.Iterations; cntr++)
            {
                if (kernel.ExpandBorder)
                {
                    retVal = Convolute_ExpandedBorder(retVal, kernel, description);
                }
                else
                {
                    retVal = Convolute_Standard(retVal, kernel, description);
                }
            }

            return retVal;
        }

        private static Convolution2D Convolute_ExpandedBorder(Convolution2D image, Convolution2D kernel, string description)
        {
            throw new ApplicationException("finish this");
        }
        private static Convolution2D Convolute_Standard(Convolution2D image, Convolution2D kernel, string description)
        {
            int returnWidth = image.Width - kernel.Width + 1;
            int returnHeight = image.Height - kernel.Height + 1;

            if (returnWidth <= 0 || returnHeight <= 0)
            {
                throw new ArgumentException(string.Format("The kernel is too large for the image.  Image={0}x{1}, Kernel={2}x{3}", image.Width, image.Height, kernel.Width, kernel.Height));
            }

            double[] values = new double[returnWidth * returnHeight];

            for (int retY = 0; retY < returnHeight; retY++)
            {
                int returnOffset = retY * returnWidth;

                for (int retX = 0; retX < returnWidth; retX++)
                {
                    double returnValue = 0;

                    // Calculate the convoluted value at this return pixel
                    for (int kerY = 0; kerY < kernel.Height; kerY++)
                    {
                        int imageOffset = (retY + kerY) * image.Width;
                        int kernelOffet = kerY * kernel.Width;

                        for (int kerX = 0; kerX < kernel.Width; kerX++)
                        {
                            returnValue += image.Values[imageOffset + retX + kerX] * kernel.Values[kernelOffet + kerX];
                        }
                    }

                    // Store it
                    values[returnOffset + retX] = returnValue * kernel.Gain;
                }
            }

            return new Convolution2D(values, returnWidth, returnHeight, image.IsNegPos || kernel.IsNegPos, description: description);
        }

        private static int Subtract_GetOffset(int diff)
        {
            int retVal = diff / 2;

            if (diff % 2 == 0)
            {
                retVal++;
            }

            return retVal;
        }

        /// <summary>
        /// This makes the values add up to the sum.  This expects all values to be the same sign
        /// </summary>
        private static double[] SumTo(IEnumerable<double> values, double sumTo)
        {
            double currentSum = Math.Abs(values.Sum());     // need to take the absolute value, or the final sign would be lost (-1/-4 would be positive)

            if (Math3D.IsNearValue(currentSum, sumTo))
            {
                return values.ToArray();
            }

            return values.
                Select(o => o / currentSum).
                ToArray();
        }

        private static Border GetKernelThumbnail_Single(Convolution2D kernel, int thumbSize, ContextMenu contextMenu)
        {
            // Figure out thumb size
            double width, height;
            if (kernel.Width == kernel.Height)
            {
                width = height = thumbSize;
            }
            else if (kernel.Width > kernel.Height)
            {
                width = thumbSize;
                height = Convert.ToDouble(kernel.Height) / Convert.ToDouble(kernel.Width) * thumbSize;
            }
            else
            {
                height = thumbSize;
                width = Convert.ToDouble(kernel.Width) / Convert.ToDouble(kernel.Height) * thumbSize;
            }

            int pixelWidth = Convert.ToInt32(Math.Ceiling(width / kernel.Width));
            int pixelHeight = Convert.ToInt32(Math.Ceiling(height / kernel.Height));

            int pixelMult = Math.Max(pixelWidth, pixelHeight);
            if (pixelMult < 1)
            {
                pixelMult = 1;
            }

            string tooltip = string.Format("{0}x{1}", kernel.Width, kernel.Height);
            if (!string.IsNullOrEmpty(kernel.Description))
            {
                tooltip = kernel.Description + "\r\n" + tooltip;
            }

            // Display it as a border and image
            Image image = new Image()
            {
                Source = GetKernelBitmap(kernel, pixelMult),
                Width = width,
                Height = height,
                ToolTip = tooltip,
            };

            Border border = new Border()
            {
                Child = image,
                BorderBrush = new SolidColorBrush(UtilityWPF.ColorFromHex("C0C0C0")),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(6),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                ContextMenu = contextMenu,
                Tag = kernel,
            };

            return border;
        }
        private static Border GetKernelThumbnail_Set(ConvolutionSet2D kernel, int thumbSize, ContextMenu contextMenu)
        {
            StackPanel children = new StackPanel()
            {
                Orientation = Orientation.Horizontal,
            };

            int childSize = Convert.ToInt32(thumbSize * .9);
            double secondChildShift = thumbSize * .33;

            foreach (ConvolutionBase2D child in kernel.Convolutions)
            {
                //NOTE: It doesn't work to apply skew transforms to a border that has children.  Instead, make a visual brush out of the child
                Border childCtrl = GetKernelThumbnail(child, childSize, null);
                childCtrl.Margin = new Thickness(0);

                Border actualChild = new Border();
                actualChild.Background = new VisualBrush() { Visual = childCtrl };

                double width = 0;
                double height = 0;
                if (child is Convolution2D)
                {
                    width = ((FrameworkElement)childCtrl.Child).Width;
                    height = ((FrameworkElement)childCtrl.Child).Height;
                }
                else //if(child is ConvolutionSet2D)
                {
                    // There doesn't seem to be a way to get the child composite's size (it's NaN).  Maybe there's a way to force it to do layout?
                    width = thumbSize;
                    height = thumbSize;
                }

                actualChild.Width = width;
                actualChild.Height = height;

                //NOTE: I wanted a trapazoid transform, but that is impossible with a 3x3 matrix.  The only way would be to render in 3D
                TransformGroup tranform = new TransformGroup();
                tranform.Children.Add(new ScaleTransform(.75, 1));
                tranform.Children.Add(new SkewTransform(0, -30));
                actualChild.LayoutTransform = tranform;

                if (children.Children.Count > 0)
                {
                    actualChild.Margin = new Thickness(-secondChildShift, 0, 0, 0);
                }

                actualChild.IsHitTestVisible = false;       // setting this to false so that click events come from the returned border instead of the child

                children.Children.Add(actualChild);
            }

            Border border = new Border()
            {
                Child = children,

                Background = new SolidColorBrush(UtilityWPF.ColorFromHex("28F2B702")),
                BorderBrush = new SolidColorBrush(UtilityWPF.ColorFromHex("F5CC05")),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(6),
                Padding = new Thickness(3),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                ContextMenu = contextMenu,
                Tag = kernel,
            };

            if (!string.IsNullOrEmpty(kernel.Description))
            {
                border.ToolTip = kernel.Description;
            }

            return border;
        }

        private static Color GetKernelPixelColor_ZeroToOne(double value, double max)
        {
            if (max.IsNearZero())
            {
                return Colors.Black;
            }

            double scaled = (value / max) * 255d;       // need to scale to max, because the sum of the cells is 1.  So if it's not scaled, the bitmap will be nearly black
            if (scaled < 0)
            {
                scaled = 0;
            }

            byte rgb = Convert.ToByte(Math.Round(scaled));
            return Color.FromRgb(rgb, rgb, rgb);
        }
        private static Color GetKernelPixelColor_NegPos_RedBlue(double value, double absMax)
        {
            if (absMax.IsNearZero())
            {
                return Colors.White;
            }

            byte[] white = new byte[] { 255, 255, 255, 255 };

            double scaled = (Math.Abs(value) / absMax) * 255d;      //NOTE: Can't use a posMax and negMax, because the black/white will be deceptive
            byte opacity = Convert.ToByte(Math.Round(scaled));

            byte[] color = new byte[4];
            color[0] = opacity;
            color[1] = Convert.ToByte(value < 0 ? 255 : 0);
            color[2] = 0;
            color[3] = Convert.ToByte(value > 0 ? 255 : 0);

            color = UtilityWPF.OverlayColors(new[] { white, color });

            return Color.FromRgb(color[1], color[2], color[3]);     // the white background is fully opaque, so there's no need for the alpha overload
        }
        private static Color GetKernelPixelColor_NegPos_BlackWhite(double value, double absMax)
        {
            if (absMax.IsNearZero())
            {
                return Colors.Gray;
            }

            double offset = (value / absMax) * 127d;        //NOTE: Can't use a posMax and negMax, because the black/white will be deceptive

            byte rgb = Convert.ToByte(128 + Math.Round(offset));

            return Color.FromRgb(rgb, rgb, rgb);
        }

        private static void Rotate45_Ring(double[] source, double[] destination, int size, int fullSize, bool isClockwise, bool shouldAdvanceEvensExtra)
        {
            int fullOffset = (fullSize - size) / 2;

            if (size == 1)
            {
                // Center, just copy it
                int index = (fullOffset * fullSize) + fullOffset;
                destination[index] = source[index];
                return;
            }

            int offset = size / 2;
            if (size % 2 == 0)
            {
                // This rotates them 90
                offset += (size / 2) - 1;

                // Now cut in half to get 45
                offset /= 2;

                if (shouldAdvanceEvensExtra)
                {
                    offset += 1;
                }
            }

            if (!isClockwise)
            {
                offset = -offset;
            }

            int size_1 = size - 1;
            int ringLength = size_1 * 4;

            // Walk around the outer ring, copying to offset
            for (int ringIndexSrc = 0; ringIndexSrc < ringLength; ringIndexSrc++)
            {
                // Convert ringIndex1 into an x,y
                var pointSrc = MapRing_1D_2D(ringIndexSrc, size_1);

                int ringIndexDst = ringIndexSrc + offset;
                if (ringIndexDst < 0)
                {
                    ringIndexDst += ringLength;
                }
                else if (ringIndexDst >= ringLength)
                {
                    ringIndexDst -= ringLength;
                }

                // Convert ringIndex2 into an x,y
                var pointDst = MapRing_1D_2D(ringIndexDst, size_1);

                int indexSource = ((pointSrc.Item2 + fullOffset) * fullSize) + pointSrc.Item1 + fullOffset;
                int indexDest = ((pointDst.Item2 + fullOffset) * fullSize) + pointDst.Item1 + fullOffset;

                destination[indexDest] = source[indexSource];
            }
        }
        private static Tuple<int, int> MapRing_1D_2D(int index, int size_1)
        {
            int section = index / size_1;

            switch (section)
            {
                case 0:     // top edge
                    return Tuple.Create(index, 0);

                case 1:     // right edge
                    return Tuple.Create(size_1, index - size_1);

                case 2:     // bottom edge
                    int offset1 = index - size_1;
                    return Tuple.Create(size_1 - (offset1 - size_1), size_1);

                case 3:     // left edge
                    int offset2 = index - (size_1 * 2);
                    return Tuple.Create(0, size_1 - (offset2 - size_1));

                default:
                    throw new ApplicationException(string.Format("Unexpected section.  index={0}, size-1={1}, section={2}", index, size_1, section));
            }
        }

        #endregion
    }

    #region Class: ConvolutionSet2D

    public class ConvolutionSet2D : ConvolutionBase2D
    {
        #region Constructor

        public ConvolutionSet2D(ConvolutionSet2D_DNA dna)
        {
            List<ConvolutionBase2D> convolutions = new List<ConvolutionBase2D>();

            if (dna.Convolutions_Single != null)
            {
                convolutions.AddRange(dna.Convolutions_Single.Select(o => new Convolution2D(o)));
            }

            if (dna.Convolutions_Set != null)
            {
                convolutions.AddRange(dna.Convolutions_Set.Select(o => new ConvolutionSet2D(o)));
            }

            this.Convolutions = convolutions.ToArray();
            this.OperationType = dna.OperationType;
            _description = dna.Description;
        }

        public ConvolutionSet2D(ConvolutionBase2D[] convolutions, SetOperationType operationType, string description = "")
        {
            foreach (object child in convolutions)
            {
                if (!(child is Convolution2D) && !(child is ConvolutionSet2D))
                {
                    throw new ArgumentException("Object passed in must be Convolution2D or ConvolutionSet2D: " + child.GetType().ToString());
                }
            }

            this.Convolutions = convolutions;
            this.OperationType = operationType;
            _description = description;
        }

        #endregion

        public readonly ConvolutionBase2D[] Convolutions;

        public readonly SetOperationType OperationType;

        public override bool IsNegPos
        {
            get
            {
                if (this.OperationType == SetOperationType.Subtract)
                {
                    return true;
                }

                return this.Convolutions.Any(o => o.IsNegPos);
            }
        }

        private readonly string _description;
        public override string Description
        {
            get
            {
                return _description;
            }
        }

        #region Public Methods

        public override VectorInt GetReduction()
        {
            VectorInt retVal = new VectorInt(0, 0);

            foreach (ConvolutionBase2D child in this.Convolutions)
            {
                var childReduce = child.GetReduction();

                retVal.X += childReduce.X;
                retVal.Y += childReduce.Y;
            }

            return retVal;
        }

        public ConvolutionSet2D_DNA ToDNA()
        {
            Convolution2D_DNA[] singles = null;
            ConvolutionSet2D_DNA[] sets = null;

            if (this.Convolutions != null)
            {
                singles = this.Convolutions.
                    Where(o => o is Convolution2D).
                    Select(o => ((Convolution2D)o).ToDNA()).
                    ToArray();

                sets = this.Convolutions.
                    Where(o => o is ConvolutionSet2D).
                    Select(o => ((ConvolutionSet2D)o).ToDNA()).
                    ToArray();
            }

            return new ConvolutionSet2D_DNA()
            {
                Convolutions_Single = singles,
                Convolutions_Set = sets,
                IsNegPos = this.IsNegPos,
                OperationType = this.OperationType,
                Description = this.Description,
            };
        }

        #endregion
    }

    #endregion
    #region Enum: SetOperationType

    public enum SetOperationType
    {
        Standard,
        Subtract,
        MaxOf,
        //TODO: Sharpen - does an edge detect, then adds those edges to the original image (probably call it Add)
        //http://www.tutorialspoint.com/dip/Concept_of_Edge_Detection.htm
    }

    #endregion

    #region Class: Convolution2D

    public class Convolution2D : ConvolutionBase2D
    {
        #region Constructor

        public Convolution2D(Convolution2D_DNA dna)
            : this(dna.Values, dna.Width, dna.Height, dna.IsNegPos, dna.Gain, dna.Iterations, dna.ExpandBorder, dna.Description) { }

        public Convolution2D(double[] values, int width, int height, bool isNegPos, double gain = 1d, int iterations = 1, bool expandBorder = false, string description = "")
        {
            if (values.Length != width * height)
            {
                throw new ArgumentException(string.Format("The array passed in isn't rectangular.  ArrayLength={0}, Width={1}, Height={2}", values.Length, width, height));
            }

            this.Values = values;
            this.Width = width;
            this.Height = height;
            _isNegPos = isNegPos;
            _description = description;
            this.Gain = gain;
            this.Iterations = iterations;
            this.ExpandBorder = expandBorder;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Range is either 0 to 1, or -1 to 1 (depends on this.IsNegPos)
        /// </summary>
        public readonly double[] Values;

        public readonly int Width;
        public readonly int Height;

        private readonly bool _isNegPos;
        public override bool IsNegPos
        {
            get
            {
                return _isNegPos;
            }
        }

        private readonly string _description;
        public override string Description
        {
            get
            {
                return _description;
            }
        }

        /// <summary>
        /// If this is true, then the convoluted image will be the same size as the input image (at the expense of making up data along
        /// the bottom right border)
        /// </summary>
        public readonly bool ExpandBorder;

        /// <summary>
        /// This acts like a multiplier
        /// </summary>
        public readonly double Gain;
        /// <summary>
        /// The kernel will be applied this many times
        /// </summary>
        public readonly int Iterations;

        public double this[int index]
        {
            get
            {
                return this.Values[index];
            }
            set
            {
                this.Values[index] = value;
            }
        }
        public double this[int x, int y]
        {
            get
            {
                return this.Values[(y * this.Width) + x];
            }
            set
            {
                this.Values[(y * this.Width) + x] = value;
            }
        }
        public double this[VectorInt pos]
        {
            get
            {
                return this.Values[(pos.Y * this.Width) + pos.X];
            }
            set
            {
                this.Values[(pos.Y * this.Width) + pos.X] = value;
            }
        }

        public VectorInt Size
        {
            get
            {
                return new VectorInt(this.Width, this.Height);
            }
        }

        #endregion

        #region Public Methods

        public override VectorInt GetReduction()
        {
            VectorInt retVal = new VectorInt();

            if (!this.ExpandBorder)
            {
                retVal.X = (this.Width - 1) * this.Iterations;
                retVal.Y = (this.Height - 1) * this.Iterations;
            }

            return retVal;
        }

        public Convolution2D Extract(RectInt rect, ConvolutionExtractType extractType)
        {
            bool isNegPos = this.IsNegPos;
            double[] values = ExtractValues(rect);

            switch (extractType)
            {
                case ConvolutionExtractType.Raw:
                    break;

                case ConvolutionExtractType.RawUnit:
                    values = Convolutions.ToUnit(values);
                    break;

                case ConvolutionExtractType.Edge:
                    isNegPos = true;
                    values = ConvertToEdge(values);
                    values = Convolutions.ToUnit(values);
                    break;

                case ConvolutionExtractType.EdgeSoftBorder:
                    isNegPos = true;
                    values = ConvertToEdge(values);
                    values = ApplySoftBorder(values, rect.Width, rect.Height);
                    values = Convolutions.ToUnit(values);
                    break;

                //case ConvolutionExtractType.EdgeCircleBorder:
                //    isNegPos = true;
                //    values = ConvertToEdge(values);
                //    values = ApplyCircleBorder(values);
                //    values = Convolutions.ToUnit(values);
                //    break;

                default:
                    throw new ApplicationException("Unknown ConvolutionExtractType: " + extractType.ToString());
            }

            return new Convolution2D(values, rect.Width, rect.Height, isNegPos, this.Gain, this.Iterations, this.ExpandBorder);
        }

        public Tuple<VectorInt, double> GetMin()
        {
            double min = double.MaxValue;
            int index = -1;

            for (int cntr = 0; cntr < this.Values.Length; cntr++)
            {
                if (this.Values[cntr] < min)
                {
                    min = this.Values[cntr];
                    index = cntr;
                }
            }

            int y = index / this.Width;
            int x = index - (y * this.Width);

            return Tuple.Create(new VectorInt(x, y), min);
        }
        public Tuple<VectorInt, double> GetMin(IEnumerable<RectInt> ignore)
        {
            double min = double.MaxValue;
            int bX = -1;
            int bY = -1;

            //TODO: Instead of doing a test for each pixel, get a set of patches that are unblocked
            //Tuple<AxisFor, AxisFor> openPatches = 

            for (int y = 0; y < this.Height; y++)
            {
                int offsetY = y * this.Width;

                for (int x = 0; x < this.Width; x++)
                {
                    if (ignore.Any(o => o.Contains(x, y)))
                    {
                        continue;
                    }

                    if (this.Values[offsetY + x] < min)
                    {
                        min = this.Values[offsetY + x];
                        bX = x;
                        bY = y;
                    }
                }
            }

            return Tuple.Create(new VectorInt(bX, bY), min);
        }

        public Tuple<VectorInt, double> GetMax()
        {
            double max = double.MinValue;
            int index = -1;

            for (int cntr = 0; cntr < this.Values.Length; cntr++)
            {
                if (this.Values[cntr] > max)
                {
                    max = this.Values[cntr];
                    index = cntr;
                }
            }

            int y = index / this.Width;
            int x = index - (y * this.Width);

            return Tuple.Create(new VectorInt(x, y), max);
        }
        public Tuple<VectorInt, double> GetMax(IEnumerable<RectInt> ignore)
        {
            double max = double.MinValue;
            int bX = -1;
            int bY = -1;

            //TODO: Instead of doing a test for each pixel, get a set of patches that are unblocked
            //Tuple<AxisFor, AxisFor> openPatches = 

            for (int y = 0; y < this.Height; y++)
            {
                int offsetY = y * this.Width;

                for (int x = 0; x < this.Width; x++)
                {
                    if (ignore.Any(o => o.Contains(x, y)))
                    {
                        continue;
                    }

                    if (this.Values[offsetY + x] > max)
                    {
                        max = this.Values[offsetY + x];
                        bX = x;
                        bY = y;
                    }
                }
            }

            return Tuple.Create(new VectorInt(bX, bY), max);
        }

        public Convolution2D_DNA ToDNA()
        {
            return new Convolution2D_DNA()
            {
                Values = this.Values,
                Width = this.Width,
                Height = this.Height,

                IsNegPos = this.IsNegPos,
                Description = this.Description,

                Gain = this.Gain,
                Iterations = this.Iterations,
                ExpandBorder = this.ExpandBorder,
            };
        }

        public override string ToString()
        {
            StringBuilder retVal = new StringBuilder(100);

            retVal.Append(string.Format("size=({0}, {1})", this.Width, this.Height));

            retVal.Append(string.Format(" [is negpos={0}]", this.IsNegPos));

            if (!this.Gain.IsNearValue(1))
            {
                retVal.Append(string.Format(" [gain={0}]", this.Gain.ToStringSignificantDigits(2)));
            }

            if (this.Iterations != 1)
            {
                retVal.Append(string.Format(" [iterations={0}]", this.Iterations));
            }

            if (this.ExpandBorder)
            {
                retVal.Append(" [expand border]");
            }

            if (!string.IsNullOrWhiteSpace(this.Description))
            {
                retVal.Append(string.Format(" \"{0}\"", this.Description));
            }

            return retVal.ToString();
        }

        #endregion

        #region Private Methods

        private double[] ExtractValues(RectInt rect)
        {
            double[] retVal = new double[rect.Width * rect.Height];

            for (int y = 0; y < rect.Height; y++)
            {
                int yOffsetSrc = (y + rect.Y) * this.Width;
                int yOffsetDest = y * rect.Width;

                for (int x = 0; x < rect.Width; x++)
                {
                    retVal[yOffsetDest + x] = this.Values[yOffsetSrc + x + rect.X];
                }
            }

            return retVal;
        }

        private static double[] ApplySoftBorder(double[] values, int width, int height)
        {
            const double MARGINPERCENT = .22;

            int leftEdge = (width * MARGINPERCENT).ToInt_Round();
            int rightEdge = width - leftEdge;

            int topEdge = (height * MARGINPERCENT).ToInt_Round();
            int bottomEdge = height - topEdge;

            // Single gradient
            ApplySoftBorder_Single(values, leftEdge, rightEdge, 0, topEdge, true, false, width);       // Top
            ApplySoftBorder_Single(values, leftEdge, rightEdge, bottomEdge, height - 1, false, false, width);      // Bottom
            ApplySoftBorder_Single(values, 0, leftEdge, topEdge, bottomEdge, true, true, width);      // Left
            ApplySoftBorder_Single(values, rightEdge, width - 1, topEdge, bottomEdge, false, true, width);        // Right

            // Double gradient
            ApplySoftBorder_Double(values, 0, leftEdge, 0, topEdge, true, true, width);     // TopLeft
            ApplySoftBorder_Double(values, rightEdge, width - 1, 0, topEdge, false, true, width);       // TopRight
            ApplySoftBorder_Double(values, 0, leftEdge, bottomEdge, height - 1, true, false, width);       // BottomLeft
            ApplySoftBorder_Double(values, rightEdge, width - 1, bottomEdge, height - 1, false, false, width);       // BottomRight

            return values;
        }
        private static void ApplySoftBorder_Single(double[] values, int xFrom, int xTo, int yFrom, int yTo, bool isUp, bool useX, int width)
        {
            double percFrom = isUp ? 0d : 1d;
            double percTo = isUp ? 1d : 0d;

            double percent = 0;

            for (int y = yFrom; y <= yTo; y++)
            {
                int yIndex = y * width;

                if (!useX)
                {
                    percent = UtilityCore.GetScaledValue(percFrom, percTo, yFrom, yTo, y);
                }

                for (int x = xFrom; x <= xTo; x++)
                {
                    if (useX)
                    {
                        percent = UtilityCore.GetScaledValue(percFrom, percTo, xFrom, xTo, x);
                    }

                    values[yIndex + x] *= percent;
                }
            }
        }
        private static void ApplySoftBorder_Double(double[] values, int xFrom, int xTo, int yFrom, int yTo, bool isUpX, bool isUpY, int width)
        {
            double percFromX = isUpX ? 0d : 1d;
            double percToX = isUpX ? 1d : 0d;

            double percFromY = isUpY ? 0d : 1d;
            double percToY = isUpY ? 1d : 0d;

            int xFromActual = isUpX ? xFrom : xFrom + 1;      // the shift logic needs to be done here, because the full from/to are needed in order for the percents to be correct
            int xToActual = isUpX ? xTo - 1 : xTo;
            int yFromActual = isUpY ? yFrom : yFrom + 1;
            int yToActual = isUpY ? yTo - 1 : yTo;

            //int xFromActual = xFrom;      // the shift logic needs to be done here, because the full from/to are needed in order for the percents to be correct
            //int xToActual = xTo;
            //int yFromActual = yFrom;
            //int yToActual = yTo;

            for (int y = yFromActual; y <= yToActual; y++)
            {
                int yIndex = y * width;

                double percentY = UtilityCore.GetScaledValue(percFromY, percToY, yFrom, yTo, y);

                for (int x = xFromActual; x <= xToActual; x++)
                {
                    double percentX = UtilityCore.GetScaledValue(percFromX, percToX, xFrom, xTo, x);
                    double percent = Math.Min(percentX, percentY);

                    values[yIndex + x] *= percent;
                }
            }
        }

        private static double[] ApplyCircleBorder(double[] values)
        {
            return values;
        }

        private static double[] ConvertToEdge(double[] values)
        {
            var minmax = Math3D.MinMax(values);
            double middle = Math3D.Avg(minmax.Item1, minmax.Item2);

            double[] offsets = values.
                Select(o => o - middle).
                ToArray();

            return offsets;
        }

        #endregion
    }

    #endregion

    #region Class: ConvolutionBase2D

    public abstract class ConvolutionBase2D
    {
        public abstract bool IsNegPos { get; }

        public abstract string Description { get; }

        public abstract VectorInt GetReduction();
    }

    #endregion

    #region Enum: ConvolutionResultNegPosColoring

    public enum ConvolutionResultNegPosColoring
    {
        Gray,
        BlackWhite,
        RedBlue,
    }

    #endregion
    #region Enum: ConvolutionExtractType

    public enum ConvolutionExtractType
    {
        Raw,
        RawUnit,
        Edge,
        /// <summary>
        /// This tapers to zero at the border
        /// </summary>
        EdgeSoftBorder,
        //EdgeCircleBorder,
    }

    #endregion

    #region Class: ConvolutionSet2D_DNA

    public class ConvolutionSet2D_DNA : ConvolutionBase2D_DNA
    {
        public string Description { get; set; }

        public Convolution2D_DNA[] Convolutions_Single { get; set; }
        public ConvolutionSet2D_DNA[] Convolutions_Set { get; set; }

        public SetOperationType OperationType { get; set; }
    }

    #endregion
    #region Class: Convolution2D_DNA

    public class Convolution2D_DNA : ConvolutionBase2D_DNA
    {
        public string Description { get; set; }

        [TypeConverter(typeof(DblArrTypeConverter))]
        public double[] Values { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }

        public bool ExpandBorder { get; set; }
        public double Gain { get; set; }
        public int Iterations { get; set; }
    }

    #endregion
    #region Class: ConvolutionBase2D_DNA

    public abstract class ConvolutionBase2D_DNA
    {
        public bool IsNegPos { get; set; }
    }

    #endregion
}
