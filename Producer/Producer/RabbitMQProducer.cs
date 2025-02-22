using RabbitMQ.Client;
using RabbitMQ.Stream.Client.AMQP;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;

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
            HostName = "localhost", // Use the container name OR if running the app locally:
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

        await _channel.ExchangeDeclareAsync(exchange: "myAtlasExchange", type: ExchangeType.Direct, autoDelete: false, durable: true);

        //Using a Dead Letter Exchange(DLX)
        var args = new Dictionary<string, object?>
        {
            { "x-dead-letter-exchange", "deadLetterExchange" },
            { "x-dead-letter-routing-key", "deadLetterQueue" } // optional: if you want to override the routing key
        };

        await _channel.QueueDeclareAsync(
            queue: "atlas",
            durable: true,
            exclusive: false,
            //arguments: args,
            autoDelete: false);

        await _channel.QueueDeclareAsync(
            queue: "atlas2",
            durable: true,
            exclusive: false,
            //arguments: args,
            autoDelete: false);

        await _channel.QueueBindAsync(queue: "atlas", exchange: "myAtlasExchange", routingKey: "atlas");
        await _channel.QueueBindAsync(queue: "atlas2", exchange: "myAtlasExchange", routingKey: "atlas2");

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

                    var queueKey = random.Next(0, 2);
                    var queueName = queueKey == 0 ? "atlas" : "atlas2";

                    var props = new BasicProperties
                    {
                        DeliveryMode = DeliveryModes.Persistent,
                        MessageId = new Guid().ToString(),
                        Timestamp = new AmqpTimestamp(new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()),
                        ContentType = "application/json",
                        Priority = 0,
                        Headers = new Dictionary<string, object?>
                        {
                            { "return-queue", $"{queueName}-retry" },
                            { "deadletter-queue", $"{queueName}-deadletter" },
                        },
                    };

                    await _channel.BasicPublishAsync(
                        exchange: "myAtlasExchange",
                        routingKey: queueName,
                        mandatory: true,
                        basicProperties: props,
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
