using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Newt.Testers.Arcanorum
{
    public class MaterialIDs
    {
        public int Wall = -1;
        public int Bot = -1;
        public int BotRam = -1;
        public int ExplodingBot = -1;		// there is no property on body to turn off collision detection, that's done by its current material
        public int Weapon = -1;
        public int TreasureBox = -1;
    }
}
