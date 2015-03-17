using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;

namespace Game.Newt.v2.Arcanorum
{
    #region ORIG

    //public class CustomMouseCursor : Canvas
    //{
    //    #region Declaration Section

    //    // parent and canvas need to be identical size (canvas won't have color, it just contains 2D visuals)
    //    /// <summary>
    //    /// This is the panel that the mouse events should come from
    //    /// </summary>
    //    private readonly FrameworkElement _parent;
    //    /// <summary>
    //    /// This is the canvas that this visual is placed in
    //    /// </summary>
    //    //private readonly Canvas _canvas;      // this isn't needed

    //    private readonly Bot _player;

    //    private Point? _position = null;

    //    private TranslateTransform _transform = null;

    //    #endregion

    //    #region Constructor

    //    public CustomMouseCursor(FrameworkElement parent, Bot player)
    //    {
    //        _parent = parent;
    //        _player = player;

    //        parent.MouseMove += Parent_MouseMove;
    //        parent.MouseEnter += Parent_MouseEnter;
    //        parent.MouseLeave += Parent_MouseLeave;



    //        //TODO: Dynamically change appearence
    //        this.Width = 30;
    //        this.Height = 30;

    //        this.HorizontalAlignment = HorizontalAlignment.Left;
    //        this.VerticalAlignment = VerticalAlignment.Top;
    //        this.Background = Brushes.Transparent;

    //        _transform = new TranslateTransform(0, 0);
    //        this.RenderTransform = _transform;



    //        // Test
    //        //TODO: Don't use things that derive from framework element, they are expensive
    //        Ellipse testCircle = new Ellipse();
    //        testCircle.IsHitTestVisible = false;
    //        testCircle.Fill = new SolidColorBrush(UtilityWPF.ColorFromHex("80FFFFFF"));
    //        testCircle.Stroke = new SolidColorBrush(UtilityWPF.ColorFromHex("80000000"));
    //        testCircle.StrokeThickness = 1;
    //        testCircle.Width = 28;
    //        testCircle.Height = 28;
    //        testCircle.HorizontalAlignment = HorizontalAlignment.Center;
    //        testCircle.VerticalAlignment = VerticalAlignment.Center;
    //        testCircle.RenderTransform = new TranslateTransform(testCircle.Width / -2d, testCircle.Height / -2d);

    //        this.Children.Add(testCircle);





    //        this.IsHitTestVisible = false;
    //        this.Cursor = Cursors.None;
    //        _parent.Cursor = Cursors.None;
    //    }

    //    #endregion

    //    #region Event Listeners

    //    private void Parent_MouseEnter(object sender, MouseEventArgs e)
    //    {
    //        _position = e.GetPosition(_parent);

    //        this.Visibility = Visibility.Visible;

    //        UpdatePosition();
    //    }
    //    private void Parent_MouseLeave(object sender, MouseEventArgs e)
    //    {
    //        _position = null;

    //        this.Visibility = Visibility.Hidden;

    //        //UpdatePosition();
    //    }

    //    private void Parent_MouseMove(object sender, MouseEventArgs e)
    //    {
    //        _position = e.GetPosition(_parent);

    //        UpdatePosition();
    //    }

    //    #endregion

    //    #region Public Methods

    //    public void Update(Point3D? point)
    //    {
    //        const double DEFAULTSIZE = 30d;

    //        if (point == null)
    //        {
    //            this.Width = DEFAULTSIZE;
    //            this.Height = DEFAULTSIZE;
    //            return;
    //        }

    //        Vector3D line = point.Value - _player.PositionWorld;

    //        double size = UtilityHelper.GetScaledValue_Capped(4d, DEFAULTSIZE, _player.Radius, _player.Radius * 5, line.Length);

    //        this.Width = size;
    //        this.Height = size;

    //    }

    //    #endregion

    //    #region Private Methods

    //    private void UpdatePosition()
    //    {
    //        if (_position != null)
    //        {
    //            _transform.X = _position.Value.X;
    //            _transform.Y = _position.Value.Y;
    //        }
    //    }

    //    #endregion
    //}

    #endregion

    public class CustomMouseCursor : FrameworkElement
    {
        #region Declaration Section

        private VisualCollection _children = null;

        // parent and canvas need to be identical size (canvas won't have color, it just contains 2D visuals)
        /// <summary>
        /// This is the panel that the mouse events should come from
        /// </summary>
        private readonly FrameworkElement _parent;
        /// <summary>
        /// This is the canvas that this visual is placed in
        /// </summary>
        //private readonly Canvas _canvas;      // this isn't needed

        private readonly Bot _player;

        private Point? _position = null;

        private ScaleTransform _scale = null;
        private TranslateTransform _translate = null;

        private SolidColorBrush _brush = null;
        private Color _color_NoWeapon = UtilityWPF.ColorFromHex("80FFFFFF");

