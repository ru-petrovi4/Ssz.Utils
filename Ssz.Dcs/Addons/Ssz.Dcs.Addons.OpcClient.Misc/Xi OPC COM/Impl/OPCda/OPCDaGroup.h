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

	ref class COPCDaServer;
	class COPCDaDataCallback;

	public ref class COPCDaGroup
		: public IDisposable
		, public IOPCGroupStateMgtCli
		, public IOPCItemMgtCli
		, public IOPCSyncIOCli
		, public IOPCAsyncIO2Cli
	{
	public:
		COPCDaGroup(cliHRESULT %HR, ::IOPCItemMgt *pIOPCItemMgt,
			COPCDaServer^ OPCServer, unsigned int hClientGroup, unsigned int hServerGroup);
		~COPCDaGroup(void);

	private:
		!COPCDaGroup(void);
	internal:
		bool DisposeThis(bool isDisposing);

	public:
		property ::IOPCItemMgt* IOPCItemMgt
		{
			::IOPCItemMgt* get() { return m_pIOPCItemMgt; }
		}
		property ::IOPCGroupStateMgt* IOPCGroupStateMgt
		{
			::IOPCGroupStateMgt* get() { return m_pIOPCGroupStateMgt; }
		}
		property ::IOPCSyncIO* IOPCSyncIO
		{
			::IOPCSyncIO* get() { return m_pIOPCSyncIO; }
		}
		property ::IOPCAsyncIO2* IOPCAsyncIO2
		{
			::IOPCAsyncIO2* get() { return m_pIOPCAsyncIO2; }
		}

	public:
		// IOPCGroupStateMgtCli
		virtual cliHRESULT GetState(
			/*[out]*/ unsigned int %dwUpdateRate,
			/*[out]*/ bool %bActive,
			/*[out]*/ String^ %sName,
			/*[out]*/ int %dwTimeBias,
			/*[out]*/ float %fPercentDeadband,
			/*[out]*/ unsigned int %dwLCID,
			/*[out]*/ unsigned int %hClientGroup,
			/*[out]*/ unsigned int %hServerGroup );
		virtual cliHRESULT SetState(
			/*[in]*/ unsigned int dwRequestedUpdateRate,
			/*[out]*/ unsigned int %dwRevisedUpdateRate,
			/*[in]*/ bool bActive,
			/*[in]*/ int iTimeBias,
			/*[in]*/ float fPercentDeadband,
			/*[in]*/ unsigned int dwLCID,
			/*[in]*/ unsigned int hClientGroup );
		virtual cliHRESULT SetName(
			/*[in]*/ String^ sName );
		virtual cliHRESULT CloneGroup(
			/*[in]*/ String^ szName,
			/*[out]*/ IOPCItemMgtCli^ %iOPCItemMgt);

		// IOPCItemMgt
		virtual cliHRESULT AddItems(
			/*[in]*/ List<OPCITEMDEF^>^ ItemList,
			/*[out]*/ List<OPCITEMRESULT^>^ %lstAddResults );
		virtual cliHRESULT ValidateItems(
			/*[in]*/ List<OPCITEMDEF^>^ ItemList,
			/*[out]*/ List<OPCITEMRESULT^>^ %lstValidationResults );
		virtual cliHRESULT RemoveItems(
			/*[in]*/ List<unsigned int>^ hServerList,
			/*[out]*/ List<HandleAndHRESULT^>^ %ErrorsList );
		virtual cliHRESULT SetActiveState(
			/*[in]*/ List<unsigned int>^ hServerList,
			/*[in]*/ bool bActive,
			/*[out]*/ List<HandleAndHRESULT^>^ %ErrorsList );
		virtual cliHRESULT SetClientHandles(
			/*[in]*/ List<HandlePair^>^ hServer_hClient,
			/*[out]*/ List<HandleAndHRESULT^>^ %ErrorsList );
		virtual cliHRESULT SetDatatypes(
			/*[in]*/ List<HandleDataType^>^ hServer_wRequestedDatatype,
			/*[out]*/ List<HandleAndHRESULT^>^ %ErrorsList );
		virtual cliHRESULT CreateEnumerator(
			/*[out]*/ List<OPCITEMATTRIBUTES^>^ %ItemAttributesList );

		// IOPCSyncIO
		virtual cliHRESULT Read(
			/*[in]*/ OPCDATASOURCE dwSource,
			/*[in]*/ List<unsigned int>^ hServerList,
			/*[out]*/ DataValueArraysWithAlias^ %ItemValues );
		virtual cliHRESULT Write(
			/*[in]*/ WriteValueArrays^ ItemValues,
			/*[out]*/ List<HandleAndHRESULT^>^ %HandleAndHResultList );

		// IOPCAsyncIO2
		virtual cliHRESULT Read(
			/*[in]*/ List<unsigned int>^ hServerList,
			/*[in]*/ unsigned int dwTransactionID,
			/*[out]*/ unsigned int %dwCancelID,
			/*[out]*/ List<HandleAndHRESULT^>^ %ErrorsList );
		virtual cliHRESULT Write(
			/*[in]*/ WriteValueArrays^ ItemValues,
			/*[in]*/ unsigned int dwTransactionID,
			/*[out]*/ unsigned int %pdwCancelID,
			/*[out]*/ List<HandleAndHRESULT^>^ %ErrorsList );
		virtual cliHRESULT Refresh2(
			/*[in]*/ OPCDATASOURCE dwSource,
			/*[in]*/ unsigned int dwTransactionID,
			/*[out]*/ unsigned int %dwCancelID );
		virtual cliHRESULT Cancel2(
			/*[in]*/ unsigned int dwCancelID );
		virtual cliHRESULT SetEnable(
			/*[in]*/ bool bEnable );
		virtual cliHRESULT GetEnable(
			/*[out]*/ bool %bEnable );

	private:
		bool m_bHasBeenDisposed;
		::IOPCItemMgt * m_pIOPCItemMgt;
		::IOPCGroupStateMgt * m_pIOPCGroupStateMgt;
		::IOPCSyncIO * m_pIOPCSyncIO;
		::IOPCAsyncIO2 * m_pIOPCAsyncIO2;
		unsigned int m_dwAdviseCookie;

		COPCDaServer^ m_OPCServer;
		unsigned int m_hClientGroup;
		unsigned int m_hServerGroup;
		// Used to obtain the client handle given the server handle
		Dictionary<unsigned int, unsigned int>^ m_hServerTohClient;
	};

}}}}
