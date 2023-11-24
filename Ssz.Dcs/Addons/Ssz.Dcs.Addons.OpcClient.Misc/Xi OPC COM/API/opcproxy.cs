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
	/// Constants used to define the OPC DA Server controlled access rights.
	/// <see cref="OpcDaAccessRights"/>
	/// </summary>
	public enum OPCACCESSRIGHTS : uint
	{
		OPC_READABLE = 1,
		OPC_WRITABLE = 2,
	}

	/// <summary>
	/// Constants for the OPC DA Server operational state.
	/// </summary>
	public enum OPCSERVERSTATE : ushort
	{
		OPC_STATUS_RUNNING = 1,
		OPC_STATUS_FAILED = 2,
		OPC_STATUS_NOCONFIG = 3,
		OPC_STATUS_SUSPENDED = 4,
		OPC_STATUS_TEST = 5
	}

	/// <summary>
	/// This is the OPC DA Server Status information this 
	/// is basically the same information available from 
	/// the OPC DA COM Server thus refer to the OPC DA 2.05a 
	/// specification for further information.
	/// </summary>
	public class OPCSERVERSTATUS
	{
		public DateTime dtStartTime;
		public DateTime dtCurrentTime;
		public DateTime dtLastUpdateTime;
		public OPCSERVERSTATE dwServerState;
		public uint dwGroupCount;
		public uint dwBandWidth;
		public ushort wMajorVersion;
		public ushort wMinorVersion;
		public ushort wBuildNumber;
		public string sVendorInfo;
	}

	/// <summary>
	/// This enumeration is used when enumerating groups 
	/// to specify the scope of that enumeration.
	/// </summary>
	public enum OPCENUMSCOPE : ushort
	{
		OPC_ENUM_PRIVATE_CONNECTIONS = 1,
		OPC_ENUM_PUBLIC_CONNECTIONS = 2,
		OPC_ENUM_ALL_CONNECTIONS = 3,
		OPC_ENUM_PRIVATE = 4,
		OPC_ENUM_PUBLIC = 5,
		OPC_ENUM_ALL = 6
	}

	/// <summary>
	/// This interface definition provides the functionality 
	/// similar to the OPC DA COM Server IOPCServer interface.  
	/// Refer to the OPC DA 2.05a specification for details 
	/// related to these methods.
	/// </summary>
	public interface IOPCServerCli
		: IOPCCommonCli
		, IDisposable
	{
		/// <summary>
		/// This method is used to add an OPC DA Group 
		/// to the OPC DA COM Server.
		/// </summary>
		/// <param name="sName"></param>
		/// <param name="bActive"></param>
		/// <param name="dwRequestedUpdateRate"></param>
		/// <param name="hClientGroup"></param>
		/// <param name="iTimeBias"></param>
		/// <param name="fPercentDeadband"></param>
		/// <param name="dwLCID"></param>
		/// <param name="hServerGroup">Not used in .NET environment</param>
		/// <param name="dwRevisedUpdateRate"></param>
		/// <param name="iOPCItemMgt"></param>
		/// <returns></returns>
		cliHRESULT AddGroup(
			/*[in]*/ string sName,
			/*[in]*/ bool bActive,
			/*[in]*/ uint dwRequestedUpdateRate,
			/*[in]*/ uint hClientGroup,
			/*[in]*/ Nullable<int> iTimeBias,
			/*[in]*/ Nullable<float> fPercentDeadband,
			/*[in]*/ uint dwLCID,
			///*[out]*/ out uint hServerGroup,
			/*[out]*/ out uint dwRevisedUpdateRate,
			/*[out]*/ out IOPCItemMgtCli iOPCItemMgt);

		/// <summary>
		/// This method is used to obtain the 
		/// error text associated with an HRESULT.
		/// </summary>
		/// <param name="dwError"></param>
		/// <param name="dwLocale"></param>
		/// <param name="sErrString"></param>
		/// <returns></returns>
		cliHRESULT GetErrorString(
			/*[in]*/ cliHRESULT dwError,
			/*[in]*/ uint dwLocale,
			/*[out]*/ out string sErrString );

		/// <summary>
		/// This method is used to obtain the interface 
		/// to a OPC DA Group using the Group Name.
		/// </summary>
		/// <param name="sName"></param>
		/// <param name="iOPCItemMgt"></param>
		/// <returns></returns>
		cliHRESULT GetGroupByName(
			/*[in]*/ string sName,
			/*[out]*/ out IOPCItemMgtCli iOPCItemMgt);

		/// <summary>
		/// This method is used to obtain the OPC DA COM Server status.
		/// </summary>
		/// <param name="ServerStatus"></param>
		/// <returns></returns>
		cliHRESULT GetStatus(
			/*[out]*/ out OPCSERVERSTATUS ServerStatus );

		/// <summary>
		/// Remove Group is used to remove the group from the OPC Server.  
		/// However, in keeping with the .NET way of doing things use 
		/// Dispose() to remove the group.
		/// </summary>
		/// <param name="hServerGroup"></param>
		/// <param name="bForce"></param>
		/// <returns></returns>
		cliHRESULT RemoveGroup(
			/*[in]*/ uint hServerGroup,
			/*[in]*/ bool bForce );

		/// <summary>
		/// This method is used to obtain an interface 
		/// that is then used to enumerate the OPC DA Groups.
		/// </summary>
		/// <param name="dwScope"></param>
		/// <param name="iOPCItemMgtList"></param>
		/// <returns></returns>
		cliHRESULT CreateGroupEnumerator(
			/*[in]*/ OPCENUMSCOPE dwScope,
			/*[out]*/ out List<IOPCItemMgtCli> iOPCItemMgtList);
	}

	/// <summary>
	/// This interface is not implemented by the OPC Wrapper.
	/// Refer to the OPC DA 2.05a specification for further information.
	/// </summary>
	public interface IOPCServerPublicGroupsCli
	{
		/// <summary>
		/// No implementation provided!
		/// </summary>
		/// <param name="sName"></param>
		/// <param name="iOPCItemMgtList"></param>
		/// <returns></returns>
		cliHRESULT GetPublicGroupByName(
			/*[in]*/ string sName,
			/*[out]*/ out List<IOPCItemMgtCli> iOPCItemMgtList);
		/// <summary>
		/// No implementation provided!
		/// </summary>
		/// <param name="hServerGroup"></param>
		/// <param name="bForce"></param>
		/// <returns></returns>
		cliHRESULT RemovePublicGroup(
			/*[in]*/ uint hServerGroup,
			/*[in]*/ bool bForce );
	}

	/// <summary>
	/// Enumeration defining constants for the 
	/// OPC DA Servers address space type.
	/// </summary>
	public enum OPCNAMESPACETYPE : ushort
	{
		/// <summary>
		/// The OPC DA Servers address space is 
		/// hierarchical in nature.  Use of a 
		/// tree view recommended.
		/// </summary>
		OPC_NS_HIERARCHIAL = 1,

		/// <summary>
		/// The OPC DA Servers address space is 
		/// flat in nature.  Use of a list view 
		/// recommended.
		/// </summary>
		OPC_NS_FLAT = 2
	}

	/// <summary>
	/// This enumeration defines constants that are 
	/// used to specify the behavior of the browser.
	/// </summary>
	public enum OPCBROWSEDIRECTION : ushort
	{
		/// <summary>
		/// This value is used to cause the browser 
		/// to move up one level in the hierarchy 
		/// from the current position.
		/// </summary>
		OPC_BROWSE_UP = 1,

		/// <summary>
		/// This value is used to cause the browser to 
		/// move down from the current position in the 
		/// hierarchy to the node specified.
		/// </summary>
		OPC_BROWSE_DOWN = 2,

		/// <summary>
		/// This value is used to cause the browser to 
		/// move to the node specified.
		/// </summary>
		OPC_BROWSE_TO = 3
	}

	/// <summary>
	/// This enumeration defines constants that are 
	/// used to specify the behavior of the browser.
	/// </summary>
	public enum OPCBROWSETYPE : ushort
	{
		/// <summary>
		/// This value is used to inform the browser 
		/// that this is a request for branch nodes.
		/// </summary>
		OPC_BRANCH = 1,

		/// <summary>
		/// This value is used to inform the browser 
		/// that this is a request for leaf nodes.
		/// </summary>
		OPC_LEAF = 2,

		/// <summary>
		/// This value is used to inform the browser 
		/// that this is a request to force the 
		/// browser to present a flat view.
		/// </summary>
		OPC_FLAT = 3
	}

	/// <summary>
	/// 
	/// </summary>
	public interface IOPCBrowseServerAddressSpaceCli
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="NameSpaceType"></param>
		/// <returns></returns>
		cliHRESULT QueryOrganization(
			/*[out]*/ out OPCNAMESPACETYPE NameSpaceType );

		/// <summary>
		/// 
		/// </summary>
		/// <param name="dwBrowseDirection"></param>
		/// <param name="sString"></param>
		/// <returns></returns>
		cliHRESULT ChangeBrowsePosition(
			/*[in]*/ OPCBROWSEDIRECTION dwBrowseDirection,
			/*[in]*/ string sString );

		/// <summary>
		/// 
		/// </summary>
		/// <param name="dwBrowseFilterType"></param>
		/// <param name="sFilterCriteria"></param>
		/// <param name="vtDataTypeFilter"></param>
		/// <param name="dwAccessRightsFilter"></param>
		/// <param name="iEnumStrings"></param>
		/// <returns></returns>
		cliHRESULT BrowseOPCItemIDs(
			/*[in]*/ OPCBROWSETYPE dwBrowseFilterType,
			/*[in]*/ string sFilterCriteria,
			/*[in]*/ ushort vtDataTypeFilter,
			/*[in]*/ uint dwAccessRightsFilter,
			/*[out]*/ out cliIEnumString iEnumStrings );

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sItemDataID"></param>
		/// <param name="sItemID"></param>
		/// <returns></returns>
		cliHRESULT GetItemID(
			/*[in]*/ string sItemDataID,
			/*[out]*/ out string sItemID );

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sItemID"></param>
		/// <param name="iEnumStrings"></param>
		/// <returns></returns>
		cliHRESULT BrowseAccessPaths(
			/*[in]*/ string sItemID,
			/*[out]*/ out cliIEnumString iEnumStrings);
	}

	/// <summary>
	/// 
	/// </summary>
	public interface IOPCGroupStateMgtCli
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="dwUpdateRate"></param>
		/// <param name="bActive"></param>
		/// <param name="sName"></param>
		/// <param name="dwTimeBias"></param>
		/// <param name="fPercentDeadband"></param>
		/// <param name="dwLCID"></param>
		/// <param name="hClientGroup"></param>
		/// <param name="hServerGroup"></param>
		/// <returns></returns>
		cliHRESULT GetState(
			/*[out]*/ out uint dwUpdateRate,
			/*[out]*/ out bool bActive,
			/*[out]*/ out string sName,
			/*[out]*/ out int dwTimeBias,
			/*[out]*/ out float fPercentDeadband,
			/*[out]*/ out uint dwLCID,
			/*[out]*/ out uint hClientGroup,
			/*[out]*/ out uint hServerGroup );

		/// <summary>
		/// 
		/// </summary>
		/// <param name="dwRequestedUpdateRate"></param>
		/// <param name="dwRevisedUpdateRate"></param>
		/// <param name="bActive"></param>
		/// <param name="iTimeBias"></param>
		/// <param name="fPercentDeadband"></param>
		/// <param name="dwLCID"></param>
		/// <param name="hClientGroup"></param>
		/// <returns></returns>
		cliHRESULT SetState(
			/*[in]*/ uint dwRequestedUpdateRate,
			/*[out]*/ out uint dwRevisedUpdateRate,
			/*[in]*/ bool bActive,
			/*[in]*/ int iTimeBias,
			/*[in]*/ float fPercentDeadband,
			/*[in]*/ uint dwLCID,
			/*[in]*/ uint hClientGroup );

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sName"></param>
		/// <returns></returns>
		cliHRESULT SetName(
			/*[in]*/ string sName );

		/// <summary>
		/// 
		/// </summary>
		/// <param name="szName"></param>
		/// <param name="iOPCItemMgt"></param>
		/// <returns></returns>
		cliHRESULT CloneGroup(
			/*[in]*/ string szName,
			/*[out]*/ out IOPCItemMgtCli iOPCItemMgt);
	}

	/// <summary>
	/// 
	/// </summary>
	public interface IOPCPublicGroupStateMgtCli
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="bPublic"></param>
		/// <returns></returns>
		cliHRESULT GetState(
			/*[out]*/ out bool bPublic);

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		cliHRESULT MoveToPublic();
	}

	/// <summary>
	/// 
	/// </summary>
	public struct OPC_
	{
		public const ushort QUALITY_MASK              = 0xC0;
		public const ushort STATUS_MASK               = 0xFC;
		public const ushort LIMIT_MASK                = 0x03;

		public const ushort QUALITY_BAD               = 0x00;
		public const ushort QUALITY_UNCERTAIN         = 0x40;
		public const ushort QUALITY_GOOD              = 0xC0;

		public const ushort BAD_NOT_SPECIFIED         = 0x00;
		public const ushort BAD_CONFIG_ERROR          = 0x04;
		public const ushort BAD_NOT_CONNECTED         = 0x08;
		public const ushort BAD_DEVICE_FAILURE        = 0x0C;
		public const ushort BAD_SENSOR_FAILURE        = 0x10;
		public const ushort BAD_LAST_KNOWN            = 0x14;
		public const ushort BAD_COMM_FAILURE          = 0x18;
		public const ushort BAD_OUT_OF_SERVICE        = 0x1C;

		public const ushort UNCERTAIN_NOT_SPECIFIED   = 0x40;
		public const ushort UNCERTAIN_LAST_USABLE     = 0x44;
		public const ushort UNCERTAIN_SENSOR_CAL      = 0x50;
		public const ushort UNCERTAIN_EGU_EXCEEDED    = 0x54;
		public const ushort UNCERTAIN_SUB_NORMAL      = 0x58;

		public const ushort GOOD_NON_SPECIFIC         = 0xC0;
		public const ushort GOOD_LOCAL_OVERRIDE       = 0xD8;

		public const ushort LIMIT_OK                  = 0x00;
		public const ushort LIMIT_LOW                 = 0x01;
		public const ushort LIMIT_HIGH                = 0x02;
		public const ushort LIMIT_CONST               = 0x03;
	}

	/// <summary>
	/// 
	/// </summary>
	public enum OPCDATASOURCE : ushort
	{
		/// <summary>
		/// 
		/// </summary>
		OPC_DS_CACHE = 1,

		/// <summary>
		/// 
		/// </summary>
		OPC_DS_DEVICE = 2
	}

	/// <summary>
	/// 
	/// </summary>
	public interface IOPCSyncIOCli
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="dwSource"></param>
		/// <param name="hServerList"></param>
		/// <param name="ItemValues"></param>
		/// <returns></returns>
		cliHRESULT Read(
			/*[in]*/ OPCDATASOURCE dwSource,
			/*[in]*/ List<uint> hServerList,
			/*[out]*/ out DataValueArraysWithAlias ItemValues);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ItemValues"></param>
		/// <param name="HandleAndHResultList"></param>
		/// <returns></returns>
		cliHRESULT Write(
			/*[in]*/WriteValueArrays ItemValues,
			/*[out]*/ out List<HandleAndHRESULT> HandleAndHResultList );
	}

	/// <summary>
	/// 
	/// </summary>
	public class OPCITEMDEF
	{
		/// <summary>
		/// 
		/// </summary>
		public string sAccessPath;

		/// <summary>
		/// 
		/// </summary>
		public string sItemID;

		/// <summary>
		/// 
		/// </summary>
		public bool bActive;

		/// <summary>
		/// 
		/// </summary>
		public uint hClient;

		/// <summary>
		/// 
		/// </summary>
		public ushort vtRequestedDataType;
	}

	/// <summary>
	/// 
	/// </summary>
	public class OPCITEMRESULT
	{
		/// <summary>
		/// 
		/// </summary>
		public uint hClient;

		/// <summary>
		/// 
		/// </summary>
		public uint hServer;

		/// <summary>
		/// 
		/// </summary>
		public cliHRESULT hResult { get; set; }

		/// <summary>
		/// 
		/// </summary>
		public ushort vtCanonicalDataType;

		/// <summary>
		/// 
		/// </summary>
		public uint dwAccessRights;
	}

	/// <summary>
	/// 
	/// </summary>
	public class HandlePair
	{
		/// <summary>
		/// 
		/// </summary>
		public uint hServer;

		/// <summary>
		/// 
		/// </summary>
		public uint hClient;
	}

	/// <summary>
	/// 
	/// </summary>
	public class HandleDataType
	{
		/// <summary>
		/// 
		/// </summary>
		public uint hServer;

		/// <summary>
		/// 
		/// </summary>
		public ushort wRequestedDatatype;
	}

	/// <summary>
	/// 
	/// </summary>
	public enum OPCEUTYPE : ushort
	{
		/// <summary>
		/// 
		/// </summary>
		OPC_NOENUM = 0,

		/// <summary>
		/// 
		/// </summary>
		OPC_ANALOG = 1,

		/// <summary>
		/// 
		/// </summary>
		OPC_ENUMERATED = 2
	}

	/// <summary>
	/// 
	/// </summary>
	public class OPCITEMATTRIBUTES
	{
		/// <summary>
		/// 
		/// </summary>
		public string szAccessPath;

		/// <summary>
		/// 
		/// </summary>
		public string szItemID;

		/// <summary>
		/// 
		/// </summary>
		public bool bActive;

		/// <summary>
		/// 
		/// </summary>
		public uint hClient;

		/// <summary>
		/// 
		/// </summary>
		public uint hServer;

		/// <summary>
		/// 
		/// </summary>
		public uint dwAccessRights;

		/// <summary>
		/// 
		/// </summary>
		public ushort vtRequestedDataType;

		/// <summary>
		/// 
		/// </summary>
		public ushort vtCanonicalDataType;

		/// <summary>
		/// 
		/// </summary>
		public OPCEUTYPE dwEUType;

		/// <summary>
		/// 
		/// </summary>
		public byte[] vEUInfo;
	}

	/// <summary>
	/// 
	/// </summary>
	public interface IOPCItemMgtCli
		: IOPCGroupStateMgtCli
		, IOPCSyncIOCli
		, IOPCAsyncIO2Cli
		, IDisposable
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="ItemList"></param>
		/// <param name="listAddResults"></param>
		/// <returns></returns>
		cliHRESULT AddItems(
			/*[in]*/ List<OPCITEMDEF> ItemList,
			/*[out]*/ out List<OPCITEMRESULT> listAddResults );

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ItemList"></param>
		/// <param name="listValidationResults"></param>
		/// <returns></returns>
		cliHRESULT ValidateItems(
			/*[in]*/ List<OPCITEMDEF> ItemList,
			/*[out]*/ out List<OPCITEMRESULT> listValidationResults );

		/// <summary>
		/// 
		/// </summary>
		/// <param name="hServerList"></param>
		/// <param name="ErrorsList"></param>
		/// <returns></returns>
		cliHRESULT RemoveItems(
			/*[in]*/ List<uint> hServerList,
			/*[out]*/ out List<HandleAndHRESULT> ErrorsList);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="hServerList"></param>
		/// <param name="bActive"></param>
		/// <param name="ErrorsList"></param>
		/// <returns></returns>
		cliHRESULT SetActiveState(
			/*[in]*/ List<uint> hServerList,
			/*[in]*/ bool bActive,
			/*[out]*/ out List<HandleAndHRESULT> ErrorsList);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="hServer_hClient"></param>
		/// <param name="ErrorsList"></param>
		/// <returns></returns>
		cliHRESULT SetClientHandles(
			/*[in]*/ List<HandlePair> hServer_hClient,
			/*[out]*/ out List<HandleAndHRESULT> ErrorsList);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="hServer_wRequestedDatatype"></param>
		/// <param name="ErrorsList"></param>
		/// <returns></returns>
		cliHRESULT SetDatatypes(
			/*[in]*/ List<HandleDataType> hServer_wRequestedDatatype,
			/*[out]*/ out List<HandleAndHRESULT> ErrorsList);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ItemAttributesList"></param>
		/// <returns></returns>
		cliHRESULT CreateEnumerator(
			/*[out]*/ out List<OPCITEMATTRIBUTES> ItemAttributesList );
	}

	/// <summary>
	/// 
	/// </summary>
	public class OPCDataCallback
	{
		/// <summary>
		/// Xi combined the On Data Change and On Read Complete 
		/// into a single callback.
		/// </summary>
		/// <param name="dwTransid"></param>
		/// <param name="hGroup"></param>
		/// <param name="hrMasterquality"></param>
		/// <param name="hrMastererror"></param>
		/// <param name="hClientItems"></param>
		/// <param name="vDataValues"></param>
		/// <param name="uStatusCode">Xi Defined Status Code</param>
		/// <param name="dtTimeStamps"></param>
		public delegate void OnDataChange(
			/*[in]*/ uint dwTransid,
			/*[in]*/ uint hGroup,
			/*[in]*/ cliHRESULT hrMasterquality,
			/*[in]*/ cliHRESULT hrMastererror,
			/*[in]*/ DataValueArraysWithAlias valueArrays );

		/// <summary>
		/// 
		/// </summary>
		/// <param name="dwTransid"></param>
		/// <param name="hGroup"></param>
		/// <param name="hrMastererr"></param>
		/// <param name="phClientItems"></param>
		/// <param name="uErrors"></param>
		/// <returns></returns>
		public delegate void OnWriteComplete(
			/*[in]*/ uint dwTransid,
			/*[in]*/ uint hGroup,
			/*[in]*/ cliHRESULT hrMastererr,
			/*[in]*/ List<uint> phClientItems,
			/*[in]*/ List<uint> uErrors);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="dwTransid"></param>
		/// <param name="hGroup"></param>
		/// <returns></returns>
		public delegate void OnCancelComplete(
			/*[in]*/ uint dwTransid,
			/*[in]*/ uint hGroup);
	}

	/// <summary>
	/// 
	/// </summary>
	public interface IAdviseOPCDataCallbackCli
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="onDataChange"></param>
		/// <returns></returns>
		cliHRESULT AdviseOnDataChange(
			/*[in]*/ OPCDataCallback.OnDataChange onDataChange);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="onWriteComplete"></param>
		/// <returns></returns>
		cliHRESULT AdviseOnWriteComplete(
			/*[in]*/ OPCDataCallback.OnWriteComplete onWriteComplete);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="onCancelComplete"></param>
		/// <returns></returns>
		cliHRESULT AdviseOnCancelComplete(
			/*[in]*/ OPCDataCallback.OnCancelComplete onCancelComplete);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="onDataChange"></param>
		/// <returns></returns>
		cliHRESULT UnadviseOnDataChange(
			/*[in]*/ OPCDataCallback.OnDataChange onDataChange);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="onWriteComplete"></param>
		/// <returns></returns>
		cliHRESULT UnadviseOnWriteComplete(
			/*[in]*/ OPCDataCallback.OnWriteComplete onWriteComplete);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="onCancelComplete"></param>
		/// <returns></returns>
		cliHRESULT UnadviseOnCancelComplete(
			/*[in]*/ OPCDataCallback.OnCancelComplete onCancelComplete);
	}

	/// <summary>
	/// 
	/// </summary>
	public interface IOPCAsyncIO2Cli
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="hServer"></param>
		/// <param name="dwTransactionID"></param>
		/// <param name="pdwCancelID"></param>
		/// <param name="ErrorsList"></param>
		/// <returns></returns>
		cliHRESULT Read(
			/*[in]*/ List<uint> hServer,
			/*[in]*/ uint dwTransactionID,
			/*[out]*/ out uint pdwCancelID,
			/*[out]*/ out List<HandleAndHRESULT> ErrorsList);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ItemValues"></param>
		/// <param name="dwTransactionID"></param>
		/// <param name="pdwCancelID"></param>
		/// <param name="ErrorsList"></param>
		/// <returns></returns>
		cliHRESULT Write(
			/*[in]*/ WriteValueArrays ItemValues,
			/*[in]*/ uint dwTransactionID,
			/*[out]*/ out uint pdwCancelID,
			/*[out]*/ out List<HandleAndHRESULT> ErrorsList);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="dwSource"></param>
		/// <param name="dwTransactionID"></param>
		/// <param name="dwCancelID"></param>
		/// <returns></returns>
		cliHRESULT Refresh2(
			/*[in]*/ OPCDATASOURCE dwSource,
			/*[in]*/ uint dwTransactionID,
			/*[out]*/ out uint dwCancelID );

		/// <summary>
		/// 
		/// </summary>
		/// <param name="dwCancelID"></param>
		/// <returns></returns>
		cliHRESULT Cancel2(
			/*[in]*/ uint dwCancelID );

		/// <summary>
		/// 
		/// </summary>
		/// <param name="bEnable"></param>
		/// <returns></returns>
		cliHRESULT SetEnable(
			/*[in]*/ bool bEnable );

		/// <summary>
		/// 
		/// </summary>
		/// <param name="bEnable"></param>
		/// <returns></returns>
		cliHRESULT GetEnable(
			/*[out]*/ out bool bEnable );
	}

	/// <summary>
	/// 
	/// </summary>
	public enum OpcDaPropIDs : uint
	{
		DataType = 1,
		ItemValue = 2,
		ItemQuality = 3,
		ItemTimestamp = 4,
		ItemAccessRights = 5,
		ServerScanRate = 6,

		EuUnits = 100,
		ItemDescription = 101,
		HighEU = 102,
		LowEU = 103,
		HighInstrumentRange = 104,
		LowInstrumentRange = 105,
		ContactCloseLabel = 106,
		ContactOpenLabel = 107,
		ItemTimezone = 108,

		DefaultDisplay = 200,
		CurrentForegroundColor = 201,
		CurrentBackgroundColor = 202,
		CurrentBlink = 203,
		BmpFile = 204,
		SoundFile = 205,
		HtmlFile = 206,
		AviFile = 207,

		ConditionStatus = 300,
		AlarmQuickHelp = 301,
		AlarmAreaList = 302,
		PrimaryAlarmArea = 303,
		ConditionLogic = 304,
		LimitExceeded = 305,
		Deadband = 306,
		HiHiLimit = 307,
		HiLimit = 308,
		LoLimit = 309,
		LoLoLimit = 310,
		RateOfChangeLimit = 311,
		DeviationLimit = 312,

	}

	/// <summary>
	/// 
	/// </summary>
	[Flags]
	public enum OpcDaAccessRights
	{
		Readable = 0x01,
		Writable = 0x02
	}
	
	/// <summary>
	/// 
	/// </summary>
	public class ItemProperty
	{
		public uint PropertyID;
		public string Description;
		public ushort PropDataType;
	}

	/// <summary>
	/// 
	/// </summary>
	public class PropertyValue
	{
		public cliHRESULT hResult;
		public cliVARIANT vDataValue;
	}

	/// <summary>
	/// 
	/// </summary>
	public class PropertyItemID
	{
		public cliHRESULT hResult;
		public string ItemID;
	}

	/// <summary>
	/// 
	/// </summary>
	public interface IOPCItemPropertiesCli
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="sItemID"></param>
		/// <param name="ItemProperties"></param>
		/// <returns></returns>
		cliHRESULT QueryAvailableProperties(
			/*[in]*/ string sItemID,
			/*[out]*/ out List<ItemProperty> ItemProperties );

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sItemID"></param>
		/// <param name="listPropertyIDs"></param>
		/// <param name="listPropertyValues"></param>
		/// <returns></returns>
		cliHRESULT GetItemProperties(
			/*[in]*/ string sItemID,
			/*[in]*/ List<uint> listPropertyIDs,
			/*[out]*/ out List<PropertyValue> listPropertyValues );

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sItemID"></param>
		/// <param name="listPropertyIDs"></param>
		/// <param name="listPropertyItemIDs"></param>
		/// <returns></returns>
		cliHRESULT LookupItemIDs(
			/*[in]*/ string sItemID,
			/*[in]*/ List<uint> listPropertyIDs,
			/*[out]*/ out List<PropertyItemID> listPropertyItemIDs );
	}
}
