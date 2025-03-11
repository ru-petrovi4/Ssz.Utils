using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Controls.Shapes;
using Ssz.Operator.Core.ControlsDesign;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.Utils;
using Avalonia.Data;
using Avalonia.Collections;

namespace Ssz.Operator.Core.DsShapeViews
{
    public class GeometryDsShapeView : DsShapeViewBase, IGeometryDsShapeView
    {
        #region construction and destruction

        public GeometryDsShapeView(GeometryDsShape dsShape, ControlsPlay.Frame? frame)
            : base(dsShape, frame)
        {
            //SnapsToDevicePixels = false;

            IsHitTestVisible = false;

            if (VisualDesignMode && DsShapeViewModel.DsShape.GetParentComplexDsShape() is null)
                DsShapeViewModel.PropertyChanged += DsShapeViewModelOnPropertyChanged;

            ClipToBounds = false; // For Path correct displaying.

            _path = new Path();
            _path.Stretch = Stretch.Fill;
            _path.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
            _path.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
            Content = _path;
        }

        #endregion        

        #region public functions

        public Geometry? Geometry => _path.Data;

        public event Action? GeometryChanged;

        public void UpdateModelLayer()
        {
            //var dsShape = (GeometryDsShape)DsShapeViewModel.DsShape;

            //var pathGeometry = _path.Data as PathGeometry;
            //if (pathGeometry is not null)
            //{
            //    var centerInitialPosition = dsShape.CenterInitialPositionNotRounded;
            //    var centerRelativePosition = dsShape.CenterRelativePosition;
            //    var tg = dsShape.GetTransformGroup();
            //    var c0 = new Point(
            //        dsShape.WidthInitialNotRounded * centerRelativePosition.X,
            //        dsShape.HeightInitialNotRounded * centerRelativePosition.Y
            //    );
            //    /*
            //    var bounds = pathGeometry.GetRenderBounds(
            //            new Pen(dsShape.StrokeInfo.ConstValue.GetBrush(dsShape.Container), dsShape.StrokeThickness));*/
            //    var bounds = pathGeometry.Bounds;
            //    if (double.IsNaN(bounds.X) || double.IsInfinity(bounds.X) ||
            //        double.IsNaN(bounds.Y) || double.IsInfinity(bounds.Y) ||
            //        double.IsNaN(bounds.Width) || double.IsInfinity(bounds.Width) ||
            //        double.IsNaN(bounds.Height) || double.IsInfinity(bounds.Height)) return;
            //    var c1 = new Point(
            //        bounds.X + bounds.Width * centerRelativePosition.X,
            //        bounds.Y + bounds.Height * centerRelativePosition.Y
            //    );

            //    _disablePathUpdate = true;

            //    dsShape.WidthInitial = (bounds.Width > 0 ? bounds.Width : DsShapeBase.MinWidth) +
            //                             dsShape.StrokeThickness;
            //    dsShape.HeightInitial = (bounds.Height > 0 ? bounds.Height : DsShapeBase.MinHeight) +
            //                              dsShape.StrokeThickness;
            //    var d = tg.Transform(new Point(c1.X - c0.X, c1.Y - c0.Y));
            //    dsShape.CenterInitialPosition = new Point(
            //        centerInitialPosition.X + d.X,
            //        centerInitialPosition.Y + d.Y);

            //    pathGeometry.DoForAllPoints((ref Point p) =>
            //    {
            //        p.X = p.X - bounds.X;
            //        p.Y = p.Y - bounds.Y;
            //    });

            //    dsShape.GeometryInfo = new DsUIElementProperty
            //    {
            //        TypeString = DsUIElementPropertySupplier.CustomTypeString,
            //        CustomXamlString = pathGeometry.ToString(CultureInfo.InvariantCulture)
            //    };

            //    _disablePathUpdate = false;

            //    PathUpdate();
            //}
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

            bool pathUpdate = false;

            var dsShape = (GeometryDsShape) DsShapeViewModel.DsShape;
            if (propertyName is null || propertyName == nameof(dsShape.GeometryInfo))
                pathUpdate = true;
            if (propertyName is null || propertyName == nameof(dsShape.FillInfo))
                _path.SetBindingOrConst(dsShape.Container, Path.FillProperty, dsShape.FillInfo,
                    BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
            if (propertyName is null || propertyName == nameof(dsShape.StrokeInfo))
                _path.SetBindingOrConst(dsShape.Container, Path.StrokeProperty, dsShape.StrokeInfo,
                    BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
            if (propertyName is null || propertyName == nameof(dsShape.StrokeThickness))
            {
                _path.SetConst(dsShape.Container, Path.StrokeThicknessProperty, dsShape.StrokeThickness);
                pathUpdate = true;
            }

            if (propertyName is null || propertyName == nameof(dsShape.StrokeDashLength) ||
                propertyName == nameof(dsShape.StrokeGapLength) ||
                propertyName == nameof(dsShape.StrokeDashArray))
            {
                if (dsShape.StrokeDashLength.HasValue && dsShape.StrokeGapLength.HasValue)
                {
                    _path.SetConst(dsShape.Container, Shape.StrokeDashArrayProperty,
                        new AvaloniaList<double>(
                            dsShape.StrokeDashLength.Value,
                            dsShape.StrokeGapLength.Value
                        ));
                }
                else
                {
                    if (!string.IsNullOrEmpty(dsShape.StrokeDashArray))
                    {
                        var a = dsShape.StrokeDashArray.Split(',');
                        if (a.Length > 1)
                            _path.SetConst(dsShape.Container, Shape.StrokeDashArrayProperty,
                                new AvaloniaList<double>(a.Select(s => ObsoleteAnyHelper.ConvertTo<double>(s, false))));
                        else
                            _path.SetConst(dsShape.Container, Shape.StrokeDashArrayProperty, null);
                    }
                    else
                    {
                        _path.SetConst(dsShape.Container, Shape.StrokeDashArrayProperty, null);
                    }
                }
            }

            if (propertyName is null || propertyName == nameof(dsShape.StrokeLineJoin))
                _path.SetConst(dsShape.Container, Shape.StrokeJoinProperty, dsShape.StrokeLineJoin);
            if (propertyName is null || propertyName == nameof(dsShape.StrokeStartLineCap))
                _path.SetConst(dsShape.Container, Shape.StrokeLineCapProperty, dsShape.StrokeStartLineCap);    
            
            if (pathUpdate)
                PathUpdate();
        }

        #endregion

        #region private functions

        private void PathUpdate()
        {
            if (_disablePathUpdate) return;

            var dsShape = (GeometryDsShape)DsShapeViewModel.DsShape;

            var geometry = GeometryHelper.GetGeometry(dsShape.GeometryInfo, new GeometryInfoSupplier(),
                dsShape.Container,
                DsShapeViewModel.GeometryEditingMode,
                dsShape.WidthInitialNotRounded, dsShape.HeightInitialNotRounded, dsShape.StrokeThickness);
            _path.Data = geometry;

            if (GeometryChanged is not null) 
                GeometryChanged();
        }

        #endregion

        #region private fields

        private bool _disablePathUpdate;

        private readonly Path _path;

        #endregion
    }
}