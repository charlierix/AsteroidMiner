using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using Game.HelperClasses;
using Game.Newt.AsteroidMiner2.ShipEditor;
using Game.Newt.HelperClasses;
using Game.Newt.NewtonDynamics;
using Game.Newt.AsteroidMiner2.MapParts;

namespace Game.Newt.AsteroidMiner2.ShipParts
{
    #region Class: CargoBayToolItem

    public class CargoBayToolItem : PartToolItemBase
    {
        #region Constructor

        public CargoBayToolItem(EditorOptions options)
            : base(options)
        {
            _visual2D = PartToolItemBase.GetVisual2D(this.Name, this.Description, options.EditorColors);
            this.TabName = PartToolItemBase.TAB_SHIPPART;
        }

        #endregion

        #region Public Properties

        public override string Name
        {
            get
            {
                return "Cargo Bay";
            }
        }
        public override string Description
        {
            get
            {
                return "Stores materials";
            }
        }
        public override string Category
        {
            get
            {
                return PartToolItemBase.CATEGORY_CONTAINER;
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
            return new CargoBayDesign(this.Options);
        }

        #endregion
    }

    #endregion
    #region Class: CargoBayDesign

    public class CargoBayDesign : PartDesignBase
    {
        #region Declaration Section

        public const PartDesignAllowedScale ALLOWEDSCALE = PartDesignAllowedScale.X_Y_Z;		// This is here so the scale can be known through reflection

        private Tuple<UtilityNewt.IObjectMassBreakdown, Vector3D, double> _massBreakdown = null;

        #endregion

        #region Constructor

        public CargoBayDesign(EditorOptions options)
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

        private GeometryModel3D _geometry = null;
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
            Transform3DGroup transform = new Transform3DGroup();
            //transform.Children.Add(new ScaleTransform3D(this.Scale));		// it ignores scale
            transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(this.Orientation)));
            transform.Children.Add(new TranslateTransform3D(this.Position.ToVector()));

