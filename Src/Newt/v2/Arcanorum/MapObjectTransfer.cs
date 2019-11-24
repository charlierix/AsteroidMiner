using Game.HelperClassesWPF;
using Game.Newt.v2.Arcanorum.MapObjects;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.NewtonDynamics;
using System;
using System.Windows.Controls;
using System.Windows.Media.Media3D;

namespace Game.Newt.v2.Arcanorum
{
    /// <summary>
    /// This is a helper class that will transfer items to/from the map
    /// </summary>
    /// <remarks>
    /// This is currently hardcoded to arc bot and weapon.  Hopefully as other types of items are needed to be attached, common
    /// logic can be shared
    /// </remarks>
    public static class MapObjectTransfer
    {
        public static bool AddToInventory(MapObjectTransferArgs args, Weapon newWeapon, ItemToFrom newFrom = ItemToFrom.Nowhere)
        {
            newWeapon.ShowAttachPoint = true;

            switch (newFrom)
            {
                case ItemToFrom.Nowhere:
                    if (newWeapon.IsGraphicsOnly)
                    {
                        args.Inventory.Weapons.Add(newWeapon);
                    }
                    else
                    {
                        // The inventory can only hold graphics only, so convert it and dispose the physics version
                        args.Inventory.Weapons.Add(new Weapon(newWeapon.DNA, new Point3D(), null, args.MaterialID_Weapon));
                        newWeapon.Dispose();
                    }
                    return true;

                case ItemToFrom.Inventory:      // there should be no reason to call this method when it's already in inventory
                    return true;

                case ItemToFrom.Map:

                    //TODO: Ask inventory first
                    //if (!args.Inventory.CanTake(newWeapon))
                    //{
                    //    return false;
                    //}


                    // Clone the weapon, but no physics
                    Weapon nonPhysics = new Weapon(newWeapon.DNA, new Point3D(), null, args.MaterialID_Weapon);

                    // Dispose the physics version
                    if (args.ShouldKeep2D)
                        args.KeepItems2D.Remove(newWeapon);

                    args.Map.RemoveItem(newWeapon, true);     // the map also removes from the viewport
                    //_viewport.Children.RemoveAll(newWeapon.Visuals3D);

                    nonPhysics.ShowAttachPoint = true;

                    // Add to inventory
                    args.Inventory.Weapons.Add(nonPhysics);
                    return true;

                default:
                    throw new ApplicationException($"Unknown ItemToFrom: {newFrom}");
            }
        }
        public static void RemoveFromInventory(MapObjectTransferArgs args, Weapon removeWeapon, ItemToFrom existingTo = ItemToFrom.Nowhere)
        {
            switch (existingTo)
            {
                case ItemToFrom.Inventory:      // there should be no reason to call this method when it's going back into inventory
                    break;

                case ItemToFrom.Nowhere:
                    // Make sure it's in the inventory
                    if (args.Inventory.Weapons.Remove(removeWeapon))
                    {
                        removeWeapon.Dispose();
                    }
                    break;

                case ItemToFrom.Map:
                    // Make sure it's in the inventory
                    if (args.Inventory.Weapons.Remove(removeWeapon))
                    {
                        // Convert from graphics only to a physics weapon
                        Weapon physicsWeapon = new Weapon(removeWeapon.DNA, new Point3D(), args.World, args.MaterialID_Weapon);

                        removeWeapon.Dispose();

                        physicsWeapon.ShowAttachPoint = true;

                        SetDropOffset(args.Bot, physicsWeapon);

                        if (args.ShouldKeep2D)
                            args.KeepItems2D.Add(physicsWeapon, false);

                        args.Map.AddItem(physicsWeapon);
                    }
                    break;

                default:
                    throw new ApplicationException("Unknown ItemToFrom: " + existingTo.ToString());
            }
        }

