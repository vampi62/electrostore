

namespace ElectrostoreIA.Kafka.Producer;

public interface IKafkaProducerService
{
    public Task PublishAsync(string topic, string key, string message, CancellationToken ct = default);
    public void Dispose();
}