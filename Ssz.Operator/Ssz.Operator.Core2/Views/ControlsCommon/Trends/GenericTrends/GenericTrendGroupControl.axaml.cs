using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Ssz.Operator.Core.ControlsCommon.Trends.ZoomLevels;

namespace Ssz.Operator.Core.ControlsCommon.Trends;

public partial class TrendGroupControl : UserControl
{
    #region construction and destruction

    public TrendGroupControl()
    {
        InitializeComponent();

        UpdateTimeAxis();
    }

    #endregion

    #region public functions

    public static readonly AvaloniaProperty SelectedItemProperty = AvaloniaProperty.Register("SelectedItem",
        typeof(object),
        typeof(
            TrendGroupControl
        ),
        new PropertyMetadata
            (SelectedItemChanged));

    public ObservableCollection<Trend>? TrendItemViewsCollection
    {
        get => _trendItemViewsCollection;
        set
        {
            _trendItemViewsCollection = value;
            TrendChartPlotter.TrendItemViewsCollection = value;
            TrendsInfoTableControl.TrendItemViewsCollection = value;

            SetBinding(DataContextProperty, new Binding
            {
                Source = TrendsInfoTableControl,
                Path = new PropertyPath("SelectedItem"),
                Mode = BindingMode.OneWay
            });
            TrendChartPlotter.SetBinding(TrendChartPlotterControl.SelectedItemProperty, new Binding
            {
                Source = TrendsInfoTableControl,
                Path =
                    new PropertyPath("SelectedItem"),
                Mode = BindingMode.OneWay
            });
            SetBinding(SelectedItemProperty, new Binding
            {
                Source = TrendsInfoTableControl,
                Path = new PropertyPath("SelectedItem"),
                Mode = BindingMode.OneWay
            });

            if (value is not null)
            {
                foreach (var tr in value)
                {
                    var tr2 = tr;
                    tr.PropertyChanged += a => TrendItemViewOnChanged(tr2);
                }

                TrendsInfoTableControl.SelectedItem = value.FirstOrDefault();
            }
        }
    }

    public object SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    #endregion

    #region private functions

    private static void SelectedItemChanged(DependencyObject d, AvaloniaPropertyChangedEventArgs e)
    {
        var control = (TrendGroupControl) d;
        var seria = e.NewValue as Trend;

        control.UpdateValueAxis();
    }

    private void UpdateTimeAxis()
    {
        TrendChartPlotter.ZoomRestriction.Interval = _currentTimeZoom.VisibleRange;
        TrendChartPlotter.Update();
    }

    private void UpdateValueAxis()
    {
        if (_trendItemViewsCollection is null) return;

        foreach (Trend trendItemView in _trendItemViewsCollection) TrendItemViewOnChanged(trendItemView);

        TrendChartPlotter.Update();
    }

    private void TrendItemViewOnChanged(Trend trendItemView)
    {
        if (double.IsNaN(trendItemView.YMin) || double.IsNaN(trendItemView.YMax)) return;

        var visibleValueRange = _currentValueZoom.VisibleRange(trendItemView.YMin, trendItemView.YMax);
        trendItemView.VisibleYMin = trendItemView.YMin - visibleValueRange / 2;
        trendItemView.VisibleYMax = trendItemView.YMax + visibleValueRange / 2;

        if (trendItemView.ObservableDataSource is not null)
            trendItemView.ObservableDataSource.SetYMapping(trendItemView.YMapping);

        if (ReferenceEquals(SelectedItem, trendItemView))
        {
            TrendChartPlotter.ZoomRestriction.ValueRange = visibleValueRange;
            TrendChartPlotter.ZoomRestriction.ValueMin = trendItemView.VisibleYMin;
            TrendChartPlotter.ZoomRestriction.ValueMax = trendItemView.VisibleYMax;
        }
    }

    private void onDecreaseTimeZoomButtonClicked(object? sender, RoutedEventArgs e)
    {
        if (_currentTimeZoom.Next is not null) _currentTimeZoom = _currentTimeZoom.Next;

        IncreaseTimeZoomButton.IsEnabled = !_currentTimeZoom.IsMinimum;
        DecreaseTimeZoomButton.IsEnabled = !_currentTimeZoom.IsMaximum;

        UpdateTimeAxis();
    }

    private void onIncreaseTimeZoomButtonClicked(object? sender, RoutedEventArgs e)
    {
        if (_currentTimeZoom.Previous is not null) _currentTimeZoom = _currentTimeZoom.Previous;

        IncreaseTimeZoomButton.IsEnabled = !_currentTimeZoom.IsMinimum;
        DecreaseTimeZoomButton.IsEnabled = !_currentTimeZoom.IsMaximum;

        UpdateTimeAxis();
    }

    private void onDecreaseValueZoomButtonClicked(object? sender, RoutedEventArgs e)
    {
        if (_currentValueZoom.Next is not null) _currentValueZoom = _currentValueZoom.Next;

        IncreaseValueZoomButton.IsEnabled = !_currentValueZoom.IsMinimum;
        DecreaseValueZoomButton.IsEnabled = !_currentValueZoom.IsMaximum;

        UpdateValueAxis();
    }

    private void onIncreaseValueZoomButtonClicked(object? sender, RoutedEventArgs e)
    {
        if (_currentValueZoom.Previous is not null) _currentValueZoom = _currentValueZoom.Previous;

        IncreaseValueZoomButton.IsEnabled = !_currentValueZoom.IsMinimum;
        DecreaseValueZoomButton.IsEnabled = !_currentValueZoom.IsMaximum;

        UpdateValueAxis();
    }

    #endregion

    #region private fields

    private ObservableCollection<Trend>? _trendItemViewsCollection;

    private TimeZoomLevel _currentTimeZoom = TimeZoomLevel.Three;
    private ValueZoomLevel _currentValueZoom = ValueZoomLevel.Four;

    #endregion
}