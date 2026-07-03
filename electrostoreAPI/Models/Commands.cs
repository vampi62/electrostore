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

    public float prix_command { get; set; }

    [MaxLength(Constants.MaxUrlLength)]
    public required string url_command { get; set; }

    public CommandStatus status_command { get; set; } = CommandStatus.Created;

    public DateTime date_command { get; set; }

    public DateTime? date_livraison_command { get; set; }

    [MaxLength(Constants.MaxTrackingNumberLength)]
    public string tracking_number { get; set; } = string.Empty;

    public int id_carrier { get; set; }
    [ForeignKey("id_carrier")]
    public Carriers? Carrier { get; set; }

    public bool is_tracking_requested { get; set; } = false;
    public bool is_tracking_validated { get; set; } = false;
    public bool is_active { get; set; } = true;

    public string? shipper_adress { get; set; }

    public T? GetShipperAdress<T>()
    {
        return string.IsNullOrEmpty(shipper_adress)
            ? default
            : JsonSerializer.Deserialize<T>(shipper_adress);
    }

    public string? recipient_adress { get; set; }

    public T? GetRecipientAdress<T>()
    {
        return string.IsNullOrEmpty(recipient_adress)
            ? default
            : JsonSerializer.Deserialize<T>(recipient_adress);
    }

    public TrackingStatus? last_status { get; set; }
    public string? raw_data { get; set; }
    public T? GetRawData<T>()
    {
        return string.IsNullOrEmpty(raw_data)
            ? default
            : JsonSerializer.Deserialize<T>(raw_data);
    }
    public ICollection<CommandsCommentaires> CommandsCommentaires { get; set; } = new List<CommandsCommentaires>();
    public ICollection<CommandsDocuments> CommandsDocuments { get; set; } = new List<CommandsDocuments>();
    public ICollection<CommandsHistory> CommandsHistory { get; set; } = new List<CommandsHistory>();
    public ICollection<CommandsItems> CommandsItems { get; set; } = new List<CommandsItems>();
}