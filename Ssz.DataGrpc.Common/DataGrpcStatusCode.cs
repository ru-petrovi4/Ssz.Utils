namespace Ssz.DataGrpc.Common
{
    /// <summary>
    ///     <para>
    ///         The Xi status code is a structured 32-bit unsigned integer.  This class
    ///         defines the structure and provides properties used to extract and set
    ///         bit-fields of the 32-bit status code.
    ///     </para>
    ///     <para>
    ///         The structure of the status code is defined as follows, where bit 32
    ///         is the Most Significant Bit (MSB):
    ///     </para>
    ///     <para>Bits    Property</para>
    ///     <para>32-25   StatusByte</para>
    ///     <para>24-17   FlagsByte</para>
    ///     <para>16-1    AdditionalDetail</para>
    ///     <para>The StatusByte contains the success/error code associated with the value. </para>
    ///     <para>
    ///         The FlagsByte contains codes that further describe the status of historical values,
    ///         and that identify the format of the AdditionalDetail bits.
    ///     </para>
    ///     <para>
    ///         The AdditionalDetail is a 16-bit value that allows the server to provide
    ///         additional detail about the value. These bits can contain a vendor-specific
    ///         code or an HRESULT as indicated by the FlagsByte.  The values for the
    ///         AdditionalDetail are defined by Xi.Contracts.Constants.AdditionalDetailType.
    ///     </para>
    ///     <para>16-1    AdditionalDetail</para>
    /// </summary>    
    public class DataGrpcStatusCode
    {
        #region public functions

        /// <summary>
        ///     This method creates a status code from the status byte, flags byte, and additional detail.
        /// </summary>
        /// <param name="statusByte">
        ///     The StatusByte to be incorporated into the Status Code.
        /// </param>
        /// <param name="flagsByte">
        ///     The FlagsByte to be incorporated into the Status Code.
        /// </param>
        /// <param name="additionalDetail">
        ///     The AdditionalDetail to be incorporated into the Status Code.
        /// </param>
        /// <returns>
        ///     The newly constructed Status Code.
        /// </returns>
        public static uint MakeStatusCode(byte statusByte, byte flagsByte, ushort additionalDetail)
        {
            return (uint) (statusByte << 24) | (uint) (flagsByte << 16) | additionalDetail;
        }

        /// <summary>
        ///     This method creates a status byte from status bits and limit bits.
        /// </summary>
        /// <param name="statusBits">
        ///     The StatusBits to be incorporated into the Status Byte.
        /// </param>
        /// <param name="limitBits">
        ///     The LimitBits to be incorporated into the Status Byte.
        /// </param>
        /// <returns>
        ///     The newly constructed Status Byte.
        /// </returns>
        public static byte MakeStatusByte(byte statusBits, byte limitBits)
        {
            return (byte) ((statusBits << 2) | (limitBits & 0x03));
        }

        /// <summary>
        ///     <para>
        ///         StatusByte is an 8-bit property that specifies the status of the
        ///         data value. It is formatted as follows:
        ///     </para>
        ///     <para>  SSBBBBLL, where</para>
        ///     <para>      SSBBBB   = StatusBits (most significant bits)</para>
        ///     <para>          LL   = LimitBits</para>
        /// </summary>
        /// <param name="statusCode">
        ///     The 32-bit status code from which the status byte is to be extracted.
        /// </param>
        /// <returns>
        ///     The 8-bit status byte.
        /// </returns>
        public static byte StatusByte(uint statusCode)
        {
            return (byte) (statusCode >> 24);
        }

        /// <summary>
        ///     StatusBits contains a 2-bit value that indicates whether the value is good,
        ///     bad, or uncertain, that is followed by a 4-bit value that provides a description
        ///     of the status.
        /// </summary>
        /// <param name="statusCode">
        ///     The 32-bit status code from which the status bits are
        ///     to be extracted.
        /// </param>
        /// <returns>
        ///     The byte value of this 2-bit property.
        /// </returns>
        public static byte StatusBits(uint statusCode)
        {
            return (byte) (statusCode >> 26);
        }

        /// <summary>
        ///     LimitBits is a 2-bit property that describes if and how the associated value
        ///     is limited.  It value is independent of the value of the StatusBits.
        /// </summary>
        /// <param name="statusCode">
        ///     The 32-bit status code from which the limit bits are
        ///     to be extracted.
        /// </param>
        /// <returns>
        ///     The byte value of this 2-bit property.
        /// </returns>
        public static byte LimitBits(uint statusCode)
        {
            return (byte) ((statusCode >> 24) & 0x03);
        }

        /// <summary>
        ///     <para>
        ///         This method creates the FlagsByte from its components. It is
        ///         formatted as follows:
        ///     </para>
        ///     <para>  VVVNCAAA, where</para>
        ///     <para>      VVV  = Historical Value Type (most significant bits)</para>
        ///     <para>      N    = Historical No Bounding</para>
        ///     <para>      C    = Historical Conversion Error</para>
        ///     <para>      AAA  = Additional Detail Desc</para>
        /// </summary>
        /// <param name="historicalValueType">
        ///     The 3-bit property that defines how the HistoricalValue property is used.
        ///     The XiStatusCodeHistoricalValueType enum defines the values for this property.
        /// </param>
        /// <param name="historicalNoBounding">
        ///     The boolean that indicates whether or not a bounding value was included
        ///     in the historical value associated with this StatusCode.
        /// </param>
        /// <param name="historicalConversionError">
        ///     The boolean that indicates whether or not a a scaling / conversionm error
        ///     occurred for the historical value associated with this StatusCode.
        /// </param>
        /// <param name="additionalDetailDesc">
        ///     The 3-bit description of the AdditionalDetail property.
        ///     The XiStatusCodeAdditionalDetailType enum defines the values for this property.
        /// </param>
        /// <returns>
        ///     The FlagsByte.
        /// </returns>
        public static byte MakeFlagsByte(byte historicalValueType, bool historicalNoBounding,
            bool historicalConversionError, byte additionalDetailDesc)
        {
            byte bitFlags = 0;
            if (historicalNoBounding)
                bitFlags = 0x10;
            if (historicalConversionError)
                bitFlags |= 0x08;
            return (byte) ((historicalValueType << 5) | (bitFlags) | (additionalDetailDesc & 0x07));
        }

        /// <summary>
        ///     <para>
        ///         FlagsByte is an 8-bit property composed of bitfields that define how
        ///         the AdditionalDetail property is to be interpreted and that provide historical
        ///         status information for historical data values.  It is formatted as follows:
        ///     </para>
        ///     <para>  VVVNCAAA, where</para>
        ///     <para>      VVV  = Historical Value Type (most significant bits)</para>
        ///     <para>      N    = Historical No Bounding</para>
        ///     <para>      C    = Historical Conversion Error</para>
        ///     <para>      AAA  = Additional Detail Desc</para>
        /// </summary>
        /// <param name="statusCode">
        ///     The 32-bit status code from which the flags byte is to be extracted.
        /// </param>
        /// <returns>
        ///     The 8-bit flags byte.
        /// </returns>
        public static byte FlagsByte(uint statusCode)
        {
            return (byte) ((statusCode >> 16) & 0xFF);
        }

        /// <summary>
        ///     <para>
        ///         The HistoricalValueType is a 3-bit property that describes the
        ///         the historical data value associated with this status code.  The
        ///         StatusCodeHistoricalValueType enumeration defines the values for
        ///         this property.
        ///     </para>
        /// </summary>
        /// <param name="statusCode">
        ///     The 32-bit status code from which HistoricalValueTypeBits is to be extracted.
        /// </param>
        /// <returns>
        ///     The HistoricalValueTypeBits value.
        /// </returns>
        public static byte HistoricalValueType(uint statusCode)
        {
            return (byte) ((statusCode >> 21) & 0x07);
        }

        /// <summary>
        ///     NoBoundingDataFlag is an 1-bit property that indicates whether or not
        ///     bounding data was included in the historical data value associated with this
        ///     status code.
        /// </summary>
        /// <param name="statusCode">
        ///     The 32-bit status code from which NoBoundingDataFlag is to be extracted.
        /// </param>
        /// <returns>
        ///     The NoBoundingDataFlag boolean value.
        /// </returns>
        public static bool NoBoundingDataFlag(uint statusCode)
        {
            return (bool) ((statusCode & 0x100000) > 0);
        }

        /// <summary>
        ///     ConversionErrorFlag is an 1-bit property that indicates whether or not
        ///     the historical data value associated with this status code had a
        ///     conversion/scaling error.
        /// </summary>
        /// <param name="statusCode">
        ///     The 32-bit status code from which ConversionErrorFlag is to be extracted.
        /// </param>
        /// <returns>
        ///     The ConversionErrorFlag boolean value.
        /// </returns>
        public static bool ConversionErrorFlag(uint statusCode)
        {
            return (bool) ((statusCode & 0x80000) > 0);
        }

        /// <summary>
        ///     <para>
        ///         AdditionalDetailType is an 3-bit property that indicates how the
        ///         AdditionalDetail property is used.  The StatusCodeAdditionalDetailType
        ///         enumeration defines the values for this property.
        ///     </para>
        /// </summary>
        /// <param name="statusCode">
        ///     The 32-bit status code from which AdditionalDetailType is to be extracted.
        /// </param>
        /// <returns>
        ///     The AdditionalDetailType value.
        /// </returns>
        public static byte AdditionalDetailType(uint statusCode)
        {
            return (byte) ((statusCode >> 16) & 0x07);
        }

        #endregion
    }
}