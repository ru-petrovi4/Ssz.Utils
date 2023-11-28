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
			ref uint localeId, ref uint contextTimeout, uint contextOptions,
			System.Security.Principal.IIdentity userIdentity,
			out string reInitiateKey)
		{
			ContextImpl contextImpl = new ContextImpl(this, Guid.NewGuid().ToString(), applicationName, workstationName, ref localeId,
				ref contextTimeout, contextOptions, userIdentity);
			
			reInitiateKey = contextImpl.ReInitiateKey;
			localeId = contextImpl.LocaleId;
			contextTimeout = (uint)contextImpl.ContextTimeout.TotalMilliseconds;

			//EndpointDefinition restRead = listEndpointDefinitions.Find(
			//	ep => ep.BindingName == "WebHttpBinding" && ep.ContractType == typeof(IRestRead).Name);
			//if (restRead != null)
			//	restRead.Url = string.Format("{0}/changes/{1}/{2}", restRead.Url, contextImpl.Id, typeof(IRestRead).Name);

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
