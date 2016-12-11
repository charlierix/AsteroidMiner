using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using Game.Newt.v2.GameItems.ShipEditor;
using Game.HelperClassesWPF;
using Game.Newt.v2.NewtonDynamics;
using Game.HelperClassesCore;

namespace Game.Newt.v2.GameItems.ShipParts
{
    #region Class: ConverterMatterToFuelToolItem

    public class ConverterMatterToFuelToolItem : PartToolItemBase
    {
        #region Constructor

        public ConverterMatterToFuelToolItem(EditorOptions options)
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
                return "Matter to Fuel Converter";
            }
        }
        public override string Description
        {
            get
            {
                return "Pulls matter out of the cargo bay, consumes some energy, and puts fuel in the fuel tank";
            }
        }
        public override string Category
        {
            get
            {
                return PartToolItemBase.CATEGORY_CONVERTERS;
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
            return new ConverterMatterToFuelDesign(this.Options, false);
        }

        #endregion
    }

    #endregion
    #region Class: ConverterMatterToFuelDesign

    public class ConverterMatterToFuelDesign : PartDesignBase
    {
        #region Declaration Section

        public const PartDesignAllowedScale ALLOWEDSCALE = PartDesignAllowedScale.XYZ;		// This is here so the scale can be known through reflection
        public const double SCALE = .5d;

        private Tuple<UtilityNewt.IObjectMassBreakdown, Vector3D, double> _massBreakdown = null;

        #endregion

        #region Constructor

        public ConverterMatterToFuelDesign(EditorOptions options, bool isFinalModel)
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

        #endregion

        #region Public Methods

        public override CollisionHull CreateCollisionHull(WorldBase world)
        {
            Transform3DGroup transform = new Transform3DGroup();
            //transform.Children.Add(new ScaleTransform3D(this.Scale));		// it ignores scale
            transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(this.Orientation)));
            transform.Children.Add(new TranslateTransform3D(this.Position.ToVector()));

            return CollisionHull.CreateBox(world, 0, this.Scale * SCALE, transform.Value);
        }
        public override UtilityNewt.IObjectMassBreakdown GetMassBreakdown(double cellSize)
        {
            if (_massBreakdown != null && _massBreakdown.Item2 == this.Scale && _massBreakdown.Item3 == cellSize)
            {
                // This has already been built for this size
                return _massBreakdown.Item1;
            }

            // Convert this.Scale into a size that the mass breakdown will use
            Vector3D size = this.Scale * SCALE;

            var breakdown = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Box, UtilityNewt.MassDistribution.Uniform, size, cellSize);

            // Store this
            _massBreakdown = new Tuple<UtilityNewt.IObjectMassBreakdown, Vector3D, double>(breakdown, this.Scale, cellSize);

            // Exit Function
            return _massBreakdown.Item1;
        }

        public override PartToolItemBase GetToolItem()
        {
            return new ConverterMatterToFuelToolItem(this.Options);
        }

        #endregion

        #region Private Methods

        private Model3DGroup CreateGeometry(bool isFinal)
        {
            return CreateGeometry(this.MaterialBrushes, base.SelectionEmissives,
                GetTransformForGeometry(isFinal),
                WorldColors.ConverterBase, WorldColors.ConverterBaseSpecular, WorldColors.ConverterFuel, WorldColors.ConverterFuelSpecular,
                isFinal);
        }

        internal static Model3DGroup CreateGeometry(List<MaterialColorProps> materialBrushes, List<EmissiveMaterial> selectionEmissives, Transform3D transform, Color baseColor, SpecularMaterial baseSpecular, Color colorColor, SpecularMaterial colorSpecular, bool isFinal)
        {
            Model3DGroup retVal = new Model3DGroup();

            GeometryModel3D geometry;
            MaterialGroup material;
            DiffuseMaterial diffuse;
            SpecularMaterial specular;

            #region Main Cube

            geometry = new GeometryModel3D();
            material = new MaterialGroup();
            diffuse = new DiffuseMaterial(new SolidColorBrush(baseColor));
            materialBrushes.Add(new MaterialColorProps(diffuse, baseColor));
            material.Children.Add(diffuse);
            specular = baseSpecular;
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

            geometry.Geometry = GetMeshBase(SCALE);

            retVal.Children.Add(geometry);

            #endregion

            #region Color Cube

            geometry = new GeometryModel3D();
            material = new MaterialGroup();
            diffuse = new DiffuseMaterial(new SolidColorBrush(colorColor));
            materialBrushes.Add(new MaterialColorProps(diffuse, colorColor));
            material.Children.Add(diffuse);
            specular = colorSpecular;
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

            geometry.Geometry = GetMeshColor(SCALE);

            retVal.Children.Add(geometry);

            #endregion

            // Transform
            retVal.Transform = transform;

            // Exit Function
            return retVal;
        }

        internal static MeshGeometry3D GetMeshBase(double scale)
        {
            //NOTE: These tips are negative
            return GetMesh(.5d * scale, -.1d * scale, 1);
        }
        internal static MeshGeometry3D GetMeshColor(double scale)
        {
            return GetMesh(.35d * scale, .15d * scale, 1);
        }
        internal static MeshGeometry3D GetMesh(double half, double tip, int faceCount)
        {
            // Define 3D mesh object
            MeshGeometry3D retVal = new MeshGeometry3D();

            int pointOffset = 0;

            // Front
            Transform3DGroup transform = new Transform3DGroup();
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90)));
            transform.Children.Add(new TranslateTransform3D(half, 0, 0));
            ConverterMatterToFuelDesign.GetMeshFace(ref pointOffset, retVal, transform, half, half, tip, faceCount);

            // Right
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 90)));
            ConverterMatterToFuelDesign.GetMeshFace(ref pointOffset, retVal, transform, half, half, tip, faceCount);

            // Back
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 90)));
            ConverterMatterToFuelDesign.GetMeshFace(ref pointOffset, retVal, transform, half, half, tip, faceCount);

            // Left
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 90)));
            ConverterMatterToFuelDesign.GetMeshFace(ref pointOffset, retVal, transform, half, half, tip, faceCount);

            // Top
            transform = new Transform3DGroup();
            transform.Children.Add(new TranslateTransform3D(0, 0, half));
            ConverterMatterToFuelDesign.GetMeshFace(ref pointOffset, retVal, transform, half, half, tip, faceCount);

            // Bottom
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 180)));
            ConverterMatterToFuelDesign.GetMeshFace(ref pointOffset, retVal, transform, half, half, tip, faceCount);

            // Exit Function
            return retVal;
        }
        internal static void GetMeshFace(ref int pointOffset, MeshGeometry3D mesh, Transform3D transform, double halfWidth, double halfHeight, double tip, int numPyramids)
        {
            double faceWidth = halfWidth / numPyramids;
            double faceHeight = halfHeight / numPyramids;

            int to = numPyramids - 1;
            int from = to * -1;

            for (int x = from; x <= to; x += 2)
            {
                for (int y = from; y <= to; y += 2)
                {
                    double offsetX = faceWidth * x;
                    double offsetY = faceHeight * y;

                    // Bottom
                    mesh.Positions.Add(transform.Transform(new Point3D(offsetX - faceWidth, offsetY - faceHeight, 0)));		// left bottom
                    mesh.Positions.Add(transform.Transform(new Point3D(offsetX + faceWidth, offsetY - faceHeight, 0)));		// right bottom
                    mesh.Positions.Add(transform.Transform(new Point3D(offsetX, offsetY, tip)));		// tip
                    mesh.TriangleIndices.Add(pointOffset + 0);
                    mesh.TriangleIndices.Add(pointOffset + 1);
                    mesh.TriangleIndices.Add(pointOffset + 2);
                    pointOffset += 3;

                    // Right
                    mesh.Positions.Add(transform.Transform(new Point3D(offsetX + faceWidth, offsetY - faceHeight, 0)));		// right bottom
                    mesh.Positions.Add(transform.Transform(new Point3D(offsetX + faceWidth, offsetY + faceHeight, 0)));		// right top
                    mesh.Positions.Add(transform.Transform(new Point3D(offsetX, offsetY, tip)));		// tip
                    mesh.TriangleIndices.Add(pointOffset + 0);
                    mesh.TriangleIndices.Add(pointOffset + 1);
                    mesh.TriangleIndices.Add(pointOffset + 2);
                    pointOffset += 3;

                    // Top
                    mesh.Positions.Add(transform.Transform(new Point3D(offsetX + faceWidth, offsetY + faceHeight, 0)));		// right top
                    mesh.Positions.Add(transform.Transform(new Point3D(offsetX - faceWidth, offsetY + faceHeight, 0)));		// left top
                    mesh.Positions.Add(transform.Transform(new Point3D(offsetX, offsetY, tip)));		// tip
                    mesh.TriangleIndices.Add(pointOffset + 0);
                    mesh.TriangleIndices.Add(pointOffset + 1);
                    mesh.TriangleIndices.Add(pointOffset + 2);
                    pointOffset += 3;

                    // Left
                    mesh.Positions.Add(transform.Transform(new Point3D(offsetX - faceWidth, offsetY + faceHeight, 0)));		// left top
                    mesh.Positions.Add(transform.Transform(new Point3D(offsetX - faceWidth, offsetY - faceHeight, 0)));		// left bottom
                    mesh.Positions.Add(transform.Transform(new Point3D(offsetX, offsetY, tip)));		// tip
                    mesh.TriangleIndices.Add(pointOffset + 0);
                    mesh.TriangleIndices.Add(pointOffset + 1);
                    mesh.TriangleIndices.Add(pointOffset + 2);
                    pointOffset += 3;
                }
            }
        }

        #endregion
    }

    #endregion
    #region Class: ConverterMatterToFuel

    public class ConverterMatterToFuel : PartBase, IPartUpdatable, IContainer, IConverterMatter
    {
        #region Declaration Section

        public const string PARTTYPE = "ConverterMatterToFuel";

        private readonly object _lock = new object();

        private readonly ItemOptions _itemOptions = null;

        private readonly IContainer _fuelTanks;

        // This stays sorted from low to high density
        private List<Cargo> _cargo = new List<Cargo>();

        private readonly Converter _converter = null;

        #endregion

        #region Constructor

        public ConverterMatterToFuel(EditorOptions options, ItemOptions itemOptions, ShipPartDNA dna, IContainer fuelTanks)
            : base(options, dna, itemOptions.MatterConverter_Damage.HitpointMin, itemOptions.MatterConverter_Damage.HitpointSlope, itemOptions.MatterConverter_Damage.Damage)
        {
            _itemOptions = itemOptions;
            _fuelTanks = fuelTanks;

            this.Design = new ConverterMatterToFuelDesign(options, true);
            this.Design.SetDNA(dna);

            double volume;
            GetMass(out _dryMass, out volume, out _scaleActual, _itemOptions, dna);

            this.MaxVolume = volume;

            if (_fuelTanks != null)
            {
                double scaleVolume = _scaleActual.X * _scaleActual.Y * _scaleActual.Z;      // can't use volume from above, because that is the amount of matter that can be held.  This is to get conversion ratios
                _converter = new Converter(this, _fuelTanks, _itemOptions.MatterToFuel_ConversionRate, _itemOptions.MatterToFuel_AmountToDraw * scaleVolume);
            }

            this.Destroyed += ConverterMatterToFuel_Destroyed;
        }

        #endregion

        #region IPartUpdatable Members

        public void Update_MainThread(double elapsedTime)
        {
        }
        public void Update_AnyThread(double elapsedTime)
        {
            //if (_converter != null && _cargo.Count > 0)
            if (!this.IsDestroyed && _converter != null)     // I don't want to need a lock here, so ignoring cargo count
            {
                _converter.Transfer(elapsedTime);
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
        #region IContainer Members

        // IContainer was only implemented so that _converter could pull from this class's cargo
        //NOTE: The converter only cares about mass, so the quantities that these IContainer methods deal with are mass.  The other methods in this class are more worried about volume

        public double QuantityCurrent
        {
            get
            {
                lock (_lock)
                {
                    return _cargo.Sum(o => o.Density * o.Volume);
                }
            }
            set
            {
                throw new NotImplementedException();
            }
        }
        public double QuantityMax
        {
            get
            {
                // This class isn't constrained by mass, only volume, so this property has no meaning
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }
        public double QuantityMax_Usable
        {
            get { throw new NotImplementedException(); }
        }

        public double QuantityMaxMinusCurrent
        {
            get { throw new NotImplementedException(); }
        }
        public double QuantityMaxMinusCurrent_Usable
        {
            get { throw new NotImplementedException(); }
        }

        public bool OnlyRemoveMultiples
        {
            get
            {
                return false;
            }
            set
            {
                throw new NotImplementedException();
            }
        }
        public double RemovalMultiple
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public double AddQuantity(double amount, bool exactAmountOnly)
        {
            throw new NotImplementedException();
        }
        public double AddQuantity(IContainer pullFrom, double amount, bool exactAmountOnly)
        {
            throw new NotImplementedException();
        }
        public double AddQuantity(IContainer pullFrom, bool exactAmountOnly)
        {
            throw new NotImplementedException();
        }

        public double RemoveQuantity(double amount, bool exactAmountOnly)
        {
            if (exactAmountOnly)
            {
                throw new ArgumentException("exactAmountOnly cannot be true");
            }

            lock (_lock)
            {
                return RemoveQuantity(amount, _cargo);
            }
        }

        #endregion
        #region IConverterMatter Members

        //NOTE: There is only an add method.  Any cargo added to this converter is burned off over time
        public bool Add(Cargo cargo)
        {
            lock (_lock)
            {
                //double sumVolume = this.UsedVolume + cargo.Volume;
                double sumVolume = _cargo.Sum(o => o.Volume) + cargo.Volume;        // inlining this.UsedVolume because of the lock

                if (sumVolume <= this.MaxVolume || Math1D.IsNearValue(sumVolume, this.MaxVolume))
                {
                    Add(_cargo, cargo);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public double UsedVolume
        {
            get
            {
                lock (_lock)
                {
                    return _cargo.Sum(o => o.Volume);
                }
            }
        }
        public double MaxVolume
        {
            get;
            private set;
        }

        #endregion

        #region Internal Methods

        internal static void GetMass(out double dryMass, out double volume, out Vector3D scale, ItemOptions itemOptions, ShipPartDNA dna)
        {
            scale = dna.Scale * ConverterMatterToFuelDesign.SCALE;

            double surfaceArea = (2d * scale.X * scale.Y) + (2d * scale.X * scale.Z) + (2d * scale.Y * scale.Z);
            dryMass = surfaceArea * itemOptions.MatterConverter_WallDensity;

            volume = scale.X * scale.Y * scale.Z;
            volume *= itemOptions.MatterConverter_InternalVolume;        // this property should be between 0 and 1
        }

        /// <summary>
        /// This keeps the cargo hold sorted by density
        /// NOTE: If volumes change outside of this thread, then this could place cargo in an imperfect order.  No real damage, just be aware
        /// </summary>
        internal static void Add(List<Cargo> cargoHold, Cargo cargo)
        {
            if (cargoHold.Count == 0)
            {
                cargoHold.Add(cargo);
                return;
            }

            for (int cntr = 0; cntr < cargoHold.Count; cntr++)
            {
                if (cargo.Density < cargoHold[cntr].Density)
                {
                    // This is less dense, put it here
                    cargoHold.Insert(cntr, cargo);
                    return;
                }
                else if (Math1D.IsNearValue(cargo.Density, cargoHold[cntr].Density) && cargo.Volume < cargoHold[cntr].Volume)
                {
                    // This is the same density, but has less volume, so put it here
                    cargoHold.Insert(cntr, cargo);
                    return;
                }
            }

            // This is more desnse than everything else, put it at the end
            cargoHold.Add(cargo);
        }

        internal static double RemoveQuantity(double amount, List<Cargo> cargo)
        {
            //NOTE: cargo should be sorted from low density to high density, so the lowest quality material is removed first

            double current = 0d;

            while (cargo.Count > 0)
            {
                double mass = cargo[0].Density * cargo[0].Volume;

                if (current + mass < amount || Math1D.IsNearValue(current + mass, amount))
                {
                    // Eat this whole piece of cargo
                    current += mass;
                    cargo.RemoveAt(0);
                }
                else
                {
                    // Remove some of this cargo
                    double remainingMass = amount - current;
                    double remainingVolume = remainingMass / cargo[0].Density;
                    cargo[0].Volume -= remainingVolume;
                    current = amount;
                    break;
                }
            }

            // Return how much of the request COULDN'T be filled
            return amount - current;
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

        private readonly Vector3D _scaleActual;
        public override Vector3D ScaleActual
        {
            get
            {
                return _scaleActual;
            }
        }

        #endregion

        #region Event Listeners

        private void ConverterMatterToFuel_Destroyed(object sender, EventArgs e)
        {
            lock (_lock)
            {
                _cargo.Clear();
            }
        }

        #endregion
    }

    #endregion

    #region Class: ConverterMatterGroup

    /// <summary>
    /// This handles all of the matter converters as a group
    /// </summary>
    /// <remarks>
    /// They need to be handled as a group, because if individual converters could take matter out of the cargo bay, they
    /// wouldn't know about each other, and would take too much, leaving the others dry
    /// 
    /// This logic could just be part of ship, but it has a lot of code already (and theoretically makes it easier to set up independent
    /// groups)
    /// </remarks>
    public class ConverterMatterGroup
    {
        private readonly object _lock = new object();

        private readonly IConverterMatter[] _converters;
        private readonly CargoBayGroup _cargoBays;

        public ConverterMatterGroup(IConverterMatter[] converters, CargoBayGroup cargoBays)
        {
            _converters = converters;
            _cargoBays = cargoBays;
        }

        public bool Transfer()
        {
            //NOTE: This doesn't call the converter's update, that is done by the ship (it randomizes the order that they are updated)
            lock (_lock)
            {
                if (_converters.Length == 0)
                {
                    return false;
                }

                // See how much volume all of the converters contain
                Tuple<double, double>[] used = _converters.Select(o => Tuple.Create(o.UsedVolume, o.MaxVolume)).ToArray();

                // If they are all greater than 50% capacity, then just leave now
                if (!used.Any(o => o.Item1 / o.Item2 < .5d))
                {
                    return false;
                }

                // See how much quantity is needed
                double needFull = used.Sum(o => o.Item2 - o.Item1);
                double needScaled = needFull * .99d;       // do this to avoid rounding errors causing overflow when distributing to multiple converters

                // Try to pull this much volume out of the cargo bays
                var cargo = _cargoBays.RemoveMineral_Volume(needScaled);

                if (Math1D.IsNearZero(cargo.Item1))
                {
                    // The cargo bays are empty
                    return false;
                }

                if (_converters.Length == 1)
                {
                    // No need to distribute to multiple converters, just give everything to the one
                    Transfer_One(cargo.Item2);
                }
                else
                {
                    // Distriube evenly across the converters
                    //NOTE: Passing in needFull, because that's what used is based off of.  Transfer_Many calculates percents, and if the scaled value is passed
                    //in, the percents will be off.
                    Transfer_Many(cargo, used, needFull);
                }

                return true;
            }
        }

        #region Private Methods

        private void Transfer_One(Cargo[] cargo)
        {
            for (int cntr = 0; cntr < cargo.Length; cntr++)
            {
                if (!_converters[0].Add(cargo[cntr]))
                {
                    // Give it back to the cargo bays (this should never happen)
                    _cargoBays.Add(cargo[cntr]);
                }
            }
        }

        private void Transfer_Many(Tuple<double, Cargo[]> cargo, Tuple<double, double>[] convertersUsed, double converterSumNeed)
        {
            // The sum of the cargo passed in will exactly fill all the converters, but each converter needs some percent of that cargo.
            // Figure out what percent each converter needs
            double[] percentsOfCargo = convertersUsed.Select(o => (o.Item2 - o.Item1) / converterSumNeed).ToArray();

            // Group the cargo by density.  This will avoid giving one converter a bunch of low density cargo and another a bunch of
            // high density
            foreach (var group in cargo.Item2.GroupBy(o => o.Density))
            {
                // Distribute this set of cargo evenly among the converters (according to each converters percent of need)
                Transfer_Many_DistributeGroup(group.ToList(), percentsOfCargo);
            }
        }
        private void Transfer_Many_DistributeGroup(List<Cargo> cargo, double[] percents)
        {
            double sumVolume = cargo.Sum(o => o.Volume);

            // This is used to keep track of how much each converter has left to go
            double[] remaining = percents.Select(o => sumVolume * o).ToArray();

            int infiniteLoopCntr = 0;

            while (cargo.Count > 0)
            {
                bool foundOne = false;

                #region Whole matches

                // Shoot through the cargo, looking for ones that will fit exactly
                int index = 0;
                while (index < cargo.Count)
                {
                    bool wasRemoved = false;
                    for (int cntr = 0; cntr < _converters.Length; cntr++)
                    {
                        if (remaining[cntr] >= cargo[index].Volume)
                        {
                            if (_converters[cntr].Add(cargo[index]))        // This should never come back false unless there is a rounding error (or something else added to the converter outside of this class)
                            {
                                remaining[cntr] -= cargo[index].Volume;
                                cargo.RemoveAt(index);
                                wasRemoved = true;
                                foundOne = true;
                                break;
                            }
                        }
                    }

                    if (!wasRemoved)
                    {
                        index++;
                    }
                }

                #endregion

                if (!foundOne)
                {
                    #region Divide one cargo

                    // Nothing was moved during the previous pass, so find a converter that still needs to be filled, and fill it exactly

                    if (cargo.Count == 0)
                    {
                        throw new ApplicationException("Execution shouldn't have gotten into this if statement if there is no more cargo to distribute");
                    }

                    for (int cntr = 0; cntr < remaining.Length; cntr++)
                    {
                        if (remaining[cntr] > 0d && !Math1D.IsNearZero(remaining[cntr]))
                        {
                            if (cargo[0].Volume < remaining[cntr])
                            {
                                throw new ApplicationException("The previous pass should have caught this");
                            }

                            Cargo splitCargo = cargo[0].Clone();

                            splitCargo.Volume = remaining[cntr];
                            cargo[0].Volume -= remaining[cntr];
                            if (cargo[0].Volume.IsNearZero())
                            {
                                cargo.RemoveAt(0);
                            }

                            if (!_converters[cntr].Add(splitCargo))
                            {
                                throw new ApplicationException(string.Format("Couldn't add cargo to the converter: {0}, {1}", remaining[cntr].ToString(), (_converters[cntr].MaxVolume - _converters[cntr].UsedVolume).ToString()));
                            }

                            remaining[cntr] = 0d;
                            break;
                        }
                    }

                    #endregion
                }

                infiniteLoopCntr++;
                if (infiniteLoopCntr > 100)
                {
                    // This happens when the cargo volumes are almost zero (but not quite below the NEARZERO threshold.  Don't bother trying to
                    // distribute such a tiny amount, just exit
                    //if(cargo.All(o => o.Volume.IsNearZero()))
                    if (cargo.All(o => Math.Abs(o.Volume) < UtilityCore.NEARZERO * 100))
                    {
                        return;
                    }

                    throw new ApplicationException("Infinite loop detected");
                }
            }
        }

        #endregion
    }

    #endregion

    #region Interface: IConverterMatter

    public interface IConverterMatter
    {
        bool Add(Cargo cargo);

        double UsedVolume { get; }
        double MaxVolume { get; }
    }

    #endregion
}
