using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;

namespace Game.HelperClassesAI
{
    /// <summary>
    /// This is a bunch of helper methods that help converting DB columns into vectors that can be fed to SelfOrganizingMaps
    /// </summary>
    public static class SelfOrganizingMapsDB
    {
        public static SOMFieldStats GetFieldStats(IEnumerable<string> field, SOMFieldType? typeOverride = null)
        {
            // Dedupe
            string[] deduped = field.
                Select(o => o.Trim()).
                Distinct().
                ToArray();

            // Unique Chars
            char[] uniqueChars = deduped.
                SelectMany(o => o).
                Distinct().
                OrderBy(o => o).
                ToArray();

            // FieldType
            SOMFieldType type = typeOverride ?? GetFieldType(field);

            // Numeric Stats
            double? numericMin = null;
            double? numericMax = null;
            double? numericAvg = null;
            double? numericStandDev = null;
            if (type == SOMFieldType.Integer || type == SOMFieldType.FloatingPoint)
            {
                double[] numerics = field.
                    Select(o => string.IsNullOrWhiteSpace(o) ? 0d : double.Parse(o.Trim())).
                    ToArray();

                numericMin = numerics.Min();
                numericMax = numerics.Max();
                var avg_stdev = Math1D.Get_Average_StandardDeviation(numerics);
                numericAvg = avg_stdev.Item1;
                numericStandDev = avg_stdev.Item2;
            }

            // Date Stats
            DateTime? dateMin = null;
            DateTime? dateMax = null;
            DateTime? dateAvg = null;
            TimeSpan? dateStandDev = null;
            if (type == SOMFieldType.DateTime)
            {
                DateTime[] dates = field.
                    Where(o => !string.IsNullOrWhiteSpace(o)).
                    Select(o => DateTime.Parse(o.Trim())).
                    ToArray();

                dateMin = dates.Min();
                dateMax = dates.Max();
                var avg_stdev = Math1D.Get_Average_StandardDeviation(dates);
                dateAvg = avg_stdev.Item1;
                dateStandDev = avg_stdev.Item2;
            }

            // Return
            return new SOMFieldStats()
            {
                Count = field.Count(),
                UniqueCount = deduped.Length,

                MinLength = deduped.Min(o => o.Length),     // deduped are already trimmed
                MaxLength = deduped.Max(o => o.Length),
                UniqueChars = uniqueChars,
                UniqueChars_NonWhitespace = uniqueChars.Where(o => !UtilityCore.IsWhitespace(o)).ToArray(),

                FieldType = type,

                Numeric_Min = numericMin,
                Numeric_Max = numericMax,
                Numeric_Avg = numericAvg,
                Numeric_StandDev = numericStandDev,

                Date_Min = dateMin,
                Date_Max = dateMax,
                Date_Avg = dateAvg,
                Date_StandDev = dateStandDev,
            };
        }
        public static SOMFieldType GetFieldType(IEnumerable<string> field)
        {
            bool possibleInteger = true;
            bool possibleFloat = true;
            bool possibleDatetime = true;
            bool possibleAlphaNum = true;

            bool foundOne = false;

            foreach (string value1 in field)
            {
                string value2 = value1.Trim();
                if (value2 == "")
                {
                    continue;
                }

                foundOne = true;

                // Integer
                if (possibleInteger)
                {
                    long longTest;
                    if (!long.TryParse(value2, out longTest))
                    {
                        possibleInteger = false;
                    }
                }

                // Float
                if (possibleFloat)
                {
                    double doubleTest;
                    if (!double.TryParse(value2, out doubleTest))
                    {
                        possibleFloat = false;
                    }
                }

                // Datetime
                if (possibleDatetime)
                {
                    DateTime datetimeTest;
                    if (!DateTime.TryParse(value2, out datetimeTest))
                    {
                        possibleDatetime = false;
                    }
                }

                // AlphaNum
                if (possibleAlphaNum)
                {
                    if (!Regex.IsMatch(value2, "^[a-z0-9]+$", RegexOptions.IgnoreCase))
                    {
                        possibleAlphaNum = false;
                    }
                }

                if (!possibleAlphaNum && !possibleFloat && !possibleInteger && !possibleDatetime)
                {
                    return SOMFieldType.AnyText;
                }
            }

            if (!foundOne)
            {
                throw new ArgumentException("Field list was empty");
            }

            if (possibleDatetime)
            {
                return SOMFieldType.DateTime;
            }
            else if (possibleInteger)
            {
                return SOMFieldType.Integer;
            }
            else if (possibleFloat)
            {
                return SOMFieldType.FloatingPoint;
            }
            else if (possibleAlphaNum)
            {
                return SOMFieldType.AlphaNumeric;
            }
            else
            {
                return SOMFieldType.AnyText;       // execution should never get here
            }
        }

