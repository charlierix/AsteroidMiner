﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using Game.Newt.v2.GameItems.ShipParts;
using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Controls3D;

namespace Game.Newt.v2.GameItems.ShipEditor
{
    /// <summary>
    /// This is a wrapper to a part that is on the design surface
    /// </summary>
    public class DesignPart
    {
        #region Declaration Section

        private EditorOptions _options = null;

        #endregion

        #region Constructor

        public DesignPart(EditorOptions options)
        {
            _options = options;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// This will be null if the part is for an item that can't be copied, resized
        /// </summary>
        public PartToolItemBase Part2D
        {
            get;
            set;
        }

        private PartDesignBase _part3D = null;
        public PartDesignBase Part3D
        {
            get
            {
                return _part3D;
            }
            set
            {
                if (_part3D != null)
                {
                    _part3D.TransformChanged -= new EventHandler(Part3D_TransformChanged);
                }

                _part3D = value;

                _part3D.TransformChanged += new EventHandler(Part3D_TransformChanged);
            }
        }

        public ModelVisual3D Model
        {
            get;
            set;
        }
        public ScreenSpaceLines3D GuideLines
        {
            get;
            set;
        }

        #endregion

        #region Public Methods

        public DesignPart Clone()
        {
            DesignPart retVal = new DesignPart(_options);
            retVal.Part2D = this.Part2D;		// this shouldn't be cloned, it's just a link to the source

            if (this.Part2D == null)
            {
                retVal.Part3D = BotConstructor.GetPartDesign(this.Part3D.GetDNA(), _options, this.Part3D.IsFinalModel);
            }
            else
            {
                retVal.Part3D = retVal.Part2D.GetNewDesignPart();
            }

            ModelVisual3D model = new ModelVisual3D();
            model.Content = retVal.Part3D.Model;
            retVal.Model = model;

            retVal.Part3D.Position = this.Part3D.Position;
            retVal.Part3D.Orientation = this.Part3D.Orientation;
            retVal.Part3D.Scale = this.Part3D.Scale;

            if (this.GuideLines != null)		// this needs to be created after the position is set
            {
                retVal.CreateGuildLines();
            }

            return retVal;
        }

        public void CreateGuildLines()
        {
            const double SIZE = 50d;

            this.GuideLines = new ScreenSpaceLines3D(true);

            // Get a color based on the part type
            Color color;

            if (this.Part3D is AmmoBoxDesign)
            {
                color = WorldColors.AmmoBox_Color;
            }
            else if (this.Part3D is BeamGunDesign || this.Part3D is GrappleGunDesign || this.Part3D is ProjectileGunDesign)
            {
                color = UtilityWPF.AlphaBlend(Colors.White, WorldColors.GunBarrel_Color, .125d);		// shift it white, because it's too dark
            }
            else if (this.Part3D is BrainDesign || this.Part3D is BrainRGBRecognizerDesign || this.Part3D is DirectionControllerRingDesign || this.Part3D is DirectionControllerSphereDesign)
            {
                color = UtilityWPF.AlphaBlend(Colors.Transparent, WorldColors.Brain_Color, .25d);
            }
            else if (this.Part3D is CargoBayDesign)
            {
                color = WorldColors.CargoBay_Color;
            }
            else if (this.Part3D is ConverterEnergyToAmmoDesign || this.Part3D is ConverterEnergyToFuelDesign || this.Part3D is ConverterEnergyToPlasmaDesign || this.Part3D is ConverterFuelToEnergyDesign || this.Part3D is ConverterMatterToAmmoDesign || this.Part3D is ConverterMatterToEnergyDesign || this.Part3D is ConverterMatterToFuelDesign || this.Part3D is ConverterRadiationToEnergyDesign || this.Part3D is ConverterMatterToPlasmaDesign)
            {
                color = UtilityWPF.AlphaBlend(Colors.White, WorldColors.ConverterBase_Color, .125d);
            }
            else if (this.Part3D is EnergyTankDesign)
            {
                color = WorldColors.EnergyTank_Color;
            }
            else if (this.Part3D is EyeDesign || this.Part3D is CameraColorRGBDesign)
            {
                color = UtilityWPF.AlphaBlend(Colors.White, WorldColors.CameraBase_Color, .15d);
            }
            else if (this.Part3D is SensorCollisionDesign || this.Part3D is SensorFluidDesign || this.Part3D is SensorGravityDesign || this.Part3D is SensorInternalForceDesign || this.Part3D is SensorNetForceDesign || this.Part3D is SensorRadiationDesign || this.Part3D is SensorSpinDesign || this.Part3D is SensorTractorDesign || this.Part3D is SensorVelocityDesign)
            {
                color = UtilityWPF.AlphaBlend(Colors.White, WorldColors.SensorBase_Color, .125d);
            }
            else if (this.Part3D is FuelTankDesign)
            {
                color = WorldColors.FuelTank_Color;
            }
            else if (this.Part3D is HangarBayDesign)
            {
                color = UtilityWPF.AlphaBlend(Colors.Transparent, WorldColors.HangarBay_Color, .25d);
            }
            else if (this.Part3D is SelfRepairDesign)
            {
                color = UtilityWPF.AlphaBlend(Colors.Transparent, WorldColors.SelfRepairBase_Color, .25d);
            }
            else if (this.Part3D is ShieldEnergyDesign || this.Part3D is ShieldKineticDesign || this.Part3D is ShieldTractorDesign)
            {
                color = UtilityWPF.AlphaBlend(Colors.Transparent, WorldColors.ShieldBase_Color, .125d);
            }
            else if (this.Part3D is ThrusterDesign)
            {
                color = WorldColors.Thruster_Color;
            }
            else if (this.Part3D is TractorBeamDesign)
            {
                color = WorldColors.TractorBeamBase_Color;
            }
            else if (this.Part3D is ImpulseEngineDesign)
            {
                color = UtilityWPF.AlphaBlend(Colors.White,
                    UtilityWPF.AlphaBlend(WorldColors.ImpulseEngineGlowball_Color, WorldColors.ImpulseEngine_Color, .25d),
                    .125d);
            }
            else if (this.Part3D is PlasmaTankDesign)
            {
                color = WorldColors.PlasmaTank_Color;
            }
            else if (this.Part3D is SwarmBayDesign)
            {
                color = WorldColors.SwarmBay_Color;
            }
            else
            {
                color = Colors.Black;
            }

            // Tone the color down a bit
            //color = UtilityWPF.AlphaBlend(color, _colors.Background, .9d);
            color = UtilityWPF.AlphaBlend(color, Colors.Transparent, .66d);

            this.GuideLines.Color = color;

            this.GuideLines.Thickness = .5d;
            this.GuideLines.AddLine(new Point3D(-SIZE, 0, 0), new Point3D(SIZE, 0, 0));
            this.GuideLines.AddLine(new Point3D(0, -SIZE, 0), new Point3D(0, SIZE, 0));
            this.GuideLines.AddLine(new Point3D(0, 0, -SIZE), new Point3D(0, 0, SIZE));

            this.GuideLines.Transform = new TranslateTransform3D(this.Part3D.Position.ToVector());
        }

        #endregion

        #region Event Listeners

        private void Part3D_TransformChanged(object sender, EventArgs e)
        {
            if (this.GuideLines != null)
            {
                this.GuideLines.Transform = new TranslateTransform3D(this.Part3D.Position.ToVector());
            }
        }

        #endregion
    }
}
