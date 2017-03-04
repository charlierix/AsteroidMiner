using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using Game.Newt.v2.GameItems.ShipEditor;
using Game.HelperClassesWPF;

namespace Game.Newt.v2.GameItems.ShipParts
{
    #region Class: GrappleGunToolItem

    public class GrappleGunToolItem : PartToolItemBase
    {
        #region Constructor

        public GrappleGunToolItem(EditorOptions options)
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
                return "Grapple Gun";
            }
        }
        public override string Description
        {
            get
            {
                return "Fires a line that attaches to whatever it hits, consumes a small amount of energy each time it's fired";
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
            return new GrappleGunDesign(this.Options, false);
        }

        #endregion
    }

    #endregion
    #region Class: GrappleGunDesign

    public class GrappleGunDesign : PartDesignBase
    {
        #region Declaration Section

        public const PartDesignAllowedScale ALLOWEDSCALE = PartDesignAllowedScale.XYZ;		// This is here so the scale can be known through reflection

        #endregion

        #region Constructor

        public GrappleGunDesign(EditorOptions options, bool isFinalModel)
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

        public override PartToolItemBase GetToolItem()
        {
            return new GrappleGunToolItem(this.Options);
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

            int domeSegments = isFinal ? 2 : 10;
            int cylinderSegments = isFinal ? 6 : 35;

            #region Mount Box

            geometry = new GeometryModel3D();
            material = new MaterialGroup();
            diffuse = new DiffuseMaterial(new SolidColorBrush(WorldColors.GunBase_Color));
            this.MaterialBrushes.Add(new MaterialColorProps(diffuse, WorldColors.GunBase_Color));
            material.Children.Add(diffuse);
            specular = WorldColors.GunBase_Specular;
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
            diffuse = new DiffuseMaterial(new SolidColorBrush(WorldColors.GunBarrel_Color));
            this.MaterialBrushes.Add(new MaterialColorProps(diffuse, WorldColors.GunBarrel_Color));
            material.Children.Add(diffuse);
            specular = WorldColors.GunBarrel_Specular;
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

            rings.Add(new TubeRingRegularPolygon(0, false, OUTERRADIUS * 1.75d, OUTERRADIUS * 1.75d, false));		// Start at the base of the barrel
            rings.Add(new TubeRingRegularPolygon(.49, false, OUTERRADIUS * 1.75d, OUTERRADIUS * 1.75d, false));
            rings.Add(new TubeRingRegularPolygon(.02, false, OUTERRADIUS, OUTERRADIUS, false));
            rings.Add(new TubeRingRegularPolygon(.24, false, OUTERRADIUS, OUTERRADIUS, false));
            rings.Add(new TubeRingRegularPolygon(.01, false, OUTERRADIUS * 1.25d, OUTERRADIUS * 1.25d, false));
            rings.Add(new TubeRingRegularPolygon(.08, false, OUTERRADIUS * 1.25d, OUTERRADIUS * 1.25d, false));		// This is the tip of the barrel
            rings.Add(new TubeRingRegularPolygon(0, false, INNERRADIUS, INNERRADIUS, false));		// Curl to the inside
            rings.Add(new TubeRingRegularPolygon(-.75d, false, INNERRADIUS, INNERRADIUS, false));		// Loop back to the base

            geometry.Geometry = UtilityWPF.GetMultiRingedTube(cylinderSegments, rings, true, false, new TranslateTransform3D(0, 0, -.49d));

            retVal.Children.Add(geometry);

            #endregion

            #region Grapple Pad

            geometry = new GeometryModel3D();
            material = new MaterialGroup();
            diffuse = new DiffuseMaterial(new SolidColorBrush(WorldColors.GrapplePad_Color));
            this.MaterialBrushes.Add(new MaterialColorProps(diffuse, WorldColors.GrapplePad_Color));
            material.Children.Add(diffuse);
            specular = WorldColors.GrapplePad_Specular;
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
            rings.Add(new TubeRingRegularPolygon(0, false, OUTERRADIUS * 2d, OUTERRADIUS * 2d, false));
            rings.Add(new TubeRingRegularPolygon(.1, false, OUTERRADIUS * 4d, OUTERRADIUS * 4d, false));
            rings.Add(new TubeRingDome(.05, false, domeSegments));

            geometry.Geometry = UtilityWPF.GetMultiRingedTube(cylinderSegments, rings, true, false, new TranslateTransform3D(0, 0, .35d));

            retVal.Children.Add(geometry);

            #endregion

            #region Trim

            geometry = new GeometryModel3D();
            material = new MaterialGroup();
            diffuse = new DiffuseMaterial(new SolidColorBrush(WorldColors.GunTrim_Color));
            this.MaterialBrushes.Add(new MaterialColorProps(diffuse, WorldColors.GunTrim_Color));
            material.Children.Add(diffuse);
            specular = WorldColors.GunTrim_Specular;
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
    #region Class: GrappleGun

    public class GrappleGun
    {
        public const string PARTTYPE = "GrappleGun";
    }

    #endregion
}
