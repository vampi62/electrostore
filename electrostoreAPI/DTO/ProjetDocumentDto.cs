using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record ReadProjetDocumentDto
{
    public int id_projet_document { get; init; }
    public int id_projet { get; init; }
    public string url_projet_document { get; init; }
    public string name_projet_document { get; init; }
    public string type_projet_document { get; init; }
    public decimal size_projet_document { get; init; }
    public DateTime date_projet_document { get; init; }
}
public record CreateProjetDocumentDto
{
    [Required] public int id_projet { get; init; }
    [Required] public string name_projet_document { get; init; }
    [Required] public string type_projet_document { get; init; }
    [Required] public IFormFile document { get; init; }
}
public record CreateProjetDocumentByProjetDto
{
    [Required] public string name_projet_document { get; init; }
    [Required] public string type_projet_document { get; init; }
    [Required] public IFormFile document { get; init; }
}
public record UpdateProjetDocumentDto
{
    public string? name_projet_document { get; init; }
    public string? type_projet_document { get; init; }
    public IFormFile? document { get; init; }
}