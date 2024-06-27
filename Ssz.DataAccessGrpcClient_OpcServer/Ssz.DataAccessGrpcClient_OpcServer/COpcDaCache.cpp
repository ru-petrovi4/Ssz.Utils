//==============================================================================
// TITLE: COpcDaCache.h
//
// CONTENTS:
// 
// The global item cache for an OPC server.
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
// 2003/03/22 RSA   Added support for complex data.
// 2003/04/08 RSA   Added check to prevent item ids from being returned for basic properties.
// 2003/05/06 RSA   Added check for empty strings when parsing item ids.
// 2003/06/25 RSA   Fixed memory problems and fetched server info from resource block.

#include "StdAfx.h"
#include "COpcDaCache.h"

#include <msclr\auto_handle.h>

using namespace msclr;

//============================================================================
// Local Declarations

#define TAG_CONFIG _T("Config")

//==============================================================================
// COpcDaCache

// Constructor
COpcDaCache::COpcDaCache()
{
    _syncRoot = gcnew LeveledLock(1200, true);

    m_eState        = OPC_STATUS_SUSPENDED;
    m_pAddressSpace = new COpcDaBrowseElement(NULL);
    m_ipSelfRegInfo = NULL;
}

// Destructor 
COpcDaCache::~COpcDaCache()
{
    // clear dictionaries.
    OPC_POS pos = m_cDictionaries.GetStartPosition();

    while (pos != NULL)
    {
        COpcString cDictionaryName;
        COpcDaTypeDictionary* pDictionary = NULL;
        m_cDictionaries.GetNextAssoc(pos, cDictionaryName, pDictionary);

        delete pDictionary;
    }

    m_cDictionaries.RemoveAll();
    
    // clear items.
    m_ItemsMap.clear();    

    // clear address space.
    delete m_pAddressSpace;
    m_pAddressSpace = NULL;

    if (m_ipSelfRegInfo != NULL)
    {
        m_ipSelfRegInfo->Release();
        m_ipSelfRegInfo = NULL;
    }
}

// GetState
OPCSERVERSTATE COpcDaCache::GetState() const
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    return GetStateInternal();
}

// GetState
OPCSERVERSTATE COpcDaCache::GetStateInternal() const
{
    return m_eState;
}

// SetState
void COpcDaCache::SetState(OPCSERVERSTATE eState)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    SetStateInternal(eState);
}

// SetState
void COpcDaCache::SetStateInternal(OPCSERVERSTATE eState)
{
    m_eState = eState;
}

// GetVersionInfo
void COpcDaCache::GetVersionInfo(OpcDaVersionInfo& cInfo)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    cInfo = m_cVersionInfo;
}

//==============================================================================
// Item Access

// AddItem
bool COpcDaCache::AddItem(const COpcString& cItemID, uint hDeviceItemHandle)
{
    uint hCacheItemHandle = 0;
    
    // check for dupicate item id.
    if (!cItemID.IsEmpty())
    {
        // check for duplicate item id.       

        if (m_ItemsMap.find(cItemID) != m_ItemsMap.end())
        {
            return false;
        }
    }    

    // create cache item and index by item id.
    hCacheItemHandle = m_Items.Put(make_shared<COpcDaCacheItem>(this, cItemID, GetDeviceInternal(cItemID), hDeviceItemHandle));    
    m_ItemsMap[cItemID] = hCacheItemHandle;    

    return true;
}

// RemoveItem
bool COpcDaCache::RemoveItem(const COpcString& cItemID)
{
    auto itItem = m_ItemsMap.find(cItemID);
    if (itItem != m_ItemsMap.end())
    {
        m_ItemsMap.erase(cItemID);
        m_Items.Reset(itItem->second);
        return true;
    }

    return false;    
}

