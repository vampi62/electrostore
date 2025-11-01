using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace electrostore.Models;

public class StoresTags : BaseEntity
{
    public int id_tag { get; set; }
    [ForeignKey("id_tag")]
    public Tags? Tag { get; set; }

    public int id_store { get; set; }
    [ForeignKey("id_store")]
    public Stores? Store { get; set; }
}