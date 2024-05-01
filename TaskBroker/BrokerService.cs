using SharedLib;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace TaskBroker
{
    
    
    public class BrokerService : IBroker
    {
        ILogger _logger { get; }
        TimeSpan _defaultTimeout = TimeSpan.FromSeconds(90);
        public BrokerService(ILogger logger)
        {
            _logger = logger;
            State = new ConcurrentDictionary<Guid, Lazy<IWorkerQueue>>();
        }
        public BrokerService(Action<string, object[]> logError, Action<string, object[]> logDebug) : this(new ActionLogger(logError, logDebug))
        {
        }
        protected ConcurrentDictionary<Guid, Lazy<IWorkerQueue>> State { get; }

        protected IWorkerQueue GetOrAdd(Guid queueId)
        {
            return State.GetOrAdd(queueId, LazyFactory).Value;
        }
        protected Lazy<IWorkerQueue> LazyFactory(Guid queueId) 
        { 
            return new Lazy<IWorkerQueue>(() => new WorkerQueue(this, queueId, _logger));
        }
        public bool EnqueueAndWait(Guid queueId, Func<bool> work)
        {
            return EnqueueAndWait(queueId, work, _defaultTimeout);
        }
        public bool EnqueueAndWait(Guid queueId, Func<bool> work, TimeSpan timeout)
        {
            return Enqueue(queueId, work, timeout)?.WaitForResult() ?? false;
        }
        public void EnqueueAndForget(Guid queueId, Func<bool> work)
        {
            EnqueueAndForget(queueId, work, _defaultTimeout);
        }
        public void EnqueueAndForget(Guid queueId, Func<bool> work, TimeSpan timeout)
        {
            Enqueue(queueId, work, timeout);
        }
        protected IWorker Enqueue(Guid queueId, Func<bool> work, TimeSpan timeout)
        {
            return GetOrAdd(queueId).Enqueue(work, timeout);
        }
        public void InitializeQueue(Guid queueId)
        {
            GetOrAdd(queueId);
        }
        public void WaitForInitialization(Guid queueId) 
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            Task.Run(() => { while (!State.ContainsKey(queueId)) { } }).Wait(10000);

            stopWatch.Stop();
            long duration = stopWatch.ElapsedMilliseconds;
            _logger.DebugFormat("BROKER queue {0} initialized after {1}", queueId, duration);
        }

        public void DeinitializeQueue(Guid queueId)
        {
            Lazy<IWorkerQueue> lazyQueue;
            if (!State.TryRemove(queueId, out lazyQueue))
            {
                _logger.DebugFormat("DeinitializeQueue {0} not needed", queueId);
                return;
            }
            lazyQueue.Value.Deinitialize();
        }

        public void CleanupStaleQueues(TimeSpan sinceLastActivity)
        {
            var oldest = DateTime.Now.Subtract(sinceLastActivity);
            State.Where(pair => pair.Value.IsValueCreated && pair.Value.Value.LastActivity < oldest).ForEach(pair => DeinitializeQueue(pair.Key));
        }
    }
}