using System;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.Properties;
using Ssz.Utils;

namespace Ssz.Operator.Core
{
    public class GeometryButtonStyleInfoSupplier : DsUIElementPropertySupplier
    {
        #region public functions

        public override string[] GetTypesStrings()
        {
            return new[]
            {
                DefaultTypeString,
                CustomTypeString
            };
        }

        public override string GetPropertyXamlString(DsUIElementProperty propertyInfo, IDsContainer? container)
        {
            switch (propertyInfo.TypeString)
            {
                case DefaultTypeString:
                    return Resources.DefaultGeometryButtonStyleXaml;
                case CustomTypeString:
                    return ConstantsHelper.ComputeValue(container, propertyInfo.CustomXamlString)!;
                default:
                    throw new NotSupportedException();
            }
        }

        public override string GetTypeString(string propertyXamlString)
        {
            if (StringHelper.CompareIgnoreCase(propertyXamlString, Resources.DefaultGeometryButtonStyleXaml))
                return DefaultTypeString;
            return CustomTypeString;
        }

        #endregion
    }
}