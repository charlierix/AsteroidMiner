using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using SharpNeat.Core;
using SharpNeat.Phenomes;

namespace Game.Newt.Testers.NEAT
{
    public class AntPos_Evaluator : IPhenomeEvaluator<IBlackBox>
    {
        #region Declaration Section

        public const double ERRORMULT = 10;

        private readonly object _lock = new object();

        private HarnessArgs _harnessArgs;
        private readonly EvaluatorArgs _evalArgs;

        #endregion

        #region Constructor

        public AntPos_Evaluator(HarnessArgs harnessArgs, EvaluatorArgs evalArgs)
        {
            lock (_lock)
            {
                _harnessArgs = harnessArgs;
            }

            _evalArgs = evalArgs;
        }

        #endregion

        #region IPhenomeEvaluator<IBlackBox>

        private ulong _evaluationCount;
        public ulong EvaluationCount => _evaluationCount;

        public bool StopConditionSatisfied => false;        // real world evaluators should have logic to know when to stop

        public FitnessInfo Evaluate(IBlackBox phenome)
        {
            const double LARGESTERROR = 10000;

            TrackedItemHarness harness = null;
            lock (_lock)
            {
                harness = new TrackedItemHarness(_harnessArgs);
            }

            harness.ItemRemoved += (s1, e1) =>
            {
                harness.SetItem(GetNewItem(harness, _evalArgs));
            };

            harness.SetItem(GetNewItem(harness, _evalArgs));

            // Input Array (pixels)
            double[] inputArr = new double[harness.InputSizeXY * harness.InputSizeXY];
            double dotRadius = (harness.VisionSize / harness.InputSizeXY) * Math.Sqrt(2);

            // Output Array (pixels)
            double[] outputArr = new double[harness.OutputSizeXY * harness.OutputSizeXY];

            Tuple<TrackedItemBase, Point, Vector> prevPos, currentPos;

            double error = 0;

            for (int cntr = 0; cntr < _evalArgs.TotalNumberEvaluations; cntr++)
            {
                harness.Tick(_evalArgs.ElapsedTime_Seconds);

                GetPrevCurrentPositions(out prevPos, out currentPos, harness, _evalArgs);

                // Apply Input
                ClearArray(inputArr);

                if (prevPos != null)
                {
                    ApplyPoint(inputArr, harness.InputCellCenters, dotRadius, prevPos.Item2, true);
                }

                // Brain.Tick
                phenome.InputSignalArray.CopyFrom(inputArr, 0);
                phenome.Activate();
                phenome.OutputSignalArray.CopyTo(outputArr, 0);

                // Evaluate output
                double[] expectedOutput = GetExpectedOutput(currentPos, harness, _evalArgs);
                error += CompareOutputs(outputArr, expectedOutput, ERRORMULT, _evalArgs.ErrorBias);
            }

            // Figure out what the worst possible error could be
            //double largestError = ERRORMULT * ERRORMULT * outputArr.Length * _evalArgs.TotalNumberEvaluations;
            double largestError = ERRORMULT * ERRORMULT * _evalArgs.TotalNumberEvaluations;     // don't include outputArr.Length. CompareOutputs() divides that out

            // Scale it down to 10000
            if (largestError > LARGESTERROR)
            {
                error *= LARGESTERROR / largestError;
                largestError = LARGESTERROR;
            }

            // SharpNEAT wants a top score instead of error, so flip it
            double score = largestError - error;

            _evaluationCount++;

            return new FitnessInfo(score, score);
        }

        public void Reset()
        {
        }

        #endregion

        #region Public Methods

        public static TrackedItemBase GetNewItem(TrackedItemHarness harness, EvaluatorArgs evalArgs)
        {
            if (evalArgs.NewItemStart != null)
            {
                var choice = evalArgs.NewItemStart[StaticRandom.Next(evalArgs.NewItemStart.Length)];
                return GetNewItem_Predefined(harness, choice.Item1, choice.Item2, choice.Item3, choice.Item4);
            }
            else
            {
                return GetNewItem_Random(harness, evalArgs.ItemTypes[StaticRandom.Next(evalArgs.ItemTypes.Length)], evalArgs.BounceOffWalls, evalArgs.MaxSpeed);
            }
        }
        public static TrackedItemBase GetNewItem_Random(TrackedItemHarness harness, TrackedItemType itemType, bool bounceOffWalls, double maxSpeed)
        {
            if (harness == null)
            {
                throw new InvalidOperationException("Harness must be created first");
            }

            switch (itemType)
            {
                case TrackedItemType.Streaker:
                    return new TrackedItemStreaker(harness.MapSize, bounceOffWalls, harness.Time)
                    {
                        Position = Math3D.GetRandomVector(harness.MapSize / 2).ToPoint2D(),
                        Velocity = Math3D.GetRandomVector_Circular(maxSpeed).ToVector2D(),
                    };

                default:
                    throw new ApplicationException("Unsupported TrackedItemType: " + itemType.ToString());
            }
        }
        public static TrackedItemBase GetNewItem_Predefined(TrackedItemHarness harness, TrackedItemType itemType, Point position, Vector velocity, bool bounceOffWalls)
        {
            if (harness == null)
            {
                throw new InvalidOperationException("Harness must be created first");
            }

            switch (itemType)
            {
                case TrackedItemType.Streaker:
                    return new TrackedItemStreaker(harness.MapSize, bounceOffWalls, harness.Time)
                    {
                        Position = position,
                        Velocity = velocity,
                    };

                default:
                    throw new ApplicationException("Unsupported TrackedItemType: " + itemType.ToString());
            }
        }

