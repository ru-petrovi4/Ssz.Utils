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
#include "OPCHdaBrowser.h"
#include "..\Helper.h"
#include "..\IEnumStrings.h"

using namespace System::Runtime::InteropServices;

namespace Xi {
namespace OPC {
namespace COM {
namespace Impl {

	COPCHdaBrowser::COPCHdaBrowser(cliHRESULT %HR, ::IOPCHDA_Browser * pIOPCHDA_Browser)
		: m_pIOPCHDA_Browser(pIOPCHDA_Browser)
		, m_bHasBeenDisposed(false)
	{
		HR = S_OK;
	}

	COPCHdaBrowser::~COPCHdaBrowser(void)
	{
		if (DisposeThis(true))
			GC::SuppressFinalize(this);
	}

	COPCHdaBrowser::!COPCHdaBrowser(void)
	{
		DisposeThis(false);
	}

	bool COPCHdaBrowser::DisposeThis(bool isDisposing)
	{
		if (m_bHasBeenDisposed)
			return false;

		if (nullptr != m_pIOPCHDA_Browser)
			m_pIOPCHDA_Browser->Release();
		m_pIOPCHDA_Browser = nullptr;

		m_bHasBeenDisposed = true;
		return true;
	}

	// IOPCHDA_Browser
	cliHRESULT COPCHdaBrowser::GetEnum( 
		/*[in]*/  OPCHDA_BROWSETYPE dwBrowseType,
		/*[out]*/ cliIEnumString^ %iEnumStrings)
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC HDA Browser has been Disposed!");

		::IEnumString * pIEnumString = nullptr;
		cliHRESULT HR = m_pIOPCHDA_Browser->GetEnum((tagOPCHDA_BROWSETYPE)dwBrowseType, &pIEnumString);
		CIEnumStrings^ iEnumStr = gcnew CIEnumStrings(pIEnumString);
		iEnumStrings = dynamic_cast<cliIEnumString^>(iEnumStr);
		return HR;
	}

	cliHRESULT COPCHdaBrowser::ChangeBrowsePosition( 
		/*[in]*/ OPCHDA_BROWSEDIRECTION dwBrowseDirection,
		/*[in]*/ String^ sString)
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC HDA Browser has been Disposed!");

		LPWSTR szString = (nullptr != sString)
			? CHelper::ConvertStringToLPWSTR(sString)
			: CHelper::ConvertStringToLPWSTR(L"");
		cliHRESULT HR = m_pIOPCHDA_Browser->ChangeBrowsePosition(
			((tagOPCHDA_BROWSEDIRECTION)(unsigned short)dwBrowseDirection), szString);
		Marshal::FreeHGlobal((IntPtr)szString);
		return HR;
	}

	cliHRESULT COPCHdaBrowser::GetItemID(
		/*[in]*/ String^ sNode,
		/*[out]*/ String^ %sItemID)
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC HDA Browser has been Disposed!");

		if (nullptr == sNode) return cliHRESULT(E_INVALIDARG);
		LPWSTR szNode = CHelper::ConvertStringToLPWSTR(sNode);
		LPWSTR szItemID = nullptr;
		cliHRESULT HR = m_pIOPCHDA_Browser->GetItemID(szNode, &szItemID);
		sItemID = gcnew String(szItemID);
		
		if (szItemID != nullptr)
		{
			::CoTaskMemFree(szItemID);
			szItemID = nullptr;
		}
		
		Marshal::FreeHGlobal((IntPtr)szNode);
		
		return HR;
	}

	cliHRESULT COPCHdaBrowser::GetBranchPosition( 
		/*[out]*/String^ %sBranchPos)
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC HDA Browser has been Disposed!");

		LPWSTR szBranchPos = nullptr;
		cliHRESULT HR = m_pIOPCHDA_Browser->GetBranchPosition(&szBranchPos);
		sBranchPos = gcnew String(szBranchPos);
		
		if (szBranchPos != nullptr)
		{
			::CoTaskMemFree(szBranchPos);
			szBranchPos = nullptr;
		}
		
		return HR;
	}

}}}}
