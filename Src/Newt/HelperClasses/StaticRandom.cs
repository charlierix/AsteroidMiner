using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Security.Cryptography;

namespace Game.Newt.HelperClasses
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
		#region Declaration Section

		/// <summary>
		/// This serves as both a lock object and a random number generator whenever a new thread needs its own random class
		/// </summary>
		/// <remarks>
		/// RNGCryptoServiceProvider is slower than Random, but is more random.  So this is used when coming up with a new random
		/// class (because speed isn't as important for the one time call per thread)
		/// </remarks>
		private static RNGCryptoServiceProvider _globalRandom = new RNGCryptoServiceProvider();

		[ThreadStatic]
		private static Random _localRandom;

		#endregion

		#region Public Methods

		/// <summary>
		/// This is the random class for the current thread.  It is exposed for optimization reasons.
		/// WARNING: Don't share this instance across thread
		/// </summary>
		/// <remarks>
		/// This is exposed publicly in case many random numbers are needed by the calling function.  The ThreadStatic attribute has a slight expense
		/// to it, so if you have a loop that needs hundreds of random numbers, it's better to call this method, and use the returned class directly, instead
		/// of calling this class's static Next method over and over.
		/// </remarks>
		public static Random GetRandomForThread()
		{
			//	The threadstatic attribute guarantees that each thread gets its own instance (this is faster than using a lock on a single object)
			Random retVal = _localRandom;

			if (retVal == null)
			{
				//	This is the first time this thread has requested a random class.  Make a new one

				//	Create a unique seed (can't just instantiate Random without a seed, because if a bunch of threads are spun up quickly, and each requests
				//	its own class, they will all have the same seed - seed is based on tickcount, which is time dependent)
				byte[] buffer = new byte[4];
				_globalRandom.GetBytes(buffer);

				_localRandom = retVal = new Random(BitConverter.ToInt32(buffer, 0));
			}

			//	Exit Function
			return retVal;
		}

		public static int Next()
		{
			return GetRandomForThread().Next();
		}
		public static int Next(int maxValue)
		{
			return GetRandomForThread().Next(maxValue);
		}
		public static int Next(int minValue, int maxValue)
		{
			return GetRandomForThread().Next(minValue, maxValue);
		}

		public static void NextBytes(byte[] buffer)
		{
			GetRandomForThread().NextBytes(buffer);
		}

		public static double NextDouble()
		{
			return GetRandomForThread().NextDouble();
		}

		#endregion
	}
}
