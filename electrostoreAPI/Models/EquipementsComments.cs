using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Models;

public class EquipementsComments : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_equipement_comment { get; set; }

    public int? id_user { get; set; }
    [ForeignKey("id_user")]
    public Users? User { get; set; }

    public int id_equipement { get; set; }
    [ForeignKey("id_equipement")]
    public Equipements? Equipement { get; set; }

    [MaxLength(Constants.MaxCommentLength)]
    public required string content_equipement_comment { get; set; }
}
