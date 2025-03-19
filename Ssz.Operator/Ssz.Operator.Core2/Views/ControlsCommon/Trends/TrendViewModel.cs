using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Ssz.Operator.Core.DataAccess;
using Ssz.Utils;

namespace Ssz.Operator.Core.ControlsCommon.Trends
{    
    public class TrendViewModel : ViewModelBase
    {
        #region construction and destruction

        public TrendViewModel(Trend trend)
        {
            Source = trend;
            Source.PropertyChanged += Source_OnPropertyChanged;

            Source_OnPropertyChanged();
            DisplayCurrentValue();
        }        

        #endregion

        #region public functions

        public async void LoadPoints(DateRange range)
        {
            if (_isPointsLoadingInProgress)
                return;

            _isPointsLoadingInProgress = true;
           
            var points = (await DsDataAccessProvider.Instance.ReadElementValuesJournal(
                    Source.HdaId,
                    range.Minimum.ToUniversalTime(),
                    range.Maximum.ToUniversalTime()))
                .Select(xiValue => new TrendPoint(
                        xiValue.TimestampUtc.ToLocalTime(),
                        xiValue.Value.ValueAsDouble(false))).ToList();

            _rawTrendPoints = points.ToArray();
           
            OnRawTrendPointsLoaded(_rawTrendPoints);

            _isPointsLoadingInProgress = false;
        }

        public IEnumerable<TrendPoint> Points
        {
            get { return _points; }
            protected set { SetValue(ref _points, value.ToArray()); }
        }

        public IEnumerable<TrendPoint> RawTrendPoints
        {
            get { return _rawTrendPoints; }
        }

        public virtual void OnTimerTick()
        {
            if (!_isValueFreezed)
                UpdateCurrentValueAndTimestamp();
        }

        public void DisplayCurrentValue()
        {
            _isValueFreezed = false;
            UpdateCurrentValueAndTimestamp();
        }        

        public void DisplayValueAtTime(DateTime time)
        {
            _isValueFreezed = true;
            ValueTimestamp = time;
            Value = GetValue(time);
        }

        public double? GetValue(DateTime time)
        {
            var trendPt = _rawTrendPoints
                .OrderBy(pt => pt.Timestamp)
                .TakeWhile(pt => pt.Timestamp <= time)
                .LastOrDefault();

            var nextTrendPt = _rawTrendPoints
                .OrderBy(pt => pt.Timestamp)
                .SkipWhile(pt => pt.Timestamp <= time)
                .FirstOrDefault();

            if (trendPt != null && nextTrendPt != null)
            {
                var percentage =
                    (double)(time - trendPt.Timestamp).Ticks /
                    (nextTrendPt.Timestamp - trendPt.Timestamp).Ticks;

                return trendPt.Value + (nextTrendPt.Value - trendPt.Value) * percentage;
            }

            if (trendPt != null && trendPt.Timestamp == time)
                return trendPt.Value;

            return null;
        }

        public Trend Source { get; private set; }

        public bool IsDisplayedOnPlot
        {
            get { return _isDisplayedOnPlot; }
            set { SetValue(ref _isDisplayedOnPlot, value); }
        }

        public double? Value
        {
            get { return _value; }
            private set { SetValue(ref _value, value); }
        }

        public string ValueFormat
        {
            get { return _valueFormat; }
            set { SetValue(ref _valueFormat, value); }
        }

        public DateTime? ValueTimestamp
        {
            get { return _valueTimestamp; }
            private set { SetValue(ref _valueTimestamp, value); }
        }

        public Brush Brush
        {
            get { return Source.Brush; }
        }

        public Color Color
        {
            get { return _color; }
            set { SetValue(ref _color, value); }
        }        

        public string HdaIdToDisplay
        {
            get { return Source.HdaIdToDisplay; }
        }

        public string TagNameToDisplay
        {
            get { return Source.TagNameToDisplay; }
        }

        public string Description
        {
            get { return Source.Description; }
        }        

