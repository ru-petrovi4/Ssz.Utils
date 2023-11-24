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

using Xi.Server.Base;

namespace Xi.OPC.Wrapper.Impl
{
	/// <summary>
	/// This partial class defines the Context Management methods of the server 
	/// implementation that override the virtual methods defined in the 
	/// Context folder of the ServerBase project.
	/// </summary>
	public partial class ContextImpl : ContextBase<ListRoot>
	{
		/// <summary>
		/// This method implements the server-specific behavior of the corresponding 
		/// Xi interface method.  It overrides its virtual method in the ContextBase 
		/// class of the ServerBase project.
		/// ServerBase.ClientKeepAlive calls LookupContext(), and LookupContext() sets 
		/// LastAccess time for the context. LastAccess is used to time-out contexts
		/// after a period of not receiving requests from the client
		/// </summary>
		public override void OnClientKeepAlive()
		{
			lock (ContextLock)
			{
				return;
			}
		}

	}
}
