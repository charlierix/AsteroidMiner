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
    #region Class: SelfRepairToolItem

    public class SelfRepairToolItem : PartToolItemBase
    {
        #region Constructor

        public SelfRepairToolItem(EditorOptions options)
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
                return "Self Repair";
            }
        }
        public override string Description
        {
            get
            {
                return "Consumes energy and repairs broken parts";
            }
        }
        public override string Category
        {
            get
            {
                return PartToolItemBase.CATEGORY_EQUIPMENT;		// in a way, this is a converter
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
            return new SelfRepairDesign(this.Options, false);
        }

        #endregion
    }

    #endregion
    #region Class: SelfRepairDesign

    public class SelfRepairDesign : PartDesignBase
    {
        #region Declaration Section

        public const PartDesignAllowedScale ALLOWEDSCALE = PartDesignAllowedScale.XYZ;		// This is here so the scale can be known through reflection

        #endregion

        #region Constructor

        public SelfRepairDesign(EditorOptions options, bool isFinalModel)
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
            return new SelfRepairToolItem(this.Options);
        }

        #endregion

        #region Private Methods

        private Model3DGroup CreateGeometry(bool isFinal)
        {
            double SQRT2 = Math.Sqrt(2d);
            double SCALE = .5d / (SQRT2 * .5d);

            Model3DGroup retVal = new Model3DGroup();

            GeometryModel3D geometry;
            MaterialGroup material;
            DiffuseMaterial diffuse;
            SpecularMaterial specular;

            #region Main Box

            geometry = new GeometryModel3D();
            material = new MaterialGroup();
            diffuse = new DiffuseMaterial(new SolidColorBrush(WorldColors.SelfRepairBase));
            this.MaterialBrushes.Add(new MaterialColorProps(diffuse, WorldColors.SelfRepairBase));
            material.Children.Add(diffuse);
            specular = WorldColors.SelfRepairBaseSpecular;
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
            rings.Add(new TubeRingRegularPolygon(0d, false, SQRT2 * .35d * SCALE, .4d * SCALE, true));
            rings.Add(new TubeRingRegularPolygon(.18d * SCALE, false, SQRT2 * .5d * SCALE, .5d * SCALE, true));
            rings.Add(new TubeRingRegularPolygon(.24d * SCALE, false, SQRT2 * .5d * SCALE, .5d * SCALE, true));
            rings.Add(new TubeRingRegularPolygon(.18d * SCALE, false, SQRT2 * .35d * SCALE, .4d * SCALE, true));
            geometry.Geometry = UtilityWPF.GetMultiRingedTube(7, rings, true, true);

            retVal.Children.Add(geometry);

            #endregion

            #region Cross

            for (int cntr = 0; cntr <= 1; cntr++)
            {
                geometry = new GeometryModel3D();
                material = new MaterialGroup();
                diffuse = new DiffuseMaterial(new SolidColorBrush(WorldColors.SelfRepairCross));
                this.MaterialBrushes.Add(new MaterialColorProps(diffuse, WorldColors.SelfRepairCross));
                material.Children.Add(diffuse);
                specular = WorldColors.SelfRepairCrossSpecular;
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

                double halfWidth = cntr == 0 ? .1d : .3d;
                double halfHeight = cntr == 0 ? .3d : .1d;
                halfWidth *= SCALE;
                halfHeight *= SCALE;

                geometry.Geometry = UtilityWPF.GetCube_IndependentFaces(new Point3D(-halfWidth, -halfHeight, -.32d * SCALE), new Point3D(halfWidth, halfHeight, .32d * SCALE));

                retVal.Children.Add(geometry);
            }

            #endregion

            // Transform
            retVal.Transform = GetTransformForGeometry(isFinal);

            // Exit Function
            return retVal;
        }

        #endregion
    }

    #endregion
    #region Class: SelfRepair

    public class SelfRepair
    {
        public const string PARTTYPE = "SelfRepair";
    }

    #endregion
}
