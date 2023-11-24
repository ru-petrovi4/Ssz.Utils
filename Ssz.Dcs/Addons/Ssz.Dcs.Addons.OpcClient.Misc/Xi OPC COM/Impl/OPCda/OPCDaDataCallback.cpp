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
#include "OPCDaDataCallback.h"
#include "OPCDaServer.h"

#include "..\Helper.h"

using namespace System::Collections::Generic;
using namespace Xi::Contracts::Data;

namespace Xi {
namespace OPC {
namespace COM {
namespace Impl {

	COPCDaDataCallback::COPCDaDataCallback(void)
	{

	}

	COPCDaDataCallback::~COPCDaDataCallback(void)
	{

	}

	STDMETHODIMP COPCDaDataCallback::OnDataChange(
		ULONG dwTransid, 
		ULONG hGroup, 
		HRESULT hrMasterQuality, 
		HRESULT hrMasterError, 
		ULONG dwCount, 
		ULONG * phClientItems, 
		VARIANT * pvValues, 
		USHORT * pwQualities, 
		_FILETIME * pftTimeStamps, 
		HRESULT * pErrors)
	{
		HRESULT hr = S_OK;
		if (nullptr != m_opcDaServer.Target)
		{
			COPCDaServer^ opcDaServer = (COPCDaServer^)(m_opcDaServer.Target);
			if (nullptr != opcDaServer)
			{
				if (nullptr != opcDaServer->m_onDataChange)
				{
					long dataTypeCounts[(short)TransportDataType::MaxTransportDataType];
					::ZeroMemory(dataTypeCounts, sizeof(dataTypeCounts));
					for (ULONG idx = 0; idx < dwCount; idx++)
					{
						TransportDataType transportType = CHelper::GetTransportDataType(pvValues[idx].vt);
						dataTypeCounts[(short)transportType] += 1;
					}
					DataValueArraysWithAlias^ valueArrays = gcnew DataValueArraysWithAlias(
						dataTypeCounts[(short)TransportDataType::Double],
						dataTypeCounts[(short)TransportDataType::Uint]
							+ dataTypeCounts[(short)TransportDataType::Unknown],
						dataTypeCounts[(short)TransportDataType::Object]);

					int dblIdx = 0;
					int lngIdx = 0;
					int objIdx = 0;
					for (ULONG idx = 0; idx < dwCount; idx++)
					{
						cliVARIANT dv = CHelper::ConvertFromVARIANT(&pvValues[idx]);
						unsigned int statusCode = (gcnew Xi::OPC::COM::API::XiStatusCodeFromOpcCOM(
							pwQualities[idx], pErrors[idx]))->StatusCode;
						DateTime timeStamp = DateTime::FromFileTimeUtc(*((__int64*)(&pftTimeStamps[idx])));
						TransportDataType transportType = CHelper::GetTransportDataType(pvValues[idx].vt);
						switch (transportType)
						{
						case TransportDataType::Unknown:
						{
							valueArrays->SetUint(lngIdx++, phClientItems[idx], statusCode, timeStamp, 0);
							break;
						}
						case TransportDataType::Double:
						{
							double doubleValue = (double)dv;
							valueArrays->SetDouble(dblIdx++, phClientItems[idx], statusCode, timeStamp, doubleValue);
							break;
						}
						case TransportDataType::Uint:
						{
							unsigned int uintValue = (unsigned int)dv;
							valueArrays->SetUint(lngIdx++, phClientItems[idx], statusCode, timeStamp, uintValue);
							break;
						}
						case TransportDataType::Object:
						{
							valueArrays->SetObject(objIdx++, phClientItems[idx], statusCode, timeStamp, dv.DataValue);
							break;
						}
						default:
							break;
						}
						::VariantClear(&pvValues[idx]);
					}
					opcDaServer->m_onDataChange(dwTransid, hGroup, 
						hrMasterQuality, hrMasterError, valueArrays);
				}
			}
		}
		return hr;
	}

	STDMETHODIMP COPCDaDataCallback::OnReadComplete(
		ULONG dwTransid, 
		ULONG hGroup, 
		HRESULT hrMasterQuality, 
		HRESULT hrMasterError,
		ULONG dwCount, 
		ULONG * phClientItems, 
		VARIANT * pvValues, 
		USHORT * pwQualities, 
		_FILETIME * pftTimeStamps, 
		HRESULT * pErrors)
	{
		return OnDataChange(dwTransid, hGroup, hrMasterQuality, hrMasterError, dwCount, 
			phClientItems, pvValues, pwQualities, pftTimeStamps, pErrors);
	}

	STDMETHODIMP COPCDaDataCallback::OnWriteComplete(
		ULONG dwTransid, 
		ULONG hGroup, 
		HRESULT hrMastererr,
		ULONG dwCount, 
		ULONG * phClientItems, 
		HRESULT * pErrors)
	{
		HRESULT hr = S_OK;

		return hr;
	}

	STDMETHODIMP COPCDaDataCallback::OnCancelComplete(
		ULONG dwTransid, 
		ULONG hGroup)
	{
		HRESULT hr = S_OK;

		return hr;
	}

}}}}
