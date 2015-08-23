using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Encog.Engine.Network.Activation;
using Encog.Neural.Data.Basic;
using Encog.Neural.Networks;
using Encog.Neural.Networks.Layers;
using Encog.Neural.Networks.Training;
using Encog.Neural.Networks.Training.Propagation.Resilient;
using Encog.Neural.NeuralData;
using Game.HelperClassesCore;

namespace Game.HelperClassesAI
{
    public static class UtilityEncog
    {
        #region Class: TrainingData

        private class TrainingData
        {
            public TrainingData(double[][] input, double[][] output)
            {
                //NOTE: This method also validates the arrays, so calling this before assigning any properties in case exceptions get thrown
                bool inputHasNegative, outputHasNegative;
                int inputSize, outputSize;
                GetStats(out inputHasNegative, out outputHasNegative, out inputSize, out outputSize, input, output);

                this.Input = input;
                this.Output = output;

                this.InputHasNegative = inputHasNegative;
                this.OutputHasNegative = outputHasNegative;

                this.InputSize = inputSize;
                this.OutputSize = outputSize;
            }

            public readonly double[][] Input;
            public readonly double[][] Output;

            public readonly bool InputHasNegative;
            public readonly bool OutputHasNegative;

            public readonly int InputSize;
            public readonly int OutputSize;

            private static void GetStats(out bool inputHasNegative, out bool outputHasNegative, out int inputSize, out int outputSize, double[][] input, double[][] output)
            {
                if (input == null || output == null)
                {
                    throw new ArgumentNullException("training input and output can't be null");
                }
                else if (input.Length != output.Length)
                {
                    throw new ArgumentException("training input and output must have the same length (same number of scenarios)");
                }
                else if (input.Length == 0)
                {
                    throw new ArgumentException("training arrays can't be empty");
                }

                inputHasNegative = false;
                outputHasNegative = false;

                inputSize = -1;
                outputSize = -1;

                for (int cntr = 0; cntr < input.Length; cntr++)
                {
                    if (cntr == 0)
                    {
                        inputSize = input[cntr].Length;
                        outputSize = output[cntr].Length;
                    }
                    else if (input[cntr].Length != inputSize || output[cntr].Length != outputSize)
                    {
                        throw new ArgumentException("all training arrays must be the same size");
                    }

                    if (!inputHasNegative)
                    {
                        inputHasNegative = input[cntr].Any(o => o < 0d);
                    }

                    if (!outputHasNegative)
                    {
                        outputHasNegative = output[cntr].Any(o => o < 0d);
                    }
                }
            }
        }

        #endregion

        #region Declaration Section

        public const double ERROR = 0.001;

        #endregion

        #region Training

        /// <summary>
        /// This looks at the training data, and builds a neural net for it
        /// </summary>
        /// <remarks>
        /// This makes a lot of assumptions.  There are all kinds of ways those assumptions could be tweaked, but hopefully this
        /// will make a satisfactory network for most cases.
        /// 
        /// TODO: Take in args that give hints about hidden layers, number of networks to try, input/output are 0to1, -1to1, autodetect
        /// 
        /// TODO: If a solution isn't found after a small amount of time, spin up another attempt (keep waiting for the first to finish),
        /// instead of waiting for one at a time to completly finish or fail
        /// </remarks>
        /// <param name="numSimultaneousCandidates">Number of networks to train at a time (each on its own thread)</param>
        public static EncogTrainingResponse GetTrainedNetwork(double[][] trainingInput, double[][] trainingOutput, double maxError = ERROR, double? maxSeconds_PerAttempt = null, double? maxSeconds_Total = null, CancellationToken? cancelToken = null)
        {
            const double MULTGROWTH = .1;

            CancellationToken cancelTokenActual = cancelToken ?? CancellationToken.None;        // can't default to CancellationToken.None in the params (compiler complains)

            TrainingData training = new TrainingData(trainingInput, trainingOutput);

            Stopwatch elapsed = new Stopwatch();
            elapsed.Start();

            //NOTE: Even if a network is a failure, it will still be stored here until a better one is made
            EncogTrainingResponse retVal = null;

            for (int cntr = 0; cntr < 1000; cntr++)
            {
                if (cancelTokenActual.IsCancellationRequested)
                {
                    break;
                }

                double? maxSecActual = null;
                if (maxSeconds_Total != null)
                {
                    maxSecActual = maxSeconds_Total.Value - elapsed.Elapsed.TotalSeconds;
                    if (maxSecActual.Value < 0)
                    {
                        break;
                    }
                }

                if (maxSecActual != null && maxSeconds_PerAttempt != null && maxSeconds_PerAttempt.Value < maxSecActual.Value)
                {
                    maxSecActual = maxSeconds_PerAttempt.Value;
                }
                else if(maxSecActual == null && maxSeconds_PerAttempt != null)
                {
                    maxSecActual = maxSeconds_PerAttempt.Value;
                }

                // Train it
                double hiddenMultActual = 1d + (cntr * MULTGROWTH);
                EncogTrainingResponse network = GetTrainedNetworkAsync(training, hiddenMultActual, maxError, cancelTokenActual, maxSecActual).Result;

                if (network.IsSuccess)
                {
                    return network;
                }
                else if (retVal == null || network.Error < retVal.Error)
                {
                    retVal = network;
                }

                if (cancelTokenActual.IsCancellationRequested || (maxSeconds_Total != null && elapsed.Elapsed.TotalSeconds >= maxSeconds_Total.Value))
                {
                    break;
                }
            }

            return retVal;
        }

