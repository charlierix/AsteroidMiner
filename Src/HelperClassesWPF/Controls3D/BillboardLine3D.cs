using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

using Game.HelperClassesCore;

namespace Game.HelperClassesWPF.Controls3D
{
    #region class: BillboardLine3D

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

        private readonly Vector3D _initialOrientation = new Vector3D(0, 0, 1);

        private readonly GeometryModel3D _lineModel;
        private GeometryModel3D _fromArrowModel = null;
        private GeometryModel3D _toArrowModel = null;

        // These get multiplied by the thickness
        //TODO: May want to expose these as public properties
        private readonly double _arrowBaseMult = 5;
        private readonly double _arrowHeightMult = 10;

        private SolidColorBrush _solidBrush = null;
        private SolidColorBrush _solidBrush_Unlit = null;

        private LinearGradientBrush _linearBrush = null;
        private LinearGradientBrush _linearBrush_Unlit = null;

        private Material _material = null;

        #endregion

        #region Constructor

        public BillboardLine3D(bool shouldUseSpinTimer = false)
        {
            // Models
            // NOTE: The arrows get added if the properties get set
            _model = new Model3DGroup();

            _lineModel = new GeometryModel3D()
            {
                Geometry = UtilityWPF.GetLine(new Point3D(0, 0, 0), new Point3D(0, 0, 1), 1),      // Create a line along Z, length 1, thickness 1
            };

            _model.Children.Add(_lineModel);

            InvalidateColor();      // this will populate the model's materials

            #region timer

            _isUsingTimer = shouldUseSpinTimer;

            if (_isUsingTimer)
            {
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

        private readonly Model3DGroup _model;
        /// <summary>
        /// This is the model that this class keeps updated
        /// </summary>
        /// <remarks>
        /// Originally, I was going to make this class derive from ModelVisual3D.  But Visual3D is expensive, so it's up to the
        /// caller to create a single visual that holds a group of these (the odds are good that when this class is used, there will
        /// be many lines needed)
        /// </remarks>
        public Model3D Model => _model;

        private bool _isReflectiveColor = false;
        public bool IsReflectiveColor
        {
            get
            {
                return _isReflectiveColor;
            }
            set
            {
                _solidBrush = null;
                _solidBrush_Unlit = null;

                _linearBrush = null;
                _linearBrush_Unlit = null;

                _material = null;

                _isReflectiveColor = value;

                InvalidateColor();
            }
        }

        private Color? _color = null;
        public Color? Color
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

        private Color? _color2 = null;
        public Color? ColorTo
        {
            get
            {
                return _color2;
            }
            set
            {
                _solidBrush = null;
                _solidBrush_Unlit = null;

                _material = null;

                _color2 = value;

                InvalidateColor();
            }
        }

        /// <summary>
        /// If this is set, it overrides this.Color and this.IsReflectiveColor
        /// NOTE: look at GetLinearGradientMaterial_Reflective, GetLinearGradientMaterial_Unlit
        /// </summary>
        /// <remarks>
        /// NOTE: Setting either of those will set this back to null
        /// </remarks>
        public Material Material
        {
            get
            {
                return _material;
            }
            set
            {
                _solidBrush = null;
                _solidBrush_Unlit = null;

                _linearBrush = null;
                _linearBrush_Unlit = null;

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

        private double? _fromArrowPercent = null;
        public double? FromArrowPercent
        {
            get
            {
                return _fromArrowPercent;
            }
            set
            {
                if (_fromArrowPercent == null && value == null)
                {
                    return;
                }
                else if (_fromArrowModel != null && value == null)
                {
                    _model.Children.Remove(_fromArrowModel);
                    _fromArrowModel = null;
                }
                else if (_fromArrowModel == null && value != null)
                {
                    _fromArrowModel = GetArrowModel();
                    _model.Children.Add(_fromArrowModel);
                }

                _fromArrowPercent = value;

                InvalidateColor();
                InvalidateGeometry();
            }
        }

        private double? _toArrowPercent = null;
        public double? ToArrowPercent
        {
            get
            {
                return _toArrowPercent;
            }
            set
            {
                if (_toArrowPercent == null && value == null)
                {
                    return;
                }
                else if (_toArrowModel != null && value == null)
                {
                    _model.Children.Remove(_toArrowModel);
                    _toArrowModel = null;
                }
                else if (_toArrowModel == null && value != null)
                {
                    _toArrowModel = GetArrowModel();
                    _model.Children.Add(_toArrowModel);
                }

                _toArrowPercent = value;

                InvalidateColor();
                InvalidateGeometry();
            }
        }

        private double _arrowSizeMult = 1d;
        public double ArrowSizeMult
        {
            get
            {
                return _arrowSizeMult;
            }
            set
            {
                _arrowSizeMult = value;

                InvalidateColor();
                InvalidateGeometry();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// This is no different than setting FromPoint and ToPoint individually, except the transform is only updated once.
        /// (you could also call BeginInit(); From=; To=; ToInit(); but that is tedious, and still less efficient than having a dedicated
        /// method)
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

        public static Material GetSolidColorMaterial(bool isReflective, Color color)
        {
            return GetSolidColorMaterial_private(isReflective, color).
                material;
        }

        public static Material GetLinearGradientMaterial_Reflective(Color from, Color to)
        {
            return GetLinearGradientMaterial_Reflective_private(from, to).
                material;
        }
        public static Material GetLinearGradientMaterial_Unlit(Color from, Color to)
        {
            return GetLinearGradientMaterial_Unlit_private(from, to).
                material;
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
            Material material = null;
            if (_material != null)
            {
                material = _material;
            }
            else if (_color != null && _color2 != null)
            {
                #region gradient

                _solidBrush = null;
                _solidBrush_Unlit = null;

                if (_isReflectiveColor)
                {
                    if (_linearBrush != null && _linearBrush_Unlit == null)
                    {
                        _linearBrush.GradientStops[0].Color = _color.Value;
                        _linearBrush.GradientStops[1].Color = _color2.Value;

                        return;
                    }
                    else
                    {
                        // create a new one
                        _linearBrush = null;
                        _linearBrush_Unlit = null;

                        var result = GetLinearGradientMaterial_Reflective_private(_color.Value, _color2.Value);
                        _linearBrush = result.brush;
                        material = result.material;
                    }
                }
                else
                {
                    if (_linearBrush != null && _linearBrush_Unlit != null)
                    {
                        Color diffuseFrom = Colors.Black;
                        diffuseFrom.ScA = _color.Value.ScA;
                        Color diffuseTo = Colors.Black;
                        diffuseTo.ScA = _color2.Value.ScA;

                        _linearBrush.GradientStops[0].Color = diffuseFrom;
                        _linearBrush.GradientStops[1].Color = diffuseTo;

                        _linearBrush_Unlit.GradientStops[0].Color = _color.Value;
                        _linearBrush_Unlit.GradientStops[1].Color = _color2.Value;

                        return;
                    }
                    else
                    {
                        // create a new one
                        _linearBrush = null;
                        _linearBrush_Unlit = null;

                        var result = GetLinearGradientMaterial_Unlit_private(_color.Value, _color2.Value);
                        _linearBrush = result.brush;
                        _linearBrush_Unlit = result.brushUnlit;
                        material = result.material;
                    }
                }

                #endregion
            }
            else if (_color != null && _color2 == null)
            {
                #region solid

                _linearBrush = null;
                _linearBrush_Unlit = null;

                if (_isReflectiveColor)
                {
                    if (_solidBrush != null && _solidBrush_Unlit == null)
                    {
                        _solidBrush.Color = _color.Value;

                        return;
                    }
                    else
                    {
                        // create a new one
                        _solidBrush = null;
                        _solidBrush_Unlit = null;

                        var result = GetSolidColorMaterial_private(true, _color.Value);
                        _solidBrush = result.brush;
                        material = result.material;
                    }
                }
                else
                {
                    if (_solidBrush != null && _solidBrush_Unlit != null)
                    {
                        Color diffuse = Colors.Black;
                        diffuse.ScA = _color.Value.ScA;

                        _solidBrush.Color = diffuse;
                        _solidBrush_Unlit.Color = _color.Value;

                        return;
                    }
                    else
                    {
                        // create a new one
                        _solidBrush = null;
                        _solidBrush_Unlit = null;

                        var result = GetSolidColorMaterial_private(false, _color.Value);
                        _solidBrush = result.brush;
                        _solidBrush_Unlit = result.brushUnlit;
                        material = result.material;
                    }
                }

                #endregion
            }

            //NOTE: Material could be null

            _lineModel.Material = material;
            _lineModel.BackMaterial = material;

            if (_fromArrowModel != null)
            {
                _fromArrowModel.Material = material;
                _fromArrowModel.BackMaterial = material;
            }

            if (_toArrowModel != null)
            {
                _toArrowModel.Material = material;
                _toArrowModel.BackMaterial = material;
            }
        }

        private void InvalidateGeometry()
        {
            Vector3D direction = _toPoint - _fromPoint;
            double directionLength = direction.Length;

            if (Math1D.IsInvalid(directionLength) || Math1D.IsNearZero(directionLength))
            {
                _lineModel.Transform = new ScaleTransform3D(0, 0, 0);

                if (_fromArrowModel != null)
                {
                    _fromArrowModel.Transform = new ScaleTransform3D(0, 0, 0);
                }

                if (_toArrowModel != null)
                {
                    _toArrowModel.Transform = new ScaleTransform3D(0, 0, 0);
                }
                return;
            }

            Vector3D directionUnit = new Vector3D();
            double arrowWidth = 0;
            double arrowLength = 0;
            double halfArrowLength = 0;
            if ((_fromArrowModel != null && _fromArrowPercent != null) || (_toArrowModel != null && _toArrowPercent != null))       // they should both be null or not null together, just being safe
            {
                directionUnit = direction.ToUnit();
                arrowWidth = _thickness * _arrowBaseMult * _arrowSizeMult;
                arrowLength = _thickness * _arrowHeightMult * _arrowSizeMult;
                halfArrowLength = arrowLength / 2;
            }

            #region line

            double length = directionLength;

            // Reduce the length if the arrow is at the end so that the line graphic doesn't poke through the arrow graphic
            double? fromOffset = null;
            if (_fromArrowModel != null && _fromArrowPercent != null && _fromArrowPercent.Value.IsNearValue(1))
            {
                fromOffset = halfArrowLength;
                length -= halfArrowLength;
            }

            if (_toArrowModel != null && _toArrowPercent != null && _toArrowPercent.Value.IsNearValue(1))
            {
                length -= halfArrowLength;
            }

            Transform3DGroup transform = new Transform3DGroup();

            if (length > Math1D.NEARZERO)
            {
                transform.Children.Add(new ScaleTransform3D(_thickness, _thickness, length));

                if (fromOffset != null)
                {
                    transform.Children.Add(new TranslateTransform3D(0, 0, fromOffset.Value));
                }

                transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(_initialOrientation, StaticRandom.NextDouble(360d))));     // spin
                transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRotation(_initialOrientation, direction))));
                transform.Children.Add(new TranslateTransform3D(_fromPoint.ToVector()));
            }
            else
            {
                // The arrows are visible, but there's not enough length to show the line graphic
                transform.Children.Add(new ScaleTransform3D(0, 0, 0));
            }

            _lineModel.Transform = transform;

            #endregion
            #region from arrow

            if (_fromArrowModel != null && _fromArrowPercent != null)
            {
                Vector3D arrowOffset = directionUnit * arrowLength;
                Point3D arrowPosition = _fromPoint + (direction * (1d - _fromArrowPercent.Value));
                arrowPosition += arrowOffset;       // this is because the tip of the arrow should be that the percent, not the base

                transform = new Transform3DGroup();

                transform.Children.Add(new ScaleTransform3D(arrowWidth, arrowWidth, arrowLength));
                transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(_initialOrientation, StaticRandom.NextDouble(360d))));      // spin
                transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRotation(_initialOrientation, -direction))));
                transform.Children.Add(new TranslateTransform3D(arrowPosition.ToVector()));

                _fromArrowModel.Transform = transform;
            }

            #endregion
            #region to arrow

            if (_toArrowModel != null && _toArrowPercent != null)
            {
                Vector3D arrowOffset = directionUnit * arrowLength;
                Point3D arrowPosition = _fromPoint + (direction * _toArrowPercent.Value);
                arrowPosition -= arrowOffset;       // this is because the tip of the arrow should be that the percent, not the base

                transform = new Transform3DGroup();

                transform.Children.Add(new ScaleTransform3D(arrowWidth, arrowWidth, arrowLength));
                transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(_initialOrientation, StaticRandom.NextDouble(360d))));      // spin
                transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRotation(_initialOrientation, direction))));
                transform.Children.Add(new TranslateTransform3D(arrowPosition.ToVector()));

                _toArrowModel.Transform = transform;
            }

            #endregion
        }

        private static GeometryModel3D GetArrowModel()
        {
            return new GeometryModel3D()
            {
                Geometry = GetArrowMesh(new Point3D(0, 0, 0), new Point3D(0, 0, 1), 1),
            };
        }
        private static MeshGeometry3D GetArrowMesh(Point3D from, Point3D to, double thickness)
        {
            double half = thickness / 2d;

            Vector3D line = to - from;
            if (line.X == 0 && line.Y == 0 && line.Z == 0) line.X = 0.000000001d;

            Vector3D orth1 = Math3D.GetArbitraryOrhonganal(line);
            orth1 = Math3D.RotateAroundAxis(orth1, line, StaticRandom.NextDouble() * Math.PI * 2d);		// give it a random rotation so that if many lines are created by this method, they won't all be oriented the same
            orth1 = orth1.ToUnit() * half;

            Vector3D orth2 = Vector3D.CrossProduct(line, orth1);
            orth2 = orth2.ToUnit() * half;

            // Define 3D mesh object
            MeshGeometry3D retVal = new MeshGeometry3D();

            // Arrow Base
            retVal.Positions.Add(from - orth1);     // 0
            retVal.Positions.Add(from + orth1);     // 1
            retVal.Positions.Add(from - orth2);     // 2
            retVal.Positions.Add(from + orth2);     // 3

            // Arrow Tip
            retVal.Positions.Add(to);       // 4

            // Tip Faces
            retVal.TriangleIndices.Add(0);
            retVal.TriangleIndices.Add(3);
            retVal.TriangleIndices.Add(4);

            retVal.TriangleIndices.Add(3);
            retVal.TriangleIndices.Add(1);
            retVal.TriangleIndices.Add(4);

            retVal.TriangleIndices.Add(1);
            retVal.TriangleIndices.Add(2);
            retVal.TriangleIndices.Add(4);

            retVal.TriangleIndices.Add(2);
            retVal.TriangleIndices.Add(0);
            retVal.TriangleIndices.Add(4);

            // Base Faces
            //NOTE: These lines need to use as few triangles as possible, and they will almost certainly leave IsShiny false.  So a backplate isn't really needed
            //retVal.TriangleIndices.Add(0);
            //retVal.TriangleIndices.Add(2);
            //retVal.TriangleIndices.Add(1);

            //retVal.TriangleIndices.Add(1);
            //retVal.TriangleIndices.Add(3);
            //retVal.TriangleIndices.Add(0);

            // shouldn't I set normals?
            //retVal.Normals

            //retVal.Freeze();
            return retVal;
        }

        private static (Point from, Point to) GetLinearGradientPoints()
        {
            return (new Point(0, 0), new Point(1, 0));
        }

        public static (Material material, SolidColorBrush brush, SolidColorBrush brushUnlit) GetSolidColorMaterial_private(bool isReflective, Color color)
        {
            if (isReflective)
            {
                SolidColorBrush brush = new SolidColorBrush(color);
                DiffuseMaterial litMaterial = new DiffuseMaterial(brush);

                //litMaterial.Freeze();     // can't freeze, because this class supports changing the colors

                return (litMaterial, brush, null);
            }
            else
            {
                // This was copied from UtilityWPF.GetUnlitMaterial

                Color diffuse = Colors.Black;
                diffuse.ScA = color.ScA;

                MaterialGroup group = new MaterialGroup();

                SolidColorBrush brush = new SolidColorBrush(diffuse);
                group.Children.Add(new DiffuseMaterial(brush));

                SolidColorBrush brushUnlit = new SolidColorBrush(color);
                group.Children.Add(new EmissiveMaterial(brushUnlit));

                //group.Freeze();       // can't freeze, because this class supports changing the colors

                return (group, brush, brushUnlit);
            }
        }

        public static (Material material, LinearGradientBrush brush) GetLinearGradientMaterial_Reflective_private(Color from, Color to)
        {
            var points = GetLinearGradientPoints();

            var brush = new LinearGradientBrush(from, to, points.from, points.to);
            DiffuseMaterial material = new DiffuseMaterial(brush);

            //material.Freeze();        // can't freeze, because this class supports changing the colors

            return (material, brush);
        }
        public static (Material material, LinearGradientBrush brush, LinearGradientBrush brushUnlit) GetLinearGradientMaterial_Unlit_private(Color from, Color to)
        {
            // this is copied from UtilityWPF.GetUnlitMaterial

            Color diffuseFrom = Colors.Black;
            diffuseFrom.ScA = from.ScA;

            Color diffuseTo = Colors.Black;
            diffuseTo.ScA = from.ScA;

            var points = GetLinearGradientPoints();

            MaterialGroup material = new MaterialGroup();

            var brush = new LinearGradientBrush(diffuseFrom, diffuseTo, points.from, points.to);
            material.Children.Add(new DiffuseMaterial(brush));

            var brushUnlit = new LinearGradientBrush(from, to, points.from, points.to);
            material.Children.Add(new EmissiveMaterial(brushUnlit));

            //material.Freeze();        // can't freeze, because this class supports changing the colors

            return (material, brush, brushUnlit);
        }

        #endregion
    }

    #endregion
    #region class: BillboardLine3DSet

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

        private readonly bool _shouldUseSpinTimer;

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

        public BillboardLine3DSet(bool shouldUseSpinTimer = false)
        {
            _shouldUseSpinTimer = shouldUseSpinTimer;

            _geometry = new Model3DGroup();
            Content = _geometry;
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
                BillboardLine3D line = new BillboardLine3D(_shouldUseSpinTimer);
                line.Material = _material;

                _lines.Add(line);
            }

            _lineCount = _lineCount.Value + 1;

            // Whatever line was at this index now gets this position
            _lines[_lineCount.Value - 1].SetPoints(start, stop, thickness);

            if (_lineCount.Value > _geometry.Children.Count)
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
            _material = BillboardLine3D.GetSolidColorMaterial(_isReflectiveColor, _color);

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
