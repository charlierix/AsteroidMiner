using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.Arcanorum.MapObjects;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.GameItems.ShipParts;
using Game.Newt.v2.NewtonDynamics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media.Media3D;

namespace Game.Newt.v2.Arcanorum.Parts
{
    //TODO: Hook to an energy tank
    //TODO: DNA should have a multiplier of SearchRadius

    #region class: SensorVisionDesign

    public class SensorVisionDesign : PartDesignBase
    {
        #region Declaration Section

        internal const double SIZEPERCENTOFSCALE = .2d;

        public const PartDesignAllowedScale ALLOWEDSCALE = PartDesignAllowedScale.XYZ;		// This is here so the scale can be known through reflection

        private MassBreakdownCache _massBreakdown = null;

        #endregion

        #region Constructor

        public SensorVisionDesign(EditorOptions options, bool isFinalModel)
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

        private Model3DGroup _geometry = null;
        public override Model3D Model
        {
            get
            {
                if (_geometry == null)
                {
                    _geometry = CreateGeometry(this.IsFinalModel);
                }

                return _geometry;
            }
        }

        public double SearchRadius { get; set; }
        //public Type FilterType { get; set; }

        #endregion

        #region Public Methods

        public override ShipPartDNA GetDNA()
        {
            SensorVisionDNA retVal = new SensorVisionDNA();

            base.FillDNA(retVal);
            retVal.SearchRadius = SearchRadius;
            //retVal.FilterType = FilterType;

            return retVal;
        }
        public override void SetDNA(ShipPartDNA dna)
        {
            if (dna is SensorVisionDNA dnaCast)
            {
                base.StoreDNA(dna);

                SearchRadius = dnaCast.SearchRadius;
                //FilterType = dnaCast.FilterType;
            }
            else
            {
                throw new ArgumentException($"The class passed in must be {nameof(SensorVisionDNA)}: {dna.GetType()}");
            }
        }

        public override CollisionHull CreateCollisionHull(WorldBase world)
        {
            return SensorGravityDesign.CreateSensorCollisionHull(world, Scale, Orientation, Position);
        }
        public override UtilityNewt.IObjectMassBreakdown GetMassBreakdown(double cellSize)
        {
            return SensorGravityDesign.GetSensorMassBreakdown(ref _massBreakdown, Scale, cellSize);
        }

        public override PartToolItemBase GetToolItem()
        {
            throw new NotImplementedException("SensorVision doesn't have a tool item class");
        }

        #endregion

        #region Private Methods

        private Model3DGroup CreateGeometry(bool isFinal)
        {
            return SensorGravityDesign.CreateGeometry(this.MaterialBrushes, base.SelectionEmissives,
                GetTransformForGeometry(isFinal),
                WorldColors.SensorBase_Color, WorldColors.SensorBase_Specular, WorldColorsArco.SensorVision_Any_Color, WorldColorsArco.SensorVision_Any_Specular.Value,
                isFinal);
        }

        #endregion
    }

    #endregion
    #region class: SensorVision

    /// <summary>
    /// This creates a 2D disk of neurons (with a hole in the middle).  The neurons correspond to positions in world
    /// coords around the bot.  The neurons light up when an item is near them.
    /// </summary>
    /// <remarks>
    /// There are also options for this see to everything, or only specific types of items (ex: only see treasure)
    /// </remarks>
    public class SensorVision : PartBase, INeuronContainer, IPartUpdatable
    {
        #region class: DistanceProps

        private class DistanceProps
        {
            public DistanceProps(double searchRadius, double maxNeuronRadius, double distanceBetweenNeurons, double slope, double b, Point3D[] neuronWorldPositions)
            {
                this.SearchRadius = searchRadius;
                this.MaxNeuronRadius = maxNeuronRadius;
                this.DistanceBetweenNeurons = distanceBetweenNeurons;
                this.Slope = slope;
                this.B = b;
                this.NeuronWorldPositions = neuronWorldPositions;
            }

            public readonly double SearchRadius;

            public readonly double MaxNeuronRadius;
            public readonly double DistanceBetweenNeurons;

            //TODO: IsBell: true=bell curve, false=linear

