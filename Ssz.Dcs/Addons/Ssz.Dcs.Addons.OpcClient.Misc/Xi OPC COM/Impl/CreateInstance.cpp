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
#include "CreateInstance.h"
#include ".\OPCae\OPCAeServer.h"
#include ".\OPCda\OPCDaServer.h"
#include ".\OPChda\OPCHdaServer.h"
#include ".\OPChda\UsoHdaServer.h"

#include "Helper.h"

namespace Xi {
namespace OPC {
namespace COM {
namespace Impl {

	// All methods on this class are static 
	// thus there is never a need to create an instance.
	CCreateInstance::CCreateInstance(void)
	{
	}

	// ########################################################################
	// COM must be initialized prior to any use of COM.
	cliHRESULT CCreateInstance::InitializeCOM()
	{
		if (M_bComSetupDone) return cliHRESULT(S_OK);

		HRESULT hr2 = S_OK;
		HRESULT hr1 = ::CoInitializeEx(
			nullptr,
			COINIT_MULTITHREADED);
		if (SUCCEEDED(hr1))
		{
			// Try to set dynamic cloaking to allow OPC COM servers to impersonate the Xi user
			hr2 = ::CoInitializeSecurity( 
				nullptr, 
				-1, 
				nullptr, 
				nullptr, 
				RPC_C_AUTHN_LEVEL_NONE, 
				RPC_C_IMP_LEVEL_IMPERSONATE, 
				nullptr, 
				EOAC_DYNAMIC_CLOAKING, 
				nullptr);
			// if dynamic cloaking fails, use EAOC_NONE. In this case, the OPC COM server
			// will run under the user account that started the Xi server process
			if (FAILED(hr2))
			{
				hr2 = ::CoInitializeSecurity( 
					nullptr, 
					-1, 
					nullptr, 
					nullptr, 
					RPC_C_AUTHN_LEVEL_NONE, 
					RPC_C_IMP_LEVEL_IMPERSONATE, 
					nullptr, 
					EOAC_NONE, 
					nullptr);
			}
		}
		M_bComSetupDone = true;
		return (FAILED(hr1)) 
			? cliHRESULT(hr1) : (FAILED(hr2)) 
			? cliHRESULT(hr2) : (S_OK != hr1) 
			? cliHRESULT(hr1) : cliHRESULT(hr2);
	}

	// ########################################################################
	// Create an instance of a managed OPC A&E Server
	cliHRESULT CCreateInstance::CreateInstanceAE(
		String^ progID, 
		IOPCEventServerCli^ %iOpcEventServer,
		ServerDescription^ serverDescription )
	{
		iOpcEventServer = nullptr;
		cliHRESULT HR = E_FAIL;

		::IOPCEventServer * pIOPCEventServer = nullptr;
		CLSID opcServerCLSID;
		LPWSTR szProgId =  static_cast<wchar_t*>((Marshal::StringToHGlobalUni(progID)).ToPointer());
		HR = ::CLSIDFromProgID( szProgId, &opcServerCLSID );
		if ( HR.Succeeded ) {
			HR = ::CoCreateInstance( opcServerCLSID, NULL, CLSCTX_LOCAL_SERVER, __uuidof(::IOPCEventServer),
				reinterpret_cast<void**>(&pIOPCEventServer) );
			if ( HR.Succeeded ) 
			{
				COPCAeServer^ rCOPCAeServer = gcnew COPCAeServer(HR, pIOPCEventServer);
				iOpcEventServer = dynamic_cast<IOPCEventServerCli^>(rCOPCAeServer);
				rCOPCAeServer->ServerDescription = serverDescription;
			}
		}
		Marshal::FreeHGlobal((IntPtr)szProgId);
		return HR;
	}

