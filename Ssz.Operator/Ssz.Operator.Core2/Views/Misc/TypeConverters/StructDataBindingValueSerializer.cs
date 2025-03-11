using Avalonia.Markup;
using Ssz.Operator.Core.Utils;

namespace Ssz.Operator.Core
{
    //public class StructDataBindingValueSerializer<T> // : ValueSerializer
    //    where T : struct
    //{
    //    #region public functions

    //    public override bool CanConvertToString(object value, IValueSerializerContext? context)
    //    {
    //        var dataSourceInfo = value as StructDataBinding<T>;
    //        if (dataSourceInfo is not null && dataSourceInfo.IsConst) return true;
    //        return false;
    //    }

    //    public override string? ConvertToString(object value, IValueSerializerContext? context)
    //    {
    //        var dataSourceInfo = value as StructDataBinding<T>;
    //        if (dataSourceInfo is not null && dataSourceInfo.IsConst)
    //            return ObsoleteAnyHelper.ConvertTo<string>(dataSourceInfo.ConstValue, false);
    //        return null;
    //    }

    //    #endregion
    //}
}