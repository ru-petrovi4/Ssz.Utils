using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Media;
using System.Windows.Threading;
using Ssz.Operator.Core;
using Ssz.Operator.Core.ControlsCommon.Trends;
using Ssz.Operator.Core.DsShapes.Trends;
using Ssz.Utils;

namespace Ssz.Operator.Core.ControlsCommon.Trends.GenericTrends
{
    public class GenericTrendsViewModel : TrendsViewModel
    {
        #region construction and destruction

        public GenericTrendsViewModel(DateTime now, DateRange? dateRange = null) :
            base(now, dateRange ?? GetDefaultDateRange(now))
        {  
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_trends != null)
                {
                    foreach (var item in _trends)
                        item.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        public string Caption
        {
            get { return _caption; }
            set { SetValue(ref _caption, value); }
        }

        public ObservableCollection<GenericTrendViewModel> TrendViewModelsCollection
        {
            get { return _trendViewModelsCollection; }
            private set { SetValue(ref _trendViewModelsCollection, value); }
        }

        public void PanToCurrentTime()
        {
            Zoom(GetDefaultDateRange(Now));
        }

        public void Pan(double percentageOfVisibleArea)
        {
            Zoom(VisibleDateRange.Pan(percentageOfVisibleArea));
        }

        public void ResetPanAndZoom()
        {
            Zoom(GetDefaultDateRange(Now));
        }

        public void Zoom(TimeSpan visibleTimeRange)
        {
            DateTime timestampToZoomAround = DisplayingHistoryValueTimestamp == null
                ? Now
                : DisplayingHistoryValueTimestamp.Value;

            double percentage = VisibleDateRange.Percentage(timestampToZoomAround);

            if (percentage < 0 || percentage > 1)
                percentage = 0.5;

            DateTime newMinVisibleTime = timestampToZoomAround -
                                         TimeSpan.FromSeconds((long) (visibleTimeRange.TotalSeconds*percentage));
            DateTime newMaxVisibleTime = newMinVisibleTime + visibleTimeRange;

            Zoom(newMinVisibleTime, newMaxVisibleTime);
        }

        public void CenterMinAndMaxAroundCurrentDisplayedValue()
        {
            DateTime center = DisplayingHistoryValueTimestamp ?? Now;

            Zoom(new DateRange(center, VisibleDateRange.Range));
        }

        public override void Display(IEnumerable<DsTrendItem> trendItemInfos)
        {
            var trends = trendItemInfos
                .Where(trendItemInfo => !String.IsNullOrEmpty(trendItemInfo.TagName))
                .Select(trendItemInfo =>
            {        
                return new Trend(trendItemInfo, false, false, PlayDsProjectView.LastActiveRootPlayWindow);
            }).ToArray();

            if (_trends != null)
            {
                foreach (var item in _trends)
                    item.Dispose();
            }
            _trends = trends;

            var items = new List<TrendViewModel>();
            foreach (var trend in trends)
            {
                items.Add(new GenericTrendViewModel(trend));
            }           

            Items = items;

            var newTrendViewModelsCollection = new ObservableCollection<GenericTrendViewModel>();
            newTrendViewModelsCollection.CollectionChanged += TrendViewModelsCollectionChanged;
            foreach (GenericTrendViewModel trendViewModel in Items)
            {
                if (String.IsNullOrEmpty(trendViewModel.Generic_TagToDisplay))
                    continue;
                newTrendViewModelsCollection.Add(trendViewModel);
                if (newTrendViewModelsCollection.Count == 8)
                    break;
            }

            for (int i = 0; i < newTrendViewModelsCollection.Count; i += 1)
            {
                newTrendViewModelsCollection[i].Num = i + 1;
            }

            TrendViewModelsCollection = newTrendViewModelsCollection;
        }

        /// <summary>
        ///     Calls Display(...)
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="tag"></param>
        public void LoadTrendGroup(string groupId, string tag)
        {
            Caption = groupId;

            List<DsTrendItem> trendItemInfos = new List<DsTrendItem>();

            if (!String.IsNullOrEmpty(tag))
            {
                groupId = @"";

                foreach (var it in DsProject.Instance.CsvDb.GetData(GenericAddon.TrendGroups_FileName).OrderBy(i => i.Key))
                {
                    if (it.Key.Contains('|') && it.Value.Count >= 2)
                    {
                        if (it.Value[1]?.StartsWith(tag + ".", StringComparison.InvariantCultureIgnoreCase) ?? false)
                        {
                            groupId = it.Key.Substring(0, it.Key.IndexOf('|'));
                            break;
                        }
                    }
                }
            }

            if (!String.IsNullOrEmpty(groupId))
            {
                foreach (var it in DsProject.Instance.CsvDb.GetData(GenericAddon.TrendGroups_FileName).OrderBy(i => i.Key))
                {
                    if (!it.Key.StartsWith(groupId + "|", StringComparison.InvariantCultureIgnoreCase) || it.Value.Count < 2)
                        continue;
                    var trendItemInfo = new DsTrendItem(it.Value[1] ?? @"");

                    if (it.Value.Count >= 3 && !String.IsNullOrEmpty(it.Value[2]))
                    {
                        trendItemInfo.DsBrush = new BrushDataBinding(false, true)
                        {
                            ConstValue = new SolidDsBrush
                            {
                                Color = new Any(it.Value[2]).ValueAs<Color>(false)
                            }
                        };
                    }
                    else
                    {
                        trendItemInfo.DsBrush = new BrushDataBinding(false, true)
                        {
                            ConstValue = new SolidDsBrush
                            {
                                Color = GenericTrendViewModel.DefaultColors[trendItemInfos.Count % GenericTrendViewModel.DefaultColors.Length]
                            }
                        };
                    }
                    trendItemInfos.Add(trendItemInfo);
                }
            }
            else
            {
                var trendItemInfo = new DsTrendItem(tag + ".PV");
                trendItemInfo.DsBrush = new BrushDataBinding(false, true)
                {
                    ConstValue = new SolidDsBrush
                    {
                        Color = GenericTrendViewModel.DefaultColors[trendItemInfos.Count % GenericTrendViewModel.DefaultColors.Length]
                    }
                };
                trendItemInfos.Add(trendItemInfo);
            }

            Display(trendItemInfos);
        }

        public void Save()
        {
            SaveTrendGroup(Caption);            
        }

        #endregion

        #region protected functions              

        protected override void UpdateMinimumMaximumVisibleTime(DateTime oldNow, DateTime now)
        {
            if (VisibleDateRange.Includes(oldNow))
            {
                Zoom(VisibleDateRange.Pan(now - oldNow));
            }
        }

        #endregion

        #region private functions        

        private void SaveTrendGroup(string groupId)
        {
            var data = new CaseInsensitiveDictionary<List<string?>>(
                DsProject.Instance.CsvDb.GetData(GenericAddon.TrendGroups_FileName));

            foreach (var key in data.Keys.ToArray())
            {
                if (key.StartsWith(groupId + "|", StringComparison.InvariantCultureIgnoreCase))
                    data.Remove(key);
            }

            int n = 0;
            foreach (var trendViewModel in _trendViewModelsCollection)
            {
                n += 1;
                string key = groupId + "|" + n;
                data[key] = new List<string?> { key, trendViewModel.Source.HdaId, new Any(trendViewModel.Source.Brush).ValueAsString(false) };
            }

            DsProject.Instance.CsvDb.SetData(GenericAddon.TrendGroups_FileName, data.Values);

            DsProject.Instance.CsvDb.SaveData(GenericAddon.TrendGroups_FileName);
        }

        private static DateRange GetDefaultDateRange(DateTime now)
        {
            TimeSpan? defaultXAxisInterval = null;
            defaultXAxisInterval = TimeSpan.FromHours(8);
            return new DateRange(now, defaultXAxisInterval.Value, 1);
        }

        private void Refresh()
        {
            var newDsTrendItems = TrendViewModelsCollection.Select(i => i.Source.DsTrendItem).ToArray();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
                new Action(() =>
                {
                    Display(newDsTrendItems);
                })
            );
        }

