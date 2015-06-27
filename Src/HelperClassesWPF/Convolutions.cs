using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.HelperClassesCore;

namespace Game.HelperClassesWPF
{
    public static class Convolutions
    {
        public static Convolution2D Convolute(Convolution2D image, ConvolutionBase2D kernel)
        {
            if (kernel is Convolution2D)
            {
                return Convolute_Single(image, (Convolution2D)kernel);
            }
            else if (kernel is ConvolutionSet2D)
            {
                return Convolute_Set(image, (ConvolutionSet2D)kernel);
            }
            else
            {
                throw new ApplicationException("Unexpected type of kernel: " + kernel.GetType().ToString());
            }
        }

        //NOTE: This ignores gain and iterations
        public static Convolution2D Subtract(Convolution2D orig, Convolution2D filtered)
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

            return new Convolution2D(values, width, height, true);
        }

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

            return new Convolution2D(values, size, size, false);
        }

        public static Convolution2D GetEdge_Sobel(bool vertical = true)
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

            return new Convolution2D(values, 3, 3, true);
        }
        public static Convolution2D GetEdge_Prewitt(bool vertical = true)
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

            return new Convolution2D(values, 3, 3, true);
        }
        public static Convolution2D GetEdge_Compass(bool vertical = true)
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

            return new Convolution2D(values, 3, 3, true);
        }
        public static Convolution2D GetEdge_Kirsch(bool vertical = true)
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

            return new Convolution2D(values, 3, 3, true);
        }

        #region Private Methods

        private static Convolution2D Convolute_Set(Convolution2D image, ConvolutionSet2D kernel)
        {
            if (kernel.OperationType == SetOperationType.MaxOf)
            {
                // This is special, because it needs to convolute each child, then pick the values it wants to keep.  Also, each child will need to be the same size
                return Convolute_Set_MaxOf(image, kernel);
            }

            Convolution2D retVal = image;

            foreach (ConvolutionBase2D child in kernel.Convolutions)
            {
                retVal = Convolute(retVal, child);
            }

            if (kernel.OperationType == SetOperationType.Subtract)
            {
                retVal = Convolutions.Subtract(image, retVal);
            }

            return retVal;
        }
        private static Convolution2D Convolute_Set_MaxOf(Convolution2D image, ConvolutionSet2D kernel)
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

            Tuple<int, int> firstReduce = kernel.Convolutions[0].GetReduction();

            for (int cntr = 1; cntr < kernel.Convolutions.Length; cntr++)
            {
                Tuple<int, int> nextReduce = kernel.Convolutions[cntr].GetReduction();

                if (firstReduce.Item1 != nextReduce.Item1 || firstReduce.Item2 != nextReduce.Item2)
                {
                    throw new ArgumentException("When the operation is MaxOf, then all kernels must reduce the same amount");
                }
            }
#endif
            #endregion

            Convolution2D[] children = kernel.Convolutions.
                Select(o => Convolute(image, o)).
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

            return new Convolution2D(retVal, children[0].Width, children[0].Height, image.IsNegPos || children.Any(o => o.IsNegPos));
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
        private static Convolution2D Convolute_Single(Convolution2D image, Convolution2D kernel)
        {
            Convolution2D retVal = image;

            for (int cntr = 0; cntr < kernel.Iterations; cntr++)
            {
                if (kernel.ExpandBorder)
                {
                    retVal = Convolute_ExpandedBorder(retVal, kernel);
                }
                else
                {
                    retVal = Convolute_Standard(retVal, kernel);
                }
            }

            return retVal;
        }

        private static Convolution2D Convolute_ExpandedBorder(Convolution2D image, Convolution2D kernel)
        {
            throw new ApplicationException("finish this");
        }
        private static Convolution2D Convolute_Standard(Convolution2D image, Convolution2D kernel)
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

            return new Convolution2D(values, returnWidth, returnHeight, image.IsNegPos || kernel.IsNegPos);
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

        #endregion
    }

    #region Class: Convolution2D

    public class Convolution2D : ConvolutionBase2D
    {
        public Convolution2D(double[] values, int width, int height, bool isNegPos, double gain = 1d, int iterations = 1, bool expandBorder = false)
        {
            if (values.Length != width * height)
            {
                throw new ArgumentException(string.Format("The array passed in isn't rectangular.  ArrayLength={0}, Width={1}, Height={2}", values.Length, width, height));
            }

            this.Values = values;
            this.Width = width;
            this.Height = height;
            _isNegPos = isNegPos;
            this.Gain = gain;
            this.Iterations = iterations;
            this.ExpandBorder = expandBorder;
        }

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

        public override Tuple<int, int> GetReduction()
        {
            int reduceX = 0;
            int reduceY = 0;

            if (!this.ExpandBorder)
            {
                reduceX = (this.Width - 1) * this.Iterations;
                reduceY = (this.Height - 1) * this.Iterations;
            }

            return Tuple.Create(reduceX, reduceY);
        }
    }

    #endregion

    #region Class: ConvolutionSet2D

    public class ConvolutionSet2D : ConvolutionBase2D
    {
        public ConvolutionSet2D(ConvolutionBase2D[] convolutions, SetOperationType operationType)
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
        }

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

        public override Tuple<int, int> GetReduction()
        {
            int reduceX = 0;
            int reduceY = 0;

            foreach (ConvolutionBase2D child in this.Convolutions)
            {
                var childReduce = child.GetReduction();

                reduceX += childReduce.Item1;
                reduceY += childReduce.Item2;
            }

            return Tuple.Create(reduceX, reduceY);
        }
    }

    #endregion
    #region Enum: SetOperationType

    public enum SetOperationType
    {
        Standard,
        Subtract,
        MaxOf,
    }

    #endregion

    #region Class: ConvolutionBase2D

    public abstract class ConvolutionBase2D
    {
        public abstract bool IsNegPos { get; }

        public abstract Tuple<int, int> GetReduction();
    }

    #endregion
}
