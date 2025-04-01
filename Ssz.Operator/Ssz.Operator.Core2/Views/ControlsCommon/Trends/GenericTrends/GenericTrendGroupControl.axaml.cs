using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Ssz.Operator.Core.ControlsCommon.Trends.ZoomLevels;

namespace Ssz.Operator.Core.ControlsCommon.Trends.GenericTrends;

public partial class TrendGroupControl : UserControl
{
    #region construction and destruction

    public TrendGroupControl()
    {
        InitializeComponent();

        DataContext = new GenericTrendsViewModel();

        UpdateTimeAxis();
    }

    #endregion

    #region public functions

    public static readonly AvaloniaProperty SelectedItemProperty = AvaloniaProperty.Register<TrendGroupControl, object?>(nameof(SelectedItem));

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public GenericTrendsViewModel GenericTrendsViewModel => (GenericTrendsViewModel)DataContext!;

    //public ObservableCollection<Trend>? TrendItemViewsCollection
    //{
    //    get => _trendItemViewsCollection;
    //    set
    //    {
    //        _trendItemViewsCollection = value;
    //        MainGenericTrendsPlotView.TrendItemViewsCollection = value;
    //        MainGenericTrendsInfoTableControl.TrendItemViewsCollection = value;

    //        SetBinding(DataContextProperty, new Binding
    //        {
    //            Source = MainGenericTrendsInfoTableControl,
    //            Path = new PropertyPath("SelectedItem"),
    //            Mode = BindingMode.OneWay
    //        });
    //        MainGenericTrendsPlotView.SetBinding(MainGenericTrendsPlotViewControl.SelectedItemProperty, new Binding
    //        {
    //            Source = MainGenericTrendsInfoTableControl,
    //            Path =
    //                new PropertyPath("SelectedItem"),
    //            Mode = BindingMode.OneWay
    //        });
    //        SetBinding(SelectedItemProperty, new Binding
    //        {
    //            Source = MainGenericTrendsInfoTableControl,
    //            Path = new PropertyPath("SelectedItem"),
    //            Mode = BindingMode.OneWay
    //        });

    //        if (value is not null)
    //        {
    //            foreach (var tr in value)
    //            {
    //                var tr2 = tr;
    //                tr.PropertyChanged += a => TrendItemViewOnChanged(tr2);
    //            }

    //            MainGenericTrendsInfoTableControl.SelectedItem = value.FirstOrDefault();
    //        }
    //    }
    //}

    #endregion    

    #region protected functions

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property == SelectedItemProperty)
        {
            UpdateValueAxis();
        }
    }

    #endregion

    #region private functions

    private void UpdateTimeAxis()
    {
        //MainGenericTrendsPlotView.ZoomRestriction.Interval = _currentTimeZoom.VisibleRange;
        //MainGenericTrendsPlotView.Update();
    }

    private void UpdateValueAxis()
    {
        if (_trendItemViewsCollection is null) 
            return;

        foreach (Trend trendItemView in _trendItemViewsCollection)
        {
            TrendItemViewOnChanged(trendItemView);
        }

        //MainGenericTrendsPlotView.Update();
    }

    private void TrendItemViewOnChanged(Trend trendItemView)
    {
        if (double.IsNaN(trendItemView.YMin) || double.IsNaN(trendItemView.YMax)) return;

        var visibleValueRange = _currentValueZoom.VisibleRange(trendItemView.YMin, trendItemView.YMax);
        trendItemView.VisibleYMin = trendItemView.YMin - visibleValueRange / 2;
        trendItemView.VisibleYMax = trendItemView.YMax + visibleValueRange / 2;

        //if (trendItemView.ObservableDataSource is not null)
        //    trendItemView.ObservableDataSource.SetYMapping(trendItemView.YMapping);

        //if (ReferenceEquals(SelectedItem, trendItemView))
        //{
        //    MainGenericTrendsPlotView.ZoomRestriction.ValueRange = visibleValueRange;
        //    MainGenericTrendsPlotView.ZoomRestriction.ValueMin = trendItemView.VisibleYMin;
        //    MainGenericTrendsPlotView.ZoomRestriction.ValueMax = trendItemView.VisibleYMax;
        //}
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