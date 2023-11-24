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
#include <vcclr.h>

#include "OPCDaServer.h"
#include "OPCDaGroup.h"
#include "..\Helper.h"
#include "..\IEnumStrings.h"
#include "OPCDaDataCallback.h"
#include "OPCDaShutdownCallback.h"

using namespace System::Runtime::InteropServices;

namespace Xi {
namespace OPC {
namespace COM {
namespace Impl {

	COPCDaServer::COPCDaServer(cliHRESULT &HR, ::IOPCServer * pIOPCServer)
		: m_pIOPCServer(pIOPCServer)
		, m_pIOPCCommon(nullptr)
		, m_pIOPCBrowseServerAddressSpace(nullptr)
		, m_pIOPCItemProperties(nullptr)
		, m_pIOPCDataCallback(nullptr)
		, m_pIOPCShutdown(nullptr)
		, m_onDataChange(nullptr)
		, m_shutdownRequest(nullptr)
		, m_dwShutdownAdviseCookie( 0 )
		, m_bHasBeenDisposed(false)
	{
		HR = E_FAIL;
		::IOPCCommon * pIOPCCommon = nullptr;
		HR = pIOPCServer->QueryInterface(IID_IOPCCommon, reinterpret_cast<void**>(&pIOPCCommon));
		if (HR.Succeeded) {
			m_pIOPCCommon = pIOPCCommon;
		}
		if (HR.Succeeded)
		{
			::IOPCBrowseServerAddressSpace * pIOPCBrowseServerAddressSpace = nullptr;
			HR = pIOPCServer->QueryInterface(IID_IOPCBrowseServerAddressSpace, 
				reinterpret_cast<void**>(&pIOPCBrowseServerAddressSpace));
			if (HR.Succeeded)
			{
				m_pIOPCBrowseServerAddressSpace = pIOPCBrowseServerAddressSpace;
			}
		}
		if (HR.Succeeded)
		{
			::IOPCItemProperties * pIOPCItemProperties = nullptr;
			HR = pIOPCServer->QueryInterface(IID_IOPCItemProperties, 
				reinterpret_cast<void**>(&pIOPCItemProperties));
			if (HR.Succeeded)
			{
				m_pIOPCItemProperties = pIOPCItemProperties;
			}
		}
		if (HR.Succeeded)
		{
			m_gcHandleTHIS = GCHandle::Alloc(this);
			::IOPCDataCallback * pIOPCDataCallback = nullptr;     // For the IConnectionPoint
			HR = COPCDaDataCallback::CreateInstance( reinterpret_cast<::IOPCDataCallback**>(&pIOPCDataCallback) );
			if (HR.Succeeded)
			{
				m_pIOPCDataCallback = pIOPCDataCallback;
				((COPCDaDataCallback*)pIOPCDataCallback)->SetOpcServerGCHandle(m_gcHandleTHIS);
			}

			::IOPCShutdown * pIOPCShutdown = nullptr;
			cliHRESULT HR1 = COPCDaShutdownCallback::CreateInstance( reinterpret_cast<::IOPCShutdown**>(&pIOPCShutdown) );
			if (HR1.Succeeded)
			{
				m_pIOPCShutdown = pIOPCShutdown;
				((COPCDaShutdownCallback*)pIOPCShutdown)->SetOpcServerGCHandle(m_gcHandleTHIS);
				CComQIPtr<IConnectionPointContainer, &IID_IConnectionPointContainer> pICPC( m_pIOPCServer );
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
			HR = (HR.Failed) ? HR : HR1;
		}
		m_OPCDaGroups = gcnew Dictionary<unsigned int, COPCDaGroup^>();
	}

	COPCDaServer::~COPCDaServer(void)
	{
		if (DisposeThis(true))
			GC::SuppressFinalize(this);
	}

	COPCDaServer::!COPCDaServer(void)
	{
		DisposeThis(false);
	}

	bool COPCDaServer::DisposeThis(bool isDisposing)
	{
		if (m_bHasBeenDisposed)
			return false;

        for each (COPCDaGroup^ g in System::Linq::Enumerable::ToArray(m_OPCDaGroups->Values))
		{
			// This should be a "Dispose" but can not remove from map now
			g->DisposeThis(isDisposing);
		}
		m_OPCDaGroups->Clear();
		m_OPCDaGroups = nullptr;

		CComQIPtr<IConnectionPointContainer, &IID_IConnectionPointContainer> pICPC( m_pIOPCServer );
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

		if ( nullptr != m_pIOPCItemProperties )
			m_pIOPCItemProperties->Release();
		m_pIOPCItemProperties = nullptr;

		if ( nullptr != m_pIOPCBrowseServerAddressSpace )
			m_pIOPCBrowseServerAddressSpace->Release();
		m_pIOPCBrowseServerAddressSpace = nullptr;

		if ( nullptr != m_pIOPCCommon )
			m_pIOPCCommon->Release();
		m_pIOPCCommon = nullptr;

		if ( nullptr != m_pIOPCServer )
			m_pIOPCServer->Release();
		m_pIOPCServer = nullptr;

		if ( nullptr != m_pIOPCDataCallback )
			m_pIOPCDataCallback->Release();
		m_pIOPCDataCallback = nullptr;

		if ( nullptr != m_pIOPCShutdown )
			m_pIOPCShutdown->Release();
		m_pIOPCShutdown = nullptr;

		m_bHasBeenDisposed = true;
		return true;
	}

	// IOPCCommon
	cliHRESULT COPCDaServer::SetLocaleID(
		/*[in]*/ unsigned int dwLcid )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC DA Server has been Disposed!");

		return IOPCCommon->SetLocaleID(dwLcid);
	}

