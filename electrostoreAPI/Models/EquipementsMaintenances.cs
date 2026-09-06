using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;

namespace ElectrostoreAPI.Models;

public class EquipementsMaintenances : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_equipement_maintenance { get; set; }

    public int id_equipement { get; set; }
    [ForeignKey("id_equipement")]
    public Equipements? Equipement { get; set; }

    public int? id_user { get; set; }
    [ForeignKey("id_user")]
    public Users? User { get; set; }

    public EquipementMaintenanceType type_equipement_maintenance { get; set; }

    public DateTime date_planned_equipement_maintenance { get; set; }

    public DateTime? date_done_equipement_maintenance { get; set; }

    [MaxLength(Constants.MaxDescriptionLength)]
    public string description_equipement_maintenance { get; set; } = string.Empty;
}
