using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.AsteroidMiner.MapParts;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.MapParts;
using Game.Newt.v2.GameItems.ShipParts;

namespace Game.Newt.v2.AsteroidMiner.AstMin2D
{
    public class ItemOptionsAstMin2D
    {
        public const double MINERAL_AVGVOLUME = .5d;
        public const double MINERAL_DENSITYMULT = .05d;

        public const double MINASTEROIDRADIUS = 1d;

        public static decimal GetCredits_Fuel()
        {
            // In asteroid miner 1, it was .5 credits per 1 unit fuel.  But asteroid miner 2 has much smaller ships
            return 5m;
        }
        public static decimal GetCredits_Energy()
        {
            return 5m;
        }
        public static decimal GetCredits_Plasma()
        {
            return 30m;
        }
        public static decimal GetCredits_Ammo()
        {
            return 20m;
        }

        /// <summary>
        /// This returns the price of a mineral with a volume of 1
        /// </summary>
        private static decimal GetCredits_Mineral_ORIG(MineralType mineralType)
        {
            const decimal BASE = 3m;

            switch (mineralType)
            {
                case MineralType.Ice:
                    return BASE * 1m;

                case MineralType.Graphite:
                    return BASE * 2m;

                case MineralType.Diamond:
                    return BASE * 3m;

                case MineralType.Emerald:
                case MineralType.Saphire:
                case MineralType.Ruby:
                    return BASE * 3.5m;

                case MineralType.Iron:
                    return BASE * 7m;

                case MineralType.Gold:
                    return BASE * 10m;

                case MineralType.Platinum:
                    return BASE * 15m;

                case MineralType.Rixium:
                    return BASE * 50m;

                default:
                    throw new ApplicationException("Unknown MineralType: " + mineralType.ToString());
            }
        }
        public static decimal GetCredits_Mineral(MineralType mineralType)
        {
            //const decimal BASE = 3m;
            const decimal BASE = 10m;

            //roughly x^2.7

            switch (mineralType)
            {
                case MineralType.Ice:
                    //return BASE * 1m;
                    return BASE * 3m;

                case MineralType.Graphite:
                    return BASE * 6.5m;

                case MineralType.Diamond:
                    return BASE * 20m;

                case MineralType.Ruby:
                    return BASE * 42m;

                case MineralType.Saphire:
                    return BASE * 77m;

                case MineralType.Emerald:
                    return BASE * 125m;

                case MineralType.Iron:
                    return BASE * 190m;

                case MineralType.Gold:
                    return BASE * 275m;

                case MineralType.Platinum:
                    return BASE * 380m;

                case MineralType.Rixium:
                    return BASE * 500m;

                default:
                    throw new ApplicationException("Unknown MineralType: " + mineralType.ToString());
            }
        }

        // These two are just helper methods
        public static decimal GetCredits_Mineral(MineralType mineralType, double volume)
        {
            return GetCredits_Mineral(mineralType) * Convert.ToDecimal(volume);
        }
        public static MineralDNA GetMineral(MineralType mineralType, decimal credits)
        {
            double volume = Convert.ToDouble(credits) / Convert.ToDouble(GetCredits_Mineral(mineralType));

            return GetMineral(mineralType, volume);
        }
        public static MineralDNA GetMineral(MineralType mineralType, double volume)
        {
            return new MineralDNA()
            {
                PartType = Mineral.PARTTYPE,
                //Radius = ,
                //Position = ,
                //Orientation = ,
                //Velocity = ,
                //AngularVelocity = ,

                MineralType = mineralType,
                Volume = volume,
                Density = Mineral.GetSettingsForMineralType(mineralType).Density * MINERAL_DENSITYMULT,
                Scale = volume / MINERAL_AVGVOLUME,
                Credits = GetCredits_Mineral(mineralType, volume),
            };
        }

