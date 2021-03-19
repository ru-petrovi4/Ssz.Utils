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

namespace Ssz.DataGrpc.Client
{
    public partial class DataGrpcProvider : IDataSource, ICallbackDoer
    {
        #region construction and destruction

        public DataGrpcProvider(ILogger<DataGrpcProvider> logger)
        {
            _logger = logger;

            _dataGrpcServerManager = new DataGrpcServerManager(logger);

            _dataGrpcElementValueListItemsManager = new DataGrpcElementValueListItemsManager(logger);
            _dataGrpcElementValueJournalListItemsManager = new DataGrpcElementValueJournalListItemsManager(logger);
            _dataGrpcEventListItemsManager = new DataGrpcEventListItemsManager(logger);
        }

        #endregion

        #region public functions

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
        public string[] SystemNames
        {
            get
            {
                if (!IsInitialized) throw new Exception("Not Initialized");
                return _systemNames;
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
        public CaseInsensitiveDictionary<string> DataGrpcContextParams
        {
            get
            {
                if (!IsInitialized) throw new Exception("Not Initialized");
                return _dataGrpcContextParams;
            }
        }

        /// <summary>
        ///     Is connected to Model Data Source.
        /// </summary>
        public bool IsConnected
        {
            get { return _isConnected; }
        }

        public bool IsInitialized { get; private set; }

        public Guid ModelDataGuid { get; private set; }

        /// <summary>
        ///     Is called using сallbackDoer, see Initialize(..).
        /// </summary>
        public event Action ValueSubscriptionsUpdated = delegate { };

        /// <summary>
        ///     Is called using сallbackDoer, see Initialize(..).
        ///     Occurs after connected to model.
        /// </summary>
        public event Action Connected = delegate { };

        /// <summary>
        ///     Is called using сallbackDoer, see Initialize(..).
        ///     Occurs after disconnected from model.
        /// </summary>
        public event Action Disconnected = delegate { };

        /// <summary>
        ///     You can set updateValueItems = false and invoke PollDataChanges(...) manually.       
        /// </summary>
        /// <param name="сallbackDoer">ICallbackDoer? for doing all callbacks.</param>
        /// <param name="tagValuesCallbackIsEnabled">Used in DataGrpc ElementValueList initialization</param>
        /// <param name="serverAddress">DataGrpc Server connection string</param>
        /// <param name="dataGrpcSystem">DataGrpc System Name</param>
        /// <param name="applicationName">Used in DataGrpc Context initialization</param>
        /// <param name="workstationName">Used in DataGrpc Context initialization</param>
        /// <param name="dataGrpcContextParams">Used in DataGrpc Context initialization</param>
        public void Initialize(ICallbackDoer? сallbackDoer, bool tagValuesCallbackIsEnabled, string serverAddress,
            string applicationName, string workstationName, string[] systemNames, CaseInsensitiveDictionary<string>? dataGrpcContextParams = null)
        {
            Close();            

            _logger.LogDebug("Starting ModelDataProvider. сallbackDoer != null " + (сallbackDoer != null).ToString());

            _сallbackDoer = сallbackDoer;
            _tagValuesCallbackIsEnabled = tagValuesCallbackIsEnabled;
            _serverAddress = serverAddress;            
            _applicationName = applicationName;            
            _workstationName = workstationName;
            _systemNames = systemNames;
            if (dataGrpcContextParams != null) _dataGrpcContextParams = dataGrpcContextParams;            

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

            _dataGrpcContextParams = new CaseInsensitiveDictionary<string>();
            _сallbackDoer = null;

            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource = null;
            }

            if (_workingThread != null) _workingThread.Join(30000);
        }

        /// <summary>        
        ///     Returns id actully used for OPC subscription, always as original id.
        ///     valueSubscription.Update() is called using сallbackDoer, see Initialize(..).        
        /// </summary>
        /// <param name="elementId"></param>
        /// <param name="valueSubscription"></param>
        public string AddItem(string elementId, IValueSubscription valueSubscription)
        {
            _logger.LogDebug("DataGrpcProvider.AddItem() " + elementId);

            valueSubscription.Obj = new ValueSubscriptionObj
            {
                ElementId = elementId
            };

            BeginInvoke(ct => _dataGrpcElementValueListItemsManager.AddItem(elementId, valueSubscription));

            return elementId;
        }

        /// <summary>        
        ///     If valueSubscription is not subscribed - does nothing.
        /// </summary>
        /// <param name="valueSubscription"></param>
        public void RemoveItem(IValueSubscription valueSubscription)
        {
            valueSubscription.Obj = null;

            BeginInvoke(ct => _dataGrpcElementValueListItemsManager.RemoveItem(valueSubscription));
        }

        /// <summary>        
        ///     setResultAction(..) is called using сallbackDoer, see Initialize(..).
        ///     If call to server failed setResultAction(null) is called, otherwise setResultAction(changedValueSubscriptions) is called.        
        /// </summary>
        public void PollDataChanges(Action<IValueSubscription[]?> setResultAction)
        {
            BeginInvoke(ct =>
            {                
                _dataGrpcElementValueListItemsManager.Subscribe(_dataGrpcServerManager, _сallbackDoer,
                    DataGrpcElementValueListItemsManagerOnInformationReport, true, ct);
                object[]? changedValueSubscriptions = _dataGrpcElementValueListItemsManager.PollChanges();
                ICallbackDoer? сallbackDoer = _сallbackDoer;
                if (сallbackDoer != null)
                {
                    try
                    {
                        сallbackDoer.BeginInvoke(ct => setResultAction(changedValueSubscriptions != null ? changedValueSubscriptions.OfType<IValueSubscription>().ToArray() : null));
                    }
                    catch (Exception)
                    {
                    }
                }                
            }
            );
        }

        /// <summary>        
        ///     setResultAction(..) is called using сallbackDoer, see Initialize(..).
        ///     setResultAction(failedValueSubscriptions) is called, failedValueSubscriptions != null.
        ///     If connection error, failedValueSubscriptions is all clientObjs.        
        /// </summary>
        public void Write(IValueSubscription[] valueSubscriptions, Any[] values, Action<IValueSubscription[]>? setResultAction)
        {
            DateTime utcNow = DateTime.UtcNow;

            BeginInvoke(ct =>
            {                
                _dataGrpcElementValueListItemsManager.Subscribe(_dataGrpcServerManager, _сallbackDoer,
                    DataGrpcElementValueListItemsManagerOnInformationReport, true, ct);                
                object[] failedValueSubscriptions = _dataGrpcElementValueListItemsManager.Write(valueSubscriptions, values, utcNow);

                if (setResultAction != null)
                {
                    ICallbackDoer? сallbackDoer = _сallbackDoer;
                    if (сallbackDoer != null)
                    {
                        try
                        {
                            сallbackDoer.BeginInvoke(ct => setResultAction(failedValueSubscriptions.OfType<IValueSubscription>().ToArray()));
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
        public void Write(IValueSubscription valueSubscription, Any value)
        {
            DateTime utcNow = DateTime.UtcNow;

            BeginInvoke(ct =>
            {                
                _dataGrpcElementValueListItemsManager.Subscribe(_dataGrpcServerManager, _сallbackDoer,
                    DataGrpcElementValueListItemsManagerOnInformationReport, true, ct);
                _dataGrpcElementValueListItemsManager.Write(valueSubscription, value, utcNow);
            }
            );
        }

        /// <summary>        
        ///     setResultAction(..) is called using сallbackDoer, see Initialize(..).
        ///     If call to server failed (exception or passthroughResult.ResultCode != 0), setResultAction(null) is called.        
        /// </summary>
        public void Passthrough(string recipientId, string passthroughName, byte[] dataToSend,
            Action<byte[]?> setResultAction)
        {
            BeginInvoke(ct =>
            {
                byte[]? result;
                try
                {                   
                    PassthroughResult passthroughResult = _dataGrpcServerManager.Passthrough(recipientId, passthroughName,
                        dataToSend);
                    if (passthroughResult.ResultCode == 0) // SUCCESS
                    {
                        result = passthroughResult.ReturnData;
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

                ICallbackDoer? сallbackDoer = _сallbackDoer;
                if (сallbackDoer != null)
                {
                    try
                    {
                        сallbackDoer.BeginInvoke(ct => setResultAction(result));
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
            _actionsForWorkingThread += action;
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

            _logger.LogDebug("Unsubscribing");

            UnsubscribeInWorkingThread();

            _logger.LogDebug("End Unsubscribing");

            _logger.LogDebug("Disconnecting");

            if (_onEventNotificationSubscribed) _dataGrpcEventListItemsManager.EventNotification -= OnEventNotification;            

            _logger.LogDebug("End Disconnecting");
        }        

        private void OnLoopInWorkingThread(CancellationToken ct)
        {
            var actionsForWorkingThread = Interlocked.Exchange(ref _actionsForWorkingThread, delegate { });
            actionsForWorkingThread.Invoke(ct);

            DateTime nowUtc = DateTime.UtcNow;

            if (ct.IsCancellationRequested) return;
            if (!_dataGrpcServerManager.ConnectionExists)
            {
                ICallbackDoer? сallbackDoer;
                if (_isConnected)
                {
                    UnsubscribeInWorkingThread();

                    #region notify subscribers disconnected

                    Logger.Info("DataGrpcProvider diconnected");

                    _isConnected = false;
                    Action disconnected = Disconnected;
                    сallbackDoer = _сallbackDoer;
                    if (disconnected != null && сallbackDoer != null)
                    {
                        if (ct.IsCancellationRequested) return;
                        try
                        {
                            сallbackDoer.BeginInvoke(ct => disconnected());
                        }
                        catch (Exception)
                        {
                        }
                    }

                    IEnumerable<IValueSubscription> valueSubscriptions =
                        _dataGrpcElementValueListItemsManager.GetAllClientObjs().OfType<IValueSubscription>();

                    сallbackDoer = _сallbackDoer;
                    if (сallbackDoer != null)
                    {
                        if (ct.IsCancellationRequested) return;
                        try
                        {
                            сallbackDoer.BeginInvoke(ct =>
                            {
                                foreach (IValueSubscription valueSubscription in valueSubscriptions)
                                {
                                    valueSubscription.Update(new Any(null));
                                }
                                ModelDataGuid = Guid.NewGuid();

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
                        

                        _dataGrpcServerManager.InitiateConnection(_serverAddress, _applicationName,
                            _workstationName, _systemNames, _dataGrpcContextParams);                        

                        _logger.LogDebug("End Connecting");

                        Logger.Info("DataGrpcProvider connected to " + _serverAddress);

                        _isConnected = true;
                        Action connected = Connected;
                        сallbackDoer = _сallbackDoer;
                        if (connected != null && сallbackDoer != null)
                        {
                            if (ct.IsCancellationRequested) return;
                            try
                            {
                                сallbackDoer.BeginInvoke(ct => connected());
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "");

                        _lastFailedConnectionDateTimeUtc = DateTime.UtcNow;
                    }
                }
            }

            if (ct.IsCancellationRequested) return;
            _dataGrpcElementValueListItemsManager.Subscribe(_dataGrpcServerManager, _сallbackDoer,
                DataGrpcElementValueListItemsManagerOnInformationReport, _tagValuesCallbackIsEnabled, ct);
            _dataGrpcEventListItemsManager.Subscribe(_dataGrpcServerManager, _сallbackDoer, true, ct);

            if (ct.IsCancellationRequested) return;
            if (_dataGrpcServerManager.ConnectionExists)
            {                
                try
                {
                    if (ct.IsCancellationRequested) return;
                    _dataGrpcServerManager.Process(nowUtc);
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
            _dataGrpcElementValueListItemsManager.Unsubscribe();
            _dataGrpcEventListItemsManager.Unsubscribe();
            _dataGrpcElementValueJournalListItemsManager.Unsubscribe();
            
            _dataGrpcServerManager.CloseConnection();
        }

        /// <summary>
        ///     Called using сallbackDoer.
        /// </summary>
        /// <param name="changedClientObjs"></param>
        /// <param name="changedValues"></param>
        private void DataGrpcElementValueListItemsManagerOnInformationReport(object[] changedClientObjs,
            DataGrpcValueStatusTimestamp[] changedValues)
        {
            if (changedClientObjs != null)
            {
                for (int i = 0; i < changedClientObjs.Length; i++)
                {
                    var changedValueSubscription = (IValueSubscription) changedClientObjs[i];
                    if (changedValueSubscription.Obj == null) continue;
                    //var valueSubscriptionObj = (ValueSubscriptionObj)changedValueSubscription.Obj;

                    changedValueSubscription.Update(changedValues[i].Value);                    
                }
                ModelDataGuid = Guid.NewGuid();
            }

            ValueSubscriptionsUpdated();
        }

        #endregion

        #region private fields

        private ILogger<DataGrpcProvider> _logger;

        /// <summary>
        ///     DataGrpc Server connection string.
        /// </summary>
        private string _serverAddress = "";

        /// <summary>
        ///     DataGrpc Systems Names.
        /// </summary>
        private string[] _systemNames = new string[0];

        /// <summary>
        ///     Used in DataGrpc Context initialization.
        /// </summary>
        private string _applicationName = "";

        /// <summary>
        ///     Used in DataGrpc ElementValueList initialization.
        /// </summary>
        private bool _tagValuesCallbackIsEnabled;

        /// <summary>
        ///     Used in DataGrpc Context initialization.
        /// </summary>
        private string _workstationName = "";

        /// <summary>
        ///     Used in DataGrpc Context initialization.
        /// </summary>
        private CaseInsensitiveDictionary<string> _dataGrpcContextParams = new CaseInsensitiveDictionary<string>();

        private volatile bool _isConnected;

        private Thread? _workingThread;

        private CancellationTokenSource? _cancellationTokenSource;

        private readonly DataGrpcServerManager _dataGrpcServerManager;

        private readonly DataGrpcElementValueListItemsManager _dataGrpcElementValueListItemsManager;

        private volatile ICallbackDoer? _сallbackDoer;

        private Action<CancellationToken> _actionsForWorkingThread = delegate { };        
        
        private DateTime _lastFailedConnectionDateTimeUtc = DateTime.MinValue;

        #endregion

        private class ValueSubscriptionObj
        {            
            public string ElementId = "";
        }
    }
}