#include "StdAfx.h"
#include "WorkingThread.h"
#include "COpcDaServer.h"

#include <vcclr.h>
#include <msclr\auto_handle.h>

using namespace msclr;

WorkingThread::WorkingThread(void)
{
	_cancellationTokenSource = gcnew System::Threading::CancellationTokenSource();

	_actionsForWorkingThreadQueue =
		gcnew List<Action<CancellationToken>^>();
	_actionsForWorkingThreadQueueCopy =
		gcnew List<Action<CancellationToken>^>();
	_actionsForWorkingThreadQueueSyncRoot = gcnew Object();

	_thread = gcnew Thread(gcnew ThreadStart(this, &WorkingThread::Run));
	_thread->IsBackground = false;
	_thread->Start();
}

WorkingThread::~WorkingThread()
{
	_cancellationTokenSource->Cancel();

	_thread->Join(30000);
}

void WorkingThread::Run()
{
	CoInitializeEx(NULL, COINIT_MULTITHREADED);

	auto ct = _cancellationTokenSource->Token;
	while (!ct.WaitHandle->WaitOne(MAX_UPDATE_RATE))
	{
		Monitor::Enter(_actionsForWorkingThreadQueueSyncRoot);
		{
			_actionsForWorkingThreadQueueCopy->AddRange(_actionsForWorkingThreadQueue);
			_actionsForWorkingThreadQueue->Clear();
		}
		Monitor::Exit(_actionsForWorkingThreadQueueSyncRoot);

		{
			auto_handle<IDisposable> cLock = COpcDaServer::GetDeviceSyncRoot()->Enter();

			for each (Action<CancellationToken> ^ action in _actionsForWorkingThreadQueueCopy)
			{
				if (ct.IsCancellationRequested) return;
				try
				{
					action->Invoke(ct);
				}
				catch (...)
				{
				}
			}
		}
		_actionsForWorkingThreadQueueCopy->Clear();

		COpcDaServer::Loop(ct);
	}

	CoUninitialize();
}

generic <typename T>
System::Threading::Tasks::Task<T>^ WorkingThread::InvokeAsync(System::Func<System::Threading::CancellationToken, T>^ action)
{
	throw gcnew NotImplementedException();
}

generic <typename T>
System::Threading::Tasks::Task<T>^ WorkingThread::InvokeExAsync(System::Func<System::Threading::CancellationToken, System::Threading::Tasks::Task<T>^>^ action)
{
	throw gcnew NotImplementedException();
}

void WorkingThread::BeginInvoke(Action<CancellationToken>^ action)
{
	Monitor::Enter(_actionsForWorkingThreadQueueSyncRoot);
	{
		_actionsForWorkingThreadQueue->Add(action);
	}
	Monitor::Exit(_actionsForWorkingThreadQueueSyncRoot);
}

void WorkingThread::BeginInvokeEx(Func<System::Threading::CancellationToken, System::Threading::Tasks::Task^>^ action)
{
	/*Monitor::Enter(_actionsForWorkingThreadQueueSyncRoot);
	{
		_actionsForWorkingThreadQueue->Add(action);
	}
	Monitor::Exit(_actionsForWorkingThreadQueueSyncRoot);*/
	throw gcnew NotImplementedException();
}