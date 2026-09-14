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

    /// <param name="name">Thread name.</param>
    /// <param name="isBackground">
    ///     <para>A background thread is abandoned by the runtime when the process
    ///     shuts down: work in flight, including the continuation after an await, is simply
    ///     dropped.</para>
    ///     <para>A foreground thread keeps the process alive until the queue is completed and
    ///     drained, so a loop runs to its end even while the application is shutting down. Pass
    ///     false only if the owner is guaranteed to call <see cref="Dispose"/>, otherwise the
    ///     process will never exit.</para>
    /// </param>
    public SingleThreadTaskScheduler(string name, bool isBackground)
    {
        _thread = new Thread(Run)
        {            
            Name = name,
            IsBackground = isBackground,
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

    /// <summary>
    ///     Lets the thread finish the tasks already queued and then exit. Call it only after the
    ///     work scheduled here has completed: a task queued afterwards - an await continuation,
    ///     for instance - fails with TaskSchedulerException and its awaiter never completes.
    /// </summary>
    public void Dispose() => _queue.CompleteAdding();
}
