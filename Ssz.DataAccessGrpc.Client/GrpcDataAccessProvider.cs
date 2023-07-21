using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Threading;
using Ssz.Utils;
using Ssz.DataAccessGrpc.Client.Managers;
using Ssz.DataAccessGrpc.ServerBase;
using Microsoft.Extensions.Logging;
using Ssz.Utils.DataAccess;
using Grpc.Net.Client;
using System.Globalization;
using System.Threading.Tasks;
using Grpc.Core;
using Ssz.Utils.Logging;
using static Ssz.DataAccessGrpc.Client.Managers.ClientElementValueListManager;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics.SymbolStore;
using Microsoft.Extensions.Configuration;

namespace Ssz.DataAccessGrpc.Client
{
    public partial class GrpcDataAccessProvider : DataAccessProviderBase, IDataAccessProvider
    {
        #region construction and destruction
        
        public GrpcDataAccessProvider(IConfiguration configuration, ILogger<GrpcDataAccessProvider> logger, IUserFriendlyLogger? userFriendlyLogger = null) :
            base(new LoggersSet(logger, userFriendlyLogger))
        {
            _clientContextManager = new ClientContextManager(logger, WorkingThreadSafeDispatcher);

            _clientElementValueListManager = new ClientElementValueListManager(logger, ConfigurationHelper.GetValue<bool>(configuration, @"GrpcDataAccessProvider:UnsubscribeValueListItemsFromServer", true));
            _clientElementValuesJournalListManager = new ClientElementValuesJournalListManager(logger, ConfigurationHelper.GetValue<bool>(configuration, @"GrpcDataAccessProvider:UnsubscribeValuesJournalListItemsFromServer", true));
            _clientEventListManager = new ClientEventListManager(logger, this);
        }              
        
        #endregion

        #region public functions

        public GrpcChannel? GrpcChannel
        {
            get { return _clientContextManager.GrpcChannel; }
        }

        /// <summary>
        ///     Is called using сallbackDispatcher, see Initialize(..).        
        /// </summary>
        public override event EventHandler<ContextStatusChangedEventArgs> ContextStatusChanged
        {
            add { WorkingThreadSafeDispatcher.BeginInvoke(ct => _clientContextManager.ServerContextStatusChanged += value); }
            remove { WorkingThreadSafeDispatcher.BeginInvoke(ct => _clientContextManager.ServerContextStatusChanged -= value); }
        }

