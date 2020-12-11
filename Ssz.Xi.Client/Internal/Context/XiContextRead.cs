using System;
using System.Collections.Generic;
using Ssz.Xi.Client.Internal.Endpoints;
using Xi.Contracts.Data;

namespace Ssz.Xi.Client.Internal.Context
{

    #region Read

    /// <summary>
    ///     This partial class defines the IRead related aspects of the XiContext class.
    /// </summary>
    internal partial class XiContext
    {
        #region public functions

        /// <summary>
        ///     <para>
        ///         This method is used to read the values of one or more data objects in a list. It is also used as a
        ///         keep-alive for the read endpoint by setting the listId parameter to 0. In this case, null is returned
        ///         immediately.
        ///     </para>
        /// </summary>
        /// <param name="serverListId">
        ///     The server identifier of the list that contains data objects to be read. Null if this is a
        ///     keep-alive.
        /// </param>
        /// <param name="serverAliases"> The server aliases of the data objects to read. </param>
        /// <returns>
        ///     <para>
        ///         The list of requested values. Each value in this list is identified by its client alias. If the server alias
        ///         for a data object to read was not found, an ErrorInfo object will be returned that contains the server alias
        ///         instead of a value, status, and timestamp.
        ///     </para>
        ///     <para> Returns null if this is a keep-alive. </para>
        /// </returns>
        public DataValueArraysWithAlias? ReadData(uint serverListId, List<uint> serverAliases)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiContext.");

            if (_readEndpoint == null) throw new Exception("No Read Endpoint");

            if (_readEndpoint.Disposed) return null;

            DataValueArraysWithAlias? readValueList = null;
            if (XiEndpointRoot.CreateChannelIfNotCreated(_readEndpoint))
            {
                try
                {
                    readValueList = _readEndpoint.Proxy.ReadData(ContextId, serverListId,
                        serverAliases);


                    _readEndpoint.LastCallUtc = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    ProcessRemoteMethodCallException(ex);
                }
            }
            return readValueList;
        }

        /// <summary>
        ///     <para>
        ///         This method is used to read the historical values that fall between a start and end time for one or more
        ///         data objects within a specific data journal list.
        ///     </para>
        /// </summary>
        /// <param name="serverListId">
        ///     The server identifier of the list that contains data objects whose historical values are to
        ///     be read.
        /// </param>
        /// <param name="firstTimeStamp">
        ///     The filter that specifies the first or beginning (of returned list) timestamp for values
        ///     to be returned. Valid operands include the Timestamp (UTC) and OpcHdaTimestampStr constants defined by the
        ///     FilterOperand class. The FilterOperand Operator is used to determine if the returned data should include data
        ///     values the occur exactly at the first or second time stamp. If the equals operator is specified then values that
        ///     occur at the first and second time stamp will be included in the sample set. Any other operator will not include
        ///     first or second time stamped values.
        /// </param>
        /// <param name="secondTimeStamp">
        ///     The filter that specifies the second or ending (of returned list) timestamp for values
        ///     to be returned. Valid operands include the Timestamp (UTC) and OpcHdaTimestampStr constants defined by the
        ///     FilterOperand class. The FilterOperand Operator is not used.
        /// </param>
        /// <param name="numValuesPerAlias"> The maximum number of JournalDataReturnValues to be returned per alias. </param>
        /// <param name="serverAliases"> The list of server aliases for the data objects whose historical values are to be read. </param>
        /// <returns> The list of requested historical values, or the reason they could not be read. </returns>
        public JournalDataValues[]? ReadJournalDataForTimeInterval(uint serverListId, FilterCriterion firstTimeStamp,
            FilterCriterion secondTimeStamp,
            uint numValuesPerAlias, List<uint> serverAliases)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiContext.");

            if (_readEndpoint == null) throw new Exception("No Read Endpoint");

            if (_readEndpoint.Disposed) return new JournalDataValues[0];

