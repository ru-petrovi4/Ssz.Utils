using System.Collections.Generic;
using Ssz.DataGrpc.Client.Core.ListItems;
using Ssz.DataGrpc.Client.Core.Lists;

namespace Ssz.DataGrpc.Client.Data
{
    /// <summary>
    ///     This delegate defines the callback for reporting new alarms and events to the client application
    /// </summary>
    /// <param name="eventList"> The IClientEventList that is sending the alarms and events to the client application. </param>
    /// <param name="newListItems"> The alarms and events that are being sent to the client application. </param>
    public delegate void EventNotificationEventHandler(
        ClientEventList eventList, ClientEventListItem[] newListItems);
}