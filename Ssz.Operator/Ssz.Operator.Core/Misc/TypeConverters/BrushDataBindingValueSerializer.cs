using System.Windows.Markup;

namespace Ssz.Operator.Core
{
    public class BrushDataBindingValueSerializer : ValueSerializer
    {
        #region public functions

        public override bool CanConvertToString(object value, IValueSerializerContext? context)
        {
            var brushDataBinding = value as BrushDataBinding;
            if (brushDataBinding is not null && brushDataBinding.IsConst && brushDataBinding.ConstValue is not null)
                return DsBrushValueSerializer.Instance.CanConvertToString(brushDataBinding.ConstValue, context);
            return false;
        }

        public override string? ConvertToString(object value, IValueSerializerContext? context)
        {
            var brushDataBinding = value as BrushDataBinding;
            if (brushDataBinding is not null && brushDataBinding.IsConst && brushDataBinding.ConstValue is not null)
                return DsBrushValueSerializer.Instance.ConvertToString(brushDataBinding.ConstValue, context);
            return null;
        }

        #endregion
    }
}