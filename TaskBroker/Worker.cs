using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskBroker
{
    public class Worker : IWorker
    {
        public IWorkerQueue Queue { get; }
        string _logFormat;
        ILogger _logger { get; }
        public TimeSpan Timeout { get; }
        CancellationTokenSource CTS { get; }
        public Guid WorkerId { get; }
        bool _loggedCompletion = false;
        bool _loggedCancellation = false;
        Task<bool> _task { get; }
        Func<bool> _wrapper { get; }

        public Worker(IWorkerQueue queue, Func<bool> work, TimeSpan timeout, ILogger logger)
        {
            _logger = logger;
            Queue = queue;
            WorkerId = Guid.NewGuid();
            CTS = new CancellationTokenSource();
            Timeout = timeout;
            _logFormat = String.Format("QUEUE {1} WORKER {0} {{0}}", WorkerId, Queue.QueueId);

            _wrapper = () => SafeExecute(work);
            _task = new Task<bool>(() => _wrapper(), CTS.Token);
        }

        public void StartAndWait()
        {
            if (_task.IsCanceled) return;
            _task.Start();
            WaitForResult();
        }
        public bool WaitForResult()
        {
            try
            {
                //if (!WaitForStart())
                //    return false;

                if (!_task.Wait(Timeout))
                    return Cancel();
                if (_task.Result && !_loggedCompletion)
                {
                    _loggedCompletion = true;
                    _logger.DebugFormat(_logFormat, "Ran to completion");
                }
                return _task.Result;
            }
            catch (AggregateException ex)
            {
                HandleCancellation(ex);
                return false;
            }
        }
        protected bool WaitForStart()
        {
            //return true;
            var waiterCTS = new CancellationTokenSource();
            var waitToStart = Task.Run(() => { while (_task.Status == TaskStatus.Created) Thread.Sleep(10); }, waiterCTS.Token);
            if (waitToStart.Wait(TimeSpan.FromHours(1))) return true;
            
            waiterCTS.Cancel();
            _logger.ErrorFormat(_logFormat, "Couldn't start");
            return Cancel();
        }
        protected bool Cancel()
        {
            if (_task.IsCanceled) return false;
            CTS.Cancel();
            if (_loggedCancellation) return false;
            _loggedCancellation = true;
            _logger.ErrorFormat(_logFormat, "Cancelled by timeout");
            return false;
        }
        protected void HandleCancellation(AggregateException ex)
        {
            if (!(ex.InnerException is TaskCanceledException)) throw ex;
            _logger.DebugFormat(_logFormat, "Was cancelled");
        }
        protected bool SafeExecute(Func<bool> work)
        {
            try
            {
                _logger.DebugFormat(_logFormat, "Trying work");
                return work();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
        }
    }
}
