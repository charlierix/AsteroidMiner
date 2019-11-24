using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using Game.HelperClassesCore.Threads;
using Game.HelperClassesWPF;
using SharpNeat.Core;
using SharpNeat.Decoders;
using SharpNeat.Decoders.HyperNeat;
using SharpNeat.Decoders.Neat;
using SharpNeat.DistanceMetrics;
using SharpNeat.Domains;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.EvolutionAlgorithms.ComplexityRegulation;
using SharpNeat.Genomes.Neat;
using SharpNeat.Network;
using SharpNeat.Phenomes;
using SharpNeat.SpeciationStrategies;

namespace Game.HelperClassesAI
{
    /// <summary>
    /// This is an implementation of INeatExperiment with the common code as a base class
    /// </summary>
    /// <remarks>
    /// NOTE: INeatExperiment likes xml, but this uses a strongly typed arg class instead
    /// </remarks>
    public class ExperimentNEATBase : INeatExperiment
    {
        #region Declaration Section

        protected NetworkActivationScheme _activationSchemeCppn = null;       // this is only populated for hyperneat
        protected NetworkActivationScheme _activationScheme = null;

        private ExperimentInitArgs_Activation _activationDefinition = null;

        private string _complexityRegulationStr = null;
        private int? _complexityThreshold = null;
        private ParallelOptions _parallelOptions = null;

        // These are the evaluators that score individuals - only one of these two will be populated
        private IPhenomeEvaluator<IBlackBox> _phenomeEvaluator = null;

        private IPhenomeTickEvaluator<IBlackBox, NeatGenome>[] _phenomeEvaluators = null;
        private RoundRobinManager _phenometickeval_roundRobinManager = null;
        private Func<double> _phenometickeval_worldTick = null;

        private bool _wasInitializeCalled = false;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the name of the experiment.
        /// </summary>
        public string Name
        {
            get;
            private set;
        }
        /// <summary>
        /// Gets human readable explanatory text for the experiment.
        /// </summary>
        public string Description
        {
            get;
            private set;
        }

        public bool IsHyperNEAT
        {
            get;
            private set;
        }

        private int _inputCount = 0;
        /// <summary>
        /// Gets the number of inputs required by the network/black-box that the underlying problem domain is based on.
        /// </summary>
        public int InputCount
        {
            get
            {
                if (IsHyperNEAT)
                {
                    throw new InvalidOperationException("This property isn't supported when the experiment is hyper neat");
                }

                return _inputCount;
            }
        }

        private int _outputCount = 0;
        /// <summary>
        /// Gets the number of outputs required by the network/black-box that the underlying problem domain is based on.
        /// </summary>
        public int OutputCount
        {
            get
            {
                if (IsHyperNEAT)
                {
                    throw new InvalidOperationException("This property isn't supported when the experiment is hyper neat");
                }

                return _outputCount;
            }
        }

        public int DefaultPopulationSize
        {
            get;
            private set;
        }

        public NeatGenomeParameters NeatGenomeParameters
        {
            get;
            private set;
        }
        public NeatEvolutionAlgorithmParameters NeatEvolutionAlgorithmParameters
        {
            get;
            private set;
        }

        #endregion

        #region Public Methods

        public void Initialize(string name, XmlElement xmlConfig)
        {
            throw new NotImplementedException("xml isn't supported, use the strongly typed arguments");
        }
        /// <summary>
        /// This is my implementation that takes in a base args class instead of xml
        /// </summary>
        /// <remarks>
        /// Initialize must be called after the constructor.  After that, NeatEvolutionAlgorithmParameters and NeatGenomeParameters will be
        /// instantiated, and can be tweaked
        /// 
        /// Once all the properties are set the way you want, call CreateEvolutionAlgorithm():
        /// 
        ///     _experiment = new ExperimentBase_NEAT()     // or something that derives from this base
        ///     _experiment.Initialize(args, evaluator);
        ///     
        ///     _experiment.NeatGenomeParameters.??? = ???
        ///     _experiment.NeatEvolutionAlgorithmParameters.??? = ???
        /// 
        ///     _ea = _experiment.CreateEvolutionAlgorithm();
        /// </remarks>
        public void Initialize(string name, ExperimentInitArgs args, IPhenomeEvaluator<IBlackBox> phenomeEvaluator)
        {
            _phenomeEvaluator = phenomeEvaluator;

            Initialize_private(name, args);
        }
        /// <summary>
        /// This overload takes multiple phenome evaluators that evaluate over many ticks.  There are multiple, because they will
        /// each be loaded with a different neural net and run at the same time
        /// </summary>
        public void Initialize(string name, ExperimentInitArgs args, IPhenomeTickEvaluator<IBlackBox, NeatGenome>[] phenomeEvaluators, RoundRobinManager roundRobinManager, Func<double> worldTick)
        {
            _phenomeEvaluators = phenomeEvaluators;
            _phenometickeval_roundRobinManager = roundRobinManager;
            _phenometickeval_worldTick = worldTick;

            Initialize_private(name, args);
        }

