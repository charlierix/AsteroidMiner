using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.HelperClassesWPF;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.MapParts;
using Game.Newt.v2.GameItems.ShipParts;

namespace Game.Newt.v2.AsteroidMiner.AstMin2D
{
    public class ItemOptionsAstMin2D
    {
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
        public static decimal GetCredits_Mineral(MineralType mineralType)
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

        public static decimal GetCredits_ShipPart(PartDNA dna)
        {
            decimal baseAmt = GetCredits_ShipPart_Base(dna.PartType ?? "");

            decimal scale = Convert.ToDecimal(Math3D.Avg(dna.Scale.X, dna.Scale.Y, dna.Scale.Z));

            return baseAmt * scale;
        }
        public static decimal GetCredits_ShipPart_Base(string partType)
        {
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

                case Thruster.PARTTYPE:
                    return BASE * 1m;

                case Brain.PARTTYPE:
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
                case ConverterFuelToEnergy.PARTTYPE:
                case ConverterMatterToAmmo.PARTTYPE:
                case ConverterMatterToEnergy.PARTTYPE:
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
