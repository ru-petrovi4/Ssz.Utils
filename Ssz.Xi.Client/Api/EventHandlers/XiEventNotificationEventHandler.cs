using System.Collections.Generic;
using Ssz.Xi.Client.Api.ListItems;
using Ssz.Xi.Client.Api.Lists;

namespace Ssz.Xi.Client.Api.EventHandlers
{
    /// <summary>
    ///     This delegate defines the callback for reporting new alarms and events to the client application
    /// </summary>
    /// <param name="eventList"> The IXiEventList that is sending the alarms and events to the client application. </param>
    /// <param name="newListItems"> The alarms and events that are being sent to the client application. </param>
    public delegate void XiEventNotificationEventHandler(
        IXiEventListProxy eventList, IEnumerable<IXiEventListItem> newListItems);
}