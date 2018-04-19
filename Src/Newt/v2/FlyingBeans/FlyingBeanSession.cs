using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xaml;

using Game.Newt.v2.GameItems;
using System.Text.RegularExpressions;
using Game.HelperClassesCore;

namespace Game.Newt.v2.FlyingBeans
{
    /// <summary>
    /// This stores the current state of the session
    /// </summary>
    /// <remarks>
    /// I didn't want to have the entire session stored in this class.  I wanted sets of files stored in a folder to make it easier to pick
    /// through previous sessions.  Otherwise, it would be stored in a monster xml file
    /// </remarks>
    public class FlyingBeanSession
    {
        #region Declaration Section

        public const string FILENAME_SESSION = "FlyingBeans Options.xml";		//NOTE: This goes in the base folder (C:\Users\<user>\AppData\Roaming\Asteroid Miner\)
        public const string FILENAME_OPTIONS = "FlyingBeanOptions.xml";
        public const string FILENAME_ITEMOPTIONS = "ItemOptions.xml";

        #endregion

        #region Public Properties

        /// <summary>
        /// This is stored so that when they start the simulation later, it will pick up from where they last saved (which may be in a
        /// session folder other than the latest one)
        /// </summary>
        public string SessionFolder
        {
            get;
            set;
        }

        #endregion

        //TODO: Rework this class to use SessionSaveLoad.cs
        public static void Save(string baseFolder, string saveFolder, FlyingBeanSession session, FlyingBeanOptions options, ItemOptions itemOptions)
        {
            // Session
            //NOTE: This is in the base folder
            UtilityCore.SerializeToFile(Path.Combine(baseFolder, FILENAME_SESSION), session);

            // ItemOptions
            UtilityCore.SerializeToFile(Path.Combine(saveFolder, FILENAME_ITEMOPTIONS), itemOptions);

            // Options
            FlyingBeanOptions optionCloned = UtilityCore.Clone(options);
            optionCloned.DefaultBeanList = null;		// this is programatically generated, no need to save it
            //optionCloned.NewBeanList		//NOTE: These will be serialized with the options file

            SortedList<string, ShipDNA> winningFilenames;
            SortedList<string, double> maxScores;
            ExtractHistory(out winningFilenames, out maxScores, options.WinnersFinal);		// can't use cloned.winners, that property is skipped when serializing
            optionCloned.WinningScores = maxScores;

            // Main class
            UtilityCore.SerializeToFile(Path.Combine(saveFolder, FILENAME_OPTIONS), optionCloned);

            // Winning beans
            if (winningFilenames != null)
            {
                foreach (string beanFile in winningFilenames.Keys)
                {
                    UtilityCore.SerializeToFile(Path.Combine(saveFolder, beanFile), winningFilenames[beanFile]);
                }
            }
        }
        public static void Load(out FlyingBeanSession session, out FlyingBeanOptions options, out ItemOptions itemOptions, out ShipDNA[] winningBeans, string baseFolder, string saveFolder)
        {
            #region Session

            string filename = Path.Combine(baseFolder, FILENAME_SESSION);
            if (File.Exists(filename))
            {
                session = UtilityCore.DeserializeFromFile<FlyingBeanSession>(filename);
            }
            else
            {
                session = new FlyingBeanSession();
            }

            #endregion

            #region FlyingBeanOptions

            filename = Path.Combine(saveFolder, FILENAME_OPTIONS);
            if (File.Exists(filename))
            {
                options = UtilityCore.DeserializeFromFile<FlyingBeanOptions>(filename);
            }
            else
            {
                //options = new FlyingBeanOptions();
                throw new ArgumentException("Didn't find " + FILENAME_OPTIONS + "\r\n\r\nThe folder might not be a valid save folder");
            }

            #endregion

            // This currently isn't stored to file
            session = new FlyingBeanSession();

            #region ItemOptions

            filename = Path.Combine(saveFolder, FILENAME_ITEMOPTIONS);
            if (File.Exists(filename))
            {
                itemOptions = UtilityCore.DeserializeFromFile<ItemOptions>(filename);
            }
            else
            {
                itemOptions = new ItemOptions();
            }

            #endregion

            #region Winning Beans

            List<Tuple<string, ShipDNA>> winners = new List<Tuple<string, ShipDNA>>();

            foreach (string filename2 in Directory.GetFiles(saveFolder))
            {
                Match match = Regex.Match(Path.GetFileName(filename2), @"^(.+?( - )){2}(?<rank>\d+)\.xml$", RegexOptions.IgnoreCase);
                if (!match.Success)
                {
                    continue;
                }

                // All other xml files should be winning beans
                ShipDNA dna = null;
                try
                {
                    dna = UtilityCore.DeserializeFromFile<ShipDNA>(filename2);
                }
                catch (Exception)
                {
                    // Must not be a ship
                    continue;
                }

                winners.Add(Tuple.Create(Path.GetFileName(filename2), dna));
            }

            winningBeans = winners.Select(o => o.Item2).ToArray();

            #endregion

            // WinnersFinal
            options.WinnersFinal = MergeHistory(options.WinningScores, winners, options.TrackingMaxLineagesFinal, options.TrackingMaxPerLineageFinal);
        }

