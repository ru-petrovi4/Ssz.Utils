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
#include "OPCDaGroup.h"
#include "OPCDaServer.h"
#include "..\Helper.h"

#include <vcclr.h>

using namespace System;
using namespace System::Runtime::InteropServices;

namespace Xi {
namespace OPC {
namespace COM {
namespace Impl {

	COPCDaGroup::COPCDaGroup(cliHRESULT %HR, ::IOPCItemMgt *pIOPCItemMgt,
		COPCDaServer^ IOPCServer, unsigned int hClientGroup, unsigned int hServerGroup)
		: m_pIOPCItemMgt(pIOPCItemMgt)
		, m_pIOPCSyncIO(nullptr)
		, m_pIOPCAsyncIO2(nullptr)
		, m_pIOPCGroupStateMgt(nullptr)
		, m_OPCServer(IOPCServer)
		, m_hClientGroup(hClientGroup)
		, m_hServerGroup(hServerGroup)
		, m_dwAdviseCookie(0)
		, m_bHasBeenDisposed(false)
	{
		::IOPCGroupStateMgt * pIOPCGroupStateMgt = nullptr;
		cliHRESULT HR1 = pIOPCItemMgt->QueryInterface(IID_IOPCGroupStateMgt, reinterpret_cast<void**>(&pIOPCGroupStateMgt));
		if (HR1.Succeeded) m_pIOPCGroupStateMgt = pIOPCGroupStateMgt;
		::IOPCSyncIO * pIOPCSyncIO = nullptr;
		cliHRESULT HR2 = pIOPCItemMgt->QueryInterface(IID_IOPCSyncIO, reinterpret_cast<void**>(&pIOPCSyncIO));
		if (HR2.Succeeded) m_pIOPCSyncIO = pIOPCSyncIO;
		::IOPCAsyncIO2 * pIOPCAsyncIO2 = nullptr;
		cliHRESULT HR3 = pIOPCItemMgt->QueryInterface(IID_IOPCAsyncIO2, reinterpret_cast<void**>(&pIOPCAsyncIO2));
		if (HR3.Succeeded) m_pIOPCAsyncIO2 = pIOPCAsyncIO2;
		HR = (HR1.Failed) ? HR1 : HR2;
		m_hServerTohClient = gcnew Dictionary<unsigned int, unsigned int>();

		CComQIPtr<IConnectionPointContainer, &IID_IConnectionPointContainer> pICPC( m_pIOPCItemMgt );
		if (nullptr != pICPC.p)
		{
			CComPtr<IConnectionPoint> pIConnectionPoint;
			cliHRESULT HR1 = pICPC->FindConnectionPoint( IID_IOPCDataCallback, &pIConnectionPoint );
			if (HR1.Succeeded && nullptr != pIConnectionPoint)
			{
				ULONG dwCookie = 0;
				cliHRESULT HR2 = pIConnectionPoint->Advise( IOPCServer->IOPCDataCallback, &dwCookie );
				if (HR2.Failed)
				{
					throw FaultHelpers::Create((unsigned int)HR2.hResult,
						gcnew String(L"Failed to setup IOPCDataCallback."));
				}
				m_dwAdviseCookie = dwCookie;
			}
			else
			{
				throw FaultHelpers::Create((unsigned int)HR1.hResult,
					gcnew String(L"Failed to Find the IOPCDataCallback for OnDataChange."));
			}
		}
		else
		{
			throw FaultHelpers::Create(gcnew String(L"Failed to Get the IConnectionPointContainer for OnDataChange"));
		}
	}

	COPCDaGroup::~COPCDaGroup(void)
	{
		if (DisposeThis(true))
			GC::SuppressFinalize(this);
	}

	COPCDaGroup::!COPCDaGroup(void)
	{
		DisposeThis(false);
	}

