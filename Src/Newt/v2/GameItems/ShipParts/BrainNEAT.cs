using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Game.HelperClassesAI;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Controls3D;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.NewtonDynamics;
using SharpNeat.Core;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;

namespace Game.Newt.v2.GameItems.ShipParts
{
    #region class: BrainNEATToolItem

    public class BrainNEATToolItem : PartToolItemBase
    {
        #region Constructor

        public BrainNEATToolItem(EditorOptions options)
            : base(options)
        {
            TabName = PartToolItemBase.TAB_SHIPPART;
            _visual2D = PartToolItemBase.GetVisual2D(Name, Description, options, this);
        }

        #endregion

        #region Public Properties

        public override string Name
        {
            get
            {
                return "Brain NEAT";
            }
        }
        public override string Description
        {
            get
            {
                return "Consumes energy, makes decisions.  This is a wrapper around a sharpNEAT brain";
            }
        }
        public override string Category
        {
            get
            {
                return PartToolItemBase.CATEGORY_BRAIN;
            }
        }

        private UIElement _visual2D = null;
        public override UIElement Visual2D
        {
            get
            {
                return _visual2D;
            }
        }

        #endregion

        #region Public Methods

        public override PartDesignBase GetNewDesignPart()
        {
            return new BrainNEATDesign(Options, false);
        }

        #endregion
    }

    #endregion
    #region class: BrainNEATDesign

    public class BrainNEATDesign : PartDesignBase
    {
        #region Declaration Section

        private const double SCALE = .75d;

        public const PartDesignAllowedScale ALLOWEDSCALE = PartDesignAllowedScale.XYZ;		// This is here so the scale can be known through reflection

        private MassBreakdownCache _massBreakdown = null;

        #endregion

        #region Constructor

        public BrainNEATDesign(EditorOptions options, bool isFinalModel)
            : base(options, isFinalModel) { }

        #endregion

        #region Public Properties

        public override PartDesignAllowedScale AllowedScale
        {
            get
            {
                return ALLOWEDSCALE;
            }
        }
        public override PartDesignAllowedRotation AllowedRotation
        {
            get
            {
                return PartDesignAllowedRotation.X_Y_Z;
            }
        }

        private Model3D _model = null;
        public override Model3D Model
        {
            get
            {
                if (_model == null)
                {
                    _model = CreateGeometry(IsFinalModel);
                }

                return _model;
            }
        }

        #endregion

        #region Public Methods

        public override ShipPartDNA GetDNA()
        {
            BrainNEATDNA retVal = new BrainNEATDNA();

            base.FillDNA(retVal);

            return retVal;
        }
        public override void SetDNA(ShipPartDNA dna)
        {
            if (!(dna is BrainNEATDNA))
            {
                throw new ArgumentException("The class passed in must be " + nameof(BrainNEATDNA));
            }

            base.StoreDNA(dna);
        }

        public override CollisionHull CreateCollisionHull(WorldBase world)
        {
            Transform3DGroup transform = new Transform3DGroup();
            //transform.Children.Add(new ScaleTransform3D(scale));		// it ignores scale
            transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(Orientation)));
            transform.Children.Add(new TranslateTransform3D(Position.ToVector()));

            Vector3D size = new Vector3D(Scale.X * SCALE * .5d, Scale.Y * SCALE * .5d, Scale.Z * SCALE * .5d);

