namespace ElectrostoreAPI.Kafka.Producer;

public interface IKafkaProducerService : IDisposable
{
    public Task PublishAsync(string topic, string key, string message, CancellationToken ct = default);
    public Task<bool> IsConnectedAsync();
}