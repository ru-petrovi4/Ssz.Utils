using System;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.Properties;
using Ssz.Utils;

namespace Ssz.Operator.Core
{
    public class GeometryInfoSupplier : DsUIElementPropertySupplier
    {
        #region public functions

        public const string EllipseTypeString = "Ellipse";
        public const string RectangleTypeString = "Rectangle";
        public const string RectangleRoundCornersTypeString = "RectangleRoundCorners";
        public const string HorizontalLineTypeString = "HorizontalLine";
        public const string VerticalLineTypeString = "VerticalLine";
        public const string LLineTypeString = "LLine";
        public const string LLineRoundCornerTypeString = "LLineRoundCorner";

        public override string[] GetTypesStrings()
        {
            return new[]
            {
                RectangleTypeString,
                RectangleRoundCornersTypeString,
                EllipseTypeString,
                HorizontalLineTypeString,
                VerticalLineTypeString,
                LLineTypeString,
                LLineRoundCornerTypeString,
                CustomTypeString
            };
        }


        public override string? GetPropertyXamlString(DsUIElementProperty propertyInfo, IDsContainer? container)
        {
            switch (propertyInfo.TypeString)
            {
                case EllipseTypeString:
                    return Resources.GeometryInfoSupplierEllipseXaml;
                case RectangleTypeString:
                    return Resources.GeometryInfoSupplierRectangleXaml;
                case RectangleRoundCornersTypeString:
                    return Resources.GeometryInfoSupplierRectangleRoundCornersXaml;
                case HorizontalLineTypeString:
                    return Resources.GeometryInfoSupplierHorizontalLineXaml;
                case VerticalLineTypeString:
                    return Resources.GeometryInfoSupplierVerticalLineXaml;
                case LLineTypeString:
                    return Resources.GeometryInfoSupplierLLineXaml;
                case LLineRoundCornerTypeString:
                    return Resources.GeometryInfoSupplierLLineRoundCornerXaml;
                case CustomTypeString:
                    return ConstantsHelper.ComputeValue(container, propertyInfo.CustomXamlString);
                default:
                    return null;
            }
        }

        public override string GetTypeString(string propertyXamlString)
        {
            if (StringHelper.CompareIgnoreCase(propertyXamlString, Resources.GeometryInfoSupplierEllipseXaml))
                return EllipseTypeString;
            if (StringHelper.CompareIgnoreCase(propertyXamlString, Resources.GeometryInfoSupplierRectangleXaml))
                return RectangleTypeString;
            if (StringHelper.CompareIgnoreCase(propertyXamlString,
                Resources.GeometryInfoSupplierRectangleRoundCornersXaml))
                return RectangleRoundCornersTypeString;
            if (StringHelper.CompareIgnoreCase(propertyXamlString, Resources.GeometryInfoSupplierHorizontalLineXaml))
                return HorizontalLineTypeString;
            if (StringHelper.CompareIgnoreCase(propertyXamlString, Resources.GeometryInfoSupplierVerticalLineXaml))
                return VerticalLineTypeString;
            if (StringHelper.CompareIgnoreCase(propertyXamlString, Resources.GeometryInfoSupplierLLineXaml))
                return LLineTypeString;
            if (StringHelper.CompareIgnoreCase(propertyXamlString, Resources.GeometryInfoSupplierLLineRoundCornerXaml))
                return LLineRoundCornerTypeString;
            return CustomTypeString;
        }

        #endregion
    }
}