﻿using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.NewtonDynamics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Game.Newt.v2.GameItems.ShipParts
{
    //TODO: Rename this to BrainFixed

    //TODO: This brain stays static over its entire life.  Make another brain that has a shell of neurons that are exposed publicly and stay static (just like
    //all of this brain's neurons).  Then have a private interior that will change based on life experiences

    //TODO: Make a class called BrainGroup, which will be a high level wrapper.
    //Its scale decides how much/many actual brains to create (in its volume).
    //It will decide which actual brains to create based on the parts nearby -- so if there's a camera nearby, it would instantiate a specialized vison brain,
    //and wire that to the camera.  If there's a thruster nearby, it may instantiate a controller, etc

    #region class: BrainToolItem

    public class BrainToolItem : PartToolItemBase
    {
        #region Constructor

        public BrainToolItem(EditorOptions options)
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
                return "Brain";
            }
        }
        public override string Description
        {
            get
            {
                return "Consumes energy, makes decisions :)";
            }
        }
        public override string Category
        {
            get
            {
                return PartToolItemBase.CATEGORY_BRAIN;
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
            return new BrainDesign(this.Options, false);
        }

        #endregion
    }

    #endregion
    #region class: BrainDesign

    public class BrainDesign : PartDesignBase
    {
        #region Declaration Section

        private const double HEIGHT = 1d;
        internal const double SCALE = .75d / HEIGHT;

        public const PartDesignAllowedScale ALLOWEDSCALE = PartDesignAllowedScale.XYZ;		// This is here so the scale can be known through reflection

        private MassBreakdownCache _massBreakdown = null;

        #endregion

        #region Constructor

        public BrainDesign(EditorOptions options, bool isFinalModel)
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

        private Model3DGroup _model = null;
        public override Model3D Model
        {
            get
            {
                if (_model == null)
                {
                    _model = CreateGeometry(this.IsFinalModel);
                }

                return _model;
            }
        }

        // It's a bit silly to store this in design - it was only used by this.GetDNA, but that method
        // is more for the ship editor, which doesn't care about neurons.  When full dna is needed,
        // Brain.GetNewDNA should be used
        //public Point3D[] NeuronLocations
        //{
        //    get;
        //    set;
        //}

        #endregion

        #region Public Methods

        public override CollisionHull CreateCollisionHull(WorldBase world)
        {
            Transform3DGroup transform = new Transform3DGroup();
            //transform.Children.Add(new ScaleTransform3D(scale));		// it ignores scale
            transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(this.Orientation)));
            transform.Children.Add(new TranslateTransform3D(this.Position.ToVector()));

            Vector3D size = new Vector3D(this.Scale.X * SCALE * .5d, this.Scale.Y * SCALE * .5d, this.Scale.Z * SCALE * .5d);

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
            Vector3D size = new Vector3D(this.Scale.X * SCALE, this.Scale.Y * SCALE, this.Scale.Z * SCALE);

            var breakdown = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Sphere, UtilityNewt.MassDistribution.Uniform, size, cellSize);

            // Store this
            _massBreakdown = new MassBreakdownCache(breakdown, Scale, cellSize);

            return _massBreakdown.Breakdown;
        }

        public override PartToolItemBase GetToolItem()
        {
            return new BrainToolItem(this.Options);
        }

        public static Model3D CreateShellVisual(bool isFinal, List<MaterialColorProps> materialBrushes, List<EmissiveMaterial> selectionEmissives, ScaleTransform3D scaleTransform)
        {
            GeometryModel3D geometry = new GeometryModel3D();
            MaterialGroup material = new MaterialGroup();
            Color shellColor = WorldColors.Brain_Color;
            if (!isFinal)
            {
                shellColor = UtilityWPF.AlphaBlend(shellColor, Colors.Transparent, .75d);
            }
            DiffuseMaterial diffuse = new DiffuseMaterial(new SolidColorBrush(shellColor));
            materialBrushes.Add(new MaterialColorProps(diffuse, shellColor));
            material.Children.Add(diffuse);

            SpecularMaterial specular = WorldColors.Brain_Specular;
            materialBrushes.Add(new MaterialColorProps(specular));
            material.Children.Add(specular);

            if (!isFinal)
            {
                EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
                material.Children.Add(selectionEmissive);
                selectionEmissives.Add(selectionEmissive);
            }

            geometry.Material = material;
            geometry.BackMaterial = material;

            Transform3DGroup transformGroup = new Transform3DGroup();
            transformGroup.Children.Add(scaleTransform);
            transformGroup.Children.Add(new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRandomRotation())));		// this is just so it's not obvious that the brains are shared visuals

            geometry.Geometry = SharedVisuals.BrainMesh;		// SharedVisuals keeps track of which thread made the request
            geometry.Transform = transformGroup;

            return geometry;
        }
        public static Model3D[] CreateInsideVisuals(double radius, List<MaterialColorProps> materialBrushes, List<EmissiveMaterial> selectionEmissives, ScaleTransform3D scaleTransform)
        {
            List<Point3D[]> insidePoints = new List<Point3D[]>();
            for (int cntr = 0; cntr < 3; cntr++)
            {
                GetLineBranch(insidePoints, Math3D.GetRandomVector_Spherical(radius).ToPoint(), radius, radius * .8d, .33d, 4);
            }

            Random rand = StaticRandom.GetRandomForThread();

            List<Model3D> retVal = new List<Model3D>();

            foreach (Point3D[] lineSegment in insidePoints)
            {
                GeometryModel3D geometry = new GeometryModel3D();
                MaterialGroup material = new MaterialGroup();

                Color color = WorldColors.BrainInsideStrand_Color;		// storing this, because it's random
                DiffuseMaterial diffuse = new DiffuseMaterial(new SolidColorBrush(color));
                materialBrushes.Add(new MaterialColorProps(diffuse, color));
                material.Children.Add(diffuse);

                SpecularMaterial specular = WorldColors.BrainInsideStrand_Specular;
                materialBrushes.Add(new MaterialColorProps(specular));
                material.Children.Add(specular);

                //if (!isFinal)
                //{
                EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
                material.Children.Add(selectionEmissive);
                selectionEmissives.Add(selectionEmissive);
                //}

                geometry.Material = material;
                geometry.BackMaterial = material;

                Vector3D line = lineSegment[1] - lineSegment[0];
                double lineLength = line.Length;
                double halfLength = lineLength * .5d;
                double widestWidth = lineLength * .033d;

                List<TubeRingBase> rings = new List<TubeRingBase>();
                rings.Add(new TubeRingPoint(0, false));
                rings.Add(new TubeRingRegularPolygon(halfLength, false, widestWidth, widestWidth, false));
                rings.Add(new TubeRingPoint(halfLength, false));

                Quaternion zRot = new Quaternion(new Vector3D(0, 0, 1), 360d * rand.NextDouble()).ToUnit();
                Quaternion rotation = Math3D.GetRotation(new Vector3D(0, 0, 1), line).ToUnit();

                Transform3DGroup transformGroup = new Transform3DGroup();
                transformGroup.Children.Add(new RotateTransform3D(new QuaternionRotation3D(Quaternion.Multiply(rotation, zRot))));
                transformGroup.Children.Add(new TranslateTransform3D(lineSegment[0].ToVector()));
                transformGroup.Children.Add(scaleTransform);

                geometry.Geometry = UtilityWPF.GetMultiRingedTube(3, rings, true, false, transformGroup);

                retVal.Add(geometry);
            }

            return retVal.ToArray();
        }

        #endregion

        #region Private Methods

        private Model3DGroup CreateGeometry(bool isFinal)
        {
            const double INSIDEPOINTRADIUS = .45d;

            ScaleTransform3D scaleTransform = new ScaleTransform3D(SCALE, SCALE, SCALE);

            Model3DGroup retVal = new Model3DGroup();

            // Insides
            if (!isFinal)
            {
                Model3D[] insideModels = CreateInsideVisuals(INSIDEPOINTRADIUS, this.MaterialBrushes, base.SelectionEmissives, scaleTransform);
                retVal.Children.AddRange(insideModels);
            }

            #region lights

            // Neat effect, but it makes my fan spin up, and won't slow back down.  Need to add an animation property to the options
            // class (and listen for when it toggles)

            //if (!isFinal)
            //{
            //    int numLights = 1 + this.Options.Random.Next(3);

            //    for (int cntr = 0; cntr < numLights; cntr++)
            //    {
            //        PointLight light = new PointLight();
            //        light.Color = Colors.Black;
            //        light.Range = SCALE * INSIDEPOINTRADIUS * 2d;
            //        light.LinearAttenuation = 1d;

            //        transformGroup = new Transform3DGroup();
            //        transformGroup.Children.Add(new TranslateTransform3D(Math3D.GetRandomVectorSpherical(this.Options.Random, INSIDEPOINTRADIUS)));
            //        transformGroup.Children.Add(scaleTransform);
            //        light.Transform = transformGroup;

            //        retVal.Children.Add(light);

            //        ColorAnimation animation = new ColorAnimation();
            //        animation.From = UtilityWPF.ColorFromHex("CC1266");
            //        animation.To = Colors.Black;
            //        animation.Duration = new Duration(TimeSpan.FromSeconds(1d + (this.Options.Random.NextDouble() * 5d)));
            //        animation.AutoReverse = true;
            //        animation.RepeatBehavior = RepeatBehavior.Forever;
            //        animation.AccelerationRatio = .5d;
            //        animation.DecelerationRatio = .5d;

            //        light.BeginAnimation(PointLight.ColorProperty, animation);
            //    }
            //}

            #endregion

            // Outer Shell
            Model3D geometry = CreateShellVisual(isFinal, this.MaterialBrushes, base.SelectionEmissives, scaleTransform);
            retVal.Children.Add(geometry);

            // Transform
            retVal.Transform = GetTransformForGeometry(isFinal);

            // Exit Function
            return retVal;
        }

        /// <summary>
        /// This is a recursive method that adds a line to the point passed in, and has a chance of branching
        /// </summary>
        private static void GetLineBranch(List<Point3D[]> resultPoints, Point3D fromPoint, double radius, double maxDistFromPoint, double splitProbability, int remaining)
        {
            if (remaining < 0)
            {
                return;
            }

            int numBranches = 1;

            // See if this should do a split
            if (StaticRandom.NextDouble() < splitProbability)
            {
                numBranches = StaticRandom.NextDouble() > .8 ? 3 : 2;
            }

            for (int cntr = 0; cntr < numBranches; cntr++)
            {
                // Add a line
                Vector3D toPoint;
                do
                {
                    toPoint = (fromPoint + Math3D.GetRandomVector_Spherical(maxDistFromPoint)).ToVector();
                } while (toPoint.LengthSquared > radius * radius);

                resultPoints.Add(new Point3D[] { fromPoint, toPoint.ToPoint() });

                double newMaxDist = maxDistFromPoint * .85d;

                // Make the next call have a higher chance of branching
                double newSplitProbability = 1d - ((1d - splitProbability) / 1.2d);

                // Recurse
                GetLineBranch(resultPoints, toPoint.ToPoint(), radius, newMaxDist, newSplitProbability, remaining - 1);
            }
        }

        #endregion
    }

    #endregion
    #region class: Brain

    public class Brain : PartBase, INeuronContainer, IPartUpdatable
    {
        #region Declaration Section

        public const string PARTTYPE = nameof(Brain);

        private readonly ItemOptions _itemOptions;

        private readonly IContainer _energyTanks;

        /// <summary>
        /// These are the standard read/write neurons that you think of when you think of brains
        /// </summary>
        private readonly Neuron_NegPos[] _neurons;

        /// <summary>
        /// These represent brain chemicals, and are implemented as writeonly neurons
        /// </summary>
        private readonly Neuron_Fade[] _brainChemicals;

        private readonly double _volume;		// this is used to calculate energy draw

        #endregion

        #region Constructor

        public Brain(EditorOptions options, ItemOptions itemOptions, ShipPartDNA dna, IContainer energyTanks)
            : base(options, dna, itemOptions.Brain_Damage.HitpointMin, itemOptions.Brain_Damage.HitpointSlope, itemOptions.Brain_Damage.Damage)
        {
            _itemOptions = itemOptions;
            _energyTanks = energyTanks;

            Design = new BrainDesign(options, true);
            Design.SetDNA(dna);

            // Build the neurons (not doing the links yet - or maybe do the internal links?)
            _brainChemicals = CreateBrainChemicals(dna, itemOptions);
            _neurons = CreateNeurons(dna, itemOptions, _brainChemicals.Select(o => o.Position).ToArray());
            //_design.NeuronLocations = _neurons.Select(o => o.Position).ToArray();

            GetMass(out _mass, out _volume, out double radius, out _scaleActual, dna, itemOptions);
            Radius = radius;
        }

        #endregion

        #region INeuronContainer Members

        public IEnumerable<INeuron> Neruons_Readonly => Enumerable.Empty<INeuron>();
        public IEnumerable<INeuron> Neruons_ReadWrite => _neurons;
        public IEnumerable<INeuron> Neruons_Writeonly => _brainChemicals;

        public IEnumerable<INeuron> Neruons_All => UtilityCore.Iterate<INeuron>(_neurons, _brainChemicals);

        public NeuronContainerType NeuronContainerType => NeuronContainerType.Brain_Standalone;

        public double Radius
        {
            get;
            private set;
        }

        private volatile bool _isOn = false;
        public bool IsOn => _isOn;

        #endregion
        #region IPartUpdatable Members

        public void Update_MainThread(double elapsedTime)
        {
        }
        public void Update_AnyThread(double elapsedTime)
        {
            //NOTE: NeuralOperation.Tick is handling the actual calculation of the neurons for the whole ship at a time.  This is more efficient than
            //having each part working independently (it processes the neurons in a random order each tick).  NeuralOperation.Tick is running in
            //a separate thread, and doesn't deal with energy draw, so this.IsOn is a loose compromise.  That tick could fire several times for each
            //one of this.Update, or the other way around.  But in the long run, they should be fairly synced.

            if (IsDestroyed ||
                _energyTanks == null ||
                _energyTanks.RemoveQuantity(elapsedTime * _volume * _itemOptions.Brain_AmountToDraw * ItemOptions.ENERGYDRAWMULT, true) > 0d)
            {
                // The energy tank didn't have enough
                //NOTE: To be clean, I should set the neuron outputs to zero, but anything pulling from them should be checking this anyway.  So
                //save the processor (also, because of threading, setting them to zero isn't ast atomic as this property).  Besides, NeuralOperation.Tick
                //will set them to zero when this is off.
                _isOn = false;
            }
            else
            {
                //TODO: May want to expose a unique writeonly neuron that can turn the brain off.  This would only be useful for multi brain bots.
                // And would only be useful as an energy saving measure.  (switch off most brains when energy level falls below 25% to conserve energy)
                //
                // This is sort of an extreme version of brain chemicals: You could have some specialty brains that only switch on during certain cases
                // (horny mode), but that functionality should already be available through brain chemicals.  (but would be more energy efficient)
                //if (_isOffNeuron.Value > .95d)
                //{
                //    _isOn = false;
                //}

                _isOn = true;
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

        public int BrainChemicalCount
        {
            get
            {
                return _brainChemicals.Length;
            }
        }

        #endregion

        #region Public Methods

        public override ShipPartDNA GetNewDNA()
        {
            ShipPartDNA retVal = this.Design.GetDNA();

            //NOTE: The design class doesn't hold neurons, since it's only used by the editor, so fill out the rest of the dna here
            retVal.Neurons = _neurons.Select(o => o.Position).ToArray();
            retVal.AltNeurons = new Point3D[1][];
            retVal.AltNeurons[0] = _brainChemicals.Select(o => o.Position).ToArray();

            return retVal;
        }

        /// <summary>
        /// This returns the current value of the requested brain chemical
        /// NOTE: This method IS threadsafe
        /// </summary>
        public double GetBrainChemicalValue(int index)
        {
            // See if there are enough brain chemicals for the request
            if (index < _brainChemicals.Length)		// no need for a lock.  The array will stay the same size for the lifetime of this brain class
            {
                return _brainChemicals[index].Value;		// Value is volatile, so it's threadsafe
            }
            else
            {
                return 0;
            }
        }

        #endregion

        #region Private Methods

        internal static void GetMass(out double mass, out double volume, out double radius, out Vector3D scaleActual, ShipPartDNA dna, ItemOptions itemOptions)
        {
            radius = (dna.Scale.X + dna.Scale.Y + dna.Scale.Z) / (3d * 2d);		// they should be identical anyway
            radius *= BrainDesign.SCALE;		// scale it

            volume = 4d / 3d * Math.PI * radius * radius * radius;
            mass = volume * itemOptions.Brain_Density;

            scaleActual = new Vector3D(radius * 2d, radius * 2d, radius * 2d);
        }

        private static Neuron_Fade[] CreateBrainChemicals(ShipPartDNA dna, ItemOptions itemOptions)
        {
            const double K_UP = 50d;
            const double K_DOWN = 750d;
            const double VALUECUTOFF = .75d;

            // Figure out how many to make
            double radius, volume;
            GetNeuronVolume(out radius, out volume, dna, itemOptions);

            int count = Convert.ToInt32(Math.Round(itemOptions.Brain_ChemicalDensity * volume));
            if (count == 0)
            {
                return new Neuron_Fade[0];
            }

            // The brain chemicals are stored in dna.AltNeurons
            Point3D[] brainChemPositions = null;
            if (dna.AltNeurons != null && dna.AltNeurons.Length > 0)
            {
                if (dna.AltNeurons.Length != 1)
                {
                    throw new ApplicationException("dna.AltNeurons.Length should be exactly 1 (" + dna.AltNeurons.Length.ToString() + ")");
                }

                brainChemPositions = dna.AltNeurons[0];
            }

            // Figure out the positions
            //NOTE: Only let them go to half radius.  Cluster% then needs to be doubled (doubling it again so that the brain chemicals don't get
            //too close together)
            Vector3D[] positions = NeuralUtility.GetNeuronPositions_Spherical_Cluster(brainChemPositions, count, radius * .5d, itemOptions.Brain_NeuronMinClusterDistPercent * 4d);

            // Exit Function
            return positions.Select(o => new Neuron_Fade(o.ToPoint(), K_UP, K_DOWN, VALUECUTOFF)).ToArray();
        }
        private static Neuron_NegPos[] CreateNeurons(ShipPartDNA dna, ItemOptions itemOptions, Point3D[] brainChemicalPositions)
        {
            // Figure out how many to make
            GetNeuronVolume(out double radius, out double volume, dna, itemOptions);

            int count = Convert.ToInt32(Math.Round(itemOptions.Brain_NeuronDensity * volume));
            if (count == 0)
            {
                count = 1;
            }

            // Figure out the positions
            Vector3D[] positions = NeuralUtility.GetNeuronPositions_Spherical_Cluster(dna.Neurons, brainChemicalPositions, 3d, count, radius, itemOptions.Brain_NeuronMinClusterDistPercent);

            return positions.
                Select(o => new Neuron_NegPos(o.ToPoint())).
                ToArray();
        }

        private static void GetNeuronVolume(out double radius, out double volume, ShipPartDNA dna, ItemOptions itemOptions)
        {
            //NOTE: This radius isn't taking SCALE into account.  The other neural parts do this as well, so the neural density properties can be more consistent
            radius = (dna.Scale.X + dna.Scale.Y + dna.Scale.Z) / (3d * 2d);		// xyz should all be the same anyway
            volume = Math.Pow(radius, itemOptions.Brain_NeuronGrowthExponent);
        }

        #endregion
    }

    #endregion
}
