using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace ElectrostoreAPI.Models;

public class Commands : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_command { get; set; }

    public float? price_command { get; set; }

    [MaxLength(Constants.MaxUrlLength)]
    public string url_command { get; set; } = string.Empty;

    public CommandStatus status_command { get; set; } = CommandStatus.Created;

    public DateTime date_command { get; set; }

    public DateTime? date_delivery_command { get; set; }

    [MaxLength(Constants.MaxTrackingNumberLength)]
    public string tracking_number_command { get; set; } = string.Empty;

    public int id_carrier { get; set; }
    [ForeignKey("id_carrier")]
    public Carriers? Carrier { get; set; }

    public bool is_tracking_requested { get; set; } = false;
    public bool is_tracking_validated { get; set; } = false;
    public bool is_active { get; set; } = true;

    public string? shipper_address_command { get; set; }

    public T? GetShipperAddress<T>()
    {
        return string.IsNullOrEmpty(shipper_address_command)
            ? default
            : JsonSerializer.Deserialize<T>(shipper_address_command);
    }

    public string? recipient_address_command { get; set; }

    public T? GetRecipientAddress<T>()
    {
        return string.IsNullOrEmpty(recipient_address_command)
            ? default
            : JsonSerializer.Deserialize<T>(recipient_address_command);
    }

    public TrackingStatus? last_status_command { get; set; }
    public TrackingSubStatus? last_sub_status_command { get; set; }
    public string? raw_data_command { get; set; }
    public T? GetRawData<T>()
    {
        return string.IsNullOrEmpty(raw_data_command)
            ? default
            : JsonSerializer.Deserialize<T>(raw_data_command);
    }
    public ICollection<CommandsComments> CommandsComments { get; set; } = new List<CommandsComments>();
    public ICollection<CommandsDocuments> CommandsDocuments { get; set; } = new List<CommandsDocuments>();
    public ICollection<CommandsHistory> CommandsHistory { get; set; } = new List<CommandsHistory>();
    public ICollection<CommandsItems> CommandsItems { get; set; } = new List<CommandsItems>();
}