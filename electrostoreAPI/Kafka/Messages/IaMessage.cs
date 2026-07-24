using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Kafka.Messages;

public class IaMessage
{
    public required string action { get; set; }
    public int id_ia { get; set; }
    public DateTime requested_at { get; set; }
    public int requested_by { get; set; }
}