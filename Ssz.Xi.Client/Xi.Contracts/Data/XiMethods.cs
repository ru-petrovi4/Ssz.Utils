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
using System.Runtime.Serialization;

namespace Xi.Contracts.Data
{
	/// <summary>
	/// This enumeration assigns a flag value to each of the Xi 
	/// methods and then defines standard sets of methods required  
	/// for all data, event, data journal, and event journal servers.  
	/// </summary>
	[Flags]
	[DataContract(Namespace = "urn:xi/data")]
	public enum XiMethods : ulong
	{
		#region IServerDiscovery Methods
		/// <summary>
		/// The server supports the IServerDiscovery.DiscoverServers() method.
		/// </summary>
		[EnumMember] IServerDiscovery_DiscoverServers              = 0x0000000000000001,

		/// <summary>
		/// The server supports the IServerDiscovery.DiscoverServerInfo() method.
		/// </summary>
		[EnumMember] IServerDiscovery_DiscoverServerInfo           = 0x0000000000000002,
		#endregion //Server Discovery Methods

		#region IResourceManagement Context Management Methods
		/// <summary>
		/// The server supports the IResourceManagement.Initiate() method.
		/// </summary>
		[EnumMember] IResourceManagement_Initiate                  = 0x0000000000000004,

		/// <summary>
		/// The server supports the IResourceManagement.ReInitiate() method.
		/// </summary>
		[EnumMember] IResourceManagement_ReInitiate                = 0x0000000000000008,

		/// <summary>
		/// The server supports the IResourceManagement.Conclude() method.
		/// </summary>
		[EnumMember] IResourceManagement_Conclude                  = 0x0000000000000010,

		/// <summary>
		/// The server supports the IResourceManagement.ClientKeepAlive() method.
		/// </summary>
		[EnumMember] IResourceManagement_ClientKeepAlive           = 0x0000000000000020,
		#endregion // IResourceManagement Context Management Methods

		#region IResourceManagement Info Discovery Methods
		/// <summary>
		/// The server supports the IResourceManagement.Identify() method.
		/// </summary>
		[EnumMember] IResourceManagement_Identify                  = 0x0000000000000040,

		/// <summary>
		/// The server supports the IResourceManagement.Status() method.
		/// </summary>
		[EnumMember] IResourceManagement_Status                    = 0x0000000000000080,

		/// <summary>
		/// The server supports the IResourceManagement.LookupResultCodes() method.
		/// </summary>
		[EnumMember] IResourceManagement_LookupResultCodes         = 0x0000000000000100,

		/// <summary>
		/// The server supports the IResourceManagement.FindObjects() method.
		/// </summary>
		[EnumMember] IResourceManagement_FindObjects               = 0x0000000000000200,

		/// <summary>
		/// The server supports the IResourceManagement.FindTypes() method.
		/// </summary>
		[EnumMember] IResourceManagement_FindTypes                 = 0x0000000000000400,

		/// <summary>
		/// The server supports the IResourceManagement.FindRootPaths() method.
		/// </summary>
		[EnumMember] IResourceManagement_FindRootPaths             = 0x0000000000000800,

		/// <summary>
		/// The server supports the IResourceManagement.GetStandardMib() method.
		/// </summary>
		[EnumMember] IResourceManagement_GetStandardMib            = 0x0000000000001000,

		/// <summary>
		/// The server supports the IResourceManagement.GetVendorMib() method.
		/// </summary>
		[EnumMember] IResourceManagement_GetVendorMib              = 0x0000000000002000,
		#endregion //IResourceManagement Info Discovery Methods

		#region IResourceManagement Endpoint Management Methods
		/// <summary>
		/// The server supports the IResourceManagement.OpenEndpoint() method.
		/// </summary>
		[EnumMember] IResourceManagement_OpenEndpoint              = 0x0000000000004000,

		/// <summary>
		/// The server supports the IResourceManagement.AddListToEndpoint() method.
		/// </summary>
		[EnumMember] IResourceManagement_AddListToEndpoint         = 0x0000000000008000,

		/// <summary>
		/// The server supports the IResourceManagement.RemoveListsFromEndpoint() method.
		/// </summary>
		[EnumMember] IResourceManagement_RemoveListsFromEndpoint   = 0x0000000000010000,

		/// <summary>
		/// The server supports the IResourceManagement.CloseEndpoint() method.
		/// </summary>
		[EnumMember] IResourceManagement_CloseEndpoint             = 0x0000000000020000,
		#endregion // IResourceManagement Endpoint Management Methods

