using Confluent.Kafka;

namespace ElectrostoreCRON.Kafka.Producer;

public class KafkaProducerService : IDisposable, IKafkaProducerService
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducerService> _logger;

    public KafkaProducerService(IConfiguration configuration, ILogger<KafkaProducerService> logger)
    {
        _logger = logger;
        var bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "kafka:9092";
        var config = new ProducerConfig { BootstrapServers = bootstrapServers };
        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync(string topic, string key, string message, CancellationToken ct = default)
    {
        try
        {
            var dr = await _producer.ProduceAsync(
                topic,
                new Message<string, string> { Key = key, Value = message },
                ct);
            _logger.LogDebug("Message published to {Topic} partition {P} offset {O}", topic, dr.Partition, dr.Offset);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(
                ex,
                "Error publishing message to {Topic} | Code: {Code} | Reason: {Reason}",
                topic, ex.Error.Code, ex.Error.Reason);
            throw new Exception($"Error publishing message to {topic}: {ex.Error.Reason}", ex);
        }
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}
