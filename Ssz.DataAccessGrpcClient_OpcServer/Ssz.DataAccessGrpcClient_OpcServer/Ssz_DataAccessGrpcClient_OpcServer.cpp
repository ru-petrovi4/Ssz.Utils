// Ssz_DataAccessGrpcClient_OpcServer.cpp : Implementation of WinMain


#include "stdafx.h"
#include "resource.h"
#include "Ssz_DataAccessGrpcClient_OpcServer_i.h"
using namespace Ssz::Utils::Net4;

OPC_BEGIN_CATEGORY_TABLE()
    OPC_CATEGORY_TABLE_ENTRY(OpcDaServer, CATID_OPCDAServer20, OPC_CATEGORY_DESCRIPTION_DA20)
    OPC_CATEGORY_TABLE_ENTRY(OpcDaServer, CATID_OPCDAServer30, OPC_CATEGORY_DESCRIPTION_DA30)
OPC_END_CATEGORY_TABLE()


class CSsz_DataAccessGrpcClient_OpcServerModule : public ATL::CAtlExeModuleT< CSsz_DataAccessGrpcClient_OpcServerModule >
	{
public :
	DECLARE_LIBID(LIBID_Ssz_DataAccessGrpcClient_OpcServerLib)
	DECLARE_REGISTRY_APPID_RESOURCEID(IDR_CTCMOPCSERVER, "{F5CDAB57-8F11-4555-A00C-FF4E97517353}")

    static HRESULT InitializeCom() throw()
    {
        HRESULT hr = ATL::CAtlExeModuleT< CSsz_DataAccessGrpcClient_OpcServerModule >::InitializeCom();

        if (FAILED(hr))
		{
			Logger::Error( "InitializeCom error code {0}", hr);
			return hr;
		}
        
        hr = CoInitializeSecurity(
            NULL, -1, NULL, NULL,
            RPC_C_AUTHN_LEVEL_NONE, 
            RPC_C_IMP_LEVEL_IDENTIFY, 
            NULL, EOAC_NONE, NULL );

		if (FAILED(hr))
			Logger::Error( "CoInitializeSecurity error code {0}", hr);
		return hr;
    }

    HRESULT RegisterServer(
		BOOL bRegTypeLib = FALSE,
		const CLSID* pCLSID = NULL) throw()
    {
        HRESULT hr = ATL::CAtlExeModuleT< CSsz_DataAccessGrpcClient_OpcServerModule >::RegisterServer(bRegTypeLib, pCLSID);

        if (SUCCEEDED(hr))
        {
            // add categories.
            for (int ii = 0; g_pCategoryTable[ii].pClsid != NULL; ii++)
            {
				HRESULT hr1;
                hr1 = RegisterClsidInCategory(*(g_pCategoryTable[ii].pClsid), *(g_pCategoryTable[ii].pCategory), g_pCategoryTable[ii].szDescription);
            }
        }

		Logger::Verbose( "CtcmOPCServer registering... {0}, code {1}", SUCCEEDED(hr) ? "Ok" : "Error", hr);
        return hr;
    }

    HRESULT UnregisterServer(
		BOOL bUnRegTypeLib,
		const CLSID* pCLSID = NULL) throw()
    {
		HRESULT hr;
        // remove categories.
        for (int ii = 0; g_pCategoryTable[ii].pClsid != NULL; ii++)
        {
            UnregisterClsidInCategory(*(g_pCategoryTable[ii].pClsid), *(g_pCategoryTable[ii].pCategory));
        }

        hr = ATL::CAtlExeModuleT< CSsz_DataAccessGrpcClient_OpcServerModule >::UnregisterServer(bUnRegTypeLib, pCLSID);
		Logger::Verbose( "CtcmOPCServer unregistering... {0}, code {1}", SUCCEEDED(hr) ? "Ok" : "Error", hr);
		return hr;
    }

	};

CSsz_DataAccessGrpcClient_OpcServerModule _AtlModule;



//
extern "C" int WINAPI _tWinMain(HINSTANCE /*hInstance*/, HINSTANCE /*hPrevInstance*/, 
								LPTSTR lpCmdLine, int nShowCmd)
{    
	//Logger::Verbose( "Starting... {0}", gcnew System::String(lpCmdLine));
	return _AtlModule.WinMain(nShowCmd);
}

