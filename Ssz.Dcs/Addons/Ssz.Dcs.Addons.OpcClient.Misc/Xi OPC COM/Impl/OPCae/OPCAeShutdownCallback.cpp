/**********************************************************************
 * Copyright � 2009, 2010, 2011, 2012 OPC Foundation, Inc. 
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
#include "OPCAeShutdownCallback.h"
#include "OPCAeServer.h"

#include "..\Helper.h"

using namespace System::Collections::Generic;
using namespace Xi::Contracts::Data;

namespace Xi {
namespace OPC {
namespace COM {
namespace Impl {

	COPCAeShutdownCallback::COPCAeShutdownCallback(void)
	{
	}

	COPCAeShutdownCallback::~COPCAeShutdownCallback(void)
	{

	}

	// IOPCShutdown
	STDMETHODIMP COPCAeShutdownCallback::ShutdownRequest(
		LPWSTR szReason)
	{
		HRESULT hr = S_OK;
		if (nullptr != m_opcAeServer.Target)
		{
			System::String^ reason = gcnew System::String(szReason);
			((COPCAeServer^)(m_opcAeServer.Target))->m_shutdownRequest(reason);
		}
		return hr;
	}

}}}}
