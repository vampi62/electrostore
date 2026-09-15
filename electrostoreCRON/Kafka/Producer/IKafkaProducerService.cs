namespace ElectrostoreCRON.Kafka.Producer;

public interface IKafkaProducerService : IDisposable
{
    Task PublishAsync(string topic, string key, string message, CancellationToken ct = default);
}
