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
    ///     Must notify changes in IsInitialized, IsConnected and IsDisconnected properties.
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
        ///     If guid the same, the data is guaranteed not to have changed.
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
        /// </summary>
        void Close();

        /// <summary>
        ///     Tou can call DisposeAsync() instead of this method.
        /// </summary>
        Task CloseAsync();

        string AddItem(string elementId, IValueSubscription valueSubscription);
        
        void RemoveItem(IValueSubscription valueSubscription);

        void PollElementValuesChanges(Action<IValueSubscription[]?> setResultAction);

        /// <summary>
        ///     No values mapping and conversion.
        /// </summary>
        /// <param name="valueSubscriptions"></param>
        /// <param name="valueStatusTimestamps"></param>
        /// <param name="setResultAction"></param>
        void Write(IValueSubscription[] valueSubscriptions, ValueStatusTimestamp[] valueStatusTimestamps, Action<IValueSubscription[]>? setResultAction);
        
        void Write(IValueSubscription valueSubscription, ValueStatusTimestamp valueStatusTimestamp, ILogger? alternativeLogger);

        /// <summary>
        ///     Returns null if any errors.
        /// </summary>
        /// <param name="recipientId"></param>
        /// <param name="passthroughName"></param>
        /// <param name="dataToSend"></param>
        /// <param name="setResultAction"></param>
        void Passthrough(string recipientId, string passthroughName, byte[] dataToSend,
            Action<IEnumerable<byte>?> setResultAction);

        /// <summary>
        ///     Returns null if any errors.
        /// </summary>
        /// <param name="recipientId"></param>
        /// <param name="passthroughName"></param>
        /// <param name="dataToSend"></param>
        /// <returns></returns>
        Task<IEnumerable<byte>?> PassthroughAsync(string recipientId, string passthroughName, byte[] dataToSend);

        /// <summary>
        ///     Returns true, if succeeded.
        /// </summary>
        /// <param name="recipientId"></param>
        /// <param name="commandName"></param>
        /// <param name="commandParams"></param>
        /// <param name="setResultAction"></param>
        void Command(string recipientId, string commandName, string commandParams,
            Action<bool> setResultAction);

        Task<bool> CommandAsync(string recipientId, string commandName, string commandParams);

        void JournalAddItem(string elementId, object valueJournalSubscription);

        void JournalRemoveItem(object valueJournalSubscription);

        void ReadElementValueJournals(DateTime firstTimestampUtc, DateTime secondTimestampUtc, uint numValuesPerDataObject, TypeId calculation, object[] valueJournalSubscriptions,
            Action<ValueStatusTimestamp[][]?> setResultAction);

        event Action<EventMessage[]> EventMessagesCallback;

        void AckAlarms(string operatorName, string comment, EventId[] eventIdsToAck);        
    }
}
