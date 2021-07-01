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
            get { return _isConnected; }
        }

        public GrpcChannel? GrpcChannel
        {
            get { return ClientConnectionManager.GrpcChannel; }
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
        /// <param name="clientApplicationName"></param>
        /// <param name="clientWorkstationName"></param>
        /// <param name="systemNames"></param>
        /// <param name="contextParams"></param>
        public virtual void Initialize(IDispatcher? сallbackDispatcher,
            bool elementValueListCallbackIsEnabled,
            bool eventListCallbackIsEnabled,
            string serverAddress,
            string clientApplicationName, string clientWorkstationName, string systemNameToConnect, CaseInsensitiveDictionary<string> contextParams)
        {
            Close();            

            Logger.LogDebug("Starting ModelDataProvider. сallbackDispatcher != null " + (сallbackDispatcher != null).ToString());

            CallbackDispatcher = сallbackDispatcher;
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

            _cancellationTokenSource = new CancellationTokenSource();

            _isConnected = false;

            _workingThread = new Thread(() => WorkingThreadMain(_cancellationTokenSource.Token));
            _workingThread.IsBackground = false;
            _workingThread.Start();

            IsInitialized = true;
        }

        public virtual void Close()
        {
            if (!IsInitialized) return;

            IsInitialized = false;

            _contextParams = new CaseInsensitiveDictionary<string>();
            CallbackDispatcher = null;

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
        public virtual string AddItem(string elementId, IValueSubscription valueSubscription)
        {
            Logger.LogDebug("DataGrpcProvider.AddItem() " + elementId);

            valueSubscription.Obj = new ValueSubscriptionObj
            {
                ElementId = elementId
            };

            BeginInvoke(ct => ClientElementValueListManager.AddItem(elementId, valueSubscription));

            return elementId;
        }

        /// <summary>        
        ///     If valueSubscription is not subscribed - does nothing.
        /// </summary>
        /// <param name="valueSubscription"></param>
        public virtual void RemoveItem(IValueSubscription valueSubscription)
        {
            valueSubscription.Obj = null;

            BeginInvoke(ct => ClientElementValueListManager.RemoveItem(valueSubscription));
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
        /// </summary>
        /// <param name="valueSubscription"></param>
        /// <param name="value"></param>
        public virtual void Write(IValueSubscription valueSubscription, ValueStatusTimestamp valueStatusTimestamp)
        {
            BeginInvoke(ct =>
            {                
                ClientElementValueListManager.Subscribe(ClientConnectionManager, CallbackDispatcher,
                    OnElementValuesCallback, true, ct);
                ClientElementValueListManager.Write(valueSubscription, valueStatusTimestamp);
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

        protected ClientConnectionManager ClientConnectionManager { get; }

        protected ClientElementValueListManager ClientElementValueListManager { get; }

        protected DateTime LastValueSubscriptionsUpdatedDateTimeUtc { get; private set; } = DateTime.MinValue;

        /// <summary>
        ///     On loop in working thread.
        /// </summary>
        /// <param name="cancellationToken"></param>
        protected virtual void Execute(CancellationToken cancellationToken)
        {
            _threadSafeDispatcher.InvokeActionsInQueue(cancellationToken);

            DateTime nowUtc = DateTime.UtcNow;

            if (cancellationToken.IsCancellationRequested) return;
            if (!ClientConnectionManager.ConnectionExists)
            {
                IDispatcher? сallbackDispatcher;
                if (_isConnected)
                {
                    Unsubscribe();

                    #region notify subscribers disconnected

                    Logger.LogInformation("DataGrpcProvider diconnected");

                    _isConnected = false;
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

                        _isConnected = true;
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

        #endregion        

        #region private functions

        private void WorkingThreadMain(CancellationToken ct)
        {
            if (_eventListCallbackIsEnabled) ClientEventListManager.EventMessagesCallback += OnEventMessagesCallbackInternal;

            while (true)
            {
                if (ct.WaitHandle.WaitOne(10)) break;

                Execute(ct);
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

        private volatile bool _isConnected;

        private Thread? _workingThread;

        private CancellationTokenSource? _cancellationTokenSource;

        private ThreadSafeDispatcher _threadSafeDispatcher = new();

        private DateTime _lastFailedConnectionDateTimeUtc = DateTime.MinValue;

        #endregion

        private class ValueSubscriptionObj
        {            
            public string ElementId = "";
        }
    }
}