        // Call CreateEvolutionAlgorithm after calling Initialize
        public NeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm()
        {
            return CreateEvolutionAlgorithm(DefaultPopulationSize);
        }
        public NeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm(int populationSize)
        {
            // Create a genome2 factory with our neat genome2 parameters object and the appropriate number of input and output neuron genes.
            IGenomeFactory<NeatGenome> genomeFactory = CreateGenomeFactory();

            // Create an initial population of randomly generated genomes.
            List<NeatGenome> genomeList = genomeFactory.CreateGenomeList(populationSize, 0);

            // Call more specific overload
            return CreateEvolutionAlgorithm_private(genomeFactory, genomeList);
        }
        public NeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm(IGenomeFactory<NeatGenome> genomeFactory, List<NeatGenome> genomeList)
        {
            return CreateEvolutionAlgorithm_private(genomeFactory, genomeList);
        }

        public NeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm(HyperNEAT_Args args)
        {
            return CreateEvolutionAlgorithm(DefaultPopulationSize, args);
        }
        public NeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm(int populationSize, HyperNEAT_Args args)
        {
            // Create a genome2 factory with our neat genome2 parameters object and the appropriate number of input and output neuron genes.
            IGenomeFactory<NeatGenome> genomeFactory = CreateGenomeFactory(args);

            // Create an initial population of randomly generated genomes.
            List<NeatGenome> genomeList = genomeFactory.CreateGenomeList(populationSize, 0);

            // Call more specific overload
            return CreateEvolutionAlgorithm_private(genomeFactory, genomeList, args);
        }
        public NeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm(IGenomeFactory<NeatGenome> genomeFactory, List<NeatGenome> genomeList, HyperNEAT_Args args)
        {
            return CreateEvolutionAlgorithm_private(genomeFactory, genomeList, args);
        }

        public IGenomeFactory<NeatGenome> CreateGenomeFactory()
        {
            if (IsHyperNEAT)
            {
                throw new InvalidOperationException("This method isn't supported when the experiment is hyper neat");
            }

            return new NeatGenomeFactory(InputCount, OutputCount, NeatGenomeParameters ?? GetNewNeatGenomeParameters(_activationDefinition));
        }
        public static IGenomeFactory<NeatGenome> CreateGenomeFactory(int inputCount, int outputCount, ExperimentInitArgs_Activation activation)
        {
            return new NeatGenomeFactory(inputCount, outputCount, GetNewNeatGenomeParameters(activation));
        }
        public IGenomeFactory<NeatGenome> CreateGenomeFactory(HyperNEAT_Args args)
        {
            if (!IsHyperNEAT)
            {
                throw new InvalidOperationException("This method can only be used when the experiment is hyper neat");
            }

            // The cppn gets handed pairs of points, and there is an extra neuron for something.  See Substrate.CreateNetworkDefinition()
            // Adding 1 to the dimension because a pair of points needs an extra dimension to separate them (2 2D squares would have Z=-1 and 1)
            // Multiplying by 2 because a pair of points is loaded into the cppn at a time
            // Adding the final 1 to store the pair's connection length
            int inputCount = ((args.InputShapeDimensions + 1) * 2) + 1;

            int outputCount = args.OutputShapeDimensions;

            //NOTE: Can't use args.NeuronCount_Input and args.NeuronCount_Output.  This genome factory is for the CPPN which
            //uses the dimensions of the positions as input/output size
            return new NeatGenomeFactory(inputCount, outputCount, DefaultActivationFunctionLibrary.CreateLibraryCppn(), NeatGenomeParameters ?? GetNewNeatGenomeParameters(_activationDefinition));
        }
        public static IGenomeFactory<NeatGenome> CreateGenomeFactory(HyperNEAT_Args args, ExperimentInitArgs_Activation activation)
        {
            // The cppn gets handed pairs of points, and there is an extra neuron for something.  See Substrate.CreateNetworkDefinition()
            // Adding 1 to the dimension because a pair of points needs an extra dimension to separate them (2 2D squares would have Z=-1 and 1)
            // Multiplying by 2 because a pair of points is loaded into the cppn at a time
            // Adding the final 1 to store the pair's connection length
            int inputCount = ((args.InputShapeDimensions + 1) * 2) + 1;

            int outputCount = args.OutputShapeDimensions;

            //NOTE: Can't use args.NeuronCount_Input and args.NeuronCount_Output.  This genome factory is for the CPPN which
            //uses the dimensions of the positions as input/output size
            return new NeatGenomeFactory(inputCount, outputCount, DefaultActivationFunctionLibrary.CreateLibraryCppn(), GetNewNeatGenomeParameters(activation));
        }

