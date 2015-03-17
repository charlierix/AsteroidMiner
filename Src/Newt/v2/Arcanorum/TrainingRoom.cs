using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.Arcanorum
{
    /// <summary>
    /// This is a self contained world.  Its purpose is to train a bot (or more accurately a lineage of bot).  Set up rules, and this will keep trying
    /// mutations of the bot to get a winning design
    /// </summary>
    /// <remarks>
    /// TODO: Support hooking in a camera to view the scene.  For this reason, this class shouldn't create threads, it should just run in whatver thread
    /// it was instantiated in
    /// 
    /// I decided to make this class 1 to 1 with world, map, etc.  Let a higher level class manage multiple rooms, and how many threads to use
    /// 
    /// Ideas for types of behaviors to reward/punish:
    ///     Easy:
    ///         Punish if stationary too long
    ///         Punish if too erratic
    ///         Collect treasure (any, or reward for certain types - punish for others)
    ///         Take damage if too far from nest (bot needs a homing sensor)
    /// 
    ///     Medium:
    ///         Ram things (treasure boxes, enemy bots)
    ///         Take damage if ram the wrong things (friendly bots)
    ///         Get a light weapon swinging as fast as possible
    ///         Get a heavy weapon to swing (raise above, swing down)
    ///         Go from A to B (or follow some kind of patrol path)
    /// 
    ///     Hard:
    ///         Swarming rules (1: go to center of flock 2: don't get too close to neighbors 3: match speed of flock)
    ///         Kill non family bots (or just kill any bot)
    ///         Defend the nest from intruders
    ///         Trade with other nests
    ///         Gather treasure, and bring back to the nest
    ///         Arrange treasure to spell obscene words
    ///         Gang up and mug another bot (beat them until they drop their weapon)
    /// </remarks>
    public class TrainingRoom
    {
        #region Declaration Section

        private const double BOUNDRYSIZE = 100;
        private const double BOUNDRYSIZEHALF = BOUNDRYSIZE * .5d;

        private Point3D _boundryMin;
        private Point3D _boundryMax;

        private World _world = null;
        private Map _map = null;
        private GravityFieldUniform _gravity = null;

        private EditorOptions _editorOptions = null;
        private ItemOptionsArco _itemOptions = null;

        private MaterialManager _materialManager = null;
        private MaterialIDs _materialIDs = null;

        //System.Timers.Timer test2 = new System.Timers.Timer() { SynchronizingObject = this };




        #endregion

        public void Update()
        {
            _world.Update();
        }



    }
}
