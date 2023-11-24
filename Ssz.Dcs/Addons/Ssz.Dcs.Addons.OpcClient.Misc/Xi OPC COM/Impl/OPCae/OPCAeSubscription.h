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
using namespace System::Threading;
using namespace System::Runtime::InteropServices;

using namespace Xi::OPC::COM::API;

namespace Xi {
namespace OPC {
namespace COM {
namespace Impl {

	ref class COPCAeServer;
	class COPCAeEventSink;

	public ref class COPCAeSubscription
		: public IDisposable
		, public IOPCEventSubscriptionMgtCli
		, public IAdviseOPCEventSink
	{
	public:
		COPCAeSubscription(cliHRESULT %hr, 
			COPCAeServer^ rCOPCAeServer, 
			unsigned int hClientSubscription,
			IOPCEventSubscriptionMgt* pOpcAeSubscription,
			::IOPCEventSink * pIOPCEventSink);
		~COPCAeSubscription(void);

	private:
		!COPCAeSubscription();
		bool DisposeThis(bool isDisposing);

	public:
		property COPCAeServer^ OPCAeServer
		{
			COPCAeServer^ get() { return m_rCOPCAeServer; }
		}

	public:
		// IOPCEventSubscriptionMgt
		virtual cliHRESULT SetFilter (
			/*[in]*/ unsigned int dwEventType,
			/*[in]*/ List<unsigned int>^ EventCategories,
			/*[in]*/ unsigned int dwLowSeverity,
			/*[in]*/ unsigned int dwHighSeverity,
			/*[in]*/ List<String^>^ AreaList,
			/*[in]*/ List<String^>^ SourceList );
		virtual cliHRESULT GetFilter (
			/*[out]*/ unsigned int %dwEventType,
			/*[out]*/ List<unsigned int>^ %EventCategories,
			/*[out]*/ unsigned int %dwLowSeverity,
			/*[out]*/ unsigned int %dwHighSeverity,
			/*[out]*/ List<String^>^ %AreaList,
			/*[out]*/ List<String^>^ %sSourceList );
		virtual cliHRESULT SelectReturnedAttributes (
			/*[in]*/ unsigned int dwEventCategory,
			/*[in]*/ List<unsigned int>^ AttributeIDs );
		virtual cliHRESULT GetReturnedAttributes (
			/*[in]*/ unsigned int dwEventCategory,
			/*[out]*/ List<unsigned int>^ %AttributeIDs );
		virtual cliHRESULT Refresh (
			/*[in]*/ unsigned int dwConnection );
		virtual cliHRESULT CancelRefresh (
			/*[in]*/ unsigned int dwConnection );
		virtual cliHRESULT GetState (
			/*[out]*/ bool %bActive,
			/*[out]*/ unsigned int %dwBufferTime,
			/*[out]*/ unsigned int %dwMaxSize,
			/*[out]*/ unsigned int %hClientSubscription );
		virtual cliHRESULT SetState (
			/*[in]*/ Nullable<bool> bActive,
			/*[in]*/ Nullable<unsigned int> dwBufferTime,
			/*[in]*/ Nullable<unsigned int> dwMaxSize,
			/*[in]*/ unsigned int hClientSubscription,
			/*[out]*/ unsigned int %dwRevisedBufferTime,
			/*[out]*/ unsigned int %dwRevisedMaxSize );

		// IAdviseOPCEventSink
		virtual cliHRESULT AdviseOnEvent(
			/*[in]*/ OPCEventSink::OnEvent^ onEvent);

		virtual cliHRESULT UnadviseOnEvent(
			/*[in]*/ OPCEventSink::OnEvent^ onEvent);

		// public to allow the OPCAeEventSink class to access directly
		OPCEventSink::OnEvent^ m_onEvent;

	private:
		bool m_bHasBeenDisposed;
		unsigned int m_hClientSubscription;
		COPCAeServer^ m_rCOPCAeServer;
		::IOPCEventSubscriptionMgt * m_pIOPCEventSubMgt;
		::IOPCEventSink * m_pIOPCEventSink;
		unsigned int m_dwAdviseCookie;
	};

}}}}