        /// <summary>
        /// This is a standard neat implementation (not hyperneat)
        /// </summary>
        public IGenomeDecoder<NeatGenome, IBlackBox> CreateGenomeDecoder()
        {
            if (IsHyperNEAT)
            {
                throw new InvalidOperationException("This NEAT overload can't be used when the experiment is for HyperNEAT");
            }

            return new NeatGenomeDecoder(_activationScheme);
        }
        public static IGenomeDecoder<NeatGenome, IBlackBox> CreateGenomeDecoder(ExperimentInitArgs_Activation activation)
        {
            return new NeatGenomeDecoder(GetActivationScheme(activation));
        }
        /// <summary>
        /// This is a hyper neat implementation
        /// </summary>
        /// <remarks>
        /// This is built from these two sources:
        /// http://www.nashcoding.com/2010/10/29/tutorial-%E2%80%93-evolving-neural-networks-with-sharpneat-2-part-3/
        /// SharpNeat.Domains.BoxesVisualDiscrimination.BoxesVisualDiscriminationExperiment.CreateGenomeDecoder()
        /// </remarks>
        public IGenomeDecoder<NeatGenome, IBlackBox> CreateGenomeDecoder(HyperNEAT_Args args)
        {
            if (!IsHyperNEAT)
            {
                throw new InvalidOperationException("This HyperNEAT overload can't be used when the experiment is for NEAT");
            }

            return CreateGenomeDecoder_Finish(args, _activationSchemeCppn, _activationScheme);
        }
        public static IGenomeDecoder<NeatGenome, IBlackBox> CreateGenomeDecoder(ExperimentInitArgs_Activation activation, HyperNEAT_Args args)
        {
            return CreateGenomeDecoder_Finish(args, GetActivationScheme_CPPN(), GetActivationScheme(activation));
        }

