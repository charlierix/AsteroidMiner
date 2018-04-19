using Game.HelperClassesAI;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.GameItems.ShipParts;
using Game.Newt.v2.NewtonDynamics;
using SharpNeat.Core;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.EvolutionAlgorithms.ComplexityRegulation;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Game.Newt.v2.Arcanorum.ArenaTester
{
    //TODO: TrainingSession and ArenaAccessor should be combined?  too many thin layers, too hard to keep track of the flow of logic

    public class TrainingSession : IDisposable
    {
        #region Declaration Section

        public const int ROOMCOUNT = 25;
        public const double ROOMSIZE = 100;
        public const double ROOMMARGIN = ROOMSIZE / 10;

        private static Lazy<ExperimentInitArgs_Activation> _activationFunctionArgs = new Lazy<ExperimentInitArgs_Activation>(() =>
        new ExperimentInitArgs_Activation_CyclicFixedTimesteps()
        {
            TimestepsPerActivation = 3,
            FastFlag = true
        });

        #endregion

        #region Constructor

        public TrainingSession(ShipDNA dna, ShipExtraArgs shipExtraArgs, int inputCount, int outputCount, Func<WorldAccessor, TrainingRoom, IPhenomeTickEvaluator<IBlackBox, NeatGenome>> getNewEvaluator)
        {
            DNA = dna;
            ShipExtraArgs = shipExtraArgs;

            Arena = new ArenaAccessor(ROOMCOUNT, ROOMSIZE, ROOMMARGIN, false, false, new Type[] { typeof(Bot) }, new Type[] { typeof(Bot) }, shipExtraArgs.NeuralPoolManual, (.1, .25));
            Arena.WorldCreated += Arena_WorldCreated;

            foreach (var (room, _) in Arena.AllRooms)
            {
                room.Evaluator = getNewEvaluator(Arena.WorldAccessor, room);
            }

            #region experiment args

            ExperimentInitArgs experimentArgs = new ExperimentInitArgs()
            {
                Description = "Trains an individual BrainNEAT part",
                InputCount = inputCount,
                OutputCount = outputCount,
                IsHyperNEAT = false,
                PopulationSize = ROOMCOUNT,        // may want to do a few more than rooms in case extra are made
                SpeciesCount = Math.Max(2, (ROOMCOUNT / 4d).ToInt_Ceiling()),
                Activation = GetActivationFunctionArgs(),
                Complexity_RegulationStrategy = ComplexityCeilingType.Absolute,
                Complexity_Threshold = 200,
            };

            #endregion

            Experiment = new ExperimentNEATBase();

            // Use the overload that takes the tick phenome
            Experiment.Initialize("BrainNEAT Trainer", experimentArgs, Arena.AllRooms.Select(o => o.room.Evaluator).ToArray(), Arena.WorldAccessor.RoundRobinManager, Arena.WorldAccessor.UpdateWorld);

            EA = Experiment.CreateEvolutionAlgorithm();

            // look in AnticipatePositionWindow for an example.  The update event just displays stats
            //ea.UpdateEvent += EA_UpdateEvent;
            //ea.PausedEvent += EA_PausedEvent;
        }

        #endregion

        #region IDisposable Support

        private bool _disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).

                    if (EA != null)
                    {
                        EA.Stop();
                        EA = null;
                    }

                    if (Arena != null)
                    {
                        Arena.Dispose();
                        Arena = null;
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                _disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~TrainingSessionBase() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        #endregion

        #region Public Properties

        public ArenaAccessor Arena { get; private set; }

        public ShipDNA DNA { get; private set; }
        public ShipExtraArgs ShipExtraArgs { get; private set; }

        public ExperimentNEATBase Experiment { get; private set; }
        public NeatEvolutionAlgorithm<NeatGenome> EA { get; set; }

        public MaterialManager MaterialManager { get; private set; }
        public MaterialIDs MaterialIDs { get; private set; }

        public KeepItems2D[] Keep2D { get; private set; }

        #endregion

        #region Public Methods

        public static ExperimentInitArgs_Activation GetActivationFunctionArgs()
        {
            return _activationFunctionArgs.Value;
        }

        #endregion

        #region Event Listeners

        /// <summary>
        /// Once the world is created on the worker thread, maps and bots need to be created for each room
        /// NOTE: This is called from the worker thread
        /// </summary>
        private void Arena_WorldCreated(object sender, EventArgs e)
        {
            World world = Arena.WorldAccessor.World;

            #region materials

            MaterialManager = new MaterialManager(world);
            MaterialIDs = new MaterialIDs();

            // Wall
            Game.Newt.v2.NewtonDynamics.Material material = new Game.Newt.v2.NewtonDynamics.Material();
            material.Elasticity = ItemOptionsArco.ELASTICITY_WALL;
            MaterialIDs.Wall = MaterialManager.AddMaterial(material);

            // Bot
            material = new Game.Newt.v2.NewtonDynamics.Material();
            MaterialIDs.Bot = MaterialManager.AddMaterial(material);

            // Bot Ram
            material = new Game.Newt.v2.NewtonDynamics.Material();
            material.Elasticity = ItemOptionsArco.ELASTICITY_BOTRAM;
            MaterialIDs.BotRam = MaterialManager.AddMaterial(material);

            // Exploding Bot
            material = new Game.Newt.v2.NewtonDynamics.Material();
            material.IsCollidable = false;
            MaterialIDs.ExplodingBot = MaterialManager.AddMaterial(material);

            // Weapon
            material = new Game.Newt.v2.NewtonDynamics.Material();
            MaterialIDs.Weapon = MaterialManager.AddMaterial(material);

            // Treasure Box
            material = new Game.Newt.v2.NewtonDynamics.Material();
            MaterialIDs.TreasureBox = MaterialManager.AddMaterial(material);

            //TODO: Uncomment these
            // Collisions
            //_materialManager.RegisterCollisionEvent(_materialIDs.Bot, _materialIDs.Bot, Collision_BotBot);
            //_materialManager.RegisterCollisionEvent(_materialIDs.Bot, _materialIDs.Weapon, Collision_BotWeapon);
            //_materialManager.RegisterCollisionEvent(_materialIDs.Weapon, _materialIDs.Weapon, Collision_WeaponWeapon);
            ////_materialManager.RegisterCollisionEvent(_materialIDs.Bot, _materialIDs.Wall, Collision_BotWall);
            //_materialManager.RegisterCollisionEvent(_materialIDs.Weapon, _materialIDs.Wall, Collision_WeaponWall);
            //_materialManager.RegisterCollisionEvent(_materialIDs.Weapon, _materialIDs.TreasureBox, Collision_WeaponTreasureBox);
            //_materialManager.RegisterCollisionEvent(_materialIDs.Bot, _materialIDs.TreasureBox, Collision_BotTreasureBox);

            #endregion

            List<KeepItems2D> keep2Ds = new List<KeepItems2D>();

            foreach (var (room, _) in Arena.AllRooms)
            {
                #region bot

                ShipCoreArgs core = new ShipCoreArgs()
                {
                    World = world,
                    Material_Ship = MaterialIDs.Bot,
                    Map = room.Map,
                };

                //BotConstructor_Events events = new BotConstructor_Events();
                //events.

                // Create the bot
                BotConstruction_Result construction = BotConstructor.ConstructBot(DNA, core, ShipExtraArgs);
                Bot bot = new Bot(construction);

                // Find some parts
                BrainNEAT brainPart = bot.Parts.FirstOrDefault(o => o is BrainNEAT) as BrainNEAT;
                if (brainPart == null)
                {
                    throw new ApplicationException("Didn't find BrainNEAT part");
                }

                SensorHoming[] homingParts = bot.Parts.
                    Where(o => o is SensorHoming).
                    Select(o => (SensorHoming)o).
                    ToArray();

                if (homingParts.Length == 0)
                {
                    throw new ApplicationException("Didn't find SensorHoming part");
                }

                #endregion

                room.Bot = bot;
                room.BrainPart = brainPart;
                room.HomingParts = homingParts;

                foreach(SensorHoming homing in homingParts)
                {
                    homing.HomePoint = room.Center;
                    homing.HomeRadius = ROOMSIZE / 2d;
                }

                room.Map.AddItem(bot);

                #region keep 2D

                //TODO: drag plane should either be a plane or a large cylinder, based on the current (level|scene|stage|area|arena|map|place|region|zone)

                // This game is 3D emulating 2D, so always have the mouse go to the XY plane
                DragHitShape dragPlane = new DragHitShape();
                dragPlane.SetShape_Plane(new Triangle(new Point3D(-1, -1, room.Center.Z), new Point3D(1, -1, room.Center.Z), new Point3D(0, 1, room.Center.Z)));

                // This will keep objects onto that plane using forces (not velocities)
                KeepItems2D keep2D = new KeepItems2D
                {
                    SnapShape = dragPlane,
                };

                keep2D.Add(room.Bot, false);
                keep2Ds.Add(keep2D);

                #endregion
            }

            Keep2D = keep2Ds.ToArray();

            world.Updated += World_Updated;
        }

        private void World_Updated(object sender, WorldUpdatingArgs e)
        {
            foreach (KeepItems2D keep2D in Keep2D)
            {
                keep2D.Update();
            }
        }

        #endregion
    }
}
