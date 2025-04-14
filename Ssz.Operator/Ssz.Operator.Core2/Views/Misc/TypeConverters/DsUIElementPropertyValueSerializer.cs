using Avalonia.Markup;
using Ssz.Operator.Core.Utils;
using Ssz.Utils;

namespace Ssz.Operator.Core
{
    public class DsUIElementPropertyValueSerializer // : ValueSerializer
    {
        #region public functions

        public static readonly DsUIElementPropertyValueSerializer Instance =
            new();

        public bool CanConvertFromString(string value, IValueSerializerContext? context)
        {
            return true;
        }

        public object? ConvertFromString(string value, IValueSerializerContext? context)
        {
            var propertyInfo = new DsUIElementProperty();

            if (string.IsNullOrWhiteSpace(value)) return propertyInfo;

            propertyInfo.TypeString = value;

            return propertyInfo;
        }

        public bool CanConvertToString(object value, IValueSerializerContext? context)
        {
            var propertyInfo = value as DsUIElementProperty;
            if (propertyInfo is null) return false;
            if (StringHelper.CompareIgnoreCase(propertyInfo.TypeString, DsUIElementPropertySupplier.CustomTypeString))
                return false;

            return true;
        }

        public string? ConvertToString(object value, IValueSerializerContext? context)
        {
            var propertyInfo = value as DsUIElementProperty;
            if (propertyInfo is null) return null;
            if (StringHelper.CompareIgnoreCase(propertyInfo.TypeString, DsUIElementPropertySupplier.CustomTypeString))
                return null;

            return propertyInfo.TypeString;
        }

        #endregion
    }
}