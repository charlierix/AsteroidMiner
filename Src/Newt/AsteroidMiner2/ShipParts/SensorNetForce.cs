﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using Game.Newt.AsteroidMiner2.ShipEditor;
using Game.Newt.HelperClasses;
using Game.Newt.NewtonDynamics;

namespace Game.Newt.AsteroidMiner2.ShipParts
{
    #region Class: SensorNetForceToolItem

    public class SensorNetForceToolItem : PartToolItemBase
    {
        #region Constructor

        public SensorNetForceToolItem(EditorOptions options)
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
                return "Net Force Sensor";
            }
        }
        public override string Description
        {
            get
            {
                return "Reports the net force (internal plus external forces)";
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
            return new SensorNetForceDesign(this.Options);
        }

        #endregion
    }

    #endregion
    #region Class: SensorNetForceDesign

    public class SensorNetForceDesign : PartDesignBase
    {
        #region Declaration Section

        public const PartDesignAllowedScale ALLOWEDSCALE = PartDesignAllowedScale.XYZ;		// This is here so the scale can be known through reflection

        private Tuple<UtilityNewt.IObjectMassBreakdown, Vector3D, double> _massBreakdown = null;

        #endregion

        #region Constructor

        public SensorNetForceDesign(EditorOptions options)
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

        private Model3DGroup _geometry = null;
        public override Model3D Model
        {
            get
            {
                if (_geometry == null)
                {
                    _geometry = CreateGeometry(false);
                }

                return _geometry;
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
            return SensorGravityDesign.CreateSensorCollisionHull(world, this.Scale, this.Orientation, this.Position);
        }
        public override UtilityNewt.IObjectMassBreakdown GetMassBreakdown(double cellSize)
        {
            return SensorGravityDesign.GetSensorMassBreakdown(ref _massBreakdown, this.Scale, cellSize);
        }

        #endregion

        #region Private Methods

        private Model3DGroup CreateGeometry(bool isFinal)
        {
            return SensorGravityDesign.CreateGeometry(this.MaterialBrushes, base.SelectionEmissives,
                GetTransformForGeometry(isFinal),
                WorldColors.SensorBase, WorldColors.SensorBaseSpecular, WorldColors.SensorNetForce, WorldColors.SensorNetForceSpecular,
                isFinal);
        }

        #endregion
    }

    #endregion
    #region Class: SensorNetForce

    public class SensorNetForce
    {
    }

    #endregion
}