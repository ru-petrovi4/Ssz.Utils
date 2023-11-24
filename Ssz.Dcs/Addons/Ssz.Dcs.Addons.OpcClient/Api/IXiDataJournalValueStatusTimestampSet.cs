using Ssz.Utils.DataAccess;
using System;
using System.Collections;
using System.Collections.Generic;
using Xi.Contracts.Data;

namespace Ssz.Xi.Client.Api
{
    /// <summary>
    /// 
    /// </summary>
    public interface IXiDataJournalValueStatusTimestampSet : IEnumerable,
        IEnumerable<ValueStatusTimestamp>
    {
        /// <summary>
        ///     The type of calculation used to create the value set as defined by the
        ///     StandardMib.DataJournalOptions.MathLibrary of the server and by
        ///     Xi.Contracts.Constants.JournalDataSampleTypes. The historical data object
        ///     for which this value set is defined may not have two value sets with the
        ///     same CalculationTypeId.
        /// </summary>
        Ssz.Utils.DataAccess.TypeId CalculationTypeId { get; }

        /// <summary>
        ///     This property contains the Result Code associated with reading this value
        ///     set from the server. See XiFaultCodes class for standardized result codes.
        /// </summary>
        uint ResultCode { get; }

        /// <summary>
        ///     This property defines the starting time for this list historical values.
        ///     Values in the value set will be between the StartTime and EndTime. The
        ///     read method used to access the values specifies whether or not values
        ///     with the starting or ending times are to be included in this value set.
        /// </summary>
        DateTime StartTime { get; }

        /// <summary>
        ///     This property defines the ending time for this list historical values.
        ///     Values in the value set will be between the StartTime and EndTime. The
        ///     read method used to access the values specifies whether or not values
        ///     with the starting or ending times are to be included in this value set.
        /// </summary>
        DateTime EndTime { get; }

        /// <summary>
        ///     This property contains the number of values in this value set.
        /// </summary>
        int Count { get; }

        /// <summary>
        ///     The property defines an object that the client application can use to
        ///     associate this value set with an object of its choice.
        /// </summary>
        object? Tag { get; set; }
    }
}