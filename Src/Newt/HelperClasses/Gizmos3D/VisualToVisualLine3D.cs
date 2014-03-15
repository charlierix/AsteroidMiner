using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Media3D;
using Game.Newt.HelperClasses.Primitives3D;
using System.Windows.Media;

namespace Game.Newt.HelperClasses.Gizmos3D
{
    public partial class VisualToVisualLine3D : ModelVisual3D, IDisposable
    {
        private ScreenSpaceLines3D _line;
        private bool _autoUpdate;

        public VisualToVisualLine3D()
        {
            _autoUpdate = true;

            _line = new ScreenSpaceLines3D(false);
            _line.RebuildingGeometry += _line_RebuildingGeometry;

            Children.Add(_line);

            if (_autoUpdate)
                CompositionTarget.Rendering += OnRender;
        }

        public ScreenSpaceLines3D Line
        {
            get { return _line; }
        }

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
                if (_autoUpdate)
                    CompositionTarget.Rendering -= OnRender;

                Viewport3DHelper.Dispose(this.Children, true);
            }
        }

        #endregion

        private void OnRender(object sender, EventArgs e)
        {
            _line.Render();
        }

        private void _line_RebuildingGeometry(object sender, EventArgs e)
        {
            ModelVisual3D fromVisual = this.FromVisual;
            ModelVisual3D toVisual = this.ToVisual;

            if ((fromVisual != null) && (toVisual != null))
            {
                Matrix3D fromMatrix = MathUtils.GetTransformToWorld(fromVisual);
                Matrix3D toMatrix = MathUtils.GetTransformToWorld(toVisual);

                _line.Clear();
                _line.AddLine(fromMatrix.Transform(this.FromOffset), toMatrix.Transform(this.ToOffset));
                //_line.AddLine(new Point3D(0, 0, 0), new Point3D(4, 4, -10));
            }
        }
    }
}
