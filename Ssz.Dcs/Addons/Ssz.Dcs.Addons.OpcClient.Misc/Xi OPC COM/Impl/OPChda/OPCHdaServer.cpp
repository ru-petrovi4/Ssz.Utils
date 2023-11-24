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
#include "OPCHdaServer.h"
#include "OPCHdaBrowser.h"
#include "OPCHdaShutdownCallback.h"
#include "..\Helper.h"

using namespace System::Runtime::InteropServices;
using namespace System::Diagnostics;

using namespace Xi::Contracts;
using namespace Xi::Contracts::Data;
using namespace Xi::Contracts::Constants;
using namespace Xi::Common::Support;

namespace Xi {
namespace OPC {
namespace COM {
namespace Impl {

	COPCHdaServer::COPCHdaServer(cliHRESULT %HR, ::IOPCHDA_Server * pIOPCHDA_Server)
		: m_pIOPCHDA_Server(pIOPCHDA_Server)
		, m_pIOPCCommon(nullptr)
		, m_pIOPCHDA_SyncRead(nullptr)
		, m_bHasBeenDisposed(false)
		, m_pIOPCShutdown(nullptr)
		, m_shutdownRequest(nullptr)
	{
		::IOPCCommon * pIOPCCommon = nullptr;
		HR = pIOPCHDA_Server->QueryInterface(IID_IOPCCommon, reinterpret_cast<void**>(&pIOPCCommon));
		if ( HR.Succeeded )
		{
			m_pIOPCCommon = pIOPCCommon;
		}
		::IOPCHDA_SyncRead * pIOPCHDA_SyncRead = nullptr;
		HR = pIOPCHDA_Server->QueryInterface(IID_IOPCHDA_SyncRead, reinterpret_cast<void**>(&pIOPCHDA_SyncRead));
		if ( HR.Succeeded )
		{
			m_pIOPCHDA_SyncRead = pIOPCHDA_SyncRead;
		}

		::IOPCShutdown * pIOPCShutdown = nullptr;
		cliHRESULT HR1 = COPCHdaShutdownCallback::CreateInstance( reinterpret_cast<::IOPCShutdown**>(&pIOPCShutdown) );
		if (HR1.Succeeded)
		{
			m_gcHandleTHIS = GCHandle::Alloc(this);
			m_pIOPCShutdown = pIOPCShutdown;
			((COPCHdaShutdownCallback*)pIOPCShutdown)->SetOpcServerGCHandle(m_gcHandleTHIS);
			CComQIPtr<IConnectionPointContainer, &IID_IConnectionPointContainer> pICPC( m_pIOPCHDA_Server );
			if (nullptr != pICPC.p)
			{
				::IConnectionPoint * pIConnectionPoint = nullptr;
				cliHRESULT HR2 = pICPC->FindConnectionPoint( IID_IOPCShutdown, &pIConnectionPoint );
				if (HR2.Succeeded && nullptr != pIConnectionPoint)
				{
					ULONG dwCookie = 0;
					cliHRESULT HR3 = pIConnectionPoint->Advise( m_pIOPCShutdown, &dwCookie );
					if (HR3.Failed)
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

	COPCHdaServer::~COPCHdaServer(void)
	{
		if (DisposeThis(true))
			GC::SuppressFinalize(this);
	}

	COPCHdaServer::!COPCHdaServer(void)
	{
		DisposeThis(false);
	}

	bool COPCHdaServer::DisposeThis(bool isDisposing)
	{
		if (m_bHasBeenDisposed)
			return false;

		CComQIPtr<IConnectionPointContainer, &IID_IConnectionPointContainer> pICPC( m_pIOPCHDA_Server );
		if (nullptr != pICPC.p)
		{
			::IConnectionPoint * pIConnectionPoint = nullptr;
			cliHRESULT HR1 = pICPC->FindConnectionPoint( IID_IOPCShutdown, &pIConnectionPoint );
			_ASSERT(HR1.Succeeded && nullptr != pIConnectionPoint);
			if (HR1.Succeeded && nullptr != pIConnectionPoint)
			{
				cliHRESULT HR2 = pIConnectionPoint->Unadvise(m_dwShutdownAdviseCookie);
				_ASSERT(HR2.Succeeded);
				pIConnectionPoint->Release();
			}
		}
		m_gcHandleTHIS.Free();

		if (nullptr != m_pIOPCHDA_Server)
			m_pIOPCHDA_Server->Release();
		m_pIOPCHDA_Server = nullptr;

		if (nullptr != m_pIOPCCommon)
			m_pIOPCCommon->Release();
		m_pIOPCCommon = nullptr;

		if (nullptr != m_pIOPCHDA_SyncRead)
			m_pIOPCHDA_SyncRead->Release();
		m_pIOPCHDA_SyncRead = nullptr;

		m_bHasBeenDisposed = true;
		return true;
	}

	// IOPCCommon
	cliHRESULT COPCHdaServer::SetLocaleID(
		/*[in]*/ unsigned int dwLcid )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC HDA Server has been Disposed!");

		return IOPCCommon->SetLocaleID(dwLcid);
	}

	cliHRESULT COPCHdaServer::GetLocaleID(
		/*[out]*/ unsigned int %dwLcid )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC HDA Server has been Disposed!");

		unsigned long ulLocaleID = 0;
		HRESULT hr = IOPCCommon->GetLocaleID(&ulLocaleID);

		if( hr == S_OK )
		{
			dwLcid = ulLocaleID;
		}

		return cliHRESULT(hr);
	}

	cliHRESULT COPCHdaServer::QueryAvailableLocaleIDs(
		/*[out]*/ List<unsigned int>^ %dwLcid )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC HDA Server has been Disposed!");

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

	cliHRESULT COPCHdaServer::GetErrorString(
		/*[in]*/ cliHRESULT dwError,
		/*[out]*/ String^ %errString )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC HDA Server has been Disposed!");

		HRESULT hr = S_OK;
		errString = nullptr;
		LPWSTR pErrorString = nullptr;
		hr = IOPCCommon->GetErrorString( dwError.hResult, &pErrorString );
		if( SUCCEEDED(hr) )
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

	cliHRESULT COPCHdaServer::SetClientName(
		/*[in]*/ String^ zName )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC HDA Server has been Disposed!");

		LPWSTR szName = CHelper::ConvertStringToLPWSTR(zName);
		HRESULT hr = IOPCCommon->SetClientName(szName);
		Marshal::FreeHGlobal((IntPtr)szName);
		return cliHRESULT(hr);
	}

	// IOPCHdaServer
	cliHRESULT COPCHdaServer::GetItemAttributes(
		/*[out]*/ List<OPCHDAITEMATTR^>^ %HDAItemAttributes)
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC HDA Server has been Disposed!");

		HRESULT hResult = S_OK;
		// get the supported History Attributes
		DWORD   i;
		DWORD   dwCount = 0;
		DWORD  *pdwID   = 0;
		LPWSTR *pszName = 0;
		LPWSTR *pszDesc = 0;
		VARTYPE *pvtAttrDataType;
		hResult = IOPCHDA_Server->GetItemAttributes(&dwCount,
			&pdwID,
			&pszName,
			&pszDesc,
			&pvtAttrDataType );
		if (S_OK == hResult)
		{
			HDAItemAttributes = gcnew List<OPCHDAITEMATTR^>(dwCount);
			OPCHDAITEMATTR^ aggDef;
			for (i = 0; i < dwCount; i++)
			{
				aggDef = gcnew OPCHDAITEMATTR();
				aggDef->dwAttrID = pdwID[i];
				aggDef->sAttrName = gcnew String(pszName[i]);
				aggDef->sAttrDesc = gcnew String(pszDesc[i]);
				aggDef->vtAttrDataType = pvtAttrDataType[i];
				
				if (pszName[i] != nullptr)
				{
					::CoTaskMemFree(pszName[i]);
					pszName[i] = nullptr;
				}

				if (pszDesc[i] != nullptr)
				{
					::CoTaskMemFree(pszDesc[i]);
					pszDesc[i] = nullptr;
				}

				HDAItemAttributes->Add(aggDef);
			}

			// free memory from the COM call
			if (pdwID != nullptr)
			{
				::CoTaskMemFree(pdwID);
				pdwID = nullptr;
			}

			if (pszName != nullptr)
			{
				::CoTaskMemFree(pszName);
				pszName = nullptr;
			}

			if (pszDesc != nullptr)
			{
				::CoTaskMemFree(pszDesc);
				pszDesc = nullptr;
			}

			if (pvtAttrDataType != nullptr)
			{
				::CoTaskMemFree(pvtAttrDataType);
				pvtAttrDataType = nullptr;
			}
		}

		return cliHRESULT(hResult);
	}

