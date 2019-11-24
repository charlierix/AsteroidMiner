using Game.Newt.v2.Arcanorum.MapObjects;
using Game.Newt.v2.GameItems;

namespace Game.Newt.v2.Arcanorum
{
    //TODO: Don't hardcode to Weapon.  Either pass in weapon as object or as T.  The problem with T is the receiver would need
    //to implement an interface for each type of object that could deal damage
    public interface ITakesDamage
    {
        /// <summary>
        /// Takes damage, returns true when destroyed
        /// </summary>
        /// <returns>
        /// null=no damage taken
        /// Item1=true if it got destroyed
        /// Item2=how much damage was inflicted
        /// </returns>
        (bool isDead, DamageProps actualDamage)? Damage(DamageProps damage, Weapon weapon = null);

        DamageProps ReceiveDamageMultipliers { get; }

        Container HitPoints { get; }
    }
}
