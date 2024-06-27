//============================================================================
// TITLE: COpcDaGroup.cpp
//
// CONTENTS:
// 
// A group object for an OPC Data Access server.
//
// (c) Copyright 2002-2003 The OPC Foundation
// ALL RIGHTS RESERVED.
//
// DISCLAIMER:
//  This code is provided by the OPC Foundation solely to assist in 
//  understanding and use of the appropriate OPC Specification(s) and may be 
//  used as set forth in the License Grant section of the OPC Specification.
//  This code is provided as-is and without warranty or support of any sort
//  and is subject to the Warranty and Liability Disclaimers which appear
//  in the printed OPC Specification.
//
// MODIFICATION LOG:
//
// Date       By    Notes
// ---------- ---   -----
// 2002/09/03 RSA   First release.
// 2002/11/16 RSA   Second release.
// 2003/05/05 RSA   Fixed return codes for GetItemDeadBands() and SetItemDeadBands().
// 2003/06/25 RSA   Ensured asynchronous write requests are processed in order.

#include "StdAfx.h"
#include "COpcDaGroup.h"
#include "COpcDaCache.h"
#include "COpcDaServer.h"
#include "COpcDaEnumItem.h"

#include <msclr\auto_handle.h>

using namespace msclr;

using namespace Ssz::Utils::Net4;

#define MAX_KEEP_ALIVE_RATE 1000

#define RESET_TICK_BASELINE             -1
#define RESET_TICK_BASELINE_NO_CALLBACK -2

//============================================================================
// COpcDaGroup

// Constructor
COpcDaGroup::COpcDaGroup(
    COpcDaServer&     cServer, 
    const COpcString& cName
)
:
    m_cServer(cServer),
    m_cName(cName)
{
     _syncRoot = gcnew LeveledLock(10100);

    RegisterInterface(IID_IOPCDataCallback);

    Init();
}

// Init
void COpcDaGroup::Init()
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    m_hServer      = (OPCHANDLE)(COpcDaGroup*)this;

    m_bActive      = FALSE;
    m_bEnabled     = TRUE;
    m_bDeleted     = FALSE;
    m_dwUpdateRate = 0;
    m_hClient      = NULL;
    m_lTimeBias    = 0;
    m_fltDeadband  = 0;
    m_dwLCID       = LOCALE_NEUTRAL;
    m_dwKeepAlive  = 0;
    m_ftLastUpdate = OpcUtcNow();
    m_uTickOffset  = RESET_TICK_BASELINE;
}

// Clear
void COpcDaGroup::Clear()
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    // release callback interface.
    UnregisterInterface(IID_IOPCDataCallback);    

    m_cItems.Clear();

    Init();
}

// Delete
bool COpcDaGroup::Delete()
{    
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    // release all resources.
    Clear();

    // flag as deleted.
    m_bDeleted = TRUE;        

    // unregister for updates.
    COpcDaServer::UnregisterForUpdates(this);

    // return false if references still exist.
    return (((IOPCItemMgt*)this)->Release() == 0);
}
   
// Initialize
HRESULT COpcDaGroup::Initialize(COpcDaGroup& cGroup)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    // copy state.
    m_bActive      = FALSE;
    m_bEnabled     = TRUE;
    m_dwUpdateRate = cGroup.m_dwUpdateRate;
    m_hClient      = cGroup.m_hClient;
    m_lTimeBias    = cGroup.m_lTimeBias;
    m_fltDeadband  = cGroup.m_fltDeadband;
    m_dwLCID       = cGroup.m_dwLCID;
    m_dwKeepAlive  = cGroup.m_dwKeepAlive;

    OPC_ASSERT(m_cItems.Count() == 0);

    // clone the items.
    for (auto it = cGroup.m_cItems.Begin(); it != cGroup.m_cItems.End(); it++)
    {        
        auto pItem = (*it)->Clone();
        
        OPCHANDLE hServer = m_cItems.Put(pItem);

        pItem->SetServerHandle(hServer);
    }

    return S_OK;
}

// Update
void COpcDaGroup::Update(LONGLONG uTick, UINT uInterval, FILETIME ftUtcNow)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    // check if group has been deleted.
    if (m_bDeleted) return;

    // inactive groups do not need updates.
    if (!m_bActive) return;

    // check if a callback object exists,
    bool bHasCallback = false;

    IOPCDataCallback* ipCallback = NULL;

    HRESULT hResult = GetCallback(IID_IOPCDataCallback, (IUnknown**)&ipCallback);

    if (SUCCEEDED(hResult))
    {
        bHasCallback = true;
        ipCallback->Release();
        ipCallback = NULL;
    }
    
    // reset the tick offset after group is activated. this ensures that the next 
    // cycle of the update thread will send an update instead of waiting until the 
    // update period has expired.

    BOOL bCallbackRequired = TRUE;

    if (m_uTickOffset < 0)
    {
        if (m_uTickOffset == RESET_TICK_BASELINE_NO_CALLBACK)
        {
            bCallbackRequired = FALSE;
        }

        m_uTickOffset = uTick;
    }
    
    LONGLONG uBaseline = uTick - m_uTickOffset;

    // update each item.
    DWORD dwCount = 0;
    std::vector<shared_ptr<COpcDaGroupItem>> cItems;

    for (auto it = m_cItems.Begin(); it != m_cItems.End(); it++)
    {
        if (uBaseline != 0 || bCallbackRequired)
        {
            DWORD dwSampleCount = (*it)->Update(
                uBaseline, 
                uInterval, 
                m_dwLCID, 
                m_dwUpdateRate, 
                m_fltDeadband);

            if (bHasCallback && dwSampleCount > 0)
            {
                cItems.push_back(*it);
                dwCount += dwSampleCount;
            }
        }
    }	

    // all finished if no items are ready for update.
    if (cItems.size() == 0)
    {
        // check if a keep alive should be sent.
        if (m_dwKeepAlive == 0)
        {
            return;
        }

        // check last update time.        

        ULONGLONG ullTicksNow  = 0;
        ULONGLONG ullTicksThen = 0;

        memcpy(&ullTicksNow,  &ftUtcNow,       sizeof(ULONGLONG));
        memcpy(&ullTicksThen, &m_ftLastUpdate, sizeof(ULONGLONG));

        if ((ullTicksNow - ullTicksThen) > m_dwKeepAlive*10000)
        {
            m_ftLastUpdate = ftUtcNow;

            // queue keep alive transaction.
            COpcDaTransaction* pTransaction = new COpcDaTransaction(
                OPC_TRANSACTION_UPDATE,
                (IOpcMessageCallback*)this,
                m_cName, 
                0
            );

            ::GetCache().QueueMessage(pTransaction);
        }
        
        return;
    }

    // save last update time.
    LONGLONG llThen = OpcToInt64(m_ftLastUpdate);
    
    m_ftLastUpdate = ftUtcNow;

    LONGLONG llNow  = OpcToInt64(m_ftLastUpdate);

    INT iDelta = (INT)((llNow - llThen)/10000 - (INT)m_dwUpdateRate);

    TCHAR tsBuffer[500];
    _stprintf_s(tsBuffer, _T("Expected: %d Actual: %d Delta: %d Ticks: %d\r\n"), m_dwUpdateRate, (INT)((llNow - llThen)/10000), iDelta, (INT)uTick);
    OutputDebugString(tsBuffer);

    // create samples to return to client.
    OPCITEMSTATE* pItems  = OpcArrayAlloc(OPCITEMSTATE, dwCount);
    HRESULT*      pErrors = OpcArrayAlloc(HRESULT, dwCount);

    memset(pItems, 0, dwCount*sizeof(OPCITEMSTATE));
    memset(pErrors, 0, dwCount*sizeof(HRESULT));

    DWORD dwIndex = 0;
    bool bOverflow = false;

    for (auto it = cItems.begin(); it != cItems.end(); it++)
    {
        if ((*it)->ReadBuffer(dwIndex, pItems, pErrors))
        {
            bOverflow = true;
        }
    }

    cItems.clear();
    
    // send update if group is enabled.
    if (SUCCEEDED(hResult) && m_bEnabled)
    {
        // queue transaction.
        COpcDaTransaction* pTransaction = new COpcDaTransaction(
            OPC_TRANSACTION_UPDATE,
            (IOpcMessageCallback*)this,
            m_cName, 
            0
        );
            
        pTransaction->dwCount = dwCount;
        pTransaction->pErrors = pErrors;

        pTransaction->SetItemStates(pItems);

        OpcFree(pItems);

        // set master error to indicate an overflow, if one occured.
        if (bOverflow)
        {
            pTransaction->hMasterError = OPC_S_DATAQUEUEOVERFLOW;
        }

        ::GetCache().QueueMessage(pTransaction);
    }

    // free memory instead of sending it to the client.
    else
    {
        for (DWORD ii = 0; ii  < dwCount; ii++)
        {
            OpcVariantClear(&(pItems[ii].vDataValue));
        }

        OpcFree(pItems);
        OpcFree(pErrors);
    }
}

