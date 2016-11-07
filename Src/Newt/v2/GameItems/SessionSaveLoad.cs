using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xaml;
using Game.HelperClassesCore;

namespace Game.Newt.v2.GameItems
{
    public static class SessionSaveLoad
    {
        #region Declaration Section

        public const int MAXAUTOSAVES = 8;

        private const string SESSION = "Session ";
        private const string SAVE = "Save ";
        private const string TIMESTAMPFORMAT = "yyyy-MM-dd HH.mm.ss.fff";

        private const string TIMESTAMPREGEX = @"\d{4}(-\d{2}){2} (\d{2}\.){3}\d{3}";

        #endregion

        /// <summary>
        /// This overload uses the standard base folder
        /// </summary>
        public static void Save(string name, ISessionOptions session, IEnumerable<Tuple<string, object>> saveFiles, bool zipped)
        {
            string baseFolder = UtilityCore.GetOptionsFolder();
            Save(baseFolder, name, session, saveFiles, zipped);
        }
        /// <summary>
        /// This will save the state of a game/tester
        /// </summary>
        /// <remarks>
        /// Folder Structure:
        ///     baseFolder
        ///         "C:\Users\charlie.rix\AppData\Roaming\Asteroid Miner"
        ///         Only one file per tester here.  Each file is "testername Options.xml"
        ///         Each tester gets one child folder off of base
        /// 
        ///     Game Folder
        ///         "C:\Users\charlie.rix\AppData\Roaming\Asteroid Miner\Flying Beans"
        ///         This folder holds custom files for this game.  It will also hold session subfolders (each session would be a different state)
        ///     
        ///     Session Folders
        ///         "C:\Users\charlie.rix\AppData\Roaming\Asteroid Miner\Flying Beans\Session 2016-10-09 09.11.35.247"
        ///     
        ///     Save Folders
        ///         "C:\Users\charlie.rix\AppData\Roaming\Asteroid Miner\Flying Beans\Session 2016-10-09 09.11.35.247\Save 2016-10-09 09.11.35.247 (auto)"
        ///         This is where all the save folders go.  Each save folder is for the parent session (there are many because backups should occur often)
        ///         When saving, it shouldn't overwrite an existing save, it should create a new save folder, then delete old ones as needed
        ///         If zipped is true, this will be a zip file instead of a folder
        /// </remarks>
        /// <param name="baseFolder">
        /// Use UtilityCore.GetOptionsFolder()
        /// "C:\Users\charlie.rix\AppData\Roaming\Asteroid Miner"
        /// </param>
        /// <param name="name">
        /// This is the name of the tester/game
        /// </param>
        /// <param name="session">
        /// This is the class that goes in the root folder
        /// NOTE: This method will set the LatestSessionFolder property
        /// </param>
        /// <param name="saveFiles">
        /// A set of files to put into the save folder (or zip file)
        /// Item1=Name of file (this method will ensure it ends in .xml)
        /// Item2=Object to serialize
        /// </param>
        /// <param name="zipped">
        /// This only affects the leaf save folder
        /// True: The save folder will be a zip file instead
        /// False: The save folder will be a standard folder containing files
        /// </param>
        public static void Save(string baseFolder, string name, ISessionOptions session, IEnumerable<Tuple<string, object>> saveFiles, bool zipped)
        {
            if (string.IsNullOrEmpty(session.LatestSessionFolder) || !Directory.Exists(session.LatestSessionFolder))
            {
                session.LatestSessionFolder = GetNewSessionFolderName(baseFolder, name);
            }

            #region root session file

            string rootSessionFilename = GetRootOptionsFilename(baseFolder, name);

            using (FileStream stream = new FileStream(rootSessionFilename, FileMode.Create))
            {
                XamlServices.Save(stream, session);
            }

            #endregion

            // Session Folder
            //NOTE: This will also create the game folder if it doesn't exist
            Directory.CreateDirectory(session.LatestSessionFolder);

            // Save Folder (or zip file)
            string saveFolder = GetNewSaveFolderName(session.LatestSessionFolder);
            if (zipped)
            {
                // Just tack on a .zip to the save folder name to turn it into a zip filename
                throw new ApplicationException("finish saving to zip file");
            }
            else
            {
                Save_Folder(saveFolder, saveFiles);
            }
        }