        private static IGenomeDecoder<NeatGenome, IBlackBox> CreateGenomeDecoder_Finish_ORIG(HyperNEAT_Args args, NetworkActivationScheme activationSchemeCppn, NetworkActivationScheme activationSchemeSubstrate)
        {
            throw new ApplicationException("obsolete");

            //if (args.InputShape != HyperNEAT_Shape.Square && args.InputShape != HyperNEAT_Shape.Square)
            //{
            //    throw new ApplicationException(string.Format("This method currently only supports square input and output sizes: Input={0}, Output={1}", args.InputShape, args.OutputShape));
            //}

            //if (args.InputCountXY != args.OutputCountXY)
            //{
            //    throw new ApplicationException(string.Format("This method currently only supports input and output counts that are the same: Input={0}, Output={1}", args.InputCountXY, args.OutputCountXY));
            //}

            //int numInputs = args.NeuronCount_Input;
            //int numOutputs = args.NeuronCount_Output;

            //SubstrateNodeSet inputLayer = new SubstrateNodeSet(numInputs);
            //SubstrateNodeSet outputLayer = new SubstrateNodeSet(numOutputs);

            ////NOTE: Can't put a neuron at the origin, because the bias node is there - see Substrate.CreateNetworkDefinition()

            //// Each node in each layer needs a unique ID
            //// Node IDs start at 1. (bias node is always zero)
            //// The input nodes use ID range { 1, inputSize^2 } and
            //// the output nodes are the next outputSize^2.
            //uint inputId = 1;
            //uint outputId = Convert.ToUInt32(numInputs + 1);

            //var inputCells = Math2D.GetCells_ORIG(args.InputSize, args.InputCountXY);
            //var outputCells = Math2D.GetCells_ORIG(args.OutputSize, args.OutputCountXY);

            //for (int y = 0; y < args.InputCountXY; y++)
            //{
            //    for (int x = 0; x < args.InputCountXY; x++)
            //    {
            //        //NOTE: This is currently hardcoded to the input and output arrays being the same size
            //        int index = (y * args.InputCountXY) + x;

            //        Point inputPoint = inputCells[index].Item2;
            //        Point outputPoint = outputCells[index].Item2;

            //        inputLayer.NodeList.Add(new SubstrateNode(inputId, new double[] { inputPoint.X, inputPoint.Y, -1d }));
            //        outputLayer.NodeList.Add(new SubstrateNode(outputId, new double[] { outputPoint.X, outputPoint.Y, 1d }));

            //        inputId++;
            //        outputId++;
            //    }
            //}

            //List<SubstrateNodeSet> nodeSetList = new List<SubstrateNodeSet>
            //{
            //    inputLayer,
            //    outputLayer
            //};

            ////TODO: The two samples that I copied from have the same number of inputs and outputs.  This mapping may be too simplistic when the counts are different
            ////TODO: Notes on using hidden layers:
            ////      The above example is a simple 2-layer CPPN with no hidden layers. If you want to add hidden layers, you simply need to create an additional SubstrateNodeSet
            ////      and add it to the nodeSetList. Then you will need two NodeSetMappings, one from the input (0) to the hidden (1) layer, and one from the hidden (1) to the
            ////      output (2) layer.3

            //// Define a connection mapping from the input layer to the output layer.
            //List<NodeSetMapping> nodeSetMappingList = new List<NodeSetMapping>();
            //nodeSetMappingList.Add(NodeSetMapping.Create
            //(
            //    //NOTE: Substrate is hardcoded to 0 and 1 for input and output.  2 and up is hidden nodes.  So passing these as params unnecessary
            //    0,      // this is the index into nodeSetList (the item in nodeSetList that is the input layer)
            //    1,      // index of nodeSetList that is the output layer

            //    (double?)null
            //));

            //// Construct the substrate using a steepened sigmoid as the phenome's activation function. All weights
            //// under .2 will not generate connections in the final phenome
            ////Substrate substrate = new Substrate(nodeSetList, DefaultActivationFunctionLibrary.CreateLibraryCppn(), 0, .2, 5, nodeSetMappingList);
            //Substrate substrate = new Substrate(nodeSetList, DefaultActivationFunctionLibrary.CreateLibraryNeat(SteepenedSigmoid.__DefaultInstance), 0, .2, 5, nodeSetMappingList);

            //// Create genome decoder. Decodes to a neural network packaged with
            //// an activation scheme that defines a fixed number of activations per evaluation.
            ////IGenomeDecoder<NeatGenome, IBlackBox> genomeDecoder = new HyperNeatDecoder(substrate, _activationSchemeCppn, _activationScheme, false);
            //return new HyperNeatDecoder(substrate, activationSchemeCppn, activationSchemeSubstrate, true);
        }
        private static IGenomeDecoder<NeatGenome, IBlackBox> CreateGenomeDecoder_Finish(HyperNEAT_Args args, NetworkActivationScheme activationSchemeCppn, NetworkActivationScheme activationSchemeSubstrate)
        {
            int numInputs = args.NeuronCount_Input;
            int numOutputs = args.NeuronCount_Output;

            SubstrateNodeSet inputLayer = new SubstrateNodeSet(numInputs);
            SubstrateNodeSet outputLayer = new SubstrateNodeSet(numOutputs);

            //NOTE: Can't put a neuron at the origin, because the bias node is there - see Substrate.CreateNetworkDefinition()

            // Each node in each layer needs a unique ID
            // Node IDs start at 1. (bias node is always zero)
            // The input nodes use ID range { 1, inputSize^2 } and
            // the output nodes are the next outputSize^2.
            uint inputId = 1;
            uint outputId = Convert.ToUInt32(numInputs + 1);

            for (int cntr = 0; cntr < args.InputPositions.Length; cntr++)
            {
                inputLayer.NodeList.Add(new SubstrateNode(inputId, args.InputPositions[cntr].ToArray()));
                inputId++;
            }

            for (int cntr = 0; cntr < args.OutputPositions.Length; cntr++)
            {
                outputLayer.NodeList.Add(new SubstrateNode(outputId, args.OutputPositions[cntr].ToArray()));
                outputId++;
            }

            List<SubstrateNodeSet> nodeSetList = new List<SubstrateNodeSet>
            {
                inputLayer,
                outputLayer
            };

            //TODO: The two samples that I copied from have the same number of inputs and outputs.  This mapping may be too simplistic when the counts are different
            //TODO: Notes on using hidden layers:
            //      The above example is a simple 2-layer CPPN with no hidden layers. If you want to add hidden layers, you simply need to create an additional SubstrateNodeSet
            //      and add it to the nodeSetList. Then you will need two NodeSetMappings, one from the input (0) to the hidden (1) layer, and one from the hidden (1) to the
            //      output (2) layer.3

            // Define a connection mapping from the input layer to the output layer.
            List<NodeSetMapping> nodeSetMappingList = new List<NodeSetMapping>();
            nodeSetMappingList.Add(NodeSetMapping.Create
            (
                //NOTE: Substrate is hardcoded to 0 and 1 for input and output.  2 and up is hidden nodes.  So passing these as params unnecessary
                0,      // this is the index into nodeSetList (the item in nodeSetList that is the input layer)
                1,      // index of nodeSetList that is the output layer

                (double?)null
            ));

            // Construct the substrate using a steepened sigmoid as the phenome's activation function. All weights
            // under .2 will not generate connections in the final phenome
            //Substrate substrate = new Substrate(nodeSetList, DefaultActivationFunctionLibrary.CreateLibraryCppn(), 0, .2, 5, nodeSetMappingList);
            Substrate substrate = new Substrate(nodeSetList, DefaultActivationFunctionLibrary.CreateLibraryNeat(SteepenedSigmoid.__DefaultInstance), 0, .2, 5, nodeSetMappingList);

            // Create genome decoder. Decodes to a neural network packaged with
            // an activation scheme that defines a fixed number of activations per evaluation.
            //IGenomeDecoder<NeatGenome, IBlackBox> genomeDecoder = new HyperNeatDecoder(substrate, _activationSchemeCppn, _activationScheme, false);
            return new HyperNeatDecoder(substrate, activationSchemeCppn, activationSchemeSubstrate, true);
        }

