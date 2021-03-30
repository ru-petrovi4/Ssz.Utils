using System;
using System.Text;
using Ssz.Utils;
using Ssz.Utils.DataSource;
using Xi.Contracts.Constants;
using Xi.Contracts.Data;

namespace Ssz.Xi.Client.Api
{
    /// <summary>
    ///     The Xi Data Value class is the Client Base representation of data values.
    /// </summary>
    public static class ValueStatusTimestampHelper
    {
        #region public functions

        /// <summary>
        ///     It creates a data value
        ///     with a good status and DateTime.MinValue timestamp.
        /// </summary>
        public static ValueStatusTimestamp NewValueStatusTimestamp(Any value, DateTime timestampUtc)
        {
            var valueStatusTimestamp = new ValueStatusTimestamp();

            valueStatusTimestamp.Value = value;
            valueStatusTimestamp.StatusCode =
                XiStatusCode.MakeStatusCode(
                    XiStatusCode.MakeStatusByte((byte)XiStatusCodeStatusBits.GoodNonSpecific, 0),
                    XiStatusCode.MakeFlagsByte((byte)XiStatusCodeHistoricalValueType.NotUsed, false, false,
                        XiStatusCodeAdditionalDetailType.NotUsed),
                    0);
            valueStatusTimestamp.TimestampUtc = timestampUtc;

            return valueStatusTimestamp;
        }

        #endregion
    }
}