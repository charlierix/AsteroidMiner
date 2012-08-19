using System;
using System.Collections.Generic;
using System.Text;

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
			//	Get the percent of value within the range
			double percent = (valueRange - minRange) / (maxRange - minRange);

			//	Get the lerp between the return range
			return minReturn + (percent * (maxReturn - minReturn));
		}
		/// <summary>
		/// This overload ensures that the return doen't go beyond the min and max return
		/// </summary>
		public static double GetScaledValue_Capped(double minReturn, double maxReturn, double minRange, double maxRange, double valueRange)
		{
			//	Get the percent of value within the range
			double percent = (valueRange - minRange) / (maxRange - minRange);

			//	Get the lerp between the return range
			double retVal = minReturn + (percent * (maxReturn - minReturn));

			//	Cap the return value
			if (retVal < minReturn)
			{
				retVal = minReturn;
			}
			else if (retVal > maxReturn)
			{
				retVal = maxReturn;
			}

			//	Exit Function
			return retVal;
        }

		public static double GetMassForRadius(double radius, double density)
		{
			//	Volume = 4/3 * pi * r^3
			//	Mass = Volume * Density
			return FOURTHIRDS * Math.PI * (radius * radius * radius) * density;
		}
		public static double GetRadiusForMass(double mass, double density)
		{
			//	Volume = Mass / Density
			//	r^3 = Volume / (4/3 * pi)
			return Math.Pow((mass / density) / (FOURTHIRDS * Math.PI), .333333d);
		}

		/// <summary>
		/// Element 0=0, element 1=1, etc
		/// </summary>
		public static int[] GetIncrementingArray(int size)
		{
			int[] retVal = new int[size];

			for (int cntr = 0; cntr < size; cntr++)
			{
				retVal[cntr] = cntr;
			}

			return retVal;
		}
		/// <summary>
		/// Element 0=0, element 1=1, etc
		/// </summary>
		public static List<int> GetIncrementingList(int size)
		{
			List<int> retVal = new List<int>();

			for (int cntr = 0; cntr < size; cntr++)
			{
				retVal.Add(cntr);
			}

			return retVal;
		}
	}
}