        /// <summary>
        /// This is a helper method that returns a neural net based on the genome
        /// </summary>
        public IBlackBox GetBlackBox(NeatGenome genome, HyperNEAT_Args hyperneatArgs = null)
        {
            IGenomeDecoder<NeatGenome, IBlackBox> decoder = null;

            if (hyperneatArgs == null)
            {
                decoder = CreateGenomeDecoder();
            }
            else
            {
                decoder = CreateGenomeDecoder(hyperneatArgs);
            }

            return decoder.Decode(genome);
        }
        public static IBlackBox GetBlackBox(NeatGenome genome, ExperimentInitArgs_Activation activation, HyperNEAT_Args hyperneatArgs = null)
        {
            IGenomeDecoder<NeatGenome, IBlackBox> decoder = null;

            if (hyperneatArgs == null)
            {
                decoder = CreateGenomeDecoder(activation);
            }
            else
            {
                decoder = CreateGenomeDecoder(activation, hyperneatArgs);
            }

            return decoder.Decode(genome);
        }

        //NOTE: These methods don't belong in experiment, they don't change it's state, and are just wrappers to static methods.  It's ugly having both
        //instance and static methods, but the INeatExperiment has the xml versions of the save/load
        public static List<NeatGenome> LoadPopulation(string xml, ExperimentInitArgs_Activation activation, int inputCount, int outputCount)
        {
            //TODO: activation, inputCount, outputCount could all be extracted from the xml, if it's too painful to also remember those settings

            List<NeatGenome> retVal = null;

            using (XmlReader xr = XmlReader.Create(new MemoryStream(Encoding.Unicode.GetBytes(xml))))
            {
                retVal = LoadPopulation_static(xr, activation, inputCount, outputCount);
            }

            return retVal;
        }
        public static List<NeatGenome> LoadPopulation(string xml, ExperimentInitArgs_Activation activation, HyperNEAT_Args hyperneatArgs)
        {
            List<NeatGenome> retVal = null;

            using (XmlReader xr = XmlReader.Create(new MemoryStream(Encoding.Unicode.GetBytes(xml))))
            {
                retVal = LoadPopulation_static(xr, activation, hyperneatArgs);
            }

            return retVal;
        }

        public List<NeatGenome> LoadPopulation(XmlReader xr)        // can't put an optional arg here because this method is part of the interface
        {
            return LoadPopulation_static(xr, _activationDefinition, InputCount, OutputCount);
        }
        public List<NeatGenome> LoadPopulation(XmlReader xr, HyperNEAT_Args hyperneatArgs)
        {
            return LoadPopulation_static(xr, _activationDefinition, hyperneatArgs);
        }

        public static string SavePopulation(IList<NeatGenome> genomeList)
        {
            StringBuilder xmlSB = new StringBuilder();

            using (var xw = XmlWriter.Create(xmlSB, new XmlWriterSettings() { Indent = true }))
            {
                SavePopulation_static(xw, genomeList);
            }

            return xmlSB.ToString();
        }
        public void SavePopulation(XmlWriter xw, IList<NeatGenome> genomeList)
        {
            SavePopulation_static(xw, genomeList);
        }

