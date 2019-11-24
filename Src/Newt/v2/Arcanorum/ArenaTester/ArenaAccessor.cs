using Game.HelperClassesWPF;
using Game.Newt.v2.GameItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Game.Newt.v2.Arcanorum.ArenaTester
{
    /// <summary>
    /// This is owned by TrainingSession and is an accessor to training rooms
    /// </summary>
    /// <remarks>
    /// This is a pretty thin class.  It arranges and creates the rooms, tells them to go each tick
    /// </remarks>
    public class ArenaAccessor : IDisposable
    {
        #region class: TrainingRoomWrapper

        private class TrainingRoomWrapper
        {
            public TrainingRoom Room { get; set; }
            public bool IsCheckedOut { get; set; }
            public UpdateManager UpdateManager { get; set; }
            public NeuralPool_ManualTick NeuralPool { get; set; }
        }

        #endregion

        #region Events

        public event EventHandler WorldCreated = null;

        #endregion

        #region Declaration Section

        private readonly TrainingRoomWrapper[] _rooms;

        private readonly bool _shouldMapBuildSnapshots;
        private readonly Type[] _updateTypes_main;
        private readonly Type[] _updateTypes_any;

        #endregion

        #region Constructor

        public ArenaAccessor(int numRooms, double roomSize, double roomMargins, bool dividerWalls, bool shouldMapBuildSnapshots, Type[] updateTypes_main, Type[] updateTypes_any, NeuralPool_ManualTick neuralPoolManual, (double elapsed, double randPercent)? worldUpdateTime = null)
        {
            if (dividerWalls)
            {
                throw new ApplicationException("TODO: Implement divider walls");
            }

            _numRooms = numRooms;
            _roomSize = roomSize;
            _roomMargin = roomMargins;

            _shouldMapBuildSnapshots = shouldMapBuildSnapshots;
            _updateTypes_main = updateTypes_main;
            _updateTypes_any = updateTypes_any;

            var cells = Math3D.GetCells_Cube(roomSize, numRooms, roomMargins).
                Take(numRooms).
                ToArray();

            _boundryMin = new Point3D(
                cells.Min(o => o.rect.Location.X - roomMargins),        // technically, this should only be half the room margin, but leaving an extra buffer around the set
                cells.Min(o => o.rect.Location.Y - roomMargins),
                cells.Min(o => o.rect.Location.Z) - roomMargins);

            _boundryMax = new Point3D(
                cells.Max(o => o.rect.Location.X + o.rect.Size.X + roomMargins),
                cells.Max(o => o.rect.Location.Y + o.rect.Size.Y + roomMargins),
                cells.Max(o => o.rect.Location.Z + o.rect.Size.Z) + roomMargins);

            _rooms = cells.
                Select((o, i) => new TrainingRoomWrapper()
                {
                    IsCheckedOut = false,
                    NeuralPool = neuralPoolManual,
                    Room = new TrainingRoom()
                    {
                        Center = o.center,
                        AABB = (o.rect.Location, o.rect.Location + o.rect.Size.ToVector()),
                        Index = i,
                    },
                }).
                ToArray();

            _worldAccessor = new WorldAccessor(_boundryMin, _boundryMax, worldUpdateTime);
            _worldAccessor.Initialized += WorldAccessor_Initialized;
        }

        #endregion

        #region IDisposable

        private bool _disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).

                    foreach (TrainingRoomWrapper room in _rooms)
                    {
                        if (room.UpdateManager != null)
                        {
                            room.UpdateManager.Dispose();
                            room.UpdateManager = null;
                        }

                        if (room.Room.Map != null)
                        {
                            room.Room.Map.Dispose();
                            room.Room.Map = null;
                        }
                    }

                    _worldAccessor.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                _disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~ArenaAccessor() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        #endregion

        #region Public Properties

        private readonly WorldAccessor _worldAccessor;
        public WorldAccessor WorldAccessor => _worldAccessor;

        /// <summary>
        /// This iterates over all rooms whether they are checked out or not.  This is good for initializing rooms or getting debug stats
        /// </summary>
        public IEnumerable<(TrainingRoom room, bool isCheckedOut)> AllRooms => _rooms.Select(o => (o.Room, o.IsCheckedOut));

        private readonly int _numRooms;
        public int NumRooms => _numRooms;

        private readonly double _roomSize;
        public double RoomSize => _roomSize;

        private readonly double _roomMargin;
        public double RoomMargin => _roomMargin;

        private readonly Point3D _boundryMin;
        public Point3D BoundryMin => _boundryMin;

        private readonly Point3D _boundryMax;
        public Point3D BoundryMax => _boundryMax;

        #endregion

        #region Event Listeners

        //NOTE: This is called from the worker thread, but only once
        private void WorldAccessor_Initialized(object sender, EventArgs e)
        {
            // Distribute the world to all the rooms
            foreach (TrainingRoomWrapper room in _rooms)
            {
                room.Room.World = _worldAccessor.World;
                room.Room.Map = new Map(null, null, room.Room.World)
                {
                    ShouldBuildSnapshots = _shouldMapBuildSnapshots,
                };

                room.UpdateManager = new UpdateManager(_updateTypes_main, _updateTypes_any, room.Room.Map, useTimer: false);
            }

            // Now that the rooms are more set up, raise an event so the caller can populate the rooms
            WorldCreated?.Invoke(this, new EventArgs());

            _worldAccessor.World.Updated += World_Updated;
        }

        private void World_Updated(object sender, NewtonDynamics.WorldUpdatingArgs e)
        {
            foreach (TrainingRoomWrapper room in _rooms)
            {
                if (room.NeuralPool != null)
                {
                    room.NeuralPool.Tick();
                }

                //TODO: May want to come up with a different way to fire any thread
                if (room.UpdateManager != null)
                {
                    room.UpdateManager.Update_AnyThread(e.ElapsedTime);
                    room.UpdateManager.Update_MainThread(e.ElapsedTime);
                }
            }
        }

        #endregion
    }
}