            JournalDataValues[]? listJDRV = null;
            if (XiEndpointRoot.CreateChannelIfNotCreated(_readEndpoint))
            {
                try
                {
                    listJDRV = _readEndpoint.Proxy.ReadJournalDataForTimeInterval(ContextId,
                        serverListId,
                        firstTimeStamp,
                        secondTimeStamp,
                        numValuesPerAlias,
                        serverAliases);

                    _readEndpoint.LastCallUtc = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    ProcessRemoteMethodCallException(ex);
                }
            }
            return listJDRV;
        }

        /// <summary>
        ///     <para>
        ///         This method is used to read the historical values at specific times for one or more data objects within a
        ///         specific data journal list. If no item exists at the specified time in the data journal for an object, the
        ///         server creates an interpolated value for that time and includes it in the response as though it actually
        ///         existed in the journal.
        ///     </para>
        /// </summary>
        /// <param name="serverListId">
        ///     The server identifier of the list that contains data objects whose historical values are to
        ///     be read.
        /// </param>
        /// <param name="timestamps">
        ///     Identifies the timestamps of historical values to be returned for each of the requested data
        ///     objects.
        /// </param>
        /// <param name="serverAliases"> The list of server aliases for the data objects whose historical values are to be read. </param>
        /// <returns> The list of requested historical values, or the reason they could not be read. </returns>
        public JournalDataValues[]? ReadJournalDataAtSpecificTimes(uint serverListId, List<DateTime> timestamps,
            List<uint> serverAliases)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiContext.");

            if (_readEndpoint == null) throw new Exception("No Read Endpoint");

            JournalDataValues[]? listJDRV = null;
            if (XiEndpointRoot.CreateChannelIfNotCreated(_readEndpoint))
            {
                try
                {
                    listJDRV = _readEndpoint.Proxy.ReadJournalDataAtSpecificTimes(ContextId,
                        serverListId,
                        timestamps,
                        serverAliases);

                    _readEndpoint.LastCallUtc = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    ProcessRemoteMethodCallException(ex);
                }
            }
            return listJDRV;
        }

        /// <summary>
        ///     <para>
        ///         This method is used to read changed historical values for one or more data objects within a specific data
        ///         journal list. Changed historical values are those that were entered into the journal and then changed
        ///         (corrected) by an operator or other user.
        ///     </para>
        /// </summary>
        /// <param name="serverListId">
        ///     The server identifier of the list that contains data objects whose historical values are to
        ///     be read.
        /// </param>
        /// <param name="firstTimeStamp">
        ///     The filter that specifies the inclusive earliest (oldest) timestamp for values to be
        ///     returned. Valid operands include the Timestamp and OpcHdaTimestampStr constants defined by the FilterOperand class.
        /// </param>
        /// <param name="secondTimeStamp">
        ///     The filter that specifies the inclusive newest (most recent) timestamp for values to be
        ///     returned. Valid operands include the Timestamp and OpcHdaTimestampStr constants defined by the FilterOperand class.
        /// </param>
        /// <param name="serverAliases"> The list of server aliases for the data objects whose historical values are to be read. </param>
        /// <param name="numValuesPerAlias"> The maximum number of JournalDataChangedValues to be returned per alias. </param>
        /// <returns>
        ///     The list of requested historical values, or the reason they could not be read. If, however, the number
        ///     returned for any alias is equal to numValuesPerDataObject, then the client should issue a
        ///     ReadJournalDataChangesNext() to retrieve any remaining values.
        /// </returns>
        public JournalDataChangedValues[]? ReadJournalDataChanges(uint serverListId, FilterCriterion firstTimeStamp,
            FilterCriterion secondTimeStamp,
            uint numValuesPerAlias, List<uint> serverAliases)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiContext.");

            if (_readEndpoint == null) throw new Exception("No Read Endpoint");

            JournalDataChangedValues[]? listJDCV = null;
            if (XiEndpointRoot.CreateChannelIfNotCreated(_readEndpoint))
            {
                try
                {
                    listJDCV = _readEndpoint.Proxy.ReadJournalDataChanges(ContextId, serverListId,
                        firstTimeStamp,
                        secondTimeStamp,
                        numValuesPerAlias,
                        serverAliases);


                    _readEndpoint.LastCallUtc = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    ProcessRemoteMethodCallException(ex);
                }
            }
            return listJDCV;
        }

        /// <summary>
        ///     <para>
        ///         This method is used to read calculated historical values (e.g. averages or interpolations) for one or more
        ///         data objects within a specific data journal list. The time-range used to select the historical values is
        ///         specified by the client. Additionally, the client specifies a calculation period that divides that time range
        ///         into periods. The server calculates a return value for each of these periods.
        ///     </para>
        /// </summary>
        /// <param name="serverListId">
        ///     The server identifier of the list that contains data objects whose historical values are to
        ///     be read.
        /// </param>
        /// <param name="firstTimeStamp">
        ///     The filter that specifies the inclusive earliest (oldest) timestamp for values to be
        ///     returned. Valid operands include the Timestamp and OpcHdaTimestampStr constants defined by the FilterOperand class.
        /// </param>
        /// <param name="secondTimeStamp">
        ///     The filter that specifies the inclusive newest (most recent) timestamp for values to be
        ///     returned. Valid operands include the Timestamp and OpcHdaTimestampStr constants defined by the FilterOperand class.
        /// </param>
        /// <param name="calculationPeriod">
        ///     The time span used to divide the specified time range into individual periods for
        ///     which return values are calculated. The specified calculation is performed on the set of historical values of a
        ///     data object that fall within each period.
        /// </param>
        /// <param name="serverAliasesAndCalculations">
        ///     The list of server aliases for the data objects whose historical values are
        ///     to be calculated, and the calculation to perform for each.
        /// </param>
        /// <returns>
        ///     The set of calculated values. There is one value for each calculation period within the specified time range
        ///     for each specific data object.
        /// </returns>
        public JournalDataValues[]? ReadCalculatedJournalData(uint serverListId, FilterCriterion firstTimeStamp,
            FilterCriterion secondTimeStamp,
            TimeSpan calculationPeriod,
            List<AliasAndCalculation> serverAliasesAndCalculations)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiContext.");

            if (_readEndpoint == null) throw new Exception("No Read Endpoint");

            JournalDataValues[]? listJDRV = null;
            if (XiEndpointRoot.CreateChannelIfNotCreated(_readEndpoint))
            {
                try
                {
                    listJDRV = _readEndpoint.Proxy.ReadCalculatedJournalData(ContextId,
                        serverListId,
                        firstTimeStamp,
                        secondTimeStamp,
                        calculationPeriod,
                        serverAliasesAndCalculations);


                    _readEndpoint.LastCallUtc = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    ProcessRemoteMethodCallException(ex);
                }
            }
            return listJDRV;
        }

        /// <summary>
        ///     This method reads the properties associated with a historized data object.
        /// </summary>
        /// <param name="serverListId">
        ///     The server identifier of the list that contains data objects whose property values are to
        ///     be read.
        /// </param>
        /// <param name="firstTimeStamp">
        ///     The filter that specifies the inclusive earliest (oldest) timestamp for values to be
        ///     returned. Valid operands include the Timestamp and OpcHdaTimestampStr constants defined by the FilterOperand class.
        /// </param>
        /// <param name="secondTimeStamp">
        ///     The filter that specifies the inclusive newest (most recent) timestamp for values to be
        ///     returned. Valid operands include the Timestamp and OpcHdaTimestampStr constants defined by the FilterOperand class.
        /// </param>
        /// <param name="serverAlias"> The server alias of the data object whose property values are to be read. </param>
        /// <param name="propertiesToRead">
        ///     The TypeIds of the properties to read. Each property is identified by its property
        ///     type.
        /// </param>
        /// <returns> The array of requested property values. </returns>
        public JournalDataPropertyValue[]? ReadJournalDataProperties(uint serverListId, FilterCriterion firstTimeStamp,
            FilterCriterion secondTimeStamp, uint serverAlias,
            List<TypeId> propertiesToRead)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiContext.");

            if (_readEndpoint == null) throw new Exception("No Read Endpoint");

            JournalDataPropertyValue[]? JDPVarray = null;
            if (XiEndpointRoot.CreateChannelIfNotCreated(_readEndpoint))
            {
                try
                {
                    JDPVarray = _readEndpoint.Proxy.ReadJournalDataProperties(ContextId,
                        serverListId,
                        firstTimeStamp,
                        secondTimeStamp,
                        serverAlias,
                        propertiesToRead);


                    _readEndpoint.LastCallUtc = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    ProcessRemoteMethodCallException(ex);
                }
            }
            return JDPVarray;
        }

        #endregion
    }

    #endregion // Read
}