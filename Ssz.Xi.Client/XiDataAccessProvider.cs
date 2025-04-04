﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using Ssz.Utils.Logging;
using Ssz.Xi.Client.Api;
using Xi.Contracts.Constants;
using Xi.Contracts.Data;

namespace Ssz.Xi.Client
{
    public partial class XiDataAccessProvider : DataAccessProviderBase, IDataAccessProvider
    {
        #region construction and destruction

        public XiDataAccessProvider(ILogger<XiDataAccessProvider> logger, IUserFriendlyLogger? userFriendlyLogger = null) :
            base(new LoggersSet<XiDataAccessProvider>(logger, userFriendlyLogger))
        {
            _xiDataListItemsManager = new XiDataListItemsManager();
            _xiDataJournalListItemsManager = new XiDataJournalListItemsManager();
            _xiEventListItemsManager = new XiEventListItemsManager(this);
        }

        #endregion

        #region public functions

        public override DateTime LastFailedConnectionDateTimeUtc => _lastFailedConnectionDateTimeUtc;

        public override DateTime LastSuccessfulConnectionDateTimeUtc => _lastSuccessfulConnectionDateTimeUtc;

        /// <summary>
        ///     Is called using сallbackDoer, see Initialize(..).
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
            CaseInsensitiveDictionary<string?> contextParams,
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

            //string pollIntervalMsString =
            //    ConfigurationManager.AppSettings["PollIntervalMs"];
            //if (!String.IsNullOrWhiteSpace(pollIntervalMsString) &&
            //    Int32.TryParse(pollIntervalMsString, out int pollIntervalMs))
            //{
            //    _pollIntervalMs = pollIntervalMs;
            //}
            _pollIntervalMs = 1000;

            lock (ConstItemsDictionary)
            {
                ConstItemsDictionary.Clear();
                if (ElementIdsMap is not null)
                {
                    foreach (var values in ElementIdsMap.Map.Values)
                    {
                        if (values.Count >= 2 && values.Skip(2).All(v => String.IsNullOrEmpty(v)))
                        {
                            var constAny = ElementIdsMap.TryGetConstValue(values[1]);
                            if (constAny.HasValue)
                            {
                                ConstItemsDictionary[values[0]!] = new ConstItem { Value = constAny.Value };
                            }
                        }
                    }
                }
            }

            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;

            var taskCompletionSource = new TaskCompletionSource<int>();
            var workingThread = new Thread(async () =>
            {
                await WorkingTaskMainAsync(cancellationToken);
                taskCompletionSource.SetResult(0);
            });
            _workingTask = taskCompletionSource.Task;
            workingThread.Start();

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
                valueSubscriptionObj.ChildValueSubscriptionsList = null;
                valueSubscriptionObj.Converter = null;
                valueSubscriptionObj.MapValues = null;
            }

            if (_cancellationTokenSource is not null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource = null;
            }

            if (_workingTask is not null)
            {
                await _workingTask;
                _workingTask = null;
            }

