using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Game.HelperClassesAI;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;

namespace Game.Newt.v2.GameItems.Collections
{
    /// <summary>
    /// This acts like a list of input vectors.  It also acts like a group by, a loose dictionary
    /// </summary>
    /// <remarks>
    /// The intended use is to add sensor data to this list on a regular basis
    /// This class will take care of running those through a self organizing map in order to group similar items together
    /// When a category gets saturated with lots of nearly identical values, this class will throw out dupes
    /// So it's purpose is to keep a catalog of unique inputs
    /// 
    /// It can then be used to query for images that are similar to another image
    /// Or to get examples of images that different from some other images
    /// </remarks>
    public class SOMList
    {
        #region class: ToVectorInstructions

        private class ToVectorInstructions
        {
            public ToVectorInstructions(int[] fromSizes, int toSize, ConvolutionBase2D convolution = null, bool shouldNormalize = false, bool isColor2D = false)
            {
                this.FromSizes = fromSizes;
                this.ToSize = toSize;
                this.Convolution = convolution;
                this.ShouldNormalize = shouldNormalize;
                this.IsColor2D = isColor2D;
            }

            public readonly int[] FromSizes;
            public readonly int ToSize;

            public readonly ConvolutionBase2D Convolution;      //TODO: handle any dimensions
            /// <summary>
            /// Normalizing will make images more similar to each other (if one image is washed out compared to another, the normalized
            /// versions should be more similar)
            /// </summary>
            /// <remarks>
            /// If the data comes from arbitrary places, normalizing would be really important.  But this data should come from the same
            /// sensor.  So it probably only makes sense to normalize if the lighting conditions change a lot
            /// </remarks>
            public readonly bool ShouldNormalize;
            public readonly bool IsColor2D;
        }

        #endregion
        #region class: SOMItem

        public class SOMItem : ISOMInput
        {
            public SOMItem(VectorND original, VectorND vector)
            {
                this.Original = original;
                this.Weights = vector;
            }

            public readonly VectorND Original;
            public VectorND Weights { get; private set; }
        }

        #endregion

        #region Declaration Section

        private const int BATCHMIN = 30;
        private const int BATCHMAX = BATCHMIN * 3;

        private readonly object _lock = new object();

        private readonly ToVectorInstructions _instructions;

        private readonly bool _discardDupes;
        private readonly double _dupeDistSquared;

        private readonly List<SOMItem> _newItems = new List<SOMItem>();
        private int _nextBatchSize = StaticRandom.Next(BATCHMIN, BATCHMAX);
        private bool _isProcessingBatch = false;

        private volatile SOMResult _result = null;

        #endregion

        #region Constructor

