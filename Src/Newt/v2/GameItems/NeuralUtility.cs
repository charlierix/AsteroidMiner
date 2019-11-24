using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Controls3D;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Game.Newt.v2.GameItems
{
    //TODO: This class is a mess.  Once LinkNeurons2 is finished, call this NeuralUtility_OLD, and create a new one that only holds used code
    public static class NeuralUtility
    {
        #region enum: ExternalLinkRatioCalcType

        /// <summary>
        /// When calculating how many links to build between neuron containers, this tells what values to look at
        /// In other words take the number of neuron in which container(s) * some ratio
        /// </summary>
        public enum ExternalLinkRatioCalcType
        {
            /// <summary>
            /// Use the number of neurons from whichever container has fewer neurons
            /// </summary>
            Smallest,
            /// <summary>
            /// Use the number of neurons from whichever container has the most neurons
            /// </summary>
            Largest,
            /// <summary>
            /// Take the average of the two container's neurons
            /// </summary>
            Average,
            /// <summary>
            /// Use the number of neurons in the source container
            /// </summary>
            Source,
            /// <summary>
            /// Use the number of neurons in the destination container
            /// </summary>
            Destination
        }

        #endregion
        #region class: ContainerInput

        public class ContainerInput
        {
            public ContainerInput(long token, INeuronContainer container, NeuronContainerType containerType, Point3D position, Quaternion orientation, double? internalRatio, (NeuronContainerType type, ExternalLinkRatioCalcType calc, double mult)[] externalRatios, int brainChemicalCount, NeuralLinkDNA[] internalLinks, NeuralLinkExternalDNA[] externalLinks)
            {
                Token = token;
                Container = container;
                ContainerType = containerType;
                Position = position;
                Orientation = orientation;
                InternalRatio = internalRatio;
                ExternalRatios = externalRatios;
                BrainChemicalCount = brainChemicalCount;
                InternalLinks = internalLinks;
                ExternalLinks = externalLinks;
            }

            /// <summary>
            /// This should come from the PartBase
            /// </summary>
            public readonly long Token;

            public readonly INeuronContainer Container;
            public readonly NeuronContainerType ContainerType;

            public readonly Point3D Position;
            public readonly Quaternion Orientation;

            /// <summary>
            /// This is how many internal links to actually build.  It is calculated as the number of neurons * ratio
            /// </summary>
            public readonly double? InternalRatio;
            /// <summary>
            /// This is how many links should be built from the perspective of this container (how many listener links to create)
            /// It is broken down by container type
            /// </summary>
            //public readonly Tuple<NeuronContainerType, ExternalLinkRatioCalcType, double>[] ExternalRatios;
            public readonly (NeuronContainerType type, ExternalLinkRatioCalcType calc, double mult)[] ExternalRatios;

            /// <summary>
            /// This is how many brain chemicals this container can support
            /// </summary>
            /// <remarks>
            /// This is just used to calculate brain chemical listeners off of links.  So this value doesn't have to be the literal count:
            ///		It could stay zero if you don't want links to have brain chemical listeners.
            ///		It could be double the actual count, and links would end up with more listeners than needed.
            ///		etc
            ///		
            /// Also, when creating a link, it won't always have this number of listeners, it will have random up to this number.
            /// </remarks>
            public readonly int BrainChemicalCount;

            //NOTE: It's ok if either of these two are null.  These are just treated as suggestions
            public readonly NeuralLinkDNA[] InternalLinks;
            public readonly NeuralLinkExternalDNA[] ExternalLinks;

            // These are helper properties so the caller doesn't have a complex statement just to get at neurons
            public IEnumerable<INeuron> ReadableNeurons => Container.Neruons_ReadWrite.Concat(Container.Neruons_Readonly);
            public IEnumerable<INeuron> WritableNeurons => Container.Neruons_ReadWrite.Concat(Container.Neruons_Writeonly);

            public override string ToString()
            {
                return string.Format("{0} | {1}", ContainerType, Container?.GetType().Name ?? "<null>");
            }
        }

        #endregion
        #region class: ContainerOutput

        public class ContainerOutput
        {
            public ContainerOutput(INeuronContainer container, NeuralLink[] internalLinks, NeuralLink[] externalLinks)
            {
                this.Container = container;
                this.InternalLinks = internalLinks;
                this.ExternalLinks = externalLinks;
            }

            public readonly INeuronContainer Container;
            //public readonly Point3D Position;		// there should be no reason to store position
            public readonly NeuralLink[] InternalLinks;
            public readonly NeuralLink[] ExternalLinks;
        }

        #endregion

        #region class: LinkIndexed

        public class LinkIndexed
        {
            public LinkIndexed(int from, int to, double weight, double[] brainChemicalModifiers)
            {
                this.From = from;
                this.To = to;
                this.Weight = weight;
                this.BrainChemicalModifiers = brainChemicalModifiers;
            }

            public readonly int From;
            public readonly int To;
            public readonly double Weight;
            public readonly double[] BrainChemicalModifiers;
        }

        #endregion
        #region class: ContainerPoints

        private class ContainerPoints
        {
            public ContainerPoints(ContainerInput container, int maxLinks)
            {
                this.Container = container;
                _maxLinks = maxLinks;
                this.AllNeurons = container.Container.Neruons_All.ToArray();
                this.AllNeuronPositions = this.AllNeurons.Select(o => o.Position).ToArray();		//TODO: transform by orientation
            }

            public readonly ContainerInput Container;
            public readonly INeuron[] AllNeurons;
            public readonly Point3D[] AllNeuronPositions;

            private readonly int _maxLinks;
            private Dictionary<Point3D, ClosestExistingResult[]> _nearestPoints = new Dictionary<Point3D, ClosestExistingResult[]>();

            public ClosestExistingResult[] GetNearestPoints(Point3D position)
            {
                //TODO: transform position by orientation
                //TODO: Take orientation into account:  I think rotate each part's neurons by orientation

                if (!_nearestPoints.ContainsKey(position))
                {
                    _nearestPoints.Add(position, GetClosestExisting(position, this.AllNeuronPositions, _maxLinks));
                }

                return _nearestPoints[position];
            }
        }

        #endregion
        #region class: ClosestExistingResult

        private class ClosestExistingResult
        {
            public ClosestExistingResult(bool isExactMatch, int index, double percent)
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
        #region class: HighestPercentResult

        private class HighestPercentResult
        {
            public HighestPercentResult(ClosestExistingResult from, ClosestExistingResult to, double percent)
            {
                this.From = from;
                this.To = to;
                this.Percent = percent;
            }

            public readonly ClosestExistingResult From;
            public readonly ClosestExistingResult To;
            public readonly double Percent;
        }

        #endregion

        //NOTE: These classes contain lists because they will be destroyed as concrete links are chosen
        #region class: ContainerReads

        private class ContainerReads
        {
            /// <summary>
            /// This is the container that holds the readable neurons
            /// </summary>
            public ContainerInput Container { get; set; }

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
            public ContainerInput Container { get; set; }

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

            // Only one of these two will be non null (RemainingCandidates is used first, then when new random links need to be generated, RemainingCandidates_BySource will be used)
            /// <summary>
            /// These are readable neurons from ContainerReads and writable neurons from ContainerWrites
            /// </summary>
            /// <remarks>
            /// NOTE: There is one instance for every possible source-dest link
            /// </remarks>
            public List<SourceDestinationLink> RemainingCandidates { get; set; }
            /// <summary>
            /// This is RemainingCandidates, but grouped up (done as an optimization)
            /// </summary>
            public List<SourceDestinationLink[]> RemainingCandidates_BySource { get; set; }

            /// <summary>
            /// This removes any links that contain either of the neurons in the link passed in
            /// NOTE: This doesn't search for an exact "and" match, just an "or" match
            /// WARNING: This only looks at RemainingCandidates (not RemainingCandidates_BySource), so is only safe to use before RemainingCandidates_BySource is populated
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

            public ContainerInput SourceContainer { get; set; }
            public ContainerInput DestinationContainer { get; set; }

            public double DistanceContainers { get; set; }
            public double DistanceNeurons { get; set; }

            /// <summary>
            /// This is only used when the destination container is a manipulator.  This holds a distance to the nearest linked neuron.
            /// This is to help spread links evenly
            /// </summary>
            public double DestinationManipulatorWeight { get; set; }

            /// <summary>
            /// This instance is shared across all links to the particular destination neuron (even across multiple instances
            /// of ContainerWrites)
            /// </summary>
            public DestinationNeuronCount DestinationNeuronNumConnections { get; set; }

            public double? ExistingWeight { get; set; }

            public bool HasContainerMatch(ContainerInput container)
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
                if (ExistingWeight != null)
                {
                    // This link already has a weight.  Return that, ignore the max weight that was passed in
                    return ExistingWeight.Value;
                }

                // If execution gets here, this is a newly created link

                if (SourceContainer.ContainerType == NeuronContainerType.Brain_Standalone || DestinationContainer.ContainerType == NeuronContainerType.Brain_Standalone)
                {
                    // The standalone brain has no internal mechanism to adust weights, so the weights should be randomized at
                    // creation, and then slight mutations over many generations will select for the winning values
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

                return string.Format("{0} {1} - {2} {3} | {4} - {5} | {6} | {7}",
                    sourceType,
                    SourceContainer.Token,
                    destType,
                    DestinationContainer.Token,
                    //SourceNeuron.Position.ToStringSignificantDigits(2),
                    //DestinationNeuron.Position.ToStringSignificantDigits(2),
                    SourceNeuron.Token,
                    DestinationNeuron.Token,
                    DistanceNeurons.ToStringSignificantDigits(3),
                    DestinationManipulatorWeight.ToStringSignificantDigits(3));
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

        #region class: NeuronShell

        /// <summary>
        /// This could hold a spherical shell, circular ring, or endpoints of a line
        /// </summary>
        public class NeuronShell
        {
            public NeuronShell(Neuron_Direct[] neurons, double radius)
            {
                this.Neurons = neurons;
                this.Radius = radius;
                this.VectorsUnit = neurons.
                    Select(o => o.Position.ToVector() / radius).
                    ToArray();
            }

            public readonly Neuron_Direct[] Neurons;
            public readonly double Radius;

            public readonly Vector3D[] VectorsUnit;

            public Vector3D GetVector()
            {
                Vector3D retVal = new Vector3D(0, 0, 0);

                for (int cntr = 0; cntr < this.Neurons.Length; cntr++)
                {
                    retVal += this.VectorsUnit[cntr] * this.Neurons[cntr].Value;
                }

                return retVal;
            }
        }

        #endregion

        #region Link Neurons

        /// <summary>
        /// After creating your ship, this method will wire up neurons
        /// </summary>
        /// <remarks>
        /// This will create random links if the link dna is null.  Or it will use the link dna (and prune if there are too many)
        /// </remarks>
        /// <param name="maxWeight">This is only used when creating random links</param>
        public static ContainerOutput[] LinkNeurons(ContainerInput[] containers, double maxWeight)
        {
            //TODO: Take these as params (these are used when hooking up existing links)
            const int MAXINTERMEDIATELINKS = 3;
            const int MAXFINALLINKS = 3;

            NeuralLink[][] internalLinks, externalLinks;
            if (containers.Any(o => o.ExternalLinks != null || o.InternalLinks != null))
            {
                internalLinks = BuildInternalLinks_Existing(containers, MAXINTERMEDIATELINKS, MAXFINALLINKS);
                externalLinks = BuildExternalLinksExisting(containers, MAXINTERMEDIATELINKS, MAXFINALLINKS);

                internalLinks = CapWeights(internalLinks, maxWeight);
                externalLinks = CapWeights(externalLinks, maxWeight);
            }
            else
            {
                internalLinks = BuildInternalLinks_Random(containers, maxWeight);
                externalLinks = BuildExternalLinks_Random(containers, maxWeight);
            }

            // Build the return
            ContainerOutput[] retVal = new ContainerOutput[containers.Length];
            for (int cntr = 0; cntr < containers.Length; cntr++)
            {
                retVal[cntr] = new ContainerOutput(containers[cntr].Container, internalLinks[cntr], externalLinks[cntr]);
            }

            return retVal;
        }
        /// <summary>
        /// This overload takes a map that tells which parts can link to which
        /// </summary>
        public static ContainerOutput[] LinkNeurons(BotConstruction_PartMap partMap, ContainerInput[] containers, double maxWeight)
        {
            //TODO: Take these as params (these are used when hooking up existing links)
            const int MAXINTERMEDIATELINKS = 3;
            const int MAXFINALLINKS = 3;

            NeuralLink[][] internalLinks, externalLinks;
            if (containers.Any(o => o.ExternalLinks != null || o.InternalLinks != null))
            {
                internalLinks = BuildInternalLinks_Existing(containers, MAXINTERMEDIATELINKS, MAXFINALLINKS);
                externalLinks = BuildExternalLinksExisting(containers, MAXINTERMEDIATELINKS, MAXFINALLINKS);        //NOTE: The partMap isn't needed for existing links.  It is just to help figure out new random links

                internalLinks = CapWeights(internalLinks, maxWeight);
                externalLinks = CapWeights(externalLinks, maxWeight);
            }
            else
            {
                internalLinks = BuildInternalLinks_Random(containers, maxWeight);
                externalLinks = BuildExternalLinks_Random(partMap, containers, maxWeight);
            }

            // Build the return
            ContainerOutput[] retVal = new ContainerOutput[containers.Length];
            for (int cntr = 0; cntr < containers.Length; cntr++)
            {
                retVal[cntr] = new ContainerOutput(containers[cntr].Container, internalLinks[cntr], externalLinks[cntr]);
            }

            return retVal;
        }

        public static ContainerOutput[] LinkNeurons3(ContainerInput[] containers, double maxWeight, double extraPercent = 1)
        {
            //TODO: Take these as params (these are used when hooking up existing links)
            const int MAXINTERMEDIATELINKS = 3;
            const int MAXFINALLINKS = 3;

            //NOTE: This method is named in a way that is generic, but in actuality, this method is written for sensors -> brains -> manipulators

            Random rand = StaticRandom.GetRandomForThread();

            #region link containers

            //TODO: Some or all of this should be passed in as a param

            // Prep for the linkers
            ItemLinker_OverflowArgs overflow = new ItemLinker_OverflowArgs();
            ItemLinker_ExtraArgs extra = new ItemLinker_ExtraArgs()
            {
                Percents = new[] { extraPercent, 0d },     // manipulators should only be mapped to one brain
            };
            ItemLinker_CombineArgs combineArgs = new ItemLinker_CombineArgs();

            // Split out the inputs into their own arrays so that the later indices make sense
            ContainerInput[] items_brain = containers.
                Where(o => o.ContainerType.In(NeuronContainerType.Brain_HasInternalNN, NeuronContainerType.Brain_Standalone)).
                ToArray();

            ContainerInput[] items_io = containers.
                Where(o => o.ContainerType.In(NeuronContainerType.Sensor, NeuronContainerType.Manipulator)).
                ToArray();

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


            int brainCount = items_brain.Sum(o => o.WritableNeurons.Count());
            int inputCount = items_io.Where(o => o.ContainerType == NeuronContainerType.Sensor).Sum(o => o.ReadableNeurons.Count());

            // Link brains to io
            var links_brain_io = ItemLinker.Link_1_2(linkItems_brain, linkItems_io, overflow, extra);

            // Link brains to brains
            ItemLinker.Link_Self(out _, out var links_brain, linkItems_brain, combineArgs);

            #endregion

            List<ContainerReads> sourceContainers = GetPossibleMappings(items_brain, items_io, links_brain_io, links_brain);
            List<SourceDestinationLink> links = new List<SourceDestinationLink>();

            NeuralLink[][] internalLinks = null;
            if (containers.Any(o => o.ExternalLinks != null || o.InternalLinks != null))
            {
                // There are already links in the dna.  Convert these into actual links (dna only holds positions and weights.  The bot could
                // have mutated from the parent, so positions could have shifted.  These functions wire up concrete neurons)
                internalLinks = BuildInternalLinks_Existing(containers, MAXINTERMEDIATELINKS, MAXFINALLINKS);
                NeuralLink[][] externalLinks = BuildExternalLinksExisting(containers, MAXINTERMEDIATELINKS, MAXFINALLINKS);

                internalLinks = CapWeights(internalLinks, maxWeight);
                externalLinks = CapWeights(externalLinks, maxWeight);

                // Store these in sourceContainers and links
                StoreExistingLinks(externalLinks, sourceContainers, links);
            }

            // Group by source
            //NOTE: This must be done after the existing dna links are wired up.  BuildExternalLinks3_NextLink uses RemainingCandidates_BySource, but the
            //above wiring uses RemainingCandidates
            foreach (ContainerReads read in sourceContainers)
            {
                foreach (ContainerWrites write in read.RemainingCandidates)
                {
                    write.RemainingCandidates_BySource = write.RemainingCandidates.
                        ToLookup(o => o.SourceNeuron.Token).        // might want to store the source container as well
                        Select(o => o.ToArray()).
                        ToList();

                    write.RemainingCandidates = null;       // set it to null so that any code below this point will get a null reference exception if they use this instead of the groups
                }
            }

            // Keep going until all input neurons are linked
            while (sourceContainers.Count > 0)
            {
                BuildExternalLinks3_NextLink(sourceContainers, links, rand);
            }

            #region build final

            List<ContainerOutput> final = new List<ContainerOutput>();

            var linksByDestinationContainer = links.
                ToLookup(o => o.DestinationContainer.Token);

            foreach (var linkSet in linksByDestinationContainer)
            {
                ContainerInput containerInput = linkSet.First().DestinationContainer;
                INeuronContainer container = containerInput.Container;

                // External Links
                NeuralLink[] externalLinks = linkSet.
                    Select(o => new NeuralLink(o.SourceContainer.Container, o.DestinationContainer.Container, o.SourceNeuron, o.DestinationNeuron, o.GetLinkWeight(maxWeight), null)).
                    ToArray();

                // Internal Links
                int index = containers.IndexOf(containerInput, (o1, o2) => o1.Token == o2.Token);

                NeuralLink[] internalLinks_local = internalLinks?[index];
                if (containerInput.InternalRatio != null && internalLinks_local == null)
                {
                    internalLinks_local = BuildInternalLinks_Random(new[] { containerInput }, maxWeight)[0];
                }

                final.Add(new ContainerOutput(container, internalLinks_local, externalLinks));
            }

            // Pick up any brains that don't have external links going to them
            final.AddRange(containers.
                Where(o => o.InternalRatio != null).
                Where(o => !linksByDestinationContainer.Any(p => p.First().DestinationContainer.Token == o.Token)).
                Select(o => new ContainerOutput(o.Container, BuildInternalLinks_Random(new[] { o }, maxWeight)[0], null)));

            #endregion

            return final.ToArray();
        }

        /// <summary>
        /// This is used when saving to DNA.
        /// Individual parts don't hold links, so when you call PartBase.GetNewDNA, the links will always be null.
        /// This populates dna.InternalLinks and dna.ExternalLinks based on the links stored in outputs.
        /// </summary>
        /// <param name="dna">This is the dna to populate</param>
        /// <param name="dnaSource">This is the container that the dna came from</param>
        /// <param name="outputs">This is all of the containers, and their links</param>
        public static void PopulateDNALinks(ShipPartDNA dna, INeuronContainer dnaSource, IEnumerable<ContainerOutput> outputs)
        {
            // Find the output for the source passed in
            ContainerOutput output = outputs.Where(o => o.Container == dnaSource).FirstOrDefault();
            if (output == null)
            {
                return;
            }

            // Internal
            dna.InternalLinks = null;
            if (output.InternalLinks != null)
            {
                dna.InternalLinks = output.InternalLinks.Select(o => new NeuralLinkDNA()
                {
                    From = o.From.Position,
                    To = o.To.Position,
                    Weight = o.Weight,
                    BrainChemicalModifiers = o.BrainChemicalModifiers == null ? null : o.BrainChemicalModifiers.ToArray()       // using ToArray to make a copy
                }).ToArray();
            }

            // External
            dna.ExternalLinks = null;
            if (output.ExternalLinks != null)
            {
                dna.ExternalLinks = output.ExternalLinks.Select(o => new NeuralLinkExternalDNA()
                {
                    FromContainerPosition = o.FromContainer.Position,
                    FromContainerOrientation = o.FromContainer.Orientation,
                    From = o.From.Position,
                    To = o.To.Position,
                    Weight = o.Weight,
                    BrainChemicalModifiers = o.BrainChemicalModifiers == null ? null : o.BrainChemicalModifiers.ToArray()       // using ToArray to make a copy
                }).ToArray();
            }
        }

        #endregion

        #region Neuron Arrangements

        private const int EVENDIST_STOPCOUNT = 150;     // the default is 1000, but these don't need to be that accurate

        /// <summary>
        /// Turns a list of points into sets of points based on their radius
        /// </summary>
        /// <remarks>
        /// Neruons are stored in DNA as a list of points.  But some parts give different meaning to neurons based on what
        /// shell they are in.  For direction controllers:
        ///     Translation may be a shell that is radius .4
        ///     Rotation would be a shell at radius 1
        ///     
        /// So when dna is null, the neurons are layed out evenly distributed in shells.  But when a child is converted to dna
        /// and mutated, neurons could move around, get added or removed
        /// 
        /// This function does two passes.  The first is to see which neurons are closest to which radius
        /// 
        /// The second pass pulls neurons from shells with too many to shells with too few
        /// 
        /// NOTE: It's ok if there are too few or too many neurons for all the shells.  This function stops when it can't do any more
        ///     ex: 100 points passed in, 3 shells of 30 are needed.  Some of the shells will have more than 30, but none will have fewer
        ///     ex: 25 points passed in, 3 shells of 10 are needed.  Some or one of the shells will have fewer than 10, but none will have more
        /// </remarks>
        /// <param name="points">The 1D list of points that need to be divided into shells</param>
        /// <param name="radii">
        /// How many shells there are and what radius each is
        /// NOTE: If the getRadius delegate is passed in, these may not actually be radius, but some sort of distance
        /// </param>
        /// <param name="countPerSet">How many neurons should be in each shell</param>
        /// <param name="getRadius">
        /// This gives the caller an opportunity to define their own "distance from set" function.  If neurons are arranged in lines instead of
        /// rings or spheres, then this could return distance from a line, or an arrangement of planes, or whatever
        /// 
        /// If null, this will default to distance from origin (radius)
        /// </param>
        /// <returns>
        /// Same number of elements as radii.  Each set corresponds to the points for each radius
        /// </returns>
        public static Point3D[][] DivideNeuronShellsByRadius(Point3D[] points, double[] radii, int countPerSet, Func<Point3D, double> getRadius = null)
        {
            #region divide by radius

            if (getRadius == null)
            {
                getRadius = new Func<Point3D, double>(p => p.ToVector().Length);
            }

            // Figure out how close each point is to the radii
            var itemsByRadius = points.
                Select((o, i) =>
                {
                    double radius = getRadius(o);
                    var distances = radii.
                        Select((p, j) => (index: j, dist: Math.Abs(p - radius))).
                        OrderBy(p => p.dist).
                        ToArray();

                    return (index: i, point: o, distances);
                }).
                ToArray();

            // Put the closest to each radius in that radius's set
            var sets1 = itemsByRadius.
                ToLookup(o => o.distances[0].index).
                ToArray();

            var sets2 = Enumerable.Range(0, radii.Length).
                Select(o => sets1.FirstOrDefault(p => p.Key == o)).
                Select(o => o?.ToList() ?? new List<(int index, Point3D point, (int index, double dist)[] distances)>()).
                Select(o => o.
                    Select(p => new
                    {
                        p.index,
                        p.point,
                        distances = p.distances.
                            OrderBy(q => q.index).      // put them back in order of index
                            Select(q => q.dist).        // since they are in order, there's no need to remember index anymore (distance[0].index is 0, distances[1].index is 1, etc).  This makes the code lower down easier
                            ToArray(),
                    }).
                    ToList()).      // it's a list, because the next pass of this function will move items from one shell to another
                ToArray();

            #endregion

            #region redistribute

            //Color[] colors = UtilityWPF.GetRandomColors(sets2.Length, 100, 200);
            //DivideNeuronShellsByRadius_DrawPointShells(sets2.Select(o => o.Select(p => p.point).ToArray()).ToArray(), colors, radii, "before");

            // Instead of passing a complex tuple structure into various functions, just pass a delegate that can give number of elements for an index
            var getCount = new Func<int, int>(i => sets2[i].Count);

            // Each iteration of this loop will move one neuron from a shell with too many toward a shell with too few
            while (true)
            {
                if (sets2.All(o => o.Count <= countPerSet) ||       // there are no sets with enough to give up
                    sets2.All(o => o.Count >= countPerSet))     // none of the sets are undersized
                {
                    break;
                }

                // A sink and source could be separated by a layer of proper count, or a source could have a sink on either side of it (or sink with
                // a source on either side), or other odd scenarios.  So forces will have requests radiate out from sinks until a source is found
                //
                // Negative values will mean move down one shell, and positive is move up one
                //
                // Forces of shells that aren't sources have no meaning
                int?[] forces = new int?[sets2.Length];

                for (int cntr = 0; cntr < sets2.Length; cntr++)
                {
                    if (sets2[cntr].Count < countPerSet)
                    {
                        // Found a sink.  Send requests up and down until sources are found
                        DivideNeuronShellsByRadius_AdjustForces(forces, cntr, sets2.Length, countPerSet, getCount);
                    }
                }

                // Now find the source with the most demand
                int pushIndex = DivideNeuronShellsByRadius_GetPushSourceIndex(forces, countPerSet, getCount);
                int pushDirection = DivideNeuronShellsByRadius_GetPushDirection(forces[pushIndex].Value);

                // Find the neuron that is closest to the destination set
                var closest = sets2[pushIndex].
                    Select((o, i) => new
                    {
                        index_sets2 = i,
                        item = o,
                        dist = o.distances[pushIndex + pushDirection],
                    }).
                    OrderBy(o => o.dist).
                    First();

                // Move the neuron
                sets2[pushIndex + pushDirection].Add(closest.item);
                sets2[pushIndex].RemoveAt(closest.index_sets2);
            }

            //DivideNeuronShellsByRadius_DrawPointShells(sets2.Select(o => o.Select(p => p.point).ToArray()).ToArray(), colors, radii, "after");

            #endregion

            return sets2.
                Select(o => o.Select(p => p.point).ToArray()).
                ToArray();
        }
        public static Point3D[][] DivideNeuronLayersIntoSheets(Point3D[] points, double[] layerZs, int countPerSet)
        {
            return DivideNeuronShellsByRadius(points, layerZs, countPerSet, o => o.Z);
        }

        public static Vector3D[] GetNeuronPositions_Spherical_Cluster(Point3D[] dnaPositions, int count, double radius, double minClusterDistPercent)
        {
            return GetNeuronPositions_Spherical_Cluster(dnaPositions, null, 1d, count, radius, minClusterDistPercent);
        }
        public static Vector3D[] GetNeuronPositions_Spherical_Cluster(Point3D[] dnaPositions, Point3D[] existingStaticPositions, double staticMultValue, int count, double radius, double minClusterDistPercent)
        {
            // Get the points passed in, or random points (capped to count)
            Vector3D[] retVal = GetNeuronPositionsInitial
            (
                out Vector3D[] staticPoints,
                out double[] staticRepulseMult,
                dnaPositions,
                existingStaticPositions,
                staticMultValue,
                count,
                radius,
                o => Math3D.GetRandomVector_Spherical(o)
            );

            // Don't let them get too close to each other
            return Math3D.GetRandomVectors_Spherical_ClusteredMinDist
            (
                retVal,
                radius,
                radius * 2d * minClusterDistPercent,        //clustdist% is of diameter, not radius
                stopIterationCount: EVENDIST_STOPCOUNT,
                existingStaticPoints: staticPoints,
                staticRepulseMultipliers: staticRepulseMult
            );
        }

        public static Vector3D[] GetNeuronPositions_Spherical_Even(Point3D[] dnaPositions, int count, double radius)
        {
            return GetNeuronPositions_Spherical_Even(dnaPositions, null, 1d, count, radius);
        }
        public static Vector3D[] GetNeuronPositions_Spherical_Even(Point3D[] dnaPositions, Point3D[] existingStaticPositions, double staticMultValue, int count, double radius)
        {
            // Get the points passed in, or random points (capped to count)
            Vector3D[] retVal = GetNeuronPositionsInitial
            (
                out Vector3D[] staticPoints,
                out double[] staticRepulseMult,
                dnaPositions,
                existingStaticPositions,
                staticMultValue,
                count,
                radius,
                o => Math3D.GetRandomVector_Spherical(o)
            );

            // Space them out evenly
            return Math3D.GetRandomVectors_Spherical_EvenDist
            (
                retVal,
                radius,
                stopIterationCount: EVENDIST_STOPCOUNT,
                existingStaticPoints: staticPoints,
                staticRepulseMultipliers: staticRepulseMult
            );
        }

        public static Vector3D[] GetNeuronPositions_SphericalShell_Even(Point3D[] dnaPositions, int count, double radius)
        {
            return GetNeuronPositions_SphericalShell_Even(dnaPositions, null, 1d, count, radius);
        }
        public static Vector3D[] GetNeuronPositions_SphericalShell_Even(Point3D[] dnaPositions, Point3D[] existingStaticPositions, double staticMultValue, int count, double radius)
        {
            // Get the points passed in, or random points (capped to count)
            Vector3D[] retVal = GetNeuronPositionsInitial
            (
                out Vector3D[] staticPoints,
                out double[] staticRepulseMult,
                dnaPositions,
                existingStaticPositions,
                staticMultValue,
                count,
                radius,
                o => Math3D.GetRandomVector_Spherical_Shell(o)
            );

            // Space them out evenly
            return Math3D.GetRandomVectors_SphericalShell_EvenDist
            (
                retVal,
                radius,
                stopIterationCount: EVENDIST_STOPCOUNT,
                existingStaticPoints: staticPoints,
                staticRepulseMultipliers: staticRepulseMult
            );
        }

        public static Vector3D[] GetNeuronPositions_Circular_Even(Point3D[] dnaPositions, int count, double radius, double z = 0)
        {
            return GetNeuronPositions_Circular_Even(dnaPositions, null, 1d, count, radius, z);
        }
        public static Vector3D[] GetNeuronPositions_Circular_Even(Point3D[] dnaPositions, Point3D[] existingStaticPositions, double staticMultValue, int count, double radius, double z = 0)
        {
            // Get the points passed in, or random points (capped to count)
            Vector3D[] retVal = GetNeuronPositionsInitial
            (
                out Vector3D[] staticPoints,
                out double[] staticRepulseMult,
                dnaPositions,
                existingStaticPositions,
                staticMultValue,
                count,
                radius,
                o => Math3D.GetRandomVector_Circular(o)
            );

            // Convert the 3D points to 2D
            Vector[] retVal2D = retVal.
                Select(o => o.ToVector2D()).
                ToArray();

            Vector[] staticPoints2D = staticPoints?.        // could be null (actually, most likely null)
                Select(o => o.ToVector2D()).
                ToArray();

            // Space them out evenly
            retVal2D = Math3D.GetRandomVectors_Circular_EvenDist
            (
                retVal2D,
                radius,
                stopIterationCount: EVENDIST_STOPCOUNT,
                existingStaticPoints: staticPoints2D,
                staticRepulseMultipliers: staticRepulseMult
            );

            return retVal2D.
                Select(o => o.ToVector3D(z)).
                ToArray();
        }

        public static Vector3D[] GetNeuronPositions_CircularShell_Even(Point3D[] dnaPositions, int count, double radius, double z = 0)
        {
            // Get the points passed in, or random points (capped to count)
            Vector3D[] retVal = GetNeuronPositionsInitial
            (
                out Vector3D[] staticPoints,
                out double[] staticRepulseMult,
                dnaPositions,
                null,
                1,
                count,
                radius, o => Math3D.GetRandomVector_Circular_Shell(o)
            );

            // Convert the 3D points to 2D
            Vector[] retVal2D = retVal.
                Select(o => o.ToVector2D()).
                ToArray();

            Vector[] staticPoints2D = staticPoints?.
                Select(o => o.ToVector2D()).
                ToArray();

            // Space them out evenly
            retVal2D = Math3D.GetRandomVectors_CircularRing_EvenDist
            (
                retVal2D,
                radius,
                stopRadiusPercent: .001,
                stopIterationCount: EVENDIST_STOPCOUNT,
                existingStaticPoints: staticPoints2D,
                staticRepulseMultipliers: staticRepulseMult
            );

            return retVal2D.
                Select(o => o.ToVector3D(z)).
                ToArray();
        }

        public static Vector3D[] GetNeuronPositions_Line_Even(Point3D[] dnaPositions, int count, double radius, double y = 0, double z = 0)
        {
            // It doesn't matter what the old positions were, this evenly distributes across a line
            if (count < 1)
            {
                return new Vector3D[0];
            }
            else if (count == 1)
            {
                return new[] { new Vector3D(0, y, z) };
            }

            double step = (radius * 2) / (count - 1);
            double start = -radius;

            return Enumerable.Range(0, count).
                Select(o => new Vector3D(start + (step * o), y, z)).
                ToArray();
        }

        public static NeuronShell CreateNeuronShell_Sphere(double radius, int count)
        {
            Vector3D[] positions = Math3D.GetRandomVectors_SphericalShell_EvenDist(count, radius, stopIterationCount: EVENDIST_STOPCOUNT);

            Neuron_Direct[] neurons = positions.
                Select(o => new Neuron_Direct(o.ToPoint(), true)).
                ToArray();

            return new NeuronShell(neurons, radius);
        }
        public static NeuronShell CreateNeuronShell_Sphere(Point3D[] existing, double radius, int count)
        {
            Vector3D[] movable;

            if (existing.Length < count)
            {
                // Not enough, add some random points
                movable = existing.
                    Select(o => o.ToVector()).
                    Concat
                    (
                        Enumerable.Range(0, count - existing.Length).
                        Select(o => Math3D.GetRandomVector_Spherical_Shell(radius))
                    ).
                    ToArray();
            }
            else if (existing.Length > count)
            {
                // Too many, remove some random points
                movable = UtilityCore.RandomRange(0, existing.Length, count).
                    Select(o => existing[o].ToVector()).
                    ToArray();
            }
            else //if (existing.Length == count)
            {
                // Just right
                movable = existing.
                    Select(o => o.ToVector()).
                    ToArray();
            }

            // Make sure they are evenly distributed
            movable = Math3D.GetRandomVectors_SphericalShell_EvenDist(movable, radius, stopIterationCount: EVENDIST_STOPCOUNT);

            // Convert these points into neurons
            Neuron_Direct[] neurons = movable.
                Select(o => new Neuron_Direct(o.ToPoint(), true)).
                ToArray();

            return new NeuronShell(neurons, radius);
        }
        public static NeuronShell CreateNeuronShell_Ring(double radius, int count, ITriangle plane = null)
        {
            Vector[] positions2D = Math3D.GetRandomVectors_CircularRing_EvenDist(count, radius, stopIterationCount: EVENDIST_STOPCOUNT);

            IEnumerable<Vector3D> positions3D;
            if (plane == null)
            {
                positions3D = positions2D.
                    Select(o => o.ToVector3D());
            }
            else
            {
                RotateTransform3D transform = new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRotation(new Vector3D(0, 0, 1), plane.Normal)));

                positions3D = positions2D.
                    Select(o => transform.Transform(o.ToVector3D()));
            }

            Neuron_Direct[] neurons = positions3D.
                Select(o => new Neuron_Direct(o.ToPoint(), true)).
                ToArray();

            return new NeuronShell(neurons, radius);
        }
        public static NeuronShell CreateNeuronShell_Line(double radius, Vector3D? line = null)
        {
            Vector3D lineUnit;

            if (line != null)
            {
                lineUnit = line.Value.ToUnit(true);
                if (Math3D.IsInvalid(lineUnit))
                {
                    lineUnit = new Vector3D(0, 0, 1);
                }
            }
            else
            {
                lineUnit = new Vector3D(0, 0, 1);
            }

            Neuron_Direct[] neurons = new[]
            {
                new Neuron_Direct((lineUnit * radius).ToPoint(), true),
                new Neuron_Direct((-lineUnit * radius).ToPoint(), true),
            };

            return new NeuronShell(neurons, radius);
        }

        #endregion

        #region Private Methods - internal links

        /// <summary>
        /// This builds random links between neurons that are all in the same container
        /// </summary>
        private static NeuralLink[][] BuildInternalLinks_Random(ContainerInput[] containers, double maxWeight)
        {
            NeuralLink[][] retVal = new NeuralLink[containers.Length][];

            for (int cntr = 0; cntr < containers.Length; cntr++)
            {
                ContainerInput container = containers[cntr];

                if (container.InternalRatio == null)
                {
                    // If ratio is null, that means to not build any links
                    retVal[cntr] = null;
                    continue;
                }

                #region Separate into buckets

                // Separate into readable and writeable buckets
                List<INeuron> readable = new List<INeuron>(container.Container.Neruons_Readonly);
                int[] readonlyIndices = Enumerable.Range(0, readable.Count).ToArray();

                List<INeuron> writeable = new List<INeuron>(container.Container.Neruons_Writeonly);
                int[] writeonlyIndices = Enumerable.Range(0, writeable.Count).ToArray();

                SortedList<int, int> readwritePairs = new SortedList<int, int>();		// storing illegal pairs so that neurons can't link to themselves (I don't know if they can in real life.  That's a tough term to search for, google gave far too generic answers)
                foreach (INeuron neuron in container.Container.Neruons_ReadWrite)
                {
                    readwritePairs.Add(readable.Count, writeable.Count);
                    readable.Add(neuron);
                    writeable.Add(neuron);
                }

                #endregion

                // Figure out how many to make
                int smallerNeuronCount = Math.Min(readable.Count, writeable.Count);
                int count = Convert.ToInt32(Math.Round(container.InternalRatio.Value * smallerNeuronCount));
                if (count == 0)
                {
                    // There are no links to create
                    retVal[cntr] = null;
                }

                // Create Random
                LinkIndexed[] links = GetRandomLinks(readable.Count, writeable.Count, count, readonlyIndices, writeonlyIndices, readwritePairs, maxWeight).		// get links
                    Select(o => new LinkIndexed(o.Item1, o.Item2, o.Item3, GetBrainChemicalModifiers(container, maxWeight))).       // tack on the brain chemical receptors
                    ToArray();

                // Exit Function
                retVal[cntr] = links.
                    Select(o => new NeuralLink(container.Container, container.Container, readable[o.From], writeable[o.To], o.Weight, o.BrainChemicalModifiers)).
                    ToArray();
            }

            // Exit Function
            return retVal;
        }

        /// <summary>
        /// This builds links between neurons that are all in the same container, based on exising links
        /// </summary>
        private static NeuralLink[][] BuildInternalLinks_Existing(ContainerInput[] containers, int maxIntermediateLinks, int maxFinalLinks)
        {
            NeuralLink[][] retVal = new NeuralLink[containers.Length][];

            for (int cntr = 0; cntr < containers.Length; cntr++)
            {
                ContainerInput container = containers[cntr];

                if (container.InternalLinks == null || container.InternalLinks.Length == 0)
                {
                    // There are no existing internal links
                    retVal[cntr] = null;
                    continue;
                }

                //TODO: May want to use readable/writable instead of all (doing this first because it's easier).  Also, the ratios are good suggestions
                //for creating a good random brain, but from there, maybe the rules should be relaxed?
                INeuron[] allNeurons = container.Container.Neruons_All.ToArray();

                int count = Convert.ToInt32(Math.Round(container.InternalRatio.Value * allNeurons.Length));
                if (count == 0)
                {
                    retVal[cntr] = null;
                    continue;
                }

                // All the real work is done in this method
                LinkIndexed[] links = BuildInternalLinksExisting_Continue(allNeurons, count, container.InternalLinks, container.BrainChemicalCount, maxIntermediateLinks, maxFinalLinks);

                // Exit Function
                retVal[cntr] = links.Select(o => new NeuralLink(container.Container, container.Container, allNeurons[o.From], allNeurons[o.To], o.Weight, o.BrainChemicalModifiers)).ToArray();
            }

            // Exit Function
            return retVal;
        }

        /// <summary>
        /// This does a fuzzy match to find the best links it can
        /// </summary>
        /// <remarks>
        /// The existing links passed in point to where neurons used to be.  This method compares those positions with where
        /// neurons are now, and tries to match up with the closest it can
        /// 
        /// NOTE: Both neurons and links could have been mutated independent of each other.  This method doesn't care how
        /// they got the way they are, it just goes by position
        /// </remarks>
        private static LinkIndexed[] BuildInternalLinksExisting_Continue(IEnumerable<INeuron> neurons, int count, NeuralLinkDNA[] existing, int maxBrainChemicals, int maxIntermediateLinks, int maxFinalLinks)
        {
            NeuralLinkDNA[] existingPruned = existing;
            if (existing.Length > count)
            {
                // Prune without distributing weight (if this isn't done here, then the prune at the bottom of this method will
                // artificially inflate weights with the links that this step is removing)
                existingPruned = existing.OrderByDescending(o => Math.Abs(o.Weight)).Take(count).ToArray();
            }

            Point3D[] allPoints = neurons.Select(o => o.Position).ToArray();

            #region Find closest points

            // Get a unique list of points
            Dictionary<Point3D, ClosestExistingResult[]> resultsByPoint = new Dictionary<Point3D, ClosestExistingResult[]>();		// can't use SortedList, because point isn't sortable (probably doesn't have IComparable)
            foreach (var exist in existingPruned)
            {
                if (!resultsByPoint.ContainsKey(exist.From))
                {
                    resultsByPoint.Add(exist.From, GetClosestExisting(exist.From, allPoints, maxIntermediateLinks));
                }

                if (!resultsByPoint.ContainsKey(exist.To))
                {
                    resultsByPoint.Add(exist.To, GetClosestExisting(exist.To, allPoints, maxIntermediateLinks));
                }
            }

            #endregion

            List<LinkIndexed> retVal = new List<LinkIndexed>();

            #region Build links

            foreach (var exist in existingPruned)
            {
                HighestPercentResult[] links = GetHighestPercent(resultsByPoint[exist.From], resultsByPoint[exist.To], maxFinalLinks, true);

                foreach (HighestPercentResult link in links)
                {
                    double[] brainChemicals = null;
                    if (exist.BrainChemicalModifiers != null)
                    {
                        brainChemicals = exist.BrainChemicalModifiers.
                            Take(maxBrainChemicals).		// if there are more, just drop them
                            Select(o => o).
                            //Select(o => o * link.Percent).		// I decided not to multiply by percent.  The weight is already reduced, no point in double reducing
                            ToArray();
                    }

                    retVal.Add(new LinkIndexed(link.From.Index, link.To.Index, exist.Weight * link.Percent, brainChemicals));
                }
            }

            #endregion

            // Exit Function
            if (retVal.Count > count)
            {
                #region Prune

                // Prune the weakest links
                // Need to redistribute the lost weight (since this method divided links into smaller ones).  If I don't, then over many generations,
                // the links will tend toward zero

                retVal = retVal.OrderByDescending(o => Math.Abs(o.Weight)).ToList();

                LinkIndexed[] kept = retVal.Take(count).ToArray();
                LinkIndexed[] removed = retVal.Skip(count).ToArray();

                double keptSum = kept.Sum(o => Math.Abs(o.Weight));
                double removedSum = removed.Sum(o => Math.Abs(o.Weight));

                double ratio = keptSum / (keptSum + removedSum);
                ratio = 1d / ratio;

                return kept.Select(o => new LinkIndexed(o.From, o.To, o.Weight * ratio, o.BrainChemicalModifiers)).ToArray();

                #endregion
            }
            else
            {
                return retVal.ToArray();
            }
        }

        #endregion
        #region Private Methods - external links

        #region Random - map

        private static NeuralLink[][] BuildExternalLinks_Random(BotConstruction_PartMap partMap, ContainerInput[] containers, double maxWeight)
        {
            NeuralLink[][] retVal = new NeuralLink[containers.Length][];

            // Pull out the readable nodes from each container
            List<INeuron>[] readable = BuildExternalLinksRandom_FindReadable(containers);

            // Shoot through each container, and create links that feed it
            for (int cntr = 0; cntr < containers.Length; cntr++)
            {
                ContainerInput container = containers[cntr];

                if (container.ExternalRatios == null || container.ExternalRatios.Length == 0)
                {
                    // This container shouldn't be fed by other containers (it's probably a sensor)
                    retVal[cntr] = null;
                    continue;
                }

                // Find writable nodes
                List<INeuron> writeable = new List<INeuron>();
                writeable.AddRange(container.Container.Neruons_ReadWrite);
                writeable.AddRange(container.Container.Neruons_Writeonly);

                if (writeable.Count == 0)
                {
                    // There are no nodes that can be written
                    retVal[cntr] = null;
                    continue;
                }

                List<NeuralLink> links = new List<NeuralLink>();

                foreach (var ratio in container.ExternalRatios)
                {
                    // Link to this container type
                    links.AddRange(BuildExternalLinks_Random_Continue(partMap, containers, cntr, ratio, readable, writeable, maxWeight));
                }

                // Add links to the return jagged array
                if (links.Count == 0)
                {
                    retVal[cntr] = null;
                }
                else
                {
                    retVal[cntr] = links.ToArray();
                }
            }

            // Exit Function
            return retVal;
        }

        private static List<NeuralLink> BuildExternalLinks_Random_Continue(BotConstruction_PartMap partMap, ContainerInput[] containers, int currentIndex, (NeuronContainerType, ExternalLinkRatioCalcType, double) ratio, List<INeuron>[] readable, List<INeuron> writeable, double maxWeight)
        {
            List<NeuralLink> retVal = new List<NeuralLink>();

            // Find eligible containers
            var matchingInputs = BuildExternalLinks_Random_Continue_Eligible(partMap, ratio.Item1, currentIndex, containers, readable);
            if (matchingInputs.Count == 0)
            {
                return retVal;
            }

            // Add up all the eligible neurons
            int sourceNeuronCount = matchingInputs.Sum(o => o.Item2.Count);

            double multiplier = matchingInputs.Average(o => o.Item3);
            multiplier *= ratio.Item3;

            // Figure out how many to create (this is the total count.  Each feeder container will get a percent of these based on its ratio of
            // neurons compared to the other feeders)
            int count = BuildExternalLinks_Count(ratio.Item2, sourceNeuronCount, writeable.Count, multiplier);
            if (count == 0)
            {
                return retVal;
            }

            // I don't want to draw so evenly from all the containers.  Draw from all containers at once.  This will have more clumping, and some containers
            // could be completely skipped.
            //
            // My reasoning for this is manipulators like thrusters don't really make sense to be fed evenly from every single sensor.  Also, thruster's count is by
            // destination neuron, which is very small.  So fewer total links will be created by doing just one pass
            List<Tuple<int, int>> inputLookup = new List<Tuple<int, int>>();
            for (int outer = 0; outer < matchingInputs.Count; outer++)
            {
                for (int inner = 0; inner < matchingInputs[outer].Item2.Count; inner++)
                {
                    //Item1 = index into matchingInputs
                    //Item2 = index into neuron
                    inputLookup.Add(new Tuple<int, int>(outer, inner));
                }
            }

            // For now, just build completely random links
            //NOTE: This is ignoring the relative weights in the map passed in.  Those weights were used to calculate how many total links there should be
            Tuple<int, int, double>[] links = GetRandomLinks(inputLookup.Count, writeable.Count, count, Enumerable.Range(0, inputLookup.Count).ToArray(), Enumerable.Range(0, writeable.Count).ToArray(), new SortedList<int, int>(), maxWeight);

            foreach (var link in links)
            {
                // link.Item1 is the from neuron.  But all the inputs were put into a single list, so use the inputLookup to figure out which container/neuron
                // is being referenced
                var input = matchingInputs[inputLookup[link.Item1].Item1];
                int neuronIndex = inputLookup[link.Item1].Item2;

                double[] brainChemicals = GetBrainChemicalModifiers(input.Item1, maxWeight);		// the brain chemicals are always based on the from container (same in NeuralOperation.Tick)

                retVal.Add(new NeuralLink(input.Item1.Container, containers[currentIndex].Container, input.Item2[neuronIndex], writeable[link.Item2], link.Item3, brainChemicals));
            }

            // Exit Function
            return retVal;
        }

        /// <summary>
        /// Gets the readable neurons from the containers of the type requested (and skips the current container)
        /// </summary>
        private static List<(ContainerInput, List<INeuron>, double)> BuildExternalLinks_Random_Continue_Eligible(BotConstruction_PartMap partMap, NeuronContainerType containerType, int currentIndex, ContainerInput[] containers, List<INeuron>[] readable)
        {
            var retVal = new List<(ContainerInput, List<INeuron>, double)>();

            for (int cntr = 0; cntr < containers.Length; cntr++)
            {
                if (cntr == currentIndex)
                {
                    continue;
                }

                if (containers[cntr].ContainerType != containerType)
                {
                    continue;
                }

                double? weight = null;
                foreach (var mapped in partMap.Map_Actual)
                {
                    if ((mapped.from.Token == containers[currentIndex].Token && mapped.to.Token == containers[cntr].Token) ||
                        (mapped.to.Token == containers[currentIndex].Token && mapped.from.Token == containers[cntr].Token))
                    {
                        weight = mapped.weight;
                        break;
                    }
                }

                if (weight == null)
                {
                    // The map doesn't hold a link between these two containers
                    continue;
                }

                retVal.Add((containers[cntr], readable[cntr], weight.Value));
            }

            return retVal;
        }

        #endregion
        #region Random - old

        /// <summary>
        /// This builds links between neurons across containers
        /// </summary>
        private static NeuralLink[][] BuildExternalLinks_Random(ContainerInput[] containers, double maxWeight)
        {
            NeuralLink[][] retVal = new NeuralLink[containers.Length][];

            // Pull out the readable nodes from each container
            List<INeuron>[] readable = BuildExternalLinksRandom_FindReadable(containers);

            // Shoot through each container, and create links that feed it
            for (int cntr = 0; cntr < containers.Length; cntr++)
            {
                ContainerInput container = containers[cntr];

                if (container.ExternalRatios == null || container.ExternalRatios.Length == 0)
                {
                    // This container shouldn't be fed by other containers (it's probably a sensor)
                    retVal[cntr] = null;
                    continue;
                }

                // Find writable nodes
                List<INeuron> writeable = new List<INeuron>();
                writeable.AddRange(container.Container.Neruons_ReadWrite);
                writeable.AddRange(container.Container.Neruons_Writeonly);

                if (writeable.Count == 0)
                {
                    // There are no nodes that can be written
                    retVal[cntr] = null;
                    continue;
                }

                List<NeuralLink> links = new List<NeuralLink>();

                foreach (var ratio in container.ExternalRatios)
                {
                    // Link to this container type
                    links.AddRange(BuildExternalLinks_Random_Continue(containers, cntr, ratio, readable, writeable, maxWeight));
                }

                // Add links to the return jagged array
                if (links.Count == 0)
                {
                    retVal[cntr] = null;
                }
                else
                {
                    retVal[cntr] = links.ToArray();
                }
            }

            // Exit Function
            return retVal;
        }
        private static List<INeuron>[] BuildExternalLinksRandom_FindReadable(ContainerInput[] containers)
        {
            return containers.
                Select(o => o.ReadableNeurons.ToList()).
                ToArray();
        }

        private static List<NeuralLink> BuildExternalLinks_Random_Continue(ContainerInput[] containers, int currentIndex, (NeuronContainerType, ExternalLinkRatioCalcType, double) ratio, List<INeuron>[] readable, List<INeuron> writeable, double maxWeight)
        {
            List<NeuralLink> retVal = new List<NeuralLink>();

            // Find eligible containers
            var matchingInputs = BuildExternalLinks_Random_Continue_Eligible(ratio.Item1, currentIndex, containers, readable);
            if (matchingInputs.Count == 0)
            {
                return retVal;
            }

            // Add up all the eligible neurons
            int sourceNeuronCount = matchingInputs.Sum(o => o.Item2.Count);

            // Figure out how many to create (this is the total count.  Each feeder container will get a percent of these based on its ratio of
            // neurons compared to the other feeders)
            int count = BuildExternalLinks_Count(ratio.Item2, sourceNeuronCount, writeable.Count, ratio.Item3);
            if (count == 0)
            {
                return retVal;
            }

            // I don't want to draw so evenly from all the containers.  Draw from all containers at once.  This will have more clumping, and some containers
            // could be completely skipped.
            //
            // My reasoning for this is manipulators like thrusters don't really make sense to be fed evenly from every single sensor.  Also, thruster's count is by
            // destination neuron, which is very small.  So fewer total links will be created by doing just one pass
            List<Tuple<int, int>> inputLookup = new List<Tuple<int, int>>();
            for (int outer = 0; outer < matchingInputs.Count; outer++)
            {
                for (int inner = 0; inner < matchingInputs[outer].Item2.Count; inner++)
                {
                    //Item1 = index into matchingInputs
                    //Item2 = index into neuron
                    inputLookup.Add(new Tuple<int, int>(outer, inner));
                }
            }

            // For now, just build completely random links
            Tuple<int, int, double>[] links = GetRandomLinks(inputLookup.Count, writeable.Count, count, Enumerable.Range(0, inputLookup.Count).ToArray(), Enumerable.Range(0, writeable.Count).ToArray(), new SortedList<int, int>(), maxWeight);

            foreach (var link in links)
            {
                // link.Item1 is the from neuron.  But all the inputs were put into a single list, so use the inputLookup to figure out which container/neuron
                // is being referenced
                var input = matchingInputs[inputLookup[link.Item1].Item1];
                int neuronIndex = inputLookup[link.Item1].Item2;

                double[] brainChemicals = GetBrainChemicalModifiers(input.Item1, maxWeight);		// the brain chemicals are always based on the from container (same in NeuralOperation.Tick)

                retVal.Add(new NeuralLink(input.Item1.Container, containers[currentIndex].Container, input.Item2[neuronIndex], writeable[link.Item2], link.Item3, brainChemicals));
            }

            // Exit Function
            return retVal;
        }

        /// <summary>
        /// Gets the readable neurons from the containers of the type requested (and skips the current container)
        /// </summary>
        private static List<Tuple<ContainerInput, List<INeuron>>> BuildExternalLinks_Random_Continue_Eligible(NeuronContainerType containerType, int currentIndex, ContainerInput[] containers, List<INeuron>[] readable)
        {
            List<Tuple<ContainerInput, List<INeuron>>> retVal = new List<Tuple<ContainerInput, List<INeuron>>>();

            for (int cntr = 0; cntr < containers.Length; cntr++)
            {
                if (cntr == currentIndex)
                {
                    continue;
                }

                if (containers[cntr].ContainerType != containerType)
                {
                    continue;
                }

                retVal.Add(new Tuple<ContainerInput, List<INeuron>>(containers[cntr], readable[cntr]));
            }

            return retVal;
        }

        #endregion
        #region Existing

        private static NeuralLink[][] BuildExternalLinksExisting(ContainerInput[] containers, int maxIntermediateLinks, int maxFinalLinks)
        {
            NeuralLink[][] retVal = new NeuralLink[containers.Length][];

            // Figure out which containers (parts) are closest to the containers[].ExternalLinks[].FromContainerPosition
            var partBreakdown = BuildExternalLinksExisting_ContainerPoints(containers, maxIntermediateLinks);

            // This gets added to as needed (avoids recalculating best matching neurons by position)
            Dictionary<ContainerInput, ContainerPoints> nearestNeurons = new Dictionary<ContainerInput, ContainerPoints>();

            for (int cntr = 0; cntr < containers.Length; cntr++)
            {
                ContainerInput container = containers[cntr];

                if (container.ExternalLinks == null || container.ExternalLinks.Length == 0)
                {
                    // There are no existing external links
                    retVal[cntr] = null;
                    continue;
                }

                List<NeuralLink> containerLinks = new List<NeuralLink>();

                // The external links are from shifting from containers, but the to container is always known (this container), so the to container
                // is always an array of one
                ClosestExistingResult[] toPart = new ClosestExistingResult[] { new ClosestExistingResult(true, cntr, 1d) };

                // Link part to part
                foreach (var exist in container.ExternalLinks)
                {
                    // Figure out which parts to draw from
                    HighestPercentResult[] partLinks = GetHighestPercent(partBreakdown[exist.FromContainerPosition], toPart, maxIntermediateLinks, true);

                    // Get links between neurons in between the matching parts
                    containerLinks.AddRange(BuildExternalLinksExisting_AcrossParts(exist, partLinks, containers, nearestNeurons, maxIntermediateLinks, maxFinalLinks));
                }

                // Prune
                containerLinks = BuildExternalLinksExisting_Prune(containerLinks, containers, container);

                retVal[cntr] = containerLinks.ToArray();
            }

            return retVal;
        }

        private static NeuralLink[] BuildExternalLinksExisting_AcrossParts(NeuralLinkExternalDNA dnaLink, HighestPercentResult[] partLinks, ContainerInput[] containers, Dictionary<ContainerInput, ContainerPoints> nearestNeurons, int maxIntermediateLinks, int maxFinalLinks)
        {
            List<NeuralLink> retVal = new List<NeuralLink>();

            foreach (HighestPercentResult partLink in partLinks)
            {
                #region get containers

                ContainerInput fromContainer = containers[partLink.From.Index];
                if (!nearestNeurons.ContainsKey(fromContainer))
                {
                    nearestNeurons.Add(fromContainer, new ContainerPoints(fromContainer, maxIntermediateLinks));
                }
                ContainerPoints from = nearestNeurons[fromContainer];

                ContainerInput toContainer = containers[partLink.To.Index];
                if (!nearestNeurons.ContainsKey(toContainer))
                {
                    nearestNeurons.Add(toContainer, new ContainerPoints(toContainer, maxIntermediateLinks));
                }
                ContainerPoints to = nearestNeurons[toContainer];

                #endregion

                List<LinkIndexed> links = new List<LinkIndexed>();

                // Build links
                HighestPercentResult[] bestLinks = GetHighestPercent(from.GetNearestPoints(dnaLink.From), to.GetNearestPoints(dnaLink.To), maxIntermediateLinks, false);
                foreach (HighestPercentResult link in bestLinks)
                {
                    double[] brainChemicals = null;
                    if (dnaLink.BrainChemicalModifiers != null)
                    {
                        brainChemicals = dnaLink.BrainChemicalModifiers.
                            Take(fromContainer.BrainChemicalCount).		// if there are more, just drop them
                            Select(o => o).
                            //Select(o => o * link.Percent).		// I decided not to multiply by percent.  The weight is already reduced, no point in double reducing
                            ToArray();
                    }

                    links.Add(new LinkIndexed(link.From.Index, link.To.Index, dnaLink.Weight * link.Percent, brainChemicals));
                }

                // Convert the indices into return links
                retVal.AddRange(links.Select(o => new NeuralLink(fromContainer.Container, toContainer.Container, from.AllNeurons[o.From], to.AllNeurons[o.To], partLink.Percent * o.Weight, o.BrainChemicalModifiers)));
            }

            if (retVal.Count > maxFinalLinks)
            {
                // Prune and normalize percents
                var pruned = Prune(retVal.Select(o => o.Weight).ToArray(), maxFinalLinks);		// choose the top X weights, and tell what the new weight should be

                retVal = pruned.        // copy the referenced retVal items, but with the altered weights
                    Select(o => new NeuralLink
                    (
                        retVal[o.index].FromContainer,
                        retVal[o.index].ToContainer,
                        retVal[o.index].From,
                        retVal[o.index].To,
                        o.weight,
                        retVal[o.index].BrainChemicalModifiers
                    )).
                    ToList();
            }

            return retVal.ToArray();
        }

        /// <summary>
        /// The containers may not be in the same place that the links think they are.  So this method goes through all the container positions
        /// that all the links point to, and figures out which containers are closest to those positions
        /// </summary>
        private static Dictionary<Point3D, ClosestExistingResult[]> BuildExternalLinksExisting_ContainerPoints(ContainerInput[] containers, int maxIntermediateLinks)
        {
            Dictionary<Point3D, ClosestExistingResult[]> retVal = new Dictionary<Point3D, ClosestExistingResult[]>();		// can't use SortedList, because point isn't sortable (probably doesn't have IComparable)

            Point3D[] allPartPoints = containers.Select(o => o.Position).ToArray();

            foreach (ContainerInput container in containers.Where(o => o.ExternalLinks != null && o.ExternalLinks.Length > 0))
            {
                // Get a unique list of referenced parts
                foreach (var exist in container.ExternalLinks)
                {
                    if (!retVal.ContainsKey(exist.FromContainerPosition))
                    {
                        retVal.Add(exist.FromContainerPosition, GetClosestExisting(exist.FromContainerPosition, allPartPoints, maxIntermediateLinks));
                    }
                }
            }

            // Exit Function
            return retVal;
        }

        private static List<NeuralLink> BuildExternalLinksExisting_Prune(List<NeuralLink> links, ContainerInput[] containers, ContainerInput toContainer)
        {
            List<NeuralLink> retVal = new List<NeuralLink>();

            // Process each from container separately
            foreach (var group in links.ToLookup(o => o.FromContainer))
            {
                ContainerInput fromContainer = containers.First(o => o.Container == group.Key);

                // Find the ratio that talks about this from container
                var ratio = toContainer.ExternalRatios.FirstOrDefault_val(o => o.type == fromContainer.ContainerType);
                if (ratio == null)
                {
                    // Links aren't allowed between these types of containers
                    continue;
                }

                NeuralLink[] groupLinks = group.ToArray();

                // Figure out how many links are supported
                int maxCount = BuildExternalLinks_Count
                (
                    ratio.Value.calc,
                    fromContainer.Container.Neruons_Readonly.Concat(fromContainer.Container.Neruons_ReadWrite).Count(),
                    toContainer.Container.Neruons_Writeonly.Concat(toContainer.Container.Neruons_ReadWrite).Count(),
                    ratio.Value.mult
                );

                if (groupLinks.Length <= maxCount)
                {
                    // No need to prune, keep all of these
                    retVal.AddRange(groupLinks);
                }
                else
                {
                    // Prune
                    var pruned = Prune(groupLinks.Select(o => o.Weight).ToArray(), maxCount);		// get the largest weights, and normalize them
                    retVal.AddRange     // copy the referenced links, but use the altered weights
                    (
                        pruned.
                            Select(o => new NeuralLink
                            (
                                groupLinks[o.index].FromContainer,
                                groupLinks[o.index].ToContainer,
                                groupLinks[o.index].From,
                                groupLinks[o.index].To,
                                o.weight,
                                groupLinks[o.index].BrainChemicalModifiers
                            ))
                    );
                }
            }

            return retVal;
        }

        #endregion

        /// <summary>
        /// Figures out how many links to create based on the number of neurons
        /// </summary>
        private static int BuildExternalLinks_Count(ExternalLinkRatioCalcType calculationType, int sourceNeuronCount, int destinationNeuronCount, double ratio)
        {
            double retVal;

            switch (calculationType)
            {
                case ExternalLinkRatioCalcType.Smallest:
                    int smallerNeuronCount = Math.Min(sourceNeuronCount, destinationNeuronCount);
                    retVal = smallerNeuronCount;
                    break;

                case ExternalLinkRatioCalcType.Largest:
                    int largerNeuronCount = Math.Max(sourceNeuronCount, destinationNeuronCount);
                    retVal = largerNeuronCount;
                    break;

                case ExternalLinkRatioCalcType.Average:
                    double averageNeuronCount = Math.Round((sourceNeuronCount + destinationNeuronCount) / 2d);
                    retVal = averageNeuronCount;
                    break;

                case ExternalLinkRatioCalcType.Source:
                    retVal = sourceNeuronCount;
                    break;

                case ExternalLinkRatioCalcType.Destination:
                    retVal = destinationNeuronCount;
                    break;

                default:
                    throw new ApplicationException("Unknown ExternalLinkRatioCalcType: " + calculationType.ToString());
            }

            return (retVal * ratio).ToInt_Round();
        }

        #endregion
        #region Private Methods - random

        /// <summary>
        /// This comes up with completely random links
        /// NOTE: readwritePairs means neurons that are in both read and write lists (not nessassarily INeuronContainer.Neruons_ReadWrite)
        /// </summary>
        private static Tuple<int, int, double>[] GetRandomLinks(int fromCount, int toCount, int count, int[] readonlyIndices, int[] writeonlyIndices, SortedList<int, int> readwritePairs, double maxWeight)
        {
            // See how many links are possible
            int possibleCount = GetPossibilityCount(fromCount, toCount, readwritePairs.Count);

            List<Tuple<int, int>> possibleLinks = null;

            if (count > possibleCount)
            {
                #region Build all possible

                // Return all possible links (but still give them random weights)
                possibleLinks = GetAllPossibleLinks(fromCount, toCount, readonlyIndices, writeonlyIndices, readwritePairs);

                #endregion
            }
            else if (count > possibleCount / 2)
            {
                #region Reduce from all possible

                // Build a list of all possible links, then choose random ones out of that list (more efficient than having a while loop throwing out dupes)
                possibleLinks = GetAllPossibleLinks(fromCount, toCount, readonlyIndices, writeonlyIndices, readwritePairs);

                Random rand = StaticRandom.GetRandomForThread();

                // Remove random links until possible links has the correct number of links
                while (possibleLinks.Count > count)
                {
                    possibleLinks.RemoveAt(rand.Next(possibleLinks.Count));
                }

                #endregion
            }
            else
            {
                #region Pick random links

                possibleLinks = new List<Tuple<int, int>>();

                Random rand = StaticRandom.GetRandomForThread();

#if DEBUG
                int numIterations = 0;
#endif
                while (possibleLinks.Count < count)
                {
#if DEBUG
                    numIterations++;
#endif

                    // Make a random attempt
                    Tuple<int, int> link = new Tuple<int, int>(rand.Next(fromCount), rand.Next(toCount));

                    if (readwritePairs.ContainsKey(link.Item1) && readwritePairs[link.Item1] == link.Item2)
                    {
                        // This represents the same node (can't have nodes pointing to themselves)
                        continue;
                    }

                    if (possibleLinks.Contains(link))
                    {
                        // This link was already used
                        continue;
                    }

                    possibleLinks.Add(link);
                }

                #endregion
            }

            // Assign random weights
            return possibleLinks.
                Select(o => Tuple.Create(o.Item1, o.Item2, Math1D.GetNearZeroValue(maxWeight))).
                ToArray();
        }
        private static Tuple<int, int, double>[] GetRandomLinks_Weight(IEnumerable<Tuple<int, int>> links, double maxWeight)
        {
            List<Tuple<int, int, double>> retVal = new List<Tuple<int, int, double>>();

            foreach (var link in links)
            {
                retVal.Add(new Tuple<int, int, double>(link.Item1, link.Item2, Math1D.GetNearZeroValue(maxWeight)));
            }

            return retVal.ToArray();
        }

        /// <summary>
        /// This returns how many links would be created if every node were connected to every other node
        /// </summary>
        private static int GetPossibilityCount(int fromCount, int toCount, int sharedCount)
        {
            // Both from and to have the same shared items, so subtract that off
            int readCount = fromCount - sharedCount;
            int writeCount = toCount - sharedCount;

            // Number of combinations between readonly and writeonly
            int retVal = readCount * writeCount;

            if (sharedCount > 0)
            {
                // Include links from read to readwrite, and links from readwrite to write
                retVal += readCount * sharedCount;
                retVal += sharedCount * writeCount;

                // Number of combinations of readwrite
                retVal += (sharedCount * (sharedCount - 1)) / 2;		// it should be safe to leave this as integer division, because the multiplication will always produce an even number
            }

            return retVal;
        }

        private static List<Tuple<int, int>> GetAllPossibleLinks(int fromCount, int toCount, int[] readonlyIndices, int[] writeonlyIndices, SortedList<int, int> readwritePairs)
        {
            List<Tuple<int, int>> retVal = new List<Tuple<int, int>>();

            #region readonly -> writeonly

            if (writeonlyIndices.Length > 0)
            {
                for (int outer = 0; outer < readonlyIndices.Length; outer++)
                {
                    for (int inner = 0; inner < writeonlyIndices.Length; inner++)
                    {
                        retVal.Add(new Tuple<int, int>(readonlyIndices[outer], writeonlyIndices[inner]));
                    }
                }
            }

            #endregion

            #region readonly -> readwrite

            if (readwritePairs.Count > 0)
            {
                for (int outer = 0; outer < readonlyIndices.Length; outer++)
                {
                    for (int inner = 0; inner < readwritePairs.Count; inner++)
                    {
                        retVal.Add(new Tuple<int, int>(readonlyIndices[outer], readwritePairs.Values[inner]));
                    }
                }
            }

            #endregion

            #region readwrite -> writeonly

            if (writeonlyIndices.Length > 0)
            {
                for (int outer = 0; outer < readwritePairs.Count; outer++)
                {
                    for (int inner = 0; inner < writeonlyIndices.Length; inner++)
                    {
                        retVal.Add(new Tuple<int, int>(readwritePairs.Keys[outer], writeonlyIndices[inner]));
                    }
                }
            }

            #endregion

            #region readwrite -> readwrite

            for (int outer = 0; outer < readwritePairs.Count - 1; outer++)
            {
                for (int inner = outer + 1; inner < readwritePairs.Count; inner++)
                {
                    retVal.Add(new Tuple<int, int>(readwritePairs.Keys[outer], readwritePairs.Values[inner]));
                }
            }

            #endregion

            return retVal;
        }

        private static double[] GetBrainChemicalModifiers(ContainerInput container, double maxWeight)
        {
            if (container.BrainChemicalCount == 0)
            {
                return null;
            }

            int count = StaticRandom.Next(container.BrainChemicalCount);
            if (count == 0)
            {
                return null;
            }

            double[] retVal = new double[count];

            for (int cntr = 0; cntr < count; cntr++)
            {
                retVal[cntr] = Math1D.GetNearZeroValue(maxWeight);
            }

            return retVal;
        }

        #endregion
        #region Private Methods - existing

        private static ClosestExistingResult[] GetClosestExisting(Point3D search, Point3D[] points, int maxReturn)
        {
            const double SEARCHRADIUSMULT = 2.5d;		// looks at other nodes that are up to minradius * mult

            // Check for exact match
            int index = FindExact(search, points);
            if (index >= 0)
            {
                return new ClosestExistingResult[] { new ClosestExistingResult(true, index, 1d) };
            }

            // Get a list of nodes that are close to the search point
            var nearNodes = GetNearNodes(search, points, SEARCHRADIUSMULT);

            if (nearNodes.Count == 1)
            {
                // There's only one, so give it the full weight
                return new ClosestExistingResult[] { new ClosestExistingResult(false, nearNodes[0].Item1, 1d) };
            }

            // Don't allow too many divisions
            if (nearNodes.Count > maxReturn)
            {
                nearNodes = nearNodes.OrderBy(o => o.Item2).Take(maxReturn).ToList();
            }

            // Figure out what percent of the weight to give these nodes (based on the ratio of their distances to the search point)
            var percents = GetPercentOfWeight(nearNodes, SEARCHRADIUSMULT);

            // Exit Function
            return percents.Select(o => new ClosestExistingResult(false, o.Item1, o.Item2)).ToArray();
        }
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

            // Figure out ratios base on distance
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

        private static HighestPercentResult[] GetHighestPercent(ClosestExistingResult[] from, ClosestExistingResult[] to, int maxReturn, bool isFromSameList)
        {
            // Find the combinations that have the highest percentage
            var products = new List<(int from, int to, double percent)>();

            for (int fromCntr = 0; fromCntr < from.Length; fromCntr++)
            {
                for (int toCntr = 0; toCntr < to.Length; toCntr++)
                {
                    if (isFromSameList && from[fromCntr].Index == to[toCntr].Index)
                    {
                        continue;
                    }

                    products.Add((fromCntr, toCntr, from[fromCntr].Percent * to[toCntr].Percent));
                }
            }

            // Don't return too many
            var topProducts = products.Count <= maxReturn ?

                products.       // no need to sort or limit
                    ToArray() :

                products.
                    OrderByDescending(o => o.percent).
                    Take(maxReturn).
                    ToArray();

            // Normalize
            double totalPercent = topProducts.Sum(o => o.percent);

            return topProducts.
                Select(o => new HighestPercentResult(from[o.from], to[o.to], o.percent / totalPercent)).
                ToArray();
        }

        /// <summary>
        /// This is meant to be a generic prune method.  It returns the top weights, and normalizes them so the sum of the smaller
        /// set is the same as the sum of the larger set
        /// </summary>
        private static (int index, double weight)[] Prune(double[] weights, int count)
        {
            // Convert the weights into a list with the original index
            var weightsIndexed = weights.
                Select((o, i) => (index: i, weight: o)).
                ToArray();

            if (count > weights.Length)
            {
                // This method shouldn't have been called, there is nothing to do
                return weightsIndexed;
            }

            var topWeights = weightsIndexed.
                OrderByDescending(o => Math.Abs(o.weight)).
                Take(count).
                ToArray();

            double sumWeights = weights.Sum(o => Math.Abs(o));
            double sumTop = topWeights.Sum(o => Math.Abs(o.weight));

            double ratio = sumWeights / sumTop;
            if (double.IsNaN(ratio))
            {
                ratio = 1d;     // probably divide by zero
            }

            // Normalize the top weights
            return topWeights.
                Select(o => (o.index, o.weight * ratio)).
                ToArray();
        }

        private static NeuralLink[][] CapWeights(NeuralLink[][] links, double maxWeight)
        {
            if (links == null)
            {
                return null;
            }

            NeuralLink[][] retVal = new NeuralLink[links.Length][];

            for (int outer = 0; outer < links.Length; outer++)
            {
                if (links[outer] == null)
                {
                    retVal[outer] = null;
                    continue;
                }

                retVal[outer] = links[outer].Select(o => new NeuralLink(o.FromContainer, o.ToContainer, o.From, o.To, CapWeights_Weight(o.Weight, maxWeight),
                    o.BrainChemicalModifiers == null ? null : o.BrainChemicalModifiers.Select(p => CapWeights_Weight(p, maxWeight)).ToArray())).ToArray();
            }

            return retVal;
        }
        private static double CapWeights_Weight(double weight, double max)
        {
            if (Math.Abs(weight) > max)
            {
                if (weight > 0)
                {
                    return max;
                }
                else
                {
                    return -max;
                }
            }
            else
            {
                return weight;
            }
        }

        #endregion
        #region Private Methods - link2 - new

        /// <summary>
        /// This finds an unlinked neuron from the source containers and wires it up to an output
        /// </summary>
        /// <remarks>
        /// Even though the neurons chosen are random, the lists are sorted so it's a weighted random
        /// </remarks>
        private static void BuildExternalLinks3_NextLink(List<ContainerReads> sourceContainers, List<SourceDestinationLink> links, Random rand)
        {
            // Choose a random destination container (weighted random based on relative sizes of containers)
            ContainerWrites destination = GetRandomDestinationContainer(sourceContainers, rand);

            // Pick a random input neuron
            int setIndex = rand.Next(destination.RemainingCandidates_BySource.Count);

            // Pick source and destination neurons
            SourceDestinationLink[] sortedLinks = null;
            if (destination.Container.ContainerType == NeuronContainerType.Manipulator)
            {
                #region manipulator

                // Get the unique destination neurons in this container
                var destNeruons = destination.RemainingCandidates_BySource[setIndex].
                    ToLookup(o => o.DestinationNeuron.Token).
                    Select(o => o.First()).
                    Select(o => new
                    {
                        token = o.DestinationNeuron.Token,
                        linkCount = o.DestinationNeuronNumConnections.Count,
                        position = o.DestinationNeuron.Position,
                    }).
                    ToArray();

                // neuron weight = dist to nearest linked neuron
                var destWeights = destNeruons.
                    Select(o =>
                    {
                        double weight = 0d;
                        if (o.linkCount == 0)
                        {
                            double? closest = destNeruons.
                                Where(p => p.token != o.token && p.linkCount > 0).
                                Select(p => (p.position - o.position).LengthSquared).
                                OrderBy(p => p).
                                FirstOrDefault();

                            if (closest != null)
                            {
                                weight = closest.Value;
                            }
                        }

                        return new
                        {
                            o.token,
                            weight,
                        };
                    }).
                    ToArray();

                // Copy those weights to destination.RemainingCandidates
                foreach (var candidate in destination.RemainingCandidates_BySource[setIndex])
                {
                    candidate.DestinationManipulatorWeight = destWeights.
                        First(o => o.token == candidate.DestinationNeuron.Token).
                        weight;
                }

                // Sort the candidates so that unlinked neurons that are far from linked neurons are first
                sortedLinks = destination.RemainingCandidates_BySource[setIndex].
                    OrderBy(o => o.DestinationNeuronNumConnections.Count).        // give unlinked neurons a much higer priority
                    ThenByDescending(o => o.DestinationManipulatorWeight).      // give top priority to the neuron that is farthest from an already linked neuron
                    ToArray();

                #endregion
            }
            else
            {
                #region all others

                sortedLinks = destination.RemainingCandidates_BySource[setIndex].
                    OrderBy(o => o.DestinationNeuronNumConnections.Count).        // give unlinked neurons a much higer priority
                    ThenBy(o => o.DistanceNeurons).     // within a set of the same number of connections, give closer neurons more priority
                    ToArray();

                #endregion
            }

            //int test_from = sortedLinks[0].DestinationNeuronNumConnections.Count;
            //int test_to = sortedLinks[sortedLinks.Length - 1].DestinationNeuronNumConnections.Count;

            int linkIndex = UtilityCore.GetIndexIntoList(rand.NextPow(2), sortedLinks.Length);

            SourceDestinationLink link = sortedLinks[linkIndex];

            links.Add(link);
            link.DestinationNeuronNumConnections.Count++;

            // Remove this link from the lists
            RemoveLink3(sourceContainers, destination, setIndex, link);
        }

        private static ContainerWrites GetRandomDestinationContainer(List<ContainerReads> sourceContainers, Random rand)
        {
            // Pick a random source container
            double totalReadable = sourceContainers.Sum(o => o.Container.ReadableNeurons.Count());
            var sourceFractions = sourceContainers.
                Select((o, i) => (i, o.Container.ReadableNeurons.Count().ToDouble() / totalReadable)).
                ToArray();

            int sourceIndex = UtilityCore.GetIndexIntoList(rand.NextDouble(), sourceFractions);     // need sourceFractions so that larger containers get chosen proportianally more
            ContainerReads source = sourceContainers[sourceIndex];

            // Pick a random destination container that is linked to it (also weighted by relative sizes)
            double totalWritable = source.RemainingCandidates.Sum(o => o.Weight * o.Container.WritableNeurons.Count());
            var destFractions = source.RemainingCandidates.
                Select((o, i) => (i, (o.Weight * o.Container.WritableNeurons.Count()).ToDouble() / totalWritable)).
                ToArray();

            int destIndex = UtilityCore.GetIndexIntoList(rand.NextDouble(), destFractions);
            return source.RemainingCandidates[destIndex];
        }

        private static void StoreExistingLinks(NeuralLink[][] externalLinks, List<ContainerReads> sourceContainers, List<SourceDestinationLink> links)
        {
            foreach (NeuralLink external in externalLinks.SelectMany(o => o ?? new NeuralLink[0]))
            {
                ContainerReads source = sourceContainers.FirstOrDefault(o => o.Container.Container.Token == external.FromContainer.Token);
                if (source == null)
                {
                    continue;       // this should probably never happen
                }

                ContainerWrites destination = source.RemainingCandidates.FirstOrDefault(o => o.Container.Container.Token == external.ToContainer.Token);
                if (destination == null)
                {
                    continue;
                }

                SourceDestinationLink link = destination.RemainingCandidates.
                    FirstOrDefault(o => o.SourceNeuron.Token == external.From.Token && o.DestinationNeuron.Token == external.To.Token);

                if (link != null)
                {
                    link.ExistingWeight = external.Weight;

                    links.Add(link);
                    link.DestinationNeuronNumConnections.Count++;
                }
            }

            foreach (SourceDestinationLink link in links)
            {
                RemoveLink(sourceContainers, link);
            }
        }

        //NOTE: This will produce dupes for brains that have readwrite neurons
        private static List<ContainerReads> GetPossibleMappings(ContainerInput[] items_brain, ContainerInput[] items_io, Tuple<int, int>[] links_brain_io, LinkSetPair[] links_brain)
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
                allLinks.AddRange(GetPossibleMappings_pair(items_brain[link.Item1], radius_brains[link.Item1], items_io[link.Item2], radius_io[link.Item2], neuronCounts));
                allLinks.AddRange(GetPossibleMappings_pair(items_io[link.Item2], radius_io[link.Item2], items_brain[link.Item1], radius_brains[link.Item1], neuronCounts));
            }

            foreach ((int, int) link in links_brain2)
            {
                allLinks.AddRange(GetPossibleMappings_pair(items_brain[link.Item1], radius_brains[link.Item1], items_brain[link.Item2], radius_brains[link.Item2], neuronCounts));
                allLinks.AddRange(GetPossibleMappings_pair(items_brain[link.Item2], radius_brains[link.Item2], items_brain[link.Item1], radius_brains[link.Item1], neuronCounts));
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
                            Weight = GetWeight(o.First().SourceContainer, p.First().DestinationContainer),
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
        private static IEnumerable<SourceDestinationLink> GetPossibleMappings_pair(ContainerInput from, double fromRadius, ContainerInput to, double toRadius, SortedList<long, DestinationNeuronCount> neuronCounts)
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

        private static double GetNeuronRadius(ContainerInput container)
        {
            double lengthSquared = container.Container.Neruons_All.
                Max(o => o.Position.ToVector().LengthSquared);

            return Math.Sqrt(lengthSquared);
        }

        private static int GetWeight(NeuralUtility.ContainerInput sourceContainer, NeuralUtility.ContainerInput destinationContainer)
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

                default:
                    throw new ApplicationException($"Unexpected {typeof(NeuronContainerType)}: {sourceContainer.ContainerType}");
            }
        }

        private static void RemoveLink(List<ContainerReads> sourceContainers, SourceDestinationLink link)
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
        private static void RemoveLink3(List<ContainerReads> sourceContainers, ContainerWrites destination, int setIndex, SourceDestinationLink link)
        {
            destination.RemainingCandidates_BySource.RemoveAt(setIndex);
            if(destination.RemainingCandidates_BySource.Count > 0)
            {
                return;
            }

            int indexSource = 0;
            while(indexSource < sourceContainers.Count)
            {
                sourceContainers[indexSource].RemainingCandidates.RemoveWhere(o => o == destination);

                if(sourceContainers[indexSource].RemainingCandidates.Count == 0)
                {
                    sourceContainers.RemoveAt(indexSource);
                }
                else
                {
                    indexSource++;
                }
            }
        }

        #endregion
        #region Private Methods - divide neurons

        private static void DivideNeuronShellsByRadius_AdjustForces(int?[] forces, int index, int count, int desiredSize, Func<int, int> getCount)
        {
            // Keep going left, applying a force to the right until a source is found
            for (int cntr = index - 1; cntr >= 0; cntr--)
            {
                forces[cntr] = (forces[cntr] ?? 0) + 1;

                if (getCount(cntr) > desiredSize)
                {
                    // A source has been found that can loose a neuron in the direction of the sink, so stop sending
                    // out a request force
                    break;
                }
            }

            // Walk right, applying a force to the left
            for (int cntr = index + 1; cntr < count; cntr++)
            {
                forces[cntr] = (forces[cntr] ?? 0) - 1;

                if (getCount(cntr) > desiredSize)
                {
                    break;
                }
            }
        }

        private static int DivideNeuronShellsByRadius_GetPushSourceIndex(int?[] forces, int countPerSet, Func<int, int> getCount)
        {
            // Only look at items where count is greater than desired (forces has no meaning for the other items)
            var mostDemand = Enumerable.Range(0, forces.Length).
                Select(o => new
                {
                    index = o,
                    count = getCount(o),
                    magnitude = forces[o] == null ?
                        0 :
                        Math.Abs(forces[o].Value),
                }).
                Where(o => o.count > countPerSet && forces[o.index] != null).       // if count is <= desired, then it's not a source.  Also, if force is null for this source, it is buffered by another source
                ToLookup(o => o.magnitude).     // group them up by magnitude (there could be a source with a desire to the left and another source with a desire to the right that happen to have the same magnitude)
                OrderByDescending(o => o.Key).
                First();

            var subset = mostDemand.ToArray();      // there can be multiple with this same magnitude of demand

            // Even though there could be multiple with this magnitude, just pick one of them for this iteration
            int subindex = StaticRandom.Next(subset.Length);

            return subset[subindex].index;
        }

        private static int DivideNeuronShellsByRadius_GetPushDirection(int force)
        {
            if (force == 0)
            {
                // It's being pulled on from both directions.  Choose a random direction
                return StaticRandom.NextBool() ?
                    -1 :
                    1;
            }
            else if (force > 0)
            {
                // It doesn't matter how strong the demand is, the direction is 1
                return 1;
            }
            else //if(force < 0)
            {
                return -1;
            }
        }

        private static void DivideNeuronShellsByRadius_DrawPointShells(Point3D[][] points, Color[] colors, double[] radii, string title)
        {
            const double LINE = .005;
            const double DOT = .02;

            Debug3DWindow window = new Debug3DWindow()
            {
                Title = title,
            };

            for (int cntr = 0; cntr < colors.Length; cntr++)
            {
                window.AddCircle(new Point3D(), radii[cntr], LINE, colors[cntr], new Triangle(new Point3D(-1, 0, 0), new Point3D(1, 0, 0), new Point3D(0, 1, 0)));      // XY
                window.AddCircle(new Point3D(), radii[cntr], LINE, colors[cntr], new Triangle(new Point3D(-1, 0, 0), new Point3D(1, 0, 0), new Point3D(0, 0, 1)));      // XZ
                window.AddCircle(new Point3D(), radii[cntr], LINE, colors[cntr], new Triangle(new Point3D(0, -1, 0), new Point3D(0, 1, 0), new Point3D(0, 0, 1)));      // YZ

                window.AddText($"{cntr}: {points[cntr].Length}", color: colors[cntr].ToHex());

                if (points[cntr].Length == 0)
                {
                    continue;
                }

                window.AddDots(points[cntr], DOT, colors[cntr]);
            }

            window.Show();
        }

        #endregion
        #region Private Methods - arrangements

        private static Vector3D[] GetNeuronPositionsInitial(out Vector3D[] staticPoints, out double[] staticRepulseMult, Point3D[] dnaPositions, Point3D[] existingStaticPositions, double staticMultValue, int count, double radius, Func<double, Vector3D> getNewPoint)
        {
            //TODO: When reducing/increasing, it is currently just being random.  It may be more realistic to take proximity into account.  Play
            // a variant of conway's game of life or something

            Vector3D[] retVal;

            if (dnaPositions == null)
            {
                #region create new

                retVal = new Vector3D[count];
                for (int cntr = 0; cntr < count; cntr++)
                {
                    //retVal[cntr] = Math3D.GetRandomVectorSpherical(radius);
                    retVal[cntr] = getNewPoint(radius);     // using a delegate instead
                }

                #endregion
            }
            else if (dnaPositions.Length > count)
            {
                #region reduce

                List<Vector3D> posList = dnaPositions.
                    Select(o => o.ToVector()).
                    ToList();

                int reduceCount = dnaPositions.Length - count;

                for (int cntr = 0; cntr < reduceCount; cntr++)
                {
                    posList.RemoveAt(StaticRandom.Next(posList.Count));
                }

                retVal = posList.ToArray();

                #endregion
            }
            else if (dnaPositions.Length < count)
            {
                #region increase

                List<Vector3D> posList = dnaPositions.
                    Select(o => o.ToVector()).
                    ToList();

                int increaseCount = count - dnaPositions.Length;

                for (int cntr = 0; cntr < increaseCount; cntr++)
                {
                    //posList.Add(Math3D.GetRandomVectorSpherical2D(radius));
                    posList.Add(getNewPoint(radius));       // using a delegate instead
                }

                retVal = posList.ToArray();

                #endregion
            }
            else
            {
                #region copy as is

                retVal = dnaPositions.
                    Select(o => o.ToVector()).
                    ToArray();

                #endregion
            }

            // Prep the static point arrays
            staticPoints = null;
            staticRepulseMult = null;
            if (existingStaticPositions != null)
            {
                staticPoints = existingStaticPositions.
                    Select(o => o.ToVector()).
                    ToArray();

                // This will force more than normal distance between brain chemical neurons and standard neurons.  The reason I'm doing this
                // is because the brain chemical neurons can have some powerful effects on the brain, and when an offspring is made, the links
                // in the dna only hold neuron position.  By having a larger gap, there is less chance of accidental miswiring
                staticRepulseMult = Enumerable.Range(0, staticPoints.Length).
                    Select(o => staticMultValue).
                    ToArray();
            }

            return retVal;
        }

        #endregion
    }
}
