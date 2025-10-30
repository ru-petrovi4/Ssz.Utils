using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Threading;
using Ssz.Utils;
using Microsoft.Extensions.Logging;
using Ssz.Utils.DataAccess;
using System.Globalization;
using System.Threading.Tasks;
using Ssz.Utils.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics.SymbolStore;
using Microsoft.Extensions.Configuration;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Ssz.Utils.DataAccess
{
    public partial class WrapperDataAccessProvider : DataAccessProviderBase, IDataAccessProvider
    {
        #region construction and destruction
        
        public WrapperDataAccessProvider(ILogger logger) :
            base(new LoggersSet(logger, null))
        {
            DataAccessProvidersObservableCollection = new();            
            DataAccessProvidersObservableCollection.CollectionChanged += DataAccessProvidersObservableCollection_OnCollectionChanged;
        }        

        #endregion

        #region public functions

        public ObservableCollection<IDataAccessProvider> DataAccessProvidersObservableCollection { get; }

        public override DateTime LastFailedConnectionDateTimeUtc => DateTime.MinValue;

        public override DateTime LastSuccessfulConnectionDateTimeUtc => DateTime.UtcNow;        

        /// <summary>
        ///     Is called using сallbackDispatcher, see Initialize(..).        
        /// </summary>
        public override event EventHandler ValueSubscriptionsUpdated = delegate { };

        /// <summary>
        ///     You can set DataAccessProviderOptions.ElementValueListCallbackIsEnabled = false and invoke PollElementValuesChangesAsync(...) manually.
        /// </summary>
        /// <param name="elementIdsMap"></param>
        /// <param name="serverAddress"></param>
        /// <param name="clientApplicationName"></param>
        /// <param name="clientWorkstationName"></param>
        /// <param name="systemNameToConnect"></param>
        /// <param name="contextParams"></param>
        /// <param name="options"></param>
        /// <param name="callbackDispatcher"></param>
        public override void Initialize(ElementIdsMap? elementIdsMap,            
            string serverAddress,
            string clientApplicationName,
            string clientWorkstationName,
            string systemNameToConnect,
            CaseInsensitiveOrderedDictionary<string?> contextParams,
            DataAccessProviderOptions options,
            IDispatcher? callbackDispatcher)
        {
            base.Initialize(elementIdsMap,                
                serverAddress,
                clientApplicationName,
                clientWorkstationName,
                systemNameToConnect, 
                contextParams,
                options,
                callbackDispatcher);            

            lock (ConstItemsDictionary)
            {
                ConstItemsDictionary.Clear();                
            }            

            foreach (ValueSubscriptionObj valueSubscriptionObj in _valueSubscriptionsCollection.Values)
            {
                valueSubscriptionObj.ValueSubscription.Update(
                    AddItem(valueSubscriptionObj));
            }
        }        

        /// <summary>
        ///     Tou can call DisposeAsync() instead of this method.
        ///     Closes WITH waiting working thread exit.
        /// </summary>
        public override async Task CloseAsync()
        {
            if (!IsInitialized)
                return;            

            foreach (ValueSubscriptionObj valueSubscriptionObj in _valueSubscriptionsCollection.Values)
            {
                if (valueSubscriptionObj.ChildValueSubscriptionsList is null)
                    continue;

                foreach (ChildValueSubscription childValueSubscription in valueSubscriptionObj.ChildValueSubscriptionsList)
                {
                    childValueSubscription.Dispose();
                }

                valueSubscriptionObj.ChildValueSubscriptionsList = null;                
            }

            await base.CloseAsync();
        }

        /// <summary>        
        ///     Returns id actully used for OPC subscription, always as original id.
        ///     valueSubscription.Update() is called using сallbackDispatcher, see Initialize(..).        
        /// </summary>
        /// <param name="elementId"></param>
        /// <param name="valueSubscription"></param>
        public override void AddItem(string? elementId, IValueSubscription valueSubscription)
        {
            LoggersSet.Logger.LogDebug("WrapperDataAccessProvider.AddItem() " + elementId);

            if (elementId == null || elementId == @"")
            {
                var callbackDispatcher = CallbackDispatcher;
                if (callbackDispatcher is not null)
                    try
                    {
                        callbackDispatcher.BeginInvoke(ct =>
                        {                            
                            valueSubscription.Update(new ValueStatusTimestamp { StatusCode = StatusCodes.BadNodeIdUnknown });
                        });
                    }
                    catch (Exception)
                    {
                    }

                return;
            }

            var valueSubscriptionObj = new ValueSubscriptionObj(elementId, valueSubscription);           
            _valueSubscriptionsCollection.Add(valueSubscription, valueSubscriptionObj);

            if (IsInitialized)
            {
                valueSubscription.Update(
                    AddItem(valueSubscriptionObj));                
            }
        }

        /// <summary>        
        ///     If valueSubscription is not subscribed - does nothing.
        /// </summary>
        /// <param name="valueSubscription"></param>
        public override void RemoveItem(IValueSubscription valueSubscription)
        {
#if NET5_0_OR_GREATER
            if (!_valueSubscriptionsCollection.Remove(valueSubscription, out ValueSubscriptionObj? valueSubscriptionObj))
                return;            
#else
            _valueSubscriptionsCollection.TryGetValue(valueSubscription, out ValueSubscriptionObj? valueSubscriptionObj);
            if (valueSubscriptionObj is null)
                return;
            _valueSubscriptionsCollection.Remove(valueSubscription);
#endif

            if (IsInitialized)
            {
                RemoveItem(valueSubscriptionObj);
            }
        }        

        ///// <summary>        
        /////     Writes to userFriendlyLogger with Information level.
        ///// </summary>
        ///// <param name="valueSubscription"></param>
        ///// <param name="valueStatusTimestamp"></param>
        ///// <param name="userFriendlyLogger"></param>
        ///// <exception cref="InvalidOperationException"></exception>
        //public override async Task<ResultInfo> WriteAsync(IValueSubscription valueSubscription, ValueStatusTimestamp valueStatusTimestamp, ILogger? userFriendlyLogger)
        //{
        //    var callbackDispatcher = CallbackDispatcher;
        //    if (!IsInitialized || callbackDispatcher is null) 
        //        return new ResultInfo { StatusCode = StatusCodes.BadInvalidState };
            
        //    var value = valueStatusTimestamp.Value;

        //    if (!_valueSubscriptionsCollection.TryGetValue(valueSubscription, out ValueSubscriptionObj? valueSubscriptionObj))
        //        return new ResultInfo { StatusCode = StatusCodes.BadInvalidArgument };

        //    if (userFriendlyLogger is not null && userFriendlyLogger.IsEnabled(LogLevel.Information))
        //        userFriendlyLogger.LogInformation("UI TAG: \"" + valueSubscriptionObj.ElementId + "\"; Value from UI: \"" +
        //                                     value + "\"");

        //    IValueSubscription[]? constItemValueSubscriptionsArray = null;
        //    lock (ConstItemsDictionary)
        //    {
        //        var constItem = ConstItemsDictionary.TryGetValue(valueSubscriptionObj.ElementId);
        //        if (constItem is not null)
        //        {
        //            constItem.Value = value;
        //            constItemValueSubscriptionsArray = constItem.Subscribers.ToArray();
        //        }
        //    }

        //    if (constItemValueSubscriptionsArray is not null)
        //    {
        //        try
        //        {
        //            callbackDispatcher.BeginInvoke(ct =>
        //            {
        //                foreach (var constItemValueSubscription in constItemValueSubscriptionsArray)
        //                    constItemValueSubscription.Update(valueStatusTimestamp);
        //            });
        //        }
        //        catch (Exception)
        //        {
        //        }

        //        return ResultInfo.GoodResultInfo;
        //    }

        //    object?[]? resultValues = null;
        //    if (valueSubscriptionObj.ChildValueSubscriptionsList is not null)
        //    {
        //        SszConverter converter = valueSubscriptionObj.Converter ?? SszConverter.Empty;                
        //        resultValues =
        //            converter.ConvertBack(value.ValueAsObject(),
        //                valueSubscriptionObj.ChildValueSubscriptionsList.Count, null, userFriendlyLogger);
        //        if (resultValues.Length == 0)
        //            return ResultInfo.GoodResultInfo;
        //    }

        //    var utcNow = DateTime.UtcNow;

            

        //    var taskCompletionSource = new TaskCompletionSource<ResultInfo>();

        //    WorkingThreadSafeDispatcher.BeginInvokeEx(async ct =>
        //    {
        //        var resultInfo = ResultInfo.GoodResultInfo;

        //        if (!IsInitialized)
        //        {
        //            taskCompletionSource.SetResult(resultInfo);
        //            return;
        //        }

        //        await _clientElementValueListManager.SubscribeAsync(_clientContextManager, CallbackDispatcher,
        //            OnElementValuesCallback, Options.UnsubscribeValueListItemsFromServer, Options.ElementValueListCallbackIsEnabled, ct);                

        //        if (valueSubscriptionObj.ChildValueSubscriptionsList is not null)
        //        {
        //            if (resultValues is null) 
        //                throw new InvalidOperationException();
        //            for (var i = 0; i < resultValues.Length; i++)
        //            {
        //                var resultValue = resultValues[i];
        //                if (resultValue != SszConverter.DoNothing)
        //                    resultInfo = await _clientElementValueListManager.WriteAsync(valueSubscriptionObj.ChildValueSubscriptionsList[i],
        //                        new ValueStatusTimestamp(new Any(resultValue), StatusCodes.Good, DateTime.UtcNow));
        //            }
        //        }
        //        else
        //        {
        //            resultInfo = await _clientElementValueListManager.WriteAsync(valueSubscription, valueStatusTimestamp);
        //        }

        //        taskCompletionSource.SetResult(resultInfo);
        //    });

        //    return await taskCompletionSource.Task;
        //}

        ///// <summary>     
        /////     No values mapping and conversion.       
        /////     Returns failed ValueSubscriptions and ResultInfos.
        /////     If connection error, all ValueSubscriptions are failed.   
        ///// </summary>
        ///// <param name="valueSubscriptions"></param>
        ///// <param name="valueStatusTimestamps"></param>
        ///// <returns></returns>
        //public override async Task<(IValueSubscription[], ResultInfo[])> WriteAsync(IValueSubscription[] valueSubscriptions, ValueStatusTimestamp[] valueStatusTimestamps)
        //{
        //    var taskCompletionSource = new TaskCompletionSource<(IValueSubscription[], ResultInfo[])>();

        //    WorkingThreadSafeDispatcher.BeginInvokeEx(async ct =>
        //    {
        //        if (!IsInitialized)
        //        {
        //            var failedResultInfo = new ResultInfo { StatusCode = StatusCodes.BadInvalidState };
        //            taskCompletionSource.SetResult((valueSubscriptions, Enumerable.Repeat(failedResultInfo, valueSubscriptions.Length).ToArray()));
        //            return;
        //        }

        //        await _clientElementValueListManager.SubscribeAsync(_clientContextManager, CallbackDispatcher,
        //            OnElementValuesCallback, Options.UnsubscribeValueListItemsFromServer, Options.ElementValueListCallbackIsEnabled, ct);
        //        (object[] failedValueSubscriptions, ResultInfo[] failedResultInfos) = await _clientElementValueListManager.WriteAsync(valueSubscriptions, valueStatusTimestamps);

        //        taskCompletionSource.SetResult((failedValueSubscriptions.OfType<IValueSubscription>().ToArray(), failedResultInfos));
        //    }
        //    );

        //    return await taskCompletionSource.Task;
        //}        

        #endregion

        #region protected functions        

        /// <summary>
        ///     This dictionary is created, because we can write to const values.
        /// </summary>
        protected CaseInsensitiveOrderedDictionary<ConstItem> ConstItemsDictionary { get; } = new();

        protected DateTime LastValueSubscriptionsUpdatedDateTimeUtc { get; private set; } = DateTime.MinValue; 

        /// <summary>
        ///     Unused
        /// </summary>
        protected void RaiseValueSubscriptionsUpdated()
        {
            LastValueSubscriptionsUpdatedDateTimeUtc = DateTime.UtcNow;

            ValueSubscriptionsUpdated(this, EventArgs.Empty);
        }

        #endregion

        #region private functions  

        private void DataAccessProvidersObservableCollection_OnCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems is not null)
                        foreach (var dataAccessProvider in e.NewItems.OfType<IDataAccessProvider>())
                        {
                            OnAdded(dataAccessProvider);
                        }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems is not null)
                        foreach (var dataAccessProvider in e.OldItems.OfType<IDataAccessProvider>())
                        {
                            OnRemoved(dataAccessProvider);
                        }
                    break;

                case NotifyCollectionChangedAction.Replace:
                    break;

                case NotifyCollectionChangedAction.Reset:
                    break;
            }
        }

        private void OnAdded(IDataAccessProvider dataAccessProvider)
        {
            if (Options.ElementValueListCallbackIsEnabled)
            {
                foreach (ValueSubscriptionObj valueSubscriptionObj in _valueSubscriptionsCollection.Values)
                {
                    if (valueSubscriptionObj.ChildValueSubscriptionsList is null)
                        continue;

                    valueSubscriptionObj.ChildValueSubscriptionsList.Add(new ChildValueSubscription(valueSubscriptionObj, dataAccessProvider, valueSubscriptionObj.ElementId));

                    valueSubscriptionObj.ChildValueSubscriptionUpdated();
                }
            }

            if (Options.EventListCallbackIsEnabled)
            {
                dataAccessProvider.EventMessagesCallback += DataAccessProvider_OnEventMessagesCallback;
            }
        }

        private void OnRemoved(IDataAccessProvider dataAccessProvider)
        {
            if (Options.ElementValueListCallbackIsEnabled)
            {
                foreach (ValueSubscriptionObj valueSubscriptionObj in _valueSubscriptionsCollection.Values)
                {
                    if (valueSubscriptionObj.ChildValueSubscriptionsList is null)
                        continue;

                    var valueSubscription = valueSubscriptionObj.ChildValueSubscriptionsList.FirstOrDefault(vs => ReferenceEquals(vs.DataAccessProvider, dataAccessProvider));
                    if (valueSubscription is not null)
                    {
                        valueSubscriptionObj.ChildValueSubscriptionsList.Remove(valueSubscription);
                        valueSubscription.Dispose();

                        valueSubscriptionObj.ChildValueSubscriptionUpdated();
                    }
                }
            }

            if (Options.EventListCallbackIsEnabled)
            {
                dataAccessProvider.EventMessagesCallback -= DataAccessProvider_OnEventMessagesCallback;
            }
        }

        /// <summary>
        ///     Preconditions: must be Initialized.
        ///     Returns MappedElementIdOrConst
        /// </summary>
        /// <param name="valueSubscriptionObj"></param>
        /// <returns></returns>
        private string AddItem(ValueSubscriptionObj valueSubscriptionObj)
        {
            string elementId = valueSubscriptionObj.ElementId;

            var callbackDispatcher = CallbackDispatcher;
            if (callbackDispatcher is null)
                return elementId;
            
            IValueSubscription valueSubscription = valueSubscriptionObj.ValueSubscription;

            var constAny = ElementIdsMap.TryGetConstValue(elementId);
            if (!constAny.HasValue)
            {
                lock (ConstItemsDictionary)
                {
                    var constItem = ConstItemsDictionary.TryGetValue(elementId);
                    if (constItem is not null)
                    {
                        constItem.Subscribers.Add(valueSubscription);
                        constAny = constItem.Value;
                    }
                }
            }
            if (constAny.HasValue)
            {
                try
                {
                    callbackDispatcher.BeginInvoke(ct =>
                    {
                        valueSubscription.Update(new ValueStatusTimestamp(constAny.Value, StatusCodes.Good,
                            DateTime.UtcNow));
                    });
                }
                catch (Exception)
                {
                }

                return constAny.Value.ValueAsString(false);
            }

            List<ChildValueSubscription> childValueSubscriptionsList = new(DataAccessProvidersObservableCollection.Count);
            foreach (var dataAccessProvider in DataAccessProvidersObservableCollection)
            {
                childValueSubscriptionsList.Add(new ChildValueSubscription(valueSubscriptionObj, dataAccessProvider, elementId));
            }
            valueSubscriptionObj.ChildValueSubscriptionsList = childValueSubscriptionsList;

            return elementId;
        }

        /// <summary>
        ///     Preconditions: must be Initialized.
        /// </summary>
        /// <param name="valueSubscriptionObj"></param>
        private void RemoveItem(ValueSubscriptionObj valueSubscriptionObj)
        {   
            var valueSubscription = valueSubscriptionObj.ValueSubscription;

            var constAny = ElementIdsMap.TryGetConstValue(valueSubscriptionObj.ElementId);
            if (constAny.HasValue) 
                return;            

            lock (ConstItemsDictionary)
            {
                var constItem = ConstItemsDictionary.TryGetValue(valueSubscriptionObj.ElementId);
                if (constItem is not null)
                {
                    constItem.Subscribers.Remove(valueSubscription);
                    return;
                }
            }

            if (valueSubscriptionObj.ChildValueSubscriptionsList is not null)
            {
                foreach (var childValueSubscription in valueSubscriptionObj.ChildValueSubscriptionsList)
                {
                    childValueSubscription.Dispose();
                }

                valueSubscriptionObj.ChildValueSubscriptionsList = null;
            }
        }        

        #endregion

        #region private fields                

        private Dictionary<IValueSubscription, ValueSubscriptionObj> _valueSubscriptionsCollection =
            new(ReferenceEqualityComparer<IValueSubscription>.Default);             

        #endregion

        protected class ConstItem
        {
            public readonly HashSet<IValueSubscription> Subscribers = new(ReferenceEqualityComparer<object>.Default);

            public Any Value;
        }        

        private class ValueSubscriptionObj
        {
            #region construction and destruction

            public ValueSubscriptionObj(string elementId, IValueSubscription valueSubscription)
            {
                ElementId = elementId;
                ValueSubscription = valueSubscription;
            }

            #endregion

            public readonly string ElementId;

            /// <summary>
            ///     Original ValueSubscription
            /// </summary>
            public readonly IValueSubscription ValueSubscription;

            public List<ChildValueSubscription>? ChildValueSubscriptionsList;            

            ///// <summary>
            /////     null or Count > 1
            ///// </summary>
            //public List<string?>? MapValues;

            public void ChildValueSubscriptionUpdated()
            {
                if (ChildValueSubscriptionsList is null) 
                    return;

                if (ChildValueSubscriptionsList.All(vs => StatusCodes.IsBad(vs.ValueStatusTimestamp.StatusCode)))
                {
                    ValueSubscription.Update(new ValueStatusTimestamp { StatusCode = StatusCodes.Bad });                    
                }
                else
                {
                    var goodValueSubscription = ChildValueSubscriptionsList.FirstOrDefault(vs => StatusCodes.IsGood(vs.ValueStatusTimestamp.StatusCode));
                    if (goodValueSubscription is not null)
                        ValueSubscription.Update(goodValueSubscription.ValueStatusTimestamp);
                    else
                        ValueSubscription.Update(new ValueStatusTimestamp { StatusCode = StatusCodes.Uncertain });
                }                
            }
        }

        private class ChildValueSubscription : IValueSubscription, IDisposable
        {
            public ChildValueSubscription(ValueSubscriptionObj parentValueSubscriptionObj, IDataAccessProvider dataAccessProvider, string elementId)
            {
                ParentValueSubscriptionObj = parentValueSubscriptionObj;
                DataAccessProvider = dataAccessProvider;
                ElementId = elementId;

                DataAccessProvider.AddItem(ElementId, this);
            }

            public void Dispose()
            {
                DataAccessProvider.RemoveItem(this);
            }

            public ValueSubscriptionObj? ParentValueSubscriptionObj;

            public IDataAccessProvider DataAccessProvider { get; }

            public string ElementId { get; private set; }

            public void Update(string mappedElementIdOrConst)
            {
            }            

            public ValueStatusTimestamp ValueStatusTimestamp = new ValueStatusTimestamp { StatusCode = StatusCodes.Uncertain };                    

            public void Update(ValueStatusTimestamp valueStatusTimestamp)
            {
                ValueStatusTimestamp = valueStatusTimestamp;

                ParentValueSubscriptionObj?.ChildValueSubscriptionUpdated();
            }
        }
    }
}