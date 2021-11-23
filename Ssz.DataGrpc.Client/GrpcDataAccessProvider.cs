using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Threading;
using Ssz.Utils;
using Ssz.DataGrpc.Client.Managers;
using Ssz.DataGrpc.Server;
using Ssz.DataGrpc.Client.Data;
using Microsoft.Extensions.Logging;
using Ssz.Utils.DataAccess;
using Grpc.Net.Client;
using System.Globalization;
using System.Threading.Tasks;
using Grpc.Core;

namespace Ssz.DataGrpc.Client
{
    public partial class GrpcDataAccessProvider : DisposableViewModelBase, IDataAccessProvider, IDispatcher
    {
        #region construction and destruction

        public GrpcDataAccessProvider(ILogger<GrpcDataAccessProvider> logger)
        {
            Logger = logger;

            ClientConnectionManager = new ClientConnectionManager(logger, this);

            ClientElementValueListManager = new ClientElementValueListManager(logger);
            ClientElementValueJournalListManager = new ClientElementValueJournalListManager(logger);
            ClientEventListManager = new ClientEventListManager(logger);
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

        public ILogger<GrpcDataAccessProvider> Logger { get; }

        /// <summary>
        ///     DataGrpc Server connection string.
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
        ///     DataGrpc Systems Names.
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
        ///     Used in DataGrpc Context initialization.
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
        ///     Used in DataGrpc Context initialization.
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
        ///     Used in DataGrpc Context initialization.
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
                return ClientConnectionManager.ServerContextId;
            }
        }

