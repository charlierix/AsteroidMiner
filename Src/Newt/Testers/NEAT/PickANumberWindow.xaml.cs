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
using Game.HelperClassesAI;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using SharpNeat.Domains;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.EvolutionAlgorithms.ComplexityRegulation;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;

namespace Game.Newt.Testers.NEAT
{
    public partial class PickANumberWindow : Window
    {
        #region Declaration Section

        private PAN_Experiment _experiment = null;
        private NeatEvolutionAlgorithm<NeatGenome> _ea;

        private NeatGenomeView _genomeViewer = null;

        #endregion

        #region Constructor

        public PickANumberWindow()
        {
            InitializeComponent();

            this.Background = SystemColors.ControlBrush;
        }

        #endregion

        #region Event Listeners

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                #region ANNOYING XML

                //                string xml = "<?xml version=\"1.0\" encoding=\"utf - 8\" ?>" + @"
                //  <Experiments>

                //    " + "<Experiment name=\"Pick a Number\"" + @">
                //  <AssemblyPath>SharpNeatDomains.dll</AssemblyPath >
                //  <ClassName>SharpNeat.Domains.FunctionRegression.FunctionRegressionExperiment</ClassName>
                //  <Config>
                //    <PopulationSize>150</PopulationSize>
                //    <SpecieCount>10</SpecieCount>
                //    <Activation>
                //      <Scheme>Acyclic</ Scheme >
                //    </Activation>
                //    <ComplexityRegulationStrategy>Absolute</ ComplexityRegulationStrategy >
                //    <ComplexityThreshold>20</ComplexityThreshold>
                //    <Description>Pick a Number</Description>
                //    </Config>
                //  </Experiment>

                //</Experiments>
                //";

                //                //< Function > Sine </ Function >
                //                //< SampleResolution > 21 </ SampleResolution >
                //                //< SampleMin > -3.14159 </ SampleMin >
                //                //< SampleMax > 3.14159 </ SampleMax >


                //                XmlDocument doc = new XmlDocument();
                //                doc.LoadXml(xml);

                //                XmlElement element = doc.SelectSingleNode("Config") as XmlElement;

                //                _experiment.Initialize("Pick a Number", element);

                #endregion

                int[] trueValues = UtilityCore.RandomRange(0, 8, StaticRandom.Next(1, 8)).ToArray();

                ExperimentInitArgs config = new ExperimentInitArgs()
                {
                    Description = "Some numbers are chosen, the NN needs to train itself to return true when one of those numbers are presented",
                    InputCount = 3,
                    OutputCount = 1,
                    PopulationSize = 150,
                    SpeciesCount = 10,
                    Activation = new ExperimentInitArgs_Activation_Acyclic(),
                    Complexity_RegulationStrategy = ComplexityCeilingType.Absolute,
                    Complexity_Threshold = 20,
                };

                PAN_Evaluator evaluator = new PAN_Evaluator(config.InputCount, PAN_Experiment.GetTrueVectors(config.InputCount, trueValues));

                _experiment = new PAN_Experiment(config.InputCount, trueValues);
                _experiment.Initialize("pick a number", config, evaluator);

                //_experiment.NeatGenomeParameters.InitialInterconnectionsProportion = .1;

                _ea = _experiment.CreateEvolutionAlgorithm();

                // Attach update event listener.
                _ea.UpdateEvent += EA_UpdateEvent;
                _ea.PausedEvent += EA_PausedEvent;

                ShowBestGenome();
                panPlot.ResetLabels(trueValues);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Start_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _ea.StartContinue();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _ea.RequestPause();
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
                Action action = () =>
                {
                    statsGrid.Children.Clear();
                    statsGrid.RowDefinitions.Clear();

                    #region grow/shrink

                    statsGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });

                    TextBlock text = new TextBlock()
                    {
                        Text = _ea.ComplexityRegulationMode == ComplexityRegulationMode.Complexifying ? "Growing" : "Shrinking",
                        Style = FindResource("valueText_Centered") as Style,
                    };

                    Grid.SetColumn(text, 0);
                    Grid.SetColumnSpan(text, 3);
                    Grid.SetRow(text, statsGrid.RowDefinitions.Count - 1);

                    statsGrid.Children.Add(text);

                    #endregion

                    AddPromptValue(this, statsGrid, "Generation", _ea.CurrentGeneration.ToString("N0"));
                    AddPromptValue(this, statsGrid, "Best Score", _ea.Statistics._maxFitness.ToString());
                    AddPromptValue(this, statsGrid, "Most Complex", _ea.Statistics._maxComplexity.ToString());

                    _genomeViewer.RefreshView(_ea.CurrentChampGenome);
                    //_graphViewer.RefreshView(_ea.CurrentChampGenome);
                    RedrawGraph(_ea.CurrentChampGenome);
                };

                Dispatcher.BeginInvoke(action);
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

        #endregion

        #region Private Methods

        private void ShowBestGenome()
        {
            if (_experiment == null)
            {
                throw new ApplicationException("This method expects _experiment to be created");
            }

            if (_genomeViewer == null)
            {
                _genomeViewer = new NeatGenomeView();
                nnViewerHost.Child = _genomeViewer;
            }
        }

        private void RedrawGraph(NeatGenome genome)
        {
            IBlackBox box = _experiment.GetBlackBox(genome);

            Color trueColor = panPlot.TrueColor;
            Color falseColor = panPlot.FalseColor;

            var samples = Enumerable.Range(0, 1000).
                Select(o =>
                {
                    Vector3D pos = Math3D.GetRandomVector(new Vector3D(0, 0, 0), new Vector3D(1, 1, 1));

                    box.ResetState();

                    //box.InputSignalArray.CopyFrom()
                    box.InputSignalArray[0] = pos.X;
                    box.InputSignalArray[1] = pos.Y;
                    box.InputSignalArray[2] = pos.Z;

                    box.Activate();

                    double percent = box.OutputSignalArray[0];

                    Color color = UtilityWPF.AlphaBlend(trueColor, falseColor, percent);

                    return Tuple.Create(pos.ToPoint(), color);
                });

            panPlot.ClearFrame();
            panPlot.AddDots(samples, .01, false);
        }

        private static void AddPromptValue(Window window, Grid grid, string prompt, string value)
        {
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });

            // Prompt
            TextBlock text = new TextBlock()
            {
                Text = prompt,
                Style = window.FindResource("promptText") as Style,
            };

            Grid.SetColumn(text, 0);
            Grid.SetRow(text, grid.RowDefinitions.Count - 1);

            grid.Children.Add(text);

            // Value
            text = new TextBlock()
            {
                Text = value,
                Style = window.FindResource("valueText") as Style,
            };

            Grid.SetColumn(text, 2);
            Grid.SetRow(text, grid.RowDefinitions.Count - 1);

            grid.Children.Add(text);
        }

        #endregion
    }
}
