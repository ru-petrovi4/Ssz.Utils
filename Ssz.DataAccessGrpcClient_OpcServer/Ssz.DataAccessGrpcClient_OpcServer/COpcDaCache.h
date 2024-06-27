//============================================================================
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
// 2003/06/25 RSA   Fixed memory problems and fetched server info from resource block.

#ifndef _COpcDaCache_H_
#define _COpcDaCache_H_

#if _MSC_VER >= 1000
#pragma once
#endif // _MSC_VER >= 1000

#include "opcda.h"

#include "COpcDaBrowseElement.h"
#include "COpcDaCacheItem.h"
#include "COpcDaWriteThread.h"
#include "OpcDaHelpers.h"
#include "COpcDaTypeDictionary.h"
#include "IOpcDaDevice.h"
#include "IOpcDaCache.h"
#include "UtilityClasses\ObjectManager.h"

#include <vcclr.h>

#include <set>
#include <map>

using namespace Ssz::Utils::Net4;

//============================================================================
// CLASS:   COpcDaCache
// PURPOSE: Maintains an in memory cache of DA items.

class COpcDaCache : public IOpcDaDevice, public IOpcDaCache
{
    OPC_CLASS_NEW_DELETE()

public:

    //========================================================================
    // Public Operators

    // Constructor
    COpcDaCache();

    // Destructor
    ~COpcDaCache();
    
    //=========================================================================
    // Configuration Functions

    // Start
    virtual bool Start();
    
    // Stop
    virtual void Stop();     
    
    // GetState
    OPCSERVERSTATE GetState() const;

    // SetState
    void SetState(OPCSERVERSTATE eState);

    // GetVersionInfo
    void GetVersionInfo(OpcDaVersionInfo& cInfo);

    //=========================================================================
    // Browsing Functions

    // BrowseUp
    bool BrowseUp(
        const COpcString& cOldPath, 
        COpcString&       cNewPath
    );

    // BrowseDown
    bool BrowseDown(
        const COpcString& cOldPath, 
        const COpcString& cName, 
        COpcString&       cNewPath
    );

    // BrowseTo
    bool BrowseTo(
        const COpcString& cItemID, 
        COpcString&       cNewPath
    );

    // GetItemID
    bool GetItemID(
        const COpcString& cPath, 
        const COpcString& cName, 
        COpcString&       cItemID
    );

    // Browse
    bool Browse(
        const COpcString& cFullPath, 
        OPCBROWSETYPE     eType, 
        const COpcString& cFilterCriteria,  
        VARTYPE           vtDataTypeFilter,     
        DWORD             dwAccessRightsFilter,
        COpcStringList&   cNodes
    );

    // Browse
    HRESULT Browse(
        const COpcString&  cPath,
        DWORD              dwMaxElements,
        OPCBROWSEFILTER    dwFilter,
        const COpcString&  cNameFilter,
        const COpcString&  cVendorFilter,
        DWORD*             pdwStartIndex,
        DWORD*               pdwCount,
        OPCBROWSEELEMENT** ppBrowseElements
    );
       
    //=========================================================================
    // Item Management
    
    // AddItem
    virtual bool AddItem(const COpcString& cItemID, uint hDeviceItemHandle);
    
    // RemoveItem
    virtual bool RemoveItem(const COpcString& cItemID);    

    // AddItemAndLink
    virtual bool AddItemAndLink(const COpcString& cBrowsePath, uint hDeviceItemHandle);    
    
    // RemoveItemAndLink
    virtual bool RemoveItemAndLink(const COpcString& cBrowsePath);    

    // AddLink
    virtual bool AddLink(const COpcString& cBrowsePath);

    // AddLink
    virtual bool AddLink(const COpcString& cBrowsePath, const COpcString& cItemID);
    
    // RemoveLink
    virtual bool RemoveLink(const COpcString& cBrowsePath);
    
    // RemoveEmptyLink
    virtual bool RemoveEmptyLink(const COpcString& cBrowsePath);

    //=========================================================================
    // Item Access

    // GetItemHandle
    uint GetItemHandle(const COpcString& cItemID, DWORD& dwPropertyID);

    // LinkExists
    bool LinkExists(const COpcString& cBrowsePath);

    // GetItemResult
    HRESULT GetItemResult(
        uint hItemHandle, 
        OPCITEMRESULT&    cResult
    );

    // GetItemAttributes
    HRESULT GetItemAttributes(
        uint hItemHandle, 
        OPCITEMATTRIBUTES& cAttributes
    );

    // GetItemProperty
    HRESULT GetItemProperty(
        uint hItemHandle, 
        DWORD              dwProperty,
        VARIANT&           cValue
    );

    // GetItemProperties
    HRESULT GetItemProperties(
        uint hItemHandle, 
        bool                bReturnValues,
        COpcDaPropertyList& cProperties
    );