        #endregion

        #region Private Methods

        private void Initialize_private(string name, ExperimentInitArgs args)
        {
            Name = name;
            Description = args.Description;

            _inputCount = args.InputCount;
            _outputCount = args.OutputCount;

            DefaultPopulationSize = args.PopulationSize;

            IsHyperNEAT = args.IsHyperNEAT;
            if (args.IsHyperNEAT)
            {
                _activationSchemeCppn = GetActivationScheme_CPPN();
            }

            _activationDefinition = args.Activation;
            _activationScheme = GetActivationScheme(args.Activation);

            _complexityRegulationStr = args.Complexity_RegulationStrategy?.ToString();
            _complexityThreshold = args.Complexity_Threshold;

            _parallelOptions = args.MaxDegreeOfParallelism == null ?
                new ParallelOptions() :
                new ParallelOptions { MaxDegreeOfParallelism = args.MaxDegreeOfParallelism.Value };

            NeatEvolutionAlgorithmParameters = new NeatEvolutionAlgorithmParameters();
            NeatEvolutionAlgorithmParameters.SpecieCount = args.SpeciesCount;
        }

        private NeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm_private(IGenomeFactory<NeatGenome> genomeFactory, List<NeatGenome> genomeList, HyperNEAT_Args args = null)
        {
            // Create distance metric. Mismatched genes have a fixed distance of 10; for matched genes the distance is their weigth difference.
            IDistanceMetric distanceMetric = new ManhattanDistanceMetric(1, 0, 10);
            ISpeciationStrategy<NeatGenome> speciationStrategy = new ParallelKMeansClusteringStrategy<NeatGenome>(distanceMetric, _parallelOptions);

            // Create complexity regulation strategy.
            IComplexityRegulationStrategy complexityRegulationStrategy = ExperimentUtils.CreateComplexityRegulationStrategy(_complexityRegulationStr, _complexityThreshold);

            // Create the evolution algorithm.
            NeatEvolutionAlgorithm<NeatGenome> retVal = new NeatEvolutionAlgorithm<NeatGenome>(NeatEvolutionAlgorithmParameters, speciationStrategy, complexityRegulationStrategy);

            // Genome Decoder
            IGenomeDecoder<NeatGenome, IBlackBox> genomeDecoder = null;
            if (args == null)
            {
                genomeDecoder = CreateGenomeDecoder();
            }
            else
            {
                genomeDecoder = CreateGenomeDecoder(args);
            }

            // Create a genome list evaluator. This packages up the genome decoder with the genome evaluator.
            IGenomeListEvaluator<NeatGenome> genomeEvaluator = null;
            if (_phenomeEvaluator != null)
            {
                IGenomeListEvaluator<NeatGenome> innerEvaluator = new ParallelGenomeListEvaluator<NeatGenome, IBlackBox>(genomeDecoder, _phenomeEvaluator, _parallelOptions);

                // Wrap the list evaluator in a 'selective' evaluator that will only evaluate new genomes. That is, we skip re-evaluating any genomes
                // that were in the population in previous generations (elite genomes). This is determined by examining each genome's evaluation info object.
                genomeEvaluator = new SelectiveGenomeListEvaluator<NeatGenome>(
                    innerEvaluator,
                    SelectiveGenomeListEvaluator<NeatGenome>.CreatePredicate_OnceOnly());
            }
            else if (_phenomeEvaluators != null)
            {
                // Use the multi tick evaluator
                genomeEvaluator = new TickGenomeListEvaluator<NeatGenome, IBlackBox>(genomeDecoder, _phenomeEvaluators, _phenometickeval_roundRobinManager, _phenometickeval_worldTick);
            }
            else
            {
                throw new ApplicationException("One of the phenome evaluators needs to be populated");
            }

            // Initialize the evolution algorithm.
            retVal.Initialize(genomeEvaluator, genomeFactory, genomeList);

            // Finished. Return the evolution algorithm
            return retVal;
        }

