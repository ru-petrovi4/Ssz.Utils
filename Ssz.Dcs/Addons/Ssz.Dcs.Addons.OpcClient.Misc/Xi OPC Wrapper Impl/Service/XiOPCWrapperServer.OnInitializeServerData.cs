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
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using Xi.Common.Support;
using Xi.Contracts;
using Xi.Contracts.Constants;
using Xi.Contracts.Data;
using Xi.OPC.COM.API;
using Xi.Server.Base;

using COMservers = Xi.OPC.COM.Impl.CCreateInstance;
using Ssz.Utils;

namespace Xi.OPC.Wrapper.Impl
{
	////[ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.PerCall)]
	//[ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Reentrant, InstanceContextMode = InstanceContextMode.PerCall)]
	public partial class XiOPCWrapperServer : ServerBase<ContextImpl, ListRoot>
	{
        public static void Initialize(CaseInsensitiveDictionary<string> contextParams)
        {
			Initialize();

            // Check the App.Config file for the server type.
            // By changing the App.Config file it is possible to connect 
            // to different OPC COM Servers.  The servers supported by this 
            // implementation are:
            // 1) OPC DA 2.05
            // 2) OPC HDA 1.2
            // 3) OPC A&E 1.1

            // Start with the base server type and then add the wrapped servers
            _ThisServerEntry.ServerDescription.ServerTypes = 0;//|= BaseXiServerType;

            // TODO:  Add support for additional server types as necessary

            string daServerProgId = contextParams.TryGetValue(@"%(OpcDa_ProgId)");
            if (!string.IsNullOrEmpty(daServerProgId))
            {
                OpcServerInfo daOpcServerInfo = new OpcServerInfo();
                daOpcServerInfo.ServerType = ServerType.OPC_DA205_Wrapper;
                daOpcServerInfo.HostName = contextParams.TryGetValue(@"%(OpcDa_Host)");
                daOpcServerInfo.ProgId = daServerProgId;
                ConfiguredOpcServerInfos.Add(daOpcServerInfo);

                _ThisServerEntry.ServerDescription.ServerTypes |= ServerType.OPC_DA205_Wrapper;
                _NumServerTypes++;
                _WrappedServerRoots.Add(new ObjectAttributes
                {
                    DataTypeId = null,
                    ObjectFlags = 0,
                    Name = DA205_RootName,
                    InstanceId = null,
                    Roles = null
                });
            }
            string hdaServerProgId = contextParams.TryGetValue(@"%(OpcHda_ProgId)");
            if (!string.IsNullOrEmpty(hdaServerProgId))
            {
                OpcServerInfo hdaOpcServerInfo = new OpcServerInfo();
                hdaOpcServerInfo.ServerType = ServerType.OPC_HDA12_Wrapper;
                hdaOpcServerInfo.HostName = contextParams.TryGetValue(@"%(OpcHda_Host)");
                hdaOpcServerInfo.ProgId = hdaServerProgId;
                ConfiguredOpcServerInfos.Add(hdaOpcServerInfo);

                _ThisServerEntry.ServerDescription.ServerTypes |= ServerType.OPC_HDA12_Wrapper;
                _NumServerTypes++;
                _WrappedServerRoots.Add(new ObjectAttributes
                {
                    DataTypeId = null,
                    ObjectFlags = 0,
                    Name = HDA_RootName,
                    InstanceId = null,
                    Roles = null
                });
            }

            var usoServerProgId = contextParams.TryGetValue(@"%(UsoHda_ProgId)");
            if (!string.IsNullOrEmpty(usoServerProgId))
            {
                var hdaOpcServerInfo = new OpcServerInfo
                {
                    ServerType = ServerType.USO_HDA_Wrapper,
                    HostName = contextParams.TryGetValue(@"%(UsoHda_Host)"),
                    ProgId = usoServerProgId
                };

                ConfiguredOpcServerInfos.Add(hdaOpcServerInfo);

                _ThisServerEntry.ServerDescription.ServerTypes |= ServerType.USO_HDA_Wrapper;
                _NumServerTypes++;
                _WrappedServerRoots.Add(new ObjectAttributes
                {
                    DataTypeId = null,
                    ObjectFlags = 0,
                    Name = HDA_RootName,
                    InstanceId = null,
                    Roles = null
                });
            }

            string aeServerProgId = contextParams.TryGetValue(@"%(OpcAe_ProgId)");
            if (!string.IsNullOrEmpty(aeServerProgId))
            {
                OpcServerInfo aeOpcServerInfo = new OpcServerInfo();
                aeOpcServerInfo.ServerType = ServerType.OPC_AE11_Wrapper;
                aeOpcServerInfo.HostName = contextParams.TryGetValue(@"%(OpcAe_Host)");
                aeOpcServerInfo.ProgId = aeServerProgId;
                ConfiguredOpcServerInfos.Add(aeOpcServerInfo);

                _ThisServerEntry.ServerDescription.ServerTypes |= ServerType.OPC_AE11_Wrapper;
                _NumServerTypes++;
                _WrappedServerRoots.Add(new ObjectAttributes
                {
                    DataTypeId = null,
                    ObjectFlags = 0,
                    Name = AE_RootName,
                    InstanceId = null,
                    Roles = new List<TypeId>() { ObjectRoleIds.AreaRootRoleId }
                });
            }

            //Set the ServerNamespace to null - there are no server specific types
            _ThisServerEntry.ServerDescription.ServerNamespace = null;
        }
        
