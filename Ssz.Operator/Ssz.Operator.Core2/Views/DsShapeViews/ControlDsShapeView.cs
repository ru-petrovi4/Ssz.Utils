using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;
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
            if (VisualDesignMode) 
                IsHitTestVisible = false;

            control.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
            control.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch; 

            Content = control;
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

        public T Control => (T)Content!;

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
                            Control.SetBindingOrConst(dsShape.Container, ToolTip.TipProperty, dsShape.ToolTipTextInfo,
                                BindingMode.OneWay, UpdateSourceTrigger.Default);
                        }
                    }
                    else
                    {
                        LoadToolTip(dsShape, toolTipFileRelativePath);                        
                    }
                }

            //if (propertyName is null || propertyName == nameof(dsShape.ToolTipPlacement))
            //    ToolTipService.SetPlacement(Control, dsShape.ToolTipPlacement);

            if (propertyName is null || propertyName == nameof(dsShape.DsFont))
            {
                FontFamily? fontFamily;
                double fontSize;
                FontStyle? fontStyle;
                FontStretch? fontStretch;
                FontWeight? fontWeight;
                ConstantsHelper.ComputeFont(dsShape.Container, dsShape.DsFont,
                    out fontFamily, out fontSize, out fontStyle, out fontStretch, out fontWeight);

                Control.SetConst(dsShape.Container, Avalonia.Controls.Primitives.TemplatedControl.FontFamilyProperty, fontFamily);
                if (fontSize > 0.0)
                    Control.SetConst(dsShape.Container, Avalonia.Controls.Primitives.TemplatedControl.FontSizeProperty, fontSize);
                Control.SetConst(dsShape.Container, Avalonia.Controls.Primitives.TemplatedControl.FontStyleProperty, fontStyle);
                Control.SetConst(dsShape.Container, Avalonia.Controls.Primitives.TemplatedControl.FontStretchProperty, fontStretch);
                Control.SetConst(dsShape.Container, Avalonia.Controls.Primitives.TemplatedControl.FontWeightProperty, fontWeight);
            }            

            if (propertyName is null || propertyName == nameof(dsShape.BackgroundInfo))
                Control.SetBindingOrConst(dsShape.Container, Avalonia.Controls.Primitives.TemplatedControl.BackgroundProperty,
                    dsShape.BackgroundInfo,
                    BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
            if (propertyName is null || propertyName == nameof(dsShape.ForegroundInfo))
                Control.SetBindingOrConst(dsShape.Container, Avalonia.Controls.Primitives.TemplatedControl.ForegroundProperty,
                    dsShape.ForegroundInfo,
                    BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
            if (propertyName is null || propertyName == nameof(dsShape.BorderThickness))
                Control.SetConst(dsShape.Container, Avalonia.Controls.Primitives.TemplatedControl.BorderThicknessProperty,
                    dsShape.BorderThickness);
            if (propertyName is null || propertyName == nameof(dsShape.Padding))
                Control.SetConst(dsShape.Container, Avalonia.Controls.Primitives.TemplatedControl.PaddingProperty,
                    dsShape.Padding);
            if (propertyName is null || propertyName == nameof(dsShape.BorderBrushInfo))
                Control.SetBindingOrConst(dsShape.Container, Avalonia.Controls.Primitives.TemplatedControl.BorderBrushProperty,
                    dsShape.BorderBrushInfo,
                    BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);            
            if (propertyName is null || propertyName == nameof(dsShape.StyleInfo))
            {
                var propertyXamlString = dsShape.GetStyleXamlString(dsShape.Container);
                ControlTheme? theme = null;
                if (!string.IsNullOrWhiteSpace(propertyXamlString))
                    try
                    {
                        theme = XamlHelper.Load(propertyXamlString!) as ControlTheme;
                    }
                    catch (Exception)
                    {
                    }

                if (theme is not null)
                    Control.SetConst(dsShape.Container, ThemeProperty, theme);
            }
        }

        #endregion

        #region private functions

        private async void LoadToolTip(ControlDsShape dsShape, string toolTipFileRelativePath)
        {
            var toolTipDsPageDrawing = await DsProject.Instance.ReadDsPageInPlayAsync(
                                toolTipFileRelativePath,
                                dsShape.Container,
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
                Control.SetValue(ToolTip.TipProperty, _playDrawingCanvas);
                //Control.SetValue(ToolTip.ShowDurationProperty, int.MaxValue);                              
            }
        }

        #endregion

        #region private fields

        private PlayDrawingCanvas? _playDrawingCanvas;

        #endregion
    }
}