//==============================================================================
// IOpcMessageCallback

// ProcessMessage
void COpcDaGroup::ProcessMessage(COpcMessage& cMsg)
{
    COpcDaTransaction& cTransaction = (COpcDaTransaction&)cMsg;
    
    // lookup callback - may have been released since transaction was posted.
    IOPCDataCallback* ipCallback = NULL;

    // check if transaction was cancelled.
    bool bCancelled = false;

    HRESULT hResult;

    {
        auto_handle<IDisposable> cLock = _syncRoot->Enter();

        hResult = GetCallback(IID_IOPCDataCallback, (IUnknown**)&ipCallback);
    
        if (FAILED(hResult))
        {
            return;
        }        
    
        if (cTransaction.dwClientID != 0)
        {
            if (!m_cTransactions.Lookup(cTransaction.GetID(), bCancelled))
            {
                bCancelled = true;
            }
            else
            {
                // write requests are not removed until a 'write complete' transaction arrives.
                if (cTransaction.GetType() != OPC_TRANSACTION_WRITE)
                {
                    m_cTransactions.RemoveKey(cTransaction.GetID());
                }
            }
        }
    }

    // send cancel complete update.
    if (bCancelled)
    {
        ipCallback->OnCancelComplete(cTransaction.dwClientID, m_hClient);
        ipCallback->Release();
        return;
    }
    
    switch (cTransaction.GetType())
    {
        // complete asynchronous read or refresh request.
        case OPC_TRANSACTION_READ:
        case OPC_TRANSACTION_REFRESH:
        {
            HRESULT* pErrors  = NULL;

            if (cTransaction.pMaxAges != NULL)
            {
                hResult = Read(
                    cTransaction.dwCount,
                    cTransaction.pServerHandles,
                    cTransaction.pMaxAges,
                    &cTransaction.pClientHandles,
                    &cTransaction.pValues,
                    &cTransaction.pQualities,
                    &cTransaction.pTimestamps,
                    &pErrors
                );
                
                COpcDaProperty::LocalizeVARIANTs(m_dwLCID, m_cServer.GetUserName(), cTransaction.dwCount, cTransaction.pValues);
            }

            else
            {
                OPCITEMSTATE* pResults = NULL;

                hResult = Read(
                    cTransaction.dwSource,
                    cTransaction.dwCount,
                    cTransaction.pServerHandles,
                    &pResults,
                    &pErrors
                );

                cTransaction.SetItemStates(pResults);
                OpcFree(pResults);
            }

            cTransaction.SetItemErrors(pErrors);
            OpcFree(pErrors);

            if (cTransaction.GetType() == OPC_TRANSACTION_READ)
            {
                ipCallback->OnReadComplete(
                    cTransaction.dwClientID,
                    m_hClient,
                    cTransaction.hMasterQuality,
                    cTransaction.hMasterError,
                    cTransaction.dwCount,
                    cTransaction.pClientHandles,
                    cTransaction.pValues,
                    cTransaction.pQualities,
                    cTransaction.pTimestamps,
                    cTransaction.pErrors
                );
            }
            else
            {
                ipCallback->OnDataChange(
                    cTransaction.dwClientID,
                    m_hClient,
                    cTransaction.hMasterQuality,
                    cTransaction.hMasterError,
                    cTransaction.dwCount,
                    cTransaction.pClientHandles,
                    cTransaction.pValues,
                    cTransaction.pQualities,
                    cTransaction.pTimestamps,
                    cTransaction.pErrors
                );                
            }

            break;
        }

        // complete data change update.
        case OPC_TRANSACTION_UPDATE:
        {
            if (cTransaction.dwCount != 0)
            {                
                COpcDaProperty::LocalizeVARIANTs(m_dwLCID, m_cServer.GetUserName(), cTransaction.dwCount, cTransaction.pValues);

                HRESULT hResult = ipCallback->OnDataChange(
                    cTransaction.dwClientID,
                    m_hClient,
                    cTransaction.hMasterQuality,
                    cTransaction.hMasterError,
                    cTransaction.dwCount,
                    cTransaction.pClientHandles,
                    cTransaction.pValues,
                    cTransaction.pQualities,
                    cTransaction.pTimestamps,
                    cTransaction.pErrors
                );
            }
            else
            {
                // create dummy variables to avoid passing null pointers.
                OPCHANDLE pClientHandles = NULL;
                VARIANT   pValues;       VariantInit(&pValues);
                WORD      pQualities     = 0;
                FILETIME  pTimestamps    = OpcMinDate();
                HRESULT   pErrors        = S_OK;

                HRESULT hResult = ipCallback->OnDataChange(
                    cTransaction.dwClientID,
                    m_hClient,
                    S_OK,
                    S_OK,
                    0,
                    &pClientHandles,
                    &pValues,
                    &pQualities,
                    &pTimestamps,
                    &pErrors
                );
            }

            // update the server status.
            m_cServer.SetLastUpdateTime();
            break;
        }

        // execute asynchronous write request.
        case OPC_TRANSACTION_WRITE:
        {
            HRESULT* pErrors = NULL;

            if (cTransaction.pValueVQTs != NULL)
            {
                hResult = WriteVQT(
                    cTransaction.dwCount,
                    cTransaction.pServerHandles,
                    cTransaction.pValueVQTs,
                    &cTransaction.pClientHandles,
                    &pErrors
                );
            }

            else
            {
                hResult = Write(
                    cTransaction.dwCount,
                    cTransaction.pServerHandles,
                    cTransaction.pValues,
                    &cTransaction.pClientHandles,
                    &pErrors
                );
            }
            
            cTransaction.SetItemErrors(pErrors);
            OpcFree(pErrors);

            break;
        }

        // complete asynchronous write request.
        case OPC_TRANSACTION_WRITE_COMPLETE:
        {
            ipCallback->OnWriteComplete(
                cTransaction.dwClientID,
                m_hClient,
                cTransaction.hMasterError,
                cTransaction.dwCount,
                cTransaction.pClientHandles,
                cTransaction.pErrors
            );

            break;
        }
    }

    // release reference to callback.
    ipCallback->Release();
    return;
}

//============================================================================
// IOPCGroupStateMgt

// GetState
HRESULT COpcDaGroup::GetState(
    DWORD     * pUpdateRate, 
    BOOL      * pActive, 
    LPWSTR    * ppName,
    LONG      * pTimeBias,
    FLOAT     * pPercentDeadband,
    DWORD     * pLCID,
    OPCHANDLE * phClientGroup,
    OPCHANDLE * phServerGroup
)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    if (m_bDeleted) return E_FAIL;

    *ppName = OpcStrDup((LPCWSTR)m_cName);

    *pActive          = m_bActive;
    *pUpdateRate      = m_dwUpdateRate;
    *phServerGroup    = m_hServer;
    *phClientGroup    = m_hClient;
    *pTimeBias        = m_lTimeBias;
    *pPercentDeadband = m_fltDeadband;
    *pLCID            = m_dwLCID;
    
    return S_OK;
}

// SetState
HRESULT COpcDaGroup::SetState( 
    DWORD     * pRequestedUpdateRate, 
    DWORD     * pRevisedUpdateRate, 
    BOOL      * pActive, 
    LONG      * pTimeBias,
    FLOAT     * pPercentDeadband,
    DWORD     * pLCID,
    OPCHANDLE * phClientGroup
)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    HRESULT hResult = S_OK;
    bool bRegister = false;
    bool bUnregister = false;       
    
    if (m_bDeleted) return E_FAIL;        

    // initialize return parameters.
    if (pRevisedUpdateRate != NULL) *pRevisedUpdateRate = 0;

    // validate deadband.
    if (pPercentDeadband != NULL) 
    {
        if (*pPercentDeadband < 0 || *pPercentDeadband > 100)
        {
            return E_INVALIDARG;
        }

        m_fltDeadband = *pPercentDeadband;
    }

    // set update rate.
    if (pRequestedUpdateRate != NULL) 
    {
        DWORD dwUpdateRate = *pRequestedUpdateRate;

        if (dwUpdateRate == 0 || dwUpdateRate%MAX_UPDATE_RATE != 0)
        {
            dwUpdateRate = MAX_UPDATE_RATE*(dwUpdateRate/MAX_UPDATE_RATE+1);

            // indicate that the requested rate will not be used.
            hResult = OPC_S_UNSUPPORTEDRATE;
        }

        // reset tick baseline.
        m_uTickOffset = RESET_TICK_BASELINE_NO_CALLBACK;

        *pRevisedUpdateRate = m_dwUpdateRate = dwUpdateRate; 
    }

    // set other parameters.
    if (pTimeBias != NULL)     m_lTimeBias   = *pTimeBias;
    if (pLCID != NULL)         m_dwLCID      = *pLCID;
    if (phClientGroup != NULL) m_hClient     = *phClientGroup;            

    if (pActive != NULL)
    {
        // changing to group to inactive state.
        if (m_bActive && !*pActive)
        {
            bUnregister = true;
        }

        // changing to group to active state.
        if (!m_bActive && *pActive)
        {    
            // force an update to be sent.
            m_uTickOffset = RESET_TICK_BASELINE;

            // clear the latest value to ensure that update get sent for unchanged items.
            for (auto it = m_cItems.Begin(); it != m_cItems.End(); it++)
            {
                (*it)->ResetLastUpdate();
            }

            // register for additional updates.
            bRegister = true;                
        }
        
        // update the state.
        m_bActive = *pActive;
    }    

    if (bRegister)
    {
        // register for additional updates.
        COpcDaServer::RegisterForUpdates(this);        
    }

    if (bUnregister)
    {
        COpcDaServer::UnregisterForUpdates(this);
    }

    return hResult;
}

