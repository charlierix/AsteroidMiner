using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;

namespace Game.Newt.v2.Arcanorum
{
    public class WeaponSpikeBall : WeaponPart
    {
        #region Declaration Section

        private readonly WeaponMaterialCache _materials;

        #endregion

        #region Constructor

        public WeaponSpikeBall(WeaponMaterialCache materials, WeaponSpikeBallDNA dna)
        {
            _materials = materials;

            var model = GetModel(dna, materials);

            this.Model = model.Item1;
            this.DNA = model.Item2;
        }

        #endregion

        #region Public Properties

        public WeaponSpikeBallDNA DNA
        {
            get;
            private set;
        }
        public override WeaponPartDNA DNAPart
        {
            get
            {
                return this.DNA;
            }
        }

        #endregion

        #region Public Methods

        public override WeaponDamage CalculateExtraDamage(Point3D positionLocal, double collisionSpeed)
        {
            //TODO: Take damage (wood could take some heavy damage, but the rest of the materials should be pretty tough)



            //TODO: Return some combination of extra kinetic and pierce damage



            return new WeaponDamage();
        }

        #endregion

        #region Private Methods - Model

        //TODO: Have an explicit attach point
        private static Tuple<Model3DGroup, WeaponSpikeBallDNA> GetModel(WeaponSpikeBallDNA dna, WeaponMaterialCache materials)
        {
            WeaponSpikeBallDNA finalDNA = UtilityCore.Clone(dna);
            if (finalDNA.KeyValues == null)
            {
                finalDNA.KeyValues = new SortedList<string, double>();
            }

            Model3DGroup model = null;

            switch (finalDNA.Material)
            {
                case WeaponSpikeBallMaterial.Wood:
                    model = GetModel_WoodIron(dna, finalDNA, materials);
                    break;

                case WeaponSpikeBallMaterial.Bronze_Iron:
                    model = GetModel_BronzeIron(dna, finalDNA, materials);
                    break;

                case WeaponSpikeBallMaterial.Iron_Steel:
                    model = GetModel_IronSteel(dna, finalDNA, materials);
                    break;

                case WeaponSpikeBallMaterial.Composite:
                    model = GetModel_Composite(dna, finalDNA, materials);
                    break;

                case WeaponSpikeBallMaterial.Klinth:
                    model = GetModel_Klinth(dna, finalDNA, materials);
                    break;

                case WeaponSpikeBallMaterial.Moon:
                    model = GetModel_Moon(dna, finalDNA, materials);
                    break;

                default:
                    throw new ApplicationException("Unknown WeaponSpikeBallMaterial: " + finalDNA.Material.ToString());
            }

            return Tuple.Create(model, finalDNA);
        }