            // y=mx+b
            public readonly double Slope;       // slope will always be negative, because it's a dropoff from the neuron position
            public readonly double B;       // the max value for B should be 1, but probably want a bit smaller value so that neurons don't saturate when there are several items close together

            /// <summary>
            /// These are still in model coords, but are scaled to world proportions (as opposed to neuron.position which
            /// is in arbitrary units)
            /// </summary>
            public readonly Point3D[] NeuronWorldPositions;
        }

        #endregion
        #region class: NeuronLayer

        //TODO: filter of bot will have two layers
        public class NeuronLayer
        {
            public SensorVisionFilterType FilterType { get; set; }
            public double Z { get; set; }
            public Neuron_SensorPosition[] Neurons { get; set; }
        }

        #endregion

        #region Declaration Section

        public const string PARTTYPE = nameof(SensorVision);

        private readonly ItemOptionsArco _itemOptions;
        private readonly Map _map;

        // Only one of these will be non null
        private readonly Neuron_SensorPosition[] _neurons;
        private readonly NeuronLayer[] _neuronLayers;
        private readonly Neuron_SensorPosition[] _allNeurons;

        private readonly double _neuronMaxRadius;
        private readonly double _neuronDistBetween;

        /// <summary>
        /// This is recalcuated whenever they change the search radius
        /// </summary>
        private volatile DistanceProps _distProps = null;

        private long _lastSnapshotToken = 0;

        #endregion

        #region Constructor

        public SensorVision(EditorOptions options, ItemOptionsArco itemOptions, SensorVisionDNA dna, Map map)
            : base(options, dna, itemOptions.VisionSensor_Damage.HitpointMin, itemOptions.VisionSensor_Damage.HitpointSlope, itemOptions.VisionSensor_Damage.Damage)
        {
            _itemOptions = itemOptions;
            _map = map;

            Design = new SensorVisionDesign(options, true);
            Design.SetDNA(dna);

            double radius, volume;
            SensorGravity.GetMass(out _mass, out volume, out radius, out _scaleActual, dna, itemOptions);

            Radius = radius;

            if (dna.Filters == null || dna.Filters.Length == 0)
            {
                _neurons = CreateNeurons(dna, itemOptions, itemOptions.VisionSensor_NeuronDensity, true, true);
                _neuronLayers = null;
                _allNeurons = _neurons;

                var distances = GetNeuronAvgDistance(_neurons.Select(o => o.Position.ToPoint2D()).ToArray());
                _neuronDistBetween = distances.distBetween;
                _neuronMaxRadius = distances.maxRadius;
            }
            else
            {
                _neuronLayers = CreateNeurons_Layers(dna, itemOptions);
                _neurons = null;
                _allNeurons = _neuronLayers.
                    SelectMany(o => o.Neurons).
                    ToArray();

                var distances = GetNeuronAvgDistance(_neuronLayers[0].Neurons.Select(o => o.Position.ToPoint2D()).ToArray());     // all the layers have neurons in the same place
                _neuronDistBetween = distances.distBetween;
                _neuronMaxRadius = distances.maxRadius;
            }

            SearchRadius = dna.SearchRadius;       // need to set this last, because it populates _distProps
        }

        #endregion

        #region INeuronContainer Members

        public IEnumerable<INeuron> Neruons_Readonly => _allNeurons;
        public IEnumerable<INeuron> Neruons_ReadWrite => Enumerable.Empty<INeuron>();
        public IEnumerable<INeuron> Neruons_Writeonly => Enumerable.Empty<INeuron>();

        public IEnumerable<INeuron> Neruons_All => _allNeurons;

        public NeuronContainerType NeuronContainerType => NeuronContainerType.Sensor;

        public double Radius { get; private set; }

        private volatile bool _isOn = false;
        public bool IsOn => _isOn;

        #endregion
        #region IPartUpdatable Members

