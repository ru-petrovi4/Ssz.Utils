using System;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.Properties;
using Ssz.Utils;

namespace Ssz.Operator.Core
{
    public class ChartItemPointGeometryInfoSupplier : DsUIElementPropertySupplier
    {
        #region public functions

        public const string CrossTypeString = "Cross";
        public const string CircleTypeString = "Circle";

        public override string[] GetTypesStrings()
        {
            return new[]
            {
                NoneTypeString,
                CrossTypeString,
                CircleTypeString,
                CustomTypeString
            };
        }


        public override string? GetPropertyXamlString(DsUIElementProperty propertyInfo, IDsContainer? container)
        {
            switch (propertyInfo.TypeString)
            {
                case NoneTypeString:
                    return @"";
                case CrossTypeString:
                    return Resources.MultiChartDsShapeGeometryInfoSupplierCrossXaml;
                case CircleTypeString:
                    return Resources.MultiChartDsShapeGeometryInfoSupplierCircleXaml;
                case CustomTypeString:
                    return ConstantsHelper.ComputeValue(container, propertyInfo.CustomXamlString);
                default:
                    return null;
            }
        }

        public override string GetTypeString(string propertyXamlString)
        {
            if (string.IsNullOrWhiteSpace(propertyXamlString))
                return NoneTypeString;
            if (StringHelper.CompareIgnoreCase(propertyXamlString,
                Resources.MultiChartDsShapeGeometryInfoSupplierCrossXaml))
                return CrossTypeString;
            if (StringHelper.CompareIgnoreCase(propertyXamlString,
                Resources.MultiChartDsShapeGeometryInfoSupplierCircleXaml))
                return CircleTypeString;
            return CustomTypeString;
        }

        #endregion
    }
}