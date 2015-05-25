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
using Encog.Engine.Network.Activation;
using Encog.Neural.Data.Basic;
using Encog.Neural.Networks;
using Encog.Neural.Networks.Layers;
using Encog.Neural.Networks.Training;
using Encog.Neural.Networks.Training.Propagation.Resilient;
using Encog.Neural.NeuralData;
using Game.HelperClassesAI;

namespace Game.Newt.Testers.Encog
{
    public partial class EncogXOR : Window
    {
        #region Class: NNResultsVisual

        private class NNResultsVisual : FrameworkElement
        {
            private readonly DrawingVisual _visual;

            public NNResultsVisual(double width, double height, Tuple<Point, double, bool>[] values, bool shouldOutlineNonMatches)
            {
                #region Min/Max Value, Colors

                bool hasNegative = values.Any(o => o.Item2 < 0);
                double maxValue = Math.Max(Math.Abs(values.Min(o => o.Item2)), Math.Abs(values.Max(o => o.Item2)));
                if (maxValue < 1d)      // should never be greater, but leave alone if it is
                {
                    maxValue = 1d;
                }

                Color positiveColor = hasNegative ? Colors.Blue : Colors.Black;
                Color negativeColor = Colors.Red;

                #endregion
                #region XY Scale

                double radius = ((width + height) / 2) / 100;

                bool hasNegativePosition = values.Any(o => o.Item1.X < 0 || o.Item1.Y < 0);
                double maxX = Math.Max(Math.Abs(values.Min(o => o.Item1.X)), Math.Abs(values.Max(o => o.Item1.X)));
                double maxY = Math.Max(Math.Abs(values.Min(o => o.Item1.Y)), Math.Abs(values.Max(o => o.Item1.Y)));

                // If they are somewhat near 1, then cap at 1
                if (maxX > .5 && maxX < 1) maxX = 1;
                if (maxY > .5 && maxY < 1) maxY = 1;

                double offsetX = hasNegativePosition ? (width / 2) : 0;
                double offsetY = hasNegativePosition ? (height / 2) : 0;

                double scaleX = maxX > 0d ? (width - offsetX) / maxX : 1;
                double scaleY = maxY > 0d ? (height - offsetY) / maxY : 1;

                #endregion

                Pen matchPen = new Pen(Brushes.Lime, radius * .32);
                Pen otherPen = shouldOutlineNonMatches ? new Pen(new SolidColorBrush(Color.FromArgb(192, 192, 192, 192)), radius * .18) : null;

                _visual = new DrawingVisual();
                using (DrawingContext dc = _visual.RenderOpen())
                {
                    foreach (var value in values)
                    {
                        Color color = value.Item2 < 0 ? negativeColor : positiveColor;
                        double alpha = Math.Abs(value.Item2) / maxValue;
                        Color finalColor = Color.FromArgb(Convert.ToByte(alpha * 255), color.R, color.G, color.B);

                        Point point = new Point(offsetX + (value.Item1.X * scaleX), offsetY + (value.Item1.Y * scaleY));

                        Pen pen = value.Item3 ? matchPen : otherPen;

                        dc.DrawEllipse(new SolidColorBrush(finalColor), pen, point, radius, radius);
                    }
                }
            }

            protected override Visual GetVisualChild(int index)
            {
                return _visual;
            }
            protected override int VisualChildrenCount
            {
                get
                {
                    return 1;
                }
            }
        }

        #endregion

        #region Declaration Section

        private readonly double? MAXSECONDS = 30d;

        private Random _rand = new Random();

        private Tuple<Point, double, bool>[] _trainingData = null;
        private Tuple<Point, double, bool>[] _results = null;

        private bool _initialized = false;

        #endregion

        #region Constructor

        public EncogXOR()
        {
            InitializeComponent();

            this.Background = SystemColors.ControlBrush;

            _initialized = true;
        }

        #endregion

        #region Event Listeners

