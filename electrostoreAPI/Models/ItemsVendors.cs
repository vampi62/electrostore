using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;

namespace ElectrostoreAPI.Models;

public class ItemsVendors : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_item_vendor { get; set; }

    public int id_item { get; set; }
    [ForeignKey("id_item")]
    public Items? Item { get; set; }

    public VendorType vendor_type_item_vendor { get; set; }

    [MaxLength(Constants.MaxVendorSkuLength)]
    public required string vendor_sku_item_vendor { get; set; }

    [MaxLength(Constants.MaxUrlLength)]
    public string? url_item_vendor { get; set; }

    public ICollection<ItemVendorPrices> ItemVendorPrices { get; set; } = new List<ItemVendorPrices>();
}
