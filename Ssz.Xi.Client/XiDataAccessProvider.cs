using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using Ssz.Xi.Client.Api;
using Xi.Contracts.Constants;
using Xi.Contracts.Data;

namespace Ssz.Xi.Client
{
    public partial class XiDataAccessProvider : DisposableViewModelBase, IDataAccessProvider
    {
        #region construction and destruction

        public XiDataAccessProvider(ILogger<XiDataAccessProvider> logger, IDispatcher? callbackDispatcher)
        {
            Logger = logger;
            CallbackDispatcher = callbackDispatcher;
        }

        /// <summary>
        ///     This method is invoked when the IDisposable.Dispose or Finalize actions are
        ///     requested.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
            {
                Close();
            }

            base.Dispose(disposing);
        }

        protected override async ValueTask DisposeAsyncCore()
        {
            if (Disposed) return;

            await CloseAsync();

            await base.DisposeAsyncCore();
        }

        #endregion

        #region public functions

        public ILogger<XiDataAccessProvider> Logger { get; }

        /// <summary>
        ///     Xi Server connection string.
        /// </summary>
        public string ServerAddress
        {
            get
            {
                if (!IsInitialized) throw new Exception("Not Initialized");
                return _serverAddress;
            }
        }

        /// <summary>
        ///     Xi System Name.
        /// </summary>
        public string SystemNameToConnect
        {
            get
            {
                if (!IsInitialized) throw new Exception("Not Initialized");
                return _systemNameToConnect;
            }
        }

        /// <summary>
        ///     Used in Xi Context initialization.
        /// </summary>
        public string ClientApplicationName
        {
            get
            {
                if (!IsInitialized) throw new Exception("Not Initialized");
                return _clientApplicationName;
            }
        }

        /// <summary>
        ///     Used in Xi Context initialization.
        /// </summary>
        public string ClientWorkstationName
        {
            get
            {
                if (!IsInitialized) throw new Exception("Not Initialized");
                return _clientWorkstationName;
            }
        }

        /// <summary>
        ///     Used in Xi Context initialization.
        ///     Can be null
        /// </summary>
        public CaseInsensitiveDictionary<string> ContextParams
        {
            get
            {
                if (!IsInitialized) throw new Exception("Not Initialized");
                return _contextParams;
            }
        }

        public string ContextId
        {
            get
            {
                if (!IsInitialized) throw new Exception("Not Initialized");
                return _xiServerProxy!.ContextId;
            }
        }

        public bool IsInitialized
        {
            get { return _isInitialized; }
            private set { SetValue(ref _isInitialized, value); }
        }

        public bool IsConnected
        {
            get { return _isConnected; }
            private set { SetValue(ref _isConnected, value); }
        }

        public bool IsDisconnected
        {
            get { return _isDisconnected; }
            private set { SetValue(ref _isDisconnected, value); }
        }

        public EventWaitHandle IsConnectedEventWaitHandle => _isConnectedEventWaitHandle;

        public DateTime LastFailedConnectionDateTimeUtc => _lastFailedConnectionDateTimeUtc;

        public DateTime LastSuccessfulConnectionDateTimeUtc => _lastSuccessfulConnectionDateTimeUtc;

        /// <summary>
        ///     If guid the same, the data is guaranteed not to have changed.
        /// </summary>
        public Guid DataGuid { get; private set; }

        public ElementIdsMap? ElementIdsMap { get; private set; }

        public object? Obj { get; set; }

        /// <summary>
        ///     Is called using сallbackDoer, see Initialize(..).
        /// </summary>
        public event Action ValueSubscriptionsUpdated = delegate { };