	cliHRESULT COPCDaServer::GetLocaleID(
		/*[out]*/ unsigned int %dwLcid )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC DA Server has been Disposed!");

		unsigned long ulLocaleID = 0;
		cliHRESULT HR = IOPCCommon->GetLocaleID(&ulLocaleID);
		dwLcid = ulLocaleID;
		return HR;
	}

	cliHRESULT COPCDaServer::QueryAvailableLocaleIDs(
		/*[out]*/ List<unsigned int>^ %dwLcid )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC DA Server has been Disposed!");

		dwLcid = gcnew List<unsigned int>();

		cliHRESULT HR(S_OK);
		DWORD dwCount = 0;

		CComHeapPtr<DWORD> pDwId;

		unsigned long ulLocaleID = 0;

		HR = IOPCCommon->QueryAvailableLocaleIDs(&dwCount, &pDwId);
		
		if(HR.Succeeded)
		{
			for(DWORD i = 0; i < dwCount; i++)
			{
				dwLcid->Add((unsigned int)pDwId[i]);
			}
		}
		
		return HR;
	}

	cliHRESULT COPCDaServer::GetErrorString(
		/*[in]*/ cliHRESULT dwError,
		/*[out]*/ String^ %errString )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC DA Server has been Disposed!");

		errString = nullptr;
		CComHeapPtr<WCHAR> pErrorString;
		cliHRESULT HR = IOPCCommon->GetErrorString( dwError.hResult, &pErrorString);

		if (HR.Succeeded) 
		{
			errString = gcnew String( pErrorString );
		}
		
		return HR;
	}

	cliHRESULT COPCDaServer::SetClientName(
		/*[in]*/ String^ sName )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC DA Server has been Disposed!");

		pin_ptr<const wchar_t> szName = PtrToStringChars(sName);
		cliHRESULT HR = IOPCCommon->SetClientName((LPWSTR)szName);

