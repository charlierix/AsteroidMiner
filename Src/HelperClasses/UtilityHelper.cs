using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xaml;

namespace Game.HelperClasses
{
    public static class UtilityHelper
    {
        private const double FOURTHIRDS = 4d / 3d;

        /// <summary>
        /// This is good for converting a trackbar into a double
        /// </summary>
        /// <param name="minReturn">This is the value that will be returned when valueRange == minRange</param>
        /// <param name="maxReturn">This is the value that will be returned with valueRange == maxRange</param>
        /// <param name="minRange">The lowest value that valueRange can be</param>
        /// <param name="maxRange">The highest value that valueRange can be</param>
        /// <param name="valueRange">The trackbar's value</param>
        /// <returns>Somewhere between minReturn and maxReturn</returns>
        public static double GetScaledValue(double minReturn, double maxReturn, double minRange, double maxRange, double valueRange)
        {
            // Get the percent of value within the range
            double percent = (valueRange - minRange) / (maxRange - minRange);

            // Get the lerp between the return range
            return minReturn + (percent * (maxReturn - minReturn));
        }
        /// <summary>
        /// This overload ensures that the return doen't go beyond the min and max return
        /// </summary>
        public static double GetScaledValue_Capped(double minReturn, double maxReturn, double minRange, double maxRange, double valueRange)
        {
            // Get the percent of value within the range
            double percent = (valueRange - minRange) / (maxRange - minRange);

            // Get the lerp between the return range
            double retVal = minReturn + (percent * (maxReturn - minReturn));

            // Cap the return value
            if (retVal < minReturn)
            {
                retVal = minReturn;
            }
            else if (retVal > maxReturn)
            {
                retVal = maxReturn;
            }

            // Exit Function
            return retVal;
        }

        public static double GetMassForRadius(double radius, double density)
        {
            // Volume = 4/3 * pi * r^3
            // Mass = Volume * Density
            return FOURTHIRDS * Math.PI * (radius * radius * radius) * density;
        }
        public static double GetRadiusForMass(double mass, double density)
        {
            // Volume = Mass / Density
            // r^3 = Volume / (4/3 * pi)
            return Math.Pow((mass / density) / (FOURTHIRDS * Math.PI), .333333d);
        }