        public static string EscapeFilename(string text)
        {
            char[] illegalChars = Path.GetInvalidFileNameChars();
            return new string(text.Select(o => illegalChars.Contains(o) ? '_' : o).ToArray());
        }

        #region Private Methods

        private static void ExtractHistory(out SortedList<string, ShipDNA> filenames, out SortedList<string, double> filenameScores, WinnerList winners)
        {
            #region Check for no winners

            if (winners == null)
            {
                filenames = null;
                filenameScores = null;
                return;
            }

            var dump = winners.Current;

            if (dump == null || dump.Length == 0)
            {
                filenames = null;
                filenameScores = null;
                return;
            }

            #endregion

            filenames = new SortedList<string, ShipDNA>();
            filenameScores = new SortedList<string, double>();
            List<string> dupeCheck = new List<string>();

            foreach (var set in dump)
            {
                // Escape the filename
                string shipname = EscapeFilename(set.ShipName);
                if (dupeCheck.Contains(shipname.ToUpper()))
                {
                    shipname += Guid.NewGuid().ToString();
                }
                dupeCheck.Add(shipname.ToUpper());

                foreach (var lineage in set.BeansByLineage)
                {
                    for (int cntr = 0; cntr < lineage.Item2.Length; cntr++)
                    {
                        // Name - Lineage - Rank.xml
                        string filename = string.Format("{0} - {1} - {2}.xml", shipname, EscapeFilename(lineage.Item1), cntr.ToString());		// there won't be a dupe on lineage, it's just a guid

                        // One or the other will be nonnull
                        if (lineage.Item2[cntr].Ship != null)
                        {
                            filenames.Add(filename, lineage.Item2[cntr].Ship.GetNewDNA());
                        }
                        else
                        {
                            filenames.Add(filename, lineage.Item2[cntr].DNA);
                        }

                        // Store the score (it's a separate list so that it can be directly serialized)
                        filenameScores.Add(filename, lineage.Item2[cntr].Score);
                    }
                }
            }
        }

        private static WinnerList MergeHistory(SortedList<string, double> scores, List<Tuple<string, ShipDNA>> winners, int maxLineages, int maxPerLineage)
        {
            if (scores == null || scores.Count == 0)
            {
                return null;
            }

            // Join the dna with scores
            List<Tuple<ShipDNA, double>> shipsScores = new List<Tuple<ShipDNA, double>>();
            foreach (var winner in winners)
            {
                if (scores.ContainsKey(winner.Item1))		// it should always be found, unless the user messed with the files
                {
                    shipsScores.Add(Tuple.Create(winner.Item2, scores[winner.Item1]));
                }
            }

            List<WinnerList.WinningSet> names = new List<WinnerList.WinningSet>();

            // Group by ship name
            var groupNames = shipsScores.GroupBy(o => o.Item1.ShipName).ToArray();
            foreach (var groupName in groupNames)
            {
                List<Tuple<string, WinnerList.WinningBean[]>> lineages = new List<Tuple<string, WinnerList.WinningBean[]>>();

                // Group by lineage
                var groupLineages = groupName.GroupBy(o => o.Item1.ShipLineage).ToArray();
                foreach (var groupLineage in groupLineages)
                {
                    // Get the individuals for this name/lineage
                    var individuals = groupLineage.Select(o => new WinnerList.WinningBean(o.Item1, o.Item2, 0d)).ToArray();

                    lineages.Add(Tuple.Create(groupLineage.Key, individuals));
                }

                if (lineages.Count > 0)
                {
                    names.Add(new WinnerList.WinningSet(groupName.Key, lineages.ToArray()));
                }
            }

            if (names.Count == 0)
            {
                return null;
            }

            return new WinnerList(false, maxLineages, maxPerLineage, names.ToArray());
        }

        #endregion
    }
}
