using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record ReadImgDto
{
    public int id_img { get; init; }
    public string nom_img { get; init; }
    public string url_img { get; init; }
    public string description_img { get; init; }
    public DateTime date_img { get; init; }
    public int id_item { get; init; }
}
public record CreateImgByItemDto
{
    [Required] public string nom_img { get; init; }
    [Required] public string description_img { get; init; }
}
public record CreateImgDto
{
    [Required] public string nom_img { get; init; }
    [Required] public string description_img { get; init; }
    [Required] public int id_item { get; init; }
}
public record UpdateImgDto
{
    public string? nom_img { get; init; }
    public string? description_img { get; init; }
}
public record GetImageFileResult
{
    public string? FilePath { get; init; }
    public string? MimeType { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}