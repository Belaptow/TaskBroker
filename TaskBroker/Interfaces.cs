using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskBroker
{
    public interface ILogger
    {
        void DebugFormat(string format, params object[] args);
        void ErrorFormat(string format, params object[] args);
    }
    public interface IWorker
    {
        Guid WorkerId { get; }
        bool WaitForResult();
    }
    public interface IBroker
    {
        void CleanupStaleQueues(TimeSpan sinceLastActivity);
        void InitializeQueue(Guid queueId);
        void DeinitializeQueue(Guid queueId);
        bool EnqueueAndWait(Guid queueId, Func<bool> work);
        bool EnqueueAndWait(Guid queueId, Func<bool> work, TimeSpan timeout);
        void EnqueueAndForget(Guid queueId, Func<bool> work);
        void EnqueueAndForget(Guid queueId, Func<bool> work, TimeSpan timeout);
    }
    public interface IWorkerQueue
    {
        Guid QueueId { get; }
        DateTime LastActivity { get; }
        int WorkerCount { get; }
        bool AnyWorkers { get; }
        void Deinitialize();
        void WaitAll();
        IWorker Enqueue(Func<bool> work, TimeSpan timeout);
    }
}
