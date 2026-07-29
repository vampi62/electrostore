using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Kafka.Messages;

public class CronJobMessage
{
    public required ReadCronJobDto data { get; set; }
    public required string action { get; set; }
}