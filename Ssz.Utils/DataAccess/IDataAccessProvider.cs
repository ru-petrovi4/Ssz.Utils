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
        string ServerAddress { get; }

        string SystemNameToConnect { get; }

        string ClientApplicationName { get; }

        string ClientWorkstationName { get; }

        CaseInsensitiveDictionary<string> ContextParams { get; }

        string ContextId { get; }

        bool IsInitialized { get; }

        bool IsConnected { get; }

        bool IsDisconnected { get; }        

        EventWaitHandle IsConnectedEventWaitHandle { get; }

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

        event Action ValueSubscriptionsUpdated;

        void Initialize(IDispatcher? сallbackDispatcher,
            ElementIdsMap? elementIdsMap,
            bool elementValueListCallbackIsEnabled,
            bool eventListCallbackIsEnabled,
            string serverAddress,
            string clientApplicationName, string clientWorkstationName, string systemNameToConnect, CaseInsensitiveDictionary<string> contextParams);

        /// <summary>
        ///     Re-initializes this object with same settings.
        ///     Items must be added again.
        ///     If not initialized then does nothing.
        /// </summary>
        public void ReInitialize();

        /// <summary>
        ///     Tou can call Dispose() instead of this method.
        ///     Closes without waiting working thread exit.
        /// </summary>
        void Close();

        /// <summary>
        ///     Tou can call DisposeAsync() instead of this method.
        ///     Closes WITH waiting working thread exit.
        /// </summary>
        Task CloseAsync();

        void AddItem(string elementId, IValueSubscription valueSubscription);
        
        void RemoveItem(IValueSubscription valueSubscription);

        /// <summary>                
        ///     If call to server failed returns null, otherwise returns changed ValueSubscriptions.        
        /// </summary>
        Task<IValueSubscription[]?> PollElementValuesChangesAsync();

        void Write(IValueSubscription valueSubscription, ValueStatusTimestamp valueStatusTimestamp, ILogger? userFriendlyLogger);

        /// <summary>     
        ///     No values mapping and conversion.       
        ///     returns failed ValueSubscriptions.
        ///     If connection error, failed ValueSubscriptions is all clientObjs.        
        /// </summary>
        /// <param name="valueSubscriptions"></param>
        /// <param name="valueStatusTimestamps"></param>
        /// <returns></returns>
        Task<IValueSubscription[]> WriteAsync(IValueSubscription[] valueSubscriptions, ValueStatusTimestamp[] valueStatusTimestamps);

        /// <summary>
        ///     Returns null if any errors.
        /// </summary>
        /// <param name="recipientId"></param>
        /// <param name="passthroughName"></param>
        /// <param name="dataToSend"></param>
        /// <returns></returns>
        Task<IEnumerable<byte>?> PassthroughAsync(string recipientId, string passthroughName, byte[] dataToSend);

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
        Task<ValueStatusTimestamp[][]?> ReadElementValuesJournalsAsync(DateTime firstTimestampUtc, DateTime secondTimestampUtc, uint numValuesPerSubscription, TypeId calculation, CaseInsensitiveDictionary<string>? params_, object[] valueJournalSubscriptions);

        /// <summary>
        ///     Returns null if error.
        /// </summary>
        /// <param name="firstTimestampUtc"></param>
        /// <param name="secondTimestampUtc"></param>
        /// <param name="params_"></param>
        /// <returns></returns>
        Task<EventMessage[]?> ReadEventMessagesJournalAsync(DateTime firstTimestampUtc, DateTime secondTimestampUtc, CaseInsensitiveDictionary<string>? params_);

        event Action<EventMessage[]> EventMessagesCallback;

        void AckAlarms(string operatorName, string comment, EventId[] eventIdsToAck);        
    }
}