	// ########################################################################
	// Create an instance of a managed OPC A&E Server
	cliHRESULT CCreateInstance::CreateInstanceAE_Clsid(
		String^ clsid,
		IOPCEventServerCli^ %iOpcEventServer,
		ServerDescription^ serverDescription)
	{
		iOpcEventServer = nullptr;
		cliHRESULT HR = E_FAIL;

		::IOPCEventServer * pIOPCEventServer = nullptr;
		CLSID opcServerCLSID;
		LPWSTR szClsid = static_cast<wchar_t*>((Marshal::StringToHGlobalUni(clsid)).ToPointer());
		HR = ::CLSIDFromString(szClsid, &opcServerCLSID);
		if (HR.Succeeded) {
			HR = ::CoCreateInstance(opcServerCLSID, NULL, CLSCTX_LOCAL_SERVER, __uuidof(::IOPCEventServer),
				reinterpret_cast<void**>(&pIOPCEventServer));
			if (HR.Succeeded)
			{
				COPCAeServer^ rCOPCAeServer = gcnew COPCAeServer(HR, pIOPCEventServer);
				iOpcEventServer = dynamic_cast<IOPCEventServerCli^>(rCOPCAeServer);
				rCOPCAeServer->ServerDescription = serverDescription;
			}
		}
		Marshal::FreeHGlobal((IntPtr)szClsid);
		return HR;
	}

	// ########################################################################
	// Create an instance of a managed OPC A&E Server
	cliHRESULT CCreateInstance::CreateInstanceAE(
		String^ hostName,
		String^ progID, 
		IOPCEventServerCli^ %iOpcEventServer,
		ServerDescription^ serverDescription )
	{
		cliHRESULT HR = E_FAIL;
		iOpcEventServer = nullptr;

		if (::String::IsNullOrEmpty(hostName))
		{
			HR = CreateInstanceAE(progID, iOpcEventServer, serverDescription);
		}
		else
		{
			::IOPCEventServer * pIOPCEventServer = nullptr;
			CLSID opcServerCLSID;
			LPWSTR szHostName = static_cast<wchar_t*>((Marshal::StringToHGlobalUni(hostName)).ToPointer());
			LPWSTR szProgId =  static_cast<wchar_t*>((Marshal::StringToHGlobalUni(progID)).ToPointer());

			HR = CHelper::CLSIDFromProgID( hostName, progID, opcServerCLSID );
			if ( HR.Succeeded ) {
				COSERVERINFO serverInfo;
				serverInfo.dwReserved1 = 0;
				serverInfo.pwszName = szHostName;
				serverInfo.pAuthInfo = NULL;
				serverInfo.dwReserved2 = 0;
				ULONG cmq = 1;
				MULTI_QI queue[1];
				queue[0].pIID = &::IID_IOPCEventServer;
				queue[0].pItf = NULL;
				queue[0].hr = 0;
				HR = ::CoCreateInstanceEx(opcServerCLSID, NULL, CLSCTX_SERVER, &serverInfo, cmq, queue);
				if ( HR.Succeeded ) 
				{
					pIOPCEventServer = static_cast<::IOPCEventServer*>(queue[0].pItf);
					COPCAeServer^ rCOPCAeServer = gcnew COPCAeServer(HR, pIOPCEventServer);
					iOpcEventServer = dynamic_cast<IOPCEventServerCli^>(rCOPCAeServer);
					rCOPCAeServer->ServerDescription = serverDescription;
				}
			}
			Marshal::FreeHGlobal((IntPtr)szHostName);
			Marshal::FreeHGlobal((IntPtr)szProgId);
		}
		return HR;
	}