        private void radShow_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                RedrawResults();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void canvas1_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                RedrawResults();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// This is based off of this article:
        /// http://www.codeproject.com/Articles/54575/An-Introduction-to-Encog-Neural-Networks-for-C
        /// </summary>
        /// <remarks>
        /// Go here for documentation of encog:
        /// http://www.heatonresearch.com/wiki
        /// 
        /// Download link:
        /// https://github.com/encog/encog-dotnet-core/releases
        /// </remarks>
        private void btnXOR_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _trainingData = null;
                _results = null;

                BasicNetwork network = new BasicNetwork();

                #region Create nodes

                // Create the network's nodes

                //NOTE: Using ActivationSigmoid, because there are no negative values.  If there were negative, use ActivationTANH
                //http://www.heatonresearch.com/wiki/Activation_Function

                //NOTE: ActivationSigmoid (0 to 1) and ActivationTANH (-1 to 1) are pure but slower.  A cruder but faster function is ActivationElliott (0 to 1) and ActivationElliottSymmetric (-1 to 1)
                //http://www.heatonresearch.com/wiki/Elliott_Activation_Function

                network.AddLayer(new BasicLayer(new ActivationSigmoid(), true, 2));     // input layer
                network.AddLayer(new BasicLayer(new ActivationSigmoid(), true, 6));     // hidden layer
                network.AddLayer(new BasicLayer(new ActivationSigmoid(), true, 1));     // output layer
                network.Structure.FinalizeStructure();

                // Randomize the links
                network.Reset();

                #endregion

                #region Training data

                // Neural networks must be trained before they are of any use. To train this neural network, we must provide training
                // data. The training data is the truth table for the XOR operator. The XOR has the following inputs:
                double[][] xor_input = new[]
                {
                    new[] { 0d, 0d },
                    new[] { 1d, 0d },
                    new[] { 0d, 1d },
                    new[] { 1d, 1d },
                };

                // And the expected outputs
                double[][] xor_ideal_output = new[]
                {
                    new[] { 0d },
                    new[] { 1d },
                    new[] { 1d },
                    new[] { 0d },
                };

                _trainingData = GetDrawDataFromTrainingData(xor_input, xor_ideal_output);

                #endregion
                #region Train network

                INeuralDataSet trainingSet = new BasicNeuralDataSet(xor_input, xor_ideal_output);

                // This is a good general purpose training algorithm
                //http://www.heatonresearch.com/wiki/Training
                ITrain train = new ResilientPropagation(network, trainingSet);

                List<double> log = new List<double>();

                int trainingIteration = 1;
                do
                {
                    train.Iteration();

                    log.Add(train.Error);

                    trainingIteration++;
                } while ((trainingIteration < 2000) && (train.Error > 0.001));

                // Paste this into excel and chart it to see the error trend
                string logExcel = string.Join("\r\n", log);

                #endregion

                #region Test

                //NOTE: I initially ran a bunch of tests, but the network always returns exactly the same result when given the same inputs
                //var test = Enumerable.Range(0, 1000).
                //    Select(o => new { In1 = _rand.Next(2), In2 = _rand.Next(2) }).

                var test = xor_input.
                    Select(o => new { In1 = Convert.ToInt32(o[0]), In2 = Convert.ToInt32(o[1]) }).
                    Select(o => new
                    {
                        o.In1,
                        o.In2,
                        Expected = XOR(o.In1, o.In2),
                        NN = CallNN(network, o.In1, o.In2),
                    }).
                    Select(o => new { o.In1, o.In2, o.Expected, o.NN, Error = Math.Abs(o.Expected - o.NN) }).
                    OrderByDescending(o => o.Error).
                    ToArray();

                #endregion
                #region Test intermediate values

                // It was only trained with inputs of 0 and 1.  Let's see what it does with values in between

                var intermediates = Enumerable.Range(0, 1000).
                    Select(o => new { In1 = _rand.NextDouble(), In2 = _rand.NextDouble() }).
                    Select(o => new
                    {
                        o.In1,
                        o.In2,
                        NN = CallNN(network, o.In1, o.In2),
                    }).
                    OrderBy(o => o.In1).
                    ThenBy(o => o.In2).
                    //OrderBy(o => o.NN).
                    ToArray();

