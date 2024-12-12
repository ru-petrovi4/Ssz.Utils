using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Ssz.Operator.Core.ControlsDesign;
using Ssz.Operator.Core.ControlsPlay;


using Ssz.Operator.Core.DsShapes;

namespace Ssz.Operator.Core.DsShapeViews
{
    public class GeometryButtonDsShapeView : ContentButtonDsShapeView, IGeometryDsShapeView
    {
        #region private fields

        private bool _disablePathUpdate;

        #endregion

        #region construction and destruction

        public GeometryButtonDsShapeView(GeometryButtonDsShape dsShape, Frame? frame)
            : base(dsShape, frame)
        {
            SnapsToDevicePixels = false;

            if (VisualDesignMode && DsShapeViewModel.DsShape.GetParentComplexDsShape() is null)
                DsShapeViewModel.PropertyChanged += DsShapeViewModelOnPropertyChanged;
        }

        #endregion

        #region private functions

        private void PathUpdate()
        {
            if (_disablePathUpdate) return;

            var dsShape = (GeometryButtonDsShape) DsShapeViewModel.DsShape;

            var geometry = GeometryHelper.GetGeometry(dsShape.GeometryInfo, new GeometryInfoSupplier(),
                dsShape.Container,
                DsShapeViewModel.GeometryEditingMode,
                dsShape.WidthInitialNotRounded, dsShape.HeightInitialNotRounded, 2);
            Control.Clip = geometry;
            if (GeometryChanged is not null) GeometryChanged();
        }

        #endregion

        #region public functions

        public Geometry Geometry => Control.Clip;

        public event Action? GeometryChanged;

        public void UpdateModelLayer()
        {
            var dsShape = (GeometryButtonDsShape) DsShapeViewModel.DsShape;

            var pathGeometry = Control.Clip as PathGeometry;
            if (pathGeometry is not null)
            {
                var centerInitialPosition = dsShape.CenterInitialPositionNotRounded;
                var centerRelativePosition = dsShape.CenterRelativePosition;
                var tg = dsShape.GetTransformGroup();
                var c0 = new Point(
                    dsShape.WidthInitialNotRounded * centerRelativePosition.X,
                    dsShape.HeightInitialNotRounded * centerRelativePosition.Y
                );
                /*
                var bounds = pathGeometry.GetRenderBounds(
                        new Pen(dsShape.StrokeInfo.ConstValue.GetBrush(dsShape.Container), dsShape.StrokeThickness));*/
                var bounds = pathGeometry.Bounds;
                if (double.IsNaN(bounds.X) || double.IsInfinity(bounds.X) ||
                    double.IsNaN(bounds.Y) || double.IsInfinity(bounds.Y) ||
                    double.IsNaN(bounds.Width) || double.IsInfinity(bounds.Width) ||
                    double.IsNaN(bounds.Height) || double.IsInfinity(bounds.Height)) return;
                var c1 = new Point(
                    bounds.X + bounds.Width * centerRelativePosition.X,
                    bounds.Y + bounds.Height * centerRelativePosition.Y
                );

                _disablePathUpdate = true;

                dsShape.WidthInitial = (bounds.Width > 0 ? bounds.Width : DsShapeBase.MinWidth) + 2;
                dsShape.HeightInitial = (bounds.Height > 0 ? bounds.Height : DsShapeBase.MinHeight) + 2;
                var d = tg.Transform(new Point(c1.X - c0.X, c1.Y - c0.Y));
                dsShape.CenterInitialPosition = new Point(
                    centerInitialPosition.X + d.X,
                    centerInitialPosition.Y + d.Y);

                pathGeometry.DoForAllPoints((ref Point p) =>
                {
                    p.X = p.X - bounds.X;
                    p.Y = p.Y - bounds.Y;
                });

                dsShape.GeometryInfo = new DsUIElementProperty
                {
                    TypeString = DsUIElementPropertySupplier.CustomTypeString,
                    CustomXamlString = pathGeometry.ToString(CultureInfo.InvariantCulture)
                };

                _disablePathUpdate = false;

                PathUpdate();
            }
        }

        #endregion

        #region protected functions

        protected virtual void DsShapeViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case @"GeometryEditingMode":
                case @"WidthInitial":
                case @"HeightInitial":
                    PathUpdate();
                    break;
            }
        }


        protected override void OnDsShapeChanged(string? propertyName)
        {
            base.OnDsShapeChanged(propertyName);

            var dsShape = (GeometryButtonDsShape) DsShapeViewModel.DsShape;
            if (propertyName is null || propertyName == nameof(dsShape.GeometryInfo)) PathUpdate();
        }

        #endregion
    }
}