        public static void GetPrevCurrentPositions(out Tuple<TrackedItemBase, Point, Vector> prevPos, out Tuple<TrackedItemBase, Point, Vector> currentPos, TrackedItemHarness harness, EvaluatorArgs evaluatorArgs)
        {
            TrackedItemBase item = harness.Item;

            currentPos = null;
            if (item != null)
            {
                currentPos = Tuple.Create(item, item.Position, item.Velocity);
            }

            prevPos = harness.GetPreviousPosition(harness.Time - evaluatorArgs.Delay_Seconds);
        }

        public static double[] GetExpectedOutput_OLD1(Tuple<TrackedItemBase, Point, Vector> prevPos, Tuple<TrackedItemBase, Point, Vector> currentPos, TrackedItemHarness harness, EvaluatorArgs evaluatorArgs)
        {
            // This is too complicated

            double[] retVal = null;

            if (prevPos == null)
            {
                //If prev is null, then output should be zeros
                retVal = GetExpectedOutput_Zeros(harness);
            }
            else if (currentPos == null || currentPos.Item1.Token != prevPos.Item1.Token)
            {
                //If prev and current tokens don't match, then????
                //desiredOutput = GetError_Unmatched(outputArr);        // might not even want to add to the error
            }
            else if (harness.Time - prevPos.Item1.CreateTime < evaluatorArgs.NewItem_Duration_Multiplier * evaluatorArgs.Delay_Seconds)
            {
                //If prev is new, then error size gradient
                double aliveTime = harness.Time - prevPos.Item1.CreateTime;        // figure out how long it's been alive
                double percent = aliveTime / (evaluatorArgs.NewItem_Duration_Multiplier * evaluatorArgs.Delay_Seconds);     // get the percent of the range
                percent = UtilityCore.Cap(1 - percent, 0, 1);       // invert the percent so that an old percent of zero will now be the full error mult
                double newItemMult = percent * evaluatorArgs.NewItem_ErrorMultiplier;

                retVal = GetExpectedOutput_Matched(currentPos, harness, evaluatorArgs, newItemMult);
            }
            else
            {
                //otherwise, standard scoring
                retVal = GetExpectedOutput_Matched(currentPos, harness, evaluatorArgs);
            }

            return retVal;
        }
        public static double[] GetExpectedOutput_OLD2(Tuple<TrackedItemBase, Point, Vector> currentPos, TrackedItemHarness harness, EvaluatorArgs evaluatorArgs)
        {
            // This is too simplistic.  It draws current before previous even exists

            double[] retVal = null;

            if (currentPos == null)
            {
                return GetExpectedOutput_Zeros(harness);
            }
            else
            {
                retVal = GetExpectedOutput_Matched(currentPos, harness, evaluatorArgs);
            }

            return retVal;
        }
        public static double[] GetExpectedOutput(Tuple<TrackedItemBase, Point, Vector> currentPos, TrackedItemHarness harness, EvaluatorArgs evaluatorArgs)
        {
            const int NUMFRAMES = 2;

            double[] retVal = null;

            if (currentPos == null)
            {
                return GetExpectedOutput_Zeros(harness);
            }
            else if (harness.Time - currentPos.Item1.CreateTime - harness.DelayBetweenInstances < evaluatorArgs.Delay_Seconds + (evaluatorArgs.ElapsedTime_Seconds * NUMFRAMES))
            {
                // Can't show the current's position yet, because previous doesn't exist yet
                return GetExpectedOutput_Zeros(harness);
            }
            else
            {
                retVal = GetExpectedOutput_Matched(currentPos, harness, evaluatorArgs);
            }

            return retVal;
        }