	// ########################################################################
	// Create an instance of a managed OPC A&E Server
	cliHRESULT CCreateInstance::CreateInstanceAE_Clsid(
		String^ hostName,
		String^ clsid,
		IOPCEventServerCli^ %iOpcEventServer,
		ServerDescription^ serverDescription)
	{
		cliHRESULT HR = E_FAIL;
		iOpcEventServer = nullptr;

		if (::String::IsNullOrEmpty(hostName))
		{
			HR = CreateInstanceAE_Clsid(clsid, iOpcEventServer, serverDescription);
		}
		else
		{
			::IOPCEventServer * pIOPCEventServer = nullptr;
			LPWSTR szHostName = static_cast<wchar_t*>((Marshal::StringToHGlobalUni(hostName)).ToPointer());
			CLSID opcServerCLSID;
			LPWSTR szClsid = static_cast<wchar_t*>((Marshal::StringToHGlobalUni(clsid)).ToPointer());
			HR = ::CLSIDFromString(szClsid, &opcServerCLSID);
			if (HR.Succeeded) {
				COSERVERINFO serverInfo;
				serverInfo.dwReserved1 = 0;
				serverInfo.pwszName = szHostName;
				serverInfo.pAuthInfo = NULL;
				serverInfo.dwReserved2 = 0;
				ULONG cmq = 1;
				MULTI_QI queue[1];
				queue[0].pIID = &::IID_IOPCEventServer;
				queue[0].pItf = NULL;
				queue[0].hr = 0;
				HR = ::CoCreateInstanceEx(opcServerCLSID, NULL, CLSCTX_SERVER, &serverInfo, cmq, queue);
				if (HR.Succeeded)
				{
					pIOPCEventServer = static_cast<::IOPCEventServer*>(queue[0].pItf);
					COPCAeServer^ rCOPCAeServer = gcnew COPCAeServer(HR, pIOPCEventServer);
					iOpcEventServer = dynamic_cast<IOPCEventServerCli^>(rCOPCAeServer);
					rCOPCAeServer->ServerDescription = serverDescription;
				}
			}
			Marshal::FreeHGlobal((IntPtr)szHostName);
			Marshal::FreeHGlobal((IntPtr)szClsid);
		}
		return HR;
	}

	// ########################################################################
	// Create an instance of a managed OPC DA Server
	cliHRESULT CCreateInstance::CreateInstanceDA(
		String^ progID, 
		IOPCServerCli^ %iOpcServer,
		ServerDescription^ serverDescription )
	{
		iOpcServer = nullptr;
		cliHRESULT HR = E_FAIL;

		::IOPCServer * pIOPCServer = nullptr;
		CLSID opcServerCLSID;
		LPWSTR szProgId =  static_cast<wchar_t*>((Marshal::StringToHGlobalUni(progID)).ToPointer());
		HR = ::CLSIDFromProgID( szProgId, &opcServerCLSID );
		if ( HR.Succeeded ) {
			HR = ::CoCreateInstance( opcServerCLSID, NULL, CLSCTX_LOCAL_SERVER, __uuidof(::IOPCServer),
				reinterpret_cast<void**>(&pIOPCServer) );
			if ( HR.Succeeded ) {
				COPCDaServer^ rCOPCDaServer = gcnew COPCDaServer(HR, pIOPCServer);
				if ( HR.Succeeded ) {
					iOpcServer = dynamic_cast<IOPCServerCli^>(rCOPCDaServer);
				}
				rCOPCDaServer->ServerDescription = serverDescription;
			}
		}
		Marshal::FreeHGlobal((IntPtr)szProgId);
		return HR;
	}

	// ########################################################################
	// Create an instance of a managed OPC DA Server
	cliHRESULT CCreateInstance::CreateInstanceDA_Clsid(
		String^ clsid,
		IOPCServerCli^ %iOpcServer,
		ServerDescription^ serverDescription)
	{
		iOpcServer = nullptr;
		cliHRESULT HR = E_FAIL;

		::IOPCServer * pIOPCServer = nullptr;
		CLSID opcServerCLSID;
		LPWSTR szClsid = static_cast<wchar_t*>((Marshal::StringToHGlobalUni(clsid)).ToPointer());
		HR = ::CLSIDFromString(szClsid, &opcServerCLSID);
		if (HR.Succeeded) {
			HR = ::CoCreateInstance(opcServerCLSID, NULL, CLSCTX_LOCAL_SERVER, __uuidof(::IOPCServer),
				reinterpret_cast<void**>(&pIOPCServer));
			if (HR.Succeeded) {
				COPCDaServer^ rCOPCDaServer = gcnew COPCDaServer(HR, pIOPCServer);
				if (HR.Succeeded) {
					iOpcServer = dynamic_cast<IOPCServerCli^>(rCOPCDaServer);
				}
				rCOPCDaServer->ServerDescription = serverDescription;
			}
		}
		Marshal::FreeHGlobal((IntPtr)szClsid);
		return HR;
	}

