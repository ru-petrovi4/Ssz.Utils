using Avalonia.Media;
using Avalonia.Controls.Shapes;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.DsShapes;

namespace Ssz.Operator.Core.DsShapeViews
{
    public class ComplexDsShapeCenterPointDsShapeView : DsShapeViewBase
    {
        #region construction and destruction

        public ComplexDsShapeCenterPointDsShapeView(ComplexDsShapeCenterPointDsShape dsShape,
            Frame? frame)
            : base(dsShape, frame)
        {
            //SnapsToDevicePixels = false;

            IsHitTestVisible = false;

            if (VisualDesignMode)
                Content = new Ellipse
                    {Fill = Brushes.Green, Width = 4, Height = 4, Stroke = Brushes.White, StrokeThickness = 1};
        }

        #endregion
    }
}