// AddItemAndLink
bool COpcDaCache::AddItemAndLink(const COpcString& cBrowsePath, uint hDeviceItemHandle)
{
    if (m_pAddressSpace->Find(cBrowsePath) != NULL)
    {
        return false;
    }

    // create new browse element.
    COpcBrowseElement* pElement = m_pAddressSpace->Insert(cBrowsePath);

    // create new item.
    if (!AddItem(pElement->GetItemID(), hDeviceItemHandle))
    {
        pElement->Remove();
        return false;
    }

    return true;
}

// RemoveItemAndLink
bool COpcDaCache::RemoveItemAndLink(const COpcString& cBrowsePath)
{
    COpcBrowseElement* pElement = m_pAddressSpace->Find(cBrowsePath);

    if (pElement == NULL)
    {
        return false;
    } 

    auto itItem = m_ItemsMap.find(pElement->GetItemID());
    if (itItem != m_ItemsMap.end())
    {        
        m_Items.Reset(itItem->second);
        m_ItemsMap.erase(pElement->GetItemID());
    }    

    // remove link.
    pElement->Remove();

    return true;
}

// AddLink
bool COpcDaCache::AddLink(const COpcString& cBrowsePath)
{
    // check for a unique browse path.
    if (m_pAddressSpace->Find(cBrowsePath) != NULL)
    {
        return false;
    }

    // create new browse element.
    m_pAddressSpace->Insert(cBrowsePath);

    return true;
}

// AddLink
bool COpcDaCache::AddLink(const COpcString& cBrowsePath, const COpcString& cItemID)
{  
    // lookup up item.
    uint hCacheItemHandle = 0;

    /*
    if (!m_ItemsMap.Lookup(cItemID, hCacheItemHandle))
    {
        return false;
    }*/

    // check for a unique browse path.
    if (m_pAddressSpace->Find(cBrowsePath) != NULL)
    {
        return false;
    }

    // create new browse element.
    m_pAddressSpace->Insert(cBrowsePath, cItemID);

    return true;
}

// RemoveEmptyLink
bool COpcDaCache::RemoveEmptyLink(const COpcString& cBrowsePath)
{
    COpcBrowseElement* pChild = m_pAddressSpace->Find(cBrowsePath);

    if (pChild == NULL)
    {
        return false;
    }

    if (pChild->GetChild(0) == NULL)
    {
        pChild->Remove();
    }   

    return true;
}

// RemoveLink
bool COpcDaCache::RemoveLink(const COpcString& cBrowsePath)
{
    COpcBrowseElement* pChild = m_pAddressSpace->Find(cBrowsePath);

    if (pChild == NULL)
    {
        return false;
    }

    pChild->Remove();

    return true;
}

// GetItemHandle
uint COpcDaCache::GetItemHandle(const COpcString& cItemID, DWORD& dwPropertyID)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    dwPropertyID = 0;
    // parse item id to determine if it is qualified with a property id.
    COpcString cParsedItemID = cItemID;    

    if (!OpcParseItemID(cParsedItemID, dwPropertyID))
    {
        return 0;
    }

    // find the cache item referenced by the item id.
    uint hCacheItemHandle = 0;

    auto itItem = m_ItemsMap.find(cParsedItemID);
    if (itItem == m_ItemsMap.end())
    {
        return 0;
    }

    return itItem->second;
}

// LinkExists
bool COpcDaCache::LinkExists(const COpcString& cBrowsePath)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    if (m_pAddressSpace->Find(cBrowsePath) == NULL)
    {
        return false;
    }

    return true;
}

// GetItemResult
HRESULT COpcDaCache::GetItemResult(
    uint hItemHandle, 
    OPCITEMRESULT&    cResult
)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    // find the cache item referenced by the item id.
    auto pItem = m_Items.Get(hItemHandle); 

    if (!pItem)
    {
        return OPC_E_UNKNOWNITEMID;
    }

    pItem->GetItemResult(cResult);

    return S_OK;
}

