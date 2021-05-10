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

namespace Ssz.DataGrpc.Client
{
    public partial class GrpcDataAccessProvider : IDataAccessProvider, IDispatcher
    {
        #region construction and destruction

        public GrpcDataAccessProvider(ILogger<GrpcDataAccessProvider> logger)
        {
            Logger = logger;

            _clientConnectionManager = new ClientConnectionManager(logger, this);

            _clientElementValueListManager = new ClientElementValueListManager(logger);
            _clientElementValueJournalListManager = new ClientElementValueJournalListManager(logger);
            _clientEventListManager = new ClientEventListManager(logger);
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
        public string ApplicationName
        {
            get
            {
                if (!IsInitialized) throw new Exception("Not Initialized");
                return _applicationName;
            }
        }

        /// <summary>
        ///     Used in DataGrpc Context initialization.
        /// </summary>
        public string WorkstationName
        {
            get
            {
                if (!IsInitialized) throw new Exception("Not Initialized");
                return _workstationName;
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
            get { return _isConnected; }
        }

        public GrpcChannel? GrpcChannel
        {
            get { return _clientConnectionManager.GrpcChannel; }
        }

        public bool IsInitialized { get; private set; }

        public Guid DataGuid { get; private set; }

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
        /// <param name="applicationName"></param>
        /// <param name="workstationName"></param>
        /// <param name="systemNames"></param>
        /// <param name="contextParams"></param>
        public void Initialize(IDispatcher? сallbackDispatcher, bool elementValueListCallbackIsEnabled, string serverAddress,
            string applicationName, string workstationName, string systemNameToConnect, CaseInsensitiveDictionary<string> contextParams)
        {
            Close();            

            Logger.LogDebug("Starting ModelDataProvider. сallbackDispatcher != null " + (сallbackDispatcher != null).ToString());

            _сallbackDispatcher = сallbackDispatcher;
            _elementValueListCallbackIsEnabled = elementValueListCallbackIsEnabled;
            _serverAddress = serverAddress;            
            _applicationName = applicationName;            
            _workstationName = workstationName;
            _systemNameToConnect = systemNameToConnect;
            _contextParams = contextParams;            

            //string pollIntervalMsString =
            //    ConfigurationManager.AppSettings["PollIntervalMs"];
            //if (!String.IsNullOrWhiteSpace(pollIntervalMsString) &&
            //    Int32.TryParse(pollIntervalMsString, out int pollIntervalMs))
            //{
            //    _pollIntervalMs = pollIntervalMs;
            //}

            _cancellationTokenSource = new CancellationTokenSource();

            _isConnected = false;

            _workingThread = new Thread(() => WorkingThreadMain(_cancellationTokenSource.Token));
            _workingThread.IsBackground = false;
            _workingThread.Start();

            IsInitialized = true;
        }

        public void Close()
        {
            if (!IsInitialized) return;

            IsInitialized = false;

            _contextParams = new CaseInsensitiveDictionary<string>();
            _сallbackDispatcher = null;

            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource = null;
            }

            if (_workingThread != null) _workingThread.Join(30000);
        }

        /// <summary>        
        ///     Returns id actully used for OPC subscription, always as original id.
        ///     valueSubscription.Update() is called using сallbackDispatcher, see Initialize(..).        
        /// </summary>
        /// <param name="elementId"></param>
        /// <param name="valueSubscription"></param>
        public string AddItem(string elementId, IValueSubscription valueSubscription)
        {
            Logger.LogDebug("DataGrpcProvider.AddItem() " + elementId);

            valueSubscription.Obj = new ValueSubscriptionObj
            {
                ElementId = elementId
            };

            BeginInvoke(ct => _clientElementValueListManager.AddItem(elementId, valueSubscription));

            return elementId;
        }

        /// <summary>        
        ///     If valueSubscription is not subscribed - does nothing.
        /// </summary>
        /// <param name="valueSubscription"></param>
        public void RemoveItem(IValueSubscription valueSubscription)
        {
            valueSubscription.Obj = null;

            BeginInvoke(ct => _clientElementValueListManager.RemoveItem(valueSubscription));
        }

        /// <summary>        
        ///     setResultAction(..) is called using сallbackDispatcher, see Initialize(..).
        ///     If call to server failed setResultAction(null) is called, otherwise setResultAction(changedValueSubscriptions) is called.        
        /// </summary>
        public void PollElementValuesChanges(Action<IValueSubscription[]?> setResultAction)
        {
            BeginInvoke(ct =>
            {                
                _clientElementValueListManager.Subscribe(_clientConnectionManager, _сallbackDispatcher,
                    ClientElementValueListItemsManagerOnElementValuesCallback, true, ct);
                object[]? changedValueSubscriptions = _clientElementValueListManager.PollChanges();
                IDispatcher? сallbackDispatcher = _сallbackDispatcher;
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
        ///     setResultAction(..) is called using сallbackDispatcher, see Initialize(..).
        ///     setResultAction(failedValueSubscriptions) is called, failedValueSubscriptions != null.
        ///     If connection error, failedValueSubscriptions is all clientObjs.        
        /// </summary>
        public void Write(IValueSubscription[] valueSubscriptions, ValueStatusTimestamp[] valueStatusTimestamps, Action<IValueSubscription[]>? setResultAction)
        {
            BeginInvoke(ct =>
            {                
                _clientElementValueListManager.Subscribe(_clientConnectionManager, _сallbackDispatcher,
                    ClientElementValueListItemsManagerOnElementValuesCallback, true, ct);                
                object[] failedValueSubscriptions = _clientElementValueListManager.Write(valueSubscriptions, valueStatusTimestamps);

                if (setResultAction != null)
                {
                    IDispatcher? сallbackDispatcher = _сallbackDispatcher;
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
        /// </summary>
        /// <param name="valueSubscription"></param>
        /// <param name="value"></param>
        public void Write(IValueSubscription valueSubscription, ValueStatusTimestamp valueStatusTimestamp)
        {
            BeginInvoke(ct =>
            {                
                _clientElementValueListManager.Subscribe(_clientConnectionManager, _сallbackDispatcher,
                    ClientElementValueListItemsManagerOnElementValuesCallback, true, ct);
                _clientElementValueListManager.Write(valueSubscription, valueStatusTimestamp);
            }
            );
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
                    uint resultCode = _clientConnectionManager.Passthrough(recipientId, passthroughName,
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

                IDispatcher? сallbackDispatcher = _сallbackDispatcher;
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

        #region private functions

        private void WorkingThreadMain(CancellationToken ct)
        {
            while (true)
            {
                if (ct.WaitHandle.WaitOne(10)) break;

                OnLoopInWorkingThread(ct);
            }

            Logger.LogDebug("Unsubscribing");

            UnsubscribeInWorkingThread();

            Logger.LogDebug("End Unsubscribing");

            Logger.LogDebug("Disconnecting");

            if (_onEventMessagesCallbackSubscribed) _clientEventListManager.EventMessagesCallback -= OnEventMessagesCallback;            

            Logger.LogDebug("End Disconnecting");
        }        

        private void OnLoopInWorkingThread(CancellationToken cancellationToken)
        {
            _threadSafeDispatcher.InvokeActionsInQueue(cancellationToken);

            DateTime nowUtc = DateTime.UtcNow;

            if (cancellationToken.IsCancellationRequested) return;
            if (!_clientConnectionManager.ConnectionExists)
            {
                IDispatcher? сallbackDispatcher;
                if (_isConnected)
                {
                    UnsubscribeInWorkingThread();

                    #region notify subscribers disconnected

                    Logger.LogInformation("DataGrpcProvider diconnected");

                    _isConnected = false;
                    Action disconnected = Disconnected;
                    сallbackDispatcher = _сallbackDispatcher;
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
                        _clientElementValueListManager.GetAllClientObjs().OfType<IValueSubscription>();

                    сallbackDispatcher = _сallbackDispatcher;
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
                        //if (Logger.ShouldTrace(System.Diagnostics.TraceEventType.Verbose))
                        //{
                        //    _logger.LogDebug("Connecting. Server Address: {0}. ApplicationName: {1}. WorkstationName: {2}. Params: {3}",
                        //        _serverAddress,
                        //        _applicationName,
                        //        _workstationName,
                        //        //NameValueCollectionHelper.GetNameValueCollectionString(_dataGrpcContextParams.OfType<string?>()));
                        //}
                        

                        _clientConnectionManager.InitiateConnection(_serverAddress, _applicationName,
                            _workstationName, _systemNameToConnect, _contextParams);                        

                        Logger.LogDebug("End Connecting");

                        Logger.LogInformation("DataGrpcProvider connected to " + _serverAddress);

                        _isConnected = true;
                        Action connected = Connected;
                        сallbackDispatcher = _сallbackDispatcher;
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
                    }
                }
            }

            if (cancellationToken.IsCancellationRequested) return;
            _clientElementValueListManager.Subscribe(_clientConnectionManager, _сallbackDispatcher,
                ClientElementValueListItemsManagerOnElementValuesCallback, _elementValueListCallbackIsEnabled, cancellationToken);
            _clientEventListManager.Subscribe(_clientConnectionManager, _сallbackDispatcher, true, cancellationToken);

            if (cancellationToken.IsCancellationRequested) return;
            if (_clientConnectionManager.ConnectionExists)
            {                
                try
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    _clientConnectionManager.Process(cancellationToken, nowUtc);
                }
                catch
                {
                }
            }            
        }

        /// <summary>
        /// </summary>
        private void UnsubscribeInWorkingThread()
        { 
            _clientElementValueListManager.Unsubscribe();
            _clientEventListManager.Unsubscribe();
            _clientElementValueJournalListManager.Unsubscribe();
            
            _clientConnectionManager.CloseConnection();
        }

        /// <summary>
        ///     Called using сallbackDispatcher.
        /// </summary>
        /// <param name="changedClientObjs"></param>
        /// <param name="changedValues"></param>
        private void ClientElementValueListItemsManagerOnElementValuesCallback(object[] changedClientObjs,
            ValueStatusTimestamp[] changedValues)
        {
            if (changedClientObjs != null)
            {
                for (int i = 0; i < changedClientObjs.Length; i++)
                {
                    var changedValueSubscription = (IValueSubscription) changedClientObjs[i];
                    if (changedValueSubscription.Obj == null) continue;
                    //var valueSubscriptionObj = (ValueSubscriptionObj)changedValueSubscription.Obj;

                    changedValueSubscription.Update(changedValues[i]);                    
                }
                DataGuid = Guid.NewGuid();
            }

            ValueSubscriptionsUpdated();
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
        private string _applicationName = "";

        /// <summary>
        ///     Used in DataGrpc ElementValueList initialization.
        /// </summary>
        private bool _elementValueListCallbackIsEnabled;

        /// <summary>
        ///     Used in DataGrpc Context initialization.
        /// </summary>
        private string _workstationName = "";

        /// <summary>
        ///     Used in DataGrpc Context initialization.
        /// </summary>
        private CaseInsensitiveDictionary<string> _contextParams = new();

        private volatile bool _isConnected;

        private Thread? _workingThread;

        private CancellationTokenSource? _cancellationTokenSource;

        private readonly ClientConnectionManager _clientConnectionManager;

        private readonly ClientElementValueListManager _clientElementValueListManager;

        private volatile IDispatcher? _сallbackDispatcher;

        private ThreadSafeDispatcher _threadSafeDispatcher = new();

        private DateTime _lastFailedConnectionDateTimeUtc = DateTime.MinValue;

        #endregion

        private class ValueSubscriptionObj
        {            
            public string ElementId = "";
        }
    }
}