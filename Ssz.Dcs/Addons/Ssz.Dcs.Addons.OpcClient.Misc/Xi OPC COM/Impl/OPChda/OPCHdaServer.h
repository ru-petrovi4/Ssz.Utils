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

	public ref class COPCHdaServer
		: public IDisposable
		, public IOPCCommonCli
		, public IOPCHDA_ServerCli
		, public IOPCHDA_SyncReadCli
		, public IAdviseOPCShutdownCli
	{
	public:
		COPCHdaServer(cliHRESULT %hr, ::IOPCHDA_Server * pIOPCHDA_Server);
		~COPCHdaServer(void);

	private:
		!COPCHdaServer(void);
		bool DisposeThis(bool isDisposing);

	public:
		property ::ServerDescription^ ServerDescription
		{
			void set(::ServerDescription^ serverDescription) { m_serverDescription = serverDescription; }
			::ServerDescription^ get() { return m_serverDescription; }
		}

	private:
		property ::IOPCHDA_Server* IOPCHDA_Server 
		{
		  ::IOPCHDA_Server* get() { return m_pIOPCHDA_Server; }
		}

		property ::IOPCCommon* IOPCCommon 
		{
		  ::IOPCCommon* get() { return m_pIOPCCommon; }
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
			/*[in]*/ String^ zName );

		// IOPCHDA_Server
		virtual cliHRESULT GetItemAttributes( 
			/*[out]*/ List<OPCHDAITEMATTR^>^ %HDAItemAttributes);
		virtual cliHRESULT GetAggregates( 
			/*[out]*/ List<OPCHDAAGGREGATES^>^ %HDAAggregates);
		virtual cliHRESULT GetHistorianStatus( 
			/*[out]*/ OPCHDA_SERVERSTATUS %wStatus,
			/*[out]*/ DateTime %dtCurrentTime,
			/*[out]*/ DateTime %dtStartTime,
			/*[out]*/ unsigned short %wMajorVersion,
			/*[out]*/ unsigned short %wMinorVersion,
			/*[out]*/ unsigned short %wBuildNumber,
			/*[out]*/ unsigned int %dwMaxReturnValues,
			/*[out]*/ String^ %sStatusString,
			/*[out]*/ String^ %sVendorInfo);
		virtual cliHRESULT GetItemHandles( 
			/*[in]*/ List<OPCHDA_ITEMDEF^>^ hClientAndItemID,
			/*[out]*/ List<OPCHDAITEMRESULT^>^ %hServerAndHResult);
		virtual cliHRESULT ReleaseItemHandles( 
			/*[in]*/ List<unsigned int>^ hServer,
			/*[out]*/ List<HandleAndHRESULT^>^ %ErrorsList);
		virtual cliHRESULT ValidateItemIDs( 
			/*[in]*/ List<String^>^ sItemID,
			/*[out]*/ List<HandleAndHRESULT^>^ %ErrorsList);
		virtual cliHRESULT CreateBrowse( 
			/*[in]*/ List<OPCHDA_BROWSEFILTER^>^ BrowseFilters,
			/*[out]*/ IOPCHDA_BrowserCli^ %iBrowser,
			/*[out]*/ List<HandleAndHRESULT^>^ %ErrorsList);

		// IOPCHDA_SyncRead
		virtual cliHRESULT ReadRaw( 
			/*[in,out]*/ OPCHDA_TIME^ %cliStartTime,
			/*[in,out]*/ OPCHDA_TIME^ %cliEndTime,
			/*[in]*/ unsigned int dwNumValues,
			/*[in]*/ bool bBounds,
			/*[in]*/ List<unsigned int>^ hServer,
			/*[out]*/ array<JournalDataValues^>^ %ItemValues);
		virtual cliHRESULT ReadProcessed( 
			/*[in,out]*/ OPCHDA_TIME^ %cliStartTime,
			/*[in,out]*/ OPCHDA_TIME^ %cliEndTime,
			/*[in]*/ TimeSpan dtResampleInterval,
			/*[in]*/ List<OPCHDA_HANDLEAGGREGATE^>^ HandleAggregate,
			/*[out]*/ array<JournalDataValues^>^ %ItemValues);
		virtual cliHRESULT ReadAtTime( 
			/*[in]*/ List<DateTime>^ dtTimeStamps,
			/*[in]*/ List<unsigned int>^ hServer,
			/*[out]*/ array<JournalDataValues^>^ %ItemValues);
		virtual cliHRESULT ReadModified( 
			/*[in,out]*/ OPCHDA_TIME^ %cliStartTime,
			/*[in,out]*/ OPCHDA_TIME^ %cliEndTime,
			/*[in]*/ unsigned int dwNumValues,
			/*[in]*/ List<unsigned int>^ hServer,
			/*[out]*/ array<JournalDataChangedValues^>^ %ItemValues);
		virtual cliHRESULT ReadAttribute( 
			/*[in,out]*/ OPCHDA_TIME^ %cliStartTime,
			/*[in,out]*/ OPCHDA_TIME^ %cliEndTime,
			/*[in]*/ unsigned int hServer,
			/*[in]*/ List<unsigned int>^ dwAttributeIDs,
			/*[out]*/ array<JournalDataPropertyValue^>^ %AttributeValues);

		// IAdviseOPCShutdownCli
		virtual cliHRESULT AdviseShutdownRequest(
			/*[in]*/ OPCShutdown::ShutdownRequest^ shutdownRequest);
		virtual cliHRESULT UnadviseShutdownRequest(
			/*[in]*/ OPCShutdown::ShutdownRequest^ shutdownRequest);

	private:
		unsigned int ConvertToJournalDataValues(
			DateTime startTime, DateTime endTime, 
			unsigned long dwNumItems,
			tagOPCHDA_ITEM *pItemValues,
			unsigned long * typeId_LocalId,
			HRESULT *pHR,
			array<JournalDataValues^>^ %itemValues);

	internal:
		OPCShutdown::ShutdownRequest^ m_shutdownRequest;

	private:
		bool m_bHasBeenDisposed;
		::IOPCHDA_Server * m_pIOPCHDA_Server;
		::IOPCCommon * m_pIOPCCommon;
		::IOPCHDA_SyncRead * m_pIOPCHDA_SyncRead;

		::IOPCShutdown * m_pIOPCShutdown;
		System::Runtime::InteropServices::GCHandle m_gcHandleTHIS;
		unsigned int m_dwShutdownAdviseCookie;

		::ServerDescription^ m_serverDescription;
	};

}}}}
