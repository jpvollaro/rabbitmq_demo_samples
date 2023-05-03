using System;
using RabbitMQ.Client;
using System.Text;
using System.IO;

namespace rabittmq_helloworld
{
    class Program
    {
        static void Main(string[] args)
        {
            Directory.SetCurrentDirectory(@"C:\Users\jvollar\work_git\jvollar-repo\EASYTEST-PROJECTS\rabbitMQ-workqueue\");

            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    // durable : messages aren't lost when rabbitMQ stops/dies
                    channel.QueueDeclare(queue: "task_queue1",
                                        durable: true,
                                        exclusive: false,
                                        autoDelete: false,
                                        arguments: null);

                    string message = String.Empty;
                    var properties = channel.CreateBasicProperties();
                    properties.Persistent = true;

                    /*
                    ** BasicQos method with the prefetchCount = 1 setting. 
                    ** This tells RabbitMQ not to give more than one message to a worker at a time. 
                    ** Or, in other words, don't dispatch a new message to a worker until it has processed 
                    ** and acknowledged the previous one. 
                    ** Instead, it will dispatch it to the next worker that is not still busy.
                    */
                    channel.BasicQos(0, 1, false);

                    do {
                        try
                        {
                            Console.WriteLine(" Enter your message X to exit.");
                            message = Console.ReadLine();
                            if (message == "test")
                            {
                                StreamReader sr = new StreamReader("TestData.txt");
                                message = sr.ReadToEnd();
                            }
                            var words = message.Split(' ');
                            foreach(string w in words)
                            {
                                var body = Encoding.UTF8.GetBytes(w);
                                channel.BasicPublish(exchange: "",
                                            routingKey: "task_queue1",
                                            basicProperties: properties,
                                            body: body);
                                Console.WriteLine(" [x] Sent {0}", w);
                            }
                        }
                        catch(Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    } while (!(message.StartsWith('X') && message.Length == 1));
                }
            }
        }
    }
}
