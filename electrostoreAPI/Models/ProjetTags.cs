using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using electrostore.Dto;

namespace electrostore.Models;

public class ProjetTags : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_projet_tag { get; set; }

    [MaxLength(Constants.MaxNameLength)]
    public required string nom_projet_tag { get; set; }

    public int poids_projet_tag { get; set; } = 0;

    public ICollection<ProjetsProjetTags> ProjetsProjetTags { get; set; } = new List<ProjetsProjetTags>();
}