        /// <summary>
        /// This overload uses the standard base folder
        /// </summary>
        public static SessionFolderResults Load(string name, Func<ISessionOptions> getSession = null)
        {
            string baseFolder = UtilityCore.GetOptionsFolder();
            return Load(baseFolder, name);
        }
        /// <summary>
        /// This will load files from a saved game
        /// </summary>
        /// <param name="baseFolder">
        /// This can be one of 4 folders:
        ///     baseFolder
        ///         "C:\Users\charlie.rix\AppData\Roaming\Asteroid Miner"
        ///         May contain a session file that points to the latest save
        ///         Or contains the Game subfolder that then contains sessions (will load the latest session)
        /// 
        ///     Game Folder
        ///         "C:\Users\charlie.rix\AppData\Roaming\Asteroid Miner\Flying Beans"
        ///         Will load the latest session subfolder
        ///     
        ///     Session Folders
        ///         "C:\Users\charlie.rix\AppData\Roaming\Asteroid Miner\Flying Beans\Session 2016-10-09 09.11.35.247"
        ///         Will load the latest save subfolder
        ///     
        ///     Save Folders
        ///         "C:\Users\charlie.rix\AppData\Roaming\Asteroid Miner\Flying Beans\Session 2016-10-09 09.11.35.247\Save 2016-10-09 09.11.35.247 (auto)"
        ///         Will load this exact save (and point the session file to this folder's parent)
        ///         If this save folder isn't sitting in the standard hierarchy (ex: folder copied to the desktop), the session file will point directly to this location
        /// </param>
        /// <param name="name">This is the name of the tester/game</param>
        /// <param name="getSession">
        /// If the folder pointed isn't the root folder, the session file won't be found.  This delegate gives the caller a chance to create a session file that
        /// will be returned in the result object.  This method will then populate the LatestSessionFolder with sessionFolder ?? saveFolder
        /// </param>
        public static SessionFolderResults Load(string baseFolder, string name, Func<ISessionOptions> getSession = null)
        {
            // Find session file.  Else walk folders to get session folder and save folder/zip
            string sessionFolder = null;
            string saveFolder = null;

            ISessionOptions session = AttemptBaseFolder(baseFolder, name);

            if (session != null)
            {
                #region has session file

                if (session.LatestSessionFolder == null || !AttemptSessionSaveFolder(out sessionFolder, out saveFolder, session.LatestSessionFolder))
                {
                    // The session file exists, so baseFolder is expected to be pointing to the folder returned by UtilityCore.GetOptionsFolder()
                    // See if the name folder is there (and contains a session folder)
                    string nameFolder = Path.Combine(baseFolder, name);

                    if (!AttemptNameFolder(out sessionFolder, out saveFolder, nameFolder))
                    {
                        return null;
                    }
                }

                #endregion
            }
            else
            {
                #region no session file

                if (!AttemptNameFolder(out sessionFolder, out saveFolder, baseFolder))
                {
                    if (!AttemptSessionSaveFolder(out sessionFolder, out saveFolder, baseFolder))
                    {
                        return null;
                    }
                }

                #endregion
            }

            // Deserialize the files in this save folder
            Tuple<string, object>[] saveFiles = Load_Specific(saveFolder);

            if (saveFiles == null || saveFiles.Length == 0)
            {
                return null;
            }

            if (session == null && getSession != null)
            {
                session = getSession();
                if (session != null)
                {
                    session.LatestSessionFolder = sessionFolder ?? saveFolder;
                }
            }

            return new SessionFolderResults(session, baseFolder, sessionFolder, saveFolder, saveFiles);
        }

