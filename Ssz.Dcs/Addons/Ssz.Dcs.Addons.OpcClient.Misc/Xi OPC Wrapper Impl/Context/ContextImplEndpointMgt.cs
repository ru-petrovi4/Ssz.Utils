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
		/// The server implementation override used to validate the security for the 
		/// IResourceManagement.OpenEndpoint() method.  This method throws an exception 
		/// if the validation fails.
		/// <param name="endpointEntry">
		/// The endpoint to be validated.
		/// </param>
		/// </summary>
		protected override void OnValidateOpenEndpointSecurity(EndpointEntry<ListRoot> endpointEntry)
		{
			// TODO: Add security validation for the endpoint.
		}
	}
}