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
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.GameItems.ShipParts;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.AsteroidMiner.AstMin2D
{
    public class SpaceStation2D : SpaceStation
    {
        #region interface: IItemChance

        private interface IItemChance
        {
            double ProbabilityPercent { get; set; }
            double ScaleBase { get; set; }
        }

        #endregion
        #region class: PartCreateChance

        private class PartCreateChance : IItemChance
        {
            public string PartType { get; set; }
            public double ProbabilityPercent { get; set; }
            public double ScaleBase { get; set; }
        }

        #endregion
        #region class: ShipCreateChance

        private class ShipCreateChance : IItemChance
        {
            public DefaultShipType ShipType { get; set; }
            public double ProbabilityPercent { get; set; }
            public double ScaleBase { get; set; }
        }

        #endregion

        #region Declaration Section

        // 3:2 ratio looks pretty good - there's a website for everything :)
        //http://www.crwflags.com/fotw/flags/xf-size.html
        //http://en.wikipedia.org/wiki/List_of_countries_by_proportions_of_national_flags
        public const double FLAGWIDTH = 300;
        public const double FLAGHEIGHT = 200;

        private const int HANGARVOLUMEINCREMENT = 20;

        private double _inventoryRandomizeCountdown = -1;

        private Tuple<string, decimal>[] _partPriceAdjustments = null;
        private Tuple<MineralType, decimal>[] _mineralPriceAdjustments = null;

        private readonly PartCreateChance[] _partChances;
        private readonly ShipCreateChance[] _shipChances;

        #endregion

        #region Constructor

        public SpaceStation2D(Point3D position, World world, int materialID, Quaternion orientation, FlagProps flag = null)
            : base(position, world, materialID, orientation)
        {
            _partChances = GetPartChances();
            _shipChances = GetShipChances();

            if (flag != null)
            {
                this.Flag = new FlagVisual(FLAGWIDTH, FLAGHEIGHT, flag);
            }
        }

        #endregion

        #region Public Properties

        private FlagVisual _flag = null;
        public FlagVisual Flag
        {
            get
            {
                if (_flag == null)
                {
                    //TODO: Generate a more elaborite flag
                    //http://flag-designer.appspot.com/

                    //_flag = new Rectangle()
                    //{
                    //    Width = FLAGWIDTH,
                    //    Height = FLAGHEIGHT,
                    //    Fill = new SolidColorBrush(UtilityWPF.GetRandomColor(0, 192))       // nothing too bright
                    //};

                    _flag = new FlagVisual(FLAGWIDTH, FLAGHEIGHT, FlagGenerator.GetRandomFlag());
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

        /// <summary>
        /// This should get called on an infrequent basis, and randomize the available inventory
        /// </summary>
        public void RandomizeInventory(bool completelyNew)
        {
            RandomizePrices();

            if (completelyNew)
            {
                this.StationInventory.Clear();
            }

            List<Inventory> inventory = new List<Inventory>();

            // Ships
            inventory.AddRange(AdjustInventory(
                this.StationInventory.Where(o => o.Ship != null),
                3,
                () =>
                {
                    DefaultShipType shipType = GetRandomItemChance(_shipChances).ShipType;
                    ShipDNA shipDNA = DefaultShips.GetDNA(shipType);
                    return new Inventory(shipDNA, StaticRandom.NextDouble(.5, 2));
                }));


            // Parts
            inventory.AddRange(AdjustInventory(
                this.StationInventory.Where(o => o.Part != null),
                4,
                () =>
                {
                    PartCreateChance partType = GetRandomItemChance(_partChances);
                    ShipPartDNA partDNA = GetRandomPart(partType);      //TODO: Have a chance to make 2 if it's something that could come in pairs (thruster, gun, etc)
                    return new Inventory(partDNA);
                }));

            // Minerals
            inventory.AddRange(AdjustInventory(
                this.StationInventory.Where(o => o.Mineral != null),
                4,
                () =>
                {
                    MineralType type = UtilityCore.GetRandomEnum<MineralType>();
                    double volume = StaticRandom.NextPercent(ItemOptionsAstMin2D.MINERAL_AVGVOLUME, 2);
                    return new Inventory(ItemOptionsAstMin2D.GetMineral(type, volume));
                }));

            this.StationInventory.Clear();
            this.StationInventory.AddRange(inventory);

            _inventoryRandomizeCountdown = StaticRandom.NextDouble(3 * 60, 8 * 60);
        }

        //TODO: These instance getprice methods should differ a bit from station to station
        //create a modification map <parttype,double>[] for each part type with random percents
        public Tuple<decimal, decimal> GetPrice_Buy(Inventory inventory)
        {
            Tuple<decimal, decimal> retVal = GetPrice_Base(inventory);
            return Tuple.Create(retVal.Item1 * 1.2m, retVal.Item2);
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

        public decimal GetPrice_Repair(PartBase part)
        {
            const decimal MULT_DESTROYED = .75m;
            const decimal MULT_PARTIAL = .4m;

            if (!part.IsDestroyed && part.HitPoints_Current.IsNearValue(part.HitPoints_Max))
            {
                return 0m;
            }

            // Make the repair cost some fraction of the cost of a new part
            decimal purchasePrice = ItemOptionsAstMin2D.GetCredits_ShipPart(part.GetNewDNA());

            if (part.IsDestroyed)
            {
                return purchasePrice * MULT_DESTROYED;
            }
            else
            {
                double percent = (part.HitPoints_Max - part.HitPoints_Current) / part.HitPoints_Max;

                return Convert.ToDecimal(percent) * purchasePrice * MULT_PARTIAL;
            }
        }

        public Tuple<decimal, decimal> GetPrice_Sell(Inventory inventory)
        {
            Tuple<decimal, decimal> retVal = GetPrice_Base(inventory);
            return Tuple.Create(retVal.Item1 * .8m, retVal.Item2);
        }

        public Tuple<decimal, decimal> GetPrice_Base(Inventory inventory)
        {
            if (inventory.Ship != null)
            {
                Tuple<decimal, decimal>[] prices = inventory.Ship.PartsByLayer.
                    SelectMany(o => o.Value).
                    Select(o =>
                    {
                        decimal standPrice = ItemOptionsAstMin2D.GetCredits_ShipPart(o);
                        decimal mult1 = FindPriceMult(o.PartType);
                        return Tuple.Create(standPrice, mult1);
                    }).
                    ToArray();

                // Get the weighted percent (taking this % times the sum will be the same as taking the sum of each individual price*%)
                double mult2 = Math1D.Avg(prices.Select(o => Tuple.Create(Convert.ToDouble(o.Item2), Convert.ToDouble(o.Item1))).ToArray());

                // Make a completed ship more expensive than just parts
                return Tuple.Create(prices.Sum(o => o.Item1) * 1.5m, Convert.ToDecimal(mult2));
            }
            else if (inventory.Part != null)
            {
                decimal standPrice = ItemOptionsAstMin2D.GetCredits_ShipPart(inventory.Part);
                decimal mult = FindPriceMult(inventory.Part.PartType);
                return Tuple.Create(standPrice, mult);
            }
            else if (inventory.Mineral != null)
            {
                //return ItemOptionsAstMin2D.GetCredits_Mineral(inventory.Mineral.MineralType, inventory.Mineral.Volume);
                decimal mult = FindPriceMult(inventory.Mineral.MineralType);
                return Tuple.Create(inventory.Mineral.Credits, mult);
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

        public override void Update_MainThread(double elapsedTime)
        {
            base.Update_MainThread(elapsedTime);

            _inventoryRandomizeCountdown -= elapsedTime;

            if (_inventoryRandomizeCountdown < 0)
            {
                RandomizeInventory(false);
            }
        }

        public SpaceStation2DDNA GetNewDNA()
        {
            return new SpaceStation2DDNA()
            {
                Position = this.PositionWorld,
                Flag = this.Flag.FlagProps,
                PurchasedVolume = this.PurchasedVolume,
                PlayerInventory = this.PlayerInventory.Select(o => o.GetNewDNA()).ToArray(),
            };
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

        private static PartCreateChance[] GetPartChances()
        {
            Random rand = StaticRandom.GetRandomForThread();

            PartCreateChance[] retVal = new[]
            {
                new PartCreateChance() { PartType = Thruster.PARTTYPE, ProbabilityPercent = 1, ScaleBase = 1 },
                new PartCreateChance() { PartType = FuelTank.PARTTYPE, ProbabilityPercent = 1, ScaleBase = 1 },

                new PartCreateChance() { PartType = CargoBay.PARTTYPE, ProbabilityPercent = .7, ScaleBase = 1 },

                new PartCreateChance() { PartType = AmmoBox.PARTTYPE, ProbabilityPercent = .5, ScaleBase = 1 },
                new PartCreateChance() { PartType = ProjectileGun.PARTTYPE, ProbabilityPercent = .5, ScaleBase = 1 },

                new PartCreateChance() { PartType = EnergyTank.PARTTYPE, ProbabilityPercent = .33, ScaleBase = 1 },
                new PartCreateChance() { PartType = PlasmaTank.PARTTYPE, ProbabilityPercent = .33, ScaleBase = 1 },

                new PartCreateChance() { PartType = ConverterMatterToFuel.PARTTYPE, ProbabilityPercent = .1, ScaleBase = 1 },
                new PartCreateChance() { PartType = ConverterMatterToEnergy.PARTTYPE, ProbabilityPercent = .1, ScaleBase = 1 },
                new PartCreateChance() { PartType = ConverterMatterToAmmo.PARTTYPE, ProbabilityPercent = .1, ScaleBase = 1 },
                new PartCreateChance() { PartType = ConverterMatterToPlasma.PARTTYPE, ProbabilityPercent = .1, ScaleBase = 1 },

                new PartCreateChance() { PartType = ConverterRadiationToEnergy.PARTTYPE, ProbabilityPercent = .05, ScaleBase = 1 },
                new PartCreateChance() { PartType = ConverterEnergyToAmmo.PARTTYPE, ProbabilityPercent = .05, ScaleBase = 1 },
                new PartCreateChance() { PartType = ConverterEnergyToFuel.PARTTYPE, ProbabilityPercent = .05, ScaleBase = 1 },
                new PartCreateChance() { PartType = ConverterEnergyToPlasma.PARTTYPE, ProbabilityPercent = .05, ScaleBase = 1 },

                new PartCreateChance() { PartType = SwarmBay.PARTTYPE, ProbabilityPercent = .025, ScaleBase = 1 },

                // Everything below still needs to be implemented

                new PartCreateChance() { PartType = BeamGun.PARTTYPE, ProbabilityPercent = .01, ScaleBase = 1 },
                new PartCreateChance() { PartType = TractorBeam.PARTTYPE, ProbabilityPercent = .01, ScaleBase = 1 },

                new PartCreateChance() { PartType = ShieldEnergy.PARTTYPE, ProbabilityPercent = .01, ScaleBase = 1 },
                new PartCreateChance() { PartType = ShieldKinetic.PARTTYPE, ProbabilityPercent = .01, ScaleBase = 1 },
                new PartCreateChance() { PartType = ShieldTractor.PARTTYPE, ProbabilityPercent = .01, ScaleBase = 1 },

                // May want to implement some variant of these.  Maybe this is just an option for free on the HUD
                //new PartCreateChance() { PartType = SensorGravity.PARTTYPE, ProbabilityPercent = .015, ScaleBase = 1 },
                //new PartCreateChance() { PartType = SensorTractor.PARTTYPE, ProbabilityPercent = .015, ScaleBase = 1 },
                //new PartCreateChance() { PartType = SensorRadiation.PARTTYPE, ProbabilityPercent = .015, ScaleBase = 1 },
            };

            // Mutate Percents
            MutatePercents(retVal, rand);

            // Normalize
            NormalizePercents(retVal);

            return retVal;
        }
        private static ShipCreateChance[] GetShipChances()
        {
            Random rand = StaticRandom.GetRandomForThread();

            DefaultShipType[] solarTypes = new[]
            {
                DefaultShipType.SolarPack1,
                DefaultShipType.SolarPack2,
                DefaultShipType.SolarPack3,
                DefaultShipType.SolarPack4,
            };

            IEnumerable<ShipCreateChance> standard = UtilityCore.GetEnums<DefaultShipType>(solarTypes).
                Select(o => new ShipCreateChance() { ShipType = o, ProbabilityPercent = 1d, ScaleBase = 1d });

            IEnumerable<ShipCreateChance> solars = solarTypes.
                Select(o => new ShipCreateChance() { ShipType = o, ProbabilityPercent = .1d, ScaleBase = 1d });

            ShipCreateChance[] retVal = standard.
                Concat(solars).
                ToArray();

            // Mutate Percents
            MutatePercents(retVal, rand);

            // Normalize
            NormalizePercents(retVal);

            return retVal;
        }

        private static void MutatePercents(IItemChance[] items, Random rand)
        {
            foreach (IItemChance item in items)
            {
                item.ProbabilityPercent = rand.NextPercent(item.ProbabilityPercent, .25);
                item.ScaleBase = rand.NextPercent(item.ScaleBase, .25);
            }
        }
        private static void NormalizePercents(IItemChance[] items)
        {
            double ratio = items.Sum(o => o.ProbabilityPercent);
            ratio = 1d / ratio;

            foreach (IItemChance item in items)
            {
                item.ProbabilityPercent *= ratio;
            }
        }

        private static ShipPartDNA GetRandomPart(PartCreateChance part)
        {
            ShipPartDNA retVal = null;
            switch (part.PartType)
            {
                case Thruster.PARTTYPE:
                    #region Thruster

                    ThrusterDNA dnaThrust = new ThrusterDNA()
                    {
                        PartType = part.PartType,
                        Orientation = Math3D.GetRandomRotation(),
                        Position = new Point3D(),
                        Scale = GetRandomScale(part.ScaleBase),
                    };

                    dnaThrust.ThrusterType = UtilityCore.GetRandomEnum<ThrusterType>(new[] { ThrusterType.Two_Two_One, ThrusterType.Two_Two_Two });     // only want 2D thrusters
                    if (dnaThrust.ThrusterType == ThrusterType.Custom)
                    {
                        dnaThrust.ThrusterDirections = Enumerable.Range(0, StaticRandom.Next(2, 5)).
                            Select(o => Math3D.GetRandomVector_Circular_Shell(1)).      // only want 2D thrusters
                            ToArray();
                    }

                    retVal = dnaThrust;

                    #endregion
                    break;

                case ConverterRadiationToEnergy.PARTTYPE:
                    #region ConverterRadiationToEnergy

                    ConverterRadiationToEnergyDNA dnaCon = new ConverterRadiationToEnergyDNA()
                    {
                        PartType = part.PartType,
                        Orientation = Math3D.GetRandomRotation(),
                        Position = new Point3D(),
                        Scale = GetRandomScale(part.ScaleBase),

                        Shape = UtilityCore.GetRandomEnum<SolarPanelShape>(),
                    };

                    retVal = dnaCon;

                    #endregion
                    break;

                default:
                    #region default

                    retVal = new ShipPartDNA()
                    {
                        PartType = part.PartType,
                        Orientation = Math3D.GetRandomRotation(),
                        Position = new Point3D(),
                        Scale = GetRandomScale(part.ScaleBase),
                    };

                    #endregion
                    break;
            }

            return retVal;
        }

        private static T GetRandomItemChance<T>(T[] items) where T : IItemChance
        {
            double randValue = StaticRandom.NextDouble();

            double sum = 0;

            foreach (T item in items)
            {
                sum += item.ProbabilityPercent;

                if (sum > randValue)
                {
                    return item;
                }
            }

            if (sum.IsNearValue(1))
            {
                return items[items.Length - 1];
            }

            throw new ApplicationException("Couldn't pick a random item.  The percents don't add to one: " + sum.ToString());
        }

        private static Vector3D GetRandomScale(double baseScale, double percent = .5)
        {
            Random rand = StaticRandom.GetRandomForThread();

            //NOTE: The parts that need the same values for X,Y,Z will take averages, so it's ok to be lazy here (they are designed to take imperfect mutated inputs)
            return new Vector3D(
                rand.NextPercent(baseScale, percent),
                rand.NextPercent(baseScale, percent),
                rand.NextPercent(baseScale, percent)
                );
        }

        private void RandomizePrices()
        {
            Random rand = StaticRandom.GetRandomForThread();

            RandomBellArgs bell = new RandomBellArgs(1, -45, 1, -45);

            _partPriceAdjustments = BotConstructor.AllPartTypes.Value.
                Select(o => Tuple.Create(o, RandomizePrices_Percent(rand, bell))).
                ToArray();

            _mineralPriceAdjustments = UtilityCore.GetEnums<MineralType>().
                Select(o => Tuple.Create(o, RandomizePrices_Percent(rand, bell))).
                ToArray();
        }
        private static decimal RandomizePrices_Percent(Random rand, RandomBellArgs bell)
        {
            const double MAXVALUE = 4;      // output goes form 1/MAX to MAX

            double retVal = rand.NextBellPercent(bell, MAXVALUE);

            return Convert.ToDecimal(retVal);
        }

        private decimal FindPriceMult(string partType)
        {
            if (_partPriceAdjustments == null)
            {
                return 1m;
            }

            foreach (var item in _partPriceAdjustments)
            {
                if (item.Item1 == partType)
                {
                    return item.Item2;
                }
            }

            // It wasn't found.  Just return a multiplier of 1
            return 1m;
        }
        private decimal FindPriceMult(MineralType mineralType)
        {
            if (_mineralPriceAdjustments == null)
            {
                return 1m;
            }

            foreach (var item in _mineralPriceAdjustments)
            {
                if (item.Item1 == mineralType)
                {
                    return item.Item2;
                }
            }

            // It wasn't found.  Just return a multiplier of 1
            return 1m;
        }

        #endregion
    }

    #region class: SpaceStation2DDNA

    /// <summary>
    /// This gets serialized to file
    /// </summary>
    public class SpaceStation2DDNA
    {
        public Point3D Position { get; set; }

        public FlagProps Flag { get; set; }

        public int PurchasedVolume { get; set; }
        public InventoryDNA[] PlayerInventory { get; set; }
    }

    #endregion
}
