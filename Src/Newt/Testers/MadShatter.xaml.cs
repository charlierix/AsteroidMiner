using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Primitives3D;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.MapParts;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.Testers
{
    public partial class MadShatter : Window
    {
        #region Class: ItemColors

        private class ItemColors
        {
            public Color PrimaryLine_Bright = UtilityWPF.ColorFromHex("DEDACE");
            public Color PrimaryLine_Dark = UtilityWPF.ColorFromHex("191C1F");
            public Color PrimaryLine_Med = UtilityWPF.ColorFromHex("7D7150");
            public Color NormalLine = UtilityWPF.ColorFromHex("BDAF8A");

            public Color HullFaceTransparent = UtilityWPF.ColorFromHex("2A4F5959");
            public Color HullFace = UtilityWPF.ColorFromHex("D94F5959");

            private Color _hullSpecularBase = UtilityWPF.ColorFromHex("B8C3C4A5");

            private SpecularMaterial _hullFaceSpecular = null;
            public SpecularMaterial HullFaceSpecular
            {
                get
                {
                    if (_hullFaceSpecular == null)
                    {
                        _hullFaceSpecular = new SpecularMaterial(new SolidColorBrush(_hullSpecularBase), 10d);
                    }

                    return _hullFaceSpecular;
                }
            }
            private SpecularMaterial _hullFaceSpecularSoft = null;
            public SpecularMaterial HullFaceSpecularSoft
            {
                get
                {
                    if (_hullFaceSpecularSoft == null)
                    {
                        _hullFaceSpecularSoft = new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(_hullSpecularBase, Colors.Transparent, .6d)), 5d);
                    }

                    return _hullFaceSpecularSoft;
                }
            }

            private SpecularMaterial _hullFaceSpecularTransparent = null;
            public SpecularMaterial HullFaceSpecularTransparent
            {
                get
                {
                    if (_hullFaceSpecularTransparent == null)
                    {
                        _hullFaceSpecularTransparent = new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(_hullSpecularBase, Colors.Transparent, .4d)), 7d);
                    }

                    return _hullFaceSpecularTransparent;
                }
            }

            public Color Shot = UtilityWPF.ColorFromHex("AD6961");

            public Color ControlPoint = UtilityWPF.ColorFromHex("BFCFD9");

            public Color GraphAxis = UtilityWPF.ColorFromHex("40000000");
        }

        #endregion
        #region Class: ExplosionForceOptions

        private class ExplosionForceOptions
        {
            //TODO: Helper method to calculate detailed props
            //  Something about depth of impact
            //      0 depth will blow out a cylinder
            //      high depth will be a spherical shape from the middle of the asteroid

            public bool ShouldSmoothShards = true;

            public double ShotMultiplier = 1d;

            /// <summary>
            /// When calculating velocities for each shot, this will advance the hit start to:
            ///     hitstart + (hitDir * %)
            /// 
            /// This will cause the shards to fly out more than just forward (because the hit start will be more inside the hull)
            /// 
            /// WARNING: Be careful not to use too large of a percent, or the shards will fly backward
            /// </summary>
            public double? InteriorVelocityCenterPercent = .15;

            /// <summary>
            /// When calculating distance between the point of impact and each body center, that distance can be warped based on
            /// the dot product.  This will cause points in line with the impact to get more of the force
            /// 
            /// If this is populated, then the linear dot product is taken to the power.  Less than one will favor items with....idk, makes
            /// the effect a bit less pronounced.  Powers greater than one amplify the dot product's effect
            /// <summary>
            public double? DistanceDot_Power = .4;

            /// <summary>
            /// Takes calculated velocity + Math3D.GetRandomVector_Spherical(velocity.Length * %))
            /// </summary>
            public double? RandomVelocityPercent = .33;
        }

        #endregion
        #region Class: ExplosionForceWorker

        private static class ExplosionForceWorker
        {
            #region Declaration Section

            private static Lazy<RandomBellArgs> _coneAxisBell = new Lazy<RandomBellArgs>(() => new RandomBellArgs(.64, -7.3, .93, -32.7));

            #endregion

            public static ExplosionForceResponse ShootHull(ITriangleIndexed[] convexHull, Tuple<Point3D, Vector3D, double>[] shots, ExplosionForceOptions options = null)
            {
                options = options ?? new ExplosionForceOptions();

                // Create a voronoi, and intersect with the hull
                var retVal = SplitHull(convexHull, shots, options);
                if (retVal == null) return null;
                else if (!retVal.Item2) return retVal.Item1;

                // Figure out velocities
                var aabb = Math3D.GetAABB(convexHull);
                retVal.Item1.Velocities = GetVelocities(retVal.Item1.Shards, retVal.Item1.Hits, Math.Sqrt((aabb.Item2 - aabb.Item1).Length / 2), options);

                return retVal.Item1;
            }

            #region Private Methods - split

            private static Tuple<ExplosionForceResponse, bool> SplitHull(ITriangleIndexed[] convexHull, Tuple<Point3D, Vector3D, double>[] shots, ExplosionForceOptions options)
            {
                #region intersect with the hull

                var hits = shots.
                    Select(o => new ShotHit()
                    {
                        Shot = o,
                        Hit = GetHit(convexHull, o.Item1, o.Item2, o.Item3),
                    }).
                    Where(o => o.Hit != null).
                    ToArray();

                if (hits.Length == 0)
                {
                    return null;
                }

                #endregion

                #region voronoi

                Point3D[] controlPoints = GetVoronoiCtrlPoints(hits, convexHull);

                VoronoiResult3D voronoi = Math3D.GetVoronoi(controlPoints, true);
                if (voronoi == null)
                {
                    return null;
                }

                #endregion

                // There is enough to start populating the response
                ExplosionForceResponse retVal = new ExplosionForceResponse()
                {
                    Hits = hits,
                    ControlPoints = controlPoints,
                    Voronoi = voronoi,
                };

                #region intersect voronoi and hull

                // Intersect
                Tuple<int, ITriangleIndexed[]>[] shards = null;
                try
                {
                    shards = Math3D.GetIntersection_Hull_Voronoi_full(convexHull, voronoi);
                }
                catch (Exception)
                {
                    return Tuple.Create(retVal, false);
                }

                if (shards == null)
                {
                    return Tuple.Create(retVal, false);
                }

                // Smooth
                if (options.ShouldSmoothShards)
                {
                    shards = shards.
                        Select(o => Tuple.Create(o.Item1, Asteroid.SmoothTriangles(o.Item2))).
                        ToArray();
                }

                // Validate
                shards = shards.
                        Where(o => o.Item2 != null && o.Item2.Length >= 3).
                        Where(o =>
                        {
                            Vector3D firstNormal = o.Item2[0].NormalUnit;
                            return o.Item2.Skip(1).Any(p => !Math.Abs(Vector3D.DotProduct(firstNormal, p.NormalUnit)).IsNearValue(1));
                        }).
                        ToArray();

                if (shards.Length == 0)
                {
                    return Tuple.Create(retVal, false);
                }

                #endregion

                #region populate shards

                retVal.Shards = shards.
                    Select(o =>
                        {
                            var aabb = Math3D.GetAABB(o.Item2);

                            double radius = Math.Sqrt((aabb.Item2 - aabb.Item1).Length / 2);

                            Point3D center = Math3D.GetCenter(TriangleIndexed.GetUsedPoints(o.Item2));
                            Vector3D centerVect = center.ToVector();

                            Point3D[] allPoints = o.Item2[0].AllPoints.
                                Select(p => p - centerVect).
                                ToArray();

                            TriangleIndexed[] shiftedTriangles = o.Item2.
                                Select(p => new TriangleIndexed(p.Index0, p.Index1, p.Index2, allPoints)).
                                ToArray();

                            return new ExplosionForceShard()
                           {
                               VoronoiControlPointIndex = o.Item1,
                               Hull_ParentCoords = o.Item2,
                               Hull_Centered = shiftedTriangles,
                               Radius = radius,
                               Center_ParentCoords = center,
                           };
                        }).
                    ToArray();

                #endregion

                return Tuple.Create(retVal, true);
            }

            public static Tuple<Point3D, Point3D> GetHit(ITriangle[] convexHull, Point3D shotStart, Vector3D shotDirection, double percent)
            {
                var intersections = Math3D.GetIntersection_Hull_Ray(convexHull, shotStart, shotDirection);

                if (intersections.Length != 2)
                {
                    return (Tuple<Point3D, Point3D>)null;
                }

                Vector3D penetrate = (intersections[1].Item1 - intersections[0].Item1) * percent;

                return Tuple.Create(intersections[0].Item1, intersections[0].Item1 + penetrate);
            }

            private static void AddToHitline(ref int runningSum, List<Tuple<int, List<Point3D>>> sets, int index, Tuple<Point3D, Point3D> hit, double entryRadius, double exitRadius, double maxAxisLength, Random rand)
            {
                Point3D[] points = GetVoronoiCtrlPoints_AroundLine_Cone(hit.Item1, hit.Item2, rand.Next(2, 5), entryRadius, exitRadius, maxAxisLength);

                runningSum += points.Length;

                var existing = sets.FirstOrDefault(o => o.Item1 == index);
                if (existing == null)
                {
                    existing = Tuple.Create(index, new List<Point3D>());
                    sets.Add(existing);
                }

                existing.Item2.AddRange(points);
            }

            /// <summary>
            /// Each hit line should create a cone of probability to place control points onto
            /// Once a point on the surface of a cone is chosen, choose a couple more on that circle
            /// </summary>
            public static Point3D[] GetVoronoiCtrlPoints_AroundLine_Cone(Point3D lineStart, Point3D lineStop, int count, double entryRadius, double exitRadius, double maxAxisLength)
            {
                // Figure out where on the axis the control point ring should go (the bell has a spike around 30% in)
                double axisPercent = StaticRandomWPF.NextBell(_coneAxisBell.Value);

                Vector3D axis = lineStop - lineStart;
                double axisLength = axis.Length;

                Point3D ringCenter = lineStart + (axis * axisPercent);

                // Figure out the radius of the cone at this point
                double exitRadiusAdjusted = (axisLength / maxAxisLength) * exitRadius;

                double ringRadius = UtilityCore.GetScaledValue(entryRadius, exitRadiusAdjusted, 0, 1, axisPercent);

                // Get some points
                var points = Enumerable.Range(0, count).
                    Select(o => Math3D.GetRandomVector_Circular_Shell(ringRadius).ToPoint());





                //TODO: Figure out the minimum to shift by
                //double shiftRadius = Math.Max(.01, ringRadius / 20);
                double shiftRadius = ringRadius / 20;
                points = points.Select(o => o + Math3D.GetRandomVector_Spherical(shiftRadius));     // the voronoi can't handle coplanar input







                // Rotate/Translate
                Quaternion roation = Math3D.GetRotation(new Vector3D(0, 0, 1), axis);

                Transform3DGroup transform = new Transform3DGroup();
                transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(roation)));
                transform.Children.Add(new TranslateTransform3D(ringCenter.X, ringCenter.Y, ringCenter.Z));

                points = points.Select(o => transform.Transform(o));

                return points.ToArray();
            }

            public static Point3D[] GetVoronoiCtrlPoints(ShotHit[] hits, ITriangle[] convexHull, int maxCount = 33)
            {
                const int MIN = 5;

                Random rand = StaticRandom.GetRandomForThread();

                #region examine hits

                Tuple<int, double>[] hitsByLength = hits.
                    Select((o, i) => Tuple.Create(i, (o.Hit.Item2 - o.Hit.Item1).Length)).
                    OrderByDescending(o => o.Item2).
                    ToArray();

                double totalLength = hitsByLength.Sum(o => o.Item2);

                Tuple<int, double>[] hitsByPercentLength = hitsByLength.
                    Select(o => Tuple.Create(o.Item1, o.Item2 / totalLength)).
                    ToArray();

                #endregion

                #region define control point cones

                var aabb = Math3D.GetAABB(convexHull);
                double aabbLen = (aabb.Item2 - aabb.Item1).Length;

                double entryRadius = aabbLen * .05;
                double exitRadius = aabbLen * .35;
                double maxAxisLength = aabbLen * .75;

                int count = hits.Length * 2;
                count += (totalLength / (aabbLen * .1)).ToInt_Round();

                if (count > maxCount)
                {
                    count = maxCount;
                }

                #endregion

                #region randomly pick control points

                // Keep adding rings around shots until the count is exceeded

                var sets = new List<Tuple<int, List<Point3D>>>();
                int runningSum = 0;

                // Make sure each hit line gets some points
                for (int cntr = 0; cntr < hits.Length; cntr++)
                {
                    AddToHitline(ref runningSum, sets, cntr, hits[cntr].Hit, entryRadius, exitRadius, maxAxisLength, rand);
                }

                // Now that all hit lines have points, randomly choose lines until count is exceeded
                while (runningSum < count)
                {
                    var pointsPerLength = hitsByLength.
                        Select(o =>
                        {
                            var pointsForIndex = sets.FirstOrDefault(p => p.Item1 == o.Item1);
                            int countForIndex = pointsForIndex == null ? 0 : pointsForIndex.Item2.Count;
                            return Tuple.Create(o.Item1, countForIndex / o.Item2);
                        }).
                        OrderBy(o => o.Item2).
                        ToArray();

                    double sumRatio = pointsPerLength.Sum(o => o.Item2);

                    var pointsPerLengthNormalized = pointsPerLength.
                        Select(o => Tuple.Create(o.Item1, o.Item2 / sumRatio)).
                        ToArray();

                    int index = UtilityCore.GetIndexIntoList(rand.NextPow(3), pointsPerLengthNormalized);

                    AddToHitline(ref runningSum, sets, index, hits[index].Hit, entryRadius, exitRadius, maxAxisLength, rand);
                }

                #endregion

                #region remove excessive points

                while (runningSum > count)
                {
                    Tuple<int, double>[] fractions = sets.
                        Select((o, i) => Tuple.Create(i, o.Item2.Count.ToDouble() / runningSum.ToDouble())).
                        OrderByDescending(o => o.Item2).
                        ToArray();

                    int fractionIndex = UtilityCore.GetIndexIntoList(rand.NextPow(1.5), fractions);     //nextPow will favor the front of the list, which is where the rings with the most points are
                    int setIndex = fractions[fractionIndex].Item1;

                    sets[setIndex].Item2.RemoveAt(UtilityCore.GetIndexIntoList(rand.NextDouble(), sets[setIndex].Item2.Count));

                    runningSum--;
                }

                #endregion

                #region ensure enough for voronoi algorithm

                List<Point3D> retVal = new List<Point3D>();
                retVal.AddRange(sets.SelectMany(o => o.Item2));

                // The voronoi algrorithm fails if there aren't at least 5 points (really should fix that).  So add points that are
                // way away from the hull.  This will make the voronoi algorithm happy, and won't affect the local results
                if (count < MIN)
                {
                    retVal.AddRange(
                        Enumerable.Range(0, MIN - count).
                        Select(o => Math3D.GetRandomVector_Spherical_Shell(aabbLen * 20).ToPoint())
                        );
                }

                #endregion

                return retVal.ToArray();
            }

            #endregion
            #region Private Methods - velocities

            private static Vector3D[] GetVelocities(ExplosionForceShard[] hullShards, ShotHit[] hits, double hullRadius, ExplosionForceOptions options)
            {
                //TODO: When the asteroid shards aren't resmoothed, the velocities are very small
                //
                //Maybe add more orth velocity
                //
                //Maybe pull the hit source slightly into the asteroid --- done
                //
                //Maybe add some random velocity (size proportional to the velocity) --- done

                Vector3D[] retVal = CalculateVelocites(hullShards.Select(o => o.Center_ParentCoords).ToArray(), hits, hullRadius, options);

                if (options.RandomVelocityPercent != null)
                {
                    retVal = retVal.
                        Select(o => o + Math3D.GetRandomVector_Spherical(o.Length * options.RandomVelocityPercent.Value)).
                        ToArray();
                }

                return retVal;
            }

            private static Vector3D[] CalculateVelocites(Point3D[] centers, ShotHit[] hits, double hullRadius, ExplosionForceOptions options)
            {
                Vector3D[] retVal = new Vector3D[centers.Length];

                foreach (var hit in hits)
                {
                    Point3D shotStart = hit.Hit.Item1;
                    if (options.InteriorVelocityCenterPercent != null)
                    {
                        shotStart += ((hit.Hit.Item2 - hit.Hit.Item1) * options.InteriorVelocityCenterPercent.Value);
                    }

                    Vector3D[] velocities = DistributeForces(centers, shotStart, hit.Shot.Item2 * (hit.Shot.Item3 * options.ShotMultiplier), hullRadius, options);

                    for (int cntr = 0; cntr < retVal.Length; cntr++)
                    {
                        retVal[cntr] += velocities[cntr];
                    }
                }

                return retVal;
            }

            /// <summary>
            /// This will figure how much velocity to apply to each bodyCenter
            /// TODO: Also calculate angular velocities
            /// </summary>
            /// <param name="bodyCenters">The bodies to apply velocity to</param>
            /// <param name="hitStart">The point of impact</param>
            /// <param name="hitDirection">The direction and force of the impact</param>
            /// <param name="mainBodyRadius">The avg radius of the body that is getting blown apart</param>
            private static Vector3D[] DistributeForces(Point3D[] bodyCenters, Point3D hitStart, Vector3D hitDirection, double mainBodyRadius, ExplosionForceOptions options)
            {
                Vector3D hitDirectionUnit = hitDirection.ToUnit();
                double hitForceBase = hitDirection.Length / mainBodyRadius;

                var vectors = bodyCenters.
                    Select(o =>
                    {
                        Vector3D direction = o - hitStart;
                        Vector3D directionUnit = direction.ToUnit();

                        double distance = direction.Length;
                        double scaledDistance = distance;
                        if (options.DistanceDot_Power != null)
                        {
                            double linearDot = Math3D.GetLinearDotProduct(hitDirectionUnit, directionUnit);     // making it linear so the power function is more predictable

                            // Exaggerate the distance based on dot product.  That way, points in line with the hit will get more of the impact
                            double scale = (1 - Math.Abs(linearDot));
                            scale = Math.Pow(scale, options.DistanceDot_Power.Value);
                            scaledDistance = scale * distance;
                        }

                        return new
                        {
                            ForcePoint = o,
                            Distance = distance,
                            ScaledDistance = scaledDistance,
                            DirectionUnit = directionUnit,
                        };
                    }).
                    ToArray();

                double[] percents = GetPercentOfProjForce(vectors.Select(o => o.ScaledDistance).ToArray());

                Vector3D[] retVal = new Vector3D[vectors.Length];

                for (int cntr = 0; cntr < vectors.Length; cntr++)
                {
                    retVal[cntr] = vectors[cntr].DirectionUnit * (hitForceBase * percents[cntr]);
                }

                return retVal;
            }

            /// <summary>
            /// Figures out how much of the force should go to each point.  The distances are distances from the point of impact.
            /// NOTE: This is basically: (1+baseConst)^-x
            /// </summary>
            /// <param name="baseConst">
            /// Any number from 0 to infinity.  Realistic values should be between .5 and 8.  There's nothing magical about using e as
            /// the constant, it just seems to have a good amount of curve
            /// </param>
            /// <remarks>
            /// This doesn't need to create super realistic percents.  If in doubt, use a larger base constant, which will give more of the force
            /// to the closer points.  The physics engine won't let close objects fly past farther, so just like racking pool balls, the outer will
            /// fly out.  But distributing the force a bit should help with jumpy behavior if the physics engine is under load
            /// 
            /// I don't know for sure, but I'm guessing the shape of the curve should be roughly 1/x^2.  This method isn't that, but should
            /// be similar enough.
            /// </remarks>
            public static double[] GetPercentOfProjForce(double[] distances, double baseConst = Math.E)
            {
                var avg_stdev = Math1D.Get_Average_StandardDeviation(distances);
                double scale = avg_stdev.Item2 / avg_stdev.Item1;
                scale *= baseConst;
                scale += 1;

                var deviations = distances.
                    Select(o =>
                    {
                        double distFromAvg = Math.Abs(o - avg_stdev.Item1);
                        double deviationsFromAvg = distFromAvg / avg_stdev.Item2;

                        if (o > avg_stdev.Item1)
                        {
                            deviationsFromAvg = -deviationsFromAvg;
                        }

                        //return Math.Pow(Math.E, deviationsFromAvg);
                        return Math.Pow(scale, deviationsFromAvg);
                    }).
                    ToArray();

                double sum = deviations.Sum();

                return deviations.
                    Select(o => o / sum).
                    ToArray();
            }

            #endregion
        }

        #endregion
        #region Class: ExplosionForceResponse

        private class ExplosionForceResponse
        {
            public ShotHit[] Hits { get; set; }
            public Point3D[] ControlPoints { get; set; }
            public VoronoiResult3D Voronoi { get; set; }

            //NOTE: If there is an error, everything below will be null
            //public Tuple<int, ITriangleIndexed[]>[] Shards { get; set; }
            public ExplosionForceShard[] Shards { get; set; }

            public Vector3D[] Velocities { get; set; }
        }

        #endregion
        #region Class: ExplosionForceShard

        private class ExplosionForceShard
        {
            public int VoronoiControlPointIndex { get; set; }
            public ITriangleIndexed[] Hull_ParentCoords { get; set; }
            public Point3D Center_ParentCoords { get; set; }

            // These properties would be used to make an actual IMapObject (Asteroid)
            public ITriangleIndexed[] Hull_Centered { get; set; }
            public double Radius { get; set; }
        }

        #endregion
        #region Class: ShotHit

        private class ShotHit
        {
            public Tuple<Point3D, Vector3D, double> Shot { get; set; }
            public Tuple<Point3D, Point3D> Hit { get; set; }
        }

        #endregion

        #region Declaration Section

        private const double BOUNDRYSIZE = 150;
        private const double BOUNDRYSIZEHALF = BOUNDRYSIZE * .5d;

        private const double MAXRADIUS = 10d;
        private const double DOTRADIUS = .05d;
        private const double LINETHICKNESS_SCREENSPACE = 1.2d;
        private const double LINETHICKNESS_BILLBOARD = .07d;

        private readonly ItemColors _colors = new ItemColors();

        private Point3D _boundryMin;
        private Point3D _boundryMax;

        private EditorOptions _editorOptions = new EditorOptions();
        private ItemOptions _itemOptions = null;
        private SharedVisuals _sharedVisuals = new SharedVisuals();

        private World _world = null;
        private Map _map = null;

        //private UpdateManager _updateManager = null;

        private MaterialManager _materialManager = null;
        private int _material_Asteroid = -1;

        /// <summary>
        /// This listens to the mouse/keyboard and controls the camera
        /// </summary>
        private TrackBallRoam _trackball = null;

        private List<Visual3D> _visuals = new List<Visual3D>();

        private readonly Effect _errorEffect;

        private static Lazy<FontFamily> _font = new Lazy<FontFamily>(() => UtilityWPF.GetFont(UtilityCore.RandomOrder(new[] { "Californian FB", "Garamond", "MingLiU", "MS PMincho", "Palatino Linotype", "Plantagenet Cherokee", "Raavi", "Shonar Bangla", "Calibri" })));

        private ITriangleIndexed[] _asteroid = null;
        private Tuple<Point3D, Vector3D, double>[] _shots = null;
        private HullVoronoiExploder_Response _explosion = null;

        //TODO: Remove these
        private VoronoiResult3D _voronoi = null;
        private Asteroid[] _asteroidShards = null;

        private Point3D[] _projForcePoints = null;

        private bool _initialized = false;

        #endregion

        #region Constructor

        public MadShatter()
        {
            InitializeComponent();

            // Camera Trackball
            _trackball = new TrackBallRoam(_camera);
            _trackball.EventSource = grdViewPort;		//NOTE:  If this control doesn't have a background color set, the trackball won't see events (I think transparent is ok, just not null)
            _trackball.AllowZoomOnMouseWheel = true;
            _trackball.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.MouseComplete_NoLeft));
            //_trackball.GetOrbitRadius += new GetOrbitRadiusHandler(Trackball_GetOrbitRadius);

            _errorEffect = new DropShadowEffect()
            {
                Color = UtilityWPF.ColorFromHex("FF0000"),
                BlurRadius = 4,
                Direction = 0,
                Opacity = .5,
                ShadowDepth = 0,
            };

            _initialized = true;
        }

        #endregion

        #region Event Listeners

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _itemOptions = new ItemOptions();

                #region Init World

                _boundryMin = new Point3D(-BOUNDRYSIZEHALF, -BOUNDRYSIZEHALF, -BOUNDRYSIZEHALF);
                _boundryMax = new Point3D(BOUNDRYSIZEHALF, BOUNDRYSIZEHALF, BOUNDRYSIZEHALF);

                _world = new World();
                //_world.Updating += new EventHandler<WorldUpdatingArgs>(World_Updating);

                List<Point3D[]> innerLines, outerLines;
                _world.SetCollisionBoundry(out innerLines, out outerLines, _boundryMin, _boundryMax);

                #endregion
                #region Materials

                _materialManager = new MaterialManager(_world);

                // Asteroid
                Game.Newt.v2.NewtonDynamics.Material material = new Game.Newt.v2.NewtonDynamics.Material();
                material.Elasticity = .1d;
                _material_Asteroid = _materialManager.AddMaterial(material);

                // Collisions
                //_materialManager.RegisterCollisionEvent(_material_Asteroid, _material_Asteroid, Collision_AsteroidAsteroid);

                #endregion
                #region Map

                _map = new Map(_viewport, null, _world);
                _map.SnapshotFequency_Milliseconds = 250;// 125;
                _map.SnapshotMaxItemsPerNode = 10;
                _map.ShouldBuildSnapshots = false;
                _map.ShouldShowSnapshotLines = false;
                _map.ShouldSnapshotCentersDrift = true;

                // Asteroid doesn't implement up IPartUpdatable
                //_updateManager = new UpdateManager(
                //    new Type[] { typeof(Asteroid) },
                //    new Type[] { typeof(Asteroid) },
                //    _map);

                #endregion

                _world.UnPause();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            try
            {
                _world.Pause();

                //_updateManager.Dispose();

                _map.Dispose();		// this will dispose the physics bodies
                _map = null;

                _world.Dispose();
                _world = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void World_Updating(object sender, WorldUpdatingArgs e)
        {
            try
            {
                //_updateManager.Update_MainThread(e.ElapsedTime);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void grdViewPort_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (_asteroid == null)
                {
                    return;
                }
                else if (e.ChangedButton != MouseButton.Left)
                {
                    return;
                }

                // Fire a ray from the mouse point
                Point clickPoint = e.GetPosition(grdViewPort);

                var ray = UtilityWPF.RayFromViewportPoint(_camera, _viewport, clickPoint);

                // Don't worry about the asteroid right now, just get the closest point to the origin
                Point3D originIntersect = Math3D.GetClosestPoint_Line_Point(ray.Origin, ray.Direction, new Point3D(0, 0, 0));

                // Store the shot
                Vector3D directionHalf = ray.Direction.ToUnit() * (MAXRADIUS * 3);
                var shot = Tuple.Create(originIntersect - directionHalf, directionHalf * 2, trkMouseShotPower.Value);

                _shots = UtilityCore.ArrayAdd(_shots, shot);

                // Cut up the asteroid, draw
                ProcessShots();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShootNewAsteroid_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearAll();

                _asteroid = Asteroid.GetHullTriangles(MAXRADIUS);

                DrawAsteroid(_asteroid);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShootOneShot_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_asteroid == null)
                {
                    MessageBox.Show("Need to make an asteroid first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ClearShots();

                // Work backward to make sure the shot doesn't miss
                Point3D pointInside = Math3D.GetRandomVector_Spherical(MAXRADIUS / 3).ToPoint();
                Vector3D directionOut = Math3D.GetRandomVector_Spherical_Shell(MAXRADIUS * 3);

                _shots = new[]
                {
                    Tuple.Create(pointInside + directionOut, -directionOut, 1d),
                };

                // Find the deepest point of intersection
                var hits = Math3D.GetIntersection_Hull_Ray(_asteroid, _shots[0].Item1, _shots[0].Item2);
                if (hits.Length != 2)
                {
                    MessageBox.Show("Expected two hits: " + hits.Length.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                //TODO: Put a point in front, then remove that subhull.  This will look like exploded material
                //or, put it on the same plane, and a cylinder will get cored out
                //Point3D[] controlPoints = GetVoronoiCtrlPoints_AroundLine_Simple(hits[0].Item1, hits[1].Item1, StaticRandom.Next(2, 7));
                Point3D[] controlPoints = GetVoronoiCtrlPoints_AroundLine(hits[0].Item1, hits[1].Item1);

                _voronoi = Math3D.GetVoronoi(controlPoints, true);

                DrawShatteredAsteroid_ORIG();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ShootTwoShots_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_asteroid == null)
                {
                    MessageBox.Show("Need to make an asteroid first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ClearShots();

                // Work backward to make sure the shots don't miss
                Point3D pointInside1 = Math3D.GetRandomVector_Spherical(MAXRADIUS / 3).ToPoint();
                Point3D pointInside2 = Math3D.GetRandomVector_Spherical(MAXRADIUS / 3).ToPoint();

                Vector3D directionOut1 = Math3D.GetRandomVector_Spherical_Shell(MAXRADIUS * 3);
                Vector3D directionOut2 = Math3D.RotateAroundAxis(directionOut1, Math3D.GetRandomVector_Spherical_Shell(1), Math1D.DegreesToRadians(Math1D.GetNearZeroValue(30)));

                _shots = new[]
                {
                    Tuple.Create(pointInside1 + directionOut1, -directionOut1, 1d),
                    Tuple.Create(pointInside2 + directionOut2, -directionOut2, 1d),
                };

                // Find the deepest point of intersection
                var hits = _shots.
                    Select(o => Math3D.GetIntersection_Hull_Ray(_asteroid, o.Item1, o.Item2)).
                    ToArray();

                if (hits.Length != 2 || hits.Any(o => o.Length != 2))
                {
                    MessageBox.Show("Expected two hits: " + hits.Length.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Voronoi
                Point3D[] controlPoints = GetVoronoiCtrlPoints_TwoLines(hits, _asteroid);

                _voronoi = Math3D.GetVoronoi(controlPoints, true);

                DrawShatteredAsteroid_ORIG();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //TODO: If they left click on the asteroid, fire a shot
        private void ClearShots_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearShots();

                if (_asteroid != null)
                {
                    DrawAsteroid(_asteroid);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ShootSmallShot_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_asteroid == null)
                {
                    MessageBox.Show("Need to make an asteroid first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                FireShot(StaticRandom.NextPercent(.15, .5));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ShootMedShot_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_asteroid == null)
                {
                    MessageBox.Show("Need to make an asteroid first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                FireShot(StaticRandom.NextPercent(.33, .5));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ShootLargeShot_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_asteroid == null)
                {
                    MessageBox.Show("Need to make an asteroid first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                FireShot(StaticRandom.NextPercent(.67, .5));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void RecalcShots_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ProcessShots();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void trkProjForce_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                RedrawProjForce();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ProjForceRandPoints_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _projForcePoints = Enumerable.Range(0, StaticRandom.Next(1, 10)).
                    Select(o => Math3D.GetRandomVector_Spherical(MAXRADIUS * .6).ToPoint()).
                    ToArray();

                RedrawProjForce();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ProjForceRedraw_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                RedrawProjForce();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void txtProjForceDistDotPow_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                RedrawProjForce();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ProjForceGraph_Click(object sender, RoutedEventArgs e)
        {
            const double MIN = .01;
            const double MAX = MAXRADIUS;

            try
            {
                #region parse gui

                int count;
                if (!int.TryParse(txtProjForceGraphCount.Text, out count))
                {
                    MessageBox.Show("Couldn't parse count", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                double baseConst;
                if (!double.TryParse(txtProjForceGraphBaseConst.Text, out baseConst))
                {
                    MessageBox.Show("Couldn't parse base constant", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                #endregion

                Random rand = StaticRandom.GetRandomForThread();

                #region get random distances

                double[] distances = null;
                if (radProjForceGraphSimple.IsChecked.Value)
                {
                    // Choose random distances from min to max
                    distances = Enumerable.Range(0, count).
                        Select(o => rand.NextDouble(MIN, MAX)).
                        ToArray();
                }
                else if (radProjForceGraphTight.IsChecked.Value || radProjForceGraph2Tight.IsChecked.Value)
                {
                    // Figure out how many to put in each grouping
                    int[] counts = null;
                    if (radProjForceGraphTight.IsChecked.Value)
                    {
                        counts = new[] { count };
                    }
                    else
                    {
                        if (count < 2)
                        {
                            MessageBox.Show("Count needs to be 2 or more", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        counts = new int[2];
                        counts[0] = 1 + rand.Next(count - 1);
                        counts[1] = count - counts[0];
                    }

                    List<double> distanceBuilder = new List<double>();

                    foreach (int setCount in counts)
                    {
                        // Pick a random distance, then cluster the samples around that
                        double rangeHalf = rand.NextDouble(MIN, MAX / 4);
                        double dist = rand.NextDouble(MIN + rangeHalf, MAX - rangeHalf);

                        distanceBuilder.AddRange(Enumerable.Range(0, setCount).
                            Select(o => rand.NextDouble(dist - rangeHalf, dist + rangeHalf)));
                    }

                    distances = distanceBuilder.ToArray();
                }
                else
                {
                    throw new ApplicationException("Unknown radio button");
                }

                #endregion

                // Calculate
                double[] percents = ExplosionForceWorker.GetPercentOfProjForce(distances, baseConst);

                #region generate plot points

                Point3D[] percentPointIdeal = GetPercentOfProjForce_Equation(distances, baseConst);

                Point3D[] drawPoints_Actual = Enumerable.Range(0, count).
                    SelectMany(o => new[] { new Point3D(distances[o], 0, 0), new Point3D(distances[o], percents[o] * MAXRADIUS, 0) }).
                    ToArray();

                double maxPercent = percents.Max();
                Point3D[] drawPoints_Exaggerated = Enumerable.Range(0, count).
                    SelectMany(o => new[] { new Point3D(distances[o], 0, 0), new Point3D(distances[o], UtilityCore.GetScaledValue(0, MAXRADIUS, 0, maxPercent, percents[o]), 0) }).
                    ToArray();

                double maxIdeal = percentPointIdeal.Max(o => o.Y);
                Point3D[] drawPoints_Ideal_Exaggerated = percentPointIdeal.
                    Select(o => new Point3D(o.X, UtilityCore.GetScaledValue(0, MAXRADIUS, 0, maxIdeal, o.Y), o.Z)).
                    ToArray();

                var verticals = Enumerable.Range(0, count).
                    Select(o => Tuple.Create(o * 2, (o * 2) + 1));

                #endregion

                #region draw

                ClearAll();

                AddLines_ScreenSpace(new[] { Tuple.Create(0, 1), Tuple.Create(0, 2) }, new[] { new Point3D(0, 0, 0), new Point3D(MAXRADIUS, 0, 0), new Point3D(0, MAXRADIUS, 0) }, LINETHICKNESS_SCREENSPACE / 2, _colors.GraphAxis);
                AddLines_ScreenSpace(drawPoints_Ideal_Exaggerated, LINETHICKNESS_SCREENSPACE * .5, _colors.PrimaryLine_Med);

                AddLines_ScreenSpace(verticals, drawPoints_Exaggerated, LINETHICKNESS_SCREENSPACE * .25, _colors.PrimaryLine_Dark);
                AddLines_ScreenSpace(verticals, drawPoints_Actual, LINETHICKNESS_SCREENSPACE, _colors.PrimaryLine_Bright);

                // Show report
                var avg_stdev = Math1D.Get_Average_StandardDeviation(distances);

                double y1a = MAXRADIUS * -.05;
                double y2a = MAXRADIUS * -.35;
                double y1b = MAXRADIUS * -.15;
                double y2b = MAXRADIUS * -.25;
                AddLines_ScreenSpace(
                    new[] 
                    {
                        Tuple.Create(new Point3D(avg_stdev.Item1, y1a, 0), new Point3D(avg_stdev.Item1, y2a, 0)),
                        Tuple.Create(new Point3D(avg_stdev.Item1 - avg_stdev.Item2, y1b, 0), new Point3D(avg_stdev.Item1 - avg_stdev.Item2, y2b, 0)),
                        Tuple.Create(new Point3D(avg_stdev.Item1 + avg_stdev.Item2, y1b, 0), new Point3D(avg_stdev.Item1 + avg_stdev.Item2, y2b, 0)),
                    },
                    LINETHICKNESS_SCREENSPACE,
                    _colors.NormalLine);

                string report = string.Format("avg={0}\r\nstd dev={1}\r\ndiv={2}", avg_stdev.Item1.ToStringSignificantDigits(2), avg_stdev.Item2.ToStringSignificantDigits(2), (avg_stdev.Item2 / avg_stdev.Item1).ToStringSignificantDigits(2));

                AddText3D(report, new Point3D(avg_stdev.Item1, MAXRADIUS * -.55, 0), .60, Colors.White);

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Voronoi1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearAll();

                Point3D[] points = GetVoronoiCtrlPoints_AroundLine_Simple(new Point3D(0, 0, -MAXRADIUS), new Point3D(0, 0, MAXRADIUS), StaticRandom.Next(2, 7));

                VoronoiResult3D voronoi = Math3D.GetVoronoi(points, true);

                AddDots(points, DOTRADIUS * 2, Colors.White);
                AddDot(new Point3D(0, 0, 0), DOTRADIUS * 4, Colors.Black);

                var lines = Edge3D.GetUniqueLines(voronoi.Edges, MAXRADIUS * 10);

                AddLines_Billboard(lines.Item1, lines.Item2, LINETHICKNESS_BILLBOARD, Colors.Silver);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void LinearDotProduct_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Vector3D vector = new Vector3D(1, 0, 0);
                Vector3D axis = new Vector3D(0, 0, 1);

                var test = Enumerable.Range(0, 360).
                    Select(o =>
                        {
                            double angle = o;
                            Vector3D rotated = vector.GetRotatedVector(axis, angle);
                            double dot = Vector3D.DotProduct(vector, rotated);
                            double linear = Math3D.GetLinearDotProduct(vector, rotated);
                            double distance = Math.Abs(dot - linear);

                            return new
                            {
                                Angle = angle,
                                Dot = dot,
                                Linear = linear,
                                Distance = distance,
                            };
                        }).
                    ToArray();

                Point3D[] allPoints = test.
                    SelectMany(o => new[] { new Point3D(o.Angle, o.Dot * 90, 0), new Point3D(o.Angle, o.Linear * 90, 0) }).
                    ToArray();

                IEnumerable<Tuple<int, int>> indicesGap = test.
                    Select((o, i) => Tuple.Create(i * 2, (i * 2) + 1));

                IEnumerable<Tuple<int, int>> indices1 = Enumerable.Range(0, test.Length - 1).
                    Select(o => Tuple.Create(o * 2, (o + 1) * 2));

                IEnumerable<Tuple<int, int>> indices2 = Enumerable.Range(0, test.Length - 1).
                    Select(o => Tuple.Create((o * 2) + 1, ((o + 1) * 2) + 1));

                double thickness = LINETHICKNESS_SCREENSPACE;

                ClearAll();
                AddLines_ScreenSpace(indicesGap, allPoints, thickness, Colors.White);
                AddLines_ScreenSpace(indices1, allPoints, thickness, Colors.Red);
                AddLines_ScreenSpace(indices2, allPoints, thickness, Colors.Blue);

                _camera.Position = new Point3D(180, 0, -500);
                _camera.LookDirection = new Vector3D(0, 0, 1);
                _camera.UpDirection = new Vector3D(0, 1, 0);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Redraw_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_explosion == null && _voronoi != null)
                {
                    DrawShatteredAsteroid_ORIG();
                }
                else
                {
                    ProcessShots();
                    //DrawShatteredAsteroid();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private void ClearAll()
        {
            _asteroid = null;

            ClearShots();
        }
        private void ClearShots()
        {
            _shots = null;
            _voronoi = null;
            _explosion = null;

            ClearVisuals();
        }
        private void ClearVisuals()
        {
            if (_asteroidShards != null)
            {
                foreach (var asteroid in _asteroidShards)
                {
                    _map.RemoveItem(asteroid);
                    asteroid.PhysicsBody.Dispose();
                }

                _asteroidShards = null;
            }

            _viewport.Children.RemoveAll(_visuals);
            _visuals.Clear();
        }

        private void FireShot(double depthPercent)
        {
            if (_asteroid == null)
            {
                return;
            }

            // Work backward to make sure the shot doesn't miss
            Point3D pointInside = Math3D.GetRandomVector_Spherical(MAXRADIUS / 3).ToPoint();
            Vector3D directionOut = Math3D.GetRandomVector_Spherical_Shell(MAXRADIUS * 3);

            var shot = Tuple.Create(pointInside + directionOut, -directionOut, depthPercent);

            _shots = UtilityCore.ArrayAdd(_shots, shot);

            ProcessShots();
        }
        private void ProcessShots_ORIG()
        {
            // Intersect with the asteroid
            #region ORIG
            //var hits = _shots.
            //    Select(o =>
            //    {
            //        var intersections = Math3D.GetIntersection_Hull_Ray(_asteroid, o.Item1, o.Item2);

            //        if (intersections.Length != 2)
            //        {
            //            return (Tuple<Point3D, Point3D>)null;
            //        }

            //        Vector3D penetrate = (intersections[1].Item1 - intersections[0].Item1) * o.Item3;

            //        return Tuple.Create(intersections[0].Item1, intersections[0].Item1 + penetrate);
            //    }).
            //    Where(o => o != null).
            //    ToArray();
            #endregion
            var hits = _shots.
                Select(o => new ShotHit()
                {
                    Shot = o,
                    Hit = ExplosionForceWorker.GetHit(_asteroid, o.Item1, o.Item2, o.Item3),
                }).
                Where(o => o != null).
                ToArray();

            if (hits.Length == 0)
            {
                if (_shots != null)
                {
                    foreach (var shot in _shots)
                    {
                        DrawShot(shot.Item1, shot.Item2 * 2);
                    }
                }

                _voronoi = null;
                DrawAsteroid(_asteroid);
                return;
            }

            // Voronoi
            Point3D[] controlPoints = ExplosionForceWorker.GetVoronoiCtrlPoints(hits, _asteroid);

            _voronoi = Math3D.GetVoronoi(controlPoints, true);

            DrawShatteredAsteroid_ORIG();
        }
        private void ProcessShots()
        {
            int parsedInt;
            int? minCount = null;
            if (int.TryParse(txtMinShards.Text, out parsedInt))
            {
                minCount = parsedInt;
                txtMinShards.Effect = null;
            }
            else
            {
                txtMinShards.Effect = _errorEffect;
            }

            int? maxCount = null;
            if (int.TryParse(txtMaxShards.Text, out parsedInt))
            {
                maxCount = parsedInt;
                txtMaxShards.Effect = null;
            }
            else
            {
                txtMaxShards.Effect = _errorEffect;
            }

            // Dist dot power
            double? distDotPow = null;
            txtDistDotPow.Effect = null;
            if (chkUseDistDot.IsChecked.Value)
            {
                double parsed;
                if (double.TryParse(txtDistDotPow.Text, out parsed))
                {
                    distDotPow = parsed;
                }
                else
                {
                    txtDistDotPow.Effect = _errorEffect;
                }
            }

            HullVoronoiExploder_Options options = new HullVoronoiExploder_Options()
            {
                DistanceDot_Power = distDotPow,
                InteriorVelocityCenterPercent = chkUseInteriorVelocityCenterPercent.IsChecked.Value ? trkInteriorVelocityCenterPercent.Value : (double?)null,
                RandomVelocityPercent = chkUseRandomVelocityPercent.IsChecked.Value ? trkRandomVelocityPercent.Value : (double?)null,
                ShouldSmoothShards = chkSmoothShards.IsChecked.Value,
            };

            if (minCount != null) options.MinCount = minCount.Value;
            if (maxCount != null) options.MaxCount = maxCount.Value;

            // The length of _shots direction is for the gui (it starts beyond the asteroid, and goes well past it).  The worker needs an actual length
            var adjustedShots = _shots.
                Select(o => Tuple.Create(o.Item1, o.Item2.ToUnit() * trkShotMultiplier.Value, o.Item3)).
                ToArray();

            _explosion = HullVoronoiExploder.ShootHull(_asteroid, adjustedShots, options);

            DrawShatteredAsteroid();
        }

        private void DrawShatteredAsteroid_ORIG()
        {
            ClearVisuals();

            if (_shots != null)
            {
                foreach (var shot in _shots)
                {
                    DrawShot(shot.Item1, shot.Item2 * 2);
                }
            }

            if (_asteroid != null && _voronoi == null)
            {
                DrawAsteroid(_asteroid);
                return;
            }
            else if (_voronoi == null)
            {
                return;
            }

            //var asteroidShards = Math3D.GetIntersection_Hull_Voronoi_full(_asteroid, _voronoi);

            Tuple<int, ITriangleIndexed[]>[] asteroidShards = null;
            try
            {
                asteroidShards = Math3D.GetIntersection_Hull_Voronoi_full(_asteroid, _voronoi);
            }
            catch (Exception ex)
            {
                asteroidShards = null;
            }


            if (chkSmoothShards.IsChecked.Value)
            {
                asteroidShards = asteroidShards.
                    Select(o => Tuple.Create(o.Item1, Asteroid.SmoothTriangles(o.Item2))).
                    ToArray();
            }

            AddDots(_voronoi.ControlPoints, DOTRADIUS * 8, _colors.ControlPoint);

            // Original Asteroid
            if (chkDrawLines.IsChecked.Value)
            {
                AddHull(_asteroid, false, true, false, false, false, false);        // just dark lines
            }

            // Shards
            if (chkPhysics.IsChecked.Value)
            {
                var aabb = Math3D.GetAABB(_asteroid);
                PhysicsAsteroids_ORIG(asteroidShards, _shots, Math.Sqrt((aabb.Item2 - aabb.Item1).Length / 2));
            }
            else
            {
                foreach (var shard in asteroidShards)
                {
                    DrawAsteroid(shard.Item2);
                }
            }
        }
        private void DrawShatteredAsteroid()
        {
            ClearVisuals();

            if (_shots != null)
            {
                foreach (var shot in _shots)
                {
                    DrawShot(shot.Item1, shot.Item2 * 2);
                }
            }

            if (_asteroid != null && (_explosion == null || _explosion.Voronoi == null || _explosion.Shards == null))
            {
                DrawAsteroid(_asteroid);
                return;
            }
            else if (_explosion == null || _explosion.Voronoi == null || _explosion.Shards == null)
            {
                return;
            }

            AddDots(_explosion.Voronoi.ControlPoints, DOTRADIUS * 8, _colors.ControlPoint);

            // Original Asteroid
            if (chkDrawLines.IsChecked.Value)
            {
                AddHull(_asteroid, false, true, false, false, false, false);        // just dark lines
            }

            // Shards
            if (chkPhysics.IsChecked.Value)
            {
                PhysicsAsteroids();
            }
            else
            {
                foreach (var shard in _explosion.Shards)
                {
                    DrawAsteroid(shard.Hull_ParentCoords);
                }
            }
        }

        private void DrawAsteroid(ITriangleIndexed[] hull)
        {
            var visuals = GetAsteroid(hull);

            foreach (Visual3D visual in visuals.Item2)
            {
                _viewport.Children.Add(visual);
                _visuals.Add(visual);
            }
        }
        private Tuple<Model3D, Visual3D[]> GetAsteroid(ITriangleIndexed[] hull)
        {
            Visual3D[] visuals = GetHull(hull, chkDrawFaces.IsChecked.Value, chkDrawLines.IsChecked.Value, chkDrawNormals.IsChecked.Value, chkNearlyTransparent.IsChecked.Value, chkSoftFaces.IsChecked.Value, true);

            return new Tuple<Model3D, Visual3D[]>(null, visuals);
        }
        private void DrawShot(Point3D source, Vector3D direction)
        {
            AddDot(source, DOTRADIUS * 4, _colors.Shot);
            AddLine_Billboard(source, source + direction, LINETHICKNESS_BILLBOARD, _colors.Shot);
        }

        private void AddHull(ITriangleIndexed[] triangles, bool drawFaces, bool drawLines, bool drawNormals, bool nearlyTransparent, bool softFaces, bool isBrightLines)
        {
            Visual3D[] visuals = GetHull(triangles, drawFaces, drawLines, drawNormals, nearlyTransparent, softFaces, isBrightLines);

            foreach (Visual3D visual in visuals)
            {
                _viewport.Children.Add(visual);
                _visuals.Add(visual);
            }
        }
        private void AddLine_ScreenSpace(Point3D from, Point3D to, double thickness, Color color)
        {
            ScreenSpaceLines3D lineVisual = new ScreenSpaceLines3D(true);
            lineVisual.Thickness = thickness;
            lineVisual.Color = color;
            lineVisual.AddLine(from, to);

            _viewport.Children.Add(lineVisual);
            _visuals.Add(lineVisual);
        }
        private void AddLines_ScreenSpace(IEnumerable<Tuple<int, int>> lines, Point3D[] points, double thickness, Color color)
        {
            // Draw the lines
            ScreenSpaceLines3D lineVisual = new ScreenSpaceLines3D(true);
            lineVisual.Thickness = thickness;
            lineVisual.Color = color;

            foreach (var line in lines)
            {
                lineVisual.AddLine(points[line.Item1], points[line.Item2]);
            }

            _viewport.Children.Add(lineVisual);
            _visuals.Add(lineVisual);
        }
        private void AddLines_ScreenSpace(IEnumerable<Tuple<Point3D, Point3D>> lines, double thickness, Color color)
        {
            // Draw the lines
            ScreenSpaceLines3D lineVisual = new ScreenSpaceLines3D(true);
            lineVisual.Thickness = thickness;
            lineVisual.Color = color;

            foreach (var line in lines)
            {
                lineVisual.AddLine(line.Item1, line.Item2);
            }

            _viewport.Children.Add(lineVisual);
            _visuals.Add(lineVisual);
        }
        private void AddLines_ScreenSpace(IEnumerable<Point3D> line, double thickness, Color color)
        {
            ScreenSpaceLines3D lineVisual = new ScreenSpaceLines3D(true);
            lineVisual.Thickness = thickness;
            lineVisual.Color = color;

            Point3D? prev = null;

            foreach (var point in line)
            {
                if (prev == null)
                {
                    prev = point;
                    continue;
                }

                lineVisual.AddLine(prev.Value, point);

                prev = point;
            }

            _viewport.Children.Add(lineVisual);
            _visuals.Add(lineVisual);
        }
        private void AddLine_Billboard(Point3D from, Point3D to, double thickness, Color color)
        {
            ModelVisual3D visual = new ModelVisual3D();

            visual.Content = new Game.HelperClassesWPF.Primitives3D.BillboardLine3D()
            {
                Color = color,
                IsReflectiveColor = false,
                Thickness = thickness,
                FromPoint = from,
                ToPoint = to,
            }.Model;

            _viewport.Children.Add(visual);
            _visuals.Add(visual);
        }
        private void AddLines_Billboard(IEnumerable<Tuple<int, int>> lines, Point3D[] points, double thickness, Color color)
        {
            BillboardLine3DSet visual = new BillboardLine3DSet();
            visual.Color = color;
            visual.BeginAddingLines();

            foreach (Tuple<int, int> line in lines)
            {
                visual.AddLine(points[line.Item1], points[line.Item2], thickness);
            }

            visual.EndAddingLines();

            _viewport.Children.Add(visual);
            _visuals.Add(visual);
        }
        private void AddDots(IEnumerable<Point3D> positions, double radius, Color color, bool isHiRes = true)
        {
            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
            materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(Colors.White, color, .3d)), 30d));

            Model3DGroup geometries = new Model3DGroup();

            foreach (Point3D position in positions)
            {
                // Geometry Model
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = materials;
                geometry.BackMaterial = materials;
                geometry.Geometry = UtilityWPF.GetSphere_Ico(radius, isHiRes ? 3 : 0, true);
                geometry.Transform = new TranslateTransform3D(position.ToVector());

                geometries.Children.Add(geometry);
            }

            // Model Visual
            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = geometries;

            _viewport.Children.Add(visual);
            _visuals.Add(visual);
        }
        private void AddDot(Point3D position, double radius, Color color, bool isHiRes = true)
        {
            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
            materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(Colors.White, color, .3d)), 30d));

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;
            geometry.Geometry = UtilityWPF.GetSphere_Ico(radius, isHiRes ? 3 : 0, true);

            // Model Visual
            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = geometry;
            visual.Transform = new TranslateTransform3D(position.ToVector());

            // Temporarily add to the viewport
            _viewport.Children.Add(visual);
            _visuals.Add(visual);
        }
        private void AddText3D(string text, Point3D center, double height, Color color)
        {
            MaterialGroup faceMaterial = new MaterialGroup();
            faceMaterial.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
            faceMaterial.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("60FFFFFF")), 5d));

            MaterialGroup edgeMaterial = new MaterialGroup();
            edgeMaterial.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.OppositeColor(color))));
            edgeMaterial.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("80808080")), 1d));

            double edgeDepth = height / 15;

            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = UtilityWPF.GetText3D(text, _font.Value, faceMaterial, edgeMaterial, height, edgeDepth);
            visual.Transform = new TranslateTransform3D(center.ToVector());

            // Temporarily add to the viewport
            _viewport.Children.Add(visual);
            _visuals.Add(visual);
        }

        private Visual3D[] GetHull(ITriangleIndexed[] triangles, bool drawFaces, bool drawLines, bool drawNormals, bool nearlyTransparent, bool softFaces, bool isBrightLines)
        {
            List<Visual3D> retVal = new List<Visual3D>();

            if (drawLines)
            {
                #region Lines

                // Draw the lines
                ScreenSpaceLines3D lineVisual = new ScreenSpaceLines3D(true);

                lineVisual.Thickness = (!drawFaces || nearlyTransparent) ?
                    LINETHICKNESS_SCREENSPACE * .5d :
                    LINETHICKNESS_SCREENSPACE;

                lineVisual.Color = isBrightLines ?
                    _colors.PrimaryLine_Bright :
                    _colors.PrimaryLine_Dark;

                Point3D[] points = triangles[0].AllPoints;

                foreach (var line in TriangleIndexed.GetUniqueLines(triangles))
                {
                    lineVisual.AddLine(points[line.Item1], points[line.Item2]);
                }

                //_viewport.Children.Add(lineVisual);
                //_visuals.Add(lineVisual);

                retVal.Add(lineVisual);

                #endregion
            }

            if (drawNormals)
            {
                #region Normals

                // Draw the lines
                ScreenSpaceLines3D lineVisual = new ScreenSpaceLines3D(true);
                lineVisual.Thickness = (!drawFaces || nearlyTransparent) ?
                    LINETHICKNESS_SCREENSPACE * .5d :
                    LINETHICKNESS_SCREENSPACE;
                lineVisual.Color = _colors.NormalLine;

                foreach (TriangleIndexed triangle in triangles)
                {
                    Point3D centerPoint = triangle.GetCenterPoint();
                    lineVisual.AddLine(centerPoint, centerPoint + triangle.Normal);
                }

                //_viewport.Children.Add(lineVisual);
                //_visuals.Add(lineVisual);

                retVal.Add(lineVisual);

                #endregion
            }

            if (drawFaces)
            {
                #region Faces

                // Material
                MaterialGroup materials = new MaterialGroup();
                materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(nearlyTransparent ? _colors.HullFaceTransparent : _colors.HullFace)));

                if (nearlyTransparent)
                {
                    materials.Children.Add(_colors.HullFaceSpecularTransparent);
                }
                else
                {
                    if (softFaces)
                    {
                        materials.Children.Add(_colors.HullFaceSpecularSoft);
                    }
                    else
                    {
                        materials.Children.Add(_colors.HullFaceSpecular);
                    }
                }

                // Geometry Mesh
                MeshGeometry3D mesh = null;

                if (softFaces)
                {
                    mesh = UtilityWPF.GetMeshFromTriangles(TriangleIndexed.Clone_CondensePoints(triangles));
                }
                else
                {
                    mesh = UtilityWPF.GetMeshFromTriangles_IndependentFaces(triangles);
                }

                // Geometry Model
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = materials;
                geometry.BackMaterial = materials;
                geometry.Geometry = mesh;

                // Model Visual
                ModelVisual3D visual = new ModelVisual3D();
                visual.Content = geometry;

                //_viewport.Children.Add(visual);
                //_visuals.Add(visual);

                retVal.Add(visual);

                #endregion
            }

            return retVal.ToArray();
        }

        private void PhysicsAsteroids_ORIG(Tuple<int, ITriangleIndexed[]>[] asteroidShards, Tuple<Point3D, Vector3D, double>[] shots, double asteroidRadius)
        {
            const double ONETHIRD = 1d / 3d;
            const double DENSITY = 10;

            var subAsteroidProps = asteroidShards.
                Select(o =>
                {
                    #region examine shard

                    var aabb = Math3D.GetAABB(o.Item2);

                    double radius = Math.Sqrt((aabb.Item2 - aabb.Item1).Length / 2);

                    Point3D center = Math3D.GetCenter(TriangleIndexed.GetUsedPoints(o.Item2));
                    Vector3D centerVect = center.ToVector();

                    Point3D[] allPoints = o.Item2[0].AllPoints.
                        Select(p => p - centerVect).
                        ToArray();

                    TriangleIndexed[] shiftedTriangles = o.Item2.
                        Select(p => new TriangleIndexed(p.Index0, p.Index1, p.Index2, allPoints)).
                        ToArray();

                    return new
                    {
                        Index = o.Item1,
                        Triangles = shiftedTriangles,
                        Radius = radius,
                        Center = center,
                    };

                    #endregion
                }).
                ToArray();


            //TODO: When the asteroid shards are resmoothed, the velocities are very small
            //
            //Maybe add more orth velocity
            //
            //Maybe pull the hit source slightly into the asteroid
            //
            //Maybe add some random velocity (size proportional to the velocity)

            Vector3D[] veclocites = CalculateVelocites(subAsteroidProps.Select(o => o.Center).ToArray(), shots, asteroidRadius);

            veclocites = veclocites.
                Select(o => o + Math3D.GetRandomVector_Spherical(o.Length * .25)).
                ToArray();



            var getMass = new Func<double, ITriangleIndexed[], double>((rad, tris) =>
            {
                double volume = 4d * ONETHIRD * Math.PI * rad * rad * rad;
                return DENSITY * volume;
            });

            _asteroidShards = new Asteroid[asteroidShards.Length];

            for (int cntr = 0; cntr < asteroidShards.Length; cntr++)
            {
                AsteroidExtra extra = new AsteroidExtra()
                {
                    Triangles = subAsteroidProps[cntr].Triangles,
                    RandomRotation = false,
                    GetVisual = GetAsteroid,
                };

                _asteroidShards[cntr] = new Asteroid(subAsteroidProps[cntr].Radius, getMass, subAsteroidProps[cntr].Center, _world, _map, _material_Asteroid, extra);

                _asteroidShards[cntr].PhysicsBody.Velocity = veclocites[cntr];

                _map.AddItem(_asteroidShards[cntr]);
            }
        }
        private void PhysicsAsteroids()
        {
            const double ONETHIRD = 1d / 3d;
            const double DENSITY = 10;

            if (_explosion == null || _explosion.Shards == null)
            {
                return;
            }

            var getMass = new Func<double, ITriangleIndexed[], double>((rad, tris) =>
            {
                double volume = 4d * ONETHIRD * Math.PI * rad * rad * rad;
                return DENSITY * volume;
            });

            _asteroidShards = new Asteroid[_explosion.Shards.Length];

            for (int cntr = 0; cntr < _explosion.Shards.Length; cntr++)
            {
                AsteroidExtra extra = new AsteroidExtra()
                {
                    Triangles = _explosion.Shards[cntr].Hull_Centered,
                    RandomRotation = false,
                    GetVisual = GetAsteroid,
                };

                _asteroidShards[cntr] = new Asteroid(_explosion.Shards[cntr].Radius, getMass, _explosion.Shards[cntr].Center_ParentCoords, _world, _map, _material_Asteroid, extra);

                if (_explosion.Velocities != null && _explosion.Velocities.Length == _explosion.Shards.Length)
                {
                    _asteroidShards[cntr].PhysicsBody.Velocity = _explosion.Velocities[cntr];
                }

                _map.AddItem(_asteroidShards[cntr]);
            }
        }

        private Vector3D[] CalculateVelocites(Point3D[] centers, Tuple<Point3D, Vector3D, double>[] shots, double asteroidRadius)
        {
            Vector3D[] retVal = new Vector3D[centers.Length];

            foreach (var shot in shots)
            {
                Vector3D[] velocities = DistributeForces(centers, shot.Item1, shot.Item2 * shot.Item3, asteroidRadius);

                for (int cntr = 0; cntr < retVal.Length; cntr++)
                {
                    retVal[cntr] += velocities[cntr];
                }
            }

            return retVal;
        }

        private Point3D[] GetVoronoiCtrlPoints_AroundLine(Point3D lineStart, Point3D lineStop)
        {
            const int MIN = 5;

            double lineLength = (lineStop - lineStart).Length;
            double drift = lineLength / 20;
            bool evenDist = chkOneShotPlateEvenDistribute.IsChecked.Value;

            List<Point3D> points = new List<Point3D>();

            // Plates
            if (radOneShot_SinglePlate.IsChecked.Value)
            {
                points.AddRange(GetVoronoiCtrlPoints_AroundLine_Plate(lineLength * trkOneShotPercentDist1.Value, 0, evenDist, drift));
            }
            else if (radOneShot_TwoPlates.IsChecked.Value)
            {
                points.AddRange(GetVoronoiCtrlPoints_AroundLine_Plate(lineLength * trkOneShotPercentDist1.Value, -lineLength / 6, evenDist, drift));
                points.AddRange(GetVoronoiCtrlPoints_AroundLine_Plate(lineLength * trkOneShotPercentDist2.Value, lineLength / 6, evenDist, drift));
            }
            else if (radOneShot_ThreePlates.IsChecked.Value)
            {
                points.AddRange(GetVoronoiCtrlPoints_AroundLine_Plate(lineLength * trkOneShotPercentDist1.Value, -lineLength / 4, evenDist, drift));
                points.AddRange(GetVoronoiCtrlPoints_AroundLine_Plate(lineLength * trkOneShotPercentDist2.Value, 0, evenDist, drift));
                points.AddRange(GetVoronoiCtrlPoints_AroundLine_Plate(lineLength * trkOneShotPercentDist3.Value, lineLength / 4, evenDist, drift));
            }
            else
            {
                throw new ApplicationException("Unknown plate configuration");
            }

            // Center line
            if (radOneShot_LineNone.IsChecked.Value)
            {
            }
            else if (radOneShot_LineAbove.IsChecked.Value)
            {
                points.AddRange(GetVoronoiCtrlPoints_AroundLine_CenterLine(new[] { -lineLength / 4 }, drift * 2));
            }
            else if (radOneShot_LineMiddle.IsChecked.Value)
            {
                points.AddRange(GetVoronoiCtrlPoints_AroundLine_CenterLine(new[] { 0d }, drift * 2));
            }
            else if (radOneShot_LineBelow.IsChecked.Value)
            {
                points.AddRange(GetVoronoiCtrlPoints_AroundLine_CenterLine(new[] { lineLength / 4 }, drift * 2));
            }
            else if (radOneShot_LineSeveral.IsChecked.Value)
            {
                var zs = Enumerable.Range(0, StaticRandom.Next(1, 5)).
                    Select(o => StaticRandom.NextDouble(-lineLength / 2, lineLength / 2));
                points.AddRange(GetVoronoiCtrlPoints_AroundLine_CenterLine(zs, drift * 2));
            }
            else
            {
                throw new ApplicationException("Unknown centerline configuration");
            }

            // The voronoi algrorithm fails if there aren't at least 5 points (really should fix that).  So add points that are
            // way away from the hull.  This will make the voronoi algorithm happy, and won't affect the local results
            if (points.Count < MIN)
            {
                points.AddRange(Enumerable.Range(0, MIN - points.Count).
                    Select(o => Math3D.GetRandomVector_Spherical_Shell(lineLength * 20).ToPoint()));
            }

            // Rotate/Translate
            Quaternion roation = Math3D.GetRotation(new Vector3D(0, 0, 1), lineStop - lineStart);

            Point3D centerPoint = Math3D.GetCenter(new[] { lineStart, lineStop });

            Transform3DGroup transform = new Transform3DGroup();
            transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(roation)));
            transform.Children.Add(new TranslateTransform3D(centerPoint.X, centerPoint.Y, centerPoint.Z));

            return points.Select(o => transform.Transform(o)).
                ToArray();
        }
        private static IEnumerable<Point3D> GetVoronoiCtrlPoints_AroundLine_Plate(double radius, double z, bool isEvenDistribute, double drift)
        {
            Vector[] initial;

            if (isEvenDistribute)
            {
                initial = Math3D.GetRandomVectors_CircularRing_EvenDist(StaticRandom.Next(3, 7), radius);
            }
            else
            {
                initial = Enumerable.Range(0, StaticRandom.Next(2, 7)).
                    Select(o => Math3D.GetRandomVector_Circular_Shell(radius).ToVector2D()).
                    ToArray();
            }

            return initial.
                Select(o => new Point3D(o.X, o.Y, z)).
                Select(o => o + Math3D.GetRandomVector_Spherical(drift));     // the voronoi can't handle coplanar input
        }
        private static IEnumerable<Point3D> GetVoronoiCtrlPoints_AroundLine_CenterLine(IEnumerable<double> zs, double drift)
        {
            return zs.
                Select(o =>
                {
                    Vector3D point = Math3D.GetRandomVector_Spherical(drift);
                    return new Point3D(point.X, point.Y, point.Z + o);
                });
        }

        /// <summary>
        /// Make a few (2 to 6) control points radially around the line.  All in a plane
        /// </summary>
        /// <remarks>
        /// The voronoi algorithm requires at least 4 (probably not coplanar).  So if you want fewer than that, extra points
        /// are created away from the hull
        /// </remarks>
        private static Point3D[] GetVoronoiCtrlPoints_AroundLine_Simple(Point3D lineStart, Point3D lineStop, int count)
        {
            const int MIN = 5;

            //double radius = (lineStop - lineStart).Length * .5;
            double radius = (lineStop - lineStart).Length * .1;

            var points = Enumerable.Range(0, count).
                Select(o => Math3D.GetRandomVector_Circular_Shell(radius).ToPoint());
            //Select(o => Math3D.GetRandomVector_Spherical(radius).ToPoint());

            // The voronoi algrorithm fails if there aren't at least 5 points (really should fix that).  So add points that are
            // way away from the hull.  This will make the voronoi algorithm happy, and won't affect the local results
            if (count < MIN)
            {
                points = points.Concat(
                    Enumerable.Range(0, MIN - count).
                    //Select(o => Math3D.GetRandomVector_Circular_Shell(radius * 100).ToPoint())
                    Select(o => Math3D.GetRandomVector_Spherical_Shell(radius * 20).ToPoint())
                    );
            }

            //points = points.Select(o => new Point3D(o.X, o.Y, Math1D.GetNearZeroValue(radius / 20)));     // the voronoi can't handle coplanar input
            points = points.Select(o => o + Math3D.GetRandomVector_Spherical(radius / 20));     // the voronoi can't handle coplanar input

            // This will cause a cylinder core to be cut out, but only if the other points are ringed around (if they clump around one angle, then this won't look right)
            //points = UtilityCore.Iterate<Point3D>(points, Math3D.GetRandomVector_Spherical(radius / 10).ToPoint());

            // Rotate/Translate
            Quaternion roation = Math3D.GetRotation(new Vector3D(0, 0, 1), lineStop - lineStart);

            Point3D centerPoint = Math3D.GetCenter(new[] { lineStart, lineStop });

            Transform3DGroup transform = new Transform3DGroup();
            transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(roation)));
            transform.Children.Add(new TranslateTransform3D(centerPoint.X, centerPoint.Y, centerPoint.Z));

            points = points.Select(o => transform.Transform(o));

            return points.ToArray();
        }

        private static Point3D[] GetVoronoiCtrlPoints_TwoLines(Tuple<Point3D, ITriangle, double>[][] hits, ITriangleIndexed[] asteroid)
        {
            if (hits.Length != 2 || hits.Any(o => o.Length != 2))
            {
                throw new ArgumentException("This method expects 2 hits, each with two endpoints");
            }

            // Examine hits
            Tuple<int, double>[] hitsByLength = hits.
                Select((o, i) => Tuple.Create(i, (o[1].Item1 - o[0].Item1).Length)).
                OrderByDescending(o => o.Item2).
                ToArray();

            double totalLength = hitsByLength.Sum(o => o.Item2);

            Tuple<int, double>[] hitsByPercentLength = hitsByLength.
                Select(o => Tuple.Create(o.Item1, o.Item2 / totalLength)).
                ToArray();

            // Define control point cones
            var aabb = Math3D.GetAABB(asteroid);
            double aabbLen = (aabb.Item2 - aabb.Item1).Length;

            double entryRadius = aabbLen * .03;
            double exitRadius = aabbLen * .15;
            double maxAxisLength = aabbLen * .75;

            List<Point3D> retVal = new List<Point3D>();

            //TODO: Instead of a fixed number here, figure out how many control points to make based on the total
            //length of the hits in relation to the volume of the asteroid
            for (int cntr = 0; cntr < 3; cntr++)
            {
                int index = UtilityCore.GetIndexIntoList(StaticRandom.NextDouble(), hitsByPercentLength);

                retVal.AddRange(ExplosionForceWorker.GetVoronoiCtrlPoints_AroundLine_Cone(hits[index][0].Item1, hits[index][1].Item1, StaticRandom.Next(2, 5), entryRadius, exitRadius, maxAxisLength));
            }

            return retVal.ToArray();

            #region OLD THOUGHTS

            //DIFFICULT: This should be doable, but seems more difficult than it's worth (also very hardcoded and specific - not very natural)
            // Draw a line between the two entry points, and another line between the two exit points
            // (or reverse one line if the shots come from opposite directions)
            //
            // Choose a 5th point that is the midpoint of the segment: Math3D.GetClosestPoints_Line_Line()
            //
            // No choose control points that will have those 4 triangles as edge faces




            //FAIL: The plate is very likely not coplanar
            // Create a plate with the four hitpoints as verticies
            //
            //  Choose two points on either side of the plate (equidistant)

            #endregion
        }

        private static Point3D[] GetVoronoiCtrlPoints_AroundLine_Cone_SPHERE(Point3D lineStart, Point3D lineStop, int count)
        {
            return Enumerable.Range(0, count).
                Select(o => Math3D.GetRandomVector_Spherical(MAXRADIUS / 3, MAXRADIUS).ToPoint())
                .ToArray();
        }

        private void RedrawProjForce()
        {
            double linearDotPow = .4;
            if (chkProjForceUseDistDot.IsChecked.Value && !double.TryParse(txtProjForceDistDotPow.Text, out linearDotPow))
            {
                txtProjForceDistDotPow.Effect = _errorEffect;
                return;
            }
            else
            {
                txtProjForceDistDotPow.Effect = null;
            }

            Point3D hitStart = new Point3D(0, MAXRADIUS * -.6, 0);
            Vector3D hitDirection = new Vector3D(0, MAXRADIUS / 2, 0);

            double asteroidSize = MAXRADIUS * .8;

            Vector3D hitDirectionUnit = hitDirection.ToUnit();
            double hitForceBase = hitDirection.Length / asteroidSize;

            ClearAll();

            AddDot(hitStart, DOTRADIUS * 4, _colors.Shot);
            AddLine_Billboard(hitStart, hitStart + hitDirection, LINETHICKNESS_BILLBOARD, _colors.Shot);

            if (_projForcePoints != null)
            {
                AddDots(_projForcePoints, DOTRADIUS, _colors.ControlPoint);

                var vectors = _projForcePoints.
                    Select(o =>
                    {
                        Vector3D toPoint = o - hitStart;
                        Vector3D toPointUnit = toPoint.ToUnit();

                        double dot = Vector3D.DotProduct(hitDirectionUnit, toPointUnit);
                        double linearDot = Math3D.GetLinearDotProduct(hitDirectionUnit, toPointUnit);

                        Vector3D along = toPoint.GetProjectedVector(hitDirection).ToUnit(false);
                        Vector3D orth = (o - Math3D.GetClosestPoint_Line_Point(hitStart, hitDirection, o)).ToUnit(false);

                        along *= linearDot * trkProjForceAlong.Value;
                        orth *= (1 - linearDot) * trkProjForceOrth.Value;

                        double distance = toPoint.Length;

                        // Exaggerate the distance based on dot product.  That way, points in line with the hit will get more of the impact
                        double scale = (1 - Math.Abs(linearDot));
                        scale = Math.Pow(scale, linearDotPow);
                        double scaledDistance = scale * distance;

                        return new
                        {
                            ForcePoint = o,

                            Distance = distance,
                            ScaledDistance = scaledDistance,

                            LinearDot = linearDot,
                            Dot = dot,

                            Along = along,
                            Orth = orth,
                        };
                    }).
                    ToArray();

                double[] distances = chkProjForceUseDistDot.IsChecked.Value ?
                    vectors.Select(o => o.ScaledDistance).ToArray() :
                    vectors.Select(o => o.Distance).ToArray();

                double[] percents = ExplosionForceWorker.GetPercentOfProjForce(distances);

                for (int cntr = 0; cntr < vectors.Length; cntr++)
                {
                    Vector3D along = vectors[cntr].Along * (hitForceBase * percents[cntr] * trkProjForceOverall.Value);
                    Vector3D orth = vectors[cntr].Orth * (hitForceBase * percents[cntr] * trkProjForceOverall.Value);

                    AddLine_Billboard(vectors[cntr].ForcePoint, vectors[cntr].ForcePoint + along, LINETHICKNESS_BILLBOARD / 3, UtilityWPF.ColorFromHex("808080"));
                    AddLine_Billboard(vectors[cntr].ForcePoint, vectors[cntr].ForcePoint + orth, LINETHICKNESS_BILLBOARD / 3, UtilityWPF.ColorFromHex("808080"));
                }
            }
        }

        private static Vector3D[] DistributeForces_UNECESSARRILYCOMLEX(Point3D[] bodyCenters, Point3D hitStart, Vector3D hitDirection, double mainBodyRadius, bool useDistDot = true, double distDotPow = .4)
        {
            Vector3D hitDirectionUnit = hitDirection.ToUnit();
            double hitForceBase = hitDirection.Length / mainBodyRadius;

            var vectors = bodyCenters.
                Select(o =>
                {
                    Vector3D toPoint = o - hitStart;
                    Vector3D toPointUnit = toPoint.ToUnit();

                    double dot = Vector3D.DotProduct(hitDirectionUnit, toPointUnit);
                    double linearDot = Math3D.GetLinearDotProduct(hitDirectionUnit, toPointUnit);

                    Vector3D along = toPoint.GetProjectedVector(hitDirection).ToUnit(false);
                    Vector3D orth = (o - Math3D.GetClosestPoint_Line_Point(hitStart, hitDirection, o)).ToUnit(false);

                    along *= linearDot;
                    orth *= (1 - linearDot);

                    double distance = toPoint.Length;

                    // Exaggerate the distance based on dot product.  That way, points in line with the hit will get more of the impact
                    double scale = (1 - Math.Abs(linearDot));
                    scale = Math.Pow(scale, distDotPow);
                    double scaledDistance = scale * distance;

                    return new
                    {
                        ForcePoint = o,
                        Distance = distance,
                        ScaledDistance = scaledDistance,
                        Along = along,
                        Orth = orth,
                    };
                }).
                ToArray();

            double[] distances = useDistDot ?
                vectors.Select(o => o.ScaledDistance).ToArray() :
                vectors.Select(o => o.Distance).ToArray();

            double[] percents = ExplosionForceWorker.GetPercentOfProjForce(distances);

            Vector3D[] retVal = new Vector3D[vectors.Length];

            for (int cntr = 0; cntr < vectors.Length; cntr++)
            {
                Vector3D along = vectors[cntr].Along * (hitForceBase * percents[cntr]);
                Vector3D orth = vectors[cntr].Orth * (hitForceBase * percents[cntr]);

                retVal[cntr] = along + orth;
            }

            return retVal;
        }
        /// <summary>
        /// This will figure how much velocity to apply to each bodyCenter
        /// TODO: Also calculate angular velocities
        /// </summary>
        /// <param name="bodyCenters">The bodies to apply velocity to</param>
        /// <param name="hitStart">The point of impact</param>
        /// <param name="hitDirection">The direction and force of the impact</param>
        /// <param name="mainBodyRadius">The avg radius of the body that is getting blown apart</param>
        /// <param name="baseMultiplier">
        /// TODO: Get rid of this.  The hit direction's length should already be scaled by this amount
        /// The returned forces will be multiplied by this
        /// </param>
        /// <param name="useDistDot">
        /// When calculating distance between the point of impact each body center, that distance can be warped based on
        /// the dot product.  This will cause points in line with the impact to get more of the force
        /// </param>
        /// <param name="distDotPow">
        /// If useDistDot is true, then the linear dot product is taken to the power.  Less than one will favor items with....idk, makes the
        /// effect a bit less pronounced.  Powers greater than one amplify the dot product's effect
        /// </param>
        private static Vector3D[] DistributeForces(Point3D[] bodyCenters, Point3D hitStart, Vector3D hitDirection, double mainBodyRadius, double baseMultiplier = 1, bool useDistDot = true, double distDotPow = .4)
        {
            Vector3D hitDirectionUnit = hitDirection.ToUnit();
            double hitForceBase = hitDirection.Length / mainBodyRadius;

            var vectors = bodyCenters.
                Select(o =>
                {
                    Vector3D direction = o - hitStart;
                    Vector3D directionUnit = direction.ToUnit();

                    double distance = direction.Length;
                    double scaledDistance = distance;
                    if (useDistDot)
                    {
                        double linearDot = Math3D.GetLinearDotProduct(hitDirectionUnit, directionUnit);     // making it linear so the power function is more predictable

                        // Exaggerate the distance based on dot product.  That way, points in line with the hit will get more of the impact
                        double scale = (1 - Math.Abs(linearDot));
                        scale = Math.Pow(scale, distDotPow);
                        scaledDistance = scale * distance;
                    }

                    return new
                    {
                        ForcePoint = o,
                        Distance = distance,
                        ScaledDistance = scaledDistance,
                        DirectionUnit = directionUnit,
                    };
                }).
                ToArray();

            double[] percents = ExplosionForceWorker.GetPercentOfProjForce(vectors.Select(o => o.ScaledDistance).ToArray());

            Vector3D[] retVal = new Vector3D[vectors.Length];

            for (int cntr = 0; cntr < vectors.Length; cntr++)
            {
                retVal[cntr] = vectors[cntr].DirectionUnit * (hitForceBase * percents[cntr]);
            }

            return retVal;
        }

        /// <summary>
        /// This returns a graph of the ideal curve (just for visual)
        /// </summary>
        private static Point3D[] GetPercentOfProjForce_Equation(double[] distances, double baseConst = Math.E)
        {
            const int COUNT = 250;

            var avg_stdev = Math1D.Get_Average_StandardDeviation(distances);
            double scale = avg_stdev.Item2 / avg_stdev.Item1;
            scale *= baseConst;
            scale += 1;

            var deviations = Enumerable.Range(0, COUNT).
                Select(o =>
                {
                    double x = UtilityCore.GetScaledValue(0, MAXRADIUS, 0, COUNT, o);

                    double distFromAvg = Math.Abs(x - avg_stdev.Item1);
                    double deviationsFromAvg = distFromAvg / avg_stdev.Item2;

                    if (x > avg_stdev.Item1)
                    {
                        deviationsFromAvg = -deviationsFromAvg;
                    }

                    //return Math.Pow(Math.E, deviationsFromAvg);
                    return new Point3D(x, Math.Pow(scale, deviationsFromAvg), 0);
                }).
                ToArray();

            double sum = deviations.Sum(o => o.Y);

            return deviations.
                Select(o => new Point3D(o.X, o.Y / sum, o.Z)).
                ToArray();
        }

        #endregion
    }
}
