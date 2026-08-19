using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Models;

public class ItemVendorPrices : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_item_vendor_price { get; set; }

    public int id_item_vendor { get; set; }
    [ForeignKey("id_item_vendor")]
    public ItemsVendors? ItemVendor { get; set; }

    public float price_item_vendor_price { get; set; }

    [MaxLength(Constants.MaxCurrencyCodeLength)]
    public required string currency_item_vendor_price { get; set; }

    public int quantity_item_vendor_price { get; set; } = 1;

    public string? price_breaks_item_vendor_price { get; set; }
}
