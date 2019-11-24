using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Game.Newt.v2.Arcanorum
{
    public class DamageProps
    {
        #region Constructor

        public DamageProps(Point3D? position = null, double? kinetic = null, double? pierce = null, double? slash = null/*, double? flame = null, double? freeze = null, double? electric = null, double? poison = null*/)
        {
            this.Position = position;

            this.Kinetic = kinetic;
            this.Pierce = pierce;
            this.Slash = slash;

            //this.Flame = flame;
            //this.Freeze = freeze;
            //this.Electric = electric;
            //this.Poison = poison;
        }

        #endregion

        /// <summary>
        /// This is in world coords
        /// </summary>
        public readonly Point3D? Position;

        public readonly double? Kinetic;
        public readonly double? Pierce;
        public readonly double? Slash;

        //TODO: Implement these - they will require something that applies damage over time (also may amplify damage if susceptible)
        //public readonly double? Flame;        // possibly sets on fire for a small period of time
        //public readonly double? Freeze;       // possibly slows down
        //public readonly double? Electric;     // possibly stuns or weakens
        //public readonly double? Poison;       // possibly applies damage over time

        #region Public Methods

        /// <summary>
        /// This is a helper method that adds up this instance's values, and multiplies each by the class passed in
        /// </summary>
        public double GetDamage()
        {
            double retVal = 0d;

            if (this.Kinetic != null)
            {
                retVal += this.Kinetic.Value;
            }

            if (this.Pierce != null)
            {
                retVal += this.Pierce.Value;
            }

            if (this.Slash != null)
            {
                retVal += this.Slash.Value;
            }

            return retVal;
        }
        public DamageProps GetDamage(DamageProps multipliers)
        {
            double? kinetic = null;
            if (this.Kinetic != null)
            {
                kinetic = this.Kinetic.Value * (multipliers.Kinetic ?? 1d);
            }

            double? pierce = null;
            if (this.Pierce != null)
            {
                pierce = this.Pierce.Value * (multipliers.Pierce ?? 1d);
            }

            double? slash = null;
            if (this.Slash != null)
            {
                slash = this.Slash.Value * (multipliers.Slash ?? 1d);
            }

            return new DamageProps(this.Position, kinetic, pierce, slash);
        }

        public static (bool isDead, DamageProps actualDamage) DoDamage(ITakesDamage item, DamageProps damage)
        {
            if (damage == null)
            {
                return (false, damage);
            }

            DamageProps damageActual = damage.GetDamage(item.ReceiveDamageMultipliers);

            // Remove the damage.  If something was returned, that means hitpoints are zero
            return (item.HitPoints.RemoveQuantity(damageActual.GetDamage(), false) > 0, damageActual);
        }

        public static DamageProps GetMerged(IEnumerable<DamageProps> damages)
        {
            double x = 0d;
            double y = 0d;
            double z = 0d;

            double kinetic = 0d;
            double pierce = 0d;
            double slash = 0d;

            int positionCount = 0;
            int kineticCount = 0;
            int pierceCount = 0;
            int slashCount = 0;

            foreach (DamageProps dmg in damages)
            {
                if (dmg.Position != null)
                {
                    x += dmg.Position.Value.X;
                    y += dmg.Position.Value.Y;
                    z += dmg.Position.Value.Z;
                    positionCount++;
                }

                if (dmg.Kinetic != null)
                {
                    kinetic += dmg.Kinetic.Value;
                    kineticCount++;
                }

                if (dmg.Pierce != null)
                {
                    pierce += dmg.Pierce.Value;
                    pierceCount++;
                }

                if (dmg.Slash != null)
                {
                    slash += dmg.Slash.Value;
                    slashCount++;
                }
            }

            return new DamageProps(
                positionCount == 0 ? (Point3D?)null : new Point3D(x / positionCount, y / positionCount, z / positionCount),
                kineticCount == 0 ? (double?)null : kinetic / kineticCount,
                pierceCount == 0 ? (double?)null : pierce / pierceCount,
                slashCount == 0 ? (double?)null : slash / slashCount);
        }
        public static DamageProps GetMerged(DamageProps damage1, DamageProps damage2)
        {
            return GetMerged(new[] { damage1, damage2 });
        }

        #endregion
    }
}
