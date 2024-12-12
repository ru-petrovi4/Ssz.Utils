using System;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.Properties;
using Ssz.Utils;

namespace Ssz.Operator.Core
{
    public class ButtonStyleInfoSupplier : DsUIElementPropertySupplier
    {
        #region public functions

        public const string ClassicStyleTypeString = "Classic";
        public const string MainStyleTypeString = "Main";
        public const string TrivialStyleTypeString = "Trivial";
        public const string InvisibleStyleTypeString = "Invisible";

        public override string[] GetTypesStrings()
        {
            return new[]
            {
                DefaultTypeString,
                ClassicStyleTypeString,
                MainStyleTypeString,
                TrivialStyleTypeString,
                InvisibleStyleTypeString,
                CustomTypeString
            };
        }

        public override string? GetPropertyXamlString(DsUIElementProperty propertyInfo, IDsContainer? container)
        {
            switch (propertyInfo.TypeString)
            {
                case DefaultTypeString:
                    return Resources.DefaultButtonStyleXaml;
                case ClassicStyleTypeString:
                    return Resources.ClassicButtonStyleXaml;
                case MainStyleTypeString:
                    return Resources.MainButtonStyleXaml;
                case TrivialStyleTypeString:
                    return Resources.TrivialButtonStyleXaml;
                case InvisibleStyleTypeString:
                    return Resources.InvisibleButtonStyleXaml;
                case CustomTypeString:
                    return ConstantsHelper.ComputeValue(container, propertyInfo.CustomXamlString);
                default:
                    throw new NotSupportedException();
            }
        }

        public override string GetTypeString(string propertyXamlString)
        {
            if (StringHelper.CompareIgnoreCase(propertyXamlString, Resources.DefaultButtonStyleXaml))
                return DefaultTypeString;
            if (StringHelper.CompareIgnoreCase(propertyXamlString, Resources.ClassicButtonStyleXaml))
                return ClassicStyleTypeString;
            if (StringHelper.CompareIgnoreCase(propertyXamlString, Resources.MainButtonStyleXaml))
                return MainStyleTypeString;
            if (StringHelper.CompareIgnoreCase(propertyXamlString, Resources.TrivialButtonStyleXaml))
                return TrivialStyleTypeString;
            if (StringHelper.CompareIgnoreCase(propertyXamlString, Resources.InvisibleButtonStyleXaml))
                return InvisibleStyleTypeString;
            return CustomTypeString;
        }

        #endregion
    }
}