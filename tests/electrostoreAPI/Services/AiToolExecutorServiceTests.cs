using Moq;
using System.Text.Json;
using Xunit;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Services.AiToolExecutorService;
using ElectrostoreAPI.Services.BoxService;
using ElectrostoreAPI.Services.ItemBoxService;
using ElectrostoreAPI.Services.ItemService;
using ElectrostoreAPI.Services.StoreService;
using ElectrostoreAPI.Services.TagService;

namespace ElectrostoreAPI.Tests.Services
{
    public class AiToolExecutorServiceTests
    {
        private readonly Mock<IItemService> _itemService = new();
        private readonly Mock<IItemBoxService> _itemBoxService = new();
        private readonly Mock<ITagService> _tagService = new();
        private readonly Mock<IBoxService> _boxService = new();
        private readonly Mock<IStoreService> _storeService = new();

        private AiToolExecutorService CreateService() =>
            new(_itemService.Object, _itemBoxService.Object, _tagService.Object, _boxService.Object, _storeService.Object);

        private static PaginatedResponseDto<T> Page<T>(params T[] data) => new()
        {
            data = data,
            pagination = new PaginationDto { limit = 100, offset = 0, total = data.Length },
            filters = null,
            sort = null
        };

        private static ReadExtendedItemDto BuildItem(int id = 1, string name = "Resistor") => new()
        {
            id_item = id,
            reference_name_item = "R" + id,
            friendly_name_item = name
        };

        private static ReadExtendedBoxDto BuildBox(int id, int storeId) => new()
        {
            id_box = id,
            id_store = storeId
        };

        private static ReadExtendedStoreDto BuildStore(int id) => new()
        {
            id_store = id,
            name_store = "store" + id,
            mqtt_name_store = "mqtt" + id
        };

        private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;

        // --- GetToolDefinitions ---

        [Fact]
        public void GetToolDefinitions_ShouldExposeEveryTool()
        {
            var service = CreateService();

            var definitions = service.GetToolDefinitions();

            Assert.Equal(
                ["search_items", "get_item_stock_location", "list_boxes", "list_stores", "list_tags", "create_item", "create_box", "create_tag", "attach_tag", "move_item_stock"],
                definitions.Select(d => d.function.name));
            Assert.All(definitions, d =>
            {
                Assert.Equal("function", d.type);
                Assert.False(string.IsNullOrWhiteSpace(d.function.description));
                Assert.NotNull(d.function.parameters);
            });
        }

        [Fact]
        public void GetToolDefinitions_ShouldBuildCreateSchemasFromDtoAnnotations()
        {
            var service = CreateService();

            var definitions = service.GetToolDefinitions();

            var createItem = definitions.Single(d => d.function.name == "create_item");
            var schema = Parse(JsonSerializer.Serialize(createItem.function.parameters));
            Assert.Equal("object", schema.GetProperty("type").GetString());
            var properties = schema.GetProperty("properties");
            Assert.Equal("string", properties.GetProperty("reference_name_item").GetProperty("type").GetString());
            Assert.True(properties.GetProperty("reference_name_item").TryGetProperty("maxLength", out _));
            Assert.Equal("integer", properties.GetProperty("threshold_min_item").GetProperty("type").GetString());
            Assert.Equal(0, properties.GetProperty("threshold_min_item").GetProperty("minimum").GetDouble());
            Assert.False(properties.TryGetProperty("img_file", out _));
            var required = schema.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToList();
            Assert.Contains("reference_name_item", required);
            Assert.Contains("threshold_min_item", required);
            Assert.DoesNotContain("description_item", required);
        }

        [Fact]
        public void GetToolDefinitions_ShouldRequireQueryForSearchItems()
        {
            var service = CreateService();

            var definition = service.GetToolDefinitions().Single(d => d.function.name == "search_items");

            var schema = Parse(JsonSerializer.Serialize(definition.function.parameters));
            Assert.Equal("query", schema.GetProperty("required")[0].GetString());
        }

        // --- ExecuteToolAsync: read tools ---

