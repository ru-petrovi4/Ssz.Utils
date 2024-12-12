using System;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.Properties;
using Ssz.Utils;

namespace Ssz.Operator.Core
{
    public class ToggleButtonStyleInfoSupplier : DsUIElementPropertySupplier
    {
        #region public functions

        public const string ClassicStyleTypeString = "Classic";
        public const string TrivialStyleTypeString = "Trivial";
        public const string InvisibleStyleTypeString = "Invisible";

        public override string[] GetTypesStrings()
        {
            return new[]
            {
                DefaultTypeString,
                ClassicStyleTypeString,
                TrivialStyleTypeString,
                InvisibleStyleTypeString,
                CustomTypeString
            };
        }

        public override string GetPropertyXamlString(DsUIElementProperty propertyInfo, IDsContainer? container)
        {
            switch (propertyInfo.TypeString)
            {
                case DefaultTypeString:
                    return Resources.DefaultToggleButtonStyleXaml;
                case ClassicStyleTypeString:
                    return Resources.ClassicToggleButtonStyleXaml;
                case TrivialStyleTypeString:
                    return Resources.TrivialToggleButtonStyleXaml;
                case InvisibleStyleTypeString:
                    return Resources.InvisibleToggleButtonStyleXaml;
                case CustomTypeString:
                    return ConstantsHelper.ComputeValue(container, propertyInfo.CustomXamlString)!;
                default:
                    throw new NotSupportedException();
            }
        }

        public override string GetTypeString(string propertyXamlString)
        {
            if (StringHelper.CompareIgnoreCase(propertyXamlString, Resources.DefaultToggleButtonStyleXaml))
                return DefaultTypeString;
            if (StringHelper.CompareIgnoreCase(propertyXamlString, Resources.ClassicToggleButtonStyleXaml))
                return ClassicStyleTypeString;
            if (StringHelper.CompareIgnoreCase(propertyXamlString, Resources.TrivialToggleButtonStyleXaml))
                return TrivialStyleTypeString;
            if (StringHelper.CompareIgnoreCase(propertyXamlString, Resources.InvisibleToggleButtonStyleXaml))
                return InvisibleStyleTypeString;
            return CustomTypeString;
        }

        #endregion
    }
}