	bool COPCDaGroup::DisposeThis(bool isDisposing)
	{
		if (m_bHasBeenDisposed)
			return false;

		if (0 != m_dwAdviseCookie)
		{
			CComQIPtr<IConnectionPointContainer, &IID_IConnectionPointContainer> pICPC( m_pIOPCItemMgt );
			if (nullptr != pICPC.p)
			{
				CComPtr<IConnectionPoint> pIConnectionPoint;

				cliHRESULT HR1 = pICPC->FindConnectionPoint( IID_IOPCDataCallback, &pIConnectionPoint );
				_ASSERTE(HR1.Succeeded && nullptr != pIConnectionPoint);
				if (HR1.Succeeded && nullptr != pIConnectionPoint)
				{
					cliHRESULT HR2 = pIConnectionPoint->Unadvise(m_dwAdviseCookie);
					_ASSERTE(HR2.Succeeded);
				}
			}
		}
		m_dwAdviseCookie = 0;

		if (isDisposing)
		{
			cliHRESULT HR = m_OPCServer->RemoveGroupInternal(m_hServerGroup);
		}

		if (nullptr != m_pIOPCSyncIO)
			m_pIOPCSyncIO->Release();
		m_pIOPCSyncIO = nullptr;

		if (nullptr != m_pIOPCAsyncIO2)
			m_pIOPCAsyncIO2->Release();
		m_pIOPCAsyncIO2 = nullptr;

		if (nullptr != m_pIOPCGroupStateMgt)
			m_pIOPCGroupStateMgt->Release();
		m_pIOPCGroupStateMgt = nullptr;

		if (nullptr != m_pIOPCItemMgt)
			m_pIOPCItemMgt->Release();
		m_pIOPCItemMgt = nullptr;

		m_hServerTohClient->Clear();
		m_hServerTohClient = nullptr;

		m_bHasBeenDisposed = true;
		return true;
	}

	// IOPCGroupStateMgtCli
	cliHRESULT COPCDaGroup::GetState(
		/*[out]*/ unsigned int %dwUpdateRate,
		/*[out]*/ bool %bActive,
		/*[out]*/ String^ %sName,
		/*[out]*/ int %dwTimeBias,
		/*[out]*/ float %fPercentDeadband,
		/*[out]*/ unsigned int %dwLCID,
		/*[out]*/ unsigned int %hClientGroup,
		/*[out]*/ unsigned int %hServerGroup )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC DA Group has been Disposed!");

		unsigned long dwUpdateRateStk = 0;
		long bActiveStk = 0;
		LPWSTR szNameStk = nullptr;
		long dwTimeBiasStk = 0;
		float fPercentDeadbandStk = 0.0F;
		unsigned long dwLCIDStk = 0;
		unsigned long hClientGroupStk = 0;
		unsigned long hServerGroupStk = 0;
		cliHRESULT HR = IOPCGroupStateMgt->GetState(
			&dwUpdateRateStk,
			&bActiveStk,
			&szNameStk,
			&dwTimeBiasStk,
			&fPercentDeadbandStk,
			&dwLCIDStk,
			&hClientGroupStk,
			&hServerGroupStk);
		if (HR.Succeeded)
		{
			dwUpdateRate = dwUpdateRateStk;
			bActive = (0 != bActiveStk) ? true : false;
			sName = gcnew String( szNameStk );
			dwTimeBias = dwTimeBiasStk;
			fPercentDeadband = fPercentDeadbandStk;
			dwLCID = dwLCIDStk;
			hClientGroup = hClientGroupStk;
			hServerGroup = hServerGroupStk;
		}

		if (szNameStk != nullptr)
		{
			::CoTaskMemFree(szNameStk);
			szNameStk = nullptr;
		}

		return HR;
	}

	cliHRESULT COPCDaGroup::SetState(
		/*[in]*/ unsigned int dwRequestedUpdateRate,
		/*[out]*/ unsigned int %dwRevisedUpdateRate,
		/*[in]*/ bool bActive,
		/*[in]*/ int iTimeBias,
		/*[in]*/ float fPercentDeadband,
		/*[in]*/ unsigned int dwLCID,
		/*[in]*/ unsigned int hClientGroup )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC DA Group has been Disposed!");

		unsigned long dwRequestedUpdateRateStk = dwRequestedUpdateRate;
		unsigned long dwRevisedUpdateRateStk = 0;
		long bActiveStk = (bActive) ? 1 : 0;
		long dwTimeBiasStk = iTimeBias;
		float fPercentDeadbandStk = fPercentDeadband;
		unsigned long dwLCIDStk = dwLCID;
		unsigned long hClientGroupStk = hClientGroup;
		cliHRESULT HR = IOPCGroupStateMgt->SetState(
			&dwRequestedUpdateRateStk,
			&dwRevisedUpdateRateStk,
			&bActiveStk,
			&dwTimeBiasStk,
			&fPercentDeadbandStk,
			&dwLCIDStk,
			&hClientGroupStk);
		if (HR.Succeeded)
		{
			dwRevisedUpdateRate = dwRevisedUpdateRateStk;
		}
		return HR;
	}