        public static SOMConvertToVectorProps GetConvertToProps(DateTime minDate, DateTime maxDate, int numDigits)
        {
            double largestValue = (maxDate - minDate).TotalDays;

            return GetConvertToProps(largestValue, numDigits);
        }
        public static SOMConvertToVectorProps GetConvertToProps(double minNumber, double maxNumber, int numDigits)
        {
            double largestValue = Math.Max(Math.Abs(minNumber), Math.Abs(maxNumber));

            return GetConvertToProps(largestValue, numDigits);
        }
        /// <summary>
        /// This figures out what base to use to make a number fit within a certain number of digits
        /// </summary>
        /// <remarks>
        /// For example 5 in base 10 is 5 and takes 1 digit to represent.  In base 2 it's 101, so takes up 3 digits.  If you wanted it to take up
        /// two digits, you would need base 3 (12)
        /// 
        /// NOTE: This method is expected to be used to feed ConvertToVector(), and could see small floating point numbers.  In order to
        /// keep high precision, the number will be scaled up to a billion
        /// </remarks>
        public static SOMConvertToVectorProps GetConvertToProps(double maxNumber, int numDigits)
        {
            int MINVALUE = 1000000000;

            double absNumber = Math.Abs(maxNumber);

            // Scale
            double scale = 1d;
            if (!absNumber.IsNearZero() && absNumber < MINVALUE)
            {
                scale = MINVALUE / absNumber;
            }

            // Base
            double scaledNumber = absNumber * scale;
            int desiredBase = Math.Pow(scaledNumber, 1d / numDigits).ToInt_Ceiling();

            if (desiredBase < 2)
            {
                desiredBase = 2;
            }

            return new SOMConvertToVectorProps(numDigits, desiredBase, scale);
        }

        public static double[] ConvertToVector(string text, SOMFieldStats stats, SOMConvertToVectorProps convertProps)
        {
            switch (stats.FieldType)
            {
                case SOMFieldType.Integer:
                case SOMFieldType.FloatingPoint:
                    #region numeric

                    double castDbl1 = string.IsNullOrWhiteSpace(text) ? 0d : double.Parse(text.Trim());

                    return ConvertToVector_LeftSignificant(castDbl1, convertProps);

                #endregion

                case SOMFieldType.DateTime:
                    #region date

                    DateTime castDt;
                    if (!DateTime.TryParse(text, out castDt))
                    {
                        castDt = stats.Date_Min.Value;
                    }

                    double castDbl2 = (castDt - stats.Date_Min.Value).TotalDays;        // convertProps was built from "(stats.Date_Max - stats.Date_Min).TotalDays"

                    return ConvertToVector_LeftSignificant(castDbl2, convertProps);

                #endregion

                case SOMFieldType.AlphaNumeric:
                case SOMFieldType.AnyText:
                    #region text

                    return ConvertToVector_Text(text, convertProps, stats.UniqueChars_NonWhitespace);

                #endregion

                default:
                    throw new ApplicationException("finish this: " + stats.FieldType.ToString());
            }
        }

        #region Private Methods

        /// <summary>
        /// This converts the value into a normalized vector (values from -1 to 1 in each dimension)
        /// </summary>
        /// <remarks>
        /// This is useful if you want to convert numbers into vectors
        /// 
        /// Say you want to do a SOM against a database.  Each column needs to be mapped to a vector.  Then all vectors of a row will get
        /// stitched together to be one intance of ISOMInput.Weights
        /// 
        /// If one of the columns is numeric (maybe dollars or quantities), then you would use this method
        /// 
        /// The first step would be to prequery so see what the range of possible values are.  Run that maximum expected value through
        /// GetConvertBaseProps() to figure out what base to represent the numbers as.  This method converts the number to that base,
        /// then normalizes each digit to -1 to 1 (sort of like percent of base)
        /// </remarks>
        private static double[] ConvertToVector_Direct(double value, SOMConvertToVectorProps props)
        {
            // Convert to a different base
            long scaledValue = Convert.ToInt64(value * props.Number_ScaleToLong);
            int[] converted = MathND.ConvertToBase(scaledValue, props.Number_BaseConvertTo.Value);

            // Too big, return 1s
            if (converted.Length > props.Width)
            {
                double maxValue = value < 0 ? -1d : 1d;
                return Enumerable.Range(0, props.Width).Select(o => maxValue).ToArray();
            }

            // Normalize (treat each item like a percent)
            double baseDbl = props.Number_BaseConvertTo.Value.ToDouble();

            double[] normalized = converted.
                Select(o => o.ToDouble() / baseDbl).
                ToArray();

            // Return, make sure the array is the right size
            if (normalized.Length < props.Width)
            {
                return Enumerable.Range(0, props.Width - normalized.Length).
                    Select(o => 0d).
                    Concat(normalized).
                    ToArray();
            }
            else
            {
                return normalized;
            }
        }