		public static List<OpcServerInfo> ConfiguredOpcServerInfos { get; } = new List<OpcServerInfo>();
		
		// TODO:  Expand this list of wrapped server names as necessary
		public const string DA205_RootName = InstanceIds.ResourceType_DA;
		public const string AE_RootName    = InstanceIds.ResourceType_AE;
		public const string HDA_RootName   = InstanceIds.ResourceType_HDA;		

		// The static XiOPCWrapper() method below sets this list according to the app.config file contents
		private static List<ObjectAttributes> _WrappedServerRoots = new List<ObjectAttributes>();
		public static List<ObjectAttributes> WrappedServerRoots
		{
			get { return _WrappedServerRoots; }
			private set { }
		}		

		/// <summary>
		/// The static XiOPCWrapper Constructor reads the App.Config file to
		/// determine the types of OPC Servers being wrapped.
		/// </summary>
		static XiOPCWrapperServer()
		{
			// It is necessary to initialize COM early to make sure it is ready 
			// prior to the first CoCreateInstance.
			cliHRESULT HR = COMservers.InitializeCOM();
			if (HR.Failed && !Debugger.IsAttached)
				throw FaultHelpers.Create((uint)HR.hResult, "OPC COM Services Initialization Failed.");	
		}

		/// <summary>
		/// This method creates a TypeId from a COM Variant type
		/// </summary>
		/// <param name="comType">
		/// The COM Variant type.
		/// </param>
		/// <returns>TypeId object or null</returns>
		private static TypeId CreateTypeId(ushort comType)
		{
			Type type = cliVARIANT.CliTypeFrom(comType);
			if (type == typeof(object)) return null;
			return new TypeId(type);
		}

