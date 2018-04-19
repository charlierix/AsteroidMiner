using Game.HelperClassesCore;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.EvolutionAlgorithms.ComplexityRegulation;
using SharpNeat.Genomes.Neat;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Game.HelperClassesAI
{
    public partial class NeatEAStats : UserControl
    {
        public NeatEAStats()
        {
            InitializeComponent();
        }

        public void Update(NeatEvolutionAlgorithm<NeatGenome> ea)
        {
            statsGrid.Children.Clear();
            statsGrid.RowDefinitions.Clear();

            #region grow/shrink

            statsGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });

            TextBlock text = new TextBlock()
            {
                Text = ea.ComplexityRegulationMode == ComplexityRegulationMode.Complexifying ? "Growing" : "Shrinking",
                Style = FindResource("valueTextStats_Centered") as Style,
            };

            Grid.SetColumn(text, 0);
            Grid.SetColumnSpan(text, 3);
            Grid.SetRow(text, statsGrid.RowDefinitions.Count - 1);

            statsGrid.Children.Add(text);

            #endregion

            AddPromptValue(this, statsGrid, "Generation", ea.CurrentGeneration.ToString("N0"));
            AddPromptValue(this, statsGrid, "Best Score", ea.Statistics._maxFitness.ToStringSignificantDigits(2));
            AddPromptValue(this, statsGrid, "Most Complex", ea.Statistics._maxComplexity.ToStringSignificantDigits(2));
        }

        private static void AddPromptValue(UserControl control, Grid grid, string prompt, string value)
        {
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });

            // Prompt
            TextBlock text = new TextBlock()
            {
                Text = prompt,
                Style = control.FindResource("promptTextStats") as Style,
            };

            Grid.SetColumn(text, 0);
            Grid.SetRow(text, grid.RowDefinitions.Count - 1);

            grid.Children.Add(text);

            // Value
            text = new TextBlock()
            {
                Text = value,
                Style = control.FindResource("valueTextStats") as Style,
            };

            Grid.SetColumn(text, 2);
            Grid.SetRow(text, grid.RowDefinitions.Count - 1);

            grid.Children.Add(text);
        }
    }
}
