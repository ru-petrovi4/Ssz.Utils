#pragma once

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Threading;
using namespace Ssz::Utils;

#define MAX_UPDATE_RATE 200

ref class WorkingThread : Ssz::Utils::IDispatcher
{
public:
	WorkingThread(void);

	~WorkingThread(void);

	generic <typename T>
	virtual System::Threading::Tasks::Task<T>^ InvokeAsync(System::Func<System::Threading::CancellationToken, T>^ action);

	generic <typename T>
	virtual System::Threading::Tasks::Task<T>^ InvokeExAsync(System::Func<System::Threading::CancellationToken, System::Threading::Tasks::Task<T>^>^ action);

	virtual void BeginInvoke(Action<System::Threading::CancellationToken>^ action);

	virtual void BeginInvokeEx(Func<System::Threading::CancellationToken, System::Threading::Tasks::Task^>^ action);

private:
	void Run();

	System::Threading::CancellationTokenSource^ _cancellationTokenSource;

	List<Action<CancellationToken>^>^ _actionsForWorkingThreadQueue;
	List<Action<CancellationToken>^>^ _actionsForWorkingThreadQueueCopy;
	Object^ _actionsForWorkingThreadQueueSyncRoot;

	Thread^ _thread;
};