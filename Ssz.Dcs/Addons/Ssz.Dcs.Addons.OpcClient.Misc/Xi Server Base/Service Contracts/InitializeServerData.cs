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

using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Xi.Contracts;
using Xi.Contracts.Constants;
using Xi.Contracts.Data;

namespace Xi.Server.Base
{
	/// <summary>
	/// This is the base class handler for a XiServer.  It includes some essential
	/// startup and stop functionality for the server.
	/// </summary>
	/// <typeparam name="TContext">Concrete context type</typeparam>
	/// <typeparam name="TList">Concrete List type</typeparam>
	public abstract partial class ServerBase<TContext, TList>
		: ServerRoot
		where TContext : ContextBase<TList>
		where TList : ListRoot
	{
		/// <summary>
		/// The list of all server types to be matched against this 
		/// server's type defined by SupportedServerTypes.
		/// </summary>
		protected static List<uint> _ServerTypeList = new List<uint> 
								{ 
									ServerType.OPC_DA205_Wrapper, 
									ServerType.OPC_AE11_Wrapper,
									ServerType.OPC_HDA12_Wrapper,
									ServerType.OPC_DA30_Wrapper,
									ServerType.OPC_XMLDA_Wrapper,
									ServerType.OPC_UA_DA_Wrapper,
									ServerType.OPC_UA_AC_Wrapper,
									ServerType.OPC_UA_HDA_Wrapper,
									ServerType.Xi_DataServer,
									ServerType.Xi_EventServer,
									ServerType.Xi_DataJournalServer,
									ServerType.Xi_EventJournalServer
								};

		/// <summary>
		/// This property is used to obtain the complete list of valid server types.
		/// </summary>
		public static List<uint> ServerTypeList
		{
			get { return _ServerTypeList; }
			private set { }
		}

		/// <summary>
		/// The details of the server description. This information is not 
		/// returned in the ServerDescription if the Identify() method is 
		/// called without a context id.
		/// </summary>
		protected static ServerDetails _ServerDetails;

		/// <summary>
		/// This flag indicates, when TRUE, that the server has a callback endpoint.
		/// It is set by the implementation subclass.
		/// </summary>
		protected static bool _CallbacksSupported = true;

		/// <summary>
		/// This flag indicates, when TRUE, that the server has a poll endpoint.
		/// It is set by the implementation subclass.
		/// </summary>
		protected static bool _PollingSupported = true;

		/// <summary>
		/// This method initializes the server data.
		/// </summary>
		/// <param name="context">
		/// The context for this method invocation
		/// </param>
		/// <returns>
		/// The result code that indicates the success or failure of this method.
		/// </returns>
		protected void InitializeServerData(TContext context)
		{			
			InitializeServerDescription(context);
			ServerDescription.XiContractsVersionNumber =
				Assembly.GetAssembly(typeof(IResourceManagement)).GetName().Version.ToString();
		}

		/// <summary>
		/// This flag indicates, when TRUE, that the roles, methods, and features have been set in 
		/// the MIB for Data Access. This is set once for all contexts. so that the same information 
		/// doesn't have to be set for each context. If the Xi server wraps a DA server, and that 
		/// DA server shuts down, this flag should be cleared, causing the info to be reloaded 
		/// the next time a context is opened for Data Access.
		/// </summary>
		public static bool DaRolesMethodsAndFeaturesSet = false;

		/// <summary>
		/// This flag indicates, when TRUE, that the roles, methods, and features have been set in 
		/// the MIB for Alarms and Events. This is set once for all contexts. so that the same information 
		/// doesn't have to be set for each context. If the Xi server wraps a DA server, and that 
		/// DA server shuts down, this flag should be cleared, causing the info to be reloaded 
		/// the next time a context is opened for Alarms and Events.
		/// </summary>
		public static bool AeRolesMethodsAndFeaturesSet = false;

		/// <summary>
		/// This flag indicates, when TRUE, that the roles, methods, and features have been set in 
		/// the MIB for Historical Data Access. This is set once for all contexts. so that the same information 
		/// doesn't have to be set for each context. If the Xi server wraps a DA server, and that 
		/// DA server shuts down, this flag should be cleared, causing the info to be reloaded 
		/// the next time a context is opened for Historical Data Access.
		/// </summary>
		public static bool HdaRolesMethodsAndFeaturesSet = false;

		/// <summary>
		/// This flag indicates, when TRUE, that the roles, methods, and features have been set in 
		/// the MIB for Historical Alarms and Events. This is set once for all contexts. so that the same information 
		/// doesn't have to be set for each context. If the Xi server wraps a DA server, and that 
		/// DA server shuts down, this flag should be cleared, causing the info to be reloaded 
		/// the next time a context is opened for Historical Alarms and Events.
		/// </summary>
		public static bool HAeRolesMethodsAndFeaturesSet = false;

		/// <summary>
		/// This flag indicates, when TRUE, that the locale ids have been set in 
		/// the MIB for Data Access. This is set once for all contexts. so that the same locale ids 
		/// doesn't have to be set for each context. If the Xi server wraps a DA server, and that 
		/// DA server shuts down, this flag should be cleared, causing the locale ids to be reloaded 
		/// the next time a context is opened for Data Access.
		/// </summary>
		public static bool DaLocaleIdSet = false;

		/// <summary>
		/// This flag indicates, when TRUE, that the locale ids have been set in 
		/// the MIB for Alarms and Events. This is set once for all contexts. so that the same locale ids 
		/// doesn't have to be set for each context. If the Xi server wraps a DA server, and that 
		/// DA server shuts down, this flag should be cleared, causing the locale ids to be reloaded 
		/// the next time a context is opened for Alarms and Events.
		/// </summary>
		public static bool AeLocaleIdSet = false;

		/// <summary>
		/// This flag indicates, when TRUE, that the locale ids have been set in 
		/// the MIB for Historical Data Access. This is set once for all contexts. so that the same locale ids 
		/// doesn't have to be set for each context. If the Xi server wraps a DA server, and that 
		/// DA server shuts down, this flag should be cleared, causing the locale ids to be reloaded 
		/// the next time a context is opened for Historical Data Access.
		/// </summary>
		public static bool HdaLocaleIdSet = false;

		/// <summary>
		/// This flag indicates, when TRUE, that the locale ids have been set in 
		/// the MIB for Historical Alarms and Events. This is set once for all contexts. so that the same locale ids 
		/// doesn't have to be set for each context. If the Xi server wraps a DA server, and that 
		/// DA server shuts down, this flag should be cleared, causing the locale ids to be reloaded 
		/// the next time a context is opened for Historical Alarms and Events.
		/// </summary>
		public static bool HAeLocaleIdSet = false;

		/// <summary>
		/// This method initializes the Server Description.
		/// </summary>
		/// <param name="context">
		/// The context for this method invocation
		/// </param>
		/// <returns>
		/// The result code that indicates the success or failure of this method.
		/// </returns>
		protected void InitializeServerDescription(TContext context)
		{
			List<uint> localeIds = null;
			// collect all the locale ids into one list for the supported server types
			foreach (var serverType in _ServerTypeList)
			{
				// Server type supported is set here and then below to ensure the server type is accessible
				// This first test below checks to see if it is configured for this server, and the test 
				// below determines if that server is accessible
				bool serverTypeSupported = ((ServerDescription.ServerTypes & serverType) > 0);
				if (serverTypeSupported)
				{
					switch (serverType)
					{
						case (uint)ServerType.OPC_DA205_Wrapper:
						case (uint)ServerType.OPC_DA30_Wrapper:
						case (uint)ServerType.OPC_UA_DA_Wrapper:
						case (uint)ServerType.OPC_XMLDA_Wrapper:
						case (uint)ServerType.Xi_DataServer:
							if ((context.NegotiatedContextOptions & (uint)ContextOptions.EnableDataAccess) == 0)
								serverTypeSupported = false;
							break;

						case (uint)ServerType.OPC_AE11_Wrapper:
						case (uint)ServerType.OPC_UA_AC_Wrapper:
						case (uint)ServerType.Xi_EventServer:
							if ((context.NegotiatedContextOptions & (uint)ContextOptions.EnableAlarmsAndEventsAccess) == 0)
								serverTypeSupported = false;
							break;

						case (uint)ServerType.OPC_HDA12_Wrapper:
						case (uint)ServerType.OPC_UA_HDA_Wrapper:
						case (uint)ServerType.Xi_DataJournalServer:
							if ((context.NegotiatedContextOptions & (uint)ContextOptions.EnableJournalDataAccess) == 0)
								serverTypeSupported = false;
							break;

						case (uint)ServerType.Xi_EventJournalServer:
							if ((context.NegotiatedContextOptions & (uint)ContextOptions.EnableJournalAlarmsAndEventsAccess) == 0)
								serverTypeSupported = false;
							break;

						default:
							break;
					}
					if (serverTypeSupported) // both configured and accessible
					{
						localeIds = OnGetLocaleIds(context, serverType);
						if (localeIds != null)
						{
							if (_ThisServerEntry.ServerDescription.SupportedLocaleIds == null)
								_ThisServerEntry.ServerDescription.SupportedLocaleIds = localeIds;
							else
							{
								foreach (var lcid in localeIds)
									_ThisServerEntry.ServerDescription.SupportedLocaleIds.Add(lcid);
							}
						}
					}
				}
			}
			// remove the duplicates from the locale id list
			if (   (_ThisServerEntry.ServerDescription.SupportedLocaleIds != null)
				&& (_ThisServerEntry.ServerDescription.SupportedLocaleIds.Count > 1))
			{
				IEnumerable<uint> distinctIds = _ThisServerEntry.ServerDescription.SupportedLocaleIds.Distinct();
				_ThisServerEntry.ServerDescription.SupportedLocaleIds = distinctIds.ToList();
			}
			context.SetSupportedLocaleIds(_ThisServerEntry.ServerDescription.SupportedLocaleIds);

			_ServerDetails = OnGetServerDetails(context);
		}

		/// <summary>
		/// Override this method in a server specific subclass of ServerBase
		/// </summary>
		/// <param name="context">
		/// The context for this method invocation
		/// </param>
		/// <returns>
		/// A subset of the description fields for the server.
		/// </returns>
		protected abstract ServerDetails OnGetServerDetails(TContext context);

		/// <summary>
		/// Override this method in a server specific subclass of ServerBase
		/// </summary>
		protected abstract void OnSetServerRoles(TContext context);

		/// <summary>
		/// Override this method in a server specific subclass of ServerBase
		/// </summary>
		protected abstract void OnSetServerMethodsAndFeatures(TContext context);		

		/// <summary>
		/// Override this method in a server specific subclass of ServerBase
		/// </summary>
		/// <param name="context">
		/// The context for this method invocation
		/// </param>
		/// <param name="serverType">
		/// The server type for which locale ids are being requested.
		/// Only one server type may be specified by this parameter.
		/// </param>
		/// <returns>
		/// The list of locale ids supported by the server.
		/// </returns>
		protected abstract List<uint> OnGetLocaleIds(TContext context, uint serverType);

		/// <summary>
		/// Override this method in a server specific subclass of ServerBase
		/// </summary>
		/// <param name="context">
		/// The context for this method invocation
		/// </param>
		/// <returns>
		/// The list of event message fields that can be used for filtering.
		/// </returns>
		protected abstract List<string> OnGetEventFilters(TContext context);

		/// <summary>
		/// Override this method in a server specific subclass of ServerBase
		/// </summary>
		/// <param name="context">
		/// The context for this method invocation
		/// </param>
		/// <returns>
		/// The list of categories supported by an alarms and events server.
		/// </returns>
		protected abstract List<CategoryConfiguration> OnGetCategoryConfiguration(TContext context);

		/// <summary>
		/// Override this method in a server specific subclass of ServerBase
		/// </summary>
		/// <param name="context">
		/// The context for this method invocation
		/// </param>
		/// <returns>
		/// The Data Journal Options supported by the data journal.
		/// </returns>
		protected abstract DataJournalOptions OnGetDataJournalOptions(TContext context);

		/// <summary>
		/// Override this method in a server specific subclass of ServerBase
		/// </summary>
		/// <param name="context">
		/// The context for this method invocation
		/// </param>
		/// <returns>
		/// The list of event message fields that can be used for filtering the 
		/// event journal.
		/// </returns>
		protected abstract List<uint> OnGetEventJournalFilters(TContext context);

		/// <summary>
		/// Override this method in a server specific subclass of ServerBase
		/// </summary>
		/// <param name="context">
		/// The context for this method invocation
		/// </param>
		/// <returns>
		/// The list of categories supported by the event journal.
		/// </returns>
		protected abstract List<CategoryConfiguration> OnGetEventJournalCategoryConfiguration(TContext context);

	}
}
