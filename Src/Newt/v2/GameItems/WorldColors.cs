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

        // Star
        public static Color StarColor
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
        public static Color StarEmissive
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

        // Asteroid
        public static Color AsteroidColor
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
        private static SpecularMaterial _asteroidSpecular;
        public static SpecularMaterial AsteroidSpecular
        {
            get
            {
                if (_asteroidSpecular == null)
                {
                    _asteroidSpecular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("A0404040")), 5d);
                }

                return _asteroidSpecular;
            }
        }

        // SpaceStation
        public static Color SpaceStationHull
        {
            get
            {
                return UtilityWPF.AlphaBlend(UtilityWPF.GetRandomColor(108, 148), Colors.Gray, .25);
            }
        }
        [ThreadStatic]
        private static SpecularMaterial _spaceStationHullSpecular;
        public static SpecularMaterial SpaceStationHullSpecular
        {
            get
            {
                if (_spaceStationHullSpecular == null)
                {
                    _spaceStationHullSpecular = new SpecularMaterial(Brushes.Silver, 75d);
                }

                return _spaceStationHullSpecular;
            }
        }

        public static Color SpaceStationGlass
        {
            get
            {
                return Color.FromArgb(25, 220, 240, 240);		// the skin is semitransparent, so you can see the components inside
            }
        }
        [ThreadStatic]
        private static SpecularMaterial _spaceStationGlassSpecular_Front;
        public static SpecularMaterial SpaceStationGlassSpecular_Front
        {
            get
            {
                if (_spaceStationGlassSpecular_Front == null)
                {
                    _spaceStationGlassSpecular_Front = new SpecularMaterial(Brushes.White, 85d);
                }

                return _spaceStationGlassSpecular_Front;
            }
        }
        [ThreadStatic]
        private static SpecularMaterial _spaceStationGlassSpecular_Back;
        public static SpecularMaterial SpaceStationGlassSpecular_Back
        {
            get
            {
                if (_spaceStationGlassSpecular_Back == null)
                {
                    _spaceStationGlassSpecular_Back = new SpecularMaterial(new SolidColorBrush(Color.FromArgb(255, 20, 20, 20)), 10d);
                }

                return _spaceStationGlassSpecular_Back;
            }
        }

        public static Color SpaceStationForceField
        {
            get
            {
                return UtilityWPF.ColorFromHex("#2086E7FF");
            }
        }
        public static Color SpaceStationForceFieldEmissive_Front
        {
            get
            {
                return UtilityWPF.ColorFromHex("#0A89BBC7");
            }
        }
        public static Color SpaceStationForceFieldEmissive_Back
        {
            get
            {
                return UtilityWPF.ColorFromHex("#0AFFC086");
            }
        }

        // Egg
        public static Color EggColor
        {
            get
            {
                return UtilityWPF.ColorFromHex("E5E4C7");
            }
        }
        [ThreadStatic]
        private static SpecularMaterial _eggSpecular;
        public static SpecularMaterial EggSpecular
        {
            get
            {
                if (_eggSpecular == null)
                {
                    _eggSpecular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("20B8B69C")), 2d);
                }

                return _eggSpecular;
            }
        }

        // ******************** Parts

        // CargoBay
        public static Color CargoBay
        {
            get
            {
                return UtilityWPF.ColorFromHex("34543B");
            }
        }
        [ThreadStatic]
        private static SpecularMaterial _cargoBaySpecular;
        public static SpecularMaterial CargoBaySpecular
        {
            get
            {
                if (_cargoBaySpecular == null)
                {
                    _cargoBaySpecular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("5E5448")), 80d);
                }

                return _cargoBaySpecular;
            }
        }

        // Converters
        public static Color ConverterBase
        {
            get
            {
                return UtilityWPF.ColorFromHex("27403B");
            }
        }
        [ThreadStatic]
        private static SpecularMaterial _converterBaseSpecular;
        public static SpecularMaterial ConverterBaseSpecular
        {
            get
            {
                if (_converterBaseSpecular == null)
                {
                    _converterBaseSpecular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("1F1F61")), 70d);
                }

                return _converterBaseSpecular;
            }
        }

        public static Color ConverterFuel
        {
            get
            {
                return UtilityWPF.AlphaBlend(FuelTank, ConverterBase, .75d);
            }
        }
        public static SpecularMaterial ConverterFuelSpecular
        {
            get
            {
                return ConverterBaseSpecular;
            }
        }

        public static Color ConverterEnergy
        {
            get
            {
                return UtilityWPF.AlphaBlend(EnergyTank, ConverterBase, .75d);
            }
        }
        public static SpecularMaterial ConverterEnergySpecular
        {
            get
            {
                return ConverterBaseSpecular;
            }
        }

        public static Color ConverterAmmo
        {
            get
            {
                return UtilityWPF.AlphaBlend(AmmoBox, ConverterBase, .75d);
            }
        }
        [ThreadStatic]
        private static SpecularMaterial _converterAmmoSpecular;
        public static SpecularMaterial ConverterAmmoSpecular
        {
            get
            {
                if (_converterAmmoSpecular == null)
                {
                    Color ammoColor = UtilityWPF.ColorFromHex("D95448");
                    Color baseColor = UtilityWPF.ColorFromHex("1F1F61");
                    _converterAmmoSpecular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(ammoColor, baseColor, .6d)), 70d);
                }

                return _converterAmmoSpecular;
            }
        }

        // Sensors
        public static Color SensorBase
        {
            get
            {
                return UtilityWPF.ColorFromHex("4B4B4B");
            }
        }
        [ThreadStatic]
        private static SpecularMaterial _sensorBaseSpecular;
        public static SpecularMaterial SensorBaseSpecular
        {
            get
            {
                if (_sensorBaseSpecular == null)
                {
                    _sensorBaseSpecular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("80808080")), 35);
                }

                return _sensorBaseSpecular;
            }
        }

        public static Color SensorGravity
        {
            get
            {
                return UtilityWPF.ColorFromHex("856E5A");
            }
        }
        public static SpecularMaterial SensorGravitySpecular
        {
            get
            {
                return SensorBaseSpecular;
            }
        }

        public static Color SensorRadiation
        {
            get
            {
                return UtilityWPF.AlphaBlend(EnergyTank, SensorBase, .75d);
            }
        }
        public static SpecularMaterial SensorRadiationSpecular
        {
            get
            {
                return SensorBaseSpecular;
            }
        }

        public static Color SensorTractor
        {
            get
            {
                //NOTE: This color is the same as ShieldTractor
                return UtilityWPF.AlphaBlend(UtilityWPF.ColorFromHex("956CA1"), SensorBase, .75d);
            }
        }
        public static SpecularMaterial SensorTractorSpecular
        {
            get
            {
                return SensorBaseSpecular;
            }
        }

        public static Color SensorCollision
        {
            get
            {
                //NOTE: This color is the same as ShieldTractor
                return UtilityWPF.AlphaBlend(UtilityWPF.ColorFromHex("505663"), SensorBase, .75d);
            }
        }
        [ThreadStatic]
        private static SpecularMaterial _sensorCollisionSpecular;
        public static SpecularMaterial SensorCollisionSpecular
        {
            get
            {
                if (_sensorCollisionSpecular == null)
                {
                    _sensorCollisionSpecular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("C0D12A2A")), 70d);
                }

                return _sensorCollisionSpecular;
            }
        }

        public static Color SensorFluid
        {
            get
            {
                return UtilityWPF.ColorFromHex("58756D");
            }
        }
        public static SpecularMaterial SensorFluidSpecular
        {
            get
            {
                return SensorBaseSpecular;
            }
        }

        public static Color SensorSpin
        {
            get
            {
                return UtilityWPF.ColorFromHex("818F27");
            }
        }
        public static SpecularMaterial SensorSpinSpecular
        {
            get
            {
                return SensorBaseSpecular;
            }
        }

        public static Color SensorVelocity
        {
            get
            {
                return UtilityWPF.ColorFromHex("A3691D");
            }
        }
        public static SpecularMaterial SensorVelocitySpecular
        {
            get
            {
                return SensorBaseSpecular;
            }
        }

        public static Color SensorInternalForce
        {
            get
            {
                return UtilityWPF.ColorFromHex("22272B");
            }
        }
        public static SpecularMaterial SensorInternalForceSpecular
        {
            get
            {
                return SensorBaseSpecular;
            }
        }

        public static Color SensorNetForce
        {
            get
            {
                return UtilityWPF.ColorFromHex("B0B4B8");
            }
        }
        public static SpecularMaterial SensorNetForceSpecular
        {
            get
            {
                return SensorBaseSpecular;
            }
        }

        // HangarBay
        public static Color HangarBay
        {
            get
            {
                return UtilityWPF.ColorFromHex("BDA88E");
            }
        }
        [ThreadStatic]
        private static SpecularMaterial _hangarBaySpecular;
        public static SpecularMaterial HangarBaySpecular
        {
            get
            {
                if (_hangarBaySpecular == null)
                {
                    _hangarBaySpecular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("70615649")), 35d);
                }

                return _hangarBaySpecular;
            }
        }
        public static Color HangarBayTrim
        {
            get
            {
                return UtilityWPF.ColorFromHex("968671");
            }
        }
        [ThreadStatic]
        private static SpecularMaterial _hangarBayTrimSpecular;
        public static SpecularMaterial HangarBayTrimSpecular
        {
            get
            {
                if (_hangarBayTrimSpecular == null)
                {
                    _hangarBayTrimSpecular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("704A443D")), 35d);
                }

                return _hangarBayTrimSpecular;
            }
        }

        // AmmoBox
        public static Color AmmoBox
        {
            get
            {
                //return UtilityWPF.ColorFromHex("666E7F");
                return UtilityWPF.ColorFromHex("4B515E");
            }
        }
        [ThreadStatic]
        private static SpecularMaterial _ammoBoxSpecular;
        public static SpecularMaterial AmmoBoxSpecular
        {
            get
            {
                if (_ammoBoxSpecular == null)
                {
                    _ammoBoxSpecular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("C0D95448")), 65d);
                }

                return _ammoBoxSpecular;
            }
        }

        public static Color AmmoBoxPlate
        {
            get
            {
                return UtilityWPF.ColorFromHex("5E3131");
            }
        }
        [ThreadStatic]
        private static SpecularMaterial _ammoBoxPlateSpecular;
        public static SpecularMaterial AmmoBoxPlateSpecular
        {
            get
            {
                if (_ammoBoxPlateSpecular == null)
                {
                    _ammoBoxPlateSpecular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("C0DE2C2C")), 65d);
                }

                return _ammoBoxPlateSpecular;
            }
        }

        // Gun
        public static Color GunBase
        {
            get
            {
                return UtilityWPF.ColorFromHex("4F5359");		// flatter dark gray
            }
        }
        [ThreadStatic]
        private static SpecularMaterial _gunBaseSpecular;
        public static SpecularMaterial GunBaseSpecular
        {
            get
            {
                if (_gunBaseSpecular == null)
                {
                    _gunBaseSpecular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("546B6F78")), 35d);
                }

                return _gunBaseSpecular;
            }
        }
        public static Color GunBarrel
        {
            get
            {
                return UtilityWPF.ColorFromHex("3C424C");		// gunmetal
            }
        }
        [ThreadStatic]
        private static SpecularMaterial _gunBarrelSpecular;
        public static SpecularMaterial GunBarrelSpecular
        {
            get
            {
                if (_gunBarrelSpecular == null)
                {
                    _gunBarrelSpecular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("D023355C")), 75d);
                }

                return _gunBarrelSpecular;
            }
        }
        public static Color GunTrim
        {
            get
            {
                // red would look tacky, use light gray
                //return UtilityWPF.ColorFromHex("4C1A1A");		// dark red
                return UtilityWPF.ColorFromHex("5E6166");
            }
        }
        [ThreadStatic]
        private static SpecularMaterial _gunTrimSpecular;
        public static SpecularMaterial GunTrimSpecular
        {
            get
            {
                if (_gunTrimSpecular == null)
                {
                    _gunTrimSpecular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("80D12A2A")), 50d);
                }

                return _gunTrimSpecular;
            }
        }

        // Grapple
        public static Color GrapplePad
        {
            get
            {
                return UtilityWPF.ColorFromHex("573A3A");
            }
        }
        [ThreadStatic]
        private static SpecularMaterial _grapplePadSpecular;
        public static SpecularMaterial GrapplePadSpecular
        {
            get
            {
                if (_grapplePadSpecular == null)
                {
                    _grapplePadSpecular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("80BF8E8E")), 25d);
                }

                return _grapplePadSpecular;
            }
        }

        // BeamGun
        public static Color BeamGunDish
        {
            get
            {
                return UtilityWPF.ColorFromHex("324669");
            }
        }
        [ThreadStatic]
        private static SpecularMaterial _beamGunDishSpecular;
        public static SpecularMaterial BeamGunDishSpecular
        {
            get
            {
                if (_beamGunDishSpecular == null)
                {
                    _beamGunDishSpecular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("A80F45A3")), 40d);
                }

                return _beamGunDishSpecular;
            }
        }
        public static Color BeamGunCrystal
        {
            get
            {
                return UtilityWPF.ColorFromHex("3B5B94");
            }
        }
        [ThreadStatic]
        private static SpecularMaterial _beamGunCrystalSpecular;
        public static SpecularMaterial BeamGunCrystalSpecular
        {
            get
            {
                if (_beamGunCrystalSpecular == null)
                {
                    _beamGunCrystalSpecular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("FF0F45A3")), 90d);
                }

                return _beamGunCrystalSpecular;
            }
        }
        [ThreadStatic]
        private static EmissiveMaterial _beamGunCrystalEmissive;
        public static EmissiveMaterial BeamGunCrystalEmissive
        {
            get
            {
                if (_beamGunCrystalEmissive == null)
                {
                    _beamGunCrystalEmissive = new EmissiveMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("30719BE3")));
                }

                return _beamGunCrystalEmissive;
            }
        }
        public static Color BeamGunTrim
        {
            get
            {
                return UtilityWPF.ColorFromHex("5E6166");
            }
        }
        [ThreadStatic]
        private static SpecularMaterial _beamGunTrimSpecular;
        public static SpecularMaterial BeamGunTrimSpecular
        {
            get
            {
                if (_beamGunTrimSpecular == null)
                {
                    _beamGunTrimSpecular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("802A67D1")), 50d);
                }

                return _beamGunTrimSpecular;
            }
        }

        // FuelTank
        public static Color FuelTank
        {
            get
            {
                //return UtilityWPF.ColorFromHex("D49820");
                return UtilityWPF.ColorFromHex("A38521");
            }
        }
        [ThreadStatic]
        private static SpecularMaterial _fuelTankSpecular;
        public static SpecularMaterial FuelTankSpecular
        {
            get
            {
                if (_fuelTankSpecular == null)
                {
                    _fuelTankSpecular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("80659C9E")), 40d);
                }

                return _fuelTankSpecular;
            }
        }

        // EnergyTank
        public static Color EnergyTank
        {
            get
            {
                return UtilityWPF.ColorFromHex("507BC7");
            }
        }
        [ThreadStatic]
        private static SpecularMaterial _energyTankSpecular;
        public static SpecularMaterial EnergyTankSpecular
        {
            get
            {
                if (_energyTankSpecular == null)
                {
                    _energyTankSpecular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("D0551FB8")), 80d);
                }

                return _energyTankSpecular;
            }
        }
        [ThreadStatic]
        private static EmissiveMaterial _energyTankEmissive;
        public static EmissiveMaterial EnergyTankEmissive
        {
            get
            {
                if (_energyTankEmissive == null)
                {
                    _energyTankEmissive = new EmissiveMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("203348D4")));
                }

                return _energyTankEmissive;
            }
        }

        // PlasmaTank
        public static Color PlasmaTank
        {
            get
            {
                return UtilityWPF.ColorFromHex("6A66D9");
            }
        }
        [ThreadStatic]
        private static SpecularMaterial _plasmaTankSpecular;
        public static SpecularMaterial PlasmaTankSpecular
        {
            get
            {
                if (_plasmaTankSpecular == null)
                {
                    _plasmaTankSpecular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("DE6491")), 90d);
                }

                return _plasmaTankSpecular;
            }
        }

        // Thruster
        public static Color Thruster
        {
            get
            {
                return UtilityWPF.ColorFromHex("754F42");
            }
        }
        [ThreadStatic]
        private static SpecularMaterial _thrusterSpecular;
        public static SpecularMaterial ThrusterSpecular
        {
            get
            {
                if (_thrusterSpecular == null)
                {
                    _thrusterSpecular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("906B7D5A")), 20d);
                }

                return _thrusterSpecular;
            }
        }
        public static Color ThrusterBack
        {
            get
            {
                return UtilityWPF.ColorFromHex("4F403A");
            }
        }

        // TractorBeam
        public static Color TractorBeamBase
        {
            get
            {
                //return UtilityWPF.ColorFromHex("6599A3");
                return UtilityWPF.ColorFromHex("6F8185");
            }
        }
        [ThreadStatic]
        private static SpecularMaterial _tractorBeamBaseSpecular;
        public static SpecularMaterial TractorBeamBaseSpecular
        {
            get
            {
                if (_tractorBeamBaseSpecular == null)
                {
                    _tractorBeamBaseSpecular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("FF6599A3")), 60);
                }

                return _tractorBeamBaseSpecular;
            }
        }
        public static Color TractorBeamRod
        {
            get
            {
                //return UtilityWPF.ColorFromHex("8F7978");
                return UtilityWPF.ColorFromHex("8A788F");
            }
        }
        [ThreadStatic]
        private static SpecularMaterial _tractorBeamRodSpecular;
        public static SpecularMaterial TractorBeamRodSpecular
        {
            get
            {
                if (_tractorBeamRodSpecular == null)
                {
                    _tractorBeamRodSpecular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("8A30A1")), 100);
                }

                return _tractorBeamRodSpecular;
            }
        }
        [ThreadStatic]
        private static EmissiveMaterial _tractorBeamRodEmissive;
        public static EmissiveMaterial TractorBeamRodEmissive
        {
            get
            {
                if (_tractorBeamRodEmissive == null)
                {
                    _tractorBeamRodEmissive = new EmissiveMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("40AE75BA")));
                }

                return _tractorBeamRodEmissive;
            }
        }

        // Camera
        public static Color CameraBase
        {
            get
            {
                return Color.FromRgb(75, 75, 75);
            }
        }
        [ThreadStatic]
        private static SpecularMaterial _cameraBaseSpecular;
        public static SpecularMaterial CameraBaseSpecular
        {
            get
            {
                if (_cameraBaseSpecular == null)
                {
                    _cameraBaseSpecular = new SpecularMaterial(new SolidColorBrush(Color.FromArgb(128, 128, 128, 128)), 35d);
                }

                return _cameraBaseSpecular;
            }
        }
        public static Color CameraLens
        {
            get
            {
                //return UtilityWPF.ColorFromHex("3D1B16");
                return UtilityWPF.ColorFromHex("49211B");
            }
        }
        [ThreadStatic]
        private static SpecularMaterial _cameraLensSpecular;
        public static SpecularMaterial CameraLensSpecular
        {
            get
            {
                if (_cameraLensSpecular == null)
                {
                    _cameraLensSpecular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("80A00000")), 75d);
                }

                return _cameraLensSpecular;
            }
        }

        // Brain
        public static Color Brain
        {
            get
            {
                return UtilityWPF.ColorFromHex("FFE32078");
            }
        }
        [ThreadStatic]
        private static SpecularMaterial _brainSpecular;
        public static SpecularMaterial BrainSpecular
        {
            get
            {
                if (_brainSpecular == null)
                {
                    _brainSpecular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("A0ED8EB9")), 35d);
                }

                return _brainSpecular;
            }
        }
        public static Color BrainInsideStrand
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
        private static SpecularMaterial _brainInsideStrandSpecular;
        public static SpecularMaterial BrainInsideStrandSpecular
        {
            get
            {
                if (_brainInsideStrandSpecular == null)
                {
                    _brainInsideStrandSpecular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("FF0073")), 100d);
                }

                return _brainInsideStrandSpecular;
            }
        }

        // Shields
        public static Color ShieldBase
        {
            get
            {
                return UtilityWPF.ColorFromHex("1D8F8D");
            }
        }
        [ThreadStatic]
        private static SpecularMaterial _shieldBaseSpecular;
        public static SpecularMaterial ShieldBaseSpecular
        {
            get
            {
                if (_shieldBaseSpecular == null)
                {
                    _shieldBaseSpecular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("503FB2")), 100d);
                }

                return _shieldBaseSpecular;
            }
        }
        [ThreadStatic]
        private static EmissiveMaterial _shieldBaseEmissive;
        public static EmissiveMaterial ShieldBaseEmissive
        {
            get
            {
                if (_shieldBaseEmissive == null)
                {
                    _shieldBaseEmissive = new EmissiveMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("205FD4CE")));
                }

                return _shieldBaseEmissive;
            }
        }

        public static Color ShieldEnergy
        {
            get
            {
                return UtilityWPF.AlphaBlend(EnergyTank, ShieldBase, .65d);
            }
        }
        public static SpecularMaterial ShieldEnergySpecular
        {
            get
            {
                return ShieldBaseSpecular;
            }
        }

        public static Color ShieldKinetic
        {
            get
            {
                return UtilityWPF.AlphaBlend(UtilityWPF.ColorFromHex("505663"), ShieldBase, .9d);
            }
        }
        [ThreadStatic]
        private static SpecularMaterial _shieldKineticSpecular;
        public static SpecularMaterial ShieldKineticSpecular
        {
            get
            {
                if (_shieldKineticSpecular == null)
                {
                    _shieldKineticSpecular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("C0A63333")), 70d);
                }

                return _shieldKineticSpecular;
            }
        }

        public static Color ShieldTractor
        {
            get
            {
                //NOTE: This color is the same as SensorTractor
                return UtilityWPF.AlphaBlend(UtilityWPF.ColorFromHex("956CA1"), ShieldBase, .9d);
            }
        }
        [ThreadStatic]
        private static SpecularMaterial _shieldTractorSpecular;
        public static SpecularMaterial ShieldTractorSpecular
        {
            get
            {
                if (_shieldTractorSpecular == null)
                {
                    _shieldTractorSpecular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("C0AE75BA")), 50d);
                }

                return _shieldTractorSpecular;
            }
        }

        // SelfRepair
        public static Color SelfRepairBase
        {
            get
            {
                return UtilityWPF.ColorFromHex("E8E0C1");
            }
        }
        [ThreadStatic]
        private static SpecularMaterial _selfRepairBaseSpecular;
        public static SpecularMaterial SelfRepairBaseSpecular
        {
            get
            {
                if (_selfRepairBaseSpecular == null)
                {
                    _selfRepairBaseSpecular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("C0707691")), 60d);
                }

                return _selfRepairBaseSpecular;
            }
        }
        public static Color SelfRepairCross
        {
            get
            {
                return UtilityWPF.ColorFromHex("43B23B");
            }
        }
        [ThreadStatic]
        private static SpecularMaterial _selfRepairCrossSpecular;
        public static SpecularMaterial SelfRepairCrossSpecular
        {
            get
            {
                if (_selfRepairCrossSpecular == null)
                {
                    _selfRepairCrossSpecular = new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("203DB2")), 85d);
                }

                return _selfRepairCrossSpecular;
            }
        }

        // Light armor should be whitish (plastic)
        // Heavy armor should be silverish (metal)
    }
}
