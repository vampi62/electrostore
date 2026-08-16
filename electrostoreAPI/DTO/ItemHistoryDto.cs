using ElectrostoreAPI.Enums;

namespace ElectrostoreAPI.Dto;

public record ReadItemHistoryDto
{
    public int id_item_history { get; init; }
    public int? id_item { get; init; }
    public int? id_box { get; init; }
    public int? id_user { get; init; }
    public ItemHistoryType type_item_history { get; init; }
    public int? quantity_change_item_history { get; init; }
    public int? old_quantity_item_history { get; init; }
    public int? new_quantity_item_history { get; init; }
    public string? notes_item_history { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}

public record ReadExtendedItemHistoryDto : ReadItemHistoryDto
{
    public ReadItemDto? item { get; init; }
    public ReadBoxDto? box { get; init; }
    public ReadUserDto? user { get; init; }
}