		#region IResourceManagement List Management Methods
		/// <summary>
		/// The server supports the IResourceManagement.DefineList() method.
		/// </summary>
		[EnumMember] IResourceManagement_DefineList                = 0x0000000000040000,

		/// <summary>
		/// The server supports the IResourceManagement.GetListAttributes() method.
		/// </summary>
		[EnumMember] IResourceManagement_GetListAttributes         = 0x0000000000080000,

		/// <summary>
		/// The server supports the IResourceManagement.RenewAliases() method.
		/// </summary>
		[EnumMember] IResourceManagement_RenewAliases              = 0x0000000000100000,

		/// <summary>
		/// The server supports the IResourceManagement.DeleteLists() method.
		/// </summary>
		[EnumMember] IResourceManagement_DeleteLists               = 0x0000000000200000,

		/// <summary>
		/// The server supports the IResourceManagement.AddDataObjectsToList() method.
		/// </summary>
		[EnumMember] IResourceManagement_AddDataObjectsToList      = 0x0000000000400000,

		/// <summary>
		/// The server supports the IResourceManagement.RemoveDataObjectsFromList() method.
		/// </summary>
		[EnumMember] IResourceManagement_RemoveDataObjectsFromList = 0x0000000000800000,

		/// <summary>
		/// The server supports the IResourceManagement.ModifyListAttributes() method.
		/// </summary>
		[EnumMember] IResourceManagement_ModifyListAttributes      = 0x0000000001000000,

		/// <summary>
		/// The server supports the IResourceManagement.EnableListUpdating() method.
		/// </summary>
		[EnumMember] IResourceManagement_EnableListUpdating        = 0x0000000002000000,

		/// <summary>
		/// The server supports the IResourceManagement.EnableListElementUpdating() method.
		/// </summary>
		[EnumMember] IResourceManagement_EnableListElementUpdating = 0x0008000000000000,

		/// <summary>
		/// The server supports the IResourceManagement.AddEventMessageFields() method.
		/// </summary>
		[EnumMember] IResourceManagement_AddEventMessageFields     = 0x0000000004000000,

		/// <summary>
		/// The server supports the IResourceManagement.TouchDataObjects() method.
		/// </summary>
		[EnumMember] IResourceManagement_TouchDataObjects          = 0x0000000008000000,

		/// <summary>
		/// The server supports the IResourceManagement.TouchList() method.
		/// </summary>
		[EnumMember] IResourceManagement_TouchList                 = 0x0000000010000000,
		#endregion //IResourceManagement List Management Methods

		#region IResourceManagement Info Alarms and Events Methods
		/// <summary>
		/// The server supports the IResourceManagement.GetAlarmSummary() method.
		/// </summary>
		[EnumMember] IResourceManagement_GetAlarmSummary           = 0x0000000020000000,

		/// <summary>
		/// The server supports the IResourceManagement.EnableAlarms() method.
		/// </summary>
		[EnumMember] IResourceManagement_EnableAlarms              = 0x0200000000000000,

		/// <summary>
		/// The server supports the IResourceManagement.GetAlarmsEnabledState() method.
		/// </summary>
		[EnumMember] IResourceManagement_GetAlarmsEnabledState     = 0x0400000000000000,

		#endregion // IResourceManagement Info Alarms and Events Methods

		#region IRead Methods
		/// <summary>
		/// The server supports the IRead.ReadData() method.
		/// </summary>
		[EnumMember] IRead_ReadData                                = 0x0000000040000000,

		/// <summary>
		/// The server supports the IRead.ReadJournalDataForTimeInterval() method.
		/// </summary>
		[EnumMember] IRead_ReadJournalDataForTimeInterval          = 0x0000000080000000,

		/// <summary>
		/// The server supports the IRead.ReadJournalDataNext() method.
		/// </summary>
		[EnumMember] IRead_ReadJournalDataNext                     = 0x0100000000000000,

		/// <summary>
		/// The server supports the IRead.ReadJournalDataAtSpecificTimes() method.
		/// </summary>
		[EnumMember] IRead_ReadJournalDataAtSpecificTimes          = 0x0000000100000000,

		/// <summary>
		/// The server supports the IRead.ReadJournalDataChanges() method.
		/// </summary>
		[EnumMember] IRead_ReadJournalDataChanges                  = 0x0000000200000000,