        #region OLD
        //// This is a failed attempt.  Threading didn't help, and there's no need to try to improve the error - once one is found, that's enough
        //private static BasicNetwork GetTrainedNetwork(double[][] trainingInput, double[][] trainingOutput, int numSimultaneousCandidates = 7, double maxError = 0.001, double? maxSeconds = null)
        //{
        //    //TODO: Instead of trying many variants at the same time, only do one or two at a time, and ramp up the complexity of the hidden layer after each failure
        //    //TODO: Don't WaitAll, be more granular, cancel other threads once one has success

        //    const int NUMITERATIONS = 4;

        //    TrainingData training = new TrainingData(trainingInput, trainingOutput);

        //    double hiddenMultiplier = 1d;
        //    double? maxSecondsDivided = maxSeconds != null ? maxSeconds.Value / NUMITERATIONS : (double?)null;

        //    for (int cntr = 0; cntr < NUMITERATIONS; cntr++)
        //    {
        //        // Train a bunch of networks
        //        //NOTE: Initially, I had all the runs use the same multiplier, but I think having several different sizes competing at the same time gives better variety
        //        var candidateTasks = Enumerable.Range(0, numSimultaneousCandidates).
        //            Select(o => GetTrainedNetworkAsync(training, hiddenMultiplier * (o % 2) * 1.5, maxError, CancellationToken.None, maxSecondsDivided)).
        //            ToArray();

        //        Task.WaitAll(candidateTasks);

        //        // Find a network with the least error
        //        var candidates = candidateTasks.
        //            Select(o => o.Result).
        //            Where(o => o.Item1 != null).
        //            OrderBy(o => o.Item2).
        //            ToArray();

        //        if (candidates.Length > 0)
        //        {
        //            return candidates[0].Item1;
        //        }

        //        // No networks worked, increase the size of the hidden layers
        //        hiddenMultiplier *= 2d;
        //    }

        //    throw new ApplicationException("Couldn't find a solution");
        //}
        #endregion

        #endregion

        #region Serialization

        // These map to/from a dna class.  The dna class is designed to be easily serialized/deserialized
        public static EncogDNA ToDNA(BasicNetwork network)
        {
            return new EncogDNA();
        }
        public static BasicNetwork FromDNA(EncogDNA dna)
        {
            throw new ApplicationException("finish this");
        }

        #endregion

        #region Misc

        /// <summary>
        /// This is a helper method for recognizers that have one output neuron per item.  In order for it to be a match, only one of the outputs can
        /// be a one, and everything else needs to be a zero
        /// </summary>
        /// <remarks>
        /// Example:
        ///     Say there are four things being recognized { A, B, C, D }
        ///     There would be 4 outputs: double[4]
        ///     { .01, .01, .99, .01 } would match C (this method would return 2)
        /// </remarks>
        public static int? IsMatch(double[] output, double minThreshold = .05, double maxThreshold = .95)
        {
            int? retVal = null;

            for (int cntr = 0; cntr < output.Length; cntr++)
            {
                if (output[cntr] <= minThreshold)
                {
                    // This is considered a value of zero
                    continue;
                }
                else if (output[cntr] >= maxThreshold)
                {
                    // This is considered a value of one (a match)
                    if (retVal == null)
                    {
                        retVal = cntr;
                    }
                    else
                    {
                        // Multiple ones.  No match
                        return null;
                    }
                }
                else
                {
                    // This is an intermediate value.  No match
                    return null;
                }
            }

            return retVal;
        }

