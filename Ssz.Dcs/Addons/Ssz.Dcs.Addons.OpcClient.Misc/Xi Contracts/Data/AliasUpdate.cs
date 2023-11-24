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
	/// This class is used to update the client alias of an object that is 
	/// identified by a server alias.
	/// </summary>
	[DataContract(Namespace = "urn:xi/data")]
	public class AliasUpdate
	{
		#region Data Members
		/// <summary>
		/// The existing server alias (identifier) of the object whose client alias is to be updated.
		/// </summary>
		[DataMember] public uint ExistingServerAlias { get; set; }

		/// <summary>
		/// The new client alias for the object.
		/// </summary>
		[DataMember] public uint NewClientAlias { get; set; }

		#endregion

		#region Constructors

		/// <summary>
		/// This constructor creates an AliasUpdate from 
		/// an existing alias and its new alias.
		/// </summary>
		/// <param name="existingServerAlias">
		/// The existing server alias.
		/// </param>
		/// <param name="newClientAlias">
		/// The new client alias.
		/// </param>
		public AliasUpdate(uint existingServerAlias, uint newClientAlias)
		{
			ExistingServerAlias = existingServerAlias;
			NewClientAlias = newClientAlias;
		}

		#endregion
	}
}