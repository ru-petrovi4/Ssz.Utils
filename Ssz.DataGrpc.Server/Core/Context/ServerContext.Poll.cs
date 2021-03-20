using System;
using System.Collections.Generic;
using Ssz.DataGrpc.Server.Core.Lists;
using Xi.Common.Support;
using Xi.Contracts;
using Xi.Contracts.Constants;
using Xi.Contracts.Data;

namespace Ssz.DataGrpc.Server.Core.Context
{
    /// <summary>
    ///   This partial class defines the methods that support the methods 
    ///   of the IPoll interface.
    /// </summary>
    public partial class ServerContext        
    {
        #region public functions

        /*
        /// <summary>
        ///   This method is used to create and initialize the response to the PollDataChanges method.
        /// </summary>
        /// <param name = "doubleCount">The number of elements in the DataValueArraysWithAlias' double arrays.</param>
        /// <param name = "uintCount">The number of elements in the DataValueArraysWithAlias' uint arrays.</param>
        /// <param name = "objectCount">The number of elements in the DataValueArraysWithAlias' object arrays.</param>
        /// <returns></returns>
        public static DataValueArraysWithAlias CreatePollResponse(ListRoot list, int doubleCount, int uintCount,
                                                                  int objectCount)
        {
            if ((list.DiscardedQueueEntries > 0) && (list.LastPollTime != DateTime.MinValue)) uintCount++;

            var pollResponse = new DataValueArraysWithAlias(doubleCount, uintCount, objectCount);

            if ((list.DiscardedQueueEntries > 0) && (list.LastPollTime != DateTime.MinValue))
            {
                byte statusByte = XiStatusCode.MakeStatusByte((byte) XiStatusCodeStatusBits.GoodNonSpecific,
                                                              (byte) XiStatusCodeLimitBits.NotLimited);
                pollResponse.SetUint(0, 0, XiStatusCode.MakeStatusCode(statusByte, 0, 0), DateTime.UtcNow,
                                     list.DiscardedQueueEntries);
            }

            return pollResponse;
        }*/

        /// <summary>
        ///   This method is to be overridden by the implementation class.
        /// </summary>
        /// <returns>
        ///   The results of executing the passthroughs. Each result in the list consists of the 
        ///   result code, the invokeId supplied in the request, and a byte array.  It is up to the 
        ///   client application to interpret this byte array.  
        /// </returns>
        public virtual List<PassthroughResult> OnPollPassthroughResponses()
        {
            // TODO:  Implement the implementation class override for this method if supported, 
            //		and also set the corresponding bit in StandardMib.MethodsSupported.
            throw RpcExceptionHelper.Create(XiFaultCodes.E_NOTIMPL, "IPoll.PollPassthroughResponses");
        }

        #endregion

        #region internal functions

        /// <summary>
        ///   This method is to be overridden by the context implementation in the 
        ///   Server Implementation project.
        /// </summary>
        /// <param name = "listId">
        ///   The identifier for the list whose changes are to be returned (reported).
        ///   Null if this is a keep-alive.
        /// </param>
        /// <returns>
        ///   The list of changed values or null if this is a keep-alive.
        /// </returns>
        internal DataValueArraysWithAlias OnPollDataChanges(uint listId)
        {
            // If this is a keep-alive, return null
            if (listId == 0) return null;

            TListRoot xiList = null;

            using (SyncRoot.Enter())
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ServerContext.");

                if (_listManager.TryGetValue(listId, out xiList))
                    xiList.AuthorizeEndpointUse(typeof (IPoll)); // throws an exception if validation fails					
                else throw RpcExceptionHelper.Create(XiFaultCodes.E_BADLISTID, "List Id not found in Poll Event Changes.");
            }

            xiList.SetLastPollTime();
            return xiList.OnPollDataChanges();
        }

        /// <summary>
        ///   This method is to be overridden by the context implementation in the 
        ///   Server Implementation project.
        /// </summary>
        /// <param name = "listId">
        ///   The identifier for the list whose changes are to be returned 
        ///   (reported).
        /// </param>
        /// <param name = "filterSet">
        ///   Optional set of filters to further refine the selection from 
        ///   the alarms and events in the list. The event list itself is 
        ///   created using a filter.
        /// </param>
        /// <returns>
        ///   The list of new alarm/event messages, changes to alarm messages 
        ///   that are already in the list, and deletions of alarm messages 
        ///   from the list.  
        /// </returns>
        internal EventMessage[] OnPollEventChanges(uint listId, FilterSet filterSet)
        {
            TListRoot xiList = null;

            using (SyncRoot.Enter())
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ServerContext.");

                if (_listManager.TryGetValue(listId, out xiList))
                    xiList.AuthorizeEndpointUse(typeof (IPoll)); // throws an exception if validation fails					
                else throw RpcExceptionHelper.Create(XiFaultCodes.E_BADLISTID, "List Id not found in Poll Event Changes.");
            }

            return xiList.OnPollEventChanges(filterSet);
        }

        #endregion
    }
}