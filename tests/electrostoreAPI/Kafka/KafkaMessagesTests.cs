using System.Text.Json;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Kafka.Messages;
using Xunit;

namespace ElectrostoreAPI.Tests.Kafka;

public class KafkaMessagesTests
{
    [Fact]
    public void MqttUserMessage_ShouldRoundTripThroughJson()
    {
        var message = new MqttUserMessage { user = "store", old_user = "old", password = "secret", delete = true };

        var json = JsonSerializer.Serialize(message);
        var parsed = JsonSerializer.Deserialize<MqttUserMessage>(json)!;

        Assert.Contains("\"user\":\"store\"", json);
        Assert.Contains("\"old_user\":\"old\"", json);
        Assert.Equal("secret", parsed.password);
        Assert.True(parsed.delete);
    }

    [Fact]
    public void MqttUserMessage_ShouldDefaultToNullFields()
    {
        var parsed = JsonSerializer.Deserialize<MqttUserMessage>("{}")!;

        Assert.Null(parsed.user);
        Assert.Null(parsed.old_user);
        Assert.Null(parsed.password);
        Assert.Null(parsed.delete);
    }

    [Fact]
    public void NotificationMessage_ShouldDefaultToEmailType_AndRoundTrip()
    {
        var message = new NotificationMessage
        {
            RecipientEmail = "a@b.c",
            TemplateId = "account-created",
            Language = "fr",
            TemplateValues = new Dictionary<string, string> { ["k"] = "v" },
            PushData = new Dictionary<string, string> { ["url"] = "/x" }
        };

        var parsed = JsonSerializer.Deserialize<NotificationMessage>(JsonSerializer.Serialize(message))!;

        Assert.Equal(["email"], parsed.Types);
        Assert.Equal("a@b.c", parsed.RecipientEmail);
        Assert.Equal("v", parsed.TemplateValues!["k"]);
        Assert.Equal("/x", parsed.PushData!["url"]);
    }

    [Fact]
    public void CronJobMessage_ShouldCarryActionAndJob()
    {
        var message = new CronJobMessage
        {
            action = "force_run",
            data = new ReadCronJobDto { id_cronjob = 3, name_cronjob = "job", cron_expression_cronjob = "* * * * *" }
        };

        var parsed = JsonSerializer.Deserialize<CronJobMessage>(JsonSerializer.Serialize(message))!;

        Assert.Equal("force_run", parsed.action);
        Assert.Equal(3, parsed.data.id_cronjob);
    }

    [Fact]
    public void TrackingActionMessage_ShouldRoundTripThroughJson()
    {
        var message = new TrackingActionMessage { tracking_number = "TRK1", carrier = 3, carrier_old = 2, tag = "t", note = "n", auto_detect = true };

        var parsed = JsonSerializer.Deserialize<TrackingActionMessage>(JsonSerializer.Serialize(message))!;

        Assert.Equal("TRK1", parsed.tracking_number);
        Assert.Equal(3, parsed.carrier);
        Assert.Equal(2, parsed.carrier_old);
        Assert.Equal("t", parsed.tag);
        Assert.Equal("n", parsed.note);
        Assert.True(parsed.auto_detect);
    }
}
