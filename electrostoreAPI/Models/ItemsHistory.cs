using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectrostoreAPI.Models;

public class ItemsHistory : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_item_history { get; set; }

    public int? id_item { get; set; }
    [ForeignKey("id_item")]
    public Items? Item { get; set; }

    public int? id_box { get; set; }
    [ForeignKey("id_box")]
    public Boxs? Box { get; set; }

    public int? id_user { get; set; }
    [ForeignKey("id_user")]
    public Users? User { get; set; }

    public ItemHistoryType type_item_history { get; set; }

    public int? quantity_change_item_history { get; set; }

    public int? old_quantity_item_history { get; set; }

    public int? new_quantity_item_history { get; set; }

    [MaxLength(Constants.MaxDescriptionLength)]
    public string? notes_item_history { get; set; }
}
