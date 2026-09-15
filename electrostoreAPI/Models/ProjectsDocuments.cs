using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ElectrostoreAPI.Constants;

namespace ElectrostoreAPI.Models;

public class ProjectsDocuments : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_project_document { get; set; }

    [MaxLength(FieldLengths.MaxUrlFileLength)]
    public required string url_project_document { get; set; }

    [MaxLength(FieldLengths.MaxNameLength)]
    public required string name_project_document { get; set; }

    [MaxLength(FieldLengths.MaxTypeLength)]
    public required string type_project_document { get; set; }

    public decimal size_project_document { get; set; }

    public int id_project { get; set; }

    [ForeignKey("id_project")]
    public Projects? Project { get; set; }
}