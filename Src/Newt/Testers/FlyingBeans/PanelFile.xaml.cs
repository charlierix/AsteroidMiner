using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Game.HelperClasses;
using Game.Newt.AsteroidMiner2;
using System.Threading.Tasks;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xaml;

namespace Game.Newt.Testers.FlyingBeans
{
    public partial class PanelFile : UserControl
    {
        #region Events

        public event EventHandler SessionChanged = null;

        #endregion

        #region Declaration Section

        private const string MSGBOXCAPTION = "PanelFile";
        private const string TIMESTAMPFORMAT = "yyyy-MM-dd HH.mm.ss.fff";
        private const string BEANSUBFOLDER = "Beans";

        private SortedList<string, ShipDNA> _defaultBeanList = null;

        private bool _isInitialized = false;

        #endregion

        #region Constructor

        public PanelFile(string sessionFolder, FlyingBeanSession session, FlyingBeanOptions options, ItemOptions itemOptions, SortedList<string, ShipDNA> defaultBeanList)
        {
            InitializeComponent();

            _defaultBeanList = defaultBeanList;

            this.SessionFolder = sessionFolder;
            this.Session = session;
            this.Options = options;
            this.ItemOptions = itemOptions;

            _isInitialized = true;
        }

        #endregion

        #region Public Properties

        public FlyingBeanSession Session
        {
            get;
            private set;
        }
        public FlyingBeanOptions Options
        {
            get;
            private set;
        }
        public ItemOptions ItemOptions
        {
            get;
            private set;
        }

        public PanelBeanTypes BeanTypesPanel
        {
            get;
            set;
        }

        public string SessionFolder
        {
            get;
            private set;
        }

        #endregion

        #region Public Methods

        public void New(bool startEmpty, bool randomSettings)
        {
            FlyingBeanOptions beanOptions = new FlyingBeanOptions();
            ItemOptions itemOptions = new ItemOptions();

            if (randomSettings)
            {
                double newBeanProb = beanOptions.NewBeanProbOfWinner;
                double scanFrequency = beanOptions.TrackingScanFrequencySeconds;

                beanOptions = MutateUtility.MutateSettingsObject(beanOptions, new MutateUtility.MuateArgs(.5d));

                beanOptions.NewBeanProbOfWinner = newBeanProb;		// leave these one alone
                beanOptions.TrackingScanFrequencySeconds = scanFrequency;

                itemOptions = MutateUtility.MutateSettingsObject(itemOptions, new MutateUtility.MuateArgs(.5d));
            }

            FinishLoad(new FlyingBeanSession(), beanOptions, itemOptions, null, null, startEmpty);
        }

