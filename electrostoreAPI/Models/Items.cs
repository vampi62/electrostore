using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ElectrostoreAPI.Constants;

namespace ElectrostoreAPI.Models;

public class Items : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_item { get; set; }

    [MaxLength(FieldLengths.MaxUrlFileLength)]
    public string? url_picture_item { get; set; }

    [MaxLength(FieldLengths.MaxUrlFileLength)]
    public string? url_thumbnail_item { get; set; }

    [MaxLength(FieldLengths.MaxNameLength)]
    public required string reference_name_item { get; set; }

    [MaxLength(FieldLengths.MaxNameLength)]
    public required string friendly_name_item { get; set; }

    public int threshold_min_item { get; set; }

    [MaxLength(FieldLengths.MaxDescriptionLength)]
    public string description_item { get; set; } = string.Empty;

    public ICollection<CommandsItems> CommandsItems { get; set; } = new List<CommandsItems>();
    public ICollection<ItemsBoxs> ItemsBoxs { get; set; } = new List<ItemsBoxs>();
    public ICollection<ItemsDocuments> ItemsDocuments { get; set; } = new List<ItemsDocuments>();
    public ICollection<ItemsTags> ItemsTags { get; set; } = new List<ItemsTags>();
    public ICollection<ItemsHistory> ItemsHistory { get; set; } = new List<ItemsHistory>();
    public ICollection<ProjectsItems> ProjectsItems { get; set; } = new List<ProjectsItems>();
}