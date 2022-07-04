using System.Collections.Generic;
using Ssz.DataAccessGrpc.Client.ClientListItems;
using Ssz.DataAccessGrpc.Client.ClientLists;

namespace Ssz.DataAccessGrpc.Client.Data
{
    /// <summary>
    ///     This delegate defines the callback for reporting new alarms and events to the client application
    /// </summary>
    /// <param name="eventList"> The IClientEventList that is sending the alarms and events to the client application. </param>
    /// <param name="newListItems"> The alarms and events that are being sent to the client application. </param>
    internal delegate void EventMessagesCallbackEventHandler(
        ClientEventList eventList, Utils.DataAccess.EventMessagesCollection newEventMessagesCollection);
}