using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Media3D;

using Game.HelperClassesWPF;
using Game.Newt.v1.NewtonDynamics1.Api;

namespace Game.Newt.v1.NewtonDynamics1
{
    public class MaterialPhysics
    {
        #region Class: MaterialProps

        private class MaterialProps
        {
            // These defaults come from the newton wiki

            /// <summary>
            /// elasticCoef must be a positive value. It is recommended that elasticCoef be set to a value lower or equal to 1.0, default value is 0.4
            /// </summary>
            public double Elasticity = .4d;

            // positive values from 0 to 1
            public double StaticFriction = .9d;
            public double KineticFriction = .4d;

            /// <summary>
            /// This should only be true for small and fast moving objects (tells the engine to be more careful when calculating collisions,
            /// but has a 40% - 80% speed penalty)
            /// </summary>
            /// <remarks>
            /// http://www.newtondynamics.com/wiki/index.php5?title=NewtonMaterialSetContinuousCollisionMode
            /// </remarks>
            public bool ContinuousCollision = false;

            public MaterialProps(double elasticity, double staticFriction, double kineticFriction, bool continuousCollision)
            {
                this.Elasticity = elasticity;
                this.StaticFriction = staticFriction;
                this.KineticFriction = kineticFriction;
                this.ContinuousCollision = continuousCollision;
            }
        }

        #endregion
        #region Class: CollisionEventProps

        private class CollisionEventProps
        {
            // These are setup once, when collision events are requested for this material pair
            public CollisionStartHandler CollisionStart = null;
            public CollisionEndHandler CollisionEnd = null;

            // --- Everything below is reset during the contact begin listener

            // These are the bodies that are currently colliding
            public object CollisionBody1 = null;
            public object CollisionBody2 = null;

            public CMaterial Material1 = null;
            public CMaterial Material2 = null;

            // The begin event fires, and if the user didn't cancel, then the contact event fires
            /// <summary>
            /// The public contact event should only be raised once, but newton could call the callback multiple times per collision
            /// </summary>
            public bool HasRaisedContactEvent = false;
            /// <summary>
            /// Holds whether the user cancelled from within the contact event.  Since the user only sees the first contact event, all
            /// others for the current collision will need to be auto cancelled, based on this initial decision
            /// </summary>
            public bool HasContactCancelled = false;
        }

        #endregion

        #region Declaration Section

        private World _world = null;

        private CMaterialPhysics _materialHelper = null;

        // These lists are kept the same size
        List<MaterialProps> _materialProps = new List<MaterialProps>();
        List<CMaterial> _materials = new List<CMaterial>();

        /// <summary>
        /// This holds on to collision events (currently only one event listener allowed for each material pair)
        /// </summary>
        private SortedList<string, CollisionEventProps> _collisionListeners = new SortedList<string, CollisionEventProps>();

        /// <summary>
        /// e.Material is different between ContactBegin and ContactProcess, so I have to store this key during ContactBegin, and trust that
        /// ContactProcess is called in order -- REALLY need to port to newton 2
        /// </summary>
        private string _currentCollidingKey = null;

        #endregion

        #region Constructor

        public MaterialPhysics(World world)
        {
            _world = world;
            _materialHelper = new CMaterialPhysics(_world.NewtonWorld);
        }

        #endregion

        #region Public Methods

        public int AddMaterial(double elasticity, double staticFriction, double kineticFriction, bool continuousCollision)
        {
            _materialProps.Add(new MaterialProps(elasticity, staticFriction, kineticFriction, continuousCollision));
            _materials.Add(new CMaterial(_world.NewtonWorld, _materials.Count == 0));

            int index = _materials.Count - 1;

            // Don't expose softness, it's going to be removed from future verions.  It defines how far two objects should penetrate before colliding (or
            // maybe spreads the impact out a bit?) - NOTE:  it wasn't removed from 2

            // Newton needs the collision properties set across all permutations of materials, so add this to existing materials (even against itself)
            for (int cntr = 0; cntr < _materials.Count; cntr++)
            {
                _materialHelper.SetDefaultCollidable(_materials[cntr], _materials[index], 1);       // true
                _materialHelper.SetDefaultSoftness(_materials[cntr], _materials[index], .11f);

                if (_materialProps[cntr].ContinuousCollision || _materialProps[index].ContinuousCollision)
                {
                    _materialHelper.SetContinuousCollisionMode(_materials[cntr], _materials[index], 1);
                }
                else
                {
                    _materialHelper.SetContinuousCollisionMode(_materials[cntr], _materials[index], 0);
                }

                float avgElasticity = Convert.ToSingle((_materialProps[cntr].Elasticity + _materialProps[index].Elasticity) / 2d);
                _materialHelper.SetDefaultElasticity(_materials[cntr], _materials[index], avgElasticity);

                float avgStaticFriction = Convert.ToSingle((_materialProps[cntr].StaticFriction + _materialProps[index].StaticFriction) / 2d); ;
                float avgKineticFriction = Convert.ToSingle((_materialProps[cntr].KineticFriction + _materialProps[index].KineticFriction) / 2d); ;
                _materialHelper.SetDefaultFriction(_materials[cntr], _materials[index], avgStaticFriction, avgKineticFriction);
            }

            // Exit Function
            return _materials[index].ID;
        }

