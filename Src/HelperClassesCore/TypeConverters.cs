using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace Game.HelperClassesCore
{
    #region class: DblArrTypeConverter

    /// <summary>
    /// XamlServices.Save is really inefficient about storing double arrays.  This stores as a pipe delimited string
    /// TODO: Make similar classes for int, Vector3D, and other highly repeated array types
    /// </summary>
    /// <remarks>
    /// Got this here:
    /// http://ludovic.chabant.com/devblog/2008/06/25/almost-everything-you-need-to-know-about-xaml-serialization-part-2/
    /// 
    /// By default, XamlServices stores each value in an array as <double>1.1</double>
    /// 
    /// Put this attribute over properties that should use this converter:
    /// [TypeConverter(typeof(DblArrTypeConverter))]
    /// </remarks>
    public class DblArrTypeConverter : TypeConverter
    {
        public const string DELIMITER = "|";

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(MarkupExtension))
            {
                return true;
            }
            else
            {
                return base.CanConvertTo(context, destinationType);
            }
        }
        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(MarkupExtension))
            {
                double[] values = (double[])value;
                DblArrExtension extension = new DblArrExtension() { Ds = string.Join(DELIMITER, values) };
                return extension;
            }
            else
            {
                return base.ConvertTo(context, culture, value, destinationType);
            }
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(MarkupExtension))
            {
                return true;
            }
            else
            {
                return base.CanConvertFrom(context, sourceType);
            }
        }
        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if (value is DblArrExtension)
            {
                DblArrExtension extension = (DblArrExtension)value;
                return extension.Ds;
            }
            else
            {
                return base.ConvertFrom(context, culture, value);
            }
        }
    }

    #endregion
    #region class: DblArrExtension

    /// <summary>
    /// XamlServices.Save won't just go between double[] and string.  It needs to go through something that inherits MarkupExtension.
    /// NOTE: This is named as small as possible to cut down on space (the word extension doesn't get stored in the xml, it's just a convention)
    /// </summary>
    public class DblArrExtension : MarkupExtension
    {
        public string Ds { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this.Ds.Split(DblArrTypeConverter.DELIMITER.ToCharArray()).
                Select(o => Convert.ToDouble(o)).
                ToArray();
        }
    }

    #endregion
}
