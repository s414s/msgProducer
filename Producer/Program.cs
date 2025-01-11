// See https://aka.ms/new-console-template for more information
//https://medium.com/@simo.matijevic/produce-and-consume-messages-with-rabbitmq-and-net-core-api-9d733f93c145

using Producer;
using Producer.Producer;

var producerCount = 100;
Console.WriteLine($"Starting {producerCount} producers with varying rates...");

var producerIds = Enumerable
    .Range(0, producerCount)
    .Select(x => DataGenerator.GenerateRandomImei())
    .Distinct()
    .ToArray();

var producer = new RabbitMQProducer(producerIds);
await producer.Start();

Console.WriteLine("Press [Enter] to exit.");
Console.ReadLine();