        public string EU
        {
            get { return Source.EU; }
        }

        public double YMin
        {
            get { return Source.YMin; }
        }

        public double YMax
        {
            get { return Source.YMax; }
        }

        public virtual void OnSelectedTrendChanged()
        {
        }

        public static string GetNumberFormat(double min, double max)
        {
            if (Double.IsNaN(min) || Double.IsNaN(max)) return "G4";
            double delta = max - min;
            if (delta > 9999999 || delta < 0.000001) return "G4";
            if (delta < 1)
            {
                int nAfter = -(int)(Math.Log10(delta) - 1);
                switch (nAfter)
                {
                    case 1:
                        return "0.0000";
                    case 2:
                        return "0.00000";
                    case 3:
                        return "0.000000";
                    case 4:
                        return "0.0000000";
                }
                return "G4";
            }
            else
            {
                var nBefore = (int)(Math.Log10(min + 0.5 * delta) + 1);
                int nAfter = 3 - nBefore;
                if (nAfter <= 0) return "######0";
                switch (nAfter)
                {
                    case 0:
                        return "######0";
                    case 1:
                        return "######0.0";
                    case 2:
                        return "######0.00";
                    case 3:
                        return "######0.000";
                    case 4:
                        return "######0.0000";
                    case 5:
                        return "######0.00000";
                }
                return "G4";
            }
        }

        #endregion

        #region protected functions
        
        /// <summary>
        ///     convert from [_minScale, _maxScale] to [targetMinScale, targetMaxScale] range.
        /// </summary>
        /// <param name="minScale"></param>
        /// <param name="maxScale"></param>
        /// <param name="targetMinScale"></param>
        /// <param name="targetMaxScale"></param>
        /// <returns></returns>
        protected IEnumerable<TrendPoint> GetScaledPoints(
            double minScale, double maxScale,
            double targetMinScale, double targetMaxScale)
        {
            return _rawTrendPoints.Select(pt =>
                new TrendPoint(pt.Timestamp,
                    (pt.Value - minScale) / (maxScale - minScale) *
                    (targetMaxScale - targetMinScale) + targetMinScale)).ToList();
        }

        protected virtual void OnRawTrendPointsLoaded(TrendPoint[] points)
        {
            Points = points;
        }

        protected virtual void Source_OnPropertyChanged(object? sender = null, AvaloniaPropertyChangedEventArgs? args = null)
        {
            if (args == null || args.Property == Trend.BrushProperty)
                UpdateColor();

            if (args == null || args.Property == Trend.YMinProperty)
            {
                if (String.IsNullOrEmpty(ValueFormat))
                    ValueFormat = GetNumberFormat(Source.YMin, Source.YMax);
            }
            if (args == null || args.Property == Trend.YMaxProperty)
            {
                if (String.IsNullOrEmpty(ValueFormat))
                    ValueFormat = GetNumberFormat(Source.YMin, Source.YMax);
            }
            if (args == null || args.Property == Trend.ValueFormatProperty)
            {
                if (!String.IsNullOrEmpty(Source.ValueFormat))
                    ValueFormat = Source.ValueFormat;
            }

            if (args != null)
                OnPropertyChanged(args.Property.Name);
        }

        #endregion

        #region private functions    

        private void UpdateCurrentValueAndTimestamp()
        {
            Value = Source.Value;
            ValueTimestamp = DateTime.Now;
        }        

        private void UpdateColor()
        {
            var brush = Source.Brush as SolidColorBrush;

            Color = brush != null
                ? brush.Color
                : Colors.White;
        }

        #endregion

        #region private fields        

        private Color _color;

        private double? _value;
        private string _valueFormat = @"";
        private DateTime? _valueTimestamp;
        private bool _isValueFreezed;

        private TrendPoint[] _rawTrendPoints = { };
        private TrendPoint[] _points = { };

        private bool _isDisplayedOnPlot = true;

        private bool _isPointsLoadingInProgress;

        #endregion
    }
}
