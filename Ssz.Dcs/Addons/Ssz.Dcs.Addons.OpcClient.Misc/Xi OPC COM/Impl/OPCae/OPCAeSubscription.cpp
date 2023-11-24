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

#include "..\StdAfx.h"
#include "OPCAeSubscription.h"
#include "OPCAeServer.h"
#include "OPCAeEventSink.h"
#include "..\Helper.h"

using namespace System::Runtime::InteropServices;
using namespace System::ServiceModel;
using namespace Xi::Contracts::Data;

namespace Xi {
namespace OPC {
namespace COM {
namespace Impl {

	COPCAeSubscription::COPCAeSubscription(cliHRESULT %HR, 
		COPCAeServer^ rCOPCAeServer, 
		unsigned int hClientSubscription,
		IOPCEventSubscriptionMgt* pIOPCEventSubscriptionMgt,
		::IOPCEventSink * pIOPCEventSink)
		: m_rCOPCAeServer(rCOPCAeServer)
		, m_hClientSubscription(hClientSubscription)
		, m_pIOPCEventSubMgt(pIOPCEventSubscriptionMgt)
		, m_pIOPCEventSink(pIOPCEventSink)
		, m_dwAdviseCookie(0)
		, m_bHasBeenDisposed(false)
	{
		HR = S_OK;
		if (nullptr != m_pIOPCEventSink)
		{
			CComQIPtr<IConnectionPointContainer, &IID_IConnectionPointContainer> pICPC( m_pIOPCEventSubMgt );
			if (nullptr != pICPC.p)
			{
				::IConnectionPoint * pIConnectionPoint = nullptr;
				cliHRESULT HR1 = pICPC->FindConnectionPoint( IID_IOPCEventSink, &pIConnectionPoint );
				if (HR1.Succeeded && nullptr != pIConnectionPoint)
				{
					ULONG dwCookie = 0;
					cliHRESULT HR2 = pIConnectionPoint->Advise( m_pIOPCEventSink, &dwCookie );
					if (HR2.Failed)
					{
						throw FaultHelpers::Create((unsigned int)HR2.hResult,
							gcnew String(L"Failed to setup IOPCEventSink."));
					}
					ULONG refCount = m_pIOPCEventSink->AddRef();
					m_dwAdviseCookie = dwCookie;
					pIConnectionPoint->Release();
					m_rCOPCAeServer->AddToDictionary(m_hClientSubscription, this);
				}
				else
				{
					throw FaultHelpers::Create((unsigned int)HR1.hResult,
						gcnew String(L"Failed to Find the IOPCEventSink for OnEvent."));
				}
			}
			else
			{
				throw FaultHelpers::Create(gcnew String(L"Failed to Get the IConnectionPointContainer for OnEvent"));
			}
		}

	}

	COPCAeSubscription::~COPCAeSubscription(void)
	{
		if (DisposeThis(true))
			GC::SuppressFinalize(this);
	}

	COPCAeSubscription::!COPCAeSubscription(void)
	{
		DisposeThis(false);
	}

	bool COPCAeSubscription::DisposeThis(bool isDisposing)
	{
		if (m_bHasBeenDisposed)
			return false;

		if (0 != m_dwAdviseCookie)
		{
			if (isDisposing)
				m_rCOPCAeServer->RemoveFromDictionary(m_hClientSubscription);

			CComQIPtr<IConnectionPointContainer, &IID_IConnectionPointContainer> pICPC( m_pIOPCEventSubMgt );
			if (nullptr != pICPC.p)
			{
				::IConnectionPoint * pIConnectionPoint = nullptr;
				cliHRESULT HR1 = pICPC->FindConnectionPoint( IID_IOPCEventSink, &pIConnectionPoint );
				_ASSERTE(HR1.Succeeded && nullptr != pIConnectionPoint);
				if (HR1.Succeeded && nullptr != pIConnectionPoint)
				{
					cliHRESULT HR2 = pIConnectionPoint->Unadvise(m_dwAdviseCookie);
					_ASSERTE(HR2.Succeeded);
					pIConnectionPoint->Release();
					ULONG refCount = m_pIOPCEventSink->Release();
					m_pIOPCEventSink = nullptr;
					m_dwAdviseCookie = 0;
				}
			}
		}

		if ( nullptr != m_pIOPCEventSubMgt )
			m_pIOPCEventSubMgt->Release();
		m_pIOPCEventSubMgt = nullptr;

		m_bHasBeenDisposed = true;
		return true;
	}

	// IOPCEventSubscriptionMgt
	cliHRESULT COPCAeSubscription::SetFilter (
		/*[in]*/ unsigned int dwEventType,
		/*[in]*/ List<unsigned int>^ EventCategories,
		/*[in]*/ unsigned int dwLowSeverity,
		/*[in]*/ unsigned int dwHighSeverity,
		/*[in]*/ List<String^>^ AreaList,
		/*[in]*/ List<String^>^ SourceList )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC A&E Subscription has been Disposed!");

		HRESULT hr = E_FAIL;
		DWORD i = 0;
		DWORD dwNumCats = EventCategories->Count;

