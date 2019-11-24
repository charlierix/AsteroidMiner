using Game.HelperClassesWPF;
using System.Windows;
using System.Windows.Media.Media3D;

namespace Game.Newt.v2.Arcanorum
{
    public class AIMousePlate
    {
        #region Declaration Section

        private readonly DragHitShape _dragPlane;

        #endregion

        #region Constructor

        public AIMousePlate(DragHitShape dragPlane, double scale = 1, double maxXY = 100)
        {
            _dragPlane = dragPlane;
            Scale = scale;
            MaxXY = maxXY;

            Position = new Point3D(0, 0, 0);
            Up = new Vector3D(0, 1, 0);
            Look = new Vector3D(0, 0, -1);
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// .5=1 2D unit to 2 3D units
        /// 2=2 2D units to 1 3D unit
        /// </summary>
        public double Scale { get; set; }

        public double MaxXY { get; set; }

        public Point3D Position { get; set; }
        public Vector3D Up { get; set; }
        public Vector3D Look { get; set; }

        private volatile object _currentPoint2D = new Point(0, 0);
        public Point CurrentPoint2D
        {
            get
            {
                return (Point)_currentPoint2D;
            }
            set
            {
                _currentPoint2D = value;
            }
        }

        #endregion

        #region Public Methods

        public Point3D? ProjectTo3D()
        {
            //TODO: Cache this
            Vector3D orth = Vector3D.CrossProduct(Look, Up);

            //TODO: Cache Up as a unit vector

            // Project the X part of the 2D along orth
            Vector3D x = orth.ToUnit(true) * (CurrentPoint2D.X / Scale);

            // Project the Y part along up
            Vector3D y = Up.ToUnit(true) * (CurrentPoint2D.Y / Scale);

            if (Math3D.IsInvalid(x) || Math3D.IsInvalid(y))
            {
                return null;
            }

            // Fire a ray along look toward the drag plane
            return _dragPlane.CastRay(new RayHitTestParameters(Position + x + y, Look));
        }

        #endregion
    }
}
