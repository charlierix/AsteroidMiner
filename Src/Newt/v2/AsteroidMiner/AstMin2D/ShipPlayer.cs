using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.MapParts;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.GameItems.ShipParts;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.AsteroidMiner.AstMin2D
{
    public class ShipPlayer : Ship
    {
        #region Class: ThrustContribution

        private class ThrustContribution
        {
            #region Constructor

            public ThrustContribution(Thruster thruster, int index, Vector3D translationForce, Vector3D torque)
            {
                this.Thruster = thruster;
                this.Index = index;

                this.TranslationForceLength = translationForce.Length;
                this.TranslationForce = translationForce;
                this.TranslationForceUnit = new Vector3D(translationForce.X / this.TranslationForceLength, translationForce.Y / this.TranslationForceLength, translationForce.Z / this.TranslationForceLength);

                this.TorqueLength = torque.Length;
                this.Torque = torque;
                this.TorqueUnit = new Vector3D(torque.X / this.TorqueLength, torque.Y / this.TorqueLength, torque.Z / this.TorqueLength);
            }

            #endregion

            public readonly Thruster Thruster;
            public readonly int Index;

            public readonly Vector3D TranslationForce;
            public readonly Vector3D TranslationForceUnit;
            public readonly double TranslationForceLength;

            public readonly Vector3D Torque;
            public readonly Vector3D TorqueUnit;
            public readonly double TorqueLength;
        }

        #endregion
        #region Class: ThrusterSetting

        private class ThrusterSetting
        {
            public ThrusterSetting(Thruster thruster, int index, double percent)
            {
                this.Thruster = thruster;
                this.Index = index;
                this.Percent = percent;
            }

            public readonly Thruster Thruster;
            public readonly int Index;
            public readonly double Percent;
        }

        #endregion

        #region Declaration Section

        private readonly Map _map;

        private readonly CargoBay[] _cargoBays;
        private readonly ProjectileGun[] _guns;

        private volatile bool _isThrustMapDirty = true;
        private SortedList<Tuple<Key, bool?>, IEnumerable<ThrusterSetting>> _thrustLines = new SortedList<Tuple<Key, bool?>, IEnumerable<ThrusterSetting>>();

        // bool is whether shift is down
        private List<Tuple<Key, bool?>> _downKeys = new List<Tuple<Key, bool?>>();
        private bool _isShiftDown = false;

        #endregion

        #region Constructor/Factory

        public static async Task<ShipPlayer> GetNewShipAsync(EditorOptions options, ItemOptions itemOptions, ShipDNA dna, World world, int material_Ship, int material_Projectile, RadiationField radiation, IGravityField gravity, CameraPool cameraPool, Map map)
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

            var construction = await GetNewShipConstructionAsync(options, itemOptions, rotatedDNA, world, material_Ship, material_Projectile, radiation, gravity, cameraPool, map, true, true);

            return new ShipPlayer(construction, map);
        }

        protected ShipPlayer(ShipConstruction construction, Map map)
            : base(construction)
        {
            _map = map;

            _cargoBays = base.Parts.Where(o => o is CargoBay).Select(o => (CargoBay)o).ToArray();
            _guns = base.Parts.Where(o => o is ProjectileGun).Select(o => (ProjectileGun)o).ToArray();

            // Explicitly set the bool so that the neurons are ignored (the ship shouldn't have a brain anyway, but it might)
            foreach (ProjectileGun gun in _guns)
            {
                gun.ShouldFire = false;
            }

            //No longer doing this here.  Keep2D classes end up with gimbal lock, and too many places need to know about this rotation
            ////NOTE: The dna is expected to be built along Z (X is right, Y is up).  But the world is in the XY plane
            //this.PhysicsBody.Rotation = this.PhysicsBody.Rotation.RotateBy(new Quaternion(new Vector3D(1, 0, 0), -90));

            //this.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(PhysicsBody_ApplyForceAndTorque);
        }

        #endregion

        #region Public Properties

        //TODO: Let the user modify these (XWing vs TIE Fighter had a great energy management interface with F9 thru F12 keys)
        //Lower values would naturally promote fuel savings, as well as controlability.  Have settings like { Low --- Max, Auto }.
        //Auto would only go to low when fuel is scarce, and go to High/Max when threats are near/attacking.
        private volatile object _maxAcceleration_Linear = 6d;
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

        private volatile object _maxAcceleration_Rotate = 2.5d;
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
        public void CollidedMineral(Mineral mineral)
        {
            //TODO: Let the user specify thresholds for which minerals to take ($, density, mass, type).  Also give an option to be less picky if near empty
            //TODO: Let the user specify thresholds for swapping lesser minerals for better ones

            var quantity = base.CargoBays.CargoVolume;

            if (quantity.Item2 - quantity.Item1 < mineral.VolumeInCubicMeters)
            {
                // The cargo bays are too full
                return;
            }

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
                _map.AddItem(mineral);
            }
        }

        #endregion

        #region Overrides

        public override void Update_MainThread(double elapsedTime)
        {
            // Reset the thrusters
            //TODO: It's ineficient to do this every tick
            foreach (Thruster thruster in this.Thrusters)
            {
                thruster.Percents = new double[thruster.ThrusterDirectionsModel.Length];
            }

            if (_downKeys.Count > 0)
            {
                EnsureThrustKeysBuilt();

                foreach (var key in _downKeys)
                {
                    IEnumerable<ThrusterSetting> thrusters;
                    if (_thrustLines.TryGetValue(key, out thrusters) || _thrustLines.TryGetValue(Tuple.Create(key.Item1, (bool?)null), out thrusters))      // _downKeys will always have the bool set to true or false, but _thrustLines may have it stored as a null (null means ignore shift key)
                    {
                        foreach (ThrusterSetting thruster in thrusters)
                        {
                            //TODO: If this percent goes over 1, the Fire method will cap it.  The control theory logic will get confused, because not all of what it said was actually used
                            thruster.Thruster.Percents[thruster.Index] += thruster.Percent;
                        }
                    }
                }
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

            if (this.Thrusters == null || this.Thrusters.Length == 0)
            {
                _isThrustMapDirty = false;
                return;
            }

            _thrustLines.Clear();

            ThrustContribution[] contributions = GetThrusterContributions(this.Thrusters, this.PhysicsBody.CenterOfMass);

            MassMatrix inertia = this.PhysicsBody.MassMatrix;
            double mass = inertia.Mass;

            #region Linear

            double? maxAccel = this.MaxAcceleration_Linear;

            var directions = new[]
            {
                //new { Direction = new Vector3D(0, 0, 1), Key = Key.W, Shift = (bool?)null, Max = (double?)null },
                new { Direction = new Vector3D(0, 0, 1), Key = Key.W, Shift = (bool?)false, Max = (double?)null },
                new { Direction = new Vector3D(0, 0, 1), Key = Key.Up, Shift = (bool?)true, Max = (double?)null },
                new { Direction = new Vector3D(0, 0, 1), Key = Key.Up, Shift = (bool?)false, Max = maxAccel },

                //new { Direction = new Vector3D(0, 0, -1), Key = Key.S, Shift = (bool?)null, Max = (double?)null },
                new { Direction = new Vector3D(0, 0, -1), Key = Key.S, Shift = (bool?)false, Max = (double?)null },
                new { Direction = new Vector3D(0, 0, -1), Key = Key.Down, Shift = (bool?)true, Max = (double?)null },
                new { Direction = new Vector3D(0, 0, -1), Key = Key.Down, Shift = (bool?)false, Max = maxAccel },

                new { Direction = new Vector3D(-1, 0, 0), Key = Key.A, Shift = (bool?)true, Max = (double?)null },
                new { Direction = new Vector3D(-1, 0, 0), Key = Key.A, Shift = (bool?)false, Max = maxAccel },

                new { Direction = new Vector3D(1, 0, 0), Key = Key.D, Shift = (bool?)true, Max = (double?)null },
                new { Direction = new Vector3D(1, 0, 0), Key = Key.D, Shift = (bool?)false, Max = maxAccel },
            };

            foreach (var direction in directions)
            {
                IEnumerable<ThrusterSetting> thrusters;
                if (direction.Max == null)
                {
                    thrusters = EnsureThrustKeysBuilt_Linear(direction.Direction, contributions);
                }
                else
                {
                    thrusters = EnsureThrustKeysBuilt_Linear(direction.Direction, contributions, direction.Max.Value, mass);
                }

                _thrustLines.Add(Tuple.Create(direction.Key, direction.Shift), thrusters);
            }

            #endregion
            #region Rotation

            maxAccel = this.MaxAcceleration_Rotate;

            var torques = new[]
            {
                new { Torque = new Vector3D(0, -1, 0), Key = Key.Left, Shift = (bool?)true, Max = (double?)null },
                new { Torque = new Vector3D(0, -1, 0), Key = Key.Left, Shift = (bool?)false, Max = maxAccel },

                new { Torque = new Vector3D(0, 1, 0), Key = Key.Right, Shift = (bool?)true, Max = (double?)null },
                new { Torque = new Vector3D(0, 1, 0), Key = Key.Right, Shift = (bool?)false, Max = maxAccel },

                new { Torque = new Vector3D(0, 0, -1), Key = Key.Q, Shift = (bool?)false, Max = (double?)null },        // roll left
                new { Torque = new Vector3D(0, 0, 1), Key = Key.E, Shift = (bool?)false, Max = (double?)null },

                //new { Torque = new Vector3D(-1, 0, 0), Key = Key.Q, Shift = (bool?)true, Max = (double?)null },     // pitch down
                //new { Torque = new Vector3D(1, 0, 0), Key = Key.E, Shift = (bool?)true, Max = (double?)null },
                new { Torque = new Vector3D(-1, 0, 0), Key = Key.W, Shift = (bool?)true, Max = (double?)null },     // pitch down
                new { Torque = new Vector3D(1, 0, 0), Key = Key.S, Shift = (bool?)true, Max = (double?)null },
            };

            foreach (var torque in torques)
            {
                IEnumerable<ThrusterSetting> thrusters;
                if (torque.Max == null)
                {
                    thrusters = EnsureThrustKeysBuilt_Rotate(torque.Torque, contributions);
                }
                else
                {
                    thrusters = EnsureThrustKeysBuilt_Rotate(torque.Torque, contributions, torque.Max.Value, inertia);
                }

                _thrustLines.Add(Tuple.Create(torque.Key, torque.Shift), thrusters);
            }

            #endregion

            _isThrustMapDirty = false;
        }
        private void EnsureThrustKeysBuilt()
        {
            if (!_isThrustMapDirty)
            {
                return;
            }

            if (this.Thrusters == null || this.Thrusters.Length == 0)
            {
                _isThrustMapDirty = false;
                return;
            }

            _thrustLines.Clear();

            ThrustContribution[] contributions = GetThrusterContributions(this.Thrusters, this.PhysicsBody.CenterOfMass);

            MassMatrix inertia = this.PhysicsBody.MassMatrix;
            double mass = inertia.Mass;

            #region Linear

            double? maxAccel = this.MaxAcceleration_Linear;

            var directions = new[]
            {
                //new { Direction = new Vector3D(0, 1, 0), Key = Key.W, Shift = (bool?)null, Max = (double?)null },
                new { Direction = new Vector3D(0, 1, 0), Key = Key.W, Shift = (bool?)false, Max = (double?)null },
                new { Direction = new Vector3D(0, 1, 0), Key = Key.Up, Shift = (bool?)true, Max = (double?)null },
                new { Direction = new Vector3D(0, 1, 0), Key = Key.Up, Shift = (bool?)false, Max = maxAccel },

                //new { Direction = new Vector3D(0, -1, 0), Key = Key.S, Shift = (bool?)null, Max = (double?)null },
                new { Direction = new Vector3D(0, -1, 0), Key = Key.S, Shift = (bool?)false, Max = (double?)null },
                new { Direction = new Vector3D(0, -1, 0), Key = Key.Down, Shift = (bool?)true, Max = (double?)null },
                new { Direction = new Vector3D(0, -1, 0), Key = Key.Down, Shift = (bool?)false, Max = maxAccel },

                new { Direction = new Vector3D(-1, 0, 0), Key = Key.A, Shift = (bool?)true, Max = (double?)null },
                new { Direction = new Vector3D(-1, 0, 0), Key = Key.A, Shift = (bool?)false, Max = maxAccel },

                new { Direction = new Vector3D(1, 0, 0), Key = Key.D, Shift = (bool?)true, Max = (double?)null },
                new { Direction = new Vector3D(1, 0, 0), Key = Key.D, Shift = (bool?)false, Max = maxAccel },
            };

            foreach (var direction in directions)
            {
                IEnumerable<ThrusterSetting> thrusters;
                if (direction.Max == null)
                {
                    thrusters = EnsureThrustKeysBuilt_Linear(direction.Direction, contributions);
                }
                else
                {
                    thrusters = EnsureThrustKeysBuilt_Linear(direction.Direction, contributions, direction.Max.Value, mass);
                }

                _thrustLines.Add(Tuple.Create(direction.Key, direction.Shift), thrusters);
            }

            #endregion
            #region Rotation

            maxAccel = this.MaxAcceleration_Rotate;

            var torques = new[]
            {
                new { Torque = new Vector3D(0, 0, 1), Key = Key.Left, Shift = (bool?)true, Max = (double?)null },
                new { Torque = new Vector3D(0, 0, 1), Key = Key.Left, Shift = (bool?)false, Max = maxAccel },

                new { Torque = new Vector3D(0, 0, -1), Key = Key.Right, Shift = (bool?)true, Max = (double?)null },
                new { Torque = new Vector3D(0, 0, -1), Key = Key.Right, Shift = (bool?)false, Max = maxAccel },

                new { Torque = new Vector3D(0, -1, 0), Key = Key.Q, Shift = (bool?)false, Max = (double?)null },        // roll left
                new { Torque = new Vector3D(0, 1, 0), Key = Key.E, Shift = (bool?)false, Max = (double?)null },

                //new { Torque = new Vector3D(-1, 0, 0), Key = Key.Q, Shift = (bool?)true, Max = (double?)null },     // pitch down
                //new { Torque = new Vector3D(1, 0, 0), Key = Key.E, Shift = (bool?)true, Max = (double?)null },
                new { Torque = new Vector3D(-1, 0, 0), Key = Key.W, Shift = (bool?)true, Max = (double?)null },     // pitch down
                new { Torque = new Vector3D(1, 0, 0), Key = Key.S, Shift = (bool?)true, Max = (double?)null },
            };

            foreach (var torque in torques)
            {
                IEnumerable<ThrusterSetting> thrusters;
                if (torque.Max == null)
                {
                    thrusters = EnsureThrustKeysBuilt_Rotate(torque.Torque, contributions);
                }
                else
                {
                    thrusters = EnsureThrustKeysBuilt_Rotate(torque.Torque, contributions, torque.Max.Value, inertia);
                }

                _thrustLines.Add(Tuple.Create(torque.Key, torque.Shift), thrusters);
            }

            #endregion

            _isThrustMapDirty = false;
        }

        /// <summary>
        /// This overload always fires contributing thrusters at 100%
        /// </summary>
        /// <remarks>
        /// This isn't the most useful overload, but makes a good template for more complex attempts
        /// </remarks>
        private static IEnumerable<ThrusterSetting> EnsureThrustKeysBuilt_Linear(Vector3D direction, ThrustContribution[] contributions)
        {
            direction = direction.ToUnit();     // doing this so the dot product can be used as a percent

            List<Tuple<ThrustContribution, double>> retVal = new List<Tuple<ThrustContribution, double>>();

            // Get a list of thrusters that will contribute to the direction
            foreach (ThrustContribution contribution in contributions)
            {
                double dot = Vector3D.DotProduct(contribution.TranslationForceUnit, direction);

                if (dot > .05d)
                {
                    retVal.Add(Tuple.Create(contribution, contribution.TranslationForceLength * dot));
                }
            }

            retVal = FilterThrusts(retVal, .05);

            return retVal.Select(o => new ThrusterSetting(o.Item1.Thruster, o.Item1.Index, 1)).ToArray();     // commiting to array so that this linq isn't rerun each time it's iterated over
        }
        private static IEnumerable<ThrusterSetting> EnsureThrustKeysBuilt_Rotate(Vector3D torque, ThrustContribution[] contributions)
        {
            torque = torque.ToUnit();     // doing this so the dot product can be used as a percent

            List<Tuple<ThrustContribution, double>> retVal = new List<Tuple<ThrustContribution, double>>();

            // Get a list of thrusters that will contribute to the direction
            foreach (ThrustContribution contribution in contributions)
            {
                double dot = Vector3D.DotProduct(contribution.TorqueUnit, torque);

                if (dot > .5d)
                {
                    retVal.Add(Tuple.Create(contribution, contribution.TorqueLength * dot));
                }

                //if (dot > .05d)
                //{
                //    retVal.Add(new ThrusterSetting(thruster, cntr, dot));     // for now, use the dot as the percent
                //}
            }

            retVal = FilterThrusts(retVal, .05);

            return retVal.Select(o => new ThrusterSetting(o.Item1.Thruster, o.Item1.Index, 1)).ToArray();     // commiting to array so that this linq isn't rerun each time it's iterated over
        }

        /// <summary>
        /// This overload will cut back the returned percents if the thrusters exceed the acceleration passed in
        /// </summary>
        private static IEnumerable<ThrusterSetting> EnsureThrustKeysBuilt_Linear(Vector3D direction, ThrustContribution[] contributions, double maxAcceleration, double mass)
        {
            direction = direction.ToUnit();     // doing this so the dot product can be used as a percent

            List<Tuple<ThrustContribution, double>> retVal = new List<Tuple<ThrustContribution, double>>();

            // Get a list of thrusters that will contribute to the direction
            foreach (ThrustContribution contribution in contributions)
            {
                double dot = Vector3D.DotProduct(contribution.TranslationForceUnit, direction);

                if (dot > .05d)
                {
                    retVal.Add(Tuple.Create(contribution, contribution.TranslationForceLength * dot));
                }
            }

            retVal = FilterThrusts(retVal);

            #region Reduce Percent

            double percent = 1;

            if (retVal.Count > 0)
            {
                double force = retVal.Sum(o => o.Item2);

                //F=MA, A=F/M
                double accel = force / mass;

                if (accel > maxAcceleration)
                {
                    percent = maxAcceleration / accel;
                }
            }

            #endregion

            return retVal.Select(o => new ThrusterSetting(o.Item1.Thruster, o.Item1.Index, percent)).ToArray();     // commiting to array so that this linq isn't rerun each time it's iterated over
        }
        private static IEnumerable<ThrusterSetting> EnsureThrustKeysBuilt_Rotate(Vector3D axis, ThrustContribution[] contributions, double maxAcceleration, MassMatrix inertia)
        {
            axis = axis.ToUnit();     // doing this so the dot product can be used as a percent

            List<Tuple<ThrustContribution, double>> retVal = new List<Tuple<ThrustContribution, double>>();

            // Get a list of thrusters that will contribute to the direction
            foreach (ThrustContribution contribution in contributions)
            {
                double dot = Vector3D.DotProduct(contribution.TorqueUnit, axis);

                if (dot > .5d)
                {
                    //retVal.Add(new ThrusterSetting(contribution.Thruster, contribution.Index, 1));     // for now, just do 100%
                    retVal.Add(Tuple.Create(contribution, contribution.TorqueLength * dot));
                }
            }

            retVal = FilterThrusts(retVal);

            #region Reduce Percent

            double percent = 1;

            if (retVal.Count > 0)
            {
                double torque = retVal.Sum(o => o.Item2);

                // A=F/M
                //double accel = torque / (Vector3D.DotProduct(inertia.Inertia, axis) * inertia.Mass);
                double accel = torque / Math.Abs(Vector3D.DotProduct(inertia.Inertia, axis));

                if (accel > maxAcceleration)
                {
                    percent = maxAcceleration / accel;
                }
            }

            #endregion

            return retVal.Select(o => new ThrusterSetting(o.Item1.Thruster, o.Item1.Index, percent)).ToArray();     // commiting to array so that this linq isn't rerun each time it's iterated over
        }

        /// <summary>
        /// This removes anything that contributes less than percent of the max
        /// </summary>
        /// <remarks>
        /// I had a case where a thruster was almost in line with the center of mass, but not quite.  The dot product of unit vectors
        /// was pretty large, but the actual contribution was almost nothing.  However, firing that at 100% was completely wrong
        /// </remarks>
        private static List<Tuple<ThrustContribution, double>> FilterThrusts(List<Tuple<ThrustContribution, double>> thrusts, double percent = .1)
        {
            if (thrusts.Count == 0)
            {
                return thrusts;
            }

            double min = thrusts.Max(o => o.Item2) * percent;

            return thrusts.Where(o => o.Item2 > min).ToList();
        }

        private static ThrustContribution[] GetThrusterContributions(IEnumerable<Thruster> thrusters, Point3D center)
        {
            //This method is copied from ShipPartTesterWindow

            List<ThrustContribution> retVal = new List<ThrustContribution>();

            foreach (Thruster thruster in thrusters)
            {
                for (int cntr = 0; cntr < thruster.ThrusterDirectionsShip.Length; cntr++)
                {
                    // This is copied from Body.AddForceAtPoint

                    Vector3D offsetFromMass = thruster.Position - center;		// this is ship's local coords
                    Vector3D force = thruster.ThrusterDirectionsShip[cntr] * thruster.ForceAtMax;

                    Vector3D translationForce, torque;
                    Math3D.SplitForceIntoTranslationAndTorque(out translationForce, out torque, offsetFromMass, force);

                    retVal.Add(new ThrustContribution(thruster, cntr, translationForce, torque));
                }
            }

            // Exit Function
            return retVal.ToArray();
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