	cliHRESULT COPCHdaServer::GetAggregates(
		/*[out]*/ List<OPCHDAAGGREGATES^>^ %HDAAggregates)
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC HDA Server has been Disposed!");

		HRESULT hResult = S_OK;
		// get the supported History Aggregates
		DWORD   i;
		DWORD   dwCount = 0;
		DWORD  *pdwID   = 0;
		LPWSTR *pszName = 0;
		LPWSTR *pszDesc = 0;
		hResult = IOPCHDA_Server->GetAggregates(&dwCount,
			&pdwID,
			&pszName,
			&pszDesc );
		if (S_OK == hResult)
		{
			HDAAggregates = gcnew List<OPCHDAAGGREGATES^>(dwCount);
			OPCHDAAGGREGATES^ aggDef;
			for (i = 0; i < dwCount; i++)
			{
				aggDef = gcnew OPCHDAAGGREGATES();
				aggDef->dwAggrID = pdwID[i];
				aggDef->sAggrName = gcnew String(pszName[i]);
				aggDef->sAggrDesc = gcnew String(pszDesc[i]);
				HDAAggregates->Add(aggDef);

				if (pszName[i] != nullptr)
				{
					::CoTaskMemFree(pszName[i]);
					pszName[i] = nullptr;
				}

				if (pszDesc[i] != nullptr)
				{
					::CoTaskMemFree(pszDesc[i]);
					pszDesc[i] = nullptr;
				}
			}
			// free memory from the COM call
			if (pdwID != nullptr)
			{
				::CoTaskMemFree(pdwID);
				pdwID = nullptr;
			}

			if (pszName != nullptr)
			{
				::CoTaskMemFree(pszName);
				pszName = nullptr;
			}

			if (pszDesc != nullptr)
			{
				::CoTaskMemFree(pszDesc);
				pszDesc = nullptr;
			}
		}
		return cliHRESULT(hResult);
	}

	cliHRESULT COPCHdaServer::GetHistorianStatus(
		/*[out]*/ OPCHDA_SERVERSTATUS %wStatus,
		/*[out]*/ DateTime %dtCurrentTime,
		/*[out]*/ DateTime %dtStartTime,
		/*[out]*/ unsigned short %wMajorVersion,
		/*[out]*/ unsigned short %wMinorVersion,
		/*[out]*/ unsigned short %wBuildNumber,
		/*[out]*/ unsigned int %dwMaxReturnValues,
		/*[out]*/ String^ %sStatusString,
		/*[out]*/ String^ %sVendorInfo)
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC HDA Server has been Disposed!");

		tagOPCHDA_SERVERSTATUS wHdaStatus = OPCHDA_INDETERMINATE;
		::FILETIME *pftCurrentTime = NULL;
		::FILETIME *pftStartTime = NULL;
		USHORT pwMajorVersion = 0;
		USHORT pwMinorVersion = 0;
		USHORT pwBuildNumber = 0;
		ULONG pdwMaxReturnValues = 0;
		LPWSTR szStatusString = NULL;
		LPWSTR szVendorInfo = NULL;

		HRESULT hr = IOPCHDA_Server->GetHistorianStatus(&wHdaStatus,
			&pftCurrentTime,
			&pftStartTime,
			&pwMajorVersion,
			&pwMinorVersion,
			&pwBuildNumber,
			&pdwMaxReturnValues,
			&szStatusString,
			&szVendorInfo);

		if (S_OK == hr &&
			NULL != pftCurrentTime && NULL != pftStartTime &&
			NULL != szStatusString && NULL != szVendorInfo )
		{
			wStatus = (OPCHDA_SERVERSTATUS)wHdaStatus;
			dtStartTime = DateTime::FromFileTimeUtc(*((__int64*)(pftStartTime)));
			dtCurrentTime = DateTime::FromFileTimeUtc(*((__int64*)(pftCurrentTime)));
			wMajorVersion = pwMajorVersion;
			wMinorVersion = pwMinorVersion;
			wBuildNumber = pwBuildNumber;
			dwMaxReturnValues = pdwMaxReturnValues;
			String^ hdaStatusString = gcnew String(szStatusString);
			sStatusString = hdaStatusString;
			String^ hdaVndrInfoString = gcnew String(szVendorInfo);
			sVendorInfo = hdaVndrInfoString;
		}
		// free memory from the COM call
		
		if (pftCurrentTime != nullptr) 
		{
			::CoTaskMemFree(pftCurrentTime);
			pftCurrentTime = NULL;
		}

		if (pftStartTime != nullptr) 
		{
			::CoTaskMemFree(pftStartTime);
			pftStartTime = NULL;
		}

		if (szStatusString != nullptr) 
		{
			::CoTaskMemFree(szStatusString);
			szStatusString = nullptr;
		}

		if (szVendorInfo != nullptr) 
		{
			::CoTaskMemFree(szVendorInfo);
			szVendorInfo = nullptr;
		}

		return cliHRESULT(hr);
	}

	cliHRESULT COPCHdaServer::GetItemHandles(
		/*[in]*/ List<OPCHDA_ITEMDEF^>^ hClientAndItemID,
		/*[out]*/ List<OPCHDAITEMRESULT^>^ %hServerAndHResult)
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC HDA Server has been Disposed!");

		unsigned long *phServer;
		HRESULT *pHR = NULL;

		// move the itemID strings and client handles to the format needed for the COM call
		unsigned long dwNumItems = hClientAndItemID->Count;

		LPWSTR *pszItemIDs;    // for the beginning address
		LPWSTR *pszItemIDsPtr; // for iterating through the array
		pszItemIDs = pszItemIDsPtr = new LPWSTR[hClientAndItemID->Count];

		ULONG *phClient;       // for the beginning address
		ULONG *phClientPtr;    // for iterating through the array
		phClient = phClientPtr = new ULONG[hClientAndItemID->Count];

		for each(OPCHDA_ITEMDEF^ itemId in hClientAndItemID)
		{
			*pszItemIDsPtr++ = CHelper::ConvertStringToLPWSTR(itemId->sItemID);
			*phClientPtr++ = itemId->hClient;
		}

		HRESULT hr = IOPCHDA_Server->GetItemHandles(dwNumItems,
			pszItemIDs,
			phClient,
			&phServer,
			&pHR);

		// convert and store the received handles and HRs to the list
		hServerAndHResult = gcnew List<OPCHDAITEMRESULT^>(hClientAndItemID->Count);
		for(unsigned long idx = 0; idx < dwNumItems; idx++)
		{
			OPCHDAITEMRESULT^ itemHandlesAndHr = gcnew OPCHDAITEMRESULT();
			itemHandlesAndHr->hClient = phClient[idx];
			itemHandlesAndHr->hServer = phServer[idx];
			itemHandlesAndHr->HResult = cliHRESULT(pHR[idx]);
			hServerAndHResult->Add(itemHandlesAndHr);
		}

		// free the un-managed memory
		for(unsigned long i = 0; i < dwNumItems; i++)
		{
			Marshal::FreeHGlobal((IntPtr)pszItemIDs[i]);
		}

		if (pszItemIDs != nullptr)
		{
			delete [] pszItemIDs;
			pszItemIDs = nullptr;
		}

		if (phClient != nullptr)
		{
			delete [] phClient;
			phClient = nullptr;
		}

