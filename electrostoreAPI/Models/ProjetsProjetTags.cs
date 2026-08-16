using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectrostoreAPI.Models;

public class ProjetsProjetTags : BaseEntity
{
    public int id_project_tag { get; set; }
    [ForeignKey("id_project_tag")]
    public ProjetTags? ProjetTag { get; set; }

    public int id_project { get; set; }
    [ForeignKey("id_project")]
    public Projets? Projet { get; set; }
}