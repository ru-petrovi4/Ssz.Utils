using System.Windows.Data;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.DsShapes;

namespace Ssz.Operator.Core.DsShapeViews
{
    public class ContentDsShapeView : DsShapeViewBase
    {
        #region construction and destruction

        public ContentDsShapeView(ContentDsShape dsShape, Frame? frame)
            : base(dsShape, frame)
        {
            SnapsToDevicePixels = false;

            IsHitTestVisible = false;
        }

        #endregion

        #region protected functions

        protected override void OnDsShapeChanged(string? propertyName)
        {
            base.OnDsShapeChanged(propertyName);

            var dsShape = (ContentDsShape) DsShapeViewModel.DsShape;
            if (propertyName is null || propertyName == nameof(dsShape.ContentInfo))
                this.SetBindingOrConst(dsShape.Container, ContentProperty, dsShape.ContentInfo,
                    BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
        }

        #endregion
    }
}