using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace ElectrostoreAPI.Models;

public class CronJobs : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_cronjob { get; set; }

    [MaxLength(Constants.MaxNameLength)]
    public required string name_cronjob { get; set; }

    [MaxLength(Constants.MaxCronExpressionLength)]
    public required string cron_expression { get; set; }

    public CronJobAction action_cronjob { get; set; }

    public string? params_cronjob { get; set; }

    public bool is_enabled { get; set; } = true;

    public DateTime? last_run_at { get; set; }

    public DateTime? next_run_at { get; set; }
}
