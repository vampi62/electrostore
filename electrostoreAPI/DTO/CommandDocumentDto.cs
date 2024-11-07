using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record ReadCommandDocumentDto
{
    public int id_command_document { get; init; }
    public int id_command { get; init; }
    public string url_command_document { get; init; }
    public string name_command_document { get; init; }
    public string type_command_document { get; init; }
    public decimal size_command_document { get; init; }
    public DateTime date_command_document { get; init; }
}
public record CreateCommandDocumentDto
{
    [Required] public int id_command { get; init; }
    [Required] public string name_command_document { get; init; }
    [Required] public string type_command_document { get; init; }
    [Required] public IFormFile document { get; init; }
}
public record CreateCommandDocumentByCommandDto
{
    [Required] public string name_command_document { get; init; }
    [Required] public string type_command_document { get; init; }
    [Required] public IFormFile document { get; init; }
}
public record UpdateCommandDocumentDto
{
    public string? name_command_document { get; init; }
    public string? type_command_document { get; init; }
    public IFormFile? document { get; init; }
}