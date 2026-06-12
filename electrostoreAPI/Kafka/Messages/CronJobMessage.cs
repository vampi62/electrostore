using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Kafka.Messages;

public class CronJobMessage
{
    public ReadCronJobDto data { get; set; }
    public string action { get; set; }
}