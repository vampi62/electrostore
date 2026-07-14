using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Models;

public class IA : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_ia { get; set; }

    [MaxLength(Constants.MaxNameLength)]
    public required string nom_ia { get; set; }

    [MaxLength(Constants.MaxDescriptionLength)]
    public string description_ia { get; set; } = string.Empty;

    public bool trained_ia { get; set; } = false;

    public DateTime? date_training_ia { get; set; }
}