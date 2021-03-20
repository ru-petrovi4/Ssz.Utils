using Ssz.DataGrpc.Client.Core.ListItems;
using Ssz.DataGrpc.Client.Core.Lists;
using Ssz.DataGrpc.Common;

namespace Ssz.DataGrpc.Client.Data
{
    /// <summary>
    ///     This delegate defines the callback for reporting data updates to the client application.
    /// </summary>
    /// <param name="dataList"> The DataGrpcSubscription that is sending the alarms and events to the client application. </param>
    /// <param name="changedListItems"> The list of data updates being reported. </param>
    /// <param name="changedValues">The time when the values were last changed.</param>
    public delegate void InformationReportEventHandler(
        ClientElementValueList dataList, ClientElementValueListItem[] changedListItems, DataGrpcValueStatusTimestamp[] changedValues);
}