	// ########################################################################
	// Create an instance of a managed OPC DA Server
	cliHRESULT CCreateInstance::CreateInstanceDA(
		String^ hostName,
		String^ progID, 
		IOPCServerCli^ %iOpcServer,
		ServerDescription^ serverDescription )
	{
		cliHRESULT HR = E_FAIL;
		iOpcServer = nullptr;

		if (::String::IsNullOrEmpty(hostName))
		{
			return CreateInstanceDA(progID, iOpcServer, serverDescription);
		}
		else
		{
			::IOPCServer * pIOPCServer = nullptr;
			CLSID opcServerCLSID;
			LPWSTR szHostName = static_cast<wchar_t*>((Marshal::StringToHGlobalUni(hostName)).ToPointer());
			LPWSTR szProgId =  static_cast<wchar_t*>((Marshal::StringToHGlobalUni(progID)).ToPointer());
			
			HR = CHelper::CLSIDFromProgID( hostName, progID, opcServerCLSID );
			if ( HR.Succeeded ) {
				COSERVERINFO serverInfo;
				serverInfo.dwReserved1 = 0;
				serverInfo.pwszName = szHostName;
				serverInfo.pAuthInfo = NULL;
				serverInfo.dwReserved2 = 0;
				ULONG cmq = 1;
				MULTI_QI queue[1];
				queue[0].pIID = &::IID_IOPCServer;
				queue[0].pItf = NULL;
				queue[0].hr = 0;
				HR = ::CoCreateInstanceEx(opcServerCLSID, NULL, CLSCTX_SERVER, &serverInfo, cmq, queue);
				if ( HR.Succeeded ) {
					pIOPCServer = static_cast<::IOPCServer*>(queue[0].pItf);
					COPCDaServer^ rCOPCDaServer = gcnew COPCDaServer(HR, pIOPCServer);
					if ( HR.Succeeded ) {
						iOpcServer = dynamic_cast<IOPCServerCli^>(rCOPCDaServer);
					}
					rCOPCDaServer->ServerDescription = serverDescription;
				}
			}
			Marshal::FreeHGlobal((IntPtr)szHostName);
			Marshal::FreeHGlobal((IntPtr)szProgId);
		}
		return HR;
	}

	// ########################################################################
	// Create an instance of a managed OPC DA Server
	cliHRESULT CCreateInstance::CreateInstanceDA_Clsid(
		String^ hostName,
		String^ clsid,
		IOPCServerCli^ %iOpcServer,
		ServerDescription^ serverDescription)
	{
		cliHRESULT HR = E_FAIL;
		iOpcServer = nullptr;

		if (::String::IsNullOrEmpty(hostName))
		{
			return CreateInstanceDA_Clsid(clsid, iOpcServer, serverDescription);
		}
		else
		{
			::IOPCServer * pIOPCServer = nullptr;
			LPWSTR szHostName = static_cast<wchar_t*>((Marshal::StringToHGlobalUni(hostName)).ToPointer());
			CLSID opcServerCLSID;
			LPWSTR szClsid = static_cast<wchar_t*>((Marshal::StringToHGlobalUni(clsid)).ToPointer());
			HR = ::CLSIDFromString(szClsid, &opcServerCLSID);

			if (HR.Succeeded) {
				COSERVERINFO serverInfo;
				serverInfo.dwReserved1 = 0;
				serverInfo.pwszName = szHostName;
				serverInfo.pAuthInfo = NULL;
				serverInfo.dwReserved2 = 0;
				ULONG cmq = 1;
				MULTI_QI queue[1];
				queue[0].pIID = &::IID_IOPCServer;
				queue[0].pItf = NULL;
				queue[0].hr = 0;
				HR = ::CoCreateInstanceEx(opcServerCLSID, NULL, CLSCTX_SERVER, &serverInfo, cmq, queue);
				if (HR.Succeeded) {
					pIOPCServer = static_cast<::IOPCServer*>(queue[0].pItf);
					COPCDaServer^ rCOPCDaServer = gcnew COPCDaServer(HR, pIOPCServer);
					if (HR.Succeeded) {
						iOpcServer = dynamic_cast<IOPCServerCli^>(rCOPCDaServer);
					}
					rCOPCDaServer->ServerDescription = serverDescription;
				}
			}
			Marshal::FreeHGlobal((IntPtr)szHostName);
			Marshal::FreeHGlobal((IntPtr)szClsid);
		}
		return HR;
	}

