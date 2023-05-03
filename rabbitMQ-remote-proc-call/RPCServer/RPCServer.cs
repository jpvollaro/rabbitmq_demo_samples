using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class RPCServer
{
    public static void Main()
    {
        int thSleepTime;
        string sTime = Environment.GetEnvironmentVariable("sleepTime");
        if (!string.IsNullOrWhiteSpace(sTime) && int.TryParse(sTime, out thSleepTime))
        {
            Thread.Sleep(thSleepTime);
        }

        _ = Task.Run(async () =>
        {
            try
            {
                var x = await ServerCode();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception Occurred {0}", e.Message);
            }
        });
            
        while (true)
        {
            Thread.Sleep(100);
        }
    }

    public static async Task<string> ServerCode()
    { 
        var rabbitHostName = Environment.GetEnvironmentVariable("RABBIT_HOSTNAME");
        var connectionFactory = new ConnectionFactory
        {
            HostName = rabbitHostName ?? "localhost",
            Port = 5672
        };
        using (var connection = connectionFactory.CreateConnection())
        using (var channel = connection.CreateModel())
        {
            channel.QueueDeclare(queue: "rpc_queue", durable: false,  
              exclusive: false, autoDelete: false, arguments: null);
            var consumer = new EventingBasicConsumer(channel);
            channel.BasicConsume(queue: "rpc_queue",
              autoAck: false, consumer: consumer);
            channel.BasicQos(0, 1, false);

            Console.WriteLine("[x] Awaiting RPC requests {0}",DateTime.Now.ToLongTimeString());

            consumer.Received += (model, ea) =>
            {
                string response = null;

                var body = ea.Body.ToArray();
                var props = ea.BasicProperties;
                var replyProps = channel.CreateBasicProperties();
                replyProps.CorrelationId = props.CorrelationId;

                try
                {
                    var message = Encoding.UTF8.GetString(body);
                    int n = message.Length;
                    Console.WriteLine(" GOT({0}) at {1}", message, DateTime.Now.ToLongTimeString());
                    response = dashString(message);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception Occurred {0}", e.Message);
                    response = "";
                }
                finally
                {
                    var responseBytes = Encoding.UTF8.GetBytes(response);
                    channel.BasicPublish(exchange: "", routingKey: props.ReplyTo,
                      basicProperties: replyProps, body: responseBytes);
                    channel.BasicAck(deliveryTag: ea.DeliveryTag,
                      multiple: false);
                }
            };

            channel.BasicRecover(true);   //TODO this is deprecated, but it does recover messages that are lost.
            do{
                Thread.Sleep(100);
            } while (true);
        }
    }

    private static string dashString(string s)
    {
        int sum = 0;
        StringBuilder sb = new StringBuilder();

        foreach (var c in s)
        {
            sum += c;
            sb.Append(c + "-");
        }
        sb.Append("->" + sum);
        Thread.Sleep((s.Length) * 10);
        return sb.ToString();
    }
}