        //TODO: Make this function more generic so it's not just arcanorum weapons
        /// <summary>
        /// This attaches the weapon passed in, and returns the previous weapon.  Also optionally manages the weapon's states within
        /// the map or inventory
        /// </summary>
        /// <param name="newFrom">If from map or inventory, this will remove it from those places</param>
        /// <param name="existingTo">If told to, will add the existing back into inventory or map</param>
        public static void AttachWeapon(MapObjectTransferArgs args, ref JointBase weaponAttachJoint, ref Weapon weapon, Weapon newWeapon, ItemToFrom newFrom = ItemToFrom.Nowhere, ItemToFrom existingTo = ItemToFrom.Nowhere)
        {
            // Pause the world
            bool shouldResume = false;
            if (!args.World.IsPaused)
            {
                shouldResume = true;
                args.World.Pause();
            }

            // Unhook previous weapon (remove joint)
            AttachWeapon_UnhookPrevious(ref weaponAttachJoint);

            // Hand off existing weapon
            if (weapon != null)
            {
                AttachWeapon_HandOffWeapon(args, weapon, existingTo);
            }

            // Take new weapon
            AttachWeapon_TakeNewWeapon(args, ref newWeapon, newFrom);

            // Hook up new weapon (add joint)
            AttachWeapon_HookupNew(args, ref weaponAttachJoint, ref weapon, newWeapon);

            // In order for semitransparency to work, this bot's visuals must be added to the viewport last
            if (newWeapon != null && args.Viewport != null && args.Visuals3D != null)
            {
                args.Viewport.Children.RemoveAll(args.Visuals3D);
                args.Viewport.Children.AddRange(args.Visuals3D);
            }

            // Resume the world
            if (shouldResume)
            {
                args.World.UnPause();
            }
        }

        /// <summary>
        /// This figures out where to drop the item
        /// </summary>
        public static void SetDropOffset(IMapObject bot, IMapObject item)
        {
            if (item.PhysicsBody == null)
            {
                throw new ArgumentException("The physics body should be populated for this item");
            }

            //TODO: See if there are other items that this item may collide with
            Vector3D offset = Math3D.GetRandomVector_Circular_Shell((bot.Radius * 3d) + (item.Radius * 2));

            item.PhysicsBody.Position = bot.PositionWorld + offset;

            Vector3D botVelocity = bot.VelocityWorld;
            item.PhysicsBody.Velocity = botVelocity + (offset.ToUnit() * (botVelocity.Length * .25d));     // don't perfectly mirror bot's velocity, push it away as well (otherwise it's too easy to run into and pick back up)
        }

        #region Private Methods

        private static void AttachWeapon_UnhookPrevious(ref JointBase weaponAttachJoint)
        {
            if (weaponAttachJoint != null)
            {
                weaponAttachJoint.Dispose();
            }

            weaponAttachJoint = null;
        }
        private static void AttachWeapon_HookupNew(MapObjectTransferArgs args, ref JointBase weaponAttachJoint, ref Weapon weapon, Weapon newWeapon)
        {
            weapon = newWeapon;

            if (weapon != null)
            {
                Point3D position = args.Bot.PositionWorld;

                // Move the weapon into position
                weapon.MoveToAttachPoint(position);

                JointBallAndSocket ballAndSocket = JointBallAndSocket.CreateBallAndSocket(args.World, position, args.Bot.PhysicsBody, weapon.PhysicsBody);
                ballAndSocket.ShouldLinkedBodiesCollideEachOther = false;

                weaponAttachJoint = ballAndSocket;

                weapon.Gravity = args.Gravity;
            }
        }