	// ########################################################################
	// Create an instance of a managed OPC HDA Server
	cliHRESULT CCreateInstance::CreateInstanceHDA(
		String^ progID, 
		IOPCHDA_ServerCli^ %iOpcHda_Server,
		ServerDescription^ serverDescription )
	{
		iOpcHda_Server = nullptr;
		cliHRESULT HR = E_FAIL;

		::IOPCHDA_Server * pIOPCHDA_Server = nullptr;
		CLSID opcServerCLSID;
		LPWSTR szProgId =  static_cast<wchar_t*>((Marshal::StringToHGlobalUni(progID)).ToPointer());
		HR = ::CLSIDFromProgID( szProgId, &opcServerCLSID );
		if ( HR.Succeeded ) {
			HR = ::CoCreateInstance( opcServerCLSID, NULL, CLSCTX_LOCAL_SERVER, __uuidof(::IOPCHDA_Server),
				reinterpret_cast<void**>(&pIOPCHDA_Server) );
			if ( HR.Succeeded ) {
				COPCHdaServer^ rCOPCHdaServer = gcnew COPCHdaServer(HR, pIOPCHDA_Server);
				iOpcHda_Server = dynamic_cast<IOPCHDA_ServerCli^>(rCOPCHdaServer);
				rCOPCHdaServer->ServerDescription = serverDescription;
			}
		}
		Marshal::FreeHGlobal((IntPtr)szProgId);
		return HR;
	}

	// ########################################################################
	// Create an instance of a managed OPC HDA Server
	cliHRESULT CCreateInstance::CreateInstanceHDA_Clsid(
		String^ clsid,
		IOPCHDA_ServerCli^ %iOpcHda_Server,
		ServerDescription^ serverDescription)
	{
		iOpcHda_Server = nullptr;
		cliHRESULT HR = E_FAIL;

		::IOPCHDA_Server * pIOPCHDA_Server = nullptr;
		CLSID opcServerCLSID;
		LPWSTR szClsid = static_cast<wchar_t*>((Marshal::StringToHGlobalUni(clsid)).ToPointer());
		HR = ::CLSIDFromString(szClsid, &opcServerCLSID);
		if (HR.Succeeded) {
			HR = ::CoCreateInstance(opcServerCLSID, NULL, CLSCTX_LOCAL_SERVER, __uuidof(::IOPCHDA_Server),
				reinterpret_cast<void**>(&pIOPCHDA_Server));
			if (HR.Succeeded) {
				COPCHdaServer^ rCOPCHdaServer = gcnew COPCHdaServer(HR, pIOPCHDA_Server);
				iOpcHda_Server = dynamic_cast<IOPCHDA_ServerCli^>(rCOPCHdaServer);
				rCOPCHdaServer->ServerDescription = serverDescription;
			}
		}
		Marshal::FreeHGlobal((IntPtr)szClsid);
		return HR;
	}