// GetItemAttributes
HRESULT COpcDaCache::GetItemAttributes(
    uint hItemHandle, 
    OPCITEMATTRIBUTES& cAttributes
)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    // find the cache item referenced by the item id.
    auto pItem = m_Items.Get(hItemHandle); 

    if (!pItem)
    {
        return OPC_E_UNKNOWNITEMID;
    }

    pItem->GetItemAttributes(cAttributes);

    return S_OK;
}

// GetItemProperty
HRESULT COpcDaCache::GetItemProperty(
    uint hItemHandle, 
    DWORD              dwProperty,
    VARIANT&           cValue
)    
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    // find the cache item referenced by the item id.
    auto pItem = m_Items.Get(hItemHandle); 

    if (pItem)
    {
        return OPC_E_UNKNOWNITEMID;
    }

    return pItem->GetProperty(dwProperty, cValue);
}

// GetItemProperties
HRESULT COpcDaCache::GetItemProperties(
    uint hItemHandle, 
    bool                bReturnValues,
    COpcDaPropertyList& cProperties
)    
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();    

    // find the cache item referenced by the item id.
    auto pItem = m_Items.Get(hItemHandle); 

    if (!pItem)
    {
        return OPC_E_UNKNOWNITEMID;
    }

    return pItem->GetProperties(bReturnValues, cProperties);
}

// GetItemProperties
HRESULT COpcDaCache::GetItemProperties(
    uint hItemHandle,
    const COpcList<DWORD>& cIDs,
    bool                   bReturnValues,
    COpcDaPropertyList&    cProperties
)    
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();    

    // find the cache item referenced by the item id.
    auto pItem = m_Items.Get(hItemHandle); 

    if (!pItem)
    {
        return OPC_E_UNKNOWNITEMID;
    }

    return pItem->GetProperties(cIDs, bReturnValues, cProperties);
}

// Read
HRESULT COpcDaCache::Read(    
    uint              hItemHandle,
    DWORD             dwPropertyID,
    LCID              lcid,
    VARTYPE           vtReqType,
    DWORD             dwMaxAge,
    VARIANT&          cValue,
    FILETIME&         ftTimestamp,
    WORD&             wQuality
)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();    
    
    // find the cache item referenced by the item id.
    auto pItem = m_Items.Get(hItemHandle); 

    if (!pItem)
    {
        return OPC_E_UNKNOWNITEMID;
    }

    // read from the item.
	return pItem->Read(
        lcid,
        vtReqType,
        dwMaxAge,
        dwPropertyID,
        cValue, 
        ftTimestamp,
        wQuality);
}

// Write
HRESULT COpcDaCache::Write(
    uint              hItemHandle,
    DWORD             dwPropertyID,
    LCID              lcid,
    VARIANT&          cValue,
    FILETIME*         pftTimestamp,
    WORD*             pwQuality
)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();    
    
    // find the cache item referenced by the item id.
    auto pItem = m_Items.Get(hItemHandle); 

    if (!pItem)
    {
        return OPC_E_UNKNOWNITEMID;
    }

    // write to the item.
    HRESULT hResult = pItem->Write(
        lcid,
        dwPropertyID,
        cValue, 
        pftTimestamp,
        pwQuality);

    if (FAILED(hResult))
    {
        return hResult;
    }

    return S_OK;
}

//==============================================================================
// Browsing Functions

// BrowseUp
bool COpcDaCache::BrowseUp(
    const COpcString& cOldPath, 
    COpcString&       cNewPath
)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    cNewPath.Empty();

    // can't browse up from root.
    if (cOldPath.IsEmpty())
    {
        return false;
    }

    // search for current node - error if missing or invalid.
    if (m_pAddressSpace != NULL)
    {
        COpcBrowseElement* pNode = m_pAddressSpace->Find(cOldPath);

        if (pNode != NULL)
        {
            // parent should never be missing if browse path is not empty.
            if (pNode->GetParent() == NULL)
            {
                return false;       
            }

            cNewPath = pNode->GetParent()->GetBrowsePath();
            return true;
        }
    }

    return false;
}

