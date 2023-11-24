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
#include "OPCAeServer.h"
#include "OPCAeAreaBrowser.h"
#include "OPCAeEventSink.h"
#include "OPCAeShutdownCallback.h"
#include "..\Helper.h"
#include <vcclr.h>

using namespace System;
using namespace System::Runtime::InteropServices;
using namespace System::Collections::Generic;

namespace Xi {
namespace OPC {
namespace COM {
namespace Impl {

	COPCAeServer::COPCAeServer(cliHRESULT &HR, ::IOPCEventServer * pIOPCEventServer)
		: m_pIOPCEventServer(pIOPCEventServer)
		, m_pIOPCCommon(nullptr)
		, m_shutdownRequest(nullptr)
		, m_dwShutdownAdviseCookie(0)
		, m_bHasBeenDisposed(false)
	{

		m_OPCAeSubscriptions = gcnew Dictionary<unsigned int, COPCAeSubscription^>();
		HR = E_FAIL;
		m_gcHandleTHIS = GCHandle::Alloc(this);
		::IOPCCommon * pIOPCCommon = nullptr;
		HR = pIOPCEventServer->QueryInterface(IID_IOPCCommon, reinterpret_cast<void**>(&pIOPCCommon));
		if ( HR.Succeeded )
		{
			m_pIOPCCommon = pIOPCCommon;
		}

		// A single instance of the COPCAeEventSink class is used for all subscriptions
		::IOPCEventSink * pIOPCEventSink = nullptr;     // For the IConnectionPoint
		cliHRESULT HR1 = COPCAeEventSink::CreateInstance( reinterpret_cast<::IOPCEventSink**>(&pIOPCEventSink) );
		if ( HR1.Succeeded )
		{
			m_pIOPCEventSink = pIOPCEventSink;
			m_pCOPCAeEventSink = (COPCAeEventSink*)pIOPCEventSink;

			m_pCOPCAeEventSink->AeServer = this;
		}

		::IOPCShutdown * pIOPCShutdown = nullptr;
		cliHRESULT HR2 = COPCAeShutdownCallback::CreateInstance( reinterpret_cast<::IOPCShutdown**>(&pIOPCShutdown) );
		if (HR2.Succeeded)
		{
			m_pIOPCShutdown = pIOPCShutdown;
			((COPCAeShutdownCallback*)pIOPCShutdown)->SetOpcServerGCHandle(m_gcHandleTHIS);

			CComQIPtr<IConnectionPointContainer, &IID_IConnectionPointContainer> pICPC( m_pIOPCEventServer );
			if (nullptr != pICPC.p)
			{
				::IConnectionPoint * pIConnectionPoint = nullptr;
				cliHRESULT HR3 = pICPC->FindConnectionPoint( IID_IOPCShutdown, &pIConnectionPoint );
				if (HR3.Succeeded && nullptr != pIConnectionPoint)
				{
					ULONG dwCookie = 0;
					cliHRESULT HR4 = pIConnectionPoint->Advise( m_pIOPCShutdown, &dwCookie );
					if (HR4.Failed)
					{
						throw FaultHelpers::Create((unsigned int)HR2.hResult,
							gcnew String(L"Failed to setup IOPCShutdown."));
					}
					m_dwShutdownAdviseCookie = dwCookie;
					pIConnectionPoint->Release();
				}
			}
			else
			{
				throw FaultHelpers::Create((unsigned int)HR1.hResult,
					gcnew String(L"Failed to Find the IOPCShutdown for ShutdownRequest."));
			}
		}
	}

	COPCAeServer::~COPCAeServer(void)
	{
		if (DisposeThis(true))
			GC::SuppressFinalize(this);
	}

	COPCAeServer::!COPCAeServer(void)
	{
		DisposeThis(false);
	}

