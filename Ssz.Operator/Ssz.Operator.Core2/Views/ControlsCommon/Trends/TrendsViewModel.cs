using System;
using System.Collections.Generic;
using System.Linq;
using Ssz.Operator.Core.ControlsCommon;
using Ssz.Operator.Core.DsShapes.Trends;
using Ssz.Operator.Core.ControlsCommon.Trends;
using Ssz.Utils;
using Ssz.Operator.Core;

namespace Ssz.Operator.Core.ControlsCommon.Trends
{    
    public abstract class TrendsViewModel : ViewModelBase, IDisposable
    {
        #region construction and destruction

        protected TrendsViewModel(
            DateTime now,
            DateRange visibleDateRange)
        {
            _now = now;
            _visibleDateRange = visibleDateRange;
        }

        ~TrendsViewModel()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {            
            if (disposing) 
            {
                foreach (var trend in _trends)
                {
                    trend.Dispose();
                }
            }
        }

        #endregion

        #region public functions

        public virtual void OnTimerTick(DateTime? now = null)
        {
            var nowOrDefault = now ?? DateTime.Now;
            UpdateMinimumMaximumVisibleTime(_now, nowOrDefault);

            foreach (var trendViewModel in _items)
                trendViewModel.OnTimerTick();

            _now = nowOrDefault;
        }        

        /// <summary>
        ///     You can override this.
        /// </summary>
        /// <param name="dsTrendItems"></param>
        public virtual void Display(IEnumerable<DsTrendItem> dsTrendItems)
        {
            var trends = dsTrendItems.Select(it =>
            {                     
                return new Trend(it, false, PlayDsProjectView.LastActiveRootPlayWindow);
            }).ToArray();

            foreach (var trend in _trends)
            {
                trend.Dispose();
            }            
            _trends = trends;

            Items = trends.Select(tr => new TrendViewModel(tr));
        }

        public IEnumerable<TrendViewModel> Items
        {
            get { return _items; }
            set
            {
                if (SetValue(ref _items, value.ToArray()))
                {
                    SelectedItem = _items.FirstOrDefault();
                    LoadTrendPoints();
                }
            }
        }

        public TrendViewModel? SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                if (SetValue(ref _selectedItem, value))
                {
                    foreach (var viewModel in Items)
                        viewModel.OnSelectedTrendChanged();
                }
            }
        }

        public DateRange VisibleDateRange
        {
            get { return _visibleDateRange; }
            private set { SetValue(ref _visibleDateRange, value); }
        }

        public void DisplayCurrentTrendValues()
        {
            foreach (var viewModel in _items)
                viewModel.DisplayCurrentValue();

            DisplayingHistoryValueTimestamp = null;
        }

        public void DisplayValuesAtTime(DateTime time)
        {
            foreach (var viewModel in _items)
                viewModel.DisplayValueAtTime(time);

            DisplayingHistoryValueTimestamp = time;
        }

        public void Zoom(DateTime minimumVisibleTime, DateTime maximumVisibleTime)
        {
            Zoom(new DateRange(minimumVisibleTime, maximumVisibleTime));
        }

        public void Zoom(DateRange dateRangeToBecomeVisible)
        {
            VisibleDateRange = dateRangeToBecomeVisible;
            LoadTrendPoints();
        }        

        #endregion        

        protected virtual void UpdateMinimumMaximumVisibleTime(DateTime oldNow, DateTime now)
        {
        }

        protected DateTime Now
        {
            get { return _now; }
        }

        protected DateTime? DisplayingHistoryValueTimestamp { get; private set; }

        #region private functions

        private void LoadTrendPoints()
        {
            var range = VisibleDateRange.AddPadding(VisibleDateRange.Range);

            foreach (var trendViewModel in _items)
                trendViewModel.LoadPoints(range);
        }

        #endregion

        #region private fields

        private Trend[] _trends = [ ];

        private TrendViewModel[] _items = [ ];

        private TrendViewModel? _selectedItem;
        
        private DateTime _now;
        private DateRange _visibleDateRange;

        #endregion
    }
}
