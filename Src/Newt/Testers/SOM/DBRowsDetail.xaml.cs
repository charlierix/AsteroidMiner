using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
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
using Game.HelperClassesAI;
using Game.HelperClassesWPF;

namespace Game.Newt.Testers.SOM
{
    public partial class DBRowsDetail : Window
    {
        #region Declaration Section

        private readonly SelfOrganizingMapsDBWindow.QueryResults _queryResults;
        private readonly SelfOrganizingMapsDBWindow.ColumnSetStats _columns;
        private readonly SOMResult _somResult;
        private readonly int _nodeIndex;

        #endregion

        #region Constructor

        public DBRowsDetail(SelfOrganizingMapsDBWindow.QueryResults queryResults, SelfOrganizingMapsDBWindow.ColumnSetStats columns, SOMResult somResult, int nodeIndex)
        {
            InitializeComponent();

            _queryResults = queryResults;
            _columns = columns;
            _somResult = somResult;
            _nodeIndex = nodeIndex;
        }

        #endregion

        #region Event Listeners

        private async void grdDisplay_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                SOMFieldStats[] columnStats = await Task.Run(() => GetColumnStats(_queryResults));

                DataTable table = new DataTable();

                #region column definitions

                for (int cntr = 0; cntr < _queryResults.ColumnNames.Length; cntr++)
                {
                    DataColumn column;

                    switch (columnStats[cntr].FieldType)
                    {
                        case SOMFieldType.Integer:
                            column = new DataColumn(_queryResults.ColumnNames[cntr], typeof(long));
                            break;

                        case SOMFieldType.FloatingPoint:
                            column = new DataColumn(_queryResults.ColumnNames[cntr], typeof(decimal));
                            break;

                        case SOMFieldType.DateTime:
                            column = new DataColumn(_queryResults.ColumnNames[cntr], typeof(DateTime));
                            break;

                        case SOMFieldType.AlphaNumeric:
                        case SOMFieldType.AnyText:
                        default:
                            column = new DataColumn(_queryResults.ColumnNames[cntr], typeof(string));
                            break;
                    }

                    table.Columns.Add(column);
                }

                #endregion

                #region rows

                var rows = await Task.Run(() => GetMatchingRows(_columns.Columns, _queryResults, _somResult, _nodeIndex));

                foreach (var row in rows)
                {
                    DataRow datarow = table.NewRow();

                    for (int cntr = 0; cntr < row.Row.Length; cntr++)
                    {
                        datarow[cntr] = CastItem(row.Row[cntr], columnStats[cntr].FieldType);
                    }

                    table.Rows.Add(datarow);
                }

                #endregion

                var grid = sender as DataGrid;
                grid.AutoGenerateColumns = true;
                grid.ItemsSource = table.DefaultView;

                #region misc

                this.Title = string.Format("Node Rows - {0}", rows.Length.ToString("N0"));

                SolidColorBrush backBrush = this.Background as SolidColorBrush;
                if (backBrush != null)
                {
                    grdDisplay.RowBackground = new SolidColorBrush(UtilityWPF.AlphaBlend(Colors.White, backBrush.Color, .9));
                    grdDisplay.Background = new SolidColorBrush(UtilityWPF.AlphaBlend(Colors.White, backBrush.Color, .5));
                }

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private static SOMFieldStats[] GetColumnStats(SelfOrganizingMapsDBWindow.QueryResults results)
        {
            return Enumerable.Range(0, results.ColumnNames.Length).
                AsParallel().
                Select(o => new { Index = o, Stats = SelfOrganizingMapsDB.GetFieldStats(results.Results.Select(p => p[o])) }).
                OrderBy(o => o.Index).
                Select(o => o.Stats).
                ToArray();
        }

        private static SelfOrganizingMapsDBWindow.RowInput[] GetMatchingRows(SelfOrganizingMapsDBWindow.ColumnStats[] columns, SelfOrganizingMapsDBWindow.QueryResults queryResults, SOMResult som, int nodeIndex)
        {
            return SelfOrganizingMapsDBWindow.GetSOMInputs(columns, queryResults, false).
                Where(o => SelfOrganizingMaps.GetClosest(som.Nodes, o).Item2 == nodeIndex).
                ToArray();
        }

        private static object CastItem(string text, SOMFieldType type)
        {
            switch (type)
            {
                case SOMFieldType.Integer:
                    long castLong;
                    if (long.TryParse(text, out castLong))
                    {
                        return castLong;
                    }
                    else
                    {
                        return (long?)null;
                    }

                case SOMFieldType.FloatingPoint:
                    decimal castDecimal;
                    if (decimal.TryParse(text, out castDecimal))
                    {
                        return castDecimal;
                    }
                    else
                    {
                        return (decimal?)null;
                    }

                case SOMFieldType.DateTime:
                    //
                    DateTime castDate;
                    if (DateTime.TryParse(text, out castDate))
                    {
                        return castDate;
                    }
                    else
                    {
                        return (DateTime?)null;
                    }

                case SOMFieldType.AlphaNumeric:
                case SOMFieldType.AnyText:
                default:
                    return text;
            }
        }

        #endregion
    }
}
