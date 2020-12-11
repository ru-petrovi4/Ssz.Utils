using System;
using System.Text;
using Ssz.Utils;
using Xi.Contracts.Constants;
using Xi.Contracts.Data;

namespace Ssz.Xi.Client.Api
{
    /// <summary>
    ///     The Xi Data Value class is the Client Base representation of data values.
    /// </summary>
    public class XiValueStatusTimestamp
    {
        #region construction and destruction

        /// <summary>
        ///     The default constructor. It creates a data value
        ///     with a bad status and DateTime.MinValue timestamp.
        /// </summary>
        public XiValueStatusTimestamp()
        {
            Value = new Any(null);
            StatusCode = 0xffffffff;
            TimestampUtc = DateTime.MinValue;
        }

        /// <summary>
        ///     Constructor. It creates a data value
        ///     with a good status and DateTime.MinValue timestamp.
        /// </summary>
        public XiValueStatusTimestamp(Any value)
        {
            Value = value;
            StatusCode =
                XiStatusCode.MakeStatusCode(
                    XiStatusCode.MakeStatusByte((byte) XiStatusCodeStatusBits.GoodNonSpecific, 0),
                    XiStatusCode.MakeFlagsByte((byte) XiStatusCodeHistoricalValueType.NotUsed, false, false,
                        XiStatusCodeAdditionalDetailType.NotUsed),
                    0);
            TimestampUtc = DateTime.MinValue;
        }

        #endregion

        #region public functions

        /// <summary>
        ///     Provides an simple overload for the ToString() method that combines the
        ///     data type, timestamp, status code, and value into a single string.
        /// </summary>
        /// <returns> </returns>
        public override string ToString()
        {
            var sb = new StringBuilder(Value.ValueType.ToString());
            sb.Append(", V = ");
            sb.Append(Value);
            sb.Append(", TS = ");
            sb.Append(TimestampUtc.ToString("u"));
            sb.Append(", SC = 0x");
            sb.Append(StatusCode.ToString("X4"));
            return sb.ToString();
        }

        /// <summary>
        ///     This property is the value of the data item.
        /// </summary>
        public Any Value;

        /// <summary>
        ///     This property is the 32-bit status associated with the value.
        ///     Status codes are defined by the Xi.Contracts.Data.XiStatusCode class.
        /// </summary>
        public uint StatusCode;

        /// <summary>
        ///     This property is the UTC DateTime timestamp of this value.
        /// </summary>
        public DateTime TimestampUtc;

        #endregion
    }
}