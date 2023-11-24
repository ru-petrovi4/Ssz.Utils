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

#pragma once

using namespace System;
using namespace System::Collections::Generic;

using namespace Xi::OPC::COM::API;
using namespace Xi::Contracts::Data;

namespace Xi {
namespace OPC {
namespace COM {
namespace Impl {

	ref class COPCDaGroup;

	public ref class COPCDaServer
		: public IDisposable
		, public IOPCCommonCli
		, public IOPCServerCli
		, public IOPCBrowseServerAddressSpaceCli
		, public IOPCItemPropertiesCli
		, public IAdviseOPCDataCallbackCli
		, public IAdviseOPCShutdownCli
	{
	public:
		COPCDaServer(cliHRESULT &hr, ::IOPCServer * pIOPCServer);
		~COPCDaServer(void);

	private:
		!COPCDaServer(void);
		bool DisposeThis(bool isDisposing);

	public:
		property ::ServerDescription^ ServerDescription
		{
			void set(::ServerDescription^ serverDescription) { m_serverDescription = serverDescription; }
			::ServerDescription^ get() { return m_serverDescription; }
		}

		property ::IOPCDataCallback * IOPCDataCallback
		{
			::IOPCDataCallback* get() { return m_pIOPCDataCallback; }
		}

	private:
		property ::IOPCServer* IOPCServer 
		{
		  ::IOPCServer* get() { return m_pIOPCServer; }
		}
		property ::IOPCCommon* IOPCCommon 
		{
		  ::IOPCCommon* get() { return m_pIOPCCommon; }
		}
		property ::IOPCBrowseServerAddressSpace* IOPCBrowseServerAddressSpace
		{
			::IOPCBrowseServerAddressSpace* get() { return m_pIOPCBrowseServerAddressSpace; }
		}
		property ::IOPCItemProperties* IOPCItemProperties
		{
			::IOPCItemProperties* get() { return m_pIOPCItemProperties; }
		}

	public:
		// IOPCCommon
		virtual cliHRESULT SetLocaleID(
			/*[in]*/ unsigned int dwLcid );
		virtual cliHRESULT GetLocaleID(
			/*[out]*/ unsigned int %dwLcid );
		virtual cliHRESULT QueryAvailableLocaleIDs(
			/*[out]*/ List<unsigned int>^ %dwLcid );
		virtual cliHRESULT GetErrorString(
			/*[in]*/ cliHRESULT dwError,
			/*[out]*/ String^ %errString );
		virtual cliHRESULT SetClientName(
			/*[in]*/ String^ sName );

		// IOPCServer
		virtual cliHRESULT AddGroup(
			/*[in]*/ String^ sName,
			/*[in]*/ bool bActive,
			/*[in]*/ unsigned int dwRequestedUpdateRate,
			/*[in]*/ unsigned int hClientGroup,
			/*[in]*/ Nullable<int> iTimeBias,
			/*[in]*/ Nullable<float> fPercentDeadband,
			/*[in]*/ unsigned int dwLCID,
			///*[out]*/ unsigned int %hServerGroup,
			/*[out]*/ unsigned int %dwRevisedUpdateRate,
			/*[out]*/ IOPCItemMgtCli^ %iOPCItemMgt );
		virtual cliHRESULT GetErrorString(
			/*[in]*/ cliHRESULT dwError,
			/*[in]*/ unsigned int dwLocale,
			/*[out]*/ String^ %sErrString );
		virtual cliHRESULT GetGroupByName(
			/*[in]*/ String^ sName,
			/*[out]*/ IOPCItemMgtCli^ %iOPCItemMgt );
		virtual cliHRESULT GetStatus(
			/*[out]*/ OPCSERVERSTATUS^ %ServerStatus );
		virtual cliHRESULT RemoveGroup(
			/*[in]*/ unsigned int hServerGroup,
			/*[in]*/ bool bForce );
		virtual cliHRESULT CreateGroupEnumerator(
			/*[in]*/ OPCENUMSCOPE dwScope,
			/*[out]*/ List<IOPCItemMgtCli^>^ %iOPCItemMgtList );

		// IOPCBrowseServerAddressSpace
		virtual cliHRESULT QueryOrganization(
			/*[out]*/ OPCNAMESPACETYPE %NameSpaceType );
		virtual cliHRESULT ChangeBrowsePosition(
			/*[in]*/ OPCBROWSEDIRECTION dwBrowseDirection,
			/*[in]*/ String^ sString );
		virtual cliHRESULT BrowseOPCItemIDs(
			/*[in]*/ OPCBROWSETYPE dwBrowseFilterType,
			/*[in]*/ String^ sFilterCriteria,
			/*[in]*/ unsigned short vtDataTypeFilter,
			/*[in]*/ unsigned int dwAccessRightsFilter,
			/*[out]*/ cliIEnumString^ %iEnumStrings );
		virtual cliHRESULT GetItemID(
			/*[in]*/ String^ sItemDataID,
			/*[out]*/ String^ %sItemID );
		virtual cliHRESULT BrowseAccessPaths(
			/*[in]*/ String^ sItemID,
			/*[out]*/ cliIEnumString^ %iEnumStrings );

		// IOPCItemProperties
		virtual cliHRESULT QueryAvailableProperties(
			/*[in]*/ String^ sItemID,
			/*[out]*/ List<ItemProperty^>^ %lstItemProperties );
		virtual cliHRESULT GetItemProperties(
			/*[in]*/ String^ sItemID,
			/*[in]*/ List<unsigned int>^ listPropertyIDs,
			/*[out]*/ List<PropertyValue^>^ %lstPropertyValues );
		virtual cliHRESULT LookupItemIDs(
			/*[in]*/ String^ sItemID,
			/*[in]*/ List<unsigned int>^ listPropertyIDs,
			/*[out]*/ List<PropertyItemID^>^ %lstPropertyItemIDs );

		// IAdviseOPCDataCallbackCli
		virtual cliHRESULT AdviseOnDataChange(
			/*[in]*/ OPCDataCallback::OnDataChange^ onDataChange);
		virtual cliHRESULT AdviseOnWriteComplete(
			/*[in]*/ OPCDataCallback::OnWriteComplete^ onWriteComplete);
		virtual cliHRESULT AdviseOnCancelComplete(
			/*[in]*/ OPCDataCallback::OnCancelComplete^ onCancelComplete);

		virtual cliHRESULT UnadviseOnDataChange(
			/*[in]*/ OPCDataCallback::OnDataChange^ onDataChange);
		virtual cliHRESULT UnadviseOnWriteComplete(
			/*[in]*/ OPCDataCallback::OnWriteComplete^ onWriteComplete);
		virtual cliHRESULT UnadviseOnCancelComplete(
			/*[in]*/ OPCDataCallback::OnCancelComplete^ onCancelComplete);

		// IAdviseOPCShutdownCli
		virtual cliHRESULT AdviseShutdownRequest(
			/*[in]*/ OPCShutdown::ShutdownRequest^ shutdownRequest);
		virtual cliHRESULT UnadviseShutdownRequest(
			/*[in]*/ OPCShutdown::ShutdownRequest^ shutdownRequest);

	internal:
		cliHRESULT RemoveGroupInternal(unsigned int hServerGroup);

		// internal to allow the OPCDaDataCallback class to access directly
		OPCDataCallback::OnDataChange^ m_onDataChange;

		OPCShutdown::ShutdownRequest^ m_shutdownRequest;

	private:
		bool m_bHasBeenDisposed;

		Dictionary<unsigned int, COPCDaGroup^>^ m_OPCDaGroups;
		::IOPCServer * m_pIOPCServer;
		::IOPCCommon * m_pIOPCCommon;
		::IOPCBrowseServerAddressSpace * m_pIOPCBrowseServerAddressSpace;
		::IOPCItemProperties * m_pIOPCItemProperties;
		::IOPCDataCallback * m_pIOPCDataCallback;
		::IOPCShutdown * m_pIOPCShutdown;
		System::Runtime::InteropServices::GCHandle m_gcHandleTHIS;
		unsigned int m_dwShutdownAdviseCookie;

		::ServerDescription^ m_serverDescription;
	};
}}}}
