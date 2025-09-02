using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using electrostore.Dto;

namespace electrostore.Models;

public class IA : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_ia { get; set; }

    [MaxLength(Constants.MaxNameLength)]
    public required string nom_ia { get; set; }

    [MaxLength(Constants.MaxDescriptionLength)]
    public required string description_ia { get; set; }

    public bool trained_ia { get; set; } = false;
}