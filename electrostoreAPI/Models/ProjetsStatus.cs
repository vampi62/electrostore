using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ElectrostoreAPI.Enums;

namespace ElectrostoreAPI.Models;

public class ProjetsStatus : BaseEntity
{
    [Key]
    public int id_projet_status { get; set; }

    public int id_projet { get; set; }
    [ForeignKey("id_projet")]
    public Projets? Projet { get; set; }

    public ProjetStatus status_projet { get; set; } = ProjetStatus.NotStarted;
}