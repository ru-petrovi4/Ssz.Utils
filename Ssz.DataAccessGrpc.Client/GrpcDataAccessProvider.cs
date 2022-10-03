using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Threading;
using Ssz.Utils;
using Ssz.DataAccessGrpc.Client.Managers;
using Ssz.DataAccessGrpc.ServerBase;
using Ssz.DataAccessGrpc.Client.Data;
using Microsoft.Extensions.Logging;
using Ssz.Utils.DataAccess;
using Grpc.Net.Client;
using System.Globalization;
using System.Threading.Tasks;
using Grpc.Core;
using Ssz.Utils.Logging;

namespace Ssz.DataAccessGrpc.Client
{
    public partial class GrpcDataAccessProvider : DataAccessProviderBase, IDataAccessProvider
    {
        #region construction and destruction

        public GrpcDataAccessProvider(ILogger<GrpcDataAccessProvider> logger, IUserFriendlyLogger? userFriendlyLogger = null) :
            base(new LoggersSet<GrpcDataAccessProvider>(logger, userFriendlyLogger))
        {
            _clientConnectionManager = new ClientConnectionManager(logger, ThreadSafeDispatcher);

            _clientElementValueListManager = new ClientElementValueListManager(logger);
            _clientElementValuesJournalListManager = new ClientElementValuesJournalListManager(logger);
            _clientEventListManager = new ClientEventListManager(logger, this);
        }        

        #endregion

        #region public functions

        public GrpcChannel? GrpcChannel
        {
            get { return _clientConnectionManager.GrpcChannel; }
        }

        /// <summary>
        ///     Is called using сallbackDispatcher, see Initialize(..).        
        /// </summary>
        public override event Action<IDataAccessProvider> ValueSubscriptionsUpdated = delegate { };

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
        /// <param name="callbackDispatcher"></param>
        public override void Initialize(ElementIdsMap? elementIdsMap,
            bool elementValueListCallbackIsEnabled,
            bool eventListCallbackIsEnabled,
            string serverAddress,
            string clientApplicationName,
            string clientWorkstationName,
            string systemNameToConnect,
            CaseInsensitiveDictionary<string?> contextParams,
            IDispatcher? callbackDispatcher)
        {
            base.Initialize(elementIdsMap,
                elementValueListCallbackIsEnabled,
                eventListCallbackIsEnabled,
                serverAddress,
                 clientApplicationName,
                 clientWorkstationName,
                 systemNameToConnect, 
                contextParams,
                callbackDispatcher);

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

            foreach (ValueSubscriptionObj valueSubscriptionObj in _valueSubscriptionsCollection.Values)
            {
                valueSubscriptionObj.ValueSubscription.MappedElementIdOrConst = AddItem(valueSubscriptionObj);
            }
        }

        /// <summary>
        ///     Tou can call Dispose() instead of this method.
        ///     Closes without waiting working thread exit.
        /// </summary>
        public override void Close()
        {
            if (!IsInitialized) return;

            base.Close();

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
        }

        /// <summary>
        ///     Tou can call DisposeAsync() instead of this method.
        ///     Closes WITH waiting working thread exit.
        /// </summary>
        public override async Task CloseAsync()
        {
            Close();

            if (_workingTask is not null)
                await _workingTask;
        }

