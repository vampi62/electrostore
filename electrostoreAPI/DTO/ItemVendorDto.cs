using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Validators;
using System.ComponentModel.DataAnnotations;

namespace ElectrostoreAPI.Dto;

public record ReadItemVendorDto
{
    public int id_item_vendor { get; init; }
    public int id_item { get; init; }
    public VendorType vendor_type_item_vendor { get; init; }
    public required string vendor_sku_item_vendor { get; init; }
    public string? url_item_vendor { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}

public record ReadExtendedItemVendorDto : ReadItemVendorDto
{
    public ReadExtendedItemDto? item { get; init; }
}

public record CreateItemVendorDto
{
    [Required(ErrorMessage = "{0} is required.")]
    public required int id_item { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [Range(0, (int)VendorType.Octopart, ErrorMessage = "{0} must be a valid vendor, between {1} and {2}.")]
    public VendorType vendor_type_item_vendor { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxVendorSkuLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public required string vendor_sku_item_vendor { get; init; }

    [MaxLength(Constants.MaxUrlLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public string? url_item_vendor { get; init; }
}

public record UpdateItemVendorDto
{
    [Range(0, (int)VendorType.Octopart, ErrorMessage = "{0} must be a valid vendor, between {1} and {2}.")]
    public VendorType? vendor_type_item_vendor { get; init; }

    [MaxLength(Constants.MaxVendorSkuLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public string? vendor_sku_item_vendor { get; init; }

    [MaxLength(Constants.MaxUrlLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public string? url_item_vendor { get; init; }
}