		/// <summary>
		/// This override gets the XiOPCWrapper Details that are part of the ServerDescription.
		/// </summary>
		/// <param name="context">
		/// The context identifier.
		/// </param>
		/// <returns>
		/// The ServerDetails.
		/// </returns>
		protected override ServerDetails OnGetServerDetails(ContextImpl context)
		{
			ServerDetails serverDetails = null;
			uint SupportedServerTypes = _ThisServerEntry.ServerDescription.ServerTypes;
			if (context.IsAccessibleDataAccess)
			{
				if ((SupportedServerTypes & ServerType.OPC_DA205_Wrapper) != 0)
				{
					OPCSERVERSTATUS opcServerStatus = null;
					cliHRESULT HR = context.IOPCServer.GetStatus(out opcServerStatus);
					if (HR.Succeeded)
					{
						serverDetails = new ServerDetails();
						serverDetails.StartTime = opcServerStatus.dtStartTime;
						serverDetails.Version = opcServerStatus.wMajorVersion.ToString()
											  + "."
											  + opcServerStatus.wMinorVersion.ToString();
						serverDetails.BuildNumber = opcServerStatus.wBuildNumber.ToString();
						serverDetails.VendorInfo = opcServerStatus.sVendorInfo;
					}
					else
					{
						context.ClearAccessibleServerTypes(AccessibleServerTypes.JournalDataAccess);						
					}

				}

				if ((SupportedServerTypes & ServerType.OPC_DA30_Wrapper) != 0)
				{
					// TODO:  if this server supports this server type
				}				
			}

			if (context.IsAccessibleAlarmsAndEvents)
			{
				if ((SupportedServerTypes & ServerType.OPC_AE11_Wrapper) != 0)
				{
					cliOPCEVENTSERVERSTATUS opcEventServerStatus = null;
					cliHRESULT HR = context.IOPCEventServer.GetStatus(out opcEventServerStatus);
					if (HR.Succeeded)
					{
						if (serverDetails == null)
						{
							serverDetails = new ServerDetails();
							serverDetails.StartTime = opcEventServerStatus.dtStartTime;
							serverDetails.Version = opcEventServerStatus.wMajorVersion.ToString()
												  + "."
												  + opcEventServerStatus.wMinorVersion.ToString();
							serverDetails.BuildNumber = opcEventServerStatus.wBuildNumber.ToString();
						}
						if ((opcEventServerStatus.sVendorInfo != null) && (opcEventServerStatus.sVendorInfo.Length > 0))
						{
							if ((serverDetails.VendorInfo == null) || (serverDetails.VendorInfo.Length == 0))
								serverDetails.VendorInfo = opcEventServerStatus.sVendorInfo;
							else
								serverDetails.VendorInfo += ".  " + opcEventServerStatus.sVendorInfo;
						}
					}
					else
					{
						context.ClearAccessibleServerTypes(AccessibleServerTypes.JournalDataAccess);						
					}
				}				
			}

			if (context.IsAccessibleJournalDataAccess)
			{
				if ((SupportedServerTypes & ServerType.OPC_HDA12_Wrapper) != 0)
				{
					OPCHDA_SERVERSTATUS opcHdaServerStatus;
					DateTime dtCurrentTime;
					DateTime dtStartTime;
					ushort wMajorVersion;
					ushort wMinorVersion;
					ushort wBuildNumber;
					uint dwMaxReturnValues;
					string sStatusString;
					string sVendorInfo;

					cliHRESULT HR = context.IOPCHDA_Server.GetHistorianStatus(out opcHdaServerStatus,
																			  out dtCurrentTime,
																			  out dtStartTime,
																			  out wMajorVersion,
																			  out wMinorVersion,
																			  out wBuildNumber,
																			  out dwMaxReturnValues,
																			  out sStatusString,
																			  out sVendorInfo);
					if (HR.Succeeded)
					{
						if (serverDetails == null)
						{
							serverDetails = new ServerDetails();
							serverDetails.StartTime = dtStartTime;
							serverDetails.Version = wMajorVersion.ToString()
												  + "."
												  + wMinorVersion.ToString();
							serverDetails.BuildNumber = wBuildNumber.ToString();
						}
						if ((sVendorInfo != null) && (sVendorInfo.Length > 0))
						{
							if ((serverDetails.VendorInfo == null) || (serverDetails.VendorInfo.Length == 0))
								serverDetails.VendorInfo = sVendorInfo;
							else
								serverDetails.VendorInfo += ".  " + sVendorInfo;
						}
					}
					else
					{
						context.ClearAccessibleServerTypes(AccessibleServerTypes.JournalDataAccess);						
					}
				}				
			}			

			return serverDetails;
		}

		/// <summary>
		/// This override adds the server-specific methods and features to the 
		/// standard methods and features required by the server types supported 
		/// by the server. 
		/// interface method.
		/// </summary>
		/// <param name="context">
		/// The context identifier.
		/// </param>
		protected override void OnSetServerRoles(ContextImpl context)
		{
			// TODO:  Add any additional Roles for this server.
			// eg.
			// 	role = new ObjectRole()
			//	{
			//		RoleId = new TypeId(XiSchemaType.LocalServer, 
			//						   XiOPCWrapper._ThisServerEntry.ServerDescription.ServerNamespace, 
			//						   "MyServerSpecificRole"), 
			//		Name = "Area", 
			//		Description = "Default Area Role"
			//	};
			//	_StandardMib.ObjectRoles.Add(role);
		}