            return CollisionHull.CreateBox(world, 0, this.Scale, transform.Value);
        }
        public override UtilityNewt.IObjectMassBreakdown GetMassBreakdown(double cellSize)
        {
            if (_massBreakdown != null && _massBreakdown.Item2 == this.Scale && _massBreakdown.Item3 == cellSize)
            {
                // This has already been built for this size
                return _massBreakdown.Item1;
            }

            var breakdown = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Box, UtilityNewt.MassDistribution.Uniform, this.Scale, cellSize);

            // Store this
            _massBreakdown = new Tuple<UtilityNewt.IObjectMassBreakdown, Vector3D, double>(breakdown, this.Scale, cellSize);

            // Exit Function
            return _massBreakdown.Item1;
        }

        #endregion

        #region Private Methods

        private GeometryModel3D CreateGeometry(bool isFinal)
        {
            MaterialGroup material = new MaterialGroup();
            DiffuseMaterial diffuse = new DiffuseMaterial(new SolidColorBrush(WorldColors.CargoBay));
            this.MaterialBrushes.Add(new MaterialColorProps(diffuse, WorldColors.CargoBay));
            material.Children.Add(diffuse);
            SpecularMaterial specular = WorldColors.CargoBaySpecular;
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

            if (isFinal)
            {
                retVal.Geometry = UtilityWPF.GetCube_IndependentFaces(new Point3D(-.5, -.5, -.5), new Point3D(.5, .5, .5));
            }
            else
            {
                retVal.Geometry = GetMesh();
            }

            // Transform
            retVal.Transform = GetTransformForGeometry(isFinal);

            // Exit Function
            return retVal;
        }

        /// <summary>
        /// This returns a cube made of shallow pyramids
        /// </summary>
        /// <remarks>
        /// I was going to keep the tip height static by overriding base.scale, and rebuilding the mesh instead of just scaling
        /// a perfect cube (I would have had to used scaled height/width and translate the sides).  But the scaled cube doesn't
        /// look too bad, and it's a lot easier
        /// </remarks>
        private static MeshGeometry3D GetMesh()
        {
            const double HALF = .5d;
            const double TIP = -.025d;
            //const double HALF = .4;
            //const double TIP = .05d;

            // Define 3D mesh object
            MeshGeometry3D retVal = new MeshGeometry3D();

            int pointOffset = 0;

            // Front
            Transform3DGroup transform = new Transform3DGroup();
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90)));
            transform.Children.Add(new TranslateTransform3D(HALF, 0, 0));
            GetMeshSprtFace(ref pointOffset, retVal, transform, HALF, HALF, TIP);

            // Right
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 90)));
            GetMeshSprtFace(ref pointOffset, retVal, transform, HALF, HALF, TIP);

            // Back
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 90)));
            GetMeshSprtFace(ref pointOffset, retVal, transform, HALF, HALF, TIP);

            // Left
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 90)));
            GetMeshSprtFace(ref pointOffset, retVal, transform, HALF, HALF, TIP);

            // Top
            transform = new Transform3DGroup();
            transform.Children.Add(new TranslateTransform3D(0, 0, HALF));
            GetMeshSprtFace(ref pointOffset, retVal, transform, HALF, HALF, TIP);

            // Bottom
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 180)));
            GetMeshSprtFace(ref pointOffset, retVal, transform, HALF, HALF, TIP);

            // Exit Function
            return retVal;
        }
        private static void GetMeshSprtFace(ref int pointOffset, MeshGeometry3D mesh, Transform3D transform, double halfWidth, double halfHeight, double tip)
        {
            double quarterWidth = halfWidth * .5d;
            double quarterHeight = halfHeight * .5d;

            for (int x = -1; x < 2; x += 2)
            {
                for (int y = -1; y < 2; y += 2)
                {
                    double offsetX = quarterWidth * x;
                    double offsetY = quarterHeight * y;

                    // Bottom
                    mesh.Positions.Add(transform.Transform(new Point3D(offsetX - quarterWidth, offsetY - quarterHeight, 0)));		// left bottom
                    mesh.Positions.Add(transform.Transform(new Point3D(offsetX + quarterWidth, offsetY - quarterHeight, 0)));		// right bottom
                    mesh.Positions.Add(transform.Transform(new Point3D(offsetX, offsetY, tip)));		// tip
                    mesh.TriangleIndices.Add(pointOffset + 0);
                    mesh.TriangleIndices.Add(pointOffset + 1);
                    mesh.TriangleIndices.Add(pointOffset + 2);
                    pointOffset += 3;

                    // Right
                    mesh.Positions.Add(transform.Transform(new Point3D(offsetX + quarterWidth, offsetY - quarterHeight, 0)));		// right bottom
                    mesh.Positions.Add(transform.Transform(new Point3D(offsetX + quarterWidth, offsetY + quarterHeight, 0)));		// right top
                    mesh.Positions.Add(transform.Transform(new Point3D(offsetX, offsetY, tip)));		// tip
                    mesh.TriangleIndices.Add(pointOffset + 0);
                    mesh.TriangleIndices.Add(pointOffset + 1);
                    mesh.TriangleIndices.Add(pointOffset + 2);
                    pointOffset += 3;

                    // Top
                    mesh.Positions.Add(transform.Transform(new Point3D(offsetX + quarterWidth, offsetY + quarterHeight, 0)));		// right top
                    mesh.Positions.Add(transform.Transform(new Point3D(offsetX - quarterWidth, offsetY + quarterHeight, 0)));		// left top
                    mesh.Positions.Add(transform.Transform(new Point3D(offsetX, offsetY, tip)));		// tip
                    mesh.TriangleIndices.Add(pointOffset + 0);
                    mesh.TriangleIndices.Add(pointOffset + 1);
                    mesh.TriangleIndices.Add(pointOffset + 2);
                    pointOffset += 3;

                    // Left
                    mesh.Positions.Add(transform.Transform(new Point3D(offsetX - quarterWidth, offsetY + quarterHeight, 0)));		// left top
                    mesh.Positions.Add(transform.Transform(new Point3D(offsetX - quarterWidth, offsetY - quarterHeight, 0)));		// left bottom
                    mesh.Positions.Add(transform.Transform(new Point3D(offsetX, offsetY, tip)));		// tip
                    mesh.TriangleIndices.Add(pointOffset + 0);
                    mesh.TriangleIndices.Add(pointOffset + 1);
                    mesh.TriangleIndices.Add(pointOffset + 2);
                    pointOffset += 3;
                }
            }
        }
        private static void GetMeshSprtFace_OLD(ref int pointOffset, MeshGeometry3D mesh, Transform3D transform, double halfWidth, double halfHeight, double tip)
        {
            // Bottom
            mesh.Positions.Add(transform.Transform(new Point3D(-halfWidth, -halfHeight, 0)));		// left bottom
            mesh.Positions.Add(transform.Transform(new Point3D(halfWidth, -halfHeight, 0)));		// right bottom
            mesh.Positions.Add(transform.Transform(new Point3D(0, 0, tip)));		// tip
            mesh.TriangleIndices.Add(pointOffset + 0);
            mesh.TriangleIndices.Add(pointOffset + 1);
            mesh.TriangleIndices.Add(pointOffset + 2);
            pointOffset += 3;

            // Right
            mesh.Positions.Add(transform.Transform(new Point3D(halfWidth, -halfHeight, 0)));		// right bottom
            mesh.Positions.Add(transform.Transform(new Point3D(halfWidth, halfHeight, 0)));		// right top
            mesh.Positions.Add(transform.Transform(new Point3D(0, 0, tip)));		// tip
            mesh.TriangleIndices.Add(pointOffset + 0);
            mesh.TriangleIndices.Add(pointOffset + 1);
            mesh.TriangleIndices.Add(pointOffset + 2);
            pointOffset += 3;

            // Top
            mesh.Positions.Add(transform.Transform(new Point3D(halfWidth, halfHeight, 0)));		// right top
            mesh.Positions.Add(transform.Transform(new Point3D(-halfWidth, halfHeight, 0)));		// left top
            mesh.Positions.Add(transform.Transform(new Point3D(0, 0, tip)));		// tip
            mesh.TriangleIndices.Add(pointOffset + 0);
            mesh.TriangleIndices.Add(pointOffset + 1);
            mesh.TriangleIndices.Add(pointOffset + 2);
            pointOffset += 3;

            // Left
            mesh.Positions.Add(transform.Transform(new Point3D(-halfWidth, halfHeight, 0)));		// left top
            mesh.Positions.Add(transform.Transform(new Point3D(-halfWidth, -halfHeight, 0)));		// left bottom
            mesh.Positions.Add(transform.Transform(new Point3D(0, 0, tip)));		// tip
            mesh.TriangleIndices.Add(pointOffset + 0);
            mesh.TriangleIndices.Add(pointOffset + 1);
            mesh.TriangleIndices.Add(pointOffset + 2);
            pointOffset += 3;
        }

        #endregion
    }

    #endregion
    #region Class: CargoBay

    //TODO: Expose two more input neurons, one will only allow cargo in if firing.  The other will eject cargo if firing (a good way to remove mass quickly)

    public class CargoBay : PartBase, INeuronContainer, IPartUpdatable
    {
        #region Declaration Section

        public const string PARTTYPE = "CargoBay";

        private readonly object _lock = new object();

        private ItemOptions _itemOptions = null;

        private List<Cargo> _cargo = new List<Cargo>();

        private Neuron_SensorPosition _neuron = null;

        #endregion

        #region Constructor

        public CargoBay(EditorOptions options, ItemOptions itemOptions, PartDNA dna)
            : base(options, dna)
        {
            _itemOptions = itemOptions;

            this.Design = new CargoBayDesign(options);
            this.Design.SetDNA(dna);

            double volume, radius;
            GetMass(out _dryMass, out volume, out radius, _itemOptions, dna);

            this.MaxVolume = volume;
            this.Radius = radius;

            _neuron = new Neuron_SensorPosition(new Point3D(0, 0, 0), false);
        }

        #endregion

        #region INeuronContainer Members

        public IEnumerable<INeuron> Neruons_Readonly
        {
            get
            {
                return new INeuron[] { _neuron };
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
                return new INeuron[] { _neuron };
            }
        }

        public double Radius
        {
            get;
            private set;
        }

        public NeuronContainerType NeuronContainerType
        {
            get
            {
                return NeuronContainerType.Sensor;
            }
        }

        public bool IsOn
        {
            get
            {
                // This is a basic container that doesn't consume energy, so is always "on"
                return true;
            }
        }

        #endregion
        #region IPartUpdatable Members

        public void Update(double elapsedTime)
        {
            // -1 when empty, 1 when full
            _neuron.Value = UtilityHelper.GetScaledValue_Capped(-1d, 1d, 0d, this.MaxVolume, this.UsedVolume);
        }

        #endregion

        #region Public Properties

        private readonly double _dryMass;
        public override double DryMass
        {
            get
            {
                return _dryMass;
            }
        }
        public override double TotalMass
        {
            get
            {
                lock (_lock)
                {
                    return _dryMass + _cargo.Sum(o => o.Density * o.Volume);
                }
            }
        }

        private readonly Vector3D _scaleActual = new Vector3D(1, 1, 1);
        public override Vector3D ScaleActual
        {
            get
            {
                return _scaleActual;
            }
        }

        private volatile object _usedVolume = 0d;
        public double UsedVolume
        {
            get
            {
                return (double)_usedVolume;
            }
            private set
            {
                _usedVolume = value;
            }
        }
        public double MaxVolume
        {
            get;
            private set;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// This converts the internal list of cargo into an array, and returns that array (this could be a bit expensive, threadsafe)
        /// </summary>
        public Cargo[] GetCargoSnapshot()
        {
            lock (_lock)
            {
                return _cargo.ToArray();
            }
        }

        //TODO: Add an overload that takes part of the cargo (only if mineral), and returns the remainder
        public bool Add(Cargo cargo)
        {
            lock (_lock)
            {
                if (this.UsedVolume + cargo.Volume > this.MaxVolume)
                {
                    return false;
                }

                _cargo.Add(cargo);
                this.UsedVolume = GetUsedVolume();

                return true;
            }
        }

        //TODO: Remove needs more overloads, especially when removing ship parts

        public Cargo Remove(long token)
        {
            lock (_lock)
            {
                int index = 0;
                while (index < _cargo.Count)
                {
                    if (_cargo[index].Token == token)
                    {
                        Cargo retVal = _cargo[index];

                        _cargo.RemoveAt(index);
                        this.UsedVolume = GetUsedVolume();

                        return retVal;
                    }
                    else
                    {
                        index++;
                    }
                }

                // It wasn't found
                return null;
            }
        }

        /// <summary>
        /// This removes enough minerals to fulfill the requested volume.  I may chop a larger mineral in two
        /// </summary>
        /// <param name="type">The type of mineral to remove</param>
        /// <param name="volume">How much to remove</param>
        /// <returns>
        /// Item1=Volume returned
        /// Item2=Cargo returned
        /// </returns>
        public Tuple<double, Cargo[]> RemoveMineral(MineralType type, double volume)
        {
            lock (_lock)
            {
                Cargo[] candidates = _cargo.Where(o => o.CargoType == CargoType.Mineral && ((Cargo_Mineral)o).MineralType == type).OrderBy(o => o.Volume).ToArray();

                return RemoveMineralSprtContinue(candidates, volume);
            }
        }
        /// <summary>
        /// This one removes any mineral (lowest volume first)
        /// </summary>
        /// <remarks>
        /// TODO: Make another overload that prioritizes certain types
        /// </remarks>
        public Tuple<double, Cargo[]> RemoveMineral(double volume)
        {
            lock (_lock)
            {
                Cargo[] candidates = _cargo.Where(o => o.CargoType == CargoType.Mineral).OrderBy(o => o.Volume).ToArray();

                return RemoveMineralSprtContinue(candidates, volume);
            }
        }
        private Tuple<double, Cargo[]> RemoveMineralSprtContinue(Cargo[] candidates, double volume)
        {
            List<Cargo> retVal = new List<Cargo>();
            double current = 0d;

            for (int cntr = 0; cntr < candidates.Length; cntr++)
            {
                if (current + candidates[cntr].Volume < volume)
                {
                    _cargo.Remove(candidates[cntr]);

                    retVal.Add(candidates[cntr]);
                    current += candidates[cntr].Volume;
                }
                else
                {
                    // This one is the next smallest, but is too big
                    Cargo_Mineral remainder = new Cargo_Mineral(((Cargo_Mineral)candidates[cntr]).MineralType, candidates[cntr].Density, volume - current);
                    candidates[cntr].Volume -= remainder.Volume;

                    retVal.Add(remainder);
                    current += remainder.Volume;
                    break;
                }
            }

            this.UsedVolume = GetUsedVolume();

            // Exit Function
            return Tuple.Create(current, retVal.ToArray());
        }

        public Cargo[] ClearContents()
        {
            lock (_lock)
            {
                Cargo[] retVal = _cargo.ToArray();

                _cargo.Clear();
                this.UsedVolume = GetUsedVolume();

                return retVal;
            }
        }

        /// <summary>
        /// If a consumer modifies cargo outside of this class, call this method to ensure this.UsedVolume is up to date
        /// </summary>
        public void UpdateVolume()
        {
            this.UsedVolume = GetUsedVolume();
        }

        #endregion

        #region Private Methods

        private static void GetMass(out double dryMass, out double volume, out double radius, ItemOptions itemOptions, PartDNA dna)
        {
            // The cargo bay is 1:1 with scale (a lot of the other parts are smaller than scale, but cargo bay is full sized)

            double surfaceArea = (2d * dna.Scale.X * dna.Scale.Y) + (2d * dna.Scale.X * dna.Scale.Z) + (2d * dna.Scale.Y * dna.Scale.Z);
            dryMass = surfaceArea * itemOptions.CargoBayWallDensity;

            volume = dna.Scale.X * dna.Scale.Y * dna.Scale.Z;

            radius = (dna.Scale.X + dna.Scale.Y + dna.Scale.Z) / 3d;       // this is just approximate, and is used by INeuronContainer
        }

        private double GetUsedVolume()
        {
            return _cargo.Sum(o => o.Volume);
        }

        #endregion
    }

    #endregion

    #region Class: CargoBayGroup

    public class CargoBayGroup
    {
        #region Declaration Section

        private readonly object _lock = new object();

        private CargoBay[] _cargoBays = null;

        #endregion

        #region Constructor

        public CargoBayGroup(CargoBay[] cargoBays)
        {
            _cargoBays = cargoBays;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// This returns the currently used and max capacity of all cargo bays (good for making a progress bar)
        /// </summary>
        public Tuple<double, double> CargoVolume
        {
            get
            {
                lock (_lock)
                {
                    double used = 0d;
                    double max = 0d;

                    //foreach (CargoBay cargoBay in _cargoBays)
                    for (int cntr = 0; cntr < _cargoBays.Length; cntr++)
                    {
                        used += _cargoBays[cntr].UsedVolume;
                        max += _cargoBays[cntr].MaxVolume;
                    }

                    return Tuple.Create(used, max);
                }
            }
        }

        #endregion

        #region Public Methods

        public bool Add(Cargo cargo)
        {
            lock (_lock)
            {
                // See which cargo bays can hold this
                CargoBay[] _available = _cargoBays.Where(o => o.MaxVolume - o.UsedVolume > cargo.Volume).ToArray();
                if (_available.Length == 0)
                {
                    return false;
                }

                // Add it to a random cargo bay
                _available[StaticRandom.Next(_available.Length)].Add(cargo);

                return true;
            }
        }

        /// <summary>
        /// This removes enough minerals to fulfill the requested volume.  I may chop a larger mineral in two
        /// </summary>
        /// <param name="type">The type of mineral to remove</param>
        /// <param name="volume">How much to remove</param>
        /// <returns>
        /// Item1=Volume returned
        /// Item2=Cargo returned
        /// </returns>
        public Tuple<double, Cargo[]> RemoveMineral_Volume(MineralType type, double volume)
        {
            lock (_lock)
            {
                List<Tuple<int, Cargo>> allCandidates = new List<Tuple<int, Cargo>>();
                for (int cntr = 0; cntr < _cargoBays.Length; cntr++)
                {
                    // Get all the minerals of the requested type from this cargo bay
                    Cargo[] candidates = _cargoBays[cntr].GetCargoSnapshot().
                        Where(o => o.CargoType == CargoType.Mineral && ((Cargo_Mineral)o).MineralType == type).
                        ToArray();

                    if (candidates.Length > 0)
                    {
                        allCandidates.AddRange(candidates.Select(o => Tuple.Create(cntr, o)));
                    }
                }

                // Remove as many as necessary (smallest first)
                return RemoveMineralSprtContinue_Volume(allCandidates.OrderBy(o => o.Item2.Volume).ToArray(), volume);
            }
        }
        /// <summary>
        /// This one removes any mineral (lowest volume first)
        /// </summary>
        /// <remarks>
        /// TODO: Make another overload that prioritizes certain types
        /// </remarks>
        public Tuple<double, Cargo[]> RemoveMineral_Volume(double volume)
        {
            lock (_lock)
            {
                List<Tuple<int, Cargo>> allCandidates = new List<Tuple<int, Cargo>>();
                for (int cntr = 0; cntr < _cargoBays.Length; cntr++)
                {
                    // Get all the minerals from this cargo bay
                    Cargo[] candidates = _cargoBays[cntr].GetCargoSnapshot().
                        Where(o => o.CargoType == CargoType.Mineral).
                        ToArray();

                    if (candidates.Length > 0)
                    {
                        allCandidates.AddRange(candidates.Select(o => Tuple.Create(cntr, o)));
                    }
                }

                // Remove as many as necessary (smallest first)
                return RemoveMineralSprtContinue_Volume(allCandidates.OrderBy(o => o.Item2.Volume).ToArray(), volume);
            }
        }

        /// <summary>
        /// This one goes after mass instead of volume (returns lowest density first)
        /// </summary>
        /// <param name="mass">How much to remove</param>
        /// <returns>
        /// Item1=Mass returned
        /// Item2=Cargo returned
        /// </returns>
        public Tuple<double, Cargo[]> RemoveMineral_Mass(double mass)
        {
            lock (_lock)
            {
                List<Tuple<int, Cargo>> allCandidates = new List<Tuple<int, Cargo>>();
                for (int cntr = 0; cntr < _cargoBays.Length; cntr++)
                {
                    // Get all the minerals from this cargo bay (lowest density first)
                    Cargo[] candidates = _cargoBays[cntr].GetCargoSnapshot().
                        Where(o => o.CargoType == CargoType.Mineral).
                        ToArray();

                    if (candidates.Length > 0)
                    {
                        allCandidates.AddRange(candidates.Select(o => Tuple.Create(cntr, o)));
                    }
                }

                // Remove as many as necessary (least dense, then lowest volume first)
                return RemoveMineralSprtContinue_Mass(allCandidates.OrderBy(o => Tuple.Create(o.Item2.Density, o.Item2.Volume)).ToArray(), mass);
            }
        }

        public Cargo[] ClearContents()
        {
            lock (_lock)
            {
                List<Cargo> retVal = new List<Cargo>();

                for (int cntr = 0; cntr < _cargoBays.Length; cntr++)
                {
                    retVal.AddRange(_cargoBays[cntr].ClearContents());
                }

                return retVal.ToArray();
            }
        }

        #endregion

        #region Private Methods

        private Tuple<double, Cargo[]> RemoveMineralSprtContinue_Volume(Tuple<int, Cargo>[] candidates, double volume)
        {
            //NOTE: The candidate cargo is sorted smallest to largest

            List<Cargo> retVal = new List<Cargo>();
            double current = 0d;

            foreach (var cargo in candidates)
            {
                if (current + cargo.Item2.Volume < volume)
                {
                    Cargo removed = _cargoBays[cargo.Item1].Remove(cargo.Item2.Token);
                    if (removed != null)        // the only way to get null is if the cargo was removed from some other class (because this class is in a lock)
                    {
                        retVal.Add(removed);
                        current += removed.Volume;
                    }
                }
                else
                {
                    // This one is the next smallest, but is too big
                    //NOTE: If this is a ship part, it will be turned to scrap.  If ship parts should stay whole, make an overload, or pass a boolean
                    Cargo remainder = cargo.Item2.Clone();
                    remainder.Volume = volume - current;
                    cargo.Item2.Volume -= remainder.Volume;

                    _cargoBays[cargo.Item1].UpdateVolume();     // the cargo bay maintains UsedVolume as an independent variable, so it needs to be told when the cargo volume changes outside of its control

                    retVal.Add(remainder);
                    current += remainder.Volume;
                    break;
                }
            }

            // Exit Function
            return Tuple.Create(current, retVal.ToArray());
        }
        private Tuple<double, Cargo[]> RemoveMineralSprtContinue_Mass(Tuple<int, Cargo>[] candidates, double mass)
        {
            //NOTE: The candidate cargo is sorted by density, volume

            List<Cargo> retVal = new List<Cargo>();
            double current = 0d;

            foreach (var cargo in candidates)
            {
                if (current + (cargo.Item2.Density * cargo.Item2.Volume) < mass)
                {
                    Cargo removed = _cargoBays[cargo.Item1].Remove(cargo.Item2.Token);
                    if (removed != null)        // the only way to get null is if the cargo was removed from some other class (because this class is in a lock)
                    {
                        retVal.Add(removed);
                        current += removed.Density * removed.Volume;
                    }
                }
                else
                {
                    // This one is the next smallest, but is too big
                    //NOTE: If this is a ship part, it will be turned to scrap.  If ship parts should stay whole, make an overload, or pass a boolean
                    Cargo remainder = cargo.Item2.Clone();
                    remainder.Volume = (mass - current) / remainder.Density;
                    cargo.Item2.Volume -= remainder.Volume;

                    _cargoBays[cargo.Item1].UpdateVolume();     // the cargo bay maintains UsedVolume as an independent variable, so it needs to be told when the cargo volume changes outside of its control

                    retVal.Add(remainder);
                    current += remainder.Density * remainder.Volume;
                    break;
                }
            }

            // Exit Function
            return Tuple.Create(current, retVal.ToArray());
        }

        #endregion
    }

    #endregion

    #region Enum: CargoType

    public enum CargoType
    {
        Mineral,
        ShipPart
    }

    #endregion
    #region Class: Cargo_ShipPart

    //TODO: If part of the volume of this cargo gets removed, a bool should be set so that this part is known to be damaged (so it can't be turned into a real part later)

    public class Cargo_ShipPart : Cargo
    {
        public Cargo_ShipPart(PartDNA dna, double density, double volume)
            : base(CargoType.ShipPart, density, volume)
        {
            this.DNA = dna;
        }

        public readonly PartDNA DNA;

        public override Cargo Clone()
        {
            return new Cargo_ShipPart(PartDNA.Clone(this.DNA), this.Density, this.Volume);
        }
    }

    #endregion
    #region Class: Cargo_Mineral

    public class Cargo_Mineral : Cargo
    {
        public Cargo_Mineral(MineralType mineralType, double density, double volume)
            : base(CargoType.Mineral, density, volume)
        {
            this.MineralType = mineralType;
        }

        public readonly MineralType MineralType;

        public override Cargo Clone()
        {
            return new Cargo_Mineral(this.MineralType, this.Density, this.Volume);
        }
    }

    #endregion
    #region Class: Cargo

    public class Cargo
    {
        public Cargo(CargoType cargoType, double density, double volume)
        {
            this.Token = TokenGenerator.Instance.NextToken();
            this.CargoType = cargoType;
            this.Density = density;
            this.Volume = volume;
        }

        /// <summary>
        /// This is redundant, since the derived type will be this as well, but is easy to write switch statements
        /// on (but there could be some types that just need volume, so no need for a derived cargo class)
        /// </summary>
        public readonly CargoType CargoType;

        public readonly long Token;

        public readonly double Density;

        private volatile object _volume = 0d;
        /// <summary>
        /// Some types could have volume change (like minerals being chopped up, and handed to matter converters)
        /// </summary>
        public double Volume
        {
            get
            {
                return (double)_volume;
            }
            set
            {
                _volume = value;
            }
        }

        public virtual Cargo Clone()
        {
            return new Cargo(this.CargoType, this.Density, this.Volume);
        }
    }

    #endregion
}
