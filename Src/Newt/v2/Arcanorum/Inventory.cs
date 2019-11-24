using Game.Newt.v2.Arcanorum.MapObjects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Newt.v2.Arcanorum
{
    //TODO: Make an Inventory2 that is handed CargoBay or CargoBayGroup.  Those will be the actual storage
    // Weapons get stored as Cargo_ShipPart, or make another derived class: Cargo_MapPart
    // The problem with Cargo_MapPart is there is no base dna class for map parts, so maybe store the map object itself (after removed from the map and viewport), or store dna as object
    //
    // Inventory has events for changes that CargoBay doesn't.  So may want to add events to cargobay.  That's made more complex by cargobay being threadsafe
    //
    // There should be some types of inventory managment that are controllable by neuron - probably in the cargo bay, but inventory could have more specific types of managment
    //      Pick up item - replace if "better"
    //              better would be defined by a classifier (or regression?) NN trained on what that bot prefers
    //      Sell "junk" items


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
