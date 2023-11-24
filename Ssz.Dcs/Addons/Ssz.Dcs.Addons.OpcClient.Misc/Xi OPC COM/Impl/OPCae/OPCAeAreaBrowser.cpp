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
#include "OPCAeAreaBrowser.h"
#include "..\Helper.h"
#include "..\IEnumStrings.h"

namespace Xi {
namespace OPC {
namespace COM {
namespace Impl {

	COPCAeAreaBrowser::COPCAeAreaBrowser(IOPCEventAreaBrowser *pIOPCEventAreaBrowser)
	{
		m_pIOPCEventAreaBrowser = pIOPCEventAreaBrowser;
	}

	COPCAeAreaBrowser::~COPCAeAreaBrowser()
	{
		if (DisposeThis(true))
			GC::SuppressFinalize(this);
	}

	COPCAeAreaBrowser::!COPCAeAreaBrowser()
	{
		DisposeThis(false);
	}

	bool COPCAeAreaBrowser::DisposeThis(bool isDisposing)
	{
		if (m_bHasBeenDisposed)
			return false;

		if (nullptr != m_pIOPCEventAreaBrowser)
			m_pIOPCEventAreaBrowser->Release();
		m_pIOPCEventAreaBrowser = nullptr;

		m_bHasBeenDisposed = true;
		return true;
	}

	// IOPCEventAreaBrowser
	cliHRESULT COPCAeAreaBrowser::ChangeBrowsePosition (
		/*[in]*/ cliOPCAEBROWSEDIRECTION dwBrowseDirection,
		/*[in]*/ String^ sString )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC A&E Area Browser has been Disposed!");

		OPCAEBROWSEDIRECTION direction = (OPCAEBROWSEDIRECTION)dwBrowseDirection;
		if (sString == nullptr)
			sString = "";
		LPWSTR	str = CHelper::ConvertStringToLPWSTR(sString);

		HRESULT hr = m_pIOPCEventAreaBrowser->ChangeBrowsePosition(direction, str);

		// free allocated memory
		Marshal::FreeHGlobal((IntPtr)str);

		return hr;
	}

	cliHRESULT COPCAeAreaBrowser:: BrowseOPCAreas (
		/*[in]*/ cliOPCAEBROWSETYPE dwBrowseFilterType,
		/*[in]*/ String^ sFilterCriteria,
		/*[out]*/ cliIEnumString^ %iEnumAreaNames )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC A&E Area Browser has been Disposed!");

		OPCAEBROWSETYPE areaType = (OPCAEBROWSETYPE)dwBrowseFilterType;
		if (sFilterCriteria == nullptr)
			sFilterCriteria = "";
		LPWSTR	pszFilterString = CHelper::ConvertStringToLPWSTR(sFilterCriteria);

		::IEnumString*	pEnumStr = nullptr;

		HRESULT hr = m_pIOPCEventAreaBrowser->BrowseOPCAreas(areaType, pszFilterString, &pEnumStr);
		CIEnumStrings^ iEnumString = gcnew CIEnumStrings(pEnumStr);
		iEnumAreaNames = dynamic_cast<cliIEnumString^>(iEnumString);

		Marshal::FreeHGlobal((IntPtr)pszFilterString);
		return cliHRESULT(hr);
	}

	cliHRESULT COPCAeAreaBrowser:: GetQualifiedAreaName (
		/*[in]*/ String^ sAreaName,
		/*[out]*/ String^ %sQualifiedAreaName )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC A&E Area Browser has been Disposed!");

		LPWSTR	szAreaName = CHelper::ConvertStringToLPWSTR(sAreaName);
		LPWSTR	pszQualifiedSourceName = nullptr;

		HRESULT hr = m_pIOPCEventAreaBrowser->GetQualifiedAreaName( szAreaName, &pszQualifiedSourceName );

		if( hr == S_OK )
		{
			sQualifiedAreaName = gcnew String( pszQualifiedSourceName);
		}

		// free allocated memory
		Marshal::FreeHGlobal((IntPtr)szAreaName);
		return cliHRESULT(hr);
	}

	cliHRESULT COPCAeAreaBrowser:: GetQualifiedSourceName (
		/*[in]*/ String^ sSourceName,
		/*[out]*/ String^ %sQualifiedSourceName )
	{
		if (m_bHasBeenDisposed)
			throw gcnew ObjectDisposedException("OPC A&E Area Browser has been Disposed!");

		LPWSTR	dwSourceName = CHelper::ConvertStringToLPWSTR(sSourceName);
		LPWSTR	pszQualifedName = nullptr;

		HRESULT hr = m_pIOPCEventAreaBrowser->GetQualifiedSourceName( dwSourceName, &pszQualifedName );

		if( hr == S_OK )
		{
			sQualifiedSourceName = gcnew String(pszQualifedName);
		}

		// free allocated memory
		Marshal::FreeHGlobal((IntPtr)dwSourceName);

		return cliHRESULT(hr);

	}

}}}}
