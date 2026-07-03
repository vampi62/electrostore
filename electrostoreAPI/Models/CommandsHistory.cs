using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectrostoreAPI.Models;
public class CommandsHistory : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_command_history { get; set; }

    public TrackingStatus? status { get; set; }

    [MaxLength(Constants.MaxTypeLength)]
    public string? sub_status { get; set; }

    [MaxLength(Constants.MaxDescriptionLength)]
    public string? description { get; set; }

    [MaxLength(Constants.MaxLocationLength)]
    public string? location { get; set; }

    [MaxLength(Constants.MaxTypeLength)]
    public string? stage { get; set; }

    public DateTime? event_time_utc { get; set; }

    [MaxLength(Constants.MaxTimezoneLength)]
    public string? timezone { get; set; }

    [MaxLength(Constants.MaxLocationLength)]
    public string? country { get; set; }

    [MaxLength(Constants.MaxLocationLength)]
    public string? state { get; set; }

    [MaxLength(Constants.MaxLocationLength)]
    public string? city { get; set; }

    [MaxLength(Constants.MaxPostalCodeLength)]
    public string? postal_code { get; set; }

    [MaxLength(Constants.MaxCoordinateLength)]
    public string? latitude { get; set; }

    [MaxLength(Constants.MaxCoordinateLength)]
    public string? longitude { get; set; }

    public int id_command { get; set; }
    [ForeignKey("id_command")]
    public Commands? command { get; set; }
}