        private static Model3DGroup GetModel_WoodIron(WeaponSpikeBallDNA dna, WeaponSpikeBallDNA finalDNA, WeaponMaterialCache materials)
        {
            Model3DGroup retVal = new Model3DGroup();

            Random rand = StaticRandom.GetRandomForThread();
            var from = dna.KeyValues;
            var to = finalDNA.KeyValues;

            double spikeLength = dna.Radius * 1.4d;
            double ballRadius = dna.Radius * 1d;

            double spikeRadius = dna.Radius * .2;

            #region Ball

            System.Windows.Media.Media3D.Material material = materials.Ball_Wood;     // the property get returns a slightly random color

            GeometryModel3D geometry = new GeometryModel3D();

            geometry.Material = material;
            geometry.BackMaterial = material;

            // Create a convex hull out of semi evenly distibuted points
            int numHullPoints = Convert.ToInt32(WeaponDNA.GetKeyValue("numHullPoints", from, to, rand.Next(20, 50)));
            TriangleIndexed[] ball = Math3D.GetConvexHull(Math3D.GetRandomVectors_SphericalShell_EvenDist(numHullPoints, ballRadius, .03, 10).Select(o => o.ToPoint()).ToArray());

            geometry.Geometry = UtilityWPF.GetMeshFromTriangles(ball);

            retVal.Children.Add(geometry);

            #endregion

            // These are placed where the rings are, to push spikes away (so that spikes don't poke through the rings)
            List<Vector3D> staticPoints = new List<Vector3D>();

            #region Rings

            material = materials.Spike_Iron;     // the property get returns a slightly random color

            // 0, 1 or 2 rings.  Higher chance of 0 than 2
            int numRings = Convert.ToInt32(WeaponDNA.GetKeyValue("numRings", from, to, Math.Floor(rand.NextPow(2, 2.3))));

            double[] zs = new double[0];

            switch (numRings)
            {
                case 0:
                    break;

                case 1:
                    zs = new double[] { WeaponDNA.GetKeyValue("ringZ1", from, to, Math1D.GetNearZeroValue(ballRadius * .75)) };
                    break;

                case 2:
                    double z1 = WeaponDNA.GetKeyValue("ringZ1", from, to, Math1D.GetNearZeroValue(ballRadius * .75));
                    double z2 = 0;

                    if (from == null || !from.TryGetValue("ringZ2", out z2))
                    {
                        do
                        {
                            z2 = Math1D.GetNearZeroValue(ballRadius * .75);
                        } while (Math.Abs(z1 - z2) < ballRadius * .4);

                        to.Add("ringZ2", z2);
                    }

                    zs = new double[] { z1, z2 };
                    break;

                default:
                    throw new ApplicationException("Unexpected number of rings: " + numRings.ToString());
            }

            // Build the rings at the z offsets that were calculated above
            for (int cntr = 0; cntr < zs.Length; cntr++)
            {
                retVal.Children.Add(GetModel_WoodIron_Ring_Band(ballRadius, zs[cntr], material, ball, from, to, "ringZ" + cntr.ToString()));

                // Store points at the rings
                double ringRadiusAvg = Math.Sqrt((ballRadius * ballRadius) - (zs[cntr] * zs[cntr]));
                staticPoints.AddRange(Math2D.GetCircle_Cached(7).Select(o => (o.ToVector() * ringRadiusAvg).ToVector3D(zs[cntr])));
            }

            #endregion

            #region Spikes

            Vector3D[] staticPointsArr = staticPoints.Count == 0 ? null : staticPoints.ToArray();
            double[] staticRepulse = staticPoints.Count == 0 ? null : Enumerable.Range(0, staticPoints.Count).Select(o => .005d).ToArray();

            int numSpikes = Convert.ToInt32(WeaponDNA.GetKeyValue("numSpikes", from, to, rand.Next(8, 14)));

            Vector3D[] spikeLocations;
            if (from != null && from.ContainsKey("spikeLoc0X"))
            {
                spikeLocations = new Vector3D[numSpikes];

                for (int cntr = 0; cntr < numSpikes; cntr++)
                {
                    string prefix = "spikeLoc" + cntr.ToString();
                    spikeLocations[cntr] = new Vector3D(to[prefix + "X"], to[prefix + "Y"], to[prefix + "Z"]);
                }
            }
            else
            {
                spikeLocations = Math3D.GetRandomVectors_SphericalShell_EvenDist(numSpikes, ballRadius, .03, 10, null, staticPointsArr, staticRepulse);

                for (int cntr = 0; cntr < numSpikes; cntr++)
                {
                    string prefix = "spikeLoc" + cntr.ToString();
                    to.Add(prefix + "X", spikeLocations[cntr].X);
                    to.Add(prefix + "Y", spikeLocations[cntr].Y);
                    to.Add(prefix + "Z", spikeLocations[cntr].Z);
                }
            }

            for (int cntr = 0; cntr < spikeLocations.Length; cntr++)
            {
                material = materials.Spike_Iron;     // the property get returns a slightly random color

                geometry = new GeometryModel3D();

                geometry.Material = material;
                geometry.BackMaterial = material;

                RotateTransform3D transform = new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRotation(new Vector3D(0, 0, 1), spikeLocations[cntr])));       // the tube builds along z

                List<TubeRingBase> rings = new List<TubeRingBase>();

                double spikeRadiusA = WeaponDNA.GetKeyValue("spikeRad" + cntr.ToString(), from, to, rand.NextPercent(spikeRadius, .33));