        /// <summary>
        /// This lets the consumer get notified when two bodies of the material type are colliding
        /// NOTE:  Currently, only one listener per matieral pair is supported
        /// </summary>
        public void SetCollisionCallback(int material1, int material2, CollisionStartHandler collisionStart, CollisionEndHandler collisionEnd)
        {
            string key = GetMaterialComboHash(material1, material2);
            if (_collisionListeners.ContainsKey(key))
            {
                // Newton 2.0 has a much better collision event system (when listening to the ContactProcess event, all I have is the material, not
                // bodies, so I have to keep track of bodies by material to make it easier on the consumer.  This is all based on the fact that I receive
                // all contact events in order for the current collision of two bodies, then I'll get a different ContactBegin/ContactProcess set for another
                // two bodies)
                throw new ApplicationException("Currently, only one event listener is allowed for each material pair -- upgrade to newton 2");
            }

            // Store the delegates for this material pair
            CollisionEventProps eventProps = new CollisionEventProps();
            eventProps.CollisionStart = collisionStart;
            eventProps.CollisionEnd = collisionEnd;
            _collisionListeners.Add(key, eventProps);

            // Set up a callback for this material pair
            //NOTE:  The callback goes to my event listener.  I then make the args easier for the consumer, and call the delegates that were passed in
            _materialHelper.SetCollisionCallback(_materials[material1], _materials[material2], null, ContactBegin, ContactProcess, null);
        }

        #endregion

        #region Event Listeners

        private void ContactBegin(object sender, Game.Newt.v1.NewtonDynamics1.Api.CContactBeginEventArgs e)
        {
            #region Get Keys

            // Get the entry for this material pair
            string key = GetMaterialComboHash(e.Body0.MaterialGroupID, e.Body1.MaterialGroupID);
            if (!_collisionListeners.ContainsKey(key))
            {
                throw new ApplicationException("There is no listener for this material pair, but how did this callback get set up???: " + key);
            }

            // Just to complicate things, the contact process callback doesn't give me bodies, and e.Material is different, so I have store the key,
            // and trust everything is called in order
            _currentCollidingKey = key;

            #endregion

            // Reset for this current collision between these two bodies
            CollisionEventProps eventProps = _collisionListeners[key];
            eventProps.CollisionBody1 = e.Body0.UserData;
            eventProps.CollisionBody2 = e.Body1.UserData;
            eventProps.Material1 = _materials[e.Body0.MaterialGroupID];
            eventProps.Material2 = _materials[e.Body1.MaterialGroupID];
            eventProps.HasRaisedContactEvent = false;
            eventProps.HasContactCancelled = false;

            if (eventProps.CollisionStart != null)
            {
                // Raise an event to the outside
                CollisionStartEventArgs args = new CollisionStartEventArgs();
                args.Body1 = e.Body0.UserData;
                args.Body2 = e.Body1.UserData;

                eventProps.CollisionStart(this, args);

                // Tell the caller what the user decided
                e.AllowCollision = args.AllowCollision;

                // This shouldn't matter, because the ContactProcess shouldn't be called if they cancelled, but I'm just being safe
                if (!args.AllowCollision)
                {
                    eventProps.HasContactCancelled = true;
                }
            }
        }
        private void ContactProcess(object sender, Game.Newt.v1.NewtonDynamics1.Api.CContactProcessEventArgs e)
        {
            #region Get Keys

            string key = _currentCollidingKey;
            if (key == null)
            {
                throw new ApplicationException("ContactProcess was called before ContactBegin");
            }
            else if (!_collisionListeners.ContainsKey(key))
            {
                throw new ApplicationException("There is no listener for this material pair, but how did this callback get set up???: " + key);
            }

            #endregion

            // See if I should duck out early
            CollisionEventProps eventProps = _collisionListeners[key];
            if (eventProps.HasRaisedContactEvent || eventProps.HasContactCancelled)       // if they cancelled in the begin event, but for some reason this process event still fires, I don't want them to see it
            {
                if (eventProps.HasContactCancelled)
                {
                    e.AllowCollision = false;
                }
                return;
            }

            // Remember that I've raised the event to the outside (so they only get informed once)
            eventProps.HasRaisedContactEvent = true;

            // Validate
            if (eventProps.CollisionBody1 == null || eventProps.CollisionBody2 == null)
            {
                throw new ApplicationException("Received a collision contact event without the start event");
            }

            if (eventProps.CollisionEnd != null)
            {
                // Raise an event to the outside
                CollisionEndEventArgs args = new CollisionEndEventArgs(e.Material, e.Contact, eventProps.Material1, eventProps.Material2);
                args.Body1 = eventProps.CollisionBody1;
                args.Body2 = eventProps.CollisionBody2;

                eventProps.CollisionEnd(this, args);

                e.AllowCollision = args.AllowCollision;
            }

            if (!e.AllowCollision)
            {
                eventProps.HasContactCancelled = true;
            }
        }
        private void ContactEnd(object sender, Game.Newt.v1.NewtonDynamics1.Api.CContactEndEventArgs e)
        {
            // No need to listen to the end event (I'm just putting it here for reference)
        }