        public static decimal GetCredits_ShipPart(ShipPartDNA dna)
        {
            decimal baseAmt = GetCredits_ShipPart_Base(dna.PartType ?? "");

            decimal scale = Convert.ToDecimal(Math1D.Avg(dna.Scale.X, dna.Scale.Y, dna.Scale.Z));

            return baseAmt * scale;
        }
        public static decimal GetCredits_ShipPart_Base(string partType)
        {
            const decimal BASE = 18m;

            switch (partType)
            {
                case FuelTank.PARTTYPE:
                case EnergyTank.PARTTYPE:
                case PlasmaTank.PARTTYPE:
                case AmmoBox.PARTTYPE:
                    return BASE * 1m;

                case CargoBay.PARTTYPE:
                    return BASE * 1.5m;

                case HangarBay.PARTTYPE:
                    return BASE * 10m;

                case SwarmBay.PARTTYPE:
                    return BASE * 20m;

                case Thruster.PARTTYPE:
                    return BASE * 1m;

                case Brain.PARTTYPE:
                case BrainRGBRecognizer.PARTTYPE:
                    return BASE * 1m;

                case CameraColorRGB.PARTTYPE:
                case Eye.PARTTYPE:
                case SensorCollision.PARTTYPE:
                case SensorFluid.PARTTYPE:
                case SensorGravity.PARTTYPE:
                case SensorInternalForce.PARTTYPE:
                case SensorNetForce.PARTTYPE:
                case SensorRadiation.PARTTYPE:
                case SensorSpin.PARTTYPE:
                case SensorTractor.PARTTYPE:
                case SensorVelocity.PARTTYPE:
                    return BASE * .5m;

                case ConverterEnergyToAmmo.PARTTYPE:
                case ConverterEnergyToFuel.PARTTYPE:
                case ConverterEnergyToPlasma.PARTTYPE:
                case ConverterFuelToEnergy.PARTTYPE:
                case ConverterMatterToAmmo.PARTTYPE:
                case ConverterMatterToEnergy.PARTTYPE:
                case ConverterMatterToPlasma.PARTTYPE:
                case ConverterMatterToFuel.PARTTYPE:
                case ConverterRadiationToEnergy.PARTTYPE:
                    return BASE * 60m;

                case SelfRepair.PARTTYPE:
                    return BASE * 150m;

                case ShieldEnergy.PARTTYPE:
                case ShieldKinetic.PARTTYPE:
                case ShieldTractor.PARTTYPE:
                    return BASE * 40m;

                case TractorBeam.PARTTYPE:
                    return BASE * 7m;

                case ProjectileGun.PARTTYPE:
                    return BASE * 4m;
                case BeamGun.PARTTYPE:
                    return BASE * 5m;
                case GrappleGun.PARTTYPE:
                    return BASE * 3m;

                default:
                    return 20m;
            }
        }

        private static decimal GetCredits_ShipPart_Base_CHEAPFLAT(string partType)
        {
            // the prices are a bit too flat (exotic parts should cost a lot more)

            const decimal BASE = 12m;

            switch (partType)
            {
                case FuelTank.PARTTYPE:
                case EnergyTank.PARTTYPE:
                case PlasmaTank.PARTTYPE:
                case AmmoBox.PARTTYPE:
                    return BASE * 1m;

                case CargoBay.PARTTYPE:
                    return BASE * 1.5m;

                case HangarBay.PARTTYPE:
                    return BASE * 3m;

                case SwarmBay.PARTTYPE:
                    return BASE * 6m;

                case Thruster.PARTTYPE:
                    return BASE * 1m;

                case Brain.PARTTYPE:
                case BrainRGBRecognizer.PARTTYPE:
                    return BASE * 1m;

                case CameraColorRGB.PARTTYPE:
                case Eye.PARTTYPE:
                case SensorCollision.PARTTYPE:
                case SensorFluid.PARTTYPE:
                case SensorGravity.PARTTYPE:
                case SensorInternalForce.PARTTYPE:
                case SensorNetForce.PARTTYPE:
                case SensorRadiation.PARTTYPE:
                case SensorSpin.PARTTYPE:
                case SensorTractor.PARTTYPE:
                case SensorVelocity.PARTTYPE:
                    return BASE * .5m;

                case ConverterEnergyToAmmo.PARTTYPE:
                case ConverterEnergyToFuel.PARTTYPE:
                case ConverterEnergyToPlasma.PARTTYPE:
                case ConverterFuelToEnergy.PARTTYPE:
                case ConverterMatterToAmmo.PARTTYPE:
                case ConverterMatterToEnergy.PARTTYPE:
                case ConverterMatterToPlasma.PARTTYPE:
                case ConverterMatterToFuel.PARTTYPE:
                case ConverterRadiationToEnergy.PARTTYPE:
                    return BASE * 10m;

                case SelfRepair.PARTTYPE:
                    return BASE * 25m;

                case ShieldEnergy.PARTTYPE:
                case ShieldKinetic.PARTTYPE:
                case ShieldTractor.PARTTYPE:
                    return BASE * 10m;

                case TractorBeam.PARTTYPE:
                    return BASE * 7m;

                case ProjectileGun.PARTTYPE:
                    return BASE * 4m;
                case BeamGun.PARTTYPE:
                    return BASE * 5m;
                case GrappleGun.PARTTYPE:
                    return BASE * 3m;

                default:
                    return 15m;
            }
        }
    }
}