		// free memory from the COM call
		if (phServer != nullptr)
		{
			::CoTaskMemFree(phServer);
			phServer = nullptr;
		}

		if (pHR != nullptr)
		{
			CoTaskMemFree(pHR);
			pHR = nullptr;
		}

		return cliHRESULT(hr);
	}

	cliHRESULT COPCHdaServer::ReleaseItemHandles(
		/*[in]*/ List<unsigned int>^ hServer,
		/*[out]*/ List<HandleAndHRESULT^>^ %ErrorsList)
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC HDA Server has been Disposed!");

		HRESULT *pHR;
		// move the server handles to the format needed for the COM call
		unsigned long dwNumItems = hServer->Count;
		unsigned long *phServer;    // for the beginning address
		unsigned long *phServerPtr; // for iterating through the array
		phServer = phServerPtr = new unsigned long[dwNumItems];
		for each(unsigned int serverHandle in hServer)
		{
			*phServerPtr++ = serverHandle;
		}

		HRESULT hr = IOPCHDA_Server->ReleaseItemHandles(dwNumItems, phServer, &pHR);

		// convert and store the received errors to the list
		ErrorsList = gcnew List<HandleAndHRESULT^>(hServer->Count);
		for(unsigned long i = 0; i < dwNumItems; i++)
		{
			HandleAndHRESULT^ itemHandlesAndHr = gcnew HandleAndHRESULT();
			itemHandlesAndHr->Handle = hServer[i];
			itemHandlesAndHr->hResult = cliHRESULT(pHR[i]);
			ErrorsList->Add(itemHandlesAndHr);
		}

		// free the un-managed memory
		if (phServer != nullptr)
		{
			delete [] phServer;
			phServer = nullptr;
		}

		// free memory from the COM call
		if (pHR != nullptr)
		{
			::CoTaskMemFree(pHR);
			pHR = nullptr;
		}

