using ElectrostoreAPI.Validators;
using System.ComponentModel.DataAnnotations;

namespace ElectrostoreAPI.Dto;

public record ReadCronJobDto
{
    public int id_cronjob { get; init; }
    public required string name_cronjob { get; init; }
    public required string cron_expression { get; init; }
    public required string action_cronjob { get; init; }
    public string? params_cronjob { get; init; }
    public bool is_enabled { get; init; }
    public DateTime? last_run_at { get; init; }
    public DateTime? next_run_at { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}

public record CreateCronJobDto
{
    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public required string name_cronjob { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(100, ErrorMessage = "{0} cannot exceed {1} characters.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public required string cron_expression { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public required string action_cronjob { get; init; }

    public string? params_cronjob { get; init; }

    public bool is_enabled { get; init; } = true;
}

public record UpdateCronJobDto
{
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public string? name_cronjob { get; init; }

    [MaxLength(100, ErrorMessage = "{0} cannot exceed {1} characters.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public string? cron_expression { get; init; }

    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public string? action_cronjob { get; init; }

    public string? params_cronjob { get; init; }

    public bool? is_enabled { get; init; }

    public DateTime? last_run_at { get; init; }

    public DateTime? next_run_at { get; init; }
}
