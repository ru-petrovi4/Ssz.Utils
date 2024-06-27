//==============================================================================
// TITLE: COpcDaTypeDictionary.cpp
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
// 2003/06/25 RSA   Fixed memory problems.
// 2003/09/17 RSA   Updated for latest draft of the complex data spec.

#include "StdAfx.h"
#include "COpcDaTypeDictionary.h"
#include "IOpcDaCache.h"
#include "COpcBinary.h"
#include "OpcDaHelpers.h"

#include <msclr\auto_handle.h>

using namespace msclr;

//============================================================================
// Local Declarations

#define MAX_SAMPLING_RATE 100

#define TAG_SEPARATOR        _T("/")
#define TAG_TYPE_DESCRIPTION _T("TypeDescription")
#define TAG_SCHEMA           _T("xsd:schema")
#define TAG_ELEMENT          _T("element")
#define TAG_OPCBINARY_NAME   _T("TypeID")
#define TAG_XMLSCHEMA_NAME   _T("name")
#define TAG_FIELD_SEPARATOR  _T("\r\n")

//============================================================================
// COpcDaTypeDictionary

// Constructor
COpcDaTypeDictionary::COpcDaTypeDictionary()
{
    _syncRoot = gcnew LeveledLock(100);
}

// Destructor
COpcDaTypeDictionary::~COpcDaTypeDictionary()
{
}

//============================================================================
// COpcDaDevice

// Start
bool COpcDaTypeDictionary::Start(IOpcDaCache* pCache, const COpcString& cFileName, bool bXmlSchemaMapping)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    COpcString cFilePath = m_cFileName = cFileName;

    // construct an absolute path.
    if (cFileName.Find(_T(":")) == -1)
    {
        cFilePath  = OpcDaGetModulePath();
        cFilePath += _T("\\");
        cFilePath += cFileName;
    }

    // extract the dictionary name from the file name.
    m_cDictionaryName = cFileName;

    int iIndex = m_cDictionaryName.ReverseFind(_T("."));

    if (iIndex != -1)
    {
        m_cDictionaryName = m_cDictionaryName.SubStr(0, iIndex);
    }
    
    // attempt to load the file as an XML document.
    if (!m_cDictionary.Load(cFilePath))
    {
        // non-xml type dictionaries not supported.
        return false;
    }

    // initialize the type description id from the XML document.
    COpcString cNamespace = m_cDictionary.GetRoot().GetNamespace();

    // file contains an OPC Binary type dictionary.
    if (cNamespace == OPCXML_NS_OPCBINARY)
    {
        m_cTypeSystemID = OPC_TYPE_SYSTEM_OPCBINARY;

        if (!LoadBinaryDictionary())
        {
            return false;
        }

        if (bXmlSchemaMapping)
        {
            if (!CreateXmlSchemaMapping())
            {
                return false;
            }
        }
    }

    // file contains an XML Schema.
    else if (cNamespace == OPCXML_NS_SCHEMA)
    {            
        m_cTypeSystemID = OPC_TYPE_SYSTEM_XMLSCHEMA;
    }

    // unsupported xml-based type system.
    else
    {
        return false;
    }

    // assign a unique id to the dictionary.
    m_cDictionaryID = m_cDictionaryName;
    m_cDictionaryID += TAG_FIELD_SEPARATOR;

    if (m_cTypeSystemID == OPC_TYPE_SYSTEM_XMLSCHEMA)
    {
        COpcXmlAttribute cAttribute = m_cDictionary.GetRoot().GetAttribute("targetNamespace");

        if (cAttribute != NULL)
        {
            cNamespace = cAttribute.GetValue();
        }
    }

    m_cDictionaryID += cNamespace;

    // appending the file modified time ensures changes made while the server was
    // offline cause the server to report a different dictionary id. this is necessary
    // since clients are allowed to cache dictionary ids between sessions.

    COpcFile cFile;

    if (!cFile.Open(cFilePath))
    {
        return false;
    }

    FILETIME ftLastModified = cFile.GetLastModified();
    
    cFile.Close();

    SYSTEMTIME cSystemTime;
    memset(&cSystemTime, 0, sizeof(cSystemTime));

    if (FileTimeToSystemTime(&ftLastModified, &cSystemTime))
    {        
        TCHAR tsTimestamp[256];
        
        _stprintf_s(
            tsTimestamp,
            _T("%04d-%02d-%02d %02d:%02d:%02d"),
            cSystemTime.wYear,
            cSystemTime.wMonth,
            cSystemTime.wDay,
            cSystemTime.wHour,
            cSystemTime.wMinute,
            cSystemTime.wSecond
        );

        m_cDictionaryID += TAG_FIELD_SEPARATOR;
        m_cDictionaryID += tsTimestamp;
    }

    // detech any types in the dictionary.
    if (!DetectTypes())
    {
        return false;
    }

    // build address space.
    if (!BuildAddressSpace(pCache))
    {
        return false;
    }

    return true;
}