        public bool TryLoadLastSave(bool useCurrentSessionIfSet)
        {
            string saveFolder;

            #region Find latest save folder

            if (this.SessionFolder == null || !useCurrentSessionIfSet)
            {
                //C:\Users\<username>\AppData\Roaming\Asteroid Miner\Flying Beans\
                string baseFolder = UtilityHelper.GetOptionsFolder();
                string beanFolder = System.IO.Path.Combine(baseFolder, FlyingBeansWindow.OPTIONSFOLDER);
                if (!Directory.Exists(beanFolder))
                {
                    // Not even this folder exists, there won't be any saves
                    return false;
                }

                string sessionFolderName = null;

                // See if the session file is here
                string sessionFilename = System.IO.Path.Combine(baseFolder, FlyingBeanSession.FILENAME_SESSION);
                if (File.Exists(sessionFilename))
                {
                    try
                    {
                        using (FileStream stream = new FileStream(sessionFilename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            FlyingBeanSession session = (FlyingBeanSession)XamlServices.Load(stream);		// it's a tiny file, it's cheap to deserialize here

                            if (Directory.Exists(session.SessionFolder))		// it may have been deleted
                            {
                                sessionFolderName = session.SessionFolder;
                            }
                        }
                    }
                    catch (Exception) { }
                }

                if (sessionFolderName == null)
                {
                    string[] sessionFolders = Directory.GetDirectories(beanFolder, FlyingBeansWindow.SESSIONFOLDERPREFIX + " *").OrderByDescending(o => o).ToArray();
                    if (sessionFolders == null || sessionFolders.Length == 0)
                    {
                        // There are no session folders
                        return false;
                    }

                    sessionFolderName = sessionFolders[0];
                }

                saveFolder = FindLatestSaveFolder(sessionFolderName);
                if (saveFolder == null)
                {
                    // Didn't find a save folder
                    return false;
                }

                // Remember the session folder
                this.SessionFolder = sessionFolderName;
            }
            else
            {
                // Use the current session
                saveFolder = FindLatestSaveFolder(this.SessionFolder);
                if (saveFolder == null)
                {
                    return false;
                }
            }

            #endregion

            Load(saveFolder);

            return true;
        }

        public void Save(bool async, bool pruneAutosavesAfter, string name)
        {
            const int MAXAUTOSAVES = 8;

            string baseFolder = UtilityHelper.GetOptionsFolder();
            string saveFolder = GetNewSaveFolder(name);

            FlyingBeanSession session = this.Session ?? new FlyingBeanSession();
            FlyingBeanOptions options = this.Options;
            ItemOptions itemOptions = this.ItemOptions;
            string sessionFolder = this.SessionFolder;

            // It doesn't matter what folder was stored there before, make sure it holds where the last save is
            session.SessionFolder = sessionFolder;

            if (async)
            {
                Task.Factory.StartNew(() =>
                    {
                        FlyingBeanSession.Save(baseFolder, saveFolder, session, options, itemOptions);

                        if (pruneAutosavesAfter)
                        {
                            PruneAutosaves(sessionFolder, MAXAUTOSAVES);
                        }
                    });
            }
            else
            {
                FlyingBeanSession.Save(baseFolder, saveFolder, session, options, itemOptions);

                if (pruneAutosavesAfter)
                {
                    PruneAutosaves(sessionFolder, MAXAUTOSAVES);
                }
            }
        }

        #endregion

        #region Event Listeners

        // Session
        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                New(chkNewStartEmpty.IsChecked.Value, chkNewRandSettings.IsChecked.Value);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnOpen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();

                if (this.SessionFolder != null)
                {
                    dialog.SelectedPath = this.SessionFolder;
                }
                else
                {
                    string baseFolder = UtilityHelper.GetOptionsFolder();

                    baseFolder = System.IO.Path.Combine(baseFolder, FlyingBeansWindow.OPTIONSFOLDER);
                    if (Directory.Exists(baseFolder))
                    {
                        dialog.SelectedPath = baseFolder;
                    }
                }

                dialog.Description = "Select save folder";
                if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                try
                {
                    Load(dialog.SelectedPath);
                }
                catch (ArgumentException ex)
                {
                    MessageBox.Show(ex.Message, MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Save(true, false, txtSaveName.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Bean
        private void btnImport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (this.BeanTypesPanel == null)
                {
                    MessageBox.Show("Reference to the beantypes panel was never set", MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // The bean types panel already has this functionality.  But I wanted to expose an import button off the file panel to sit next to the export
                // button
                this.BeanTypesPanel.AddFromFile(BEANSUBFOLDER);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get the best of each bean type
                var topBeans = GetTopBeans();
                if (topBeans == null || topBeans.Length == 0)
                {
                    MessageBox.Show("There are no high scoring beans yet", MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                //TODO: May want to ask the user to pick one

                // Make sure the folder exists
                string foldername = PanelBeanTypes.EnsureShipFolderExists(BEANSUBFOLDER);

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss.fff");

                // Write a file for each
                foreach (var bean in topBeans)
                {
                    if (bean.Item2 == null)
                    {
                        throw new ApplicationException("ShipDNA should never be null from the finals list");
                    }

                    // Build up filename
                    string filename = timestamp + " - " + bean.Item1 + " - " + Math.Round(bean.Item3, 1).ToString();
                    if (txtExportName.Text != "")
                    {
                        filename += " (" + FlyingBeanSession.EscapeFilename(txtExportName.Text) + ")";
                    }

                    filename += ".xml";

                    filename = System.IO.Path.Combine(foldername, filename);

                    // Write it
                    using (FileStream stream = new FileStream(filename, FileMode.CreateNew))
                    {
                        XamlServices.Save(stream, bean.Item2);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), MSGBOXCAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private void Load(string saveFolder)
        {
            string baseFolder = UtilityHelper.GetOptionsFolder();

            FlyingBeanSession session;
            FlyingBeanOptions options;
            ItemOptions itemOptions;
            ShipDNA[] winningBeans;
            FlyingBeanSession.Load(out session, out options, out itemOptions, out winningBeans, baseFolder, saveFolder);

            FinishLoad(session, options, itemOptions, winningBeans, saveFolder, false);
        }

        private void FinishLoad(FlyingBeanSession session, FlyingBeanOptions options, ItemOptions itemOptions, ShipDNA[] winningBeans, string saveFolder, bool startEmpty)
        {
            // Manually instantiate some of the properties that didn't get serialized
            options.DefaultBeanList = _defaultBeanList;

            if (options.NewBeanList == null)		// if this was called by new, it will still be null
            {
                options.NewBeanList = new SortedList<string, ShipDNA>();

                if (!startEmpty)
                {
                    string[] beanKeys = options.DefaultBeanList.Keys.ToArray();

                    foreach (int keyIndex in UtilityHelper.RandomRange(0, beanKeys.Length, Math.Min(3, beanKeys.Length)))		// only do 3 of the defaults
                    {
                        string key = beanKeys[keyIndex];
                        options.NewBeanList.Add(key, options.DefaultBeanList[key]);
                    }
                }
            }

            options.MutateArgs = PanelMutation.BuildMutateArgs(options);

            options.GravityField = new GravityFieldUniform();
            options.Gravity = options.Gravity;		// the property set modifies the gravity field

            options.WinnersLive = new WinnerList(true, options.TrackingMaxLineagesLive, options.TrackingMaxPerLineageLive);

            if (options.WinnersFinal == null)		// if a previous save had winning ships, this will already be loaded with them
            {
                options.WinnersFinal = new WinnerList(false, options.TrackingMaxLineagesFinal, options.TrackingMaxPerLineageFinal);
            }

            options.WinnerCandidates = new CandidateWinners();
            // These are already in the final list, there's no point in putting them in the candidate list as well
            //if (winningBeans != null)
            //{
            //    foreach (ShipDNA dna in winningBeans)
            //    {
            //        options.WinnerCandidates.Add(dna);
            //    }
            //}

            // Make sure the session folder is up to date
            if (saveFolder == null)
            {
                this.SessionFolder = null;
            }
            else
            {
                string dirChar = Regex.Escape(System.IO.Path.DirectorySeparatorChar.ToString());

                string pattern = dirChar + Regex.Escape(FlyingBeansWindow.SESSIONFOLDERPREFIX) + "[^" + dirChar + "]+(?<upto>" + dirChar + ")" + Regex.Escape(FlyingBeansWindow.SAVEFOLDERPREFIX);
                Match match = Regex.Match(saveFolder, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    this.SessionFolder = saveFolder.Substring(0, match.Groups["upto"].Index);		// the session folder is everything before the save subfolder
                }
                else
                {
                    // They may have chosen a folder that they unziped onto their desktop, or wherever.  Leaving this null so that if they hit save, it will be saved
                    // in the appropriate location
                    this.SessionFolder = null;
                }
            }

            // Swap out the settings
            this.Session = session;
            this.Options = options;
            this.ItemOptions = itemOptions;

            // Inform the world
            if (this.SessionChanged != null)
            {
                this.SessionChanged(this, new EventArgs());
            }
        }

        private string GetNewSaveFolder(string name)
        {
            //C:\Users\username\AppData\Roaming\Asteroid Miner\Flying Beans\Session 2013-02-16 15.20.00.000\Save 2013-02-16 15.20.000

            string timestamp = DateTime.Now.ToString(TIMESTAMPFORMAT);

            // Session folder
            if (this.SessionFolder == null)
            {
                string baseFolder = UtilityHelper.GetOptionsFolder();
                baseFolder = System.IO.Path.Combine(baseFolder, FlyingBeansWindow.OPTIONSFOLDER);
                if (!Directory.Exists(baseFolder))
                {
                    Directory.CreateDirectory(baseFolder);
                }

                this.SessionFolder = System.IO.Path.Combine(baseFolder, FlyingBeansWindow.SESSIONFOLDERPREFIX + " " + timestamp);
            }

            if (!Directory.Exists(this.SessionFolder))
            {
                Directory.CreateDirectory(this.SessionFolder);
            }

            // Save folder
            //NOTE: This class doesn't store the current save folder, just the parent session
            string retVal = System.IO.Path.Combine(this.SessionFolder, FlyingBeansWindow.SAVEFOLDERPREFIX + " " + timestamp);
            if (!string.IsNullOrEmpty(name))
            {
                retVal += " " + FlyingBeanSession.EscapeFilename(name);
            }

            Directory.CreateDirectory(retVal);

            // Exit Function
            return retVal;
        }

        private static string FindLatestSaveFolder(string sessionFolder)
        {
            string[] saveFolders = Directory.GetDirectories(sessionFolder, FlyingBeansWindow.SAVEFOLDERPREFIX + " *").OrderByDescending(o => o).ToArray();
            if (saveFolders == null || saveFolders.Length == 0)
            {
                // There are no save folders
                return null;
            }

            return saveFolders[0];
        }

        /// <summary>
        /// This only touches autosave folders.  If there are more folders than wanted, this will delete the excess folders, and
        /// the folders remaining will be as evenly spaced by time as possible
        /// </summary>
        private static void PruneAutosaves(string sessionFolder, int numToKeep)
        {
            // Get autosave folders
            var autosaveFolders = PruneAutosavesSprtCreateTimes(GetAutosaveFolders(sessionFolder));
            if (autosaveFolders == null || autosaveFolders.Length <= numToKeep)
            {
                return;
            }

            // Keep the first and last.  Try to keep them evenly spaced in between.  Get the folders that most line up with the sub elapsed times
            int[] bestFits = PruneAutosavesSprtBestFits(autosaveFolders, numToKeep);

            // Return the extra indices as sets
            List<int>[] deletableSets = PruneAutosavesSprtDeletable(bestFits);

            // Pick the most expendable indices
            int[] deletes = PruneAutosavesSprtGetDeletes(deletableSets, autosaveFolders.Length - numToKeep);

            // Delete these folders (wrapping in a try block just in case a file is referenced or something)
            foreach (int index in deletes)
            {
                try
                {
                    Directory.Delete(autosaveFolders[index].Item1, true);
                }
                catch (Exception) { }
            }
        }
        private static Tuple<string, DateTime>[] PruneAutosavesSprtCreateTimes(string[] autosaveFolders)
        {
            // I decided not to go with the folder create date
            //return autosaveFolders.Select(o => Tuple.Create(o, Directory.GetCreationTime(o))).ToArray();

            if (autosaveFolders == null || autosaveFolders.Length == 0)
            {
                return null;
            }

            // Since all the alphas in TIMESTAMPFORMAT represent some kind of number, just change them all to \d
            string datePattern = Regex.Replace(Regex.Escape(TIMESTAMPFORMAT), "[A-Za-z]", @"\d");		// need to escape before replacing with \d, or will end up with \\d's

            List<Tuple<string, DateTime>> retVal = new List<Tuple<string, DateTime>>();

            for (int cntr = 0; cntr < autosaveFolders.Length; cntr++)
            {
                // Only look at the save folder
                string name = System.IO.Path.GetFileName(autosaveFolders[cntr]);

                // Get the date portion from the folder name
                Match match = Regex.Match(name, datePattern);
                if (!match.Success)
                {
                    continue;		// the only way it wouldn't be a success is if someone messed with the folder name
                }

                // Cast it
                DateTime date;
                if (!DateTime.TryParseExact(match.Value, TIMESTAMPFORMAT, null, System.Globalization.DateTimeStyles.None, out date))
                {
                    continue;
                }

                retVal.Add(Tuple.Create(autosaveFolders[cntr], date));
            }

            return retVal.ToArray();
        }
        private static int[] PruneAutosavesSprtBestFits(Tuple<string, DateTime>[] savesAndTimes, int sliceCount)
        {
            // Get the elapsed time between the first and last snapshot
            double elapsed = (savesAndTimes[savesAndTimes.Length - 1].Item2 - savesAndTimes[0].Item2).TotalMinutes;
            double averageElapsed = elapsed / Convert.ToDouble(sliceCount - 1);

            //Item1 = slice index
            //Item2 = savesAndTimes index
            //Item3 = distance to slice time
            List<Tuple<int, int, double>> closest = new List<Tuple<int, int, double>>();

            for (int cntr = 0; cntr < sliceCount; cntr++)
            {
                DateTime sliceTime = savesAndTimes[0].Item2 + TimeSpan.FromMinutes(cntr * averageElapsed);

                int currentIndex = -1;
                double currentDist = double.MaxValue;

                // Find the index that is closest to this time
                for (int inner = 0; inner < savesAndTimes.Length; inner++)
                {
                    double distance = Math.Abs((savesAndTimes[inner].Item2 - sliceTime).TotalMinutes);

                    if (distance < currentDist)
                    {
                        currentIndex = inner;
                        currentDist = distance;
                    }
                }

                // See if one of the previous winners is farther than this
                var existingClosest = closest.Where(o => o.Item2 == currentIndex).FirstOrDefault();
                if (existingClosest != null)
                {
                    if (existingClosest.Item3 < currentDist)
                    {
                        // Keep this one
                        continue;
                    }
                    else
                    {
                        // Keep the new one, throw this one out
                        closest.Remove(existingClosest);
                    }
                }

                // Store it
                closest.Add(Tuple.Create(cntr, currentIndex, currentDist));
            }

            // Exit Function
            return closest.Select(o => o.Item2).ToArray();		// just return the index into savesAndTimes
        }
        private static List<int>[] PruneAutosavesSprtDeletable(int[] bestFits)
        {
            List<List<int>> retVal = new List<List<int>>();

            for (int cntr = 0; cntr < bestFits.Length - 1; cntr++)
            {
                if (bestFits[cntr + 1] - bestFits[cntr] > 1)
                {
                    // There are indices in between.  Store each of those indices
                    retVal.Add(Enumerable.Range(bestFits[cntr] + 1, (bestFits[cntr + 1] - bestFits[cntr]) - 1).ToList());
                }
            }

            return retVal.ToArray();
        }
        private static int[] PruneAutosavesSprtGetDeletes(List<int>[] deletableSets, int numToDelete)
        {
            if (deletableSets.Sum(o => o.Count) == numToDelete)
            {
                // Everything in deletable sets needs to be deleted, so just return them all instead of taking the expense of load balancing
                return deletableSets.SelectMany(o => o).ToArray();
            }

            List<int> retVal = new List<int>();

            for (int cntr = 0; cntr < numToDelete; cntr++)
            {
                // Get the largest sets
                int maxSize = deletableSets.Max(o => o.Count);
                List<int>[] largest = deletableSets.Where(o => o.Count == maxSize).ToArray();

                // Pick a random set, and a random index within that set
                int setIndex = StaticRandom.Next(largest.Length);
                int innerIndex = StaticRandom.Next(largest[setIndex].Count);

                // Store the value pointed to
                retVal.Add(largest[setIndex][innerIndex]);

                // Now remove this from the set
                largest[setIndex].RemoveAt(innerIndex);
            }

            return retVal.ToArray();
        }

        private static string[] GetAutosaveFolders(string sessionFolder)
        {
            if (sessionFolder == null || !Directory.Exists(sessionFolder))
            {
                return null;
            }

            List<string> retVal = new List<string>();

            foreach (string foldername in Directory.GetDirectories(sessionFolder))
            {
                string subfolder = System.IO.Path.GetFileName(foldername);

                if (subfolder.StartsWith(FlyingBeansWindow.SAVEFOLDERPREFIX) && subfolder.EndsWith(FlyingBeansWindow.AUTOSAVESUFFIX))
                {
                    retVal.Add(foldername);
                }
            }

            return retVal.ToArray();
        }

        private Tuple<string, ShipDNA, double>[] GetTopBeans()
        {
            // Get a dump of the hall of fame
            var dumpFinal = this.Options.WinnersFinal.Current;
            if (dumpFinal == null || dumpFinal.Length == 0)
            {
                return null;
            }

            List<Tuple<string, ShipDNA, double>> retVal = new List<Tuple<string, ShipDNA, double>>();

            foreach (var name in dumpFinal)
            {
                // Grab the highest scoring bean
                var topBean = name.BeansByLineage.SelectMany(o => o.Item2).OrderByDescending(o => o.Score).First();

                retVal.Add(Tuple.Create(name.ShipName, topBean.DNA, topBean.Score));
            }

            // Exit Function
            return retVal.ToArray();
        }

        #endregion
    }
}