        /// <summary>
        /// This washes the bits to the right with values approaching one
        /// </summary>
        /// <remarks>
        /// The leftmost bit is most significant, and needs to be returned acurately.  The bits to the right don't matter as much, but
        /// the self organizing map just groups things together based on the pattern of the bits.  So the bits to the right need to approach
        /// one (think of them as overidden by the bits to the left)
        /// 
        /// I didn't want linear, I wanted something faster.  So the bits to the right follow a sqrt curve (x axis scaled between 0 and 1
        /// over the remaining bits)
        /// 
        /// Example:
        ///     If this trend toward one isn't there, then these two values would map close to each other (even though the first one represents
        ///     1, and the second could represent 201)
        ///         0 0 0 0 1
        ///         0 .1 0 0 1
        ///     This method would turn these into something like:
        ///         0 0 0 0 1
        ///         0 .1 .6 .95 1       --- bits to the right follow a sqrt toward 1
        ///         
        /// Instead of sqrt, it's actually between x^POWMIN and x^POWMAX.  The value of the bit becomes a percent from min to max
        /// </remarks>
        private static double[] ConvertToVector_LeftSignificant(double value, SOMConvertToVectorProps props)
        {
            const double POWMIN = .1;
            const double POWMAX = .04;

            // Convert to a different base
            long scaledValue = Convert.ToInt64(value * props.Number_ScaleToLong);
            int[] converted = MathND.ConvertToBase(scaledValue, props.Number_BaseConvertTo.Value);

            if (converted.Length == 0)
            {
                // Zero, return 0s
                return Enumerable.Range(0, props.Width).Select(o => 0d).ToArray();
            }
            else if (converted.Length > props.Width)
            {
                // Too big, return 1s
                double maxValue = value < 0 ? -1d : 1d;
                return Enumerable.Range(0, props.Width).Select(o => maxValue).ToArray();
            }

            // Normalize so it's between -1 and 1
            double[] normalized = new double[converted.Length];

            double baseDbl = props.Number_BaseConvertTo.Value.ToDouble();

            // Leftmost bit
            normalized[0] = converted[0].ToDouble() / baseDbl;
            double absFirst = Math.Abs(normalized[0]);

            // Bits to the right of the leftmost (their values are made to approach 1)
            if (converted.Length > 1)
            {
                // The sqrt will be between 0 and 1, so scale the x and y
                double yGap = 1d - absFirst;
                double xScale = 1d / (normalized.Length - 1);

                for (int cntr = 1; cntr < normalized.Length; cntr++)
                {
                    // Y will be between these two curves
                    double yMin = Math.Pow(cntr * xScale, POWMIN);
                    double yMax = Math.Pow(cntr * xScale, POWMAX);

                    // Treat this bit like a percent between the two curves
                    double y = UtilityCore.GetScaledValue(yMin, yMax, 0, props.Number_BaseConvertTo.Value, Math.Abs(converted[cntr]));

                    y *= yGap;
                    y += absFirst;

                    if (normalized[0] < 0)
                    {
                        y = -y;
                    }

                    normalized[cntr] = y;
                }
            }

            // Return, make sure the array is the right size
            if (normalized.Length < props.Width)
            {
                return Enumerable.Range(0, props.Width - normalized.Length).
                    Select(o => 0d).
                    Concat(normalized).
                    ToArray();
            }
            else
            {
                return normalized;
            }
        }

        /// <summary>
        /// This overload converts text to a vector
        /// </summary>
        /// <param name="uniqueNonWhitespace">This is a list of all possible characters that could be encountered (but not any characters that would cause IsWhitespace to return true)</param>  
        private static double[] ConvertToVector_Text(string text, SOMConvertToVectorProps props, char[] uniqueNonWhitespace)
        {
            if (uniqueNonWhitespace.Length == 0)
            {
                // It's all zeros
                return new double[props.Width];
            }

            int[] numbers = ConvertToVector_Text_Number(text, uniqueNonWhitespace);

            double[] normalized = ConvertToVector_Text_Normalize(numbers, uniqueNonWhitespace.Length);

            return ConvertToVector_Text_Fit(normalized, props.Width, props.Text_Justification.Value);
        }
        private static int[] ConvertToVector_Text_Number(string text, char[] uniqueNonWhitespace)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new int[0];
            }

            int[] retVal = new int[text.Length];

            for (int cntr = 0; cntr < retVal.Length; cntr++)
            {
                char current = text[cntr];

                if (!UtilityCore.IsWhitespace(current))      // just leave whitespace as zero
                {
                    retVal[cntr] = Array.IndexOf<char>(uniqueNonWhitespace, current) + 1;     // UniqueChars should hold all possible chars.  If it doesn't, IndexOf will return -1, and adding one will make it look like whitespace (but that should never happen)
                }
            }

