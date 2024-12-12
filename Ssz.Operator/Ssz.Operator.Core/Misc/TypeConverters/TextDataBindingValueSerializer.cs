using System.Windows.Markup;

namespace Ssz.Operator.Core
{
    public class TextDataBindingValueSerializer : ValueSerializer
    {
        #region public functions

        public override bool CanConvertToString(object value, IValueSerializerContext? context)
        {
            var dataSourceInfo = value as TextDataBinding;
            if (dataSourceInfo is not null && dataSourceInfo.IsConst) return true;
            return false;
        }

        public override string? ConvertToString(object value, IValueSerializerContext? context)
        {
            var dataSourceInfo = value as TextDataBinding;
            if (dataSourceInfo is not null && dataSourceInfo.IsConst) return dataSourceInfo.ConstValue;
            return null;
        }

        #endregion
    }
}