	// ########################################################################
	// Create an instance of a managed OPC HDA Server
	cliHRESULT CCreateInstance::CreateInstanceHDA( 
		String^ hostName,
		String^ progID, 
		IOPCHDA_ServerCli^ %iOpcHda_Server,
		ServerDescription^ serverDescription )
	{
		cliHRESULT HR = E_FAIL;
		iOpcHda_Server = nullptr;

		if (::String::IsNullOrEmpty(hostName))
		{
			return CreateInstanceHDA(progID, iOpcHda_Server, serverDescription);
		}
		else
		{
			::IOPCHDA_Server * pIOPCHDA_Server = nullptr;
			CLSID opcServerCLSID;
			LPWSTR szHostName = static_cast<wchar_t*>((Marshal::StringToHGlobalUni(hostName)).ToPointer());
			LPWSTR szProgId =  static_cast<wchar_t*>((Marshal::StringToHGlobalUni(progID)).ToPointer());

			HR = CHelper::CLSIDFromProgID( hostName, progID, opcServerCLSID );
			if ( HR.Succeeded ) {
				COSERVERINFO serverInfo;
				serverInfo.dwReserved1 = 0;
				serverInfo.pwszName = szHostName;
				serverInfo.pAuthInfo = NULL;
				serverInfo.dwReserved2 = 0;
				ULONG cmq = 1;
				MULTI_QI queue[1];
				queue[0].pIID = &::IID_IOPCHDA_Server;
				queue[0].pItf = NULL;
				queue[0].hr = 0;
				HR = ::CoCreateInstanceEx(opcServerCLSID, NULL, CLSCTX_SERVER, &serverInfo, cmq, queue);
				if ( HR.Succeeded ) {
					pIOPCHDA_Server = static_cast<::IOPCHDA_Server*>(queue[0].pItf);
					COPCHdaServer^ rCOPCHdaServer = gcnew COPCHdaServer(HR, pIOPCHDA_Server);
					iOpcHda_Server = dynamic_cast<IOPCHDA_ServerCli^>(rCOPCHdaServer);
					rCOPCHdaServer->ServerDescription = serverDescription;
				}
			}
			Marshal::FreeHGlobal((IntPtr)szHostName);
			Marshal::FreeHGlobal((IntPtr)szProgId);
		}
		return HR;
	}

	// ########################################################################
	// Create an instance of a managed OPC HDA Server
	cliHRESULT CCreateInstance::CreateInstanceHDA_Clsid(
		String^ hostName,
		String^ clsid,
		IOPCHDA_ServerCli^ %iOpcHda_Server,
		ServerDescription^ serverDescription)
	{
		cliHRESULT HR = E_FAIL;
		iOpcHda_Server = nullptr;

		if (::String::IsNullOrEmpty(hostName))
		{
			return CreateInstanceHDA_Clsid(clsid, iOpcHda_Server, serverDescription);
		}
		else
		{
			::IOPCHDA_Server * pIOPCHDA_Server = nullptr;
			LPWSTR szHostName = static_cast<wchar_t*>((Marshal::StringToHGlobalUni(hostName)).ToPointer());
			CLSID opcServerCLSID;
			LPWSTR szClsid = static_cast<wchar_t*>((Marshal::StringToHGlobalUni(clsid)).ToPointer());
			HR = ::CLSIDFromString(szClsid, &opcServerCLSID);

			if (HR.Succeeded) {
				COSERVERINFO serverInfo;
				serverInfo.dwReserved1 = 0;
				serverInfo.pwszName = szHostName;
				serverInfo.pAuthInfo = NULL;
				serverInfo.dwReserved2 = 0;
				ULONG cmq = 1;
				MULTI_QI queue[1];
				queue[0].pIID = &::IID_IOPCHDA_Server;
				queue[0].pItf = NULL;
				queue[0].hr = 0;
				HR = ::CoCreateInstanceEx(opcServerCLSID, NULL, CLSCTX_SERVER, &serverInfo, cmq, queue);
				if (HR.Succeeded) {
					pIOPCHDA_Server = static_cast<::IOPCHDA_Server*>(queue[0].pItf);
					COPCHdaServer^ rCOPCHdaServer = gcnew COPCHdaServer(HR, pIOPCHDA_Server);
					iOpcHda_Server = dynamic_cast<IOPCHDA_ServerCli^>(rCOPCHdaServer);
					rCOPCHdaServer->ServerDescription = serverDescription;
				}
			}
			Marshal::FreeHGlobal((IntPtr)szHostName);
			Marshal::FreeHGlobal((IntPtr)szClsid);
		}
		return HR;
	}

#ifdef USO
	// ########################################################################
	// Create an instance of a managed Unisim HDA Server
	cliHRESULT CCreateInstance::CreateInstanceUsoHDA(
		String^ progID,
		IOPCServerCli ^opcDAServer,
		IOPCHDA_ServerCli^ %iOpcHda_Server,
		ServerDescription^ serverDescription)
	{
		iOpcHda_Server = nullptr;
		cliHRESULT HR = E_FAIL;

		uso_interfaces::IUsoOpcAccess * pIOPCHDA_Server = nullptr;
		CLSID opcServerCLSID;
		LPWSTR szProgId = static_cast<wchar_t*>((Marshal::StringToHGlobalUni(progID)).ToPointer());
		HR = ::CLSIDFromProgID(szProgId, &opcServerCLSID);
		if (HR.Succeeded) {
			HR = ::CoCreateInstance(opcServerCLSID, NULL, CLSCTX_LOCAL_SERVER, __uuidof(uso_interfaces::IUsoOpcAccess),
				reinterpret_cast<void**>(&pIOPCHDA_Server));
			if (HR.Succeeded) {
				CUsoHdaServer^ rCOPCHdaServer = gcnew CUsoHdaServer(HR, pIOPCHDA_Server, opcDAServer);
				iOpcHda_Server = dynamic_cast<IOPCHDA_ServerCli^>(rCOPCHdaServer);
			//	rCOPCHdaServer->ServerDescription = serverDescription;
			}
		}
		Marshal::FreeHGlobal((IntPtr)szProgId);
		return HR;
	}