        #endregion

        #region Private Methods

        private static string GetMaterialComboHash(int materialID1, int materialID2)
        {
            // Always return the lowest value first
            if (materialID1 < materialID2)
            {
                return materialID1.ToString() + "|" + materialID2.ToString();
            }
            else
            {
                return materialID2.ToString() + "|" + materialID1.ToString();
            }
        }

        #endregion
    }

    #region Collision Notification Events

    public delegate void CollisionStartHandler(object sender, CollisionStartEventArgs e);
    public class CollisionStartEventArgs : EventArgs
    {
        public object Body1 = null;
        public object Body2 = null;

        public bool AllowCollision = true;
    }

    public delegate void CollisionEndHandler(object sender, CollisionEndEventArgs e);
    public class CollisionEndEventArgs : EventArgs
    {
        public object Body1 = null;
        public object Body2 = null;

        //NOTE: With newt 1.53, I can't figure out how to build up all the contacts AND let the user stop the collision, so I'll raise this the first
        // time the contact occurs, only
        //private List<IntPtr> ContactPoints = null;
        private IntPtr _contactPoint = IntPtr.Zero;
        private IntPtr _materialPtr = IntPtr.Zero;

        private CMaterial _material1 = null;
        private CMaterial _material2 = null;

        //TODO:  Expose public gets that ask newton about this contact, using _contactPoint as the key
        // I think I need to call CMaterial.(one of the methods with Contact in its name???) to get the contact details???
        //
        // Here is the list of possible things I can do with the contact pointer
        // http://www.newtondynamics.com/wiki/index.php5?title=NewtonContactProcess

        private Point3D? _contactPositionWorld = null;
        public Point3D ContactPositionWorld
        {
            get
            {
                if (_contactPositionWorld == null)
                {
                    GetContactPositionaAndNormal();
                }

                return _contactPositionWorld.Value;
            }
        }
        private Vector3D? _contactNormalWorld = null;
        public Vector3D ContactNormalWorld
        {
            get
            {
                if (_contactNormalWorld == null)
                {
                    GetContactPositionaAndNormal();
                }

                return _contactNormalWorld.Value;
            }
        }

        private double? _contactNormalSpeed = null;
        public double ContactNormalSpeed
        {
            get
            {
                if (_contactNormalSpeed == null)
                {
                    _contactNormalSpeed = _material1.GetContactNormalSpeed(_contactPoint);
                }

                return _contactNormalSpeed.Value;
            }
        }

        private Vector3D? _contactForceWorld = null;
        public Vector3D ContactForceWorld
        {
            get
            {
                if (_contactForceWorld == null)
                {
                    //_contactForceWorld = _material1.GetContactForce();

                    NewtonVector3 aForce = new NewtonVector3(new Vector3D());
                    Newton.NewtonMaterialGetContactForce(_materialPtr, aForce.NWVector3);
                    _contactForceWorld = aForce.ToDirectX();

                }

                return _contactForceWorld.Value;
            }
        }

        private Vector3D? _contactTangentDirection1 = null;
        public Vector3D ContactTangentDirection1
        {
            get
            {
                if (_contactTangentDirection1 == null)
                {
                    GetContactTangentDirections();
                }

                return _contactTangentDirection1.Value;
            }
        }
        private Vector3D? _contactTangentDirection2 = null;
        public Vector3D ContactTangentDirection2
        {
            get
            {
                if (_contactTangentDirection2 == null)
                {
                    GetContactTangentDirections();
                }

                return _contactTangentDirection2.Value;
            }
        }


        public bool AllowCollision = true;

        #region Constructor

        public CollisionEndEventArgs(IntPtr materialPtr, IntPtr contactPoint, CMaterial material1, CMaterial material2)
        {
            _materialPtr = materialPtr;
            _contactPoint = contactPoint;
            _material1 = material1;
            _material2 = material2;
        }

        #endregion

        #region Private Methods

        private void GetContactPositionaAndNormal()
        {
            Vector3D position = new Vector3D();
            Vector3D normal = new Vector3D();
            //_material1.GetContactPositionAndNormal(ref position, ref normal);

            Newton.NewtonMaterialGetContactPositionAndNormal(_materialPtr, new NewtonVector3(position).NWVector3, new NewtonVector3(normal).NWVector3);


            _contactPositionWorld = position.ToPoint();
            _contactNormalWorld = normal;
        }

        private void GetContactTangentDirections()
        {
            Vector3D dir1 = new Vector3D();
            Vector3D dir2 = new Vector3D();
            _material1.GetContactTangentDirections(ref dir1, ref dir2);

            _contactTangentDirection1 = dir1;
            _contactTangentDirection2 = dir2;
        }

        #endregion
    }

    #endregion
}
