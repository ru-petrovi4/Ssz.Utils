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
using System.Threading;
using Microsoft.Extensions.Logging;

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
        public async Task<ClientElementValueListItem[]> PollElementValuesChangesAsync(ClientElementValueList tagValueList)
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
                    PollElementValuesChangesReply reply = await _resourceManagementClient.PollElementValuesChangesAsync(request);
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
        public async Task<Utils.DataAccess.EventMessagesCollection> PollEventsChangesAsync(ClientEventList eventList)
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
                    PollEventsChangesReply reply = await _resourceManagementClient.PollEventsChangesAsync(request);
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

        private async Task ReadCallbackMessagesAsync(IAsyncStreamReader<CallbackMessage> reader, CancellationToken cancellationToken)
        {
            while (true)
            {
                if (!_serverContextIsOperational || cancellationToken.IsCancellationRequested) 
                    return;

                try
                {
                    if (!await reader.MoveNext(cancellationToken)) 
                        return;
                }
                //catch (RpcException e) when (e.StatusCode == StatusCode.NotFound)
                //{
                //    break;
                //}
                //catch (OperationCanceledException)
                //{
                //    break;
                //}
                catch
                {
                    _serverContextIsOperational = false;
                    _pendingClientContextNotificationEventArgs = new ClientContextNotificationEventArgs(ClientContextNotificationType.ReadCallbackMessagesException, null);
                    return;
                }

                if (!_serverContextIsOperational || cancellationToken.IsCancellationRequested) 
                    return;

                CallbackMessage current = reader.Current;
                _workingDispatcher.BeginInvoke(ct =>
                {
                    if (ct.IsCancellationRequested) return;
                    try
                    {
                        switch (current.OptionalMessageCase)
                        {
                            case CallbackMessage.OptionalMessageOneofCase.ContextStatus:
                                ServerContextStatusCallback(current.ContextStatus);
                                break;
                            case CallbackMessage.OptionalMessageOneofCase.ElementValuesCallback:
                                ElementValuesCallback elementValuesCallback = current.ElementValuesCallback;
                                ClientElementValueList valueList = GetElementValueList(elementValuesCallback.ListClientAlias);
                                ElementValuesCallback(valueList, elementValuesCallback.ElementValuesCollection);
                                break;
                            case CallbackMessage.OptionalMessageOneofCase.EventMessagesCallback:
                                EventMessagesCallback eventMessagesCallback = current.EventMessagesCallback;
                                ClientEventList eventList = GetEventList(eventMessagesCallback.ListClientAlias);
                                EventMessagesCallback(eventList, eventMessagesCallback.EventMessagesCollection);
                                break;
                            case CallbackMessage.OptionalMessageOneofCase.LongrunningPassthroughCallback:
                                LongrunningPassthroughCallback(current.LongrunningPassthroughCallback);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Callback message exception.");
                    }
                });
            }
        }

        private void ServerContextStatusCallback(ContextStatus contextStatus)
        {
            ServerContextStatus = contextStatus;
            if (ServerContextStatus is not null && ServerContextStatus.StateCode == ContextStateCodes.STATE_ABORTING)
            {
                _serverContextIsOperational = false;
                _pendingClientContextNotificationEventArgs = new ClientContextNotificationEventArgs(ClientContextNotificationType.Shutdown,
                    null);
            }
            if (ServerContextStatus is not null)
                ServerContextNotification(this, new ContextStatusChangedEventArgs
                {
                    ContextStateCode = ServerContextStatus.StateCode,
                    Info = ServerContextStatus.Info ?? @"",
                    Label = ServerContextStatus.Label ?? @"",
                    Details = ServerContextStatus.Details ?? @"",
                });
        }

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
            var jobId = longrunningPassthroughCallback.JobId ?? @"";
            if (_longrunningPassthroughRequestsCollection.TryGetValue(jobId, out List<LongrunningPassthroughRequest>? longrunningPassthroughRequestsList))
            {
                var statusCode = longrunningPassthroughCallback.StatusCode;

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
                            StatusCode = statusCode
                        });
                    }

                    if (!StatusCodes.IsGood(statusCode) ||
                        longrunningPassthroughCallback.ProgressPercent == 100)
                    {
                        longrunningPassthroughRequest.TaskCompletionSource.SetResult(statusCode);
                    }
                }

                if (!StatusCodes.IsGood(statusCode) ||
                        longrunningPassthroughCallback.ProgressPercent == 100)
                {
                    _longrunningPassthroughRequestsCollection.Remove(jobId);
                }
            }
        }

        #endregion
    }
}