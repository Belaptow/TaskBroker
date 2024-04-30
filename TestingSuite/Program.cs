// See https://aka.ms/new-console-template for more information
using System.Threading.Tasks;
using TaskBroker;

Console.WriteLine("Hello, World!");

var logger = new ActionLogger(Console.WriteLine, Console.WriteLine);

var limit = 10;
var broker = new BrokerService(logger);

var queueTasks = new List<Task>();
var source = new CancellationTokenSource();

var queues = new List<Guid>() { Guid.NewGuid() };

foreach (var queue in queues)
{
    queueTasks.Add(new Task(() => {
        var tasks = new List<Task>();
        var testList = Enumerable.Range(0, limit);
        foreach (var test in testList)
            tasks.Add(new Task(() =>
            {
                broker.EnqueueAndWait(queue, () =>
                {
                    var timeout = new Random().Next(5, 15);
                    logger.DebugFormat("doin work for {0} sec.", timeout);
                    Thread.Sleep(timeout * 1000);
                    return true;
                }, TimeSpan.FromSeconds(1000));
            }
            ));
        foreach (var task in tasks.Where(qTask => qTask.Status == TaskStatus.Created))
            task.Start();
        Task.WaitAll(tasks.ToArray());
    }));
    
}


foreach (var task in queueTasks)
    task.Start();
//Thread.Sleep(10000);
//source.Cancel();
Task.WaitAll(queueTasks.ToArray());
Console.WriteLine("all finished");