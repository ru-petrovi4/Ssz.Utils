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
#include "OPCAeAreaBrowser.h"
#include "OPCAeSubscription.h"

using namespace System;
using namespace System::Collections::Generic;

using namespace Xi::OPC::COM::API;
using namespace Xi::Contracts::Data;

namespace Xi {
namespace OPC {
namespace COM {
namespace Impl {

	public ref class COPCAeServer
		: public IDisposable
		, public IOPCCommonCli
		, public IOPCEventServerCli
		, public IAdviseOPCShutdownCli
	{
	public:
		COPCAeServer(cliHRESULT &hr, ::IOPCEventServer * pIOPCEventServer);
		~COPCAeServer(void);

	private:
		!COPCAeServer(void);
		bool DisposeThis(bool isDisposing);

	public:
		property ::ServerDescription^ ServerDescription
		{
			void set(::ServerDescription^ serverDescription) { m_serverDescription = serverDescription; }
			::ServerDescription^ get() { return m_serverDescription; }
		}

	private:
		property ::IOPCEventServer* IOPCEventServer 
		{
		  ::IOPCEventServer* get() { return m_pIOPCEventServer; }
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

		// IOPCEventServer
		virtual cliHRESULT GetStatus (
			/*[out]*/ cliOPCEVENTSERVERSTATUS^ %EventServerStatus );
		virtual cliHRESULT CreateEventSubscription (
			/*[in]*/ bool bActive,
			/*[in]*/ unsigned int dwBufferTime,
			/*[in]*/ unsigned int dwMaxSize,
			/*[in]*/ unsigned int hClientSubscription,
			/*[out]*/ IOPCEventSubscriptionMgtCli^ %iOPCEventSubscriptionMgt,
			/*[out]*/ unsigned int %dwRevisedBufferTime,
			/*[out]*/ unsigned int %dwRevisedMaxSize );
		virtual cliHRESULT QueryAvailableFilters (
			/*[out]*/ unsigned int %dwFilterMask );
		virtual cliHRESULT QueryEventCategories (
			/*[in]*/ unsigned int dwEventType,
			/*[out]*/ List<OPCEVENTCATEGORY^>^ %EventCategories );
		virtual cliHRESULT QueryConditionNames (
			/*[in]*/ unsigned int dwEventCategory,
			/*[out]*/ List<String^>^ %ConditionNames );
		virtual cliHRESULT QuerySubConditionNames (
			/*[in]*/ String^ sConditionName,
			/*[out]*/ List<String^>^ %SubConditionNames );
		virtual cliHRESULT QuerySourceConditions (
			/*[in]*/ String^ sSource,
			/*[out]*/ List<String^>^ %ConditionNames );
		virtual cliHRESULT QueryEventAttributes (
			/*[in]*/ unsigned int dwEventCategory,
			/*[out]*/ List<OPCEVENTATTRIBUTE^>^ %EventCategories );
		virtual cliHRESULT TranslateToItemIDs (
			/*[in]*/ String^ sSource,
			/*[in]*/ unsigned int dwEventCategory,
			/*[in]*/ String^ sConditionName,
			/*[in]*/ String^ sSubconditionName,
			/*[in]*/ List<unsigned int>^ dwAssocAttrIDs,
			/*[out]*/ List<OPCEVENTITEMID^>^ %EventItemIDs );
		virtual cliHRESULT GetConditionState (
			/*[in]*/ String^ sSource,
			/*[in]*/ String^ sConditionName,
			/*[in]*/ List<unsigned int>^ AttributeIDs,
			/*[out]*/ List<cliOPCCONDITIONSTATE^>^ %ConditionStates );
		virtual cliHRESULT EnableConditionByArea (
			/*[in]*/ List<String^>^ Areas );
		virtual cliHRESULT EnableConditionBySource (
			/*[in]*/ List<String^>^ Sources );
		virtual cliHRESULT DisableConditionByArea (
			/*[in]*/ List<String^>^ Areas );
		virtual cliHRESULT DisableConditionBySource (
			/*[in]*/ List<String^>^ Sources );
		virtual cliHRESULT AckCondition (
			/*[in]*/ String^ sAcknowledgerID,
			/*[in]*/ String^ sComment,
			/*[in]*/ List<OPCEVENTACKCONDITION^>^ AckConditions,
			/*[out]*/ List<HandleAndHRESULT^>^ %ErrorList );
		virtual cliHRESULT CreateAreaBrowser (
			/*[out]*/ IOPCEventAreaBrowserCli^ %iOPCEventAreaBrowser);

		// IAdviseOPCShutdownCli
		virtual cliHRESULT AdviseShutdownRequest(
			/*[in]*/ OPCShutdown::ShutdownRequest^ shutdownRequest);
		virtual cliHRESULT UnadviseShutdownRequest(
			/*[in]*/ OPCShutdown::ShutdownRequest^ shutdownRequest);

		inline void AddToDictionary(unsigned int key,  COPCAeSubscription^ opcAeSubscription)
		{
			_ASSERT(!m_OPCAeSubscriptions->ContainsKey(key));
			m_OPCAeSubscriptions->Add(key, opcAeSubscription);
		}

		inline COPCAeSubscription^ FindUsingKey(unsigned int key) {
			COPCAeSubscription^ opcAeSubscription = nullptr;
			m_OPCAeSubscriptions->TryGetValue(key, opcAeSubscription);
			return opcAeSubscription;
		}

		inline void RemoveFromDictionary(unsigned int key)
		{
			m_OPCAeSubscriptions->Remove(key);
		}

	internal:
		OPCShutdown::ShutdownRequest^ m_shutdownRequest;

	private:
		Dictionary<unsigned int, COPCAeSubscription^>^ m_OPCAeSubscriptions;

		bool m_bHasBeenDisposed;

		::IOPCEventServer * m_pIOPCEventServer;
		::IOPCCommon * m_pIOPCCommon;

		::IOPCEventSink * m_pIOPCEventSink;		// For the IConnectionPoint -- OnEvent
		COPCAeEventSink * m_pCOPCAeEventSink;

		::IOPCShutdown * m_pIOPCShutdown;
		System::Runtime::InteropServices::GCHandle m_gcHandleTHIS;
		unsigned int m_dwShutdownAdviseCookie;

		::ServerDescription^ m_serverDescription;
	};

}}}}
