using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Media3D;

namespace Game.Newt.HelperClasses
{
    public class Viewport3D : System.Windows.Controls.Viewport3D
    {
        protected override void OnRender(System.Windows.Media.DrawingContext drawingContext)
        {
            ProcessChildren(this.Children);

            base.OnRender(drawingContext);
        }

        private void ProcessChildren(Visual3DCollection items)
        {
            foreach (Visual3D item in items)
            {
                IRenderNotify render = (item as IRenderNotify);
                if (render != null)
                    render.Render();

                ModelVisual3D visual = (item as ModelVisual3D);
                if (visual != null)
                    ProcessChildren(visual.Children);
            }
        }
    }
}
