using RabbitMQ.Stream.Client;
using RabbitMQ.Stream.Client.Reliable;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
namespace Producer.Producer;

//docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 -p 5552:5552 rabbitmq:management
// plugin activation
//docker exec rabbitmq rabbitmq-plugins enable rabbitmq_stream

public class RabbitMQStreamProducer
{
    private RabbitMQ.Stream.Client.Reliable.Producer? _producer;
    private readonly IEnumerable<(string, double, double)> _items;

    public RabbitMQStreamProducer(int numberOfProducers)
    {
        _items = Enumerable
             .Range(0, numberOfProducers)
             .Select(x => (
                DataGenerator.GenerateRandomImei(),
                DataGenerator.GenerateRandomCoord(-8.3, 3.1),
                DataGenerator.GenerateRandomCoord(36.0, 43.8)))
             .Distinct()
             .ToArray();
    }

    public async Task Start()
    {
        var config = new StreamSystemConfig()
        {
            UserName = "guest",
            Password = "guest",
            VirtualHost = "/",
            Endpoints =
            [
                new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5552), // Atention!! different port for streams
            ]
        };

        var streamSystem = await StreamSystem.Create(config);

        await streamSystem.CreateStream(new StreamSpec("hello-stream")
        {
            MaxLengthBytes = 5_000_000_000
        });

        _producer = await RabbitMQ.Stream.Client.Reliable.Producer.Create(new ProducerConfig(streamSystem, "hello-stream"));

        var random = new Random();

        try
        {
            while (true)
            {
                foreach (var p in _items)
                {
                    var msg = new AtlasUpdate
                    {
                        Imei = p.Item1,
                        Long = p.Item2,
                        Lat = p.Item3,
                        Battery = random.Next(1, 100),
                    };

                    var json = JsonSerializer.Serialize(msg);
                    var body = Encoding.UTF8.GetBytes(json);

                    await _producer.Send(new Message(body));
                    Console.WriteLine($"Producer {p.Item1} sent \t battery: {msg.Battery} \t {DateTime.Now:HH:mm:ss}");

                    //await Task.Delay(random.Next(100, 700));
                    //await Task.Delay(random.Next(1, 10));
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error {e.Message}");
        }
        finally
        {
            await _producer.Close();
            await streamSystem.Close();
        }
    }
}
