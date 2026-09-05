using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;

namespace ElectrostoreAPI.Models;

public class Equipements : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_equipement { get; set; }

    [MaxLength(Constants.MaxNameLength)]
    public required string reference_name_equipement { get; set; }

    [MaxLength(Constants.MaxNameLength)]
    public required string friendly_name_equipement { get; set; }

    [MaxLength(Constants.MaxDescriptionLength)]
    public string description_equipement { get; set; } = string.Empty;

    public EquipementStatus status_equipement { get; set; } = EquipementStatus.Operational;

    public ICollection<EquipementsBoxs> EquipementsBoxs { get; set; } = new List<EquipementsBoxs>();
    public ICollection<EquipementsDocuments> EquipementsDocuments { get; set; } = new List<EquipementsDocuments>();
    public ICollection<EquipementsTags> EquipementsTags { get; set; } = new List<EquipementsTags>();
    public ICollection<EquipementsStatus> EquipementsStatus { get; set; } = new List<EquipementsStatus>();
    public ICollection<EquipementsMaintenances> EquipementsMaintenances { get; set; } = new List<EquipementsMaintenances>();
    public ICollection<EquipementsComments> EquipementsComments { get; set; } = new List<EquipementsComments>();
}
