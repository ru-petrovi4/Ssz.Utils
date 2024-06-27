//============================================================================
// TITLE: COpcDaDevice.cpp
//
// CONTENTS:
// 
// Simulates a simple I/O device.
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
//

#include "StdAfx.h"
#include "COpcDaDevice.h"
#include "OpcDaHelpers.h"
#include <atltime.h>

//============================================================================
// Local Declarations

#define MAX_SAMPLING_RATE 100

#define TAG_SEPARATOR      _T("/")
#define TAG_BROWSE_ELEMENT _T("BrowseElement")
#define TAG_ELEMENT_NAME   _T("ElementName")
#define TAG_ITEM           _T("Item")
#define TAG_CHILD_ELEMENTS _T("Children")

//============================================================================
// COpcDaDevice

// Start
bool COpcDaDevice::Start(IOpcDaCache* pCache, COpcXmlElement& cElement, Ssz::Utils::IDispatcher^ callbackDispatcher)
{ 	
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    /*auto contextParams = gcnew CaseInsensitiveDictionary<String^>();
	args["UserName"] = "OPC";
	try
	{
		args["WindowsUserName"] = Environment::UserName;
	}
	catch (...)
	{
	}
	args["UserRole"] = "TRAINEE";
	args["InterfaceDesc"] = System::Configuration::ConfigurationManager::AppSettings["InterfaceDesc"];*/
	String^ clientWorkstationName;
	try
	{
        clientWorkstationName = System::Net::Dns::GetHostName();
	}
	catch (...)
	{
        clientWorkstationName = Environment::MachineName;
	}	

    String^ serverAddress = System::Configuration::ConfigurationManager::AppSettings["ServerAddress"];    
    
    _dataAccessProvider->Initialize(nullptr, serverAddress, "Ssz_DataAccessGrpcClient_OpcServer", clientWorkstationName, 
        System::Configuration::ConfigurationManager::AppSettings["SystemNameToConnect"],
        gcnew Ssz::Utils::CaseInsensitiveDictionary<String^>(),
        gcnew Ssz::Utils::DataAccess::DataAccessProviderOptions(),
        callbackDispatcher);
    
    // parse the xml document.
    if (((IXMLDOMElement*)cElement != NULL) && !Read(pCache, cElement))
    {
        return false;
    }

    // add items to cache.
    if (!BuildAddressSpace(pCache))
    {
        return false;
    }

    return true;
}

// Stop
void COpcDaDevice::Stop(IOpcDaCache* pCache)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();    

    // clear items from cache.
    ClearAddressSpace(pCache);

    _dataAccessProvider->CloseAsync();      
}
 
// Update
void COpcDaDevice::Loop(CancellationToken ct, LONGLONG uTicks, UINT uInterval, FILETIME ftUtcNow)
{
	if (ct.IsCancellationRequested) return;
	
    {
		auto_handle<IDisposable> cLock = _syncRoot->Enter();		
        
		// update each group.
		OPC_POS pos = m_cItems.GetStartPosition();

		while (pos != NULL)
		{
			COpcString cItemID;
			COpcDaDeviceItem* pItem = NULL;
			m_cItems.GetNextAssoc(pos, cItemID, pItem);

			pItem->Update(uTicks, uInterval, ftUtcNow);
		}
    }
}

//========================================================================
// IOpcDaDevice

// BuildAddressSpace
bool COpcDaDevice::BuildAddressSpace(IOpcDaCache* pCache)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    // items to address space.
    OPC_POS pos = m_cItemList.GetHeadPosition();

    while (pos != NULL)
    {
        COpcString cItemID = m_cItemList.GetNext(pos);

        COpcDaDeviceItem* pItem = NULL;

        if (m_cItems.Lookup(cItemID, pItem))
        {
            if (!pItem->BuildAddressSpace(pCache))
            {
                return false;
            }
        }
    }

    return true;
}

// ClearAddressSpace
void COpcDaDevice::ClearAddressSpace(IOpcDaCache* pCache)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    // items to address space.
    OPC_POS pos = m_cItemList.GetHeadPosition();

    while (pos != NULL)
    {
        COpcString cItemID = m_cItemList.GetNext(pos);

        COpcDaDeviceItem* pItem = NULL;

        if (m_cItems.Lookup(cItemID, pItem))
        {
            pItem->ClearAddressSpace(pCache);
        }
    }
}

