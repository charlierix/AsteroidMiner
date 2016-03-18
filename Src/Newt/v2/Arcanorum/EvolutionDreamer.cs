using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.GameItems.ShipParts;
using Game.HelperClassesWPF;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.Arcanorum
{
    //TODO: Don't need lots of small worlds.  Just make this one bigger with little islands
    //TODO: Bot.AttachWeapon is directly populating viewport.  Make my own viewport class.  It should be responsible for creating model visuals.  That way, if an offline camera gets attached arbitrarily, it can get all visuals
    //  AddModel(Model3D, isMainOnly)
    //  RemoveModel(Model3D)
    //  AddCamera/RemoveCamera
    public class EvolutionDreamer : IDisposable
    {
        #region Class: TrackedBot

        private class TrackedBot
        {
            public TrackedBot(object parentLock)
            {
                _parentLock = parentLock;
            }

            // This is managed outside the lock
            public volatile FitnessTracker Rules = null;

            // All variables below are managed by this lock
            //public readonly object Lock = new object();     //TODO: May also want a separate lock here (but that's a lot of locks each tick)

            private readonly object _parentLock;

            public BotState State = BotState.None;

            public ArcBotNPC Bot = null;

            public double Lifespan = 45d;       // seconds

            #region Public Methods

            public void BotAdded(IMapObject item)
            {
                lock (_parentLock)
                {
                    this.Bot = (ArcBotNPC)item;
                    this.Rules = GetRules(this.Bot);

                    this.State = BotState.Added;
                }
            }
            public void BotRemoved()
            {
                lock (_parentLock)
                {
                    this.Bot.Dispose();
                    this.Bot = null;
                    // _rules was set to null immediately

                    this.State = BotState.None;
                }
            }

            #endregion

            #region Private Methods

            private FitnessTracker GetRules(ArcBot bot = null)
            {
                //TODO: Base this on a tempate that was passed into the constructor

                List<Tuple<IFitnessRule, double>> rules = new List<Tuple<IFitnessRule, double>>();

                rules.Add(new Tuple<IFitnessRule, double>(new FitnessRule_TooFar(bot, new Point3D(0, 0, 0), HOMINGRADIUS, HOMINGRADIUS * 2d), 1d));
                rules.Add(new Tuple<IFitnessRule, double>(new FitnessRule_TooStill(bot, .1, 1), 1d));
                rules.Add(new Tuple<IFitnessRule, double>(new FitnessRule_TooTwitchy(bot, -.8), 1d));

                return new FitnessTracker(bot, rules.ToArray());
            }

            #endregion
        }

        #endregion

        #region Enum: BotState

        private enum BotState
        {
            None,
            Adding,
            Added,
            Removing
        }

        #endregion

        #region Declaration Section

        private const double BOUNDRYSIZE = 50;
        private const double HOMINGRADIUS = 8d;

        private volatile bool _isDisposed = false;

        private readonly BotShellColorsDNA _shellColors;

        private readonly OfflineWorld _dreamWorld;

        private readonly MutateUtility.NeuronMutateArgs _mutateArgs;

        /// <summary>
        /// This ticks at a slower rate, and takes care of populating/resetting the arena
        /// </summary>
        private readonly System.Timers.Timer _timer;
        /// <summary>
        /// This one ticks at a faster rate, and just runs the fitness rules
        /// </summary>
        private readonly System.Timers.Timer _ruleTimer;

        /// <summary>
        /// Intead of calculating the elapsed time each tick, just approximate with the same value
        /// </summary>
        private readonly double _ruleElapsed;

        //TODO: Allow a variable number of bots to be tracked
        private readonly TrackedBot[] _trackedBots;

        private readonly object _lock = new object();

        private volatile BotDNA _winningBot = null;     // making this volatile so it can be read without a lock
        private double _winningScore = 0d;

        #endregion

        #region Constructor

        public EvolutionDreamer(ItemOptionsArco itemOptions, BotShellColorsDNA shellColors, int numTrackedBots)
        {
            _shellColors = shellColors;

            _mutateArgs = GetMutateArgs(itemOptions);

            _dreamWorld = new OfflineWorld(BOUNDRYSIZE, itemOptions);

            _trackedBots = new TrackedBot[numTrackedBots];
            for (int cntr = 0; cntr < numTrackedBots; cntr++)
            {
                _trackedBots[cntr] = new TrackedBot(_lock);
            }

            _timer = new System.Timers.Timer();
            _timer.AutoReset = false;
            _timer.Interval = 250;
            _timer.Elapsed += Timer_Elapsed;
            _timer.Start();

            _ruleTimer = new System.Timers.Timer();
            _ruleTimer.AutoReset = false;
            _ruleTimer.Interval = 50;
            _ruleTimer.Elapsed += RuleTimer_Elapsed;
            _ruleTimer.Start();

            _ruleElapsed = _ruleTimer.Interval / 1000d;
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (_lock)        // this makes sure the standard timer isn't mid tick
                {
                    _isDisposed = true;

                    foreach (TrackedBot tracked in _trackedBots)
                    {
                        tracked.Rules = null;      // this will decrease the chance of the rule timer doing anything while in transition
                    }
                }

                _ruleTimer.Stop();
                _timer.Stop();

                _dreamWorld.Dispose();
            }
        }

        #endregion

        #region Public Properties

        private volatile WeaponDNA _weaponDNA = null;
        /// <summary>
        /// This is the weapon that should be used for all bots generated by this class
        /// </summary>
        /// <remarks>
        /// As the nest improves, it might use upgraded weapons - or maybe start out null until one of its bots finds a weapon, etc
        /// </remarks>
        public WeaponDNA WeaponDNA
        {
            get
            {
                return _weaponDNA;
            }
            set
            {
                _weaponDNA = value;
            }
        }

        #endregion

        #region Public Methods

        public Tuple<BotDNA, WeaponDNA> GetBestBotDNA()
        {
            BotDNA winner = _winningBot;

            if (winner == null)
            {
                return GetRandomDNA(_shellColors, this.WeaponDNA);
            }
            else
            {
                return Tuple.Create(winner, this.WeaponDNA);
            }
        }

        public static Tuple<BotDNA, WeaponDNA> GetRandomDNA(BotShellColorsDNA shellColors = null, WeaponDNA weapon = null)
        {
            Random rand = StaticRandom.GetRandomForThread();

            BotDNA bot = new BotDNA()
            {
                UniqueID = Guid.NewGuid(),
                Lineage = Guid.NewGuid().ToString(),
                Generation = 0,
                DraggingMaxVelocity = rand.NextPercent(5, .25),
                DraggingMultiplier = rand.NextPercent(20, .25),
            };

            #region Parts

            List<ShipPartDNA> parts = new List<ShipPartDNA>();

            double partSize;

            // Homing
            partSize = rand.NextPercent(1, .5);
            parts.Add(new ShipPartDNA() { PartType = SensorHoming.PARTTYPE, Position = new Point3D(0, 0, 2.5), Orientation = Quaternion.Identity, Scale = new Vector3D(partSize, partSize, partSize) });

            // Vision
            //TODO: Support filtering by type
            partSize = rand.NextPercent(1, .5);
            parts.Add(new ShipPartDNA() { PartType = SensorVision.PARTTYPE, Position = new Point3D(0, 0, 1.5), Orientation = Quaternion.Identity, Scale = new Vector3D(partSize, partSize, partSize) });

            // Brains
            int numBrains = 1 + Convert.ToInt32(rand.NextPow(5, 1d) * 4);

            for (int cntr = 0; cntr < numBrains; cntr++)
            {
                partSize = rand.NextPercent(1, .5);

                Point3D position = new Point3D(0, 0, 0);
                if (numBrains > 1)
                {
                    position = Math3D.GetRandomVector_Circular(1).ToPoint();
                }

                parts.Add(new ShipPartDNA() { PartType = Brain.PARTTYPE, Position = position, Orientation = Quaternion.Identity, Scale = new Vector3D(partSize, partSize, partSize) });
            }

            // MotionController_Linear - always exactly one of these
            partSize = rand.NextPercent(1, .5);
            parts.Add(new ShipPartDNA() { PartType = MotionController_Linear.PARTTYPE, Position = new Point3D(0, 0, -1.5), Orientation = Quaternion.Identity, Scale = new Vector3D(partSize, partSize, partSize) });

            // Store it
            bot.Parts = parts.ToArray();

            #endregion

            if (shellColors == null)
            {
                bot.ShellColors = BotShellColorsDNA.GetRandomColors();
            }
            else
            {
                bot.ShellColors = shellColors;
            }

            #region Weapon

            WeaponDNA weaponActual = null;
            if (weapon == null)
            {
                if (rand.NextDouble() < .95d)
                {
                    WeaponHandleMaterial[] weaponMaterials = new WeaponHandleMaterial[] { WeaponHandleMaterial.Soft_Wood, WeaponHandleMaterial.Hard_Wood };

                    weaponActual = new WeaponDNA()
                    {
                        UniqueID = Guid.NewGuid(),
                        Handle = WeaponHandleDNA.GetRandomDNA(weaponMaterials[StaticRandom.Next(weaponMaterials.Length)])
                    };
                }
            }
            else
            {
                weaponActual = weapon;
            }

            #endregion

            return Tuple.Create(bot, weaponActual);
        }

        #endregion

        #region Event Listeners

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            const double LIFESPAN = 45;     // seconds
            const double MINSCORE = .5;
            const int LEVEL = 1;

            lock (_lock)
            {
                if (_isDisposed)
                {
                    return;
                }

                foreach (TrackedBot tracked in _trackedBots)
                {
                    switch (tracked.State)
                    {
                        case BotState.Adding:
                        case BotState.Removing:
                            break;

                        case BotState.None:
                            #region None

                            // Create a bot

                            BotDNA dna = null;
                            BotDNA winningBot = _winningBot;
                            if (winningBot == null)
                            {
                                dna = GetRandomDNA(_shellColors).Item1;
                            }
                            else
                            {
                                // Create a mutated copy of the winning design
                                dna = UtilityCore.Clone(winningBot);
                                dna.UniqueID = Guid.NewGuid();
                                dna.Generation++;

                                if (dna.Parts != null)
                                {
                                    MutateUtility.Mutate(dna.Parts, _mutateArgs);
                                }
                            }

                            tracked.State = BotState.Adding;

                            tracked.Lifespan = StaticRandom.NextPercent(LIFESPAN, .1);      // randomizing the lifespan a bit to stagger when bots get killed/created

                            Point3D position = Math3D.GetRandomVector_Circular(HOMINGRADIUS / 4d).ToPoint();
                            _dreamWorld.Add(new OfflineWorld.AddBotArgs(position, null, tracked.BotAdded, dna, LEVEL, new Point3D(0, 0, 0), HOMINGRADIUS, this.WeaponDNA));

                            #endregion
                            break;

                        case BotState.Added:
                            #region Added

                            // See if this should die
                            double age = (DateTime.UtcNow - tracked.Bot.CreationTime).TotalSeconds;
                            if (age > tracked.Lifespan)
                            {
                                FitnessTracker rules = tracked.Rules;
                                tracked.Rules = null;      // set it to null as soon as possible so that the rules tick doesn't do unnecessary work

                                double score = rules.Score / age;

                                if (score > MINSCORE && score > _winningScore)
                                {
                                    BotDNA winningDNA = tracked.Bot.DNAFinal;

                                    if (winningDNA != null)     // it should be non null long before this point, but just make sure
                                    {
                                        _winningScore = score;
                                        _winningBot = winningDNA;
                                    }
                                }

                                // Kill it
                                tracked.State = BotState.Removing;
                                _dreamWorld.Remove(new OfflineWorld.RemoveArgs(tracked.Bot.Token, tracked.BotRemoved));
                            }

                            #endregion
                            break;
                    }

                    _timer.Start();
                }
            }
        }

        private void RuleTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (_isDisposed)
            {
                return;
            }

            foreach (TrackedBot tracked in _trackedBots)
            {
                FitnessTracker rules = tracked.Rules;
                if (rules != null)
                {
                    rules.Update_AnyThread(_ruleElapsed);
                }
            }

            _ruleTimer.Start();
        }

        #endregion

        #region Private Methods

        //TODO: Make a random version
        //private static FitnessTracker GetRandomRules(Bot bot = null)

        private static MutateUtility.NeuronMutateArgs GetMutateArgs(ItemOptionsArco options)
        {
            MutateUtility.NeuronMutateArgs retVal = null;

            MutateUtility.MuateArgs neuronMovement = new MutateUtility.MuateArgs(false, options.NeuronPercentToMutate, null, null,
                new MutateUtility.MuateFactorArgs(MutateUtility.FactorType.Distance, options.NeuronMovementAmount));		// neurons are all point3D (positions need to drift around freely.  percent doesn't make much sense)

            MutateUtility.MuateArgs linkMovement = new MutateUtility.MuateArgs(false, options.LinkPercentToMutate,
                new Tuple<string, MutateUtility.MuateFactorArgs>[]
					{
						Tuple.Create("FromContainerPosition", new MutateUtility.MuateFactorArgs(MutateUtility.FactorType.Distance, options.LinkContainerMovementAmount)),
						Tuple.Create("FromContainerOrientation", new MutateUtility.MuateFactorArgs(MutateUtility.FactorType.Percent, options.LinkContainerRotateAmount))
					},
                new Tuple<PropsByPercent.DataType, MutateUtility.MuateFactorArgs>[]
					{
						Tuple.Create(PropsByPercent.DataType.Double, new MutateUtility.MuateFactorArgs(MutateUtility.FactorType.Distance, options.LinkWeightAmount)),		// all the doubles are weights, which need to be able to cross over zero (percents can't go + to -)
						Tuple.Create(PropsByPercent.DataType.Point3D, new MutateUtility.MuateFactorArgs(MutateUtility.FactorType.Distance, options.LinkMovementAmount)),		// using a larger value for the links
					},
                null);

            retVal = new MutateUtility.NeuronMutateArgs(neuronMovement, null, linkMovement, null);

            return retVal;
        }

        #endregion
    }

    #region Class: WinnerList

    public class WinnerList
    {
    }

    #endregion
}
