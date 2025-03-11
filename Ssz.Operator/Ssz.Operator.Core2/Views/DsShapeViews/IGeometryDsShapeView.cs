using System;
using Avalonia.Media;
using Microsoft.Extensions.Logging;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.ControlsDesign;


using Ssz.Operator.Core.Utils;
using Ssz.Utils;

namespace Ssz.Operator.Core.DsShapeViews
{
    public interface IGeometryDsShapeView
    {
        Geometry? Geometry { get; }

        DsShapeViewModel DsShapeViewModel { get; }

        event Action GeometryChanged;

        void UpdateModelLayer();
    }

    public static class GeometryHelper
    {
        #region public functions

        public static Geometry? GetGeometry(DsUIElementProperty geometryInfo,
            DsUIElementPropertySupplier uiElementPropertyInfoSupplier, IDsContainer? container,
            bool geometryEditingMode = false,
            double width = double.NaN, double height = double.NaN, double strokeThickness = double.NaN)
        {
            Geometry? geometry = null;
            var geometryPathString = uiElementPropertyInfoSupplier.GetPropertyXamlString(geometryInfo, container);
            if (!string.IsNullOrWhiteSpace(geometryPathString))
            {
                geometryPathString = geometryPathString!.Trim();
                try
                {
                    if (StringHelper.StartsWithIgnoreCase(geometryPathString, @"<"))
                    {
                        geometry = XamlHelper.Load(geometryPathString) as Geometry;
                    }
                    else
                    {
                        geometry = Geometry.Parse(geometryPathString);
                        //if (geometryInfo.TypeString == DsUIElementPropertySupplier.CustomTypeString &&
                        //    ConstantsHelper.ContainsQuery(geometryInfo.CustomXamlString))
                        //{
                        //    // Do nothing
                        //}
                        //else
                        //{
                        //    if (geometryEditingMode && geometry is not null)
                        //    {
                        //        var pathGeometry = new PathGeometry();
                        //        if (geometry is StreamGeometry streamGeometry)
                        //        {
                        //            using (var context = pathGeometry.Open())
                        //            {
                        //                streamGeometry.Open().CopyTo(context);
                        //            }
                        //        }

                        //        var pathGeometry = PathGeometry.CreateFromGeometry(geometry);
                        //        if (pathGeometry is not null)
                        //        {
                        //            pathGeometry.Normalize(width, height, strokeThickness);
                        //            geometry = pathGeometry;
                        //        }
                        //    }
                        //}
                    }
                }
                catch (Exception ex)
                {
                    geometry = null;
                    DsProject.LoggersSet.Logger.LogWarning(ex, @"Incorrect path string.");
                }
            }

            return geometry;
        }

        #endregion
    }
}