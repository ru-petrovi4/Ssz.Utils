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

#include <vcclr.h>

using namespace Xi::OPC::COM::API;

namespace Xi {
namespace OPC {
namespace COM {
namespace Impl {

	ref class COPCAeSubscription;
	ref class COPCAeServer;

	class ATL_NO_VTABLE COPCAeEventSink
		: public ::CComObjectRootEx<::CComMultiThreadModel>
		, public ::CComCoClass<COPCAeEventSink>
		, public ::IOPCEventSink
	{
	public:
		COPCAeEventSink(void);
		virtual ~COPCAeEventSink(void);

		DECLARE_NOT_AGGREGATABLE(COPCAeEventSink)
		DECLARE_GET_CONTROLLING_UNKNOWN()

		DECLARE_PROTECT_FINAL_CONSTRUCT()

		BEGIN_COM_MAP(COPCAeEventSink)
			COM_INTERFACE_ENTRY(::IOPCEventSink)
			COM_INTERFACE_ENTRY_AGGREGATE(::IID_IMarshal, m_pUnkMarshaler.p)
		END_COM_MAP()

		HRESULT FinalConstruct()
		{
			HRESULT hr = ::CoCreateFreeThreadedMarshaler( GetControllingUnknown(), &m_pUnkMarshaler.p);
			return hr;
		}

		void FinalRelease()
		{
			if (nullptr != m_pUnkMarshaler)
				m_pUnkMarshaler.Release();
			m_pUnkMarshaler = nullptr;
		}

		CComPtr<IUnknown> m_pUnkMarshaler;

		gcroot<COPCAeServer^> AeServer;
	public:
		// IOPCEventSink
		STDMETHOD(OnEvent)(unsigned long hClientSubscription, long bRefresh,
			long bLastRefresh, unsigned long dwCount, ONEVENTSTRUCT * pEvents );
	};

}}}}
