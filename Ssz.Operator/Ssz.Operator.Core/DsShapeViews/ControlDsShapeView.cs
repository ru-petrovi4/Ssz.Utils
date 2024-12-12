using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.DsShapes;

namespace Ssz.Operator.Core.DsShapeViews
{
    public abstract class ControlDsShapeView<T> : DsShapeViewBase
        where T : Control
    {
        #region construction and destruction

        protected ControlDsShapeView(T control, ControlDsShape dsShape, ControlsPlay.Frame? frame)
            : base(dsShape, frame)
        {
            if (VisualDesignMode) IsHitTestVisible = false;

            Content = control;
            Control = control;
        }


        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
                if (_playDrawingCanvas is not null)
                    _playDrawingCanvas.Dispose();

            _playDrawingCanvas = null;

            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        public T Control { get; }

        #endregion

        #region protected functions

        protected override void OnDsShapeChanged(string? propertyName)
        {
            base.OnDsShapeChanged(propertyName);

            var dsShape = (ControlDsShape) DsShapeViewModel.DsShape;
            if (propertyName is null || propertyName == nameof(dsShape.ToolTipFileRelativePath))
                if (!VisualDesignMode)
                {
                    var toolTipFileRelativePath = dsShape.ToolTipFileRelativePath;
                    if (string.IsNullOrWhiteSpace(toolTipFileRelativePath))
                    {
                        if (dsShape.ToolTipTextInfo.IsConst &&
                            string.IsNullOrWhiteSpace(dsShape.ToolTipTextInfo.ConstValue))
                        {
                            // Do nothing
                        }
                        else
                        {
                            Control.SetBindingOrConst(dsShape.Container, ToolTipProperty, dsShape.ToolTipTextInfo,
                                BindingMode.OneWay, UpdateSourceTrigger.Default);
                        }
                    }
                    else
                    {
                        var toolTipDsPageDrawing =
                            DsProject.Instance.ReadDsPageInPlay(
                                toolTipFileRelativePath, dsShape.Container,
                                Frame is not null ? Frame.PlayWindow : null);
                        if (toolTipDsPageDrawing is null)
                        {
                            if (DsProject.Instance.Review)
                                MessageBoxHelper.ShowError(Properties.Resources.ReadToolTipDrawingErrorMessage + @" " +
                                                           toolTipFileRelativePath);
                            return;
                        }

                        if (Frame is not null)
                        {
                            _playDrawingCanvas = new PlayDrawingCanvas(toolTipDsPageDrawing, Frame);
                            Control.ToolTip = _playDrawingCanvas;
                            ToolTipService.SetShowDuration(Control, int.MaxValue);                            
                        }
                    }
                }

            if (propertyName is null || propertyName == nameof(dsShape.ToolTipPlacement))
                ToolTipService.SetPlacement(Control, dsShape.ToolTipPlacement);

            if (propertyName is null || propertyName == nameof(dsShape.DsFont))
            {
                FontFamily? fontFamily;
                double fontSize;
                FontStyle? fontStyle;
                FontStretch? fontStretch;
                FontWeight? fontWeight;
                ConstantsHelper.ComputeFont(dsShape.Container, dsShape.DsFont,
                    out fontFamily, out fontSize, out fontStyle, out fontStretch, out fontWeight);

                Control.SetConst(dsShape.Container, System.Windows.Controls.Control.FontFamilyProperty, fontFamily);
                if (fontSize > 0.0)
                    Control.SetConst(dsShape.Container, System.Windows.Controls.Control.FontSizeProperty, fontSize);
                Control.SetConst(dsShape.Container, System.Windows.Controls.Control.FontStyleProperty, fontStyle);
                Control.SetConst(dsShape.Container, System.Windows.Controls.Control.FontStretchProperty, fontStretch);
                Control.SetConst(dsShape.Container, System.Windows.Controls.Control.FontWeightProperty, fontWeight);
            }            

            if (propertyName is null || propertyName == nameof(dsShape.BackgroundInfo))
                Control.SetBindingOrConst(dsShape.Container, System.Windows.Controls.Control.BackgroundProperty,
                    dsShape.BackgroundInfo,
                    BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
            if (propertyName is null || propertyName == nameof(dsShape.ForegroundInfo))
                Control.SetBindingOrConst(dsShape.Container, System.Windows.Controls.Control.ForegroundProperty,
                    dsShape.ForegroundInfo,
                    BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
            if (propertyName is null || propertyName == nameof(dsShape.BorderThickness))
                Control.SetConst(dsShape.Container, System.Windows.Controls.Control.BorderThicknessProperty,
                    dsShape.BorderThickness);
            if (propertyName is null || propertyName == nameof(dsShape.Padding))
                Control.SetConst(dsShape.Container, System.Windows.Controls.Control.PaddingProperty,
                    dsShape.Padding);
            if (propertyName is null || propertyName == nameof(dsShape.BorderBrushInfo))
                Control.SetBindingOrConst(dsShape.Container, System.Windows.Controls.Control.BorderBrushProperty,
                    dsShape.BorderBrushInfo,
                    BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
            if (propertyName is null || propertyName == nameof(dsShape.HorizontalContentAlignment))
                Control.SetConst(dsShape.Container,
                    System.Windows.Controls.Control.HorizontalContentAlignmentProperty,
                    dsShape.HorizontalContentAlignment);
            if (propertyName is null || propertyName == nameof(dsShape.VerticalContentAlignment))
                Control.SetConst(dsShape.Container, System.Windows.Controls.Control.VerticalContentAlignmentProperty,
                    dsShape.VerticalContentAlignment);
            if (propertyName is null || propertyName == nameof(dsShape.StyleInfo))
            {
                var propertyXamlString = dsShape.GetStyleXamlString(dsShape.Container);
                Style? style = null;
                if (!string.IsNullOrWhiteSpace(propertyXamlString))
                    try
                    {
                        style = XamlHelper.Load(propertyXamlString!) as Style;
                    }
                    catch (Exception)
                    {
                    }

                Control.SetConst(dsShape.Container, StyleProperty, style);
            }
        }

        #endregion        

        #region private functions

        private PlayDrawingCanvas? _playDrawingCanvas;

        #endregion
    }
}