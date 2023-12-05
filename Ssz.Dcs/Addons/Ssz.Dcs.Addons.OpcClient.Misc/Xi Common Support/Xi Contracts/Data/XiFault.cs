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

using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Xi.Contracts.Data
{
	/// <summary>
	/// This class provides the Detail for the FaultException class used to 
	/// return fault / exception data back to the Xi Client.
	/// The static FaultHelpers class provides static methods which may be
	/// used to create instance of this class returning a FaultException instance.
	/// </summary>
	[DataContract(Namespace = "urn:xi/data")]
	public class XiFault
	{
		/// <summary>
		/// This Error Code is used when an internal Xi Server fault has occurred.
		/// This Error Code value is duplicated in XiFaultCodes.cs.
		/// </summary>
		public const uint E_XIMESSAGEFROMTEXT			= 0x877780FE;

		/// <summary>
		/// This Error code is used when an exception was caught and the message 
		/// from that exception is being returned.
		/// This Error Code value is duplicated in XiFaultCodes.cs.
		/// </summary>
		public const uint E_XIMESSAGEFROMEXCEPTION		= 0x877780FF;

		/// <summary>
		/// A server-specific error code value.  For wrapped COM servers, 
		/// it may contain a COM error code.
		/// </summary>
		[DataMember] public uint ErrorCode { get; set; }

		/// <summary>
		/// Optional text that describes the error. 
		/// </summary>
		[DataMember] public string ErrorText { get; set; }

		/// <summary>
		/// This constructor creates a general failure XiFault that is described by 
		/// a specific error string.
		/// </summary>
		/// <param name="errorText">
		/// The error message associated with the error code.
		/// </param>
		public XiFault(string errorText)
			: this(E_XIMESSAGEFROMTEXT, errorText)
		{
		}

		/// <summary>
		/// This constructor creates a general failure XiFault that is described by 
		/// an inner exception.
		/// </summary>
		/// <param name="exception">
		/// The exception whose message is to be copied into the details error text.
		/// </param>
		public XiFault(Exception exception)
			: this(E_XIMESSAGEFROMEXCEPTION, exception.Message)
		{
		}

		/// <summary>
		/// This constructor creates an XiFault from a specific error code and  
		/// error string.
		/// </summary>
		/// <param name="errorCode">
		/// The error code.
		/// </param>
		/// <param name="message">
		/// The error string.
		/// </param>
		public XiFault(uint errorCode, string message)
		{
			ErrorCode = errorCode;
			ErrorText = message;
			Trace.TraceError(string.Format("Xi Fault: 0x{0:X} {1}", errorCode, message));
		}
	}
}