		/// <summary>
		/// This override adds the server-specific methods and features to the 
		/// standard methods and features required by the server types supported 
		/// by the server. 
		/// interface method.
		/// </summary>
		/// <param name="context">
		/// The context identifier.
		/// </param>
		protected override void OnSetServerMethodsAndFeatures(ContextImpl context)
		{
			// TODO:  Set the optional methods supported by the server by
			// "ORing" them into the _Mib.MethodsSupported element.
			// e.g.
			//	_StandardMib.MethodsSupported |= (ulong)XiMethods.IRead_ReadJournalDataAtSpecificTimes;
			//	_StandardMib.FeaturesSupported |= (ulong)XiFeatures.CustomDataType_Feature;
		}		

		/// <summary>
		/// This override gets the locale ids supported by a single server type of the server. 
		/// interface method.
		/// </summary>
		/// <param name="context">
		/// The context identifier.
		/// </param>
		/// <param name="serverType">
		/// The server type for which locale ids are being requested.
		/// Only one server type may be specified by this parameter.
		/// </param>
		/// <returns>The list of locale ids supported by the specified server type.</returns>
		protected override List<uint> OnGetLocaleIds(ContextImpl context, uint serverType)
		{
			// TODO:  Add any additional supported locale ids in the case statement below 

			List<uint> localeIds = null;
			cliHRESULT HR;
			switch (serverType)
			{
				case ServerType.OPC_DA205_Wrapper:
					if ((context.IOPCCommonDA != null) && (DaLocaleIdSet == false))
					{
						HR = context.IOPCCommonDA.QueryAvailableLocaleIDs(out localeIds);
						if (HR.Succeeded)
							DaLocaleIdSet = true;
						else
						{
							context.ClearAccessibleServerTypes(AccessibleServerTypes.DataAccess);							
						}
					}
					break;
				case ServerType.OPC_AE11_Wrapper:
					if ((context.IOPCCommonAE != null) && (AeLocaleIdSet == false))
					{
						HR = context.IOPCCommonAE.QueryAvailableLocaleIDs(out localeIds);
						if (HR.Succeeded)
							AeLocaleIdSet = true;
						else
						{
							context.ClearAccessibleServerTypes(AccessibleServerTypes.AlarmsAndEventsAccess);							
						}
					}
					break;
				case ServerType.OPC_HDA12_Wrapper:
					if ((context.IOPCCommonHDA != null) && (HdaLocaleIdSet == false))
					{
						HR = context.IOPCCommonHDA.QueryAvailableLocaleIDs(out localeIds);
						if (HR.Succeeded)
							HdaLocaleIdSet = true;
						else
						{
							context.ClearAccessibleServerTypes(AccessibleServerTypes.JournalDataAccess);							
						}
					}
					break;
				case ServerType.OPC_DA30_Wrapper:
					break;				
				default:
					break;
			}
			return localeIds;
		}

		/// <summary>
		/// This override gets the ids of the standard event message fields that can be 
		/// used for filtering by alarms and events servers.
		/// </summary>
		/// <param name="context">
		/// The context identifier.
		/// </param>
		/// <returns>The list of ids for the supported filters.</returns>
		protected override List<string> OnGetEventFilters(ContextImpl context)
		{
			List<string> standardEventMessageFilters = null;
			// Get the list of event message  fields that can be used for filtering
			uint opcEventFilters;
			if (context.IOPCEventServer != null)
			{
				cliHRESULT HR = context.IOPCEventServer.QueryAvailableFilters(out opcEventFilters);
				if (HR.Succeeded == false)
				{
					context.ClearAccessibleServerTypes(AccessibleServerTypes.AlarmsAndEventsAccess);					
				}
				if (opcEventFilters != 0)
				{
					standardEventMessageFilters = new List<string>();
					if ((opcEventFilters & (uint)OPCAEFILTERS.OPC_FILTER_BY_EVENT) != 0)
						standardEventMessageFilters.Add(FilterOperandNames.EventType);
					if ((opcEventFilters & (uint)OPCAEFILTERS.OPC_FILTER_BY_AREA) != 0)
						standardEventMessageFilters.Add(FilterOperandNames.Area);
					if ((opcEventFilters & (uint)OPCAEFILTERS.OPC_FILTER_BY_CATEGORY) != 0)
						standardEventMessageFilters.Add(FilterOperandNames.EventCategory);
					if ((opcEventFilters & (uint)OPCAEFILTERS.OPC_FILTER_BY_SEVERITY) != 0)
						standardEventMessageFilters.Add(FilterOperandNames.EventPriority);
					if ((opcEventFilters & (uint)OPCAEFILTERS.OPC_FILTER_BY_SOURCE) != 0)
						standardEventMessageFilters.Add(FilterOperandNames.EventSourceId);
				}
			}
			return standardEventMessageFilters;
		}

