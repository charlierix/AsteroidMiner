using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Threading;
using System.Xaml;
using System.Xml;
using Game.HelperClassesAI;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.GameItems.ShipParts;
using SharpNeat.Core;
using SharpNeat.Decoders.HyperNeat;
using SharpNeat.Domains;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.EvolutionAlgorithms.ComplexityRegulation;
using SharpNeat.Genomes.Neat;
using SharpNeat.Network;
using SharpNeat.Phenomes;

namespace Game.Newt.Testers.NEAT
{
    // Improvement Thoughts:
    //      Instead of training with random new positions, train a net with a fixed animation (leave a random duration of empty space between playbacks, so it doesn't memorize the resets)
    //      Once a few nets are trained against different animations, train a single net with those animations, drawing from the specific nets
    //
    //      Penalize false positives more than false negatives
    //
    //      The harness should have a dead time where it's not showing anything
    //
    //      Try hyperneat by starting with low resolution, then increase resolution once the average score is good

    public partial class AnticipatePositionWindow : Window
    {
        #region Declaration Section

        // These two always get created at the same time
        private TrackedItemHarness _harness = null;
        private HarnessArgs _harnessArgs = null;
        private EvaluatorArgs _evalArgs = null;

        private DispatcherTimer _timer = null;
        private int _tickCounter = -1;

        private DateTime _prevTick = DateTime.UtcNow;

        private ExperimentInitArgs _experimentArgs = null;
        private HyperNEAT_Args _hyperneatArgs = null;
        private ExperimentNEATBase _experiment = null;
        private NeatEvolutionAlgorithm<NeatGenome> _ea = null;
        DateTime _winningBrainTime = DateTime.UtcNow;
        private IBlackBox _winningBrain = null;

        private NeatGenomeView _genomeViewer = null;
        private NeatGenomeView _genomeViewer2 = null;

        #endregion

        #region Constructor

        public AnticipatePositionWindow()
        {
            InitializeComponent();

            this.Background = SystemColors.ControlBrush;

            cboTrackedItemType.Items.Add(TrackedItemType.Streaker);
            //cboTrackedItemType.Items.Add(TrackedItemType.Polygon);
            //cboTrackedItemType.Items.Add(TrackedItemType.Elliptical);
            //cboTrackedItemType.Items.Add(TrackedItemType.Brownian);
            cboTrackedItemType.SelectedValue = TrackedItemType.Streaker;

            cboErrorBias.Items.Add(ScoreLeftRightBias.Standard);
            cboErrorBias.Items.Add(ScoreLeftRightBias.PenalizeFalsePositives_Weak);
            cboErrorBias.Items.Add(ScoreLeftRightBias.PenalizeFalsePositives_Medium);
            cboErrorBias.Items.Add(ScoreLeftRightBias.PenalizeFalsePositives_Strong);
            cboErrorBias.Items.Add(ScoreLeftRightBias.PenalizeFalseNegatives);
            cboErrorBias.SelectedValue = ScoreLeftRightBias.PenalizeFalsePositives_Weak;

            _timer = new DispatcherTimer();
            //_timer.Interval = TimeSpan.FromMilliseconds(350);
            _timer.Interval = TimeSpan.FromMilliseconds(35);
            _timer.Tick += Timer_Tick;
            _timer.IsEnabled = true;
        }

        #endregion

        #region Event Listeners

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (_timer != null)
                {
                    _timer.Stop();
                }

