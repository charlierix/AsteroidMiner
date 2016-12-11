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

namespace Game.Newt.v2.GameItems.ShipParts
{
    #region Class: ConverterFuelToEnergyToolItem

    public class ConverterFuelToEnergyToolItem : PartToolItemBase
    {
        #region Constructor

        public ConverterFuelToEnergyToolItem(EditorOptions options)
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
                return "Fuel to Energy Converter";
            }
        }
        public override string Description
        {
            get
            {
                return "Burns fuel and stores energy";
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
            return new ConverterFuelToEnergyDesign(this.Options, false);
        }

        #endregion
    }

    #endregion
    #region Class: ConverterFuelToEnergyDesign

    public class ConverterFuelToEnergyDesign : PartDesignBase
    {
        #region Declaration Section

        public const double RADIUSPERCENTOFSCALE = .3d;
        public const double HEIGHTPERCENTOFSCALE = .4d;		// NOTE: This height is for radius (double it to get the actual height of the part)

        public const PartDesignAllowedScale ALLOWEDSCALE = PartDesignAllowedScale.XYZ;		// This is here so the scale can be known through reflection

        private Tuple<UtilityNewt.IObjectMassBreakdown, Vector3D, double> _massBreakdown = null;

        #endregion

        #region Constructor

        public ConverterFuelToEnergyDesign(EditorOptions options, bool isFinalModel)
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

            Vector3D scale = this.Scale;

            return CollisionHull.CreateSphere(world, 0, new Vector3D(scale.X * RADIUSPERCENTOFSCALE, scale.Y * RADIUSPERCENTOFSCALE, scale.Z * HEIGHTPERCENTOFSCALE), transform.Value);
        }

        public override UtilityNewt.IObjectMassBreakdown GetMassBreakdown(double cellSize)
        {
            if (_massBreakdown != null && _massBreakdown.Item2 == this.Scale && _massBreakdown.Item3 == cellSize)
            {
                // This has already been built for this size
                return _massBreakdown.Item1;
            }

            // Convert this.Scale into a size that the mass breakdown will use (mass breakdown wants height along X, and scale is for radius, but the mass breakdown wants diameter
            Vector3D size = new Vector3D(this.Scale.Z * HEIGHTPERCENTOFSCALE * 2d, this.Scale.X * RADIUSPERCENTOFSCALE * 2d, this.Scale.Y * RADIUSPERCENTOFSCALE * 2d);		// HEIGHTPERCENTOFSCALE is a radius, so it needs to be doubled too

            // Center
            Vector3D centerSize = new Vector3D(size.X * .5d, size.Y, size.Z);
            double centerVolume = Math.Pow((centerSize.Y + centerSize.Z) / 4d, 2d) * Math.PI * centerSize.X;		// dividing by 4, because div 2 is the average, then another 2 is to convert diameter to radius
            var centerCylinder = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Cylinder, UtilityNewt.MassDistribution.Uniform, centerSize, cellSize);

            // Cap
            Vector3D capSize = new Vector3D(size.X * .25d, size.Y * .6d, size.Z * .6d);
            double capVolume = Math.Pow((capSize.Y + capSize.Z) / 4d, 2d) * Math.PI * capSize.X;
            var capCylinder = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Cylinder, UtilityNewt.MassDistribution.Uniform, capSize, cellSize);

            // Combined shape
            double offsetX = (centerSize.X * .5d) + (capSize.X * .5d);
            Quaternion rotate = new Quaternion(new Vector3D(0, 1, 0), 90);

            var objects = new Tuple<UtilityNewt.ObjectMassBreakdown, Point3D, Quaternion, double>[3];
            objects[0] = new Tuple<UtilityNewt.ObjectMassBreakdown, Point3D, Quaternion, double>(centerCylinder, new Point3D(0, 0, 0), rotate, centerVolume);
            objects[1] = new Tuple<UtilityNewt.ObjectMassBreakdown, Point3D, Quaternion, double>(capCylinder, new Point3D(0, 0, offsetX), rotate, capVolume);		// the breakdowns were build along X, but now putting them back along Z
            objects[2] = new Tuple<UtilityNewt.ObjectMassBreakdown, Point3D, Quaternion, double>(capCylinder, new Point3D(0, 0, -offsetX), rotate, capVolume);

            var combined = UtilityNewt.Combine(objects);

            // Store this
            _massBreakdown = new Tuple<UtilityNewt.IObjectMassBreakdown, Vector3D, double>(combined, this.Scale, cellSize);

            // Exit Function
            return _massBreakdown.Item1;
        }

        public override PartToolItemBase GetToolItem()
        {
            return new ConverterFuelToEnergyToolItem(this.Options);
        }

        #endregion

        #region Private Methods

        private Model3DGroup CreateGeometry(bool isFinal)
        {
            const double SCALE = .75d;

            Model3DGroup retVal = new Model3DGroup();

            GeometryModel3D geometry;
            MaterialGroup material;
            DiffuseMaterial diffuse;
            SpecularMaterial specular;

            int domeSegments = isFinal ? 2 : 10;
            int cylinderSegments = isFinal ? 6 : 35;

            #region Main Cylinder

            geometry = new GeometryModel3D();
            material = new MaterialGroup();
            diffuse = new DiffuseMaterial(new SolidColorBrush(WorldColors.ConverterBase));
            this.MaterialBrushes.Add(new MaterialColorProps(diffuse, WorldColors.ConverterBase));
            material.Children.Add(diffuse);
            specular = WorldColors.ConverterBaseSpecular;
            this.MaterialBrushes.Add(new MaterialColorProps(specular));
            material.Children.Add(specular);

            if (!isFinal)
            {
                EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
                material.Children.Add(selectionEmissive);
                this.SelectionEmissives.Add(selectionEmissive);
            }

            geometry.Material = material;
            geometry.BackMaterial = material;

            List<TubeRingBase> rings = new List<TubeRingBase>();
            rings.Add(new TubeRingRegularPolygon(0, false, .4, .4, true));
            rings.Add(new TubeRingRegularPolygon(.125, false, .48, .48, false));
            rings.Add(new TubeRingRegularPolygon(.25, false, .6, .6, false));

            rings.Add(new TubeRingRegularPolygon(.125, false, .65, .65, false));
            rings.Add(new TubeRingRegularPolygon(.25, false, .7, .7, false));
            rings.Add(new TubeRingRegularPolygon(.25, false, .65, .65, false));

            rings.Add(new TubeRingRegularPolygon(.125, false, .6, .6, false));
            rings.Add(new TubeRingRegularPolygon(.25, false, .48, .48, false));
            rings.Add(new TubeRingRegularPolygon(.125, false, .4, .4, true));

            // Scale this so the height is 1
            double localScale = SCALE * .85d / rings.Sum(o => o.DistFromPrevRing);

            Transform3DGroup transformInitial = new Transform3DGroup();
            transformInitial.Children.Add(new ScaleTransform3D(localScale, localScale, localScale));
            geometry.Geometry = UtilityWPF.GetMultiRingedTube(cylinderSegments, rings, true, true, transformInitial);

            retVal.Children.Add(geometry);

            #endregion

            #region Axis

            geometry = new GeometryModel3D();
            material = new MaterialGroup();
            diffuse = new DiffuseMaterial(new SolidColorBrush(WorldColors.ConverterEnergy));
            this.MaterialBrushes.Add(new MaterialColorProps(diffuse, WorldColors.ConverterEnergy));
            material.Children.Add(diffuse);
            specular = WorldColors.ConverterEnergySpecular;
            this.MaterialBrushes.Add(new MaterialColorProps(specular));
            material.Children.Add(specular);

            if (!isFinal)
            {
                EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
                material.Children.Add(selectionEmissive);
                this.SelectionEmissives.Add(selectionEmissive);
            }

            geometry.Material = material;
            geometry.BackMaterial = material;

            geometry.Geometry = UtilityWPF.GetCapsule_AlongZ(cylinderSegments, domeSegments, .15 * SCALE, SCALE);

            retVal.Children.Add(geometry);

            #endregion

            // Transform
            retVal.Transform = GetTransformForGeometry(isFinal);

            // Exit Function
            return retVal;
        }

        #endregion
    }

    #endregion
    #region Class: ConverterFuelToEnergy

    public class ConverterFuelToEnergy : PartBase, IPartUpdatable
    {
        #region Declaration Section

        public const string PARTTYPE = "ConverterFuelToEnergy";

        private ItemOptions _itemOptions = null;

        private Converter _converter = null;

        #endregion

        #region Constructor

        public ConverterFuelToEnergy(EditorOptions options, ItemOptions itemOptions, ShipPartDNA dna, IContainer fuelTanks, IContainer energyTanks)
            : base(options, dna, itemOptions.FuelToEnergy_Damage.HitpointMin, itemOptions.FuelToEnergy_Damage.HitpointSlope, itemOptions.FuelToEnergy_Damage.Damage)
        {
            _itemOptions = itemOptions;

            this.Design = new ConverterFuelToEnergyDesign(options, true);
            this.Design.SetDNA(dna);

            double volume = GetVolume(out _scaleActual, dna);

            if (fuelTanks != null && energyTanks != null)
            {
                _converter = new Converter(fuelTanks, energyTanks, itemOptions.FuelToEnergy_ConversionRate, itemOptions.FuelToEnergy_AmountToDraw * volume);
            }

            _mass = volume * itemOptions.FuelToEnergy_Density;
        }

        #endregion

        #region IPartUpdatable Members

        public void Update_MainThread(double elapsedTime)
        {
        }
        public void Update_AnyThread(double elapsedTime)
        {
            if (this.IsDestroyed)
            {
                return;
            }

            this.Transfer(elapsedTime, 1);
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

        private double _mass = 0d;
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

        private Vector3D _scaleActual;
        public override Vector3D ScaleActual
        {
            get
            {
                return _scaleActual;
            }
        }

        #endregion

        #region Public Methods

        public void Transfer(double elapsedTime, double percent)
        {
            if (_converter != null)
            {
                _converter.Transfer(elapsedTime, percent);
            }
        }

        #endregion

        #region Private Methods

        private static double GetVolume(out Vector3D actualScale, ShipPartDNA dna)
        {
            // In reality, it's an odd shape.  But for this, just assume an ellipse
            double radX = dna.Scale.X * ConverterFuelToEnergyDesign.RADIUSPERCENTOFSCALE;
            double radY = dna.Scale.Y * ConverterFuelToEnergyDesign.RADIUSPERCENTOFSCALE;
            double radZ = dna.Scale.Z * ConverterFuelToEnergyDesign.HEIGHTPERCENTOFSCALE;		// even though the const says height, it's really a radius

            actualScale = new Vector3D(radX * 2d, radY * 2d, radZ * 2d);

            return 4d / 3d * Math.PI * radX * radY * radZ;
        }

        #endregion
    }

    #endregion
}
