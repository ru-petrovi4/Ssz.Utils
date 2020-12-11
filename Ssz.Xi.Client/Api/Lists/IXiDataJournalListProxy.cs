using System;
using System.Collections.Generic;
using Ssz.Xi.Client.Api.ListItems;
using Xi.Contracts.Data;

namespace Ssz.Xi.Client.Api.Lists
{
    public interface IXiDataJournalListProxy : IXiListProxy<IXiDataJournalListItem>
    {
        /// <summary>
        ///     <para>
        ///         This method is used to read the historical values that fall between a start and end time for one or more
        ///         data objects within a specific data journal list.
        ///     </para>
        /// </summary>
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
        /// <param name="numValuesPerDataObject"> The maximum number of values to be returned for each data object. </param>
        /// <param name="xiValueStatusTimestampSetCollection">
        ///     The list of data objects whose historical values are to be read. Each data
        ///     object is represented by a value set that contains the values selected and returned by the server.
        /// </param>
        void ReadJournalDataForTimeInterval(FilterCriterion firstTimeStamp, FilterCriterion secondTimeStamp,
            uint numValuesPerDataObject,
            IEnumerable<IXiDataJournalValueStatusTimestampSet>? xiValueStatusTimestampSetCollection);

        /// <summary>
        ///     This method is used to read the historical values at specific times for
        ///     one or more data objects within a specific data journal list.  If no element exists
        ///     at the specified time in the data journal for an object, the server creates an
        ///     interpolated value for that time and includes it in the response as though it
        ///     actually existed in the journal.
        /// </summary>
        /// <param name="timestamps">
        ///     Identifies the timestamps of historical values to be returned for each of the requested data
        ///     objects.
        /// </param>
        /// <param name="xiValueStatusTimestampSetList">
        ///     The list of data objects whose historical values are to be read. Each data
        ///     object is represented by a value set that contains the values selected and returned by the server.
        /// </param>
        void ReadJournalDataAtSpecificTimes(List<DateTime> timestamps,
            List<IXiDataJournalValueStatusTimestampSet> xiValueStatusTimestampSetList);

        /// <summary>
        ///     This method is used to read calculated historical values (e.g. averages or
        ///     interpolations) for one or more data objects within a specific data journal list.
        ///     The time-range used to select the historical values is specified by the client.
        ///     Additionally, the client specifies a calculation period that divides that time
        ///     range into periods. The server calculates a return value for each of these periods.
        /// </summary>
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
        /// <param name="xiValueStatusTimestampSetList">
        ///     The list of data objects whose historical values are to be read. Each data
        ///     object is represented by a value set that contains the values calculated and returned by the server.
        /// </param>
        void ReadCalculatedJournalData(FilterCriterion firstTimeStamp, FilterCriterion secondTimeStamp,
            TimeSpan calculationPeriod,
            List<IXiDataJournalValueStatusTimestampSet> xiValueStatusTimestampSetList);
    }
}