		/// <summary>
		/// This override gets the CategoryConfigurations supported by the server.
		/// </summary>
		/// <param name="context">
		/// The context identifier.
		/// </param>
		/// <returns>
		/// The list of CategoryConfigurations supported by the server.
		/// </returns>
		protected override List<CategoryConfiguration> OnGetCategoryConfiguration(ContextImpl context)
		{
			List<CategoryConfiguration> categoryConfigurations = null;
			if (context.IOPCEventServer != null)
			{
				// Get the Categories for the OPC A&E Event Typess
				uint eventType = (uint)OPCAEEVENTTYPE.OPC_SIMPLE_EVENT;
				bool bContinue = true;
				while (bContinue)// values 1, 2, and 4 are valid
				{
					// Create CategoryConfigurations 
					List<OPCEVENTCATEGORY> eventCategories = null;
					cliHRESULT HR = context.IOPCEventServer.QueryEventCategories(eventType, out eventCategories);
					if (false == HR.Succeeded)
					{
						context.ClearAccessibleServerTypes(AccessibleServerTypes.AlarmsAndEventsAccess);						
					}

					if (eventCategories != null)
					{
						if (categoryConfigurations == null)
							categoryConfigurations = new List<CategoryConfiguration>();
						foreach (var category in eventCategories)
						{
							CategoryConfiguration catConfig = new CategoryConfiguration();
							catConfig.CategoryId = category.dwEventCategory;
							catConfig.Name = category.sEventCategoryDesc;
							catConfig.Description = category.sEventCategoryDesc;

							// set the event type. Alarm types are set further below.
							if (eventType == (uint)OPCAEEVENTTYPE.OPC_SIMPLE_EVENT)
								catConfig.EventType = EventType.SystemEvent;
							else if (eventType == (uint)OPCAEEVENTTYPE.OPC_TRACKING_EVENT)
								catConfig.EventType = EventType.OperatorActionEvent;

							// Get the supported event message fields for the category
							List<OPCEVENTATTRIBUTE> opcEventAttrs;
							HR = context.IOPCEventServer.QueryEventAttributes(category.dwEventCategory, out opcEventAttrs);
							if (HR.Failed)
							{
								context.ClearAccessibleServerTypes(AccessibleServerTypes.AlarmsAndEventsAccess);								
							}
							if (opcEventAttrs != null)
							{
								catConfig.EventMessageFields = new List<ParameterDefinition>();

								foreach (var opcAttr in opcEventAttrs)
								{
									ParameterDefinition field = new ParameterDefinition();
									field.ObjectTypeId = new TypeId(
												XiSchemaType.OPC,
												XiOPCWrapperServer._ThisServerEntry.ServerDescription.VendorNamespace,
												opcAttr.dwAttrID.ToString());
									field.Name = opcAttr.sAttrDesc;
									field.DataTypeId = CreateTypeId(opcAttr.vtAttrType);
									catConfig.EventMessageFields.Add(field);
								}
							}

							if (eventType == (uint)OPCAEEVENTTYPE.OPC_CONDITION_EVENT)// if an alarm
							{
								// Generally set the event type to SimpleAlarm, then override it below if necessary
								catConfig.EventType = EventType.SimpleAlarm;

								// Get the condition names for the category
								List<string> condNames;
								HR = context.IOPCEventServer.QueryConditionNames(category.dwEventCategory, out condNames);
								if (false == HR.Succeeded)
								{
									context.ClearAccessibleServerTypes(AccessibleServerTypes.AlarmsAndEventsAccess);									
								}
								// Get the subcondition names for each condition
								if (condNames != null)
								{
									catConfig.AlarmDescriptions = new List<AlarmDescription>();
									List<string> subCondNames;
									foreach (var condName in condNames)
									{
										// create an alarm description for each condition defined for 
										// the category and then add it ot the CategoryConfiguration
										AlarmDescription alarmDesc = new AlarmDescription();
										alarmDesc.AlarmConditionNames = new List<TypeId>();
										// see if there are subconditions. If so, use the condition name 
										// as the grouped or eclipsed name. If not, use the condition name 
										// the name of a single alarm condition in the AlarmDescription
										HR = context.IOPCEventServer.QuerySubConditionNames(condName, out subCondNames);
										if (false == HR.Succeeded)
										{
											context.ClearAccessibleServerTypes(AccessibleServerTypes.AlarmsAndEventsAccess);											
										}
										if (subCondNames != null)
										{
											if (   (subCondNames.Count > 1)
												|| (   (subCondNames.Count == 1)                      // only 1 subcondition 
													&& (string.Compare(subCondNames[0], condName) != 0)// but not the condition name
												   )
											   )
											{
												// If there are subconditions, set the event type to "eclipsed" (because 
												// OPC A&E does not support "grouped"), and use the condition name as 
												// the eclipsed name. 
												catConfig.EventType = EventType.EclipsedAlarm;
												alarmDesc.MultiplexedAlarmContainer = new TypeId()
												{
													SchemaType = XiSchemaType.OPC,
													Namespace = XiOPCWrapperServer.ServerDescription.VendorNamespace,
													LocalId = condName
												};
											}
											foreach (var subCondName in subCondNames)
											{
												alarmDesc.AlarmConditionNames.Add(new TypeId()
												{
													SchemaType = XiSchemaType.OPC,
													Namespace = XiOPCWrapperServer.ServerDescription.VendorNamespace,
													LocalId = subCondName
												});
											}
										}
										else
										{
											// If there are no subconditions, set the event type to "SimpleAlarm" 
											// and use the condition name as the name of a single alarm condition 
											// in the AlarmDescription
											alarmDesc.AlarmConditionNames.Add(new TypeId()
											{
												SchemaType = XiSchemaType.OPC,
												Namespace = XiOPCWrapperServer.ServerDescription.VendorName,
												LocalId = condName
											});
										}
										catConfig.AlarmDescriptions.Add(alarmDesc);
									}
								}
							}

                            // VladP: category names should be unique, because they are used as dictionary keys!
                            if (!categoryConfigurations.Any(config => config.Name == catConfig.Name))
							    categoryConfigurations.Add(catConfig);
						}
					}
					// set the eventType for the next loop. Break if finished
					if (eventType == (uint)OPCAEEVENTTYPE.OPC_SIMPLE_EVENT)
						eventType = (uint)OPCAEEVENTTYPE.OPC_TRACKING_EVENT;
					else if (eventType == (uint)OPCAEEVENTTYPE.OPC_TRACKING_EVENT)
						eventType = (uint)OPCAEEVENTTYPE.OPC_CONDITION_EVENT;
					else if (eventType == (uint)OPCAEEVENTTYPE.OPC_CONDITION_EVENT)
						bContinue = false; // break if finished
				}
			}
			return categoryConfigurations;
		}