// SetName
HRESULT COpcDaGroup::SetName( 
    LPCWSTR szName
)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    if (m_bDeleted) return E_FAIL;

    HRESULT hResult = m_cServer.SetGroupName(m_cName, szName);

    if (FAILED(hResult))
    {
        return hResult;
    }

    m_cName = szName;

    return S_OK;
}

// CloneGroup
HRESULT COpcDaGroup::CloneGroup(
    LPCWSTR     szName,
    REFIID      riid,
    LPUNKNOWN * ppUnk
)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    if (m_bDeleted) return E_FAIL;

    // validate arguments.
    if (ppUnk == NULL)
    {
        return E_INVALIDARG;
    }

    // clone group - locks server until clone operation is complete.
    COpcDaGroup* pGroup = NULL;

    HRESULT hResult = m_cServer.CloneGroup(m_cName, szName, &pGroup);

    if (FAILED(hResult))
    {
        return hResult;
    }

    // query for the requested interface.
    hResult = ((IOPCItemMgt*)pGroup)->QueryInterface(riid, (void**)ppUnk);

    // remove group on error.
    if (FAILED(hResult))
    {
        m_cServer.RemoveGroup(pGroup->m_hServer, FALSE);
    }

    // release local reference.
    ((IOPCItemMgt*)pGroup)->Release();

    return hResult;
}

// SetKeepAlive
HRESULT COpcDaGroup::SetKeepAlive( 
    DWORD   dwKeepAliveTime,
    DWORD * pdwRevisedKeepAliveTime 
)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    if (pdwRevisedKeepAliveTime == NULL)
    {
        return E_INVALIDARG;
    }

    // turn off keep alive updates.
    if (dwKeepAliveTime == 0)
    {
        m_dwKeepAlive = *pdwRevisedKeepAliveTime = 0;
        return S_OK;
    }

    // adjust keep alive time (multiple of 1 second).
    DWORD dwActualRate = dwKeepAliveTime;

    if (dwActualRate%MAX_KEEP_ALIVE_RATE != 0)
    {
        dwActualRate = MAX_KEEP_ALIVE_RATE*(dwActualRate/MAX_KEEP_ALIVE_RATE+1);
    }

    // set new update interval.
    m_dwKeepAlive = *pdwRevisedKeepAliveTime = dwActualRate;

    return (m_dwKeepAlive != dwKeepAliveTime)?OPC_S_UNSUPPORTEDRATE:S_OK;
}

// GetKeepAlive
HRESULT COpcDaGroup::GetKeepAlive( 
    DWORD * pdwKeepAliveTime 
)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    if (pdwKeepAliveTime == NULL)
    {
        return E_INVALIDARG;
    }

    *pdwKeepAliveTime = m_dwKeepAlive;

    return S_OK;
}

//============================================================================
// IOPCItemMgt

// IsValidReqType
static bool IsValidReqType(VARTYPE vtType)
{
    if (vtType & VT_ARRAY)
    {
        if (((vtType & VT_TYPEMASK) | VT_ARRAY) != vtType)
        {
            return false;
        }
    }

    switch (vtType & VT_TYPEMASK)
    {
        case VT_EMPTY:
        case VT_I1:
        case VT_UI1:
        case VT_I2:
        case VT_UI2:
        case VT_I4:
        case VT_UI4:
        case VT_I8:
        case VT_UI8:
        case VT_R4:
        case VT_R8:
        case VT_CY:
        case VT_BOOL:
        case VT_DATE:
        case VT_BSTR:
        case VT_VARIANT:
        {
            return true;
        }
    }

    return false;
}

// AddItems
HRESULT COpcDaGroup::AddItems( 
    DWORD            dwCount,
    OPCITEMDEF     * pItemArray,
    OPCITEMRESULT ** ppAddResults,
    HRESULT       ** ppErrors
)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();
    
    if (m_bDeleted) return E_FAIL;

    // check arguments.
    if (pItemArray == NULL || ppAddResults == NULL || ppErrors == NULL)
    {
        return E_INVALIDARG;
    }

    *ppAddResults = NULL;
    *ppErrors     = NULL;

    if (dwCount == 0)
    {
        return E_INVALIDARG;
    }

    bool bErrors = false;

    *ppAddResults = OpcArrayAlloc(OPCITEMRESULT, dwCount);
    *ppErrors     = OpcArrayAlloc(HRESULT, dwCount);

    memset(*ppAddResults, 0, sizeof(OPCITEMRESULT)*dwCount);    

    for (DWORD ii = 0; ii < dwCount; ii++)
    {
        // validate item id.
        if (pItemArray[ii].szItemID == NULL || wcslen(pItemArray[ii].szItemID) == 0)
        {
            (*ppErrors)[ii] = OPC_E_INVALIDITEMID;

            bErrors = true;
            continue;
        }

        ::GetCache().PrepareAddItem(nullptr, pItemArray[ii].szItemID);       

        // check if item exists.
        DWORD dwPropertyID = 0;
        uint hCacheItemHandle = ::GetCache().GetItemHandle(pItemArray[ii].szItemID, dwPropertyID);
        if (hCacheItemHandle == 0)
        {
            (*ppErrors)[ii] = OPC_E_UNKNOWNITEMID;

            bErrors = true;
            continue;
        }

        // validate data type.
        if (!IsValidReqType(pItemArray[ii].vtRequestedDataType))
        {
            (*ppErrors)[ii] = OPC_E_BADTYPE;
            
            bErrors = true;
            continue;
        }

        // add item to group.
        auto pItem = make_shared<COpcDaGroupItem>(hCacheItemHandle, dwPropertyID);

        OPCHANDLE hServer = m_cItems.Put(pItem);

        (*ppErrors)[ii] = pItem->Init(hServer, this->m_dwLCID, pItemArray[ii], (*ppAddResults)[ii]);

        if (FAILED((*ppErrors)[ii]))
        {
            m_cItems.Reset(hServer);
            bErrors = true;
            continue;
        }
    }

    ::GetCache().CommitAddItems(nullptr);
    
    return (bErrors)?S_FALSE:S_OK;
}

// ValidateItems
HRESULT COpcDaGroup::ValidateItems( 
    DWORD             dwCount,
    OPCITEMDEF      * pItemArray,
    BOOL              bBlobUpdate,
    OPCITEMRESULT  ** ppValidationResults,
    HRESULT        ** ppErrors
)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();
    
    if (m_bDeleted) return E_FAIL;

    // check arguments.
    if (pItemArray == NULL || ppValidationResults == NULL || ppErrors == NULL)
    {
        return E_INVALIDARG;
    }

    *ppValidationResults = NULL;
    *ppErrors            = NULL;

    if (dwCount == 0)
    {
        return E_INVALIDARG;
    }

    bool bErrors = false;

    *ppValidationResults = OpcArrayAlloc(OPCITEMRESULT, dwCount);
    *ppErrors            = OpcArrayAlloc(HRESULT, dwCount);

    memset(*ppValidationResults, 0, sizeof(OPCITEMRESULT)*dwCount);    

    for (DWORD ii = 0; ii < dwCount; ii++)
    {
        // validate item id.
        if (pItemArray[ii].szItemID == NULL || wcslen(pItemArray[ii].szItemID) == 0)
        {
            (*ppErrors)[ii] = OPC_E_INVALIDITEMID;

            bErrors = true;
            continue;
        }
        
        ::GetCache().PrepareAddItem(nullptr, pItemArray[ii].szItemID); // Add item to device if possible            

        // check if item exists.
        DWORD dwPropertyID = 0;
        uint hCacheItemHandle = ::GetCache().GetItemHandle(pItemArray[ii].szItemID, dwPropertyID);
        if (hCacheItemHandle == 0)
        {
            (*ppErrors)[ii] = OPC_E_UNKNOWNITEMID;

            bErrors = true;
            continue;
        }

        // validate data type.
        if (!IsValidReqType(pItemArray[ii].vtRequestedDataType))
        {
            (*ppErrors)[ii] = OPC_E_BADTYPE;
            
            bErrors = true;
            continue;
        }

        // add item to group.
        auto pItem = make_shared<COpcDaGroupItem>(hCacheItemHandle, dwPropertyID);        

        (*ppErrors)[ii] = pItem->Init(0, this->m_dwLCID, pItemArray[ii], (*ppValidationResults)[ii]);

        if (FAILED((*ppErrors)[ii]))
        {            
            bErrors = true;
            continue;
        }        
    }

    ::GetCache().CommitAddItems(nullptr);

    return (bErrors)?S_FALSE:S_OK;
}

