using System.Text;
using System.Text.Json;
using Confluent.Kafka;

namespace ImportService.Kafka;

public class KafkaProducer : IKafkaProducer, IDisposable
{
    private readonly IProducer<string, string> _producer;

    public KafkaProducer(IConfiguration config)
    {
        var bootstrap =
            Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS")
            ?? config["KAFKA_BOOTSTRAP_SERVERS"]
            ?? "localhost:29092";

        _producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = bootstrap,
            Acks = Acks.All
        }).Build();
    }

    public Task PublishAsync(
        string topic,
        string key,
        string payload,
        IDictionary<string, string>? headers = null,
        CancellationToken ct = default)
        => ProduceInternalAsync(topic, key, payload, headers, ct);

    public Task PublishAsync<T>(
        string topic,
        string key,
        T message,
        IDictionary<string, string>? headers = null,
        CancellationToken ct = default)
        => ProduceInternalAsync(topic, key, JsonSerializer.Serialize(message), headers, ct);

    private Task ProduceInternalAsync(
        string topic,
        string key,
        string value,
        IDictionary<string, string>? headers,
        CancellationToken ct)
    {
        var kafkaHeaders = new Headers();
        if (headers is not null)
            foreach (var (k, v) in headers)
                kafkaHeaders.Add(k, Encoding.UTF8.GetBytes(v));

        return _producer.ProduceAsync(topic, new Message<string, string>
        {
            Key = key,
            Value = value,
            Headers = kafkaHeaders
        }, ct);
    }

    public void Dispose()
    {
        _producer.Flush();
        _producer.Dispose();
    }
}