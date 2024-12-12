using System;
using System.Collections;
using System.Linq;
using System.Windows;
using Ssz.Operator.Core.Drawings;

namespace Ssz.Operator.Core.DsShapes
{
    public static class ComplexDsShapeExtensions
    {
        #region public functions

        public static DsShapeBase[] Ungroup(this ComplexDsShape complexDsShape, bool replaceConstants)
        {
            var complexDsShapeCenterX =
                complexDsShape.WidthInitialNotRounded * complexDsShape.CenterRelativePosition.X;
            var complexDsShapeCenterY =
                complexDsShape.HeightInitialNotRounded * complexDsShape.CenterRelativePosition.Y;
            var complexDsShapeAngleRadians = Math.PI * complexDsShape.AngleInitial / 180;

            string xaml = XamlHelper.Save(complexDsShape.DsShapesArray ?? new ArrayList());
            DsShapeBase[] copyDsShapes =
                ((ArrayList) (XamlHelper.Load(xaml) ?? new ArrayList())).OfType<DsShapeBase>().ToArray();

            foreach (DsShapeBase copyDsShape in copyDsShapes)
            {
                var centerInitialPositionNotRounded = copyDsShape.CenterInitialPositionNotRounded;
                centerInitialPositionNotRounded.X = centerInitialPositionNotRounded.X - complexDsShapeCenterX;
                centerInitialPositionNotRounded.Y = centerInitialPositionNotRounded.Y - complexDsShapeCenterY;
                var angleRadians = Math.Atan2(centerInitialPositionNotRounded.Y, centerInitialPositionNotRounded.X) +
                                   complexDsShapeAngleRadians;
                var length = Math.Sqrt(Math.Pow(centerInitialPositionNotRounded.X, 2) +
                                       Math.Pow(centerInitialPositionNotRounded.Y, 2));
                centerInitialPositionNotRounded.X = length * Math.Cos(angleRadians);
                centerInitialPositionNotRounded.X = centerInitialPositionNotRounded.X +
                                                    complexDsShape.CenterInitialPositionNotRounded.X;
                centerInitialPositionNotRounded.Y = length * Math.Sin(angleRadians);
                centerInitialPositionNotRounded.Y = centerInitialPositionNotRounded.Y +
                                                    complexDsShape.CenterInitialPositionNotRounded.Y;
                copyDsShape.CenterInitialPosition = centerInitialPositionNotRounded;
                copyDsShape.AngleInitial += complexDsShape.AngleInitial;

                if (replaceConstants)
                {
                    copyDsShape.ParentItem = complexDsShape;
                    copyDsShape.ReplaceConstants(copyDsShape.Container);
                }
            }

            return copyDsShapes;
        }


        public static void ResizeHorizontalFromLeft(this ComplexDsShape complexDsShape, double horizontalChange)
        {
            foreach (DsShapeBase dsShape in complexDsShape.DsShapes)
            {
                var point = dsShape.CenterInitialPositionNotRounded;

                point.X = point.X - horizontalChange;

                dsShape.CenterInitialPosition = point;
            }

            complexDsShape.WidthInitial = complexDsShape.WidthInitialNotRounded - horizontalChange;
        }


        public static void ResizeVerticalFromTop(this ComplexDsShape complexDsShape, double verticalChange)
        {
            foreach (DsShapeBase dsShape in complexDsShape.DsShapes)
            {
                var point = dsShape.CenterInitialPositionNotRounded;

                point.Y = point.Y - verticalChange;

                dsShape.CenterInitialPosition = point;
            }

            complexDsShape.HeightInitial = complexDsShape.HeightInitialNotRounded - verticalChange;
        }


        public static void ResizeHorizontalFromRight(this ComplexDsShape complexDsShape, double horizontalChange)
        {
            complexDsShape.WidthInitial = complexDsShape.WidthInitialNotRounded + horizontalChange;
        }


        public static void ResizeVerticalFromBottom(this ComplexDsShape complexDsShape, double verticalChange)
        {
            complexDsShape.HeightInitial = complexDsShape.HeightInitialNotRounded + verticalChange;
        }