// RemoveItems
HRESULT COpcDaGroup::RemoveItems( 
    DWORD        dwCount,
    OPCHANDLE  * phServer,
    HRESULT   ** ppErrors
)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    if (m_bDeleted) return E_FAIL;

    // check arguments.
    if (phServer == NULL || ppErrors == NULL)
    {
        return E_INVALIDARG;
    }

    *ppErrors = NULL;

    if (dwCount == 0)
    {
        return E_INVALIDARG;
    }

    bool bErrors = false;

    // remove items.
    *ppErrors = OpcArrayAlloc(HRESULT, dwCount);    

    for (DWORD ii = 0; ii < dwCount; ii++)
    {
        if (!m_cItems.Reset(phServer[ii]))
        {
            (*ppErrors)[ii] = OPC_E_INVALIDHANDLE;

            bErrors = true;
            continue;
        }

        (*ppErrors)[ii] = S_OK;
    }

    return (bErrors)?S_FALSE:S_OK;
}

// SetActiveState
HRESULT COpcDaGroup::SetActiveState(
    DWORD        dwCount,
    OPCHANDLE  * phServer,
    BOOL         bActive, 
    HRESULT   ** ppErrors
)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();
    
    if (m_bDeleted) return E_FAIL;

    // check arguments.
    if (phServer == NULL || ppErrors == NULL)
    {
        return E_INVALIDARG;
    }
    
    *ppErrors = NULL;

    if (dwCount == 0)
    {
        return E_INVALIDARG;
    }

    bool bErrors = false;

    // update items.
    *ppErrors = OpcArrayAlloc(HRESULT, dwCount);

    for (DWORD ii = 0; ii < dwCount; ii++)
    {
        auto pItem = m_cItems.Get(phServer[ii]);

        if (!pItem)
        {
            (*ppErrors)[ii] = OPC_E_INVALIDHANDLE;

            bErrors = true;
            continue;
        }

        pItem->SetActive(bActive);

        if (!bActive)
        {
            pItem->ResetLastUpdate();
        }

        (*ppErrors)[ii] = S_OK;
    }

    return (bErrors)?S_FALSE:S_OK;
}

// SetClientHandles
HRESULT COpcDaGroup::SetClientHandles(
    DWORD        dwCount,
    OPCHANDLE  * phServer,
    OPCHANDLE  * phClient,
    HRESULT   ** ppErrors
)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    if (m_bDeleted) return E_FAIL;

    // check arguments.
    if (phServer == NULL || phClient == NULL || ppErrors == NULL)
    {
        return E_INVALIDARG;
    }

    *ppErrors = NULL;

    if (dwCount == 0)
    {
        return E_INVALIDARG;
    }

    bool bErrors = false;

    // update items.
    *ppErrors = OpcArrayAlloc(HRESULT, dwCount);

    for (DWORD ii = 0; ii < dwCount; ii++)
    {
        auto pItem = m_cItems.Get(phServer[ii]);

        if (!pItem)
        {
            (*ppErrors)[ii] = OPC_E_INVALIDHANDLE;

            bErrors = true;
            continue;
        }

        pItem->SetClientHandle(phClient[ii]);

        (*ppErrors)[ii] = S_OK;
    }

    return (bErrors)?S_FALSE:S_OK;
}

// SetDatatypes
HRESULT COpcDaGroup::SetDatatypes(
    DWORD        dwCount,
    OPCHANDLE  * phServer,
    VARTYPE    * pRequestedDatatypes,
    HRESULT   ** ppErrors
)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    if (m_bDeleted) return E_FAIL;

    // check arguments.
    if (phServer == NULL || pRequestedDatatypes == NULL || ppErrors == NULL)
    {
        return E_INVALIDARG;
    }

    *ppErrors = NULL;

    if (dwCount == 0)
    {
        return E_INVALIDARG;
    }

    bool bErrors = false;

    // update items.
    *ppErrors = OpcArrayAlloc(HRESULT, dwCount);

    for (DWORD ii = 0; ii < dwCount; ii++)
    {
        auto pItem = m_cItems.Get(phServer[ii]);

        if (!pItem)
        {
            (*ppErrors)[ii] = OPC_E_INVALIDHANDLE;

            bErrors = true;
            continue;
        }

        // validate data type.
        if (!IsValidReqType(pRequestedDatatypes[ii]))
        {
            (*ppErrors)[ii] = OPC_E_BADTYPE;
            
            bErrors = true;
            continue;
        }

        pItem->SetReqType(pRequestedDatatypes[ii]);

        (*ppErrors)[ii] = S_OK;
    }

    return (bErrors)?S_FALSE:S_OK;
}

// CreateEnumerator
HRESULT COpcDaGroup::CreateEnumerator(
    REFIID      riid,
    LPUNKNOWN * ppUnk
)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    if (m_bDeleted) return E_FAIL;

    // check arguments.
    if (ppUnk == NULL)
    {
        return E_INVALIDARG;
    }

    UINT uCount = 0;
    OPCITEMATTRIBUTES* pItems = NULL;

    // get item attribute array.
    if (m_cItems.Count() > 0)
    {
        pItems = OpcArrayAlloc(OPCITEMATTRIBUTES, m_cItems.Count());

        for (auto it = m_cItems.Begin(); it != m_cItems.End(); it++)
        {
            (*it)->GetItemAttributes(pItems[uCount++]);
        }
    }

    // create enumerator.
    COpcDaEnumItem* pEnum = new COpcDaEnumItem(uCount, pItems);

    // query for interface.
    HRESULT hResult = pEnum->QueryInterface(riid, (void**)ppUnk);

    // check for a valid interface id.
    if (FAILED(hResult))
    {
        hResult = E_INVALIDARG;
    }
    
    // release local reference.
    pEnum->Release();

    return hResult;
}

//============================================================================
// IOPCSyncIO2

// Read
HRESULT COpcDaGroup::Read(
    OPCDATASOURCE   dwSource,
    DWORD           dwCount, 
    OPCHANDLE     * phServer, 
    OPCITEMSTATE ** ppItemValues,
    HRESULT      ** ppErrors
)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    if (m_bDeleted) return E_FAIL;

    // check arguments.
    if (phServer == NULL || ppItemValues == NULL || ppErrors == NULL)
    {
        return E_INVALIDARG;
    }

    // check datasource arguments
    if (dwSource != OPC_DS_CACHE && dwSource != OPC_DS_DEVICE)
    {
        return E_INVALIDARG;
    }

    *ppErrors = NULL;
    
    if (dwCount == 0)
    {
        return E_INVALIDARG;
    }

    // read items.
    *ppItemValues = OpcArrayAlloc(OPCITEMSTATE, dwCount);
    *ppErrors     = OpcArrayAlloc(HRESULT, dwCount);

    memset(*ppItemValues, 0, dwCount*sizeof(OPCITEMSTATE));
    memset(*ppErrors, 0, dwCount*sizeof(HRESULT));

    bool bErrors = false;

    for (DWORD ii = 0; ii < dwCount; ii++)
    {
        // lookup item by server handle.
        auto pItem = m_cItems.Get(phServer[ii]);

        if (!pItem)
        {
            (*ppErrors)[ii] = OPC_E_INVALIDHANDLE;

            bErrors = true;
            continue;
        }

        // read the current item state.
        (*ppErrors)[ii] = pItem->Read(dwSource, m_dwLCID, (*ppItemValues)[ii]);

        if (FAILED((*ppErrors)[ii]))
        {
            bErrors = true;
            continue;
        }

        // override quality if group is not active.
        if (!m_bActive && dwSource == OPC_DS_CACHE)
        {
            (*ppItemValues)[ii].wQuality = OPC_QUALITY_OUT_OF_SERVICE;
        }
    }
    
    COpcDaProperty::LocalizeOPCITEMSTATE(m_dwLCID, m_cServer.GetUserName(), dwCount, *ppItemValues);
    return (bErrors)?S_FALSE:S_OK;
}

// Write
HRESULT COpcDaGroup::Write(
    DWORD        dwCount, 
    OPCHANDLE  * phServer, 
    VARIANT    * pItemValues, 
    HRESULT   ** ppErrors
)
{
    OPCHANDLE* phClient = NULL;

    HRESULT hResult = Write(dwCount, phServer, pItemValues, &phClient, ppErrors);

    OpcFree(phClient);

    return hResult;
}