// Stop
void COpcDaTypeDictionary::Stop(IOpcDaCache* pCache)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();
    ClearAddressSpace(pCache);
}

// GetFileName
COpcString COpcDaTypeDictionary::GetFileName() 
{ 
    auto_handle<IDisposable> cLock = _syncRoot->Enter();
    return m_cFileName; 
}

// GetItemID
COpcString COpcDaTypeDictionary::GetItemID()
{ 
    auto_handle<IDisposable> cLock = _syncRoot->Enter();
    return m_cItemID; 
}

// GetTypeSystemID
COpcString COpcDaTypeDictionary::GetTypeSystemID() 
{ 
    auto_handle<IDisposable> cLock = _syncRoot->Enter();
    return m_cTypeSystemID; 
}

// GetDictionaryID
COpcString COpcDaTypeDictionary::GetDictionaryID()
{ 
    auto_handle<IDisposable> cLock = _syncRoot->Enter();
    return m_cDictionaryID; 
}

// GetTypeID
COpcString COpcDaTypeDictionary::GetTypeID(const COpcString& cTypeName) 
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    if (!m_cTypeXPaths.Lookup(cTypeName))
    {
        return (LPCWSTR)NULL;
    }

    return cTypeName;
}

// GetTypeItemID
COpcString COpcDaTypeDictionary::GetTypeItemID(const COpcString& cTypeName) 
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    COpcString cItemID;

    cItemID += m_cItemID;
    cItemID += TAG_SEPARATOR;
    cItemID += GetTypeID(cTypeName);

    return cItemID;
}

// GetBinaryDictionary
COpcTypeDictionary* COpcDaTypeDictionary::GetBinaryDictionary()
{ 
    auto_handle<IDisposable> cLock = _syncRoot->Enter();
    return &m_cBinaryDictionary; 
}

//========================================================================
// IOpcDaDevice

// BuildAddressSpace
bool COpcDaTypeDictionary::BuildAddressSpace(IOpcDaCache* pCache)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();    

    m_cItemID.Empty();

    m_cItemID += CPX_DATABASE_ROOT;
    m_cItemID += TAG_SEPARATOR;
    m_cItemID += GetTypeSystemID();
    m_cItemID += TAG_SEPARATOR;
    m_cItemID += m_cDictionaryName;

    // if this fails the dictionary name is already in use.
    if (!pCache->AddItemAndLink(m_cItemID, 0))
    {
        return false;
    }

    COpcString cBasePath;

    cBasePath += m_cItemID;
    cBasePath += TAG_SEPARATOR;

    // add additional items for individual types.
    OPC_POS pos = m_cTypeXPaths.GetStartPosition();

    while (pos != NULL)
    {
        COpcString cTypeName;
        m_cTypeXPaths.GetNextAssoc(pos, cTypeName);

        pCache->AddItemAndLink(cBasePath + cTypeName, 0);
    }

    return true;
}

// ClearAddressSpace
void COpcDaTypeDictionary::ClearAddressSpace(IOpcDaCache* pCache)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();    

    COpcString cBasePath;
    
    cBasePath += m_cItemID;
    cBasePath += TAG_SEPARATOR;

    // remove items for individual types.
    OPC_POS pos = m_cTypeXPaths.GetStartPosition();

    while (pos != NULL)
    {
        COpcString cTypeName;
        m_cTypeXPaths.GetNextAssoc(pos, cTypeName);

        pCache->RemoveItemAndLink(cBasePath + cTypeName);
    }

    // remove dictionary item.
    pCache->RemoveItemAndLink(m_cItemID);
}