        /// <summary>
        /// This iterates over all combinations of a set of numbers
        /// NOTE: The number of iterations is (2^inputSize) - 1, so be careful with input sizes over 10 to 15
        /// </summary>
        /// <remarks>
        /// For example, if you pass in 4, you will get:
        ///		0,1,2,3
        ///		0,1,2
        ///		0,1,3
        ///		0,2,3
        ///		1,2,3
        ///		0,1
        ///		0,2
        ///		0,3
        ///		1,2
        ///		1,3
        ///		2,3
        ///		0
        ///		1
        ///		2
        ///		3
        /// </remarks>
        public static IEnumerable<int[]> AllCombosEnumerator(int inputSize)
        {
            int inputMax = inputSize - 1;		// save me from subtracting one all the time

            for (int numUsed = inputSize; numUsed >= 1; numUsed--)
            {
                int usedMax = numUsed - 1;		// save me from subtracting one all the time

                // Seed the return with everything at the left
                int[] retVal = Enumerable.Range(0, numUsed).ToArray();
                yield return (int[])retVal.Clone();		// if this isn't cloned here, then the consumer needs to do it

                while (true)
                {
                    // Try to bump the last item
                    if (retVal[usedMax] < inputMax)
                    {
                        retVal[usedMax]++;
                        yield return (int[])retVal.Clone();
                        continue;
                    }

                    // The last item is as far as it will go, find an item to the left of it to bump
                    bool foundOne = false;

                    for (int cntr = usedMax - 1; cntr >= 0; cntr--)
                    {
                        if (retVal[cntr] < retVal[cntr + 1] - 1)
                        {
                            // This one has room to bump
                            retVal[cntr]++;

                            // Reset everything to the right of this spot
                            for (int resetCntr = cntr + 1; resetCntr < numUsed; resetCntr++)
                            {
                                retVal[resetCntr] = retVal[cntr] + (resetCntr - cntr);
                            }

                            foundOne = true;
                            yield return (int[])retVal.Clone();
                            break;
                        }
                    }

                    if (!foundOne)
                    {
                        // This input size is exhausted (everything is as far right as they can go)
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// This acts like Enumerable.Range, but the values returned are in a random order
        /// </summary>
        public static IEnumerable<int> RandomRange(int start, int count)
        {
            // Prepare a list of indices (these represent what's left to return)
            //int[] indices = Enumerable.Range(start, count).ToArray();		// this is a smaller amount of code, but slower
            int[] indices = new int[count];
            for (int cntr = 0; cntr < count; cntr++)
            {
                indices[cntr] = start + cntr;
            }

            Random rand = StaticRandom.GetRandomForThread();

            for (int cntr = count - 1; cntr >= 0; cntr--)
            {
                // Come up with a random value that hasn't been returned yet
                int index1 = rand.Next(cntr + 1);
                int index2 = indices[index1];
                indices[index1] = indices[cntr];

                yield return index2;
            }
        }
        /// <summary>
        /// This overload wont iterate over all the values, just some of them
        /// </summary>
        /// <remarks>
        /// Example:
        ///		start=0, rangeCount=10, iterateCount=3
        ///		This will return 3 values, but their range is from 0 to 10 (and it will never return dupes)
        /// </remarks>
        public static IEnumerable<int> RandomRange(int start, int rangeCount, int iterateCount)
        {
            if (iterateCount > rangeCount)
            {
                throw new ArgumentOutOfRangeException(string.Format("iterateCount can't be greater than rangeCount.  iterateCount={0}, rangeCount={1}", iterateCount.ToString(), rangeCount.ToString()));
            }

            if (iterateCount < rangeCount / 3)
            {
                #region While Loop

                Random rand = StaticRandom.GetRandomForThread();

                // Rather than going through the overhead of building an array of all values up front, just remember what I've returned
                List<int> used = new List<int>();
                int maxValue = start + rangeCount;

                for (int cntr = 0; cntr < iterateCount; cntr++)
                {
                    // Find a value that hasn't been returned yet
                    int retVal = 0;
                    while (true)
                    {
                        retVal = rand.Next(start, maxValue);

                        if (!used.Contains(retVal))
                        {
                            used.Add(retVal);
                            break;
                        }
                    }

                    // Return this
                    yield return retVal;
                }

                #endregion
            }
            else if (iterateCount > 0)
            {
                #region Maintain Array

                // Reuse my other overload, just stop prematurely

                int cntr = 0;
                foreach (int retVal in RandomRange(start, rangeCount))
                {
                    yield return retVal;

                    cntr++;
                    if (cntr == iterateCount)
                    {
                        break;
                    }
                }

                #endregion
            }
        }

        //TODO: See if there is a cleaner way to write this.  I tried to take params, but that seems to make the caller cast to array
        // I had a case where I had several arrays that may or may not be null, and I wanted to iterate over all of the non null ones
        // Usage: foreach(T item in Iterate(array1, array2, array3))
        public static IEnumerable<T> Iterate<T>(IEnumerable<T> list1)
        {
            return Iterate(list1, null, null, null, null, null, null, null);
        }
        public static IEnumerable<T> Iterate<T>(IEnumerable<T> list1, IEnumerable<T> list2)
        {
            return Iterate(list1, list2, null, null, null, null, null, null);
        }
        public static IEnumerable<T> Iterate<T>(IEnumerable<T> list1, IEnumerable<T> list2, IEnumerable<T> list3)
        {
            return Iterate(list1, list2, list3, null, null, null, null, null);
        }
        public static IEnumerable<T> Iterate<T>(IEnumerable<T> list1, IEnumerable<T> list2, IEnumerable<T> list3, IEnumerable<T> list4)
        {
            return Iterate(list1, list2, list3, list4, null, null, null, null);
        }
        public static IEnumerable<T> Iterate<T>(IEnumerable<T> list1, IEnumerable<T> list2, IEnumerable<T> list3, IEnumerable<T> list4, IEnumerable<T> list5)
        {
            return Iterate(list1, list2, list3, list4, list5, null, null, null);
        }
        public static IEnumerable<T> Iterate<T>(IEnumerable<T> list1, IEnumerable<T> list2, IEnumerable<T> list3, IEnumerable<T> list4, IEnumerable<T> list5, IEnumerable<T> list6)
        {
            return Iterate(list1, list2, list3, list4, list5, list6, null, null);
        }
        public static IEnumerable<T> Iterate<T>(IEnumerable<T> list1, IEnumerable<T> list2, IEnumerable<T> list3, IEnumerable<T> list4, IEnumerable<T> list5, IEnumerable<T> list6, IEnumerable<T> list7)
        {
            return Iterate(list1, list2, list3, list4, list5, list6, list7, null);
        }
        public static IEnumerable<T> Iterate<T>(IEnumerable<T> list1, IEnumerable<T> list2, IEnumerable<T> list3, IEnumerable<T> list4, IEnumerable<T> list5, IEnumerable<T> list6, IEnumerable<T> list7, IEnumerable<T> list8)
        {
            if (list1 != null)
            {
                foreach (T item in list1)
                {
                    yield return item;
                }
            }

            if (list2 != null)
            {
                foreach (T item in list2)
                {
                    yield return item;
                }
            }

            if (list3 != null)
            {
                foreach (T item in list3)
                {
                    yield return item;
                }
            }

            if (list4 != null)
            {
                foreach (T item in list4)
                {
                    yield return item;
                }
            }

            if (list5 != null)
            {
                foreach (T item in list5)
                {
                    yield return item;
                }
            }

            if (list6 != null)
            {
                foreach (T item in list6)
                {
                    yield return item;
                }
            }

            if (list7 != null)
            {
                foreach (T item in list7)
                {
                    yield return item;
                }
            }

            if (list8 != null)
            {
                foreach (T item in list8)
                {
                    yield return item;
                }
            }
        }

        public static T GetRandomEnum<T>(T excluding)
        {
            return GetRandomEnum<T>(new T[] { excluding });
        }
        public static T GetRandomEnum<T>(IEnumerable<T> excluding)
        {
            while (true)
            {
                T retVal = GetRandomEnum<T>();
                if (!excluding.Contains(retVal))
                {
                    return retVal;
                }
            }
        }
        public static T GetRandomEnum<T>()
        {
            Array allValues = Enum.GetValues(typeof(T));
            if (allValues.Length == 0)
            {
                throw new ArgumentException("This enum has no values");
            }

            return (T)allValues.GetValue(StaticRandom.Next(allValues.Length));
        }

        /// <summary>
        /// This creates a new array with the item added to the end
        /// </summary>
        public static T[] ArrayAdd<T>(T[] array, T item)
        {
            if (array == null)
            {
                return new T[] { item };
            }

            T[] retVal = new T[array.Length + 1];

            Array.Copy(array, retVal, array.Length);
            retVal[retVal.Length - 1] = item;

            return retVal;
        }

        /// <summary>
        /// WARNING: This only works if T is serializable
        /// WARNING: Dictionary fails on load, use SortedList instead
        /// </summary>
        public static T Clone<T>(T item)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                XamlServices.Save(stream, item);
                stream.Position = 0;
                return (T)XamlServices.Load(stream);
            }
        }

        /// <summary>
        /// This deserializes the options class from a previous call in appdata
        /// </summary>
        public static T ReadOptions<T>(string filenameNoFolder) where T : class
        {
            string filename = GetOptionsFilename(filenameNoFolder);
            if (!File.Exists(filename))
            {
                return null;
            }

            T retVal = null;
            using (FileStream file = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                //deserialized = XamlReader.Load(file);		// this is the old way, it doesn't like generic lists
                retVal = XamlServices.Load(file) as T;
            }

            return retVal;
        }
        public static void SaveOptions<T>(T options, string filenameNoFolder) where T : class
        {
            string filename = GetOptionsFilename(filenameNoFolder);

            //string xamlText = XamlWriter.Save(options);		// this is the old one, it doesn't like generic lists
            string xamlText = XamlServices.Save(options);

            using (StreamWriter writer = new StreamWriter(filename, false))
            {
                writer.Write(xamlText);
            }
        }
        private static string GetOptionsFilename(string filenameNoFolder)
        {
            string foldername = UtilityHelper.GetOptionsFolder();
            return Path.Combine(foldername, filenameNoFolder);
        }
        /// <summary>
        /// This is where all user options xml files should be stored
        /// </summary>
        public static string GetOptionsFolder()
        {
            string foldername = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            foldername = Path.Combine(foldername, "Asteroid Miner");

            // Make sure the folder exists
            if (!Directory.Exists(foldername))
            {
                Directory.CreateDirectory(foldername);
            }

            return foldername;
        }
    }
}
