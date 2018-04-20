using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.HelperClassesCore;
using Game.Newt.v2.AsteroidMiner.MapParts;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.MapParts;
using Game.Newt.v2.GameItems.ShipParts;

namespace Game.Newt.v2.AsteroidMiner.AstMin2D
{
    public class Inventory
    {
        #region Constructor

        public Inventory(ShipDNA ship, double scale)
        {
            ShipDNA scaledShip = scale.IsNearValue(1) ? ship : ShipDNA.Resize(ship, scale);

            this.Ship = scaledShip;
            this.Part = null;
            this.Mineral = null;

            this.Count = 1;

            //TODO: calculate these properly
            this.Volume = scale;
            this.Mass = 1;

            this.Token = TokenGenerator.NextToken();
        }
        public Inventory(ShipPartDNA part)
        {
            this.Ship = null;
            this.Part = part;
            this.Mineral = null;

            this.Count = 1;

            this.Volume = Math1D.Avg(part.Scale.X, part.Scale.Y, part.Scale.Z);
            this.Mass = 1;

            this.Token = TokenGenerator.NextToken();
        }
        public Inventory(MineralDNA mineral)
        {
            this.Ship = null;
            this.Part = null;
            this.Mineral = mineral;

            this.Count = 1;

            this.Volume = mineral.Volume;
            this.Mass = mineral.Density * mineral.Volume;

            this.Token = TokenGenerator.NextToken();
        }

        #endregion

        public readonly double Volume;

        public readonly double Mass;

        //TODO: Create multiple entries instead
        /// <summary>
        /// This lets parts be sold as sets (when building a ship, it's you usually want it symetrical, so help the player
        /// out by two of the same kind of part)
        /// </summary>
        public readonly int Count;

        // Only one of these will be set
        public readonly ShipDNA Ship;
        public readonly ShipPartDNA Part;
        public readonly MineralDNA Mineral;     // can't store the actual mineral, because when a cargo bay removes, it reduces volume.  Since this is a readonly property, there's no way to overwrite with the clone

        public readonly long Token;

        #region Public Methods

        public InventoryDNA GetNewDNA()
        {
            return new InventoryDNA()
            {
                Volume = this.Volume,
                Mass = this.Mass,
                Count = this.Count,

                Ship = this.Ship,
                Part = this.Part,
                Mineral = this.Mineral,
            };
        }

        #endregion
    }

    #region Class: InventoryDNA

    public class InventoryDNA
    {
        public double Volume { get; set; }
        public double Mass { get; set; }
        public int Count { get; set; }

        public ShipDNA Ship { get; set; }
        public ShipPartDNA Part { get; set; }
        public MineralDNA Mineral { get; set; }

        public Inventory ToInventory()
        {
            if (this.Ship != null)
            {
                return new Inventory(this.Ship, this.Volume);       //TODO: handle scale vs volume better
            }
            else if (this.Part != null)
            {
                return new Inventory(this.Part);
            }
            else if (this.Mineral != null)
            {
                return new Inventory(this.Mineral);
            }
            else
            {
                throw new ApplicationException("Unknown type of inventory, everything is null");
            }
        }
    }

    #endregion
}