        public GrpcChannel? GrpcChannel
        {
            get { return ClientConnectionManager.GrpcChannel; }
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

        public EventWaitHandle IsConnectedEventWaitHandle { get; } = new ManualResetEvent(false);

        public DateTime LastFailedConnectionDateTimeUtc => _lastFailedConnectionDateTimeUtc;

        public DateTime LastSuccessfulConnectionDateTimeUtc => _lastSuccessfulConnectionDateTimeUtc;

        /// <summary>
        ///     If guid the same, the data is guaranteed not to have changed.
        /// </summary>
        public Guid DataGuid { get; private set; }

        public ElementIdsMap? ElementIdsMap { get; private set; }

        public CaseInsensitiveDictionary<string> BackMapDictionary { get; } = new();

        public object? Obj { get; set; }

        /// <summary>
        ///     Is called using сallbackDispatcher, see Initialize(..).
        /// </summary>
        public event Action ValueSubscriptionsUpdated = delegate { };

        /// <summary>
        ///     You can set updateValueItems = false and invoke PollElementValuesChanges(...) manually.
        /// </summary>
        /// <param name="callbackDispatcher"></param>
        /// <param name="elementValueListCallbackIsEnabled"></param>
        /// <param name="serverAddress"></param>
        /// <param name="clientApplicationName"></param>
        /// <param name="clientWorkstationName"></param>
        /// <param name="systemNames"></param>
        /// <param name="contextParams"></param>
        public virtual void Initialize(IDispatcher? callbackDispatcher,
            ElementIdsMap? elementIdsMap,
            bool elementValueListCallbackIsEnabled,
            bool eventListCallbackIsEnabled,
            string serverAddress,
            string clientApplicationName, string clientWorkstationName, string systemNameToConnect, CaseInsensitiveDictionary<string> contextParams)
        {
            Close();

            Logger.LogDebug("Starting ModelDataProvider. сallbackDispatcher is not null " + (callbackDispatcher is not null).ToString());

            CallbackDispatcher = callbackDispatcher;
            ElementIdsMap = elementIdsMap;
            _elementValueListCallbackIsEnabled = elementValueListCallbackIsEnabled;
            _eventListCallbackIsEnabled = eventListCallbackIsEnabled;
            _serverAddress = serverAddress;
            _clientApplicationName = clientApplicationName;
            _clientWorkstationName = clientWorkstationName;
            _systemNameToConnect = systemNameToConnect;
            _contextParams = contextParams;

            _lastSuccessfulConnectionDateTimeUtc = DateTime.UtcNow;

            //string pollIntervalMsString =
            //    ConfigurationManager.AppSettings["PollIntervalMs"];
            //if (!String.IsNullOrWhiteSpace(pollIntervalMsString) &&
            //    Int32.TryParse(pollIntervalMsString, out int pollIntervalMs))
            //{
            //    _pollIntervalMs = pollIntervalMs;
            //}

            lock (ConstItemsDictionary)
            {
                ConstItemsDictionary.Clear();
                BackMapDictionary.Clear();
                if (ElementIdsMap is not null)
                {
                    foreach (var values in ElementIdsMap.Map.Values)
                    {
                        if (values.Count >= 2)
                        {
                            var constAny = ElementIdsMap.TryGetConstValue(values[1]);
                            if (constAny.HasValue)
                            {
                                ConstItemsDictionary[values[0] ?? ""] = new ConstItem { Value = constAny.Value };
                            }
                            else
                            {
                                BackMapDictionary[values[1] ?? ""] = values[0] ?? "";
                            }
                            continue;
                        }

                        for (int i = 2; i < values.Count; i++)
                        {
                            string v = values[i] ?? "";
                            if ((StringHelper.StartsWithIgnoreCase(v, @"READCONVERTER") ||
                                    StringHelper.StartsWithIgnoreCase(v, @"WRITECONVERTER"))
                                && v.IndexOf('=') > 0) continue;

                            var constAny = ElementIdsMap.TryGetConstValue(v);
                            if (!constAny.HasValue)
                            {
                                BackMapDictionary[v] = values[0] ?? "";
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
        /// </summary>
        public virtual void Close()
        {
            if (!IsInitialized) return;

            IsInitialized = false;

            foreach (ValueSubscriptionObj valueSubscriptionObj in _valueSubscriptionsCollection.Values)
            {
                valueSubscriptionObj.ChildValueSubscriptionsList = null;
                valueSubscriptionObj.Converter = null;
                valueSubscriptionObj.MapValues = ValueSubscriptionObj.EmptyMapValues;
            }

            _contextParams = new CaseInsensitiveDictionary<string>();
            CallbackDispatcher = null;
            ElementIdsMap = null;

            if (_cancellationTokenSource is not null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource = null;
            }            
        }

        /// <summary>
        ///     Tou can call DisposeAsync() instead of this method.
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

            Initialize(CallbackDispatcher,
                ElementIdsMap,
                _elementValueListCallbackIsEnabled,
                _eventListCallbackIsEnabled,
                _serverAddress,
                _clientApplicationName, _clientWorkstationName, _systemNameToConnect, _contextParams);
        }

        /// <summary>        
        ///     Returns id actully used for OPC subscription, always as original id.
        ///     valueSubscription.Update() is called using сallbackDispatcher, see Initialize(..).        
        /// </summary>
        /// <param name="elementId"></param>
        /// <param name="valueSubscription"></param>
        public virtual void AddItem(string elementId, IValueSubscription valueSubscription)
        {
            Logger.LogDebug("DataGrpcProvider.AddItem() " + elementId);

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
        public virtual void RemoveItem(IValueSubscription valueSubscription)
        {
            if (!_valueSubscriptionsCollection.Remove(valueSubscription, out ValueSubscriptionObj? valueSubscriptionObj))
                return;

            if (IsInitialized)
            {
                RemoveItem(valueSubscriptionObj);
            }
        }

        /// <summary>        
        ///     setResultAction(..) is called using сallbackDispatcher, see Initialize(..).
        ///     If call to server failed setResultAction(null) is called, otherwise setResultAction(changedValueSubscriptions) is called.        
        /// </summary>
        public void PollElementValuesChanges(Action<IValueSubscription[]?> setResultAction)
        {
            BeginInvoke(ct =>
            {
                ClientElementValueListManager.Subscribe(ClientConnectionManager, CallbackDispatcher,
                    OnElementValuesCallback, true, ct);
                object[]? changedValueSubscriptions = ClientElementValueListManager.PollChanges();
                IDispatcher? сallbackDispatcher = CallbackDispatcher;
                if (сallbackDispatcher is not null)
                {
                    try
                    {
                        сallbackDispatcher.BeginInvoke(ct => setResultAction(changedValueSubscriptions is not null ? changedValueSubscriptions.OfType<IValueSubscription>().ToArray() : null));
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            );
        }

        /// <summary>     
        ///     No values mapping and conversion.
        ///     setResultAction(..) is called using сallbackDispatcher, see Initialize(..).
        ///     setResultAction(failedValueSubscriptions) is called, failedValueSubscriptions is not null.
        ///     If connection error, failedValueSubscriptions is all clientObjs.        
        /// </summary>
        public virtual void Write(IValueSubscription[] valueSubscriptions, ValueStatusTimestamp[] valueStatusTimestamps, Action<IValueSubscription[]>? setResultAction)
        {
            BeginInvoke(ct =>
            {
                ClientElementValueListManager.Subscribe(ClientConnectionManager, CallbackDispatcher,
                    OnElementValuesCallback, true, ct);
                object[] failedValueSubscriptions = ClientElementValueListManager.Write(valueSubscriptions, valueStatusTimestamps);

                if (setResultAction is not null)
                {
                    IDispatcher? сallbackDispatcher = CallbackDispatcher;
                    if (сallbackDispatcher is not null)
                    {
                        try
                        {
                            сallbackDispatcher.BeginInvoke(ct => setResultAction(failedValueSubscriptions.OfType<IValueSubscription>().ToArray()));
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }
            );
        }

        /// <summary>
        ///     Writes to logger with Debug level.
        /// </summary>
        /// <param name="valueSubscription"></param>
        /// <param name="valueStatusTimestamp"></param>
        /// <param name="alternativeLogger"></param>
        public virtual void Write(IValueSubscription valueSubscription, ValueStatusTimestamp valueStatusTimestamp, ILogger? alternativeLogger)
        {
            var callbackDispatcher = CallbackDispatcher;
            if (!IsInitialized || callbackDispatcher is null) return;

            if (!ValueStatusCode.IsGood(valueStatusTimestamp.ValueStatusCode)) return;
            var value = valueStatusTimestamp.Value;

            if (!_valueSubscriptionsCollection.TryGetValue(valueSubscription, out ValueSubscriptionObj? valueSubscriptionObj))
                return;            

            var logger = alternativeLogger ?? Logger;

            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("UI TAG: \"" + valueSubscriptionObj.ElementId + "\"; Value from UI: \"" +
                                             value + "\"");

            IValueSubscription[]? constItemValueSubscriptionsArray = null;
            lock (ConstItemsDictionary)
            {
                var constItem = ConstItemsDictionary.TryGetValue(valueSubscriptionObj.ElementId);
                if (constItem is not null && constItem.Value.ValueAsObject() != DBNull.Value)
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
                SszConverter converter;
                if (valueSubscriptionObj.Converter is not null)
                    converter = valueSubscriptionObj.Converter;
                else
                    converter = SszConverter.Empty;
                resultValues =
                    converter.ConvertBack(value.ValueAsObject(),
                        valueSubscriptionObj.ChildValueSubscriptionsList.Count, logger);
                if (resultValues.Length == 0) return;
            }

            var utcNow = DateTime.UtcNow;

            if (logger.IsEnabled(LogLevel.Debug))
            {
                if (valueSubscriptionObj.ChildValueSubscriptionsList is not null)
                {
                    if (resultValues is null) throw new InvalidOperationException();
                    for (var i = 0; i < resultValues.Length; i++)
                    {
                        var resultValue = resultValues[i];
                        if (resultValue != SszConverter.DoNothing)
                            logger.LogDebug("Model TAG: \"" +
                                                         valueSubscriptionObj.ChildValueSubscriptionsList[i]
                                                             .MappedElementIdOrConst + "\"; Write Value to Model: \"" +
                                                         new Any(resultValue) + "\"");
                    }
                }
                else
                {
                    if (value.ValueAsObject() != SszConverter.DoNothing)
                        logger.LogDebug("Model TAG: \"" + valueSubscriptionObj.MapValues[1] +
                                                     "\"; Write Value to Model: \"" + value + "\"");
                }
            }

            BeginInvoke(ct =>
            {
                ClientElementValueListManager.Subscribe(ClientConnectionManager, CallbackDispatcher,
                    OnElementValuesCallback, true, ct);

                if (valueSubscriptionObj.ChildValueSubscriptionsList is not null)
                {
                    if (resultValues is null) throw new InvalidOperationException();
                    for (var i = 0; i < resultValues.Length; i++)
                    {
                        var resultValue = resultValues[i];
                        if (resultValue != SszConverter.DoNothing)
                            ClientElementValueListManager.Write(valueSubscriptionObj.ChildValueSubscriptionsList[i],
                                new ValueStatusTimestamp(new Any(resultValue), ValueStatusCode.Good, DateTime.UtcNow));
                    }
                }
                else
                {
                    if (value.ValueAsObject() != SszConverter.DoNothing)
                        ClientElementValueListManager.Write(valueSubscription,
                            new ValueStatusTimestamp(value, ValueStatusCode.Good, DateTime.UtcNow));
                }
            });
        }

        /// <summary>
        ///     Returns null if any errors.
        /// </summary>
        /// <param name="recipientId"></param>
        /// <param name="passthroughName"></param>
        /// <param name="dataToSend"></param>
        /// <returns></returns>
        public async Task<IEnumerable<byte>?> PassthroughAsync(string recipientId, string passthroughName, byte[] dataToSend)
        {
            var taskCompletionSource = new TaskCompletionSource<IEnumerable<byte>?>();
            BeginInvoke(ct =>
            {
                IEnumerable<byte>? result;
                try
                {
                    IEnumerable<byte> returnData;
                    ClientConnectionManager.Passthrough(recipientId, passthroughName,
                        dataToSend, out returnData);
                    result = returnData;
                }
                catch (RpcException ex)
                {
                    Logger.LogError(ex, ex.Status.Detail);
                    result = null;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Passthrough exception.");
                    result = null;
                }

                taskCompletionSource.SetResult(result);
            });
            return await taskCompletionSource.Task;
        }

        /// <summary>
        ///     Returns true if succeeded.
        /// </summary>
        /// <param name="recipientId"></param>
        /// <param name="passthroughName"></param>
        /// <param name="dataToSend"></param>
        /// <param name="callbackAction"></param>
        /// <returns></returns>
        public async Task<bool> LongrunningPassthroughAsync(string recipientId, string passthroughName, byte[]? dataToSend,
            Action<Ssz.Utils.DataAccess.LongrunningPassthroughCallback>? callbackAction)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            BeginInvoke(async ct =>
            {
                IDispatcher? сallbackDispatcher = CallbackDispatcher;
                Action<Ssz.Utils.DataAccess.LongrunningPassthroughCallback>? callbackActionDispatched;
                if (callbackAction is not null && сallbackDispatcher is not null)
                {
                    callbackActionDispatched = a =>
                    {
                        try
                        {
                            сallbackDispatcher.BeginInvoke(ct => callbackAction(a));
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
                bool succeeded;
                try
                {
                    StatusCode statusCode = await ClientConnectionManager.LongrunningPassthroughAsync(recipientId, passthroughName,
                        dataToSend, callbackActionDispatched);
                    succeeded = statusCode == StatusCode.OK;
                }
                catch (RpcException ex)
                {
                    Logger.LogError(ex, ex.Status.Detail);
                    if (callbackActionDispatched is not null)
                    {
                        callbackActionDispatched(new Utils.DataAccess.LongrunningPassthroughCallback
                        {   
                            Succeeded = false
                        });
                    }
                    succeeded = false;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Passthrough exception.");
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

        /// <summary>
        ///     Invokes Action in working thread with cancellation support.
        /// </summary>
        /// <param name="action"></param>
        public void BeginInvoke(Action<CancellationToken> action)
        {
            _threadSafeDispatcher.BeginInvoke(action);
        }

        #endregion

        #region protected functions

        protected IDispatcher? CallbackDispatcher { get; private set; }

        protected CaseInsensitiveDictionary<ConstItem> ConstItemsDictionary { get; } = new();

        protected ClientConnectionManager ClientConnectionManager { get; }

        protected ClientElementValueListManager ClientElementValueListManager { get; }

        protected DateTime LastValueSubscriptionsUpdatedDateTimeUtc { get; private set; } = DateTime.MinValue;

        /// <summary>
        ///     On loop in working thread.
        /// </summary>
        /// <param name="cancellationToken"></param>
        protected virtual void DoWork(DateTime nowUtc, CancellationToken cancellationToken)
        {
            _threadSafeDispatcher.InvokeActionsInQueue(cancellationToken);

            if (cancellationToken.IsCancellationRequested) return;
            if (!ClientConnectionManager.ConnectionExists)
            {
                IDispatcher? сallbackDispatcher;
                if (IsConnectedEventWaitHandle.WaitOne(0))
                {
                    Unsubscribe(false);

                    #region notify subscribers disconnected

                    Logger.LogInformation("DataGrpcProvider diconnected");                    

                    IEnumerable<IValueSubscription> valueSubscriptions =
                        ClientElementValueListManager.GetAllClientObjs().OfType<IValueSubscription>();

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

                                RaiseValueSubscriptionsUpdated();
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
                        //if (Logger.ShouldTrace(System.Diagnostics.TraceEventType.Verbose))
                        //{
                        //    _logger.LogDebug("Connecting. Server Address: {0}. ApplicationName: {1}. WorkstationName: {2}. Params: {3}",
                        //        _serverAddress,
                        //        _applicationName,
                        //        _workstationName,
                        //        //NameValueCollectionHelper.GetNameValueCollectionString(_dataGrpcContextParams.OfType<string?>()));
                        //}


                        ClientConnectionManager.InitiateConnection(_serverAddress, _clientApplicationName,
                            _clientWorkstationName, _systemNameToConnect, _contextParams);

                        Logger.LogDebug("End Connecting");

                        Logger.LogInformation("DataGrpcProvider connected to " + _serverAddress);

                        IsConnectedEventWaitHandle.Set();                        
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
                    catch (Exception ex)
                    {
                        Logger.LogDebug(ex, "");

                        _lastFailedConnectionDateTimeUtc = nowUtc;

                        OnInitiateConnectionException(ex);
                    }
                }
            }

            if (cancellationToken.IsCancellationRequested) return;
            ClientElementValueListManager.Subscribe(ClientConnectionManager, CallbackDispatcher,
                OnElementValuesCallback, _elementValueListCallbackIsEnabled, cancellationToken);
            ClientEventListManager.Subscribe(ClientConnectionManager, CallbackDispatcher, _eventListCallbackIsEnabled, cancellationToken);

            if (cancellationToken.IsCancellationRequested) return;
            if (ClientConnectionManager.ConnectionExists)
            {
                _lastSuccessfulConnectionDateTimeUtc = nowUtc;
                try
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    ClientConnectionManager.DoWork(cancellationToken, nowUtc);
                }
                catch
                {
                }
            }
        }

        protected virtual void OnInitiateConnectionException(Exception ex)
        {            
        }

        /// <summary>
        ///     Working thread.
        /// </summary>
        protected virtual void Unsubscribe(bool clearClientSubscriptions)
        {
            IsConnectedEventWaitHandle.Reset();

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

            ClientConnectionManager.CloseConnection();

            ClientElementValueListManager.Unsubscribe(clearClientSubscriptions);
            ClientEventListManager.Unsubscribe();
            ClientElementValueJournalListManager.Unsubscribe(clearClientSubscriptions);            
        }

        /// <summary>
        ///     Called using сallbackDispatcher.
        /// </summary>
        /// <param name="changedClientObjs"></param>
        /// <param name="changedValues"></param>
        protected void OnElementValuesCallback(object[] changedClientObjs,
            ValueStatusTimestamp[] changedValues)
        {
            for (int i = 0; i < changedClientObjs.Length; i++)
            {
                var changedValueSubscription = (IValueSubscription)changedClientObjs[i];
                changedValueSubscription.Update(changedValues[i]);
            }
            DataGuid = Guid.NewGuid();

            RaiseValueSubscriptionsUpdated();
        }

        protected void RaiseValueSubscriptionsUpdated()
        {
            LastValueSubscriptionsUpdatedDateTimeUtc = DateTime.UtcNow;

            ValueSubscriptionsUpdated();
        }

        protected ConstItem? ConstItemsDictionaryTryGetValue(string? tag, string? propertyPath,
            string? tagType)
        {
            if (ElementIdsMap is null) return null;

            string elementId = tag + propertyPath;
            var constItem = ConstItemsDictionary.TryGetValue(elementId);
            if (constItem is not null) return constItem;

            if (!string.IsNullOrEmpty(propertyPath))
            {
                ConstItem? templateConstItem = null;
                if (!string.IsNullOrEmpty(tagType))
                    templateConstItem = ConstItemsDictionary.TryGetValue(tagType +
                        ElementIdsMap.TagTypeSeparator + ElementIdsMap.GenericTag + propertyPath);
                if (templateConstItem is null)
                    templateConstItem =
                        ConstItemsDictionary.TryGetValue(ElementIdsMap.GenericTag + propertyPath);
                if (templateConstItem is not null)
                {
                    constItem = new ConstItem { Value = templateConstItem.Value };
                    ConstItemsDictionary[elementId] = constItem;
                    return constItem;
                }
            }

            return null;
        }

        #endregion        

        #region private functions

        private async Task WorkingTaskMainAsync(CancellationToken ct)
        {
            if (_eventListCallbackIsEnabled) ClientEventListManager.EventMessagesCallback += OnEventMessagesCallbackInternal;

            while (true)
            {
                if (ct.IsCancellationRequested) break;
                await Task.Delay(10);
                if (ct.IsCancellationRequested) break;

                var nowUtc = DateTime.UtcNow;

                DoWork(nowUtc, ct);
            }

            Unsubscribe(true);

            if (_eventListCallbackIsEnabled) ClientEventListManager.EventMessagesCallback -= OnEventMessagesCallbackInternal;
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

            if (ElementIdsMap is not null)
            {
                string? tag = null;
                string? propertyPath = null;
                string? tagType = null;

                var constAny = ElementIdsMap.TryGetConstValue(elementId);
                if (!constAny.HasValue)
                {
                    var separatorIndex = elementId.LastIndexOf(ElementIdsMap.TagAndPropertySeparator);
                    if (separatorIndex > 0 && separatorIndex < elementId.Length - 1)
                    {
                        tag = elementId.Substring(0, separatorIndex);
                        propertyPath = elementId.Substring(separatorIndex);
                        tagType = ElementIdsMap.GetTagType(tag);
                    }
                    else
                    {
                        tag = elementId;
                        propertyPath = null;
                        tagType = null;
                    }

                    lock (ConstItemsDictionary)
                    {
                        var constItem = ConstItemsDictionaryTryGetValue(tag, propertyPath, tagType);
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
                            valueSubscription.Update(new ValueStatusTimestamp(constAny.Value, ValueStatusCode.Good,
                                DateTime.UtcNow)));
                    }
                    catch (Exception)
                    {
                    }

                    return elementId;
                }

                valueSubscriptionObj.MapValues = ElementIdsMap.GetFromMap(tag, propertyPath, tagType, null);

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
                //    converter.ParentItem = DsSolution.Instance;
                //    converter.ReplaceConstants(DsSolution.Instance);
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

                BeginInvoke(ct =>
                {
                    if (valueSubscriptionObj.ChildValueSubscriptionsList is not null)
                    {
                        foreach (var childValueSubscription in valueSubscriptionObj.ChildValueSubscriptionsList)
                            if (!childValueSubscription.IsConst)
                                ClientElementValueListManager.AddItem(childValueSubscription.MappedElementIdOrConst,
                                    childValueSubscription);
                    }
                    else
                    {
                        ClientElementValueListManager.AddItem(valueSubscriptionObj.MapValues[1] ?? "", valueSubscription);
                    }
                });

                return valueSubscriptionObj.MapValues[1] ?? "";
            }
            else
            {
                BeginInvoke(ct =>
                {
                    ClientElementValueListManager.AddItem(elementId, valueSubscription);
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

            BeginInvoke(ct =>
            {
                if (valueSubscriptionObj.ChildValueSubscriptionsList is not null)
                {
                    foreach (var childValueSubscription in valueSubscriptionObj.ChildValueSubscriptionsList)
                    {
                        if (!childValueSubscription.IsConst)
                            ClientElementValueListManager.RemoveItem(childValueSubscription);
                        childValueSubscription.ValueSubscriptionObj = null;
                    }

                    valueSubscriptionObj.ChildValueSubscriptionsList = null;
                }
                else
                {
                    ClientElementValueListManager.RemoveItem(valueSubscription);
                }
            });
        }

        #endregion

        #region private fields

        private bool _isInitialized;

        private bool _isConnected;

        private bool _isDisconnected = true;        

        /// <summary>
        ///     DataGrpc Server connection string.
        /// </summary>
        private string _serverAddress = "";

        /// <summary>
        ///     DataGrpc Systems Names.
        /// </summary>
        private string _systemNameToConnect = @"";

        /// <summary>
        ///     Used in DataGrpc Context initialization.
        /// </summary>
        private string _clientApplicationName = "";

        /// <summary>
        ///     Used in DataGrpc Context initialization.
        /// </summary>
        private string _clientWorkstationName = "";        

        /// <summary>
        ///     Used in DataGrpc ElementValueList initialization.
        /// </summary>
        private bool _elementValueListCallbackIsEnabled;

        private bool _eventListCallbackIsEnabled;        

        /// <summary>
        ///     Used in DataGrpc Context initialization.
        /// </summary>
        private CaseInsensitiveDictionary<string> _contextParams = new();        

        private Task? _workingTask;

        private CancellationTokenSource? _cancellationTokenSource;

        private ThreadSafeDispatcher _threadSafeDispatcher = new();

        private DateTime _lastFailedConnectionDateTimeUtc = DateTime.MinValue;

        private DateTime _lastSuccessfulConnectionDateTimeUtc = DateTime.MinValue;

        private Dictionary<IValueSubscription, ValueSubscriptionObj> _valueSubscriptionsCollection =
            new Dictionary<IValueSubscription, ValueSubscriptionObj>(ReferenceEqualityComparer.Instance);

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

            public static readonly List<string?> EmptyMapValues = new List<string?>(){ @"", @"" };

            public readonly string ElementId;

            public readonly IValueSubscription ValueSubscription;

            public List<ChildValueSubscription>? ChildValueSubscriptionsList;

            public SszConverter? Converter;

            /// <summary>
            ///     Count > 1
            /// </summary>
            public List<string?> MapValues = EmptyMapValues;

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
                SszConverter converter;
                if (Converter is not null)
                    converter = Converter;
                else
                    converter = SszConverter.Empty;
                var convertedValue = converter.Convert(values.ToArray(), null);
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