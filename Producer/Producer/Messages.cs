namespace Producer.Producer;

internal record AtlasUpdate
{
    public required string Imei { get; init; }
    public required string Payload { get; init; }
}
