using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.ControlsCommon;
using Ssz.Operator.Core.ControlsPlay;

using Ssz.Operator.Core.DsShapes;

namespace Ssz.Operator.Core.DsShapeViews
{
    public class MultiChartDsShapeView : DsShapeViewBase
    {
        #region private fields

        private bool _refreshing;

        #endregion

        #region protected functions

        protected override void OnDsShapeChanged(string? propertyName)
        {
            base.OnDsShapeChanged(propertyName);

            var dsShape = (MultiChartDsShape) DsShapeViewModel.DsShape;
            if (propertyName is null || propertyName == nameof(dsShape.AxisXTopIsVisible) ||
                propertyName == nameof(dsShape.AxisXBottomIsVisible) ||
                propertyName == nameof(dsShape.AxisYLeftIsVisible) ||
                propertyName == nameof(dsShape.AxisYRightIsVisible))
                RefreshMainGrid();
            if (propertyName is null || propertyName == nameof(dsShape.Type) ||
                propertyName == nameof(dsShape.ChartGridIsVisible) ||
                propertyName == nameof(dsShape.MultiDsChartItemsCollection))
                RefreshChartBorderChildGridAsync();
            if (propertyName is null ||
                propertyName == nameof(dsShape.FormatXInfo) ||
                propertyName == nameof(dsShape.EngUnitXInfo) ||
                propertyName == nameof(dsShape.TickFrequencyXInfo) ||
                propertyName == nameof(dsShape.TextTickFrequencyXInfo) ||
                propertyName == nameof(dsShape.TickStrokeThickness) ||
                propertyName == nameof(dsShape.TickLength))
            {
                AxisXTopControl.SetBindingOrConst(dsShape.Container, TextTickBar.FormatProperty,
                    dsShape.FormatXInfo, BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
                AxisXTopControl.SetBindingOrConst(dsShape.Container, TextTickBar.EngUnitProperty,
                    dsShape.EngUnitXInfo, BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
                AxisXTopControl.SetBindingOrConst(dsShape.Container, TickBar.TickFrequencyProperty,
                    dsShape.TickFrequencyXInfo, BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
                AxisXTopControl.SetBindingOrConst(dsShape.Container, TextTickBar.TextTickFrequencyProperty,
                    dsShape.TextTickFrequencyXInfo, BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
                AxisXTopControl.TickStrokeThickness = dsShape.TickStrokeThickness;
                AxisXTopControl.TickLength = dsShape.TickLength;

                AxisXBottomControl.SetBindingOrConst(dsShape.Container, TextTickBar.FormatProperty,
                    dsShape.FormatXInfo, BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
                AxisXBottomControl.SetBindingOrConst(dsShape.Container, TextTickBar.EngUnitProperty,
                    dsShape.EngUnitXInfo, BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
                AxisXBottomControl.SetBindingOrConst(dsShape.Container, TickBar.TickFrequencyProperty,
                    dsShape.TickFrequencyXInfo, BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
                AxisXBottomControl.SetBindingOrConst(dsShape.Container, TextTickBar.TextTickFrequencyProperty,
                    dsShape.TextTickFrequencyXInfo, BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
                AxisXBottomControl.TickStrokeThickness = dsShape.TickStrokeThickness;
                AxisXBottomControl.TickLength = dsShape.TickLength;

                VerticalChartTickBar.SetBindingOrConst(dsShape.Container, ChartTickBar.FormatProperty,
                    dsShape.FormatXInfo, BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
                VerticalChartTickBar.SetBindingOrConst(dsShape.Container, ChartTickBar.EngUnitProperty,
                    dsShape.EngUnitXInfo, BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
                VerticalChartTickBar.SetBindingOrConst(dsShape.Container, TickBar.TickFrequencyProperty,
                    dsShape.TextTickFrequencyXInfo, BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
                VerticalChartTickBar.TickStrokeThickness = dsShape.TickStrokeThickness;
            }

            if (propertyName is null ||
                propertyName == nameof(dsShape.FormatYInfo) ||
                propertyName == nameof(dsShape.EngUnitYInfo) ||
                propertyName == nameof(dsShape.TickFrequencyYInfo) ||
                propertyName == nameof(dsShape.TextTickFrequencyYInfo) ||
                propertyName == nameof(dsShape.TickStrokeThickness) ||
                propertyName == nameof(dsShape.TickLength))
            {
                AxisYLeftControl.SetBindingOrConst(dsShape.Container, TextTickBar.FormatProperty,
                    dsShape.FormatYInfo, BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
                AxisYLeftControl.SetBindingOrConst(dsShape.Container, TextTickBar.EngUnitProperty,
                    dsShape.EngUnitYInfo, BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
                AxisYLeftControl.SetBindingOrConst(dsShape.Container, TickBar.TickFrequencyProperty,
                    dsShape.TickFrequencyYInfo, BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
                AxisYLeftControl.SetBindingOrConst(dsShape.Container, TextTickBar.TextTickFrequencyProperty,
                    dsShape.TextTickFrequencyYInfo, BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
                AxisYLeftControl.TickStrokeThickness = dsShape.TickStrokeThickness;
                AxisYLeftControl.TickLength = dsShape.TickLength;

                AxisYRightControl.SetBindingOrConst(dsShape.Container, TextTickBar.FormatProperty,
                    dsShape.FormatYInfo, BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
                AxisYRightControl.SetBindingOrConst(dsShape.Container, TextTickBar.EngUnitProperty,
                    dsShape.EngUnitYInfo, BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
                AxisYRightControl.SetBindingOrConst(dsShape.Container, TickBar.TickFrequencyProperty,
                    dsShape.TickFrequencyYInfo, BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
                AxisYRightControl.SetBindingOrConst(dsShape.Container, TextTickBar.TextTickFrequencyProperty,
                    dsShape.TextTickFrequencyYInfo, BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
                AxisYRightControl.TickStrokeThickness = dsShape.TickStrokeThickness;
                AxisYRightControl.TickLength = dsShape.TickLength;

                HorizontalChartTickBar.SetBindingOrConst(dsShape.Container, ChartTickBar.FormatProperty,
                    dsShape.FormatYInfo, BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
                HorizontalChartTickBar.SetBindingOrConst(dsShape.Container, ChartTickBar.EngUnitProperty,
                    dsShape.EngUnitYInfo, BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
                HorizontalChartTickBar.SetBindingOrConst(dsShape.Container, TickBar.TickFrequencyProperty,
                    dsShape.TextTickFrequencyYInfo, BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
                HorizontalChartTickBar.TickStrokeThickness = dsShape.TickStrokeThickness;
            }

            if (propertyName is null || propertyName == nameof(dsShape.MaximumXInfo))
            {
                AxisXTopControl.SetBindingOrConst(dsShape.Container, TickBar.MaximumProperty, dsShape.MaximumXInfo,
                    BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
                AxisXBottomControl.SetBindingOrConst(dsShape.Container, TickBar.MaximumProperty,
                    dsShape.MaximumXInfo, BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
                VerticalChartTickBar.SetBindingOrConst(dsShape.Container, TickBar.MaximumProperty,
                    dsShape.MaximumXInfo, BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
            }

            if (propertyName is null || propertyName == nameof(dsShape.MinimumXInfo))
            {
                AxisXTopControl.SetBindingOrConst(dsShape.Container, TickBar.MinimumProperty, dsShape.MinimumXInfo,
                    BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
                AxisXBottomControl.SetBindingOrConst(dsShape.Container, TickBar.MinimumProperty,
                    dsShape.MinimumXInfo, BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
                VerticalChartTickBar.SetBindingOrConst(dsShape.Container, TickBar.MinimumProperty,
                    dsShape.MinimumXInfo, BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
            }

            if (propertyName is null || propertyName == nameof(dsShape.MaximumYInfo))
            {
                AxisYLeftControl.SetBindingOrConst(dsShape.Container, TickBar.MaximumProperty, dsShape.MaximumYInfo,
                    BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
                AxisYRightControl.SetBindingOrConst(dsShape.Container, TickBar.MaximumProperty,
                    dsShape.MaximumYInfo, BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
                HorizontalChartTickBar.SetBindingOrConst(dsShape.Container, TickBar.MaximumProperty,
                    dsShape.MaximumYInfo, BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
            }

            if (propertyName is null || propertyName == nameof(dsShape.MinimumYInfo))
            {
                AxisYLeftControl.SetBindingOrConst(dsShape.Container, TickBar.MinimumProperty, dsShape.MinimumYInfo,
                    BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
                AxisYRightControl.SetBindingOrConst(dsShape.Container, TickBar.MinimumProperty,
                    dsShape.MinimumYInfo, BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
                HorizontalChartTickBar.SetBindingOrConst(dsShape.Container, TickBar.MinimumProperty,
                    dsShape.MinimumYInfo, BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
            }

            if (propertyName is null || propertyName == nameof(dsShape.DsFont))
            {
                FontFamily? fontFamily;
                double fontSize;
                FontStyle? fontStyle;
                FontStretch? fontStretch;
                FontWeight? fontWeight;
                ConstantsHelper.ComputeFont(dsShape.Container, dsShape.DsFont,
                    out fontFamily, out fontSize, out fontStyle, out fontStretch, out fontWeight);

                AxisXTopControl.SetConst(dsShape.Container, TextTickBar.FontFamilyProperty, fontFamily);
                if (fontSize > 0.0)
                    AxisXTopControl.SetConst(dsShape.Container, TextTickBar.FontSizeProperty, fontSize);
                AxisXTopControl.SetConst(dsShape.Container, TextTickBar.FontStyleProperty, fontStyle);
                AxisXTopControl.SetConst(dsShape.Container, TextTickBar.FontStretchProperty, fontStretch);
                AxisXTopControl.SetConst(dsShape.Container, TextTickBar.FontWeightProperty, fontWeight);

                AxisXBottomControl.SetConst(dsShape.Container, TextTickBar.FontFamilyProperty, fontFamily);
                if (fontSize > 0.0)
                    AxisXBottomControl.SetConst(dsShape.Container, TextTickBar.FontSizeProperty, fontSize);
                AxisXBottomControl.SetConst(dsShape.Container, TextTickBar.FontStyleProperty, fontStyle);
                AxisXBottomControl.SetConst(dsShape.Container, TextTickBar.FontStretchProperty, fontStretch);
                AxisXBottomControl.SetConst(dsShape.Container, TextTickBar.FontWeightProperty, fontWeight);

                AxisYLeftControl.SetConst(dsShape.Container, TextTickBar.FontFamilyProperty, fontFamily);
                if (fontSize > 0.0)
                    AxisYLeftControl.SetConst(dsShape.Container, TextTickBar.FontSizeProperty, fontSize);
                AxisYLeftControl.SetConst(dsShape.Container, TextTickBar.FontStyleProperty, fontStyle);
                AxisYLeftControl.SetConst(dsShape.Container, TextTickBar.FontStretchProperty, fontStretch);
                AxisYLeftControl.SetConst(dsShape.Container, TextTickBar.FontWeightProperty, fontWeight);

                AxisYRightControl.SetConst(dsShape.Container, TextTickBar.FontFamilyProperty, fontFamily);
                if (fontSize > 0.0)
                    AxisYRightControl.SetConst(dsShape.Container, TextTickBar.FontSizeProperty, fontSize);
                AxisYRightControl.SetConst(dsShape.Container, TextTickBar.FontStyleProperty, fontStyle);
                AxisYRightControl.SetConst(dsShape.Container, TextTickBar.FontStretchProperty, fontStretch);
                AxisYRightControl.SetConst(dsShape.Container, TextTickBar.FontWeightProperty, fontWeight);

                HorizontalChartTickBar.SetConst(dsShape.Container, ChartTickBar.FontFamilyProperty, fontFamily);
                if (fontSize > 0.0)
                    HorizontalChartTickBar.SetConst(dsShape.Container, ChartTickBar.FontSizeProperty, fontSize);
                HorizontalChartTickBar.SetConst(dsShape.Container, ChartTickBar.FontStyleProperty, fontStyle);
                HorizontalChartTickBar.SetConst(dsShape.Container, ChartTickBar.FontStretchProperty, fontStretch);
                HorizontalChartTickBar.SetConst(dsShape.Container, ChartTickBar.FontWeightProperty, fontWeight);

                VerticalChartTickBar.SetConst(dsShape.Container, ChartTickBar.FontFamilyProperty, fontFamily);
                if (fontSize > 0.0)
                    VerticalChartTickBar.SetConst(dsShape.Container, ChartTickBar.FontSizeProperty, fontSize);
                VerticalChartTickBar.SetConst(dsShape.Container, ChartTickBar.FontStyleProperty, fontStyle);
                VerticalChartTickBar.SetConst(dsShape.Container, ChartTickBar.FontStretchProperty, fontStretch);
                VerticalChartTickBar.SetConst(dsShape.Container, ChartTickBar.FontWeightProperty, fontWeight);
            }

            if (propertyName is null || propertyName == nameof(dsShape.BackgroundInfo))
                ChartBorder.SetBindingOrConst(dsShape.Container, Border.BackgroundProperty, dsShape.BackgroundInfo,
                    BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
            if (propertyName is null || propertyName == nameof(dsShape.BorderThickness) ||
                propertyName == nameof(dsShape.Padding))
            {
                var borderThickness = dsShape.BorderThickness;
                var padding = dsShape.Padding;
                ChartBorder.BorderThickness = borderThickness;
                ChartBorder.Padding = padding;
                AxisXTopControl.Margin = AxisXBottomControl.Margin = new Thickness(borderThickness.Left + padding.Left,
                    0, borderThickness.Right + padding.Right, 0);
                AxisYLeftControl.Margin = AxisYRightControl.Margin = new Thickness(0, borderThickness.Top + padding.Top,
                    0, borderThickness.Bottom + padding.Bottom);
            }

            if (propertyName is null || propertyName == nameof(dsShape.ForegroundInfo))
            {
                ChartBorder.SetBindingOrConst(dsShape.Container, Border.BorderBrushProperty, dsShape.ForegroundInfo,
                    BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
                AxisXTopControl.SetBindingOrConst(dsShape.Container, TickBar.FillProperty, dsShape.ForegroundInfo,
                    BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
                AxisXBottomControl.SetBindingOrConst(dsShape.Container, TickBar.FillProperty,
                    dsShape.ForegroundInfo, BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
                AxisYLeftControl.SetBindingOrConst(dsShape.Container, TickBar.FillProperty, dsShape.ForegroundInfo,
                    BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
                AxisYRightControl.SetBindingOrConst(dsShape.Container, TickBar.FillProperty, dsShape.ForegroundInfo,
                    BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
                HorizontalChartTickBar.SetBindingOrConst(dsShape.Container, ChartTickBar.LabelsFillProperty,
                    dsShape.ForegroundInfo, BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
                VerticalChartTickBar.SetBindingOrConst(dsShape.Container, ChartTickBar.LabelsFillProperty,
                    dsShape.ForegroundInfo, BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
            }

            if (propertyName is null || propertyName == nameof(dsShape.ChartGridLabelsLeftIsVisible))
                HorizontalChartTickBar.LabelsBeginIsVisible = dsShape.ChartGridLabelsLeftIsVisible;
            if (propertyName is null || propertyName == nameof(dsShape.ChartGridLabelsTopIsVisible))
                VerticalChartTickBar.LabelsBeginIsVisible = dsShape.ChartGridLabelsTopIsVisible;
            if (propertyName is null || propertyName == nameof(dsShape.ChartGridLabelsRightIsVisible))
                HorizontalChartTickBar.LabelsEndIsVisible = dsShape.ChartGridLabelsRightIsVisible;
            if (propertyName is null || propertyName == nameof(dsShape.ChartGridLabelsBottomIsVisible))
                VerticalChartTickBar.LabelsEndIsVisible = dsShape.ChartGridLabelsBottomIsVisible;
            if (propertyName is null || propertyName == nameof(dsShape.ChartGridBrush))
                HorizontalChartTickBar.Fill =
                    VerticalChartTickBar.Fill = dsShape.ChartGridBrush.GetBrush(dsShape.Container);
        }

        #endregion

        #region construction and destruction

        public MultiChartDsShapeView(MultiChartDsShape dsShape, ControlsPlay.Frame? frame)
            : base(dsShape, frame)
        {
            MainGrid = new Grid();
            MainGrid.ColumnDefinitions.Add(new ColumnDefinition {Width = new GridLength(1.0, GridUnitType.Auto)});
            MainGrid.ColumnDefinitions.Add(new ColumnDefinition {Width = new GridLength(1.0, GridUnitType.Star)});
            MainGrid.ColumnDefinitions.Add(new ColumnDefinition {Width = new GridLength(1.0, GridUnitType.Auto)});
            MainGrid.RowDefinitions.Add(new RowDefinition {Height = new GridLength(1.0, GridUnitType.Auto)});
            MainGrid.RowDefinitions.Add(new RowDefinition {Height = new GridLength(1.0, GridUnitType.Star)});
            MainGrid.RowDefinitions.Add(new RowDefinition {Height = new GridLength(1.0, GridUnitType.Auto)});
            ChartBorder = new Border();
            ChartBorderChildGrid = new Grid();
            HorizontalChartTickBar = new ChartTickBar {Placement = TickBarPlacement.Left};
            VerticalChartTickBar = new ChartTickBar {Placement = TickBarPlacement.Top};
            AxisXTopControl = new TextTickBar {Placement = TickBarPlacement.Top};
            AxisXBottomControl = new TextTickBar {Placement = TickBarPlacement.Bottom};
            AxisYLeftControl = new TextTickBar {Placement = TickBarPlacement.Left};
            AxisYRightControl = new TextTickBar {Placement = TickBarPlacement.Right};

            if (VisualDesignMode) IsHitTestVisible = false;

            SnapsToDevicePixels = false;
        }

        private void HorizontalChartTickBar_SizeChanged(object? sender, SizeChangedEventArgs e)
        {
            throw new NotImplementedException();
        }


        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
            {
                foreach (IDisposable disposable in ChartBorderChildGrid.Children.OfType<IDisposable>())
                    disposable.Dispose();
                ChartBorderChildGrid.Children.Clear();
            }

            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        public Grid MainGrid { get; }

        public Border ChartBorder { get; }

        public Grid ChartBorderChildGrid { get; }

        public ChartTickBar HorizontalChartTickBar { get; }

        public ChartTickBar VerticalChartTickBar { get; }

        public TextTickBar AxisXTopControl { get; }

        public TextTickBar AxisXBottomControl { get; }

        public TextTickBar AxisYLeftControl { get; }

        public TextTickBar AxisYRightControl { get; }

        #endregion

        #region private functions

        private void RefreshMainGrid()
        {
            MainGrid.Children.Clear();

            var dsShape = (MultiChartDsShape) DsShapeViewModel.DsShape;

            if (dsShape.AxisXTopIsVisible)
            {
                MainGrid.Children.Add(AxisXTopControl);
                Grid.SetColumn(AxisXTopControl, 1);
                Grid.SetRow(AxisXTopControl, 0);
            }

            if (dsShape.AxisXBottomIsVisible)
            {
                MainGrid.Children.Add(AxisXBottomControl);
                Grid.SetColumn(AxisXBottomControl, 1);
                Grid.SetRow(AxisXBottomControl, 2);
            }

            if (dsShape.AxisYLeftIsVisible)
            {
                MainGrid.Children.Add(AxisYLeftControl);
                Grid.SetColumn(AxisYLeftControl, 0);
                Grid.SetRow(AxisYLeftControl, 1);
            }

            if (dsShape.AxisYRightIsVisible)
            {
                MainGrid.Children.Add(AxisYRightControl);
                Grid.SetColumn(AxisYRightControl, 2);
                Grid.SetRow(AxisYRightControl, 1);
            }

            MainGrid.Children.Add(ChartBorder);
            Grid.SetColumn(ChartBorder, 1);
            Grid.SetColumnSpan(ChartBorder, 1);
            Grid.SetRow(ChartBorder, 1);
            Grid.SetRowSpan(ChartBorder, 1);

            Content = MainGrid;
        }

        private async void RefreshChartBorderChildGridAsync()
        {
            if (_refreshing) return;
            _refreshing = true;

            await Task.Delay(100); // Wait for other calls to RefreshAsync(), and then execute refreshing only once

            _refreshing = false;

            var dsShape = (MultiChartDsShape) DsShapeViewModel.DsShape;

            foreach (IDisposable disposable in ChartBorderChildGrid.Children.OfType<IDisposable>())
                disposable.Dispose();
            ChartBorderChildGrid.Children.Clear();

            foreach (MultiDsChartItem multiDsChartItem in dsShape.MultiDsChartItemsCollection)
            {
                var chartCanvas = new ChartCanvas(this, multiDsChartItem, VisualDesignMode);

                chartCanvas.SetBindingOrConst(dsShape.Container, ChartCanvas.PointsCountProperty,
                    multiDsChartItem.PointsCountInfo,
                    BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);

                ChartBorderChildGrid.Children.Add(chartCanvas);
            }

            if (dsShape.ChartGridIsVisible)
            {
                ChartBorderChildGrid.Children.Add(HorizontalChartTickBar);
                ChartBorderChildGrid.Children.Add(VerticalChartTickBar);
            }

            ChartBorder.Child = ChartBorderChildGrid;
        }

        #endregion
    }
}

/*
private void RefreshStackedChart()
        {
            var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
            stackPanel.Width = 1.0;
            stackPanel.Height = 1.0;
            MainBorder.Child = new Viewbox
            {
                Stretch = Stretch.Fill,
                Child = stackPanel,
            };

            var dsShape = (MultiChartDsShape)DsShapeViewModel.DsShape;
            int pointsCount = (int)GetValue(PointsCountProperty);
            double rectWidth = 1.0 / pointsCount;
            var genericContainer = new GenericContainer();
            genericContainer.ParentItem = dsShape.Container;
            var gpi = new DsConstant
            {
                Name = MultiChartDsShape.PointNumberConstantConst,
            };
            genericContainer.DsConstantsCollection.Add(gpi);
            for (int index = 0; index < pointsCount; index += 1)
            {
                gpi.Value = (index + 1).ToString();                
                var rect = new Rectangle
                {
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Width = rectWidth
                };
                stackPanel.Children.Add(rect);                
                rect.SetBindingOrConst(genericContainer, Shape.FillProperty, dsShape.PointDsBrush, BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
                rect.SetBindingOrConst(genericContainer, HeightProperty, dsShape.PointValueYInfo, BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
            }
        }
*/