                rings.Add(new TubeRingRegularPolygon(0, false, spikeRadiusA, spikeRadiusA, false));
                rings.Add(new TubeRingPoint(WeaponDNA.GetKeyValue("spikeLen" + cntr.ToString(), from, to, rand.NextDouble(spikeLength * .9, spikeLength * 1.1)), false));

                int numSegments = Convert.ToInt32(WeaponDNA.GetKeyValue("spikeSegs" + cntr.ToString(), from, to, rand.Next(3, 6)));
                bool isSoft = Math1D.IsNearZero(WeaponDNA.GetKeyValue("spikeSoft" + cntr.ToString(), from, to, rand.Next(2)));
                geometry.Geometry = UtilityWPF.GetMultiRingedTube(numSegments, rings, isSoft, false, transform);

                retVal.Children.Add(geometry);
            }

            #endregion

            return retVal;
        }
        private static GeometryModel3D GetModel_WoodIron_Ring_Band(double ballRadius, double z, System.Windows.Media.Media3D.Material material, TriangleIndexed[] ball, SortedList<string, double> from, SortedList<string, double> to, string prefix)
        {
            const double ENLARGE = 1.04d;

            GeometryModel3D retVal = new GeometryModel3D();

            retVal.Material = material;
            retVal.BackMaterial = material;

            double bandHeight = WeaponDNA.GetKeyValue(prefix + "Height", from, to, StaticRandom.NextPercent(ballRadius * .15, .5));
            double bandHeightHalf = bandHeight / 2d;

            // Slice the hull at the top and bottom band z's
            Point3D[] slice1 = Math3D.GetIntersection_Hull_Plane(ball, new Triangle(new Point3D(0, 0, z - bandHeightHalf), new Point3D(1, 0, z - bandHeightHalf), new Point3D(0, 1, z - bandHeightHalf)));
            Point3D[] slice2 = Math3D.GetIntersection_Hull_Plane(ball, new Triangle(new Point3D(0, 0, z + bandHeightHalf), new Point3D(1, 0, z + bandHeightHalf), new Point3D(0, 1, z + bandHeightHalf)));

            // Enlarge those polygons xy, leave z alone
            slice1 = slice1.Select(o => new Point3D(o.X * ENLARGE, o.Y * ENLARGE, o.Z)).ToArray();
            slice2 = slice2.Select(o => new Point3D(o.X * ENLARGE, o.Y * ENLARGE, o.Z)).ToArray();

            // Now turn those two polygons into a 3d hull
            TriangleIndexed[] band = Math3D.GetConvexHull(UtilityCore.Iterate(slice1, slice2).ToArray());

            retVal.Geometry = UtilityWPF.GetMeshFromTriangles(band);

            return retVal;
        }

        private static Model3DGroup GetModel_BronzeIron(WeaponSpikeBallDNA dna, WeaponSpikeBallDNA finalDNA, WeaponMaterialCache materials)
        {
            Model3DGroup retVal = new Model3DGroup();
            var from = dna.KeyValues;
            var to = finalDNA.KeyValues;

            double spikeLength = WeaponDNA.GetKeyValue("spikeLen", from, to, (dna.Radius * 1.1d) * StaticRandom.NextDouble(1d, 1.25d));
            double ballRadius = dna.Radius;

            #region Spikes

            System.Windows.Media.Media3D.Material material = materials.Spike_Iron;     // the property get returns a slightly random color

            GeometryModel3D geometry = new GeometryModel3D();

            geometry.Material = material;
            geometry.BackMaterial = material;

            double[] radii = new double[] { spikeLength, ballRadius * WeaponDNA.GetKeyValue("spikeRadMult", from, to, StaticRandom.NextDouble(.8, .93d)) };
            TriangleIndexed[] triangles = UtilityWPF.GetIcosahedron(radii);

            geometry.Geometry = UtilityWPF.GetMeshFromTriangles_IndependentFaces(triangles);

            retVal.Children.Add(geometry);

            #endregion

            #region Ball

            material = materials.Ball_Bronze;     // the property get returns a slightly random color

            geometry = new GeometryModel3D();

            geometry.Material = material;
            geometry.BackMaterial = material;

            triangles = UtilityWPF.GetIcosahedron(ballRadius, 1);

            geometry.Geometry = UtilityWPF.GetMeshFromTriangles(triangles);

            retVal.Children.Add(geometry);

            #endregion

            return retVal;
        }
        private static Model3DGroup GetModel_IronSteel(WeaponSpikeBallDNA dna, WeaponSpikeBallDNA finalDNA, WeaponMaterialCache materials)
        {
            Model3DGroup retVal = new Model3DGroup();
            var from = dna.KeyValues;
            var to = finalDNA.KeyValues;

            double spikeLength = WeaponDNA.GetKeyValue("spikeLen", from, to, dna.Radius * StaticRandom.NextDouble(1.3d, 1.8d));
            double ballRadius = spikeLength * .6d;

            #region Spikes

            System.Windows.Media.Media3D.Material material = materials.Spike_Steel;     // the property get returns a slightly random color

            GeometryModel3D geometry = new GeometryModel3D();

            geometry.Material = material;
            geometry.BackMaterial = material;

            double[] radii = new double[] { spikeLength, ballRadius * WeaponDNA.GetKeyValue("spikeRadMult", from, to, StaticRandom.NextDouble(.7, .87)) };
            TriangleIndexed[] triangles = UtilityWPF.GetIcosahedron(radii);

            geometry.Geometry = UtilityWPF.GetMeshFromTriangles_IndependentFaces(triangles);

            retVal.Children.Add(geometry);

            #endregion

            #region Ball

            material = materials.Ball_Iron;     // the property get returns a slightly random color

            geometry = new GeometryModel3D();

            geometry.Material = material;
            geometry.BackMaterial = material;

            triangles = UtilityWPF.GetIcosahedron(ballRadius, 1);

            geometry.Geometry = UtilityWPF.GetMeshFromTriangles_IndependentFaces(triangles);

            retVal.Children.Add(geometry);

            #endregion

            return retVal;
        }

        private static Model3DGroup GetModel_Composite(WeaponSpikeBallDNA dna, WeaponSpikeBallDNA finalDNA, WeaponMaterialCache materials)
        {
            Model3DGroup retVal = new Model3DGroup();

            Random rand = StaticRandom.GetRandomForThread();

            var from = dna.KeyValues;
            var to = finalDNA.KeyValues;

            double spikeOrthLength = WeaponDNA.GetKeyValue("spikeOrthLength", from, to, rand.NextPercent(dna.Radius * 1.4d, .1));
            double spikeDiagLength = WeaponDNA.GetKeyValue("spikeDiagLength", from, to, rand.NextPercent(dna.Radius * 1.15d, .05));
            double ballRadius = dna.Radius;

            double spikeOrthRadius = WeaponDNA.GetKeyValue("spikeOrthRadius", from, to, rand.NextPercent(dna.Radius * .5, .1));
            double spikeDiagRadius = WeaponDNA.GetKeyValue("spikeDiagRadius", from, to, rand.NextPercent(dna.Radius * .5, .1));

            double ballRadiusDepth = ballRadius * .1;       //this is how far the triangle parts of the ball sink in

            var color = WeaponMaterialCache.GetComposite(dna.MaterialsForCustomizable);
            finalDNA.MaterialsForCustomizable = color.Item4;

            GeometryModel3D geometry;

            #region Ball - outer

            geometry = new GeometryModel3D();

            geometry.Material = color.Item1;
            geometry.BackMaterial = color.Item1;

            Rhombicuboctahedron ball = UtilityWPF.GetRhombicuboctahedron(ballRadius * 2, ballRadius * 2, ballRadius * 2);

            TriangleIndexed[] usedTriangles = UtilityCore.Iterate(
                ball.Squares_Orth.SelectMany(o => o),
                ball.Squares_Diag.SelectMany(o => o),
                GetModel_Composite_SquareSides(ball, ballRadiusDepth * 1.1)       // this builds plates that go toward the center of the ball, because the triangles are indented
                ).ToArray();

            geometry.Geometry = UtilityWPF.GetMeshFromTriangles_IndependentFaces(usedTriangles);

            retVal.Children.Add(geometry);

            #endregion
            #region Ball - inner

            geometry = new GeometryModel3D();

            geometry.Material = color.Item2;
            geometry.BackMaterial = color.Item2;

            // Use the triangles, but suck them in a bit
            geometry.Geometry = UtilityWPF.GetMeshFromTriangles_IndependentFaces(GetModel_Composite_IndentedTriangles(ball, ballRadiusDepth));

            retVal.Children.Add(geometry);

            #endregion

            #region Spikes

            var spikeLocations = UtilityCore.Iterate(ball.SquarePolys_Orth.Select(o => Tuple.Create(true, o)), ball.SquarePolys_Diag.Select(o => Tuple.Create(false, o))).
                Select(o => new { IsOrth = o.Item1, Center = Math3D.GetCenter(o.Item2.Select(p => ball.AllPoints[p])).ToVector() });

            // Put a spike through the center of each square
            foreach (var spikeLocation in spikeLocations)
            {
                geometry = new GeometryModel3D();

                geometry.Material = color.Item3;
                geometry.BackMaterial = color.Item3;

                RotateTransform3D transform = new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRotation(new Vector3D(0, 0, 1), spikeLocation.Center)));       // the tube builds along z

                List<TubeRingBase> rings = new List<TubeRingBase>();

                double spikeLengthActual = spikeLocation.IsOrth ? spikeOrthLength : spikeDiagLength;
                double spikeRadiusActual = spikeLocation.IsOrth ? spikeOrthRadius : spikeDiagRadius;

                rings.Add(new TubeRingRegularPolygon(0, false, spikeRadiusActual, spikeRadiusActual, false));
                rings.Add(new TubeRingPoint(spikeLengthActual, false));

                geometry.Geometry = UtilityWPF.GetMultiRingedTube(9, rings, true, false, transform);

                retVal.Children.Add(geometry);
            }

            #endregion

            return retVal;
        }
        private static TriangleIndexed[] GetModel_Composite_SquareSides(Rhombicuboctahedron ball, double depth)
        {
            List<TriangleIndexed> retVal = new List<TriangleIndexed>();

            foreach (int[] square in UtilityCore.Iterate(ball.SquarePolys_Orth, ball.SquarePolys_Diag))
            {
                Point3D[] initialPoints = square.Select(o => ball.AllPoints[o]).ToArray();

                Vector3D offset = Math2D.GetPolygonNormal(initialPoints, PolygonNormalLength.Unit) * -depth;

                for (int cntr = 0; cntr < square.Length - 1; cntr++)
                {
                    retVal.AddRange(GetModel_Composite_SquareSides_Side(initialPoints[cntr], initialPoints[cntr + 1], offset, depth));
                }

                retVal.AddRange(GetModel_Composite_SquareSides_Side(initialPoints[square.Length - 1], initialPoints[0], offset, depth));
            }

            return retVal.ToArray();
        }
        private static TriangleIndexed[] GetModel_Composite_SquareSides_Side(Point3D point1, Point3D point2, Vector3D offset, double offsetLength)
        {
            // Find the points straight down
            Point3D point1a = point1 + offset;
            Point3D point2a = point2 + offset;

            Vector3D dir12 = (point2a - point1a).ToUnit();

            // Need to suck them toward each other so that the sides of this don't poke through the neighboring square
            //NOTE: This is just making an angle of 45 degrees, which works for squares that are up to 90 degrees relative to each other (these squares are all 45).
            //Since no two neighboring sides are coplanar, you won't notice the overlap
            Point3D point1b = point1a + (dir12 * (offsetLength * .99));       // pulling it in just a bit so the edge of this side doen't intersect with the neighgoring square
            Point3D point2b = point2a - (dir12 * (offsetLength * .99));

            Point3D[] allPoints = new Point3D[] { point1, point2, point2b, point1b };

            return new TriangleIndexed[] { new TriangleIndexed(0, 1, 2, allPoints), new TriangleIndexed(0, 2, 3, allPoints) };
        }
        private static TriangleIndexed[] GetModel_Composite_IndentedTriangles(Rhombicuboctahedron ball, double depth)
        {
            List<Point3D> points = new List<Point3D>();
            List<Tuple<int, int, int>> triangles = new List<Tuple<int, int, int>>();

            int index = 0;

            foreach (TriangleIndexed triangle in ball.Triangles)
            {
                Vector3D offset = triangle.NormalUnit * -depth;

                points.Add(triangle.Point0 + offset);
                points.Add(triangle.Point1 + offset);
                points.Add(triangle.Point2 + offset);

                //TODO: May need to wind these the other direction at times
                triangles.Add(Tuple.Create(index + 0, index + 1, index + 2));

                index += 3;
            }

            Point3D[] allPoints = points.ToArray();

            return triangles.Select(o => new TriangleIndexed(o.Item1, o.Item2, o.Item3, allPoints)).ToArray();
        }

        private static Model3DGroup GetModel_Klinth(WeaponSpikeBallDNA dna, WeaponSpikeBallDNA finalDNA, WeaponMaterialCache materials)
        {
            Model3DGroup retVal = new Model3DGroup();

            var from = dna.KeyValues;
            var to = finalDNA.KeyValues;

            double spikeLength = WeaponDNA.GetKeyValue("spikeLength", from, to, StaticRandom.NextPercent(dna.Radius * 1.1d, .05));
            double ballRadius = dna.Radius;

            double spikeRadius = WeaponDNA.GetKeyValue("spikeRadius", from, to, StaticRandom.NextPercent(dna.Radius * .2, .1));

            var color = WeaponMaterialCache.GetKlinth(dna.MaterialsForCustomizable);
            finalDNA.MaterialsForCustomizable = color.Item3;

            GeometryModel3D geometry;

            #region Ball

            geometry = new GeometryModel3D();

            geometry.Material = color.Item1;
            geometry.BackMaterial = color.Item1;

            Icosidodecahedron ball = UtilityWPF.GetIcosidodecahedron(ballRadius);

            geometry.Geometry = UtilityWPF.GetMeshFromTriangles_IndependentFaces(ball.AllTriangles);

            retVal.Children.Add(geometry);

            #endregion

            #region Spikes

            // Put a spike through the center of each pentagon
            foreach (Vector3D spikeLocation in ball.PentagonPolys.Select(o => Math3D.GetCenter(o.Select(p => ball.AllPoints[p]))))
            {
                geometry = new GeometryModel3D();

                geometry.Material = color.Item2;
                geometry.BackMaterial = color.Item2;

                RotateTransform3D transform = new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRotation(new Vector3D(0, 0, 1), spikeLocation)));       // the tube builds along z

                List<TubeRingBase> rings = new List<TubeRingBase>();

                double spikeLengthMid = spikeLength * .8d;

                rings.Add(new TubeRingRegularPolygon(0, false, spikeRadius, spikeRadius, false));
                rings.Add(new TubeRingRegularPolygon(spikeLengthMid, false, spikeRadius, spikeRadius, false));
                rings.Add(new TubeRingDome(spikeLength - spikeLengthMid, false, 3));

                geometry.Geometry = UtilityWPF.GetMultiRingedTube(9, rings, false, false, transform);

                retVal.Children.Add(geometry);
            }

            #endregion

            return retVal;
        }

        private static Model3DGroup GetModel_Moon(WeaponSpikeBallDNA dna, WeaponSpikeBallDNA finalDNA, WeaponMaterialCache materials)
        {
            //TODO: Animate the spikes (shafts of light) by choosing one as the independently rotated spike, and the others run through one step of Math3D.GetRandomVectors_SphericalShell_EvenDist
            //TODO: Also animate each spike's radius and length (or at least length)
            //TODO: Also animate each spike's color opacity

            Model3DGroup retVal = new Model3DGroup();

            double spikeLength = dna.Radius * 2d;
            double ballRadius = dna.Radius * 1d;

            double spikeRadius = dna.Radius * .033;

            System.Windows.Media.Media3D.Material material;
            GeometryModel3D geometry;

            #region Ball

            material = materials.Ball_Moon;     // the property get returns a slightly random color

            geometry = new GeometryModel3D();

            geometry.Material = material;
            geometry.BackMaterial = material;

            geometry.Geometry = UtilityWPF.GetSphere_LatLon(5, ballRadius);

            retVal.Children.Add(geometry);

            #endregion

            #region Spikes

            foreach (Vector3D spikeLocation in Math3D.GetRandomVectors_SphericalShell_EvenDist(30, ballRadius, .03, 100))
            {
                material = materials.Spike_Moon;

                geometry = new GeometryModel3D();

                geometry.Material = material;
                geometry.BackMaterial = material;

                RotateTransform3D transform = new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRotation(new Vector3D(0, 0, 1), spikeLocation)));       // the tube builds along z

                List<TubeRingBase> rings = new List<TubeRingBase>();

                double spikeLengthMid = spikeLength * .75d;

                rings.Add(new TubeRingRegularPolygon(0, false, spikeRadius, spikeRadius, false));
                rings.Add(new TubeRingRegularPolygon(spikeLengthMid, false, spikeRadius, spikeRadius, false));
                rings.Add(new TubeRingDome(spikeLength - spikeLengthMid, false, 2));

                geometry.Geometry = UtilityWPF.GetMultiRingedTube(9, rings, true, false, transform);

                retVal.Children.Add(geometry);
            }

            #endregion

            return retVal;
        }

        #endregion
    }

    #region Class: WeaponSpikeBallDNA

    public class WeaponSpikeBallDNA : WeaponPartDNA
    {
        public WeaponSpikeBallMaterial Material { get; set; }

        public double Radius { get; set; }

        #region Public Methods

        /// <summary>
        /// This returns a random ball that will fit well with the handle passed in
        /// </summary>
        public static WeaponSpikeBallDNA GetRandomDNA(WeaponHandleDNA handle)
        {
            WeaponSpikeBallDNA retVal = new WeaponSpikeBallDNA();

            Random rand = StaticRandom.GetRandomForThread();

            // Radius
            retVal.Radius = GetRandomRadius(handle.Radius, handle.Length);

            #region Material

            // Choose a ball material that goes with the handle's material
            //NOTE: This is assuming that the ball is an appropriate radius for the handle it will be attached to.
            //A large soft wood could safely handle a very small ball, etc

            WeaponSpikeBallMaterial[] ballMaterials = null;

            switch (handle.HandleMaterial)
            {
                case WeaponHandleMaterial.Soft_Wood:
                    ballMaterials = new[] { WeaponSpikeBallMaterial.Wood, WeaponSpikeBallMaterial.Composite };
                    break;

                case WeaponHandleMaterial.Hard_Wood:
                    ballMaterials = new[] { WeaponSpikeBallMaterial.Wood, WeaponSpikeBallMaterial.Bronze_Iron, WeaponSpikeBallMaterial.Iron_Steel, WeaponSpikeBallMaterial.Composite, WeaponSpikeBallMaterial.Klinth };
                    break;

                case WeaponHandleMaterial.Bronze:
                case WeaponHandleMaterial.Iron:
                case WeaponHandleMaterial.Steel:
                    ballMaterials = new[] { WeaponSpikeBallMaterial.Bronze_Iron, WeaponSpikeBallMaterial.Iron_Steel, WeaponSpikeBallMaterial.Moon };
                    break;

                case WeaponHandleMaterial.Composite:
                    ballMaterials = new[] { WeaponSpikeBallMaterial.Bronze_Iron, WeaponSpikeBallMaterial.Iron_Steel, WeaponSpikeBallMaterial.Composite, WeaponSpikeBallMaterial.Klinth, WeaponSpikeBallMaterial.Moon };
                    break;

                case WeaponHandleMaterial.Klinth:
                    ballMaterials = new[] { WeaponSpikeBallMaterial.Bronze_Iron, WeaponSpikeBallMaterial.Iron_Steel, WeaponSpikeBallMaterial.Klinth, WeaponSpikeBallMaterial.Moon };
                    break;

                case WeaponHandleMaterial.Moon:
                    ballMaterials = new[] { WeaponSpikeBallMaterial.Iron_Steel, WeaponSpikeBallMaterial.Klinth, WeaponSpikeBallMaterial.Moon };
                    break;

                default:
                    throw new ApplicationException("Unknown WeaponHandleMaterial: " + handle.HandleMaterial.ToString());
            }

            // Choose one from the filtered list
            retVal.Material = rand.NextItem(ballMaterials);

            #endregion

            #region Color

            ColorHSV? basedOn = null;
            if (handle.MaterialsForCustomizable != null && handle.MaterialsForCustomizable.Length > 0 && !string.IsNullOrEmpty(handle.MaterialsForCustomizable[0].DiffuseColor))
            {
                basedOn = UtilityWPF.ColorFromHex(handle.MaterialsForCustomizable[0].DiffuseColor).ToHSV();
            }

            switch (retVal.Material)
            {
                case WeaponSpikeBallMaterial.Composite:
                    retVal.MaterialsForCustomizable = WeaponHandleDNA.GetRandomMaterials_Composite(null, basedOn);
                    break;

                case WeaponSpikeBallMaterial.Klinth:
                    retVal.MaterialsForCustomizable = WeaponHandleDNA.GetRandomMaterials_Klinth(null, basedOn);
                    break;
            }

            #endregion

            return retVal;
        }
        /// <summary>
        /// This returns a random ball, with some optional fixed values
        /// </summary>
        public static WeaponSpikeBallDNA GetRandomDNA(WeaponSpikeBallMaterial? material = null, double? radius = null)
        {
            WeaponSpikeBallDNA retVal = new WeaponSpikeBallDNA();

            Random rand = StaticRandom.GetRandomForThread();

            // Radius
            if (radius != null)
            {
                retVal.Radius = radius.Value;
            }
            else
            {
                retVal.Radius = rand.NextDouble(.2, .5);
            }

            // Material
            if (material == null)
            {
                retVal.Material = UtilityCore.GetRandomEnum<WeaponSpikeBallMaterial>();
            }
            else
            {
                retVal.Material = material.Value;
            }

            return retVal;
        }

        /// <summary>
        /// This returns a value that is reasonable for the length of the handle
        /// </summary>
        public static double GetRandomRadius(double handleRadius, double handleLength)
        {
            //const double AVGLENGTH = 2.1;

            double min = handleRadius * 2d;
            double max = handleRadius * 2.6d;

            //// Adjust min/max based on the length of the handle (assuming
            ////TODO: Don't be so linear
            //double distPercent = (handleLength) / AVGLENGTH;

            //min *= distPercent;
            //max *= distPercent;

            return StaticRandom.NextDouble(min, max);
        }

        #endregion
    }

    #endregion

    #region Enum: WeaponSpikeBallMaterial

    public enum WeaponSpikeBallMaterial
    {
        /// <summary>
        /// A hard wood ball with:
        ///     1 or 2 iron bands
        ///     A few iron spikes (semi evenly distributed)
        /// </summary>
        /// <remarks>
        /// Light, decent damage, breaks easily
        /// </remarks>
        Wood,

        /// <summary>
        /// Bronze ball, Iron studs
        /// </summary>
        /// <remarks>
        /// Heavy, medium damage
        /// </remarks>
        Bronze_Iron,
        /// <summary>
        /// Iron ball, Steel spikes
        /// </summary>
        /// <remarks>
        /// Heavy, high damage
        /// </remarks>
        Iron_Steel,

        /// <summary>
        /// Composite ball, Combosite bands
        /// (smooth round)
        /// </summary>
        /// <remarks>
        /// Fairly light, decent damage for how light it is
        /// </remarks>
        Composite,
        /// <summary>
        /// Klinth ball, Klinth spikes
        /// </summary>
        /// <remarks>
        /// Med weight, Med damage
        /// </remarks>
        Klinth,

        /// <summary>
        /// Blue jewel ball (smooth), White light spikes (high emissive)
        /// </summary>
        /// <remarks>
        /// Heavy, special damage
        /// </remarks>
        Moon,
    }

    #endregion
}
