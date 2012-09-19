using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Media3D;

using Game.Newt.AsteroidMiner2.MapParts;
using Game.Newt.HelperClasses;
using Game.Newt.NewtonDynamics;
using System.Windows.Threading;
using Game.Newt.HelperClasses.Primitives3D;
using Game.HelperClasses;

namespace Game.Newt.AsteroidMiner2
{
	/// <summary>
	/// This is a wrapper to the wpf viewers and newtondynamics world.  This gets handed high level objects (ship, asteroid, etc),
	/// and will update the various objects
	/// </summary>
	public class Map : IDisposable
	{
		//TODO: Make add/remove events
		//TODO: Figure out ways to allow the physics objects to be in their own thread (currently very tied to the gui thread)

		#region Declaration Section

		private List<Asteroid> _asteroids = new List<Asteroid>();
		//private List<ModelVisual3D> _asteroidBlips = new List<ModelVisual3D>();

		private List<Mineral> _minerals = new List<Mineral>();
		//private List<ModelVisual3D> _mineralBlips = new List<ModelVisual3D>();

		private List<SpaceStation> _stations = new List<SpaceStation>();
		//private List<ModelVisual3D> _stationBlips = new List<ModelVisual3D>();

		private List<Ship> _ships = new List<Ship>();
		//private List<ModelVisual3D> _shipBlips = new List<ModelVisual3D>();

		private IEnumerable<IMapObject>[] _allLists = null;		//	this has to be instantiated in the constructor

		/// <summary>
		/// This isn't meant to tick in regular intervals.  When a snapshot is finished, this is enabled with an interval that will
		/// tick when the next snapshot should be built (and disabled until needed again)
		/// </summary>
		private DispatcherTimer _snapshotTimer = null;

		private List<ScreenSpaceLines3D> _snapshotLines = null;

		#endregion

		#region Constructor

		public Map(Viewport3D viewport, Viewport3D viewportMap, World world)
		{
			_allLists = new IEnumerable<IMapObject>[] { _asteroids, _minerals, _stations, _ships };

			this.Viewport = viewport;
			//_viewportMap = viewportMap;
			this.World = world;
		}

		#endregion

		#region IDisposable Members

