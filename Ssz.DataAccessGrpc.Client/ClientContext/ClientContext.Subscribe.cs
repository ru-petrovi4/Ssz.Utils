using System;
using System.Collections.Generic;
using Ssz.DataAccessGrpc.Client.ClientLists;
using Ssz.DataAccessGrpc.ServerBase;
using Ssz.DataAccessGrpc.Client.ClientListItems;
using Ssz.Utils.DataAccess;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using EventMessagesCollection = Ssz.DataAccessGrpc.ServerBase.EventMessagesCollection;
using Google.Protobuf.Collections;
using Ssz.Utils;

namespace Ssz.DataAccessGrpc.Client
{
    /// <summary>
    ///     This partial class defines the Callback and Polling related aspects of the ClientContext class.
    /// </summary>
    internal partial class ClientContext
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
        public Utils.DataAccess.EventMessagesCollection PollEventsChanges(ClientEventList eventList)
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

                    var eventMessagesCollection = EventMessagesCallback(eventList, reply.EventMessagesCollection);
                    if (eventMessagesCollection is not null)
                        return eventMessagesCollection;
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
                List<ValueStatusTimestamp> changeValueStatusTimestampsList = new List<ValueStatusTimestamp>(changedListItems.Length);
                foreach (ClientElementValueListItem changedListItem in changedListItems)
                {
                    changeValueStatusTimestampsList.Add(changedListItem.ValueStatusTimestamp);
                }
                dataList.RaiseElementValuesCallbackEvent(changedListItems, changeValueStatusTimestampsList.ToArray());
            }
            return changedListItems;
        }

        /// <summary>
        ///     Returns null, if incomplete EventMessageArray.
        /// </summary>
        /// <param name="eventList"></param>
        /// <param name="eventMessages"></param>
        /// <returns></returns>
        private Utils.DataAccess.EventMessagesCollection? EventMessagesCallback(ClientEventList eventList, EventMessagesCollection eventMessagesCollection)
        {
            Utils.DataAccess.EventMessagesCollection? eventMessagesCollection2 = eventList.GetEventMessagesCollection(eventMessagesCollection);
            if (eventMessagesCollection2 is not null && eventMessagesCollection2.EventMessages.Count > 0)
            {
                eventList.RaiseEventMessagesCallbackEvent(eventMessagesCollection2);
            }
            return eventMessagesCollection2;
        }

        private void LongrunningPassthroughCallback(ServerBase.LongrunningPassthroughCallback longrunningPassthroughCallback)
        {
            lock (_longrunningPassthroughRequestsCollection)
            {
                var jobId = longrunningPassthroughCallback.JobId ?? @"";                
                if (_longrunningPassthroughRequestsCollection.TryGetValue(jobId, out List<LongrunningPassthroughRequest>? longrunningPassthroughRequestsList))
                {
                    var jobStatusCode = longrunningPassthroughCallback.JobStatusCode;

                    foreach (LongrunningPassthroughRequest longrunningPassthroughRequest in longrunningPassthroughRequestsList)
                    {                        
                        var callbackAction = longrunningPassthroughRequest.CallbackAction;
                        if (callbackAction is not null)
                        {
                            callbackAction(new Utils.DataAccess.LongrunningPassthroughCallback
                            {
                                JobId = jobId,
                                ProgressPercent = longrunningPassthroughCallback.ProgressPercent,
                                ProgressLabel = longrunningPassthroughCallback.ProgressLabel ?? @"",
                                ProgressDetails = longrunningPassthroughCallback.ProgressDetails ?? @"",
                                JobStatusCode = jobStatusCode
                            });
                        }

                        if (jobStatusCode != JobStatusCodes.OK ||
                            longrunningPassthroughCallback.ProgressPercent > 99.9)
                        {
                            longrunningPassthroughRequest.TaskCompletionSource.SetResult(jobStatusCode);                            
                        }                        
                    }

                    if (jobStatusCode != JobStatusCodes.OK ||
                            longrunningPassthroughCallback.ProgressPercent > 99.9)
                    {
                        _longrunningPassthroughRequestsCollection.Remove(jobId);
                    }
                }
            }
        }

        #endregion
    }
}