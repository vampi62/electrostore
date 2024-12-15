using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;

namespace electrostore.Models;

public class ItemsDocuments
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_item_document { get; set; }

    [MaxLength(150)]
    public string url_item_document { get; set; }

    [MaxLength(50)]
    public string name_item_document { get; set; }

    [MaxLength(50)]
    public string type_item_document { get; set; }

    public decimal size_item_document { get; set; }

    public DateTime date_item_document { get; set; }

    public int id_item { get; set; }
    [ForeignKey("id_item")]
    public Items Item { get; set; }
}