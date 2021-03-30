using Ssz.Utils;
using Ssz.Utils.DataSource;
using System;
using System.Text;

namespace Ssz.DataGrpc.Common
{
    /// <summary>
    ///     The DataGrpc Data Value class is the Client Base representation of data values.
    /// </summary>
    public class ValueStatusTimestampHelper
    {
        #region public functions

        /// <summary>
        ///     It creates a data value with a good status.
        /// </summary>
        public static ValueStatusTimestamp NewValueStatusTimestamp(Any value, DateTime timestampUtc)
        {
            var valueStatusTimestamp = new ValueStatusTimestamp();

            valueStatusTimestamp.Value = value;
            valueStatusTimestamp.StatusCode =
                DataGrpcStatusCode.MakeStatusCode(
                    DataGrpcStatusCode.MakeStatusByte((byte)DataGrpcStatusCodeStatusBits.GoodNonSpecific, 0),
                    DataGrpcStatusCode.MakeFlagsByte((byte)DataGrpcStatusCodeHistoricalValueType.NotUsed, false, false,
                        DataGrpcStatusCodeAdditionalDetailType.NotUsed),
                    0);
            valueStatusTimestamp.TimestampUtc = timestampUtc;

            return valueStatusTimestamp;
        }

        #endregion
    }
}