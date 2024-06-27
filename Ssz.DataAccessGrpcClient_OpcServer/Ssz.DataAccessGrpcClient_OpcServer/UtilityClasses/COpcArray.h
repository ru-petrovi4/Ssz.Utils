//==============================================================================
// TITLE: COpcArray.h
//
// CONTENTS:
// 
// A template indexable array class.
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
// 2003/03/22 RSA   Added GetData() method.
//

#ifndef _COpcArray_H
#define _COpcArray_H

#if _MSC_VER >= 1000
#pragma once
#endif // _MSC_VER >= 1000

#include "COpcString.h"

//==============================================================================
// CLASS:   COpcList<TYPE>
// PURPOSE: Defines a indexable array template class.

template<class TYPE>
class COpcArray 
{
    OPC_CLASS_NEW_DELETE_ARRAY()

    public:

    //==========================================================================
    // Constructor
    COpcArray(UINT uSize = 0)
    :
        m_pData(NULL),
        m_uSize(0)
    {
        SetSize(uSize);
    }

    //==========================================================================
    // Copy Constructor
    COpcArray(const COpcArray& cArray)
    :
        m_pData(NULL),
        m_uSize(0)
    {
        *this = cArray;
    }  

    //==========================================================================
    // Destructor
    ~COpcArray()
    {
        RemoveAll();
    }

    //==========================================================================
    // Assignment
    COpcArray& operator=(const COpcArray& cArray)
    {
        SetSize(cArray.m_uSize);

        for (UINT ii = 0; ii < cArray.m_uSize; ii++)
        {
            m_pData[ii] = cArray[ii];
        }

        return *this;
    }

    //==========================================================================
    // GetSize
    UINT GetSize() const
    {
        return m_uSize;
    }

    //==========================================================================
    // GetData
    TYPE* GetData() const
    {
        return m_pData;
    }

    //==========================================================================
    // SetSize
    void SetSize(UINT uNewSize)
    {
        if (uNewSize == 0)
        {
            RemoveAll();
            return;
        }

        TYPE* pData = new TYPE[uNewSize];

        for (UINT ii = 0; ii < uNewSize && ii < m_uSize; ii++)
        {
            pData[ii] = m_pData[ii];
        }

        if (m_pData != NULL)
        {
            delete [] m_pData;
        }

        m_pData = pData;
        m_uSize = uNewSize;
    }

    //==========================================================================
    // RemoveAll    
    void RemoveAll()
    {
        if (m_pData != NULL)
        {
            delete [] m_pData;
        }

        m_uSize = 0;
        m_pData = NULL;
    }

    //==========================================================================
    // operator[]    
    TYPE& operator[](UINT uIndex)
    {
        OPC_ASSERT(uIndex < m_uSize);
        return m_pData[uIndex];
    }

    const TYPE& operator[](UINT uIndex) const
    {
        OPC_ASSERT(uIndex < m_uSize);
        return m_pData[uIndex];
    }

    //==========================================================================
    // SetAtGrow
    void SetAtGrow(UINT uIndex, const TYPE& newElement)
    {
        if (uIndex+1 > m_uSize)
        {
            SetSize(uIndex+1);
        }

        m_pData[uIndex] = newElement;
    }

    //==========================================================================
    // Append
    void Append(const TYPE& newElement)
    {
        SetAtGrow(m_uSize, newElement);
    }

    //==========================================================================
    // InsertAt
    void InsertAt(UINT uIndex, const TYPE& newElement, UINT uCount = 1)
    {
        OPC_ASSERT(uIndex < m_uSize);

        UINT uNewSize = m_uSize+uCount;
        TYPE* pData = new TYPE[uNewSize];

        for (UINT ii = 0; ii < uIndex; ii++)
        {
            pData[ii] = m_pData[ii];
        }

        for (UINT ii = uIndex; ii < uCount; ii++)
        {
            pData[ii] = newElement;
        }

        for (UINT ii = uIndex+uCount; ii < uNewSize; ii++)
        {
            pData[ii] = m_pData[ii-uCount];
        }

        delete [] m_pData;
        m_pData = pData;
        m_uSize = uNewSize;
    }

    //==========================================================================
    // RemoveAt
    void RemoveAt(UINT uIndex, UINT uCount = 1)
    {
        OPC_ASSERT(uIndex < m_uSize);

        UINT uNewSize = m_uSize-uCount;
        TYPE* pData = new TYPE[uNewSize];

        for (UINT ii = 0; ii < uIndex; ii++)
        {
            pData[ii] = m_pData[ii];
        }

        for (UINT ii = uIndex+uCount; ii < m_uSize; ii++)
        {
            pData[ii-uCount] = m_pData[ii];
        }

        delete [] m_pData;
        m_pData = pData;
        m_uSize = uNewSize;
    }

private:

    TYPE* m_pData;
    UINT  m_uSize;
};

//==============================================================================
// TYPE:    COpcStringArray
// PURPOSE: An array of strings.

typedef COpcArray<COpcString> COpcStringArray;

#ifndef OPCUTILS_EXPORTS
template class OPCUTILS_API COpcArray<COpcString>;
#endif

#endif //ndef _COpcArray_H

