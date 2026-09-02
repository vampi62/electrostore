using System.ComponentModel.DataAnnotations;

namespace ElectrostoreAPI.Dto;

public record ReadProjectItemDto
{
    public int id_project { get; init; }
    public int id_item { get; init; }
    public int quantity_project_item { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}
public record ReadExtendedProjectItemDto : ReadProjectItemDto
{
    public ReadItemDto? item { get; init; }
    public ReadProjectDto? project { get; init; }
}
public record ReadBulkProjectItemDto
{
    public required List<ReadProjectItemDto> valide { get; init; }
    public required List<ErrorDetail> error { get; init; }
}
public record CreateProjectItemByProjectDto
{
    [Required(ErrorMessage = "{0} is required.")]
    public required int id_item { get; init; }
    
    [Required(ErrorMessage = "{0} is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public required int quantity_project_item { get; init; }
}
public record CreateProjectItemByItemDto
{
    [Required(ErrorMessage = "{0} is required.")]
    public required int id_project { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public required int quantity_project_item { get; init; }
}
public record CreateProjectItemDto
{
    [Required(ErrorMessage = "{0} is required.")]
    public required int id_project { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    public required int id_item { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public required int quantity_project_item { get; init; }
}
public record UpdateProjectItemDto
{
    [Range(1, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public int? quantity_project_item { get; init; }
}