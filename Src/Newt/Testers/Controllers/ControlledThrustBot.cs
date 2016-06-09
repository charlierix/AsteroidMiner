using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.ShipParts;

namespace Game.Newt.Testers.Controllers
{
    public class ControlledThrustBot : Bot
    {
        #region Declaration Section

        private volatile bool _isThrustMapDirty = true;

        #endregion

        #region Constructor

        public ControlledThrustBot(BotConstruction_Result construction)
            : base(construction)
        {
        }

        #endregion

        #region Public Properties

        private volatile ThrusterMap _forwardMap = null;
        public ThrusterMap ForwardMap
        {
            get
            {
                return _forwardMap;
            }
            set
            {
                _forwardMap = value;
            }
        }

        #endregion

        #region Overrides

        private void Update_MainThread_FROMASTMINER(double elapsedTime)
        {
            //// Reset the thrusters
            ////TODO: It's ineficient to do this every tick
            //foreach (Thruster thruster in this.Thrusters)
            //{
            //    thruster.Percents = new double[thruster.ThrusterDirectionsModel.Length];
            //}

            //if (_downKeys.Count > 0)
            //{
            //    EnsureThrustKeysBuilt();

            //    foreach (var key in _downKeys)
            //    {
            //        IEnumerable<ThrusterSetting> thrusters;
            //        if (_thrustLines.TryGetValue(key, out thrusters) || _thrustLines.TryGetValue(Tuple.Create(key.Item1, (bool?)null), out thrusters))      // _downKeys will always have the bool set to true or false, but _thrustLines may have it stored as a null (null means ignore shift key)
            //        {
            //            foreach (ThrusterSetting thruster in thrusters)
            //            {
            //                //TODO: If this percent goes over 1, the Fire method will cap it.  The control theory logic will get confused, because not all of what it said was actually used
            //                thruster.Thruster.Percents[thruster.Index] += thruster.Percent;
            //            }
            //        }
            //    }
            //}

            //base.Update_MainThread(elapsedTime);
        }
        public override void Update_MainThread(double elapsedTime)
        {
            ThrusterMap forwardMap = this.ForwardMap;

            if (forwardMap != null)
            {
                //NOTE: This isn't range checking.  The map's flattened should have every thruster and thruster's direction
                foreach (var thruster in forwardMap.Flattened.ToLookup(o => o.Item1))
                {
                    double[] percents = thruster.
                        OrderBy(o => o.Item2).
                        Select(o => o.Item3).
                        ToArray();

                    this.Thrusters[thruster.Key].Percents = percents;
                }
            }
            else
            {
                foreach (Thruster thruster in this.Thrusters)
                {
                    thruster.Percents = Enumerable.Range(0, thruster.ThrusterDirectionsModel.Length).
                        Select(o => 1d).
                        ToArray();
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

        #endregion
    }
}
