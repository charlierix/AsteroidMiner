using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Controls3D;
using Game.Newt.v2.Arcanorum.Parts;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Game.Newt.v2.Arcanorum.Views
{
    /// <summary>
    /// This shows a sensor vision's output projected into world coords
    /// </summary>
    public class SensorVisionViewer : IDisposable
    {
        #region Declaration Section

        private readonly SensorVision _sensor;
        private readonly Viewport3D _viewport;
        private readonly Map _map;

        private readonly ShipViewerWindow.ItemColors _neuronColors = new ShipViewerWindow.ItemColors();

        private (INeuron neuron, SolidColorBrush brush)[] _neuronBrushes = null;

        private TranslateTransform3D _translate = new TranslateTransform3D();

        private List<Visual3D> _visuals = new List<Visual3D>();
        private List<Visual3D> _tempVisuals = new List<Visual3D>();

        #endregion

        #region Constructor

        public SensorVisionViewer(SensorVision sensor, Viewport3D viewport, Map map)
        {
            _sensor = sensor;
            _viewport = viewport;
            _map = map;

            CreateVisuals();
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
                    _viewport.Children.RemoveAll(_visuals);
                    _visuals.Clear();

                    _viewport.Children.RemoveAll(_tempVisuals);
                    _tempVisuals.Clear();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                _disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~SensorVisionViewer() {
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

        public bool ShowTempVisuals { get; set; }

        #endregion

        #region Public Methods

        public void Update()
        {
            // All of the models and visuals should be created and added to the viewport in the constructor
            // Update should just update the transforms and change colors

            // Translate
            var worldLoc = _sensor.GetWorldLocation();

            _translate.OffsetX = worldLoc.position.X;
            _translate.OffsetY = worldLoc.position.Y;
            _translate.OffsetZ = worldLoc.position.Z;

            // Neuron Colors
            foreach (var neuron in _neuronBrushes)
            {
                neuron.brush.Color = _neuronColors.GetNeuronColor(neuron.neuron);
            }

            // Temp Visuals
            _viewport.Children.RemoveAll(_tempVisuals);
            _tempVisuals.Clear();

            if (ShowTempVisuals)
            {
                // The points of items that the sensor sees
                Visual3D visual_ItemPoints = GetItemPointsVisual(worldLoc.position, _sensor, _map);
                if (visual_ItemPoints != null)
                {
                    _viewport.Children.Add(visual_ItemPoints);
                    _tempVisuals.Add(visual_ItemPoints);
                }
            }
        }

        private static Visual3D GetItemPointsVisual(Point3D position, SensorVision sensor, Map map)
        {
            var snapshot = map.LatestSnapshot;
            if (snapshot == null)
            {
                return null;
            }

            var items = SensorVision.GetItemPoints(snapshot.GetItems(position, sensor.SearchRadius), sensor.BotToken, sensor.NestToken);
            if (items.Length == 0)
            {
                return null;
            }

            double DOT = sensor.Radius * 2;

            Model3DGroup geometries = new Model3DGroup();

            MaterialGroup material = new MaterialGroup();
            material.Children.Add(new DiffuseMaterial(Brushes.Plum));
            material.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("80C0C0C0")), 50d));

            foreach (Point3D point in items.SelectMany(o => o.points))
            {
                GeometryModel3D geometry = new GeometryModel3D
                {
                    Material = material,
                    BackMaterial = material,
                    Geometry = UtilityWPF.GetSphere_Ico(DOT, 1, true),
                    Transform = new TranslateTransform3D(point.ToVector())
                };

                geometries.Children.Add(geometry);
            }

            return new ModelVisual3D
            {
                Content = geometries,
            };
        }

        #endregion

        #region Private Methods

        private void CreateVisuals()
        {
            double LINE = _sensor.Radius / 4;
            double DOT = _sensor.Radius / 2;

            // Circle
            Visual3D circle = Debug3DWindow.GetCircle(new Point3D(0, 0, 0), _sensor.SearchRadius, LINE, Colors.Plum);
            circle.Transform = _translate;

            _visuals.Add(circle);
            _viewport.Children.Add(circle);

            // Dots
            Point3D[] neuronPositions = _sensor.NeuronWorldPositions;

            (INeuron[] neurons, Point3D position)[] neurons_positions = null;

            var layers = _sensor.NeuronLayers;
            if (layers != null)
            {
                neurons_positions = Enumerable.Range(0, neuronPositions.Length).
                    Select(o =>
                    (
                        layers.
                            Select(p => (INeuron)p.Neurons[o]).
                            ToArray(),
                        neuronPositions[o]
                    )).
                    ToArray();
            }
            else
            {
                INeuron[] neurons = _sensor.Neruons_All.ToArray();

                neurons_positions = Enumerable.Range(0, neurons.Length).
                    Select(o => (new[] { neurons[o] }, neuronPositions[o])).
                    ToArray();
            }

            var dots = BuildNeuronVisuals(neurons_positions, _neuronColors, DOT);
            dots.visual.Transform = _translate;

            _neuronBrushes = dots.brushes;

            _visuals.Add(dots.visual);
            _viewport.Children.Add(dots.visual);
        }

        private static ((INeuron, SolidColorBrush)[] brushes, Visual3D visual) BuildNeuronVisuals((INeuron[] neurons, Point3D position)[] neurons, ShipViewerWindow.ItemColors colors, double dotSize)
        {
            // Copied from ShipViewerWindow

            var outNeurons = new List<(INeuron, SolidColorBrush)>();

            Model3DGroup geometries = new Model3DGroup();

            Vector3D[] offsets = null;
            if (neurons[0].neurons.Length == 1)
            {
                // There's only one neuron per set.  Just place each at neurons.position
                offsets = new[] { new Vector3D() };
            }
            else
            {
                // neurons.position is the position on the plane, but there are multiple neurons that need to be shows at that point.  Put them in a 
                // tight circle around that point (don't want to extend the dots into Z, because the sheet will be looked at straight down)
                offsets = Math2D.GetCircle_Cached(neurons[0].neurons.Length).
                    Select(o => o.ToVector3D() * (dotSize * 1.2)).
                    ToArray();
            }

            foreach (var set in neurons)
            {
                for (int cntr = 0; cntr < set.neurons.Length; cntr++)
                {
                    MaterialGroup material = new MaterialGroup();
                    SolidColorBrush brush = new SolidColorBrush(set.neurons[cntr].IsPositiveOnly ? colors.Neuron_Zero_ZerPos : colors.Neuron_Zero_NegPos);
                    DiffuseMaterial diffuse = new DiffuseMaterial(brush);
                    material.Children.Add(diffuse);
                    material.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("80C0C0C0")), 50d));

                    GeometryModel3D geometry = new GeometryModel3D
                    {
                        Material = material,
                        BackMaterial = material,
                        Geometry = UtilityWPF.GetSphere_Ico(dotSize, 1, true),
                        Transform = new TranslateTransform3D((set.position + offsets[cntr]).ToVector()),
                    };

                    geometries.Children.Add(geometry);

                    outNeurons.Add((set.neurons[cntr], brush));
                }
            }

            ModelVisual3D visual = new ModelVisual3D
            {
                Content = geometries,
            };

            return (outNeurons.ToArray(), visual);
        }

        #endregion
    }
}