// IsKnownItem
bool COpcDaTypeDictionary::IsKnownItem(IOpcDaCache* pCache, const COpcString& cItemID)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    if (cItemID.Find(m_cItemID) == 0)
    {
        return true;
    }

    return false;
}

// GetAvailableProperties
HRESULT COpcDaTypeDictionary::GetAvailableProperties(
    IOpcDaCache* pCache,
    const COpcString&   cItemID, 
    uint                hItemHandle,
    bool                bReturnValues,
    COpcDaPropertyList& cProperties
)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();
    
    // check if it is a valid item.
    if (!IsKnownItem(pCache, cItemID))
    {
        return OPC_E_INVALIDITEMID;
    }

    // initialize array larger that necessary.
    COpcList<DWORD> cIDs;

    // add standard properties.
    cIDs.AddTail(OPC_PROPERTY_DATATYPE);
    cIDs.AddTail(OPC_PROPERTY_VALUE);
    cIDs.AddTail(OPC_PROPERTY_QUALITY);
    cIDs.AddTail(OPC_PROPERTY_TIMESTAMP);
    cIDs.AddTail(OPC_PROPERTY_ACCESS_RIGHTS);
    cIDs.AddTail(OPC_PROPERTY_SCAN_RATE);
    cIDs.AddTail(OPC_PROPERTY_EU_TYPE);
    cIDs.AddTail(OPC_PROPERTY_EU_INFO);

    // add properties for the dictionary item.
    if (cItemID == m_cItemID)
    {
        cIDs.AddTail(OPC_PROPERTY_DICTIONARY);
    }

    // add properties for a type description item.
    else
    {
        COpcString cTypeName = cItemID.SubStr(m_cItemID.GetLength()+1);

        if (!m_cTypeXPaths.Lookup(cTypeName))
        {
            return OPC_E_UNKNOWNITEMID;
        }

        cIDs.AddTail(OPC_PROPERTY_TYPE_DESCRIPTION);
    }

    // fetch the values for each property.
    return GetAvailableProperties(pCache, cItemID, 0, cIDs, bReturnValues, cProperties);
}

// GetAvailableProperties
HRESULT COpcDaTypeDictionary::GetAvailableProperties(
    IOpcDaCache* pCache,
    const COpcString&      cItemID, 
    uint                   hItemHandle,
    const COpcList<DWORD>& cIDs,
    bool                   bReturnValues,
    COpcDaPropertyList&    cProperties
)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    COpcDaProperty::Create(cIDs, cProperties);

    for (UINT ii = 0; ii < cProperties.GetSize(); ii++)
    {
        HRESULT hResult = S_OK;
    
        if (bReturnValues)
        {
            hResult = GetValue(pCache, cItemID, cProperties[ii]->GetID(), cProperties[ii]->GetValue());
        }
        else
        {
            hResult = ValidatePropertyID(pCache, cItemID, cProperties[ii]->GetID(), OPC_READABLE);
        }

        cProperties[ii]->SetError(hResult);
    }

    return S_OK;
}

// Read
HRESULT COpcDaTypeDictionary::Read(
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

    // read the property value.
    HRESULT hResult = GetValue(pCache, cItemID, dwPropertyID, cValue);

    if (FAILED(hResult))
    {
        return hResult;
    }

    // set timestamp/quality as required.
    if (pftTimestamp != NULL) *pftTimestamp = OpcUtcNow();
    if (pwQuality != NULL)    *pwQuality    = OPC_QUALITY_GOOD;

    return S_OK;
}

// Write
HRESULT COpcDaTypeDictionary::Write(
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

    // validate item and property ids.
    HRESULT hResult = ValidatePropertyID(pCache, cItemID, dwPropertyID, OPC_WRITEABLE);

    if (FAILED(hResult))
    {
        return hResult;
    }

    // no writing supported at this time.
    return OPC_E_BADRIGHTS;
}

// PrepareAddItem
bool COpcDaTypeDictionary::PrepareAddItem(IOpcDaCache* pCache, const COpcString& cItemID)
{
    return false;
}

// CommitAddItems
void COpcDaTypeDictionary::CommitAddItems(IOpcDaCache* pCache)
{
}

