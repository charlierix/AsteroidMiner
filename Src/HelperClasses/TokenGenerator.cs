﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Game.HelperClasses
{
	/// <summary>
	/// This hands out a unique token each time its called:  TokenGenerator.Instance.NextToken()
	/// NOTE: This is IS threadsafe
	/// </summary>
	/// <remarks>
	/// Random trivia:
	///	int64 can go up to 9 quintillion
	///	If you burn through 1 billion tokens a second, it will take 286 years to reach long.Max (double if starting at long.Min instead of zero)
	/// </remarks>
	public class TokenGenerator
	{
		#region Declaration Section

		private static readonly object _lockStatic = new object();
		private readonly object _lockInstance;

		/// <summary>
		/// The static constructor makes sure that this instance is created only once.  The outside users of this class
		/// call the static property Instance to get this one instance copy.  (then they can use the rest of the instance
		/// methods)
		/// </summary>
		private static TokenGenerator _instance;

		private long _nextToken;

		#endregion

		#region Constructor / Instance Property

		/// <summary>
		/// Static constructor.  Called only once before the first time you use my static properties/methods.
		/// </summary>
		static TokenGenerator()
		{
			lock (_lockStatic)
			{
				//	If the instance version of this class hasn't been instantiated yet, then do so
				if (_instance == null)
				{
					_instance = new TokenGenerator();
				}
			}
		}
		/// <summary>
		/// Instance constructor.  This is called only once by one of the calls from my static constructor.
		/// </summary>
		private TokenGenerator()
		{
			_lockInstance = new object();

			_nextToken = 0;
		}

		/// <summary>
		/// This is how you get at my instance.  The act of calling this property guarantees that the static constructor gets called
		/// exactly once (per process?)
		/// </summary>
		public static TokenGenerator Instance
		{
			get
			{
				//	There is no need to check the static lock, because _instance is only set one time, and that is guaranteed to be
				//	finished before this function gets called
				return _instance;
			}
		}

		#endregion

		#region Public Methods

		public long NextToken()
		{
			lock (_lockInstance)
			{
				long retVal = _nextToken;
				_nextToken++;
				return retVal;
			}
		}

		#endregion
	}
}