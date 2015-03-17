using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.Controls;
using Game.Newt.v2.GameItems.MapParts;
using Game.HelperClassesWPF;

namespace Game.Newt.v2.GenePool
{
    public class SelectedItemSwimbots : MapObject_ChasePoint_Direct
    {
        #region Declaration Section

        const string MESSAGECAPTION = "Swimbot Selection";

        private readonly Canvas _canvas;
        private readonly PerspectiveCamera _camera;
        private readonly FrameworkElement _viewportContainer;

        private Ellipse _highlight = null;

        #endregion

        #region Constructor

        public SelectedItemSwimbots(IMapObject item, Vector3D offset, ShipViewerWindow shipViewer, FrameworkElement viewportContainer, Viewport3D viewport, Canvas canvas, PerspectiveCamera camera, bool shouldMoveWidthSpring, bool shouldSpringCauseTorque, Color? springColor)
            : base(item, offset, shouldMoveWidthSpring, shouldSpringCauseTorque, true, viewport, springColor)
        {
            this.Viewer = shipViewer;
            _viewportContainer = viewportContainer;
            _canvas = canvas;
            _camera = camera;

            if (this.Viewer != null)
            {
                this.Viewer.Closed += new EventHandler(Viewer_Closed);
            }

            item.PhysicsBody.BodyMoved += new EventHandler(Item_BodyMoved);
            _camera.Changed += new EventHandler(Camera_Changed);
            _viewportContainer.SizeChanged += new SizeChangedEventHandler(ViewportContainer_SizeChanged);

            UpdateHightlight();
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// It's possible for this to become null if they close the viewer independent of unselecting the item
        /// </summary>
        public ShipViewerWindow Viewer;

        #endregion

        #region Overrides

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.Viewer != null)
                {
                    this.Viewer.Close();
                    this.Viewer = null;
                }

                if (_canvas != null && _highlight != null)
                {
                    _canvas.Children.Remove(_highlight);
                    _highlight = null;
                }

                if (_camera != null)
                {
                    _camera.Changed -= new EventHandler(Camera_Changed);
                }

                if (this.Item != null)
                {
                    this.Item.PhysicsBody.BodyMoved -= new EventHandler(Item_BodyMoved);
                }

                if (_viewportContainer != null)
                {
                    _viewportContainer.SizeChanged -= new SizeChangedEventHandler(ViewportContainer_SizeChanged);
                }
            }

            base.Dispose(disposing);
        }

        #endregion

        #region Event Listeners

        private void Viewer_Closed(object sender, EventArgs e)
        {
            this.Viewer = null;
        }

        private void Camera_Changed(object sender, EventArgs e)
        {
            try
            {
                UpdateHightlight();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), MESSAGECAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Item_BodyMoved(object sender, EventArgs e)
        {
            try
            {
                UpdateHightlight();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), MESSAGECAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ViewportContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                UpdateHightlight();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), MESSAGECAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private void UpdateHightlight()
        {
            bool isInFront;
            var circle = UtilityWPF.Project3Dto2D(out isInFront, _viewport, this.Item.PositionWorld, this.Item.Radius * 1.2d);

            if (circle != null && isInFront)
            {
                // Camera can see it, make sure the highlight is created
                if (_highlight == null)
                {
                    _highlight = new Ellipse();

                    _highlight.Stroke = new SolidColorBrush(UtilityWPF.ColorFromHex("56FFFFFF"));

                    RadialGradientBrush brush = new RadialGradientBrush();
                    brush.GradientStops.Add(new GradientStop(UtilityWPF.ColorFromHex("#00000000"), 0d));
                    brush.GradientStops.Add(new GradientStop(UtilityWPF.ColorFromHex("#00BEBEBE"), .635548));
                    brush.GradientStops.Add(new GradientStop(UtilityWPF.ColorFromHex("#06D6D6D6"), .771993d));
                    brush.GradientStops.Add(new GradientStop(UtilityWPF.ColorFromHex("#1BDEDEDE"), .906643d));
                    brush.GradientStops.Add(new GradientStop(UtilityWPF.ColorFromHex("#37E9E9E9"), .965889d));
                    brush.GradientStops.Add(new GradientStop(UtilityWPF.ColorFromHex("#65FFFFFF"), 1d));
                    _highlight.Fill = brush;

                    _canvas.Children.Add(_highlight);
                }

                // I like to move it move it
                _highlight.Width = circle.Item2 * 2d;
                _highlight.Height = circle.Item2 * 2d;
                Canvas.SetLeft(_highlight, circle.Item1.X - circle.Item2);
                Canvas.SetTop(_highlight, circle.Item1.Y - circle.Item2);
            }
            else
            {
                // Camera can't see it, make sure the highlight is gone
                if (_highlight != null)
                {
                    _canvas.Children.Remove(_highlight);
                    _highlight = null;
                }
            }
        }

        #endregion
    }
}