// ParseItemID
COpcDaDeviceItem* COpcDaDevice::ParseItemID(const COpcString& cItemID, uint hItemHandle, COpcString& cItemPath)
{
    cItemPath   = (LPCWSTR)NULL;

    if (hItemHandle != 0)
    {
        auto pp = m_ItemsManager.Get(hItemHandle);
        if (!!pp) return pp->P;        
    }

    COpcString cRootID = cItemID;

    int iIndex = cRootID.Find(CPX_DATABASE_ROOT);

    if (iIndex != -1)
    {
        cItemPath = cRootID.SubStr(iIndex+_tcslen(CPX_DATABASE_ROOT)+1);
        cRootID   = cRootID.SubStr(0, iIndex-1);
    }

    COpcDaDeviceItem* pItem = NULL;

    if (!m_cItems.Lookup(cRootID, pItem))
    {
        pItem = NULL;
    }   

    return pItem;
}

// IsKnownItem
bool COpcDaDevice::IsKnownItem(IOpcDaCache* pCache, const COpcString& cItemID)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    COpcString cItemPath;
    COpcDaDeviceItem* pItem = ParseItemID(cItemID, 0, cItemPath);

    if (pItem != NULL) return true;

    if (_allowAddingUnknownTags)
    {
        String^ itemId = gcnew String((LPCWSTR)cItemID);
        if (!String::IsNullOrWhiteSpace(_filterForAddingUnknownTags))
        {
            if (itemId->StartsWith(_filterForAddingUnknownTags))
            {
                pItem = AddXiItem(pCache, cItemID, itemId->Remove(0, _filterForAddingUnknownTags->Length));                
            }
        }
        else
        {
            pItem = AddXiItem(pCache, cItemID, itemId);            ;
        }        
    }
    
    return pItem != NULL;
}

// GetAvailableProperties
HRESULT COpcDaDevice::GetAvailableProperties(
    IOpcDaCache* pCache,
    const COpcString&   cItemID, 
    uint                hItemHandle,
    bool                bReturnValues,
    COpcDaPropertyList& cProperties
)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    COpcString cItemPath;
    COpcDaDeviceItem* pItem = ParseItemID(cItemID, hItemHandle, cItemPath);

    if (pItem == NULL)
    {
        return OPC_E_UNKNOWNITEMID;
    }
    
    return pItem->GetAvailableProperties(pCache, cItemPath, bReturnValues, cProperties);
}

// GetAvailableProperties
HRESULT COpcDaDevice::GetAvailableProperties(
    IOpcDaCache* pCache,
    const COpcString&      cItemID, 
    uint                   hItemHandle,
    const COpcList<DWORD>& cIDs,
    bool                   bReturnValues,
    COpcDaPropertyList&    cProperties
)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    COpcString cItemPath;
    COpcDaDeviceItem* pItem = ParseItemID(cItemID, hItemHandle, cItemPath);

    if (pItem == NULL)
    {
        return OPC_E_UNKNOWNITEMID;
    }

    return pItem->GetAvailableProperties(pCache, cItemPath, cIDs, bReturnValues, cProperties);
}

// Read
HRESULT COpcDaDevice::Read(
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

    COpcString cItemPath;
    COpcDaDeviceItem* pItem = ParseItemID(cItemID, hItemHandle, cItemPath);    

    if (pItem == NULL)
    {
        return OPC_E_UNKNOWNITEMID;
    }    

	return pItem->Read(pCache, cItemPath, dwPropertyID, cValue, pftTimestamp, pwQuality);
}

// Write
HRESULT COpcDaDevice::Write(
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

    COpcString cItemPath;
    COpcDaDeviceItem* pItem = ParseItemID(cItemID, hItemHandle, cItemPath);

    if (pItem == NULL)
    {
        return OPC_E_UNKNOWNITEMID;
    }

    return pItem->Write(pCache, cItemPath, false, dwPropertyID, cValue, pftTimestamp, pwQuality);
}

// PrepareAddItem
bool COpcDaDevice::PrepareAddItem(IOpcDaCache* pCache, const COpcString& cItemID)
{
    return IsKnownItem(pCache, cItemID);
}

// CommitAddItems
void COpcDaDevice::CommitAddItems(IOpcDaCache* pCache)
{    
}

// GetDeviceItem
COpcDaDeviceItem* COpcDaDevice::GetDeviceItem(const COpcString& cItemID)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    COpcDaDeviceItem* pItem = NULL;

    if (!m_cItems.Lookup(cItemID, pItem))
    {
        return NULL;
    }

    return pItem;
}

//========================================================================
// IOpcXmlSerialize

// Init
void COpcDaDevice::Init()
{
    m_cItems.RemoveAll();
    m_ItemsManager.Clear();
    m_cItemList.RemoveAll();
    m_cBranches.RemoveAll();

    // Read App Settings
    System::Collections::Specialized::NameValueCollection^ appSettings = System::Configuration::ConfigurationManager::AppSettings;
    System::String^ sAllowAddingUnknownTags = appSettings["AllowAddingUnknownTags"];
    if (sAllowAddingUnknownTags != nullptr && sAllowAddingUnknownTags->ToUpper() == "TRUE")
    _allowAddingUnknownTags = true;
    else _allowAddingUnknownTags = false;

    _filterForAddingUnknownTags = appSettings["FilterForAddingUnknownTags"];    
}

