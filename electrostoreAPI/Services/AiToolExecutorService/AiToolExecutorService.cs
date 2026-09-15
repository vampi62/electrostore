using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Services.BoxService;
using ElectrostoreAPI.Services.ItemBoxService;
using ElectrostoreAPI.Services.ItemService;
using ElectrostoreAPI.Services.LlmChatService;
using ElectrostoreAPI.Services.StoreService;
using ElectrostoreAPI.Services.TagService;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
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

    private static readonly string[] SearchItemsRequired = ["query"];
    private static readonly string[] GetItemStockLocationRequired = ["id_item"];
    private static readonly string[] CreateItemRequired = ["reference_name_item", "friendly_name_item", "threshold_min_item"];
    private static readonly string[] CreateTagRequired = ["name_tag"];
    private static readonly string[] AttachTagRequired = ["id_item", "id_tag"];
    private static readonly string[] MoveItemStockRequired = ["id_item", "id_box", "quantity_item_box"];

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
                required = SearchItemsRequired
            }),
            Def("get_item_stock_location", "Get the boxes (and quantities) an item is currently stored in.", new
            {
                type = "object",
                properties = new
                {
                    id_item = new { type = "integer", description = "The item id." }
                },
                required = GetItemStockLocationRequired
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
            Def("create_item", "Propose creating a new item. This does not create anything: it only returns the proposed data for the user to validate.", BuildDtoSchema<CreateItemDto>()),
            Def("create_box", "Propose creating a new box in a store. This does not create anything: it only returns the proposed data for the user to validate.", BuildDtoSchema<CreateBoxDto>()),
            Def("create_tag", "Propose creating a new tag. This does not create anything: it only returns the proposed data for the user to validate.", BuildDtoSchema<CreateTagDto>()),
            Def("attach_tag", "Propose attaching an existing tag to an existing item. This does not attach anything: it only returns the proposed data for the user to validate.", BuildDtoSchema<CreateItemTagDto>()),
            Def("move_item_stock", "Propose storing/moving/adjusting an item's quantity in a box. This does not change any quantity: it only returns the proposed data for the user to validate.", BuildDtoSchema<CreateItemBoxDto>())
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
                "create_box" => ProposeCreateBox(argumentsJson),
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

    private static AiToolExecutionResult ProposeCreateItem(string argumentsJson)
    {
        return ProposedResult("create_item", ParseDto<CreateItemDto>(argumentsJson));
    }

    private AiToolExecutionResult ProposeCreateBox(string argumentsJson)
    {
        return ProposedResult("create_box", ParseDto<CreateBoxDto>(argumentsJson));
    }

    private static AiToolExecutionResult ProposeCreateTag(string argumentsJson)
    {
        return ProposedResult("create_tag", ParseDto<CreateTagDto>(argumentsJson));
    }

    private static AiToolExecutionResult ProposeAttachTag(string argumentsJson)
    {
        return ProposedResult("attach_tag", ParseDto<CreateItemTagDto>(argumentsJson));
    }

    private static AiToolExecutionResult ProposeMoveItemStock(string argumentsJson)
    {
        return ProposedResult("move_item_stock", ParseDto<CreateItemBoxDto>(argumentsJson));
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

    /// <summary>
    /// Deserializes tool-call arguments directly into an API create* DTO, so required fields
    /// and validation stay driven by the DTO rather than a hand-maintained mirror class.
    /// </summary>
    private static T ParseDto<T>(string argumentsJson)
    {
        if (string.IsNullOrWhiteSpace(argumentsJson))
        {
            throw new JsonException($"Missing arguments for '{typeof(T).Name}'.");
        }
        return JsonSerializer.Deserialize<T>(argumentsJson, JsonOptions)
            ?? throw new JsonException($"Invalid arguments for '{typeof(T).Name}'.");
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

    /// <summary>
    /// Builds a JSON-schema "object" description straight from a create* DTO's public properties
    /// and DataAnnotations, so the tool definition sent to the LLM stays in sync with the API DTOs.
    /// </summary>
    private static Dictionary<string, object> BuildDtoSchema<T>()
    {
        var properties = new Dictionary<string, object>();
        var required = new List<string>();
        foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            // File uploads are not representable as LLM tool-call arguments.
            if (prop.PropertyType == typeof(IFormFile))
            {
                continue;
            }
            var schema = new Dictionary<string, object> { ["type"] = JsonSchemaType(prop.PropertyType) };
            if (prop.GetCustomAttribute<RangeAttribute>() is { } range)
            {
                schema["minimum"] = Convert.ToDouble(range.Minimum);
                schema["maximum"] = Convert.ToDouble(range.Maximum);
            }
            if (prop.GetCustomAttribute<MaxLengthAttribute>() is { } maxLength)
            {
                schema["maxLength"] = maxLength.Length;
            }
            properties[prop.Name] = schema;
            if (prop.GetCustomAttribute<RequiredAttribute>() is not null)
            {
                required.Add(prop.Name);
            }
        }
        return new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = properties,
            ["required"] = required
        };
    }

    private static string JsonSchemaType(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        if (underlying == typeof(bool)) return "boolean";
        if (underlying == typeof(int) || underlying == typeof(long) || underlying == typeof(short)) return "integer";
        if (underlying == typeof(double) || underlying == typeof(float) || underlying == typeof(decimal)) return "number";
        return "string";
    }

    private class SearchItemsArgs
    {
        public string? query { get; set; } = null;
        public int? limit { get; set; } = null;
    }

    private sealed class ItemStockLocationArgs
    {
        public int id_item { get; set; } = 0;
    }

    private sealed class ListBoxesArgs
    {
        public int? id_store { get; set; } = null;
    }

    private sealed class ListTagsArgs
    {
        public string? query { get; set; } = null;
    }
}