		public void Dispose()
		{
			if (_snapshotTimer != null)
			{
				_snapshotTimer.IsEnabled = false;
				_snapshotTimer = null;
			}

			//	Kill em all
			foreach (IMapObject mapObject in _allLists.SelectMany(o => o))
			{
				//RemoveFromViewport(mapObject);		//	the map will only be disposed at the end, so the state of the viewport isn't very important

				if (mapObject.PhysicsBody != null)
				{
					mapObject.PhysicsBody.Dispose();
				}
			}

			_asteroids = null;
			_minerals = null;
			_stations = null;
			_ships = null;
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

		private volatile object _snapshotFequency_Milliseconds = 500d;		//	double can't be volatile, but object storing a double can
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

		private int _snapshotMaxItemsPerNode = 15;
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

		private int _snapshotCountForNewTask = 250;
		/// <summary>
		/// When building the octree, if a child has more than this many objects, it is processed in a new task (likely on a unique
		/// thread)
		/// </summary>
		/// <remarks>
		/// There is some expense for the task to be set up, so don't use too small of a number (the tree would be built across more
		/// threads, but it would have been cheaper to brute force in fewer threads)
		/// </remarks>
		public int SnapshotCountForNewTask
		{
			get
			{
				return _snapshotCountForNewTask;
			}
			set
			{
				_snapshotCountForNewTask = value;
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

		//NOTE:  These add/remove the visuals from the viewports
		//NOTE:  These do NOT add/remove from world (that's done interally by the body when it is constructed/disposed)
		public void AddItem(IMapObject item)
		{
			if (item is Asteroid)
			{
				#region Asteroid

				//blip = GetAsteroidBlip((Asteroid)item);

				_asteroids.Add((Asteroid)item);
				//_asteroidBlips.Add(blip);

				AddToViewport(item);

				//if (blip != null)        // I may not bother making a blip for the tiny asteroids
				//{
				//    _viewportMap.Children.Add(blip);
				//}

				#endregion
			}
			else if (item is Mineral)
			{
				#region Mineral

				//blip = GetMineralBlip((Mineral)item);

				_minerals.Add((Mineral)item);
				//_mineralBlips.Add(blip);

				AddToViewport(item);

				//if (blip != null)        // I may not bother making a blip for low value minerals
				//{
				//    _viewportMap.Children.Add(blip);
				//}

				#endregion
			}
			else if (item is SpaceStation)
			{
				#region SpaceStation

				//blip = GetStationBlip((SpaceStation)item);

				_stations.Add((SpaceStation)item);
				//_stationBlips.Add(blip);

				AddToViewport(item);
				//_viewportMap.Children.Add(blip);

				#endregion
			}
			else if (item is Ship)
			{
				#region Ship

				//blip = GetShipBlip((Ship)item);

				_ships.Add((Ship)item);
				//_shipsBlips.Add(blip);

				AddToViewport(item);
				//_viewportMap.Children.Add(blip);

				#endregion
			}
			else
			{
				throw new ApplicationException("Unknown item type: " + item.ToString());
			}
		}
		public void RemoveItem(IMapObject item)
		{
			int index = -1;

			if (item is Asteroid)
			{
				#region Asteroid

				index = _asteroids.IndexOf((Asteroid)item);

				//if (_asteroidBlips[index] != null)
				//{
				//    _viewportMap.Children.Remove(_asteroidBlips[index]);
				//}

				RemoveFromViewport(item);

				_asteroids.RemoveAt(index);
				//_asteroidBlips.RemoveAt(index);

				#endregion
			}
			else if (item is Mineral)
			{
				#region Mineral

				index = _minerals.IndexOf((Mineral)item);

				//if (_mineralBlips[index] != null)
				//{
				//    _viewportMap.Children.Remove(_mineralBlips[index]);
				//}

				RemoveFromViewport(item);

				_minerals.RemoveAt(index);
				//_mineralBlips.RemoveAt(index);

				#endregion
			}
			else if (item is SpaceStation)
			{
				#region SpaceStation

				index = _stations.IndexOf((SpaceStation)item);

				//if (_stationBlips[index] != null)
				//{
				//    _viewportMap.Children.Remove(_stationBlips[index]);
				//}

				RemoveFromViewport(item);

				_stations.RemoveAt(index);
				//_stationBlips.RemoveAt(index);

				#endregion
			}
			else if (item is Ship)
			{
				#region Ship

				index = _ships.IndexOf((Ship)item);

				//if (_shipBlips[index] != null)
				//{
				//    _viewportMap.Children.Remove(_shipBlips[index]);
				//}

				RemoveFromViewport(item);

				_ships.RemoveAt(index);
				//_shipBlips.RemoveAt(index);

				#endregion
			}
			else
			{
				throw new ApplicationException("Unknown item type: " + item.ToString());
			}
		}

		/// <summary>
		/// This gives a dump of all the object the map knows about
		/// </summary>
		/// <remarks>
		/// I didn't want map.dispose to dispose all the physics objects, so I'm exposing this method instead
		/// </remarks>
		public List<IMapObject> GetAllObjects()
		{
			List<IMapObject> retVal = new List<IMapObject>();

			retVal.AddRange(_asteroids);
			retVal.AddRange(_minerals);
			retVal.AddRange(_stations);
			retVal.AddRange(_ships);

			return retVal;
		}

		/// <summary>
		/// This overload only searches the objects that are the type passed in (more efficient, because fewer objects are looked at)
		/// </summary>
		public IMapObject GetItem(Body physicsBody, Type type)
		{
			//	Figure out which list to look in
			IEnumerable<IMapObject> list = null;
			if (type == typeof(Asteroid))
			{
				list = _asteroids;
			}
			else if (type == typeof(Mineral))
			{
				list = _minerals;
			}
			else if (type == typeof(SpaceStation))
			{
				list = _stations;
			}
			else if (type == typeof(Ship))
			{
				list = _ships;
			}
			else
			{
				throw new ArgumentException("Unexpected type passed in: " + type.ToString());
			}

			//	Search for the body (I don't want to use linq, because speed is critical in this class)
			foreach (IMapObject item in list)
			{
				if (item.PhysicsBody != null && item.PhysicsBody == physicsBody)
				{
					return item;
				}
			}

			return null;
		}
		/// <summary>
		/// This overload searches all the parts until a match is found (stops searching once found, but still less efficient than the other overload if
		/// you know what type to search for)
		/// </summary>
		public IMapObject GetItem(Body physicsBody)
		{
			foreach (IEnumerable<IMapObject> list in _allLists)
			{
				foreach (IMapObject item in list)
				{
					if (item.PhysicsBody != null && item.PhysicsBody == physicsBody)
					{
						return item;
					}
				}
			}

			return null;
		}

		#endregion

		#region Event Listeners

		private void SnapshotTimer_Tick(object sender, EventArgs e)
		{
			if (_snapshotTimer == null)
			{
				return;		//	I had a rare case where this tick fired after dispose
			}

			_snapshotTimer.IsEnabled = false;

			BuildSnapshot();
		}

		#endregion

		#region Private Methods

		private void AddToViewport(IMapObject item)
		{
			if (item.Visuals3D != null)
			{
				foreach (Visual3D visual in item.Visuals3D)
				{
					this.Viewport.Children.Add(visual);
				}
			}
		}
		private void RemoveFromViewport(IMapObject item)
		{
			if (item.Visuals3D != null)
			{
				foreach (Visual3D visual in item.Visuals3D)
				{
					this.Viewport.Children.Remove(visual);
				}
			}
		}

		private void BuildSnapshot()
		{
			DateTime startTime = DateTime.Now;

			#region Build MapObjectInfo[]

			//	Get an array of info objects (this part must be done in this thread, because it's calling thread unsafe properties)
			//NOTE: There's a lot of code here, but I want this to be as fast and efficient as possible.  This is running on the main thread, and is pure expense

			MapObjectInfo[] mapObjects = new MapObjectInfo[_asteroids.Count + _minerals.Count + _stations.Count + _ships.Count];

			int offset = 0;
			int count = _asteroids.Count;		//	this will avoid hitting the count property on each iteration
			for (int cntr = 0; cntr < count; cntr++)
			{
				mapObjects[offset + cntr] = new MapObjectInfo(_asteroids[cntr]);		//	I'm still not sure if the hit on List<T>[index] is more or less than foreach.  I think the foreach is slightly slower, but could be wrong (plus I'd need to manually increment an index int)
			}

			offset += count;
			count = _minerals.Count;
			for (int cntr = 0; cntr < count; cntr++)
			{
				mapObjects[offset + cntr] = new MapObjectInfo(_minerals[cntr]);
			}

			offset += count;
			count = _stations.Count;
			for (int cntr = 0; cntr < count; cntr++)
			{
				mapObjects[offset + cntr] = new MapObjectInfo(_stations[cntr]);
			}

			offset += count;
			count = _ships.Count;
			for (int cntr = 0; cntr < count; cntr++)
			{
				mapObjects[offset + cntr] = new MapObjectInfo(_ships[cntr]);
			}

			#endregion

			//	Get these values while in the main thread
			bool shouldCentersDrift = _shouldSnapshotCentersDrift;
			int maxItemsPerNode = _snapshotMaxItemsPerNode;
			int countForNewTask = _snapshotCountForNewTask;

			//	Build the octree in another thread (it's ok to store the tree from this thread, LatestSnapshot is volatile - and I don't want
			//	to wait for the thread to sync up)
			var task = Task.Factory.StartNew(() =>
			{
				this.LatestSnapshot = BuildSnapshotTree(mapObjects, shouldCentersDrift, maxItemsPerNode, countForNewTask);
			});

			//	After the tree is built, schedule the next build in this current thread
			task.ContinueWith(resultTask =>
				{
					//	Schedule the next snapshot
					//NOTE: Don't want to recurse or set up a while loop if there is zero time remaining to build the next snapshot.  If I do, the caller will
					//wait indefinitely for this method to finish, and this thread will be completely tied up building sna
					if (_shouldBuildSnapshots)		//	it may have been turned off
					{
						double nextSnapshotWaitTime = this.SnapshotFequency_Milliseconds - (DateTime.Now - startTime).TotalMilliseconds;
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
						//	Clear the old
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

						//	Get the boundries of the nodes at each depth
						List<List<Point3D[]>> linesByDepth = new List<List<Point3D[]>>();
						GetSnapshotLines(linesByDepth, new MapOctree[] { this.LatestSnapshot });

						//	Draw lines
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
			const double RANGE = .05d;		//	this will make the surrounding box 10% bigger than normal.  This will avoid objects sitting right on the boundries (so that a < check may fail, but <= would have worked)

			if (items == null || items.Length == 0)
			{
				return new MapOctree(new Point3D(-1, -1, -1), new Point3D(1, 1, 1), new Point3D(0, 0, 0), null);
			}

			//	Calculate the extremes
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

				//	Min
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

				//	Max
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

			//	Add a bit to the range
			double rangeX = (maxX - minX) * RANGE;
			double rangeY = (maxY - minY) * RANGE;
			double rangeZ = (maxZ - minZ) * RANGE;

			Point3D minRange = new Point3D(minX - rangeX, minY - rangeY, minZ - rangeZ);
			Point3D maxRange = new Point3D(maxX + rangeX, maxY + rangeY, maxZ + rangeZ);

			//	Call the other overload
			return BuildSnapshotTree(items, minRange, maxRange, shouldCentersDrift, maxItemsPerNode, snapshotCountForNewTask);
		}
		/// <summary>
		/// This overload builds a tree node (and recurses for children)
		/// </summary>
		private static MapOctree BuildSnapshotTree(MapObjectInfo[] items, Point3D minRange, Point3D maxRange, bool shouldCentersDrift, int maxItemsPerNode, int snapshotCountForNewTask)
		{
			const double CENTERDRIFT = .05d;		//	Instead of the center point always being dead center, this will offset up to this random percent, to help avoid strange stabilities

			double centerX = (minRange.X + maxRange.X) * .5d;
			double centerY = (minRange.Y + maxRange.Y) * .5d;
			double centerZ = (minRange.Z + maxRange.Z) * .5d;

			if (items.Length <= maxItemsPerNode)
			{
				//	Leaf node
				return new MapOctree(minRange, maxRange, new Point3D(centerX, centerY, centerZ), items);		//	don't bother drifing the center point, octree requires centerpoint, but I doubt it will be used much for leaf nodes
			}

			#region Drift center point

			//	I only want to take the expense of doing the random if there will be children

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
							//	Straddles Z
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
							//	Straddles Z
							local.Add(items[cntr]);
						}
					}
					else
					{
						//	Straddles Y
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
							//	Straddles Z
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
							//	Straddles Z
							local.Add(items[cntr]);
						}
					}
					else
					{
						//	Straddles Y
						local.Add(items[cntr]);
					}
				}
				else
				{
					//	Straddles X
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

			//	Exit Function
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

					//	Top
					lines.Add(new Point3D[] { new Point3D(node.MinRange.X, node.MinRange.Y, node.MinRange.Z), new Point3D(node.MaxRange.X, node.MinRange.Y, node.MinRange.Z) });
					lines.Add(new Point3D[] { new Point3D(node.MaxRange.X, node.MinRange.Y, node.MinRange.Z), new Point3D(node.MaxRange.X, node.MaxRange.Y, node.MinRange.Z) });
					lines.Add(new Point3D[] { new Point3D(node.MaxRange.X, node.MaxRange.Y, node.MinRange.Z), new Point3D(node.MinRange.X, node.MaxRange.Y, node.MinRange.Z) });
					lines.Add(new Point3D[] { new Point3D(node.MinRange.X, node.MaxRange.Y, node.MinRange.Z), new Point3D(node.MinRange.X, node.MinRange.Y, node.MinRange.Z) });

					//	Bottom
					lines.Add(new Point3D[] { new Point3D(node.MinRange.X, node.MinRange.Y, node.MaxRange.Z), new Point3D(node.MaxRange.X, node.MinRange.Y, node.MaxRange.Z) });
					lines.Add(new Point3D[] { new Point3D(node.MaxRange.X, node.MinRange.Y, node.MaxRange.Z), new Point3D(node.MaxRange.X, node.MaxRange.Y, node.MaxRange.Z) });
					lines.Add(new Point3D[] { new Point3D(node.MaxRange.X, node.MaxRange.Y, node.MaxRange.Z), new Point3D(node.MinRange.X, node.MaxRange.Y, node.MaxRange.Z) });
					lines.Add(new Point3D[] { new Point3D(node.MinRange.X, node.MaxRange.Y, node.MaxRange.Z), new Point3D(node.MinRange.X, node.MinRange.Y, node.MaxRange.Z) });

					//	Sides
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
			const double MINHUE = 0d;		//	red
			const double MAXHUE = 260d;		//	purple
			const int DEPTHMAX = 5;

			for (int cntr = 0; cntr < linesByDepth.Count; cntr++)
			{
				ScreenSpaceLines3D model = new ScreenSpaceLines3D(true);
				double hue = UtilityHelper.GetScaledValue_Capped(MINHUE, MAXHUE, 0, DEPTHMAX, cntr);
				hue = MAXHUE - hue;		//	flip it so the bigger the cntr, the closer to red
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
			this.Token = TokenGenerator.Instance.NextToken();
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
			this.Token = TokenGenerator.Instance.NextToken();
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

		//	It can't be enforced by the compiler, but if this is a parent, then each child will be an even subdivision of this volume (1/8th)
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

		//	Since an octree has 8 children, each child corresponds to a +-X, +-Y, +-Z
		//	For naming, negative is 0, positive is 1.  So X0Y0Z0 is the bottom left back cell, and X1Y1Z1 is the top right front cell
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
				retVal.AddRange(this.Items);
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
					if (Math3D.Intersects(item.AABBMin, item.AABBMax, center, searchRadius))
					{
						retVal.Add(item);
					}
				}
			}

			if (this.HasChildren)
			{
				if (this.X0_Y0_Z0 != null && Math3D.Intersects(this.X0_Y0_Z0.MinRange, this.X0_Y0_Z0.MaxRange, center, searchRadius))
				{
					retVal.AddRange(this.X0_Y0_Z0.GetItems(center, searchRadius));
				}

				if (this.X0_Y0_Z1 != null && Math3D.Intersects(this.X0_Y0_Z1.MinRange, this.X0_Y0_Z1.MaxRange, center, searchRadius))
				{
					retVal.AddRange(this.X0_Y0_Z1.GetItems(center, searchRadius));
				}

				if (this.X0_Y1_Z0 != null && Math3D.Intersects(this.X0_Y1_Z0.MinRange, this.X0_Y1_Z0.MaxRange, center, searchRadius))
				{
					retVal.AddRange(this.X0_Y1_Z0.GetItems(center, searchRadius));
				}

				if (this.X0_Y1_Z1 != null && Math3D.Intersects(this.X0_Y1_Z1.MinRange, this.X0_Y1_Z1.MaxRange, center, searchRadius))
				{
					retVal.AddRange(this.X0_Y1_Z1.GetItems(center, searchRadius));
				}

				if (this.X1_Y0_Z0 != null && Math3D.Intersects(this.X1_Y0_Z0.MinRange, this.X1_Y0_Z0.MaxRange, center, searchRadius))
				{
					retVal.AddRange(this.X1_Y0_Z0.GetItems(center, searchRadius));
				}

				if (this.X1_Y0_Z1 != null && Math3D.Intersects(this.X1_Y0_Z1.MinRange, this.X1_Y0_Z1.MaxRange, center, searchRadius))
				{
					retVal.AddRange(this.X1_Y0_Z1.GetItems(center, searchRadius));
				}

				if (this.X1_Y1_Z0 != null && Math3D.Intersects(this.X1_Y1_Z0.MinRange, this.X1_Y1_Z0.MaxRange, center, searchRadius))
				{
					retVal.AddRange(this.X1_Y1_Z0.GetItems(center, searchRadius));
				}

				if (this.X1_Y1_Z1 != null && Math3D.Intersects(this.X1_Y1_Z1.MinRange, this.X1_Y1_Z1.MaxRange, center, searchRadius))
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
					if (Math3D.Intersects(item.AABBMin, item.AABBMax, min, max))
					{
						retVal.AddRange(this.Items);
					}
				}
			}

			if (this.HasChildren)
			{
				if (this.X0_Y0_Z0 != null && Math3D.Intersects(this.X0_Y0_Z0.MinRange, this.X0_Y0_Z0.MaxRange, min, max))
				{
					retVal.AddRange(this.X0_Y0_Z0.GetItems(min, max));
				}

				if (this.X0_Y0_Z1 != null && Math3D.Intersects(this.X0_Y0_Z1.MinRange, this.X0_Y0_Z1.MaxRange, min, max))
				{
					retVal.AddRange(this.X0_Y0_Z1.GetItems(min, max));
				}

				if (this.X0_Y1_Z0 != null && Math3D.Intersects(this.X0_Y1_Z0.MinRange, this.X0_Y1_Z0.MaxRange, min, max))
				{
					retVal.AddRange(this.X0_Y1_Z0.GetItems(min, max));
				}

				if (this.X0_Y1_Z1 != null && Math3D.Intersects(this.X0_Y1_Z1.MinRange, this.X0_Y1_Z1.MaxRange, min, max))
				{
					retVal.AddRange(this.X0_Y1_Z1.GetItems(min, max));
				}

				if (this.X1_Y0_Z0 != null && Math3D.Intersects(this.X1_Y0_Z0.MinRange, this.X1_Y0_Z0.MaxRange, min, max))
				{
					retVal.AddRange(this.X1_Y0_Z0.GetItems(min, max));
				}

				if (this.X1_Y0_Z1 != null && Math3D.Intersects(this.X1_Y0_Z1.MinRange, this.X1_Y0_Z1.MaxRange, min, max))
				{
					retVal.AddRange(this.X1_Y0_Z1.GetItems(min, max));
				}

				if (this.X1_Y1_Z0 != null && Math3D.Intersects(this.X1_Y1_Z0.MinRange, this.X1_Y1_Z0.MaxRange, min, max))
				{
					retVal.AddRange(this.X1_Y1_Z0.GetItems(min, max));
				}

				if (this.X1_Y1_Z1 != null && Math3D.Intersects(this.X1_Y1_Z1.MinRange, this.X1_Y1_Z1.MaxRange, min, max))
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

		#endregion
	}

	#endregion
	#region Class: MapObjectInfo

	public class MapObjectInfo
	{
		#region Constructor

		public MapObjectInfo(IMapObject mapObject)
		{
			this.Token = mapObject.PhysicsBody.Token;
			this.MapObject = mapObject;
			this.Position = mapObject.PositionWorld;
			this.Velocity = mapObject.VelocityWorld;
			this.Mass = mapObject.PhysicsBody.Mass;
			this.Radius = mapObject.Radius;

			//NOTE: This is a bit bigger than what newton would report as the AABB, but is very fast to calculate
			this.AABBMin = new Point3D(this.Position.X - this.Radius, this.Position.Y - this.Radius, this.Position.Z - this.Radius);
			this.AABBMax = new Point3D(this.Position.X + this.Radius, this.Position.Y + this.Radius, this.Position.Z + this.Radius);
		}

		#endregion

		public readonly long Token;

		/// <summary>
		/// WARNING: This is stored here so the user can see what type it is.  The properties/methods of mapobject aren't thread safe
		/// </summary>
		public readonly IMapObject MapObject;

		public readonly Point3D Position;
		public readonly Vector3D Velocity;

		public readonly double Mass;
		public readonly double Radius;

		public readonly Point3D AABBMin;
		public readonly Point3D AABBMax;
	}

	#endregion
}
