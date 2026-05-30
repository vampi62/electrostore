namespace ElectrostoreCRON.Kafka.Producer;

public interface IKafkaProducerService
{
    Task PublishAsync(string topic, string key, string message, CancellationToken ct = default);
    void Dispose();
}
