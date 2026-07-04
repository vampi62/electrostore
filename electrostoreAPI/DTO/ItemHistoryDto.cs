using ElectrostoreAPI.Enums;

namespace ElectrostoreAPI.Dto;

public record ReadItemHistoryDto
{
    public int id_item_history { get; init; }
    public int? id_item { get; init; }
    public int? id_box { get; init; }
    public int? id_user { get; init; }
    public ItemHistoryType type { get; init; }
    public int? quantity_change { get; init; }
    public int? old_quantity { get; init; }
    public int? new_quantity { get; init; }
    public string? notes { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}

public record ReadExtendedItemHistoryDto : ReadItemHistoryDto
{
    public ReadItemDto? item { get; init; }
    public ReadBoxDto? box { get; init; }
    public ReadUserDto? user { get; init; }
}