// BrowseDown
bool COpcDaCache::BrowseDown(
    const COpcString& cOldPath, 
    const COpcString& cName, 
    COpcString&       cNewPath
)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    cNewPath.Empty();

    // no where to go to.
    if (cName.IsEmpty())
    {
        cNewPath = cOldPath;
        return true;
    }

    // nothing to search.
    if (m_pAddressSpace == NULL)
    {
        return false;
    }
    
    // search for current node - error if missing or invalid.
    COpcBrowseElement* pNode = m_pAddressSpace;

    if (!cOldPath.IsEmpty())
    {
        pNode = m_pAddressSpace->Find(cOldPath);

        if (pNode == NULL)
        {
            return false;
        }
    }

    // search for child - error if missing.
    COpcBrowseElement* pChild = pNode->Find(cName);

    if (pChild == NULL)
    {
        return false;
    }

    // check for a leaf node.
    if (pChild->GetChild(0) == NULL)
    {
        return false;
    }

    cNewPath = pChild->GetBrowsePath();
    return true;
}

// BrowseTo
bool COpcDaCache::BrowseTo(
    const COpcString& cItemID, 
    COpcString&       cNewPath
)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    cNewPath.Empty();

    // nothing to search.
    if (m_pAddressSpace == NULL)
    {
        return false;
    }

    // move to root.
    if (cItemID.IsEmpty())
    {
        return true;
    }

    // search assuming the browse and item id are the same.
    COpcBrowseElement* pNode = m_pAddressSpace->Find(cItemID);

    if (pNode == NULL)
    {
        return false;
    }

    // check for a leaf node.
    if (pNode->GetChild(0) == NULL)
    {
        return false;
    }

    cNewPath = pNode->GetBrowsePath();
    return true;
}

// GetItemID
bool COpcDaCache::GetItemID(
    const COpcString& cPath, 
    const COpcString& cName, 
    COpcString&       cItemID
)
{
    if (cPath.IsEmpty())
    {
        cItemID = cName;
        return true;
    }

    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    cItemID.Empty();

    // nothing to search.
    if (m_pAddressSpace == NULL)
    {
        return false;
    }

    COpcBrowseElement* pNode = m_pAddressSpace;

    // search for current node - error if missing or invalid.
    if (!cPath.IsEmpty())
    {
        pNode = m_pAddressSpace->Find(cPath);

        if (pNode == NULL)
        {
            return false;
        }
    }

    // return the node item id if child name not specified. 
    if (cName.IsEmpty())
    {
        cItemID = pNode->GetItemID();
        return true;
    }

    // search for child - error if missing.
    COpcBrowseElement* pChild = pNode->Find(cName);

    if (pChild == NULL)
    {
        return false;
    }

    cItemID = pChild->GetItemID();
    return true;
}

