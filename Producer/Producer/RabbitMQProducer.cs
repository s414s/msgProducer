using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Producer.Producer;

internal class RabbitMQProducer : IMessageProducer
{
    private readonly IConnection _connection;
    private readonly IEnumerable<(string, double, double)> _producers;
    private readonly IModel _channel;

    public RabbitMQProducer(int numberOfProducers)
    {
        _producers = Enumerable
             .Range(0, numberOfProducers)
             .Select(x => (DataGenerator.GenerateRandomImei(), DataGenerator.GenerateRandomCoord(), DataGenerator.GenerateRandomCoord()))
             .Distinct()
             .ToArray();

        Console.WriteLine($"Producer started.");

        var factory = new ConnectionFactory()
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

        try
        {
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Connection Error: {ex.Message}");
            throw;
        }
    }

    public async Task Start()
    {
        var random = new Random();

        try
        {
            while (true)
            {
                foreach (var p in _producers)
                {
                    var msg = new AtlasUpdate
                    {
                        Imei = p.Item1,
                        Long = p.Item2,
                        Lat = p.Item3,
                        Battery = random.Next(1, 100),
                    };

                    //SendMessage(msg);
                    Send(msg);
                    Console.WriteLine($"Producer {p.Item1} sent \t battery: {msg.Battery} \t: {DateTime.Now:HH:mm:ss}");
                    await Task.Delay(random.Next(100, 700));
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

    public void SendMessage<T>(T message)
    {
        using (var channel = _connection.CreateModel())
        {
            channel.QueueDeclare(queue: "atlas",
                                    durable: false,
                                    exclusive: false,
                                    autoDelete: false,
                                    arguments: null);

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            channel.BasicPublish(exchange: "",
                                    routingKey: "atlas",
                                    basicProperties: null,
                                    body: body);
        }
    }

    public void Send<T>(T message)
    {
        _channel.QueueDeclare(queue: "atlas",
                                durable: false,
                                exclusive: false,
                                autoDelete: false,
                                arguments: null);

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        _channel.BasicPublish(exchange: "",
                                routingKey: "atlas",
                                basicProperties: null,
                                body: body);

    }
}
