namespace ImportService.Kafka;


public interface IKafkaProducer
{
    Task PublishAsync(
        string topic,
        string key,
        string payload,
        IDictionary<string, string>? headers = null,
        CancellationToken ct = default);

    Task PublishAsync<T>(
        string topic,
        string key,
        T message,
        IDictionary<string, string>? headers = null,
        CancellationToken ct = default);
}