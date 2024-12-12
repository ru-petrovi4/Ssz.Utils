using System;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.Properties;
using Ssz.Utils;

namespace Ssz.Operator.Core
{
    public class ComboBoxStyleInfoSupplier : DsUIElementPropertySupplier
    {
        #region public functions

        public const string ClassicStyleTypeString = "Classic";

        public override string[] GetTypesStrings()
        {
            return new[]
            {
                DefaultTypeString,
                ClassicStyleTypeString,
                CustomTypeString
            };
        }

        public override string GetPropertyXamlString(DsUIElementProperty propertyInfo, IDsContainer? container)
        {
            switch (propertyInfo.TypeString)
            {
                case DefaultTypeString:
                    return Resources.DefaultComboBoxStyleXaml;
                case ClassicStyleTypeString:
                    return Resources.ClassicComboBoxStyleXaml;
                case CustomTypeString:
                    return ConstantsHelper.ComputeValue(container, propertyInfo.CustomXamlString)!;
                default:
                    throw new NotSupportedException();
            }
        }

        public override string GetTypeString(string propertyXamlString)
        {
            if (StringHelper.CompareIgnoreCase(propertyXamlString, Resources.DefaultComboBoxStyleXaml))
                return DefaultTypeString;
            if (StringHelper.CompareIgnoreCase(propertyXamlString, Resources.ClassicComboBoxStyleXaml))
                return ClassicStyleTypeString;
            return CustomTypeString;
        }

        #endregion
    }
}