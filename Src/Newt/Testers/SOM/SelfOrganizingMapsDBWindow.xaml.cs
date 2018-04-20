using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
using Game.HelperClassesCore;
using Game.HelperClassesCore.Threads;
using Game.HelperClassesWPF;
using Game.HelperClassesWPF.Controls2D;

namespace Game.Newt.Testers.SOM
{
    public partial class SelfOrganizingMapsDBWindow : Window
    {
        #region Class: QueryRequest

        private class QueryRequest
        {
            public QueryRequest(string connectionString, string sqlStatement, string[] columns)
            {
                this.ConnectionString = connectionString;
                this.SQLStatement = sqlStatement;
                this.Columns = columns;
            }

            public readonly string ConnectionString;
            public readonly string SQLStatement;
            public readonly string[] Columns;
        }

        #endregion
        #region Class: ColumnStatsRequest

        private class ColumnStatsRequest
        {
            public ColumnStatsRequest(string[] names, QueryResults results)
            {
                this.Names = names;
                this.Results = results;
            }

            public readonly string[] Names;
            public readonly QueryResults Results;
        }

        #endregion
        #region Class: SOMRequest

        private class SOMRequest
        {
            public SOMRequest(RowInput[] inputs, SOMRules rules)
            {
                this.Inputs = inputs;
                this.Rules = rules;
            }

            public readonly RowInput[] Inputs;
            public readonly SOMRules Rules;
        }

        #endregion

        #region Class: QueryResults

        public class QueryResults
        {
            public QueryResults(string connectionString, string sqlStatement, string[] columnNames, string[][] results)
            {
                this.ConnectionString = connectionString;
                this.SQLStatement = sqlStatement;
                this.ColumnNames = columnNames;
                this.Results = results;

                this.Exception = null;
            }
            public QueryResults(string connectionString, string sqlStatement, string exception)
            {
                this.ConnectionString = connectionString;
                this.SQLStatement = sqlStatement;
                this.Exception = exception;

                this.ColumnNames = null;
                this.Results = null;
            }

            public readonly string ConnectionString;
            public readonly string SQLStatement;

            // Only one of these two will be populated
            public readonly string Exception;

            public readonly string[] ColumnNames;
            public readonly string[][] Results;
        }

        #endregion
        #region Class: ColumnSetStats

        public class ColumnSetStats
        {
            #region Constructor

            public ColumnSetStats(string[] names, string exception)
            {
                this.Names = names;
                this.Exception = exception;

                this.Columns = null;
            }
            public ColumnSetStats(ColumnStats[] columns)
            {
                this.Names = columns.Select(o => o.Name).ToArray();
                this.Columns = columns;

                this.Exception = null;
            }

            #endregion

            #region Public Properties

            public readonly string[] Names;

            // Only one of these will be populated
            public readonly string Exception;
            public readonly ColumnStats[] Columns;      // this array is synced with names

            #endregion

            #region Public Methods

            public static string[] GetColumnNames(string names)
            {
                List<string> retVal = new List<string>();

                foreach (Match match in Regex.Matches(names.Replace(',', ' '), @"[^\s]+"))
                {
                    retVal.Add(match.Value);
                }

                return retVal.ToArray();
            }

            public ColumnSetStats CloneIfSameNames(string names, string[] allNames)
            {
                return CloneIfSameNames(GetColumnNames(names), allNames);
            }
            public ColumnSetStats CloneIfSameNames(string[] names, string[] allNames)
            {
                Tuple<int, int>[] map = GetNameMapping(names, allNames);
                if (map == null)
                {
                    if (this.Exception != null && IsSame(names, this.Names))
                    {
                        return new ColumnSetStats(names, this.Exception);
                    }
                    else
                    {
                        return null;
                    }
                }

                ColumnStats[] columns = map.
                    Select((o, i) =>
                        {
                            ColumnStats col = this.Columns[o.Item2];

                            return new ColumnStats(names[i], o.Item1, col.FieldStats, col.FieldStatsText)
                            {
                                Width = col.Width,
                                ForceText = col.ForceText,
                                Override_Number_Min = col.Override_Number_Min,
                                Override_Number_Max = col.Override_Number_Max,
                                Override_Date_Min = col.Override_Date_Min,
                                Override_Date_Max = col.Override_Date_Max,
                                Override_UniqueChars = col.Override_UniqueChars,
                            };
                        }).
                    ToArray();

                return new ColumnSetStats(columns);
            }

            #endregion

            #region Private Methods

            /// <summary>
            /// This will return an array the same size as names, or null.
            /// Index into array is the corresponding index into names
            /// Value of array at that position is the index into this.Names
            /// </summary>
            private Tuple<int, int>[] GetNameMapping(string[] names, string[] allNames)
            {
                if (this.Columns == null)
                {
                    return null;
                }
                else if (names.Length != this.Names.Length)     // the constructor ensures Names and Columns are the same size
                {
                    return null;
                }

                var indices = GetIndices(names, allNames);
                if (indices.Item2 != null)
                {
                    return null;
                }

                int[] thisIndices = this.Columns.
                    Select(o => o.Index).
                    ToArray();

                var retVal = new Tuple<int, int>[names.Length];

                for (int cntr = 0; cntr < retVal.Length; cntr++)
                {
                    int index = Array.IndexOf<int>(thisIndices, indices.Item1[cntr]);
                    if (index < 0)
                    {
                        return null;
                    }

                    retVal[cntr] = Tuple.Create(indices.Item1[cntr], index);
                }

                return retVal;
            }

