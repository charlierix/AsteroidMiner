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
    #region Class: ImpulseEngineToolItem

    public class ImpulseEngineToolItem : PartToolItemBase
    {
        #region Constructor

        public ImpulseEngineToolItem(EditorOptions options)
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
                return "Impulse Engine";
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
            return new ImpulseEngineDesign(this.Options, false);
        }

        #endregion
    }

    #endregion
    #region Class: ImpulseEngineDesign

    public class ImpulseEngineDesign : PartDesignBase
    {
        #region Class: GlowBall

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

        private Tuple<UtilityNewt.IObjectMassBreakdown, Vector3D, double> _massBreakdown = null;

        private GlowBall _glowBall = null;

        #endregion

        #region Constructor

        public ImpulseEngineDesign(EditorOptions options, bool isFinalModel)
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
            if (_massBreakdown != null && _massBreakdown.Item2 == this.Scale && _massBreakdown.Item3 == cellSize)
            {
                // This has already been built for this size
                return _massBreakdown.Item1;
            }

            // Convert this.Scale into a size that the mass breakdown will use
            Vector3D size = new Vector3D(this.Scale.X * SIZEPERCENTOFSCALE, this.Scale.Y * SIZEPERCENTOFSCALE, this.Scale.Z * SIZEPERCENTOFSCALE);

            var breakdown = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Sphere, UtilityNewt.MassDistribution.Uniform, size, cellSize);

            // Store this
            _massBreakdown = new Tuple<UtilityNewt.IObjectMassBreakdown, Vector3D, double>(breakdown, this.Scale, cellSize);

            // Exit Function
            return _massBreakdown.Item1;
        }

        public override PartToolItemBase GetToolItem()
        {
            return new ImpulseEngineToolItem(this.Options);
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
            double BEVELSIDEDEPTH = .5;     // go down roughly the radius of the sphere (the walls also extend out, so it's not just radius)

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
            geometry.Transform = new ScaleTransform3D(SIZEPERCENTOFSCALE * .5, SIZEPERCENTOFSCALE * .5, SIZEPERCENTOFSCALE * .5);       // reducing by .5, because the sphere was created with radius 1 (which would make the diameter 2)

            group.Children.Add(geometry);

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
    #region Class: ImpulseEngine

    public class ImpulseEngine : PartBase, INeuronContainer, IPartUpdatable
    {
        #region Declaration Section

        public const string PARTTYPE = "ImpulseEngine";

        private readonly ItemOptions _itemOptions;

        private readonly IContainer _plasmaTanks;

        private readonly DirectionControllerRing.NeuronShell _neuronsLinear;
        private readonly DirectionControllerRing.NeuronShell _neuronsRotation;
        private readonly Neuron_SensorPosition[] _neurons;

        // If this is true, then it ignores neurons
        private volatile bool _isManuallyControlled = false;
        private volatile Tuple<Vector3D?, Vector3D?>[] _linearsTorquesPercent = null;       // these are the percents that were last passed in (only used when _isManuallyControlled is true, and it overrides the neurons)

        #endregion

        #region Constructor

        public ImpulseEngine(EditorOptions options, ItemOptions itemOptions, ShipPartDNA dna, IContainer plasmaTanks)
            : base(options, dna, itemOptions.ImpulseEngine_Damage.HitpointMin, itemOptions.ImpulseEngine_Damage.HitpointSlope, itemOptions.ImpulseEngine_Damage.Damage)
        {
            _itemOptions = itemOptions;
            _plasmaTanks = plasmaTanks;

            this.Design = new ImpulseEngineDesign(options, true);
            this.Design.SetDNA(dna);

            double radius, volume;
            GetMass(out _mass, out volume, out radius, out _scaleActual, dna, itemOptions);
            this.Radius = radius;

            _linearForceAtMax = volume * itemOptions.Impulse_LinearStrengthRatio * ItemOptions.IMPULSEENGINE_FORCESTRENGTHMULT;		//ImpulseStrengthRatio is stored as a lower value so that the user doesn't see such a huge number
            _rotationForceAtMax = volume * itemOptions.Impulse_RotationStrengthRatio * ItemOptions.IMPULSEENGINE_FORCESTRENGTHMULT;

            #region neurons

            int neuronCount = Convert.ToInt32(Math.Ceiling(itemOptions.ImpulseEngine_NeuronDensity_Half * volume));
            if (neuronCount == 0)
            {
                neuronCount = 1;
            }

            _neuronsLinear = DirectionControllerRing.CreateNeuronShell_Sphere(1, neuronCount);
            _neuronsRotation = DirectionControllerRing.CreateNeuronShell_Sphere(.4, neuronCount);

            _neurons = _neuronsLinear.Neurons.
                Concat(_neuronsRotation.Neurons).
                ToArray();

            #endregion
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
            Tuple<Vector3D?, Vector3D?> forceTorquePercent = null;
            if (_isManuallyControlled)
            {
                forceTorquePercent = GetForceTorquePercent_Manual(_linearsTorquesPercent);      //NOTE: This never returns null.  If no force is desired, it returns a tuple containing nulls
            }
            else
            {
                forceTorquePercent = GetForceTorquePercent_Neural(_neuronsLinear, _neuronsRotation);
            }

            // Get desired force, torque
            Tuple<double, Vector3D?, Vector3D?> forceTorque = GetForceTorque(forceTorquePercent.Item1, forceTorquePercent.Item2, _linearForceAtMax, _rotationForceAtMax, elapsedTime, _itemOptions, this.IsDestroyed, _plasmaTanks);

            // Store percent, force, torque
            _thrustsTorquesLastUpdate = Tuple.Create(forceTorque.Item1, Tuple.Create(forceTorque.Item2, forceTorque.Item3));
        }

        public int? IntervalSkips_MainThread
        {
            get
            {
                return 0;
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
        public double LinearForceAtMax
        {
            get
            {
                return _linearForceAtMax;
            }
        }

        private readonly double _rotationForceAtMax;
        public double RotationForceAtMax
        {
            get
            {
                return _rotationForceAtMax;
            }
        }

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
        /// </summary>
        /// <param name="linearsTorques">
        /// Item1 = linear force percent (length from 0 to 1)
        /// Item2 = torque percent (length from 0 to 1)
        /// </param>
        public void SetDesiredDirection(Tuple<Vector3D?, Vector3D?>[] linearsTorquesPercent)
        {
            _isManuallyControlled = true;
            _linearsTorquesPercent = linearsTorquesPercent;
        }

        #endregion

        #region Private Methods

        //NOTE: This doesn't cap the percents
        private static Tuple<Vector3D?, Vector3D?> GetForceTorquePercent_Manual(Tuple<Vector3D?, Vector3D?>[] linearsTorquesPercent)
        {
            if (linearsTorquesPercent == null)
            {
                return new Tuple<Vector3D?, Vector3D?>(null, null);
            }

            Vector3D? linear = null;
            Vector3D? rotation = null;

            foreach (var item in linearsTorquesPercent)
            {
                if (item == null)
                {
                    continue;
                }

                // Linear
                if (item.Item1 != null)
                {
                    if (linear == null)
                    {
                        linear = item.Item1;
                    }
                    else
                    {
                        linear = linear.Value + item.Item1;
                    }
                }

                // Torque
                if (item.Item2 != null)
                {
                    if (rotation == null)
                    {
                        rotation = item.Item2;
                    }
                    else
                    {
                        rotation = rotation.Value + item.Item2;
                    }
                }
            }

            return Tuple.Create(linear, rotation);
        }
        private static Tuple<Vector3D?, Vector3D?> GetForceTorquePercent_Neural(DirectionControllerRing.NeuronShell linear, DirectionControllerRing.NeuronShell rotation)
        {
            return new Tuple<Vector3D?, Vector3D?>(
                linear.GetVector(),
                rotation.GetVector());
        }

        private static Tuple<double, Vector3D?, Vector3D?> GetForceTorque(Vector3D? linearPercent, Vector3D? rotationPercent, double linearForceAtMax, double rotationForceAtMax, double elapsedTime, ItemOptions itemOptions, bool isDetroyed, IContainer plasmaTanks)
        {
            if (isDetroyed || plasmaTanks == null)
            {
                return new Tuple<double, Vector3D?, Vector3D?>(0d, null, null);
            }

            // Cap percents
            double linearPecentLength = CapPercent(ref linearPercent);
            double rotationPercentLength = CapPercent(ref rotationPercent);
            if ((linearPecentLength + rotationPercentLength).IsNearZero())
            {
                // No force desired
                return new Tuple<double, Vector3D?, Vector3D?>(0d, null, null);
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
                return new Tuple<double, Vector3D?, Vector3D?>(0d, null, null);
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

            return Tuple.Create(finalPercent, actualLinearForce, actualRotationForce);
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
}
