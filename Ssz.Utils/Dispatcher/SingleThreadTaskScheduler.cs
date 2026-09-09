using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.Utils;

public sealed class SingleThreadTaskScheduler : TaskScheduler, IDisposable
{
    private readonly BlockingCollection<Task> _queue = new();
    private readonly Thread _thread;

    public SingleThreadTaskScheduler(string name = "STTS")
    {
        _thread = new Thread(Run)
        {
            IsBackground = true,
            Name = name
        };
        _thread.Start();
    }

    public override int MaximumConcurrencyLevel => 1;

    protected override void QueueTask(Task task) => _queue.Add(task);

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
        // Инлайним только если мы уже на нашем потоке — иначе нарушим однопоточность.
        if (Thread.CurrentThread != _thread) return false;
        if (taskWasPreviouslyQueued && !TryDequeue(task)) return false;
        return TryExecuteTask(task);
    }

    protected override bool TryDequeue(Task task) => false; // упрощение

    protected override IEnumerable<Task> GetScheduledTasks() => _queue.ToArray();

    private void Run()
    {
        foreach (var task in _queue.GetConsumingEnumerable())
            TryExecuteTask(task);
    }

    public void Dispose() => _queue.CompleteAdding();
}