		std::vector<DWORD> pdwEventCategories(dwNumCats);

		for each(DWORD ec in EventCategories)
		{
			pdwEventCategories[i++] = ec;
		}

		DWORD dwNumAreas = AreaList->Count;
		std::vector<LPWSTR> ppszAreaList(dwNumAreas);

		for(unsigned int j = 0; j < dwNumAreas; j++ )
		{
			ppszAreaList[j] = CHelper::ConvertStringToLPWSTR( AreaList[j] );
		}

		DWORD dwNumSources = SourceList->Count;
		std::vector<LPWSTR> ppszSourceList(dwNumSources);
		for(unsigned int k = 0; k < dwNumSources; k++ )
		{
			ppszSourceList[k] = CHelper::ConvertStringToLPWSTR( SourceList[k] );
		}

		try
		{
			hr = m_pIOPCEventSubMgt->SetFilter(dwEventType,
				dwNumCats,
				pdwEventCategories.data(),
				dwLowSeverity,
				dwHighSeverity,
				dwNumAreas,
				ppszAreaList.data(),
				dwNumSources,
				ppszSourceList.data());
		}
		finally
		{
			// free allocated memory
			for (unsigned int j = 0; j < dwNumAreas; j++)
			{
				Marshal::FreeHGlobal((IntPtr)ppszAreaList[j]);
			}
			for (unsigned int k = 0; k < dwNumSources; k++)
			{
				Marshal::FreeHGlobal((IntPtr)ppszSourceList[k]);
			}
		}

		return hr;
	}

	cliHRESULT COPCAeSubscription::GetFilter (
		/*[out]*/ unsigned int %dwEventType,
		/*[out]*/ List<unsigned int>^ %EventCategories,
		/*[out]*/ unsigned int %dwLowSeverity,
		/*[out]*/ unsigned int %dwHighSeverity,
		/*[out]*/ List<String^>^ %AreaList,
		/*[out]*/ List<String^>^ %SourceList )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC A&E Subscription has been Disposed!");

		HRESULT hr = E_FAIL;
		DWORD	pdwEventType;
		DWORD dwNumCats = 0;
		DWORD dwNumAreas = 0;
		DWORD dwNumSources = 0;
		DWORD* ppszEventCats = nullptr;
		LPWSTR* ppszAreaList = nullptr;
		LPWSTR* ppszSourceList= nullptr;
		DWORD	pdwLowSeverity = 0;
		DWORD	pdwHighSeverity = 0;
		DWORD	i = 0;

		EventCategories = gcnew List<unsigned int>;

		hr = m_pIOPCEventSubMgt->GetFilter(&pdwEventType, 
			&dwNumCats, 
			&ppszEventCats, 
			&pdwLowSeverity, 
			&pdwHighSeverity,
			&dwNumAreas,
			&ppszAreaList,
			&dwNumSources,
			&ppszSourceList);

		if( hr == S_OK )
		{
			dwEventType = pdwEventType;
			dwLowSeverity = pdwLowSeverity;
			dwHighSeverity = pdwHighSeverity;

			EventCategories = gcnew List<unsigned int>(dwNumCats);
			for( i = 0; i < dwNumCats; i++ )
			{
				EventCategories[i] = ppszEventCats[i];
			}

			AreaList = gcnew List<String^>(dwNumAreas);
			for( i = 0; i < dwNumAreas; i++ )
			{
				AreaList[i] = gcnew String( ppszAreaList[i] );
				
				if (ppszAreaList[i] != nullptr)
				{
					::CoTaskMemFree(ppszAreaList[i]);
					ppszAreaList[i] = nullptr;
				}
			}

			SourceList = gcnew List<String^>(dwNumSources);
			for( i = 0; i < dwNumSources; i++ )
			{
				SourceList[i] = gcnew String( ppszSourceList[i] );
				
				if (ppszSourceList[i] != nullptr)
				{
					::CoTaskMemFree(ppszSourceList[i]);
					ppszSourceList[i] = nullptr;
				}
			}

			if (ppszEventCats != nullptr)
			{
				::CoTaskMemFree(ppszEventCats);
				ppszEventCats = nullptr;
			}

			if (ppszAreaList != nullptr)
			{
				::CoTaskMemFree(ppszAreaList);
				ppszAreaList = nullptr;
			}

			if (ppszSourceList != nullptr)
			{
				::CoTaskMemFree(ppszSourceList);
				ppszSourceList = nullptr;
			}
		}

		return hr;
	}

	cliHRESULT COPCAeSubscription::SelectReturnedAttributes (
		/*[in]*/ unsigned int dwEventCategory,
		/*[in]*/ List<unsigned int>^ AttributeIDs )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC A&E Subscription has been Disposed!");

		HRESULT hr = E_FAIL;
		DWORD	dwCount = AttributeIDs->Count;
		DWORD*	pdwAttributeIDs = new DWORD[dwCount];

		for( DWORD i = 0; i < dwCount; i++ )
		{
			pdwAttributeIDs[i] = AttributeIDs[i];
		}

		hr = m_pIOPCEventSubMgt->SelectReturnedAttributes(dwEventCategory, dwCount, pdwAttributeIDs );