        /// <summary>        
        ///     Returns id actully used for OPC subscription, always as original id.
        ///     valueSubscription.Update() is called using сallbackDispatcher, see Initialize(..).        
        /// </summary>
        /// <param name="elementId"></param>
        /// <param name="valueSubscription"></param>
        public override void AddItem(string? elementId, IValueSubscription valueSubscription)
        {
            LoggersSet.Logger.LogDebug("DataAccessGrpcProvider.AddItem() " + elementId);

            if (elementId == null || elementId == @"")
            {
                var callbackDispatcher = CallbackDispatcher;
                if (callbackDispatcher is not null)
                    try
                    {
                        callbackDispatcher.BeginInvoke(ct =>
                            valueSubscription.Update(new ValueStatusTimestamp { ValueStatusCode = ValueStatusCodes.ItemDoesNotExist }));
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
            ThreadSafeDispatcher.BeginInvoke(ct =>
            {
                _clientElementValueListManager.Subscribe(_clientConnectionManager, CallbackDispatcher,
                    OnElementValuesCallback, true, ct);
                object[]? changedValueSubscriptions = _clientElementValueListManager.PollChanges();
                taskCompletionSource.SetResult(changedValueSubscriptions is not null ? changedValueSubscriptions.OfType<IValueSubscription>().ToArray() : null);                
            }
            );
            return await taskCompletionSource.Task;
        }

        /// <summary>
        ///     Writes to userFriendlyLogger with Information level.
        /// </summary>
        /// <param name="valueSubscription"></param>
        /// <param name="valueStatusTimestamp"></param>
        /// <param name="userFriendlyLogger"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public override void Write(IValueSubscription valueSubscription, ValueStatusTimestamp valueStatusTimestamp, ILogger? userFriendlyLogger)
        {
            var callbackDispatcher = CallbackDispatcher;
            if (!IsInitialized || callbackDispatcher is null) return;

            if (!ValueStatusCodes.IsGood(valueStatusTimestamp.ValueStatusCode)) return;
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
                _clientElementValueListManager.Subscribe(_clientConnectionManager, CallbackDispatcher,
                    OnElementValuesCallback, true, ct);

                if (valueSubscriptionObj.ChildValueSubscriptionsList is not null)
                {
                    if (resultValues is null) throw new InvalidOperationException();
                    for (var i = 0; i < resultValues.Length; i++)
                    {
                        var resultValue = resultValues[i];
                        if (resultValue != SszConverter.DoNothing)
                            _clientElementValueListManager.Write(valueSubscriptionObj.ChildValueSubscriptionsList[i],
                                new ValueStatusTimestamp(new Any(resultValue), ValueStatusCodes.Good, DateTime.UtcNow));
                    }
                }
                else
                {
                    if (value.ValueAsObject() != SszConverter.DoNothing)
                        _clientElementValueListManager.Write(valueSubscription,
                            new ValueStatusTimestamp(value, ValueStatusCodes.Good, DateTime.UtcNow));
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
        public override async Task<IValueSubscription[]> WriteAsync(IValueSubscription[] valueSubscriptions, ValueStatusTimestamp[] valueStatusTimestamps)
        {
            var taskCompletionSource = new TaskCompletionSource<IValueSubscription[]>();
            ThreadSafeDispatcher.BeginInvoke(ct =>
            {
                _clientElementValueListManager.Subscribe(_clientConnectionManager, CallbackDispatcher,
                    OnElementValuesCallback, true, ct);
                object[] failedValueSubscriptions = _clientElementValueListManager.Write(valueSubscriptions, valueStatusTimestamps);

                taskCompletionSource.SetResult(failedValueSubscriptions.OfType<IValueSubscription>().ToArray());
            }
            );
            return await taskCompletionSource.Task;
        }

        /// <summary>
        ///     Throws if any errors.
        /// </summary>
        /// <param name="recipientId"></param>
        /// <param name="passthroughName"></param>
        /// <param name="dataToSend"></param>
        /// <returns></returns>
        public override async Task<IEnumerable<byte>> PassthroughAsync(string recipientId, string passthroughName, byte[] dataToSend)
        {
            var taskCompletionSource = new TaskCompletionSource<IEnumerable<byte>>();
            ThreadSafeDispatcher.BeginInvoke(ct =>
            {                
                try
                {
                    IEnumerable<byte> returnData;
                    _clientConnectionManager.Passthrough(recipientId, passthroughName,
                        dataToSend, out returnData);
                    taskCompletionSource.SetResult(returnData);                    
                }
                catch (RpcException ex)
                {
                    LoggersSet.Logger.LogError(ex, ex.Status.Detail);
                    taskCompletionSource.TrySetException(ex);
                }
                catch (Exception ex)
                {
                    LoggersSet.Logger.LogError(ex, "Passthrough exception.");
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
        public override async Task<bool> LongrunningPassthroughAsync(string recipientId, string passthroughName, byte[]? dataToSend,
            Action<Ssz.Utils.DataAccess.LongrunningPassthroughCallback>? progressCallbackAction)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            ThreadSafeDispatcher.BeginInvoke(async ct =>
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
                bool succeeded;
                try
                {
                    uint jobStatusCode = await _clientConnectionManager.LongrunningPassthroughAsync(recipientId, passthroughName,
                        dataToSend, callbackActionDispatched);
                    succeeded = jobStatusCode == JobStatusCodes.OK;
                }
                catch (RpcException ex)
                {
                    LoggersSet.Logger.LogError(ex, ex.Status.Detail);
                    if (callbackActionDispatched is not null)
                    {
                        callbackActionDispatched(new Utils.DataAccess.LongrunningPassthroughCallback
                        {   
                            JobStatusCode = JobStatusCodes.UnknownError
                        });
                    }
                    succeeded = false;
                }
                catch (Exception ex)
                {
                    LoggersSet.Logger.LogError(ex, "Passthrough exception.");
                    if (callbackActionDispatched is not null)
                    {
                        callbackActionDispatched(new Utils.DataAccess.LongrunningPassthroughCallback
                        {
                            JobStatusCode = JobStatusCodes.UnknownError
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
        ///     Dispacther for working thread.
        /// </summary>
        protected ThreadSafeDispatcher ThreadSafeDispatcher { get; } = new();

        /// <summary>
        ///     This dictionary is created, because we can write to const values.
        /// </summary>
        protected CaseInsensitiveDictionary<ConstItem> ConstItemsDictionary { get; } = new();

        protected DateTime LastValueSubscriptionsUpdatedDateTimeUtc { get; private set; } = DateTime.MinValue;

        /// <summary>
        ///     On loop in working thread.
        /// </summary>
        /// <param name="cancellationToken"></param>
        protected virtual async Task DoWorkAsync(DateTime nowUtc, CancellationToken cancellationToken)
        {
            await ThreadSafeDispatcher.InvokeActionsInQueueAsync(cancellationToken);

            if (cancellationToken.IsCancellationRequested) return;

            if (!_clientConnectionManager.ConnectionExists)
            {
                IDispatcher? сallbackDispatcher;
                if (IsConnectedEventWaitHandle.WaitOne(0))
                {
                    Unsubscribe(false);

#region notify subscribers disconnected

                    LoggersSet.Logger.LogInformation("DataAccessGrpcProvider diconnected");                    

                    IEnumerable<IValueSubscription> valueSubscriptions =
                        _clientElementValueListManager.GetAllClientObjs().OfType<IValueSubscription>();

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

                if (IsInitialized && !String.IsNullOrWhiteSpace(ServerAddress) &&
                    nowUtc > LastFailedConnectionDateTimeUtc + TimeSpan.FromSeconds(5))
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


                        _clientConnectionManager.InitiateConnection(ServerAddress, ClientApplicationName,
                            ClientWorkstationName, SystemNameToConnect, ContextParams);

                        LoggersSet.Logger.LogDebug("End Connecting");

                        LoggersSet.Logger.LogInformation("DataAccessGrpcProvider connected to " + ServerAddress);

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
                                });
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggersSet.Logger.LogDebug(ex, "");

                        LastFailedConnectionDateTimeUtc = nowUtc;

                        OnInitiateConnectionException(ex);
                    }
                }
            }

            if (cancellationToken.IsCancellationRequested) 
                return;
            bool elementValueListCallbackIsEnabled;
            bool eventListCallbackIsEnabled;
            try
            {
                elementValueListCallbackIsEnabled = ElementValueListCallbackIsEnabled;
                eventListCallbackIsEnabled = EventListCallbackIsEnabled;
            }
            catch
            {
                return;
            }

            _clientElementValueListManager.Subscribe(_clientConnectionManager, CallbackDispatcher,
                OnElementValuesCallback, elementValueListCallbackIsEnabled, cancellationToken);
            _clientEventListManager.Subscribe(_clientConnectionManager, CallbackDispatcher, eventListCallbackIsEnabled, cancellationToken);

            if (cancellationToken.IsCancellationRequested) return;
            if (_clientConnectionManager.ConnectionExists)
            {
                LastSuccessfulConnectionDateTimeUtc = nowUtc;
                try
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    _clientConnectionManager.DoWork(cancellationToken, nowUtc);
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
                    });
                }
                catch (Exception)
                {
                }
            }

            _clientConnectionManager.CloseConnection();

            _clientElementValueListManager.Unsubscribe(clearClientSubscriptions);
            _clientEventListManager.Unsubscribe();
            _clientElementValuesJournalListManager.Unsubscribe(clearClientSubscriptions);            
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

            ValueSubscriptionsUpdated(this);
        }

#endregion

#region private functions

        private async Task WorkingTaskMainAsync(CancellationToken cancellationToken)
        {
            bool elementValueListCallbackIsEnabled;
            bool eventListCallbackIsEnabled;
            try
            {
                elementValueListCallbackIsEnabled = ElementValueListCallbackIsEnabled;
                eventListCallbackIsEnabled = EventListCallbackIsEnabled;
            }
            catch
            {
                return;
            }

            if (eventListCallbackIsEnabled) 
                _clientEventListManager.EventMessagesCallback += OnEventMessagesCallbackInternal;

            while (true)
            {
                if (cancellationToken.IsCancellationRequested) break;
                await Task.Delay(10);
                if (cancellationToken.IsCancellationRequested) break;                

                var nowUtc = DateTime.UtcNow;                

                await DoWorkAsync(nowUtc, cancellationToken);
            }

            Unsubscribe(true);

            if (eventListCallbackIsEnabled)
                _clientEventListManager.EventMessagesCallback -= OnEventMessagesCallbackInternal;
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
                            valueSubscription.Update(new ValueStatusTimestamp(constAny.Value, ValueStatusCodes.Good,
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
                                _clientElementValueListManager.AddItem(childValueSubscription.MappedElementIdOrConst,
                                    childValueSubscription);
                    }
                    else
                    {
                        _clientElementValueListManager.AddItem(valueSubscriptionObj.MapValues[1] ?? "", valueSubscription);
                    }
                });

                return valueSubscriptionObj.MapValues[1] ?? "";
            }
            else
            {
                ThreadSafeDispatcher.BeginInvoke(ct =>
                {
                    _clientElementValueListManager.AddItem(elementId, valueSubscription);
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

            ThreadSafeDispatcher.BeginInvoke(ct =>
            {
                if (valueSubscriptionObj.ChildValueSubscriptionsList is not null)
                {
                    foreach (var childValueSubscription in valueSubscriptionObj.ChildValueSubscriptionsList)
                    {
                        if (!childValueSubscription.IsConst)
                            _clientElementValueListManager.RemoveItem(childValueSubscription);
                        childValueSubscription.ValueSubscriptionObj = null;
                    }

                    valueSubscriptionObj.ChildValueSubscriptionsList = null;
                }
                else
                {
                    _clientElementValueListManager.RemoveItem(valueSubscription);
                }
            });
        }

        #endregion

        #region private fields        

        private Task? _workingTask;

        private CancellationTokenSource? _cancellationTokenSource;        

        private Dictionary<IValueSubscription, ValueSubscriptionObj> _valueSubscriptionsCollection =
            new(ReferenceEqualityComparer<IValueSubscription>.Default);

        private ClientConnectionManager _clientConnectionManager { get; }

        private ClientElementValueListManager _clientElementValueListManager { get; }

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

                if (ChildValueSubscriptionsList.Any(vs => vs.ValueStatusTimestamp.ValueStatusCode == ValueStatusCodes.ItemDoesNotExist))
                {
                    ValueSubscription.Update(new ValueStatusTimestamp { ValueStatusCode = ValueStatusCodes.ItemDoesNotExist });
                    return;
                }
                if (ChildValueSubscriptionsList.Any(vs => vs.ValueStatusTimestamp.ValueStatusCode == ValueStatusCodes.Unknown))
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
                ValueSubscription.Update(new ValueStatusTimestamp(new Any(convertedValue), ValueStatusCodes.Good,
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