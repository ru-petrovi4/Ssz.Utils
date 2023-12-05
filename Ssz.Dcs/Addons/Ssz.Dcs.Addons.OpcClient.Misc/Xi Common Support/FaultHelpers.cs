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
using System.ServiceModel;
using Xi.Contracts.Data;

namespace Xi.Common.Support
{
	/// <summary>
	/// Static class used to create FaultException<XiFalultInfo> to support WCF compliant faults.
	/// The FaultException<XiFalultInfo> class is a subclass of the CommunicationException class. 
	/// faults of this class are thrown by the server if they are intended to be communicated back 
	/// to the client.
	/// </summary>
	public static class FaultHelpers
	{
		/// <summary>
		/// This method will return a FaultException with a XiFault
		/// where the ErrorCode will be E_XIMESSAGEFROMEXCEPTION.
		/// The message is from the exception passed into this method.
		/// </summary>
		/// <param name="ex"></param>
		static public Exception Create(Exception ex)
		{			
			return ex;
		}

		/// <summary>
		/// This method will return a FaultException with a XiFault
		/// where the ErrorCode will be E_XIFAULTMESSAGE.
		/// </summary>
		/// <param name="message">Error string</param>
		static public Exception Create(string message)
		{
			return new Exception(message);
		}

		/// <summary>
		/// This throws a new FaultException with the XiFault detail
		/// </summary>
		/// <param name="errorCode">Error string</param>
		static public Exception Create(uint errorCode)
		{
			string text = FaultStrings.Get(errorCode);
			return new Exception(text);
		}

		/// <summary>
		/// This throws a new FaultException with the XiFault detail
		/// </summary>
		/// <param name="errorCode">Error code</param>
		/// <param name="message">Error string</param>
		static public Exception Create(uint errorCode, string message)
		{
			return new Exception(message);
		}

		/// <summary>
		/// This method provides functionality like the SUCCEEDED macro.
		/// </summary>
		/// <param name="errorCode"></param>
		/// <returns></returns>
		static public bool Succeeded(uint errorCode)
		{
			return (0 == (0x80000000u & errorCode));
		}

		/// <summary>
		/// This method provides functionality like the FAILED marco.
		/// </summary>
		/// <param name="errorCode"></param>
		/// <returns></returns>
		static public bool Failed(uint errorCode)
		{
			return !Succeeded(errorCode);
		}
	}
}