		return HR;
	}


	// IOPCServer
	cliHRESULT COPCDaServer::AddGroup(
		/*[in]*/ String^ sName,
		/*[in]*/ bool bActive,
		/*[in]*/ unsigned int dwRequestedUpdateRate,
		/*[in]*/ unsigned int hClientGroup,
		/*[in]*/ Nullable<int> iTimeBias,
		/*[in]*/ Nullable<float> fPercentDeadband,
		/*[in]*/ unsigned int dwLCID,
		///*[out]*/ unsigned int %hServerGroup,
		/*[out]*/ unsigned int %dwRevisedUpdateRate,
		/*[out]*/ IOPCItemMgtCli^ %iOPCItemMgt )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC DA Server has been Disposed!");

		if (sName == nullptr)
			return cliHRESULT(E_FAIL);

		iOPCItemMgt = nullptr;
		pin_ptr<const wchar_t> szName = PtrToStringChars(sName);

		long lTimeBias = (iTimeBias.HasValue) ? iTimeBias.Value : 0;
		float fPercentDeadband1 = (fPercentDeadband.HasValue) ? fPercentDeadband.Value : 0.0F;
		unsigned long ulServerGroup = 0;
		unsigned long ulRevisedUpdateRate = 0;
		GUID iid_IOPCItemMgt = IID_IOPCItemMgt;
		::IOPCItemMgt * pIOPCItemMgt = nullptr;
		cliHRESULT HR = IOPCServer->AddGroup((LPWSTR) szName, ((bActive) ? -1 : 0), 
			dwRequestedUpdateRate, hClientGroup,
			((iTimeBias.HasValue) ? (&lTimeBias) : nullptr), 
			((fPercentDeadband.HasValue) ? (&fPercentDeadband1) : nullptr),
			dwLCID, &ulServerGroup, &ulRevisedUpdateRate, 
			&iid_IOPCItemMgt, reinterpret_cast<IUnknown**>(&pIOPCItemMgt));
		if (HR.Succeeded)
		{
			COPCDaGroup^ OPCDaGroup = gcnew COPCDaGroup(HR, pIOPCItemMgt, 
				this, hClientGroup, ulServerGroup );
			if (HR.Succeeded) 
			{
				iOPCItemMgt = dynamic_cast<IOPCItemMgtCli^>(OPCDaGroup);
				m_OPCDaGroups->Add(ulServerGroup, OPCDaGroup);
			}
		}
		dwRevisedUpdateRate = ulRevisedUpdateRate;

		return cliHRESULT(HR);
	}

	cliHRESULT COPCDaServer::GetErrorString(
		/*[in]*/ cliHRESULT dwError,
		/*[in]*/ unsigned int dwLocale,
		/*[out]*/ String^ %sErrString )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC DA Server has been Disposed!");

		sErrString = nullptr;
		CComHeapPtr<wchar_t> szString;
		cliHRESULT HR = IOPCServer->GetErrorString( dwError, dwLocale, &szString);

		if (HR.Succeeded)
		{
			sErrString = gcnew String(szString);
		}
		
		return HR;
	}

	cliHRESULT COPCDaServer::GetGroupByName(
		/*[in]*/ String^ sName,
		/*[out]*/ IOPCItemMgtCli^ %iOPCItemMgt )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC DA Server has been Disposed!");

		iOPCItemMgt = nullptr;

		pin_ptr<const wchar_t> szName = PtrToStringChars(sName);

		GUID iid_IOPCItemMgt = IID_IOPCItemMgt;
		::IOPCItemMgt * pIOPCItemMgt = nullptr;
		cliHRESULT HR = IOPCServer->GetGroupByName((LPWSTR)szName, &iid_IOPCItemMgt, 
			reinterpret_cast<IUnknown**>(&pIOPCItemMgt));
		if (HR.Succeeded)
		{
			for each(KeyValuePair<unsigned int, COPCDaGroup^>^ kp in m_OPCDaGroups)
			{
				if (pIOPCItemMgt == kp->Value->IOPCItemMgt)
				{
					iOPCItemMgt = dynamic_cast<IOPCItemMgtCli^>(kp->Value);
					break;
				}
			}
		}
		
		return HR;
	}

	cliHRESULT COPCDaServer::GetStatus(
		/*[out]*/ OPCSERVERSTATUS^ %ServerStatus )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC DA Server has been Disposed!");

		ServerStatus = nullptr;
		CComHeapPtr<tagOPCSERVERSTATUS> pServerStatus;
		cliHRESULT HR = IOPCServer->GetStatus(&pServerStatus);


		if (HR.Succeeded && nullptr != pServerStatus)
		{
			ServerStatus = gcnew OPCSERVERSTATUS();
			ServerStatus->dtStartTime = DateTime::FromFileTimeUtc(*((long long*)&pServerStatus->ftStartTime));
			ServerStatus->dtCurrentTime = DateTime::FromFileTimeUtc(*((long long*)&pServerStatus->ftCurrentTime));
			if ((long long)0 != (*((long long*)&pServerStatus->ftLastUpdateTime)))
			{
				ServerStatus->dtLastUpdateTime = DateTime::FromFileTimeUtc(*((long long*)&pServerStatus->ftLastUpdateTime));
			}
			ServerStatus->dwServerState = (OPCSERVERSTATE)(unsigned short)pServerStatus->dwServerState;
			ServerStatus->dwGroupCount = pServerStatus->dwGroupCount;
			ServerStatus->dwBandWidth = pServerStatus->dwBandWidth;
			ServerStatus->wMajorVersion = pServerStatus->wMajorVersion;
			ServerStatus->wMinorVersion = pServerStatus->wMinorVersion;
			ServerStatus-> wBuildNumber = pServerStatus->wBuildNumber;
			ServerStatus->sVendorInfo = gcnew String(pServerStatus->szVendorInfo);
			
			if (pServerStatus->szVendorInfo != nullptr)
			{
				::CoTaskMemFree(pServerStatus->szVendorInfo);
				pServerStatus->szVendorInfo = nullptr;
			}
		}

		return HR;
	}

	cliHRESULT COPCDaServer::RemoveGroup(
		/*[in]*/ unsigned int hServerGroup,
		/*[in]*/ bool bForce )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC DA Server has been Disposed!");

		throw FaultHelpers::Create(gcnew String(L"Use IDisposable.Dispose() in place of RemoveGroup(...)!"));
	}

	cliHRESULT COPCDaServer::CreateGroupEnumerator(
		/*[in]*/ OPCENUMSCOPE dwScope,
		/*[out]*/ List<IOPCItemMgtCli^>^ %iOPCItemMgtList )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC DA Server has been Disposed!");

		return cliHRESULT(E_NOTIMPL);
	}

	// IOPCBrowseServerAddressSpace
	cliHRESULT COPCDaServer::QueryOrganization(
		/*[out]*/ OPCNAMESPACETYPE %NameSpaceType )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC DA Server has been Disposed!");

		tagOPCNAMESPACETYPE opcNameSpaceType = (tagOPCNAMESPACETYPE)0;
		cliHRESULT HR = IOPCBrowseServerAddressSpace->QueryOrganization(&opcNameSpaceType);
		if (HR.Succeeded)
		{
			NameSpaceType = (OPCNAMESPACETYPE)(unsigned short)opcNameSpaceType;
		}
		return HR;
	}

	cliHRESULT COPCDaServer::ChangeBrowsePosition(
		/*[in]*/ OPCBROWSEDIRECTION dwBrowseDirection,
		/*[in]*/ String^ sString )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC DA Server has been Disposed!");

		pin_ptr<const wchar_t> szString = (nullptr != sString) ?
			PtrToStringChars(sString) :
			PtrToStringChars("");

		cliHRESULT HR = IOPCBrowseServerAddressSpace->ChangeBrowsePosition(
			((tagOPCBROWSEDIRECTION)(unsigned short)dwBrowseDirection), (LPWSTR)szString);

		return HR;
	}

	//  This method returns an IEnumString because there may be too many 
	//  strings for a list. This allows the caller to get the enumeration 
	// and iterate through the strings.
	cliHRESULT COPCDaServer::BrowseOPCItemIDs(
		/*[in]*/ OPCBROWSETYPE dwBrowseFilterType,
		/*[in]*/ String^ sFilterCriteria,
		/*[in]*/ unsigned short vtDataTypeFilter,
		/*[in]*/ unsigned int dwAccessRightsFilter,
		/*[out]*/ cliIEnumString^ %iEnumStrings )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC DA Server has been Disposed!");

		pin_ptr<const wchar_t> szFilterCriteria = (nullptr != sFilterCriteria)
			? PtrToStringChars(sFilterCriteria)
			: PtrToStringChars("");

		::IEnumString * pIEnumString = nullptr;
		cliHRESULT HR = IOPCBrowseServerAddressSpace->BrowseOPCItemIDs(
			(tagOPCBROWSETYPE)(unsigned short)dwBrowseFilterType, 
			(LPWSTR)szFilterCriteria, vtDataTypeFilter, dwAccessRightsFilter, &pIEnumString);
		if (HR.Succeeded)
		{
			CIEnumStrings^ iEnumStr = gcnew CIEnumStrings(pIEnumString);
			iEnumStrings = dynamic_cast<cliIEnumString^>(iEnumStr);
		}

		return HR;
	}

	cliHRESULT COPCDaServer::GetItemID(
		/*[in]*/ String^ sItemDataID,
		/*[out]*/ String^ %sItemID )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC DA Server has been Disposed!");

		if (nullptr == sItemDataID) return cliHRESULT(E_INVALIDARG);

		pin_ptr<const wchar_t> szItemDataID = PtrToStringChars(sItemDataID);

		LPWSTR szItemID = nullptr;
		cliHRESULT HR = IOPCBrowseServerAddressSpace->GetItemID((LPWSTR)szItemDataID, &szItemID);
		sItemID = gcnew String(szItemID);
		
		if (szItemID != nullptr)
		{
			::CoTaskMemFree(szItemID);
			szItemID = nullptr;
		}

		return HR;
	}

	cliHRESULT COPCDaServer::BrowseAccessPaths(
		/*[in]*/ String^ sItemID,
		/*[out]*/ cliIEnumString^ %iEnumStrings )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC DA Server has been Disposed!");

		if (nullptr == sItemID) return cliHRESULT(E_INVALIDARG);

		pin_ptr<const wchar_t> szItemID = PtrToStringChars(sItemID);

		::IEnumString * pIEnumString = nullptr;
		cliHRESULT HR = IOPCBrowseServerAddressSpace->BrowseAccessPaths((LPWSTR)szItemID, &pIEnumString);
		if (HR.Succeeded)
		{
			CIEnumStrings^ iEnumStr = gcnew CIEnumStrings(pIEnumString);
			iEnumStrings = dynamic_cast<cliIEnumString^>(iEnumStr);
		}
		return HR;
	}

	cliHRESULT COPCDaServer::QueryAvailableProperties(
		/*[in]*/ String^ sItemID,
		/*[out]*/ List<ItemProperty^>^ %lstItemProperties )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC DA Server has been Disposed!");

		lstItemProperties = nullptr;
		if (nullptr == sItemID) return cliHRESULT(E_INVALIDARG);

		pin_ptr<const wchar_t> szItemID = PtrToStringChars(sItemID);

		unsigned long dwCount = 0;

		CComHeapPtr<unsigned long> pPropertyIDs;
		CComHeapPtr<LPWSTR> pDescriptions;
		CComHeapPtr<unsigned short> pvtDataTypes;

		cliHRESULT HR = IOPCItemProperties->QueryAvailableProperties((LPWSTR)szItemID, &dwCount, &pPropertyIDs, &pDescriptions, &pvtDataTypes);

		if (HR.Succeeded)
		{
			lstItemProperties = gcnew List<ItemProperty^>(dwCount);
			for ( unsigned long ulCount = 0; ulCount < dwCount; ulCount++)
			{
				CComHeapPtr<wchar_t> pDescriptionItemPtr(pDescriptions[ulCount]);

				ItemProperty^ itemProperty = gcnew ItemProperty();
				itemProperty->PropertyID = pPropertyIDs[ulCount];
				itemProperty->Description = gcnew String(pDescriptions[ulCount]);
				itemProperty->PropDataType = pvtDataTypes[ulCount];
				
				lstItemProperties->Add(itemProperty);
			}
		}

		return HR;
	}

	cliHRESULT COPCDaServer::GetItemProperties(
		/*[in]*/ String^ sItemID,
		/*[in]*/ List<unsigned int>^ listPropertyIDs,
		/*[out]*/ List<PropertyValue^>^ %lstPropertyValues )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC DA Server has been Disposed!");

		lstPropertyValues = nullptr;
		if (nullptr == sItemID) return cliHRESULT(E_INVALIDARG);

		pin_ptr<const wchar_t> szItemID = PtrToStringChars(sItemID);
		unsigned long dwCount = listPropertyIDs->Count;

		std::vector<unsigned long> dwPropertyIDs(dwCount);
		unsigned long idx = 0;
		for each (unsigned int dwPropId in listPropertyIDs) 
			dwPropertyIDs[idx++] = dwPropId;
		CComHeapPtr<VARIANT> pvData;
		CComHeapPtr<HRESULT> pErrors;
		cliHRESULT HR = IOPCItemProperties->GetItemProperties((LPWSTR)szItemID, dwCount, dwPropertyIDs.data(), &pvData.m_pData, &pErrors.m_pData);

		if (HR.Succeeded)
		{
			lstPropertyValues = gcnew List<PropertyValue^>(dwCount);
			for ( idx = 0; idx < dwCount; idx++ )
			{
				PropertyValue^ propValue = gcnew PropertyValue();
				propValue->hResult = pErrors[idx];
				propValue->vDataValue = (S_OK == pErrors[idx]) 
					? CHelper::ConvertFromVARIANT( &(pvData[idx]) ) 
					: nullptr;
				lstPropertyValues->Add(propValue);
				::VariantClear( &pvData[idx] );
			}
		}

		return HR;
	}

	cliHRESULT COPCDaServer::LookupItemIDs(
		/*[in]*/ String^ sItemID,
		/*[in]*/ List<unsigned int>^ listPropertyIDs,
		/*[out]*/ List<PropertyItemID^>^ %lstPropertyItemIDs )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC DA Server has been Disposed!");

		lstPropertyItemIDs = nullptr;
		if (nullptr == sItemID) return cliHRESULT(E_INVALIDARG);

		pin_ptr<const wchar_t> szItemID = PtrToStringChars(sItemID);
		unsigned long dwCount = listPropertyIDs->Count;

		std::vector<unsigned long> dwPropertyIDs(dwCount);
		unsigned long idx = 0;
		for each (unsigned int dwPropId in listPropertyIDs) dwPropertyIDs[idx++] = dwPropId;
		CComHeapPtr<LPWSTR> pszNewItemIDs;
		CComHeapPtr<HRESULT> pErrors;

		cliHRESULT HR = IOPCItemProperties->LookupItemIDs((LPWSTR)szItemID, dwCount, dwPropertyIDs.data(), &pszNewItemIDs, &pErrors);
		if (HR.Succeeded)
		{
			lstPropertyItemIDs = gcnew List<PropertyItemID^>(dwCount);
			for (idx = 0; idx < dwCount; idx++)
			{
				CComHeapPtr<wchar_t> pszNewItemId(pszNewItemIDs[idx]);

				PropertyItemID^ propItemId = gcnew PropertyItemID();
				propItemId->hResult = pErrors[idx];
				propItemId->ItemID = gcnew String(pszNewItemIDs[idx]);
				lstPropertyItemIDs->Add(propItemId);
			}
		}

		return HR;
	}

	// IAdviseOPCDataCallbackCli
	cliHRESULT COPCDaServer::AdviseOnDataChange(
		/*[in]*/ OPCDataCallback::OnDataChange^ onDataChange)
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC DA Server has been Disposed!");

		m_onDataChange += onDataChange;
		return cliHRESULT(S_OK);
	}

	cliHRESULT COPCDaServer::AdviseOnWriteComplete(
		/*[in]*/ OPCDataCallback::OnWriteComplete^ onWriteComplete)
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC DA Server has been Disposed!");

		return cliHRESULT(E_NOTIMPL);
	}

	cliHRESULT COPCDaServer::AdviseOnCancelComplete(
		/*[in]*/ OPCDataCallback::OnCancelComplete^ onCancelComplete)
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC DA Server has been Disposed!");

		return cliHRESULT(E_NOTIMPL);
	}

	cliHRESULT COPCDaServer::UnadviseOnDataChange(
		/*[in]*/ OPCDataCallback::OnDataChange^ onDataChange)
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC DA Server has been Disposed!");

		m_onDataChange -= onDataChange;
		return cliHRESULT(S_OK);
	}

	cliHRESULT COPCDaServer::UnadviseOnWriteComplete(
		/*[in]*/ OPCDataCallback::OnWriteComplete^ onWriteComplete)
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC DA Server has been Disposed!");

		return cliHRESULT(E_NOTIMPL);
	}

	cliHRESULT COPCDaServer::UnadviseOnCancelComplete(
		/*[in]*/ OPCDataCallback::OnCancelComplete^ onCancelComplete)
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC DA Server has been Disposed!");

		return cliHRESULT(E_NOTIMPL);
	}

	// IAdviseOPCShutdownCli
	cliHRESULT COPCDaServer::AdviseShutdownRequest(
		/*[in]*/ OPCShutdown::ShutdownRequest^ shutdownRequest)
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC DA Server has been Disposed!");

		m_shutdownRequest += shutdownRequest;
		return cliHRESULT(S_OK);
	}

	cliHRESULT COPCDaServer::UnadviseShutdownRequest(
		/*[in]*/ OPCShutdown::ShutdownRequest^ shutdownRequest)
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC DA Server has been Disposed!");

		m_shutdownRequest -= shutdownRequest;
		return cliHRESULT(S_OK);
	}


	cliHRESULT COPCDaServer::RemoveGroupInternal(unsigned int hServerGroup)
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC DA Server has been Disposed!");

		cliHRESULT HR = E_INVALIDARG;
		COPCDaGroup^ OPCDaGroup = nullptr;
		if (m_OPCDaGroups->TryGetValue(hServerGroup, OPCDaGroup))
		{
			HR = IOPCServer->RemoveGroup(hServerGroup, 1);
			if (HR.Succeeded)
			{
				m_OPCDaGroups->Remove(hServerGroup);
			}
		}
		return HR;
	}

}}}}