        // For now, just hardcoding the cppn activation function
        private static NetworkActivationScheme GetActivationScheme_CPPN()
        {
            return GetActivationScheme(new ExperimentInitArgs_Activation_CyclicFixedTimesteps()
            {
                TimestepsPerActivation = 4,
                FastFlag = true
            });
        }
        /// <summary>
        /// This is inspired by ExperimentUtils.CreateActivationScheme, but can't be put there, because NeatConfigInfo_Activation isn't
        /// defined that low
        /// </summary>
        private static NetworkActivationScheme GetActivationScheme(ExperimentInitArgs_Activation scheme)
        {
            if (scheme == null)
            {
                throw new ArgumentNullException("scheme");
            }
            else if (scheme is ExperimentInitArgs_Activation_Acyclic)
            {
                return NetworkActivationScheme.CreateAcyclicScheme();
            }
            else if (scheme is ExperimentInitArgs_Activation_CyclicFixedTimesteps)
            {
                ExperimentInitArgs_Activation_CyclicFixedTimesteps cast = (ExperimentInitArgs_Activation_CyclicFixedTimesteps)scheme;
                return NetworkActivationScheme.CreateCyclicFixedTimestepsScheme(cast.TimestepsPerActivation, cast.FastFlag);
            }
            else if (scheme is ExperimentInitArgs_Activation_CyclicRelaxing)
            {
                ExperimentInitArgs_Activation_CyclicRelaxing cast = (ExperimentInitArgs_Activation_CyclicRelaxing)scheme;
                return NetworkActivationScheme.CreateCyclicRelaxingActivationScheme(cast.SignalDeltaThreshold, cast.MaxTimesteps, cast.FastFlag);
            }
            else
            {
                throw new ArgumentException("Unknown scheme type: " + scheme.GetType().ToString());
            }
        }

        private static NeatGenomeParameters GetNewNeatGenomeParameters(ExperimentInitArgs_Activation activation)
        {
            return new NeatGenomeParameters
            {
                FeedforwardOnly = activation is ExperimentInitArgs_Activation_Acyclic      // if this is false while activation is acyclic, then the genome generator will create the wrong types of genomes, and later code with throw an exception
            };
        }

        private static List<NeatGenome> LoadPopulation_static(XmlReader xr, ExperimentInitArgs_Activation activation, int inputCount, int outputCount)
        {
            NeatGenomeFactory genomeFactory = (NeatGenomeFactory)CreateGenomeFactory(inputCount, outputCount, activation);
            return NeatGenomeXmlIO.ReadCompleteGenomeList(xr, false, genomeFactory);
        }
        private static List<NeatGenome> LoadPopulation_static(XmlReader xr, ExperimentInitArgs_Activation activation, HyperNEAT_Args hyperneatArgs)
        {
            NeatGenomeFactory genomeFactory = (NeatGenomeFactory)CreateGenomeFactory(hyperneatArgs, activation);
            return NeatGenomeXmlIO.ReadCompleteGenomeList(xr, false, genomeFactory);
        }
        private static void SavePopulation_static(XmlWriter xw, IList<NeatGenome> genomeList)
        {
            // Writing node IDs is not necessary for NEAT.
            NeatGenomeXmlIO.WriteComplete(xw, genomeList, false);
        }

        #endregion
    }

    #region ExperimentInitArgs Classes

    /// <summary>
    /// INeatExperiment.Initialize takes in xml, but that is tedious to use.  This class holds the same properties that is expected of the xml.
    /// Derive this class if more properties are needed
    /// </summary>
    public class ExperimentInitArgs
    {
        // Input/Output Count are for NEAT only.  HyperNEAT is more dynamic
        public int InputCount { get; set; }
        public int OutputCount { get; set; }

        public int PopulationSize { get; set; }
        public int SpeciesCount { get; set; }

        public ExperimentInitArgs_Activation Activation { get; set; }
        /// <summary>
        /// If true, then InputCount/OutputCount are variable
        /// </summary>
        /// <remarks>
        /// For now, the activation function of the CPPN is hardcoded, it can always be added as a property if the default isn't good enough
        /// 
        /// http://eplex.cs.ucf.edu/hyperNEATpage/HyperNEAT.html
        /// </remarks>
        public bool IsHyperNEAT { get; set; }

        // These two need to be null or nonnull together
        public ComplexityCeilingType? Complexity_RegulationStrategy { get; set; }
        public int? Complexity_Threshold { get; set; }

        public string Description { get; set; }

        public int? MaxDegreeOfParallelism { get; set; }
    }

    // These set of classes are treated like they are an enum with a few extra props

    public abstract class ExperimentInitArgs_Activation
    {
        // no properties, this is just a placeholder for a derived version
    }

    /// <summary>
    /// Acyclic is a simple feed forword neural net.  The network is stateless (output is purely based on the input given in an
    /// activation tick)
    /// </summary>
    public class ExperimentInitArgs_Activation_Acyclic : ExperimentInitArgs_Activation
    {
        // acylic doesn't have any extra params
    }

