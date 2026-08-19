using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;

namespace ElectrostoreAPI.Models;

public class ProjectsSteps : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_project_step { get; set; }

    public int id_project { get; set; }
    [ForeignKey("id_project")]
    public Projects? Project { get; set; }

    [MaxLength(Constants.MaxNameLength)]
    public required string name_project_step { get; set; }

    [MaxLength(Constants.MaxDescriptionLength)]
    public string description_project_step { get; set; } = string.Empty;

    public ProjectStepStatus status_project_step { get; set; } = ProjectStepStatus.NotStarted;

    public int order_project_step { get; set; }

    public DateTime? planned_start_project_step { get; set; }

    public DateTime? planned_end_project_step { get; set; }

    public DateTime? actual_start_project_step { get; set; }

    public DateTime? actual_end_project_step { get; set; }
}
