namespace Producer.Producer;

internal readonly struct AtlasUpdate
{
    public required string Imei { get; init; }
    public required double Long { get; init; }
    public required double Lat { get; init; }
    public required int Battery { get; init; }
}
