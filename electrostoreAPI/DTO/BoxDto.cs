using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace electrostore.Dto;

public record ReadBoxDto
{
    public int id_box { get; init; }
    public int xstart_box { get; init; }
    public int ystart_box { get; init; }
    public int xend_box { get; init; }
    public int yend_box { get; init; }
    public int id_store { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}
public record ReadExtendedBoxDto : ReadBoxDto
{
    public ReadStoreDto? store { get; init; }
    public int box_tags_count { get; init; }
    public int item_boxs_count { get; init; }
    public IEnumerable<ReadBoxTagDto>? box_tags { get; init; }
    public IEnumerable<ReadItemBoxDto>? item_boxs { get; init; }
}
public record ReadBulkBoxDto
{
    public required List<ReadBoxDto> Valide { get; init; }
    public required List<ErrorDetail> Error { get; init; }
}
public record CreateBoxByStoreDto
{
    [Required(ErrorMessage = "{0} is required.")]
    [Range(0, int.MaxValue, ErrorMessage = "{0} must be between {1} and {2}.")]
    public required int xstart_box { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [Range(0, int.MaxValue, ErrorMessage = "{0} must be between {1} and {2}.")]
    public required int ystart_box { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [Range(0, int.MaxValue, ErrorMessage = "{0} must be between {1} and {2}.")]
    public required int xend_box { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [Range(0, int.MaxValue, ErrorMessage = "{0} must be between {1} and {2}.")]
    public required int yend_box { get; init; }
}
public record CreateBoxDto
{
    [Required(ErrorMessage = "{0} is required.")]
    [Range(0, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public required int xstart_box { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [Range(0, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public required int ystart_box { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [Range(0, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public required int xend_box { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [Range(0, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public required int yend_box { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    public required int id_store { get; init; }
}
public record UpdateBoxByStoreDto
{
    [Range(0, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public int? xstart_box { get; init; }

    [Range(0, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public int? ystart_box { get; init; }

    [Range(0, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public int? xend_box { get; init; }

    [Range(0, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public int? yend_box { get; init; }
}
public record UpdateBoxDto
{
    [Range(0, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public int? xstart_box { get; init; }

    [Range(0, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public int? ystart_box { get; init; }

    [Range(0, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public int? xend_box { get; init; }

    [Range(0, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public int? yend_box { get; init; }

    public int? new_id_store { get; init; }
}
public record UpdateBulkBoxByStoreDto
{
    [Required(ErrorMessage = "{0} is required.")]
    public required int id_box { get; init; }

    [Range(0, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public int? xstart_box { get; init; }

    [Range(0, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public int? ystart_box { get; init; }

    [Range(0, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public int? xend_box { get; init; }

    [Range(0, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public int? yend_box { get; init; }
    
    public int? new_id_store { get; init; }

    public string? status { get; init; } // status field to indicate the new status "delete", "modified", "new"
}