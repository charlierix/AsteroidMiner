using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.GameItems.ShipParts;
using Game.HelperClassesWPF;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.Arcanorum.Parts
{
    //TODO: Support filters: type, family bot, player bot, other bot - don't hardcode, use the lineage
    //TODO: Hook to an energy tank
    //TODO: DNA should have a multiplier of SearchRadius

    #region class: SensorVisionDesign

    public class SensorVisionDesign : PartDesignBase
    {
        #region Declaration Section

        internal const double SIZEPERCENTOFSCALE_XY = 1d;
        internal const double SIZEPERCENTOFSCALE_Z = .1d;

        public const PartDesignAllowedScale ALLOWEDSCALE = PartDesignAllowedScale.XYZ;		// This is here so the scale can be known through reflection

        private Tuple<UtilityNewt.IObjectMassBreakdown, Vector3D, double> _massBreakdown = null;

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

        private GeometryModel3D _geometry = null;
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

        #endregion

        #region Public Methods

        public override CollisionHull CreateCollisionHull(WorldBase world)
        {
            return CreateSensorCollisionHull(world, this.Scale, this.Orientation, this.Position);
        }
        public override UtilityNewt.IObjectMassBreakdown GetMassBreakdown(double cellSize)
        {
            return GetSensorMassBreakdown(ref _massBreakdown, this.Scale, cellSize);
        }

        internal static CollisionHull CreateSensorCollisionHull(WorldBase world, Vector3D scale, Quaternion orientation, Point3D position)
        {
            Transform3DGroup transform = new Transform3DGroup();
            //transform.Children.Add(new ScaleTransform3D(scale));		// it ignores scale
            transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(orientation)));
            transform.Children.Add(new TranslateTransform3D(position.ToVector()));

            // Scale X and Y should be identical, but average them to be safe
            double radius = ((SIZEPERCENTOFSCALE_XY * scale.X) + (SIZEPERCENTOFSCALE_XY * scale.Y)) / 2d;

            return CollisionHull.CreateCylinder(world, 0, radius, SIZEPERCENTOFSCALE_Z * scale.Z, transform.Value);
        }
        internal static UtilityNewt.IObjectMassBreakdown GetSensorMassBreakdown(ref Tuple<UtilityNewt.IObjectMassBreakdown, Vector3D, double> existing, Vector3D scale, double cellSize)
        {
            if (existing != null && existing.Item2 == scale && existing.Item3 == cellSize)
            {
                // This has already been built for this size
                return existing.Item1;
            }

            // Convert this.Scale into a size that the mass breakdown will use
            Vector3D size = new Vector3D(scale.X * SIZEPERCENTOFSCALE_XY, scale.Y * SIZEPERCENTOFSCALE_XY, scale.Z * SIZEPERCENTOFSCALE_Z);

            var breakdown = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Cylinder, UtilityNewt.MassDistribution.Uniform, size, cellSize);

            // Store this
            existing = new Tuple<UtilityNewt.IObjectMassBreakdown, Vector3D, double>(breakdown, scale, cellSize);

            // Exit Function
            return existing.Item1;
        }

        public override PartToolItemBase GetToolItem()
        {
            throw new NotImplementedException("SensorVision doesn't have a tool item class");
        }

        #endregion

        #region Private Methods

        private GeometryModel3D CreateGeometry(bool isFinal)
        {
            DiffuseMaterial diffuse = WorldColorsArco.SensorVision_Any_Diffuse.Value;
            SpecularMaterial specular = WorldColorsArco.SensorVision_Any_Specular.Value;
            if (!isFinal)
            {
                diffuse = diffuse.Clone();      // cloning, because the editor will manipulate the brush, and WorldColors is handing out a shared brush
                specular = specular.Clone();
            }

            MaterialGroup material = new MaterialGroup();
            this.MaterialBrushes.Add(new MaterialColorProps(diffuse, WorldColorsArco.SensorVision_Any_Color));
            material.Children.Add(diffuse);
            this.MaterialBrushes.Add(new MaterialColorProps(specular));
            material.Children.Add(specular);

            if (!isFinal)
            {
                EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
                material.Children.Add(selectionEmissive);
                base.SelectionEmissives.Add(selectionEmissive);
            }

            GeometryModel3D retVal = new GeometryModel3D();
            retVal.Material = material;
            retVal.BackMaterial = material;

            int segments = isFinal ? 6 : 35;

            double radius = ((this.Scale.X * SIZEPERCENTOFSCALE_XY) + (this.Scale.Y * SIZEPERCENTOFSCALE_XY)) / 2d;
            double height = this.Scale.Z * SIZEPERCENTOFSCALE_Z;
            RotateTransform3D rotateTransform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90));     // this needs to be along Z instead of X

            retVal.Geometry = UtilityWPF.GetCylinder_AlongX(segments, radius, height, rotateTransform);

            // Transform
            retVal.Transform = GetTransformForGeometry(isFinal);

            // Exit Function
            return retVal;
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

        #region Declaration Section

        public const string PARTTYPE = nameof(SensorVision);

        private readonly ItemOptionsArco _itemOptions;
        private readonly Map _map;

        private readonly Neuron_SensorPosition[] _neurons;
        private readonly double _neuronMaxRadius;
        private readonly double _neuronDistBetween;

        //TODO: Make this an array
        private readonly Type _filterType;

        /// <summary>
        /// This is recalcuated whenever they change the search radius
        /// </summary>
        private volatile DistanceProps _distProps = null;

        #endregion

        #region Constructor

        public SensorVision(EditorOptions options, ItemOptionsArco itemOptions, ShipPartDNA dna, Map map, double searchRadius, Type filterType = null)
            : base(options, dna, itemOptions.VisionSensor_Damage.HitpointMin, itemOptions.VisionSensor_Damage.HitpointSlope, itemOptions.VisionSensor_Damage.Damage)
        {
            _itemOptions = itemOptions;
            _map = map;
            _filterType = filterType;

            this.Design = new SensorVisionDesign(options, true);
            this.Design.SetDNA(dna);

            double radius, volume;
            GetMass(out _mass, out volume, out radius, out _scaleActual, dna, itemOptions);

            this.Radius = radius;

            _neurons = CreateNeurons(dna, itemOptions, itemOptions.VisionSensor_NeuronDensity, true, true);

            #region Store stats about neurons

            if (_neurons.Length == 0)
            {
                throw new ApplicationException("CreateNeurons should have guaranteed at least one neuron");
            }
            else if (_neurons.Length == 1)
            {
                // Since the neuron was forced to not be at the origin, just take the offset from origin (a single neuron vision
                // field is pretty useless anyway)
                _neuronDistBetween = _neurons[0].Position.ToVector().Length;
            }
            else
            {
                // Get the distance between each neuron
                List<Tuple<int, int, double>> distances = new List<Tuple<int, int, double>>();

                for (int outer = 0; outer < _neurons.Length - 1; outer++)
                {
                    for (int inner = outer + 1; inner < _neurons.Length; inner++)
                    {
                        double distance = (_neurons[outer].Position - _neurons[inner].Position).LengthSquared;

                        distances.Add(Tuple.Create(outer, inner, distance));
                    }
                }

                // Get the average of the minimum distance of each index
                _neuronDistBetween = Enumerable.Range(0, _neurons.Length).
                    Select(o => distances.
                        Where(p => p.Item1 == o || p.Item2 == o).       // get the disances that mention this index
                        Min(p => p.Item3)).     // only keep the smallest of those distances
                    Average();      // get the average of all the mins
            }

            // Find the one that's farthest away from the origin (since they form a circle, there will be an outer ring of them that
            // are about the same distance from the center)
            _neuronMaxRadius = _neurons.Max(o => o.PositionLength);

            #endregion

            this.SearchRadius = searchRadius;       // need to set this last, because it populates _distProps
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

        public NeuronContainerType NeuronContainerType
        {
            get
            {
                return NeuronContainerType.Sensor;
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
                foreach (var neuron in _neurons)
                {
                    neuron.Value = 0d;
                }
            }
            else
            {
                var location = base.GetWorldLocation();

                var items = snapshot.GetItems(location.Item1, this.SearchRadius);

                if (_filterType != null)
                {
                    items = MapOctree.FilterType(_filterType, items, true);
                }

                //TODO: There may be other filters, like type of bot { family, player, other }

                UpdateNeurons(_neurons, location, items, _distProps, _botToken);
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

        private double _searchRadius = -1d;
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
                return _searchRadius;
            }
            set
            {
                _searchRadius = value;

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

        #endregion

        #region Internal Methods

        internal static void GetMass(out double mass, out double volume, out double radius, out Vector3D actualScale, ShipPartDNA dna, ItemOptions itemOptions)
        {
            double radiusLocal = ((dna.Scale.X * SensorVisionDesign.SIZEPERCENTOFSCALE_XY) + (dna.Scale.Y * SensorVisionDesign.SIZEPERCENTOFSCALE_XY)) / (2d * 2d);     // scale is diameter, so divide an extra two to get radius
            double heightLocal = dna.Scale.Z * SensorVisionDesign.SIZEPERCENTOFSCALE_Z;
            double halfHeightLocal = heightLocal / 2d;

            volume = Math.PI * radiusLocal * radiusLocal * heightLocal;		// get volume of the cylinder

            // This isn't the radius of the cylinder, it is the radius of the bounding sphere
            radius = Math.Sqrt((radiusLocal * radiusLocal) + (halfHeightLocal * halfHeightLocal));

            mass = volume * itemOptions.Sensor_Density;

            actualScale = new Vector3D(dna.Scale.X * SensorVisionDesign.SIZEPERCENTOFSCALE_XY, dna.Scale.Y * SensorVisionDesign.SIZEPERCENTOFSCALE_XY, dna.Scale.Z * SensorVisionDesign.SIZEPERCENTOFSCALE_Z);
        }

        internal static Neuron_SensorPosition[] CreateNeurons(ShipPartDNA dna, ItemOptions itemOptions, double neuronDensity, bool hasHoleInMiddle, bool ignoreSetValue)
        {
            #region Calculate Counts

            // Figure out how many to make
            //NOTE: This radius isn't taking SCALE into account.  The other neural parts do this as well, so the neural density properties can be more consistent
            double radius = (dna.Scale.X + dna.Scale.Y) / (2d * 2d);		// XY should always be the same anyway (not looking at Z for this.  Z is just to keep the sensors from getting too close to each other)
            double area = Math.Pow(radius, itemOptions.Sensor_NeuronGrowthExponent);

            int neuronCount = Convert.ToInt32(Math.Ceiling(neuronDensity * area));
            if (neuronCount == 0)
            {
                neuronCount = 1;
            }

            #endregion

            Point3D[] staticPositions = null;
            if (hasHoleInMiddle)
            {
                staticPositions = new Point3D[] { new Point3D(0, 0, 0) };
            }

            // Place them evenly within a circle.
            // I don't want a neuron in the center, so placing a static point there to force the neurons away from the center
            Vector3D[] positions = Brain.GetNeuronPositions_Even2D(dna.Neurons, staticPositions, 1d, neuronCount, radius);

            // Exit Function
            return positions.
                Select(o => new Neuron_SensorPosition(o.ToPoint(), true, ignoreSetValue)).
                ToArray();
        }

        #endregion

        #region Private Methods

        private static void UpdateNeurons(Neuron_SensorPosition[] neurons, Tuple<Point3D, Quaternion> location, IEnumerable<MapObjectInfo> items, DistanceProps distProps, long botToken)
        {
            //TODO: Rotate into world


            // Since neuron.Value is a volatile, build up the final values in a local array
            double[] values = new double[neurons.Length];

            Vector3D positionVect = location.Item1.ToVector();

            foreach (MapObjectInfo item in items)
            {
                if (item.Token == botToken)
                {
                    continue;
                }

                for (int cntr = 0; cntr < neurons.Length; cntr++)
                {
                    //TODO: don't just look at item's position.  Should also account for its radius
                    double distance = (distProps.NeuronWorldPositions[cntr] + positionVect - item.Position).Length;

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

        private void RebuildDistanceProps()
        {
            const double B = .75d;      // y=mx+b:  don't want to use a b of 1, because y gets added to a neuron's current output, so multiple objects close together would quickly saturate a neuron
            const double B_at_neighbor = .4d;       // at a distance of nearest neighbor, what the output should be (this needs to be relatively high so that there is some overlap.  Otherwise there are holes where items won't be seen)

            // Rise over run, but negative
            double slope = (B_at_neighbor - B) / _neuronDistBetween;

            // I want a distance of _neuronDistBetween between the outermost neuron and SearchRadius
            double scale = _searchRadius / (_neuronMaxRadius + _neuronDistBetween);

            // Take each existing length times scale to see where they should be.  Don't need to worry about checking for
            // null, there are no neurons at 0,0,0.
            Point3D[] worldScalePositions = _neurons.Select(o => (o.PositionUnit.Value * (o.PositionLength * scale)).ToPoint()).ToArray();

            // Store a new one
            _distProps = new DistanceProps(_searchRadius, _neuronMaxRadius, _neuronDistBetween, slope, B, worldScalePositions);
        }

        #endregion
    }

    #endregion
}
