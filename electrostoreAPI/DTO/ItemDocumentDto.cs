using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record ReadItemDocumentDto
{
    public int id_item_document { get; init; }
    public int id_item { get; init; }
    public string url_item_document { get; init; }
    public string name_item_document { get; init; }
    public string type_item_document { get; init; }
    public decimal size_item_document { get; init; }
    public DateTime date_item_document { get; init; }
}
public record CreateItemDocumentDto
{
    [Required] public int id_item { get; init; }
    [Required] public string name_item_document { get; init; }
    [Required] public string type_item_document { get; init; }
    [Required] public IFormFile document { get; init; }
}
public record CreateItemDocumentByItemDto
{
    [Required] public string name_item_document { get; init; }
    [Required] public string type_item_document { get; init; }
    [Required] public IFormFile document { get; init; }
}
public record UpdateItemDocumentDto
{
    public string? name_item_document { get; init; }
    public string? type_item_document { get; init; }
    public IFormFile? document { get; init; }
}