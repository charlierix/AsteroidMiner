using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Game.Newt.Testers.FluidFields
{
    public partial class VelocityVisualizer2DWindow : Window
    {
        #region Declaration Section

        private Line[] _lineVisuals = null;

        private Brush _lineBrush = Brushes.DodgerBlue;

        private VelocityVisualizerVisualHost _visualHost = null;

        #endregion

        #region Constructor

        public VelocityVisualizer2DWindow()
        {
            InitializeComponent();
        }

        #endregion

        #region Public Properties

        private FluidField2D _field = null;
        public FluidField2D Field
        {
            get
            {
                return _field;
            }
            set
            {
                _field = value;

                ResetField();
            }
        }

        #endregion

        #region Public Methods

        public void Update()
        {
            if (_field == null)
            {
                return;
            }



            _visualHost.Update();






            //double[] xVel = _field.XVel;
            //double[] yVel = _field.YVel;

            //if (xVel.Length != yVel.Length)
            //{
            //    throw new ApplicationException("X and Y arrays aren't the same size: " + xVel.Length.ToString() + ", " + yVel.Length.ToString());
            //}
            //if (xVel.Length != _lineVisuals.Length)
            //{
            //    ResetField();
            //    return;     // try again next update
            //}

            //for (int cntr = 0; cntr < xVel.Length; cntr++)
            //{
            //    _lineVisuals[cntr].X2 = _lineVisuals[cntr].X1 + xVel[cntr];
            //    _lineVisuals[cntr].Y2 = _lineVisuals[cntr].Y1 + yVel[cntr];
            //}
        }

        #endregion

        #region Private Methods

        private void ResetField()
        {
            grdVelocities.Children.Clear();
            _visualHost = null;

            if (_field == null)
            {
                return;
            }

            _visualHost = new VelocityVisualizerVisualHost(_field);
            grdVelocities.Children.Add(_visualHost);
        }

        #endregion
    }

    #region class: VelocityVisualizerVisualHost

    public class VelocityVisualizerVisualHost : FrameworkElement
    {
        #region Declaration Section

        private DrawingVisual _visual = new DrawingVisual();
        private Pen _pen = new Pen(Brushes.Gold, 1);

        private FluidField2D _field = null;

        #endregion

        #region Constructor

        public VelocityVisualizerVisualHost(FluidField2D field)
        {
            _field = field;

            this.AddVisualChild(_visual);
        }

        #endregion

        #region Public Methods

        public void Update()
        {
            const int NUMSAMPLES = 20;

            double width = this.ActualWidth;
            double height = this.ActualHeight;

            if (double.IsNaN(width) || double.IsInfinity(width))
            {
                // This control isn't visible yet
                return;
            }

            DrawingContext drawing = _visual.RenderOpen();

            double[] xVel, yVel;

            xVel = _field.XVel;
            yVel = _field.YVel;

            int xSize = _field.XSize;
            int ySize = _field.YSize;

            double cellWidth = width / xSize;
            double cellHeight = height / ySize;
            double cellHalfWidth = cellWidth / 2d;
            double cellHalfHeight = cellHeight / 2d;

            double scale = (width + height) / 2d;

            double dotRadius = (cellWidth + cellHeight) / 5d;

            // Always include 0 and size - 1.  The rest should be evenly distributed
            double incX = Convert.ToDouble(xSize - 1) / (NUMSAMPLES - 1);        // subtracting 1 to guarantee the edges get one
            double incY = Convert.ToDouble(ySize - 1) / (NUMSAMPLES - 1);

            for (double xd = 0; xd < xSize; xd += incX)
            {
                for (double yd = 0; yd < ySize; yd += incY)
                {
                    int x = Convert.ToInt32(Math.Round(xd));
                    if (x > xSize - 1) x = xSize - 1;
                    int y = Convert.ToInt32(Math.Round(yd));
                    if (y > ySize - 1) y = ySize - 1;

                    // Figure out the center of this cell
                    Point center = new Point((cellWidth * x) + cellHalfWidth, (cellHeight * y) + cellHalfHeight);

                    // Add the velocity at this cell
                    int index = FluidField2D.GetK(xSize, x, y);
                    Point end = new Point(center.X + (xVel[index] * scale), center.Y + (yVel[index] * scale));

                    drawing.DrawLine(_pen, center, end);

                    //drawing.DrawEllipse(Brushes.Gold, _pen, center, dotRadius, dotRadius);        // too expensive
                }
            }

            drawing.Close();

            this.InvalidateVisual();
        }

        #endregion

        #region Overrides

        protected override int VisualChildrenCount
        {
            get { return 1; }
        }

        protected override Visual GetVisualChild(int index)
        {
            return _visual;
        }

        #endregion
    }

    #endregion
}