	// ########################################################################
	// Create an instance of a managed Unisim HDA Server
	cliHRESULT CCreateInstance::CreateInstanceUsoHDA_Clsid(
		String^ clsid,
		IOPCServerCli ^opcDAServer,
		IOPCHDA_ServerCli^ %iOpcHda_Server,
		ServerDescription^ serverDescription)
	{
		iOpcHda_Server = nullptr;
		cliHRESULT HR = E_FAIL;

		uso_interfaces::IUsoOpcAccess * pIOPCHDA_Server = nullptr;
		CLSID opcServerCLSID;
		LPWSTR szClsid = static_cast<wchar_t*>((Marshal::StringToHGlobalUni(clsid)).ToPointer());
		HR = ::CLSIDFromString(szClsid, &opcServerCLSID);
		if (HR.Succeeded) {
			HR = ::CoCreateInstance(opcServerCLSID, NULL, CLSCTX_LOCAL_SERVER, __uuidof(uso_interfaces::IUsoOpcAccess),
				reinterpret_cast<void**>(&pIOPCHDA_Server));
			if (HR.Succeeded) {
				CUsoHdaServer^ rCOPCHdaServer = gcnew CUsoHdaServer(HR, pIOPCHDA_Server, opcDAServer);
				iOpcHda_Server = dynamic_cast<IOPCHDA_ServerCli^>(rCOPCHdaServer);
				//	rCOPCHdaServer->ServerDescription = serverDescription;
			}
		}
		Marshal::FreeHGlobal((IntPtr)szClsid);
		return HR;
	}

