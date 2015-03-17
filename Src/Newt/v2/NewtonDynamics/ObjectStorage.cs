using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Game.Newt.v2.NewtonDynamics
{
    /// <summary>
    /// This stores all the objects
    /// </summary>
    public class ObjectStorage
    {
        #region Declaration Section

        private static volatile ObjectStorage _instance;
        private static object _lockStatic = new Object();

        private object _lock;

        // The objects that are being stored
        private SortedList<long, WorldBase> _worlds;
        private SortedList<long, CollisionHull> _collisionHulls;		// these are collision shapes, not actual collisions between bodies.  Null collision shapes aren't stored here
        private SortedList<long, Body> _bodies;
        private SortedList<long, JointBase> _joints;

        #endregion

        #region Constructor

        private ObjectStorage()
        {
            _lock = new object();

            _worlds = new SortedList<long, WorldBase>();
            _collisionHulls = new SortedList<long, CollisionHull>();
            _bodies = new SortedList<long, Body>();
            _joints = new SortedList<long, JointBase>();
        }

        #endregion

        #region Public Properties

        public static ObjectStorage Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lockStatic)
                    {
                        if (_instance == null)		// make sure another thread didn't already make it
                        {
                            _instance = new ObjectStorage();
                        }
                    }
                }

                return _instance;
            }
        }

        #endregion

        #region Public Methods

        // World
        public void AddWorld(WorldBase world)
        {
            lock (_lock)
            {
                if (AddItem(_worlds, world.Handle, world))
                {
                    world.CollisionHullDestroyed += new EventHandler<CollisionHullDestroyedArgs>(World_CollisionHullDestroyed);
                }
            }
        }
        public void RemoveWorld(WorldBase world)
        {
            lock (_lock)
            {
                _worlds.Remove(world.Handle.ToInt64());
                world.CollisionHullDestroyed -= new EventHandler<CollisionHullDestroyedArgs>(World_CollisionHullDestroyed);
            }
        }

        // CollisionHull
        public void AddCollisionHull(IntPtr ptr, CollisionHull collision)
        {
            lock (_lock)
            {
                //_collisions.Add(ptr.ToInt64(), collision);
                AddItem(_collisionHulls, ptr, collision);
            }
        }
        public CollisionHull GetCollisionHull(IntPtr ptr)
        {
            lock (_lock)
            {
                return _collisionHulls[ptr.ToInt64()];
            }
        }

        // Body
        public void AddBody(IntPtr ptr, Body body)
        {
            lock (_lock)
            {
                _bodies.Add(ptr.ToInt64(), body);
            }
        }
        public void RemoveBody(IntPtr ptr)
        {
            lock (_lock)
            {
                _bodies.Remove(ptr.ToInt64());
            }
        }
        public Body GetBody(IntPtr ptr)
        {
            lock (_lock)
            {
                return _bodies[ptr.ToInt64()];
            }
        }

        // Joint (objectstorage holds the base class)
        public void AddJoint(IntPtr ptr, JointBase joint)
        {
            lock (_lock)
            {
                _joints.Add(ptr.ToInt64(), joint);
            }
        }
        public void RemoveJoint(IntPtr ptr)
        {
            lock (_lock)
            {
                _joints.Remove(ptr.ToInt64());
            }
        }
        public JointBase GetJoint(IntPtr ptr)
        {
            lock (_lock)
            {
                return _joints[ptr.ToInt64()];
            }
        }

        public void GetStats(out int worldCount, out int hullCount, out int bodyCount, out int jointCount)
        {
            lock (_lock)
            {
                worldCount = _worlds.Count;
                hullCount = _collisionHulls.Count;
                bodyCount = _bodies.Count;
                jointCount = _joints.Count;
            }
        }

        #endregion

        #region Event Listeners

        private void World_CollisionHullDestroyed(object sender, CollisionHullDestroyedArgs e)
        {
            lock (_lock)
            {
                _collisionHulls.Remove(e.CollisionHull.Handle.ToInt64());
            }
        }

        #endregion

        #region Private Methods

        private static bool AddItem<T>(SortedList<long, T> list, IntPtr ptr, T item)
        {
            long key = ptr.ToInt64();

            if (list.ContainsKey(key))
            {
                T other = list[key];
                //if (list[key] != item)
                //{
                //    throw new ApplicationException("The item passed in has the same key, but is a different instance");
                //}

                return false;
            }

            list.Add(key, item);

            return true;
        }

        #endregion
    }

    #region OLD

    ///// <summary>
    ///// This stores all the objects
    ///// </summary>
    //public class ObjectStorage_OLD
    //{
    //    #region Declaration Section

    //    private static volatile ObjectStorage_OLD _instance;
    //    private static object _lockStatic = new Object();

    //    private object _lock;

    //    // The objects that are being stored
    //    private SortedList<long, CollisionHull> _collisionHulls;		// these are collision shapes, not actual collisions between bodies.  Null collision shapes aren't stored here
    //    private SortedList<long, Body> _bodies;
    //    private SortedList<long, JointBase> _joints;

    //    #endregion

    //    #region Constructor

    //    private ObjectStorage_OLD()
    //    {
    //        _lock = new object();

    //        _collisionHulls = new SortedList<long, CollisionHull>();
    //        _bodies = new SortedList<long, Body>();
    //        _joints = new SortedList<long, JointBase>();
    //    }

    //    #endregion

    //    #region Public Properties

    //    public static ObjectStorage_OLD Instance
    //    {
    //        get
    //        {
    //            if (_instance == null)
    //            {
    //                lock (_lockStatic)
    //                {
    //                    if (_instance == null)		// make sure another thread didn't already make it
    //                    {
    //                        _instance = new ObjectStorage_OLD();
    //                    }
    //                }
    //            }

    //            return _instance;
    //        }
    //    }

    //    #endregion

    //    #region Public Methods

    //    /// <summary>
    //    /// This will remove and dispose all items stored by this class
    //    /// NOTE:  It's probably safer to pause the world before this is called
    //    /// TODO: This method is reckless.  Make an overload that takes in a world so that this doesn't ruin other window's objects
    //    /// </summary>
    //    public void ClearAll()
    //    {
    //        // Can't lock on the lock object while removing items, because the dispose methods call this class's remove, and there would be deadlock
    //        // This also means I can't do a foreach directly on the lists.  I also don't want to remove items that were added while this method is running,
    //        // so I'll do two passes.
    //        //
    //        // This doesn't save against the user calling this method a second time half way through the first one's run.  But if they do that, they deserve
    //        // the exceptions from a dual dispose
    //        JointBase[] joints = null;
    //        Body[] bodies = null;
    //        CollisionHull[] hulls = null;

    //        lock (_lock)
    //        {
    //            joints = _joints.Values.ToArray();
    //            bodies = _bodies.Values.ToArray();
    //            hulls = _collisionHulls.Values.ToArray();
    //        }

    //        // These must be done in order

    //        // Joints
    //        foreach (JointBase joint in joints)
    //        {
    //            joint.Dispose();
    //        }
    //        //_joints.Clear();		// not calling clear, because other joints could have been added while this method is running

    //        // Bodies
    //        foreach (Body body in bodies)
    //        {
    //            body.Dispose();
    //        }
    //        //_bodies.Clear();

    //        // Hulls
    //        foreach (CollisionHull hull in hulls)
    //        {
    //            hull.Dispose();
    //        }
    //        //_collisionHulls.Clear();
    //    }

    //    // CollisionHull
    //    public void AddCollisionHull(IntPtr ptr, CollisionHull collision)
    //    {
    //        lock (_lock)
    //        {
    //            //_collisions.Add(ptr.ToInt64(), collision);
    //            AddItem(_collisionHulls, ptr, collision);
    //        }
    //    }
    //    /// <summary>
    //    /// WARNING:  Don't remove hulls while there are still bodies.  The .net code may think it created two separate hulls, but newton consolidates them, so
    //    /// disposing a hull prematurely will have bad consequences for any bodies sharing that hull
    //    /// </summary>
    //    public void RemoveCollisionHull(IntPtr ptr)
    //    {
    //        lock (_lock)
    //        {
    //            _collisionHulls.Remove(ptr.ToInt64());
    //        }
    //    }
    //    public CollisionHull GetCollisionHull(IntPtr ptr)
    //    {
    //        lock (_lock)
    //        {
    //            return _collisionHulls[ptr.ToInt64()];
    //        }
    //    }

    //    // Body
    //    public void AddBody(IntPtr ptr, Body body)
    //    {
    //        lock (_lock)
    //        {
    //            _bodies.Add(ptr.ToInt64(), body);
    //        }
    //    }
    //    public void RemoveBody(IntPtr ptr)
    //    {
    //        lock (_lock)
    //        {
    //            _bodies.Remove(ptr.ToInt64());
    //        }
    //    }
    //    public Body GetBody(IntPtr ptr)
    //    {
    //        lock (_lock)
    //        {
    //            return _bodies[ptr.ToInt64()];
    //        }
    //    }

    //    // Joint (objectstorage holds the base class)
    //    public void AddJoint(IntPtr ptr, JointBase joint)
    //    {
    //        lock (_lock)
    //        {
    //            _joints.Add(ptr.ToInt64(), joint);
    //        }
    //    }
    //    public void RemoveJoint(IntPtr ptr)
    //    {
    //        lock (_lock)
    //        {
    //            _joints.Remove(ptr.ToInt64());
    //        }
    //    }
    //    public JointBase GetJoint(IntPtr ptr)
    //    {
    //        lock (_lock)
    //        {
    //            return _joints[ptr.ToInt64()];
    //        }
    //    }

    //    #endregion

    //    #region Private Methods

    //    private static bool AddItem<T>(SortedList<long, T> list, IntPtr ptr, T item)
    //    {
    //        long key = ptr.ToInt64();

    //        if (list.ContainsKey(key))
    //        {
    //            T other = list[key];
    //            //if (list[key] != item)
    //            //{
    //            //    throw new ApplicationException("The item passed in has the same key, but is a different instance");
    //            //}

    //            return false;
    //        }

    //        list.Add(key, item);

    //        return true;
    //    }

    //    #endregion
    //}

    #endregion
}