// Browse
bool COpcDaCache::Browse(
    const COpcString& cPath, 
    OPCBROWSETYPE     eType, 
    const COpcString& cNameFilter,
    VARTYPE           vtDataTypeFilter,     
    DWORD             dwAccessRightsFilter,
    COpcStringList&   cNodes
)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    cNodes.RemoveAll();

    // nothing to search.
    if (m_pAddressSpace == NULL)
    {
        return false;
    }

    COpcStringList cHits;
    COpcBrowseElement* pNode = NULL;

    // check if currently at root.
    if (cPath.IsEmpty())
    { 
        pNode = m_pAddressSpace;
        m_pAddressSpace->Browse(eType, OPC_EMPTY_STRING, cHits);
    }

     // search for current node - error if missing or invalid.
    else
    {
        pNode = m_pAddressSpace->Find(cPath);

        if (pNode == NULL)
        {
            return false;
        }

        // find all names at the current level and below (if requested). 
        ((COpcDaBrowseElement*)pNode)->Browse(eType, OPC_EMPTY_STRING, cHits);
    }

    // apply filters.
    OPC_POS pos = cHits.GetHeadPosition();

    while (pos != NULL)
    {
        COpcString cName = cHits.GetNext(pos);

        COpcBrowseElement* pChild = pNode->Find(cName);        
        
        uint hItemHandle = 0;
        bool bIsItem = false;
        auto itItem = m_ItemsMap.find(pChild->GetItemID());
        if (itItem != m_ItemsMap.end())
        {
            hItemHandle = itItem->second;
            bIsItem = true;
        }
        
        bool bHasChildren = (pChild->GetChild(0) != NULL); 
        
        // filter items.
        if (eType != OPC_FLAT)
        {
            if ((eType == OPC_BRANCH && !bHasChildren) || (eType != OPC_BRANCH && bHasChildren))
            {
                continue;
            }
        }
        else
        {
            if (!bIsItem)
            {
                continue;
            }
        }

        // apply the element name filter.
        if (!OpcMatchPattern(cName, cNameFilter))
        {
            continue;
        }

        // apply data type/access rights filter.
        if (vtDataTypeFilter != VT_EMPTY || dwAccessRightsFilter != 0)
        {
            // lookup item.
            auto pItem = m_Items.Get(hItemHandle); 

            if (!pItem)
            {
                continue;
            }

            // apply data type filter.
            if (vtDataTypeFilter != VT_EMPTY && pItem->GetDataType() != vtDataTypeFilter)
            {
                continue;
            }

            // apply access rights filter.
            if (dwAccessRightsFilter != 0 && (dwAccessRightsFilter & pItem->GetAccessRights()) == 0)
            {
                continue;
            }
        }

        if (eType == OPC_FLAT)
        {
            cNodes.AddTail(pChild->GetItemID());
        }
        else
        {
            cNodes.AddTail(cName);
        }
    }

    return true;
}

