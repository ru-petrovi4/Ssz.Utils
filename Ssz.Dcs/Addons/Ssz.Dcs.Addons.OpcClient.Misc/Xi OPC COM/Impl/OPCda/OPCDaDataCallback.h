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

#include "OPCDaGroup.h"

using namespace Xi::OPC::COM::API;

namespace Xi {
namespace OPC {
namespace COM {
namespace Impl {

	class ATL_NO_VTABLE COPCDaDataCallback
		: public ::CComObjectRootEx<::CComMultiThreadModel>
		, public ::CComCoClass<COPCDaDataCallback>
		, public ::IOPCDataCallback
	{
	public:
		COPCDaDataCallback(void);
		virtual ~COPCDaDataCallback(void);

		DECLARE_NOT_AGGREGATABLE(COPCDaDataCallback)
		DECLARE_GET_CONTROLLING_UNKNOWN()

		DECLARE_PROTECT_FINAL_CONSTRUCT()

		BEGIN_COM_MAP(COPCDaDataCallback)
			COM_INTERFACE_ENTRY(::IOPCDataCallback)
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

		inline void SetOpcServerGCHandle(System::Runtime::InteropServices::GCHandle opcDaServer)
		{
			m_opcDaServer = opcDaServer;
		}

	public:
		// IOPCDataCallback
		STDMETHOD(OnDataChange)(ULONG dwTransid, ULONG hGroup, HRESULT hrMasterQuality, 
			HRESULT hrMasterError, ULONG dwCount, ULONG * phClientItems, VARIANT * pvValues, 
			USHORT * pwQualities, _FILETIME * pftTimeStamps, HRESULT * pErrors);
		STDMETHOD(OnReadComplete)(ULONG dwTransid, ULONG hGroup, HRESULT hrMasterQuality, 
			HRESULT hrMasterError, ULONG dwCount, ULONG * phClientItems, VARIANT * pvValues, 
			USHORT * pwQualities, _FILETIME * pftTimeStamps, HRESULT * pErrors);
		STDMETHOD(OnWriteComplete)(ULONG dwTransid, ULONG hGroup, HRESULT hrMastererr,
			ULONG dwCount, ULONG * phClientItems, HRESULT * pErrors);
		STDMETHOD(OnCancelComplete)(ULONG dwTransid, ULONG hGroup);

	private:
		System::Runtime::InteropServices::GCHandle m_opcDaServer;
	};

}}}}
