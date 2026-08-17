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

    public TrackingStatus? status_command_history { get; set; }

    [MaxLength(Constants.MaxTypeLength)]
    public TrackingSubStatus? sub_status_command_history { get; set; }

    [MaxLength(Constants.MaxDescriptionLength)]
    public string? description_command_history { get; set; }

    [MaxLength(Constants.MaxLocationLength)]
    public string? location_command_history { get; set; }

    [MaxLength(Constants.MaxTypeLength)]
    public string? stage_command_history { get; set; }

    public DateTime? event_time_utc { get; set; }

    [MaxLength(Constants.MaxTimezoneLength)]
    public string? timezone_command_history { get; set; }

    [MaxLength(Constants.MaxLocationLength)]
    public string? country_command_history { get; set; }

    [MaxLength(Constants.MaxLocationLength)]
    public string? state_command_history { get; set; }

    [MaxLength(Constants.MaxLocationLength)]
    public string? city_command_history { get; set; }

    [MaxLength(Constants.MaxPostalCodeLength)]
    public string? postal_code_command_history { get; set; }

    [MaxLength(Constants.MaxCoordinateLength)]
    public string? latitude_command_history { get; set; }

    [MaxLength(Constants.MaxCoordinateLength)]
    public string? longitude_command_history { get; set; }

    public int id_command { get; set; }
    [ForeignKey("id_command")]
    public Commands? command { get; set; }
}