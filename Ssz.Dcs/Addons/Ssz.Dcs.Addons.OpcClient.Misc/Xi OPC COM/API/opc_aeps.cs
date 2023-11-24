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

using Xi.Contracts.Data;

namespace Xi.OPC.COM.API
{
	/// <summary>
	/// 
	/// </summary>
	public enum OPCEVENTSERVERSTATE
	{
		OPCAE_STATUS_RUNNING   = 1,
		OPCAE_STATUS_FAILED    = 2,
		OPCAE_STATUS_NOCONFIG  = 3,
		OPCAE_STATUS_SUSPENDED = 4,
		OPCAE_STATUS_TEST      = 5
	}

	/// <summary>
	/// 
	/// </summary>
	public class cliOPCEVENTSERVERSTATUS
	{
		public DateTime dtStartTime;
		public DateTime dtCurrentTime;
		public DateTime dtLastUpdateTime;
		public OPCEVENTSERVERSTATE dwServerState;
		public ushort wMajorVersion;
		public ushort wMinorVersion;
		public ushort wBuildNumber;
		public ushort wReserved;
		public string sVendorInfo;
	}

	/// <summary>
	/// 
	/// </summary>
	[Flags]
	public enum OPCAECONDITIONSTATE : ushort
	{
		OPC_CONDITION_ENABLED = 0x0001,
		OPC_CONDITION_ACTIVE  = 0x0002,
		OPC_CONDITION_ACKED   = 0x0004,
	}

	/// <summary>
	/// 
	/// </summary>
	[Flags]
	public enum OPCAEEVENTTYPE : uint
	{
		OPC_SIMPLE_EVENT    = 0x0001,
		OPC_TRACKING_EVENT  = 0x0002,
		OPC_CONDITION_EVENT = 0x0004,
	}

	/// <summary>
	/// 
	/// </summary>
	[Flags]
	public enum OPCAEFILTERS
	{
		OPC_FILTER_BY_EVENT    = 1,
		OPC_FILTER_BY_CATEGORY = 2,
		OPC_FILTER_BY_SEVERITY = 4,
		OPC_FILTER_BY_AREA     = 8,
		OPC_FILTER_BY_SOURCE   = 16
	}

	/// <summary>
	/// 
	/// </summary>
	[Flags]
	public enum OPCCHANGEMASK : ushort
	{
		OPC_CHANGE_ACTIVE_STATE		= 1,
		OPC_CHANGE_ACK_STATE		= 2,
		OPC_CHANGE_ENABLE_STATE		= 4,
		OPC_CHANGE_QUALITY			= 8,
		OPC_CHANGE_SEVERITY			= 16,
		OPC_CHANGE_SUBCONDITION		= 32,
		OPC_CHANGE_MESSAGE			= 64,
		OPC_CHANGE_ATTRIBUTE		= 128,
	}

	/// <summary>
	/// 
	/// </summary>
	public class OPCEVENTCATEGORY
	{
		public uint dwEventCategory;
		public string sEventCategoryDesc;
	}

	/// <summary>
	/// 
	/// </summary>
	public class OPCEVENTATTRIBUTE
	{
		public uint dwAttrID;
		public string sAttrDesc;
		public ushort vtAttrType;
	}

	/// <summary>
	/// 
	/// </summary>
	public class OPCEVENTITEMID
	{
		public string sAttrItemID;
		public string sNodeName;
		public Guid DaServerCLSID;
	}

	/// <summary>
	/// 
	/// </summary>
	public class OPCEVENTACKCONDITION
	{
		public string sSource;
		public string sConditionName;
		public DateTime dtActiveTime;
		public uint dwCookie;
	}

	/// <summary>
	/// 
	/// </summary>
	public interface IOPCEventServerCli
		: IOPCCommonCli
		, IDisposable
	{
		cliHRESULT GetStatus(
			/*[out]*/ out cliOPCEVENTSERVERSTATUS EventServerStatus);
		cliHRESULT CreateEventSubscription(
			/*[in]*/ bool bActive,
			/*[in]*/ uint dwBufferTime,
			/*[in]*/ uint dwMaxSize,
			/*[in]*/ uint hClientSubscription,
			/*[out]*/ out IOPCEventSubscriptionMgtCli iOPCEventSubscriptionMgt,
			/*[out]*/ out uint dwRevisedBufferTime,
			/*[out]*/ out uint dwRevisedMaxSize);
		cliHRESULT QueryAvailableFilters(
			/*[out]*/ out uint dwFilterMask);
		cliHRESULT QueryEventCategories(
			/*[in]*/ uint dwEventType,
			/*[out]*/ out List<OPCEVENTCATEGORY> EventCategories);
		cliHRESULT QueryConditionNames(
			/*[in]*/ uint dwEventCategory,
			/*[out]*/ out List<string> ConditionNames);
		cliHRESULT QuerySubConditionNames(
			/*[in]*/ string sConditionName,
			/*[out]*/ out List<string> SubConditionNames);
		cliHRESULT QuerySourceConditions(
			/*[in]*/ string sSource,
			/*[out]*/ out List<string> ConditionNames);
		cliHRESULT QueryEventAttributes(
			/*[in]*/ uint dwEventCategory,
			/*[out]*/ out List<OPCEVENTATTRIBUTE> EventCategories);
		cliHRESULT TranslateToItemIDs(
			/*[in]*/ string sSource,
			/*[in]*/ uint dwEventCategory,
			/*[in]*/ string sConditionName,
			/*[in]*/ string sSubconditionName,
			/*[in]*/ List<uint> dwAssocAttrIDs,
			/*[out]*/ out List<OPCEVENTITEMID> EventItemIDs);
		cliHRESULT GetConditionState(
			/*[in]*/ string sSource,
			/*[in]*/ string sConditionName,
			/*[in]*/ List<uint> AttributeIDs,
			/*[out]*/ out List<cliOPCCONDITIONSTATE> ConditionStates);
		cliHRESULT EnableConditionByArea(
			/*[in]*/ List<string> Areas);
		cliHRESULT EnableConditionBySource(
			/*[in]*/ List<string> Sources);
		cliHRESULT DisableConditionByArea(
			/*[in]*/ List<string> Areas);
		cliHRESULT DisableConditionBySource(
			/*[in]*/ List<string> Sources);
		cliHRESULT AckCondition(
			/*[in]*/ string sAcknowledgerID,
			/*[in]*/ string sComment,
			/*[in]*/ List<OPCEVENTACKCONDITION> AckConditions,
			/*[out]*/ out List<HandleAndHRESULT> ErrorList);
		cliHRESULT CreateAreaBrowser(
			/*[out]*/ out IOPCEventAreaBrowserCli iOPCEventAreaBrowser);
	}

