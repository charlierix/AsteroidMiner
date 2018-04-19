using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.GameItems.ShipParts
{
    #region class: CameraHardCodedToolItem

    public class CameraHardCodedToolItem : PartToolItemBase
    {
        #region Constructor

        public CameraHardCodedToolItem(EditorOptions options)
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
                return "Camera (hard coded)";
            }
        }
        public override string Description
        {
            get
            {
                return "Consumes energy, used to see other objects (doesn't use vision, just queries the map directly)";
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
            return new CameraHardCodedDesign(this.Options, false);
        }

        #endregion
    }

    #endregion
    #region class: CameraHardCodedDesign

    public class CameraHardCodedDesign : PartDesignBase
    {
        #region Declaration Section

        private const double HEIGHT = 1.65d;
        internal const double SCALE = .33d / HEIGHT;

        public const PartDesignAllowedScale ALLOWEDSCALE = PartDesignAllowedScale.XYZ;		// This is here so the scale can be known through reflection

        private MassBreakdownCache _massBreakdown = null;

        #endregion

        #region Constructor

        public CameraHardCodedDesign(EditorOptions options, bool isFinalModel)
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

        private Model3DGroup _model = null;
        public override Model3D Model
        {
            get
            {
                if (_model == null)
                {
                    _model = CreateGeometry(this.IsFinalModel);
                }

                return _model;
            }
        }

        #endregion

        #region Public Methods

        public override ShipPartDNA GetDNA()
        {
            CameraHardCodedDNA retVal = new CameraHardCodedDNA();

            base.FillDNA(retVal);

            //TODO: Tack on custom stuff

            return retVal;
        }
        public override void SetDNA(ShipPartDNA dna)
        {
            base.StoreDNA(dna);

            if (dna is CameraHardCodedDNA)
            {
                //TODO: Store the custom stuff
            }

        }

        public override CollisionHull CreateCollisionHull(WorldBase world)
        {
            return CameraColorRGBDesign.CreateCameraCollisionHull(world, this.Scale, this.Orientation, this.Position);
        }
        public override UtilityNewt.IObjectMassBreakdown GetMassBreakdown(double cellSize)
        {
            return CameraColorRGBDesign.GetCameraMassBreakdown(ref _massBreakdown, this.Scale, cellSize);
        }

        public override PartToolItemBase GetToolItem()
        {
            return new CameraHardCodedToolItem(this.Options);
        }

        #endregion

        #region Private Methods

        private Model3DGroup CreateGeometry(bool isFinal)
        {
            return CameraColorRGBDesign.CreateGeometry(this.MaterialBrushes, base.SelectionEmissives,
                GetTransformForGeometry(isFinal),
                WorldColors.CameraBase_Color, WorldColors.CameraBase_Specular, WorldColors.CameraHardCodedLens_Color, WorldColors.CameraLens_Specular,
                isFinal);
        }

        #endregion
    }

    #endregion
    #region class: CameraHardCoded

    public class CameraHardCoded : PartBase, INeuronContainer, IPartUpdatable
    {
        #region class: SamplePoint

        private class SamplePoint
        {
            public SamplePoint(Point3D positionNeuron, Vector3D positionWorld, double searchRadius, Neuron_SensorPosition[] neurons)
            {
                this.Position_Neuron = positionNeuron;
                this.Position_World = positionWorld;
                this.SearchRadius = searchRadius;
                this.Neurons = neurons;
            }

            //NOTE: These are the center point
            public readonly Point3D Position_Neuron;
            public readonly Vector3D Position_World;

            public readonly double SearchRadius;
            //TODO: Have some kind of dropoff function

            //TODO: There shouldn't just be one neuron, there should be the 5 classifiers, as well as others that represent quantity
            public readonly Neuron_SensorPosition[] Neurons;
        }

        #endregion

        #region Declaration Section

        public const string PARTTYPE = nameof(CameraHardCoded);

        private readonly object _lock = new object();

        private readonly ItemOptions _itemOptions;

        private readonly IContainer _energyTanks;
        private readonly Map _map;

        private readonly Neuron_SensorPosition[][] _neuronSets;
        private readonly Neuron_SensorPosition[] _neurons;
        private readonly double _neuronMaxRadius;

        private readonly SamplePoint[] _neuronPoints;

        private readonly double _volume;        // this is used to calculate energy draw

        private long _lastSnapshotToken = 0;

        #endregion

        #region Constructor

        //NOTE: It's ok to pass in the base dna type.  If the derived is passed in, those settings will be used
        public CameraHardCoded(EditorOptions options, ItemOptions itemOptions, ShipPartDNA dna, IContainer energyTanks, Map map)
            : base(options, dna, itemOptions.Camera_Damage.HitpointMin, itemOptions.Camera_Damage.HitpointSlope, itemOptions.Camera_Damage.Damage)
        {
            _itemOptions = itemOptions;
            _energyTanks = energyTanks;
            _map = map;

            this.Design = new CameraHardCodedDesign(options, true);
            this.Design.SetDNA(dna);

            double radius;
            CameraColorRGB.GetMass(out _mass, out _volume, out radius, dna, itemOptions);

            this.Radius = radius;
            _scaleActual = new Vector3D(radius * 2d, radius * 2d, radius * 2d);

            //TODO: design should have stored custom stuff if the dna has it.  Get/Set
            var neuronResults = CreateNeurons(dna, itemOptions);
            _neuronPoints = neuronResults.Item1;
            _neuronSets = neuronResults.Item2;
            _neurons = neuronResults.Item2.
                SelectMany(o => o).
                ToArray();
            _neuronMaxRadius = _neurons.Max(o => o.PositionLength);
        }

        #endregion

        #region INeuronContainer Members

        public IEnumerable<INeuron> Neruons_Readonly => _neurons;
        public IEnumerable<INeuron> Neruons_ReadWrite => Enumerable.Empty<INeuron>();
        public IEnumerable<INeuron> Neruons_Writeonly => Enumerable.Empty<INeuron>();

        public IEnumerable<INeuron> Neruons_All => _neurons;

        public NeuronContainerType NeuronContainerType => NeuronContainerType.Sensor;

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
            lock (_lock)
            {
                if (this.IsDestroyed || _energyTanks == null || _energyTanks.RemoveQuantity(elapsedTime * _volume * _itemOptions.GravitySensor_AmountToDraw * ItemOptions.ENERGYDRAWMULT, true) > 0d)
                {
                    // The energy tank didn't have enough
                    //NOTE: To be clean, I should set the neuron outputs to zero, but anything pulling from them should be checking this
                    //anyway.  So save the processor (also, because of threading, setting them to zero isn't as atomic as this property)
                    _isOn = false;
                    return;
                }

                _isOn = true;

                var snapshot = _map.LatestSnapshot;
                if (snapshot == null)
                {
                    foreach (SamplePoint neuron in _neuronPoints)
                    {
                        foreach (var actualNeuron in neuron.Neurons)
                        {
                            actualNeuron.Value = 0;
                        }
                    }

                    return;
                }

                long previous = Interlocked.Exchange(ref _lastSnapshotToken, snapshot.Token);
                if (previous == snapshot.Token)
                {
                    // Same as last tick, the neurons are already showing this
                    return;
                }

                var worldLoc = this.GetWorldLocation();

                Func<MapObjectInfo, double[]> classify = _itemOptions.CameraHardCoded_ClassifyObject;
                if (classify == null)
                {
                    classify = ClassifyObject;
                }

                foreach (SamplePoint neuron in _neuronPoints)
                {
                    Point3D worldPos = worldLoc.Item1 + worldLoc.Item2.GetRotatedVector(neuron.Position_World);

                    double[] combinedValues = new double[neuron.Neurons.Length];

                    foreach (MapObjectInfo mapObject in snapshot.GetItems(worldPos, neuron.SearchRadius))
                    {
                        double[] values = classify(mapObject);

                        if (values == null || values.Length != combinedValues.Length)
                        {
                            throw new ApplicationException("classification array is invalid");
                        }

                        for (int cntr = 0; cntr < values.Length; cntr++)
                        {
                            combinedValues[cntr] += values[cntr];
                        }
                    }

                    for (int cntr = 0; cntr < combinedValues.Length; cntr++)
                    {
                        // Cap -1 or 1
                        //TODO: May want to run through an SCurve instead
                        if (combinedValues[cntr] >= 0)
                        {
                            neuron.Neurons[cntr].Value = Math.Min(combinedValues[cntr], 1d);
                        }
                        else
                        {
                            neuron.Neurons[cntr].Value = Math.Max(combinedValues[cntr], -1d);
                        }
                    }
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

        #endregion

        #region Public Methods

        /// <summary>
        /// This is exposed for debug reasons
        /// </summary>
        /// <returns>
        /// Item1=position in model coords
        /// Item2=position in world coords
        /// Item3=search radius
        /// </returns>
        public Tuple<Point3D, Point3D, double>[] GetNeurons_DEBUG()
        {
            return _neuronPoints.
                Select(o => Tuple.Create(o.Position_Neuron, o.Position_World.ToPoint(), o.SearchRadius)).
                ToArray();
        }

        /// <summary>
        /// 0 = touch
        /// 1 = stay near
        /// 2 = neutral, basically ignore it (this is 0 to 1)
        /// 3 =  avoid
        /// 4 = attack
        /// </summary>
        /// <remarks>
        /// This default implementation is very simplistic.  It is meant to be overridden with ItemOptions.CameraHardCoded_ClassifyObject
        /// </remarks>
        public static double[] ClassifyObject(MapObjectInfo mapObject)
        {
            return new[]
            {
                0d,
                0d,
                mapObject == null ? 0d : 1d,
                0d,
                0d,
            };
        }

        #endregion

        #region Private Methods

        private static Tuple<SamplePoint[], Neuron_SensorPosition[][]> CreateNeurons(ShipPartDNA dna, ItemOptions itemOptions)
        {
            #region count

            // Figure out how many sample points to create
            //NOTE: radius assumes a sphere, but this camera is a cone.  Not perfect, but good enough
            double radius = (dna.Scale.X + dna.Scale.Y + dna.Scale.Z) / (3d * 2d);		// xyz should all be the same anyway
            double volume = Math.Pow(radius, itemOptions.Sensor_NeuronGrowthExponent);

            int numSamples = Convert.ToInt32(Math.Ceiling(itemOptions.CameraHardCoded_NeuronDensity * volume));
            if (numSamples == 0)
            {
                numSamples = 1;
            }

            #endregion

            //TODO: Calculate this based on the densitiy of sets inside the cone portion of the unit sphere
            double setSize = .1;

            #region positions

            // Get points in a cone
            //NOTE: Going with a narrower angle, because the neurons will go to the edges, but the neurons will be the center of a vision sphere -- so the actual vision will be larger than this cone
            Vector3D[] pointsModel = Math3D.GetRandomVectors_Cone_EvenDist(numSamples, CameraColorRGBDesign.CameraDirection, 60, .2, 1);

            // Turn into sensor neurons
            Neuron_SensorPosition[][] neurons = pointsModel.
                Select(o => GetNeuronSet(o.ToPoint(), setSize)).
                ToArray();

            double worldMax = itemOptions.CameraHardCoded_WorldMax;

            //NOTE: The term world is a bit misleading.  It's world distances, but still technically model coords.  It's just not neural's unit circle coords
            Point3D[] pointsWorld = pointsModel.
                Select(o =>
                {
                    double distanceFromOrigin = o.Length;
                    return o.ToUnit(false) * (worldMax * Math.Pow(distanceFromOrigin, 3));
                }).
                Select(o => o.ToPoint()).
                ToArray();

            #endregion

            #region vision radius

            // Delaunay
            //TODO: Throw out thin triangles
            Tetrahedron[] tetras = Math3D.GetDelaunay(pointsWorld, 10);

            double approximateRadius = worldMax * .667;

            double[] cellRadii = null;
            if (tetras == null)
            {
                cellRadii = Enumerable.Range(0, pointsWorld.Length).
                    Select(o => approximateRadius).
                    ToArray();
            }
            else
            {
                Tuple<int, int>[] uniqueLines = Tetrahedron.GetUniqueLines(tetras);

                cellRadii = Enumerable.Range(0, pointsWorld.Length).
                    Select(o => GetCellRadius(o, pointsWorld, uniqueLines, approximateRadius)).
                    ToArray();
            }

            #endregion

            // Turn into sample points
            SamplePoint[] samplePoints = Enumerable.Range(0, numSamples).
                Select(o => new SamplePoint(pointsModel[o].ToPoint(), pointsWorld[o].ToVector(), cellRadii[o], neurons[o])).
                ToArray();

            return Tuple.Create(samplePoints, neurons);
        }

        private static Neuron_SensorPosition[] GetNeuronSet_STRAIGHTLINE(Point3D centerPoint, double size)
        {
            //TODO: If the count gets much over 4, put them in a more compact pattern, some kind of spiral, or the verticies of a polyhedron
            //Math3D.GetRandomVectors_SphericalShell_EvenDist

            Neuron_SensorPosition[] retVal = new Neuron_SensorPosition[5];

            double stepSize = size / retVal.Length;
            double half = size / 2;

            for (int cntr = 0; cntr < retVal.Length; cntr++)
            {
                double offset = -half + (cntr * stepSize);
                Point3D position = new Point3D(centerPoint.X + offset, centerPoint.Y + offset, centerPoint.Z + offset);     // doing the offset on each axis so the total length is sqrt(2) instead of 1

                retVal[cntr] = new Neuron_SensorPosition(position, true);
            }

            return retVal;
        }
        private static Neuron_SensorPosition[] GetNeuronSet(Point3D centerPoint, double size)
        {
            double half = size / 2;

            Vector3D posDir = new Vector3D(half, half, half);
            Vector3D halfPosDir = posDir / 2;       // posDir's length is half of the cube's diagonal, so halfPosDir length is a quarter of the diagonal

            Vector3D orth = Vector3D.CrossProduct(posDir, new Vector3D(-1, 1, -1));
            orth = Vector3D.CrossProduct(posDir, orth);
            orth = orth.ToUnit() * halfPosDir.Length;

            Point3D posMid = centerPoint + halfPosDir + orth;
            Point3D negMid = centerPoint - halfPosDir - (orth * .5);        // using a different distance so it's not symetric

            Neuron_SensorPosition[] retVal = new Neuron_SensorPosition[5];

            retVal[0] = new Neuron_SensorPosition(centerPoint - posDir, true);
            retVal[1] = new Neuron_SensorPosition(negMid, true);
            retVal[2] = new Neuron_SensorPosition(centerPoint, true);
            retVal[3] = new Neuron_SensorPosition(posMid, true);
            retVal[4] = new Neuron_SensorPosition(centerPoint + posDir, true);

            return retVal;
        }

        private static double GetCellRadius(int index, Point3D[] points, Tuple<int, int>[] lines, double defaultRadius)
        {
            Tuple<int, int>[] connectedLines = lines.
                Where(o => o.Item1 == index || o.Item2 == index).
                ToArray();

            if (connectedLines.Length == 0)
            {
                return defaultRadius;
            }

            var stdDev = Math1D.Get_Average_StandardDeviation(connectedLines.Select(o => (points[o.Item2] - points[o.Item1]).Length));

            //NOTE: avg + 1 std dev works well when the points are pretty evenly spaced.  But the points that are close to each other also tend to
            //link to the farthest out points, so subtracting instead

            //return stdDev.Item1 + stdDev.Item2;
            return stdDev.Item1 - (stdDev.Item2 / 2);
        }

        #endregion
    }

    #endregion

    #region class: CameraHardCodedDNA

    public class CameraHardCodedDNA : ShipPartDNA
    {

    }

    #endregion
}