    // GetItemProperties
    HRESULT GetItemProperties(
        uint hItemHandle,
        const COpcList<DWORD>& cIDs,
        bool                   bReturnValues,
        COpcDaPropertyList&    cProperties
    );

    // Read
    HRESULT Read(
        uint              hItemHandle,
        DWORD             dwPropertyID,
        LCID              lcid,
        VARTYPE           vtReqType,
        DWORD             dwMaxAge,
        VARIANT&          cValue,
        FILETIME&         ftTimestamp,
        WORD&             wQuality
    );

    // Write
    HRESULT Write(
        uint              hItemHandle,
        DWORD             dwPropertyID,
        LCID              lcid,
        VARIANT&          cValue,
        FILETIME*         pftTimestamp = NULL,
        WORD*             pwQuality    = NULL
    );    

    //========================================================================
    // Message Queue

    // QueueMessage
    void QueueMessage(COpcMessage* pMsg);    

    //========================================================================
    // Complex Data
    
    // creates a type dictionary (if it does not already exist) from the specified file.
    virtual COpcDaTypeDictionary* CreateTypeDictionary(const COpcString& cFileName);

    // gets the type dictionary referenced by the item id.
    virtual COpcDaTypeDictionary* GetTypeDictionary(const COpcString& cItemID);    
    
    // creates an XML schema mapping (if it does not already exist) from the specified file.
    virtual COpcDaTypeDictionary* CreateXmlSchemaMapping(const COpcString& cFileName);
    
    //========================================================================
    // IOpcDaDevice

    // BuildAddressSpace
    virtual bool BuildAddressSpace(IOpcDaCache* pCache);

    // ClearAddressSpace
    virtual void ClearAddressSpace(IOpcDaCache* pCache);

    // IsKnownItem
    virtual bool IsKnownItem(IOpcDaCache* pCache, const COpcString& cItemID);

    // GetAvailableProperties
    virtual HRESULT GetAvailableProperties(
        IOpcDaCache* pCache,
        const COpcString&   cItemID, 
        uint                hItemHandle,
        bool                bReturnValues,
        COpcDaPropertyList& cProperties);

    // GetAvailableProperties
    virtual HRESULT GetAvailableProperties(
        IOpcDaCache* pCache,
        const COpcString&      cItemID,
        uint                   hItemHandle,
        const COpcList<DWORD>& cIDs,
        bool                   bReturnValues,
        COpcDaPropertyList&    cProperties);

    // Read
    virtual HRESULT Read(
        IOpcDaCache* pCache,
        const COpcString& cItemID,
        uint              hItemHandle,
        DWORD             dwPropertyID,
        VARIANT&          cValue, 
        FILETIME*         pftTimestamp = NULL,
        WORD*             pwQuality = NULL);

    // Write
    virtual HRESULT Write(
        IOpcDaCache* pCache,
        const COpcString& cItemID, 
        uint              hItemHandle,
        DWORD             dwPropertyID,
        const VARIANT&    cValue, 
        FILETIME*         pftTimestamp = NULL,
        WORD*             pwQuality = NULL);

    // PrepareAddItem
	virtual bool PrepareAddItem(IOpcDaCache* pCache, const COpcString& cItemID);

    // CommitAddItems
	virtual void CommitAddItems(IOpcDaCache* pCache);

    // returns the device object for the specified item.
    virtual IOpcDaDevice* GetDevice(const COpcString& cItemID);    

protected:
    // GetState
    OPCSERVERSTATE GetStateInternal() const;

    // SetState
    void SetStateInternal(OPCSERVERSTATE eState);    

    // returns the device object for the specified item.
    IOpcDaDevice* GetDeviceInternal(const COpcString& cItemID);

    // IsKnownItem
    bool IsKnownItemInternal(const COpcString& cItemID);

private:

    //========================================================================
    // Private Members
    gcroot<LeveledLock^> _syncRoot;

    OpcDaVersionInfo        m_cVersionInfo;
    
    COpcDaBrowseElement*    m_pAddressSpace;    
    ::ObjectManager<COpcDaCacheItem> m_Items;
    std::map<COpcString, uint>  m_ItemsMap;
    OPCSERVERSTATE          m_eState;
    
    COpcThreadPool        m_cQueue;
    COpcDaWriteThread       m_cWriteThread;    

    IXMLDOMElement*         m_ipSelfRegInfo;    

    COpcDaTypeDictionaryMap m_cDictionaries;

    std::set<IOpcDaDevice*> m_devices;
};

//============================================================================
// FUNCTION: GetCache
// PURPOSE:  Returns a reference (must never be null) to the global cache.

extern COpcDaCache& GetCache();

#endif // _COpcDaCache_H_