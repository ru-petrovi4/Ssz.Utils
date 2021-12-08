using System;
using System.Collections.Generic;
using Ssz.DataGrpc.Client.ClientLists;
using Ssz.DataGrpc.Server;
using Ssz.DataGrpc.Client.ClientListItems;
using Ssz.Utils.DataAccess;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Ssz.Utils;

namespace Ssz.DataGrpc.Client
{
    /// <summary>
    ///     This partial class defines the Callback and Polling related aspects of the ClientContext class.
    /// </summary>
    public partial class ClientContext
    {
        #region public functions

        /// <summary>        
        /// </summary>
        /// <param name="tagValueList"></param>
        /// <returns></returns>
        public ClientElementValueListItem[] PollElementValuesChanges(ClientElementValueList tagValueList)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed ClientContext.");

            if (!ServerContextIsOperational) throw new InvalidOperationException();

            try
            {
                while (true)
                {
                    var request = new PollElementValuesChangesRequest
                    {
                        ContextId = _serverContextId,
                        ListServerAlias = tagValueList.ListServerAlias
                    };
                    PollElementValuesChangesReply reply = _resourceManagementClient.PollElementValuesChanges(request);
                    SetResourceManagementLastCallUtc();

                    var changedItems = ElementValuesCallback(tagValueList, reply.ElementValuesCollection);
                    if (changedItems is not null) return changedItems;
                }
            }
            catch (Exception ex)
            {
                ProcessRemoteMethodCallException(ex);
                throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="eventList"></param>
        /// <returns></returns>
        public Server.EventMessage[] PollEventsChanges(ClientEventList eventList)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed ClientContext.");

            if (!ServerContextIsOperational) throw new InvalidOperationException();

            try
            {
                while (true)
                {
                    var request = new PollEventsChangesRequest
                    {
                        ContextId = _serverContextId,
                        ListServerAlias = eventList.ListServerAlias
                    };
                    PollEventsChangesReply reply = _resourceManagementClient.PollEventsChanges(request);
                    SetResourceManagementLastCallUtc();

                    var newEventMessages = EventMessagesCallback(eventList, reply.EventMessagesCollection);
                    if (newEventMessages is not null) return newEventMessages;
                }
            }
            catch (Exception ex)
            {
                ProcessRemoteMethodCallException(ex);
                throw;
            }
        }        

        #endregion

        #region private functions

        /// <summary>
        ///     Returns null, if incomplete ElementValuesCollection.
        /// </summary>
        /// <param name="dataList"></param>
        /// <param name="elementValuesCollections"></param>
        /// <returns></returns>
        private ClientElementValueListItem[]? ElementValuesCallback(ClientElementValueList dataList, ElementValuesCollection elementValuesCollections)
        {
            ClientElementValueListItem[]? changedListItems = dataList.OnElementValuesCallback(elementValuesCollections);
            if (changedListItems is not null && changedListItems.Length > 0)
            {
                List<ValueStatusTimestamp> changedValuesList = new List<ValueStatusTimestamp>(changedListItems.Length);
                foreach (ClientElementValueListItem changedListItem in changedListItems)
                {
                    changedValuesList.Add(changedListItem.ValueStatusTimestamp);
                }
                dataList.RaiseElementValuesCallbackEvent(changedListItems, changedValuesList.ToArray());
            }
            return changedListItems;
        }

        /// <summary>
        ///     Returns null, if incomplete EventMessageArray.
        /// </summary>
        /// <param name="eventList"></param>
        /// <param name="eventMessages"></param>
        /// <returns></returns>
        private Server.EventMessage[]? EventMessagesCallback(ClientEventList eventList, EventMessagesCollection eventMessagesCollection)
        {
            Server.EventMessage[]? newEventMessages = eventList.EventMessagesCallback(eventMessagesCollection);
            if (newEventMessages is not null && newEventMessages.Length > 0)
            {
                eventList.RaiseEventMessagesCallbackEvent(newEventMessages);
            }
            return newEventMessages;
        }

        private void LongrunningPassthroughCallback(Server.LongrunningPassthroughCallback longrunningPassthroughCallback)
        {
            lock (_incompleteLongrunningPassthroughRequestsCollection)
            {
                var invokeId = longrunningPassthroughCallback.InvokeId ?? @"";
                if (_incompleteLongrunningPassthroughRequestsCollection.TryGetValue(invokeId, out IncompleteLongrunningPassthroughRequest? incompleteLongrunningPassthroughRequest))
                {                    
                    var statusCode = (StatusCode)longrunningPassthroughCallback.StatusCode;
                    var callbackAction = incompleteLongrunningPassthroughRequest.CallbackAction;
                    if (callbackAction is not null)
                    {
                        callbackAction(new Utils.DataAccess.LongrunningPassthroughCallback
                        {
                            InvokeId = invokeId,
                            ProgressPercent = longrunningPassthroughCallback.ProgressPercent,
                            ProgressLabel = longrunningPassthroughCallback.ProgressLabel ?? @"",
                            Succeeded = statusCode == StatusCode.OK
                        });
                    }
                    if (statusCode != StatusCode.OK)
                    {
                        incompleteLongrunningPassthroughRequest.TaskCompletionSource.SetResult(statusCode);
                        _incompleteLongrunningPassthroughRequestsCollection.Remove(invokeId);
                    }
                    else // statusCode == StatusCode.OK
                    {
                        if (longrunningPassthroughCallback.ProgressPercent > 99.9)
                        {
                            incompleteLongrunningPassthroughRequest.TaskCompletionSource.SetResult(StatusCode.OK);
                            _incompleteLongrunningPassthroughRequestsCollection.Remove(invokeId);
                        }
                    }                    
                }
            }
        }

        #endregion
    }
}