        #endregion

        #region Constructor

        public CustomMouseCursor(FrameworkElement parent, Bot player)
        {
            _parent = parent;
            _player = player;

            parent.MouseMove += Parent_MouseMove;
            parent.MouseEnter += Parent_MouseEnter;
            parent.MouseLeave += Parent_MouseLeave;

            _children = new VisualCollection(this);
            var ellipse = CreateEllipse(30);
            _brush = ellipse.Item2;
            _children.Add(ellipse.Item1);

            this.Width = 30;
            this.Height = 30;

            this.HorizontalAlignment = HorizontalAlignment.Left;
            this.VerticalAlignment = VerticalAlignment.Top;

            this.IsHitTestVisible = false;
            this.Cursor = Cursors.None;
            _parent.Cursor = Cursors.None;

            // Transform
            _scale = new ScaleTransform(1, 1);
            _translate = new TranslateTransform(0, 0);
            TransformGroup transform = new TransformGroup();
            transform.Children.Add(_scale);
            transform.Children.Add(_translate);
            this.RenderTransform = transform;
        }

        #endregion

        #region Event Listeners

        private void Parent_MouseEnter(object sender, MouseEventArgs e)
        {
            _position = e.GetPosition(_parent);

            this.Visibility = Visibility.Visible;

            UpdatePosition();
        }
        private void Parent_MouseLeave(object sender, MouseEventArgs e)
        {
            _position = null;

            this.Visibility = Visibility.Hidden;

            //UpdatePosition();
        }

        private void Parent_MouseMove(object sender, MouseEventArgs e)
        {
            _position = e.GetPosition(_parent);

            UpdatePosition();
        }

        #endregion

        #region Public Methods

        public void Update(Point3D? point)
        {
            if (point == null)
            {
                _scale.ScaleX = 1d;
                _scale.ScaleY = 1d;
                //_brush.Color = _color_NoWeapon;
                return;
            }

            Vector3D line = point.Value - _player.PositionWorld;
            double lineLength = line.Length;

            #region Scale, Opacity

            double scale, opacity;
            if (lineLength < _player.Radius)
            {
                scale = UtilityCore.GetScaledValue_Capped(.01d, .1d, 0d, _player.Radius, lineLength);
                opacity = UtilityCore.GetScaledValue_Capped(0d, 1d, 0d, _player.Radius, lineLength);
            }
            else
            {
                scale = UtilityCore.GetScaledValue_Capped(.1d, 1d, _player.Radius, _player.Radius * 3.5, lineLength);
                opacity = 1d;
            }

            _scale.ScaleX = scale;
            _scale.ScaleY = scale;

            this.Opacity = opacity;

            #endregion

            //TODO: Give a visual indication of whether they are speeding up or slowing down the swing of the weapon
            #region Color

            //if (_player.Weapon == null)
            //{
            //    _brush.Color = _color_NoWeapon;
            //}
            //else
            //{
            //    //Vector3D weaponDirection = _player.Weapon.PositionWorld - _player.PositionWorld;
            //    //Vector3D angVelocity = _player.Weapon.PhysicsBody.AngularVelocity;
            //    //Vector3D velocity = _player.VelocityWorld;



            // Most change is orth to the velocity?, so that should be most red or green


            // The ideal place for the mouse to be is opposite of the heavier part of the weapon.  Then based on the direction the weapon is swinging, pull or push










            //    _brush.Color = _color_NoWeapon;
            //}

            #endregion
        }

        #endregion
        #region Protected Methods

        protected override int VisualChildrenCount
        {
            get
            {
                return _children.Count;
            }
        }

        protected override Visual GetVisualChild(int index)
        {
            if (index < 0 || index >= _children.Count)
            {
                throw new ArgumentOutOfRangeException("index: " + index.ToString() + ", ChildCount: " + _children.Count.ToString());
            }

            return _children[index];
        }

        #endregion

        #region Private Methods

        private void UpdatePosition()
        {
            if (_position != null)
            {
                _translate.X = _position.Value.X;
                _translate.Y = _position.Value.Y;
            }
        }

        private static Tuple<DrawingVisual, SolidColorBrush> CreateEllipse(double size)
        {
            DrawingVisual retVal = new DrawingVisual();

            DrawingContext context = retVal.RenderOpen();

            double half = size / 2d;

            SolidColorBrush brush = new SolidColorBrush(UtilityWPF.ColorFromHex("80FFFFFF"));

            context.DrawEllipse(
                brush,
                new Pen(new SolidColorBrush(UtilityWPF.ColorFromHex("80000000")), 1),
                new Point(0, 0), half, half);

            // Persist the drawing content.
            context.Close();

            return Tuple.Create(retVal, brush);
        }

        #endregion
    }
}
