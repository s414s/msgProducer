// See https://aka.ms/new-console-template for more information
//https://medium.com/@simo.matijevic/produce-and-consume-messages-with-rabbitmq-and-net-core-api-9d733f93c145
using Producer.Producer;

var numberOfProducers = 10_000;
Console.WriteLine($"Starting {numberOfProducers} producers with varying rates...");

var producer = new RabbitMQProducer(numberOfProducers);
await producer.Start();

//var producer = new RabbitMQStreamProducer(numberOfProducers);
//await producer.Start();

Console.WriteLine("Press [Enter] to exit.");
Console.ReadLine();

