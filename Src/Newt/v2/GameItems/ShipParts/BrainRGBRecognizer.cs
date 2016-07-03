using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Encog.Neural.Networks;
using Game.HelperClassesAI;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.GameItems.Collections;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.GameItems.ShipParts
{
    //TODO: CollisionHull and Mass need to be the trapazoidal cone instead of a cylinder
    #region Class: BrainRGBRecognizerToolItem

    public class BrainRGBRecognizerToolItem : PartToolItemBase
    {
        #region Constructor

        public BrainRGBRecognizerToolItem(EditorOptions options)
            : base(options)
        {
            _visual2D = PartToolItemBase.GetVisual2D(this.Name, this.Description, options.EditorColors);
            this.TabName = PartToolItemBase.TAB_SHIPPART;
        }

        #endregion

        #region Public Properties

        public override string Name
        {
            get
            {
                return "Brain RGB Recognizer";
            }
        }
        public override string Description
        {
            get
            {
                return "Consumes energy, tied to CameraColorRGB, does image recognition";
            }
        }
        public override string Category
        {
            get
            {
                return PartToolItemBase.CATEGORY_EQUIPMENT;
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
            return new BrainRGBRecognizerDesign(this.Options);
        }

        #endregion
    }

    #endregion
    #region Class: BrainRGBRecognizerDesign

    public class BrainRGBRecognizerDesign : PartDesignBase
    {
        #region Declaration Section

        internal const double HEIGHT = .33d;
        internal const double RADIUSPERCENTOFSCALE_WIDE = HEIGHT * .6;		// when x scale is 1, the x radius will be this (x and y are the same)
        internal const double RADIUSPERCENTOFSCALE_NARROW = HEIGHT * .27;

        public const PartDesignAllowedScale ALLOWEDSCALE = PartDesignAllowedScale.XYZ;		// This is here so the scale can be known through reflection

        private Tuple<UtilityNewt.IObjectMassBreakdown, Vector3D, double> _massBreakdown = null;

        #endregion

        #region Constructor

        public BrainRGBRecognizerDesign(EditorOptions options)
            : base(options) { }

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

        private Model3DGroup _geometry = null;
        public override Model3D Model
        {
            get
            {
                if (_geometry == null)
                {
                    _geometry = CreateGeometry(false);
                }

                return _geometry;
            }
        }

        #endregion

        #region Public Methods

        public override Model3D GetFinalModel()
        {
            return CreateGeometry(true);
        }

        public override ShipPartDNA GetDNA()
        {
            BrainRGBRecognizerDNA retVal = new BrainRGBRecognizerDNA();

            base.FillDNA(retVal);

            return retVal;
        }
        public override void SetDNA(ShipPartDNA dna)
        {
            if (!(dna is BrainRGBRecognizerDNA))
            {
                throw new ArgumentException("The class passed in must be BrainRGBRecognizerDNA");
            }

            base.StoreDNA(dna);
        }

        public override CollisionHull CreateCollisionHull(WorldBase world)
        {
            Transform3DGroup transform = new Transform3DGroup();
            //transform.Children.Add(new ScaleTransform3D(this.Scale));		// it ignores scale
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90)));		// the physics hull is along x, but dna is along z
            transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(this.Orientation)));
            transform.Children.Add(new TranslateTransform3D(this.Position.ToVector()));

            Vector3D scale = this.Scale;
            double radius = Math1D.Avg(RADIUSPERCENTOFSCALE_WIDE, RADIUSPERCENTOFSCALE_NARROW) * Math1D.Avg(scale.X, scale.Y);
            double height = HEIGHT * scale.Z;

            return CollisionHull.CreateCylinder(world, 0, radius, height, transform.Value);
        }

        public override UtilityNewt.IObjectMassBreakdown GetMassBreakdown(double cellSize)
        {
            if (_massBreakdown != null && _massBreakdown.Item2 == this.Scale && _massBreakdown.Item3 == cellSize)
            {
                // This has already been built for this size
                return _massBreakdown.Item1;
            }

            // Convert this.Scale into a size that the mass breakdown will use (mass breakdown wants height along X, and scale is for radius, but the mass breakdown wants diameter)
            double radPercent = Math1D.Avg(RADIUSPERCENTOFSCALE_WIDE, RADIUSPERCENTOFSCALE_NARROW);
            Vector3D size = new Vector3D(this.Scale.Z * HEIGHT, this.Scale.X * radPercent * 2d, this.Scale.Y * radPercent * 2d);

            // Cylinder
            UtilityNewt.ObjectMassBreakdown cylinder = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Cylinder, UtilityNewt.MassDistribution.Uniform, size, cellSize);

            Transform3D transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90));		// the physics hull is along x, but dna is along z

            // Rotated
            UtilityNewt.ObjectMassBreakdownSet combined = new UtilityNewt.ObjectMassBreakdownSet(
                new UtilityNewt.ObjectMassBreakdown[] { cylinder },
                new Transform3D[] { transform });

            // Store this
            _massBreakdown = new Tuple<UtilityNewt.IObjectMassBreakdown, Vector3D, double>(combined, this.Scale, cellSize);

            // Exit Function
            return _massBreakdown.Item1;
        }

        #endregion

        #region Private Methods

        private Model3DGroup CreateGeometry(bool isFinal)
        {
            int domeSegments = isFinal ? 2 : 10;
            int cylinderSegments = isFinal ? 6 : 35;

            Model3DGroup retVal = new Model3DGroup();

            GeometryModel3D geometry;
            MaterialGroup material;
            DiffuseMaterial diffuse;
            SpecularMaterial specular;

            #region Insides

            if (!isFinal)
            {
                ScaleTransform3D scaleTransform = new ScaleTransform3D(HEIGHT, HEIGHT, HEIGHT);

                //TODO: This caps them to a sphere.  It doesn't look too bad, but could be better
                Model3D[] insideModels = BrainDesign.CreateInsideVisuals(.4, this.MaterialBrushes, base.SelectionEmissives, scaleTransform);

                retVal.Children.AddRange(insideModels);
            }

            #endregion
            #region Outer Shell

            geometry = new GeometryModel3D();
            material = new MaterialGroup();
            Color shellColor = WorldColors.Brain;
            if (!isFinal)
            {
                shellColor = UtilityWPF.AlphaBlend(shellColor, Colors.Transparent, .75d);
            }
            diffuse = new DiffuseMaterial(new SolidColorBrush(shellColor));
            this.MaterialBrushes.Add(new MaterialColorProps(diffuse, shellColor));
            material.Children.Add(diffuse);

            specular = WorldColors.BrainSpecular;
            this.MaterialBrushes.Add(new MaterialColorProps(specular));
            material.Children.Add(specular);

            if (!isFinal)
            {
                EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
                material.Children.Add(selectionEmissive);
                base.SelectionEmissives.Add(selectionEmissive);
            }

            geometry.Material = material;
            geometry.BackMaterial = material;

            List<TubeRingBase> rings = new List<TubeRingBase>();

            rings.Add(new TubeRingRegularPolygon(0, false, RADIUSPERCENTOFSCALE_NARROW, RADIUSPERCENTOFSCALE_NARROW, true));
            rings.Add(new TubeRingRegularPolygon(HEIGHT, false, RADIUSPERCENTOFSCALE_WIDE, RADIUSPERCENTOFSCALE_WIDE, true));

            geometry.Geometry = UtilityWPF.GetMultiRingedTube(cylinderSegments, rings, true, true);

            retVal.Children.Add(geometry);

            #endregion

            retVal.Transform = GetTransformForGeometry(isFinal);

            return retVal;
        }

        #endregion
    }

    #endregion
    #region Class: BrainRGBRecognizer

    public class BrainRGBRecognizer : PartBase, INeuronContainer, IPartUpdatable
    {
        #region Class: TrainerInput

        /// <summary>
        /// Args to the training method
        /// </summary>
        public class TrainerInput
        {
            public double[][] UnimportantEvents { get; set; }
            public Tuple<LifeEventVectorArgs, double[]>[] ImportantEvents { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public bool IsColor { get; set; }

            private readonly object _lock = new object();
            private long? _token = null;
            public long Token
            {
                get
                {
                    lock (_lock)
                    {
                        if (_token == null)
                        {
                            _token = TokenGenerator.NextToken();
                        }

                        return _token.Value;
                    }
                }
            }
        }

        #endregion
        #region Class: TrainedRecognizer

        /// <summary>
        /// Output of the training method
        /// </summary>
        private class TrainedRecognizer
        {
            public BasicNetwork Network { get; set; }

            /// <summary>
            /// This is what the network was trained on
            /// </summary>
            public TrainerInput InputRaw { get; set; }
            /// <summary>
            /// This is what the neural net was actually trained on -- noramalized, convolutions, etc
            /// </summary>
            public TrainerInput InputNormalized { get; set; }

            //TODO: May want to store the convolution chain that was used (this will allow networks to have their own convolutions)
        }

        #endregion
        #region Class: RecognitionResults

        /// <summary>
        /// This is the result of sensor data being classified with trained networks
        /// </summary>
        private class RecognitionResults
        {
            public RecognitionResults(long token, double[] output)
            {
                this.Token = token;
                this.Output = output;
            }

            public readonly long Token;

            public double[] Output;

            //TODO: more here
        }

        #endregion

        #region Declaration Section

        public const string PARTTYPE = "BrainRGBRecognizer";

        private readonly object _lock = new object();

        private readonly ItemOptions _itemOptions;

        private readonly IContainer _energyTanks;

        //TODO: This needs to be part of the dna
        private volatile CameraColorRGB _camera = null;

        private volatile LifeEventToVector _lifeEvents = null;

        private volatile SOMList _somList = null;

        private readonly double _volume;		// this is used to calculate energy draw

        /// <summary>
        /// These are just the output neurons.
        /// Inputs come from bitmaps out of the camera, internal neural nets are more complex.
        /// </summary>
        /// <remarks>
        /// The count and positions aren't known until the life event watcher as passed in
        /// </remarks>
        private Neuron_SensorPosition[] _outputNeurons = null;

        private Task<TrainedRecognizer> _trainingTask = null;
        private bool _areLifeEventsDirty = false;

        private volatile TrainedRecognizer[] _recognizers = null;

        private volatile RecognitionResults _results = null;

        private readonly ShortTermMemory<double[]> _shortTermMemory;
        private readonly NonLifeEventSnapshots<double[]> _nonLifeEventSnapshots;

        private List<Tuple<LifeEventVectorArgs, double[]>> _importantEvents = new List<Tuple<LifeEventVectorArgs, double[]>>();

        private readonly ConvolutionBase2D _convolution;
        /// <summary>
        /// True=The array is triple sized.  Each pixel has an R,G,B.  Convolutions will need to be applied independently to each color
        /// False=It is a grayscale.  One value per pixel
        /// </summary>
        private readonly bool _isColor;

        private readonly int _finalResolution;

        private readonly bool _somDiscardDupes;
        private readonly bool _somIsColor;

        private readonly BrainRGBRecognizerDNAExtra _dnaExtra;

        #endregion

        #region Constructor

        public BrainRGBRecognizer(EditorOptions options, ItemOptions itemOptions, BrainRGBRecognizerDNA dna, IContainer energyTanks)
            : base(options, dna)
        {
            _itemOptions = itemOptions;
            _energyTanks = energyTanks;

            _dnaExtra = dna.Extra ?? BrainRGBRecognizerDNAExtra.GetDefaultDNA();

            _isColor = _dnaExtra.IsColor;
            _finalResolution = _dnaExtra.FinalResolution;
            if (_dnaExtra.UseEdgeDetect)
            {
                _convolution = Convolutions.GetEdgeSet_Sobel();
            }
            else
            {
                _convolution = null;
            }

            _somDiscardDupes = _dnaExtra.ShouldSOMDiscardDupes;
            _somIsColor = _isColor;

            this.Design = new BrainRGBRecognizerDesign(options);
            this.Design.SetDNA(dna);

            _shortTermMemory = new ShortTermMemory<double[]>(itemOptions.ShortTermMemory_MillisecondsBetween, itemOptions.ShortTermMemory_Size);
            //TODO: Get params from itemOptions
            _nonLifeEventSnapshots = new NonLifeEventSnapshots<double[]>();
            //_nonLifeEventSnapshots = new NonLifeEventSnapshots<double[]>(.25, .6, 2);     // faster times for debugging

            GetMass(out _mass, out _volume, out _radius, out _scaleActual, dna, itemOptions);
        }

        #endregion

        #region INeuronContainer Members

        public IEnumerable<INeuron> Neruons_Readonly
        {
            get
            {
                if (_outputNeurons == null)
                {
                    throw new InvalidOperationException("Must assign the life event watcher before the neurons are defined");
                }

                return _outputNeurons;
            }
        }
        public IEnumerable<INeuron> Neruons_ReadWrite
        {
            get
            {
                return Enumerable.Empty<INeuron>();
            }
        }
        public IEnumerable<INeuron> Neruons_Writeonly
        {
            get
            {
                return Enumerable.Empty<INeuron>();
            }
        }

        public IEnumerable<INeuron> Neruons_All
        {
            get
            {
                if (_outputNeurons == null)
                {
                    throw new InvalidOperationException("Must assign the life event watcher before the neurons are defined");
                }

                return _outputNeurons;
            }
        }

        //NOTE: Even though this uses neural nets, its just another sensor from the rest of the bot's perspective (because it's only active when tied to a camera
        public NeuronContainerType NeuronContainerType
        {
            get
            {
                return NeuronContainerType.Sensor;
            }
        }

        private readonly double _radius;
        public double Radius
        {
            get
            {
                return _radius;
            }
        }

        private volatile bool _isOn = false;
        public bool IsOn
        {
            get
            {
                return _isOn;
            }
        }

        #endregion
        #region IPartUpdatable Members

        public void Update_MainThread(double elapsedTime)
        {
        }
        public void Update_AnyThread(double elapsedTime)
        {
            bool shouldBeZero;

            if (_energyTanks == null || _energyTanks.RemoveQuantity(elapsedTime * _volume * _itemOptions.Brain_AmountToDraw * ItemOptions.ENERGYDRAWMULT, true) > 0d)
            {
                _isOn = false;
                shouldBeZero = true;
            }
            else
            {
                _isOn = true;
                shouldBeZero = !Tick();
            }

            if (shouldBeZero)
            {
                lock (_lock)
                {
                    for (int cntr = 0; cntr < _outputNeurons.Length; cntr++)
                    {
                        _outputNeurons[cntr].Value = 0;
                    }
                }
            }
        }

        public int? IntervalSkips_MainThread
        {
            get
            {
                return null;
            }
        }
        public int? IntervalSkips_AnyThread
        {
            get
            {
                return 0;
            }
        }

        #endregion

        #region Public Properties

        private readonly double _mass;
        public override double DryMass
        {
            get
            {
                return _mass;
            }
        }
        public override double TotalMass
        {
            get
            {
                return _mass;
            }
        }

        private readonly Vector3D _scaleActual;
        public override Vector3D ScaleActual
        {
            get
            {
                return _scaleActual;
            }
        }

        // These are exposed for viewers/debuggers
        public SOMResult SOM
        {
            get
            {
                SOMList list = _somList;
                if (list == null)
                {
                    return null;
                }

                return list.CurrentResult;
            }
        }
        private volatile double[] _latestImage = null;
        public double[] LatestImage
        {
            get
            {
                //NOTE: This technically isn't the latest possible image, just the last stored image
                //return _shortTermMemory.GetSnapshot(DateTime.UtcNow);

                return _latestImage;
            }
        }
        private volatile Tuple<TrainerInput, TrainerInput> _currentTrainingData = null;
        /// <summary>
        /// Item1=Raw input
        /// Item2=Input actually sent to neural net (with convolutions applied)
        /// </summary>
        public Tuple<TrainerInput, TrainerInput> TrainingData
        {
            get
            {
                return _currentTrainingData;
            }
        }
        public Tuple<int, int> CameraWidthHeight
        {
            get
            {
                lock (_lock)
                {
                    var camera = _camera;
                    if (camera == null)
                    {
                        return null;
                    }

                    return Tuple.Create(camera.PixelWidthHeight, camera.PixelWidthHeight);
                }
            }
        }
        public Tuple<LifeEventType, double>[] CurrentOutput
        {
            get
            {
                RecognitionResults results = _results;
                if (results == null)
                {
                    return null;
                }

                LifeEventToVector lifeEvents = _lifeEvents;
                if (lifeEvents == null)
                {
                    return null;
                }

                if (results.Output.Length != lifeEvents.Types.Length)
                {
                    return null;
                }

                return Enumerable.Range(0, results.Output.Length).
                    Select(o => Tuple.Create(lifeEvents.Types[o], results.Output[o])).
                    ToArray();
            }
        }
        public bool IsColor
        {
            get
            {
                return _isColor;
            }
        }

        #endregion

        #region Public Methods

        public override ShipPartDNA GetNewDNA()
        {
            BrainRGBRecognizerDNA retVal = (BrainRGBRecognizerDNA)this.Design.GetDNA();

            //NOTE: The design class doesn't hold neurons, since it's only used by the editor, so fill out the rest of the dna here
            retVal.Neurons = _outputNeurons.Select(o => o.Position).ToArray();

            retVal.Extra = _dnaExtra;

            return retVal;
        }

        //TODO: This should store results in dna
        public void AssignOutputs(LifeEventToVector lifeEvents)
        {
            if (_lifeEvents != null)
            {
                throw new InvalidOperationException("LifeEvents can only be assigned once");
            }

            _lifeEvents = lifeEvents;
            lifeEvents.EventOccurred += LifeEvents_EventOccurred;

            // Build the neurons
            _outputNeurons = CreateNeurons(this.DNA, _itemOptions, lifeEvents);
        }

        public void SetCamera(CameraColorRGB camera)
        {
            lock (_lock)
            {
                if (camera == null)
                {
                    _camera = null;
                    _somList = null;
                }
                else
                {
                    _camera = camera;
                    _somList = new SOMList(new[] { camera.PixelWidthHeight, camera.PixelWidthHeight }, Convolutions.GetEdgeSet_Sobel(), discardDupes: _somDiscardDupes, isColor2D: _somIsColor);        // the edge detect really helps.  without it, there tended to just be one big somnode after a while
                }

                _shortTermMemory.Clear();
                _nonLifeEventSnapshots.Clear();
                _importantEvents.Clear();
                _recognizers = null;
                _results = null;
            }
        }

        //TODO: This should store results in dna
        public static void AssignCameras(BrainRGBRecognizer[] recognizers, CameraColorRGB[] cameras)
        {
            if (cameras != null)
            {
                foreach (CameraColorRGB camera in cameras)
                {
                    camera.NeuronContainerType = NeuronContainerType.Sensor;
                }
            }

            if (recognizers == null || recognizers.Length == 0)
            {
                return;
            }

            foreach (BrainRGBRecognizer recognizer in recognizers)
            {
                recognizer.SetCamera(null);
            }

            if (cameras == null || cameras.Length == 0)
            {
                return;
            }

            // Call linker
            LinkItem[] recogItems = recognizers.
                Select(o => new LinkItem(o.Position, o.Radius)).
                ToArray();

            LinkItem[] cameraItems = cameras.
                Select(o => new LinkItem(o.Position, o.Radius)).
                ToArray();

            Tuple<int, int>[] links = ItemLinker.Link_1_2(cameraItems, recogItems, new ItemLinker_OverflowArgs());

            // Assign cameras
            foreach (var link in links)
            {
                recognizers[link.Item2].SetCamera(cameras[link.Item1]);
                cameras[link.Item1].NeuronContainerType = NeuronContainerType.None;     // this recognizer will now be this camera's output
            }
        }

        /// <summary>
        /// This saves all images as files.  This is meant for debugging
        /// </summary>
        public void SaveImages(string baseFolder)
        {
            Tuple<int, int> widthHeight = this.CameraWidthHeight;
            if (widthHeight == null)
            {
                return;
            }

            var trainingData = this.TrainingData;

            if (trainingData != null)
            {
                // Training Raw
                SaveImages(baseFolder, "TrainRaw", trainingData.Item1);

                // Training Normalized (include negatives)
                SaveImages(baseFolder, "TrainNormalized", trainingData.Item2);
            }

            // ShortTerm
            double[][] shortTerm = _shortTermMemory.GetSnapshots();
            SaveImages(baseFolder, "ShortTerm", widthHeight.Item1, widthHeight.Item2, shortTerm, _isColor);

            // Non life events
            Tuple<string, double[]>[] nonLifeEvents = _nonLifeEventSnapshots.GetSamples();
            SaveImages(baseFolder, "NonLifeEvents", widthHeight.Item1, widthHeight.Item2, nonLifeEvents, _isColor);

            // SOM
            SOMResult som = this.SOM;
            SaveImages(baseFolder, "SOM", widthHeight.Item1, widthHeight.Item2, som, false);        //TODO: Change isColor when SOM can support color
        }

        #endregion

        #region Event Listeners

        private void LifeEvents_EventOccurred(object sender, LifeEventVectorArgs e)
        {
            // Tell this list to suppress adds for a while
            _nonLifeEventSnapshots.EventOcurred();

            var camera = _camera;
            if (camera == null)
            {
                return;
            }

            // Get a copy of the input that occurred before the event happened
            double[][] inputs = _shortTermMemory.GetSnapshots(e.Time, 3);
            if (inputs == null || inputs.Length == 0)
            {
                return;
            }

            // Get some rotations of it
            inputs = inputs.
                SelectMany(o => GetRotations(o, camera.PixelWidthHeight, camera.PixelWidthHeight, _isColor)).
                ToArray();

            lock (_lock)
            {
                // Store these pairings
                foreach (double[] input in inputs)
                {
                    _importantEvents.Add(Tuple.Create(e, input));
                }
                _areLifeEventsDirty = true;

                if (_trainingTask != null)
                {
                    // Training is currently running.  Let it finish
                    return;
                }

                // Kick off a training
                TrainerInput trainerInput = GetTrainingInput();
                if (trainerInput == null)
                {
                    return;
                }

                _areLifeEventsDirty = false;
                _trainingTask = new Task<TrainedRecognizer>(() => Train(trainerInput, _convolution, _isColor, _finalResolution));
                _trainingTask.ContinueWith(r => FinishedTraining(r.Result));
                _trainingTask.Start();
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// This gets called on a regular basis.  It gets the camera's current image, stores that image in various memory lists, and
        /// classifies the image
        /// </summary>
        /// <returns>
        /// True: This method has populated the output neurons
        /// False: The output neurons need to be set to zero by the caller
        /// </returns>
        private bool Tick()
        {
            CameraColorRGB camera = _camera;
            if (camera == null)
            {
                return false;
            }

            //TODO: This portion may need to run in a different thread
            Tuple<long, IBitmapCustom> bitmap = camera.Bitmap;
            if (bitmap == null)
            {
                return false;
            }

            RecognitionResults results = _results;

            if (results != null && results.Token == bitmap.Item1)
            {
                // The bitmap is old, and has already be classified
                return false;
            }

            // The camera output is ARGB colors from 0 to 255.  Convert to values from 0 to 1

            double[] nnInput;
            if (_isColor)
            {
                // Color, 3 values per pixel
                nnInput = bitmap.Item2.GetColors_Byte().
                    SelectMany(o => new[] { o[1] / 255d, o[2] / 255d, o[3] / 255d }).     // o[0] is alpha
                    ToArray();
            }
            else
            {
                // Grayscale, 1 value per pixel
                nnInput = bitmap.Item2.GetColors_Byte().
                    Select(o => UtilityWPF.ConvertToGray(o[1], o[2], o[3]) / 255d).     // o[0] is alpha
                    ToArray();
            }

            _latestImage = nnInput;
            _shortTermMemory.StoreSnapshot(nnInput);
            _nonLifeEventSnapshots.Add(nnInput);

            var somList = _somList;
            if (somList != null)
            {
                somList.Add(nnInput);
            }

            LifeEventToVector lifeEvents = _lifeEvents;
            if (lifeEvents == null)
            {
                // Lifeevents defines how many output neurons there are.  So if it's not set, then there's nothing
                // to train to
                return false;
            }

            // Recognize the image, and set the output neurons
            results = RecognizeImage(nnInput, bitmap.Item1, bitmap.Item2.Width, bitmap.Item2.Height, _finalResolution, _recognizers, lifeEvents, _convolution, _isColor);

            lock (_lock)
            {
                _results = results;

                for (int cntr = 0; cntr < results.Output.Length; cntr++)
                {
                    _outputNeurons[cntr].Value = results.Output[cntr];
                }
            }

            return true;
        }

        private static TrainedRecognizer Train(TrainerInput input, ConvolutionBase2D convolution, bool isColor, int finalResolution)
        {
            if (input == null || input.ImportantEvents == null || input.ImportantEvents.Length == 0)
            {
                return null;
            }

            TrainerInput normalized = GetTrainingInput(input, convolution, isColor, finalResolution);

            List<double[]> inputs = new List<double[]>();
            List<double[]> outputs = new List<double[]>();

            // Important Events
            inputs.AddRange(normalized.ImportantEvents.Select(o => o.Item2));
            outputs.AddRange(normalized.ImportantEvents.Select(o => o.Item1.Vector));

            // Unimportant Events (sensor input that should output zeros)
            if (normalized.UnimportantEvents != null && normalized.UnimportantEvents.Length > 0)
            {
                int outputVectorLength = outputs[0].Length;

                inputs.AddRange(normalized.UnimportantEvents);
                outputs.AddRange(Enumerable.Range(0, normalized.UnimportantEvents.Length).
                    Select(o => new double[outputVectorLength]));
            }

            //NOTE: If there is an exception, the network couldn't be trained
            BasicNetwork network = null;
            try
            {
                //network = UtilityEncog.GetTrainedNetwork(inputs.ToArray(), outputs.ToArray(), UtilityEncog.ERROR, 15, 45).NetworkOrNull;
                network = UtilityEncog.GetTrainedNetwork(inputs.ToArray(), outputs.ToArray(), UtilityEncog.ERROR, 5, 15).NetworkOrNull;
            }
            catch (Exception) { }

            if (network == null)
            {
                return null;
            }

            return new TrainedRecognizer()
            {
                Network = network,
                InputRaw = input,
                InputNormalized = normalized,
            };
        }
        private void FinishedTraining(TrainedRecognizer result)
        {
            lock (_lock)
            {
                _trainingTask = null;

                if (result != null)
                {
                    //TODO: Keep a few around.  If max count is met, throw out the oldest/worst
                    _recognizers = new[] { result };
                    _currentTrainingData = Tuple.Create(result.InputRaw, result.InputNormalized);
                }

                if (_areLifeEventsDirty || result == null)      // null means it was unsuccessful.  Try again
                {
                    // There is new data, train against it
                    TrainerInput trainerInput = GetTrainingInput();

                    if (trainerInput != null)
                    {
                        _areLifeEventsDirty = false;
                        _trainingTask = new Task<TrainedRecognizer>(() => Train(trainerInput, _convolution, _isColor, _finalResolution));
                        _trainingTask.ContinueWith(r => FinishedTraining(r.Result));
                        _trainingTask.Start();
                    }
                }
            }
        }

        private static void GetMass(out double mass, out double volume, out double radius, out Vector3D actualScale, ShipPartDNA dna, ItemOptions itemOptions)
        {
            // Technically, this is a trapazoid cone, but the collision hull and mass breakdown are cylinders, so just treat it like a cylinder

            // If that changes, take the mass of the full cone minus the top cone.  Would need to calculate the height of the full cone based
            // on the slope of wide radius to small radius:
            //  slope=((wide/2 - narrow/2) / height)
            //  fullheight = (-wide/2) / slope   ----   y=mx+b

            double rad = Math1D.Avg(BrainRGBRecognizerDesign.RADIUSPERCENTOFSCALE_WIDE, BrainRGBRecognizerDesign.RADIUSPERCENTOFSCALE_NARROW) * Math1D.Avg(dna.Scale.X, dna.Scale.Y);
            double height = BrainRGBRecognizerDesign.HEIGHT * dna.Scale.Z;

            volume = Math.PI * rad * rad * height;

            mass = volume * itemOptions.Brain_Density;

            radius = Math1D.Avg(rad, height);       // this is just approximate, and is used by INeuronContainer
            actualScale = new Vector3D(rad * 2d, rad * 2d, height);     // I think this is just used to get a bounding box
        }

        private static Neuron_SensorPosition[] CreateNeurons(ShipPartDNA dna, ItemOptions itemOptions, LifeEventToVector lifeEvents)
        {
            //TODO: Instead of just having a single output neuron for each life event type, report the type at location?
            //For example: the camera's input is a square, so have a grid of blocks.  The output grid doesn't need to be very high
            //resolution, maybe 3x3 up to 5x5.  Each block would be a line of neurons perpendicular to the square

            double radius = (dna.Scale.X + dna.Scale.Y + dna.Scale.Z) / (3d * 2d);		// xyz should all be the same anyway

            Vector3D[] positions = Brain.GetNeuronPositions_Line2D(null, lifeEvents.Types.Length, radius);

            return positions.
                Select(o => new Neuron_SensorPosition(o.ToPoint(), true)).
                ToArray();
        }

        /// <summary>
        /// This returns raw input (still need to call NormalizeInput)
        /// </summary>
        private TrainerInput GetTrainingInput()
        {
            var camera = _camera;
            if (camera == null)
            {
                return null;
            }

            double numCategories = _importantEvents.
                Distinct(o => o.Item1.Type).
                Count();

            int unimportantCount = Math.Min(20, (_importantEvents.Count / numCategories).ToInt_Round());

            TrainerInput trainerInput = new TrainerInput()
            {
                ImportantEvents = _importantEvents.ToArray(),
                UnimportantEvents = _nonLifeEventSnapshots.GetSamples(unimportantCount),
                Width = camera.PixelWidthHeight,
                Height = camera.PixelWidthHeight,
                IsColor = _isColor,
            };

            return trainerInput;
        }
        /// <summary>
        /// This runs the input through NormalizeInput, and returns an object with those results
        /// </summary>
        private static TrainerInput GetTrainingInput(TrainerInput raw, ConvolutionBase2D convolution, bool isColor, int finalResolution)
        {
            var importantEvents = raw.ImportantEvents.
                Select(o =>
                {
                    double[] normalized = NormalizeInput(o.Item2, raw.Width, raw.Height, finalResolution, convolution, isColor);
                    return Tuple.Create(o.Item1, normalized);
                }).
                ToArray();

            var unimportantEvents = raw.UnimportantEvents.
                Select(o => NormalizeInput(o, raw.Width, raw.Height, finalResolution, convolution, isColor)).
                ToArray();

            var awayPoints = GetAwayPoints(importantEvents.Select(o => o.Item2).ToArray());

            //VectorInt reduction;
            //if (convolution == null)
            //{
            //    reduction = new VectorInt(0, 0);
            //}
            //else
            //{
            //    reduction = convolution.GetReduction();
            //}

            return new TrainerInput()
            {
                //Width = raw.Width - reduction.X,
                //Height = raw.Height - reduction.Y,
                Width = finalResolution,
                Height = finalResolution,
                ImportantEvents = importantEvents,
                UnimportantEvents = unimportantEvents.Concat(awayPoints).ToArray(),
                IsColor = isColor,
            };
        }

        private static RecognitionResults RecognizeImage(double[] input, long inputToken, int width, int height, int finalResolution, TrainedRecognizer[] recognizers, LifeEventToVector lifeEvents, ConvolutionBase2D convolution, bool isColor)
        {
            if (recognizers == null)
            {
                return new RecognitionResults(inputToken, new double[lifeEvents.Types.Length]);
            }

            double[] normalized = NormalizeInput(input, width, height, finalResolution, convolution, isColor);

            foreach (var recognizer in recognizers)
            {
                double[] output = recognizer.Network.Compute(normalized);

                //TODO: Analyze outputs of all the recognizers to come up with a final result.  Can't just take the average -- if they all
                //agree, that's great.  But disagreement should have a zero output (or at least a very weak output)
                return new RecognitionResults(inputToken, output);
            }

            throw new ApplicationException("finish this");
        }

        /// <summary>
        /// The values are from 0 to 255, and need to be 0 to 1
        /// </summary>
        private static double[] NormalizeInput(double[] input, int width, int height, int finalResolution, ConvolutionBase2D convolution, bool isColor)
        {
            // This part is now done earlier on
            //double[] retVal = input.
            //    Select(o => o / 255d).
            //    ToArray();

            double[] retVal = input;

            if (convolution != null)
            {
                if (isColor)
                {
                    // Each pixel in input is R,G,B (so 3 values per pixel).  Each of the 3 colors needs to be run through the
                    // convolution independently.  Then put them back into triples to feed the neural net
                    //
                    // Note that it's ok that they are jumbled up when going to the neural net.  Weights are assigned randomly
                    // before starting training, so it could be all Rs, then all Gs, then Bs, and it wouldn't make any difference.
                    //
                    // But it is important for the convolutions to work with pure 2D images, because the convolutions are
                    // essentially sliding a rectangle across another rectangle, and taking dot products (so rgb triples would
                    // cause the convolution result to be nonsense)

                    // Split into 3 arrays
                    var split = SplitColor_conv(input, width, height);

                    // Convolute independently
                    Convolution2D r = Convolutions.Convolute(split.Item1, convolution);
                    Convolution2D g = Convolutions.Convolute(split.Item2, convolution);
                    Convolution2D b = Convolutions.Convolute(split.Item3, convolution);

                    r = Convolutions.MaxPool(r, finalResolution, finalResolution);
                    g = Convolutions.MaxPool(g, finalResolution, finalResolution);
                    b = Convolutions.MaxPool(b, finalResolution, finalResolution);

                    // Put back into one large array (but smaller than the original)
                    retVal = MergeColor(r, g, b);
                }
                else
                {
                    Convolution2D convoluted = Convolutions.Convolute(new Convolution2D(input, width, height, false), convolution);
                    convoluted = Convolutions.MaxPool(convoluted, finalResolution, finalResolution);
                    retVal = convoluted.Values;
                }
            }

            return retVal;
        }

        private static double[][] GetRotations(double[] input, int width, int height, bool isColor)
        {
            if (isColor)
            {
                return GetRotations_Color(input, width, height);
            }
            else
            {
                return GetRotations_BW(input, width, height);
            }
        }
        private static double[][] GetRotations_Color(double[] input, int width, int height)
        {
            var conv0 = SplitColor_conv(input, width, height);

            var conv90 = Tuple.Create(
                Convolutions.Rotate_90(conv0.Item1, true),
                Convolutions.Rotate_90(conv0.Item2, true),
                Convolutions.Rotate_90(conv0.Item3, true));

            var conv180 = Tuple.Create(
                Convolutions.Rotate_90(conv90.Item1, true),
                Convolutions.Rotate_90(conv90.Item2, true),
                Convolutions.Rotate_90(conv90.Item3, true));

            var conv270 = Tuple.Create(
                Convolutions.Rotate_90(conv0.Item1, false),
                Convolutions.Rotate_90(conv0.Item2, false),
                Convolutions.Rotate_90(conv0.Item3, false));

            return new[]
            {
                input,
                MergeColor(conv90.Item1, conv90.Item2, conv90.Item3),
                MergeColor(conv180.Item1, conv180.Item2, conv180.Item3),
                MergeColor(conv270.Item1, conv270.Item2, conv270.Item3),
            };
        }
        private static double[][] GetRotations_BW(double[] input, int width, int height)
        {
            Convolution2D conv = new Convolution2D(input, width, height, false);
            Convolution2D conv90 = Convolutions.Rotate_90(conv, true);

            return new[]
            {
                input,
                conv90.Values,
                Convolutions.Rotate_90(conv90, true).Values,
                Convolutions.Rotate_90(conv, false).Values,
            };
        }

        // These split a color array into its 3 parts
        private static Tuple<Convolution2D, Convolution2D, Convolution2D> SplitColor_conv(double[] triples, int width, int height)
        {
            var arrays = SplitColor(triples, width, height);

            return Tuple.Create(
                new Convolution2D(arrays.Item1, width, height, false),
                new Convolution2D(arrays.Item2, width, height, false),
                new Convolution2D(arrays.Item3, width, height, false));
        }
        private static Tuple<double[], double[], double[]> SplitColor(double[] triples, int width, int height)
        {
            int size = width * height;
            double[] r = new double[size];
            double[] g = new double[size];
            double[] b = new double[size];

            for (int cntr = 0; cntr < size; cntr++)
            {
                int baseIndex = cntr * 3;
                r[cntr] = triples[baseIndex + 0];
                g[cntr] = triples[baseIndex + 1];
                b[cntr] = triples[baseIndex + 2];
            }

            return Tuple.Create(r, g, b);
        }
        // These merge the 3 back into one big array
        private static double[] MergeColor(Convolution2D r, Convolution2D g, Convolution2D b)
        {
            return MergeColor(r.Values, g.Values, b.Values);
        }
        private static double[] MergeColor(double[] r, double[] g, double[] b)
        {
            double[] retVal = new double[r.Length * 3];

            for (int cntr = 0; cntr < r.Length; cntr++)
            {
                int baseIndex = cntr * 3;
                retVal[baseIndex + 0] = r[cntr];
                retVal[baseIndex + 1] = g[cntr];
                retVal[baseIndex + 2] = b[cntr];
            }

            return retVal;
        }

        /// <summary>
        /// This gets random points (images) that are evenly distributed within the possible set of all images.
        /// These images stay away from the samples passed in
        /// </summary>
        /// <remarks>
        /// This is an attempt to get the neural net to default to zero output in all regions except the positive
        /// training data
        /// </remarks>
        private static double[][] GetAwayPoints(double[][] points)
        {
            if (points.Length == 0)
            {
                return new double[0][];
            }

            //int returnCount = points.Length;
            int returnCount = 100;
            int dimensions = points[0].Length;

            //TODO: Examine mins/maxes of the points to see if negative is allowed
            Tuple<double[], double[]> aabb = Tuple.Create(
                Enumerable.Range(0, dimensions).Select(o => 0d).ToArray(),
                Enumerable.Range(0, dimensions).Select(o => 1d).ToArray());

            return MathND.GetRandomVectors_Cube_EventDist(returnCount, aabb, existingStaticPoints: points);
        }

        private static void SaveImages(string parentFolder, string childFolder, TrainerInput trainerInput)
        {
            if (trainerInput == null)
            {
                return;
            }

            foreach (var set in trainerInput.ImportantEvents.ToLookup(o => o.Item1))
            {
                SaveImages(parentFolder, childFolder, trainerInput.Width, trainerInput.Height, set.Select(o => o.Item2).ToArray(), trainerInput.IsColor, set.Key.ToString());
            }

            SaveImages(parentFolder, childFolder, trainerInput.Width, trainerInput.Height, trainerInput.UnimportantEvents, trainerInput.IsColor, "none");
        }
        private static void SaveImages(string parentFolder, string childFolder, int width, int height, Tuple<string, double[]>[] images, bool isColor)
        {
            if (images == null || images.Length == 0)
            {
                return;
            }

            foreach (var set in images.ToLookup(o => o.Item1))
            {
                SaveImages(parentFolder, childFolder, width, height, set.Select(o => o.Item2).ToArray(), isColor, set.Key);
            }
        }
        private static void SaveImages(string parentFolder, string childFolder, int width, int height, SOMResult som, bool isColor)
        {
            if (som == null || som.InputsByNode == null || som.InputsByNode.Length == 0)
            {
                return;
            }

            // Pull out the images
            double[][] images = som.InputsByNode.
                SelectMany(o => o).
                Select(o =>
                {
                    var cast = o as SOMInput<SOMList.SOMItem>;
                    if (cast == null) return null;

                    return cast.Source.Original;
                }).
                Where(o => o != null).
                ToArray();

            SaveImages(parentFolder, childFolder, width, height, images, isColor);
        }
        private static void SaveImages(string parentFolder, string childFolder, int width, int height, double[][] images, bool isColor, string filename = null)
        {
            if (images == null || images.Length == 0)
            {
                return;
            }

            // Folder
            string folder = Path.Combine(parentFolder, childFolder);
            Directory.CreateDirectory(folder);

            for (int cntr = 0; cntr < images.Length; cntr++)
            {
                // Bitmap
                BitmapSource bitmap;
                if (isColor)
                {
                    bitmap = UtilityWPF.GetBitmap_RGB(images[cntr], width, height);
                }
                else
                {
                    bitmap = UtilityWPF.GetBitmap(images[cntr], width, height);
                }

                // Filename
                string finalFilename;
                string number = (cntr + 1).ToString();
                if (string.IsNullOrWhiteSpace(filename))
                {
                    finalFilename = number;
                }
                else
                {
                    finalFilename = filename;

                    if (images.Length > 1)
                    {
                        finalFilename += " " + number;
                    }
                }

                finalFilename = Path.Combine(folder, finalFilename + ".png");

                // Save
                UtilityWPF.SaveBitmapPNG(bitmap, finalFilename);
            }
        }

        #endregion
    }

    #endregion

    #region Class: BrainRGBRecognizerDNA

    public class BrainRGBRecognizerDNA : ShipPartDNA
    {
        public BrainRGBRecognizerDNAExtra Extra { get; set; }

        public static void GetDefaultDNA(BrainRGBRecognizerDNA dna)
        {
            if (dna.Extra == null)
            {
                dna.Extra = BrainRGBRecognizerDNAExtra.GetDefaultDNA();
            }
        }
    }

    #endregion
    #region Class: BrainRGBRecognizerDNAExtra

    public class BrainRGBRecognizerDNAExtra
    {
        public bool IsColor { get; set; }

        public int FinalResolution { get; set; }

        public bool UseEdgeDetect { get; set; }

        public bool ShouldSOMDiscardDupes { get; set; }

        public bool UseSOM { get; set; }
        public bool UseNonLifeEvents { get; set; }
        public bool UseRandomNonPoints { get; set; }

        public static BrainRGBRecognizerDNAExtra GetDefaultDNA()
        {
            return new BrainRGBRecognizerDNAExtra()
            {
                IsColor = true,
                FinalResolution = 7,
                UseEdgeDetect = true,
                ShouldSOMDiscardDupes = false,
                UseSOM = true,
                UseNonLifeEvents = true,
                UseRandomNonPoints = true,
            };
        }
    }

    #endregion
}