        [Fact]
        public async Task ExecuteToolAsync_SearchItems_ShouldFilterByFriendlyNameAndHonourLimit()
        {
            _itemService
                .Setup(s => s.GetItems(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<FilterDto>?>(), It.IsAny<SorterDto?>(), It.IsAny<List<string>?>(), It.IsAny<List<int>?>()))
                .ReturnsAsync(Page(BuildItem(1, "Resistor 10k")));
            var service = CreateService();

            var result = await service.ExecuteToolAsync("search_items", """{"query":"resistor","limit":5}""");

            Assert.Null(result.ProposedAction);
            var json = Parse(result.ResultJson);
            Assert.Equal("Resistor 10k", json[0].GetProperty("friendly_name_item").GetString());
            _itemService.Verify(s => s.GetItems(5, It.IsAny<int>(),
                It.Is<List<FilterDto>?>(f => f != null && f.Count == 1 && f[0].field == "friendly_name_item" && f[0].search_type == "like" && f[0].value == "resistor"),
                It.IsAny<SorterDto?>(), It.IsAny<List<string>?>(), It.IsAny<List<int>?>()), Times.Once);
        }

        [Theory]
        [InlineData("")]
        [InlineData("{}")]
        [InlineData("""{"query":"  "}""")]
        public async Task ExecuteToolAsync_SearchItems_ShouldUseDefaultLimitAndNoFilter_WhenQueryIsBlank(string arguments)
        {
            _itemService
                .Setup(s => s.GetItems(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<FilterDto>?>(), It.IsAny<SorterDto?>(), It.IsAny<List<string>?>(), It.IsAny<List<int>?>()))
                .ReturnsAsync(Page<ReadExtendedItemDto>());
            var service = CreateService();

            var result = await service.ExecuteToolAsync("search_items", arguments);

            Assert.Equal("[]", result.ResultJson);
            _itemService.Verify(s => s.GetItems(20, It.IsAny<int>(), null, It.IsAny<SorterDto?>(), It.IsAny<List<string>?>(), It.IsAny<List<int>?>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteToolAsync_GetItemStockLocation_ShouldQueryBoxesOfItemWithBoxExpanded()
        {
            _itemBoxService
                .Setup(s => s.GetItemsBoxsByItemId(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<FilterDto>?>(), It.IsAny<SorterDto?>(), It.IsAny<List<string>?>()))
                .ReturnsAsync(Page(new ReadExtendedItemBoxDto { id_item = 7, id_box = 3, quantity_item_box = 12 }));
            var service = CreateService();

            var result = await service.ExecuteToolAsync("get_item_stock_location", """{"id_item":7}""");

            var json = Parse(result.ResultJson);
            Assert.Equal(12, json[0].GetProperty("quantity_item_box").GetInt32());
            _itemBoxService.Verify(s => s.GetItemsBoxsByItemId(7, 100, It.IsAny<int>(), It.IsAny<List<FilterDto>?>(), It.IsAny<SorterDto?>(),
                It.Is<List<string>?>(e => e != null && e.Contains("box"))), Times.Once);
        }

        [Fact]
        public async Task ExecuteToolAsync_ListBoxes_ShouldQuerySingleStore_WhenStoreIdProvided()
        {
            _boxService
                .Setup(s => s.GetBoxsByStoreId(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<FilterDto>?>(), It.IsAny<SorterDto?>(), It.IsAny<List<string>?>()))
                .ReturnsAsync(Page(BuildBox(1, 4), BuildBox(2, 4)));
            var service = CreateService();

            var result = await service.ExecuteToolAsync("list_boxes", """{"id_store":4}""");

            Assert.Equal(2, Parse(result.ResultJson).GetArrayLength());
            _boxService.Verify(s => s.GetBoxsByStoreId(4, 50, It.IsAny<int>(), It.IsAny<List<FilterDto>?>(), It.IsAny<SorterDto?>(), It.IsAny<List<string>?>()), Times.Once);
            _storeService.Verify(s => s.GetStores(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<FilterDto>?>(), It.IsAny<SorterDto?>(), It.IsAny<List<string>?>(), It.IsAny<List<int>?>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteToolAsync_ListBoxes_ShouldAggregateBoxesAcrossStores_WhenNoStoreProvided()
        {
            _storeService
                .Setup(s => s.GetStores(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<FilterDto>?>(), It.IsAny<SorterDto?>(), It.IsAny<List<string>?>(), It.IsAny<List<int>?>()))
                .ReturnsAsync(Page(BuildStore(1), BuildStore(2)));
            _boxService
                .Setup(s => s.GetBoxsByStoreId(1, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<FilterDto>?>(), It.IsAny<SorterDto?>(), It.IsAny<List<string>?>()))
                .ReturnsAsync(Page(BuildBox(10, 1)));
            _boxService
                .Setup(s => s.GetBoxsByStoreId(2, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<FilterDto>?>(), It.IsAny<SorterDto?>(), It.IsAny<List<string>?>()))
                .ReturnsAsync(Page(BuildBox(20, 2), BuildBox(21, 2)));
            var service = CreateService();

            var result = await service.ExecuteToolAsync("list_boxes", "{}");

            var ids = Parse(result.ResultJson).EnumerateArray().Select(b => b.GetProperty("id_box").GetInt32()).ToList();
            Assert.Equal([10, 20, 21], ids);
            _storeService.Verify(s => s.GetStores(20, It.IsAny<int>(), It.IsAny<List<FilterDto>?>(), It.IsAny<SorterDto?>(), It.IsAny<List<string>?>(), It.IsAny<List<int>?>()), Times.Once);
            _boxService.Verify(s => s.GetBoxsByStoreId(It.IsAny<int>(), 10, It.IsAny<int>(), It.IsAny<List<FilterDto>?>(), It.IsAny<SorterDto?>(), It.IsAny<List<string>?>()), Times.Exactly(2));
        }

        [Fact]
        public async Task ExecuteToolAsync_ListStores_ShouldReturnStores()
        {
            _storeService
                .Setup(s => s.GetStores(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<FilterDto>?>(), It.IsAny<SorterDto?>(), It.IsAny<List<string>?>(), It.IsAny<List<int>?>()))
                .ReturnsAsync(Page(BuildStore(1)));
            var service = CreateService();

            var result = await service.ExecuteToolAsync("list_stores", "");

            Assert.Equal("store1", Parse(result.ResultJson)[0].GetProperty("name_store").GetString());
            _storeService.Verify(s => s.GetStores(50, It.IsAny<int>(), It.IsAny<List<FilterDto>?>(), It.IsAny<SorterDto?>(), It.IsAny<List<string>?>(), It.IsAny<List<int>?>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteToolAsync_ListTags_ShouldFilterByName_WhenQueryProvided()
        {
            _tagService
                .Setup(s => s.GetTags(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<FilterDto>?>(), It.IsAny<SorterDto?>(), It.IsAny<List<string>?>(), It.IsAny<List<int>?>()))
                .ReturnsAsync(Page(new ReadExtendedTagDto { id_tag = 1, name_tag = "smd", weight_tag = 1 }));
            var service = CreateService();

            var result = await service.ExecuteToolAsync("list_tags", """{"query":"smd"}""");

            Assert.Equal("smd", Parse(result.ResultJson)[0].GetProperty("name_tag").GetString());
            _tagService.Verify(s => s.GetTags(50, It.IsAny<int>(),
                It.Is<List<FilterDto>?>(f => f != null && f.Count == 1 && f[0].field == "name_tag" && f[0].value == "smd"),
                It.IsAny<SorterDto?>(), It.IsAny<List<string>?>(), It.IsAny<List<int>?>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteToolAsync_ListTags_ShouldNotFilter_WhenNoQuery()
        {
            _tagService
                .Setup(s => s.GetTags(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<FilterDto>?>(), It.IsAny<SorterDto?>(), It.IsAny<List<string>?>(), It.IsAny<List<int>?>()))
                .ReturnsAsync(Page<ReadExtendedTagDto>());
            var service = CreateService();

            await service.ExecuteToolAsync("list_tags", "{}");

            _tagService.Verify(s => s.GetTags(50, It.IsAny<int>(), null, It.IsAny<SorterDto?>(), It.IsAny<List<string>?>(), It.IsAny<List<int>?>()), Times.Once);
        }

        // --- ExecuteToolAsync: proposal tools ---

        [Theory]
        [InlineData("create_item", """{"reference_name_item":"R10","friendly_name_item":"Resistor 10k","threshold_min_item":5}""")]
        [InlineData("create_box", """{"xstart_box":0,"ystart_box":0,"xend_box":2,"yend_box":2,"id_store":1}""")]
        [InlineData("create_tag", """{"name_tag":"smd","weight_tag":1}""")]
        [InlineData("attach_tag", """{"id_item":1,"id_tag":2}""")]
        [InlineData("move_item_stock", """{"id_box":1,"id_item":2,"quantity_item_box":3,"threshold_max_item_item_box":10}""")]
        public async Task ExecuteToolAsync_ProposalTools_ShouldReturnProposedActionWithoutCallingServices(string tool, string arguments)
        {
            var service = CreateService();

            var result = await service.ExecuteToolAsync(tool, arguments);

            Assert.NotNull(result.ProposedAction);
            Assert.Equal(tool, result.ProposedAction!.action_type);
            Assert.NotNull(result.ProposedAction.payload);
            var json = Parse(result.ResultJson);
            Assert.Equal("proposed", json.GetProperty("status").GetString());
            Assert.True(json.TryGetProperty("payload", out _));
            _itemService.VerifyNoOtherCalls();
            _itemBoxService.VerifyNoOtherCalls();
            _tagService.VerifyNoOtherCalls();
            _boxService.VerifyNoOtherCalls();
            _storeService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ExecuteToolAsync_CreateItem_ShouldEchoProposedPayload()
        {
            var service = CreateService();

            var result = await service.ExecuteToolAsync("create_item", """{"reference_name_item":"R10","friendly_name_item":"Resistor 10k","threshold_min_item":5}""");

            var payload = Parse(result.ResultJson).GetProperty("payload");
            Assert.Equal("R10", payload.GetProperty("reference_name_item").GetString());
            Assert.Equal(5, payload.GetProperty("threshold_min_item").GetInt32());
            var typed = Assert.IsType<CreateItemDto>(result.ProposedAction!.payload);
            Assert.Equal("Resistor 10k", typed.friendly_name_item);
        }

        [Theory]
        [InlineData("create_item")]
        [InlineData("create_box")]
        [InlineData("create_tag")]
        [InlineData("attach_tag")]
        [InlineData("move_item_stock")]
        public async Task ExecuteToolAsync_ProposalTools_ShouldReturnError_WhenArgumentsAreMissing(string tool)
        {
            var service = CreateService();

            var result = await service.ExecuteToolAsync(tool, "");

            Assert.Null(result.ProposedAction);
            Assert.True(Parse(result.ResultJson).TryGetProperty("error", out _));
        }

        [Fact]
        public async Task ExecuteToolAsync_ProposalTools_ShouldReturnError_WhenArgumentsAreJsonNull()
        {
            var service = CreateService();

            var result = await service.ExecuteToolAsync("create_tag", "null");

            Assert.Null(result.ProposedAction);
            Assert.True(Parse(result.ResultJson).TryGetProperty("error", out _));
        }

        [Fact]
        public async Task ExecuteToolAsync_ProposalTools_ShouldReturnError_WhenRequiredFieldIsMissing()
        {
            var service = CreateService();

            var result = await service.ExecuteToolAsync("create_tag", """{"name_tag":"smd"}""");

            Assert.Null(result.ProposedAction);
            Assert.True(Parse(result.ResultJson).TryGetProperty("error", out _));
        }

        // --- ExecuteToolAsync: error handling ---

        [Fact]
        public async Task ExecuteToolAsync_ShouldReturnError_WhenToolIsUnknown()
        {
            var service = CreateService();

            var result = await service.ExecuteToolAsync("drop_database", "{}");

            Assert.Null(result.ProposedAction);
            Assert.Contains("drop_database", Parse(result.ResultJson).GetProperty("error").GetString());
        }

        [Fact]
        public async Task ExecuteToolAsync_ShouldReturnError_WhenArgumentsAreMalformedJson()
        {
            var service = CreateService();

            var result = await service.ExecuteToolAsync("search_items", "{not json");

            Assert.True(Parse(result.ResultJson).TryGetProperty("error", out _));
        }

        [Fact]
        public async Task ExecuteToolAsync_ShouldReturnServiceErrorMessage_WhenServiceThrows()
        {
            _itemBoxService
                .Setup(s => s.GetItemsBoxsByItemId(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<FilterDto>?>(), It.IsAny<SorterDto?>(), It.IsAny<List<string>?>()))
                .ThrowsAsync(new KeyNotFoundException("Item with id '9' not found"));
            var service = CreateService();

            var result = await service.ExecuteToolAsync("get_item_stock_location", """{"id_item":9}""");

            Assert.Equal("Item with id '9' not found", Parse(result.ResultJson).GetProperty("error").GetString());
        }

        [Fact]
        public async Task ExecuteToolAsync_ShouldPropagateCancellation()
        {
            _storeService
                .Setup(s => s.GetStores(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<FilterDto>?>(), It.IsAny<SorterDto?>(), It.IsAny<List<string>?>(), It.IsAny<List<int>?>()))
                .ThrowsAsync(new OperationCanceledException());
            var service = CreateService();

            await Assert.ThrowsAsync<OperationCanceledException>(() => service.ExecuteToolAsync("list_stores", "{}"));
        }
    }
}
