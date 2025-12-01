using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xi.Contracts;
using Xi.Contracts.Data;

namespace Ssz.Xi.Client.Internal.Context
{
    internal partial class XiContext
    {
        #region public functions

        /// <summary>
        ///     This method is used to write data of the specified list to the server.  It is called
        ///     by the ClientBase after the client application has prepared and committed the data
        ///     values.
        /// </summary>
        /// <param name="serverListId"> The server identifier of the list containing the data objects to write. </param>
        /// <param name="writeValueArrays"> The data values to write. </param>
        /// <returns>
        ///     The list server aliases and result codes for the data objects whose write failed. Returns null if all writes
        ///     succeeded or null if this is a keep-alive.
        /// </returns>
        public List<AliasResult>? WriteData(uint serverListId, DataValueArraysWithAlias writeValueArrays)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiContext.");

            if (_writeEndpoint is null) throw new Exception("No Write Endpoint");

            if (_writeEndpoint.Disposed) return null;

            IWrite iWrite = _writeEndpoint.Proxy;
            string contextId = ContextId;

            _writeEndpoint.LastCallUtc = DateTime.UtcNow;

            List<AliasResult>? listAliasResult = null;

            try
            {
                listAliasResult = iWrite.WriteVST(contextId, serverListId, writeValueArrays);
            }
            catch (Exception ex)
            {
                ProcessRemoteMethodCallException(ex);
            }

            return listAliasResult;
        }

        /// <summary>
        ///     <para>This method is used to acknowledge one or more alarms.</para>
        /// </summary>
        /// <param name="serverListId">
        ///     The server identifier for the list that contains the alarms to be
        ///     acknowledged.
        /// </param>
        /// <param name="operatorName">
        ///     The name or other identifier of the operator who is acknowledging
        ///     the alarm.
        /// </param>
        /// <param name="comment">
        ///     An optional comment submitted by the operator to accompany the
        ///     acknowledgement.
        /// </param>
        /// <param name="alarmsToAck">
        ///     The list of alarms to acknowledge.
        /// </param>
        /// <returns>
        ///     The list EventIds and result codes for the alarms whose
        ///     acknowledgement failed. Returns null if all acknowledgements
        ///     succeeded.
        /// </returns>
        public List<EventIdResult>? AcknowledgeAlarms(uint serverListId,
            string? operatorName, string? comment, List<EventId> alarmsToAck)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiContext.");

            if (_writeEndpoint is null) throw new Exception("No Write Endpoint");

            if (_writeEndpoint.Disposed) return null;

            string contextId = ContextId;

            _writeEndpoint.LastCallUtc = DateTime.UtcNow;

            List<EventIdResult>? listAliasResult = null;

            if (serverListId != 0)
            {
                try
                {
                    listAliasResult = _writeEndpoint.Proxy.AcknowledgeAlarms(contextId, serverListId,
                        operatorName, comment, alarmsToAck);
                }
                catch (Exception ex)
                {
                    ProcessRemoteMethodCallException(ex);
                }
            }            

            return listAliasResult;
        }

        public PassthroughResult? Passthrough(string recipientId,
                                      string passthroughName, ReadOnlyMemory<byte> dataToSend)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiContext.");

            if (_writeEndpoint is null) throw new Exception("No Write Endpoint");

            if (_writeEndpoint.Disposed) return null;

            string contextId = ContextId;

            _writeEndpoint.LastCallUtc = DateTime.UtcNow;

            PassthroughResult? passthroughResult = null;

            try
            {
                passthroughResult = _writeEndpoint.Proxy.Passthrough(contextId, recipientId, 0,
                                      passthroughName, dataToSend.ToArray());
            }
            catch (Exception ex)
            {
                ProcessRemoteMethodCallException(ex);
            }

            return passthroughResult;
        }

        /// <summary>
        ///     Returns StatusCode <see cref="StatusCodes"/>
        /// <param name="recipientId"></param>
        /// <param name="passthroughName"></param>
        /// <param name="dataToSend"></param>
        /// <param name="callbackAction"></param>
        /// <returns></returns>
        public Task<Task<uint>> LongrunningPassthroughAsync(string recipientId, string passthroughName, ReadOnlyMemory<byte> dataToSend,
            Action<Ssz.Utils.DataAccess.LongrunningPassthroughCallback>? callbackAction)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiContext.");

            if (_writeEndpoint is null) throw new Exception("No Write Endpoint");

            if (_writeEndpoint.Disposed) 
                return Task.FromResult(Task.FromResult(StatusCodes.BadInvalidState));

            string contextId = ContextId;

            _writeEndpoint.LastCallUtc = DateTime.UtcNow;

            int invokeId;
            var taskCompletionSource = new TaskCompletionSource<uint>();
            lock (_incompleteCommandCallsCollection)
            {
                invokeId = (int)_incompleteCommandCallsCollection.Add(taskCompletionSource);
            }
            try
            {
                // InvokeId != 0, so there must be ICallback.PassthroughCallback(string contextId, int invokeId, PassthroughResult passthroughResult)
                PassthroughResult? passthroughResult = _writeEndpoint.Proxy.Passthrough(contextId, recipientId, invokeId,
                                      passthroughName, dataToSend.ToArray());
                return Task.FromResult(taskCompletionSource.Task);
            }
            catch (Exception ex)
            {
                lock (_incompleteCommandCallsCollection)
                {
                    _incompleteCommandCallsCollection.Remove((uint)invokeId);
                }
                ProcessRemoteMethodCallException(ex);
                throw;
            }            
        }

        #endregion

        #region private fields

        private readonly ObjectManager<TaskCompletionSource<uint>> _incompleteCommandCallsCollection = new(256);

        #endregion
    }
}