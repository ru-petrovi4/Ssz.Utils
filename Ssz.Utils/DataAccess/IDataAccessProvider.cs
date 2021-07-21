using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Utils.DataAccess
{
    public interface IDataAccessProvider: IDisposable
    {
        string ServerAddress { get; }

        string SystemNameToConnect { get; }

        string ClientApplicationName { get; }

        string ClientWorkstationName { get; }

        CaseInsensitiveDictionary<string> ContextParams { get; }

        bool IsConnected { get; }

        bool IsInitialized { get; }

        Guid DataGuid { get; }

        /// <summary>
        ///     You can use this property as temp storage.
        /// </summary>
        object? Obj { get; set; }

        event Action ValueSubscriptionsUpdated;

        event Action Connected;

        event Action Disconnected;

        void Initialize(IDispatcher? сallbackDispatcher,
            TagsMap? tagsMap,
            bool elementValueListCallbackIsEnabled,
            bool eventListCallbackIsEnabled,
            string serverAddress,
            string clientApplicationName, string clientWorkstationName, string systemNameToConnect, CaseInsensitiveDictionary<string> contextParams);

        void Close();
        
        string AddItem(string elementId, IValueSubscription valueSubscription);
        
        void RemoveItem(IValueSubscription valueSubscription);

        void PollElementValuesChanges(Action<IValueSubscription[]?> setResultAction);

        void Write(IValueSubscription[] valueSubscriptions, ValueStatusTimestamp[] valueStatusTimestamps, Action<IValueSubscription[]>? setResultAction);
        
        void Write(IValueSubscription valueSubscription, ValueStatusTimestamp valueStatusTimestamp);

        void Passthrough(string recipientId, string passthroughName, byte[] dataToSend,
            Action<IEnumerable<byte>?> setResultAction);

        void JournalAddItem(string elementId, object valueJournalSubscription);

        void JournalRemoveItem(object valueJournalSubscription);

        void ReadElementValueJournals(DateTime firstTimestampUtc, DateTime secondTimestampUtc, uint numValuesPerDataObject, TypeId calculation, object[] valueJournalSubscriptions,
            Action<ValueStatusTimestamp[][]?> setResultAction);

        event Action<EventMessage[]> EventMessagesCallback;

        void AckAlarms(string operatorName, string comment, EventId[] eventIdsToAck);        
    }
}
