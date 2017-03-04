using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace Game.Newt.Testers
{
    /// <summary>
    /// This is meant to be more of a proof of concept than any kind of real tester
    /// </summary>
    public partial class NeuralTester : Window
    {
        #region Class: Neuron

        private class Neuron //: INeuron
        {
            public Neuron(Point3D position, ModelVisual3D visual, DiffuseMaterial material)
            {
                _position = position;
                this.Visual = visual;
                this.Material = material;
                this.Value = 0d;
            }

            //#region INeuron Members

            public double Value
            {
                get;
                private set;
            }

            private readonly Point3D _position;
            public Point3D Position
            {
                get
                {
                    return _position;
                }
            }

            public void SetValue(double sumInputs)
            {
                this.Value = Transform_S_NegPos(sumInputs);
            }

            //#endregion

            public readonly ModelVisual3D Visual;
            public readonly DiffuseMaterial Material;

            #region Private Methods

            private static double Transform_LinearCapped_NegPos(double sumInputs)
            {
                if (sumInputs < -1d)
                {
                    return -1d;
                }
                else if (sumInputs > 1d)
                {
                    return 1d;
                }
                else
                {
                    return sumInputs;
                }
            }
            private static double Transform_LinearCapped_ZeroPos(double sumInputs)
            {
                if (sumInputs < 0d)
                {
                    return 0d;
                }
                else if (sumInputs > 1d)
                {
                    return 1d;
                }
                else
                {
                    return sumInputs;
                }
            }
            private static double Transform_S_NegPos(double sumInputs)
            {
                // This returns an S curve with asymptotes at -1 and 1
                double e2x = Math.Pow(Math.E, 2d * sumInputs);
                return (e2x - 1d) / (e2x + 1d);
            }
            private static double Transform_S_ZeroPos(double sumInputs)
            {
                // This returns an S curve with asymptotes at 0 and 1
                return 1d / (1d + Math.Pow(Math.E, -1d * sumInputs));
            }

            #endregion
        }

        #endregion
        #region Class: NeuronLink

        //TODO: Having links store an index doesn't allow for dynamically changing brains
        //TODO: Storing the weight as readonly doesn't allow for dynamically changing brains
        //TODO: Representing the link as a line doesn't allow for neurons to point to themselves
        //TODO: Add properties that allow current brain chemicals to influence the final weight (emulates dopamine, seratonin, etc) - NOTE: This has been implemented in the real NeuralLink class
        private class NeuronLink
        {
            public NeuronLink(bool from_IsInput, int from_Index, bool to_IsOutput, int to_Index, double weight, ScreenSpaceLines3D visual)
            {
                this.From_IsInput = from_IsInput;
                this.From_Index = from_Index;
                this.To_IsOutput = to_IsOutput;
                this.To_Index = to_Index;
                this.Weight = weight;
                this.Visual = visual;
                this.Value = 0d;
            }

            public readonly bool From_IsInput;
            public readonly int From_Index;

            public readonly bool To_IsOutput;
            public readonly int To_Index;

            public readonly double Weight;

            public readonly ScreenSpaceLines3D Visual;

            public double Value;
        }

        #endregion
        #region Class: NeuronBackPointer

        /// <summary>
        /// This holds a reference to a neuron, and all the neurons that feed it
        /// </summary>
        /// <remarks>
        /// I'm hesitant to store links directly in the neurons themselves.  Hopefully, this will make it easier to have shifting
        /// links/weights over time (learning)
        /// 
        /// This will allow for an array of these backpointer instances to be created for a small period of time, it could also
        /// allow the neurons to be processed in parallel
        /// 
        /// This may turn out to be a bad design though, time will tell.
        /// </remarks>
        private class NeuronBackPointer
        {
            public NeuronBackPointer(Neuron neuron, Neuron[] inputs, NeuronLink[] links)
            {
                this.Neuron = neuron;
                this.Inputs = inputs;
                this.Links = links;
            }

            public readonly Neuron Neuron;

            // These arrays are the same size (may want a single array of tuples, but that seems like a lot of unnessassary tuple instances created)
            public readonly Neuron[] Inputs;
            public readonly NeuronLink[] Links;
        }

        #endregion

        #region Declaration Section

        private const double RADIUS_IO = 10d;
        private const double RADIUS_INNER = 7d;
        private const double RADIUS_OUTER = 15d;

        private const double NODESIZE = .5d;

        private const double FACTOR_IO = .5d;
        private const double FACTOR_NEURON = .2d;

        private const double MAXWEIGHT = 2d;

        private const string COLOR_NEGATIVE = "FC0300";
        private const string COLOR_POSITIVE = "00B237";
        private const string COLOR_ZERO = "808080";

        /// <summary>
        /// This listens to the mouse/keyboard and controls the camera
        /// </summary>
        private TrackBallRoam _trackball = null;

        private Neuron[] _inputs = null;
        private Neuron[] _outputs = null;

        private Neuron[] _neurons = null;

        private NeuronLink[] _links = null;

        private NeuronBackPointer[] _backLinks = null;

        #endregion

        #region Constructor

        public NeuralTester()
        {
            InitializeComponent();

            #region Trackball

            // Trackball
            _trackball = new TrackBallRoam(_camera);
            _trackball.KeyPanScale = 1d;
            _trackball.EventSource = grdViewPort;		//NOTE:  If this control doesn't have a background color set, the trackball won't see events (I think transparent is ok, just not null)
            _trackball.AllowZoomOnMouseWheel = true;
            _trackball.Mappings.AddRange(TrackBallMapping.GetPrebuilt(TrackBallMapping.PrebuiltMapping.MouseComplete));
            //_trackball.GetOrbitRadius += new GetOrbitRadiusHandler(Trackball_GetOrbitRadius);
            _trackball.ShouldHitTestOnOrbit = false;

            #endregion
        }

        #endregion

        #region Event Listeners

        private void trkInput_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                TransferInputValues();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnPlaceIO_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                #region Wipe Existing

                foreach (Neuron node in UtilityCore.Iterate(_inputs, _outputs, _neurons))		// the iterate method will skip null arrays
                {
                    _viewport.Children.Remove(node.Visual);
                }

                if (_links != null)
                {
                    foreach (NeuronLink link in _links)
                    {
                        _viewport.Children.Remove(link.Visual);
                    }
                }

                _inputs = null;
                _outputs = null;
                _neurons = null;
                _links = null;
                _backLinks = null;

                #endregion

                #region Calculate Positions

                Point3D[] inputPos, outputPos;

                if (radPlates.IsChecked.Value)
                {
                    inputPos = GetPlatePlacement(4, RADIUS_IO, RADIUS_IO, FACTOR_IO);
                    outputPos = GetPlatePlacement(3, RADIUS_IO, -RADIUS_IO, FACTOR_IO);
                }
                else if (radRandom.IsChecked.Value)
                {
                    Point3D[] positions = GetSpherePlacement(7, RADIUS_IO, FACTOR_IO, null);

                    inputPos = Enumerable.Range(0, 4).Select(o => positions[o]).ToArray();
                    outputPos = Enumerable.Range(4, 3).Select(o => positions[o]).ToArray();
                }
                else
                {
                    MessageBox.Show("Unknown Placement Type", this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                #endregion

                _inputs = BuildIO(inputPos, true);
                _outputs = BuildIO(outputPos, false);

                TransferInputValues();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnPlaceNet_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                #region Cast Ints

                int neuronCount;
                if (!int.TryParse(txtNumNeurons.Text, out neuronCount))
                {
                    MessageBox.Show("Couldn't cast neuron count as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int inputLinkCount;
                if (!int.TryParse(txtNumLinksInput.Text, out inputLinkCount))
                {
                    MessageBox.Show("Couldn't cast number of input links as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int internalLinkCount;
                if (!int.TryParse(txtNumLinksInternal.Text, out internalLinkCount))
                {
                    MessageBox.Show("Couldn't cast number of internal links as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int outputLinkCount;
                if (!int.TryParse(txtNumLinksOutput.Text, out outputLinkCount))
                {
                    MessageBox.Show("Couldn't cast number of output links as an integer", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                #endregion

                #region Wipe Existing

                if (_neurons != null)
                {
                    foreach (Neuron node in _neurons)
                    {
                        _viewport.Children.Remove(node.Visual);
                    }
                }

                if (_links != null)
                {
                    foreach (NeuronLink link in _links)
                    {
                        _viewport.Children.Remove(link.Visual);
                    }
                }

                _neurons = null;
                _links = null;
                _backLinks = null;

                #endregion

                // Make sure the inputs/outputs are placed
                if (_inputs == null)
                {
                    btnPlaceIO_Click(this, new RoutedEventArgs());
                }

                #region Calculate Positions

                Point3D[] positions = null;
                Point3D[] existing = UtilityCore.Iterate(_inputs, _outputs).Select(o => o.Position).ToArray();

                if (radInsideBall.IsChecked.Value)
                {
                    positions = GetSpherePlacement(neuronCount, RADIUS_INNER, FACTOR_NEURON, radRandom.IsChecked.Value ? existing : null);
                }
                else if (radInsideShell.IsChecked.Value)
                {
                    positions = GetShellPlacement(neuronCount, RADIUS_INNER, FACTOR_NEURON);
                }
                else if (radOutsideBall.IsChecked.Value)
                {
                    positions = GetSpherePlacement(neuronCount, RADIUS_OUTER, FACTOR_NEURON, existing);
                }
                else if (radOutsideShell.IsChecked.Value)
                {
                    positions = GetShellPlacement(neuronCount, RADIUS_OUTER, FACTOR_NEURON);
                }
                else
                {
                    MessageBox.Show("Unknown neuron placement type", this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                #endregion

                _neurons = BuildNeurons(positions);

                if (radLinkRandom.IsChecked.Value)
                {
                    _links = BuildLinksRandom(_neurons, _inputs, _outputs, inputLinkCount, internalLinkCount, outputLinkCount);
                }
                else if (radLinkProximity.IsChecked.Value)
                {
                    MessageBox.Show("finish proximity placement", this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                else
                {
                    MessageBox.Show("Unknown link placement type", this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnAdvance_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Make sure a network is set up
                if (_links == null)
                {
                    btnPlaceNet_Click(null, new RoutedEventArgs());
                    if (_links == null)
                    {
                        return;
                    }
                }

                if (_backLinks == null)
                {
                    _backLinks = BuildBackLinks(_inputs, _neurons, _outputs, _links);
                }

                // Add up the weights
                double[] newWeights = new double[_backLinks.Length];
                for (int cntr = 0; cntr < _backLinks.Length; cntr++)
                {
                    double newWeight = 0;
                    for (int inner = 0; inner < _backLinks[cntr].Inputs.Length; inner++)
                    {
                        newWeight += _backLinks[cntr].Inputs[inner].Value * _backLinks[cntr].Links[inner].Weight;
                    }

                    newWeights[cntr] = newWeight;
                }

                // Apply those weights
                for (int cntr = 0; cntr < _backLinks.Length; cntr++)
                {
                    _backLinks[cntr].Neuron.SetValue(newWeights[cntr]);
                    _backLinks[cntr].Neuron.Material.Color = GetWeightedColor(_backLinks[cntr].Neuron.Value);
                }

                trkOutput1.Value = _outputs[0].Value;
                trkOutput2.Value = _outputs[1].Value;
                trkOutput3.Value = _outputs[2].Value;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private static Point3D[] GetPlatePlacement(int count, double radius, double z, double factor)
        {
            // Calculate min distance
            double area = Math.PI * radius * radius;
            area *= factor;		// give some slack (factor needs to be less than 1, the closer they're allowed to be together)
            area /= count;		// the sum of the little areas can't exceed the total area

            double minDistanceSquared = area / Math.PI;		// run the area equation backward to get the radius

            // Exit Function
            return GetPlacements(count, minDistanceSquared, null, new Func<Vector3D>(delegate()
                {
                    Vector3D vector = Math3D.GetRandomVector_Circular(radius);
                    return new Vector3D(vector.X, vector.Y, z);
                }));
        }
        private static Point3D[] GetShellPlacement(int count, double radius, double factor)
        {
            // Calculate min distance
            double surfaceArea = 4d * Math.PI * radius * radius;
            surfaceArea *= factor;		// give some slack (factor needs to be less than 1, the closer they're allowed to be together)
            surfaceArea /= count;		// the sum of the little areas can't exceed the total area

            double minDistanceSquared = surfaceArea / (4d * Math.PI);		// run the surface area equation backward to get the radius

            // Exit Function
            return GetPlacements(count, minDistanceSquared, null, new Func<Vector3D>(delegate()
            {
                return Math3D.GetRandomVector_Spherical_Shell(radius);
            }));
        }
        private static Point3D[] GetSpherePlacement(int count, double radius, double factor, Point3D[] existing)
        {
            // Calculate min distance
            double fourThirds = 4d / 3d;
            double volume = fourThirds * Math.PI * radius * radius * radius;
            volume *= factor;		// give some slack (factor needs to be less than 1, the smaller it is, the closer they're allowed to be together)
            volume /= count + (existing == null ? 0 : existing.Length);		// the sum of the little volumes can't exceed the total volume

            double minDistance = Math.Pow(volume / (fourThirds * Math.PI), 1d / 3d);		// run the volume equation backward to get the radius

            // Convert to a vector array
            Vector3D[] existingVectors = null;
            if (existing != null)
            {
                existingVectors = existing.Select(o => o.ToVector()).ToArray();
            }

            // Use this method instead.  It should be a bit more efficient
            return Math3D.GetRandomVectors_Spherical_ClusteredMinDist(count, radius, minDistance, .001d, 1000, null, existingVectors, null).Select(o => o.ToPoint()).ToArray();
        }
        private static Point3D[] GetSpherePlacement_ORIG(int count, double radius, double factor, Point3D[] existing)
        {
            // Calculate min distance
            double fourThirds = 4d / 3d;
            double volume = fourThirds * Math.PI * radius * radius * radius;
            volume *= factor;		// give some slack (factor needs to be less than 1, the smaller it is, the closer they're allowed to be together)
            volume /= count + (existing == null ? 0 : existing.Length);		// the sum of the little volumes can't exceed the total volume

            double minDistanceSquared = Math.Pow(volume / (fourThirds * Math.PI), 1d / 3d);		// run the volume equation backward to get the radius
            minDistanceSquared = minDistanceSquared * minDistanceSquared;

            // Exit Function
            return GetPlacements(count, minDistanceSquared, existing, new Func<Vector3D>(delegate()
            {
                return Math3D.GetRandomVector_Spherical(radius);
            }));
        }
        private static Point3D[] GetPlacements(int count, double minDistanceSquared, Point3D[] existing, Func<Vector3D> getRandVector)
        {
            Vector3D[] existingCast = existing == null ? new Vector3D[0] : existing.Select(o => o.ToVector()).ToArray();

            List<Vector3D> retVal = new List<Vector3D>();

            for (int cntr = 0; cntr < count; cntr++)
            {
                while (true)
                {
                    // Ask the delegate for a random vector
                    Vector3D test = getRandVector();

                    // Make sure this isn't too close to the other items
                    bool wasTooClose = false;

                    foreach (Vector3D prev in UtilityCore.Iterate(existingCast, retVal))
                    {
                        double distance = (test - prev).LengthSquared;
                        if (distance < minDistanceSquared)
                        {
                            wasTooClose = true;
                            break;
                        }
                    }

                    if (!wasTooClose)
                    {
                        retVal.Add(test);
                        break;
                    }
                }
            }

            // Exit Function
            return retVal.Select(o => o.ToPoint()).ToArray();
        }

        private static Point3D[] GetSpherePlacement2(int count, double radius, Point3D[] existing)
        {
            Vector3D[] retVal = new Vector3D[count];

            // Place them randomly
            for (int cntr = 0; cntr < count; cntr++)
            {
                retVal[cntr] = Math3D.GetRandomVector_Spherical(radius);
            }

            //int existingLength = existing == null ? 0 : existing.Length;

            //if (count + existingLength < 50)
            //{
            //}
            //else
            //{
            //    //TODO: Use a quadtree
            //    throw new ApplicationException("finish this");
            //}

            // Exit Function
            return retVal.Select(o => o.ToPoint()).ToArray();
        }
        private static void GetSpherePlacement2Links(Vector3D[] points, Point3D[] existing)
        {
            // Build links between all the points




            // Move the points

            // Move the one that is farthest from the center first?




        }

        private Neuron[] BuildIO(Point3D[] positions, bool isInput)
        {
            Neuron[] retVal = new Neuron[positions.Length];

            for (int cntr = 0; cntr < positions.Length; cntr++)
            {
                // Material
                MaterialGroup materials = new MaterialGroup();
                DiffuseMaterial bodyMaterial = new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex(COLOR_ZERO)));
                materials.Children.Add(bodyMaterial);
                materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("C0C0C0")), 75d));

                // Geometry Model
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = materials;
                geometry.BackMaterial = materials;

                if (isInput)
                {
                    geometry.Geometry = UtilityWPF.GetCone_AlongX(20, NODESIZE, NODESIZE);
                }
                else
                {
                    double halfSize = NODESIZE / 2d;
                    geometry.Geometry = UtilityWPF.GetCube_IndependentFaces(new Point3D(-halfSize, -halfSize, -halfSize), new Point3D(halfSize, halfSize, halfSize));
                }

                Transform3DGroup transform = new Transform3DGroup();
                transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRandomRotation())));
                transform.Children.Add(new TranslateTransform3D(positions[cntr].ToVector()));

                // Model Visual
                ModelVisual3D visual = new ModelVisual3D();
                visual.Content = geometry;
                visual.Transform = transform;

                // Store it
                retVal[cntr] = new Neuron(positions[cntr], visual, bodyMaterial);
                _viewport.Children.Add(visual);
            }

            return retVal;
        }
        private Neuron[] BuildNeurons(Point3D[] positions)
        {
            Neuron[] retVal = new Neuron[positions.Length];

            for (int cntr = 0; cntr < positions.Length; cntr++)
            {
                // Material
                MaterialGroup materials = new MaterialGroup();
                DiffuseMaterial bodyMaterial = new DiffuseMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex(COLOR_ZERO)));
                materials.Children.Add(bodyMaterial);
                materials.Children.Add(new SpecularMaterial(new SolidColorBrush(UtilityWPF.ColorFromHex("C0C0C0")), 75d));

                // Geometry Model
                GeometryModel3D geometry = new GeometryModel3D();
                geometry.Material = materials;
                geometry.BackMaterial = materials;
                geometry.Geometry = UtilityWPF.GetSphere_LatLon(20, NODESIZE);

                Transform3DGroup transform = new Transform3DGroup();
                transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(Math3D.GetRandomRotation())));
                transform.Children.Add(new TranslateTransform3D(positions[cntr].ToVector()));

                // Model Visual
                ModelVisual3D visual = new ModelVisual3D();
                visual.Content = geometry;
                visual.Transform = transform;

                // Store it
                retVal[cntr] = new Neuron(positions[cntr], visual, bodyMaterial);
                _viewport.Children.Add(visual);
            }

            return retVal;
        }

        private NeuronLink[] BuildLinksRandom(Neuron[] neurons, Neuron[] inputs, Neuron[] outputs, int inputCount, int internalCount, int outputCount)
        {
            //TODO: Make another overload that favors close together nodes

            List<NeuronLink> retVal = new List<NeuronLink>();

            // Input -> Internal
            foreach (Tuple<int, int, double> link in BuildLinksRandomSprtLinks(inputs.Length, neurons.Length, inputCount, false))
            {
                ScreenSpaceLines3D line = BuildLinksSprtLine(inputs[link.Item1].Position, neurons[link.Item2].Position, link.Item3);
                retVal.Add(new NeuronLink(true, link.Item1, false, link.Item2, link.Item3, line));
            }

            // Internal -> Internal
            foreach (Tuple<int, int, double> link in BuildLinksRandomSprtLinks(neurons.Length, neurons.Length, internalCount, true))
            {
                ScreenSpaceLines3D line = BuildLinksSprtLine(neurons[link.Item1].Position, neurons[link.Item2].Position, link.Item3);
                retVal.Add(new NeuronLink(false, link.Item1, false, link.Item2, link.Item3, line));
            }

            // Internal -> Output
            foreach (Tuple<int, int, double> link in BuildLinksRandomSprtLinks(neurons.Length, outputs.Length, outputCount, false))
            {
                ScreenSpaceLines3D line = BuildLinksSprtLine(neurons[link.Item1].Position, outputs[link.Item2].Position, link.Item3);
                retVal.Add(new NeuronLink(false, link.Item1, true, link.Item2, link.Item3, line));
            }

            // Add to viewport
            foreach (NeuronLink link in retVal)
            {
                _viewport.Children.Add(link.Visual);
            }

            // Exit Function
            return retVal.ToArray();
        }
        private static Tuple<int, int, double>[] BuildLinksRandomSprtLinks(int fromCount, int toCount, int count, bool isSameList)
        {
            SortedList<Tuple<int, int>, double> retVal = new SortedList<Tuple<int, int>, double>();

            Random rand = StaticRandom.GetRandomForThread();

            int fromIndex, toIndex;
            double weight;

            // Come up with links
            for (int cntr = 0; cntr < count; cntr++)
            {
                fromIndex = rand.Next(fromCount);

                if (isSameList)
                {
                    toIndex = rand.Next(toCount - 1);		// a neuron can't point to itself, so pick from one less than the size of the list
                    if (toIndex >= fromIndex)
                    {
                        toIndex++;
                    }
                }
                else
                {
                    toIndex = rand.Next(toCount);
                }

                weight = Math1D.GetNearZeroValue(MAXWEIGHT);

                Tuple<int, int> key = new Tuple<int, int>(fromIndex, toIndex);

                // Add to the return (don't allow dupes)
                if (retVal.ContainsKey(key))
                {
                    retVal[key] += weight;
                }
                else
                {
                    retVal.Add(key, weight);
                }
            }

            // Exit Function
            return retVal.Keys.Select(o => new Tuple<int, int, double>(o.Item1, o.Item2, retVal[o])).ToArray();
        }
        private ScreenSpaceLines3D BuildLinksSprtLine(Point3D from, Point3D to, double weight)
        {
            ScreenSpaceLines3D retVal = new ScreenSpaceLines3D();
            if (weight > 0)
            {
                retVal.Color = UtilityWPF.ColorFromHex(COLOR_POSITIVE);
            }
            else
            {
                retVal.Color = UtilityWPF.ColorFromHex(COLOR_NEGATIVE);
            }
            retVal.Thickness = Math.Abs(weight) * 2d;
            retVal.AddLine(from, to);

            return retVal;
        }

        private static NeuronBackPointer[] BuildBackLinks(Neuron[] inputs, Neuron[] neurons, Neuron[] outputs, NeuronLink[] links)
        {
            List<NeuronBackPointer> retVal = new List<NeuronBackPointer>();

            // Split the list apart by To (and get all links by each To neuron)
            var neuronLinks = links.Where(o => !o.To_IsOutput).GroupBy(o => o.To_Index).ToArray();
            var outputLinks = links.Where(o => o.To_IsOutput).GroupBy(o => o.To_Index).ToArray();

            // Build return items
            foreach (var link in UtilityCore.Iterate(neuronLinks, outputLinks))
            {
                retVal.Add(new NeuronBackPointer(
                    link.First().To_IsOutput ? outputs[link.Key] : neurons[link.Key],
                    link.Select(o => o.From_IsInput ? inputs[o.From_Index] : neurons[o.From_Index]).ToArray(),
                    link.ToArray()));
            }

            // Exit Function
            return retVal.ToArray();
        }

        private void TransferInputValues()
        {
            if (_inputs == null)
            {
                return;
            }

            // Apply input weights
            _inputs[0].SetValue(trkInput1.Value);
            _inputs[1].SetValue(trkInput2.Value);
            _inputs[2].SetValue(trkInput3.Value);
            _inputs[3].SetValue(trkInput4.Value);

            foreach (Neuron neruon in _inputs)
            {
                neruon.Material.Color = GetWeightedColor(neruon.Value);
            }
        }

        private static Color GetWeightedColor(double weight)
        {
            Color zero = UtilityWPF.ColorFromHex(COLOR_ZERO);

            if (weight > 0d)
            {
                Color positive = UtilityWPF.ColorFromHex(COLOR_POSITIVE);
                return UtilityWPF.AlphaBlend(positive, zero, weight);
            }
            else
            {
                Color negative = UtilityWPF.ColorFromHex(COLOR_NEGATIVE);
                return UtilityWPF.AlphaBlend(negative, zero, Math.Abs(weight));
            }
        }

        #endregion
    }
}
