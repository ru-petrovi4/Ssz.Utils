namespace Ssz.DataGrpc.Common
{
    /// <summary>
    ///     <para>
    ///         This class defines standard Xi success and fault codes.
    ///         Xi servers can report error to the client as:
    ///     </para>
    ///     <para>a)    Exceptions, if the handling of a request completely fails</para>
    ///     <para>b)    Result codes. These are uint values that hold any HResult code.</para>
    ///     <para>c)    Status codes. These are used only in methods that return data values. </para>
    ///     <para>    The uint status code merges OPC quality and a subset of HResult codes. </para>
    ///     <para>    Additional error info can be passed in the associated ErrorInfo object.</para>
    ///     <para>
    ///         NOTE: Entries in this file should have a corresponding entry in either ErrorCodes.xml
    ///         or ErrorCodesOpc.xml except Win32/COM error code values that are defined here with the
    ///         exact values from WinError.h or other Microsoft defined error codes.
    ///     </para>
    /// </summary>
    public static class DataGrpcFaultCodes
    {
        #region public functions

        /// <summary>
        ///     This code indicates success.
        /// </summary>
        public const uint S_OK = 0x00000000;

        /// <summary>
        ///     This code is used to indicate success with additional failure information;
        /// </summary>
        public const uint S_FALSE = 0x00000001;

        /// <summary>
        ///     <para>Win32 Error Code</para>
        ///     This is the standard error code for not implemented.
        ///     It is used as both a result code and in exceptions
        ///     when a function is not implemented.
        /// </summary>
        public const uint E_NOTIMPL = 0x80004001;

        /// <summary>
        ///     <para>Win32 Error Code</para>
        ///     This code indicates a general failure.  The error text
        ///     associated with this error code may be more
        ///     specific.
        /// </summary>
        public const uint E_FAIL = 0x80004005;

        /// <summary>
        ///     This code indicates failure.
        /// </summary>
        public const uint E_NOCONTEXT = 0x87770001;

        /// <summary>
        ///     This code indicates that an invalid list id was used.
        /// </summary>
        public const uint E_BADLISTID = 0x87770002;

        /// <summary>
        ///     This code indicates that an invalid endpoint id was used.
        /// </summary>
        public const uint E_BADENDPOINTID = 0x87770003;

        /// <summary>
        ///     No match was found for the server alias supplied.
        ///     Alias value (client alias) is returned as the client alias.
        ///     No translation to server alias is possible.
        /// </summary>
        public const uint E_ALIASNOTFOUND = 0x87770004;

        /// <summary>
        ///     See the Error Info structure for information.
        /// </summary>
        public const uint E_SEEERRORINFO = 0x87770005;

        /// <summary>
        ///     This code indicates that the requested object was not found.
        /// </summary>
        public const uint E_NOTFOUND = 0x87770006;

        /// <summary>
        ///     This code indicates that the requested operation could not be completed
        ///     because the list was in the disabled state.
        /// </summary>
        public const uint E_LISTDISABLED = 0x87770007;

        /// <summary>
        ///     This code indicates that the request contained a bad parameter value.
        /// </summary>
        public const uint E_BADARGUMENT = 0X87770008;

        /// <summary>
        ///     This code indicates that parameter of a method identifies an object whose
        ///     type is inconsistent with the method. For example, EnableListElementUpdating()
        ///     returns E_INCONSISTENTUSEAGE for a serverAlias if that serverAlias does not
        ///     identify a list element whose type is not DataListValue.
        /// </summary>
        public const uint E_INCONSISTENTUSEAGE = 0x87770009;

        /// <summary>
        ///     The requested action is invalid.
        /// </summary>
        public const uint E_INVALIDREQUEST = 0x8777000A;

        /// <summary>
        ///     The requested operation failed due to an Endpoint related error condition.
        /// </summary>
        public const uint E_ENDPOINTERROR = 0x8777000B;

        /// <summary>
        ///     The Transport Data Type is not valid or is inconsistent.
        /// </summary>
        public const uint E_INCONSISTENT_TRANSPORTDATATYPE = 0x8777000C;

        /// <summary>
        ///     The server has shutdown.
        /// </summary>
        public const uint E_SERVER_SHUTDOWN = 0x8777000D;

        /// <summary>
        ///     The wrapped server is not accessible.
        /// </summary>
        public const uint E_WRAPPEDSERVER_NOT_ACCESSIBLE = 0x8777000E;

        /// <summary>
        ///     An exception occured in a COM server method
        /// </summary>
        public const uint E_WRAPPEDSERVER_EXCEPTION = 0x8777000F;

        /// <summary>
        ///     This code indicates that the requested operation could not be completed
        ///     because the list was not attached to the appropriate endpoint.
        /// </summary>
        public const uint E_LISTNOTATTACHEDTOENDPOINT = 0x87770010;

        /// <summary>
        ///     This code indicates that the requested operation could not be completed
        ///     because the list element was in the disabled state.
        /// </summary>
        public const uint E_LISTELEMENTDISABLED = 0x87770011;

        /// <summary>
        ///     This Error code is used when an exception was caught and the message
        ///     from that exception is being returned.
        ///     This Error Code value is duplicated from Xi.Contract.Data.XiFault
        /// </summary>
        public const uint E_INVALIDVALUE_BADSTATUS = 0x87770012;

        /// <summary>
        ///     This code indicates that an invalid list id was used.
        /// </summary>
        public const uint E_LISTDELETED = 0x87770013;

        ///// <summary>
        /////     This Error Code is used when an internal Xi Server fault has occurred.
        /////     This Error Code value is duplicated from Xi.Contract.Data.XiFault
        ///// </summary>
        //public const uint E_XIMESSAGEFROMTEXT = XiFault.E_XIMESSAGEFROMTEXT; // 0x877780FE;

        ///// <summary>
        /////     This Error code is used when an exception was caught and the message
        /////     from that exception is being returned.
        /////     This Error Code value is duplicated from Xi.Contract.Data.XiFault
        ///// </summary>
        //public const uint E_XIMESSAGEFROMEXCEPTION = XiFault.E_XIMESSAGEFROMEXCEPTION; // 0x877780FF;

        // ################################################################################
        // ################################################################################
        // OPC Error messages as defined by OPC COM specifications
        //
        //  Values are 32 bit values laid out as follows:
        //
        //   3 3 2 2 2 2 2 2 2 2 2 2 1 1 1 1 1 1 1 1 1 1
        //   1 0 9 8 7 6 5 4 3 2 1 0 9 8 7 6 5 4 3 2 1 0 9 8 7 6 5 4 3 2 1 0
        //  +---+-+-+-----------------------+-------------------------------+
        //  |Sev|C|R|     Facility          |               Code            |
        //  +---+-+-+-----------------------+-------------------------------+
        //
        //  where
        //
        //      Sev - is the severity code
        //
        //          00 - Success
        //          01 - Informational
        //          10 - Warning
        //          11 - Error
        //
        //      C - is the Customer code flag
        //
        //      R - is a reserved bit
        //
        //      Facility - is the facility code
        //
        //      Code - is the facility's status code
        //

        /// <summary>
        ///     MessageId: OPC_E_INVALIDHANDLE
        ///     MessageText:
        ///     An invalid handle was passed.
        /// </summary>
        public const uint OPC_E_INVALIDHANDLE = 0xC0040001;

        /// <summary>
        ///     MessageId: OPC_E_BADTYPE
        ///     MessageText:
        ///     The server cannot convert between the passed or requested data type and the canonical type.
        /// </summary>
        public const uint OPC_E_BADTYPE = 0xC0040004;

        /// <summary>
        ///     MessageId: OPC_E_PUBLIC
        ///     MessageText:
        ///     The requested operation cannot be done on a public group.
        /// </summary>
        public const uint OPC_E_PUBLIC = 0xC0040005;

        /// <summary>
        ///     MessageId: OPC_E_BADRIGHTS
        ///     MessageText:
        ///     The item's AccessRights do not allow the operation.
        /// </summary>
        public const uint OPC_E_BADRIGHTS = 0xC0040006;

        /// <summary>
        ///     MessageId: OPC_E_UNKNOWNITEMID
        ///     MessageText:
        ///     The item definition does not exist within the servers address space.
        /// </summary>
        public const uint OPC_E_UNKNOWNITEMID = 0xC0040007;

        /// <summary>
        ///     MessageId: OPC_E_INVALIDITEMID
        ///     MessageText:
        ///     The item definition does not conform to the server's syntax.
        /// </summary>
        public const uint OPC_E_INVALIDITEMID = 0xC0040008;

        /// <summary>
        ///     MessageId: OPC_E_INVALIDFILTER
        ///     MessageText:
        ///     The filter string is not valid.
        /// </summary>
        public const uint OPC_E_INVALIDFILTER = 0xC0040009;

        /// <summary>
        ///     MessageId: OPC_E_UNKNOWNPATH
        ///     MessageText:
        ///     The item's access path is not known to the server.
        /// </summary>
        public const uint OPC_E_UNKNOWNPATH = 0xC004000A;

        /// <summary>
        ///     MessageId: OPC_E_RANGE
        ///     MessageText:
        ///     The value passed to WRITE was out of range.
        /// </summary>
        public const uint OPC_E_RANGE = 0xC004000B;

        /// <summary>
        ///     MessageId: OPC_E_DUPLICATE_NAME
        ///     MessageText:
        ///     A group with a duplicate name already exists in the server.
        /// </summary>
        public const uint OPC_E_DUPLICATE_NAME = 0xC004000C;

        /// <summary>
        ///     MessageId: OPC_S_UNSUPPORTEDRATE
        ///     MessageText:
        ///     The server does not support the requested rate, but will use the closest available.
        /// </summary>
        public const uint OPC_S_UNSUPPORTEDRATE = 0x0004000D;

        /// <summary>
        ///     MessageId: OPC_S_CLAMP
        ///     MessageText:
        ///     A value passed to WRITE was accepted, but was clamped.
        /// </summary>
        public const uint OPC_S_CLAMP = 0x0004000E;

        /// <summary>
        ///     MessageId: OPC_S_INUSE
        ///     MessageText:
        ///     The operation  cannot be completed because the object still has references that exist.
        /// </summary>
        public const uint OPC_S_INUSE = 0x0004000F;

        /// <summary>
        ///     MessageId: OPC_E_INVALIDCONFIGFILE
        ///     MessageText:
        ///     The server's configuration file is an invalid format.
        /// </summary>
        public const uint OPC_E_INVALIDCONFIGFILE = 0xC0040010;

        /// <summary>
        ///     MessageId: OPC_E_NOTFOUND
        ///     MessageText:
        ///     The server could not locate the requested object.
        /// </summary>
        public const uint OPC_E_NOTFOUND = 0xC0040011;

        /// <summary>
        ///     MessageId: OPC_E_INVALID_PID
        ///     MessageText:
        ///     The server does not recognise the passed property ID.
        /// </summary>
        public const uint OPC_E_INVALID_PID = 0xC0040203;

        /// <summary>
        ///     MessageId: OPC_E_MAXEXCEEDED
        ///     MessageText:
        ///     The maximum number of values requested exceeds the server's limit.
        /// </summary>
        public const uint OPC_E_MAXEXCEEDED = 0xC0041001;

        /// <summary>
        ///     MessageId: OPC_S_NODATA
        ///     MessageText:
        ///     There is no data within the specified parameters.
        /// </summary>
        public const uint OPC_S_NODATA = 0x40041002;

        /// <summary>
        ///     MessageId: OPC_S_MOREDATA
        ///     MessageText:
        ///     There is more data satisfying the query than was returned.
        /// </summary>
        public const uint OPC_S_MOREDATA = 0x40041003;

        /// <summary>
        ///     MessageId: OPC_E_INVALIDAGGREGATE
        ///     MessageText:
        ///     The aggregate requested is not valid.
        /// </summary>
        public const uint OPC_E_INVALIDAGGREGATE = 0xC0041004;

        /// <summary>
        ///     MessageId: OPC_S_CURRENTVALUE
        ///     MessageText:
        ///     The server only returns current values for the requested item attributes.
        /// </summary>
        public const uint OPC_S_CURRENTVALUE = 0x40041005;

        /// <summary>
        ///     MessageId: OPC_S_EXTRADATA
        ///     MessageText:
        ///     Additional data satisfying the query was found.
        /// </summary>
        public const uint OPC_S_EXTRADATA = 0x40041006;

        /// <summary>
        ///     MessageId: OPC_W_NOFILTER
        ///     MessageText:
        ///     The server does not support this filter.
        /// </summary>
        public const uint OPC_W_NOFILTER = 0x80041007;

        /// <summary>
        ///     MessageId: OPC_E_UNKNOWNATTRID
        ///     MessageText:
        ///     The server does not support this attribute.
        /// </summary>
        public const uint OPC_E_UNKNOWNATTRID = 0xC0041008;

        /// <summary>
        ///     MessageId: OPC_E_NOT_AVAIL
        ///     MessageText:
        ///     The requested aggregate is not available for the specified item.
        /// </summary>
        public const uint OPC_E_NOT_AVAIL = 0xC0041009;

        /// <summary>
        ///     MessageId: OPC_E_INVALIDDATATYPE
        ///     MessageText:
        ///     The supplied value for the attribute is not a correct data type.
        /// </summary>
        public const uint OPC_E_INVALIDDATATYPE = 0xC004100A;

        /// <summary>
        ///     MessageId: OPC_E_DATAEXISTS
        ///     MessageText:
        ///     Unable to insert - data already present.
        /// </summary>
        public const uint OPC_E_DATAEXISTS = 0xC004100B;

        /// <summary>
        ///     MessageId: OPC_E_INVALIDATTRID
        ///     MessageText:
        ///     The supplied attribute ID is not valid.
        /// </summary>
        public const uint OPC_E_INVALIDATTRID = 0xC004100C;

        /// <summary>
        ///     MessageId: OPC_E_NODATAEXISTS
        ///     MessageText:
        ///     The server has no value for the specified time and item ID.
        /// </summary>
        public const uint OPC_E_NODATAEXISTS = 0xC004100D;

        /// <summary>
        ///     MessageId: OPC_S_INSERTED
        ///     MessageText:
        ///     The requested insert occurred.
        /// </summary>
        public const uint OPC_S_INSERTED = 0x4004100E;

        /// <summary>
        ///     MessageId: OPC_S_REPLACED
        ///     MessageText:
        ///     The requested replace occurred.
        /// </summary>
        public const uint OPC_S_REPLACED = 0x4004100F;

        /// <summary>
        ///     MessageId: OPC_S_ALREADYACKED
        ///     MessageText:
        ///     The condition has already been acknowleged
        /// </summary>
        public const uint OPC_S_ALREADYACKED = 0x00040200;

        /// <summary>
        ///     MessageId: OPC_S_INVALIDBUFFERTIME
        ///     MessageText:
        ///     The buffer time parameter was invalid
        /// </summary>
        public const uint OPC_S_INVALIDBUFFERTIME = 0x00040201;

        /// <summary>
        ///     MessageId: OPC_S_INVALIDMAXSIZE
        ///     MessageText:
        ///     The max size parameter was invalid
        /// </summary>
        public const uint OPC_S_INVALIDMAXSIZE = 0x00040202;

        /// <summary>
        ///     MessageId: OPC_E_INVALIDBRANCHNAME
        ///     MessageText:
        ///     The string was not recognized as an area name
        /// </summary>
        public const uint OPC_E_INVALIDBRANCHNAME = 0xC0040203;

        /// <summary>
        ///     MessageId: OPC_E_INVALIDTIME
        ///     MessageText:
        ///     The time does not match the latest active time
        /// </summary>
        public const uint OPC_E_INVALIDTIME = 0xC0040204;

        /// <summary>
        ///     MessageId: OPC_E_BUSY
        ///     MessageText:
        ///     A refresh is currently in progress
        /// </summary>
        public const uint OPC_E_BUSY = 0xC0040205;

        /// <summary>
        ///     MessageId: OPC_E_NOINFO
        ///     MessageText:
        ///     Information is not available
        /// </summary>
        public const uint OPC_E_NOINFO = 0xC0040206;

        #endregion
    }
}