using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Ssz.Utils;

namespace Ssz.Operator.Core.ControlsCommon.Trends.GenericTrends;

public class XAxisInterval
{
    #region construction and destruction

    public XAxisInterval(TimeSpan visibleRange)
    {
        _visibleRange = visibleRange;
    }

    #endregion

    #region public functions

    public TimeSpan VisibleRange
    {
        get { return _visibleRange; }
    }

    public override bool Equals(object? obj)
    {
        var that = obj as XAxisInterval;
        if (that == null)
            return false;

        return _visibleRange == that._visibleRange;
    }

    public override int GetHashCode()
    {
        return _visibleRange.GetHashCode();
    }

    #endregion

    #region private fields

    private readonly TimeSpan _visibleRange;

    #endregion
}

public class YAxisInterval
{
    #region construction and destruction

    public YAxisInterval(double scaleCoefficient)
    {
        _scaleCoefficient = scaleCoefficient;
    }

    #endregion

    #region public functions

    public double ScaleCoefficient
    {
        get { return _scaleCoefficient; }
    }

    public double VisibleRange(double minScale, double maxScale)
    {
        return (maxScale - minScale)*_scaleCoefficient;
    }

    public override bool Equals(object? obj)
    {
        var that = obj as YAxisInterval;
        if (that == null)
            return false;

        return Math.Abs(_scaleCoefficient - that._scaleCoefficient) < 1e-6;
    }

    public override int GetHashCode()
    {
        return _scaleCoefficient.GetHashCode();
    }

    #endregion

    #region private fields

    private readonly double _scaleCoefficient;

    #endregion
}

public class GenericToolBarViewModel : ViewModelBase
{
    #region construction and destruction

    public GenericToolBarViewModel(GenericTrendsViewModel trendsViewModel)
    {
        _trendsViewModel = trendsViewModel;

        _timeZoomLevels = RoskTimeZoomLevels;
        _currentTimeZoomLevelIndex = 3;

        trendsViewModel.ZoomTime(RoskTimeZoomLevels[_currentTimeZoomLevelIndex].VisibleRange);

        _valueZoomLevels = RoskValueZoomLevels;
        _currentValueZoomLevelIndex = 2;
        trendsViewModel.ValueZoomLevel = RoskValueZoomLevels[_currentValueZoomLevelIndex];

        ZoomInTimeCommand = new RelayCommand(
            zoomInTime,
            canZoomInTime);

        ZoomOutTimeCommand = new RelayCommand(
            zoomOutTime,
            canZoomOutTime);

        ZoomInValueCommand = new RelayCommand(
            zoomInValue,
            canZoomInValue);

        ZoomOutValueCommand = new RelayCommand(
            zoomOutValue,
            canZoomOutValue);
    }

    #endregion

    #region public functions

    public static readonly XAxisInterval[] RoskTimeZoomLevels =
    {
        new XAxisInterval(TimeSpan.FromMinutes(14)),
        new XAxisInterval(TimeSpan.FromMinutes(25)),
        new XAxisInterval(TimeSpan.FromMinutes(45)),
        new XAxisInterval(TimeSpan.FromMinutes(81)),
        new XAxisInterval(TimeSpan.FromMinutes(146)),
        new XAxisInterval(TimeSpan.FromMinutes(264)),
        new XAxisInterval(TimeSpan.FromMinutes(480))
    };

    public static readonly YAxisInterval[] RoskValueZoomLevels =
    {
        new YAxisInterval(0.1),
        new YAxisInterval(0.33),
        new YAxisInterval(0.55),
        new YAxisInterval(0.78),
        new YAxisInterval(1)
    };

    public ICommand ZoomInTimeCommand { get; private set; }
    public ICommand ZoomOutTimeCommand { get; private set; }
    public ICommand ZoomInValueCommand { get; private set; }
    public ICommand ZoomOutValueCommand { get; private set; }

    public IEnumerable<XAxisInterval> TimeZoomLevels
    {
        get { return _timeZoomLevels; }
    }

    public IEnumerable<YAxisInterval> ValueZoomLevels
    {
        get { return _valueZoomLevels; }
    }

    //public void LoadFromDsProjectSettings(TrendsInfo trendProperties)
    //{        
    //    XAxisInterval[] timeZoomLevels = trendProperties.ParseXAxisIntervals()
    //        .Select(timeSpan => new XAxisInterval(timeSpan))
    //        .ToArray();

    //    if (timeZoomLevels.Length != 0)
    //    {
    //        XAxisInterval defaultTimeZoomLevel = loadDefaultTimeZoomLevel(
    //            trendProperties,
    //            timeZoomLevels);

    //        _timeZoomLevels = timeZoomLevels;

    //        _currentTimeZoomLevelIndex = 0;
    //        for (int i = 0; i < _timeZoomLevels.Length; i += 1)
    //            if (defaultTimeZoomLevel.VisibleRange == _timeZoomLevels[i].VisibleRange)
    //                _currentTimeZoomLevelIndex = i;

