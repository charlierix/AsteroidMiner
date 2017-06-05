using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Schedulers;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using Game.HelperClassesAI;
using Game.HelperClassesCore;
using Game.HelperClassesCore.Threads;
using Game.HelperClassesWPF;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.MapParts;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.GameItems.ShipParts;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.AsteroidMiner.AstMin2D
{
    public class ShipPlayer : Bot
    {
        #region Class: KeyThrustRequest

        private class KeyThrustRequest
        {
            public Vector3D? Linear { get; set; }
            public Vector3D? Rotate { get; set; }

            public Key Key { get; set; }

            public bool? Shift { get; set; }
            public double? MaxLinear { get; set; }
            public double? MaxRotate { get; set; }
        }

        #endregion
        #region Class: ThrusterSolution

        private class ThrusterSolution
        {
            public ThrusterSolution(KeyThrustRequest request, ThrustContributionModel model, MassMatrix inertia, double mass)
            {
                this.Request = request;

                this.Model = model;
                this.Inertia = inertia;
                this.Mass = mass;
            }

            /// <summary>
            /// This is the key that was pressed, and how much acceleration they want
            /// </summary>
            public readonly KeyThrustRequest Request;

            // These are used to figure out what percent of the map to use
            public readonly ThrustContributionModel Model;
            public readonly MassMatrix Inertia;
            public readonly double Mass;

            /// <summary>
            /// This holds a map of which thrusters to fire at what percent in order to go the requested direction
            /// </summary>
            public volatile ThrusterSolutionMap Map = null;
        }

        #endregion

        #region Declaration Section

        private readonly Map _map;

        private readonly CargoBay[] _cargoBays;
        private readonly ProjectileGun[] _guns;

        private volatile bool _isThrustMapDirty = true;
        private SortedList<Tuple<Key, bool?>, ThrusterSolution> _thrustLines = new SortedList<Tuple<Key, bool?>, ThrusterSolution>();
        private CancellationTokenSource _cancelCurrentBalancer = null;

        private KeyThrustRequest[] _keyThrustRequests = null;

        /// <summary>
        /// Workers that figure out which thrusters to fire will run on this thread
        /// TODO: If there will be many instances of ShipPlayer, then they should share a common worker thread
        /// </summary>
        /// <remarks>
        /// If the workers ran in their own threads, they would consume the threadpool, and the game would stutter
        /// </remarks>
        private readonly RoundRobinManager _thrustWorkerThread = new RoundRobinManager(new StaTaskScheduler(1));

        // bool is whether shift is down
        private List<Tuple<Key, bool?>> _downKeys = new List<Tuple<Key, bool?>>();
        private bool _isShiftDown = false;

        #endregion

        #region Constructor/Factory

        public static ShipPlayer GetNewShip(ShipDNA dna, World world, int material_Ship, Map map, ShipExtraArgs extra)
        {
            ShipDNA rotatedDNA = RotateDNA_FromExternal(dna);

            #region asserts

            //ShipDNA rockedDNA1 = RotateDNA_ToExternal(RotateDNA_FromExternal(dna));
            //ShipDNA rockedDNA2 = RotateDNA_FromExternal(rockedDNA1);

            //var compare1a = dna.PartsByLayer.SelectMany(o => o.Value).ToArray();
            //var compare1b = rockedDNA1.PartsByLayer.SelectMany(o => o.Value).ToArray();

            //var compare2a = rotatedDNA.PartsByLayer.SelectMany(o => o.Value).ToArray();
            //var compare2b = rockedDNA2.PartsByLayer.SelectMany(o => o.Value).ToArray();

            //for(int cntr = 0; cntr < compare1a.Length; cntr++)
            //{
            //    var com1a = compare1a[cntr];
            //    var com1b = compare1b[cntr];

            //    var com2a = compare2a[cntr];
            //    var com2b = compare2b[cntr];

            //    if(!Math3D.IsNearValue(com1a.Position, com1b.Position))
            //    {
            //        int three = 3;
            //    }
            //    if (!Math3D.IsNearValue(com1a.Orientation, com1b.Orientation))
            //    {
            //        int three = 3;
            //    }

            //    if (!Math3D.IsNearValue(com2a.Position, com2b.Position))
            //    {
            //        int four = 4;
            //    }
            //    if (!Math3D.IsNearValue(com2a.Orientation, com2b.Orientation))
            //    {
            //        int four = 4;
            //    }
            //}

            #endregion

            ShipCoreArgs core = new ShipCoreArgs()
            {
                Map = map,
                Material_Ship = material_Ship,
                World = world,
            };

            //var construction = await GetNewShipConstructionAsync(options, itemOptions, rotatedDNA, world, material_Ship, material_Projectile, radiation, gravity, cameraPool, map, true, true);
            //var construction = await GetNewShipConstructionAsync(rotatedDNA, world, material_Ship, map, extra);
            var construction = BotConstructor.ConstructBot(rotatedDNA, core, extra);

            return new ShipPlayer(construction);
        }

        protected ShipPlayer(BotConstruction_Result construction)
            : base(construction)
        {
            _map = construction.ArgsCore.Map;

            _cargoBays = base.Parts.Where(o => o is CargoBay).Select(o => (CargoBay)o).ToArray();
            _guns = base.Parts.Where(o => o is ProjectileGun).Select(o => (ProjectileGun)o).ToArray();

            // Listen to part destructions
            foreach (PartBase part in base.Parts)
            {
                if (part is Thruster)
                {
                    part.Destroyed += Part_DestroyedResurrected;
                    part.Resurrected += Part_DestroyedResurrected;
                }
            }

            // Explicitly set the bool so that the neurons are ignored (the ship shouldn't have a brain anyway, but it might)
            foreach (ProjectileGun gun in _guns)
            {
                gun.ShouldFire = false;
            }

            //No longer doing this here.  Keep2D classes end up with gimbal lock, and too many places need to know about this rotation
            ////NOTE: The dna is expected to be built along Z (X is right, Y is up).  But the world is in the XY plane
            //this.PhysicsBody.Rotation = this.PhysicsBody.Rotation.RotateBy(new Quaternion(new Vector3D(1, 0, 0), -90));

            //this.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(PhysicsBody_ApplyForceAndTorque);

            EnsureThrustKeysBuilt_YISUP();
        }

        #endregion

        #region Public Properties

        private volatile object _maxAcceleration_Linear = 100d; //6d;
        public double MaxAcceleration_Linear
        {
            get
            {
                return (double)_maxAcceleration_Linear;
            }
            set
            {
                _maxAcceleration_Linear = value;
                _isThrustMapDirty = true;
            }
        }

        private volatile object _maxAcceleration_Rotate = 3d; //2.5d;
        public double MaxAcceleration_Rotate
        {
            get
            {
                return (double)_maxAcceleration_Rotate;
            }
            set
            {
                _maxAcceleration_Rotate = value;
                _isThrustMapDirty = true;
            }
        }

        #endregion

        #region Public Methods

        public void KeyDown(Key key)
        {
            if (key == Key.Y)        // why y? why not?
            {
                DirectionControllerRing ring = this.Parts.FirstOrDefault(o => o is DirectionControllerRing) as DirectionControllerRing;
                if (ring != null)
                {
                    var test = ring.FindSolution_DEBUG(Math3D.GetRandomVector_Circular_Shell(1), null);
                }

                DirectionControllerSphere ball = this.Parts.FirstOrDefault(o => o is DirectionControllerSphere) as DirectionControllerSphere;
                if (ball != null)
                {
                    var test = ball.FindSolution_DEBUG(Math3D.GetRandomVector_Spherical_Shell(1), null);
                }
            }



            if (key == Key.LeftShift || key == Key.RightShift)
            {
                #region Thrust boost

                if (!_isShiftDown)
                {
                    _isShiftDown = true;
                    SwapShiftKeys(_isShiftDown);
                }

                #endregion
            }
            else if (key == Key.LeftCtrl || key == Key.RightCtrl)
            {
                #region Fire guns

                foreach (ProjectileGun gun in _guns)
                {
                    gun.ShouldFire = true;
                }

                #endregion
            }
            else
            {
                #region Thrust

                Tuple<Key, bool?> key2 = new Tuple<Key, bool?>(key, _isShiftDown);

                if (!_downKeys.Contains(key2))
                {
                    _downKeys.Add(key2);
                }

                #endregion
            }
        }
        public void KeyUp(Key key)
        {
            if (key == Key.LeftShift || key == Key.RightShift)
            {
                #region Thrust boost

                if (_isShiftDown)
                {
                    _isShiftDown = false;
                    SwapShiftKeys(_isShiftDown);
                }

                #endregion
            }
            else if (key == Key.LeftCtrl || key == Key.RightCtrl)
            {
                #region Fire guns

                foreach (ProjectileGun gun in _guns)
                {
                    gun.ShouldFire = false;
                }

                #endregion
            }
            else
            {
                #region Thrust

                Tuple<Key, bool?> key2 = new Tuple<Key, bool?>(key, _isShiftDown);

                if (_downKeys.Contains(key2))
                {
                    _downKeys.Remove(key2);
                }

                #endregion
            }
        }

        /// <summary>
        /// This fires combinations of thrusters to get to the destination
        /// </summary>
        /// <remarks>
        /// This is a higher level method than SetVelocity
        /// 
        /// This sort of method would be useful when following a target
        /// </remarks>
        /// <param name="velocity">This is the velocity to be when the ship as at the destination point (doesn't matter what the velocity is when getting there)</param>
        /// <param name="spin">This is the spin to have when at the destination point</param>
        /// <param name="direction">This is the direction to face when at the destination point</param>
        private void SetDestination(Point3D point, Vector3D velocity, Vector3D? spin = null, DoubleVector? direction = null)
        {
            throw new ApplicationException("finish this");
        }
        /// <summary>
        /// This fires combinations of thrusters to try to get to the velocity/orientation passed in
        /// </summary>
        /// <remarks>
        /// The SetForce method is a direct control.  SetVelocity is one step higher.
        /// 
        /// The advantage of calling SetVelocity is that if there are unknown forces acting on the ship, this will
        /// account for that over time (less logic for the caller to implement)
        /// </remarks>
        private void SetVelocity(Vector3D velocity, Vector3D? spin = null, DoubleVector? direction = null)
        {
            throw new ApplicationException("finish this");
        }
        /// <summary>
        /// This fires combinations of thrusters to try to produce the force/torque passed in
        /// </summary>
        public void SetForce(Vector3D velocity, Vector3D? spin = null)
        {

        }

        /// <summary>
        /// This will take the mineral if it fits
        /// NOTE: This method removes the mineral from the map
        /// </summary>
        private void CollidedMineral_ORIG(Mineral mineral, World world, int materialID, SharedVisuals sharedVisuals)
        {
            //TODO: Let the user specify thresholds for which minerals to take ($, density, mass, type).  Also give an option to be less picky if near empty
            //TODO: Let the user specify thresholds for swapping lesser minerals for better ones

            if (base.CargoBays == null)
            {
                return;
            }
            else if (mineral.IsDisposed)
            {
                return;
            }

            var quantity = base.CargoBays.CargoVolume;

            if (quantity.Item2 - quantity.Item1 < mineral.VolumeInCubicMeters)
            {
                // The cargo bays are too full
                return;
            }

            // Save location in case it needs to be brought back
            Point3D position = mineral.PositionWorld;

            // Try to pop this out of the map
            if (!_map.RemoveItem(mineral, true))
            {
                // It's already gone
                return;
            }

            // Convert it to cargo
            Cargo_Mineral cargo = new Cargo_Mineral(mineral.MineralType, mineral.Density, mineral.VolumeInCubicMeters);

            // Try to add this to the cargo bays - the total volume may be enough, but the mineral may be too large for any
            // one cargo bay
            if (base.CargoBays.Add(cargo))
            {
                // Finish removing it from the real world
                mineral.PhysicsBody.Dispose();

                this.ShouldRecalcMass_Large = true;
            }
            else
            {
                // It didn't fit, give it back to the map
                Mineral clone = new Mineral(mineral.MineralType, position, mineral.VolumeInCubicMeters, world, materialID, sharedVisuals, ItemOptionsAstMin2D.MINERAL_DENSITYMULT, mineral.Scale, mineral.Credits);
                _map.AddItem(clone);
            }
        }
        public void CollidedMineral(Mineral mineral, World world, int materialID, SharedVisuals sharedVisuals)
        {
            //TODO: Let the user specify thresholds for which minerals to take ($, density, mass, type).  Also give an option to be less picky if near empty
            //TODO: Let the user specify thresholds for swapping lesser minerals for better ones
            //TODO: Add a portion

            if (base.CargoBays == null)
            {
                return;
            }
            else if (mineral.IsDisposed)
            {
                return;
            }

            var quantity = base.CargoBays.CargoVolume;

            if (quantity.Item2 - quantity.Item1 < mineral.VolumeInCubicMeters)
            {
                // The cargo bays are too full
                return;
            }

            // Save location in case it needs to be brought back
            Point3D position = mineral.PositionWorld;

            // Convert it to cargo
            Cargo_Mineral cargo = new Cargo_Mineral(mineral.MineralType, mineral.Density, mineral.VolumeInCubicMeters);

            // Try to add this to the cargo bays - the total volume may be enough, but the mineral may be too large for any
            // one cargo bay (or some of the cargo bays could be destroyed)
            if (base.CargoBays.Add(cargo))
            {
                // Finish removing it from the real world
                _map.RemoveItem(mineral, true);
                mineral.PhysicsBody.Dispose();

                this.ShouldRecalcMass_Large = true;
            }
        }

        #endregion

        #region Overrides

        public override void Update_MainThread(double elapsedTime)
        {
            #region clear thrusters/impulse

            // Reset the thrusters
            //TODO: It's ineficient to do this every tick
            if (this.Thrusters != null)
            {
                foreach (Thruster thruster in this.Thrusters)
                {
                    thruster.Percents = new double[thruster.ThrusterDirectionsModel.Length];
                }
            }

            if (this.ImpulseEngines != null)
            {
                foreach (ImpulseEngine impulse in this.ImpulseEngines)
                {
                    impulse.SetDesiredDirection(null);      // can't call clear, because then it will listen to its neurons
                }
            }

            #endregion

            if (_downKeys.Count > 0)
            {
                EnsureThrustKeysBuilt_YISUP();

                List<Tuple<Vector3D?, Vector3D?>> impulseEngineCommand = new List<Tuple<Vector3D?, Vector3D?>>();

                foreach (var key in _downKeys)
                {
                    #region thrusters

                    ThrusterSolution solution;
                    if (_thrustLines.TryGetValue(key, out solution) || _thrustLines.TryGetValue(Tuple.Create(key.Item1, (bool?)null), out solution))      // _downKeys will always have the bool set to true or false, but _thrustLines may have it stored as a null (null means ignore shift key)
                    {
                        ThrusterSolutionMap map = solution.Map;
                        if (map != null)
                        {
                            //TODO: The thrust map is normalized for maximum thrust.  If they want full thrust immediately, use it.  Otherwise roll on the thrust as they
                            //hold the key in (start at a fixed min accel, then gradient up to full after a second or two)
                            //solution.Request.Max

                            double percentLin = 1d;
                            if (solution.Request.Linear != null && solution.Request.MaxLinear != null && map.LinearAccel > solution.Request.MaxLinear.Value)
                            {
                                percentLin = solution.Request.MaxLinear.Value / map.LinearAccel;
                            }

                            double percentRot = 1d;
                            if (solution.Request.Rotate != null && solution.Request.MaxRotate != null && map.RotateAccel > solution.Request.MaxRotate.Value)
                            {
                                percentRot = solution.Request.MaxRotate.Value / map.RotateAccel;
                            }

                            double percent = Math.Min(percentLin, percentRot);

                            foreach (ThrusterSetting thruster in map.Map.UsedThrusters)
                            {
                                //NOTE: If this percent goes over 1, the Fire method will cap it.  Any future control theory logic will get confused, because not all of what it said was actually used
                                thruster.Thruster.Percents[thruster.SubIndex] += thruster.Percent * percent;
                            }
                        }
                    }

                    #endregion

                    #region impulse engines - WRONG

                    // This converts to acceleration, then force.  It ignored the size of the engine
                    // But I changed the impulse engines to take percents, and they can output force based on their scale

                    //if (this.ImpulseEngines != null && this.ImpulseEngines.Length > 0)
                    //{
                    //    // Figure out what vector (linear and/or torque) is associated with this key
                    //    var match = _keyThrustRequests.
                    //        Where(o => o.Key == key.Item1).
                    //        Select(o => new
                    //        {
                    //            Request = o,
                    //            Score = o.Shift == null || key.Item2 == null ? 1 :      // shift press is null, this is the middle score
                    //                o.Shift.Value == key.Item2.Value ? 0 :      // shift presses match, this is the best score
                    //                2,      // shift presses don't match, this is the worst score
                    //        }).
                    //        OrderBy(o => o.Score).
                    //        Select(o => o.Request).
                    //        FirstOrDefault();

                    //    if (match != null)
                    //    {
                    //        double magnitude;
                    //        Vector3D? force = null;
                    //        if (match.Linear != null)
                    //        {
                    //            magnitude = match.MaxLinear ?? this.MaxAcceleration_Linear;
                    //            double mass = PhysicsBody.Mass;
                    //            force = match.Linear.Value * magnitude * mass;        //NOTE: The request force is expected to be a unit vector
                    //        }

                    //        Vector3D? torque = null;
                    //        if (match.Rotate != null)
                    //        {
                    //            magnitude = match.MaxRotate ?? this.MaxAcceleration_Rotate;

                    //            MassMatrix massMatrix = PhysicsBody.MassMatrix;
                    //            //double massMult = Vector3D.DotProduct(massMatrix.Inertia.ToUnit(false), match.Rotate.Value.ToUnit(false));
                    //            //massMult = Math.Abs(massMult);
                    //            //massMult *= massMatrix.Mass;
                    //            double massMult = Vector3D.DotProduct(massMatrix.Inertia, match.Rotate.Value.ToUnit(false));
                    //            massMult = Math.Abs(massMult);
                    //            //massMult *= massMatrix.Mass;

                    //            torque = match.Rotate.Value * magnitude * massMult;
                    //        }

                    //        impulseEngineCommand.Add(Tuple.Create(force, torque));
                    //    }
                    //}

                    #endregion
                    #region impulse engines

                    if (this.ImpulseEngines != null && this.ImpulseEngines.Length > 0)
                    {
                        // Figure out what vector (linear and/or torque) is associated with this key
                        var match = _keyThrustRequests.
                            Where(o => o.Key == key.Item1).
                            Select(o => new
                            {
                                Request = o,
                                Score = o.Shift == null || key.Item2 == null ? 1 :      // shift press is null, this is the middle score
                                    o.Shift.Value == key.Item2.Value ? 0 :      // shift presses match, this is the best score
                                    2,      // shift presses don't match, this is the worst score
                            }).
                            OrderBy(o => o.Score).
                            Select(o => o.Request).
                            FirstOrDefault();

                        if (match != null)
                        {
                            // Impulse engine wants the vectors to be percents (length up to 1)
                            impulseEngineCommand.Add(Tuple.Create(match.Linear, match.Rotate));
                        }
                    }

                    #endregion
                }

                #region impulse engines

                if (this.ImpulseEngines != null && this.ImpulseEngines.Length > 0 && impulseEngineCommand.Count > 0)
                {
                    var impulseCommand = impulseEngineCommand.ToArray();

                    foreach (ImpulseEngine impulseEngine in this.ImpulseEngines)
                    {
                        impulseEngine.SetDesiredDirection(impulseCommand);
                    }
                }

                #endregion
            }

            base.Update_MainThread(elapsedTime);
        }

        public override int? IntervalSkips_MainThread
        {
            get
            {
                return 0;
            }
        }

        protected override void OnMassRecalculated()
        {
            base.OnMassRecalculated();

            //NOTE: This method is called from random threads
            _isThrustMapDirty = true;
        }

        public override ShipDNA GetNewDNA()
        {
            // Internally, the dna is along Y, but externally, it needs to be along Z
            return RotateDNA_ToExternal(base.GetNewDNA());
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_cancelCurrentBalancer != null)
                {
                    _cancelCurrentBalancer.Cancel();
                    _cancelCurrentBalancer = null;
                }
            }

            base.Dispose(disposing);
        }

        #endregion

        #region Event Listeners

        private void Part_DestroyedResurrected(object sender, EventArgs e)
        {
            _isThrustMapDirty = true;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// This searches for any keys that have different functions
        /// </summary>
        /// <param name="currentShift"></param>
        private void SwapShiftKeys(bool currentShift)
        {
            int index = 0;

            List<Key> swaps = new List<Key>();

            // Remove any that no longer belong
            while (index < _downKeys.Count)
            {
                if (_downKeys[index].Item2 == null || _downKeys[index].Item2.Value == currentShift)
                {
                    index++;
                }
                else
                {
                    swaps.Add(_downKeys[index].Item1);
                    _downKeys.RemoveAt(index);
                }
            }

            // Add the new shift modified versions
            foreach (Key key in swaps)
            {
                var key2 = Tuple.Create(key, (bool?)currentShift);

                if (!_downKeys.Contains(key2))
                {
                    _downKeys.Add(key2);
                }
            }
        }

        private void EnsureThrustKeysBuilt_ZISUP()
        {
            if (!_isThrustMapDirty)
            {
                return;
            }

            #region linear

            double? maxAccel = this.MaxAcceleration_Linear;

            var directions = new[]
            {
                //new KeyThrustRequest(){ Linear = new Vector3D(0, 0, 1), Key = Key.W, Shift = (bool?)null },
                new KeyThrustRequest(){ Linear = new Vector3D(0, 0, 1), Key = Key.W, Shift = (bool?)false },
                new KeyThrustRequest(){ Linear = new Vector3D(0, 0, 1), Key = Key.Up, Shift = (bool?)true },
                new KeyThrustRequest(){ Linear = new Vector3D(0, 0, 1), Key = Key.Up, Shift = (bool?)false, MaxLinear = maxAccel },

                //new KeyThrustRequest(){ Linear = new Vector3D(0, 0, -1), Key = Key.S, Shift = (bool?)null },
                new KeyThrustRequest(){ Linear = new Vector3D(0, 0, -1), Key = Key.S, Shift = (bool?)false },
                new KeyThrustRequest(){ Linear = new Vector3D(0, 0, -1), Key = Key.Down, Shift = (bool?)true },
                new KeyThrustRequest(){ Linear = new Vector3D(0, 0, -1), Key = Key.Down, Shift = (bool?)false, MaxLinear = maxAccel },

                new KeyThrustRequest(){ Linear = new Vector3D(-1, 0, 0), Key = Key.A, Shift = (bool?)true },
                new KeyThrustRequest(){ Linear = new Vector3D(-1, 0, 0), Key = Key.A, Shift = (bool?)false, MaxLinear = maxAccel },

                new KeyThrustRequest(){ Linear = new Vector3D(1, 0, 0), Key = Key.D, Shift = (bool?)true },
                new KeyThrustRequest(){ Linear = new Vector3D(1, 0, 0), Key = Key.D, Shift = (bool?)false, MaxLinear = maxAccel },
            };

            #endregion
            #region rotation

            maxAccel = this.MaxAcceleration_Rotate;

            var torques = new[]
            {
                new KeyThrustRequest(){ Rotate = new Vector3D(0, -1, 0), Key = Key.Left, Shift = (bool?)true },
                new KeyThrustRequest(){ Rotate = new Vector3D(0, -1, 0), Key = Key.Left, Shift = (bool?)false, MaxRotate = maxAccel },

                new KeyThrustRequest(){ Rotate = new Vector3D(0, 1, 0), Key = Key.Right, Shift = (bool?)true },
                new KeyThrustRequest(){ Rotate = new Vector3D(0, 1, 0), Key = Key.Right, Shift = (bool?)false, MaxRotate = maxAccel },

                new KeyThrustRequest(){ Rotate = new Vector3D(0, 0, -1), Key = Key.Q, Shift = (bool?)false },        // roll left
                new KeyThrustRequest(){ Rotate = new Vector3D(0, 0, 1), Key = Key.E, Shift = (bool?)false },

                //new KeyThrustRequest(){ Rotate = new Vector3D(-1, 0, 0), Key = Key.Q, Shift = (bool?)true },     // pitch down
                //new KeyThrustRequest(){ Rotate = new Vector3D(1, 0, 0), Key = Key.E, Shift = (bool?)true },
                new KeyThrustRequest(){ Rotate = new Vector3D(-1, 0, 0), Key = Key.W, Shift = (bool?)true },     // pitch down
                new KeyThrustRequest(){ Rotate = new Vector3D(1, 0, 0), Key = Key.S, Shift = (bool?)true },
            };

            #endregion

            EnsureThrustKeysBuilt_Finish(directions.Concat(torques).ToArray());
        }
        private void EnsureThrustKeysBuilt_YISUP()
        {
            if (!_isThrustMapDirty)
            {
                return;
            }

            #region linear

            double? maxAccel = this.MaxAcceleration_Linear;

            var directions = new[]
            {
                //new KeyThrustRequest(){ Linear = new Vector3D(0, 1, 0), Key = Key.W, Shift = (bool?)null },
                new KeyThrustRequest(){ Linear = new Vector3D(0, 1, 0), Key = Key.W, Shift = (bool?)false },
                new KeyThrustRequest(){ Linear = new Vector3D(0, 1, 0), Key = Key.Up, Shift = (bool?)true},
                new KeyThrustRequest(){ Linear = new Vector3D(0, 1, 0), Key = Key.Up, Shift = (bool?)false, MaxLinear = maxAccel },

                //new KeyThrustRequest(){ Linear = new Vector3D(0, -1, 0), Key = Key.S, Shift = (bool?)null },
                new KeyThrustRequest(){ Linear = new Vector3D(0, -1, 0), Key = Key.S, Shift = (bool?)false},
                new KeyThrustRequest(){ Linear = new Vector3D(0, -1, 0), Key = Key.Down, Shift = (bool?)true },
                new KeyThrustRequest(){ Linear = new Vector3D(0, -1, 0), Key = Key.Down, Shift = (bool?)false, MaxLinear = maxAccel },

                new KeyThrustRequest(){ Linear = new Vector3D(-1, 0, 0), Key = Key.A, Shift = (bool?)true },
                new KeyThrustRequest(){ Linear = new Vector3D(-1, 0, 0), Key = Key.A, Shift = (bool?)false, MaxLinear = maxAccel },

                new KeyThrustRequest(){ Linear = new Vector3D(1, 0, 0), Key = Key.D, Shift = (bool?)true },
                new KeyThrustRequest(){ Linear = new Vector3D(1, 0, 0), Key = Key.D, Shift = (bool?)false, MaxLinear = maxAccel },
            };

            #endregion
            #region rotation

            maxAccel = this.MaxAcceleration_Rotate;

            var torques = new[]
            {
                new KeyThrustRequest(){ Rotate = new Vector3D(0, 0, 1), Key = Key.Left, Shift = (bool?)true },
                new KeyThrustRequest(){ Rotate = new Vector3D(0, 0, 1), Key = Key.Left, Shift = (bool?)false, MaxRotate = maxAccel },

                new KeyThrustRequest(){ Rotate = new Vector3D(0, 0, -1), Key = Key.Right, Shift = (bool?)true },
                new KeyThrustRequest(){ Rotate = new Vector3D(0, 0, -1), Key = Key.Right, Shift = (bool?)false, MaxRotate = maxAccel },

                new KeyThrustRequest(){ Rotate = new Vector3D(0, -1, 0), Key = Key.Q, Shift = (bool?)false },        // roll left
                new KeyThrustRequest(){ Rotate = new Vector3D(0, 1, 0), Key = Key.E, Shift = (bool?)false },

                //new KeyThrustRequest(){ Rotate = new Vector3D(-1, 0, 0), Key = Key.Q, Shift = (bool?)true },     // pitch down
                //new KeyThrustRequest(){ Rotate = new Vector3D(1, 0, 0), Key = Key.E, Shift = (bool?)true },
                new KeyThrustRequest(){ Rotate = new Vector3D(-1, 0, 0), Key = Key.W, Shift = (bool?)true },     // pitch down
                new KeyThrustRequest(){ Rotate = new Vector3D(1, 0, 0), Key = Key.S, Shift = (bool?)true },
            };

            #endregion

            EnsureThrustKeysBuilt_Finish(directions.Concat(torques).ToArray());
        }
        private void EnsureThrustKeysBuilt_Finish(KeyThrustRequest[] requests)
        {
            // Remember the mappings between key and desired thrust (this is used to drive the impulse engine)
            _keyThrustRequests = requests;

            if (this.Thrusters == null || this.Thrusters.Length == 0)
            {
                _isThrustMapDirty = false;
                return;
            }

            if (_cancelCurrentBalancer != null)
            {
                _cancelCurrentBalancer.Cancel();
                _cancelCurrentBalancer = null;
            }

            // Remember the current solutions, so they can help get a good start on the new solver
            var previous = _thrustLines.Values.ToArray();

            _thrustLines.Clear();
            _cancelCurrentBalancer = new CancellationTokenSource();

            ThrustContributionModel model = new ThrustContributionModel(this.Thrusters, this.PhysicsBody.CenterOfMass);
            MassMatrix inertia = this.PhysicsBody.MassMatrix;
            double mass = inertia.Mass;

            // Several key combos may request the same direction, so group them
            var grouped = requests.
                ToLookup(KeyThrustRequestComparer);

            foreach (var set in grouped)
            {
                // Create wrappers for this set
                ThrusterSolution[] solutionWrappers = set.
                    Select(o => new ThrusterSolution(o, model, inertia, mass)).
                    ToArray();

                // Store the wrappers
                foreach (var wrapper in solutionWrappers)
                {
                    _thrustLines.Add(Tuple.Create(wrapper.Request.Key, wrapper.Request.Shift), wrapper);
                }

                // This delegate gets called when a better solution is found.  Distribute the map to the solution wrappers
                var newBestFound = new Action<ThrusterMap>(o =>
                {
                    ThrusterSolutionMap solutionMap = GetThrusterSolutionMap(o, model, inertia, mass);

                    foreach (ThrusterSolution wrapper in solutionWrappers)
                    {
                        wrapper.Map = solutionMap;
                    }
                });

                var options = new DiscoverSolutionOptions2<Tuple<int, int, double>>()
                {
                    //MaxIterations = 2000,     //TODO: Find a reasonable stop condition
                    ThreadShare = _thrustWorkerThread,
                };

                // Find the previous solution for this request
                var prevMatch = previous.FirstOrDefault(o => KeyThrustRequestComparer(set.Key, o.Request));
                if (prevMatch != null && prevMatch.Map != null)
                {
                    options.Predefined = new[] { prevMatch.Map.Map.Flattened };
                }

                // Find the combination of thrusters that push in the requested direction
                //ThrustControlUtil.DiscoverSolutionAsync(this, solutionWrappers[0].Request.Linear, solutionWrappers[0].Request.Rotate, _cancelCurrentBalancer.Token, model, newBestFound, null, options);
                ThrustControlUtil.DiscoverSolutionAsync2(this, solutionWrappers[0].Request.Linear, solutionWrappers[0].Request.Rotate, _cancelCurrentBalancer.Token, model, newBestFound, options: options);
            }

            _isThrustMapDirty = false;
        }

        private static ThrusterSolutionMap GetThrusterSolutionMap(ThrusterMap map, ThrustContributionModel model, MassMatrix inertia, double mass)
        {
            // Add up the forces
            Vector3D sumLinearForce = new Vector3D();
            Vector3D sumTorque = new Vector3D();
            foreach (ThrusterSetting thruster in map.UsedThrusters)
            {
                var contribution = model.Contributions.FirstOrDefault(o => o.Item1 == thruster.ThrusterIndex && o.Item2 == thruster.SubIndex);
                if (contribution == null)
                {
                    throw new ApplicationException(string.Format("Didn't find contribution for thruster: {0}, {1}", thruster.ThrusterIndex, thruster.SubIndex));
                }

                sumLinearForce += contribution.Item3.TranslationForce * thruster.Percent;
                sumTorque += contribution.Item3.Torque * thruster.Percent;
            }

            // Divide by mass
            //F=MA, A=F/M
            double accel = sumLinearForce.Length / mass;

            Vector3D projected = inertia.Inertia.GetProjectedVector(sumTorque);
            double angAccel = sumTorque.Length / projected.Length;
            if (Math1D.IsInvalid(angAccel))
            {
                angAccel = 0;       // this happens when there is no net torque
            }

            return new ThrusterSolutionMap(map, accel, angAccel);
        }

        private static bool KeyThrustRequestComparer(KeyThrustRequest item1, KeyThrustRequest item2)
        {
            if (item1 == null && item2 == null)
            {
                return true;
            }
            else if (item1 == null || item2 == null)
            {
                return false;
            }

            return VectorComparer(item1.Linear, item2.Linear) && VectorComparer(item1.Rotate, item2.Rotate);
        }
        private static bool VectorComparer(Vector3D? item1, Vector3D? item2)
        {
            if (item1 == null && item2 == null)
            {
                return true;
            }
            else if (item1 == null || item2 == null)
            {
                return false;
            }

            return Math3D.IsNearValue(item1.Value, item2.Value);
        }

        private static ShipDNA RotateDNA_FromExternal(ShipDNA dna)
        {
            Quaternion quat = new Quaternion(new Vector3D(1, 0, 0), -90);

            RotateTransform3D rotation = new RotateTransform3D(new QuaternionRotation3D(quat));


            //double sqrt2div2 = Math.Sqrt(2d) / 2d;
            //Quaternion orig = new Quaternion(0, -sqrt2div2, 0, sqrt2div2);
            //Quaternion rotated = orig.RotateBy(quat);
            //Quaternion rotated3 = quat.RotateBy(orig);


            //Vector3D z = new Vector3D(0, 0, 1);
            //Vector3D zOrig = orig.GetRotatedVector(z);
            //Vector3D zRot = rotated.GetRotatedVector(z);
            //Vector3D zRot3 = rotated3.GetRotatedVector(z);


            //Transform3DGroup group = new Transform3DGroup();
            //group.Children.Add(new RotateTransform3D(new QuaternionRotation3D(orig)));
            //group.Children.Add(new RotateTransform3D(new QuaternionRotation3D(quat)));

            //Vector3D zRot2 = group.Transform(z);


            //Thruster testThrust = new Thruster();


            return RotateDNA_DoIt(dna, quat, rotation);
        }
        private static ShipDNA RotateDNA_ToExternal(ShipDNA dna)
        {
            Quaternion quat = new Quaternion(new Vector3D(1, 0, 0), 90);

            RotateTransform3D rotation = new RotateTransform3D(new QuaternionRotation3D(quat));

            return RotateDNA_DoIt(dna, quat, rotation);
        }
        private static ShipDNA RotateDNA_DoIt(ShipDNA dna, Quaternion rotation, Transform3D positionTransform)
        {
            ShipDNA retVal = UtilityCore.Clone(dna);

            foreach (ShipPartDNA part in retVal.PartsByLayer.SelectMany(o => o.Value))
            {
                // Rotate the orientation
                //part.Orientation = part.Orientation.RotateBy(rotation);
                part.Orientation = rotation.RotateBy(part.Orientation);


                // Apply a transform to the poisition
                part.Position = positionTransform.Transform(part.Position);


                //TODO: See if these need to be rotated as well
                //part.Neurons;
                //part.ExternalLinks;
                //part.InternalLinks;
            }

            return retVal;
        }

        #endregion
    }
}
