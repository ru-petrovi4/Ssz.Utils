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
using System.Runtime.Serialization;

namespace Xi.Contracts.Data
{
	/// <summary>
	/// This class is used to return a result code along with 
	/// the negotiated KeepAliveSkipCount and CallbackRate.
	/// </summary>
	[DataContract(Namespace = "urn:xi/data")]
	public class SetCallbackResult
	{
		#region Data Members
		/// <summary>
		/// The Result Code being returned.
		/// </summary>
		[DataMember] public uint Result { get; set; }

		/// <summary>
		/// The server-negotiated KeepAliveSkipCount.
		/// </summary>
		[DataMember] public uint KeepAliveSkipCount { get; set; }

		/// <summary>
		/// The server-negotiated callback rate.
		/// </summary>
		[DataMember] public TimeSpan CallbackRate { get; set; }

		#endregion

		#region Constructors

		/// <summary>
		/// This constructor creates a SetCallbackResult from a result code, 
		/// a keepAliveSkipCount, and a callbackRate.
		/// </summary>
		/// <param name="result">
		/// The result code.
		/// </param>
		/// <param name="keepAliveSkipCount">
		/// The KeepAliveSkipCount.
		/// </param>
		/// <param name="callbackRate">
		/// The callback rate.
		/// </param>
		public SetCallbackResult(uint result, uint keepAliveSkipCount, TimeSpan callbackRate)
		{
			Result = result;
			KeepAliveSkipCount = keepAliveSkipCount;
			CallbackRate = callbackRate;
		}

		#endregion
	}
}