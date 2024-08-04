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
        /// <param name="elementValueList"></param>
        /// <returns></returns>
        public async Task<ClientElementValueListItem[]> PollElementValuesChangesAsync(ClientElementValueList elementValueList)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed ClientContext.");

            if (!ContextIsOperational) throw new InvalidOperationException();

            try
            {
                var request = new PollElementValuesChangesRequest
                {
                    ContextId = _serverContextId,
                    ListServerAlias = elementValueList.ListServerAlias
                };
                var reply = _resourceManagementClient.PollElementValuesChanges(request);
                SetResourceManagementLastCallUtc();

                List<ClientElementValueListItem> result = new();

                while (await reply.ResponseStream.MoveNext())
                {
                    var changedItems = ElementValuesCallback(elementValueList, reply.ResponseStream.Current.ElementValuesCollection);
                    if (changedItems is not null)
                        result.AddRange(changedItems);
                }

                return result.ToArray();
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
        public async Task<List<Utils.DataAccess.EventMessagesCollection>> PollEventsChangesAsync(ClientEventList eventList)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed ClientContext.");

            if (!ContextIsOperational) throw new InvalidOperationException();

            try
            {
                var request = new PollEventsChangesRequest
                {
                    ContextId = _serverContextId,
                    ListServerAlias = eventList.ListServerAlias
                };
                var reply = _resourceManagementClient.PollEventsChanges(request);
                SetResourceManagementLastCallUtc();

                List<Utils.DataAccess.EventMessagesCollection> result = new();

                while (await reply.ResponseStream.MoveNext())
                {
                    var eventMessagesCollection = EventMessagesCallback(eventList, reply.ResponseStream.Current);
                    if (eventMessagesCollection is not null)
                        result.Add(eventMessagesCollection);
                }

                return result;                
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
            await Task.Delay(0);

            while (true)
            {
                if (!_contextIsOperational || cancellationToken.IsCancellationRequested) 
                    break;

                try
                {
                    if (!await reader.MoveNext(cancellationToken))
                        break;
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
                    _contextIsOperational = false;                    
                    break;
                }

                if (!_contextIsOperational || cancellationToken.IsCancellationRequested)
                    break;

                CallbackMessage current = reader.Current;
                _workingDispatcher.BeginInvoke(ct =>
                {
                    if (ct.IsCancellationRequested) 
                        return;
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
                _contextIsOperational = false;                
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
        /// <param name="dataChunk"></param>
        /// <returns></returns>
        private ClientElementValueListItem[]? ElementValuesCallback(ClientElementValueList dataList, DataChunk dataChunk)
        {
            ClientElementValueListItem[]? changedListItems = dataList.OnElementValuesCallback(dataChunk);
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
            Utils.DataAccess.EventMessagesCollection? fullEventMessagesCollection = eventList.GetEventMessagesCollection(eventMessagesCollection);
            if (fullEventMessagesCollection is not null && fullEventMessagesCollection.EventMessages.Count > 0)
            {
                eventList.RaiseEventMessagesCallbackEvent(fullEventMessagesCollection);
            }
            return fullEventMessagesCollection;
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