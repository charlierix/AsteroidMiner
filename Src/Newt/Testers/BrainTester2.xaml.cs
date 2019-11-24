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
using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Controls3D;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.MapParts;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.GameItems.ShipParts;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.Testers
{
    public partial class BrainTester2 : Window
    {
        #region class: ItemColors

        private class ItemColors : BrainTester.ItemColors
        {
            //TODO: Vision cone, voronoi line colors
        }

        #endregion
        #region class: ContainerStuff

        private class ContainerStuff
        {
            public EnergyTank Energy = null;
            public Body EnergyBody = null;
            public Visual3D EnergyVisual = null;

            public FuelTank Fuel = null;
            public Body FuelBody = null;
            public Visual3D FuelVisual = null;

            public PlasmaTank Plasma = null;
            public Body PlasmaBody = null;
            public Visual3D PlasmaVisual = null;
        }

        #endregion
        #region class: CameraStuff

        private class CameraStuff
        {
            public CameraHardCoded Camera = null;
            public Body Body = null;
            public Visual3D Visual = null;
            public List<Tuple<INeuron, SolidColorBrush>> Neurons = null;
            public Visual3D NeuronVisual = null;

            //TODO: Show vision cone, voronoi
        }

        #endregion
        #region class: ControllerStuff

        private class ControllerStuff
        {
            public DirectionControllerRing Ring = null;
            public DirectionControllerSphere Sphere = null;

            public Body Body = null;
            public Visual3D Visual = null;
            public List<Tuple<INeuron, SolidColorBrush>> Neurons = null;
            public Visual3D NeuronVisual = null;
        }

        #endregion
        #region class: MotorStuff

        private class MotorStuff
        {
            public ImpulseEngine ImpulseEngine = null;

            public Body Body = null;
            public Visual3D Visual = null;
            public List<Tuple<INeuron, SolidColorBrush>> Neurons = null;
            public Visual3D NeuronVisual = null;
        }

        #endregion
        #region class: MapObjectStuff

        private class MapObjectStuff
        {
            public Mineral[] Food = null;
            public Mineral[] Poison = null;
            public Mineral[] Ice = null;
            public Asteroid[] Asteroids = null;
            public Egg<ShipDNA>[] Eggs = null;
        }

        #endregion

        #region Declaration Section

        private ItemColors _colors = new ItemColors();
        private EditorOptions _editorOptions = new EditorOptions();
        private ItemOptions _itemOptions = new ItemOptions();
        private SharedVisuals _sharedVisuals = new SharedVisuals();

        private Point3D _boundryMin;
        private Point3D _boundryMax;
        //private ScreenSpaceLines3D _boundryLines = null;

        private World _world = null;
        private Map _map = null;

        private MaterialManager _materialManager = null;
        private int _material_Ship = -1;
        private int _material_Mineral = -1;
        private int _material_Asteroid = -1;
        private int _material_Egg = -1;

        // These listen to the mouse/keyboard and controls the camera
        private TrackBallRoam _trackballBot = null;
        private TrackBallRoam _trackballNeural = null;
        private TrackBallRoam _trackballWorld = null;

        private ContainerStuff _containers = null;
        private CameraStuff[] _cameras = null;
        private ControllerStuff[] _controllers = null;
        private MotorStuff[] _motors = null;
        private MapObjectStuff _mapObjects = null;

        private List<Visual3D> _debugVisuals = new List<Visual3D>();

        private DateTime _lastUpdate = DateTime.UtcNow;

        #endregion

        #region Constructor

        public BrainTester2()
        {
            InitializeComponent();
        }

        #endregion

        #region Event Listeners

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _itemOptions.CameraHardCoded_ClassifyObject = ClassifyObject;

                #region Init World

                _boundryMin = new Point3D(-100, -100, -100);
                _boundryMax = new Point3D(100, 100, 100);

                _world = new World();
                _world.Updating += new EventHandler<WorldUpdatingArgs>(World_Updating);

                _world.SetCollisionBoundry(_boundryMin, _boundryMax);

                // Don't bother with the boundry lines.  It looks funny with a partial viewport
                //// Draw the lines
                //_boundryLines = new ScreenSpaceLines3D(true);
                //_boundryLines.Thickness = 1d;
                //_boundryLines.Color = _colors.BoundryLines;
                //_viewport.Children.Add(_boundryLines);

                //foreach (Point3D[] line in innerLines)
                //{
                //    _boundryLines.AddLine(line[0], line[1]);
                //}

                #endregion
                #region Materials

                _materialManager = new MaterialManager(_world);

                // Ship
                var material = new Game.Newt.v2.NewtonDynamics.Material();
                _material_Ship = _materialManager.AddMaterial(material);

                // Mineral
                material = new Game.Newt.v2.NewtonDynamics.Material();
                _material_Mineral = _materialManager.AddMaterial(material);

                // Asteroid
                material = new Game.Newt.v2.NewtonDynamics.Material();
                _material_Asteroid = _materialManager.AddMaterial(material);

                // Egg
                material = new Game.Newt.v2.NewtonDynamics.Material();
                _material_Egg = _materialManager.AddMaterial(material);

                //_materialManager.RegisterCollisionEvent(_material_Terrain, _material_Bean, Collision_BeanTerrain);

                #endregion
                #region Trackball

                // Nerual
                _trackballNeural = new TrackBallRoam(_cameraNeural);
                //_trackballNeural.KeyPanScale = 15d;
                _trackballNeural.EventSource = pnlViewportNeural;		//NOTE:  If this control doesn't have a background color set, the trackball won't see events (I think transparent is ok, just not null)
                _trackballNeural.AllowZoomOnMouseWheel = true;
                _trackballNeural.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.MouseComplete));
                //_trackballNeural.GetOrbitRadius += new GetOrbitRadiusHandler(Trackball_GetOrbitRadius);
                _trackballNeural.ShouldHitTestOnOrbit = false;

                // Bot
                _trackballBot = new TrackBallRoam(_cameraBot);
                //_trackballBot.KeyPanScale = 15d;
                _trackballBot.EventSource = pnlViewportBot;		//NOTE:  If this control doesn't have a background color set, the trackball won't see events (I think transparent is ok, just not null)
                _trackballBot.AllowZoomOnMouseWheel = true;
                _trackballBot.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.MouseComplete));
                //_trackballBot.GetOrbitRadius += new GetOrbitRadiusHandler(Trackball_GetOrbitRadius);
                _trackballBot.ShouldHitTestOnOrbit = false;

                // World
                _trackballWorld = new TrackBallRoam(_cameraWorld);
                //_trackballWorld.KeyPanScale = 15d;
                _trackballWorld.EventSource = pnlViewportWorld;		//NOTE:  If this control doesn't have a background color set, the trackball won't see events (I think transparent is ok, just not null)
                _trackballWorld.AllowZoomOnMouseWheel = true;
                _trackballWorld.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.MouseComplete));
                //_trackballWorld.GetOrbitRadius += new GetOrbitRadiusHandler(Trackball_GetOrbitRadius);
                _trackballWorld.ShouldHitTestOnOrbit = false;

                #endregion
                #region Map

                _map = new Map(_viewportWorld, null, _world);
                _map.SnapshotFequency_Milliseconds = 125;
                _map.SnapshotMaxItemsPerNode = 10;
                _map.ShouldBuildSnapshots = true;
                //_map.ShouldShowSnapshotLines = false;
                //_map.ShouldSnapshotCentersDrift = true;

                #endregion
                #region Fields

                //_radiation = new RadiationField()
                //{
                //    AmbientRadiation = 0d,
                //};

                //_gravityField = new GravityFieldUniform();

                #endregion

                _world.UnPause();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            try
            {
                ClearCameras();
                ClearContainers();

                _map.Dispose();		// this will dispose the physics bodies
                _map = null;

                _world.Pause();
                _world.Dispose();
                _world = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void World_Updating(object sender, WorldUpdatingArgs e)
        {
            DateTime thisUpdate = DateTime.UtcNow;
            double elapsedTime = (thisUpdate - _lastUpdate).TotalSeconds;

            #region refill containers

            if (_containers != null)
            {
                _containers.Energy.QuantityCurrent = _containers.Energy.QuantityMax;
                _containers.Fuel.QuantityCurrent = _containers.Fuel.QuantityMax;
                _containers.Plasma.QuantityCurrent = _containers.Plasma.QuantityMax;
            }

            #endregion

            #region update cameras

            if (_cameras != null)
            {
                foreach (CameraStuff camera in _cameras)
                {
                    camera.Camera.Update_MainThread(elapsedTime);
                    camera.Camera.Update_AnyThread(elapsedTime);
                }
            }

            #endregion
            #region update controllers

            if (_controllers != null)
            {
                foreach (ControllerStuff controller in _controllers)
                {
                    if (controller.Ring != null)
                    {
                        controller.Ring.Update_MainThread(elapsedTime);
                        controller.Ring.Update_AnyThread(elapsedTime);
                    }
                    else if (controller.Sphere != null)
                    {
                        controller.Sphere.Update_MainThread(elapsedTime);
                        controller.Sphere.Update_AnyThread(elapsedTime);
                    }
                    else
                    {
                        throw new ApplicationException("Unknown type of controller");
                    }
                }
            }

            #endregion
            #region update motors

            if (_motors != null)
            {
                foreach (MotorStuff motor in _motors)
                {
                    if (motor.ImpulseEngine != null)
                    {
                        motor.ImpulseEngine.Update_MainThread(elapsedTime);
                        motor.ImpulseEngine.Update_AnyThread(elapsedTime);
                    }
                    else
                    {
                        throw new ApplicationException("Unknown type of motor");
                    }
                }
            }

            #endregion

            #region color neurons

            foreach (var neuron in UtilityCore.Iterate(
                _cameras == null ? null : _cameras.SelectMany(o => o.Neurons),
                _controllers == null ? null : _controllers.SelectMany(o => o.Neurons),
                _motors == null ? null : _motors.SelectMany(o => o.Neurons)
                ))
            {
                if (neuron.Item1.IsPositiveOnly)
                {
                    neuron.Item2.Color = UtilityWPF.AlphaBlend(_colors.Neuron_One_ZerPos, _colors.Neuron_Zero_ZerPos, neuron.Item1.Value);
                }
                else
                {
                    double weight = neuron.Item1.Value;		// need to grab the value locally, because it could be modified from a different thread

                    if (weight < 0)
                    {
                        neuron.Item2.Color = UtilityWPF.AlphaBlend(_colors.Neuron_NegOne_NegPos, _colors.Neuron_Zero_NegPos, Math.Abs(weight));
                    }
                    else
                    {
                        neuron.Item2.Color = UtilityWPF.AlphaBlend(_colors.Neuron_One_NegPos, _colors.Neuron_Zero_NegPos, weight);
                    }
                }
            }

            #endregion

            _lastUpdate = thisUpdate;
        }

        private void Body_RequestWorldLocation(object sender, PartRequestWorldLocationArgs e)
        {
            try
            {
                if (!(sender is PartBase))
                {
                    throw new ApplicationException("Expected sender to be PartBase");
                }

                PartBase senderCast = (PartBase)sender;

                // These parts aren't part of a bot, so model coords is world coords
                e.Orientation = senderCast.Orientation;
                e.Position = senderCast.Position;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DebugCameraWorld1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_cameras == null)
                {
                    MessageBox.Show("Need to set cameras first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Debug3DWindow window = new Debug3DWindow();

                window.AddDot(new Point3D(0, 0, 0), .75, Colors.Yellow);

                foreach (CameraStuff camera in _cameras)
                {
                    #region neuron positions

                    var neurons = camera.Camera.GetNeurons_DEBUG();

                    Transform3DGroup transform = new Transform3DGroup();
                    transform.Children.Add(new TranslateTransform3D(camera.Camera.Position.ToVector()));
                    transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(camera.Camera.Orientation)));

                    Point3D[] points = neurons.
                        Select(o => transform.Transform(o.Item2)).
                        ToArray();

                    #endregion

                    // Show all neurons
                    window.AddDots(points, .5, Colors.White);

                    #region showdelaunay

                    //TODO: Throw out thin triangles
                    Tetrahedron[] tetras = Math3D.GetDelaunay(points, 10);
                    Tuple<int, int>[] uniqueLines = Tetrahedron.GetUniqueLines(tetras);

                    window.AddLines(uniqueLines.Select(o => (points[o.Item1], points[o.Item2])), .05, UtilityWPF.ColorFromHex("D8D8D8"));

                    #endregion

                    #region random point details

                    // Pick a random point
                    int randIndex = StaticRandom.Next(points.Length);

                    window.AddDot(points[randIndex], .75, Colors.Red);

                    // Draw connections to this point
                    Tuple<int, int>[] connectedLines = uniqueLines.
                        Where(o => o.Item1 == randIndex || o.Item2 == randIndex).
                        ToArray();

                    //window.AddLines(connectedLines.Select(o => Tuple.Create(points[o.Item1], points[o.Item2])), .1, Colors.Maroon);

                    var distances = connectedLines.
                        Select(o => new
                        {
                            Index1 = o.Item1,
                            Index2 = o.Item2,
                            Point1 = points[o.Item1],
                            Point2 = points[o.Item2],
                            Length = (points[o.Item2] - points[o.Item1]).Length,
                        }).
                        OrderBy(o => o.Length).
                        ToArray();

                    var stdDev = Math1D.Get_Average_StandardDeviation(distances.Select(o => o.Length));
                    double minDist = distances[0].Length;
                    double maxDist = distances[distances.Length - 1].Length;

                    Color minColor = UtilityWPF.ColorFromHex("4D8E27");
                    Color avgColor = UtilityWPF.ColorFromHex("F5B700");
                    Color maxColor = UtilityWPF.ColorFromHex("E03900");

                    foreach (var line in distances)
                    {
                        Color color;
                        if (line.Length > stdDev.Item1)
                        {
                            color = UtilityWPF.AlphaBlend(maxColor, avgColor, (line.Length - stdDev.Item1) / (maxDist - stdDev.Item1));
                        }
                        else
                        {
                            color = UtilityWPF.AlphaBlend(avgColor, minColor, (line.Length - minDist) / (stdDev.Item1 - minDist));
                        }

                        window.AddLine(line.Point1, line.Point2, .15, color);
                    }

                    window.AddDot(points[randIndex], stdDev.Item1, UtilityWPF.ColorFromHex("10FFFFFF"));
                    window.AddDot(points[randIndex], stdDev.Item1 + stdDev.Item2, UtilityWPF.ColorFromHex("40FFFFFF"));
                    //window.AddDot(points[randIndex], stdDev.Item1 + (stdDev.Item2 * 2), UtilityWPF.ColorFromHex("40FFFFFF"));

                    #endregion
                }

                //TODO: Set camera position -- this should set itself automatically in window load (if they didn't call set camera)

                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void DebugCameraWorld2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_cameras == null)
                {
                    MessageBox.Show("Need to set cameras first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Debug3DWindow window = new Debug3DWindow();

                window.AddDot(new Point3D(0, 0, 0), .75, Colors.Yellow);

                foreach (CameraStuff camera in _cameras)
                {
                    #region neuron positions

                    var neurons = camera.Camera.GetNeurons_DEBUG();

                    Transform3DGroup transform = new Transform3DGroup();
                    transform.Children.Add(new TranslateTransform3D(camera.Camera.Position.ToVector()));
                    transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(camera.Camera.Orientation)));

                    Point3D[] points = neurons.
                        Select(o => transform.Transform(o.Item2)).
                        ToArray();

                    #endregion

                    // Show all neurons
                    window.AddDots(points, .5, Colors.White);

                    // Delaunay
                    //TODO: Throw out thin triangles
                    Tetrahedron[] tetras = Math3D.GetDelaunay(points, 10);
                    Tuple<int, int>[] uniqueLines = Tetrahedron.GetUniqueLines(tetras);

                    for (int cntr = 0; cntr < points.Length; cntr++)
                    {
                        Tuple<int, int>[] connectedLines = uniqueLines.
                            Where(o => o.Item1 == cntr || o.Item2 == cntr).
                            ToArray();

                        var stdDev = Math1D.Get_Average_StandardDeviation(connectedLines.Select(o => (points[o.Item2] - points[o.Item1]).Length));

                        window.AddDot(points[cntr], stdDev.Item1 + stdDev.Item2, UtilityWPF.ColorFromHex("20FFFFFF"));
                        //window.AddDot(points[cntr], stdDev.Item1 + (stdDev.Item2 / 2), UtilityWPF.ColorFromHex("40FFFFFF"));
                        //window.AddDot(points[cntr], stdDev.Item1, UtilityWPF.ColorFromHex("40FFFFFF"));
                    }
                }

                //TODO: Set camera position -- this should set itself automatically in window load (if they didn't call set camera)

                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void DebugCameraWorld3_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_cameras == null)
                {
                    MessageBox.Show("Need to set cameras first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Debug3DWindow window = new Debug3DWindow();

                window.AddDot(new Point3D(0, 0, 0), .75, Colors.Yellow);

                foreach (CameraStuff camera in _cameras)
                {
                    #region neuron positions

                    var neurons = camera.Camera.GetNeurons_DEBUG();

                    Transform3DGroup transform = new Transform3DGroup();
                    transform.Children.Add(new TranslateTransform3D(camera.Camera.Position.ToVector()));
                    transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(camera.Camera.Orientation)));

                    Point3D[] points = neurons.
                        Select(o => transform.Transform(o.Item2)).
                        ToArray();

                    #endregion

                    // Show all neurons
                    window.AddDots(points, .5, Colors.White);

                    //window.AddLines(neurons.SelectMany(o => new[]
                    //{
                    //    Tuple.Create(transform.Transform(o.Item2), transform.Transform(o.Item2) + new Vector3D(o.Item3, 0, 0)),
                    //    Tuple.Create(transform.Transform(o.Item2), transform.Transform(o.Item2) - new Vector3D(o.Item3, 0, 0)),
                    //    Tuple.Create(transform.Transform(o.Item2), transform.Transform(o.Item2) + new Vector3D(0, o.Item3, 0)),
                    //    Tuple.Create(transform.Transform(o.Item2), transform.Transform(o.Item2) - new Vector3D(0, o.Item3, 0)),
                    //    Tuple.Create(transform.Transform(o.Item2), transform.Transform(o.Item2) + new Vector3D(0, 0, o.Item3)),
                    //    Tuple.Create(transform.Transform(o.Item2), transform.Transform(o.Item2) - new Vector3D(0, 0, o.Item3)),
                    //}),
                    //.05, Colors.White);

                    window.AddDots(neurons.Select(o => (transform.Transform(o.Item2), o.Item3, UtilityWPF.ColorFromHex("20FFFFFF"), false, false)));
                }

                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ConeNonLinearDistance_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int numSamples = 18;

                Vector3D[] pointsModel = Math3D.GetRandomVectors_Cone_EvenDist(numSamples, CameraColorRGBDesign.CameraDirection, 60, .2, 1);


                double[] distances = pointsModel.
                    Select(o => o.Length).
                    OrderBy().
                    ToArray();


                double worldMax = 50;
                double pow = 3;

                Func<double, double> projectToWorld_Linear = new Func<double, double>(o => o * worldMax);

                Func<double, double> projectToWorld_NonLinear = new Func<double, double>(o => worldMax * Math.Pow(o, pow));

                Vector3D[] pointsWorld_Linear = pointsModel.
                    Select(o =>
                    {
                        double distanceFromOrigin = o.Length;
                        return o * projectToWorld_Linear(distanceFromOrigin);
                    }).
                    ToArray();

                Vector3D[] pointsWorld_NonLinear = pointsModel.
                    Select(o =>
                    {
                        double distanceFromOrigin = o.Length;
                        return o.ToUnit() * projectToWorld_NonLinear(distanceFromOrigin);
                    }).
                    ToArray();

                //double[] distances2 = pointsWorld_NonLinear.
                //    Select(o => o.Length).
                //    OrderBy().
                //    ToArray();

                #region draw

                Debug3DWindow window = new Debug3DWindow();

                window.AddDot(new Point3D(0, 0, 0), .75, Colors.Yellow);

                window.AddDots(pointsWorld_Linear.Select(o => o.ToPoint()), .5, Colors.Black);
                window.AddDots(pointsWorld_NonLinear.Select(o => o.ToPoint()), .5, Colors.White);

                window.AddLines(Enumerable.Range(0, pointsWorld_Linear.Length).Select(o => (pointsWorld_Linear[o].ToPoint(), pointsWorld_NonLinear[o].ToPoint())), .05, Colors.Plum);

                window.Messages_Bottom.Add(new TextBlock() { Text = pow.ToString() });

                window.Show();

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void PackCube_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Debug3DWindow window = new Debug3DWindow();

                window.AddDot(new Point3D(-.5, -.5, -.5), .015, UtilityWPF.ColorFromHex("A0A0A0"));
                window.AddDot(new Point3D(.5, .5, .5), .015, UtilityWPF.ColorFromHex("A0A0A0"));

                Color lineColor = UtilityWPF.ColorFromHex("606060");
                window.AddLine(new Point3D(-1, -1, -1), new Point3D(1, 1, 1), .003, lineColor);

                Vector3D line = new Vector3D(1, 1, 1);
                Vector3D orth = Vector3D.CrossProduct(line, new Vector3D(-1, 1, -1));
                orth = Vector3D.CrossProduct(line, orth);
                orth = orth.ToUnit() * (line.Length / 2);

                Point3D mid1 = new Point3D(.5, .5, .5);
                Point3D posMid = mid1 + orth;

                Point3D mid2 = new Point3D(-.5, -.5, -.5);
                Point3D negMid = mid2 - orth;

                window.AddLines(new[] { (mid1, posMid), (mid2, negMid) }, .003, lineColor);

                window.AddLines(new[]
                    {
                        (new Point3D(-1,-1,-1), negMid),
                        (negMid, new Point3D(0,0,0)),
                        (new Point3D(0,0,0), posMid),
                        (posMid, new Point3D(1,1,1)),
                    },
                    .004, Colors.White);

                window.AddDots(new[]
                    {
                        new Point3D(-1, -1, -1),
                        negMid,
                        new Point3D(0, 0, 0),
                        posMid,
                        new Point3D(1, 1, 1),
                    },
                    .03, Colors.White);

                var lines = GetCubeLines(2);

                window.AddLines(lines, .002, Colors.Silver);

                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ControllerVisual2D3D_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Debug3DWindow window = new Debug3DWindow();

                var lines = GetCubeLines(1);
                window.AddLines(lines, .002, Colors.Silver);

                bool isFinal = StaticRandom.NextBool();
                //bool isFinal = false;

                bool is3D = StaticRandom.NextBool();
                //bool is3D = true;

                Color ringColor = UtilityWPF.AlphaBlend(WorldColors.Brain_Color, WorldColors.SensorBase_Color, .15);

                #region rings

                //NOTE: These torii are arranged to line up with UtilityWPF.GetIcosidodecahedron();

                Tuple<Vector3D, double>[][] tori = null;

                if (is3D)
                {
                    const double ANGLE1 = 58;       // got to angle1 and 2 by experimentation
                    const double ANGLE2 = 32;
                    const double HALFROTATE = 18;       // the first rotation around Z is 18 because there are 10 points (36 degrees / 2) -- it needs half a rotation to line up

                    tori = new[]
                    {
                        new[] { Tuple.Create(new Vector3D(1, 0, 0), ANGLE1) },
                        new[] { Tuple.Create(new Vector3D(1, 0, 0), -ANGLE1) },

                        new[] { Tuple.Create(new Vector3D(0, 0, 1), HALFROTATE), Tuple.Create(new Vector3D(0, 1, 0), ANGLE2) },
                        new[] { Tuple.Create(new Vector3D(0, 0, 1), HALFROTATE), Tuple.Create(new Vector3D(0, 1, 0), -ANGLE2) },

                        new[] { Tuple.Create(new Vector3D(0, 0, 1), HALFROTATE), Tuple.Create(new Vector3D(1, 0, 0), 90d), Tuple.Create(new Vector3D(0, 0, 1), ANGLE1) },
                        new[] { Tuple.Create(new Vector3D(0, 0, 1), HALFROTATE), Tuple.Create(new Vector3D(1, 0, 0), 90d), Tuple.Create(new Vector3D(0, 0, 1), -ANGLE1) },
                    };
                }
                else
                {
                    tori = new[] { new[] { Tuple.Create(new Vector3D(1, 0, 0), 0d) } };        // no rotation needed.  This is just here to make a ring
                }

                Model3DGroup torusGroup = new Model3DGroup();

                foreach (var torusAngle in tori)
                {
                    GeometryModel3D geometry = new GeometryModel3D();
                    MaterialGroup material = new MaterialGroup();
                    DiffuseMaterial diffuse = new DiffuseMaterial(new SolidColorBrush(ringColor));
                    material.Children.Add(diffuse);

                    geometry.Material = material;
                    geometry.BackMaterial = material;

                    Transform3DGroup transformGroup = new Transform3DGroup();
                    //transformGroup.Children.Add(scaleTransform);
                    foreach (var individualAngle in torusAngle)
                    {
                        transformGroup.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(individualAngle.Item1, individualAngle.Item2)));
                    }

                    const double THICKNESS = .04;       // it's actually half thickness
                    const double RINGRADIUS = .5 - THICKNESS;

                    if (isFinal)
                    {
                        geometry.Geometry = UtilityWPF.GetTorus(10, 4, THICKNESS, RINGRADIUS);       // must be 10 sided to match the icosidodecahedron (not same as the ichthyosaur)
                    }
                    else
                    {
                        geometry.Geometry = UtilityWPF.GetTorus(60, 12, THICKNESS, RINGRADIUS);
                    }
                    geometry.Transform = transformGroup;

                    torusGroup.Children.Add(geometry);
                }

                ModelVisual3D visual2;
                visual2 = new ModelVisual3D();
                visual2.Content = torusGroup;
                window.Visuals3D.Add(visual2);

                #endregion

                #region brain

                double brainScale = .5d;
                ScaleTransform3D scaleTransform = new ScaleTransform3D();
                scaleTransform.ScaleX = brainScale;
                scaleTransform.ScaleY = brainScale;
                scaleTransform.ScaleZ = brainScale;

                ModelVisual3D visual;
                if (!isFinal)
                {
                    visual = new ModelVisual3D();
                    Model3DGroup group = new Model3DGroup();
                    group.Children.AddRange(BrainDesign.CreateInsideVisuals(brainScale * .9, new List<PartDesignBase.MaterialColorProps>(), new List<EmissiveMaterial>(), scaleTransform));
                    visual.Content = group;
                    window.Visuals3D.Add(visual);
                }

                visual = new ModelVisual3D();
                visual.Content = BrainDesign.CreateShellVisual(isFinal, new List<PartDesignBase.MaterialColorProps>(), new List<EmissiveMaterial>(), scaleTransform);
                window.Visuals3D.Add(visual);

                #endregion

                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ControllerVisualTorus_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Debug3DWindow window = new Debug3DWindow()
                {
                    Background = new SolidColorBrush(UtilityWPF.ColorFromHex("999")),
                };

                var lines = GetCubeLines(1);
                window.AddLines(lines, .002, Colors.Silver);

                window.AddLine(new Point3D(0, 0, 0), new Point3D(2, 0, 0), .003, Colors.Red);
                window.AddLine(new Point3D(0, 0, 0), new Point3D(0, 2, 0), .003, Colors.Green);
                window.AddLine(new Point3D(0, 0, 0), new Point3D(0, 0, 2), .003, Colors.Blue);

                window.Messages_Bottom.Add(new TextBlock() { Text = "X", Foreground = Brushes.Red });
                window.Messages_Bottom.Add(new TextBlock() { Text = "Y", Foreground = Brushes.Green });
                window.Messages_Bottom.Add(new TextBlock() { Text = "Z", Foreground = Brushes.Blue });


                bool isFinal = StaticRandom.NextBool();
                //bool isFinal = true;

                Color ringColor = UtilityWPF.AlphaBlend(WorldColors.Brain_Color, WorldColors.SensorBase_Color, .15);

                #region sphere

                double sphereRadius = .66d;

                Icosidodecahedron ico = UtilityWPF.GetIcosidodecahedron(sphereRadius);

                var icoLines = ico.GetUniqueLines().
                    Select(o => (ico.AllPoints[o.Item1], ico.AllPoints[o.Item2]));

                window.AddLines(icoLines, .015, UtilityWPF.AlphaBlend(ringColor, Colors.White, .5));

                #endregion
                #region rings

                //NOTE: These torii are arranged to line up with UtilityWPF.GetIcosidodecahedron();

                Model3DGroup torusGroup = new Model3DGroup();

                double angle1 = 58;
                double angle2 = 32;

                Tuple<Vector3D, double>[][] tori = new[]
                {
                    new[] { Tuple.Create(new Vector3D(1, 0, 0), angle1) },
                    new[] { Tuple.Create(new Vector3D(1, 0, 0), -angle1) },

                    new[] { Tuple.Create(new Vector3D(0, 0, 1), 18d), Tuple.Create(new Vector3D(0, 1, 0), angle2) },        // the first rotation around Z is 18 because there are 10 points (36 degrees / 2) -- it needs half a rotation to line up
                    new[] { Tuple.Create(new Vector3D(0, 0, 1), 18d), Tuple.Create(new Vector3D(0, 1, 0), -angle2) },

                    new[] { Tuple.Create(new Vector3D(0, 0, 1), 18d), Tuple.Create(new Vector3D(1, 0, 0), 90d), Tuple.Create(new Vector3D(0, 0, 1), angle1) },
                    new[] { Tuple.Create(new Vector3D(0, 0, 1), 18d), Tuple.Create(new Vector3D(1, 0, 0), 90d), Tuple.Create(new Vector3D(0, 0, 1), -angle1) },
                };

                foreach (var torusAngle in tori)
                {
                    GeometryModel3D geometry = new GeometryModel3D();
                    MaterialGroup material = new MaterialGroup();
                    DiffuseMaterial diffuse = new DiffuseMaterial(new SolidColorBrush(ringColor));
                    material.Children.Add(diffuse);

                    geometry.Material = material;
                    geometry.BackMaterial = material;

                    Transform3DGroup transformGroup = new Transform3DGroup();
                    //transformGroup.Children.Add(scaleTransform);
                    foreach (var individualAngle in torusAngle)
                    {
                        transformGroup.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(individualAngle.Item1, individualAngle.Item2)));
                    }

                    if (isFinal)
                    {
                        geometry.Geometry = UtilityWPF.GetTorus(10, 4, .015, .5);
                    }
                    else
                    {
                        geometry.Geometry = UtilityWPF.GetTorus(40, 9, .015, .5);
                    }
                    geometry.Transform = transformGroup;

                    torusGroup.Children.Add(geometry);
                }

                ModelVisual3D visual2;
                visual2 = new ModelVisual3D();
                visual2.Content = torusGroup;
                window.Visuals3D.Add(visual2);

                #endregion

                #region brain

                double brainScale = .5d;
                ScaleTransform3D scaleTransform = new ScaleTransform3D();
                scaleTransform.ScaleX = brainScale;
                scaleTransform.ScaleY = brainScale;
                scaleTransform.ScaleZ = brainScale;

                ModelVisual3D visual;
                if (!isFinal)
                {
                    visual = new ModelVisual3D();
                    Model3DGroup group = new Model3DGroup();
                    group.Children.AddRange(BrainDesign.CreateInsideVisuals(brainScale * .9, new List<PartDesignBase.MaterialColorProps>(), new List<EmissiveMaterial>(), scaleTransform));
                    visual.Content = group;
                    window.Visuals3D.Add(visual);
                }

                visual = new ModelVisual3D();
                visual.Content = BrainDesign.CreateShellVisual(isFinal, new List<PartDesignBase.MaterialColorProps>(), new List<EmissiveMaterial>(), scaleTransform);
                window.Visuals3D.Add(visual);

                #endregion

                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ImpulseEngineDesign_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Debug3DWindow window = new Debug3DWindow();

                double OUTERSCALE = 1;
                double SCALE = .7;
                double INNERSCALE = StaticRandom.NextBool() ? .75 : .92;

                const double SIDENORMALHEIGHTMULT = .33d;
                double SIDEDEPTH = (OUTERSCALE - INNERSCALE) * 1.5;

                TriangleIndexed[] sphereTrianglesInner = UtilityWPF.GetIcosahedron(INNERSCALE, 1);
                TriangleIndexed[] sphereTrianglesOuter = UtilityWPF.GetPentakisDodecahedron(OUTERSCALE);

                #region ball

                MaterialGroup material = new MaterialGroup();
                DiffuseMaterial diffuse = new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("AB49F2")));
                material.Children.Add(diffuse);
                material.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("30C94AFF")), 3));
                material.Children.Add(new EmissiveMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("504F2B69"))));

                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = material;
                geometry.BackMaterial = material;
                geometry.Geometry = UtilityWPF.GetMeshFromTriangles(sphereTrianglesInner);
                geometry.Transform = new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRandomRotation()));

                ModelVisual3D visual = new ModelVisual3D();
                visual.Content = geometry;
                window.Visuals3D.Add(visual);

                #endregion
                #region ball exploded

                ITriangle[] expandedTriangles = sphereTrianglesOuter.
                    SelectMany(o =>
                    {
                        Point3D center = o.GetCenterPoint();

                        Triangle outerPlate = new Triangle(
                            center + ((o.Point0 - center) * SCALE),
                            center + ((o.Point1 - center) * SCALE),
                            center + ((o.Point2 - center) * SCALE));

                        #region FAIL
                        //Vector3D offset = Math3D.GetRandomVector_Circular(o.NormalLength * .05);
                        //Quaternion rotation = Math3D.GetRotation(new Vector3D(0, 0, 1), o.Normal);
                        //offset = rotation.GetRotatedVector(offset);

                        //outerPlate = new Triangle(outerPlate.Point0 + offset, outerPlate.Point1, outerPlate.Point2 + offset);
                        #endregion

                        List<ITriangle> retVal = new List<ITriangle>();
                        retVal.Add(outerPlate);
                        retVal.AddRange(GetSides(outerPlate, SIDENORMALHEIGHTMULT, SIDEDEPTH));

                        return retVal.ToArray();
                    }).
                    ToArray();

                material = new MaterialGroup();
                diffuse = new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("2B271D")));
                material.Children.Add(diffuse);
                material.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("885B74AB")), 25));

                geometry = new GeometryModel3D();
                geometry.Material = material;
                geometry.BackMaterial = material;
                geometry.Geometry = UtilityWPF.GetMeshFromTriangles_IndependentFaces(expandedTriangles);

                visual = new ModelVisual3D();
                visual.Content = geometry;
                window.Visuals3D.Add(visual);

                #endregion

                #region random lines attempt - noope

                // Pick a random point on the sphere that is at least X away from other points
                // Travel to X random neighbor points


                // Draw a bezier along that line

                //Random rand = StaticRandom.GetRandomForThread();

                //Point3D[] allPoints = sphereTriangles[0].AllPoints;

                //List<Tuple<int, int>> availableLines = new List<Tuple<int, int>>(TriangleIndexed.GetUniqueLines(sphereTriangles));
                //List<Edge3D> usedLines = new List<Edge3D>();

                //int index = rand.Next(availableLines.Count);
                //usedLines.Add(new Edge3D(availableLines[index].Item1, availableLines[index].Item2, allPoints));
                //availableLines.RemoveAt(index);

                #endregion

                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void SetImpulseNeurons_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_motors == null || !_motors.Any(o => o.ImpulseEngine != null))
                {
                    MessageBox.Show("Need to create an impulse engine first", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ImpulseEngine engine = _motors.
                    Select(o => o.ImpulseEngine).
                    First(o => o != null);

                foreach (INeuron neuron in engine.Neruons_Writeonly)
                {
                    neuron.SetValue(StaticRandom.NextDouble(3));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateCameras_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double size;
                if (!double.TryParse(txtCameraSize.Text, out size))
                {
                    MessageBox.Show("Couldn't parse camera size", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int numCameras;
                if (!int.TryParse(txtCameraCount.Text, out numCameras))
                {
                    MessageBox.Show("Couldn't parse number of cameras", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (numCameras < 1)
                {
                    MessageBox.Show("The number of cameras must be greater than zero", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Wipe Existing
                ClearCameras();

                if (_containers == null)
                {
                    CreateContainers();
                }

                // Build DNA
                ShipPartDNA dna = new ShipPartDNA()
                {
                    PartType = CameraHardCoded.PARTTYPE,
                    Position = new Point3D(-1.5, 0, 0),
                    Orientation = Quaternion.Identity,
                    Scale = new Vector3D(size, size, size),
                };

                ShipPartDNA[] dnas = new ShipPartDNA[numCameras];

                for (int cntr = 0; cntr < numCameras; cntr++)
                {
                    if (numCameras == 1)
                    {
                        dnas[cntr] = dna;
                    }
                    else
                    {
                        ShipPartDNA dnaCopy = UtilityCore.Clone(dna);

                        Vector3D rotateBy = GetPositionAroundCircle(dna.Position.X, dnas.Length, cntr);
                        dnaCopy.Position += rotateBy;

                        //NOTE: Starting at Z instead of Y, because it needs to be rotated out 90 degrees
                        dnaCopy.Orientation = Math3D.GetRotation(new Vector3D(0, 0, 1), rotateBy);

                        dnas[cntr] = dnaCopy;
                    }
                }

                // Build/show cameras
                CreateCameras(dnas);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void CreateDirectionControllers_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double size;
                if (!double.TryParse(txtControllerSize.Text, out size))
                {
                    MessageBox.Show("Couldn't parse controller size", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int numRings;
                if (!int.TryParse(txtDirControllerRingCount.Text, out numRings))
                {
                    MessageBox.Show("Couldn't parse number of ring controllers", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int numSpheres;
                if (!int.TryParse(txtDirControllerSphereCount.Text, out numSpheres))
                {
                    MessageBox.Show("Couldn't parse number of sphere controllers", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Wipe Existing
                ClearControllers();

                if (_containers == null)
                {
                    CreateContainers();
                }

                // Build DNA
                ShipPartDNA dna = new ShipPartDNA()
                {
                    Position = new Point3D(2.1, 0, 0),
                    Orientation = new Quaternion(new Vector3D(0, 1, 0), 90),        // default is along Z.  Make it along X
                    Scale = new Vector3D(size, size, size),
                };

                ShipPartDNA[] dnas = new ShipPartDNA[numRings + numSpheres];

                for (int cntr = 0; cntr < dnas.Length; cntr++)
                {
                    bool isRing = cntr < numRings;

                    if (dnas.Length == 1)
                    {
                        dnas[cntr] = dna;
                    }
                    else
                    {
                        ShipPartDNA dnaCopy = UtilityCore.Clone(dna);

                        dnaCopy.Position += GetPositionAroundCircle(dna.Position.X, dnas.Length, cntr);

                        //dnaCopy.Orientation = Math3D.GetRotation(new Vector3D(0, 0, 1), rotated);     // no need to rotate the controller, just ring array the position

                        dnas[cntr] = dnaCopy;
                    }

                    if (isRing)
                    {
                        dnas[cntr].PartType = DirectionControllerRing.PARTTYPE;
                    }
                    else
                    {
                        dnas[cntr].PartType = DirectionControllerSphere.PARTTYPE;
                    }

                    if (chkDirControllerRandomOrientation.IsChecked.Value)
                    {
                        dnas[cntr].Orientation = Math3D.GetRandomRotation();
                    }
                }

                // Build/show controllers
                CreateControllers(dnas);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void CreateMotor_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!double.TryParse(txtMotorSize.Text, out double size))
                {
                    MessageBox.Show("Couldn't parse motor size", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(txtImpulseCount.Text, out int impulseCount))
                {
                    MessageBox.Show("Couldn't parse number of impulse engines", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Wipe Existing
                ClearMotors();

                if (_containers == null)
                {
                    CreateContainers();
                }

                // Build DNA
                ImpulseEngineDNA dna = new ImpulseEngineDNA()
                {
                    PartType = ImpulseEngine.PARTTYPE,
                    Position = new Point3D(3, 0, 0),
                    Orientation = Quaternion.Identity,
                    Scale = new Vector3D(size, size, size),
                    ImpulseEngineType = ImpulseEngineType.Both,
                };

                ImpulseEngineDNA[] dnas = new ImpulseEngineDNA[impulseCount];

                for (int cntr = 0; cntr < impulseCount; cntr++)
                {
                    if (impulseCount == 1)
                    {
                        dnas[cntr] = dna;
                    }
                    else
                    {
                        ImpulseEngineDNA dnaCopy = UtilityCore.Clone(dna);

                        dnaCopy.Position += GetPositionAroundCircle(dna.Position.X, dnas.Length, cntr);

                        //dnaCopy.Orientation = Math3D.GetRotation(new Vector3D(0, 0, 1), rotated);     // no need to rotate the motor, just ring array the position

                        dnas[cntr] = dnaCopy;
                    }

                    if (chkMotorRandomOrientation.IsChecked.Value)
                    {
                        dnas[cntr].Orientation = Math3D.GetRandomRotation();
                    }
                }

                // Build/show motors
                CreateMotors(dnas);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void CreateMapObjects_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int foodCount;
                if (!int.TryParse(txtFoodCount.Text, out foodCount))
                {
                    MessageBox.Show("Couldn't parse number of food", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int poisonCount;
                if (!int.TryParse(txtPoisonCount.Text, out poisonCount))
                {
                    MessageBox.Show("Couldn't parse number of poison", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int iceCount;
                if (!int.TryParse(txtIceCount.Text, out iceCount))
                {
                    MessageBox.Show("Couldn't parse number of ice", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int asteroidCount;
                if (!int.TryParse(txtAsteroidCount.Text, out asteroidCount))
                {
                    MessageBox.Show("Couldn't parse number of asteroids", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int eggCount;
                if (!int.TryParse(txtEggCount.Text, out eggCount))
                {
                    MessageBox.Show("Couldn't parse number of eggs", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Wipe Existing
                ClearMapObjects();

                #region food / poison / ice

                var addMineral = new Action<int, MineralType, List<Mineral>>((count, type, list) =>
                {
                    for (int cntr = 0; cntr < count; cntr++)
                    {
                        Point3D position = Math3D.GetRandomVector(_boundryMin.ToVector(), _boundryMax.ToVector()).ToPoint();

                        Mineral mineral = new Mineral(type, position, .15d, _world, _material_Mineral, _sharedVisuals);

                        mineral.PhysicsBody.AngularVelocity = Math3D.GetRandomVector_Spherical(1d);
                        mineral.PhysicsBody.Velocity = Math3D.GetRandomVector_Spherical(4d);

                        // Uncomment this if you want to constrain them to a tighter space, or cause them to change velocities
                        //mineral.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(MapObject_ApplyForceAndTorque);

                        list.Add(mineral);
                        _map.AddItem(mineral);
                    }
                });

                List<Mineral> food = new List<Mineral>();
                addMineral(foodCount, MineralType.Emerald, food);

                List<Mineral> poison = new List<Mineral>();
                addMineral(poisonCount, MineralType.Ruby, poison);

                List<Mineral> ice = new List<Mineral>();
                addMineral(iceCount, MineralType.Ice, ice);

                #endregion
                #region asteroids

                List<Asteroid> asteroids = new List<Asteroid>();

                for (int cntr = 0; cntr < asteroidCount; cntr++)
                {
                    Point3D position = Math3D.GetRandomVector(_boundryMin.ToVector(), _boundryMax.ToVector()).ToPoint();

                    Asteroid asteroid = new Asteroid(1, (r, t) => 10, position, _world, _map, _material_Asteroid);

                    asteroid.PhysicsBody.AngularVelocity = Math3D.GetRandomVector_Spherical(1d);
                    asteroid.PhysicsBody.Velocity = Math3D.GetRandomVector_Spherical(4d);

                    // Uncomment this if you want to constrain them to a tighter space, or cause them to change velocities
                    //asteroid.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(MapObject_ApplyForceAndTorque);

                    asteroids.Add(asteroid);
                    _map.AddItem(asteroid);
                }

                #endregion
                #region eggs

                List<Egg<ShipDNA>> eggs = new List<Egg<ShipDNA>>();

                for (int cntr = 0; cntr < eggCount; cntr++)
                {
                    Point3D position = Math3D.GetRandomVector(_boundryMin.ToVector(), _boundryMax.ToVector()).ToPoint();

                    Egg<ShipDNA> egg = new Egg<ShipDNA>(position, .5, _world, _material_Egg, _itemOptions, null);

                    egg.PhysicsBody.AngularVelocity = Math3D.GetRandomVector_Spherical(1d);
                    egg.PhysicsBody.Velocity = Math3D.GetRandomVector_Spherical(4d);

                    // Uncomment this if you want to constrain them to a tighter space, or cause them to change velocities
                    //egg.PhysicsBody.ApplyForceAndTorque += new EventHandler<BodyApplyForceAndTorqueArgs>(MapObject_ApplyForceAndTorque);

                    eggs.Add(egg);
                    _map.AddItem(egg);
                }

                #endregion

                _mapObjects = new MapObjectStuff()
                {
                    Food = food.ToArray(),
                    Poison = poison.ToArray(),
                    Ice = ice.ToArray(),
                    Asteroids = asteroids.ToArray(),
                    Eggs = eggs.ToArray(),
                };

                UpdateCountReport();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearCameras();
                ClearControllers();
                ClearMotors();
                ClearContainers();
                ClearMapObjects();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private void ClearCameras()
        {
            ClearDebugVisuals();
            ClearLinks();

            if (_cameras != null)
            {
                foreach (CameraStuff camera in _cameras)
                {
                    _viewportBot.Children.Remove(camera.Visual);

                    _viewportNeural.Children.Remove(camera.NeuronVisual);

                    //TODO: vision cone lines
                    //_viewportNeural.Children.Remove(camera.Voronoi);

                    camera.Body.Dispose();
                }

                _cameras = null;
            }
        }
        private void ClearContainers()
        {
            ClearDebugVisuals();
            //ClearLinks();

            if (_containers != null)
            {
                _viewportBot.Children.Remove(_containers.EnergyVisual);
                _viewportBot.Children.Remove(_containers.FuelVisual);
                _viewportBot.Children.Remove(_containers.PlasmaVisual);

                _containers.EnergyBody.Dispose();
                _containers.FuelBody.Dispose();
                _containers.PlasmaBody.Dispose();

                _containers = null;
            }
        }
        private void ClearLinks()
        {
            ClearDebugVisuals();
            //CancelBrainOperation();

            //if (_links != null)
            //{
            //    foreach (Visual3D visual in _links.Visuals)
            //    {
            //        _viewportNeural.Children.Remove(visual);
            //    }

            //    _links = null;
            //}

            //// Reset the neuron values (leave the sensors alone, because they aren't affected by links)
            //foreach (var neuron in UtilityCore.Iterate(
            //    _brains == null ? null : _brains.SelectMany(o => o.Neurons),
            //    _thrusters == null ? null : _thrusters.SelectMany(o => o.Neurons)))
            //{
            //    neuron.Item1.SetValue(0d);
            //}
        }
        private void ClearControllers()
        {
            ClearDebugVisuals();
            ClearLinks();

            if (_controllers != null)
            {
                foreach (ControllerStuff controller in _controllers)
                {
                    _viewportBot.Children.Remove(controller.Visual);

                    _viewportNeural.Children.Remove(controller.NeuronVisual);

                    controller.Body.Dispose();
                }

                _controllers = null;
            }
        }
        private void ClearMotors()
        {
            ClearDebugVisuals();
            ClearLinks();

            if (_motors != null)
            {
                foreach (MotorStuff motor in _motors)
                {
                    _viewportBot.Children.Remove(motor.Visual);

                    _viewportNeural.Children.Remove(motor.NeuronVisual);

                    motor.Body.Dispose();
                }

                _motors = null;
            }
        }
        private void ClearMapObjects()
        {
            ClearDebugVisuals();

            if (_mapObjects != null)
            {
                IEnumerable<IMapObject> all = UtilityCore.Iterate<IMapObject>(
                    _mapObjects.Food,
                    _mapObjects.Poison,
                    _mapObjects.Ice,
                    _mapObjects.Asteroids,
                    _mapObjects.Eggs);

                foreach (IMapObject mapObject in all)
                {
                    _map.RemoveItem(mapObject);
                }

                _mapObjects = null;
            }
        }
        private void ClearDebugVisuals()
        {
            _viewportNeural.Children.RemoveAll(_debugVisuals);
            _debugVisuals.Clear();
        }

        private void CreateContainers()
        {
            const double HEIGHT = .07d;
            const double OFFSET = 1.5d;

            double offset2 = ((.1d - HEIGHT) / 2d) + HEIGHT;

            ShipPartDNA dnaEnergy = new ShipPartDNA()
            {
                PartType = EnergyTank.PARTTYPE,
                Position = new Point3D(OFFSET - offset2, 0, 0),
                Orientation = new Quaternion(new Vector3D(0, 1, 0), 90),
                Scale = new Vector3D(1.3, 1.3, HEIGHT),     // the energy tank is slightly wider than the fuel tank
            };

            ShipPartDNA dnaFuel = new ShipPartDNA()
            {
                PartType = FuelTank.PARTTYPE,
                Position = new Point3D(OFFSET, 0, 0),
                Orientation = new Quaternion(new Vector3D(0, 1, 0), 90),
                Scale = new Vector3D(1.5, 1.5, HEIGHT),
            };

            ShipPartDNA dnaPlasma = new ShipPartDNA()
            {
                PartType = PlasmaTank.PARTTYPE,
                Position = new Point3D(OFFSET + offset2, 0, 0),
                Orientation = new Quaternion(new Vector3D(0, 1, 0), 90),
                Scale = new Vector3D(1.6, 1.6, HEIGHT),
            };

            CreateContainers(dnaEnergy, dnaFuel, dnaPlasma);
        }
        private void CreateContainers(ShipPartDNA energyDNA, ShipPartDNA fuelDNA, ShipPartDNA plasmaDNA)
        {
            if (_containers != null)
            {
                throw new InvalidOperationException("Existing containers should have been wiped out before calling CreateContainers");
            }

            _containers = new ContainerStuff();

            // Energy
            _containers.Energy = new EnergyTank(_editorOptions, _itemOptions, energyDNA);
            _containers.Energy.QuantityCurrent = _containers.Energy.QuantityMax;

            // Fuel
            _containers.Fuel = new FuelTank(_editorOptions, _itemOptions, fuelDNA);
            _containers.Fuel.QuantityCurrent = _containers.Fuel.QuantityMax;

            // Plasma
            _containers.Plasma = new PlasmaTank(_editorOptions, _itemOptions, plasmaDNA);
            _containers.Plasma.QuantityCurrent = _containers.Fuel.QuantityMax;

            #region ship visuals (energy)

            // WPF
            ModelVisual3D model = new ModelVisual3D();
            model.Content = _containers.Energy.Model;

            _viewportBot.Children.Add(model);
            _containers.EnergyVisual = model;

            // Physics
            CollisionHull hull = _containers.Energy.CreateCollisionHull(_world);
            _containers.EnergyBody = new Body(hull, Matrix3D.Identity, _containers.Energy.TotalMass, new Visual3D[] { model });
            hull.Dispose();
            _containers.EnergyBody.MaterialGroupID = _material_Ship;
            _containers.EnergyBody.LinearDamping = .01f;
            _containers.EnergyBody.AngularDamping = new Vector3D(.01f, .01f, .01f);

            #endregion
            #region ship visuals (fuel)

            // WPF
            model = new ModelVisual3D();
            model.Content = _containers.Fuel.Model;

            _viewportBot.Children.Add(model);
            _containers.FuelVisual = model;

            // Physics
            hull = _containers.Fuel.CreateCollisionHull(_world);
            _containers.FuelBody = new Body(hull, Matrix3D.Identity, _containers.Fuel.TotalMass, new Visual3D[] { model });
            hull.Dispose();
            _containers.FuelBody.MaterialGroupID = _material_Ship;
            _containers.FuelBody.LinearDamping = .01f;
            _containers.FuelBody.AngularDamping = new Vector3D(.01f, .01f, .01f);

            #endregion
            #region ship visuals (plama)

            // WPF
            model = new ModelVisual3D();
            model.Content = _containers.Plasma.Model;

            _viewportBot.Children.Add(model);
            _containers.PlasmaVisual = model;

            // Physics
            hull = _containers.Plasma.CreateCollisionHull(_world);
            _containers.PlasmaBody = new Body(hull, Matrix3D.Identity, _containers.Plasma.TotalMass, new Visual3D[] { model });
            hull.Dispose();
            _containers.PlasmaBody.MaterialGroupID = _material_Ship;
            _containers.PlasmaBody.LinearDamping = .01f;
            _containers.PlasmaBody.AngularDamping = new Vector3D(.01f, .01f, .01f);

            #endregion
        }
        private void CreateCameras(ShipPartDNA[] dna)
        {
            if (_cameras != null)
            {
                throw new InvalidOperationException("Existing cameras should have been wiped out before calling CreateCameras");
            }

            CameraStuff[] cameras = new CameraStuff[dna.Length];
            for (int cntr = 0; cntr < dna.Length; cntr++)
            {
                cameras[cntr] = new CameraStuff();
                cameras[cntr].Camera = new CameraHardCoded(_editorOptions, _itemOptions, dna[cntr], _containers == null ? null : _containers.Energy, _map);
                cameras[cntr].Camera.RequestWorldLocation += new EventHandler<PartRequestWorldLocationArgs>(Body_RequestWorldLocation);

                #region Ship Visual

                // WPF
                ModelVisual3D visual = new ModelVisual3D();
                visual.Content = cameras[cntr].Camera.Model;

                _viewportBot.Children.Add(visual);
                cameras[cntr].Visual = visual;

                // Physics
                using (CollisionHull hull = cameras[cntr].Camera.CreateCollisionHull(_world))
                {
                    cameras[cntr].Body = new Body(hull, Matrix3D.Identity, cameras[cntr].Camera.TotalMass, new Visual3D[] { visual })
                    {
                        MaterialGroupID = _material_Ship,
                        LinearDamping = .01f,
                        AngularDamping = new Vector3D(.01f, .01f, .01f)
                    };
                }

                #endregion
                #region Debug Visuals

                //_gravSensors[cntr].Gravity = new ScreenSpaceLines3D();
                //_gravSensors[cntr].Gravity.Thickness = 2d;
                //_gravSensors[cntr].Gravity.Color = _colors.TrackballAxisMajorColor;

                //_viewportNeural.Children.Add(_gravSensors[cntr].Gravity);

                #endregion
                #region Neuron Visuals

                //NOTE: Since the neurons are semitransparent, they need to be added last

                BrainTester.BuildNeuronVisuals(out cameras[cntr].Neurons, out visual, cameras[cntr].Camera.Neruons_All, cameras[cntr].Camera, _colors);

                cameras[cntr].NeuronVisual = visual;
                _viewportNeural.Children.Add(visual);

                #endregion
            }

            _cameras = cameras;

            UpdateCountReport();
        }
        private void CreateControllers(ShipPartDNA[] dna)
        {
            if (_controllers != null)
            {
                throw new InvalidOperationException("Existing controllers should have been wiped out before calling CreateControllers");
            }

            ControllerStuff[] controllers = new ControllerStuff[dna.Length];
            for (int cntr = 0; cntr < dna.Length; cntr++)
            {
                bool isRing;
                switch (dna[cntr].PartType)
                {
                    case DirectionControllerRing.PARTTYPE:
                        isRing = true;
                        break;

                    case DirectionControllerSphere.PARTTYPE:
                        isRing = false;
                        break;

                    default:
                        throw new ApplicationException("Unexpected PartType: " + dna[cntr].PartType);
                }

                controllers[cntr] = new ControllerStuff();

                PartBase part;
                INeuronContainer neuralPart;
                if (isRing)
                {
                    controllers[cntr].Ring = new DirectionControllerRing(_editorOptions, _itemOptions, dna[cntr], _containers == null ? null : _containers.Energy, null, null);     // motors could be created later, and this tester isn't concerned with that level of functionality
                    part = controllers[cntr].Ring;
                    neuralPart = controllers[cntr].Ring;
                }
                else
                {
                    controllers[cntr].Sphere = new DirectionControllerSphere(_editorOptions, _itemOptions, dna[cntr], _containers == null ? null : _containers.Energy, null, null);
                    part = controllers[cntr].Sphere;
                    neuralPart = controllers[cntr].Sphere;
                }

                part.RequestWorldLocation += new EventHandler<PartRequestWorldLocationArgs>(Body_RequestWorldLocation);

                #region Ship Visual

                // WPF
                ModelVisual3D visual = new ModelVisual3D();
                visual.Content = part.Model;

                _viewportBot.Children.Add(visual);
                controllers[cntr].Visual = visual;

                // Physics
                using (CollisionHull hull = part.CreateCollisionHull(_world))
                {
                    controllers[cntr].Body = new Body(hull, Matrix3D.Identity, part.TotalMass, new Visual3D[] { visual })
                    {
                        MaterialGroupID = _material_Ship,
                        LinearDamping = .01f,
                        AngularDamping = new Vector3D(.01f, .01f, .01f)
                    };
                }

                #endregion
                #region Debug Visuals

                //_gravSensors[cntr].Gravity = new ScreenSpaceLines3D();
                //_gravSensors[cntr].Gravity.Thickness = 2d;
                //_gravSensors[cntr].Gravity.Color = _colors.TrackballAxisMajorColor;

                //_viewportNeural.Children.Add(_gravSensors[cntr].Gravity);

                #endregion
                #region Neuron Visuals

                //NOTE: Since the neurons are semitransparent, they need to be added last

                BrainTester.BuildNeuronVisuals(out controllers[cntr].Neurons, out visual, neuralPart.Neruons_All, neuralPart, _colors);

                controllers[cntr].NeuronVisual = visual;
                _viewportNeural.Children.Add(visual);

                #endregion
            }

            _controllers = controllers;

            UpdateCountReport();
        }
        private void CreateMotors(ImpulseEngineDNA[] dna)
        {
            if (_motors != null)
            {
                throw new InvalidOperationException("Existing motors should have been wiped out before calling CreateMotors");
            }

            MotorStuff[] motors = new MotorStuff[dna.Length];
            for (int cntr = 0; cntr < dna.Length; cntr++)
            {
                motors[cntr] = new MotorStuff();
                motors[cntr].ImpulseEngine = new ImpulseEngine(_editorOptions, _itemOptions, dna[cntr], _containers?.Plasma);
                motors[cntr].ImpulseEngine.RequestWorldLocation += new EventHandler<PartRequestWorldLocationArgs>(Body_RequestWorldLocation);

                #region ship visual

                // WPF
                ModelVisual3D visual = new ModelVisual3D();
                visual.Content = motors[cntr].ImpulseEngine.Model;

                _viewportBot.Children.Add(visual);
                motors[cntr].Visual = visual;

                // Physics
                using (CollisionHull hull = motors[cntr].ImpulseEngine.CreateCollisionHull(_world))
                {
                    motors[cntr].Body = new Body(hull, Matrix3D.Identity, motors[cntr].ImpulseEngine.TotalMass, new Visual3D[] { visual })
                    {
                        MaterialGroupID = _material_Ship,
                        LinearDamping = .01f,
                        AngularDamping = new Vector3D(.01f, .01f, .01f)
                    };
                }

                #endregion
                #region debug visuals

                //_gravSensors[cntr].Gravity = new ScreenSpaceLines3D();
                //_gravSensors[cntr].Gravity.Thickness = 2d;
                //_gravSensors[cntr].Gravity.Color = _colors.TrackballAxisMajorColor;

                //_viewportNeural.Children.Add(_gravSensors[cntr].Gravity);

                #endregion
                #region neuron visuals

                //NOTE: Since the neurons are semitransparent, they need to be added last

                BrainTester.BuildNeuronVisuals(out motors[cntr].Neurons, out visual, motors[cntr].ImpulseEngine.Neruons_All, motors[cntr].ImpulseEngine, _colors);

                motors[cntr].NeuronVisual = visual;
                _viewportNeural.Children.Add(visual);

                #endregion
            }

            _motors = motors;

            UpdateCountReport();
        }

        private void UpdateCountReport()
        {
            //TODO: May want to give counts

            //int numInputs = 0;
            //if (_gravSensors != null)
            //{
            //    numInputs = _gravSensors.Sum(o => o.Neurons.Count);
            //}

            //int numBrains = 0;
            //if (_brains != null)
            //{
            //    numBrains = _brains.Sum(o => o.Neurons.Count);
            //}

            //int numManipulators = 0;
            //if (_thrusters != null)
            //{
            //    numManipulators = _thrusters.Sum(o => o.Neurons.Count);
            //}

            //int numLinks = 0;
            //if (_links != null)
            //{
            //    numLinks = _links.Outputs.Sum(o => (o.ExternalLinks == null ? 0 : o.ExternalLinks.Length) + (o.InternalLinks == null ? 0 : o.InternalLinks.Length));
            //}

            //lblInputNeuronCount.Text = numInputs.ToString("N0");
            //lblBrainNeuronCount.Text = numBrains.ToString("N0");
            //lblThrustNeuronCount.Text = numManipulators.ToString("N0");
            //lblTotalNeuronCount.Text = (numInputs + numBrains + numManipulators).ToString("N0");
            //lblTotalLinkCount.Text = numLinks.ToString("N0");
        }

        private static Vector3D GetPositionAroundCircle(double x, int itemCount, int index)
        {
            double angle = 360d / Convert.ToDouble(itemCount);

            return new Vector3D(0, .75, 0).
                GetRotatedVector(new Vector3D(1, 0, 0), angle * index);
        }

        /// <summary>
        /// See CameraHardCoded.ClassifyObject for array meanings
        /// </summary>
        private static double[] ClassifyObject(MapObjectInfo mapObject)
        {
            if (mapObject == null || mapObject.IsDisposed)
            {
                return new double[] { 0d, 0d, 0d, 0d, 0d };     // nothing there
            }

            Mineral mineral = mapObject.MapObject as Mineral;
            if (mineral != null)
            {
                switch (mineral.MineralType)
                {
                    case MineralType.Emerald:
                        return new double[] { 1d, 0d, 0d, 0d, 0d };     // food, go touch it

                    case MineralType.Ruby:
                        return new double[] { 0d, 0d, 0d, 1d, 0d };     // poison, don't touch it

                    default:        // not doing anything special with ice.  It's just a way to test the neutral response
                        return new double[] { 0d, 0d, 1d, 0d, 0d };     // something there, but it's neutral
                }
            }

            Asteroid asteroid = mapObject.MapObject as Asteroid;
            if (asteroid != null)
            {
                return new double[] { 0d, 0d, 0d, 0d, 1d };     // attack it
            }

            Egg<ShipDNA> egg = mapObject.MapObject as Egg<ShipDNA>;
            if (egg != null)
            {
                return new double[] { 0d, 1d, 0d, 0d, 0d };     // stay near it
            }

            return new double[] { 0d, 0d, 1d, 0d, 0d };     // something there, but it's neutral

        }

        private static IEnumerable<(Point3D, Point3D)> GetCubeLines(double sideLength)
        {
            double half = sideLength / 2;

            double[][] combos = new[]
            {
                    new[] { -half, -half },
                    new[] { -half, half },
                    new[] { half, -half },
                    new[] { half, half },
                };

            return Enumerable.Range(0, 3).
                SelectMany(o => combos.Select(p =>
                {
                    List<double> from = new List<double>(p);
                    List<double> to = new List<double>(p);
                    from.Insert(o, -half);
                    to.Insert(o, half);

                    return (new VectorND(from.ToArray()).ToPoint3D(), new VectorND(to.ToArray()).ToPoint3D());
                }));
        }

        private static IEnumerable<ITriangle> GetSides(ITriangle triangle, double normalHeight, double sideDepth)
        {
            Point3D tip = triangle.GetCenterPoint() + (triangle.NormalUnit * normalHeight);

            Point3D[] extended = triangle.PointArray.
                Select(o => o + ((o - tip).ToUnit() * sideDepth)).
                ToArray();

            Point3D[] allPoints = triangle.
                PointArray.
                Concat(extended).
                ToArray();

            List<TriangleIndexed> retVal = new List<TriangleIndexed>();

            // 0-1
            retVal.Add(new TriangleIndexed(0, 1, 3, allPoints));
            retVal.Add(new TriangleIndexed(1, 4, 3, allPoints));

            // 1-2
            retVal.Add(new TriangleIndexed(1, 2, 4, allPoints));
            retVal.Add(new TriangleIndexed(2, 5, 4, allPoints));

            // 2-0
            retVal.Add(new TriangleIndexed(2, 0, 5, allPoints));
            retVal.Add(new TriangleIndexed(0, 3, 5, allPoints));

            return retVal;
        }

        #endregion
    }
}
