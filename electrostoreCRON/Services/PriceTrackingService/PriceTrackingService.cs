using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ElectrostoreCRON.Grpc;
using ElectrostoreCRON.Kafka.Messages;
using ElectrostoreCRON.Kafka.Producer;
using Grpc.Core;

namespace ElectrostoreCRON.Services.PriceTrackingService;

public class PriceTrackingService : IPriceTrackingService
{
    private const string ResultTopic = "item-vendor-price-result";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ItemVendorPricingGrpc.ItemVendorPricingGrpcClient _apiClient;
    private readonly IKafkaProducerService _kafka;
    private readonly ILogger<PriceTrackingService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private string? _nexarAccessToken;
    private DateTimeOffset _nexarTokenExpiresAt = DateTimeOffset.MinValue;

    public PriceTrackingService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ItemVendorPricingGrpc.ItemVendorPricingGrpcClient apiClient,
        IKafkaProducerService kafka,
        ILogger<PriceTrackingService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _apiClient = apiClient;
        _kafka = kafka;
        _logger = logger;
    }

    public async Task SyncAllAsync(CancellationToken ct = default)
    {
        var mouserEnabled = _configuration.GetValue<bool>("Mouser:Enable");
        var nexarEnabled = _configuration.GetValue<bool>("Nexar:Enable");
        if (!mouserEnabled && !nexarEnabled)
        {
            _logger.LogWarning("PriceTracking: no vendor enabled (Mouser:Enable and Nexar:Enable are both false) - sync skipped.");
            return;
        }

        var fetchBatchSize = _configuration.GetValue<int>("PriceTracking:FetchBatchSize", 100);
        GetItemVendorsToPriceReply links;
        try
        {
            links = await _apiClient.GetItemVendorsToPriceAsync(
                new GetItemVendorsToPriceRequest { Limit = fetchBatchSize }, cancellationToken: ct);
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "PriceTracking: failed to fetch item-vendor links from the API - sync aborted.");
            return;
        }

        var mouserLinks = links.ItemVendors.Where(l => l.VendorTypeItemVendor == VendorType.Mouser).ToList();
        var octopartLinks = links.ItemVendors.Where(l => l.VendorTypeItemVendor == VendorType.Octopart).ToList();

        var observations = new List<ItemVendorPriceObservation>();

        if (mouserEnabled && mouserLinks.Count > 0)
        {
            try
            {
                observations.AddRange(await FetchMouserPricesAsync(mouserLinks, ct));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PriceTracking: Mouser sync failed - continuing with other vendors.");
            }
        }

        if (nexarEnabled && octopartLinks.Count > 0)
        {
            try
            {
                observations.AddRange(await FetchNexarPricesAsync(octopartLinks, ct));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PriceTracking: Nexar/Octopart sync failed - continuing with other vendors.");
            }
        }

        if (observations.Count == 0)
        {
            _logger.LogInformation("PriceTracking: no price observations collected this run.");
            return;
        }

        await PublishObservationsAsync(observations, ct);
    }

    // ---- Publish observations to Kafka (the WORKER consumes them and forwards to the API via gRPC) ----

    private async Task PublishObservationsAsync(List<ItemVendorPriceObservation> observations, CancellationToken ct)
    {
        var ok = 0;
        foreach (var observation in observations)
        {
            var message = new ItemVendorPriceResultMessage
            {
                id_item_vendor = observation.IdItemVendor,
                price_item_vendor_price = observation.PriceItemVendorPrice,
                currency_item_vendor_price = observation.CurrencyItemVendorPrice,
                quantity_item_vendor_price = observation.QuantityItemVendorPrice,
                price_breaks_item_vendor_price = string.IsNullOrEmpty(observation.PriceBreaksItemVendorPrice) ? null : observation.PriceBreaksItemVendorPrice,
            };
            try
            {
                await _kafka.PublishAsync(ResultTopic, observation.IdItemVendor.ToString(), JsonSerializer.Serialize(message, JsonOptions), ct);
                ok++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PriceTracking: failed to publish price observation for item_vendor={Id} to {Topic}.",
                    observation.IdItemVendor, ResultTopic);
            }
        }
        _logger.LogInformation("PriceTracking: {Ok}/{Total} price observation(s) published to {Topic}.", ok, observations.Count, ResultTopic);
    }

    // ---- Mouser --------------------------------------------------------------
    // Verify against current Mouser API docs before relying on this in production - endpoint/auth/response
    // shape are subject to change (https://www.mouser.com/api-search/).

    private async Task<List<ItemVendorPriceObservation>> FetchMouserPricesAsync(
        List<ItemVendorItem> links, CancellationToken ct)
    {
        var apiKey = _configuration["Mouser:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("PriceTracking: Mouser:ApiKey not configured - Mouser sync skipped.");
            return [];
        }
        var baseUrl = _configuration["Mouser:BaseUrl"] ?? "https://api.mouser.com/api/v1";
        using var client = _httpClientFactory.CreateClient();
        var results = new List<ItemVendorPriceObservation>();

        foreach (var link in links)
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                var requestBody = JsonSerializer.Serialize(new MouserSearchRequest(
                    new MouserSearchByPartRequest(link.VendorSkuItemVendor, "None")));
                using var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                var url = $"{baseUrl}/search/partnumber?apiKey={Uri.EscapeDataString(apiKey)}";
                var response = await client.PostAsync(url, content, ct);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("PriceTracking: Mouser HTTP {Code} for sku={Sku}.",
                        (int)response.StatusCode, link.VendorSkuItemVendor);
                    continue;
                }
                var json = await response.Content.ReadAsStringAsync(ct);
                var apiResp = JsonSerializer.Deserialize<MouserSearchResponse>(json, JsonOptions);
                var part = apiResp?.SearchResults?.Parts?.FirstOrDefault();
                var priceBreaks = part?.PriceBreaks;
                if (priceBreaks is null || priceBreaks.Length == 0)
                {
                    _logger.LogDebug("PriceTracking: Mouser returned no price breaks for sku={Sku}.", link.VendorSkuItemVendor);
                    continue;
                }
                var reference = priceBreaks.OrderBy(p => p.Quantity).First();
                if (!TryParseVendorPrice(reference.Price, out var price))
                {
                    _logger.LogWarning("PriceTracking: could not parse Mouser price '{Price}' for sku={Sku}.",
                        reference.Price, link.VendorSkuItemVendor);
                    continue;
                }
                results.Add(new ItemVendorPriceObservation
                {
                    IdItemVendor = link.IdItemVendor,
                    PriceItemVendorPrice = price,
                    CurrencyItemVendorPrice = reference.Currency ?? "USD",
                    QuantityItemVendorPrice = reference.Quantity,
                    PriceBreaksItemVendorPrice = JsonSerializer.Serialize(priceBreaks),
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PriceTracking: Mouser lookup failed for sku={Sku}.", link.VendorSkuItemVendor);
            }
        }
        return results;
    }

    // ---- Nexar / Octopart ------------------------------------------------------
    // Verify against current Nexar API docs/GraphQL schema before relying on this in production - the
    // supSearchMpn schema has evolved over time (https://nexar.com/api).

    private async Task<List<ItemVendorPriceObservation>> FetchNexarPricesAsync(
        List<ItemVendorItem> links, CancellationToken ct)
    {
        var accessToken = await GetNexarAccessTokenAsync(ct);
        if (accessToken is null)
        {
            return [];
        }
        var graphQlUrl = _configuration["Nexar:GraphQlUrl"] ?? "https://api.nexar.com/graphql";
        using var client = _httpClientFactory.CreateClient();
        var results = new List<ItemVendorPriceObservation>();

        const string query = """
            query($mpn: String!) {
              supSearchMpn(q: $mpn, limit: 1) {
                results {
                  part {
                    sellers {
                      company { name }
                      offers {
                        prices { quantity price currency }
                      }
                    }
                  }
                }
              }
            }
            """;

        foreach (var link in links)
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                var requestBody = JsonSerializer.Serialize(new NexarGraphQlRequest(query, new NexarGraphQlVariables(link.VendorSkuItemVendor)));
                using var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                using var request = new HttpRequestMessage(HttpMethod.Post, graphQlUrl) { Content = content };
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                var response = await client.SendAsync(request, ct);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("PriceTracking: Nexar HTTP {Code} for mpn={Mpn}.",
                        (int)response.StatusCode, link.VendorSkuItemVendor);
                    continue;
                }
                var json = await response.Content.ReadAsStringAsync(ct);
                var apiResp = JsonSerializer.Deserialize<NexarGraphQlResponse>(json, JsonOptions);
                var offer = apiResp?.Data?.SupSearchMpn?.Results?
                    .SelectMany(r => r.Part?.Sellers ?? [])
                    .SelectMany(s => s.Offers ?? [])
                    .FirstOrDefault(o => o.Prices is { Length: > 0 });
                var priceBreaks = offer?.Prices;
                if (priceBreaks is null || priceBreaks.Length == 0)
                {
                    _logger.LogDebug("PriceTracking: Nexar returned no price breaks for mpn={Mpn}.", link.VendorSkuItemVendor);
                    continue;
                }
                var reference = priceBreaks.OrderBy(p => p.Quantity).First();
                results.Add(new ItemVendorPriceObservation
                {
                    IdItemVendor = link.IdItemVendor,
                    PriceItemVendorPrice = (float)reference.Price,
                    CurrencyItemVendorPrice = reference.Currency ?? "USD",
                    QuantityItemVendorPrice = reference.Quantity,
                    PriceBreaksItemVendorPrice = JsonSerializer.Serialize(priceBreaks),
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PriceTracking: Nexar lookup failed for mpn={Mpn}.", link.VendorSkuItemVendor);
            }
        }
        return results;
    }

    private async Task<string?> GetNexarAccessTokenAsync(CancellationToken ct)
    {
        if (_nexarAccessToken is not null && DateTimeOffset.UtcNow < _nexarTokenExpiresAt)
        {
            return _nexarAccessToken;
        }
        var clientId = _configuration["Nexar:ClientId"];
        var clientSecret = _configuration["Nexar:ClientSecret"];
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            _logger.LogWarning("PriceTracking: Nexar:ClientId/ClientSecret not configured - Nexar sync skipped.");
            return null;
        }
        var tokenUrl = _configuration["Nexar:TokenUrl"] ?? "https://identity.nexar.com/connect/token";
        using var client = _httpClientFactory.CreateClient();
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
        };
        try
        {
            var response = await client.PostAsync(tokenUrl, new FormUrlEncodedContent(form), ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("PriceTracking: Nexar token request failed with HTTP {Code}.", (int)response.StatusCode);
                return null;
            }
            var json = await response.Content.ReadAsStringAsync(ct);
            var token = JsonSerializer.Deserialize<NexarTokenResponse>(json, JsonOptions);
            if (string.IsNullOrWhiteSpace(token?.AccessToken))
            {
                _logger.LogError("PriceTracking: Nexar token response did not contain an access_token.");
                return null;
            }
            _nexarAccessToken = token.AccessToken;
            _nexarTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(token.ExpiresIn - 60, 60));
            return _nexarAccessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PriceTracking: Nexar token request threw an exception.");
            return null;
        }
    }

    // ---- Helpers ---------------------------------------------------------------

    private static bool TryParseVendorPrice(string? raw, out float price)
    {
        price = 0f;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var cleaned = new string(raw.Where(c => char.IsDigit(c) || c == '.').ToArray());
        return float.TryParse(cleaned, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out price);
    }

    // ---- Mouser request/response models -----------------------------------------

    private sealed record MouserSearchRequest(
        [property: JsonPropertyName("SearchByPartRequest")] MouserSearchByPartRequest SearchByPartRequest);

    private sealed record MouserSearchByPartRequest(
        [property: JsonPropertyName("mouserPartNumber")] string MouserPartNumber,
        [property: JsonPropertyName("partSearchOptions")] string PartSearchOptions);

    private sealed record MouserSearchResponse(MouserSearchResults? SearchResults);

    private sealed record MouserSearchResults(MouserPart[]? Parts);

    private sealed record MouserPart(string? MouserPartNumber, MouserPriceBreak[]? PriceBreaks);

    private sealed record MouserPriceBreak(int Quantity, string? Price, string? Currency);

    // ---- Nexar request/response models -------------------------------------------

    private sealed record NexarTokenResponse(
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);

    private sealed record NexarGraphQlRequest(string Query, NexarGraphQlVariables Variables);

    private sealed record NexarGraphQlVariables(string Mpn);

    private sealed record NexarGraphQlResponse(NexarGraphQlData? Data);

    private sealed record NexarGraphQlData(NexarSupSearchMpn? SupSearchMpn);

    private sealed record NexarSupSearchMpn(NexarSearchResult[]? Results);

    private sealed record NexarSearchResult(NexarPart? Part);

    private sealed record NexarPart(NexarSeller[]? Sellers);

    private sealed record NexarSeller(NexarCompany? Company, NexarOffer[]? Offers);

    private sealed record NexarCompany(string? Name);

    private sealed record NexarOffer(NexarPrice[]? Prices);

    private sealed record NexarPrice(int Quantity, decimal Price, string? Currency);
}