        /// <summary>
        /// This creates one entry per output category
        /// </summary>
        /// <remarks>
        /// If you pass in 4, you will get:
        ///     1 0 0 0
        ///     0 1 0 0
        ///     0 0 1 0
        ///     0 0 0 1
        /// </remarks>
        public static double[][] GetOutputLayer(int count)
        {
            double[][] retVal = new double[count][];

            for (int cntr = 0; cntr < count; cntr++)
            {
                double[] entry = new double[count];     // all values default to zero
                entry[cntr] = 1;        // set the pin for this category to 1

                retVal[cntr] = entry;
            }

            return retVal;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// This trains a network on a separate thread
        /// </summary>
        /// <param name="hiddenMultiplier">The method calculates how many hidden layers and neurons per layer (with a bit of randomization), then multiplies those values by this multiplier</param>
        private static Task<EncogTrainingResponse> GetTrainedNetworkAsync(TrainingData training, double hiddenMultiplier, double maxError, CancellationToken cancelToken, double? maxSeconds = null)
        {
            return Task.Run(() =>
            {
                // Create a network, build input/hidden/output layers
                //TODO: Figure out why telling it to use the faster activation functions always causes the trainer to return an error of NaN
                BasicNetwork network = CreateNetwork(training, hiddenMultiplier, false);

                // Train the network
                return TrainNetwork(network, training, maxError, cancelToken, maxSeconds);        // lower score is better
            }, cancelToken);
        }

        /// <summary>
        /// Create a network with input/hidden/output layers
        /// </summary>
        private static BasicNetwork CreateNetwork(TrainingData training, double hiddenMultiplier, bool useFast = true)
        {
            const double TWOTHIRDS = 2d / 3d;
            const double HIDDENLAYER_RANDRANGE = .5;        // the number of neurons in a layer will be +- 50% of desired
            const double ANOTHERLAYERPERCENT = .1;      // 10% chance of creating another hidden layer (so 10% for 2 layers, 1% for 3, .1% for 4, etc)

            Random rand = StaticRandom.GetRandomForThread();

            BasicNetwork retVal = new BasicNetwork();

            // Input Layer
            retVal.AddLayer(new BasicLayer(GetActivationFunction(training.InputHasNegative, useFast), true, training.InputSize));

            #region Hidden Layers

            // Initialize these with the input layer's values
            bool wasPrevNeg = training.InputHasNegative;
            int prevCount = training.InputSize;

            do
            {
                // Use Negative
                bool isCurNeg = false;
                if (wasPrevNeg || training.OutputHasNegative)
                {
                    isCurNeg = rand.NextBool();
                }

                // Neuron Count
                int minNodes = Math.Min(prevCount, training.OutputSize);

                double desiredNodes = (prevCount + training.OutputSize) * TWOTHIRDS * hiddenMultiplier;
                desiredNodes = rand.NextPercent(desiredNodes, HIDDENLAYER_RANDRANGE);       // randomize the count

                int curCount = Convert.ToInt32(Math.Round(desiredNodes));
                if (curCount < minNodes)
                {
                    curCount = minNodes;
                }
                else if (curCount < 4)
                {
                    curCount = 4;
                }

                // Create it
                retVal.AddLayer(new BasicLayer(GetActivationFunction(isCurNeg, useFast), true, curCount));     // hidden layer

                // Prep for the next iteration
                wasPrevNeg = isCurNeg;
                prevCount = curCount;
            } while (rand.NextDouble() < ANOTHERLAYERPERCENT);      // see if another hidden layer should be created

            #endregion

            // Output Layer
            retVal.AddLayer(new BasicLayer(GetActivationFunction(training.OutputHasNegative, useFast), true, training.OutputSize));

            // Finish
            retVal.Structure.FinalizeStructure();
            retVal.Reset();     // Randomize the links
            return retVal;
        }

        /// <summary>
        /// This trains the data until the error is acceptable
        /// </summary>
        /// <remarks>
        /// ResilientPropagation is a good general purpose training algorithm:
        /// http://www.heatonresearch.com/wiki/Training
        /// </remarks>
        private static EncogTrainingResponse TrainNetwork(BasicNetwork network, TrainingData training, double maxError, CancellationToken cancelToken, double? maxSeconds = null)
        {
            const int MAXITERATIONS = 5000;

            INeuralDataSet trainingSet = new BasicNeuralDataSet(training.Input, training.Output);
            ITrain train = new ResilientPropagation(network, trainingSet);

            DateTime startTime = DateTime.UtcNow;
            TimeSpan? maxTime = maxSeconds != null ? TimeSpan.FromSeconds(maxSeconds.Value) : (TimeSpan?)null;

            bool success = false;

            //List<double> log = new List<double>();
            int iteration = 1;
            double error = double.MaxValue;
            while (true)
            {
                if (cancelToken.IsCancellationRequested)
                {
                    break;
                }

                train.Iteration();

                error = train.Error;
                //log.Add(error);

                iteration++;

                if (double.IsNaN(error))
                {
                    break;
                }
                else if (error < maxError)
                {
                    success = true;
                    break;
                }
                else if (iteration >= MAXITERATIONS)
                {
                    break;
                }
                else if (maxTime != null && DateTime.UtcNow - startTime > maxTime)
                {
                    break;
                }
            }

            //string logExcel = string.Join("\r\n", log);       // paste this into excel and chart it to see the error trend

            train.FinishTraining();

            return new EncogTrainingResponse(network, success, error, iteration, (DateTime.UtcNow - startTime).TotalSeconds);
        }

        /// <summary>
        /// This chooses an activation function for a layer of neurons
        /// </summary>
        /// <param name="isNegative">
        /// True: output is -1 to 1
        /// False: output is 0 to 1
        /// </param>
        /// <param name="useFast">
        /// True: Use a faster but less precise implementation (see remarks)
        /// False: Use a slower but more exact implementation
        /// </param>
        /// <remarks>
        /// If a neuron should return 0 to 1, then use ActivationSigmoid
        /// If a neuron should return -1 to 1, then use ActivationTANH
        /// http://www.heatonresearch.com/wiki/Activation_Function
        /// 
        /// ActivationSigmoid (0 to 1) and ActivationTANH (-1 to 1) are pure but slower.
        /// A cruder but faster function is ActivationElliott (0 to 1) and ActivationElliottSymmetric (-1 to 1)
        /// http://www.heatonresearch.com/wiki/Elliott_Activation_Function
        /// </remarks>
        private static IActivationFunction GetActivationFunction(bool isNegative, bool useFast)
        {
            if (isNegative)
            {
                // output is -1 to 1
                if (useFast)
                {
                    return new ActivationElliottSymmetric();
                }
                else
                {
                    return new ActivationTANH();
                }
            }
            else
            {
                // output is 0 to 1
                if (useFast)
                {
                    return new ActivationElliott();
                }
                else
                {
                    return new ActivationSigmoid();
                }
            }
        }

        #endregion
    }

    #region Class: EncogTrainingResponse

    public class EncogTrainingResponse
    {
        public EncogTrainingResponse(BasicNetwork network, bool isSuccess, double error, int iterations, double seconds)
        {
            this.Network = network;
            this.IsSuccess = isSuccess;
            this.Error = error;
            this.Iterations = iterations;
            this.Seconds = seconds;
        }

        public readonly BasicNetwork Network;
        /// <summary>
        /// This is just a helper that returns null if !IsSuccess
        /// </summary>
        public BasicNetwork NetworkOrNull
        {
            get
            {
                if(this.IsSuccess)
                {
                    return this.Network;
                }
                else
                {
                    return null;
                }
            }
        }

        public readonly bool IsSuccess;

        public readonly double Error;
        public readonly int Iterations;
        public readonly double Seconds;
    }

    #endregion

    #region Class: EncogDNA

    public class EncogDNA
    {

    }

    #endregion
}