// Write
HRESULT COpcDaGroup::Write(
    DWORD        dwCount, 
    OPCHANDLE  * phServer, 
    VARIANT    * pItemValues, 
    OPCHANDLE ** phClient,
    HRESULT   ** ppErrors
)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    if (m_bDeleted) return E_FAIL;

    // check arguments;
    if (phServer == NULL || pItemValues == NULL || phClient == NULL || ppErrors == NULL)
    {
        return E_INVALIDARG;
    }

    *ppErrors = NULL;

    if (dwCount == 0)
    {
        return E_INVALIDARG;
    }

    // write items.
    *phClient = OpcArrayAlloc(OPCHANDLE, dwCount);
    *ppErrors = OpcArrayAlloc(HRESULT,   dwCount);

    memset(*ppErrors, 0, dwCount*sizeof(HRESULT));

    bool bErrors = false;

    for (DWORD ii = 0; ii < dwCount; ii++)
    {
        // lookup item by server handle.
        auto pItem = m_cItems.Get(phServer[ii]);

        if (!pItem)
        {
            (*ppErrors)[ii] = OPC_E_INVALIDHANDLE;

            bErrors = true;
            continue;
        }

        (*phClient)[ii] = pItem->GetClientHandle();

        // write the new item value.
        (*ppErrors)[ii] = pItem->Write(m_dwLCID, pItemValues[ii]);

        if (FAILED((*ppErrors)[ii]))
        {
            bErrors = true;
            continue;
        }
    }

    return (bErrors)?S_FALSE:S_OK;
}

// ReadMaxAge
HRESULT COpcDaGroup::ReadMaxAge(
    DWORD       dwCount, 
    OPCHANDLE * phServer, 
    DWORD     * pdwMaxAge,
    VARIANT  ** ppvValues,
    WORD     ** ppwQualities,
    FILETIME ** ppftTimeStamps,
    HRESULT  ** ppErrors
)
{
    HRESULT hResult = Read(
        dwCount, 
        phServer, 
        pdwMaxAge, 
        NULL, 
        ppvValues, 
        ppwQualities, 
        ppftTimeStamps, 
        ppErrors
    );

    if (FAILED(hResult))
    {
        return hResult;
    }

    return hResult;
}

// Read
HRESULT COpcDaGroup::Read(
    DWORD        dwCount, 
    OPCHANDLE *  phServer, 
    DWORD     *  pdwMaxAge,
    OPCHANDLE ** pphClient, 
    VARIANT   ** ppvValues,
    WORD      ** ppwQualities,
    FILETIME  ** ppftTimeStamps,
    HRESULT   ** ppErrors
)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    if (m_bDeleted) return E_FAIL;

    // check arguments;
    if (
          phServer       == NULL || 
          pdwMaxAge      == NULL || 
          ppvValues      == NULL || 
          ppwQualities   == NULL || 
          ppftTimeStamps == NULL ||
          ppErrors       == NULL
       )
    {
        return E_INVALIDARG;
    }

    *ppErrors = NULL;
    
    if (dwCount == 0)
    {
        return E_INVALIDARG;
    }

    // read items.
    *ppvValues      = OpcArrayAlloc(VARIANT, dwCount);
    *ppwQualities   = OpcArrayAlloc(WORD, dwCount);
    *ppftTimeStamps = OpcArrayAlloc(FILETIME, dwCount);
    *ppErrors       = OpcArrayAlloc(HRESULT, dwCount);

    memset(*ppvValues,      0, dwCount*sizeof(VARIANT));
    memset(*ppwQualities,   0, dwCount*sizeof(WORD));
    memset(*ppftTimeStamps, 0, dwCount*sizeof(FILETIME));
    memset(*ppErrors,       0, dwCount*sizeof(HRESULT));

    if (pphClient != NULL)
    {
        *pphClient = OpcArrayAlloc(OPCHANDLE, dwCount);
        memset(*pphClient, 0, dwCount*sizeof(OPCHANDLE));       
    }

    bool bErrors = false;

    for (DWORD ii = 0; ii < dwCount; ii++)
    {
        // lookup item by server handle.
        auto pItem = m_cItems.Get(phServer[ii]);

        if (!pItem)
        {
            (*ppErrors)[ii] = OPC_E_INVALIDHANDLE;

            bErrors = true;
            continue;
        }

        // lookup client handle.
        if (pphClient != NULL)
        {
            (*pphClient)[ii] = pItem->GetClientHandle();
        }

        // read the current item
        (*ppErrors)[ii] = pItem->Read(
            pdwMaxAge[ii], 
            m_dwLCID, 
            (*ppvValues)[ii],
            (*ppftTimeStamps)[ii],
            (*ppwQualities)[ii]
        );

        if (FAILED((*ppErrors)[ii]))
        {
            bErrors = true;
            continue;
        }
    }

    COpcDaProperty::LocalizeVARIANTs(m_dwLCID, m_cServer.GetUserName(), dwCount, *ppvValues);
    return (bErrors)?S_FALSE:S_OK;
}

// WriteVQT
HRESULT COpcDaGroup::WriteVQT(
    DWORD         dwCount, 
    OPCHANDLE  *  phServer, 
    OPCITEMVQT *  pItemVQT,
    HRESULT    ** ppErrors
)
{
    return WriteVQT(dwCount, phServer, pItemVQT, NULL, ppErrors);
}

// WriteVQT
HRESULT COpcDaGroup::WriteVQT(
    DWORD         dwCount, 
    OPCHANDLE  *  phServer, 
    OPCITEMVQT *  pItemVQT,
    OPCHANDLE  ** ppClientHandles,
    HRESULT    ** ppErrors
)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    if (m_bDeleted) return E_FAIL;

    // check arguments;
    if (phServer == NULL || pItemVQT == NULL || ppErrors == NULL)
    {
        return E_INVALIDARG;
    }

    *ppErrors = NULL;

    if (dwCount == 0)
    {
        return E_INVALIDARG;
    }

    // write items.
    *ppErrors = OpcArrayAlloc(HRESULT, dwCount);
    memset(*ppErrors, 0, dwCount*sizeof(HRESULT));

    if (ppClientHandles != NULL)
    {
        *ppClientHandles = OpcArrayAlloc(OPCHANDLE, dwCount);
        memset(*ppClientHandles, 0, dwCount*sizeof(OPCHANDLE));
    }

    bool bErrors = false;

    for (DWORD ii = 0; ii < dwCount; ii++)
    {
        // lookup item by server handle.
        auto pItem = m_cItems.Get(phServer[ii]);

        if (!pItem)
        {
            (*ppErrors)[ii] = OPC_E_INVALIDHANDLE;

            bErrors = true;
            continue;
        }

        // lookup client handle.
        if (ppClientHandles != NULL)
        {
            (*ppClientHandles)[ii] = pItem->GetClientHandle();
        }

        // write the new item value.
        (*ppErrors)[ii] = pItem->Write(
            m_dwLCID, 
            pItemVQT[ii].vDataValue,
            (pItemVQT[ii].bTimeStampSpecified)?&(pItemVQT[ii].ftTimeStamp):NULL,
            (pItemVQT[ii].bQualitySpecified)?&(pItemVQT[ii].wQuality):NULL
        );

        if (FAILED((*ppErrors)[ii]))
        {
            bErrors = true;
            continue;
        }
    }

    return (bErrors)?S_FALSE:S_OK;
}

//============================================================================
// IOPCAsyncIO2

// Read
HRESULT COpcDaGroup::Read(
    DWORD           dwCount,
    OPCHANDLE     * phServer,
    DWORD           dwTransactionID,
    DWORD         * pdwCancelID,
    HRESULT      ** ppErrors
)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    if (m_bDeleted) return E_FAIL;

    // check arguments;
    if (
           phServer        == NULL || 
           pdwCancelID     == NULL || 
           ppErrors        == NULL
       )
    {
        return E_INVALIDARG;
    }
    
    *pdwCancelID = NULL;
    *ppErrors    = NULL;

    if (dwCount == 0)
    {
        return E_INVALIDARG;
    }

    // check for an active connection.
    if (!IsConnected(IID_IOPCDataCallback))
    {
        return CONNECT_E_NOCONNECTION;
    }

    // initialize return values.
    *ppErrors = OpcArrayAlloc(HRESULT, dwCount);

    memset(*ppErrors, 0, dwCount*sizeof(HRESULT));

    bool bError = false;

    COpcArray<OPCHANDLE> cItems;

    for (DWORD ii = 0; ii < dwCount; ii++)
    {
        // lookup item by server handle.
        auto pItem = m_cItems.Get(phServer[ii]);

        if (!pItem)
        {
            (*ppErrors)[ii] = OPC_E_INVALIDHANDLE;

            bError = true;
            continue;
        }

        cItems.Append(phServer[ii]);
    }

    // do not queue a transaction if no valid items.
    if (cItems.GetSize() > 0)
    {
        COpcDaTransaction* pTransaction = new COpcDaTransaction(
            OPC_TRANSACTION_READ,
            (IOpcMessageCallback*)this,
            m_cName, 
            dwTransactionID
        );
            
        pTransaction->dwSource = OPC_DS_DEVICE;
        pTransaction->dwCount  = cItems.GetSize();

        pTransaction->pServerHandles = OpcArrayAlloc(OPCHANDLE, pTransaction->dwCount);

        for (DWORD ii = 0; ii < pTransaction->dwCount; ii++)
        {
            pTransaction->pServerHandles[ii] = cItems[ii];
        }

        *pdwCancelID = pTransaction->GetID();
        m_cTransactions[*pdwCancelID] = false;
        ::GetCache().QueueMessage(pTransaction);
    }
    
    return (bError)?S_FALSE:S_OK;
}

