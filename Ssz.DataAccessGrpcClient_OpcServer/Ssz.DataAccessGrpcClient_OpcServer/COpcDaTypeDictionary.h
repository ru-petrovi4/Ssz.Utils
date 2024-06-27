//==============================================================================
// TITLE: COpcDaTypeDictionary.h
//
// CONTENTS:
// 
// Manages complex type items and complex type descriptions.
//
// (c) Copyright 2003 The OPC Foundation
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
// 2003/03/22 RSA   First implementation.
// 2003/09/17 RSA   Updated for latest draft of the complex data spec.

#ifndef _COpcDaTypeDictionary_H_
#define _COpcDaTypeDictionary_H_

#if _MSC_VER >= 1000
#pragma once
#endif // _MSC_VER >= 1000

#include "IOpcDaDevice.h"
#include "COpcBinary.h"

#define CPX_DATABASE_ROOT _T("CPX")
#define CPX_DATA_FILTERS  _T("DataFilters")

#include <vcclr.h>

using namespace Ssz::Utils::Net4;

//============================================================================
// CLASS:   COpcDaTypeDictionary
// PURPOSE: Manages complex type items and complex type descriptions.

class COpcDaTypeDictionary : 
    public IOpcDaDevice    
{
    OPC_CLASS_NEW_DELETE()

public:

    //========================================================================
    // Public Operators

    // Constructor
    COpcDaTypeDictionary();
            
    // Destructor
    ~COpcDaTypeDictionary();

    //========================================================================
    // Public Methods

    // Start
    bool Start(IOpcDaCache* pCache, const COpcString& cFileName, bool bXmlSchemaMapping = false);
        
    // Stop
    void Stop(IOpcDaCache* pCache);
    
    // GetFileName
    COpcString GetFileName();

    // GetItemID
    COpcString GetItemID();

    // GetTypeSystemID
    COpcString GetTypeSystemID();

    // GetDictionaryID
    COpcString GetDictionaryID();

    // GetTypeID
    COpcString GetTypeID(const COpcString& cTypeName);

    // GetTypeItemID
    COpcString GetTypeItemID(const COpcString& cTypeName);

    // GetBinaryDictionary
    COpcTypeDictionary* GetBinaryDictionary();

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
        

private:
    
    //========================================================================
    // Private Methods

    // ValidatePropertyID
    HRESULT ValidatePropertyID(
        IOpcDaCache* pCache,
        const COpcString& cItemID, 
        DWORD             dwPropertyID, 
        int               iAccessRequired
    );

    // GetValue
    HRESULT GetValue(
        IOpcDaCache* pCache,
        const COpcString& cItemID, 
        DWORD             dwPropertyID,
        VARIANT&          cValue
    );

    // DetectTypes
    bool DetectTypes();
    
    // LoadBinaryDictionary
    bool LoadBinaryDictionary();

    // CreateXmlSchemaMapping
    bool CreateXmlSchemaMapping();

    //========================================================================
    // Private Members
    
    COpcString         m_cFileName;
    COpcString         m_cItemID;

    COpcString         m_cTypeSystemID;
    COpcString         m_cDictionaryName;
    
    COpcXmlDocument    m_cDictionary;

    COpcString         m_cDictionaryID;
    COpcStringMap      m_cTypeXPaths;

    COpcTypeDictionary m_cBinaryDictionary;
    
    gcroot<LeveledLock^> _syncRoot;
};

//============================================================================
// TYPE:    COpcDaTypeDictionaryMap
// PURPOSE: A table type dictionaries indexed by a string.

typedef COpcMap<COpcString,COpcDaTypeDictionary*> COpcDaTypeDictionaryMap;

#endif // _COpcDaTypeDictionary_H_