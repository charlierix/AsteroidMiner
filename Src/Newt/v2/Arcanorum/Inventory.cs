using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Newt.v2.Arcanorum
{
    //TODO: Place volume limits
    public class Inventory
    {
        public event EventHandler InventoryChanged = null;

        private double _mass = 0;

        public Inventory()
        {
            Weapons.CollectionChanged += Weapons_CollectionChanged;
        }

        /// <summary>
        /// These are complete weapons
        /// </summary>
        public readonly ObservableCollection<Weapon> Weapons = new ObservableCollection<Weapon>();

        //NOTE: If you listen to Weapons.CollectionChanged directly, mass might not have had a chance to update
        public double Mass => _mass;

        private void Weapons_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnInventoryChanged();
        }
        private void OnInventoryChanged()
        {
            _mass = Weapons.Sum(o => o.Mass);

            InventoryChanged?.Invoke(this, new EventArgs());
        }
    }
}
