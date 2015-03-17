using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.AsteroidMiner.MapParts;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.MapParts;
using Game.Newt.v2.GameItems.ShipParts;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.AsteroidMiner.AstMin2D
{
    public class SpaceStation2D : SpaceStation
    {
        #region Declaration Section

        // 3:2 ratio looks pretty good - there's a website for everything :)
        //http://www.crwflags.com/fotw/flags/xf-size.html
        //http://en.wikipedia.org/wiki/List_of_countries_by_proportions_of_national_flags
        public const double FLAGWIDTH = 300;
        public const double FLAGHEIGHT = 200;

        private const int HANGARVOLUMEINCREMENT = 20;

        #endregion

        #region Constructor

        public SpaceStation2D(Point3D position, World world, int materialID, Quaternion orientation)
            : base(position, world, materialID, orientation) { }

        #endregion

        #region Public Properties

        private FrameworkElement _flag = null;
        public FrameworkElement Flag
        {
            get
            {
                if (_flag == null)
                {
                    //TODO: Generate a more elaborite flag
                    //http://flag-designer.appspot.com/

                    _flag = new Rectangle()
                    {
                        Width = FLAGWIDTH,
                        Height = FLAGHEIGHT,
                        Fill = new SolidColorBrush(UtilityWPF.GetRandomColor(0, 192))       // nothing too bright
                    };
                }

                return _flag;
            }
            set
            {
                _flag = value;
            }
        }

        //TODO: May want to make a Credits property.  This would force the player to hop merchants to sell off all their inventory

        // This is inventory that the player can purchase
        public List<Inventory> StationInventory = new List<Inventory>();

        /// <summary>
        /// This is how much of the station's volume the player has purchased
        /// </summary>
        public int PurchasedVolume = 0;

        public double UsedVolume
        {
            get
            {
                return this.PlayerInventory.Sum(o => o.Volume);
            }
        }

        // This is inventory that the player has stored in the station
        public List<Inventory> PlayerInventory = new List<Inventory>();

        #endregion

        #region Public Methods

        //TODO: Call this from Update_MainThread every once in a while
        /// <summary>
        /// This should get called on an infrequent basis, and randomize the available inventory
        /// </summary>
        public void RandomizeInventory(bool completelyNew)
        {
            if (completelyNew)
            {
                this.StationInventory.Clear();
            }

            List<Inventory> inventory = new List<Inventory>();

            // Ships
            inventory.AddRange(AdjustInventory(
                this.StationInventory.Where(o => o.Ship != null),
                3,
                () => new Inventory(DefaultShips.GetDNA(UtilityCore.GetRandomEnum<DefaultShipType>()), StaticRandom.NextDouble(.5, 2))));

            // Minerals
            inventory.AddRange(AdjustInventory(
                this.StationInventory.Where(o => o.Mineral != null),
                6,
                () =>
                {
                    MineralType type = UtilityCore.GetRandomEnum<MineralType>();
                    return new Inventory(new GameItems.ShipParts.Cargo_Mineral(type, Mineral.GetSettingsForMineralType(type).Density * Miner.MINERAL_DENSITYMULT, StaticRandom.NextPercent(Miner.MINERAL_AVGVOLUME, 2)));
                }));

            //TODO: Parts

            this.StationInventory.Clear();
            this.StationInventory.AddRange(inventory);
        }

        //TODO: These instance getprice methods should differ a bit from station to station
        public decimal GetPrice_Buy(Inventory inventory)
        {
            decimal retVal = GetPrice_Base(inventory);
            return retVal * .8m;
        }
        public decimal GetPrice_Buy_Fuel()
        {
            return ItemOptionsAstMin2D.GetCredits_Fuel();
        }
        public decimal GetPrice_Buy_Energy()
        {
            return ItemOptionsAstMin2D.GetCredits_Energy();
        }
        public decimal GetPrice_Buy_Plasma()
        {
            return ItemOptionsAstMin2D.GetCredits_Plasma();
        }
        public decimal GetPrice_Buy_Ammo()
        {
            return ItemOptionsAstMin2D.GetCredits_Ammo();
        }

        public decimal GetPrice_Sell(Inventory inventory)
        {
            decimal retVal = GetPrice_Base(inventory);
            return retVal * 1.2m;
        }

        public static decimal GetPrice_Base(Inventory inventory)
        {
            if (inventory.Ship != null)
            {
                decimal sumParts = inventory.Ship.PartsByLayer.
                    SelectMany(o => o.Value).
                    Sum(o => ItemOptionsAstMin2D.GetCredits_ShipPart(o));

                return sumParts * 1.5m;
            }
            else if (inventory.Part != null)
            {
                return ItemOptionsAstMin2D.GetCredits_ShipPart(inventory.Part);
            }
            else if (inventory.Mineral != null)
            {
                return ItemOptionsAstMin2D.GetCredits_Mineral(inventory.Mineral.MineralType) * Convert.ToDecimal(inventory.Mineral.Volume);
            }
            else
            {
                throw new ApplicationException("Unknown type of inventory");
            }
        }

        /// <summary>
        /// This returns how much it will cost to buy more hangar space
        /// </summary>
        /// <returns>
        /// int=how much volume will be purchased
        /// decimal=how much it will cost to buy that volume
        /// </returns>
        public Tuple<int, decimal> GetHangarSpacePrice_Buy()
        {
            //TODO: Make each increment more expensive than the last
            return Tuple.Create(HANGARVOLUMEINCREMENT, 10m);
        }
        /// <summary>
        /// This returns how much the player should receive when selling back hangar space
        /// </summary>
        /// <returns>
        /// int=how much volume will be sold
        /// decimal=how much the player will get for that volume
        /// </returns>
        public Tuple<int, decimal> GetHangarSpacePrice_Sell()
        {
            if (this.PurchasedVolume == 0)
            {
                return Tuple.Create(0, 0m);
            }
            else
            {
                return Tuple.Create(HANGARVOLUMEINCREMENT, 8m);
            }
        }

        // These buy and sell one increment of hangar volume at a time
        public void BuyHangarSpace()
        {
            this.PurchasedVolume += HANGARVOLUMEINCREMENT;
        }
        public void SellHangarSpace()
        {
            int newVolume = this.PurchasedVolume - HANGARVOLUMEINCREMENT;

            if (newVolume < 0)
            {
                throw new ApplicationException("Can't sell back more volume than was purchased");
            }
            else if (this.UsedVolume > newVolume)
            {
                throw new ApplicationException("There is more inventory than the new lower volume");
            }

            this.PurchasedVolume = newVolume;
        }

        #endregion

        #region Private Methods

        private static IEnumerable<Inventory> AdjustInventory(IEnumerable<Inventory> inventory, int averageCountIfNone, Func<Inventory> getNew)
        {
            List<Inventory> retVal = new List<Inventory>(inventory);

            int numTotal = GetRandomInventoryCount(retVal.Count, averageCountIfNone);

            RemoveRandomItems(retVal);

            if (retVal.Count > numTotal)
            {
                // There are still too many.  Remove some to get down to numTotal
                RemoveRandomItems(retVal, retVal.Count - numTotal);
            }
            else if (numTotal > retVal.Count)
            {
                // There are too few.  Add some to get up to numTotal
                while (retVal.Count < numTotal)
                {
                    retVal.Add(getNew());
                }
            }

            return retVal;
        }

        private static int GetRandomInventoryCount(int current, int averageIfNone)
        {
            double retVal;

            if (current == 0)
            {
                retVal = StaticRandom.NextPercent(averageIfNone, 2.5);
            }
            else
            {
                retVal = current * StaticRandom.NextPercent(1, .5);
            }

            if (retVal == 0)
            {
                retVal = 1;
            }

            return Convert.ToInt32(Math.Round(retVal));
        }

        /// <summary>
        /// This method removes how many it's told, or figures out how many to remove
        /// </summary>
        private static void RemoveRandomItems<T>(IList<T> list, int? removeCount = null)
        {
            if (list.Count == 0)
            {
                return;
            }

            Random rand = StaticRandom.GetRandomForThread();

            if (removeCount == null)
            {
                double percent = rand.NextDouble(.33, 1);

                removeCount = Convert.ToInt32(list.Count * percent);
            }

            for (int cntr = 0; cntr < removeCount.Value; cntr++)
            {
                list.RemoveAt(rand.Next(list.Count));
            }
        }

        #endregion
    }
}
