using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ElectrostoreAPI.Enums;

namespace ElectrostoreAPI.Models;

public class ProjetsStatus : BaseEntity
{
    [Key]
    public int id_project_status { get; set; }

    public int id_project { get; set; }
    [ForeignKey("id_project")]
    public Projets? Projet { get; set; }

    public ProjetStatus status_project { get; set; } = ProjetStatus.NotStarted;
}