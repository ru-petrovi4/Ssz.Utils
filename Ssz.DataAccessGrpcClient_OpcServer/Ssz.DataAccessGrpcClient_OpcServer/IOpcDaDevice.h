//============================================================================
// TITLE: IOpcDaDevice.h
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

#ifndef _IOpcDaDevice_H_
#define _IOpcDaDevice_H_

#if _MSC_VER >= 1000
#pragma once
#endif // _MSC_VER >= 1000

#include "COpcDaProperty.h"
#include "IOpcDaCache.h"

//============================================================================
// INTERFACE: IOpcDaDevice
// PURPOSE:   Abstract interface to basic device I/O functions.

interface IOpcDaDevice
{
	// BuildAddressSpace
	virtual bool BuildAddressSpace(IOpcDaCache* pCache) = 0;

	// ClearAddressSpace
	virtual void ClearAddressSpace(IOpcDaCache* pCache) = 0;

	// IsKnownItem
	virtual bool IsKnownItem(IOpcDaCache* pCache, const COpcString& cItemID) = 0;

	// GetAvailableProperties
    virtual HRESULT GetAvailableProperties(
        IOpcDaCache* pCache,
        const COpcString&   cItemID,
        uint                hItemHandle,
		bool                bReturnValues,
		COpcDaPropertyList& cProperties) = 0;

	// GetAvailableProperties
    virtual HRESULT GetAvailableProperties(
        IOpcDaCache* pCache,
        const COpcString&      cItemID, 
        uint                   hItemHandle,
		const COpcList<DWORD>& cIDs,
		bool                   bReturnValues,
		COpcDaPropertyList&    cProperties) = 0;

	// Read
	virtual HRESULT Read(
        IOpcDaCache* pCache,
        const COpcString& cItemID, 
        uint              hItemHandle,
        DWORD             dwPropertyID,
        VARIANT&          cValue, 
        FILETIME*         pftTimestamp = NULL,
        WORD*             pwQuality = NULL) = 0;

	// Write
	virtual HRESULT Write(
        IOpcDaCache* pCache,
        const COpcString& cItemID,
        uint              hItemHandle,
        DWORD             dwPropertyID,
        const VARIANT&    cValue, 
        FILETIME*         pftTimestamp = NULL,
        WORD*             pwQuality = NULL) = 0;

    // PrepareAddItem
	virtual bool PrepareAddItem(IOpcDaCache* pCache, const COpcString& cItemID) = 0;

    // CommitAddItems
	virtual void CommitAddItems(IOpcDaCache* pCache) = 0;
};

//============================================================================
// FUNCTION: GetDevice
// PURPOSE:  Returns a pointer (must never be null) to the primary device.

extern IOpcDaDevice* GetDevice(const COpcString& cItemID);

#endif // _IOpcDaDevice_H_