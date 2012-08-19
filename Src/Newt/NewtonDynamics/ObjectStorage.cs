using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Game.Newt.NewtonDynamics
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

		//	The objects that are being stored
		private SortedList<long, CollisionHull> _collisionHulls;		//	these are collision shapes, not actual collisions between bodies.  Null collision shapes aren't stored here
		private SortedList<long, Body> _bodies;
		private SortedList<long, JointBase> _joints;

		private long _nextToken;

		#endregion

		#region Constructor

		private ObjectStorage()
		{
			_lock = new object();

			_nextToken = 0;

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
						if (_instance == null)		//	make sure another thread didn't already make it
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

		/// <summary>
		/// This hands out a unique ID each time it's called
		/// </summary>
		public long GetNextToken()
		{
			lock (_lock)
			{
				long retVal = _nextToken;
				_nextToken++;
				return retVal;
			}
		}

		/// <summary>
		/// This will remove and dispose all items stored by this class
		/// NOTE:  It's probably safer to pause the world before this is called
		/// </summary>
		public void ClearAll()
		{
			//	Can't lock on the lock object while removing items, because the dispose methods call this class's remove, and there would be deadlock
			//	This also means I can't do a foreach directly on the lists.  I also don't want to remove items that were added while this method is running,
			//	so I'll do two passes.
			//
			//	This doesn't save against the user calling this method a second time half way through the first one's run.  But if they do that, they deserve
			//	the exceptions from a dual dispose
			JointBase[] joints = null;
			Body[] bodies = null;
			CollisionHull[] hulls = null;

			lock (_lock)
			{
				joints = _joints.Values.ToArray();
				bodies = _bodies.Values.ToArray();
				hulls = _collisionHulls.Values.ToArray();
			}

			//	These must be done in order

			//	Joints
			foreach (JointBase joint in joints)
			{
				joint.Dispose();
			}
			//_joints.Clear();		//	not calling clear, because other joints could have been added while this method is running

			//	Bodies
			foreach (Body body in bodies)
			{
				body.Dispose();
			}
			//_bodies.Clear();

			//	Hulls
			foreach (CollisionHull hull in hulls)
			{
				hull.Dispose();
			}
			//_collisionHulls.Clear();
		}

		//	CollisionHull
		public void AddCollisionHull(IntPtr ptr, CollisionHull collision)
		{
			lock (_lock)
			{
				//_collisions.Add(ptr.ToInt64(), collision);
				AddItem(_collisionHulls, ptr, collision);
			}
		}
		/// <summary>
		/// WARNING:  Don't remove hulls while there are still bodies.  The .net code may think it created two separate hulls, but newton consolidates them, so
		/// disposing a hull prematurely will have bad consequences for any bodies sharing that hull
		/// </summary>
		public void RemoveCollisionHull(IntPtr ptr)
		{
			lock (_lock)
			{
				_collisionHulls.Remove(ptr.ToInt64());
			}
		}
		public CollisionHull GetCollisionHull(IntPtr ptr)
		{
			lock (_lock)
			{
				return _collisionHulls[ptr.ToInt64()];
			}
		}

		//	Body
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

		//	Joint (objectstorage holds the base class)
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
}
