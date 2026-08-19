namespace ElectrostoreCRON.Kafka.Messages;

// ---- Outgoing messages (produced by the CRON, consumed by the WORKER) ----

public record ItemVendorPriceResultMessage
{
    public int     id_item_vendor                  { get; init; }
    public float   price_item_vendor_price         { get; init; }
    public string  currency_item_vendor_price      { get; init; } = string.Empty;
    public int     quantity_item_vendor_price      { get; init; } = 1;
    public string? price_breaks_item_vendor_price  { get; init; }
}
