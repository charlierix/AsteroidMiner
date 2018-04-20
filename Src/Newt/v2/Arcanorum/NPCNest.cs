using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.GameItems.ShipParts;
using Game.HelperClassesWPF;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.Arcanorum
{
    //TODO: Calculate radius based on DNA scale
    //TODO: Represent the nest's level visually (based on how accomplished/evolved the bots are)

    /// <summary>
    /// This is home to a family of NPC bots.  It has limits on how many bots can be alive at the same time (and can kill any of its
    /// bots that fail rules - like straying too far from the nest)
    /// </summary>
    /// <remarks>
    /// I didn't want to have the map filled with a bunch of single random bots.  Instead I want nests, each family would be variants
    /// of the same dna.  So all members of a family will act roughly the same way.  (could still have a few lone bots roaming around,
    /// make them stronger than average, but most should be a part of nests)
    /// 
    /// This will also try to improve the bots by training them in offline threads (not visible from the main game).  Then only winning
    /// designs will be kept and spawned in the actual game
    /// 
    /// I don't want the nest itself to be a physical object in the game, just a visual.  But to kill a nest, make some rules like kill all the
    /// family members, and all support buildings.
    /// </remarks>
    public class NPCNest : IDisposable, IMapObject, IPartUpdatable
    {
        #region Declaration Section

        private readonly TaskScheduler _mainThread = TaskScheduler.FromCurrentSynchronizationContext();

        private readonly World _world;
        private readonly Map _map;
        private readonly KeepItems2D _keepItems2D;
        private readonly MaterialIDs _materialIDs;
        private readonly Viewport3D _viewport;
        private readonly EditorOptions _editorOptions;
        private readonly ItemOptionsArco _itemOptions;
        private readonly IGravityField _gravity;
        private readonly DragHitShape _dragPlane;

        private readonly EvolutionDreamer _dreamer;

        private readonly BotShellColorsDNA _shellColors;
        //TODO: Don't just have one.  Keep candidates, and try to evolve better ones
        private readonly BotDNA _botDNA;
        private readonly WeaponDNA _weaponDNA;

        private double _time = 0d;

        private readonly Container _energy;

        private readonly Model3DGroup _eggModels;
        private readonly List<Tuple<GeometryModel3D, Point3D, double>> _eggs = new List<Tuple<GeometryModel3D, Point3D, double>>();
        // There isn't much point in limiting by time and energy.  Just let energy be the limit
        //private double _newEggCountdown = -1;       // when this gets to zero, a new egg can be created

        private readonly List<long> _botTokens = new List<long>();

        #endregion

        #region Constructor

        public NPCNest(NPCNestDNA dna, double radius, World world, Map map, KeepItems2D keepItems2D, MaterialIDs materialIDs, Viewport3D viewport, EditorOptions editorOptions, ItemOptionsArco itemOptions, IGravityField gravity, DragHitShape dragPlane)
        {
            // Store stuff
            _world = world;
            _map = map;
            _keepItems2D = keepItems2D;
            _materialIDs = materialIDs;
            _viewport = viewport;
            _editorOptions = editorOptions;
            _itemOptions = itemOptions;
            _gravity = gravity;
            _dragPlane = dragPlane;

            // DNA
            NPCNestDNA fixedDNA = GetFinalDNA(dna);
            _shellColors = fixedDNA.ShellColors;
            _botDNA = fixedDNA.BotDNA;
            _weaponDNA = fixedDNA.WeaponDNA;

            //TODO: Hand this a winner list, and filter criteria
            _dreamer = new EvolutionDreamer(_itemOptions, _shellColors, 4);     //TODO: Num bots should come from item options
            _dreamer.WeaponDNA = EvolutionDreamer.GetRandomDNA().Item2;

            #region WPF Model

            var models = GetModel(_shellColors, radius);
            this.Model = models.Item1;
            _eggModels = models.Item2;

            _rotateTransform = new QuaternionRotation3D();
            _translateTransform = new TranslateTransform3D();

            Transform3DGroup transform = new Transform3DGroup();
            transform.Children.Add(new RotateTransform3D(_rotateTransform));
            transform.Children.Add(_translateTransform);

            ModelVisual3D visual = new ModelVisual3D();
            visual.Transform = transform;
            visual.Content = this.Model;

            this.Visuals3D = new Visual3D[] { visual };

            #endregion

            // Energy tank
            _energy = new Container();
            _energy.QuantityMax = _itemOptions.Nest_Energy_Max * radius;
            _energy.QuantityCurrent = _energy.QuantityMax * .5d;

            // Finish
            this.Token = TokenGenerator.NextToken();
            this.Radius = radius;
            this.CreationTime = DateTime.UtcNow;
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
                _isDisposed = true;

                _dreamer.Dispose();

                //this.PhysicsBody.Dispose();       // the nest doesn't have a body
            }
        }

        #endregion
        #region IMapObject Members

        public long Token
        {
            get;
            private set;
        }

        private volatile bool _isDisposed = false;
        public bool IsDisposed
        {
            get
            {
                return _isDisposed;// || this.PhysicsBody.IsDisposed;       // the nest doesn't have a body
            }
        }

        public Body PhysicsBody
        {
            get
            {
                return null;
            }
        }

        public Visual3D[] Visuals3D
        {
            get;
            private set;
        }
        public Model3D Model
        {
            get;
            private set;
        }

        private Point3D _positionWorld;     // storing this off, because the transform isn't threadsafe
        private TranslateTransform3D _translateTransform = null;
        public Point3D PositionWorld
        {
            get
            {
                //return new Point3D(_translateTransform.OffsetX, _translateTransform.OffsetY, _translateTransform.OffsetZ);
                return _positionWorld;
            }
            set
            {
                Task.Factory.StartNew(() =>
                {
                    // The transform must be set in the same thread that created it
                    _translateTransform.OffsetX = value.X;
                    _translateTransform.OffsetY = value.Y;
                    _translateTransform.OffsetZ = value.Z;
                }, CancellationToken.None, TaskCreationOptions.None, _mainThread).Wait();

                _positionWorld = value;
            }
        }

        private Quaternion _rotationWorld;      // the transform isn't threadsafe
        private QuaternionRotation3D _rotateTransform = null;
        public Quaternion RotationWorld
        {
            get
            {
                //return _rotateTransform.Quaternion;
                return _rotationWorld;
            }
            set
            {
                Task.Factory.StartNew(() =>
                {
                    // The transform must be set in the same thread that created it
                    _rotateTransform.Quaternion = value;
                }, CancellationToken.None, TaskCreationOptions.None, _mainThread).Wait();

                _rotationWorld = value;
            }
        }

        public Vector3D VelocityWorld
        {
            get
            {
                return new Vector3D(0, 0, 0);
            }
        }
        public Matrix3D OffsetMatrix
        {
            get
            {
                throw new InvalidOperationException("This class doesn't have a physics body");
            }
        }

        public double Radius
        {
            get;
            private set;
        }

        public DateTime CreationTime
        {
            get;
            private set;
        }

        public int CompareTo(IMapObject other)
        {
            return MapObjectUtil.CompareToT(this, other);
        }

        public bool Equals(IMapObject other)
        {
            return MapObjectUtil.EqualsT(this, other);
        }
        public override bool Equals(object obj)
        {
            return MapObjectUtil.EqualsObj(this, obj);
        }

        public override int GetHashCode()
        {
            return MapObjectUtil.GetHashCode(this);
        }

        #endregion
        #region IPartUpdatable Members

        //TODO: Move all but graphics to the any thread
        public void Update_MainThread(double elapsedTime)
        {
            _time += elapsedTime;

            _energy.AddQuantity(_itemOptions.Nest_Energy_Add * elapsedTime, false);

            // Get the currently live bots
            ArcBotNPC[] bots;
            if (_botTokens.Count == 0)
            {
                bots = new ArcBotNPC[0];
            }
            else
            {
                bots = _map.GetItems<ArcBotNPC>(false).Where(o => _botTokens.Contains(o.Token)).ToArray();
            }

            // Get rid of tokens that point to dead bots
            foreach (long token in _botTokens.Where(o => !bots.Any(p => p.Token == o)).ToArray())
            {
                _botTokens.Remove(token);
            }

            // Apply damage to existing bots
            bots = Update_DamageBots(bots);

            // Level up existing bots

            // Hatch a new bot
            Update_AddBot(bots);

            // Add egg
            Update_AddEgg(elapsedTime);
        }
        public void Update_AnyThread(double elapsedTime)
        {
        }

        public int? IntervalSkips_MainThread
        {
            get
            {
                return 5;
            }
        }
        public int? IntervalSkips_AnyThread
        {
            get
            {
                return null;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// As this nest runs, it is continually trying to evolve the bots.  So the dna may be different each time this is called (but some parts of the
        /// dna will stay the same, like color)
        /// </summary>
        public NPCNestDNA GetDNA()
        {
            var dna = _dreamer.GetBestBotDNA();

            return new NPCNestDNA()
            {
                ShellColors = _shellColors,
                BotDNA = dna.Item1,
                WeaponDNA = dna.Item2
            };
        }

        #endregion

        #region Private Methods

        private ArcBotNPC[] Update_DamageBots(ArcBotNPC[] bots)
        {
            if (bots.Length == 0)
            {
                return bots;
            }

            List<ArcBotNPC> retVal = new List<ArcBotNPC>();

            Point3D nestPosition = this.PositionWorld;

            double maxDistanceSq = Math.Pow(this.Radius * 14, 2);

            foreach (ArcBotNPC bot in bots)
            {
                //TODO: Add more rules
                if ((bot.PositionWorld - nestPosition).LengthSquared > maxDistanceSq)
                {
                    _map.RemoveItem(bot, true);
                    bot.Dispose();
                }
                else
                {
                    retVal.Add(bot);
                }
            }

            return retVal.ToArray();
        }

        private void Update_AddBot(ArcBotNPC[] currentBots)
        {
            const double HOMINGRADIUS = 8d;
            //const int LEVEL = 1;

            if (currentBots.Length >= 2)       //TODO: Get this from DNA
            {
                // There are enough bots
                return;
            }

            if (_eggs.Count == 0)
            {
                return;
            }
            else if (_eggs[0].Item3 > _time - 4)        // item 0 will always be the oldest egg.  Time is in seconds
            {
                // The egg hasn't been sitting long enough to hatch
                return;
            }

            // Remove the egg
            _eggModels.Children.Remove(_eggs[0].Item1);
            _eggs.RemoveAt(0);

            // Create a bot
            Tuple<BotDNA, WeaponDNA> dna = _dreamer.GetBestBotDNA();

            Point3D? position = _dragPlane.CastRay(this.PositionWorld + Math3D.GetRandomVector_Circular(this.Radius * 3));
            if (position == null)
            {
                throw new ApplicationException("Drag plane couldn't find the nearest point");
            }

            int level = StaticRandom.Next(1, 20);
            ArcBotNPC bot = new ArcBotNPC(dna.Item1, level, position.Value, _world, _map, _keepItems2D, _materialIDs, _viewport, _editorOptions, _itemOptions, _gravity, _dragPlane, this.PositionWorld, HOMINGRADIUS, true, true);

            _map.AddItem(bot);
            _botTokens.Add(bot.Token);

            // Attached Weapon
            //NOTE: Doing this here, because bot.AttachWeapon plays with the viewport, so does map.  If this is done before map, it blows up.
            //if it's done after, then bot.AttachWeapon handles that properly
            if (dna.Item2 != null)
            {
                Weapon weapon = new Weapon(dna.Item2, new Point3D(0, 0, 0), _world, _materialIDs.Weapon);

                bot.AttachWeapon(weapon);
            }
        }

        //TODO: These props are very hard coded, put more in DNA.  Some nests could have a few elites, or many simple bots.
        //Also, allow the nest to grow if it captures territory, allowing for a larger number of eggs, and larger energy
        private void Update_AddEgg(double elapsedTime)
        {
            //if (_newEggCountdown > 0)       // double has a HUGE range, but I feel safer only decrementing when positive
            //{
            //    _newEggCountdown -= elapsedTime;
            //}

            if (_eggs.Count >= 4)       //TODO: Get this from DNA
            {
                // There are enough eggs
                return;
            }
            //else if (_newEggCountdown > 0)
            //{
            //    // Not ready to create a new egg
            //    return;
            //}
            else if (!Math1D.IsNearZero(_energy.RemoveQuantity(_itemOptions.Nest_Energy_Egg, true)))
            {
                // Not enough energy to create an egg
                return;
            }

            //TODO: Get the egg's size based on dna
            double eggRadius = this.Radius * .3;

            // Find a position that's not too close to the other eggs
            Point3D position = GetNewEggPosition(_eggs.Select(o => o.Item2), eggRadius, this.Radius);

            GeometryModel3D geometry = GetEggModel(position, eggRadius, _shellColors);
            _eggModels.Children.Add(geometry);

            _eggs.Add(Tuple.Create(geometry, position, _time));

            //TODO: Get this from DNA
            //_newEggCountdown = 4;       // 4 seconds
        }

        private NPCNestDNA GetFinalDNA(NPCNestDNA dna)
        {
            // Main instance
            NPCNestDNA retVal;
            if (dna == null)
            {
                retVal = new NPCNestDNA();
            }
            else
            {
                retVal = UtilityCore.Clone(dna);
            }

            // Colors
            if (retVal.ShellColors == null)
            {
                retVal.ShellColors = BotShellColorsDNA.GetRandomColors();
            }

            //if (retVal.BotDNA == null)
            //{
            //    //TODO: don't hardcode so much

            //    retVal.BotDNA = new BotDNA()
            //    {
            //        ShellColors = retVal.ShellColors,

            //        DraggingMaxVelocity = StaticRandom.NextPercent(5, .25),
            //        DraggingMultiplier = StaticRandom.NextPercent(20, .25),

            //        Parts = new PartDNA[]
            //        {
            //            new PartDNA() { PartType = SensorVision.PARTTYPE, Position = new Point3D(0, 0, 1.5), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1) },
            //            new PartDNA() { PartType = Brain.PARTTYPE, Position = new Point3D(0, 0, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1) },
            //            new PartDNA() { PartType = MotionController_Linear.PARTTYPE, Position = new Point3D(0, 0, -1.5), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1) },
            //        }
            //    };

            //    //NOTE: Letting the bot create random neural links.  Once this nest has a few winners, it won't need to be creating new random ones anymore

            //}

            //if (retVal.WeaponDNA == null)
            //{
            //    WeaponHandleMaterial[] weaponMaterials = new WeaponHandleMaterial[] { WeaponHandleMaterial.Soft_Wood, WeaponHandleMaterial.Hard_Wood };

            //    retVal.WeaponDNA = WeaponHandleDNA.GetRandomDNA(weaponMaterials[StaticRandom.Next(weaponMaterials.Length)]);
            //}

            return retVal;
        }

        private static Tuple<Model3DGroup, Model3DGroup> GetModel(BotShellColorsDNA colors, double radius)
        {
            //TODO: Maybe throw some other debris in the nest

            Model3DGroup retVal = new Model3DGroup();

            #region Light

            // Light
            Color lightColor = UtilityWPF.ColorFromHex(colors.Light);
            PointLight pointLight = new PointLight(lightColor, new Point3D(0, 0, 0));
            UtilityWPF.SetAttenuation(pointLight, radius * 2d, .1d);

            retVal.Children.Add(pointLight);

            #endregion

            #region Bowl

            //-------- outer bowl

            // Material
            MaterialGroup material = new MaterialGroup();
            material.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("8C8174"))));
            material.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("40736355")), 5d));

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = material;
            geometry.BackMaterial = material;

            List<TubeRingBase> rings = new List<TubeRingBase>();
            rings.Add(new TubeRingDome(0, false, 3));
            rings.Add(new TubeRingRegularPolygon(radius * .4, false, radius, radius, false));
            rings.Add(new TubeRingRegularPolygon(radius * .1, false, radius * .9, radius * .9, false));

            geometry.Geometry = UtilityWPF.GetMultiRingedTube(20, rings, true, true, new TranslateTransform3D(0, 0, radius * -.3));

            retVal.Children.Add(geometry);


            //-------- inner bowl

            // Material
            material = new MaterialGroup();
            material.Children.Add(new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("A6998A"))));
            material.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("80C2A78F")), 2d));

            // Geometry Model
            geometry = new GeometryModel3D();
            geometry.Material = material;
            geometry.BackMaterial = material;

            rings = new List<TubeRingBase>();
            rings.Add(new TubeRingRegularPolygon(0, false, radius * .9, radius * .9, false));
            rings.Add(new TubeRingDome(radius * -.4, false, 5));

            geometry.Geometry = UtilityWPF.GetMultiRingedTube(20, rings, true, false, new TranslateTransform3D(0, 0, (radius * .25) + (radius * -.3)));

            retVal.Children.Add(geometry);

            #endregion

            // Make a bucket to add egg visuals into
            Model3DGroup eggs = new Model3DGroup();
            retVal.Children.Add(eggs);

            return Tuple.Create(retVal, eggs);
        }
        private static Point3D GetNewEggPosition(IEnumerable<Point3D> existing, double eggRadius, double nestRadius)
        {
            Vector[] retVal = Math3D.GetRandomVectors_Circular_ClusteredMinDist(1, nestRadius * .6, eggRadius * 2 * 1.03, .03, 250, null, existing.Select(o => o.ToVector2D()).ToArray());

            return retVal[0].ToPoint3D();
        }
        private static GeometryModel3D GetEggModel(Point3D position, double radius, BotShellColorsDNA colors)
        {
            // Material
            MaterialGroup material = new MaterialGroup();

            Color baseColor = UtilityWPF.ColorFromHex(colors.InnerColorDiffuse);

            ColorHSV driftedColor = baseColor.ToHSV();
            driftedColor = new ColorHSV(UtilityWPF.GetCappedAngle(driftedColor.H + (colors.DiffuseDrift * StaticRandom.NextDouble(0, 4))), driftedColor.S, driftedColor.V);

            material.Children.Add(new DiffuseMaterial(new SolidColorBrush(driftedColor.ToRGB())));
            material.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("80FFFFFF")), 20));
            material.Children.Add(new EmissiveMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("20" + colors.EmissiveColor.Substring(colors.EmissiveColor.Length - 6)))));

            // Geometry Model
            GeometryModel3D retVal = new GeometryModel3D();
            retVal.Material = material;
            retVal.BackMaterial = material;

            retVal.Geometry = UtilityWPF.GetSphere_LatLon(5, radius);

            Transform3DGroup transform = new Transform3DGroup();
            transform.Children.Add(new ScaleTransform3D(new Vector3D(.75d, .75d, 1d)));
            transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRandomRotation())));
            transform.Children.Add(new TranslateTransform3D(position.ToVector()));

            retVal.Transform = transform;

            return retVal;
        }

        //private Tuple<BotDNA, WeaponHandleDNA> GetBestBotDNA()
        //{
        //    //TODO: This will get more complex when there are many to choose from
        //    BotDNA retVal = _botDNA;
        //    if (retVal == null)
        //    {
        //        retVal = new BotDNA();
        //    }

        //    retVal.ShellColors = _shellColors;

        //    return Tuple.Create(retVal, _weaponDNA);
        //}

        #endregion
    }

    #region Class: NPCNestDNA

    public class NPCNestDNA
    {
        /// <summary>
        /// Storing this separately to guarantee that all bots are the same color
        /// </summary>
        public BotShellColorsDNA ShellColors { get; set; }

        //TODO: Instead of one, keep the top X
        public BotDNA BotDNA { get; set; }
        public WeaponDNA WeaponDNA { get; set; }

        //TODO: Store rules for training rooms
    }

    #endregion
}
