using System.Windows.Markup;

using Ssz.Utils;

namespace Ssz.Operator.Core
{
    public class DsUIElementPropertyValueSerializer : ValueSerializer
    {
        #region public functions

        public static readonly DsUIElementPropertyValueSerializer Instance =
            new();

        public override bool CanConvertFromString(string value, IValueSerializerContext? context)
        {
            return true;
        }

        public override object? ConvertFromString(string value, IValueSerializerContext? context)
        {
            var propertyInfo = new DsUIElementProperty();

            if (string.IsNullOrWhiteSpace(value)) return propertyInfo;

            propertyInfo.TypeString = value;

            return propertyInfo;
        }

        public override bool CanConvertToString(object value, IValueSerializerContext? context)
        {
            var propertyInfo = value as DsUIElementProperty;
            if (propertyInfo is null) return false;
            if (StringHelper.CompareIgnoreCase(propertyInfo.TypeString, DsUIElementPropertySupplier.CustomTypeString))
                return false;

            return true;
        }

        public override string? ConvertToString(object value, IValueSerializerContext? context)
        {
            var propertyInfo = value as DsUIElementProperty;
            if (propertyInfo is null) return null;
            if (StringHelper.CompareIgnoreCase(propertyInfo.TypeString, DsUIElementPropertySupplier.CustomTypeString)
            ) return null;

            return propertyInfo.TypeString;
        }

        #endregion
    }
}