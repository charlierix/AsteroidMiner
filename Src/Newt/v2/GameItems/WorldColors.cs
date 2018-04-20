using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using Game.HelperClassesCore;
using Game.HelperClassesWPF;

namespace Game.Newt.v2.GameItems
{
    //TODO: Add properties that return diffuse material (cached per thread).  Change out the consumers to use that property - keep the color property, because that is sometimes used for other reasons

    // Armor should be silvers/blacks/whites
    // Internal parts are colored based on function group:
    //		Fuel=Yellow (thruster=dark yellow)
    //		Energy=Blue
    //		Tractor=Purple
    //		Kinetic=Slate/Red
    //		Converters=DarkGreen with DarkBlue reflection
    //		Brain=HotPink
    //
    //		Greens and Browns seem to be misc
    public static class WorldColors
    {
        // ******************** Environment

        public static Color SpaceBackground = UtilityWPF.ColorFromHex("383838");		// this is hardcoded in xaml, but I want it here for reference
        public static Color BoundryLines = UtilityWPF.ColorFromHex("303030");

        #region Destroyed

        [ThreadStatic]
        private static Brush _destroyed_Brush;
        public static Brush Destroyed_Brush
        {
            get
            {
                if (_destroyed_Brush == null)
                {
                    _destroyed_Brush = new SolidColorBrush(UtilityWPF.ColorFromHex("211A16"));
                }

                return _destroyed_Brush;
            }
        }

        [ThreadStatic]
        private static Brush _destroyed_SpecularBrush;
        public static Brush Destroyed_SpecularBrush
        {
            get
            {
                if (_destroyed_SpecularBrush == null)
                {
                    _destroyed_SpecularBrush = new SolidColorBrush(UtilityWPF.ColorFromHex("30CF750E"));
                }

                return _destroyed_SpecularBrush;
            }
        }

        public static double Destroyed_SpecularPower
        {
            get
            {
                return 4;
            }
        }

        #endregion

        #region Star

        public static Color Star_Color
        {
            get
            {
                if (StaticRandom.NextDouble() < .95d)
                {
                    byte color = Convert.ToByte(StaticRandom.Next(192, 256));
                    return Color.FromRgb(color, color, color);
                }
                else
                {
                    return UtilityWPF.GetRandomColor(160, 192);
                }
            }
        }
        public static Color Star_Emissive
        {
            get
            {
                Color color = Color.FromRgb(255, 255, 255);
                if (StaticRandom.NextDouble() > .8d)
                {
                    color = UtilityWPF.GetRandomColor(128, 192);
                }

                return Color.FromArgb(Convert.ToByte(StaticRandom.Next(32, 128)), color.R, color.G, color.B);
            }
        }

        #endregion
        #region Asteroid

        public static Color Asteroid_Color
        {
            get
            {
                //byte rgb = Convert.ToByte(StaticRandom.Next(76, 104));
                byte rgb = Convert.ToByte(StaticRandom.Next(64, 89));

                if (StaticRandom.NextDouble() < .95d)
                {
                    return Color.FromRgb(rgb, rgb, rgb);
                }
                else
                {
                    //return UtilityWPF.GetRandomColor(95, 99);
                    return UtilityWPF.GetRandomColor(75, 78);
                }
            }
        }

        [ThreadStatic]
        private static SpecularMaterial _asteroid_Specular;
        public static SpecularMaterial Asteroid_Specular
        {
            get
            {
                if (_asteroid_Specular == null)
                {
                    _asteroid_Specular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("A0404040")), 5d);
                }

