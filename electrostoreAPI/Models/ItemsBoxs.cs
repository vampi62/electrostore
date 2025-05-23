using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace electrostore.Models;

public class ItemsBoxs : BaseEntity
{
    public int id_box { get; set; }
    [ForeignKey("id_box")]
    public Boxs Box { get; set; }

    public int id_item { get; set; }
    [ForeignKey("id_item")]
    public Items Item { get; set; }

    public int qte_item_box { get; set; }
    
    public int seuil_max_item_item_box { get; set; }
}