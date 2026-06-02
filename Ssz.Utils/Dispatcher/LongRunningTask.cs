using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.Utils.Dispatcher
{
    public class LongRunningTask
    {
        public LongRunningTask(ILogger? logger = null)
        {
            ThreadSafeDispatcher = new(logger);
        }

        public readonly ThreadSafeDispatcher ThreadSafeDispatcher;

        public void Initialize(Func<CancellationToken, Task> LongRunning_TaskMainAsync)
        {
            _longRunning_Task_CancellationTokenSource = new();
            _longRunning_Task = LongRunning_TaskMainAsync(_longRunning_Task_CancellationTokenSource.Token);
        }

        public void Close()
        {
            _longRunning_Task_CancellationTokenSource?.Cancel();
            _longRunning_Task_CancellationTokenSource = null;
        }

        private Task? _longRunning_Task;        
        private CancellationTokenSource? _longRunning_Task_CancellationTokenSource;
    }
}