// Write
HRESULT COpcDaGroup::Write(
    DWORD           dwCount, 
    OPCHANDLE     * phServer,
    VARIANT       * pItemValues, 
    DWORD           dwTransactionID,
    DWORD         * pdwCancelID,
    HRESULT      ** ppErrors
)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();
    
    if (m_bDeleted) return E_FAIL;

    // check arguments;
    if (
           phServer        == NULL || 
           pItemValues     == NULL || 
           pdwCancelID     == NULL || 
           ppErrors        == NULL
       )
    {
        return E_INVALIDARG;
    }
   
    // initialize return values.
    *pdwCancelID = NULL;
    *ppErrors    = NULL;
   
    if (dwCount == 0)
    {
        return E_INVALIDARG;
    }

    // check for an active connection.
    if (!IsConnected(IID_IOPCDataCallback))
    {
        return CONNECT_E_NOCONNECTION;
    }

    *ppErrors = OpcArrayAlloc(HRESULT, dwCount);
   
    memset(*ppErrors, 0, dwCount*sizeof(HRESULT));

    COpcArray<OPCHANDLE> cItems;
    COpcArray<VARIANT>   cValues;

    bool bError = false;

    for (DWORD ii = 0; ii < dwCount; ii++)
    {
        // lookup item by server handle.
        auto pItem = m_cItems.Get(phServer[ii]);

        if (!pItem)
        {
            (*ppErrors)[ii] = OPC_E_INVALIDHANDLE;

            bError = true;
            continue;
        }

        // check if the variant is valid.
        VARIANT cValue; OpcVariantInit(&cValue);

        OpcVariantCopy(&cValue, &pItemValues[ii]);

        cItems.Append(phServer[ii]);

        // appending a VARIANT to the array passes ownership of the memory.
        cValues.Append(cValue);
    }

    // queue a transaction if one or more valid items.
    if (cItems.GetSize())
    {
        COpcDaTransaction* pTransaction = new COpcDaTransaction(
            OPC_TRANSACTION_WRITE,
            (IOpcMessageCallback*)this,
            m_cName, 
            dwTransactionID
        );
            
        pTransaction->dwCount = cItems.GetSize();

        pTransaction->pServerHandles = OpcArrayAlloc(OPCHANDLE, pTransaction->dwCount);
        pTransaction->pValues        = OpcArrayAlloc(VARIANT,   pTransaction->dwCount);

        UINT uIndex = 0;

        for (DWORD ii = 0; ii < pTransaction->dwCount; ii++)
        {
            pTransaction->pServerHandles[ii] = cItems[ii];

            // simple assignment to a VARIANT array passes ownership of the memory.
            pTransaction->pValues[ii] = cValues[ii];
        }

        *pdwCancelID = pTransaction->GetID();
        m_cTransactions[*pdwCancelID] = false;
        ::GetCache().QueueMessage(pTransaction);
    }
    
    return (bError)?S_FALSE:S_OK;
}

// Refresh2
HRESULT COpcDaGroup::Refresh2(
    OPCDATASOURCE   dwSource,
    DWORD           dwTransactionID,
    DWORD         * pdwCancelID
)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    if (m_bDeleted) return E_FAIL;

    // check arguments;
    if (pdwCancelID == NULL)
    {
        return E_INVALIDARG;
    }    
    
    // check datasource argument.
    if (dwSource != OPC_DS_CACHE && dwSource != OPC_DS_DEVICE)
    {
        return E_INVALIDARG;
    }

    // initialize return values.
    *pdwCancelID = NULL;
   
    // check for an active connection.
    if (!IsConnected(IID_IOPCDataCallback))
    {
        return CONNECT_E_NOCONNECTION;
    }

    // return error is group is not active.
    if (!m_bActive)
    {
        return E_FAIL;
    }

    // determine list available active items.
    COpcArray<OPCHANDLE> cItems;

    for (auto it = m_cItems.Begin(); it != m_cItems.End(); it++)
    {
        if ((*it)->GetActive())
        {
            cItems.Append((*it)->GetServerHandle());
        }
    }

    // no active items available.
    if (cItems.GetSize() == 0)
    {
        return E_FAIL;
    }

    // create transaction.
    DWORD dwCount = cItems.GetSize();
    OPCHANDLE* phItems = OpcArrayAlloc(OPCHANDLE, dwCount);

    for (DWORD ii = 0; ii < dwCount; ii++)
    {
        phItems[ii] = cItems[ii];
    }

    COpcDaTransaction* pTransaction = new COpcDaTransaction(
        OPC_TRANSACTION_REFRESH,
        (IOpcMessageCallback*)this,
        m_cName, 
        dwTransactionID
    );

    pTransaction->dwSource       = dwSource;
    pTransaction->dwCount        = dwCount;
    pTransaction->pServerHandles = phItems;

    // queue transaction return id to client.
    *pdwCancelID = pTransaction->GetID();
    m_cTransactions[*pdwCancelID] = false;
    ::GetCache().QueueMessage(pTransaction);

    return S_OK;
}

// Cancel2
HRESULT COpcDaGroup::Cancel2(DWORD dwCancelID)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    if (m_bDeleted) return E_FAIL;
   
    // check for an active connection.
    if (!IsConnected(IID_IOPCDataCallback))
    {
        return CONNECT_E_NOCONNECTION;
    }

    // lookup the transaction.
    if (!m_cTransactions.Lookup(dwCancelID))
    {
        return E_FAIL;
    }

    m_cTransactions[dwCancelID] = true;
    return S_OK;
}

// SetEnable
HRESULT COpcDaGroup::SetEnable(BOOL bEnable)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    if (m_bDeleted) return E_FAIL;

    // check for an active connection.
    if (!IsConnected(IID_IOPCDataCallback))
    {
        return CONNECT_E_NOCONNECTION;
    }

    m_bEnabled = bEnable;

    return S_OK;
}

// GetEnable
HRESULT COpcDaGroup::GetEnable(BOOL* pbEnable)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    if (m_bDeleted) return E_FAIL;

    // check for valid arguments.
    if (pbEnable == NULL)
    {
        return E_INVALIDARG;
    }

    // check for an active connection.
    if (!IsConnected(IID_IOPCDataCallback))
    {
        return CONNECT_E_NOCONNECTION;
    }


    *pbEnable = m_bEnabled;

    return S_OK;
}

//============================================================================
// IOPCAsyncIO3

HRESULT COpcDaGroup::ReadMaxAge(
    DWORD       dwCount, 
    OPCHANDLE * phServer,
    DWORD     * pdwMaxAge,
    DWORD       dwTransactionID,
    DWORD     * pdwCancelID,
    HRESULT  ** ppErrors
)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    if (m_bDeleted) return E_FAIL;

    // check arguments;
    if (
           phServer        == NULL || 
           pdwMaxAge       == NULL ||
           pdwCancelID     == NULL || 
           ppErrors        == NULL
       )
    {
        return E_INVALIDARG;
    }
    
    *pdwCancelID = NULL;
    *ppErrors    = NULL;

    if (dwCount == 0)
    {
        return E_INVALIDARG;
    }

    // check for an active connection.
    if (!IsConnected(IID_IOPCDataCallback))
    {
        return CONNECT_E_NOCONNECTION;
    }

    // initialize return values.
    *ppErrors = OpcArrayAlloc(HRESULT, dwCount);

    memset(*ppErrors, 0, dwCount*sizeof(HRESULT));

    bool bError = false;

    COpcArray<OPCHANDLE> cItems;

    for (DWORD ii = 0; ii < dwCount; ii++)
    {
        // lookup item by server handle.
        auto pItem = m_cItems.Get(phServer[ii]);

        if (!pItem)
        {
            (*ppErrors)[ii] = OPC_E_INVALIDHANDLE;

            bError = true;
            continue;
        }

        cItems.Append(phServer[ii]);
    }

    // do not queue a transaction if no valid items.
    if (cItems.GetSize() > 0)
    {
        COpcDaTransaction* pTransaction = new COpcDaTransaction(
            OPC_TRANSACTION_READ,
            (IOpcMessageCallback*)this,
            m_cName, 
            dwTransactionID
        );
            
        pTransaction->dwSource       = OPC_DS_DEVICE;
        pTransaction->dwCount        = cItems.GetSize();

        pTransaction->pServerHandles = OpcArrayAlloc(OPCHANDLE, pTransaction->dwCount);
        pTransaction->pMaxAges       = OpcArrayAlloc(DWORD, pTransaction->dwCount);

        for (DWORD ii = 0; ii < pTransaction->dwCount; ii++)
        {
            pTransaction->pServerHandles[ii] = cItems[ii];
            pTransaction->pMaxAges[ii]       = pdwMaxAge[ii];
        }

        *pdwCancelID = pTransaction->GetID();
        m_cTransactions[*pdwCancelID] = false;
        ::GetCache().QueueMessage(pTransaction);
    }
    
    return (bError)?S_FALSE:S_OK;
}

