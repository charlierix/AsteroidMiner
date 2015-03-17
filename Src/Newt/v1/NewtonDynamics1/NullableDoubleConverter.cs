using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Game.Newt.v1.NewtonDynamics1
{
    [ValueConversion(typeof(double?), typeof(string))]
    [ValueConversion(typeof(string), typeof(double?))]
    public class NullableDoubleConverter : ValidationRule,
        IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType == typeof(double?))
            {
                if (value is string)
                {
                    string s = (string)value;
                    if (s == "")
                        return null;
                    else
                    {
                        double result;
                        if (double.TryParse(s, out result))
                            return result;
                    }
                }
            }
            else if (targetType == typeof(string))
            {
                if (value == null)
                    return "";
                else if (value is double)
                   return value.ToString();
            }

            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Convert(value, targetType, parameter, culture);
        }

        #endregion

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (value is string)
            {
                if (ConvertBack(value, value.GetType(), null, cultureInfo) == DependencyProperty.UnsetValue)
                    return new ValidationResult(false, "The number is not valid.");
            }

            return null;
        }
    }
}
