using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using Game.Newt.AsteroidMiner2.ShipEditor;
using Game.Newt.HelperClasses;

namespace Game.Newt.AsteroidMiner2.ShipParts
{
    #region Class: ShieldEnergyToolItem

    public class ShieldEnergyToolItem : PartToolItemBase
    {
        #region Constructor

        public ShieldEnergyToolItem(EditorOptions options)
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
            return new ShieldEnergyDesign(this.Options);
        }

        #endregion
    }

    #endregion
    #region Class: ShieldEnergyDesign

    public class ShieldEnergyDesign : PartDesignBase
    {
        #region Declaration Section

        public const PartDesignAllowedScale ALLOWEDSCALE = PartDesignAllowedScale.XYZ;		// This is here so the scale can be known through reflection

        #endregion

        #region Constructor

        public ShieldEnergyDesign(EditorOptions options)
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

        #endregion

        #region Private Methods

        private Model3DGroup CreateGeometry(bool isFinal)
        {
            return CreateGeometry(this.MaterialBrushes, this.SelectionEmissives,
                GetTransformForGeometry(isFinal),
                WorldColors.ShieldBase, WorldColors.ShieldBaseSpecular, WorldColors.ShieldBaseEmissive, WorldColors.ShieldEnergy, WorldColors.ShieldEnergySpecular,
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

            geometry.Geometry = UtilityWPF.GetSphere(numSegments, .9 * SCALE, .9 * SCALE, .65d * SCALE);

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

            geometry.Geometry = UtilityWPF.GetSphere(numSegments, 1d * SCALE, 1d * SCALE, .15d * SCALE);

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

            geometry.Geometry = UtilityWPF.GetSphere(numSegments, .25d * SCALE, .25d * SCALE, 1d * SCALE);

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

    public class ShieldEnergy
    {
        public const string PARTTYPE = "ShieldEnergy";
    }

    #endregion
}
