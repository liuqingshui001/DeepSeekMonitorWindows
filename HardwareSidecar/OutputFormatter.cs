using System.Text.Json;

namespace HardwareSidecar;

public static class OutputFormatter
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static string Serialize(long timestamp, Dictionary<string, double> sensors, long sequence)
    {
        var payload = new Dictionary<string, object>
        {
            ["t"] = timestamp,
            ["s"] = sensors,
            ["i"] = sequence
        };
        return JsonSerializer.Serialize(payload, JsonOpts);
    }

    public static void WriteError(string level, string message)
    {
        var err = new Dictionary<string, string>
        {
            ["level"] = level,
            ["msg"] = message
        };
        Console.Error.WriteLine(JsonSerializer.Serialize(err, JsonOpts));
    }
}
