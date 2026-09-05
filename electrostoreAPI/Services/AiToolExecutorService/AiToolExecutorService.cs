using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Services.BoxService;
using ElectrostoreAPI.Services.ItemBoxService;
using ElectrostoreAPI.Services.ItemService;
using ElectrostoreAPI.Services.LlmChatService;
using ElectrostoreAPI.Services.StoreService;
using ElectrostoreAPI.Services.TagService;
using System.Text.Json;

namespace ElectrostoreAPI.Services.AiToolExecutorService;

public class AiToolExecutorService : IAiToolExecutorService
{
    private readonly IItemService _itemService;
    private readonly IItemBoxService _itemBoxService;
    private readonly ITagService _tagService;
    private readonly IBoxService _boxService;
    private readonly IStoreService _storeService;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public AiToolExecutorService(
        IItemService itemService,
        IItemBoxService itemBoxService,
        ITagService tagService,
        IBoxService boxService,
        IStoreService storeService)
    {
        _itemService = itemService;
        _itemBoxService = itemBoxService;
        _tagService = tagService;
        _boxService = boxService;
        _storeService = storeService;
    }

    public List<LlmToolDefinition> GetToolDefinitions()
    {
        return
        [
            Def("search_items", "Search items by reference or friendly name.", new
            {
                type = "object",
                properties = new
                {
                    query = new { type = "string", description = "Free-text search on the item name." },
                    limit = new { type = "integer", description = "Max number of results (default 20)." }
                },
                required = new[] { "query" }
            }),
            Def("get_item_stock_location", "Get the boxes (and quantities) an item is currently stored in.", new
            {
                type = "object",
                properties = new
                {
                    id_item = new { type = "integer", description = "The item id." }
                },
                required = new[] { "id_item" }
            }),
            Def("list_boxes", "List boxes, optionally filtered by store.", new
            {
                type = "object",
                properties = new
                {
                    id_store = new { type = "integer", description = "Optional store id to filter on." }
                }
            }),
            Def("list_stores", "List all stores.", new
            {
                type = "object",
                properties = new { }
            }),
            Def("list_tags", "List tags, optionally filtered by name.", new
            {
                type = "object",
                properties = new
                {
                    query = new { type = "string", description = "Optional free-text search on the tag name." }
                }
            }),
            Def("create_item", "Propose creating a new item. This does not create anything: it only returns the proposed data for the user to validate.", new
            {
                type = "object",
                properties = new
                {
                    reference_name_item = new { type = "string" },
                    friendly_name_item = new { type = "string" },
                    threshold_min_item = new { type = "integer" },
                    description_item = new { type = "string" }
                },
                required = new[] { "reference_name_item", "friendly_name_item", "threshold_min_item" }
            }),
            Def("create_tag", "Propose creating a new tag. This does not create anything: it only returns the proposed data for the user to validate.", new
            {
                type = "object",
                properties = new
                {
                    name_tag = new { type = "string" },
                    weight_tag = new { type = "integer" }
                },
                required = new[] { "name_tag" }
            }),
            Def("attach_tag", "Propose attaching an existing tag to an existing item. This does not attach anything: it only returns the proposed data for the user to validate.", new
            {
                type = "object",
                properties = new
                {
                    id_item = new { type = "integer" },
                    id_tag = new { type = "integer" }
                },
                required = new[] { "id_item", "id_tag" }
            }),
            Def("move_item_stock", "Propose storing/moving/adjusting an item's quantity in a box. This does not change any quantity: it only returns the proposed data for the user to validate.", new
            {
                type = "object",
                properties = new
                {
                    id_item = new { type = "integer" },
                    id_box = new { type = "integer" },
                    quantity_item_box = new { type = "integer", description = "The resulting quantity of the item in that box." },
                    threshold_max_item_item_box = new { type = "integer" }
                },
                required = new[] { "id_item", "id_box", "quantity_item_box" }
            })
        ];
    }

