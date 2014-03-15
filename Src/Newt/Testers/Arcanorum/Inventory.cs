using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Newt.Testers.Arcanorum
{
    //TODO: Place volume/mass limits
    //TODO: Add Volume/Mass properties
    public class Inventory
    {
        /// <summary>
        /// These are complete weapons
        /// </summary>
        public readonly ObservableCollection<Weapon> Weapons = new ObservableCollection<Weapon>();
    }
}
