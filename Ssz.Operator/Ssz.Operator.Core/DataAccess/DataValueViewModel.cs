using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Utils;
using Ssz.Utils.Wpf;

namespace Ssz.Operator.Core.DataAccess
{
    public class DataValueViewModel : DisposableViewModelBase
    {
        #region private fields

        private readonly Dictionary<string, DataValueItem> _dataValueItemsDictionary =
            new();

        #endregion

        #region protected functions

        protected virtual void OnGlobalUITimerEvent(int phase)
        {
            if (IsDisposed) return;

            OnPropertyChanged(Binding.IndexerName);
        }        

        #endregion

        #region construction and destruction

        public DataValueViewModel(IPlayWindowBase? playWindow, bool visualDesignMode)
        {
            PlayWindow = playWindow;
            VisualDesignMode = visualDesignMode;

            MultiBindings = new List<MultiBinding>();

            DsProject.Instance.GlobalUITimerEvent += OnGlobalUITimerEvent;
        }        

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed) return;

            if (disposing)
            {
                DsProject.Instance.GlobalUITimerEvent -= OnGlobalUITimerEvent;

                foreach (MultiBinding multiBinding in MultiBindings)
                {
                    var disposable = multiBinding.Converter as IDisposable;
                    if (disposable is not null) 
                        disposable.Dispose();
                }

                MultiBindings.Clear();

                // Release and Dispose managed resources.
                foreach (DataValueItem dataValueItem in _dataValueItemsDictionary.Values)
                {
                    dataValueItem.Dispose();
                }
                _dataValueItemsDictionary.Clear();
            }

            PlayWindow = null;

            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        public IPlayWindowBase? PlayWindow { get; private set; }

        public bool VisualDesignMode { get; }

        public List<MultiBinding> MultiBindings { get; }

        public object? this[string dataSourceString]
        {
            get
            {
                DataValueItem? dataValueItem;
                _dataValueItemsDictionary.TryGetValue(dataSourceString, out dataValueItem);
                if (dataValueItem is null)
                {
                    dataValueItem = new DataValueItem(dataSourceString, DsProject.Instance.GlobalVariables, PlayWindow,
                        VisualDesignMode);
                    _dataValueItemsDictionary[dataSourceString] = dataValueItem;
                }

                return dataValueItem.Value;
            }
            set
            {
                DataValueItem? dataValueItem;
                _dataValueItemsDictionary.TryGetValue(dataSourceString, out dataValueItem);
                if (dataValueItem is null)
                {
                    dataValueItem = new DataValueItem(dataSourceString, DsProject.Instance.GlobalVariables, PlayWindow,
                        VisualDesignMode);
                    _dataValueItemsDictionary[dataSourceString] = dataValueItem;
                }

                dataValueItem.Value = value;
            }
        }

        public async Task WaitAllDataValueItemsUpdated()
        {
            var events = _dataValueItemsDictionary.Select(kvp => kvp.Value.ValueUpdatedEvent).ToArray();
            if (events.All(e => e.WaitOne(0))) return;
            await Task.Run(() => WaitHandle.WaitAll(events));
        }

        #endregion
    }
}