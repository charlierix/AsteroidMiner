using System;
using System.ComponentModel;

namespace Game.Newt.v1.NewtonDynamics1
{
    class CollisionMaskConverter : TypeConverter
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
                switch ((string)value)
                {
                    case "GeometryCollisionMask3D":
                        return new GeometryCollisionMask3D();
                    case "TerrianCollisionMask3D":
                        return new TerrianCollisionMask3D();
                    case "CollisionCloud":
                        return new CollisionCloud();
                    case "null":
                        return new NullCollision();
                    default:
                        throw new InvalidOperationException(string.Format("\"{0}\" is not a valid Collision Mask.", value));
                }
            }
            else
                return base.ConvertFrom(context, culture, value);
        }
    }
}
