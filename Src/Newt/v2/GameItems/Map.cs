using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using System.Xaml;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Controls3D;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.GameItems
{
    //TODO: Instead of taking in viewport as its own, take in an array of viewport setter objects:
    //      Viewport3D
    //      Thread
    //      Optional delagate: Visual3D[] GetVisual(IMapObject)
    //          once created, remember that visual for later removal

    /// <summary>
    /// This is a wrapper to the wpf viewers and newtondynamics world.  This gets handed high level objects (ship, asteroid, etc),
    /// and will update the various objects
    /// </summary>
    public class Map : IDisposable
    {
        //TODO: Make add/remove events
        //TODO: Make this class threadsafe
        //TODO: Figure out ways to allow the physics objects to be in their own thread (currently very tied to the gui thread)

        #region Class: TypeNode

        private class TypeNode
        {
            #region Constructor

            public TypeNode(Type key)
            {
                this.Key = key;
                this.ItemsLock = new object();
                this.Items = new List<IMapObject>();
            }

            #endregion

            #region Public Properties

            /// <summary>
            /// Items of this type are stored in this node
            /// </summary>
            public readonly Type Key;

            /// <summary>
            /// Use this when accessing/updating Items
            /// </summary>
            public readonly object ItemsLock;
            /// <summary>
            /// These are the items of type key
            /// </summary>
            public readonly List<IMapObject> Items;
            /// <summary>
            /// Whenever Items is added to/removed from, this gets set to null.  Whenever an array of all the items is requested, the list.toarray
            /// gets cached here.  So if this is non null, there's no need to do another .toarray
            /// </summary>
            public IMapObject[] ItemsArray = null;

            /// <summary>
            /// These child nodes are for classes that are derived from this.key
            /// </summary>
            /// <remarks>
            /// This is volatile, because the node tree can be pruned in one thread while other treads are reading/writing the items (recursing through the tree)
            /// </remarks>
            public volatile TypeNode[] DerivedChildren = null;      // no need for volatile, since all logic around this array is within reader/writer locks

            #endregion

            #region Public Methods

            public IMapObject[] GetItemsSnapshot()
            {
                lock (this.ItemsLock)
                {
                    if (this.ItemsArray == null)
                    {
                        this.ItemsArray = this.Items.ToArray();
                    }

                    return this.ItemsArray;
                }
            }

            //NOTE: These methods need to be called from inside a lock
            /// <summary>
            /// This finds a node for the type without adding nodes.  This is optimized for speed
            /// </summary>
            public static TypeNode GetNode_NoAdd(Type type, TypeNode[] nodes)
            {
                if (nodes != null)
                {
                    foreach (TypeNode node in nodes)
                    {
                        if (type.Equals(node.Key))
                        {
                            return node;
                        }

                        if (node.DerivedChildren != null && type.IsSubclassOf(node.Key))
                        {
                            TypeNode retVal = GetNode_NoAdd(type, node.DerivedChildren);
                            if (retVal != null)
                            {
                                return retVal;
                            }
                        }
                    }
                }

                return null;
            }
            /// <summary>
            /// WARNING: This should only be called when GetNode_NoAdd comes back null, because it WILL add a new
            /// node without checking to see if a node of that type was already added
            /// </summary>
            public static TypeNode GetNode_Add(Type type, ref TypeNode[] nodes)
            {
                if (nodes != null)
                {
                    for (int cntr = 0; cntr < nodes.Length; cntr++)
                    {
                        if (nodes[cntr].Key.IsSubclassOf(type))
                        {
                            #region Swap derived with base

                            List<TypeNode> list = new List<TypeNode>(nodes);

                            // The type passed in is a base of the current node.  Insert a new node between list and the current node
                            TypeNode retVal1 = new TypeNode(type);
                            List<TypeNode> childList = new List<TypeNode>();

                            childList.Add(nodes[cntr]);
                            list.RemoveAt(cntr);
                            list.Insert(cntr, retVal1);

                            // There may be other children to add to this node (siblings of the node that was swapped)
                            //NOTE: There shouldn't be any need to scan the rest of this tree for siblings
                            int index = cntr + 1;
                            while (index < list.Count)
                            {
                                if (list[index].Key.IsSubclassOf(type))
                                {
                                    childList.Add(nodes[index]);
                                    list.RemoveAt(index);
                                }
                                else
                                {
                                    index++;
                                }
                            }

                            nodes = list.ToArray();
                            retVal1.DerivedChildren = childList.ToArray();
                            return retVal1;

                            #endregion
                        }
                        else if (type.IsSubclassOf(nodes[cntr].Key))
                        {
                            #region Add derived (recurse)

                            // The type passed in derives from the current node.  Add to this node (recurse)
                            TypeNode[] reworked1 = nodes[cntr].DerivedChildren;

                            TypeNode retVal2 = GetNode_Add(type, ref reworked1);

                            nodes[cntr].DerivedChildren = reworked1;

                            return retVal2;

                            #endregion
                        }
                    }
                }

                #region Add to nodes

                // This is the start of a whole new branch
                TypeNode retVal3 = new TypeNode(type);
                nodes = UtilityCore.ArrayAdd(nodes, retVal3);
                return retVal3;

                #endregion
            }

            /// <summary>
            /// This is similar to GetNode_NoAdd, but only looks for nodes that derive from type
            /// NOTE: Once a node is found, this doesn't recurse further.  But it does look for other siblings of that found node that derive from type
            /// NOTE: Only call this if GetNode_NoAdd comes back null
            /// </summary>
            public static TypeNode[] GetDerivedNodes_NoAdd(Type type, TypeNode[] nodes)
            {
                List<TypeNode> retVal = new List<TypeNode>();

                if (nodes != null)
                {
                    foreach (TypeNode node in nodes)
                    {
                        if (node.Key.IsSubclassOf(type))
                        {
                            retVal.Add(node);
                            continue;       // don't want to recurse, because all children derive from type, and that would return too much (give the caller root nodes, and let them decide whether to flatten)
                        }

                        if (node.DerivedChildren != null)
                        {
                            retVal.AddRange(GetDerivedNodes_NoAdd(type, node.DerivedChildren));
                        }
                    }
                }

                return retVal.ToArray();
            }

            #endregion
        }

        #endregion

        #region Events

        public event EventHandler<MapItemArgs> ItemAdded = null;
        public event EventHandler<MapItemArgs> ItemRemoved = null;

        #endregion

        #region Declaration Section

        private readonly ReaderWriterLockSlim _nodeLock = new ReaderWriterLockSlim();
        private TypeNode[] _nodes = null;

        /// <summary>
        /// This isn't meant to tick in regular intervals.  When a snapshot is finished, this is enabled with an interval that will
        /// tick when the next snapshot should be built (and disabled until needed again)
        /// </summary>
        private DispatcherTimer _snapshotTimer = null;

        private List<ScreenSpaceLines3D> _snapshotLines = null;

        /// <summary>
        /// When building the octree, if a child has more than this many objects, it is processed in a new task (likely on a unique
        /// thread)
        /// </summary>
        /// <remarks>
        /// There is some expense for the task to be set up, so don't use too small of a number (the tree would be built across more
        /// threads, but it would have been cheaper to brute force in fewer threads)
        /// </remarks>
        private readonly int _snapshotCountForNewTask = 250;

        private readonly CameraPool _cameraPool;
        private readonly List<CameraPoolVisual> _cameraPoolVisuals;

        private readonly Type _typeofIMapObject = typeof(IMapObject);

        #endregion

        #region Constructor

        //TODO: Take in additional viewports, and delegates to get a visual based on item (visuals for minimap blips)
        public Map(Viewport3D viewport, CameraPool cameraPool, World world)
        {
            this.Viewport = viewport;

            _cameraPool = cameraPool;
            if (_cameraPool == null)
            {
                _cameraPoolVisuals = null;
            }
            else
            {
                // Copy the lights from the viewport
                foreach (var visual in viewport.Children)
                {
                    byte[] modelBytes = GetLightModelSerialized(visual);

                    if (modelBytes != null)
                    {
                        CameraPoolVisual lightVisual = new CameraPoolVisual(TokenGenerator.NextToken(), modelBytes, null);
                        _cameraPool.Add(lightVisual);
                    }
                }

                _cameraPoolVisuals = new List<CameraPoolVisual>();
            }

            this.World = world;
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_snapshotTimer != null)
                {
                    _snapshotTimer.IsEnabled = false;
                    _snapshotTimer = null;
                }

                // No need to do anything with the camera pool.  The caller should dispose it

                // Kill em all
                foreach (IMapObject mapObject in GetAllItems())
                {
                    if (this.ItemRemoved != null)
                    {
                        // Let the listener dispose it
                        this.ItemRemoved(this, new MapItemArgs(mapObject));
                    }
                    else
                    {
                        // No listener, dispose it
                        if (mapObject is IDisposable)
                        {
                            ((IDisposable)mapObject).Dispose();     // IMapObject doesn't implement disposable, but ship does (and possibly others in the future)
                        }
                        else if (mapObject.PhysicsBody != null)
                        {
                            mapObject.PhysicsBody.Dispose();
                        }
                    }

                    //RemoveFromViewport(mapObject);		// the map will only be disposed at the end, so the state of the viewport isn't very important
                }
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// This is the main game viewer
        /// </summary>
        public Viewport3D Viewport
        {
            get;
            private set;
        }

        /// <summary>
        /// The Newton physics world
        /// </summary>
        public World World
        {
            get;
            private set;
        }

        private bool _shouldBuildSnapshots = false;
        public bool ShouldBuildSnapshots
        {
            get
            {
                return _shouldBuildSnapshots;
            }
            set
            {
                _shouldBuildSnapshots = value;

                if (_shouldBuildSnapshots && _snapshotTimer == null)
                {
                    _snapshotTimer = new DispatcherTimer();
                    _snapshotTimer.Tick += new EventHandler(SnapshotTimer_Tick);
                }

                if (_snapshotTimer != null)
                {
                    _snapshotTimer.IsEnabled = false;
                    _snapshotTimer.Interval = TimeSpan.FromMilliseconds(this.SnapshotFequency_Milliseconds);
                    _snapshotTimer.IsEnabled = true;
                }
            }
        }

        private bool _shouldShowSnapshotLines = false;
        /// <summary>
        /// This is just for debugging to see the octree
        /// </summary>
        public bool ShouldShowSnapshotLines
        {
            get
            {
                return _shouldBuildSnapshots;
            }
            set
            {
                if (_shouldShowSnapshotLines == value)
                {
                    return;
                }

                _shouldShowSnapshotLines = value;
            }
        }

        private bool _shouldSnapshotCentersDrift = true;
        public bool ShouldSnapshotCentersDrift
        {
            get
            {
                return _shouldSnapshotCentersDrift;
            }
            set
            {
                _shouldSnapshotCentersDrift = value;
            }
        }

        private volatile object _snapshotFequency_Milliseconds = 250d;		// double can't be volatile, but object storing a double can
        /// <summary>
        /// This is how often a snapshot is built
        /// NOTE: This one is volatile, so it can safely be called from any thread
        /// </summary>
        public double SnapshotFequency_Milliseconds
        {
            get
            {
                return (double)_snapshotFequency_Milliseconds;
            }
            set
            {
                _snapshotFequency_Milliseconds = value;

                if (_snapshotTimer != null)		//	 if it's null, wait for ShouldBuildSnapshots to be set, and that will set up the timer
                {
                    _snapshotTimer.IsEnabled = false;
                    _snapshotTimer.Interval = TimeSpan.FromMilliseconds(value);
                    _snapshotTimer.IsEnabled = true;
                }
            }
        }

        private int _snapshotMaxItemsPerNode = 13;
        /// <summary>
        /// If a node holds more than this many objects, it will be divided into 8 children
        /// </summary>
        public int SnapshotMaxItemsPerNode
        {
            get
            {
                return _snapshotMaxItemsPerNode;
            }
            set
            {
                _snapshotMaxItemsPerNode = value;
            }
        }

        /// <summary>
        /// This is the latest snapshot of the object in the map.  This is meant to be read from many threads without issue (the
        /// snapshot is readonly, so can be parsed in parallel without issue)
        /// </summary>
        /// <remarks>
        /// The other instance methods of this map class can only be called from the map's thread.  This snapshot can be called
        /// from anywhere.
        /// 
        /// This snapshot is useful for seeing what objects are near points, ray casting, calculate gravity, etc
        /// </remarks>
        public volatile MapOctree LatestSnapshot = null;

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds an item to the map
        /// NOTE:  This adds/removes the visuals from the viewports
        /// NOTE:  This does NOT add/remove from world (that's done interally by the body when it is constructed/disposed)
        /// </summary>
        /// <remarks>
        /// The items are stored by type so that queries don't need to scan all objects.
        /// 
        /// This method is generic for a couple reasons:
        ///     typeof is compile time, so is faster than GetType
        ///     You can pass a base type as T so that a hierarchy of objects can be stored together - that will allow a single call to GetItem to scan multiple derived types (because I use is) - but in reality, I don't see the point in trying to micro optimize like this
        /// </remarks>
        public void AddItem<T>(T item) where T : IMapObject
        {
            TypeNode node = GetNode<T>(true);

            lock (node.ItemsLock)
            {
                node.Items.Add(item);
                node.ItemsArray = null;
            }

            //TODO: Make this threadsafe
            AddToViewport(item);

            if (this.ItemAdded != null)
            {
                this.ItemAdded(this, new MapItemArgs(item));
            }
        }
        /// <summary>
        /// Removes the item, and raises the ItemRemoved event
        /// NOTE: It's up to the main game window to handle disposing items (the easiest is to listen to ItemRemoved)
        /// </summary>
        /// <remarks>
        /// Here is a sample removed listener:
        /// 
        /// private void Map_ItemRemoved(object sender, MapItemArgs e)
        /// {
        ///     if (e.Item is IDisposable)
        ///     {
        ///         ((IDisposable)e.Item).Dispose();
        ///     }
        ///     else if (e.Item.PhysicsBody != null)
        ///     {
        ///         e.Item.PhysicsBody.Dispose();
        ///     }
        /// }
        /// </remarks>
        /// <param name="isFinalType">
        /// True: The item passed in is the same type that was added
        /// False: The item passed in was stored as another type (like as an interface)
        /// </param>
        public bool RemoveItem<T>(T item, bool isFinalType = false) where T : IMapObject
        {
            TypeNode[] nodes = GetNodeSnapshot(isFinalType ? typeof(T) : item.GetType(), true);        // it's a bit overkill to include derived children, but safer
            if (nodes.Length == 0)
            {
                // If this happens, there's a good chance that the wrong type was inferred
                return false;
            }

            foreach (TypeNode node in nodes)
            {
                lock (node.ItemsLock)
                {
                    int index = node.Items.IndexOf(item);
                    if (index < 0)
                    {
                        continue;
                    }

                    node.Items.RemoveAt(index);
                    node.ItemsArray = null;
                }

                //TODO: Make this threadsafe
                RemoveFromViewport(item);

                if (this.ItemRemoved != null)
                {
                    this.ItemRemoved(this, new MapItemArgs(item));
                }

                return true;
            }

            return false;
        }
        /// <summary>
        /// This overload is a find and remove.  It's useful for when listening to collision events.  Some types need to always
        /// be removed when collided, but all that's passed in is the body
        /// </summary>
        /// <remarks>
        /// This is a copy/tweak of the other overload
        /// </remarks>
        /// <returns>
        /// The item that was removed (or null if not found)
        /// </returns>
        public T RemoveItem<T>(Body physicsBody) where T : IMapObject
        {
            T nullT = default(T);

            TypeNode[] nodes = GetNodeSnapshot(typeof(T), true);        // it's a bit overkill to include derived children, but safer
            if (nodes.Length == 0)
            {
                // If this happens, there's a good chance that the wrong type was inferred
                return nullT;
            }

            foreach (TypeNode node in nodes)
            {
                T retVal = nullT;

                lock (node.ItemsLock)
                {
                    int index = -1;
                    for (int cntr = 0; cntr < node.Items.Count; cntr++)
                    {
                        if (node.Items[cntr].Token == physicsBody.Token)        // all map objects should use the physics body's token, so this should be safe to do
                        {
                            index = cntr;
                            retVal = (T)node.Items[cntr];
                            break;
                        }
                    }

                    if (index < 0)
                    {
                        continue;
                    }

                    node.Items.RemoveAt(index);
                    node.ItemsArray = null;
                }

                //TODO: Make this threadsafe
                RemoveFromViewport(retVal);

                if (this.ItemRemoved != null)
                {
                    this.ItemRemoved(this, new MapItemArgs(retVal));
                }

                return retVal;
            }

            return nullT;
        }

        public void Clear()
        {
            foreach (IMapObject item in GetAllItems().ToArray())
            {
                RemoveItem(item);
            }
        }

        /// <summary>
        /// This gives a dump of all the object the map knows about
        /// WARNING: If there are multiple threads, the items returned could be disposed
        /// </summary>
        /// <remarks>
        /// I didn't want map.dispose to dispose all the physics objects, so I'm exposing this method instead
        /// </remarks>
        /// <param name="includeDisposed">Only include dipsosed if you are debugging the map.  Any regular code shouldn't request disposed objects</param>
        public IEnumerable<IMapObject> GetAllItems(bool includeDisposed = false)
        {
            foreach (TypeNode node in GetNodeSnapshot())        // GetNodeSnapshot returns an array.  So a thread could change the tree around, but the foreach will be working against the array that was returned
            {
                foreach (IMapObject item in node.GetItemsSnapshot())        // this is also working against an array, so items could be added/removed from the node, and this foreach won't see those changes
                {
                    if(!includeDisposed && item.IsDisposed)
                    {
                        continue;
                    }

                    yield return item;
                }
            }
        }

        /// <summary>
        /// This returns items of a specific type
        /// WARNING: If there are multiple threads, the items returned could be disposed
        /// </summary>
        public IEnumerable<T> GetItems<T>(bool includeDerived) where T : IMapObject
        {
            Type type = typeof(T);

            foreach (TypeNode node in GetNodeSnapshot(type, includeDerived))
            {
                foreach (IMapObject item in node.GetItemsSnapshot())     // GetItemsSnapshot returns an array.  So a thread could change the node, but the foreach will be working against the array that was returned
                {
                    if (item.IsDisposed)
                    {
                        continue;
                    }

                    yield return (T)item;
                }
            }
        }
        public IEnumerable<IMapObject> GetItems(Type type, bool includeDerived)
        {
            foreach (TypeNode node in GetNodeSnapshot(type, includeDerived))
            {
                foreach (IMapObject item in node.GetItemsSnapshot())     // GetItemsSnapshot returns an array.  So a thread could change the node, but the foreach will be working against the array that was returned
                {
                    if (item.IsDisposed)
                    {
                        continue;
                    }

                    yield return item;
                }
            }
        }

        public T GetItem<T>(Body physicsBody) where T : class, IMapObject       // using the class as a where so the compiler lets me return null.  Technically, I should have the constraint be nullable, but then all consumers will have to use .Value, and the IMapObjects are always classes anyway
        {
            if (physicsBody == null)
            {
                return null;
            }

            TypeNode[] nodes = GetNodeSnapshot(typeof(T), true);
            if (nodes.Length == 0)
            {
                // Even though the exact type wasn't found, it may have been stored as a base, or T may be the base, and
                // the actual node is stored as the derived - so grab candidate nodes
                nodes = GetNodeSnapshot_Related<T>();
            }

            IMapObject retVal = GetItem(physicsBody, nodes);
            if (retVal == null)
            {
                return null;
            }
            else
            {
                return (T)retVal;
            }
        }
        public IMapObject GetItem_UnknownType(Body physicsBody)
        {
            return GetItem(physicsBody, GetNodeSnapshot());
        }
        public IMapObject GetItem_UnknownType(long token)
        {
            return GetItem(token, GetNodeSnapshot());
        }

        #endregion

        #region Event Listeners

        private void SnapshotTimer_Tick(object sender, EventArgs e)
        {
            if (_snapshotTimer == null)
            {
                return;		// I had a rare case where this tick fired after dispose
            }

            _snapshotTimer.IsEnabled = false;

            BuildSnapshot();
        }

        #endregion

        #region Private Methods

        private TypeNode GetNode<T>(bool shouldAdd) where T : IMapObject
        {
            Type type = typeof(T);
            if (type.Equals(_typeofIMapObject))
            {
                throw new ArgumentException("The item's type is IMapObject.  Please pass in as the appropriate type");
            }

            TypeNode retVal = null;

            #region Lock

            if (shouldAdd)
            {
                _nodeLock.EnterUpgradeableReadLock();       // infinite threads can be in a read lock, but only one in a readupgradable.  So only use this when necessary
            }
            else
            {
                _nodeLock.EnterReadLock();
            }

            #endregion

            try
            {
                // Find Node
                retVal = TypeNode.GetNode_NoAdd(type, _nodes);

                if (retVal == null && shouldAdd)
                {
                    _nodeLock.EnterWriteLock();
                    try
                    {
                        #region Add Node

                        retVal = TypeNode.GetNode_NoAdd(type, _nodes);      // trying again from inside the write lock to make sure another thread didn't add it first
                        if (retVal == null)
                        {
                            TypeNode[] itemsNew = _nodes;
                            retVal = TypeNode.GetNode_Add(type, ref itemsNew);
                            _nodes = itemsNew;
                        }

                        #endregion
                    }
                    finally
                    {
                        _nodeLock.ExitWriteLock();
                    }
                }
            }
            finally
            {
                #region Unlock

                if (shouldAdd)
                {
                    _nodeLock.ExitUpgradeableReadLock();
                }
                else
                {
                    _nodeLock.ExitReadLock();
                }

                #endregion
            }

            return retVal;
        }

        /// <summary>
        /// When scanning the type node tree, use this method, then iterate the results.  That way, the lock is held for
        /// as small a time as possible
        /// </summary>
        private TypeNode[] GetNodeSnapshot()
        {
            _nodeLock.EnterReadLock();
            try
            {
                if (_nodes == null)
                {
                    return new TypeNode[0];
                }
                else
                {
                    return _nodes.SelectMany(o => o.Descendants(p => p.DerivedChildren)).ToArray();
                }
            }
            finally
            {
                _nodeLock.ExitReadLock();
            }
        }
        /// <summary>
        /// This filters for a specific type, and optionally descendents
        /// </summary>
        private TypeNode[] GetNodeSnapshot(Type type, bool includeDerived)
        {
            _nodeLock.EnterReadLock();
            try
            {
                TypeNode head = TypeNode.GetNode_NoAdd(type, _nodes);

                if (head == null)
                {
                    #region No head node

                    if (includeDerived)
                    {
                        // The base type wasn't found, but maybe some derived types are in the tree
                        TypeNode[] heads = TypeNode.GetDerivedNodes_NoAdd(type, _nodes);

                        return heads.SelectMany(o => o.Descendants(p => p.DerivedChildren)).ToArray();
                    }
                    else
                    {
                        return new TypeNode[0];
                    }

                    #endregion
                }
                else
                {
                    #region Found head node

                    if (includeDerived)
                    {
                        return head.Descendants(o => o.DerivedChildren).ToArray();
                    }
                    else
                    {
                        return new TypeNode[] { head };
                    }

                    #endregion
                }
            }
            finally
            {
                _nodeLock.ExitReadLock();
            }
        }

        /// <summary>
        /// This walks the entire node tree looking for the same type, base types, derived types
        /// </summary>
        private TypeNode[] GetNodeSnapshot_Related<T>() where T : IMapObject
        {
            List<TypeNode> retVal = new List<TypeNode>();

            Type type = typeof(T);

            foreach (TypeNode node in GetNodeSnapshot())
            {
                if (type.Equals(node.Key) || node.Key.IsSubclassOf(type) || type.IsSubclassOf(node.Key))
                {
                    retVal.Add(node);
                }
            }

            return retVal.ToArray();
        }

        private static IMapObject GetItem(Body physicsBody, IEnumerable<TypeNode> nodes)
        {
            if (physicsBody == null)
            {
                return null;
            }

            foreach (TypeNode node in nodes)
            {
                foreach (IMapObject item in node.GetItemsSnapshot())
                {
                    if(item.IsDisposed)
                    {
                        continue;
                    }

                    if (item.PhysicsBody != null && item.PhysicsBody.Equals(physicsBody))
                    {
                        return item;
                    }
                }
            }

            return null;
        }
        private static IMapObject GetItem(long token, IEnumerable<TypeNode> nodes)
        {
            foreach (TypeNode node in nodes)
            {
                foreach (IMapObject item in node.GetItemsSnapshot())
                {
                    if(item.IsDisposed)
                    {
                        continue;
                    }

                    if (item.PhysicsBody != null && item.PhysicsBody.Token == token)
                    {
                        return item;
                    }
                }
            }

            return null;
        }

        private void AddToViewport(IMapObject item)
        {
            #region Main Viewport

            if (this.Viewport != null && item.Visuals3D != null)
            {
                foreach (Visual3D visual in item.Visuals3D)
                {
                    this.Viewport.Children.Add(visual);
                }
            }

            #endregion

            #region Camera Pool

            if (_cameraPool != null && item.Model != null)
            {
                byte[] modelBytes = SerializeModel(item.Model);
                CameraPoolVisual poolVisual = new CameraPoolVisual(item.PhysicsBody.Token, modelBytes, item);

                //  Add it
                _cameraPoolVisuals.Add(poolVisual);
                _cameraPool.Add(poolVisual);
            }

            #endregion
        }
        private void RemoveFromViewport(IMapObject item)
        {
            #region Main Viewport

            if (this.Viewport != null && item.Visuals3D != null)
            {
                foreach (Visual3D visual in item.Visuals3D)
                {
                    this.Viewport.Children.Remove(visual);
                }
            }

            #endregion

            #region Camera Pool

            if (_cameraPoolVisuals != null)
            {
                long token = item.PhysicsBody.Token;

                CameraPoolVisual visual = _cameraPoolVisuals.Where(o => o.Token == token).FirstOrDefault();
                if (visual != null)
                {
                    _cameraPool.Remove(visual);
                    _cameraPoolVisuals.Remove(visual);
                }
            }

            #endregion
        }

        /// <summary>
        /// This clones any models out of the visual that are lights, then serializes that clone.
        /// (returns a serialization of just the lights)
        /// </summary>
        private static byte[] GetLightModelSerialized(Visual3D visual)
        {
            ModelVisual3D visualCast = visual as ModelVisual3D;
            if (visualCast == null)
            {
                return null;
            }

            // Clone the lights (or group of lights)
            Model3D lightModel = GetLightModelSerialized_Model(visualCast.Content);
            if (lightModel == null)
            {
                return null;
            }

            // Add the visual's transform to the model's transform
            if (visualCast.Transform != null && !visualCast.Transform.Value.IsIdentity)
            {
                if (lightModel.Transform == null || lightModel.Transform.Value.IsIdentity)
                {
                    lightModel.Transform = visualCast.Transform.Clone();
                }
                else
                {
                    Transform3DGroup transformGroup = new Transform3DGroup();
                    transformGroup.Children.Add(lightModel.Transform.Clone());
                    transformGroup.Children.Add(visualCast.Transform.Clone());
                    lightModel.Transform = transformGroup;
                }
            }

            return SerializeModel(lightModel);
        }
        /// <summary>
        /// This creates a clone of only lights (or null)
        /// </summary>
        private static Model3D GetLightModelSerialized_Model(Model3D model)
        {
            if (model is Model3DGroup)
            {
                Model3DGroup retVal = new Model3DGroup();

                bool foundLight = false;

                foreach (Model3D child in ((Model3DGroup)model).Children)
                {
                    // Recurse with the child
                    Model3D childLight = GetLightModelSerialized_Model(child);

                    if (childLight != null)
                    {
                        retVal.Children.Add(childLight);
                        foundLight = true;
                    }
                }

                if (foundLight)
                {
                    if (model.Transform != null)
                    {
                        retVal.Transform = model.Transform.Clone();
                    }

                    return retVal;
                }
                else
                {
                    // None of the group's children were lights
                    return null;
                }
            }

            if (model is Light)     // AmbientLight, DirectionalLight, SpotLight
            {
                return model.Clone();
            }
            else
            {
                return null;
            }
        }

        private static byte[] SerializeModel(Model3D model)
        {
            byte[] retVal = null;

            using (MemoryStream stream = new MemoryStream())
            {
                XamlServices.Save(stream, model);
                stream.Position = 0;

                retVal = stream.ToArray();
            }

            return retVal;
        }

        private void BuildSnapshot()
        {
            DateTime startTime = DateTime.UtcNow;

            // Get these values while in the main thread
            bool shouldCentersDrift = _shouldSnapshotCentersDrift;
            int maxItemsPerNode = _snapshotMaxItemsPerNode;
            int countForNewTask = _snapshotCountForNewTask;

            // Build the octree in another thread (it's ok to store the tree from this thread, LatestSnapshot is volatile - and I don't want
            // to wait for the thread to sync up)
            var task = Task.Factory.StartNew(() =>
            {
                // Turn all the items into a snapshot
                MapObjectInfo[] mapObjects = GetNodeSnapshot().
                    SelectMany(o => o.GetItemsSnapshot().
                        Where(p => !p.IsDisposed).
                        Select(p => new MapObjectInfo(p, o.Key))).
                    ToArray();

                this.LatestSnapshot = BuildSnapshotTree(mapObjects, shouldCentersDrift, maxItemsPerNode, countForNewTask);
            });

            // After the tree is built, schedule the next build in this current thread
            task.ContinueWith(resultTask =>
            {
                // Schedule the next snapshot
                //NOTE: Don't want to recurse or set up a while loop if there is zero time remaining to build the next snapshot.  If I do, the caller will
                //wait indefinitely for this method to finish, and this thread will be completely tied up building sna
                if (_shouldBuildSnapshots && _snapshotTimer != null)		// it may have been turned off.  Ran into a case where the timer was set to null
                {
                    double nextSnapshotWaitTime = this.SnapshotFequency_Milliseconds - (DateTime.UtcNow - startTime).TotalMilliseconds;
                    if (nextSnapshotWaitTime < 1d)
                    {
                        nextSnapshotWaitTime = 1d;
                    }

                    _snapshotTimer.Interval = TimeSpan.FromMilliseconds(nextSnapshotWaitTime);
                    _snapshotTimer.IsEnabled = true;
                }

                #region Snapshot debug lines

                if (_snapshotLines != null && _snapshotLines.Count > 0)
                {
                    // Clear the old
                    foreach (ScreenSpaceLines3D line in _snapshotLines)
                    {
                        this.Viewport.Children.Remove(line);
                    }
                    _snapshotLines.Clear();
                }

                if (_shouldBuildSnapshots && _shouldShowSnapshotLines)
                {
                    if (_snapshotLines == null)
                    {
                        _snapshotLines = new List<ScreenSpaceLines3D>();
                    }

                    // Get the boundries of the nodes at each depth
                    List<List<Point3D[]>> linesByDepth = new List<List<Point3D[]>>();
                    GetSnapshotLines(linesByDepth, new MapOctree[] { this.LatestSnapshot });

                    // Draw lines
                    ShowSnapshotLines(linesByDepth);
                }

                #endregion
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        /// <summary>
        /// This overload builds a global tree
        /// </summary>
        private static MapOctree BuildSnapshotTree(MapObjectInfo[] items, bool shouldCentersDrift, int maxItemsPerNode, int snapshotCountForNewTask)
        {
            const double RANGE = .05d;		// this will make the surrounding box 10% bigger than normal.  This will avoid objects sitting right on the boundries (so that a < check may fail, but <= would have worked)

            if (items == null || items.Length == 0)
            {
                return new MapOctree(new Point3D(-1, -1, -1), new Point3D(1, 1, 1), new Point3D(0, 0, 0), null);
            }

            // Calculate the extremes
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double minZ = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;
            double maxZ = double.MinValue;

            for (int cntr = 0; cntr < items.Length; cntr++)
            {
                #region Examine Object

                MapObjectInfo item = items[cntr];

                // Min
                if (item.AABBMin.X < minX)
                {
                    minX = item.AABBMin.X;
                }

                if (item.AABBMin.Y < minY)
                {
                    minY = item.AABBMin.Y;
                }

                if (item.AABBMin.Z < minZ)
                {
                    minZ = item.AABBMin.Z;
                }

                // Max
                if (item.AABBMax.X > maxX)
                {
                    maxX = item.AABBMax.X;
                }

                if (item.AABBMax.Y > maxY)
                {
                    maxY = item.AABBMax.Y;
                }

                if (item.AABBMax.Z > maxZ)
                {
                    maxZ = item.AABBMax.Z;
                }

                #endregion
            }

            // Add a bit to the range
            double rangeX = (maxX - minX) * RANGE;
            double rangeY = (maxY - minY) * RANGE;
            double rangeZ = (maxZ - minZ) * RANGE;

            Point3D minRange = new Point3D(minX - rangeX, minY - rangeY, minZ - rangeZ);
            Point3D maxRange = new Point3D(maxX + rangeX, maxY + rangeY, maxZ + rangeZ);

            // Call the other overload
            return BuildSnapshotTree(items, minRange, maxRange, shouldCentersDrift, maxItemsPerNode, snapshotCountForNewTask);
        }
        /// <summary>
        /// This overload builds a tree node (and recurses for children)
        /// </summary>
        private static MapOctree BuildSnapshotTree(MapObjectInfo[] items, Point3D minRange, Point3D maxRange, bool shouldCentersDrift, int maxItemsPerNode, int snapshotCountForNewTask)
        {
            const double CENTERDRIFT = .05d;		// Instead of the center point always being dead center, this will offset up to this random percent, to help avoid strange stabilities

            double centerX = (minRange.X + maxRange.X) * .5d;
            double centerY = (minRange.Y + maxRange.Y) * .5d;
            double centerZ = (minRange.Z + maxRange.Z) * .5d;

            if (items.Length <= maxItemsPerNode)
            {
                // Leaf node
                return new MapOctree(minRange, maxRange, new Point3D(centerX, centerY, centerZ), items);		// don't bother drifing the center point, octree requires centerpoint, but I doubt it will be used much for leaf nodes
            }

            #region Drift center point

            // I only want to take the expense of doing the random if there will be children

            if (shouldCentersDrift)
            {
                Random rand = StaticRandom.GetRandomForThread();

                double drift = (maxRange.X - minRange.X) * (rand.NextDouble() * CENTERDRIFT);
                centerX += rand.Next(2) == 0 ? -drift : drift;

                drift = (maxRange.Y - minRange.Y) * (rand.NextDouble() * CENTERDRIFT);
                centerY += rand.Next(2) == 0 ? -drift : drift;

                drift = (maxRange.Z - minRange.Z) * (rand.NextDouble() * CENTERDRIFT);
                centerZ += rand.Next(2) == 0 ? -drift : drift;
            }

            Point3D centerPoint = new Point3D(centerX, centerY, centerZ);

            #endregion

            #region Divide the items

            List<MapObjectInfo> local = new List<MapObjectInfo>();
            List<MapObjectInfo> itemsX0_Y0_Z0 = new List<MapObjectInfo>();
            List<MapObjectInfo> itemsX0_Y0_Z1 = new List<MapObjectInfo>();
            List<MapObjectInfo> itemsX0_Y1_Z0 = new List<MapObjectInfo>();
            List<MapObjectInfo> itemsX0_Y1_Z1 = new List<MapObjectInfo>();
            List<MapObjectInfo> itemsX1_Y0_Z0 = new List<MapObjectInfo>();
            List<MapObjectInfo> itemsX1_Y0_Z1 = new List<MapObjectInfo>();
            List<MapObjectInfo> itemsX1_Y1_Z0 = new List<MapObjectInfo>();
            List<MapObjectInfo> itemsX1_Y1_Z1 = new List<MapObjectInfo>();

            for (int cntr = 0; cntr < items.Length; cntr++)
            {
                if (items[cntr].AABBMax.X < centerPoint.X)
                {
                    if (items[cntr].AABBMax.Y < centerPoint.Y)
                    {
                        if (items[cntr].AABBMax.Z < centerPoint.Z)
                        {
                            itemsX0_Y0_Z0.Add(items[cntr]);
                        }
                        else if (items[cntr].AABBMin.Z > centerPoint.Z)
                        {
                            itemsX0_Y0_Z1.Add(items[cntr]);
                        }
                        else
                        {
                            // Straddles Z
                            local.Add(items[cntr]);
                        }
                    }
                    else if (items[cntr].AABBMin.Y > centerPoint.Y)
                    {
                        if (items[cntr].AABBMax.Z < centerPoint.Z)
                        {
                            itemsX0_Y1_Z0.Add(items[cntr]);
                        }
                        else if (items[cntr].AABBMin.Z > centerPoint.Z)
                        {
                            itemsX0_Y1_Z1.Add(items[cntr]);
                        }
                        else
                        {
                            // Straddles Z
                            local.Add(items[cntr]);
                        }
                    }
                    else
                    {
                        // Straddles Y
                        local.Add(items[cntr]);
                    }
                }
                else if (items[cntr].AABBMin.X > centerPoint.X)
                {
                    if (items[cntr].AABBMax.Y < centerPoint.Y)
                    {
                        if (items[cntr].AABBMax.Z < centerPoint.Z)
                        {
                            itemsX1_Y0_Z0.Add(items[cntr]);
                        }
                        else if (items[cntr].AABBMin.Z > centerPoint.Z)
                        {
                            itemsX1_Y0_Z1.Add(items[cntr]);
                        }
                        else
                        {
                            // Straddles Z
                            local.Add(items[cntr]);
                        }
                    }
                    else if (items[cntr].AABBMin.Y > centerPoint.Y)
                    {
                        if (items[cntr].AABBMax.Z < centerPoint.Z)
                        {
                            itemsX1_Y1_Z0.Add(items[cntr]);
                        }
                        else if (items[cntr].AABBMin.Z > centerPoint.Z)
                        {
                            itemsX1_Y1_Z1.Add(items[cntr]);
                        }
                        else
                        {
                            // Straddles Z
                            local.Add(items[cntr]);
                        }
                    }
                    else
                    {
                        // Straddles Y
                        local.Add(items[cntr]);
                    }
                }
                else
                {
                    // Straddles X
                    local.Add(items[cntr]);
                }
            }

            #endregion

            #region Build child nodes

            MapOctree childX0_Y0_Z0 = null;
            MapOctree childX0_Y0_Z1 = null;
            MapOctree childX0_Y1_Z0 = null;
            MapOctree childX0_Y1_Z1 = null;
            MapOctree childX1_Y0_Z0 = null;
            MapOctree childX1_Y0_Z1 = null;
            MapOctree childX1_Y1_Z0 = null;
            MapOctree childX1_Y1_Z1 = null;

            //TODO: Spin up tasks where appropriate

            if (itemsX0_Y0_Z0.Count > 0)
            {
                childX0_Y0_Z0 = BuildSnapshotTree(itemsX0_Y0_Z0.ToArray(),
                    new Point3D(minRange.X, minRange.Y, minRange.Z),
                    new Point3D(centerPoint.X, centerPoint.Y, centerPoint.Z),
                    shouldCentersDrift, maxItemsPerNode, snapshotCountForNewTask);
            }

            if (itemsX0_Y0_Z1.Count > 0)
            {
                childX0_Y0_Z1 = BuildSnapshotTree(itemsX0_Y0_Z1.ToArray(),
                    new Point3D(minRange.X, minRange.Y, centerPoint.Z),
                    new Point3D(centerPoint.X, centerPoint.Y, maxRange.Z),
                    shouldCentersDrift, maxItemsPerNode, snapshotCountForNewTask);
            }

            if (itemsX0_Y1_Z0.Count > 0)
            {
                childX0_Y1_Z0 = BuildSnapshotTree(itemsX0_Y1_Z0.ToArray(),
                    new Point3D(minRange.X, centerPoint.Y, minRange.Z),
                    new Point3D(centerPoint.X, maxRange.Y, centerPoint.Z),
                    shouldCentersDrift, maxItemsPerNode, snapshotCountForNewTask);
            }

            if (itemsX0_Y1_Z1.Count > 0)
            {
                childX0_Y1_Z1 = BuildSnapshotTree(itemsX0_Y1_Z1.ToArray(),
                    new Point3D(minRange.X, centerPoint.Y, centerPoint.Z),
                    new Point3D(centerPoint.X, maxRange.Y, maxRange.Z),
                    shouldCentersDrift, maxItemsPerNode, snapshotCountForNewTask);
            }

            if (itemsX1_Y0_Z0.Count > 0)
            {
                childX1_Y0_Z0 = BuildSnapshotTree(itemsX1_Y0_Z0.ToArray(),
                    new Point3D(centerPoint.X, minRange.Y, minRange.Z),
                    new Point3D(maxRange.X, centerPoint.Y, centerPoint.Z),
                    shouldCentersDrift, maxItemsPerNode, snapshotCountForNewTask);
            }

            if (itemsX1_Y0_Z1.Count > 0)
            {
                childX1_Y0_Z1 = BuildSnapshotTree(itemsX1_Y0_Z1.ToArray(),
                    new Point3D(centerPoint.X, minRange.Y, centerPoint.Z),
                    new Point3D(maxRange.X, centerPoint.Y, maxRange.Z),
                    shouldCentersDrift, maxItemsPerNode, snapshotCountForNewTask);
            }

            if (itemsX1_Y1_Z0.Count > 0)
            {
                childX1_Y1_Z0 = BuildSnapshotTree(itemsX1_Y1_Z0.ToArray(),
                    new Point3D(centerPoint.X, centerPoint.Y, minRange.Z),
                    new Point3D(maxRange.X, maxRange.Y, centerPoint.Z),
                    shouldCentersDrift, maxItemsPerNode, snapshotCountForNewTask);
            }

            if (itemsX1_Y1_Z1.Count > 0)
            {
                childX1_Y1_Z1 = BuildSnapshotTree(itemsX1_Y1_Z1.ToArray(),
                    new Point3D(centerPoint.X, centerPoint.Y, centerPoint.Z),
                    new Point3D(maxRange.X, maxRange.Y, maxRange.Z),
                    shouldCentersDrift, maxItemsPerNode, snapshotCountForNewTask);
            }

            #endregion

            // Exit Function
            return new MapOctree(minRange, maxRange, centerPoint,
                local.Count == 0 ? null : local.ToArray(),
                childX0_Y0_Z0, childX0_Y0_Z1, childX0_Y1_Z0, childX0_Y1_Z1, childX1_Y0_Z0, childX1_Y0_Z1, childX1_Y1_Z0, childX1_Y1_Z1);
        }

        private static void GetSnapshotLines(List<List<Point3D[]>> linesByDepth, IEnumerable<MapOctree> nodesAtDepth)
        {
            List<Point3D[]> lines = new List<Point3D[]>();
            List<MapOctree> childDepthNodes = new List<MapOctree>();

            foreach (MapOctree node in nodesAtDepth)
            {
                if (node.Items != null)
                {
                    #region Add to the lines

                    // Top
                    lines.Add(new Point3D[] { new Point3D(node.MinRange.X, node.MinRange.Y, node.MinRange.Z), new Point3D(node.MaxRange.X, node.MinRange.Y, node.MinRange.Z) });
                    lines.Add(new Point3D[] { new Point3D(node.MaxRange.X, node.MinRange.Y, node.MinRange.Z), new Point3D(node.MaxRange.X, node.MaxRange.Y, node.MinRange.Z) });
                    lines.Add(new Point3D[] { new Point3D(node.MaxRange.X, node.MaxRange.Y, node.MinRange.Z), new Point3D(node.MinRange.X, node.MaxRange.Y, node.MinRange.Z) });
                    lines.Add(new Point3D[] { new Point3D(node.MinRange.X, node.MaxRange.Y, node.MinRange.Z), new Point3D(node.MinRange.X, node.MinRange.Y, node.MinRange.Z) });

                    // Bottom
                    lines.Add(new Point3D[] { new Point3D(node.MinRange.X, node.MinRange.Y, node.MaxRange.Z), new Point3D(node.MaxRange.X, node.MinRange.Y, node.MaxRange.Z) });
                    lines.Add(new Point3D[] { new Point3D(node.MaxRange.X, node.MinRange.Y, node.MaxRange.Z), new Point3D(node.MaxRange.X, node.MaxRange.Y, node.MaxRange.Z) });
                    lines.Add(new Point3D[] { new Point3D(node.MaxRange.X, node.MaxRange.Y, node.MaxRange.Z), new Point3D(node.MinRange.X, node.MaxRange.Y, node.MaxRange.Z) });
                    lines.Add(new Point3D[] { new Point3D(node.MinRange.X, node.MaxRange.Y, node.MaxRange.Z), new Point3D(node.MinRange.X, node.MinRange.Y, node.MaxRange.Z) });

                    // Sides
                    lines.Add(new Point3D[] { new Point3D(node.MinRange.X, node.MinRange.Y, node.MinRange.Z), new Point3D(node.MinRange.X, node.MinRange.Y, node.MaxRange.Z) });
                    lines.Add(new Point3D[] { new Point3D(node.MaxRange.X, node.MinRange.Y, node.MinRange.Z), new Point3D(node.MaxRange.X, node.MinRange.Y, node.MaxRange.Z) });
                    lines.Add(new Point3D[] { new Point3D(node.MaxRange.X, node.MaxRange.Y, node.MinRange.Z), new Point3D(node.MaxRange.X, node.MaxRange.Y, node.MaxRange.Z) });
                    lines.Add(new Point3D[] { new Point3D(node.MinRange.X, node.MaxRange.Y, node.MinRange.Z), new Point3D(node.MinRange.X, node.MaxRange.Y, node.MaxRange.Z) });

                    #endregion
                }

                #region Add nonnull children

                if (node.X0_Y0_Z0 != null)
                {
                    childDepthNodes.Add(node.X0_Y0_Z0);
                }

                if (node.X0_Y0_Z1 != null)
                {
                    childDepthNodes.Add(node.X0_Y0_Z1);
                }

                if (node.X0_Y1_Z0 != null)
                {
                    childDepthNodes.Add(node.X0_Y1_Z0);
                }

                if (node.X0_Y1_Z1 != null)
                {
                    childDepthNodes.Add(node.X0_Y1_Z1);
                }

                if (node.X1_Y0_Z0 != null)
                {
                    childDepthNodes.Add(node.X1_Y0_Z0);
                }

                if (node.X1_Y0_Z1 != null)
                {
                    childDepthNodes.Add(node.X1_Y0_Z1);
                }

                if (node.X1_Y1_Z0 != null)
                {
                    childDepthNodes.Add(node.X1_Y1_Z0);
                }

                if (node.X1_Y1_Z1 != null)
                {
                    childDepthNodes.Add(node.X1_Y1_Z1);
                }

                #endregion
            }

            linesByDepth.Add(lines);

            if (childDepthNodes.Count > 0)
            {
                GetSnapshotLines(linesByDepth, childDepthNodes);
            }
        }
        private void ShowSnapshotLines(List<List<Point3D[]>> linesByDepth)
        {
            const double MINHUE = 0d;		// red
            const double MAXHUE = 260d;		// purple
            const int DEPTHMAX = 5;

            for (int cntr = 0; cntr < linesByDepth.Count; cntr++)
            {
                ScreenSpaceLines3D model = new ScreenSpaceLines3D(true);
                double hue = UtilityCore.GetScaledValue_Capped(MINHUE, MAXHUE, 0, DEPTHMAX, cntr);
                hue = MAXHUE - hue;		// flip it so the bigger the cntr, the closer to red
                model.Color = UtilityWPF.HSVtoRGB(32, hue, 100, 100);
                model.Thickness = (cntr + 1) * .25d;

                foreach (Point3D[] line in linesByDepth[cntr])
                {
                    model.AddLine(line[0], line[1]);
                }

                _snapshotLines.Add(model);
                this.Viewport.Children.Add(model);
            }
        }

        #endregion
    }

    #region Class: MapItemArgs

    public class MapItemArgs : EventArgs
    {
        public MapItemArgs(IMapObject item)
        {
            this.Item = item;
        }

        public readonly IMapObject Item;
    }

    #endregion

    #region Class: MapOctree

    //TODO: It would be cool to have a parent property, but since the entire object is readonly, all children must be built
    //before the parent is built

    /// <summary>
    /// This class is meant to hold a completly readonly snapshot of the map.  All fields are immutable, so can
    /// be safely shared across threads without any locks
    /// </summary>
    /// <remarks>
    /// NOTE: Speed is critical within this class, so no linq
    /// </remarks>
    public class MapOctree
    {
        #region Constructor

        /// <summary>
        /// This overload defines a parent
        /// </summary>
        public MapOctree(Point3D minRange, Point3D maxRange, Point3D centerPoint, MapObjectInfo[] items)
        {
            this.Token = TokenGenerator.NextToken();
            this.MinRange = minRange;
            this.MaxRange = maxRange;
            this.CenterPoint = centerPoint;
            this.Items = items;

            this.HasChildren = false;
            this.X0_Y0_Z0 = null;
            this.X0_Y0_Z1 = null;
            this.X0_Y1_Z0 = null;
            this.X0_Y1_Z1 = null;
            this.X1_Y0_Z0 = null;
            this.X1_Y0_Z1 = null;
            this.X1_Y1_Z0 = null;
            this.X1_Y1_Z1 = null;
        }
        public MapOctree(Point3D minRange, Point3D maxRange, Point3D centerPoint, MapObjectInfo[] items, MapOctree x0_y0_z0, MapOctree x0_y0_z1, MapOctree x0_y1_z0, MapOctree x0_y1_z1, MapOctree x1_y0_z0, MapOctree x1_y0_z1, MapOctree x1_y1_z0, MapOctree x1_y1_z1)
        {
            this.Token = TokenGenerator.NextToken();
            this.MinRange = minRange;
            this.MaxRange = maxRange;
            this.CenterPoint = centerPoint;
            this.Items = items;

            this.HasChildren = true;
            this.X0_Y0_Z0 = x0_y0_z0;
            this.X0_Y0_Z1 = x0_y0_z1;
            this.X0_Y1_Z0 = x0_y1_z0;
            this.X0_Y1_Z1 = x0_y1_z1;
            this.X1_Y0_Z0 = x1_y0_z0;
            this.X1_Y0_Z1 = x1_y0_z1;
            this.X1_Y1_Z0 = x1_y1_z0;
            this.X1_Y1_Z1 = x1_y1_z1;
        }

        #endregion

        #region Public Properties

        public readonly long Token;

        // It can't be enforced by the compiler, but if this is a parent, then each child will be an even subdivision of this volume (1/8th)
        public readonly Point3D MinRange;
        public readonly Point3D MaxRange;

        /// <summary>
        /// This is useful for figuring out which child tree to look in
        /// NOTE: use LessThanOrEqual, and GreaterThan for doing comparisons (never GreaterThanOrEqual)
        /// </summary>
        public readonly Point3D CenterPoint;

        /// <summary>
        /// C# doesn't have an immutable array, and I don't want to take the expense of wrapping this in IEnumerable
        /// or ReadOnlyCollectionBase.  So if you change out items, then you are a very BAD BAD person - don't do it.
        /// </summary>
        /// <remarks>
        /// It's possible for a parent node to have some items, and child nodes to also have items.  The items at the parent
        /// would straddle multiple children
        /// </remarks>
        public readonly MapObjectInfo[] Items;

        public readonly bool HasChildren;

        /// <summary>
        /// This iterates over the non null child nodes
        /// </summary>
        /// <remarks>
        /// If you want to walk the whole tree linq style, use the extension method in HelperClassesCore:
        /// snapshot.Descendants(o => o.Children).Select(~~~
        /// </remarks>
        public IEnumerable<MapOctree> Children
        {
            get
            {
                return UtilityCore.Iterate<MapOctree>(
                    X0_Y0_Z0,
                    X0_Y0_Z1,
                    X0_Y1_Z0,
                    X0_Y1_Z1,
                    X1_Y0_Z0,
                    X1_Y0_Z1,
                    X1_Y1_Z0,
                    X1_Y1_Z1);
            }
        }

        // Since an octree has 8 children, each child corresponds to a +-X, +-Y, +-Z
        // For naming, negative is 0, positive is 1.  So X0Y0Z0 is the bottom left back cell, and X1Y1Z1 is the top right front cell
        public readonly MapOctree X0_Y0_Z0;
        public readonly MapOctree X0_Y0_Z1;
        public readonly MapOctree X0_Y1_Z0;
        public readonly MapOctree X0_Y1_Z1;
        public readonly MapOctree X1_Y0_Z0;
        public readonly MapOctree X1_Y0_Z1;
        public readonly MapOctree X1_Y1_Z0;
        public readonly MapOctree X1_Y1_Z1;

        #endregion

        #region Public Methods

        public IEnumerable<MapObjectInfo> GetItems()
        {
            List<MapObjectInfo> retVal = new List<MapObjectInfo>();

            if (this.Items != null)
            {
                retVal.AddRange(this.Items.Where(o => !o.IsDisposed));
            }

            if (this.HasChildren)
            {
                if (this.X0_Y0_Z0 != null)
                {
                    retVal.AddRange(this.X0_Y0_Z0.GetItems());
                }

                if (this.X0_Y0_Z1 != null)
                {
                    retVal.AddRange(this.X0_Y0_Z1.GetItems());
                }

                if (this.X0_Y1_Z0 != null)
                {
                    retVal.AddRange(this.X0_Y1_Z0.GetItems());
                }

                if (this.X0_Y1_Z1 != null)
                {
                    retVal.AddRange(this.X0_Y1_Z1.GetItems());
                }

                if (this.X1_Y0_Z0 != null)
                {
                    retVal.AddRange(this.X1_Y0_Z0.GetItems());
                }

                if (this.X1_Y0_Z1 != null)
                {
                    retVal.AddRange(this.X1_Y0_Z1.GetItems());
                }

                if (this.X1_Y1_Z0 != null)
                {
                    retVal.AddRange(this.X1_Y1_Z0.GetItems());
                }

                if (this.X1_Y1_Z1 != null)
                {
                    retVal.AddRange(this.X1_Y1_Z1.GetItems());
                }
            }

            return retVal;
        }
        public IEnumerable<MapObjectInfo> GetItems(Point3D center, double searchRadius)
        {
            List<MapObjectInfo> retVal = new List<MapObjectInfo>();

            if (this.Items != null)
            {
                foreach (MapObjectInfo item in this.Items)
                {
                    if (Math3D.IsIntersecting_AABB_Sphere(item.AABBMin, item.AABBMax, center, searchRadius))
                    {
                        retVal.Add(item);
                    }
                }
            }

            if (this.HasChildren)
            {
                if (this.X0_Y0_Z0 != null && Math3D.IsIntersecting_AABB_Sphere(this.X0_Y0_Z0.MinRange, this.X0_Y0_Z0.MaxRange, center, searchRadius))
                {
                    retVal.AddRange(this.X0_Y0_Z0.GetItems(center, searchRadius));
                }

                if (this.X0_Y0_Z1 != null && Math3D.IsIntersecting_AABB_Sphere(this.X0_Y0_Z1.MinRange, this.X0_Y0_Z1.MaxRange, center, searchRadius))
                {
                    retVal.AddRange(this.X0_Y0_Z1.GetItems(center, searchRadius));
                }

                if (this.X0_Y1_Z0 != null && Math3D.IsIntersecting_AABB_Sphere(this.X0_Y1_Z0.MinRange, this.X0_Y1_Z0.MaxRange, center, searchRadius))
                {
                    retVal.AddRange(this.X0_Y1_Z0.GetItems(center, searchRadius));
                }

                if (this.X0_Y1_Z1 != null && Math3D.IsIntersecting_AABB_Sphere(this.X0_Y1_Z1.MinRange, this.X0_Y1_Z1.MaxRange, center, searchRadius))
                {
                    retVal.AddRange(this.X0_Y1_Z1.GetItems(center, searchRadius));
                }

                if (this.X1_Y0_Z0 != null && Math3D.IsIntersecting_AABB_Sphere(this.X1_Y0_Z0.MinRange, this.X1_Y0_Z0.MaxRange, center, searchRadius))
                {
                    retVal.AddRange(this.X1_Y0_Z0.GetItems(center, searchRadius));
                }

                if (this.X1_Y0_Z1 != null && Math3D.IsIntersecting_AABB_Sphere(this.X1_Y0_Z1.MinRange, this.X1_Y0_Z1.MaxRange, center, searchRadius))
                {
                    retVal.AddRange(this.X1_Y0_Z1.GetItems(center, searchRadius));
                }

                if (this.X1_Y1_Z0 != null && Math3D.IsIntersecting_AABB_Sphere(this.X1_Y1_Z0.MinRange, this.X1_Y1_Z0.MaxRange, center, searchRadius))
                {
                    retVal.AddRange(this.X1_Y1_Z0.GetItems(center, searchRadius));
                }

                if (this.X1_Y1_Z1 != null && Math3D.IsIntersecting_AABB_Sphere(this.X1_Y1_Z1.MinRange, this.X1_Y1_Z1.MaxRange, center, searchRadius))
                {
                    retVal.AddRange(this.X1_Y1_Z1.GetItems(center, searchRadius));
                }
            }

            return retVal;
        }
        public IEnumerable<MapObjectInfo> GetItems(Point3D min, Point3D max)
        {
            List<MapObjectInfo> retVal = new List<MapObjectInfo>();

            if (this.Items != null)
            {
                foreach (MapObjectInfo item in this.Items)
                {
                    if (Math3D.IsIntersecting_AABB_AABB(item.AABBMin, item.AABBMax, min, max))
                    {
                        retVal.AddRange(this.Items);
                    }
                }
            }

            if (this.HasChildren)
            {
                if (this.X0_Y0_Z0 != null && Math3D.IsIntersecting_AABB_AABB(this.X0_Y0_Z0.MinRange, this.X0_Y0_Z0.MaxRange, min, max))
                {
                    retVal.AddRange(this.X0_Y0_Z0.GetItems(min, max));
                }

                if (this.X0_Y0_Z1 != null && Math3D.IsIntersecting_AABB_AABB(this.X0_Y0_Z1.MinRange, this.X0_Y0_Z1.MaxRange, min, max))
                {
                    retVal.AddRange(this.X0_Y0_Z1.GetItems(min, max));
                }

                if (this.X0_Y1_Z0 != null && Math3D.IsIntersecting_AABB_AABB(this.X0_Y1_Z0.MinRange, this.X0_Y1_Z0.MaxRange, min, max))
                {
                    retVal.AddRange(this.X0_Y1_Z0.GetItems(min, max));
                }

                if (this.X0_Y1_Z1 != null && Math3D.IsIntersecting_AABB_AABB(this.X0_Y1_Z1.MinRange, this.X0_Y1_Z1.MaxRange, min, max))
                {
                    retVal.AddRange(this.X0_Y1_Z1.GetItems(min, max));
                }

                if (this.X1_Y0_Z0 != null && Math3D.IsIntersecting_AABB_AABB(this.X1_Y0_Z0.MinRange, this.X1_Y0_Z0.MaxRange, min, max))
                {
                    retVal.AddRange(this.X1_Y0_Z0.GetItems(min, max));
                }

                if (this.X1_Y0_Z1 != null && Math3D.IsIntersecting_AABB_AABB(this.X1_Y0_Z1.MinRange, this.X1_Y0_Z1.MaxRange, min, max))
                {
                    retVal.AddRange(this.X1_Y0_Z1.GetItems(min, max));
                }

                if (this.X1_Y1_Z0 != null && Math3D.IsIntersecting_AABB_AABB(this.X1_Y1_Z0.MinRange, this.X1_Y1_Z0.MaxRange, min, max))
                {
                    retVal.AddRange(this.X1_Y1_Z0.GetItems(min, max));
                }

                if (this.X1_Y1_Z1 != null && Math3D.IsIntersecting_AABB_AABB(this.X1_Y1_Z1.MinRange, this.X1_Y1_Z1.MaxRange, min, max))
                {
                    retVal.AddRange(this.X1_Y1_Z1.GetItems(min, max));
                }
            }

            return retVal;
        }
        public IEnumerable<MapObjectInfo> GetItems(Point3D rayStart, Vector3D rayDirection, double coneAngle, double searchRadius)
        {
            throw new NotSupportedException("finish this");
        }

        public static IEnumerable<MapObjectInfo> FilterType<T>(IEnumerable<MapObjectInfo> items, bool includeDerived)
        {
            return FilterType(typeof(T), items, includeDerived);
        }
        public static IEnumerable<MapObjectInfo> FilterType(Type type, IEnumerable<MapObjectInfo> items, bool includeDerived)
        {
            foreach (MapObjectInfo item in items)
            {
                if (type.Equals(item.MapObjectType))
                {
                    yield return item;
                }
                else if (includeDerived && item.MapObjectType.IsSubclassOf(type))
                {
                    yield return item;
                }
            }
        }

        #endregion
    }

    #endregion
    #region Class: MapObjectInfo

    public class MapObjectInfo
    {
        #region Constructor

        public MapObjectInfo(IMapObject mapObject, Type mapObjectType)
        {
            this.Token = mapObject.Token;
            this.MapObject = mapObject;
            this.MapObjectType = mapObjectType;
            this.Position = mapObject.PositionWorld;
            this.Velocity = mapObject.VelocityWorld;
            this.Mass = mapObject.PhysicsBody == null ? 0d : mapObject.PhysicsBody.Mass;
            this.Radius = mapObject.Radius;

            //NOTE: This is a bit bigger than what newton would report as the AABB, but is very fast to calculate
            this.AABBMin = new Point3D(this.Position.X - this.Radius, this.Position.Y - this.Radius, this.Position.Z - this.Radius);
            this.AABBMax = new Point3D(this.Position.X + this.Radius, this.Position.Y + this.Radius, this.Position.Z + this.Radius);
        }

        #endregion

        public readonly long Token;

        /// <summary>
        /// WARNING: The properties/methods of mapobject aren't thread safe (and it could be disposed any time after this node is created)
        /// </summary>
        public readonly IMapObject MapObject;
        public readonly Type MapObjectType;

        public readonly Point3D Position;
        public readonly Vector3D Velocity;

        public readonly double Mass;
        public readonly double Radius;

        public readonly Point3D AABBMin;
        public readonly Point3D AABBMax;

        public bool IsDisposed
        {
            get
            {
                if (this.MapObject != null)
                {
                    return this.MapObject.IsDisposed;
                }
                else
                {
                    return false;
                }
            }
        }
    }

    #endregion
}