        /// <summary>
        ///     Is called using сallbackDispatcher, see Initialize(..).        
        /// </summary>
        public override event EventHandler ValueSubscriptionsUpdated = delegate { };

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
                valueSubscriptionObj.ValueSubscription.Update(
                    AddItem(valueSubscriptionObj));
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
                        {
                            valueSubscription.Update(AddItemResult.InvalidArgumentAddItemResult);
                            valueSubscription.Update(new ValueStatusTimestamp { ValueStatusCode = ValueStatusCodes.ItemDoesNotExist });
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

        /// <summary>                
        ///     If call to server failed returns null, otherwise returns changed ValueSubscriptions.        
        /// </summary>
        public override async Task<IValueSubscription[]?> PollElementValuesChangesAsync()
        {
            var taskCompletionSource = new TaskCompletionSource<IValueSubscription[]?>();

            WorkingThreadSafeDispatcher.BeginInvoke(ct =>
            {
                _clientElementValueListManager.Subscribe(_clientContextManager, CallbackDispatcher,
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
        public override async Task<ResultInfo> WriteAsync(IValueSubscription valueSubscription, ValueStatusTimestamp valueStatusTimestamp, ILogger? userFriendlyLogger)
        {
            var callbackDispatcher = CallbackDispatcher;
            if (!IsInitialized || callbackDispatcher is null) 
                return new ResultInfo { StatusCode = JobStatusCodes.FailedPrecondition };

            if (!ValueStatusCodes.IsGood(valueStatusTimestamp.ValueStatusCode)) 
                return new ResultInfo { StatusCode = JobStatusCodes.InvalidArgument };
            var value = valueStatusTimestamp.Value;

            if (!_valueSubscriptionsCollection.TryGetValue(valueSubscription, out ValueSubscriptionObj? valueSubscriptionObj))
                return new ResultInfo { StatusCode = JobStatusCodes.InvalidArgument };

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

                return ResultInfo.OkResultInfo;
            }

            object?[]? resultValues = null;
            if (valueSubscriptionObj.ChildValueSubscriptionsList is not null)
            {
                SszConverter converter = valueSubscriptionObj.Converter ?? SszConverter.Empty;                
                resultValues =
                    converter.ConvertBack(value.ValueAsObject(),
                        valueSubscriptionObj.ChildValueSubscriptionsList.Count, null, userFriendlyLogger);
                if (resultValues.Length == 0)
                    return ResultInfo.OkResultInfo;
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

            var taskCompletionSource = new TaskCompletionSource<ResultInfo>();

            WorkingThreadSafeDispatcher.BeginInvoke(ct =>
            {
                _clientElementValueListManager.Subscribe(_clientContextManager, CallbackDispatcher,
                    OnElementValuesCallback, true, ct);

                var resultInfo = ResultInfo.OkResultInfo;

                if (valueSubscriptionObj.ChildValueSubscriptionsList is not null)
                {
                    if (resultValues is null) throw new InvalidOperationException();
                    for (var i = 0; i < resultValues.Length; i++)
                    {
                        var resultValue = resultValues[i];
                        if (resultValue != SszConverter.DoNothing)
                            resultInfo = _clientElementValueListManager.Write(valueSubscriptionObj.ChildValueSubscriptionsList[i],
                                new ValueStatusTimestamp(new Any(resultValue), ValueStatusCodes.Good, DateTime.UtcNow));
                    }
                }
                else
                {
                    if (value.ValueAsObject() != SszConverter.DoNothing)
                        resultInfo = _clientElementValueListManager.Write(valueSubscription,
                            new ValueStatusTimestamp(value, ValueStatusCodes.Good, DateTime.UtcNow));
                }

                taskCompletionSource.SetResult(resultInfo);
            });

            return await taskCompletionSource.Task;
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
                _clientElementValueListManager.Subscribe(_clientContextManager, CallbackDispatcher,
                    OnElementValuesCallback, true, ct);
                (object[] failedValueSubscriptions, ResultInfo[] failedResultInfos) = _clientElementValueListManager.Write(valueSubscriptions, valueStatusTimestamps);

                taskCompletionSource.SetResult((failedValueSubscriptions.OfType<IValueSubscription>().ToArray(), failedResultInfos));
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
            // Early exception
            if (!_clientContextManager.ConnectionExists)
                throw new ConnectionDoesNotExistException();

            var taskCompletionSource = new TaskCompletionSource<IEnumerable<byte>>();

            WorkingThreadSafeDispatcher.BeginInvoke(ct =>
            {                
                try
                {
                    IEnumerable<byte> returnData;
                    _clientContextManager.Passthrough(recipientId, passthroughName,
                        dataToSend, out returnData);
                    taskCompletionSource.SetResult(returnData);                    
                }
                catch (RpcException ex)
                {
                    LoggersSet.Logger.LogError(ex, ex.Status.Detail);
                    taskCompletionSource.TrySetException(ex);
                }
                catch (ConnectionDoesNotExistException ex)
                {
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
        ///     Returns JobStatusCode <see cref="JobStatusCodes"/>
        ///     No throws.
        /// </summary>
        /// <param name="recipientId"></param>
        /// <param name="passthroughName"></param>
        /// <param name="dataToSend"></param>
        /// <param name="progressCallbackAction"></param>
        /// <returns></returns>
        public override async Task<uint> LongrunningPassthroughAsync(string recipientId, string passthroughName, byte[]? dataToSend,
            Action<Ssz.Utils.DataAccess.LongrunningPassthroughCallback>? progressCallbackAction)
        {
            // Early exception
            if (!_clientContextManager.ConnectionExists)
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
                        JobStatusCode = JobStatusCodes.Unknown
                    });
                }
                return JobStatusCodes.Unknown;
            }                

            var taskCompletionSource = new TaskCompletionSource<uint>();

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
                uint jobStatusCode;
                try
                {
                    jobStatusCode = await _clientContextManager.LongrunningPassthroughAsync(recipientId, passthroughName,
                        dataToSend, callbackActionDispatched);
                }
                catch (RpcException ex)
                {
                    LoggersSet.Logger.LogError(ex, ex.Status.Detail);
                    if (callbackActionDispatched is not null)
                    {
                        callbackActionDispatched(new Utils.DataAccess.LongrunningPassthroughCallback
                        {   
                            JobStatusCode = JobStatusCodes.Unknown
                        });
                    }
                    jobStatusCode = JobStatusCodes.Unknown;
                }
                catch (ConnectionDoesNotExistException ex)
                {
                    if (callbackActionDispatched is not null)
                    {
                        callbackActionDispatched(new Utils.DataAccess.LongrunningPassthroughCallback
                        {
                            JobStatusCode = JobStatusCodes.Unknown
                        });
                    }
                    jobStatusCode = JobStatusCodes.Unknown;
                }
                catch (Exception ex)
                {
                    LoggersSet.Logger.LogError(ex, "Passthrough exception.");
                    if (callbackActionDispatched is not null)
                    {
                        callbackActionDispatched(new Utils.DataAccess.LongrunningPassthroughCallback
                        {
                            JobStatusCode = JobStatusCodes.Unknown
                        });
                    }
                    jobStatusCode = JobStatusCodes.Unknown;
                }

                taskCompletionSource.SetResult(jobStatusCode);
            });

            return await taskCompletionSource.Task;
        }

        #endregion

        #region protected functions
        
        /// <summary>
        ///     Dispacther for working thread.
        /// </summary>
        protected ThreadSafeDispatcher WorkingThreadSafeDispatcher { get; } = new();

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
            await WorkingThreadSafeDispatcher.InvokeActionsInQueueAsync(cancellationToken).ConfigureAwait(false);

            if (cancellationToken.IsCancellationRequested) return;

            if (!_clientContextManager.ConnectionExists)
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
                                    valueSubscription.Update(AddItemResult.UnknownAddItemResult);
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


                        _clientContextManager.InitiateConnection(ServerAddress, ClientApplicationName,
                            ClientWorkstationName, SystemNameToConnect, ContextParams, CallbackDispatcher);

                        LoggersSet.Logger.LogDebug("End Connecting");

                        LoggersSet.Logger.LogInformation("DataAccessGrpcProvider connected to " + ServerAddress);
                                                
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

            _clientElementValueListManager.Subscribe(_clientContextManager, CallbackDispatcher,
                OnElementValuesCallback, elementValueListCallbackIsEnabled, cancellationToken);
            _clientEventListManager.Subscribe(_clientContextManager, CallbackDispatcher, eventListCallbackIsEnabled, cancellationToken);

            if (cancellationToken.IsCancellationRequested) return;
            if (_clientContextManager.ConnectionExists)
            {
                LastSuccessfulConnectionDateTimeUtc = nowUtc;
                try
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    _clientContextManager.DoWork(cancellationToken, nowUtc);
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

            _clientContextManager.CloseConnection();

            _clientElementValueListManager.Unsubscribe(clearClientSubscriptions);
            _clientEventListManager.Unsubscribe();
            _clientElementValuesJournalListManager.Unsubscribe(clearClientSubscriptions);            
        }

        protected void RaiseValueSubscriptionsUpdated()
        {
            LastValueSubscriptionsUpdatedDateTimeUtc = DateTime.UtcNow;

            ValueSubscriptionsUpdated(this, EventArgs.Empty);
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
                _clientEventListManager.EventMessagesCallback += OnClientEventListManager_EventMessagesCallbackInternal;

            while (true)
            {
                if (cancellationToken.IsCancellationRequested) break;
                await Task.Delay(10).ConfigureAwait(false);
                if (cancellationToken.IsCancellationRequested) break;                

                var nowUtc = DateTime.UtcNow;                

                await DoWorkAsync(nowUtc, cancellationToken).ConfigureAwait(false);
            }

            Unsubscribe(true);

            if (eventListCallbackIsEnabled)
                _clientEventListManager.EventMessagesCallback -= OnClientEventListManager_EventMessagesCallbackInternal;
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
                        {
                            valueSubscription.Update(ConstAddItemResult);
                            valueSubscription.Update(new ValueStatusTimestamp(constAny.Value, ValueStatusCodes.Good,
                                DateTime.UtcNow));
                        });
                    }
                    catch (Exception)
                    {
                    }

                    return constAny.Value.ValueAsString(false);
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
                WorkingThreadSafeDispatcher.BeginInvoke(ct =>
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

            WorkingThreadSafeDispatcher.BeginInvoke(ct =>
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

        /// <summary>
        ///     Called using сallbackDispatcher.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void OnElementValuesCallback(object? sender,
            ClientElementValueListManager.ElementValuesCallbackEventArgs eventArgs)
        {
            foreach (ClientElementValueListManager.ElementValuesCallbackChange elementValuesCallbackChange in eventArgs.ElementValuesCallbackChanges)
            {
                var changedValueSubscription = (IValueSubscription)elementValuesCallbackChange.ClientObj;
                if (elementValuesCallbackChange.AddItemResult is not null)
                {
                    changedValueSubscription.Update(elementValuesCallbackChange.AddItemResult);
                }
                if (elementValuesCallbackChange.ValueStatusTimestamp.HasValue)
                {
                    changedValueSubscription.Update(elementValuesCallbackChange.ValueStatusTimestamp.Value);
                }                
            }
            DataGuid = Guid.NewGuid();

            RaiseValueSubscriptionsUpdated();
        }

        #endregion

        #region private fields        

        private Task? _workingTask;

        private CancellationTokenSource? _cancellationTokenSource;        

        private Dictionary<IValueSubscription, ValueSubscriptionObj> _valueSubscriptionsCollection =
            new(ReferenceEqualityComparer<IValueSubscription>.Default);

        private ClientContextManager _clientContextManager { get; }

        private ClientElementValueListManager _clientElementValueListManager { get; }

        /// <summary>
        ///     Unspecified Unknown AddItemResult.
        /// </summary>
        private static readonly AddItemResult ConstAddItemResult = new AddItemResult {
            ResultInfo = new ResultInfo { StatusCode = JobStatusCodes.OK },
            IsReadable = true,
            IsWritable = true,
        };

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

            public string MappedElementIdOrConst { get; private set; }

            public void Update(string mappedElementIdOrConst)
            {
            }

            public void Update(AddItemResult addItemResult)
            {
            }

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