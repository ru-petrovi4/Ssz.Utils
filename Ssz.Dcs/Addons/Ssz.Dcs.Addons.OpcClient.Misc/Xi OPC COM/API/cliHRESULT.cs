/**********************************************************************
 * Copyright © 2009, 2010, 2011, 2012 OPC Foundation, Inc. 
 *
 * The source code and all binaries built with the OPC .NET 3.0 source
 * code are subject to the terms of the Express Interface Public
 * License (Xi-PL).  See http://www.opcfoundation.org/License/Xi-PL/
 *
 * The source code may be distributed from an OPC member company in
 * its original or modified form to its customers and to any others who
 * have software that needs to interoperate with the OPC member's OPC
* .NET 3.0 products. No other redistribution is permitted.
 *
 * You must not remove this notice, or any other, from this software.
 *
 *********************************************************************/

namespace Xi.OPC.COM.API
{
	public static class cliHR
	{
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
//
// Define the facility codes
//
		public const int OPC_Facility = 0x4;

//
// Define the severity codes
//
		public const int STATUS_SEVERITY_WARNING       = 0x2;
		public const int STATUS_SEVERITY_SUCCESS       = 0x0;
		public const int STATUS_SEVERITY_INFORMATIONAL = 0x1;
		public const int STATUS_SEVERITY_ERROR         = 0x3;
		public const int STATUS_SEVERITY_COM_ERROR     = 0x2;

		//
		// Define the severity codes mask
		//
		// AND this with the HRESULT to get the severity bits 
		public const int HR_SEVERITY_MASK          = unchecked((int)0xC0000000);
 
		public const int HR_SEVERITY_SUCCESS       = unchecked((int)0x00000000);
		public const int HR_SEVERITY_WARNING_MASK  = unchecked((int)0x80000000);
		public const int HR_SEVERITY_INFORMATIONAL = unchecked((int)0x40000000);
		public const int HR_SEVERITY_ERROR         = unchecked((int)0xC0000000);


//
// MessageId: OPC_E_INVALIDHANDLE
//
// MessageText:
//
// The value of the handle is invalid.
//
		public const int OPC_E_INVALIDHANDLE = unchecked((int)0xC0040001);

//
// MessageId: OPC_E_BADTYPE
//
// MessageText:
//
// The server cannot convert the data between the 
// requested data type and the canonical data type.
//
		public const int OPC_E_BADTYPE = unchecked((int)0xC0040004);

//
// MessageId: OPC_E_PUBLIC
//
// MessageText:
//
// The requested operation cannot be done on a public group.
//
		public const int OPC_E_PUBLIC = unchecked((int)0xC0040005);

//
// MessageId: OPC_E_BADRIGHTS
//
// MessageText:
//
// The Items AccessRights do not allow the operation.
//
		public const int OPC_E_BADRIGHTS = unchecked((int)0xC0040006);

//
// MessageId: OPC_E_UNKNOWNITEMID
//
// MessageText:
//
// The item definition does not exist within the servers address space.
//
		public const int OPC_E_UNKNOWNITEMID = unchecked((int)0xC0040007);

//
// MessageId: OPC_E_INVALIDITEMID
//
// MessageText:
//
// The item definition doesn't conform to the server's syntax.
//
		public const int OPC_E_INVALIDITEMID = unchecked((int)0xC0040008);

//
// MessageId: OPC_E_INVALIDFILTER
//
// MessageText:
//
// The filter string was not valid.
//
		public const int OPC_E_INVALIDFILTER = unchecked((int)0xC0040009);

//
// MessageId: OPC_E_UNKNOWNPATH
//
// MessageText:
//
// The item's access path is not known to the server.
//
		public const int OPC_E_UNKNOWNPATH = unchecked((int)0xC004000A);

//
// MessageId: OPC_E_RANGE
//
// MessageText:
//
// The value was out of range.
//
		public const int OPC_E_RANGE = unchecked((int)0xC004000B);

//
// MessageId: OPC_E_DUPLICATE_NAME
//
// MessageText:
//
// Duplicate name not allowed.
//
		public const int OPC_E_DUPLICATE_NAME = unchecked((int)0xC004000C);

//
// MessageId: OPC_S_UNSUPPORTEDRATE
//
// MessageText:
//
// The server does not support the requested data rate 
// but will use the closest available rate.
//
		public const int OPC_S_UNSUPPORTEDRATE = unchecked((int)0x0004000D);

//
// MessageId: OPC_S_CLAMP
//
// MessageText:
//
// A value passed to WRITE was accepted but was clamped.
//
		public const int OPC_S_CLAMP = unchecked((int)0x0004000E);

//
// MessageId: OPC_S_INUSE
//
// MessageText:
//
// The operation cannot be completed because the 
// object still has references that exist.
//
		public const int OPC_S_INUSE = unchecked((int)0x00040200);

//
// MessageId: OPC_E_INVALIDCONFIGFILE
//
// MessageText:
//
// The server's configuration file is an invalid format.
//
		public const int OPC_E_INVALIDCONFIGFILE = unchecked((int)0xC0040201);

//
// MessageId: OPC_E_NOTFOUND
//
// MessageText:
//
// The server could not locate the requested object.
//
		public const int OPC_E_NOTFOUND = unchecked((int)0xC0040202);

//
// MessageId: OPC_E_INVALID_PID
//
// MessageText:
//
// The server does not recognise the passed property ID.
//
		public const int OPC_E_INVALID_PID = unchecked((int)0xC0040203);

// ***************************************************************************
// ***************************************************************************
//
// End of Foundation Defined codes
//
// ***************************************************************************

		public const int S_OK = 0x00000000;
		public const int S_FALSE = 0x00000001;
		public const int E_FAIL = unchecked((int)0x80004005);
	}

	/// <summary>
	/// The cliHRESULT is used to represnt a COM HRESULT or a Win32 Error.
	/// </summary>
	public struct cliHRESULT
	{
		public cliHRESULT(cliHRESULT hr)
		{
			HResult = hr.HResult;
		}
		public cliHRESULT(int hr)
		{
			HResult = hr;
		}
		private int HResult;
		public int hResult { get { return HResult; } set { HResult = value; } }
		public bool Succeeded { get { return 0 <= hResult; } }
		public bool Failed { get { return 0 > hResult; } }
		public bool IsS_OK { get { return cliHR.S_OK == hResult; } }
		public bool IsS_FALSE { get { return cliHR.S_FALSE == hResult; } }

		public static implicit operator int(cliHRESULT hr)
		{
			return hr.hResult;
		}
		public static explicit operator uint(cliHRESULT hr)
		{
			return (uint)hr.hResult;
		}

		public static implicit operator cliHRESULT(int hr)
		{
			cliHRESULT HR = new cliHRESULT();
			HR.hResult = hr;
			return HR;
		}
		public static implicit operator cliHRESULT(uint hr)
		{
			cliHRESULT HR = new cliHRESULT();
			HR.hResult = (int)hr;
			return HR;
		}

	}
}