                #endregion

                #region Serialize/Deserialize

                // Serialize it
                string weightDump = network.DumpWeights();
                double[] dumpArray = weightDump.Split(',').
                    Select(o => double.Parse(o)).
                    ToArray();

                //TODO: Shoot through the layers, and store in some custom structure that can be serialized, then walked through to rebuild on deserialize
                //string[] layerDump = network.Structure.Layers.
                //    Select(o => o.ToString()).
                //    ToArray();

                // Create a clone
                BasicNetwork clone = new BasicNetwork();

                clone.AddLayer(new BasicLayer(new ActivationSigmoid(), true, 2));
                clone.AddLayer(new BasicLayer(new ActivationSigmoid(), true, 6));
                clone.AddLayer(new BasicLayer(new ActivationSigmoid(), true, 1));
                clone.Structure.FinalizeStructure();

                clone.DecodeFromArray(dumpArray);

                // Test the clone
                string cloneDump = clone.DumpWeights();

                bool isSame = weightDump == cloneDump;

                var cloneTests = xor_input.
                    Select(o => new
                    {
                        Input = o,
                        NN = CallNN(clone, o[0], o[1]),
                    }).ToArray();

                #endregion

                #region Store results

                double[] matchValues = new[] { 0d, 1d };
                double matchRange = .03;        //+- 5% of target value would be considered a match

