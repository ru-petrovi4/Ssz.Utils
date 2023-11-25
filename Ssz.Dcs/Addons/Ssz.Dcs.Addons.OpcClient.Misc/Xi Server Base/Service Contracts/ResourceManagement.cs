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
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Text;
//TODO: Enable the three using statements below to support Impersonation of the connected Xi user to the wrapped OPC COM servers
//      Also note that the DefineList method in this file provides an example of how to implement impersonation
//using System.Security.Principal;
//using System.Security.Permissions;
//using System.Runtime.InteropServices;
using Ssz.Utils;
using Ssz.Utils.Net4;
using Xi.Common.Support;
using Xi.Contracts;
using Xi.Contracts.Constants;
using Xi.Contracts.Data;

namespace Xi.Server.Base
{
	/// <summary>
	/// This partial class implements the IResourceManagement interface
	/// </summary>
	public abstract partial class ServerBase<TContext, TList> 
		: ServerRoot
		, IResourceManagement
		where TContext : ContextBase<TList>
		where TList : ListRoot
	{
		#region Context Management

		/// <summary>
		/// The publicly accessible property indicates that the server is initializing. 
		/// </summary>
		public static bool ServerInitializing;

		/// <summary>
		/// <para>This method is used to establish a context between 
		/// the client and the server.  The server must authenticate 
		/// the client when this method is called.</para> 
		/// <para>Once created, the context is capable of multiplexing 
		/// concurrent WCF connections to Xi endpoints.  In some  
		/// cases, there may be more than one instance of the Read 
		/// or Write endpoints (see the OpenEndpoint() method 
		/// for more information about endpoints. </para>  
		/// <para>If the WCF connection to the Resource Management 
		/// Endpoint is inadvertanatly disconnected, the client can 
		/// prevent the context from timing-out and automatically 
		/// closing by calling the ReInitiateContext() method.</para>
		/// </summary>
		/// <param name="applicationName">
		/// The name of the client application.  
		/// </param>
		/// <param name="workstationName">
		/// The name of the workstation on which the client application 
		/// is running.  
		/// </param>
		/// <param name="localeId">
		/// The localeId to be used for the context. If the requested context 
		/// is not supported by the server, the server will return its default 
		/// context. 
		/// </param>
		/// <param name="contextTimeout">
		/// The context timeout is expressed in milliseconds.  The requested 
		/// timeout value can be negotiated up or down by the server. The 
		/// negotiated value is returned to the client.  A request value of 
		/// zero causes the server to use its default timeout.
		/// </param>
		/// <param name="contextOptions">
		/// This parameter enables various debug and tracing options used to 
		/// aide in diagnosing issues. See ContextOptions enum for the 
		/// valid values.
		/// </param>
		/// <param name="reInitiateKey">
		/// A server-specific string that is to be supplied by the client in the 
		/// ReInitiate() method call. This parameter is used to prevent interloping 
		/// clients from re-initiating a context using only the context id that was 
		/// obtained through observing message sent to unencrypted Xi endpoints.  
		/// The reinitiate key value returned to the client by this method is 
		/// server-specific.   
		/// </param>
		/// <returns>
		/// The server generated context id.
		/// </returns>
		string IResourceManagement.Initiate(
			string applicationName, string workstationName, ref uint localeId, ref uint contextTimeout, 
			ref uint contextOptions, out string reInitiateKey)
		{
            using (Logger.EnterMethod(applicationName, workstationName, localeId))
			{
				StringBuilder sb = new StringBuilder("Initiate From Application [");
				sb.Append(applicationName);
				sb.Append("] WorkStation [");
				sb.Append(workstationName);
				sb.Append("].");
				Logger.Info(sb.ToString());

				if (ServerInitializing)
				{
					// clear this flag to make sure server has to have completed initialization on the subsequent Initiate() calls
					ServerInitializing = false;
					ServerState = ServerState.Initializing;
				}
				else if (ServerState == ServerState.Initializing)
				{
					throw FaultHelpers.Create("Server is initializing");
				}
				else if (ServerState != ServerState.Operational)
				{
					throw FaultHelpers.Create("Server is not operational");
				}

			    try
			    {
			        OperationContext ctx = OperationContext.Current;

                    List<EndpointDefinition> listEndpointDefinitions = new List<EndpointDefinition>
					{						
						new EndpointDefinition() { EndpointId = nameof(IRead) },
						new EndpointDefinition() { EndpointId = nameof(IWrite) },
						new EndpointDefinition() { EndpointId = nameof(IPoll) },
						new EndpointDefinition() { EndpointId = nameof(IRegisterForCallback) },
					};                    

			        // check security for this context
			        OnValidateContextSecurity(applicationName, workstationName, ctx);

			        // Get the user identity used by this server
			        System.Security.Principal.IIdentity userIdentity = OnGetPrimaryIdentity(ctx);

			        // negotiate the context options for this context
			        uint negotiatedContextOptions = NegotiateContextOptions(contextOptions);

			        // Create the context for the client
			        uint localeId1 = localeId;
			        TContext tContext = OnInitiate(applicationName, workstationName,
			            ref localeId1, ref contextTimeout, negotiatedContextOptions,
			            ctx, userIdentity, listEndpointDefinitions, out reInitiateKey);

			        Logger.Verbose("Context being created for {0}", tContext.Identity.Name);
			        ContextManager<TContext, TList>.AddContext(tContext);

			        // The StandardMib, VendorMib, and ServerDescription are initialized when the wrapper initiates its first context for a client.
			        // It is reinitialized for each subsequent context of previously inaccessible supported wrappers become accessible.
			        try
			        {
			            InitializeServerData(tContext);
			        }
			        catch (FaultException<XiFault> fe)
			        {
			            _StandardMib = null;
			            _ServerDetails = null;
			            throw fe;
			        }
			        catch (Exception ex)
			        {
			            _StandardMib = null;
			            _ServerDetails = null;
			            throw (ex);
			        }

			        if (_ThisServerEntry.ServerDescription.SupportedLocaleIds != null)
			        {
			            uint requestedLocale = localeId;
			            uint localeId2 = _ThisServerEntry.ServerDescription.SupportedLocaleIds.Find(lid => lid == requestedLocale);
			            if (localeId2 == 0 && 0 < _ThisServerEntry.ServerDescription.SupportedLocaleIds.Count)
			                localeId2 = _ThisServerEntry.ServerDescription.SupportedLocaleIds.First();
			            localeId = (0 != localeId2) ? localeId2 : localeId1;
			        }
			        contextOptions = tContext.NegotiatedContextOptions;

                    if (ServerState == ServerState.Initializing)
                        ServerState = ServerState.Operational;

			        return tContext.Id;
			    }
			    catch (FaultException<XiFault> fe)
			    {
                    ServerInitializing = true;
			        ServerState = ServerState.Operational;
                    throw fe;
			    }
			    catch (Exception ex)
			    {
                    ServerInitializing = true;
                    ServerState = ServerState.Operational;
                    throw FaultHelpers.Create(ex);
			    }
			}
		}

		/// <summary>
		/// This method is called prior to calling OnInitiate to validate security for the context.  
		/// If any problems are found, an XiFault should be thrown to communicate them to the client.
		/// </summary>
		/// <param name="applicationName">The client application name to authorize.</param>
		/// <param name="workstationName">The client workstation name to authorize</param>
		/// <param name="ctx">The Operation Context to authorize</param>
		protected abstract void OnValidateContextSecurity(string applicationName, string workstationName,
			OperationContext ctx);

		/// <summary>
		/// This method is called prior to calling OnInitiate to set the Identity for the context 
		/// appropriately for this server.
		/// </summary>
		/// <param name="ctx">The Operation Context that identifies the calling user.</param>
		/// <returns>The user identity to be used for this context</returns>
		protected abstract System.Security.Principal.IIdentity OnGetPrimaryIdentity(OperationContext ctx);

		/// <summary>
		/// This method is called prior to calling OnInitiate to set the context options for the 
		/// context being opened.  This method is defined for the ServerBase class instead of the 
		/// ContextBase class to ensure that it is called. The ContextBase construction is done by 
		/// the implementation class.
		/// </summary>
		/// <param name="requestedContextOptions">The requested context options</param>
		/// <returns>The context options negotiated for this context</returns>
		public uint NegotiateContextOptions(uint requestedContextOptions)
		{
			// convert all "0s" in the access bits to the appropriate bits
			if (   ((requestedContextOptions & (uint)ContextOptions.EnableDataAccess) == 0)
				&& ((requestedContextOptions & (uint)ContextOptions.EnableAlarmsAndEventsAccess) == 0)
				&& ((requestedContextOptions & (uint)ContextOptions.EnableJournalDataAccess) == 0)
				&& ((requestedContextOptions & (uint)ContextOptions.EnableJournalAlarmsAndEventsAccess) == 0)
			   )
			{
				requestedContextOptions |= (uint)ContextOptions.EnableDataAccess;
				requestedContextOptions |= (uint)ContextOptions.EnableAlarmsAndEventsAccess;
				requestedContextOptions |= (uint)ContextOptions.EnableJournalDataAccess;
				requestedContextOptions |= (uint)ContextOptions.EnableJournalAlarmsAndEventsAccess;
			}
			// call the override for the implementation-specific support for the requested options
			return OnNegotiateContextOptions(requestedContextOptions);
		}

		/// <summary>
		/// This method is to be overridden by the context implementation in the 
		/// Server Implementation project to set the context options.  
		/// <summary>
		/// This method is to be overridden by the context implementation in the 
		/// Server Implementation project.
		/// </summary>
		/// <param name="contextOptions">The requested context options</param>
		/// <returns>The context options negotiated for this context</returns>
		public abstract uint OnNegotiateContextOptions(uint requestedContextOptions);

		/// to validate the passed context information.  If any problems are found, a XiFault 
		/// should be thrown to communicate them to the client.
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
		/// <returns>An instance of a Context Implementation</returns>
		protected abstract TContext OnInitiate(string applicationName, string workstationName,
			ref uint localeId, ref uint contextTimeout, uint contextOptions, OperationContext ctx,
			System.Security.Principal.IIdentity userIdentity, List<EndpointDefinition> listEndpointDefinitions, 
			out string reInitiateKey);

		/// <summary>
		/// <para>This method is used to reinitate an existing context 
		/// after a failure of the underlying WCF connection.  The 
		/// server must reauthenticate the client when this method is 
		/// called.</para> 
		/// </summary>
		/// <param name="existingContext">
		/// The context identifier of the existing context to reinitate. 
		/// </param>
		void IResourceManagement.ReInitiate(string existingContext, ref uint contextOptions,
			ref string reInitiateKey)
		{
            using (Logger.EnterMethod(existingContext))
			{
				Logger.Info("ReInitiate requested.");
				try
				{
					// Don't validate the context here
					// It will be validated in OnReInitiate()
					TContext context = ContextManager<TContext, TList>.LookupContext(existingContext, false);
					if (context == null)
						throw FaultHelpers.Create(XiFaultCodes.E_NOCONTEXT);

					bool allowDifferentClientIpAddress = ((context.NegotiatedContextOptions & (uint)ContextOptions.AllowDifferentClientIpAddress) > 0);
					// negotiate the context options for this context
					uint negotiatedContextOptions = NegotiateContextOptions(contextOptions);

					context.OnReInitiate(allowDifferentClientIpAddress, negotiatedContextOptions, ref reInitiateKey);
				}
				catch (FaultException<XiFault> fe)
				{
					throw fe;
				}
				catch (Exception ex)
				{
					throw FaultHelpers.Create(ex);
				}
			}
		}

		/// <summary>
		/// This method is used to close a context. When the context 
		/// is closed, all resources/endpoints associated with the  
		/// context are released.
		/// </summary>
		/// <param name="contextId">
		/// The context identifier of the context to close. 
		/// </param>
		void IResourceManagement.Conclude(
			string contextId)
		{
            using (Logger.EnterMethod(contextId))
			{
				Logger.Info("Conclude requested.");
				try
				{
					TContext context = ContextManager<TContext, TList>.CloseContext(contextId);
					if (context != null)
					{
						context.OnConclude();
					}
				}
				catch (FaultException<XiFault> fe)
				{
					throw fe;
				}
				catch (Exception ex)
				{
					throw FaultHelpers.Create(ex);
				}
			}
		}

		/// <summary>
		/// This method should be invoked by a client that is not actively 
		/// invoking other methods that use a Context LocalId to keep the 
		/// client – server context from timing out.
		/// </summary>
		/// <param name="contextId"></param>
		void IResourceManagement.ClientKeepAlive(
			string contextId)
		{
            using (Logger.EnterMethod(contextId, DateTime.Now))
			{
				try
				{
					TContext context = ContextManager<TContext, TList>.LookupContext(contextId);
					if (context != null)
					{
						context.OnClientKeepAlive();
						return;
					}
				}
				catch (FaultException<XiFault> fe)
				{
					throw fe;
				}
				catch (Exception ex)
				{
					throw FaultHelpers.Create(ex);
				}
			}
		}

		#endregion

		#region Discovery Methods

		/// <summary>
		/// <para>This method is used to get the description of the 
		/// server.  This method can be called before a context has 
		/// been established with the server.</para>
		/// </summary>
		/// <param name="contextId">
		/// The optional context identifier. This call can be issued 
		/// without first having established a client context.  
		/// However, the ServerDetails element of the ServerDescription 
		/// is not returned unless this parameter is present.
		/// </param>
		/// <returns>
		/// The description of the server. 
		/// </returns>
		ServerDescription IResourceManagement.Identify(
			string contextId)
		{
            using (Logger.EnterMethod())
			{
				ServerDescription serverDescription        = new ServerDescription();
				serverDescription.ServerDiscoveryUrl       = _ThisServerEntry.ServerDescription.ServerDiscoveryUrl;
				serverDescription.XiContractsVersionNumber = _ThisServerEntry.ServerDescription.XiContractsVersionNumber;
				serverDescription.SecurityTokenServiceUrl  = _ThisServerEntry.ServerDescription.SecurityTokenServiceUrl;
				serverDescription.HostName                 = _ThisServerEntry.ServerDescription.HostName;
				serverDescription.ServerName               = _ThisServerEntry.ServerDescription.ServerName;
				serverDescription.SystemName               = _ThisServerEntry.ServerDescription.SystemName;
				serverDescription.ServerTypes              = _ThisServerEntry.ServerDescription.ServerTypes;
				serverDescription.SupportedLocaleIds       = _ThisServerEntry.ServerDescription.SupportedLocaleIds;
				serverDescription.VendorName               = _ThisServerEntry.ServerDescription.VendorName;
				serverDescription.UserInfo                 = _ThisServerEntry.ServerDescription.UserInfo;

				// if a context was supplied, set the server details, otherwise, don't return them
				try
				{
					TContext context = ContextManager<TContext, TList>.LookupContext(contextId);
					if (context != null)
						serverDescription.ServerDetails = _ServerDetails;
				}
		        catch (Exception e)
		        {
		            Logger.Verbose(e);
					serverDescription.ServerDetails = null;
				}
				return serverDescription;
			}
		}

		/// <summary>
		/// This method is used to get the state of the server, and 
		/// the state of any wrapped servers.
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <returns>
		/// The status of the server. 
		/// </returns>
		List<ServerStatus> IResourceManagement.Status(
			string contextId)
		{
            using (Logger.EnterMethod(contextId))
			{
				try
				{
					TContext context = ContextManager<TContext, TList>.LookupContext(contextId);
					if (context == null)
						throw FaultHelpers.Create(XiFaultCodes.E_NOCONTEXT);

					// return immediately if the sever is initializing or shutting down.
					if (   ((_ServerState & ServerState.Initializing) != 0)
						|| ((_ServerState & ServerState.Faulted) != 0)
						|| ((_ServerState & ServerState.Aborting) != 0))
					{
						List<ServerStatus> serverStatusList = new List<ServerStatus>();
						ServerStatus serverStatus = new ServerStatus();
						serverStatus.ServerType = _ThisServerEntry.ServerDescription.ServerTypes; 
						serverStatus.CurrentTime = DateTime.UtcNow;
						serverStatus.ServerState = _ServerState;
						serverStatusList.Add(serverStatus);
						return serverStatusList;
					}
					else
						return context.OnStatus();
				}
				catch (FaultException<XiFault> fe)
				{
					throw fe;
				}
				catch (Exception ex)
				{
					throw FaultHelpers.Create(ex);
				}
			}
		}

		/// <summary>
		/// <para>This method returns text descriptions of error codes.</para>  
		/// </summary>
		/// <param name="contextId">
		/// The context identifier. 
		/// </param>
		/// <param name="resultCodes">
		/// The result codes for which text descriptions are being requested.
		/// </param>
		/// <returns>
		/// The list of result codes and if a result code indicates success, 
		/// the requested text descriptions. The size and order of this 
		/// list matches the size and order of the resultCodes parameter.
		/// </returns>
		List<RequestedString> IResourceManagement.LookupResultCodes(
			string contextId, List<uint> resultCodes)
		{
            using (Logger.EnterMethod(contextId))
			{
				try
				{
					TContext context = ContextManager<TContext, TList>.LookupContext(contextId);
					if (context == null)
						throw FaultHelpers.Create(XiFaultCodes.E_NOCONTEXT);

					return context.OnLookupResultCodes(resultCodes);
				}
				catch (FaultException<XiFault> fe)
				{
					throw fe;
				}
				catch (Exception ex)
				{
					throw FaultHelpers.Create(ex);
				}
			}
		}

		/// <summary>
		/// <para>This method is used to find objects in the server.  The 
		/// client uses the findCriteria parameter to identify a starting 
		/// branch and a set of filter criteria.  It also specifies the 
		/// maximum number of objects to return.  </para> 
		/// <para>The server examines the objects that are children of the 
		/// specified branch and selects those that match the filter criteria.
		/// Note that "children" are objects whose root paths can be created 
		/// by appending their names to the path used to identify the starting  
		/// branch.</para>  
		/// <para>The object attributes of the selected objects are 
		/// returned to the client. The number returned is limited by the 
		/// number specified in the numberToReturn parameter.  If the number 
		/// returned is less than than that number, then the client can 
		/// safely assume that the server has no more to return.</para>  
		/// <para>However, if the number returned is equal to that number, 
		/// then the client can retrieve the next set of results by issuing 
		/// another FindObjects() call with the findCriteria parameter set to 
		/// null. A null findCriteria indicates to the server to continue 
		/// returning results from those remaining in the list.  The client 
		/// eventually detects the end of the list by receiving a response 
		/// that returns less than specified by the numberToReturn parameter.</para>
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="findCriteria">
		/// The criteria used by the server to find objects.  If this 
		/// parameter is null, then this call is a continuation of the 
		/// previous find.
		/// </param>
		/// <param name="numberToReturn">
		/// The maximum number of objects  to return in a single response.
		/// </param>
		/// <returns>
		/// <para>The list of object attributes for the objects that met 
		/// the filter criteria. </para>  
		/// <para>Returns an empty list if the starting object is a leaf, or 
		/// no objects were found that meet the filter criteria, or if the call 
		/// was made with a null findCriteria and there are no more objects to 
		/// return.</para>
		/// <para>May also return null if there is nothing (left) to return.</para>
		/// </returns>
		List<ObjectAttributes> IResourceManagement.FindObjects(
			string contextId, FindCriteria findCriteria, uint numberToReturn)
		{
            using (Logger.EnterMethod(contextId))
			{
				try
				{
					TContext context = ContextManager<TContext, TList>.LookupContext(contextId);
					if (context == null)
						throw FaultHelpers.Create(XiFaultCodes.E_NOCONTEXT);

					return context.OnFindObjects(findCriteria, numberToReturn);
				}
				catch (FaultException<XiFault> fe)
				{
					throw fe;
				}
				catch (Exception ex)
				{
					throw FaultHelpers.Create(ex);
				}
			}
		}

		/// <summary>
		/// <para>Each discoverable object in the server has at least 
		/// one path to the root (called the root path).  A root path 
		/// is represented by an ordered list of object names beginning 
		/// with "Root" and ending with the name of the object.</para>
		/// <para>This method identifies an object by one of its root 
		/// paths and requests the server to return any additional root 
		/// paths that exist.  If there are no other root paths that can 
		/// be used to reach the object, then null is returned.</para>
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="objectPath">
		/// The root path that identifies the object for which alternate 
		/// root paths are being requested. 
		/// </param>
		/// <returns>
		/// The list of additional root paths to the specified object.  
		/// Null if specified objectPath is the only root path to the 
		/// object. An exception is thrown if the specified objectPath is 
		/// invalid.  
		/// </returns>
		List<ObjectPath> IResourceManagement.FindRootPaths(
			string contextId, ObjectPath objectPath)
		{
            using (Logger.EnterMethod(contextId))
			{
				try
				{
					TContext context = ContextManager<TContext, TList>.LookupContext(contextId);
					if (context == null)
						throw FaultHelpers.Create(XiFaultCodes.E_NOCONTEXT);

					return context.OnFindRootPaths(objectPath);
				}
				catch (FaultException<XiFault> fe)
				{
					throw fe;
				}
				catch (Exception ex)
				{
					throw FaultHelpers.Create(ex);
				}
			}
		}

		/// <summary>
		/// <para>This method is used to get the description of a data 
		/// type or an object type. This description is intended to be 
		/// used by the client to understand the semantics and composition 
		/// of the data type or object type.  It cannot be used for 
		/// standard data types.  </para>
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="findCriteria">
		/// The criteria used by the server to find types.  If this 
		/// parameter is null, then this call is a continuation of the 
		/// previous find.
		/// </param>
		/// <param name="numberToReturn">
		/// The maximum number of objects to return in a single response.
		/// </param>
		/// <returns>
		/// <para>The list of type attributes for the type that met 
		/// the filter criteria. </para>  
		/// <para>Returns null if the starting type is a leaf, or no types 
		/// were found that meet the filter criteria, or if the call was made 
		/// with a null findCriteria and there are no more types to return.</para>
		/// </returns>
		List<TypeAttributes> IResourceManagement.FindTypes(
			string contextId, FindCriteria findCriteria, uint numberToReturn)
		{
            using (Logger.EnterMethod(contextId))
			{
				try
				{
					TContext context = ContextManager<TContext, TList>.LookupContext(contextId);
					if (context == null)
						throw FaultHelpers.Create(XiFaultCodes.E_NOCONTEXT);

					return context.OnFindTypes(findCriteria, numberToReturn);
				}
				catch (FaultException<XiFault> fe)
				{
					throw fe;
				}
				catch (Exception ex)
				{
					throw FaultHelpers.Create(ex);
				}
			}
		}

		/// <summary>
		/// <para>This method is used to read the standard MIB.</para>  
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <returns>
		/// The standard MIB is returned.
		/// </returns>
		StandardMib IResourceManagement.GetStandardMib(
			string contextId)
		{
			using (Logger.EnterMethod(contextId))
			{
				try
				{
					TContext context = ContextManager<TContext, TList>.LookupContext(contextId);
					if (context == null)
						throw FaultHelpers.Create(XiFaultCodes.E_NOCONTEXT);

					return context.OnGetStandardMib();
				}
				catch (FaultException<XiFault> fe)
				{
					throw fe;
				}
				catch (Exception ex)
				{
					throw FaultHelpers.Create(ex);
				}
			}
		}

		/// <summary>
		/// <para>This method is used to read the vendor-specific MIB objects.</para>  
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="vendorMibInstanceIds">
		/// The list of identifiers for the vendor MIB objects to be returned.
		/// </param>
		/// <returns>
		/// The requested vendor-specific MIB objects.
		/// </returns>
		DataValueArrays IResourceManagement.GetVendorMib(
			string contextId, List<InstanceId> vendorMibInstanceId)
		{
            using (Logger.EnterMethod(contextId))
			{
				try
				{
					TContext context = ContextManager<TContext, TList>.LookupContext(contextId);
					if (context == null)
						throw FaultHelpers.Create(XiFaultCodes.E_NOCONTEXT);

					return context.OnGetVendorMib(vendorMibInstanceId);
				}
				catch (FaultException<XiFault> fe)
				{
					throw fe;
				}
				catch (Exception ex)
				{
					throw FaultHelpers.Create(ex);
				}
			}
		}

		#endregion

		#region Endpoint Management

		/// <summary>
		/// <para>This method opens an endpoint that can be used to 
		/// access one or more lists.  Each newly opened endpoint is 
		/// assigned its own unique identifier.  </para>
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="contractType">
		/// <para>The type of the endpoint as specified by the interface that it 
		/// supports.  IResourceManagement and IServerDiscovery cannot be created.</para>
		/// <para>The values are defined using the typeof(IXXX).Name property, where IXXX is 
		/// the contract name (e.g. IRead).  This value is also used as the value of the 
		/// following property: </para>
		/// <para> System.ServiceModel.Description.ServiceEndpoint.Contract.Name  </para>
		/// </param>
		/// <param name="endpointUrl">
		/// The URL of the endpoint as defined by System.ServiceModel.Description.ServiceEndpoint.Address.Uri.OriginalString.
		/// </param>
		/// <returns>
		/// The definition of the endpoint.
		/// </returns>
		EndpointDefinition IResourceManagement.OpenEndpoint(
			string contextId, string contractType, string endpointUrl)
		{
			using (Logger.EnterMethod(contextId, contractType, endpointUrl))
			{
				try
				{
					TContext context = ContextManager<TContext, TList>.LookupContext(contextId);
					if (context == null)
						throw FaultHelpers.Create(XiFaultCodes.E_NOCONTEXT);
					return context.OnOpenEndpoint(contractType, endpointUrl);
				}
				catch (FaultException<XiFault> fe)
				{
					throw fe;
				}
				catch (Exception ex)
				{
					throw FaultHelpers.Create(ex);
				}
			}
		}

		/// <summary>
		/// This method adds a list to an endpoint.
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="endpointId">
		/// A string value that uniquely identifies the endpoint 
		/// to which the list is to be added.
		/// </param>
		/// <param name="serverListId">
		/// The identifier of the list to add to the endpoint.
		/// </param>
		/// <returns>
		/// The list identifier and result code for the list whose 
		/// add failed. Returns null if the add succeeded.  Throws 
		/// a fault if the specified context could not be found.
		/// </returns>
		AliasResult IResourceManagement.AddListToEndpoint(
			string contextId, string endpointId, uint serverListId)
		{
            using (Logger.EnterMethod(contextId, endpointId))
			{
				try
				{
					TContext context = ContextManager<TContext, TList>.LookupContext(contextId);
					if (context == null)
						throw FaultHelpers.Create(XiFaultCodes.E_NOCONTEXT);
					return context.OnAddListToEndpoint(endpointId, serverListId);
				}
				catch (FaultException<XiFault> fe)
				{
					throw fe;
				}
				catch (Exception ex)
				{
					throw FaultHelpers.Create(ex);
				}
			}
		}

		/// <summary>
		/// This method removes one or more lists from an endpoint.
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="endpointId">
		/// A string value that uniquely identifies the endpoint 
		/// from which the list is to be removed.
		/// </param>
		/// <param name="listIds">
		/// The identifiers of the lists to remove from the endpoint.
		/// </param>
		/// <returns>
		/// The list identifiers and result codes for the lists whose 
		/// removal failed. Returns null if all removals succeeded.  
		/// </returns>
		List<AliasResult> IResourceManagement.RemoveListsFromEndpoint(
			string contextId, string endpointId, List<uint> listIds)
		{
            using (Logger.EnterMethod(contextId, endpointId))
			{
				try
				{
					TContext context = ContextManager<TContext, TList>.LookupContext(contextId);
					if (context == null)
						throw FaultHelpers.Create(XiFaultCodes.E_NOCONTEXT);
					return context.OnRemoveListsFromEndpoint(endpointId, listIds);
				}
				catch (FaultException<XiFault> fe)
				{
					throw fe;
				}
				catch (Exception ex)
				{
					throw FaultHelpers.Create(ex);
				}
			}
		}

		/// <summary>
		/// This method closes an endpoint.
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="endpointId">
		/// The endpoint LocalId of the endpoint to be deleted.
		/// </param>
		void IResourceManagement.CloseEndpoint(
			string contextId, string endpointId)
		{
            using (Logger.EnterMethod(contextId, endpointId))
			{
				try
				{
					TContext context = ContextManager<TContext, TList>.LookupContext(contextId);
					if (context == null)
						throw FaultHelpers.Create(XiFaultCodes.E_NOCONTEXT);

					context.OnCloseEndpoint(endpointId);
					return;
				}
				catch (FaultException<XiFault> fe)
				{
					throw fe;
				}
				catch (Exception ex)
				{
					throw FaultHelpers.Create(ex);
				}
			}
		}

		#endregion

		#region List Management
		/// <summary>
		/// <para>This method is used to create a list of data 
		/// objects or alarms/events within the context.  </para>
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="clientId">
		/// The Client LocalId for this list.  Used in callbacks to allow the 
		/// client to identify this list.
		/// </param>
		/// <param name="listType">
		/// Indicates the type of list to be created.
		/// Standard list types as defined by the Xi.Contacts.Constants.StandardListType 
		/// enumeration are: 
		/// 1) Data List, 
		/// 2) History Data List, 
		/// 3) Event List 
		/// 4) History Event List
		/// </param>
		/// <param name="updateRate">
		/// The requested update rate in milliseconds for the list. The  
		/// update rate indicates how often the server updates the 
		/// values of elements in the list.  A value of 0 indicates 
		/// that updating is exception-based. The server may negotiate 
		/// this value, up or down as necessary to support its efficient 
		/// operation.
		/// </param>
		/// <param name="bufferingRate">
		/// <para>An optional-use parameter that indicates that the server is 
		/// to buffer data updates, rather than overwriting them, until either 
		/// the time span defined by the buffering rate expires or the values 
		/// are transmitted to the client in a callback or poll response. If 
		/// the time span expires, then the oldest value for a data object is 
		/// discarded when a new value is received from the underlying system.</para>
		/// <para>The value of the bufferingRate is set to 0 to indicate 
		/// that it is not to be used and that new values overwrite (replace) existing 
		/// cached values.  </para>
		/// <para>When used, this parameter contains the client-requested buffering 
		/// rate, which the server may negotiate up or down, or to 0 if the 
		/// server does not support the buffering rate. </para>
		/// <para>The FeaturesSupported member of the StandardMib is used to indicate 
		/// server support for the buffering rate.</para>
		/// </param>
		/// <param name="filterSet">
		/// The FilterSet used to select elements for the list.  
		/// </param>
		/// <returns>
		/// The attributes created for the list.
		/// </returns>
		//TODO: Enable the attribute below to have the XiUser impersonated in the call to the OPC COM server 
		[OperationBehavior(Impersonation = ImpersonationOption.Allowed)]
		ListAttributes IResourceManagement.DefineList(
			string contextId, uint clientId, uint listType,
			uint updateRate, uint bufferingRate, FilterSet filterSet)
		{
            using (Logger.EnterMethod(contextId, clientId))
			{
				try
				{
					//TODO: Enable the lines below to use impersonation between the Xi server and the OPC COM server
					//WindowsIdentity callerWindowsIdentity = ServiceSecurityContext.Current.WindowsIdentity;
					//if (callerWindowsIdentity == null)
					//{
					//    throw new FaultException("The caller cannot be mapped to a Windows identity.");
					//}

					//using (WindowsImpersonationContext wic = callerWindowsIdentity.Impersonate())
					{
						TContext context = ContextManager<TContext, TList>.LookupContext(contextId);
						if (context == null)
						{
							throw FaultHelpers.Create(XiFaultCodes.E_NOCONTEXT);
						}

						return context.OnDefineList(clientId, listType, updateRate, bufferingRate, filterSet);
					}
				}
				catch (FaultException<XiFault> fe)
				{
					throw fe;
				}
				catch (Exception ex)
				{
					throw FaultHelpers.Create(ex);
				}
			}
		}

		/// <summary>
		/// <para>This method gets the attributes of a list.</para>
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="listIds">
		/// The identifiers for the lists whose attributes are to be 
		/// retrieved.
		/// </param>
		/// <returns>
		/// The list of requested List Attributes. The size and order 
		/// of this list matches the size and order of the listAliases 
		/// parameter.  
		/// </returns>
		List<ListAttributes> IResourceManagement.GetListAttributes(
			string contextId, List<uint> listIds)
		{
            using (Logger.EnterMethod(contextId))
			{
				try
				{
					TContext context = ContextManager<TContext, TList>.LookupContext(contextId);
					if (context == null)
						throw FaultHelpers.Create(XiFaultCodes.E_NOCONTEXT);

					return context.OnGetListAttributes(listIds);
				}
				catch (FaultException<XiFault> fe)
				{
					throw fe;
				}
				catch (Exception ex)
				{
					throw FaultHelpers.Create(ex);
				}
			}
		}

		/// <summary>
		/// This method is used to renew the aliases for a list.  Successful completion 
		/// of this method invalidates the previous server alias, but not the previous 
		/// client alias. However, the server begins using the new client alias at its 
		/// earliest opportunity and ceases using the previous client alias. This behavior 
		/// accommodates the race condition that may occur when this method is being processed 
		/// by the server concurrently with the publishing of data to the client.
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="listId">
		/// The identifier for the list whose aliases are to be 
		/// updated.
		/// </param>
		/// <param name="newAliases">
		/// The list of aliases to be updated. Each AliasUpdate in the list 
		/// contains the existing server alias and new client alias for it.
		/// </param>
		/// <returns>
		/// The list of updated aliases. The size and order of this list matches 
		/// the size and order of the listAliases parameter.  Each AliasResult 
		/// in the list contains the new client alias from the request and its 
		/// corresponding new server alias assigned by the server.
		/// </returns>
		List<AliasResult> IResourceManagement.RenewAliases(string contextId, uint listId, List<AliasUpdate> newAliases)
		{
            using (Logger.EnterMethod(contextId, listId))
			{
				try
				{
					TContext context = ContextManager<TContext, TList>.LookupContext(contextId);
					if (context == null)
						throw FaultHelpers.Create(XiFaultCodes.E_NOCONTEXT);

					return context.OnRenewAliases(listId, newAliases);
				}
				catch (FaultException<XiFault> fe)
				{
					throw fe;
				}
				catch (Exception ex)
				{
					throw FaultHelpers.Create(ex);
				}
			}
		}

		/// <summary>
		/// <para>This method deletes one or more lists for the specified context.  </para>
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="listIds">
		/// The identifiers for the lists to be deleted.  If this parameter is null,
		/// then all lists for the context is to be deleted.
		/// </param>
		/// <returns>
		/// The list identifiers and result codes for the lists whose 
		/// deletion failed. Returns null if all deletes succeeded.  
		/// </returns>
		List<AliasResult> IResourceManagement.DeleteLists(string contextId, List<uint> listIds)
		{
            using (Logger.EnterMethod(contextId))
			{
				try
				{
					TContext context = ContextManager<TContext, TList>.LookupContext(contextId);
					if (context == null)
						throw FaultHelpers.Create(XiFaultCodes.E_NOCONTEXT);

					List<TList> removedLists = null;
					List<AliasResult> listAliasResult = context.RemoveListsFromContext(listIds, out removedLists);
					context.DisposeLists(removedLists);
					return listAliasResult;
				}
				catch (FaultException<XiFault> fe)
				{
					throw fe;
				}
				catch (Exception ex)
				{
					throw FaultHelpers.Create(ex);
				}
			}
		}

		/// <summary>
		/// <para>This method is used to add objects to a list.  Objects 
		/// are added with updating of their values by the server 
		/// disabled. Updating of values by the server can be enabled 
		/// using the EnableListUpdating() method.</para>
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="listId">
		/// The identifier for the list to which data objects are to be 
		/// added.
		/// </param>
		/// <param name="dataObjectsToAdd">
		/// The data objects to add.
		/// </param>
		/// <returns>
		/// The list of results. The size and order of this list matches 
		/// the size and order of the objectsToAdd parameter.  
		/// </returns>
		List<AddDataObjectResult> IResourceManagement.AddDataObjectsToList(
			string contextId, uint listId, List<ListInstanceId> dataObjectsToAdd)
		{
            using (Logger.EnterMethod(contextId, listId))
			{
				try
				{
					TContext context = ContextManager<TContext, TList>.LookupContext(contextId);
					if (context == null)
						throw FaultHelpers.Create(XiFaultCodes.E_NOCONTEXT);

					return context.OnAddDataObjectsToList(listId, dataObjectsToAdd);
				}
                catch (FaultException<XiFault> fe)
                {
                    throw fe;
                }
                catch (Exception ex)
                {
                    throw FaultHelpers.Create(ex);
                }
			}
		}

		/// <summary>
		/// <para>This method is used to remove members from a list.  
		/// It does not, however, delete the corresponding data object 
		/// from the server.</para>
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="listId">
		/// The identifier for the list from which data objects are 
		/// to be removed.
		/// </param>
		/// <param name="serverAliasesToDelete">
		/// <para>The server aliases of the data objects to remove. When this value 
		/// is null all elements of the list are to be removed.</para> 
		/// <para>If the value of a serverAlias in this list is zero, then its 
		/// zero-based index into the serverAliases list is used as the ClientAlias 
		/// in a returned AliasResult. </para>
		/// </param>
		/// <returns>
		/// <para>Returns null if all removals succeeded. If not, returns the 
		/// client and server aliases and result codes for the data objects that could 
		/// not be removed. Data objects that were successfully removed are not included 
		/// in this list. See XiFaultCodes claass for standardized result codes.</para> 
		/// <para>If the value of a serverAlias in the serverAliases list is zero, 
		/// then its zero-based index into the serverAliases list is used as the 
		/// ClientAlias in a returned AliasResult to allow the client to locate the 
		/// entry in the submitted list of serverAliases..</para>
		/// </returns>
		List<AliasResult> IResourceManagement.RemoveDataObjectsFromList(
			string contextId, uint listId, List<uint> serverAliasesToDelete)
		{
            using (Logger.EnterMethod(contextId, listId))
			{
				try
				{
					TContext context = ContextManager<TContext, TList>.LookupContext(contextId);
					if (context == null)
						throw FaultHelpers.Create(XiFaultCodes.E_NOCONTEXT);

					List<AliasResult> results = context.OnRemoveDataObjectsFromList(listId, serverAliasesToDelete);
					// return null if there are no results
					return ((results == null) || (results.Count == 0)) ? null : results;
				}
				catch (FaultException<XiFault> fe)
				{
					throw fe;
				}
				catch (Exception ex)
				{
					throw FaultHelpers.Create(ex);
				}
			}
		}

		/// <summary>
		/// This method is used to change the filters of a list.  The 
		/// new filters replace the old filters if they exist.
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="listId">
		/// The identifier for the list to which the filters are to 
		/// be set.
		/// </param>
		/// <param name="updateRate">
		/// List update or scan rate.  The server will negotiate this rate to one 
		/// that it can support.  GetListAttributes can be used to obtain the current 
		/// value of this parameter.  Null if the update rate is not to be updated.  
		/// </param>
		/// <param name="bufferingRate">
		/// List buffering rate.  The server will negotiate this rate to one 
		/// that it can support.  GetListAttributes can be used to obtain the current 
		/// value of this parameter.  Null if the buffering rate is not to be updated.
		/// </param>
		/// <param name="filterSet">
		/// The new set of filters.  The server will negotiate these filters to those 
		/// that it can support.  GetListAttributes can be used to obtain the current 
		/// value of this parameter.  Null if the filters are not to be updated.
		/// </param>
		/// <returns>
		/// The revised update rate, buffering rate, and filter set.  Attributes 
		/// that were not updated will be null in this response.
		/// </returns>
		ModifyListAttrsResult IResourceManagement.ModifyListAttributes(
			string contextId, uint listId, Nullable<uint> updateRate, Nullable<uint> bufferingRate, FilterSet filterSet)
		{
            using (Logger.EnterMethod(contextId, listId))
			{
				try
				{
					TContext context = ContextManager<TContext, TList>.LookupContext(contextId);
					if (context == null)
						throw FaultHelpers.Create(XiFaultCodes.E_NOCONTEXT);

					return context.OnModifyListAttributes(listId, updateRate, bufferingRate, filterSet);
				}
				catch (FaultException<XiFault> fe)
				{
					throw fe;
				}
				catch (Exception ex)
				{
					throw FaultHelpers.Create(ex);
				}
			}
		}

		/// <summary>
		/// This method is used to enable or disable updating of an entire 
		/// list. When this method is called, the enabled state of the list is changed, 
		/// but the enabled state of the individual elements of the list is unchanged. 
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="listId">
		/// The identifier for the list for which updating is to be 
		/// enabled or disabled.
		///</param>
		/// <param name="enable">
		/// Indicates, when TRUE, that updating of the list is to be enabled,
		/// and when FALSE, that updating of the list is to be disabled.
		/// </param>
		ListAttributes IResourceManagement.EnableListUpdating(string contextId, uint listId, bool enableUpdating)
		{
            using (Logger.EnterMethod(contextId, listId))
			{
				try
				{
					TContext context = ContextManager<TContext, TList>.LookupContext(contextId);
					if (context == null)
						throw FaultHelpers.Create(XiFaultCodes.E_NOCONTEXT);

					return context.OnEnableListUpdating(listId, enableUpdating);
				}
				catch (FaultException<XiFault> fe)
				{
					throw fe;
				}
				catch (Exception ex)
				{
					throw FaultHelpers.Create(ex);
				}
			}
		}

		/// <summary>
		/// This method is used to enable or disable updating of individual 
		/// elements of a list.  If the server aliases parameter is null, then 
		/// all elements of the list are enabled/disabled.  This call does not 
		/// change the enabled state of the list itself.
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="listId">
		/// The identifier for the list for which updating is to be 
		/// enabled or disabled.
		///</param>
		/// <param name="enableUpdating">
		/// Indicates, when TRUE, that updating of the list is to be enabled,
		/// and when FALSE, that updating of the list is to be disabled.
		/// </param>
		/// <param name="serverAliases">
		/// <para>The list of aliases for data objects of a list for which updating 
		/// is to be enabled or disabled.  When this value is null updating all 
		/// elements of the list are to be enabled/disabled. In this case, however, 
		/// the enable/disable state of the list itself is not changed.</para>
		/// <para>If the value of a serverAlias in this list is zero, then its 
		/// zero-based index into the serverAliases list is used as the ClientAlias 
		/// in a returned AliasResult. </para>
		/// </param>
		/// <returns>
		/// <para>Returns null if the server was able to successfully enable/disable 
		/// the the specified elements for the specified list.  If not, returns the 
		/// client and server aliases and result codes for the data objects that could 
		/// not be enabled/disabled. Data objects that were successfully enabled/disabled 
		/// are not included in this list. See XiFaultCodes claass for standardized 
		/// result codes. </para> 
		/// <para>If the value of a serverAlias in the serverAliases list is zero, 
		/// then its zero-based index into the serverAliases list is used as the 
		/// ClientAlias in a returned AliasResult to allow the client to locate the 
		/// entry in the submitted list of serverAliases.</para>
		/// <para>Throws an exception if the specified context or list could not be found.</para> 
		/// </returns>
		List<AliasResult> IResourceManagement.EnableListElementUpdating(
			string contextId, uint listId, bool enableUpdating, List<uint> serverAliases)
		{
            using (Logger.EnterMethod(contextId, listId))
			{
				try
				{
					TContext context = ContextManager<TContext, TList>.LookupContext(contextId);
					if (context == null)
						throw FaultHelpers.Create(XiFaultCodes.E_NOCONTEXT);

					return context.OnEnableListElementUpdating(listId, enableUpdating, serverAliases);
				}
				catch (FaultException<XiFault> fe)
				{
					throw fe;
				}
				catch (Exception ex)
				{
					throw FaultHelpers.Create(ex);
				}
			}
		}

		/// <summary>
		/// This method is used to request that category-specific fields be 
		/// included in event messages generated for alarms and events of 
		/// the category for the specified Event/Alarm List.
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		///</param>
		/// <param name="listId">
		/// The identifier for the list for which event message fields are being added. 
		///</param>
		/// <param name="categoryId">
		/// The category for which event message fields are being added.
		/// </param>
		/// <param name="fieldObjectTypeIds">
		/// The list of category-specific fields to be included in the event 
		/// messages generated for alarms and events of the category.  Each field 
		/// is identified by its ObjectType LocalId obtained from the EventMessageFields 
		/// contained in the EventCategoryConfigurations Standard MIB element.
		/// </param>
		/// <returns>
		/// The ObjectTypeIds and result codes for the fields that could not be  
		/// added to the event message. Returns null if all succeeded.  
		/// </returns>
		List<TypeIdResult> IResourceManagement.AddEventMessageFields(string contextId, uint listId, uint categoryId, List<TypeId> fieldObjectTypeIds)
		{
            using (Logger.EnterMethod(contextId))
			{
				try
				{
					TContext context = ContextManager<TContext, TList>.LookupContext(contextId);
					if (context == null)
						throw FaultHelpers.Create(XiFaultCodes.E_NOCONTEXT);

					return context.OnAddEventMessageFields(listId, categoryId, fieldObjectTypeIds);
				}
				catch (FaultException<XiFault> fe)
				{
					throw fe;
				}
				catch (Exception ex)
				{
					throw FaultHelpers.Create(ex);
				}
			}
		}

		/// <summary>
		/// <para>This method is used to cause one or more data objects of 
		/// a list to be "touched".  Data objects that are in the disabled 
		/// state (see the EnableListElementUpdating() method) are not 
		/// affected by this method.  This method cannot be used with 
		/// event lists.</para>
		/// <para>Touching an enabled data object causes the server to update 
		/// the data object, mark it as changed (even if their values did not change), 
		/// and then return it to the client in the next callback or poll.</para> 
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="listId">
		/// The server identifier for the lists whose data objects are to be touched.
		/// </param>
		/// <param name="serverAliases">
		/// <para>The aliases for the data objects to touch. If null, all data objects 
		/// are to be touched.</para>
		/// <para>If the value of a serverAlias in this list is zero, then its 
		/// zero-based index into the serverAliases list is used as the ClientAlias 
		/// in a returned AliasResult. </para>
		/// </param>
		/// <returns>
		/// <para>Returns null if the server was able to successfully enable/disable 
		/// the the specified elements for the specified list.  If not, returns the 
		/// client and server aliases and result codes for the data objects that could 
		/// not be touched. Data objects that were successfully touched are not included 
		/// in this list. See XiFaultCodes claass for standardized result codes. </para>
		/// <para>If the value of a serverAlias in the serverAliases list is zero, 
		/// then its zero-based index into the serverAliases list is used as the 
		/// ClientAlias in a returned AliasResult to allow the client to locate the 
		/// entry in the submitted list of serverAliases.</para>
		/// <para>Throws an exception if the specified context or list could not be found.</para> 
		/// </returns>
		List<AliasResult> IResourceManagement.TouchDataObjects(string contextId, uint listId, List<uint> serverAliases)
		{
            using (Logger.EnterMethod(contextId, listId))
			{
				try
				{
					TContext context = ContextManager<TContext, TList>.LookupContext(contextId, false);
					if (context == null)
						throw FaultHelpers.Create(XiFaultCodes.E_NOCONTEXT);

					List<AliasResult> listAliasResult = null;
					listAliasResult = context.OnTouchDataObjects(listId, serverAliases);
					return listAliasResult;
				}
				catch (FaultException<XiFault> fe)
				{
					throw fe;
				}
				catch (Exception ex)
				{
					throw FaultHelpers.Create(ex);
				}
			}
		}

		/// <summary>
		/// This method is used to touch (mark as changed) all itmes that
		/// are active within the list.
		/// </summary>
		/// <param name="contextId"></param>
		/// <param name="listId"></param>
		/// <returns></returns>
		uint IResourceManagement.TouchList(string contextId, uint listId)
		{
            using (Logger.EnterMethod(contextId, listId))
			{
				try
				{
					TContext context = ContextManager<TContext, TList>.LookupContext(contextId, false);
					if (context == null)
						throw FaultHelpers.Create(XiFaultCodes.E_NOCONTEXT);

					uint rtnValue = context.OnTouchList(listId);
					return rtnValue;
				}
				catch (FaultException<XiFault> fe)
				{
					throw fe;
				}
				catch (Exception ex)
				{
					throw FaultHelpers.Create(ex);
				}
			}
		}

		#endregion

		#region Alarms and Events

		/// <summary>
		/// <para>This method is used to request summary information for the 
		/// alarms that can be generated for a given event source.</para>
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="eventSource">
		/// The InstanceId for the event source for which alarm summaries are 
		/// being requested.
		/// </param>
		/// <returns>
		/// The summaries of the alarms that can be generated by the specified 
		/// event source.  
		/// </returns>
		List<AlarmSummary> IResourceManagement.GetAlarmSummary(
			string contextId, InstanceId eventSourceId)
		{
            using (Logger.EnterMethod(contextId))
			{
				try
				{
					TContext context = ContextManager<TContext, TList>.LookupContext(contextId);
					if (context == null)
						throw FaultHelpers.Create(XiFaultCodes.E_NOCONTEXT);

					return context.OnGetAlarmSummary(eventSourceId);
				}
				catch (FaultException<XiFault> fe)
				{
					throw fe;
				}
				catch (Exception ex)
				{
					throw FaultHelpers.Create(ex);
				}
			}
		}

		/// <summary>
		/// This method is used to enable or disable alarms for a specified area or event source.
		/// It is independent of the XiLists on which the alarms may be reported.
		/// It throws a fault if the requested operation cannot be performed successfully.
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="enableFlag">
		/// This flag indicates, when TRUE, that alarms are to be enabled, and when FALSE, that they 
		/// are to be disabled.
		/// </param>
		/// <param name="areaFlag">
		/// This flag indicates, when TRUE, that the eventContainerIds parameter contains a list of 
		/// InstanceIds for areas, and when FALSE, that it contains a list of InstanceIds for event sources.
		/// </param>
		/// <param name="eventContainerId">
		/// The InstanceId for the area or the event source for which alarms are to be enabled or disabled.
		/// </param>
		/// <returns>Null if all requested enable/disable operations succeeded. Otherwise, the list of result codes. The size and 
		/// order of this list matches that of the eventContainerIds.  Standard result code values are defined by 
		/// the Xi.Contracts.Constants.XiFaultCodes class. There is one result code for each eventContainerId.</returns>
		List<UInt32> IResourceManagement.EnableAlarms(string contextId, bool enableFlag, bool areaFlag, List<InstanceId> eventContainerIds)
		{
            using (Logger.EnterMethod(contextId))
			{
				try
				{
					TContext context = ContextManager<TContext, TList>.LookupContext(contextId);
					if (context == null)
						throw FaultHelpers.Create(XiFaultCodes.E_NOCONTEXT);

					return context.OnEnableAlarms(enableFlag, areaFlag, eventContainerIds);
				}
				catch (FaultException<XiFault> fe)
				{
					throw fe;
				}
				catch (Exception ex)
				{
					throw FaultHelpers.Create(ex);
				}
			}
		}

		/// <summary>
		/// This method returns the enable state for a specified area or event source.
		/// It throws a fault if the requested operation cannot be performed successfully.
		/// </summary>
		/// <param name="contextId">The context identifier.</param>
		/// <param name="areaFlag">
		/// This flag indicates, when TRUE, that the eventContainerIds parameter contains a list of 
		/// InstanceIds for areas, and when FALSE, that it contains a list of InstanceIds for event sources.</param>
		/// <param name="eventContainerId">
		/// The InstanceId for the area or the event source for which alarms are to be enabled or disabled.
		/// </param>
		/// <returns>An object with the enabled state and result code for each requested InstanceId.
		/// </returns>
		List<AlarmEnabledState> IResourceManagement.GetAlarmsEnabledState(string contextId, bool areaFlag, List<InstanceId> eventContainerIds)
		{
            using (Logger.EnterMethod(contextId))
			{
				try
				{
					TContext context = ContextManager<TContext, TList>.LookupContext(contextId);
					if (context == null)
						throw FaultHelpers.Create(XiFaultCodes.E_NOCONTEXT);

					return context.OnGetAlarmsEnabledState(areaFlag, eventContainerIds);
				}
				catch (FaultException<XiFault> fe)
				{
					throw fe;
				}
				catch (Exception ex)
				{
					throw FaultHelpers.Create(ex);
				}
			}
		}

		#endregion
	}
}
