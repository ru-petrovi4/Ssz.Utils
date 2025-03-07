using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Ssz.DataAccessGrpc.ServerBase;
using Ssz.Utils;

namespace Ssz.DataAccessGrpc.Client
{
    internal partial class ClientContext
    {
        #region public functions

        /// <summary>
        ///     This method is used to write data of the specified list to the ServerBase.  It is called
        ///     by the ClientBase after the client application has prepared and committed the data
        ///     values.
        /// </summary>
        /// <param name="listServerAlias"> The server identifier of the list containing the data objects to write. </param>
        /// <param name="fullElementValuesCollection"> The data values to write. </param>
        /// <returns>
        ///     The list server aliases and result codes for the data objects whose write failed. Returns empty if all writes
        ///     succeeded.
        /// </returns>
        public async Task<AliasResult[]> WriteElementValuesAsync(uint listServerAlias, byte[] fullElementValuesCollection)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed ClientContext.");

            if (!ContextIsOperational) throw new InvalidOperationException();            

            try
            {
                var reply = await _dataAccessService.WriteElementValuesAsync(listServerAlias, fullElementValuesCollection, _serverContextId);                
                SetResourceManagementLastCallUtc();
                return reply.Results.ToArray();                
            }
            catch (Exception ex)
            {
                ProcessRemoteMethodCallException(ex);
                throw;
            }
        }

        /// <summary>
        ///     Returns the array EventIds and result codes for the alarms whose
        ///     acknowledgement failed.
        /// </summary>
        /// <param name="listServerAlias"></param>
        /// <param name="operatorName"></param>
        /// <param name="comment"></param>
        /// <param name="eventIdsToAck"></param>
        /// <returns></returns>
        public async Task<EventIdResult[]> AckAlarmsAsync(uint listServerAlias,
            string operatorName, string comment, Ssz.Utils.DataAccess.EventId[] eventIdsToAck)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed ClientContext.");

            if (!ContextIsOperational) throw new InvalidOperationException();

            try
            {
                var request = new AckAlarmsRequest
                {
                    ContextId = _serverContextId,
                    ListServerAlias = listServerAlias,
                    OperatorName = operatorName,
                    Comment = comment,                   
                };
                request.EventIdsToAck.Add(eventIdsToAck.Select(e => new EventId(e)));
                AckAlarmsReply reply = await _dataAccessService.AckAlarmsAsync(request);
                SetResourceManagementLastCallUtc();
                return reply.Results.ToArray();
            }
            catch (Exception ex)
            {
                ProcessRemoteMethodCallException(ex);
                throw;
            }
        }

        public async Task UpdateContextParamsAsync(CaseInsensitiveDictionary<string?> contextParams)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed Context.");

            if (!ContextIsOperational) throw new InvalidOperationException();

            try
            {
                var request = new UpdateContextParamsRequest
                {
                    ContextId = _serverContextId                    
                };
                foreach (var kvp in contextParams)
                    request.ContextParams.Add(kvp.Key,
                        kvp.Value is not null ? new NullableString { Data = kvp.Value } : new NullableString { Null = NullValue.NullValue });
                await _dataAccessService.UpdateContextParamsAsync(request);
                SetResourceManagementLastCallUtc();
            }
            catch (Exception ex)
            {
                ProcessRemoteMethodCallException(ex);
                throw;
            }
        }

        public async Task<ReadOnlyMemory<byte>> PassthroughAsync(string recipientPath, string passthroughName, ReadOnlyMemory<byte> dataToSend)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed Context.");

            if (!ContextIsOperational) throw new InvalidOperationException();

            try
            {   
                var result = await _dataAccessService.PassthroughAsync(recipientPath, passthroughName, dataToSend, _serverContextId);
                SetResourceManagementLastCallUtc();
                return result;
            }
            catch (Exception ex)
            {
                ProcessRemoteMethodCallException(ex);
                throw;
            }            
        }

        /// <summary>
        ///     Returns StatusCode
        /// </summary>
        /// <param name="recipientPath"></param>
        /// <param name="passthroughName"></param>
        /// <param name="dataToSend"></param>
        /// <param name="callbackAction"></param>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<Task<uint>> LongrunningPassthroughAsync(string recipientPath, string passthroughName, ReadOnlyMemory<byte> dataToSend,
            Action<Ssz.Utils.DataAccess.LongrunningPassthroughCallback>? callbackAction)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed Context.");

            if (!ContextIsOperational) throw new InvalidOperationException();
            
            try
            {
                LongrunningPassthroughReply reply = await _dataAccessService.LongrunningPassthroughAsync(recipientPath, passthroughName, dataToSend, _serverContextId);
                SetResourceManagementLastCallUtc();                

                string jobId = reply.JobId;

                var longrunningPassthroughRequest = new LongrunningPassthroughRequest
                {
                    CallbackAction = callbackAction
                };
                if (!_longrunningPassthroughRequestsCollection.TryGetValue(jobId, out List<LongrunningPassthroughRequest>? longrunningPassthroughRequestsList))
                {
                    longrunningPassthroughRequestsList = new List<LongrunningPassthroughRequest>();
                    _longrunningPassthroughRequestsCollection.Add(jobId, longrunningPassthroughRequestsList);
                }
                longrunningPassthroughRequestsList.Add(longrunningPassthroughRequest);

                return longrunningPassthroughRequest.TaskCompletionSource.Task;
            }
            catch (Exception ex)
            {                
                ProcessRemoteMethodCallException(ex);
                throw;
            }            
        }

        #endregion

        #region private fields

        /// <summary>
        ///     [JobId, IncompleteLongrunningPassthroughRequest]
        /// </summary>
        private readonly Dictionary<string, List<LongrunningPassthroughRequest>> _longrunningPassthroughRequestsCollection = new();

        #endregion

        private class LongrunningPassthroughRequest
        {
            public readonly TaskCompletionSource<uint> TaskCompletionSource = new();

            public Action<Ssz.Utils.DataAccess.LongrunningPassthroughCallback>? CallbackAction;
        }
    }
}