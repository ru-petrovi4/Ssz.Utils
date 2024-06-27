//============================================================================
// TITLE: COpcDaWriteThread.cpp
//
// CONTENTS:
// 
// A thread that serializes asynchronous write requests.
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
// 2003/06/20 RSA   Initial implementation.

#include "StdAfx.h"
#include "COpcDaWriteThread.h"

#include "COpcDaCache.h"

#include <msclr\auto_handle.h>

using namespace msclr;

//==============================================================================
// Local Functions

static DWORD WINAPI ThreadProc(LPVOID lpParameter)
{
    ((COpcDaWriteThread*)lpParameter)->Run();
    return 0;
}

//============================================================================
// COpcDaWriteThread

// Constructor
COpcDaWriteThread::COpcDaWriteThread()
{
     _syncRoot = gcnew LeveledLock(100);

    m_dwID   = NULL;
    m_hEvent = NULL;
}

// Destructor
COpcDaWriteThread::~COpcDaWriteThread()
{
}

// Start
bool COpcDaWriteThread::Start()
{
    {
        auto_handle<IDisposable> cLock = _syncRoot->Enter();

        // check that the thread is not already running.
        if (m_dwID != NULL)
        {
            return false;
        }

        // create event to wait for thread.
        m_hEvent = CreateEvent(
            NULL,
            TRUE,
            FALSE,
            NULL
        );

        if (m_hEvent == NULL)
        {
            return false;
        }

        // start thread.
        HANDLE hThread = CreateThread(
            NULL,
            NULL,
            ThreadProc,
            (void*)this,
            NULL,
            &m_dwID);

        if (hThread == NULL)
        {
            return false;
        }

        // close thread handle.
        CloseHandle(hThread);
    }

    // wait for update thread to start.
    WaitForSingleObject(m_hEvent, INFINITE);

    {
        auto_handle<IDisposable> cLock = _syncRoot->Enter();
        ResetEvent(m_hEvent);
    }

    return true;
}

// Stop
void COpcDaWriteThread::Stop()
{
    if (m_dwID != NULL)
    {
        {
            auto_handle<IDisposable> cLock = _syncRoot->Enter();

            // post a quit message.
            PostThreadMessage(m_dwID, WM_QUIT, NULL, NULL);

            // clear thread id to signal shutdown.
            m_dwID = NULL;
        }        

        // wait for update thread to shutdown.
        WaitForSingleObject(m_hEvent, INFINITE);

        {
            auto_handle<IDisposable> cLock = _syncRoot->Enter();
        
            CloseHandle(m_hEvent);
            m_hEvent = NULL;
        }
    }
}

// Run
void COpcDaWriteThread::Run()
{
    MSG cMsg; memset(&cMsg, 0, sizeof(MSG));

    {
        auto_handle<IDisposable> cLock = _syncRoot->Enter();

        // create the message queue.        
        PeekMessage(&cMsg, NULL, NULL, NULL, PM_NOREMOVE);

        // signal startup.
        SetEvent(m_hEvent);
    }    

    // enter the message loop.
    while (GetMessage(&cMsg, NULL, NULL, NULL))
    {
        // process message.
        if (cMsg.message == OPC_TRANSACTION_WRITE)
        {
            // process write message.
            COpcDaTransaction* pTransaction = (COpcDaTransaction*)cMsg.wParam;
            pTransaction->Process();

            // queue response to client.
            pTransaction->ChangeType(OPC_TRANSACTION_WRITE_COMPLETE);
            ::GetCache().QueueMessage(pTransaction);
        }
    }

    {
        auto_handle<IDisposable> cLock = _syncRoot->Enter();

        // signal shutdown.
        SetEvent(m_hEvent);
    }
}

// QueueTransaction
bool COpcDaWriteThread::QueueTransaction(COpcDaTransaction* pTransaction)
{
    auto_handle<IDisposable> cLock = _syncRoot->Enter();

    if (!PostThreadMessage(m_dwID, OPC_TRANSACTION_WRITE, (WPARAM)pTransaction, NULL))
    {
        return false;
    }

    return true;
}
