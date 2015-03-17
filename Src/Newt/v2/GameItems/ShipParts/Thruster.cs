using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using Game.HelperClassesCore;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.HelperClassesWPF;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.GameItems.ShipParts
{
    #region Class: ThrusterToolItem

    //TODO: Support custom thrusters
    public class ThrusterToolItem : PartToolItemBase
    {
        #region Declaration Section

        private readonly string _subName;
        private readonly Vector3D[] _directions;

        #endregion

        #region Constructor

        public ThrusterToolItem(EditorOptions options, ThrusterType thrusterType)
            : base(options)
        {
            if (thrusterType == ThrusterType.Custom)
            {
                throw new ArgumentException("Can't pass custom into this overload");
            }

            this.ThrusterType = thrusterType;

            _subName = thrusterType.ToString().ToLower().Replace('_', ' ');
            _directions = null;

            _visual2D = PartToolItemBase.GetVisual2D(this.Name, this.Description, options.EditorColors);

            this.TabName = PartToolItemBase.TAB_SHIPPART;
        }

        public ThrusterToolItem(EditorOptions options, Vector3D[] directions, string name)
            : base(options)
        {
            this.ThrusterType = ThrusterType.Custom;

            _subName = name;
            _directions = directions;

            _visual2D = PartToolItemBase.GetVisual2D(this.Name, this.Description, options.EditorColors);

            this.TabName = PartToolItemBase.TAB_SHIPPART;
        }

        #endregion

        #region Public Properties

        public override string Name
        {
            get
            {
                return "Thruster (" + _subName + ")";
            }
        }
        public override string Description
        {
            get
            {
                return "Consumes fuel, and produces force";
            }
        }
        public override string Category
        {
            get
            {
                return PartToolItemBase.CATEGORY_PROPULSION;
            }
        }

        public ThrusterType ThrusterType
        {
            get;
            private set;
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
            if(this.ThrusterType == ThrusterType.Custom)
            {
                return new ThrusterDesign(this.Options, _directions);
            }
            else
            {
                return new ThrusterDesign(this.Options, this.ThrusterType);
            }
        }

        #endregion
    }

    #endregion
    #region Class: ThrusterDesign

    public class ThrusterDesign : PartDesignBase
    {
        #region Declaration Section

        public const double RADIUSPERCENTOFSCALE = .17d;

        public const PartDesignAllowedScale ALLOWEDSCALE = PartDesignAllowedScale.XYZ;		// This is here so the scale can be known through reflection

        private Point3D[] _pointsForHull = null;

        private Tuple<UtilityNewt.IObjectMassBreakdown, Vector3D, double> _massBreakdown = null;

        #endregion

        #region Constructor

        public ThrusterDesign(EditorOptions options, ThrusterType thrusterType)
            : base(options)
        {
            if (thrusterType == ThrusterType.Custom)
            {
                throw new ArgumentException("This overload doesn't allow a ThrusterType of Custom");
            }

            this.ThrusterType = thrusterType;
            this.ThrusterDirections = GetThrusterDirections(this.ThrusterType);
        }
        public ThrusterDesign(EditorOptions options, Vector3D[] thrusters)
            : base(options)
        {
            if (thrusters == null || thrusters.Length == 0)
            {
                throw new ArgumentNullException("The custom thrusts overload must have at least one thrust direction");
            }

            this.ThrusterType = ThrusterType.Custom;

            // Make sure they are stored as unit vectors
            this.ThrusterDirections = new Vector3D[thrusters.Length];
            for (int cntr = 0; cntr < thrusters.Length; cntr++)
            {
                if (Math3D.IsNearZero(thrusters[cntr]))
                {
                    throw new ArgumentException("Can't have a thrust direction of zero");
                }

                this.ThrusterDirections[cntr] = thrusters[cntr].ToUnit();
            }
        }

        #endregion

        #region Public Properties

        public ThrusterType ThrusterType
        {
            get;
            private set;
        }
        // These are unit vectors
        public Vector3D[] ThrusterDirections
        {
            get;
            private set;
        }

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
                switch (this.ThrusterType)
                {
                    case ThrusterType.One:
                    case ThrusterType.Two:
                        return PartDesignAllowedRotation.X_Y_Z;

                    default:
                        return PartDesignAllowedRotation.X_Y_Z;
                }
            }
        }

        private Model3DGroup _geometries = null;
        public override Model3D Model
        {
            get
            {
                if (_geometries == null)
                {
                    _geometries = CreateGeometry(false, true);
                }

                return _geometries;
            }
        }

        #endregion

        #region Public Methods

        public override Model3D GetFinalModel()
        {
            return CreateGeometry(true, true);
        }

        public override PartDNA GetDNA()
        {
            ThrusterDNA retVal = new ThrusterDNA();

            base.FillDNA(retVal);
            retVal.ThrusterType = this.ThrusterType;
            retVal.ThrusterDirections = this.ThrusterDirections;

            return retVal;
        }
        public override void SetDNA(PartDNA dna)
        {
            if (!(dna is ThrusterDNA))
            {
                throw new ArgumentException("The class passed in must be ThrusterDNA");
            }

            ThrusterDNA dnaCast = (ThrusterDNA)dna;

            base.StoreDNA(dna);

            // The constructor already took care of these (the dna could be incomplete, the constructor has more certainty)
            //this.ThrusterType = dnaCast.ThrusterType;
            //this.ThrusterDirections = dnaCast.ThrusterDirections;
        }

        public override CollisionHull CreateCollisionHull(WorldBase world)
        {
            Transform3DGroup transform = new Transform3DGroup();
            //transform.Children.Add(new ScaleTransform3D(this.Scale));		// it ignores scale
            //transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90)));		// only needed for one/two types
            transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(this.Orientation)));
            transform.Children.Add(new TranslateTransform3D(this.Position.ToVector()));

            Vector3D scale = this.Scale;

            switch (this.ThrusterType)
            {
                case ShipParts.ThrusterType.One:
                case ShipParts.ThrusterType.Two:
                    #region Cylinder

                    transform.Children.Insert(0, new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90)));		// the physics hull is along x, but dna is along z

                    double radius = RADIUSPERCENTOFSCALE * (scale.X + scale.Y) * .5d;
                    double height = scale.Z;

                    if (height < radius * 2d)
                    {
                        // Newton keeps the capsule caps spherical, but the visual scales them.  So when the height is less than the radius, newton
                        // make a sphere.  So just make a cylinder instead
                        //return CollisionHull.CreateChamferCylinder(world, 0, radius, height, transform.Value);
                        return CollisionHull.CreateCylinder(world, 0, radius, height, transform.Value);
                    }
                    else
                    {
                        //NOTE: The visual changes the caps around, but I want the physics to be a capsule
                        return CollisionHull.CreateCapsule(world, 0, radius, height, transform.Value);
                    }

                    #endregion

                default:
                    #region Convex Hull

                    if (_pointsForHull == null)
                    {
                        CreateGeometry(true, false);        // passing false, because this may be executing in a different thread
                    }

                    double maxScale = Math3D.Max(scale.X * RADIUSPERCENTOFSCALE, scale.Y * RADIUSPERCENTOFSCALE, scale.Z);

                    //NOTE: _pointsForHull comes off the wpf model points, and is already rotated properly
                    Point3D[] points = _pointsForHull.Select(o => new Point3D(o.X * maxScale, o.Y * maxScale, o.Z * maxScale)).ToArray();

                    return CollisionHull.CreateConvexHull(world, 0, points, transform.Value);

                    #endregion
            }
        }
        public override UtilityNewt.IObjectMassBreakdown GetMassBreakdown(double cellSize)
        {
            if (_massBreakdown != null && _massBreakdown.Item2 == this.Scale && _massBreakdown.Item3 == cellSize)
            {
                // This has already been built for this size
                return _massBreakdown.Item1;
            }

            UtilityNewt.ObjectMassBreakdownSet breakdown = null;

            switch (this.ThrusterType)
            {
                case ShipParts.ThrusterType.One:
                case ShipParts.ThrusterType.Two:
                    breakdown = GetMassBreakdownSprtCylinder(this.Scale, cellSize);
                    break;

                case ShipParts.ThrusterType.Two_One:
                case ShipParts.ThrusterType.Two_Two:
                case ShipParts.ThrusterType.Two_Two_One:
                case ShipParts.ThrusterType.Two_Two_Two:
                case ShipParts.ThrusterType.Custom:
                    breakdown = GetMassBreakdownSprtMulti(this.Scale, cellSize, this.ThrusterDirections);
                    break;

                default:
                    throw new ApplicationException("Unknown ThrusterType: " + this.ThrusterType.ToString());
            }

            // Store this
            _massBreakdown = new Tuple<UtilityNewt.IObjectMassBreakdown, Vector3D, double>(breakdown, this.Scale, cellSize);

            // Exit Function
            return _massBreakdown.Item1;
        }

        public static Vector3D[] GetThrusterDirections(ThrusterType thrusterType)
        {
            Vector3D[] retVal = null;

            switch (thrusterType)
            {
                case ThrusterType.One:
                    #region OneWay

                    // Directions
                    retVal = new Vector3D[1];
                    retVal[0] = new Vector3D(0, 0, 1);		// the visual's bottle points down, but the thrust is up

                    #endregion
                    break;

                case ThrusterType.Two:
                    #region TwoWay

                    // Directions
                    retVal = new Vector3D[2];
                    retVal[0] = new Vector3D(0, 0, 1);
                    retVal[1] = new Vector3D(0, 0, -1);

                    #endregion
                    break;

                case ThrusterType.Two_One:
                    #region Two_One

                    // Directions
                    retVal = new Vector3D[3];
                    retVal[0] = new Vector3D(0, 0, 1);
                    retVal[1] = new Vector3D(0, 0, -1);
                    retVal[2] = new Vector3D(1, 0, 0);

                    #endregion
                    break;

                case ThrusterType.Two_Two:
                    #region Two_Two

                    // Directions
                    retVal = new Vector3D[4];
                    retVal[0] = new Vector3D(0, 0, 1);
                    retVal[1] = new Vector3D(0, 0, -1);
                    retVal[2] = new Vector3D(1, 0, 0);
                    retVal[3] = new Vector3D(-1, 0, 0);

                    #endregion
                    break;

                case ThrusterType.Two_Two_One:
                    #region Two_Two_One

                    // Directions
                    retVal = new Vector3D[5];
                    retVal[0] = new Vector3D(0, 0, 1);
                    retVal[1] = new Vector3D(0, 0, -1);
                    retVal[2] = new Vector3D(1, 0, 0);
                    retVal[3] = new Vector3D(-1, 0, 0);
                    retVal[4] = new Vector3D(0, 1, 0);

                    #endregion
                    break;

                case ThrusterType.Two_Two_Two:
                    #region Two_Two_Two

                    // Directions
                    retVal = new Vector3D[6];
                    retVal[0] = new Vector3D(0, 0, 1);
                    retVal[1] = new Vector3D(0, 0, -1);
                    retVal[2] = new Vector3D(1, 0, 0);
                    retVal[3] = new Vector3D(-1, 0, 0);
                    retVal[4] = new Vector3D(0, 1, 0);
                    retVal[5] = new Vector3D(0, -1, 0);

                    #endregion
                    break;

                case ThrusterType.Custom:
                    #region Custom

                    throw new ApplicationException("finish implementing custom thrusters");

                    #endregion
                    break;

                default:
                    throw new ApplicationException("Unknown ThrusterType: " + thrusterType.ToString());
            }

            // Exit Function
            return retVal;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// The part may be created in an arbitrary thread.  When that happens, wpf specific objects can't be stored (they aren't threadsafe).
        /// But this method is complex enough that I don't want to duplicate bits of it
        /// </summary>
        private Model3DGroup CreateGeometry(bool isFinal, bool shouldCommitWPF)
        {
            #region Materials

            // Front Material
            MaterialGroup frontMaterial = new MaterialGroup();
            DiffuseMaterial diffuse = new DiffuseMaterial(new SolidColorBrush(WorldColors.Thruster));
            if (shouldCommitWPF)
            {
                this.MaterialBrushes.Add(new MaterialColorProps(diffuse, WorldColors.Thruster));
            }
            frontMaterial.Children.Add(diffuse);

            SpecularMaterial specular = WorldColors.ThrusterSpecular;
            if (shouldCommitWPF)
            {
                this.MaterialBrushes.Add(new MaterialColorProps(specular));
            }
            frontMaterial.Children.Add(specular);

            // Back Material
            MaterialGroup backMaterial = new MaterialGroup();
            diffuse = new DiffuseMaterial(new SolidColorBrush(WorldColors.ThrusterBack));
            if (shouldCommitWPF)
            {
                this.MaterialBrushes.Add(new MaterialColorProps(diffuse, WorldColors.ThrusterBack));
            }
            backMaterial.Children.Add(diffuse);

            // Glow
            if (!isFinal)
            {
                EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
                frontMaterial.Children.Add(selectionEmissive);
                if (shouldCommitWPF)
                {
                    base.SelectionEmissives.Add(selectionEmissive);
                }

                selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
                backMaterial.Children.Add(selectionEmissive);
                if (shouldCommitWPF)
                {
                    base.SelectionEmissives.Add(selectionEmissive);
                }
            }

            #endregion

            GeometryModel3D geometry;
            List<TubeRingBase> rings;
            Transform3DGroup transform;
            double scale;

            int domeSegments = isFinal ? 2 : 10;
            int cylinderSegments = isFinal ? 6 : 35;

            Model3DGroup retVal = new Model3DGroup();

            switch (this.ThrusterType)
            {
                case ThrusterType.One:
                    #region OneWay

                    // Geometry 1
                    geometry = new GeometryModel3D();
                    geometry.Material = frontMaterial;
                    geometry.BackMaterial = backMaterial;

                    rings = GetThrusterRings1Full(domeSegments);

                    scale = 1d / rings.Sum(o => o.DistFromPrevRing);		// Scale this so the height is 1
                    transform = new Transform3DGroup();
                    transform.Children.Add(new ScaleTransform3D(scale, scale, scale));

                    geometry.Geometry = UtilityWPF.GetMultiRingedTube(cylinderSegments, rings, true, true, transform);
                    retVal.Children.Add(geometry);

                    #endregion
                    break;

                case ThrusterType.Two:
                    #region TwoWay

                    // Geometry 1
                    geometry = new GeometryModel3D();
                    geometry.Material = frontMaterial;
                    geometry.BackMaterial = backMaterial;

                    rings = GetThrusterRings2Full();

                    scale = 1d / rings.Sum(o => o.DistFromPrevRing);		// Scale this so the height is 1
                    transform = new Transform3DGroup();
                    transform.Children.Add(new ScaleTransform3D(scale, scale, scale));

                    geometry.Geometry = UtilityWPF.GetMultiRingedTube(cylinderSegments, rings, true, true, transform);
                    retVal.Children.Add(geometry);

                    // This will make a glass tube around the thruster
                    //MaterialGroup testMaterial = new MaterialGroup();
                    //testMaterial.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("#30FFFFFF"))));

                    //geometry = new GeometryModel3D();
                    //geometry.Material = testMaterial;
                    //geometry.BackMaterial = testMaterial;
                    //geometry.Geometry = UtilityWPF.GetCylinder_AlongX(20, RADIUSPERCENTOFSCALE, 1, new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90)));
                    //retVal.Children.Add(geometry);

                    #endregion
                    break;

                case ThrusterType.Two_One:
                    #region Two_One

                    // Geometry 1
                    geometry = new GeometryModel3D();
                    geometry.Material = frontMaterial;
                    geometry.BackMaterial = backMaterial;

                    rings = GetThrusterRings2Full();

                    scale = 1d / rings.Sum(o => o.DistFromPrevRing);		// Scale this so the height is 1
                    transform = new Transform3DGroup();
                    transform.Children.Add(new ScaleTransform3D(scale, scale, scale));

                    geometry.Geometry = UtilityWPF.GetMultiRingedTube(cylinderSegments, rings, true, true, transform);
                    retVal.Children.Add(geometry);

                    // Geometry 2
                    geometry = new GeometryModel3D();
                    geometry.Material = frontMaterial;
                    geometry.BackMaterial = backMaterial;

                    rings = GetThrusterRings1Half(false, domeSegments);
                    transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90)));
                    transform.Children.Add(new TranslateTransform3D(-scale * rings.Sum(o => o.DistFromPrevRing), 0, 0));
                    geometry.Geometry = UtilityWPF.GetMultiRingedTube(cylinderSegments, rings, true, false, transform);

                    retVal.Children.Add(geometry);

                    #endregion
                    break;

                case ThrusterType.Two_Two:
                    #region Two_Two

                    // Geometry Model1
                    geometry = new GeometryModel3D();
                    geometry.Material = frontMaterial;
                    geometry.BackMaterial = backMaterial;

                    rings = GetThrusterRings2Full();

                    scale = 1d / rings.Sum(o => o.DistFromPrevRing);		// Scale this so the height is 1
                    transform = new Transform3DGroup();
                    transform.Children.Add(new ScaleTransform3D(scale, scale, scale));

                    geometry.Geometry = UtilityWPF.GetMultiRingedTube(cylinderSegments, rings, true, true, transform);

                    retVal.Children.Add(geometry);

                    // Geometry Model2
                    geometry = new GeometryModel3D();
                    geometry.Material = frontMaterial;
                    geometry.BackMaterial = backMaterial;

                    transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90)));
                    geometry.Geometry = UtilityWPF.GetMultiRingedTube(cylinderSegments, rings, true, true, transform);

                    retVal.Children.Add(geometry);

                    #endregion
                    break;

                case ThrusterType.Two_Two_One:
                    #region Two_Two_One

                    // Geometry Model1
                    geometry = new GeometryModel3D();
                    geometry.Material = frontMaterial;
                    geometry.BackMaterial = backMaterial;

                    rings = GetThrusterRings2Full();

                    scale = 1d / rings.Sum(o => o.DistFromPrevRing);		// Scale this so the height is 1
                    transform = new Transform3DGroup();
                    transform.Children.Add(new ScaleTransform3D(scale, scale, scale));

                    geometry.Geometry = UtilityWPF.GetMultiRingedTube(cylinderSegments, rings, true, true, transform);

                    retVal.Children.Add(geometry);

                    // Geometry Model2
                    geometry = new GeometryModel3D();
                    geometry.Material = frontMaterial;
                    geometry.BackMaterial = backMaterial;

                    transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90)));
                    geometry.Geometry = UtilityWPF.GetMultiRingedTube(cylinderSegments, rings, true, true, transform);

                    retVal.Children.Add(geometry);

                    // Geometry Model3
                    geometry = new GeometryModel3D();
                    geometry.Material = frontMaterial;
                    geometry.BackMaterial = backMaterial;

                    rings = GetThrusterRings1Half(false, domeSegments);
                    transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 90)));
                    transform.Children.Add(new TranslateTransform3D(0, -scale * rings.Sum(o => o.DistFromPrevRing), 0));
                    geometry.Geometry = UtilityWPF.GetMultiRingedTube(cylinderSegments, rings, true, false, transform);

                    retVal.Children.Add(geometry);

                    #endregion
                    break;

                case ThrusterType.Two_Two_Two:
                    #region Two_Two_Two

                    // Geometry Model1
                    geometry = new GeometryModel3D();
                    geometry.Material = frontMaterial;
                    geometry.BackMaterial = backMaterial;

                    rings = GetThrusterRings2Full();

                    scale = 1d / rings.Sum(o => o.DistFromPrevRing);		// Scale this so the height is 1
                    transform = new Transform3DGroup();
                    transform.Children.Add(new ScaleTransform3D(scale, scale, scale));

                    geometry.Geometry = UtilityWPF.GetMultiRingedTube(cylinderSegments, rings, true, true, transform);

                    retVal.Children.Add(geometry);

                    // Geometry Model2
                    geometry = new GeometryModel3D();
                    geometry.Material = frontMaterial;
                    geometry.BackMaterial = backMaterial;

                    transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90)));
                    geometry.Geometry = UtilityWPF.GetMultiRingedTube(cylinderSegments, rings, true, true, transform);

                    retVal.Children.Add(geometry);

                    // Geometry Model3
                    geometry = new GeometryModel3D();
                    geometry.Material = frontMaterial;
                    geometry.BackMaterial = backMaterial;

                    transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 90)));
                    geometry.Geometry = UtilityWPF.GetMultiRingedTube(cylinderSegments, rings, true, true, transform);

                    retVal.Children.Add(geometry);

                    #endregion
                    break;

                case ThrusterType.Custom:
                    #region Custom

                    if (this.ThrusterDirections.Length == 1)
                    {
                        #region One

                        geometry = new GeometryModel3D();
                        geometry.Material = frontMaterial;
                        geometry.BackMaterial = backMaterial;

                        rings = GetThrusterRings1Full(domeSegments);

                        scale = 1d / rings.Sum(o => o.DistFromPrevRing);		// Scale this so the height is 1
                        transform = new Transform3DGroup();
                        transform.Children.Add(new ScaleTransform3D(scale, scale, scale));
                        transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRotation(new Vector3D(0, 0, 1), this.ThrusterDirections[0]))));

                        geometry.Geometry = UtilityWPF.GetMultiRingedTube(cylinderSegments, rings, true, true, transform);
                        retVal.Children.Add(geometry);

                        #endregion
                    }
                    else
                    {
                        #region Many

                        scale = 1d / GetThrusterRings2Full().Sum(o => o.DistFromPrevRing);		// Scale this so the height is 1 (need to use full rings so scale is accurate)
                        double offset = -scale * GetThrusterRings1Half(true, domeSegments).Sum(o => o.DistFromPrevRing);		// get the height of half1 which is a bit smaller than half2

                        rings = GetThrusterRings1Half2(true, domeSegments);		// this is what will actually be drawn

                        for (int cntr = 0; cntr < this.ThrusterDirections.Length; cntr++)
                        {
                            geometry = new GeometryModel3D();
                            geometry.Material = frontMaterial;
                            geometry.BackMaterial = backMaterial;

                            transform = new Transform3DGroup();
                            transform.Children.Add(new ScaleTransform3D(scale, scale, scale));
                            transform.Children.Add(new TranslateTransform3D(0, 0, offset));		// this translation makes the thruster come out of origin (otherwise it would be centered funny on origin)
                            transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRotation(new Vector3D(0, 0, 1), this.ThrusterDirections[cntr]))));

                            geometry.Geometry = UtilityWPF.GetMultiRingedTube(cylinderSegments, rings, true, false, transform);

                            retVal.Children.Add(geometry);
                        }

                        #endregion
                    }

                    #endregion
                    break;

                default:
                    throw new ApplicationException("Unknown ThrusterType: " + this.ThrusterType.ToString());
            }

            // Remember the points
            if (isFinal)
            {
                _pointsForHull = UtilityWPF.GetPointsFromMesh(retVal);
            }

            // Transform
            retVal.Transform = GetTransformForGeometry(isFinal);

            // Exit Function
            return retVal;
        }

        private static List<TubeRingBase> GetThrusterRings1Full(int domeSegments)
        {
            List<TubeRingBase> retVal = new List<TubeRingBase>();

            retVal.Add(new TubeRingRegularPolygon(0, false, .25, .25, false));
            retVal.Add(new TubeRingRegularPolygon(1.2, false, .9, .9, false));
            retVal.Add(new TubeRingRegularPolygon(.5, false, 1, 1, false));
            retVal.Add(new TubeRingRegularPolygon(1.5, false, 1.1, 1.1, false));
            retVal.Add(new TubeRingRegularPolygon(1.5, false, 1.05, 1.05, false));
            retVal.Add(new TubeRingDome(.66, false, domeSegments));

            return retVal;
        }
        private static List<TubeRingBase> GetThrusterRings1Half(bool includeDome, int domeSegments)
        {
            List<TubeRingBase> retVal = new List<TubeRingBase>();

            retVal.Add(new TubeRingRegularPolygon(0, false, .25, .25, false));
            retVal.Add(new TubeRingRegularPolygon(1.2, false, .9, .9, false));
            retVal.Add(new TubeRingRegularPolygon(.5, false, 1, 1, false));
            retVal.Add(new TubeRingRegularPolygon(1.5, false, 1.1, 1.1, false));
            if (includeDome)
            {
                retVal.Add(new TubeRingDome(.55, false, domeSegments));
            }

            return retVal;
        }
        private static List<TubeRingBase> GetThrusterRings1Half2(bool includeDome, int domeSegments)
        {
            List<TubeRingBase> retVal = new List<TubeRingBase>();

            retVal.Add(new TubeRingRegularPolygon(0, false, .25, .25, false));
            retVal.Add(new TubeRingRegularPolygon(1.2, false, .9, .9, false));
            retVal.Add(new TubeRingRegularPolygon(.5, false, 1, 1, false));
            retVal.Add(new TubeRingRegularPolygon(1.5, false, 1.1, 1.1, false));
            retVal.Add(new TubeRingRegularPolygon(.4, false, 1, 1, false));		// this is an extra bit that GetThrusterRings1Half doesn't have (looks better when combined with other halves)
            if (includeDome)
            {
                retVal.Add(new TubeRingDome(.55, false, domeSegments));
            }

            return retVal;
        }
        private static List<TubeRingBase> GetThrusterRings2Full()
        {
            List<TubeRingBase> retVal = new List<TubeRingBase>();

            retVal.Add(new TubeRingRegularPolygon(0, false, .25, .25, false));
            retVal.Add(new TubeRingRegularPolygon(1.2, false, .9, .9, false));
            retVal.Add(new TubeRingRegularPolygon(.5, false, 1, 1, false));
            retVal.Add(new TubeRingRegularPolygon(1.5, false, 1.1, 1.1, false));
            retVal.Add(new TubeRingRegularPolygon(1.5, false, 1, 1, false));
            retVal.Add(new TubeRingRegularPolygon(.5, false, .9, .9, false));
            retVal.Add(new TubeRingRegularPolygon(1.2, false, .25, .25, false));

            return retVal;
        }

        private static UtilityNewt.ObjectMassBreakdownSet GetMassBreakdownSprtCylinder(Vector3D scale, double cellSize)
        {
            // Convert this.Scale into a size that the mass breakdown will use (mass breakdown wants height along X, and scale is for radius, but the mass breakdown wants diameter
            // Reducing Z a bit, because the thruster has tapered ends
            Vector3D size = new Vector3D(scale.Z * .9d, scale.X * RADIUSPERCENTOFSCALE * 2d, scale.Y * RADIUSPERCENTOFSCALE * 2d);

            // Cylinder
            UtilityNewt.ObjectMassBreakdown cylinder = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Cylinder, UtilityNewt.MassDistribution.Uniform, size, cellSize);

            Transform3D transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90));		// the physics hull is along x, but dna is along z

            // Rotated
            return new UtilityNewt.ObjectMassBreakdownSet(new UtilityNewt.ObjectMassBreakdown[] { cylinder }, new Transform3D[] { transform });
        }
        private static UtilityNewt.ObjectMassBreakdownSet GetMassBreakdownSprtMulti(Vector3D scale, double cellSize, Vector3D[] directions)
        {
            // Convert this.Scale into a size that the mass breakdown will use (mass breakdown wants height along X, and scale is for radius, but the mass breakdown wants diameter
            Vector3D size = new Vector3D(scale.Z, scale.X * RADIUSPERCENTOFSCALE * 2d, scale.Y * RADIUSPERCENTOFSCALE * 2d);

            // Center ball
            double centerSize = Math.Max(size.X * RADIUSPERCENTOFSCALE * 2d, Math.Max(size.Y, size.Z));
            centerSize *= .5d;
            double centerVolume = 4d / 3d * Math.PI * Math.Pow(centerSize * .5d, 3d);
            var centerBall = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Box, UtilityNewt.MassDistribution.Uniform, new Vector3D(centerSize, centerSize, centerSize), centerSize * 1.1d);

            // Cylinder
            Vector3D cylinderSize = new Vector3D(size.X * .35d, size.Y, size.Z);		// the cylinder's length is the radius (or half of x) minus the center ball, and a bit less, since it's not a complete cylinder
            double cylinderVolume = Math.Pow((cylinderSize.Y + cylinderSize.Z) / 4d, 2d) * Math.PI * cylinderSize.X;		// dividing by 4, because div 2 is the average, then another 2 is to convert diameter to radius
            var cylinder = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Cylinder, UtilityNewt.MassDistribution.Uniform, cylinderSize, cellSize);

            // Build up the final
            Vector3D cylinderOffset = new Vector3D((centerSize * .5d) + (cylinderSize.X * .5d), 0d, 0d);
            Vector3D offset = new Vector3D(1, 0, 0);

            List<Tuple<UtilityNewt.ObjectMassBreakdown, Point3D, Quaternion, double>> items = new List<Tuple<UtilityNewt.ObjectMassBreakdown, Point3D, Quaternion, double>>();

            items.Add(new Tuple<UtilityNewt.ObjectMassBreakdown, Point3D, Quaternion, double>(centerBall, new Point3D(0, 0, 0), Quaternion.Identity, centerVolume));

            foreach (Vector3D dir in directions)
            {
                Quaternion quat = Math3D.GetRotation(offset, dir);
                Point3D translate = new RotateTransform3D(new QuaternionRotation3D(quat)).Transform(cylinderOffset).ToPoint();

                items.Add(new Tuple<UtilityNewt.ObjectMassBreakdown, Point3D, Quaternion, double>(cylinder, translate, quat, cylinderVolume));
            }

            // Exit Function
            return UtilityNewt.Combine(items.ToArray());
        }

        #endregion
    }

    #endregion
    #region Class: Thruster

    public class Thruster : PartBase, INeuronContainer, IPartUpdatable
    {
        #region Declaration Section

        public const string PARTTYPE = "Thruster";

        private readonly object _lock = new object();

        private readonly ItemOptions _itemOptions;

        private readonly IContainer _fuelTanks;

        /// <summary>
        /// These are on a 1:1 with the thrusters.  
        /// </summary>
        /// <remarks>
        /// There are three ways to use the thruster:
        ///     Directly calling the fire method - you would then need to manually apply forces to the body, as well as draw the flames
        ///     Setting Percents, which will get looked at each update tick
        ///     Using these neurons, which will get looked at each update tick
        /// </remarks>
        private readonly Neuron_ZeroPos[] _neurons;

        #endregion

        #region Constructor

        public Thruster(EditorOptions options, ItemOptions itemOptions, ThrusterDNA dna, IContainer fuelTanks)
            : base(options, dna)
        {
            _itemOptions = itemOptions;
            _fuelTanks = fuelTanks;

            if (dna.ThrusterType == ThrusterType.Custom)
            {
                this.Design = new ThrusterDesign(options, dna.ThrusterDirections);
            }
            else
            {
                this.Design = new ThrusterDesign(options, dna.ThrusterType);
            }
            this.Design.SetDNA(dna);

            double radius;
            double cylinderVolume = GetVolume(out radius, out _scaleActual, dna);

            this.Radius = radius;

            Vector3D[] thrustDirections = ((ThrusterDesign)this.Design).ThrusterDirections;
            _mass = GetMass(itemOptions, thrustDirections.Length, cylinderVolume);
            _forceAtMax = cylinderVolume * itemOptions.ThrusterStrengthRatio * ItemOptions.FORCESTRENGTHMULT;		//ThrusterStrengthRatio is stored as a lower value so that the user doesn't see such a huge number

            RotateTransform3D transform = new RotateTransform3D(new QuaternionRotation3D(dna.Orientation));
            this.ThrusterDirectionsShip = thrustDirections.Select(o => transform.Transform(o)).ToArray();		//NOTE: It is expected that Design.ThrusterDirections are unit vectors

            _neurons = CreateNeurons(thrustDirections);
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

        public bool IsOn
        {
            get
            {
                // Even though thruster exposes neurons to control it, that's just an interface to the thruster, so there's
                // no reason to draw from an energy tank to power those neurons.  (this IsOn property is from the
                // perspective of a brain, that's why it's independant of how much fuel is available - you can always
                // try to tell the thruster to fire, that's when it will check for fuel)
                return true;
            }
        }

        #endregion
        #region IPartUpdatable Members

        public void Update_MainThread(double elapsedTime)
        {
        }
        public void Update_AnyThread(double elapsedTime)
        {
            //NOTE: This method doesn't have a lock.  Instead, this.Fire has the lock, which this method calls multiple times.  Since the fuel is updated outside of this thruster's
            //control anyway, putting an extra lock here wouldn't really do any good.  It's possible that this method could get called multiple times simultaneously, in which
            //case this.FiredThrustsLastUpdate would only get the last result, and fuel would be burnt unnecessarily, but that shouldn't happen, and if it does, there's not much
            //harm caused

            Vector3D?[] thrustResults = new Vector3D?[_neurons.Length];

            double[] manualPercents = this.Percents;

            // Instead of doing for(int cntr = 0, etc.  Randomly go through the list.  That way if the fuel runs out halfway through, the
            // first thrusters won't always win
            foreach (int i in UtilityCore.RandomRange(0, _neurons.Length))
            {
                // If this.Percents is populated, it overrides.  Otherwise, use the neurons
                double percent = manualPercents != null ? manualPercents[i] : _neurons[i].Value;

                // See if it should fire
                if (percent > 0d)
                {
                    thrustResults[i] = Fire(ref percent, i, elapsedTime);
                }
                else
                {
                    thrustResults[i] = null;
                }
            }

            this.FiredThrustsLastUpdate = thrustResults;
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

        public ThrusterType ThrusterType
        {
            get
            {
                return ((ThrusterDesign)this.Design).ThrusterType;
            }
        }
        // These are unit vectors
        public Vector3D[] ThrusterDirectionsModel
        {
            get
            {
                return ((ThrusterDesign)this.Design).ThrusterDirections;
            }
        }
        public Vector3D[] ThrusterDirectionsShip
        {
            get;
            private set;
        }

        private volatile object _percent = null;
        /// <summary>
        /// This lets the thruster to be set manually.  Each update, this.Percents is looked it.  If nonnull, this.Percents is used,
        /// otherwise neurons are used
        /// WARNING: This is stored as a volatile, so it's best to set the whole array at once
        /// </summary>
        public double[] Percents
        {
            get
            {
                return (double[])_percent;
            }
            set
            {
                if (value == null)
                {
                }
                else
                {
                    if (value.Length != _neurons.Length)
                    {
                        throw new ArgumentException(string.Format("The array passed in is the wrong length.  Expected={0}, Passed In={1}", _neurons.Length.ToString(), value.Length.ToString()));
                    }

                    _percent = value;
                }

            }
        }

        /// <summary>
        /// This is only populated by the update method.  It is the results of calling the Fire method for
        /// each neuron (or looking at this.Percents)
        /// </summary>
        /// <remarks>
        /// This is exposed so the ship can apply forces, also draw thrust lines
        /// </remarks>
        public volatile Vector3D?[] FiredThrustsLastUpdate = null;

        /// <summary>
        /// When drawing a thrust line visual, this is where to start it
        /// </summary>
        public double ThrustVisualStartRadius
        {
            get
            {
                double maxSize = Math3D.Max(this.Design.Scale.X, this.Design.Scale.Y, this.Design.Scale.Z);		// they should all be the same

                return maxSize * .5d;
            }
        }

        private readonly double _forceAtMax;
        public double ForceAtMax
        {
            get
            {
                return _forceAtMax;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// This will fire the thruster
        /// NOTE: It is assumed that the thruster is firing steady throughout elapsedTime
        /// NOTE: The percent max is byref so that the caller knows what percent was actually used (good for visuals/sounds)
        /// NOTE: The returned vector is in ship coords
        /// </summary>
        /// <remarks>
        /// This method is here for cases when a thruster will be programatically controlled.
        /// 
        /// The next higher level way to use thrusters is to set the desired percent, and call Update_AnyThread on a
        /// regular basis.  There are two ways to set the desired percent:
        ///     Set this.Percents
        ///     Set _neurons values (only looked at if this.Percents is null)
        /// </remarks>
        public Vector3D? Fire(ref double percentMax, int index, double elapsedTime)
        {
            lock (_lock)
            {
                double actualForce;

                if (_fuelTanks != null && _fuelTanks.QuantityCurrent > 0d)
                {
                    // Cap at 100%
                    if (percentMax > 1d)
                    {
                        percentMax = 1d;
                    }

                    // Figure out how much force will be generated
                    actualForce = _forceAtMax * percentMax;

                    // See how much fuel that will take
                    double fuelToUse = actualForce * elapsedTime * _itemOptions.FuelToThrustRatio * ItemOptions.FUELTOTHRUSTMULT;		// FuelToThrustRatio is stored as a larger value so the user can work with it easier

                    // Try to burn that much fuel
                    double fuelUnused = _fuelTanks.RemoveQuantity(fuelToUse, false);
                    if (fuelUnused > 0d)
                    {
                        // Not enough fuel, reduce the amount of force
                        actualForce -= fuelUnused / (elapsedTime * _itemOptions.FuelToThrustRatio * ItemOptions.FUELTOTHRUSTMULT);
                        percentMax *= (fuelToUse - fuelUnused) / fuelToUse;		// multiply by the ratio that was actually used
                    }
                }
                else
                {
                    // No fuel
                    actualForce = 0d;
                    percentMax = 0d;
                }

                // Exit Function
                if (actualForce > 0d)
                {
                    return this.ThrusterDirectionsShip[index] * actualForce;
                }
                else
                {
                    return null;
                }
            }
        }

        #endregion

        #region Private Methods

        private static double GetVolume(out double radius, out Vector3D actualScale, ThrusterDNA dna)
        {
            // Just assume it's a cylinder
            double radX = dna.Scale.X * ThrusterDesign.RADIUSPERCENTOFSCALE;
            double radY = dna.Scale.Y * ThrusterDesign.RADIUSPERCENTOFSCALE;
            double height = dna.Scale.Z;

            radius = (radX + radY + (height * .5d)) / 3d;		// this is just an approximation for the neural container

            actualScale = new Vector3D(radX * 2d, radY * 2d, height);

            return Math.PI * radX * radY * height;
        }

        private static double GetMass(ItemOptions itemOptions, int thrustCount, double cylinderVolume)
        {
            // Get the mass of the cylinder
            double retVal = cylinderVolume * itemOptions.ThrusterDensity;

            // Instead of trying some complex formula to figure out how much each extra thruster weighs, just add
            // a percent of the base mass for each additional thruster
            if (thrustCount > 1)
            {
                retVal += (thrustCount - 1) * (retVal * itemOptions.ThrusterAdditionalMassPercent);
            }

            // Exit Function
            return retVal;
        }

        private static Neuron_ZeroPos[] CreateNeurons(Vector3D[] thrustDirections)
        {
            Neuron_ZeroPos[] retVal;

            if (thrustDirections.Length == 1)
            {
                // Since there's only one, just put it in the center
                retVal = new Neuron_ZeroPos[] { new Neuron_ZeroPos(new Point3D(0, 0, 0)) };
            }
            else
            {
                // Place the neurons along the direction of the corresponding thrust (the directions are already unit vectors)
                retVal = thrustDirections.Select(o => new Neuron_ZeroPos(o.ToPoint())).ToArray();
            }

            return retVal;
        }

        #endregion
    }

    #endregion

    #region Class: ThrusterDNA

    public class ThrusterDNA : PartDNA
    {
        public ThrusterType ThrusterType
        {
            get;
            set;
        }
        public Vector3D[] ThrusterDirections
        {
            get;
            set;
        }
    }

    #endregion

    #region Enum: ThrusterType

    public enum ThrusterType
    {
        /// <summary>
        /// Big thruster, pointing in a single direction
        /// </summary>
        One,
        /// <summary>
        /// Two thrusters pointing +/-  z axis
        /// </summary>
        Two,
        /// <summary>
        /// Two way along the z axis, one way along the x axis
        /// </summary>
        Two_One,
        /// <summary>
        /// Two way along the z axis, two way along the x axis
        /// </summary>
        Two_Two,
        /// <summary>
        /// Two way along the z axis, two way along the x axis, one way along the y axis
        /// </summary>
        Two_Two_One,
        /// <summary>
        /// 3 Two ways along the x/y/z axiis
        /// </summary>
        Two_Two_Two,
        /// <summary>
        /// An array of vectors is passed in, and a thruster is built along each vector (both size and direction)
        /// </summary>
        Custom
    }

    #endregion
}
