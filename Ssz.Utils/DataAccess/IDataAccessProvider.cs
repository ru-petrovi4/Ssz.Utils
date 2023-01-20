using Microsoft.Extensions.Logging;
using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.Utils.DataAccess
{
    /// <summary>
    ///     Must notify changes in IsConnected and IsDisconnected properties.
    /// </summary>
    public interface IDataAccessProvider: INotifyPropertyChanged, IDisposable, IAsyncDisposable
    {
        ElementIdsMap? ElementIdsMap { get; }

        /// <summary>
        ///     Used in DataAccessGrpc ElementValueList initialization.
        /// </summary>
        bool ElementValueListCallbackIsEnabled { get; }

        bool EventListCallbackIsEnabled { get; }        

        string ServerAddress { get; }

        string SystemNameToConnect { get; }

        string ClientApplicationName { get; }

        string ClientWorkstationName { get; }

        CaseInsensitiveDictionary<string?> ContextParams { get; }

        bool IsInitialized { get; }

        bool IsConnected { get; }

        bool IsDisconnected { get; }

        bool IsDisposed { get; }

        EventWaitHandle IsConnectedEventWaitHandle { get; }

        DateTime InitializedDateTimeUtc { get; }

        DateTime LastFailedConnectionDateTimeUtc { get; }

        DateTime LastSuccessfulConnectionDateTimeUtc { get; }

        /// <summary>
        ///     If guid the same, the data is guaranteed not changed.
        /// </summary>
        Guid DataGuid { get; }        

        /// <summary>
        ///     You can use this property as temp storage.
        /// </summary>
        object? Obj { get; set; }

        event EventHandler<ContextStatusChangedEventArgs> ContextStatusChanged;

        event EventHandler ValueSubscriptionsUpdated;

        event EventHandler<EventMessagesCallbackEventArgs> EventMessagesCallback;

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
        void Initialize(ElementIdsMap? elementIdsMap,
            bool elementValueListCallbackIsEnabled,
            bool eventListCallbackIsEnabled,
            string serverAddress,
            string clientApplicationName,
            string clientWorkstationName,
            string systemNameToConnect,
            CaseInsensitiveDictionary<string?> contextParams,
            IDispatcher? callbackDispatcher);

        /// <summary>
        ///     Re-initializes this object with same settings.
        ///     Items must be added again.
        ///     If not initialized then does nothing.
        /// </summary>
        public void ReInitialize();

        /// <summary>
        ///     You can call Dispose() instead of this method.
        ///     Closes without waiting working thread exit.
        /// </summary>
        void Close();

        /// <summary>
        ///     Tou can call DisposeAsync() instead of this method.
        ///     Closes WITH waiting working thread exit.
        /// </summary>
        Task CloseAsync();

        void AddItem(string? elementId, IValueSubscription valueSubscription);
        
        void RemoveItem(IValueSubscription valueSubscription);

        /// <summary>                
        ///     If call to server failed returns null, otherwise returns changed ValueSubscriptions.        
        /// </summary>
        Task<IValueSubscription[]?> PollElementValuesChangesAsync();

        /// <summary>
        ///     Returns StatusCode (See Ssz.Utils.JobStatusCodes).
        /// </summary>
        /// <param name="valueSubscription"></param>
        /// <param name="valueStatusTimestamp"></param>
        /// <param name="userFriendlyLogger"></param>
        /// <returns></returns>
        Task<uint> WriteAsync(IValueSubscription valueSubscription, ValueStatusTimestamp valueStatusTimestamp, ILogger? userFriendlyLogger);

        /// <summary>     
        ///     No values mapping and conversion.       
        ///     Returns failed ValueSubscriptions and StatusCodes (See Ssz.Utils.JobStatusCodes).
        ///     If connection error, all ValueSubscriptions are failed.        
        /// </summary>
        /// <param name="valueSubscriptions"></param>
        /// <param name="valueStatusTimestamps"></param>
        /// <returns></returns>
        Task<(IValueSubscription[], uint[])> WriteAsync(IValueSubscription[] valueSubscriptions, ValueStatusTimestamp[] valueStatusTimestamps);

        /// <summary>
        ///     Throws if any errors.
        /// </summary>
        /// <param name="recipientId"></param>
        /// <param name="passthroughName"></param>
        /// <param name="dataToSend"></param>
        /// <returns></returns>
        Task<IEnumerable<byte>> PassthroughAsync(string recipientId, string passthroughName, byte[] dataToSend);

        /// <summary>
        ///     Returns true if succeeded.
        /// </summary>
        /// <param name="recipientId"></param>
        /// <param name="passthroughName"></param>
        /// <param name="dataToSend"></param>
        /// <param name="progressCallbackAction"></param>
        /// <returns></returns>
        Task<bool> LongrunningPassthroughAsync(string recipientId, string passthroughName, byte[] dataToSend, Action<LongrunningPassthroughCallback>? progressCallbackAction);

        void JournalAddItem(string elementId, object valueJournalSubscription);

        void JournalRemoveItem(object valueJournalSubscription);

        /// <summary>
        ///     Returns null if error.
        /// </summary>
        /// <param name="firstTimestampUtc"></param>
        /// <param name="secondTimestampUtc"></param>
        /// <param name="numValuesPerSubscription"></param>
        /// <param name="calculation"></param>
        /// <param name="params_"></param>
        /// <param name="valueJournalSubscriptions"></param>
        /// <returns></returns>
        Task<ValueStatusTimestamp[][]?> ReadElementValuesJournalsAsync(DateTime firstTimestampUtc, DateTime secondTimestampUtc, uint numValuesPerSubscription, TypeId? calculation, CaseInsensitiveDictionary<string?>? params_, object[] valueJournalSubscriptions);

        /// <summary>
        ///     Returns null if error.
        /// </summary>
        /// <param name="firstTimestampUtc"></param>
        /// <param name="secondTimestampUtc"></param>
        /// <param name="params_"></param>
        /// <returns></returns>
        Task<EventMessagesCollection?> ReadEventMessagesJournalAsync(DateTime firstTimestampUtc, DateTime secondTimestampUtc, CaseInsensitiveDictionary<string?>? params_);
        
        void AckAlarms(string operatorName, string comment, EventId[] eventIdsToAck);        
    }

    public class ContextStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        ///     See consts in <see cref="ContextStateCodes"/>
        /// </summary>
        public uint ContextStateCode { get; set; }

        public string Info { get; set; } = null!;

        /// <summary>
        ///     User-friendly status label
        /// </summary>
        public string Label { get; set; } = null!;

        /// <summary>
        ///     User-friendly status details
        /// </summary>
        public string Details { get; set; } = null!;
    }

    public static class ContextStateCodes
    {
        public const uint STATE_OPERATIONAL = 0;
        public const uint STATE_DIAGNOSTIC = 1;
        public const uint STATE_INITIALIZING = 2;
        public const uint STATE_FAULTED = 3;
        public const uint STATE_NEEDS_CONFIGURATION = 4;
        public const uint STATE_OUT_OF_SERVICE = 5;
        public const uint STATE_NOT_CONNECTED = 6;
        public const uint STATE_ABORTING = 7;
        public const uint STATE_NOT_OPERATIONAL = 8;
    }

    public class EventMessagesCallbackEventArgs : EventArgs
    {
        public EventMessagesCollection EventMessagesCollection { get; set; } = null!;
    }
}
