using System.ComponentModel.DataAnnotations.Schema;

namespace ElectrostoreAPI.Models;

public class EquipementsBoxs : BaseEntity
{
    public int id_box { get; set; }
    [ForeignKey("id_box")]
    public Boxs? Box { get; set; }

    public int id_equipement { get; set; }
    [ForeignKey("id_equipement")]
    public Equipements? Equipement { get; set; }
}