// Browse
HRESULT COpcDaCache::Browse(
    const COpcString&  cPath,
    DWORD              dwMaxElements,
    OPCBROWSEFILTER    dwFilter,
    const COpcString&  cNameFilter,
    const COpcString&  cVendorFilter,
    DWORD*             pdwStartIndex,
    DWORD*               pdwCount,
    OPCBROWSEELEMENT** ppBrowseElements
)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    *pdwCount = 0;
    *ppBrowseElements = NULL;

    // nothing to search.
    if (m_pAddressSpace == NULL)
    {
        return S_OK;
    }

    // find the element to browse.
    COpcBrowseElement* pElement = NULL;

    if (cPath.IsEmpty() || m_pAddressSpace == NULL)
    { 
        pElement = m_pAddressSpace;
    }
    else
    {
        pElement = m_pAddressSpace->Find(cPath);
    }

    if (pElement == NULL)
    {
        return OPC_E_UNKNOWNITEMID;
    }

    // apply filters.
    COpcArray<OPCBROWSEELEMENT*> cHits;

    COpcBrowseElement* pChild = pElement->GetChild((*pdwStartIndex)++);

    for (;pChild != NULL; pChild = pElement->GetChild((*pdwStartIndex)++))
    {
        COpcString cName   = pChild->GetName();
        COpcString cItemID = pChild->GetItemID();   
        bool bHasChildren  = (pChild->GetChild(0) != NULL);
        bool bIsItem = m_ItemsMap.find(cItemID) != m_ItemsMap.end();

        // apply the element name filter 
        if (!OpcMatchPattern(cName, cNameFilter))
        {
            continue;
        }

        // apply the vendor filter as a secondary element name filter.
        if (!OpcMatchPattern(cName, cVendorFilter))
        {
            continue;
        }

        // apply has children mask.
        if (dwFilter == OPC_BROWSE_FILTER_BRANCHES) 
        {
            if (!bHasChildren)
            {
                continue;
            }
        }

        // apply has is item mask.
        if (dwFilter == OPC_BROWSE_FILTER_ITEMS) 
        {
            if (!bIsItem)
            {
                continue;
            }
        }

        // check if max elements exceeded.
        if (dwMaxElements > 0 && dwMaxElements <= cHits.GetSize())
        {
            (*pdwStartIndex)--;
            break;
        }

        // create result.
        OPCBROWSEELEMENT* pResult = new OPCBROWSEELEMENT();

        pResult->szName       = OpcStrDup((LPCWSTR)cName);
        pResult->szItemID     = OpcStrDup((LPCWSTR)cItemID);

        pResult->dwFlagValue  = 0;
        pResult->dwFlagValue |= (bIsItem)?OPC_BROWSE_ISITEM:0;
        pResult->dwFlagValue |= (bHasChildren)?OPC_BROWSE_HASCHILDREN:0;

        pResult->ItemProperties.hrErrorID       = S_OK;
        pResult->ItemProperties.dwNumProperties = 0;
        pResult->ItemProperties.pItemProperties = NULL;

        cHits.Append(pResult);
    }

    // check if search completed.
    if (pChild == NULL)
    {
        *pdwStartIndex = 0;
    }

    // check if no matched found.
    if (cHits.GetSize() == 0)
    {
        *pdwStartIndex = 0;

        return S_OK;
    }

    // the memory is in the hits list is copied to the returned array.
    *pdwCount = cHits.GetSize();
    *ppBrowseElements = OpcArrayAlloc(OPCBROWSEELEMENT, *pdwCount);

    for (DWORD ii = 0; ii < *pdwCount; ii++)
    {
        OPCBROWSEELEMENT* pHit = cHits[ii];    
        (*ppBrowseElements)[ii] = *pHit;
        delete pHit;
    }

    cHits.RemoveAll();

    return S_OK;
}

// Start
bool COpcDaCache::Start()
{
    bool bResult = true;

    TRY
    {        
        auto_handle<IDisposable> cLock = _syncRoot->Enter();

        // get the executable version information.
        if (!OpcDaGetModuleVersion(m_cVersionInfo))
        {
            m_cVersionInfo.cFileDescription = _T("Unknown");
            m_cVersionInfo.wMajorVersion    = 0;
            m_cVersionInfo.wMinorVersion    = 0;
            m_cVersionInfo.wMajorVersion    = 0;
            m_cVersionInfo.wRevisionNumber  = 0;
        }                

        // start the message queue.
        if (!m_cQueue.Start())
        {
            THROW_(bResult, false);
        }

        // start the write thread.
        if (!m_cWriteThread.Start())
        {
            THROW_(bResult, false);
        }         
    }
    CATCH
    {
        Stop();
    }

    return bResult;
}

// Stop
void COpcDaCache::Stop()
{    
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    // de-activate any type dictionaries.  
    OPC_POS pos = m_cDictionaries.GetStartPosition();

    while (pos != NULL)
    {
        COpcString cItemID;
        COpcDaTypeDictionary* pDictionary = NULL;
        m_cDictionaries.GetNextAssoc(pos, cItemID, pDictionary);

        pDictionary->Stop(this);
    }    

    m_cQueue.Stop();
    m_cWriteThread.Stop();    
}

//=========================================================================
// Message Queue Functions

// QueueMessage
void COpcDaCache::QueueMessage(COpcMessage* pMsg)
{
    if (pMsg->GetType() == OPC_TRANSACTION_WRITE)
    {
        if (!m_cWriteThread.QueueTransaction((COpcDaTransaction*)pMsg))
        {
            delete pMsg;
        }
    }
    else
    {
        if (!m_cQueue.QueueMessage(pMsg))
        {
            delete pMsg;
        }
    }
}