// ValidatePropertyID
HRESULT COpcDaTypeDictionary::ValidatePropertyID(
    IOpcDaCache* pCache,
    const COpcString& cItemID, 
    DWORD             dwPropertyID, 
    int               iAccessRequired
)
{
    // check if it is a valid item.
    if (!IsKnownItem(pCache, cItemID))
    {
        return OPC_E_INVALIDITEMID;
    }
        
    // validate item id.
    COpcString cTypeXPath;

    if (cItemID != m_cItemID)
    {
        COpcString cTypeName = cItemID.SubStr(m_cItemID.GetLength()+1);

        if (!m_cTypeXPaths.Lookup(cTypeName, cTypeXPath))
        {
            return OPC_E_UNKNOWNITEMID;
        }
    }

    // no type description properties are writable.
    if (iAccessRequired != OPC_READABLE)
    {
        return OPC_E_BADRIGHTS;
    }

    // switch on the requested property.
    switch (dwPropertyID)
    {
        case NULL:
        case OPC_PROPERTY_VALUE:
        case OPC_PROPERTY_DATATYPE:           
        case OPC_PROPERTY_QUALITY:
        case OPC_PROPERTY_TIMESTAMP:
        case OPC_PROPERTY_ACCESS_RIGHTS:
        case OPC_PROPERTY_SCAN_RATE: 
        case OPC_PROPERTY_EU_TYPE: 
        case OPC_PROPERTY_EU_INFO:
        {
            break;
        }

        case OPC_PROPERTY_DICTIONARY:
        {        
            if (!cTypeXPath.IsEmpty())
            {
                return OPC_E_INVALID_PID;
            }

            break;
        }
    
        case OPC_PROPERTY_TYPE_DESCRIPTION:
        {    
            if (cTypeXPath.IsEmpty())
            {
                return OPC_E_INVALID_PID;
            }

            break;
        }

        default:
        {
            return OPC_E_INVALID_PID;
        }
    }

    return S_OK;
}

// GetValue
HRESULT COpcDaTypeDictionary::GetValue(
    IOpcDaCache* pCache,
    const COpcString& cItemID, 
    DWORD             dwPropertyID,
    VARIANT&          cValue
)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    HRESULT hResult = ValidatePropertyID(pCache, cItemID, dwPropertyID, OPC_READABLE);

    if (FAILED(hResult))
    {
        return hResult;
    }

    // extract type id.
    COpcString cTypeName;
    COpcString cTypeXPath;

    if (cItemID != m_cItemID)
    {
        cTypeName = cItemID.SubStr(m_cItemID.GetLength()+1);

        if (!m_cTypeXPaths.Lookup(cTypeName, cTypeXPath))
        {
            return OPC_E_UNKNOWNITEMID;
        }
    }

    // switch on the requested property.
    switch (dwPropertyID)
    {
        case OPC_PROPERTY_DATATYPE:      { OpcWriteVariant(cValue, (short)VT_BSTR);          break; }
        case OPC_PROPERTY_QUALITY:       { OpcWriteVariant(cValue, (short)OPC_QUALITY_GOOD); break; }
        case OPC_PROPERTY_TIMESTAMP:     { OpcWriteVariant(cValue, OpcUtcNow());             break; }
        case OPC_PROPERTY_ACCESS_RIGHTS: { OpcWriteVariant(cValue, (int)OPC_READABLE);       break; }
        case OPC_PROPERTY_SCAN_RATE:     { OpcWriteVariant(cValue, (float)60000.0);          break; }
        case OPC_PROPERTY_EU_TYPE:       { OpcWriteVariant(cValue, (int)OPC_NOENUM);         break; }
        case OPC_PROPERTY_EU_INFO:       { OpcVariantInit(&cValue);                          break; }

        case NULL:
        case OPC_PROPERTY_VALUE:
        {
            if (!cTypeXPath.IsEmpty())
            {
                OpcWriteVariant(cValue, cTypeName); 
                break;
            }
                
            OpcWriteVariant(cValue, m_cDictionaryID);
            break;
        }

        case OPC_PROPERTY_DICTIONARY:
        {        
            COpcString cXml;
            
            if (!m_cDictionary.GetXml(cXml))
            {
                return E_FAIL;
            }

            OpcWriteVariant(cValue, cXml);
            break;
        }

        case OPC_PROPERTY_TYPE_DESCRIPTION:
        {    
            // extract the XML from the document.
            COpcXmlElement cElement = m_cDictionary.FindElement(cTypeXPath);

            if (cElement == NULL)
            {
                return E_FAIL;
            }

            // construct an XML document containing only the type description.
            COpcXmlDocument cDocument;

            if (!cDocument.New(cElement))
            {
                return E_FAIL;
            }

            COpcString cXml;

            if (!cDocument.GetXml(cXml))
            {
                return E_FAIL;
            }
            
            OpcWriteVariant(cValue, cXml);
            break;
        }
    }

    return S_OK;
}

