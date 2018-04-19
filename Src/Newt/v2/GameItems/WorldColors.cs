using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

        private static ThreadLocal<Brush> _destroyed_Brush = new ThreadLocal<Brush>(() => new SolidColorBrush(UtilityWPF.ColorFromHex("211A16")));
        public static Brush Destroyed_Brush => _destroyed_Brush.Value;

        private static ThreadLocal<Brush> _destroyed_SpecularBrush = new ThreadLocal<Brush>(() => new SolidColorBrush(UtilityWPF.ColorFromHex("30CF750E")));
        public static Brush Destroyed_SpecularBrush => _destroyed_SpecularBrush.Value;

        public static double Destroyed_SpecularPower => 4;

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

        private static ThreadLocal<SpecularMaterial> _asteroid_Specular = new ThreadLocal<SpecularMaterial>(() => new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("A0404040")), 5d));
        public static SpecularMaterial Asteroid_Specular => _asteroid_Specular.Value;

        #endregion
        #region SpaceStation

        public static Color SpaceStationHull_Color => UtilityWPF.AlphaBlend(UtilityWPF.GetRandomColor(108, 148), Colors.Gray, .25);

        private static ThreadLocal<SpecularMaterial> _spaceStationHull_Specular = new ThreadLocal<SpecularMaterial>(() => new SpecularMaterial(Brushes.Silver, 75d));
        public static SpecularMaterial SpaceStationHull_Specular => _spaceStationHull_Specular.Value;

        public static Color SpaceStationGlass_Color => Color.FromArgb(25, 220, 240, 240);       // the skin is semitransparent, so you can see the components inside

        private static ThreadLocal<SpecularMaterial> _spaceStationGlass_Specular_Front = new ThreadLocal<SpecularMaterial>(() => new SpecularMaterial(Brushes.White, 85d));
        public static SpecularMaterial SpaceStationGlass_Specular_Front => _spaceStationGlass_Specular_Front.Value;

        private static ThreadLocal<SpecularMaterial> _spaceStationGlass_Specular_Back = new ThreadLocal<SpecularMaterial>(() => new SpecularMaterial(new SolidColorBrush(Color.FromArgb(255, 20, 20, 20)), 10d));
        public static SpecularMaterial SpaceStationGlass_Specular_Back => _spaceStationGlass_Specular_Back.Value;

        public static Color SpaceStationForceField_Color => UtilityWPF.ColorFromHex("#2086E7FF");
        public static Color SpaceStationForceField_Emissive_Front => UtilityWPF.ColorFromHex("#0A89BBC7");
        public static Color SpaceStationForceField_Emissive_Back => UtilityWPF.ColorFromHex("#0AFFC086");

        #endregion
        #region Egg

        public static Color Egg_Color => UtilityWPF.ColorFromHex("E5E4C7");

        private static ThreadLocal<SpecularMaterial> _egg_Specular = new ThreadLocal<SpecularMaterial>(() => new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("20B8B69C")), 2d));
        public static SpecularMaterial Egg_Specular => _egg_Specular.Value;

        #endregion

        // ******************** Parts

        #region CargoBay

        public static Color CargoBay_Color => UtilityWPF.ColorFromHex("34543B");

        private static ThreadLocal<SpecularMaterial> _cargoBay_Specular = new ThreadLocal<SpecularMaterial>(() => new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("5E5448")), 80d));
        public static SpecularMaterial CargoBay_Specular => _cargoBay_Specular.Value;

        #endregion
        #region Converters

        public static Color ConverterBase_Color => UtilityWPF.ColorFromHex("27403B");
        private static ThreadLocal<SpecularMaterial> _converterBase_Specular = new ThreadLocal<SpecularMaterial>(() => new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("1F1F61")), 70d));
        public static SpecularMaterial ConverterBase_Specular => _converterBase_Specular.Value;

        public static Color ConverterFuel_Color => UtilityWPF.AlphaBlend(FuelTank_Color, ConverterBase_Color, .75d);
        public static SpecularMaterial ConverterFuel_Specular => ConverterBase_Specular;

        public static Color ConverterEnergy_Color => UtilityWPF.AlphaBlend(EnergyTank_Color, ConverterBase_Color, .75d);
        public static SpecularMaterial ConverterEnergy_Specular => ConverterBase_Specular;

        public static Color ConverterPlasma_Color => UtilityWPF.AlphaBlend(PlasmaTank_Color, ConverterBase_Color, .75d);
        private static ThreadLocal<SpecularMaterial> _converterPlasma_Specular = new ThreadLocal<SpecularMaterial>(() => PlasmaTank_Specular);
        public static SpecularMaterial ConverterPlasma_Specular => _converterPlasma_Specular.Value;

        public static Color ConverterAmmo_Color => UtilityWPF.AlphaBlend(AmmoBox_Color, ConverterBase_Color, .75d);

        private static ThreadLocal<SpecularMaterial> _converterAmmo_Specular = new ThreadLocal<SpecularMaterial>(() =>
        {
            Color ammoColor = UtilityWPF.ColorFromHex("D95448");
            Color baseColor = UtilityWPF.ColorFromHex("1F1F61");
            return new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(ammoColor, baseColor, .6d)), 70d);
        });
        public static SpecularMaterial ConverterAmmo_Specular => _converterAmmo_Specular.Value;

        #endregion
        #region Sensors

        public static Color SensorBase_Color => UtilityWPF.ColorFromHex("4B4B4B");
        private static ThreadLocal<SpecularMaterial> _sensorBase_Specular = new ThreadLocal<SpecularMaterial>(() => new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("80808080")), 35));
        public static SpecularMaterial SensorBase_Specular => _sensorBase_Specular.Value;

        public static Color SensorGravity_Color => UtilityWPF.ColorFromHex("856E5A");
        public static SpecularMaterial SensorGravity_Specular => SensorBase_Specular;

        public static Color SensorRadiation_Color => UtilityWPF.AlphaBlend(EnergyTank_Color, SensorBase_Color, .75d);
        public static SpecularMaterial SensorRadiation_Specular => SensorBase_Specular;

        //NOTE: This color is the same as ShieldTractor
        public static Color SensorTractor_Color => UtilityWPF.AlphaBlend(UtilityWPF.ColorFromHex("956CA1"), SensorBase_Color, .75d);
        public static SpecularMaterial SensorTractor_Specular => SensorBase_Specular;

        //NOTE: This color is the same as ShieldKinetic
        public static Color SensorCollision_Color => UtilityWPF.AlphaBlend(UtilityWPF.ColorFromHex("505663"), SensorBase_Color, .75d);
        private static ThreadLocal<SpecularMaterial> _sensorCollision_Specular = new ThreadLocal<SpecularMaterial>(() => new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("C0D12A2A")), 70d));
        public static SpecularMaterial SensorCollision_Specular => _sensorCollision_Specular.Value;

        public static Color SensorFluid_Color => UtilityWPF.ColorFromHex("58756D");
        public static SpecularMaterial SensorFluid_Specular => SensorBase_Specular;

        public static Color SensorSpin_Color => UtilityWPF.ColorFromHex("818F27");
        public static SpecularMaterial SensorSpin_Specular => SensorBase_Specular;

        public static Color SensorVelocity_Color => UtilityWPF.ColorFromHex("A3691D");
        public static SpecularMaterial SensorVelocity_Specular => SensorBase_Specular;

        public static Color SensorInternalForce_Color => UtilityWPF.ColorFromHex("22272B");
        public static SpecularMaterial SensorInternalForce_Specular => SensorBase_Specular;

        public static Color SensorNetForce_Color => UtilityWPF.ColorFromHex("B0B4B8");
        public static SpecularMaterial SensorNetForce_Specular => SensorBase_Specular;

        public static Color SensorHoming_Color => UtilityWPF.ColorFromHex("2531CF");
        private static ThreadLocal<SpecularMaterial> _sensorHoming_Specular = new ThreadLocal<SpecularMaterial>(() => new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("80D1680D")), 5d));
        public static SpecularMaterial SensorHoming_Specular => _sensorHoming_Specular.Value;

        #endregion
        #region HangarBay

        public static Color HangarBay_Color => UtilityWPF.ColorFromHex("BDA88E");

        private static ThreadLocal<SpecularMaterial> _hangarBay_Specular = new ThreadLocal<SpecularMaterial>(() => new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("70615649")), 35d));
        public static SpecularMaterial HangarBay_Specular => _hangarBay_Specular.Value;

        public static Color HangarBayTrim_Color => UtilityWPF.ColorFromHex("968671");

        private static ThreadLocal<SpecularMaterial> _hangarBayTrim_Specular = new ThreadLocal<SpecularMaterial>(() => new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("704A443D")), 35d));
        public static SpecularMaterial HangarBayTrim_Specular => _hangarBayTrim_Specular.Value;

        #endregion
        #region SwarmBay

        public static Color SwarmBay_Color => UtilityWPF.ColorFromHex("BDA88E");

        private static ThreadLocal<SpecularMaterial> _swarmBay_Specular = new ThreadLocal<SpecularMaterial>(() => new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("70615649")), 35d));
        public static SpecularMaterial SwarmBay_Specular => _swarmBay_Specular.Value;

        #endregion
        #region AmmoBox

        public static Color AmmoBox_Color => UtilityWPF.ColorFromHex("4B515E");     //UtilityWPF.ColorFromHex("666E7F");

        private static ThreadLocal<SpecularMaterial> _ammoBox_Specular = new ThreadLocal<SpecularMaterial>(() => new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("C0D95448")), 65d));
        public static SpecularMaterial AmmoBox_Specular => _ammoBox_Specular.Value;

        public static Color AmmoBoxPlate_Color => UtilityWPF.ColorFromHex("5E3131");

        private static ThreadLocal<SpecularMaterial> _ammoBoxPlate_Specular = new ThreadLocal<SpecularMaterial>(() => new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("C0DE2C2C")), 65d));
        public static SpecularMaterial AmmoBoxPlate_Specular => _ammoBoxPlate_Specular.Value;

        #endregion
        #region Gun

        public static Color GunBase_Color => UtilityWPF.ColorFromHex("4F5359");     // flatter dark gray

        private static ThreadLocal<SpecularMaterial> _gunBase_Specular = new ThreadLocal<SpecularMaterial>(() => new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("546B6F78")), 35d));
        public static SpecularMaterial GunBase_Specular => _gunBase_Specular.Value;

        public static Color GunBarrel_Color => UtilityWPF.ColorFromHex("3C424C");       // gunmetal

        private static ThreadLocal<SpecularMaterial> _gunBarrel_Specular = new ThreadLocal<SpecularMaterial>(() => new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("D023355C")), 75d));
        public static SpecularMaterial GunBarrel_Specular => _gunBarrel_Specular.Value;

        public static Color GunTrim_Color => UtilityWPF.ColorFromHex("5E6166");     //UtilityWPF.ColorFromHex("4C1A1A");		// dark red     (red would look tacky, use light gray)

        private static ThreadLocal<SpecularMaterial> _gunTrim_Specular = new ThreadLocal<SpecularMaterial>(() => new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("80D12A2A")), 50d));
        public static SpecularMaterial GunTrim_Specular => _gunTrim_Specular.Value;

        #endregion
        #region Grapple

        public static Color GrapplePad_Color => UtilityWPF.ColorFromHex("573A3A");

        private static ThreadLocal<SpecularMaterial> _grapplePad_Specular = new ThreadLocal<SpecularMaterial>(() => new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("80BF8E8E")), 25d));
        public static SpecularMaterial GrapplePad_Specular => _grapplePad_Specular.Value;

        #endregion
        #region BeamGun

        public static Color BeamGunDish_Color => UtilityWPF.ColorFromHex("324669");

        private static ThreadLocal<SpecularMaterial> _beamGunDish_Specular = new ThreadLocal<SpecularMaterial>(() => new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("A80F45A3")), 40d));
        public static SpecularMaterial BeamGunDish_Specular => _beamGunDish_Specular.Value;

        public static Color BeamGunCrystal_Color => UtilityWPF.ColorFromHex("3B5B94");

        private static ThreadLocal<SpecularMaterial> _beamGunCrystal_Specular = new ThreadLocal<SpecularMaterial>(() => new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("FF0F45A3")), 90d));
        public static SpecularMaterial BeamGunCrystal_Specular => _beamGunCrystal_Specular.Value;

        private static ThreadLocal<EmissiveMaterial> _beamGunCrystal_Emissive = new ThreadLocal<EmissiveMaterial>(() => new EmissiveMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("30719BE3"))));
        public static EmissiveMaterial BeamGunCrystal_Emissive => _beamGunCrystal_Emissive.Value;

        public static Color BeamGunTrim_Color => UtilityWPF.ColorFromHex("5E6166");

        private static ThreadLocal<SpecularMaterial> _beamGunTrim_Specular = new ThreadLocal<SpecularMaterial>(() => new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("802A67D1")), 50d));
        public static SpecularMaterial BeamGunTrim_Specular => _beamGunTrim_Specular.Value;

        #endregion
        #region FuelTank

        public static Color FuelTank_Color => UtilityWPF.ColorFromHex("A38521");

        private static ThreadLocal<SpecularMaterial> _fuelTank_Specular = new ThreadLocal<SpecularMaterial>(() => new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("80659C9E")), 40d));
        public static SpecularMaterial FuelTank_Specular => _fuelTank_Specular.Value;

        #endregion
        #region EnergyTank

        public static Color EnergyTank_Color => UtilityWPF.ColorFromHex("507BC7");

        private static ThreadLocal<SpecularMaterial> _energyTank_Specular = new ThreadLocal<SpecularMaterial>(() => new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("D0551FB8")), 80d));
        public static SpecularMaterial EnergyTank_Specular => _energyTank_Specular.Value;

        private static ThreadLocal<EmissiveMaterial> _energyTank_Emissive = new ThreadLocal<EmissiveMaterial>(() => new EmissiveMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("203348D4"))));
        public static EmissiveMaterial EnergyTank_Emissive => _energyTank_Emissive.Value;

        #endregion
        #region PlasmaTank

        public static Color PlasmaTank_Color => UtilityWPF.ColorFromHex("6A66D9");

        private static ThreadLocal<SpecularMaterial> _plasmaTank_Specular = new ThreadLocal<SpecularMaterial>(() => new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("DE6491")), 90d));
        public static SpecularMaterial PlasmaTank_Specular => _plasmaTank_Specular.Value;

        #endregion
        #region Thruster

        public static Color Thruster_Color => UtilityWPF.ColorFromHex("754F42");

        private static ThreadLocal<SpecularMaterial> _thruster_Specular = new ThreadLocal<SpecularMaterial>(() => new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("906B7D5A")), 20d));
        public static SpecularMaterial Thruster_Specular => _thruster_Specular.Value;

        public static Color ThrusterBack_Color => UtilityWPF.ColorFromHex("4F403A");

        #endregion
        #region TractorBeam

        public static Color TractorBeamBase_Color => UtilityWPF.ColorFromHex("6F8185");

        private static ThreadLocal<SpecularMaterial> _tractorBeamBase_Specular = new ThreadLocal<SpecularMaterial>(() => new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("FF6599A3")), 60));
        public static SpecularMaterial TractorBeamBase_Specular => _tractorBeamBase_Specular.Value;

        public static Color TractorBeamRod_Color => UtilityWPF.ColorFromHex("8A788F");

        private static ThreadLocal<SpecularMaterial> _tractorBeamRod_Specular = new ThreadLocal<SpecularMaterial>(() => new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("8A30A1")), 100));
        public static SpecularMaterial TractorBeamRod_Specular => _tractorBeamRod_Specular.Value;

        private static ThreadLocal<EmissiveMaterial> _tractorBeamRod_Emissive = new ThreadLocal<EmissiveMaterial>(() => new EmissiveMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("40AE75BA"))));
        public static EmissiveMaterial TractorBeamRod_Emissive => _tractorBeamRod_Emissive.Value;

        #endregion
        #region ImpulseEngine

        public static Color ImpulseEngine_Color => UtilityWPF.ColorFromHex("2B271D");

        private static ThreadLocal<SpecularMaterial> _impulseEngine_Specular = new ThreadLocal<SpecularMaterial>(() => new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("885B74AB")), 25));
        public static SpecularMaterial ImpulseEngine_Specular => _impulseEngine_Specular.Value;

        public static Color ImpulseEngineGlowball_Color => UtilityWPF.ColorFromHex("AB49F2");

        private static ThreadLocal<SpecularMaterial> _impulseEngineGlowball_Specular = new ThreadLocal<SpecularMaterial>(() => new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("30C94AFF")), 3));
        public static SpecularMaterial ImpulseEngineGlowball_Specular => _impulseEngineGlowball_Specular.Value;

        private static ThreadLocal<EmissiveMaterial> _impulseEngineGlowball_Emissive = new ThreadLocal<EmissiveMaterial>(() => new EmissiveMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("504F2B69"))));
        public static EmissiveMaterial ImpulseEngineGlowball_Emissive => _impulseEngineGlowball_Emissive.Value;

        public static Color ImpulseEngine_Icon_Color => UtilityWPF.ColorFromHex("5B74AB");

        private static ThreadLocal<SpecularMaterial> _impulseEngine_Icon_Specular = new ThreadLocal<SpecularMaterial>(() => new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("404C4533")), 10));
        public static SpecularMaterial ImpulseEngine_Icon_Specular => _impulseEngine_Icon_Specular.Value;

        private static ThreadLocal<EmissiveMaterial> _impulseEngine_Icon_Emissive = new ThreadLocal<EmissiveMaterial>(() => new EmissiveMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("24495D8A"))));
        public static EmissiveMaterial ImpulseEngine_Icon_Emissive => _impulseEngine_Icon_Emissive.Value;

        #endregion
        #region Camera

        public static Color CameraBase_Color => Color.FromRgb(75, 75, 75);

        private static ThreadLocal<SpecularMaterial> _cameraBase_Specular = new ThreadLocal<SpecularMaterial>(() => new SpecularMaterial(new SolidColorBrush(Color.FromArgb(128, 128, 128, 128)), 35d));
        public static SpecularMaterial CameraBase_Specular => _cameraBase_Specular.Value;

        public static Color CameraLens_Color => UtilityWPF.ColorFromHex("49211B");

        private static ThreadLocal<SpecularMaterial> _cameraLens_Specular = new ThreadLocal<SpecularMaterial>(() => new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("80A00000")), 75d));
        public static SpecularMaterial CameraLens_Specular => _cameraLens_Specular.Value;

        public static Color CameraHardCodedLens_Color => UtilityWPF.AlphaBlend(Brain_Color, CameraLens_Color, .75);

        #endregion
        #region Brain

        public static Color Brain_Color => UtilityWPF.ColorFromHex("FFE32078");

        private static ThreadLocal<SpecularMaterial> _brain_Specular = new ThreadLocal<SpecularMaterial>(() => new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("A0ED8EB9")), 35d));
        public static SpecularMaterial Brain_Specular => _brain_Specular.Value;

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

        private static ThreadLocal<SpecularMaterial> _brainInsideStrand_Specular = new ThreadLocal<SpecularMaterial>(() => new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("FF0073")), 100d));
        public static SpecularMaterial BrainInsideStrand_Specular => _brainInsideStrand_Specular.Value;

        #endregion
        #region DirectionController

        public static Color DirectionControllerRing_Color => UtilityWPF.AlphaBlend(WorldColors.Brain_Color, WorldColors.SensorBase_Color, .15);

        private static ThreadLocal<SpecularMaterial> _directionControllerRing_Specular = new ThreadLocal<SpecularMaterial>(() => new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("30FFFFFF")), 18d));
        public static SpecularMaterial DirectionControllerRing_Specular => _directionControllerRing_Specular.Value;

        #endregion
        #region Shields

        public static Color ShieldBase_Color => UtilityWPF.ColorFromHex("1D8F8D");

        private static ThreadLocal<SpecularMaterial> _shieldBase_Specular = new ThreadLocal<SpecularMaterial>(() => new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("503FB2")), 100d));
        public static SpecularMaterial ShieldBase_Specular => _shieldBase_Specular.Value;

        private static ThreadLocal<EmissiveMaterial> _shieldBase_Emissive = new ThreadLocal<EmissiveMaterial>(() => new EmissiveMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("205FD4CE"))));
        public static EmissiveMaterial ShieldBase_Emissive => _shieldBase_Emissive.Value;

        public static Color ShieldEnergy_Color => UtilityWPF.AlphaBlend(EnergyTank_Color, ShieldBase_Color, .65d);
        public static SpecularMaterial ShieldEnergy_Specular => ShieldBase_Specular;

        public static Color ShieldKinetic_Color => UtilityWPF.AlphaBlend(UtilityWPF.ColorFromHex("505663"), ShieldBase_Color, .9d);

        private static ThreadLocal<SpecularMaterial> _shieldKinetic_Specular = new ThreadLocal<SpecularMaterial>(() => new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("C0A63333")), 70d));
        public static SpecularMaterial ShieldKinetic_Specular => _shieldKinetic_Specular.Value;

        //NOTE: This color is the same as SensorTractor
        public static Color ShieldTractor_Color => UtilityWPF.AlphaBlend(UtilityWPF.ColorFromHex("956CA1"), ShieldBase_Color, .9d);

        private static ThreadLocal<SpecularMaterial> _shieldTractor_Specular = new ThreadLocal<SpecularMaterial>(() => new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("C0AE75BA")), 50d));
        public static SpecularMaterial ShieldTractor_Specular => _shieldTractor_Specular.Value;

        #endregion
        #region SelfRepair

        public static Color SelfRepairBase_Color => UtilityWPF.ColorFromHex("E8E0C1");

        private static ThreadLocal<SpecularMaterial> _selfRepairBase_Specular = new ThreadLocal<SpecularMaterial>(() => new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("C0707691")), 60d));
        public static SpecularMaterial SelfRepairBase_Specular => _selfRepairBase_Specular.Value;

        public static Color SelfRepairCross_Color => UtilityWPF.ColorFromHex("43B23B");

        private static ThreadLocal<SpecularMaterial> _selfRepairCross_Specular = new ThreadLocal<SpecularMaterial>(() => new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("203DB2")), 85d));
        public static SpecularMaterial SelfRepairCross_Specular => _selfRepairCross_Specular.Value;

        #endregion

        // Light armor should be whitish (plastic)
        // Heavy armor should be silverish (metal)
    }
}
