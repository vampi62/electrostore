using System.Reflection;
using Confluent.Kafka;
using ElectrostoreWORKER.Grpc;
using ElectrostoreWORKER.Kafka.Consumers;
using ElectrostoreWORKER.Kafka.Messages;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Metadata = Grpc.Core.Metadata;

namespace ElectrostoreWORKER.Tests.Kafka.Consumers;

public class KafkaItemVendorPriceResultConsumerTests
{
    private readonly Mock<ItemVendorPricingGrpc.ItemVendorPricingGrpcClient> _itemVendorPricingClient = new();
    private readonly Mock<ILogger<KafkaItemVendorPriceResultConsumer>> _logger = new();

    private KafkaItemVendorPriceResultConsumer CreateConsumer()
    {
        var configuration = new ConfigurationBuilder().Build();
        return new KafkaItemVendorPriceResultConsumer(_itemVendorPricingClient.Object, configuration, _logger.Object);
    }

    private static AsyncUnaryCall<TResponse> CreateAsyncUnaryCall<TResponse>(TResponse response)
    {
        return new AsyncUnaryCall<TResponse>(
            Task.FromResult(response),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });
    }

    private static Task<bool> DispatchAsync(KafkaItemVendorPriceResultConsumer consumer, ItemVendorPriceResultMessage message, CancellationToken ct = default)
    {
        var method = typeof(KafkaItemVendorPriceResultConsumer).GetMethod("DispatchAsync", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("DispatchAsync method not found");
        return (Task<bool>)method.Invoke(consumer, new object[] { message, ct })!;
    }

    private static ConsumeResult<string, string> CreateConsumeResult(string? value, bool isPartitionEOF = false, long offset = 1)
    {
        return new ConsumeResult<string, string>
        {
            Message = value is null ? null : new Message<string, string> { Value = value },
            IsPartitionEOF = isPartitionEOF,
            Offset = offset
        };
    }

    private static Task<ConsumeResult<string, string>?> ConsumeMessageAsync(KafkaItemVendorPriceResultConsumer consumer, IConsumer<string, string> kafkaConsumer, CancellationToken ct = default)
    {
        var method = typeof(KafkaItemVendorPriceResultConsumer).GetMethod("ConsumeMessageAsync", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("ConsumeMessageAsync method not found");
        return (Task<ConsumeResult<string, string>?>)method.Invoke(consumer, new object[] { kafkaConsumer, ct })!;
    }

    private static Task ProcessMessageAsync(KafkaItemVendorPriceResultConsumer consumer, IConsumer<string, string> kafkaConsumer, ConsumeResult<string, string> result, CancellationToken ct = default)
    {
        var method = typeof(KafkaItemVendorPriceResultConsumer).GetMethod("ProcessMessageAsync", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("ProcessMessageAsync method not found");
        return (Task)method.Invoke(consumer, new object[] { kafkaConsumer, result, ct })!;
    }

    private static ItemVendorPriceResultMessage? DeserializeMessage(KafkaItemVendorPriceResultConsumer consumer, ConsumeResult<string, string> result)
    {
        var method = typeof(KafkaItemVendorPriceResultConsumer).GetMethod("DeserializeMessage", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("DeserializeMessage method not found");
        return (ItemVendorPriceResultMessage?)method.Invoke(consumer, new object[] { result });
    }

    // ---- DispatchAsync ----

    [Fact]
    public async Task DispatchAsync_ShouldReturnTrue_WhenApiRecordsSuccessfully()
    {
        // Arrange
        var consumer = CreateConsumer();
        var message = new ItemVendorPriceResultMessage
        {
            id_item_vendor = 3,
            price_item_vendor_price = 0.47f,
            currency_item_vendor_price = "USD",
            quantity_item_vendor_price = 1,
        };
        var reply = new RecordItemVendorPricesReply();
        reply.Results.Add(new ItemVendorPriceResult { IdItemVendor = 3, Success = true });
        _itemVendorPricingClient
            .Setup(c => c.RecordItemVendorPricesAsync(It.IsAny<RecordItemVendorPricesRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncUnaryCall(reply));

        // Act
        var dispatched = await DispatchAsync(consumer, message);

        // Assert
        Assert.True(dispatched);
        _itemVendorPricingClient.Verify(c => c.RecordItemVendorPricesAsync(
            It.Is<RecordItemVendorPricesRequest>(r =>
                r.Observations.Count == 1 &&
                r.Observations[0].IdItemVendor == 3 &&
                r.Observations[0].CurrencyItemVendorPrice == "USD"),
            It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_ShouldReturnFalse_WhenApiRejects()
    {
        // Arrange
        var consumer = CreateConsumer();
        var message = new ItemVendorPriceResultMessage
        {
            id_item_vendor = 9,
            price_item_vendor_price = 1.23f,
            currency_item_vendor_price = "EUR",
            quantity_item_vendor_price = 1,
        };
        var reply = new RecordItemVendorPricesReply();
        reply.Results.Add(new ItemVendorPriceResult { IdItemVendor = 9, Success = false, ErrorMessage = "not found" });
        _itemVendorPricingClient
            .Setup(c => c.RecordItemVendorPricesAsync(It.IsAny<RecordItemVendorPricesRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncUnaryCall(reply));

        // Act
        var dispatched = await DispatchAsync(consumer, message);

        // Assert
        Assert.False(dispatched);
    }

    // ---- DeserializeMessage ----

    [Fact]
    public void DeserializeMessage_ShouldReturnMessage_WhenJsonIsValid()
    {
        // Arrange
        var consumer = CreateConsumer();
        var result = CreateConsumeResult("""{"id_item_vendor":3,"price_item_vendor_price":0.47,"currency_item_vendor_price":"USD","quantity_item_vendor_price":1}""");

        // Act
        var msg = DeserializeMessage(consumer, result);

        // Assert
        Assert.NotNull(msg);
        Assert.Equal(3, msg!.id_item_vendor);
    }

    [Fact]
    public void DeserializeMessage_ShouldReturnNull_WhenJsonIsInvalid()
    {
        // Arrange
        var consumer = CreateConsumer();
        var result = CreateConsumeResult("{not-json");

        // Act
        var msg = DeserializeMessage(consumer, result);

        // Assert
        Assert.Null(msg);
    }

    // ---- ConsumeMessageAsync ----

    [Fact]
    public async Task ConsumeMessageAsync_ShouldReturnResult_WhenConsumeSucceeds()
    {
        // Arrange
        var consumer = CreateConsumer();
        var expected = CreateConsumeResult("""{"id_item_vendor":1,"price_item_vendor_price":1,"currency_item_vendor_price":"USD","quantity_item_vendor_price":1}""");
        var kafkaConsumer = new Mock<IConsumer<string, string>>();
        kafkaConsumer.Setup(c => c.Consume(It.IsAny<CancellationToken>())).Returns(expected);

        // Act
        var result = await ConsumeMessageAsync(consumer, kafkaConsumer.Object);

        // Assert
        Assert.Same(expected, result);
    }

    [Fact]
    public async Task ConsumeMessageAsync_ShouldReturnNull_WhenOperationIsCancelled()
    {
        // Arrange
        var consumer = CreateConsumer();
        var kafkaConsumer = new Mock<IConsumer<string, string>>();
        kafkaConsumer.Setup(c => c.Consume(It.IsAny<CancellationToken>())).Throws<OperationCanceledException>();

        // Act
        var result = await ConsumeMessageAsync(consumer, kafkaConsumer.Object);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ConsumeMessageAsync_ShouldReturnNull_WhenConsumeExceptionIsThrown()
    {
        // Arrange
        var consumer = CreateConsumer();
        var kafkaConsumer = new Mock<IConsumer<string, string>>();
        kafkaConsumer
            .Setup(c => c.Consume(It.IsAny<CancellationToken>()))
            .Throws(new ConsumeException(new ConsumeResult<byte[], byte[]>(), new Error(ErrorCode.UnknownTopicOrPart)));

        // Act
        var result = await ConsumeMessageAsync(consumer, kafkaConsumer.Object);

        // Assert
        Assert.Null(result);
    }

    // ---- ProcessMessageAsync ----

    [Fact]
    public async Task ProcessMessageAsync_ShouldDoNothing_WhenResultIsPartitionEOF()
    {
        // Arrange
        var consumer = CreateConsumer();
        var kafkaConsumer = new Mock<IConsumer<string, string>>();
        var result = CreateConsumeResult(null, isPartitionEOF: true);

        // Act
        await ProcessMessageAsync(consumer, kafkaConsumer.Object, result);

        // Assert
        kafkaConsumer.Verify(c => c.Commit(It.IsAny<ConsumeResult<string, string>>()), Times.Never);
        _itemVendorPricingClient.Verify(c => c.RecordItemVendorPricesAsync(It.IsAny<RecordItemVendorPricesRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessMessageAsync_ShouldCommitWithoutDispatching_WhenJsonIsInvalid()
    {
        // Arrange
        var consumer = CreateConsumer();
        var kafkaConsumer = new Mock<IConsumer<string, string>>();
        var result = CreateConsumeResult("{not-json");

        // Act
        await ProcessMessageAsync(consumer, kafkaConsumer.Object, result);

        // Assert
        kafkaConsumer.Verify(c => c.Commit(result), Times.Once);
        _itemVendorPricingClient.Verify(c => c.RecordItemVendorPricesAsync(It.IsAny<RecordItemVendorPricesRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessMessageAsync_ShouldDispatchAndCommit_WhenMessageIsValid()
    {
        // Arrange
        var consumer = CreateConsumer();
        var kafkaConsumer = new Mock<IConsumer<string, string>>();
        var result = CreateConsumeResult("""{"id_item_vendor":5,"price_item_vendor_price":2.5,"currency_item_vendor_price":"USD","quantity_item_vendor_price":1}""");
        var reply = new RecordItemVendorPricesReply();
        reply.Results.Add(new ItemVendorPriceResult { IdItemVendor = 5, Success = true });
        _itemVendorPricingClient
            .Setup(c => c.RecordItemVendorPricesAsync(It.IsAny<RecordItemVendorPricesRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncUnaryCall(reply));

        // Act
        await ProcessMessageAsync(consumer, kafkaConsumer.Object, result);

        // Assert
        _itemVendorPricingClient.Verify(c => c.RecordItemVendorPricesAsync(It.Is<RecordItemVendorPricesRequest>(r => r.Observations[0].IdItemVendor == 5), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once);
        kafkaConsumer.Verify(c => c.Commit(result), Times.Once);
    }

    [Fact]
    public async Task ProcessMessageAsync_ShouldNotCommit_WhenApiRejects()
    {
        // Arrange
        var consumer = CreateConsumer();
        var kafkaConsumer = new Mock<IConsumer<string, string>>();
        var result = CreateConsumeResult("""{"id_item_vendor":6,"price_item_vendor_price":2.5,"currency_item_vendor_price":"USD","quantity_item_vendor_price":1}""");
        var reply = new RecordItemVendorPricesReply();
        reply.Results.Add(new ItemVendorPriceResult { IdItemVendor = 6, Success = false, ErrorMessage = "not found" });
        _itemVendorPricingClient
            .Setup(c => c.RecordItemVendorPricesAsync(It.IsAny<RecordItemVendorPricesRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncUnaryCall(reply));

        // Act
        await ProcessMessageAsync(consumer, kafkaConsumer.Object, result);

        // Assert
        kafkaConsumer.Verify(c => c.Commit(It.IsAny<ConsumeResult<string, string>>()), Times.Never);
    }

    [Fact]
    public async Task ProcessMessageAsync_ShouldNotCommitOrThrow_WhenDispatchThrows()
    {
        // Arrange
        var consumer = CreateConsumer();
        var kafkaConsumer = new Mock<IConsumer<string, string>>();
        var result = CreateConsumeResult("""{"id_item_vendor":7,"price_item_vendor_price":2.5,"currency_item_vendor_price":"USD","quantity_item_vendor_price":1}""");
        _itemVendorPricingClient
            .Setup(c => c.RecordItemVendorPricesAsync(It.IsAny<RecordItemVendorPricesRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Throws(new RpcException(new Status(StatusCode.Unavailable, "down")));

        // Act
        var exception = await Record.ExceptionAsync(() => ProcessMessageAsync(consumer, kafkaConsumer.Object, result));

        // Assert
        Assert.Null(exception);
        kafkaConsumer.Verify(c => c.Commit(It.IsAny<ConsumeResult<string, string>>()), Times.Never);
    }
}
