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

using System.Runtime.Serialization;

namespace Xi.Contracts.Data
{
	/// <summary>
	/// This class is used to return a result code along with 
	/// a client and server alias if the result code indicates 
	/// success.
	/// </summary>
	[DataContract(Namespace = "urn:xi/data")]
	public class AliasResult
	{
		#region Data Members
		/// <summary>
		/// The Result Code being returned.
		/// </summary>
		[DataMember] public uint Result { get; set; }

		/// <summary>
		/// The client-assigned alias (identifier) for an InstanceId. Set to 0 if unknown.
		/// </summary>
		[DataMember] public uint ClientAlias { get; set; }

		/// <summary>
		/// The server-assigned alias (identifier) for an InstanceId.
		/// </summary>
		[DataMember] public uint ServerAlias { get; set; }

		#endregion

		#region Constructors

		/// <summary>
		/// This constructor creates an AliasResult from a result code, 
		/// a client alias, and a server alias.
		/// </summary>
		/// <param name="result">
		/// The result code.
		/// </param>
		/// <param name="ca">
		/// The client alias.
		/// </param>
		/// <param name="sa">
		/// The server alias.
		/// </param>
		public AliasResult(uint result, uint ca, uint sa)
		{
			ClientAlias = ca;
			ServerAlias = sa;
			Result = result;
		}

		#endregion
	}
}