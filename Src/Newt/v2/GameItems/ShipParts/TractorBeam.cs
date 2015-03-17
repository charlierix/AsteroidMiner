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
    //TODO:  Draw small visual whenever activated

    #region Class: TractorBeamToolItem

    public class TractorBeamToolItem : PartToolItemBase
    {
        #region Constructor

        public TractorBeamToolItem(EditorOptions options)
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
                return "Tractor Beam";
            }
        }
        public override string Description
        {
            get
            {
                return "Consumes energy, and produces force (will only produce force when pushing against other objects)";
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
            return new TractorBeamDesign(this.Options);
        }

        #endregion
    }

    #endregion
    #region Class: TractorBeamDesign

    public class TractorBeamDesign : PartDesignBase
    {
        #region Declaration Section

        public const double RADIUSPERCENTOFSCALE = .2d;
        public const double HEIGHTPERCENTOFSCALE = .6d;

        public const PartDesignAllowedScale ALLOWEDSCALE = PartDesignAllowedScale.XYZ;		// This is here so the scale can be known through reflection

        private Tuple<UtilityNewt.IObjectMassBreakdown, Vector3D, double> _massBreakdown = null;

        #endregion

        #region Constructor

        public TractorBeamDesign(EditorOptions options)
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
            double height = HEIGHTPERCENTOFSCALE * scale.Z;

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
            Vector3D size = new Vector3D(this.Scale.Z * HEIGHTPERCENTOFSCALE, this.Scale.X * RADIUSPERCENTOFSCALE * 2d, this.Scale.Y * RADIUSPERCENTOFSCALE * 2d);

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
            const double SCALE = 1d / ((1.5d + 1d) * 2d);		// the rod is longer than the base, so double that

            ScaleTransform3D transform = new ScaleTransform3D(SCALE, SCALE, SCALE);

            int domeSegments = isFinal ? 2 : 10;
            int cylinderSegments = isFinal ? 6 : 35;

            Model3DGroup retVal = new Model3DGroup();

            #region Base

            GeometryModel3D geometry = new GeometryModel3D();
            MaterialGroup material = new MaterialGroup();
            DiffuseMaterial diffuse = new DiffuseMaterial(new SolidColorBrush(WorldColors.TractorBeamBase));
            this.MaterialBrushes.Add(new MaterialColorProps(diffuse, WorldColors.TractorBeamBase));
            material.Children.Add(diffuse);

            if (!isFinal)
            {
                EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
                material.Children.Add(selectionEmissive);
                base.SelectionEmissives.Add(selectionEmissive);
            }

            geometry.Material = material;
            geometry.BackMaterial = material;

            List<TubeRingBase> rings = new List<TubeRingBase>();
            rings.Add(new TubeRingPoint(0, false));
            rings.Add(new TubeRingRegularPolygon(.3, false, .5, .5, false));
            rings.Add(new TubeRingRegularPolygon(2, false, 1, 1, false));
            rings.Add(new TubeRingDome(.66, false, domeSegments));

            geometry.Geometry = UtilityWPF.GetMultiRingedTube(cylinderSegments, rings, true, true, transform);

            retVal.Children.Add(geometry);

            #endregion
            #region Rod

            geometry = new GeometryModel3D();
            material = new MaterialGroup();
            diffuse = new DiffuseMaterial(new SolidColorBrush(WorldColors.TractorBeamRod));
            this.MaterialBrushes.Add(new MaterialColorProps(diffuse, WorldColors.TractorBeamRod));
            material.Children.Add(diffuse);
            SpecularMaterial specular = WorldColors.TractorBeamRodSpecular;
            this.MaterialBrushes.Add(new MaterialColorProps(specular));
            material.Children.Add(specular);
            EmissiveMaterial emissive = WorldColors.TractorBeamRodEmissive;
            this.MaterialBrushes.Add(new MaterialColorProps(emissive));
            material.Children.Add(emissive);

            if (!isFinal)
            {
                EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
                material.Children.Add(selectionEmissive);
                base.SelectionEmissives.Add(selectionEmissive);
            }

            geometry.Material = material;
            geometry.BackMaterial = material;

            rings = new List<TubeRingBase>();
            rings.Add(new TubeRingRegularPolygon(0, false, .25, .25, false));
            rings.Add(new TubeRingRegularPolygon(1.5, false, .25, .25, false));
            rings.Add(new TubeRingDome(1, false, domeSegments));

            geometry.Geometry = UtilityWPF.GetMultiRingedTube(cylinderSegments, rings, true, false, transform);

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
    #region Class: TractorBeam

    public class TractorBeam : PartBase
    {
        #region Declaration Section

        public const string PARTTYPE = "TractorBeam";

        private readonly ItemOptions _itemOptions;

        private readonly IContainer _plasma;

        #endregion

        #region Constructor

        public TractorBeam(EditorOptions options, ItemOptions itemOptions, PartDNA dna, IContainer plasma)
            : base(options, dna)
        {
            _itemOptions = itemOptions;
            _plasma = plasma;

            this.Design = new TractorBeamDesign(options);
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
            double radX = dna.Scale.X * .5 * TractorBeamDesign.RADIUSPERCENTOFSCALE;
            double radY = dna.Scale.Y * .5 * TractorBeamDesign.RADIUSPERCENTOFSCALE;
            double height = dna.Scale.Z * TractorBeamDesign.HEIGHTPERCENTOFSCALE;

            radius = (radX + radY + (height * .5d)) / 3d;		// this is just an approximation for the neural container

            actualScale = new Vector3D(radX * 2d, radY * 2d, height);

            volume = Math.PI * radX * radY * height;

            mass = volume * itemOptions.TractorBeamDensity;
        }

        #endregion
    }

    #endregion
}
