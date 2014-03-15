using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Game.Newt.AsteroidMiner2;
using Game.Newt.AsteroidMiner2.ShipEditor;
using Game.Newt.AsteroidMiner2.ShipParts;
using Game.Newt.HelperClasses;
using Game.Newt.NewtonDynamics;

namespace Game.Newt.Testers.FlyingBeans
{
    //TODO: Put some of this logic directly in the ship class

    public class Bean : Ship
    {
        #region Events

        public event EventHandler<DeadBeanArgs> Dead = null;

        #endregion

        #region Declaration Section

        private FlyingBeanOptions _beanOptions;

        private SortedList<CauseOfDeath, int> _dyingCounters = null;

        private int _numGroundCollisions = 0;
        private int _numBeanCollisions = 0;

        #endregion

        #region Constructor/Factory

        public static async Task<Bean> GetNewBeanAsync(EditorOptions options, ItemOptions itemOptions, FlyingBeanOptions beanOptions, ShipDNA dna, World world, int materialID, RadiationField radiation, IGravityField gravity, bool runNeural, bool repairPartPositions)
        {
            var construction = await GetNewShipConstructionAsync(options, itemOptions, dna, world, materialID, radiation, gravity, null, runNeural, repairPartPositions);

            return new Bean(construction, beanOptions);
        }

        protected Bean(ShipConstruction construction, FlyingBeanOptions beanOptions)
            : base(construction)
        {
            _beanOptions = beanOptions;

            _dyingCounters = new SortedList<CauseOfDeath, int>();
            foreach (CauseOfDeath cause in Enum.GetValues(typeof(CauseOfDeath)))
            {
                _dyingCounters.Add(cause, 0);
            }
        }

        #endregion

        #region Public Methods

        public override void Update(double elapsedTime)
        {
            base.Update(elapsedTime);

            #region Check for death

            // Energy
            if (IsDead(CauseOfDeath.ZeroEnergy, 75, this.Energy == null || this.Energy.QuantityCurrent < this.Energy.QuantityMax * .01d))		// can't check for zero because there will be a tiny bit remaining
            {
                return;
            }

            // Fuel
            if (IsDead(CauseOfDeath.ZeroFuel, 75, this.Fuel == null || this.Fuel.QuantityCurrent < this.Fuel.QuantityMax * .01d))
            {
                return;
            }

            // Spinning
            if (IsDead(CauseOfDeath.Spinning, 75, this.PhysicsBody.AngularVelocity.LengthSquared > Math.Pow(_beanOptions.AngularVelocityDeath, 2d)))
            {
                return;
            }

            // TerrainCollision
            if (IsDead(CauseOfDeath.TerrainCollision, 5, _beanOptions.MaxGroundCollisions > 0 && _numGroundCollisions > _beanOptions.MaxGroundCollisions))
            {
                return;
            }

            // BeanCollision
            if (IsDead(CauseOfDeath.BeanCollision, 0, _numBeanCollisions > 0))		// instant death.  When beans spawn inside each other, they can get pulled apart violently, and may shoot upward.  I don't want these accidents to be tracked as winners
            {
                return;
            }

            // Old age
            //TODO: Use sum of elapsed time instead of the clock - the way it's written now, the simulation can never change speed
            if (IsDead(CauseOfDeath.Old, 0, this.GetAgeSeconds() > _beanOptions.MaxAgeSeconds))
            {
                return;
            }

            #endregion
        }

        /// <summary>
        /// NOTE: This is a very flaky approach.  If the simulation is sped up or down, this is not affected.  Use base.Age instead
        /// </summary>
        public double GetAgeSeconds()
        {
            return (DateTime.Now - this.CreationTime).TotalSeconds;
        }

        public void CollidedGround()
        {
            _numGroundCollisions++;
        }
        public void CollidedBean()
        {
            _numBeanCollisions++;
        }

        #endregion
        #region Protected Methods

        protected virtual void OnDead(CauseOfDeath cause)
        {
            if (this.Dead != null)
            {
                this.Dead(this, new DeadBeanArgs(cause));
            }
        }

        #endregion

        #region Private Methods

        private bool IsDead(CauseOfDeath cause, int countdown, bool shouldDie)
        {
            if (shouldDie)
            {
                _dyingCounters[cause]++;

                if (_dyingCounters[cause] > countdown)
                {
                    OnDead(cause);
                    return true;
                }
            }
            else
            {
                _dyingCounters[cause] = 0;
            }

            return false;
        }

        #endregion
    }

    #region Enum: CauseOfDeath

    public enum CauseOfDeath
    {
        ZeroEnergy,
        ZeroFuel,
        Spinning,
        TerrainCollision,
        BeanCollision,
        Old
    }

    #endregion
    #region Class: DeadBeanArgs

    public class DeadBeanArgs : EventArgs
    {
        public DeadBeanArgs(CauseOfDeath cause)
        {
            this.Cause = cause;
        }

        public readonly CauseOfDeath Cause;
    }

    #endregion
}
