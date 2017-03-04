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
    //TODO: There will be a camera eye, and rangefinder eye (and more, look in PartBase.ShipParts enum)
    //TODO: Use color options class

    #region Class: EyeToolItem

    public class EyeToolItem : PartToolItemBase
    {
        #region Constructor

        public EyeToolItem(EditorOptions options)
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
                return "Eye";
            }
        }
        public override string Description
        {
            get
            {
                return "Consumes energy, used to see other objects";
            }
        }
        public override string Category
        {
            get
            {
                return PartToolItemBase.CATEGORY_SENSOR;
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
            return new EyeDesign(this.Options, false);
        }

        #endregion
    }

    #endregion
    #region Class: EyeDesign

    public class EyeDesign : PartDesignBase
    {
        #region Declaration Section

        public const PartDesignAllowedScale ALLOWEDSCALE = PartDesignAllowedScale.XYZ;		// This is here so the scale can be known through reflection

        #endregion

        #region Constructor

        public EyeDesign(EditorOptions options, bool isFinalModel)
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
            return new EyeToolItem(this.Options);
        }

        #endregion

        #region Private Methods

        private Model3DGroup CreateGeometry(bool isFinal)
        {
            const double HEIGHT = 1.65d;
            const double SCALE = .33d / HEIGHT;

            ScaleTransform3D scaleTransform = new ScaleTransform3D(SCALE, SCALE, SCALE);

            int domeSegments = isFinal ? 2 : 10;
            int cylinderSegments = isFinal ? 6 : 35;

            Model3DGroup retVal = new Model3DGroup();

            GeometryModel3D geometry;
            MaterialGroup material;
            DiffuseMaterial diffuse;
            SpecularMaterial specular;

            Transform3DGroup transformGroup = new Transform3DGroup();
            transformGroup.Children.Add(new TranslateTransform3D(0, 0, (HEIGHT / 2d) - .15d));
            transformGroup.Children.Add(scaleTransform);

            #region Spotlight

            //// Even when I make it extreme, it doesn't seem to make the gradient brighter
            //SpotLight spotLight = new SpotLight();
            //spotLight.Color = Colors.White;
            ////spotLight.LinearAttenuation = 1d;
            //spotLight.LinearAttenuation = .1d;
            //spotLight.Range = 10;
            //spotLight.InnerConeAngle = 66;
            //spotLight.OuterConeAngle = 80;
            //spotLight.Direction = new Vector3D(0, 0, -1);
            //spotLight.Transform = new TranslateTransform3D(0, 0, 1);

            //retVal.Children.Add(spotLight);

            #endregion

            #region Back Lens

            if (!isFinal)
            {
                geometry = new GeometryModel3D();
                material = new MaterialGroup();

                RadialGradientBrush eyeBrush = new RadialGradientBrush();
                eyeBrush.GradientStops.Add(new GradientStop(UtilityWPF.ColorFromHex("#FFFFEA00"), 0d));
                eyeBrush.GradientStops.Add(new GradientStop(UtilityWPF.ColorFromHex("#FFF5E100"), 0.0187702d));
                eyeBrush.GradientStops.Add(new GradientStop(UtilityWPF.ColorFromHex("#FFECD800"), 0.0320388d));
                eyeBrush.GradientStops.Add(new GradientStop(UtilityWPF.ColorFromHex("#FFD46C00"), 0.0485437d));
                eyeBrush.GradientStops.Add(new GradientStop(UtilityWPF.ColorFromHex("#FFBC0000"), 0.104167d));
                eyeBrush.GradientStops.Add(new GradientStop(UtilityWPF.ColorFromHex("#FF8E0000"), 0.267322d));
                eyeBrush.GradientStops.Add(new GradientStop(UtilityWPF.ColorFromHex("#FF600000"), 0.486408d));
                eyeBrush.GradientStops.Add(new GradientStop(UtilityWPF.ColorFromHex("#FF3E0000"), 0.61068d));
                eyeBrush.GradientStops.Add(new GradientStop(UtilityWPF.ColorFromHex("#FF1D0000"), 0.713592d));
                eyeBrush.GradientStops.Add(new GradientStop(UtilityWPF.ColorFromHex("#FF0E0000"), 0.760544d));
                eyeBrush.GradientStops.Add(new GradientStop(UtilityWPF.ColorFromHex("#FF000000"), 1d));

                diffuse = new DiffuseMaterial(eyeBrush);
                this.MaterialBrushes.Add(new MaterialColorProps(diffuse, WorldColors.CameraLens_Color));		// using the final's lens color, because it's a solid color
                material.Children.Add(diffuse);

                //if (!isFinal)
                //{
                EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
                material.Children.Add(selectionEmissive);
                base.SelectionEmissives.Add(selectionEmissive);
                //}

                geometry.Material = material;
                geometry.BackMaterial = material;

                geometry.Geometry = UtilityWPF.GetCircle2D(cylinderSegments, transformGroup, transformGroup);

                retVal.Children.Add(geometry);
            }

            #endregion

            #region Glass Cover

            geometry = new GeometryModel3D();
            material = new MaterialGroup();

            if (isFinal)
            {
                material.Children.Add(new DiffuseMaterial(new SolidColorBrush(WorldColors.CameraLens_Color)));		// no need to add these to this.MaterialBrushes (those are only for editing)
                material.Children.Add(WorldColors.CameraLens_Specular);
            }
            else
            {
                //NOTE: Not using the world color, because that's for final.  The editor has a HAL9000 eye, and this is a glass plate
                Color color = Color.FromArgb(26, 255, 255, 255);
                diffuse = new DiffuseMaterial(new SolidColorBrush(color));
                this.MaterialBrushes.Add(new MaterialColorProps(diffuse, color));
                material.Children.Add(diffuse);
                specular = new SpecularMaterial(new SolidColorBrush(Color.FromArgb(224, 255, 255, 255)), 95d);
                this.MaterialBrushes.Add(new MaterialColorProps(specular));
                material.Children.Add(specular);

                EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
                material.Children.Add(selectionEmissive);
                base.SelectionEmissives.Add(selectionEmissive);
            }

            geometry.Material = material;
            geometry.BackMaterial = material;

            List<TubeRingBase> rings = new List<TubeRingBase>();
            rings.Add(new TubeRingRegularPolygon(0, false, 1, 1, false));
            rings.Add(new TubeRingDome(.15, false, domeSegments));

            geometry.Geometry = UtilityWPF.GetMultiRingedTube(cylinderSegments, rings, true, false, transformGroup);

            retVal.Children.Add(geometry);

            #endregion

            #region Silver Ring

            if (!isFinal)
            {
                geometry = new GeometryModel3D();
                material = new MaterialGroup();
                Color color = Color.FromRgb(90, 90, 90);
                diffuse = new DiffuseMaterial(new SolidColorBrush(color));
                this.MaterialBrushes.Add(new MaterialColorProps(diffuse, color));
                material.Children.Add(diffuse);
                specular = new SpecularMaterial(Brushes.White, 100d);
                this.MaterialBrushes.Add(new MaterialColorProps(specular));
                material.Children.Add(specular);

                //if (!isFinal)
                //{
                EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
                material.Children.Add(selectionEmissive);
                base.SelectionEmissives.Add(selectionEmissive);
                //}

                geometry.Material = material;
                geometry.BackMaterial = material;

                geometry.Geometry = UtilityWPF.GetRing(cylinderSegments, .97, 1.03, .05, transformGroup);

                retVal.Children.Add(geometry);
            }

            #endregion

            #region Back Cover

            geometry = new GeometryModel3D();
            material = new MaterialGroup();
            diffuse = new DiffuseMaterial(new SolidColorBrush(WorldColors.CameraBase_Color));
            this.MaterialBrushes.Add(new MaterialColorProps(diffuse, WorldColors.CameraBase_Color));
            material.Children.Add(diffuse);
            specular = WorldColors.CameraBase_Specular;
            this.MaterialBrushes.Add(new MaterialColorProps(specular));
            material.Children.Add(specular);

            if (!isFinal)
            {
                EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
                material.Children.Add(selectionEmissive);
                base.SelectionEmissives.Add(selectionEmissive);
            }

            geometry.Material = material;
            geometry.BackMaterial = material;

            rings = new List<TubeRingBase>();
            rings.Add(new TubeRingDome(0, false, domeSegments));
            rings.Add(new TubeRingRegularPolygon(1.5, false, 1, 1, false));

            transformGroup = new Transform3DGroup();
            transformGroup.Children.Add(new TranslateTransform3D(0, 0, 1.65 / -2d));
            transformGroup.Children.Add(scaleTransform);

            geometry.Geometry = UtilityWPF.GetMultiRingedTube(cylinderSegments, rings, true, false, transformGroup);

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
    #region Class: Eye

    public class Eye
    {
        public const string PARTTYPE = "Eye";
    }

    #endregion
}
