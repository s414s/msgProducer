namespace Producer.Producer;

internal interface IMessageProducer
{
    void SendMessage<T>(T message);
}
