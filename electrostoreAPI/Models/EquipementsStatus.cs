using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ElectrostoreAPI.Enums;

namespace ElectrostoreAPI.Models;

public class EquipementsStatus : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_equipement_status { get; set; }

    public int id_equipement { get; set; }
    [ForeignKey("id_equipement")]
    public Equipements? Equipement { get; set; }

    public EquipementStatus status_equipement { get; set; } = EquipementStatus.Operational;
}