    public async Task<AiToolExecutionResult> ExecuteToolAsync(string toolName, string argumentsJson, CancellationToken cancellationToken = default)
    {
        try
        {
            return toolName switch
            {
                "search_items" => await SearchItems(argumentsJson),
                "get_item_stock_location" => await GetItemStockLocation(argumentsJson),
                "list_boxes" => await ListBoxes(argumentsJson),
                "list_stores" => await ListStores(),
                "list_tags" => await ListTags(argumentsJson),
                "create_item" => ProposeCreateItem(argumentsJson),
                "create_tag" => ProposeCreateTag(argumentsJson),
                "attach_tag" => ProposeAttachTag(argumentsJson),
                "move_item_stock" => ProposeMoveItemStock(argumentsJson),
                _ => new AiToolExecutionResult { ResultJson = JsonSerializer.Serialize(new { error = $"Unknown tool '{toolName}'" }) }
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new AiToolExecutionResult { ResultJson = JsonSerializer.Serialize(new { error = ex.Message }) };
        }
    }

    private async Task<AiToolExecutionResult> SearchItems(string argumentsJson)
    {
        var args = Parse<SearchItemsArgs>(argumentsJson);
        var rsql = new List<FilterDto>();
        if (!string.IsNullOrWhiteSpace(args.query))
        {
            rsql.Add(new FilterDto { field = "friendly_name_item", search_type = "like", value = args.query });
        }
        var items = await _itemService.GetItems(limit: args.limit ?? 20, rsql: rsql.Count > 0 ? rsql : null);
        return Result(items.data);
    }

    private async Task<AiToolExecutionResult> GetItemStockLocation(string argumentsJson)
    {
        var args = Parse<ItemStockLocationArgs>(argumentsJson);
        var boxes = await _itemBoxService.GetItemsBoxsByItemId(args.id_item, limit: 100, expand: ["box"]);
        return Result(boxes.data);
    }

    private async Task<AiToolExecutionResult> ListBoxes(string argumentsJson)
    {
        var args = Parse<ListBoxesArgs>(argumentsJson);
        if (args.id_store is not null)
        {
            var boxes = await _boxService.GetBoxsByStoreId(args.id_store.Value, limit: 50);
            return Result(boxes.data);
        }
        // No store given: aggregate a small sample of boxes across all stores.
        var stores = await _storeService.GetStores(limit: 20);
        var allBoxes = new List<ReadExtendedBoxDto>();
        foreach (var store in stores.data)
        {
            var storeBoxes = await _boxService.GetBoxsByStoreId(store.id_store, limit: 10);
            allBoxes.AddRange(storeBoxes.data);
        }
        return Result(allBoxes);
    }

    private async Task<AiToolExecutionResult> ListStores()
    {
        var stores = await _storeService.GetStores(limit: 50);
        return Result(stores.data);
    }

    private async Task<AiToolExecutionResult> ListTags(string argumentsJson)
    {
        var args = Parse<ListTagsArgs>(argumentsJson);
        var rsql = new List<FilterDto>();
        if (!string.IsNullOrWhiteSpace(args.query))
        {
            rsql.Add(new FilterDto { field = "name_tag", search_type = "like", value = args.query });
        }
        var tags = await _tagService.GetTags(limit: 50, rsql: rsql.Count > 0 ? rsql : null);
        return Result(tags.data);
    }

    private AiToolExecutionResult ProposeCreateItem(string argumentsJson)
    {
        var args = Parse<CreateItemArgs>(argumentsJson);
        var payload = new CreateItemDto
        {
            reference_name_item = args.reference_name_item,
            friendly_name_item = args.friendly_name_item,
            threshold_min_item = args.threshold_min_item,
            description_item = args.description_item
        };
        return ProposedResult("create_item", payload);
    }

    private AiToolExecutionResult ProposeCreateTag(string argumentsJson)
    {
        var args = Parse<CreateTagArgs>(argumentsJson);
        var payload = new CreateTagDto
        {
            name_tag = args.name_tag,
            weight_tag = args.weight_tag ?? 0
        };
        return ProposedResult("create_tag", payload);
    }

    private AiToolExecutionResult ProposeAttachTag(string argumentsJson)
    {
        var args = Parse<AttachTagArgs>(argumentsJson);
        var payload = new CreateItemTagDto
        {
            id_item = args.id_item,
            id_tag = args.id_tag
        };
        return ProposedResult("attach_tag", payload);
    }

    private AiToolExecutionResult ProposeMoveItemStock(string argumentsJson)
    {
        var args = Parse<MoveItemStockArgs>(argumentsJson);
        var payload = new CreateItemBoxDto
        {
            id_item = args.id_item,
            id_box = args.id_box,
            quantity_item_box = args.quantity_item_box,
            threshold_max_item_item_box = args.threshold_max_item_item_box ?? 0
        };
        return ProposedResult("move_item_stock", payload);
    }

    private static AiToolExecutionResult ProposedResult(string actionType, object payload)
    {
        var proposedAction = new ProposedActionDto { action_type = actionType, payload = payload };
        return new AiToolExecutionResult
        {
            ResultJson = JsonSerializer.Serialize(new
            {
                status = "proposed",
                message = "This action was proposed to the user; it has not been applied yet.",
                payload
            }, JsonOptions),
            ProposedAction = proposedAction
        };
    }

    private static T Parse<T>(string argumentsJson) where T : new()
    {
        if (string.IsNullOrWhiteSpace(argumentsJson))
        {
            return new T();
        }
        return JsonSerializer.Deserialize<T>(argumentsJson, JsonOptions) ?? new T();
    }

    private static AiToolExecutionResult Result<T>(T value)
    {
        return new AiToolExecutionResult { ResultJson = JsonSerializer.Serialize(value, JsonOptions) };
    }

    private static LlmToolDefinition Def(string name, string description, object parameters)
    {
        return new LlmToolDefinition
        {
            function = new LlmFunctionDefinition
            {
                name = name,
                description = description,
                parameters = parameters
            }
        };
    }

    private class SearchItemsArgs
    {
        public string? query { get; set; }
        public int? limit { get; set; }
    }

    private class ItemStockLocationArgs
    {
        public int id_item { get; set; }
    }

    private class ListBoxesArgs
    {
        public int? id_store { get; set; }
    }

    private class ListTagsArgs
    {
        public string? query { get; set; }
    }

    private class CreateItemArgs
    {
        public string reference_name_item { get; set; } = string.Empty;
        public string friendly_name_item { get; set; } = string.Empty;
        public int threshold_min_item { get; set; }
        public string? description_item { get; set; }
    }

    private class CreateTagArgs
    {
        public string name_tag { get; set; } = string.Empty;
        public int? weight_tag { get; set; }
    }

    private class AttachTagArgs
    {
        public int id_item { get; set; }
        public int id_tag { get; set; }
    }

    private class MoveItemStockArgs
    {
        public int id_item { get; set; }
        public int id_box { get; set; }
        public int quantity_item_box { get; set; }
        public int? threshold_max_item_item_box { get; set; }
    }
}