		if (pdwAttributeIDs != nullptr)
		{
			delete [] pdwAttributeIDs;
			pdwAttributeIDs = nullptr;
		}

		return hr;
	}

	cliHRESULT COPCAeSubscription::GetReturnedAttributes (
		/*[in]*/ unsigned int dwEventCategory,
		/*[out]*/ List<unsigned int>^ %AttributeIDs )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC A&E Subscription has been Disposed!");

		HRESULT hr = E_FAIL;
		AttributeIDs = nullptr;
		DWORD	dwCount = AttributeIDs->Count;
		DWORD*	pdwAttributeIDs = nullptr;

		hr = m_pIOPCEventSubMgt->GetReturnedAttributes(dwEventCategory, &dwCount, &pdwAttributeIDs );

		if( hr == S_OK )
		{
			AttributeIDs = gcnew List<unsigned int>(dwCount);

			for( DWORD i = 0; i < dwCount; i++ )
			{
				AttributeIDs->Add(pdwAttributeIDs[i]);
			}

			if (pdwAttributeIDs != nullptr)
			{
				::CoTaskMemFree(pdwAttributeIDs);
				pdwAttributeIDs = nullptr;
			}
		}

		return hr;
	}

	cliHRESULT COPCAeSubscription::Refresh (
		/*[in]*/ unsigned int dwConnection )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC A&E Subscription has been Disposed!");

		HRESULT hr = E_FAIL;
		hr = m_pIOPCEventSubMgt->Refresh( m_dwAdviseCookie );

		return hr;
	}

	cliHRESULT COPCAeSubscription::CancelRefresh (
		/*[in]*/ unsigned int dwConnection )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC A&E Subscription has been Disposed!");

		HRESULT hr = E_FAIL;
		hr = m_pIOPCEventSubMgt->CancelRefresh( dwConnection );

		return hr;	
	}

	cliHRESULT COPCAeSubscription::GetState (
		/*[out]*/ bool %bActive,
		/*[out]*/ unsigned int %uiBufferTime,
		/*[out]*/ unsigned int %uiMaxSize,
		/*[out]*/ unsigned int %uiClientSubscription )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC A&E Subscription has been Disposed!");

		HRESULT hr = E_FAIL;
		long lActive = TRUE; 
		DWORD dwBufferTime = 0;
		DWORD dwMaxSize = 0;
		DWORD hClientHandle = 0;

		hr = m_pIOPCEventSubMgt->GetState( &lActive,
			&dwBufferTime,
			&dwMaxSize,
			&hClientHandle);

		if( hr == S_OK )
		{
			bActive = (0 != lActive) ? true : false;
			uiBufferTime = dwBufferTime;
			uiMaxSize = dwMaxSize;
			uiClientSubscription = hClientHandle;
		}

		return hr;
	}

	cliHRESULT COPCAeSubscription::SetState (
		/*[in]*/ Nullable<bool> bActive,
		/*[in]*/ Nullable<unsigned int> uiBufferTime,
		/*[in]*/ Nullable<unsigned int> uiMaxSize,
		/*[in]*/ unsigned int hClientSubscription,
		/*[out]*/ unsigned int %uiRevisedBufferTime,
		/*[out]*/ unsigned int %uiRevisedMaxSize )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC A&E Subscription has been Disposed!");

		HRESULT hr = E_FAIL;
		long	lActive = (bActive.HasValue) ? ((bActive.Value) ? 1 : 0) : 0; 
		long*	plActive = (bActive.HasValue) ? &lActive : nullptr;
		DWORD	dwBufferTime = (uiBufferTime.HasValue) ? uiBufferTime.Value : 0;
		DWORD*	pdwBufferTime = (uiBufferTime.HasValue) ? &dwBufferTime : nullptr;
		DWORD	dwMaxSize = (uiMaxSize.HasValue) ? uiMaxSize.Value : 0;
		DWORD*	pdwMaxSize = (uiMaxSize.HasValue) ? &dwMaxSize : nullptr;
		DWORD	hClientHandle = hClientSubscription;
		DWORD	dwReviseBufferTime = 0;
		DWORD	dwRevisedMaxSize = 0; 

		hr = m_pIOPCEventSubMgt->SetState( 
			plActive,
			pdwBufferTime,
			pdwMaxSize,
			hClientHandle,
			&dwReviseBufferTime,
			&dwRevisedMaxSize);

		if( hr == S_OK )
		{
			uiRevisedBufferTime = dwReviseBufferTime;
			uiRevisedMaxSize = dwRevisedMaxSize;
		}

		return hr;
	}

	cliHRESULT COPCAeSubscription::AdviseOnEvent(
		/*[in]*/ OPCEventSink::OnEvent^ onEvent)
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC A&E Subscription has been Disposed!");

		m_onEvent += onEvent;
		return cliHRESULT(S_OK);
	}

	cliHRESULT COPCAeSubscription::UnadviseOnEvent(
		/*[in]*/ OPCEventSink::OnEvent^ onEvent)
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC A&E Subscription has been Disposed!");

		m_onEvent -= onEvent;
		return cliHRESULT(S_OK);
	}

}}}}
