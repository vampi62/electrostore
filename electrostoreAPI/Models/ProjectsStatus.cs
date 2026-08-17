using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ElectrostoreAPI.Enums;

namespace ElectrostoreAPI.Models;

public class ProjectsStatus : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_project_status { get; set; }

    public int id_project { get; set; }
    [ForeignKey("id_project")]
    public Projects? Project { get; set; }

    public ProjectStatus status_project { get; set; } = ProjectStatus.NotStarted;
}