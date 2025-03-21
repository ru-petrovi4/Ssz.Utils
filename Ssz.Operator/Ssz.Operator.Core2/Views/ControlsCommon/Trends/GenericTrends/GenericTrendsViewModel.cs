using System;
using System.Collections.Generic;
using System.Linq;
using Ssz.Operator.Core.DsShapes.Trends;


namespace Ssz.Operator.Core.ControlsCommon.Trends;

public class GenericTrendsViewModel : TrendsViewModel
{
    #region construction and destruction

    public GenericTrendsViewModel() :
        this(DateTime.Now)
    {
    }

    private GenericTrendsViewModel(DateTime now) :
        base(now, new DateRange(getMaximumTime(now) - TimeSpan.FromMinutes(5), getMaximumTime(now)))
    {
        TotalDateRange = new DateRange(getMinimumTime(now), getMaximumTime(now));

        ValueZoomLevel = new YAxisInterval(1);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var trend in _trends)
            {
                trend.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    #endregion

    #region public functions

    public static readonly TimeSpan TotalTimeRange = TimeSpan.FromDays(1);

    public bool AutoUpdateCurrentTime
    {
        get { return VisibleDateRange.Maximum == TotalDateRange.Maximum; }
    }

    public YAxisInterval ValueZoomLevel
    {
        get { return _valueZoomLevel; }
        set
        {
            SetValue(ref _valueZoomLevel, value);
            foreach (GenericTrendViewModel item in Items.OfType<GenericTrendViewModel>())
                item.UpdateMinimumMaximumBorders();
        }
    }

    public DateRange TotalDateRange
    {
        get { return _totalDateRange; }
        private set { SetValue(ref _totalDateRange, value); }
    }

    public void ZoomTime(TimeSpan visibleTimeRange)
    {
        if (AutoUpdateCurrentTime)
        {
            TotalDateRange = new DateRange(getMinimumTime(Now), getMaximumTime(Now));
            Zoom(Now - visibleTimeRange, Now);
        }
        else
        {
            Zoom(TotalDateRange.Clamp(new DateRange(VisibleDateRange.Center, visibleTimeRange)));
        }
    }

    public void OnHorizontalScrollChanged(double scrollFrom0To1)
    {
        DateTime virtualTime = new DateRange(
            TotalDateRange.Minimum + VisibleDateRange.Range,
            TotalDateRange.Maximum).Interpolate(scrollFrom0To1);

        Zoom(getMinimumVisibleTime(virtualTime), getMaximumVisibleTime(virtualTime));
    }

    public override void Display(IEnumerable<DsTrendItem> dsTrendItems)
    {
        var trends = dsTrendItems.Select(i =>
        {
            var trend = new Trend();
            trend.SubscribeTo(i);
            return trend;
        }).ToArray();

        foreach (var trend in _trends)
        {
            trend.Dispose();
        }
        _trends = trends;

        Items = trends.Select((i, idx) => new GenericTrendViewModel(this, i, idx));
    }

    public void Display(IEnumerable<Trend> trends)
    {
        foreach (var trend in _trends)
        {
            trend.Dispose();
        }
        _trends = trends.ToArray();

        Items = trends.Select((i, idx) => new GenericTrendViewModel(this, i, idx));
    }

    #endregion

    #region protected functions

    protected override void UpdateMinimumMaximumVisibleTime(DateTime oldNow, DateTime now)
    {
        if (AutoUpdateCurrentTime)
        {
            TotalDateRange = new DateRange(getMinimumTime(now), getMaximumTime(now));
            Zoom(
                getMinimumVisibleTime(now),
                getMaximumVisibleTime(now));
        }
    }

    #endregion

    #region private functions

    private static DateTime getMaximumTime(DateTime currentTime)
    {
        return currentTime;
    }

    private static DateTime getMinimumTime(DateTime currentTime)
    {
        return getMaximumTime(currentTime) - TotalTimeRange;
    }

    private static DateTime getMaximumVisibleTime(DateTime currentTime)
    {
        return currentTime;
    }

    private DateTime getMinimumVisibleTime(DateTime currentTime)
    {
        return getMaximumVisibleTime(currentTime) - VisibleDateRange.Range;
    }

    #endregion

    #region private fields

    private Trend[] _trends = [];

    private YAxisInterval _valueZoomLevel;
    private DateRange _totalDateRange;

    #endregion
}