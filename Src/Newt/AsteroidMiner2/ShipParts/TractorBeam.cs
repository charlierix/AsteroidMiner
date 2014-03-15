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

        public const PartDesignAllowedScale ALLOWEDSCALE = PartDesignAllowedScale.XYZ;		// This is here so the scale can be known through reflection

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

        #endregion

        #region Private Methods

        private Model3DGroup CreateGeometry(bool isFinal)
        {
            const double SCALE = 1d / ((1.5d + 1d) * 2d);		// the rod is longer than the base, so double that

            ScaleTransform3D transform = new ScaleTransform3D(SCALE, SCALE, SCALE);

            int domeSegments = isFinal ? 2 : 10;
            int cylinderSegments = isFinal ? 6 : 35;

            Model3DGroup retVal = new Model3DGroup();

            // Geometry Model1
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

            // Geometry Model2
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

            // Transform
            retVal.Transform = GetTransformForGeometry(isFinal);

            // Exit Function
            return retVal;
        }

        #endregion
    }

    #endregion
    #region Class: TractorBeam

    public class TractorBeam
    {
        public const string PARTTYPE = "TractorBeam";
    }

    #endregion
}
