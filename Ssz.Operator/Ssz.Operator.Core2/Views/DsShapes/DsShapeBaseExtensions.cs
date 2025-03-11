using System;
using Avalonia;
using Ssz.Operator.Core.ControlsDesign;
using Ssz.Operator.Core.Drawings;

namespace Ssz.Operator.Core.DsShapes
{
    public static class DsShapeBaseExtensions
    {
        #region public functions

        public static DsShapeInfo GetDsShapeInfo(this DsShapeBase dsShape)
        {
            return new()
            {
                Name = dsShape.Name,
                Desc = dsShape.Desc,
                DsShapeTypeNameToDisplay = dsShape.GetDsShapeTypeNameToDisplay(),
                IsRootDsShape = dsShape.IsRootDsShape(),
                Index = dsShape.Index                
            };
        }

        public static bool IsRootDsShape(this DsShapeBase dsShape)
        {
            return (dsShape.ParentItem as ComplexDsShape) is null;
        }

        public static ComplexDsShape? GetParentComplexDsShape(this DsShapeBase dsShape)
        {
            return dsShape.ParentItem as ComplexDsShape;
        }

        public static DrawingBase? GetParentDrawing(this DsShapeBase dsShape)
        {
            return dsShape.ParentItem.Find<DrawingBase>();
        }

        public static string GetDsShapeNameBase(this DsShapeBase dsShape, out int dsShapeNameNumber)
        {
            string name = dsShape.Name;
            dsShapeNameNumber = 1;
            if (string.IsNullOrWhiteSpace(name)) return name;
            var i = name.LastIndexOf('-');
            if (i < 0 || i >= name.Length - 1) return name;
            if (int.TryParse(name.Substring(i + 1), out dsShapeNameNumber))
                return name.Substring(0, i);
            return name;
        }

        public static string GetDsShapeNameToDisplayAndType(this DsShapeBase dsShape)
        {
            if (string.IsNullOrEmpty(dsShape.Name)) return "[" + dsShape.GetDsShapeTypeNameToDisplay() + "]";
            return dsShape.Name + " [" + dsShape.GetDsShapeTypeNameToDisplay() + "]";
        }

        public static string GetDsShapePath(this DsShapeBase? parentComplexDsShape)
        {
            if (parentComplexDsShape is null) return "";
            string elementName = parentComplexDsShape.Name;
            while (true)
            {
                parentComplexDsShape = parentComplexDsShape.GetParentComplexDsShape();
                if (parentComplexDsShape is null) break;
                elementName = parentComplexDsShape.Name + @"/" + elementName;
            }

            return elementName;
        }

        public static bool Contains(this DsShapeBase dsShape, Point drawingPoint, bool testResizeThumb)
        {
            if (dsShape is null) return false;

            var dsShapePoint = GetDsShapePoint(dsShape, drawingPoint);
            var width = dsShape.WidthInitialNotRounded;
            var height = dsShape.HeightInitialNotRounded;

            //if (testResizeThumb)
            //    return -DesignDsShapeView.ResizeThumbThikness <= dsShapePoint.X &&
            //           dsShapePoint.X <= width + DesignDsShapeView.ResizeThumbThikness &&
            //           -DesignDsShapeView.ResizeThumbThikness <= dsShapePoint.Y &&
            //           dsShapePoint.Y <= height + DesignDsShapeView.ResizeThumbThikness;

            return 0 <= dsShapePoint.X && dsShapePoint.X <= width &&
                   0 <= dsShapePoint.Y && dsShapePoint.Y <= height;
        }

        //public static bool ResizeThumbContains(this DsShapeBase dsShape, Point dsShapePoint)
        //{
        //    if (dsShape is null) return false;

        //    var width = dsShape.WidthInitialNotRounded;
        //    var height = dsShape.HeightInitialNotRounded;

        //    if (-DesignDsShapeView.ResizeThumbThikness <= dsShapePoint.X && dsShapePoint.X <= 0 &&
        //        -DesignDsShapeView.ResizeThumbThikness <= dsShapePoint.Y &&
        //        dsShapePoint.Y <= height + DesignDsShapeView.ResizeThumbThikness ||
        //        -DesignDsShapeView.ResizeThumbThikness <= dsShapePoint.X &&
        //        dsShapePoint.X <= width + DesignDsShapeView.ResizeThumbThikness &&
        //        -DesignDsShapeView.ResizeThumbThikness <= dsShapePoint.Y && dsShapePoint.Y <= 0 ||
        //        width <= dsShapePoint.X && dsShapePoint.X <= width + DesignDsShapeView.ResizeThumbThikness &&
        //        -DesignDsShapeView.ResizeThumbThikness <= dsShapePoint.Y &&
        //        dsShapePoint.Y <= height + DesignDsShapeView.ResizeThumbThikness ||
        //        -DesignDsShapeView.ResizeThumbThikness <= dsShapePoint.X &&
        //        dsShapePoint.X <= width + DesignDsShapeView.ResizeThumbThikness &&
        //        height <= dsShapePoint.Y &&
        //        dsShapePoint.Y <= height + DesignDsShapeView.ResizeThumbThikness) return true;

        //    return false;
        //}

        public static Point GetCenterInitialPositionOnDrawing(this DsShapeBase dsShape)
        {
            var p = dsShape.CenterInitialPositionNotRounded;

            var parentComplexDsShape = dsShape.GetParentComplexDsShape();
            if (parentComplexDsShape is not null) p = parentComplexDsShape.GetDrawingPoint(p);

            return p;
        }

        public static ComplexDsShape? GetRootComplexDsShape(this DsShapeBase dsShape)
        {
            ComplexDsShape? rootComplexDsShape = null;
            var parentComplexDsShape = dsShape.GetParentComplexDsShape();
            while (parentComplexDsShape is not null)
            {
                rootComplexDsShape = parentComplexDsShape;
                parentComplexDsShape = parentComplexDsShape.GetParentComplexDsShape();
            }

            return rootComplexDsShape;
        }

        public static Point GetDrawingPoint(this DsShapeBase dsShape, Point dsShapePoint)
        {
            dsShapePoint += new Point(dsShape.LeftNotTransformed, dsShape.TopNotTransformed);

            if (dsShape.IsFlipped || dsShape.AngleInitial != 0.0)
            {
                var center = dsShape.CenterInitialPositionNotRounded;
                dsShapePoint += new Point(-center.X, -center.Y);
                dsShapePoint = dsShape.GetTransformGroup().Value.Transform(dsShapePoint);
                dsShapePoint += new Point(center.X, center.Y);
            }

            var parentComplexDsShape = dsShape.GetParentComplexDsShape();
            if (parentComplexDsShape is not null)
                dsShapePoint = parentComplexDsShape.GetDrawingPoint(dsShapePoint);

            return dsShapePoint;
        }

        public static Point GetDsShapePoint(this DsShapeBase dsShape, Point drawingPoint)
        {
            var parentComplexDsShape = dsShape.GetParentComplexDsShape();
            if (parentComplexDsShape is not null) drawingPoint = parentComplexDsShape.GetDsShapePoint(drawingPoint);

            if (dsShape.IsFlipped || dsShape.AngleInitial != 0.0)
            {
                var center = dsShape.CenterInitialPositionNotRounded;
                drawingPoint += new Point(-center.X, -center.Y);
                drawingPoint = dsShape.GetInverseTransformGroup().Value.Transform(drawingPoint);
                drawingPoint += new Point(center.X, center.Y);
            }

            drawingPoint += new Point(-dsShape.LeftNotTransformed, -dsShape.TopNotTransformed);

            return drawingPoint;
        }

        #endregion
    }
}