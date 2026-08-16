using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;

namespace ElectrostoreAPI.Models;

public class Projects : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_project { get; set; }

    [MaxLength(Constants.MaxNameLength)]
    public required string name_project { get; set; }

    [MaxLength(Constants.MaxDescriptionLength)]
    public string description_project { get; set; } = string.Empty;

    [MaxLength(Constants.MaxUrlLength)]
    public string url_project { get; set; } = string.Empty;

    public ProjectStatus status_project { get; set; } = ProjectStatus.NotStarted;

    public ICollection<ProjectsComments> ProjectsComments { get; set; } = new List<ProjectsComments>();
    public ICollection<ProjectsDocuments> ProjectsDocuments { get; set; } = new List<ProjectsDocuments>();
    public ICollection<ProjectsItems> ProjectsItems { get; set; } = new List<ProjectsItems>();
    public ICollection<ProjectsProjectTags> ProjectsProjectTags { get; set; } = new List<ProjectsProjectTags>();
    public ICollection<ProjectsStatus> ProjectsStatus { get; set; } = new List<ProjectsStatus>();
}