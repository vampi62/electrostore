using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Models;

public class ProjetsCommentaires : BaseEntity
{
    [Key]
    public int id_project_comment { get; set; }

    public int? id_user { get; set; }
    [ForeignKey("id_user")]
    public Users? User { get; set; }

    public int id_project { get; set; }
    [ForeignKey("id_project")]
    public Projets? Projet { get; set; }

    [MaxLength(Constants.MaxCommentLength)]
    public required string content_project_comment { get; set; }
}