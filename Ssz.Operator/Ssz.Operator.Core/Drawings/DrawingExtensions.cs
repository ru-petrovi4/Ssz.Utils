using System.Windows.Media;

namespace Ssz.Operator.Core.Drawings
{
    public static class DrawingExtensions
    {
        #region public functions

        public static DsPageStretchMode ComputeDsPageStretchMode(this DsPageDrawing dsPageDrawing)
        {
            if (dsPageDrawing.StretchMode != DsPageStretchMode.Default)
                return dsPageDrawing.StretchMode;
            return DsProject.Instance.DsPageStretchMode;
        }


        public static DsPageHorizontalAlignment ComputeDsPageHorizontalAlignment(
            this DsPageDrawing dsPageDrawing)
        {
            if (dsPageDrawing.HorizontalAlignment != DsPageHorizontalAlignment.Default)
                return dsPageDrawing.HorizontalAlignment;
            return DsProject.Instance.DsPageHorizontalAlignment;
        }


        public static DsPageVerticalAlignment ComputeDsPageVerticalAlignment(this DsPageDrawing dsPageDrawing)
        {
            if (dsPageDrawing.VerticalAlignment != DsPageVerticalAlignment.Default)
                return dsPageDrawing.VerticalAlignment;
            return DsProject.Instance.DsPageVerticalAlignment;
        }


        public static Brush? ComputeDsPageBackgroundBrush(this DsPageDrawing? dsPageDrawing)
        {
            if (dsPageDrawing is not null && dsPageDrawing.Background is not null)
                return dsPageDrawing.Background.GetBrush(dsPageDrawing);

            if (DsProject.Instance.DsPageBackground is not null)
                return DsProject.Instance.DsPageBackground.GetBrush(DsProject.Instance);
            return null;
        }

        public static void CropUnusedSpace(this DrawingBase drawing)
        {
            if (drawing.DsShapes.Length == 0) return;
            var rect = drawing.GetBoundingRectOfAllDsShapes();
            drawing.ResizeHorizontalFromRight(rect.Right - drawing.Width);
            drawing.ResizeHorizontalFromLeft(rect.Left);
            drawing.ResizeVerticalFromBottom(rect.Bottom - drawing.Height);
            drawing.ResizeVerticalFromTop(rect.Top);
        }

        public static object? GetUnderlyingContent(this DsPageDrawing dsPageDrawing)
        {
            if (dsPageDrawing.UnderlyingXaml.IsEmpty) return null;
            try
            {
                return XamlHelper.Load(dsPageDrawing.UnderlyingXaml.Xaml);
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }
}