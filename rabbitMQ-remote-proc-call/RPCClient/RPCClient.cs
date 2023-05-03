using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class RpcClient
{
    private readonly IConnection connection;
    private readonly IModel channel;
    private readonly string replyQueueName;
    private readonly EventingBasicConsumer consumer;
    private readonly BlockingCollection<string> respQueue = new BlockingCollection<string>();
    private readonly IBasicProperties props;

    public RpcClient()
      { 
        var rabbitHostName = Environment.GetEnvironmentVariable("RABBIT_HOSTNAME");
        var _connectionFactory = new ConnectionFactory
        {
            HostName = rabbitHostName ?? "localhost",
            Port = 5672,
        };

        connection = _connectionFactory.CreateConnection();
        channel = connection.CreateModel();
        replyQueueName = channel.QueueDeclare().QueueName;
        consumer = new EventingBasicConsumer(channel);

        props = channel.CreateBasicProperties();
        var correlationId = Guid.NewGuid().ToString();
        props.CorrelationId = correlationId;
        props.ReplyTo = replyQueueName;

       channel.BasicConsume(
            consumer: consumer,
            queue: replyQueueName,
            autoAck: true);

        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var response = Encoding.UTF8.GetString(body);
            if (ea.BasicProperties.CorrelationId == correlationId)
            {
                respQueue.Add(response);
            }
        };
    }

    public async Task<string> Call(string message)
    { 
        Thread thread = Thread.CurrentThread;      
        var messageBytes = Encoding.UTF8.GetBytes(message);
        channel.BasicPublish(
            exchange: "",
            routingKey: "rpc_queue",
            basicProperties: props,
            body: messageBytes);

        string x="UNASSIGNED";
        if (respQueue.TryTake(out x, 30000) == true)
        {
            Console.WriteLine("Thread [{0}] Got '{1}' at {2}", thread.ManagedThreadId, x, DateTime.Now.ToLongTimeString());
        }
        else
        {
            Console.WriteLine("Thread [{0}] timed out' at {1}", thread.ManagedThreadId, DateTime.Now.ToLongTimeString());
        }
        Rpc.bag.Add(this); 
        return x;
    }

    public void Close()
    {
        connection.Close();
    }
}

public class Rpc
{
    public static ConcurrentBag<RpcClient> bag = new ConcurrentBag<RpcClient>();

    public static void Main(string[] args)
    {
        int thSleepTime;
        string sTime = Environment.GetEnvironmentVariable("sleepTime");
        if (!string.IsNullOrWhiteSpace(sTime) && int.TryParse(sTime, out thSleepTime))
        {
            Thread.Sleep(thSleepTime);
        }

        for (int i = 0; i < 10; i++)
        {
            bag.Add(new RpcClient());
        }

        _ = Task.Run(async () =>
        {
            try
            {
                var x = await ProcessFile(@"TestData.txt");
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception Occurred {0}", e.Message);
            }
        });

        while (true)
        {
            Thread.Sleep(10000);
        }
    }

    public static async Task<string> ProcessFile(string fileName)
    { 
        try
        {
            using (StreamReader sr = new StreamReader(@"TestData.txt"))
            {
                do
                {
                    string sentence = sr.ReadLine();
                    var words = sentence.Split(' ');
                    foreach (var w in words)
                    {
                        RpcClient rpcClientObj;
                        while (bag.IsEmpty)
                            Thread.Sleep(10);

                        if (bag.TryTake(out rpcClientObj))
                        {
                            _ = Task.Run(async () =>
                            {
                                Thread procthread = Thread.CurrentThread;
                                string response ="UNASSIGNED";
                                try
                                {
                                    response = await rpcClientObj.Call(w);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("Exception Occurred {0}", e.Message);
                                }
                            });
                        }
                    }
                } while (!sr.EndOfStream);
            };
        }
        catch( Exception e)
        {
            Console.WriteLine("Exception Occurred {0}", e.Message);
        }

        return ("DONE");
    }
}