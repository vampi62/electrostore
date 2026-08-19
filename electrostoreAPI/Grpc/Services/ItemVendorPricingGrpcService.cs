using Grpc.Core;
using ElectrostoreAPI.Services.ItemVendorService;
using ElectrostoreAPI.Services.ItemVendorPriceService;

namespace ElectrostoreAPI.Grpc.Services;

public class ItemVendorPricingGrpcService : ItemVendorPricingGrpc.ItemVendorPricingGrpcBase
{
    private readonly IItemVendorService _itemVendorService;
    private readonly IItemVendorPriceService _itemVendorPriceService;
    private readonly ILogger<ItemVendorPricingGrpcService> _logger;

    public ItemVendorPricingGrpcService(
        IItemVendorService itemVendorService,
        IItemVendorPriceService itemVendorPriceService,
        ILogger<ItemVendorPricingGrpcService> logger)
    {
        _itemVendorService = itemVendorService;
        _itemVendorPriceService = itemVendorPriceService;
        _logger = logger;
    }

    public override async Task<GetItemVendorsToPriceReply> GetItemVendorsToPrice(
        GetItemVendorsToPriceRequest request, ServerCallContext context)
    {
        var limit = request.Limit > 0 ? request.Limit : int.MaxValue;
        var itemVendors = await _itemVendorService.GetItemVendors(limit, 0);
        var data = itemVendors.data.AsEnumerable();
        if (request.HasVendorType)
        {
            data = data.Where(iv => (int)iv.vendor_type_item_vendor == (int)request.VendorType);
        }
        var reply = new GetItemVendorsToPriceReply();
        reply.ItemVendors.AddRange(data.Select(iv => new ItemVendorItem
        {
            IdItemVendor = iv.id_item_vendor,
            IdItem = iv.id_item,
            VendorTypeItemVendor = (VendorType)(int)iv.vendor_type_item_vendor,
            VendorSkuItemVendor = iv.vendor_sku_item_vendor,
            UrlItemVendor = iv.url_item_vendor ?? string.Empty,
        }));
        _logger.LogDebug("GetItemVendorsToPrice: returned {Count} item-vendor link(s)", reply.ItemVendors.Count);
        return reply;
    }

    public override async Task<RecordItemVendorPricesReply> RecordItemVendorPrices(
        RecordItemVendorPricesRequest request, ServerCallContext context)
    {
        var reply = new RecordItemVendorPricesReply();
        foreach (var observation in request.Observations)
        {
            try
            {
                await _itemVendorPriceService.RecordPriceObservation(
                    observation.IdItemVendor,
                    observation.PriceItemVendorPrice,
                    observation.CurrencyItemVendorPrice,
                    observation.QuantityItemVendorPrice > 0 ? observation.QuantityItemVendorPrice : 1,
                    string.IsNullOrWhiteSpace(observation.PriceBreaksItemVendorPrice) ? null : observation.PriceBreaksItemVendorPrice);
                reply.Results.Add(new ItemVendorPriceResult { IdItemVendor = observation.IdItemVendor, Success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RecordItemVendorPrices: error for item_vendor={Id}", observation.IdItemVendor);
                reply.Results.Add(new ItemVendorPriceResult { IdItemVendor = observation.IdItemVendor, Success = false, ErrorMessage = ex.Message });
            }
        }
        _logger.LogInformation("RecordItemVendorPrices: {Ok}/{Total} observation(s) recorded",
            reply.Results.Count(r => r.Success), reply.Results.Count);
        return reply;
    }
}
