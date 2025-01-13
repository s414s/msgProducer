namespace Producer;

internal static class DataGenerator
{
    public static string GenerateRandomImei()
    {
        var random = new Random();

        int[] imei = Enumerable
            .Range(0, 15)
            .Select(x => random.Next(0, 9))
            .ToArray();

        return string.Join("", imei);
    }

    public static double GenerateRandomCoord(double min, double max)
    {
        var random = new Random();
        return random.NextDouble() * (max - min) + min;
    }
}
