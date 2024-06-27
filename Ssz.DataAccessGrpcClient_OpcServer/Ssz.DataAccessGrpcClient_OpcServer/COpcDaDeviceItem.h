//==============================================================================
// TITLE: COpcDaDeviceItem.h
//
// CONTENTS:
// 
// A single simulated I/O point.
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
// 2002/09/09 RSA   First release.
// 2002/11/16 RSA   Second release.
//

#ifndef _COpcDaDeviceItem_H_
#define _COpcDaDeviceItem_H_

#if _MSC_VER >= 1000
#pragma once
#endif // _MSC_VER >= 1000

#include "COpcBinary.h"
#include "COpcDaProperty.h"
#include "COpcDaTypeDictionary.h"
#include "IOpcDaCache.h"

#include <vcclr.h>

using namespace Ssz::Utils::Net4;
using namespace Ssz::Utils::DataAccess;
using namespace System;
using namespace System::Collections::Generic;

//==============================================================================
// CLASS:   COpcDaComplexItem
// PURPOSE: Contains the complex type information for a sub-item within a complex type.

class COpcDaComplexItem
{
    OPC_CLASS_NEW_DELETE()

public:

    COpcString    TypeName;
    COpcString    TypeSystemID;
    COpcString    DictionaryID;
    COpcString    DictionaryItemID;
    COpcString    TypeID;
    COpcString    TypeItemID;
    bool          EnableFilters;
    COpcStringMap Filters;

    COpcDaComplexItem() {}
    ~COpcDaComplexItem() {}
};

//============================================================================
// TYPEDEF: COpcDaComplexItemMap
// PURPOSE: A table of virtual items indexed by item id.

typedef COpcMap<COpcString,COpcDaComplexItem*> COpcDaComplexItemMap;

//==============================================================================
// CLASS:   COpcDaDeviceItem
// PURPOSE: A source server for data exchange data.

class COpcDaDeviceItem
{
    OPC_CLASS_NEW_DELETE()

public:

    //==========================================================================
    // Operators

    // Constructor
    COpcDaDeviceItem(const COpcString& cItemID, String^ elementId, IDataAccessProvider^ dataAccessProvider);

    // Destructor 
    ~COpcDaDeviceItem();
    
    //==========================================================================
    // Public Methods

    // GetItemID
    COpcString GetItemID() { return m_cItemID; }
    
    // GetDataType
    VARTYPE GetDataType() { return m_vtDataType; }

    // GetAccessRights
    INT GetAccessRights() { return m_iAccessRights; }

    // GetInternalWritesOnly
    bool GetInternalWritesOnly() { return m_bInternalWritesOnly; }

    // SetInternalWritesOnly
    void SetInternalWritesOnly(bool bInternalWritesOnly) { m_bInternalWritesOnly = bInternalWritesOnly; }
    
    // CheckRange
    bool CheckRange(const VARIANT& cValue);
    
    // GetAvailableProperties
    HRESULT GetAvailableProperties(
        IOpcDaCache* pCache,
        const COpcString&     cItemPath,
        bool                  bReturnValues,
        COpcDaPropertyList&   cProperties
    );

    // GetAvailableProperties
    HRESULT GetAvailableProperties(
        IOpcDaCache* pCache,
        const COpcString&      cItemPath,
        const COpcList<DWORD>& cIDs,
        bool                   bReturnValues,
        COpcDaPropertyList&    cProperties);

    // Read
    HRESULT Read(
        IOpcDaCache* pCache,
        const COpcString&     cItemPath,
        DWORD                 dwPropertyID,
        VARIANT&              cValue, 
        FILETIME*             pftTimestamp = NULL,
        WORD*                 pwQuality    = NULL
    );

    // Write
    HRESULT Write(
        IOpcDaCache* pCache,
        const COpcString&     cItemPath,
        bool                  bInternalWrite,
        DWORD                 dwPropertyID,
        const VARIANT&        cValue, 
        FILETIME*             pftTimestamp = NULL,
        WORD*                 pwQuality    = NULL
    );

    // Update
    void Update(LONGLONG uTicks, UINT uInterval, FILETIME ftUtcNow);

    // BuildAddressSpace
    bool BuildAddressSpace(IOpcDaCache* pCache);

    // ClearAddressSpace
    void ClearAddressSpace(IOpcDaCache* pCache);

    //==========================================================================
    // Xml Serialize

    // Init
    void Init();

    // Clear
    void Clear();

    // Read
    bool Read(IOpcDaCache* pCache, COpcXmlElement& cElement);

    // Write
    bool Write(IOpcDaCache* pCache, COpcXmlElement& cElement);    

    void UpdateDataType();

    uint Handle;

private:

    //========================================================================
    // Private Methods
    
    // Parameters
    struct Parameters
    {
        LONGLONG uTicks;
        UINT     uInterval;
        UINT     uPeriod;
        UINT     uSamplingRate;
        int      eWaveform;
        DOUBLE   dblMaxValue;
        DOUBLE   dblMinValue;
        
