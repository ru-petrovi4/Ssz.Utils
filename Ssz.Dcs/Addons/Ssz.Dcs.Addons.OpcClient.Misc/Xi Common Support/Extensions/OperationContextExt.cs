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

using System.Linq;
using System.ServiceModel.Channels;
using System.ServiceModel;

namespace Xi.Common.Support.Extensions
{
	/// <summary>
	/// This class provides some extension helpers for pulling data out of the 
	/// WCF operation context.  Note that these are only usable in the call context of
	/// an active WCF operation.
	/// </summary>
	static public class OperationContextExt
	{
		/// <summary>
		/// This returns the current user (if any) on the operation context.
		/// </summary>
		/// <param name="ctx">Operation Context</param>
		/// <returns>Username</returns>
		static public string GetCurrentUser(this OperationContext ctx)
		{
			if (ctx != null && ctx.ServiceSecurityContext != null)
			{
				return (ctx.ServiceSecurityContext.PrimaryIdentity.Name);
			}

			return "Anonymous";
		}

		/// <summary>
		/// This returns the remote client's IP address and port when they are using a TCP/IP based channel
		/// </summary>
		/// <param name="ctx">Operation Context</param>
		/// <param name="ipAddress">Returning IP address</param>
		/// <param name="port">Returnign port</param>
		/// <returns>True/False success code</returns>
		static public bool GetRemoteAddress(this OperationContext ctx, out string ipAddress, out int port)
		{
			var clientEndpoint =
				(ctx.IncomingMessageProperties.FirstOrDefault(p => p.Key == RemoteEndpointMessageProperty.Name)).Value
				as RemoteEndpointMessageProperty;

			if (clientEndpoint != null)
			{
				ipAddress = clientEndpoint.Address;
				port = clientEndpoint.Port;
				return true;
			}

			ipAddress = string.Empty;
			port = 0;

			return false;
		}

		/// <summary>
		/// This returns the binding which was used to create the current operation context.
		/// </summary>
		/// <param name="ctx">Operation Context</param>
		/// <returns>Binding name</returns>
		static public string GetBinding(this OperationContext ctx)
		{
			return (from ep in ctx.Host.Description.Endpoints
					where ctx.Channel.LocalAddress.Uri.AbsoluteUri == ep.Address.Uri.AbsoluteUri
					select ep.Binding.Name).FirstOrDefault();
		}
	}
}