		/// <summary>
		/// The server supports the IRead.ReadJournalDataChangesNext() method.
		/// </summary>
		[EnumMember] IRead_ReadJournalDataChangesNext              = 0x0010000000000000,

		/// <summary>
		/// The server supports the IRead.ReadCalculatedJournalData() method.
		/// </summary>
		[EnumMember] IRead_ReadCalculatedJournalData               = 0x0000000400000000,

		/// <summary>
		/// The server supports the IRead.ReadJournalDataProperties() method.
		/// </summary>
		[EnumMember] IRead_ReadJournalDataProperties               = 0x0000000800000000,

		/// <summary>
		/// The server supports the IRead.ReadEvents() method.
		/// </summary>
		[EnumMember] IRead_ReadEvents                              = 0x0000001000000000,

		/// <summary>
		/// The server supports the IRead.ReadJournalEvents() method.
		/// </summary>
		[EnumMember] IRead_ReadJournalEvents                       = 0x0000002000000000,

		/// <summary>
		/// The server supports the IRead.ReadJournalEventsNext() method.
		/// </summary>
		[EnumMember] IRead_ReadJournalEventsNext                   = 0x0020000000000000,
		#endregion // IRead Methods

		#region IWrite Methods
		/// <summary>
		/// The server supports the IWrite.WriteValues() method.
		/// </summary>
		[EnumMember] IWrite_WriteValues                            = 0x0000004000000000,

		/// <summary>
		/// The server supports the IWrite.WriteVST() method.
		/// </summary>
		[EnumMember] IWrite_WriteVST                               = 0x0000008000000000,

		/// <summary>
		/// The server supports the IWrite.WriteJournalData() method.
		/// </summary>
		[EnumMember] IWrite_WriteJournalData                       = 0x0000010000000000,

		/// <summary>
		/// The server supports the IWrite.WriteJournalEvents() method.
		/// </summary>
		[EnumMember] IWrite_WriteJournalEvents                     = 0x0000020000000000,

		/// <summary>
		/// The server supports the IWrite.AcknowledgeAlarms() method.
		/// </summary>
		[EnumMember] IWrite_AcknowledgeAlarms                      = 0x0000040000000000,

		/// <summary>
		/// The server supports the IWrite.Passthrough() method.
		/// </summary>
		[EnumMember] IWrite_Passthrough                            = 0x0000080000000000,
		#endregion //IWrite Methods

		#region ICallback and IRegisterForCallback Methods
		/// <summary>
		/// The server supports the ICallback.Abort() method.
		/// </summary>
		[EnumMember] ICallback_Abort                               = 0x0000100000000000,

		/// <summary>
		/// The server supports the ICallback.InformationReport() method.
		/// </summary>
		[EnumMember] ICallback_InformationReport = 0x0000200000000000,

		/// <summary>
		/// The server supports the ICallback.EventNotification() method.
		/// </summary>
		[EnumMember] ICallback_EventNotification = 0x0000400000000000,

		/// <summary>
		/// The server supports the ICallback.PassthroughCallback() method.
		/// </summary>
		[EnumMember] ICallback_PassthroughCallback                 = 0x0040000000000000,

		/// <summary>
		/// The server supports the IRegisterForCallback.SetCallback() method.
		/// </summary>
		[EnumMember] IRegisterForCallback_SetCallback              = 0x0000800000000000,
		#endregion // ICallback and IRegisterForCallback Methods

		#region IPoll Methods
		/// <summary>
		/// The server supports the IPoll.PollDataChanges() method.
		/// </summary>
		[EnumMember] IPoll_PollDataChanges                         = 0x0001000000000000,

		/// <summary>
		/// The server supports the IPoll.PollEventChanges() method.
		/// </summary>
		[EnumMember] IPoll_PollEventChanges                        = 0x0002000000000000,

		/// <summary>
		/// The server supports the IPoll.PollPassthroughResponses() method.
		/// </summary>
		[EnumMember] IPoll_PollPassthroughResponses                = 0x0080000000000000,
		#endregion // IPoll Methods

		#region IRestRead Methods
		/// <summary>
		/// The server supports the IRestRead.RestReadData() method.
		/// </summary>
		[EnumMember] IRestRead_ReadData                            = 0x0004000000000000,
		#endregion // IRestRead Methods

