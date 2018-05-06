using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.NewtonDynamics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Game.Newt.v2.Arcanorum.ArenaTester
{
    // This is creating world too early.  Need to wait until a request from the worker thread

    public class ArenaAccessor_FAIL : IDisposable
    {
        #region class: RoomStorage

        private class RoomStorage
        {
            public Point3D Center { get; set; }
            public Rect3D Rect { get; set; }
            public Map Map { get; set; }
            public UpdateManager UpdateManager { get; set; }
            public bool IsCheckedOut { get; set; }

            public TrainingRoom Room { get; set; }
        }

        #endregion

        #region Declaration Section

        private readonly RoomStorage[] _roomStorage;

        #endregion

        #region Constructor

        /// <remarks>
        /// This overload creates the world and maps
        /// 
        /// If there needs to be an arena along with other items in a world, then make another overload that passes
        /// in the world and an offset, etc
        /// </remarks>
        public ArenaAccessor_FAIL(int numRooms, double roomSize, double roomMargins, bool dividerWalls, bool shouldMapBuildSnapshots, Type[] updateTypes_main, Type[] updateTypes_any)
        {
            if (dividerWalls)
            {
                throw new ApplicationException("TODO: Implement divider walls");
            }

            _numRooms = numRooms;
            _roomSize = roomSize;
            _roomMargin = roomMargins;

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

            _world = new World(false);
            _world.SetCollisionBoundry(_boundryMin, _boundryMax);
            _world.Updating += World_Updating;

            _roomStorage = cells.
                Select((o, i) =>
                {
                    Map map = new Map(null, null, _world)
                    {
                        ShouldBuildSnapshots = shouldMapBuildSnapshots
                    };

                    TrainingRoom room = new TrainingRoom()
                    {
                        Map = map,
                        World = _world,
                        Center = o.center,
                        AABB = (o.rect.Location, o.rect.Location + o.rect.Size.ToVector()),
                        Index = i,
                    };

                    return new RoomStorage()
                    {
                        Center = o.center,
                        Rect = o.rect,
                        Map = map,
                        UpdateManager = new UpdateManager(updateTypes_main, updateTypes_any, map, useTimer: false),
                        IsCheckedOut = false,
                        Room = room,
                    };
                }).
                ToArray();
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

                    foreach (RoomStorage room in _roomStorage)
                    {
                        room.UpdateManager.Dispose();
                        room.Map.Dispose();
                    }

                    _world.Dispose();
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

        private readonly World _world;
        public World World => _world;

        /// <summary>
        /// This iterates over all rooms whether they are checked out or not.  This is good for initializing rooms or getting debug stats
        /// </summary>
        public IEnumerable<(TrainingRoom room, bool isCheckedOut)> AllRooms => _roomStorage.Select(o => (o.Room, o.IsCheckedOut));

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

        #region Public Methods

        /// <summary>
        /// The physics world is a limited resource, and would need to run on a single thread.  So if there are many evaluator instances already running,
        /// this call would queue up and return when available
        /// </summary>
        /// <remarks>
        /// Figure out how to not block the thread.  That burden probably shouldn't be passed this far down.  Instead, make a custom version of SharpNEAT's
        /// evaluator that only has a certain number of live evaluations --- or maybe it already does this?
        /// </remarks>
        public TrainingRoom CheckoutRoom()
        {
            for (int cntr = 0; cntr < _roomStorage.Length; cntr++)
            {
                if (!_roomStorage[cntr].IsCheckedOut)
                {
                    _roomStorage[cntr].IsCheckedOut = true;
                    return _roomStorage[cntr].Room;
                }
            }

            return null;
        }

        public void UncheckoutRoom(TrainingRoom room)
        {
            #region validate

            if (room == null)
            {
                throw new ArgumentNullException("room", "room passed in can't be null");
            }

            if (room.Index < 0 || room.Index >= _roomStorage.Length)
            {
                throw new ArgumentException(string.Format("room.Index is out of range. index={0}, count={1}", room.Index, _roomStorage.Length), "room.Index");
            }

            if (!_roomStorage[room.Index].IsCheckedOut)
            {
                throw new ArgumentException("room isn't checked out");
            }

            #endregion

            _roomStorage[room.Index].IsCheckedOut = false;
        }

        #endregion

        #region Event Listeners

        //TODO: May not want an event listener.  There may end up being a direct Update call
        private void World_Updating(object sender, WorldUpdatingArgs e)
        {
            foreach (var room in _roomStorage)
            {
                if (room.IsCheckedOut)
                {
                    room.UpdateManager.Update_MainThread(e.ElapsedTime);
                }
            }
        }

        #endregion
    }
}