        public static double CompareOutputs_OLD(double[] outputArr, double[] desiredOutput, double multiplier)
        {
            // <1 will penalize false positives more
            // >1 will penalize false negatives more
            //const double POW = .25;

            double retVal = 0;

            for (int cntr = 0; cntr < outputArr.Length; cntr++)
            {
                double difference = Math.Abs(outputArr[cntr] - desiredOutput[cntr]);

                // No
                //double difference = Math.Abs(Math.Pow(outputArr[cntr], POW) - desiredOutput[cntr]);

                difference *= multiplier;
                retVal += difference * difference;
            }

            retVal /= outputArr.Length;

            return retVal;
        }
        public static double CompareOutputs(double[] outputArr, double[] desiredOutput, double multiplier, ScoreLeftRightBias bias = ScoreLeftRightBias.Standard)
        {
            //const double POW = .25;     // need to go less than .5, because it will be squared, and .5 would make it linear.  So .25 will turn it into a square root
            //const double POW = .5;     // need to go less than .5, because it will be squared, and .5 would make it linear.  So .25 will turn it into a square root

            double retVal = 0;

            for (int cntr = 0; cntr < outputArr.Length; cntr++)
            {
                double difference = Math.Abs(outputArr[cntr] - desiredOutput[cntr]);

                switch (bias)
                {
                    case ScoreLeftRightBias.PenalizeFalsePositives_Weak:
                        if (outputArr[cntr] > desiredOutput[cntr])
                        {
                            difference = Math.Pow(difference, .75);
                        }
                        break;

                    case ScoreLeftRightBias.PenalizeFalsePositives_Medium:
                        if (outputArr[cntr] > desiredOutput[cntr])
                        {
                            difference = Math.Pow(difference, .5);
                        }
                        break;

                    case ScoreLeftRightBias.PenalizeFalsePositives_Strong:
                        if (outputArr[cntr] > desiredOutput[cntr])
                        {
                            difference = Math.Pow(difference, .25);
                        }
                        break;

                    case ScoreLeftRightBias.PenalizeFalseNegatives:
                        if (outputArr[cntr] < desiredOutput[cntr])
                        {
                            difference = Math.Pow(difference, .5);
                        }
                        break;
                }

                difference *= multiplier;
                retVal += difference * difference;
            }

            retVal /= outputArr.Length;

            return retVal;
        }

        public static void ClearArray(double[] inputArr)
        {
            for (int cntr = 0; cntr < inputArr.Length; cntr++)
            {
                inputArr[cntr] = 0d;
            }
        }
        /// <summary>
        /// This adds a dot to the input array
        /// </summary>
        public static void ApplyPoint(double[] inputArr, Point[] cellCenters, double dotRadius, Point position, bool isPositive)
        {
            if (inputArr.Length != cellCenters.Length)
            {
                throw new ArgumentException(string.Format("inputArr and cellCenters must be the same size ({0}), ({1})", inputArr.Length, cellCenters.Length));
            }

            double radiusSquared = dotRadius * dotRadius;

            for (int cntr = 0; cntr < inputArr.Length; cntr++)
            {
                double displacementSqr = (position - cellCenters[cntr]).LengthSquared;
                if (displacementSqr > radiusSquared)
                {
                    continue;
                }

                double percent = Math.Sqrt(displacementSqr) / dotRadius;
                percent = 1d - percent;

                if (isPositive)
                {
                    inputArr[cntr] = Math.Min(inputArr[cntr] + percent, 1d);
                }
                else
                {
                    inputArr[cntr] = Math.Max(inputArr[cntr] - percent, -1d);
                }
            }
        }

        public void ChangedResolution(int inputSizeXY, int outputSizeXY)
        {
            lock (_lock)
            {
                _harnessArgs = new HarnessArgs(
                    _harnessArgs.MapSize,
                    _harnessArgs.VisionSize,
                    _harnessArgs.OutputSize,
                    inputSizeXY,
                    outputSizeXY,
                    _harnessArgs.DelayBetweenInstances);
            }
        }

        #endregion

        #region Private Methods

        private static double[] GetExpectedOutput_Zeros(TrackedItemHarness harness)
        {
            return new double[harness.OutputSizeXY * harness.OutputSizeXY];        // it conveniently defaults to zeros
        }
        private static double[] GetExpectedOutput_Matched(Tuple<TrackedItemBase, Point, Vector> position, TrackedItemHarness harness, EvaluatorArgs evaluatorArgs, double? additionalErrorMult = null)
        {
            //TODO: Pull the sqrt out of this method
            // This is the smallest radius that will fit in the grid's pixels.  If the dot is smaller than this, then it could nearly dissapear if the
            // center is between sample points
            double minDotRadius = (harness.OutputSize / harness.OutputSizeXY) * Math.Sqrt(2);

            // Convert error into real world distance: item's max speed * avg tick time
            double errorRadius = position.Item1.MaxPositionError + (additionalErrorMult ?? 0);
            errorRadius *= evaluatorArgs.MaxDistancePerTick;

            // Get the final dot radius
            double dotRadius = Math.Max(minDotRadius, errorRadius);

            // Draw onto the array
            double[] retVal = new double[harness.OutputSizeXY * harness.OutputSizeXY];
            ApplyPoint(retVal, harness.OutputCellCenters, dotRadius, position.Item2, true);

            return retVal;
        }

        #endregion
    }
}
