using System;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.Properties;
using Ssz.Utils;

namespace Ssz.Operator.Core
{
    public class SliderStyleInfoSupplier : DsUIElementPropertySupplier
    {
        #region public functions

        public const string ClassicStyleTypeString = "Classic";
        public const string TouchScreenStyleTypeString = "TouchScreen";
        public const string ContentStyleTypeString = "Content";

        public override string[] GetTypesStrings()
        {
            return new[]
            {
                DefaultTypeString,
                //ClassicStyleTypeString,
                TouchScreenStyleTypeString,
                ContentStyleTypeString,
                CustomTypeString
            };
        }

        public override string GetPropertyXamlString(DsUIElementProperty propertyInfo, IDsContainer? container)
        {
            switch (propertyInfo.TypeString)
            {
                case DefaultTypeString:
                    return "";
                case ClassicStyleTypeString:
                    return Resources.ClassicSliderStyleXaml;
                case TouchScreenStyleTypeString:
                    return Resources.TouchScreenSliderStyleXaml;
                case ContentStyleTypeString:
                    return Resources.ContentSliderStyleXaml;
                case CustomTypeString:
                    return ConstantsHelper.ComputeValue(container, propertyInfo.CustomXamlString)!;
                default:
                    throw new NotSupportedException();
            }
        }

        public override string GetTypeString(string propertyXamlString)
        {
            if (string.IsNullOrWhiteSpace(propertyXamlString)) return DefaultTypeString;
            if (StringHelper.CompareIgnoreCase(propertyXamlString, Resources.ClassicSliderStyleXaml))
                return ClassicStyleTypeString;
            if (StringHelper.CompareIgnoreCase(propertyXamlString, Resources.TouchScreenSliderStyleXaml))
                return TouchScreenStyleTypeString;
            if (StringHelper.CompareIgnoreCase(propertyXamlString, Resources.ContentSliderStyleXaml))
                return ContentStyleTypeString;
            return CustomTypeString;
        }

        #endregion
    }
}