// DetectTypes
bool COpcDaTypeDictionary::DetectTypes()
{
    m_cTypeXPaths.RemoveAll();

    // select the name of the XML element that contains type descriptions.
    COpcString cPrefix;
    COpcString cElementName;

    if (m_cTypeSystemID == OPC_TYPE_SYSTEM_OPCBINARY)
    {
        cPrefix      = m_cDictionary.GetNamespacePrefix(OPCXML_NS_OPCBINARY);
        cElementName = TAG_TYPE_DESCRIPTION;
    }
    else if (m_cTypeSystemID == OPC_TYPE_SYSTEM_XMLSCHEMA)
    {
        cPrefix      = m_cDictionary.GetNamespacePrefix(OPCXML_NS_SCHEMA);
        cElementName = TAG_ELEMENT;
    }

    // generate a XPath that can be used to search for types in the document.
    COpcString cXPath;

    cXPath += _T("/*/");

    if (!cPrefix.IsEmpty())
    {
        cXPath += cPrefix;
        cXPath += _T(":");
    }

    cXPath += cElementName;

    // find all matching elements
    COpcXmlElementList cElements;

    if (m_cDictionary.FindElements(cXPath, cElements) == 0)
    {
        return false;
    }

    // guess which attribute specifies the type name.
    COpcString cAttributeName = TAG_OPCBINARY_NAME;

    if (cElements[0].GetAttribute(cAttributeName) == NULL)
    {
        cAttributeName = TAG_XMLSCHEMA_NAME;

        if (cElements[0].GetAttribute(cAttributeName) == NULL)
        {
            return false;
        }
    }

    // generate a set of type ids and type names.
    for (UINT ii = 0; ii < cElements.GetSize(); ii++)
    {
        COpcXmlAttribute cAttribute = cElements[ii].GetAttribute(cAttributeName);

        if (cAttribute == NULL)
        {
            continue;
        }

        COpcString cTypeName = cAttribute.GetValue();

        // contruct type id.
        COpcString cTypeXPath = cXPath;

        cTypeXPath += "[@";
        cTypeXPath += cAttributeName;
        cTypeXPath += "='";
        cTypeXPath += cTypeName;
        cTypeXPath += "']";

        m_cTypeXPaths[cTypeName] = cTypeXPath;
    }

    return true;
}

// LoadBinaryDictionary
bool COpcDaTypeDictionary::LoadBinaryDictionary()
{
    if (m_cTypeSystemID != OPC_TYPE_SYSTEM_OPCBINARY)
    {
        return false;
    }

    // parse the XML document.
    return m_cBinaryDictionary.Read(m_cDictionary.GetRoot());
}