        public static Rect GetBoundingRectOfAllDsShapes(this ComplexDsShape complexDsShape)
        {
            return DsShapeBase.GetBoundingRect(complexDsShape.DsShapes);
        }


        public static void CropUnusedSpace(this ComplexDsShape complexDsShape)
        {
            if (complexDsShape.DsShapes.Length == 0) return;

            var widthInitialNotRounded = complexDsShape.WidthInitialNotRounded;
            var heightInitialNotRounded = complexDsShape.HeightInitialNotRounded;
            var rect = complexDsShape.GetBoundingRectOfAllDsShapes();
            complexDsShape.ResizeHorizontalFromRight(rect.Right - complexDsShape.WidthInitialNotRounded);
            complexDsShape.ResizeHorizontalFromLeft(rect.Left);
            complexDsShape.ResizeVerticalFromBottom(rect.Bottom - complexDsShape.HeightInitialNotRounded);
            complexDsShape.ResizeVerticalFromTop(rect.Top);

            if (!DsProjectExtensions.IsValidLength(complexDsShape.DsShapeDrawingWidth) ||
                !DsProjectExtensions.IsValidLength(complexDsShape.DsShapeDrawingHeight))
                return;

            var kX = complexDsShape.WidthInitialNotRounded / widthInitialNotRounded;
            var kY = complexDsShape.HeightInitialNotRounded / heightInitialNotRounded;
            complexDsShape.DsShapeDrawingWidth = kX * complexDsShape.DsShapeDrawingWidth;
            complexDsShape.DsShapeDrawingHeight = kY * complexDsShape.DsShapeDrawingHeight;
        }


        public static void PrepareComplexDsShapeGeometry(this ComplexDsShape complexDsShape,
            DsShapeDrawing dsShapeDrawing)
        {
            if (!DsProjectExtensions.IsValidLength(complexDsShape.DsShapeDrawingWidth) ||
                !DsProjectExtensions.IsValidLength(complexDsShape.DsShapeDrawingHeight) ||
                !DsProjectExtensions.IsValidCenterRelativePosition(complexDsShape
                    .DsShapeDrawingCenterRelativePosition))
                return;

            var centerRelativePosition = dsShapeDrawing.CenterRelativePosition;
            var originalCenterRelativePosition = complexDsShape.DsShapeDrawingCenterRelativePosition;
            var widthK = complexDsShape.WidthInitialNotRounded / complexDsShape.DsShapeDrawingWidth;
            var heightK = complexDsShape.HeightInitialNotRounded / complexDsShape.DsShapeDrawingHeight;
            var deltaLeft = widthK * (dsShapeDrawing.Width * centerRelativePosition.X -
                                      complexDsShape.DsShapeDrawingWidth *
                                      originalCenterRelativePosition.X);
            var deltaTop = heightK * (dsShapeDrawing.Height * centerRelativePosition.Y -
                                      complexDsShape.DsShapeDrawingHeight *
                                      originalCenterRelativePosition.Y);
            var deltaRight = widthK * (dsShapeDrawing.Width * (1 - centerRelativePosition.X) -
                                       complexDsShape.DsShapeDrawingWidth *
                                       (1 - originalCenterRelativePosition.X));
            var deltaBottom = heightK * (dsShapeDrawing.Height * (1 - centerRelativePosition.Y) -
                                         complexDsShape.DsShapeDrawingHeight *
                                         (1 - originalCenterRelativePosition.Y));
            var left = complexDsShape.LeftNotTransformed;
            var top = complexDsShape.TopNotTransformed;
            complexDsShape.WidthInitial = complexDsShape.WidthInitialNotRounded + deltaLeft + deltaRight;
            complexDsShape.HeightInitial = complexDsShape.HeightInitialNotRounded + deltaTop + deltaBottom;
            complexDsShape.LeftNotTransformed = left - deltaLeft;
            complexDsShape.TopNotTransformed = top - deltaTop;
        }

        #endregion

        #region private functions

        #endregion
    }
}