		#region Profiles
		/// <summary>
		/// The methods required of all servers except for server discovery servers.
		/// </summary>
		[EnumMember] ServerCommonMethods = IServerDiscovery_DiscoverServerInfo

										 | IResourceManagement_Initiate
										 | IResourceManagement_ReInitiate
										 | IResourceManagement_Conclude
										 | IResourceManagement_ClientKeepAlive

										 | IResourceManagement_Identify
										 | IResourceManagement_Status
										 | IResourceManagement_LookupResultCodes
										 | IResourceManagement_FindObjects
										 | IResourceManagement_GetStandardMib

										 | IResourceManagement_OpenEndpoint
										 | IResourceManagement_AddListToEndpoint
										 | IResourceManagement_RemoveListsFromEndpoint
										 | IResourceManagement_CloseEndpoint

										 | IResourceManagement_DefineList
										 | IResourceManagement_GetListAttributes
										 | IResourceManagement_RenewAliases
										 | IResourceManagement_DeleteLists
										 | IResourceManagement_ModifyListAttributes
										 | IResourceManagement_EnableListUpdating
										 | IResourceManagement_EnableListElementUpdating
										 | IResourceManagement_TouchDataObjects 
										 | IResourceManagement_TouchList,

		/// <summary>
		/// The methods required of all data servers.
		/// </summary>
		[EnumMember] BasicDataServerMethodProfile = ServerCommonMethods
												  | IResourceManagement_AddDataObjectsToList
												  | IResourceManagement_RemoveDataObjectsFromList
												  | IRead_ReadData
												  | IWrite_WriteValues,

		/// <summary>
		/// The methods required of all data servers that support polling.
		/// </summary>
		[EnumMember] PolledDataServerMethodProfile = BasicDataServerMethodProfile
												   | IPoll_PollDataChanges,

		/// <summary>
		/// The methods required of all data servers that support callbacks.
		/// </summary>
		[EnumMember] CallbackDataServerMethodProfile = BasicDataServerMethodProfile
													 | ICallback_Abort
													 | IRegisterForCallback_SetCallback
													 | ICallback_InformationReport,

		/// <summary>
		/// The methods required of all data servers that support polling and callbacks.
		/// </summary>
		[EnumMember] FullDataServerMethodProfile = BasicDataServerMethodProfile
												 | IPoll_PollDataChanges
												 | IRegisterForCallback_SetCallback
												 | ICallback_Abort
												 | ICallback_InformationReport,

		/// <summary>
		/// The methods required of all event servers.
		/// </summary>
		[EnumMember] BasicEventServerMethodProfile = ServerCommonMethods
												   | IResourceManagement_GetAlarmSummary
												   | IRead_ReadEvents,

		/// <summary>
		/// The methods required of all event servers that support polling.
		/// </summary>
		[EnumMember] PolledEventServerMethodProfile = BasicEventServerMethodProfile
													| IPoll_PollDataChanges,

		/// <summary>
		/// The methods required of all event servers that support callbacks.
		/// </summary>
		[EnumMember] CallbackEventServerMethodProfile = BasicEventServerMethodProfile
													  | IRegisterForCallback_SetCallback
													  | ICallback_Abort
													  | ICallback_EventNotification,

		/// <summary>
		/// The methods required of all event servers that support polling and callbacks.
		/// </summary>
		[EnumMember] FullEventServerMethodProfile = BasicEventServerMethodProfile
												  | IPoll_PollDataChanges
												  | IRegisterForCallback_SetCallback
												  | ICallback_Abort
												  | ICallback_EventNotification,

		/// <summary>
		/// The methods required of all data journals.
		/// </summary>
		[EnumMember] DataJournalMethodProfile = ServerCommonMethods
											  | IResourceManagement_AddDataObjectsToList
											  | IResourceManagement_RemoveDataObjectsFromList
											  | IRead_ReadJournalDataForTimeInterval
											  | IRead_ReadJournalDataNext
											  | IRead_ReadJournalDataAtSpecificTimes
											  | IRead_ReadJournalDataChanges
											  | IRead_ReadJournalDataChangesNext
											  | IRead_ReadCalculatedJournalData
											  | IRead_ReadJournalDataProperties,

		/// <summary>
		/// The methods required of all event journals.
		/// </summary>
		[EnumMember] EventJournalMethodProfile = ServerCommonMethods 
											   | IRead_ReadJournalEvents
											   | IRead_ReadJournalEventsNext,
		#endregion // Profiles

	}
}