HRESULT COpcDaGroup::WriteVQT(
    DWORD        dwCount, 
    OPCHANDLE  * phServer,
    OPCITEMVQT * pItemVQT,
    DWORD        dwTransactionID,
    DWORD      * pdwCancelID,
    HRESULT   ** ppErrors
)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();
    
    if (m_bDeleted) return E_FAIL;

    // check arguments;
    if (
           phServer        == NULL || 
           pItemVQT        == NULL || 
           pdwCancelID     == NULL || 
           ppErrors        == NULL
       )
    {
        return E_INVALIDARG;
    }
   
    // initialize return values.
    *pdwCancelID = NULL;
    *ppErrors    = NULL;
   
    if (dwCount == 0)
    {
        return E_INVALIDARG;
    }

    // check for an active connection.
    if (!IsConnected(IID_IOPCDataCallback))
    {
        return CONNECT_E_NOCONNECTION;
    }

    *ppErrors = OpcArrayAlloc(HRESULT, dwCount);
   
    memset(*ppErrors, 0, dwCount*sizeof(HRESULT));

    COpcArray<OPCHANDLE>  cItems;
    COpcArray<OPCITEMVQT> cValues;

    bool bError = false;

    for (DWORD ii = 0; ii < dwCount; ii++)
    {
        // lookup item by server handle.
        auto pItem = m_cItems.Get(phServer[ii]);

        if (!pItem)
        {
            (*ppErrors)[ii] = OPC_E_INVALIDHANDLE;

            bError = true;
            continue;
        }

        // copy the new values.
        OPCITEMVQT cValue;

        memset(&cValue, 0, sizeof(OPCITEMVQT));
        memcpy(&cValue, &(pItemVQT[ii]), sizeof(OPCITEMVQT));
        memset(&cValue.vDataValue, 0, sizeof(VARIANT));

        OpcVariantCopy(&cValue.vDataValue, &pItemVQT[ii].vDataValue);

        cItems.Append(phServer[ii]);

        // appending a VARIANT to the array passes ownership of the memory.
        cValues.Append(cValue);
    }

    // queue a transaction if one or more valid items.
    if (cItems.GetSize())
    {
        COpcDaTransaction* pTransaction = new COpcDaTransaction(
            OPC_TRANSACTION_WRITE,
            (IOpcMessageCallback*)this,
            m_cName, 
            dwTransactionID
        );
        
        pTransaction->dwCount = cItems.GetSize();

        pTransaction->pServerHandles = OpcArrayAlloc(OPCHANDLE,  pTransaction->dwCount);
        pTransaction->pValueVQTs     = OpcArrayAlloc(OPCITEMVQT, pTransaction->dwCount);

        UINT uIndex = 0;

        for (DWORD ii = 0; ii < pTransaction->dwCount; ii++)
        {
            pTransaction->pServerHandles[ii] = cItems[ii];

            // simple assignment to a VARIANT array passes ownership of the memory.
            pTransaction->pValueVQTs[ii] = cValues[ii];
        }

        *pdwCancelID = pTransaction->GetID();
        m_cTransactions[*pdwCancelID] = false;
        ::GetCache().QueueMessage(pTransaction);
    }
    
    return (bError)?S_FALSE:S_OK;
}

HRESULT COpcDaGroup::RefreshMaxAge(
    DWORD   dwMaxAge,
    DWORD   dwTransactionID,
    DWORD * pdwCancelID
)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    if (m_bDeleted) return E_FAIL;

    // check arguments;
    if (pdwCancelID == NULL)
    {
        return E_INVALIDARG;
    }

    // initialize return values.
    *pdwCancelID = NULL;
   
    // check for an active connection.
    if (!IsConnected(IID_IOPCDataCallback))
    {
        return CONNECT_E_NOCONNECTION;
    }

    // return error is group is not active.
    if (!m_bActive)
    {
        return E_FAIL;
    }

    // determine list available active items.
    COpcArray<OPCHANDLE> cItems;

    for (auto it = m_cItems.Begin(); it != m_cItems.End(); it++)
    {        
        if ((*it)->GetActive())
        {
            cItems.Append((*it)->GetServerHandle());
        }
    }

    // no active items available.
    if (cItems.GetSize() == 0)
    {
        return E_FAIL;
    }

    // create transaction.
    DWORD dwCount = cItems.GetSize();
    OPCHANDLE* phItems = OpcArrayAlloc(OPCHANDLE, dwCount);
    DWORD* pdwMaxAges  = OpcArrayAlloc(DWORD, dwCount);

    for (DWORD ii = 0; ii < dwCount; ii++)
    {
        phItems[ii]    = cItems[ii];
        pdwMaxAges[ii] = dwMaxAge;
    }

    COpcDaTransaction* pTransaction = new COpcDaTransaction(
        OPC_TRANSACTION_REFRESH,
        (IOpcMessageCallback*)this,
        m_cName, 
        dwTransactionID
    );

    pTransaction->dwSource       = OPC_DS_DEVICE;
    pTransaction->dwCount        = dwCount;
    pTransaction->pServerHandles = phItems;
    pTransaction->pMaxAges       = pdwMaxAges;

    // queue transaction return id to client.
    *pdwCancelID = pTransaction->GetID();
    m_cTransactions[*pdwCancelID] = false;
    ::GetCache().QueueMessage(pTransaction);

    return S_OK;
}

//============================================================================
// IOPCItemDeadbandMgt

// SetItemDeadband
HRESULT  COpcDaGroup::SetItemDeadband( 
    DWORD         dwCount,
    OPCHANDLE * phServer,
    FLOAT     * pPercentDeadband,
    HRESULT  ** ppErrors
)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    if (m_bDeleted) return E_FAIL;

    // check arguments.
    if (phServer == NULL || pPercentDeadband == NULL || ppErrors == NULL)
    {
        return E_INVALIDARG;
    }

    *ppErrors = NULL;

    if (dwCount == 0)
    {
        return E_INVALIDARG;
    }

    bool bErrors = false;

    // update items.
    *ppErrors = OpcArrayAlloc(HRESULT, dwCount);

    for (DWORD ii = 0; ii < dwCount; ii++)
    {
        auto pItem = m_cItems.Get(phServer[ii]);

        if (!pItem)
        {
            (*ppErrors)[ii] = OPC_E_INVALIDHANDLE;

            bErrors = true;
            continue;
        }

        if (pPercentDeadband[ii] < 0.0 || pPercentDeadband[ii] > 100.0)
        {
            (*ppErrors)[ii] = E_INVALIDARG;
            
            bErrors = true;
            continue;
        }

        pItem->SetDeadband(pPercentDeadband[ii]);

        (*ppErrors)[ii] = S_OK;
    }

    return (bErrors)?S_FALSE:S_OK;
}

// GetItemDeadband
HRESULT COpcDaGroup::GetItemDeadband( 
    DWORD         dwCount,
    OPCHANDLE * phServer,
    FLOAT    ** ppPercentDeadband,
    HRESULT  ** ppErrors
)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    if (m_bDeleted) return E_FAIL;

    // check arguments.
    if (phServer == NULL || ppPercentDeadband == NULL || ppErrors == NULL)
    {
        return E_INVALIDARG;
    }

    *ppPercentDeadband = NULL;
    *ppErrors          = NULL;

    if (dwCount == 0)
    {
        return E_INVALIDARG;
    }

    bool bErrors = false;

    // update items.
    *ppPercentDeadband = OpcArrayAlloc(FLOAT, dwCount);
    *ppErrors          = OpcArrayAlloc(HRESULT, dwCount);

    for (DWORD ii = 0; ii < dwCount; ii++)
    {
        (*ppPercentDeadband)[ii] = 0.0;

        auto pItem = m_cItems.Get(phServer[ii]);

        if (!pItem)
        {
            (*ppErrors)[ii] = OPC_E_INVALIDHANDLE;

            bErrors = true;
            continue;
        }

        FLOAT ftlDeadband = pItem->GetDeadband();
       
        if (ftlDeadband == OPC_NO_DEADBAND)
        {
            (*ppErrors)[ii] = OPC_E_DEADBANDNOTSET;

            bErrors = true;
            continue;
        }

        (*ppPercentDeadband)[ii] = ftlDeadband;
        (*ppErrors)[ii] = S_OK;
    }

    return (bErrors)?S_FALSE:S_OK;
}