        /// <summary>
        ///     You can set updateValueItems = false and invoke PollElementValuesChangesAsync(...) manually.
        /// </summary>
        /// <param name="elementIdsMap"></param>
        /// <param name="elementValueListCallbackIsEnabled"></param>
        /// <param name="eventListCallbackIsEnabled"></param>
        /// <param name="serverAddress"></param>
        /// <param name="clientApplicationName"></param>
        /// <param name="clientWorkstationName"></param>
        /// <param name="systemNameToConnect"></param>
        /// <param name="contextParams"></param>
        public void Initialize(ElementIdsMap? elementIdsMap,
            bool elementValueListCallbackIsEnabled,
            bool eventListCallbackIsEnabled,
            string serverAddress,
            string clientApplicationName, string clientWorkstationName, string systemNameToConnect, CaseInsensitiveDictionary<string> contextParams)
        {
            Close();

            //Logger?.LogDebug("Starting ModelDataProvider. сallbackDoer is not null " + (сallbackDispatcher is not null).ToString());

            ElementIdsMap = elementIdsMap;
            _elementValueListCallbackIsEnabled = elementValueListCallbackIsEnabled;
            _eventListCallbackIsEnabled = eventListCallbackIsEnabled;
            _serverAddress = serverAddress;
            _systemNameToConnect = systemNameToConnect;
            _xiDataListItemsManager.XiSystem = _systemNameToConnect;
            _xiEventListItemsManager.XiSystem = _systemNameToConnect;
            _xiDataJournalListItemsManager.XiSystem = _systemNameToConnect;
            _clientApplicationName = clientApplicationName;            
            _clientWorkstationName = clientWorkstationName;            
            _contextParams = contextParams;

            _lastSuccessfulConnectionDateTimeUtc = DateTime.UtcNow;

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
                        if (values.Count >= 2 && !values[0]!.Contains(ElementIdsMap.GenericTag)
                            && values.Skip(2).All(v => String.IsNullOrEmpty(v)))
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

            var previousWorkingTask = _workingTask;
            _workingTask = Task.Factory.StartNew(() =>
            {
                if (previousWorkingTask is not null)
                    previousWorkingTask.Wait();
                WorkingTaskMainAsync(cancellationToken).Wait();
            }, TaskCreationOptions.LongRunning);

            IsInitialized = true;

            foreach (ValueSubscriptionObj valueSubscriptionObj in _valueSubscriptionsCollection.Values)
            {
                valueSubscriptionObj.ValueSubscription.MappedElementIdOrConst = AddItem(valueSubscriptionObj);
            }
        }

        /// <summary>
        ///     Tou can call Dispose() instead of this method.
        ///     Closes without waiting working thread exit.
        /// </summary>
        public void Close()
        {
            if (!IsInitialized) return;

            IsInitialized = false;            

            _contextParams = new CaseInsensitiveDictionary<string>();
            ElementIdsMap = null;

            if (_cancellationTokenSource is not null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource = null;
            }
        }

        /// <summary>
        ///     Tou can call DisposeAsync() instead of this method.
        ///     Closes WITH waiting working thread exit.
        /// </summary>
        public async Task CloseAsync()
        {
            Close();

            if (_workingTask is not null)
                await _workingTask;
        }

        /// <summary>
        ///     Re-initializes this object with same settings.
        ///     Items must be added again.
        ///     If not initialized then does nothing.
        /// </summary>
        public void ReInitialize()
        {
            if (!IsInitialized) return;

            Initialize(ElementIdsMap,
                _elementValueListCallbackIsEnabled,
                _eventListCallbackIsEnabled,
                _serverAddress,
                _clientApplicationName, _clientWorkstationName, _systemNameToConnect, _contextParams);
        }

