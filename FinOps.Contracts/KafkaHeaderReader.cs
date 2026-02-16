using System.Text;
using Confluent.Kafka;

namespace FinOps.Contracts;

public static class KafkaHeaderReader
{
    public static string? GetString(this Headers headers, string key)
    {
        var v = headers?.GetLastBytes(key);
        return v is null ? null : Encoding.UTF8.GetString(v);
    }

    public static Guid? GetGuid(this Headers headers, string key)
        => Guid.TryParse(headers.GetString(key), out var g) ? g : null;

    public static int? GetInt(this Headers headers, string key)
        => int.TryParse(headers.GetString(key), out var i) ? i : null;
}