            return CollisionHull.CreateSphere(world, 0, size, transform.Value);
        }
        public override UtilityNewt.IObjectMassBreakdown GetMassBreakdown(double cellSize)
        {
            if (_massBreakdown != null && _massBreakdown.Scale == Scale && _massBreakdown.CellSize == cellSize)
            {
                // This has already been built for this size
                return _massBreakdown.Breakdown;
            }

            // Convert this.Scale into a size that the mass breakdown will use
            Vector3D size = new Vector3D(Scale.X * SCALE, Scale.Y * SCALE, Scale.Z * SCALE);

            var breakdown = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Sphere, UtilityNewt.MassDistribution.Uniform, size, cellSize);

            // Store this
            _massBreakdown = new MassBreakdownCache(breakdown, Scale, cellSize);

            return _massBreakdown.Breakdown;
        }

        public override PartToolItemBase GetToolItem()
        {
            return new BrainNEATToolItem(Options);
        }

        #endregion

        #region Private Methods

        private Model3D CreateGeometry(bool isFinal)
        {
            Model3DGroup retVal = new Model3DGroup();

            #region box

            MaterialGroup material = new MaterialGroup();

            Color ringColor = WorldColors.DirectionControllerRing_Color;
            DiffuseMaterial diffuse = new DiffuseMaterial(new SolidColorBrush(ringColor));
            MaterialBrushes.Add(new MaterialColorProps(diffuse, ringColor));
            material.Children.Add(diffuse);

            SpecularMaterial specular = WorldColors.DirectionControllerRing_Specular;
            MaterialBrushes.Add(new MaterialColorProps(specular));
            material.Children.Add(specular);

            if (!isFinal)
            {
                EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
                material.Children.Add(selectionEmissive);
                SelectionEmissives.Add(selectionEmissive);
            }

            GeometryModel3D geometry = new GeometryModel3D
            {
                Material = material,
                BackMaterial = material,

                Geometry = ConverterMatterToFuelDesign.GetMesh(.36d * SCALE, .04d * SCALE, 3),
            };

            retVal.Children.Add(geometry);

            #endregion

            #region brain

            ScaleTransform3D scaleTransform = new ScaleTransform3D(SCALE, SCALE, SCALE);

            if (!isFinal)
            {
                retVal.Children.AddRange(BrainDesign.CreateInsideVisuals(SCALE * .6, MaterialBrushes, SelectionEmissives, scaleTransform));
            }

            retVal.Children.Add(BrainDesign.CreateShellVisual(isFinal, MaterialBrushes, SelectionEmissives, scaleTransform));

            #endregion

            retVal.Transform = GetTransformForGeometry(isFinal);

            return retVal;
        }

        #endregion
    }

    #endregion
    #region class: BrainNEAT

    /// <summary>
    /// This is a brain that is a wrapper to a sharpneat brain.  Every tick it takes the inputs, feeds them to the sharpneat brain, then sets those
    /// outputs to the external outputs
    /// </summary>
    public class BrainNEAT : PartBase, INeuronContainer, IPartUpdatable
    {
        #region class: NeuronMapping

        private class NeuronMapping
        {
            public int Index_External { get; set; }
            public int Index_NEAT { get; set; }
            public double Weight { get; set; }

            public override string ToString()
            {
                return string.Format("ext - int: {0} - {1} x {2}", Index_External, Index_NEAT, Weight);
            }
        }

        #endregion

        #region Declaration Section

        public const string PARTTYPE = nameof(BrainNEAT);

        private readonly ItemOptions _itemOptions;

        private readonly IContainer _energyTanks;

        private readonly Neuron_NegPos[] _neuronsInput;
        private readonly Neuron_SensorPosition[] _neuronsOutput;
        private readonly INeuron[] _neuronsAll;

        private readonly double _volume;        // this is used to calculate energy draw

        // This is the trained neat brain
        private IBlackBox _brain = null;

        NeuronMapping[] _inputsMap = null;
        NeuronMapping[] _outputsMap = null;

        // IBlackBox can't be directly serialized/deserialized.  There's a chance that this brain will get trained in an offline bot, then stay
        // frozen for all decendants.  Or for other reasons, have the need for the trained brain to be ready at the time of this part's construction
        //
        // So these properties are the minimum necessary to instantiate the IBlackBox.  Note that neat vs hyperneat need a few props
        // specific to them

        private NeatGenome _genome = null;
        /// <summary>
        /// When this is for hyperneat, it is the activation of the final IBlackBox.  The CPPN has it's own activation function, which is handled
        /// by the experiment class (not stored here)
        /// </summary>
        private ExperimentInitArgs_Activation _activation = null;

        // This is only used when it's hyperneat
        private HyperNEAT_Args _hyperneatArgs = null;

        // These are the positions at the time that the neat brain was trained.  They help with mapping to the current external neuron
        // positions
        private Point3D[] _neatPositions_Input = null;
        private Point3D[] _neatPositions_Output = null;

        #endregion

        #region Constructor

        public BrainNEAT(EditorOptions options, ItemOptions itemOptions, BrainNEATDNA dna, IContainer energyTanks)
            : base(options, dna, itemOptions.Brain_Damage.HitpointMin, itemOptions.Brain_Damage.HitpointSlope, itemOptions.Brain_Damage.Damage)
        {
            _itemOptions = itemOptions;
            _energyTanks = energyTanks;

            Design = new BrainNEATDesign(options, true);
            Design.SetDNA(dna);

            if(dna == null || dna.UniqueID == Guid.Empty)
            {
                _uniqueID = Guid.NewGuid();
            }
            else
            {
                _uniqueID = dna.UniqueID;
            }

            Brain.GetMass(out _mass, out _volume, out double radius, out _scaleActual, dna, itemOptions);
            Radius = radius;

            //var neurons = CreateNeurons_HARDCODED(dna, itemOptions);
            var neurons = CreateNeurons(dna, itemOptions);
            _neuronsInput = neurons.input;
            _neuronsOutput = neurons.output;
            _neuronsAll = UtilityCore.Iterate<INeuron>(_neuronsInput, _neuronsOutput).ToArray();

            BuildNEATBrain(dna);
        }

        #endregion

        #region INeuronContainer Members

        public IEnumerable<INeuron> Neruons_Readonly => _neuronsOutput;
        public IEnumerable<INeuron> Neruons_ReadWrite => Enumerable.Empty<INeuron>();
        public IEnumerable<INeuron> Neruons_Writeonly => _neuronsInput;

        public IEnumerable<INeuron> Neruons_All => _neuronsAll;

        public NeuronContainerType NeuronContainerType => NeuronContainerType.Brain;

        public double Radius
        {
            get;
            private set;
        }

        private volatile bool _isOn = false;
        public bool IsOn => _isOn;

        #endregion
        #region IPartUpdatable Members

        public void Update_MainThread(double elapsedTime)
        {
        }
        public void Update_AnyThread(double elapsedTime)
        {
            if (IsDestroyed ||
                _energyTanks == null ||
                _brain == null ||
                _energyTanks.RemoveQuantity(elapsedTime * _volume * _itemOptions.Brain_AmountToDraw * ItemOptions.ENERGYDRAWMULT, true) > 0d)
            {
                _isOn = false;

                foreach (Neuron_SensorPosition neuron in _neuronsOutput)
                {
                    neuron.Value = 0;
                }
            }
            else
            {
                _isOn = true;

                // Inputs
                double[] inputArr = new double[_brain.InputCount];

                foreach (NeuronMapping link in _inputsMap)
                {
                    inputArr[link.Index_NEAT] += _neuronsInput[link.Index_External].Value * link.Weight;
                }

                _brain.InputSignalArray.CopyFrom(inputArr, 0);

                // Brain.Tick
                _brain.Activate();

                // Outputs
                double[] outputArr1 = new double[_brain.OutputCount];

                _brain.OutputSignalArray.CopyTo(outputArr1, 0);

                double[] outputArr2 = new double[_neuronsOutput.Length];

                foreach (NeuronMapping link in _outputsMap)
                {
                    outputArr2[link.Index_External] += outputArr1[link.Index_NEAT] * link.Weight;
                }

                for (int cntr = 0; cntr < outputArr2.Length; cntr++)
                {
                    _neuronsOutput[cntr].Value = outputArr2[cntr];
                }
            }
        }

        public int? IntervalSkips_MainThread => null;
        public int? IntervalSkips_AnyThread => 0;

        #endregion

        #region Public Properties

        private readonly double _mass;
        public override double DryMass => _mass;
        public override double TotalMass => _mass;

        private readonly Vector3D _scaleActual;
        public override Vector3D ScaleActual => _scaleActual;

        private readonly Guid _uniqueID;
        public Guid UniqueID => _uniqueID;

        #endregion

        #region Public Methods

        public override ShipPartDNA GetNewDNA()
        {
            BrainNEATDNA retVal = (BrainNEATDNA)Design.GetDNA();

            retVal.UniqueID = UniqueID;

            //NOTE: The design class doesn't hold neurons, since it's only used by the editor, so fill out the rest of the dna here
            retVal.Neurons = _neuronsAll.
                Select(o => o.Position).
                ToArray();

            if (_genome != null)
            {
                retVal.Activation = _activation;
                retVal.NEATPositions_Input = _neatPositions_Input;
                retVal.NEATPositions_Output = _neatPositions_Output;
                retVal.Hyper = _hyperneatArgs;
                retVal.Genome = ExperimentNEATBase.SavePopulation(new[] { _genome });
            }

            return retVal;
        }

        /// <summary>
        /// This replaces the internal neural net with a new one.  If the part is created with dna that has an internal NN, then there is
        /// no need to call this method
        /// </summary>
        /// <remarks>
        /// TODO: For hyperneat, see if genome is CPPN (it should be).  Then is activation cppn or final?
        /// </remarks>
        /// <param name="phenome">
        /// This is the actual neural net
        /// NOTE: If this is hyperneat, the phenome is still the final neural net, not the cppn
        /// </param>
        public void SetPhenome(IBlackBox phenome, NeatGenome genome, ExperimentInitArgs_Activation activation, HyperNEAT_Args hyperneatArgs = null)
        {
            if (phenome.InputCount != _neuronsInput.Length || phenome.OutputCount != _neuronsOutput.Length)
            {
                // Without this constraint, it would be difficult to map between external and phenome, because it can't be known
                // what the phenome's intended neuron positions would be.  It's expected that during training, a candidate phenome
                // will be placed into a BrainNEAT, then the bot will be tested and scored.  So that during training there is always a
                // 1:1 between external and phenome.  It's only future generations that mutate where a fuzzy mapping needs to be
                // done
                throw new ArgumentException(string.Format("The phenome passed in must have the same number of inputs and outputs as this wrapper brain's external inputs and outputs. Phenome ({0}, {1}), External ({2}, {3})", phenome.InputCount, phenome.OutputCount, _neuronsInput.Length, _neuronsOutput.Length));
            }

            _brain = phenome;
            _genome = genome;
            _activation = activation;
            _hyperneatArgs = hyperneatArgs;

            _neatPositions_Input = _neuronsInput.
                Select(o => o.Position).
                ToArray();

            _neatPositions_Output = _neuronsOutput.
                Select(o => o.Position).
                ToArray();

            // It's a 1:1 mapping between external and internal
            _inputsMap = Enumerable.Range(0, _neuronsInput.Length).
                Select(o => new NeuronMapping()
                {
                    Index_External = o,
                    Index_NEAT = o,
                    Weight = 1d,
                }).
                ToArray();

            _outputsMap = Enumerable.Range(0, _neuronsOutput.Length).
                Select(o => new NeuronMapping()
                {
                    Index_External = o,
                    Index_NEAT = o,
                    Weight = 1d,
                }).
                ToArray();
        }

        #endregion

        #region Private Methods

        private static (Neuron_NegPos[] input, Neuron_SensorPosition[] output) CreateNeurons_HARDCODED(ShipPartDNA dna, ItemOptions itemOptions)
        {
            // Make an evenly distributed sherical shell of neurons.  Inputs with a positive Z coord, Outputs with a negative Z coord,
            // and a band with no neurons around the equator

            //dna.Neurons

            return
            (
                Brain.GetNeuronPositions_Line2D(null, 16, 1, z: -1).
                    Select(o => new Neuron_NegPos(o.ToPoint())).
                    ToArray(),

                Brain.GetNeuronPositions_Line2D(null, 16, 1, z: 1).
                    Select(o => new Neuron_SensorPosition(o.ToPoint(), false)).
                    ToArray()
            );
        }
        /// <summary>
        /// inputs are +Z, outputs are -Z
        /// </summary>
        private static (Neuron_NegPos[] input, Neuron_SensorPosition[] output) CreateNeurons(ShipPartDNA dna, ItemOptions itemOptions)
        {
            const double MAXANGLE = 83;

            GetNeuronVolume(out double radius, out double volume, dna, itemOptions);

            int inputCount = Math.Max(1, (volume * itemOptions.BrainNEAT_NeuronDensity_Input).ToInt_Round());
            int outputCount = Math.Max(1, (volume * itemOptions.BrainNEAT_NeuronDensity_Output).ToInt_Round());

            Point3D[] inputs = null;
            Point3D[] outputs = null;
            if (dna.Neurons != null)
            {
                inputs = dna.Neurons.
                    Where(o => o.Z >= 0).       // there's a band around the equator that has no neurons, so even minor mutations shouldn't cross over
                    ToArray();

                outputs = dna.Neurons.
                    Where(o => o.Z < 0).
                    ToArray();
            }

            //NOTE: This will repair positions to keep neurons evenly spaced.  Other parts are more allowing of mutation, but this is a wrapper to a trained
            //neural net, so mutation would just unnecessarily blur the signals - but even with the position repair, there could be drift over many generations,
            //especially if scale causes neuron counts to change
            inputs = GetNeuronPositions(inputs, inputCount, radius, new Vector3D(0, 0, 1), MAXANGLE);
            outputs = GetNeuronPositions(outputs, outputCount, radius, new Vector3D(0, 0, -1), MAXANGLE);

            return
            (
                inputs.
                    Select(o => new Neuron_NegPos(o)).
                    ToArray(),

                outputs.
                    Select(o => new Neuron_SensorPosition(o, false)).
                    ToArray()
            );
        }

        private static void GetNeuronVolume(out double radius, out double volume, ShipPartDNA dna, ItemOptions itemOptions)
        {
            //NOTE: This radius isn't taking SCALE into account.  The other neural parts do this as well, so the neural density properties can be more consistent
            radius = (dna.Scale.X + dna.Scale.Y + dna.Scale.Z) / (3d * 2d);		// xyz should all be the same anyway
            volume = Math.Pow(radius, itemOptions.Brain_NeuronGrowthExponent);
        }

        private static Point3D[] GetNeuronPositions(Point3D[] dnaPositions, int count, double radius, Vector3D axis, double maxAngle)
        {
            //NOTE: This was copied from Brain.GetNeuronPositionsInitial.  It would be difficult to use that one because Math3D.GetRandomVectors_Cone
            //is optimized to do many points at once (it does the expensive prep work once at the beginning)

            //TODO: When reducing/increasing, it is currently just being random.  It may be more realistic to take proximity into account.  Play
            //a variant of conway's game of life or something

            Vector3D[] retVal;
            bool shouldRepair = true;

            if (dnaPositions == null)
            {
                #region create new

                retVal = Math3D.GetRandomVectors_Cone_EvenDist(count, axis, maxAngle, radius, radius);

                shouldRepair = false;

                #endregion
            }
            else if (dnaPositions.Length > count)
            {
                #region reduce

                List<Vector3D> posList = dnaPositions.Select(o => o.ToVector()).ToList();

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

                List<Vector3D> posList = dnaPositions.Select(o => o.ToVector()).ToList();

                int increaseCount = count - dnaPositions.Length;

                posList.AddRange(Math3D.GetRandomVectors_Cone(increaseCount, axis, 0, maxAngle, radius, radius));

                retVal = posList.ToArray();

                #endregion
            }
            else
            {
                #region copy as is

                retVal = dnaPositions.Select(o => o.ToVector()).ToArray();

                #endregion
            }

            if (shouldRepair)
            {
                retVal = Math3D.GetRandomVectors_Cone_EvenDist(retVal, axis, maxAngle, radius, radius);
            }

            return retVal.
                Select(o => o.ToPoint()).
                ToArray();
        }

        private void BuildNEATBrain(BrainNEATDNA dna)
        {
            var nn = DeserializeBrain(dna);
            if (nn == null)
            {
                return;
            }

            _inputsMap = MapNeurons(_neuronsInput.Select(o => o.Position).ToArray(), dna.NEATPositions_Input);
            _outputsMap = MapNeurons(_neuronsOutput.Select(o => o.Position).ToArray(), dna.NEATPositions_Output);

            _neatPositions_Input = dna.NEATPositions_Input;
            _neatPositions_Output = dna.NEATPositions_Output;

            _activation = dna.Activation;
            _hyperneatArgs = dna.Hyper;

            _genome = nn.Value.genome;
            _brain = nn.Value.phenome;
        }

        private static NeuronMapping[] MapNeurons(Point3D[] externalPoints, Point3D[] internalPoints)
        {
            // FuzzyLink wasn't designed for from and to points to be sitting on top of each other, so need to pull them apart
            //
            // external is pulled to -Z, internal is +Z.  These offset coordinates don't have any meaning outside this function, they are just
            // a hack to allow FuzzyLink to work properly

            // Figure out how far to separate them to ensure they are fully separated
            var aabb = Math3D.GetAABB(externalPoints.Concat(internalPoints));
            double offsetDist = Math1D.Max(aabb.Item2.X - aabb.Item1.X, aabb.Item2.Y - aabb.Item1.Y, aabb.Item2.Z - aabb.Item1.Z);
            offsetDist *= 3;
            Vector3D offset = new Vector3D(0, 0, offsetDist);

            // Create seed links.  The easiest approach is just to create one per internal point and then let the fuzzy linker find the best
            // external point
            //
            // I could see the argument for remembering the links across generations so that if the external points drift around too much,
            // the original linking will better persist.  But if the bot is mutating that much over generations, the neat NN should be retrained
            // from time to time.  Also, if a mismapping causes the bot to perform bad, then it won't be making children (and a mismapping
            // might end up causing better performance)
            Tuple<Point3D, Point3D, double>[] initialLinks = internalPoints.
                Select(o => Tuple.Create(o - offset, o + offset, 1d)).
                ToArray();

            Point3D[] pointsForFuzzy = externalPoints.Select(o => o - offset).
                Concat(internalPoints.Select(o => o + offset)).
                ToArray();

            // Allow a few more links than points.  If the external and internal points are aligned well, then the extra link allowance won't
            // be used
            int numLinks = (internalPoints.Length * 1.1).ToInt_Ceiling();

            var finalLinks = ItemLinker.FuzzyLink(initialLinks, pointsForFuzzy, numLinks, 6);


            const double THICKNESS = .005;
            const double DOT = THICKNESS * 3;

            Debug3DWindow window = new Debug3DWindow();

            window.AddDots(externalPoints, DOT, Colors.IndianRed);
            window.AddDots(internalPoints, DOT, Colors.DodgerBlue);

            window.AddDots(pointsForFuzzy, DOT, Colors.Silver);
            window.AddLines(initialLinks.Select(o => (o.Item1, o.Item2)), THICKNESS, Colors.Orchid);


            List<NeuronMapping> retVal = new List<NeuronMapping>();

            foreach (var link in finalLinks)
            {
                int? externalIndex = null;
                int? internalIndex = null;

                foreach (int index in new[] { link.Item1, link.Item2 })
                {
                    if (index < externalPoints.Length)
                    {
                        externalIndex = index;
                    }
                    else
                    {
                        internalIndex = index - externalPoints.Length;
                    }
                }

                if (externalIndex == null || internalIndex == null)
                {
                    // This should never happen in practice, the internal and external sets are pulled too far apart to accidentally be linked together.
                    // Just ignore this link
                    continue;
                }

                retVal.Add(new NeuronMapping()
                {
                    Index_External = externalIndex.Value,
                    Index_NEAT = internalIndex.Value,
                    Weight = link.Item3,
                });
            }


            foreach (var link in finalLinks)
            {
                window.AddLine(pointsForFuzzy[link.Item1], pointsForFuzzy[link.Item2], THICKNESS * link.Item3, Colors.GhostWhite);
            }
            foreach (var link in retVal)
            {
                window.AddLine(externalPoints[link.Index_External], internalPoints[link.Index_NEAT], THICKNESS * link.Weight, Colors.Coral);
            }

            window.Show();

            return retVal.ToArray();
        }

        /// <summary>
        /// IBlackBox can't be directly deserialized, so it takes a village of properties to construct it
        /// </summary>
        private static (IBlackBox phenome, NeatGenome genome)? DeserializeBrain(BrainNEATDNA dna)
        {
            if (dna == null)
            {
                return null;
            }
            else if (string.IsNullOrEmpty(dna.Genome) || dna.Activation == null)
            {
                return null;
            }

            List<NeatGenome> genomeList = null;
            try
            {
                // Deserialize the genome (sharpneat library has a custom serialize/deserialize)
                if (dna.Hyper == null)
                {
                    genomeList = ExperimentNEATBase.LoadPopulation(dna.Genome, dna.Activation, dna.NEATPositions_Input.Length, dna.NEATPositions_Output.Length);
                }
                else
                {
                    genomeList = ExperimentNEATBase.LoadPopulation(dna.Genome, dna.Activation, dna.Hyper);
                }

                if (genomeList == null || genomeList.Count == 0)
                {
                    return null;
                }

                // Construct the phenome (the actual instance of a neural net)
                IBlackBox phenome = ExperimentNEATBase.GetBlackBox(genomeList[0], dna.Activation, dna.Hyper);

                return (phenome, genomeList[0]);
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion
    }

    #endregion

    #region class: BrainNEATDNA

    public class BrainNEATDNA : ShipPartDNA
    {
        public ExperimentInitArgs_Activation Activation { get; set; }
        public HyperNEAT_Args Hyper { get; set; }

        //public NeatGenome Genome { get; set; }
        /// <summary>
        /// Can't directly serialize/deserialize NeatGenome.  Instead need to use ExperimentBase_NEAT.SavePopulation/LoadPopulation
        /// </summary>
        /// <remarks>
        /// This genome property holds xml.  Then this BrainNEATDNA gets serialized as xml, so XamlServices.Save escapes all of this property's
        /// xml.  It's ugly and inflated, but it works
        /// </remarks>
        public string Genome { get; set; }

        // This can't be serialized/deserialized.  The above properties are needed to be able to generate this
        //public IBlackBox Phenome { get; set; }

        public Point3D[] NEATPositions_Input { get; set; }
        public Point3D[] NEATPositions_Output { get; set; }

        /// <summary>
        /// This is generated when first created, but then needs to persist across saves/loads
        /// </summary>
        public Guid UniqueID { get; set; }
    }

    #endregion
}
