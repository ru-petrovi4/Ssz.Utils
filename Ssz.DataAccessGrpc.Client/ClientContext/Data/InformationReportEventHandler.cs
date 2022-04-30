using Ssz.DataAccessGrpc.Client.ClientListItems;
using Ssz.DataAccessGrpc.Client.ClientLists;
using Ssz.Utils.DataAccess;

namespace Ssz.DataAccessGrpc.Client.Data
{
    /// <summary>
    ///     This delegate defines the callback for reporting data updates to the client application.
    /// </summary>
    /// <param name="dataList"> The DataAccessGrpcSubscription that is sending the alarms and events to the client application. </param>
    /// <param name="changedListItems"> The list of data updates being reported. </param>
    /// <param name="changedValues">The time when the values were last changed.</param>
    internal delegate void ElementValuesCallbackEventHandler(
        ClientElementValueList dataList, ClientElementValueListItem[] changedListItems, ValueStatusTimestamp[] changedValues);
}