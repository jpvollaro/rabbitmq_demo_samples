  
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Threading;

class Receive
{
    public static void Main()
    {
        int letterCount = 0;
        int messageCount = 0;
        var factory = new ConnectionFactory() { HostName = "localhost" };
        using(var connection = factory.CreateConnection())
        using(var channel = connection.CreateModel())
        {
            // durable : messages aren't lost when rabbitMQ stops/dies
            channel.QueueDeclare(queue: "task_queue1", 
                                durable: true, 
                                exclusive: false, 
                                autoDelete: false, 
                                arguments: null);
    
            /*
            ** BasicQos method with the prefetchCount = 1 setting. 
            ** This tells RabbitMQ not to give more than one message to a worker at a time. 
            ** Or, in other words, don't dispatch a new message to a worker until it has processed 
            ** and acknowledged the previous one. 
            ** Instead, it will dispatch it to the next worker that is not still busy.
            */
            channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            Console.WriteLine(" [*] Waiting for messages. Press [enter] to exit.");

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
          
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine("{0}", message);
                Thread.Sleep(10 * (message.Length/2));
                channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                messageCount++;
                letterCount+=message.Length;
            };
            channel.BasicConsume(queue: "task_queue1", autoAck: false, consumer: consumer);
            Console.ReadLine();
            Console.WriteLine("message Count {0}, {1}",messageCount,letterCount);
        }
    }
}