// Clear
void COpcDaDevice::Clear()
{
    OPC_POS pos = NULL;

    // clear all items.
    pos = m_cItems.GetStartPosition();

    while (pos != NULL)
    {
        COpcString cItemID;
        COpcDaDeviceItem* pItem = NULL;
        m_cItems.GetNextAssoc(pos, cItemID, pItem);

        delete pItem;
    }

    m_cItems.RemoveAll();
    m_ItemsManager.Clear();
    m_cItemList.RemoveAll();

    // clear all branches.
    pos = m_cBranches.GetStartPosition();

    while (pos != NULL)
    {
        COpcString cItemID;
        COpcStringList* pBranch = NULL;
        m_cBranches.GetNextAssoc(pos, cItemID, pBranch);

        delete pBranch;
    }

    m_cBranches.RemoveAll();

    // clear dictionaries.
    pos = m_cDictionaries.GetStartPosition();

    while (pos != NULL)
    {
        COpcString cDictionaryID;
        COpcDaTypeDictionary* pDictionary = NULL;
        m_cDictionaries.GetNextAssoc(pos, cDictionaryID, pDictionary);

        delete pDictionary;
    }

    m_cDictionaries.RemoveAll();

    Init();
}

// Read
bool COpcDaDevice::Read(IOpcDaCache* pCache, COpcXmlElement& cElement)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    // read top level items.
    COpcXmlElementList cItems;

    UINT uCount = cElement.GetChildren(cItems);

    for (UINT ii = 0; ii < uCount; ii++)
    {
        Read(pCache, (LPTSTR)NULL, cItems[ii]);
    }

    return true;
}

// Read
bool COpcDaDevice::Read(IOpcDaCache* pCache, const COpcString& cElementPath, COpcXmlElement& cElement)
{
    // read element name.
    COpcString cElementName;

    READ_ATTRIBUTE(TAG_ELEMENT_NAME, cElementName);

    if (cElementName.IsEmpty())
    {
        return false;
    }

    // index element by path
    COpcStringList* pBranch = NULL;

    if (!m_cBranches.Lookup(cElementPath, pBranch))
    {
        m_cBranches[cElementPath] = pBranch = new COpcStringList();
    }

    pBranch->AddTail(cElementName);

    // build item id.
    COpcString cItemID;

    cItemID += cElementPath;
    cItemID += (cElementPath.IsEmpty())?_T(""):TAG_SEPARATOR;
    cItemID += cElementName;

    // check if current element is an item.
    COpcXmlElement cItem = cElement.GetChild(TAG_ITEM);

    if (cItem != NULL)
    {
        // check for duplicate item ids.
        if (m_cItems.Lookup(cItemID))
        {
            return false;
        }

        COpcDaDeviceItem* pItem = new COpcDaDeviceItem(cItemID, nullptr, _dataAccessProvider);

        // initialize item.
        if (!pItem->Read(pCache, cItem))
        {
            delete pItem;
            return false;
        }

        // index item by item id.
        m_cItems[cItemID] = pItem;
        pItem->Handle = m_ItemsManager.Put(make_shared<DeviceItemPointer>(pItem));
        m_cItemList.AddTail(cItemID);
    }

    // read child elements.
    COpcXmlElement cChildren = cElement.GetChild(TAG_CHILD_ELEMENTS);

    if (cChildren != NULL)
    {
        COpcXmlElementList cItems;

        UINT uCount = cChildren.GetChildren(cItems);

        for (UINT ii = 0; ii < uCount; ii++)
        {
            Read(pCache, cItemID, cItems[ii]);
        }
    }

    return true;
}

// Write
bool COpcDaDevice::Write(IOpcDaCache* pCache, COpcXmlElement& cElement)
{  
    OPC_ASSERT(false);  
    
    // not implemented.
    
    return false;
}

COpcDaDeviceItem* COpcDaDevice::AddXiItem(IOpcDaCache* pCache, const COpcString& cItemID, String^ xiLocalId)
{ 
    COpcDaDeviceItem* pItem = new COpcDaDeviceItem(cItemID, xiLocalId, _dataAccessProvider);

    // index item by item id.
    m_cItems[cItemID] = pItem;
    pItem->Handle = m_ItemsManager.Put(make_shared<DeviceItemPointer>(pItem));
    m_cItemList.AddTail(cItemID);

    pItem->BuildAddressSpace(pCache);

    return pItem;
}