	/// <summary>
	/// 
	/// </summary>
	public class cliOPCCONDITIONSTATE
	{
		public ushort wState;
		public ushort wReserved1;
		public string sActiveSubCondition;
		public string sASCDefinition;
		public uint dwASCSeverity;
		public string sASCDescription;
		public ushort wQuality;
		public ushort wReserved2;
		public DateTime dtLastAckTime;
		public DateTime dtSubCondLastActive;
		public DateTime dtCondLastActive;
		public DateTime dtCondLastInactive;
		public string sAcknowledgerID;
		public string sComment;
		public uint dwNumSCs;
		public List<string> sSCNames;
		public List<string> sSCDefinitions;
		public List<uint> dwSCSeverities;
		public List<string> sSCDescriptions;
		public List<cliVARIANT> vDataValues;
		public cliHRESULT Error;
	}

	/// <summary>
	/// 
	/// </summary>
	public interface IOPCEventSubscriptionMgtCli
	{
		cliHRESULT SetFilter(
			/*[in]*/ uint dwEventType,
			/*[in]*/ List<uint> eventCategories,
			/*[in]*/ uint dwLowSeverity,
			/*[in]*/ uint dwHighSeverity,
			/*[in]*/ List<string> areaList,
			/*[in]*/ List<string> sourceList);
		cliHRESULT GetFilter(
			/*[out]*/ out uint dwEventType,
			/*[out]*/ out List<uint> EventCategories,
			/*[out]*/ out uint dwLowSeverity,
			/*[out]*/ out uint dwHighSeverity,
			/*[out]*/ out List<string> areaList,
			/*[out]*/ out List<string> sSourceList);
		cliHRESULT SelectReturnedAttributes(
			/*[in]*/ uint dwEventCategory,
			/*[in]*/ List<uint> attributeIDs);
		cliHRESULT GetReturnedAttributes(
			/*[in]*/ uint dwEventCategory,
			/*[out]*/ out List<uint> attributeIDs);
		cliHRESULT Refresh(
			/*[in]*/ uint dwConnection);
		cliHRESULT CancelRefresh(
			/*[in]*/ uint dwConnection);
		cliHRESULT GetState(
			/*[out]*/ out bool bActive,
			/*[out]*/ out uint dwBufferTime,
			/*[out]*/ out uint dwMaxSize,
			/*[out]*/ out uint hClientSubscription);
		cliHRESULT SetState(
			/*[in]*/ Nullable<bool> bActive,
			/*[in]*/ Nullable<uint> dwBufferTime,
			/*[in]*/ Nullable<uint> dwMaxSize,
			/*[in]*/ uint hClientSubscription,
			/*[out]*/ out uint dwRevisedBufferTime,
			/*[out]*/ out uint dwRevisedMaxSize);
	}

	/// <summary>
	/// 
	/// </summary>
	public enum cliOPCAEBROWSEDIRECTION
	{
		OPCAE_BROWSE_UP = 1,
		OPCAE_BROWSE_DOWN = 2,
		OPCAE_BROWSE_TO = 3
	}

	/// <summary>
	/// 
	/// </summary>
	public enum cliOPCAEBROWSETYPE
	{
		OPC_AREA = 1,
		OPC_SOURCE = 2
	}

	/// <summary>
	/// 
	/// </summary>
	public interface IOPCEventAreaBrowserCli
		: IDisposable
	{
		cliHRESULT ChangeBrowsePosition(
			/*[in]*/ cliOPCAEBROWSEDIRECTION dwBrowseDirection,
			/*[in]*/ string sString);
		cliHRESULT BrowseOPCAreas(
			/*[in]*/ cliOPCAEBROWSETYPE dwBrowseFilterType,
			/*[in]*/ string sFilterCriteria,
			/*[out]*/ out cliIEnumString iEnumAreaNames);
		cliHRESULT GetQualifiedAreaName(
			/*[in]*/ string sAreaName,
			/*[out]*/ out string sQualifiedAreaName);
		cliHRESULT GetQualifiedSourceName(
			/*[in]*/ string sSourceName,
			/*[out]*/ out string sQualifiedSourceName);
	}

	/// <summary>
	/// 
	/// </summary>
	public class OPCEventSink
	{
		public delegate void OnEvent(
			/*[in]*/ uint hClientSubscription,
			/*[in]*/ bool bRefresh,
			/*[in]*/ bool bLastRefresh,
			/*[in]*/ EventMessage[] Events);
	}

	/// <summary>
	/// 
	/// </summary>
	public interface IAdviseOPCEventSink
	{
		cliHRESULT AdviseOnEvent(
			/*[in]*/ OPCEventSink.OnEvent onEvent);

		cliHRESULT UnadviseOnEvent(
			/*[in]*/ OPCEventSink.OnEvent onEvent);
	}
}
