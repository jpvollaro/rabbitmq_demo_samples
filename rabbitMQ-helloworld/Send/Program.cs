using System;
using RabbitMQ.Client;
using System.Text;

namespace rabittmq_helloworld
{
    class Program
    {
        static void Main(string[] args)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare(queue: "hello",
                                        durable: false,
                                        exclusive: false,
                                        autoDelete: false,
                                        arguments: null);

                    string message;
                    do {
                        Console.WriteLine(" Enter your message X to exit.");
                        message = Console.ReadLine();
                        var body = Encoding.UTF8.GetBytes(message);

                        channel.BasicPublish(exchange: "",
                                    routingKey: "hello",
                                    basicProperties: null,
                                    body: body);
                        Console.WriteLine(" [x] Sent {0}", message);
                    } while (!(message.StartsWith('X') && message.Length == 1));
                }
            }


        }
    }
}