	// ########################################################################
	// Create an instance of a managed OPC HDA Server
	cliHRESULT CCreateInstance::CreateInstanceUsoHDA(
		String^ hostName,
		String^ progID,
		IOPCServerCli ^opcDAServer,
		IOPCHDA_ServerCli^ %iOpcHda_Server,
		ServerDescription^ serverDescription)
	{
		cliHRESULT HR = E_FAIL;
		iOpcHda_Server = nullptr;

		if (::String::IsNullOrEmpty(hostName))
		{
			return CreateInstanceUsoHDA(progID, opcDAServer, iOpcHda_Server, serverDescription);
		}
		else
		{
			uso_interfaces::IUsoOpcAccess * pIOPCHDA_Server = nullptr;
			CLSID opcServerCLSID;
			LPWSTR szHostName = static_cast<wchar_t*>((Marshal::StringToHGlobalUni(hostName)).ToPointer());
			LPWSTR szProgId = static_cast<wchar_t*>((Marshal::StringToHGlobalUni(progID)).ToPointer());

			HR = CHelper::CLSIDFromProgID(hostName, progID, opcServerCLSID);
			if (HR.Succeeded) {
				COSERVERINFO serverInfo;
				serverInfo.dwReserved1 = 0;
				serverInfo.pwszName = szHostName;
				serverInfo.pAuthInfo = NULL;
				serverInfo.dwReserved2 = 0;
				ULONG cmq = 1;
				MULTI_QI queue[1];
				queue[0].pIID = &__uuidof(uso_interfaces::IUsoOpcAccess);
				queue[0].pItf = NULL;
				queue[0].hr = 0;
				HR = ::CoCreateInstanceEx(opcServerCLSID, NULL, CLSCTX_SERVER, &serverInfo, cmq, queue);
				if (HR.Succeeded) {
					pIOPCHDA_Server = static_cast<uso_interfaces::IUsoOpcAccess*>(queue[0].pItf);
					CUsoHdaServer^ rCOPCHdaServer = gcnew CUsoHdaServer(HR, pIOPCHDA_Server, opcDAServer);
					iOpcHda_Server = dynamic_cast<IOPCHDA_ServerCli^>(rCOPCHdaServer);
				//	rCOPCHdaServer->ServerDescription = serverDescription;
				}
			}
			Marshal::FreeHGlobal((IntPtr)szHostName);
			Marshal::FreeHGlobal((IntPtr)szProgId);
		}
		return HR;
	}

	// ########################################################################
	// Create an instance of a managed OPC HDA Server
	cliHRESULT CCreateInstance::CreateInstanceUsoHDA_Clsid(
		String^ hostName,
		String^ clsid,
		IOPCServerCli ^opcDAServer,
		IOPCHDA_ServerCli^ %iOpcHda_Server,
		ServerDescription^ serverDescription)
	{
		cliHRESULT HR = E_FAIL;
		iOpcHda_Server = nullptr;

		if (::String::IsNullOrEmpty(hostName))
		{
			return CreateInstanceUsoHDA_Clsid(clsid, opcDAServer, iOpcHda_Server, serverDescription);
		}
		else
		{
			uso_interfaces::IUsoOpcAccess * pIOPCHDA_Server = nullptr;
			LPWSTR szHostName = static_cast<wchar_t*>((Marshal::StringToHGlobalUni(hostName)).ToPointer());
			CLSID opcServerCLSID;
			LPWSTR szClsid = static_cast<wchar_t*>((Marshal::StringToHGlobalUni(clsid)).ToPointer());
			HR = ::CLSIDFromString(szClsid, &opcServerCLSID);
			if (HR.Succeeded) {
				COSERVERINFO serverInfo;
				serverInfo.dwReserved1 = 0;
				serverInfo.pwszName = szHostName;
				serverInfo.pAuthInfo = NULL;
				serverInfo.dwReserved2 = 0;
				ULONG cmq = 1;
				MULTI_QI queue[1];
				queue[0].pIID = &__uuidof(uso_interfaces::IUsoOpcAccess);
				queue[0].pItf = NULL;
				queue[0].hr = 0;
				HR = ::CoCreateInstanceEx(opcServerCLSID, NULL, CLSCTX_SERVER, &serverInfo, cmq, queue);
				if (HR.Succeeded) {
					pIOPCHDA_Server = static_cast<uso_interfaces::IUsoOpcAccess*>(queue[0].pItf);
					CUsoHdaServer^ rCOPCHdaServer = gcnew CUsoHdaServer(HR, pIOPCHDA_Server, opcDAServer);
					iOpcHda_Server = dynamic_cast<IOPCHDA_ServerCli^>(rCOPCHdaServer);
					//	rCOPCHdaServer->ServerDescription = serverDescription;
				}
			}
			Marshal::FreeHGlobal((IntPtr)szHostName);
			Marshal::FreeHGlobal((IntPtr)szClsid);
		}
		return HR;
	}

#endif

}}}}
