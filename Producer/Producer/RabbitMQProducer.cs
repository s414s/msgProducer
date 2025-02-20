using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace Producer.Producer;

internal class RabbitMQProducer : IMessageProducer
{
    private readonly IEnumerable<(string, double, double)> _items;
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly ConnectionFactory _factory;

    public RabbitMQProducer(int numberOfProducers)
    {
        _items = Enumerable
             .Range(0, numberOfProducers)
             .Select(x => (
                DataGenerator.GenerateRandomImei(),
                DataGenerator.GenerateRandomCoord(-8.3, 3.1),
                DataGenerator.GenerateRandomCoord(36.0, 43.8)))
             .Distinct()
             .ToArray();

        Console.WriteLine($"Producer started.");

        _factory = new ConnectionFactory()
        {
            // HostName = "rabbitmq",
            // Use the container name OR if running the app locally:
            HostName = "localhost",
            Port = 5672,
            UserName = "guest",
            Password = "guest",
            VirtualHost = "/",
            //RequestedHeartbeat = TimeSpan.FromSeconds(30),
            //AutomaticRecoveryEnabled = true,
            //NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };

        //var factory = new ConnectionFactory()
        //{
        //    Uri = new Uri("amqp://guest:guest@localhost:5672")
        //};
    }

    public async Task Start()
    {
        try
        {
            _connection = await _factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Connection Error: {ex.Message}");
            throw;
        }
        var random = new Random();

        //await _channel.ExchangeDeclareAsync(exchange: "logs", type: ExchangeType.Fanout);

        await _channel.QueueDeclareAsync(queue: "atlas",
                                durable: false,
                                exclusive: false,
                                autoDelete: false,
                                arguments: null);

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

                    await _channel.BasicPublishAsync(
                        exchange: "",
                        routingKey: "atlas",
                        mandatory: true,
                        body: body);

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
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
}
