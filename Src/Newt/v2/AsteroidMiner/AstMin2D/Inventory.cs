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
        public Inventory(ShipPartDNA part, double scale, int count)
        {
            //TODO: Rebuild the dna with a changed scaled
            //PartDNA scaledPart = 

            this.Ship = null;
            this.Part = part;
            this.Mineral = null;

            this.Count = count;

            this.Volume = 1;
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
    }
}
