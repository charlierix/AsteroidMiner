using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
using Game.Newt.v2.GameItems.ShipEditor;
using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Primitives3D;
using Game.Newt.v2.NewtonDynamics;
using Game.Newt.v2.GameItems.ShipParts;

namespace Game.Newt.Testers
{
    //TODO: Use this tester as an inspiration for a ship viewer popup control.  Instead of having both the neural and ship
    //views on the same panel, make some kind carousel control.  Or make the popup itelf appear to be a 3D plate, and
    //show different aspects of the ship on each side

    //TODO: When they mouse over a neuron, use an emissive on it, and its inputs and what feeds (different colors for the
    //different directions.  Repeat this 3+ times, reducing the intensity at each step

    //TODO: Do something similar when over a brain chemical receptor

    //TODO: When they mouse over a neuron, show a pulse travel from it through the network

    //TODO: Superimpose a number in the brain chemical neurons

    public partial class BrainTester : Window
    {
        #region Class: ItemColors

        private class ItemColors
        {
            public Color BoundryLines = UtilityWPF.ColorFromHex("C4BDAB");

            public Color TrackballAxisMajorColor = UtilityWPF.ColorFromHex("4C638C");
            public DiffuseMaterial TrackballAxisMajor = new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("4C638C")));
            public Color TrackballAxisLine = Color.FromArgb(96, 117, 108, 97);
            public SpecularMaterial TrackballAxisSpecular = new SpecularMaterial(Brushes.White, 100d);
            public Color TrackballGrabberHoverLight = UtilityWPF.ColorFromHex("3F382A");

            public Color Neuron_Zero_NegPos = UtilityWPF.ColorFromHex("20808080");
            public Color Neuron_Zero_ZerPos = UtilityWPF.ColorFromHex("205E88D1");

            public Color Neuron_One_ZerPos = UtilityWPF.ColorFromHex("326CD1");

            public Color Neuron_NegOne_NegPos = UtilityWPF.ColorFromHex("D63633");
            public Color Neuron_One_NegPos = UtilityWPF.ColorFromHex("25994A");

