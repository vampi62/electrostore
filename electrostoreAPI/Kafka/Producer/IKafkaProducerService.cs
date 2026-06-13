namespace ElectrostoreAPI.Kafka.Producer;

public interface IKafkaProducerService
{
    public Task PublishAsync(string topic, string key, string message, CancellationToken ct = default);
    public Task<bool> IsConnectedAsync();
    public void Dispose();
}