// ClearItemDeadband
HRESULT COpcDaGroup::ClearItemDeadband(
    DWORD       dwCount,
    OPCHANDLE * phServer,
    HRESULT  ** ppErrors
)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    if (m_bDeleted) return E_FAIL;

    // check arguments.
    if (phServer == NULL || ppErrors == NULL)
    {
        return E_INVALIDARG;
    }

    *ppErrors = NULL;

    if (dwCount == 0)
    {
        return E_INVALIDARG;
    }

    bool bErrors = false;

    // update items.
    *ppErrors = OpcArrayAlloc(HRESULT, dwCount);

    for (DWORD ii = 0; ii < dwCount; ii++)
    {
        auto pItem = m_cItems.Get(phServer[ii]);

        if (!pItem)
        {
            (*ppErrors)[ii] = OPC_E_INVALIDHANDLE;

            bErrors = true;
            continue;
        }
        
        // check if the deadband is not currently set for the item.
        FLOAT ftlDeadband = pItem->GetDeadband();
       
        if (ftlDeadband == OPC_NO_DEADBAND)
        {
            (*ppErrors)[ii] = OPC_E_DEADBANDNOTSET;

            bErrors = true;
            continue;
        }

        // clear the deadband.
        pItem->SetDeadband(OPC_NO_DEADBAND);
        (*ppErrors)[ii] = S_OK;
    }

    return (bErrors)?S_FALSE:S_OK;
}

//============================================================================
// IOPCItemSamplingMgt

// SetItemSamplingRate
HRESULT COpcDaGroup::SetItemSamplingRate(
    DWORD         dwCount,
    OPCHANDLE * phServer,
    DWORD     * pdwRequestedSamplingRate,
    DWORD    ** ppdwRevisedSamplingRate,
    HRESULT  ** ppErrors
)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    if (m_bDeleted) return E_FAIL;

    // check arguments.
    if (phServer == NULL || pdwRequestedSamplingRate == NULL || ppdwRevisedSamplingRate == NULL || ppErrors == NULL)
    {
        return E_INVALIDARG;
    }

    *ppdwRevisedSamplingRate = NULL;
    *ppErrors                = NULL;

    if (dwCount == 0)
    {

        return E_INVALIDARG;
    }

    bool bErrors = false;

    // update items.
    *ppdwRevisedSamplingRate = OpcArrayAlloc(DWORD, dwCount);
    *ppErrors                = OpcArrayAlloc(HRESULT, dwCount);
    
    for (DWORD ii = 0; ii < dwCount; ii++)
    {
        auto pItem = m_cItems.Get(phServer[ii]);

        if (!pItem)
        {
            (*ppErrors)[ii] = OPC_E_INVALIDHANDLE;

            bErrors = true;
            continue;
        }
        
        // adjust sampling rate.
        DWORD dwSamplingRate = pdwRequestedSamplingRate[ii];

        if (dwSamplingRate == 0 || dwSamplingRate%MAX_UPDATE_RATE != 0)
        {
            dwSamplingRate = MAX_UPDATE_RATE*(dwSamplingRate/MAX_UPDATE_RATE+1);
        }

        (*ppdwRevisedSamplingRate)[ii] = dwSamplingRate;

        // update item.
        pItem->SetSamplingRate(dwSamplingRate);      
        
        // set error status.
        (*ppErrors)[ii] = S_OK;

        if (pdwRequestedSamplingRate[ii] != dwSamplingRate)
        {
            (*ppErrors)[ii] = OPC_S_UNSUPPORTEDRATE;
            bErrors = true;
        }
    }

    return (bErrors)?S_FALSE:S_OK;
}

// GetItemSamplingRate
HRESULT COpcDaGroup::GetItemSamplingRate(
    DWORD         dwCount,
    OPCHANDLE * phServer,
    DWORD    ** ppdwSamplingRate,
    HRESULT  ** ppErrors
)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    if (m_bDeleted) return E_FAIL;

    // check arguments.
    if (phServer == NULL || ppdwSamplingRate == NULL || ppErrors == NULL)
    {
        return E_INVALIDARG;
    }

    *ppdwSamplingRate = NULL;
    *ppErrors         = NULL;

    if (dwCount == 0)
    {
        return E_INVALIDARG;
    }

    bool bErrors = false;

    // update items.
    *ppdwSamplingRate = OpcArrayAlloc(DWORD, dwCount);
    *ppErrors         = OpcArrayAlloc(HRESULT, dwCount);
    
    for (DWORD ii = 0; ii < dwCount; ii++)
    {
        (*ppdwSamplingRate)[ii] = 0;

        auto pItem = m_cItems.Get(phServer[ii]);

        if (!pItem)
        {
            (*ppErrors)[ii] = OPC_E_INVALIDHANDLE;

            bErrors = true;
            continue;
        }

        DWORD dwSamplingRate = pItem->GetSamplingRate();
       
        if (dwSamplingRate == OPC_NO_SAMPLING_RATE)
        {
            (*ppErrors)[ii] = OPC_E_RATENOTSET;

            bErrors = true;
            continue;
        }

        (*ppdwSamplingRate)[ii] = dwSamplingRate;
        (*ppErrors)[ii] = S_OK;
    }

    return (bErrors)?S_FALSE:S_OK;
}

// ClearItemSamplingRate
HRESULT COpcDaGroup::ClearItemSamplingRate(
    DWORD         dwCount,
    OPCHANDLE * phServer,
    HRESULT  ** ppErrors
)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    if (m_bDeleted) return E_FAIL;

    // check arguments.
    if (phServer == NULL || ppErrors == NULL)
    {
        return E_INVALIDARG;
    }

    *ppErrors = NULL;

       if (dwCount == 0)
    {
        return E_INVALIDARG;
    }
    
    bool bErrors = false;

    // update items.
    *ppErrors = OpcArrayAlloc(HRESULT, dwCount);
    
    for (DWORD ii = 0; ii < dwCount; ii++)
    {
        auto pItem = m_cItems.Get(phServer[ii]);

        if (!pItem)
        {
            (*ppErrors)[ii] = OPC_E_INVALIDHANDLE;

            bErrors = true;
            continue;
        }
        
        // check if the sampling rate is not currently set for the item.
        DWORD dwSamplingRate = pItem->GetSamplingRate();
       
        if (dwSamplingRate == OPC_NO_SAMPLING_RATE)
        {
            (*ppErrors)[ii] = OPC_E_RATENOTSET;

            bErrors = true;
            continue;
        }

        // clear the sampling rate.
        pItem->SetSamplingRate(OPC_NO_SAMPLING_RATE);
        (*ppErrors)[ii] = S_OK;
    }

    return (bErrors)?S_FALSE:S_OK;
}

// SetItemBufferEnable
HRESULT COpcDaGroup::SetItemBufferEnable(
    DWORD       dwCount, 
    OPCHANDLE * phServer, 
    BOOL      * pbEnable,
    HRESULT  ** ppErrors
)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    if (m_bDeleted) return E_FAIL;

    // check arguments.
    if (phServer == NULL || pbEnable == NULL || ppErrors == NULL)
    {
        return E_INVALIDARG;
    }

    *ppErrors = NULL;

       if (dwCount == 0)
    {
        return E_INVALIDARG;
    }

    bool bErrors = false;

    // update items.
    *ppErrors = OpcArrayAlloc(HRESULT, dwCount);
    
    for (DWORD ii = 0; ii < dwCount; ii++)
    {
        auto pItem = m_cItems.Get(phServer[ii]);

        if (!pItem)
        {
            (*ppErrors)[ii] = OPC_E_INVALIDHANDLE;

            bErrors = true;
            continue;
        }

        pItem->SetBufferEnabled(pbEnable[ii]);

        (*ppErrors)[ii] = S_OK;
    }

    return (bErrors)?S_FALSE:S_OK;
}

// GetItemBufferEnable
HRESULT COpcDaGroup::GetItemBufferEnable(
    DWORD       dwCount, 
    OPCHANDLE * phServer, 
    BOOL     ** ppbEnable,
    HRESULT  ** ppErrors
)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    if (m_bDeleted) return E_FAIL;

    // check arguments.
    if (phServer == NULL || ppbEnable == NULL || ppErrors == NULL)
    {
        return E_INVALIDARG;
    }

    *ppbEnable = NULL;
    *ppErrors = NULL;

       if (dwCount == 0)
    {
        return E_INVALIDARG;
    }

    bool bErrors = false;

    // update items.
    *ppbEnable = OpcArrayAlloc(BOOL, dwCount);
    *ppErrors  = OpcArrayAlloc(HRESULT, dwCount);
    
    for (DWORD ii = 0; ii < dwCount; ii++)
    {
        auto pItem = m_cItems.Get(phServer[ii]);

        if (!pItem)
        {
            (*ppErrors)[ii] = OPC_E_INVALIDHANDLE;

            bErrors = true;
            continue;
        }

        (*ppbEnable)[ii] = pItem->GetBufferEnabled();
        (*ppErrors)[ii]  = S_OK;
    }

    return (bErrors)?S_FALSE:S_OK;
}

