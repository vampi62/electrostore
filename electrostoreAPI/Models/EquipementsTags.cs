using System.ComponentModel.DataAnnotations.Schema;

namespace ElectrostoreAPI.Models;

public class EquipementsTags : BaseEntity
{
    public int id_tag { get; set; }
    [ForeignKey("id_tag")]
    public Tags? Tag { get; set; }

    public int id_equipement { get; set; }
    [ForeignKey("id_equipement")]
    public Equipements? Equipement { get; set; }
}
