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
    public partial class XiDataAccessProvider : DisposableViewModelBase, IDataAccessProvider, IDispatcher
    {
        #region construction and destruction

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

        public object? Obj { get; set; }

        /// <summary>
        ///     Is called using сallbackDoer, see Initialize(..).
        /// </summary>
        public event Action ValueSubscriptionsUpdated = delegate { };
        
        public void Initialize(IDispatcher? callbackDispatcher,
            ElementIdsMap? elementIdsMap,
            bool elementValueListCallbackIsEnabled,
            bool eventListCallbackIsEnabled,
            string serverAddress,
            string clientApplicationName, string clientWorkstationName, string systemNameToConnect, CaseInsensitiveDictionary<string> contextParams)
        {
            Close();            

            //Logger?.LogDebug("Starting ModelDataProvider. сallbackDoer is not null " + (сallbackDispatcher is not null).ToString());

            _callbackDispatcher = callbackDispatcher;
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
        }

        /// <summary>
        ///     Tou can call Dispose() instead of this method.
        /// </summary>
        public void Close()
        {
            if (!IsInitialized) return;

            IsInitialized = false;

            _contextParams = new CaseInsensitiveDictionary<string>();
            _callbackDispatcher = null;

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

            Initialize(_callbackDispatcher,
                null,
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
                if (_xiServerProxy is null) throw new InvalidOperationException();
                _xiDataListItemsManager.Subscribe(_xiServerProxy, _callbackDispatcher,
                    XiDataListItemsManagerOnElementValuesCallback, true, ct);
                object[]? changedValueSubscriptions = _xiDataListItemsManager.PollChanges();
                IDispatcher? сallbackDoer = _callbackDispatcher;
                if (сallbackDoer is not null)
                {
                    try
                    {
                        сallbackDoer.BeginInvoke(ct => setResultAction(changedValueSubscriptions is not null ? changedValueSubscriptions.OfType<IValueSubscription>().ToArray() : null));
                    }
                    catch (Exception)
                    {
                    }
                }                
            });
        }

        /// <summary>        
        ///     setResultAction(..) is called using сallbackDoer, see Initialize(..).
        ///     setResultAction(failedValueSubscriptions) is called, failedValueSubscriptions is not null.
        ///     If connection error, failedValueSubscriptions is all clientObjs.        
        /// </summary>
        public void Write(IValueSubscription[] valueSubscriptions, ValueStatusTimestamp[] valueStatusTimestamps, Action<IValueSubscription[]>? setResultAction)
        {
            BeginInvoke(ct =>
            {
                if (_xiServerProxy is null) throw new InvalidOperationException();
                _xiDataListItemsManager.Subscribe(_xiServerProxy, _callbackDispatcher,
                    XiDataListItemsManagerOnElementValuesCallback, true, ct);                
                object[] failedValueSubscriptions = _xiDataListItemsManager.Write(valueSubscriptions, valueStatusTimestamps);

                if (setResultAction is not null)
                {
                    IDispatcher? сallbackDoer = _callbackDispatcher;
                    if (сallbackDoer is not null)
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
        /// <param name="alternativeLogger"></param>
        public void Write(IValueSubscription valueSubscription, ValueStatusTimestamp valueStatusTimestamp, ILogger? alternativeLogger)
        {
            BeginInvoke(ct =>
            {
                if (_xiServerProxy is null) throw new InvalidOperationException();
                _xiDataListItemsManager.Subscribe(_xiServerProxy, _callbackDispatcher,
                    XiDataListItemsManagerOnElementValuesCallback, true, ct);
                _xiDataListItemsManager.Write(valueSubscription, valueStatusTimestamp);
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
                byte[]? result;
                try
                {
                    if (_xiServerProxy is null) throw new InvalidOperationException();
                    PassthroughResult? passthroughResult = _xiServerProxy.Passthrough(recipientId, passthroughName,
                        dataToSend);
                    if (passthroughResult is not null && passthroughResult.ResultCode == 0) // SUCCESS
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
            Action<LongrunningPassthroughCallback>? callbackAction)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            BeginInvoke(async ct =>
            {
                bool succeeded;
                IDispatcher? сallbackDispatcher = _callbackDispatcher;
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
            _threadSafeDispatcher.InvokeActionsInQueue(cancellationToken);

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

                    сallbackDispatcher = _callbackDispatcher;
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

                        _xiServerProxy.InitiateXiContext(_serverAddress, _clientApplicationName,
                            workstationName, this);

                        //Logger?.LogDebug("End Connecting");

                        //Logger.Info("XiDataProvider connected to " + _serverAddress);

                        _isConnectedEventWaitHandle.Set();
                        сallbackDispatcher = _callbackDispatcher;
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
            _xiDataListItemsManager.Subscribe(_xiServerProxy, _callbackDispatcher,
                XiDataListItemsManagerOnElementValuesCallback, _elementValueListCallbackIsEnabled, cancellationToken);
            _xiEventListItemsManager.Subscribe(_xiServerProxy, _callbackDispatcher, true, cancellationToken);

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

            var сallbackDispatcher = _callbackDispatcher;
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
                    if (changedValueSubscription.Obj is null) continue;
                    //var valueSubscriptionObj = (ValueSubscriptionObj)changedValueSubscription.Obj;

                    changedValueSubscription.Update(changedValues[i]);                    
                }
                DataGuid = Guid.NewGuid();
            }

            ValueSubscriptionsUpdated();
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

        private volatile IDispatcher? _callbackDispatcher;

        private ThreadSafeDispatcher _threadSafeDispatcher = new();

        private DateTime _pollLastCallUtc = DateTime.MinValue;

        private int _pollIntervalMs = 1000; 

        private DateTime _lastFailedConnectionDateTimeUtc = DateTime.MinValue;

        private DateTime _lastSuccessfulConnectionDateTimeUtc = DateTime.MinValue;

        #endregion

        private class ValueSubscriptionObj
        {            
            public string Id = "";
        }
    }
}