        /// <param name="itemDimensions">
        /// The width, height, depth, etc of each item
        /// </param>
        /// <param name="convolution">
        /// This gives a chance to run an edge detect, or some other convolution
        /// TODO: Support convolutions that can handle arbitrary dimensions
        /// </param>
        /// <param name="shouldNormalize">
        /// Forces all inputs to go between 0 and 1.
        /// NOTE: Only do this if the inputs come from varied sources
        /// </param>
        /// <param name="dupeDistance">
        /// If two items are closer together than this, they will be treated like they are the same.
        /// NOTE: If all the inputs are 0 to 1 (or even -1 to 1), then the default value should be fine.  But if the
        /// inputs have larger values (like 0 to 255), you would want a larger min value
        /// </param>
        public SOMList(int[] itemDimensions, ConvolutionBase2D convolution = null, bool shouldNormalize = false, bool discardDupes = true, double dupeDistance = .001, bool isColor2D = false)
        {
            _instructions = new ToVectorInstructions(itemDimensions, GetResize(itemDimensions), convolution, shouldNormalize, isColor2D);
            _dupeDistSquared = dupeDistance * dupeDistance;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// This is exposed so debuggers can view contents
        /// </summary>
        public SOMResult CurrentResult
        {
            get
            {
                return _result;
            }
        }

        #endregion

        #region Public Methods

        public void Add(VectorND item)
        {
            //TODO: Probably want something that ignores images if this is too soon from a previous call

            SOMItem somItem = GetSOMItem(item, _instructions);

            SOMResult result = _result;
            if (result != null)
            {
                if (_discardDupes)
                {
                    // Run this through the SOM
                    var closest = SelfOrganizingMaps.GetClosest(result.Nodes, somItem);

                    // If it's too similar to another, then just ignore it
                    if (IsTooClose(somItem, result.InputsByNode[closest.Item2], _dupeDistSquared))
                    {
                        return;
                    }
                }
            }

            #region process batch

            // Store this in a need-to-work list
            // When that list gets to a certain size, build a new SOM
            SOMItem[] newItemBatch = null;
            lock (_lock)
            {
                _newItems.Add(somItem);

                if (!_isProcessingBatch && _newItems.Count > _nextBatchSize)
                {
                    _nextBatchSize = StaticRandom.Next(BATCHMIN, BATCHMAX);
                    newItemBatch = _newItems.ToArray();
                    _newItems.Clear();
                    _isProcessingBatch = true;
                }
            }

            if (newItemBatch != null)
            {
                Task.Run(() => { _result = ProcessNewItemBatch(newItemBatch, _discardDupes, _dupeDistSquared, _result); }).
                    ContinueWith(t => { lock (_lock) _isProcessingBatch = false; });
            }

            #endregion
        }

        public VectorND[] GetSimilarItems(VectorND item, int count)
        {
            return new VectorND[0];
        }
        public VectorND[] GetDissimilarItems(IEnumerable<VectorND> items, int count)
        {
            return new VectorND[0];
        }

        #endregion

        #region Private Methods

        private static SOMResult ProcessNewItemBatch(SOMItem[] newItemBatch, bool discardDupes, double dupeDistSquared, SOMResult existing)
        {
            const int TOTALMAX = BATCHMAX * 10;

            // Items only make it here when they aren't too similar to the som nodes, but items within this list may be dupes
            if (discardDupes)
            {
                newItemBatch = DedupeItems(newItemBatch, dupeDistSquared);
            }

            if (newItemBatch.Length > BATCHMAX)
            {
                // There are too many, just take a sample
                newItemBatch = UtilityCore.RandomRange(0, newItemBatch.Length, BATCHMAX).
                    Select(o => newItemBatch[o]).
                    ToArray();
            }

            SOMItem[] existingItems = null;
            if (existing != null)
            {
                existingItems = existing.InputsByNode.
                    SelectMany(o => o).
                    Select(o => ((SOMInput<SOMItem>)o).Source).
                    ToArray();
            }

            SOMItem[] allItems = UtilityCore.ArrayAdd(existingItems, newItemBatch);



            //TODO: This is too simplistic.  See if existingItems + newItemBatch > total.  If so, try to draw down the existing nodes evenly.  Try
            //to preserve previous images better.  Maybe even throw in a timestamp to get a good spread of times
            //
            //or get a SOM of the new, independent of the old.  Then merge the two pulling representatives to keep the most diversity.  Finally,
            //take a SOM of the combined
            if (allItems.Length > TOTALMAX)
            {
                allItems = UtilityCore.RandomRange(0, allItems.Length, TOTALMAX).
                    Select(o => allItems[o]).
                    ToArray();
            }






            SOMInput<SOMItem>[] inputs = allItems.
                Select(o => new SOMInput<SOMItem>() { Source = o, Weights = o.Weights, }).
                ToArray();

            //TODO: May want rules to persist from run to run
            SOMRules rules = GetSOMRules_Rand();

            return SelfOrganizingMaps.TrainSOM(inputs, rules, true);
        }

        private static SOMItem[] DedupeItems(SOMItem[] items, double dupeDistSquared)
        {
            if (items.Length < 2)
            {
                return items;
            }

            List<SOMItem> retVal = new List<SOMItem>();

            retVal.Add(items[0]);

            for (int cntr = 1; cntr < items.Length - 1; cntr++)
            {
                IEnumerable<SOMItem> others = Enumerable.Range(cntr + 1, items.Length - cntr - 1).
                    Select(o => items[o]);

                if (!IsTooClose(items[cntr], others, dupeDistSquared))
                {
                    retVal.Add(items[cntr]);
                }
            }

            return retVal.ToArray();
        }

        private static SOMRules GetSOMRules_Rand()
        {
            Random rand = StaticRandom.GetRandomForThread();

            return new SOMRules(
                rand.Next(15, 50),
                rand.Next(2000, 5000),
                rand.NextDouble(.2, .4),
                rand.NextDouble(.05, .15));
        }

        private static SOMItem GetSOMItem(VectorND item, ToVectorInstructions instr)
        {
            switch (instr.FromSizes.Length)
            {
                case 1:
                    return GetSOMItem_1D(item, instr);

                case 2:
                    if (instr.IsColor2D) return GetSOMItem_2D_Color(item, instr);
                    else return GetSOMItem_2D_Gray(item, instr);

                default:
                    throw new ApplicationException("TODO: handle arbitrary number of dimensions");
            }
        }

        private static SOMItem GetSOMItem_1D(VectorND item, ToVectorInstructions instr)
        {
            if (instr.FromSizes[0] == instr.ToSize)
            {
                return new SOMItem(item, item);
            }

            // Create a bezier through the points, then pull points off of that curve.  Unless I read this wrong, this is what bicubic interpolation of images does (I'm just doing 1D instead of 2D)
            Point3D[] points = item.
                Select(o => new Point3D(o, 0, 0)).
                ToArray();

            BezierSegment3D[] bezier = BezierUtil.GetBezierSegments(points);

            VectorND resized = BezierUtil.GetPath(instr.ToSize, bezier).
                Select(o => o.X).
                ToArray().
                ToVectorND();

            return new SOMItem(item, resized);
        }

        private static SOMItem GetSOMItem_2D_Color(VectorND item, ToVectorInstructions instr)
        {
            int width = instr.FromSizes[0];
            int height = instr.FromSizes[1];

            #region build arrays

            double[] r = new double[width * height];
            double[] g = new double[width * height];
            double[] b = new double[width * height];

            //double scale = 1d / 255d;
            double scale = 1d;

            for (int y = 0; y < height; y++)
            {
                int yOffsetDest = y * width;		// offset into the return array
                int yOffsetSource = y * width * 3;

                for (int x = 0; x < width; x++)
                {
                    int xOffsetSource = x * 3;

                    r[yOffsetDest + x] = item[yOffsetSource + xOffsetSource + 0] * scale;
                    g[yOffsetDest + x] = item[yOffsetSource + xOffsetSource + 1] * scale;
                    b[yOffsetDest + x] = item[yOffsetSource + xOffsetSource + 2] * scale;
                }
            }

            #endregion

            Convolution2D convR = Convolute(new Convolution2D(r, width, height, false), instr);
            Convolution2D convG = Convolute(new Convolution2D(g, width, height, false), instr);
            Convolution2D convB = Convolute(new Convolution2D(b, width, height, false), instr);

            #region merge arrays

            double[] merged = new double[convR.Values.Length * 3];

            for (int cntr = 0; cntr < convR.Values.Length; cntr++)
            {
                int index = cntr * 3;
                merged[index + 0] = convR.Values[cntr];
                merged[index + 1] = convG.Values[cntr];
                merged[index + 2] = convB.Values[cntr];
            }

            #endregion

            return new SOMItem(item, merged.ToVectorND());
        }
        private static SOMItem GetSOMItem_2D_Gray(VectorND item, ToVectorInstructions instr)
        {
            Convolution2D conv = new Convolution2D(item.VectorArray, instr.FromSizes[0], instr.FromSizes[1], false);

            conv = Convolute(conv, instr);

            return new SOMItem(item, conv.Values.ToVectorND());
        }

        private static Convolution2D Convolute(Convolution2D conv, ToVectorInstructions instr)
        {
            Convolution2D retVal = conv;

            if (retVal.Width != retVal.Height)
            {
                int max = Math.Max(retVal.Width, retVal.Height);
                retVal = Convolutions.ExtendBorders(retVal, max, max);      // make it square
            }

            if (instr.ShouldNormalize)
            {
                retVal = Convolutions.Normalize(retVal);
            }

            if (instr.Convolution != null)
            {
                retVal = Convolutions.Convolute(retVal, instr.Convolution);
            }

            retVal = Convolutions.MaxPool(retVal, instr.ToSize, instr.ToSize);
            retVal = Convolutions.Abs(retVal);

            return retVal;
        }

        private static int GetResize(int[] itemDimensions)
        {
            // Ideal sizes:
            //  2D=7x7

            int retVal = -1;

            switch (itemDimensions.Length)
            {
                case 1:
                    #region 1D

                    //TODO: This is just a rough guess at an ideal range.  When there are real scenarios, adjust as necessary

                    // I don't think the s curve makes sense here.  It's too fuzzy, and has the potential to enlarge when that's not wanted
                    //double scaled = Math1D.PositiveSCurve(itemDimensions[0], 50);
                    //retVal = UtilityCore.GetScaledValue(5, 50, 0, 50, scaled).ToInt_Round();

                    if (itemDimensions[0] < 5) retVal = 5;
                    else if (itemDimensions[0] <= 50) retVal = itemDimensions[0];
                    else retVal = 50;

                    #endregion
                    break;

                case 2:
                    #region 2D

                    double avg = Math1D.Avg(itemDimensions[0], itemDimensions[1]);

                    if (avg < 3) retVal = 3;
                    else if (avg <= 7) retVal = avg.ToInt_Round();
                    else retVal = 7;

                    #endregion
                    break;

                default:
                    // The convolutions class will need to be expanded first
                    throw new ApplicationException("TODO: Handle more than 2 dimensions");
            }

            return retVal;
        }

        private static bool IsTooClose(ISOMInput item, IEnumerable<ISOMInput> others, double dupeDistSquared)
        {
            foreach (ISOMInput other in others)
            {
                double distSqr = (item.Weights - other.Weights).LengthSquared;

                if (distSqr < dupeDistSquared)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}
