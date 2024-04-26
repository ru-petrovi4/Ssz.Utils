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
	public partial class XiOPCWrapperServer : ServerBase<ContextImpl, ListRoot>
	{
		/// <summary>
		/// The server implementation override used to support the 
		/// IResourceManagement.Initiate() method.
		/// </summary>
		/// <param name="applicationName"></param>
		/// <param name="workstationName"></param>
		/// <param name="localeId"></param>		
		/// <returns>An instance of a context to be used by this client.</returns>
		protected override ContextImpl OnInitiate(string applicationName, string workstationName, ref uint localeId)
		{
			ContextImpl contextImpl = new ContextImpl(this, Guid.NewGuid().ToString(), applicationName, workstationName, ref localeId);			
			
			localeId = contextImpl.LocaleId;			

			//EndpointDefinition restRead = listEndpointDefinitions.Find(
			//	ep => ep.BindingName == "WebHttpBinding" && ep.ContractType == typeof(IRestRead).Name);
			//if (restRead != null)
			//	restRead.Url = string.Format("{0}/changes/{1}/{2}", restRead.Url, contextImpl.Id, typeof(IRestRead).Name);
			
			contextImpl.OpcCreateInstance(ref localeId, _ThisServerEntry.ServerDescription);
			
			return contextImpl;
		}

	}
}