		return cliHRESULT(hr);
	}

	cliHRESULT COPCHdaServer::ValidateItemIDs(
		/*[in]*/ List<String^>^ sItemID,
		/*[out]*/ List<HandleAndHRESULT^>^ %ErrorsList)
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC HDA Server has been Disposed!");

		HRESULT *pHR;
		// move the itemID strings to the format needed for the COM call
		unsigned long dwNumItems = sItemID->Count;
		LPWSTR *pszItemIDs;    // for the beginning address
		LPWSTR *pszItemIDsPtr; // for iterating through the array
		pszItemIDs = pszItemIDsPtr = new LPWSTR[dwNumItems];
		for each(String^ ItemIdString in sItemID)
		{
			*pszItemIDsPtr++ = CHelper::ConvertStringToLPWSTR(ItemIdString);
		}

		HRESULT hr = IOPCHDA_Server->ValidateItemIDs(dwNumItems,
			pszItemIDs,
			&pHR );

		// convert and store the received errors to the list
		ErrorsList = gcnew List<HandleAndHRESULT^>(sItemID->Count);
		for(unsigned long i = 0; i < dwNumItems; i++)
		{
			HandleAndHRESULT^ itemHandlesAndHr = gcnew HandleAndHRESULT();
			itemHandlesAndHr->hResult = cliHRESULT(pHR[i]);
			ErrorsList->Add(itemHandlesAndHr);
		}

		// free the un-managed memory
		for(unsigned long i = 0; i < dwNumItems; i++)
		{
			Marshal::FreeHGlobal((IntPtr)pszItemIDs[i]);
		}

		if (pszItemIDs != nullptr)
		{
			delete [] pszItemIDs;
			pszItemIDs = nullptr;
		}

		// free memory from the COM call
		if (pHR != nullptr)
		{
			::CoTaskMemFree(pHR);
			pHR = nullptr;
		}

		return cliHRESULT(hr);
	}

	cliHRESULT COPCHdaServer::CreateBrowse(
		/*[in]*/ List<OPCHDA_BROWSEFILTER^>^ BrowseFilters,
		/*[out]*/ IOPCHDA_BrowserCli^ %iBrowser,
		/*[out]*/ List<HandleAndHRESULT^>^ %ErrorsList)
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC HDA Server has been Disposed!");

		cliHRESULT HR = E_FAIL;
		iBrowser = nullptr;
		ErrorsList = nullptr;

		// move the browse parameters to the format needed for the COM call
		unsigned long dwNumFilters = 0;
		unsigned long *pdwAttrID = 0;  // ptr to array of ids
		VARIANT *vFilters;                    // ptr to array of values
		tagOPCHDA_OPERATORCODES *pOperators;  // ptr to array of op codes
		if (BrowseFilters != nullptr)
		{
			dwNumFilters = BrowseFilters->Count;

			unsigned long *pdwAttrIDPtr;              // for iterating through the array
			pdwAttrID = pdwAttrIDPtr = new unsigned long[dwNumFilters];
			::ZeroMemory(pdwAttrID, (sizeof(unsigned long) * dwNumFilters));

			tagOPCHDA_OPERATORCODES *pOperatorsPtr;      // for iterating through the array
			pOperators = pOperatorsPtr = new tagOPCHDA_OPERATORCODES[dwNumFilters];
			::ZeroMemory(pOperators, (sizeof(tagOPCHDA_OPERATORCODES) * dwNumFilters));

			vFilters = new VARIANT[dwNumFilters];
			::ZeroMemory(vFilters, (sizeof(VARIANT) * dwNumFilters));

			for (int idx = 0; idx < BrowseFilters->Count; idx++)
			{
				pdwAttrIDPtr[idx] = BrowseFilters[idx]->dwAttrID;
				pOperatorsPtr[idx] = (tagOPCHDA_OPERATORCODES)BrowseFilters[idx]->FilterOperator;
				// move the filter string to the variant BSTR
				::VariantInit(&vFilters[idx]);
				CHelper::ConvertToVARIANT(BrowseFilters[idx]->FilterValue, &vFilters[idx]);
			}
		}
		else // no browse filters so default the arrays to contain 0s
		{
			pdwAttrID = new unsigned long[1];
			pdwAttrID[0] = 0; // default the array to empty
			pOperators = new tagOPCHDA_OPERATORCODES[1];
			pOperators[0] = (tagOPCHDA_OPERATORCODES)0;
			vFilters = new VARIANT[1];
			::ZeroMemory(vFilters, (sizeof(VARIANT)));
		}
		::IOPCHDA_Browser * pIOPCHDA_Browser = nullptr;
		HRESULT *pHRs = nullptr;
		HR = m_pIOPCHDA_Server->CreateBrowse(dwNumFilters,
			pdwAttrID,
			pOperators,
			vFilters,
			&pIOPCHDA_Browser,
			&pHRs);
		if (HR.Succeeded)
		{
			// create an instance of the IOPCHDA_Browser
			COPCHdaBrowser^ rCOPCHdaBrowser = gcnew COPCHdaBrowser(HR, pIOPCHDA_Browser);
			iBrowser = dynamic_cast<IOPCHDA_BrowserCli^>(rCOPCHdaBrowser);
			if (pHRs != nullptr)
			{
				ErrorsList = gcnew List<HandleAndHRESULT^>();
				for (unsigned long idx = 0; idx < dwNumFilters; idx++)
				{
					if (S_OK != pHRs[idx])
					{
						HandleAndHRESULT ^hdlAndHR = gcnew HandleAndHRESULT();
						hdlAndHR->Handle = idx;
						hdlAndHR->hResult = pHRs[idx];
						ErrorsList->Add(hdlAndHR);
					}
				}
			}
		}

		// free the un-managed memory
		if (nullptr != pHRs)
		{
			::CoTaskMemFree(pHRs);
			pHRs = nullptr;
		}

		if (pdwAttrID != nullptr)
		{
			delete [] pdwAttrID;
			pdwAttrID = nullptr;
		}

		if (pOperators != nullptr)
		{
			delete [] pOperators;
			pOperators = nullptr;
		}

		dwNumFilters = (dwNumFilters > 0)?dwNumFilters:1;
		for(unsigned long i = 0; i < dwNumFilters; i++)
		{
			::VariantClear(&vFilters[i]);
		}

		if (vFilters != nullptr)
		{
			delete [] vFilters;
			vFilters = nullptr;
		}

		return HR;
	}

	// IOPCHDA_SyncRead
	cliHRESULT COPCHdaServer::ReadRaw(
		/*[in,out]*/ OPCHDA_TIME^ %cliStartTime,
		/*[in,out]*/ OPCHDA_TIME^ %cliEndTime,
		/*[in]*/ unsigned int dwNumValues,
		/*[in]*/ bool bBounds,
		/*[in]*/ List<unsigned int>^ hServer,
		/*[out]*/ array<JournalDataValues^>^ %ItemValues)
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC HDA Server has been Disposed!");

		tagOPCHDA_TIME hdaStartTime;
		tagOPCHDA_TIME hdaEndTime;
		CHelper::cliHdaTimeToHdaTime(cliStartTime, &hdaStartTime);
		CHelper::cliHdaTimeToHdaTime(cliEndTime, &hdaEndTime);

		unsigned long dwNumItems = hServer->Count;
		// Need array of aggregate for the conversion later
		DWORD *haAggregate;         // for the beginning address
		DWORD *haAggregatePtr;      // for iterating through the array
		haAggregate = haAggregatePtr = new DWORD[dwNumItems];

		// move the server handles to the format needed for the COM call
		unsigned long *phServer;    // for the beginning address
		unsigned long *phServerPtr; // for iterating through the array
		phServer = phServerPtr = new unsigned long[dwNumItems];
		for each(unsigned int serverHandle in hServer)
		{
			*phServerPtr++ = serverHandle;
			*haAggregatePtr++ = JournalDataSampleTypes::RawDataSamples;
		}

		tagOPCHDA_ITEM *pItemValues = nullptr;
		HRESULT *pHR = nullptr;
		HRESULT hr = m_pIOPCHDA_SyncRead->ReadRaw(
			&hdaStartTime,
			&hdaEndTime,
			dwNumValues,
			bBounds,
			dwNumItems,
			phServer,
			&pItemValues,
			&pHR);
		if (SUCCEEDED(hr) && nullptr != pItemValues)
		{
			CHelper::HdaTimeToCliHdaTime(&hdaStartTime, cliStartTime);
			CHelper::HdaTimeToCliHdaTime(&hdaEndTime, cliEndTime);

			unsigned int resultCode = ConvertToJournalDataValues(
				cliStartTime->dtTime, cliEndTime->dtTime, dwNumItems,
				pItemValues, haAggregate, pHR, ItemValues);

			hr = (S_OK != hr) ? hr : resultCode;
		}

		// free the un-managed memory
		if (haAggregate != nullptr)
		{
			delete [] haAggregate;
			haAggregate = nullptr;
		}

		if (phServer != nullptr)
		{
			delete [] phServer;
			phServer = nullptr;
		}

		// free memory from the COM call
		if (pItemValues != nullptr)
		{
			for (unsigned long idxItems = 0; idxItems < dwNumItems; idxItems++)
			{
				if (pItemValues[idxItems].pdwQualities != nullptr)
				{
					::CoTaskMemFree(pItemValues[idxItems].pdwQualities);
					pItemValues[idxItems].pdwQualities = nullptr;
				}

				if (pItemValues[idxItems].pftTimeStamps != nullptr)
				{
					::CoTaskMemFree(pItemValues[idxItems].pftTimeStamps);
					pItemValues[idxItems].pftTimeStamps = nullptr;
				}

				if (pItemValues[idxItems].pvDataValues != nullptr)
				{
					::CoTaskMemFree(pItemValues[idxItems].pvDataValues);
					pItemValues[idxItems].pvDataValues = nullptr;
				}
			}
			::CoTaskMemFree(pItemValues);
			pItemValues = nullptr;
		}

		if (pHR != nullptr)
		{
			::CoTaskMemFree(pHR);
			pHR = nullptr;
		}

		return cliHRESULT(hr);
	}

	cliHRESULT COPCHdaServer::ReadProcessed(
		/*[in,out]*/ OPCHDA_TIME^ %cliStartTime,
		/*[in,out]*/ OPCHDA_TIME^ %cliEndTime,
		/*[in]*/ TimeSpan dtResampleInterval,
		/*[in]*/ List<OPCHDA_HANDLEAGGREGATE^>^ HandleAggregate,
		/*[out]*/ array<JournalDataValues^>^ %ItemValues)
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC HDA Server has been Disposed!");

		tagOPCHDA_TIME hdaStartTime;
		tagOPCHDA_TIME hdaEndTime;
		CHelper::cliHdaTimeToHdaTime(cliStartTime, &hdaStartTime);
		CHelper::cliHdaTimeToHdaTime(cliEndTime, &hdaEndTime);

		_FILETIME ftResampleInterval;
		*((__int64*)&ftResampleInterval) = dtResampleInterval.Ticks;

		// move the aggregate and server handles to the format needed for the COM call
		unsigned long dwNumItems = HandleAggregate->Count;

		DWORD *haAggregate;         // for the beginning address
		DWORD *haAggregatePtr;      // for iterating through the array
		haAggregate = haAggregatePtr = new DWORD[dwNumItems];

		unsigned long *phServer;    // for the beginning address
		unsigned long *phServerPtr; // for iterating through the array
		phServer = phServerPtr = new unsigned long[dwNumItems];

		for each(OPCHDA_HANDLEAGGREGATE^ aggregateHandle in HandleAggregate)
		{
			*haAggregatePtr++ = aggregateHandle->haAggregate;
			*phServerPtr++ = aggregateHandle->hServer;
		}

		tagOPCHDA_ITEM *pItemValues = nullptr;
		HRESULT *pHR = nullptr;
		HRESULT hr = m_pIOPCHDA_SyncRead->ReadProcessed(
			&hdaStartTime,
			&hdaEndTime,
			ftResampleInterval,
			dwNumItems,
			phServer,
			haAggregate,
			&pItemValues,
			&pHR);

		if (SUCCEEDED(hr) && nullptr != pItemValues)
		{
			CHelper::HdaTimeToCliHdaTime(&hdaStartTime, cliStartTime);
			CHelper::HdaTimeToCliHdaTime(&hdaEndTime, cliEndTime);

			unsigned int resultCode = ConvertToJournalDataValues(
				cliStartTime->dtTime, cliEndTime->dtTime, dwNumItems,
				pItemValues, haAggregate, pHR, ItemValues);

			hr = (S_OK != hr) ? hr : resultCode;
		}

		// free the un-managed memory
		if (haAggregate != nullptr)
		{
			delete [] haAggregate;
			haAggregate = nullptr;
		}

		if (phServer != nullptr)
		{
			delete [] phServer;
			phServer = nullptr;
		}

		// free memory from the COM call
		if (pItemValues != nullptr)
		{
			for (unsigned long idxItems = 0; idxItems < dwNumItems; idxItems++)
			{
				if (pItemValues[idxItems].pdwQualities != nullptr)
				{
					::CoTaskMemFree(pItemValues[idxItems].pdwQualities);
					pItemValues[idxItems].pdwQualities = nullptr;
				}

				if (pItemValues[idxItems].pftTimeStamps != nullptr)
				{
					::CoTaskMemFree(pItemValues[idxItems].pftTimeStamps);
					pItemValues[idxItems].pftTimeStamps = nullptr;
				}

				if (pItemValues[idxItems].pvDataValues != nullptr)
				{
					::CoTaskMemFree(pItemValues[idxItems].pvDataValues);
					pItemValues[idxItems].pvDataValues = nullptr;
				}
			}
			::CoTaskMemFree(pItemValues);
			pItemValues = nullptr;
		}

		if (pHR != nullptr)
		{
			::CoTaskMemFree(pHR);
			pHR = nullptr;
		}

		return cliHRESULT(hr);
	}

	cliHRESULT COPCHdaServer::ReadAtTime(
		/*[in]*/ List<DateTime>^ dtTimeStamps,
		/*[in]*/ List<unsigned int>^ hServer,
		/*[out]*/ array<JournalDataValues^>^ %ItemValues)
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC HDA Server has been Disposed!");

		unsigned long dwNumTimeStamps = dtTimeStamps->Count;
		_FILETIME * pftTimeStamps = new _FILETIME[dwNumTimeStamps];
		__int64 * pftTimeStampsPtr = (__int64*)pftTimeStamps;
		for each (DateTime dt in dtTimeStamps)
		{
			*pftTimeStampsPtr++ = dt.ToFileTimeUtc();
		}
		unsigned long dwNumItems = hServer->Count;

		// Need array of aggregate for the conversion later
		DWORD *haAggregate;         // for the beginning address
		DWORD *haAggregatePtr;      // for iterating through the array
		haAggregate = haAggregatePtr = new DWORD[dwNumItems];

		// move the server handles to the format needed for the COM call
		unsigned long * phServer = new unsigned long[dwNumItems];
		unsigned long * phServerPtr = phServer;
		for each (unsigned long hSvr in hServer)
		{
			*phServerPtr++ = hSvr;
			*haAggregatePtr++ = JournalDataSampleTypes::AtTimeDataSamples;
		}

		tagOPCHDA_ITEM * pItemValues = nullptr;
		HRESULT * pErrors = nullptr;
		HRESULT hr = m_pIOPCHDA_SyncRead->ReadAtTime(
			dwNumTimeStamps,
			pftTimeStamps,
			dwNumItems,
			phServer,
			&pItemValues,
			&pErrors);

		if (SUCCEEDED(hr) && nullptr != pItemValues)
		{
			unsigned int resultCode = ConvertToJournalDataValues(
				DateTime::MinValue, DateTime::MaxValue, dwNumItems,
				pItemValues, haAggregate, pErrors, ItemValues);

			hr = (S_OK != hr) ? hr : resultCode;
		}

		// free the un-managed memory
		if (pftTimeStamps != nullptr)
		{
			delete [] pftTimeStamps;
			pftTimeStamps = nullptr;
		}

		if (haAggregate != nullptr)
		{
			delete [] haAggregate;
			haAggregate = nullptr;
		}

		if (phServer != nullptr)
		{
			delete [] phServer;
			phServer = nullptr;
		}

		// free memory from the COM call
		if (pItemValues != nullptr)
		{
			for (unsigned long idxItems = 0; idxItems < dwNumItems; idxItems++)
			{
				if (pItemValues[idxItems].pdwQualities != nullptr)
				{
					::CoTaskMemFree(pItemValues[idxItems].pdwQualities);
					pItemValues[idxItems].pdwQualities = nullptr;
				}

				if (pItemValues[idxItems].pftTimeStamps != nullptr)
				{
					::CoTaskMemFree(pItemValues[idxItems].pftTimeStamps);
					pItemValues[idxItems].pftTimeStamps = nullptr;
				}

				if (pItemValues[idxItems].pvDataValues != nullptr)
				{
					::CoTaskMemFree(pItemValues[idxItems].pvDataValues);
					pItemValues[idxItems].pvDataValues = nullptr;
				}
			}
			::CoTaskMemFree(pItemValues);
			pItemValues = nullptr;
		}

		if (pErrors != nullptr)
		{
			::CoTaskMemFree(pErrors);
			pErrors = nullptr;
		}

		return cliHRESULT(hr);
	}

	cliHRESULT COPCHdaServer::ReadModified(
		/*[in,out]*/ OPCHDA_TIME^ %cliStartTime,
		/*[in,out]*/ OPCHDA_TIME^ %cliEndTime,
		/*[in]*/ unsigned int dwNumValues,
		/*[in]*/ List<unsigned int>^ hServer,
		/*[out]*/ array<JournalDataChangedValues^>^ %ItemValues)
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC HDA Server has been Disposed!");

		tagOPCHDA_TIME hdaStartTime;
		tagOPCHDA_TIME hdaEndTime;
		CHelper::cliHdaTimeToHdaTime(cliStartTime, &hdaStartTime);
		CHelper::cliHdaTimeToHdaTime(cliEndTime, &hdaEndTime);

		// move the server handles to the format needed for the COM call
		unsigned long dwNumItems = hServer->Count;
		unsigned long *phServer;    // for the beginning address
		unsigned long *phServerPtr; // for iterating through the array
		phServer = phServerPtr = new unsigned long[dwNumItems];
		for each(unsigned int serverHandle in hServer)
		{
			*phServerPtr++ = serverHandle;
		}

		tagOPCHDA_MODIFIEDITEM *pItemValues = nullptr;
		HRESULT *pHR = nullptr;
		HRESULT hr = m_pIOPCHDA_SyncRead->ReadModified(
			&hdaStartTime,
			&hdaEndTime,
			dwNumValues,
			dwNumItems,
			phServer,
			&pItemValues,
			&pHR);

		if (SUCCEEDED(hr) && nullptr != pItemValues)
		{
			CHelper::HdaTimeToCliHdaTime(&hdaStartTime, cliStartTime);
			CHelper::HdaTimeToCliHdaTime(&hdaEndTime, cliEndTime);

			unsigned int resultCode = S_OK;
			ItemValues = gcnew array<JournalDataChangedValues^>(dwNumItems);

			long dataTypeCounts[(short)TransportDataType::MaxTransportDataType];
			for (unsigned long idxItems = 0; idxItems < dwNumItems; idxItems++)
			{
				::ZeroMemory(dataTypeCounts, sizeof(dataTypeCounts));
				if (0 != pItemValues[idxItems].dwCount)
				{
					// Count the number of sample in each transport grouping
					for (unsigned long idxSamples = 0;
						idxSamples < pItemValues[idxItems].dwCount; idxSamples++)
					{
						TransportDataType transportType = CHelper::GetTransportDataType(
							pItemValues[idxItems].pvDataValues[idxSamples].vt);
						dataTypeCounts[(short)transportType] += 1;
					}

					// Copy the common values to the Journal Data to be returned
					ItemValues[idxItems] = gcnew JournalDataChangedValues();
					ItemValues[idxItems]->ClientAlias = pItemValues[idxItems].hClient;
					ItemValues[idxItems]->Calculation = gcnew TypeId(XiSchemaType::OPC,
						XiNamespace::OPCHDA, JournalDataSampleTypes::ChangedDataSamples.ToString());
					ItemValues[idxItems]->ResultCode = pHR[idxItems];
					ItemValues[idxItems]->StartTime = cliStartTime->dtTime;
					ItemValues[idxItems]->EndTime = cliEndTime->dtTime;

					// Based on the VARIANT type found in checking the samples convert to a .NET type
					if (0 != dataTypeCounts[(short)TransportDataType::Object])
					{
						// The Data Values are to be returned as type object
						ItemValues[idxItems]->ModificationAttributes = gcnew ModificationAttributesList(
							0, 0, pItemValues[idxItems].dwCount);
						for (unsigned long idxSamples = 0;
							idxSamples < pItemValues[idxItems].dwCount; idxSamples++)
						{
							unsigned int statusCode = (gcnew Xi::OPC::COM::API::XiStatusCodeFromOpcCOM(
								(unsigned int)pItemValues[idxItems].pdwQualities[idxSamples], S_OK))->StatusCode;
							DateTime timeStamp = DateTime::FromFileTimeUtc(
								*((__int64*)(&pItemValues[idxItems].pftTimeStamps[idxSamples])));
							cliVARIANT dv = CHelper::ConvertFromVARIANT(
								&pItemValues[idxItems].pvDataValues[idxSamples]);
							ItemValues[idxItems]->ModificationAttributes->SetObject(
								idxSamples, statusCode, timeStamp, dv.DataValue);
							::VariantClear(&pItemValues[idxItems].pvDataValues[idxSamples]);
						}
					}
					else if (0 != dataTypeCounts[(short)TransportDataType::Double])
					{
						// The Data Values are to be returned as type double
						if (0 != dataTypeCounts[(short)TransportDataType::Uint])
						{
							resultCode = XiFaultCodes::E_INCONSISTENT_TRANSPORTDATATYPE;
							if (S_OK == pHR[idxItems])
								ItemValues[idxItems]->ResultCode =
									XiFaultCodes::E_INCONSISTENT_TRANSPORTDATATYPE;
						}

						ItemValues[idxItems]->ModificationAttributes = gcnew ModificationAttributesList(
							pItemValues[idxItems].dwCount, 0, 0);
						for (unsigned long idxSamples = 0;
							idxSamples < pItemValues[idxItems].dwCount; idxSamples++)
						{
							unsigned int statusCode = (gcnew Xi::OPC::COM::API::XiStatusCodeFromOpcCOM(
								(unsigned int)pItemValues[idxItems].pdwQualities[idxSamples], S_OK))->StatusCode;
							DateTime timeStamp = DateTime::FromFileTimeUtc(
								*((__int64*)(&pItemValues[idxItems].pftTimeStamps[idxSamples])));
							cliVARIANT dv = CHelper::ConvertFromVARIANTdefaultDouble(
								&pItemValues[idxItems].pvDataValues[idxSamples]);
							ItemValues[idxItems]->ModificationAttributes->SetDouble(
								idxSamples, statusCode, timeStamp, (double)dv);
							::VariantClear(&pItemValues[idxItems].pvDataValues[idxSamples]);
						}
					}
					else if (0 != dataTypeCounts[(short)TransportDataType::Uint])
					{
						// The Data Values are to be returned as type long long
						ItemValues[idxItems]->ModificationAttributes = gcnew ModificationAttributesList(
							0, pItemValues[idxItems].dwCount, 0);
						for (unsigned long idxSamples = 0;
							idxSamples < pItemValues[idxItems].dwCount; idxSamples++)
						{
							unsigned int statusCode = (gcnew Xi::OPC::COM::API::XiStatusCodeFromOpcCOM(
								(unsigned int)pItemValues[idxItems].pdwQualities[idxSamples], S_OK))->StatusCode;
							DateTime timeStamp = DateTime::FromFileTimeUtc(
								*((__int64*)(&pItemValues[idxItems].pftTimeStamps[idxSamples])));
							cliVARIANT dv = CHelper::ConvertFromVARIANTdefaultUint(
								&pItemValues[idxItems].pvDataValues[idxSamples]);
							ItemValues[idxItems]->ModificationAttributes->SetUint(
								idxSamples, statusCode, timeStamp, (unsigned int)dv);
							::VariantClear(&pItemValues[idxItems].pvDataValues[idxSamples]);
						}
					}
					else
					{
						// Not a known data type so return them as long long
						ItemValues[idxItems]->ModificationAttributes = gcnew ModificationAttributesList(
							0, pItemValues[idxItems].dwCount, 0);
						for (unsigned long idxSamples = 0;
							idxSamples < pItemValues[idxItems].dwCount; idxSamples++)
						{
							unsigned int statusCode =  (gcnew Xi::OPC::COM::API::XiStatusCodeFromOpcCOM(
								(unsigned int)pItemValues[idxItems].pdwQualities[idxSamples], S_OK))->StatusCode;
							DateTime timeStamp = DateTime::FromFileTimeUtc(
								*((__int64*)(&pItemValues[idxItems].pftTimeStamps[idxSamples])));
							cliVARIANT dv = CHelper::ConvertFromVARIANTdefaultUint(
								&pItemValues[idxItems].pvDataValues[idxSamples]);
							ItemValues[idxItems]->ModificationAttributes->SetUint(
								idxSamples, statusCode, timeStamp, (unsigned int)dv);
							::VariantClear(&pItemValues[idxItems].pvDataValues[idxSamples]);
						}
					}
					hr = (S_OK != hr) ? hr : resultCode;
				}
				else
				{
					ItemValues[idxItems]->ModificationAttributes = gcnew ModificationAttributesList(0, 0, 0);
				}

				if (pItemValues[idxItems].pdwQualities != nullptr)
				{
					::CoTaskMemFree(pItemValues[idxItems].pdwQualities);
					pItemValues[idxItems].pdwQualities = nullptr;
				}

				if (pItemValues[idxItems].pftTimeStamps != nullptr)
				{
					::CoTaskMemFree(pItemValues[idxItems].pftTimeStamps);
					pItemValues[idxItems].pftTimeStamps = nullptr;
				}

				if (pItemValues[idxItems].pvDataValues != nullptr)
				{
					::CoTaskMemFree(pItemValues[idxItems].pvDataValues);
					pItemValues[idxItems].pvDataValues = nullptr;
				}

				if (pItemValues[idxItems].pftModificationTime != nullptr)
				{
					::CoTaskMemFree(pItemValues[idxItems].pftModificationTime);
					pItemValues[idxItems].pftModificationTime = nullptr;
				}

				if (pItemValues[idxItems].pEditType != nullptr)
				{
					::CoTaskMemFree(pItemValues[idxItems].pEditType);
					pItemValues[idxItems].pEditType = nullptr;
				}

				if (pItemValues[idxItems].szUser != nullptr)
				{
					::CoTaskMemFree(pItemValues[idxItems].szUser);
					pItemValues[idxItems].szUser = nullptr;
				}
			}
		}

		// free the un-managed memory
		if (phServer != nullptr)
		{
			delete [] phServer;
			phServer = nullptr;
		}

		// free memory from the COM call
		if (pItemValues != nullptr)
		{
			::CoTaskMemFree(pItemValues);
			pItemValues = nullptr;
		}

		if (pHR != nullptr)
		{
			::CoTaskMemFree(pHR);
			pHR = nullptr;
		}

		return cliHRESULT(hr);
	}

	cliHRESULT COPCHdaServer::ReadAttribute(
		/*[in,out]*/ OPCHDA_TIME^ %cliStartTime,
		/*[in,out]*/ OPCHDA_TIME^ %cliEndTime,
		/*[in]*/ unsigned int hServer,
		/*[in]*/ List<unsigned int>^ dwAttributeIDs,
		/*[out]*/ array<JournalDataPropertyValue^>^ %AttributeValues)
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC HDA Server has been Disposed!");

		tagOPCHDA_TIME hdaStartTime;
		tagOPCHDA_TIME hdaEndTime;
		CHelper::cliHdaTimeToHdaTime(cliStartTime, &hdaStartTime);
		CHelper::cliHdaTimeToHdaTime(cliEndTime, &hdaEndTime);

		// move the attribute IDs to the format needed for the COM call
		unsigned long dwNumAttributes = dwAttributeIDs->Count;
		unsigned long *pdwAttributeIDs = new unsigned long[dwNumAttributes];
		for(unsigned long i = 0; i < dwNumAttributes; i++ )
		{
			pdwAttributeIDs[i] = dwAttributeIDs[i];
		}

		tagOPCHDA_ATTRIBUTE *pAttributeValues = nullptr;
		HRESULT *pHR = nullptr;
		HRESULT hr = m_pIOPCHDA_SyncRead->ReadAttribute(
			&hdaStartTime,
			&hdaEndTime,
			hServer,
			dwNumAttributes,
			pdwAttributeIDs,
			&pAttributeValues,
			&pHR);

		if (SUCCEEDED(hr) && nullptr != pAttributeValues)
		{
			unsigned int resultCode = S_OK;
			CHelper::HdaTimeToCliHdaTime(&hdaStartTime, cliStartTime);
			CHelper::HdaTimeToCliHdaTime(&hdaEndTime, cliEndTime);

			// convert and store the received values to the list
			AttributeValues = gcnew array<JournalDataPropertyValue^>(dwNumAttributes);

			long dataTypeCounts[(short)TransportDataType::MaxTransportDataType];
			for( unsigned long idx1 = 0; idx1 < dwNumAttributes; idx1++)
			{
				::ZeroMemory(dataTypeCounts, sizeof(dataTypeCounts));
				if (0 != pAttributeValues[idx1].dwNumValues)
				{
					// Count the number of sample in each transport grouping
					for (unsigned long idxSamples = 0;
						idxSamples < pAttributeValues[idx1].dwNumValues; idxSamples++)
					{
						TransportDataType transportType = CHelper::GetTransportDataType(
							pAttributeValues[idx1].vAttributeValues[idxSamples].vt);
						dataTypeCounts[(short)transportType] += 1;
					}

					AttributeValues[idx1] = gcnew JournalDataPropertyValue();
					AttributeValues[idx1]->ClientAlias = pAttributeValues[idx1].hClient;
					AttributeValues[idx1]->ResultCode = pHR[idx1];
					AttributeValues[idx1]->PropertyId = gcnew TypeId(
						XiSchemaType::OPC, XiNamespace::OPCHDA, pAttributeValues[idx1].dwAttributeID.ToString());

					// Based on the VARIANT type found in checking the samples convert to a .NET type
					if (0 != dataTypeCounts[(short)TransportDataType::Object])
					{
						if (0 != dataTypeCounts[(short)TransportDataType::Uint]
							|| 0 != dataTypeCounts[(short)TransportDataType::Double])
						{
							resultCode = XiFaultCodes::E_INCONSISTENT_TRANSPORTDATATYPE;
							if (S_OK == pHR[idx1])
								AttributeValues[idx1]->ResultCode =
									XiFaultCodes::E_INCONSISTENT_TRANSPORTDATATYPE;
						}

						// The Data Values are to be returned as type object
						AttributeValues[idx1]->PropertyValues = gcnew DataValueArrays(
							0, 0, pAttributeValues[idx1].dwNumValues);
						for (unsigned long idxSamples = 0;
							idxSamples < pAttributeValues[idx1].dwNumValues; idxSamples++)
						{
							unsigned int statusCode = (gcnew Xi::OPC::COM::API::XiStatusCodeFromOpcCOM(
								(unsigned int)OPC_::QUALITY_GOOD, S_OK))->StatusCode;
							DateTime timeStamp = DateTime::FromFileTimeUtc(
								*((__int64*)(&pAttributeValues[idx1].ftTimeStamps[idxSamples])));
							cliVARIANT dv = CHelper::ConvertFromVARIANT(
								&pAttributeValues[idx1].vAttributeValues[idxSamples]);
							AttributeValues[idx1]->PropertyValues->SetObject(
								idxSamples, statusCode, timeStamp, dv.DataValue);
							::VariantClear(&pAttributeValues[idx1].vAttributeValues[idxSamples]);
						}
					}
					else if (0 != dataTypeCounts[(short)TransportDataType::Double])
					{
						if (0 != dataTypeCounts[(short)TransportDataType::Uint]
							|| 0 != dataTypeCounts[(short)TransportDataType::Object])
						{
							resultCode = XiFaultCodes::E_INCONSISTENT_TRANSPORTDATATYPE;
							if (S_OK == pHR[idx1])
								AttributeValues[idx1]->ResultCode =
									XiFaultCodes::E_INCONSISTENT_TRANSPORTDATATYPE;
						}

						// The Data Values are to be returned as type double
						AttributeValues[idx1]->PropertyValues = gcnew DataValueArrays(
							pAttributeValues[idx1].dwNumValues, 0, 0);
						for (unsigned long idxSamples = 0;
							idxSamples < pAttributeValues[idx1].dwNumValues; idxSamples++)
						{
							unsigned int statusCode = (gcnew Xi::OPC::COM::API::XiStatusCodeFromOpcCOM(
								(unsigned int)OPC_::QUALITY_GOOD, S_OK))->StatusCode;
							DateTime timeStamp = DateTime::FromFileTimeUtc(
								*((__int64*)(&pAttributeValues[idx1].ftTimeStamps[idxSamples])));
							cliVARIANT dv = CHelper::ConvertFromVARIANTdefaultDouble(
								&pAttributeValues[idx1].vAttributeValues[idxSamples]);
							AttributeValues[idx1]->PropertyValues->SetDouble(
								idxSamples, statusCode, timeStamp, (double)dv);
							::VariantClear(&pAttributeValues[idx1].vAttributeValues[idxSamples]);
						}
					}
					else if (0 != dataTypeCounts[(short)TransportDataType::Uint])
					{
						if (0 != dataTypeCounts[(short)TransportDataType::Double]
							|| 0 != dataTypeCounts[(short)TransportDataType::Object])
						{
							resultCode = XiFaultCodes::E_INCONSISTENT_TRANSPORTDATATYPE;
							if (S_OK == pHR[idx1])
								AttributeValues[idx1]->ResultCode =
									XiFaultCodes::E_INCONSISTENT_TRANSPORTDATATYPE;
						}

						// The Data Values are to be returned as type long long
						AttributeValues[idx1]->PropertyValues = gcnew DataValueArrays(
							0, pAttributeValues[idx1].dwNumValues, 0);
						for (unsigned long idxSamples = 0;
							idxSamples < pAttributeValues[idx1].dwNumValues; idxSamples++)
						{
							unsigned int statusCode = (gcnew Xi::OPC::COM::API::XiStatusCodeFromOpcCOM(
								(unsigned int)OPC_::QUALITY_GOOD, S_OK))->StatusCode;
							DateTime timeStamp = DateTime::FromFileTimeUtc(
								*((__int64*)(&pAttributeValues[idx1].ftTimeStamps[idxSamples])));
							cliVARIANT dv = CHelper::ConvertFromVARIANTdefaultUint(
								&pAttributeValues[idx1].vAttributeValues[idxSamples]);
							AttributeValues[idx1]->PropertyValues->SetUint(
								idxSamples, statusCode, timeStamp, (unsigned int)dv);
							::VariantClear(&pAttributeValues[idx1].vAttributeValues[idxSamples]);
						}
					}
					else
					{
						// Uncertain - There may not be any property values present
						if (nullptr != AttributeValues[idx1]->PropertyValues)
						{
							AttributeValues[idx1]->PropertyValues = gcnew DataValueArrays(
								0, pAttributeValues[idx1].dwNumValues, 0);
							for (unsigned long idxSamples = 0;
								idxSamples < pAttributeValues[idx1].dwNumValues; idxSamples++)
							{
								unsigned int statusCode = (gcnew Xi::OPC::COM::API::XiStatusCodeFromOpcCOM(
									(unsigned int)OPC_::QUALITY_GOOD, S_OK))->StatusCode;
								DateTime timeStamp = DateTime::FromFileTimeUtc(
									*((__int64*)(&pAttributeValues[idx1].ftTimeStamps[idxSamples])));
								cliVARIANT dv = CHelper::ConvertFromVARIANTdefaultUint(
									&pAttributeValues[idx1].vAttributeValues[idxSamples]);
								AttributeValues[idx1]->PropertyValues->SetUint(
									idxSamples, statusCode, timeStamp, (unsigned int)dv);
								::VariantClear(&pAttributeValues[idx1].vAttributeValues[idxSamples]);
							}
						}
					}
				}
				else
				{
					AttributeValues[idx1]->PropertyValues = gcnew DataValueArrays(0, 0, 0);
				}
				
				if (pAttributeValues[idx1].vAttributeValues != nullptr)
				{
					::CoTaskMemFree(pAttributeValues[idx1].vAttributeValues);
					pAttributeValues[idx1].vAttributeValues = nullptr;
				}
			}
		}
		
		// free the un-managed memory
		if (pdwAttributeIDs != nullptr)
		{
			delete [] pdwAttributeIDs;
			pdwAttributeIDs = nullptr;
		}

		// free memory from the COM call
		if (pAttributeValues != nullptr)
		{
			::CoTaskMemFree(pAttributeValues);
			pAttributeValues = nullptr;
		}

		if (pHR != nullptr)
		{
			::CoTaskMemFree(pHR);
			pHR = nullptr;
		}

		return cliHRESULT(hr);
	}

	// IAdviseOPCShutdownCli
	cliHRESULT COPCHdaServer::AdviseShutdownRequest(
		/*[in]*/ OPCShutdown::ShutdownRequest^ shutdownRequest)
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC HDA Server has been Disposed!");

		m_shutdownRequest += shutdownRequest;
		return cliHRESULT(S_OK);
	}

	cliHRESULT COPCHdaServer::UnadviseShutdownRequest(
		/*[in]*/ OPCShutdown::ShutdownRequest^ shutdownRequest)
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC HDA Server has been Disposed!");

		m_shutdownRequest -= shutdownRequest;
		return cliHRESULT(S_OK);
	}

	unsigned int COPCHdaServer::ConvertToJournalDataValues(
		DateTime startTime, DateTime endTime,
		unsigned long dwNumItems,
		tagOPCHDA_ITEM *pItemValues,
		unsigned long * typeId_LocalId,
		HRESULT *pHR,
		array<JournalDataValues^>^ %itemValues)
	{
		// convert and store the received values to the arrays
		unsigned int resultCode = S_OK;
		unsigned long * typeId_LocalIdPtr = typeId_LocalId;
		itemValues = gcnew array<JournalDataValues^>(dwNumItems);

		long dataTypeCounts[(short)TransportDataType::MaxTransportDataType];
		for (unsigned long idxItems = 0; idxItems < dwNumItems; idxItems++)
		{
			::ZeroMemory(dataTypeCounts, sizeof(dataTypeCounts));

			// Copy the common values to the Journal Data to be returned
			itemValues[idxItems] = gcnew JournalDataValues();
			itemValues[idxItems]->ClientAlias = pItemValues[idxItems].hClient;
			itemValues[idxItems]->Calculation = gcnew TypeId(XiSchemaType::OPC,
				XiNamespace::OPCHDA, (*typeId_LocalIdPtr++).ToString());
			itemValues[idxItems]->ResultCode = pHR[idxItems];
			itemValues[idxItems]->StartTime = startTime;
			itemValues[idxItems]->EndTime = endTime;

				if (0 != pItemValues[idxItems].dwCount)
			{
				// Count the number of sample in each transport grouping
				for (unsigned long idxSamples = 0;
					idxSamples < pItemValues[idxItems].dwCount; idxSamples++)
				{
					TransportDataType transportType = CHelper::GetTransportDataType(
						pItemValues[idxItems].pvDataValues[idxSamples].vt);
					dataTypeCounts[(short)transportType] += 1;
				}
				_ASSERTE(pItemValues[idxItems].dwCount == (dataTypeCounts[0] + dataTypeCounts[1] + dataTypeCounts[2] + dataTypeCounts[3] + dataTypeCounts[4]));

				// Based on the VARIANT type found in checking the samples convert to a .NET type
				if (0 != dataTypeCounts[(short)TransportDataType::Object])
				{
					// The Data Values are to be returned as type object
					itemValues[idxItems]->HistoricalValues = gcnew DataValueArrays(
						0, 0, pItemValues[idxItems].dwCount);
					for (unsigned long idxSamples = 0;
						idxSamples < pItemValues[idxItems].dwCount; idxSamples++)
					{
						unsigned int statusCode = (gcnew Xi::OPC::COM::API::XiStatusCodeFromOpcCOM(
							(unsigned int)pItemValues[idxItems].pdwQualities[idxSamples], S_OK))->StatusCode;
						DateTime timeStamp = DateTime::FromFileTimeUtc(
							*((__int64*)(&pItemValues[idxItems].pftTimeStamps[idxSamples])));
						cliVARIANT dv = CHelper::ConvertFromVARIANT(
							&pItemValues[idxItems].pvDataValues[idxSamples]);
						itemValues[idxItems]->HistoricalValues->SetObject(
							idxSamples, statusCode, timeStamp, dv.DataValue);
						::VariantClear(&pItemValues[idxItems].pvDataValues[idxSamples]);
					}
				}
				else if (0 != dataTypeCounts[(short)TransportDataType::Double])
				{
					// The Data Values are to be returned as type double
					if (0 != dataTypeCounts[(short)TransportDataType::Uint])
					{
						resultCode = XiFaultCodes::E_INCONSISTENT_TRANSPORTDATATYPE;
						if (S_OK == pHR[idxItems])
							itemValues[idxItems]->ResultCode =
								XiFaultCodes::E_INCONSISTENT_TRANSPORTDATATYPE;
					}

					itemValues[idxItems]->HistoricalValues = gcnew DataValueArrays(
						pItemValues[idxItems].dwCount, 0, 0);
					for (unsigned long idxSamples = 0;
						idxSamples < pItemValues[idxItems].dwCount; idxSamples++)
					{
						unsigned int statusCode = (gcnew Xi::OPC::COM::API::XiStatusCodeFromOpcCOM(
							(unsigned int)pItemValues[idxItems].pdwQualities[idxSamples], S_OK))->StatusCode;
						DateTime timeStamp = DateTime::FromFileTimeUtc(
							*((__int64*)(&pItemValues[idxItems].pftTimeStamps[idxSamples])));
						cliVARIANT dv = CHelper::ConvertFromVARIANTdefaultDouble(
							&pItemValues[idxItems].pvDataValues[idxSamples]);
						itemValues[idxItems]->HistoricalValues->SetDouble(
							idxSamples, statusCode, timeStamp, (double)dv);
						::VariantClear(&pItemValues[idxItems].pvDataValues[idxSamples]);
					}
				}
				else if (0 != dataTypeCounts[(short)TransportDataType::Uint])
				{
					// The Data Values are to be returned as type long long
					itemValues[idxItems]->HistoricalValues = gcnew DataValueArrays(
						0, pItemValues[idxItems].dwCount, 0);
					for (unsigned long idxSamples = 0;
						idxSamples < pItemValues[idxItems].dwCount; idxSamples++)
					{
						unsigned int statusCode = (gcnew Xi::OPC::COM::API::XiStatusCodeFromOpcCOM(
							(unsigned int)pItemValues[idxItems].pdwQualities[idxSamples], S_OK))->StatusCode;
						DateTime timeStamp = DateTime::FromFileTimeUtc(
							*((__int64*)(&pItemValues[idxItems].pftTimeStamps[idxSamples])));
						cliVARIANT dv = CHelper::ConvertFromVARIANTdefaultUint(
							&pItemValues[idxItems].pvDataValues[idxSamples]);
						itemValues[idxItems]->HistoricalValues->SetUint(
							idxSamples, statusCode, timeStamp, (unsigned int)dv);
						::VariantClear(&pItemValues[idxItems].pvDataValues[idxSamples]);
					}
				}
				else
				{
					// Uncertain so try to return them all as long long
					itemValues[idxItems]->HistoricalValues = gcnew DataValueArrays(
						0, pItemValues[idxItems].dwCount, 0);
					for (unsigned long idxSamples = 0;
						idxSamples < pItemValues[idxItems].dwCount; idxSamples++)
					{
						unsigned int statusCode = (gcnew Xi::OPC::COM::API::XiStatusCodeFromOpcCOM(
							(unsigned int)pItemValues[idxItems].pdwQualities[idxSamples], S_OK))->StatusCode;
						DateTime timeStamp = DateTime::FromFileTimeUtc(
							*((__int64*)(&pItemValues[idxItems].pftTimeStamps[idxSamples])));
						cliVARIANT dv = CHelper::ConvertFromVARIANTdefaultUint(
							&pItemValues[idxItems].pvDataValues[idxSamples]);
						itemValues[idxItems]->HistoricalValues->SetUint(
							idxSamples, statusCode, timeStamp, (unsigned int)dv);
						::VariantClear(&pItemValues[idxItems].pvDataValues[idxSamples]);
					}
				}
			}
			else
			{
				itemValues[idxItems]->HistoricalValues = gcnew DataValueArrays(0, 0, 0);
			}
		}
		return cliHRESULT(resultCode);
	}

}}}}
