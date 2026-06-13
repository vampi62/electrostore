using Confluent.Kafka;

namespace ElectrostoreAPI.Kafka.Producer;

public class KafkaProducerService : IDisposable, IKafkaProducerService
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducerService> _logger;
    private readonly string _bootstrapServers;

    public KafkaProducerService(IConfiguration configuration, ILogger<KafkaProducerService> logger)
    {
        _logger = logger;
        _bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "kafka:9092";
        var config = new ProducerConfig { BootstrapServers = _bootstrapServers };
        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync(string topic, string key, string message, CancellationToken ct = default)
    {
        try
        {
            var dr = await _producer.ProduceAsync(topic, new Message<string, string> { Key = key, Value = message }, ct);
            _logger.LogDebug("Message published to {Topic} partition {P} offset {O}", topic, dr.Partition, dr.Offset);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Failed to publish message to {Topic}", topic);
            throw;
        }
    }

    public Task<bool> IsConnectedAsync()
    {
        try
        {
            using var adminClient = new AdminClientBuilder(new AdminClientConfig 
            { 
                BootstrapServers = _bootstrapServers,
                SocketTimeoutMs = 5000
            }).Build();
            
            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(5));
            return Task.FromResult(metadata.Brokers.Count > 0);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Kafka connection check failed");
            return Task.FromResult(false);
        }
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}
