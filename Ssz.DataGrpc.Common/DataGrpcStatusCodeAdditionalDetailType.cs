namespace Ssz.DataGrpc.Common
{
    /// <summary>
    ///     The AdditionalDetailType indicates how the 16-bit AdditionalDetail
    ///     property of the StatusCode is used. Unused values are reserved.
    /// </summary>
    public class DataGrpcStatusCodeAdditionalDetailType
    {
        #region public functions

        /// <summary>
        ///     This property returns the AdditionalDetail as a 16-bit vendor-specific value
        ///     if the AdditionalDetailType is set to AdditionalDetailType.VendorSpecific.
        ///     If the AdditionalDetailType is set to a different value, 0 is returned.
        /// </summary>
        /// <param name="statusCode">
        ///     The 32-bit status code from which AdditionalDetail is to be extracted.
        /// </param>
        /// <returns>
        ///     The vendor-specific AdditionalDetail value. 0 if the AdditionalDetailType
        ///     indicates that the AdditionalDetail does not contain a vendor-specific value.
        /// </returns>
        public static ushort VendorSpecific(uint statusCode)
        {
            if (DataGrpcStatusCode.AdditionalDetailType(statusCode) == (byte) VendorSpecificDetail)
                return (ushort) ((statusCode) & AdditionalDetailMask);
            return 0;
        }

        /// <summary>
        ///     This mask value may be used to keep only the Additional Detail Type code.
        /// </summary>
        public const uint AdditionalDetailTypeMask = 0x00070000;

        /// <summary>
        ///     Use this mask to keep the Additional Details Value.
        /// </summary>
        public const uint AdditionalDetailMask = 0x0000FFFF;

        /// <summary>
        ///     The AdditionalDetail property is not used and should be ignored.
        ///     Its value should be set to 0.
        /// </summary>
        public const byte NotUsed = 0;

        /// <summary>
        ///     The AdditionalDetail property contains a vendor-specific value.
        /// </summary>
        public const byte VendorSpecificDetail = 1;

        /// <summary>
        ///     The AdditionalDetail property contains a vendor-specific value
        ///     This value is in Xi Status Code bit position.
        /// </summary>
        public const uint VendorSpecificDetailBits = 0x00010000;

        /// <summary>
        ///     The AdditionalDetail property contains the low order 16-bits
        ///     of the default HRESULT (Facility Code = 0). The StatusCode.HRESULT()
        ///     method creates this HRESULT from the status code.
        /// </summary>
        public const byte DefaultHResult = 2;

        /// <summary>
        ///     The AdditionalDetail property contains the low order 16-bits
        ///     of the default HRESULT (Facility Code = 0). The StatusCode.HRESULT()
        ///     method creates this HRESULT from the status code.
        ///     This value is in Xi Status Code bit position.
        /// </summary>
        public const uint DefaultHResultBits = 0x00020000;

        /// <summary>
        ///     The AdditionalDetail property contains the low order 16-bits
        ///     of an Xi HRESULT (Facility Code = 0x777). The StatusCode.HRESULT()
        ///     method creates this HRESULT from the status code.
        /// </summary>
        public const byte XiHResult = 3;

        /// <summary>
        ///     The AdditionalDetail property contains the low order 16-bits
        ///     of an Xi HRESULT (Facility Code = 0x777). The StatusCode.HRESULT()
        ///     method creates this HRESULT from the status code.
        ///     This value is in Xi Status Code bit position.
        /// </summary>
        public const uint XiHResultBits = 0x00030000;

        /// <summary>
        ///     The AdditionalDetail property contains the low order 16-bits
        ///     of a FACILITY_IO_ERROR_CODE NTSTATUS (Facility Code = 4). The
        ///     StatusCode.HRESULT() method creates this HRESULT from the status code.
        /// </summary>
        public const byte IO_ERROR_CODE = 4;

        /// <summary>
        ///     The AdditionalDetail property contains the low order 16-bits
        ///     of a FACILITY_IO_ERROR_CODE NTSTATUS (Facility Code = 4). The
        ///     StatusCode.HRESULT() method creates this HRESULT from the status code.
        ///     This value is in Xi Status Code bit position.
        /// </summary>
        public const uint IO_ERROR_CODEBits = 0x00040000;

        /// <summary>
        ///     The AdditionalDetail property contains the low order 16-bits
        ///     of a COM FACILITY_ITF HRESULT (Facility Code = 4). The
        ///     StatusCode.HRESULT() method creates this HRESULT from the status code.
        /// </summary>
        public const byte ITF_HResult = 5;

        /// <summary>
        ///     The AdditionalDetail property contains the low order 16-bits
        ///     of a COM FACILITY_ITF HRESULT (Facility Code = 4). The
        ///     StatusCode.HRESULT() method creates this HRESULT from the status code.
        ///     This value is in Xi Status Code bit position.
        /// </summary>
        public const uint ITF_HResultBits = 0x00050000;

        /// <summary>
        ///     The AdditionalDetail property contains the low order 16-bits
        ///     of a Win32 HRESULT (Facility Code = 5). The StatusCode.HRESULT()
        ///     method creates this HRESULT from the status code.
        /// </summary>
        public const byte Win32HResult = 6;

        /// <summary>
        ///     The AdditionalDetail property contains the low order 16-bits
        ///     of a Win32 HRESULT (Facility Code = 5). The StatusCode.HRESULT()
        ///     method creates this HRESULT from the status code.
        ///     This value is in Xi Status Code bit position.
        /// </summary>
        public const uint Win32HResultBits = 0x00060000;

        /// <summary>
        ///     <para>
        ///         This code is used to indicate that an additional HRESULT
        ///         accompanies this StatusCode.  The additional HRESULT is contained
        ///         in the HResult member of an ErrorInfo object that is located in the
        ///         ErrorInfo list contained in the DataValueArrays in which this StatusCode
        ///         is present.
        ///     </para>
        ///     <para>
        ///         This code does not have to be present if the Context was opened
        ///         with ContextOptions set to DebugErrorMessages using either the
        ///         Initiate() or ReInitiate() method.
        ///     </para>
        /// </summary>
        public const byte AdditionalErrorCode = 7;

        /// <summary>
        ///     <para>
        ///         This code is used to indicate that an additional HRESULT
        ///         accompanies this StatusCode.  The additional HRESULT is contained
        ///         in the HResult member of an ErrorInfo object that is located in the
        ///         ErrorInfo list contained in the DataValueArrays in which this StatusCode
        ///         is present.
        ///     </para>
        ///     <para>
        ///         This code does not have to be present if the Context was opened
        ///         with ContextOptions set to DebugErrorMessages using either the
        ///         Initiate() or ReInitiate() method.
        ///     </para>
        ///     This value is in Xi Status Code bit position.
        /// </summary>
        public const uint AdditionalErrorCodeBits = 0x00070000;

        #endregion
    }
}