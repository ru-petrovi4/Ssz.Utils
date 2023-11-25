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
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

using Xi.Common.Support;
using Xi.Contracts;
using Xi.Contracts.Constants;
using Xi.Contracts.Data;
using Xi.Server.Base;

namespace Xi.OPC.Wrapper.Impl
{
	public partial class XiOPCWrapper : ServerBase<ContextImpl, ListRoot>
	{
		/// <summary>
		/// The server implementation override used to validate the security for the 
		/// IResourceManagement.Initiate() method.
		/// </summary>
		/// <param name="applicationName">The client application name to authorize.</param>
		/// <param name="workstationName">The client workstation name to authorize</param>
		/// <param name="ctx">The Operation Context to authorize</param>
		protected override void OnValidateContextSecurity(string applicationName, string workstationName,
			OperationContext ctx)
		{
			if (string.IsNullOrEmpty(applicationName))
			{
				ArgumentNullException ane = new ArgumentNullException("applicationName");
				throw FaultHelpers.Create(ane);
			}
			else
			{
				// TODO:  Add security checks for the client applicationName
				//        throw an exception if the checks fail
			}

			if (string.IsNullOrEmpty(workstationName))
			{
				ArgumentNullException ane = new ArgumentNullException("workstationName");
				throw FaultHelpers.Create(ane);
			}
			else
			{
				// TODO:  Add security checks for the client workstationName
				//        throw an exception if the checks fail
			}

			// TODO:  Add security checks related to the WCF operation context
			//        such as the ctx.Channel.RemoteAddress 
			//        throw an exception if the checks fail
		}

		/// <summary>
		/// The server implementation override used to support the 
		/// IResourceManagement.Initiate() method.
		/// </summary>
		/// <param name="ctx">The Operation Context that identifies the calling user.</param>
		protected override System.Security.Principal.IIdentity OnGetPrimaryIdentity(OperationContext ctx)
		{
			// TODO:  Set the user identity appropriately for this server

			// This implementation defaults to the current logged on user, and overrides that 
			// with the calling user identity.
			System.Security.Principal.IIdentity identity = System.Security.Principal.WindowsIdentity.GetCurrent();
			if ((ctx.ServiceSecurityContext != null) && (ctx.ServiceSecurityContext.PrimaryIdentity != null))
			{
				identity = ctx.ServiceSecurityContext.PrimaryIdentity;
			}
			return identity;
		}

		/// <summary>
		/// This method is to be overridden by the context implementation in the 
		/// Server Implementation project.
		/// </summary>
		/// <param name="requestedContextOptions">The requested context options</param>
		/// <returns>The context options supported for this context.</returns>
		public override uint OnNegotiateContextOptions(uint requestedContextOptions)
		{
			uint negotiatedContextOptions = requestedContextOptions;
			// TODO:  Add code for supported context options
			// Do not add code here to validate requested server types. They will be negotiated in the Implementation subclass.
			return negotiatedContextOptions;
		}

		/// <summary>
		/// The server implementation override used to support the 
		/// IResourceManagement.Initiate() method.
		/// </summary>
		/// <param name="applicationName"></param>
		/// <param name="workstationName"></param>
		/// <param name="localeId"></param>
		/// <param name="contextTimeout"></param>
		/// <param name="contextOptions"></param>
		/// <param name="ctx"></param>
		/// <param name="userIdentity"></param>
		/// <param name="listEndpointDefinitions"></param>
		/// <param name="reInitiateKey"></param>
		/// <returns>An instance of a context to be used by this client.</returns>
		protected override ContextImpl OnInitiate(string applicationName, string workstationName,
			ref uint localeId, ref uint contextTimeout, uint contextOptions, OperationContext ctx,
			System.Security.Principal.IIdentity userIdentity, List<EndpointDefinition> listEndpointDefinitions,
			out string reInitiateKey)
		{
			ContextImpl contextImpl = new ContextImpl(this, ctx.SessionId, applicationName, workstationName, ref localeId,
				ref contextTimeout, contextOptions, userIdentity);
			
			reInitiateKey = contextImpl.ReInitiateKey;
			localeId = contextImpl.LocaleId;
			contextTimeout = (uint)contextImpl.ContextTimeout.TotalMilliseconds;

			//EndpointDefinition restRead = listEndpointDefinitions.Find(
			//	ep => ep.BindingName == "WebHttpBinding" && ep.ContractType == typeof(IRestRead).Name);
			//if (restRead != null)
			//	restRead.Url = string.Format("{0}/changes/{1}/{2}", restRead.Url, contextImpl.Id, typeof(IRestRead).Name);

			contextImpl.AddEndpointsToContext(listEndpointDefinitions);

			// Connect to the requested OPC COM Servers.
			uint requestedServers = contextImpl.NegotiatedContextOptions;
			uint rc = contextImpl.OpcCreateInstance(ref localeId, _ThisServerEntry.ServerDescription);
			if (XiFaultCodes.S_OK != rc)
			{
				string msg = "The OPC .NET Server was unable to connect to the following OPC COM Servers: \n";
				int count = 0;
				foreach (var server in XiOPCWrapper.OpcWrappedServers)
				{
					switch (server.ServerType)
					{
						case ServerType.OPC_DA205_Wrapper:
							if ((requestedServers & (uint)ContextOptions.EnableDataAccess) > 0)
							{
								if (count > 0)
									msg += "\n";
								count++;
								msg += "  ";
								if (string.IsNullOrEmpty(server.HostName) == false)
								{
									msg += server.HostName;
									msg += "/";
								}
								msg += server.ProgId;
							}
							break;
						case ServerType.OPC_HDA12_Wrapper:
							if ((requestedServers & (uint)ContextOptions.EnableJournalDataAccess) > 0)
							{
								if (count > 0)
									msg += "\n";
								count++;
								msg += "  ";
								if (string.IsNullOrEmpty(server.HostName) == false)
								{
									msg += server.HostName;
									msg += "/";
								}
								msg += server.ProgId;
							}
							break;
						case ServerType.OPC_AE11_Wrapper:
							if ((requestedServers & (uint)ContextOptions.EnableAlarmsAndEventsAccess) > 0)
							{
								if (count > 0)
									msg += "\n";
								count++;
								msg += "  ";
								if (string.IsNullOrEmpty(server.HostName) == false)
								{
									msg += server.HostName;
									msg += "/";
								}
								msg += server.ProgId;
							}
							break;
						default:
							break;
					}
				}
				throw FaultHelpers.Create(rc, msg);
			}
			return contextImpl;
		}

	}
}
