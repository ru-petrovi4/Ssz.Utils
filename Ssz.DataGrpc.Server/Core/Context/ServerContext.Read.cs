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
    ///   This partial class defines the methods to be overridden by the server implementation 
    ///   to support the methods of the IRead interface.
    /// </summary>
    public partial class ServerContext        
    {
        #region internal functions

        /// <summary>
        ///   This method is to be overridden by the context implementation in the 
        ///   Server Implementation project.
        /// </summary>
        /// <param name = "listId">
        ///   The identifier of the list that contains data objects to be read.
        ///   Null if this is a keep-alive.
        /// </param>
        /// <param name = "serverAliases">
        ///   The server aliases of the data objects to read.
        /// </param>
        /// <returns>
        ///   <para>The list of requested values. Each value in this list is identified 
        ///     by its client alias.  If the server alias for a data object to read 
        ///     was not found, an ErrorInfo object will be returned that contains 
        ///     the server alias instead of a value, status, and timestamp.  </para>
        ///   <para>Returns null if this is a keep-alive.</para>
        /// </returns>
        internal DataValueArraysWithAlias OnReadData(uint listId, List<uint> serverAliases)
        {
            // If this is a keep-alive, return null
            if (listId == 0) return null;

            TListRoot xiList = null;

            using (SyncRoot.Enter())
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ServerContext.");

                if (_listManager.TryGetValue(listId, out xiList))
                    xiList.AuthorizeEndpointUse(typeof (IRead)); // throws an exception if validation fails					
                else RpcExceptionHelper.Create(XiFaultCodes.E_BADLISTID, "List Id not found in Read Data.");
            }

            return xiList.OnReadData(serverAliases);
        }

        /// <summary>
        ///   <para>This method is used to read the historical values that fall between 
        ///     a start and end time for one or more data objects within a specific data 
        ///     journal list.</para>
        /// </summary>
        /// <param name = "listId">
        ///   The identifier of the list that contains data objects whose 
        ///   historical values are to be read.
        /// </param>
        /// <param name = "firstTimeStamp">
        ///   The filter that specifies the inclusive beginning (of returned list) 
        ///   timestamp for values to be returned.  Valid operands include the 
        ///   Timestamp and OpcHdaTimestampStr constants defined by the 
        ///   FilterOperand class.
        /// </param>
        /// <param name = "secondTimeStamp">
        ///   The filter that specifies the inclusive ending (of returned list)
        ///   timestamp for values to be returned.  Valid operands include the 
        ///   Timestamp and OpcHdaTimestampStr constants defined by the 
        ///   FilterOperand class.
        /// </param>
        /// <param name = "numValuesPerAlias">
        ///   The maximum number of data sample value to be returned.
        /// </param>
        /// <param name = "serverAliases">
        ///   The list of server aliases for the data objects whose historical 
        ///   values are to be read.  
        /// </param>
        /// <returns>
        ///   The list of requested historical values, or the reason they could not 
        ///   be read.
        /// </returns>
        internal JournalDataValues[] OnReadJournalDataForTimeInterval(uint listId, FilterCriterion firstTimeStamp,
                                                                      FilterCriterion secondTimeStamp,
                                                                      uint numValuesPerAlias, List<uint> serverAliases)
        {
            TListRoot xiList = null;

            using (SyncRoot.Enter())
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ServerContext.");

                if (_listManager.TryGetValue(listId, out xiList))
                    xiList.AuthorizeEndpointUse(typeof (IRead)); // throws an exception if validation fails					
                else throw RpcExceptionHelper.Create(XiFaultCodes.E_BADLISTID, "List Id not found in Read Journal Data.");
            }

            return xiList.OnReadJournalDataForTimeInterval(firstTimeStamp, secondTimeStamp, numValuesPerAlias,
                                                           serverAliases);
        }

        internal JournalDataValues[] OnReadJournalDataNext(uint listId, uint numValuesPerAlias)
        {
            TListRoot xiList = null;

            using (SyncRoot.Enter())
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ServerContext.");

                if (_listManager.TryGetValue(listId, out xiList))
                    xiList.AuthorizeEndpointUse(typeof (IRead)); // throws an exception if validation fails					
                else
                    throw RpcExceptionHelper.Create(XiFaultCodes.E_BADLISTID, "List Id not found in Read Journal Data Next.");
            }

            return xiList.OnReadJournalDataNext(numValuesPerAlias);
        }

        internal JournalDataValues[] OnReadJournalDataAtSpecificTimes(uint listId, List<DateTime> timestamps,
                                                                      List<uint> serverAliases)
        {
            TListRoot xiList = null;

            using (SyncRoot.Enter())
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ServerContext.");

                if (_listManager.TryGetValue(listId, out xiList))
                    xiList.AuthorizeEndpointUse(typeof (IRead)); // throws an exception if validation fails					
                else throw RpcExceptionHelper.Create(XiFaultCodes.E_BADLISTID, "List Id not found in Read Journal Data.");
            }

            return xiList.OnReadJournalDataAtSpecificTimes(timestamps, serverAliases);
        }

        /// <summary>
        /// </summary>
        /// <param name = "listId"></param>
        /// <param name = "firstTimeStamp"></param>
        /// <param name = "secondTimeStamp"></param>
        /// <param name = "serverAliases"></param>
        /// <returns></returns>
        internal JournalDataChangedValues[] OnReadJournalDataChanges(uint listId, FilterCriterion firstTimeStamp,
                                                                     FilterCriterion secondTimeStamp,
                                                                     uint numValuesPerAlias, List<uint> serverAliases)
        {
            TListRoot xiList = null;

            using (SyncRoot.Enter())
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ServerContext.");

                if (_listManager.TryGetValue(listId, out xiList))
                    xiList.AuthorizeEndpointUse(typeof (IRead)); // throws an exception if validation fails					
                else
                    throw RpcExceptionHelper.Create(XiFaultCodes.E_BADLISTID,
                                              "List Id not found in Read Journal Data Changes.");
            }

            return xiList.OnReadJournalDataChanges(firstTimeStamp, secondTimeStamp, numValuesPerAlias, serverAliases);
        }

        internal JournalDataChangedValues[] OnReadJournalDataChangesNext(uint listId, uint numValuesPerAlias)
        {
            TListRoot xiList = null;

            using (SyncRoot.Enter())
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ServerContext.");

                if (_listManager.TryGetValue(listId, out xiList))
                    xiList.AuthorizeEndpointUse(typeof (IRead)); // throws an exception if validation fails					
                else
                    throw RpcExceptionHelper.Create(XiFaultCodes.E_BADLISTID,
                                              "List Id not found in Read Journal Data Changes Next.");
            }

            return xiList.OnReadJournalDataChangesNext(numValuesPerAlias);
        }

        /// <summary>
        /// </summary>
        /// <param name = "listId"></param>
        /// <param name = "firstTimeStamp"></param>
        /// <param name = "secondTimeStamp"></param>
        /// <param name = "calculationPeriod"></param>
        /// <param name = "serverAliasesAndCalculations"></param>
        /// <returns></returns>
        internal JournalDataValues[] OnReadCalculatedJournalData(uint listId, FilterCriterion firstTimeStamp,
                                                                 FilterCriterion secondTimeStamp,
                                                                 TimeSpan calculationPeriod,
                                                                 List<AliasAndCalculation> serverAliasesAndCalculations)
        {
            TListRoot xiList = null;

            using (SyncRoot.Enter())
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ServerContext.");

                if (_listManager.TryGetValue(listId, out xiList))
                    xiList.AuthorizeEndpointUse(typeof (IRead)); // throws an exception if validation fails					
                else throw RpcExceptionHelper.Create(XiFaultCodes.E_BADLISTID, "List Id not found in Read Journal Data.");
            }

            return xiList.OnReadCalculatedJournalData(firstTimeStamp, secondTimeStamp, calculationPeriod,
                                                      serverAliasesAndCalculations);
        }

        /// <summary>
        /// </summary>
        /// <param name = "listId"></param>
        /// <param name = "firstTimeStamp"></param>
        /// <param name = "secondTimeStamp"></param>
        /// <param name = "serverAlias"></param>
        /// <param name = "propertiesToRead"></param>
        /// <returns></returns>
        internal JournalDataPropertyValue[] OnReadJournalDataProperties(uint listId, FilterCriterion firstTimeStamp,
                                                                        FilterCriterion secondTimeStamp,
                                                                        uint serverAlias, List<TypeId> propertiesToRead)
        {
            TListRoot xiList = null;

            using (SyncRoot.Enter())
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ServerContext.");

                if (_listManager.TryGetValue(listId, out xiList))
                    xiList.AuthorizeEndpointUse(typeof (IRead)); // throws an exception if validation fails					
                else throw RpcExceptionHelper.Create(XiFaultCodes.E_BADLISTID, "List Id not found in Read Journal Data.");
            }

            return xiList.OnReadJournalDataProperties(firstTimeStamp, secondTimeStamp, serverAlias, propertiesToRead);
        }

        /// <summary>
        ///   This method is to be overridden by the context implementation 
        ///   in the Server Implementation project.
        /// </summary>
        /// <param name = "listId">
        ///   The identifier of the list that contains alarms and events 
        ///   to be read.
        /// </param>
        /// <param name = "filterSet">
        ///   The set of filters used to select alarms and events to be read.
        /// </param>
        /// <returns>
        ///   The list of selected alarms and events.
        ///   Null if no alarms or events were selected.
        /// </returns>
        internal EventMessage[] OnReadEvents(uint listId, FilterSet filterSet)
        {
            TListRoot xiList = null;

            using (SyncRoot.Enter())
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ServerContext.");

                if (_listManager.TryGetValue(listId, out xiList))
                    xiList.AuthorizeEndpointUse(typeof (IRead)); // throws an exception if validation fails					
                else throw RpcExceptionHelper.Create(XiFaultCodes.E_BADLISTID, "List Id not found in Read Events.");
            }

            return xiList.OnReadEvents(filterSet);
        }

        /// <summary>
        ///   This method is to be overridden by the context implementation in the 
        ///   Server Implementation project.
        /// </summary>
        /// <param name = "listId">
        ///   The identifier of the list that contains historical alarms and 
        ///   events that are to be read.
        /// </param>
        /// <param name = "firstTimeStamp">
        ///   The filter that specifies the first or beginning (of returned list) 
        ///   timestamp for event messages to be returned.  Valid operands include 
        ///   the Timestamp (UTC) constant defined by the FilterOperand class.
        /// </param>
        /// <param name = "secondTimeStamp">
        ///   The filter that specifies the second or ending (of returned list)
        ///   timestamp for event messages to be returned.  Valid operands include 
        ///   the Timestamp (UTC) constant defined by the FilterOperand class.
        /// </param>
        /// <param name = "numEventMessages">
        ///   The maximum number of EventMessages to be returned.
        /// </param>
        /// <param name = "filterSet">
        ///   The set of filters used to select historical alarms and events 
        ///   to be read.
        /// </param>
        /// <returns>
        ///   The list of selected historical alarms and events.
        ///   Or null if no alarms or events were selected.
        /// </returns>
        internal EventMessage[] OnReadJournalEvents(uint listId, FilterCriterion firstTimeStamp,
                                                    FilterCriterion secondTimeStamp, uint numEventMessages,
                                                    FilterSet filterSet)
        {
            TListRoot xiList = null;

            using (SyncRoot.Enter())
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ServerContext.");

                if (_listManager.TryGetValue(listId, out xiList))
                    xiList.AuthorizeEndpointUse(typeof (IRead)); // throws an exception if validation fails					
                else throw RpcExceptionHelper.Create(XiFaultCodes.E_BADLISTID, "List Id not found in Read Journal Events.");
            }

            return xiList.OnReadJournalEvents(firstTimeStamp, secondTimeStamp, numEventMessages, filterSet);
        }

        /// <summary>
        ///   This method is to be overridden by the context implementation in the 
        ///   Server Implementation project.
        /// </summary>
        /// <param name = "listId">
        ///   The identifier of the list that contains historical alarms and 
        ///   events that are to be read.
        /// </param>
        /// <param name = "numEventMessages">
        ///   The maximum number of EventMessages to return.
        /// </param>
        /// <returns>
        ///   The list of selected historical alarms and events.
        ///   Null if no alarms or events were selected.
        /// </returns>
        internal EventMessage[] OnReadJournalEventsNext(uint listId, uint numEventMessages)
        {
            TListRoot xiList = null;

            using (SyncRoot.Enter())
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ServerContext.");

                if (_listManager.TryGetValue(listId, out xiList))
                    xiList.AuthorizeEndpointUse(typeof (IRead)); // throws an exception if validation fails					
                else
                    throw RpcExceptionHelper.Create(XiFaultCodes.E_BADLISTID, "List Id not found in Read Journal Events Next.");
            }

            return xiList.OnReadJournalEventsNext(numEventMessages);
        }

        #endregion
    }
}