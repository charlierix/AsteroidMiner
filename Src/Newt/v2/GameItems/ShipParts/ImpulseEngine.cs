using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.GameItems.ShipParts
{
    #region class: ImpulseEngineToolItem

    public class ImpulseEngineToolItem : PartToolItemBase
    {
        #region Declaration Section

        private readonly ImpulseEngineType _engineType;

        #endregion

        #region Constructor

        public ImpulseEngineToolItem(EditorOptions options, ImpulseEngineType engineType)
            : base(options)
        {
            _engineType = engineType;
            TabName = PartToolItemBase.TAB_SHIPPART;
            _visual2D = PartToolItemBase.GetVisual2D(Name, Description, options, this);
        }

        #endregion

        #region Public Properties

        public override string Name
        {
            get
            {
                string extra = "";
                switch (_engineType)
                {
                    case ImpulseEngineType.Rotate:
                        extra = "rotate";
                        break;

                    case ImpulseEngineType.Translate:
                        extra = "translate";
                        break;
                }

                if (extra != "")
                {
                    extra = string.Format(" ({0} only)", extra);
                }


                return "Impulse Engine" + extra;
            }
        }
        public override string Description
        {
            get
            {
                return "Consumes plasma, applies forces/torques exactly as it's told (this is a cheat part, it doesn't matter where it's placed within the bot)";
            }
        }
        public override string Category
        {
            get
            {
                return PartToolItemBase.CATEGORY_PROPULSION;
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
            return new ImpulseEngineDesign(Options, false, _engineType);
        }

        #endregion
    }

    #endregion
    #region class: ImpulseEngineDesign

    public class ImpulseEngineDesign : PartDesignBase
    {
        #region class: GlowBall

        private class GlowBall
        {
            public Model3D Model { get; set; }
            public ScaleTransform3D Scale { get; set; }

            //public RotateTransform3D Rotate { get; set; }
            public QuaternionRotation3D Rotate_Quat { get; set; }

            private AnimateRotation _rotateAnimate = null;

            public void RotateTick(double elapsedTime)
            {
                if (this.Rotate_Quat == null)
                {
                    return;
                }

                if (_rotateAnimate == null)
                {
                    _rotateAnimate = AnimateRotation.Create_AnyOrientation(this.Rotate_Quat, 45d);
                }

                _rotateAnimate.Tick(elapsedTime);
            }
            public void SetPercent(double percent)
            {
                //if (percent < 0) percent = 0;     // capped caps the output anyway
                //else if (percent > 1) percent = 1;

                // If it gets much smaller than .75, it won't be visible, and much bigger than .92, it will poke though the outer visual
                double scale = UtilityCore.GetScaledValue_Capped(.75, .92, 0, 1, percent);

                this.Scale.ScaleX = scale;
                this.Scale.ScaleY = scale;
                this.Scale.ScaleZ = scale;
            }
        }

        #endregion

        #region Declaration Section

        internal const double SIZEPERCENTOFSCALE = 1d;

        public const PartDesignAllowedScale ALLOWEDSCALE = PartDesignAllowedScale.XYZ;		// This is here so the scale can be known through reflection

        private MassBreakdownCache _massBreakdown = null;

        private GlowBall _glowBall = null;

        #endregion

        #region Constructor

        public ImpulseEngineDesign(EditorOptions options, bool isFinalModel, ImpulseEngineType engineType)
            : base(options, isFinalModel)
        {
            _engineType = engineType;
        }

        #endregion

        #region Public Properties

        private readonly ImpulseEngineType _engineType;
        public ImpulseEngineType EngineType => _engineType;

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

        private Model3D _model = null;
        public override Model3D Model
        {
            get
            {
                if (_model == null)
                {
                    var models = CreateGeometry(this.IsFinalModel);
                    _model = models.Item1;
                    _glowBall = models.Item2;
                }

                return _model;
            }
        }

        #endregion

        #region Public Methods

        public override ShipPartDNA GetDNA()
        {
            ImpulseEngineDNA retVal = new ImpulseEngineDNA();

            base.FillDNA(retVal);
            retVal.ImpulseEngineType = EngineType;

            return retVal;
        }
        public override void SetDNA(ShipPartDNA dna)
        {
            if (!(dna is ImpulseEngineDNA))
            {
                throw new ArgumentException("The class passed in must be " + nameof(ImpulseEngineDNA));
            }

            base.StoreDNA(dna);

            // The constructor already took care of engine type
            //ImpulseEngineDNA dnaCast = (ImpulseEngineDNA)dna;
            //_engineType = dnaCast.ImpulseEngineType;
        }

        public override CollisionHull CreateCollisionHull(WorldBase world)
        {
            Transform3DGroup transform = new Transform3DGroup();
            //transform.Children.Add(new ScaleTransform3D(scale));		// it ignores scale
            transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(this.Orientation)));
            transform.Children.Add(new TranslateTransform3D(this.Position.ToVector()));

            Vector3D size = new Vector3D(this.Scale.X * SIZEPERCENTOFSCALE * .5d, this.Scale.Y * SIZEPERCENTOFSCALE * .5d, this.Scale.Z * SIZEPERCENTOFSCALE * .5d);

            return CollisionHull.CreateSphere(world, 0, size, transform.Value);
        }
        public override UtilityNewt.IObjectMassBreakdown GetMassBreakdown(double cellSize)
        {
            if (_massBreakdown != null && _massBreakdown.Scale == Scale && _massBreakdown.CellSize == cellSize)
            {
                // This has already been built for this size
                return _massBreakdown.Breakdown;
            }

            // Convert this.Scale into a size that the mass breakdown will use
            Vector3D size = new Vector3D(this.Scale.X * SIZEPERCENTOFSCALE, this.Scale.Y * SIZEPERCENTOFSCALE, this.Scale.Z * SIZEPERCENTOFSCALE);

            var breakdown = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Sphere, UtilityNewt.MassDistribution.Uniform, size, cellSize);

            // Store this
            _massBreakdown = new MassBreakdownCache(breakdown, Scale, cellSize);

            return _massBreakdown.Breakdown;
        }

        public override PartToolItemBase GetToolItem()
        {
            return new ImpulseEngineToolItem(this.Options, _engineType);
        }

        public void SetGlowballPercent(double percent)
        {
            _glowBall.SetPercent(percent);
        }
        public void RotateGlowball(double elapsedTime)
        {
            _glowBall.RotateTick(elapsedTime);
        }

        #endregion

        #region Private Methods

        private Tuple<Model3D, GlowBall> CreateGeometry(bool isFinal)
        {
            const double REDUCTIONSCALE = .7;
            const double BEVELNORMALHEIGHTMULT = .33d;
            const double BEVELSIDEDEPTH = .5;     // go down roughly the radius of the sphere (the walls also extend out, so it's not just radius)
            const double SPHERERADIUS = SIZEPERCENTOFSCALE * .5;        // reducing by .5, because the sphere was created with radius 1 (which would make the diameter 2)
            const double ICONRADIUS = SPHERERADIUS * .5;
            const double ARROWTHICKNESS_SHAFT = ICONRADIUS / 4;
            const double ARROWTHICKNESS_ARROW = ARROWTHICKNESS_SHAFT * 3;

            Model3DGroup group = new Model3DGroup();

            #region shell

            TriangleIndexed[] sphereTrianglesOrig = UtilityWPF.GetPentakisDodecahedron(1);

            ITriangle[] beveledTriangles = sphereTrianglesOrig.
                SelectMany(o => GetBeveledTriangle(o, REDUCTIONSCALE, BEVELNORMALHEIGHTMULT, BEVELSIDEDEPTH)).
                ToArray();

            MaterialGroup material = new MaterialGroup();

            DiffuseMaterial diffuse = new DiffuseMaterial(new SolidColorBrush(WorldColors.ImpulseEngine_Color));
            this.MaterialBrushes.Add(new MaterialColorProps(diffuse, WorldColors.ImpulseEngine_Color));
            material.Children.Add(diffuse);

            SpecularMaterial specular = WorldColors.ImpulseEngine_Specular;
            this.MaterialBrushes.Add(new MaterialColorProps(specular));
            material.Children.Add(specular);

            if (!isFinal)
            {
                EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
                material.Children.Add(selectionEmissive);
                this.SelectionEmissives.Add(selectionEmissive);
            }

            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = material;
            geometry.BackMaterial = material;
            geometry.Geometry = UtilityWPF.GetMeshFromTriangles_IndependentFaces(beveledTriangles);
            geometry.Transform = new ScaleTransform3D(SPHERERADIUS, SPHERERADIUS, SPHERERADIUS);

            group.Children.Add(geometry);

            #endregion

            #region rotate/translate icons

            if (_engineType == ImpulseEngineType.Rotate)
            {
                group.Children.AddRange(CreateIcons_Rotate(ICONRADIUS, ARROWTHICKNESS_SHAFT, ARROWTHICKNESS_ARROW, SPHERERADIUS));
            }
            else if (_engineType == ImpulseEngineType.Translate)
            {
                group.Children.AddRange(CreateIcons_Translate(ICONRADIUS, ARROWTHICKNESS_SHAFT, ARROWTHICKNESS_ARROW, SPHERERADIUS));
            }

            #endregion

            #region glow ball

            GlowBall glowBall = CreateGlowBall();

            group.Children.Add(glowBall.Model);

            #endregion

            group.Transform = GetTransformForGeometry(isFinal);

            return new Tuple<Model3D, GlowBall>(group, glowBall);
        }

        private static GlowBall CreateGlowBall()
        {
            //NOTE: Not adding these materials to this.MaterialBrushes or this.SelectionEmissives, because this will only be visible while the
            //engine is in use (those properties are for the ship editor)

            MaterialGroup material = new MaterialGroup();
            material.Children.Add(new DiffuseMaterial(new SolidColorBrush(WorldColors.ImpulseEngineGlowball_Color)));
            material.Children.Add(WorldColors.ImpulseEngineGlowball_Specular);
            material.Children.Add(WorldColors.ImpulseEngineGlowball_Emissive);

            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = material;
            geometry.BackMaterial = material;
            geometry.Geometry = UtilityWPF.GetMeshFromTriangles(UtilityWPF.GetIcosahedron(1, 1));

            Transform3DGroup transform = new Transform3DGroup();

            ScaleTransform3D scale = new ScaleTransform3D()
            {
                ScaleX = .01,       // start it so small that it's invisible
                ScaleY = .01,
                ScaleZ = .01,
            };
            transform.Children.Add(scale);

            QuaternionRotation3D quatRot = new QuaternionRotation3D(Math3D.GetRandomRotation());
            transform.Children.Add(new RotateTransform3D(quatRot));

            transform.Children.Add(new ScaleTransform3D(SIZEPERCENTOFSCALE * .5, SIZEPERCENTOFSCALE * .5, SIZEPERCENTOFSCALE * .5));       // reducing by .5, because the sphere was created with radius 1 (which would make the diameter 2)

            geometry.Transform = transform;

            return new GlowBall()
            {
                Model = geometry,
                Scale = scale,
                Rotate_Quat = quatRot,
            };
        }

        private static Model3D[] CreateIcons_Rotate(double arcRadius, double arcThickness, double arrowThickness, double sphereRadius)
        {
            const int NUMSIDES_TOTAL = 13;
            const int NUMSIDES_USE = 9;

            #region create triangles

            Point[] pointsTheta = Math2D.GetCircle_Cached(NUMSIDES_TOTAL);

            var circlePoints = pointsTheta.
                Select(o => GetCirclePoints(o, arcRadius, arcThickness, sphereRadius)).
                ToArray();

            List<Triangle> triangles = new List<Triangle>();

            for (int cntr = 0; cntr < NUMSIDES_USE - 1; cntr++)
            {
                triangles.Add(new Triangle(circlePoints[cntr].inner, circlePoints[cntr].outer, circlePoints[cntr + 1].inner));
                triangles.Add(new Triangle(circlePoints[cntr].outer, circlePoints[cntr + 1].outer, circlePoints[cntr + 1].inner));
            }

            int i1 = NUMSIDES_USE - 1;
            int i2 = NUMSIDES_USE;

            var arrowBase = GetCirclePoints(pointsTheta[i1], arcRadius, arrowThickness, sphereRadius);
            var arrowTip = GetCirclePoints(pointsTheta[i2], arcRadius, arcThickness * .75, sphereRadius);        //NOTE: Not using mid, because that curls the arrow too steeply (looks odd).  So using outer as a comprimise between pointing in and pointing straight

            triangles.Add(new Triangle(arrowBase.inner, circlePoints[i1].inner, arrowTip.outer));
            triangles.Add(new Triangle(circlePoints[i1].inner, circlePoints[i1].outer, arrowTip.outer));
            triangles.Add(new Triangle(circlePoints[i1].outer, arrowBase.outer, arrowTip.outer));

            // It would be more efficient to build the link triangles directly, but a lot more logic
            ITriangleIndexed[] indexedTriangles = TriangleIndexed.ConvertToIndexed(triangles.ToArray());

            #endregion

            List<Model3D> retVal = new List<Model3D>();

            MaterialGroup material = new MaterialGroup();
            material.Children.Add(new DiffuseMaterial(new SolidColorBrush(WorldColors.ImpulseEngine_Icon_Color)));
            material.Children.Add(WorldColors.ImpulseEngine_Icon_Specular);
            material.Children.Add(WorldColors.ImpulseEngine_Icon_Emissive);

            foreach (Transform3D transform in GetRotations_Tetrahedron(sphereRadius))
            {
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = material;
                geometry.BackMaterial = material;
                geometry.Geometry = UtilityWPF.GetMeshFromTriangles(indexedTriangles);
                geometry.Transform = transform;

                retVal.Add(geometry);
            }

            return retVal.ToArray();
        }
        private static Model3D[] CreateIcons_Translate(double lineRadius, double lineThickness, double arrowThickness, double sphereRadius)
        {
            #region define points

            Vector[] cores1 = new[]
            {
                    new Vector(1,0),
                    new Vector(0, 1),
                    new Vector(-1, 0),
                    new Vector(0, -1),
                };

            var cores2 = cores1.
                Select(o =>
                (
                    o * lineThickness / 2,
                    o * lineRadius * 2d / 3d,
                    o * lineRadius
                )).
                ToArray();

            var lines = cores2.
                Select(o => new
                {
                    from = Math3D.ProjectPointOntoSphere(o.Item1.X, o.Item1.Y, sphereRadius),
                    to = Math3D.ProjectPointOntoSphere(o.Item2.X, o.Item2.Y, sphereRadius),
                    tip = Math3D.ProjectPointOntoSphere(o.Item3.X, o.Item3.Y, sphereRadius),
                    bar = GetLinePoints(o.Item1, o.Item2, lineThickness, arrowThickness, sphereRadius),
                }).
                ToArray();

            Point3D origin = Math3D.ProjectPointOntoSphere(0, 0, sphereRadius);

            #endregion
            #region create triangles

            List<Triangle> triangles = new List<Triangle>();

            foreach (var line in lines)
            {
                triangles.Add(new Triangle(origin, line.bar.fromRight, line.bar.fromLeft));

                triangles.Add(new Triangle(line.bar.fromLeft, line.bar.fromRight, line.bar.toRight));
                triangles.Add(new Triangle(line.bar.toRight, line.bar.toLeft, line.bar.fromLeft));

                triangles.Add(new Triangle(line.bar.baseLeft, line.bar.toLeft, line.tip));
                triangles.Add(new Triangle(line.bar.toLeft, line.bar.toRight, line.tip));
                triangles.Add(new Triangle(line.bar.toRight, line.bar.baseRight, line.tip));
            }

            ITriangleIndexed[] indexedTriangles = TriangleIndexed.ConvertToIndexed(triangles.ToArray());

            #endregion

            List<Model3D> retVal = new List<Model3D>();

            MaterialGroup material = new MaterialGroup();
            material.Children.Add(new DiffuseMaterial(new SolidColorBrush(WorldColors.ImpulseEngine_Icon_Color)));
            material.Children.Add(WorldColors.ImpulseEngine_Icon_Specular);
            material.Children.Add(WorldColors.ImpulseEngine_Icon_Emissive);

            foreach (Transform3D transform in GetRotations_Tetrahedron(sphereRadius))
            {
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = material;
                geometry.BackMaterial = material;
                geometry.Geometry = UtilityWPF.GetMeshFromTriangles(indexedTriangles);
                geometry.Transform = transform;

                retVal.Add(geometry);
            }

            return retVal.ToArray();
        }

        /// <summary>
        /// This is used to rotate a 2D icon onto 4 evenly distributed points around a sphere
        /// </summary>
        private static Transform3D[] GetRotations_Tetrahedron(double radius)
        {
            List<Transform3D> retVal = new List<Transform3D>();

            Vector3D position = new Vector3D(radius, 0, 0);
            Vector3D right = new Vector3D(0, 1, 0);
            Vector3D up = new Vector3D(0, 0, 1);

            Tetrahedron tetra = UtilityWPF.GetTetrahedron(1);

            foreach (Point3D dir in tetra.AllPoints)
            {
                Quaternion randRot = Math3D.GetRotation(right, Math3D.GetArbitraryOrhonganal(position));        // give it a random spin so that the final icons aren't semi lined up
                Quaternion majorRot = Math3D.GetRotation(position, dir.ToVector());

                Transform3DGroup transform = new Transform3DGroup();
                transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(randRot)));
                transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(majorRot)));

                retVal.Add(transform);
            }

            return retVal.ToArray();
        }

        /// <summary>
        /// This is for making a 2D circle out of triangles.  This takes a point on the edge of a circle and gives the corresponding point
        /// on an inner and outer circle.  It then does a projection onto the surface of a sphere (the sphere should be at least 1.5 times
        /// larger than the drawn circle, or it will look bad)
        /// </summary>
        private static (Point3D inner, Point3D mid, Point3D outer) GetCirclePoints(Point pointOnCircle, double circleRadius, double thickness, double sphereRadius)
        {
            Vector asVect = pointOnCircle.ToVector();

            double half = thickness / 2;
            double innerRadius = Math.Max(0, circleRadius - half);
            double outerRadius = circleRadius + half;

            Vector inner = asVect * innerRadius;
            Vector mid = asVect * circleRadius;
            Vector outer = asVect * outerRadius;

            return
            (
                Math3D.ProjectPointOntoSphere(inner.X, inner.Y, sphereRadius),
                Math3D.ProjectPointOntoSphere(mid.X, mid.Y, sphereRadius),
                Math3D.ProjectPointOntoSphere(outer.X, outer.Y, sphereRadius)
            );
        }
        private static (Point3D fromLeft, Point3D fromRight, Point3D toLeft, Point3D toRight, Point3D baseLeft, Point3D baseRight) GetLinePoints(Vector from, Vector to, double shaftThickness, double arrowThickness, double sphereRadius)
        {
            Vector3D direction = (to - from).ToVector3D().ToUnit(false);

            Vector3D axis = new Vector3D(0, 0, 1);

            Vector left = direction.GetRotatedVector(axis, 90).ToVector2D();
            Vector right = direction.GetRotatedVector(axis, -90).ToVector2D();

            Point fromPoint = from.ToPoint();
            Point toPoint = to.ToPoint();

            double halfShaft = shaftThickness / 2;
            double halfArrow = arrowThickness / 2;

            Point fromLeft = fromPoint + (left * halfShaft);
            Point fromRight = fromPoint + (right * halfShaft);
            Point toLeft = toPoint + (left * halfShaft);
            Point toRight = toPoint + (right * halfShaft);
            Point baseLeft = toPoint + (left * halfArrow);
            Point baseRight = toPoint + (right * halfArrow);

            return
            (
                Math3D.ProjectPointOntoSphere(fromLeft.X, fromLeft.Y, sphereRadius),
                Math3D.ProjectPointOntoSphere(fromRight.X, fromRight.Y, sphereRadius),
                Math3D.ProjectPointOntoSphere(toLeft.X, toLeft.Y, sphereRadius),
                Math3D.ProjectPointOntoSphere(toRight.X, toRight.Y, sphereRadius),
                Math3D.ProjectPointOntoSphere(baseLeft.X, baseLeft.Y, sphereRadius),
                Math3D.ProjectPointOntoSphere(baseRight.X, baseRight.Y, sphereRadius)
            );
        }

        /// <summary>
        /// This takes a triangle, reduces its size, and creates sloped sides
        /// </summary>
        /// <param name="scale">This is the percent to reduce the incoming triangle by</param>
        /// <param name="normalHeight">
        /// A line is drawn from a point above the triangle to each vertex.  The smaller this height, the more angled out
        /// the beveled edges will be (if this height is infinity, the walls would be vertical)
        /// </param>
        /// <param name="sideDepth">
        /// How tall the edges should be (you want them deep enough that you don't see the bottom edge, but if too deep, they
        /// will poke through the back side of the sphere)
        /// </param>
        private static ITriangle[] GetBeveledTriangle(ITriangle triangle, double scale, double normalHeight, double sideDepth)
        {
            Point3D center = triangle.GetCenterPoint();

            Triangle outerPlate = new Triangle(
                center + ((triangle.Point0 - center) * scale),
                center + ((triangle.Point1 - center) * scale),
                center + ((triangle.Point2 - center) * scale));

            List<ITriangle> retVal = new List<ITriangle>();
            retVal.Add(outerPlate);
            retVal.AddRange(GetBeveledTriangle_Sides(outerPlate, normalHeight, sideDepth));

            return retVal.ToArray();
        }
        private static IEnumerable<ITriangle> GetBeveledTriangle_Sides(ITriangle triangle, double normalHeight, double sideDepth)
        {
            Point3D tip = triangle.GetCenterPoint() + (triangle.NormalUnit * normalHeight);

            Point3D[] extended = triangle.PointArray.
                Select(o => o + ((o - tip).ToUnit() * sideDepth)).
                ToArray();

            Point3D[] allPoints = triangle.
                PointArray.
                Concat(extended).
                ToArray();

            List<TriangleIndexed> retVal = new List<TriangleIndexed>();

            // 0-1
            retVal.Add(new TriangleIndexed(0, 1, 3, allPoints));
            retVal.Add(new TriangleIndexed(1, 4, 3, allPoints));

            // 1-2
            retVal.Add(new TriangleIndexed(1, 2, 4, allPoints));
            retVal.Add(new TriangleIndexed(2, 5, 4, allPoints));

            // 2-0
            retVal.Add(new TriangleIndexed(2, 0, 5, allPoints));
            retVal.Add(new TriangleIndexed(0, 3, 5, allPoints));

            return retVal;
        }

        #endregion
    }

    #endregion
    #region class: ImpulseEngine

    public class ImpulseEngine : PartBase, INeuronContainer, IPartUpdatable
    {
        #region Declaration Section

        public const string PARTTYPE = nameof(ImpulseEngine);

        private readonly ItemOptions _itemOptions;

        private readonly IContainer _plasmaTanks;

        private readonly DirectionControllerRing.NeuronShell _neuronsLinear;
        private readonly DirectionControllerRing.NeuronShell _neuronsRotation;
        private readonly Neuron_SensorPosition[] _neurons;

        // If this is true, then it ignores neurons
        private volatile bool _isManuallyControlled = false;
        private volatile (Vector3D? linear, Vector3D? torque)[] _linearsTorquesPercent = null;       // these are the percents that were last passed in (only used when _isManuallyControlled is true, and it overrides the neurons)

        #endregion

        #region Constructor

        public ImpulseEngine(EditorOptions options, ItemOptions itemOptions, ImpulseEngineDNA dna, IContainer plasmaTanks)
            : base(options, dna, itemOptions.ImpulseEngine_Damage.HitpointMin, itemOptions.ImpulseEngine_Damage.HitpointSlope, itemOptions.ImpulseEngine_Damage.Damage)
        {
            _itemOptions = itemOptions;
            _plasmaTanks = plasmaTanks;

            Design = new ImpulseEngineDesign(options, true, dna.ImpulseEngineType);
            Design.SetDNA(dna);

            GetMass(out _mass, out double volume, out double radius, out _scaleActual, dna, itemOptions);
            Radius = radius;

            _linearForceAtMax = volume * itemOptions.Impulse_LinearStrengthRatio * ItemOptions.IMPULSEENGINE_FORCESTRENGTHMULT;		//ImpulseStrengthRatio is stored as a lower value so that the user doesn't see such a huge number
            _rotationForceAtMax = volume * itemOptions.Impulse_RotationStrengthRatio * ItemOptions.IMPULSEENGINE_FORCESTRENGTHMULT;

            #region neurons

            double area = Math.Pow(radius, itemOptions.ImpulseEngine_NeuronGrowthExponent);

            int neuronCount = Convert.ToInt32(Math.Ceiling(itemOptions.ImpulseEngine_NeuronDensity_Half * area));
            if (neuronCount == 0)
            {
                neuronCount = 1;
            }

            List<Neuron_SensorPosition> allNeurons = new List<Neuron_SensorPosition>();

            if (dna.ImpulseEngineType == ImpulseEngineType.Both || dna.ImpulseEngineType == ImpulseEngineType.Translate)
            {
                _neuronsLinear = DirectionControllerRing.CreateNeuronShell_Sphere(1, neuronCount);
                allNeurons.AddRange(_neuronsLinear.Neurons);
            }
            else
            {
                _neuronsLinear = null;
            }

            if (dna.ImpulseEngineType == ImpulseEngineType.Both || dna.ImpulseEngineType == ImpulseEngineType.Rotate)
            {
                _neuronsRotation = DirectionControllerRing.CreateNeuronShell_Sphere(.4, neuronCount);
                allNeurons.AddRange(_neuronsRotation.Neurons);
            }
            else
            {
                _neuronsRotation = null;
            }

            _neurons = allNeurons.ToArray();

            #endregion
        }

        #endregion

        #region INeuronContainer Members

        public IEnumerable<INeuron> Neruons_Readonly => Enumerable.Empty<INeuron>();
        public IEnumerable<INeuron> Neruons_ReadWrite => Enumerable.Empty<INeuron>();
        public IEnumerable<INeuron> Neruons_Writeonly => _neurons;

        public IEnumerable<INeuron> Neruons_All => _neurons;

        public NeuronContainerType NeuronContainerType => NeuronContainerType.Manipulator;

        public double Radius
        {
            get;
            private set;
        }

        private volatile bool _isOn = true;
        public bool IsOn => _isOn;

        #endregion
        #region IPartUpdatable Members

        public void Update_MainThread(double elapsedTime)
        {
            // Update glowball
            var current = _thrustsTorquesLastUpdate;

            double percent = current == null ? 0d : current.Item1;

            ImpulseEngineDesign design = (ImpulseEngineDesign)this.Design;
            design.SetGlowballPercent(percent);
            design.RotateGlowball(elapsedTime);
        }
        public void Update_AnyThread(double elapsedTime)
        {
            //TODO: Lock
            (Vector3D? linear, Vector3D? rotate)? forceTorquePercent = null;
            if (_isManuallyControlled)
            {
                forceTorquePercent = GetForceTorquePercent_Manual(_linearsTorquesPercent, EngineType);      //NOTE: This never returns null.  If no force is desired, it returns a tuple containing nulls
            }
            else
            {
                forceTorquePercent = GetForceTorquePercent_Neural(_neuronsLinear, _neuronsRotation, EngineType);
            }

            // Get desired force, torque
            var forceTorque = GetForceTorque(forceTorquePercent?.linear, forceTorquePercent?.rotate, _linearForceAtMax, _rotationForceAtMax, elapsedTime, _itemOptions, IsDestroyed, _plasmaTanks);

            // Store percent, force, torque
            _thrustsTorquesLastUpdate = Tuple.Create(forceTorque.Item1, Tuple.Create(forceTorque.Item2, forceTorque.Item3));
        }

        public int? IntervalSkips_MainThread => 0;
        public int? IntervalSkips_AnyThread => 0;

        #endregion

        #region Public Properties

        private readonly double _mass;
        public override double DryMass => _mass;
        public override double TotalMass => _mass;

        private readonly Vector3D _scaleActual;
        public override Vector3D ScaleActual => _scaleActual;

        private volatile Tuple<double, Tuple<Vector3D?, Vector3D?>> _thrustsTorquesLastUpdate = null;
        public Tuple<Vector3D?, Vector3D?> ThrustsTorquesLastUpdate
        {
            get
            {
                var current = _thrustsTorquesLastUpdate;

                if (current == null)
                {
                    return null;
                }
                else
                {
                    return current.Item2;
                }
            }
        }

        private readonly double _linearForceAtMax;
        public double LinearForceAtMax => _linearForceAtMax;

        private readonly double _rotationForceAtMax;
        public double RotationForceAtMax => _rotationForceAtMax;

        public ImpulseEngineType EngineType => ((ImpulseEngineDesign)Design).EngineType;

        #endregion

        #region Public Methods

        /// <summary>
        /// If SetDesiredDirection was previously called, then calling this will make the engine listen to the neurons again
        /// </summary>
        public void EndManualControl()
        {
            _isManuallyControlled = false;
            _thrustsTorquesLastUpdate = null;
        }

        /// <summary>
        /// Setting this will run the impulse engine directly.  Neurons will be ignored.  Pass in null if you don't want to
        /// apply any force
        /// NOTE: If the impulse engine type is setup up as linear only or rotate only, then the other value will be ignored, even if non null is passed in
        /// </summary>
        /// <param name="linearsTorques">
        /// Item1 = linear force percent (length from 0 to 1)
        /// Item2 = torque percent (length from 0 to 1)
        /// </param>
        public void SetDesiredDirection((Vector3D? linear, Vector3D? torque)[] linearsTorquesPercent)
        {
            _isManuallyControlled = true;
            _linearsTorquesPercent = linearsTorquesPercent;
        }

        #endregion

        #region Private Methods

        //NOTE: This doesn't cap the percents
        private static (Vector3D? linear, Vector3D? rotate) GetForceTorquePercent_Manual((Vector3D? linear, Vector3D? torque)[] linearsTorquesPercent, ImpulseEngineType engineType)
        {
            if (linearsTorquesPercent == null)
            {
                return (null, null);
            }

            bool hasLinear = engineType == ImpulseEngineType.Both || engineType == ImpulseEngineType.Translate;
            bool hasRotation = engineType == ImpulseEngineType.Both || engineType == ImpulseEngineType.Rotate;

            Vector3D? linear = null;
            Vector3D? rotation = null;

            foreach (var item in linearsTorquesPercent)
            {
                // Linear
                if (hasLinear && item.linear != null)
                {
                    if (linear == null)
                    {
                        linear = item.linear;
                    }
                    else
                    {
                        linear = linear.Value + item.linear;
                    }
                }

                // Torque
                if (hasRotation && item.torque != null)
                {
                    if (rotation == null)
                    {
                        rotation = item.torque;
                    }
                    else
                    {
                        rotation = rotation.Value + item.torque;
                    }
                }
            }

            return (linear, rotation);
        }
        private static (Vector3D? linear, Vector3D? rotate) GetForceTorquePercent_Neural(DirectionControllerRing.NeuronShell linear, DirectionControllerRing.NeuronShell rotation, ImpulseEngineType engineType)
        {
            Vector3D? linearPercent = null;
            if (engineType == ImpulseEngineType.Both || engineType == ImpulseEngineType.Translate)
            {
                linearPercent = linear?.GetVector();
            }

            Vector3D? rotatePercent = null;
            if (engineType == ImpulseEngineType.Both || engineType == ImpulseEngineType.Rotate)
            {
                rotatePercent = rotation?.GetVector();
            }

            return (linearPercent, rotatePercent);
        }

        private static (double glowballSizePercent, Vector3D? linear, Vector3D? torque) GetForceTorque(Vector3D? linearPercent, Vector3D? rotationPercent, double linearForceAtMax, double rotationForceAtMax, double elapsedTime, ItemOptions itemOptions, bool isDetroyed, IContainer plasmaTanks)
        {
            if (isDetroyed || plasmaTanks == null)
            {
                return (0d, null, null);
            }

            // Cap percents
            double linearPecentLength = CapPercent(ref linearPercent);
            double rotationPercentLength = CapPercent(ref rotationPercent);
            if ((linearPecentLength + rotationPercentLength).IsNearZero())
            {
                // No force desired
                return (0d, null, null);
            }

            #region convert % to force

            // Figure out how much force will be generated

            Vector3D? actualLinearForce = null;
            double actualLinearForceLength = 0;
            if (linearPercent != null)
            {
                actualLinearForce = linearPercent * linearForceAtMax;
                actualLinearForceLength = actualLinearForce.Value.Length;
            }

            Vector3D? actualRotationForce = null;
            double actualRotationForceLength = 0;
            if (rotationPercent != null)
            {
                actualRotationForce = rotationPercent * rotationForceAtMax;
                actualRotationForceLength = actualRotationForce.Value.Length;
            }

            #endregion

            // See how much plasma that will take
            double plasmaToUse = (actualLinearForceLength + actualRotationForceLength) * elapsedTime * itemOptions.ImpulseEngine_PlasmaToThrustRatio * ItemOptions.IMPULSEENGINE_PLASMATOTHRUSTMULT;       // PlasmaToThrustRatio is stored as a larger value so the user can work with it easier

            // Try to pull that much plasma
            double plasmaUnused = plasmaTanks.RemoveQuantity(plasmaToUse, false);

            #region reduce if not enough plasma

            if (plasmaUnused.IsNearValue(plasmaToUse))
            {
                // No plasma
                return (0d, null, null);
            }
            else if (plasmaUnused > 0d)
            {
                // Not enough plasma, reduce the amount of force
                double reducePercent = plasmaUnused / plasmaToUse;

                double ratioLinear = actualLinearForceLength / (actualLinearForceLength + actualRotationForceLength);

                if (actualLinearForce != null)
                {
                    double percent = reducePercent * ratioLinear;
                    actualLinearForce = actualLinearForce.Value * percent;
                    actualLinearForceLength *= percent;
                    linearPecentLength *= percent;
                }

                if (actualRotationForce != null)
                {
                    double percent = reducePercent * (1 - ratioLinear);
                    actualRotationForce = actualRotationForce.Value * percent;
                    actualRotationForceLength *= percent;
                    rotationPercentLength *= percent;
                }
            }

            #endregion

            #region final percent

            //NOTE: This percent is just used to scale the glow ball, so isn't important for actual force generation

            // Figure out the final percent.  At the least, it should be the max of the two
            double finalPercent = Math.Max(linearPecentLength, rotationPercentLength);

            // I don't think it should be the sum, but should be more if both rotation and linear are firing at the same time.
            // What if linear and rotation are each 66%?  Should final be 66%, 100%, or a bit more than 66%?

            // This is adding on 25% of the lesser force (just an arbitrary amount of extra)
            double addition = Math.Min(linearPecentLength, rotationPercentLength) * .25;
            finalPercent += addition;

            if (finalPercent > 1)
            {
                finalPercent = 1;
            }

            #endregion

            return (finalPercent, actualLinearForce, actualRotationForce);
        }

        private static double CapPercent(ref Vector3D? percent)
        {
            double retVal = 0d;

            if (percent != null)
            {
                retVal = percent.Value.Length;
                if (retVal > 1)
                {
                    retVal = 1;
                    percent = percent.Value.ToUnit(false);
                }
            }

            return retVal;
        }

        private static void GetMass(out double mass, out double volume, out double radius, out Vector3D actualScale, ShipPartDNA dna, ItemOptions itemOptions)
        {
            radius = (dna.Scale.X + dna.Scale.Y + dna.Scale.Z) / (3d * 2d);     // they should be identical anyway

            actualScale = new Vector3D(radius * 2d, radius * 2d, radius * 2d);

            volume = 4d / 3d * Math.PI * radius * radius * radius;
            mass = volume * itemOptions.ImpulseEngine_Density;
        }

        #endregion
    }

    #endregion

    #region class: ImpulseEngineDNA

    public class ImpulseEngineDNA : ShipPartDNA
    {
        public ImpulseEngineType ImpulseEngineType { get; set; }
    }

    #endregion

    #region enum: ImpulseEngineType

    public enum ImpulseEngineType
    {
        Translate,
        Rotate,
        Both,
    }

    #endregion
}
