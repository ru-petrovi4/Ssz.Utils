using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Threading;
using Ssz.Utils;
using Ssz.Xi.Client.Api;
using Xi.Contracts.Constants;
using Xi.Contracts.Data;

namespace Ssz.Xi.Client
{
    public partial class XiDataProvider : IDataSource, ICallbackDoer
    {
        #region public functions

        /// <summary>
        ///     Xi Server connection string.
        /// </summary>
        public string ServerDiscoveryEndpointHttpUrl
        {
            get
            {
                if (!IsInitialized) throw new Exception("Not Initialized");
                return _serverDiscoveryEndpointHttpUrl;
            }
        }

        /// <summary>
        ///     Xi System Name.
        /// </summary>
        public string XiSystem
        {
            get
            {
                if (!IsInitialized) throw new Exception("Not Initialized");
                return _xiSystem;
            }
        }

        /// <summary>
        ///     Used in Xi Context initialization.
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
        ///     Used in Xi Context initialization.
        /// </summary>
        public string WorkstationNameWithoutArgs
        {
            get
            {
                if (!IsInitialized) throw new Exception("Not Initialized");
                return _workstationNameWithoutArgs;
            }
        }

        /// <summary>
        ///     Used in Xi Context initialization.
        ///     Can be null
        /// </summary>
        public CaseInsensitiveDictionary<string?> XiContextParams
        {
            get
            {
                if (!IsInitialized) throw new Exception("Not Initialized");
                return _xiContextParams;
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
        ///     You can set updateDataItems = false and invoke PollDataChanges(...) manually.       
        /// </summary>
        /// <param name="сallbackDoer">ICallbackDoer? for doing all callbacks.</param>
        /// <param name="updateDataItems">Used in Xi DataList initialization</param>
        /// <param name="serverDiscoveryEndpointHttpUrl">Xi Server connection string</param>
        /// <param name="xiSystem">Xi System Name</param>
        /// <param name="applicationName">Used in Xi Context initialization</param>
        /// <param name="workstationNameWithoutArgs">Used in Xi Context initialization</param>
        /// <param name="xiContextParams">Used in Xi Context initialization</param>
        public void Initialize(ICallbackDoer? сallbackDoer, bool updateDataItems, string serverDiscoveryEndpointHttpUrl,
            string xiSystem, string applicationName, string workstationNameWithoutArgs, CaseInsensitiveDictionary<string?>? xiContextParams = null)
        {
            Close();            

            Logger.Verbose("Starting ModelDataProvider. сallbackDoer != null " + (сallbackDoer != null).ToString());

            _сallbackDoer = сallbackDoer;
            _updateDataItems = updateDataItems;
            _serverDiscoveryEndpointHttpUrl = serverDiscoveryEndpointHttpUrl;
            _xiSystem = xiSystem;
            _xiDataListItemsManager.XiSystem = _xiSystem;
            _xiEventListItemsManager.XiSystem = _xiSystem;
            _xiDataJournalListItemsManager.XiSystem = _xiSystem;
            _applicationName = applicationName;            
            _workstationNameWithoutArgs = workstationNameWithoutArgs;            
            if (xiContextParams != null) _xiContextParams = xiContextParams;            

            //string pollIntervalMsString =
            //    ConfigurationManager.AppSettings["PollIntervalMs"];
            //if (!String.IsNullOrWhiteSpace(pollIntervalMsString) &&
            //    Int32.TryParse(pollIntervalMsString, out int pollIntervalMs))
            //{
            //    _pollIntervalMs = pollIntervalMs;
            //}
            _pollIntervalMs = 1000;

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

            _xiContextParams = new CaseInsensitiveDictionary<string?>();
            _сallbackDoer = null;

            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource = null;
            }

            if (_workingThread != null) _workingThread.Join(30000);
        }

        public InstanceId GetDaInstanceId(string id)
        {
            return new InstanceId(InstanceIds.ResourceType_DA, XiSystem, id);
        }

        public InstanceId GetAeInstanceId(string id)
        {
            return new InstanceId(InstanceIds.ResourceType_AE, XiSystem, id);
        }

        public InstanceId GetHdaInstanceId(string id)
        {
            return new InstanceId(InstanceIds.ResourceType_HDA, XiSystem, id);
        }

        /// <summary>        
        ///     Returns id actully used for OPC subscription, always as original id.
        ///     valueSubscription.Update() is called using сallbackDoer, see Initialize(..).        
        /// </summary>
        /// <param name="id"></param>
        /// <param name="valueSubscription"></param>
        public string AddItem(string id, IValueSubscription valueSubscription)
        {
            Logger.Verbose("XiDataProvider.AddItem() " + id);

            valueSubscription.Obj = new ValueSubscriptionObj
            {
                Id = id
            };

            BeginInvoke(ct => _xiDataListItemsManager.AddItem(id, valueSubscription));

            return id;
        }

        /// <summary>        
        ///     If valueSubscription is not subscribed - does nothing.
        /// </summary>
        /// <param name="valueSubscription"></param>
        public void RemoveItem(IValueSubscription valueSubscription)
        {
            valueSubscription.Obj = null;

            BeginInvoke(ct => _xiDataListItemsManager.RemoveItem(valueSubscription));
        }

        /// <summary>        
        ///     setResultAction(..) is called using сallbackDoer, see Initialize(..).
        ///     If call to server failed setResultAction(null) is called, otherwise setResultAction(changedValueSubscriptions) is called.        
        /// </summary>
        public void PollDataChanges(Action<IValueSubscription[]?> setResultAction)
        {
            BeginInvoke(ct =>
            {
                if (_xiServerProxy == null) throw new InvalidOperationException();
                _xiDataListItemsManager.Subscribe(_xiServerProxy, _сallbackDoer,
                    XiDataListItemsManagerOnInformationReport, true, ct);
                object[]? changedValueSubscriptions = _xiDataListItemsManager.PollChanges();
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
            });
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
                if (_xiServerProxy == null) throw new InvalidOperationException();
                _xiDataListItemsManager.Subscribe(_xiServerProxy, _сallbackDoer,
                    XiDataListItemsManagerOnInformationReport, true, ct);                
                object[] failedValueSubscriptions = _xiDataListItemsManager.Write(valueSubscriptions, values, utcNow);

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
            });
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
                if (_xiServerProxy == null) throw new InvalidOperationException();
                _xiDataListItemsManager.Subscribe(_xiServerProxy, _сallbackDoer,
                    XiDataListItemsManagerOnInformationReport, true, ct);
                _xiDataListItemsManager.Write(valueSubscription, value, utcNow);
            });
        }

        /// <summary>        
        ///     setResultAction(..) is called using сallbackDoer, see Initialize(..).
        ///     If call to server failed (exception or passthroughResult == null or passthroughResult.ResultCode != 0), setResultAction(null) is called.        
        /// </summary>
        public void Passthrough(Action<byte[]?> setResultAction, string recipientId, string passthroughName,
            byte[] dataToSend)
        {
            BeginInvoke(ct =>
            {
                byte[]? result;
                try
                {
                    if (_xiServerProxy == null) throw new InvalidOperationException();
                    PassthroughResult? passthroughResult = _xiServerProxy.Passthrough(recipientId, 0, passthroughName,
                        dataToSend);
                    if (passthroughResult != null && passthroughResult.ResultCode == 0) // SUCCESS
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
            });
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
            _xiServerProxy = new XiServerProxy();
            
            while (true)
            {
                if (ct.WaitHandle.WaitOne(10)) break;

                OnLoopInWorkingThread(ct);
            }

            Logger.Verbose("Unsubscribing");

            UnsubscribeInWorkingThread();

            Logger.Verbose("End Unsubscribing");

            Logger.Verbose("Disconnecting");

            if (_onEventNotificationSubscribed) _xiEventListItemsManager.EventNotification -= OnEventNotification;
            _xiServerProxy.Dispose();
            _xiServerProxy = null;

            Logger.Verbose("End Disconnecting");
        }        

        private void OnLoopInWorkingThread(CancellationToken ct)
        {
            var actionsForWorkingThread = Interlocked.Exchange(ref _actionsForWorkingThread, delegate { });
            actionsForWorkingThread.Invoke(ct);

            DateTime nowUtc = DateTime.UtcNow;

            if (_xiServerProxy == null) throw new InvalidOperationException();

            if (ct.IsCancellationRequested) return;
            if (!_xiServerProxy.ContextExists)
            {
                ICallbackDoer? сallbackDoer;
                if (_isConnected)
                {
                    UnsubscribeInWorkingThread();

                    #region notify subscribers disconnected

                    Logger.Info("XiDataProvider diconnected");

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
                        _xiDataListItemsManager.GetAllClientObjs().OfType<IValueSubscription>();

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

                if (!String.IsNullOrWhiteSpace(_serverDiscoveryEndpointHttpUrl) &&
                    nowUtc > _lastFailedConnectionDateTimeUtc + TimeSpan.FromSeconds(5))
                {
                    try
                    {
                        string workstationName = _workstationNameWithoutArgs;
                        string xiContextParamsString =
                            NameValueCollectionHelper.GetNameValueCollectionString(_xiContextParams);
                        if (!String.IsNullOrEmpty(xiContextParamsString))
                        {
                            workstationName += @"?" + xiContextParamsString;
                        }

                        Logger.Verbose("Connecting. Endpoint: {0}. ApplicationName: {1}. WorkstationName: {2}",
                            _serverDiscoveryEndpointHttpUrl,
                            _applicationName,
                            workstationName);

                        _xiServerProxy.InitiateXiContext(_serverDiscoveryEndpointHttpUrl, _applicationName,
                            workstationName, this);                        

                        Logger.Verbose("End Connecting");

                        Logger.Info("XiDataProvider connected to " + _serverDiscoveryEndpointHttpUrl);

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
                        Logger.Verbose(ex);

                        _lastFailedConnectionDateTimeUtc = DateTime.UtcNow;
                    }
                }
            }

            if (ct.IsCancellationRequested) return;
            _xiDataListItemsManager.Subscribe(_xiServerProxy, _сallbackDoer,
                XiDataListItemsManagerOnInformationReport, _updateDataItems, ct);
            _xiEventListItemsManager.Subscribe(_xiServerProxy, _сallbackDoer, true, ct);

            if (ct.IsCancellationRequested) return;
            if (_xiServerProxy.ContextExists)
            {                
                try
                {
                    if (ct.IsCancellationRequested) return;
                    _xiServerProxy.KeepContextAlive(nowUtc);
                    
                    var timeDiffInMs = (uint) (nowUtc - _pollLastCallUtc).TotalMilliseconds;
                    bool pollExpired = timeDiffInMs >= _pollIntervalMs;

                    if (pollExpired)
                    {
                        if (ct.IsCancellationRequested) return;

                        if (_updateDataItems)
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
        /// </summary>
        private void UnsubscribeInWorkingThread()
        { 
            _xiDataListItemsManager.Unsubscribe();
            _xiEventListItemsManager.Unsubscribe();
            _xiDataJournalListItemsManager.Unsubscribe();

            if (_xiServerProxy == null) throw new InvalidOperationException();
            _xiServerProxy.ConcludeXiContext();
        }

        /// <summary>
        ///     Called using сallbackDoer.
        /// </summary>
        /// <param name="changedClientObjs"></param>
        /// <param name="changedValues"></param>
        private void XiDataListItemsManagerOnInformationReport(object[] changedClientObjs,
            XiValueStatusTimestamp[] changedValues)
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

        /// <summary>
        ///     Xi Server connection string.
        /// </summary>
        private string _serverDiscoveryEndpointHttpUrl = "";

        /// <summary>
        ///     Xi System Name.
        /// </summary>
        private string _xiSystem = "";

        /// <summary>
        ///     Used in Xi Context initialization.
        /// </summary>
        private string _applicationName = "";

        /// <summary>
        ///     Used in Xi DataList initialization.
        /// </summary>
        private bool _updateDataItems;

        /// <summary>
        ///     Used in Xi Context initialization.
        /// </summary>
        private string _workstationNameWithoutArgs = "";

        /// <summary>
        ///     Used in Xi Context initialization.
        /// </summary>
        private CaseInsensitiveDictionary<string?> _xiContextParams = new CaseInsensitiveDictionary<string?>();

        private volatile bool _isConnected;

        private Thread? _workingThread;

        private CancellationTokenSource? _cancellationTokenSource;

        private XiServerProxy? _xiServerProxy;

        private readonly XiDataListItemsManager _xiDataListItemsManager = new XiDataListItemsManager();

        private volatile ICallbackDoer? _сallbackDoer;

        private Action<CancellationToken> _actionsForWorkingThread = delegate { };

        private DateTime _pollLastCallUtc = DateTime.MinValue;
        private int _pollIntervalMs = 1000; 
        private DateTime _lastFailedConnectionDateTimeUtc = DateTime.MinValue;

        #endregion

        private class ValueSubscriptionObj
        {            
            public string Id = "";
        }
    }
}