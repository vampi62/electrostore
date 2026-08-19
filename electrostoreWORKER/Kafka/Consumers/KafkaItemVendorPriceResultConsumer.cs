using System.Text.Json;
using Confluent.Kafka;
using ElectrostoreWORKER.Grpc;
using ElectrostoreWORKER.Kafka.Messages;

namespace ElectrostoreWORKER.Kafka.Consumers;

public class KafkaItemVendorPriceResultConsumer : BackgroundService
{
    private const string Topic = "item-vendor-price-result";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly ItemVendorPricingGrpc.ItemVendorPricingGrpcClient _itemVendorPricingClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KafkaItemVendorPriceResultConsumer> _logger;

    public KafkaItemVendorPriceResultConsumer(
        ItemVendorPricingGrpc.ItemVendorPricingGrpcClient itemVendorPricingClient,
        IConfiguration configuration,
        ILogger<KafkaItemVendorPriceResultConsumer> logger)
    {
        _itemVendorPricingClient = itemVendorPricingClient;
        _configuration           = configuration;
        _logger                  = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var bootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "kafka:9092";
        var groupId          = _configuration["Kafka:ConsumerGroupId"]  ?? "worker-service";

        var config = new ConsumerConfig
        {
            BootstrapServers    = bootstrapServers,
            GroupId             = groupId,
            AutoOffsetReset     = AutoOffsetReset.Earliest,
            EnableAutoCommit    = false,
            EnablePartitionEof  = true,
            SessionTimeoutMs    = 60_000,
            HeartbeatIntervalMs = 15_000,
        };

        using var consumer = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, e) =>
                _logger.LogError(
                    "[Kafka] Broker error | Code={Code} | Reason={Reason} | Fatal={Fatal}",
                    e.Code, e.Reason, e.IsFatal))
            .SetPartitionsAssignedHandler((_, parts) =>
                _logger.LogInformation(
                    "[Kafka] Partitions assigned → {Parts}",
                    string.Join(", ", parts.Select(p => $"{p.Topic}[{p.Partition}]"))))
            .SetPartitionsRevokedHandler((_, parts) =>
                _logger.LogWarning(
                    "[Kafka] Partitions revoked → {Parts}",
                    string.Join(", ", parts.Select(p => $"{p.Topic}[{p.Partition}]"))))
            .Build();

        consumer.Subscribe(Topic);
        _logger.LogInformation(
            "KafkaItemVendorPriceResultConsumer started (group={Group}, servers={Servers})",
            groupId, bootstrapServers);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var result = await ConsumeMessageAsync(consumer, stoppingToken);
                if (result is null)
                {
                    continue;
                }
                await ProcessMessageAsync(consumer, result, stoppingToken);
            }
        }
        finally
        {
            consumer.Close();
            _logger.LogInformation("KafkaItemVendorPriceResultConsumer stopped");
        }
    }

    private async Task<ConsumeResult<string, string>?> ConsumeMessageAsync(
        IConsumer<string, string> consumer,
        CancellationToken ct)
    {
        try
        {
            return await Task.Run(() => consumer.Consume(ct), ct);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (ConsumeException ex)
        {
            _logger.LogError(ex, "Kafka error: {Reason}", ex.Error.Reason);
            return null;
        }
    }

    private async Task ProcessMessageAsync(
        IConsumer<string, string> consumer,
        ConsumeResult<string, string> result,
        CancellationToken ct)
    {
        if (result.IsPartitionEOF || result.Message?.Value is null)
        {
            return;
        }
        var msg = DeserializeMessage(result);
        if (msg is null)
        {
            consumer.Commit(result);
            return;
        }
        var dispatched = false;
        try
        {
            dispatched = await DispatchAsync(msg, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching message for offset {Offset}", result.Offset);
        }
        if (dispatched)
        {
            consumer.Commit(result);
        }
        else
        {
            _logger.LogWarning("Message for offset {Offset} was not dispatched.", result.Offset);
        }
    }

    private ItemVendorPriceResultMessage? DeserializeMessage(ConsumeResult<string, string> result)
    {
        try
        {
            return JsonSerializer.Deserialize<ItemVendorPriceResultMessage>(result.Message.Value, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid Kafka message (JSON) - offset {Offset}", result.Offset);
            return null;
        }
    }

    private async Task<bool> DispatchAsync(ItemVendorPriceResultMessage msg, CancellationToken ct)
    {
        var request = new RecordItemVendorPricesRequest();
        request.Observations.Add(new ItemVendorPriceObservation
        {
            IdItemVendor = msg.id_item_vendor,
            PriceItemVendorPrice = msg.price_item_vendor_price,
            CurrencyItemVendorPrice = msg.currency_item_vendor_price,
            QuantityItemVendorPrice = msg.quantity_item_vendor_price,
            PriceBreaksItemVendorPrice = msg.price_breaks_item_vendor_price ?? string.Empty,
        });

        var reply = await _itemVendorPricingClient.RecordItemVendorPricesAsync(request, cancellationToken: ct);
        var itemResult = reply.Results.FirstOrDefault(r => r.IdItemVendor == msg.id_item_vendor);
        if (itemResult is { Success: true })
        {
            _logger.LogInformation(
                "ItemVendorPrice result: item_vendor={Id} price={Price} {Currency} - recorded.",
                msg.id_item_vendor, msg.price_item_vendor_price, msg.currency_item_vendor_price);
            return true;
        }
        _logger.LogWarning(
            "ItemVendorPrice result: item_vendor={Id} - gRPC record rejected ({Error}).",
            msg.id_item_vendor, itemResult?.ErrorMessage);
        return false;
    }
}
