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
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

using Game.HelperClassesCore;
using Game.Newt.v2.GameItems;
using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Controls3D;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.Testers.FluidFields
{
    public partial class VelocityVisualizer3DWindow : Window
    {
        #region enum: LinePlacementType

        public enum LinePlacementType
        {
            Grid,
            PlateXY,
            PlateXZ,
            PlateYZ,
            RandomInstant,
            RandomPersist
        }

        #endregion

        #region Declaration Section

        private double _sizeMult = 1d;
        private double _velocityMult = 1d;

        /// <summary>
        /// This listens to the mouse/keyboard and controls the camera
        /// </summary>
        private TrackBallRoam _trackball = null;

        private BillboardLine3DSet _velocityLines = null;

        private ScreenSpaceLines3D _border = null;

        private DoubleVector? _lookDirection = null;
        private ScreenSpaceLines3D _marker2D = null;

        private bool _isBlockedCellDirty = false;
        private ScreenSpaceLines3D _blockedCellsWireframe = null;

        private DateTime _sceneRemaining = DateTime.MinValue;

        private Mapping_3D_1D[] _randPersistIndices = null;

        private int _plateCurrentIndex = -1;
        private Point? _mousePoint = null;

        #endregion

        #region Constructor

        public VelocityVisualizer3DWindow()
        {
            InitializeComponent();

            // Camera Trackball
            _trackball = new TrackBallRoam(_camera);
            _trackball.EventSource = grdViewPort;		//NOTE:  If this control doesn't have a background color set, the trackball won't see events (I think transparent is ok, just not null)
            _trackball.AllowZoomOnMouseWheel = true;
            _trackball.MouseWheelScale *= .1d;
            _trackball.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.MouseComplete));
            //_trackball.GetOrbitRadius += new GetOrbitRadiusHandler(Trackball_GetOrbitRadius);

            // Velocity line visual
            _velocityLines = new BillboardLine3DSet();
            _velocityLines.Color = Colors.GhostWhite;
            _velocityLines.IsReflectiveColor = true;
            _viewport.Children.Add(_velocityLines);

            UpdateLinePlacement();

            ShowHideBlockedCells();
        }

        #endregion

        #region Public Properties

        private FluidField3D _field = null;
        public FluidField3D Field
        {
            get
            {
                return _field;
            }
            set
            {
                if (_field != null)
                {
                    _field.BlockedCellsChanged -= Field_BlockedCellsChanged;
                }

                _field = value;

                _field.BlockedCellsChanged += Field_BlockedCellsChanged;

                ResetField();
            }
        }

        public LinePlacementType LinePlacement
        {
            get;
            set;        // no need to listen for a change, this gets examined during each update
        }

        #endregion

        #region Public Methods

        public void Update()
        {
            const int NUMSAMPLES = 7;

            if (_field == null)
            {
                return;
            }

            if (_isBlockedCellDirty)
            {
                ShowHideBlockedCells();
            }

            double half = (_field.Size * _sizeMult) / 2d;
            double lineThickness = _sizeMult * .12;

            switch (this.LinePlacement)
            {
                case LinePlacementType.Grid:
                    DrawLines_Grid(NUMSAMPLES, half, lineThickness);
                    break;

                case LinePlacementType.PlateXY:
                    DrawLines_Plate(NUMSAMPLES, half, lineThickness, new AxisFor(Axis.X, -1, -1), new AxisFor(Axis.Y, -1, -1), new AxisFor(Axis.Z, -1, -1));
                    break;

                case LinePlacementType.PlateXZ:
                    DrawLines_Plate(NUMSAMPLES, half, lineThickness, new AxisFor(Axis.X, -1, -1), new AxisFor(Axis.Z, -1, -1), new AxisFor(Axis.Y, -1, -1));
                    break;

                case LinePlacementType.PlateYZ:
                    DrawLines_Plate(NUMSAMPLES, half, lineThickness, new AxisFor(Axis.Y, -1, -1), new AxisFor(Axis.Z, -1, -1), new AxisFor(Axis.X, -1, -1));
                    break;

                case LinePlacementType.RandomInstant:
                    DrawLines_RandomInstant(NUMSAMPLES, half, lineThickness);
                    break;

                case LinePlacementType.RandomPersist:
                    DrawLines_RandomPersist(NUMSAMPLES, half, lineThickness);
                    break;

                default:
                    throw new ApplicationException("Unknown LinePlacementType: " + this.LinePlacement.ToString());
            }
        }

        public void ViewChanged(DoubleVector lookDirection)
        {
            if (_marker2D != null)
            {
                _viewport.Children.Remove(_marker2D);
            }

            if (_marker2D == null)
            {
                _marker2D = new ScreenSpaceLines3D();
                _marker2D.Color = UtilityWPF.ColorFromHex("60A0A0A0");
                _marker2D.Thickness = 4d;
            }

            _marker2D.Clear();

            RotateTransform3D transform = new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRotation(new DoubleVector(0, 0, 1, 0, -1, 0), lookDirection)));

            #region Marker 2D

            double orthDist = (_sizeMult * _field.Size * 1.1d) / 2d;
            double cornerDist = (_sizeMult * _field.Size * .2d) / 2d;

            // TopLeft
            Point3D corner = transform.Transform(new Point3D(-orthDist, -orthDist, -orthDist));
            Vector3D direction = transform.Transform(new Vector3D(cornerDist, 0, 0));
            _marker2D.AddLine(corner, corner + direction);
            direction = transform.Transform(new Vector3D(0, cornerDist, 0));
            _marker2D.AddLine(corner, corner + direction);

            //TopRight
            corner = transform.Transform(new Point3D(orthDist, -orthDist, -orthDist));
            direction = transform.Transform(new Vector3D(-cornerDist, 0, 0));
            _marker2D.AddLine(corner, corner + direction);
            direction = transform.Transform(new Vector3D(0, cornerDist, 0));
            _marker2D.AddLine(corner, corner + direction);

            //BottomRight
            corner = transform.Transform(new Point3D(orthDist, orthDist, -orthDist));
            direction = transform.Transform(new Vector3D(-cornerDist, 0, 0));
            _marker2D.AddLine(corner, corner + direction);
            direction = transform.Transform(new Vector3D(0, -cornerDist, 0));
            _marker2D.AddLine(corner, corner + direction);

            //BottomLeft
            corner = transform.Transform(new Point3D(-orthDist, orthDist, -orthDist));
            direction = transform.Transform(new Vector3D(cornerDist, 0, 0));
            _marker2D.AddLine(corner, corner + direction);
            direction = transform.Transform(new Vector3D(0, -cornerDist, 0));
            _marker2D.AddLine(corner, corner + direction);

            _viewport.Children.Add(_marker2D);

            #endregion

            // Camera
            _camera.Position = transform.Transform(new Point3D(0, 0, (_sizeMult * _field.Size * -4d) / 2d));
            _camera.LookDirection = lookDirection.Standard;
            _camera.UpDirection = lookDirection.Orth;

            // Remember it
            _lookDirection = lookDirection;
        }

        #endregion

        #region Event Listeners

        private void Window_Closed(object sender, EventArgs e)
        {
            try
            {
                if (_velocityLines != null)
                {
                    _velocityLines.Dispose();
                    _velocityLines = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void grdViewPort_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                _mousePoint = e.GetPosition(grdViewPort);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void grdViewPort_MouseLeave(object sender, MouseEventArgs e)
        {
            _mousePoint = null;
        }

        private void Field_BlockedCellsChanged(object sender, EventArgs e)
        {
            _isBlockedCellDirty = true;
        }

        private void RadioOption_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateLinePlacement();

                expanderOptions.IsExpanded = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void chkShowBlockedCells_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowHideBlockedCells();

                expanderOptions.IsExpanded = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private void ResetField()
        {
            _sceneRemaining = DateTime.MinValue;

            if (_border != null)
            {
                _viewport.Children.Remove(_border);
            }

            if (_marker2D != null)
            {
                _viewport.Children.Remove(_marker2D);
            }

            _velocityLines.Clear();

            _sizeMult = 1d / _field.Size;
            _velocityMult = 4d;

            // Border Lines
            _border = new ScreenSpaceLines3D();
            _border.Color = Colors.DimGray;
            _border.Thickness = 1d;

            double l = (_field.Size * _sizeMult) / 2d;       // using lower case l to represent half (just because it looks a lot like 1)
            _border.AddLine(new Point3D(-l, -l, -l), new Point3D(l, -l, -l));
            _border.AddLine(new Point3D(l, -l, -l), new Point3D(l, l, -l));
            _border.AddLine(new Point3D(l, l, -l), new Point3D(-l, l, -l));
            _border.AddLine(new Point3D(-l, l, -l), new Point3D(-l, -l, -l));

            _border.AddLine(new Point3D(-l, -l, l), new Point3D(l, -l, l));
            _border.AddLine(new Point3D(l, -l, l), new Point3D(l, l, l));
            _border.AddLine(new Point3D(l, l, l), new Point3D(-l, l, l));
            _border.AddLine(new Point3D(-l, l, l), new Point3D(-l, -l, l));

            _border.AddLine(new Point3D(-l, -l, -l), new Point3D(-l, -l, l));
            _border.AddLine(new Point3D(l, -l, -l), new Point3D(l, -l, l));
            _border.AddLine(new Point3D(l, l, -l), new Point3D(l, l, l));
            _border.AddLine(new Point3D(-l, l, -l), new Point3D(-l, l, l));

            _viewport.Children.Add(_border);

            ShowHideBlockedCells();     // this will update _blockedCellsWireframe

            if (_lookDirection != null)
            {
                ViewChanged(_lookDirection.Value);      // this will update _marker2D
            }
        }
        private void ShowHideBlockedCells()
        {
            _isBlockedCellDirty = false;

            // Wipe out the old one
            if (_blockedCellsWireframe != null)
            {
                _viewport.Children.Remove(_blockedCellsWireframe);
                _blockedCellsWireframe = null;
            }

            // See if a new one needs to be created
            if (!chkShowBlockedCells.IsChecked.Value || _field == null)
            {
                return;
            }

            int[] blockedCellIndices = Enumerable.Range(0, _field.Size1D).Where(o => _field.Blocked[o]).ToArray();
            if (blockedCellIndices.Length == 0)
            {
                return;
            }

            Rectangle3DIndexedMapped[] cells = _field.GetCells(_sizeMult * _field.Size);

            // Get a deduped list of blocked cell's edge lines
            var lines = Rectangle3DIndexed.GetEdgeLinesDeduped(blockedCellIndices.Select(o => cells[o]));

            // Create the visual
            _blockedCellsWireframe = new ScreenSpaceLines3D();
            _blockedCellsWireframe.Color = UtilityWPF.ColorFromHex("60FFFFFF");
            _blockedCellsWireframe.Thickness = 1;

            Point3D[] allPoints = cells[0].AllPoints;

            foreach (var line in lines)
            {
                _blockedCellsWireframe.AddLine(allPoints[line.Item1], allPoints[line.Item2]);
            }

            _viewport.Children.Add(_blockedCellsWireframe);
        }

        private void UpdateLinePlacement()
        {
            if (radPlacementGrid.IsChecked.Value)
            {
                this.LinePlacement = LinePlacementType.Grid;
            }
            else if (radPlacementPlateXY.IsChecked.Value)
            {
                this.LinePlacement = LinePlacementType.PlateXY;
            }
            else if (radPlacementPlateXZ.IsChecked.Value)
            {
                this.LinePlacement = LinePlacementType.PlateXZ;
            }
            else if (radPlacementPlateYZ.IsChecked.Value)
            {
                this.LinePlacement = LinePlacementType.PlateYZ;
            }
            else if (radPlacementRandomInstant.IsChecked.Value)
            {
                this.LinePlacement = LinePlacementType.RandomInstant;
            }
            else if (radPlacementRandomPersist.IsChecked.Value)
            {
                this.LinePlacement = LinePlacementType.RandomPersist;
            }
            else
            {
                throw new ApplicationException("Unknown line placement option");
            }
        }

        private void DrawLines_Grid(int numSamples, double half, double lineThickness)
        {
            double[] velX = _field.VelocityX;
            double[] velY = _field.VelocityY;
            double[] velZ = _field.VelocityZ;

            // Always include 0 and size - 1.  The rest should be evenly distributed
            double increment = Convert.ToDouble(_field.Size - 1) / (numSamples - 1);        // subtracting 1 to guarantee the edges get one

            _velocityLines.BeginAddingLines();

            // Try all of them
            for (double x = 0; x < _field.Size; x += increment)
            {
                for (double y = 0; y < _field.Size; y += increment)
                {
                    for (double z = 0; z < _field.Size; z += increment)
                    {
                        int ix = Convert.ToInt32(Math.Round(x));
                        if (ix > _field.Size - 1) ix = _field.Size - 1;
                        int iy = Convert.ToInt32(Math.Round(y));
                        if (iy > _field.Size - 1) iy = _field.Size - 1;
                        int iz = Convert.ToInt32(Math.Round(z));
                        if (iz > _field.Size - 1) iz = _field.Size - 1;

                        int index = _field.Get1DIndex(ix, iy, iz);

                        DrawLinesSprtAddLine(ix, iy, iz, index, half, lineThickness, velX, velY, velZ);
                    }
                }
            }

            _velocityLines.EndAddingLines();
        }
        private void DrawLines_Plate(int numSamples, double half, double lineThickness, AxisFor axisX, AxisFor axisY, AxisFor axisZ)
        {
            const double ELAPSEDURATIONSECONDS = 1;

            // Figure out how wide to make the plate
            int totalSamples = numSamples * numSamples * numSamples;        // numsamples is per axis, so cube it
            int cellsPerSlice = _field.Size * _field.Size;
            int numSlices = Convert.ToInt32(Math.Round(Convert.ToDouble(totalSamples) / Convert.ToDouble(cellsPerSlice)));
            if (numSlices == 0)
            {
                numSlices = 1;
            }

            int toOffset = numSlices / 2;
            int fromOffset = numSlices - toOffset - 1;

            DateTime now = DateTime.UtcNow;

            bool isOverField = false;

            if (_mousePoint != null)
            {
                #region Snap to mouse

                // Cast a ray (Copied this from ItemSelectDragLogic.ChangeDragPlane, DragItem)

                Point3D point = new Point3D(0, 0, 0);

                RayHitTestParameters cameraLookCenter = UtilityWPF.RayFromViewportPoint(_camera, _viewport, new Point(_viewport.ActualWidth * .5d, _viewport.ActualHeight * .5d));

                // Come up with a snap plane
                Vector3D standard = Math3D.GetArbitraryOrhonganal(cameraLookCenter.Direction);
                Vector3D orth = Vector3D.CrossProduct(standard, cameraLookCenter.Direction);
                ITriangle plane = new Triangle(point, point + standard, point + orth);

                DragHitShape dragPlane = new DragHitShape();
                dragPlane.SetShape_Plane(plane);

                // Cast a ray onto that plane from the current mouse position
                RayHitTestParameters mouseRay = UtilityWPF.RayFromViewportPoint(_camera, _viewport, _mousePoint.Value);
                Point3D? hitPoint = dragPlane.CastRay(mouseRay);

                if (hitPoint != null)
                {
                    // Find the nearest Z cell
                    double halfSize = (_field.Size * _sizeMult) / 2d;
                    double cellSize = (_field.Size * _sizeMult) / _field.Size;

                    int zIndex = Convert.ToInt32((halfSize - axisZ.GetValue(hitPoint.Value)) / cellSize);

                    if (zIndex >= 0 && zIndex < _field.Size)
                    {
                        isOverField = true;

                        // Cap to field
                        _plateCurrentIndex = _field.Size - zIndex;        // it's actually the opposite
                        if (_plateCurrentIndex - fromOffset < 0)
                        {
                            _plateCurrentIndex = fromOffset;
                        }
                        else if (_plateCurrentIndex + toOffset > _field.Size - 1)
                        {
                            _plateCurrentIndex = _field.Size - toOffset - 1;
                        }

                        _sceneRemaining = now + TimeSpan.FromSeconds(ELAPSEDURATIONSECONDS);
                    }
                }

                #endregion
            }

            if (!isOverField)
            {
                #region Shift the plate

                if (_plateCurrentIndex + toOffset > _field.Size - 1)
                {
                    _plateCurrentIndex = _field.Size - toOffset - 1;
                    _sceneRemaining = now + TimeSpan.FromSeconds(ELAPSEDURATIONSECONDS);
                }
                else if (now > _sceneRemaining)
                {
                    _plateCurrentIndex--;

                    if (_plateCurrentIndex - fromOffset <= 0)
                    {
                        _plateCurrentIndex = _field.Size - toOffset - 1;
                    }

                    _sceneRemaining = now + TimeSpan.FromSeconds(ELAPSEDURATIONSECONDS);
                }

                #endregion
            }

            double[] velX = _field.VelocityX;
            double[] velY = _field.VelocityY;
            double[] velZ = _field.VelocityZ;

            bool[] blocked = _field.Blocked;

            _velocityLines.BeginAddingLines();

            for (int z = _plateCurrentIndex - fromOffset; z <= _plateCurrentIndex + toOffset; z++)
            {
                for (int x = 0; x < _field.Size; x++)
                {
                    for (int y = 0; y < _field.Size; y++)
                    {
                        int xRef = -1;
                        int yRef = -1;
                        int zRef = -1;

                        axisX.Set3DIndex(ref xRef, ref yRef, ref zRef, x);
                        axisY.Set3DIndex(ref xRef, ref yRef, ref zRef, y);
                        axisZ.Set3DIndex(ref xRef, ref yRef, ref zRef, z);

                        int index1D = _field.Get1DIndex(xRef, yRef, zRef);

                        if (blocked[index1D])
                        {
                            continue;
                        }

                        DrawLinesSprtAddLine(xRef, yRef, zRef, index1D, half, lineThickness, velX, velY, velZ);
                    }
                }
            }

            _velocityLines.EndAddingLines();
        }
        private void DrawLines_RandomInstant(int numSamples, double half, double lineThickness)
        {
            double[] velX = _field.VelocityX;
            double[] velY = _field.VelocityY;
            double[] velZ = _field.VelocityZ;

            bool[] blocked = _field.Blocked;

            int totalSamples = numSamples * numSamples * numSamples;        // numsamples is per axis, so cube it
            int counter = 0;

            _velocityLines.BeginAddingLines();

            foreach (int index1D in UtilityCore.RandomRange(0, _field.Size1D))
            {
                if (blocked[index1D])
                {
                    continue;
                }

                counter++;

                var index3D = _field.Get3DIndex(index1D);

                DrawLinesSprtAddLine(index3D.Item1, index3D.Item2, index3D.Item3, index1D, half, lineThickness, velX, velY, velZ);

                if (counter >= totalSamples - 1)
                {
                    // Enough lines have been drawn
                    break;
                }
            }

            _velocityLines.EndAddingLines();
        }
        private void DrawLines_RandomPersist(int numSamples, double half, double lineThickness)
        {
            const double ELAPSEDURATIONSECONDS = 10;

            if (_randPersistIndices == null || DateTime.UtcNow > _sceneRemaining)
            {
                // Rebuild the indices
                bool[] blocked = _field.Blocked;
                int totalSamples = numSamples * numSamples * numSamples;        // numsamples is per axis, so cube it

                _randPersistIndices = UtilityCore.RandomRange(0, _field.Size1D).
                    Where(o => !blocked[o]).
                    Take(totalSamples).
                    Select(o =>
                        {
                            var index3D = _field.Get3DIndex(o);
                            return new Mapping_3D_1D(index3D.Item1, index3D.Item2, index3D.Item3, o);
                        }).
                        ToArray();

                _sceneRemaining = DateTime.UtcNow + TimeSpan.FromSeconds(ELAPSEDURATIONSECONDS);
            }

            double[] velX = _field.VelocityX;
            double[] velY = _field.VelocityY;
            double[] velZ = _field.VelocityZ;

            _velocityLines.BeginAddingLines();

            foreach (var index in _randPersistIndices)
            {
                DrawLinesSprtAddLine(index.X, index.Y, index.Z, index.Offset1D, half, lineThickness, velX, velY, velZ);
            }

            _velocityLines.EndAddingLines();
        }
        private void DrawLinesSprtAddLine(int x, int y, int z, int index, double half, double lineThickness, double[] velX, double[] velY, double[] velZ)
        {
            Point3D start = new Point3D((x * _sizeMult) - half, (y * _sizeMult) - half, (z * _sizeMult) - half);
            Point3D stop = new Point3D(start.X + (velX[index] * _velocityMult), start.Y + (velY[index] * _velocityMult), start.Z + (velZ[index] * _velocityMult));

            _velocityLines.AddLine(start, stop, lineThickness);
        }

        private static Visual3D GetVisual_Dot(Point3D position, double radius, Color color)
        {
            // Material
            MaterialGroup materials = new MaterialGroup();
            materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
            materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.AlphaBlend(color, Colors.White, .5d)), 50d));

            // Geometry Model
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = materials;
            geometry.BackMaterial = materials;
            geometry.Geometry = UtilityWPF.GetSphere_LatLon(3, radius, radius, radius);

            // Model Visual
            ModelVisual3D retVal = new ModelVisual3D();
            retVal.Content = geometry;
            retVal.Transform = new TranslateTransform3D(position.ToVector());

            return retVal;
        }

        #region OLD

        ////private DiffuseMaterial _materialVelocity;
        ///// <summary>
        ///// Geometries will get added/removed from this, which will be seen from this.Visual
        ///// </summary>
        //private Model3DGroup _geometriesVelocity;
        ///// <summary>
        ///// This is the actual visual of the velocity lines
        ///// </summary>
        //private ModelVisual3D _visualVelocity;
        ///// <summary>
        ///// Lines will get reused frame to frame, unless more or less are needed
        ///// </summary>
        //private List<BillboardLine3D> _linesVelocity = new List<BillboardLine3D>();



        //        _geometriesVelocity = new Model3DGroup();

        //_visualVelocity = new ModelVisual3D();
        //_visualVelocity.Content = _geometriesVelocity;
        //_viewport.Children.Add(_visualVelocity);




        //_geometriesVelocity.Children.Clear();

        //foreach (BillboardLine3D line in _linesVelocity)
        //{
        //    line.Dispose();
        //}
        //_linesVelocity.Clear();






        //private void DrawLinesSprtAddLine(int lineIndex, int x, int y, int z, int index, double half, double lineThickness, double[] velX, double[] velY, double[] velZ)
        //{
        //    bool addedOne = false;

        //    if (lineIndex > _linesVelocity.Count)
        //    {
        //        throw new ApplicationException("Tried to add more than one line at a time");
        //    }
        //    else if (lineIndex == _linesVelocity.Count)
        //    {
        //        // Create a new line
        //        BillboardLine3D line = new BillboardLine3D();
        //        line.Color = Colors.GhostWhite;
        //        line.IsReflectiveColor = true;

        //        _linesVelocity.Add(line);
        //        addedOne = true;
        //    }

        //    // Update the point
        //    Point3D lineStart = new Point3D((x * _sizeMult) - half, (y * _sizeMult) - half, (z * _sizeMult) - half);
        //    Point3D lineStop = new Point3D(lineStart.X + (velX[index] * _velocityMult), lineStart.Y + (velY[index] * _velocityMult), lineStart.Z + (velZ[index] * _velocityMult));

        //    _linesVelocity[lineIndex].SetPoints(lineStart, lineStop, lineThickness);

        //    if (addedOne)
        //    {
        //        _geometriesVelocity.Children.Add(_linesVelocity[lineIndex].Model);      // this must be done last.  Otherwise, when the computer is stressed, the line will get rendered before it is positioned
        //    }
        //}



        //private void RemoveUnusedLines(int lastUsedIndex)
        //{
        //    for (int cntr = _linesVelocity.Count - 1; cntr > lastUsedIndex; cntr--)
        //    {
        //        _geometriesVelocity.Children.RemoveAt(cntr);
        //        _linesVelocity[cntr].Dispose();
        //        _linesVelocity.RemoveAt(cntr);
        //    }
        //}

        #endregion

        #endregion
    }
}