// CreateXmlSchemaMapping
bool COpcDaTypeDictionary::CreateXmlSchemaMapping()
{
    COpcXmlDocument cXmlSchemaMapping;

    if (!cXmlSchemaMapping.New(_T("xsd:schema"), OPCXML_NS_SCHEMA))
    {
        return false;
    }

    COpcXmlElement cRoot = cXmlSchemaMapping.GetRoot();

    cXmlSchemaMapping.AddNamespace((LPCWSTR)NULL, m_cBinaryDictionary.Name);

    cRoot.SetAttribute(_T("targetNamespace"),  m_cBinaryDictionary.Name);
    cRoot.SetAttribute(_T("elementFormDefault"), _T("qualified"));

    OpcXml::QName ELEMENT(_T("element"), OPCXML_NS_SCHEMA);
    OpcXml::QName COMPLEX_TYPE(_T("complexType"), OPCXML_NS_SCHEMA);
    OpcXml::QName SEQUENCE(_T("sequence"), OPCXML_NS_SCHEMA);

    for (UINT ii = 0; ii < m_cBinaryDictionary.Types.GetSize(); ii++)
    {
        COpcTypeDescription* pType = m_cBinaryDictionary.Types[ii];

        // add complex type.
        COpcXmlElement cTypeElement = cRoot.AppendChild(ELEMENT);

        cTypeElement.SetAttribute("name", pType->TypeID);
        
        // add field definitions.
        cTypeElement = cTypeElement.AppendChild(COMPLEX_TYPE);
        cTypeElement = cTypeElement.AppendChild(SEQUENCE);

        for (UINT jj = 0; jj < pType->Fields.GetSize(); jj++)
        {
            COpcFieldType* pField = pType->Fields[jj];

            COpcXmlElement cFieldElement = cTypeElement.AppendChild(ELEMENT);
            
            // set element name.
            cFieldElement.SetAttribute(_T("name"), OpcBinaryGetFieldName(*pField));

            // determine xml data type.
            COpcString cType;

            switch (pField->Type)
            {
                case OPC_BINARY_INTEGER:        { cType = _T("xsd:int");           break; }
                case OPC_BINARY_INT8:           { cType = _T("xsd:byte");          break; }
                case OPC_BINARY_INT16:          { cType = _T("xsd:short");         break; }
                case OPC_BINARY_INT32:          { cType = _T("xsd:int");           break; }
                case OPC_BINARY_INT64:          { cType = _T("xsd:long");          break; }
                case OPC_BINARY_UINT8:          { cType = _T("xsd:unsignedByte");  break; }
                case OPC_BINARY_UINT16:         { cType = _T("xsd:unsignedShort"); break; }
                case OPC_BINARY_UINT32:         { cType = _T("xsd:unsignedInt");   break; }
                case OPC_BINARY_UINT64:         { cType = _T("xsd:unsignedLong");  break; }
                case OPC_BINARY_TYPE_REFERENCE: { cType = pField->TypeID;          break; }
                case OPC_BINARY_BIT_STRING:     { cType = _T("xsd:string");        break; }
                case OPC_BINARY_FLOATING_POINT: { cType = _T("xsd:decimal");       break; }
                case OPC_BINARY_SINGLE:         { cType = _T("xsd:float");         break; }
                case OPC_BINARY_DOUBLE:         { cType = _T("xsd:double");        break; }
                case OPC_BINARY_CHAR_STRING:    { cType = _T("xsd:string");        break; }
                case OPC_BINARY_ASCII:          { cType = _T("xsd:string");        break; }
                case OPC_BINARY_UNICODE:        { cType = _T("xsd:string");        break; }
                default:                        { cType = _T("xsd:string");        break; }
            }
            
            // check for an array.
            if (pField->ElementCountRefSpecified || pField->ElementCountSpecified || pField->FieldTerminatorSpecified)
            {
                COpcXmlElement cArray = cFieldElement;

                cArray = cArray.AppendChild(COMPLEX_TYPE);
                cArray = cArray.AppendChild(SEQUENCE);
                cArray = cArray.AppendChild(ELEMENT);

                COpcString cName = cType;

                int iIndex = cName.Find(_T(":"));

                if (iIndex != -1)
                {
                    cName = cName.SubStr(iIndex+1);
                }
                
                cArray.SetAttribute(_T("name"), cName);
                cArray.SetAttribute(_T("type"), cType);
                cArray.SetAttribute(_T("minOccurs"), _T("1"));
                cArray.SetAttribute(_T("maxOccurs"), _T("unbounded"));
            }

            // set scalar type.
            else
            {
                cFieldElement.SetAttribute(_T("type"), cType);
            }

            cFieldElement.SetAttribute(_T("minOccurs"), _T("1"));
            cFieldElement.SetAttribute(_T("maxOccurs"), _T("1"));
        }
    }

    // replace the dictionary.
    m_cDictionary   = cXmlSchemaMapping;
    m_cTypeSystemID = OPC_TYPE_SYSTEM_XMLSCHEMA;
    
    return true;
}