        /// <summary>
        /// Returns the names and deserialized objects in the save folder
        /// </summary>
        /// <param name="saveFolder">Either a folder or zip file</param>
        public static Tuple<string, object>[] Load_Specific(string saveFolder)
        {
            if (saveFolder.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                throw new ApplicationException("finish handling zip files");
            }

            List<Tuple<string, object>> retVal = new List<Tuple<string, object>>();

            foreach (string xmlFileName in Directory.GetFiles(saveFolder, "*.xml"))
            {
                using (FileStream stream = new FileStream(xmlFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    object deserialized = XamlServices.Load(stream);
                    retVal.Add(Tuple.Create(Path.GetFileNameWithoutExtension(xmlFileName), deserialized));
                }
            }

            return retVal.ToArray();
        }

        /// <summary>
        /// This only touches autosave folders.  If there are more folders than wanted, this will delete the excess folders, and
        /// the folders remaining will be as evenly spaced by time as possible
        /// </summary>
        public static void PruneAutosaves(string sessionFolder, int numToKeep = MAXAUTOSAVES)
        {
            // Get autosave folders
            var autosaveFolders = PruneAutosaves_CreateTimes(GetAutosaveFolders(sessionFolder));
            if (autosaveFolders == null || autosaveFolders.Length <= numToKeep)
            {
                return;
            }

            // Keep the first and last two.  Try to keep them evenly spaced in between.  Get the folders that most line up with the sub elapsed times
            int[] bestFits = PruneAutosaves_BestFits(autosaveFolders, numToKeep);

            // Return the extra indices as sets
            List<int>[] deletableSets = PruneAutosaves_Deletable(bestFits);

            // Pick the most expendable indices
            int[] deletes = PruneAutosaves_GetDeletes(deletableSets, autosaveFolders.Length - numToKeep);

            // Delete these folders (wrapping in a try block just in case a file is referenced or something)
            foreach (int index in deletes)
            {
                try
                {
                    string name = autosaveFolders[index].Item1;
                    if (Directory.Exists(name))     // could be a folder
                    {
                        Directory.Delete(name, true);
                    }
                    else if (File.Exists(name))     // or could be a zip file
                    {
                        File.Delete(name);
                    }
                }
                catch (Exception) { }
            }
        }

        #region Private Methods - save

        private static void Save_Folder(string saveFolder, IEnumerable<Tuple<string, object>> saveFiles)
        {
            Directory.CreateDirectory(saveFolder);

            foreach (var saveFile in saveFiles)
            {
                string filename = saveFile.Item1;
                if (!filename.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    filename += ".xml";
                }

                filename = Path.Combine(saveFolder, filename);

                using (FileStream stream = new FileStream(filename, FileMode.CreateNew))
                {
                    XamlServices.Save(stream, saveFile.Item2);
                }
            }
        }

        private static string GetNewSessionFolderName(string baseFolder, string name)
        {
            //Session 2016-10-09 09.11.35.247
            string retVal = SESSION;
            retVal += DateTime.Now.ToString(TIMESTAMPFORMAT);

            string folder = Path.Combine(baseFolder, name);

            return Path.Combine(folder, retVal);
        }
        private static string GetNewSaveFolderName(string sessionFolder)
        {
            string retVal = SAVE;
            retVal += DateTime.Now.ToString(TIMESTAMPFORMAT);

            retVal = Path.Combine(sessionFolder, retVal);

            return retVal;
        }

        #endregion
        #region Private Methods - load

        //NOTE: This only looks for the session file.  If there is no session file, call AttemptNameFolder with baseFolder\name to see if that works
        private static ISessionOptions AttemptBaseFolder(string baseFolder, string name)
        {
            string sessionFilename = GetRootOptionsFilename(baseFolder, name);

            if (!File.Exists(sessionFilename))
            {
                return null;
            }

            ISessionOptions retVal = null;
            using (FileStream stream = new FileStream(sessionFilename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                retVal = (ISessionOptions)XamlServices.Load(stream);
            }

            return retVal;
        }

        private static bool AttemptNameFolder(out string sessionFolder, out string saveFolder, string nameFolder)
        {
            sessionFolder = null;
            saveFolder = null;

            // Make sure the name folder exists
            if (string.IsNullOrEmpty(nameFolder) || !Directory.Exists(nameFolder))
            {
                return false;
            }

            // Find the latest session
            string pattern = "^" + SESSION + TIMESTAMPREGEX;

            var latestSession = Directory.GetDirectories(nameFolder).
                Select(o => new
                {
                    Name = o,
                    Match = Regex.Match(Path.GetFileName(o), pattern),
                }).
                Where(o => o.Match.Success).
                OrderByDescending(o => o.Match.Value).      // the timestamp is year to millisecond, so a string sort is accurate
                FirstOrDefault();

            if (latestSession == null)
            {
                return false;
            }

            // Session folder found, find the latest save
            return AttemptSessionSaveFolder(out sessionFolder, out saveFolder, latestSession.Name);
        }

        private static bool AttemptSessionSaveFolder(out string sessionFolder, out string saveFolder, string sessionSaveFolder)
        {
            sessionFolder = null;
            saveFolder = null;

            if (string.IsNullOrEmpty(sessionSaveFolder) || !Directory.Exists(sessionSaveFolder))
            {
                return false;
            }

            // Assume that sessionSaveFolder is the session folder, and find the latest save
            string test = FindLatestSaveFolder(sessionSaveFolder);
            if (test != null)
            {
                sessionFolder = sessionSaveFolder;
                saveFolder = test;
                return true;
            }

            // Assume that sessionSaveFolder is the save folder.  See if the parent contains save folders
            string parentFolder = Path.GetDirectoryName(sessionSaveFolder);
            string latestSave = FindLatestSaveFolder(parentFolder);
            if (latestSave != null)
            {
                sessionFolder = parentFolder;
            }

            // Regardless of the outcome, assume the folder passed in is the save folder (it may be a save folder sitting outside
            // the standard save location - copied to the desktop or something)
            saveFolder = sessionSaveFolder;

            return true;
        }

        private static string FindLatestSaveFolder(string sessionFolder)
        {
            if (string.IsNullOrEmpty(sessionFolder) || !Directory.Exists(sessionFolder))
            {
                return null;
            }

            string pattern = "^" + SAVE + TIMESTAMPREGEX;

            var latestSave = Directory.GetDirectories(sessionFolder).
                Concat(Directory.GetFiles(sessionFolder, "*.zip")).
                Select(o => new
                {
                    Name = o,
                    Match = Regex.Match(Path.GetFileName(o), pattern),
                }).
                Where(o => o.Match.Success).
                OrderByDescending(o => o.Match.Value).      // the timestamp is year to millisecond, so a string sort is accurate
                FirstOrDefault();

            if (latestSave == null)
            {
                return null;
            }
            else
            {
                return latestSave.Name;
            }
        }

        #endregion
        #region Private Methods - prune

        private static Tuple<string, DateTime>[] PruneAutosaves_CreateTimes(string[] autosaveFolders)
        {
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
        private static int[] PruneAutosaves_BestFits(Tuple<string, DateTime>[] savesAndTimes, int sliceCount)
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
                var existingClosest = closest.
                    Where(o => o.Item2 == currentIndex).
                    FirstOrDefault();

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

            // Make sure the second to last index is in the return list (that way the last two saves are always kept)
            // The first and last elements of the input array will be in the closest list, because they line up exactly with the first and last slice
            int secondToLast = savesAndTimes.Length - 2;
            if (!closest.Any(o => o.Item2 == secondToLast))
            {
                closest.Add(Tuple.Create(-1, secondToLast, -1d));
            }

            // Exit Function
            return closest.
                Select(o => o.Item2).       // just return the index into savesAndTimes
                OrderBy(o => o).
                ToArray();
        }
        private static List<int>[] PruneAutosaves_Deletable(int[] bestFits)
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



        private static int[] PruneAutosaves_GetDeletes_ORIG(List<int>[] deletableSets, int numToDelete)
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
                List<int>[] largest = deletableSets.
                    Where(o => o.Count == maxSize).
                    ToArray();

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
        private static int[] PruneAutosaves_GetDeletes(List<int>[] deletableSets, int numToDelete)
        {
            if (deletableSets.Sum(o => o.Count) <= numToDelete)
            {
                // Everything in deletable sets needs to be deleted, so just return them all instead of taking the expense of load balancing
                return deletableSets.SelectMany(o => o).ToArray();
            }

            List<int> retVal = new List<int>();

            int maxSize;
            List<int>[] largest;
            int setIndex, innerIndex;

            for (int cntr = 0; cntr < numToDelete; cntr++)
            {
                try
                {
                    // Get the largest sets
                    maxSize = deletableSets.Max(o => o.Count);
                    largest = deletableSets.
                        Where(o => o.Count == maxSize).
                        ToArray();
                }
                catch (Exception ex)
                {
                    throw;
                }

                try
                {
                    // Pick a random set, and a random index within that set
                    setIndex = StaticRandom.Next(largest.Length);
                    innerIndex = StaticRandom.Next(largest[setIndex].Count);
                }
                catch (Exception ex)
                {
                    throw;
                }

                try
                {
                    // Store the value pointed to
                    retVal.Add(largest[setIndex][innerIndex]);
                }
                catch (Exception ex)
                {
                    throw;
                }

                try
                {
                    // Now remove this from the set
                    largest[setIndex].RemoveAt(innerIndex);
                }
                catch (Exception ex)
                {
                    throw;
                }
            }

            return retVal.ToArray();
        }




        private static string[] GetAutosaveFolders(string sessionFolder)
        {
            if (sessionFolder == null || !Directory.Exists(sessionFolder))
            {
                return null;
            }

            string pattern = "^" + SAVE + TIMESTAMPREGEX;

            return Directory.GetDirectories(sessionFolder).
                Concat(Directory.GetFiles(sessionFolder, "*.zip")).
                Select(o => new
                {
                    Name = o,
                    Match = Regex.Match(Path.GetFileName(o), pattern),
                }).
                Where(o => o.Match.Success).
                OrderBy(o => o.Match.Value).      // the timestamp is year to millisecond, so a string sort is accurate
                Select(o => o.Name).
                ToArray();
        }

        #endregion
        #region Private Methods

        private static string GetRootOptionsFilename(string baseFolder, string name)
        {
            string retVal = name.Replace(" ", "");
            retVal += " Options.xml";
            retVal = Path.Combine(baseFolder, retVal);

            return retVal;
        }

        #endregion
    }

    #region Class: SessionFolderResults

    public class SessionFolderResults
    {
        public SessionFolderResults(ISessionOptions sessionFile, string passedInFolder, string sessionFolder, string saveFolder_Zip, Tuple<string, object>[] savedFiles)
        {
            this.SessionFile = sessionFile;
            this.PassedInFolder = passedInFolder;
            this.SessionFolder = sessionFolder;
            this.SaveFolder_Zip = saveFolder_Zip;
            this.SavedFiles = savedFiles;
        }

        /// <summary>
        /// If the folder handed to load contains a session file, this will be it.  Otherwise, this will be null
        /// </summary>
        public readonly ISessionOptions SessionFile;

        /// <summary>
        /// This is the folder that was passed to the method
        /// </summary>
        /// <remarks>
        /// This could be the root folder pointed to by UtilityCore.GetOptionsFolder()
        /// Or could be that folder + name (contains all session folders)
        /// Or could be a session folder (contains save folders)
        /// Or could be a specific save folder/zip (contains save files)
        /// </remarks>
        public readonly string PassedInFolder;
        /// <summary>
        /// This is the folder that contains the save folder/zip
        /// </summary>
        /// <remarks>
        /// This is the location that should should go into the session file (if you want to save the location for the future)
        /// </remarks>
        public readonly string SessionFolder;
        /// <summary>
        /// This is the location of the actual save files
        /// </summary>
        /// <remarks>
        /// This is just returned as a convenience.  It could be stored in the session file, but will require more effort every time
        /// you call load
        /// </remarks>
        public readonly string SaveFolder_Zip;

        /// <summary>
        /// These are the actual xml files that were saved in the save folder/zip
        /// </summary>
        public readonly Tuple<string, object>[] SavedFiles;
    }

    #endregion

    #region Interface: ISessionOptions

    /// <summary>
    /// The session options class will be saved in a root folder
    /// "C:\Users\charlie.rix\AppData\Roaming\Asteroid Miner"
    /// </summary>
    public interface ISessionOptions
    {
        /// <summary>
        /// This points to the last played session
        /// "C:\Users\charlie.rix\AppData\Roaming\Asteroid Miner\Flying Beans\Session 2016-10-09 09.11.35.247"
        /// </summary>
        string LatestSessionFolder { get; set; }
    }

    #endregion
}