                return _asteroid_Specular;
            }
        }

        #endregion
        #region SpaceStation

        public static Color SpaceStationHull_Color
        {
            get
            {
                return UtilityWPF.AlphaBlend(UtilityWPF.GetRandomColor(108, 148), Colors.Gray, .25);
            }
        }

        [ThreadStatic]
        private static SpecularMaterial _spaceStationHull_Specular;
        public static SpecularMaterial SpaceStationHull_Specular
        {
            get
            {
                if (_spaceStationHull_Specular == null)
                {
                    _spaceStationHull_Specular = new SpecularMaterial(Brushes.Silver, 75d);
                }

                return _spaceStationHull_Specular;
            }
        }

        public static Color SpaceStationGlass_Color
        {
            get
            {
                return Color.FromArgb(25, 220, 240, 240);		// the skin is semitransparent, so you can see the components inside
            }
        }

        [ThreadStatic]
        private static SpecularMaterial _spaceStationGlass_Specular_Front;
        public static SpecularMaterial SpaceStationGlass_Specular_Front
        {
            get
            {
                if (_spaceStationGlass_Specular_Front == null)
                {
                    _spaceStationGlass_Specular_Front = new SpecularMaterial(Brushes.White, 85d);
                }

                return _spaceStationGlass_Specular_Front;
            }
        }

        [ThreadStatic]
        private static SpecularMaterial _spaceStationGlass_Specular_Back;
        public static SpecularMaterial SpaceStationGlass_Specular_Back
        {
            get
            {
                if (_spaceStationGlass_Specular_Back == null)
                {
                    _spaceStationGlass_Specular_Back = new SpecularMaterial(new SolidColorBrush(Color.FromArgb(255, 20, 20, 20)), 10d);
                }

                return _spaceStationGlass_Specular_Back;
            }
        }

        public static Color SpaceStationForceField_Color
        {
            get
            {
                return UtilityWPF.ColorFromHex("#2086E7FF");
            }
        }
        public static Color SpaceStationForceField_Emissive_Front
        {
            get
            {
                return UtilityWPF.ColorFromHex("#0A89BBC7");
            }
        }
        public static Color SpaceStationForceField_Emissive_Back
        {
            get
            {
                return UtilityWPF.ColorFromHex("#0AFFC086");
            }
        }

        #endregion
        #region Egg

        public static Color Egg_Color
        {
            get
            {
                return UtilityWPF.ColorFromHex("E5E4C7");
            }
        }

        [ThreadStatic]
        private static SpecularMaterial _egg_Specular;
        public static SpecularMaterial Egg_Specular
        {
            get
            {
                if (_egg_Specular == null)
                {
                    _egg_Specular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("20B8B69C")), 2d);
                }

                return _egg_Specular;
            }
        }

        #endregion

        // ******************** Parts

        #region CargoBay

        public static Color CargoBay_Color
        {
            get
            {
                return UtilityWPF.ColorFromHex("34543B");
            }
        }

        [ThreadStatic]
        private static SpecularMaterial _cargoBay_Specular;
        public static SpecularMaterial CargoBay_Specular
        {
            get
            {
                if (_cargoBay_Specular == null)
                {
                    _cargoBay_Specular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("5E5448")), 80d);
                }

                return _cargoBay_Specular;
            }
        }

        #endregion
        #region Converters

        public static Color ConverterBase_Color
        {
            get
            {
                return UtilityWPF.ColorFromHex("27403B");
            }
        }
        [ThreadStatic]
        private static SpecularMaterial _converterBase_Specular;
        public static SpecularMaterial ConverterBase_Specular
        {
            get
            {
                if (_converterBase_Specular == null)
                {
                    _converterBase_Specular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("1F1F61")), 70d);
                }

                return _converterBase_Specular;
            }
        }

        public static Color ConverterFuel_Color
        {
            get
            {
                return UtilityWPF.AlphaBlend(FuelTank_Color, ConverterBase_Color, .75d);
            }
        }
        public static SpecularMaterial ConverterFuel_Specular
        {
            get
            {
                return ConverterBase_Specular;
            }
        }

        public static Color ConverterEnergy_Color
        {
            get
            {
                return UtilityWPF.AlphaBlend(EnergyTank_Color, ConverterBase_Color, .75d);
            }
        }
        public static SpecularMaterial ConverterEnergy_Specular
        {
            get
            {
                return ConverterBase_Specular;
            }
        }

        public static Color ConverterPlasma_Color
        {
            get
            {
                return UtilityWPF.AlphaBlend(PlasmaTank_Color, ConverterBase_Color, .75d);
            }
        }
        [ThreadStatic]
        private static SpecularMaterial _converterPlasma_Specular;
        public static SpecularMaterial ConverterPlasma_Specular
        {
            get
            {
                if (_converterPlasma_Specular == null)
                {
                    //Color ammoColor = UtilityWPF.ColorFromHex("D95448");
                    //Color baseColor = UtilityWPF.ColorFromHex("1F1F61");
                    //_converterPlasmaSpecular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(ammoColor, baseColor, .6d)), 70d);

                    _converterPlasma_Specular = PlasmaTank_Specular;
                }

                return _converterPlasma_Specular;
            }
        }

        public static Color ConverterAmmo_Color
        {
            get
            {
                return UtilityWPF.AlphaBlend(AmmoBox_Color, ConverterBase_Color, .75d);
            }
        }
        [ThreadStatic]
        private static SpecularMaterial _converterAmmo_Specular;
        public static SpecularMaterial ConverterAmmo_Specular
        {
            get
            {
                if (_converterAmmo_Specular == null)
                {
                    Color ammoColor = UtilityWPF.ColorFromHex("D95448");
                    Color baseColor = UtilityWPF.ColorFromHex("1F1F61");
                    _converterAmmo_Specular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(ammoColor, baseColor, .6d)), 70d);
                }

                return _converterAmmo_Specular;
            }
        }

        #endregion
        #region Sensors

        public static Color SensorBase_Color
        {
            get
            {
                return UtilityWPF.ColorFromHex("4B4B4B");
            }
        }
        [ThreadStatic]
        private static SpecularMaterial _sensorBase_Specular;
        public static SpecularMaterial SensorBase_Specular
        {
            get
            {
                if (_sensorBase_Specular == null)
                {
                    _sensorBase_Specular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("80808080")), 35);
                }

                return _sensorBase_Specular;
            }
        }

        public static Color SensorGravity_Color
        {
            get
            {
                return UtilityWPF.ColorFromHex("856E5A");
            }
        }
        public static SpecularMaterial SensorGravity_Specular
        {
            get
            {
                return SensorBase_Specular;
            }
        }

        public static Color SensorRadiation_Color
        {
            get
            {
                return UtilityWPF.AlphaBlend(EnergyTank_Color, SensorBase_Color, .75d);
            }
        }
        public static SpecularMaterial SensorRadiation_Specular
        {
            get
            {
                return SensorBase_Specular;
            }
        }

        public static Color SensorTractor_Color
        {
            get
            {
                //NOTE: This color is the same as ShieldTractor
                return UtilityWPF.AlphaBlend(UtilityWPF.ColorFromHex("956CA1"), SensorBase_Color, .75d);
            }
        }
        public static SpecularMaterial SensorTractor_Specular
        {
            get
            {
                return SensorBase_Specular;
            }
        }

        public static Color SensorCollision_Color
        {
            get
            {
                //NOTE: This color is the same as ShieldTractor
                return UtilityWPF.AlphaBlend(UtilityWPF.ColorFromHex("505663"), SensorBase_Color, .75d);
            }
        }
        [ThreadStatic]
        private static SpecularMaterial _sensorCollision_Specular;
        public static SpecularMaterial SensorCollision_Specular
        {
            get
            {
                if (_sensorCollision_Specular == null)
                {
                    _sensorCollision_Specular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("C0D12A2A")), 70d);
                }

                return _sensorCollision_Specular;
            }
        }

        public static Color SensorFluid_Color
        {
            get
            {
                return UtilityWPF.ColorFromHex("58756D");
            }
        }
        public static SpecularMaterial SensorFluid_Specular
        {
            get
            {
                return SensorBase_Specular;
            }
        }

        public static Color SensorSpin_Color
        {
            get
            {
                return UtilityWPF.ColorFromHex("818F27");
            }
        }
        public static SpecularMaterial SensorSpin_Specular
        {
            get
            {
                return SensorBase_Specular;
            }
        }

        public static Color SensorVelocity_Color
        {
            get
            {
                return UtilityWPF.ColorFromHex("A3691D");
            }
        }
        public static SpecularMaterial SensorVelocity_Specular
        {
            get
            {
                return SensorBase_Specular;
            }
        }

        public static Color SensorInternalForce_Color
        {
            get
            {
                return UtilityWPF.ColorFromHex("22272B");
            }
        }
        public static SpecularMaterial SensorInternalForce_Specular
        {
            get
            {
                return SensorBase_Specular;
            }
        }

        public static Color SensorNetForce_Color
        {
            get
            {
                return UtilityWPF.ColorFromHex("B0B4B8");
            }
        }
        public static SpecularMaterial SensorNetForce_Specular
        {
            get
            {
                return SensorBase_Specular;
            }
        }

        #endregion
        #region HangarBay

        public static Color HangarBay_Color
        {
            get
            {
                return UtilityWPF.ColorFromHex("BDA88E");
            }
        }

        [ThreadStatic]
        private static SpecularMaterial _hangarBay_Specular;
        public static SpecularMaterial HangarBay_Specular
        {
            get
            {
                if (_hangarBay_Specular == null)
                {
                    _hangarBay_Specular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("70615649")), 35d);
                }

                return _hangarBay_Specular;
            }
        }

        public static Color HangarBayTrim_Color
        {
            get
            {
                return UtilityWPF.ColorFromHex("968671");
            }
        }

        [ThreadStatic]
        private static SpecularMaterial _hangarBayTrim_Specular;
        public static SpecularMaterial HangarBayTrim_Specular
        {
            get
            {
                if (_hangarBayTrim_Specular == null)
                {
                    _hangarBayTrim_Specular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("704A443D")), 35d);
                }

                return _hangarBayTrim_Specular;
            }
        }

        #endregion
        #region SwarmBay

        public static Color SwarmBay_Color
        {
            get
            {
                return UtilityWPF.ColorFromHex("BDA88E");
            }
        }

        [ThreadStatic]
        private static SpecularMaterial _swarmBay_Specular;
        public static SpecularMaterial SwarmBay_Specular
        {
            get
            {
                if (_swarmBay_Specular == null)
                {
                    _swarmBay_Specular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("70615649")), 35d);
                }

                return _swarmBay_Specular;
            }
        }

        #endregion
        #region AmmoBox

        public static Color AmmoBox_Color
        {
            get
            {
                //return UtilityWPF.ColorFromHex("666E7F");
                return UtilityWPF.ColorFromHex("4B515E");
            }
        }

        [ThreadStatic]
        private static SpecularMaterial _ammoBox_Specular;
        public static SpecularMaterial AmmoBox_Specular
        {
            get
            {
                if (_ammoBox_Specular == null)
                {
                    _ammoBox_Specular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("C0D95448")), 65d);
                }

                return _ammoBox_Specular;
            }
        }

        public static Color AmmoBoxPlate_Color
        {
            get
            {
                return UtilityWPF.ColorFromHex("5E3131");
            }
        }

        [ThreadStatic]
        private static SpecularMaterial _ammoBoxPlate_Specular;
        public static SpecularMaterial AmmoBoxPlate_Specular
        {
            get
            {
                if (_ammoBoxPlate_Specular == null)
                {
                    _ammoBoxPlate_Specular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("C0DE2C2C")), 65d);
                }

                return _ammoBoxPlate_Specular;
            }
        }

        #endregion
        #region Gun

        public static Color GunBase_Color
        {
            get
            {
                return UtilityWPF.ColorFromHex("4F5359");		// flatter dark gray
            }
        }

        [ThreadStatic]
        private static SpecularMaterial _gunBase_Specular;
        public static SpecularMaterial GunBase_Specular
        {
            get
            {
                if (_gunBase_Specular == null)
                {
                    _gunBase_Specular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("546B6F78")), 35d);
                }

                return _gunBase_Specular;
            }
        }

        public static Color GunBarrel_Color
        {
            get
            {
                return UtilityWPF.ColorFromHex("3C424C");		// gunmetal
            }
        }

        [ThreadStatic]
        private static SpecularMaterial _gunBarrel_Specular;
        public static SpecularMaterial GunBarrel_Specular
        {
            get
            {
                if (_gunBarrel_Specular == null)
                {
                    _gunBarrel_Specular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("D023355C")), 75d);
                }

                return _gunBarrel_Specular;
            }
        }

        public static Color GunTrim_Color
        {
            get
            {
                // red would look tacky, use light gray
                //return UtilityWPF.ColorFromHex("4C1A1A");		// dark red
                return UtilityWPF.ColorFromHex("5E6166");
            }
        }

        [ThreadStatic]
        private static SpecularMaterial _gunTrim_Specular;
        public static SpecularMaterial GunTrim_Specular
        {
            get
            {
                if (_gunTrim_Specular == null)
                {
                    _gunTrim_Specular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("80D12A2A")), 50d);
                }

                return _gunTrim_Specular;
            }
        }

        #endregion
        #region Grapple

        public static Color GrapplePad_Color
        {
            get
            {
                return UtilityWPF.ColorFromHex("573A3A");
            }
        }

        [ThreadStatic]
        private static SpecularMaterial _grapplePad_Specular;
        public static SpecularMaterial GrapplePad_Specular
        {
            get
            {
                if (_grapplePad_Specular == null)
                {
                    _grapplePad_Specular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("80BF8E8E")), 25d);
                }

                return _grapplePad_Specular;
            }
        }

        #endregion
        #region BeamGun

        public static Color BeamGunDish_Color
        {
            get
            {
                return UtilityWPF.ColorFromHex("324669");
            }
        }

        [ThreadStatic]
        private static SpecularMaterial _beamGunDish_Specular;
        public static SpecularMaterial BeamGunDish_Specular
        {
            get
            {
                if (_beamGunDish_Specular == null)
                {
                    _beamGunDish_Specular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("A80F45A3")), 40d);
                }

                return _beamGunDish_Specular;
            }
        }

        public static Color BeamGunCrystal_Color
        {
            get
            {
                return UtilityWPF.ColorFromHex("3B5B94");
            }
        }

        [ThreadStatic]
        private static SpecularMaterial _beamGunCrystal_Specular;
        public static SpecularMaterial BeamGunCrystal_Specular
        {
            get
            {
                if (_beamGunCrystal_Specular == null)
                {
                    _beamGunCrystal_Specular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("FF0F45A3")), 90d);
                }

                return _beamGunCrystal_Specular;
            }
        }

        [ThreadStatic]
        private static EmissiveMaterial _beamGunCrystal_Emissive;
        public static EmissiveMaterial BeamGunCrystal_Emissive
        {
            get
            {
                if (_beamGunCrystal_Emissive == null)
                {
                    _beamGunCrystal_Emissive = new EmissiveMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("30719BE3")));
                }

                return _beamGunCrystal_Emissive;
            }
        }

        public static Color BeamGunTrim_Color
        {
            get
            {
                return UtilityWPF.ColorFromHex("5E6166");
            }
        }

        [ThreadStatic]
        private static SpecularMaterial _beamGunTrim_Specular;
        public static SpecularMaterial BeamGunTrim_Specular
        {
            get
            {
                if (_beamGunTrim_Specular == null)
                {
                    _beamGunTrim_Specular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("802A67D1")), 50d);
                }

                return _beamGunTrim_Specular;
            }
        }

        #endregion
        #region FuelTank

        public static Color FuelTank_Color
        {
            get
            {
                //return UtilityWPF.ColorFromHex("D49820");
                return UtilityWPF.ColorFromHex("A38521");
            }
        }

        [ThreadStatic]
        private static SpecularMaterial _fuelTank_Specular;
        public static SpecularMaterial FuelTank_Specular
        {
            get
            {
                if (_fuelTank_Specular == null)
                {
                    _fuelTank_Specular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("80659C9E")), 40d);
                }

                return _fuelTank_Specular;
            }
        }

        #endregion
        #region EnergyTank

        public static Color EnergyTank_Color
        {
            get
            {
                return UtilityWPF.ColorFromHex("507BC7");
            }
        }

        [ThreadStatic]
        private static SpecularMaterial _energyTank_Specular;
        public static SpecularMaterial EnergyTank_Specular
        {
            get
            {
                if (_energyTank_Specular == null)
                {
                    _energyTank_Specular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("D0551FB8")), 80d);
                }

                return _energyTank_Specular;
            }
        }

        [ThreadStatic]
        private static EmissiveMaterial _energyTank_Emissive;
        public static EmissiveMaterial EnergyTank_Emissive
        {
            get
            {
                if (_energyTank_Emissive == null)
                {
                    _energyTank_Emissive = new EmissiveMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("203348D4")));
                }

                return _energyTank_Emissive;
            }
        }

        #endregion
        #region PlasmaTank

        public static Color PlasmaTank_Color
        {
            get
            {
                return UtilityWPF.ColorFromHex("6A66D9");
            }
        }

        [ThreadStatic]
        private static SpecularMaterial _plasmaTank_Specular;
        public static SpecularMaterial PlasmaTank_Specular
        {
            get
            {
                if (_plasmaTank_Specular == null)
                {
                    _plasmaTank_Specular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("DE6491")), 90d);
                }

                return _plasmaTank_Specular;
            }
        }

        #endregion
        #region Thruster

        public static Color Thruster_Color
        {
            get
            {
                return UtilityWPF.ColorFromHex("754F42");
            }
        }

        [ThreadStatic]
        private static SpecularMaterial _thruster_Specular;
        public static SpecularMaterial Thruster_Specular
        {
            get
            {
                if (_thruster_Specular == null)
                {
                    _thruster_Specular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("906B7D5A")), 20d);
                }

                return _thruster_Specular;
            }
        }

        public static Color ThrusterBack_Color
        {
            get
            {
                return UtilityWPF.ColorFromHex("4F403A");
            }
        }

        #endregion
        #region TractorBeam

        public static Color TractorBeamBase_Color
        {
            get
            {
                //return UtilityWPF.ColorFromHex("6599A3");
                return UtilityWPF.ColorFromHex("6F8185");
            }
        }

        [ThreadStatic]
        private static SpecularMaterial _tractorBeamBase_Specular;
        public static SpecularMaterial TractorBeamBase_Specular
        {
            get
            {
                if (_tractorBeamBase_Specular == null)
                {
                    _tractorBeamBase_Specular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("FF6599A3")), 60);
                }

                return _tractorBeamBase_Specular;
            }
        }

        public static Color TractorBeamRod_Color
        {
            get
            {
                //return UtilityWPF.ColorFromHex("8F7978");
                return UtilityWPF.ColorFromHex("8A788F");
            }
        }

        [ThreadStatic]
        private static SpecularMaterial _tractorBeamRod_Specular;
        public static SpecularMaterial TractorBeamRod_Specular
        {
            get
            {
                if (_tractorBeamRod_Specular == null)
                {
                    _tractorBeamRod_Specular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("8A30A1")), 100);
                }

                return _tractorBeamRod_Specular;
            }
        }

        [ThreadStatic]
        private static EmissiveMaterial _tractorBeamRod_Emissive;
        public static EmissiveMaterial TractorBeamRod_Emissive
        {
            get
            {
                if (_tractorBeamRod_Emissive == null)
                {
                    _tractorBeamRod_Emissive = new EmissiveMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("40AE75BA")));
                }

                return _tractorBeamRod_Emissive;
            }
        }

        #endregion
        #region ImpulseEngine

        public static Color ImpulseEngine_Color
        {
            get
            {
                return UtilityWPF.ColorFromHex("2B271D");
            }
        }

        [ThreadStatic]
        private static SpecularMaterial _impulseEngine_Specular;
        public static SpecularMaterial ImpulseEngine_Specular
        {
            get
            {
                if (_impulseEngine_Specular == null)
                {
                    _impulseEngine_Specular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("885B74AB")), 25);
                }

                return _impulseEngine_Specular;
            }
        }

        public static Color ImpulseEngineGlowball_Color
        {
            get
            {
                return UtilityWPF.ColorFromHex("AB49F2");
            }
        }

        [ThreadStatic]
        private static SpecularMaterial _impulseEngineGlowball_Specular;
        public static SpecularMaterial ImpulseEngineGlowball_Specular
        {
            get
            {
                if (_impulseEngineGlowball_Specular == null)
                {
                    _impulseEngineGlowball_Specular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("30C94AFF")), 3);
                }

                return _impulseEngineGlowball_Specular;
            }
        }

        [ThreadStatic]
        private static EmissiveMaterial _impulseEngineGlowball_Emissive;
        public static EmissiveMaterial ImpulseEngineGlowball_Emissive
        {
            get
            {
                if (_impulseEngineGlowball_Emissive == null)
                {
                    _impulseEngineGlowball_Emissive = new EmissiveMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("504F2B69")));
                }

                return _impulseEngineGlowball_Emissive;
            }
        }

        #endregion
        #region Camera

        public static Color CameraBase_Color
        {
            get
            {
                return Color.FromRgb(75, 75, 75);
            }
        }

        [ThreadStatic]
        private static SpecularMaterial _cameraBase_Specular;
        public static SpecularMaterial CameraBase_Specular
        {
            get
            {
                if (_cameraBase_Specular == null)
                {
                    _cameraBase_Specular = new SpecularMaterial(new SolidColorBrush(Color.FromArgb(128, 128, 128, 128)), 35d);
                }

                return _cameraBase_Specular;
            }
        }

        public static Color CameraLens_Color
        {
            get
            {
                //return UtilityWPF.ColorFromHex("3D1B16");
                return UtilityWPF.ColorFromHex("49211B");
            }
        }

        [ThreadStatic]
        private static SpecularMaterial _cameraLens_Specular;
        public static SpecularMaterial CameraLens_Specular
        {
            get
            {
                if (_cameraLens_Specular == null)
                {
                    _cameraLens_Specular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("80A00000")), 75d);
                }

                return _cameraLens_Specular;
            }
        }

        public static Color CameraHardCodedLens_Color
        {
            get
            {
                return UtilityWPF.AlphaBlend(Brain_Color, CameraLens_Color, .75);
            }
        }

        #endregion
        #region Brain

        public static Color Brain_Color
        {
            get
            {
                return UtilityWPF.ColorFromHex("FFE32078");
            }
        }

        [ThreadStatic]
        private static SpecularMaterial _brain_Specular;
        public static SpecularMaterial Brain_Specular
        {
            get
            {
                if (_brain_Specular == null)
                {
                    _brain_Specular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("A0ED8EB9")), 35d);
                }

                return _brain_Specular;
            }
        }

        public static Color BrainInsideStrand_Color
        {
            get
            {
                //return UtilityWPF.ColorFromHex("E597BB");
                Color color1 = UtilityWPF.ColorFromHex("E58EB7");
                Color color2 = UtilityWPF.ColorFromHex("E5AEC8");
                return UtilityWPF.AlphaBlend(color1, color2, StaticRandom.NextDouble());
            }
        }

        [ThreadStatic]
        private static SpecularMaterial _brainInsideStrand_Specular;
        public static SpecularMaterial BrainInsideStrand_Specular
        {
            get
            {
                if (_brainInsideStrand_Specular == null)
                {
                    _brainInsideStrand_Specular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("FF0073")), 100d);
                }

                return _brainInsideStrand_Specular;
            }
        }

        #endregion
        #region DirectionController

        public static Color DirectionControllerRing_Color
        {
            get
            {
                return UtilityWPF.AlphaBlend(WorldColors.Brain_Color, WorldColors.SensorBase_Color, .15);
            }
        }

        [ThreadStatic]
        private static SpecularMaterial _directionControllerRing_Specular;
        public static SpecularMaterial DirectionControllerRing_Specular
        {
            get
            {
                if (_directionControllerRing_Specular == null)
                {
                    _directionControllerRing_Specular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("30FFFFFF")), 18d);
                }

                return _directionControllerRing_Specular;
            }
        }

        #endregion
        #region Shields

        public static Color ShieldBase_Color
        {
            get
            {
                return UtilityWPF.ColorFromHex("1D8F8D");
            }
        }

        [ThreadStatic]
        private static SpecularMaterial _shieldBase_Specular;
        public static SpecularMaterial ShieldBase_Specular
        {
            get
            {
                if (_shieldBase_Specular == null)
                {
                    _shieldBase_Specular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("503FB2")), 100d);
                }

                return _shieldBase_Specular;
            }
        }

        [ThreadStatic]
        private static EmissiveMaterial _shieldBase_Emissive;
        public static EmissiveMaterial ShieldBase_Emissive
        {
            get
            {
                if (_shieldBase_Emissive == null)
                {
                    _shieldBase_Emissive = new EmissiveMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("205FD4CE")));
                }

                return _shieldBase_Emissive;
            }
        }

        public static Color ShieldEnergy_Color
        {
            get
            {
                return UtilityWPF.AlphaBlend(EnergyTank_Color, ShieldBase_Color, .65d);
            }
        }
        public static SpecularMaterial ShieldEnergy_Specular
        {
            get
            {
                return ShieldBase_Specular;
            }
        }

        public static Color ShieldKinetic_Color
        {
            get
            {
                return UtilityWPF.AlphaBlend(UtilityWPF.ColorFromHex("505663"), ShieldBase_Color, .9d);
            }
        }

        [ThreadStatic]
        private static SpecularMaterial _shieldKinetic_Specular;
        public static SpecularMaterial ShieldKinetic_Specular
        {
            get
            {
                if (_shieldKinetic_Specular == null)
                {
                    _shieldKinetic_Specular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("C0A63333")), 70d);
                }

                return _shieldKinetic_Specular;
            }
        }

        public static Color ShieldTractor_Color
        {
            get
            {
                //NOTE: This color is the same as SensorTractor
                return UtilityWPF.AlphaBlend(UtilityWPF.ColorFromHex("956CA1"), ShieldBase_Color, .9d);
            }
        }

        [ThreadStatic]
        private static SpecularMaterial _shieldTractor_Specular;
        public static SpecularMaterial ShieldTractor_Specular
        {
            get
            {
                if (_shieldTractor_Specular == null)
                {
                    _shieldTractor_Specular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("C0AE75BA")), 50d);
                }

                return _shieldTractor_Specular;
            }
        }

        #endregion
        #region SelfRepair

        public static Color SelfRepairBase_Color
        {
            get
            {
                return UtilityWPF.ColorFromHex("E8E0C1");
            }
        }

        [ThreadStatic]
        private static SpecularMaterial _selfRepairBase_Specular;
        public static SpecularMaterial SelfRepairBase_Specular
        {
            get
            {
                if (_selfRepairBase_Specular == null)
                {
                    _selfRepairBase_Specular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("C0707691")), 60d);
                }

                return _selfRepairBase_Specular;
            }
        }

        public static Color SelfRepairCross_Color
        {
            get
            {
                return UtilityWPF.ColorFromHex("43B23B");
            }
        }

        [ThreadStatic]
        private static SpecularMaterial _selfRepairCross_Specular;
        public static SpecularMaterial SelfRepairCross_Specular
        {
            get
            {
                if (_selfRepairCross_Specular == null)
                {
                    _selfRepairCross_Specular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("203DB2")), 85d);
                }

                return _selfRepairCross_Specular;
            }
        }

        #endregion

        // Light armor should be whitish (plastic)
        // Heavy armor should be silverish (metal)
    }
}
