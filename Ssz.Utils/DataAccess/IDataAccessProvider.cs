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

        string ServerAddress { get; set; }

        string ServerHost { get; }

        string SystemNameToConnect { get; }

        string ClientApplicationName { get; }

        string ClientWorkstationName { get; }

        CaseInsensitiveDictionary<string?> ContextParams { get; }

        DataAccessProviderOptions Options { get; }

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
        ///     Precondition: must be not initializied (closed or newly created).        
        /// </summary>
        /// <param name="elementIdsMap"></param>
        /// <param name="serverAddress"></param>
        /// <param name="clientApplicationName"></param>
        /// <param name="clientWorkstationName"></param>
        /// <param name="systemNameToConnect"></param>
        /// <param name="contextParams"></param>
        /// <param name="options"></param>
        /// <param name="callbackDispatcher"></param>
        void Initialize(ElementIdsMap? elementIdsMap,            
            string serverAddress,
            string clientApplicationName,
            string clientWorkstationName,
            string systemNameToConnect,
            CaseInsensitiveDictionary<string?> contextParams,
            DataAccessProviderOptions options,
            IDispatcher? callbackDispatcher);

        Task UpdateContextParamsAsync(CaseInsensitiveDictionary<string?> contextParams);

        /// <summary>
        ///     Re-initializes this object with same settings.
        ///     Items must not be added again.
        ///     Precondition: must be initializied.   
        /// </summary>
        Task ReInitializeAsync();        

        /// <summary>
        ///     You can call DisposeAsync() or Dispose() instead of this method.        
        /// </summary>
        Task CloseAsync();

        void AddItem(string? elementId, IValueSubscription valueSubscription);
        
        void RemoveItem(IValueSubscription valueSubscription);

        /// <summary>                
        ///     If call to server failed returns null, otherwise returns changed ValueSubscriptions.        
        /// </summary>
        Task<IValueSubscription[]?> PollElementValuesChangesAsync();

        /// <summary>
        ///     Returns ResultInfo.
        /// </summary>
        /// <param name="valueSubscription"></param>
        /// <param name="valueStatusTimestamp"></param>        
        /// <returns></returns>
        Task<ResultInfo> WriteAsync(IValueSubscription valueSubscription, ValueStatusTimestamp valueStatusTimestamp);

        /// <summary>     
        ///     No values mapping and conversion.       
        ///     Returns failed ValueSubscriptions and ResultInfos.
        ///     If connection error, all ValueSubscriptions are failed.        
        /// </summary>
        /// <param name="valueSubscriptions"></param>
        /// <param name="valueStatusTimestamps"></param>
        /// <returns></returns>
        Task<(IValueSubscription[], ResultInfo[])> WriteAsync(IValueSubscription[] valueSubscriptions, ValueStatusTimestamp[] valueStatusTimestamps);

        /// <summary>
        ///     Throws if any errors.
        /// </summary>
        /// <param name="recipientPath"></param>
        /// <param name="passthroughName"></param>
        /// <param name="dataToSend"></param>
        /// <returns></returns>
        Task<ReadOnlyMemory<byte>> PassthroughAsync(string recipientPath, string passthroughName, ReadOnlyMemory<byte> dataToSend);

        /// <summary>
        ///     Returns StatusCode <see cref="StatusCodes"/>
        ///     No throws.
        /// </summary>
        /// <param name="recipientPath"></param>
        /// <param name="passthroughName"></param>
        /// <param name="dataToSend"></param>
        /// <param name="progressCallbackAction"></param>
        /// <returns></returns>
        Task<Task<uint>> LongrunningPassthroughAsync(string recipientPath, string passthroughName, ReadOnlyMemory<byte> dataToSend, Action<LongrunningPassthroughCallback>? progressCallbackAction);

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
        Task<ElementValuesJournal[]?> ReadElementValuesJournalsAsync(DateTime firstTimestampUtc, DateTime secondTimestampUtc, uint numValuesPerSubscription, TypeId? calculation, CaseInsensitiveDictionary<string?>? params_, object[] valueJournalSubscriptions);

        /// <summary>
        ///     Returns null if error.
        /// </summary>
        /// <param name="firstTimestampUtc"></param>
        /// <param name="secondTimestampUtc"></param>
        /// <param name="params_"></param>
        /// <returns></returns>
        Task<List<Utils.DataAccess.EventMessagesCollection>?> ReadEventMessagesJournalAsync(DateTime firstTimestampUtc, DateTime secondTimestampUtc, CaseInsensitiveDictionary<string?>? params_);
        
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

    public class DataAccessProviderOptions
    {
        public bool ElementValueListCallbackIsEnabled { get; set; } = true;

        public bool EventListCallbackIsEnabled { get; set; } = true;

        public bool UnsubscribeValueListItemsFromServer { get; set; } = true;

        public bool UnsubscribeValuesJournalListItemsFromServer { get; set; } = true;

        public bool DangerousAcceptAnyServerCertificate { get; set; } = true;
    }
}
