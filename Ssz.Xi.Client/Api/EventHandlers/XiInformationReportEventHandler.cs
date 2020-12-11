using Ssz.Xi.Client.Api.ListItems;
using Ssz.Xi.Client.Api.Lists;

namespace Ssz.Xi.Client.Api.EventHandlers
{
    /// <summary>
    ///     This delegate defines the callback for reporting data updates to the client application.
    /// </summary>
    /// <param name="dataList"> The XiSubscription that is sending the alarms and events to the client application. </param>
    /// <param name="changedListItems"> The list of data updates being reported. </param>
    /// <param name="changedValues">The time when the values were last changed.</param>
    public delegate void XiInformationReportEventHandler(
        IXiDataListProxy dataList, IXiDataListItem[] changedListItems, XiValueStatusTimestamp[] changedValues);
}