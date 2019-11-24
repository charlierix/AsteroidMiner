using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.Arcanorum.MapObjects;

namespace Game.Newt.v2.Arcanorum
{
    public class WeaponAxe : WeaponPart
    {
        #region class: AxeSymetricalProps

        private class AxeSymetricalProps
        {
            public double leftX;
            public double leftY;
            public double rightX;
            public double rightY;

            public double edgeAngle;
            public double edgePercent;

            public double leftAngle;
            public double leftPercent;
            public bool leftAway;

            public double rightAngle;
            public double rightPercent;
            public bool rightAway;

            public bool isCenterFilled;

            public double Scale1X;
            public double Scale1Y;
            public double Z1;

            public double Scale2X;
            public double Scale2Y;
            public double Z2L;
            public double Z2R;
        }

        #endregion
        #region class: AxeSecondProps

        public class AxeSecondProps
        {
            // Points
            public Point3D EndTL { get; set; }
            public Point3D EndTR { get; set; }
            public Point3D EndBR { get; set; }
            public Point3D EndBL_1 { get; set; }
            public Point3D? EndBL_2 { get; set; }

            public readonly int IndexTL = 0;
            public readonly int IndexTR = 1;
            public readonly int IndexBR = 2;
            public readonly int IndexBL_1 = 3;
            public readonly int IndexBL_2 = 4;

            // Curve Controls
            public double EdgeAngleT { get; set; }
            public double EdgePercentT { get; set; }

            public double EdgeAngleB { get; set; }
            public double EdgePercentB { get; set; }

            // Only used if EndBL_2 is null
            public double B1AngleR { get; set; }
            public double B1PercentR { get; set; }

            public double B1AngleL { get; set; }
            public double B1PercentL { get; set; }

            // Only used if EndBL_2 is populated
            public double B2AngleR { get; set; }
            public double B2PercentR { get; set; }

            public double B2AngleL { get; set; }
            public double B2PercentL { get; set; }

            #region Public Methods

            public Point3D[] GetAllPoints()
            {
                return UtilityCore.Iterate<Point3D>(this.EndTL, this.EndTR, this.EndBR, this.EndBL_1, this.EndBL_2).ToArray();
            }

            public AxeSecondProps CloneNegateZ()
            {
                AxeSecondProps retVal = UtilityCore.Clone_Shallow(this);

                retVal.EndTL = new Point3D(retVal.EndTL.X, retVal.EndTL.Y, retVal.EndTL.Z * -1);
                retVal.EndTR = new Point3D(retVal.EndTR.X, retVal.EndTR.Y, retVal.EndTR.Z * -1);
                retVal.EndBR = new Point3D(retVal.EndBR.X, retVal.EndBR.Y, retVal.EndBR.Z * -1);
                retVal.EndBL_1 = new Point3D(retVal.EndBL_1.X, retVal.EndBL_1.Y, retVal.EndBL_1.Z * -1);

                if (retVal.EndBL_2 != null)
                {
                    retVal.EndBL_2 = new Point3D(retVal.EndBL_2.Value.X, retVal.EndBL_2.Value.Y, retVal.EndBL_2.Value.Z * -1);
                }

                return retVal;
            }

            #endregion
        }

        #endregion

        #region Declaration Section

        private readonly WeaponMaterialCache _materials;

        #endregion

        #region Constructor

        public WeaponAxe(WeaponMaterialCache materials, WeaponAxeDNA dna, bool isLeftSide)
        {
            _materials = materials;

            var model = GetModel(dna, materials, isLeftSide);

            this.Model = model.Item1;
            this.DNA = model.Item2;
        }

        #endregion

        #region Public Properties

        public WeaponAxeDNA DNA
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

        public override DamageProps CalculateExtraDamage(Point3D positionLocal, double collisionSpeed)
        {
            //TODO: Take damage (not much, just a bit of dulling)



            //TODO: Return some combination of extra kinetic, slash or pierce damage

            return new DamageProps();
        }

        #endregion

        #region Private Methods - Model

