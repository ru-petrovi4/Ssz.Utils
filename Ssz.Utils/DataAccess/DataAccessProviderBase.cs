using Microsoft.Extensions.Logging;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using Ssz.Utils.Logging;
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

        public DataAccessProviderBase(ILoggersSet loggersSet)
        {
            LoggersSet = loggersSet;        
        }

        /// <summary>
        ///     This method is invoked when the IDisposable.Dispose or Finalize actions are
        ///     requested.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (IsDisposed) return;

            if (disposing)
            {
                var t = CloseAsync();
            }

            base.Dispose(disposing);
        }

        protected override async ValueTask DisposeAsyncCore()
        {
            if (IsDisposed) return;

            await CloseAsync();

            await base.DisposeAsyncCore();
        }

        #endregion

        #region public functions    

        public ILoggersSet LoggersSet { get; }

        /// <summary>
        ///     Can be configured in map, 'TagAndPropertySeparator' key
        /// </summary>
        public string TagAndPropertySeparator { get; private set; } = @".";

        public ElementIdsMap? ElementIdsMap { get; private set; }

        /// <summary>
        ///     DataAccessGrpc Server connection string.
        /// </summary>
        public string ServerAddress { get; private set; } = @"";

        /// <summary>
        ///     DataAccessGrpc Systems Names.
        /// </summary>
        public string SystemNameToConnect { get; private set; } = @"";

        /// <summary>
        ///     Used in DataAccessGrpc Context initialization.
        /// </summary>
        public string ClientApplicationName { get; private set; } = @"";

        /// <summary>
        ///     Used in DataAccessGrpc Context initialization.
        /// </summary>
        public string ClientWorkstationName { get; private set; } = @"";

        /// <summary>
        ///     Used in DataAccessGrpc Context initialization.
        ///     Can be null
        /// </summary>
        public CaseInsensitiveDictionary<string?> ContextParams { get; private set; } = new();

        public DataAccessProviderOptions Options { get; private set; } = new();

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
                {
                    OnPropertyChanged(nameof(IsDisconnected));
                    if (_isConnected)
                        IsConnectedEventWaitHandle.Set();
                    else
                        IsConnectedEventWaitHandle.Reset();
                }
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

        public virtual event EventHandler<ContextStatusChangedEventArgs> ContextStatusChanged = delegate { };

        public virtual event EventHandler ValueSubscriptionsUpdated = delegate { };

        public virtual event EventHandler<EventMessagesCallbackEventArgs> EventMessagesCallback = delegate { };

        /// <summary>
        ///     You can set DataAccessProviderOptions.ElementValueListCallbackIsEnabled = false and invoke PollElementValuesChangesAsync(...) manually.
        /// </summary>
        /// <param name="elementIdsMap"></param>
        /// <param name="serverAddress"></param>
        /// <param name="clientApplicationName"></param>
        /// <param name="clientWorkstationName"></param>
        /// <param name="systemNameToConnect"></param>
        /// <param name="contextParams"></param>
        /// <param name="options"></param>
        /// <param name="callbackDispatcher"></param>
        public virtual void Initialize(
            ElementIdsMap? elementIdsMap,            
            string serverAddress,
            string clientApplicationName,
            string clientWorkstationName,
            string systemNameToConnect,
            CaseInsensitiveDictionary<string?> contextParams,
            DataAccessProviderOptions options,
            IDispatcher? callbackDispatcher)
        {
            if (IsInitialized)
                throw new InvalidOperationException(@"Must be not initialized (closed or newly created).");

            LoggersSet.Logger.LogDebug("Starting ModelDataProvider. сallbackDispatcher != null: " + (callbackDispatcher is not null).ToString());

            ElementIdsMap = elementIdsMap;                     
            ServerAddress = serverAddress;
            ClientApplicationName = clientApplicationName;
            ClientWorkstationName = clientWorkstationName;
            SystemNameToConnect = systemNameToConnect;
            ContextParams = contextParams;
            Options = options;
            if (callbackDispatcher is null)
                callbackDispatcher = new DefaultDispatcher();
            CallbackDispatcher = callbackDispatcher;

            InitializedDateTimeUtc = DateTime.UtcNow;

            IsInitialized = true;
        }

        public virtual async Task ReInitializeAsync()
        {
            if (!IsInitialized)
                throw new InvalidOperationException(@"Must be initialized.");

            var elementIdsMap = ElementIdsMap;
            var serverAddress = ServerAddress;
            var clientApplicationName = ClientApplicationName;
            var clientWorkstationName = ClientWorkstationName;
            var systemNameToConnect = SystemNameToConnect;
            var contextParams = ContextParams;
            var options = Options;
            var callbackDispatcher = CallbackDispatcher;

            await CloseAsync();

            Initialize(elementIdsMap,                
                serverAddress,
                clientApplicationName,
                clientWorkstationName,
                systemNameToConnect,
                contextParams,
                options,
                callbackDispatcher);
        }        

        public virtual Task CloseAsync()
        {
            if (!IsInitialized)
                return Task.CompletedTask;

            IsInitialized = false;

            ElementIdsMap = null;
            ContextParams = new CaseInsensitiveDictionary<string?>();
            CallbackDispatcher = null;

            return Task.CompletedTask;
        }

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
                        valueSubscription.Update(new ValueStatusTimestamp { StatusCode = StatusCodes.BadNodeIdUnknown }));
                }
                catch (Exception)
                {
                }
        }

        public virtual void JournalAddItem(string elementId, object valueJournalSubscription)
        {            
        }

        public virtual void JournalRemoveItem(object valueJournalSubscription)
        {            
        }

        /// <summary>
        ///     Returns StatusCode <see cref="StatusCodes"/>
        /// </summary>
        /// <param name="recipientId"></param>
        /// <param name="passthroughName"></param>
        /// <param name="dataToSend"></param>
        /// <param name="progressCallbackAction"></param>
        /// <returns></returns>
        public virtual Task<Task<uint>> LongrunningPassthroughAsync(string recipientId, string passthroughName, byte[] dataToSend, Action<LongrunningPassthroughCallback>? progressCallbackAction)
        {            
            return Task.FromResult(Task.FromResult(StatusCodes.Good));
        }

        /// <summary>
        ///     Throws if any errors.
        /// </summary>
        /// <param name="recipientId"></param>
        /// <param name="passthroughName"></param>
        /// <param name="dataToSend"></param>
        /// <returns></returns>
        public virtual Task<IEnumerable<byte>> PassthroughAsync(string recipientId, string passthroughName, byte[] dataToSend)
        {
            return Task.FromResult<IEnumerable<byte>>(new byte[0]);
        }

        public virtual Task<IValueSubscription[]?> PollElementValuesChangesAsync()
        {
            return Task.FromResult<IValueSubscription[]?>(null);
        }

        public virtual Task<ValueStatusTimestamp[][]?> ReadElementValuesJournalsAsync(DateTime firstTimestampUtc, DateTime secondTimestampUtc, uint numValuesPerSubscription, TypeId? calculation, CaseInsensitiveDictionary<string?>? params_, object[] valueJournalSubscriptions)
        {
            return Task.FromResult<ValueStatusTimestamp[][]?>(null);
        }

        public virtual Task<EventMessagesCollection?> ReadEventMessagesJournalAsync(DateTime firstTimestampUtc, DateTime secondTimestampUtc, CaseInsensitiveDictionary<string?>? params_)
        {
            return Task.FromResult<EventMessagesCollection?>(null);
        }        

        public virtual void RemoveItem(IValueSubscription valueSubscription)
        {            
        }

        /// <summary>
        ///     Returns ResultInfo.
        /// </summary>
        /// <param name="valueSubscription"></param>
        /// <param name="valueStatusTimestamp"></param>
        /// <param name="userFriendlyLogger"></param>
        /// <returns></returns>
        public virtual Task<ResultInfo> WriteAsync(IValueSubscription valueSubscription, ValueStatusTimestamp valueStatusTimestamp, ILogger? userFriendlyLogger)
        {
            return Task.FromResult(new ResultInfo { StatusCode = StatusCodes.BadInvalidArgument });
        }

        /// <summary>
        ///     No values mapping and conversion.       
        ///     Returns failed ValueSubscriptions and ResultInfos.
        ///     If connection error, all ValueSubscriptions are failed.
        /// </summary>
        /// <param name="valueSubscriptions"></param>
        /// <param name="valueStatusTimestamps"></param>
        /// <returns></returns>
        public virtual Task<(IValueSubscription[], ResultInfo[])> WriteAsync(IValueSubscription[] valueSubscriptions, ValueStatusTimestamp[] valueStatusTimestamps)
        {
            return Task.FromResult((new IValueSubscription[0], new ResultInfo[0]));
        }        

        #endregion

        #region protected functions        

        /// <summary>
        ///     Dispatcher for callbacks to client.
        /// </summary>
        protected IDispatcher? CallbackDispatcher { get; private set; }             

        #endregion

        #region private fields        

        private bool _isInitialized;

        private bool _isConnected;

        #endregion
    }
}
