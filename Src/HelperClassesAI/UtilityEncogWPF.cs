using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using Encog.Neural.Networks;
using Game.HelperClassesWPF;

namespace Game.HelperClassesAI
{
    //TODO: A method that shows a summary of scores
    //TODO: These methods should return custom user controls instead.  The properties of the controls should cause visual changes.  After that is done, these methods would just be helper factory methods - use them or don't

    /// <summary>
    /// These are helper methods that return visuals
    /// </summary>
    public static class UtilityEncogWPF
    {
        /// <summary>
        /// This returns a label with a border and green dropshadow, or an invisible border
        /// </summary>
        public static UIElement GetFinalGuess(string[] categories, double[] result)
        {
            #region original xaml

            // No Match
            //<Style x:Key="mildBorder" TargetType="Border">
            //    <Setter Property="Background" Value="#18FFFFFF"/>
            //    <Setter Property="BorderBrush" Value="#18000000"/>
            //    <Setter Property="CornerRadius" Value="3"/>
            //    <Setter Property="BorderThickness" Value="1"/>
            //</Style>

            // Match
            //<Style x:Key="validGuessBorder" TargetType="Border">
            //    <Setter Property="Background" Value="#FBFFFB"/>
            //    <Setter Property="BorderBrush" Value="#30000000"/>
            //    <Setter Property="CornerRadius" Value="3"/>
            //    <Setter Property="BorderThickness" Value="1"/>
            //    <Setter Property="Effect">
            //        <Setter.Value>
            //            <DropShadowEffect Color="#60E060" BlurRadius="15" Direction="0" ShadowDepth="0" Opacity=".8"/>
            //        </Setter.Value>
            //    </Setter>
            //</Style>

            //<Border CornerRadius="5" Name="pnlCurrentGuess" HorizontalAlignment="Stretch" Style="{StaticResource mildBorder}">
            //    <TextBlock Name="lblCurrentGuess" FontSize="20" HorizontalAlignment="Center"/>
            //</Border>

            #endregion

            int? matchIndex = UtilityEncog.IsMatch(result);

            TextBlock textblock = new TextBlock()
            {
                Text = matchIndex == null ? "" : categories[matchIndex.Value],
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
            };

            Border retVal = new Border()
            {
                Child = textblock,
                CornerRadius = new CornerRadius(3),
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };

            if (matchIndex == null)
            {
                // No Match
                retVal.Background = new SolidColorBrush(UtilityWPF.ColorFromHex("18FFFFFF"));
                retVal.BorderBrush = new SolidColorBrush(UtilityWPF.ColorFromHex("18000000"));
            }
            else
            {
                // Match
                retVal.Background = new SolidColorBrush(UtilityWPF.ColorFromHex("FBFFFB"));
                retVal.BorderBrush = new SolidColorBrush(UtilityWPF.ColorFromHex("30000000"));
                retVal.Effect = new DropShadowEffect()
                {
                    Color = UtilityWPF.ColorFromHex("60E060"),
                    BlurRadius = 15,
                    Direction = 0,
                    ShadowDepth = 0,
                    Opacity = .8,
                };
            }

            return retVal;
        }

        //TODO: Let it dynamically size instead of hardcoding the width
        public static UIElement GetResultList(string[] categories, double[] result, bool shouldSort)
        {
            Grid retVal = new Grid();
            retVal.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
            retVal.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(4) });
            retVal.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });

            Brush percentStroke = new SolidColorBrush(UtilityWPF.ColorFromHex("60000000"));
            Brush percentFill = new SolidColorBrush(UtilityWPF.ColorFromHex("30000000"));

            var resultEntries = result.
                Select((o, i) => new { Value = o, Index = i, Category = categories[i] });

            if (shouldSort)
            {
                resultEntries = resultEntries.OrderByDescending(o => o.Value);
            }

            foreach (var resultEntry in resultEntries)
            {
                retVal.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });

                // Category
                TextBlock outputText = new TextBlock()
                {
                    Text = resultEntry.Category,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center,
                };

                Grid.SetColumn(outputText, 0);
                Grid.SetRow(outputText, retVal.RowDefinitions.Count - 1);

                retVal.Children.Add(outputText);

                // % background
                Rectangle rectPerc = new Rectangle()
                {
                    Height = 20,
                    Width = resultEntry.Value * 100,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Fill = percentFill,
                };

                Grid.SetColumn(rectPerc, 2);
                Grid.SetRow(rectPerc, retVal.RowDefinitions.Count - 1);

                retVal.Children.Add(rectPerc);

                // % border
                rectPerc = new Rectangle()
                {
                    Height = 20,
                    Width = 100,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Stroke = percentStroke,
                    StrokeThickness = 1,
                };

                Grid.SetColumn(rectPerc, 2);
                Grid.SetRow(rectPerc, retVal.RowDefinitions.Count - 1);

                retVal.Children.Add(rectPerc);

                // % text
                TextBlock outputPerc = new TextBlock()
                {
                    Text = Math.Round(resultEntry.Value * 100).ToString(),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };

                Grid.SetColumn(outputPerc, 2);
                Grid.SetRow(outputPerc, retVal.RowDefinitions.Count - 1);

                retVal.Children.Add(outputPerc);
            }

            return retVal;
        }

        /// <summary>
        /// Just show the number of nodes in each layer
        /// </summary>
        /// <remarks>
        /// Color the text (use outlined textblock):
        ///     Black/White for 0 to 1 nodes
        ///     Red/Blue for -1 to 1 nodes
        /// </remarks>
        public static UIElement GetNetworkVisual_Collapsed(BasicNetwork network)
        {
            return null;
        }
        /// <summary>
        /// Draw each layer, and lines that show links
        /// </summary>
        /// <remarks>
        /// Color the nodes:
        ///     Black/White for 0 to 1 nodes
        ///     Red/Blue for -1 to 1 nodes
        /// 
        /// Node opacity enum:
        ///     Full: everything is drawn full opacity
        ///     Strength: opacity is less for weak links
        /// 
        /// Link options:
        ///     Full: all links are drawn
        ///     None: no links are drawn
        /// 
        /// Mouse over node options:
        ///     When links=full: extra highlight all links flowing in and out of the node, as well as hops to other nodes (also highlight the nodes)
        ///     When links=none: draw the link full, but only for the node they are over (as well as the nodes it touches)
        /// 
        /// Another helpful visual would be for networks that are fed from images.  Have a rectangle that represents
        /// the image:
        ///     Full: shows a heatmap of important pixels
        ///     None: blank until they mouse over nodes, then shows the heatmap for only those nodes
        /// </remarks>
        public static UIElement GetNetworkVisual_Expanded(BasicNetwork network)
        {
            return null;
        }

        // Return an object that holds results, and a visual that draws them with spring forces
        //TODO: Take in a token, and a delegate: Func<long,UIElement>
        public static object GetResultAxis2D(string[] categories, double[] result, object existing)
        {


            return null;
        }
        public static object GetResultAxis3D(string[] categories, double[] result, object existing)
        {



            return null;
        }
    }
}
