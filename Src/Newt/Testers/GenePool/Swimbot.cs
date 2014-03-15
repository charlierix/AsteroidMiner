using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using Game.HelperClasses;
using Game.Newt.AsteroidMiner2;
using Game.Newt.AsteroidMiner2.MapParts;
using Game.Newt.AsteroidMiner2.ShipEditor;
using Game.Newt.AsteroidMiner2.ShipParts;
using Game.Newt.HelperClasses;
using Game.Newt.NewtonDynamics;

namespace Game.Newt.Testers.GenePool
{
    public class Swimbot : Ship
    {
        #region Declaration Section

        private Map _map;

        private CargoBay[] _cargoBays = null;

        #endregion

        #region Constructor/Factory

        public static async Task<Swimbot> GetNewSwimbotAsync(EditorOptions options, ItemOptions itemOptions, ShipDNA dna, World world, int materialID, RadiationField radiation, IGravityField gravity, CameraPool cameraPool, Map map)
        {
            var construction = await GetNewShipConstructionAsync(options, itemOptions, dna, world, materialID, radiation, gravity, cameraPool, true, true);

            return new Swimbot(construction, map);
        }

        protected Swimbot(ShipConstruction construction, Map map)
            : base(construction)
        {
            _map = map;

            _cargoBays = base.Parts.Where(o => o is CargoBay).Select(o => (CargoBay)o).ToArray();
        }

        #endregion

        #region Public Methods

        public void CollidedMineral(Mineral mineral)
        {
            if (mineral.MineralType == MineralType.Ruby)
            {
                // Treat this like anti food.  Remove cargo/energy/fuel to compensate for the mass of this poison.  If there's not enough,
                // then die
                AddPoison(mineral);
            }
            else
            {
                // The only other mineral type should be emerald, but just consider everything else as food (the converters just go by
                // mass anyway)
                AddFood(mineral);
            }
        }

        #endregion

        #region Private Methods

        private void AddFood(Mineral mineral)
        {
            var quantity = base.CargoBays.CargoVolume;

            if (quantity.Item2 - quantity.Item1 < mineral.VolumeInCubicMeters)
            {
                // The cargo bays are too full
                return;
            }

            // Try to pop this out of the map
            if (!_map.RemoveItem(mineral))
            {
                // It's already gone
                return;
            }

            // Convert it to cargo
            Cargo_Mineral cargo = new Cargo_Mineral(mineral.MineralType, mineral.Density, mineral.VolumeInCubicMeters);

            // Try to add this to the cargo bays - the total volume may be enough, but the mineral may be too large for any
            // one cargo bay
            if (base.CargoBays.Add(cargo))
            {
                // Finish removing it from the real world
                mineral.PhysicsBody.Dispose();

                this.ShouldRecalcMass_Large = true;
            }
            else
            {
                // It didn't fit, give it back to the map
                _map.AddItem(mineral);
            }
        }
        private void AddPoison(Mineral mineral)
        {
            // Try to pop this out of the map
            if (!_map.RemoveItem(mineral))
            {
                // It's already gone
                return;
            }

            double mass = mineral.VolumeInCubicMeters * mineral.Density;

            // Try to remove the equivalent mass from the cargo bay
            var vomit = base.CargoBays.RemoveMineral_Mass(mass);

            if (vomit.Item1 > 0d)
            {
                this.ShouldRecalcMass_Large = true;
            }

            if (Math3D.IsNearValue(vomit.Item1, mass))
            {
                // There was enough in the cargo bay to balance out the poison
                return;
            }

            mass -= vomit.Item1;

            // The remaining mass needs to come out of plasma/energy/fuel

            // The conversion ratios are meant to be lossy when going from mass to energy/fuel/plasma.  But when run the other direction, they
            // become overly ideal, so bump the target mass a bit to account for that
            mass *= 1.1d;

            // Plasma is least important, take from that first
            if (AddPoisonSprtContainer(ref mass, this.Plasma, _itemOptions.MatterToPlasmaConversionRate))
            {
                // There was enough to cover it
                return;
            }

            // Draw from fuel next
            if (this.Fuel != null && this.Fuel.QuantityCurrent > 0d)
            {
                this.ShouldRecalcMass_Large = true;     // fuel is about to be removed, so set this now
            }

            if (AddPoisonSprtContainer(ref mass, this.Fuel, _itemOptions.MatterToFuelConversionRate))
            {
                // There was enough to cover it
                return;
            }

            // Go after energy as a last resort
            if (AddPoisonSprtContainer(ref mass, this.Energy, _itemOptions.MatterToEnergyConversionRate))
            {
                // There was enough to cover it
                return;
            }

            // If there's nothing left, then just exist.  The next time this bot is examined, it will be considered dead
        }
        private static bool AddPoisonSprtContainer(ref double mass, IContainer container, double ratio)
        {
            if (container == null)
            {
                return false;
            }

            double needed = mass * ratio;
            double unmet = container.RemoveQuantity(needed, false);

            // If unmet is zero, then there was enough in this container
            if (Math3D.IsNearZero(unmet))
            {
                mass = 0d;
                return true;
            }

            // Reduce mass by the amount removed
            double removed = needed - unmet;
            mass -= removed * (1d / ratio);

            return false;
        }

        #endregion
    }
}
