using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using electrostore.Dto;

namespace electrostore.Models;

public class Tags
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_tag { get; set; }
    
    [MaxLength(Constants.MaxNameLength)]
    public string nom_tag { get; set; }

    public int poids_tag { get; set; } = 0;
    
    public ICollection<StoresTags> StoresTags { get; set; } = new List<StoresTags>();
    public ICollection<BoxsTags> BoxsTags { get; set; } = new List<BoxsTags>();
    public ICollection<ItemsTags> ItemsTags { get; set; } = new List<ItemsTags>();
}