        private void TrendViewModelsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {     
                foreach (GenericTrendViewModel trendViewModel in e.NewItems!)                
                {                    
                    trendViewModel.PropertyChanged += TrendViewModel_OnPropertyChanged;
                    if (String.IsNullOrEmpty(trendViewModel.Source.DsTrendItem.TagName))
                    {
                        trendViewModel.Source.DsTrendItem.DsBrush = new BrushDataBinding(false, true)
                        {
                            ConstValue = new SolidDsBrush
                            {
                                Color = GenericTrendViewModel.DefaultColors[
                                    _trendViewModelsCollection.IndexOf(trendViewModel) % GenericTrendViewModel.DefaultColors.Length]
                            }
                        };
                    }
                }
            } 
            else
            {
                Refresh();
            }

            for (int i = 0; i < _trendViewModelsCollection.Count; i += 1)
            {
                _trendViewModelsCollection[i].Num = i + 1;
            }
        }

        private void TrendViewModel_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GenericTrendViewModel.HdaId))
            {
                Refresh();
            }
        }        

        #endregion

        #region private fields

        private string _caption;

        private Trend[] _trends = null!;

        private ObservableCollection<GenericTrendViewModel> _trendViewModelsCollection = new ObservableCollection<GenericTrendViewModel>();

        #endregion        
    }

    //public sealed class TrendsDesignData : GenericTrendsViewModel
    //{
    //    #region construction and destruction

    //    public TrendsDesignData() :
    //        base(DateTime.Now, @"", @"")
    //    {
    //        //Display(RandomTrend.SamplesForDesignData);
    //    }

    //    #endregion
    //}
}
      