                if (_ea != null)
                {
                    _ea.Dispose();
                    _ea = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Timer_Tick_REAL(object sender, EventArgs e)
        {
            // This uses actual time elapsed, and runs continuously, grabbing a new neural net every 10 seconds

            try
            {
                imageInput.Source = null;
                canvasMain.Children.Clear();

                DateTime now = DateTime.UtcNow;

                if (_harness == null)
                {
                    _prevTick = now;
                    return;
                }

                // Tell the harness to go
                _harness.Tick((now - _prevTick).TotalSeconds);

                var prevPosition = _harness.GetPreviousPosition(_harness.Time - _evalArgs.Delay_Seconds);
                var currentPosition = _harness.Item;

                if (_experiment != null && (_winningBrain == null || (now - _winningBrainTime).TotalSeconds > 10))
                {
                    NeatGenome winner = _ea.CurrentChampGenome;
                    _genomeViewer.RefreshView(winner);
                    _winningBrain = _experiment.GetBlackBox(winner);
                    _winningBrainTime = now;
                }

                #region draw input

                double[] inputArr = new double[_harness.InputSizeXY * _harness.InputSizeXY];

                AntPos_Evaluator.ClearArray(inputArr);

                if (prevPosition != null)
                {
                    double dotRadius = (_harness.VisionSize / _harness.InputSizeXY) * Math.Sqrt(2);
                    AntPos_Evaluator.ApplyPoint(inputArr, _harness.InputCellCenters, dotRadius, prevPosition.Item2, true);
                }

                imageInput.Source = UtilityWPF.GetBitmap(inputArr, _harness.InputSizeXY, _harness.InputSizeXY, invert: true);

                #endregion
                #region draw expected output

                double[] expectedOutput = null;

                if (currentPosition == null)
                {
                    expectedOutput = AntPos_Evaluator.GetExpectedOutput(null, _harness, _evalArgs);
                }
                else
                {
                    var currentPos = Tuple.Create(currentPosition, currentPosition.Position, currentPosition.Velocity);
                    expectedOutput = AntPos_Evaluator.GetExpectedOutput(currentPos, _harness, _evalArgs);
                }

                imageExpectedOutput.Source = UtilityWPF.GetBitmap(expectedOutput, _harness.OutputSizeXY, _harness.OutputSizeXY, invert: true);

                #endregion
                #region draw nn output

                double[] nnOutput = null;

                if (_winningBrain != null)
                {
                    nnOutput = new double[_harness.OutputSizeXY * _harness.OutputSizeXY];

                    // Brain.Tick
                    _winningBrain.InputSignalArray.CopyFrom(inputArr, 0);
                    _winningBrain.Activate();
                    _winningBrain.OutputSignalArray.CopyTo(nnOutput, 0);

                    imageNNOutput.Source = UtilityWPF.GetBitmap(nnOutput, _harness.OutputSizeXY, _harness.OutputSizeXY, invert: true);
                }
                else
                {
                    imageNNOutput.Source = null;
                }

                #endregion
                #region draw error (nn - expected)

                double[] error = null;

                if (nnOutput != null)
                {
                    error = Enumerable.Range(0, nnOutput.Length).
                        Select(o => Math.Abs(nnOutput[o] - expectedOutput[o])).
                        ToArray();

                    imageError.Source = UtilityWPF.GetBitmap(error, _harness.OutputSizeXY, _harness.OutputSizeXY, invert: true);
                }
                else
                {
                    imageError.Source = null;
                }

                #endregion
                #region draw actual

                // Vision Rectangle
                Rectangle visionRect = new Rectangle()
                {
                    Stroke = Brushes.Silver,
                    StrokeThickness = .3,
                    Width = _harness.VisionSize,
                    Height = _harness.VisionSize,
                };

                Canvas.SetLeft(visionRect, _harness.VisionSize / -2);
                Canvas.SetTop(visionRect, _harness.VisionSize / -2);

                canvasMain.Children.Add(visionRect);

                // Dot Previous
                if (prevPosition != null)
                {
                    Ellipse dot = new Ellipse()
                    {
                        Fill = new SolidColorBrush(prevPosition.Item1.Color),
                        Stroke = Brushes.Black,
                        StrokeThickness = .3,
                        Width = 2,
                        Height = 2,
                    };

                    Canvas.SetLeft(dot, prevPosition.Item2.X - 1);
                    Canvas.SetTop(dot, prevPosition.Item2.Y - 1);

                    canvasMain.Children.Add(dot);
                }

                // Dot Current
                if (currentPosition != null)
                {
                    Ellipse dot = new Ellipse()
                    {
                        Fill = new SolidColorBrush(currentPosition.Color),
                        Stroke = Brushes.White,
                        StrokeThickness = .3,
                        Width = 2,
                        Height = 2,
                    };

                    Canvas.SetLeft(dot, currentPosition.Position.X - 1);
                    Canvas.SetTop(dot, currentPosition.Position.Y - 1);

                    canvasMain.Children.Add(dot);
                }

                // Transform
                TransformGroup transform = new TransformGroup();

                transform.Children.Add(new ScaleTransform(canvasMain.ActualWidth / _harness.MapSize, canvasMain.ActualHeight / _harness.MapSize));
                transform.Children.Add(new TranslateTransform(canvasMain.ActualWidth / 2, canvasMain.ActualHeight / 2));

                canvasMain.RenderTransform = transform;

                #endregion

                _prevTick = now;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            // This uses the eval's duration, and uses a neural net for the trial tick count.  This is to recreate what the training actually sees

            try
            {
                imageInput.Source = null;
                canvasMain.Children.Clear();

                DateTime now = DateTime.UtcNow;

                if (_harness == null || _evalArgs == null)
                {
                    _prevTick = now;
                    return;
                }

                // Reset
                if (_tickCounter >= _evalArgs.TotalNumberEvaluations)
                {
                    #region reset

                    if (_experiment != null)
                    {
                        NeatGenome winner = _ea.CurrentChampGenome;
                        _genomeViewer.RefreshView(winner);

                        if (winner == null)
                        {
                            _prevTick = now;
                            return;
                        }

                        bool showedHyper = false;
                        if (_experiment.IsHyperNEAT)
                        {
                            var decoder = _experiment.CreateGenomeDecoder(_hyperneatArgs);
                            if (decoder is HyperNeatDecoder hyperDecoder)
                            {
                                //NOTE: There is a flaw.  The INetworkDefinition that this method returns is acyclic, but the method that generates the IBlackBox is CreateSubstrateNetwork_FastCyclicNetwork.
                                //So that INetworkDefinition seems to be limited or incorrect
                                //
                                //Actually, that might not be a flaw.  The FastCyclic may be processing the CPPN which is cyclic

                                var finalPhenome = hyperDecoder.Decode2(winner);

                                _genomeViewer2.RefreshView(finalPhenome.Item2);

                                nnViewerHost2.Visibility = Visibility.Visible;
                                showedHyper = true;
                            }
                        }

                        if (!showedHyper)
                        {
                            nnViewerHost2.Visibility = Visibility.Collapsed;
                        }

                        if (_experiment.IsHyperNEAT)
                        {
                            _winningBrain = _experiment.GetBlackBox(winner, _hyperneatArgs);
                        }
                        else
                        {
                            _winningBrain = _experiment.GetBlackBox(winner);
                        }

                        _winningBrainTime = now;

                        _harness.ClearItem();

                        _harness.SetItem(AntPos_Evaluator.GetNewItem(_harness, _evalArgs));
                        _tickCounter = -1;
                    }

                    #endregion
                }

                // Tell the harness to go
                _harness.Tick(_evalArgs.ElapsedTime_Seconds);
                _tickCounter++;

                var prevPosition = _harness.GetPreviousPosition(_harness.Time - _evalArgs.Delay_Seconds);
                var currentPosition = _harness.Item;

                #region draw input

                double[] inputArr = new double[_harness.InputSizeXY * _harness.InputSizeXY];

                AntPos_Evaluator.ClearArray(inputArr);

                if (prevPosition != null)
                {
                    double dotRadius = (_harness.VisionSize / _harness.InputSizeXY) * Math.Sqrt(2);
                    AntPos_Evaluator.ApplyPoint(inputArr, _harness.InputCellCenters, dotRadius, prevPosition.Item2, true);
                }

                imageInput.Source = UtilityWPF.GetBitmap(inputArr, _harness.InputSizeXY, _harness.InputSizeXY, invert: true);

                #endregion
                #region draw expected output

                double[] expectedOutput = null;

                if (currentPosition == null)
                {
                    expectedOutput = AntPos_Evaluator.GetExpectedOutput(null, _harness, _evalArgs);
                }
                else
                {
                    var currentPos = Tuple.Create(currentPosition, currentPosition.Position, currentPosition.Velocity);
                    expectedOutput = AntPos_Evaluator.GetExpectedOutput(currentPos, _harness, _evalArgs);
                }

                imageExpectedOutput.Source = UtilityWPF.GetBitmap(expectedOutput, _harness.OutputSizeXY, _harness.OutputSizeXY, invert: true);

                #endregion
                #region draw nn output

                double[] nnOutput = null;

                if (_winningBrain != null)
                {
                    nnOutput = new double[_harness.OutputSizeXY * _harness.OutputSizeXY];

                    // Brain.Tick
                    _winningBrain.InputSignalArray.CopyFrom(inputArr, 0);
                    _winningBrain.Activate();
                    _winningBrain.OutputSignalArray.CopyTo(nnOutput, 0);

                    imageNNOutput.Source = UtilityWPF.GetBitmap(nnOutput, _harness.OutputSizeXY, _harness.OutputSizeXY, invert: true);
                }
                else
                {
                    imageNNOutput.Source = null;
                }

                #endregion
                #region draw error (nn - expected)

                double[] error = null;

                if (nnOutput != null)
                {
                    error = Enumerable.Range(0, nnOutput.Length).
                        Select(o => Math.Abs(nnOutput[o] - expectedOutput[o])).
                        ToArray();

                    imageError.Source = UtilityWPF.GetBitmap(error, _harness.OutputSizeXY, _harness.OutputSizeXY, invert: true);
                }
                else
                {
                    imageError.Source = null;
                }

                #endregion
                #region draw actual

                // Vision Rectangle
                Rectangle visionRect = new Rectangle()
                {
                    Stroke = Brushes.Silver,
                    StrokeThickness = .3,
                    Width = _harness.VisionSize,
                    Height = _harness.VisionSize,
                };

                Canvas.SetLeft(visionRect, _harness.VisionSize / -2);
                Canvas.SetTop(visionRect, _harness.VisionSize / -2);

                canvasMain.Children.Add(visionRect);

                // Dot Previous
                if (prevPosition != null)
                {
                    Ellipse dot = new Ellipse()
                    {
                        Fill = new SolidColorBrush(prevPosition.Item1.Color),
                        Stroke = Brushes.Black,
                        StrokeThickness = .3,
                        Width = 2,
                        Height = 2,
                    };

                    Canvas.SetLeft(dot, prevPosition.Item2.X - 1);
                    Canvas.SetTop(dot, prevPosition.Item2.Y - 1);

                    canvasMain.Children.Add(dot);
                }

                // Dot Current
                if (currentPosition != null)
                {
                    Ellipse dot = new Ellipse()
                    {
                        Fill = new SolidColorBrush(currentPosition.Color),
                        Stroke = Brushes.White,
                        StrokeThickness = .3,
                        Width = 2,
                        Height = 2,
                    };

                    Canvas.SetLeft(dot, currentPosition.Position.X - 1);
                    Canvas.SetTop(dot, currentPosition.Position.Y - 1);

                    canvasMain.Children.Add(dot);
                }

                // Transform
                TransformGroup transform = new TransformGroup();

                transform.Children.Add(new ScaleTransform(canvasMain.ActualWidth / _harness.MapSize, canvasMain.ActualHeight / _harness.MapSize));
                transform.Children.Add(new TranslateTransform(canvasMain.ActualWidth / 2, canvasMain.ActualHeight / 2));

                canvasMain.RenderTransform = transform;

                #endregion

                _prevTick = now;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EA_UpdateEvent(object sender, EventArgs e)
        {
            try
            {
                if (sender is NeatEvolutionAlgorithm<NeatGenome> ea)
                {
                    Action action = () =>
                    {
                        eaStats.Update(ea);
                    };

                    Dispatcher.BeginInvoke(action);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void EA_PausedEvent(object sender, EventArgs e)
        {
            try
            {
                Action action = () =>
                {
                    //TODO: Update UI
                };

                Dispatcher.BeginInvoke(action);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Reset2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // If not hyperneat, just replace with a new net
                // If hyperneat, compare other args and retain genomes if the only change is input/output resolution

                string prevGenomeXML = null;
                if (chkHyperNEAT.IsChecked.Value && _experiment != null && _ea != null)
                {
                    //_ea.Stop();       // currently, Stop just calls RequestPause, so don't use it.  There needs to be a dispose that removes the underlying thread
                    _ea.RequestPauseAndWait();
                    prevGenomeXML = ExperimentNEATBase.SavePopulation(_ea.GenomeList);
                }

                RemoveExistingExperiment();

                // My stuff
                #region harness args

                _harnessArgs = new HarnessArgs(
                    trkMapSize.Value,
                    trkVisionSize.Value,
                    trkOutputSize.Value,
                    trkInputPixels.Value.ToInt_Round(),
                    trkOutputPixels.Value.ToInt_Round(),
                    trkDelayBetweenInstances.Value);

                #endregion
                #region eval args

                if (chkRandomStartingConditions.IsChecked.Value)
                {
                    _evalArgs = new EvaluatorArgs(
                        trkEvalIterations.Value.ToInt_Round(),
                        trkDelay.Value,
                        trkEvalElapsedTime.Value,
                        trkMaxSpeed.Value,
                        chkBounceOffWalls.IsChecked.Value,
                        new[] { (TrackedItemType)cboTrackedItemType.SelectedValue },
                        trkNewItemDuration.Value,
                        trkNewItemErrorMultiplier.Value,
                        (ScoreLeftRightBias)cboErrorBias.SelectedValue);
                }
                else
                {
                    Point position = Math3D.GetRandomVector(_harnessArgs.MapSize / 2).ToPoint2D();
                    Vector velocity = Math3D.GetRandomVector_Circular(trkMaxSpeed.Value).ToVector2D();

                    // Don't let the velocity be in the same quadrant as the position (otherwise, you could have something spawn next to a wall, heading
                    // toward the wall).  These if statements force it to cross the x,y axiis
                    if (Math.Sign(position.X) == Math.Sign(velocity.X))
                    {
                        velocity = new Vector(-velocity.X, velocity.Y);
                    }

                    if (Math.Sign(position.Y) == Math.Sign(velocity.Y))
                    {
                        velocity = new Vector(velocity.X, -velocity.Y);
                    }

                    _evalArgs = new EvaluatorArgs(
                        trkEvalIterations.Value.ToInt_Round(),
                        trkDelay.Value,
                        trkEvalElapsedTime.Value,
                        new[] { Tuple.Create((TrackedItemType)cboTrackedItemType.SelectedValue, position, velocity, chkBounceOffWalls.IsChecked.Value) },
                        trkNewItemDuration.Value,
                        trkNewItemErrorMultiplier.Value,
                        (ScoreLeftRightBias)cboErrorBias.SelectedValue);
                }

                #endregion

                // SharpNEAT
                #region experiment args

                _experimentArgs = new ExperimentInitArgs()
                {
                    Description = "Input is a pixel array.  Output is a pixel array.  The NN needs to watch the object and anticipate where it will be at some fixed time in the future",
                    InputCount = _harnessArgs.InputSizeXY * _harnessArgs.InputSizeXY,
                    OutputCount = _harnessArgs.OutputSizeXY * _harnessArgs.OutputSizeXY,
                    IsHyperNEAT = chkHyperNEAT.IsChecked.Value,
                    PopulationSize = trkPopulationSize.Value.ToInt_Round(),
                    SpeciesCount = trkSpeciesCount.Value.ToInt_Round(),
                    Activation = new ExperimentInitArgs_Activation_CyclicFixedTimesteps()
                    {
                        TimestepsPerActivation = trkTimestepsPerActivation.Value.ToInt_Round(),
                        FastFlag = true
                    },
                    Complexity_RegulationStrategy = ComplexityCeilingType.Absolute,
                    Complexity_Threshold = trkComplexityThreshold.Value.ToInt_Round(),
                };

                #endregion
                #region hyperneat args

                _hyperneatArgs = null;

                if (chkHyperNEAT.IsChecked.Value)
                {
                    // Use two square sheets
                    var hyperPoints = HyperNEAT_Args.GetSquareSheets(trkVisionSize.Value, trkOutputSize.Value, _harnessArgs.InputSizeXY, _harnessArgs.OutputSizeXY);

                    _hyperneatArgs = new HyperNEAT_Args()
                    {
                        InputPositions = hyperPoints.inputs,
                        OutputPositions = hyperPoints.outputs,
                    };
                }

                #endregion

                #region create harness

                _harness = new TrackedItemHarness(_harnessArgs);

                _harness.ItemRemoved += (s1, e1) =>
                {
                    _harness.SetItem(AntPos_Evaluator.GetNewItem(_harness, _evalArgs));
                };

                _harness.SetItem(AntPos_Evaluator.GetNewItem(_harness, _evalArgs));

                #endregion
                #region create evaluator

                AntPos_Evaluator evaluator = new AntPos_Evaluator(_harnessArgs, _evalArgs);

                //FitnessInfo score = evaluator.Evaluate(new RandomBlackBoxNetwork(_harness.InputSizeXY * _harness.InputSizeXY, _harness.OutputSizeXY * _harness.OutputSizeXY, true));      // this is a good place to unit test the evaluator

                #endregion
                #region create experiment

                _experiment = new ExperimentNEATBase();
                _experiment.Initialize("anticipate position", _experimentArgs, evaluator);

                #endregion
                #region create evolution algorithm

                if (prevGenomeXML == null)
                {
                    if (chkHyperNEAT.IsChecked.Value)
                    {
                        _ea = _experiment.CreateEvolutionAlgorithm(_hyperneatArgs);
                    }
                    else
                    {
                        _ea = _experiment.CreateEvolutionAlgorithm();
                    }
                }
                else
                {
                    List<NeatGenome> genomeList;
                    if (_hyperneatArgs == null)
                    {
                        genomeList = ExperimentNEATBase.LoadPopulation(prevGenomeXML, _experimentArgs.Activation, _experimentArgs.InputCount, _experimentArgs.OutputCount);
                    }
                    else
                    {
                        genomeList = ExperimentNEATBase.LoadPopulation(prevGenomeXML, _experimentArgs.Activation, _hyperneatArgs);
                    }

                    // The factory is the same for all items, so just grab the first one
                    NeatGenomeFactory genomeFactory = genomeList[0].GenomeFactory;

                    _ea = _experiment.CreateEvolutionAlgorithm(genomeFactory, genomeList, _hyperneatArgs);
                }

                _ea.UpdateEvent += EA_UpdateEvent;
                _ea.PausedEvent += EA_PausedEvent;

                #endregion

                ShowBestGenome();       // this ensures the neural viewer is created
                _winningBrainTime = DateTime.UtcNow - TimeSpan.FromDays(1);     // put it way in the past so the first tick will request a new winner
                _winningBrain = null;
                //_winningBrain = new RandomBlackBoxNetwork(_harness.InputSizeXY * _harness.InputSizeXY, _harness.OutputSizeXY * _harness.OutputSizeXY, true);

                _tickCounter = _evalArgs.TotalNumberEvaluations * 2;        // force the timer to get the winning NN right away (otherwise it will do a round before refreshing)

                _ea.StartContinue();        // this needs to be done last
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private void ShowBestGenome()
        {
            //if (_experiment == null)
            //{
            //    throw new ApplicationException("This method expects _experiment to be created");
            //}

            if (_genomeViewer == null)
            {
                _genomeViewer = new NeatGenomeView();
                nnViewerHost.Child = _genomeViewer;
            }

            if (_genomeViewer2 == null)
            {
                _genomeViewer2 = new NeatGenomeView();
                nnViewerHost2.Child = _genomeViewer2;
            }

        }

        private void RemoveExistingExperiment()
        {
            if (_ea != null)
            {
                //_ea.Stop();
                _ea.Dispose();
            }

            if (_experiment != null)
            {
                //_experiment.
            }

            _experiment = null;
            _ea = null;

            _experimentArgs = null;
            _hyperneatArgs = null;

            _harness = null;
            _harnessArgs = null;
            _evalArgs = null;

            _winningBrain = null;
        }

        #endregion

        #region brainNEAT

        private void BrainNEAT_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_experiment == null || _ea == null)
                {
                    //NOTE: Just doing this for laziness.  I don't want to write a bunch of logic to train a phenome up front
                    MessageBox.Show("Need to have a running experiment", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Get a genome
                NeatGenome genome = _ea.CurrentChampGenome;

                // Create a phenome
                IBlackBox phenome = ExperimentNEATBase.GetBlackBox(genome, _experimentArgs.Activation, _hyperneatArgs);

                // Instantiate a BrainNEAT
                EditorOptions options = new EditorOptions();
                ItemOptions itemOptions = new ItemOptions();

                Container energy = new Container()
                {
                    QuantityMax = 1000,
                    QuantityCurrent = 1000,
                };

                BrainNEATDNA dnaBrain = new BrainNEATDNA() { PartType = BrainNEAT.PARTTYPE, Position = new Point3D(0, 0, 0), Orientation = Quaternion.Identity, Scale = new Vector3D(1, 1, 1) };

                BrainNEAT brain = new BrainNEAT(options, itemOptions, dnaBrain, energy);


                brain.SetPhenome(phenome, genome, _experimentArgs.Activation, _hyperneatArgs);


                for (int cntr = 0; cntr < 100; cntr++)
                {
                    foreach (INeuron neuron in brain.Neruons_Writeonly)
                    {
                        neuron.SetValue(StaticRandom.NextDouble(-2.5, 2.5));
                    }

                    brain.Update_AnyThread(1);
                }




                #region save/load test2

                // let BrainNEAT do the save/load

                BrainNEATDNA dna2 = (BrainNEATDNA)brain.GetNewDNA();
                string dna2Text = XamlServices.Save(dna2).Replace('"', '\'');
                BrainNEAT brain2 = new BrainNEAT(options, itemOptions, dna2, energy);

                for (int cntr = 0; cntr < 100; cntr++)
                {
                    foreach (INeuron neuron in brain2.Neruons_Writeonly)
                    {
                        neuron.SetValue(StaticRandom.NextDouble(-2.5, 2.5));
                    }

                    brain2.Update_AnyThread(1);
                }

                BrainNEATDNA dna3 = (BrainNEATDNA)brain2.GetNewDNA();
                BrainNEAT brain3 = new BrainNEAT(options, itemOptions, dna3, energy);

                for (int cntr = 0; cntr < 100; cntr++)
                {
                    foreach (INeuron neuron in brain3.Neruons_Writeonly)
                    {
                        neuron.SetValue(StaticRandom.NextDouble(-2.5, 2.5));
                    }

                    brain3.Update_AnyThread(1);
                }

                #endregion
                #region save/load test1

                // initial test, building minimum necessary dna

                BrainNEATDNA brainDNA = new BrainNEATDNA()
                {
                    Activation = _experimentArgs.Activation,
                    Hyper = _hyperneatArgs,
                    NEATPositions_Input = brain.Neruons_Readonly.Select(o => o.Position).ToArray(),
                    NEATPositions_Output = brain.Neruons_Writeonly.Select(o => o.Position).ToArray(),
                    Genome = ExperimentNEATBase.SavePopulation(new[] { genome }),
                };


                // Make sure this can be serialized/deserialized
                string testString = XamlServices.Save(brainDNA);
                brainDNA = UtilityCore.Clone(brainDNA);


                List<NeatGenome> genomeList = null;
                if (_hyperneatArgs == null)
                {
                    genomeList = ExperimentNEATBase.LoadPopulation(brainDNA.Genome, brainDNA.Activation, brainDNA.NEATPositions_Input.Length, brainDNA.NEATPositions_Output.Length);
                }
                else
                {
                    genomeList = ExperimentNEATBase.LoadPopulation(brainDNA.Genome, brainDNA.Activation, _hyperneatArgs);
                }

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
        #region test hyperneat

        //private void TestHyperNEAT_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        // With hyperneat, it's the CPPN that is getting evolved.  That CPPN doesn't care about resolution, it is used to generate neural nets
        //        // at arbitrary resolutions.
        //        //
        //        // The CPPN's input/output size is the dimentions of a point.  In this case, the final NN's input and output neurons are two square
        //        // sheets, so each point is a 3D position (x,y in the sheet, then z=-1 or 1 so the sheets are separated).  So the CPPN's input size is 3*2
        //        // (plus a bias node), and the output size is 1, 2 or 3 (I forget).  The CPPN is a recurrent neural net that is loaded with 1 input point and
        //        // 1 output point, and an extra neuron for distance between those points.  Then it is run for a few cycles and the output is the built
        //        // NN's input weight at that point
        //        //
        //        // That last paragraph may be a bit innacurate, especially about how the CPPN's output is used to populate the final NN
        //        //
        //        // So it's the CPPN that is being mutated/retained, but it is the NNs that are constructed at the desired resolution that are being
        //        // evaluated /scored


        //        // Start training with a 2x2
        //        TestHyperNEATSave save = TestHyperNEAT(2);

        //        // Bump up to 3x3, but keep the current genomes
        //        save = TestHyperNEAT(3, save);

        //        // Keep bumping up the resolution
        //        save = TestHyperNEAT(4, save);
        //        save = TestHyperNEAT(5, save);
        //        save = TestHyperNEAT(6, save);
        //        save = TestHyperNEAT(7, save);
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}

        //private TestHyperNEATSave TestHyperNEAT(int inputOutputXY, TestHyperNEATSave save = null)
        //{
        //    #region harness args

        //    HarnessArgs harnessArgs = new HarnessArgs(
        //        trkMapSize.Value,
        //        trkVisionSize.Value,
        //        trkOutputSize.Value,
        //        inputOutputXY,
        //        inputOutputXY,
        //        trkDelayBetweenInstances.Value);

        //    #endregion
        //    #region eval args

        //    EvaluatorArgs evalArgs = null;

        //    if (chkRandomStartingConditions.IsChecked.Value)
        //    {
        //        evalArgs = new EvaluatorArgs(
        //            trkEvalIterations.Value.ToInt_Round(),
        //            trkDelay.Value,
        //            trkEvalElapsedTime.Value,
        //            trkMaxSpeed.Value,
        //            chkBounceOffWalls.IsChecked.Value,
        //            new[] { (TrackedItemType)cboTrackedItemType.SelectedValue },
        //            trkNewItemDuration.Value,
        //            trkNewItemErrorMultiplier.Value,
        //            (ScoreLeftRightBias)cboErrorBias.SelectedValue);
        //    }
        //    else
        //    {
        //        Point position = Math3D.GetRandomVector(harnessArgs.MapSize / 2).ToPoint2D();
        //        Vector velocity = Math3D.GetRandomVector_Circular(trkMaxSpeed.Value).ToVector2D();

        //        // Don't let the velocity be in the same quadrant as the position (otherwise, you could have something spawn next to a wall, heading
        //        // toward the wall).  These if statements force it to cross the x,y axiis
        //        if (Math.Sign(position.X) == Math.Sign(velocity.X))
        //        {
        //            velocity = new Vector(-velocity.X, velocity.Y);
        //        }

        //        if (Math.Sign(position.Y) == Math.Sign(velocity.Y))
        //        {
        //            velocity = new Vector(velocity.X, -velocity.Y);
        //        }

        //        evalArgs = new EvaluatorArgs(
        //            trkEvalIterations.Value.ToInt_Round(),
        //            trkDelay.Value,
        //            trkEvalElapsedTime.Value,
        //            new[] { Tuple.Create((TrackedItemType)cboTrackedItemType.SelectedValue, position, velocity, chkBounceOffWalls.IsChecked.Value) },
        //            trkNewItemDuration.Value,
        //            trkNewItemErrorMultiplier.Value,
        //            (ScoreLeftRightBias)cboErrorBias.SelectedValue);
        //    }

        //    #endregion
        //    #region experiment args

        //    ExperimentInitArgs config = new ExperimentInitArgs()
        //    {
        //        Description = "Input is a pixel array.  Output is a pixel array.  The NN needs to watch the object and anticipate where it will be at some fixed time in the future",
        //        //InputCount = harness.InputSizeXY * harness.InputSizeXY,
        //        //OutputCount = harness.OutputSizeXY * harness.OutputSizeXY,
        //        IsHyperNEAT = true,
        //        PopulationSize = trkPopulationSize.Value.ToInt_Round(),
        //        SpeciesCount = trkSpeciesCount.Value.ToInt_Round(),
        //        Activation = new ExperimentInitArgs_Activation_CyclicFixedTimesteps()
        //        {
        //            TimestepsPerActivation = trkTimestepsPerActivation.Value.ToInt_Round(),
        //            FastFlag = true
        //        },
        //        Complexity_RegulationStrategy = ComplexityCeilingType.Absolute,
        //        Complexity_Threshold = trkComplexityThreshold.Value.ToInt_Round(),
        //    };

        //    #endregion
        //    #region hyperneat args

        //    HyperNEAT_Args hyperneatArgs = new HyperNEAT_Args()
        //    {
        //        InputShape = HyperNEAT_Shape.Square,
        //        OutputShape = HyperNEAT_Shape.Square,
        //        InputSize = trkVisionSize.Value,
        //        OutputSize = trkOutputSize.Value,
        //        InputCountXY = inputOutputXY,
        //        OutputCountXY = inputOutputXY,
        //    };

        //    #endregion

        //    #region create

        //    TrackedItemHarness harness = new TrackedItemHarness(harnessArgs);

        //    harness.ItemRemoved += (s1, e1) =>
        //    {
        //        harness.SetItem(AntPos_Evaluator.GetNewItem(harness, evalArgs));
        //    };

        //    harness.SetItem(AntPos_Evaluator.GetNewItem(harness, evalArgs));

        //    AntPos_Evaluator evaluator = new AntPos_Evaluator(harnessArgs, evalArgs);

        //    ExperimentBase_NEAT experiment = new ExperimentBase_NEAT();

        //    experiment.Initialize("test hyperneat", config, evaluator);

        //    NeatEvolutionAlgorithm<NeatGenome> ea = null;
        //    if (save?.GenomePopulationXML == null)
        //    {
        //        ea = experiment.CreateEvolutionAlgorithm(hyperneatArgs);
        //    }
        //    else
        //    {
        //        List<NeatGenome> genomeList = experiment.LoadPopulation(save.GenomePopulationXML, hyperneatArgs);

        //        // The factory is the same for all items, so just grab the first one
        //        NeatGenomeFactory genomeFactory = genomeList[0].GenomeFactory;

        //        ea = experiment.CreateEvolutionAlgorithm(genomeFactory, genomeList, hyperneatArgs);
        //    }

        //    ea.UpdateEvent += EA_UpdateEvent;

        //    #endregion
        //    #region run

        //    ShowBestGenome();

        //    ea.StartContinue();


        //    for (int cntr = 0; cntr < 100000000; cntr++)
        //    {
        //        int seven = 6;
        //    }

        //    ea.RequestPauseAndWait();
        //    ea.Stop();

        //    #endregion

        //    return new TestHyperNEATSave()
        //    {
        //        //WinningGenomeXML = experiment.SavePopulation(new[] { ea.CurrentChampGenome }),
        //        GenomePopulationXML = experiment.SavePopulation(ea.GenomeList),
        //    };
        //}

        //private class TestHyperNEATSave
        //{
        //    //NOTE: The population contains the winning genome.  If they are loaded separately then concatenated, NeatGenomeXmlIO.WriteComplete()
        //    //has an assert that fails because the ActivationFnLibrary instance is different between genomes.  (the values are all the same, but the assert is simplistic
        //    //an only compares object instances)
        //    //public string WinningGenomeXML { get; set; }

        //    public string GenomePopulationXML { get; set; }
        //}

        #endregion
    }
}