//==============================================================================
// Complex Data

COpcDaTypeDictionary* COpcDaCache::CreateTypeDictionary(const COpcString& cFileName)
{
    // check if the dictionary has already been loaded.
    OPC_POS pos = m_cDictionaries.GetStartPosition();

    while (pos != NULL)
    {
        COpcString cItemID;
        COpcDaTypeDictionary* pDictionary = NULL;
        m_cDictionaries.GetNextAssoc(pos, cItemID, pDictionary);

        if (pDictionary->GetFileName() == cFileName && pDictionary->GetTypeSystemID() == OPC_TYPE_SYSTEM_OPCBINARY)
        {
            return pDictionary;
        }
    }

    // initialize a new dictionary.
    COpcDaTypeDictionary* pDictionary = new COpcDaTypeDictionary();

    if (!pDictionary->Start(this, cFileName, false))
    {
        delete pDictionary; 
        return NULL;
    }

    // index dictionary by item id.
    m_cDictionaries[pDictionary->GetItemID()] = pDictionary;

    return pDictionary;
}

// GetTypeDictionary
COpcDaTypeDictionary* COpcDaCache::GetTypeDictionary(const COpcString& cItemID)
{
    // parse the item id to find the item id for the dictionary.
    COpcString cBaseItemID = cItemID;

    // the item has the format 'CPX/<system>/<dictionary>[/<type>]'
    int iSlashes = 0;

    for (UINT ii = 0; ii < cItemID.GetLength(); ii++)
    {
        if (cItemID[ii] == '/') iSlashes++;

        if (iSlashes == 3)
        {
            cBaseItemID = cItemID.SubStr(0, ii);
            break;
        }
    }

    // lookup the type dictionary.
    COpcDaTypeDictionary* pDictionary = NULL;

    if (!m_cDictionaries.Lookup(cBaseItemID, pDictionary))
    {
        return NULL;
    }

    return pDictionary;
}

// CreateXmlSchemaMapping
COpcDaTypeDictionary* COpcDaCache::CreateXmlSchemaMapping(const COpcString& cFileName)
{
    // check if the dictionary has already been loaded.
    OPC_POS pos = m_cDictionaries.GetStartPosition();

    while (pos != NULL)
    {
        COpcString cItemID;
        COpcDaTypeDictionary* pDictionary = NULL;
        m_cDictionaries.GetNextAssoc(pos, cItemID, pDictionary);

        if (pDictionary->GetFileName() == cFileName && pDictionary->GetTypeSystemID() == OPC_TYPE_SYSTEM_XMLSCHEMA)
        {
            return pDictionary;
        }
    }

    // initialize a new dictionary.
    COpcDaTypeDictionary* pDictionary = new COpcDaTypeDictionary();

    if (!pDictionary->Start(this, cFileName, true))
    {
        delete pDictionary; 
        return NULL;
    }

    // index dictionary by item id.
    m_cDictionaries[pDictionary->GetItemID()] = pDictionary;

    return pDictionary;
}

// GetDevice
IOpcDaDevice* COpcDaCache::GetDevice(const COpcString& cItemID)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();    

    return GetDeviceInternal(cItemID);
}

// GetDevice
IOpcDaDevice* COpcDaCache::GetDeviceInternal(const COpcString& cItemID)
{    
    // check for items in the complex type tree.
    if (IsKnownItemInternal(cItemID))
    {
        return this;
    }

    return ::GetDevice(cItemID);
}

//========================================================================
// IOpcDaDevice

// BuildAddressSpace
bool COpcDaCache::BuildAddressSpace(IOpcDaCache* pCache)
{    
    // type dictionaries are added to the address space by the devices that use them.
    return true;
}

