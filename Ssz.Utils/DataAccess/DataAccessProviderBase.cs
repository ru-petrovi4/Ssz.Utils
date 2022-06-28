﻿using Microsoft.Extensions.Logging;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.Utils.DataAccess
{
    public class DataAccessProviderBase : DisposableViewModelBase, IDataAccessProvider
    {
        #region construction and destruction

        public DataAccessProviderBase(ILogger logger, IDispatcher? callbackDispatcher)
        {
            Logger = logger;
            CallbackDispatcher = callbackDispatcher;
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

        public ElementIdsMap? ElementIdsMap
        {
            get
            {
                if (!IsInitialized) throw new Exception("Not Initialized");
                return _elementIdsMap;
            }
        }

        /// <summary>
        ///     Used in DataAccessGrpc ElementValueList initialization.
        /// </summary>
        public bool ElementValueListCallbackIsEnabled
        {
            get
            {
                if (!IsInitialized) throw new Exception("Not Initialized");
                return _elementValueListCallbackIsEnabled;
            }
        }

        public bool EventListCallbackIsEnabled
        {
            get
            {
                if (!IsInitialized) throw new Exception("Not Initialized");
                return _eventListCallbackIsEnabled;
            }
        }

        /// <summary>
        ///     DataAccessGrpc Server connection string.
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
        ///     DataAccessGrpc Systems Names.
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
        ///     Used in DataAccessGrpc Context initialization.
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
        ///     Used in DataAccessGrpc Context initialization.
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
        ///     Used in DataAccessGrpc Context initialization.
        ///     Can be null
        /// </summary>
        public CaseInsensitiveDictionary<string?> ContextParams
        {
            get
            {
                if (!IsInitialized) throw new Exception("Not Initialized");
                return _contextParams;
            }
        }

        public virtual string ContextId => "1";

        public bool IsInitialized
        {
            get { return _isInitialized; }
            private set { SetValue(ref _isInitialized, value); }
        }

        public bool IsConnected
        {
            get { return _isConnected; }
            protected set
            {
                if (SetValue(ref _isConnected, value))
                    OnPropertyChanged(nameof(IsDisconnected));
            }
        }

        public bool IsDisconnected
        {
            get { return !_isConnected; }
        }

        public EventWaitHandle IsConnectedEventWaitHandle { get; } = new ManualResetEvent(false);

        public DateTime InitializedDateTimeUtc { get; private set; }

        public DateTime LastFailedConnectionDateTimeUtc { get; protected set; }

        public DateTime LastSuccessfulConnectionDateTimeUtc { get; protected set; }

        public Guid DataGuid { get; protected set; }

        public object? Obj { get; set; }

        public virtual event Action<IDataAccessProvider> ValueSubscriptionsUpdated = delegate { };

        public virtual event Action<IDataAccessProvider, EventMessage[]> EventMessagesCallback = delegate { };

        public virtual void AckAlarms(string operatorName, string comment, Ssz.Utils.DataAccess.EventId[] eventIdsToAck)
        {
        }

        public virtual void AddItem(string? elementId, IValueSubscription valueSubscription)
        {
            var callbackDispatcher = CallbackDispatcher;
            if (callbackDispatcher is not null)
                try
                {
                    callbackDispatcher.BeginInvoke(ct =>
                        valueSubscription.Update(new ValueStatusTimestamp { ValueStatusCode = ValueStatusCode.ItemDoesNotExist }));
                }
                catch (Exception)
                {
                }
        }

        public virtual void Close()
        {
            if (!IsInitialized) return;

            IsInitialized = false;

            _elementIdsMap = null;
            _contextParams = new CaseInsensitiveDictionary<string?>();            
        }

        public virtual Task CloseAsync()
        {
            Close();

            return Task.CompletedTask;
        }

        public virtual void Initialize(ElementIdsMap? elementIdsMap, bool elementValueListCallbackIsEnabled, bool eventListCallbackIsEnabled, string serverAddress, string clientApplicationName, string clientWorkstationName, string systemNameToConnect, CaseInsensitiveDictionary<string?> contextParams)
        {
            Close();

            Logger.LogDebug("Starting ModelDataProvider. сallbackDispatcher != null: " + (CallbackDispatcher is not null).ToString());

            _elementIdsMap = elementIdsMap;
            _elementValueListCallbackIsEnabled = elementValueListCallbackIsEnabled;
            _eventListCallbackIsEnabled = eventListCallbackIsEnabled;
            _serverAddress = serverAddress;
            _clientApplicationName = clientApplicationName;
            _clientWorkstationName = clientWorkstationName;
            _systemNameToConnect = systemNameToConnect;
            _contextParams = contextParams;

            InitializedDateTimeUtc = DateTime.UtcNow;

            IsInitialized = true;
        }

        public virtual void JournalAddItem(string elementId, object valueJournalSubscription)
        {            
        }

        public virtual void JournalRemoveItem(object valueJournalSubscription)
        {            
        }

        public virtual Task<bool> LongrunningPassthroughAsync(string recipientId, string passthroughName, byte[] dataToSend, Action<LongrunningPassthroughCallback>? progressCallbackAction)
        {            
            return Task.FromResult(true);
        }

        public virtual Task<IEnumerable<byte>> PassthroughAsync(string recipientId, string passthroughName, byte[] dataToSend)
        {
            return Task.FromResult<IEnumerable<byte>>(new byte[0]);
        }

        public virtual Task<IValueSubscription[]?> PollElementValuesChangesAsync()
        {
            return Task.FromResult<IValueSubscription[]?>(null);
        }

        public virtual Task<ValueStatusTimestamp[][]?> ReadElementValuesJournalsAsync(DateTime firstTimestampUtc, DateTime secondTimestampUtc, uint numValuesPerSubscription, TypeId? calculation, CaseInsensitiveDictionary<string>? params_, object[] valueJournalSubscriptions)
        {
            return Task.FromResult<ValueStatusTimestamp[][]?>(null);
        }

        public virtual Task<EventMessage[]?> ReadEventMessagesJournalAsync(DateTime firstTimestampUtc, DateTime secondTimestampUtc, CaseInsensitiveDictionary<string>? params_)
        {
            return Task.FromResult<EventMessage[]?>(null);
        }

        public virtual void ReInitialize()
        {
            if (!IsInitialized) return;

            Initialize(_elementIdsMap,
                _elementValueListCallbackIsEnabled,
                _eventListCallbackIsEnabled,
                _serverAddress,
                _clientApplicationName, _clientWorkstationName, _systemNameToConnect, _contextParams);
        }

        public virtual void RemoveItem(IValueSubscription valueSubscription)
        {            
        }

        public virtual void Write(IValueSubscription valueSubscription, ValueStatusTimestamp valueStatusTimestamp, ILogger? userFriendlyLogger)
        {            
        }

        public virtual Task<IValueSubscription[]> WriteAsync(IValueSubscription[] valueSubscriptions, ValueStatusTimestamp[] valueStatusTimestamps)
        {
            return Task.FromResult<IValueSubscription[]>(new IValueSubscription[0]);
        }

        #endregion

        #region protected functions

        protected ILogger Logger { get; }

        /// <summary>
        ///     Dispatcher for callbacks to client.
        /// </summary>
        protected IDispatcher? CallbackDispatcher { get; }

        #endregion

        #region private fields

        private ElementIdsMap? _elementIdsMap;

        /// <summary>
        ///     Used in DataAccessGrpc ElementValueList initialization.
        /// </summary>
        private bool _elementValueListCallbackIsEnabled;

        private bool _eventListCallbackIsEnabled;

        /// <summary>
        ///     DataAccessGrpc Server connection string.
        /// </summary>
        private string _serverAddress = "";

        /// <summary>
        ///     DataAccessGrpc Systems Names.
        /// </summary>
        private string _systemNameToConnect = @"";

        /// <summary>
        ///     Used in DataAccessGrpc Context initialization.
        /// </summary>
        private string _clientApplicationName = "";

        /// <summary>
        ///     Used in DataAccessGrpc Context initialization.
        /// </summary>
        private string _clientWorkstationName = "";

        /// <summary>
        ///     Used in DataAccessGrpc Context initialization.
        /// </summary>
        private CaseInsensitiveDictionary<string?> _contextParams = new();

        private bool _isInitialized;

        private bool _isConnected;

        #endregion
    }
}