            await base.CloseAsync();
        }        

        /// <summary>        
        ///     Returns id actully used for OPC subscription, always as original id.
        ///     valueSubscription.Update() is called using сallbackDoer, see Initialize(..).        
        /// </summary>
        /// <param name="elementId"></param>
        /// <param name="valueSubscription"></param>
        public override void AddItem(string? elementId, IValueSubscription valueSubscription)
        {
            //Logger?.LogDebug("XiDataProvider.AddItem() " + elementId);

            if (elementId is null || elementId == @"")
            {
                var callbackDispatcher = CallbackDispatcher;
                if (callbackDispatcher is not null)
                    try
                    {
                        callbackDispatcher.BeginInvoke(ct =>
                        {                            
                            valueSubscription.Update(new ValueStatusTimestamp { StatusCode = StatusCodes.BadNodeIdUnknown });

                            RaiseValueSubscriptionsUpdated();
                        });
                    }
                    catch (Exception)
                    {
                    }

                return;
            }
            else
            {
                var valueSubscriptionObj = new ValueSubscriptionObj(elementId, valueSubscription);
                _valueSubscriptionsCollection.Add(valueSubscription, valueSubscriptionObj);

                if (IsInitialized)
                {
                    valueSubscription.Update(
                        AddItem(valueSubscriptionObj));
                }
            }            
        }

        /// <summary>        
        ///     If valueSubscription is not subscribed - does nothing.
        /// </summary>
        /// <param name="valueSubscription"></param>
        public override void RemoveItem(IValueSubscription valueSubscription)
        {
#if NETSTANDARD2_0
            _valueSubscriptionsCollection.TryGetValue(valueSubscription, out ValueSubscriptionObj? valueSubscriptionObj);
            if (valueSubscriptionObj is null)
                return;
            _valueSubscriptionsCollection.Remove(valueSubscription);
#else
            if (!_valueSubscriptionsCollection.Remove(valueSubscription, out ValueSubscriptionObj? valueSubscriptionObj))
                return;
#endif

            if (IsInitialized)
            {
                RemoveItem(valueSubscriptionObj);
            }
        }

        /// <summary>                
        ///     If call to server failed returns null, otherwise returns changed ValueSubscriptions.        
        /// </summary>
        public override async Task<IValueSubscription[]?> PollElementValuesChangesAsync()
        {
            var taskCompletionSource = new TaskCompletionSource<IValueSubscription[]?>();
            WorkingThreadSafeDispatcher.BeginInvoke(ct =>
            {
                try
                {
                    if (_xiServerProxy is null) throw new InvalidOperationException();
                    _xiDataListItemsManager.Subscribe(_xiServerProxy, CallbackDispatcher,
                        XiDataListItemsManagerOnElementValuesCallback, Options.ElementValueListCallbackIsEnabled, Options.UnsubscribeValueListItemsFromServer, ct);
                    object[]? changedValueSubscriptions = _xiDataListItemsManager.PollChanges();
                    taskCompletionSource.SetResult(changedValueSubscriptions is not null ? changedValueSubscriptions.OfType<IValueSubscription>().ToArray() : null);
                }
                catch (Exception ex) 
                {
                    taskCompletionSource.TrySetException(ex);
                }                
            });
            return await taskCompletionSource.Task;
        }

        /// <summary>
        ///     Returns ResultInfo.
        /// </summary>
        /// <param name="valueSubscription"></param>
        /// <param name="valueStatusTimestamp"></param>
        /// <param name="userFriendlyLogger"></param>
        public override Task<ResultInfo> WriteAsync(IValueSubscription valueSubscription, ValueStatusTimestamp valueStatusTimestamp)
        {
            //BeginInvoke(ct =>
            //{
            //    if (_xiServerProxy is null) throw new InvalidOperationException();
            //    _xiDataListItemsManager.Subscribe(_xiServerProxy, CallbackDispatcher,
            //        XiDataListItemsManagerOnElementValuesCallback, true, ct);
            //    _xiDataListItemsManager.Write(valueSubscription, valueStatusTimestamp);
            //});

            var callbackDispatcher = CallbackDispatcher;
            if (!IsInitialized || callbackDispatcher is null) 
                return Task.FromResult(ResultInfo.UncertainResultInfo);

            if (!StatusCodes.IsGood(valueStatusTimestamp.StatusCode))
                return Task.FromResult(ResultInfo.UncertainResultInfo);
            var value = valueStatusTimestamp.Value;

            if (!_valueSubscriptionsCollection.TryGetValue(valueSubscription, out ValueSubscriptionObj? valueSubscriptionObj))
                return Task.FromResult(ResultInfo.UncertainResultInfo);

            if (LoggersSet.UserFriendlyLogger.IsEnabled(LogLevel.Information))
                LoggersSet.UserFriendlyLogger.LogInformation("UI TAG: \"" + valueSubscriptionObj.ElementId + "\"; Value from UI: \"" +
                                             value + "\"");

            IValueSubscription[]? constItemValueSubscriptionsArray = null;
            lock (ConstItemsDictionary)
            {
                var constItem = ConstItemsDictionary.TryGetValue(valueSubscriptionObj.ElementId);
                if (constItem is not null)
                {
                    constItem.Value = value;
                    constItemValueSubscriptionsArray = constItem.Subscribers.ToArray();
                }
            }

            if (constItemValueSubscriptionsArray is not null)
            {
                try
                {
                    callbackDispatcher.BeginInvoke(ct =>
                    {
                        foreach (var constItemValueSubscription in constItemValueSubscriptionsArray)
                            constItemValueSubscription.Update(valueStatusTimestamp);
                    });
                }
                catch (Exception)
                {
                }

                return Task.FromResult(ResultInfo.GoodResultInfo);
            }

            object?[]? resultValues = null;
            if (valueSubscriptionObj.ChildValueSubscriptionsList is not null)
            {
                SszConverter converter = valueSubscriptionObj.Converter ?? SszConverter.Empty;
                resultValues =
                    converter.ConvertBack(value.ValueAsObject(),
                        valueSubscriptionObj.ChildValueSubscriptionsList.Count, 
                        null,
                        LoggersSet);
                if (resultValues.Length == 0)
                    return Task.FromResult(ResultInfo.GoodResultInfo);
            }

            var utcNow = DateTime.UtcNow;

            if (LoggersSet.UserFriendlyLogger.IsEnabled(LogLevel.Information))
            {
                if (valueSubscriptionObj.ChildValueSubscriptionsList is not null)
                {
                    if (resultValues is null) throw new InvalidOperationException();
                    for (var i = 0; i < resultValues.Length; i++)
                    {
                        var resultValue = resultValues[i];
                        if (resultValue != SszConverter.DoNothing)
                            LoggersSet.UserFriendlyLogger.LogInformation("Model TAG: \"" +
                                                         valueSubscriptionObj.ChildValueSubscriptionsList[i]
                                                             .ElementId + "\"; Write Value to Model: \"" +
                                                         new Any(resultValue) + "\"");
                    }
                }
                else
                {
                    if (value.ValueAsObject() != SszConverter.DoNothing)
                        LoggersSet.UserFriendlyLogger.LogInformation("Model TAG: \"" +
                            (valueSubscriptionObj.MapValues is not null ? valueSubscriptionObj.MapValues[1] : valueSubscriptionObj.ElementId) +
                                                     "\"; Write Value to Model: \"" + value + "\"");
                }
            }

            WorkingThreadSafeDispatcher.BeginInvoke(ct =>
            {
                _xiDataListItemsManager.Subscribe(_xiServerProxy!, CallbackDispatcher,
                    XiDataListItemsManagerOnElementValuesCallback, Options.ElementValueListCallbackIsEnabled, Options.UnsubscribeValueListItemsFromServer, ct);

                if (valueSubscriptionObj.ChildValueSubscriptionsList is not null)
                {
                    if (resultValues is null) throw new InvalidOperationException();
                    for (var i = 0; i < resultValues.Length; i++)
                    {
                        var resultValue = resultValues[i];
                        if (resultValue != SszConverter.DoNothing)
                            _xiDataListItemsManager.Write(valueSubscriptionObj.ChildValueSubscriptionsList[i],
                                new ValueStatusTimestamp(new Any(resultValue), StatusCodes.Good, DateTime.UtcNow));
                    }
                }
                else
                {
                    if (value.ValueAsObject() != SszConverter.DoNothing)
                        _xiDataListItemsManager.Write(valueSubscription, valueStatusTimestamp);
                }
            });

            return Task.FromResult(ResultInfo.GoodResultInfo);
        }

        /// <summary>     
        ///     No values mapping and conversion.       
        ///     Returns failed ValueSubscriptions and ResultInfos.
        ///     If connection error, all ValueSubscriptions are failed.      
        /// </summary>
        /// <param name="valueSubscriptions"></param>
        /// <param name="valueStatusTimestamps"></param>
        /// <returns></returns>
        public override async Task<(IValueSubscription[], ResultInfo[])> WriteAsync(IValueSubscription[] valueSubscriptions, ValueStatusTimestamp[] valueStatusTimestamps)
        {
            var taskCompletionSource = new TaskCompletionSource<(IValueSubscription[], ResultInfo[])>();

            WorkingThreadSafeDispatcher.BeginInvoke(ct =>
            {
                try
                {
                    if (_xiServerProxy is null) throw new InvalidOperationException();
                    _xiDataListItemsManager.Subscribe(_xiServerProxy, CallbackDispatcher,
                        XiDataListItemsManagerOnElementValuesCallback, Options.ElementValueListCallbackIsEnabled, Options.UnsubscribeValueListItemsFromServer, ct);
                    object[] failedValueSubscriptions = _xiDataListItemsManager.Write(valueSubscriptions, valueStatusTimestamps);

                    LoggersSet.Logger.LogDebug("XiDataAccessProvider.WriteAsync " +
                        valueSubscriptions.FirstOrDefault()?.ToString() + "; " +
                        valueStatusTimestamps.FirstOrDefault().ToString() + 
                        "; valueSubscriptions.Count: " + valueSubscriptions +
                        "; failedValueSubscriptions.Count: " + failedValueSubscriptions.Length);

                    taskCompletionSource.SetResult((failedValueSubscriptions.OfType<IValueSubscription>().ToArray(), Enumerable.Repeat(ResultInfo.UncertainResultInfo, failedValueSubscriptions.Length).ToArray()));
                }
                catch (Exception ex)
                {
                    LoggersSet.Logger.LogDebug(ex, "XiDataAccessProvider.WriteAsync Exception");

                    taskCompletionSource.TrySetException(ex);
                }                
            });

            return await taskCompletionSource.Task;
        }

        /// <summary>
        ///     Throws if any errors.
        /// </summary>
        /// <param name="recipientPath"></param>
        /// <param name="passthroughName"></param>
        /// <param name="dataToSend"></param>
        /// <returns></returns>
        public override async Task<ReadOnlyMemory<byte>> PassthroughAsync(string recipientPath, string passthroughName, ReadOnlyMemory<byte> dataToSend)
        {
            // Early exception
            if (!_xiServerProxy!.ContextExists)
                throw new XiServerNotExistException();

            var taskCompletionSource = new TaskCompletionSource<ReadOnlyMemory<byte>>();
            WorkingThreadSafeDispatcher.BeginInvoke(ct =>
            {                
                try
                {
                    if (_xiServerProxy is null) throw new InvalidOperationException();
                    PassthroughResult? passthroughResult = _xiServerProxy.Passthrough(recipientPath, passthroughName,
                        dataToSend);
                    if (passthroughResult is not null && passthroughResult.ResultCode == 0) // SUCCESS
                    {
                        taskCompletionSource.SetResult(passthroughResult.ReturnData ?? new byte[0]);
                    }
                    else
                    {
                        throw new Exception(@"ResultCode = " + passthroughResult?.ResultCode);
                    }
                }
                catch (Exception ex)
                {
                    taskCompletionSource.TrySetException(ex);
                }
            });
            return await taskCompletionSource.Task;
        }

        /// <summary>
        ///     Returns StatusCode <see cref="StatusCodes"/>
        ///     No throws.
        /// </summary>
        /// <param name="recipientId"></param>
        /// <param name="passthroughName"></param>
        /// <param name="dataToSend"></param>
        /// <param name="progressCallbackAction"></param>
        /// <returns></returns>
        public override async Task<Task<uint>> LongrunningPassthroughAsync(string recipientId, string passthroughName, ReadOnlyMemory<byte> dataToSend, 
            Action<LongrunningPassthroughCallback>? progressCallbackAction)
        {
            // Early exception
            if (!_xiServerProxy!.ContextExists)
            {
                IDispatcher? сallbackDispatcher = CallbackDispatcher;
                Action<Ssz.Utils.DataAccess.LongrunningPassthroughCallback>? callbackActionDispatched;
                if (progressCallbackAction is not null && сallbackDispatcher is not null)
                {
                    callbackActionDispatched = a =>
                    {
                        try
                        {
                            сallbackDispatcher.BeginInvoke(ct => progressCallbackAction(a));
                        }
                        catch (Exception)
                        {
                        }
                    };
                }
                else
                {
                    callbackActionDispatched = null;
                }

                if (callbackActionDispatched is not null)
                {
                    callbackActionDispatched(new Utils.DataAccess.LongrunningPassthroughCallback
                    {
                        StatusCode = StatusCodes.Uncertain
                    });
                }
                return Task.FromResult(StatusCodes.Uncertain);
            }

            var taskCompletionSource = new TaskCompletionSource<Task<uint>>();

            WorkingThreadSafeDispatcher.BeginInvoke(async ct =>
            {                
                IDispatcher? сallbackDispatcher = CallbackDispatcher;
                Action<Ssz.Utils.DataAccess.LongrunningPassthroughCallback>? callbackActionDispatched;
                if (progressCallbackAction is not null && сallbackDispatcher is not null)
                {
                    callbackActionDispatched = a =>
                    {
                        try
                        {
                            сallbackDispatcher.BeginInvoke(ct => progressCallbackAction(a));
                        }
                        catch (Exception)
                        {
                        }
                    };
                }
                else
                {
                    callbackActionDispatched = null;
                }
                Task<uint> statusCodeTask;
                try
                {
                    if (_xiServerProxy is null) throw new InvalidOperationException();

                    statusCodeTask = await _xiServerProxy.LongrunningPassthroughAsync(recipientId, passthroughName,
                        dataToSend, callbackActionDispatched);
                }
                catch
                {
                    if (callbackActionDispatched is not null)
                    {
                        callbackActionDispatched(new Utils.DataAccess.LongrunningPassthroughCallback
                        {
                            StatusCode = StatusCodes.Uncertain
                        });
                    }
                    statusCodeTask = Task.FromResult(StatusCodes.Uncertain);
                }

                taskCompletionSource.SetResult(statusCodeTask);
            });

            return await taskCompletionSource.Task;
        }

        #endregion

        #region protected functions

        /// <summary>
        ///     Dispacther for working thread.
        /// </summary>
        protected ThreadSafeDispatcher WorkingThreadSafeDispatcher { get; } = new();        

        protected CaseInsensitiveDictionary<ConstItem> ConstItemsDictionary { get; } = new();

        protected void RaiseValueSubscriptionsUpdated()
        {            
            DataGuid = Guid.NewGuid();

            ValueSubscriptionsUpdated(this, EventArgs.Empty);
        }

        #endregion

        #region private functions

        private async Task WorkingTaskMainAsync(CancellationToken cancellationToken)
        {
            _xiServerProxy = new XiServerProxy();
            
            bool eventListCallbackIsEnabled = Options.EventListCallbackIsEnabled;            

            if (eventListCallbackIsEnabled)
                _xiEventListItemsManager.EventMessagesCallback += OnXiEventListItemsManager_EventMessagesCallback;

            while (true)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Delay(3, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();

                    await DoWorkAsync(DateTime.UtcNow, cancellationToken);
                }
                catch when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    LoggersSet.Logger.LogWarning(ex, @"ServerContext Callback Thread Exception");                    
                }
            }            

            Unsubscribe(true);

            _xiServerProxy.Dispose();
            _xiServerProxy = null;
            if (eventListCallbackIsEnabled)
                _xiEventListItemsManager.EventMessagesCallback -= OnXiEventListItemsManager_EventMessagesCallback;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nowUtc"></param>
        /// <param name="cancellationToken"></param>
        private async Task DoWorkAsync(DateTime nowUtc, CancellationToken cancellationToken)
        {
            if (_xiServerProxy is null) 
                throw new InvalidOperationException();            

            await WorkingThreadSafeDispatcher.InvokeActionsInQueueAsync(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            if (!_xiServerProxy.ContextExists)
            {
                IDispatcher? сallbackDispatcher;
                if (IsConnectedEventWaitHandle.WaitOne(0))
                {
                    Unsubscribe(false);

                    #region notify subscribers disconnected

                    //Logger.Info("XiDataProvider diconnected");                    

                    IEnumerable<IValueSubscription> valueSubscriptions =
                        _xiDataListItemsManager.GetAllClientObjs().OfType<IValueSubscription>();

                    сallbackDispatcher = CallbackDispatcher;
                    if (сallbackDispatcher is not null)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        try
                        {
                            сallbackDispatcher.BeginInvoke(ct =>
                            {
                                foreach (IValueSubscription valueSubscription in valueSubscriptions)
                                {                                    
                                    valueSubscription.Update(new ValueStatusTimestamp { StatusCode = StatusCodes.Uncertain });
                                }

                                RaiseValueSubscriptionsUpdated();
                            });
                        }
                        catch (Exception)
                        {
                        }
                    }

                    #endregion                    
                }                

                if (IsInitialized && !String.IsNullOrWhiteSpace(ServerAddress) &&
                    nowUtc > LastFailedConnectionDateTimeUtc + TimeSpan.FromSeconds(5))
                {
                    try
                    {
                        string workstationName = ClientWorkstationName;
#if NETSTANDARD2_0
                        var dictionary = new CaseInsensitiveDictionary<string?>(ContextParams.Count);
                        foreach (var kvp in ContextParams)
                            dictionary.Add(kvp.Key, kvp.Value);                        
                        string xiContextParamsString =
                            NameValueCollectionHelper.GetNameValueCollectionString(dictionary);
#else
                        string xiContextParamsString =
                            NameValueCollectionHelper.GetNameValueCollectionString(ContextParams);
#endif

                        if (!String.IsNullOrEmpty(xiContextParamsString))
                        {
                            workstationName += @"?" + xiContextParamsString;
                        }

                        //Logger?.LogDebug("Connecting. Endpoint: {0}. ApplicationName: {1}. WorkstationName: {2}",
                        //    _serverAddress,
                        //    _applicationName,
                         //   workstationName);

                        _xiServerProxy.InitiateXiContext(ServerAddress, ClientApplicationName,
                            workstationName, WorkingThreadSafeDispatcher);

                        //Logger?.LogDebug("End Connecting");

                        //Logger.Info("XiDataProvider connected to " + _serverAddress);
                        
                        сallbackDispatcher = CallbackDispatcher;
                        if (сallbackDispatcher is not null)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            try
                            {
                                сallbackDispatcher.BeginInvoke(ct =>
                                {
                                    IsConnected = true;                                    
                                });
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                    catch
                    {
                        //Logger?.LogDebug(ex);

                        _lastFailedConnectionDateTimeUtc = nowUtc;
                    }
                }
            }

            if (!IsInitialized) 
                return;

            cancellationToken.ThrowIfCancellationRequested();

            _xiDataListItemsManager.Subscribe(_xiServerProxy, CallbackDispatcher,
                XiDataListItemsManagerOnElementValuesCallback, Options.ElementValueListCallbackIsEnabled, Options.UnsubscribeValueListItemsFromServer, cancellationToken);
            _xiEventListItemsManager.Subscribe(_xiServerProxy, CallbackDispatcher, true, cancellationToken);

            if (!IsInitialized) 
                return;

            cancellationToken.ThrowIfCancellationRequested();

            if (_xiServerProxy.ContextExists)
            {
                _lastSuccessfulConnectionDateTimeUtc = nowUtc;
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    _xiServerProxy.KeepContextAlive(nowUtc);
                    
                    var timeDiffInMs = (uint) (nowUtc - _pollLastCallUtc).TotalMilliseconds;
                    bool pollExpired = timeDiffInMs >= _pollIntervalMs;

                    if (pollExpired)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (Options.ElementValueListCallbackIsEnabled)
                            _xiDataListItemsManager.PollChangesIfNotCallbackable();

                        _xiEventListItemsManager.PollChangesIfNotCallbackable();

                        _pollLastCallUtc = nowUtc;
                    }
                }
                catch
                {
                }
            }            
        }

        /// <summary>
        ///     Working thread.
        /// </summary>
        private void Unsubscribe(bool clearClientSubscriptions)
        {
            var сallbackDispatcher = CallbackDispatcher;
            if (сallbackDispatcher is not null)
            {
                try
                {
                    сallbackDispatcher.BeginInvoke(ct =>
                    {
                        IsConnected = false;                        
                    });
                }
                catch (Exception)
                {
                }
            }

            if (_xiServerProxy is null) throw new InvalidOperationException();
            _xiServerProxy.ConcludeXiContext();

            _xiDataListItemsManager.Unsubscribe(clearClientSubscriptions);
            _xiEventListItemsManager.Unsubscribe();
            _xiDataJournalListItemsManager.Unsubscribe(clearClientSubscriptions);
        }

        /// <summary>
        ///     Called using сallbackDoer.
        /// </summary>
        /// <param name="changedClientObjs"></param>
        /// <param name="changedValues"></param>
        private void XiDataListItemsManagerOnElementValuesCallback(object[] changedClientObjs,
            ValueStatusTimestamp[] changedValues)
        {
            if (changedClientObjs is not null)
            {
                for (int i = 0; i < changedClientObjs.Length; i++)
                {
                    var changedValueSubscription = (IValueSubscription) changedClientObjs[i];                    
                    changedValueSubscription.Update(changedValues[i]);                    
                }                
            }

            RaiseValueSubscriptionsUpdated();
        }

        /// <summary>
        ///     Preconditions: must be Initialized.
        ///     Returns MappedElementIdOrConst
        /// </summary>
        /// <param name="valueSubscriptionObj"></param>
        /// <returns></returns>
        private string AddItem(ValueSubscriptionObj valueSubscriptionObj)
        {
            //string elementId = valueSubscriptionObj.ElementId;
            //IValueSubscription valueSubscription = valueSubscriptionObj.ValueSubscription;

            //BeginInvoke(ct => _xiDataListItemsManager.AddItem(elementId, valueSubscription));

            //return elementId;

            string elementId = valueSubscriptionObj.ElementId;

            var callbackDispatcher = CallbackDispatcher;
            if (callbackDispatcher is null)
                return elementId;

            IValueSubscription valueSubscription = valueSubscriptionObj.ValueSubscription;

            if (ElementIdsMap is not null)
            {
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

                            RaiseValueSubscriptionsUpdated();
                        });
                    }
                    catch (Exception)
                    {
                    }

                    return constAny.Value.ValueAsString(false);
                }

                valueSubscriptionObj.MapValues = ElementIdsMap.GetFromMap(elementId);

                if (valueSubscriptionObj.MapValues is not null)
                {
                    var childValueSubscriptionsList = new List<ChildValueSubscription>();
                    SszConverter? converter = null;

                    for (var i = 1; i < valueSubscriptionObj.MapValues.Count; i++)
                    {
                        string v = valueSubscriptionObj.MapValues[i] ?? "";

                        int index;
                        if ((StringHelper.StartsWithIgnoreCase(v, @"READCONVERTER") ||
                             StringHelper.StartsWithIgnoreCase(v, @"WRITECONVERTER"))
                            && (index = v.IndexOf('=')) > 0)
                        {
                            if (converter is null)
                                converter = new SszConverter();

                            var values = v.Substring(index + 1).Split(new[] { "->" }, StringSplitOptions.None);
                            switch (v.Substring(0, index).Trim().ToUpperInvariant())
                            {
                                case @"READCONVERTER":
                                    if (values.Length == 1)
                                        converter.Statements.Add(new SszStatement(@"true", values[0].Trim(), 0));
                                    else // values.Length > 1
                                        converter.Statements.Add(new SszStatement(values[0].Trim(),
                                            values[1].Trim(), 0));
                                    break;
                                case @"WRITECONVERTER":
                                    if (values.Length == 1)
                                        converter.BackStatements.Add(new SszStatement(@"true", values[0].Trim(), 0));
                                    else if (values.Length == 2)
                                        converter.BackStatements.Add(new SszStatement(values[0].Trim(),
                                            values[1].Trim(), 0));
                                    else // values.Length > 2
                                        converter.BackStatements.Add(new SszStatement(
                                            values[0].Trim(),
                                            values[1].Trim(),
                                            new Any(values[2].Trim()).ValueAsInt32(false)));
                                    break;
                            }
                        }
                        else
                        {
                            if (converter is not null)
                                continue;

                            if (!String.IsNullOrEmpty(v))
                            {
                                var childValueSubscription = new ChildValueSubscription(valueSubscriptionObj, v);
                                childValueSubscriptionsList.Add(childValueSubscription);
                            }
                        }
                    }

                    if (converter is not null && converter.Statements.Count == 0 && converter.BackStatements.Count == 0)
                    {
                        converter = null;
                    }
                    //else
                    //{
                    //    converter.ParentItem = DsProject.Instance;
                    //    converter.ReplaceConstants(DsProject.Instance);
                    //}

                    if (childValueSubscriptionsList.Count > 1 || converter is not null)
                    {
                        valueSubscriptionObj.ChildValueSubscriptionsList = childValueSubscriptionsList;
                        valueSubscriptionObj.Converter = converter;
                        try
                        {
                            callbackDispatcher.BeginInvoke(ct => valueSubscriptionObj.ChildValueSubscriptionUpdated());
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }

            if (valueSubscriptionObj.MapValues is not null)
            {
                WorkingThreadSafeDispatcher.BeginInvoke(ct =>
                {
                    if (valueSubscriptionObj.ChildValueSubscriptionsList is not null)
                    {
                        foreach (var childValueSubscription in valueSubscriptionObj.ChildValueSubscriptionsList)
                            if (!childValueSubscription.IsConst)
                                _xiDataListItemsManager.AddItem(childValueSubscription.ElementId,
                                    childValueSubscription);
                    }
                    else
                    {
                        _xiDataListItemsManager.AddItem(valueSubscriptionObj.MapValues[1] ?? "", valueSubscription);
                    }
                });

                return valueSubscriptionObj.MapValues[1] ?? "";
            }
            else
            {
                WorkingThreadSafeDispatcher.BeginInvoke(ct =>
                {
                    _xiDataListItemsManager.AddItem(elementId, valueSubscription);
                });

                return elementId;
            }
        }

        /// <summary>
        ///     Preconditions: must be Initialized.
        /// </summary>
        /// <param name="valueSubscriptionObj"></param>
        private void RemoveItem(ValueSubscriptionObj valueSubscriptionObj)
        {
            //var valueSubscription = valueSubscriptionObj.ValueSubscription;

            //BeginInvoke(ct => _xiDataListItemsManager.RemoveItem(valueSubscription));

            var valueSubscription = valueSubscriptionObj.ValueSubscription;

            var constAny = ElementIdsMap.TryGetConstValue(valueSubscriptionObj.ElementId);
            if (constAny.HasValue) return;

            var disposable = valueSubscriptionObj.Converter as IDisposable;
            if (disposable is not null) disposable.Dispose();

            lock (ConstItemsDictionary)
            {
                var constItem = ConstItemsDictionary.TryGetValue(valueSubscriptionObj.ElementId);
                if (constItem is not null)
                {
                    constItem.Subscribers.Remove(valueSubscription);
                    return;
                }
            }

            WorkingThreadSafeDispatcher.BeginInvoke(ct =>
            {
                if (valueSubscriptionObj.ChildValueSubscriptionsList is not null)
                {
                    foreach (var childValueSubscription in valueSubscriptionObj.ChildValueSubscriptionsList)
                    {
                        if (!childValueSubscription.IsConst)
                            _xiDataListItemsManager.RemoveItem(childValueSubscription);
                        childValueSubscription.ParentValueSubscriptionObj = null;
                    }

                    valueSubscriptionObj.ChildValueSubscriptionsList = null;
                }
                else
                {
                    _xiDataListItemsManager.RemoveItem(valueSubscription);
                }
            });
        }

        #endregion

        #region private fields

        private DateTime _lastFailedConnectionDateTimeUtc;

        private DateTime _lastSuccessfulConnectionDateTimeUtc;

        private Task? _workingTask;

        private CancellationTokenSource? _cancellationTokenSource;

        private XiServerProxy? _xiServerProxy;

        private readonly XiDataListItemsManager _xiDataListItemsManager;                

        private DateTime _pollLastCallUtc = DateTime.MinValue;

        private int _pollIntervalMs = 1000; 
        
        private Dictionary<IValueSubscription, ValueSubscriptionObj> _valueSubscriptionsCollection =
            new(ReferenceEqualityComparer<object>.Default);

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

            public readonly IValueSubscription ValueSubscription;

            public List<ChildValueSubscription>? ChildValueSubscriptionsList;

            public SszConverter? Converter;

            /// <summary>
            ///     null or Count > 1
            /// </summary>
            public List<string?>? MapValues;

            public void ChildValueSubscriptionUpdated()
            {
                if (ChildValueSubscriptionsList is null) return;

                if (ChildValueSubscriptionsList.Any(vs => StatusCodes.IsBad(vs.ValueStatusTimestamp.StatusCode)))
                {
                    ValueSubscription.Update(new ValueStatusTimestamp { StatusCode = StatusCodes.Bad });
                    return;
                }
                if (ChildValueSubscriptionsList.Any(vs => StatusCodes.IsUncertain(vs.ValueStatusTimestamp.StatusCode)))
                {
                    ValueSubscription.Update(new ValueStatusTimestamp { StatusCode = StatusCodes.Uncertain });
                    return;
                }

                var values = new List<object?>();
                foreach (var childValueSubscription in ChildValueSubscriptionsList)
                    values.Add(childValueSubscription.ValueStatusTimestamp.Value.ValueAsObject());
                SszConverter converter = Converter ?? SszConverter.Empty;
                var convertedValue = converter.Convert(values.ToArray(), null, Ssz.Utils.Logging.LoggersSet.Empty);
                if (convertedValue == SszConverter.DoNothing) return;
                ValueSubscription.Update(new ValueStatusTimestamp(new Any(convertedValue), StatusCodes.Good,
                    DateTime.UtcNow));
            }
        }

        private class ChildValueSubscription : IValueSubscription
        {
            public ChildValueSubscription(ValueSubscriptionObj parentValueSubscriptionObj, string elementId)
            {
                ParentValueSubscriptionObj = parentValueSubscriptionObj;
                ElementId = elementId;

                var constAny = ElementIdsMap.TryGetConstValue(elementId);
                if (constAny.HasValue)
                {
                    ValueStatusTimestamp = new ValueStatusTimestamp(constAny.Value);
                    IsConst = true;
                }
            }

            public ValueSubscriptionObj? ParentValueSubscriptionObj;

            public string ElementId { get; }

            public void Update(string mappedElementIdOrConst)
            {
            }

            public ValueStatusTimestamp ValueStatusTimestamp = new ValueStatusTimestamp { StatusCode = StatusCodes.Uncertain };

            public readonly bool IsConst;            

            public void Update(ValueStatusTimestamp valueStatusTimestamp)
            {
                ValueStatusTimestamp = valueStatusTimestamp;

                ParentValueSubscriptionObj?.ChildValueSubscriptionUpdated();
            }
        }
    }
}