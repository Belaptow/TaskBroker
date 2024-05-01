// See https://aka.ms/new-console-template for more information
using System.Threading.Tasks;
using TaskBroker;
using SharedLib;


Console.WriteLine("Hello, World!");

var logger = new ActionLogger(Console.WriteLine, Console.WriteLine);

var limit = 10;
var broker = new BrokerService(logger);

var queueTasks = new List<Task>();
var source = new CancellationTokenSource();

var queues = new List<Guid>() { Guid.NewGuid(), Guid.NewGuid() };
queues.ForEachAsync(
    queue =>
        Enumerable.Range(0, limit).ForEachAsync(
            test =>
                broker.EnqueueAndWait(queue, () =>
                {
                    var timeout = new Random().Next(5, 15);
                    logger.DebugFormat("doin work for {0} sec.", timeout);
                    Thread.Sleep(timeout * 1000);
                    return true;
                }, TimeSpan.FromSeconds(1000))
            )
    );
Console.WriteLine("all finished");