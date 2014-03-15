using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

using Game.HelperClasses;

namespace Game.Newt.HelperClasses.Primitives3D
{
    #region Class: BillboardLine3D

    /// <summary>
    /// This is a line segment that uses billboarding as a crude way of appearing 3D.  It is meant to be cheap.
    /// </summary>
    /// <remarks>
    /// Use ScreenSpaceLines3D if you want the lines to appear the same size on screen regardless of depth
    /// 
    /// Use a cylinder if you want a better looking line (at the expense of poly count)
    /// </remarks>
    public class BillboardLine3D : IDisposable
    {
        #region Declaration Section

        /// <summary>
        /// This is used to rotate the line about its axis if from/to won't be updated on a regular interval
        /// </summary>
        [ThreadStatic]
        private static DispatcherTimer _timer;
        [ThreadStatic]
        private static int _numUsers;

        private readonly bool _isUsingTimer;
        private readonly bool _shouldSpin;

        private readonly Vector3D _initialOrientation = new Vector3D(0, 0, 1);

        #endregion

        #region Constructor

        public BillboardLine3D(bool isArrow = false, bool shouldSpin = true, bool areFromToPointsUpdatedOnRegularInterval = true)
        {
            _shouldSpin = shouldSpin;

            // Model
            this.Model = new GeometryModel3D();
            InvalidateColor();      // this will populate the model's materials
            this.Model.Geometry = GetInitialGeometry(isArrow);      // Create a line along Z, length 1, thickness 1

            #region Timer

            if (areFromToPointsUpdatedOnRegularInterval)
            {
                _isUsingTimer = false;
            }
            else
            {
                _isUsingTimer = true;

                if (_timer == null)
                {
                    _timer = new DispatcherTimer();
                    _timer.Interval = TimeSpan.FromMilliseconds(75);
                }

                _timer.Tick += Timer_Tick;

                if (_numUsers == 0)
                {
                    _timer.Start();
                }

                _numUsers++;
            }

            #endregion
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
                if (_isUsingTimer)
                {
                    _timer.Tick -= Timer_Tick;

                    _numUsers--;
                    if (_numUsers == 0)
                    {
                        _timer.Stop();
                    }
                }
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// This is the model that this class keeps updated
        /// </summary>
        /// <remarks>
        /// Originally, I was going to make this class derive from ModelVisual3D.  But Visual3D is expensive, so it's up to the
        /// caller to create a single visual that holds a group of these (the odds are good that when this class is used, there will
        /// be many lines needed)
        /// </remarks>
        public readonly GeometryModel3D Model;

        private bool _isReflectiveColor = false;
        public bool IsReflectiveColor
        {
            get
            {
                return _isReflectiveColor;
            }
            set
            {
                _material = null;
                _isReflectiveColor = value;

                InvalidateColor();
            }
        }

        private Color _color = Colors.Transparent;
        public Color Color
        {
            get
            {
                return _color;
            }
            set
            {
                _material = null;
                _color = value;

                InvalidateColor();
            }
        }

        // If this is set, it overrides this.Color and this.IsReflectiveColor
        //NOTE: Setting either of those will set this back to null
        private Material _material = null;
        internal Material Material
        {
            get
            {
                return _material;
            }
            set
            {
                _material = value;

                InvalidateColor();
            }
        }

        private double _thickness;
        public double Thickness
        {
            get
            {
                return _thickness;
            }
            set
            {
                _thickness = value;

                InvalidateGeometry();
            }
        }

        private Point3D _fromPoint;
        public Point3D FromPoint
        {
            get
            {
                return _fromPoint;
            }
            set
            {
                _fromPoint = value;

                InvalidateGeometry();
            }
        }

        private Point3D _toPoint;
        public Point3D ToPoint
        {
            get
            {
                return _toPoint;
            }
            set
            {
                _toPoint = value;

                InvalidateGeometry();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// This is no different than setting FromPoint and ToPoint individually, except the transform is only updated once.
        /// (you could also call BeginInit(); From=; To=; ToInit(); but that is tedious, and still less efficient than having a dedicated
        /// metho)
        /// </summary>
        public void SetPoints(Point3D fromPoint, Point3D toPoint)
        {
            _fromPoint = fromPoint;
            _toPoint = toPoint;

            InvalidateGeometry();
        }
        public void SetPoints(Point3D fromPoint, Point3D toPoint, double thickness)
        {
            _thickness = thickness;
            _fromPoint = fromPoint;
            _toPoint = toPoint;

            InvalidateGeometry();
        }

        internal static Material GetMaterial(bool isReflective, Color color)
        {
            if (isReflective)
            {
                DiffuseMaterial litMaterial = new DiffuseMaterial(new SolidColorBrush(color));
                litMaterial.Freeze();

                return litMaterial;
            }
            else
            {
                return UtilityWPF.GetUnlitMaterial(color);
            }
        }

        #endregion

        #region Event Listeners

        private void Timer_Tick(object sender, EventArgs e)
        {
            InvalidateGeometry();
        }

        #endregion

        #region Private Methods

        private void InvalidateColor()
        {
            Material material = _material ?? GetMaterial(_isReflectiveColor, _color);

            this.Model.Material = material;
            this.Model.BackMaterial = material;
        }

        private void InvalidateGeometry()
        {
            Vector3D direction = _toPoint - _fromPoint;
            double directionLength = direction.Length;

            if (Math3D.IsInvalid(directionLength) || Math3D.IsNearZero(directionLength))
            {
                this.Model.Transform = new ScaleTransform3D(0, 0, 0);
                return;
            }

            Transform3DGroup transform = new Transform3DGroup();

            transform.Children.Add(new ScaleTransform3D(_thickness, _thickness, directionLength));

            if (_shouldSpin)
            {
                transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(_initialOrientation, StaticRandom.NextDouble(360d))));
            }

            transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRotation(_initialOrientation, direction))));

