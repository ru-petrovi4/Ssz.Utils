using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.Properties;
using Ssz.Utils;
using System;


namespace Ssz.Operator.Core
{
    public class TabStyleInfoSupplier : DsUIElementPropertySupplier
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
            if (propertyInfo == null) throw new ArgumentNullException(@"propertyInfo");

            switch (propertyInfo.TypeString)
            {
                case DefaultTypeString:
                    return Resources.DefaultTabStyleXaml;
                case ClassicStyleTypeString:
                    return Resources.ClassicTabStyleXaml;
                case CustomTypeString:
                    return ConstantsHelper.ComputeValue(container, propertyInfo.CustomXamlString)!;
                default:
                    throw new NotSupportedException();
            }
        }

        public override string GetTypeString(string propertyXamlString)
        {
            if (StringHelper.CompareIgnoreCase(propertyXamlString, Resources.DefaultTabStyleXaml))
                return DefaultTypeString;
            if (StringHelper.CompareIgnoreCase(propertyXamlString, Resources.ClassicTabStyleXaml))
                return ClassicStyleTypeString;
            return CustomTypeString;
        }

        #endregion
    }
}