	cliHRESULT COPCDaGroup::SetName(
		/*[in]*/ String^ sName )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC DA Group has been Disposed!");

		pin_ptr<const wchar_t> szName = PtrToStringChars(sName);
		cliHRESULT HR = IOPCGroupStateMgt->SetName((LPWSTR)szName);
		return HR;
	}

	cliHRESULT COPCDaGroup::CloneGroup(
		/*[in]*/ String^ szName,
		/*[out]*/ IOPCItemMgtCli^ %iOPCItemMgt)
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC DA Group has been Disposed!");

		return cliHRESULT(E_NOTIMPL);
	}

	// IOPCItemMgt
	cliHRESULT COPCDaGroup::AddItems(
		/*[in]*/ List<OPCITEMDEF^>^ ItemList,
		/*[out]*/ List<OPCITEMRESULT^>^ %lstAddResults )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC DA Group has been Disposed!");

		lstAddResults = nullptr;
		unsigned long dwCount = ItemList->Count;

		std::vector<tagOPCITEMDEF> pItemArray(dwCount);
		::ZeroMemory(pItemArray.data(), (sizeof(tagOPCITEMDEF) * dwCount));
		unsigned long idx = 0;
		for each (OPCITEMDEF^ opcItemDef in ItemList)
		{
			pItemArray[idx].szAccessPath = CHelper::ConvertStringToLPWSTR(opcItemDef->sAccessPath);
			pItemArray[idx].szItemID = CHelper::ConvertStringToLPWSTR(opcItemDef->sItemID);
			pItemArray[idx].bActive = (opcItemDef->bActive) ? 1 : 0;
			pItemArray[idx].hClient = opcItemDef->hClient;
			pItemArray[idx].vtRequestedDataType = opcItemDef->vtRequestedDataType;
			pItemArray[idx].dwBlobSize = 0;
			pItemArray[idx].pBlob = nullptr;
			idx += 1;
		}

		try
		{
			CComHeapPtr<tagOPCITEMRESULT> pAddResult;
			CComHeapPtr<HRESULT> pErrors;

			cliHRESULT HR = IOPCItemMgt->AddItems(dwCount, pItemArray.data(), &pAddResult, &pErrors);

            if (HR.Succeeded)
            {
                // Try secondary to Add Items (USO OPC server bug, failed to add exsisting items if some errors were before)
                for (size_t i = 0; i < dwCount; ++i)
                    if (pErrors[i] != S_OK)
                    {
                        CComHeapPtr<tagOPCITEMRESULT> pAddResult2;
                        CComHeapPtr<HRESULT> pErrors2;

                        HR = IOPCItemMgt->AddItems(1, pItemArray.data() + i, &pAddResult2, &pErrors2);
                        if (HR.Succeeded)
                        {
                            pAddResult[i] = pAddResult2[0];
                            pErrors[i] = pErrors2[0];
                        }
                    }                
			
				lstAddResults = gcnew List<OPCITEMRESULT^>(ItemList->Count);
				for (idx = 0; idx < dwCount; idx++)
				{
					OPCITEMRESULT^ opcItemResult = gcnew OPCITEMRESULT();
					opcItemResult->hClient = pItemArray[idx].hClient;
					opcItemResult->hServer = pAddResult[idx].hServer;
					opcItemResult->hResult = pErrors[idx];
					opcItemResult->vtCanonicalDataType = pAddResult[idx].vtCanonicalDataType;
					opcItemResult->dwAccessRights = pAddResult[idx].dwAccessRights;
					lstAddResults->Add(opcItemResult);
					if (S_OK == pErrors[idx])
					{
						if (!m_hServerTohClient->ContainsKey(pAddResult[idx].hServer))
							m_hServerTohClient->Add(pAddResult[idx].hServer, pItemArray[idx].hClient);
					}
				}
			}

			return HR;
		}
		finally
		{
			for (idx = 0; idx < dwCount; idx++)
			{
				Marshal::FreeHGlobal((IntPtr)pItemArray[idx].szAccessPath);
				Marshal::FreeHGlobal((IntPtr)pItemArray[idx].szItemID);
			}
		}
	}

	cliHRESULT COPCDaGroup::ValidateItems(
		/*[in]*/ List<OPCITEMDEF^>^ ItemList,
		/*[out]*/ List<OPCITEMRESULT^>^ %lstValidationResults )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC DA Group has been Disposed!");

		lstValidationResults = nullptr;
		unsigned long dwCount = ItemList->Count;
		std::vector<tagOPCITEMDEF> pItemArray(dwCount);
		::ZeroMemory(pItemArray.data(), (sizeof(tagOPCITEMDEF) * dwCount));

		unsigned long idx = 0;
		for each (OPCITEMDEF^ opcItemDef in ItemList)
		{
			pItemArray[idx].szAccessPath = CHelper::ConvertStringToLPWSTR(opcItemDef->sAccessPath);
			pItemArray[idx].szItemID = CHelper::ConvertStringToLPWSTR(opcItemDef->sItemID);
			pItemArray[idx].bActive = (opcItemDef->bActive) ? 1 : 0;
			pItemArray[idx].hClient = opcItemDef->hClient;
			pItemArray[idx].vtRequestedDataType = opcItemDef->vtRequestedDataType;
			pItemArray[idx].dwBlobSize = 0;
			pItemArray[idx].pBlob = nullptr;
		}
		try
		{
			CComHeapPtr<tagOPCITEMRESULT> pAddResult;
			CComHeapPtr<HRESULT> pErrors;

			cliHRESULT HR = IOPCItemMgt->AddItems(dwCount, pItemArray.data(), &pAddResult, &pErrors);
			if (HR.Succeeded)
			{
				lstValidationResults = gcnew List<OPCITEMRESULT^>(ItemList->Count);
				for (idx = 0; idx < dwCount; idx++)
				{
					OPCITEMRESULT^ opcItemResult = gcnew OPCITEMRESULT();
					opcItemResult->hServer = pAddResult[idx].hServer;
					opcItemResult->hResult = pErrors[idx];
					opcItemResult->vtCanonicalDataType = pAddResult[idx].vtCanonicalDataType;
					opcItemResult->dwAccessRights = pAddResult[idx].dwAccessRights;
					lstValidationResults->Add(opcItemResult);
				}
			}

			return HR;
		}
		finally
		{
			for (idx = 0; idx < dwCount; idx++)
			{
				Marshal::FreeHGlobal((IntPtr)pItemArray[idx].szAccessPath);
				Marshal::FreeHGlobal((IntPtr)pItemArray[idx].szItemID);
			}
		}
	}

	cliHRESULT COPCDaGroup::RemoveItems(
		/*[in]*/ List<unsigned int>^ hServerList,
		/*[out]*/ List<HandleAndHRESULT^>^ %ErrorsList )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC DA Group has been Disposed!");

		ErrorsList = nullptr;
		unsigned long dwCount = hServerList->Count;
		std::vector<unsigned long> hServer(dwCount);

		unsigned long * phServer = hServer.data();
		for each (unsigned long hSvr in hServerList) *phServer++ = hSvr;
		CComHeapPtr<HRESULT> pErrors;

		cliHRESULT HR = IOPCItemMgt->RemoveItems(dwCount, hServer.data(), &pErrors);
		if (HR.Succeeded)
		{
			if (HR.IsS_FALSE)
			{
				ErrorsList = gcnew List<HandleAndHRESULT^>();
				for (unsigned long idx = 0; idx < dwCount; idx++)
				{
					if (S_OK != pErrors[idx])
					{
						HandleAndHRESULT^ hdlAndHr = gcnew HandleAndHRESULT();
						hdlAndHr->hResult = pErrors[idx];
						hdlAndHr->Handle = m_hServerTohClient[hServer[idx]];
						ErrorsList->Add(hdlAndHr);
					}
				}
			}
			for (unsigned long idx = 0; idx < dwCount; idx++)
			{
				m_hServerTohClient->Remove(hServer[idx]);
			}
		}

		return HR;
	}

	cliHRESULT COPCDaGroup::SetActiveState(
		/*[in]*/ List<unsigned int>^ hServerList,
		/*[in]*/ bool bActive,
		/*[out]*/ List<HandleAndHRESULT^>^ %ErrorsList )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC DA Group has been Disposed!");

		ErrorsList = nullptr;
		long lActive = (bActive) ? 1 : 0;
		unsigned long dwCount = hServerList->Count;

		std::vector<unsigned long> hServer(dwCount);

		unsigned long * phServer = hServer.data();
		for each (unsigned long hSvr in hServerList) *phServer++ = hSvr;

		CComHeapPtr<HRESULT> pErrors;
		cliHRESULT HR = IOPCItemMgt->SetActiveState(dwCount, hServer.data(), lActive, &pErrors);


		if (HR.Succeeded)
		{
			if (HR.IsS_FALSE)
			{
				ErrorsList = gcnew List<HandleAndHRESULT^>();
				for (unsigned long idx = 0; idx < dwCount; idx++)
				{
                    unsigned int handle;
                    if (S_OK != pErrors[idx] && m_hServerTohClient->TryGetValue(hServer[idx], handle))
					{
						HandleAndHRESULT^ hdlAndHr = gcnew HandleAndHRESULT();
						hdlAndHr->hResult = pErrors[idx];
                        hdlAndHr->Handle = handle;
						ErrorsList->Add(hdlAndHr);
					}
				}
			}
		}

		return HR;
	}

	cliHRESULT COPCDaGroup::SetClientHandles(
		/*[in]*/ List<HandlePair^>^ hServer_hClient,
		/*[out]*/ List<HandleAndHRESULT^>^ %ErrorsList )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC DA Group has been Disposed!");

		ErrorsList = nullptr;
		unsigned long dwCount = hServer_hClient->Count;

		std::vector<unsigned long> hServer(dwCount);
		std::vector<unsigned long> hClient(dwCount);

		unsigned long * phServer = hServer.data();
		unsigned long * phClient = hClient.data();
		for each (HandlePair^ hdlPair in hServer_hClient)
		{
			*phServer++ = hdlPair->hServer;
			*phClient++ = hdlPair->hClient;
		}
		CComHeapPtr<HRESULT> pErrors;
		cliHRESULT HR = IOPCItemMgt->SetClientHandles(dwCount, hServer.data(), hClient.data(), &pErrors);
		if (HR.Succeeded)
		{
			if (HR.IsS_FALSE)
			{
				ErrorsList = gcnew List<HandleAndHRESULT^>();
				for (unsigned long idx = 0; idx < dwCount; idx++)
				{
					if (S_OK != pErrors[idx])
					{
						HandleAndHRESULT^ hdlAndHr = gcnew HandleAndHRESULT();
						hdlAndHr->hResult = pErrors[idx];
						hdlAndHr->Handle = hClient[idx];
						ErrorsList->Add(hdlAndHr);
					}
				}
			}
		}

		return HR;
	}

	cliHRESULT COPCDaGroup::SetDatatypes(
		/*[in]*/ List<HandleDataType^>^ hServer_wRequestedDatatype,
		/*[out]*/ List<HandleAndHRESULT^>^ %ErrorsList )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC DA Group has been Disposed!");

		ErrorsList = nullptr;
		unsigned long dwCount = hServer_wRequestedDatatype->Count;

		std::vector<unsigned long> hServer(dwCount);
		std::vector<VARTYPE> varType(dwCount);

		unsigned long * phServer = hServer.data();
		::VARTYPE * pvarType = varType.data();
		for each (HandleDataType^ hdlVarType in hServer_wRequestedDatatype)
		{
			*phServer++ = hdlVarType->hServer;
			*pvarType++ = hdlVarType->wRequestedDatatype;
		}

		CComHeapPtr<HRESULT> pErrors;
		cliHRESULT HR = IOPCItemMgt->SetDatatypes(dwCount, hServer.data(), varType.data(), &pErrors);
		if (HR.Succeeded)
		{
			if (HR.IsS_FALSE)
			{
				ErrorsList = gcnew List<HandleAndHRESULT^>();
				for (unsigned long idx = 0; idx < dwCount; idx++)
				{
					if (S_OK != pErrors[idx])
					{
						HandleAndHRESULT^ hdlAndHr = gcnew HandleAndHRESULT();
						hdlAndHr->hResult = pErrors[idx];
						hdlAndHr->Handle = m_hServerTohClient[hServer[idx]];
						ErrorsList->Add(hdlAndHr);
					}
				}
			}
		}

		return HR;
	}

	cliHRESULT COPCDaGroup::CreateEnumerator(
		/*[out]*/ List<OPCITEMATTRIBUTES^>^ %ItemAttributesList )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC DA Group has been Disposed!");

		return cliHRESULT(E_NOTIMPL);
	}

	// IOPCSyncIO
	cliHRESULT COPCDaGroup::Read(
		/*[in]*/ OPCDATASOURCE dwSource,
		/*[in]*/ List<unsigned int>^ hServerList,
		/*[out]*/ DataValueArraysWithAlias^ %ItemValues )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC DA Group has been Disposed!");

		ItemValues = nullptr;
		unsigned long dwCount = hServerList->Count;

		std::vector<unsigned long> hServer(dwCount);
		unsigned long * phServer = hServer.data();
		for each (unsigned long svrHdl in hServerList) 
			*phServer++ = svrHdl;
		CComHeapPtr<tagOPCITEMSTATE> pItemValues;
		CComHeapPtr<HRESULT> pErrors;
		cliHRESULT HR = IOPCSyncIO->Read((tagOPCDATASOURCE)(unsigned short)dwSource, 
			dwCount, hServer.data(), &pItemValues, &pErrors);
		if (HR.Succeeded && nullptr != pItemValues)
		{
			long dataTypeCounts[(short)TransportDataType::MaxTransportDataType];
			::ZeroMemory(dataTypeCounts, sizeof(dataTypeCounts));
			for (ULONG idx = 0; idx < dwCount; idx++)
			{
				TransportDataType transportType = CHelper::GetTransportDataType(pItemValues[idx].vDataValue.vt);
				dataTypeCounts[(short)transportType] += 1;
			}
			DataValueArraysWithAlias^ valueArrays = gcnew DataValueArraysWithAlias(
				dataTypeCounts[(short)TransportDataType::Double],
				dataTypeCounts[(short)TransportDataType::Uint]
					+ dataTypeCounts[(short)TransportDataType::Unknown],
				dataTypeCounts[(short)TransportDataType::Object]);

			int doubleIdx = 0;
			int longIdx = 0;
			int objectIdx = 0;
			for (ULONG idx = 0; idx < dwCount; idx++)
			{
				cliVARIANT dv = CHelper::ConvertFromVARIANT(&pItemValues[idx].vDataValue);
				unsigned int statusCode = (gcnew Xi::OPC::COM::API::XiStatusCodeFromOpcCOM(
					pItemValues[idx].wQuality, pErrors[idx]))->StatusCode;
				DateTime timeStamp = DateTime::FromFileTimeUtc(*((__int64*)(&pItemValues[idx].ftTimeStamp)));
				TransportDataType transportType = CHelper::GetTransportDataType(pItemValues[idx].vDataValue.vt);
				switch (transportType)
				{
				case TransportDataType::Unknown:
				{
					valueArrays->SetUint(longIdx++, pItemValues[idx].hClient, statusCode, timeStamp, 0);
					break;
				}
				case TransportDataType::Double:
				{
					double doubleValue = (double)dv;
					valueArrays->SetDouble(doubleIdx++, pItemValues[idx].hClient, statusCode, timeStamp, doubleValue);
					break;
				}
				case TransportDataType::Uint:
				{
					unsigned int uintValue = (unsigned int)dv;
					valueArrays->SetUint(longIdx++, pItemValues[idx].hClient, statusCode, timeStamp, uintValue);
					break;
				}
				case TransportDataType::Object:
				{
					valueArrays->SetObject(objectIdx++, pItemValues[idx].hClient, statusCode, timeStamp, dv.DataValue);
					break;
				}
				default:
					break;
				}
				::VariantClear(&pItemValues[idx].vDataValue);
			}
			ItemValues = valueArrays;
		}
		else
		{
			if (nullptr != pItemValues)
			{
				for (unsigned long idx = 0; idx < dwCount; idx++)
				{
					::VariantClear(&pItemValues[idx].vDataValue);
				}
			}
		}

		return HR;
	}

	cliHRESULT COPCDaGroup::Write(
		/*[in]*/ WriteValueArrays^ ItemValues,
		/*[out]*/ List<HandleAndHRESULT^>^ %HandleAndHResultList )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC DA Group has been Disposed!");

		HandleAndHResultList = nullptr;
		unsigned long dwCount = 0;
		if (nullptr != ItemValues->DoubleServerAlias)
			dwCount += ItemValues->DoubleServerAlias->Length;
		if (nullptr != ItemValues->UintServerAlias)
			dwCount += ItemValues->UintServerAlias->Length;
		if (nullptr != ItemValues->ObjectServerAlias)
			dwCount += ItemValues->ObjectServerAlias->Length;
		unsigned long * hServer = new unsigned long[dwCount];
		::VARIANT * pItemValues = new ::VARIANT[dwCount];

		::HRESULT * pErrors = nullptr;

		try
		{
			unsigned long opcIdx = 0;
			if (nullptr != ItemValues->DoubleServerAlias)
			{
				for (int idx = 0;
					idx < ItemValues->DoubleServerAlias->Length; idx++)
				{
					hServer[opcIdx] = ItemValues->DoubleServerAlias[idx];
					_variant_t var(ItemValues->DoubleValues[idx]);
					pItemValues[opcIdx++] = var.Detach();
				}
			}
			if (nullptr != ItemValues->UintServerAlias)
			{
				for (int idx = 0;
					idx < ItemValues->UintServerAlias->Length; idx++)
				{
					hServer[opcIdx] = ItemValues->UintServerAlias[idx];
					_variant_t var(ItemValues->UintValues[idx]);
					pItemValues[opcIdx++] = var.Detach();
				}
			}
			if (nullptr != ItemValues->ObjectServerAlias)
			{
				for (int idx = 0;
					idx < ItemValues->ObjectServerAlias->Length; idx++)
				{
					hServer[opcIdx] = ItemValues->ObjectServerAlias[idx];
					System::IntPtr vp = (System::IntPtr)&(pItemValues[opcIdx++]);
					Marshal::GetNativeVariantForObject(ItemValues->ObjectValues[idx], vp);
				}
			}
			cliHRESULT HR = IOPCSyncIO->Write(dwCount, hServer, pItemValues, &pErrors);
			if ((HR.IsS_FALSE || HR.Failed) && (nullptr != pErrors))
			{
				HandleAndHResultList = gcnew List<HandleAndHRESULT^>();
				for (unsigned long idx = 0; idx < dwCount; idx++)
				{
					if (S_OK != pErrors[idx])
					{
						HandleAndHRESULT^ hdlAndHr = gcnew HandleAndHRESULT();
						hdlAndHr->hResult = pErrors[idx];
						hdlAndHr->Handle = m_hServerTohClient[hServer[idx]];
						HandleAndHResultList->Add(hdlAndHr);
					}
				}
			}
			else
			{
				if (HR.Failed && nullptr == pErrors)
				{
					HandleAndHResultList = gcnew List<HandleAndHRESULT^>(dwCount);
					for (int idx = 0;
						idx < ItemValues->DoubleServerAlias->Length; idx++)
					{
						HandleAndHRESULT^ hdlAndHr = gcnew HandleAndHRESULT();
						hdlAndHr->hResult = HR.hResult;
						hdlAndHr->Handle = m_hServerTohClient[ItemValues->DoubleServerAlias[idx]];
						HandleAndHResultList->Add(hdlAndHr);
					}
					for (int idx = 0;
						idx < ItemValues->UintServerAlias->Length; idx++)
					{
						HandleAndHRESULT^ hdlAndHr = gcnew HandleAndHRESULT();
						hdlAndHr->hResult = HR.hResult;
						hdlAndHr->Handle = m_hServerTohClient[ItemValues->UintServerAlias[idx]];
						HandleAndHResultList->Add(hdlAndHr);
					}
					for (int idx = 0;
						idx < ItemValues->ObjectServerAlias->Length; idx++)
					{
						HandleAndHRESULT^ hdlAndHr = gcnew HandleAndHRESULT();
						hdlAndHr->hResult = HR.hResult;
						hdlAndHr->Handle = m_hServerTohClient[ItemValues->ObjectServerAlias[idx]];
						HandleAndHResultList->Add(hdlAndHr);
					}
				}
			}

			return HR;
		}
		finally
		{
			for (unsigned long idx = 0; idx < dwCount; idx++)
			{
				::VariantClear(&pItemValues[idx]);
			}

			if (pErrors != nullptr)
			{
				::CoTaskMemFree(pErrors);
				pErrors = nullptr;
			}

			if (pItemValues != nullptr)
			{
				delete[] pItemValues;
				pItemValues = nullptr;
			}

			if (hServer != nullptr)
			{
				delete[] hServer;
				hServer = nullptr;
			}
		}
	}

	// IOPCAsyncIO2
	cliHRESULT COPCDaGroup::Read(
		/*[in]*/ List<unsigned int>^ hServerList,
		/*[in]*/ unsigned int dwTransactionID,
		/*[out]*/ unsigned int %dwCancelID,
		/*[out]*/ List<HandleAndHRESULT^>^ %ErrorsList )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC DA Group has been Disposed!");

		ErrorsList = nullptr;
		unsigned long dwCount = hServerList->Count;
		unsigned long * hServer = new unsigned long[dwCount];
		unsigned long * phServer = hServer;
		for each (unsigned long svrHdl in hServerList) *phServer++ = svrHdl;
		unsigned long lCancelID = 0;
		::HRESULT * pErrors = nullptr;

		try
		{
			cliHRESULT HR = IOPCAsyncIO2->Read(dwCount, hServer, dwTransactionID, &lCancelID, &pErrors);
			if (HR.Succeeded)
			{
				dwCancelID = lCancelID;
				if (HR.IsS_FALSE)
				{
					ErrorsList = gcnew List<HandleAndHRESULT^>();
					for (unsigned long idx = 0; idx < dwCount; idx++)
					{
						if (S_OK != pErrors[idx])
						{
							HandleAndHRESULT^ hdlAndHr = gcnew HandleAndHRESULT();
							hdlAndHr->hResult = pErrors[idx];
							hdlAndHr->Handle = m_hServerTohClient[hServer[idx]];
							ErrorsList->Add(hdlAndHr);
						}
					}
				}
			}

			return HR;
		}
		finally
		{
			if (pErrors != nullptr)
			{
				::CoTaskMemFree(pErrors);
				pErrors = nullptr;
			}

			if (hServer != nullptr)
			{
				delete[] hServer;
				hServer = nullptr;
			}
		}
	}

	cliHRESULT COPCDaGroup::Write(
		/*[in]*/ WriteValueArrays^ ItemValues,
		/*[in]*/ unsigned int dwTransactionID,
		/*[out]*/ unsigned int %pdwCancelID,
		/*[out]*/ List<HandleAndHRESULT^>^ %ErrorsList )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC DA Group has been Disposed!");

		return cliHRESULT(E_NOTIMPL);
	}

	cliHRESULT COPCDaGroup::Refresh2(
		/*[in]*/ OPCDATASOURCE dwSource,
		/*[in]*/ unsigned int dwTransactionID,
		/*[out]*/ unsigned int %dwCancelID )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC DA Group has been Disposed!");

		unsigned long lCancelID = 0;
		cliHRESULT HR = IOPCAsyncIO2->Refresh2(
			((tagOPCDATASOURCE)(unsigned short)dwSource), dwTransactionID, &lCancelID);
		dwCancelID = lCancelID;
		return HR;
	}

	cliHRESULT COPCDaGroup::Cancel2(
		/*[in]*/ unsigned int dwCancelID )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC DA Group has been Disposed!");

		cliHRESULT HR = IOPCAsyncIO2->Cancel2(dwCancelID);
		return HR;
	}

	cliHRESULT COPCDaGroup::SetEnable(
		/*[in]*/ bool bEnable )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC DA Group has been Disposed!");

		long dwEnable = (bEnable) ? 1 : 0;
		cliHRESULT HR = IOPCAsyncIO2->SetEnable(dwEnable);
		return HR;
	}

	cliHRESULT COPCDaGroup::GetEnable(
		/*[out]*/ bool %bEnable )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC DA Group has been Disposed!");

		long dwEnable = 0;
		cliHRESULT HR = IOPCAsyncIO2->GetEnable(&dwEnable);
		bEnable = (0 != dwEnable) ? true : false;
		return HR;
	}

}}}}
