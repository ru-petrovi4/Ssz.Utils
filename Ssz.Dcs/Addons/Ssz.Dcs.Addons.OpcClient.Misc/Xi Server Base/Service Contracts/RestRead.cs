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

using Xi.Contracts;
using Xi.Contracts.Data;

namespace Xi.Server.Base
{
	/// <summary>
	/// This partial class implements the IIRestRead interface
	/// </summary>
	public abstract partial class ServerBase<TContext, TList> : ServerRoot
									, IRestRead
									where TContext : ContextBase<TList>
									where TList : ListRoot
	{

		/// <summary>
		/// <para>This method is used to read the values of the 
		/// data objects in a list.</para>
		/// </para> 
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="listId">
		/// The identifier of the list that contains data objects to be read.
		/// </param>
		/// <returns>
		/// The list of requested values. The size and order of this list 
		/// matches the size and order of serverAliases parameter.
		/// </returns>
		DataValueArraysWithAlias IRestRead.RestReadData(string contextId, string listId)
		{
			return ((IRead)this).ReadData(contextId, UInt32.Parse(listId), null);
		}

	}
}