// ClearAddressSpace
void COpcDaCache::ClearAddressSpace(IOpcDaCache* pCache)
{
    OPC_POS pos = m_cDictionaries.GetStartPosition();

    while (pos != NULL)
    {
        COpcString cItemID;
        COpcDaTypeDictionary* pDictionary = NULL;
        m_cDictionaries.GetNextAssoc(pos, cItemID, pDictionary);

        pDictionary->ClearAddressSpace(this);
    }
}

// IsKnownItem
bool COpcDaCache::IsKnownItem(IOpcDaCache* pCache, const COpcString& cItemID)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    return IsKnownItemInternal(cItemID);
}

// IsKnownItem
bool COpcDaCache::IsKnownItemInternal(const COpcString& cItemID)
{   
    if (_tcsncmp(CPX_DATABASE_ROOT, (LPCTSTR)cItemID, _tcslen(CPX_DATABASE_ROOT)) == 0)
    {
        return true;
    }

    return false;
}

// GetAvailableProperties
HRESULT COpcDaCache::GetAvailableProperties(
    IOpcDaCache* pCache,
    const COpcString&   cItemID, 
    uint                hItemHandle,
    bool                bReturnValues,
    COpcDaPropertyList& cProperties
)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    COpcDaTypeDictionary* pDictionary = GetTypeDictionary(cItemID);

    if (pDictionary == NULL)
    {
        return OPC_E_UNKNOWNITEMID;
    }

    return pDictionary->GetAvailableProperties(this, cItemID, 0, bReturnValues, cProperties);
}

// GetAvailableProperties
HRESULT COpcDaCache::GetAvailableProperties(
    IOpcDaCache* pCache,
    const COpcString&      cItemID, 
    uint                   hItemHandle,
    const COpcList<DWORD>& cIDs,
    bool                   bReturnValues,
    COpcDaPropertyList&    cProperties
)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    COpcDaTypeDictionary* pDictionary = GetTypeDictionary(cItemID);

    if (pDictionary == NULL)
    {
        return OPC_E_UNKNOWNITEMID;
    }

    return pDictionary->GetAvailableProperties(this, cItemID, 0, cIDs, bReturnValues, cProperties);
}

// Read
HRESULT COpcDaCache::Read(
    IOpcDaCache* pCache,
    const COpcString& cItemID, 
    uint              hItemHandle,
    DWORD             dwPropertyID,
    VARIANT&          cValue, 
    FILETIME*         pftTimestamp,
    WORD*             pwQuality
)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    COpcDaTypeDictionary* pDictionary = GetTypeDictionary(cItemID);

    if (pDictionary == NULL)
    {
        return OPC_E_UNKNOWNITEMID;
    }

    return pDictionary->Read(this, cItemID, 0, dwPropertyID, cValue, pftTimestamp, pwQuality);
}

// Write
HRESULT COpcDaCache::Write(
    IOpcDaCache* pCache,
    const COpcString& cItemID, 
    uint              hItemHandle,
    DWORD             dwPropertyID,
    const VARIANT&    cValue, 
    FILETIME*         pftTimestamp,
    WORD*             pwQuality
)
{    
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    COpcDaTypeDictionary* pDictionary = GetTypeDictionary(cItemID);

    if (pDictionary == NULL)
    {
        return OPC_E_UNKNOWNITEMID;
    }

    return pDictionary->Write(this, cItemID, 0, dwPropertyID, cValue, pftTimestamp, pwQuality);
}

// PrepareAddItem
bool COpcDaCache::PrepareAddItem(IOpcDaCache* pCache, const COpcString& cItemID)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    IOpcDaDevice* pDevice = ::GetDevice(cItemID);
    bool result = pDevice->PrepareAddItem(this, cItemID); // Add item to device if possible
    m_devices.insert(pDevice);

    return result;
}

// CommitAddItems
void COpcDaCache::CommitAddItems(IOpcDaCache* pCache)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    for (auto it = m_devices.begin(); it != m_devices.end(); it++)
    {
        (*it)->CommitAddItems(this);
    }  

    m_devices.clear();
}
