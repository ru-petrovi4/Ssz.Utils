using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Threading;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Utils;
//using Ssz.Utils.Wpf;

namespace Ssz.Operator.Core.DataAccess
{
    public class DataValueViewModel : Play.ViewModels.DisposableViewModelBase
    {
        #region construction and destruction

        public DataValueViewModel(IPlayWindowBase? playWindow, bool visualDesignMode)
        {
            PlayWindow = playWindow;
            VisualDesignMode = visualDesignMode;

            DsProject.Instance.GlobalUITimerEvent += OnGlobalUITimerEvent;
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed) return;

            if (disposing)
            {
                DsProject.Instance.GlobalUITimerEvent -= OnGlobalUITimerEvent;

                Close();
            }

            PlayWindow = null;

            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        public IPlayWindowBase? PlayWindow { get; private set; }

        public bool VisualDesignMode { get; }
        
        public virtual void Initialize(object? param_)
        {
        }

        public virtual void Close()
        {
            foreach (MultiBinding multiBinding in _multiBindings)
            {
                var disposable = multiBinding.Converter as IDisposable;
                if (disposable is not null)
                    disposable.Dispose();
            }

            _multiBindings.Clear();

            // Release and Dispose managed resources.
            foreach (DataValueItem dataValueItem in _dataValueItemsDictionary.Values)
            {
                dataValueItem.Dispose();
            }
            _dataValueItemsDictionary.Clear();
        }

        public void RegisterMultiBinding(MultiBinding multiBinding)
        {            
            _multiBindings.Add(multiBinding);
        }

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

        #region protected functions

        protected virtual void OnGlobalUITimerEvent(int phase)
        {
            if (IsDisposed) 
                return;

            NotifyIndexerChanged();
        }

        protected void NotifyIndexerChanged()
        {
            OnPropertyChanged("Item");
        }

        #endregion

        #region private fields

        private readonly Dictionary<string, DataValueItem> _dataValueItemsDictionary =
            new();

        private readonly List<MultiBinding> _multiBindings = new();

        #endregion
    }
}