	bool COPCAeServer::DisposeThis(bool isDisposing)
	{
		if (m_bHasBeenDisposed)
			return false;

		CComQIPtr<IConnectionPointContainer, &IID_IConnectionPointContainer> pICPC( m_pIOPCEventServer );
		if (nullptr != pICPC.p)
		{
			::IConnectionPoint * pIConnectionPoint = nullptr;
			cliHRESULT HR1 = pICPC->FindConnectionPoint( IID_IOPCShutdown, &pIConnectionPoint );
			_ASSERTE(HR1.Succeeded && nullptr != pIConnectionPoint);
			if (HR1.Succeeded && nullptr != pIConnectionPoint)
			{
				cliHRESULT HR2 = pIConnectionPoint->Unadvise(m_dwShutdownAdviseCookie);
				_ASSERT(HR2.Succeeded);
				pIConnectionPoint->Release();
			}
		}
		m_gcHandleTHIS.Free();

		if (nullptr != m_pIOPCEventSink)
			m_pIOPCEventSink->Release();
		m_pIOPCEventSink = nullptr;

		if (nullptr != m_pIOPCEventServer)
			m_pIOPCEventServer->Release();
		m_pIOPCEventServer = nullptr;

		if (nullptr != m_pIOPCCommon)
			m_pIOPCCommon->Release();
		m_pIOPCCommon = nullptr;

		m_bHasBeenDisposed = true;
		return true;
	}

	// IOPCCommon
	cliHRESULT COPCAeServer::SetLocaleID(
		/*[in]*/ unsigned int dwLcid )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC A&E Server has been Disposed!");

		return IOPCCommon->SetLocaleID(dwLcid);
	}

	cliHRESULT COPCAeServer::GetLocaleID(
		/*[out]*/ unsigned int %dwLcid )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC A&E Server has been Disposed!");

		unsigned long ulLocaleID = 0;
		HRESULT hr = IOPCCommon->GetLocaleID(&ulLocaleID);

		if( hr == S_OK )
		{
			dwLcid = ulLocaleID;
		}

		return cliHRESULT(hr);
	}

	cliHRESULT COPCAeServer::QueryAvailableLocaleIDs(
		/*[out]*/ List<unsigned int>^ %dwLcid )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC A&E Server has been Disposed!");

		dwLcid = gcnew List<unsigned int>();

		HRESULT hr = S_OK;
		DWORD dwCount = 0;
		DWORD* pDwId = nullptr;
		unsigned long ulLocaleID = 0;

		hr = IOPCCommon->QueryAvailableLocaleIDs(&dwCount, &pDwId);

		if( hr == S_OK )
		{
			for(DWORD i = 0; i < dwCount; i++)
			{
				dwLcid->Add((unsigned int)pDwId[i]);
			}
			
			if (pDwId != nullptr)
			{
				::CoTaskMemFree(pDwId);
				pDwId = nullptr;
			}
		}

		return hr;
	}

	cliHRESULT COPCAeServer::GetErrorString(
		/*[in]*/ cliHRESULT dwError,
		/*[out]*/ String^ %errString )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC A&E Server has been Disposed!");

		HRESULT hr = S_OK;
		errString = nullptr;
		LPWSTR pErrorString = nullptr;
		hr = IOPCCommon->GetErrorString( dwError.hResult, &pErrorString );
		if( hr == S_OK )
		{
			errString = gcnew String( pErrorString );
		}
		
		if (pErrorString != nullptr)
		{
			::CoTaskMemFree(pErrorString);
			pErrorString = nullptr;
		}

