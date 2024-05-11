using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskBroker
{
    public class WorkerQueue : IWorkerQueue
    {
        IBroker _broker;
        ILogger _logger { get; }
        public TimeSpan WaitTime { get => new TimeSpan(_jobs.Sum(qJob => qJob.Timeout.Ticks)); }
        public DateTime LastActivity { get; private set; }
        public Guid QueueId { get; }

        public int WorkerCount => _jobs.Count();

        public bool AnyWorkers => _jobs.Count() > 0;

        public WorkerQueue(IBroker broker, Guid queueId, ILogger logger) 
        {
            _logger = logger;
            _broker = broker;
            QueueId = queueId;
            //_logger.DebugFormat("QUEUE {0} CREATED", queueId);
        }
        private Queue<Worker> _jobs = new Queue<Worker>();
        private bool _delegateQueuedOrRunning = false;
        private bool _disposing = false;
        public IWorker Enqueue(Func<bool> work, TimeSpan timeout)
        {
            return Enqueue(new Worker(this, work, timeout, _logger));
        }

        protected IWorker Enqueue(Worker worker)
        {
            if (_disposing) return null;
            lock (_jobs)
            {
                _jobs.Enqueue(worker);
                _logger.DebugFormat("QUEUE {0} WORKER {1} enqueued", QueueId, worker.WorkerId);
                LastActivity = DateTime.Now;
                if (!_delegateQueuedOrRunning)
                {
                    _delegateQueuedOrRunning = true;
                    ThreadPool.UnsafeQueueUserWorkItem(ProcessQueuedItems, null);
                }
                return worker;
            }
        }

        private void ProcessQueuedItems(object ignored)
        {
            while (true)
            {
                Worker job;
                lock (_jobs)
                {
                    if (_jobs.Count == 0)
                    {
                        _delegateQueuedOrRunning = false;
                        break;
                    }
                    job = _jobs.Dequeue();
                    LastActivity = DateTime.Now; 
                    _logger.DebugFormat("QUEUE {0} WORKER {1} dequeued", QueueId, job.WorkerId);
                }
                try
                {
                    job.StartAndWait();
                }
                catch
                {
                    ThreadPool.UnsafeQueueUserWorkItem(ProcessQueuedItems, null);
                    throw;
                }
            }
        }

        public void WaitAll()
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            Task.Run(() => { while (AnyWorkers) { Task.Delay(1000).Wait(); } }).Wait(60000);

            stopWatch.Stop();
            long duration = stopWatch.ElapsedMilliseconds;
            _logger.DebugFormat("QUEUE {0} all workers finished after {1}", QueueId, duration);
        }

        public void Deinitialize()
        {
            _logger.DebugFormat("QUEUE {0} WorkerCount {1} Deinitialize...", QueueId, WorkerCount);
            _disposing = true;
            WaitAll();
            lock (_jobs)
                _jobs.Clear();
            _logger.DebugFormat("QUEUE {0} WorkerCount {1} Deinitialize SUCCESS", QueueId, WorkerCount);
        }
    }
}