            transform.Children.Add(new TranslateTransform3D(_fromPoint.ToVector()));

            this.Model.Transform = transform;
        }

        private static MeshGeometry3D GetInitialGeometry(bool isArrow)
        {
            if (isArrow)
            {
                //TODO: Copy bits of GetLine in, and add arrows
                throw new ApplicationException("finish this");
            }
            else
            {
                //TODO: If _shouldSpin is false, then apply a random orientation here (otherwise all the line's crosses will be aligned and look funny)
                return UtilityWPF.GetLine(new Point3D(0, 0, 0), new Point3D(0, 0, 1), 1);
            }
        }

        #endregion
    }

    #endregion
    #region Class: BillboardLine3DSet

    /// <summary>
    /// BillboardLine3D is an individual line, and is also just a model.  This class manages a set of lines, and is a visual.
    /// </summary>
    /// <remarks>
    /// This is designed for lines to be added/removed on a regular basis.  It holds on to instances that have been removed, anticipating
    /// that other lines will be added later, which speeds things up
    /// </remarks>
    public class BillboardLine3DSet : ModelVisual3D, IDisposable
    {
        #region Declaration Section

        private readonly bool _isArrow;
        private readonly bool _shouldSpin;
        private readonly bool _areFromToPointsUpdatedOnRegularInterval;

        private Material _material = null;

        private readonly List<BillboardLine3D> _lines = new List<BillboardLine3D>();
        private readonly Model3DGroup _geometry;

        /// <summary>
        /// This is how many lines are currently showing
        /// </summary>
        /// <remarks>
        /// This gets updated in BeginAddingLines, AddLine, EndAddingLines
        /// 
        /// _lines may hold more than this.  The extras are just on standby in case more lines are requested later (saves from needing
        /// to reinstantiate so much)
        /// </remarks>
        private int? _lineCount = null;

        #endregion

        #region Constructor

        public BillboardLine3DSet(bool isArrow = false, bool shouldSpin = true, bool areFromToPointsUpdatedOnRegularInterval = true)
        {
            _isArrow = isArrow;
            _shouldSpin = shouldSpin;
            _areFromToPointsUpdatedOnRegularInterval = areFromToPointsUpdatedOnRegularInterval;

            _geometry = new Model3DGroup();
            this.Content = _geometry;
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
                Clear();
            }
        }

        #endregion

        #region Public Properties

        private bool _isReflectiveColor = false;
        public bool IsReflectiveColor
        {
            get
            {
                return _isReflectiveColor;
            }
            set
            {
                _isReflectiveColor = value;

                InvalidateColor();
            }
        }

        private Color _color = Colors.Transparent;
        public Color Color
        {
            get
            {
                return _color;
            }
            set
            {
                _color = value;

                InvalidateColor();
            }
        }

        #endregion

        #region Public Methods

        public void Clear()
        {
            _geometry.Children.Clear();

            foreach (BillboardLine3D line in _lines)
            {
                line.Dispose();
            }
            _lines.Clear();

            _lineCount = null;
        }

        public void BeginAddingLines()
        {
            _lineCount = 0;
        }
        public void AddLine(Point3D start, Point3D stop, double thickness)
        {
            //bool addedOne = false;

            if (_lineCount == null)
            {
                throw new InvalidOperationException("Must call BeginAddingLines before calling AddLine");
            }
            else if (_lineCount.Value > _lines.Count)
            {
                throw new ApplicationException("_lineCount fell out of sync");
            }
            else if (_lineCount.Value == _lines.Count)
            {
                // Create a new line
                BillboardLine3D line = new BillboardLine3D();
                line.Material = _material;

                _lines.Add(line);
                //addedOne = true;
            }

            _lineCount = _lineCount.Value + 1;

            // Whatever line was at this index now gets this position
            _lines[_lineCount.Value - 1].SetPoints(start, stop, thickness);

            //if (addedOne)
            if(_lineCount.Value > _geometry.Children.Count)
            {
                // This must be done last.  Otherwise, when the computer is stressed, the line will get rendered before it is positioned
                _geometry.Children.Add(_lines[_lineCount.Value - 1].Model);
            }
        }
        public void EndAddingLines()
        {
            if (_lineCount == null)
            {
                throw new InvalidOperationException("Must call BeginAddingLines before calling EndAddingLines");
            }

            for (int cntr = _geometry.Children.Count - 1; cntr >= _lineCount.Value; cntr--)
            {
                _geometry.Children.RemoveAt(cntr);

                //NOTE: Don't want to dispose this line here.  It might need to be recreated next frame
                //TODO: May want to store a lifespan per index, so that really old lines get disposed.  But only bother if some use of this class is a hog.  In most cases, the number of lines frame to frame should be pretty consistent
                //_lines[cntr].Dispose();
                //_lines.RemoveAt(cntr);
            }
        }

        #endregion

        #region Private Methods

        private void InvalidateColor()
        {
            _material = BillboardLine3D.GetMaterial(_isReflectiveColor, _color);

            // Update existing lines
            foreach (BillboardLine3D line in _lines)
            {
                line.Material = _material;
            }
        }

        #endregion
    }

    #endregion
}
