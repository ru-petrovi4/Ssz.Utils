﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Threading;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using Ssz.Xi.Client.Api;
using Xi.Contracts.Constants;
using Xi.Contracts.Data;

namespace Ssz.Xi.Client
{
    public partial class XiDataAccessProvider : IDataAccessProvider, IDispatcher
    {
        #region construction and destruction
        
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
        ~XiDataAccessProvider()
        {
            Dispose(false);
        }

        public bool Disposed { get; private set; }

        #endregion

        #region public functions

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
                return _applicationName;
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
                return _workstationName;
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

        /// <summary>
        ///     Is connected to Model Data Source.
        /// </summary>
        public bool IsConnected
        {
            get { return _isConnected; }
        }

        public bool IsInitialized { get; private set; }

        public Guid DataGuid { get; private set; }

        public object? Obj { get; set; }

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
        
        public void Initialize(IDispatcher? сallbackDispatcher,
            bool elementValueListCallbackIsEnabled,
            bool eventListCallbackIsEnabled,
            string serverAddress,
            string clientApplicationName, string clientWorkstationName, string systemNameToConnect, CaseInsensitiveDictionary<string> contextParams)
        {
            Close();            

            //Logger?.LogDebug("Starting ModelDataProvider. сallbackDoer != null " + (сallbackDispatcher != null).ToString());

            _сallbackDispatcher = сallbackDispatcher;
            _elementValueListCallbackIsEnabled = elementValueListCallbackIsEnabled;
            _eventListCallbackIsEnabled = eventListCallbackIsEnabled;
            _serverAddress = serverAddress;
            _systemNameToConnect = systemNameToConnect;
            _xiDataListItemsManager.XiSystem = _systemNameToConnect;
            _xiEventListItemsManager.XiSystem = _systemNameToConnect;
            _xiDataJournalListItemsManager.XiSystem = _systemNameToConnect;
            _applicationName = clientApplicationName;            
            _workstationName = clientWorkstationName;            
            if (contextParams != null) _contextParams = contextParams;            

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
        ///     valueSubscription.Update() is called using сallbackDoer, see Initialize(..).        
        /// </summary>
        /// <param name="elementId"></param>
        /// <param name="valueSubscription"></param>
        public string AddItem(string elementId, IValueSubscription valueSubscription)
        {
            //Logger?.LogDebug("XiDataProvider.AddItem() " + elementId);

            valueSubscription.Obj = new ValueSubscriptionObj
            {
                Id = elementId
            };

            BeginInvoke(ct => _xiDataListItemsManager.AddItem(elementId, valueSubscription));

            return elementId;
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
        public void PollElementValuesChanges(Action<IValueSubscription[]?> setResultAction)
        {
            BeginInvoke(ct =>
            {
                if (_xiServerProxy == null) throw new InvalidOperationException();
                _xiDataListItemsManager.Subscribe(_xiServerProxy, _сallbackDispatcher,
                    XiDataListItemsManagerOnElementValuesCallback, true, ct);
                object[]? changedValueSubscriptions = _xiDataListItemsManager.PollChanges();
                IDispatcher? сallbackDoer = _сallbackDispatcher;
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
        public void Write(IValueSubscription[] valueSubscriptions, ValueStatusTimestamp[] valueStatusTimestamps, Action<IValueSubscription[]>? setResultAction)
        {
            BeginInvoke(ct =>
            {
                if (_xiServerProxy == null) throw new InvalidOperationException();
                _xiDataListItemsManager.Subscribe(_xiServerProxy, _сallbackDispatcher,
                    XiDataListItemsManagerOnElementValuesCallback, true, ct);                
                object[] failedValueSubscriptions = _xiDataListItemsManager.Write(valueSubscriptions, valueStatusTimestamps);

                if (setResultAction != null)
                {
                    IDispatcher? сallbackDoer = _сallbackDispatcher;
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
        ///
        /// </summary>
        /// <param name="valueSubscription"></param>
        /// <param name="valueStatusTimestamp"></param>
        public void Write(IValueSubscription valueSubscription, ValueStatusTimestamp valueStatusTimestamp)
        {
            BeginInvoke(ct =>
            {
                if (_xiServerProxy == null) throw new InvalidOperationException();
                _xiDataListItemsManager.Subscribe(_xiServerProxy, _сallbackDispatcher,
                    XiDataListItemsManagerOnElementValuesCallback, true, ct);
                _xiDataListItemsManager.Write(valueSubscription, valueStatusTimestamp);
            });
        }

        /// <summary>        
        ///     setResultAction(..) is called using сallbackDoer, see Initialize(..).
        ///     If call to server failed (exception or passthroughResult == null or passthroughResult.ResultCode != 0), setResultAction(null) is called.        
        /// </summary>
        public void Passthrough(string recipientId, string passthroughName,
            byte[] dataToSend, Action<IEnumerable<byte>?> setResultAction)
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

                IDispatcher? сallbackDoer = _сallbackDispatcher;
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
            _threadSafeDispatcher.BeginInvoke(action);
        }

        #endregion

        #region private functions

        private void WorkingThreadMain(CancellationToken ct)
        {
            _xiServerProxy = new XiServerProxy();

            if (_eventListCallbackIsEnabled) _xiEventListItemsManager.EventMessagesCallback += OnEventMessagesCallback;

            while (true)
            {
                if (ct.WaitHandle.WaitOne(10)) break;

                Execute(ct);
            }            

            Unsubscribe();

            if (_eventListCallbackIsEnabled) _xiEventListItemsManager.EventMessagesCallback -= OnEventMessagesCallback;
            _xiServerProxy.Dispose();
            _xiServerProxy = null;
        }

        /// <summary>
        ///     On loop in working thread.
        /// </summary>
        /// <param name="cancellationToken"></param>
        private void Execute(CancellationToken cancellationToken)
        {
            _threadSafeDispatcher.InvokeActionsInQueue(cancellationToken);

            DateTime nowUtc = DateTime.UtcNow;

            if (_xiServerProxy == null) throw new InvalidOperationException();

            if (cancellationToken.IsCancellationRequested) return;
            if (!_xiServerProxy.ContextExists)
            {
                IDispatcher? сallbackDoer;
                if (_isConnected)
                {
                    Unsubscribe();

                    #region notify subscribers disconnected

                    //Logger.Info("XiDataProvider diconnected");

                    _isConnected = false;
                    Action disconnected = Disconnected;
                    сallbackDoer = _сallbackDispatcher;
                    if (disconnected != null && сallbackDoer != null)
                    {
                        if (cancellationToken.IsCancellationRequested) return;
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

                    сallbackDoer = _сallbackDispatcher;
                    if (сallbackDoer != null)
                    {
                        if (cancellationToken.IsCancellationRequested) return;
                        try
                        {
                            сallbackDoer.BeginInvoke(ct =>
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
                        string workstationName = _workstationName;
                        string xiContextParamsString =
                            NameValueCollectionHelper.GetNameValueCollectionString(new CaseInsensitiveDictionary<string?>(_contextParams.Select(kvp => new KeyValuePair<string, string?>(kvp.Key, kvp.Value))));
                        if (!String.IsNullOrEmpty(xiContextParamsString))
                        {
                            workstationName += @"?" + xiContextParamsString;
                        }

                        //Logger?.LogDebug("Connecting. Endpoint: {0}. ApplicationName: {1}. WorkstationName: {2}",
                        //    _serverAddress,
                        //    _applicationName,
                         //   workstationName);

                        _xiServerProxy.InitiateXiContext(_serverAddress, _applicationName,
                            workstationName, this);                        

                        //Logger?.LogDebug("End Connecting");

                        //Logger.Info("XiDataProvider connected to " + _serverAddress);

                        _isConnected = true;
                        Action connected = Connected;
                        сallbackDoer = _сallbackDispatcher;
                        if (connected != null && сallbackDoer != null)
                        {
                            if (cancellationToken.IsCancellationRequested) return;
                            try
                            {
                                сallbackDoer.BeginInvoke(ct => connected());
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                    catch
                    {
                        //Logger?.LogDebug(ex);

                        _lastFailedConnectionDateTimeUtc = DateTime.UtcNow;
                    }
                }
            }

            if (cancellationToken.IsCancellationRequested) return;
            _xiDataListItemsManager.Subscribe(_xiServerProxy, _сallbackDispatcher,
                XiDataListItemsManagerOnElementValuesCallback, _elementValueListCallbackIsEnabled, cancellationToken);
            _xiEventListItemsManager.Subscribe(_xiServerProxy, _сallbackDispatcher, true, cancellationToken);

            if (cancellationToken.IsCancellationRequested) return;
            if (_xiServerProxy.ContextExists)
            {                
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
        private void Unsubscribe()
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
        private void XiDataListItemsManagerOnElementValuesCallback(object[] changedClientObjs,
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
        private string _applicationName = "";

        /// <summary>
        ///     Used in Xi DataList initialization.
        /// </summary>
        private bool _elementValueListCallbackIsEnabled;

        private bool _eventListCallbackIsEnabled;

        /// <summary>
        ///     Used in Xi Context initialization.
        /// </summary>
        private string _workstationName = "";

        /// <summary>
        ///     Used in Xi Context initialization.
        /// </summary>
        private CaseInsensitiveDictionary<string> _contextParams = new CaseInsensitiveDictionary<string>();

        private volatile bool _isConnected;

        private Thread? _workingThread;

        private CancellationTokenSource? _cancellationTokenSource;

        private XiServerProxy? _xiServerProxy;

        private readonly XiDataListItemsManager _xiDataListItemsManager = new XiDataListItemsManager();

        private volatile IDispatcher? _сallbackDispatcher;

        private ThreadSafeDispatcher _threadSafeDispatcher = new();

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