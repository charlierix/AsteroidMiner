using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Game.HelperClassesWPF;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.GameItems.ShipParts
{
    #region Class: BeamGunToolItem

    public class BeamGunToolItem : PartToolItemBase
    {
        #region Constructor

        public BeamGunToolItem(EditorOptions options)
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
                return "Beam Gun";
            }
        }
        public override string Description
        {
            get
            {
                return "Short range energy weapon.  More like a laser sword or flame thrower than a laser gun.  Consumes energy";
            }
        }
        public override string Category
        {
            get
            {
                return PartToolItemBase.CATEGORY_WEAPON;
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
            return new BeamGunDesign(this.Options);
        }

        #endregion
    }

    #endregion
    #region Class: BeamGunDesign

    public class BeamGunDesign : PartDesignBase
    {
        #region Declaration Section

        public const double RADIUSPERCENTOFSCALE = .15d;

        public const PartDesignAllowedScale ALLOWEDSCALE = PartDesignAllowedScale.XYZ;		// This is here so the scale can be known through reflection

        private Tuple<UtilityNewt.IObjectMassBreakdown, Vector3D, double> _massBreakdown = null;

        #endregion

        #region Constructor

        public BeamGunDesign(EditorOptions options)
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

        private Model3DGroup _geometries = null;
        public override Model3D Model
        {
            get
            {
                if (_geometries == null)
                {
                    _geometries = CreateGeometry(false);
                }

                return _geometries;
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
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90)));		// the physics hull is along x, but dna is along z
            transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(this.Orientation)));
            transform.Children.Add(new TranslateTransform3D(this.Position.ToVector()));

            Vector3D scale = this.Scale;
            double radius = RADIUSPERCENTOFSCALE * ((scale.X + scale.Y) * .5d);
            double height = scale.Z;

            return CollisionHull.CreateCylinder(world, 0, radius, height, transform.Value);
        }

        public override UtilityNewt.IObjectMassBreakdown GetMassBreakdown(double cellSize)
        {
            if (_massBreakdown != null && _massBreakdown.Item2 == this.Scale && _massBreakdown.Item3 == cellSize)
            {
                // This has already been built for this size
                return _massBreakdown.Item1;
            }

            // Convert this.Scale into a size that the mass breakdown will use (mass breakdown wants height along X, and scale is for radius, but the mass breakdown wants diameter
            // Reducing Z a bit, because the energy tank has a rounded cap
            Vector3D size = new Vector3D(this.Scale.Z, this.Scale.X * RADIUSPERCENTOFSCALE * 2d, this.Scale.Y * RADIUSPERCENTOFSCALE * 2d);

            // Cylinder
            UtilityNewt.ObjectMassBreakdown cylinder = UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Cylinder, UtilityNewt.MassDistribution.Uniform, size, cellSize);

            Transform3D transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90));		// the physics hull is along x, but dna is along z

            // Rotated
            UtilityNewt.ObjectMassBreakdownSet combined = new UtilityNewt.ObjectMassBreakdownSet(
                new UtilityNewt.ObjectMassBreakdown[] { cylinder },
                new Transform3D[] { transform });

            // Store this
            _massBreakdown = new Tuple<UtilityNewt.IObjectMassBreakdown, Vector3D, double>(combined, this.Scale, cellSize);

            // Exit Function
            return _massBreakdown.Item1;
        }

        #endregion

        #region Private Methods

        private Model3DGroup CreateGeometry(bool isFinal)
        {
            Model3DGroup retVal = new Model3DGroup();

            GeometryModel3D geometry;
            MaterialGroup material;
            DiffuseMaterial diffuse;
            SpecularMaterial specular;
            EmissiveMaterial emissive;

            int domeSegments = isFinal ? 2 : 10;
            int cylinderSegments = isFinal ? 6 : 35;

            #region Mount Box

            geometry = new GeometryModel3D();
            material = new MaterialGroup();
            diffuse = new DiffuseMaterial(new SolidColorBrush(WorldColors.GunBase));
            this.MaterialBrushes.Add(new MaterialColorProps(diffuse, WorldColors.GunBase));
            material.Children.Add(diffuse);
            specular = WorldColors.GunBaseSpecular;
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

            geometry.Geometry = UtilityWPF.GetCube(new Point3D(-.115, -.1, -.5), new Point3D(.115, .1, -.1));

            retVal.Children.Add(geometry);

            #endregion

            #region Barrel

            geometry = new GeometryModel3D();
            material = new MaterialGroup();
            diffuse = new DiffuseMaterial(new SolidColorBrush(WorldColors.GunBarrel));
            this.MaterialBrushes.Add(new MaterialColorProps(diffuse, WorldColors.GunBarrel));
            material.Children.Add(diffuse);
            specular = WorldColors.GunBarrelSpecular;
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

            const double OUTERRADIUS = .045;
            const double INNERRADIUS = .04;

            List<TubeRingBase> rings = new List<TubeRingBase>();

            rings.Add(new TubeRingRegularPolygon(0, false, OUTERRADIUS * .33, OUTERRADIUS * .33, false));		// Start at the base of the barrel
            rings.Add(new TubeRingRegularPolygon(.69, false, OUTERRADIUS * .33, OUTERRADIUS * .33, false));		// This is the tip of the barrel
            //rings.Add(new TubeRingRegularPolygon(0, false, INNERRADIUS, INNERRADIUS, false));		// Curl to the inside
            //rings.Add(new TubeRingRegularPolygon(-.75d, false, INNERRADIUS, INNERRADIUS, false));		// Loop back to the base

            geometry.Geometry = UtilityWPF.GetMultiRingedTube(cylinderSegments, rings, true, false, new TranslateTransform3D(0, 0, -.49d));

            retVal.Children.Add(geometry);

            #endregion

            #region Dish

            geometry = new GeometryModel3D();
            material = new MaterialGroup();
            diffuse = new DiffuseMaterial(new SolidColorBrush(WorldColors.BeamGunDish));
            this.MaterialBrushes.Add(new MaterialColorProps(diffuse, WorldColors.BeamGunDish));
            material.Children.Add(diffuse);
            specular = WorldColors.BeamGunDishSpecular;
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

            rings = new List<TubeRingBase>();
            rings.Add(new TubeRingDome(0, false, domeSegments));
            rings.Add(new TubeRingRegularPolygon(.16, false, OUTERRADIUS * 3.5d, OUTERRADIUS * 3.5d, false));
            rings.Add(new TubeRingDome(-.08, false, domeSegments));		// concave dish

            geometry.Geometry = UtilityWPF.GetMultiRingedTube(cylinderSegments, rings, true, false, new TranslateTransform3D(0, 0, -.12d));

            retVal.Children.Add(geometry);

            #endregion

            #region Focus Crystal

            geometry = new GeometryModel3D();
            material = new MaterialGroup();
            diffuse = new DiffuseMaterial(new SolidColorBrush(WorldColors.BeamGunCrystal));
            this.MaterialBrushes.Add(new MaterialColorProps(diffuse, WorldColors.BeamGunCrystal));
            material.Children.Add(diffuse);
            specular = WorldColors.BeamGunCrystalSpecular;
            this.MaterialBrushes.Add(new MaterialColorProps(specular));
            material.Children.Add(specular);
            emissive = WorldColors.BeamGunCrystalEmissive;
            this.MaterialBrushes.Add(new MaterialColorProps(emissive));
            material.Children.Add(emissive);

            if (!isFinal)
            {
                EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
                material.Children.Add(selectionEmissive);
                this.SelectionEmissives.Add(selectionEmissive);
            }

            geometry.Material = material;
            geometry.BackMaterial = material;

            //rings = new List<TubeRingBase>();
            //rings.Add(new TubeRingPoint(0, false));
            //rings.Add(new TubeRingRegularPolygon(.667, false, OUTERRADIUS * 2d, OUTERRADIUS * 2d, false));
            //rings.Add(new TubeRingPoint(1.33, false));

            //geometry.Geometry = UtilityWPF.GetMultiRingedTube(3, rings, true, false, new TranslateTransform3D(0, 0, .25d));

            List<TubeRingDefinition_ORIG> rings2 = new List<TubeRingDefinition_ORIG>();
            rings2.Add(new TubeRingDefinition_ORIG(0, false));
            rings2.Add(new TubeRingDefinition_ORIG(OUTERRADIUS * 3d, OUTERRADIUS * 3d, .067d, true, false));		// I think the old tube definition is diameter instead of radius, so needs to be doubled
            rings2.Add(new TubeRingDefinition_ORIG(.133, false));

            geometry.Geometry = UtilityWPF.GetMultiRingedTube_ORIG(5, rings2, false, false);
            geometry.Transform = new TranslateTransform3D(0, 0, .15d);

            retVal.Children.Add(geometry);

            #endregion

            #region Trim

            geometry = new GeometryModel3D();
            material = new MaterialGroup();
            diffuse = new DiffuseMaterial(new SolidColorBrush(WorldColors.BeamGunTrim));
            this.MaterialBrushes.Add(new MaterialColorProps(diffuse, WorldColors.BeamGunTrim));
            material.Children.Add(diffuse);
            specular = WorldColors.BeamGunTrimSpecular;
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

            geometry.Geometry = UtilityWPF.GetCube(new Point3D(-.095, -.11, -.45), new Point3D(.095, .11, -.15));

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
    #region Class: BeamGun

    public class BeamGun : PartBase
    {
        #region Declaration Section

        public const string PARTTYPE = "BeamGun";

        private readonly ItemOptions _itemOptions;

        private readonly IContainer _plasma;

        #endregion

        #region Constructor

        public BeamGun(EditorOptions options, ItemOptions itemOptions, PartDNA dna, IContainer plasma)
            : base(options, dna)
        {
            _itemOptions = itemOptions;
            _plasma = plasma;

            this.Design = new BeamGunDesign(options);
            this.Design.SetDNA(dna);

            double volume, radius;
            GetMass(out _mass, out volume, out radius, out _scaleActual, dna, itemOptions);

            //this.Radius = radius;

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

        #endregion

        #region Private Methods

        private static void GetMass(out double mass, out double volume, out double radius, out Vector3D actualScale, PartDNA dna, ItemOptions itemOptions)
        {
            // Just assume it's a cylinder
            double radX = dna.Scale.X * .5 * BeamGunDesign.RADIUSPERCENTOFSCALE;
            double radY = dna.Scale.Y * .5 * BeamGunDesign.RADIUSPERCENTOFSCALE;
            double height = dna.Scale.Z;

            radius = (radX + radY + (height * .5d)) / 3d;		// this is just an approximation for the neural container

            actualScale = new Vector3D(radX * 2d, radY * 2d, height);

            volume = Math.PI * radX * radY * height;

            mass = volume * itemOptions.BeamGunDensity;
        }

        #endregion
    }

    #endregion
}
