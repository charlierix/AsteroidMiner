using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.Arcanorum
{
    public class WeaponHandle : WeaponPart
    {
        #region Declaration Section

        private readonly WeaponMaterialCache _materials;

        #endregion

        #region Constructor

        public WeaponHandle(WeaponMaterialCache materials, WeaponHandleDNA dna)
        {
            _materials = materials;

            var model = GetModel(dna, materials);

            this.Model = model.Item1;
            this.DNA = model.Item2;
        }

        #endregion

        #region Public Properties

        public WeaponHandleDNA DNA
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
            //TODO: Take damage.  If it's just a handle, it won't take much damage.  But if there is a heavy weight attached, the handle should take a lot of damage (if it's wood)


            // The handle doesn't have any kind of special damage.  Maybe some of the special materials might (demon handle)
            return new WeaponDamage();
        }

        #endregion

        #region Private Methods - Model

        private static Tuple<Model3DGroup, WeaponHandleDNA> GetModel(WeaponHandleDNA dna, WeaponMaterialCache materials)
        {
            WeaponHandleDNA finalDNA = UtilityCore.Clone(dna);
            if (finalDNA.KeyValues == null)
            {
                finalDNA.KeyValues = new SortedList<string, double>();
            }

            Model3DGroup model = null;

            // If there is a mismatch between desired material and type, prefer material
            finalDNA.HandleType = GetHandleType(dna.HandleMaterial, dna.HandleType);

            // Build the handle
            switch (finalDNA.HandleType)
            {
                case WeaponHandleType.Rod:
                    model = GetModel_Rod(dna, finalDNA, materials);
                    break;

                case WeaponHandleType.Rope:
                    throw new ApplicationException("Rope currently isn't supported");

                default:
                    throw new ApplicationException("Unknown WeaponHandleType: " + finalDNA.HandleType.ToString());
            }

            // Exit Function
            return Tuple.Create(model, finalDNA);

            #region WRAITH
            //retVal.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex(this.DiffuseColor))));
            //retVal.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex(this.SpecularColor)), this.SpecularPower.Value));
            //retVal.Children.Add(new EmissiveMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex(this.EmissiveColor))));


            //case WeaponHandleMaterial.Wraith:
            //    #region Wraith

            //    retVal.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("6048573E"))));
            //    retVal.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("202C1D33")), 50d));
            //    retVal.Children.Add(new EmissiveMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("1021222B"))));

            //    #endregion
            //    break;




            //if (useDebugVisual)
            //{
            //    geometry.Geometry = UtilityWPF.GetLine(new Point3D(-(handle.Length / 2d), 0, 0), new Point3D(handle.Length / 2d, 0, 0), handle.Radius * 2d);
            //}
            #endregion
        }

        private static Model3DGroup GetModel_Rod(WeaponHandleDNA dna, WeaponHandleDNA finalDNA, WeaponMaterialCache materials)
        {
            Model3DGroup retVal = new Model3DGroup();

            switch (dna.HandleMaterial)
            {
                case WeaponHandleMaterial.Soft_Wood:
                case WeaponHandleMaterial.Hard_Wood:
                    GetModel_Rod_Wood(retVal, dna, finalDNA, materials);
                    break;

                case WeaponHandleMaterial.Bronze:
                case WeaponHandleMaterial.Iron:
                case WeaponHandleMaterial.Steel:
                    GetModel_Rod_Metal(retVal, dna, finalDNA, materials);
                    break;

                case WeaponHandleMaterial.Composite:
                    GetModel_Rod_Composite(retVal, dna, finalDNA, materials);
                    break;

                case WeaponHandleMaterial.Klinth:
                    GetModel_Rod_Klinth(retVal, dna, finalDNA, materials);
                    break;

                case WeaponHandleMaterial.Moon:
                    GetModel_Rod_Moon(retVal, dna, finalDNA, materials);
                    break;

                default:
                    throw new ApplicationException("Unknown WeaponHandleMaterial: " + dna.HandleMaterial.ToString());
            }

            // Exit Function
            return retVal;
        }

        //TODO: random chance of randomly placing metal fittings on the handle
        private static void GetModel_Rod_Wood(Model3DGroup geometries, WeaponHandleDNA dna, WeaponHandleDNA finalDNA, WeaponMaterialCache materials)
        {
            const double PERCENT = 1;

            Random rand = StaticRandom.GetRandomForThread();
            var from = dna.KeyValues;
            var to = finalDNA.KeyValues;

            GeometryModel3D geometry = new GeometryModel3D();

            #region Material

            switch (dna.HandleMaterial)
            {
                case WeaponHandleMaterial.Soft_Wood:
                    geometry.Material = materials.Handle_SoftWood;
                    break;

                case WeaponHandleMaterial.Hard_Wood:
                    geometry.Material = materials.Handle_HardWood;
                    break;

                default:
                    throw new ApplicationException("Unexpected WeaponHandleMaterial: " + dna.HandleMaterial.ToString());
            }

            geometry.BackMaterial = geometry.Material;

            #endregion

            #region Tube

            double maxX1 = WeaponDNA.GetKeyValue("maxX1", from, to, rand.NextDouble(.45, .7));
            double maxX2 = WeaponDNA.GetKeyValue("maxX2", from, to, rand.NextDouble(.45, .7));

            double maxY1 = WeaponDNA.GetKeyValue("maxY1", from, to, rand.NextDouble(.85, 1.05));
            double maxY2 = WeaponDNA.GetKeyValue("maxY2", from, to, rand.NextDouble(.85, 1.05));

            List<TubeRingBase> rings = new List<TubeRingBase>();
            rings.Add(new TubeRingRegularPolygon(0, false, maxX1 * .45, maxY1 * .75, true));
            rings.Add(new TubeRingRegularPolygon(WeaponDNA.GetKeyValue("ring1", from, to, rand.NextPercent(.5, PERCENT)), false, maxX1 * .5, maxY1 * 1, false));
            rings.Add(new TubeRingRegularPolygon(WeaponDNA.GetKeyValue("ring2", from, to, rand.NextPercent(2, PERCENT)), false, maxX1 * .4, maxY1 * .8, false));

            rings.Add(new TubeRingRegularPolygon(WeaponDNA.GetKeyValue("ring3", from, to, rand.NextPercent(5, PERCENT)), false, Math1D.Avg(maxX1, maxX2) * .35, Math1D.Avg(maxY1, maxY2) * .75, false));

            rings.Add(new TubeRingRegularPolygon(WeaponDNA.GetKeyValue("ring4", from, to, rand.NextPercent(5, PERCENT)), false, maxX2 * .4, maxY2 * .8, false));
            rings.Add(new TubeRingRegularPolygon(WeaponDNA.GetKeyValue("ring5", from, to, rand.NextPercent(2, PERCENT)), false, maxX2 * .5, maxY2 * 1, false));
            rings.Add(new TubeRingRegularPolygon(WeaponDNA.GetKeyValue("ring6", from, to, rand.NextPercent(.5, PERCENT)), false, maxX2 * .45, maxY2 * .75, true));

            rings = TubeRingBase.FitNewSize(rings, dna.Radius * Math.Max(maxX1, maxX2), dna.Radius * Math.Max(maxY1, maxY2), dna.Length);        // multiplying x by maxX, because the rings were defined with x maxing at maxX, and y maxing at 1

            geometry.Geometry = UtilityWPF.GetMultiRingedTube(10, rings, true, true, new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90)));      // the tube builds along z, but this class wants along x

            #endregion

            geometries.Children.Add(geometry);
        }

        private static void GetModel_Rod_Metal(Model3DGroup geometries, WeaponHandleDNA dna, WeaponHandleDNA finalDNA, WeaponMaterialCache materials)
        {
            Random rand = StaticRandom.GetRandomForThread();
            var from = dna.KeyValues;
            var to = finalDNA.KeyValues;

            #region Materials

            System.Windows.Media.Media3D.Material material1, material2;

            switch (dna.HandleMaterial)
            {
                case WeaponHandleMaterial.Bronze:
                    material1 = materials.Handle_Bronze;       // the property get returns a slightly random color
                    material2 = materials.Handle_Bronze;
                    break;

                case WeaponHandleMaterial.Iron:
                    material1 = materials.Handle_Iron;
                    material2 = materials.Handle_Iron;
                    break;

                case WeaponHandleMaterial.Steel:
                    material1 = materials.Handle_Steel;
                    material2 = materials.Handle_Steel;
                    break;

                default:
                    throw new ApplicationException("Unexpected WeaponHandleMaterial: " + dna.HandleMaterial.ToString());
            }

            #endregion

            #region Ends

            double capRadius = dna.Radius * 1.1;

            List<TubeRingBase> rings = new List<TubeRingBase>();
            rings.Add(new TubeRingRegularPolygon(0, false, capRadius * .75, capRadius * .75, true));
            rings.Add(new TubeRingRegularPolygon(capRadius * .2, false, capRadius, capRadius, false));
            rings.Add(new TubeRingRegularPolygon(WeaponDNA.GetKeyValue("capWidth", from, to, capRadius * (1d + rand.NextPow(7d, 2.2d, false))), false, capRadius, capRadius, false));
            rings.Add(new TubeRingRegularPolygon(capRadius * .8, false, capRadius * .75, capRadius * .75, true));

            double capHeight = TubeRingBase.GetTotalHeight(rings);
            double halfLength = dna.Length / 2d;
            double halfCap = capHeight / 2d;

            // Cap 1
            GeometryModel3D geometry = new GeometryModel3D();

            geometry.Material = material1;
            geometry.BackMaterial = material1;

            Transform3DGroup transform = new Transform3DGroup();
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90)));      // the tube builds along z, but this class wants along x
            transform.Children.Add(new TranslateTransform3D(-halfLength + halfCap, 0, 0));

            geometry.Geometry = UtilityWPF.GetMultiRingedTube(6, rings, false, true, transform);

            geometries.Children.Add(geometry);

            // Cap 2
            geometry = new GeometryModel3D();

            geometry.Material = material1;
            geometry.BackMaterial = material1;

            transform = new Transform3DGroup();
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), -90)));      // the tube builds along z, but this class wants along x
            transform.Children.Add(new TranslateTransform3D(halfLength - halfCap, 0, 0));

            geometry.Geometry = UtilityWPF.GetMultiRingedTube(6, rings, false, true, transform);

            geometries.Children.Add(geometry);

            #endregion

            #region Shaft

            geometry = new GeometryModel3D();

            geometry.Material = material2;
            geometry.BackMaterial = material2;

            rings = new List<TubeRingBase>();

            rings.Add(new TubeRingRegularPolygon(0, false, dna.Radius * .8, dna.Radius * .8, true));
            rings.Add(new TubeRingRegularPolygon(dna.Length - capHeight, false, dna.Radius * .8, dna.Radius * .8, true));

            transform = new Transform3DGroup();
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90)));      // the tube builds along z, but this class wants along x
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 45)));      // make the bar impact along the edge instead of the flat part

            geometry.Geometry = UtilityWPF.GetMultiRingedTube(4, rings, false, true, transform);

            geometries.Children.Add(geometry);

            #endregion
        }

        private static void GetModel_Rod_Composite(Model3DGroup geometries, WeaponHandleDNA dna, WeaponHandleDNA finalDNA, WeaponMaterialCache materials)
        {
            Random rand = StaticRandom.GetRandomForThread();

            finalDNA.MaterialsForCustomizable = WeaponHandleDNA.GetRandomMaterials_Composite(dna.MaterialsForCustomizable);
            var from = dna.KeyValues;
            var to = finalDNA.KeyValues;

            double halfLength = dna.Length / 2d;
            double halfBeamThickness = dna.Radius / 8d;
            double halfCoreThickness = (dna.Radius * .66d) / 2d;
            double washerRadius = dna.Radius * 1.1;

            double washerThickness1 = WeaponDNA.GetKeyValue("washerThickness1", from, to, dna.Length * rand.NextPercent(.015d, .5d));
            double washerThickness2 = WeaponDNA.GetKeyValue("washerThickness2", from, to, dna.Length * rand.NextPercent(.15d, .5d));
            double washerOffset = WeaponDNA.GetKeyValue("washerOffset", from, to, rand.NextPercent(.05d, .5d));

            var material = GetModel_Rod_CompositeSprtMaterial(finalDNA.MaterialsForCustomizable[0]);

            //NOTE: The beam/core dimensions shouldn't be randomized.  This should look like a manufactured, almost mass produced product
            #region Beams

            // Beam1
            GeometryModel3D geometry = new GeometryModel3D();

            geometry.Material = material;
            geometry.BackMaterial = material;

            geometry.Geometry = UtilityWPF.GetCube_IndependentFaces(new Point3D(-halfLength, -dna.Radius, -halfBeamThickness), new Point3D(halfLength, dna.Radius, halfBeamThickness));

            geometries.Children.Add(geometry);

            // Beam2
            geometry = new GeometryModel3D();

            geometry.Material = material;
            geometry.BackMaterial = material;

            geometry.Geometry = UtilityWPF.GetCube_IndependentFaces(new Point3D(-halfLength, -halfBeamThickness, -dna.Radius), new Point3D(halfLength, halfBeamThickness, dna.Radius));

            geometries.Children.Add(geometry);

            #endregion
            #region Core

            material = GetModel_Rod_CompositeSprtMaterial(finalDNA.MaterialsForCustomizable[1]);

            geometry = new GeometryModel3D();

            geometry.Material = material;
            geometry.BackMaterial = material;

            geometry.Geometry = UtilityWPF.GetCube_IndependentFaces(new Point3D(-halfLength, -halfCoreThickness, -halfCoreThickness), new Point3D(halfLength, halfCoreThickness, halfCoreThickness));

            geometry.Transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 45));

            geometries.Children.Add(geometry);

            #endregion
            #region Washers

            material = GetModel_Rod_CompositeSprtMaterial(finalDNA.MaterialsForCustomizable[2]);

            var locations = new Tuple<double, double>[] 
            {
                Tuple.Create(0d, washerThickness1),
                Tuple.Create(washerOffset, washerThickness1),
                Tuple.Create(.5d, washerThickness2),
                Tuple.Create(1d - washerOffset, washerThickness1),
                Tuple.Create(1d, washerThickness1)
            };

            foreach (var loc in locations)
            {
                geometry = new GeometryModel3D();

                geometry.Material = material;
                geometry.BackMaterial = material;

                geometry.Geometry = UtilityWPF.GetCylinder_AlongX(8, washerRadius, loc.Item2);

                geometry.Transform = new TranslateTransform3D(-halfLength + (dna.Length * loc.Item1), 0, 0);

                geometries.Children.Add(geometry);
            }

            #endregion
        }
        private static System.Windows.Media.Media3D.Material GetModel_Rod_CompositeSprtMaterial(MaterialDefinition definition)
        {
            MaterialGroup retVal = new MaterialGroup();

            Color color1 = UtilityWPF.ColorFromHex(definition.DiffuseColor);

            retVal.Children.Add(new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(color1.R, color1.G, color1.B))));     // making sure there is no semitransparency
            retVal.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("80808080")), 50));

            return retVal;
        }

        private static void GetModel_Rod_Klinth(Model3DGroup geometries, WeaponHandleDNA dna, WeaponHandleDNA finalDNA, WeaponMaterialCache materials)
        {
            GeometryModel3D geometry = new GeometryModel3D();

            var color = WeaponMaterialCache.GetKlinth(dna.MaterialsForCustomizable);
            finalDNA.MaterialsForCustomizable = color.Item3;

            geometry.Material = color.Item1;
            geometry.BackMaterial = color.Item1;

            //NOTE: The dimensions shouldn't be randomized.  This should look like a manufactured, almost mass produced product.
            // Also, being a crystal, it needs to appear solid

            List<TubeRingBase> rings = new List<TubeRingBase>();

            rings.Add(new TubeRingPoint(0, false));
            rings.Add(new TubeRingRegularPolygon(.2, false, .75, .75, false));
            rings.Add(new TubeRingRegularPolygon(.3, false, 1, 1, false));
            rings.Add(new TubeRingRegularPolygon(.5, false, .9, .9, false));
            rings.Add(new TubeRingRegularPolygon(1, false, .8, .8, false));
            rings.Add(new TubeRingRegularPolygon(15, false, .8, .8, false));
            rings.Add(new TubeRingRegularPolygon(1, false, .9, .9, false));
            rings.Add(new TubeRingRegularPolygon(.5, false, 1, 1, false));
            rings.Add(new TubeRingRegularPolygon(.3, false, .75, .75, false));
            rings.Add(new TubeRingPoint(.2, false));

            rings = TubeRingBase.FitNewSize(rings, dna.Radius, dna.Radius, dna.Length);

            geometry.Geometry = UtilityWPF.GetMultiRingedTube(5, rings, false, true, new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90)));      // the tube builds along z, but this class wants along x

            geometries.Children.Add(geometry);
        }

        private static void GetModel_Rod_Moon(Model3DGroup geometries, WeaponHandleDNA dna, WeaponHandleDNA finalDNA, WeaponMaterialCache materials)
        {
            const double PERCENT = 1;

            Random rand = StaticRandom.GetRandomForThread();
            var from = dna.KeyValues;
            var to = finalDNA.KeyValues;

            #region Shaft

            GeometryModel3D shaft = new GeometryModel3D();

            shaft.Material = materials.Handle_Moon;
            shaft.BackMaterial = shaft.Material;

            double maxRad1 = WeaponDNA.GetKeyValue("maxRad1", from, to, rand.NextDouble(.7, 1.02));
            double maxRad2 = WeaponDNA.GetKeyValue("maxRad2", from, to, rand.NextDouble(.7, 1.02));
            double maxRad12 = Math.Max(maxRad1, maxRad2);       // this is used in several places

            List<TubeRingBase> rings = new List<TubeRingBase>();

            rings.Add(new TubeRingRegularPolygon(0, false, maxRad1 * .4, maxRad1 * .4, true));
            rings.Add(new TubeRingRegularPolygon(WeaponDNA.GetKeyValue("tube1", from, to, rand.NextPercent(.25, PERCENT)), false, maxRad1 * .8, maxRad1 * .8, false));
            rings.Add(new TubeRingRegularPolygon(WeaponDNA.GetKeyValue("tube2", from, to, rand.NextPercent(.3, PERCENT)), false, maxRad1 * .85, maxRad1 * .85, false));
            rings.Add(new TubeRingRegularPolygon(WeaponDNA.GetKeyValue("tube3", from, to, rand.NextPercent(.75, PERCENT)), false, maxRad1 * .6, maxRad1 * .6, false));

            rings.Add(new TubeRingRegularPolygon(WeaponDNA.GetKeyValue("tube4", from, to, rand.NextPercent(20, PERCENT)), false, maxRad2 * .8, maxRad2 * .8, false));
            rings.Add(new TubeRingRegularPolygon(WeaponDNA.GetKeyValue("tube5", from, to, rand.NextPercent(1, PERCENT)), false, maxRad2 * .9, maxRad2 * .9, false));
            rings.Add(new TubeRingRegularPolygon(WeaponDNA.GetKeyValue("tube6", from, to, rand.NextPercent(1, PERCENT)), false, maxRad2 * 1, maxRad2 * 1, false));
            rings.Add(new TubeRingDome(WeaponDNA.GetKeyValue("tube7", from, to, rand.NextPercent(2.5, PERCENT)), false, 4));

            rings = TubeRingBase.FitNewSize(rings, maxRad12 * dna.Radius, maxRad12 * dna.Radius, dna.Length);

            shaft.Geometry = UtilityWPF.GetMultiRingedTube(10, rings, true, true, new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), -90)));      // the tube builds along z, but this class wants along x

            #endregion

            // Number of gems
            int numIfNew = 0;
            if (rand.NextDouble() > .66d)       // only 33% will get gems
            {
                // Of the handles with gems, only 5% will get 2
                numIfNew = rand.NextDouble() > .95 ? 2 : 1;
            }

            int numGems = Convert.ToInt32(WeaponDNA.GetKeyValue("numGems", from, to, numIfNew));

            if (numGems == 0)
            {
                geometries.Children.Add(shaft);
                return;
            }

            #region Gems

            List<double> percents = new List<double>();

            for (int cntr = 0; cntr < numGems; cntr++)
            {
                string keyPrefix = "gem" + cntr.ToString();

                // Get a placement for this gem
                double percentIfNew = 0;
                do
                {
                    percentIfNew = rand.NextDouble(.15, .85);

                    if (percents.Count == 0)
                    {
                        break;
                    }
                } while (percents.Any(o => Math.Abs(percentIfNew - o) < .15));

                double percent = WeaponDNA.GetKeyValue(keyPrefix + "Percent", from, to, percentIfNew);

                percents.Add(percent);

                // Gem
                GeometryModel3D gem = new GeometryModel3D();

                gem.Material = materials.Handle_MoonGem;
                gem.BackMaterial = gem.Material;

                double width = WeaponDNA.GetKeyValue(keyPrefix + "Width", from, to, rand.NextDouble(maxRad12 * 1d, maxRad12 * 1.4d));

                gem.Geometry = UtilityWPF.GetSphere_LatLon(5, dna.Radius * width);
                Point3D position = new Point3D((dna.Length * percent) - (dna.Length / 2d), 0, 0);
                gem.Transform = new TranslateTransform3D(position.ToVector());

                // Light
                PointLight pointLight = new PointLight(materials.Handle_MoonGemLight, position);
                UtilityWPF.SetAttenuation(pointLight, dna.Radius * 120d, .1d);

                geometries.Children.Add(pointLight);
                geometries.Children.Add(gem);
            }

            // Adding this after so that you don't see the shaft through the gems
            geometries.Children.Add(shaft);

            #endregion
        }

        #endregion
        #region Private Methods

        /// <summary>
        /// Some types are only valid for certain materials.  So rather than throw an error, just change the type
        /// </summary>
        private static WeaponHandleType GetHandleType(WeaponHandleMaterial material, WeaponHandleType requestedType)
        {
            switch (material)
            {
                case WeaponHandleMaterial.Soft_Wood:
                case WeaponHandleMaterial.Hard_Wood:
                    return WeaponHandleType.Rod;

                //case WeaponHandleMaterial.CheapRope:
                //case WeaponHandleMaterial.HempRope:
                //case WeaponHandleMaterial.Wraith:
                //    return WeaponHandleType.Rope;

                default:
                    return requestedType;
            }
        }

        #endregion
    }

    #region Class: WeaponHandleDNA

    // Multiple handles could be attached together
    //TODO:
    //      Durability, % damaged
    //      The details of the graphic are randomly generated at build time.  Need to store those details so that a particular handle can be perfectly recreated (useful for saving/loading, moving to/from world/inventory)
    public class WeaponHandleDNA : WeaponPartDNA
    {
        public WeaponHandleMaterial HandleMaterial { get; set; }

        public WeaponHandleType HandleType { get; set; }

        /// <summary>
        /// This is the intended attach point.  A weapon can have multiple handles, so this is more of a suggestion than a hard rule
        /// </summary>
        /// <remarks>
        /// 0 is 0% of handle length (at -X)
        /// 1 is 100% of handle length (at +X)
        /// </remarks>
        public double AttachPointPercent { get; set; }

        //public bool IsExtendable { get; set; }

        public double Length { get; set; }
        //public double MaxLength { get; set; }

        public double Radius { get; set; }

        #region Public Methods

        public static WeaponHandleDNA GetRandomDNA(WeaponHandleMaterial? material = null, WeaponHandleType? type = null, bool? isDouble = null, double? length = null, double? radius = null)
        {
            const double DOUBLE_END = .2d;
            const double DOUBLE_CENTER = .05d;       // this is only half of the center

            WeaponHandleDNA retVal = new WeaponHandleDNA();

            Random rand = StaticRandom.GetRandomForThread();

            bool isDoubleActual = isDouble ?? (rand.Next(2) == 0);

            #region Material

            if (material == null)
            {
                retVal.HandleMaterial = UtilityCore.GetRandomEnum<WeaponHandleMaterial>();
            }
            else
            {
                retVal.HandleMaterial = material.Value;
            }

            #endregion

            //TODO: Support rope
            retVal.HandleType = WeaponHandleType.Rod;

            #region Length

            if (length == null)
            {
                //double lengthActual = rand.NextDouble(1.5d, 3.5d);
                double lengthActual = rand.NextDouble(1.5d, 2.8d);
                if (isDoubleActual)
                {
                    lengthActual *= 2d;
                }

                retVal.Length = lengthActual;
            }
            else
            {
                retVal.Length = length.Value;
            }

            #endregion
            #region Radius

            if (radius == null)
            {
                //double radiusActual = rand.NextDouble(.03d, .17d);
                double radiusActual = rand.NextDouble(.03d, .12d);
                if (isDoubleActual)
                {
                    radiusActual *= 2d;
                }

                retVal.Radius = radiusActual;
            }
            else
            {
                retVal.Radius = radius.Value;
            }

            #endregion
            #region AttachPoint

            if (isDoubleActual)
            {
                // Since the center of mass is in the middle, the attach point can't be in the middle.
                // Both ends are out as well.
                // So, choose one of the areas that are stars
                //  |----|********|--------|********|----|

                double randMaxValue = .5d - DOUBLE_END - DOUBLE_CENTER;
                double randValue = rand.NextDouble(randMaxValue);
                double half = DOUBLE_CENTER + randValue;

                if (rand.NextBool())
                {
                    // left side
                    retVal.AttachPointPercent = .5d - half;
                }
                else
                {
                    // right side
                    retVal.AttachPointPercent = .5d + half;
                }
            }
            else
            {
                // Choose one of the ends
                retVal.AttachPointPercent = rand.NextBool() ? 0d : 1d;
            }

            #endregion

            #region Color

            switch (retVal.HandleMaterial)
            {
                case WeaponHandleMaterial.Composite:
                    retVal.MaterialsForCustomizable = GetRandomMaterials_Composite();
                    break;

                case WeaponHandleMaterial.Klinth:
                    retVal.MaterialsForCustomizable = GetRandomMaterials_Klinth();
                    break;
            }

            #endregion

            return retVal;
        }

        /// <summary>
        /// Creates or adds to a set of colors to be used by composite weapons
        /// </summary>
        /// <param name="existing">This is a material definition that may already exist, or partially filled out</param>
        /// <param name="basedOn">This is a way to force the return hue (a spike ball's colors could be based on the handle's colors)</param>
        public static MaterialDefinition[] GetRandomMaterials_Composite(MaterialDefinition[] existing = null, ColorHSV? basedOn = null)
        {
            List<MaterialDefinition> retVal = new List<MaterialDefinition>();

            Random rand = StaticRandom.GetRandomForThread();

            if (existing != null)
            {
                retVal.AddRange(existing);
            }

            if (retVal.Count < 1)
            {
                if (basedOn == null)
                {
                    // For the first one, just pick any random color
                    retVal.Add(new MaterialDefinition() { DiffuseColor = UtilityWPF.GetRandomColor(0, 255).ToHex() });
                }
                else
                {
                    // Make this based on the hue of the color passed in
                    retVal.Add(new MaterialDefinition() { DiffuseColor = UtilityWPF.HSVtoRGB(GetRandomHue(basedOn.Value.H), rand.NextDouble(100), rand.NextDouble(100)).ToHex() });
                }
            }

            ColorHSV first = UtilityWPF.ColorFromHex(retVal[0].DiffuseColor).ToHSV();

            if (retVal.Count < 2)
            {
                retVal.Add(new MaterialDefinition() { DiffuseColor = UtilityWPF.HSVtoRGB(GetRandomHue(first.H), rand.NextDouble(100), rand.NextDouble(100)).ToHex() });
            }

            if (retVal.Count < 3)
            {
                retVal.Add(new MaterialDefinition() { DiffuseColor = UtilityWPF.HSVtoRGB(GetRandomHue(first.H), rand.NextDouble(100), rand.NextDouble(100)).ToHex() });
            }

            return retVal.ToArray();
        }
        /// <summary>
        /// Creates or adds to a set of colors to be used by klinth weapons
        /// </summary>
        /// <param name="existing">This is a material definition that may already exist, or partially filled out</param>
        /// <param name="basedOn">This is a way to force the return hue (a spike ball's colors could be based on the handle's colors)</param>
        public static MaterialDefinition[] GetRandomMaterials_Klinth(MaterialDefinition[] existing = null, ColorHSV? basedOn = null)
        {
            List<MaterialDefinition> retVal = new List<MaterialDefinition>();

            Random rand = StaticRandom.GetRandomForThread();

            if (existing != null)
            {
                retVal.AddRange(existing);
            }

            // Main color (in the case of a handle, the only color)
            if (retVal.Count < 1)
            {
                double hue;
                if (basedOn == null)
                {
                    hue = StaticRandom.NextDouble(0, 360);
                }
                else
                {
                    //hue = GetRandomHue(basedOn.Value.H);
                    hue = basedOn.Value.H;      // klinth shouldn't allow an opposite hue (it looks really bad)
                }

                retVal.Add(new MaterialDefinition()
                {
                    DiffuseColor = UtilityWPF.HSVtoRGB(hue, StaticRandom.NextDouble(50, 80), StaticRandom.NextDouble(40, 90)).ToHex(),
                    SpecularColor = UtilityWPF.HSVtoRGB(StaticRandom.NextDouble(0, 360), StaticRandom.NextDouble(50, 80), StaticRandom.NextDouble(40, 90)).ToHex(),
                    EmissiveColor = UtilityWPF.GetRandomColor(0, 255).ToHex()
                });
            }

            // Secondary color (in the case of ball and spikes, this is the spikes)
            if (retVal.Count < 2)
            {
                ColorHSV firstDiff = UtilityWPF.ColorFromHex(retVal[0].DiffuseColor).ToHSV();
                ColorHSV firstSpec = UtilityWPF.ColorFromHex(retVal[0].SpecularColor).ToHSV();
                ColorHSV firstEmis = UtilityWPF.ColorFromHex(retVal[0].EmissiveColor).ToHSV();

                // Needs to be roughly the same color as the ball, just a bit darker
                retVal.Add(new MaterialDefinition()
                {
                    DiffuseColor = UtilityWPF.HSVtoRGB(firstDiff.H, firstDiff.S * 1.25, firstDiff.V * .66d).ToHex(),
                    SpecularColor = UtilityWPF.HSVtoRGB(firstSpec.H, firstSpec.S * 1.1, firstSpec.V).ToHex(),
                    EmissiveColor = UtilityWPF.HSVtoRGB(firstEmis.H, firstEmis.S, firstEmis.V).ToHex(),
                });
            }

            return retVal.ToArray();
        }

        //TODO: Take in previous hue's and have a slight chance of 120 or 90 degree offset instead of always 0 and 180
        private static double GetRandomHue(double hue)
        {
            // Randomly choose a new hue that is similar to the one passed in
            double retVal = hue + StaticRandom.NextPow(3d, 18d, true);

            if (retVal < 0d)
            {
                retVal += 360d;
            }
            else if (retVal > 360d)
            {
                retVal -= 360d;
            }

            // Maybe rotate 180 degrees
            if (StaticRandom.Next(2) == 0)
            {
                retVal += 180d;

                if (retVal < 0d)
                {
                    retVal += 360d;
                }
                else if (retVal > 360d)
                {
                    retVal -= 360d;
                }
            }

            return retVal;
        }

        #endregion
    }

    #endregion

    #region Enum: WeaponHandleMaterial

    public enum WeaponHandleMaterial
    {
        // Wood - only for rods
        Soft_Wood,
        Hard_Wood,

        // Rope - only for ropes
        //CheapRope,
        //HempRope,

        // Metal - these are durable, but very dense
        Bronze,
        Iron,
        Steel,

        // The user can customize the colors of these two.  They are a step up from wood and metal
        /// <summary>
        /// Density and durability are between wood and metal
        /// </summary>
        Composite,
        /// <summary>
        /// A bit less dense than metal, same durability as composite (but is self repairing) - crystal appearance
        /// </summary>
        Klinth,

        // Special
        //      These will be rare.  not available when creating a weapon, must be picked up/purchased as is.
        //      They will have certain magic traits that come on randomly.
        //      Socketed gems won't change traits, just enhance
        //      Come up with heads that are more/less effective with each type of handle
        /// <summary>
        /// Boost strength - maybe some knockback
        /// Weapon self repairs
        /// </summary>
        /// <remarks>
        /// Blugeon heads work best
        /// 
        /// If they do so many kills using a moon with no gems, attach point at the end, and a proportional size, then give
        /// them the title of "Deadly Lolipop", and maybe an extra ability
        /// </remarks>
        Moon,
        /// <summary>
        /// Slow time
        /// a bit extra damage
        /// (tricky types of effects, maybe some random ranged damage)
        /// confusion/invisibility
        /// TODO: Read about wraiths, what are they really?
        /// </summary>
        /// <remarks>
        /// Extra effective when combined with a sword or spike
        /// </remarks>
        //Wraith,     //TODO: instead of a cylinder, this needs to be 2 to 5 shards.  put a point light in each.  maybe some random sparks appear and disappear.  this will always be rope, but limit the angles
        /// <summary>
        /// Life steal?  Reduce some abilites if used too much
        /// </summary>
        /// <remarks>
        /// If combined with an axe head, it gets extra enraged, reduces damage overall
        /// </remarks>
        //Enttrail,       // made from a dead ent.  regenerative abilities, but also angry
        /// <summary>
        /// Possessed by a demon.  Not moody/angry like the ent, but powerful
        /// Heavy damage
        /// </summary>
        /// <remarks>
        /// Maybe some kind of tractor beam - or pull the enemy bot closer, but repel the weapon (would need to repel it
        /// orth to direction so that double sided weapons aren't an issue)
        /// 
        /// have it help maintain swing speed (the equivalent of a small thruster attached to it, not always on, but will sometimes
        /// decide to get the weapon spun up quickly)
        /// 
        /// Thrives on chain kills...faster, faster.  Gets more and more powerful
        /// </remarks>
        //Demon,
    }

    #endregion
    #region Enum: WeaponHandleType

    public enum WeaponHandleType
    {
        Rope,
        Rod,

        // Overly complex.  Just allow for being extendable
        ///// <summary>
        ///// A rod that can extend (Make an option for spring loaded or just inertia)
        ///// </summary>
        //Telescoping,
        ///// <summary>
        ///// A rod that connects to a rope that can be released, retracted
        ///// </summary>
        //Harpoon,
    }

    #endregion
}
