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

using namespace Xi::OPC::COM::API;

namespace Xi {
namespace OPC {
namespace COM {
namespace Impl {

	class ATL_NO_VTABLE COPCAeShutdownCallback
		: public ::CComObjectRootEx<::CComMultiThreadModel>
		, public ::CComCoClass<COPCAeShutdownCallback>
		, public ::IOPCShutdown
	{
	public:
		COPCAeShutdownCallback(void);
		virtual ~COPCAeShutdownCallback(void);

		DECLARE_NOT_AGGREGATABLE(COPCAeShutdownCallback)
		DECLARE_GET_CONTROLLING_UNKNOWN()

		DECLARE_PROTECT_FINAL_CONSTRUCT()

		BEGIN_COM_MAP(COPCAeShutdownCallback)
			COM_INTERFACE_ENTRY(::IOPCShutdown)
			COM_INTERFACE_ENTRY_AGGREGATE(::IID_IMarshal, m_pUnkMarshaler.p)
		END_COM_MAP()

		HRESULT FinalConstruct()
		{
			HRESULT hr = ::CoCreateFreeThreadedMarshaler( GetControllingUnknown(), &m_pUnkMarshaler.p);
			return hr;
		}

		void FinalRelease()
		{
			m_pUnkMarshaler.Release();
		}

		CComPtr<IUnknown> m_pUnkMarshaler;

		inline void SetOpcServerGCHandle(System::Runtime::InteropServices::GCHandle opcAeServer)
		{
			m_opcAeServer = opcAeServer;
		}

	public:
		// IOPCShutdown
		STDMETHOD(ShutdownRequest)(LPWSTR szReason);

	private:
		System::Runtime::InteropServices::GCHandle m_opcAeServer;
	};

}}}}