        /// <summary>        
        ///     Returns id actully used for OPC subscription, always as original id.
        ///     valueSubscription.Update() is called using сallbackDoer, see Initialize(..).        
        /// </summary>
        /// <param name="elementId"></param>
        /// <param name="valueSubscription"></param>
        public void AddItem(string? elementId, IValueSubscription valueSubscription)
        {
            //Logger?.LogDebug("XiDataProvider.AddItem() " + elementId);

            if (elementId is null || elementId == @"")
            {
                var callbackDispatcher = CallbackDispatcher;
                if (callbackDispatcher is not null)
                    try
                    {
                        callbackDispatcher.BeginInvoke(ct =>
                            valueSubscription.Update(new ValueStatusTimestamp { ValueStatusCode = ValueStatusCode.ItemDoesNotExist }));
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
                valueSubscription.MappedElementIdOrConst = AddItem(valueSubscriptionObj);
            }
        }

        /// <summary>        
        ///     If valueSubscription is not subscribed - does nothing.
        /// </summary>
        /// <param name="valueSubscription"></param>
        public void RemoveItem(IValueSubscription valueSubscription)
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
        public async Task<IValueSubscription[]?> PollElementValuesChangesAsync()
        {
            var taskCompletionSource = new TaskCompletionSource<IValueSubscription[]?>();
            ThreadSafeDispatcher.BeginInvoke(ct =>
            {
                if (_xiServerProxy is null) throw new InvalidOperationException();
                _xiDataListItemsManager.Subscribe(_xiServerProxy, CallbackDispatcher,
                    XiDataListItemsManagerOnElementValuesCallback, true, ct);
                object[]? changedValueSubscriptions = _xiDataListItemsManager.PollChanges();
                taskCompletionSource.SetResult(changedValueSubscriptions is not null ? changedValueSubscriptions.OfType<IValueSubscription>().ToArray() : null);
            });
            return await taskCompletionSource.Task;
        }        

        /// <summary>
        /// 
        /// </summary>
        /// <param name="valueSubscription"></param>
        /// <param name="valueStatusTimestamp"></param>
        /// <param name="userFriendlyLogger"></param>
        public void Write(IValueSubscription valueSubscription, ValueStatusTimestamp valueStatusTimestamp, ILogger? userFriendlyLogger)
        {
            //BeginInvoke(ct =>
            //{
            //    if (_xiServerProxy is null) throw new InvalidOperationException();
            //    _xiDataListItemsManager.Subscribe(_xiServerProxy, CallbackDispatcher,
            //        XiDataListItemsManagerOnElementValuesCallback, true, ct);
            //    _xiDataListItemsManager.Write(valueSubscription, valueStatusTimestamp);
            //});

            var callbackDispatcher = CallbackDispatcher;
            if (!IsInitialized || callbackDispatcher is null) return;

            if (!ValueStatusCode.IsGood(valueStatusTimestamp.ValueStatusCode)) return;
            var value = valueStatusTimestamp.Value;

            if (!_valueSubscriptionsCollection.TryGetValue(valueSubscription, out ValueSubscriptionObj? valueSubscriptionObj))
                return;

            if (userFriendlyLogger is not null && userFriendlyLogger.IsEnabled(LogLevel.Information))
                userFriendlyLogger.LogInformation("UI TAG: \"" + valueSubscriptionObj.ElementId + "\"; Value from UI: \"" +
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

                return;
            }

            object?[]? resultValues = null;
            if (valueSubscriptionObj.ChildValueSubscriptionsList is not null)
            {
                SszConverter converter = valueSubscriptionObj.Converter ?? SszConverter.Empty;
                resultValues =
                    converter.ConvertBack(value.ValueAsObject(),
                        valueSubscriptionObj.ChildValueSubscriptionsList.Count, null, userFriendlyLogger);
                if (resultValues.Length == 0) return;
            }

            var utcNow = DateTime.UtcNow;

            if (userFriendlyLogger is not null && userFriendlyLogger.IsEnabled(LogLevel.Information))
            {
                if (valueSubscriptionObj.ChildValueSubscriptionsList is not null)
                {
                    if (resultValues is null) throw new InvalidOperationException();
                    for (var i = 0; i < resultValues.Length; i++)
                    {
                        var resultValue = resultValues[i];
                        if (resultValue != SszConverter.DoNothing)
                            userFriendlyLogger.LogInformation("Model TAG: \"" +
                                                         valueSubscriptionObj.ChildValueSubscriptionsList[i]
                                                             .MappedElementIdOrConst + "\"; Write Value to Model: \"" +
                                                         new Any(resultValue) + "\"");
                    }
                }
                else
                {
                    if (value.ValueAsObject() != SszConverter.DoNothing)
                        userFriendlyLogger.LogInformation("Model TAG: \"" +
                            (valueSubscriptionObj.MapValues is not null ? valueSubscriptionObj.MapValues[1] : valueSubscriptionObj.ElementId) +
                                                     "\"; Write Value to Model: \"" + value + "\"");
                }
            }

            ThreadSafeDispatcher.BeginInvoke(ct =>
            {
                _xiDataListItemsManager.Subscribe(_xiServerProxy!, CallbackDispatcher,
                    XiDataListItemsManagerOnElementValuesCallback, true, ct);

                if (valueSubscriptionObj.ChildValueSubscriptionsList is not null)
                {
                    if (resultValues is null) throw new InvalidOperationException();
                    for (var i = 0; i < resultValues.Length; i++)
                    {
                        var resultValue = resultValues[i];
                        if (resultValue != SszConverter.DoNothing)
                            _xiDataListItemsManager.Write(valueSubscriptionObj.ChildValueSubscriptionsList[i],
                                new ValueStatusTimestamp(new Any(resultValue), ValueStatusCode.Good, DateTime.UtcNow));
                    }
                }
                else
                {
                    if (value.ValueAsObject() != SszConverter.DoNothing)
                        _xiDataListItemsManager.Write(valueSubscription,
                            new ValueStatusTimestamp(value, ValueStatusCode.Good, DateTime.UtcNow));
                }
            });
        }

        /// <summary>     
        ///     No values mapping and conversion.       
        ///     returns failed ValueSubscriptions.
        ///     If connection error, failed ValueSubscriptions is all clientObjs.        
        /// </summary>
        /// <param name="valueSubscriptions"></param>
        /// <param name="valueStatusTimestamps"></param>
        /// <returns></returns>
        public virtual async Task<IValueSubscription[]> WriteAsync(IValueSubscription[] valueSubscriptions, ValueStatusTimestamp[] valueStatusTimestamps)
        {
            var taskCompletionSource = new TaskCompletionSource<IValueSubscription[]>();
            ThreadSafeDispatcher.BeginInvoke(ct =>
            {
                if (_xiServerProxy is null) throw new InvalidOperationException();
                _xiDataListItemsManager.Subscribe(_xiServerProxy, CallbackDispatcher,
                    XiDataListItemsManagerOnElementValuesCallback, true, ct);
                object[] failedValueSubscriptions = _xiDataListItemsManager.Write(valueSubscriptions, valueStatusTimestamps);

                taskCompletionSource.SetResult(failedValueSubscriptions.OfType<IValueSubscription>().ToArray());
            });
            return await taskCompletionSource.Task;
        }

        /// <summary>
        ///     Throws if any errors.
        /// </summary>
        /// <param name="recipientId"></param>
        /// <param name="passthroughName"></param>
        /// <param name="dataToSend"></param>
        /// <returns></returns>
        public async Task<IEnumerable<byte>> PassthroughAsync(string recipientId, string passthroughName, byte[] dataToSend)
        {
            var taskCompletionSource = new TaskCompletionSource<IEnumerable<byte>>();
            ThreadSafeDispatcher.BeginInvoke(ct =>
            {                
                try
                {
                    if (_xiServerProxy is null) throw new InvalidOperationException();
                    PassthroughResult? passthroughResult = _xiServerProxy.Passthrough(recipientId, passthroughName,
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
        ///     Returns true if succeeded.
        /// </summary>
        /// <param name="recipientId"></param>
        /// <param name="passthroughName"></param>
        /// <param name="dataToSend"></param>
        /// <param name="progressCallbackAction"></param>
        /// <returns></returns>
        public async Task<bool> LongrunningPassthroughAsync(string recipientId, string passthroughName, byte[]? dataToSend, 
            Action<LongrunningPassthroughCallback>? progressCallbackAction)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            ThreadSafeDispatcher.BeginAsyncInvoke(async ct =>
            {
                bool succeeded;
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

                try
                {
                    if (_xiServerProxy is null) throw new InvalidOperationException();

                    succeeded = await _xiServerProxy.LongrunningPassthroughAsync(recipientId, passthroughName,
                        dataToSend, callbackActionDispatched);
                }
                catch
                {
                    if (callbackActionDispatched is not null)
                    {
                        callbackActionDispatched(new Utils.DataAccess.LongrunningPassthroughCallback
                        {
                            Succeeded = false
                        });
                    }
                    succeeded = false;
                }

                taskCompletionSource.SetResult(succeeded);
            });
            return await taskCompletionSource.Task;
        }

        #endregion

        #region protected functions

        /// <summary>
        ///     Dispatcher for callbacks to client.
        /// </summary>
        protected IDispatcher? CallbackDispatcher { get; }

        /// <summary>
        ///     Dispacther for working thread.
        /// </summary>
        protected ThreadSafeDispatcher ThreadSafeDispatcher { get; } = new();

        protected CaseInsensitiveDictionary<ConstItem> ConstItemsDictionary { get; } = new();

        #endregion

        #region private functions

        private async Task WorkingTaskMainAsync(CancellationToken ct)
        {
            _xiServerProxy = new XiServerProxy();

            if (_eventListCallbackIsEnabled) _xiEventListItemsManager.EventMessagesCallback += OnEventMessagesCallback;

            while (true)
            {
                if (ct.IsCancellationRequested) break;
                await Task.Delay(10);
                if (ct.IsCancellationRequested) break;

                var nowUtc = DateTime.UtcNow;

                DoWork(nowUtc, ct);
            }            

            Unsubscribe(true);

            _xiServerProxy.Dispose();
            _xiServerProxy = null;
            if (_eventListCallbackIsEnabled) _xiEventListItemsManager.EventMessagesCallback -= OnEventMessagesCallback;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nowUtc"></param>
        /// <param name="cancellationToken"></param>
        private void DoWork(DateTime nowUtc, CancellationToken cancellationToken)
        {
            var t = ThreadSafeDispatcher.InvokeActionsInQueue(cancellationToken);

            if (_xiServerProxy is null) throw new InvalidOperationException();

            if (cancellationToken.IsCancellationRequested) return;
            if (!_xiServerProxy.ContextExists)
            {
                IDispatcher? сallbackDispatcher;
                if (_isConnectedEventWaitHandle.WaitOne(0))
                {
                    Unsubscribe(false);

                    #region notify subscribers disconnected

                    //Logger.Info("XiDataProvider diconnected");                    

                    IEnumerable<IValueSubscription> valueSubscriptions =
                        _xiDataListItemsManager.GetAllClientObjs().OfType<IValueSubscription>();

                    сallbackDispatcher = CallbackDispatcher;
                    if (сallbackDispatcher is not null)
                    {
                        if (cancellationToken.IsCancellationRequested) return;
                        try
                        {
                            сallbackDispatcher.BeginInvoke(ct =>
                            {
                                foreach (IValueSubscription valueSubscription in valueSubscriptions)
                                {
                                    valueSubscription.Update(new ValueStatusTimestamp());
                                }
                                DataGuid = Guid.NewGuid();

                                ValueSubscriptionsUpdated();
                            });
                        }
                        catch (Exception)
                        {
                        }
                    }

                    #endregion                    
                }                

                if (!String.IsNullOrWhiteSpace(_serverAddress) &&
                    nowUtc > _lastFailedConnectionDateTimeUtc + TimeSpan.FromSeconds(5))
                {
                    try
                    {
                        string workstationName = _clientWorkstationName;
#if NETSTANDARD2_0
                        var dictionary = new CaseInsensitiveDictionary<string?>(_contextParams.Count);
                        foreach (var kvp in _contextParams)
                            dictionary.Add(kvp.Key, kvp.Value);                        
                        string xiContextParamsString =
                            NameValueCollectionHelper.GetNameValueCollectionString(dictionary);
#else
                        string xiContextParamsString =
                            NameValueCollectionHelper.GetNameValueCollectionString(new CaseInsensitiveDictionary<string?>(_contextParams.Select(kvp => new KeyValuePair<string, string?>(kvp.Key, kvp.Value))));
#endif

                        if (!String.IsNullOrEmpty(xiContextParamsString))
                        {
                            workstationName += @"?" + xiContextParamsString;
                        }

                        //Logger?.LogDebug("Connecting. Endpoint: {0}. ApplicationName: {1}. WorkstationName: {2}",
                        //    _serverAddress,
                        //    _applicationName,
                         //   workstationName);

                        _xiServerProxy.InitiateXiContext(_serverAddress, _clientApplicationName,
                            workstationName, ThreadSafeDispatcher);

                        //Logger?.LogDebug("End Connecting");

                        //Logger.Info("XiDataProvider connected to " + _serverAddress);

                        _isConnectedEventWaitHandle.Set();
                        сallbackDispatcher = CallbackDispatcher;
                        if (сallbackDispatcher is not null)
                        {
                            if (cancellationToken.IsCancellationRequested) return;
                            try
                            {
                                сallbackDispatcher.BeginInvoke(ct =>
                                {
                                    IsConnected = true;
                                    IsDisconnected = false;
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

            if (cancellationToken.IsCancellationRequested) return;
            _xiDataListItemsManager.Subscribe(_xiServerProxy, CallbackDispatcher,
                XiDataListItemsManagerOnElementValuesCallback, _elementValueListCallbackIsEnabled, cancellationToken);
            _xiEventListItemsManager.Subscribe(_xiServerProxy, CallbackDispatcher, true, cancellationToken);

            if (cancellationToken.IsCancellationRequested) return;
            if (_xiServerProxy.ContextExists)
            {
                _lastSuccessfulConnectionDateTimeUtc = nowUtc;
                try
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    _xiServerProxy.KeepContextAlive(nowUtc);
                    
                    var timeDiffInMs = (uint) (nowUtc - _pollLastCallUtc).TotalMilliseconds;
                    bool pollExpired = timeDiffInMs >= _pollIntervalMs;

                    if (pollExpired)
                    {
                        if (cancellationToken.IsCancellationRequested) return;

                        if (_elementValueListCallbackIsEnabled)
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
            _isConnectedEventWaitHandle.Reset();

            var сallbackDispatcher = CallbackDispatcher;
            if (сallbackDispatcher is not null)
            {
                try
                {
                    сallbackDispatcher.BeginInvoke(ct =>
                    {
                        IsConnected = false;
                        IsDisconnected = true;
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
                DataGuid = Guid.NewGuid();
            }

            ValueSubscriptionsUpdated();
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
                if (!constAny.HasValue)
                {
                    valueSubscriptionObj.MapValues = ElementIdsMap.GetFromMap(elementId);

                    if (valueSubscriptionObj.MapValues is not null)
                    {
                        if (valueSubscriptionObj.MapValues.Skip(2).All(v => String.IsNullOrEmpty(v)))
                        {
                            constAny = ElementIdsMap.TryGetConstValue(valueSubscriptionObj.MapValues[1]);
                        }
                        else
                        {
                            var childValueSubscriptionsList = new List<ChildValueSubscription>();
                            var converter = new SszConverter();

                            for (var i = 1; i < valueSubscriptionObj.MapValues.Count; i++)
                            {
                                string v = valueSubscriptionObj.MapValues[i] ?? "";

                                int index;
                                if ((StringHelper.StartsWithIgnoreCase(v, @"READCONVERTER") ||
                                     StringHelper.StartsWithIgnoreCase(v, @"WRITECONVERTER"))
                                    && (index = v.IndexOf('=')) > 0)
                                {
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
                                    var childValueSubscription = new ChildValueSubscription(valueSubscriptionObj, v);
                                    childValueSubscriptionsList.Add(childValueSubscription);
                                }
                            }

                            if (converter.Statements.Count == 0 && converter.BackStatements.Count == 0)
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
                }

                if (constAny.HasValue)
                {
                    try
                    {
                        callbackDispatcher.BeginInvoke(ct =>
                            valueSubscription.Update(new ValueStatusTimestamp(constAny.Value, ValueStatusCode.Good,
                                DateTime.UtcNow)));
                    }
                    catch (Exception)
                    {
                    }

                    return constAny.Value.ValueAsString(false);
                }
            }

            if (valueSubscriptionObj.MapValues is not null)
            {
                ThreadSafeDispatcher.BeginInvoke(ct =>
                {
                    if (valueSubscriptionObj.ChildValueSubscriptionsList is not null)
                    {
                        foreach (var childValueSubscription in valueSubscriptionObj.ChildValueSubscriptionsList)
                            if (!childValueSubscription.IsConst)
                                _xiDataListItemsManager.AddItem(childValueSubscription.MappedElementIdOrConst,
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
                ThreadSafeDispatcher.BeginInvoke(ct =>
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

            ThreadSafeDispatcher.BeginInvoke(ct =>
            {
                if (valueSubscriptionObj.ChildValueSubscriptionsList is not null)
                {
                    foreach (var childValueSubscription in valueSubscriptionObj.ChildValueSubscriptionsList)
                    {
                        if (!childValueSubscription.IsConst)
                            _xiDataListItemsManager.RemoveItem(childValueSubscription);
                        childValueSubscription.ValueSubscriptionObj = null;
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

        private bool _isInitialized;

        private bool _isConnected;

        private bool _isDisconnected = true;        

        private readonly ManualResetEvent _isConnectedEventWaitHandle = new ManualResetEvent(false);

        /// <summary>
        ///     Xi Server connection string.
        /// </summary>
        private string _serverAddress = "";

        /// <summary>
        ///     Xi System Name.
        /// </summary>
        private string _systemNameToConnect = @"";

        /// <summary>
        ///     Used in Xi Context initialization.
        /// </summary>
        private string _clientApplicationName = "";

        /// <summary>
        ///     Used in Xi DataList initialization.
        /// </summary>
        private bool _elementValueListCallbackIsEnabled;

        private bool _eventListCallbackIsEnabled;

        /// <summary>
        ///     Used in Xi Context initialization.
        /// </summary>
        private string _clientWorkstationName = "";

        /// <summary>
        ///     Used in Xi Context initialization.
        /// </summary>
        private CaseInsensitiveDictionary<string> _contextParams = new CaseInsensitiveDictionary<string>();        

        private Task? _workingTask;

        private CancellationTokenSource? _cancellationTokenSource;

        private XiServerProxy? _xiServerProxy;

        private readonly XiDataListItemsManager _xiDataListItemsManager = new XiDataListItemsManager();                

        private DateTime _pollLastCallUtc = DateTime.MinValue;

        private int _pollIntervalMs = 1000; 

        private DateTime _lastFailedConnectionDateTimeUtc = DateTime.MinValue;

        private DateTime _lastSuccessfulConnectionDateTimeUtc = DateTime.MinValue;

        private Dictionary<IValueSubscription, ValueSubscriptionObj> _valueSubscriptionsCollection =
            new Dictionary<IValueSubscription, ValueSubscriptionObj>(ReferenceEqualityComparer<object>.Default);

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

                if (ChildValueSubscriptionsList.Any(vs => vs.ValueStatusTimestamp.ValueStatusCode == ValueStatusCode.ItemDoesNotExist))
                {
                    ValueSubscription.Update(new ValueStatusTimestamp { ValueStatusCode = ValueStatusCode.ItemDoesNotExist });
                    return;
                }
                if (ChildValueSubscriptionsList.Any(vs => vs.ValueStatusTimestamp.ValueStatusCode == ValueStatusCode.Unknown))
                {
                    ValueSubscription.Update(new ValueStatusTimestamp());
                    return;
                }

                var values = new List<object?>();
                foreach (var childValueSubscription in ChildValueSubscriptionsList)
                    values.Add(childValueSubscription.ValueStatusTimestamp.Value.ValueAsObject());
                SszConverter converter = Converter ?? SszConverter.Empty;
                var convertedValue = converter.Convert(values.ToArray(), null, null);
                if (convertedValue == SszConverter.DoNothing) return;
                ValueSubscription.Update(new ValueStatusTimestamp(new Any(convertedValue), ValueStatusCode.Good,
                    DateTime.UtcNow));
            }
        }

        private class ChildValueSubscription : IValueSubscription
        {
            public ChildValueSubscription(ValueSubscriptionObj valueSubscriptionObj, string mappedElementIdOrConst)
            {
                ValueSubscriptionObj = valueSubscriptionObj;
                MappedElementIdOrConst = mappedElementIdOrConst;

                var constAny = ElementIdsMap.TryGetConstValue(mappedElementIdOrConst);
                if (constAny.HasValue)
                {
                    ValueStatusTimestamp = new ValueStatusTimestamp(constAny.Value);
                    IsConst = true;
                }
            }

            public ValueSubscriptionObj? ValueSubscriptionObj;

            public string MappedElementIdOrConst { get; set; }

            public ValueStatusTimestamp ValueStatusTimestamp;

            public readonly bool IsConst;

            public void Update(ValueStatusTimestamp valueStatusTimestamp)
            {
                ValueStatusTimestamp = valueStatusTimestamp;

                ValueSubscriptionObj?.ChildValueSubscriptionUpdated();
            }
        }
    }
}