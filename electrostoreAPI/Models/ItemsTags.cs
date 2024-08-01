using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace electrostore.Models;

public class ItemsTags
{
    public int id_tag { get; set; }
    [ForeignKey("id_tag")]
    public Tags Tag { get; set; }

    public int id_item { get; set; }
    [ForeignKey("id_item")]
    public Items Item { get; set; }
}