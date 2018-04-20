using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;

namespace Game.Newt.v2.GameItems
{
    //TODO: ITakesDamage may want to expose events as well (TookDamage, Repaired, Destroyed, Resurrected)
    public interface ITakesDamage
    {
        //NOTE: It would be easier for the caller if positions were in world coords.  But that would be a problem for parts inside of bots

        event EventHandler Destroyed;
        event EventHandler Resurrected;

        void TakeDamage_Collision(IMapObject collidedWith, Point3D positionModel, Vector3D velocityModel, double mass1, double mass2);
        void TakeDamage_Energy(double amount, Point3D positionModel);
        void TakeDamage_Heat(double amount, Point3D positionModel);

        bool IsDestroyed { get; }

        double HitPoints_Current { get; }
        double HitPoints_Max { get; }
    }

    #region Class: TakesDamageWorker

    /// <summary>
    /// This is a helper class that holds properties about how much damage to take, multipliers for certain types, etc
    /// </summary>
    public class TakesDamageWorker
    {
        #region Declaration Section

        private readonly TakesDamageWorker_Props _default;
        private readonly Tuple<Type, TakesDamageWorker_Props>[] _typeModifiers;

        #endregion

        #region Constructor

        public TakesDamageWorker(TakesDamageWorker_Props defaultProps, Tuple<Type, TakesDamageWorker_Props>[] typeModifiers = null)
        {
            _default = defaultProps;

            if (typeModifiers != null && typeModifiers.Length > 0)
            {
                _typeModifiers = typeModifiers;
            }
            else
            {
                _typeModifiers = null;
            }
        }

        #endregion

        public double GetDamage_Collision(IMapObject collidedWith, Point3D positionModel, Vector3D velocityModel, double mass1, double mass2)
        {
            const double MAXMASSRATIO = 3;

            TakesDamageWorker_Props props = GetTypeModifier(collidedWith);

            double speed = velocityModel.Length;

            // If the impact velocity is low enough, there won't be damage
            if (speed < props.VelocityThreshold)
            {
                return 0;
            }

            // Restrict mass
            // If the difference in mass is large enough, then the larger could just be considered as a stationary object.  Including more of the larger's
            // mass would make it appear like there is more energy than there actually is (a probe hitting a large asteroid or a planet will feel the
            // same impulse, even though the masses are very different)
            UtilityCore.MinMax(ref mass1, ref mass2);
            if (mass2 > mass1 * MAXMASSRATIO)
            {
                mass2 = mass1 * MAXMASSRATIO;
            }

            // Next check is the collision energy
            // Energy = m/2 * v^2
            double energy = ((mass1 + mass2) / 2d) * speed * speed;

            //May or may not want this check
            // If one of the masses is significantly small relative to the other, then velocity will need to be very large for damage to occur

            // Next is the min energy threshold
            if (energy < props.EnergyTheshold)
            {
                return 0;
            }

            // Final step should just be to convert energy into hitpoint loss
            // HitPointLoss = Energy * c
            double retVal = energy * props.EnergyToHitpointMult;

            // Finally, run it through a randomization
            retVal *= StaticRandom.GetRandomForThread().NextBellPercent(ItemOptions.DAMAGE_RANDOMBELL, props.RandomPercent);

            //LogHit(mass1, mass2, speed, energy);

            return retVal;
        }

        #region Private Methods

        private TakesDamageWorker_Props GetTypeModifier(IMapObject collidedWith)
        {
            if (_typeModifiers == null)
            {
                return _default;
            }

            Type type = collidedWith.GetType();

            // Look for an exact type match
            var retVal = _typeModifiers.
                FirstOrDefault(o => type.Equals(o.Item1));

            if (retVal == null)
            {
                // No match, look for a match with a base type
                retVal = _typeModifiers.
                    FirstOrDefault(o => type.IsSubclassOf(o.Item1));
            }

            if (retVal == null)
            {
                return null;
            }
            else
            {
                return retVal.Item2;
            }
        }

        private static void LogHit(double mass1, double mass2, double speed, double energy)
        {
            // When you have a bunch of these files, go to command prompt,
            //      type: copy * all.txt
            //      then copy the contents into excel, chart away

            string folder = UtilityCore.GetOptionsFolder();
            folder = System.IO.Path.Combine(folder, "Logs");

            System.IO.Directory.CreateDirectory(folder);

            string filename = "collision " + Guid.NewGuid().ToString() + ".txt";
            filename = System.IO.Path.Combine(folder, filename);

            using (System.IO.StreamWriter writer = new System.IO.StreamWriter(new System.IO.FileStream(filename, System.IO.FileMode.CreateNew)))
            {
                writer.WriteLine(string.Format("mass1, mass2, speed, energy\t{0}\t{1}\t{2}\t{3}", mass1, mass2, speed, energy));
            }
        }

        #endregion
    }

    #endregion
    #region Class: TakesDamageWorker_Props

    public class TakesDamageWorker_Props
    {
        // This class's properties would be derived from characteristics like these
        //private double Elasticity;
        //private double Toughness;
        //private double Hardness;        // elasticity and toughness just go low to high, but hardness would have 3 zones:  low=soft/weak | middle=durable | high=strong but brittle

        /// <summary>
        /// If the impact velocity is less than this, then no damage
        /// </summary>
        /// <remarks>
        /// High elasticity, high toughness will increase this value
        /// </remarks>
        public double VelocityThreshold { get; set; }
        /// <summary>
        /// Even if it's a high velocity impact, low mass may cause the actual energy to be below this threshold
        /// </summary>
        /// <remarks>
        /// Toughness would increase this, maybe mid hardness
        /// </remarks>
        public double EnergyTheshold { get; set; }

        /// <summary>
        /// This will apply a randomization to the final
        /// </summary>
        /// <remarks>
        /// This uses a bell curve so that most of the time the percent is around 100%, but can go from 1/RandomPercent to RandomPercent
        /// 
        /// It think the bell curve logic is a bit overkill.  It would be simpler to do something like x^2 and a random
        /// bool for multiply vs divide
        /// </remarks>
        public double RandomPercent { get; set; }

        public double EnergyToHitpointMult { get; set; }

        public TakesDamageWorker_Props Clone()
        {
            return new TakesDamageWorker_Props()
            {
                VelocityThreshold = this.VelocityThreshold,
                EnergyTheshold = this.EnergyTheshold,
                EnergyToHitpointMult = this.EnergyToHitpointMult,
            };
        }

        public static TakesDamageWorker_Props GetStandard()
        {
            return new TakesDamageWorker_Props()
            {
                VelocityThreshold = 5,
                EnergyTheshold = 0,
                EnergyToHitpointMult = .01,
            };
        }
    }

    #endregion
}