            #endregion
        }

        #endregion
        #region Class: ColumnStats

        public class ColumnStats
        {
            public ColumnStats(string name, int index, SOMFieldStats fieldStats, SOMFieldStats fieldStatsText)
            {
                this.Name = name;
                this.Index = index;
                this.FieldStats = fieldStats;
                this.FieldStatsText = fieldStatsText;

                this.Width = 5;
                this.ForceText = false;
            }

            public readonly string Name;
            public readonly int Index;
            public readonly SOMFieldStats FieldStats;
            public readonly SOMFieldStats FieldStatsText;

            // ------- Editable
            public int Width { get; set; }
            public bool ForceText { get; set; }

            public double? Override_Number_Min { get; set; }
            public double? Override_Number_Max { get; set; }

            public DateTime? Override_Date_Min { get; set; }
            public DateTime? Override_Date_Max { get; set; }

            public TextAlignment? Override_Text_Justify { get; set; }
            public char[] Override_UniqueChars { get; set; }
        }

        #endregion
        #region Class: RowInput

        public class RowInput : ISOMInput
        {
            public string[] Row { get; set; }
            public VectorND Weights { get; set; }
        }

        #endregion

        #region Class: OverlayPolygonStats

        /// <summary>
        /// This is the tooltip when they go over a node
        /// </summary>
        private class OverlayPolygonStats
        {
            public OverlayPolygonStats(SOMNode node, ISOMInput[] images, Rect canvasAABB, Vector cursorOffset, Canvas overlay)
            {
                this.Node = node;
                this.Images = images;
                this.CanvasAABB = canvasAABB;
                this.CursorOffset = cursorOffset;
                this.Overlay = overlay;
            }

            public readonly SOMNode Node;
            public readonly ISOMInput[] Images;

            public readonly Rect CanvasAABB;
            public readonly Vector CursorOffset;

            public readonly Canvas Overlay;
        }

        #endregion

        #region Declaration Section

        private const string TITLE = "Self Organizing Maps DB";

        private QueryResults _queryResults = null;
        private ColumnSetStats _columns = null;
        private SOMResult _result = null;
        private OverlayPolygonStats _overlayPolyStats = null;

        private readonly BackgroundTaskWorker<QueryRequest, QueryResults> _workerQuery;
        private readonly BackgroundTaskWorker<ColumnStatsRequest, ColumnSetStats> _workerColumns;
        private readonly BackgroundTaskWorker<SOMRequest, SOMResult> _workerSOM;

        private readonly Style _textSummaryHeaderStyle;
        private readonly Style _textSummaryItemFullStyle;
        private readonly Style _textSummaryItemPromptStyle;
        private readonly Style _textSummaryItemValueStyle;
        private readonly Style _textSummarySliderStyle;
        private readonly Style _detailsHeaderStyle;
        private readonly Style _detailsBodyStyle;

        private readonly List<Window> _childWindows = new List<Window>();

        private bool _initialized = false;

        #endregion

        #region Constructor

        public SelfOrganizingMapsDBWindow()
        {
            InitializeComponent();

            _textSummaryHeaderStyle = (Style)this.Resources["textSummaryHeader"];
            _textSummaryItemFullStyle = (Style)this.Resources["textSummaryItemFull"];
            _textSummaryItemPromptStyle = (Style)this.Resources["textSummaryItemPrompt"];
            _textSummaryItemValueStyle = (Style)this.Resources["textSummaryItemValue"];
            _textSummarySliderStyle = (Style)this.Resources["textSummarySlider"];
            _detailsHeaderStyle = (Style)this.Resources["detailsHeader"];
            _detailsBodyStyle = (Style)this.Resources["detailsBody"];


            txtConnectString.Text = "data source=<server>;initial catalog=<database>;integrated security=True";
            txtQuery.Text =
@"select
      A
      ,B
      ,C
  from DB.dbo.Table nolock";
            txtColumns.Text = @"A
C";


//            txtConnectString.Text = "data source=IS-OMAD617PRD;initial catalog=SOMTests;integrated security=True";

//            txtQuery.Text = @"select r.ChargeAmount, r.StatementDateFrom, r.FacilityTypeCode, r.DiagCd from 
//(select A.ChargeAmount, A.StatementDateFrom, A.FacilityTypeCode, B.DiagCd, ROW_NUMBER() OVER
//    (Partition BY A.ClaimKey ORDER BY B.DiagOrder ) RowNumber from dbo.ClaimFields A
//join dbo.DiagnosisCodes B on A.ClaimKey = b.ClaimKey ) r
//where r.RowNumber = 1
//";
//            txtColumns.Text = @"ChargeAmount
//DiagCd";


            _workerSOM = new BackgroundTaskWorker<SOMRequest, SOMResult>(DoWork_SOM, Finished_SOM, Exception_SOM);
            _workerColumns = new BackgroundTaskWorker<ColumnStatsRequest, ColumnSetStats>(DoWork_ColumnStats, Finished_ColumnStats, Exception_ColumnStats, new ICancel[] { _workerSOM });
            _workerQuery = new BackgroundTaskWorker<QueryRequest, QueryResults>(DoWork_Query, Finished_Query, Exception_Query, new ICancel[] { _workerColumns, _workerSOM });

            _initialized = true;
        }