		return hr;
	}

	cliHRESULT COPCAeServer::SetClientName(
		/*[in]*/ String^ zName )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC A&E Server has been Disposed!");

		LPWSTR	szName = CHelper::ConvertStringToLPWSTR(zName);

		HRESULT hr = IOPCCommon->SetClientName(szName);

		// free allocated memory
		Marshal::FreeHGlobal((IntPtr)szName);

		return hr;

	}

	// IOPCEventServer
	cliHRESULT COPCAeServer::GetStatus (
		/*[out]*/ cliOPCEVENTSERVERSTATUS^ %EventServerStatus )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC A&E Server has been Disposed!");

		EventServerStatus = gcnew cliOPCEVENTSERVERSTATUS();
		CComHeapPtr<OPCEVENTSERVERSTATUS> pServerStatus;

		HRESULT hr = m_pIOPCEventServer->GetStatus( &pServerStatus );

		if ( nullptr != pServerStatus && S_OK == hr )
		{
			EventServerStatus->dtStartTime      = DateTime::FromFileTimeUtc(*( (_int64*)&pServerStatus->ftStartTime));
			EventServerStatus->dtCurrentTime    = DateTime::FromFileTimeUtc(*( (_int64*)&pServerStatus->ftCurrentTime));
			// If the server does not support the LastUpdateTime, this try will keep it from faulting the call.
			try {EventServerStatus->dtLastUpdateTime = DateTime::FromFileTimeUtc(*( (_int64*)&pServerStatus->ftLastUpdateTime));}
			catch (Exception^ e) {e = nullptr;/* to keep from getting a warning*/}
			EventServerStatus->dwServerState    = (OPCEVENTSERVERSTATE)pServerStatus->dwServerState;
			EventServerStatus->wMajorVersion    = pServerStatus->wMajorVersion;
			EventServerStatus->wMinorVersion    =  pServerStatus->wMinorVersion;
			EventServerStatus->wBuildNumber     =  pServerStatus->wBuildNumber;
			EventServerStatus->wReserved        = pServerStatus->wReserved;
			EventServerStatus->sVendorInfo      = gcnew String(pServerStatus->szVendorInfo);
		}

		return cliHRESULT(hr);
	}

	cliHRESULT COPCAeServer::CreateEventSubscription (
		/*[in]*/ bool bActive,
		/*[in]*/ unsigned int dwBufferTime,
		/*[in]*/ unsigned int dwMaxSize,
		/*[in]*/ unsigned int hClientSubscription,
		/*[out]*/ IOPCEventSubscriptionMgtCli^ %iOPCEventSubscriptionMgt,
		/*[out]*/ unsigned int %uiRevisedBufferTime,
		/*[out]*/ unsigned int %uiRevisedMaxSize )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC A&E Server has been Disposed!");

		cliHRESULT hResult = S_OK;
		iOPCEventSubscriptionMgt = nullptr;

		DWORD dwRevisedBufferTime;
		DWORD dwRevisedMaxSize;
		::IOPCEventSubscriptionMgt * pIOPCEventSubscriptionMgt = nullptr;

		hResult = m_pIOPCEventServer->CreateEventSubscription( 
			bActive, dwBufferTime, dwMaxSize, hClientSubscription, 
			(_GUID *)(& __uuidof(IOPCEventSubscriptionMgt)),
			(::IUnknown **)&pIOPCEventSubscriptionMgt,
			&dwRevisedBufferTime, &dwRevisedMaxSize );
		if ( hResult.Succeeded ) 
		{
			uiRevisedBufferTime = dwRevisedBufferTime;
			uiRevisedMaxSize = dwRevisedMaxSize;
			COPCAeSubscription^ rCOPCAeSubscription = gcnew COPCAeSubscription(hResult, 
				this, hClientSubscription, pIOPCEventSubscriptionMgt, m_pIOPCEventSink);
			iOPCEventSubscriptionMgt = dynamic_cast<IOPCEventSubscriptionMgtCli^>(rCOPCAeSubscription);
		}
		return hResult;
	}

	cliHRESULT COPCAeServer::QueryAvailableFilters (
		/*[out]*/ unsigned int %dwFilterMask )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC A&E Server has been Disposed!");

		HRESULT        hResult = S_OK;
		DWORD          dwFilter = 0;

		hResult = m_pIOPCEventServer->QueryAvailableFilters(&dwFilter);
		if (hResult == S_OK)
		{
			dwFilterMask = dwFilter;
		}

		return cliHRESULT(hResult);
	}

	cliHRESULT COPCAeServer::QueryEventCategories (
		/*[in]*/ unsigned int dwEventType,
		/*[out]*/ List<OPCEVENTCATEGORY^>^ %EventCategories )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC A&E Server has been Disposed!");

		EventCategories = nullptr;

		HRESULT        hResult = S_OK;
		DWORD          dwCount = 0;
		CComHeapPtr<DWORD>         pdwEventCategories;
		CComHeapPtr<LPWSTR>        pEventCategoryDescs;

		hResult = m_pIOPCEventServer->QueryEventCategories(dwEventType, 
			&dwCount,
			&pdwEventCategories,
			&pEventCategoryDescs);
		if (hResult == S_OK)
		{
			EventCategories = gcnew List<OPCEVENTCATEGORY^>();

			for(DWORD i=0; i < dwCount; i++)
			{
				OPCEVENTCATEGORY^ eventCategory = gcnew OPCEVENTCATEGORY();

				eventCategory->dwEventCategory = pdwEventCategories[i];
				eventCategory->sEventCategoryDesc = gcnew String(pEventCategoryDescs[i]);

				EventCategories->Add(eventCategory);

				if (pEventCategoryDescs[i] != nullptr)
				{
					::CoTaskMemFree(pEventCategoryDescs[i]);
					pEventCategoryDescs[i] = nullptr;
				}
			}
		}

		return cliHRESULT(hResult);
	}

	cliHRESULT COPCAeServer::QueryConditionNames (
		/*[in]*/ unsigned int dwEventCategory,
		/*[out]*/ List<String^>^ %ConditionNames )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC A&E Server has been Disposed!");

		ConditionNames = nullptr;
		ConditionNames = gcnew List<String^>();
		HRESULT hr = S_OK;
		DWORD dwCount;
		CComHeapPtr<LPWSTR> pszConditionNames;

		hr = m_pIOPCEventServer->QueryConditionNames(dwEventCategory, &dwCount, &pszConditionNames);

		if( hr == S_OK )
		{
			for (DWORD i = 0; i < dwCount; i++)
			{
				try
				{
					ConditionNames->Add(gcnew String(pszConditionNames[i]));
				}
				finally
				{
					if (pszConditionNames[i] != nullptr)
					{
						::CoTaskMemFree(pszConditionNames[i]);
						pszConditionNames[i] = nullptr;
					}
				}
			}
		}

		return cliHRESULT(hr);
	}

	cliHRESULT COPCAeServer::QuerySubConditionNames (
		/*[in]*/ String^ sConditionName,
		/*[out]*/ List<String^>^ %SubConditionNames )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC A&E Server has been Disposed!");

		SubConditionNames = nullptr;
		pin_ptr<const wchar_t> pszCondName = PtrToStringChars(sConditionName);
		DWORD dwCount;
		LPWSTR *pszSubcondNames = nullptr;
		HRESULT hr = S_OK;

		hr = m_pIOPCEventServer->QuerySubConditionNames((LPWSTR)pszCondName, &dwCount, &pszSubcondNames);

		if( hr == S_OK )
		{
			SubConditionNames = gcnew List<String^>;
			for (DWORD i = 0; i < dwCount; i++)
			{
				SubConditionNames->Add(gcnew String(pszSubcondNames[i]));
				
				if (pszSubcondNames[i] != nullptr)
				{
					::CoTaskMemFree(pszSubcondNames[i]);
					pszSubcondNames[i] = nullptr;
				}
			}

			if (pszSubcondNames != nullptr) 
			{
				::CoTaskMemFree(pszSubcondNames);
				pszSubcondNames = nullptr;
			}
		}

		// free allocated memory
		return cliHRESULT(hr);
	}

	cliHRESULT COPCAeServer::QuerySourceConditions (
		/*[in]*/ String^ sSource,
		/*[out]*/ List<String^>^ %ConditionNames )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC A&E Server has been Disposed!");

		ConditionNames = nullptr;
		DWORD       dwCount = 0;
		CComHeapPtr<LPWSTR>	pszConditionNames;

		pin_ptr<const wchar_t> szSource = PtrToStringChars(sSource);

		HRESULT hResult = m_pIOPCEventServer->QuerySourceConditions((LPWSTR)szSource, &dwCount, &pszConditionNames);

		if (hResult == S_OK)
		{
			ConditionNames = gcnew List<String^>();

			for (DWORD i = 0; i < dwCount; i++)
			{
				ConditionNames->Add( gcnew String(pszConditionNames[i]));
				
				if (pszConditionNames[i] != nullptr)
				{
					::CoTaskMemFree(pszConditionNames[i]);
					pszConditionNames[i] = nullptr;
				}
			}
		}

		return cliHRESULT(hResult);
	}

	cliHRESULT COPCAeServer::QueryEventAttributes (
		/*[in]*/ unsigned int dwEventCategory,
		/*[out]*/ List<OPCEVENTATTRIBUTE^>^ %EventAttributes )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC A&E Server has been Disposed!");

		EventAttributes = nullptr;
		HRESULT hr = E_FAIL;
		DWORD dwCAT = dwEventCategory;
		DWORD dwCount = 0;
		CComHeapPtr<DWORD> pdwAttrIDs;
		CComHeapPtr<LPWSTR> pszAttrDescs;
		CComHeapPtr<VARTYPE> pvtAttrTypes;

		hr = m_pIOPCEventServer->QueryEventAttributes(dwCAT, &dwCount, &pdwAttrIDs, &pszAttrDescs, &pvtAttrTypes);

		if (hr == S_OK)
		{
			EventAttributes = gcnew List<OPCEVENTATTRIBUTE^>();

			for (DWORD i = 0; i < dwCount; i++)
			{	
				OPCEVENTATTRIBUTE^ eAttr = gcnew OPCEVENTATTRIBUTE();

				eAttr->dwAttrID = pdwAttrIDs[i];
				eAttr->sAttrDesc = gcnew String(pszAttrDescs[i]);
				eAttr->vtAttrType = pvtAttrTypes[i];
				
				try
				{
					EventAttributes->Add(eAttr);
				}
				finally
				{
					if (pszAttrDescs[i] != nullptr)
					{
						::CoTaskMemFree(pszAttrDescs[i]);
						pszAttrDescs[i] = nullptr;
					}
				}
			}
		}

		return cliHRESULT(hr);
	}

	cliHRESULT COPCAeServer::TranslateToItemIDs (
		/*[in]*/ String^ sSource,
		/*[in]*/ unsigned int dwEventCategory,
		/*[in]*/ String^ sConditionName,
		/*[in]*/ String^ sSubconditionName,
		/*[in]*/ List<unsigned int>^ dwAssocAttrIDs,
		/*[out]*/ List<OPCEVENTITEMID^>^ %EventItemIDs )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC A&E Server has been Disposed!");

		dwAssocAttrIDs = nullptr;
		EventItemIDs = nullptr;

		HRESULT        hResult = S_OK;
		pin_ptr<const wchar_t> szSource = PtrToStringChars(sSource);
		pin_ptr<const wchar_t> szConditionName = PtrToStringChars(sConditionName);
		pin_ptr<const wchar_t> szSubconditionName = PtrToStringChars(sSubconditionName);

		DWORD dwCount = dwAssocAttrIDs->Count;

		std::vector<DWORD> pdwAssocAttrIDs(dwCount);

		CComHeapPtr<LPWSTR> pszAttrItemIDs;
		CComHeapPtr<LPWSTR> pszNodeNames;
		CComHeapPtr<GUID> pCLSIDs;
		int i = 0;

		for each(DWORD dwAttrId in dwAssocAttrIDs)
		{
			pdwAssocAttrIDs[i++] = dwAttrId;
		}

		hResult = m_pIOPCEventServer->TranslateToItemIDs(
			(LPWSTR)szSource,
			dwEventCategory,
			(LPWSTR)szConditionName,
			(LPWSTR)szSubconditionName,
			dwCount,
			pdwAssocAttrIDs.data(),
			&pszAttrItemIDs,
			&pszNodeNames,
			&pCLSIDs);
		if( hResult == S_OK)
		{
			EventItemIDs = gcnew List<OPCEVENTITEMID^>(dwCount);

			for(DWORD i = 0; i < dwCount; i++)
			{
				try
				{
					OPCEVENTITEMID^ itemID = gcnew OPCEVENTITEMID();

					itemID->sAttrItemID = gcnew String(pszAttrItemIDs[i]);
					itemID->sNodeName = gcnew String(pszNodeNames[i]);
					itemID->DaServerCLSID = CHelper::ConvertGUIDToGuid(pCLSIDs[i]);

					EventItemIDs->Add(itemID);
				}
				finally
				{
					if (pszAttrItemIDs[i] != nullptr)
					{
						::CoTaskMemFree(pszAttrItemIDs[i]);
						pszAttrItemIDs[i] = nullptr;
					}

					if (pszNodeNames[i] != nullptr)
					{
						::CoTaskMemFree(pszNodeNames[i]);
						pszNodeNames[i] = nullptr;
					}
				}
			}
		}

		return cliHRESULT(hResult);
	}

	cliHRESULT COPCAeServer::GetConditionState (
		/*[in]*/ String^ sSource,
		/*[in]*/ String^ sConditionName,
		/*[in]*/ List<unsigned int>^ AttributeIDs,
		/*[out]*/ List<cliOPCCONDITIONSTATE^>^ %ConditionStates )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC A&E Server has been Disposed!");

		ConditionStates = gcnew List<cliOPCCONDITIONSTATE^>();
		HRESULT hResult = S_OK;

		if( AttributeIDs->Count > 0 )
		{
			pin_ptr<const wchar_t> szSource = PtrToStringChars(sSource);
			pin_ptr<const wchar_t> szCondName = PtrToStringChars(sConditionName);

			int i = 0;
			CComHeapPtr<OPCCONDITIONSTATE> pConditionState;
			DWORD dwNumEventAttrs = AttributeIDs->Count;

			std::vector<DWORD>pdwAttributeIDs(dwNumEventAttrs);

			for each( unsigned long id in AttributeIDs )
			{
				pdwAttributeIDs[i++] = id;
			}

			hResult = m_pIOPCEventServer->GetConditionState((LPWSTR)szSource,
				(LPWSTR)szCondName,
				dwNumEventAttrs,
				pdwAttributeIDs.data(),
				&pConditionState);
			if (hResult == S_OK)
			{
				for(unsigned int i = 0; i < dwNumEventAttrs; i++ )
				{
					try
					{
						cliOPCCONDITIONSTATE^ condState = gcnew cliOPCCONDITIONSTATE();

						condState->wState = pConditionState[i].wState;
						condState->wReserved1 = pConditionState[i].wReserved1;
						condState->sActiveSubCondition = gcnew String(pConditionState[i].szActiveSubCondition);
						condState->sASCDefinition = gcnew String(pConditionState[i].szASCDefinition);
						condState->dwASCSeverity = pConditionState[i].dwASCSeverity;
						condState->sASCDescription = gcnew String(pConditionState[i].szASCDescription);
						condState->wQuality = pConditionState[i].wQuality;
						condState->wReserved2 = pConditionState[i].wReserved2;
						condState->dtLastAckTime = DateTime::FromFileTimeUtc(*((__int64*)(&pConditionState[i].ftLastAckTime)));
						condState->dtSubCondLastActive = DateTime::FromFileTimeUtc(*((__int64*)(&pConditionState[i].ftSubCondLastActive)));
						condState->dtCondLastActive = DateTime::FromFileTimeUtc(*((__int64*)(&pConditionState[i].ftCondLastActive)));
						condState->dtCondLastInactive = DateTime::FromFileTimeUtc(*((__int64*)(&pConditionState[i].ftCondLastInactive)));
						condState->sAcknowledgerID = gcnew String(pConditionState[i].szAcknowledgerID);
						condState->sComment = gcnew String(pConditionState[i].szComment);

						condState->dwNumSCs = pConditionState[i].dwNumSCs;
						if (pConditionState[i].dwNumSCs > 0)
						{
							condState->sSCNames = gcnew List < String^ > ;
							condState->sSCDefinitions = gcnew List < String^ > ;
							condState->dwSCSeverities = gcnew List < unsigned int > ;
							condState->sSCDescriptions = gcnew List < String^ > ;
							for (unsigned int j = 0; j < condState->dwNumSCs; j++)
							{
								try
								{
									condState->sSCNames->Add(gcnew String((const wchar_t*)*pConditionState[i].pszSCNames[j]));
									condState->sSCDefinitions->Add(gcnew String((const wchar_t*)*pConditionState[i].pszSCDefinitions[j]));
									condState->dwSCSeverities->Add((unsigned int)pConditionState[i].pdwSCSeverities[j]);
									condState->sSCDescriptions->Add(gcnew String((const wchar_t*)*pConditionState[i].pszSCDescriptions[j]));
								}
								finally
								{
									if (pConditionState[i].pszSCNames[j] != nullptr)
									{
										::CoTaskMemFree(pConditionState[i].pszSCNames[j]);
										pConditionState[i].pszSCNames[j] = nullptr;
									}

									if (pConditionState[i].pszSCDefinitions[j] != nullptr)
									{
										::CoTaskMemFree(pConditionState[i].pszSCDefinitions[j]);
										pConditionState[i].pszSCDefinitions[j] = nullptr;
									}

									if (pConditionState[i].pszSCDescriptions[j] != nullptr)
									{
										::CoTaskMemFree(pConditionState[i].pszSCDescriptions[j]);
										pConditionState[i].pszSCDescriptions[j] = nullptr;
									}
								}
							}
						}
						condState->vDataValues = gcnew List < cliVARIANT > ;
						for (unsigned int k = 0; k < pConditionState->dwNumEventAttrs; k++)
							condState->vDataValues->Add(CHelper::ConvertFromVARIANT(&pConditionState[i].pEventAttributes[k]));

						condState->Error = (cliHRESULT)(*pConditionState[i].pErrors);

						ConditionStates->Add(condState);
					}
					finally
					{
						if (pConditionState[i].pszSCNames != nullptr)
						{
							::CoTaskMemFree(pConditionState[i].pszSCNames);
							pConditionState[i].pszSCNames = nullptr;
						}

						if (pConditionState[i].pszSCDefinitions != nullptr)
						{
							::CoTaskMemFree(pConditionState[i].pszSCDefinitions);
							pConditionState[i].pszSCDefinitions = nullptr;
						}

						if (pConditionState[i].pszSCDescriptions != nullptr)
						{
							::CoTaskMemFree(pConditionState[i].pszSCDescriptions);
							pConditionState[i].pszSCDescriptions = nullptr;
						}

						if (pConditionState[i].pEventAttributes != nullptr)
						{
							::CoTaskMemFree(pConditionState[i].pEventAttributes);
							pConditionState[i].pEventAttributes = nullptr;
						}

						::CoTaskMemFree(&pConditionState[i]);
					}
				}
			}
		}

		return cliHRESULT(hResult);
	}

	cliHRESULT COPCAeServer::EnableConditionByArea (
		/*[in]*/ List<String^>^ Areas )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC A&E Server has been Disposed!");

		HRESULT        hResult = S_OK;

		std::vector<LPWSTR> pszArea(Areas->Count);

		int i = 0;
		try
		{
			for each(String^ area in Areas)
			{
				pszArea[i++] = CHelper::ConvertStringToLPWSTR(area);
			}

			hResult = m_pIOPCEventServer->EnableConditionByArea(Areas->Count, pszArea.data());
		}
		finally
		{
			// free allocated memory
			for (int j = 0; j < i; j++)
			{
				Marshal::FreeHGlobal((IntPtr)pszArea[j]);
			}
		}

		return cliHRESULT(hResult);
	}

	cliHRESULT COPCAeServer::EnableConditionBySource (
		/*[in]*/ List<String^>^ Sources )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC A&E Server has been Disposed!");

		HRESULT        hResult = S_OK;

		std::vector<LPWSTR> pszSources(Sources->Count);
		int i = 0;

		for each(String^ source in Sources)
		{
			pszSources[i++] = CHelper::ConvertStringToLPWSTR(source);
		}

		try
		{
			hResult = m_pIOPCEventServer->EnableConditionBySource(Sources->Count, pszSources.data());
			return cliHRESULT(hResult);
		}
		finally
		{
			// free allocated memory
			for (int j = 0; j < i; j++)
			{
				Marshal::FreeHGlobal((IntPtr)pszSources[j]);
			}
		}
	}

	cliHRESULT COPCAeServer::DisableConditionByArea (
		/*[in]*/ List<String^>^ Areas )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC A&E Server has been Disposed!");

		HRESULT        hResult = S_OK;

		std::vector<LPWSTR> pszArea(Areas->Count);

		int i = 0;

		for each(String^ area in Areas)
		{
			pszArea[i++] = CHelper::ConvertStringToLPWSTR(area);
		}

		try
		{
			hResult = m_pIOPCEventServer->DisableConditionByArea(Areas->Count, pszArea.data());
		}
		finally
		{
			// free allocated memory
			for (int j = 0; j < i; j++)
			{
				Marshal::FreeHGlobal((IntPtr)pszArea[j]);
			}
		}
		
		return cliHRESULT(hResult);
	}

	cliHRESULT COPCAeServer::DisableConditionBySource (
		/*[in]*/ List<String^>^ Sources )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC A&E Server has been Disposed!");

		HRESULT        hResult = S_OK;

		std::vector<LPWSTR> pszSources(Sources->Count);
		int i = 0;

		for each(String^ source in Sources)
		{
			pszSources[i++] = CHelper::ConvertStringToLPWSTR(source);
		}

		try
		{
			hResult = m_pIOPCEventServer->DisableConditionBySource(Sources->Count, pszSources.data());
		}
		finally
		{
			// free allocated memory
			for (int j = 0; j < i; j++)
			{
				Marshal::FreeHGlobal((IntPtr)pszSources[j]);
			}
		}
		
		return cliHRESULT(hResult);
	}

	cliHRESULT COPCAeServer::AckCondition (
		/*[in]*/ String^ sAcknowledgerID,
		/*[in]*/ String^ sComment,
		/*[in]*/ List<OPCEVENTACKCONDITION^>^ AckConditions,
		/*[out]*/ List<HandleAndHRESULT^>^ %ErrorList )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC A&E Server has been Disposed!");

		ErrorList = nullptr;
		HRESULT        hResult = S_OK;

		unsigned long dwCount = AckConditions->Count;
		pin_ptr<const wchar_t> szAcknowledgerID = PtrToStringChars(sAcknowledgerID);
		pin_ptr<const wchar_t> szComment = PtrToStringChars(sComment);

		std::vector<LPWSTR> pszSource(dwCount);
		std::vector<LPWSTR> pszConditionName(dwCount);
		std::vector<_FILETIME> pftActiveTime(dwCount);
		std::vector<DWORD> pdwCookie(dwCount);

		HRESULT* pErrors = nullptr;
		int i = 0;

		try
		{
			for each(OPCEVENTACKCONDITION^ cond in AckConditions)
			{
				pszSource[i] = CHelper::ConvertStringToLPWSTR(cond->sSource);
				pszConditionName[i] = CHelper::ConvertStringToLPWSTR(cond->sConditionName);
				*((__int64*)&pftActiveTime[i]) = cond->dtActiveTime.ToFileTimeUtc();
				pdwCookie[i] = cond->dwCookie;
				i++;
			}

			hResult = m_pIOPCEventServer->AckCondition(dwCount,
				(LPWSTR)szAcknowledgerID,
				(LPWSTR)szComment,
				pszSource.data(),
				pszConditionName.data(),
				pftActiveTime.data(),
				pdwCookie.data(),
				&pErrors);
			if (hResult == S_OK)
			{
				ErrorList = gcnew List<HandleAndHRESULT^>();

				for (unsigned int k = 0; k < dwCount; k++)
				{
					HandleAndHRESULT^ hAndHr = gcnew HandleAndHRESULT();
					hAndHr->Handle = 0;
					hAndHr->hResult = pErrors[k];
					ErrorList->Add(hAndHr);
				}

				if (pErrors != nullptr)
				{
					::CoTaskMemFree(pErrors);
					pErrors = nullptr;
				}
			}

			return cliHRESULT(hResult);
		}
		finally
		{
			for (int j = 0; j < i; j++)
			{
				Marshal::FreeHGlobal((IntPtr)pszSource[j]);
				Marshal::FreeHGlobal((IntPtr)pszConditionName[j]);
			}
		}
	}

	cliHRESULT COPCAeServer::CreateAreaBrowser (
		/*[out]*/ IOPCEventAreaBrowserCli^ %iOPCEventAreaBrowser)
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC A&E Server has been Disposed!");

		cliHRESULT hResult = E_FAIL;
		iOPCEventAreaBrowser = nullptr;
		IOPCEventAreaBrowser* pIAreaBrowser = nullptr;
		hResult = m_pIOPCEventServer->CreateAreaBrowser((GUID*)&__uuidof(IOPCEventAreaBrowser), (IUnknown **)&pIAreaBrowser);
		if ( hResult.Succeeded )
		{
			COPCAeAreaBrowser ^ rCOPCAeAreaBrowser = gcnew COPCAeAreaBrowser( pIAreaBrowser );
			iOPCEventAreaBrowser = dynamic_cast<IOPCEventAreaBrowserCli^>(rCOPCAeAreaBrowser);
		}
		return hResult;
	}

	// IAdviseOPCShutdownCli
	cliHRESULT COPCAeServer::AdviseShutdownRequest(
		/*[in]*/ OPCShutdown::ShutdownRequest^ shutdownRequest)
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC A&E Server has been Disposed!");

		m_shutdownRequest += shutdownRequest;
		return cliHRESULT(S_OK);
	}

	cliHRESULT COPCAeServer::UnadviseShutdownRequest(
		/*[in]*/ OPCShutdown::ShutdownRequest^ shutdownRequest)
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC A&E Server has been Disposed!");

		m_shutdownRequest -= shutdownRequest;
		return cliHRESULT(S_OK);
	}

}}}}
