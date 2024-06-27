//============================================================================
// TITLE: COpcDaDevice.h
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

#ifndef _COpcDaDevice_H_
#define _COpcDaDevice_H_

#if _MSC_VER >= 1000
#pragma once
#endif // _MSC_VER >= 1000

#include "COpcThread.h"
#include "COpcDaDeviceItem.h"
#include "IOpcDaDevice.h"
#include "COpcDaTypeDictionary.h"
#include "UtilityClasses\ObjectManager.h"

//============================================================================
// CLASS:   COpcDaDevice
// PURPOSE: Simulates an I/O device.

#include <vcclr.h>
#include <msclr\auto_handle.h>

using namespace msclr;

using namespace Ssz::Utils::Net4;
using namespace System;
using namespace System::Collections::Generic;
using namespace System::Threading;
using namespace Ssz::DataAccessGrpcClient_OpcServer::Common;

class COpcDaDevice :     
    public IOpcDaDevice    
{
    OPC_CLASS_NEW_DELETE()

public:   

    //========================================================================
    // Public Operators

    // Constructor
    COpcDaDevice()
    {
        _syncRoot = gcnew LeveledLock(1100, true);
        _dataAccessProvider = GrpcDataAccessProviderHelper::GetGrpcDataAccessProvider();
        Init();
    }

    // Destructor
    ~COpcDaDevice()
    {
        Clear();
    }

    //========================================================================
    // Public Methods

    // Start    
    bool Start(IOpcDaCache* pCache, COpcXmlElement& cElement, Ssz::Utils::IDispatcher^ callbackDispatcher);

    // Stop
    void Stop(IOpcDaCache* pCache);    

    void Loop(CancellationToken ct, LONGLONG uTicks, UINT uInterval, FILETIME ftUtcNow);

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

    //========================================================================
    // Xml Serialize
    
    // Init
    void Init();

    // Clear
    void Clear();

    // Read
    bool Read(IOpcDaCache* pCache, COpcXmlElement& cElement);

    // Write
    bool Write(IOpcDaCache* pCache, COpcXmlElement& cElement);

	LeveledLock^ GetSyncRoot()
	{
		return _syncRoot;
	}

protected:

    //========================================================================
    // Protected Methods    

    // GetDeviceItem
    COpcDaDeviceItem* GetDeviceItem(const COpcString& cItemID);

private:   

    struct DeviceItemPointer
    {
        DeviceItemPointer(COpcDaDeviceItem* p)
        {
            P = p;
        }

        COpcDaDeviceItem* P;
    };

    //========================================================================
    // Private Methods

    // Read
    bool Read(IOpcDaCache* pCache, const COpcString& cElementPath, COpcXmlElement& cElement);
    
    // ParseItemID
    COpcDaDeviceItem* ParseItemID(const COpcString& cItemID, uint hItemHandle, COpcString& cItemPath);

    COpcDaDeviceItem* AddXiItem(IOpcDaCache* pCache, const COpcString& cItemID, System::String^ itemId);    

    //========================================================================
    // Private Members

    COpcDaDeviceItemMap                 m_cItems;
    COpcStringList                      m_cItemList;
    COpcMap<COpcString, COpcStringList*> m_cBranches;    
    COpcDaTypeDictionaryMap             m_cDictionaries;

    ::ObjectManager<DeviceItemPointer>     m_ItemsManager;	

    bool _allowAddingUnknownTags;
    gcroot<String^> _filterForAddingUnknownTags;    
	
    gcroot<IDataAccessProvider^> _dataAccessProvider;    

	gcroot<LeveledLock^> _syncRoot;
};   


#endif // _COpcDaDevice_H_
