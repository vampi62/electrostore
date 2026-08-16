using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectrostoreAPI.Models;

public class ProjectsProjectTags : BaseEntity
{
    public int id_project_tag { get; set; }
    [ForeignKey("id_project_tag")]
    public ProjectTags? ProjectTag { get; set; }

    public int id_project { get; set; }
    [ForeignKey("id_project")]
    public Projects? Project { get; set; }
}