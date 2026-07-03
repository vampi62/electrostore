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

    [MaxLength(Constants.MaxStatusLength)]
    public required string status_command_history { get; set; }

    [MaxLength(Constants.MaxNameLength)]
    public string? tracking_number { get; set; }

    public string? carrier { get; set; }
    [MaxLength(Constants.MaxTypeLength)]
    public string? sub_status { get; set; }

    [MaxLength(Constants.MaxDescriptionLength)]
    public string? tracking_event { get; set; }

    public DateTime event_at { get; set; }

    public int id_command { get; set; }
    [ForeignKey(nameof(id_command))]
    public Commands? Command { get; set; }
}
