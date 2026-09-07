using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Models;

public class EquipementsDocuments : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_equipement_document { get; set; }

    [MaxLength(Constants.MaxUrlFileLength)]
    public required string url_equipement_document { get; set; }

    [MaxLength(Constants.MaxNameLength)]
    public required string name_equipement_document { get; set; }

    [MaxLength(Constants.MaxTypeLength)]
    public required string type_equipement_document { get; set; }

    public decimal size_equipement_document { get; set; }

    public int id_equipement { get; set; }
    [ForeignKey("id_equipement")]
    public Equipements? Equipement { get; set; }
}
