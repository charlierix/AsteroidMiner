using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.GameItems.ShipParts
{
    #region Class: CameraColorRGBToolItem

    public class CameraColorRGBToolItem : PartToolItemBase
    {
        #region Constructor

        public CameraColorRGBToolItem(EditorOptions options)
            : base(options)
        {
            this.TabName = PartToolItemBase.TAB_SHIPPART;
            _visual2D = PartToolItemBase.GetVisual2D(this.Name, this.Description, options, this);
        }

        #endregion

        #region Public Properties

        public override string Name
        {
            get
            {
                return "Camera (RGB)";
            }
        }
        public override string Description
        {
            get
            {
                return "Consumes energy, used to see other objects (cones are hard coded to red/green/blue)";
            }
        }
        public override string Category
        {
            get
            {
                return PartToolItemBase.CATEGORY_SENSOR;
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
            return new CameraColorRGBDesign(this.Options);
        }

        #endregion
    }

    #endregion
    #region Class: CameraColorRGBDesign

    public class CameraColorRGBDesign : PartDesignBase
    {
        #region Declaration Section

        private const double HEIGHT = 1.65d;
        internal const double SCALE = .33d / HEIGHT;

        public const PartDesignAllowedScale ALLOWEDSCALE = PartDesignAllowedScale.XYZ;		// This is here so the scale can be known through reflection

        private Tuple<UtilityNewt.IObjectMassBreakdown, Vector3D, double> _massBreakdown = null;

        #endregion

        #region Constructor

        public CameraColorRGBDesign(EditorOptions options)
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

        private Model3DGroup _geometries = null;
        public override Model3D Model
        {
            get
            {
                if (_geometries == null)
                {
                    _geometries = CreateGeometry(false);
                }

                return _geometries;
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
            return CreateCameraCollisionHull(world, this.Scale, this.Orientation, this.Position);
        }
        public override UtilityNewt.IObjectMassBreakdown GetMassBreakdown(double cellSize)
        {
            return GetCameraMassBreakdown(ref _massBreakdown, this.Scale, cellSize);
        }

        // Just going with a sphere for the physics
        internal static CollisionHull CreateCameraCollisionHull(WorldBase world, Vector3D scale, Quaternion orientation, Point3D position)
        {
            Transform3DGroup transform = new Transform3DGroup();
            //transform.Children.Add(new ScaleTransform3D(scale));		// it ignores scale
            transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(orientation)));
            transform.Children.Add(new TranslateTransform3D(position.ToVector()));

            Vector3D size = new Vector3D(scale.X * SCALE * .5d, scale.Y * SCALE * .5d, scale.Z * SCALE * .5d);

            return CollisionHull.CreateSphere(world, 0, size, transform.Value);
        }
        internal static UtilityNewt.IObjectMassBreakdown GetCameraMassBreakdown(ref Tuple<UtilityNewt.IObjectMassBreakdown, Vector3D, double> existing, Vector3D scale, double cellSize)
        {
            if (existing != null && existing.Item2 == scale && existing.Item3 == cellSize)
            {
                // This has already been built for this size
                return existing.Item1;
            }

            // Convert this.Scale into a size that the mass breakdown will use
            Vector3D size = new Vector3D(scale.X * SCALE, scale.Y * SCALE, scale.Z * SCALE);

            var breakdown = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Sphere, UtilityNewt.MassDistribution.Uniform, size, cellSize);

            // Store this
            existing = new Tuple<UtilityNewt.IObjectMassBreakdown, Vector3D, double>(breakdown, scale, cellSize);

            // Exit Function
            return existing.Item1;
        }

        public override PartToolItemBase GetToolItem()
        {
            return new CameraColorRGBToolItem(this.Options);
        }

        #endregion

        #region Private Methods

        private Model3DGroup CreateGeometry(bool isFinal)
        {
            return CreateGeometry(this.MaterialBrushes, base.SelectionEmissives,
                GetTransformForGeometry(isFinal),
                WorldColors.CameraBase, WorldColors.CameraBaseSpecular, WorldColors.CameraLens, WorldColors.CameraLensSpecular,
                isFinal);
        }

        internal static Model3DGroup CreateGeometry(List<MaterialColorProps> materialBrushes, List<EmissiveMaterial> selectionEmissives, Transform3D transform, Color cameraBase, SpecularMaterial cameraBaseSpecular, Color cameraLensColor, SpecularMaterial cameraLensSpecular, bool isFinal)
        {
            ScaleTransform3D scaleTransformLocal = new ScaleTransform3D(SCALE, SCALE, SCALE);

            int domeSegments = isFinal ? 2 : 10;
            int cylinderSegments = isFinal ? 6 : 35;

            Model3DGroup retVal = new Model3DGroup();

            GeometryModel3D geometry;
            MaterialGroup material;
            DiffuseMaterial diffuse;
            SpecularMaterial specular;

            Transform3DGroup transformGroup = new Transform3DGroup();
            transformGroup.Children.Add(new TranslateTransform3D(0, 0, (HEIGHT / 2d) - .15d));
            transformGroup.Children.Add(scaleTransformLocal);

            #region Spotlight

            //// Even when I make it extreme, it doesn't seem to make the gradient brighter
            //SpotLight spotLight = new SpotLight();
            //spotLight.Color = Colors.White;
            ////spotLight.LinearAttenuation = 1d;
            //spotLight.LinearAttenuation = .1d;
            //spotLight.Range = 10;
            //spotLight.InnerConeAngle = 66;
            //spotLight.OuterConeAngle = 80;
            //spotLight.Direction = new Vector3D(0, 0, -1);
            //spotLight.Transform = new TranslateTransform3D(0, 0, 1);

            //retVal.Children.Add(spotLight);

            #endregion

            #region Back Lens

            if (!isFinal)
            {
                geometry = new GeometryModel3D();
                material = new MaterialGroup();

                RadialGradientBrush eyeBrush = new RadialGradientBrush();
                eyeBrush.GradientStops.Add(new GradientStop(UtilityWPF.ColorFromHex("#FFFFEA00"), 0d));
                eyeBrush.GradientStops.Add(new GradientStop(UtilityWPF.ColorFromHex("#FFF5E100"), 0.0187702d));
                eyeBrush.GradientStops.Add(new GradientStop(UtilityWPF.ColorFromHex("#FFECD800"), 0.0320388d));
                eyeBrush.GradientStops.Add(new GradientStop(UtilityWPF.ColorFromHex("#FFD46C00"), 0.0485437d));
                eyeBrush.GradientStops.Add(new GradientStop(UtilityWPF.ColorFromHex("#FFBC0000"), 0.104167d));
                eyeBrush.GradientStops.Add(new GradientStop(UtilityWPF.ColorFromHex("#FF8E0000"), 0.267322d));
                eyeBrush.GradientStops.Add(new GradientStop(UtilityWPF.ColorFromHex("#FF600000"), 0.486408d));
                eyeBrush.GradientStops.Add(new GradientStop(UtilityWPF.ColorFromHex("#FF3E0000"), 0.61068d));
                eyeBrush.GradientStops.Add(new GradientStop(UtilityWPF.ColorFromHex("#FF1D0000"), 0.713592d));
                eyeBrush.GradientStops.Add(new GradientStop(UtilityWPF.ColorFromHex("#FF0E0000"), 0.760544d));
                eyeBrush.GradientStops.Add(new GradientStop(UtilityWPF.ColorFromHex("#FF000000"), 1d));

                diffuse = new DiffuseMaterial(eyeBrush);
                materialBrushes.Add(new MaterialColorProps(diffuse, cameraLensColor));		// using the final's lens color, because it's a solid color
                material.Children.Add(diffuse);

                //if (!isFinal)
                //{
                EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
                material.Children.Add(selectionEmissive);
                selectionEmissives.Add(selectionEmissive);
                //}

                geometry.Material = material;
                geometry.BackMaterial = material;

                geometry.Geometry = UtilityWPF.GetCircle2D(cylinderSegments, transformGroup, transformGroup);

                retVal.Children.Add(geometry);
            }

            #endregion

            #region Glass Cover

            geometry = new GeometryModel3D();
            material = new MaterialGroup();

            if (isFinal)
            {
                material.Children.Add(new DiffuseMaterial(new SolidColorBrush(cameraLensColor)));		// no need to add these to this.MaterialBrushes (those are only for editing)
                material.Children.Add(cameraLensSpecular);
            }
            else
            {
                //NOTE: Not using the world color, because that's for final.  The editor has a HAL9000 eye, and this is a glass plate
                Color color = Color.FromArgb(26, 255, 255, 255);
                diffuse = new DiffuseMaterial(new SolidColorBrush(color));
                materialBrushes.Add(new MaterialColorProps(diffuse, color));
                material.Children.Add(diffuse);
                specular = new SpecularMaterial(new SolidColorBrush(Color.FromArgb(224, 255, 255, 255)), 95d);
                materialBrushes.Add(new MaterialColorProps(specular));
                material.Children.Add(specular);

                EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
                material.Children.Add(selectionEmissive);
                selectionEmissives.Add(selectionEmissive);
            }

            geometry.Material = material;
            //NOTE: The position handed to the camera pool is the center of this camera.  So need to leave the back material null, or it would
            //be like taking pictures with the lens cap on
            //geometry.BackMaterial = material;

            List<TubeRingBase> rings = new List<TubeRingBase>();
            rings.Add(new TubeRingRegularPolygon(0, false, 1, 1, false));
            rings.Add(new TubeRingDome(.15, false, domeSegments));

            geometry.Geometry = UtilityWPF.GetMultiRingedTube(cylinderSegments, rings, true, false, transformGroup);

            retVal.Children.Add(geometry);

            #endregion

            #region Silver Ring

            if (!isFinal)
            {
                geometry = new GeometryModel3D();
                material = new MaterialGroup();
                Color color = Color.FromRgb(90, 90, 90);
                diffuse = new DiffuseMaterial(new SolidColorBrush(color));
                materialBrushes.Add(new MaterialColorProps(diffuse, color));
                material.Children.Add(diffuse);
                specular = new SpecularMaterial(Brushes.White, 100d);
                materialBrushes.Add(new MaterialColorProps(specular));
                material.Children.Add(specular);

                //if (!isFinal)
                //{
                EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
                material.Children.Add(selectionEmissive);
                selectionEmissives.Add(selectionEmissive);
                //}

                geometry.Material = material;
                geometry.BackMaterial = material;

                geometry.Geometry = UtilityWPF.GetRing(cylinderSegments, .97, 1.03, .05, transformGroup);

                retVal.Children.Add(geometry);
            }

            #endregion

            #region Back Cover

            geometry = new GeometryModel3D();
            material = new MaterialGroup();
            diffuse = new DiffuseMaterial(new SolidColorBrush(cameraBase));
            materialBrushes.Add(new MaterialColorProps(diffuse, cameraBase));
            material.Children.Add(diffuse);
            specular = cameraBaseSpecular;
            materialBrushes.Add(new MaterialColorProps(specular));
            material.Children.Add(specular);

            if (!isFinal)
            {
                EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
                material.Children.Add(selectionEmissive);
                selectionEmissives.Add(selectionEmissive);
            }

            geometry.Material = material;
            geometry.BackMaterial = material;

            rings = new List<TubeRingBase>();
            rings.Add(new TubeRingDome(0, false, domeSegments));
            rings.Add(new TubeRingRegularPolygon(1.5, false, 1, 1, false));

            transformGroup = new Transform3DGroup();
            transformGroup.Children.Add(new TranslateTransform3D(0, 0, 1.65 / -2d));
            transformGroup.Children.Add(scaleTransformLocal);

            geometry.Geometry = UtilityWPF.GetMultiRingedTube(cylinderSegments, rings, true, false, transformGroup);

            retVal.Children.Add(geometry);

            #endregion

            // Transform
            retVal.Transform = transform;

            // Exit Function
            return retVal;
        }

        #endregion
    }

    #endregion
    #region Class: CameraColorRGB

    public class CameraColorRGB : PartBase, INeuronContainer, IPartUpdatable, ICameraPoolCamera
    {
        #region Class: OverlayResult

        internal class OverlayResult
        {
            public OverlayResult(int x, int y, double percent)
            {
                this.X = x;
                this.Y = y;
                this.Percent = percent;
            }

            public readonly int X;
            public readonly int Y;
            public readonly double Percent;
        }

        #endregion

        #region Declaration Section

        public const string PARTTYPE = "CameraColorRGB";

        private readonly ItemOptions _itemOptions;

        private readonly IContainer _energyTanks;

        private readonly CameraPool _cameraPool;

        private readonly Neuron_SensorPosition[] _neuronsR;
        private readonly Neuron_SensorPosition[] _neuronsG;
        private readonly Neuron_SensorPosition[] _neuronsB;

        private readonly OverlayResult[][] _overlayR;
        private readonly OverlayResult[][] _overlayG;
        private readonly OverlayResult[][] _overlayB;

        private readonly DoubleVector _cameraLookInitial = new DoubleVector(0, 0, 1, 0, 1, 0);

        private readonly double _volume;		// this is used to calculate energy draw

        #endregion

        #region Constructor

        public CameraColorRGB(EditorOptions options, ItemOptions itemOptions, ShipPartDNA dna, IContainer energyTanks, CameraPool cameraPool)
            : base(options, dna)
        {
            _itemOptions = itemOptions;
            _energyTanks = energyTanks;
            _cameraPool = cameraPool;

            this.Design = new CameraColorRGBDesign(options);
            this.Design.SetDNA(dna);

            double radius;
            GetMass(out _mass, out _volume, out radius, dna, itemOptions);

            this.Radius = radius;
            _scaleActual = new Vector3D(radius * 2d, radius * 2d, radius * 2d);

            //TODO: Rework this method to take in the number of cone types (plates of neurons), instead of hardcoding to 3 and having all these out params
            CreateNeurons(out _neuronsR, out _neuronsG, out _neuronsB, out _overlayR, out _overlayG, out _overlayB, out _pixelWidthHeight, dna, itemOptions, itemOptions.CameraColorRGB_NeuronDensity);

            if (_cameraPool != null)
            {
                _cameraPool.Add(this);
            }
        }

        #endregion

        #region IDisposable Members

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_cameraPool != null)
                {
                    _cameraPool.Remove(this);
                }
            }

            base.Dispose(disposing);
        }

        #endregion

        #region INeuronContainer Members

        public IEnumerable<INeuron> Neruons_Readonly
        {
            get
            {
                return UtilityCore.Iterate(_neuronsR, _neuronsG, _neuronsB);
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
                return UtilityCore.Iterate(_neuronsR, _neuronsG, _neuronsB);
            }
        }

        private volatile NeuronContainerType _neuronContainerType = NeuronContainerType.Sensor;
        public NeuronContainerType NeuronContainerType
        {
            get
            {
                return _neuronContainerType;
            }
            set
            {
                switch (value)
                {
                    case NeuronContainerType.Sensor:
                    case NeuronContainerType.None:
                        _neuronContainerType = value;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException("Can only set this camera as a sensor or none: " + value.ToString());
                }
            }
        }

        public double Radius
        {
            get;
            private set;
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
            //NOTE: This method is called by the physics thread, and is only worried about energy tanks.  StoreSnapshot() is called
            //from the camera pool thread, and the individual neurons are read independently by the ai pool thread

            if (_energyTanks == null || _energyTanks.RemoveQuantity(elapsedTime * _volume * _itemOptions.CameraColorRGB_AmountToDraw * ItemOptions.ENERGYDRAWMULT, true) > 0d)
            {
                // The energy tank didn't have enough
                //NOTE: To be clean, I should set the neuron outputs to zero, but anything pulling from them should be checking this
                //anyway.  So save the processor (also, because of threading, setting them to zero isn't as atomic as this property)
                _isOn = false;
                return;
            }

            _isOn = true;
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
        #region ICameraPoolCamera Members

        private readonly int _pixelWidthHeight;
        public int PixelWidthHeight
        {
            get
            {
                return _pixelWidthHeight;
            }
        }

        public Tuple<Point3D, DoubleVector> GetWorldLocation_Camera()
        {
            var location = base.GetWorldLocation();

            // Use the orientation to rotate a look and up vector
            return Tuple.Create(location.Item1, location.Item2.GetRotatedVector(_cameraLookInitial));
        }

        public void StoreSnapshot(IBitmapCustom bitmap)
        {
            const double INVERSE255 = 1d / 255d;

            this.Bitmap = Tuple.Create(TokenGenerator.NextToken(), bitmap);

            //TODO: This is called from the camera pool's thread, which is pretty taxed, so may want this logic in a task

            // R
            for (int cntr = 0; cntr < _neuronsR.Length; cntr++)
            {
                byte[] color = GetColor(_overlayR[cntr], bitmap);
                _neuronsR[cntr].Value = color[1] * INVERSE255;
            }

            // G
            for (int cntr = 0; cntr < _neuronsG.Length; cntr++)
            {
                byte[] color = GetColor(_overlayG[cntr], bitmap);
                _neuronsG[cntr].Value = color[2] * INVERSE255;
            }

            // B
            for (int cntr = 0; cntr < _neuronsB.Length; cntr++)
            {
                byte[] color = GetColor(_overlayB[cntr], bitmap);
                _neuronsB[cntr].Value = color[3] * INVERSE255;
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

        /// <summary>
        /// Item1=Token
        /// Item2=Bitmap
        /// </summary>
        /// <remarks>
        /// This is exposed so that other parts could use the raw image instead of the neurons
        /// </remarks>
        public volatile Tuple<long, IBitmapCustom> Bitmap = null;

        #endregion

        #region Private Methods

        internal static void GetMass(out double mass, out double volume, out double radius, ShipPartDNA dna, ItemOptions itemOptions)
        {
            radius = (dna.Scale.X + dna.Scale.Y + dna.Scale.Z) / (3d * 2d);		// they should be identical anyway
            radius *= CameraColorRGBDesign.SCALE;		// scale it

            volume = 4d / 3d * Math.PI * radius * radius * radius;
            mass = volume * itemOptions.Camera_Density;
        }

        internal static void CreateNeurons(out Neuron_SensorPosition[] neuronsR, out Neuron_SensorPosition[] neuronsG, out Neuron_SensorPosition[] neuronsB, out OverlayResult[][] overlayR, out OverlayResult[][] overlayG, out OverlayResult[][] overlayB, out int pixelWidthHeight, ShipPartDNA dna, ItemOptions itemOptions, double neuronDensity)
        {
            const int MINPIXELWIDTH = 16;

            #region Calculate counts

            // Figure out how many neurons to make
            //NOTE: This radius isn't taking SCALE into account.  The other neural parts do this as well, so the neural density properties can be more consistent
            double radius = (dna.Scale.X + dna.Scale.Y + dna.Scale.Z) / (3d * 2d);		// xyz should all be the same anyway
            double area = Math.Pow(radius, itemOptions.Sensor_NeuronGrowthExponent);

            int neuronCount = Convert.ToInt32(Math.Ceiling(neuronDensity * area));
            if (neuronCount == 0)
            {
                neuronCount = 1;
            }

            // Figure out how many pixels to make
            pixelWidthHeight = neuronCount / 9;     // dividing by 3 to get the number of neurons in a single plate.  divide that by 3, because that's a good ratio of neuron cells to pixels
            if (pixelWidthHeight < MINPIXELWIDTH)
            {
                pixelWidthHeight = MINPIXELWIDTH;
            }

            #endregion

            #region Neurons

            // Place them evenly in a sphere
            //NOTE: An interesting side effect of calling this for each generation is that the parent may not have been perfectly evenly spaced, but calling this
            //again will slightly refine the positions
            Vector3D[][] positions = GetNeuronPositions(dna.Neurons, neuronCount, 3, radius);

            // Create neurons
            neuronsR = positions[0].Select(o => new Neuron_SensorPosition(o.ToPoint(), true)).ToArray();
            neuronsG = positions[1].Select(o => new Neuron_SensorPosition(o.ToPoint(), true)).ToArray();
            neuronsB = positions[2].Select(o => new Neuron_SensorPosition(o.ToPoint(), true)).ToArray();

            #endregion

            #region Polygons around neurons

            // Figure out which pixels each neuron intersects with
            VoronoiResult2D[] voronoi = new VoronoiResult2D[3];
            voronoi[0] = Math2D.CapVoronoiCircle(Math2D.GetVoronoi(positions[0].Select(o => new Point(o.X, o.Y)).ToArray(), true));
            voronoi[1] = Math2D.CapVoronoiCircle(Math2D.GetVoronoi(positions[1].Select(o => new Point(o.X, o.Y)).ToArray(), true));
            voronoi[2] = Math2D.CapVoronoiCircle(Math2D.GetVoronoi(positions[2].Select(o => new Point(o.X, o.Y)).ToArray(), true));

            #region Figure out the extremes

            Point[] allEdgePoints = voronoi.SelectMany(o => o.EdgePoints).ToArray();

            Point min = new Point(allEdgePoints.Min(o => o.X), allEdgePoints.Min(o => o.Y));
            Point max = new Point(allEdgePoints.Max(o => o.X), allEdgePoints.Max(o => o.Y));

            double width = max.X - min.X;
            double height = max.Y - min.Y;

            // Enlarge a bit
            min.X -= width * .05d;
            min.Y -= height * .05d;
            max.X += width * .05d;
            max.Y += height * .05d;

            width = max.X - min.X;
            height = max.Y - min.Y;

            Vector offset = new Vector(-min.X, -min.Y);

            #endregion

            //  Figure out which pixels each polygon overlaps
            overlayR = GetIntersections(new Size(width, height), pixelWidthHeight, pixelWidthHeight, GetPolygons(voronoi[0], offset));
            overlayG = GetIntersections(new Size(width, height), pixelWidthHeight, pixelWidthHeight, GetPolygons(voronoi[1], offset));
            overlayB = GetIntersections(new Size(width, height), pixelWidthHeight, pixelWidthHeight, GetPolygons(voronoi[2], offset));

            #endregion
        }

        internal static Vector3D[][] GetNeuronPositions(Point3D[] dnaPositions, int count, int numPlates, double radius)
        {
            Vector3D[][] retVal = GetNeuronPositionsInitial(dnaPositions, count, numPlates, radius);

            for (int cntr = 0; cntr < retVal.Length; cntr++)
            {
                // Convert to 2D (avoiding linq for speed reasons)
                Vector[] evenPoints = new Vector[retVal[cntr].Length];
                for (int inner = 0; inner < evenPoints.Length; inner++)
                {
                    evenPoints[inner] = new Vector(retVal[cntr][inner].X, retVal[cntr][inner].Y);
                }

                // Evenly distribute these 2D points
                evenPoints = Math3D.GetRandomVectors_Circular_CenterPacked(evenPoints, radius, .03d, 1000, null, null, null);

                // Join those moved points with the initial (to get the Z)
                Vector3D[] newPoints = new Vector3D[evenPoints.Length];
                for (int inner = 0; inner < newPoints.Length; inner++)
                {
                    newPoints[inner] = new Vector3D(evenPoints[inner].X, evenPoints[inner].Y, retVal[cntr][inner].Z);
                }

                // Swap out with the new points
                retVal[cntr] = newPoints;
            }

            // Exit Function
            return retVal;
        }

        private static Vector3D[][] GetNeuronPositionsInitial(Point3D[] dnaPositions, int count, int numPlates, double radius)
        {
            //TODO: When reducing/increasing, it is currently just being random.  It may be more realistic to take proximity into account.  Play
            // a variant of conway's game of life or something

            Vector3D[][] retVal;

            double[] plateZs = GetNeuronPositionsInitialSprtZs(numPlates, radius);

            if (dnaPositions == null)
            {
                #region Create new

                // Figure out how many neurons to put in each plate
                int[] countsPerPlate = GetNeuronPositionsInitialSprtInitialPlateBreakdown(count, numPlates);

                retVal = new Vector3D[numPlates][];

                for (int plateCntr = 0; plateCntr < numPlates; plateCntr++)
                {
                    Vector3D[] plate = new Vector3D[countsPerPlate[plateCntr]];
                    for (int cntr = 0; cntr < plate.Length; cntr++)
                    {
                        plate[cntr] = Math3D.GetRandomVector_Circular(radius);
                        plate[cntr].Z = plateZs[plateCntr];
                    }

                    retVal[plateCntr] = plate;
                }

                #endregion
            }
            else
            {
                // Separate the existing into plates
                List<Vector3D>[] separated = GetNeuronPositionsInitialSprtSeparateExisting(dnaPositions, plateZs);

                if (dnaPositions.Length > count)
                {
                    #region Reduce

                    int reduceCount = dnaPositions.Length - count;

                    for (int cntr = 0; cntr < reduceCount; cntr++)
                    {
                        // Figure out which plate to remove from
                        int index = GetNeuronPositionsInitialSprtGetRemoveIndex(separated);

                        separated[index].RemoveAt(StaticRandom.Next(separated[index].Count));
                    }

                    #endregion
                }
                else if (dnaPositions.Length < count)
                {
                    #region Increase

                    int increaseCount = count - dnaPositions.Length;

                    for (int cntr = 0; cntr < increaseCount; cntr++)
                    {
                        // Figure out which plate to add to
                        int index = GetNeuronPositionsInitialSprtGetAddIndex(separated);

                        Vector3D newNeuron = Math3D.GetRandomVector_Circular(radius);
                        newNeuron.Z = plateZs[index];

                        separated[index].Add(newNeuron);
                    }

                    #endregion
                }

                // Convert the lists to arrays
                retVal = separated.Select(o => o.ToArray()).ToArray();
            }

            // Exit Function
            return retVal;
        }
        private static double[] GetNeuronPositionsInitialSprtZs(int numPlates, double radius)
        {
            if (numPlates == 1)
            {
                return new double[] { 0d };
            }

            // I don't want the plate's Z to go all the way to the edge of radius, so suck it in a bit
            double max = radius * .75d;

            double gap = (max * 2d) / Convert.ToDouble(numPlates - 1);		// multiplying by 2 because radius is only half

            double[] retVal = new double[numPlates];
            double current = max * -1d;

            for (int cntr = 0; cntr < numPlates; cntr++)
            {
                retVal[cntr] = current;
                current += gap;
            }

            return retVal;
        }
        private static int[] GetNeuronPositionsInitialSprtInitialPlateBreakdown(int count, int numPlates)
        {
            int[] retVal = new int[numPlates];

            int average = count / numPlates;
            int remaining = count;

            for (int cntr = 0; cntr < numPlates; cntr++)
            {
                // Figure out how many to make
                if (remaining == 0)
                {
                    retVal[cntr] = 0;
                    continue;
                }

                retVal[cntr] = average;
                if (remaining % numPlates > 0)
                {
                    // Count isn't evenly distributable by numPlates, and there's enough room for a remainder, so give it to this one
                    retVal[cntr]++;
                }

                remaining -= retVal[cntr];
            }

            // Some of the plates may have remainders, and the above loop always puts them in the first plates, so shuffle it
            retVal = UtilityCore.RandomRange(0, numPlates).Select(o => retVal[o]).ToArray();

            // Exit Function
            return retVal;
        }
        private static List<Vector3D>[] GetNeuronPositionsInitialSprtSeparateExisting(Point3D[] dnaPositions, double[] plateZs)
        {
            List<Vector3D>[] retVal = Enumerable.Range(0, dnaPositions.Length).Select(o => new List<Vector3D>()).ToArray();

            foreach (Point3D position in dnaPositions)
            {
                // Get the plate that this is closest to
                int index = GetNearest(plateZs, position.Z);

                // Add to that plate
                retVal[index].Add(new Vector3D(position.X, position.Z, plateZs[index]));
            }

            // Exit Function
            return retVal;
        }
        private static int GetNeuronPositionsInitialSprtGetAddIndex(List<Vector3D>[] separated)
        {
            // See which plate has the smallest count
            int min = separated.Min(o => o.Count);

            // Get the index of all the plates with that count
            List<int> candidates = new List<int>();

            for (int cntr = 0; cntr < separated.Length; cntr++)
            {
                if (separated[cntr].Count == min)
                {
                    candidates.Add(cntr);
                }
            }

            // Pick a random plate from that list
            return candidates[StaticRandom.Next(candidates.Count)];
        }
        private static int GetNeuronPositionsInitialSprtGetRemoveIndex(List<Vector3D>[] separated)
        {
            // See which plate has the largest count
            int max = separated.Max(o => o.Count);

            // Get the index of all the plates with that count
            List<int> candidates = new List<int>();

            for (int cntr = 0; cntr < separated.Length; cntr++)
            {
                if (separated[cntr].Count == max)
                {
                    candidates.Add(cntr);
                }
            }

            // Pick a random plate from that list
            return candidates[StaticRandom.Next(candidates.Count)];
        }

        private static int GetNearest(double[] values, double test)
        {
            int retVal = -1;
            double minDist = double.MaxValue;

            for (int cntr = 0; cntr < values.Length; cntr++)
            {
                double dist = Math.Abs(test - values[cntr]);

                if (dist < minDist)
                {
                    retVal = cntr;
                    minDist = dist;
                }
            }

            if (retVal < 0)
            {
                throw new ApplicationException("Didn't find an index");
            }

            return retVal;
        }

        private static Point[][] GetPolygons(VoronoiResult2D voronoi, Vector offset)
        {
            Point[][] retVal = new Point[voronoi.ControlPoints.Length][];

            for (int cntr = 0; cntr < voronoi.ControlPoints.Length; cntr++)
            {
                retVal[cntr] = voronoi.GetPolygon(cntr, 1).     // don't need to worry about ray length, they are all segments
                    Select(o => o + offset).        // shifting the points by offset
                    ToArray();
            }

            return retVal;
        }

        /// <summary>
        /// This figures out which tiles each polygon intersects
        /// </summary>
        /// <param name="size">The size of the grid</param>
        /// <param name="pixelsX">How many tiles along X</param>
        /// <param name="pixelsY">How many tiles along Y</param>
        /// <param name="triangles">The triangles should be shifted to cover the grid, Z is ignored</param>
        /// <returns>
        /// [index of triangle][array of tiles it covers]
        /// </returns>
        private static OverlayResult[][] GetIntersections(Size size, int pixelsX, int pixelsY, Point[][] polygons)
        {
            OverlayResult[][] retVal = new OverlayResult[polygons.Length][];

            // Convert each pixel into a rectangle
            Tuple<Rect, int, int>[] tiles = GetTiles(size, pixelsX, pixelsY);

            for (int cntr = 0; cntr < polygons.Length; cntr++)
            {
                // See which tiles this triangle intersects (and how much of each tile)
                retVal[cntr] = IntersectTiles(polygons[cntr], tiles);
            }

            // Exit Function
            return retVal;
        }

        private static Tuple<Rect, int, int>[] GetTiles(Size size, int pixelsX, int pixelsY)
        {
            Tuple<Rect, int, int>[] retVal = new Tuple<Rect, int, int>[pixelsX * pixelsY];

            Size cellSize = new Size(size.Width / Convert.ToDouble(pixelsX), size.Height / Convert.ToDouble(pixelsY));

            for (int y = 0; y < pixelsY; y++)
            {
                int offsetY = y * pixelsX;

                for (int x = 0; x < pixelsX; x++)
                {
                    retVal[offsetY + x] = Tuple.Create(new Rect(cellSize.Width * Convert.ToDouble(x), cellSize.Height * Convert.ToDouble(y), cellSize.Width, cellSize.Height), x, y);
                }
            }

            return retVal;
        }

        private static OverlayResult[] IntersectTiles(Point[] polygon, Tuple<Rect, int, int>[] tiles)
        {
            List<OverlayResult> retVal = new List<OverlayResult>();

            #region Get polygon AABB

            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;

            for (int cntr = 0; cntr < polygon.Length; cntr++)
            {
                if (polygon[cntr].X < minX)
                {
                    minX = polygon[cntr].X;
                }

                if (polygon[cntr].X > maxX)
                {
                    maxX = polygon[cntr].X;
                }

                if (polygon[cntr].Y < minY)
                {
                    minY = polygon[cntr].Y;
                }

                if (polygon[cntr].Y > maxY)
                {
                    maxY = polygon[cntr].Y;
                }
            }

            #endregion

            foreach (var tile in tiles)
            {
                // AABB check
                if (tile.Item1.Left > maxX)
                {
                    continue;		// polygon is left of the rectangle
                }
                else if (tile.Item1.Right < minX)
                {
                    continue;		// polygon is right of the rectangle
                }
                else if (tile.Item1.Top > maxY)
                {
                    continue;		// polygon is above rectangle
                }
                else if (tile.Item1.Bottom < minY)
                {
                    continue;		// polygon is below rectangle
                }

                // See if the polygon is completely inside the rectangle
                if (minX >= tile.Item1.Left && maxX <= tile.Item1.Right && minY >= tile.Item1.Top && maxY <= tile.Item1.Bottom)
                {
                    double areaPoly = Math2D.GetAreaPolygon(polygon);
                    double areaRect = tile.Item1.Width * tile.Item1.Height;

                    if (areaPoly > areaRect)
                    {
                        throw new ApplicationException(string.Format("Area of contained polygon is larger than the rectangle that contains it: polygon={0}, rectangle={1}", areaPoly.ToString(), areaRect.ToString()));
                    }

                    retVal.Add(new OverlayResult(tile.Item2, tile.Item3, areaPoly / areaRect));
                    continue;
                }

                // Intersect polygon with rect
                var percent = GetPercentCovered(polygon, tile.Item1);
                if (percent != null)
                {
                    retVal.Add(new OverlayResult(tile.Item2, tile.Item3, percent.Item1));
                }
            }

            return retVal.ToArray();
        }

        private static Tuple<double, Point[]> GetPercentCovered(Point[] polygon, Rect rect)
        {
            // Figure out the intersected polygon
            Point[] intersection = Math2D.GetIntersection_Polygon_Polygon(
                polygon,
                new Point[] { rect.TopLeft, rect.TopRight, rect.BottomRight, rect.BottomLeft });

            if (intersection == null || intersection.Length == 0)
            {
                return null;
            }

            // Calculate the area of the polygon
            double areaPolygon = Math2D.GetAreaPolygon(intersection);

            double areaRect = rect.Width * rect.Height;

            if (areaPolygon > areaRect)
            {
                if (areaPolygon > areaRect * 1.01)
                {
                    throw new ApplicationException(string.Format("Area of intersected polygon is larger than the rectangle that clipped it: polygon={0}, rectangle={1}", areaPolygon.ToString(), areaRect.ToString()));
                }
                areaPolygon = areaRect;
            }

            return Tuple.Create(areaPolygon / areaRect, intersection);
        }

        /// <summary>
        /// a1 is line1 start, a2 is line1 end, b1 is line2 start, b2 is line2 end
        /// </summary>
        /// <remarks>
        /// Got this here:
        /// http://stackoverflow.com/questions/14480124/how-do-i-detect-triangle-and-rectangle-intersection
        /// 
        /// A similar article:
        /// http://www.flipcode.com/archives/Point-Plane_Collision.shtml
        /// </remarks>
        private static Vector? Intersects(Vector a1, Vector a2, Vector b1, Vector b2)
        {
            Vector b = Vector.Subtract(a2, a1);
            Vector d = Vector.Subtract(b2, b1);
            double bDotDPerp = (b.X * d.Y) - (b.Y * d.X);

            // if b dot d == 0, it means the lines are parallel so have infinite intersection points
            if (Math1D.IsNearZero(bDotDPerp))
                return null;

            Vector c = Vector.Subtract(b1, a1);
            double t = (c.X * d.Y - c.Y * d.X) / bDotDPerp;
            if (t < 0 || t > 1)
                return null;

            double u = (c.X * b.Y - c.Y * b.X) / bDotDPerp;
            if (u < 0 || u > 1)
                return null;

            // Return the intersection point
            return Vector.Add(a1, Vector.Multiply(b, t));
        }

        internal static byte[] GetColor(OverlayResult[] pixels, IBitmapCustom bitmap)
        {
            Tuple<byte[], double>[] colors = new Tuple<byte[], double>[pixels.Length];

            for (int cntr = 0; cntr < pixels.Length; cntr++)
            {
                colors[cntr] = Tuple.Create(bitmap.GetColor_Byte(pixels[cntr].X, pixels[cntr].Y), pixels[cntr].Percent);
            }

            return UtilityWPF.AverageColors(colors);
        }

        #endregion
    }

    #endregion
}
