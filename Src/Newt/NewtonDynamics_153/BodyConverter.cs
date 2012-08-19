using System;
using System.ComponentModel;

namespace Game.Newt.NewtonDynamics_153
{
    public class BodyConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
                return true;
            else
                return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if (value is string)
            {
                switch((string)value)
                {
                    case "ConvexBody3D":
                        return new ConvexBody3D();
                    case "TerrianBody3D":
                        return new TerrianBody3D();
                    case "null":
                        return new VisualNullBody3D();
                    default:
                        throw new InvalidOperationException(string.Format("\"{0}\" is not a valid Body.", value));
                }
            }
            else
                return base.ConvertFrom(context, culture, value);
        }
    }
}
