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
using Game.HelperClassesWPF;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.Arcanorum
{
    #region Class: MotionController_LinearDesign

    public class MotionController_LinearDesign : PartDesignBase
    {
        #region Declaration Section

        public const PartDesignAllowedScale ALLOWEDSCALE = PartDesignAllowedScale.XYZ;		// This is here so the scale can be known through reflection

        private Tuple<UtilityNewt.IObjectMassBreakdown, Vector3D, double> _massBreakdown = null;

        #endregion

        #region Constructor

        public MotionController_LinearDesign(EditorOptions options)
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

        private Model3D _geometry = null;
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
            return SensorVisionDesign.CreateSensorCollisionHull(world, this.Scale, this.Orientation, this.Position);
        }
        public override UtilityNewt.IObjectMassBreakdown GetMassBreakdown(double cellSize)
        {
            return SensorVisionDesign.GetSensorMassBreakdown(ref _massBreakdown, this.Scale, cellSize);
        }

        #endregion

        #region Private Methods

        private Model3D CreateGeometry(bool isFinal)
        {
            #region Material

            DiffuseMaterial diffuse = WorldColorsArco.MotionController_Linear_Diffuse.Value;
            SpecularMaterial specular = WorldColorsArco.MotionController_Linear_Specular.Value;
            if (!isFinal)
            {
                diffuse = diffuse.Clone();      // cloning, because the editor will manipulate the brush, and WorldColors is handing out a shared brush
                specular = specular.Clone();
            }

            MaterialGroup material = new MaterialGroup();
            this.MaterialBrushes.Add(new MaterialColorProps(diffuse, WorldColorsArco.MotionController_Linear_Color));
            material.Children.Add(diffuse);
            this.MaterialBrushes.Add(new MaterialColorProps(specular));
            material.Children.Add(specular);

            if (!isFinal)
            {
                EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
                material.Children.Add(selectionEmissive);
                base.SelectionEmissives.Add(selectionEmissive);
            }

            #endregion

            double radius = ((this.Scale.X * SensorVisionDesign.SIZEPERCENTOFSCALE_XY) + (this.Scale.Y * SensorVisionDesign.SIZEPERCENTOFSCALE_XY)) / 2d;
            double height = this.Scale.Z * SensorVisionDesign.SIZEPERCENTOFSCALE_Z;
            double halfHeight = height / 2d;

            Model3DGroup retVal = new Model3DGroup();

            #region Center

            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = material;
            geometry.BackMaterial = material;

            List<TubeRingBase> rings = new List<TubeRingBase>();

            rings.Add(new TubeRingPoint(0, false));
            rings.Add(new TubeRingRegularPolygon(halfHeight, false, radius * .3, radius * .3, false));
            rings.Add(new TubeRingPoint(halfHeight, false));

            geometry.Geometry = UtilityWPF.GetMultiRingedTube(5, rings, false, true);

            retVal.Children.Add(geometry);

            #endregion

            #region Ring

            geometry = new GeometryModel3D();
            geometry.Material = material;
            geometry.BackMaterial = material;

            int segments = isFinal ? 10 : 35;

            geometry.Geometry = UtilityWPF.GetRing(segments, radius - (height / 2d), radius, height);

            retVal.Children.Add(geometry);

            #endregion

            // Transform
            retVal.Transform = GetTransformForGeometry(isFinal);

            // Exit Function
            return retVal;
        }

        #endregion
    }

    #endregion
    #region Class: MotionController_Linear

    /// <summary>
    /// This creates a 2D disk of neurons.  The neurons correspond to positions in world coords around the bot.
    /// Every tick, based on which neurons are lit brightest, a position is set onto AIMousePlate.
    /// </summary>
    public class MotionController_Linear : PartBase, INeuronContainer, IPartUpdatable
    {
        #region Declaration Section

        public const string PARTTYPE = "MotionController_Linear";

        private readonly ItemOptionsArco _itemOptions;

        private readonly AIMousePlate _mousePlate;

        private readonly Neuron_SensorPosition[] _neurons;

        private readonly double _distanceMult;

        private readonly object _lockContour = new object();

        /// <summary>
        /// These are the same positions as what is stored in each of _terrainTriangles.AllPoints.  Each update, the Z's need to be set to each
        /// corresponding neuron's value
        /// </summary>
        /// <remarks>
        /// The positions from 0 to _neurons.Length correspond to the neurons.  All the others are extra points around the edge of the circle, and
        /// their Z needs to stay zero
        /// </remarks>
        private Point3D[] _terrainPoints = null;
        /// <summary>
        /// This is a terrain of triangles.  The height at each point is the value of the neuron, and allows a contour map to
        /// be calculated
        /// </summary>
        TriangleIndexedLinked[] _terrainTriangles = null;

        #endregion

        #region Constructor

        public MotionController_Linear(EditorOptions options, ItemOptionsArco itemOptions, PartDNA dna, AIMousePlate mousePlate)
            : base(options, dna)
        {
            _itemOptions = itemOptions;
            _mousePlate = mousePlate;

            this.Design = new MotionController_LinearDesign(options);
            this.Design.SetDNA(dna);

            double radius, volume;
            SensorVision.GetMass(out _mass, out volume, out radius, out _scaleActual, dna, itemOptions);

            this.Radius = radius;

            _neurons = SensorVision.CreateNeurons(dna, itemOptions, itemOptions.MotionController_Linear_NeuronDensity, false, false);

            BuildTerrain();

            _distanceMult = _mousePlate.MaxXY / _neurons.Max(o => o.PositionLength);
        }

        #endregion

        #region INeuronContainer Members

        public IEnumerable<INeuron> Neruons_Readonly
        {
            get
            {
                return Enumerable.Empty<INeuron>();
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
                return _neurons;
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
                return NeuronContainerType.Manipulator;
            }
        }

        public double Radius
        {
            get;
            private set;
        }

        //private volatile bool _isOn = false;
        private readonly bool _isOn = true;
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

            //_isOn = true;

            //Update_Simplest();
            //Update_GlobalAverage();

            lock (_lockContour)
            {
                Update_Contour();
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

        #endregion

        #region Private Methods

        // These are just crude test methods
        private void Update_Simplest()
        {
            var top = _neurons.OrderByDescending(o => o.Value).FirstOrDefault();

            if (top != null && !Math3D.IsNearZero(top.Value))
            {
                // Map this to the mouse plate
                Point point = new Point(top.Position.X * _distanceMult, top.Position.Y * _distanceMult);        // no need to look at Z.  The neurons should have been laid out on the XY plane

                _mousePlate.CurrentPoint2D = point;
            }
        }
        private void Update_GlobalAverage()
        {
            double x = 0d;
            double y = 0d;

            foreach (var neuron in _neurons)
            {
                double value = neuron.Value;

                x += neuron.Position.X * value;
                y += neuron.Position.Y * value;
            }

            _mousePlate.CurrentPoint2D = new Point(x, y);
        }

        /// <summary>
        /// This finds the best point by converting neuron positions/values into a hilly terrain, and taking a contour plot.
        /// The middle of the biggest polygon is the chosen point.
        /// </summary>
        private void Update_Contour()
        {
            const double CONTOURHEIGHT = .6667d;

            #region Set heights

            double maxHeight = 0;

            for (int cntr = 0; cntr < _neurons.Length; cntr++)
            {
                _terrainPoints[cntr].Z = _neurons[cntr].Value * 100;
                if (_terrainPoints[cntr].Z > maxHeight)
                {
                    maxHeight = _terrainPoints[cntr].Z;
                }
            }

            for (int cntr = 0; cntr < _terrainTriangles.Length; cntr++)
            {
                _terrainTriangles[cntr].PointsChanged();
            }

            double height = maxHeight * CONTOURHEIGHT;

            #endregion

            Triangle plane = new Triangle(new Point3D(-1, 0, height), new Point3D(1, 0, height), new Point3D(0, 1, height));      // normal needs to point up

            // Get the contour polygons
            var polys = Math3D.GetIntersection_Mesh_Plane(_terrainTriangles, plane);
            if (polys == null || polys.Length == 0)
            {
                // Nothing, don't move
                _mousePlate.CurrentPoint2D = new Point(0, 0);
                return;
            }
            else if (polys.Length == 1)
            {
                // Just one polygon, no need to take the expense of calculating volume
                _mousePlate.CurrentPoint2D = Math3D.GetCenter(polys[0].Polygon3D).ToPoint2D();
                return;
            }

            // Find the polygon with the highest volume
            var topPoly = polys.
                Select(o => new { Poly = o, Volume = o.GetVolumeAbove() }).
                OrderByDescending(o => o.Volume).
                First();

            // Go to the center of this polygon
            _mousePlate.CurrentPoint2D = Math3D.GetCenter(topPoly.Poly.Polygon3D).ToPoint2D();
        }

        private void BuildTerrain()
        {
            // Figure out where the extra ring of points should go
            double radius = BuildTerrainSprtRadius(_neurons.Select(o => o.Position.ToPoint2D()).ToArray());

            // Figure out how many extra points to make
            int numExtra = BuildTerrainSprtNumExtra(_neurons.Length);

            // Lay down all the points into a single array
            _terrainPoints = UtilityCore.Iterate(
                _neurons.Select(o => o.Position),       // first points are the neuron's locations (don't worry about Z right now, they will change each update)
                Math2D.GetCircle_Cached(numExtra).Select(o => new Point3D(o.X * radius, o.Y * radius, 0))       // tack on a bunch of points around the edge
                ).ToArray();

            // Get the delaunay of these points
            TriangleIndexed[] triangles = Math2D.GetDelaunayTriangulation(_terrainPoints.Select(o => o.ToPoint2D()).ToArray(), _terrainPoints);

            // Convert into linked triangles
            List<TriangleIndexedLinked> trianglesLinked = triangles.Select(o => new TriangleIndexedLinked(o.Index0, o.Index1, o.Index2, o.AllPoints)).ToList();
            TriangleIndexedLinked.LinkTriangles_Edges(trianglesLinked, true);

            _terrainTriangles = trianglesLinked.ToArray();
        }
        private static double BuildTerrainSprtRadius(Point[] points)
        {
            // Get the average min distance between the points
            List<Tuple<int, int, double>> distances = new List<Tuple<int, int, double>>();

            for (int outer = 0; outer < points.Length - 1; outer++)
            {
                for (int inner = outer + 1; inner < points.Length; inner++)
                {
                    double distance = (points[outer] - points[inner]).LengthSquared;

                    distances.Add(Tuple.Create(outer, inner, distance));
                }
            }

            double avgDist;
            if (distances.Count == 0)
            {
                avgDist = points[0].ToVector().Length;
                if (Math3D.IsNearZero(avgDist))
                {
                    avgDist = .1;
                }
            }
            else
            {
                avgDist = Enumerable.Range(0, points.Length).
                    Select(o => distances.
                        Where(p => p.Item1 == o || p.Item2 == o).       // get the disances that mention this index
                        Min(p => p.Item3)).     // only keep the smallest of those distances
                    Average();      // get the average of all the mins

                avgDist = Math.Sqrt(avgDist);
            }

            // Get the distance of the farthest out neuron
            double maxDist = points.Max(o => o.ToVector().LengthSquared);
            maxDist = Math.Sqrt(maxDist);

            // Radius of the extra points will be the avg distance beyond that max
            return maxDist + avgDist;
        }
        private static int BuildTerrainSprtNumExtra(int count)
        {
            const int MIN = 6;

            //TODO: .5 is good for small numbers, but over 100, the % of extra should drop off.  By 1000, % should probably be .1
            int retVal = Convert.ToInt32(Math.Ceiling(count * .5));

            if (retVal < MIN)
            {
                return MIN;
            }
            else
            {
                return retVal;
            }
        }

        #endregion
    }

    #endregion

    //TODO: Implement this
    #region Class: MotionController_Circular

    /// <summary>
    /// Instead of moving directly toward the desired point, this moves in circles
    /// </summary>
    /// <remarks>
    /// Linear's neurons are layed out evenly on a plate, and it emulates being moved by a mouse
    /// 
    /// This has a T of neurons:
    /// 
    ///      |
    ///      |
    /// ----------
    /// 
    /// The horizontal line represents the radius of the circle
    ///     Left of center goes counter clockwise
    ///     Right of center goes clockwise
    ///     
    /// The vertical line represents the speed (the speed wouldn't even need to be a line, just a single neuron that goes from 0 to 1)
    /// 
    /// The two lines are read from independently (and wouldn't really even need to intersect like a T.  Maybe just two parallel lines)
    /// </remarks>
    public class MotionController_Circular
    {
    }

    #endregion
}