        private static void AttachWeapon_HandOffWeapon(MapObjectTransferArgs args, Weapon existing, ItemToFrom existingTo)
        {
            existing.Gravity = null;

            existing.ShowAttachPoint = true;

            switch (existingTo)
            {
                case ItemToFrom.Nowhere:
                    // Dispose the existing weapon
                    if (args.ShouldKeep2D)
                        args.KeepItems2D.Remove(existing);

                    if (args.Viewport != null && existing.Visuals3D != null)
                    {
                        args.Viewport.Children.RemoveAll(existing.Visuals3D);
                    }
                    existing.Dispose();
                    break;

                case ItemToFrom.Map:
                    // Give the weapon back to the map
                    SetDropOffset(args.Bot, existing);

                    if (args.Viewport != null && existing.Visuals3D != null)
                    {
                        args.Viewport.Children.RemoveAll(existing.Visuals3D);       // Map adds to the viewport, so remove from viewport first
                    }
                    args.Map.AddItem(existing);

                    // no need to add to _keepItems2D, because the existing was added to it when hooked up to this bot
                    break;

                case ItemToFrom.Inventory:
                    // Clone the weapon, but no physics
                    Weapon nonPhysics = new Weapon(existing.DNA, new Point3D(), null, args.MaterialID_Weapon);

                    // Dispose the physics object
                    if (args.ShouldKeep2D)
                        args.KeepItems2D.Remove(existing);

                    if (args.Viewport != null && existing.PhysicsBody.Visuals != null)
                    {
                        args.Viewport.Children.RemoveAll(existing.PhysicsBody.Visuals);
                    }
                    existing.Dispose();

                    // Add to inventory
                    args.Inventory.Weapons.Add(nonPhysics);
                    break;

                default:
                    throw new ApplicationException($"Unknown ItemToFrom: {existingTo}");
            }
        }
        private static void AttachWeapon_TakeNewWeapon(MapObjectTransferArgs args, ref Weapon newWeapon, ItemToFrom newFrom)
        {
            if (newWeapon == null)
            {
                return;
            }

            newWeapon.ShowAttachPoint = false;

            switch (newFrom)
            {
                case ItemToFrom.Nowhere:
                    if (args.ShouldKeep2D)
                        args.KeepItems2D.Add(newWeapon, true);

                    if (args.Viewport != null && newWeapon.Visuals3D != null)
                    {
                        args.Viewport.Children.AddRange(newWeapon.Visuals3D);
                    }
                    break;

                case ItemToFrom.Map:
                    args.Map.RemoveItem(newWeapon, true, false);     // this also removes from the viewport
                    if (args.Viewport != null && newWeapon.Visuals3D != null)
                    {
                        args.Viewport.Children.AddRange(newWeapon.Visuals3D);       // put the visuals back
                    }
                    break;

                case ItemToFrom.Inventory:
                    // Make sure it's in the inventory
                    if (args.Inventory.Weapons.Remove(newWeapon))
                    {
                        // Convert from graphics only to a physics weapon
                        Weapon physicsWeapon = new Weapon(newWeapon.DNA, new Point3D(), args.World, args.MaterialID_Weapon);

                        // Swap them
                        newWeapon.Dispose();
                        newWeapon = physicsWeapon;

                        newWeapon.ShowAttachPoint = false;

                        if (args.ShouldKeep2D)
                            args.KeepItems2D.Add(newWeapon, true);

                        if (args.Viewport != null && newWeapon.Visuals3D != null)
                        {
                            args.Viewport.Children.AddRange(newWeapon.Visuals3D);
                        }
                    }
                    break;

                default:
                    throw new ApplicationException($"Unknown ItemToFrom: {newFrom}");
            }
        }

        #endregion
    }

    #region enum: ItemToFrom

    public enum ItemToFrom
    {
        Nowhere,
        Map,
        Inventory
    }

    #endregion
    #region class: MapObjectTransferArgs

    public class MapObjectTransferArgs
    {
        public World World { get; set; }
        public Map Map { get; set; }
        public Inventory Inventory { get; set; }
        public int MaterialID_Weapon { get; set; }

        public IMapObject Bot { get; set; }

        public bool ShouldKeep2D { get; set; }
        public KeepItems2D KeepItems2D { get; set; }

        public IGravityField Gravity { get; set; }

        public Viewport3D Viewport { get; set; }
        public Visual3D[] Visuals3D { get; set; }
    }

    #endregion
}
