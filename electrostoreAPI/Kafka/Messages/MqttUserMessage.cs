namespace ElectrostoreAPI.Kafka.Messages;

public class MqttUserMessage
{
    public string? user { get; set; }
    public string? old_user { get; set; }
    public string? password { get; set; }
    public bool? delete { get; set; }
}