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

#include "StdAfx.h"
#include "IEnumStrings.h"

namespace Xi {
namespace OPC {
namespace COM {
namespace Impl {

	CIEnumStrings::CIEnumStrings(::IEnumString* iEnumCOMString)
		: m_iEnumCOMString( iEnumCOMString )
	{
	}

	CIEnumStrings::~CIEnumStrings()
	{
		if (nullptr != m_iEnumCOMString)
			m_iEnumCOMString->Release();
		m_iEnumCOMString = nullptr;
		GC::SuppressFinalize(this);
	}

	CIEnumStrings::!CIEnumStrings()
	{
		if (nullptr != m_iEnumCOMString)
			m_iEnumCOMString->Release();
		m_iEnumCOMString = nullptr;
	}

	cliHRESULT CIEnumStrings::Next(unsigned int celt, List<String^>^ %rgelt)
	{
		LPOLESTR * pOleStrs = new LPOLESTR[celt];
		::ZeroMemory(pOleStrs, (sizeof(LPOLESTR) * celt));
		unsigned long cletFetched = 0;
		cliHRESULT HR = m_iEnumCOMString->Next(celt, pOleStrs, &cletFetched);
		if (HR.Succeeded)
		{
			rgelt = gcnew List<String^>(cletFetched);
			for (unsigned int idx = 0; idx < cletFetched; idx++)
			{
				String^ str = gcnew String(pOleStrs[idx]);
				rgelt->Add(str);
				
				if (pOleStrs[idx] != nullptr)
				{
					::CoTaskMemFree(pOleStrs[idx]);
					pOleStrs[idx] = nullptr;
				}
			}
		}

		if (pOleStrs != nullptr)
		{
			delete [] pOleStrs;
			pOleStrs = nullptr;
		}

		return HR;
	}

	cliHRESULT CIEnumStrings::Skip(unsigned int celt)
	{
		cliHRESULT HR = m_iEnumCOMString->Skip(celt);
		return HR;
	}

	cliHRESULT CIEnumStrings::Reset()
	{
		cliHRESULT HR = m_iEnumCOMString->Reset();
		return HR;
	}

	cliHRESULT CIEnumStrings::Clone(cliIEnumString^ %newIEnumString)
	{
		if (nullptr == m_iEnumCOMString) return cliHRESULT(E_FAIL);
		cliHRESULT HR = S_OK;
		m_iEnumCOMString->AddRef();
		CIEnumStrings^ iEnumString = gcnew CIEnumStrings(m_iEnumCOMString);
		newIEnumString = dynamic_cast<cliIEnumString^>(iEnumString);
		return HR;
	}

}}}}