        private static Tuple<Model3DGroup, WeaponAxeDNA> GetModel(WeaponAxeDNA dna, WeaponMaterialCache materials, bool isLeftSide)
        {
            WeaponAxeDNA finalDNA = UtilityCore.Clone(dna);
            if (finalDNA.KeyValues == null)
            {
                finalDNA.KeyValues = new SortedList<string, double>();
            }

            Model3DGroup model = null;

            switch (finalDNA.AxeType)
            {
                case WeaponAxeType.Symetrical:
                    model = GetModel_Symetrical(dna, finalDNA, materials);
                    break;

                case WeaponAxeType.Lumber:
                case WeaponAxeType.Bearded:
                    model = GetModel_Second(dna, finalDNA, materials, finalDNA.AxeType == WeaponAxeType.Bearded);
                    break;

                default:
                    throw new ApplicationException("Unknown WeaponAxeType: " + finalDNA.AxeType.ToString());
            }

            Transform3DGroup transform = new Transform3DGroup();
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 90)));

            if(dna.IsBackward)
            {
                // This won't make a difference if it's a double sided axe, but will for single sided
                transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 180)));
            }

            if (isLeftSide)
            {
                transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 180)));
            }

            model.Transform = transform;

            return Tuple.Create(model, finalDNA);
        }

        // Symetrical
        private static Model3DGroup GetModel_Symetrical(WeaponAxeDNA dna, WeaponAxeDNA finalDNA, WeaponMaterialCache materials)
        {
            Model3DGroup retVal = new Model3DGroup();
            var from = dna.KeyValues;
            var to = finalDNA.KeyValues;

            // Define the curves
            AxeSymetricalProps arg = GetModel_Symetrical_Props(from, to);
            BezierSegment3D[][] segmentSets = GetModel_Symetrical_Curves(arg);

            //TODO: Use WeaponMaterialCache
            MaterialGroup materialMiddle = new MaterialGroup();
            materialMiddle.Children.Add(new DiffuseMaterial(new SolidColorBrush(Colors.DimGray)));
            Color derivedColor = UtilityWPF.AlphaBlend(Colors.DimGray, Colors.White, .8d);
            materialMiddle.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(128, derivedColor.R, derivedColor.G, derivedColor.B)), 2d));

            MaterialGroup materialEdge = new MaterialGroup();
            materialEdge.Children.Add(new DiffuseMaterial(new SolidColorBrush(Colors.GhostWhite)));
            materialEdge.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(Colors.GhostWhite, Colors.White, .5d)), 5d));

            #region Axe Blade (right)

            Model3D model = GetModel_Symetrical_Axe(segmentSets, materialMiddle, materialEdge, arg);

            double scale = dna.SizeSingle / (arg.leftY * 2d);

            Transform3DGroup transform = new Transform3DGroup();
            transform.Children.Add(new TranslateTransform3D(-arg.leftX, 0, 0));
            transform.Children.Add(new ScaleTransform3D(scale, scale, scale));

            model.Transform = transform;
            retVal.Children.Add(model);

            #endregion

            if (dna.Sides == WeaponAxeSides.Double)
            {
                #region Axe Blade (left)

                model = GetModel_Symetrical_Axe(segmentSets, materialMiddle, materialEdge, arg);

                transform = new Transform3DGroup();
                transform.Children.Add(new TranslateTransform3D(-arg.leftX, 0, 0));
                transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 180)));
                transform.Children.Add(new ScaleTransform3D(scale, scale, scale));

                model.Transform = transform;

                retVal.Children.Add(model);

                #endregion
            }

            Point3D topLeft = segmentSets[2][0].EndPoint0;
            Point3D bottomLeft = segmentSets[2][1].EndPoint0;
            double z = Math.Abs(segmentSets[0][0].EndPoint0.Z);

            if (dna.Sides == WeaponAxeSides.Single_BackSpike)
            {
                // Spike
                retVal.Children.Add(GetAxeSpike(z * 1.1, Math.Abs(topLeft.Y - bottomLeft.Y) * WeaponDNA.GetKeyValue("spikeLength", from, to, StaticRandom.NextDouble(.7, 1.1)), scale, 0, materialMiddle, materialEdge));
            }

            if (!(dna.Sides == WeaponAxeSides.Double && arg.isCenterFilled))     // double centerfilled is the only case that shouldn't have a collar
            {
                // Collar
                retVal.Children.Add(GetAxeCylinder(z * 1.33, Math.Abs(topLeft.Y - bottomLeft.Y) * 1.15, scale, 0, materialMiddle));
            }

            return retVal;
        }
        private static AxeSymetricalProps GetModel_Symetrical_Props(SortedList<string, double> from, SortedList<string, double> to)
        {
            AxeSymetricalProps retVal = new AxeSymetricalProps();

            Random rand = StaticRandom.GetRandomForThread();

            // Center Filled
            retVal.isCenterFilled = WeaponDNA.GetKeyValue_Bool("isCenterFilled", from, to, rand.NextBool());

            // Edge
            retVal.edgeAngle = WeaponDNA.GetKeyValue("edgeAngle", from, to, rand.NextPercent(45, 1));
            retVal.edgePercent = WeaponDNA.GetKeyValue("edgePercent", from, to, rand.NextPercent(.33, .75));

            // Left
            retVal.leftAway = WeaponDNA.GetKeyValue_Bool("leftAway", from, to, rand.NextBool());

            if (retVal.leftAway)
            {
                retVal.leftAngle = WeaponDNA.GetKeyValue("leftAngle", from, to, rand.NextDouble(0, 15));
                retVal.leftPercent = WeaponDNA.GetKeyValue("leftPercent", from, to, rand.NextPercent(.25, .5));
            }
            else
            {
                retVal.leftAngle = WeaponDNA.GetKeyValue("leftAngle", from, to, rand.NextPercent(20, .5));
                retVal.leftPercent = WeaponDNA.GetKeyValue("leftPercent", from, to, rand.NextPercent(.25, .75));
            }

            // Right
            retVal.rightAway = retVal.leftAway ? true : WeaponDNA.GetKeyValue_Bool("rightAway", from, to, rand.NextBool());        // it looks like a vase when left is away, and right is toward

            if (retVal.rightAway)
            {
                retVal.rightAngle = WeaponDNA.GetKeyValue("rightAngle", from, to, rand.NextDouble(0, 15));
                retVal.rightPercent = WeaponDNA.GetKeyValue("rightPercent", from, to, rand.NextPercent(.25, .5));
            }
            else
            {
                retVal.rightAngle = WeaponDNA.GetKeyValue("rightAngle", from, to, rand.NextPercent(20, .75));
                retVal.rightPercent = WeaponDNA.GetKeyValue("rightPercent", from, to, rand.NextPercent(.25, .75));
            }

            // Points
            retVal.leftX = -WeaponDNA.GetKeyValue("leftX", from, to, rand.NextDouble(1, 3));
            if (retVal.leftAway)
            {
                retVal.leftY = WeaponDNA.GetKeyValue("leftY", from, to, rand.NextDouble(2, 2.5));
            }
            else
            {
                retVal.leftY = WeaponDNA.GetKeyValue("leftY", from, to, rand.NextDouble(1.25, 2.5));
            }

            retVal.rightX = 2;
            retVal.rightY = 3.4;

            // Z
            retVal.Scale1X = WeaponDNA.GetKeyValue("Scale1X", from, to, rand.NextDouble(.6, .8));
            retVal.Scale1Y = WeaponDNA.GetKeyValue("Scale1Y", from, to, rand.NextDouble(.8, .95));
            retVal.Z1 = WeaponDNA.GetKeyValue("Z1", from, to, rand.NextPercent(.2, .5));

            retVal.Scale2X = WeaponDNA.GetKeyValue("Scale2X", from, to, rand.NextPercent(.4, .25));
            retVal.Scale2Y = WeaponDNA.GetKeyValue("Scale2Y", from, to, rand.NextPercent(.4, .25));
            retVal.Z2L = WeaponDNA.GetKeyValue("Z2L", from, to, rand.NextPercent(.55, .25));
            retVal.Z2R = WeaponDNA.GetKeyValue("Z2R", from, to, rand.NextPercent(.33, .25));

            return retVal;
        }
        private static BezierSegment3D[][] GetModel_Symetrical_Curves(AxeSymetricalProps arg)
        {
            BezierSegment3D[][] retVal = new BezierSegment3D[5][];

            //TODO: Come up with a transform based on arg's extremes and dna.Scale
            Transform3D transform = Transform3D.Identity;

            for (int cntr = 0; cntr < 5; cntr++)
            {
                #region scale, z

                double zL, zR, scaleX, scaleY;

                switch (cntr)
                {
                    case 0:
                    case 4:
                        zL = arg.Z2L;
                        zR = arg.Z2R;
                        scaleX = arg.Scale2X;
                        scaleY = arg.Scale2Y;
                        break;

                    case 1:
                    case 3:
                        zL = zR = arg.Z1;
                        scaleX = arg.Scale1X;
                        scaleY = arg.Scale1Y;
                        break;

                    case 2:
                        zL = zR = 0;
                        scaleX = scaleY = 1;
                        break;

                    default:
                        throw new ApplicationException("Unknown cntr: " + cntr.ToString());
                }

                if (cntr < 2)
                {
                    zL *= -1d;
                    zR *= -1d;
                }

                #endregion

                Point3D[] endPoints = new[]
                    {
                        new Point3D(arg.leftX, -arg.leftY * scaleY, zL),     // top left
                        new Point3D(arg.leftX, arg.leftY * scaleY, zL),     // bottom left
                        new Point3D(arg.rightX * scaleX, -arg.rightY * scaleY, zR),       // top right
                        new Point3D(arg.rightX * scaleX, arg.rightY * scaleY, zR),        // bottom right
                    }.Select(o => transform.Transform(o)).ToArray();

                BezierSegment3D[] segments = GetModel_Symetrical_Segments(endPoints, arg);
                retVal[cntr] = segments;
            }

            return retVal;
        }
        private static BezierSegment3D[] GetModel_Symetrical_Segments(Point3D[] endPoints, AxeSymetricalProps arg)
        {
            const int TOPLEFT = 0;
            const int BOTTOMLEFT = 1;
            const int TOPRIGHT = 2;
            const int BOTTOMRIGHT = 3;

            // Edge
            Point3D controlTR = BezierUtil.GetControlPoint_End(endPoints[TOPRIGHT], endPoints[BOTTOMRIGHT], endPoints[TOPLEFT], true, arg.edgeAngle, arg.edgePercent);
            Point3D controlBR = BezierUtil.GetControlPoint_End(endPoints[BOTTOMRIGHT], endPoints[TOPRIGHT], endPoints[BOTTOMLEFT], true, arg.edgeAngle, arg.edgePercent);
            BezierSegment3D edge = new BezierSegment3D(TOPRIGHT, BOTTOMRIGHT, new[] { controlTR, controlBR }, endPoints);

            // Bottom
            Point3D controlBL = BezierUtil.GetControlPoint_End(endPoints[BOTTOMLEFT], endPoints[BOTTOMRIGHT], endPoints[TOPRIGHT], arg.leftAway, arg.leftAngle, arg.leftPercent);
            controlBR = BezierUtil.GetControlPoint_End(endPoints[BOTTOMRIGHT], endPoints[BOTTOMLEFT], endPoints[TOPRIGHT], arg.rightAway, arg.rightAngle, arg.rightPercent);
            BezierSegment3D bottom = new BezierSegment3D(BOTTOMLEFT, BOTTOMRIGHT, new[] { controlBL, controlBR }, endPoints);

            // Top
            Point3D controlTL = BezierUtil.GetControlPoint_End(endPoints[TOPLEFT], endPoints[TOPRIGHT], endPoints[BOTTOMRIGHT], arg.leftAway, arg.leftAngle, arg.leftPercent);
            controlTR = BezierUtil.GetControlPoint_End(endPoints[TOPRIGHT], endPoints[TOPLEFT], endPoints[BOTTOMRIGHT], arg.rightAway, arg.rightAngle, arg.rightPercent);
            BezierSegment3D top = new BezierSegment3D(TOPLEFT, TOPRIGHT, new[] { controlTL, controlTR }, endPoints);

            return new[] { bottom, top, edge };
        }
        private static Model3D GetModel_Symetrical_Axe(BezierSegment3D[][] segmentSets, MaterialGroup materialMiddle, MaterialGroup materialEdge, AxeSymetricalProps arg)
        {
            Model3DGroup retVal = new Model3DGroup();

            int squareCount = 8;

            // 2 is z=0.  0,4 are z=max.  1,3 are intermediate z's

            AddBezierPlates(squareCount, segmentSets[0], segmentSets[1], retVal, materialMiddle);

            AddBezierPlates(squareCount, new[] { segmentSets[1][0], segmentSets[1][1] }, new[] { segmentSets[2][0], segmentSets[2][1] }, retVal, materialMiddle);
            AddBezierPlate(squareCount, segmentSets[1][2], segmentSets[2][2], retVal, materialEdge);

            AddBezierPlates(squareCount, new[] { segmentSets[2][0], segmentSets[2][1] }, new[] { segmentSets[3][0], segmentSets[3][1] }, retVal, materialMiddle);
            AddBezierPlate(squareCount, segmentSets[2][2], segmentSets[3][2], retVal, materialEdge);

            AddBezierPlates(squareCount, segmentSets[3], segmentSets[4], retVal, materialMiddle);

            // End cap plates
            if (arg.isCenterFilled)
            {
                for (int cntr = 0; cntr < 2; cntr++)
                {
                    int index = cntr == 0 ? 0 : 4;

                    AddBezierPlate(squareCount, segmentSets[index][0], segmentSets[index][1], retVal, materialMiddle);        // top - bottom

                    BezierSegment3D extraSeg = new BezierSegment3D(segmentSets[index][2].EndIndex0, segmentSets[index][2].EndIndex1, null, segmentSets[index][2].AllEndPoints);
                    AddBezierPlate(squareCount, extraSeg, segmentSets[index][2], retVal, materialMiddle);     // edge
                }
            }
            else
            {
                AddBezierPlates(squareCount, segmentSets[0], segmentSets[4], retVal, materialMiddle);
            }

            return retVal;
        }

        // Second design
        private static Model3DGroup GetModel_Second(WeaponAxeDNA dna, WeaponAxeDNA finalDNA, WeaponMaterialCache materials, bool hasBeard)
        {
            Model3DGroup retVal = new Model3DGroup();
            var from = dna.KeyValues;
            var to = finalDNA.KeyValues;

            // Define the curves
            AxeSecondProps arg = GetModel_Second_Props(from, to, hasBeard);
            BezierSegment3D[][] segmentSets = GetModel_Second_Curves(arg);

            //TODO: Use WeaponMaterialCache
            MaterialGroup materialMiddle = new MaterialGroup();
            materialMiddle.Children.Add(new DiffuseMaterial(new SolidColorBrush(Colors.DimGray)));
            Color derivedColor = UtilityWPF.AlphaBlend(Colors.DimGray, Colors.White, .8d);
            materialMiddle.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(128, derivedColor.R, derivedColor.G, derivedColor.B)), 2d));

            MaterialGroup materialEdge = new MaterialGroup();
            materialEdge.Children.Add(new DiffuseMaterial(new SolidColorBrush(Colors.GhostWhite)));
            materialEdge.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(Colors.GhostWhite, Colors.White, .5d)), 5d));

            #region Axe Blade (right)

            Model3D model = GetModel_Second_Axe(segmentSets, materialMiddle, materialEdge);

            double scale = dna.SizeSingle / (((arg.EndBL_2 ?? arg.EndBL_1).Y - arg.EndTL.Y) * 2);

            Transform3DGroup transform = new Transform3DGroup();
            transform.Children.Add(new TranslateTransform3D(-arg.EndTL.X, 0, 0));
            transform.Children.Add(new ScaleTransform3D(scale, scale, scale));

            model.Transform = transform;
            retVal.Children.Add(model);

            #endregion

            if (dna.Sides == WeaponAxeSides.Double)
            {
                #region Axe Blade (left)

                model = GetModel_Second_Axe(segmentSets, materialMiddle, materialEdge);

                transform = new Transform3DGroup();
                transform.Children.Add(new TranslateTransform3D(-arg.EndTL.X, 0, 0));
                transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 180)));
                transform.Children.Add(new ScaleTransform3D(scale, scale, scale));

                model.Transform = transform;

                retVal.Children.Add(model);

                #endregion
            }

            Point3D topLeft = segmentSets[2][0].EndPoint0;
            Point3D bottomLeft = segmentSets[2][segmentSets[2].Length - 1].EndPoint1;
            double z = Math.Abs(segmentSets[0][0].EndPoint0.Z);

            if (dna.Sides == WeaponAxeSides.Single_BackSpike)
            {
                // Spike
                retVal.Children.Add(GetAxeSpike(z * .9, Math.Abs(topLeft.Y - bottomLeft.Y) * WeaponDNA.GetKeyValue("spikeLength", from, to, StaticRandom.NextDouble(.9, 1.4)), scale, -.2, materialMiddle, materialEdge));
            }

            if (dna.Sides != WeaponAxeSides.Double)     // double centerfilled is the only case that shouldn't have a collar
            {
                // Collar
                retVal.Children.Add(GetAxeCylinder(z * 1.33, Math.Abs(topLeft.Y - bottomLeft.Y) * 1.15, scale, -.2, materialMiddle));
            }

            return retVal;
        }
        private static AxeSecondProps GetModel_Second_Props(SortedList<string, double> from, SortedList<string, double> to, bool hasBeard)
        {
            AxeSecondProps retVal = new AxeSecondProps();

            Random rand = StaticRandom.GetRandomForThread();

            #region Points

            // Edge
            retVal.EndTR = new Point3D(2, -1.1, 0) + WeaponDNA.GetKeyValue_Vector("EndTR", from, to, Math3D.GetRandomVector_Circular(.25));
            retVal.EndBR = new Point3D(1.3, 1.8, 0) + WeaponDNA.GetKeyValue_Vector("EndBR", from, to, Math3D.GetRandomVector_Circular(.25));

            // Left
            retVal.EndTL = new Point3D(-1.5, -1, 0);
            retVal.EndBL_1 = new Point3D(-1.5, .5, 0);

            if (hasBeard)
            {
                // Put an extra point along the bottom (left of this is that circle cutout)
                retVal.EndBL_2 = retVal.EndBL_1;        // 2 is now the last point
                retVal.EndBL_1 = new Point3D(.3, 1.2, 0) + WeaponDNA.GetKeyValue_Vector("EndBL_1", from, to, Math3D.GetRandomVector_Circular(.25));
            }
            else
            {
                retVal.EndBL_2 = null;
            }

            if (retVal.EndBL_2 != null && WeaponDNA.GetKeyValue_Bool("shouldExtendBeard", from, to, rand.NextBool()))
            {
                // Extend the beard
                retVal.EndBR += new Vector3D(0, WeaponDNA.GetKeyValue("extendBR", from, to, rand.NextDouble(.25, 2.2)), 0);
                retVal.EndBL_1 += new Vector3D(0, WeaponDNA.GetKeyValue("extendBL", from, to, rand.NextDouble(.25, 1.8)), 0);
            }

            double maxY = retVal.EndBR.Y - .25;

            if (retVal.EndBL_2 != null && retVal.EndBL_1.Y > maxY)
            {
                retVal.EndBL_1 = new Point3D(retVal.EndBL_1.X, maxY, retVal.EndBL_1.Z);       // can't let the middle point get lower, because the 3D would look wrong)
            }

            #endregion

            #region Curve Controls

            retVal.EdgeAngleT = WeaponDNA.GetKeyValue("EdgeAngleT", from, to, rand.NextPercent(15, .25));
            retVal.EdgePercentT = WeaponDNA.GetKeyValue("EdgePercentT", from, to, rand.NextPercent(.3, .25));

            retVal.EdgeAngleB = WeaponDNA.GetKeyValue("EdgeAngleB", from, to, rand.NextPercent(15, .25));
            retVal.EdgePercentB = WeaponDNA.GetKeyValue("EdgePercentB", from, to, rand.NextPercent(.3, .25));

            // Only used if EndBL_2 is null
            retVal.B1AngleR = WeaponDNA.GetKeyValue("B1AngleR", from, to, rand.NextPercent(10, .25));
            retVal.B1PercentR = WeaponDNA.GetKeyValue("B1PercentR", from, to, rand.NextPercent(.5, .25));

            retVal.B1AngleL = WeaponDNA.GetKeyValue("B1AngleL", from, to, rand.NextPercent(10, .25));
            retVal.B1PercentL = WeaponDNA.GetKeyValue("B1PercentL", from, to, rand.NextPercent(.33, .25));

            // Only used if EndBL_2 is populated
            retVal.B2AngleR = WeaponDNA.GetKeyValue("B2AngleR", from, to, rand.NextDouble(40, 80));
            retVal.B2PercentR = WeaponDNA.GetKeyValue("B2PercentR", from, to, rand.NextPercent(.6, .25));

            retVal.B2AngleL = WeaponDNA.GetKeyValue("B2AngleL", from, to, rand.NextDouble(40, 80));
            retVal.B2PercentL = WeaponDNA.GetKeyValue("B2PercentL", from, to, rand.NextPercent(.4, .25));

            #endregion

            return retVal;
        }
        private static BezierSegment3D[][] GetModel_Second_Curves(AxeSecondProps arg)
        {
            BezierSegment3D[][] retVal = new BezierSegment3D[5][];

            AxeSecondProps[] argLevels = new AxeSecondProps[5];

            argLevels[2] = arg;     // Edge
            #region Middle

            argLevels[1] = UtilityCore.Clone_Shallow(arg);

            // Right
            argLevels[1].EndTR = new Point3D(argLevels[1].EndTR.X * .8, argLevels[1].EndTR.Y * .9, .15);

            Vector3D offsetTR = new Point3D(argLevels[1].EndTR.X, argLevels[1].EndTR.Y, 0) - arg.EndTR;
            Quaternion angleTR = Math3D.GetRotation((arg.EndTL - arg.EndTR), offsetTR);

            Vector3D rotated = (arg.EndBL_1 - arg.EndBR).ToUnit() * offsetTR.Length;
            rotated = rotated.GetRotatedVector(angleTR.Axis, angleTR.Angle * -1.3);

            argLevels[1].EndBR = new Point3D(argLevels[1].EndBR.X + rotated.X, argLevels[1].EndBR.Y + rotated.Y, .15);      // can't just use percents of coordinates.  Instead use the same offset angle,distance that top left had

            // Left
            argLevels[1].EndTL = new Point3D(argLevels[1].EndTL.X, argLevels[1].EndTL.Y * .95, .3);

            if (argLevels[1].EndBL_2 == null)
            {
                argLevels[1].EndBL_1 = new Point3D(argLevels[1].EndBL_1.X, argLevels[1].EndBL_1.Y * .9, .3);
            }
            else
            {
                Vector3D offsetBL1 = arg.EndBR - arg.EndBL_1;
                double lengthBL1 = (new Point3D(argLevels[1].EndBR.X, argLevels[1].EndBR.Y, 0) - arg.EndBR).Length;
                offsetBL1 = offsetBL1.ToUnit() * (offsetBL1.Length - lengthBL1);

                argLevels[1].EndBL_1 = argLevels[1].EndBR - offsetBL1;
                argLevels[1].EndBL_1 = new Point3D(argLevels[1].EndBL_1.X, argLevels[1].EndBL_1.Y, .18);

                argLevels[1].EndBL_2 = new Point3D(argLevels[1].EndBL_2.Value.X, argLevels[1].EndBL_2.Value.Y * .9, .3);
            }

            argLevels[3] = argLevels[1].CloneNegateZ();

            #endregion
            #region Far

            argLevels[0] = UtilityCore.Clone_Shallow(arg);

            argLevels[0].EndTL = new Point3D(argLevels[0].EndTL.X, argLevels[0].EndTL.Y * .7, .4);

            argLevels[0].EndTR = new Point3D(argLevels[0].EndTR.X * .5, argLevels[0].EndTR.Y * .6, .25);

            if (argLevels[0].EndBL_2 == null)
            {
                argLevels[0].EndBR = new Point3D(argLevels[0].EndBR.X * .5, argLevels[0].EndBR.Y * .6, .25);
                argLevels[0].EndBL_1 = new Point3D(argLevels[0].EndBL_1.X, argLevels[0].EndBL_1.Y * .6, .4);
            }
            else
            {
                // Bottom Right
                Vector3D offset = (argLevels[1].EndBR - argLevels[1].EndBL_1) * .5;
                Point3D startPoint = argLevels[1].EndBL_1 + offset;     // midway along bottom edge

                offset = argLevels[1].EndTR - startPoint;

                argLevels[0].EndBR = startPoint + (offset * .15);       // from midway point toward upper right point
                argLevels[0].EndBR = new Point3D(argLevels[0].EndBR.X, argLevels[0].EndBR.Y, .25);      // fix z

                // Left of bottom right (where the circle cutout ends)
                offset = argLevels[1].EndBR - argLevels[1].EndBL_1;
                argLevels[0].EndBL_1 = Math3D.GetClosestPoint_Line_Point(argLevels[0].EndBR, offset, argLevels[0].EndBL_1);

                offset *= .05;
                Point3D minBL1 = argLevels[0].EndBR - offset;
                Vector3D testOffset = argLevels[0].EndBL_1 - argLevels[0].EndBR;
                if (Vector3D.DotProduct(testOffset, offset) < 0 || testOffset.LengthSquared < offset.LengthSquared)
                {
                    argLevels[0].EndBL_1 = minBL1;
                }

                // Bottom Left
                argLevels[0].EndBL_2 = new Point3D(argLevels[0].EndBL_2.Value.X, argLevels[0].EndBL_2.Value.Y * .6, .4);

                // Reduce the curve a bit
                argLevels[0].B2AngleL = argLevels[0].B2AngleL * .9;
                argLevels[0].B2PercentL = argLevels[0].B2PercentL * .95;

                argLevels[0].B2AngleR = argLevels[0].B2AngleR * .85;
                argLevels[0].B2PercentR = argLevels[0].B2PercentR * .95;
            }

            argLevels[4] = argLevels[0].CloneNegateZ();

            #endregion

            for (int cntr = 0; cntr < 5; cntr++)
            {
                BezierSegment3D[] segments = GetModel_Second_Segments(argLevels[cntr]);
                retVal[cntr] = segments;
            }

            return retVal;
        }
        private static BezierSegment3D[] GetModel_Second_Segments(AxeSecondProps arg)
        {
            Point3D[] points = arg.GetAllPoints();

            // Top
            BezierSegment3D top = new BezierSegment3D(arg.IndexTL, arg.IndexTR, null, points);

            // Edge
            Point3D controlTR = BezierUtil.GetControlPoint_End(arg.EndTR, arg.EndBR, arg.EndBL_1, true, arg.EdgeAngleT, arg.EdgePercentT);
            Point3D controlBR = BezierUtil.GetControlPoint_End(arg.EndBR, arg.EndTR, arg.EndTL, true, arg.EdgeAngleB, arg.EdgePercentB);
            BezierSegment3D edge = new BezierSegment3D(arg.IndexTR, arg.IndexBR, new[] { controlTR, controlBR }, points);

            // Bottom (right portion)
            BezierSegment3D bottomRight = null;
            if (arg.EndBL_2 == null)
            {
                Point3D controlR = BezierUtil.GetControlPoint_End(arg.EndBR, arg.EndBL_1, arg.EndTR, false, arg.B1AngleR, arg.B1PercentR);
                Point3D controlL = BezierUtil.GetControlPoint_End(arg.EndBL_1, arg.EndBR, arg.EndTR, false, arg.B1AngleL, arg.B1PercentL);
                bottomRight = new BezierSegment3D(arg.IndexBR, arg.IndexBL_1, new[] { controlR, controlL }, points);
            }
            else
            {
                bottomRight = new BezierSegment3D(arg.IndexBR, arg.IndexBL_1, null, points);
            }

            // Bottom (left portion)
            BezierSegment3D bottomLeft = null;
            if (arg.EndBL_2 != null)
            {
                Point3D controlR = BezierUtil.GetControlPoint_End(arg.EndBL_1, arg.EndBL_2.Value, arg.EndTR, false, arg.B2AngleR, arg.B2PercentR);
                Point3D controlL = BezierUtil.GetControlPoint_End(arg.EndBL_2.Value, arg.EndBL_1, arg.EndTR, false, arg.B2AngleL, arg.B2PercentL);
                bottomLeft = new BezierSegment3D(arg.IndexBL_1, arg.IndexBL_2, new[] { controlR, controlL }, points);
            }

            return UtilityCore.Iterate<BezierSegment3D>(top, edge, bottomRight, bottomLeft).ToArray();
        }
        private static Model3D GetModel_Second_Axe(BezierSegment3D[][] segmentSets, MaterialGroup materialMiddle, MaterialGroup materialEdge)
        {
            Model3DGroup retVal = new Model3DGroup();

            int numSegments = segmentSets[0].Length;

            int squareCount = 8;

            // 2 is z=0.  0,4 are z=max.  1,3 are intermediate z's

            // Z to Z
            AddBezierPlates(squareCount, segmentSets[0], segmentSets[1], retVal, materialMiddle);

            if (numSegments == 3)
            {
                AddBezierPlates(squareCount, new[] { segmentSets[1][0], segmentSets[1][2] }, new[] { segmentSets[2][0], segmentSets[2][2] }, retVal, materialMiddle);
                AddBezierPlate(squareCount, segmentSets[1][1], segmentSets[2][1], retVal, materialEdge);

                AddBezierPlates(squareCount, new[] { segmentSets[2][0], segmentSets[2][2] }, new[] { segmentSets[3][0], segmentSets[3][2] }, retVal, materialMiddle);
                AddBezierPlate(squareCount, segmentSets[2][1], segmentSets[3][1], retVal, materialEdge);
            }
            else
            {
                AddBezierPlates(squareCount, new[] { segmentSets[1][0], segmentSets[1][3] }, new[] { segmentSets[2][0], segmentSets[2][3] }, retVal, materialMiddle);
                AddBezierPlates(squareCount, new[] { segmentSets[1][1], segmentSets[1][2] }, new[] { segmentSets[2][1], segmentSets[2][2] }, retVal, materialEdge);

                AddBezierPlates(squareCount, new[] { segmentSets[2][0], segmentSets[2][3] }, new[] { segmentSets[3][0], segmentSets[3][3] }, retVal, materialMiddle);
                AddBezierPlates(squareCount, new[] { segmentSets[2][1], segmentSets[2][2] }, new[] { segmentSets[3][1], segmentSets[3][2] }, retVal, materialEdge);
            }

            AddBezierPlates(squareCount, segmentSets[3], segmentSets[4], retVal, materialMiddle);

            // End cap plates
            for (int cntr = 0; cntr < 2; cntr++)
            {
                int index = cntr == 0 ? 0 : 4;

                // Turn the end cap into a polygon, then triangulate it
                Point3D[] endCapPoly = BezierUtil.GetPath(squareCount, segmentSets[index].Select(o => new[] { o }).ToArray());       // Call the jagged array overload so that the individual bezier end points don't get smoothed out

                TriangleIndexed[] endCapTriangles = Math2D.GetTrianglesFromConcavePoly3D(endCapPoly);
                if (cntr == 0)
                {
                    endCapTriangles = endCapTriangles.Select(o => new TriangleIndexed(o.Index0, o.Index2, o.Index1, o.AllPoints)).ToArray();        // need to do this so the normals point in the proper direction
                }

                AddPolyPlate(endCapTriangles, retVal, materialMiddle);
            }

            return retVal;
        }

        private static Model3D GetAxeCylinder(double radius, double height, double scale, double yOffset, MaterialGroup material)
        {
            GeometryModel3D retVal = new GeometryModel3D();
            retVal.Material = material;
            retVal.BackMaterial = material;

            double bevel = radius * .2;

            List<TubeRingBase> tubes = new List<TubeRingBase>();

            tubes.Add(new TubeRingRegularPolygon(0, false, radius - bevel, radius - bevel, true));
            tubes.Add(new TubeRingRegularPolygon(bevel, false, radius, radius, false));
            tubes.Add(new TubeRingRegularPolygon(height - (bevel * 2), false, radius, radius, false));
            tubes.Add(new TubeRingRegularPolygon(bevel, false, radius - bevel, radius - bevel, true));

            retVal.Geometry = UtilityWPF.GetMultiRingedTube(10, tubes, true, true);

            Transform3DGroup transform = new Transform3DGroup();
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 90)));      // the tube is built along z, rotate so it's along y

            if (!Math1D.IsNearZero(yOffset))
            {
                transform.Children.Add(new TranslateTransform3D(0, yOffset, 0));
            }

            transform.Children.Add(new ScaleTransform3D(scale, scale, scale));

            retVal.Transform = transform;

            return retVal;
        }
        private static Model3D GetAxeSpike(double radius, double length, double scale, double yOffset, MaterialGroup materialMiddle, MaterialGroup materialEdge)
        {
            GeometryModel3D retVal = new GeometryModel3D();
            retVal.Material = materialEdge;
            retVal.BackMaterial = materialEdge;

            double bevel = radius * .2;

            List<TubeRingBase> tubes = new List<TubeRingBase>();

            tubes.Add(new TubeRingRegularPolygon(0, false, radius, radius * 2, true));
            tubes.Add(new TubeRingPoint(length, false));

            retVal.Geometry = UtilityWPF.GetMultiRingedTube(10, tubes, true, false);

            Transform3DGroup transform = new Transform3DGroup();
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), -90)));      // the tube is built along z, rotate so it's along x

            if (!Math1D.IsNearZero(yOffset))
            {
                transform.Children.Add(new TranslateTransform3D(0, yOffset, 0));
            }

            transform.Children.Add(new ScaleTransform3D(scale, scale, scale));

            retVal.Transform = transform;

            return retVal;
        }

        // WPF models
        private static void AddBezierPlates(int count, BezierSegment3D[] seg1, BezierSegment3D[] seg2, Model3DGroup group, Material material)
        {
            for (int cntr = 0; cntr < seg1.Length; cntr++)
            {
                AddBezierPlate(count, seg1[cntr], seg2[cntr], group, material);
            }
        }
        private static void AddBezierPlate(int count, BezierSegment3D seg1, BezierSegment3D seg2, Model3DGroup group, Material material)
        {
            // Since the bezier curves will have the same number of points, create a bunch of squares linking them (it's up to the caller
            // to make sure the curves don't cross, or you would get a bow tie)
            Point3D[] rim1 = BezierUtil.GetPoints(count, seg1);
            Point3D[] rim2 = BezierUtil.GetPoints(count, seg2);

            Point3D[] allPoints = UtilityCore.Iterate(rim1, rim2).ToArray();

            List<TriangleIndexed> triangles = new List<TriangleIndexed>();

            for (int cntr = 0; cntr < count - 1; cntr++)
            {
                triangles.Add(new TriangleIndexed(count + cntr, count + cntr + 1, cntr, allPoints));        // bottom left
                triangles.Add(new TriangleIndexed(cntr + 1, cntr, count + cntr + 1, allPoints));        // top right
            }

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = material;
            geometry.BackMaterial = material;
            geometry.Geometry = UtilityWPF.GetMeshFromTriangles(triangles.ToArray());

            group.Children.Add(geometry);
        }
        private static void AddPolyPlate(ITriangleIndexed[] triangles, Model3DGroup group, Material material)
        {
            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = material;
            geometry.BackMaterial = material;
            geometry.Geometry = UtilityWPF.GetMeshFromTriangles(triangles);

            group.Children.Add(geometry);
        }

        #endregion
    }

    #region class: WeaponAxeDNA

    public class WeaponAxeDNA : WeaponPartDNA
    {
        /// <summary>
        /// This is the size of the head
        /// NOTE: If Sides is double, then the final size will be roughly twice this
        /// </summary>
        public double SizeSingle { get; set; }

        public WeaponAxeType AxeType { get; set; }
        public WeaponAxeSides Sides { get; set; }
        public WeaponAxeStye Style { get; set; }

        #region Public Methods

        /// <summary>
        /// This returns a random axe, with some optional fixed values
        /// </summary>
        public static WeaponAxeDNA GetRandomDNA(double? sizeSingleSide = null, WeaponAxeType? axeType = null, WeaponAxeSides? sides = null, WeaponAxeStye? style = null)
        {
            WeaponAxeDNA retVal = new WeaponAxeDNA();

            Random rand = StaticRandom.GetRandomForThread();

            // Size
            if (sizeSingleSide != null)
            {
                retVal.SizeSingle = sizeSingleSide.Value;
            }
            else
            {
                retVal.SizeSingle = rand.NextDouble(.4, 1);
            }

            // AxeType
            if (axeType != null)
            {
                retVal.AxeType = axeType.Value;
            }
            else
            {
                retVal.AxeType = UtilityCore.GetRandomEnum<WeaponAxeType>();
            }

            // Sides
            if (sides != null)
            {
                retVal.Sides = sides.Value;
            }
            else
            {
                retVal.Sides = UtilityCore.GetRandomEnum<WeaponAxeSides>();
            }

            // Style
            if (style != null)
            {
                retVal.Style = style.Value;
            }
            else
            {
                retVal.Style = UtilityCore.GetRandomEnum<WeaponAxeStye>();
            }

            return retVal;
        }

        #endregion
    }

    #endregion

    #region enum: WeaponAxeType

    /// <summary>
    /// These are shapes that the axe could take.  A lot of this variation could be achieved with parameters to an axe building algorithm
    /// instead of separate enum values
    /// </summary>
    public enum WeaponAxeType
    {
        Symetrical,
        Lumber,
        Bearded


        // Flint,       // just a piece of flint lashed to the handle

        //Lumber_Iron,     // Standard wood cutting axe (flat cutting surface)
        //Lumber_Steel,

        //Battle,      // Simple convex battle axe, single has a small spike on the back --- this is what Symetrical is

        //Concave,        // the cutting edge is concave

        //Circular,     // A circular blade (if single sided is allowed, have the back side end like a spiral)

        //Fan,        // the blade faces up instead of to the sides

        //DownSpar,       // An extended spar (beard) facing toward the yielder (used for hooking a shield, probably also for easier to carry the axe)
        //UpSpar,     // An exteded spar facing toward (used for stabing)

        //Parabola,     // Sort of a boomarang shape http://www.elfwood.com/~kuzey/Dragon-Axe.2932139.html
    }

    #endregion
    #region enum: WeaponAxeSides

    public enum WeaponAxeSides
    {
        Double,
        Single_BackFlat,        // a strike with the back of the axe is like a small hammer
        Single_BackSpike,       // a strike with the back of the axe does some pierce
    }

    #endregion
    #region enum: WeaponAxeStye

    //TODO: Use different names
    public enum WeaponAxeStye
    {
        Basic,
        //Dwarf,      // I don't like dwarf axes, but something thats a bit more decorative than basic, more angular
        //Orc,
    }

    #endregion
}
