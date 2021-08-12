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
using Ssz.DataGrpc.Common;
using Ssz.Utils.DataAccess;
using Grpc.Net.Client;
using System.Globalization;
using System.Threading.Tasks;

namespace Ssz.DataGrpc.Client
{
    public partial class GrpcDataAccessProvider : IDataAccessProvider, IDispatcher
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
        ///     This is the implementation of the IDisposable.Dispose method.  The client
        ///     application should invoke this method when this instance is no longer needed.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     This method is invoked when the IDisposable.Dispose or Finalize actions are
        ///     requested.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Close();
            }

            Disposed = true;
        }

        /// <summary>
        ///     Invoked by the .NET Framework while doing heap managment (Finalize).
        /// </summary>
        ~GrpcDataAccessProvider()
        {
            Dispose(false);
        }

        public bool Disposed { get; private set; }

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

        /// <summary>
        ///     Is connected to Model Data Source.
        /// </summary>
        public bool IsConnected
        {
            get { return IsConnectedEvent.WaitOne(0); }
        }

        public ManualResetEvent IsConnectedEvent { get; } = new ManualResetEvent(false);

        public GrpcChannel? GrpcChannel
        {
            get { return ClientConnectionManager.GrpcChannel; }
        }

        public bool IsInitialized { get; private set; }

        public Guid DataGuid { get; private set; }

        public ElementIdsMap? ElementIdsMap { get; private set; }

        public CaseInsensitiveDictionary<string> BackMapDictionary { get; } = new();

        public object? Obj { get; set; }

        /// <summary>
        ///     Is called using сallbackDispatcher, see Initialize(..).
        /// </summary>
        public event Action ValueSubscriptionsUpdated = delegate { };

        /// <summary>
        ///     Is called using сallbackDispatcher, see Initialize(..).
        ///     Occurs after connected to model.
        /// </summary>
        public event Action Connected = delegate { };

        /// <summary>
        ///     Is called using сallbackDispatcher, see Initialize(..).
        ///     Occurs after disconnected from model.
        /// </summary>
        public event Action Disconnected = delegate { };        

        /// <summary>
        ///     You can set updateValueItems = false and invoke PollElementValuesChanges(...) manually.
        /// </summary>
        /// <param name="сallbackDispatcher"></param>
        /// <param name="elementValueListCallbackIsEnabled"></param>
        /// <param name="serverAddress"></param>
        /// <param name="clientApplicationName"></param>
        /// <param name="clientWorkstationName"></param>
        /// <param name="systemNames"></param>
        /// <param name="contextParams"></param>
        public virtual void Initialize(IDispatcher? сallbackDispatcher,
            ElementIdsMap? elementIdsMap,
            bool elementValueListCallbackIsEnabled,
            bool eventListCallbackIsEnabled,
            string serverAddress,
            string clientApplicationName, string clientWorkstationName, string systemNameToConnect, CaseInsensitiveDictionary<string> contextParams)
        {
            Close();            

            Logger.LogDebug("Starting ModelDataProvider. сallbackDispatcher != null " + (сallbackDispatcher != null).ToString());

            CallbackDispatcher = сallbackDispatcher;
            ElementIdsMap = elementIdsMap;
            _elementValueListCallbackIsEnabled = elementValueListCallbackIsEnabled;
            _eventListCallbackIsEnabled = eventListCallbackIsEnabled;
            _serverAddress = serverAddress;            
            _clientApplicationName = clientApplicationName;            
            _clientWorkstationName = clientWorkstationName;
            _systemNameToConnect = systemNameToConnect;
            _contextParams = contextParams;

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
                if (ElementIdsMap != null)
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
            
            IsConnectedEvent.Reset();

            _workingTask = Task.Run(async () =>
            {
                await WorkingTaskMain(_cancellationTokenSource.Token);
            });

            IsInitialized = true;
        }

        public virtual void Close()
        {
            if (!IsInitialized) return;

            IsInitialized = false;

            _contextParams = new CaseInsensitiveDictionary<string>();
            CallbackDispatcher = null;
            ElementIdsMap = null;

            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource = null;
            }

            if (_workingTask != null) _workingTask.Wait(30000);
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
        public virtual string AddItem(string elementId, IValueSubscription valueSubscription)
        {
            Logger.LogDebug("DataGrpcProvider.AddItem() " + elementId);

            var callbackDispatcher = CallbackDispatcher;
            if (!IsInitialized || callbackDispatcher == null) return elementId;

            var valueSubscriptionObj = new ValueSubscriptionObj
            {
                ValueSubscription = valueSubscription,
                ElementId = elementId
            };
            valueSubscription.Obj = valueSubscriptionObj;

            if (ElementIdsMap != null)
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
                        if (constItem != null)
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
                            valueSubscription.Update(new ValueStatusTimestamp(constAny.Value, StatusCodes.Good,
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
                        var childValueSubscription = new ChildValueSubscription(valueSubscriptionObj, v, ElementIdsMap);
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

                if (childValueSubscriptionsList.Count > 1 || converter != null)
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
                    if (valueSubscriptionObj.ChildValueSubscriptionsList != null)
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
        ///     If valueSubscription is not subscribed - does nothing.
        /// </summary>
        /// <param name="valueSubscription"></param>
        public virtual void RemoveItem(IValueSubscription valueSubscription)
        {
            if (!IsInitialized) return;

            var valueSubscriptionObj = valueSubscription.Obj as ValueSubscriptionObj;
            if (valueSubscriptionObj == null) return;

            valueSubscriptionObj.ValueSubscription = null;
            valueSubscription.Obj = null;

            var constAny = ElementIdsMap?.TryGetConstValue(valueSubscriptionObj.ElementId);
            if (constAny.HasValue) return;

            var disposable = valueSubscriptionObj.Converter as IDisposable;
            if (disposable != null) disposable.Dispose();

            lock (ConstItemsDictionary)
            {
                var constItem = ConstItemsDictionary.TryGetValue(valueSubscriptionObj.ElementId);
                if (constItem != null)
                {
                    constItem.Subscribers.Remove(valueSubscription);
                    return;
                }
            }

            BeginInvoke(ct =>
            {
                if (valueSubscriptionObj.ChildValueSubscriptionsList != null)
                {
                    foreach (var childValueSubscription in valueSubscriptionObj.ChildValueSubscriptionsList)
                    {
                        if (!childValueSubscription.IsConst) 
                            ClientElementValueListManager.RemoveItem(childValueSubscription);                        
                        childValueSubscription.Obj = null;
                    }

                    valueSubscriptionObj.ChildValueSubscriptionsList = null;
                }
                else
                {
                    ClientElementValueListManager.RemoveItem(valueSubscription);
                }
            });
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
                if (сallbackDispatcher != null)
                {
                    try
                    {
                        сallbackDispatcher.BeginInvoke(ct => setResultAction(changedValueSubscriptions != null ? changedValueSubscriptions.OfType<IValueSubscription>().ToArray() : null));
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
        ///     setResultAction(failedValueSubscriptions) is called, failedValueSubscriptions != null.
        ///     If connection error, failedValueSubscriptions is all clientObjs.        
        /// </summary>
        public virtual void Write(IValueSubscription[] valueSubscriptions, ValueStatusTimestamp[] valueStatusTimestamps, Action<IValueSubscription[]>? setResultAction)
        {
            BeginInvoke(ct =>
            {                
                ClientElementValueListManager.Subscribe(ClientConnectionManager, CallbackDispatcher,
                    OnElementValuesCallback, true, ct);                
                object[] failedValueSubscriptions = ClientElementValueListManager.Write(valueSubscriptions, valueStatusTimestamps);

                if (setResultAction != null)
                {
                    IDispatcher? сallbackDispatcher = CallbackDispatcher;
                    if (сallbackDispatcher != null)
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
            if (!IsInitialized || callbackDispatcher == null) return;

            if (!StatusCodes.IsGood(valueStatusTimestamp.StatusCode)) return;
            var value = valueStatusTimestamp.Value;

            var valueSubscriptionObj = valueSubscription.Obj as ValueSubscriptionObj;
            if (valueSubscriptionObj == null) return;

            var logger = alternativeLogger ?? Logger;

            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("UI TAG: \"" + valueSubscriptionObj.ElementId + "\"; Value from UI: \"" +
                                             value + "\"");

            IValueSubscription[]? constItemValueSubscriptionsArray = null;
            lock (ConstItemsDictionary)
            {
                var constItem = ConstItemsDictionary.TryGetValue(valueSubscriptionObj.ElementId);
                if (constItem != null && constItem.Value.ValueAsObject() != DBNull.Value)
                {
                    constItem.Value = value;
                    constItemValueSubscriptionsArray = constItem.Subscribers.ToArray();
                }
            }

            if (constItemValueSubscriptionsArray != null)
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
            if (valueSubscriptionObj.ChildValueSubscriptionsList != null)
            {
                SszConverter converter;
                if (valueSubscriptionObj.Converter != null)
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
                if (valueSubscriptionObj.ChildValueSubscriptionsList != null)
                {
                    if (resultValues == null) throw new InvalidOperationException();
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

                if (valueSubscriptionObj.ChildValueSubscriptionsList != null)
                {
                    if (resultValues == null) throw new InvalidOperationException();
                    for (var i = 0; i < resultValues.Length; i++)
                    {
                        var resultValue = resultValues[i];
                        if (resultValue != SszConverter.DoNothing)
                            ClientElementValueListManager.Write(valueSubscriptionObj.ChildValueSubscriptionsList[i],
                                new ValueStatusTimestamp(new Any(resultValue), StatusCodes.Good, DateTime.UtcNow));
                    }
                }
                else
                {
                    if (value.ValueAsObject() != SszConverter.DoNothing)
                        ClientElementValueListManager.Write(valueSubscription,
                            new ValueStatusTimestamp(value, StatusCodes.Good, DateTime.UtcNow));
                }
            });
        }

        /// <summary>        
        ///     setResultAction(..) is called using сallbackDispatcher, see Initialize(..).
        ///     If call to server failed (exception or passthroughResult.ResultCode != 0), setResultAction(null) is called.        
        /// </summary>
        public void Passthrough(string recipientId, string passthroughName, byte[] dataToSend,
            Action<IEnumerable<byte>?> setResultAction)
        {
            BeginInvoke(ct =>
            {
                IEnumerable<byte>? result;
                try
                {
                    IEnumerable<byte> returnData;
                    uint resultCode = ClientConnectionManager.Passthrough(recipientId, passthroughName,
                        dataToSend, out returnData);
                    if (DataGrpcResultCodes.Succeeded(resultCode))
                    {
                        result = returnData;
                    }
                    else
                    {
                        result = null;
                    }
                }
                catch (Exception)
                {
                    result = null;
                }

                IDispatcher? сallbackDispatcher = CallbackDispatcher;
                if (сallbackDispatcher != null)
                {
                    try
                    {
                        сallbackDispatcher.BeginInvoke(ct => setResultAction(result));
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            );
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
                if (IsConnected)
                {
                    Unsubscribe();

                    #region notify subscribers disconnected

                    Logger.LogInformation("DataGrpcProvider diconnected");

                    IsConnectedEvent.Reset();
                    Action disconnected = Disconnected;
                    сallbackDispatcher = CallbackDispatcher;
                    if (disconnected != null && сallbackDispatcher != null)
                    {
                        if (cancellationToken.IsCancellationRequested) return;
                        try
                        {
                            сallbackDispatcher.BeginInvoke(ct => disconnected());
                        }
                        catch (Exception)
                        {
                        }
                    }

                    IEnumerable<IValueSubscription> valueSubscriptions =
                        ClientElementValueListManager.GetAllClientObjs().OfType<IValueSubscription>();

                    сallbackDispatcher = CallbackDispatcher;
                    if (сallbackDispatcher != null)
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

                        IsConnectedEvent.Set();
                        Action connected = Connected;
                        сallbackDispatcher = CallbackDispatcher;
                        if (connected != null && сallbackDispatcher != null)
                        {
                            if (cancellationToken.IsCancellationRequested) return;
                            try
                            {
                                сallbackDispatcher.BeginInvoke(ct => connected());
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogDebug(ex, "");

                        _lastFailedConnectionDateTimeUtc = DateTime.UtcNow;

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
                try
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    ClientConnectionManager.Process(cancellationToken, nowUtc);
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
        protected virtual void Unsubscribe()
        {
            ClientElementValueListManager.Unsubscribe();
            ClientEventListManager.Unsubscribe();
            ClientElementValueJournalListManager.Unsubscribe();

            ClientConnectionManager.CloseConnection();
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
                if (changedValueSubscription.Obj == null) continue;
                //var valueSubscriptionObj = (ValueSubscriptionObj)changedValueSubscription.Obj;

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
            if (ElementIdsMap == null) return null;

            string elementId = tag + propertyPath;
            var constItem = ConstItemsDictionary.TryGetValue(elementId);
            if (constItem != null) return constItem;

            if (!string.IsNullOrEmpty(propertyPath))
            {
                ConstItem? templateConstItem = null;
                if (!string.IsNullOrEmpty(tagType))
                    templateConstItem = ConstItemsDictionary.TryGetValue(tagType +
                        ElementIdsMap.TagTypeSeparator + ElementIdsMap.GenericTag + propertyPath);
                if (templateConstItem == null)
                    templateConstItem =
                        ConstItemsDictionary.TryGetValue(ElementIdsMap.GenericTag + propertyPath);
                if (templateConstItem != null)
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

        private async Task WorkingTaskMain(CancellationToken ct)
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

            Unsubscribe();

            if (_eventListCallbackIsEnabled) ClientEventListManager.EventMessagesCallback -= OnEventMessagesCallbackInternal; 
        }        

        #endregion

        #region private fields        

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

        #endregion

        protected class ConstItem
        {
            public readonly HashSet<IValueSubscription> Subscribers = new(ReferenceEqualityComparer<object>.Default);

            public Any Value;
        }        

        private class ValueSubscriptionObj
        {
            public List<ChildValueSubscription>? ChildValueSubscriptionsList;

            public SszConverter? Converter;

            public List<string?> MapValues = new();

            public string ElementId = @"";

            public IValueSubscription? ValueSubscription;

            public void ChildValueSubscriptionUpdated()
            {
                if (ValueSubscription == null || ChildValueSubscriptionsList == null) return;

                var values = new List<object?>();
                foreach (var childValueSubscription in ChildValueSubscriptionsList)
                    values.Add(childValueSubscription.Value.ValueAsObject());
                SszConverter converter;
                if (Converter != null)
                    converter = Converter;
                else
                    converter = SszConverter.Empty;
                var convertedValue = converter.Convert(values.ToArray(), null);
                if (convertedValue == SszConverter.DoNothing) return;
                ValueSubscription.Update(new ValueStatusTimestamp(new Any(convertedValue), StatusCodes.Good,
                    DateTime.UtcNow));
            }
        }

        private class ChildValueSubscription : IValueSubscription
        {
            public readonly bool IsConst;

            public readonly string MappedElementIdOrConst;

            public Any Value;            

            public ChildValueSubscription(ValueSubscriptionObj valueSubscriptionObj, string mappedElementIdOrConst, ElementIdsMap? elementIdsMap)
            {
                Obj = valueSubscriptionObj;
                MappedElementIdOrConst = mappedElementIdOrConst;

                if (elementIdsMap != null)
                {
                    var constAny = elementIdsMap.TryGetConstValue(mappedElementIdOrConst);
                    if (constAny.HasValue)
                    {
                        Value = constAny.Value;
                        IsConst = true;
                    }
                }                
            }

            public object? Obj { get; set; }

            public void Update(ValueStatusTimestamp valueStatusTimestamp)
            {
                switch (valueStatusTimestamp.StatusCode)
                {
                    case StatusCodes.Unknown:
                        Value = new Any();
                        break;
                    case StatusCodes.ItemDoesNotExist:
                        Value = new Any(DBNull.Value);
                        break;
                    default:
                        Value = valueStatusTimestamp.Value;
                        break;
                }

                if (Obj != null) 
                    ((ValueSubscriptionObj)Obj).ChildValueSubscriptionUpdated();
            }
        }
    }
}