            public Color Link_Negative = UtilityWPF.ColorFromHex("FC0300");
            public Color Link_Positive = UtilityWPF.ColorFromHex("00B237");
        }

        #endregion
        #region Class: LinkTestUtil

        private class LinkTestUtil
        {
            #region Class: LinkResult1

            public class LinkResult1
            {
                public LinkResult1(int from, int to, double weight)
                {
                    this.From = from;
                    this.To = to;
                    this.Weight = weight;
                }

                public readonly int From;
                public readonly int To;
                public readonly double Weight;
            }

            #endregion
            #region Class: LinkResult2

            public class LinkResult2
            {
                public LinkResult2(bool isExactMatch, int index, double percent)
                {
                    this.IsExactMatch = isExactMatch;
                    this.Index = index;
                    this.Percent = percent;
                }

                public readonly bool IsExactMatch;
                public readonly int Index;
                public readonly double Percent;
            }

            #endregion
            #region Class: LinkResult3

            public class LinkResult3
            {
                public LinkResult3(LinkResult2 from, LinkResult2 to, double percent)
                {
                    this.From = from;
                    this.To = to;
                    this.Percent = percent;
                }

                public readonly LinkResult2 From;
                public readonly LinkResult2 To;
                public readonly double Percent;
            }

            #endregion

            /// <summary>
            /// This is a case where the dna says to make a link between from and to, but there is no neuron at to, instead there are neurons
            /// at toNew.  This needs to do some fuzzy linking
            /// </summary>
            /// <remarks>
            /// TODO: This approach will create extra, and never remove (but there could be an independent sweep that gets rid of very small links based on how many this creates?)
            /// </remarks>
            public static LinkResult1[] Test1(Point3D from, Point3D to, Point3D[] toNew, double weight)
            {
                const double SEARCHRADIUSMULT = 2.5d;		// looks at other nodes that are up to minradius * mult
                const int MAXDIVISIONS = 3;

                // Check for exact match
                int index = FindExact(to, toNew);
                if (index >= 0)
                {
                    return new LinkResult1[] { new LinkResult1(0, index, weight) };
                }

                // Get a list of nodes that are close to the search point
                var nearNodes = GetNearNodes(to, toNew, SEARCHRADIUSMULT);

                if (nearNodes.Count == 1)
                {
                    // There's only one, so give it the full weight
                    return new LinkResult1[] { new LinkResult1(0, nearNodes[0].Item1, weight) };
                }

                // Don't allow too many divisions
                if (nearNodes.Count > MAXDIVISIONS)
                {
                    nearNodes = nearNodes.
                        OrderBy(o => o.Item2).
                        Take(MAXDIVISIONS).
                        ToList();
                }

                // Figure out what percent of the weight to give these nodes (based on the ratio of their distances to the search point)
                var percents = GetPercentOfWeight(nearNodes, SEARCHRADIUSMULT);

                // Exit Function
                return percents.Select(o => new LinkResult1(0, o.Item1, weight * o.Item2)).ToArray();
            }
            /// <remarks>
            /// This is a reworking of Test1.  It will be up to the caller to get the list of points referenced by all the dna links, then
            /// call this method for each referenced point
            /// </remarks>
            public static LinkResult2[] Test2(Point3D search, Point3D[] points, int maxReturn)
            {
                const double SEARCHRADIUSMULT = 2.5d;		// looks at other nodes that are up to minradius * mult

                // Check for exact match
                int index = FindExact(search, points);
                if (index >= 0)
                {
                    return new LinkResult2[] { new LinkResult2(true, index, 1d) };
                }

                // Get a list of nodes that are close to the search point
                var nearNodes = GetNearNodes(search, points, SEARCHRADIUSMULT);

                if (nearNodes.Count == 1)
                {
                    // There's only one, so give it the full weight
                    return new LinkResult2[] { new LinkResult2(false, nearNodes[0].Item1, 1d) };
                }

                // Don't allow too many divisions
                if (nearNodes.Count > maxReturn)
                {
                    nearNodes = nearNodes.
                        OrderBy(o => o.Item2).
                        Take(maxReturn).
                        ToList();
                }

                // Figure out what percent of the weight to give these nodes (based on the ratio of their distances to the search point)
                var percents = GetPercentOfWeight(nearNodes, SEARCHRADIUSMULT);

                // Exit Function
                return percents.Select(o => new LinkResult2(false, o.Item1, o.Item2)).ToArray();
            }

            public static LinkResult3[] Test3(Point3D fromSearch, Point3D toSearch, LinkResult2[] from, LinkResult2[] to, Point3D[] points)
            {

                throw new ApplicationException("flawed");


                // Check for exact match
                if (from.Length == 1 && to.Length == 1)
                {
                    return new LinkResult3[] { new LinkResult3(from[0], to[0], 1d) };
                }

                // Get a line between the two points
                Vector3D line = toSearch - fromSearch;



                #region FLAWED

                // I started by defining an up vector, but the points could go left or right of in, so I would need a double vector.  Rather than go
                // through all that effort, just dot the froms to the tos directly

                //// Get an arbitrary orthoganal that will be defined as an up vector
                //Vector3D orth = Math3D.GetArbitraryOrhonganal(line).ToUnit();

                //double[] fromDots = GetDots(from, orth);
                //double[] toDots = GetDots(to, orth);

                #endregion






                // Get the best matches






            }
            /// <summary>
            /// Test2 takes an original point, and chooses the nearest X items from the new positions.
            /// 
            /// Say two of those original points had a link.  Now you could have several new points that correspond to one
            /// of the originals, linked to several that correspond to the second original
            /// 
            /// This method creates links from one set to the other
            /// </summary>
            public static LinkResult3[] Test4(LinkResult2[] from, LinkResult2[] to, int maxReturn)
            {
                // Find the combinations that have the highest percentage
                List<Tuple<int, int, double>> products = new List<Tuple<int, int, double>>();
                for (int fromCntr = 0; fromCntr < from.Length; fromCntr++)
                {
                    for (int toCntr = 0; toCntr < to.Length; toCntr++)
                    {
                        products.Add(new Tuple<int, int, double>(fromCntr, toCntr, from[fromCntr].Percent * to[toCntr].Percent));
                    }
                }

                // Don't return too many
                IEnumerable<Tuple<int, int, double>> topProducts = null;
                if (products.Count <= maxReturn)
                {
                    topProducts = products;		// no need to sort or limit
                }
                else
                {
                    topProducts = products.
                        OrderByDescending(o => o.Item3).
                        Take(maxReturn).
                        ToArray();
                }

                // Normalize
                double totalPercent = topProducts.Sum(o => o.Item3);

                LinkResult3[] retVal = topProducts.
                    Select(o => new LinkResult3(from[o.Item1], to[o.Item2], o.Item3 / totalPercent)).
                    ToArray();

                return retVal;
            }

            #region Private Methods

            private static int FindExact(Point3D search, Point3D[] points)
            {
                for (int cntr = 0; cntr < points.Length; cntr++)
                {
                    if (Math3D.IsNearValue(points[cntr], search))
                    {
                        return cntr;
                    }
                }

                return -1;
            }

            /// <summary>
            /// This returns the index and distance of the nodes that are close to search
            /// </summary>
            private static List<Tuple<int, double>> GetNearNodes(Point3D search, Point3D[] points, double searchRadiusMultiplier)
            {
                // Get the distances to each point
                double[] distSquared = points.Select(o => (o - search).LengthSquared).ToArray();

                // Find the smallest distance
                int smallestIndex = 0;
                for (int cntr = 1; cntr < distSquared.Length; cntr++)
                {
                    if (distSquared[cntr] < distSquared[smallestIndex])
                    {
                        smallestIndex = cntr;
                    }
                }

                // Figure out how far out to allow
                double min = Math.Sqrt(distSquared[smallestIndex]);
                double maxSquared = Math.Pow(min * searchRadiusMultiplier, 2d);

                // Find all the points in range
                List<Tuple<int, double>> retVal = new List<Tuple<int, double>>();

                // This one is obviously in range (adding it now to avoid an unnessary sqrt)
                retVal.Add(new Tuple<int, double>(smallestIndex, min));

                for (int cntr = 0; cntr < distSquared.Length; cntr++)
                {
                    if (cntr == smallestIndex)
                    {
                        continue;
                    }

                    if (distSquared[cntr] < maxSquared)
                    {
                        retVal.Add(new Tuple<int, double>(cntr, Math.Sqrt(distSquared[cntr])));
                    }
                }

                // Exit Function
                return retVal;
            }

            /// <summary>
            /// This takes in a list of distances, and returns a list of percents (the int just comes along for the ride)
            /// </summary>
            private static List<Tuple<int, double>> GetPercentOfWeight(List<Tuple<int, double>> distances, double searchRadiusMultiplier)
            {
                const double OFFSET = .1d;

                // Find the smallest distance in the list
                double min = distances.Min(o => o.Item2);

                // Figure out what the maximum possible distance would be
                double maxRange = (min * searchRadiusMultiplier) - min;

                // Figure out ratios based on distance
                double[] ratios = new double[distances.Count];
                for (int cntr = 0; cntr < ratios.Length; cntr++)
                {
                    // Normalize the distance
                    ratios[cntr] = UtilityCore.GetScaledValue_Capped(0d, 1d, 0d, maxRange, distances[cntr].Item2 - min);

                    // Run it through a function
                    ratios[cntr] = 1d / (ratios[cntr] + OFFSET);		// need to add an offset, because one of these will be zero
                }

                double total = ratios.Sum();

                // Turn those ratios into percents (normalizing the ratios)
                List<Tuple<int, double>> retVal = new List<Tuple<int, double>>();
                for (int cntr = 0; cntr < ratios.Length; cntr++)
                {
                    retVal.Add(new Tuple<int, double>(distances[cntr].Item1, ratios[cntr] / total));
                }

                // Exit Function
                return retVal;
            }
            private static List<Tuple<int, double>> GetPercentOfWeight_OLD(List<Tuple<int, double>> distances, double searchRadiusMultiplier)
            {
                const double AMOUNTATMAX = .01d;

                double min = distances.Min(o => o.Item2);
                double max = min * searchRadiusMultiplier * 10;		// making max greater so that there is more of a distance

                // The equation will be y=1/kx
                // Where x is the distance from center
                // So solving for k, k=1/xy
                double k = 1d / (max * AMOUNTATMAX);

                double[] scaled = distances.Select(o => 1d / (k * o.Item2)).ToArray();

                double total = scaled.Sum();

                List<Tuple<int, double>> retVal = new List<Tuple<int, double>>();
                for (int cntr = 0; cntr < distances.Count; cntr++)
                {
                    retVal.Add(new Tuple<int, double>(distances[cntr].Item1, scaled[cntr] / total));
                }

                // Exit Function
                return retVal;
            }

            /// <summary>
            /// This returns each link dotted with up
            /// NOTE: up must be a unit vector
            /// </summary>
            private static double[] GetDots(LinkResult2[] links, DoubleVector up, Point3D center, Vector3D line, Point3D[] points)
            {

                throw new ApplicationException("flawed");


                // If it's an exact match, then the link is sitting on the center point
                if (links.Length == 1 && links[0].IsExactMatch)
                {
                    return null;
                }

                double[] retVal = new double[links.Length];

                for (int cntr = 0; cntr < links.Length; cntr++)
                {
                    if (links[cntr].IsExactMatch)
                    {
                        throw new ApplicationException("Can't have an exact match in a list of non exact matches");		// or in any list greater than one
                    }

                    Point3D pointOnLine = Math3D.GetClosestPoint_Line_Point(center, line, points[links[cntr].Index]);
                    // See if it's on the line
                    //if(




                }

                // Exit Function
                return retVal;
            }

            #endregion
        }

        #endregion
        #region Class: GravSensorStuff

        private class GravSensorStuff
        {
            public SensorGravity Sensor = null;
            public Body Body = null;
            public Visual3D Visual = null;
            public List<Tuple<INeuron, SolidColorBrush>> Neurons = null;
            public Visual3D NeuronVisual = null;

            public ScreenSpaceLines3D Gravity = null;
        }

        #endregion
        #region Class: BrainStuff

        private class BrainStuff
        {
            public Brain Brain = null;
            public Body Body = null;
            public Visual3D Visual = null;
            public List<Tuple<INeuron, SolidColorBrush>> Neurons = null;
            public Visual3D NeuronVisual = null;

            public NeuralLinkDNA[] DNAInternalLinks = null;
            public NeuralLinkExternalDNA[] DNAExternalLinks = null;
        }

        #endregion
        #region Class: ThrusterStuff

        private class ThrusterStuff
        {
            public Thruster Thrust = null;
            public Body Body = null;
            public Visual3D Visual = null;
            public List<Tuple<INeuron, SolidColorBrush>> Neurons = null;
            public Visual3D NeuronVisual = null;

            public NeuralLinkExternalDNA[] DNAExternalLinks = null;
        }

        #endregion
        #region Class: ContainerStuff

        private class ContainerStuff
        {
            public EnergyTank Energy = null;
            public Body EnergyBody = null;
            public Visual3D EnergyVisual = null;

            public FuelTank Fuel = null;
            public Body FuelBody = null;
            public Visual3D FuelVisual = null;
        }

        #endregion
        #region Class: LinkStuff

        private class LinkStuff
        {
            public NeuralUtility.ContainerOutput[] Outputs = null;
            //public List<Visual3D> Lines = null;
            public List<Visual3D> Visuals = null;
        }

        #endregion

        #region Declaration Section

        private ItemColors _colors = new ItemColors();
        private EditorOptions _editorOptions = new EditorOptions();
        private ItemOptions _itemOptions = new ItemOptions();

        private Point3D _boundryMin;
        private Point3D _boundryMax;
        //private ScreenSpaceLines3D _boundryLines = null;

        private World _world = null;
        //private Map _map = null;

        private RadiationField _radiation = null;
        private GravityFieldUniform _gravityField = null;

        private MaterialManager _materialManager = null;
        private int _material_Ship = -1;

        private List<ModelVisual3D> _gravityOrientationVisuals = new List<ModelVisual3D>();
        private TrackballGrabber _gravityOrientationTrackball = null;
        private Vector3D _gravity = new Vector3D(0, 0, 1);

        // These listen to the mouse/keyboard and controls the camera
        private TrackBallRoam _trackball = null;
        private TrackBallRoam _trackballNeural = null;

        private GravSensorStuff[] _gravSensors = null;
        private BrainStuff[] _brains = null;
        private ThrusterStuff[] _thrusters = null;
        private ContainerStuff _containers = null;
        private LinkStuff _links = null;
        private List<Visual3D> _debugVisuals = new List<Visual3D>();

        /// <summary>
        /// This runs the brain on a separate thread.  It runs independent of newton's game loop
        /// </summary>
        private Task _brainOperationTask = null;
        private CancellationTokenSource _brainOperationCancel = null;

        private DateTime _lastUpdate = DateTime.UtcNow;

        private bool _isInitialized = false;

        #endregion

        #region Constructor

        public BrainTester()
        {
            InitializeComponent();

            txtGravNeuronDensity.Text = _itemOptions.GravitySensor_NeuronDensity.ToString();
            txtBrainNeuronDensity.Text = _itemOptions.Brain_NeuronDensity.ToString();
            txtBrainChemicalDensity.Text = _itemOptions.Brain_ChemicalDensity.ToString();
            txtLinkBrainInternal.Text = _itemOptions.Brain_LinksPerNeuron_Internal.ToString();
            txtLinkBrainExternalFromSensor.Text = _itemOptions.Brain_LinksPerNeuron_External_FromSensor.ToString();
            txtLinkBrainExternalFromBrain.Text = _itemOptions.Brain_LinksPerNeuron_External_FromBrain.ToString();
            txtBrainNeuronMinDistPercent.Text = _itemOptions.Brain_NeuronMinClusterDistPercent.ToString();

            txtLinkThrusterExternalFromSensor.Text = _itemOptions.Thruster_LinksPerNeuron_Sensor.ToString();
            txtLinkThrusterExternalFromBrain.Text = _itemOptions.Thruster_LinksPerNeuron_Brain.ToString();

            _isInitialized = true;
        }

        #endregion

        #region Event Listeners

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                #region Init World

                _boundryMin = new Point3D(-100, -100, -100);
                _boundryMax = new Point3D(100, 100, 100);

                _world = new World();
                _world.Updating += new EventHandler<WorldUpdatingArgs>(World_Updating);

                List<Point3D[]> innerLines, outerLines;
                _world.SetCollisionBoundry(out innerLines, out outerLines, _boundryMin, _boundryMax);

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

                //_materialManager.RegisterCollisionEvent(_material_Terrain, _material_Bean, Collision_BeanTerrain);

                #endregion
                #region Trackball

                // Trackball
                _trackball = new TrackBallRoam(_camera);
                //_trackball.KeyPanScale = 15d;
                _trackball.EventSource = pnlViewport;		//NOTE:  If this control doesn't have a background color set, the trackball won't see events (I think transparent is ok, just not null)
                _trackball.AllowZoomOnMouseWheel = true;
                _trackball.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.MouseComplete));
                //_trackball.GetOrbitRadius += new GetOrbitRadiusHandler(Trackball_GetOrbitRadius);
                _trackball.ShouldHitTestOnOrbit = false;

                // Trackball
                _trackballNeural = new TrackBallRoam(_cameraNeural);
                //_trackballNeural.KeyPanScale = 15d;
                _trackballNeural.EventSource = pnlViewportNeural;		//NOTE:  If this control doesn't have a background color set, the trackball won't see events (I think transparent is ok, just not null)
                _trackballNeural.AllowZoomOnMouseWheel = true;
                _trackballNeural.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.MouseComplete));
                //_trackballNeural.GetOrbitRadius += new GetOrbitRadiusHandler(Trackball_GetOrbitRadius);
                _trackballNeural.ShouldHitTestOnOrbit = false;

                #endregion
                #region Map

                //_map = new Map(_viewport, null, _world);
                //_map.SnapshotFequency_Milliseconds = 125;
                //_map.SnapshotMaxItemsPerNode = 10;
                //_map.ShouldBuildSnapshots = false;// true;
                //_map.ShouldShowSnapshotLines = false;
                //_map.ShouldSnapshotCentersDrift = true;

                #endregion
                #region Fields

                _radiation = new RadiationField()
                {
                    AmbientRadiation = 0d,
                };

                _gravityField = new GravityFieldUniform();

                #endregion

                // Trackball Controls
                SetupGravityTrackball();

                UpdateGravity();

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

                // Copied from btnClear_Click
                ClearDebugVisuals();
                ClearLinks();
                ClearGravSensors();
                ClearBrains();
                ClearThrusters();
                ClearContainers();




                //_map.Dispose();		// this will dispose the physics bodies
                //_map = null;

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

            #region Refill containers

            if (_containers != null)
            {
                _containers.Energy.QuantityCurrent = _containers.Energy.QuantityMax;
                _containers.Fuel.QuantityCurrent = _containers.Fuel.QuantityMax;
            }

            #endregion

            #region Update sensors

            if (_gravSensors != null)
            {
                foreach (GravSensorStuff sensor in _gravSensors)
                {
                    sensor.Sensor.Update_MainThread(elapsedTime);
                    sensor.Sensor.Update_AnyThread(elapsedTime);
                }

                lblSensorMagnitude.Text = Math.Round(_gravSensors[0].Sensor.CurrentMax, 3).ToString();		// just report the first sensor's value
            }
            else
            {
                lblSensorMagnitude.Text = "";
            }

            #endregion
            #region Update brains

            if (_brains != null)
            {
                foreach (BrainStuff brain in _brains)
                {
                    brain.Brain.Update_MainThread(elapsedTime);
                    brain.Brain.Update_AnyThread(elapsedTime);
                }
            }

            #endregion

            #region Color neurons

            foreach (var neuron in UtilityCore.Iterate(
                _gravSensors == null ? null : _gravSensors.SelectMany(o => o.Neurons),
                _brains == null ? null : _brains.SelectMany(o => o.Neurons),
                _thrusters == null ? null : _thrusters.SelectMany(o => o.Neurons)))
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

        private void txtGravNeuronDensity_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                double? newValue = UpdateSetting(_isInitialized, txtGravNeuronDensity);
                if (newValue != null)
                {
                    _itemOptions.GravitySensor_NeuronDensity = newValue.Value;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void GravityOrientationTrackball_RotationChanged(object sender, EventArgs e)
        {
            //TODO: Draw gravity in the neural and world viewports
            UpdateGravity();
        }
        private void trkGravityStrength_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateGravity();
        }

        private void Sensor_RequestWorldLocation(object sender, PartRequestWorldLocationArgs e)
        {
            try
            {
                if (!(sender is PartBase))
                {
                    throw new ApplicationException("Expected sender to be PartBase");
                }

                PartBase senderCast = (PartBase)sender;

                // These parts aren't part of a ship, so model coords is world coords
                e.Orientation = senderCast.Orientation;
                e.Position = senderCast.Position;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void txtBrainNeuronDensity_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                double? newValue = UpdateSetting(_isInitialized, txtBrainNeuronDensity);
                if (newValue != null)
                {
                    _itemOptions.Brain_NeuronDensity = newValue.Value;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void txtBrainChemicalDensity_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                double? newValue = UpdateSetting(_isInitialized, txtBrainChemicalDensity);
                if (newValue != null)
                {
                    _itemOptions.Brain_ChemicalDensity = newValue.Value;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void txtBrainNeuronMinDistPercent_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                double? newValue = UpdateSetting(_isInitialized, txtBrainNeuronMinDistPercent);
                if (newValue != null)
                {
                    _itemOptions.Brain_NeuronMinClusterDistPercent = newValue.Value;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void txtLinkBrainInternal_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                double? newValue = UpdateSetting(_isInitialized, txtLinkBrainInternal);
                if (newValue != null)
                {
                    _itemOptions.Brain_LinksPerNeuron_Internal = newValue.Value;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void txtLinkBrainExternalFromSensor_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                double? newValue = UpdateSetting(_isInitialized, txtLinkBrainExternalFromSensor);
                if (newValue != null)
                {
                    _itemOptions.Brain_LinksPerNeuron_External_FromSensor = newValue.Value;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void txtLinkBrainExternalFromBrain_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                double? newValue = UpdateSetting(_isInitialized, txtLinkBrainExternalFromBrain);
                if (newValue != null)
                {
                    _itemOptions.Brain_LinksPerNeuron_External_FromBrain = newValue.Value;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void txtLinkThrusterExternalFromSensor_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                double? newValue = UpdateSetting(_isInitialized, txtLinkThrusterExternalFromSensor);
                if (newValue != null)
                {
                    _itemOptions.Thruster_LinksPerNeuron_Sensor = newValue.Value;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void txtLinkThrusterExternalFromBrain_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                double? newValue = UpdateSetting(_isInitialized, txtLinkThrusterExternalFromBrain);
                if (newValue != null)
                {
                    _itemOptions.Thruster_LinksPerNeuron_Brain = newValue.Value;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void chkBrainRunning_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_isInitialized)
                {
                    return;
                }

                CancelBrainOperation();		// just to be safe

                if (chkBrainRunning.IsChecked.Value)
                {
                    chkBrainRunning.Content = "Running";
                    StartBrainOperation();
                }
                else
                {
                    chkBrainRunning.Content = "Paused";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnAdvanceBrainOne_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_links == null)
                {
                    MessageBox.Show("There are no neural links", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                NeuralBucket worker = new NeuralBucket(_links.Outputs.SelectMany(o => UtilityCore.Iterate(o.InternalLinks, o.ExternalLinks)).ToArray());

                worker.Tick();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnMutate100ManyTimes_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // This test verifies that the value will drift randomly (an earlier attempt always drifted toward zero until I fixed it)
                // It's also a good demonstration of why mutating 10% each step over many iterations isn't very smart :)

                //TODO: Show a graph.  That would be cool

                double initial = 100d;
                double mutated = initial;
                double min = initial;
                double max = initial;

                for (int cntr = 0; cntr < 10000000; cntr++)
                {
                    mutated = MutateUtility.Mutate_RandPercent(mutated, .0001d);
                    if (mutated < min)
                    {
                        min = mutated;
                    }

                    if (mutated > max)
                    {
                        max = mutated;
                    }
                }

                MessageBox.Show(string.Format("Initial={0}\r\nFinal={1}\r\nMin={2}\r\nMax={3}", initial.ToString(), mutated.ToString(), min.ToString(), max.ToString()), this.Title, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnRandRange_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<int[]> results = new List<int[]>();
                results.Add(UtilityCore.RandomRange(0, 10).ToArray());
                results.Add(UtilityCore.RandomRange(0, 100, 10).ToArray());
                results.Add(UtilityCore.RandomRange(0, 100, 50).ToArray());
                results.Add(UtilityCore.RandomRange(0, 100, -1).ToArray());
                results.Add(UtilityCore.RandomRange(0, 100, 0).ToArray());
                results.Add(UtilityCore.RandomRange(0, 100, 100).ToArray());
                //results.Add(UtilityHelper.RandomRange(0, 100, 101).ToArray());		// this one throws an exception

                // Check for dupes
                foreach (int[] result in results)
                {
                    if (result.GroupBy(o => o).Any(o => o.Count() != 1))
                    {
                        throw new ApplicationException("This one's a problem");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnMutateItemOptions_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // This method is just to unit test the mutate class

                ItemOptions test1 = MutateUtility.MutateSettingsObject(_itemOptions, new MutateUtility.MuateArgs(.1d));
                ItemOptions test2 = MutateUtility.MutateSettingsObject(_itemOptions, new MutateUtility.MuateArgs(true, 3.5d, .1d));
                ItemOptions test3 = MutateUtility.MutateSettingsObject(_itemOptions, new MutateUtility.MuateArgs(false, 10d, .1d));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #region test classes

        private class NeruonContainerShell : INeuronContainer
        {
            public NeruonContainerShell(Point3D position, Quaternion orientation, INeuron[] neurons)
            {
                this.Position = position;
                this.Orientation = orientation;
                this.Radius = 1d;

                this.Neruons_Readonly = Enumerable.Empty<INeuron>();
                this.Neruons_Writeonly = Enumerable.Empty<INeuron>();
                this.Neruons_ReadWrite = neurons;
                this.Neruons_All = neurons;
            }

            #region INeuronContainer Members

            public IEnumerable<INeuron> Neruons_Readonly
            {
                get;
                private set;
            }
            public IEnumerable<INeuron> Neruons_ReadWrite
            {
                get;
                private set;
            }
            public IEnumerable<INeuron> Neruons_Writeonly
            {
                get;
                private set;
            }

            public IEnumerable<INeuron> Neruons_All
            {
                get;
                private set;
            }

            public Point3D Position
            {
                get;
                private set;
            }
            public Quaternion Orientation
            {
                get;
                private set;
            }
            public double Radius
            {
                get;
                private set;
            }

            public NeuronContainerType NeuronContainerType
            {
                get
                {
                    return NeuronContainerType.Brain;
                }
            }

            public bool IsOn
            {
                get
                {
                    return true;
                }
            }

            #endregion
        }

        #endregion
        private void btnFuzzyLinkTest_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int numTo;
                if (!int.TryParse(txtFuzzyLinkTestCount.Text, out numTo))
                {
                    MessageBox.Show("Couldn't parse the count as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                btnClear_Click(this, new RoutedEventArgs());

                #region Create Neurons

                Point3D fromPoint = new Point3D(-2, 0, 0);
                Point3D toPoint = new Point3D(2, 0, 0);

                List<INeuron> inNeurons = new List<INeuron>();

                // From
                inNeurons.Add(new Neuron_ZeroPos(fromPoint));		// old from

                // To
                inNeurons.Add(new Neuron_NegPos(toPoint));		// old to

                Point3D[] positions = new Point3D[numTo];
                for (int cntr = 0; cntr < numTo; cntr++)
                {
                    positions[cntr] = toPoint + Math3D.GetRandomVector_Spherical(1d);
                    inNeurons.Add(new Neuron_ZeroPos(positions[cntr]));
                }

                // Draw neurons
                List<Tuple<INeuron, SolidColorBrush>> outNeurons;
                ModelVisual3D model;
                BuildNeuronVisuals(out outNeurons, out model, inNeurons, new NeruonContainerShell(new Point3D(0, 0, 0), Quaternion.Identity, null), _colors);

                _debugVisuals.Add(model);
                _viewportNeural.Children.Add(model);

                #endregion
                #region Create Links

                // Get links
                var results = LinkTestUtil.Test1(inNeurons[0].Position, inNeurons[1].Position, positions, 2d);

                // Draw links
                Model3DGroup posLines = null, negLines = null;
                DiffuseMaterial posDiffuse = null, negDiffuse = null;

                BuildLinkVisual(ref posLines, ref posDiffuse, ref negLines, ref negDiffuse, inNeurons[0].Position, inNeurons[1].Position, -.33d, null, _colors);		// original (negative so it's a different color)

                foreach (var result in results)
                {
                    BuildLinkVisual(ref posLines, ref posDiffuse, ref negLines, ref negDiffuse, inNeurons[0].Position, positions[result.To], result.Weight, null, _colors);
                }

                model = new ModelVisual3D();
                model.Content = posLines;
                _debugVisuals.Add(model);
                _viewportNeural.Children.Add(model);

                model = new ModelVisual3D();
                model.Content = negLines;
                _debugVisuals.Add(model);
                _viewportNeural.Children.Add(model);

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnFuzzyLinkTest2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                #region Parse Textboxes

                int numFrom;
                if (!int.TryParse(txtFuzzyLinkTest2FromCount.Text, out numFrom))
                {
                    MessageBox.Show("Couldn't parse the from count as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int numTo;
                if (!int.TryParse(txtFuzzyLinkTest2ToCount.Text, out numTo))
                {
                    MessageBox.Show("Couldn't parse the to count as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int maxIntermediate;
                if (!int.TryParse(txtFuzzyLinkTest2MaxIntermediate.Text, out maxIntermediate))
                {
                    MessageBox.Show("Couldn't parse the max intermediate as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int maxFinal;
                if (!int.TryParse(txtFuzzyLinkTest2MaxFinal.Text, out maxFinal))
                {
                    MessageBox.Show("Couldn't parse the max final as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                #endregion

                btnClear_Click(this, new RoutedEventArgs());

                #region Create Neurons

                Point3D fromPoint = new Point3D(-1.1, 0, 0);
                Point3D toPoint = new Point3D(1.1, 0, 0);

                List<INeuron> inNeurons = new List<INeuron>();

                // From
                inNeurons.Add(new Neuron_ZeroPos(fromPoint));		// old from

                Point3D[] fromPositions = new Point3D[numFrom];
                for (int cntr = 0; cntr < numFrom; cntr++)
                {
                    fromPositions[cntr] = fromPoint + Math3D.GetRandomVector_Spherical(1d);
                    inNeurons.Add(new Neuron_NegPos(fromPositions[cntr]));
                }

                // To
                inNeurons.Add(new Neuron_ZeroPos(toPoint));		// old to

                Point3D[] toPositions = new Point3D[numTo];
                for (int cntr = 0; cntr < numTo; cntr++)
                {
                    toPositions[cntr] = toPoint + Math3D.GetRandomVector_Spherical(1d);
                    inNeurons.Add(new Neuron_NegPos(toPositions[cntr]));
                }


                // Draw neurons
                List<Tuple<INeuron, SolidColorBrush>> outNeurons;
                ModelVisual3D model;
                BuildNeuronVisuals(out outNeurons, out model, inNeurons, new NeruonContainerShell(new Point3D(0, 0, 0), Quaternion.Identity, null), _colors);

                _debugVisuals.Add(model);
                _viewportNeural.Children.Add(model);

                #endregion
                #region Create Links

                // Get links
                var fromResults = LinkTestUtil.Test2(fromPoint, fromPositions, maxIntermediate);
                var toResults = LinkTestUtil.Test2(toPoint, toPositions, maxIntermediate);

                var finalResults = LinkTestUtil.Test4(fromResults, toResults, maxFinal);


                // Draw links
                Model3DGroup posLines = null, negLines = null;
                DiffuseMaterial posDiffuse = null, negDiffuse = null;

                // Draw intermediate results
                foreach (var result in fromResults)
                {
                    BuildLinkVisual(ref posLines, ref posDiffuse, ref negLines, ref negDiffuse, fromPositions[result.Index], toPoint, -result.Percent, null, _colors);
                }

                foreach (var result in toResults)
                {
                    BuildLinkVisual(ref posLines, ref posDiffuse, ref negLines, ref negDiffuse, fromPoint, toPositions[result.Index], -result.Percent, null, _colors);
                }

                // Draw final results
                foreach (var result in finalResults)
                {
                    BuildLinkVisual(ref posLines, ref posDiffuse, ref negLines, ref negDiffuse, fromPositions[result.From.Index], toPositions[result.To.Index], result.Percent * 4d, null, _colors);
                }

                model = new ModelVisual3D();
                model.Content = posLines;
                _debugVisuals.Add(model);
                _viewportNeural.Children.Add(model);

                model = new ModelVisual3D();
                model.Content = negLines;
                _debugVisuals.Add(model);
                _viewportNeural.Children.Add(model);


                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnFuzzyLinkTest2New_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                #region Parse Gui

                int numFrom;
                if (!int.TryParse(txtFuzzyLinkTest2FromCount.Text, out numFrom))
                {
                    MessageBox.Show("Couldn't parse the from count as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int numTo;
                if (!int.TryParse(txtFuzzyLinkTest2ToCount.Text, out numTo))
                {
                    MessageBox.Show("Couldn't parse the to count as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int maxIntermediate;
                if (!int.TryParse(txtFuzzyLinkTest2MaxIntermediate.Text, out maxIntermediate))
                {
                    MessageBox.Show("Couldn't parse the max intermediate as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int maxFinal;
                if (!int.TryParse(txtFuzzyLinkTest2MaxFinal.Text, out maxFinal))
                {
                    MessageBox.Show("Couldn't parse the max final as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                #endregion

                btnClear_Click(this, new RoutedEventArgs());

                #region Create Neurons

                Point3D fromPoint = new Point3D(-1.1, 0, 0);
                Point3D toPoint = new Point3D(1.1, 0, 0);

                List<INeuron> inNeurons = new List<INeuron>();

                // From
                inNeurons.Add(new Neuron_ZeroPos(fromPoint));		// old from

                Point3D[] fromPositions = new Point3D[numFrom];
                for (int cntr = 0; cntr < numFrom; cntr++)
                {
                    fromPositions[cntr] = fromPoint + Math3D.GetRandomVector_Spherical(1d);
                    inNeurons.Add(new Neuron_NegPos(fromPositions[cntr]));
                }

                // To
                inNeurons.Add(new Neuron_ZeroPos(toPoint));		// old to

                Point3D[] toPositions = new Point3D[numTo];
                for (int cntr = 0; cntr < numTo; cntr++)
                {
                    toPositions[cntr] = toPoint + Math3D.GetRandomVector_Spherical(1d);
                    inNeurons.Add(new Neuron_NegPos(toPositions[cntr]));
                }

                Point3D[] allNew = fromPositions.
                    Concat(toPositions).
                    ToArray();

                // Draw neurons
                List<Tuple<INeuron, SolidColorBrush>> outNeurons;
                ModelVisual3D model;
                BuildNeuronVisuals(out outNeurons, out model, inNeurons, new NeruonContainerShell(new Point3D(0, 0, 0), Quaternion.Identity, null), _colors);

                _debugVisuals.Add(model);
                _viewportNeural.Children.Add(model);

                #endregion

                var existingLinks = new[]
                {
                    Tuple.Create(fromPoint, toPoint, 1d),
                };

                var newLinks = ItemLinker.FuzzyLink(existingLinks, allNew, maxFinal, maxIntermediate);

                #region Draw Links

                // Draw links
                Model3DGroup posLines = null, negLines = null;
                DiffuseMaterial posDiffuse = null, negDiffuse = null;

                // Draw initial results
                BuildLinkVisual(ref posLines, ref posDiffuse, ref negLines, ref negDiffuse, fromPoint, toPoint, -1d, null, _colors);

                // Draw final results
                foreach (var result in newLinks)
                {
                    BuildLinkVisual(ref posLines, ref posDiffuse, ref negLines, ref negDiffuse, allNew[result.Item1], allNew[result.Item2], result.Item3 * 4d, null, _colors);
                }

                model = new ModelVisual3D();
                model.Content = posLines;
                _debugVisuals.Add(model);
                _viewportNeural.Children.Add(model);

                model = new ModelVisual3D();
                model.Content = negLines;
                _debugVisuals.Add(model);
                _viewportNeural.Children.Add(model);

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnFuzzyLinkTest3_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                #region parse gui

                int numPoints;
                if (!int.TryParse(txtFuzzyLinkTest3Points.Text, out numPoints))
                {
                    MessageBox.Show("Couldn't parse the number of points as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int initialLinkCount;
                if (!int.TryParse(txtFuzzyLinkTest3Links.Text, out initialLinkCount))
                {
                    MessageBox.Show("Couldn't parse initial links as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int maxIntermediateLinkCount;
                if (!int.TryParse(txtFuzzyLinkTest3MaxIntermediate.Text, out maxIntermediateLinkCount))
                {
                    MessageBox.Show("Couldn't parse max intermediate links as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int maxFinalLinkCount;
                if (!int.TryParse(txtFuzzyLinkTest3MaxFinal.Text, out maxFinalLinkCount))
                {
                    MessageBox.Show("Couldn't parse max final links as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                #endregion

                btnClear_Click(this, new RoutedEventArgs());

                #region create a cloud of points

                List<INeuron> initialNeurons = new List<INeuron>();

                Point3D[] initialPositions = new Point3D[numPoints];
                for (int cntr = 0; cntr < numPoints; cntr++)
                {
                    initialPositions[cntr] = Math3D.GetRandomVector_Spherical(2.5d).ToPoint();
                    initialNeurons.Add(new Neuron_ZeroPos(initialPositions[cntr]));
                }

                #endregion

                #region create some initial links

                var initialLinks = UtilityCore.GetRandomPairs(numPoints, initialLinkCount).
                    ToArray();

                #endregion

                #region create a new cloud of points

                List<INeuron> finalNeurons = new List<INeuron>();

                Point3D[] finalPositions = new Point3D[numPoints];
                for (int cntr = 0; cntr < numPoints; cntr++)
                {
                    finalPositions[cntr] = Math3D.GetRandomVector_Spherical(2.5d).ToPoint();
                    finalNeurons.Add(new Neuron_NegPos(finalPositions[cntr]));
                }

                #endregion

                #region relink

                var initialLinkPoints = initialLinks.
                    Select(o => Tuple.Create(initialPositions[o.Item1], initialPositions[o.Item2], 1d)).
                    ToArray();

                var newLinks = ItemLinker.FuzzyLink(initialLinkPoints, finalPositions, maxFinalLinkCount, maxIntermediateLinkCount);

                #endregion

                #region draw neurons

                // Initial (don't want to draw all of them, that's just distracting)
                var initialFiltered = initialLinks.SelectMany(o => new[] { o.Item1, o.Item2 }).
                    Distinct().
                    Select(o => initialNeurons[o]);

                List<Tuple<INeuron, SolidColorBrush>> outNeurons;
                ModelVisual3D model;
                BuildNeuronVisuals(out outNeurons, out model, initialFiltered, new NeruonContainerShell(new Point3D(0, 0, 0), Quaternion.Identity, null), _colors);

                _debugVisuals.Add(model);
                _viewportNeural.Children.Add(model);

                // Final
                BuildNeuronVisuals(out outNeurons, out model, finalNeurons, new NeruonContainerShell(new Point3D(0, 0, 0), Quaternion.Identity, null), _colors);

                _debugVisuals.Add(model);
                _viewportNeural.Children.Add(model);

                #endregion
                #region draw links

                Model3DGroup posLines = null, negLines = null;
                DiffuseMaterial posDiffuse = null, negDiffuse = null;

                // Initial
                foreach (var link in initialLinks)
                {
                    BuildLinkVisual(ref posLines, ref posDiffuse, ref negLines, ref negDiffuse, initialPositions[link.Item1], initialPositions[link.Item2], -1d, null, _colors);
                }

                // Final
                foreach (var result in newLinks)
                {
                    BuildLinkVisual(ref posLines, ref posDiffuse, ref negLines, ref negDiffuse, finalPositions[result.Item1], finalPositions[result.Item2], result.Item3 * 4d, null, _colors);
                }

                model = new ModelVisual3D();
                model.Content = posLines;
                _debugVisuals.Add(model);
                _viewportNeural.Children.Add(model);

                model = new ModelVisual3D();
                model.Content = negLines;
                _debugVisuals.Add(model);
                _viewportNeural.Children.Add(model);

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #region test classes

        private class DNATwin
        {
            public string[] List0
            {
                get;
                set;
            }
            public string[] List1
            {
                get;
                set;
            }
        }

        private class DNAQuad : DNATwin
        {
            public string String0
            {
                get;
                set;
            }
            public string[][] Jagged0
            {
                get;
                set;
            }
        }

        #endregion
        private void btnPropsByPercent1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //TODO: Make a test button that builds a tree, then parses it into a 1D list

                Random rand = StaticRandom.GetRandomForThread();

                #region Build Lists

                // These were copied from the previous tester buttons

                // List1
                string[] list1 = Enumerable.Repeat("", 3 + rand.Next(100)).ToArray();

                // List2
                List<string[]> list2 = Enumerable.Range(0, 3 + rand.Next(30)).
                    Select(o => Enumerable.Repeat("", rand.Next(20)).ToArray()).ToList();

                // List3
                List<DNATwin> list3 = Enumerable.Range(0, 3 + rand.Next(20)).
                    Select(o => new DNATwin()
                    {
                        List0 = Enumerable.Repeat("", rand.Next(12)).ToArray(),
                        List1 = Enumerable.Repeat("", rand.Next(18)).ToArray()
                    }).ToList();

                // List4
                List<DNATwin> list4 = new List<DNATwin>();
                foreach (var cntr in Enumerable.Range(0, 3 + rand.Next(20)))
                {
                    if (rand.Next(2) == 0)
                    {
                        list4.Add(new DNATwin()
                        {
                            List0 = Enumerable.Repeat("", rand.Next(12)).ToArray(),
                            List1 = Enumerable.Repeat("", rand.Next(18)).ToArray()
                        });
                    }
                    else
                    {
                        list4.Add(new DNAQuad()
                        {
                            List0 = Enumerable.Repeat("", rand.Next(12)).ToArray(),
                            List1 = Enumerable.Repeat("", rand.Next(18)).ToArray(),
                            String0 = "",
                            Jagged0 = Enumerable.Range(0, 3 + rand.Next(5)).
                                Select(o => Enumerable.Repeat("", rand.Next(10)).ToArray()).ToArray()
                        });
                    }
                }

                #endregion

                #region Index the lists

                List<PropsByPercent> props = new List<PropsByPercent>();

                //NOTE: Can't have a naked list of items (strings, doubles).  The PropsByPercent class was written to handle a list of classes that hold
                //properties, not a list of properties directly.  If a string[] were to be passed in, it would be seen as a list of strings that hold char arrays
                //
                //NOTE: Tuple is readonly, so PropsByPercent will index it just fine, but when you try to call PropsByPercent.PropWrapper.SetValue,
                //it throws a readonly exception
                //props.Add(new PropsByPercent(list1.Select(o => new Tuple<string>(o))));
                //props.Add(new PropsByPercent(list2.Select(o => new Tuple<string[]>(o))));

                props.Add(new PropsByPercent(list3));
                props.Add(new PropsByPercent(list4));

                #endregion

                #region Modify random properties

                foreach (PropsByPercent propList in props)
                {
                    List<PropsByPercent.PropWrapper> used = new List<PropsByPercent.PropWrapper>();

                    // Modify 10% of the properties in this list
                    int count = Convert.ToInt32(Math.Ceiling(propList.Count * .1d));

                    for (int cntr = 0; cntr < count; cntr++)
                    {
                        while (true)
                        {
                            double percent = rand.NextDouble();

                            PropsByPercent.PropWrapper prop = propList.GetProperty(percent);
                            if (prop == null)
                            {
                                throw new ApplicationException("Didn't find property at percent: " + percent.ToString());
                            }

                            if (!used.Contains(prop))
                            {
                                prop.SetValue("XXXXXX");		// all the lists in this test method are some kind of string, so no need to check datatype
                                used.Add(prop);
                                break;
                            }
                        }
                    }
                }

                #endregion

                #region Modify all properties

                //foreach (PropsByPercent.PropWrapper prop in props.SelectMany(o => o))		// this will work, but for this tester, I want to more explicitly hit foreach

                foreach (PropsByPercent propList in props)
                {
                    foreach (PropsByPercent.PropWrapper prop in propList)
                    {
                        prop.SetValue("all work and no play makes jack a dull boy");
                    }
                }

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnPropsByPercent2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Random rand = StaticRandom.GetRandomForThread();

                #region Build Lists

                //// List1
                //string[] list1 = Enumerable.Repeat("", 3 + rand.Next(100)).ToArray();

                //// List2
                //List<string[]> list2 = Enumerable.Range(0, 3 + rand.Next(30)).
                //    Select(o => Enumerable.Repeat("", rand.Next(20)).ToArray()).ToList();

                //// List3
                //List<DNATwin> list3 = Enumerable.Range(0, 3 + rand.Next(20)).
                //    Select(o => new DNATwin()
                //    {
                //        List0 = Enumerable.Repeat("", rand.Next(12)).ToArray(),
                //        List1 = Enumerable.Repeat("", rand.Next(18)).ToArray()
                //    }).ToList();

                // List4
                List<DNATwin> list4 = new List<DNATwin>();
                foreach (var cntr in Enumerable.Range(0, 3 + rand.Next(20)))
                {
                    if (rand.Next(2) == 0)
                    {
                        list4.Add(new DNATwin()
                        {
                            List0 = Enumerable.Repeat("", rand.Next(12)).ToArray(),
                            List1 = Enumerable.Repeat("", rand.Next(18)).ToArray()
                        });
                    }
                    else
                    {
                        list4.Add(new DNAQuad()
                        {
                            List0 = Enumerable.Repeat("", rand.Next(12)).ToArray(),
                            List1 = Enumerable.Repeat("", rand.Next(18)).ToArray(),
                            String0 = "",
                            Jagged0 = Enumerable.Range(0, 3 + rand.Next(5)).
                                Select(o => Enumerable.Repeat("", rand.Next(10)).ToArray()).ToArray()
                        });
                    }
                }

                // List5 (empty)
                List<DNATwin> list5 = Enumerable.Range(0, 3 + rand.Next(20)).
                    Select(o => new DNATwin()
                    {
                        List0 = new string[0],
                        List1 = new string[0]
                    }).ToList();

                // List6 (one empty item)
                List<DNATwin> list6 = new List<DNATwin>();
                list6.Add(new DNATwin() { List0 = new string[0], List1 = new string[0] });
                list6.Add(new DNATwin() { List0 = new string[] { "" }, List1 = new string[0] });
                list6.Add(new DNATwin() { List0 = new string[0], List1 = new string[0] });

                #endregion

                #region Valid Filters

                PropsByPercent props1 = new PropsByPercent(list4, new PropsByPercent.FilterArgs() { IgnoreNames = new string[] { "List1", "String0" } });
                PropsByPercent props2 = new PropsByPercent(list4, new PropsByPercent.FilterArgs() { OnlyUseNames = new string[] { "List1", "String0" } });

                PropsByPercent props3 = new PropsByPercent(list4, new PropsByPercent.FilterArgs() { IgnoreTypesRaw = new string[] { PropsByPercent.PROP_STRINGARRAY } });
                PropsByPercent props4 = new PropsByPercent(list4, new PropsByPercent.FilterArgs() { OnlyUseTypesRaw = new string[] { PropsByPercent.PROP_STRINGARRAY } });

                PropsByPercent props5 = new PropsByPercent(list4, new PropsByPercent.FilterArgs() { IgnoreTypes = new PropsByPercent.DataType[] { PropsByPercent.DataType.String } });		// this had divide by zero because no props matched the filter
                PropsByPercent props5a = new PropsByPercent(list5);		// tests divide by 0 when the list is empty
                PropsByPercent props5b = new PropsByPercent(list6);		// tests divide by 0 when the list is empty
                PropsByPercent props6 = new PropsByPercent(list4, new PropsByPercent.FilterArgs() { OnlyUseTypes = new PropsByPercent.DataType[] { PropsByPercent.DataType.String } });

                PropsByPercent props7 = new PropsByPercent(list4, new PropsByPercent.FilterArgs()
                {
                    IgnoreNames = new string[] { "List1", "String0" },
                    OnlyUseTypesRaw = new string[] { PropsByPercent.PROP_STRINGJAGGED }
                });

                double percent = .42d;
                var result1 = props1.GetProperty(percent);
                var result2 = props2.GetProperty(percent);
                var result3 = props3.GetProperty(percent);
                var result4 = props4.GetProperty(percent);
                var result5 = props5.GetProperty(percent);
                var result5a = props5a.GetProperty(percent);
                var result5b = props5b.GetProperty(percent);
                var result6 = props6.GetProperty(percent);
                var result7 = props7.GetProperty(percent);

                foreach (var result in props5)
                {
                    int three = 6;
                }

                foreach (var result in props5a)
                {
                    int three = 2;
                }

                foreach (var result in props5b)
                {
                    int three = 5;
                }

                #endregion

                #region Invalid Filters

                try
                {
                    PropsByPercent props8 = new PropsByPercent(list4, new PropsByPercent.FilterArgs()
                    {
                        IgnoreNames = new string[] { "List1", "String0" },
                        OnlyUseNames = new string[] { "List1", "String0" }
                    });
                }
                catch (Exception) { }

                try
                {
                    PropsByPercent props9 = new PropsByPercent(list4, new PropsByPercent.FilterArgs()
                    {
                        IgnoreTypesRaw = new string[] { "a", "b" },
                        OnlyUseTypesRaw = new string[] { "c", "d" }
                    });
                }
                catch (Exception) { }

                try
                {
                    PropsByPercent props10 = new PropsByPercent(list4, new PropsByPercent.FilterArgs()
                    {
                        IgnoreTypes = new PropsByPercent.DataType[] { PropsByPercent.DataType.Unknown },
                        OnlyUseTypes = new PropsByPercent.DataType[] { PropsByPercent.DataType.String }
                    });
                }
                catch (Exception) { }

                try
                {
                    PropsByPercent props11 = new PropsByPercent(list4, new PropsByPercent.FilterArgs()
                    {
                        OnlyUseTypesRaw = new string[] { "c", "d" },
                        OnlyUseTypes = new PropsByPercent.DataType[] { PropsByPercent.DataType.String }
                    });
                }
                catch (Exception) { }

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnGravitySensor_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double size;
                if (!double.TryParse(txtGravitySensorSize.Text, out size))
                {
                    MessageBox.Show("Couldn't parse sensor size", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int numSensors;
                if (!int.TryParse(txtGravitySensorCount.Text, out numSensors))
                {
                    MessageBox.Show("Couldn't parse number of sensors", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (numSensors < 1)
                {
                    MessageBox.Show("The number of sensors must be greater than zero", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Wipe Existing
                ClearGravSensors();

                if (_containers == null)
                {
                    CreateContainers();
                }

                // Build DNA
                ShipPartDNA dna = new ShipPartDNA();
                dna.PartType = SensorGravity.PARTTYPE;
                dna.Position = new Point3D(-1.5, 0, 0);
                dna.Orientation = Quaternion.Identity;
                dna.Scale = new Vector3D(size, size, size);

                ShipPartDNA[] gravDNA = new ShipPartDNA[numSensors];

                for (int cntr = 0; cntr < numSensors; cntr++)
                {
                    if (numSensors == 1)
                    {
                        gravDNA[cntr] = dna;
                    }
                    else
                    {
                        ShipPartDNA dnaCopy = ShipPartDNA.Clone(dna);
                        double angle = 360d / Convert.ToDouble(numSensors);
                        dnaCopy.Position += new Vector3D(0, .75, 0).GetRotatedVector(new Vector3D(1, 0, 0), angle * cntr);

                        gravDNA[cntr] = dnaCopy;
                    }
                }

                // Build/show sensors
                CreateGravSensors(gravDNA);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnBrain_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double size;
                if (!double.TryParse(txtBrainSize.Text, out size))
                {
                    MessageBox.Show("Couldn't parse brain size", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int numBrains;
                if (!int.TryParse(txtBrainCount.Text, out numBrains))
                {
                    MessageBox.Show("Couldn't parse number of brains", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (numBrains < 1)
                {
                    MessageBox.Show("The number of brains must be greater than zero", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Wipe Existing
                ClearBrains();

                if (_containers == null)
                {
                    CreateContainers();
                }

                // Build DNA
                ShipPartDNA dna = new ShipPartDNA();
                dna.PartType = Brain.PARTTYPE;
                dna.Position = new Point3D(0, 0, 0);
                dna.Orientation = Quaternion.Identity;
                dna.Scale = new Vector3D(size, size, size);
                dna.Neurons = null;
                dna.AltNeurons = null;
                dna.InternalLinks = null;
                dna.ExternalLinks = null;

                ShipPartDNA[] brainDNA = new ShipPartDNA[numBrains];
                for (int cntr = 0; cntr < numBrains; cntr++)
                {
                    if (numBrains == 1)
                    {
                        brainDNA[cntr] = dna;
                    }
                    else
                    {
                        ShipPartDNA dnaCopy = ShipPartDNA.Clone(dna);
                        double angle = 360d / Convert.ToDouble(numBrains);
                        dnaCopy.Position += new Vector3D(0, 1, 0).GetRotatedVector(new Vector3D(1, 0, 0), angle * cntr);

                        brainDNA[cntr] = dnaCopy;
                    }
                }

                // Build/Show brains
                CreateBrains(brainDNA);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnThrusters_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int numThrusters;
                if (!int.TryParse(txtThrusterCount.Text, out numThrusters))
                {
                    MessageBox.Show("Couldn't parse number of thrusters", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (numThrusters < 1)
                {
                    MessageBox.Show("The number of thrusters must be greater than zero", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Wipe Existing
                ClearThrusters();

                if (_containers == null)
                {
                    CreateContainers();
                }

                // Build DNA
                ThrusterDNA dna = new ThrusterDNA();
                dna.PartType = Thruster.PARTTYPE;
                dna.Position = new Point3D(2, 0, 0);
                dna.Orientation = new Quaternion(new Vector3D(0, 1, 0), -90);
                dna.Scale = new Vector3D(.5, .5, .5);
                dna.ThrusterType = ThrusterType.One;
                //dna.ThrusterType = ThrusterType.Custom;
                dna.ThrusterDirections = ThrusterDesign.GetThrusterDirections(ThrusterType.One);

                ThrusterDNA[] dnaThrust = new ThrusterDNA[numThrusters];

                for (int cntr = 0; cntr < numThrusters; cntr++)
                {
                    if (numThrusters == 1)
                    {
                        dnaThrust[cntr] = dna;
                    }
                    else
                    {
                        ThrusterDNA dnaCopy = (ThrusterDNA)ShipPartDNA.Clone(dna);
                        double angle = 360d / Convert.ToDouble(numThrusters);
                        dnaCopy.Position += new Vector3D(0, .75, 0).GetRotatedVector(new Vector3D(1, 0, 0), angle * cntr);

                        dnaThrust[cntr] = dnaCopy;
                    }
                }

                // Create/Show fuel,thrusters
                CreateThrusters(dnaThrust);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnLinks_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Wipe Existing
                ClearLinks();

                // Wipe out all the dna links so that it creates random links
                if (_brains != null)
                {
                    foreach (BrainStuff brain in _brains)
                    {
                        brain.DNAInternalLinks = null;
                        brain.DNAExternalLinks = null;
                    }
                }

                if (_thrusters != null)
                {
                    foreach (var thrust in _thrusters)
                    {
                        thrust.DNAExternalLinks = null;
                    }
                }

                // Build new
                CreateLinks();

                #region OLD
                //#region Build up input args

                //List<NeuralUtility.ContainerInput> inputs = new List<NeuralUtility.ContainerInput>();
                //if (_gravSensors != null)
                //{
                //    foreach (GravSensorStuff sensor in _gravSensors)
                //    {
                //        // The sensor is a source, so shouldn't have any links.  But it needs to be included in the args so that other
                //        // neuron containers can hook to it
                //        inputs.Add(new NeuralUtility.ContainerInput(sensor.Sensor, NeuralUtility.NeuronContainerType.Sensor, sensor.Sensor.Position, sensor.Sensor.Orientation, null, null, 0, null, null));
                //    }
                //}

                //if (_brains != null)
                //{
                //    foreach (BrainStuff brain in _brains)
                //    {
                //        //TODO: Consider existing links
                //        inputs.Add(new NeuralUtility.ContainerInput(
                //            brain.Brain, NeuralUtility.NeuronContainerType.Brain,
                //            brain.Brain.Position, brain.Brain.Orientation,
                //            _itemOptions.BrainLinksPerNeuron_Internal,
                //            new Tuple<NeuralUtility.NeuronContainerType, NeuralUtility.ExternalLinkRatioCalcType, double>[]
                //            {
                //                new Tuple<NeuralUtility.NeuronContainerType,NeuralUtility.ExternalLinkRatioCalcType,double>(NeuralUtility.NeuronContainerType.Sensor, NeuralUtility.ExternalLinkRatioCalcType.Smallest, _itemOptions.BrainLinksPerNeuron_External_FromSensor),
                //                new Tuple<NeuralUtility.NeuronContainerType,NeuralUtility.ExternalLinkRatioCalcType,double>(NeuralUtility.NeuronContainerType.Brain, NeuralUtility.ExternalLinkRatioCalcType.Average, _itemOptions.BrainLinksPerNeuron_External_FromBrain),
                //                new Tuple<NeuralUtility.NeuronContainerType,NeuralUtility.ExternalLinkRatioCalcType,double>(NeuralUtility.NeuronContainerType.Manipulator, NeuralUtility.ExternalLinkRatioCalcType.Smallest, _itemOptions.BrainLinksPerNeuron_External_FromManipulator)
                //            },
                //            Convert.ToInt32(Math.Round(brain.Brain.BrainChemicalCount * 1.33d, 0)),		// increasing so that there is a higher chance of listeners
                //            null, null));
                //    }
                //}

                //if (_thrusters != null)
                //{
                //    foreach (var thrust in _thrusters)
                //    {
                //        //TODO: Consider existing links
                //        //NOTE: This won't be fed by other manipulators
                //        inputs.Add(new NeuralUtility.ContainerInput(
                //            thrust.Thrust, NeuralUtility.NeuronContainerType.Manipulator,
                //            thrust.Thrust.Position, thrust.Thrust.Orientation,
                //            null,
                //            new Tuple<NeuralUtility.NeuronContainerType, NeuralUtility.ExternalLinkRatioCalcType, double>[]
                //            {
                //                new Tuple<NeuralUtility.NeuronContainerType,NeuralUtility.ExternalLinkRatioCalcType,double>(NeuralUtility.NeuronContainerType.Sensor, NeuralUtility.ExternalLinkRatioCalcType.Destination, _itemOptions.ThrusterLinksPerNeuron_Sensor),
                //                new Tuple<NeuralUtility.NeuronContainerType,NeuralUtility.ExternalLinkRatioCalcType,double>(NeuralUtility.NeuronContainerType.Brain, NeuralUtility.ExternalLinkRatioCalcType.Destination, _itemOptions.ThrusterLinksPerNeuron_Brain),
                //            },
                //            0,
                //            null, null));
                //    }
                //}

                //#endregion

                //// Create links
                //NeuralUtility.ContainerOutput[] outputs = null;
                //if (inputs.Count > 0)
                //{
                //    outputs = NeuralUtility.LinkNeurons(inputs.ToArray(), _itemOptions.NeuralLinkMaxWeight);
                //}




                //#region Show new links

                //if (outputs != null)
                //{
                //    _links = new LinkStuff();
                //    _links.Outputs = outputs;
                //    _links.Visuals = new List<Visual3D>();

                //    Model3DGroup posLines = null, negLines = null;
                //    DiffuseMaterial posDiffuse = null, negDiffuse = null;

                //    Dictionary<INeuronContainer, Transform3D> containerTransforms = new Dictionary<INeuronContainer, Transform3D>();

                //    foreach (var output in outputs)
                //    {
                //        Transform3D toTransform = GetContainerTransform(containerTransforms, output.Container);

                //        foreach (var link in UtilityHelper.Iterate(output.InternalLinks, output.ExternalLinks))
                //        {
                //            Transform3D fromTransform = GetContainerTransform(containerTransforms, link.FromContainer);

                //            BuildLinkVisual(ref posLines, ref posDiffuse, ref negLines, ref negDiffuse, fromTransform.Transform(link.From.Position), toTransform.Transform(link.To.Position), link.Weight, link.BrainChemicalModifiers, _colors);
                //        }
                //    }

                //    if (posLines != null)
                //    {
                //        ModelVisual3D model = new ModelVisual3D();
                //        model.Content = posLines;
                //        _links.Visuals.Add(model);
                //        _viewportNeural.Children.Add(model);
                //    }

                //    if (negLines != null)
                //    {
                //        ModelVisual3D model = new ModelVisual3D();
                //        model.Content = negLines;
                //        _links.Visuals.Add(model);
                //        _viewportNeural.Children.Add(model);
                //    }
                //}

                //#endregion

                //UpdateCountReport();

                //if (chkBrainRunning.IsChecked.Value)
                //{
                //    StartBrainOperation();
                //}
                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnMutate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                #region Extract dna

                ShipPartDNA[] gravDNA1 = null;
                if (_gravSensors != null)
                {
                    gravDNA1 = _gravSensors.Select(o => o.Sensor.GetNewDNA()).ToArray();		// no need to call NeuralUtility.PopulateDNALinks, only the neurons are stored
                }

                ShipPartDNA[] brainDNA1 = null;
                if (_brains != null)
                {
                    brainDNA1 = _brains.Select(o =>
                    {
                        ShipPartDNA dna = o.Brain.GetNewDNA();
                        if (_links != null)
                        {
                            NeuralUtility.PopulateDNALinks(dna, o.Brain, _links.Outputs);
                        }
                        return dna;
                    }).ToArray();
                }

                ThrusterDNA[] thrustDNA1 = null;
                if (_thrusters != null)
                {
                    thrustDNA1 = _thrusters.Select(o =>
                    {
                        ThrusterDNA dna = (ThrusterDNA)o.Thrust.GetNewDNA();
                        if (_links != null)
                        {
                            NeuralUtility.PopulateDNALinks(dna, o.Thrust, _links.Outputs);
                        }
                        return dna;
                    }).ToArray();
                }

                ShipPartDNA energyDNA1 = null;
                ShipPartDNA fuelDNA1 = null;
                if (_containers != null)
                {
                    energyDNA1 = _containers.Energy.GetNewDNA();
                    fuelDNA1 = _containers.Fuel.GetNewDNA();
                }

                // Combine the lists
                List<ShipPartDNA> allParts1 = UtilityCore.Iterate<ShipPartDNA>(gravDNA1, brainDNA1, thrustDNA1).ToList();
                if (allParts1.Count == 0)
                {
                    // There is nothing to do
                    return;
                }

                if (energyDNA1 != null)
                {
                    allParts1.Add(energyDNA1);
                }

                if (fuelDNA1 != null)
                {
                    allParts1.Add(fuelDNA1);
                }

                #endregion

                bool hadLinks = _links != null;

                // Clear existing
                ClearLinks();
                ClearGravSensors();
                ClearBrains();
                ClearThrusters();

                #region Fill in mutate args

                //TODO: Store these args in ShipDNA itself.  That way, mutation rates are a property of ship
                //TODO: Fill in the params from ItemOptions
                //NOTE: These mutate factors are pretty large.  Good for a unit tester, but not a real simulation

                var mutate_Vector3D = Tuple.Create(PropsByPercent.DataType.Vector3D, new MutateUtility.MuateFactorArgs(MutateUtility.FactorType.Percent, .08d));
                var mutate_Point3D = Tuple.Create(PropsByPercent.DataType.Point3D, new MutateUtility.MuateFactorArgs(MutateUtility.FactorType.Distance, .04d));		// positions need to drift around freely.  percent doesn't make much sense
                var mutate_Quaternion = Tuple.Create(PropsByPercent.DataType.Quaternion, new MutateUtility.MuateFactorArgs(MutateUtility.FactorType.Percent, .08d));

                MutateUtility.ShipPartAddRemoveArgs addRemoveArgs = null;
                if (chkMutateAddRemoveParts.IsChecked.Value)
                {
                    //addRemoveArgs = new MutateUtility.ShipPartAddRemoveArgs();
                }

                MutateUtility.MuateArgs partChangeArgs = null;
                if (chkMutateChangeParts.IsChecked.Value)
                {
                    //NOTE: The mutate class has special logic for Scale and ThrusterDirections
                    partChangeArgs = new MutateUtility.MuateArgs(true, 1d,
                        null,
                        new Tuple<PropsByPercent.DataType, MutateUtility.MuateFactorArgs>[]
						{
							mutate_Vector3D,
							mutate_Point3D,
							mutate_Quaternion,
						},
                        new MutateUtility.MuateFactorArgs(MutateUtility.FactorType.Percent, .1d));
                }

                MutateUtility.NeuronMutateArgs neuralArgs = null;
                if (chkMutateChangeNeural.IsChecked.Value)
                {
                    MutateUtility.MuateArgs neuronMovement = new MutateUtility.MuateArgs(false, .05d, null, null, mutate_Point3D.Item2);		// neurons are all point3D

                    MutateUtility.MuateArgs linkMovement = new MutateUtility.MuateArgs(false, .05d,
                        null,
                        new Tuple<PropsByPercent.DataType, MutateUtility.MuateFactorArgs>[]
						{
							Tuple.Create(PropsByPercent.DataType.Double, new MutateUtility.MuateFactorArgs(MutateUtility.FactorType.Distance, .1d)),		// all the doubles are weights, which need to be able to cross over zero (percents can't go + to -)
							Tuple.Create(PropsByPercent.DataType.Point3D, new MutateUtility.MuateFactorArgs(MutateUtility.FactorType.Distance, .2d)),		// using a larger value for the links
							mutate_Quaternion,
						},
                        null);

                    neuralArgs = new MutateUtility.NeuronMutateArgs(neuronMovement, null, linkMovement, null);
                }

                MutateUtility.ShipMutateArgs shipArgs = new MutateUtility.ShipMutateArgs(addRemoveArgs, partChangeArgs, neuralArgs);

                #endregion

                // Mutate
                ShipDNA oldDNA = ShipDNA.Create(allParts1);
                ShipDNA newDNA = MutateUtility.Mutate(oldDNA, shipArgs);

                if (chkMutateWriteToFile.IsChecked.Value)
                {
                    #region Write to file

                    string foldername = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    string timestamp = DateTime.Now.ToString("yyyyMMdd HHmmssfff");

                    string oldFilename = System.IO.Path.Combine(foldername, timestamp + " Old DNA.xml");
                    string newFilename = System.IO.Path.Combine(foldername, timestamp + " New DNA.xml");

                    using (System.IO.StreamWriter writer = new System.IO.StreamWriter(oldFilename, false))
                    {
                        System.Xaml.XamlServices.Save(writer, oldDNA);
                    }

                    using (System.IO.StreamWriter writer = new System.IO.StreamWriter(newFilename, false))
                    {
                        System.Xaml.XamlServices.Save(writer, newDNA);
                    }

                    #endregion
                }

                #region Rebuild Parts

                ShipPartDNA[] allParts2 = newDNA.PartsByLayer.SelectMany(o => o.Value).ToArray();

                ShipPartDNA[] gravDNA2 = allParts2.Where(o => o.PartType == SensorGravity.PARTTYPE).ToArray();
                if (gravDNA2.Length > 0)
                {
                    CreateGravSensors(gravDNA2);
                }

                ShipPartDNA[] brainDNA2 = allParts2.Where(o => o.PartType == Brain.PARTTYPE).ToArray();
                if (brainDNA2.Length > 0)
                {
                    CreateBrains(brainDNA2);
                }

                ShipPartDNA fuelDNA2 = allParts2.Where(o => o.PartType == FuelTank.PARTTYPE).FirstOrDefault();		//NOTE: This is too simplistic if part remove/add is allowed in the mutator
                ThrusterDNA[] thrustDNA2 = allParts2.Where(o => o.PartType == Thruster.PARTTYPE).Select(o => (ThrusterDNA)o).ToArray();
                if (thrustDNA2.Length > 0)
                {
                    CreateThrusters(thrustDNA2);
                }

                #endregion

                // Relink
                if (hadLinks)
                {
                    CreateLinks();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearDebugVisuals();
                ClearLinks();
                ClearGravSensors();
                ClearBrains();
                ClearThrusters();
                ClearContainers();

                UpdateCountReport();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private static void ModifyDNA(ShipPartDNA dna, bool randSize, bool randOrientation)
        {
            if (randSize)
            {
                dna.Scale = Math3D.GetRandomVector(new Vector3D(.25, .25, .25), new Vector3D(2.5, 2.5, 2.5));
            }

            if (randOrientation)
            {
                dna.Orientation = Math3D.GetRandomRotation();
            }
        }

        private static void BuildNeuronVisuals(out List<Tuple<INeuron, SolidColorBrush>> outNeurons, out ModelVisual3D model, IEnumerable<INeuron> inNeurons, INeuronContainer container, ItemColors colors)
        {
            outNeurons = new List<Tuple<INeuron, SolidColorBrush>>();

            Model3DGroup geometries = new Model3DGroup();

            int neuronCount = inNeurons.Count();
            double neuronRadius;
            if (neuronCount < 20)
            {
                neuronRadius = .03d;
            }
            else if (neuronCount > 100)
            {
                neuronRadius = .007d;
            }
            else
            {
                neuronRadius = UtilityCore.GetScaledValue(.03d, .007d, 20, 100, neuronCount);
            }

            foreach (INeuron neuron in inNeurons)
            {
                MaterialGroup material = new MaterialGroup();
                SolidColorBrush brush = new SolidColorBrush(neuron.IsPositiveOnly ? colors.Neuron_Zero_ZerPos : colors.Neuron_Zero_NegPos);
                DiffuseMaterial diffuse = new DiffuseMaterial(brush);
                material.Children.Add(diffuse);
                material.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("80C0C0C0")), 50d));

                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = material;
                geometry.BackMaterial = material;
                geometry.Geometry = UtilityWPF.GetSphere_LatLon(2, neuronRadius);
                geometry.Transform = new TranslateTransform3D(neuron.Position.ToVector());

                geometries.Children.Add(geometry);

                outNeurons.Add(new Tuple<INeuron, SolidColorBrush>(neuron, brush));
            }

            model = new ModelVisual3D();
            model.Content = geometries;
            //model.Transform = new TranslateTransform3D(position.ToVector());
            model.Transform = GetContainerTransform(new Dictionary<INeuronContainer, Transform3D>(), container);
        }
        private static void BuildLinkVisual(ref Model3DGroup posLines, ref DiffuseMaterial posDiffuse, ref Model3DGroup negLines, ref DiffuseMaterial negDiffuse, Point3D from, Point3D to, double weight, double[] brainChemicals, ItemColors colors)
        {
            const double GAP = .05d;
            const double CHEMSPACE = .01d;

            double thickness = Math.Abs(weight) * .003d;

            #region Shorten Line

            // Leave a little bit of gap between the from node and the line so the user knows what direction the link is
            Vector3D line = from - to;
            double length = line.Length;
            double newLength = length - GAP;
            if (newLength > length * .75d)		// don't shorten it if it's going to be too small
            {
                line = line.ToUnit() * newLength;
            }
            else
            {
                newLength = length;		// doing this here so the logic below doesn't need an if statement
            }

            Point3D fromActual = to + line;

            #endregion

            GeometryModel3D geometry = null;

            #region Draw Line

            if (weight > 0)
            {
                if (posLines == null)
                {
                    posLines = new Model3DGroup();
                    posDiffuse = new DiffuseMaterial(new SolidColorBrush(colors.Link_Positive));
                }

                geometry = new GeometryModel3D();
                geometry.Material = posDiffuse;
                geometry.BackMaterial = posDiffuse;
                geometry.Geometry = UtilityWPF.GetLine(fromActual, to, thickness);

                posLines.Children.Add(geometry);
            }
            else
            {
                if (negLines == null)
                {
                    negLines = new Model3DGroup();
                    negDiffuse = new DiffuseMaterial(new SolidColorBrush(colors.Link_Negative));
                }

                geometry = new GeometryModel3D();
                geometry.Material = negDiffuse;
                geometry.BackMaterial = negDiffuse;
                geometry.Geometry = UtilityWPF.GetLine(fromActual, to, thickness);

                negLines.Children.Add(geometry);
            }

            #endregion

            if (brainChemicals != null)
            {
                #region Draw Brain Chemicals

                #region Calculations

                double workingLength = newLength - GAP;
                if (Math1D.IsNearValue(length, newLength))
                {
                    // Logic above didn't use a gap, so don't do one here either
                    workingLength = length;
                }

                double totalChemSpace = (brainChemicals.Length - 1) * CHEMSPACE;

                double chemSpace = CHEMSPACE;
                if (totalChemSpace > workingLength)
                {
                    chemSpace = workingLength / (brainChemicals.Length - 1);		// shouldn't get divide by zero
                }

                Vector3D chemOffset = line.ToUnit() * (chemSpace * -1d);

                RotateTransform3D rotTrans = new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRotation(new Vector3D(0, 0, 1), line)));

                #endregion

                // Place the chemicals
                for (int cntr = 0; cntr < brainChemicals.Length; cntr++)
                {
                    Transform3DGroup transform = new Transform3DGroup();
                    transform.Children.Add(rotTrans);
                    transform.Children.Add(new TranslateTransform3D((fromActual + (chemOffset * cntr)).ToVector()));

                    double scale = .0062d * Math.Abs(brainChemicals[cntr]);
                    ScaleTransform3D scaleTransform = new ScaleTransform3D(scale, scale, scale);

                    if (brainChemicals[cntr] > 0)
                    {
                        if (posLines == null)
                        {
                            posLines = new Model3DGroup();
                            posDiffuse = new DiffuseMaterial(new SolidColorBrush(colors.Link_Positive));
                        }

                        geometry = new GeometryModel3D();
                        geometry.Material = posDiffuse;
                        geometry.BackMaterial = posDiffuse;
                        geometry.Geometry = UtilityWPF.GetCircle2D(5, scaleTransform, Transform3D.Identity);
                        geometry.Transform = transform;

                        posLines.Children.Add(geometry);
                    }
                    else
                    {
                        if (negLines == null)
                        {
                            negLines = new Model3DGroup();
                            negDiffuse = new DiffuseMaterial(new SolidColorBrush(colors.Link_Negative));
                        }

                        geometry = new GeometryModel3D();
                        geometry.Material = negDiffuse;
                        geometry.BackMaterial = negDiffuse;
                        geometry.Geometry = UtilityWPF.GetCircle2D(5, scaleTransform, Transform3D.Identity);
                        geometry.Transform = transform;

                        negLines.Children.Add(geometry);
                    }
                }

                #endregion
            }
        }

        private static Transform3D GetContainerTransform(Dictionary<INeuronContainer, Transform3D> existing, INeuronContainer container)
        {
            if (!existing.ContainsKey(container))
            {
                Transform3DGroup transform = new Transform3DGroup();
                transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(container.Orientation)));
                transform.Children.Add(new TranslateTransform3D(container.Position.ToVector()));
                existing.Add(container, transform);
            }

            return existing[container];
        }

        private void ClearGravSensors()
        {
            ClearDebugVisuals();
            ClearLinks();

            if (_gravSensors != null)
            {
                foreach (GravSensorStuff sensor in _gravSensors)
                {
                    _viewport.Children.Remove(sensor.Visual);

                    _viewportNeural.Children.Remove(sensor.NeuronVisual);
                    _viewportNeural.Children.Remove(sensor.Gravity);

                    sensor.Body.Dispose();
                }

                _gravSensors = null;
            }
        }
        private void ClearBrains()
        {
            ClearDebugVisuals();
            ClearLinks();

            if (_brains != null)
            {
                foreach (BrainStuff brain in _brains)
                {
                    _viewport.Children.Remove(brain.Visual);

                    _viewportNeural.Children.Remove(brain.NeuronVisual);

                    brain.Body.Dispose();
                }

                _brains = null;
            }
        }
        private void ClearThrusters()
        {
            ClearDebugVisuals();
            ClearLinks();

            if (_thrusters != null)
            {
                foreach (var thrust in _thrusters)
                {
                    _viewport.Children.Remove(thrust.Visual);
                    thrust.Body.Dispose();

                    _viewportNeural.Children.Remove(thrust.NeuronVisual);
                }

                _thrusters = null;
            }
        }
        private void ClearContainers()
        {
            ClearDebugVisuals();
            ClearLinks();

            if (_containers != null)
            {
                _viewport.Children.Remove(_containers.EnergyVisual);
                _viewport.Children.Remove(_containers.FuelVisual);

                _containers.EnergyBody.Dispose();
                _containers.FuelBody.Dispose();

                _containers = null;
            }
        }
        private void ClearLinks()
        {
            ClearDebugVisuals();
            CancelBrainOperation();

            if (_links != null)
            {
                foreach (Visual3D visual in _links.Visuals)
                {
                    _viewportNeural.Children.Remove(visual);
                }

                _links = null;
            }

            // Reset the neuron values (leave the sensors alone, because they aren't affected by links)
            foreach (var neuron in UtilityCore.Iterate(
                _brains == null ? null : _brains.SelectMany(o => o.Neurons),
                _thrusters == null ? null : _thrusters.SelectMany(o => o.Neurons)))
            {
                neuron.Item1.SetValue(0d);
            }
        }
        private void ClearDebugVisuals()
        {
            foreach (Visual3D visual in _debugVisuals)
            {
                _viewportNeural.Children.Remove(visual);
            }

            _debugVisuals.Clear();
        }

        private void CreateGravSensors(ShipPartDNA[] dna)
        {
            if (_gravSensors != null)
            {
                throw new InvalidOperationException("Existing gravity sensors should have been wiped out before calling CreateGravSensors");
            }

            _gravSensors = new GravSensorStuff[dna.Length];
            for (int cntr = 0; cntr < dna.Length; cntr++)
            {
                _gravSensors[cntr] = new GravSensorStuff();
                _gravSensors[cntr].Sensor = new SensorGravity(_editorOptions, _itemOptions, dna[cntr], _containers == null ? null : _containers.Energy, _gravityField);
                _gravSensors[cntr].Sensor.RequestWorldLocation += new EventHandler<PartRequestWorldLocationArgs>(Sensor_RequestWorldLocation);

                #region Ship Visual

                // WPF
                ModelVisual3D model = new ModelVisual3D();
                model.Content = _gravSensors[cntr].Sensor.Model;
                //TODO: Offset this if there are multiple parts

                _viewport.Children.Add(model);
                _gravSensors[cntr].Visual = model;

                // Physics
                using (CollisionHull hull = _gravSensors[cntr].Sensor.CreateCollisionHull(_world))
                {
                    _gravSensors[cntr].Body = new Body(hull, Matrix3D.Identity, _gravSensors[cntr].Sensor.TotalMass, new Visual3D[] { model });
                    _gravSensors[cntr].Body.MaterialGroupID = _material_Ship;
                    _gravSensors[cntr].Body.LinearDamping = .01f;
                    _gravSensors[cntr].Body.AngularDamping = new Vector3D(.01f, .01f, .01f);
                }

                #endregion
                #region Gravity Visual

                //NOTE: Since the neurons are semitransparent, they need to be added last

                _gravSensors[cntr].Gravity = new ScreenSpaceLines3D();
                _gravSensors[cntr].Gravity.Thickness = 2d;
                _gravSensors[cntr].Gravity.Color = _colors.TrackballAxisMajorColor;

                _viewportNeural.Children.Add(_gravSensors[cntr].Gravity);

                #endregion
                #region Neuron Visuals

                BuildNeuronVisuals(out _gravSensors[cntr].Neurons, out model, _gravSensors[cntr].Sensor.Neruons_All, _gravSensors[cntr].Sensor, _colors);

                _gravSensors[cntr].NeuronVisual = model;
                _viewportNeural.Children.Add(model);

                #endregion
            }

            UpdateGravity();		// this shows the gravity line
            UpdateCountReport();
        }
        private void CreateBrains(ShipPartDNA[] dna)
        {
            if (_brains != null)
            {
                throw new InvalidOperationException("Existing brains should have been wiped out before calling CreateBrains");
            }

            _brains = new BrainStuff[dna.Length];

            for (int cntr = 0; cntr < dna.Length; cntr++)
            {
                _brains[cntr] = new BrainStuff();
                BrainStuff brain = _brains[cntr];
                brain.Brain = new Brain(_editorOptions, _itemOptions, dna[cntr], _containers == null ? null : _containers.Energy);
                brain.DNAInternalLinks = dna[cntr].InternalLinks;
                brain.DNAExternalLinks = dna[cntr].ExternalLinks;

                #region Ship Visual

                // WPF
                ModelVisual3D model = new ModelVisual3D();
                model.Content = brain.Brain.Model;

                _viewport.Children.Add(model);
                brain.Visual = model;

                // Physics
                using (CollisionHull hull = brain.Brain.CreateCollisionHull(_world))
                {
                    brain.Body = new Body(hull, Matrix3D.Identity, brain.Brain.TotalMass, new Visual3D[] { model });
                    brain.Body.MaterialGroupID = _material_Ship;
                    brain.Body.LinearDamping = .01f;
                    brain.Body.AngularDamping = new Vector3D(.01f, .01f, .01f);
                }

                #endregion
                #region Neuron Visuals

                BuildNeuronVisuals(out brain.Neurons, out model, brain.Brain.Neruons_All, brain.Brain, _colors);

                brain.NeuronVisual = model;
                _viewportNeural.Children.Add(model);

                #endregion
            }

            UpdateCountReport();
        }
        private void CreateThrusters(ThrusterDNA[] thrustDNA)
        {
            if (_thrusters != null)
            {
                throw new InvalidOperationException("Existing thrusters should have been wiped out before calling CreateThrusters");
            }

            _thrusters = new ThrusterStuff[thrustDNA.Length];

            // Thrusters
            for (int cntr = 0; cntr < thrustDNA.Length; cntr++)
            {
                _thrusters[cntr] = new ThrusterStuff();

                _thrusters[cntr].Thrust = new Thruster(_editorOptions, _itemOptions, thrustDNA[cntr], _containers == null ? null : _containers.Fuel);

                _thrusters[cntr].DNAExternalLinks = thrustDNA[cntr].ExternalLinks;
            }

            #region Ship Visuals

            foreach (var thrust in _thrusters)
            {
                // WPF
                ModelVisual3D model = new ModelVisual3D();
                model.Content = thrust.Thrust.Model;
                //TODO: Offset this if there are multiple parts

                _viewport.Children.Add(model);
                thrust.Visual = model;

                // Physics
                using (CollisionHull hull = thrust.Thrust.CreateCollisionHull(_world))
                {
                    thrust.Body = new Body(hull, Matrix3D.Identity, thrust.Thrust.TotalMass, new Visual3D[] { model });
                    thrust.Body.MaterialGroupID = _material_Ship;
                    thrust.Body.LinearDamping = .01f;
                    thrust.Body.AngularDamping = new Vector3D(.01f, .01f, .01f);
                }
            }

            #endregion
            #region Neuron Visuals

            foreach (var thrust in _thrusters)
            {
                ModelVisual3D model;
                BuildNeuronVisuals(out thrust.Neurons, out model, thrust.Thrust.Neruons_All, thrust.Thrust, _colors);

                thrust.NeuronVisual = model;
                _viewportNeural.Children.Add(model);
            }

            #endregion

            UpdateCountReport();
        }
        private void CreateContainers()
        {
            const double HEIGHT = .07d;
            const double OFFSET = 1.5d;

            double offset2 = ((.1d - HEIGHT) / 2d) + (HEIGHT / 2d);

            ShipPartDNA dnaEnergy = new ShipPartDNA();
            dnaEnergy.PartType = EnergyTank.PARTTYPE;
            dnaEnergy.Position = new Point3D(OFFSET - offset2, 0, 0);
            dnaEnergy.Orientation = new Quaternion(new Vector3D(0, 1, 0), 90);
            dnaEnergy.Scale = new Vector3D(1.3, 1.3, HEIGHT);		// the energy tank is slightly wider than the fuel tank

            ShipPartDNA dnaFuel = new ShipPartDNA();
            dnaFuel.PartType = FuelTank.PARTTYPE;
            dnaFuel.Position = new Point3D(OFFSET + offset2, 0, 0);
            dnaFuel.Orientation = new Quaternion(new Vector3D(0, 1, 0), 90);
            dnaFuel.Scale = new Vector3D(1.5, 1.5, HEIGHT);

            CreateContainers(dnaEnergy, dnaFuel);
        }
        private void CreateContainers(ShipPartDNA energyDNA, ShipPartDNA fuelDNA)
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

            #region Ship Visuals (energy)

            // WPF
            ModelVisual3D model = new ModelVisual3D();
            model.Content = _containers.Energy.Model;

            _viewport.Children.Add(model);
            _containers.EnergyVisual = model;

            // Physics
            CollisionHull hull = _containers.Energy.CreateCollisionHull(_world);
            _containers.EnergyBody = new Body(hull, Matrix3D.Identity, _containers.Energy.TotalMass, new Visual3D[] { model });
            hull.Dispose();
            _containers.EnergyBody.MaterialGroupID = _material_Ship;
            _containers.EnergyBody.LinearDamping = .01f;
            _containers.EnergyBody.AngularDamping = new Vector3D(.01f, .01f, .01f);

            #endregion
            #region Ship Visuals (fuel)

            // WPF
            model = new ModelVisual3D();
            model.Content = _containers.Fuel.Model;

            _viewport.Children.Add(model);
            _containers.FuelVisual = model;

            // Physics
            hull = _containers.Fuel.CreateCollisionHull(_world);
            _containers.FuelBody = new Body(hull, Matrix3D.Identity, _containers.Fuel.TotalMass, new Visual3D[] { model });
            hull.Dispose();
            _containers.FuelBody.MaterialGroupID = _material_Ship;
            _containers.FuelBody.LinearDamping = .01f;
            _containers.FuelBody.AngularDamping = new Vector3D(.01f, .01f, .01f);

            #endregion
        }
        private void CreateLinks()
        {
            #region Build up input args

            List<NeuralUtility.ContainerInput> inputs = new List<NeuralUtility.ContainerInput>();
            if (_gravSensors != null)
            {
                foreach (GravSensorStuff sensor in _gravSensors)
                {
                    // The sensor is a source, so shouldn't have any links.  But it needs to be included in the args so that other
                    // neuron containers can hook to it
                    inputs.Add(new NeuralUtility.ContainerInput(sensor.Body.Token, sensor.Sensor, NeuronContainerType.Sensor, sensor.Sensor.Position, sensor.Sensor.Orientation, null, null, 0, null, null));
                }
            }

            if (_brains != null)
            {
                foreach (BrainStuff brain in _brains)
                {
                    inputs.Add(new NeuralUtility.ContainerInput(
                        brain.Body.Token,
                        brain.Brain, NeuronContainerType.Brain,
                        brain.Brain.Position, brain.Brain.Orientation,
                        _itemOptions.Brain_LinksPerNeuron_Internal,
                        new Tuple<NeuronContainerType, NeuralUtility.ExternalLinkRatioCalcType, double>[]
							{
								Tuple.Create(NeuronContainerType.Sensor, NeuralUtility.ExternalLinkRatioCalcType.Smallest, _itemOptions.Brain_LinksPerNeuron_External_FromSensor),
								Tuple.Create(NeuronContainerType.Brain, NeuralUtility.ExternalLinkRatioCalcType.Average, _itemOptions.Brain_LinksPerNeuron_External_FromBrain),
								Tuple.Create(NeuronContainerType.Manipulator, NeuralUtility.ExternalLinkRatioCalcType.Smallest, _itemOptions.Brain_LinksPerNeuron_External_FromManipulator)
							},
                        Convert.ToInt32(Math.Round(brain.Brain.BrainChemicalCount * 1.33d, 0)),		// increasing so that there is a higher chance of listeners
                        brain.DNAInternalLinks, brain.DNAExternalLinks));
                }
            }

            if (_thrusters != null)
            {
                foreach (var thrust in _thrusters)
                {
                    //TODO: Consider existing links
                    //NOTE: This won't be fed by other manipulators
                    inputs.Add(new NeuralUtility.ContainerInput(
                        thrust.Body.Token,
                        thrust.Thrust, NeuronContainerType.Manipulator,
                        thrust.Thrust.Position, thrust.Thrust.Orientation,
                        null,
                        new Tuple<NeuronContainerType, NeuralUtility.ExternalLinkRatioCalcType, double>[]
							{
								Tuple.Create(NeuronContainerType.Sensor, NeuralUtility.ExternalLinkRatioCalcType.Destination, _itemOptions.Thruster_LinksPerNeuron_Sensor),
								Tuple.Create(NeuronContainerType.Brain, NeuralUtility.ExternalLinkRatioCalcType.Destination, _itemOptions.Thruster_LinksPerNeuron_Brain),
							},
                        0,
                        null, thrust.DNAExternalLinks));
                }
            }

            #endregion

            // Create links
            NeuralUtility.ContainerOutput[] outputs = null;
            if (inputs.Count > 0)
            {
                outputs = NeuralUtility.LinkNeurons(inputs.ToArray(), _itemOptions.NeuralLink_MaxWeight);
            }

            #region Show new links

            if (outputs != null)
            {
                _links = new LinkStuff();
                _links.Outputs = outputs;
                _links.Visuals = new List<Visual3D>();

                Model3DGroup posLines = null, negLines = null;
                DiffuseMaterial posDiffuse = null, negDiffuse = null;

                Dictionary<INeuronContainer, Transform3D> containerTransforms = new Dictionary<INeuronContainer, Transform3D>();

                foreach (var output in outputs)
                {
                    Transform3D toTransform = GetContainerTransform(containerTransforms, output.Container);

                    foreach (var link in UtilityCore.Iterate(output.InternalLinks, output.ExternalLinks))
                    {
                        Transform3D fromTransform = GetContainerTransform(containerTransforms, link.FromContainer);

                        BuildLinkVisual(ref posLines, ref posDiffuse, ref negLines, ref negDiffuse, fromTransform.Transform(link.From.Position), toTransform.Transform(link.To.Position), link.Weight, link.BrainChemicalModifiers, _colors);
                    }
                }

                if (posLines != null)
                {
                    ModelVisual3D model = new ModelVisual3D();
                    model.Content = posLines;
                    _links.Visuals.Add(model);
                    _viewportNeural.Children.Add(model);
                }

                if (negLines != null)
                {
                    ModelVisual3D model = new ModelVisual3D();
                    model.Content = negLines;
                    _links.Visuals.Add(model);
                    _viewportNeural.Children.Add(model);
                }
            }

            #endregion

            UpdateCountReport();

            if (chkBrainRunning.IsChecked.Value)
            {
                StartBrainOperation();
            }
        }

        private void UpdateGravity()
        {
            if (_gravityOrientationTrackball == null || trkGravityStrength == null)
            {
                return;
            }

            // The trackball's axis is along X
            _gravity = _gravityOrientationTrackball.Transform.Transform(new Vector3D(trkGravityStrength.Value, 0, 0));

            _gravityField.Gravity = _gravity;

            lblCurrentGravity.Text = Math.Round(_gravity.Length, 3).ToString();

            // Show the gravity in neuron space
            if (_gravSensors != null)
            {
                foreach (GravSensorStuff sensor in _gravSensors)
                {
                    sensor.Gravity.Clear();
                    sensor.Gravity.AddLine(sensor.Sensor.Position, sensor.Sensor.Position + _gravity);
                }
            }
        }

        private void UpdateCountReport()
        {
            int numInputs = 0;
            if (_gravSensors != null)
            {
                numInputs = _gravSensors.Sum(o => o.Neurons.Count);
            }

            int numBrains = 0;
            if (_brains != null)
            {
                numBrains = _brains.Sum(o => o.Neurons.Count);
            }

            int numManipulators = 0;
            if (_thrusters != null)
            {
                numManipulators = _thrusters.Sum(o => o.Neurons.Count);
            }

            int numLinks = 0;
            if (_links != null)
            {
                numLinks = _links.Outputs.Sum(o => (o.ExternalLinks == null ? 0 : o.ExternalLinks.Length) + (o.InternalLinks == null ? 0 : o.InternalLinks.Length));
            }

            lblInputNeuronCount.Text = numInputs.ToString("N0");
            lblBrainNeuronCount.Text = numBrains.ToString("N0");
            lblThrustNeuronCount.Text = numManipulators.ToString("N0");
            lblTotalNeuronCount.Text = (numInputs + numBrains + numManipulators).ToString("N0");
            lblTotalLinkCount.Text = numLinks.ToString("N0");
        }

        private void StartBrainOperation()
        {
            // Make sure an existing operation isn't running
            CancelBrainOperation();

            if (_links == null)
            {
                // There are no links, so there is nothing to do
                return;
            }

            // Hand all the links to the worker
            NeuralBucket worker = new NeuralBucket(_links.Outputs.SelectMany(o => UtilityCore.Iterate(o.InternalLinks, o.ExternalLinks)).ToArray());

            _brainOperationCancel = new CancellationTokenSource();

            // Run this on an arbitrary thread
            _brainOperationTask = Task.Factory.StartNew(() =>
            {
                OperateBrain(worker, _brainOperationCancel.Token);
            }, _brainOperationCancel.Token);
        }
        private void CancelBrainOperation()
        {
            if (_brainOperationTask == null)
            {
                // There is nothing to cancel
                return;
            }

            // Initiate a cancel, and block until it's finished (it should finish very quickly)
            _brainOperationCancel.Cancel();
            try
            {
                _brainOperationTask.Wait();
            }
            catch (Exception) { }

            // Clean up
            _brainOperationTask.Dispose();
            _brainOperationCancel.Dispose();
            _brainOperationTask = null;
            _brainOperationCancel = null;
        }
        private static void OperateBrain(NeuralBucket worker, CancellationToken cancel)
        {
            //NOTE: This method is running on an arbitrary thread
            try
            {
                while (!cancel.IsCancellationRequested)
                {
                    worker.Tick();
                }
            }
            catch (Exception)
            {
                // Don't leak errors, just go away
            }
        }

        private void SetupGravityTrackball()
        {
            // major arrow along x
            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = TrackballGrabber.GetMajorArrow(Axis.X, true, _colors.TrackballAxisMajor, _colors.TrackballAxisSpecular);

            _viewportGravityRotate.Children.Add(visual);
            _gravityOrientationVisuals.Add(visual);

            // Create the trackball
            _gravityOrientationTrackball = new TrackballGrabber(grdGravityRotateViewport, _viewportGravityRotate, 1d, _colors.TrackballGrabberHoverLight);
            _gravityOrientationTrackball.SyncedLights.Add(_lightGravity1);
            _gravityOrientationTrackball.RotationChanged += new EventHandler(GravityOrientationTrackball_RotationChanged);

            // Faint lines
            _gravityOrientationTrackball.HoverVisuals.Add(TrackballGrabber.GetGuideLine(Axis.X, false, _colors.TrackballAxisLine));
            _gravityOrientationTrackball.HoverVisuals.Add(TrackballGrabber.GetGuideLineDouble(Axis.Y, _colors.TrackballAxisLine));
            _gravityOrientationTrackball.HoverVisuals.Add(TrackballGrabber.GetGuideLineDouble(Axis.Z, _colors.TrackballAxisLine));

            // The trackball's axis that is showing is X, but I want gravity to start out down
            _gravityOrientationTrackball.Transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), -90));
        }

        #region OLD

        //private void SetupGravityTrackball()
        //{
        //    //NOTE:  When they start with different transforms, the display vectors are screwed up
        //    RotateTransform3D transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 0d));

        //    //NOTE: grdGravityRotateViewport is a stackpanel with only this control it in.  If other controls were in its subtree, they would steal mouseup events
        //    SetupTrackballSprtAddIt(ref _gravityOrientationTrackball, _gravityOrientationVisuals, _viewportGravityRotate, grdGravityRotateViewport, _colors, transform, true, false, false);
        //    _gravityOrientationTrackball.SyncedLights.Add(_lightGravity1);
        //    _gravityOrientationTrackball.RotationChanged += new EventHandler(GravityOrientationTrackball_RotationChanged);

        //    // The trackball's axis that is showing is X, but I want gravity to start out down
        //    _gravityOrientationTrackball.Transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), -90));
        //}
        //private void SetupTrackballSprtAddIt(ref TrackballGrabber trackball, List<ModelVisual3D> visuals, Viewport3D viewport, FrameworkElement eventSource, ItemColors colors, RotateTransform3D modelTransform, bool showX, bool showY, bool showZ)
        //{
        //    MaterialGroup materials = null;
        //    GeometryModel3D geometry = null;
        //    Transform3DGroup transform = null;
        //    ModelVisual3D model = null;

        //    if (showX)
        //    {
        //        #region major arrow along x

        //        // Material
        //        materials = new MaterialGroup();
        //        materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(colors.TrackballAxisX)));
        //        materials.Children.Add(colors.TrackballAxisSpecular);

        //        // Geometry Model
        //        geometry = new GeometryModel3D();
        //        geometry.Material = materials;
        //        geometry.BackMaterial = materials;
        //        geometry.Geometry = UtilityWPF.GetCylinder_AlongX(20, .075, 1d);

        //        transform = new Transform3DGroup();		// rotate needs to be added before translate
        //        transform.Children.Add(new TranslateTransform3D(new Vector3D(.5d, 0, 0)));

        //        geometry.Transform = transform;

        //        // Model Visual
        //        model = new ModelVisual3D();
        //        model.Content = geometry;

        //        // Add it
        //        viewport.Children.Add(model);
        //        visuals.Add(model);

        //        #endregion
        //        #region x line cone

        //        // Material
        //        materials = new MaterialGroup();
        //        materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(colors.TrackballAxisX)));
        //        materials.Children.Add(colors.TrackballAxisSpecular);

        //        // Geometry Model
        //        geometry = new GeometryModel3D();
        //        geometry.Material = materials;
        //        geometry.BackMaterial = materials;
        //        geometry.Geometry = UtilityWPF.GetCone_AlongX(10, .15d, .3d);

        //        transform = new Transform3DGroup();		// rotate needs to be added before translate
        //        transform.Children.Add(new TranslateTransform3D(new Vector3D(1d + .1d, 0, 0)));

        //        geometry.Transform = transform;

        //        // Model Visual
        //        model = new ModelVisual3D();
        //        model.Content = geometry;

        //        // Add it
        //        viewport.Children.Add(model);
        //        visuals.Add(model);

        //        #endregion
        //        #region x line cap

        //        // Material
        //        materials = new MaterialGroup();
        //        materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(colors.TrackballAxisX)));
        //        materials.Children.Add(colors.TrackballAxisSpecular);

        //        // Geometry Model
        //        geometry = new GeometryModel3D();
        //        geometry.Material = materials;
        //        geometry.BackMaterial = materials;
        //        geometry.Geometry = UtilityWPF.GetSphere(20, .075d);

        //        // Model Visual
        //        model = new ModelVisual3D();
        //        model.Content = geometry;

        //        // Add it
        //        viewport.Children.Add(model);
        //        visuals.Add(model);

        //        #endregion
        //    }

        //    if (showZ)
        //    {
        //        #region minor arrow along z

        //        // Material
        //        materials = new MaterialGroup();
        //        materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(colors.TrackballAxisZ)));
        //        materials.Children.Add(colors.TrackballAxisSpecular);

        //        // Geometry Model
        //        geometry = new GeometryModel3D();
        //        geometry.Material = materials;
        //        geometry.BackMaterial = materials;
        //        geometry.Geometry = UtilityWPF.GetCylinder_AlongX(10, .05, .5d);

        //        transform = new Transform3DGroup();		// rotate needs to be added before translate
        //        transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, -1, 0), 90d)));
        //        transform.Children.Add(new TranslateTransform3D(new Vector3D(0, 0, .25d)));

        //        geometry.Transform = transform;

        //        // Model Visual
        //        model = new ModelVisual3D();
        //        model.Content = geometry;

        //        // Add it
        //        viewport.Children.Add(model);
        //        visuals.Add(model);

        //        #endregion
        //        #region z line cone

        //        // Material
        //        materials = new MaterialGroup();
        //        materials.Children.Add(new DiffuseMaterial(new SolidColorBrush(colors.TrackballAxisZ)));
        //        materials.Children.Add(colors.TrackballAxisSpecular);

        //        // Geometry Model
        //        geometry = new GeometryModel3D();
        //        geometry.Material = materials;
        //        geometry.BackMaterial = materials;
        //        geometry.Geometry = UtilityWPF.GetCone_AlongX(10, .075d, .2d);

        //        transform = new Transform3DGroup();		// rotate needs to be added before translate
        //        transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, -1, 0), 90d)));
        //        transform.Children.Add(new TranslateTransform3D(new Vector3D(0, 0, .5d + .1d)));

        //        geometry.Transform = transform;

        //        // Model Visual
        //        model = new ModelVisual3D();
        //        model.Content = geometry;

        //        // Add it
        //        viewport.Children.Add(model);
        //        visuals.Add(model);

        //        #endregion
        //    }

        //    if (showY)
        //    {
        //        #region faint line along y

        //        ScreenSpaceLines3D line = new ScreenSpaceLines3D(true);
        //        line.Thickness = 1d;
        //        line.Color = colors.TrackballAxisY;
        //        line.AddLine(new Point3D(0, -.75d, 0), new Point3D(0, .75d, 0));

        //        // Add it
        //        viewport.Children.Add(line);
        //        visuals.Add(line);

        //        #endregion
        //    }

        //    trackball = new TrackballGrabber(eventSource, viewport, 1d, colors.TrackballGrabberHoverLight);
        //    trackball.Transform = modelTransform;
        //}

        #endregion

        /// <summary>
        /// There's just enough logic here, that I wanted it in a private method.  The isInitialized check is a bit of a hack, that should be done
        /// by the caller, but it reduces total code
        /// </summary>
        private static double? UpdateSetting(bool isInitialized, TextBox textbox)
        {
            if (!isInitialized)
            {
                return null;
            }

            double? retVal = null;

            double newValue;
            if (double.TryParse(textbox.Text, out newValue))
            {
                retVal = newValue;
                textbox.Foreground = SystemColors.WindowTextBrush;
            }
            else
            {
                textbox.Foreground = Brushes.Red;
            }

            return retVal;
        }

        #endregion
    }
}
