using System.Collections.Generic;
using Ssz.DataGrpc.Client.Core.ListItems;
using Ssz.DataGrpc.Client.Core.Lists;

namespace Ssz.DataGrpc.Client.Data.EventHandlers
{
    /// <summary>
    ///     This delegate defines the callback for reporting new alarms and events to the client application
    /// </summary>
    /// <param name="eventList"> The IDataGrpcEventList that is sending the alarms and events to the client application. </param>
    /// <param name="newListItems"> The alarms and events that are being sent to the client application. </param>
    public delegate void DataGrpcEventNotificationEventHandler(
        DataGrpcEventList eventList, DataGrpcEventListItem[] newListItems);
}