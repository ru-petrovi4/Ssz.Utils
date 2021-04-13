using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Utils.DataAccess
{
    public interface IDataAccessProvider
    {
        string ServerAddress { get; }

        string SystemNameToConnect { get; }

        string ApplicationName { get; }

        string WorkstationName { get; }

        CaseInsensitiveDictionary<string> ContextParams { get; }

        bool IsConnected { get; }

        bool IsInitialized { get; }

        Guid DataGuid { get; }
        
        event Action ValueSubscriptionsUpdated;

        event Action Connected;

        event Action Disconnected;

        void Initialize(IDispatcher? сallbackDispatcher, bool elementValueListCallbackIsEnabled, string serverAddress,
            string applicationName, string workstationName, string systemNameToConnect, CaseInsensitiveDictionary<string> contextParams);

        void Close();
        
        string AddItem(string elementId, IValueSubscription valueSubscription);
        
        void RemoveItem(IValueSubscription valueSubscription);

        void PollElementValuesChanges(Action<IValueSubscription[]?> setResultAction);

        void Write(IValueSubscription[] valueSubscriptions, ValueStatusTimestamp[] vsts, Action<IValueSubscription[]>? setResultAction);
        
        void Write(IValueSubscription valueSubscription, ValueStatusTimestamp vst);

        void Passthrough(string recipientId, string passthroughName, byte[] dataToSend,
            Action<IEnumerable<byte>?> setResultAction);

        void HdaAddItem(string elementId, object valueSubscription);

        void HdaRemoveItem(object valueSubscription);

        void HdaReadElementValueJournals(DateTime firstTimestampUtc, DateTime secondTimestampUtc, uint numValuesPerDataObject, TypeId calculation, object[] valueSubscriptionsCollection,
            Action<ValueStatusTimestamp[][]?> setResultAction);

        event Action<EventMessage[]> EventMessagesCallback;

        void AckAlarms(EventId[] events);
    }
}
