using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Utils.DataSource
{
    public interface IDataProvider
    {
        string ServerAddress { get; }

        string[] SystemNames { get; }

        string ApplicationName { get; }

        string WorkstationName { get; }

        CaseInsensitiveDictionary<string> ContextParams { get; }

        bool IsConnected { get; }

        bool IsInitialized { get; }

        Guid ModelDataGuid { get; }
        
        event Action ValueSubscriptionsUpdated;

        event Action Connected;

        event Action Disconnected;

        void Initialize(IDispatcher? сallbackDispatcher, bool elementValueListCallbackIsEnabled, string serverAddress,
            string applicationName, string workstationName, string[] systemNames, CaseInsensitiveDictionary<string> contextParams);

        void Close();
        
        string AddItem(string elementId, IValueSubscription valueSubscription);
        
        void RemoveItem(IValueSubscription valueSubscription);

        void PollElementValuesChanges(Action<IValueSubscription[]?> setResultAction);

        void Write(IValueSubscription[] valueSubscriptions, Any[] values, Action<IValueSubscription[]>? setResultAction);
        
        void Write(IValueSubscription valueSubscription, Any value);

        void Passthrough(string recipientId, string passthroughName, byte[] dataToSend,
            Action<byte[]?> setResultAction);

        void HdaAddItem(string elementId, object valueSubscription);

        void HdaRemoveItem(object valueSubscription);

        void HdaReadElementValueJournals(DateTime firstTimeStampUtc, DateTime secondTimeStampUtc, uint numValuesPerDataObject, TypeId calculation, object[] valueSubscriptionsCollection,
            Action<ValueStatusTimestamp[][]?> setResultAction);

        event Action<EventMessage[]> EventNotification;

        void AckAlarms(EventId[] events);
    }
}