		/// <summary>
		/// This override gets the DataJournalOptions supported by a data journal server.
		/// </summary>
		/// <param name="context">
		/// The context for this method invocation
		/// </param>
		/// <returns>
		/// The Data Journal Options supported by the data journal.
		/// </returns>
		protected override DataJournalOptions OnGetDataJournalOptions(ContextImpl context)
		{
			DataJournalOptions dataJournalOptions = new DataJournalOptions();

			// get the math libraries supported
			List<OPCHDAAGGREGATES> opcHdaAggregates = null;
			cliHRESULT HR = context.IOPCHDA_Server.GetAggregates(out opcHdaAggregates);
			if (false == HR.Succeeded)
			{
				context.ThrowOnDisconnectedServer(HR.hResult, context.IOPCHDAServer_ProgId);
				// The next line will not be executed if the call above throws
				throw FaultHelpers.Create((uint)HR.hResult, "OPC HDA GetAggregates() failed.");
			}
			if (opcHdaAggregates != null)
			{
				dataJournalOptions.MathLibrary = new List<TypeAttributes>();
				foreach (var aggregate in opcHdaAggregates)
				{
					TypeAttributes mathFunction    = new TypeAttributes();
					mathFunction.Name              = aggregate.sAggrName;
					mathFunction.Description       = aggregate.sAggrDesc;
					string nspace;
					if (aggregate.dwAggrID < 0x80000000)
						nspace = XiNamespace.OPCHDA;
					else
						nspace = _ThisServerEntry.ServerDescription.VendorName;
					mathFunction.TypeId = new TypeId(XiSchemaType.OPC, nspace, aggregate.dwAggrID.ToString());
					dataJournalOptions.MathLibrary.Add(mathFunction);
				}
			}
			// Get the supported properties for journal data
			List<OPCHDAITEMATTR> opcHdaItemAttributes;
			HR = context.IOPCHDA_Server.GetItemAttributes(out opcHdaItemAttributes);
			if (false == HR.Succeeded)
			{
				context.ThrowOnDisconnectedServer(HR.hResult, context.IOPCHDAServer_ProgId);
				// The next line will not be executed if the call above throws
				throw FaultHelpers.Create((uint)HR.hResult, "OPC HDA GetItemAttributes() failed.");
			}
			if (opcHdaItemAttributes != null)
			{
				dataJournalOptions.Properties = new List<ParameterDefinition>();

				foreach (var opcItemAttr in opcHdaItemAttributes)
				{
					ParameterDefinition property = new ParameterDefinition();
					property.ObjectTypeId = (opcItemAttr.dwAttrID < 0x80000000)
										  ? new TypeId(XiSchemaType.OPC, XiNamespace.OPCHDA, opcItemAttr.dwAttrID.ToString())
										  : new TypeId(XiSchemaType.OPC,
											  XiOPCWrapperServer._ThisServerEntry.ServerDescription.VendorNamespace, opcItemAttr.dwAttrID.ToString());
					property.Name         = opcItemAttr.sAttrName;
					property.Description  = opcItemAttr.sAttrDesc;
					property.DataTypeId   = CreateTypeId(opcItemAttr.vtAttrDataType);
					dataJournalOptions.Properties.Add(property);
				}
			}

			OPCHDA_SERVERSTATUS opcHdaServerStatus;
			DateTime dtCurrentTime;
			DateTime dtStartTime;
			ushort wMajorVersion;
			ushort wMinorVersion;
			ushort wBuildNumber;
			uint dwMaxReturnValues;
			string sStatusString;
			string sVendorInfo;

			HR = context.IOPCHDA_Server.GetHistorianStatus(out opcHdaServerStatus,
															out dtCurrentTime,
															out dtStartTime,
															out wMajorVersion,
															out wMinorVersion,
															out wBuildNumber,
															out dwMaxReturnValues,
															out sStatusString,
															out sVendorInfo);
			if (HR.Succeeded)
			{
				dataJournalOptions.MaxReturnValues = dwMaxReturnValues;
			}
			return dataJournalOptions;
		}

		/// <summary>
		/// This override gets the ids of the standard event message fields that can be 
		/// used for filtering by event journal servers.
		/// </summary>
		/// <param name="context">
		/// The context for this method invocation
		/// </param>
		/// <returns>
		/// The list of event message fields that can be used for filtering the 
		/// event journal.
		/// </returns>
		protected override List<uint> OnGetEventJournalFilters(ContextImpl context)
		{
			return null;
		}

		/// <summary>
		/// This override gets the CategoryConfigurations supported by an event journal server.
		/// </summary>
		/// <param name="context">
		/// The context for this method invocation
		/// </param>
		/// <returns>
		/// The list of categories supported by the event journal.
		/// </returns>
		protected override List<CategoryConfiguration> OnGetEventJournalCategoryConfiguration(ContextImpl context)
		{
			return null;
		}
	}
}
