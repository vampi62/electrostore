using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectrostoreAPI.Models;

public class ProjetsItems : BaseEntity
{
    public int id_project { get; set; }
    [ForeignKey("id_project")]
    public Projets? Projet { get; set; }

    public int id_item { get; set; }
    [ForeignKey("id_item")]
    public Items? Item { get; set; }

    public int quantity_project_item { get; set; }
}