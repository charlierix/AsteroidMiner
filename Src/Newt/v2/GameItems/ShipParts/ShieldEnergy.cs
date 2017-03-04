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
    #region Class: ShieldEnergyToolItem

    public class ShieldEnergyToolItem : PartToolItemBase
    {
        #region Constructor

        public ShieldEnergyToolItem(EditorOptions options)
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
                return "Energy shields";
            }
        }
        public override string Description
        {
            get
            {
                return "Blocks radiation and energy weapons (consumes energy)";
            }
        }
        public override string Category
        {
            get
            {
                return PartToolItemBase.CATEGORY_SHIELD;
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
            return new ShieldEnergyDesign(this.Options, false);
        }

        #endregion
    }

    #endregion
    #region Class: ShieldEnergyDesign

    public class ShieldEnergyDesign : PartDesignBase
    {
        #region Declaration Section

        public const PartDesignAllowedScale ALLOWEDSCALE = PartDesignAllowedScale.XYZ;		// This is here so the scale can be known through reflection

        private Tuple<UtilityNewt.IObjectMassBreakdown, Vector3D, double> _massBreakdown = null;

        #endregion

        #region Constructor

        public ShieldEnergyDesign(EditorOptions options, bool isFinalModel)
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
            return CreateCollisionHull(world, this.Scale, this.Orientation, this.Position);
        }

        internal static CollisionHull CreateCollisionHull(WorldBase world, Vector3D scale, Quaternion orientation, Point3D position)
        {
            Transform3DGroup transform = new Transform3DGroup();
            //transform.Children.Add(new ScaleTransform3D(scale));		// it ignores scale
            transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(orientation)));
            transform.Children.Add(new TranslateTransform3D(position.ToVector()));

            Vector3D size = new Vector3D(scale.X * .5d, scale.Y * .5d, scale.Z * .5d);

            return CollisionHull.CreateSphere(world, 0, size, transform.Value);
        }

        public override UtilityNewt.IObjectMassBreakdown GetMassBreakdown(double cellSize)
        {
            if (_massBreakdown != null && _massBreakdown.Item2 == this.Scale && _massBreakdown.Item3 == cellSize)
            {
                // This has already been built for this size
                return _massBreakdown.Item1;
            }

            var breakdown = GetMassBreakdown(this.Scale, cellSize);

            // Store this
            _massBreakdown = new Tuple<UtilityNewt.IObjectMassBreakdown, Vector3D, double>(breakdown, this.Scale, cellSize);

            // Exit Function
            return _massBreakdown.Item1;
        }

        internal static UtilityNewt.IObjectMassBreakdown GetMassBreakdown(Vector3D scale, double cellSize)
        {
            // Convert this.Scale into a size that the mass breakdown will use
            Vector3D size = new Vector3D(scale.X, scale.Y, scale.Z);

            return UtilityNewt.GetMassBreakdown(UtilityNewt.ObjectBreakdownType.Sphere, UtilityNewt.MassDistribution.Uniform, size, cellSize);
        }

        public override PartToolItemBase GetToolItem()
        {
            return new ShieldEnergyToolItem(this.Options);
        }

        #endregion

        #region Private Methods

        private Model3DGroup CreateGeometry(bool isFinal)
        {
            return CreateGeometry(this.MaterialBrushes, this.SelectionEmissives,
                GetTransformForGeometry(isFinal),
                WorldColors.ShieldBase_Color, WorldColors.ShieldBase_Specular, WorldColors.ShieldBase_Emissive, WorldColors.ShieldEnergy_Color, WorldColors.ShieldEnergy_Specular,
                isFinal);
        }

        internal static Model3DGroup CreateGeometry(List<MaterialColorProps> materialBrushes, List<EmissiveMaterial> selectionEmissives, Transform3D transform, Color baseColor, SpecularMaterial baseSpecular, EmissiveMaterial baseEmissive, Color colorColor, SpecularMaterial colorSpecular, bool isFinal)
        {
            const double SCALE = .5d;

            Model3DGroup retVal = new Model3DGroup();

            GeometryModel3D geometry;
            MaterialGroup material;
            DiffuseMaterial diffuse;
            SpecularMaterial specular;
            EmissiveMaterial emissive;

            int numSegments = isFinal ? 3 : 20;

            #region Main Sphere

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

            geometry.Geometry = UtilityWPF.GetSphere_LatLon(numSegments, .9 * SCALE, .9 * SCALE, .65d * SCALE);

            retVal.Children.Add(geometry);

            #endregion

            #region Ring

            geometry = new GeometryModel3D();
            material = new MaterialGroup();
            diffuse = new DiffuseMaterial(new SolidColorBrush(baseColor));
            materialBrushes.Add(new MaterialColorProps(diffuse, baseColor));
            material.Children.Add(diffuse);
            specular = baseSpecular;
            materialBrushes.Add(new MaterialColorProps(specular));
            material.Children.Add(specular);
            emissive = baseEmissive;
            materialBrushes.Add(new MaterialColorProps(emissive));
            material.Children.Add(emissive);

            if (!isFinal)
            {
                EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
                material.Children.Add(selectionEmissive);
                selectionEmissives.Add(selectionEmissive);
            }

            geometry.Material = material;
            geometry.BackMaterial = material;

            geometry.Geometry = UtilityWPF.GetSphere_LatLon(numSegments, 1d * SCALE, 1d * SCALE, .15d * SCALE);

            retVal.Children.Add(geometry);

            #endregion

            #region Axis

            geometry = new GeometryModel3D();
            material = new MaterialGroup();
            diffuse = new DiffuseMaterial(new SolidColorBrush(baseColor));
            materialBrushes.Add(new MaterialColorProps(diffuse, baseColor));
            material.Children.Add(diffuse);
            specular = baseSpecular;
            materialBrushes.Add(new MaterialColorProps(specular));
            material.Children.Add(specular);
            emissive = baseEmissive;
            materialBrushes.Add(new MaterialColorProps(emissive));
            material.Children.Add(emissive);

            if (!isFinal)
            {
                EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
                material.Children.Add(selectionEmissive);
                selectionEmissives.Add(selectionEmissive);
            }

            geometry.Material = material;
            geometry.BackMaterial = material;

            geometry.Geometry = UtilityWPF.GetSphere_LatLon(numSegments, .25d * SCALE, .25d * SCALE, 1d * SCALE);

            retVal.Children.Add(geometry);

            #endregion

            // Transform
            retVal.Transform = transform;

            // Exit Function
            return retVal;
        }

        #endregion
    }

    #endregion
    #region Class: ShieldEnergy

    public class ShieldEnergy : PartBase
    {
        #region Declaration Section

        public const string PARTTYPE = "ShieldEnergy";

        private readonly ItemOptions _itemOptions;

        private readonly IContainer _plasma;

        #endregion

        #region Constructor

        public ShieldEnergy(EditorOptions options, ItemOptions itemOptions, ShipPartDNA dna, IContainer plasma)
            : base(options, dna, itemOptions.Shield_Damage.HitpointMin, itemOptions.Shield_Damage.HitpointSlope, itemOptions.Shield_Damage.Damage)
        {
            _itemOptions = itemOptions;
            _plasma = plasma;

            this.Design = new ShieldEnergyDesign(options, true);
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

        internal static void GetMass(out double mass, out double volume, out double radius, out Vector3D actualScale, ShipPartDNA dna, ItemOptions itemOptions)
        {
            // Just assume it's a sphere
            double radX = dna.Scale.X * .5;
            double radY = dna.Scale.Y * .5;
            double radZ = dna.Scale.Z * .5;

            radius = (radX + radY + radZ) / 3d;

            actualScale = new Vector3D(radX * 2d, radY * 2d, radZ * 2d);

            volume = (4d / 3d) * Math.PI * radX * radY * radZ;

            mass = volume * itemOptions.Shield_Density;
        }

        #endregion
    }

    #endregion
}
