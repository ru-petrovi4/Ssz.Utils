// OpcDaServer.h : Declaration of the COpcDaServer

#pragma once
#include "resource.h"       // main symbols



#include "Ssz_DataAccessGrpcClient_OpcServer_i.h"

#include "COpcDaCache.h"
#include "COpcDaGroup.h"
#include "COpcDaTransaction.h"
#include "COpcThread.h"
#include "WorkingThread.h"

using namespace ATL;

#include <vcclr.h>

using namespace Ssz::Utils::Net4;

// COpcDaServer

class ATL_NO_VTABLE COpcDaServer :
	public CComObjectRootEx<CComMultiThreadModel>,
	public CComCoClass<COpcDaServer, &CLSID_OpcDaServer>,
	public COpcCommon,
	public COpcCPContainer,
	public IOPCBrowseServerAddressSpace,
    public IOPCItemProperties,
    public IOPCServer,
    public IOPCBrowse,
    public IOPCItemIO	
{
public:
	COpcDaServer();

	~COpcDaServer();
	

DECLARE_REGISTRY_RESOURCEID(IDR_CTCMOPCSERVER)

DECLARE_NOT_AGGREGATABLE(COpcDaServer)

BEGIN_COM_MAP(COpcDaServer)
    COM_INTERFACE_ENTRY(IOPCCommon)
    COM_INTERFACE_ENTRY(IConnectionPointContainer)
    COM_INTERFACE_ENTRY(IOPCServer)
    COM_INTERFACE_ENTRY(IOPCBrowseServerAddressSpace)
    COM_INTERFACE_ENTRY(IOPCBrowse)
    COM_INTERFACE_ENTRY(IOPCItemProperties)
    COM_INTERFACE_ENTRY(IOPCItemIO)
    COM_INTERFACE_ENTRY(IOPCSecurityNT)
    COM_INTERFACE_ENTRY(IOPCSecurityPrivate)	
END_COM_MAP()



	DECLARE_PROTECT_FINAL_CONSTRUCT()

	HRESULT FinalConstruct();	

	void FinalRelease();	

	// CreateGroup
    virtual COpcDaGroup* CreateGroup(COpcDaServer& cServer, const COpcString& cName) { return new COpcDaGroup(cServer, cName); }

    // SetLastUpdateTime
    void SetLastUpdateTime();

    // SetGroupName
    HRESULT SetGroupName(
        const COpcString& cOldName, 
        const COpcString& cNewName);

    // CreateGroup
    HRESULT CreateGroup(
        const COpcString& cName, 
        COpcDaGroup**     ppGroup);

    // CloneGroup
    HRESULT CloneGroup(
        const COpcString& cName, 
        const COpcString& cCloneName, 
        COpcDaGroup**     ppClone);

    // DeleteGroup
    HRESULT DeleteGroup(const COpcString& cName);

public:
	//=========================================================================
    // IOPCServer

    // AddGroup
    STDMETHODIMP AddGroup(
        LPCWSTR    szName,
        BOOL       bActive,
        DWORD      dwRequestedUpdateRate,
        OPCHANDLE  hClientGroup,
        LONG*      pTimeBias,
        FLOAT*     pPercentDeadband,
        DWORD      dwLCID,
        OPCHANDLE* phServerGroup,
        DWORD*     pRevisedUpdateRate,
        REFIID     riid,
        LPUNKNOWN* ppUnk
    );

    // GetErrorString
    STDMETHODIMP GetErrorString( 
        HRESULT dwError,
        LCID    dwLocale,
        LPWSTR* ppString
    );

    // GetGroupByName
    STDMETHODIMP GetGroupByName(
        LPCWSTR    szName,
        REFIID     riid,
        LPUNKNOWN* ppUnk
    );

    // GetStatus
    STDMETHODIMP GetStatus( 
        OPCSERVERSTATUS** ppServerStatus
    );

    // RemoveGroup
    STDMETHODIMP RemoveGroup(
        OPCHANDLE hServerGroup,
        BOOL      bForce
    );

    // CreateGroupEnumerator
    STDMETHODIMP CreateGroupEnumerator(
        OPCENUMSCOPE dwScope, 
        REFIID       riid, 
        LPUNKNOWN*   ppUnk
    );

    //=========================================================================
    // IOPCBrowseServerAddressSpace
    
    // QueryOrganization
    STDMETHODIMP QueryOrganization(OPCNAMESPACETYPE* pNameSpaceType);
    
    // ChangeBrowsePosition
    STDMETHODIMP ChangeBrowsePosition(
        OPCBROWSEDIRECTION dwBrowseDirection,  
        LPCWSTR            szString
    );

    // BrowseOPCItemIDs
    STDMETHODIMP BrowseOPCItemIDs(
        OPCBROWSETYPE   dwBrowseFilterType,
        LPCWSTR         szFilterCriteria,  
        VARTYPE         vtDataTypeFilter,     
        DWORD           dwAccessRightsFilter,
        LPENUMSTRING*   ppIEnumString
    );

    // GetItemID
    STDMETHODIMP GetItemID(
        LPWSTR  wszItemName,
        LPWSTR* pszItemID
    );

    // BrowseAccessPaths
    STDMETHODIMP BrowseAccessPaths(
        LPCWSTR       szItemID,  
        LPENUMSTRING* ppIEnumString
    );

    //=========================================================================
    // IOPCItemProperties

    // QueryAvailableProperties
    STDMETHODIMP QueryAvailableProperties( 
        LPWSTR     szItemID,
        DWORD    * pdwCount,
        DWORD   ** ppPropertyIDs,
        LPWSTR  ** ppDescriptions,
        VARTYPE ** ppvtDataTypes
    );

    // GetItemProperties
    STDMETHODIMP GetItemProperties ( 
        LPWSTR     szItemID,
        DWORD      dwCount,
        DWORD    * pdwPropertyIDs,
        VARIANT ** ppvData,
        HRESULT ** ppErrors
    );

    // LookupItemIDs
    STDMETHODIMP LookupItemIDs( 
        LPWSTR     szItemID,
        DWORD      dwCount,
        DWORD    * pdwPropertyIDs,
        LPWSTR  ** ppszNewItemIDs,
        HRESULT ** ppErrors
    );

    //=========================================================================
    // IOPCBrowse

    // GetProperties
    STDMETHODIMP GetProperties( 
        DWORD                dwItemCount,
        LPWSTR*             pszItemIDs,
        BOOL                bReturnPropertyValues,
        DWORD                dwPropertyCount,
        DWORD*              pdwPropertyIDs,
        OPCITEMPROPERTIES** ppItemProperties 
    );

    // Browse
    STDMETHODIMP Browse(
        LPWSTR               szItemName,
        LPWSTR*               pszContinuationPoint,
        DWORD              dwMaxElementsReturned,
        OPCBROWSEFILTER    dwFilter,
        LPWSTR             szElementNameFilter,
        LPWSTR             szVendorFilter,
        BOOL               bReturnAllProperties,
        BOOL               bReturnPropertyValues,
        DWORD              dwPropertyCount,
        DWORD*             pdwPropertyIDs,
        BOOL*              pbMoreElements,
        DWORD*               pdwCount,
        OPCBROWSEELEMENT** ppBrowseElements
    );
    
    //=========================================================================
    // IOPCItemIO

    // Read
    STDMETHODIMP Read(
        DWORD       dwCount, 
        LPCWSTR   * pszItemIDs,
        DWORD     * pdwMaxAge,
        VARIANT  ** ppvValues,
        WORD     ** ppwQualities,
        FILETIME ** ppftTimeStamps,
        HRESULT  ** ppErrors
    );

    // WriteVQT
    STDMETHODIMP WriteVQT(
        DWORD         dwCount, 
        LPCWSTR    *  pszItemIDs,
        OPCITEMVQT *  pItemVQT,
        HRESULT    ** ppErrors
    );


    //========================================================================
    // Cache Updates

    // RegisterForUpdates
    static void RegisterForUpdates(COpcDaGroup* pGroup);    

    // UnregisterForUpdates
    static void UnregisterForUpdates(COpcDaGroup* pGroup);    

    // Working Thread	
    static void Loop(System::Threading::CancellationToken ct);	

	static LeveledLock^ GetDeviceSyncRoot();

private:
    // Start
    static bool Start();

    // Stop
    static void Stop();

    static gcroot<WorkingThread^> g_WorkingThread;

    static COpcList<COpcDaGroup*> g_cGroups;

    static gcroot<LeveledLock^> g_SyncRoot;


    //==========================================================================
    // Private Members

    COpcString      m_cBrowsePath;
    COpcDaGroupMap  m_cGroups;
    OPCSERVERSTATUS m_cStatus;
    UINT            m_uCounter;

    gcroot<LeveledLock^> _syncRoot;
};

OBJECT_ENTRY_AUTO(__uuidof(OpcDaServer), COpcDaServer)