            return retVal;
        }
        private static double[] ConvertToVector_Text_Normalize(int[] number, double numUniqueChars)
        {
            const double WHITESPACEGAP = .15;

            double[] retVal = new double[number.Length];

            for (int cntr = 0; cntr < number.Length; cntr++)
            {
                if (number[cntr] == 0)
                {
                    // Leave it zero
                    continue;
                }

                double percent = number[cntr] / numUniqueChars;

                //NOTE: 1 is actually the smallest value, so the range is from 1/numunique to 1 (so all the values in retVal will be 0 or greater than WHITESPACEGAP)
                retVal[cntr] = UtilityCore.GetScaledValue(WHITESPACEGAP, 1d, 0d, 1d, percent);
            }

            return retVal;
        }
        private static double[] ConvertToVector_Text_Fit(double[] normalized, int width, TextAlignment justify)
        {
            if (normalized.Length == width)
            {
                return normalized;
            }
            else if (normalized.Length == 0)
            {
                return new double[width];
            }

            if (normalized.Length < width)
            {
                #region Expand

                switch (justify)
                {
                    case TextAlignment.Left:
                        return normalized.
                            Concat(new double[width - normalized.Length]).
                            ToArray();

                    case TextAlignment.Right:
                        return new double[width - normalized.Length].
                            Concat(normalized).
                            ToArray();

                    case TextAlignment.Center:
                        int left = (width - normalized.Length) / 2;
                        int right = width - (left + normalized.Length);

                        return new double[left].
                            Concat(normalized).
                            Concat(new double[right]).
                            ToArray();

                    case TextAlignment.Justify:
                        // Nothing needs to be done
                        break;

                    default:
                        throw new ApplicationException("Unknown TextAlignment");
                }

                #endregion
            }

            // Create a bezier through the points, then pull points off of that curve.  Unless I read this wrong, this is what bicubic interpolation of images does (I'm just doing 1D instead of 2D)
            return BezierUtil.GetPath(width, BezierUtil.GetBezierSegments(normalized.Select(o => new Point3D(o, 0, 0)).ToArray(), isClosed: false)).
                Select(o => o.X).
                ToArray();
        }

        #endregion
    }

    #region Class: SOMFieldStats

    public class SOMFieldStats
    {
        public int Count { get; set; }
        public int UniqueCount { get; set; }

        public int MinLength { get; set; }
        public int MaxLength { get; set; }

        public SOMFieldType FieldType { get; set; }

        public char[] UniqueChars { get; set; }
        public char[] UniqueChars_NonWhitespace { get; set; }

        public double? Numeric_Min { get; set; }
        public double? Numeric_Max { get; set; }
        public double? Numeric_Avg { get; set; }
        public double? Numeric_StandDev { get; set; }

        public DateTime? Date_Min { get; set; }
        public DateTime? Date_Max { get; set; }
        public DateTime? Date_Avg { get; set; }
        public TimeSpan? Date_StandDev { get; set; }
    }

    #endregion
    #region Enum: SOMFieldType

    public enum SOMFieldType
    {
        FloatingPoint,
        Integer,
        DateTime,
        AlphaNumeric,
        AnyText,
    }

    #endregion

    #region Class: SOMConvertToVectorProps

    public class SOMConvertToVectorProps
    {
        /// <summary>
        /// This overload is for converting a number
        /// </summary>
        public SOMConvertToVectorProps(int width, int baseConvertTo, double scaleToLong)
        {
            this.Width = width;
            this.Number_BaseConvertTo = baseConvertTo;
            this.Number_ScaleToLong = scaleToLong;

            this.Text_Justification = null;
        }
        /// <summary>
        /// This overload is for converting text
        /// </summary>
        public SOMConvertToVectorProps(int width, TextAlignment justification)
        {
            this.Width = width;
            this.Text_Justification = justification;

            this.Number_BaseConvertTo = null;
            this.Number_ScaleToLong = null;
        }

        /// <summary>
        /// True: Number
        /// False: Text
        /// </summary>
        public readonly bool IsNumber;

        /// <summary>
        /// This is the number of digits required
        /// </summary>
        public readonly int Width;

        /// <summary>
        /// This is the base of the converted number
        /// </summary>
        public readonly int? Number_BaseConvertTo;
        /// <summary>
        /// This is a value that will scale a double to a long
        /// </summary>
        /// <remarks>
        /// This is needed if the double is a small range.  All this base conversion logic uses integer math, so it works best with
        /// large integers.
        /// 
        /// So if the double has values from 0 to 2.5, it would need to be scaled into larger values (like multiply by 1,000,000)
        /// </remarks>
        public readonly double? Number_ScaleToLong;

        /// <summary>
        /// This is how the text should fill dead space
        /// </summary>
        public readonly TextAlignment? Text_Justification;
    }

    #endregion
}