        #endregion

        #region Public Methods

        public static RowInput[] GetSOMInputs(ColumnStats[] columns, QueryResults results, bool randomSample)
        {
            var finalColumns = GetFinalColumnStats(columns);

            IEnumerable<int> indices = randomSample ?
                UtilityCore.RandomRange(0, results.Results.Length, StaticRandom.Next(100, 200)) :
                Enumerable.Range(0, results.Results.Length);

            return indices.
                Select(o =>
                {
                    return new RowInput()
                    {
                        Row = results.Results[o],
                        Weights = finalColumns.
                            Select(p => SelfOrganizingMapsDB.ConvertToVector(results.Results[o][p.Item1.Index], p.Item2, p.Item3)).
                            SelectMany(p => p).
                            ToArray().
                            ToVectorND(),
                    };
                }).
                ToArray();
        }

        #endregion

        #region Event Listeners

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // This needs to wait until the window has finished sizing itself
                DoSOM();
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
                foreach (Window child in _childWindows.ToArray())       //cloning, because the child's closed event will remove from the list
                {
                    child.Close();
                }

                _childWindows.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SQL_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (!_initialized)
                {
                    return;
                }

                DoSOM();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ColumnTextCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                CheckBox senderCast = sender as CheckBox;
                if (senderCast == null)
                {
                    MessageBox.Show("Couldn't cast sender as a checkbox", TITLE, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ColumnStats column = senderCast.Tag as ColumnStats;
                if (column == null)
                {
                    MessageBox.Show("Couldn't cast sender.Tag as ColumnStats", TITLE, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                column.ForceText = senderCast.IsChecked.Value;

                _result = null;
                DoSOM();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ColumnSlider_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                SliderShowValues senderCast = sender as SliderShowValues;
                if (senderCast == null)
                {
                    MessageBox.Show("Couldn't cast sender as a SliderShowValues", TITLE, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ColumnStats column = senderCast.Tag as ColumnStats;
                if (column == null)
                {
                    MessageBox.Show("Couldn't cast sender.Tag as ColumnStats", TITLE, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                column.Width = senderCast.Value.ToInt_Round();

                _result = null;
                DoSOM();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void panelDisplay_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                if (_result == null)
                {
                    return;
                }

                var events = new SelfOrganizingMapsWPF.BlobEvents(Polygon_MouseMove, Polygon_MouseLeave, Polygon_Click);

                SelfOrganizingMapsWPF.ShowResults2D_Blobs(panelDisplay, _result, SelfOrganizingMapsWPF.GetNodeColor, events);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Polygon_MouseMove(Polygon poly, SOMNode node, ISOMInput[] inputs, MouseEventArgs e)
        {
            try
            {
                if (_overlayPolyStats == null || _overlayPolyStats.Node.Token != node.Token)
                {
                    BuildOverlay2D(node, inputs, true, true);
                }

                Point mousePos = e.GetPosition(panelDisplay);

                double left = mousePos.X + _overlayPolyStats.CanvasAABB.Left + _overlayPolyStats.CursorOffset.X - 1;
                //double top = mousePos.Y + _overlayPolyStats.CanvasAABB.Top + _overlayPolyStats.CursorOffset.Y - 1;
                //double top = mousePos.Y + _overlayPolyStats.CanvasAABB.Top - _overlayPolyStats.CursorOffset.Y - 1;
                double top = mousePos.Y + _overlayPolyStats.CanvasAABB.Top - 1;     // Y is already centered

                Canvas.SetLeft(_overlayPolyStats.Overlay, left);
                Canvas.SetTop(_overlayPolyStats.Overlay, top);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Polygon_MouseLeave()
        {
            try
            {
                _overlayPolyStats = null;
                panelOverlay.Children.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Polygon_Click(Polygon poly, SOMNode node, ISOMInput[] inputs, MouseEventArgs e)
        {
            try
            {
                int index = _result.Nodes.IndexOf(node, (o, p) => o.Token == p.Token);

                DBRowsDetail viewer = new DBRowsDetail(_queryResults, _columns, _result, index)
                {
                    Background = poly.Fill,
                };

                viewer.Closed += DBRowsDetail_Closed;
                _childWindows.Add(viewer);

                viewer.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DBRowsDetail_Closed(object sender, EventArgs e)
        {
            try
            {
                Window senderCast = sender as Window;
                if (senderCast == null)
                {
                    return;
                }

                _childWindows.Remove(senderCast);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// This looks at the current inputs, and starts a new SOM async
        /// </summary>
        private void DoSOM()
        {
            #region Init

            // Parse all the inputs
            QueryRequest queryReq = GetQueryRequest();

            if (queryReq.Columns.Length == 0)
            {
                lblErrorMessage.Text = "No columns specified";
                lblQueryStatus.Text = "";
                pnlColumns.Children.Clear();
                _queryResults = null;
                _columns = null;
                _result = null;
                return;
            }

            #endregion
            #region Query

            // Run query if changed
            if (_queryResults == null || _queryResults.ConnectionString != queryReq.ConnectionString || _queryResults.SQLStatement != queryReq.SQLStatement)
            {
                lblQueryStatus.Text = "Running query...";
                lblErrorMessage.Text = "";
                pnlColumns.Children.Clear();
                _columns = null;
                _result = null;

                _workerQuery.Start(queryReq);
                return;     //_workerQuery.finish will call this method again, and execution will flow past this if statement
            }

            lblQueryStatus.Text = "";

            if (!string.IsNullOrEmpty(_queryResults.Exception))
            {
                lblErrorMessage.Text = _queryResults.Exception;
                _result = null;
                return;
            }

            lblQueryStatus.Text = string.Format("{0} row{1}", _queryResults.Results.Length.ToString("N0"), _queryResults.Results.Length == 1 ? "" : "s");

            #endregion
            #region Columns

            if (_columns != null)
            {
                _columns = _columns.CloneIfSameNames(queryReq.Columns, _queryResults.ColumnNames);      //NOTE: This will still clone if it just holds an exception
            }

            if (_columns == null || !IsSame(queryReq.Columns, _columns.Names))
            {
                lblErrorMessage.Text = "";
                _result = null;
                _workerColumns.Start(new ColumnStatsRequest(queryReq.Columns, _queryResults));
                return;
            }

            if (_columns != null && !string.IsNullOrEmpty(_columns.Exception))
            {
                lblErrorMessage.Text = _columns.Exception;
                _result = null;
                return;
            }

            #endregion
            #region Do SOM

            if (_result == null)
            {
                RowInput[] inputs = GetSOMInputs(_columns.Columns, _queryResults, true);

                //TODO: Get these from the gui.  Add an option to randomize against their settings
                SOMRules rules = GetSOMRules_Rand();

                //TODO: Make an option for display1D.  Then do a SOM for each column and put the results in the column details dump
                _workerSOM.Start(new SOMRequest(inputs, rules));
                return;
            }

            #endregion
            #region Show Results

            var events = new SelfOrganizingMapsWPF.BlobEvents(Polygon_MouseMove, Polygon_MouseLeave, Polygon_Click);

            SelfOrganizingMapsWPF.ShowResults2D_Blobs(panelDisplay, _result, SelfOrganizingMapsWPF.GetNodeColor, events);

            #endregion
        }

        private static QueryResults DoWork_Query(QueryRequest req, CancellationToken cancel)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(req.ConnectionString))
                {
                    if (cancel.IsCancellationRequested) return new QueryResults(req.ConnectionString, req.SQLStatement, "cancelled");

                    connection.Open();

                    if (cancel.IsCancellationRequested) return new QueryResults(req.ConnectionString, req.SQLStatement, "cancelled");

                    using (SqlCommand command = new SqlCommand(req.SQLStatement, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (cancel.IsCancellationRequested) return new QueryResults(req.ConnectionString, req.SQLStatement, "cancelled");

                            string[] columnNames = null;
                            List<string[]> rows = new List<string[]>();

                            while (reader.Read())
                            {
                                if (cancel.IsCancellationRequested) return new QueryResults(req.ConnectionString, req.SQLStatement, "cancelled");

                                // Column Names
                                if (columnNames == null)
                                {
                                    columnNames = new string[reader.VisibleFieldCount];
                                    for (int cntr = 0; cntr < reader.VisibleFieldCount; cntr++)
                                    {
                                        columnNames[cntr] = reader.GetName(cntr);
                                    }
                                }

                                // Row Values
                                string[] row = new string[reader.VisibleFieldCount];
                                for (int cntr = 0; cntr < reader.VisibleFieldCount; cntr++)
                                {
                                    row[cntr] = reader.GetValue(cntr).ToString();
                                }

                                rows.Add(row);
                            }

                            if (cancel.IsCancellationRequested) return new QueryResults(req.ConnectionString, req.SQLStatement, "cancelled");

                            if (rows.Count == 0)
                            {
                                return new QueryResults(req.ConnectionString, req.SQLStatement, "No rows found");
                            }
                            else
                            {
                                return new QueryResults(req.ConnectionString, req.SQLStatement, columnNames, rows.ToArray());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new QueryResults(req.ConnectionString, req.SQLStatement, ex.Message);
            }
        }
        private void Finished_Query(QueryRequest req, QueryResults results)
        {
            _queryResults = results;
            DoSOM();
        }
        private void Exception_Query(QueryRequest req, Exception ex)
        {
            _queryResults = new QueryResults(req.ConnectionString, req.SQLStatement, ex.Message);
            DoSOM();
        }

        private static ColumnSetStats DoWork_ColumnStats(ColumnStatsRequest req, CancellationToken cancel)
        {
            var indices = GetIndices(req.Names, req.Results.ColumnNames);
            if (indices.Item2 != null)
            {
                return new ColumnSetStats(req.Names, indices.Item2);
            }

            ColumnStats[] columns = new ColumnStats[req.Names.Length];

            for (int cntr = 0; cntr < req.Names.Length; cntr++)
            {
                int index = indices.Item1[cntr];
                string[] values = req.Results.Results.
                    Select(o => o[index]).
                    ToArray();

                SOMFieldStats stats = SelfOrganizingMapsDB.GetFieldStats(values);
                SOMFieldStats textStats = null;

                switch (stats.FieldType)
                {
                    case SOMFieldType.AlphaNumeric:
                    case SOMFieldType.AnyText:
                        textStats = stats;
                        break;

                    default:
                        textStats = SelfOrganizingMapsDB.GetFieldStats(values, SOMFieldType.AnyText);
                        break;
                }

                columns[cntr] = new ColumnStats(req.Names[cntr], index, stats, textStats);
            }

            return new ColumnSetStats(columns);
        }
        private void Finished_ColumnStats(ColumnStatsRequest req, ColumnSetStats results)
        {
            _columns = results;
            ShowUsedColumnStats();

            DoSOM();
        }
        private void Exception_ColumnStats(ColumnStatsRequest req, Exception ex)
        {
            _columns = new ColumnSetStats(req.Names, ex.Message);
            pnlColumns.Children.Clear();

            DoSOM();
        }

        private static SOMResult DoWork_SOM(SOMRequest req, CancellationToken cancel)
        {
            return SelfOrganizingMaps.TrainSOM(req.Inputs, req.Rules, true);
        }
        private void Finished_SOM(SOMRequest req, SOMResult result)
        {
            _result = result;
            DoSOM();
        }
        private void Exception_SOM(SOMRequest req, Exception ex)
        {
            _result = new SOMResult(new SOMNode[0], new RowInput[0][], false);
            DoSOM();
        }

        private QueryRequest GetQueryRequest()
        {
            return new QueryRequest(
                txtConnectString.Text,
                txtQuery.Text,
                ColumnSetStats.GetColumnNames(txtColumns.Text));
        }

        private void ShowUsedColumnStats()
        {
            if (_columns == null || !string.IsNullOrEmpty(_columns.Exception))
            {
                return;
            }

            pnlColumns.Children.Clear();

            for (int cntr = 0; cntr < _columns.Names.Length; cntr++)
            {
                pnlColumns.Children.Add(GetColumnVisual(_columns.Columns[cntr], _textSummaryHeaderStyle, _textSummaryItemPromptStyle, _textSummaryItemValueStyle, _textSummarySliderStyle));
            }
        }

        private UIElement GetColumnVisual(ColumnStats column, Style headerStyle, Style promptStyle, Style valueStyle, Style sliderStyle)
        {
            Grid grid = new Grid();// { HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch, Background = Brushes.Yellow, };
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(10) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto), SharedSizeGroup = "itemPrompt" });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(5) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star), SharedSizeGroup = "itemValue" });

            #region header

            TextBlock text = new TextBlock()
            {
                Text = column.Name,
                Style = headerStyle,
            };

            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength() });
            Grid.SetColumn(text, 0);
            Grid.SetColumnSpan(text, 4);
            Grid.SetRow(text, grid.RowDefinitions.Count - 1);
            grid.Children.Add(text);

            #endregion
            #region datatype

            // Type
            text = new TextBlock()
            {
                Text = column.FieldStats.FieldType.ToString(),
                Style = promptStyle,
            };

            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(2) });
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength() });
            Grid.SetColumn(text, 1);
            Grid.SetRow(text, grid.RowDefinitions.Count - 1);
            grid.Children.Add(text);

            // Checkbox
            if (column.FieldStats.FieldType != SOMFieldType.AlphaNumeric && column.FieldStats.FieldType != SOMFieldType.AnyText)
            {
                CheckBox checkbox = new CheckBox()
                {
                    Content = "Force text",
                    Tag = column,
                };

                checkbox.Checked += ColumnTextCheckbox_Checked;
                checkbox.Unchecked += ColumnTextCheckbox_Checked;

                Grid.SetColumn(checkbox, 3);
                Grid.SetRow(checkbox, grid.RowDefinitions.Count - 1);
                grid.Children.Add(checkbox);
            }

            #endregion
            #region vector width

            // Prompt
            text = new TextBlock()
            {
                Text = "vector width",
                Style = promptStyle,
            };

            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(2) });
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength() });
            Grid.SetColumn(text, 1);
            Grid.SetRow(text, grid.RowDefinitions.Count - 1);
            grid.Children.Add(text);

            // Value
            SliderShowValues slider = new SliderShowValues()
            {
                Minimum = 1,
                Maximum = 10,
                Value = 5,
                IsInteger = true,
                Tag = column,
                Style = sliderStyle,
            };

            slider.ValueChanged += ColumnSlider_ValueChanged;

            Grid.SetColumn(slider, 3);
            Grid.SetRow(slider, grid.RowDefinitions.Count - 1);
            grid.Children.Add(slider);

            #endregion

            // counts
            AddDescriptionRow(grid, "count", column.FieldStats.Count.ToString("N0"), promptStyle, valueStyle);
            AddDescriptionRow(grid, "unique", column.FieldStats.UniqueCount.ToString("N0"), promptStyle, valueStyle);

            #region datatype specific fields

            switch (column.FieldStats.FieldType)
            {
                case SOMFieldType.AlphaNumeric:
                case SOMFieldType.AnyText:
                    ShowUsedColumnStats_Text(grid, column.FieldStats, promptStyle, valueStyle);
                    break;

                case SOMFieldType.DateTime:
                    ShowUsedColumnStats_Datetime(grid, column.FieldStats, promptStyle, valueStyle);
                    break;

                case SOMFieldType.FloatingPoint:
                case SOMFieldType.Integer:
                    ShowUsedColumnStats_Number(grid, column.FieldStats, promptStyle, valueStyle);
                    break;

                default:
                    throw new ApplicationException("Unknown SOMFieldType: " + column.FieldStats.FieldType.ToString());
            }

            #endregion

            return new Border()
            {
                Child = grid,
                Padding = new Thickness(4),
            };
        }

        private static void ShowUsedColumnStats_Text(Grid grid, SOMFieldStats stats, Style stylePrompt, Style styleValue)
        {
            AddDescriptionRow(grid, "min length", stats.MinLength.ToString("N0"), stylePrompt, styleValue);
            AddDescriptionRow(grid, "max length", stats.MaxLength.ToString("N0"), stylePrompt, styleValue);

            //UniqueChars
            //UniqueChars_NonWhitespace
        }
        private static void ShowUsedColumnStats_Datetime(Grid grid, SOMFieldStats stats, Style stylePrompt, Style styleValue)
        {
            AddDescriptionRow(grid, "min", GetDateTimeText(stats.Date_Min.Value), stylePrompt, styleValue);
            AddDescriptionRow(grid, "max", GetDateTimeText(stats.Date_Max.Value), stylePrompt, styleValue);
            AddDescriptionRow(grid, "average", GetDateTimeText(stats.Date_Avg.Value), stylePrompt, styleValue);
            AddDescriptionRow(grid, "std deviation", stats.Date_StandDev.Value.ToString(), stylePrompt, styleValue);
        }
        private static void ShowUsedColumnStats_Number(Grid grid, SOMFieldStats stats, Style stylePrompt, Style styleValue)
        {
            AddDescriptionRow(grid, "min", stats.Numeric_Min.Value.ToStringSignificantDigits(3), stylePrompt, styleValue);
            AddDescriptionRow(grid, "max", stats.Numeric_Max.Value.ToStringSignificantDigits(3), stylePrompt, styleValue);
            AddDescriptionRow(grid, "average", stats.Numeric_Avg.Value.ToStringSignificantDigits(3), stylePrompt, styleValue);
            AddDescriptionRow(grid, "std deviation", stats.Numeric_StandDev.Value.ToStringSignificantDigits(3), stylePrompt, styleValue);
        }

        private static void AddDescriptionRow(Grid grid, string prompt, string value, Style stylePrompt, Style styleValue)
        {
            // Prompt
            TextBlock text = new TextBlock()
            {
                Text = prompt,
                Style = stylePrompt,
            };

            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(2) });
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength() });
            Grid.SetColumn(text, 1);
            Grid.SetRow(text, grid.RowDefinitions.Count - 1);
            grid.Children.Add(text);

            // Value
            text = new TextBlock()
            {
                Text = value,
                Style = styleValue,
            };

            Grid.SetColumn(text, 3);
            Grid.SetRow(text, grid.RowDefinitions.Count - 1);
            grid.Children.Add(text);
        }

        private static string GetDateTimeText(DateTime date)
        {
            if (date.TimeOfDay.TotalMilliseconds.IsNearZero())
            {
                return date.ToShortDateString();
            }
            else
            {
                return date.ToString();
            }
        }

        private static Tuple<int[], string> GetIndices(string[] names, string[] allNames)
        {
            int[] retVal = new int[names.Length];

            for (int cntr = 0; cntr < names.Length; cntr++)
            {
                int index = allNames.IndexOf(names[cntr], (o, p) => o.Equals(p, StringComparison.OrdinalIgnoreCase));
                if (index < 0)
                {
                    //TODO: Report error
                    return Tuple.Create((int[])null, string.Format("Column not found: \"{0}\"", names[cntr]));
                }

                retVal[cntr] = index;
            }

            return Tuple.Create(retVal, (string)null);
        }

        private static Tuple<ColumnStats, SOMFieldStats, SOMConvertToVectorProps>[] GetFinalColumnStats(ColumnStats[] columns)
        {
            var retVal = new Tuple<ColumnStats, SOMFieldStats, SOMConvertToVectorProps>[columns.Length];

            for (int cntr = 0; cntr < columns.Length; cntr++)
            {
                // Field
                SOMFieldStats field = null;
                if (columns[cntr].ForceText)
                {
                    field = columns[cntr].FieldStatsText;
                }
                else
                {
                    field = columns[cntr].FieldStats;
                }

                //TODO: look at overrides



                // Convert
                SOMConvertToVectorProps convertProps;

                switch (field.FieldType)
                {
                    case SOMFieldType.AlphaNumeric:
                    case SOMFieldType.AnyText:
                        convertProps = new SOMConvertToVectorProps(columns[cntr].Width, columns[cntr].Override_Text_Justify ?? TextAlignment.Center);
                        break;

                    case SOMFieldType.DateTime:
                        convertProps = SelfOrganizingMapsDB.GetConvertToProps(field.Date_Min.Value, field.Date_Max.Value, columns[cntr].Width);
                        break;

                    case SOMFieldType.FloatingPoint:
                    case SOMFieldType.Integer:
                        convertProps = SelfOrganizingMapsDB.GetConvertToProps(field.Numeric_Min.Value, field.Numeric_Max.Value, columns[cntr].Width);
                        break;

                    default:
                        throw new ApplicationException("Unknown SOMFieldType: " + field.FieldType.ToString());
                }

                // Build it
                retVal[cntr] = Tuple.Create(columns[cntr], field, convertProps);
            }

            return retVal;
        }

        private static SOMRules GetSOMRules_Rand()
        {
            Random rand = StaticRandom.GetRandomForThread();

            return new SOMRules(
                rand.Next(15, 50),
                rand.Next(2000, 5000),
                rand.NextDouble(.2, .4),
                rand.NextDouble(.05, .15));
        }

        private void BuildOverlay2D(SOMNode node, ISOMInput[] inputs, bool showCount = false, bool showNodeHash = false)
        {
            const double SMALLFONT1 = 17;
            const double LARGEFONT1 = 21;
            const double SMALLFONT2 = 15;
            const double LARGEFONT2 = 18;
            const double SMALLFONT3 = 12;
            const double LARGEFONT3 = 14;

            const double SMALLLINE1 = .8;
            const double LARGELINE1 = 1;
            const double SMALLLINE2 = .5;
            const double LARGELINE2 = .85;
            const double SMALLLINE3 = .3;
            const double LARGELINE3 = .7;

            Canvas canvas = new Canvas();
            List<Rect> rectangles = new List<Rect>();

            #region cursor rectangle

            var cursorRect = SelfOrganizingMapsWPF.GetMouseCursorRect();
            rectangles.Add(cursorRect.Item1);

            // This is just for debugging
            //Rectangle cursorVisual = new Rectangle()
            //{
            //    Width = cursorRect.Item1.Width,
            //    Height = cursorRect.Item1.Height,
            //    Fill = new SolidColorBrush(UtilityWPF.GetRandomColor(64, 192, 255)),
            //};

            //Canvas.SetLeft(cursorVisual, cursorRect.Item1.Left);
            //Canvas.SetTop(cursorVisual, cursorRect.Item1.Top);

            //canvas.Children.Add(cursorVisual);

            #endregion

            #region count text

            if (showCount && _result != null && _result.InputsByNode != null)       // it's possible that they are changing things while old graphics are still showing
            {
                StackPanel textPanel = new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                };

                // "rows "
                OutlinedTextBlock text = SelfOrganizingMapsWPF.GetOutlineText("rows ", SMALLFONT1, SMALLLINE1);
                text.Margin = new Thickness(0, 0, 4, 0);
                textPanel.Children.Add(text);

                // percent
                double percent = -1;
                if (_result.InputsByNode.Length == 0)
                {
                    if (inputs.Length == 0)
                    {
                        percent = 0;
                    }
                    else
                    {
                        percent = -1;
                    }
                }
                else
                {
                    percent = inputs.Length.ToDouble() / _result.InputsByNode.SelectMany(o => o).Count().ToDouble();
                    percent *= 100d;
                }
                text = SelfOrganizingMapsWPF.GetOutlineText(percent.ToStringSignificantDigits(2) + "%", LARGEFONT1, LARGELINE1);
                textPanel.Children.Add(text);

                // Place on canvas
                textPanel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));       // aparently, the infinity is important to get an accurate desired size
                Size textSize = textPanel.DesiredSize;

                Rect textRect = SelfOrganizingMapsWPF.GetFreeSpot(textSize, new Point(0, 0), new Vector(0, -1), rectangles);
                rectangles.Add(textRect);

                Canvas.SetLeft(textPanel, textRect.Left);
                Canvas.SetTop(textPanel, textRect.Top);

                canvas.Children.Add(textPanel);
            }

            #endregion

            VectorND nodeCenter = inputs.Length == 0 ? node.Weights : MathND.GetCenter(inputs.Select(o => o.Weights));

            #region node hash

            if (showNodeHash)
            {
                var nodeCtrl = GetVectorVisual(node.Weights.VectorArray);

                // Place on canvas
                Rect nodeRect = SelfOrganizingMapsWPF.GetFreeSpot(new Size(nodeCtrl.Item2.X, nodeCtrl.Item2.Y), new Point(0, 0), new Vector(0, -1), rectangles);
                rectangles.Add(nodeRect);

                Canvas.SetLeft(nodeCtrl.Item1, nodeRect.Left);
                Canvas.SetTop(nodeCtrl.Item1, nodeRect.Top);

                canvas.Children.Add(nodeCtrl.Item1);
            }

            #endregion

            //TODO: For the first attempt, put all row descriptions under
            #region rows

            if (_queryResults != null && _queryResults.Results != null)
            {
                var rowsCtrl = GetRowsVisual(_queryResults.ColumnNames, inputs, _detailsHeaderStyle, _detailsBodyStyle);

                // Place on canvas
                Rect rowsRect = SelfOrganizingMapsWPF.GetFreeSpot(new Size(rowsCtrl.Item2.X, rowsCtrl.Item2.Y), new Point(0, 0), new Vector(0, 1), rectangles);
                rectangles.Add(rowsRect);

                Canvas.SetLeft(rowsCtrl.Item1, rowsRect.Left);
                Canvas.SetTop(rowsCtrl.Item1, rowsRect.Top);

                canvas.Children.Add(rowsCtrl.Item1);
            }

            #endregion

            Rect canvasAABB = Math2D.GetAABB(rectangles);

            //NOTE: All the items are placed around zero zero, but that may not be half width and height (items may not be centered)
            canvas.RenderTransform = new TranslateTransform(-canvasAABB.Left, -canvasAABB.Top);

            panelOverlay.Children.Clear();
            panelOverlay.Children.Add(canvas);

            _overlayPolyStats = new OverlayPolygonStats(node, inputs, canvasAABB, cursorRect.Item2, canvas);
        }

        private static Tuple<UIElement, VectorInt> GetRowsVisual(string[] columnNames, ISOMInput[] inputs, Style headerStyle, Style bodyStyle)
        {
            Grid grid = new Grid();

            #region Header

            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });

            // Header
            for (int cntr = 0; cntr < columnNames.Length; cntr++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });

                TextBlock text = new TextBlock()
                {
                    Text = columnNames[cntr],
                    Style = headerStyle,
                };

                Grid.SetColumn(text, cntr);
                Grid.SetRow(text, grid.RowDefinitions.Count - 1);

                grid.Children.Add(text);
            }

            #endregion

            //TODO: Sort by distance.  Call this with 1D coords
            //SelfOrganizingMaps.ArrangeNodes_LikesAttract()
            foreach (RowInput input in UtilityCore.RandomOrder(inputs, 300))       // Mix it up.  Also getting exceptions if too much is added, and it won't all be shown anyway
            {
                #region Body Row

                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });

                // Header
                for (int cntr = 0; cntr < columnNames.Length; cntr++)
                {
                    grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });

                    TextBlock text = new TextBlock()
                    {
                        Text = input.Row[cntr],
                        Style = bodyStyle,
                        HorizontalAlignment = HorizontalAlignment.Right,
                    };

                    Grid.SetColumn(text, cntr);
                    Grid.SetRow(text, grid.RowDefinitions.Count - 1);

                    grid.Children.Add(text);
                }

                #endregion
            }

            Border retVal = new Border()
            {
                Child = grid,
                Background = new SolidColorBrush(UtilityWPF.ColorFromHex("D0F7F1DA")),
                BorderBrush = new SolidColorBrush(UtilityWPF.ColorFromHex("B8B4A0")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(8),
            };

            retVal.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));       // aparently, the infinity is important to get an accurate desired size
            Size returnSize = retVal.DesiredSize;

            return Tuple.Create((UIElement)retVal, new VectorInt(returnSize.Width.ToInt_Ceiling(), returnSize.Height.ToInt_Ceiling()));
        }

        private static Tuple<UIElement, VectorInt> GetVectorVisual(double[] values)
        {
            const int VECTORWIDTH = 100;

            double? maxValue = values.Max() > 1d ? (double?)null : 1d;

            Convolution2D conv = new Convolution2D(values, values.Length, 1, false);      //TODO: have a flag that tells whether there are any negatives
            BitmapSource bitmap = Convolutions.GetBitmap_Aliased(conv, absMaxValue: maxValue, negPosColoring: ConvolutionResultNegPosColoring.BlackWhite, forcePos_WhiteBlack: false);

            bitmap = UtilityWPF.ResizeImage(bitmap, VECTORWIDTH, true);

            Image retVal = new Image()
            {
                Source = bitmap,
                Width = bitmap.PixelWidth,
            };

            return Tuple.Create((UIElement)retVal, new VectorInt(bitmap.PixelWidth, bitmap.PixelHeight));
        }

        private static bool IsSame(string[] arr1, string[] arr2)
        {
            if ((arr1 == null && arr2 != null) || (arr1 != null && arr2 == null))
            {
                return false;
            }
            else if (arr1 == null || arr2 == null)
            {
                return true;
            }
            else if (arr1.Length != arr2.Length)
            {
                return false;
            }

            for (int cntr = 0; cntr < arr1.Length; cntr++)
            {
                if (arr1[cntr] != arr2[cntr])
                {
                    return false;
                }
            }

            return true;
        }

        #endregion
    }
}
