using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using electrostore.Dto;
using System.Numerics;

namespace electrostore.Models;

public class ItemsDocuments : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_item_document { get; set; }

    [MaxLength(Constants.MaxUrlLength)]
    public string url_item_document { get; set; }

    [MaxLength(Constants.MaxNameLength)]
    public string name_item_document { get; set; }

    [MaxLength(Constants.MaxTypeLength)]
    public string type_item_document { get; set; }

    public decimal size_item_document { get; set; }

    public int id_item { get; set; }
    [ForeignKey("id_item")]
    public Items Item { get; set; }
}