                _results = intermediates.
                    Select(o => Tuple.Create(new Point(o.In1, o.In2), o.NN, IsMatch(o.NN, matchValues, matchRange))).
                    ToArray();

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                RedrawResults();
            }
        }
        /// <summary>
        /// This changes outputs from (0 to 1) to (-1 to 1).  It also trains intermediate input values (.1 to .9) to have an output of zero
        /// </summary>
        private void btnXORPosNeg_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _trainingData = null;
                _results = null;

                #region Training data

                List<double[]> trainingInput = new List<double[]>();
                List<double[]> trainingOutput = new List<double[]>();

                // Good Values
                trainingInput.Add(new[] { 0d, 0d });
                trainingOutput.Add(new[] { -1d });

                trainingInput.Add(new[] { 1d, 0d });
                trainingOutput.Add(new[] { 1d });

                trainingInput.Add(new[] { 0d, 1d });
                trainingOutput.Add(new[] { 1d });

                trainingInput.Add(new[] { 1d, 1d });
                trainingOutput.Add(new[] { -1d });

                // Bad Values
                int numDirtySteps;
                if (!int.TryParse(txtNumSteps.Text, out numDirtySteps))
                {
                    MessageBox.Show("", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                double stepSize = 1d / numDirtySteps;

                for (int cntr1 = 0; cntr1 <= numDirtySteps; cntr1++)
                {
                    double dirtyValue1 = cntr1 * stepSize;

                    for (int cntr2 = 0; cntr2 <= numDirtySteps; cntr2++)
                    {
                        if ((cntr1 == 0 || cntr1 == numDirtySteps) && (cntr2 == 0 || cntr2 == numDirtySteps))
                        {
                            continue;
                        }

                        double dirtyValue2 = cntr2 * stepSize;

                        trainingInput.Add(new[] { dirtyValue2, dirtyValue1 });
                        trainingOutput.Add(new[] { 0d });

                        trainingInput.Add(new[] { dirtyValue1, dirtyValue2 });
                        trainingOutput.Add(new[] { 0d });
                    }
                }

                _trainingData = GetDrawDataFromTrainingData(trainingInput.ToArray(), trainingOutput.ToArray());

                #endregion

                BasicNetwork network = UtilityEncog.GetTrainedNetwork(trainingInput.ToArray(), trainingOutput.ToArray(), maxSeconds: MAXSECONDS);

                #region Test

                //NOTE: I initially ran a bunch of tests, but the network always returns exactly the same result when given the same inputs
                //var test = Enumerable.Range(0, 1000).
                //    Select(o => new { In1 = _rand.Next(2), In2 = _rand.Next(2) }).

                var test = trainingInput.
                    Select(o => new { In1 = Convert.ToInt32(o[0]), In2 = Convert.ToInt32(o[1]) }).
                    Select(o => new
                    {
                        o.In1,
                        o.In2,
                        Expected = XORPosNeg(o.In1, o.In2),
                        NN = CallNN(network, o.In1, o.In2),
                    }).
                    Select(o => new { o.In1, o.In2, o.Expected, o.NN, Error = Math.Abs(o.Expected - o.NN) }).
                    OrderByDescending(o => o.Error).
                    ToArray();

                #endregion
                #region Test intermediate values

                // It was only trained with inputs of 0 and 1.  Let's see what it does with values in between

                var intermediates = Enumerable.Range(0, 1000).
                    Select(o => new { In1 = _rand.NextDouble(), In2 = _rand.NextDouble() }).
                    Select(o => new
                    {
                        o.In1,
                        o.In2,
                        NN = CallNN(network, o.In1, o.In2),
                    }).
                    //OrderBy(o => o.In1).
                    //ThenBy(o => o.In2).
                    OrderBy(o => o.NN).
                    ToArray();

                #endregion

                #region Store results

                double[] matchValues = new[] { -1d, 1d };
                double matchRange = .06;        //+- 5% of target value would be considered a match

                _results = intermediates.
                    Select(o => Tuple.Create(new Point(o.In1, o.In2), o.NN, IsMatch(o.NN, matchValues, matchRange))).
                    ToArray();

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                RedrawResults();
            }
        }
        /// <summary>
        /// Nearly identical to the above method, but the 4 corner values are shifted a bit
        /// </summary>
        private void btnXORPosNeg2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _trainingData = null;
                _results = null;

                #region Training data

                int numDirtySteps;
                if (!int.TryParse(txtNumSteps2.Text, out numDirtySteps))
                {
                    MessageBox.Show("", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                double stepSize = 1d / numDirtySteps;
                double otherStepSize = stepSize / 6d;

                List<double[]> trainingInput = new List<double[]>();
                List<double[]> trainingOutput = new List<double[]>();

                // Good Values (pure)
                trainingInput.Add(new[] { 0d, 0d });
                trainingOutput.Add(new[] { -1d });

                trainingInput.Add(new[] { 1d, 0d });
                trainingOutput.Add(new[] { 1d });

                trainingInput.Add(new[] { 0d, 1d });
                trainingOutput.Add(new[] { 1d });

                trainingInput.Add(new[] { 1d, 1d });
                trainingOutput.Add(new[] { -1d });

                // Good Values (shifted)
                trainingInput.Add(new[] { otherStepSize, otherStepSize });
                trainingOutput.Add(new[] { -1d });

                trainingInput.Add(new[] { 1d - otherStepSize, otherStepSize });
                trainingOutput.Add(new[] { 1d });

                trainingInput.Add(new[] { otherStepSize, 1d - otherStepSize });
                trainingOutput.Add(new[] { 1d });

                trainingInput.Add(new[] { 1d - otherStepSize, 1d - otherStepSize });
                trainingOutput.Add(new[] { -1d });

                // Bad Values
                for (int cntr1 = 0; cntr1 <= numDirtySteps; cntr1++)
                {
                    double dirtyValue1 = cntr1 * stepSize;

                    for (int cntr2 = 0; cntr2 <= numDirtySteps; cntr2++)
                    {
                        if ((cntr1 == 0 || cntr1 == numDirtySteps) && (cntr2 == 0 || cntr2 == numDirtySteps))
                        {
                            continue;
                        }

                        double dirtyValue2 = cntr2 * stepSize;

                        trainingInput.Add(new[] { dirtyValue2, dirtyValue1 });
                        trainingOutput.Add(new[] { 0d });

                        trainingInput.Add(new[] { dirtyValue1, dirtyValue2 });
                        trainingOutput.Add(new[] { 0d });
                    }
                }

                _trainingData = GetDrawDataFromTrainingData(trainingInput.ToArray(), trainingOutput.ToArray());

                #endregion

                BasicNetwork network = UtilityEncog.GetTrainedNetwork(trainingInput.ToArray(), trainingOutput.ToArray(), maxSeconds: MAXSECONDS);

                #region Test

                //NOTE: I initially ran a bunch of tests, but the network always returns exactly the same result when given the same inputs
                //var test = Enumerable.Range(0, 1000).
                //    Select(o => new { In1 = _rand.Next(2), In2 = _rand.Next(2) }).

                var test = trainingInput.
                    Select(o => new { In1 = Convert.ToInt32(o[0]), In2 = Convert.ToInt32(o[1]) }).
                    Select(o => new
                    {
                        o.In1,
                        o.In2,
                        Expected = XORPosNeg(o.In1, o.In2),
                        NN = CallNN(network, o.In1, o.In2),
                    }).
                    Select(o => new { o.In1, o.In2, o.Expected, o.NN, Error = Math.Abs(o.Expected - o.NN) }).
                    OrderByDescending(o => o.Error).
                    ToArray();

                #endregion
                #region Test intermediate values

                // It was only trained with inputs of 0 and 1.  Let's see what it does with values in between

                var intermediates = Enumerable.Range(0, 1000).
                    Select(o => new { In1 = _rand.NextDouble(), In2 = _rand.NextDouble() }).
                    Select(o => new
                    {
                        o.In1,
                        o.In2,
                        NN = CallNN(network, o.In1, o.In2),
                    }).
                    //OrderBy(o => o.In1).
                    //ThenBy(o => o.In2).
                    OrderBy(o => o.NN).
                    ToArray();

                #endregion

                #region Store results

                double[] matchValues = new[] { -1d, 1d };
                double matchRange = .06;        //+- 5% of target value would be considered a match

                _results = intermediates.
                    Select(o => Tuple.Create(new Point(o.In1, o.In2), o.NN, IsMatch(o.NN, matchValues, matchRange))).
                    ToArray();

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                RedrawResults();
            }
        }

        #endregion

        #region Private Methods

        private void RedrawResults()
        {
            canvas1.Children.Clear();

            if (radShowResults.IsChecked.Value && _results != null)
            {
                canvas1.Children.Add(new NNResultsVisual(canvas1.ActualWidth, canvas1.ActualHeight, _results, false));
            }
            else if (radShowTrainingData.IsChecked.Value && _trainingData != null)
            {
                canvas1.Children.Add(new NNResultsVisual(canvas1.ActualWidth, canvas1.ActualHeight, _trainingData, true));
            }
        }

        private static Tuple<Point, double, bool>[] GetDrawDataFromTrainingData(double[][] trainingInput, double[][] trainingOutput)
        {
            return GetDrawDataFromTrainingData(trainingInput, trainingOutput.Select(o => o[0]).ToArray());
        }
        private static Tuple<Point, double, bool>[] GetDrawDataFromTrainingData(double[][] trainingInput, double[] trainingOutput)
        {
            var retVal = new Tuple<Point, double, bool>[trainingInput.Length];

            for (int cntr = 0; cntr < retVal.Length; cntr++)
            {
                retVal[cntr] = Tuple.Create(new Point(trainingInput[cntr][0], trainingInput[cntr][1]), trainingOutput[cntr], false);
            }

            return retVal;
        }

        private static int XOR(int i1, int i2)
        {
            return i1 == i2 ? 0 : 1;
        }
        private static int XORPosNeg(int i1, int i2)
        {
            return i1 == i2 ? -1 : 1;
        }

        // Wrap it to be linq friendly
        private static double CallNN(BasicNetwork network, double input1, double input2)
        {
            double[] input = new[] { input1, input2 };
            double[] output = new double[1];

            network.Compute(input, output);

            return output[0];
        }

        private static bool IsMatch(double testValue, double[] matchValues, double range)
        {
            return matchValues.Any(o => Math.Abs(testValue - o) < range);
        }

        #endregion
    }
}
