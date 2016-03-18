using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
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
        #region Class: RecognitionResults

        private class RecognitionResults
        {
            public RecognitionResults(long token)
            {
                this.Token = token;
            }

            public readonly long Token;

            //TODO: more here
        }

        #endregion

        #region Declaration Section

        public const string PARTTYPE = "BrainRGBRecognizer";

        private readonly ItemOptions _itemOptions;

        private readonly IContainer _energyTanks;

        //TODO: This needs to be part of the dna
        private volatile CameraColorRGB _camera;

        /// <summary>
        /// These are just the output neurons.
        /// Inputs come from bitmaps out of the camera, internal neural nets are more complex.
        /// </summary>
        private readonly Neuron_ZeroPos[] _neurons;

        private readonly double _volume;		// this is used to calculate energy draw

        private volatile RecognitionResults _results = null;

        #endregion

        #region Constructor

        public BrainRGBRecognizer(EditorOptions options, ItemOptions itemOptions, ShipPartDNA dna, IContainer energyTanks)
            : base(options, dna)
        {
            _itemOptions = itemOptions;
            _energyTanks = energyTanks;

            this.Design = new BrainRGBRecognizerDesign(options);
            this.Design.SetDNA(dna);

            // Build the neurons
            _neurons = CreateNeurons(dna, itemOptions);

            GetMass(out _mass, out _volume, out _radius, out _scaleActual, dna, itemOptions);
        }

        #endregion

        #region INeuronContainer Members

        public IEnumerable<INeuron> Neruons_Readonly
        {
            get
            {
                return _neurons;
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
                return _neurons;
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
            if (_energyTanks != null && _energyTanks.RemoveQuantity(elapsedTime * _volume * _itemOptions.BrainAmountToDraw * ItemOptions.ENERGYDRAWMULT, true) > 0d)
            {
                _isOn = false;
            }
            else
            {
                _isOn = true;
            }




            CameraColorRGB camera = _camera;
            if (camera == null)
            {
                return;
            }

            //TODO: This portion may need to run in a different thread
            Tuple<long, IBitmapCustom> bitmap = camera.Bitmap;
            if (bitmap == null)
            {
                return;
            }

            RecognitionResults results = _results;

            if (results != null && results.Token == bitmap.Item1)
            {
                return;
            }

            //TODO: Run the bitmap through convolution/neural chains

            //TODO: Map the chain outputs onto the final output neurons

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

        #endregion

        #region Public Methods

        public override ShipPartDNA GetNewDNA()
        {
            ShipPartDNA retVal = this.Design.GetDNA();

            //NOTE: The design class doesn't hold neurons, since it's only used by the editor, so fill out the rest of the dna here
            retVal.Neurons = _neurons.Select(o => o.Position).ToArray();

            //TODO: Store some of this class's internal state
            //Store two different versions:
            //  full copy to be used when loading the same bot
            //  lossy compressed version to be passed to children --- actually, there's no need to store this second copy.  It could be generated at the time of instantiating a child

            return retVal;
        }

        public void Train(object trainingData)
        {
            // A part needs to record snapshots of sensor data during life events (eating, taking damage, etc), then periodically give
            // that data to this train methods (grouped by event type)
            //
            // That way this class can recognize scenes that occur before/during those events (this brain's job is just to recognize.  Other
            // brains will look at this recognizer's output and act on it)

            //TODO: Come up with convolution/neural chains that try to recognize this data
        }

        //TODO: This should store results in dna
        public static void AssignCameras(BrainRGBRecognizer[] recognizers, CameraColorRGB[] cameras)
        {
            if(cameras != null)
            {
                foreach(CameraColorRGB camera in cameras)
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
                recognizer._camera = null;
            }

            if(cameras == null || cameras.Length == 0)
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
                recognizers[link.Item2]._camera = cameras[link.Item1];
                cameras[link.Item1].NeuronContainerType = NeuronContainerType.None;     // this recognizer will now be this camera's output
            }
        }

        #endregion

        #region Private Methods

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

            mass = volume * itemOptions.BrainDensity;

            radius = Math1D.Avg(rad, height);       // this is just approximate, and is used by INeuronContainer
            actualScale = new Vector3D(rad * 2d, rad * 2d, height);     // I think this is just used to get a bounding box
        }

        //TODO: Finish this
        //  Could make a line of output neurons
        //  Or could put that line into an arc (wouldn't add any meaning, just look better)
        //  Or create 3 spokes that point 120 degrees apart (3 arcs that follow the curve of the sphere)
        //  Or evenly distribute along the surface of the sphere
        //
        //The line is simplist, because explicit meanings are mapped to each neuron
        //The spokes would have a meaning for each spoke, and sub meaning for each neuron in that spoke --- wouldn't need to limit to 3
        //The surface would probably map meanings to polar coordinates, and nearby neurons light up --- I don't like this
        private static Neuron_ZeroPos[] CreateNeurons(ShipPartDNA dna, ItemOptions itemOptions)
        {
            return new[] { new Neuron_ZeroPos(new Point3D()) };
        }

        #endregion
    }

    #endregion
}
