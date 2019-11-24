using Game.HelperClassesAI;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Controls3D;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.GameItems.ShipParts;
using Game.Newt.v2.NewtonDynamics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Game.Newt.Testers
{
    //TODO: Use this tester as an inspiration for a ship viewer popup control.  Instead of having both the neural and ship
    //views on the same panel, make some kind carousel control.  Or make the popup itelf appear to be a 3D plate, and
    //show different aspects of the ship on each side

    //TODO: When they mouse over a neuron, use an emissive on it, and its inputs and what feeds (different colors for the
    //different directions.  Repeat this 3+ times, reducing the intensity at each step

    //TODO: Do something similar when over a brain chemical receptor

    //TODO: When they mouse over a neuron, show a pulse travel from it through the network

    //TODO: Superimpose a number in the brain chemical neurons

    public partial class BrainTester : Window
    {
        #region class: ItemColors

        internal class ItemColors
        {
            public Color BoundryLines = UtilityWPF.ColorFromHex("C4BDAB");

            public Color TrackballAxisMajorColor = UtilityWPF.ColorFromHex("4C638C");
            public DiffuseMaterial TrackballAxisMajor = new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("4C638C")));
            public Color TrackballAxisLine = Color.FromArgb(96, 117, 108, 97);
            public SpecularMaterial TrackballAxisSpecular = new SpecularMaterial(Brushes.White, 100d);
            public Color TrackballGrabberHoverLight = UtilityWPF.ColorFromHex("3F382A");

            public Color Neuron_Zero_NegPos = UtilityWPF.ColorFromHex("20808080");
            public Color Neuron_Zero_ZerPos = UtilityWPF.ColorFromHex("205E88D1");

            public Color Neuron_One_ZerPos = UtilityWPF.ColorFromHex("326CD1");

            public Color Neuron_NegOne_NegPos = UtilityWPF.ColorFromHex("D63633");
            public Color Neuron_One_NegPos = UtilityWPF.ColorFromHex("25994A");

            public Color Link_Negative = UtilityWPF.ColorFromHex("FC0300");
            public Color Link_Positive = UtilityWPF.ColorFromHex("00B237");
        }

        #endregion
        #region class: LinkTestUtil

        private class LinkTestUtil
        {
            #region class: LinkResult1

            public class LinkResult1
            {
                public LinkResult1(int from, int to, double weight)
                {
                    this.From = from;
                    this.To = to;
                    this.Weight = weight;
                }

                public readonly int From;
                public readonly int To;
                public readonly double Weight;
            }

            #endregion
            #region class: LinkResult2

            public class LinkResult2
            {
                public LinkResult2(bool isExactMatch, int index, double percent)
                {
                    this.IsExactMatch = isExactMatch;
                    this.Index = index;
                    this.Percent = percent;
                }

                public readonly bool IsExactMatch;
                public readonly int Index;
                public readonly double Percent;
            }

            #endregion
            #region class: LinkResult3

            public class LinkResult3
            {
                public LinkResult3(LinkResult2 from, LinkResult2 to, double percent)
                {
                    this.From = from;
                    this.To = to;
                    this.Percent = percent;
                }

                public readonly LinkResult2 From;
                public readonly LinkResult2 To;
                public readonly double Percent;
            }

            #endregion

            /// <summary>
            /// This is a case where the dna says to make a link between from and to, but there is no neuron at to, instead there are neurons
            /// at toNew.  This needs to do some fuzzy linking
            /// </summary>
            /// <remarks>
            /// TODO: This approach will create extra, and never remove (but there could be an independent sweep that gets rid of very small links based on how many this creates?)
            /// </remarks>
            public static LinkResult1[] Test1(Point3D from, Point3D to, Point3D[] toNew, double weight)
            {
                const double SEARCHRADIUSMULT = 2.5d;		// looks at other nodes that are up to minradius * mult
                const int MAXDIVISIONS = 3;

                // Check for exact match
                int index = FindExact(to, toNew);
                if (index >= 0)
                {
                    return new LinkResult1[] { new LinkResult1(0, index, weight) };
                }

                // Get a list of nodes that are close to the search point
                var nearNodes = GetNearNodes(to, toNew, SEARCHRADIUSMULT);

                if (nearNodes.Count == 1)
                {
                    // There's only one, so give it the full weight
                    return new LinkResult1[] { new LinkResult1(0, nearNodes[0].Item1, weight) };
                }

                // Don't allow too many divisions
                if (nearNodes.Count > MAXDIVISIONS)
                {
                    nearNodes = nearNodes.
                        OrderBy(o => o.Item2).
                        Take(MAXDIVISIONS).
                        ToList();
                }

                // Figure out what percent of the weight to give these nodes (based on the ratio of their distances to the search point)
                var percents = GetPercentOfWeight(nearNodes, SEARCHRADIUSMULT);

                // Exit Function
                return percents.Select(o => new LinkResult1(0, o.Item1, weight * o.Item2)).ToArray();
            }
            /// <remarks>
            /// This is a reworking of Test1.  It will be up to the caller to get the list of points referenced by all the dna links, then
            /// call this method for each referenced point
            /// </remarks>
            public static LinkResult2[] Test2(Point3D search, Point3D[] points, int maxReturn)
            {
                const double SEARCHRADIUSMULT = 2.5d;		// looks at other nodes that are up to minradius * mult

                // Check for exact match
                int index = FindExact(search, points);
                if (index >= 0)
                {
                    return new LinkResult2[] { new LinkResult2(true, index, 1d) };
                }

                // Get a list of nodes that are close to the search point
                var nearNodes = GetNearNodes(search, points, SEARCHRADIUSMULT);

                if (nearNodes.Count == 1)
                {
                    // There's only one, so give it the full weight
                    return new LinkResult2[] { new LinkResult2(false, nearNodes[0].Item1, 1d) };
                }

                // Don't allow too many divisions
                if (nearNodes.Count > maxReturn)
                {
                    nearNodes = nearNodes.
                        OrderBy(o => o.Item2).
                        Take(maxReturn).
                        ToList();
                }

                // Figure out what percent of the weight to give these nodes (based on the ratio of their distances to the search point)
                var percents = GetPercentOfWeight(nearNodes, SEARCHRADIUSMULT);

                // Exit Function
                return percents.Select(o => new LinkResult2(false, o.Item1, o.Item2)).ToArray();
            }

            public static LinkResult3[] Test3(Point3D fromSearch, Point3D toSearch, LinkResult2[] from, LinkResult2[] to, Point3D[] points)
            {

                throw new ApplicationException("flawed");


                // Check for exact match
                if (from.Length == 1 && to.Length == 1)
                {
                    return new LinkResult3[] { new LinkResult3(from[0], to[0], 1d) };
                }

                // Get a line between the two points
                Vector3D line = toSearch - fromSearch;



                #region FLAWED

                // I started by defining an up vector, but the points could go left or right of in, so I would need a double vector.  Rather than go
                // through all that effort, just dot the froms to the tos directly

                //// Get an arbitrary orthoganal that will be defined as an up vector
                //Vector3D orth = Math3D.GetArbitraryOrhonganal(line).ToUnit();

                //double[] fromDots = GetDots(from, orth);
                //double[] toDots = GetDots(to, orth);

                #endregion






                // Get the best matches






            }
            /// <summary>
            /// Test2 takes an original point, and chooses the nearest X items from the new positions.
            /// 
            /// Say two of those original points had a link.  Now you could have several new points that correspond to one
            /// of the originals, linked to several that correspond to the second original
            /// 
            /// This method creates links from one set to the other
            /// </summary>
            public static LinkResult3[] Test4(LinkResult2[] from, LinkResult2[] to, int maxReturn)
            {
                // Find the combinations that have the highest percentage
                List<Tuple<int, int, double>> products = new List<Tuple<int, int, double>>();
                for (int fromCntr = 0; fromCntr < from.Length; fromCntr++)
                {
                    for (int toCntr = 0; toCntr < to.Length; toCntr++)
                    {
                        products.Add(new Tuple<int, int, double>(fromCntr, toCntr, from[fromCntr].Percent * to[toCntr].Percent));
                    }
                }

                // Don't return too many
                IEnumerable<Tuple<int, int, double>> topProducts = null;
                if (products.Count <= maxReturn)
                {
                    topProducts = products;		// no need to sort or limit
                }
                else
                {
                    topProducts = products.
                        OrderByDescending(o => o.Item3).
                        Take(maxReturn).
                        ToArray();
                }

                // Normalize
                double totalPercent = topProducts.Sum(o => o.Item3);

                LinkResult3[] retVal = topProducts.
                    Select(o => new LinkResult3(from[o.Item1], to[o.Item2], o.Item3 / totalPercent)).
                    ToArray();

                return retVal;
            }

            #region Private Methods

            private static int FindExact(Point3D search, Point3D[] points)
            {
                for (int cntr = 0; cntr < points.Length; cntr++)
                {
                    if (Math3D.IsNearValue(points[cntr], search))
                    {
                        return cntr;
                    }
                }

                return -1;
            }

            /// <summary>
            /// This returns the index and distance of the nodes that are close to search
            /// </summary>
            private static List<Tuple<int, double>> GetNearNodes(Point3D search, Point3D[] points, double searchRadiusMultiplier)
            {
                // Get the distances to each point
                double[] distSquared = points.Select(o => (o - search).LengthSquared).ToArray();

                // Find the smallest distance
                int smallestIndex = 0;
                for (int cntr = 1; cntr < distSquared.Length; cntr++)
                {
                    if (distSquared[cntr] < distSquared[smallestIndex])
                    {
                        smallestIndex = cntr;
                    }
                }

                // Figure out how far out to allow
                double min = Math.Sqrt(distSquared[smallestIndex]);
                double maxSquared = Math.Pow(min * searchRadiusMultiplier, 2d);

                // Find all the points in range
                List<Tuple<int, double>> retVal = new List<Tuple<int, double>>();

                // This one is obviously in range (adding it now to avoid an unnessary sqrt)
                retVal.Add(new Tuple<int, double>(smallestIndex, min));

                for (int cntr = 0; cntr < distSquared.Length; cntr++)
                {
                    if (cntr == smallestIndex)
                    {
                        continue;
                    }

                    if (distSquared[cntr] < maxSquared)
                    {
                        retVal.Add(new Tuple<int, double>(cntr, Math.Sqrt(distSquared[cntr])));
                    }
                }

                // Exit Function
                return retVal;
            }

            /// <summary>
            /// This takes in a list of distances, and returns a list of percents (the int just comes along for the ride)
            /// </summary>
            private static List<Tuple<int, double>> GetPercentOfWeight(List<Tuple<int, double>> distances, double searchRadiusMultiplier)
            {
                const double OFFSET = .1d;

                // Find the smallest distance in the list
                double min = distances.Min(o => o.Item2);

                // Figure out what the maximum possible distance would be
                double maxRange = (min * searchRadiusMultiplier) - min;

                // Figure out ratios based on distance
                double[] ratios = new double[distances.Count];
                for (int cntr = 0; cntr < ratios.Length; cntr++)
                {
                    // Normalize the distance
                    ratios[cntr] = UtilityCore.GetScaledValue_Capped(0d, 1d, 0d, maxRange, distances[cntr].Item2 - min);

                    // Run it through a function
                    ratios[cntr] = 1d / (ratios[cntr] + OFFSET);		// need to add an offset, because one of these will be zero
                }

                double total = ratios.Sum();

                // Turn those ratios into percents (normalizing the ratios)
                List<Tuple<int, double>> retVal = new List<Tuple<int, double>>();
                for (int cntr = 0; cntr < ratios.Length; cntr++)
                {
                    retVal.Add(new Tuple<int, double>(distances[cntr].Item1, ratios[cntr] / total));
                }

                // Exit Function
                return retVal;
            }
            private static List<Tuple<int, double>> GetPercentOfWeight_OLD(List<Tuple<int, double>> distances, double searchRadiusMultiplier)
            {
                const double AMOUNTATMAX = .01d;

                double min = distances.Min(o => o.Item2);
                double max = min * searchRadiusMultiplier * 10;		// making max greater so that there is more of a distance

                // The equation will be y=1/kx
                // Where x is the distance from center
                // So solving for k, k=1/xy
                double k = 1d / (max * AMOUNTATMAX);

                double[] scaled = distances.Select(o => 1d / (k * o.Item2)).ToArray();

                double total = scaled.Sum();

                List<Tuple<int, double>> retVal = new List<Tuple<int, double>>();
                for (int cntr = 0; cntr < distances.Count; cntr++)
                {
                    retVal.Add(new Tuple<int, double>(distances[cntr].Item1, scaled[cntr] / total));
                }

                // Exit Function
                return retVal;
            }

            /// <summary>
            /// This returns each link dotted with up
            /// NOTE: up must be a unit vector
            /// </summary>
            private static double[] GetDots(LinkResult2[] links, DoubleVector up, Point3D center, Vector3D line, Point3D[] points)
            {

                throw new ApplicationException("flawed");


                // If it's an exact match, then the link is sitting on the center point
                if (links.Length == 1 && links[0].IsExactMatch)
                {
                    return null;
                }

                double[] retVal = new double[links.Length];

                for (int cntr = 0; cntr < links.Length; cntr++)
                {
                    if (links[cntr].IsExactMatch)
                    {
                        throw new ApplicationException("Can't have an exact match in a list of non exact matches");		// or in any list greater than one
                    }

                    Point3D pointOnLine = Math3D.GetClosestPoint_Line_Point(center, line, points[links[cntr].Index]);
                    // See if it's on the line
                    //if(




                }

                // Exit Function
                return retVal;
            }

            #endregion
        }

        #endregion
        #region class: GravSensorStuff

        private class GravSensorStuff
        {
            public SensorGravity Sensor = null;
            public Body Body = null;
            public Visual3D Visual = null;
            public List<Tuple<INeuron, SolidColorBrush>> Neurons = null;
            public Visual3D NeuronVisual = null;

            public ScreenSpaceLines3D Gravity = null;
        }

        #endregion
        #region class: BrainStuff

        private class BrainStuff
        {
            public Brain BrainStandard = null;
            public BrainNEAT BrainNEAT = null;
            // Both brains implement all 3 interfaces, so expose each interface on its own
            public (PartBase part, INeuronContainer container, IPartUpdatable update) Brain
            {
                get
                {
                    if (BrainStandard != null)
                    {
                        return (BrainStandard, BrainStandard, BrainStandard);
                    }
                    else
                    {
                        return (BrainNEAT, BrainNEAT, BrainNEAT);
                    }
                }
            }

            public Body Body = null;
            public Visual3D Visual = null;
            public List<Tuple<INeuron, SolidColorBrush>> Neurons = null;
            public Visual3D NeuronVisual = null;

            public NeuralLinkDNA[] DNAInternalLinks = null;
            public NeuralLinkExternalDNA[] DNAExternalLinks = null;
        }

        #endregion
        #region class: ThrusterStuff

        private class ThrusterStuff
        {
            public Thruster Thrust = null;
            public Body Body = null;
            public Visual3D Visual = null;
            public List<Tuple<INeuron, SolidColorBrush>> Neurons = null;
            public Visual3D NeuronVisual = null;

            public NeuralLinkExternalDNA[] DNAExternalLinks = null;
        }

        #endregion
        #region class: ImpulseStuff

        private class ImpulseStuff
        {
            public ImpulseEngine Impulse = null;
            public Body Body = null;
            public Visual3D Visual = null;
            public List<Tuple<INeuron, SolidColorBrush>> Neurons = null;
            public Visual3D NeuronVisual = null;

            public NeuralLinkExternalDNA[] DNAExternalLinks = null;
        }

        #endregion
        #region class: ContainerStuff

        private class ContainerStuff
        {
            public EnergyTank Energy = null;
            public Body EnergyBody = null;
            public Visual3D EnergyVisual = null;

            public FuelTank Fuel = null;
            public Body FuelBody = null;
            public Visual3D FuelVisual = null;

            public PlasmaTank Plasma = null;
            public Body PlasmaBody = null;
            public Visual3D PlasmaVisual = null;
        }

        #endregion
        #region class: LinkStuff

        private class LinkStuff
        {
            public NeuralUtility.ContainerOutput[] Outputs = null;
            //public List<Visual3D> Lines = null;
            public List<Visual3D> Visuals = null;
        }

        #endregion

        #region class: NewLinker (temp)

        private static class NewLinker
        {
            #region class: BrainCluster

            private class BrainCluster
            {
                public int DesiredCount { get; set; }
                public Point3D NodeCenter { get; set; }
                public List<INeuron> Neurons { get; set; }
            }

            #endregion

            //TODO: Can't do brains in isolation, because brains need to link to themselves
            //Figure out ratios, then add links one at a time (cycling through the brains as well), until all neurons are accounted for

            public static NeuralUtility.ContainerOutput LinkNeurons_Sensors(NeuralUtility.ContainerInput[] sensors, NeuralUtility.ContainerInput brain)
            {
                if (sensors == null || sensors.Length == 0)
                {
                    throw new ArgumentException("No sensors passed in");
                }

                int brainCount = brain.WritableNeurons.Count();
                if (brainCount <= sensors.Length)
                {
                    // This should be really rare.  Entire sensors will map to individual neurons
                    return LinkNeurons_AllToOne(sensors, brain);
                }

                List<NeuralLink> retVal = new List<NeuralLink>();

                Random rand = StaticRandom.GetRandomForThread();

                int[] sensorCounts = sensors.
                    Select(o => o.ReadableNeurons.Count()).
                    ToArray();

                int[] useCounts = GetOutputUsageCounts_sensors(sensorCounts, brainCount);

                INeuron[][] useNeurons = DivideOutput_sensors(sensors, useCounts, brain);

                for (int cntr = 0; cntr < sensors.Length; cntr++)
                {
                    retVal.AddRange(Link_InputToSetOfOutputs(sensors[cntr], brain, useNeurons[cntr]));
                }

                return new NeuralUtility.ContainerOutput(brain.Container, null, retVal.ToArray());
            }
            public static NeuralUtility.ContainerOutput[] LinkNeurons_Manipulators(NeuralUtility.ContainerInput[] manipulators, NeuralUtility.ContainerInput brain)
            {
                // This probably doesn't work for manipulators.  You probably need close to a 1:1 neuron ratio between brain an manipulator, or fewer manipulator neurons than
                // brain neurons.  Going the other direction would just cause the manipulator to spaz out

                int brainCount = brain.ReadableNeurons.Count();
                if (brainCount <= manipulators.Length)
                {
                    // This should be really rare.  Entire sensors will map to individual neurons
                    throw new ApplicationException("TODO: Handle fewer neurons than outputs");
                }

                Random rand = StaticRandom.GetRandomForThread();

                int[] manipulatorCounts = manipulators.
                    Select(o => o.WritableNeurons.Count()).
                    ToArray();

                int[] useCounts = GetOutputUsageCounts_manipulators(brainCount, manipulatorCounts);

                //NOTE: This is returning brain's neurons, but should be returning manipulator's - according to how LinkNeurons_Sensors and Link_InputToSetOfOutputs were written
                INeuron[][] useNeurons = DivideOutput_manipulators(manipulators, useCounts, brain);

                NeuralUtility.ContainerOutput[] retVal = new NeuralUtility.ContainerOutput[useNeurons.Length];

                for (int cntr = 0; cntr < manipulators.Length; cntr++)
                {
                    retVal[cntr] = new NeuralUtility.ContainerOutput(manipulators[cntr].Container, null, Link_BrainToManipulator(brain, manipulators[cntr], useNeurons[cntr]));
                }

                return retVal;

                #region FAIL

                ////NOTE: This is returning brain's neurons, but should be returning manipulator's - according to how LinkNeurons_Sensors and Link_InputToSetOfOutputs were written
                //INeuron[][] useNeurons = DivideOutput_manipulators(manipulators, useCounts, brain);

                //NeuralUtility.ContainerOutput[] retVal = new NeuralUtility.ContainerOutput[useNeurons.Length];

                //for (int cntr = 0; cntr < manipulators.Length; cntr++)
                //{
                //    //retVal[cntr] = new NeuralUtility.ContainerOutput(manipulators[cntr].Container, null, Link_InputToSetOfOutputs(brain, manipulators[cntr], useNeurons[cntr]));

                //    var links = Link_InputToSetOfOutputs(manipulators[cntr], brain, useNeurons[cntr]).      // passing the arguments backward, because this method thinks useNeurons is the output
                //        Select(o => new NeuralLink(o.ToContainer, o.FromContainer, o.To, o.From, o.Weight, o.BrainChemicalModifiers)).      // reverse it
                //        ToArray();
                //    retVal[cntr] = new NeuralUtility.ContainerOutput(manipulators[cntr].Container, null, links);
                //}

                ////return new NeuralUtility.ContainerOutput(brain.Container, null, retVal.ToArray());
                //return retVal;

                #endregion
            }

            #region Private Methods

            private static NeuralUtility.ContainerOutput LinkNeurons_AllToOne(NeuralUtility.ContainerInput[] sensors, NeuralUtility.ContainerInput brain)
            {
                List<NeuralLink> retVal = new List<NeuralLink>();

                // There are more sensors than there are brain neurons, so set up an iterator that loops through
                // all the brain's neurons, then again, again, forever
                var outputIterator = InfiniteRandomOrder(brain.WritableNeurons.ToArray()).GetEnumerator();
                outputIterator.MoveNext();

                foreach (NeuralUtility.ContainerInput sensor in sensors)
                {
                    // Map all of this sensor's neurons onto one of the brain's neurons
                    foreach (INeuron input in sensor.ReadableNeurons)
                    {
                        retVal.Add(new NeuralLink(sensor.Container, brain.Container, input, outputIterator.Current, 1d, null));
                    }

                    outputIterator.MoveNext();
                }

                return new NeuralUtility.ContainerOutput(brain.Container, null, retVal.ToArray());
            }

            /// <summary>
            /// This maps all of the input's neurons to the set of output's neurons
            /// </summary>
            private static NeuralLink[] Link_InputToSetOfOutputs(NeuralUtility.ContainerInput input, NeuralUtility.ContainerInput output, INeuron[] outputNeurons)
            {
                List<NeuralLink> retVal = new List<NeuralLink>();

                // There are more input neurons than there are output neurons (or the count is exact), so set up an iterator that
                // loops through all the output's neurons, then again, again, forever
                var outputIterator = InfiniteRandomOrder(outputNeurons).GetEnumerator();
                outputIterator.MoveNext();

                // Map all of this sensor's neurons onto one of the brain's neurons
                foreach (INeuron inputNeuron in input.ReadableNeurons)
                {
                    retVal.Add(new NeuralLink(input.Container, output.Container, inputNeuron, outputIterator.Current, 1d, null));
                    outputIterator.MoveNext();
                }

                return retVal.ToArray();
            }
            private static NeuralLink[] Link_BrainToManipulator(NeuralUtility.ContainerInput brain, NeuralUtility.ContainerInput manipulator, INeuron[] brainNeurons)
            {
                List<NeuralLink> retVal = new List<NeuralLink>();

                var outputIterator = InfiniteRandomOrder(manipulator.WritableNeurons.ToArray()).GetEnumerator();
                outputIterator.MoveNext();

                // Map all of this brain's neurons manipulator's neurons
                foreach (INeuron inputNeuron in brainNeurons)
                {
                    retVal.Add(new NeuralLink(brain.Container, manipulator.Container, inputNeuron, outputIterator.Current, 1d, null));
                    outputIterator.MoveNext();
                }

                return retVal.ToArray();
            }

            /// <summary>
            /// This gets called when the sum of input neurons is greater than the output neurons.  It tells how many of the output's neurons
            /// each input gets
            /// </summary>
            /// <returns>
            /// How many of the output each input gets.  The sum of the return values will equal the outputCount that was passed in
            /// </returns>
            private static int[] GetOutputUsageCounts_sensors(int[] inputCounts, int outputCount)
            {
                int sumInputs = inputCounts.Sum();

                if (sumInputs < outputCount)
                {
                    //throw new ArgumentException(string.Format("This method can only be called when there are more inputs than outputs:  inputs={0}, outputs={1}", sumInputs, outputCount));
                    outputCount = sumInputs;
                }

                return GetOutputUsageCounts_scarcity(outputCount, inputCounts, sumInputs);
            }
            /// <summary>
            /// This one maps a brain to a set of manipulators
            /// </summary>
            /// <param name="inputCount">How many of a brain's neurons are available to run the manipulators</param>
            /// <param name="outputCounts">How many neurons are in each manipulator</param>
            private static int[] GetOutputUsageCounts_manipulators(int inputCount, int[] outputCounts)
            {
                int sumOutputs = outputCounts.Sum();

                if (inputCount >= sumOutputs)
                {
                    // There's enough for all the outputs
                    return outputCounts.ToArray();      // using to array to clone what was passed in - just in case
                }

                return GetOutputUsageCounts_scarcity(inputCount, outputCounts, sumOutputs);
            }
            private static int[] GetOutputUsageCounts_scarcity(int count1, int[] counts2, int sumCount2)
            {
                // Calculate the ratio that each output is of the total.  That way larger buckets will get more of the input
                double[] ratios = counts2.
                    Select(o => o.ToDouble() / sumCount2.ToDouble()).
                    ToArray();

                // Calculate the initial assignments (taking floor instead of rounding, to make sure too many don't get assigned)
                int[] retVal = ratios.
                    Select(o => Math.Max(1, (o * count1).ToInt_Floor())).
                    ToArray();

                int gap = count1 - retVal.Sum();

                if (gap < 0)
                {
                    throw new ApplicationException("Even though the below logic fixes this gap, it causes an infinite loop later by the consumer.  Need to track down that infinite loop");

                    // This can happen when some containers have a very small number of neurons (maybe only when they have 1?).  Just
                    // remove from the largest until the gap is zero
                    for (int cntr = gap; cntr < 0; cntr++)
                    {
                        var index1 = retVal.
                            Select((o, i) => new { Index = i, Count = o }).
                            Where(o => o.Count > 1).
                            ToLookup(o => o.Count).
                            OrderByDescending(o => o.Key).
                            FirstOrDefault();

                        int index = -1;
                        if (index1 == null)
                        {
                            // They're all one, and there's still too many.  Set one of them to zero
                            var index2 = retVal.
                                Select((o, i) => new { Index = i, Count = o }).
                                Where(o => o.Count == 1).
                                ToArray();

                            if (index2.Length == 0)
                            {
                                throw new ApplicationException("This should never happen");
                            }

                            index = index2[StaticRandom.Next(index2.Length)].Index;
                        }
                        else
                        {
                            var index1a = index1.ToArray();
                            index = index1a[StaticRandom.Next(index1a.Length)].Index;
                        }

                        retVal[index]--;
                    }
                }

                Random rand = StaticRandom.GetRandomForThread();

                // Since the floor was taken, there are probably some remaining slots to fill.  Randomly assign those remaining
                //NOTE: Could use the ratios, but the remainder should only be a couple off, so just use even chance
                while (gap > 0)
                {
                    int index = rand.Next(counts2.Length);

                    if (counts2[index] > retVal[index])
                    {
                        retVal[index]++;
                        gap--;
                    }
                }

                return retVal;
            }

            private static INeuron[][] DivideOutput_sensors(NeuralUtility.ContainerInput[] sensors, int[] brainUsedPerSensor, NeuralUtility.ContainerInput brain)
            {
                const double DOT = .015;
                const double THICKNESS = .005;

                // Do an initial kmeans to cluster the brain's neurons
                SOMInput<INeuron>[] outputNeurons = brain.WritableNeurons.
                    Select(o => new SOMInput<INeuron>() { Source = o, Weights = o.Position.ToVectorND() }).
                    ToArray();

                SOMResult kmeans = SelfOrganizingMaps.TrainKMeans(outputNeurons, sensors.Length, true);

                // Find the centers of sensors and kmeans
                Point3D centerSensors = Math3D.GetCenter(sensors.Select(o => o.Position));
                Point3D centerNodes = Math3D.GetCenter(kmeans.Nodes.Select(o => o.Weights.ToPoint3D()));

                // Now get offsets from those centers
                Vector3D[] offsetsSensors = sensors.
                    Select(o => o.Position - centerSensors).
                    ToArray();

                Vector3D[] offsetsNodes = kmeans.Nodes.
                    Select(o => o.Weights.ToPoint3D() - centerNodes).
                    ToArray();

                // Match sensors to their best clusters (taking dot product between the offsets)
                var map_sensor_braincluster = GetMostLinedUp(offsetsSensors, offsetsNodes);

                #region draw 1

                //Debug3DWindow window = new Debug3DWindow();

                //Color[] colors = UtilityWPF.GetRandomColors(kmeans.Nodes.Length, 128, 200);

                //for (int cntr = 0; cntr < kmeans.Nodes.Length; cntr++)
                //{
                //    foreach (var neuron in kmeans.InputsByNode[cntr])
                //    {
                //        window.AddDot(neuron.Weights.ToPoint3D(), DOT, colors[cntr]);
                //    }

                //    window.AddDot(kmeans.Nodes[cntr].Weights.ToPoint3D(), DOT / 2, UtilityWPF.AlphaBlend(colors[cntr], Colors.Transparent, .25));

                //    window.AddLine(centerSensors, centerSensors + offsetsSensors[cntr], THICKNESS, Colors.Black);
                //    window.AddLine(centerNodes, centerNodes + offsetsNodes[cntr], THICKNESS, Colors.White);

                //    window.AddText(brainUsedPerSensor[cntr].ToString(), false);
                //    window.AddText(kmeans.InputsByNode[cntr].Length.ToString(), true);
                //}

                //window.Show();

                #endregion

                var brainClusters1 = map_sensor_braincluster.
                    Select(o => new BrainCluster()
                    {
                        DesiredCount = brainUsedPerSensor[o.index1],
                        NodeCenter = kmeans.Nodes[o.index2].Weights.ToPoint3D(),
                        Neurons = kmeans.InputsByNode[o.index2].
                            Select(p => ((SOMInput<INeuron>)p).Source).
                            ToList()
                    }).
                    ToArray();

                INeuron[][] brainClusters2 = AdjustNodes(brainClusters1);

                #region draw 2

                //window = new Debug3DWindow()
                //{
                //    Background = new SolidColorBrush(UtilityWPF.ColorFromHex("222")),
                //};

                //for (int cntr = 0; cntr < sensors.Length; cntr++)
                //{
                //    foreach (var neuron in brainClusters2[cntr])
                //    {
                //        window.AddDot(neuron.Position, DOT, colors[cntr]);
                //    }

                //    Point3D neuronCenter = Math3D.GetCenter(brainClusters2[cntr].Select(o => o.Position));
                //    window.AddDot(neuronCenter, DOT / 2, UtilityWPF.AlphaBlend(colors[cntr], Colors.Transparent, .25));

                //    window.AddLine(centerSensors, centerSensors + offsetsSensors[cntr], THICKNESS, colors[cntr]);
                //    window.AddLine(centerNodes, neuronCenter, THICKNESS, colors[cntr]);

                //    window.AddText(brainUsedPerSensor[cntr].ToString(), false, UtilityWPF.ColorToHex(colors[cntr]));
                //    window.AddText(brainClusters2[cntr].Length.ToString(), true, UtilityWPF.ColorToHex(colors[cntr]));
                //}

                //window.Show();

                #endregion

                return brainClusters2;
            }
            private static INeuron[][] DivideOutput_manipulators(NeuralUtility.ContainerInput[] manipulators, int[] brainUsedPerManipulator, NeuralUtility.ContainerInput brain)
            {
                const double DOT = .015;
                const double THICKNESS = .005;

                // Do an initial kmeans to cluster the brain's neurons
                SOMInput<INeuron>[] inputNeurons = brain.ReadableNeurons.
                    Select(o => new SOMInput<INeuron>() { Source = o, Weights = o.Position.ToVectorND() }).
                    ToArray();

                SOMResult kmeans = SelfOrganizingMaps.TrainKMeans(inputNeurons, manipulators.Length, true);

                // Find the centers of manipulators and kmeans
                Point3D centerManipulators = Math3D.GetCenter(manipulators.Select(o => o.Position));
                Point3D centerNodes = Math3D.GetCenter(kmeans.Nodes.Select(o => o.Weights.ToPoint3D()));

                // Now get offsets from those centers
                Vector3D[] offsetsManipulators = manipulators.
                    Select(o => o.Position - centerManipulators).
                    ToArray();

                Vector3D[] offsetsNodes = kmeans.Nodes.
                    Select(o => o.Weights.ToPoint3D() - centerNodes).
                    ToArray();

                // Match manipulators to their best clusters (taking dot product between the offsets)
                var map_manipulator_braincluster = GetMostLinedUp(offsetsManipulators, offsetsNodes);

                var brainClusters1 = map_manipulator_braincluster.
                    Select(o => new BrainCluster()
                    {
                        DesiredCount = brainUsedPerManipulator[o.index1],
                        NodeCenter = kmeans.Nodes[o.index2].Weights.ToPoint3D(),
                        Neurons = kmeans.InputsByNode[o.index2].
                            Select(p => ((SOMInput<INeuron>)p).Source).
                            ToList()
                    }).
                    ToArray();

                INeuron[][] brainClusters2 = AdjustNodes(brainClusters1);

                return brainClusters2;
            }

            private static (int index1, int index2)[] GetMostLinedUp(Vector3D[] offsets1, Vector3D[] offsets2)
            {
                if (offsets1.Length != offsets2.Length)
                {
                    throw new ArgumentException(string.Format("The two arrays need to be the same length: offsets1={0}, offsets2={1}", offsets1.Length, offsets2.Length));
                }

                //Color[] colors1 = UtilityWPF.GetRandomColors(offsets1.Length, 64, 100);
                //Color[] colors2 = UtilityWPF.GetRandomColors(offsets2.Length, 156, 192);

                // Convert the lines passed in into unit vectors
                var unit1 = offsets1.
                    Select((o, i) => (vect: o.ToUnit(), index: i /*, color: colors1[i] */)).
                    ToArray();

                var unit2 = offsets2.
                    Select((o, i) => (vect: o.ToUnit(), index: i /*, color: colors2[i] */)).
                    ToArray();

                // Get all possible pairs of lines and get the dot product
                var dots = UtilityCore.Collate(unit1, unit2).
                    Select(o => new
                    {
                        pair = o,
                        dot = Vector3D.DotProduct(o.Item1.vect, o.Item2.vect),
                    }).
                    ToArray();

                // Now get all possible sets of pairs
                var combos = UtilityCore.AllUniquePairSets(unit1.Length).
                    Select(o => o.
                        Select(p => new
                        {
                            index = p,
                            dot = dots.First(q => q.pair.Item1.index == p.index1 && q.pair.Item2.index == p.index2),
                        }).
                        ToArray()).
                    ToArray();

                #region draw 1

                //const double DOT = .015;
                //const double THICKNESS = .025;

                //Debug3DWindow window = new Debug3DWindow()
                //{
                //    Background = Brushes.Black,
                //};

                //foreach (var item in unit1)
                //{
                //    window.AddLine(new Point3D(0, 0, 0), item.vect.ToPoint(), THICKNESS, item.color);
                //    window.AddText3D(item.index.ToString(), (item.vect * 1.5).ToPoint(), item.vect, .5, item.color, true);
                //}

                //foreach (var item in unit2)
                //{
                //    window.AddLine(new Point3D(0, 0, 0), item.vect.ToPoint(), THICKNESS, item.color);
                //    window.AddText3D(item.index.ToString(), (item.vect * 1.5).ToPoint(), item.vect, .5, item.color, true);
                //}

                //double increment = 1.5;
                //double offset = 2;

                //foreach (var pair in dots)
                //{
                //    offset += increment;

                //    Vector3D offsetVect = new Vector3D(0, 0, offset);

                //    window.AddLine(offsetVect, pair.pair.Item1.vect + offsetVect, THICKNESS, pair.pair.Item1.color);
                //    window.AddLine(offsetVect, pair.pair.Item2.vect + offsetVect, THICKNESS, pair.pair.Item2.color);

                //    string text = string.Format("{0} - {1}: {2}", pair.pair.Item1.index, pair.pair.Item2.index, pair.dot.ToStringSignificantDigits(2));
                //    window.AddText3D(text, new Point3D(1.5, 0, offset), new Vector3D(1, 0, 0), increment * .4, Colors.Gray, true, new Vector3D(0, 1, 0));
                //}

                //window.Show();

                #endregion
                #region draw 2

                //foreach (var combo in combos)
                //{
                //    window = new Debug3DWindow()
                //    {
                //        Background = new SolidColorBrush(UtilityWPF.ColorFromHex("303030")),
                //    };

                //    Color[] colors = UtilityWPF.GetRandomColors(combo.Length, 100, 180);

                //    //foreach (var pair in combo)
                //    for (int cntr = 0; cntr < combo.Length; cntr++)
                //    {
                //        window.AddLine(new Vector3D(0, 0, 0), combo[cntr].dot.pair.Item1.vect, THICKNESS / 2, colors[cntr]);
                //        window.AddLine(new Vector3D(0, 0, 0), combo[cntr].dot.pair.Item2.vect, THICKNESS / 2, colors[cntr]);

                //        window.AddText(string.Format("{0} - {1}: {2}", combo[cntr].index.index1, combo[cntr].index.index2, combo[cntr].dot.dot.ToStringSignificantDigits(2)), color: UtilityWPF.ColorToHex(colors[cntr]));
                //    }

                //    window.AddText(string.Format("total: {0}", combo.Sum(o => o.dot.dot).ToStringSignificantDigits(2)));

                //    window.Show();
                //}

                #endregion

                // Best match could be most number of positive dot products, or maybe the one with the highest dot product, but
                // I think the best overall would just be the highest sum of dot products
                var best = combos.
                    OrderByDescending(o => o.Sum(p => p.dot.dot)).
                    First();

                return best.
                    Select(o => o.index).
                    ToArray();
            }

            private static INeuron[][] AdjustNodes(BrainCluster[] brainClusters)
            {
                #region draw 1

                //const double DOT = .015;
                //const double THICKNESS = .005;

                //Debug3DWindow window = new Debug3DWindow()
                //{
                //    Background = new SolidColorBrush(UtilityWPF.ColorFromHex("333")),
                //};

                //Color[] colors = UtilityWPF.GetRandomColors(brainClusters.Length, 100, 180);

                //for (int cntr = 0; cntr < brainClusters.Length; cntr++)
                //{
                //    foreach (var neuron in brainClusters[cntr].Neurons)
                //    {
                //        window.AddDot(neuron.Position, DOT, colors[cntr]);
                //    }

                //    window.AddDot(brainClusters[cntr].NodeCenter, DOT / 2, UtilityWPF.AlphaBlend(colors[cntr], Colors.Transparent, .25));

                //    window.AddText(string.Format("desired: {0}, actual: {1}", brainClusters[cntr].DesiredCount, brainClusters[cntr].Neurons.Count), color: UtilityWPF.ColorToHex(colors[cntr]));
                //}

                //window.Show();

                #endregion

                AdjustNodes_TransferHighToLow(brainClusters);

                #region draw 2

                //window = new Debug3DWindow()
                //{
                //    Background = new SolidColorBrush(UtilityWPF.ColorFromHex("444")),
                //};

                //for (int cntr = 0; cntr < brainClusters.Length; cntr++)
                //{
                //    foreach (var neuron in brainClusters[cntr].Neurons)
                //    {
                //        window.AddDot(neuron.Position, DOT, colors[cntr]);
                //    }

                //    window.AddDot(brainClusters[cntr].NodeCenter, DOT / 2, UtilityWPF.AlphaBlend(colors[cntr], Colors.Transparent, .25));

                //    window.AddText(string.Format("desired: {0}, actual: {1}", brainClusters[cntr].DesiredCount, brainClusters[cntr].Neurons.Count), color: UtilityWPF.ColorToHex(colors[cntr]));
                //}

                //window.Show();

                #endregion

                AdjustNodes_RemoveExcess(brainClusters);

                #region draw 3

                //window = new Debug3DWindow()
                //{
                //    Background = new SolidColorBrush(UtilityWPF.ColorFromHex("555")),
                //};

                //for (int cntr = 0; cntr < brainClusters.Length; cntr++)
                //{
                //    foreach (var neuron in brainClusters[cntr].Neurons)
                //    {
                //        window.AddDot(neuron.Position, DOT, colors[cntr]);
                //    }

                //    window.AddDot(brainClusters[cntr].NodeCenter, DOT / 2, UtilityWPF.AlphaBlend(colors[cntr], Colors.Transparent, .25));

                //    window.AddText(string.Format("desired: {0}, actual: {1}", brainClusters[cntr].DesiredCount, brainClusters[cntr].Neurons.Count), color: UtilityWPF.ColorToHex(colors[cntr]));
                //}

                //window.Show();

                #endregion

                return brainClusters.
                    Select(o => o.Neurons.ToArray()).
                    ToArray();
            }
            private static void AdjustNodes_TransferHighToLow(BrainCluster[] brainClusters)
            {
                while (true)
                {
                    // Find clusters that need more neurons
                    var under = brainClusters.
                        Where(o => o.Neurons.Count < o.DesiredCount).
                        ToArray();

                    if (under.Length == 0)
                    {
                        break;
                    }

                    // Find clusters that have too many
                    var over = brainClusters.
                        Where(o => o.Neurons.Count > o.DesiredCount).
                        ToArray();

                    // Take the closest neuron
                    //NOTE: Since this only looks at under/over clusters, ignoring clusters that have the correct amount, this may miss closer neurons.
                    //But the logic would be more complex and need to keep track of which neurons were traded so they don't get traded back during
                    //the next step.  So even though this approach doesn't give the tightest possible clusters, it's simple and good enough (this whole
                    //decision to cluster won't affect the performance of the neural net, it just makes the final links look cleaner)
                    var best = under.
                        Select(o => new
                        {
                            under = o,
                            candidate = over.
                                Select(p => new
                                {
                                    cluster = p,
                                    closestNeuron = p.Neurons.Select((q, i) => new
                                    {
                                        neuron = q,
                                        index = i,
                                        distanceSqr = (q.Position - o.NodeCenter).LengthSquared,
                                    }).
                                    OrderBy(q => q.distanceSqr).
                                    First(),
                                }).
                                OrderBy(p => p.closestNeuron.distanceSqr).
                                First(),
                        }).
                        OrderBy(o => o.candidate.closestNeuron.distanceSqr).
                        First();

                    // Move the neuron to the new cluster
                    var pickedUnder = best.under;
                    var pickedOver = best.candidate.cluster;
                    var neuronShift = best.candidate.closestNeuron;

                    pickedUnder.Neurons.Add(pickedOver.Neurons[neuronShift.index]);
                    pickedOver.Neurons.RemoveAt(neuronShift.index);

                    pickedUnder.NodeCenter = Math3D.GetCenter(pickedUnder.Neurons.Select(o => o.Position));
                    pickedOver.NodeCenter = Math3D.GetCenter(pickedOver.Neurons.Select(o => o.Position));
                }
            }
            private static void AdjustNodes_RemoveExcess(BrainCluster[] brainClusters)
            {
                foreach (var cluster in brainClusters)
                {
                    while (cluster.Neurons.Count > cluster.DesiredCount)
                    {
                        // Find the neuron that is farthest from the center
                        var farthestNeuron = cluster.Neurons.Select((o, i) => new
                        {
                            neuron = o,
                            index = i,
                            distanceSqr = (o.Position - cluster.NodeCenter).LengthSquared,
                        }).
                        OrderByDescending(q => q.distanceSqr).
                        First();

                        cluster.Neurons.RemoveAt(farthestNeuron.index);
                        cluster.NodeCenter = Math3D.GetCenter(cluster.Neurons.Select(o => o.Position));
                    }
                }
            }

            /// <summary>
            /// This randomly exausts the list, then starts over
            /// </summary>
            private static IEnumerable<T> InfiniteRandomOrder<T>(T[] array)
            {
                while (true)
                {
                    foreach (T item in UtilityCore.RandomOrder(array))
                    {
                        yield return item;
                    }
                }
            }

            #endregion
        }

        #endregion

        //TODO: Don't do the container optimization, because source may have been passed in, but if it's a readwrite neuron, it could be a destination for something else

        //NOTE: These classes contain lists because they will be destroyed as concrete links are chosen
        #region class: ContainerReads

        private class ContainerReads
        {
            /// <summary>
            /// This is the container that holds the readable neurons
            /// </summary>
            public NeuralUtility.ContainerInput Container { get; set; }

            /// <summary>
            /// These are containers that have writable neurons
            /// </summary>
            public List<ContainerWrites> RemainingCandidates { get; set; }

            public void RemoveLink(SourceDestinationLink link)
            {
                int index = 0;

                while (index < RemainingCandidates.Count)
                {
                    //if (link.HasContainerMatch(RemainingCandidates[index].Container))       // doing this token check here to avoid needless iterations of the write container's links.  (this scan would work without this optimization, just slower)
                    {
                        RemainingCandidates[index].RemoveLink(link);

                        if (RemainingCandidates[index].RemainingCandidates.Count == 0)
                        {
                            RemainingCandidates.RemoveAt(index);
                        }
                        else
                        {
                            index++;
                        }
                    }
                    //else
                    //{
                    //    index++;
                    //}
                }
            }

            public override string ToString()
            {
                string sourceType = Container.Container.GetType().ToString();
                int index = sourceType.LastIndexOf('.');
                if (index >= 0)
                {
                    sourceType = sourceType.Substring(index + 1);
                }

                string destinations = RemainingCandidates.
                    Select(o => o.ToString()).
                    ToJoin(", ");

                return $"{sourceType} | {destinations}";
            }
        }

        #endregion
        #region class: ContainerWrites

        private class ContainerWrites
        {
            /// <summary>
            /// This is the container that holds the writable neurons
            /// </summary>
            public NeuralUtility.ContainerInput Container { get; set; }

            /// <summary>
            /// This is a copy of SourceDestinationLink.DistanceContainers
            /// </summary>
            public double Distance { get; set; }

            /// <summary>
            /// When a brain is writing to manipulators and other brains, it needs to dedicate more neurons to manipulators than other brains.
            /// So brain-brain would have a weight of 1, brain-manipulator would be 3 (or whatever ratio makes sense)
            /// Sensor-brain would just be 1
            /// </summary>
            public int Weight { get; set; }

            /// <summary>
            /// These are readable neurons from ContainerReads and writable neurons from ContainerWrites
            /// </summary>
            public List<SourceDestinationLink> RemainingCandidates { get; set; }

            /// <summary>
            /// This removes any links that contain either of the neurons in the link passed in
            /// NOTE: This doesn't search for an exact "and" match, just an "or" match
            /// </summary>
            public void RemoveLink(SourceDestinationLink link)
            {
                int index = 0;

                while (index < RemainingCandidates.Count)
                {
                    if (RemainingCandidates[index].HasNeuronMatch(link))
                    {
                        RemainingCandidates.RemoveAt(index);
                    }
                    else
                    {
                        index++;
                    }
                }
            }

            public override string ToString()
            {
                string type = Container.Container.GetType().ToString();
                int index = type.LastIndexOf('.');
                if (index >= 0)
                {
                    type = type.Substring(index + 1);
                }

                return string.Format("{0} x {1}",
                    type,
                    RemainingCandidates.Count);
            }
        }

        #endregion
        #region class: SourceDestinationLink

        private class SourceDestinationLink
        {
            public INeuron SourceNeuron { get; set; }
            public INeuron DestinationNeuron { get; set; }

            public NeuralUtility.ContainerInput SourceContainer { get; set; }
            public NeuralUtility.ContainerInput DestinationContainer { get; set; }

            public double DistanceContainers { get; set; }
            public double DistanceNeurons { get; set; }

            /// <summary>
            /// This instance is shared across all links to the particular destination neuron (even across multiple instances
            /// of ContainerWrites)
            /// </summary>
            public DestinationNeuronCount DestinationNeuronNumConnections { get; set; }

            public bool HasContainerMatch(NeuralUtility.ContainerInput container)
            {
                return SourceContainer.Token == container.Token || DestinationContainer.Token == container.Token;
            }
            /// <summary>
            /// This is true if the link passed in causes a source to be unavailable
            /// </summary>
            public bool HasNeuronMatch(SourceDestinationLink link)
            {
                // Need to do four comparisons (from-from, from-to, to-from, to-to)

                // from-from
                if (SourceContainer.Token == link.SourceContainer.Token && SourceNeuron.Token == link.SourceNeuron.Token)
                {
                    return true;
                }

                // from-to
                else if (SourceContainer.Token == link.DestinationContainer.Token && SourceNeuron.Token == link.DestinationNeuron.Token)
                {
                    return true;
                }

                // Don't want to return true for these because the to is a valid destination for other sources
                // to-from
                //else if (DestinationContainer.Token == link.SourceContainer.Token && DestinationNeuron.Token == link.SourceNeuron.Token)
                //{
                //    return true;
                //}

                // to-to
                //else if (DestinationContainer.Token == link.DestinationContainer.Token && DestinationNeuron.Token == link.DestinationNeuronToken)
                //{
                //    return true;
                //}

                else
                {
                    return false;
                }
            }

            public double GetLinkWeight(double maxWeight)
            {
                if (SourceContainer.ContainerType == NeuronContainerType.Brain_Standalone || DestinationContainer.ContainerType == NeuronContainerType.Brain_Standalone)
                {
                    // The standalone brain has no internal mechanism to adust weights, so the weights should be randomized at
                    // creation, and then slight mutations over many generations will select for the winning values.  That means this
                    // method is assumed to only be called for newly created links
                    return Math1D.GetNearZeroValue(maxWeight);
                }
                else
                {
                    // Since this is going to a custom neural net, there is no reason to randomize the weights.  Best case, it doesn't affect
                    // anything.  Worse case, it causes problems
                    return 1d;
                }
            }

            public override string ToString()
            {
                string sourceType = SourceContainer.Container.GetType().ToString();
                int index = sourceType.LastIndexOf('.');
                if (index >= 0)
                {
                    sourceType = sourceType.Substring(index + 1);
                }

                string destType = DestinationContainer.Container.GetType().ToString();
                index = destType.LastIndexOf('.');
                if (index >= 0)
                {
                    destType = destType.Substring(index + 1);
                }

                return string.Format("{0} {1} - {2} {3} | {4} - {5} | {6}",
                    sourceType,
                    SourceContainer.Token,
                    destType,
                    DestinationContainer.Token,
                    //SourceNeuron.Position.ToStringSignificantDigits(2),
                    //DestinationNeuron.Position.ToStringSignificantDigits(2),
                    SourceNeuron.Token,
                    DestinationNeuron.Token,
                    DistanceNeurons.ToStringSignificantDigits(2));
            }
        }

        #endregion
        #region class: DestinationNeuronCount

        private class DestinationNeuronCount
        {
            public long NeuronToken { get; set; }
            public int Count { get; set; }
        }

        #endregion

        #region Declaration section

        private ItemColors _colors = new ItemColors();
        private EditorOptions _editorOptions = new EditorOptions();
        private ItemOptions _itemOptions = new ItemOptions();

        private Point3D _boundryMin;
        private Point3D _boundryMax;
        //private ScreenSpaceLines3D _boundryLines = null;

        private World _world = null;
        //private Map _map = null;

        private RadiationField _radiation = null;
        private GravityFieldUniform _gravityField = null;

        private MaterialManager _materialManager = null;
        private int _material_Ship = -1;

        private List<ModelVisual3D> _gravityOrientationVisuals = new List<ModelVisual3D>();
        private TrackballGrabber _gravityOrientationTrackball = null;
        private Vector3D _gravity = new Vector3D(0, 0, 1);

        // These listen to the mouse/keyboard and controls the camera
        private TrackBallRoam _trackball = null;
        private TrackBallRoam _trackballNeural = null;

        private GravSensorStuff[] _gravSensors = null;
        private BrainStuff[] _brains = null;
        private ThrusterStuff[] _thrusters = null;
        private ImpulseStuff[] _impulse = null;
        private ContainerStuff _containers = null;
        private LinkStuff _links = null;
        private List<Visual3D> _debugVisuals = new List<Visual3D>();

        /// <summary>
        /// This runs the brain on a separate thread.  It runs independent of newton's game loop
        /// </summary>
        private Task _brainOperationTask = null;
        private CancellationTokenSource _brainOperationCancel = null;

        private DateTime _lastUpdate = DateTime.UtcNow;

        private bool _isInitialized = false;

        #endregion

        #region Constructor

        public BrainTester()
        {
            InitializeComponent();

            txtGravNeuronDensity.Text = _itemOptions.GravitySensor_NeuronDensity.ToString();
            txtBrainNeuronDensity.Text = _itemOptions.Brain_NeuronDensity.ToString();
            txtBrainChemicalDensity.Text = _itemOptions.Brain_ChemicalDensity.ToString();
            txtLinkBrainInternal.Text = _itemOptions.Brain_LinksPerNeuron_Internal.ToString();
            txtLinkBrainExternalFromSensor.Text = _itemOptions.Brain_LinksPerNeuron_External_FromSensor.ToString();
            txtLinkBrainExternalFromBrain.Text = _itemOptions.Brain_LinksPerNeuron_External_FromBrain.ToString();
            txtBrainNeuronMinDistPercent.Text = _itemOptions.Brain_NeuronMinClusterDistPercent.ToString();

            txtLinkThrusterExternalFromSensor.Text = _itemOptions.Thruster_LinksPerNeuron_Sensor.ToString();
            txtLinkThrusterExternalFromBrain.Text = _itemOptions.Thruster_LinksPerNeuron_Brain.ToString();

            _isInitialized = true;
        }

        #endregion

        #region Event Listeners

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                #region Init World

                _boundryMin = new Point3D(-100, -100, -100);
                _boundryMax = new Point3D(100, 100, 100);

                _world = new World();
                _world.Updating += new EventHandler<WorldUpdatingArgs>(World_Updating);

                _world.SetCollisionBoundry(_boundryMin, _boundryMax);

                // Don't bother with the boundry lines.  It looks funny with a partial viewport
                //// Draw the lines
                //_boundryLines = new ScreenSpaceLines3D(true);
                //_boundryLines.Thickness = 1d;
                //_boundryLines.Color = _colors.BoundryLines;
                //_viewport.Children.Add(_boundryLines);

                //foreach (Point3D[] line in innerLines)
                //{
                //    _boundryLines.AddLine(line[0], line[1]);
                //}

                #endregion
                #region Materials

                _materialManager = new MaterialManager(_world);

                // Ship
                var material = new Game.Newt.v2.NewtonDynamics.Material();
                _material_Ship = _materialManager.AddMaterial(material);

                //_materialManager.RegisterCollisionEvent(_material_Terrain, _material_Bean, Collision_BeanTerrain);

                #endregion
                #region Trackball

                // Trackball
                _trackball = new TrackBallRoam(_camera);
                //_trackball.KeyPanScale = 15d;
                _trackball.EventSource = pnlViewport;		//NOTE:  If this control doesn't have a background color set, the trackball won't see events (I think transparent is ok, just not null)
                _trackball.AllowZoomOnMouseWheel = true;
                _trackball.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.MouseComplete));
                //_trackball.GetOrbitRadius += new GetOrbitRadiusHandler(Trackball_GetOrbitRadius);
                _trackball.ShouldHitTestOnOrbit = false;

                // Trackball
                _trackballNeural = new TrackBallRoam(_cameraNeural);
                //_trackballNeural.KeyPanScale = 15d;
                _trackballNeural.EventSource = pnlViewportNeural;		//NOTE:  If this control doesn't have a background color set, the trackball won't see events (I think transparent is ok, just not null)
                _trackballNeural.AllowZoomOnMouseWheel = true;
                _trackballNeural.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.MouseComplete));
                //_trackballNeural.GetOrbitRadius += new GetOrbitRadiusHandler(Trackball_GetOrbitRadius);
                _trackballNeural.ShouldHitTestOnOrbit = false;

                #endregion
                #region Map

                //_map = new Map(_viewport, null, _world);
                //_map.SnapshotFequency_Milliseconds = 125;
                //_map.SnapshotMaxItemsPerNode = 10;
                //_map.ShouldBuildSnapshots = false;// true;
                //_map.ShouldShowSnapshotLines = false;
                //_map.ShouldSnapshotCentersDrift = true;

                #endregion
                #region Fields

                _radiation = new RadiationField()
                {
                    AmbientRadiation = 0d,
                };

                _gravityField = new GravityFieldUniform();

                #endregion

                // Trackball Controls
                SetupGravityTrackball();

                UpdateGravity();

                _world.UnPause();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            try
            {
                // Copied from Clear_Click
                ClearDebugVisuals();
                ClearLinks();
                ClearGravSensors();
                ClearBrains();
                ClearThrusters();
                ClearContainers();

                //_map.Dispose();		// this will dispose the physics bodies
                //_map = null;

                _world.Pause();
                _world.Dispose();
                _world = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void World_Updating(object sender, WorldUpdatingArgs e)
        {
            DateTime thisUpdate = DateTime.UtcNow;
            double elapsedTime = (thisUpdate - _lastUpdate).TotalSeconds;

            #region Refill containers

            if (_containers != null)
            {
                _containers.Energy.QuantityCurrent = _containers.Energy.QuantityMax;
                _containers.Fuel.QuantityCurrent = _containers.Fuel.QuantityMax;
            }

            #endregion

            #region Update sensors

            if (_gravSensors != null)
            {
                foreach (GravSensorStuff sensor in _gravSensors)
                {
                    sensor.Sensor.Update_MainThread(elapsedTime);
                    sensor.Sensor.Update_AnyThread(elapsedTime);
                }

                lblSensorMagnitude.Text = Math.Round(_gravSensors[0].Sensor.CurrentMax, 3).ToString();		// just report the first sensor's value
            }
            else
            {
                lblSensorMagnitude.Text = "";
            }

            #endregion
            #region Update brains

            if (_brains != null)
            {
                foreach (BrainStuff brain in _brains)
                {
                    brain.Brain.update.Update_MainThread(elapsedTime);
                    brain.Brain.update.Update_AnyThread(elapsedTime);
                }
            }

            #endregion

            #region Color neurons

            foreach (var neuron in UtilityCore.Iterate(
                _gravSensors == null ? null : _gravSensors.SelectMany(o => o.Neurons),
                _brains == null ? null : _brains.SelectMany(o => o.Neurons),
                _thrusters == null ? null : _thrusters.SelectMany(o => o.Neurons)))
            {
                if (neuron.Item1.IsPositiveOnly)
                {
                    neuron.Item2.Color = UtilityWPF.AlphaBlend(_colors.Neuron_One_ZerPos, _colors.Neuron_Zero_ZerPos, neuron.Item1.Value);
                }
                else
                {
                    double weight = neuron.Item1.Value;		// need to grab the value locally, because it could be modified from a different thread

                    if (weight < 0)
                    {
                        neuron.Item2.Color = UtilityWPF.AlphaBlend(_colors.Neuron_NegOne_NegPos, _colors.Neuron_Zero_NegPos, Math.Abs(weight));
                    }
                    else
                    {
                        neuron.Item2.Color = UtilityWPF.AlphaBlend(_colors.Neuron_One_NegPos, _colors.Neuron_Zero_NegPos, weight);
                    }
                }
            }

            #endregion

            _lastUpdate = thisUpdate;
        }

        private void txtGravNeuronDensity_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                double? newValue = UpdateSetting(_isInitialized, txtGravNeuronDensity);
                if (newValue != null)
                {
                    _itemOptions.GravitySensor_NeuronDensity = newValue.Value;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void GravityOrientationTrackball_RotationChanged(object sender, EventArgs e)
        {
            //TODO: Draw gravity in the neural and world viewports
            UpdateGravity();
        }
        private void trkGravityStrength_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateGravity();
        }

        private void Body_RequestWorldLocation(object sender, PartRequestWorldLocationArgs e)
        {
            try
            {
                if (sender is PartBase senderCast)
                {
                    // These parts aren't part of a ship, so model coords is world coords
                    e.Orientation = senderCast.Orientation;
                    e.Position = senderCast.Position;
                }
                else
                {
                    throw new ApplicationException("Expected sender to be PartBase");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void txtBrainNeuronDensity_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                double? newValue = UpdateSetting(_isInitialized, txtBrainNeuronDensity);
                if (newValue != null)
                {
                    _itemOptions.Brain_NeuronDensity = newValue.Value;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void txtBrainChemicalDensity_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                double? newValue = UpdateSetting(_isInitialized, txtBrainChemicalDensity);
                if (newValue != null)
                {
                    _itemOptions.Brain_ChemicalDensity = newValue.Value;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void txtBrainNeuronMinDistPercent_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                double? newValue = UpdateSetting(_isInitialized, txtBrainNeuronMinDistPercent);
                if (newValue != null)
                {
                    _itemOptions.Brain_NeuronMinClusterDistPercent = newValue.Value;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void txtLinkBrainInternal_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                double? newValue = UpdateSetting(_isInitialized, txtLinkBrainInternal);
                if (newValue != null)
                {
                    _itemOptions.Brain_LinksPerNeuron_Internal = newValue.Value;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void txtLinkBrainExternalFromSensor_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                double? newValue = UpdateSetting(_isInitialized, txtLinkBrainExternalFromSensor);
                if (newValue != null)
                {
                    _itemOptions.Brain_LinksPerNeuron_External_FromSensor = newValue.Value;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void txtLinkBrainExternalFromBrain_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                double? newValue = UpdateSetting(_isInitialized, txtLinkBrainExternalFromBrain);
                if (newValue != null)
                {
                    _itemOptions.Brain_LinksPerNeuron_External_FromBrain = newValue.Value;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void txtLinkThrusterExternalFromSensor_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                double? newValue = UpdateSetting(_isInitialized, txtLinkThrusterExternalFromSensor);
                if (newValue != null)
                {
                    _itemOptions.Thruster_LinksPerNeuron_Sensor = newValue.Value;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void txtLinkThrusterExternalFromBrain_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                double? newValue = UpdateSetting(_isInitialized, txtLinkThrusterExternalFromBrain);
                if (newValue != null)
                {
                    _itemOptions.Thruster_LinksPerNeuron_Brain = newValue.Value;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void chkBrainRunning_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_isInitialized)
                {
                    return;
                }

                CancelBrainOperation();		// just to be safe

                if (chkBrainRunning.IsChecked.Value)
                {
                    chkBrainRunning.Content = "Running";
                    StartBrainOperation();
                }
                else
                {
                    chkBrainRunning.Content = "Paused";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void AdvanceBrainOne_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_links == null)
                {
                    MessageBox.Show("There are no neural links", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                NeuralBucket worker = new NeuralBucket(_links.Outputs.SelectMany(o => UtilityCore.Iterate(o.InternalLinks, o.ExternalLinks)).ToArray());

                worker.Tick();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GravitySensor_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!double.TryParse(txtGravitySensorSize.Text, out double size))
                {
                    MessageBox.Show("Couldn't parse sensor size", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(txtGravitySensorCount.Text, out int numSensors))
                {
                    MessageBox.Show("Couldn't parse number of sensors", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (numSensors < 1)
                {
                    MessageBox.Show("The number of sensors must be greater than zero", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Wipe Existing
                ClearGravSensors();

                if (_containers == null)
                {
                    CreateContainers();
                }

                // Build DNA
                ShipPartDNA dna = new ShipPartDNA
                {
                    PartType = SensorGravity.PARTTYPE,
                    Position = new Point3D(-1.5, 0, 0),
                    Orientation = Quaternion.Identity,
                    Scale = new Vector3D(size, size, size)
                };

                ShipPartDNA[] gravDNA = new ShipPartDNA[numSensors];

                for (int cntr = 0; cntr < numSensors; cntr++)
                {
                    if (numSensors == 1)
                    {
                        gravDNA[cntr] = dna;
                    }
                    else
                    {
                        ShipPartDNA dnaCopy = UtilityCore.Clone(dna);
                        double angle = 360d / Convert.ToDouble(numSensors);
                        dnaCopy.Position += new Vector3D(0, .75, 0).GetRotatedVector(new Vector3D(1, 0, 0), angle * cntr);

                        gravDNA[cntr] = dnaCopy;
                    }
                }

                // Build/show sensors
                CreateGravSensors(gravDNA);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Brain_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!double.TryParse(txtBrainSize.Text, out double size))
                {
                    MessageBox.Show("Couldn't parse brain size", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(txtBrainCount.Text, out int numBrains))
                {
                    MessageBox.Show("Couldn't parse number of brains", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (numBrains < 1)
                {
                    MessageBox.Show("The number of brains must be greater than zero", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Wipe Existing
                ClearBrains();

                if (_containers == null)
                {
                    CreateContainers();
                }

                ShipPartDNA[] brainDNA = new ShipPartDNA[numBrains];
                for (int cntr = 0; cntr < numBrains; cntr++)
                {
                    // Decide which type of brain to make
                    bool isStandardBrain;

                    if (chkStandardBrain.IsChecked.Value && chkNEATBrain.IsChecked.Value)
                    {
                        isStandardBrain = StaticRandom.NextBool();
                    }
                    else if (chkStandardBrain.IsChecked.Value)
                    {
                        isStandardBrain = true;
                    }
                    else if (chkNEATBrain.IsChecked.Value)
                    {
                        isStandardBrain = false;
                    }
                    else
                    {
                        MessageBox.Show("Need to check at least one type of brain", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Position
                    Point3D position = new Point3D(0, 0, 0);
                    if (numBrains > 1)
                    {
                        double angle = 360d / Convert.ToDouble(numBrains);
                        position += new Vector3D(0, 1, 0).GetRotatedVector(new Vector3D(1, 0, 0), angle * cntr);
                    }

                    if (isStandardBrain)
                    {
                        brainDNA[cntr] = new ShipPartDNA
                        {
                            PartType = Brain.PARTTYPE,
                            Position = position,
                            Orientation = Quaternion.Identity,
                            Scale = new Vector3D(size, size, size),
                        };
                    }
                    else
                    {
                        brainDNA[cntr] = new BrainNEATDNA
                        {
                            PartType = BrainNEAT.PARTTYPE,
                            Position = position,
                            Orientation = Quaternion.Identity,
                            Scale = new Vector3D(size, size, size),
                        };
                    }
                }

                // Build/Show brains
                CreateBrains(brainDNA);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Thrusters_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!int.TryParse(txtThrusterCount.Text, out int numThrusters))
                {
                    MessageBox.Show("Couldn't parse number of thrusters", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (numThrusters < 1)
                {
                    MessageBox.Show("The number of thrusters must be greater than zero", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Wipe Existing
                ClearThrusters();

                if (_containers == null)
                {
                    CreateContainers();
                }

                // Build DNA
                ThrusterDNA dna = new ThrusterDNA
                {
                    PartType = Thruster.PARTTYPE,
                    Position = new Point3D(2, 0, 0),
                    Orientation = new Quaternion(new Vector3D(0, 1, 0), -90),
                    Scale = new Vector3D(.5, .5, .5),
                    ThrusterType = ThrusterType.One,
                    //ThrusterType = ThrusterType.Custom;
                    ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.One)
                };

                ThrusterDNA[] thrustDNA = new ThrusterDNA[numThrusters];

                for (int cntr = 0; cntr < numThrusters; cntr++)
                {
                    if (numThrusters == 1)
                    {
                        thrustDNA[cntr] = dna;
                    }
                    else
                    {
                        ThrusterDNA dnaCopy = UtilityCore.Clone(dna);
                        double angle = 360d / Convert.ToDouble(numThrusters);
                        dnaCopy.Position += new Vector3D(0, .75, 0).GetRotatedVector(new Vector3D(1, 0, 0), angle * cntr);

                        thrustDNA[cntr] = dnaCopy;
                    }
                }

                // Create/Show fuel,thrusters
                CreateThrusters(thrustDNA);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Impulse_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!double.TryParse(txtImpulseSize.Text, out double size))
                {
                    MessageBox.Show("Couldn't parse size of impulse engine", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(txtImpulseCount.Text, out int count))
                {
                    MessageBox.Show("Couldn't parse number of impulse engines", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (count < 1)
                {
                    MessageBox.Show("The number of impulse engines must be greater than zero", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Wipe Existing
                ClearImpulse();

                if (_containers == null)
                {
                    CreateContainers();
                }

                // Build DNA
                ImpulseEngineDNA dna = new ImpulseEngineDNA
                {
                    PartType = ImpulseEngine.PARTTYPE,
                    Position = new Point3D(3.5, 0, 0),
                    Orientation = chkImpulseRandomOrientation.IsChecked.Value ?
                        Math3D.GetRandomRotation() :
                        Quaternion.Identity,
                    Scale = new Vector3D(size, size, size),
                    ImpulseEngineType = ImpulseEngineType.Both,
                };

                ImpulseEngineDNA[] impulseDNA = new ImpulseEngineDNA[count];

                for (int cntr = 0; cntr < count; cntr++)
                {
                    if (count == 1)
                    {
                        impulseDNA[cntr] = dna;
                    }
                    else
                    {
                        ImpulseEngineDNA dnaCopy = UtilityCore.Clone(dna);
                        double angle = 360d / Convert.ToDouble(count);
                        dnaCopy.Position += new Vector3D(0, .75, 0).GetRotatedVector(new Vector3D(1, 0, 0), angle * cntr);

                        if (chkImpulseRandomOrientation.IsChecked.Value)
                        {
                            dnaCopy.Orientation = Math3D.GetRandomRotation();
                        }

                        impulseDNA[cntr] = dnaCopy;
                    }
                }

                // Create/Show impulse engines
                CreateImpulse(impulseDNA);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Links1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Wipe Existing
                ClearLinks();

                // Wipe out all the dna links so that it creates random links
                if (_brains != null)
                {
                    foreach (BrainStuff brain in _brains)
                    {
                        brain.DNAInternalLinks = null;
                        brain.DNAExternalLinks = null;
                    }
                }

                if (_thrusters != null)
                {
                    foreach (var thrust in _thrusters)
                    {
                        thrust.DNAExternalLinks = null;
                    }
                }

                if (_impulse != null)
                {
                    foreach (var impulse in _impulse)
                    {
                        impulse.DNAExternalLinks = null;
                    }
                }

                // Build new
                CreateLinks();

                #region OLD
                //#region Build up input args

                //List<NeuralUtility.ContainerInput> inputs = new List<NeuralUtility.ContainerInput>();
                //if (_gravSensors != null)
                //{
                //    foreach (GravSensorStuff sensor in _gravSensors)
                //    {
                //        // The sensor is a source, so shouldn't have any links.  But it needs to be included in the args so that other
                //        // neuron containers can hook to it
                //        inputs.Add(new NeuralUtility.ContainerInput(sensor.Sensor, NeuralUtility.NeuronContainerType.Sensor, sensor.Sensor.Position, sensor.Sensor.Orientation, null, null, 0, null, null));
                //    }
                //}

                //if (_brains != null)
                //{
                //    foreach (BrainStuff brain in _brains)
                //    {
                //        //TODO: Consider existing links
                //        inputs.Add(new NeuralUtility.ContainerInput(
                //            brain.Brain, NeuralUtility.NeuronContainerType.Brain,
                //            brain.Brain.Position, brain.Brain.Orientation,
                //            _itemOptions.BrainLinksPerNeuron_Internal,
                //            new Tuple<NeuralUtility.NeuronContainerType, NeuralUtility.ExternalLinkRatioCalcType, double>[]
                //            {
                //                new Tuple<NeuralUtility.NeuronContainerType,NeuralUtility.ExternalLinkRatioCalcType,double>(NeuralUtility.NeuronContainerType.Sensor, NeuralUtility.ExternalLinkRatioCalcType.Smallest, _itemOptions.BrainLinksPerNeuron_External_FromSensor),
                //                new Tuple<NeuralUtility.NeuronContainerType,NeuralUtility.ExternalLinkRatioCalcType,double>(NeuralUtility.NeuronContainerType.Brain, NeuralUtility.ExternalLinkRatioCalcType.Average, _itemOptions.BrainLinksPerNeuron_External_FromBrain),
                //                new Tuple<NeuralUtility.NeuronContainerType,NeuralUtility.ExternalLinkRatioCalcType,double>(NeuralUtility.NeuronContainerType.Manipulator, NeuralUtility.ExternalLinkRatioCalcType.Smallest, _itemOptions.BrainLinksPerNeuron_External_FromManipulator)
                //            },
                //            Convert.ToInt32(Math.Round(brain.Brain.BrainChemicalCount * 1.33d, 0)),		// increasing so that there is a higher chance of listeners
                //            null, null));
                //    }
                //}

                //if (_thrusters != null)
                //{
                //    foreach (var thrust in _thrusters)
                //    {
                //        //TODO: Consider existing links
                //        //NOTE: This won't be fed by other manipulators
                //        inputs.Add(new NeuralUtility.ContainerInput(
                //            thrust.Thrust, NeuralUtility.NeuronContainerType.Manipulator,
                //            thrust.Thrust.Position, thrust.Thrust.Orientation,
                //            null,
                //            new Tuple<NeuralUtility.NeuronContainerType, NeuralUtility.ExternalLinkRatioCalcType, double>[]
                //            {
                //                new Tuple<NeuralUtility.NeuronContainerType,NeuralUtility.ExternalLinkRatioCalcType,double>(NeuralUtility.NeuronContainerType.Sensor, NeuralUtility.ExternalLinkRatioCalcType.Destination, _itemOptions.ThrusterLinksPerNeuron_Sensor),
                //                new Tuple<NeuralUtility.NeuronContainerType,NeuralUtility.ExternalLinkRatioCalcType,double>(NeuralUtility.NeuronContainerType.Brain, NeuralUtility.ExternalLinkRatioCalcType.Destination, _itemOptions.ThrusterLinksPerNeuron_Brain),
                //            },
                //            0,
                //            null, null));
                //    }
                //}

                //#endregion

                //// Create links
                //NeuralUtility.ContainerOutput[] outputs = null;
                //if (inputs.Count > 0)
                //{
                //    outputs = NeuralUtility.LinkNeurons(inputs.ToArray(), _itemOptions.NeuralLinkMaxWeight);
                //}




                //#region Show new links

                //if (outputs != null)
                //{
                //    _links = new LinkStuff();
                //    _links.Outputs = outputs;
                //    _links.Visuals = new List<Visual3D>();

                //    Model3DGroup posLines = null, negLines = null;
                //    DiffuseMaterial posDiffuse = null, negDiffuse = null;

                //    Dictionary<INeuronContainer, Transform3D> containerTransforms = new Dictionary<INeuronContainer, Transform3D>();

                //    foreach (var output in outputs)
                //    {
                //        Transform3D toTransform = GetContainerTransform(containerTransforms, output.Container);

                //        foreach (var link in UtilityHelper.Iterate(output.InternalLinks, output.ExternalLinks))
                //        {
                //            Transform3D fromTransform = GetContainerTransform(containerTransforms, link.FromContainer);

                //            BuildLinkVisual(ref posLines, ref posDiffuse, ref negLines, ref negDiffuse, fromTransform.Transform(link.From.Position), toTransform.Transform(link.To.Position), link.Weight, link.BrainChemicalModifiers, _colors);
                //        }
                //    }

                //    if (posLines != null)
                //    {
                //        ModelVisual3D model = new ModelVisual3D();
                //        model.Content = posLines;
                //        _links.Visuals.Add(model);
                //        _viewportNeural.Children.Add(model);
                //    }

                //    if (negLines != null)
                //    {
                //        ModelVisual3D model = new ModelVisual3D();
                //        model.Content = negLines;
                //        _links.Visuals.Add(model);
                //        _viewportNeural.Children.Add(model);
                //    }
                //}

                //#endregion

                //UpdateCountReport();

                //if (chkBrainRunning.IsChecked.Value)
                //{
                //    StartBrainOperation();
                //}
                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Links2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Wipe Existing
                ClearLinks();

                // Wipe out all the dna links so that it creates random links
                if (_brains != null)
                {
                    foreach (BrainStuff brain in _brains)
                    {
                        brain.DNAInternalLinks = null;
                        brain.DNAExternalLinks = null;
                    }
                }

                if (_thrusters != null)
                {
                    foreach (var thrust in _thrusters)
                    {
                        thrust.DNAExternalLinks = null;
                    }
                }

                if (_impulse != null)
                {
                    foreach (var impulse in _impulse)
                    {
                        impulse.DNAExternalLinks = null;
                    }
                }

                // Build new
                CreateLinks2();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Links3_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Wipe Existing
                ClearLinks();

                // Wipe out all the dna links so that it creates random links
                if (_brains != null)
                {
                    foreach (BrainStuff brain in _brains)
                    {
                        brain.DNAInternalLinks = null;
                        brain.DNAExternalLinks = null;
                    }
                }

                if (_thrusters != null)
                {
                    foreach (var thrust in _thrusters)
                    {
                        thrust.DNAExternalLinks = null;
                    }
                }

                if (_impulse != null)
                {
                    foreach (var impulse in _impulse)
                    {
                        impulse.DNAExternalLinks = null;
                    }
                }

                // Build new
                CreateLinks3();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Links4_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Wipe Existing
                ClearLinks();

                // Wipe out all the dna links so that it creates random links
                if (_brains != null)
                {
                    foreach (BrainStuff brain in _brains)
                    {
                        brain.DNAInternalLinks = null;
                        brain.DNAExternalLinks = null;
                    }
                }

                if (_thrusters != null)
                {
                    foreach (var thrust in _thrusters)
                    {
                        thrust.DNAExternalLinks = null;
                    }
                }

                if (_impulse != null)
                {
                    foreach (var impulse in _impulse)
                    {
                        impulse.DNAExternalLinks = null;
                    }
                }

                // Build new
                CreateLinks4();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Mutate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                #region Extract dna

                ShipPartDNA[] gravDNA1 = null;
                if (_gravSensors != null)
                {
                    gravDNA1 = _gravSensors.Select(o => o.Sensor.GetNewDNA()).ToArray();		// no need to call NeuralUtility.PopulateDNALinks, only the neurons are stored
                }

                ShipPartDNA[] brainDNA1 = null;
                if (_brains != null)
                {
                    brainDNA1 = _brains.Select(o =>
                    {
                        ShipPartDNA dna = o.Brain.part.GetNewDNA();
                        if (_links != null)
                        {
                            NeuralUtility.PopulateDNALinks(dna, o.Brain.container, _links.Outputs);
                        }
                        return dna;
                    }).ToArray();
                }

                ThrusterDNA[] thrustDNA1 = null;
                if (_thrusters != null)
                {
                    thrustDNA1 = _thrusters.Select(o =>
                    {
                        ThrusterDNA dna = (ThrusterDNA)o.Thrust.GetNewDNA();
                        if (_links != null)
                        {
                            NeuralUtility.PopulateDNALinks(dna, o.Thrust, _links.Outputs);
                        }
                        return dna;
                    }).ToArray();
                }

                ImpulseEngineDNA[] impulseDNA1 = null;
                if (_impulse != null)
                {
                    impulseDNA1 = _impulse.Select(o =>
                    {
                        ImpulseEngineDNA dna = (ImpulseEngineDNA)o.Impulse.GetNewDNA();
                        if (_links != null)
                        {
                            NeuralUtility.PopulateDNALinks(dna, o.Impulse, _links.Outputs);
                        }
                        return dna;
                    }).ToArray();
                }

                ShipPartDNA energyDNA1 = null;
                ShipPartDNA fuelDNA1 = null;
                if (_containers != null)
                {
                    energyDNA1 = _containers.Energy.GetNewDNA();
                    fuelDNA1 = _containers.Fuel.GetNewDNA();
                }

                // Combine the lists
                List<ShipPartDNA> allParts1 = UtilityCore.Iterate<ShipPartDNA>(gravDNA1, brainDNA1, thrustDNA1, impulseDNA1).ToList();
                if (allParts1.Count == 0)
                {
                    // There is nothing to do
                    return;
                }

                if (energyDNA1 != null)
                {
                    allParts1.Add(energyDNA1);
                }

                if (fuelDNA1 != null)
                {
                    allParts1.Add(fuelDNA1);
                }

                #endregion

                bool hadLinks = _links != null;

                // Clear existing
                ClearLinks();
                ClearGravSensors();
                ClearBrains();
                ClearThrusters();
                ClearImpulse();

                #region Fill in mutate args

                //TODO: Store these args in ShipDNA itself.  That way, mutation rates are a property of ship
                //TODO: Fill in the params from ItemOptions
                //NOTE: These mutate factors are pretty large.  Good for a unit tester, but not a real simulation

                var mutate_Vector3D = Tuple.Create(PropsByPercent.DataType.Vector3D, new MutateUtility.MuateFactorArgs(MutateUtility.FactorType.Percent, .08d));
                var mutate_Point3D = Tuple.Create(PropsByPercent.DataType.Point3D, new MutateUtility.MuateFactorArgs(MutateUtility.FactorType.Distance, .04d));		// positions need to drift around freely.  percent doesn't make much sense
                var mutate_Quaternion = Tuple.Create(PropsByPercent.DataType.Quaternion, new MutateUtility.MuateFactorArgs(MutateUtility.FactorType.Percent, .08d));

                MutateUtility.ShipPartAddRemoveArgs addRemoveArgs = null;
                if (chkMutateAddRemoveParts.IsChecked.Value)
                {
                    //addRemoveArgs = new MutateUtility.ShipPartAddRemoveArgs();
                }

                MutateUtility.MuateArgs partChangeArgs = null;
                if (chkMutateChangeParts.IsChecked.Value)
                {
                    //NOTE: The mutate class has special logic for Scale and ThrusterDirections
                    partChangeArgs = new MutateUtility.MuateArgs(true, 1d,
                        null,
                        new Tuple<PropsByPercent.DataType, MutateUtility.MuateFactorArgs>[]
                        {
                            mutate_Vector3D,
                            mutate_Point3D,
                            mutate_Quaternion,
                        },
                        new MutateUtility.MuateFactorArgs(MutateUtility.FactorType.Percent, .1d));
                }

                MutateUtility.NeuronMutateArgs neuralArgs = null;
                if (chkMutateChangeNeural.IsChecked.Value)
                {
                    MutateUtility.MuateArgs neuronMovement = new MutateUtility.MuateArgs(false, .05d, null, null, mutate_Point3D.Item2);		// neurons are all point3D

                    MutateUtility.MuateArgs linkMovement = new MutateUtility.MuateArgs(false, .05d,
                        null,
                        new Tuple<PropsByPercent.DataType, MutateUtility.MuateFactorArgs>[]
                        {
                            Tuple.Create(PropsByPercent.DataType.Double, new MutateUtility.MuateFactorArgs(MutateUtility.FactorType.Distance, .1d)),		// all the doubles are weights, which need to be able to cross over zero (percents can't go + to -)
							Tuple.Create(PropsByPercent.DataType.Point3D, new MutateUtility.MuateFactorArgs(MutateUtility.FactorType.Distance, .2d)),		// using a larger value for the links
							mutate_Quaternion,
                        },
                        null);

                    neuralArgs = new MutateUtility.NeuronMutateArgs(neuronMovement, null, linkMovement, null);
                }

                MutateUtility.ShipMutateArgs shipArgs = new MutateUtility.ShipMutateArgs(addRemoveArgs, partChangeArgs, neuralArgs);

                #endregion

                // Mutate
                ShipDNA oldDNA = ShipDNA.Create(allParts1);
                ShipDNA newDNA = MutateUtility.Mutate(oldDNA, shipArgs);

                if (chkMutateWriteToFile.IsChecked.Value)
                {
                    #region Write to file

                    string foldername = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    string timestamp = DateTime.Now.ToString("yyyyMMdd HHmmssfff");

                    string oldFilename = System.IO.Path.Combine(foldername, timestamp + " Old DNA.xml");
                    string newFilename = System.IO.Path.Combine(foldername, timestamp + " New DNA.xml");

                    UtilityCore.SerializeToFile(oldFilename, oldDNA);
                    UtilityCore.SerializeToFile(newFilename, newDNA);

                    #endregion
                }

                #region Rebuild Parts

                ShipPartDNA[] allParts2 = newDNA.PartsByLayer.
                    SelectMany(o => o.Value).
                    ToArray();

                ShipPartDNA[] gravDNA2 = allParts2.
                    Where(o => o.PartType == SensorGravity.PARTTYPE).
                    ToArray();
                if (gravDNA2.Length > 0)
                {
                    CreateGravSensors(gravDNA2);
                }

                ShipPartDNA[] brainDNA2 = allParts2.
                    Where(o => o.PartType.In(Brain.PARTTYPE, BrainNEAT.PARTTYPE)).
                    ToArray();
                if (brainDNA2.Length > 0)
                {
                    CreateBrains(brainDNA2);
                }

                //ShipPartDNA fuelDNA2 = allParts2.Where(o => o.PartType == FuelTank.PARTTYPE).FirstOrDefault();		//NOTE: This is too simplistic if part remove/add is allowed in the mutator
                ThrusterDNA[] thrustDNA2 = allParts2.
                    Where(o => o.PartType == Thruster.PARTTYPE).
                    Select(o => (ThrusterDNA)o).
                    ToArray();
                if (thrustDNA2.Length > 0)
                {
                    CreateThrusters(thrustDNA2);
                }

                ImpulseEngineDNA[] impulseDNA2 = allParts2.
                    Where(o => o.PartType == ImpulseEngine.PARTTYPE).
                    Select(o => (ImpulseEngineDNA)o).
                    ToArray();
                if (impulseDNA2.Length > 0)
                {
                    CreateImpulse(impulseDNA2);
                }

                #endregion

                // Relink
                if (hadLinks)
                {
                    CreateLinks4();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearDebugVisuals();
                ClearLinks();
                ClearGravSensors();
                ClearBrains();
                ClearThrusters();
                ClearImpulse();
                ClearContainers();

                UpdateCountReport();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
        #region Event Listeners - misc

        private void Mutate100ManyTimes_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // This test verifies that the value will drift randomly (an earlier attempt always drifted toward zero until I fixed it)
                // It's also a good demonstration of why mutating 10% each step over many iterations isn't very smart :)

                //TODO: Show a graph.  That would be cool

                double initial = 100d;
                double mutated = initial;
                double min = initial;
                double max = initial;

                for (int cntr = 0; cntr < 10000000; cntr++)
                {
                    mutated = MutateUtility.Mutate_RandPercent(mutated, .0001d);
                    if (mutated < min)
                    {
                        min = mutated;
                    }

                    if (mutated > max)
                    {
                        max = mutated;
                    }
                }

                MessageBox.Show(string.Format("Initial={0}\r\nFinal={1}\r\nMin={2}\r\nMax={3}", initial.ToString(), mutated.ToString(), min.ToString(), max.ToString()), this.Title, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void RandRange_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<int[]> results = new List<int[]>();
                results.Add(UtilityCore.RandomRange(0, 10).ToArray());
                results.Add(UtilityCore.RandomRange(0, 100, 10).ToArray());
                results.Add(UtilityCore.RandomRange(0, 100, 50).ToArray());
                results.Add(UtilityCore.RandomRange(0, 100, -1).ToArray());
                results.Add(UtilityCore.RandomRange(0, 100, 0).ToArray());
                results.Add(UtilityCore.RandomRange(0, 100, 100).ToArray());
                //results.Add(UtilityHelper.RandomRange(0, 100, 101).ToArray());		// this one throws an exception

                // Check for dupes
                foreach (int[] result in results)
                {
                    if (result.GroupBy(o => o).Any(o => o.Count() != 1))
                    {
                        throw new ApplicationException("This one's a problem");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void MutateItemOptions_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // This method is just to unit test the mutate class

                ItemOptions test1 = MutateUtility.MutateSettingsObject(_itemOptions, new MutateUtility.MuateArgs(.1d));
                ItemOptions test2 = MutateUtility.MutateSettingsObject(_itemOptions, new MutateUtility.MuateArgs(true, 3.5d, .1d));
                ItemOptions test3 = MutateUtility.MutateSettingsObject(_itemOptions, new MutateUtility.MuateArgs(false, 10d, .1d));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #region test classes

        private class NeruonContainerShell : INeuronContainer
        {
            public NeruonContainerShell(Point3D position, Quaternion orientation, INeuron[] neurons)
            {
                Position = position;
                Orientation = orientation;
                Radius = 1d;

                Neruons_Readonly = Enumerable.Empty<INeuron>();
                Neruons_Writeonly = Enumerable.Empty<INeuron>();
                Neruons_ReadWrite = neurons;
                Neruons_All = neurons;

                Token = TokenGenerator.NextToken();
            }

            #region INeuronContainer Members

            public IEnumerable<INeuron> Neruons_Readonly { get; private set; }
            public IEnumerable<INeuron> Neruons_ReadWrite { get; private set; }
            public IEnumerable<INeuron> Neruons_Writeonly { get; private set; }

            public IEnumerable<INeuron> Neruons_All { get; private set; }

            public Point3D Position { get; private set; }
            public Quaternion Orientation { get; private set; }
            public double Radius { get; private set; }

            public NeuronContainerType NeuronContainerType => NeuronContainerType.Brain_Standalone;

            public bool IsOn => true;

            public long Token { get; private set; }

            #endregion
        }

        #endregion
        private void FuzzyLinkTest_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int numTo;
                if (!int.TryParse(txtFuzzyLinkTestCount.Text, out numTo))
                {
                    MessageBox.Show("Couldn't parse the count as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Clear_Click(this, new RoutedEventArgs());

                #region Create Neurons

                Point3D fromPoint = new Point3D(-2, 0, 0);
                Point3D toPoint = new Point3D(2, 0, 0);

                List<INeuron> inNeurons = new List<INeuron>();

                // From
                inNeurons.Add(new Neuron_ZeroPos(fromPoint));		// old from

                // To
                inNeurons.Add(new Neuron_NegPos(toPoint));		// old to

                Point3D[] positions = new Point3D[numTo];
                for (int cntr = 0; cntr < numTo; cntr++)
                {
                    positions[cntr] = toPoint + Math3D.GetRandomVector_Spherical(1d);
                    inNeurons.Add(new Neuron_ZeroPos(positions[cntr]));
                }

                // Draw neurons
                List<Tuple<INeuron, SolidColorBrush>> outNeurons;
                ModelVisual3D model;
                BuildNeuronVisuals(out outNeurons, out model, inNeurons, new NeruonContainerShell(new Point3D(0, 0, 0), Quaternion.Identity, null), _colors);

                _debugVisuals.Add(model);
                _viewportNeural.Children.Add(model);

                #endregion
                #region Create Links

                // Get links
                var results = LinkTestUtil.Test1(inNeurons[0].Position, inNeurons[1].Position, positions, 2d);

                // Draw links
                Model3DGroup posLines = null, negLines = null;
                DiffuseMaterial posDiffuse = null, negDiffuse = null;

                BuildLinkVisual(ref posLines, ref posDiffuse, ref negLines, ref negDiffuse, inNeurons[0].Position, inNeurons[1].Position, -.33d, null, _colors);		// original (negative so it's a different color)

                foreach (var result in results)
                {
                    BuildLinkVisual(ref posLines, ref posDiffuse, ref negLines, ref negDiffuse, inNeurons[0].Position, positions[result.To], result.Weight, null, _colors);
                }

                model = new ModelVisual3D();
                model.Content = posLines;
                _debugVisuals.Add(model);
                _viewportNeural.Children.Add(model);

                model = new ModelVisual3D();
                model.Content = negLines;
                _debugVisuals.Add(model);
                _viewportNeural.Children.Add(model);

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void FuzzyLinkTest2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                #region Parse Textboxes

                int numFrom;
                if (!int.TryParse(txtFuzzyLinkTest2FromCount.Text, out numFrom))
                {
                    MessageBox.Show("Couldn't parse the from count as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int numTo;
                if (!int.TryParse(txtFuzzyLinkTest2ToCount.Text, out numTo))
                {
                    MessageBox.Show("Couldn't parse the to count as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int maxIntermediate;
                if (!int.TryParse(txtFuzzyLinkTest2MaxIntermediate.Text, out maxIntermediate))
                {
                    MessageBox.Show("Couldn't parse the max intermediate as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int maxFinal;
                if (!int.TryParse(txtFuzzyLinkTest2MaxFinal.Text, out maxFinal))
                {
                    MessageBox.Show("Couldn't parse the max final as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                #endregion

                Clear_Click(this, new RoutedEventArgs());

                #region Create Neurons

                Point3D fromPoint = new Point3D(-1.1, 0, 0);
                Point3D toPoint = new Point3D(1.1, 0, 0);

                List<INeuron> inNeurons = new List<INeuron>();

                // From
                inNeurons.Add(new Neuron_ZeroPos(fromPoint));		// old from

                Point3D[] fromPositions = new Point3D[numFrom];
                for (int cntr = 0; cntr < numFrom; cntr++)
                {
                    fromPositions[cntr] = fromPoint + Math3D.GetRandomVector_Spherical(1d);
                    inNeurons.Add(new Neuron_NegPos(fromPositions[cntr]));
                }

                // To
                inNeurons.Add(new Neuron_ZeroPos(toPoint));		// old to

                Point3D[] toPositions = new Point3D[numTo];
                for (int cntr = 0; cntr < numTo; cntr++)
                {
                    toPositions[cntr] = toPoint + Math3D.GetRandomVector_Spherical(1d);
                    inNeurons.Add(new Neuron_NegPos(toPositions[cntr]));
                }


                // Draw neurons
                List<Tuple<INeuron, SolidColorBrush>> outNeurons;
                ModelVisual3D model;
                BuildNeuronVisuals(out outNeurons, out model, inNeurons, new NeruonContainerShell(new Point3D(0, 0, 0), Quaternion.Identity, null), _colors);

                _debugVisuals.Add(model);
                _viewportNeural.Children.Add(model);

                #endregion
                #region Create Links

                // Get links
                var fromResults = LinkTestUtil.Test2(fromPoint, fromPositions, maxIntermediate);
                var toResults = LinkTestUtil.Test2(toPoint, toPositions, maxIntermediate);

                var finalResults = LinkTestUtil.Test4(fromResults, toResults, maxFinal);


                // Draw links
                Model3DGroup posLines = null, negLines = null;
                DiffuseMaterial posDiffuse = null, negDiffuse = null;

                // Draw intermediate results
                foreach (var result in fromResults)
                {
                    BuildLinkVisual(ref posLines, ref posDiffuse, ref negLines, ref negDiffuse, fromPositions[result.Index], toPoint, -result.Percent, null, _colors);
                }

                foreach (var result in toResults)
                {
                    BuildLinkVisual(ref posLines, ref posDiffuse, ref negLines, ref negDiffuse, fromPoint, toPositions[result.Index], -result.Percent, null, _colors);
                }

                // Draw final results
                foreach (var result in finalResults)
                {
                    BuildLinkVisual(ref posLines, ref posDiffuse, ref negLines, ref negDiffuse, fromPositions[result.From.Index], toPositions[result.To.Index], result.Percent * 4d, null, _colors);
                }

                model = new ModelVisual3D();
                model.Content = posLines;
                _debugVisuals.Add(model);
                _viewportNeural.Children.Add(model);

                model = new ModelVisual3D();
                model.Content = negLines;
                _debugVisuals.Add(model);
                _viewportNeural.Children.Add(model);


                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void FuzzyLinkTest2New_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                #region Parse Gui

                if (!int.TryParse(txtFuzzyLinkTest2FromCount.Text, out int numFrom))
                {
                    MessageBox.Show("Couldn't parse the from count as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(txtFuzzyLinkTest2ToCount.Text, out int numTo))
                {
                    MessageBox.Show("Couldn't parse the to count as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(txtFuzzyLinkTest2MaxIntermediate.Text, out int maxIntermediate))
                {
                    MessageBox.Show("Couldn't parse the max intermediate as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(txtFuzzyLinkTest2MaxFinal.Text, out int maxFinal))
                {
                    MessageBox.Show("Couldn't parse the max final as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                #endregion

                Clear_Click(this, new RoutedEventArgs());

                #region Create Neurons

                Point3D fromPoint = new Point3D(-1.1, 0, 0);
                Point3D toPoint = new Point3D(1.1, 0, 0);

                List<INeuron> inNeurons = new List<INeuron>();

                // From
                inNeurons.Add(new Neuron_ZeroPos(fromPoint));		// old from

                Point3D[] fromPositions = new Point3D[numFrom];
                for (int cntr = 0; cntr < numFrom; cntr++)
                {
                    fromPositions[cntr] = fromPoint + Math3D.GetRandomVector_Spherical(1d);
                    inNeurons.Add(new Neuron_NegPos(fromPositions[cntr]));
                }

                // To
                inNeurons.Add(new Neuron_ZeroPos(toPoint));		// old to

                Point3D[] toPositions = new Point3D[numTo];
                for (int cntr = 0; cntr < numTo; cntr++)
                {
                    toPositions[cntr] = toPoint + Math3D.GetRandomVector_Spherical(1d);
                    inNeurons.Add(new Neuron_NegPos(toPositions[cntr]));
                }

                Point3D[] allNew = fromPositions.
                    Concat(toPositions).
                    ToArray();

                // Draw neurons
                List<Tuple<INeuron, SolidColorBrush>> outNeurons;
                ModelVisual3D model;
                BuildNeuronVisuals(out outNeurons, out model, inNeurons, new NeruonContainerShell(new Point3D(0, 0, 0), Quaternion.Identity, null), _colors);

                _debugVisuals.Add(model);
                _viewportNeural.Children.Add(model);

                #endregion

                var existingLinks = new[]
                {
                    Tuple.Create(fromPoint, toPoint, 1d),
                };

                var newLinks = ItemLinker.FuzzyLink(existingLinks, allNew, maxFinal, maxIntermediate);

                #region Draw Links

                // Draw links
                Model3DGroup posLines = null, negLines = null;
                DiffuseMaterial posDiffuse = null, negDiffuse = null;

                // Draw initial results
                BuildLinkVisual(ref posLines, ref posDiffuse, ref negLines, ref negDiffuse, fromPoint, toPoint, -1d, null, _colors);

                // Draw final results
                foreach (var result in newLinks)
                {
                    BuildLinkVisual(ref posLines, ref posDiffuse, ref negLines, ref negDiffuse, allNew[result.Item1], allNew[result.Item2], result.Item3 * 4d, null, _colors);
                }

                model = new ModelVisual3D();
                model.Content = posLines;
                _debugVisuals.Add(model);
                _viewportNeural.Children.Add(model);

                model = new ModelVisual3D();
                model.Content = negLines;
                _debugVisuals.Add(model);
                _viewportNeural.Children.Add(model);

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void FuzzyLinkTest3_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                #region parse gui

                if (!int.TryParse(txtFuzzyLinkTest3Points.Text, out int numPoints))
                {
                    MessageBox.Show("Couldn't parse the number of points as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(txtFuzzyLinkTest3Links.Text, out int initialLinkCount))
                {
                    MessageBox.Show("Couldn't parse initial links as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(txtFuzzyLinkTest3MaxIntermediate.Text, out int maxIntermediateLinkCount))
                {
                    MessageBox.Show("Couldn't parse max intermediate links as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(txtFuzzyLinkTest3MaxFinal.Text, out int maxFinalLinkCount))
                {
                    MessageBox.Show("Couldn't parse max final links as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                #endregion

                Clear_Click(this, new RoutedEventArgs());

                #region create a cloud of points

                List<INeuron> initialNeurons = new List<INeuron>();

                Point3D[] initialPositions = new Point3D[numPoints];
                for (int cntr = 0; cntr < numPoints; cntr++)
                {
                    initialPositions[cntr] = Math3D.GetRandomVector_Spherical(2.5d).ToPoint();
                    initialNeurons.Add(new Neuron_ZeroPos(initialPositions[cntr]));
                }

                #endregion

                #region create some initial links

                var initialLinks = UtilityCore.GetRandomPairs(numPoints, initialLinkCount).
                    ToArray();

                #endregion

                #region create a new cloud of points

                List<INeuron> finalNeurons = new List<INeuron>();

                Point3D[] finalPositions = new Point3D[numPoints];
                for (int cntr = 0; cntr < numPoints; cntr++)
                {
                    finalPositions[cntr] = Math3D.GetRandomVector_Spherical(2.5d).ToPoint();
                    finalNeurons.Add(new Neuron_NegPos(finalPositions[cntr]));
                }

                #endregion

                #region relink

                var initialLinkPoints = initialLinks.
                    Select(o => Tuple.Create(initialPositions[o.Item1], initialPositions[o.Item2], 1d)).
                    ToArray();

                var newLinks = ItemLinker.FuzzyLink(initialLinkPoints, finalPositions, maxFinalLinkCount, maxIntermediateLinkCount);

                #endregion

                #region draw neurons

                // Initial (don't want to draw all of them, that's just distracting)
                var initialFiltered = initialLinks.SelectMany(o => new[] { o.Item1, o.Item2 }).
                    Distinct().
                    Select(o => initialNeurons[o]);

                List<Tuple<INeuron, SolidColorBrush>> outNeurons;
                ModelVisual3D model;
                BuildNeuronVisuals(out outNeurons, out model, initialFiltered, new NeruonContainerShell(new Point3D(0, 0, 0), Quaternion.Identity, null), _colors);

                _debugVisuals.Add(model);
                _viewportNeural.Children.Add(model);

                // Final
                BuildNeuronVisuals(out outNeurons, out model, finalNeurons, new NeruonContainerShell(new Point3D(0, 0, 0), Quaternion.Identity, null), _colors);

                _debugVisuals.Add(model);
                _viewportNeural.Children.Add(model);

                #endregion
                #region draw links

                Model3DGroup posLines = null, negLines = null;
                DiffuseMaterial posDiffuse = null, negDiffuse = null;

                // Initial
                foreach (var link in initialLinks)
                {
                    BuildLinkVisual(ref posLines, ref posDiffuse, ref negLines, ref negDiffuse, initialPositions[link.Item1], initialPositions[link.Item2], -1d, null, _colors);
                }

                // Final
                foreach (var result in newLinks)
                {
                    BuildLinkVisual(ref posLines, ref posDiffuse, ref negLines, ref negDiffuse, finalPositions[result.Item1], finalPositions[result.Item2], result.Item3 * 4d, null, _colors);
                }

                model = new ModelVisual3D();
                model.Content = posLines;
                _debugVisuals.Add(model);
                _viewportNeural.Children.Add(model);

                model = new ModelVisual3D();
                model.Content = negLines;
                _debugVisuals.Add(model);
                _viewportNeural.Children.Add(model);

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #region test classes

        private class DNATwin
        {
            public string[] List0
            {
                get;
                set;
            }
            public string[] List1
            {
                get;
                set;
            }
        }

        private class DNAQuad : DNATwin
        {
            public string String0
            {
                get;
                set;
            }
            public string[][] Jagged0
            {
                get;
                set;
            }
        }

        #endregion
        private void PropsByPercent1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //TODO: Make a test button that builds a tree, then parses it into a 1D list

                Random rand = StaticRandom.GetRandomForThread();

                #region Build Lists

                // These were copied from the previous tester buttons

                // List1
                string[] list1 = Enumerable.Repeat("", 3 + rand.Next(100)).ToArray();

                // List2
                List<string[]> list2 = Enumerable.Range(0, 3 + rand.Next(30)).
                    Select(o => Enumerable.Repeat("", rand.Next(20)).ToArray()).ToList();

                // List3
                List<DNATwin> list3 = Enumerable.Range(0, 3 + rand.Next(20)).
                    Select(o => new DNATwin()
                    {
                        List0 = Enumerable.Repeat("", rand.Next(12)).ToArray(),
                        List1 = Enumerable.Repeat("", rand.Next(18)).ToArray()
                    }).ToList();

                // List4
                List<DNATwin> list4 = new List<DNATwin>();
                foreach (var cntr in Enumerable.Range(0, 3 + rand.Next(20)))
                {
                    if (rand.Next(2) == 0)
                    {
                        list4.Add(new DNATwin()
                        {
                            List0 = Enumerable.Repeat("", rand.Next(12)).ToArray(),
                            List1 = Enumerable.Repeat("", rand.Next(18)).ToArray()
                        });
                    }
                    else
                    {
                        list4.Add(new DNAQuad()
                        {
                            List0 = Enumerable.Repeat("", rand.Next(12)).ToArray(),
                            List1 = Enumerable.Repeat("", rand.Next(18)).ToArray(),
                            String0 = "",
                            Jagged0 = Enumerable.Range(0, 3 + rand.Next(5)).
                                Select(o => Enumerable.Repeat("", rand.Next(10)).ToArray()).ToArray()
                        });
                    }
                }

                #endregion

                #region Index the lists

                List<PropsByPercent> props = new List<PropsByPercent>();

                //NOTE: Can't have a naked list of items (strings, doubles).  The PropsByPercent class was written to handle a list of classes that hold
                //properties, not a list of properties directly.  If a string[] were to be passed in, it would be seen as a list of strings that hold char arrays
                //
                //NOTE: Tuple is readonly, so PropsByPercent will index it just fine, but when you try to call PropsByPercent.PropWrapper.SetValue,
                //it throws a readonly exception
                //props.Add(new PropsByPercent(list1.Select(o => new Tuple<string>(o))));
                //props.Add(new PropsByPercent(list2.Select(o => new Tuple<string[]>(o))));

                props.Add(new PropsByPercent(list3));
                props.Add(new PropsByPercent(list4));

                #endregion

                #region Modify random properties

                foreach (PropsByPercent propList in props)
                {
                    List<PropsByPercent.PropWrapper> used = new List<PropsByPercent.PropWrapper>();

                    // Modify 10% of the properties in this list
                    int count = Convert.ToInt32(Math.Ceiling(propList.Count * .1d));

                    for (int cntr = 0; cntr < count; cntr++)
                    {
                        while (true)
                        {
                            double percent = rand.NextDouble();

                            PropsByPercent.PropWrapper prop = propList.GetProperty(percent);
                            if (prop == null)
                            {
                                throw new ApplicationException("Didn't find property at percent: " + percent.ToString());
                            }

                            if (!used.Contains(prop))
                            {
                                prop.SetValue("XXXXXX");		// all the lists in this test method are some kind of string, so no need to check datatype
                                used.Add(prop);
                                break;
                            }
                        }
                    }
                }

                #endregion

                #region Modify all properties

                //foreach (PropsByPercent.PropWrapper prop in props.SelectMany(o => o))		// this will work, but for this tester, I want to more explicitly hit foreach

                foreach (PropsByPercent propList in props)
                {
                    foreach (PropsByPercent.PropWrapper prop in propList)
                    {
                        prop.SetValue("all work and no play makes jack a dull boy");
                    }
                }

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void PropsByPercent2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Random rand = StaticRandom.GetRandomForThread();

                #region Build Lists

                //// List1
                //string[] list1 = Enumerable.Repeat("", 3 + rand.Next(100)).ToArray();

                //// List2
                //List<string[]> list2 = Enumerable.Range(0, 3 + rand.Next(30)).
                //    Select(o => Enumerable.Repeat("", rand.Next(20)).ToArray()).ToList();

                //// List3
                //List<DNATwin> list3 = Enumerable.Range(0, 3 + rand.Next(20)).
                //    Select(o => new DNATwin()
                //    {
                //        List0 = Enumerable.Repeat("", rand.Next(12)).ToArray(),
                //        List1 = Enumerable.Repeat("", rand.Next(18)).ToArray()
                //    }).ToList();

                // List4
                List<DNATwin> list4 = new List<DNATwin>();
                foreach (var cntr in Enumerable.Range(0, 3 + rand.Next(20)))
                {
                    if (rand.Next(2) == 0)
                    {
                        list4.Add(new DNATwin()
                        {
                            List0 = Enumerable.Repeat("", rand.Next(12)).ToArray(),
                            List1 = Enumerable.Repeat("", rand.Next(18)).ToArray()
                        });
                    }
                    else
                    {
                        list4.Add(new DNAQuad()
                        {
                            List0 = Enumerable.Repeat("", rand.Next(12)).ToArray(),
                            List1 = Enumerable.Repeat("", rand.Next(18)).ToArray(),
                            String0 = "",
                            Jagged0 = Enumerable.Range(0, 3 + rand.Next(5)).
                                Select(o => Enumerable.Repeat("", rand.Next(10)).ToArray()).ToArray()
                        });
                    }
                }

                // List5 (empty)
                List<DNATwin> list5 = Enumerable.Range(0, 3 + rand.Next(20)).
                    Select(o => new DNATwin()
                    {
                        List0 = new string[0],
                        List1 = new string[0]
                    }).ToList();

                // List6 (one empty item)
                List<DNATwin> list6 = new List<DNATwin>();
                list6.Add(new DNATwin() { List0 = new string[0], List1 = new string[0] });
                list6.Add(new DNATwin() { List0 = new string[] { "" }, List1 = new string[0] });
                list6.Add(new DNATwin() { List0 = new string[0], List1 = new string[0] });

                #endregion

                #region Valid Filters

                PropsByPercent props1 = new PropsByPercent(list4, new PropsByPercent.FilterArgs() { IgnoreNames = new string[] { "List1", "String0" } });
                PropsByPercent props2 = new PropsByPercent(list4, new PropsByPercent.FilterArgs() { OnlyUseNames = new string[] { "List1", "String0" } });

                PropsByPercent props3 = new PropsByPercent(list4, new PropsByPercent.FilterArgs() { IgnoreTypesRaw = new string[] { PropsByPercent.PROP_STRINGARRAY } });
                PropsByPercent props4 = new PropsByPercent(list4, new PropsByPercent.FilterArgs() { OnlyUseTypesRaw = new string[] { PropsByPercent.PROP_STRINGARRAY } });

                PropsByPercent props5 = new PropsByPercent(list4, new PropsByPercent.FilterArgs() { IgnoreTypes = new PropsByPercent.DataType[] { PropsByPercent.DataType.String } });		// this had divide by zero because no props matched the filter
                PropsByPercent props5a = new PropsByPercent(list5);		// tests divide by 0 when the list is empty
                PropsByPercent props5b = new PropsByPercent(list6);		// tests divide by 0 when the list is empty
                PropsByPercent props6 = new PropsByPercent(list4, new PropsByPercent.FilterArgs() { OnlyUseTypes = new PropsByPercent.DataType[] { PropsByPercent.DataType.String } });

                PropsByPercent props7 = new PropsByPercent(list4, new PropsByPercent.FilterArgs()
                {
                    IgnoreNames = new string[] { "List1", "String0" },
                    OnlyUseTypesRaw = new string[] { PropsByPercent.PROP_STRINGJAGGED }
                });

                double percent = .42d;
                var result1 = props1.GetProperty(percent);
                var result2 = props2.GetProperty(percent);
                var result3 = props3.GetProperty(percent);
                var result4 = props4.GetProperty(percent);
                var result5 = props5.GetProperty(percent);
                var result5a = props5a.GetProperty(percent);
                var result5b = props5b.GetProperty(percent);
                var result6 = props6.GetProperty(percent);
                var result7 = props7.GetProperty(percent);

                foreach (var result in props5)
                {
                    int three = 6;
                }

                foreach (var result in props5a)
                {
                    int three = 2;
                }

                foreach (var result in props5b)
                {
                    int three = 5;
                }

                #endregion

                #region Invalid Filters

                try
                {
                    PropsByPercent props8 = new PropsByPercent(list4, new PropsByPercent.FilterArgs()
                    {
                        IgnoreNames = new string[] { "List1", "String0" },
                        OnlyUseNames = new string[] { "List1", "String0" }
                    });
                }
                catch (Exception) { }

                try
                {
                    PropsByPercent props9 = new PropsByPercent(list4, new PropsByPercent.FilterArgs()
                    {
                        IgnoreTypesRaw = new string[] { "a", "b" },
                        OnlyUseTypesRaw = new string[] { "c", "d" }
                    });
                }
                catch (Exception) { }

                try
                {
                    PropsByPercent props10 = new PropsByPercent(list4, new PropsByPercent.FilterArgs()
                    {
                        IgnoreTypes = new PropsByPercent.DataType[] { PropsByPercent.DataType.Unknown },
                        OnlyUseTypes = new PropsByPercent.DataType[] { PropsByPercent.DataType.String }
                    });
                }
                catch (Exception) { }

                try
                {
                    PropsByPercent props11 = new PropsByPercent(list4, new PropsByPercent.FilterArgs()
                    {
                        OnlyUseTypesRaw = new string[] { "c", "d" },
                        OnlyUseTypes = new PropsByPercent.DataType[] { PropsByPercent.DataType.String }
                    });
                }
                catch (Exception) { }

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Iterators_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var setsForCount = new List<(int count, (int, int)[][] sets)>();

                for (int cntr = 2; cntr <= 6; cntr++)
                {
                    setsForCount.Add(
                    (
                        cntr,
                        TestGetAllSets(cntr)
                    ));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void LineArrows_Click(object sender, RoutedEventArgs e)
        {
            const double DOT = .015;
            const double THICKNESS = .025;

            try
            {
                Debug3DWindow window = new Debug3DWindow();


                window.AddModel(new BillboardLine3D()
                {
                    Color = Colors.DarkGoldenrod,
                    Thickness = THICKNESS,
                    //IsReflectiveColor = true,
                    FromPoint = new Point3D(-5, 0, 0),
                    ToPoint = new Point3D(5, 0, 0),
                }.Model);


                window.AddModel(new BillboardLine3D()
                {
                    Color = Colors.Olive,
                    Thickness = THICKNESS,
                    //IsReflectiveColor = true,
                    FromPoint = new Point3D(-5, .2, 0),
                    ToPoint = new Point3D(5, .2, 0),
                    ToArrowPercent = 1,
                }.Model);

                window.AddModel(new BillboardLine3D()
                {
                    Color = Colors.SlateBlue,
                    Thickness = THICKNESS,
                    //IsReflectiveColor = true,
                    FromPoint = new Point3D(-5, .4, 0),
                    ToPoint = new Point3D(5, .4, 0),
                    FromArrowPercent = 1,
                }.Model);

                window.AddModel(new BillboardLine3D()
                {
                    Color = Colors.Teal,
                    Thickness = THICKNESS,
                    //IsReflectiveColor = true,
                    FromPoint = new Point3D(-5, .6, 0),
                    ToPoint = new Point3D(5, .6, 0),
                    FromArrowPercent = 1,
                    ToArrowPercent = 1,
                }.Model);

                window.AddModel(new BillboardLine3D()
                {
                    Color = Colors.SaddleBrown,
                    Thickness = THICKNESS,
                    //IsReflectiveColor = true,
                    FromPoint = new Point3D(-5, .8, 0),
                    ToPoint = new Point3D(5, .8, 0),
                    FromArrowPercent = .75,
                    ToArrowPercent = .99,
                }.Model);


                //var arrowMesh = GetArrow(new Point3D(0, 0, 0), new Point3D(0, 0, 1), 1);
                //window.AddMesh(arrowMesh, Colors.OliveDrab, false);


                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void NeuronActivation_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Neuron_SensorPosition neuron = new Neuron_SensorPosition(new Point3D(), true, false);

                List<string> report = new List<string>();

                foreach (double input in new[] { -3d, -2d, -1d, 0d, 1d, 2d, 3d, 4d, 5d })
                {
                    neuron.SetValue(input);

                    report.Add($"{input.ToStringSignificantDigits(3)}: {neuron.Value.ToStringSignificantDigits(3)}");
                }

                MessageBox.Show(report.ToJoin("\r\n"), Title, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private static void ModifyDNA(ShipPartDNA dna, bool randSize, bool randOrientation)
        {
            if (randSize)
            {
                dna.Scale = Math3D.GetRandomVector(new Vector3D(.25, .25, .25), new Vector3D(2.5, 2.5, 2.5));
            }

            if (randOrientation)
            {
                dna.Orientation = Math3D.GetRandomRotation();
            }
        }

        internal static void BuildNeuronVisuals(out List<Tuple<INeuron, SolidColorBrush>> outNeurons, out ModelVisual3D model, IEnumerable<INeuron> inNeurons, INeuronContainer container, ItemColors colors)
        {
            outNeurons = new List<Tuple<INeuron, SolidColorBrush>>();

            Model3DGroup geometries = new Model3DGroup();

            int neuronCount = inNeurons.Count();
            double neuronRadius;
            if (neuronCount < 20)
            {
                neuronRadius = .03d;
            }
            else if (neuronCount > 100)
            {
                neuronRadius = .007d;
            }
            else
            {
                neuronRadius = UtilityCore.GetScaledValue(.03d, .007d, 20, 100, neuronCount);
            }

            foreach (INeuron neuron in inNeurons)
            {
                MaterialGroup material = new MaterialGroup();
                SolidColorBrush brush = new SolidColorBrush(neuron.IsPositiveOnly ? colors.Neuron_Zero_ZerPos : colors.Neuron_Zero_NegPos);
                DiffuseMaterial diffuse = new DiffuseMaterial(brush);
                material.Children.Add(diffuse);
                material.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("80C0C0C0")), 50d));

                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = material;
                geometry.BackMaterial = material;
                geometry.Geometry = UtilityWPF.GetSphere_LatLon(2, neuronRadius);
                geometry.Transform = new TranslateTransform3D(neuron.Position.ToVector());

                geometries.Children.Add(geometry);

                outNeurons.Add(new Tuple<INeuron, SolidColorBrush>(neuron, brush));
            }

            model = new ModelVisual3D();
            model.Content = geometries;
            //model.Transform = new TranslateTransform3D(position.ToVector());
            model.Transform = GetContainerTransform(container);
        }
        internal static void BuildLinkVisual(ref Model3DGroup posLines, ref DiffuseMaterial posDiffuse, ref Model3DGroup negLines, ref DiffuseMaterial negDiffuse, Point3D from, Point3D to, double weight, double[] brainChemicals, ItemColors colors)
        {
            const double GAP = .05d;
            const double CHEMSPACE = .01d;

            double thickness = Math.Abs(weight) * .003d;

            #region Shorten Line

            // Leave a little bit of gap between the from node and the line so the user knows what direction the link is
            Vector3D line = from - to;
            double length = line.Length;
            double newLength = length - GAP;
            if (newLength > length * .75d)		// don't shorten it if it's going to be too small
            {
                line = line.ToUnit() * newLength;
            }
            else
            {
                newLength = length;		// doing this here so the logic below doesn't need an if statement
            }

            Point3D fromActual = to + line;

            #endregion

            GeometryModel3D geometry = null;

            #region Draw Line

            if (weight > 0)
            {
                if (posLines == null)
                {
                    posLines = new Model3DGroup();
                    posDiffuse = new DiffuseMaterial(new SolidColorBrush(colors.Link_Positive));
                }

                geometry = new GeometryModel3D();
                geometry.Material = posDiffuse;
                geometry.BackMaterial = posDiffuse;
                geometry.Geometry = UtilityWPF.GetLine(fromActual, to, thickness);

                posLines.Children.Add(geometry);
            }
            else
            {
                if (negLines == null)
                {
                    negLines = new Model3DGroup();
                    negDiffuse = new DiffuseMaterial(new SolidColorBrush(colors.Link_Negative));
                }

                geometry = new GeometryModel3D();
                geometry.Material = negDiffuse;
                geometry.BackMaterial = negDiffuse;
                geometry.Geometry = UtilityWPF.GetLine(fromActual, to, thickness);

                negLines.Children.Add(geometry);
            }

            #endregion

            if (brainChemicals != null)
            {
                #region Draw Brain Chemicals

                #region Calculations

                double workingLength = newLength - GAP;
                if (Math1D.IsNearValue(length, newLength))
                {
                    // Logic above didn't use a gap, so don't do one here either
                    workingLength = length;
                }

                double totalChemSpace = (brainChemicals.Length - 1) * CHEMSPACE;

                double chemSpace = CHEMSPACE;
                if (totalChemSpace > workingLength)
                {
                    chemSpace = workingLength / (brainChemicals.Length - 1);		// shouldn't get divide by zero
                }

                Vector3D chemOffset = line.ToUnit() * (chemSpace * -1d);

                RotateTransform3D rotTrans = new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRotation(new Vector3D(0, 0, 1), line)));

                #endregion

                // Place the chemicals
                for (int cntr = 0; cntr < brainChemicals.Length; cntr++)
                {
                    Transform3DGroup transform = new Transform3DGroup();
                    transform.Children.Add(rotTrans);
                    transform.Children.Add(new TranslateTransform3D((fromActual + (chemOffset * cntr)).ToVector()));

                    double scale = .0062d * Math.Abs(brainChemicals[cntr]);
                    ScaleTransform3D scaleTransform = new ScaleTransform3D(scale, scale, scale);

                    if (brainChemicals[cntr] > 0)
                    {
                        if (posLines == null)
                        {
                            posLines = new Model3DGroup();
                            posDiffuse = new DiffuseMaterial(new SolidColorBrush(colors.Link_Positive));
                        }

                        geometry = new GeometryModel3D();
                        geometry.Material = posDiffuse;
                        geometry.BackMaterial = posDiffuse;
                        geometry.Geometry = UtilityWPF.GetCircle2D(5, scaleTransform, Transform3D.Identity);
                        geometry.Transform = transform;

                        posLines.Children.Add(geometry);
                    }
                    else
                    {
                        if (negLines == null)
                        {
                            negLines = new Model3DGroup();
                            negDiffuse = new DiffuseMaterial(new SolidColorBrush(colors.Link_Negative));
                        }

                        geometry = new GeometryModel3D();
                        geometry.Material = negDiffuse;
                        geometry.BackMaterial = negDiffuse;
                        geometry.Geometry = UtilityWPF.GetCircle2D(5, scaleTransform, Transform3D.Identity);
                        geometry.Transform = transform;

                        negLines.Children.Add(geometry);
                    }
                }

                #endregion
            }
        }

        private static Transform3D GetContainerTransform(INeuronContainer container, Dictionary<INeuronContainer, Transform3D> existing = null)
        {
            if (existing != null && existing.TryGetValue(container, out Transform3D cached))
            {
                return cached;
            }

            Transform3DGroup transform = new Transform3DGroup();
            transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(container.Orientation)));
            transform.Children.Add(new TranslateTransform3D(container.Position.ToVector()));

            if (existing != null)
            {
                existing.Add(container, transform);
            }

            return transform;
        }

        private void ClearGravSensors()
        {
            ClearDebugVisuals();
            ClearLinks();

            if (_gravSensors != null)
            {
                foreach (GravSensorStuff sensor in _gravSensors)
                {
                    _viewport.Children.Remove(sensor.Visual);

                    _viewportNeural.Children.Remove(sensor.NeuronVisual);
                    _viewportNeural.Children.Remove(sensor.Gravity);

                    sensor.Body.Dispose();
                }

                _gravSensors = null;
            }
        }
        private void ClearBrains()
        {
            ClearDebugVisuals();
            ClearLinks();

            if (_brains != null)
            {
                foreach (BrainStuff brain in _brains)
                {
                    _viewport.Children.Remove(brain.Visual);

                    _viewportNeural.Children.Remove(brain.NeuronVisual);

                    brain.Body.Dispose();
                }

                _brains = null;
            }
        }
        private void ClearThrusters()
        {
            ClearDebugVisuals();
            ClearLinks();

            if (_thrusters != null)
            {
                foreach (var thrust in _thrusters)
                {
                    _viewport.Children.Remove(thrust.Visual);
                    thrust.Body.Dispose();

                    _viewportNeural.Children.Remove(thrust.NeuronVisual);
                }

                _thrusters = null;
            }
        }
        private void ClearImpulse()
        {
            ClearDebugVisuals();
            ClearLinks();

            if (_impulse != null)
            {
                foreach (var impulse in _impulse)
                {
                    _viewport.Children.Remove(impulse.Visual);
                    impulse.Body.Dispose();

                    _viewportNeural.Children.Remove(impulse.NeuronVisual);
                }

                _impulse = null;
            }
        }
        private void ClearContainers()
        {
            ClearDebugVisuals();
            ClearLinks();

            if (_containers != null)
            {
                _viewport.Children.Remove(_containers.EnergyVisual);
                _viewport.Children.Remove(_containers.FuelVisual);
                _viewport.Children.Remove(_containers.PlasmaVisual);

                _containers.EnergyBody.Dispose();
                _containers.FuelBody.Dispose();
                _containers.PlasmaBody.Dispose();

                _containers = null;
            }
        }
        private void ClearLinks()
        {
            ClearDebugVisuals();
            CancelBrainOperation();

            if (_links != null)
            {
                foreach (Visual3D visual in _links.Visuals)
                {
                    _viewportNeural.Children.Remove(visual);
                }

                _links = null;
            }

            // Reset the neuron values (leave the sensors alone, because they aren't affected by links)
            foreach (var neuron in UtilityCore.Iterate(
                _brains == null ? null : _brains.SelectMany(o => o.Neurons),
                _thrusters == null ? null : _thrusters.SelectMany(o => o.Neurons)))
            {
                neuron.Item1.SetValue(0d);
            }
        }
        private void ClearDebugVisuals()
        {
            _viewportNeural.Children.RemoveAll(_debugVisuals);
            _debugVisuals.Clear();
        }

        private void CreateGravSensors(ShipPartDNA[] dna)
        {
            if (_gravSensors != null)
            {
                throw new InvalidOperationException("Existing gravity sensors should have been wiped out before calling CreateGravSensors");
            }

            _gravSensors = new GravSensorStuff[dna.Length];
            for (int cntr = 0; cntr < dna.Length; cntr++)
            {
                _gravSensors[cntr] = new GravSensorStuff();
                _gravSensors[cntr].Sensor = new SensorGravity(_editorOptions, _itemOptions, dna[cntr], _containers == null ? null : _containers.Energy, _gravityField);
                _gravSensors[cntr].Sensor.RequestWorldLocation += new EventHandler<PartRequestWorldLocationArgs>(Body_RequestWorldLocation);

                #region ship visual

                // WPF
                ModelVisual3D model = new ModelVisual3D();
                model.Content = _gravSensors[cntr].Sensor.Model;
                //TODO: Offset this if there are multiple parts

                _viewport.Children.Add(model);
                _gravSensors[cntr].Visual = model;

                // Physics
                using (CollisionHull hull = _gravSensors[cntr].Sensor.CreateCollisionHull(_world))
                {
                    _gravSensors[cntr].Body = new Body(hull, Matrix3D.Identity, _gravSensors[cntr].Sensor.TotalMass, new Visual3D[] { model });
                    _gravSensors[cntr].Body.MaterialGroupID = _material_Ship;
                    _gravSensors[cntr].Body.LinearDamping = .01f;
                    _gravSensors[cntr].Body.AngularDamping = new Vector3D(.01f, .01f, .01f);
                }

                #endregion
                #region gravity visual

                //NOTE: Since the neurons are semitransparent, they need to be added last

                _gravSensors[cntr].Gravity = new ScreenSpaceLines3D();
                _gravSensors[cntr].Gravity.Thickness = 2d;
                _gravSensors[cntr].Gravity.Color = _colors.TrackballAxisMajorColor;

                _viewportNeural.Children.Add(_gravSensors[cntr].Gravity);

                #endregion
                #region neuron visuals

                BuildNeuronVisuals(out _gravSensors[cntr].Neurons, out model, _gravSensors[cntr].Sensor.Neruons_All, _gravSensors[cntr].Sensor, _colors);

                _gravSensors[cntr].NeuronVisual = model;
                _viewportNeural.Children.Add(model);

                #endregion
            }

            UpdateGravity();		// this shows the gravity line
            UpdateCountReport();
        }
        private void CreateBrains(ShipPartDNA[] dna)
        {
            if (_brains != null)
            {
                throw new InvalidOperationException("Existing brains should have been wiped out before calling CreateBrains");
            }

            _brains = new BrainStuff[dna.Length];

            for (int cntr = 0; cntr < dna.Length; cntr++)
            {
                _brains[cntr] = new BrainStuff();
                BrainStuff brain = _brains[cntr];

                if (dna[cntr] is BrainNEATDNA neatDNA)
                {
                    brain.BrainNEAT = new BrainNEAT(_editorOptions, _itemOptions, neatDNA, _containers == null ? null : _containers.Energy);
                    //brain.SetPhenome(phenome, genome, _experimentArgs.Activation, _hyperneatArgs);        // having the brain actually running isn't necessary for this tester.  This tester is focused on wiring up the external neurons
                }
                else
                {
                    brain.BrainStandard = new Brain(_editorOptions, _itemOptions, dna[cntr], _containers == null ? null : _containers.Energy);
                }

                brain.DNAInternalLinks = dna[cntr].InternalLinks;
                brain.DNAExternalLinks = dna[cntr].ExternalLinks;

                #region ship visual

                // WPF
                ModelVisual3D model = new ModelVisual3D();
                model.Content = brain.Brain.part.Model;

                _viewport.Children.Add(model);
                brain.Visual = model;

                // Physics
                using (CollisionHull hull = brain.Brain.part.CreateCollisionHull(_world))
                {
                    brain.Body = new Body(hull, Matrix3D.Identity, brain.Brain.part.TotalMass, new Visual3D[] { model });
                    brain.Body.MaterialGroupID = _material_Ship;
                    brain.Body.LinearDamping = .01f;
                    brain.Body.AngularDamping = new Vector3D(.01f, .01f, .01f);
                }

                #endregion
                #region neuron visuals

                BuildNeuronVisuals(out brain.Neurons, out model, brain.Brain.container.Neruons_All, brain.Brain.container, _colors);

                brain.NeuronVisual = model;
                _viewportNeural.Children.Add(model);

                #endregion
            }

            UpdateCountReport();
        }
        private void CreateThrusters(ThrusterDNA[] dna)
        {
            if (_thrusters != null)
            {
                throw new InvalidOperationException("Existing thrusters should have been wiped out before calling CreateThrusters");
            }

            _thrusters = new ThrusterStuff[dna.Length];

            // Thrusters
            for (int cntr = 0; cntr < dna.Length; cntr++)
            {
                _thrusters[cntr] = new ThrusterStuff
                {
                    Thrust = new Thruster(_editorOptions, _itemOptions, dna[cntr], _containers == null ? null : _containers.Fuel),
                    DNAExternalLinks = dna[cntr].ExternalLinks
                };
            }

            #region ship visuals

            foreach (var thrust in _thrusters)
            {
                // WPF
                ModelVisual3D model = new ModelVisual3D();
                model.Content = thrust.Thrust.Model;
                //TODO: Offset this if there are multiple parts

                _viewport.Children.Add(model);
                thrust.Visual = model;

                // Physics
                using (CollisionHull hull = thrust.Thrust.CreateCollisionHull(_world))
                {
                    thrust.Body = new Body(hull, Matrix3D.Identity, thrust.Thrust.TotalMass, new Visual3D[] { model });
                    thrust.Body.MaterialGroupID = _material_Ship;
                    thrust.Body.LinearDamping = .01f;
                    thrust.Body.AngularDamping = new Vector3D(.01f, .01f, .01f);
                }
            }

            #endregion
            #region neuron visuals

            foreach (var thrust in _thrusters)
            {
                BuildNeuronVisuals(out thrust.Neurons, out ModelVisual3D model, thrust.Thrust.Neruons_All, thrust.Thrust, _colors);

                thrust.NeuronVisual = model;
                _viewportNeural.Children.Add(model);
            }

            #endregion

            UpdateCountReport();
        }
        private void CreateImpulse(ImpulseEngineDNA[] dna)
        {
            if (_impulse != null)
            {
                throw new InvalidOperationException("Existing impulse should have been wiped out before calling CreateImpulse");
            }

            ImpulseStuff[] impulse = new ImpulseStuff[dna.Length];
            for (int cntr = 0; cntr < dna.Length; cntr++)
            {
                impulse[cntr] = new ImpulseStuff()
                {
                    Impulse = new ImpulseEngine(_editorOptions, _itemOptions, dna[cntr], _containers?.Plasma),
                    DNAExternalLinks = dna[cntr].ExternalLinks,
                };
                impulse[cntr].Impulse.RequestWorldLocation += new EventHandler<PartRequestWorldLocationArgs>(Body_RequestWorldLocation);

                #region ship visual

                // WPF
                ModelVisual3D visual = new ModelVisual3D();
                visual.Content = impulse[cntr].Impulse.Model;

                _viewport.Children.Add(visual);
                impulse[cntr].Visual = visual;

                // Physics
                using (CollisionHull hull = impulse[cntr].Impulse.CreateCollisionHull(_world))
                {
                    impulse[cntr].Body = new Body(hull, Matrix3D.Identity, impulse[cntr].Impulse.TotalMass, new Visual3D[] { visual })
                    {
                        MaterialGroupID = _material_Ship,
                        LinearDamping = .01f,
                        AngularDamping = new Vector3D(.01f, .01f, .01f)
                    };
                }

                #endregion
                #region neuron visuals

                //NOTE: Since the neurons are semitransparent, they need to be added last

                BrainTester.BuildNeuronVisuals(out impulse[cntr].Neurons, out visual, impulse[cntr].Impulse.Neruons_All, impulse[cntr].Impulse, _colors);

                impulse[cntr].NeuronVisual = visual;
                _viewportNeural.Children.Add(visual);

                #endregion
            }

            _impulse = impulse;

            UpdateCountReport();
        }
        private void CreateContainers()
        {
            const double HEIGHT = .07d;
            const double OFFSET = 1.5d;

            double offset2 = ((.1d - HEIGHT) / 2d) + HEIGHT;

            ShipPartDNA dnaEnergy = new ShipPartDNA
            {
                PartType = EnergyTank.PARTTYPE,
                Position = new Point3D(OFFSET - offset2, 0, 0),
                Orientation = new Quaternion(new Vector3D(0, 1, 0), 90),
                Scale = new Vector3D(1.3, 1.3, HEIGHT)      // the energy tank is slightly wider than the fuel tank
            };

            ShipPartDNA dnaFuel = new ShipPartDNA
            {
                PartType = FuelTank.PARTTYPE,
                Position = new Point3D(OFFSET, 0, 0),
                Orientation = new Quaternion(new Vector3D(0, 1, 0), 90),
                Scale = new Vector3D(1.5, 1.5, HEIGHT)
            };

            ShipPartDNA dnaPlasma = new ShipPartDNA()
            {
                PartType = PlasmaTank.PARTTYPE,
                Position = new Point3D(OFFSET + offset2, 0, 0),
                Orientation = new Quaternion(new Vector3D(0, 1, 0), 90),
                Scale = new Vector3D(1.6, 1.6, HEIGHT),
            };

            CreateContainers(dnaEnergy, dnaFuel, dnaPlasma);
        }
        private void CreateContainers(ShipPartDNA energyDNA, ShipPartDNA fuelDNA, ShipPartDNA plasmaDNA)
        {
            if (_containers != null)
            {
                throw new InvalidOperationException("Existing containers should have been wiped out before calling CreateContainers");
            }

            _containers = new ContainerStuff();

            // Energy
            _containers.Energy = new EnergyTank(_editorOptions, _itemOptions, energyDNA);
            _containers.Energy.QuantityCurrent = _containers.Energy.QuantityMax;

            // Fuel
            _containers.Fuel = new FuelTank(_editorOptions, _itemOptions, fuelDNA);
            _containers.Fuel.QuantityCurrent = _containers.Fuel.QuantityMax;

            // Plasma
            _containers.Plasma = new PlasmaTank(_editorOptions, _itemOptions, plasmaDNA);
            _containers.Plasma.QuantityCurrent = _containers.Fuel.QuantityMax;

            #region ship visuals (energy)

            // WPF
            ModelVisual3D model = new ModelVisual3D();
            model.Content = _containers.Energy.Model;

            _viewport.Children.Add(model);
            _containers.EnergyVisual = model;

            // Physics
            CollisionHull hull = _containers.Energy.CreateCollisionHull(_world);
            _containers.EnergyBody = new Body(hull, Matrix3D.Identity, _containers.Energy.TotalMass, new Visual3D[] { model });
            hull.Dispose();
            _containers.EnergyBody.MaterialGroupID = _material_Ship;
            _containers.EnergyBody.LinearDamping = .01f;
            _containers.EnergyBody.AngularDamping = new Vector3D(.01f, .01f, .01f);

            #endregion
            #region ship visuals (fuel)

            // WPF
            model = new ModelVisual3D();
            model.Content = _containers.Fuel.Model;

            _viewport.Children.Add(model);
            _containers.FuelVisual = model;

            // Physics
            hull = _containers.Fuel.CreateCollisionHull(_world);
            _containers.FuelBody = new Body(hull, Matrix3D.Identity, _containers.Fuel.TotalMass, new Visual3D[] { model });
            hull.Dispose();
            _containers.FuelBody.MaterialGroupID = _material_Ship;
            _containers.FuelBody.LinearDamping = .01f;
            _containers.FuelBody.AngularDamping = new Vector3D(.01f, .01f, .01f);

            #endregion
            #region ship visuals (plama)

            // WPF
            model = new ModelVisual3D();
            model.Content = _containers.Plasma.Model;

            _viewport.Children.Add(model);
            _containers.PlasmaVisual = model;

            // Physics
            hull = _containers.Plasma.CreateCollisionHull(_world);
            _containers.PlasmaBody = new Body(hull, Matrix3D.Identity, _containers.Plasma.TotalMass, new Visual3D[] { model });
            hull.Dispose();
            _containers.PlasmaBody.MaterialGroupID = _material_Ship;
            _containers.PlasmaBody.LinearDamping = .01f;
            _containers.PlasmaBody.AngularDamping = new Vector3D(.01f, .01f, .01f);

            #endregion
        }
        private void CreateLinks()
        {
            #region build up input args

            List<NeuralUtility.ContainerInput> inputs = new List<NeuralUtility.ContainerInput>();
            if (_gravSensors != null)
            {
                foreach (GravSensorStuff sensor in _gravSensors)
                {
                    // The sensor is a source, so shouldn't have any links.  But it needs to be included in the args so that other
                    // neuron containers can hook to it
                    inputs.Add(new NeuralUtility.ContainerInput(sensor.Body.Token, sensor.Sensor, NeuronContainerType.Sensor, sensor.Sensor.Position, sensor.Sensor.Orientation, null, null, 0, null, null));
                }
            }

            if (_brains != null)
            {
                foreach (BrainStuff brain in _brains)
                {
                    int brainChemicalCount = 0;
                    if (brain.BrainStandard != null)
                    {
                        brainChemicalCount = Convert.ToInt32(Math.Round(brain.BrainStandard.BrainChemicalCount * 1.33d, 0));		// increasing so that there is a higher chance of listeners
                    }

                    inputs.Add(new NeuralUtility.ContainerInput
                    (
                        brain.Body.Token,
                        brain.Brain.container,
                        brain.BrainStandard != null ?
                            NeuronContainerType.Brain_Standalone :
                            NeuronContainerType.Brain_HasInternalNN,
                        brain.Brain.part.Position, brain.Brain.part.Orientation,
                        _itemOptions.Brain_LinksPerNeuron_Internal,
                        new[]
                        {
                            (NeuronContainerType.Sensor, NeuralUtility.ExternalLinkRatioCalcType.Smallest, _itemOptions.Brain_LinksPerNeuron_External_FromSensor),
                            (NeuronContainerType.Brain_HasInternalNN, NeuralUtility.ExternalLinkRatioCalcType.Average, _itemOptions.Brain_LinksPerNeuron_External_FromBrain),
                            (NeuronContainerType.Brain_Standalone, NeuralUtility.ExternalLinkRatioCalcType.Average, _itemOptions.Brain_LinksPerNeuron_External_FromBrain),
                            (NeuronContainerType.Manipulator, NeuralUtility.ExternalLinkRatioCalcType.Smallest, _itemOptions.Brain_LinksPerNeuron_External_FromManipulator)
                        },
                        brainChemicalCount,
                        brain.DNAInternalLinks, brain.DNAExternalLinks
                    ));
                }
            }

            if (_thrusters != null)
            {
                foreach (var thrust in _thrusters)
                {
                    //NOTE: This won't be fed by other manipulators
                    inputs.Add(new NeuralUtility.ContainerInput
                    (
                        thrust.Body.Token,
                        thrust.Thrust, NeuronContainerType.Manipulator,
                        thrust.Thrust.Position, thrust.Thrust.Orientation,
                        null,
                        new[]
                        {
                            (NeuronContainerType.Sensor, NeuralUtility.ExternalLinkRatioCalcType.Destination, _itemOptions.Thruster_LinksPerNeuron_Sensor),
                            (NeuronContainerType.Brain_HasInternalNN, NeuralUtility.ExternalLinkRatioCalcType.Destination, _itemOptions.Thruster_LinksPerNeuron_Brain),
                            (NeuronContainerType.Brain_Standalone, NeuralUtility.ExternalLinkRatioCalcType.Destination, _itemOptions.Thruster_LinksPerNeuron_Brain),
                        },
                        0,
                        null, thrust.DNAExternalLinks
                    ));
                }
            }

            if (_impulse != null)
            {
                foreach (var impulse in _impulse)
                {
                    inputs.Add(new NeuralUtility.ContainerInput
                    (
                        impulse.Body.Token,
                        impulse.Impulse, NeuronContainerType.Manipulator,
                        impulse.Impulse.Position, impulse.Impulse.Orientation,
                        null,
                        new[]
                        {
                            (NeuronContainerType.Sensor, NeuralUtility.ExternalLinkRatioCalcType.Destination, _itemOptions.Thruster_LinksPerNeuron_Sensor),
                            (NeuronContainerType.Brain_HasInternalNN, NeuralUtility.ExternalLinkRatioCalcType.Destination, _itemOptions.Thruster_LinksPerNeuron_Brain),
                            (NeuronContainerType.Brain_Standalone, NeuralUtility.ExternalLinkRatioCalcType.Destination, _itemOptions.Thruster_LinksPerNeuron_Brain),
                        },
                        0,
                        null, impulse.DNAExternalLinks
                    ));
                }
            }

            #endregion

            // Create links
            NeuralUtility.ContainerOutput[] outputs = null;
            if (inputs.Count > 0)
            {
                outputs = NeuralUtility.LinkNeurons(inputs.ToArray(), _itemOptions.NeuralLink_MaxWeight);
            }

            // Show new links
            if (outputs != null)
            {
                ShowLinks(outputs);
            }

            UpdateCountReport();

            if (chkBrainRunning.IsChecked.Value)
            {
                StartBrainOperation();
            }
        }

        private void ShowLinks(NeuralUtility.ContainerOutput[] outputs)
        {
            _links = new LinkStuff
            {
                Outputs = outputs,
                Visuals = new List<Visual3D>(),
            };

            Model3DGroup posLines = null, negLines = null;
            DiffuseMaterial posDiffuse = null, negDiffuse = null;

            Dictionary<INeuronContainer, Transform3D> containerTransforms = new Dictionary<INeuronContainer, Transform3D>();

            foreach (var output in outputs)
            {
                Transform3D toTransform = GetContainerTransform(output.Container, containerTransforms);

                foreach (var link in UtilityCore.Iterate(output.InternalLinks, output.ExternalLinks))
                {
                    Transform3D fromTransform = GetContainerTransform(link.FromContainer, containerTransforms);

                    BuildLinkVisual(ref posLines, ref posDiffuse, ref negLines, ref negDiffuse, fromTransform.Transform(link.From.Position), toTransform.Transform(link.To.Position), link.Weight, link.BrainChemicalModifiers, _colors);
                }
            }

            if (posLines != null)
            {
                ModelVisual3D model = new ModelVisual3D();
                model.Content = posLines;
                _links.Visuals.Add(model);
                _viewportNeural.Children.Add(model);
            }

            if (negLines != null)
            {
                ModelVisual3D model = new ModelVisual3D();
                model.Content = negLines;
                _links.Visuals.Add(model);
                _viewportNeural.Children.Add(model);
            }
        }

        private void CreateLinks2()
        {
            Color colorInput = UtilityWPF.AlphaBlend(Colors.DarkGreen, Colors.LawnGreen, .2);
            Color colorOutput = UtilityWPF.AlphaBlend(Colors.Indigo, Colors.DodgerBlue, .2);
            Color colorBrain = UtilityWPF.AlphaBlend(Colors.Crimson, Colors.HotPink, .2);

            #region input args

            List<NeuralUtility.ContainerInput> inputs = new List<NeuralUtility.ContainerInput>();
            if (_gravSensors != null)
            {
                foreach (GravSensorStuff sensor in _gravSensors)
                {
                    // The sensor is a source, so shouldn't have any links.  But it needs to be included in the args so that other
                    // neuron containers can hook to it
                    inputs.Add(new NeuralUtility.ContainerInput(sensor.Body.Token, sensor.Sensor, NeuronContainerType.Sensor, sensor.Sensor.Position, sensor.Sensor.Orientation, null, null, 0, null, null));
                }
            }

            if (_brains != null)
            {
                foreach (BrainStuff brain in _brains)
                {
                    int brainChemicalCount = 0;
                    if (brain.BrainStandard != null)
                    {
                        brainChemicalCount = Convert.ToInt32(Math.Round(brain.BrainStandard.BrainChemicalCount * 1.33d, 0));		// increasing so that there is a higher chance of listeners
                    }

                    inputs.Add(new NeuralUtility.ContainerInput(
                        brain.Body.Token,
                        brain.Brain.container,
                        brain.BrainStandard != null ?
                            NeuronContainerType.Brain_Standalone :
                            NeuronContainerType.Brain_HasInternalNN,
                        brain.Brain.part.Position, brain.Brain.part.Orientation,
                        _itemOptions.Brain_LinksPerNeuron_Internal,
                        null,
                        brainChemicalCount,
                        brain.DNAInternalLinks, brain.DNAExternalLinks));
                }
            }

            if (_thrusters != null)
            {
                foreach (var thrust in _thrusters)
                {
                    //NOTE: This won't be fed by other manipulators
                    inputs.Add(new NeuralUtility.ContainerInput(
                        thrust.Body.Token,
                        thrust.Thrust, NeuronContainerType.Manipulator,
                        thrust.Thrust.Position, thrust.Thrust.Orientation,
                        null,
                        null,
                        0,
                        null, thrust.DNAExternalLinks));
                }
            }

            if (_impulse != null)
            {
                foreach (var impulse in _impulse)
                {
                    inputs.Add(new NeuralUtility.ContainerInput(
                        impulse.Body.Token,
                        impulse.Impulse, NeuronContainerType.Manipulator,
                        impulse.Impulse.Position, impulse.Impulse.Orientation,
                        null,
                        null,
                        0,
                        null, impulse.DNAExternalLinks));
                }
            }

            #endregion

            // quick test, just worry about wiring inputs to brains
            //CreateLinks2_brains_inputs1(inputs.ToArray(), colorInput, colorBrain);

            #region link containers

            // Split out the inputs into their own arrays so that the later indices make sense
            NeuralUtility.ContainerInput[] items_brain = inputs.
                Where(o => o.ContainerType.In(NeuronContainerType.Brain_HasInternalNN, NeuronContainerType.Brain_Standalone)).
                ToArray();

            NeuralUtility.ContainerInput[] items_io = inputs.
                Where(o => o.ContainerType.In(NeuronContainerType.Sensor, NeuronContainerType.Manipulator)).
                ToArray();

            // Prep for the linkers
            ItemLinker_OverflowArgs overflow = new ItemLinker_OverflowArgs();
            ItemLinker_ExtraArgs extra = new ItemLinker_ExtraArgs();
            ItemLinker_CombineArgs combineArgs = new ItemLinker_CombineArgs();

            LinkItem[] linkItems_brain = items_brain.
                Select(o => new LinkItem(o.Position, o.Container.Neruons_All.Count())).
                ToArray();

            LinkItem[] linkItems_io = items_io.
                Select(o => new LinkItem(o.Position, o.Container.Neruons_All.Count())).
                ToArray();

            // Link brains to io
            //TODO: extra.Percent should differ between input and output (multiple brains writing to the same manipulator would probably be a mess)
            var links_brain_io = ItemLinker.Link_1_2(linkItems_brain, linkItems_io, overflow, extra);

            // Link brains to brains
            ItemLinker.Link_Self(out var links_allBrain, out var links_finalBrain, linkItems_brain, combineArgs);

            #endregion

            CreateLinks2_containers(items_brain, items_io, links_brain_io, links_allBrain, links_finalBrain, colorBrain, colorInput, colorOutput);

            for (int brainCntr = 0; brainCntr < linkItems_brain.Length; brainCntr++)
            {
                //CreateLinks2_brains_inputs2a(brainCntr, items_brain, items_io, links_brain_io, links_allBrain, links_finalBrain, colorBrain, colorInput, colorOutput);
                CreateLinks2_brains_inputs2b(brainCntr, items_brain, items_io, links_brain_io, links_allBrain, links_finalBrain, colorBrain, colorInput, colorOutput);
            }

            //ShowLinks();

            //UpdateCountReport();

            //if (chkBrainRunning.IsChecked.Value)
            //{
            //    StartBrainOperation();
            //}
        }
        private static void CreateLinks2_containers(NeuralUtility.ContainerInput[] items_brain, NeuralUtility.ContainerInput[] items_io, Tuple<int, int>[] links_brain_io, SortedList<Tuple<int, int>, double> links_allBrain, LinkSetPair[] links_finalBrain, Color colorBrain, Color colorInput, Color colorOutput)
        {
            const double THICKNESS = .005;
            const double DOT = .03;

            Debug3DWindow window = new Debug3DWindow()
            {
                Title = "container links",
            };

            #region switch color

            var getColor = new Func<NeuralUtility.ContainerInput, Color>(c =>
            {
                switch (c.ContainerType)
                {
                    case NeuronContainerType.Sensor:
                        return colorInput;

                    case NeuronContainerType.Brain_HasInternalNN:
                    case NeuronContainerType.Brain_Standalone:
                        return colorBrain;

                    case NeuronContainerType.Manipulator:
                        return colorOutput;

                    default:
                        return Colors.Black;
                }
            });

            #endregion

            #region containers

            foreach (NeuralUtility.ContainerInput container in items_brain.Concat(items_io))
            {
                Color color = getColor(container);

                window.AddDot(container.Position, DOT * 3, color);
            }

            #endregion

            #region brain - io

            foreach (var link in links_brain_io)
            {
                Color color = getColor(items_io[link.Item2]);
                color = UtilityWPF.AlphaBlend(color, Colors.White, .25);

                window.AddLine(items_brain[link.Item1].Position, items_io[link.Item2].Position, THICKNESS, color);
            }

            #endregion

            #region brain - brain

            foreach (var link in links_allBrain)
            {
                Color color = UtilityWPF.AlphaBlend(colorBrain, Colors.White, .25);

                window.AddLine(items_brain[link.Key.Item1].Position, items_brain[link.Key.Item2].Position, THICKNESS * link.Value, color);
            }

            #endregion

            window.Show();
        }
        private static void CreateLinks2_brains_inputs1(NeuralUtility.ContainerInput[] inputs, Color colorInput, Color colorBrain)
        {
            const double THICKNESS = .005;
            const double DOT = .03;

            var sensors = inputs.
                Where(o => o.ContainerType == NeuronContainerType.Sensor).
                ToArray();

            foreach (var brain in inputs.Where(o => o.ContainerType.In(NeuronContainerType.Brain_HasInternalNN, NeuronContainerType.Brain_Standalone)))
            {
                Debug3DWindow window = new Debug3DWindow();

                #region draw input neurons

                foreach (var sensor in sensors)
                {
                    window.AddDot(sensor.Position, DOT / 2, UtilityWPF.AlphaBlend(colorInput, Colors.Transparent, .25));

                    Transform3D transform1 = GetContainerTransform(sensor.Container);

                    foreach (var neuron in sensor.ReadableNeurons)
                    {
                        window.AddDot(transform1.Transform(neuron.Position), DOT, colorInput);
                    }
                }

                #endregion
                #region draw brain neurons

                window.AddDot(brain.Position, DOT / 2, UtilityWPF.AlphaBlend(colorBrain, Colors.Transparent, .25));

                Transform3D transform2 = GetContainerTransform(brain.Container);

                foreach (var neuron in brain.WritableNeurons)
                {
                    window.AddDot(transform2.Transform(neuron.Position), DOT, colorBrain);
                }

                #endregion

                // Give statistics
                int inputCount = sensors.Sum(o => o.ReadableNeurons.Count());
                int brainCount = brain.WritableNeurons.Count();
                window.AddText(string.Format("Input Neurons: {0}", inputCount));
                window.AddText(string.Format("Brain Neurons: {0}", brainCount));

                #region links

                NeuralUtility.ContainerOutput links = NewLinker.LinkNeurons_Sensors(sensors, brain);

                foreach (NeuralLink link in links.ExternalLinks)
                {
                    Transform3D transformFrom = GetContainerTransform(link.FromContainer);
                    Transform3D transformTo = GetContainerTransform(link.ToContainer);

                    window.AddLine(transformFrom.Transform(link.From.Position), transformTo.Transform(link.To.Position), THICKNESS, Colors.White);
                }

                #endregion

                window.Show();
            }
        }
        private static void CreateLinks2_brains_inputs2a(int brainIndex, NeuralUtility.ContainerInput[] items_brain, NeuralUtility.ContainerInput[] items_io, Tuple<int, int>[] links_brain_io, SortedList<Tuple<int, int>, double> links_allBrain, LinkSetPair[] links_finalBrain, Color colorBrain, Color colorInput, Color colorOutput)
        {
            const double THICKNESS = .005;
            const double DOT = .03;

            // first draft: ignore outputs, ignore brain-brain

            if (items_io.Any(o => o.ContainerType == NeuronContainerType.Manipulator))
            {
                throw new ApplicationException("this tester function can't handle outputs");
            }

            Debug3DWindow window = new Debug3DWindow()
            {
                Title = $"brain {brainIndex}",
            };

            NeuralUtility.ContainerInput[] items_io_actual = links_brain_io.
                Where(o => o.Item1 == brainIndex).
                Select(o => items_io[o.Item2]).
                ToArray();

            #region draw input neurons

            foreach (var io in items_io_actual)
            {
                window.AddDot(io.Position, DOT / 2, UtilityWPF.AlphaBlend(colorInput, Colors.Transparent, .25));

                Transform3D transform1 = GetContainerTransform(io.Container);

                foreach (var neuron in io.ReadableNeurons)
                {
                    window.AddDot(transform1.Transform(neuron.Position), DOT, colorInput);
                }
            }

            #endregion
            #region draw brain neurons

            window.AddDot(items_brain[brainIndex].Position, DOT / 2, UtilityWPF.AlphaBlend(colorBrain, Colors.Transparent, .25));

            Transform3D transform2 = GetContainerTransform(items_brain[brainIndex].Container);

            foreach (var neuron in items_brain[brainIndex].WritableNeurons)
            {
                window.AddDot(transform2.Transform(neuron.Position), DOT, colorBrain);
            }

            #endregion

            // Give statistics
            int inputCount = items_io_actual.Sum(o => o.ReadableNeurons.Count());
            int brainCount = items_brain[brainIndex].WritableNeurons.Count();
            window.AddText(string.Format("Input Neurons: {0}", inputCount));
            window.AddText(string.Format("Brain Neurons: {0}", brainCount));

            #region links

            NeuralUtility.ContainerOutput links = NewLinker.LinkNeurons_Sensors(items_io_actual, items_brain[brainIndex]);

            foreach (NeuralLink link in links.ExternalLinks)
            {
                Transform3D transformFrom = GetContainerTransform(link.FromContainer);
                Transform3D transformTo = GetContainerTransform(link.ToContainer);

                window.AddLine(transformFrom.Transform(link.From.Position), transformTo.Transform(link.To.Position), THICKNESS, Colors.White);
            }

            #endregion

            window.Show();
        }
        private static void CreateLinks2_brains_inputs2b(int brainIndex, NeuralUtility.ContainerInput[] items_brain, NeuralUtility.ContainerInput[] items_io, Tuple<int, int>[] links_brain_io, SortedList<Tuple<int, int>, double> links_allBrain, LinkSetPair[] links_finalBrain, Color colorBrain, Color colorInput, Color colorOutput)
        {
            const double THICKNESS = .005;
            const double DOT = .03;

            // second draft: ignore brain-brain

            Debug3DWindow window = new Debug3DWindow()
            {
                Title = $"brain {brainIndex}",
            };

            NeuralUtility.ContainerInput[] items_io_actual = links_brain_io.
                Where(o => o.Item1 == brainIndex).
                Select(o => items_io[o.Item2]).
                ToArray();

            #region switch color

            var getColor = new Func<NeuralUtility.ContainerInput, Color>(c =>
            {
                switch (c.ContainerType)
                {
                    case NeuronContainerType.Sensor:
                        return colorInput;

                    case NeuronContainerType.Brain_HasInternalNN:
                    case NeuronContainerType.Brain_Standalone:
                        return colorBrain;

                    case NeuronContainerType.Manipulator:
                        return colorOutput;

                    default:
                        return Colors.Black;
                }
            });

            #endregion

            #region draw io neurons

            foreach (var io in items_io_actual)
            {
                Color color = getColor(io);

                window.AddDot(io.Position, DOT / 2, UtilityWPF.AlphaBlend(color, Colors.Transparent, .25));

                Transform3D transform1 = GetContainerTransform(io.Container);

                var neurons = io.ContainerType == NeuronContainerType.Sensor ?
                    io.ReadableNeurons :
                    io.WritableNeurons;

                foreach (var neuron in neurons)
                {
                    window.AddDot(transform1.Transform(neuron.Position), DOT, color);
                }
            }

            #endregion
            #region draw brain neurons

            window.AddDot(items_brain[brainIndex].Position, DOT / 2, UtilityWPF.AlphaBlend(colorBrain, Colors.Transparent, .25));

            Transform3D transform2 = GetContainerTransform(items_brain[brainIndex].Container);

            foreach (var neuron in items_brain[brainIndex].Container.Neruons_Writeonly)
            {
                window.AddDot(transform2.Transform(neuron.Position), DOT, UtilityWPF.AlphaBlend(Colors.White, colorBrain, .25));
            }

            foreach (var neuron in items_brain[brainIndex].Container.Neruons_ReadWrite)
            {
                window.AddDot(transform2.Transform(neuron.Position), DOT, colorBrain);
            }

            foreach (var neuron in items_brain[brainIndex].Container.Neruons_Readonly)
            {
                window.AddDot(transform2.Transform(neuron.Position), DOT, UtilityWPF.AlphaBlend(Colors.Black, colorBrain, .25));
            }

            #endregion

            // Give statistics

            #region links input

            NeuralUtility.ContainerInput[] items_input_actual = items_io_actual.
                Where(o => o.ContainerType == NeuronContainerType.Sensor).
                ToArray();

            int inputCount = items_input_actual.Sum(o => o.ReadableNeurons.Count());
            window.AddText(string.Format("Input Neurons: {0}", inputCount));

            if (inputCount > 0)
            {
                NeuralUtility.ContainerOutput linksInput = NewLinker.LinkNeurons_Sensors(items_input_actual, items_brain[brainIndex]);

                foreach (NeuralLink link in linksInput.ExternalLinks)
                {
                    Transform3D transformFrom = GetContainerTransform(link.FromContainer);
                    Transform3D transformTo = GetContainerTransform(link.ToContainer);

                    window.AddLine(transformFrom.Transform(link.From.Position), transformTo.Transform(link.To.Position), THICKNESS, Colors.White);
                }
            }

            #endregion
            #region links output

            NeuralUtility.ContainerInput[] items_output_actual = items_io_actual.
                Where(o => o.ContainerType == NeuronContainerType.Manipulator).
                ToArray();

            int outputCount = items_output_actual.Sum(o => o.WritableNeurons.Count());
            window.AddText(string.Format("Output Neurons: {0}", outputCount));

            if (outputCount > 0)
            {
                NeuralUtility.ContainerOutput[] linksOutputs = NewLinker.LinkNeurons_Manipulators(items_output_actual, items_brain[brainIndex]);

                foreach (NeuralUtility.ContainerOutput linksOutput in linksOutputs)
                {
                    foreach (NeuralLink link in linksOutput.ExternalLinks)
                    {
                        Transform3D transformFrom = GetContainerTransform(link.FromContainer);
                        Transform3D transformTo = GetContainerTransform(link.ToContainer);

                        window.AddLine(transformFrom.Transform(link.From.Position), transformTo.Transform(link.To.Position), THICKNESS, Colors.White);
                    }
                }
            }

            #endregion

            int brainCount = items_brain[brainIndex].WritableNeurons.Count();
            window.AddText(string.Format("Brain Neurons: {0}", brainCount));

            window.Show();
        }

        private void CreateLinks3()
        {
            Color colorInput = UtilityWPF.AlphaBlend(Colors.DarkGreen, Colors.LawnGreen, .2);
            Color colorOutput = UtilityWPF.AlphaBlend(Colors.Indigo, Colors.DodgerBlue, .2);
            Color colorBrain = UtilityWPF.AlphaBlend(Colors.Crimson, Colors.HotPink, .2);

            Random rand = StaticRandom.GetRandomForThread();

            #region input args

            List<NeuralUtility.ContainerInput> inputs = new List<NeuralUtility.ContainerInput>();
            if (_gravSensors != null)
            {
                foreach (GravSensorStuff sensor in _gravSensors)
                {
                    // The sensor is a source, so shouldn't have any links.  But it needs to be included in the args so that other
                    // neuron containers can hook to it
                    inputs.Add(new NeuralUtility.ContainerInput(sensor.Body.Token, sensor.Sensor, NeuronContainerType.Sensor, sensor.Sensor.Position, sensor.Sensor.Orientation, null, null, 0, null, null));
                }
            }

            if (_brains != null)
            {
                foreach (BrainStuff brain in _brains)
                {
                    int brainChemicalCount = 0;
                    if (brain.BrainStandard != null)
                    {
                        brainChemicalCount = Convert.ToInt32(Math.Round(brain.BrainStandard.BrainChemicalCount * 1.33d, 0));		// increasing so that there is a higher chance of listeners
                    }

                    inputs.Add(new NeuralUtility.ContainerInput(
                        brain.Body.Token,
                        brain.Brain.container,
                        brain.BrainStandard != null ?
                            NeuronContainerType.Brain_Standalone :
                            NeuronContainerType.Brain_HasInternalNN,
                        brain.Brain.part.Position, brain.Brain.part.Orientation,
                        _itemOptions.Brain_LinksPerNeuron_Internal,
                        null,
                        brainChemicalCount,
                        brain.DNAInternalLinks, brain.DNAExternalLinks));
                }
            }

            if (_thrusters != null)
            {
                foreach (var thrust in _thrusters)
                {
                    //NOTE: This won't be fed by other manipulators
                    inputs.Add(new NeuralUtility.ContainerInput(
                        thrust.Body.Token,
                        thrust.Thrust, NeuronContainerType.Manipulator,
                        thrust.Thrust.Position, thrust.Thrust.Orientation,
                        null,
                        null,
                        0,
                        null, thrust.DNAExternalLinks));
                }
            }

            if (_impulse != null)
            {
                foreach (var impulse in _impulse)
                {
                    inputs.Add(new NeuralUtility.ContainerInput(
                        impulse.Body.Token,
                        impulse.Impulse, NeuronContainerType.Manipulator,
                        impulse.Impulse.Position, impulse.Impulse.Orientation,
                        null,
                        null,
                        0,
                        null, impulse.DNAExternalLinks));
                }
            }

            #endregion

            #region link containers

            // Split out the inputs into their own arrays so that the later indices make sense
            NeuralUtility.ContainerInput[] items_brain = inputs.
                Where(o => o.ContainerType.In(NeuronContainerType.Brain_HasInternalNN, NeuronContainerType.Brain_Standalone)).
                ToArray();

            NeuralUtility.ContainerInput[] items_io = inputs.
                Where(o => o.ContainerType.In(NeuronContainerType.Sensor, NeuronContainerType.Manipulator)).
                ToArray();

            // Prep for the linkers
            ItemLinker_OverflowArgs overflow = new ItemLinker_OverflowArgs();
            ItemLinker_ExtraArgs extra = new ItemLinker_ExtraArgs()
            {
                Percents = new[] { 1d, 0d },     // manipulators should only be mapped to one brain
            };
            ItemLinker_CombineArgs combineArgs = new ItemLinker_CombineArgs();

            LinkItem[] linkItems_brain = items_brain.
                Select(o => new LinkItem(o.Position, o.Container.Neruons_All.Count())).
                ToArray();

            LinkItem[] linkItems_io = items_io.
                Select(o =>
                {
                    int percentIndex = -1;      // this corresponds with the extra.Percents array
                    switch (o.ContainerType)
                    {
                        case NeuronContainerType.Sensor: percentIndex = 0; break;
                        case NeuronContainerType.Manipulator: percentIndex = 1; break;
                    }
                    return new LinkItem(o.Position, o.Container.Neruons_All.Count(), percentIndex);
                }).
                ToArray();

            // Link brains to io
            var links_brain_io = ItemLinker.Link_1_2(linkItems_brain, linkItems_io, overflow, extra);

            // Link brains to brains
            ItemLinker.Link_Self(out _, out var links_brain, linkItems_brain, combineArgs);

            CreateLinks3_drawContainers(items_brain, items_io, links_brain_io, links_brain, colorBrain, colorInput, colorOutput);

            #endregion

            #region link neurons - fail 1

            //List<ContainerReads> sourceContainers = CreateLinks3_possibleMappings(items_brain, items_io, links_brain_io, links_brain);

            //List<SourceDestinationLink> links = new List<SourceDestinationLink>();

            //// Iterate through containers one at a time (probably just the containers that have something to write):
            ////      Sort them by readable count so that the larger ones get chosen more often
            //while (sourceContainers.Count > 0)
            //{
            //    // Pick a random source container
            //    int sourceIndex = UtilityCore.GetIndexIntoList(rand.NextPow(2), sourceContainers.Count);
            //    ContainerReads source = sourceContainers[sourceIndex];

            //    // Pick a random destination container that is linked to it (also weighted by relative sizes)
            //    int destIndex = UtilityCore.GetIndexIntoList(rand.NextPow(2), source.RemainingCandidates.Count);
            //    ContainerWrites destination = source.RemainingCandidates[destIndex];

            //    // Pick source and destination neurons - these should be the next available, sorted by distance - but with a bit of randomness thrown in
            //    //      When not all destinations are linked yet, choose an unlinked.  Once they all have one link, choose from the list that only have one link, etc
            //    int linkIndex = UtilityCore.GetIndexIntoList(rand.NextPow(2), destination.RemainingCandidates.Count);
            //    SourceDestinationLink link = destination.RemainingCandidates[linkIndex];

            //    links.Add(link);

            //    // Remove this link from the lists
            //    CreateLinks3_removeLink(sourceContainers, link);
            //}


            //TODO: Create internal links


            //[optional]: Once everything is mapped, clean up each container by keeping clusters together

            //[optional]: Randomize the weights



            #endregion
            #region link neurons

            List<ContainerReads> sourceContainers = CreateLinks3_possibleMappings(items_brain, items_io, links_brain_io, links_brain);

            List<SourceDestinationLink> links = new List<SourceDestinationLink>();

            // Iterate through containers one at a time (probably just the containers that have something to write):
            //      Sort them by readable count so that the larger ones get chosen more often
            while (sourceContainers.Count > 0)
            {
                // Pick a random source container
                double totalReadable = sourceContainers.Sum(o => o.Container.ReadableNeurons.Count());
                var sourceFractions = sourceContainers.
                    Select((o, i) => (i, o.Container.ReadableNeurons.Count().ToDouble() / totalReadable)).
                    ToArray();

                //int sourceIndex = UtilityCore.GetIndexIntoList(rand.NextPow(2), sourceContainers.Count);      // this just favors one source.  Need to pull from all sources evenly
                int sourceIndex = UtilityCore.GetIndexIntoList(rand.NextDouble(), sourceFractions);     // need sourceFractions so that larger containers get chosen proportianally more
                ContainerReads source = sourceContainers[sourceIndex];

                // Pick a random destination container that is linked to it (also weighted by relative sizes)
                double totalWritable = source.RemainingCandidates.Sum(o => o.Weight * o.Container.WritableNeurons.Count());
                var destFractions = source.RemainingCandidates.
                    Select((o, i) => (i, (o.Weight * o.Container.WritableNeurons.Count()).ToDouble() / totalWritable)).
                    ToArray();

                //int destIndex = UtilityCore.GetIndexIntoList(rand.NextPow(2), source.RemainingCandidates.Count);
                int destIndex = UtilityCore.GetIndexIntoList(rand.NextDouble(), destFractions);
                ContainerWrites destination = source.RemainingCandidates[destIndex];

                // Pick source and destination neurons
                SourceDestinationLink[] sortedLinks = destination.RemainingCandidates.
                    OrderBy(o => o.DestinationNeuronNumConnections.Count).        // give unlinked neurons a much higer priority
                    ThenBy(o => o.DistanceNeurons).     // within a set of the same number of connections, give closer neurons more priority
                    ToArray();

                int test_from = sortedLinks[0].DestinationNeuronNumConnections.Count;
                int test_to = sortedLinks[sortedLinks.Length - 1].DestinationNeuronNumConnections.Count;

                int linkIndex = UtilityCore.GetIndexIntoList(rand.NextPow(2), sortedLinks.Length);

                SourceDestinationLink link = sortedLinks[linkIndex];

                links.Add(link);
                link.DestinationNeuronNumConnections.Count++;

                // Remove this link from the lists
                CreateLinks3_removeLink(sourceContainers, link);
            }


            //TODO: Create internal links


            //[optional]: Once everything is mapped, clean up each container by keeping clusters together

            //[optional]: Randomize the weights



            #endregion

            #region commit results

            List<NeuralUtility.ContainerOutput> final = new List<NeuralUtility.ContainerOutput>();

            var linksByDestinationContainer = links.
                ToLookup(o => o.DestinationContainer.Token);

            foreach (var linkSet in linksByDestinationContainer)
            {
                INeuronContainer container = linkSet.First().DestinationContainer.Container;
                NeuralLink[] externalLinks = linkSet.
                    Select(o => new NeuralLink(o.SourceContainer.Container, o.DestinationContainer.Container, o.SourceNeuron, o.DestinationNeuron, o.GetLinkWeight(_itemOptions.NeuralLink_MaxWeight), null)).
                    ToArray();

                final.Add(new NeuralUtility.ContainerOutput(container, null, externalLinks));
            }

            ShowLinks(final.ToArray());

            UpdateCountReport();

            if (chkBrainRunning.IsChecked.Value)
            {
                StartBrainOperation();
            }

            #endregion
        }
        private static void CreateLinks3_drawContainers(NeuralUtility.ContainerInput[] items_brain, NeuralUtility.ContainerInput[] items_io, Tuple<int, int>[] links_brain_io, LinkSetPair[] links_brain, Color colorBrain, Color colorInput, Color colorOutput)
        {
            const double THICKNESS = .005;
            const double DOT = .03;
            const double HEIGHT = .1;

            Debug3DWindow window = new Debug3DWindow()
            {
                Title = "container links",
            };

            #region getColor

            var getColor = new Func<NeuralUtility.ContainerInput, Color>(c =>
            {
                switch (c.ContainerType)
                {
                    case NeuronContainerType.Sensor:
                        return colorInput;

                    case NeuronContainerType.Brain_HasInternalNN:
                    case NeuronContainerType.Brain_Standalone:
                        return colorBrain;

                    case NeuronContainerType.Manipulator:
                        return colorOutput;

                    default:
                        return Colors.Black;
                }
            });

            #endregion

            #region containers

            foreach (NeuralUtility.ContainerInput container in items_brain.Concat(items_io))
            {
                Color color = getColor(container);
                string text = string.Format("R={0}\r\nRW={1}\r\nW={2}", container.Container.Neruons_Readonly.Count(), container.Container.Neruons_ReadWrite.Count(), container.Container.Neruons_Writeonly.Count());

                window.AddText3D(text, container.Position, new Vector3D(0, 0, 1), HEIGHT, color, true);
                //window.AddDot(container.Position, DOT * 3, color);
            }

            #endregion

            #region brain - io

            foreach (var link in links_brain_io)
            {
                Color color = getColor(items_io[link.Item2]);
                color = UtilityWPF.AlphaBlend(color, Colors.White, .25);

                window.AddLine(items_brain[link.Item1].Position, items_io[link.Item2].Position, THICKNESS, color);
            }

            #endregion

            #region brain - brain

            foreach (var link in links_brain)
            {
                Color color = UtilityWPF.AlphaBlend(colorBrain, Colors.White, .25);

                window.AddLine(link.Set1.Center, link.Set2.Center, THICKNESS, color);
            }

            #endregion

            window.Show();
        }

        //NOTE: This will produce dupes for brains that have readwrite neurons
        private static List<ContainerReads> CreateLinks3_possibleMappings(NeuralUtility.ContainerInput[] items_brain, NeuralUtility.ContainerInput[] items_io, Tuple<int, int>[] links_brain_io, LinkSetPair[] links_brain)
        {
            #region destination neruon count

            SortedList<long, DestinationNeuronCount> neuronCounts = new SortedList<long, DestinationNeuronCount>();

            foreach (INeuron neuron in items_brain.Concat(items_io).SelectMany(o => o.Container.Neruons_All))
            {
                neuronCounts.Add(neuron.Token, new DestinationNeuronCount()
                {
                    NeuronToken = neuron.Token,
                    Count = 0,
                });
            }

            #endregion

            #region brain-brain index map

            (int index, NeuralUtility.ContainerInput item)[] brainMap = items_brain.
                Select((o, i) => (i, o)).
                ToArray();

            (int, int)[] links_brain2 = links_brain.
                Select(o =>
                (
                    brainMap.First(p => p.item.Position.IsNearValue(o.Set1.Center)).index,
                    brainMap.First(p => p.item.Position.IsNearValue(o.Set2.Center)).index
                )).
                ToArray();

            double[] radius_brains = items_brain.
                Select(o => GetNeuronRadius(o)).
                ToArray();

            double[] radius_io = items_io.
                Select(o => GetNeuronRadius(o)).
                ToArray();

            #endregion

            #region map all neurons

            List<SourceDestinationLink> allLinks = new List<SourceDestinationLink>();

            foreach (Tuple<int, int> link in links_brain_io)
            {
                allLinks.AddRange(CreateLinks3_possibleMappings_pair(items_brain[link.Item1], radius_brains[link.Item1], items_io[link.Item2], radius_io[link.Item2], neuronCounts));
                allLinks.AddRange(CreateLinks3_possibleMappings_pair(items_io[link.Item2], radius_io[link.Item2], items_brain[link.Item1], radius_brains[link.Item1], neuronCounts));
            }

            foreach ((int, int) link in links_brain2)
            {
                allLinks.AddRange(CreateLinks3_possibleMappings_pair(items_brain[link.Item1], radius_brains[link.Item1], items_brain[link.Item2], radius_brains[link.Item2], neuronCounts));
                allLinks.AddRange(CreateLinks3_possibleMappings_pair(items_brain[link.Item2], radius_brains[link.Item2], items_brain[link.Item1], radius_brains[link.Item1], neuronCounts));
            }

            #endregion

            #region group by source container

            return allLinks.
                ToLookup(o => o.SourceContainer.Token).
                Select(o => new ContainerReads()        // from container
                {
                    Container = o.First().SourceContainer,
                    RemainingCandidates = o.
                        ToLookup(p => p.DestinationContainer.Token).
                        Select(p => new ContainerWrites()       // to container
                        {
                            Container = p.First().DestinationContainer,
                            Distance = p.First().DistanceContainers,
                            Weight = CreateLinks3_GetWeight(o.First().SourceContainer, p.First().DestinationContainer),
                            RemainingCandidates = p.        // individual neuron links between from and to containers
                                                            //OrderBy(q => q.DistanceNeurons).        //NOTE: this was going to be the only sort needed, but an extra sort by num connections was needed.  So a sort is performed after each linking
                                ToList(),
                        }).
                        //OrderBy(p => p.Distance).     // this isn't needed anymore, because the containers are now drawn from uniformly
                        ToList(),
                }).
                //OrderByDescending(o => o.Container.ReadableNeurons.Count()).      // this isn't needed anymore, because the containers are now drawn from uniformly
                ToList();

            #endregion
        }
        private static IEnumerable<SourceDestinationLink> CreateLinks3_possibleMappings_pair(NeuralUtility.ContainerInput from, double fromRadius, NeuralUtility.ContainerInput to, double toRadius, SortedList<long, DestinationNeuronCount> neuronCounts)
        {
            const double MULT = 3;

            Vector3D offset = (to.Position - from.Position).ToUnit() * ((fromRadius + toRadius) * MULT / 2);
            double distanceFromTo = (from.Position - to.Position).Length;

            List<SourceDestinationLink> retVal = new List<SourceDestinationLink>();

            foreach (var read in from.ReadableNeurons)
            {
                Point3D fromPos = -offset + read.Position;

                foreach (var write in to.WritableNeurons)
                {
                    Point3D toPos = offset + write.Position;

                    retVal.Add(new SourceDestinationLink()
                    {
                        SourceContainer = from,
                        DestinationContainer = to,

                        SourceNeuron = read,
                        DestinationNeuron = write,

                        DistanceContainers = distanceFromTo,
                        DistanceNeurons = (toPos - fromPos).Length,

                        DestinationNeuronNumConnections = neuronCounts[write.Token],
                    });
                }
            }

            return retVal;
        }

        private static int CreateLinks3_GetWeight(NeuralUtility.ContainerInput sourceContainer, NeuralUtility.ContainerInput destinationContainer)
        {
            switch (sourceContainer.ContainerType)
            {
                case NeuronContainerType.None:      // this should never happen, just return 1
                case NeuronContainerType.Manipulator:       // this should be rare, just return 1
                case NeuronContainerType.Sensor:
                    return 1;

                case NeuronContainerType.Brain_Standalone:
                case NeuronContainerType.Brain_HasInternalNN:
                    switch (destinationContainer.ContainerType)
                    {
                        case NeuronContainerType.None:
                        case NeuronContainerType.Sensor:
                            return 1;       // this shouldn't happen, so give it a low weight

                        case NeuronContainerType.Brain_HasInternalNN:
                        case NeuronContainerType.Brain_Standalone:
                            return 3;       // make brain:brain about one third less likely than brain:manipulator

                        case NeuronContainerType.Manipulator:
                            return 100;

                        default:
                            throw new ApplicationException($"Unexpected {typeof(NeuronContainerType)}: {destinationContainer.ContainerType}");
                    }
                    break;

                default:
                    throw new ApplicationException($"Unexpected {typeof(NeuronContainerType)}: {sourceContainer.ContainerType}");
            }
        }

        //TODO: This goes too far and removes all links to destination neuron.  Instead, it should just remove any reference to the source neuron (even
        //when it is a destination of some other source
        private static void CreateLinks3_removeLink(List<ContainerReads> sourceContainers, SourceDestinationLink link)
        {
            int index = 0;

            while (index < sourceContainers.Count)
            {
                //if (link.HasContainerMatch(sourceContainers[index].Container))      // this token check is just an optimization, because source.remove is another set of nested loops
                {
                    sourceContainers[index].RemoveLink(link);

                    if (sourceContainers[index].RemainingCandidates.Count == 0)
                    {
                        sourceContainers.RemoveAt(index);
                    }
                    else
                    {
                        index++;
                    }
                }
                //else
                //{
                //    index++;
                //}
            }
        }

        private void CreateLinks4()
        {
            const double EXISTINGRATIOCAP = 1.05;       //TODO: put this in _itemOptions

            Random rand = StaticRandom.GetRandomForThread();

            #region input args

            //NOTE: The external ratios are only looked at when pruning existing links (after wiring up existing links from a mutated child).  New
            //bots get wired up a different way

            List<NeuralUtility.ContainerInput> inputs = new List<NeuralUtility.ContainerInput>();
            if (_gravSensors != null)
            {
                foreach (GravSensorStuff sensor in _gravSensors)
                {
                    // The sensor is a source, so shouldn't have any links.  But it needs to be included in the args so that other
                    // neuron containers can hook to it
                    inputs.Add(new NeuralUtility.ContainerInput(sensor.Body.Token, sensor.Sensor, NeuronContainerType.Sensor, sensor.Sensor.Position, sensor.Sensor.Orientation, null, null, 0, null, null));
                }
            }

            if (_brains != null)
            {
                foreach (BrainStuff brain in _brains)
                {
                    int brainChemicalCount = 0;
                    if (brain.BrainStandard != null)
                    {
                        brainChemicalCount = Convert.ToInt32(Math.Round(brain.BrainStandard.BrainChemicalCount * 1.33d, 0));		// increasing so that there is a higher chance of listeners
                    }

                    inputs.Add(new NeuralUtility.ContainerInput
                    (
                        brain.Body.Token,
                        brain.Brain.container,
                        brain.BrainStandard != null ?
                            NeuronContainerType.Brain_Standalone :
                            NeuronContainerType.Brain_HasInternalNN,
                        brain.Brain.part.Position, brain.Brain.part.Orientation,
                        brain.BrainStandard != null ?
                            _itemOptions.Brain_LinksPerNeuron_Internal :
                            (double?)null,
                        new[]
                        {
                            (NeuronContainerType.Sensor, NeuralUtility.ExternalLinkRatioCalcType.Source, EXISTINGRATIOCAP),
                            (NeuronContainerType.Brain_HasInternalNN, NeuralUtility.ExternalLinkRatioCalcType.Source, EXISTINGRATIOCAP),
                            (NeuronContainerType.Brain_Standalone, NeuralUtility.ExternalLinkRatioCalcType.Average, _itemOptions.Brain_LinksPerNeuron_External_FromBrain),
                            (NeuronContainerType.Manipulator, NeuralUtility.ExternalLinkRatioCalcType.Source, EXISTINGRATIOCAP)       // not sure why this is here.  maybe if a manipulator also has some readable neurons?
                        },
                        brainChemicalCount,
                        brain.DNAInternalLinks,
                        brain.DNAExternalLinks
                    ));
                }
            }

            if (_thrusters != null)
            {
                foreach (var thrust in _thrusters)
                {
                    //NOTE: This won't be fed by other manipulators
                    inputs.Add(new NeuralUtility.ContainerInput
                    (
                        thrust.Body.Token,
                        thrust.Thrust, NeuronContainerType.Manipulator,
                        thrust.Thrust.Position, thrust.Thrust.Orientation,
                        null,
                        new[]       //NOTE: not mapping from a sensor to a manipulator.  signals must flow through a brain
                        {
                            (NeuronContainerType.Brain_HasInternalNN, NeuralUtility.ExternalLinkRatioCalcType.Source, EXISTINGRATIOCAP),
                            (NeuronContainerType.Brain_Standalone, NeuralUtility.ExternalLinkRatioCalcType.Source, EXISTINGRATIOCAP),
                        },
                        0,
                        null, thrust.DNAExternalLinks
                    ));
                }
            }

            if (_impulse != null)
            {
                foreach (var impulse in _impulse)
                {
                    inputs.Add(new NeuralUtility.ContainerInput
                    (
                        impulse.Body.Token,
                        impulse.Impulse, NeuronContainerType.Manipulator,
                        impulse.Impulse.Position, impulse.Impulse.Orientation,
                        null,
                        new[]
                        {
                            (NeuronContainerType.Brain_HasInternalNN, NeuralUtility.ExternalLinkRatioCalcType.Source, EXISTINGRATIOCAP),
                            (NeuronContainerType.Brain_Standalone, NeuralUtility.ExternalLinkRatioCalcType.Source, EXISTINGRATIOCAP),
                        },
                        0,
                        null, impulse.DNAExternalLinks
                    ));
                }
            }

            #endregion

            // Create links
            NeuralUtility.ContainerOutput[] outputs = null;
            if (inputs.Count > 0)
            {
                //outputs = NeuralUtility.LinkNeurons2(inputs.ToArray(), _itemOptions.NeuralLink_MaxWeight);
                outputs = NeuralUtility.LinkNeurons3(inputs.ToArray(), _itemOptions.NeuralLink_MaxWeight);
            }

            #region commit results

            ShowLinks(outputs);

            UpdateCountReport();

            if (chkBrainRunning.IsChecked.Value)
            {
                StartBrainOperation();
            }

            #endregion
        }

        // These are copied from BotConstructor
        private static BotConstruction_PartMap CreateLinks4_GetLinkMap(BotPartMapLinkDNA[] initial, PartBase[] allParts, ItemOptions itemOptions, ItemLinker_OverflowArgs overflowArgs, ItemLinker_ExtraArgs extraArgs)
        {
            if (initial == null)
            {
                initial = CreateLinks4_CreatePartMap(allParts, overflowArgs, extraArgs);
            }

            BotConstruction_PartMap map = CreateLinks4_AnalyzeMap(initial, allParts, itemOptions, extraArgs);


            // After many generations, links could drift too much.  May want to try to correct that (but the act of correcting could get
            // heavy handed and ruin a trend?)


            //TODO: Make sure all the parts are linked
            //map = EnsureAllPartsMapped(map, parts);

            //TODO: Call a delegate here

            //TODO: Analyze burdens

            //TODO: Adjust links



            return new BotConstruction_PartMap()
            {
                Map_DNA = map.Map_DNA,
                Map_Actual = map.Map_DNA.
                    Select(o =>
                    (
                        allParts.First(p => p.Position.IsNearValue(o.From)),
                        allParts.First(p => p.Position.IsNearValue(o.To)),
                        o.Weight
                    )).
                    ToArray(),
            };
        }
        private static BotPartMapLinkDNA[] CreateLinks4_CreatePartMap(PartBase[] allParts, ItemLinker_OverflowArgs overflowArgs, ItemLinker_ExtraArgs extraArgs)
        {
            INeuronContainer[] neuralParts = allParts.
                Where(o => o is INeuronContainer).
                Select(o => (INeuronContainer)o).
                ToArray();

            //NOTE: Some parts may have been set to none
            LinkItem[] brains = neuralParts.
                Where(o => o.NeuronContainerType == NeuronContainerType.Brain_HasInternalNN || o.NeuronContainerType == NeuronContainerType.Brain_Standalone).
                Select(o => new LinkItem(o.Position, o.Radius)).
                ToArray();

            LinkItem[] io = neuralParts.
                Where(o => o.NeuronContainerType == NeuronContainerType.Sensor || o.NeuronContainerType == NeuronContainerType.Manipulator).
                Select(o => new LinkItem(o.Position, o.Radius)).
                ToArray();

            Tuple<int, int>[] links = ItemLinker.Link_1_2(brains, io, overflowArgs, extraArgs);

            return links.
                Select(o => new BotPartMapLinkDNA()
                {
                    From = brains[o.Item1].Position,
                    To = io[o.Item2].Position,
                    Weight = 1d,
                }).
                ToArray();
        }
        private static BotConstruction_PartMap CreateLinks4_AnalyzeMap(BotPartMapLinkDNA[] map, PartBase[] allParts, ItemOptions itemOptions, ItemLinker_ExtraArgs extraLinks)
        {
            Point3D[] partPositions = allParts.
                Select(o => o.Position).
                ToArray();

            var initialLinkPoints = map.
                Select(o => Tuple.Create(o.From, o.To, o.Weight)).
                ToArray();

            // Figure out the max allowed links
            int maxFinalLinkCount = (CreateLinks4_GetEstimatedLinkCount(allParts, extraLinks) * itemOptions.PartMap_FuzzyLink_MaxLinkPercent).ToInt_Round();

            // If parts moved since the links were assigned, this will find the best matches for links
            var newLinks = ItemLinker.FuzzyLink(initialLinkPoints, partPositions, maxFinalLinkCount, itemOptions.PartMap_FuzzyLink_MaxIntermediateCount);

            return new BotConstruction_PartMap()
            {
                Map_DNA = newLinks.
                    Select(o => new BotPartMapLinkDNA()
                    {
                        From = partPositions[o.Item1],
                        To = partPositions[o.Item2],
                        Weight = o.Item3,
                    }).
                    ToArray(),
            };
        }
        private static double CreateLinks4_GetEstimatedLinkCount(PartBase[] parts, ItemLinker_ExtraArgs extraLinks)
        {
            // The ratio of brains to IO is a big influence on the estimate, so count the number of brains
            int numBrains = parts.Count(o => o is INeuronContainer oN && oN.NeuronContainerType.In(NeuronContainerType.Brain_HasInternalNN, NeuronContainerType.Brain_Standalone));
            double ratio = numBrains.ToDouble() / parts.Length.ToDouble();

            // Use the mesh to do a bicubic interpolation
            //double retVal = _linkEstimateMesh.Value.EstimateValue(ratio, parts.Length);
            double retVal = CreateLinks4_CreateLinkEstimateMesh().EstimateValue(ratio, parts.Length);

            // Average is possibly too low, Max is a bit high, so take the average of (avg, max)
            double percent = Math1D.Avg(extraLinks.Percents.Average(), extraLinks.Percents.Max());
            retVal *= 1d + percent;

            return retVal;
        }
        /// <summary>
        /// This was built from running this:
        /// MissileCommand0D.DelaunaySegmentEquation1b_Click()
        /// </summary>
        private static BezierMesh CreateLinks4_CreateLinkEstimateMesh()
        {
            double[] ratios = new double[] { 0, .1, .2, .3, .4, .5, .6, .7, .8, .9, 1 };        // x axis
            double[] pointCounts = new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 20, 26, 35, 47, 61, 77, 96, 118, 142, 168, 197, 229, 263, 300 };       // y axis
            double[] linkCounts = new double[]      // z values
                {
                    0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
                    0,  0,  0,  1,  1,  1,  1,  1,  1,  1,  1,
                    0,  0,  2,  2,  2,  2,  2,  2,  2,  3,  3,
                    0,  0,  3,  3,  3,  3,  3,  4,  4,  6,  6,
                    0,  0,  4,  4,  4,  4,  5,  7,  7,  7,  9.65,
                    0,  5,  5,  5,  5,  6,  8,  8,  10.5875,    10.7125,    13.65,
                    0,  6,  6,  6,  7,  9,  9,  11.6,   14.875,     15,     18.35,
                    0,  7,  7,  7,  8,  10,     12.7,   15.8875,    15.8625,    19.3125,    22.9875,
                    0,  8,  8,  9,  11,     11,     13.6375,    16.925,     20.2625,    24.15,  28.3125,
                    0,  9,  9,  10,     12,     14.725,     17.85,  21.4375,    25.2125,    29.4625,    33.3125,
                    0,  10,     10,     11,     13,     18.7375,    22.175,     26.35,  30.1625,    34.4125,    38.5875,
                    0,  11,     11,     14,     16.7375,    19.8,   23.225,     27.3875,    35.8,   39.7375,    43.9125,
                    0,  12,     13,     15,     17.775,     20.8875,    28.2375,    32.6,   36.3125,    45.5,   49.7,
                    0,  13,     14,     16,     21.9125,    25.2375,    29.0625,    37.275,     41.55,  51.1,   55.6125,
                    0,  14,     15,     17,     22.8875,    30.0625,    33.9125,    38.4375,    47.4875,    56.9,   61.3,
                    0,  15,     16,     20.65,  23.9625,    31.2625,    39.4875,    43.5,   52.9625,    57.75,  67.4375,
                    0,  19,     22,     27.7125,    35.4125,    43.325,     52.575,     61.8875,    70.9625,    81.325,     92.025,
                    0,  26,     30.7125,    41.3875,    49.2375,    62.7875,    77.5875,    87.5875,    102.7875,   112.9625,   130.1625,
                    0,  37,     46.2,   58.05,  77.1875,    96.7,   111.2375,   127.1625,   149.225,    170.65,     187.2375,
                    0,  51.725,     66.25,  88.4875,    113.325,    139.7,  160.75,     188.2125,   216.375,    239.225,    268.4125,
                    0,  68.775,     93.7875,    122.475,    154.125,    185.6875,   224.4375,   259.775,    294.8,  330.8125,   366.6625,
                    0,  92.0875,    123.45,     163.65,     206.8625,   246.5875,   293.4875,   338.775,    386.3125,   428.5,  476.425,
                    0,  119.3875,   162.0875,   216.4,  266.2375,   324.325,    381.125,    435.9,  496.625,    550.9375,   613.95,
                    0,  150.3875,   210.2125,   269.775,    338.0125,   409.425,    482.075,    554.5125,   623.5125,   696.275,    769.2875,
                    0,  183.4875,   256.6125,   340.5875,   422.725,    507.1125,   591.3125,   675.8625,   768.6375,   856.35,     942.5375,
                    0,  223.725,    315.6625,   407.325,    507.8375,   609.775,    715.7,  819.425,    921.65,     1026.95,    1130.8125,
                    0,  268.2125,   373.125,    490.2375,   608.125,    726.0375,   847.1875,   972.9625,   1100.25,    1219.45,    1345.4875,
                    0,  316.35,     443.4625,   581.4625,   720.925,    854.7875,   1000.775,   1144.6375,  1290.3,     1434.2875,  1584.5125,
                    0,  365.9875,   520.325,    675.4625,   834.6,  1003.275,   1164.6,     1328.7,     1492.9625,  1665.35,    1830.9875,
                    0,  424.6375,   598.35,     779.7,  963.8,  1151.2375,  1340.875,   1530.5,     1722.0375,  1914.4625,  2106.5375
                };

            return new BezierMesh(ratios, pointCounts, linkCounts);
        }

        private static double GetNeuronRadius(NeuralUtility.ContainerInput container)
        {
            double lengthSquared = container.Container.Neruons_All.
                Max(o => o.Position.ToVector().LengthSquared);

            return Math.Sqrt(lengthSquared);
        }

        private void UpdateGravity()
        {
            if (_gravityOrientationTrackball == null || trkGravityStrength == null)
            {
                return;
            }

            // The trackball's axis is along X
            _gravity = _gravityOrientationTrackball.Transform.Transform(new Vector3D(trkGravityStrength.Value, 0, 0));

            _gravityField.Gravity = _gravity;

            lblCurrentGravity.Text = Math.Round(_gravity.Length, 3).ToString();

            // Show the gravity in neuron space
            if (_gravSensors != null)
            {
                foreach (GravSensorStuff sensor in _gravSensors)
                {
                    sensor.Gravity.Clear();
                    sensor.Gravity.AddLine(sensor.Sensor.Position, sensor.Sensor.Position + _gravity);
                }
            }
        }

        private void UpdateCountReport()
        {
            int numInputs = 0;
            if (_gravSensors != null)
            {
                numInputs = _gravSensors.Sum(o => o.Neurons.Count);
            }

            int numBrains = 0;
            if (_brains != null)
            {
                numBrains = _brains.Sum(o => o.Neurons.Count);
            }

            int numManipulators = 0;
            if (_thrusters != null)
            {
                numManipulators = _thrusters.Sum(o => o.Neurons.Count);
            }

            int numImpulse = 0;
            if (_impulse != null)
            {
                numImpulse = _impulse.Sum(o => o.Neurons.Count);
            }

            int numLinks = 0;
            if (_links != null)
            {
                numLinks = _links.Outputs.Sum(o => (o.ExternalLinks == null ? 0 : o.ExternalLinks.Length) + (o.InternalLinks == null ? 0 : o.InternalLinks.Length));
            }

            lblInputNeuronCount.Text = numInputs.ToString("N0");
            lblBrainNeuronCount.Text = numBrains.ToString("N0");
            lblThrustNeuronCount.Text = numManipulators.ToString("N0");
            lblImpulseNeuronCount.Text = numImpulse.ToString("N0");
            lblTotalNeuronCount.Text = (numInputs + numBrains + numManipulators + numImpulse).ToString("N0");
            lblTotalLinkCount.Text = numLinks.ToString("N0");
        }

        private void StartBrainOperation()
        {
            // Make sure an existing operation isn't running
            CancelBrainOperation();

            if (_links == null)
            {
                // There are no links, so there is nothing to do
                return;
            }

            // Hand all the links to the worker
            NeuralBucket worker = new NeuralBucket(_links.Outputs.SelectMany(o => UtilityCore.Iterate(o.InternalLinks, o.ExternalLinks)).ToArray());

            _brainOperationCancel = new CancellationTokenSource();

            // Run this on an arbitrary thread
            _brainOperationTask = Task.Factory.StartNew(() =>
            {
                OperateBrain(worker, _brainOperationCancel.Token);
            }, _brainOperationCancel.Token);
        }
        private void CancelBrainOperation()
        {
            if (_brainOperationTask == null)
            {
                // There is nothing to cancel
                return;
            }

            // Initiate a cancel, and block until it's finished (it should finish very quickly)
            _brainOperationCancel.Cancel();
            try
            {
                _brainOperationTask.Wait();
            }
            catch (Exception) { }

            // Clean up
            _brainOperationTask.Dispose();
            _brainOperationCancel.Dispose();
            _brainOperationTask = null;
            _brainOperationCancel = null;
        }
        private static void OperateBrain(NeuralBucket worker, CancellationToken cancel)
        {
            //NOTE: This method is running on an arbitrary thread
            try
            {
                while (!cancel.IsCancellationRequested)
                {
                    worker.Tick();
                }
            }
            catch (Exception)
            {
                // Don't leak errors, just go away
            }
        }

        private void SetupGravityTrackball()
        {
            // major arrow along x
            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = TrackballGrabber.GetMajorArrow(Axis.X, true, _colors.TrackballAxisMajor, _colors.TrackballAxisSpecular);

            _viewportGravityRotate.Children.Add(visual);
            _gravityOrientationVisuals.Add(visual);

            // Create the trackball
            _gravityOrientationTrackball = new TrackballGrabber(grdGravityRotateViewport, _viewportGravityRotate, 1d, _colors.TrackballGrabberHoverLight);
            _gravityOrientationTrackball.SyncedLights.Add(_lightGravity1);
            _gravityOrientationTrackball.RotationChanged += new EventHandler(GravityOrientationTrackball_RotationChanged);

            // Faint lines
            _gravityOrientationTrackball.HoverVisuals.Add(TrackballGrabber.GetGuideLine(Axis.X, false, _colors.TrackballAxisLine));
            _gravityOrientationTrackball.HoverVisuals.Add(TrackballGrabber.GetGuideLineDouble(Axis.Y, _colors.TrackballAxisLine));
            _gravityOrientationTrackball.HoverVisuals.Add(TrackballGrabber.GetGuideLineDouble(Axis.Z, _colors.TrackballAxisLine));

            // The trackball's axis that is showing is X, but I want gravity to start out down
            _gravityOrientationTrackball.Transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), -90));
        }

        /// <summary>
        /// There's just enough logic here, that I wanted it in a private method.  The isInitialized check is a bit of a hack, that should be done
        /// by the caller, but it reduces total code
        /// </summary>
        private static double? UpdateSetting(bool isInitialized, TextBox textbox)
        {
            if (!isInitialized)
            {
                return null;
            }

            double? retVal = null;

            double newValue;
            if (double.TryParse(textbox.Text, out newValue))
            {
                retVal = newValue;
                textbox.Foreground = SystemColors.WindowTextBrush;
            }
            else
            {
                textbox.Foreground = Brushes.Red;
            }

            return retVal;
        }

        #endregion
        #region Private Methods - misc

        private static (int, int)[][] TestGetAllSets(int count)
        {
            (int, int)[][] log = UtilityCore.AllUniquePairSets(count).ToArray();

            string reportBasic2 = GetReport_basic(log);
            string reportHTML = GetReport_html(log);       // paste this directly into excel.  It won't wordwrap that way

            return log.ToArray();
        }

        private static string GetReport_basic(IEnumerable<(int, int)[]> sets)
        {
            int count = sets.First().Length;

            var grouped = sets.
                ToLookup(o => o[0].Item2).
                ToArray();

            StringBuilder retVal = new StringBuilder();

            foreach (var group in grouped)
            {
                for (int cntr = 0; cntr < count; cntr++)
                {
                    retVal.AppendLine(group.
                        Select(o => string.Format("{0} {1}", o[cntr].Item1, o[cntr].Item2)).
                        ToJoin("\t"));
                }

                retVal.AppendLine();
                retVal.AppendLine();
            }

            return retVal.ToString();
        }
        private static string GetReport_html(IEnumerable<(int, int)[]> sets)
        {
            int count = sets.First().Length;

            var grouped = sets.
                ToLookup(o => o[0].Item2).
                ToArray();

            StringBuilder retVal = new StringBuilder();

            retVal.AppendLine("<html>");
            retVal.AppendLine("<head/>");
            retVal.AppendLine("<body>");
            retVal.AppendLine("<table>");

            foreach (var group in grouped)
            {
                retVal.AppendLine("<tr>");

                for (int cntr = 0; cntr < count; cntr++)
                {
                    foreach (var item in group)
                    {
                        retVal.Append(GetReport_html_stripe(item, cntr, count));
                        retVal.AppendLine("<td width=\"10\"/>");
                    }

                    retVal.AppendLine("</tr>");
                    retVal.AppendLine("<tr height=\"10\"/>");
                }
            }

            retVal.AppendLine("</table>");
            retVal.AppendLine("</body>");
            retVal.AppendLine("</html>");

            return retVal.ToString();
        }
        private static string GetReport_html_stripe((int, int)[] items, int row, int count)
        {
            StringBuilder retVal = new StringBuilder();

            for (int cntr = 0; cntr < count; cntr++)
            {
                retVal.Append("<td");
                if (items.Any(o => o.Item1 == cntr && o.Item2 == row))
                {
                    retVal.Append(" style=\"background-color: #666\"");
                }
                retVal.Append(">");

                retVal.Append(cntr.ToString());
                retVal.Append(" ");
                retVal.Append(row.ToString());

                retVal.AppendLine("</td>");
            }

            return retVal.ToString();
        }

        #endregion
    }
}