    /// <summary>
    /// Cyclic is a recurrent neural net (output feeds into input, so it has memory from previous runs)
    /// </summary>
    public class ExperimentInitArgs_Activation_CyclicFixedTimesteps : ExperimentInitArgs_Activation
    {
        /// <summary>
        /// This is how many times to privately run through the network each activation tick
        /// </summary>
        /// <remarks>
        /// Not sure what dictates needing more than one.  Values in the sharpneat samples vary from 1 to 4
        /// </remarks>
        public int TimestepsPerActivation { get; set; }
        /// <summary>
        /// Use true unless you want to debug into sharpneat's implementation
        /// </summary>
        public bool FastFlag { get; set; }
    }

    /// <summary>
    /// Instead of privately doing a fixed number of steps each activation tick, this will keep going until the output settles down
    /// </summary>
    public class ExperimentInitArgs_Activation_CyclicRelaxing : ExperimentInitArgs_Activation
    {
        public double SignalDeltaThreshold { get; set; }
        public int MaxTimesteps { get; set; }
        /// <summary>
        /// Use true unless you want to debug into sharpneat's implementation
        /// </summary>
        public bool FastFlag { get; set; }
    }

    #endregion
    #region HyperNEAT Classes

    //public enum HyperNEAT_Shape
    //{
    //    Line,
    //    Square,
    //    Cube,
    //    CirclePerimiter,
    //    CircleArea,
    //    SphereShell,
    //    SphereFilled,
    //}

    public class HyperNEAT_Args
    {
        //public HyperNEAT_Shape InputShape { get; set; }
        //public HyperNEAT_Shape OutputShape { get; set; }

        // This is the number of neurons in the input and output layer.  It will be up to the implementer to ensure they will be layed out in a consistent way
        //public int NeuronCount_Input { get; set; }
        //public int NeuronCount_Output { get; set; }

        //TODO: Come up with a way to use the above properties instead of these properties, which are hardcoded for cubes
        //public int InputCountXY { get; set; }
        //public int OutputCountXY { get; set; }

        // These will have different meaning for different shapes.  Cube would be the length of the sides.  Sphere would be radius (though diameter would be more consistent with the cube's sides)
        //public double InputSize { get; set; }
        //public double OutputSize { get; set; }

        //public int NeuronCount_Input => GetNeuronCount(InputShape, InputCountXY);
        //public int NeuronCount_Output => GetNeuronCount(OutputShape, OutputCountXY);

        //public int InputShapeDimensions => GetPositionDimensions(InputShape);
        //public int OutputShapeDimensions => GetPositionDimensions(OutputShape);

        public VectorND[] InputPositions { get; set; }
        public VectorND[] OutputPositions { get; set; }

        public int NeuronCount_Input => InputPositions.Length;
        public int NeuronCount_Output => OutputPositions.Length;

        public int InputShapeDimensions => InputPositions[0].Size;
        public int OutputShapeDimensions => OutputPositions[0].Size;

        #region Public Methods

        public static (VectorND[] inputs, VectorND[] outputs) GetSquareSheets(double inputSize, double outputSize, int inputCountXY, int outputCountXY)
        {
            return
            (
                inputs: Math2D.GetCells_WithinSquare(inputSize, inputCountXY).
                    Select(o => new VectorND(o.center.X, o.center.Y, -1d)).
                    ToArray(),

                outputs: Math2D.GetCells_WithinSquare(outputSize, outputCountXY).
                    Select(o => new VectorND(o.center.X, o.center.Y, 1d)).
                    ToArray()
            );
        }

        #endregion

        #region Private Methods

        //private static int GetNeuronCount(HyperNEAT_Shape shape, int countXY)
        //{
        //    switch (shape)
        //    {
        //        case HyperNEAT_Shape.Square:
        //            return countXY * countXY;

        //        default:
        //            throw new ApplicationException("This HyperNEAT_Shape currently isn't supported: " + shape);
        //    }
        //}

        //private static int GetPositionDimensions(HyperNEAT_Shape shape)
        //{
        //    switch (shape)
        //    {
        //        case HyperNEAT_Shape.Line:
        //            // 1D
        //            return 1;

        //        case HyperNEAT_Shape.Square:
        //        case HyperNEAT_Shape.CircleArea:
        //        case HyperNEAT_Shape.CirclePerimiter:
        //            // 2D
        //            return 2;

        //        case HyperNEAT_Shape.Cube:
        //        case HyperNEAT_Shape.SphereFilled:
        //        case HyperNEAT_Shape.SphereShell:
        //            // 3D
        //            return 3;

        //        default:
        //            throw new ApplicationException("Unknown HyperNEAT_Shape: " + shape.ToString());
        //    }
        //}

        #endregion
    }

    #endregion
}
