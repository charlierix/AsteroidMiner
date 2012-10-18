using System;
using System.Collections.Generic;
using System.Linq;
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
			int inputMax = inputSize - 1;		//	save me from subtracting one all the time

			for (int numUsed = inputSize; numUsed >= 1; numUsed--)
			{
				int usedMax = numUsed - 1;		//	save me from subtracting one all the time

				//	Seed the return with everything at the left
				int[] retVal = Enumerable.Range(0, numUsed).ToArray();
				yield return (int[])retVal.Clone();		//	if this isn't cloned here, then the consumer needs to do it

				while (true)
				{
					//	Try to bump the last item
					if (retVal[usedMax] < inputMax)
					{
						retVal[usedMax]++;
						yield return (int[])retVal.Clone();
						continue;
					}

					//	The last item is as far as it will go, find an item to the left of it to bump
					bool foundOne = false;

					for (int cntr = usedMax - 1; cntr >= 0; cntr--)
					{
						if (retVal[cntr] < retVal[cntr + 1] - 1)
						{
							//	This one has room to bump
							retVal[cntr]++;

							//	Reset everything to the right of this spot
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
						//	This input size is exhausted (everything is as far right as they can go)
						break;
					}
				}
			}
		}
	}
}
