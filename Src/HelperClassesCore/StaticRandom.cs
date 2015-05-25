using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Security.Cryptography;
using System.Threading;

namespace Game.HelperClassesCore
{
    /// <summary>
    /// This is a wrapper to unique random classes per thread, each one generated with a random seed
    /// </summary>
    /// <remarks>
    /// Got this here:
    /// http://blogs.msdn.com/b/pfxteam/archive/2009/02/19/9434171.aspx
    /// </remarks>
    public static class StaticRandom
    {
        #region Class: RandomWrapper

        private class RandomWrapper
        {
            /// <summary>
            /// This way the random instance gets swapped out using a new crypto seed.  This gives a good blend between
            /// speed and true randomness
            /// </summary>
            private int _elapse = -1;

            private Random _rand = null;
            public Random Random
            {
                get
                {
                    _elapse--;

                    if (_elapse < 0)
                    {
                        // Create a unique seed (can't just instantiate Random without a seed, because if a bunch of threads are spun up quickly, and each requests
                        // its own class, they will all have the same seed - seed is based on tickcount, which is time dependent)
                        byte[] buffer = new byte[4];
                        _globalRandom.GetBytes(buffer);

                        _rand = new Random(BitConverter.ToInt32(buffer, 0));

                        // Figure out how long this should live
                        _elapse = _rand.Next(400, 1500);
                    }

                    return _rand;
                }
            }
        }

        #endregion

        #region Declaration Section

        /// <summary>
        /// This serves as both a lock object and a random number generator whenever a new thread needs its own random class
        /// </summary>
        /// <remarks>
        /// RNGCryptoServiceProvider is slower than Random, but is more random.  So this is used when coming up with a new random
        /// class (because speed isn't as important for the one time call per thread)
        /// </remarks>
        private static RNGCryptoServiceProvider _globalRandom = new RNGCryptoServiceProvider();

        private static ThreadLocal<RandomWrapper> _localRandom = new ThreadLocal<RandomWrapper>(() => new RandomWrapper());

        #endregion

        #region Public Methods

        /// <summary>
        /// This is the random class for the current thread.  It is exposed for optimization reasons.
        /// WARNING: Don't share this instance across threads
        /// </summary>
        /// <remarks>
        /// This is exposed publicly in case many random numbers are needed by the calling function.  The ThreadStatic attribute has a slight expense
        /// to it, so if you have a loop that needs hundreds of random numbers, it's better to call this method, and use the returned class directly, instead
        /// of calling this class's static Next method over and over.
        /// </remarks>
        public static Random GetRandomForThread()
        {
            return _localRandom.Value.Random;
        }

        public static int Next()
        {
            return _localRandom.Value.Random.Next();
        }
        public static int Next(int maxValue)
        {
            return _localRandom.Value.Random.Next(maxValue);
        }
        public static int Next(int minValue, int maxValue)
        {
            return _localRandom.Value.Random.Next(minValue, maxValue);
        }

        public static void NextBytes(byte[] buffer)
        {
            _localRandom.Value.Random.NextBytes(buffer);
        }

        public static double NextDouble()
        {
            return _localRandom.Value.Random.NextDouble();
        }
        public static double NextDouble(double maxValue)
        {
            return _localRandom.Value.Random.NextDouble(maxValue);
        }
        public static double NextDouble(double minValue, double maxValue)
        {
            return _localRandom.Value.Random.NextDouble(minValue, maxValue);
        }

        public static double NextPercent(double midPoint, double percent, bool useRandomPercent = true)
        {
            return _localRandom.Value.Random.NextPercent(midPoint, percent, useRandomPercent);
        }
        public static double NextDrift(double midPoint, double drift, bool useRandomDrift = true)
        {
            return _localRandom.Value.Random.NextDrift(midPoint, drift, useRandomDrift);
        }

        public static double NextPow(double power, double maxValue = 1d, bool isPlusMinus = true)
        {
            return _localRandom.Value.Random.NextPow(power, maxValue, isPlusMinus);
        }

        public static bool NextBool()
        {
            return _localRandom.Value.Random.NextBool();
        }

        public static string NextString(int length)
        {
            return _localRandom.Value.Random.NextString(length);
        }
        public static string NextString(int randLengthFrom, int randLengthTo)
        {
            return _localRandom.Value.Random.NextString(randLengthFrom, randLengthTo);
        }

        public static T NextItem<T>(T[] items)
        {
            return _localRandom.Value.Random.NextItem(items);
        }
        public static T NextItem<T>(IList<T> items)
        {
            return _localRandom.Value.Random.NextItem(items);
        }

        #endregion
    }
}