        Parameters();
        Parameters(const Parameters& cParameters);
    };

    // Read
    HRESULT Read(
        IOpcDaCache* pCache,
        COpcDaComplexItem*    pItem,
        const COpcString&     cItemPath,
        VARIANT&              cValue, 
        FILETIME*             pftTimestamp,
        WORD*                 pwQuality
    );

    // Write
    HRESULT Write(
        IOpcDaCache* pCache,
        COpcDaComplexItem*    pItem,
        const COpcString&     cItemPath,
        const VARIANT&        cValue, 
        FILETIME*             pftTimestamp,
        WORD*                 pwQuality
    );

    // WriteFilter
    HRESULT WriteFilter(
        IOpcDaCache* pCache,
        COpcDaComplexItem* pItem,
        const COpcString&  cItemPath,
        const VARIANT&     cValue
    );
    
    // FindItem
    COpcDaComplexItem* FindItem(const COpcString& cItemPath);

    // IsFilterItem
    bool IsFilterItem(const COpcString& cItemPath);

    // GetFilterName
    COpcString GetFilterName(const COpcString& cItemPath);
    
    // GetItemID
    COpcString GetItemID(const COpcString& cItemPath);

    // AddItemAndLink
    bool AddItemAndLink(IOpcDaCache* pCache, const COpcString& cItemPath);

    // RemoveItemAndLink
    void RemoveItemAndLink(IOpcDaCache* pCache, const COpcString& cItemPath);

    // Init
    void Init(OpcXml::AnyType& cValue, Parameters& cParameters);

    // Update
    bool Update(OpcXml::AnyType& cValue, Parameters cParameters, LONGLONG uTicks, UINT uInterval);

    // Calculate
    OpcXml::SByte Calculate(OpcXml::SByte cValue, Parameters& cParameters);
    OpcXml::Byte Calculate(OpcXml::Byte cValue, Parameters& cParameters);
    OpcXml::Short Calculate(OpcXml::Short cValue, Parameters& cParameters);
    OpcXml::UShort Calculate(OpcXml::UShort cValue, Parameters& cParameters);
    OpcXml::Int Calculate(OpcXml::Int cValue, Parameters& cParameters);
    OpcXml::UInt Calculate(OpcXml::UInt cValue, Parameters& cParameters);
    OpcXml::Long Calculate(OpcXml::Long cValue, Parameters& cParameters);
    OpcXml::ULong Calculate(OpcXml::ULong cValue, Parameters& cParameters);
    OpcXml::Float Calculate(OpcXml::Float cValue, Parameters& cParameters);
    OpcXml::Double Calculate(OpcXml::Double cValue, Parameters& cParameters);
    OpcXml::Decimal Calculate(OpcXml::Decimal cValue, Parameters& cParameters);
    OpcXml::DateTime Calculate(OpcXml::DateTime cValue, Parameters& cParameters);
    OpcXml::Boolean Calculate(OpcXml::Boolean cValue, Parameters& cParameters);
    OpcXml::String Calculate(OpcXml::String& cValue, Parameters& cParameters);

    // ValidatePropertyID
    HRESULT ValidatePropertyID(
        COpcDaComplexItem* pItem,
        const COpcString&  cItemPath, 
        DWORD              dwID, 
        int                iAccessRequired
    );

    // Calculate
    void Calculate(OpcXml::AnyType& cNewValue, const OpcXml::AnyType& cOldValue, double dblFraction);

    // FindEnumIndex
    int FindEnumIndex(LPCWSTR szEnumValue);

    // FindEnumValue
    BSTR FindEnumValue(int iEnumIndex);

    // Ramp
    LONGLONG Ramp(double dblFraction, LONGLONG ullMax, LONGLONG ullMin);

    // Sin
    double Sin(double dblFraction, double dblMax, double dblMin);
    
    //==========================================================================
    // Private Members

    COpcString          m_cItemID;    
    bool                m_bInternalWritesOnly;

    short               m_vtDataType;    //1
    OpcXml::AnyType     m_cValue;        //2
    WORD                m_wQuality;      //3
    FILETIME            m_ftTimestamp;   //4
    INT                 m_iAccessRights; //5
    FLOAT               m_fltScanRate;   //6
    OPCEUTYPE           m_eEUType;       //7
    COpcStringArray     m_cEnumValues;   //8
    double              m_dblMaxValue;   //102
    double              m_dblMinValue;   //103

    COpcMap<DWORD,OpcXml::AnyType*> m_cProperties;

    // complex data properties.
    COpcDaComplexItem*   m_pNativeFormat;
    COpcDaComplexItemMap m_cTypeConversions;

    gcroot<String^> _elementId;
    gcroot<ValueSubscription^> _valueSubscription;
};

//============================================================================
// TYPEDEF: COpcDaDeviceItemMap
// PURPOSE: A table of device items indexed by item id.

typedef COpcMap<COpcString,COpcDaDeviceItem*> COpcDaDeviceItemMap;

#endif // _COpcDaDeviceItem_H_
