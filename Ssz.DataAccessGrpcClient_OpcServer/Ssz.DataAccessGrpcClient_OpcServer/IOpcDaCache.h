//============================================================================
// TITLE: IOpcDaCache.h
//
// CONTENTS:
// 
// The interface to a device object.
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
// 2002/09/20 RSA   First release.

#ifndef _IOpcDaCache_H_
#define _IOpcDaCache_H_

#if _MSC_VER >= 1000
#pragma once
#endif // _MSC_VER >= 1000

class COpcDaTypeDictionary;

//============================================================================
// INTERFACE: IOpcDaCache
// PURPOSE:   Abstract interface to cache.

interface IOpcDaCache
{
    //=========================================================================
    // Item Management

    // AddItem
    virtual bool AddItem(const COpcString& cItemID, uint hItemHandle) = 0;
    
    // RemoveItem
    virtual bool RemoveItem(const COpcString& cItemID) = 0;    

    // AddItemAndLink
    virtual bool AddItemAndLink(const COpcString& cBrowsePath, uint hItemHandle) = 0;    
    
    // RemoveItemAndLink
    virtual bool RemoveItemAndLink(const COpcString& cBrowsePath) = 0;    

    // AddLink
    virtual bool AddLink(const COpcString& cBrowsePath) = 0;

    // AddLink
    virtual bool AddLink(const COpcString& cBrowsePath, const COpcString& cItemID) = 0;
    
    // RemoveLink
    virtual bool RemoveLink(const COpcString& cBrowsePath) = 0;
    
    // RemoveEmptyLink
    virtual bool RemoveEmptyLink(const COpcString& cBrowsePath) = 0;

    //========================================================================
    // Complex Data
    
    // creates a type dictionary (if it does not already exist) from the specified file.
    virtual COpcDaTypeDictionary* CreateTypeDictionary(const COpcString& cFileName) = 0;

    // gets the type dictionary referenced by the item id.
    virtual COpcDaTypeDictionary* GetTypeDictionary(const COpcString& cItemID) = 0;    
    
    // creates an XML schema mapping (if it does not already exist) from the specified file.
    virtual COpcDaTypeDictionary* CreateXmlSchemaMapping(const COpcString& cFileName) = 0;
};

#endif // _IOpcDaCache_H_