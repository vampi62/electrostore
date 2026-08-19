namespace ElectrostoreAPI.Dto;

public record ReadItemVendorPriceDto
{
    public int id_item_vendor_price { get; init; }
    public int id_item_vendor { get; init; }
    public float price_item_vendor_price { get; init; }
    public required string currency_item_vendor_price { get; init; }
    public int quantity_item_vendor_price { get; init; }
    public string? price_breaks_item_vendor_price { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}

public record ReadExtendedItemVendorPriceDto : ReadItemVendorPriceDto
{
    public ReadItemVendorDto? item_vendor { get; init; }
}