        public void Update_MainThread(double elapsedTime)
        {
        }
        public void Update_AnyThread(double elapsedTime)
        {
            //TODO: Draw from energy (maybe only if non null)
            //if (_energyTanks.RemoveQuantity(elapsedTime * _volume * _itemOptions.VisionSensorAmountToDraw * ItemOptions.ENERGYDRAWMULT, true) > 0d)
            //{
            //    // The energy tank didn't have enough
            //    //NOTE: To be clean, I should set the neuron outputs to zero, but anything pulling from them should be checking this
            //    //anyway.  So save the processor (also, because of threading, setting them to zero isn't as atomic as this property)
            //    _isOn = false;
            //    return;
            //}

            _isOn = true;

            var snapshot = _map.LatestSnapshot;
            if (snapshot == null)
            {
                #region zero everything

                if (_neurons != null)
                {
                    foreach (var neuron in _neurons)
                    {
                        neuron.Value = 0d;
                    }
                }
                else if (_neuronLayers != null)
                {
                    foreach (var neuron in _neuronLayers.SelectMany(o => o.Neurons))
                    {
                        neuron.Value = 0d;
                    }
                }

                #endregion
                return;
            }

            long previous = Interlocked.Exchange(ref _lastSnapshotToken, snapshot.Token);
            if (previous == snapshot.Token)
            {
                // Same as last tick, the neurons are already showing this
                return;
            }

            var location = base.GetWorldLocation();

            var items = snapshot.GetItems(location.position, SearchRadius);

            if (_neurons != null)
            {
                // Add attached weapons
                var personalWeaponItems = items.
                    Select(o => ApplyFilter(o, SensorVisionFilterType.Weapon_Attached_Personal, _botToken, NestToken)).
                    Where(o => o != null).
                    ToArray();

                var attachedWeaponItems = items.
                    Select(o => ApplyFilter(o, SensorVisionFilterType.Weapon_Attached_Other, _botToken, NestToken)).
                    Where(o => o != null).
                    ToArray();

                UpdateNeurons(_neurons, location, items.Concat(personalWeaponItems).Concat(attachedWeaponItems), _distProps, _botToken);
            }
            else if (_neuronLayers != null)
            {
                foreach (var layer in _neuronLayers)
                {
                    var filteredItems = items.
                        Select(o => ApplyFilter(o, layer.FilterType, _botToken, NestToken)).
                        Where(o => o != null);

                    UpdateNeurons(layer.Neurons, location, filteredItems, _distProps, _botToken);
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
        public override double DryMass => _mass;
        public override double TotalMass => _mass;

        private readonly Vector3D _scaleActual;
        public override Vector3D ScaleActual => _scaleActual;

        /// <summary>
        /// How far this part can see
        /// NOTE: The farthest out neuron isn't exactly this radius, but can see out a bit far
        /// </summary>
        /// <remarks>
        /// I decided that search radius shouldn't be based on the part size, but instead on the bot's size (or some other "part" of the
        /// bot other than this part).  This part's size will dictate how many neurons there are/energy draw/mass.
        /// 
        /// But I think it's better if all the vision parts are scaled to the same search radius.  Maybe it doesn't matter if it's a pure neural
        /// net, or mabye different scales would require more layers of neurons to get everything lined up.  Not sure, just a guess.
        /// 
        /// I may want to have scripted AI that overlays the results of several vision parts based on other conditions.  (and even if all
        /// the parts have a different search radius, I would still need this property to know how to transform everything to common
        /// units)
        /// </remarks>
        public double SearchRadius
        {
            get
            {
                return ((SensorVisionDesign)Design).SearchRadius;
            }
            set
            {
                ((SensorVisionDesign)Design).SearchRadius = value;

                RebuildDistanceProps();
            }
        }

        public Point3D[] NeuronWorldPositions
        {
            get
            {
                if (_distProps == null)
                {
                    // This should never happen, _distProps gets set in the constructor, I just don't want a null exception
                    throw new InvalidOperationException("This property can't be used until _distProps is populated");
                }

                return _distProps.NeuronWorldPositions;
            }
        }

        private long _botToken = long.MinValue;
        /// <summary>
        /// This is needed so that this vision class won't see itself
        /// </summary>
        public long BotToken
        {
            get
            {
                return _botToken;
            }
            set
            {
                _botToken = value;
            }
        }

        /// <summary>
        /// If the bot belongs to a nest, this is that token
        /// </summary>
        public long? NestToken { get; set; }

        // This is exposed for debug
        public NeuronLayer[] NeuronLayers => _neuronLayers;

        #endregion

        #region Internal Methods

        internal static Neuron_SensorPosition[] CreateNeurons(ShipPartDNA dna, ItemOptions itemOptions, double neuronDensity, bool hasHoleInMiddle, bool ignoreSetValue)
        {
            var count = GetNeuronCount(dna.Scale, itemOptions.Sensor_NeuronGrowthExponent, neuronDensity);

            Point3D[] staticPositions = null;
            if (hasHoleInMiddle)
            {
                staticPositions = new Point3D[] { new Point3D(0, 0, 0) };
            }

            // Place them evenly within a circle.
            // I don't want a neuron in the center, so placing a static point there to force the neurons away from the center
            Vector3D[] positions = NeuralUtility.GetNeuronPositions_Circular_Even(dna.Neurons, staticPositions, 1d, count.neuronCount, count.radius);

            return positions.
                Select(o => new Neuron_SensorPosition(o.ToPoint(), true, ignoreSetValue)).
                ToArray();
        }
        private static NeuronLayer[] CreateNeurons_Layers(SensorVisionDNA dna, ItemOptionsArco itemOptions)
        {
            if (dna.Filters == null || dna.Filters.Length == 0)
            {
                throw new ApplicationException("This method requires filters to be populated");
            }

            var count = GetNeuronCount(dna.Scale, itemOptions.Sensor_NeuronGrowthExponent, itemOptions.VisionSensor_NeuronDensity);

            //TODO: SensorVisionFilterType.Bot will need two layers
            var neuronZs = GetNeuron_Zs(dna.Filters.Length, count.radius);

            Point3D[] staticPositions = new Point3D[] { new Point3D(0, 0, 0) };

            Point3D[] singlePlate = null;
            if (dna.Neurons != null && dna.Neurons.Length > 0)
            {
                var byLayer = NeuralUtility.DivideNeuronLayersIntoSheets(dna.Neurons, neuronZs.z, count.neuronCount);

                // Just use one of the layer's positions to re evenly distribute (first layer with the correct number of neurons - or closest number).
                // Could try to find the nearest between each layer and use the average of each set, but that's a lot of expense with very little payoff
                singlePlate = GetBestNeuronSheet(byLayer, count.neuronCount);
            }

            // Make sure there is a correct number of neurons and apply an even distribution
            singlePlate = NeuralUtility.GetNeuronPositions_Circular_Even(singlePlate, staticPositions, 1d, count.neuronCount, count.radius).
                Select(o => o.ToPoint()).
                ToArray();

            // Use the location of neurons in this single layer for all layers.  By making sure that each neuron represents the same point in each
            // layer (and the index of that neuron is the same in each layer), position logic in each tick can be optimized
            return Enumerable.Range(0, dna.Filters.Length).
                Select(o => new NeuronLayer()
                {
                    FilterType = dna.Filters[o],
                    Z = neuronZs.z[o],
                    Neurons = singlePlate.
                        Select(p => new Neuron_SensorPosition(new Point3D(p.X, p.Y, neuronZs.z[o]), true, true)).
                        ToArray(),
                }).
                ToArray();
        }

        internal static (MapObjectInfo item, Point3D[] points)[] GetItemPoints(IEnumerable<MapObjectInfo> items, long botToken, long? nestToken)
        {
            var personalWeaponItems = items.
                Select(o => ApplyFilter(o, SensorVisionFilterType.Weapon_Attached_Personal, botToken, nestToken)).
                Where(o => o != null).
                ToArray();

            var attachedWeaponItems = items.
                Select(o => ApplyFilter(o, SensorVisionFilterType.Weapon_Attached_Other, botToken, nestToken)).
                Where(o => o != null).
                ToArray();

            return items.
                Where(o => o.Token != botToken).
                Concat(personalWeaponItems).
                Concat(attachedWeaponItems).
                Select(o =>
                {
                    Point3D[] points;
                    if (o.MapObject is ISensorVisionPoints sp)
                        points = sp.GetSensorVisionPoints();
                    else
                        points = new[] { o.Position };

                    return (o, points);
                }).
                ToArray();
        }

        #endregion

        #region Private Methods

        private static void UpdateNeurons(Neuron_SensorPosition[] neurons, (Point3D position, Quaternion rotation) worldLoc, IEnumerable<MapObjectInfo> items, DistanceProps distProps, long botToken)
        {
            var itemPoints = items.
                Select(o =>
                {
                    Point3D[] points;
                    if (o.MapObject is ISensorVisionPoints sp)
                        points = sp.GetSensorVisionPoints();
                    else
                        points = new[] { o.Position };

                    return new
                    {
                        item = o,
                        points,
                    };
                }).
                ToArray();

            // Since neuron.Value is a volatile, build up the final values in a local array
            double[] values = new double[neurons.Length];

            Vector3D worldLocVect = worldLoc.position.ToVector();

            for (int cntr = 0; cntr < neurons.Length; cntr++)
            {
                // Don't want to rotate, the neurons are already lined up with their real world position.  Just need to translate
                //Point3D worldPos = worldLoc.position + worldLoc.rotation.GetRotatedVector(distProps.NeuronWorldPositions[cntr].ToVector());
                Point3D worldPos = distProps.NeuronWorldPositions[cntr] + worldLocVect;

                //foreach (MapObjectInfo item in items)
                foreach (var item in itemPoints)
                {
                    //if (item.Token == botToken)
                    //{
                    //    continue;
                    //}

                    //TODO: don't just look at item's position.  Should also account for its radius - especially weapons
                    //double distance = (worldPos - item.Position).Length;

                    double distance = item.points.
                        Select(o => (worldPos - o).LengthSquared).
                        OrderBy().
                        First();

                    distance = Math.Sqrt(distance);

                    // y=mx+b
                    double neuronValue = (distProps.Slope * distance) + distProps.B;

                    if (neuronValue > 0)        // if the item is too far from the neuron, the slope will take the value negative, but just ignore those
                    {
                        values[cntr] += neuronValue;
                    }
                }
            }

            // Store the new neuron values
            for (int cntr = 0; cntr < neurons.Length; cntr++)
            {
                if (values[cntr] > 1)
                {
                    neurons[cntr].Value = 1;
                }
                else
                {
                    neurons[cntr].Value = values[cntr];
                }
            }
        }

        private static (double radius, int neuronCount) GetNeuronCount(Vector3D scale, double neuronGrowthExponent, double neuronDensity)
        {
            // Figure out how many to make
            //NOTE: This radius isn't taking SCALE into account.  The other neural parts do this as well, so the neural density properties can be more consistent
            double radius = (scale.X + scale.Y) / (2d * 2d);		// XY should always be the same anyway (not looking at Z for this.  Z is just to keep the sensors from getting too close to each other)
            double area = Math.Pow(radius, neuronGrowthExponent);

            int neuronCount = Convert.ToInt32(Math.Ceiling(neuronDensity * area));
            if (neuronCount == 0)
            {
                neuronCount = 1;
            }

            return (radius, neuronCount);
        }

        private static (double[] z, double gap) GetNeuron_Zs(int numPlates, double radius)
        {
            if (numPlates == 1)
            {
                return (new double[] { 0d }, 0);
            }

            // Don't want the plate's Z to go all the way to the edge of radius, so suck it in a bit
            double max = radius * .75d;

            double gap = (max * 2d) / Convert.ToDouble(numPlates - 1);		// multiplying by 2 because radius is only half

            double[] retVal = new double[numPlates];
            double current = max * -1d;

            for (int cntr = 0; cntr < numPlates; cntr++)
            {
                retVal[cntr] = current;
                current += gap;
            }

            return (retVal, gap);
        }

        private static Point3D[] GetBestNeuronSheet(Point3D[][] sheets, int neuronCount)
        {
            //NOTE: NeuralUtility.DivideNeuronLayersIntoSheets redistributes so that each layer has count.  So while getting first layer with count will work, taking it a step farther by finding the layer with the lowest spread
            //var retVal = sheets.FirstOrDefault(o => o.Length == neuronCount);

            // Look for an exact count match
            var retVal = sheets.
                Where(o => o.Length == neuronCount).
                Select(o => new
                {
                    layer = o,
                    avg_stddev = Math1D.Get_Average_StandardDeviation(o.Select(p => p.Z)),
                }).
                OrderBy(o => o.avg_stddev.Item2).
                Select(o => o.layer).
                FirstOrDefault();

            if (retVal != null)
            {
                return retVal;
            }

            // Look for the smallest layer with more than count
            retVal = sheets.
                Where(o => o.Length > neuronCount).
                Select(o => new
                {
                    layer = o,
                    avg_stddev = Math1D.Get_Average_StandardDeviation(o.Select(p => p.Z)),
                }).
                OrderBy(o => o.layer.Length).
                ThenBy(o => o.avg_stddev.Item2).
                Select(o => o.layer).
                FirstOrDefault();

            if (retVal != null)
            {
                return retVal;
            }

            // Look for the largest layer with less than count
            retVal = sheets.
                Where(o => o.Length < neuronCount).      // this where is unnecessary, just putting it here for completeness (at this point, all layers are less than count
                Select(o => new
                {
                    layer = o,
                    avg_stddev = Math1D.Get_Average_StandardDeviation(o.Select(p => p.Z)),
                }).
                OrderByDescending(o => o.layer.Length).
                ThenBy(o => o.avg_stddev.Item2).
                Select(o => o.layer).
                FirstOrDefault();

            if (retVal != null)
            {
                return retVal;
            }

            throw new ApplicationException("byLayer is empty (this should never happen)");
        }

        private static (double distBetween, double maxRadius) GetNeuronAvgDistance(Point[] neurons)
        {
            double distBetween;

            if (neurons.Length == 0)
            {
                throw new ApplicationException("CreateNeurons should have guaranteed at least one neuron");
            }

            // Find the one that's farthest away from the origin (since they form a circle, there will be an outer ring of them that
            // are about the same distance from the center)
            double maxRadius = Math.Sqrt(neurons.Max(o => o.ToVector().LengthSquared));

            if (neurons.Length == 1)
            {
                // Since the neuron was forced to not be at the origin, just take the offset from origin (a single neuron vision
                // field is pretty useless anyway)
                distBetween = neurons[0].ToVector().Length;
            }
            else
            {
                // Get the distance between each neuron
                var distances = new List<(int index1, int index2, double distance)>();

                for (int outer = 0; outer < neurons.Length - 1; outer++)
                {
                    for (int inner = outer + 1; inner < neurons.Length; inner++)
                    {
                        double distance = (neurons[outer] - neurons[inner]).LengthSquared;

                        distances.Add((outer, inner, distance));
                    }
                }

                // Get the average of the minimum distance of each index
                distBetween = Enumerable.Range(0, neurons.Length).
                    Select(o => distances.
                        Where(p => p.index1 == o || p.index2 == o).       // get the disances that mention this index
                        Min(p => p.distance)).     // only keep the smallest of those distances
                    Select(o => Math.Sqrt(o)).
                    Average();      // get the average of all the mins

                #region TEST

                //var test = Enumerable.Range(0, neurons.Length).
                //    Select(o => distances.
                //        Where(p => p.index1 == o || p.index2 == o).       // get the disances that mention this index
                //        Min(p => p.distance)).     // only keep the smallest of those distances
                //    Select(o => Math.Sqrt(o)).
                //    OrderBy(o => o).
                //    ToArray();

                //var size = Debug3DWindow.GetDrawSizes(maxRadius);

                //Debug3DWindow window = new Debug3DWindow();

                //window.AddAxisLines(maxRadius * 1.1, size.line);

                //window.AddDots(neurons.Select(o => o.Position), size.dot, Colors.White);

                //window.AddLines
                //(
                //    Enumerable.Range(0, neurons.Length).
                //        Select(o => distances.
                //            Where(p => p.index1 == o || p.index2 == o).
                //            OrderBy(p => p.distance).
                //            First()).
                //        Select(o => (neurons[o.index1].Position, neurons[o.index2].Position)),
                //    size.line,
                //    Colors.Gainsboro
                //);

                //window.AddCircle(new Point3D(), distBetween, size.line, Colors.Gray);

                //window.AddText($"max radius: {maxRadius}");
                //window.AddText($"dist between: {distBetween}");

                //window.Show();

                #endregion
            }

            return (distBetween, maxRadius);
        }

        private static MapObjectInfo ApplyFilter(MapObjectInfo item, SensorVisionFilterType filterType, long botToken, long? nestToken)
        {
            switch (filterType)
            {
                case SensorVisionFilterType.Bot_Family:
                    if (item.Token == botToken)
                        return null;
                    else
                    {
                        if (item.MapObject is ArcBotNPC npc && npc.NestToken == nestToken)
                            return item;
                        else
                            return null;
                    }

                case SensorVisionFilterType.Bot_Other:
                    if (item.Token == botToken)
                        return null;
                    else
                    {
                        if (item.MapObject is ArcBot || item.MapObject is ArcBot2)
                        {
                            if (ApplyFilter(item, SensorVisionFilterType.Bot_Family, botToken, nestToken) == null)
                                return item;
                            else
                                return null;        // it's a family bot, which means it's not an other bot
                        }
                        else
                            return null;
                    }

                case SensorVisionFilterType.Nest:
                    if (item.MapObject is NPCNest)
                        return item;
                    else
                        return null;

                case SensorVisionFilterType.TreasureBox:
                    if (item.MapObject is TreasureBox)
                        return item;
                    else
                        return null;

                case SensorVisionFilterType.Weapon_Attached_Personal:
                case SensorVisionFilterType.Weapon_Attached_Other:
                    Weapon weapon = null;
                    if (item.MapObject is ArcBot bot)
                    {
                        weapon = bot.Weapon;
                    }
                    else if (item.MapObject is ArcBot2 bot2)
                    {
                        weapon = bot2.Weapon;
                    }

                    if (weapon == null)
                    {
                        return null;
                    }
                    else
                    {
                        if ((filterType == SensorVisionFilterType.Weapon_Attached_Personal && item.Token != botToken) || (filterType == SensorVisionFilterType.Weapon_Attached_Other && item.Token == botToken))
                            return null;
                        else
                            return new MapObjectInfo(weapon, weapon.GetType());
                    }

                case SensorVisionFilterType.Weapon_FreeFloating:
                    if (item.MapObject is Weapon)
                        return item;
                    else
                        return null;

                default:
                    throw new ApplicationException($"Unknown {nameof(SensorVisionFilterType)}: {filterType}");
            }
        }

        private void RebuildDistanceProps()
        {
            const double B = .9; //.75d;      // y=mx+b:  don't want to use a b of 1, because y gets added to a neuron's current output, so multiple objects close together would quickly saturate a neuron
            const double B_at_neighbor = .1; //.4d;       // at a distance of nearest neighbor, what the output should be (this needs to be relatively high so that there is some overlap.  Otherwise there are holes where items won't be seen)

            double scale = SearchRadius / (_neuronMaxRadius + _neuronDistBetween);

            // Rise over run, but negative
            double slope = (B_at_neighbor - B) / (_neuronDistBetween * scale);

            // Take each existing length times scale to see where they should be.  Don't need to worry about checking for
            // null, there are no neurons at 0,0,0.
            Neuron_SensorPosition[] neurons = _neurons != null ? _neurons : _neuronLayers[0].Neurons;

            Point3D[] worldScalePositions = neurons.
                Select(o =>
                {
                    Vector3D pos2D = o.Position.ToVector2D().ToVector3D();      // need to set the neuron's Z to zero, or the real world position will be way off

                    return (pos2D.ToUnit() * (pos2D.Length * scale)).ToPoint();
                }).
                ToArray();

            // Store a new one
            _distProps = new DistanceProps(SearchRadius, _neuronMaxRadius, _neuronDistBetween, slope, B, worldScalePositions);
        }

        #endregion
    }

    #endregion

    #region enum: SensorVisionFilterType

    public enum SensorVisionFilterType
    {
        Bot_Family,
        Bot_Other,

        TreasureBox,
        Nest,

        Weapon_FreeFloating,
        Weapon_Attached_Personal,
        Weapon_Attached_Other,

        // These might not belong here.  All other enum values are types (nouns), these are suggested actions (verbs)
        // Would need some way of keeping track whether a particular bot or family of bots has been friendly or hostile
        //Attack,
        //Avoid,
        //Protect,
        //PickUp,
    }

    #endregion

    #region class: SensorVisionDNA

    public class SensorVisionDNA : ShipPartDNA
    {
        public double SearchRadius { get; set; }

        //public Type FilterType { get; set; }

        /// <summary>
        /// There will be one layer of neurons for each filter.  If this is null, there will be one layer that fires for everything
        /// </summary>
        public SensorVisionFilterType[] Filters { get; set; }
    }

    #endregion
}