    //        _trendsViewModel.ZoomTime(defaultTimeZoomLevel.VisibleRange);
    //    }

    //    YAxisInterval[] valueZoomLevels = trendProperties.ParseYAxisIntervals()
    //        .Select(value => new YAxisInterval(value))
    //        .ToArray();

    //    if (valueZoomLevels.Length == 0)
    //    {
    //        _valueZoomLevels = RoskValueZoomLevels;
    //        _trendsViewModel.ValueZoomLevel = RoskValueZoomLevels[2];
    //    }
    //    else
    //    {
    //        YAxisInterval defaultValueZoomLevel = loadDefaultValueZoomLevel(
    //            trendProperties,
    //            valueZoomLevels);

    //        _valueZoomLevels = valueZoomLevels;

    //        _currentValueZoomLevelIndex = 0;
    //        for (int i = 0; i < _valueZoomLevels.Length; i += 1)
    //            if (Math.Abs(defaultValueZoomLevel.ScaleCoefficient - _valueZoomLevels[i].ScaleCoefficient) < 1e-6)
    //                _currentValueZoomLevelIndex = i;

    //        _trendsViewModel.ValueZoomLevel = defaultValueZoomLevel;
    //    }
    //}

    #endregion

    #region private functions

    //private static XAxisInterval loadDefaultTimeZoomLevel(TrendsInfo trendProperties,
    //    XAxisInterval[] timeZoomLevels)
    //{
    //    return timeZoomLevels[timeZoomLevels.Length / 2];
    //    /*
    //    XAxisInterval fallbackZoom = timeZoomLevels[timeZoomLevels.Length/2];

    //    TimeSpan? defaultXAxisInterval = trendProperties.ParseDefaultXAxisInterval();
    //    if (defaultXAxisInterval == null)
    //        return fallbackZoom;

    //    var timeZoom = new XAxisInterval(defaultXAxisInterval.Value);

    //    return timeZoomLevels.Contains(timeZoom) ? timeZoom : fallbackZoom;
    //    */
    //}

    //private static YAxisInterval loadDefaultValueZoomLevel(TrendsInfo trendProperties,
    //    YAxisInterval[] valueZoomLevels)
    //{
    //    return valueZoomLevels[valueZoomLevels.Length / 2];
    //    /*
    //    YAxisInterval fallbackZoom = valueZoomLevels[valueZoomLevels.Length/2];

    //    double? valueZoomValue = trendProperties.ParseDefaultYAxisInterval();
    //    if (valueZoomValue == null)
    //        return fallbackZoom;

    //    var valueZoom = new YAxisInterval(valueZoomValue.Value);

    //    return valueZoomLevels.Contains(valueZoom) ? valueZoom : fallbackZoom;
    //    */
    //}

    private void zoomInTime()
    {
        if (_currentTimeZoomLevelIndex < 1)
            return;

        XAxisInterval previousZoom = _timeZoomLevels[_currentTimeZoomLevelIndex - 1];
        _trendsViewModel.ZoomTime(previousZoom.VisibleRange);

        _currentTimeZoomLevelIndex --;
    }

    private void zoomOutTime()
    {
        if (_currentTimeZoomLevelIndex == _timeZoomLevels.Length - 1)
            return;

        XAxisInterval nextZoom = _timeZoomLevels[_currentTimeZoomLevelIndex + 1];
        _trendsViewModel.ZoomTime(nextZoom.VisibleRange);
        _currentTimeZoomLevelIndex  += 1;
    }

    private bool canZoomInTime()
    {
        return _currentTimeZoomLevelIndex >= 1;
    }

    private bool canZoomOutTime()
    {
        return _currentTimeZoomLevelIndex < _timeZoomLevels.Length - 1;
    }

    private void zoomInValue()
    {
        if (_currentValueZoomLevelIndex < 1)
            return;

        YAxisInterval previousZoom = _valueZoomLevels[_currentValueZoomLevelIndex - 1];
        _trendsViewModel.ValueZoomLevel = previousZoom;
        _currentValueZoomLevelIndex --;
    }

    private void zoomOutValue()
    {
        if (_currentValueZoomLevelIndex == _valueZoomLevels.Length - 1)
            return;

        YAxisInterval nextZoom = _valueZoomLevels[_currentValueZoomLevelIndex + 1];
        _trendsViewModel.ValueZoomLevel = nextZoom;
        _currentValueZoomLevelIndex  += 1;
    }

    private bool canZoomInValue()
    {
        return _currentValueZoomLevelIndex >= 1;
    }

    private bool canZoomOutValue()
    {
        return _currentValueZoomLevelIndex < _valueZoomLevels.Length - 1;
    }

    #endregion

    #region private fields

    private readonly GenericTrendsViewModel _trendsViewModel;
    private XAxisInterval[] _timeZoomLevels;
    private YAxisInterval[] _valueZoomLevels;

    private int _currentTimeZoomLevelIndex, _currentValueZoomLevelIndex;

    #endregion
}